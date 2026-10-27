# B9 · R2T3B2 —— 战斗界面_HUD 账 B 行（前半 55 条）逐条核账报告

日期：2026-10-27 · 代理：R2T3B2（只读 + 落账型）
覆盖：`账/战斗界面_HUD.tsv` 中 **状态 = B 的 111 行里按文件顺序的前 55 行**（主表文件行 **92 – 744**）。
边界（按方法名钉，勿按行号）：**我的最后一条 = `CardScript::ActivateCard(bool activateY)`**；**后半由另一路代理从 `CardScript::CancelActiveAbility()` 起**。
产出：
- 逐条判据表 → `判据/B行逐条核_HUD_前半.tsv`（8 列，55 行）
- 账补丁 → `账/_parts/B9_R2T3B2.tsv`（11 列表头逐字复制；`状态` 保持 B；`等级` 原值；`轮次`=B9_R2T3B2；`方法` 全带 `(...)`）
- `check_patch_conflicts.py` 已跑：**✅ 没有跨补丁重复键（3 份补丁）**

---

## ① 核了多少条 / 建议分布

| 建议等级 | 条数 |
|---|---|
| **A（已实现且与原版一致）** | **2**（#2 `ManaBuff.DeepCopy` · #32 `BattleManager.IsTutorialOrCampaignMatch`） |
| **D（原版死方法，建议回 D）** | **1**（#3 `ManaBuff.GetNewInstanceOfThisEffect`） |
| **落点失效（行为可能等价但落点指错/无落点）** | **6**（#6 · #8 · #11 · #20 · #31 · #36） |
| **判据缺（原版方法体两份产物都读不出）** | **1**（#35 `ResolveDisappearUnit`） |
| **B（有已知偏离 / 未闭合）** | **45** |

方法：55 行全部逐条过；**约 50 行**做了「原版方法体（`decomp_full/*.c` 或 `out_cpp2il_dev_ilspy/*.cs`）vs 我方代码」现读核对（清单见附录）。**凡证据只引 `.c` 前几十行的，一律重读了全文**——本批抓到 7 处判据要点只读了前半/读错了对象（见 §②）。

**判 A 的两条**
- **#2 `ManaBuff.DeepCopy()`**：原版 = `MemberwiseClone()` + 类型检查（ILSpy `ManaBuff.cs:48-65`），且被**内联**进活的 `AbilityLogic.PlayAbility`（cpp2il `AbilityLogic.cs:275` 的 `((object)manaBuff).MemberwiseClone()`）⇒ 可达。我们 `EffectOp.Clone()` = `(EffectOp)MemberwiseClone()`（`RuleEngine/Core/EffectText.cs:857-860`），同一机制。
  ⚠️ **判据出处必须改**：`decomp_full/ManaBuff__DeepCopy.c` 内容是 **delegate `Clone`**（错内容），正确出处 = ILSpy。
- **#32 `BattleManager.IsTutorialOrCampaignMatch()`**：原版真值集 = `{100 Tutorial, 130 Campaign, 140 TutorialReplay, 190 Scene}`（ILSpy `:42844-42872`）；我们 `IsTutorialMatch ∪ IsCampaignLike` = `{100,140} ∪ {130,190}`（`BattleDriver.cs:10102-10105`），调用点传 `Ctx.MatchType`（`:10083`）。落点行号账里写 `:10004`，现读 `:10102`。

**建议回 D 的一条**
- **#3 `ManaBuff.GetNewInstanceOfThisEffect()`**：`grep -rl ManaBuff__GetNewInstanceOfThisEffect` **只命中它自己**；`AbilityLogic.PlayAbility` 里内联的是 `CardEffect__` / `HandEffect__` 的**同名方法（不同类）**，不是 ManaBuff 这份 ⇒ 全树 0 调用者、无内联证据 ⇒ 按《方案》§4.1「死代码 ⇒ D」应回 D。

---

## ② 最有价值 10 条（带文件:行号）

