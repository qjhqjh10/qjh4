# B9 · R2T3B —— 战斗逻辑驱动 账 B 行（后半 211 条）逐条核账报告

日期：2026-10-27 · 代理：R2T3B（只读 + 落账型）
覆盖：`账/战斗逻辑驱动.tsv` 中 **状态 = B 的 422 行里按文件顺序的后一半**（主表文件行 **636 – 1376**，共 **211 行**）。前一半归 R2T3A。
产出：
- 逐条判据表 → `判据/B行逐条核_R2_后半.tsv`
- 账补丁 → `账/_parts/B9_R2T3B.tsv`（11 列表头逐字复制主表；`等级` 保持原值；只改 `差异`/`备注`/`轮次`；`状态` 仍为 B）

---

## ① 核了多少条 / 建议分布

| 建议等级 | 条数 | 说明 |
|---|---|---|
| **A（已实现且与原版一致）** | **1** | #43 `CardScript.GetHighestAttackValue` |
| **B（有已知偏离 / 未闭合）** | **207** | 其中 5 条为「原账写净结果同、实际有偏离」（见 §②） |
| **建议回 D（原版死代码）** | **3** | #202 / #203 / #204（TurnExtensions 三个方法）——与 J12DK 的 D→B 回退冲突，请调度台裁定 |

**覆盖口径（诚实说明）**
- 211 行全部过了一遍：`判据要点` / `差异` / `我们的落点` / `备注`，并做落点符号存在性校验。
- **约 55 行**做了「原版方法体（`decomp_full/*.c` 或 `out_cpp2il_dev_ilspy/*.cs`）**逐句** vs 我方代码」的现读核对（清单见本报告附录）；其余行按原账的逐句证据 + 关键数值现读（如 `cards_engine.json` 的 trigger 可达性）抽查判定。
- 落点结构校验：211 行里 **74 个落点段落在 ±5 行容差内、109 段行号漂移 >5 行（其中 89 行的 Δ>10）、47 段无法按声明名解析**（描述性落点 / 相对路径 / 字段引用）。这是**行号漂移**，不是内容失效；建议重同步点统一 `check_ledger_points --all-A --fix`（本次未跑）。

**判 A 的唯一一条**
- **#43 `CardScript::GetHighestAttackValue()`**：原版 = `max(CurrentMeleeAttack, CurrentRangeAttack)`（`CardScript__GetHighestAttackValue.c:6-14`）；其**唯一调用者**是 `AbilityLogic.GetTargets` 的排序 lambda（`AbilityLogic.__c__<GetTargets>b__26_1.c:6`）。我方 `CountEval.HighestAttackKey`（`RuleEngine/Core/CountEval.cs:997-1004`）逐字同式、正是 `targetSelectMethod == 1` 的排序键。原账落点指的是 `AiTargets.HighestAttack`（`RuleEngine/Data/AiTargets.cs:80-86`）——那是**同式第二份**（AI 排序用），建议落点改指 `CountEval.cs:997`。

---

## ② 最有价值 10 条（带文件:行号）

1. **#68 `CardScript::RemoveAttackThisTurn()` —— 原账「只剩观测缺口」不成立，是真偏离。**
   原版 `BattleManager__AddForceAttack.c:43-46`：对 `dontCountAttack` 三族（chosenAbility 231/237/238）**入队时**先记 `wasFree = (attacksThisTurn == 0)`，再**无条件**调 `RemoveAttackThisTurn`（`>0` 才 -1）；真打之后只有 `wasFree` 才排 `ResetActions(-1)`。
   我方 `RuleCore.DeclareAttackRefundingForced`（`RuleEngine/Core/RuleCore.cs:14408-14424`）**不在入队预扣**，只在「打之前 `AttacksThisTurn == 0`」时打后 -1。
   ⇒ **已攻击过的单位被强制攻击**这一格：原版 `1→0→(打+1)→1`（净不变），我方 `1→(打+1)→2`（多 1）。影响 `ActivateBloodThirst`（判 `==1`）、配额查询等。

