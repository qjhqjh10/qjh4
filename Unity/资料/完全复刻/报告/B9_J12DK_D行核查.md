# B9_J12DK — 战斗逻辑驱动 237 条 D（不适用）逐条打标签与复核

> 日期 2026-10-26（第八十四会话）· 只读核查代理 J12DK
> 输入 = `d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv`（1,374 行，其中 **状态==D 共 237 条**）
> 产出 = `判据/B9_战斗D_标签与核查.tsv`（237 行）· `账/_parts/B9_J12DK.tsv`（237 行补丁）· 本报告
> 起因 = 用户 2026-10-26 原话：「**D 不适用，这些是什么？为什么会这样？有什么影响？原版游戏离线都可以玩，说明代码什么的都在本地，那么应该是有一个自洽的代码逻辑。**」

---

## ① 结论

### 1·1 六类固定标签分布（237 条全覆盖，无 `D?`）

| 标签 | 含义 | 条数 | 占比 |
|---|---|---|---|
| **D1 原版不执行** | 三源 0 调用点 / 二进制空桩 / 体只有日志 / 体恒返回常量 | **85** | 35.9% |
| **D2 池内不可达** | 本 build 数据里 0 张卡 / 0 个 SO 能走到（**全部带时间限定**） | **21** | 8.9% |
| **D3 需服务端/账户/远端数据** | PlayFab / CloudScript / 远端 liveops / 账户进度，**断在哪一行**已写 | **47** | 19.8% |
| **D4 用户拍板不做** | 只有 1 条（`MatchData.ShouldOwnCards` ⇒ 用户 2026-09-17「卡池全解锁」） | **1** | 0.4% |
| **D5 我们有等价物 / 架构不同、不单独实现** | 替代物是谁 + 能不能指到行 已逐条判 | **48** | 20.3% |
| **D6 纯工具/调试/编辑器/本地化** | **逐条写清了影响面**（⛔ 没有一条只写「工具所以不管」） | **35** | 14.8% |
| **D? 判不准** | —— | **0** | —— |

次级标签 21 条（D2+D3 组合为主，例如「远端 liveops 表 0 条」既是 D3 也是 D2）。

### 1·2 抽检 35 条（14.8% ≥ 10%）的结果

- 抽检重点 = **D1（15 条）/ D5（10 条）/ D2（6 条）**，另 D3 3 条、D6 1 条。
- **35 条里 8 条站不住**（22.9%）⇒ 建议翻档：`986 / 989 / 996 / 1094 / 1180 / 1234 / 1280 / 1361`。
- 另外 **15 条站得住但备注必须订正**（存在活调用点却没写、判据要点是模板文本、理由与判据自相矛盾等），见 §二·2。
- 加上「全量 237 条调用点普查」（§二·0）另抓出的 15 条，**合计 23 条建议翻档**。

### 1·3 最该翻档的前 5 条

