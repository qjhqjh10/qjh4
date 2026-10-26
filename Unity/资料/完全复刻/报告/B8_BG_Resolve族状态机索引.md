# B8_BG · `BattleManager.Resolve*` 族「`BattleActionType` → 状态机」索引

> 代号 `B8_BG` · 2026-10-26 · 判据代理（只写判据产物 + 账补丁）
> 产物：`判据/B8_Resolve族_状态机索引.tsv`（89 行）· `账/_parts/B8_BG.tsv`（21 行，11 列）· 本报告
> 关联：`报告/B8_AU_引擎核心批量.md:141`（本轮普查的触发点）

---

## ① 结论

### 1·1 先纠一条前提（铁律 5：就地改正）

`B8_AU` 的说法是「`BattleManager.Resolve*` 那一族在原版里**全是**闭包构造器」。
**实测只对一半**：`BattleManager` 里 `Resolve*` 共 **83 个方法**（另 1 个同名重载行在账里出现两次，账行合计 84）：

| 形态 | 个数 | `.c` 是不是真体 | 能否靠它升 A |
|---|---|---|---|
| **`IEnumerator`（迭代器 / 闭包 kickoff 壳）** | **39** | ❌ `BattleManager__ResolveX.c` 只有「建状态机对象 + 拷 `BattleAction` 结构体（0x100 字节）+ 返回」，**零游戏逻辑** | ❌ 必须改引状态机 |
| **同步方法（`void` 31 个 / `DamageInfo` 3 个 / `void` 里带尾协程 1 个）** | **44** | ✅ **就是真体**（例：`ResolveRemoveEffectsFrom.c` 全文 4 行 = `BroadcastRemoveEffectsFrom` + `FinishResolvingAction(manager, 0x2f, 0)`） | ✅ 可以直接当判据 |

⇒ 账里靠 `Resolve*.c` **不能升 A 的只有那 39 个迭代器对应的行**，不是 82 行。

### 1·2 索引结果：39/39 全部对上，**零对不上**

- 对法（🥇 一手证据）：ILSpy `out_cpp2il_dev_ilspy/BattleManager.cs` 里每个 kickoff 方法体都写着
  `new <Xxx>d__NNN(0)` —— 编译器把状态机类型名与编号**明写**在方法体里，不需要猜。
  例：`BattleManager.cs:25882 ResolveHeal` → `:25893 new <ResolveHeal>d__491(0)`。
- 逐个核对 `decomp_full/BattleManager._<X>_d__NNN__MoveNext.c` 的**首行函数名是不是它自己**
  （防「文件名与内容不符」那 16.5%）：**39/39 首行都是 `BattleManager_<X>d__NNN__MoveNext`**。
- 逻辑密度：39 份状态机**没有一份是空壳** —— 业务调用最少 **2** 处（`ResolveTriggerTraitAnim`）、
  中位数 **10** 处、最多 **120** 处（`ResolveAttack`，1,653 行）。⇒ 改引之后这些行**真的有判据可读**。
- 另有 5 条同族闭包（`CardScript.PrepareAttack/ReceiveDamage/UnitDeath`、`PlayerHand.AddCardNotDrawnToHand[AtIndex]`）
  一并核了：账里**本来就引对了**状态机（`d__353 / d__381 / d__446 / d__39 / d__40`），无需改。

### 1·3 `BattleActionType → Resolve 方法` 分派表（72/90 个枚举值有证据）

两条独立证据通道（都出自 `decomp_full/BattleManager__ResolveAction.c`，9,301 行）：

1. **类令牌通道（🥇）**：每个 `case N:` 体里 `thunk_FUN_18042f4a0(DAT_x)` 的 `DAT_x` 就是它要 new 的状态机类型；
   把 39 个 kickoff `.c` 里各自的 `DAT_x` 取出来做反查 ⇒ 唯一对应。
2. **`FinishResolvingAction` 常量通道（🥇）**：同步方法的 `.c` 里写着 `BattleManager__FinishResolvingAction(manager, <枚举值>, 0)`
   （例 `ResolveChangeBaseAttack` → `0x13`=19=`changeAttack`、`ResolveRemoveEffectsFrom` → `0x2f`=47、`ResolveResetActions` → `0x29`=41）。
