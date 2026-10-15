# D3 · 结算/胜负门时序 + 战斗音响「时机 → cue」总表（2026-10-15 · 只读普查）

> 触发 = `战斗复刻_正本.md` **§八** 的 **#8（结算 / 胜负门时序）** 与 **#7（战斗音响「时机 → cue」）**，
> 这两条是正本自陈「**没有任何文档系统分析过**」的。
> 甲方 = `d:/2/tools/decomp_full/`（26,282 个方法）+ `d:/2/il2cpp_out/dump.cs` + `assets_full/bundle_scenes_scenes_battlearena2/MonoBehaviour/` + 运行时 dump。
> ⚠️ **本文是子代理报告原文，主对话只做了转存**；本会话口径：只做不验。

---

## 结论

1. **结算**：原版**没有独立的「结算」系统** —— 承载它的是 `BattleManager`（`MonoBehaviour_4569.json`，字段 `endBattleDoor` / `battleScoreManager`）+ `BattleDoors/Canvas/EndBattlePanel` 节点树 + `EndBattleDoors` 组件（`MonoBehaviour_5455.json`）。**`EndBattlePanel` 在 26,282 个方法里零命中**（它不是类，只是节点名）。
2. **胜负判定在「每条动作解析完」那一刻**，不是回合末：`BattleManager.FinishResolvingAction`（70+ 个 `Resolve*` 收尾都调它）末尾查双方督军 `cardState`，任一方 `==5(waitingToDie)` → `DeadHero(玩家是否输, BattleResult)`。回合末只是**动作之一**。
3. **骷髅分 = 敌方督军生命跨过 {20,10,0} 三档的档数**（`HealthThresholdData.HealthThreshold`，来自 `MatchData.GetMilestones()`，按 `playMode` 给 3 档或空数组）。`BattleScoreUiManager` 只负责把这档数画成 `x N` + 三个图标，**它不算分**。我们已按同一口径实现（`RuleEngine/Core/DeckRules.cs:109 SkullThresholds={20,10,0}`、`BattleDriver._foeSkullCount`）。
4. **音响**：正本 §八 #7 引的「战斗内 45 键 SFX」是 **Godot 时代 `SFX.gd` 的账**（`d:/2/战斗缺口清单_0827.md:30`），**不是原版 Unity 的**。原版战斗 SFX 真身 = `SoundAssetCollection` **28 条 cue** + `BattleCardSounds` **9 条 cue** + `EndBattleDoors` 3 条结算 cue + 卡上序列化 `AudioCue`。
5. **差多少**：结算链我们**做完了**「胜负门 → 面板 → 开门片 → 骷髅行 → 投降钮 → 对局记录」，差的是**服务器那段**（奖励/评分/XP 在服务器，本地无判据）与 `EndBattleDoors` 的 ratings/seals 那半屏；音响我们**只做了**残骸体 6 条 + 开门 3 条 + 超时 1 条 + VO —— **28+9 条战斗 cue 一条都没接**。

---

## 表 A：结算与胜负门时序