| 序 | 账行 | 方法 | 为什么站不住 | 建议 |
|---|---|---|---|---|
| 1 | **1280** | `RawCardScript.ReleaseFullHeroSprite()` | 差异列自己写「归音频/语音线 + 特效线（不在战斗逻辑域，**也不是「原版没有」**）」—— 这正是《方案》§4.1 明令**不合法**的 D 理由（移交 ≠ 不适用）；且 **B8_AT 已把同一族三条 Addressables 释放改判 C**（`CardArt._cache` 只增不减是真差异）。**4 个活调用点** | **改 C**（与「收藏:203」同一工作项，≈25 行） |
| 2 | **989** | `CardTraitCollection.GetTrait(DefinedTrait)` | `CardTraitCollection__GetTrait(` 在 `decomp_full` 有 **17 个外部文件**调用（`AI.ValueOfTraits` / `BattleCardUI.*` / `CardTexts` / `CardDisplayWindow`…），ilspy 侧 7 个文件同样命中 ⇒ **活方法** | **改 B**（我们已有 trait 表 `Core/Badges.cs` / `Core/CardIcons.cs`） |
| 3 | **996** | `CardTraitCollection.Init()` | `CardTraitCollection__Init` 有 **2 个活调用点**（`BattleManager__SetupDataDependentElements.c` / `PlayerDataManager__Awake.c`），`CardTraitCollection.Instance` 另有 **6 个外部文件**调用（`TextTooltipController__GetSpiritStoneTooltip` / `GetFaithTooltip` / `GetQPTooltip` / `GetSabotageTooltip` / `GetRangedTooltip` / `GetMeleeTooltip`）⇒ 活的初始化链 | **改 B** |
| 4 | **1234** | `RawCardScript.GetLocalizedCardName(string)` | **53 个外部文件**在 decomp 调用它（ilspy 侧 34 个）⇒ 本地化主入口之一，不可能是「不适用」 | **改 B**（或按 `Loc.T` 落点升 A）；同族 8 条（1221/1229/1230/1232/1233/1239/1241）一并改 B |
| 5 | **1361 / 1364** | `TurnExtensions.GetFinishedTurn` / `SetFinishedTurn` | **状态与备注自相矛盾**：备注自己两处结论都写「**保持 B**」（F3 第4轮 / B8_L 第8轮），状态却是 **D** | **按备注自身结论改 B**；否则要补「原版 0 消费方 ⇒ 不适用」的判据行（目前没有） |

> 另 2 条「理由不合法」的：**1279** `PreloadAssets()` 差异列同样写「归音频/语音线…也不是原版没有」⇒ 建议改 B。
> 另 1 条同类群发：**986** `GetAbilityToTraitDict` —— 调用点在 `CardTraitCollection__GetTraitInPlayListForIcons.c:112`，而 `GetTraitInPlayListForIcons` 被 `BattleCardUI.GetAbilityListInPlayForIcons` / `UpdateTraitIcons` 调用 ⇒ 可达 ⇒ 建议改 B。

### 1·4 用户那句质疑的答复（本条最重要的结论）

> 「原版游戏离线都可以玩，说明代码什么的都在本地，那么应该是有一个自洽的代码逻辑。」

**代码确实都在本地，但「本地有代码」≠「离线会执行」。** 237 条 D 拆开后是这样：

- **85 条（D1）是原版自己就不执行**：体是空的（`RVA 0x4B33B0` 全局空桩 / 两处反编译都空）、体只有一行 `CustomDebug.Log(… "Implement")`、或全仓 0 调用点。**这不是「我们没做」，是「原版也没做」** —— 典型如 `PlayBossAnim`「整支一句 `LogWarning("[WARNING] Implement")`」。
- **47 条（D3）断在服务器/账户**：原版是**在线游戏**，比赛结算、ELO、机器人难度表（`AIBotsConfig`）、体力（`EnergyContext`）、里程碑都在 PlayFab / liveops 下发。**离线启动后这些路径拿不到数据**，这与「客户端代码完整」并不矛盾。
- **21 条（D2）是本地代码活、但本 build 的卡池/数据一张都走不到**：例如陷阱（`spellType==70`）、`buffType 35`（daemonHost）、`deckCriteria`（1351 张全零）、`trigger 210/250/270/450/460/646` —— 我用 `cards_truth.json` **全池 1351 张递归扫过，全部 = 0**（对照组：`trigger 120 = 22` / `240 = 35` / `46 = 105`，证明扫法有效）。**这类全部带时间限定**「数据若出现需重跑」。
- **48 条（D5）是原版有行为、但我们用等价机制承担**：如 2D 投影（`Get2DPosOfCard` 等 9 条 → `TargetReticle.ResolveAim`）、牌库逐卡视图（`HideDeckAtStart` / `DestroyHiddenCards`）、逐卡子 Canvas（`ToggleOverrideSorting`）。
- **35 条（D6）是纯工具/调试/编辑器/本地化**：逐条写了影响面（谁在用它、影响哪块 UI）。
- **1 条（D4）** 是用户拍板：卡池全解锁 ⇒ `ShouldOwnCards` 恒真。