2. **#131 `MatchData::SetMatchRecording(...)` —— 判据要点整条读错。**
   原版签名是 `SetMatchRecording(BattleRecordData matchRecording, PlayerDataManager playerData)`（`out_cpp2il_dev_ilspy/MatchData.cs:577`；`decomp_full/MatchData__SetMatchRecording.c` 全文），方法体是**把 BattleRecordData 灌进 MatchData**（rngNum / oppId / 双方 `DeckAndWarlordData` / playerLog / enemyLog / 双方环境卡 / 双方 mulligan / type / gameMode+liveops）⇒ 是**回放装载**，不是「本局要不要录」。我方 ReplayStore 走「起始条件 + 动作流」另一套契约。

3. **#134 `MatchData::ShoultSetAiOpponent(PlayModes)` —— 「净结果同」不成立。**
   原版 `MatchData__ShoultSetAiOpponent.c:8-20`：`mode ∈ [3,12]` 除 `7/10/11` ⇒ AI（Dungeon/Tutorial/CutScene/OfflinePractice/Campaign/TutorialReplay/OwnDeckTraining）；其余（含 Classic 0 / Duel 1 / PracticeLodge 2 / ClosedDeck 7 / RankedFriendly 11）**非 AI**。
   我方 = `AiShouldDriveOpponent = _net == null`（`CardPresentation/Battle/BattleDriver.cs:246`）⇒ **任何单机局都判 AI** ⇒ 上述原版非 AI 的档，单机开局会由 AI 驾驶对手。

4. **#152 `PlayerHand::CompleteMulliganPhase()` —— 判据要点只读了前半。**
   `PlayerHand__CompleteMulliganPhase.c:22-72`：不只「清 `currentMulligan`」——它先逐张把换牌区牌**送回牌库**，并逐张 `SetStateInHand` / `ToggleOverrideSorting(1)` / `SetSortingLayer(Hand)` / `FinishMulliganUi` / `ToggleTooltips(false)`，然后才重建空列表 + `StartCoroutine(FinishMulliganFinalPhase)`。我方是 `RuleCore.Mulligan`（弃回→洗→抽）+ `EndMulligan`（关阶段+防御卡）两段拆开、逐卡 UI 在表现层。

5. **#27 `CardScript::CancelAttack(bool)` —— 漏一道闸。**
   `CardScript__CancelAttack.c:37-40`：`if (playerInitiated && *(byte*)(this + 0x350) == 0) { LogWarning; return; }`（发起位与 playerInitiated 必须一致，否则不 `PrepareAttack`）。我方无 `+0x350` 位、也无此闸。

6. **#130 `MatchData::SetBotOpponentDeck(...)` —— 洗牌闸漏了 TutorialReplay。**
   `MatchData__SetBotOpponentDeck.c:20-26`：建好 `DeckAndWarlordData` 后**立刻 `ShuffleCardLibrary`**，唯一例外 `matchType ∈ {100 Tutorial, 140 TutorialReplay}`。我方是 `NewBattle(shuffle:)` 单一开关（玩家/对手共用），`RuleEngine/Core/RuleCore.cs:188` **只挡 Tutorial(100)、不挡 TutorialReplay(140)**。

7. **#164 `PlayerHand::SetMulliganCardsToDiscard(int[])` —— 校验与置态都在本方法里。**
   原版 `.c:32-37` **自己就做整批校验**（`CheckIfMulliganCardsCorrect` 不过 ⇒ LogError + 整批拒绝），随后逐张按「该卡 id 是否在入参数组」调 `SetStateAsMulligan`（13/14）；`…Start` 同样先校验再逐张 `ClickMulliganSelected`。原账「只记录、校验在 Start」不成立。

8. **#43 `CardScript::GetHighestAttackValue()` → 建议升 A**（见 §①）：真等价落点 `CountEval.cs:997`；`AiTargets.cs:80-86` 是同式第二份。