| 环节 | 原版行为 | 触发条件 | 时序/时机 | 我们做了没 | 出处 |
|---|---|---|---|---|---|
| 胜负门唯一入口 | `BattleManager.DeadHero(bool playerIsLoser, BattleResult reason)` | 见下面各行 | 立即（同步） | ✅ `RuleCore.CheckWinner` | `BattleManager__DeadHero.c:2` · `dump.cs`(BattleManager) |
| **战斗中督军倒下** | `FinishResolvingAction` 收尾：双方督军 `cardState` 都非 5 就 `BroadcastWhileInPlay`；否则 `DeadHero(battle, 玩家督军.cardState==5, BattleResult.BattleVictory=1)` | 每次 `Resolve*` 演完（70+ 调用点） | **队列演完那一刻，不是回合末** | ✅ 我们的 `CheckWinner` 也在动作收尾 | `BattleManager__FinishResolvingAction.c:56-88`（`:84` 即 DeadHero 调用）· `CardScript__ResolveDeadCard.c:314` · `CardStateOptions{waitingToDie=5,inCemetery=6}` |
| 投降（我方） | `ResignButtonOnClick` → `BattleManager.ClickExitBattle`：联网先 `SendForfeit()`；`AddResignAction(battle,1)`（制造 `PlayerAction` type `0x36=54`）→ `DeadHero(battle,1,BattleResult.Forfeit=2)` | 点 `BattleSettingsPanel/Bottom buttons/Resign Button` | 点击当帧 | ✅ `Battle/SettingsPanel.cs` → `BattleDriver.Forfeit` | `BattleSettingsWindow__ResignButtonOnClick.c:27` · `BattleManager__ClickExitBattle.c:47,49,59` |
| 对面投降 | `ReceiveEnemyForfeit` → `DeadHero(battle,0,Forfeit=2)`（判我方胜） | 收到 PvP `SendForfeit` | 收包即判 | ✅ `BattleDriver.NetRemoteResign` | `BattleManager__ReceiveEnemyForfeit.c:37` |
| 教程拦住投降 | `preventPlayerResign` 教程阶段：`ClickExitBattle:57` 直接走脚本，不判负 | 教程阶段 `+0xb9` | — | ✗ | `ClickExitBattle.c:50-76` |
| 掉线 | `_ForfeitDisconnectedEnemy` / `FailedToReconnectAfterDisconnect` → `DeadHero` | 重连超时协程 | 倒计时结束 | ⚠️ 我们只走 `NetKind.Resign` | `BattleManager._ForfeitDisconnectedEnemy_d__322__MoveNext.c` · `__FailedToReconnectAfterDisconnect.c:32` |
| 取消对局 | `CancelMatch(battle, param)`：`param!=0` → `AddResignAction(0)`+`DeadHero(0,Cancelled=5)` | — | — | ✗ | `BattleManager__CancelMatch.c:28-29` |
| 调试钮 | 面板 `Debug Buttons/Buttons container/{Win,Draw}` → 4 / 6 | 仅调试 | — | ✗（我们无入口） | `DebugWinBattle.c:15` · `DebugDrawBattle.c` |
| **Winner 取值** | `BattleWinner{Undefined=0, Player=10, Enemy=20, Draw=30}`；`playerIsLoser==0 ⇒ 10`，`==1 ⇒ 20` | — | — | ✅（我们用 1/2/3，**编号不同**） | `dump.cs` `BattleWinner` · `DeadHero.c:244-251` |
| **平局判定** | 取**胜方**督军：`cardState==5` 或 `生命<1` 或 `cardState==6` 或 `reason==DrawButton(6)` ⇒ **改判 Draw(30)** | 上述四条任一 | 与 winner 同一次 | ⚠️ 我们 `Winner==3` 的来源要另核 | `DeadHero.c:253-269` · 公开版 `BattleManager__GetWinnerAfterBattleEnd.c:23-47` |
| 教程强制胜 | 教程阶段 `playerAlwaysWins` ⇒ 强制 `10` | Campaign/Tutorial 阶段 | — | ✗ | `DeadHero.c:270-285` · `GetWinnerAfterBattleEnd.c:49-68` |
| BattleResult 六档 | `Undefined0 / BattleVictory1 / Forfeit2 / Disconnect3 / WinButton4 / Cancelled5 / DrawButton6` | — | — | ✅ 同名同值 | `dump.cs` `BattleResult` · 我们 `RuleEngine/Core/RuleCore.cs:56-71` |
| 阶段闸（防重入） | `_battleStage`(`0x294`) 必须已 = `PostBattle(60)`，否则 `LogError` 返回；`UIstate`(`0x290`) 先写 7 再写 8 | — | DeadHero 内先写 stage | ⚠️ 我们用 `_settled` 闩 | `dump.cs` `BattleStage` / `UIstateOptions` · `DeadHero.c:45,157,164` |
| 停战 | `StopBattleAfterBattleEnd`：停 5 条协程 · 关 `TutorialPointer` · `ClockManager.StopClock` · `VoiceLinesController.ForcePopUpClose` · 关 `activeSkillDesc` | 紧跟 DeadHero | 同步 | ⚠️ 部分 | `BattleManager__StopBattleAfterBattleEnd.c:13-49` |
| 记结果 | `RankedMatchResult{resultSaved,targetMilestone=GetSkullCount(),enemyMilestone=GetEnemySkullCount(督军生命),duration=分钟,battleResult,battleWinner}` | — | DeadHero 尾部 | ⚠️ 我们记对局记录，无 `RankedMatchResult` | `dump.cs` `RankedMatchResult` · `DeadHero.c:296-337` |
| 结果上传 | `SaveMatchWinner` → `ProcessMatchResult` → `PlayerDataManager.ProcessMatchEndResult` / `GameModes.CombatFinished`（**走服务器**）→ 回调 `FinishedProcessingMatchResult` | stage==60 且 `finishedRecordMatchResult` 未置 | 网络往返后 | ✗ **在服务器、本地无判据** | `SaveMatchWinner.c:34,41` · `ProcessMatchResult.c:177,198` |
| 打开结算 | `FinishedProcessingMatchResult` → 协程 `ShowFinishScreen` → 等语音气泡 → `CloseBattleDoors` | 服务器回调 / 奖励落地后 | 协程 | ✅ | `FinishedProcessingMatchResult.c:45-46` · `_ShowFinishScreen_d__362__MoveNext.c:108-150` |
| **开门 + 面板** | `CloseBattleDoors`：置活 `endBattleDoor`、`BattleHud.ToggleWithAnimation(false)`、`SetupDoor(...)`、`TouchInputManager.Toggle(false)`、`WaitForSeconds(视频长)`、最后 `matchFinishedAndWaitingToLeave=1` | 上一步 | 协程，等视频播完 | ✅ `Battle/BattleDoors.cs` | `BattleManager._CloseBattleDoors_d__393__MoveNext.c:22-72` |
| 结算视频/开门片 | `SetupDoor` 按 winner 三选一（`victoryVideo`/`defeatVideo`/`drawVideo` + 对应 cue）；建 `CustomRenderTexture 1920×1080` 挂 `RawImage`+`VideoPlayer.targetTexture`；**返回 `clip.length + 常量`**；`SoundManager.Play2D(cue, MixerType.Jingles=3)`；`BattleMusicManager.FadeOut`；`WindowsManager.CloseAllWindows` | — | 视频播完才放行 | ✅ **已结案，⛔ 别再当待办** | `EndBattleDoors__SetupDoor.c:39-166` · `MonoBehaviour_5455.json` |
| 奖励面板 | `ShowRewards`：`SkullsHolder` 按 `skullsObtained` 逐个开 · `RewardsHolder` 逐个 `Instantiate(rewardHolderPrefab)` | 开门时 | 同步 | ⚠️ 只做了骷髅行 | `EndBattleDoors__ShowRewards.c:37-193` |
| **骷髅/评分（什么分）** | `BattleScoreManager.targetThresholds` = `MatchData.GetMilestones()`；本版**硬编码 3 档 `{20,10,0}`**，只对 `playMode ∈ {0,3,6,7,10,11,12,13,14}` 给 | 开局 `Initialize` | 开局取一次 | ✅ | `BattleManager__Initialize.c:30` · `MatchData__GetMilestones.c:17-88` · 我们 `DeckRules.cs:109` |
| 怎么算 | `HealthThresholdData.CheckThreshold(health)` = `!AlreadyAccomplished && health <= HealthThreshold`；`GetSkullCount()` = 数已达成；`GetEnemySkullCount(ownLife)` = 数 `HealthThreshold >= ownLife` | — | — | ✅ 我们 `DeckRules.SkullsFor(hp)` | `HealthThresholdData__CheckThreshold.c:5-8` · `BattleScoreManager__GetSkullCount.c:31` · `__GetEnemySkullCount.c:26-30` |
| 谁调它 | ① 开局 `Initialize()` 注册 `Signal<EnemyHealthChangeSignal>`；② 敌督军生命变化 → `CheckThresholds` → 跨档则 `GrantSkull(i)`；③ **每次喊骷髅**：`CardScript.CheckForSkullEarned`（只对 `cardType==10=Hero`）算 `GetEnemySkullCount`，比缓存 `+0x354` 涨了就 `BroadcastEarnSkull` | 生命变化 / 跨档 | 事件驱动，**只增不减** | ✅ 同口径（`BattleDriver.cs:15089-15109`） | `BattleScoreManager__Initialize.c:30-43` · `CardScript__CheckForSkullEarned.c:20-61` |
| 计数文案 | `UpdateMilestonesCount(i)` 写 `(i+1)`；初始化传 `-1` ⇒ **开局必是 "0"** | — | — | ✅ `_skullScore.SetText("x"+N)` | `BattleScoreUiManager__UpdateMilestonesCount.c:13-18` · `__SetMilestones.c:74-101` |
| 面板显隐初值 | 运行时 dump：`BattleDoors` **activeSelf=False**，其下 `Canvas`=True、`EndBattlePanel`=True（`Awake` 只关了 `armyRatingIcon`） | — | — | ✅ | `runtime_ui_dump_Battle_Arena_1.tsv:1147-1153` · `EndBattleDoors__Awake.c:7-11` |
| 结算后离开 | `_CompleteBattleFinished`：等 `resultSaved` → `LeaveBattleRoom(是否输)` → 置活 `+0x160 shareButton` | 结果存完 | 协程 | ⚠️ 部分 | `BattleManager._CompleteBattleFinished_d__389__MoveNext.c:41-63` |