⇒ **「自洽」的地方在于：原版离线能跑，是因为它把「拿不到服务器的部分」全部降级成不执行/空转；那部分正是本表的 D。**

---

## ② 逐条证据

> 237 行的逐条判据在 `判据/B9_战斗D_标签与核查.tsv`（列：`账行号 | 原版类 | 方法 | 原标签理由(摘要) | 主标签 | 次标签 | 抽检了没 | 三源结果 | 结论 | 证据(文件:行) | 还差什么`）。本节只给方法与关键证据。

### 2·0 全量 237 条的调用点普查（不是抽检，是普查）

方法：`decomp_full` 的 mangled 名 `类__方法`，**不带括号**、加「尾字符不能是 `[A-Za-z0-9_]`」守卫（防 `DebugActions` 误命中 `DebugActionsInQueue`）。单遍扫 25,096 个 `.c`。

| 结果 | 条数 |
|---|---|
| decomp 有外部调用点 | **123** |
| decomp 0 外部调用点 | **114** |

**方法学教训（实测踩到，已绕过）**：`grep "Class__Method("` **带括号会漏** —— Ghidra 把实参换到下一行。实测 `BattleManager__AddMissAction(` / `AddDisappearUnit(` / `AddDiscardFromHand(` / `AddRemoveEnchantment(` 四个全都返回「0 调用点」，**而 `AbilityLogic__PlayAbility.c:338/597/2280/2360/2525/2789` 全都在调它们**。⇒ 判「0 调用点」**一律用不带括号的名 + 尾字符守卫**。

**对照组（证明扫法有效）**：`BroadcastCardDrawn`=2 文件、`GetCardInPos`=4、`DrawCardFromDeck`=5、`AddManaBuff`=4；`cards_truth` 侧 `trigger 120`=22 张、`240`=35、`46`=105。

### 2·1 23 条建议翻档（逐条证据 + 应改什么）