9. **#202/#203/#204 `TurnExtensions` 三个方法 → 建议回 D（死类）**：
   - `TurnExtensions` 全树只被 `PunTurnManager.cs` 调用（`out_cpp2il_dev_ilspy` 全扫）；
   - `PunTurnManager` 只被自身与 `IPunTurnManagerCallbacks` 引用，该接口**全树 0 实现者**；
   - AssetRipper 导出工程（`d:/2/tools/assetripper_out/ExportedProject`）只含这两个 `.cs`，**无 prefab/scene 引用**；`assets_full` 仅命中 `MonoScript_540`。
   ⇒ 按《方案》§4.1「死类一律 D、不许判 A」，J12DK 2026-10-26 的 **D→B 回退**请复核。

10. **#4 `BroadcastTurnStart` 的观测量数字是错的。**
    `cards_engine.json` 的 `abilityTriggers` 现读：**30 → 5 张**（GOF86/SOR66/TAU6/TAU7/TAU13）· **31 → 8 张**（BL35/GSC5/GOF80/TAU3/TL8/TL37/UM88/9218576f…）。原备注「18 处 / 10 处（cards_truth 现读）」不成立——`cards_truth.json` **没有 `abilityTriggers` 列**；全字段 occurrences = 21/34。「没有一张两个都有」仍成立（交集 0）。

**另外确认到的两条真偏离（原账已写、本轮现核成立）**
- #33 `CheckAndApplyCurrentPack`：原版部队取 `unitList.Count − 1`（`CardScript__CheckAndApplyCurrentPack.c:29`），我方 `FieldAttack`（`RuleCore.cs:4986-5012`）数**含自己**的全部非督军 ⇒ 带 pack 的部队**多 +1**。
- #49 `HasPackBuffTrait`：原版在 `activeEffects(+0x108)` 里 `Find` 一条效果（`.c:26-41`），我方查 `u.Has("pack")` 关键词 ⇒ 未挂 buff 时也判真。

---

## ③ 没查清的部分（如实列，不猜）

| 行 | 差什么 |
|---|---|
| #155 `PlayerHand::GetWhispersCardsCount()` | 反编译产物文件名与内容不符（内容是 `<>f__AnonymousType31__GetHashCode`）；whispers 到底数什么**判据缺** |
| #205 `TurnScriptedData::UpdateName` | 字符串拼法反编译读不全（`num6 = default` 等失真）⇒ 不能逐字等价 |
| #42 `CardScript::GetAdjustedDamage` | 伪码把 dropPod/dodge/invulnerable 段反成死存（算了没用），真分支读不到 |
| #181 `RawCardScript::GetTransformedKnightId` | 50 行只读了前段，`+0x208` 映射表的匹配规则未核 |
| #185 / #192 / #193 | 120 / 272 / 116 行的大方法只读了前段或签名，不能声称等价 |
| #150 `MinionManager::RemoveSwarm` | 引擎属性回滚落在哪条路**没找到**（不在本方法/`RemoveSwarm` 内） |
| #209 `BroadcastUnitUsedSacrifice` | 手牌两趟是否逐趟排除发起者、自排除口径（原版在调用方）**未核** |
| #145 `GetNextSlotToCard` | 「贴着这张卡（IndexOf+2）」两半，我方 `summonPos` 是否实现**未核** |
| #143 `GetMinionList` | 排序键是缓存委托，本份 `.c` 读不出 key 字段 |
| #18 `GetManaLeft` | `unusedMana`/`savedMana` 回合切换两步次序未逐句核 |
| #124 `HasEnchantmentWhileInPlay` | 数据列 `whileInPlayAnim` 未进卡表 ⇒ 今天对所有附魔返回 false（数据缺口） |
| #210 `get_displaySummonSickness` | 光环 `AddAuraKeyword` 重挂路是否重算两位，原版判据未闭合 |
| 37 条「J12DK 由 D 改 B」的资产/本地化查询类 | 108/109/111/114/120/136/141/147/149/153/156/158/170/172-178/189：原账只写「等价，未逐方法核」⇒ 本轮未逐条核，仍 B |