3. **内联体相似度通道（🥈）**：同步方法被整段**内联**进 `ResolveAction` 的 case 体
   （实证：`ResolveStun.c` 223 行 vs case 17 体 234 行、调用集重叠 2/2；`ResolveJamUnit.c` 322 行 vs case 22 体 331 行、重叠 5/8）。
4. **直接调用通道（🥇）**：`BattleManager__ResolveX(...)` 字面出现在 case 体里
   （`ResolveEndTurn` L1270 · `ResolveTransformUnit` L2931 · `ResolveDestroyUnit` L4061 · `ResolveFollowAfterResolution` L4746 ·
   `ResolveTriggerRequiem` L5574 · `ResolveTriggerCodex` L6074 · `ResolveTriggerPlayerCodex` L6122 · `ResolveTriggerMob` L7320/7368 · `ResolveTriggerPray` L7416）。

⚠️ **ILSpy 那份 `ResolveAction`（`BattleManager.cs:21939-22450`）不可用** —— 它的体被 Cpp2IL 打成
`_ = battleAction.actionType;` 一类的垃圾 + `goto IL_xxxx`，一行 switch 都读不出来。**分派判据只能看 `decomp_full` 的 `.c`。**

---

## ② 索引表摘要（前 20 行；全 89 行见 `判据/B8_Resolve族_状态机索引.tsv`）