| 账行 | 方法 | 证据 | 建议 |
|---|---|---|---|
| **986** | `CardTraitCollection.GetAbilityToTraitDict` | 调用点 `CardTraitCollection__GetTraitInPlayListForIcons.c:112`；而后者被 `BattleCardUI__GetAbilityListInPlayForIcons.c:16` 与 `BattleCardUI__UpdateTraitIcons.c:59` 调用 | 改 B |
| **989** | `CardTraitCollection.GetTrait` | 17 个外部文件：`AI__ValueOfTraits.c` / `BattleCardUI__DisplayStatusAnim.c` / `BattleCardUI__DisplayTriggerAnim.c` / `BattleCardUI__ShowCodex.c` / `TextTooltipController__GetTraitTooltip.c` / `CardScript__AddEffect.c` …；ilspy 7 文件 | 改 B |
| **996** | `CardTraitCollection.Init` | `BattleManager__SetupDataDependentElements.c` / `PlayerDataManager__Awake.c` 直调；`CardTraitCollection__get_Instance(` 另有 7 处 | 改 B |
| **1031** | `EntityScript.GetCurrentTraitData` | 1 个活调用点 `CardScript__RemoveBastionDamage`；**备注为空**、无任何 D 依据 | 改 B（需给 CardDef/CardIcons 落点） |
| **1094** | `MinionManager.AddMinionOccupation` | 1 个活调用点 `AbilityLogic__PlayAbility`；**备注为空** | 改 B（连续无洞模型 + BoardSlots 需方法级说明） |
| **1106** | `MinionManager.GetCardInPos` | 1 个活调用点 `BattleManager__Update`；**备注为空** | 改 B |
| **1138** | `MinionManager.PositionWillBeAtLeft` | 1 个活调用点 `ScriptedActionData__CheckIfMatchesActionData`（脚本/教程动作匹配）；备注只写「教程线」 | 改 B（或改 C，教程线要逐个核） |
| **1140** | `MinionManager.RefreshOccupationSlots` | **4 个**活调用点（`_ResolveSummonUnit` / `ActivateMinions` / …）；备注为空 | 改 B |
| **1162** | `PlayerHand.GetCardInPos` | 1 个活调用点 `BattleManager__GetAvailableActions`；备注为空 | 改 B |
| **1175** | `PlayerHand.IsRawCardInHand` | 1 个活调用点 `AI__ShouldPlayComboCard`；备注为空 | 改 B |
| **1180** | `PlayerHand.NumCardsInHand` | **3 个**活调用点（`AI__ScoreFromKeepingTheCard` / `AbilityLogic__PlayAbility` / `GetAvailableActions`）；备注为空 | 改 B |
| **1221** | `RawCardScript.GetCardNameTerm` | 2 个活调用点；备注为空 | 改 B |
| **1229** | `RawCardScript.GetLocalizedAbilityDesc` | 3 个活调用点；备注为空 | 改 B |
| **1230** | `…RemovingSiege` | 1 个活调用点 `CardScript__GetCurrentLocalizedAbilityDescription`；备注为空 | 改 B |
| **1232** | `RawCardScript.GetLocalizedCardDesc` | 4 个活调用点；备注为空 | 改 B |
| **1233** | `…WithLineBreak` | 3 个活调用点；备注为空 | 改 B |
| **1234** | `RawCardScript.GetLocalizedCardName` | **53 个**外部文件（ilspy 34）；备注为空 | 改 B |
| **1239** | `RawCardScript.GetLocalizedFlavourText` | 1 个活调用点 `CardDisplayWindow__SetCardLore`；备注为空 | 改 B |
| **1241** | `RawCardScript.GetLocalizedTargetted` | 1 个活调用点 `_ChooseUnitTarget`；备注为空 | 改 B |
| **1279** | `RawCardScript.PreloadAssets` | 差异列写「归音频/语音线…**也不是「原版没有」**」= 自认不符合任何合法 D 理由；1 个活调用点 `BattleCardManager__PreloadCard` | 改 B（移交，状态待那边核） |
| **1280** | `RawCardScript.ReleaseFullHeroSprite` | 同上 + **B8_AT 已把同族三条 Addressables 释放改判 C**（`CardArt._cache` 只增不减 = 真差异）；4 个活调用点（`DeckInfoPopup__Close` / `DraftModeMenuDemo__ReleaseMenuWarlordImage` / `DraftRunDisplay__OnDestroy` / `PlayerDeckDemoDeckInfoWrapper`） | **改 C** |
| **1361** | `TurnExtensions.GetFinishedTurn` | 备注两处自称「保持 B」，状态是 D；decomp 0 外部调用点 | 改 B（按备注自身结论） |
| **1364** | `TurnExtensions.SetFinishedTurn` | 同 1361 | 改 B |

**系统性根因**：23 条里 **10 条集中在 `RawCardScript` 的本地化族**、**4 条在 `MinionManager` 的占位表族**、**3 条在 `CardTraitCollection`** ⇒ 这是「**按类批量判 D**」的后果（同一族里有一条被认定不适用，其余跟着填 D，且**备注为空**）。**96 条 D 的备注是空的**，正是这个形状。

### 2·2 抽检里「维持 D 但必须订正」的代表（不改状态）

