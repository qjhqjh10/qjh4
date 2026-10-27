# B9_R1T3A2 — 手牌两件（1157/1170）与 B 段深挖

> 本轮执行型。目标 1 = 写 `CardPresentation/Hand/` 两条；目标 2 = 只读逐句核 R387/R388/R380。
> **结论先行：目标 1 的两条在盘上【已按施工单逐句实现并接线完毕】（前一轮 `E6P14` 2026-10-26 落的），本批逐句复核后确认等价，无需再改代码**；`TMPDIR=/tmp/wf_R1T3A bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**。目标 2 三条 116/120/272 行已逐句读完，结论见 `判据/B行逐条核_R1_第三段_补.tsv`。

## ① 结论

### 目标 1 —— 两条均已实现（施工单 1157 / 1170 那两行已过期）
- **1157 `DisplayHandMinusCard`**：起拖首帧「无洞 N-1 张」已在 `CardInteraction.cs:946-965`（`_insertIndex = -1; Relayout(false)`；`VisibleCards()` 已排除 `_dragging`）；下一帧起切回带洞（`UpdateDrag` 的 `_insertIndex = slot; Relayout(false)`，`CardInteraction.cs:1043`）。逐句见下。
- **1170 `HandCardMovedToNewIndex`**：三处早退 + SendCardReorder 只在真动时发 + `CheckForTutorialPointer` 已在 `CardInteraction.cs:1471-1489`，且**教程指针重算已接线**：`OnHandCardMovedToNewIndex` 事件（`CardInteraction.cs:2001`）→ `BattleDriver.cs:5631` 订阅 → `BattleDriver.cs:33941` `OnHandCardMovedToNewIndex(CardView){ CheckForTutorialPointer(); }`。所以**不需要动 `TutorialScript.cs`**（那侧被 `E1RNG` 占的约束不构成阻塞）。
- ⇒ 没有需要落的代码改动；施工单 1157/1170 建议标「已实现（E6P14 2026-10-26）/ 可核 A」。

### 目标 2 —— R387/R388/R380 逐句结论（仍保持 B，但判据已补齐）
见 `判据/B行逐条核_R1_第三段_补.tsv`（3 行）。要点：
- **R380 `OnCardPlayedWithTarget`**：触发扇出全貌 = `thisCard==cardPlayed` 时 trigger0 / 精神石广播 / 600 / 605(仅 cardType 20战术·40低语) / 690(同回合抽到)；无条件 trigger10(thisCard!=played) / 15(`CardsPlayedInTurn==3`) / 20(always)；另有 `+0x268` 下「有 target 位却无目标」LogWarning + 战术目标带 `0x4dd` ⇒ `OnTrigger(710)` + `AddTriggerCardPlayedByStimulation`。我们只覆盖一部分。
- **R387 `TriggerAbilities`**：**双列表**（标准 `cardAbilities(+0x290)` vs 附魔 `thisCard+0x110` 每条各 `+0x290`）+ `ability+0x40`（targeted）**逐目标各触发一次** + trigger240 不能触发时 `RemoveExtrinsicEffectsFrom` + chosenAbility 410 特例。
- **R388 `TriggerAbility`**：`SetNewData` 打包 / trigger20⇒+0x40、240⇒+0x41 / trigger600 且 thisCard==played ⇒ `UseMana` / trigger∉{20,240} ⇒ `PlayTriggerTraitAnim` / `LogActivatedTrigger` / **`ability+0x48` 为真且 logic>1 ⇒ 随机取 1 条，否则逐条 `PlayAbility`**。

## ② 证据（目标 1 逐句对，`.c` vs 我们的代码）

### 1157 `PlayerHand.DisplayHandMinusCard`（`decomp_full/PlayerHand._DisplayHandMinusCard_d__80__MoveNext.c`）
| 原版步骤（.c） | 我们（`CardInteraction.cs` / `HandLayout.cs`） | 判 |
|---|---|---|
| 起手置 `+0x61 = 1`（.c:33） | 无独立 isDoingAnim 标记（`⛔` 刻意不 ArmAnimGate，见 `:956-958` 注释：那条闸等价的是「关碰撞体」，起拖这 0.2 s 原版照旧能交互） | 等价（无消费方） |
| 遍历候选列表 | `VisibleCards()`（`_cards` 去掉 `_dragging`，`:1556-1561`） | ✓ |
| 跳过 `card == cardToExclude`（.c:65-74，Ghidra 吞第二实参） | `_dragging` 已在列表外 ⇒ 等价排除 | ✓ |
| 跳过 `cardState == 4`（.c:75） | **N/A**：我们引擎/表现层没有 `cardState` 字段（`CardHighlight.cs:345` 明写；`CardStateId` 无 4），打出的卡在 `:1410 _cards.Remove` 后离开手牌 ⇒ 手牌列表里不会有 waitingToBePlayed(4) | 等价（无该态） |
| `PositionCardInHand(card, iVar8++ 连续, total-1, stepTime, -1, 0)`（.c:83-84） | `Refresh(list, hoveredIndex=-1, insertIndex=-1)`：`hole=false ⇒ slots=n`，`slot=i` 连续（`HandLayout.cs:712-768`）；n = `_cards.Count-1` = 原版 `total-1` | ✓ |
| 摆完 `WaitForSeconds(stepTime)` 后 `+0x61=0`（.c:52-61 / 状态1） | 无（同上，无该标记） | 等价 |
| tween 时长/缓动 = `globalVars+0xa4`(0.2s)、不写 SetEase | `Place` 用 `CardTween.RelayoutDuration`(0.2s) + `Ease.OutQuad`（`HandLayout.cs:882`），判据段见 `:848-882` | ✓ |

### 1170 `PlayerHand.HandCardMovedToNewIndex`（`decomp_full/PlayerHand__HandCardMovedToNewIndex.c`）
| 原版步骤（.c） | 我们（`CardInteraction.cs:1471-1489`） | 判 |
|---|---|---|
| `idx = IndexOf(card)`（.c:16-18） | `int from = _cards.IndexOf(card)` | ✓ |
| `newIndex == idx` ⇒ 不动列表、只重排、返回 0（.c:19-24） | `to != from` 才改列表 ⇒ 相等时跳过、仍走末尾 `Relayout(true)` | ✓ |
| `Count <= newIndex` ⇒ 只重排、返回 0（.c:27） | `to < _cards.Count` 才改 ⇒ 越界跳过 | ✓ |
| 否则 `Remove+Insert` + 重排 + `CheckForTutorialPointer()`、返回 1（.c:28-38） | `RemoveAt/Insert` + `NotifyHandReordered`（= `SendCardReorder`，只在真动时发）+ `OnHandCardMovedToNewIndex`（→ `BattleDriver.CheckForTutorialPointer`） | ✓ |
| SendCardReorder 由 `BattleManager.Update` 按「返回1 且 IsNetworkedGame 且 connected」发 | `NotifyHandReordered` 判 `net!=null && net.InBattle` | 等价（网络细节） |

新增成员纪律：本批**未新增任何成员**（无改动）⇒ 无「追加类尾/行号漂移」问题。

## ③ 没查清 / 边界
- **1157 的 `+0x61`（isDoingAnim）与 `WaitForSeconds`**：我们不实现该标记 —— 因为它只服务「起拖 0.2 s 内是否算做动画」，而我们的输入闸是另一条（关碰撞体）。**没有任何消费者读它**，故无可见差；若将来有人要「起拖 0.2s 内算 isDoingAnim」需另开。
- **1170 的 `Count <= newIndex`**：我们的 `_insertIndex` 恒 `< _cards.Count`（由 `GetClosestInHandSlot` 产生），早退分支结构上到不了 ⇒ 与施工单「这一条主要卡③」一致（已覆盖）。
- **R380/R387/R388 的结论已够升「判据齐」，但仍 B**：因为它们是**结构级**差异（我们按 EffectOp 粒度、无 AbilityLogic 对象层、无双列表/逐目标 `+0x40`），不是补一行能等价，需要单独设计（不是本波范围）。

## ④ 改动清单 + 待接线
- **代码改动：无**（两条已实现、已接线、typecheck 0/0）。
- **本批新增文件**：`判据/B行逐条核_R1_第三段_补.tsv`（R380/R387/R388 逐句结论）。
- **待接线（不在本波、也不在白名单）**：
  - R387 双列表 + `+0x40` 逐目标触发 + 240 摘 extrinsic：落在 `ActionPump`/`EffectResolver`（引擎线写手）。
  - R380 的 trigger690/15/10/20 扇出 + 0x4dd 刺激支：落在 `EffectResolver.PlayTacticBodyDeferred`（引擎线）。
  - R388 ability 级「随机取一条 vs 逐条」：需引入 AbilityLogic 粒度的执行器（引擎线设计项，非一行）。
- **建议**：施工单 1157/1170 两行由调度台标「已实现/E6P14」并核 A；R380/R387/R388 继续 B，判据已在 `_补.tsv` 补齐。