1. **#22 `BattleManager.CompleteBattleFinished` —— 0.25 s 常量读出来了，且原账漏了两道闸。**
   `0x1834b2e7c` 用 `read_literal.py` 现读 = **0.25f**（4B，确定；8B 是噪声）。协程真身（`_CompleteBattleFinished_d__389__MoveNext.c:20-80`）：
   ① 2.0 s 等待**只在 `matchResultData+0x24 == 2` 时**（不是无条件）；
   ② 之后 `if (matchResultData+0x1c == 0) return` —— **整条协程结束，不显示面板、不离房**；
   ③ 真才再等 **0.25 s** ⇒ `battleStage = 8` ⇒ `LeaveBattleRoom(bVar6)`（`bVar6 = +0x28==0x14 && +0x24==2`）⇒ 激活 `+0x160`。
   我们 `BattleDriver.cs:31653-31681`：**无条件 2.0 s、无 `+0x1c` 闸、无 0.25 s**，`LeaveNetRoom()` 也不带 bVar6。

2. **#52 `CardDamageCounterController.DamageReceived(int)` —— 原版不累加。**
   `damageCounterParent.SetActive(true)` ⇒ `damageText.text = damageValue.ToString()` ⇒ `myAnimation.Play()`（ILSpy `CardDamageCounterController.cs:12-19`；`.c:9` 的 `Int32__ToString(local_res10)` 同证）。原账「原版是累计计数，我们每次飘一条」**两半都不成立**；#53 `Toggle(bool)` 就一行 `SetActive(option)`（ILSpy `:21-24`），我们无此开关。

3. **#31 `BattleManager.GetUnitSizeRatio(bool)` —— 落点指错。**
   原版 = 我方 **1f** / 敌方 `globalVars.enemyUnitsScaleRatio`（ILSpy `:45540-45548`，后者=0.86）。落点 `ArenaSlots.CardScale` 是另一件（MinionArea 序列化 `desiredScale` 0.36/0.69），**0.69/0.36 = 1.9167 ≠ 1/0.86 = 1.1628** ⇒ 比例并没有「折进」CardScale。

4. **#20 `BattleManager.CloseBattleDoors(MatchResultData)` —— 落点只是开门视频。**
   原版协程 = SetActive(门) ⇒ `BattleHud.ToggleWithAnimation(false)` ⇒ `TouchInputManager.Toggle(false)` ⇒ `WaitForSeconds(门长)` ⇒ `matchFinishedAndWaitingToLeave = true`（`_CloseBattleDoors_d__393__MoveNext.c:26-52`）。`BattleDoors.cs:434 Play` 只做视频播放；HUD 收在 `BattleDriver.cs:31508` 一带、触摸闸在 `TouchInputManager`。

5. **#11 `BattleManager.BasicBattleEndSequence` —— 落点只盖尾段（六段已解）。**
   `_BasicBattleEndSequence_d__391__MoveNext.c:35-173`：等 **1.0**（`0x1834b2bb8`）⇒ 藏两颗 GO ⇒ 己方逐单位 `DestroyUnitsAfterBattleEnd` ⇒ 等 `(count+1)*globalVars+0x8c + +0x34` ⇒ 对方同 ⇒ 等 **0.5**（`0x1834b2bb4`，read_literal 现读）⇒ 起 `CompleteBattleFinished` 协程。我们 `ShowEndPanelAndFreezeInput` 只对应末段。

6. **#6 / #8 `ActiveWarlordAbility.GetParentCard()` / `Start()` —— 落点指错。**
   原版 = 返回/缓存组件父卡（ILSpy `ActiveWarlordAbility.cs:79-82`；`.c` 的 `GetParentCard.c` 内容是 **`FriendInfo.get_Room`**，错内容）。我们等价物是 `BattleDriver.cs:34518/34526/34539` 的 `WarlordSetupActiveSkill(side,slot)` / `WarlordTryUseActiveAbility(side,slot)`（按棋盘反查卡），**不是** `CardInteraction.BeginPressCandidate`（手牌按下候选逻辑）。