| 账行 | 方法 | 问题 | 订正要求 |
|---|---|---|---|
| 66 | `AbilityLogic.CanPlayAbility` | 备注写「TriggerAbilities/ResolveAbilities 里没有它」，**漏了另一跳** `RawCardScript__CanPlayAbilities.c:25`（该函数自身 0 调用 ⇒ 链仍断） | 补上这一跳再写「链断」 |
| 877 | `CardScript.ResolveWeaponKilled` | 只有 decomp + cpp2il 双源 0 调用 | **第三源（prefab `m_MethodName` / SO AssetReference）未查**，要求补 |
| 239 | `BattleManager.CancelAnimsFromUnit` | 备注写「铁律 11 例外①（原版本身没有）」，但判据要点描述的是**有体方法** | 理由应改「四条队列结构与我们不同（D5）」，不能写「原版没有」 |
| 315 | `BattleManager.GcCollectWhileInactive` | 判据要点仍是**模板文本**「表现层 / HUD / 动画 / 输入 / 声音」（CLAUDE.md 记过这是旧记录的错误填法；265/200 已订正，本条漏了） | 就地改成「只触发一次 GC、不写状态」（B8_AT 已有 cpp2il:35259 的活调用点证据） |
| 413/426/486 | `IsDesiredHumanOpponent` / `IsSupiciousGame` / `RequiresResultRecording` | 备注只写「按口径 1 / 口径 4 判 D」，**没写具体依据行** | 补「原版调用链断在这儿」的代码行 |
| 1005/1006/1010 | `DamageDeltToEnemyUnit.*` | 备注写「替代物」，但**没写这个类不是死类**：`assets_full` 里 **7 份 `ACH*.json` 的 `m_Script` 指着它**（`bundle_staticgeneralassets_assets_all/MonoBehaviour`） | 必须写「判 D 理由 = 替代物（`Ctx.DamageToEnemyWarlord` + `ProfileData.ApplyAchievementTally`，`BattlePresentation/Battle/BattleDriver.cs:31845`）」，⛔ 不能写「死代码」 |
| 1008/1009 | `EnemyHeroDamageScorer.*` | 备注「断链（零实例）」正确 —— 已复核：类名在 `assets_full` **只命中 `globalgamemanagers/MonoScript/MonoScript_1619.json`（类元数据）**，无任何 prefab/SO/场景挂接 | 维持 D1 |
| 987/988/995 | `CardTraitCollection.*` 三个 dict-builder | 0 外部调用点，但 `Init` 体里出现 ICF 折叠名 ⇒ **可能已被内联**，而 `Init` 是活的 | 维持 D1，但备注要写「内联未证；若证明确由 Init 内联，应改 B」 |

### 2·3 六类的判据与代表证据

- **D1（85）** 四种子形状：①**空体/空桩**（`ClickSettings` `return;`、`RunCheckAchievements` RVA `0x4B33B0`、`ShowAbilityAnim`/`ShowAbilityIcon` 双反编译都空、`SetCardTrackedRotation` 双空）②**只有日志**（`ShareScreenResult` 整链日志、`ShowDescription` 一条 Log、`ShowManaCostAura` 一条 LogWarning、`PlayBossAnim`/`PlayCampaignBossAnim`/`PlaySkullsAnim` 一句 `LogWarning("Implement")`）③**0 调用点**（`AutoActionsLeftInQueue`、`HasMutation`、`GetAvailableMarksOfChaos`、`ResolvePsyker`/`ResolveReactor`/`ResolveWeaponKilled`）④**链断**（`AbilityLogic.CanPlayAbility` 的唯一调用点自身 0 调用、`EntityScript.GetDaemonHosts`/`GetMarksOfChaos` 的调用者本身 0 调用、`TargetCriteria.DeepCopy` 的 5 个调用点全在 `DeepCopy` 死链里）。
- **D2（21）** 硬数字（`cards_truth.json` 1351 张全扫）：`spellType 70 = 0` / `15 = 0` / `60 = 0` / `95 = 0`；`trigger 210 = 250 = 270 = 450 = 460 = 646 = 0`；`plasmaCost`/`psychicCost` 键存在（145 张）但**值全为 0**；`deckCriteria` 1351 张全零默认对象。**每行备注都加了「数据若出现需重跑」+ 具体重开条件。**
- **D3（47）** 断点都点到行：`_ShowFinishScreen_d__362__MoveNext`（ELO，包在 `onlineMatchResult != null` 内）· `MatchData__IsInFTUEState.c:30/36/37`（三条输入/远端 liveops）· `Currency.cs:93` / `CurrencyExtensions.cs:130,180,210` / `RewardService.cs:671,843`（体力）· `AIBotsConfig`（`grep -ral BotListFTUE|skipChance|minLossStreak` 在 `assets_full`+`关服前存档` = **0 文件**）。
- **D4（1）** `MatchData.ShouldOwnCards` —— 1 个活调用点 `SearchOpponentManager__ValidateDeck`；本项目按用户 2026-09-17 边界②「卡池全解锁」⇒ 恒真。
- **D5（48）** 替代物都写清了：2D 投影 9 条 → `TargetReticle.ResolveAim`；牌库/墓地逐卡视图 4 条；逐卡子 Canvas 2 条；单例/实例化模型 3 条；`GetBoardPosOfCard` → transform 本身；`CardPosAndScaleToUI` → 单套世界坐标 + `HandLayout` 重算。**判 D5 时逐条问了「等价物能不能指到行」**，指不到的（22 条）已改判 B。
- **D6（35）** 每条都写了影响面与「替代物未落盘」的催促，重点两条：`SupportMethods.ModifyLocalization`（18 个活调用点，影响全部上屏文本后处理）· `SupportMethods.GetTimeString`（9 个活调用点，影响所有倒计时）。