---

## 表 B：战斗音响「时机 → cue」

⚠️ 口径更正：**原版没有「45 键 SFX 表」**。下文 37 行是解包资产 + `dump.cs` 字段名 + 反编译调用点逐条核出来的**现行总表**。
所有 cue 走 `SoundManager.Play2D(cue, MixerType)`（`MixerType{FX=0,Music=1,Voices=2,Jingles=3,BattleGroupLow/Med/High=4/5/6}`）。

### A 组 · `SoundAssetCollection`（28 条 · 挂在 `BattleManager.soundCollection`(`+0x80`)）

| 时机 | cue 名 | 出处 | 我们做了没 |
|---|---|---|---|
| 抽牌落手 | `DrawCardFromDeck` | `CardScript__DrawCard.c:49` | ✗ |
| 单位被治疗 | `MinionHeal` | `CardScript._Heal_d__468__MoveNext.c:146` | ✗ |
| 献祭/投喂吸血（紧接治疗之后**无条件**再播一条） | `Syphon` | 同上 `:152` | ✗ |
| 单位发起攻击（抬手） | `MinionInitiatingAttack` | `CardScript._PrepareAttack_d__353__MoveNext.c:83` | ✗ |
| 卡从手牌飞进战场 | `MoveCardIntoBattle` | `CardScript__StartMovingFromHand.c:43` · `_ResolveCardAutoSummoned…:104,122` · `_ResolvePlayCardFromHand…:231,253` | ✗ |
| 从手牌打出**单位**（`cardType==0`） | `PlayMinionFromHand` | `_ResolvePlayCardFromHand…:397` · `_ResolveCardAutoSummoned…:187` | ✗ |
| 潜行攻击命中 | `SneakAttack` | `CardScript__ReceiveSneakAttackAnim.c:33` | ✗ |
| 伤害结算（多目标/自毁） | `UnitDamage` | `_ResolveMultiDamage_d__456__MoveNext.c` · `_ResolveMultiDamageAndDestroySelf_d__457…` | ✗ |
| 技能条按下 / 触摸 | `AbilityBoxClick` | `ActiveSkillDisplay__ClickDown.c:60` · `__OnTouchDown.c:56` | ✗ |
| 技能条展开 | `AbilityBoxEnter` | `ActiveSkillDisplay__FadeIn.c:73` | ✗ |
| 技能条收起 | `AbilityBoxExit` | `ActiveSkillDisplay__FadeOut.c:61` | ✗ |
| 聊天气泡弹出 | `ChatShow` | `BattleManager__VoiceLinesPopUpShowedUp.c:16` | ✗ |
| 结束回合（置灰/不可点） | `EndTurnButtonInactive` | `BattleManager__EndTurnClick.c:171` | ✗ |
| 结束回合（可点、非阻塞） | `EndTurnButton` | `BattleManager__EndTurnClick.c:50` | ✗ |
| 回合计时归零 | `TimerExpired` | `ClockManager__EndCountdown.c:33` | ✗ |
| 回合开始 | `StartTurn` | `BattleManager._NextTurn_d__395__MoveNext.c:447` | ✗ |
| 换牌阶段开始 | `Mulligan` | `BattleManager._MulliganCountdown_d__347__MoveNext.c:31` | ✗ |
| 没行动可用（UI 置灰） | `OutOfActions` | `ClockManager__SetNoActionsUi.c:51` | ✗ |
| 播放主动技能（该卡无专用音时） | `DefaultActiveAbility` | `_ResolveActiveAbilityAnim_d__478__MoveNext.c:19` | ✗ |
| 不可阻挡（Stun 反制） | `ActivateUnstunnable` | `CardScript__Stun.c:153` | ✗ |
| 幸存者 | `ActivateSurvivor` | `CardScript__UseSurvivor.c:67` | ✗ |
| 恐惧 | `ActivateTerror` | `_ResolveAttack_d__438__MoveNext.c:955` | ✗ |
| **哨兵（Sentinel）** | `ActivateSentinel` | **全反编译 0 调用点** | ✗ |
| 自动攻击 | `ActivateAutoAttack` | `CardScript__ResolveActiveAbilityPlayed.c` | ✗ |
| 切换钮（换牌选中态） | `Toggle` | `BattleCardUI__SetAsMulliganSelected.c:99` | ✗ |
| 爆炸（Blast，两处分支） | `ActivateBlast` | `_ResolveAttack_d__438__MoveNext.c:1052,1109` | ✗ |
| 献祭触发 | `ActivateSacrifice` | `CardScript__TriggerSacrifice.c:37` | ✗ |
| 手牌消散 | `DissolveCardInHand` | `_ResolveDisappearFromHand_d__545__MoveNext.c:127` | ✗ |
| 预加载（**不是播**） | 上表 6 条 | `BattleCacheInitializer._Initialize_d__0__MoveNext.c` | — |

