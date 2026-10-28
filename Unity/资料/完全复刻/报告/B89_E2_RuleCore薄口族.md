# B89-E2 · 批次 2「RuleCore 薄口族」（Chain R 第 1 棒）—— 执行报告

- 代理：`B89` 第 1 波执行代理 `E2` · 2026-10-28
- 授权文件（唯一写手）：`RuleEngine/Core/RuleCore.cs` · `RuleEngine/Core/BoardSlots.cs` · `RuleEngine/Core/BattleContext.cs`
- 账补丁：`账/_parts/B89_E2.tsv`（7 行 = 表头 + 6 条，11 列校验通过，LF，无裸 CR）
- 判据：`判据/B88_R9_主线68条分批.md` §「批次 2」+ 本节每条的 `.c` / ILSpy 现读

---

## §1 逐条结论

| 账行 | 原版判据（现读） | 改动（文件:符号名 + 现读行号） | 状态建议 | 未做 / 前提 |
|---|---|---|---|---|
| **L158** `AddResetDuty` | `BattleManager__AddResetDuty.c:1-101` = 造 `BattleAction(0x57=87 resetDuty)` + `AddAutoActionToQueue(bm, action, priority, 0)`；出队 `BattleManager__ResolveResetDuty.c` **全文 14 行** = `*(byte*)(target+0x4c)=0` → `CardScript.UpdateFigures` → `FinishResolvingAction(0x57)`。唯一调用点 `AbilityLogic__PlayAbility.c:1949`（chosenAbility `0x1db=475`） | **新增** `RuleCore.AddResetDuty`（`RuleCore.cs:15693`）+ `RuleCore.ResolveResetDuty`（`:15705`），追加在类尾 | **A** | ① `UpdateFigures` 是表现层那一跳（账 `L976` = `CardView.UpdateFigures`）⇒ 引擎侧无陈旧副本，出队体只清 `ActiveAbilityUsed` + `ctx.Log` 出声（可观察）。② **没调** `UnitState.RefreshForNewTurn` —— 施工判据那一句与 `.c` 冲突，按 `.c` 办（见 §3-b）。③ **今天没有调用方**：3 张 `chosenAbility 475` 卡（`Cadian Honour` / `Lead by Example` / `AM47 Press the Attack`）走既有文本路 `EffectResolver.DoReloadDuty`；切过来 = 一行，属 `EffectResolver` 写手 |
| **L319** `GetAdjacentUnitRight` | `BattleManager__GetAdjacentUnitRight.c`（全文）+ `MinionManager__GetAdjacentRightTroop.c`（全文）：左列表 `i>0 ⇒ left[i-1]`；`i==0` ⇒ 无右邻；右列表 `i+1<Count ⇒ right[i+1]`；两表都没有 ⇒ null | **新增** `BoardSlots.RightNeighbor(PlayerState,int)`（`BoardSlots.cs:620`）与 `(PlayerState,UnitState)`（`:632`）；**接线** `RuleCore.TrySwarmMerge` / `RuleCore.SwarmMergeNow` | **A** | 逐字等价（连续棋盘 = `Board[slot+1]`，但**跳过督军格**：左侧 3 号格下一个是 4 号督军格，原版那两条 List 不含督军 ⇒ null；改前会拿到督军、靠 `IsSwarmHost` 挡住，现池不可达） |
| **L365** `GetNextCardsInDeck` | `BattleManager__GetNextCardsInDeck.c:1-46` + ILSpy `:41091` —— 取该侧 `*ShuffledDeck`，`for i<numCards { if (i<Count) add(list[i]) }`；**无 cardState 过滤**、不移动 | **新增** `RuleCore.PeekNextCards`（`RuleCore.cs:15617`），镜像取 `Deck[Count-1-i]` | **A** | 现读无调用方（原版唯一调用点 = `AbilityLogic__AddDrawCard`；我们那条路的等价物仍是 `ctx.DrawnThisResolve`）；「只看不抽」池内 0 张可达 |
| **L428** `IsThereValidAbilityTarget` | `BattleManager__IsThereValidAbilityTarget.c:1-33` —— 扫 `unitsInPlay(+0x470)`（全场）逐张 `IsValidActiveAbilityTarget`，任一真即真 | **新增** `RuleCore.IsThereValidAbilityTarget`（`RuleCore.cs:15647`），逐目标转发 `RuleCore.CanUseAbilityTarget`（`:11367`） | **A** | 现读无调用方（我们「有没有目标」走 `EffectResolver.PlayTargetPool`，账 `L430`） |
| **L943** `TriggerCodex` | `CardScript__TriggerCodex.c:18-30` = `OnTrigger(0x91=145, mgr, this, this, targetCard ?? this)` | **改调用点** `RuleCore.cs:13274`（`CheckCodex` 里 `FireTriggerAt(..., chosen: u)`），行号未动 | **A** | ⚠️ 改了实参 ⇒ `chosen` 同时喂「指代 it」与 `AbilityFamilyGateOpsForTrigger` 族闸 ⇒ **可能改 `ruleprobe play check` 指纹，待同步点核**（本批红线禁跑） |
| **L336** `GetCardsInCemetery` | `BattleManager__GetCardsInCemetery.c`（全文）+ ILSpy `:41882` —— 墓地列表正序；`inclPlayer/inclEnemy` 挡两侧；**无条件**排 `cardType 10(Hero)` 与 `isRemnant(+0x65)`；`inclMinions/Spells/Whispers` 排 0/20/40；`inclWeapons` **体内一次没读**；另需 `+0x24b != 0` | **新增** `BattleContext.DeadUnit.IsRemnant`（`BattleContext.cs:132`，与 `DeathTurn` **同一行**以保零行数变化）；**灌值** `RuleCore.cs:9795`（残骸被摧毁 ⇒ true）· `:10255`（普通阵亡 ⇒ `u.IsRemnant`）；**新增** `RuleCore.GetCardsInCemetery`（`RuleCore.cs:15743`） | **A**（消费端接线未做，见右） | ⚠️ **消费方还没切**：`EffectResolver.ReturnFromCemetery`（`EffectResolver.cs:6812` 一带，`ta=60` 那一支）仍自己扫 `DeadUnits` ⇒ **那一支的残骸闸还没生效**（换成本口 = 一行，属 `EffectResolver` 写手）。⚠️ `+0x24b`（取出途中）我们无对应物（同步结算，先例 `PlayerState.cs:305-308`）。⚠️ 已知差：原版**一条全局墓地列表**（进墓先后），我们两方各一条 ⇒ 本口按「座位 0 ++ 座位 1」，只影响「候选多于上限时先取谁」 |