| # | 原版方法 | 类 | 闭包/状态机 | 状态机 .c 路径:行号 | 对应 BattleActionType（值） | 账行号（哪些行引了它） | 建议 |
|---|---|---|---|---|---|---|---|
| 1 | ResolveAction(…) | BattleManager | —（同步方法，无状态机，本身即真体） | decomp_full/BattleManager__ResolveAction.c:1-9301 | —（不由 ResolveAction 分派 / 未确认） | 538 | 同步方法，现行 .c 即真体，无需改引 |
| 2 | ResolveDrawCard(…) | BattleManager | <ResolveDrawCard>d__432（IEnumerator 状态机） | decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c:1-314 ｜ kickoff = decomp_full/BattleManager__ResolveDrawCard.c:1-83（仅建协程壳，零游戏逻辑） | drawCard=3（DAT_18426ca88） | 562,563 | 引状态机（<ResolveDrawCard>d__432（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 3 | ResolveAttack(…) | BattleManager | <ResolveAttack>d__438（IEnumerator 状态机） | decomp_full/BattleManager._ResolveAttack_d__438__MoveNext.c:1-1653 ｜ kickoff = decomp_full/BattleManager__ResolveAttack.c:1-83（仅建协程壳，零游戏逻辑） | attack=1（DAT_18426c1b0） · scriptedAttack=52（同组 case） · attackContinuation=53（同组 case） · redirectedAttack=74（同组 case） | 546 | 引状态机（<ResolveAttack>d__438（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 4 | ResolveAttackDamage(…) | BattleManager | —（同步方法，无状态机，本身即真体） | decomp_full/BattleManager__ResolveAttackDamage.c:1-38 | —（不由 ResolveAction 分派 / 未确认） | 547 | 同步方法，现行 .c 即真体，无需改引 |
| 5 | ResolveTraitAdjacentDamage(…) | BattleManager | —（同步方法，无状态机，本身即真体） | decomp_full/BattleManager__ResolveTraitAdjacentDamage.c:1-33 | —（不由 ResolveAction 分派 / 未确认） | 596 | 同步方法，现行 .c 即真体，无需改引 |
| 6 | ResolveTraitStompDamage(…) | BattleManager | —（同步方法，无状态机，本身即真体） | decomp_full/BattleManager__ResolveTraitStompDamage.c:1-30 | —（不由 ResolveAction 分派 / 未确认） | 598 | 同步方法，现行 .c 即真体，无需改引 |
| 7 | ResolvePlayCardFromHand(…) | BattleManager | <ResolvePlayCardFromHand>d__447（IEnumerator 状态机） | decomp_full/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:1-1549 ｜ kickoff = decomp_full/BattleManager__ResolvePlayCardFromHand.c:1-83（仅建协程壳，零游戏逻辑） | playCardFromHand=5（DAT_18426d460） | 580 | 引状态机（<ResolvePlayCardFromHand>d__447（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 8 | ResolveFollowAfterResolution(…) | BattleManager | —（同步方法，无状态机，本身即真体） | decomp_full/BattleManager__ResolveFollowAfterResolution.c:1-434 | followAfterResolution=58（直调 L4746） | 569 | 同步方法，现行 .c 即真体，无需改引 |
| 9 | ResolveBacklash(…) | BattleManager | <ResolveBacklash>d__451（IEnumerator 状态机） | decomp_full/BattleManager._ResolveBacklash_d__451__MoveNext.c:1-179 ｜ kickoff = decomp_full/BattleManager__ResolveBacklash.c:1-83（仅建协程壳，零游戏逻辑） | backlash=13（DAT_18426c2a8） | 548 | 引状态机（<ResolveBacklash>d__451（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 10 | ResolveMinionDeath(…) | BattleManager | <ResolveMinionDeath>d__454（IEnumerator 状态机） | decomp_full/BattleManager._ResolveMinionDeath_d__454__MoveNext.c:1-158 ｜ kickoff = decomp_full/BattleManager__ResolveMinionDeath.c:1-83（仅建协程壳，零游戏逻辑） | minionDeath=4（DAT_18426d070） | 575 | 引状态机（<ResolveMinionDeath>d__454（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 11 | ResolveReassembleMinions(…) | BattleManager | <ResolveReassembleMinions>d__455（IEnumerator 状态机） | decomp_full/BattleManager._ResolveReassembleMinions_d__455__MoveNext.c:1-48 ｜ kickoff = decomp_full/BattleManager__ResolveReassembleMinions.c:1-23（仅建协程壳，零游戏逻辑） | reorderMinionsInPlay=7（DAT_18426d560） | 581 | 引状态机（<ResolveReassembleMinions>d__455（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 12 | ResolveMultiDamage(…) | BattleManager | <ResolveMultiDamage>d__456（IEnumerator 状态机） | decomp_full/BattleManager._ResolveMultiDamage_d__456__MoveNext.c:1-195 ｜ kickoff = decomp_full/BattleManager__ResolveMultiDamage.c:1-83（仅建协程壳，零游戏逻辑） | doMultiDamage=9（DAT_18426d170） | 577 | 引状态机（<ResolveMultiDamage>d__456（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 13 | ResolveMultiDamageAndDestroySelf(…) | BattleManager | <ResolveMultiDamageAndDestroySelf>d__457（IEnumerator 状态机） | decomp_full/BattleManager._ResolveMultiDamageAndDestroySelf_d__457__MoveNext.c:1-335 ｜ kickoff = decomp_full/BattleManager__ResolveMultiDamageAndDestroySelf.c:1-83（仅建协程壳，零游戏逻辑） | doMultiDamageAndDestroySelf=75（DAT_18426d268） | 578 | 引状态机（<ResolveMultiDamageAndDestroySelf>d__457（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 14 | ResolveTraitAnim(…) | BattleManager | <ResolveTraitAnim>d__465（IEnumerator 状态机） | decomp_full/BattleManager._ResolveTraitAnim_d__465__MoveNext.c:1-311 ｜ kickoff = decomp_full/BattleManager__ResolveTraitAnim.c:1-33（仅建协程壳，零游戏逻辑） | —（不由 ResolveAction 分派 / 未确认） | 597 | 引状态机（<ResolveTraitAnim>d__465（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 15 | ResolveAnim(…) | BattleManager | <ResolveAnim>d__466（IEnumerator 状态机） | decomp_full/BattleManager._ResolveAnim_d__466__MoveNext.c:1-252 ｜ kickoff = decomp_full/BattleManager__ResolveAnim.c:1-83（仅建协程壳，零游戏逻辑） | playAnim=11（DAT_18426beb8） | 543 | 引状态机（<ResolveAnim>d__466（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 16 | ResolveAnimInfoRangeAttack(…) | BattleManager | <ResolveAnimInfoRangeAttack>d__468（IEnumerator 状态机） | decomp_full/BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:1-149 ｜ kickoff = decomp_full/BattleManager__ResolveAnimInfoRangeAttack.c:1-31（仅建协程壳，零游戏逻辑） | —（不由 ResolveAction 分派 / 未确认） | 545 | 引状态机（<ResolveAnimInfoRangeAttack>d__468（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 17 | ResolveUnitShake(…) | BattleManager | <ResolveUnitShake>d__472（IEnumerator 状态机） | decomp_full/BattleManager._ResolveUnitShake_d__472__MoveNext.c:1-182 ｜ kickoff = decomp_full/BattleManager__ResolveUnitShake.c:1-24（仅建协程壳，零游戏逻辑） | —（不由 ResolveAction 分派 / 未确认） | 619 | 引状态机（<ResolveUnitShake>d__472（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 18 | ResolveAnimAnimationClip(…) | BattleManager | <ResolveAnimAnimationClip>d__473（IEnumerator 状态机） | decomp_full/BattleManager._ResolveAnimAnimationClip_d__473__MoveNext.c:1-121 ｜ kickoff = decomp_full/BattleManager__ResolveAnimAnimationClip.c:1-31（仅建协程壳，零游戏逻辑） | —（不由 ResolveAction 分派 / 未确认） | 544 | 引状态机（<ResolveAnimAnimationClip>d__473（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 19 | ResolveAbilityIconAnim(…) | BattleManager | <ResolveAbilityIconAnim>d__475（IEnumerator 状态机） | decomp_full/BattleManager._ResolveAbilityIconAnim_d__475__MoveNext.c:1-47 ｜ kickoff = decomp_full/BattleManager__ResolveAbilityIconAnim.c:1-83（仅建协程壳，零游戏逻辑） | abilityIconAnim=21（DAT_18426bcc0） | 537 | 引状态机（<ResolveAbilityIconAnim>d__475（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |
| 20 | ResolveTriggerTraitAnim(…) | BattleManager | <ResolveTriggerTraitAnim>d__476（IEnumerator 状态机） | decomp_full/BattleManager._ResolveTriggerTraitAnim_d__476__MoveNext.c:1-32 ｜ kickoff = decomp_full/BattleManager__ResolveTriggerTraitAnim.c:1-83（仅建协程壳，零游戏逻辑） | traitTriggerAnim=68（DAT_18426e030） | 617 | 引状态机（<ResolveTriggerTraitAnim>d__476（IEnumerator 状态机））；kickoff .c 不能当 B 升 A 依据 |