---

## ④ 顺手发现 + 已完成到哪 / 下一批从哪继续

### 顺手发现
1. **落点行号大面积漂移**（89 行 Δ>10）：`RuleCore.cs` 普遍 +60~+70、`EffectResolver.cs` +40~+200、`UnitState.cs` +8~+37、`MulliganPanel.cs` +86、`CardInteraction.cs` +48、`CardFeel.cs` +52；个别为负（如 `EffectResolver.cs` 的 `DoCreate` 支 −66~−75，因为原账写的是**调用支**而不是声明行）。**建议在重同步点（无 `.cs` 写手时）跑 `check_ledger_points --all-A --fix`。** 本代理未跑（主表与本批补丁分开）。
2. **#43 同一条公式两份实现**（`AiTargets.HighestAttack` / `CountEval.HighestAttackKey`）——「两处写同一条规则 = 迟早不一致」，建议收口或两处互指。
3. **#202 原备注自相矛盾**：备注两处写「保持 B」、状态曾是 D；本轮给出 D 的独立证据，建议调 度台裁。
4. **判据要点普遍只读方法前半段**：本轮抓到 5 例（#152/#164/#131/#27/#130）。筛行时发现：凡「判据要点 ≤ 60 字而原版方法 > 60 行」的行，二次核的命中率很高，建议后续优先抽查这一类。
5. **`cards_truth.json` 没有 `abilityTriggers` 列**（#4 的错数根因）；统计 trigger 可达性一律用 `cards_engine.json`。
6. **#31 的落点描述已过期**：`DoDouble` 自 2026-10-25 E1 起落 `AddStatMod`（不写 base），原账「写新基础值」那一半不再成立；`ChangeBaseAttack` 的「先 RemoveAt 固有近战增益」仍无。

### 已完成到哪 / 下一批
- 主表 `战斗逻辑驱动.tsv` 的 **B 行到文件行 1376 结束**（最后一条即 `EntityScript::get_displaySummonSickness`）。本批覆盖 636–1376，加上 R2T3A 的前半（25–635），**该文件 B 行已全覆盖**，没有下一半。
- 建议下一步（交调度台）：
  1. 合并补丁：`merge_ledger_parts.py --only B9_R2T3B`（⚠️ 先跑 `check_patch_conflicts.py`；本批只有一份补丁，但仍按流程）。
     🔴 **合并前先比一次主表**：本补丁基于本会话开始时的快照（主表 mtime 2026-10-27 00:39）；本会话期间主表被其他写手改过（`git status` 显示 M）。核对结果：我的 211 行里 **210 行与现表逐字相同、1 行（`PlayerHand::GetWhispersCardsCount`）已有 R1T3A2 新修订** —— 该行**已按现表内容重写**后再落补丁，未覆盖新版本。合并时如再遇并发修订，请按方法名逐条比、别整批盖。
  2. 按 §② 第 1–7 条逐条立账/派修（#68 / #131 / #134 / #152 / #27 / #130 / #164）；#43 升 A 并改落点；#202-204 做 D/B 裁定。
  3. 重同步点跑 `check_ledger_points --all-A --fix` 修 §④·1 的行号漂移（需无 `.cs` 写手）。
  4. 本批**未**改任何 `Assets/**/*.cs`、未跑 Unity、未动 git、未改主表与两张正本。

---

## 附录：本轮做了「原版方法体 vs 我方代码」逐句/现读核对的行

0, 1, 2, 3, 4, 5, 6, 7, 12, 16, 18, 19, 21, 22, 27, 28, 30, 31, 33, 36, 39, 43, 44, 46, 47, 48, 49, 52, 54, 61, 68, 71, 80, 90, 100, 102, 103, 105, 106, 107, 119, 124, 127, 128, 129, 130, 131, 132, 133, 134, 140, 151, 152, 155, 163, 164, 165, 195, 202, 203, 204, 205, 208, 210
（约 64 行；其余行按原账证据 + 落点存在性 + 数值现读判定）