---

## ③ 没查清（如实记）

1. **ilspy 树（`out_cpp2il_dev_ilspy`）是扁平的 per-class `.cs`**，跨类调用**不带 `Class__` 前缀** ⇒ 无法按 `类__方法` 精确到类。本次抽检只能做「同名方法在哪些文件里出现」的粗核（对 `Add` / `Init` / `Initialize` / `Update` 这类通用名**完全无效**，实测 `CircularBuffer.Add` 命中 137 个文件、`Initialize` 命中 390 个文件 = 纯噪声）。**本表「三源」= ① `decomp_full`（mangled 名，可精确到类）② `assets_full`（MonoScript / prefab / SO）③ 我方工程源码。**
2. **第三源（prefab `m_MethodName` / SO AssetReference）只对 6 个类做了 GUID 级核对**（`DamageDeltToEnemyUnit` / `EnemyHeroDamageScorer` / `TroopsKilledCount` / `YourTurnUI` / `CircularBuffer` / `CardTraitCollection`），**其余 231 行未做**。⇒ 「0 外部调用点」的 114 条里，理论上仍有「挂在 prefab UnityEvent 上」的可能（`ViewDeckInfoPopup` 那个先例就是这么翻的）。
3. **724 条无方法体的调用方仍未覆盖** —— 所有「0 调用点」结论都带这个残余风险（`AbilityLogic.CanPlayAbility` 就是靠另一条路才发现的）。
4. **`AddXxx` 族仍可能漏检**：本次已改用「不带括号 + 尾字符守卫」，但 Ghidra 若把方法名整个折叠进 `FUN_xxxx`（ICF），仍查不到。
5. **`CardTraitCollection` 的 4 个 dict-builder（987/988/995 + 986 的一半）是否被 `Init` 内联**未证 —— `Init` 体里出现 `NetworkingPeer__OnMessage` / `List<AnimatorClipInfo>__GetEnumerator` 这类 ICF 折叠名，说明 Ghidra 把多个函数折叠到同一地址。若证明是内联，它们的**行为是活的**，D1 就不成立。
6. **D6 的 35 条「替代物未落盘」**：本次只标了「影响面 + 替代物未落盘」，**没有逐条去我方代码里找出等价实现**（那需要逐方法读我们 20 多个 `*.cs`）。这是 35 条里最大的未覆盖面。
7. 行 **1368** `WarlordPathData.GetCampaignPathData` 的「服务端」结论沿用 `判据/战役开局_判据与落点.md §2·7`，**本次未重新取证**。

---

## ④ 改动清单

**新增 3 个文件，⛔ 未改任何既有文件：**