### B 组 · `BattleCardSounds`（9 条 · 挂在卡 prefab 上）

| 时机 | cue 名（字段） | 出处 | 我们做了没 |
|---|---|---|---|
| 从手牌变单位（召唤）**且稀有度==4** | `invokeCUELegendary` | `CardScript__CardToMinionAnim.c:28` → `BattleCardSounds__GetInvokeCue.c:5` | ✗ |
| 从手牌变单位（其它稀有度） | `invokeCUE` | 同上 | ✗ |
| 同上（瞬移版） | 同上两条 | `CardScript__CardToMinionAnimTeleport.c:29` | ✗ |
| 单位死亡 | `minionDeathCUE` | `CardScript._UnitDeath_d__446__MoveNext.c` | ✗ |
| 近战命中（小/中/大） | `meleeAttackHitSmall/Medium/Big` | `CardScript._AttackMeleeAnim_d__357__MoveNext.c:83` | ✗ |
| 回手 | `backToHand` | **只找到属性 getter，调用点没查到** | ✗ |
| 落地 | `landOnGround` | **同上** | ✗ |
| 手牌里新建卡 | `createCardInHand` | **同上** | ✗ |

### C 组 · 结算 / 其它

| 时机 | cue 名 | 出处 | 我们做了没 |
|---|---|---|---|
| 开门（胜/负/平，`MixerType.Jingles`） | `victoryCue` / `defeatAudioCue` / `drawAudioCue` | `EndBattleDoors__SetupDoor.c:113` | ✅ `MatchVictory/MatchDefeat/MatchDraw` |
| 结算 BGM 收尾 | 不是 cue：`AudioMixerSnapshot.TransitionTo` + `hasMatchEnded=1` | `BattleMusicManager__FinishMatch.c:5-8` | ⚠️ 部分 |
| 战斗 BGM 强度三档 | `battleLow/Medium/High` 快照，阈值 `midHealthChange`/`highHealthChange` | `dump.cs` `BattleMusicManager` | ⚠️ 部分 |
| 残骸体三时机×两阵营 | 6 条 | 我们 `Battle/RemnantSfx.cs` 文件头 + `工具/import_remnant_sfx.py` | ✅ |
| 超时开场 | `OvertimeStart` | 我们 `BattleDriver.cs:14240` | ✅ |
| VFX 挂点音 | `AnimFXController.PlaySound` / `PlaySoundOnTime.sound` | `AnimFXController__PlaySound.c` · `PlaySoundOnTime__Update.c` | ✅ `Battle/AnimFXController.cs` |
| 卡 VO / 聊天 VO | `VoiceLinesController` 那条线 | 另档已有文档 | ⚠️ 部分 |
| `SoundCollection`（旧的 `AudioClip` 表） | **`decomp_full` 里 0 引用** —— 战斗只用 `SoundAssetCollection` | `dump.cs` `SoundCollection` | — |