---

## ③ 改了 21 行账的判据出处

补丁：`账/_parts/B8_BG.tsv`（**11 列**，只放改过的行，`(原版类, 方法)` 与主表逐字相同）
`merge_ledger_parts.py --dry --parts <只含本补丁的目录>` 回报：**更新 21 · 新增 0 · 无变化 0**。

| 情况 | 行数 | 处理 |
|---|---|---|
| 迭代器行、原本只引 kickoff `.c` 或 ILSpy `.cs` | **21** | 判据出处**追加**状态机 `.c` 全路径 + `:1-N` 行号范围；`备注` 追加「原引闭包构造器、已改引状态机（B8_BG 2026-10-26；状态机 d__NNN 由 ILSpy `new <X>d__NNN(0)` 实证）」 |
| 迭代器行、本来就引对状态机 | 19 | **不动**（并逐个核了引用行号是否越界：**0 条越界**） |
| 同步方法行 | 44 | **不动**（`.c` 即真体，无状态机可引） |

改动的 21 行（`战斗逻辑驱动.tsv` 行号）：
`537 540 543 544 545 554 555 556 560 561 570 571 589 590 591 592 595 597 617 619 620`

顺手做了两件事（都在「判据出处」列，未碰 `状态`）：
- 补全裸路径 `BattleManager.cs:NNNN` → `out_cpp2il_dev_ilspy/BattleManager.cs:NNNN`（原先缺反编译来源前缀，不符 G0 的「标明是哪一份反编译」）。
- ⚠️ **未改、留给调度台裁**：行 **594 `ResolveStun`** 的判据出处写着「🥇 **状态机**：…」，
  但 `ResolveStun` 是**同步方法、没有状态机**（`BattleManager__ResolveStun.c` 223 行即真体）。**文字标签错、内容对**。
  我没动它（避免与本补丁的「改引」主题混在一起），建议下一位把「状态机」三字换成「同步方法体」。

---

## ④ 顺手判成 A 的：**0 条**

**理由（不凑数）**：G0–G8 里 G3/G4/G5 要求「选一个观测量、两侧数值、两次读数、边界反例」，
那要动探针（`ruleprobe play <卡ID>` 的夹具与卡 ID 选取）并逐句读我们的落点 —— 超出「改引」这一件事，
硬判会产出「看着像 A」的假 A。**本报告只把 G0 那一侧做实了**（方法体在位、行号可复算、来源前缀齐）。

给执行代理的**开工顺序建议**（按「体小、观测量现成」排序，都满足 G0）：