7. **#0 `BattleCardManager.CheckToBeAlternate(...)` —— 判据「走完 ⇒ null」错。**
   `.c:38-41` / ILSpy `BattleCardManager.cs:193`：扫完 `shuffledCardLibrary` **没命中就 `return rawCardData`（入参那张）**，`CreateCard` 正是用它决定「不换立绘」。数据缺口（card→altArt[] 未进卡表）仍在。

8. **#54 `CardScript.ActivateCard(bool)` —— 强制 false 的档位集合读错。**
   `.c:52` 的条件是 `((state − 4) & 0xfffffff9) == 0` ⇒ **state ∈ {4,5,12,13}**（waitingToBePlayed / waitingToDie / inHandJustCreated / inMulliganSelected），原账写的 `{4,5,6,7,10,11} 且非 8` 与现读不符。另外 ILSpy `CardScript.cs:7950-8060` 还有 `ephemeral ⇒ ToggleGlitch(true)` + `ShowEphemeral()` 两句，原账漏了。④⑤（Codex/Oath）归 D 的结论成立（`trait_collection.json` 的 customObject 空引用）。

9. **#27 `BattleManager.GetCurrentTutorialStage()` —— 漏了战役支。**
   `.c:20-60`：`{100,140}` ⇒ `currentTutorialStage`；**`{130,190}` ⇒ `currentCampaignStage`**；其余 null。我们落点 `Ctx.Tutorial` 只有教程支，`Ctx.CampaignStage` **没有任何写点**（`RuleCore.cs:1220/2799` 自承）⇒ 战役支恒 null。

10. **#42 `BattleManager.StartBattleManager()` —— 两件真缺。**
    `.c:28-110`：SetupDataDependentElements ⇒ BattleHud.Setup ⇒ **Time.timeScale = 1.0** ⇒ **`DOTween.Init` + `SetTweensCapacity(200,10)`** ⇒ PlayerData 标志 ⇒ 藏 GO(+0x238) ⇒ `ChatGlobalManager.SetOnlineStatus` ⇒ SetupBoardPhase ⇒ `TargetReticleController.Initialize` ⇒ new AvailableActionsPool。
    全仓 `Time.timeScale` 只有两处**注释**、无赋值；`DOTween.Init` / `SetTweensCapacity` **0 命中** ⇒ 这两件真缺（其余需逐件核）。

**另外三条判据订正（原账读错对象/只读前半）**
- **#25 `FinishedLeavingBattle`**：原版 **`battleStage == PostBattle` ⇒ 只把 UIstate 设 postBattle**；否则才 `LeaveBattle()`（ILSpy `:16642-16660`）。
- **#21 `ClosePopupsAfterBattleEnd`**：原版 = `isAnyPopUpOpen=false` ⇒ **语音弹窗 ForcePopUpClose** ⇒ 连接状态复位 ⇒ `activeSkillDesc.SetActive(false)`；**不是「其余弹窗逐颗 SetActive(false)」**（ILSpy `:19489-19500`）。
- **#34 `OnInitializationComplete`**：原版 = Missions/Achievements/Alliances **BlockAutoUpdates/Upload** ⇒ 停音乐 ⇒ battleMusic.Init ⇒ `StartBattleSequence` 协程；不是「启 StartBattleManager」（ILSpy `:15442-15460`）。

---

## ③ 没查清的部分（如实列，不猜）