| 路径 | 内容 |
|---|---|
| `d:/4/Unity/资料/完全复刻/判据/B9_战斗D_标签与核查.tsv` | 237 行 × 11 列（含表头） |
| `d:/4/Unity/资料/完全复刻/账/_parts/B9_J12DK.tsv` | 237 行 × 11 列补丁，**只改「备注」列**（追加 `【J12DK 2026-10-26 D标签】…`），⛔ 未改「状态」 |
| `d:/4/Unity/资料/完全复刻/报告/B9_J12DK_D行核查.md` | 本报告 |

**校验（全部通过）：**
- 补丁：237 行 · 11 列全对 · 表头与主表**逐字相等** · `方法` 列 237/237 带 `(` · **状态列改动 = 0** · 其余 6 个非目标列改动 = 0 · **LF 行尾、裸 CR = 0**（主表也是纯 LF）。
- 官方合并工具 dry-run（`merge_ledger_parts.py --dry --parts <隔离目录>`）：**「更新 237 · 新增 0 · 无变化 0（dry-run，未写盘）」**。
- 跨表键冲突：237 个 `(原版类, 方法)` 在**全部其它 20 份主表里 0 命中**，本表内也 0 重复 ⇒ 合并不会撞键。

**⛔ 没做（红线）**：没改 `.cs` / 资产 / 主账表 / 正本；没跑 Unity；没动 git。

---

## ⑤ 🔴 顺手发现（不归本条但必须报）

1. **「按类批量判 D」是系统性风险**：`CardTraitCollection` 6 条 D 里 **3 条被推翻**（986/989/996），而 `RawCardScript` 本地化族 8 条、`MinionManager` 占位表族 4 条属于同一形状。**96 条 D 的备注是空的** ⇒ 建议下批对「备注为空 + 同族 ≥3 条」的组合做一次专项复查。
2. **`decomp_full/CircularBuffer__GetBuffer.c` 文件名与内容不符**：内容是 `<>f__AnonymousType0<object>__get_itemId`（一个匿名类型取值器）。⇒ `CircularBuffer.GetBuffer`（账行 999）的**真体读不到**。这是已抓到 6 例「文件名与内容不符」之后的**第 7 例**。
3. **行 315 `GcCollectWhileInactive` 的「判据要点」仍是模板文本**「表现层 / HUD / 动画 / 输入 / 声音」。CLAUDE.md 记过这类模板是旧记录的错填法（265 / 200 都已订正），本条漏改 ⇒ 建议就地订正（铁律 5）。
4. **行 239 `CancelAnimsFromUnit` 的理由与判据矛盾**：备注写「铁律 11 例外①（原版本身没有）」，但「判据要点」描述的是有体方法（对四条队列各跑一次取消）。
5. **行 66 `AbilityLogic.CanPlayAbility` 漏了一跳**：备注只比较 `TriggerAbilities` / `ResolveAbilities`，漏了 `RawCardScript__CanPlayAbilities.c:25`（结论不变 —— 该函数自身 0 调用）。
6. **`DamageDeltToEnemyUnit` 不是死类**：7 份 `ACH*.json`（`bundle_staticgeneralassets_assets_all/MonoBehaviour/ACH1 Slay the Warlord.json` 等）的 `m_Script` 指着它。**将来若有人按「死代码」重判，会错** —— 它的 D 只能靠「替代物」。
7. **方法论（建议写进 `资料/工作准则_操作经验.md`）**：判「0 调用点」**⛔ 不能用 `grep "Class__Method("`** —— Ghidra 会把实参换到下一行，实测一次漏掉 4 个真实调用点（`AddMissAction` / `AddDisappearUnit` / `AddDiscardFromHand` / `AddRemoveEnchantment`，全在 `AbilityLogic__PlayAbility.c`）。正确写法 = 不带括号的 `Class__Method` + 尾字符守卫（`(?![A-Za-z0-9_])`），并**必配对照组**（已知有调用点的方法必须扫出 >1 个文件）。
8. **值得当范式的一条**：`TargetCriteria.DeepCopy`（1354）有 5 个调用点、全部落在 `DeepCopy` 死链里 —— 备注写死「有调用方 ≠ 可达」，正好挡住「下轮只看 :116/:119 就判回 B/C」。**这种「链式断链」的写法应该推广到其余 6 条链式 D**（66/68/1034/1035/1354 等）。
9. **`YourTurnUI.FinishAnim`（1370）的 decomp 命中是 ICF 折叠**：`UIRankingListController___SetTimer_b__19_0.c` 的首行就是 `YourTurnUI__FinishAnim`，体不是它自己 ⇒ 不是调用方。与 CLAUDE.md 那条「首行名不是自己，也可能是 ICF 折叠」一致，**别拿它当调用证据**。