| 建议顺序 | 方法 | 状态机 | 行数 | 业务调用数 | 观测量（`判据/战斗逻辑驱动_B2A_口径.md` §三） |
|---|---|---|---|---|---|
| 1 | `ResolveRevealOrders` | `d__549` | 90 | 6 | 手牌/牌库内容 |
| 2 | `ResolveFatigue` | `d__587` | 157 | 6 | 血量（疲劳掉血） |
| 3 | `ResolveBacklash` | `d__451` | 179 | 7 | 血量 / 墓地 |
| 4 | `ResolveHeal` | `d__491` | 231 | 10 | 血量 / 最大血量 |
| 5 | `ResolveDiscardFromHand` | `d__544` | 165 | 10 | 手牌张数 |
| 6 | `ResolveCreateDeckCard` | `d__513` | 131 | 10 | 牌库张数 |
| ⚠️ 不建议先碰 | `ResolveAttack` / `ResolvePlayCardFromHand` | `d__438`/`d__447` | 1653/1549 | 120/82 | 太大，先拆块 |

---

## ⑤ 没查清的部分（写清「查不到」）

1. **`ResolveAction` 的 90 个 case 里有 3 个归不了属**：
   - `case 71` = `setDeckCardTop` —— 体长 246 行、最像 `ResolveDisappearUnit`（r=0.96），但那是**共用尾巴**，不认。
   - `case 86` = `showCardsWindow` —— 只用到共享令牌 `1842b2208`。
   - `case 91` = `triggerCardPlayedByStimulation` —— 只用到共享令牌 `184249088`
     （该令牌同时被 `ResolveAttackDamage` / `ResolveTraitAdjacentDamage` / `ResolveTraitStompDamage` 复用 ⇒ **ICF 折叠，不能归属**）。
   ⇒ 索引表里这 3 个方法的状态机**仍然是对的**（ILSpy 明写 `new <X>d__NNN(0)`），只是**「哪个枚举值触发它」查不到**。
2. **`case 85` = `afterResolutionAttackContinuation`** → 调 `AddAttackContinuationAction`，**不是** `Resolve*` 方法。
   `case 73` = `reanimate` **没有独立 case**（并入 `case 72` 的体内）。
3. **枚举值 2/6/8/12/24/32/33/40/55 没有证据**：它们**不在** `ResolveAction` 的 switch 里
   （`createMinion` / `showEnemyCard` / `doDamage` / `playHeroPower` / `replaceHeroPower` / `mulligan` /
   `returnToDeck` / `growthAnim` / `chatMessage`）⇒ 在别处处理，本轮**没查**（不在本任务范围）。
4. **4 个方法不由 `ResolveAction` 分派**：`ResolveUnitShake`(`d__472`) ·
   `ResolveTraitAnim`(`d__465`) · `ResolveAnimInfoRangeAttack`(`d__468`) · `ResolveAnimAnimationClip`(`d__473`)
   —— 由别的 `Resolve*` 状态机内部调用；索引表枚举列写「—」，**不猜**。
5. **`ResolveChangeBaseAttack` / `ResolveChangeMaxHealth` 的账里行号偏小**：账写 `.c:1-22`，实际文件 **28 行**
   （真体，已验：`Log` + `CardScript__ChangeXxx` + `FinishResolvingAction`）。不影响结论，仅供下轮订正。

---

## 出处速查

| 事 | 判据 |
|---|---|
| 39 个迭代器 → 状态机编号 | `out_cpp2il_dev_ilspy/BattleManager.cs`（每个 kickoff 体内的 `new <X>d__NNN(0)`） |
| 状态机 `.c` 首行自证 | `decomp_full/BattleManager._<X>_d__NNN__MoveNext.c:1` |
| case → 类令牌 | `decomp_full/BattleManager__ResolveAction.c` 的 `case N:` + `thunk_FUN_18042f4a0(DAT_x)` |
| `FinishResolvingAction` 常量 | 各同步方法 `.c`（例 `BattleManager__ResolveRemoveEffectsFrom.c:4`） |
| 内联体相似度 | 本报告 §1·3 第 3 条（case 体长度 vs 方法 `.c` 长度 + 调用集重叠） |
| 枚举定义 | `out_cpp2il_dev_ilspy/BattleActionType.cs`（0–91） |
| G0–G8 口径 | `判据/战斗逻辑驱动_B2A_口径.md:24-85` |