---

## 没查清的

1. **`DAT_1834b2bb4 / bb8 / bbc` 三个常量的数值** —— `BasicBattleEndSequence` 的三处 `WaitForSeconds` 与 `SetupDoor` 的 `length + 常数` 用的就是它们。**不是字符串，是 float** ⇒ 已搜 `stringliteral.json` 按地址反查，**查不到**。
2. **掉线两路的精确条件/秒数**（`_ForfeitDisconnectedEnemy`、`FailedToReconnectAfterDisconnect`、`CancelMatchWithVictory`）—— 只确认它们都落到 `DeadHero`。
3. **`EnemyHealthChangeSignal` 的 `Raise` 点** —— 全 `decomp_full` **零命中**（原版用 `Signal.Register<T>/Raise<T>` + 类型以 `Il2CppClass*` 常量传入）。只能按 `dump.cs` 的方法签名认定注册方。
4. **`DeadHero` 里 `GetEnemySkullCount` 那一路的实参**：反编译器给的是未定值的 union 槽 ⇒ **不能断言它读哪一边的生命**。
5. **`BattleCardSounds` 的 `backToHand` / `landOnGround` / `createCardInHand` 调用点** —— 全仓只找到 getter。
6. **`ActivateSentinel` 的触发点** —— 0 命中。
7. **`EndBattlePanel` 类** —— `decomp_full` + `dump.cs` 均零命中（它只是节点名）；`EndBattleDoors` **组件挂在哪个节点没有查到**。
8. **`③ 在服务器、本地无判据`**：奖励/XP/评分段 —— `EndMatchResult{XP,NewRating,EarnedRating,EarnedSeals,…}` 全部来自 PlayFab/CloudScript，本地只有字段表、**没有取值规则**。
9. 我们 `EndPanel.cs:198` 写的「原版有 `Battle/BattleEnd/{Victory,Defeat,Draw}` 三条本地化键」—— 用 `stringliteral.json` **没查到**，**未复核**。