| 行 | 差什么 |
|---|---|
| #35 `ResolveDisappearUnit` | **判据缺**：`decomp_full/*.c` 内容是 BattleAction 结构体拷贝（anonymous-type 模式），ILSpy `:27356-27450` 满是 unmanaged-load 失真 ⇒ 现有「残骸/溶解两分支」是从别处推的；需 VA 反汇编或 MoveNext 才能定 |
| #51 `Update` | 1416 行 / 13 条分支，账的判据只有一句「吃输入、推进动作与演出」⇒ 过粗；未逐条读 |
| #1 `CreateCard` | 只读到 `.c:20-90`；③④⑤（卡背 / 埋伏面朝下 / 后续 CardSetup）未逐句核 |
| #23 `DestroyUnitsAfterBattleEnd` | hero 特例与 `skipDeathSound` 的等待次序只 skim 了 `.c:20-180`，未逐句 |
| #25/#47/#48 | 离场/触摸恢复/淡出复位的**次序与时长**未核 |
| #10 `AddNextReplayAction` | 判据要点已订正为「回放推进协程」，但我们自己回放契约怎么对上未核 |
| #36 `ResolveDisplayFromHand` | 真身在 `_d__548__MoveNext`（可读），但我们的展示链映射未核 |
| #44 `StartMovingCardInHand` | 「跟手」不在本方法（在 Update/track），我们哪一段接上未核 |

---

## ④ 顺手发现 + 已完成到哪 / 下一批从哪继续

### 顺手发现（本批最硬的一条）
- 🔴 **本域 `.c`「文件名与内容不符」一次抓到 7 个**：
  `BattleManager__AddNextReplayAction.c` · `__BasicBattleEndSequence.c` · `__ResolveDisplayFromHand.c` · `__ResolveDisappearUnit.c`（内容都是 `<>f__AnonymousType7<Int32Enum>___ctor` 结构体拷贝）· `BattleManager__GetMainCamera.c`（内容是 `BasicCardUI.get_HealthCountersController`）· `ActiveWarlordAbility__GetParentCard.c`（内容是 `FriendInfo.get_Room`）· `ManaBuff__DeepCopy.c`（内容是 delegate `Clone`）。
  建议并入 `资料/普查产出_第八十会话/G4_文件名与内容不符_全量.tsv`，并在任何「判据出处 = decomp_full」的行上先跑「首行函数名 == 自己」检查。
- **read_literal 现读常量**（本批用到）：`0x1834b2e7c` = **0.25f** · `0x1834b2bb4` = **0.5f** · `0x1834b2bbc` = **2.0f** · `0x1834b2bb8` = **1.0f**。
- **落点行号漂移**（同步点可一并 `--fix`）：`IsTutorialOrCampaignMatch` 10004→10102 · `ShowEndPanelAndFreezeInput` 31214→31693 · `WarlordTryUseActiveAbility` 34352→34539 · `TutorialAllowsCardDisplay` 属性现在 17045。
- 本批 55 行与主表现表**逐字一致**（快照 00:39 起、生成前复核 55/55 同）。

### 已完成到哪 / 下一批
- 本批覆盖主表 B 行第 **1–55** 条（文件行 92–744），最后一条 = `CardScript::ActivateCard(bool activateY)`。
- **下一批（另一路代理）从第 56 条 `CardScript::CancelActiveAbility()` 起**；`判据/B行逐条核_HUD_后半.tsv` + `_parts/B9_*.tsv` 各写各的。
- 交调度台：
  1. 合并 `B9_R2T3B2`（先 `check_patch_conflicts.py`，已跑：无冲突）。
  2. 按 §② 第 1–10 条立账/派修：#22（0.25 s + 两道闸）· #52（判据订正）· #31/#20/#11/#6/#8（落点改指）· #0/#54/#27/#42（判据订正 + 真缺）· #2 升 A（判据出处改 ILSpy）· #32 升 A（落点行号）· #3 回 D。
  3. 7 个错内容 `.c` 并入 G4 清单。
- 本批**未**改 `Assets/**/*.cs`、未跑 Unity、未动 git、未改主表与两张正本（只新增了判据表 + `_parts` 补丁 + 本报告）。

---

## 附录：本轮做了「原版方法体 vs 我方代码」现读核对的行
0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 52, 53, 54（#51 只 skim）