---

## ⑥ 追加（2026-10-26 · 第二批：23 条状态补丁 + 用户点名的「旧 grep」量化）

### 6·1 状态补丁

`账/_parts/B9_J12DKb.tsv` —— **25 行**：**22 条 D→B**（`等级` 留空）+ **1 条 D→C**（`1280 ReleaseFullHeroSprite`，`等级 = P2`：无玩家可见症状、只在长会话里累积纹理内存；若真机发现内存增长可升 P1）+ **2 条只改 `备注`**（`315` 模板文本 / `239` 理由与判据矛盾）。
每行 `备注` 都带 `｜🔴 2026-10-26（J12DK 三源复核 · 调度台采纳）：D→B/C —— <理由 + 外部调用点数>`；`判据出处 / 判据要点 / 我们的落点 / 轮次` **一格未动**；`差异` 列在 23 条上都加了「⚠️ 2026-10-26 订正（J12DK）：原记「不适用」，实为 B/C —— … 原文：…」（保留更正痕迹，铁律 5）。

### 6·2 「靠旧 grep 判的」量化（用户/调度台点名要的）

| 口径 | 237 行里 ext=0 的行数 |
|---|---|
| 旧写法 `grep "Class__Method("`（带左括号） | **125** |
| 正确写法（不带括号 + 尾字符守卫 `(?![A-Za-z0-9_]`)） | **114** |
| ⇒ **旧写法会误判为「0 调用点」** | **11 行** |

这 11 行：`56 GetRandomCardTier` · `129 AddDisappearUnit` · `130 AddDiscardFromHand` · `146 AddMissAction` · `155 AddRemoveEnchantment` · `317 Get2DPosOfSpecificItemInCard` · `332 GetBoardPosOfCard` · `671 DeckMeetsCriteria` · `986 GetAbilityToTraitDict` · `1272 OnTrapDrawn` · `1273 OnTrapResolved`。
**只有 `986` 一条因此改变结论**（已进 23 条翻档名单）；其余 10 条的 D 依据**不是**「0 调用点」（129/130/146/155/671/1272/1273 是 D2 数据不可达、317/332 是 D5 等价物、56 是 D3 远端池）⇒ 结论不变。

**114 条 ext=0 里，备注明确写了「0 调用点 / 死代码 / 仅定义处」的有 40 条。** 这 40 条**已全部用正确写法重扫，40/40 仍然是 0** ⇒ **没有一条因旧 grep 而站不住**。但它们**没写当时用的搜法**，所以只能说「结论已用正确方法复核通过」，不能说「当时的依据可靠」。
⇒ **结论：需要重判的 = 0 条；需要补一句「已按正确搜法（不带括号 + 尾字符守卫 + 对照组）复核」的 = 40 条。**

### 6·3 自检

① 键唯一命中：隔离目录 `merge_ledger_parts.py --dry` ⇒ **更新 25 / 新增 0 / 无变化 0**。
② `verify_annot`：基线 同名 803 / 异名已注解 2782 / **缺注解 0**；把补丁合进**账目录副本**后再跑 ⇒ 803 / 2782 / **缺注解 0**（不变）。
③ `check_patch_conflicts.py`（不带 `--all`）⇒ **「✅ 没有跨补丁重复键（2 份补丁）」**（在飞的是 `B9_J12DKb` 与 `B9_W13EN`）。
⛔ 主表未被本轮触碰（仍 1,375 行 / D=237 → 合并前）。