---

## 顺手发现（⛔ 未改，报给主对话）

1. 🔴 **「45 键 SFX」是 Godot 时代的账**（`d:/2/战斗缺口清单_0827.md:30` 明写 `SFX.gd` 45 键），被 `战斗复刻_正本.md` §八 #7 当成原版口径引用了。原版真身是 `SoundAssetCollection`(28) + `BattleCardSounds`(9)。**建议订正 §八 #7 那一行的措辞。**
2. `decomp_full/BattleManager__get_SoundCollection.c` 的文件名与内容**错配** —— 内容是 `ActiveSkillDisplay__get_parentCard`。拿它当 SoundCollection getter 会读错。
3. `BattleManager.PlaySkullsAnim` **是空实现**：方法体只有 `CustomDebug.LogWarning("[WARNING] Implement")`。它被 `_StartBattlePhase` 调用 ⇒ **原版的「骷髅飞入动画」根本没做**，别再照它复刻。
4. 运行时 dump 里 `BattleSettingsPanel/Match Skulls` 的 `activeSelf=False`（与已知「丢行」那条同族），而它下面两个子件是 True —— **不是新问题**。
5. `MonoBehaviour_4569.json`（BattleManager）里有个序列化值 **`ownZCards = 25.0`**、以及 `shareButton: {m_PathID: 0}`（**原版这一格是空的**）—— 我们那边 `+0x160` 在 `CompleteBattleFinished` 里被置活，值得记一笔。
6. `EndBattleDoors` 还有半屏我们没做：`ratingText` / `ratingIcon` / `armyRatingIcon` / `sealIconSprite` / `ratingObj` / `ratingLoseMaterial`（输局换材质）—— 单机版用不上的结论**没有原版判据**，属我们自定的裁剪。
7. `BattleScoreManager.CheckThresholds` 用 `<=`、`GetEnemySkullCount` 用 `>=` —— 边界（生命正好 == 20）两条**等价**，但如果将来只改一处会出现 `SkullsFor(20)` 差 1 的口径漂移。