---

## §2 验证读数

| 项 | 读数 |
|---|---|
| **秒级类型检查** | ✅ **运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_B89E2 bash 工具/typecheck.sh`）。第一次跑出 7 条错，**全部**在 `CardPresentation/Battle/BattleDriver.cs`（E5）与 `RuleEngine/Core/TutorialScript.cs`（E7）——**不是我的文件**；隔一轮复跑已归零 |
| **行数守恒（铁律 18·b）** | `BattleContext.cs` **2319 → 2319**（`IsRemnant` 写在 `DeathTurn` 同一行 ⇒ 下面 30+ 处账落点**零漂移**）· `BoardSlots.cs` 590 → 640（+50 **全在类尾**）· `RuleCore.cs` 15587 → 15780（+193 **全在类尾**） |
| **既有落点未漂** | `RuleCore.cs:9795` / `:10255` / `:13274` 三个改动点行号**改动前后完全一致**（逐条 `grep -n` 复验） |
| **账补丁自检** | 7 行 × 11 列（表头 + 6 条）；LF / 无裸 CR / UTF-8 |
| **ruleprobe / play check** | ⛔ **没跑**（本批红线：不跑 Unity、不跑 ruleprobe）⇒ 指纹待同步点核 |
| **旧闸** | 本笔**没跑** `check_ledger_points`（主表只有调度台能写；补丁在校验范围内） |

---

## §3 顺手发现（⛔ 我没有顺手改别人的东西）

**a) 账 `L365` 的判据要点写错了（证伪，未改主表）。**
原写「从 `playerShuffledDeck` / `enemyShuffledDeck` 里**按序取 `cardState == inDeck(0)`** 的前 N 张」。
`decomp_full/BattleManager__GetNextCardsInDeck.c` 与 `cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:41091-41125` **两份产物里都没有那道过滤**（`0x228` 在本方法体 0 命中）。⇒ 本口照 `.c` 实现、⛔ 不补过滤；主表那一格建议调度台就地改（铁律 5）。

**b) 施工判据给 `L158` 写的「出队 = 清标记 + 调 `UnitState.RefreshForNewTurn`」与 `.c` 冲突（按 `.c` 办，已在代码注释里留反证）。**
`BattleManager__ResolveResetDuty.c` 全文 14 行 + ILSpy `:26518` 都只有 `usedActiveAbility = false` → `UpdateFigures()` → `FinishResolvingAction`。
`UnitState.RefreshForNewTurn`（`UnitState.cs:1782` 一带）清的是 `Exhausted / SummonSickness / CannotAttackThisTurn / AttacksThisTurn`，判据是 `CardScript__OnTurnStart` —— 那是**回合开始**那一批，与 `resetDuty` 是两件事。照施工判据调它会在结算中途**静默清掉疲劳/召唤病**。
⇒ 出队体只做 `.c` 那两件；口径写在 `RuleCore.AddResetDuty` 的 doc 里。

**c) 账 `L943` 差异列「R2T3B 现核：`FireTriggerAt` 无第 6 参等价物 ⇒ 差异确认」已过期。**
现签名第 6 参**就是** `chosen`（`RuleCore.cs:12514-12518`），原版第 6 参 `target ?? this` 的等价物已经存在，缺的只是**调用点没传**。已改（`RuleCore.cs:13274`）。

**d) `EffectResolver.ReturnFromCemetery` 的注释自己写着「将来若有卡面写 `return … remnants that died …`，要回来补这一维」**（`EffectResolver.cs:6805-6810`）——
这一维现在补上了（`DeadUnit.IsRemnant` + `RuleCore.GetCardsInCemetery`），那一支切过来是**一行**（属 `EffectResolver` 写手，不在本代理白名单）。

**e) `BoardSlots` 里「左邻」其实也**没有**独立薄口**（施工简报说「左邻已有，右邻缺」）——
`grep -rn "LeftNeighbor|Neighbor("` 全仓 0 命中；左邻今天是各调用点用 `BoardSpec.AdjacentSlots` / `slot-1` 现算。不影响本笔（原版也没有 `GetAdjacentUnitLeft`，只有 `GetAdjacentRightTroop`）。

---

## §4 没查清（如实留着，⛔ 不是「不做」）

1. **`RuleCore.TriggerKeywordOf`（`:13315` 一带）那条强制触发路**（`Trigger the Codex ability of a friendly unit` 等 8 张卡）传的第 6 参仍是 `null`。原版那条链最终也落到同一个 `CardScript.TriggerCodex(那张)`（目标 = 自己），**但「是不是要一并传 `chosen: u`」我没有逐句核**（它会连带影响 Ambush/Mob/… 其它关键词的强制触发）。⇒ 建议另开一条判据代理核「`AddTrigger*` 族逐个 Trigger 方法的 `targetCard` 实参」。
2. **`chosen = u` 对族闸的影响面没查**：`AbilityFamilyGateOpsForTrigger` 只在「按 trigger 定位不唯一」的卡上启用；现池 20 张 `Codex` 卡里哪些属于这一档、改后会不会剔掉某条 op —— **没查**（需要跑数据侧 + `ruleprobe play check`）。⇒ 同步点第一件事就是跑 `play check` 对指纹。
3. **`GetCardsInCemetery` 的一个边角组合未实测**：同一份实例「先当残骸被摧毁进墓 → `reanimate` 变回 → 再次阵亡」会在 `DeadUnits` 留两条（一条 `IsRemnant=true`），而本口按「任一条为残骸即排除」。原版读的是卡上那一格的**当前值**（`TransformFromRemnant` 会清 0）。该组合现读被「取走时从墓地移除」挡掉，**未实测**。
4. **原版 `GetNextCardsInDeck` 之外是否还有第二道 cardState 过滤**：我只核了方法体本身与它唯一的调用点 `AddDrawCard`（那边是 `AddRange` 直收）⇒ 结论成立；但**牌库列表本身的写入侧**（`CreatePlayerDeck` / `ShuffleDeck`）没有逐行核「有没有把非 inDeck 的卡放进 `*ShuffledDeck`」。

---

## §5 交接给下一个代理（Chain R 第 2 棒 = 批次 3）

**我动过 / 新增过的符号（批次 3 直接可用现读行号，⛔ 不需要重钉）：**

| 文件 | 符号 | 现读行号 | 动的方式 |
|---|---|---|---|
| `RuleEngine/Core/BattleContext.cs` | `DeadUnit.IsRemnant` | **132**（与 `public int DeathTurn;` **同一行**） | 新增字段（**零行数变化**，所以本文件下面 30+ 处落点**全部未漂**） |
| `RuleEngine/Core/BoardSlots.cs` | `RightNeighbor(PlayerState,int)` / `(PlayerState,UnitState)` | 620 / 632 | 新增（类尾） |
| `RuleEngine/Core/RuleCore.cs` | `PeekNextCards` | 15617 | 新增（类尾） |
| 同上 | `IsThereValidAbilityTarget` | 15647 | 新增（类尾） |
| 同上 | `AddResetDuty` / `ResolveResetDuty` | 15693 / 15705 | 新增（类尾） |
| 同上 | `GetCardsInCemetery` | 15743 | 新增（类尾） |
| 同上 | `CheckCodex` 里的 `FireTriggerAt` 调用 | **13274** | **改实参**（加 `chosen: u`），行号未变 |
| 同上 | 两处 `ctx.DeadUnits.Add(new DeadUnit{…})` | **9795** / **10255** | **改实参**（加 `IsRemnant`），行号未变 |
| 同上 | `TrySwarmMerge` / `SwarmMergeNow` 的右邻 | 4573 / 4606 一带 | **改一行**（`BoardSlots.RightNeighbor`），行数未变 |

**给批次 3 的四条硬提醒：**
1. **`RuleCore.cs` 归你**（批次 3 的 10 条账）。我只在**类尾**追加，中间**一行没插** ⇒ 你的落点直接用现读行号。
2. **你会碰 `CheckCodex` / `FireTriggerAt` 那一带**（账 `L214` 快照族、`L815/818` 回合广播）：注意 `:13274` 现在是 `chosen: u` —— 如果你要改 `FireTriggerAt` 的**形参表**，那 20+ 处调用会二义 `CS0121`，⛔ 别加形参。
3. **类尾已被我占用 4 个方法名**（`PeekNextCards` / `IsThereValidAbilityTarget` / `AddResetDuty`+`ResolveResetDuty` / `GetCardsInCemetery`），你追加时别重名。
4. **指纹**：本批改了 `TriggerCodex` 的实参 ⇒ 同步点第一件事 = `ruleprobe play check` 前后对指纹 + `play all dump` 逐字节 diff；我这边**跑不了**，读数待你或调度台补。

**未接线的待接点（都属别人文件，⛔ 我没动）：**
- `EffectResolver.ReturnFromCemetery`（`:6812` 一带）→ 切到 `RuleCore.GetCardsInCemetery`（`ta=60` 那一支的残骸闸才会生效）。
- `EffectResolver.DoReloadDuty`（`:15171`）→ 切到 `RuleCore.AddResetDuty`（同步 ⇒ 入队；净结果同）。
- `RuleCore.TriggerKeywordOf`（`:13315`）→ 强制触发那条路的第 6 参是否也传 `u`（见 §4-1）。
