# B9_E5 · 战斗逻辑驱动 · RuleCore 切片账升级（前 45 行）

> 切片命令：`python -I -X utf8 工具/ledger_slice.py --domain 战斗逻辑驱动 --status B --file RuleCore --limit 0`（431 条）
> 本轮做的是**按账行号升序前 45 行**（账行 61 → 168，含 6 条非 `Add*` 行 + 39 条 `BattleManager.Add*`）。
> 补丁：`资料/完全复刻/账/_parts/B9_E5.tsv`（44 行 · 11 列 · 键全部与主账逐字对应）
> 落点机检：`python 工具/check_ledger_points.py 资料/完全复刻/账/_parts/B9_E5.tsv` ⇒ **A=26 通过=26 漂移=0 告警=0 失败=0**
> ⛔ 未跑 Unity、未动 git、未改主账表、未改任何 `.cs`（本轮只有账与报告落盘）。

---

## ① 结论

| 档 | 条数 | 说明 |
|---|---|---|
| **A 升** | **26** | 判据（`.c` 全文或 ilspy 方法体）现读 + 我们侧落点逐段读过 |
| **C 判** | **1** | `AddResetActions`（退还那一跳是**同步**而非入队 ⇒ 与同批触发的先后可能不同） |
| **D 判** | **7** | 4 条「原版这一支在本 build 不可达」（chosenAbility 165/240/290 池内各 0 张 + `CanPlayAbility` 整条链 0 调用点）；3 条「原版这条路本身没有数值效果」（`GetAbilityInstance` 死代码 · Plasma/Psychic mana 的 buff 位零读者） |
| **留 B** | **11** | 本轮写了 10 条（更新判据/差异但仍是 B）+ 第 88 行 `ActiveWarlordAbility.OnTouchUpAsButton` **一个字没改**（见 ③） |

### 三个数字

- **升 A = 26**
- **判 C = 1**
- **留 B = 11**（其中 10 条已改写进补丁，1 条 = 第 88 行未动）

### 判 C 的条目

- `BattleManager.AddResetActions(...)` —— 原版是**入队**一条 `resetActions(41)` 动作（`_ResolveForceAttack_d__504__MoveNext.c:76`，道 = `globalVars+0xb8`），我们由 `RuleCore.DeclareAttackRefundingForced` 在攻击结算后**同步**调 `RefundForcedAttackAction`。**效果逐句对得上**（`ResetActions.c:18/22-27/29-31/34` 四条都在），差的是**它在动作序列里的位置**：我们恒在泵队之前 ⇒ 恒先于「同一记攻击里入队的其它触发」；原版取决于道。等级 P2（今天池内触发它的只有 231/237/238 那条强制攻击退还路）。修法写在该行备注里。

---

## ② 逐条证据（只列 C 与「有疑点」的）

### C-1 `BattleManager.AddResetActions(CardScript targetCard, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions)`

- **原版**：`decomp_full/BattleManager__AddResetActions.c:27` 造 `resetActions(0x29=41)` → `:77` 入队。三个调用点：`_ResolveForceAttack_d__504__MoveNext.c:76`（道 `globalVars+0xb8`）· `_ResolveScriptedTargetCardAttack_d__505__MoveNext.c` · `AbilityLogic__PlayAbility.c:2632`（`case 0xdc` = chosenAbility **220**，`cards_truth.json` 现读 **0 行**）。
- **我们**：`RuleEngine/Core/RuleCore.cs:14102（DeclareAttackRefundingForced）` → `:14139（RefundForcedAttackAction）`。
- **差在哪**：效果一致、**时序不一致**（见 ①）。

### 疑点 1（最要紧）`AddAttackContinuationAction` / `AddFollowResolutionAttackContinuationAction`（两行都留 B）

- **原版**：`_ResolveAttack_d__438__MoveNext.c` 有两条「这一记不打」的支——
  - 伏击翻开（`:305` `SetAmbush(false)` 之后）→ `goto LAB_1809ad363`；
  - `landing` 取消（`:368-379` `CancelAttack` + 两次 `ClearPendingDamage` + `OnTrigger(440)`）→ **`:426` `AddAttackContinuationAction`**；
  - `LAB_1809ad363`（`:488`）**一定**调 `AddFollowResolutionAttackContinuationAction`（造一条 `attack(1)` 入队到 `globalVars+0xb8`）。
- **我们**：伏击支与 `landing` 支都是 `return RuleCodes.OK`（`RuleCore.cs:6227-6250` / `:6384-6390`），**不压任何续攻**。
- **未查清**：续攻动作出队后走 `_ResolveAttack` 会不会真的出伤害（它的 case 与标记位没解出来；`AddFollowResolutionAttackContinuationAction.c:406` 还有一条「上一条是 `forceAttackFriendlyUnits(0x42)` 时改道」的分叉）。
- ⇒ **伏击在池内可达**：如果续攻真出伤害，这就是 **P0 级漏复刻**；如果续攻只发 `FollowUpResolution(410)` 不出伤害，本行只是 P2。**这是本轮最该接着查的一条。**

### 疑点 2 `AddCardToDeck` / `AddInitialCardToDeck`（两行留 B）

- 两份产物里**都只有定义、0 调用点**（`AddCardToDeck` 还有一份 `BattleCardManager` 的 private 同名件，同样 0 调用）。调用点在「建库那一带」，而 cpp2il 已知会静默丢调用点（先例 `AddMissAction`：ilspy 0 命中、decomp 里有）⇒ **不足以判 D**。
- 我们侧：建库直接 `Deck.Add`（`RuleCore.cs:618`）+ 三处专门口（`decktop` / `InsertIntoDeckRandom` / `DoReturn`），**没有通用「插到第 N 位」口**。

### 疑点 3 `BattleManager.AddCardAnim(...)`（留 B）

- 原版只是「压一条 `anim(1)` 动作，带三锚点 + `waitForAnim`」（`.c` 98 行）。
- 我们：`Card3DAnimationController`（`CardPresentation/Battle/Card3DAnimationController.cs:208 DoAnim`）已逐方法复刻，但**调度**不是同一形状 —— 驱动从 `ctx.Events` 事件流按 `AbilityAnimDurations` 表播，三锚点播前现解析，`waitForAnim` 的「阻塞后续动作」没有逐条对过 ⇒ 留 B（表现层，不主张对齐）。

### 疑点 4 `BattleManager.AddNextReplayAction()`（留 B）

- `.c` 全文 21 行、真身是被 Ghidra 看丢的 `yield` 状态机（ilspy 签名是 `private IEnumerator`）；两份产物 0 调用点。
- 我们自己的录像走另一套契约（`起始条件 + 动作流`，见 `资料/本地录像_设计与交接.md`），与原版这个动作没有 1:1 关系。**谁调它、回放时它做什么 = 未查清。**

### 疑点 5 `AddRemoveFromCemetery`（留 B，**不是漏做**）

- 原版整条动作（`.c` 91 行 + `ResolveRemoveFromCemetery.c` 232 行）**唯一效果** = `CardScript.RemoveCardFromCemetery`，而那份方法体 5 行：`*(byte*)(this+0x24b) = 0`。
- `+0x24b` = `shouldCheckIfIsInCemetery`（`Awake` 置 1、`GetCardsInCemetery.c:54` 读它跳过「正在取出」的卡）⇒ 是**重入闸**、不是数据。
- 我们同步结算下没有这个中间态 ⇒ 无落点行。判 B 沿用先例（`PlayerState.cs:305-308`）。

### 疑点 6 `AbilityData.SetNewData` / `ActiveAbility.DeepCopy` / `ShallowCopy`（三行留 B）

- `SetNewData` 的 `isPlayerTurn/priority/blockPlayerActions` 三格在 cpp2il 里被反编译成 `default`（**形参丢失**）⇒ 判据侧不可靠，要 VA 反汇编才能确认。我们侧没有 `AbilityData` 这个可重置打包体。
- `DeepCopy`/`ShallowCopy`：两份产物 0 调用点，但 decomp 只覆盖约 29% 且 cpp2il 会丢调用点 ⇒ **证据不足以判死**。等价的前提 = 没有任何消费者写那份能力数据 —— 现读只证实了 `targetsAvailableCounter`（`CardScript.cs:8317-8344`、`ActiveSkillDisplay.cs:157-172` 全只读），`targetCriteria(+0x38)` 的写点**没查**。

### 顺带订正（写进补丁备注）

🔴 **旧账「原版每跳一次能力就克隆一份 AbilityData（防污染）」不成立**：`AbilityData.GetAbilityInstance` 两份产物 **0 调用点**；真在跑的是**共享一份 `pooledAbilityData` + `SetNewData`**（`RawCardScript.cs:1768` / `:2005`，`RawCardScript__ResolveActiveAbility` 亦如此）。

🔴 **旧账「我们无动作队列」已过期**（行 128-133 那一带的通用差异）：`Core/ActionQueue.cs`（四道 FIFO + 消费序 2→1→3→0 写死）+ `Core/ActionPump.cs`（泵点/动作外壳）自 2026-10-21 起就是原版那份形状。本轮把这 39 行的落点从「无队列」改成实名入口。

🔴 **`AddGoesSecondCardToDeck` 名字误导**：`.c:184` 落点是 **`AddNewCardToHand`**（进手牌，不进牌库）—— 我们按手牌实现是对的。

---

## ③ 没查清的部分（如实）

1. **续攻那两条动作出队后是否出伤害**（疑点 1）—— 决定它是 P0 还是 P2。这是本轮最该接着查的。
2. **第 88 行 `ActiveWarlordAbility.OnTouchUpAsButton` 一个字没改**（仍 B）：它整条是表现层（`parentCard.isPlayer` 闸 → `activeSkillDesc.SetupActiveSkill` → `TryUsingActiveAbilityOfCard(forceSelectCard: true)` → `PressedBtn` 亮 0.4 s；另有 `Update` 里按住 0.3 s → `ForceDisplayCard`）。引擎面的等价 = `RuleCore.CanUseAbility/CanStartAbility` + `SkillPanelConfirmRelease`（`CardPresentation/Battle/BattleDriver.cs:15092`），但 **0.3 s 长按开卡 + 0.4 s 按压反馈** 两条我在 `CardPresentation` 里没找到对应件（`CardInteraction` 只有手牌那条 0.2 s 轻点门）⇒ **不敢判 C**（可能是别的形状实现的），留给表现层代理核。
3. **`AddEffect` 的 `BroadcastUnitPoisoned`**：`ResolveAddEffect.c` 里那条广播我们对应到哪条 `WhenEvent` 没逐条核（本行判 A 只主张「挂效果」这一跳）。
4. **`globalVars+0xc4`（`AddEndTurnAction` 的道）** 与 **`globalVars+0xb8`（`AddBerzerkAttack`/续攻那两跳的道）** 的具体档位没解 —— 我们没有这组 `globalVars` 优先级枚举，不猜。
5. `AddCardToDeck` / `AddInitialCardToDeck` 的**可达性**（疑点 2）没定。

---

## ④ 交接给下一个代理

**做到哪了**：账行 61 → 168（第 1–45 条）全部过了一遍，44 条写进补丁（第 88 行未动）。下一条从 **账行 169 `BattleManager.AddResetDuty(...)`** 开始。

**已确定的规律**（下一批直接用，别重查）：

1. **`Add*` 家族的统一形状**：`BattleAction___ctor(&a, <动作号>, …)` + 塞 1–3 个字段 + `AddAutoActionToQueue(bm, &a, <道>)`。所以判据要点写「动作号 + 字段 + 道 + 出队体做了什么」四样就够。
   已解出的动作号：attack=1 · draw=3 · endTurn=14 · addEffect=15 · removeEnchantment=16 · addHandEffect=25 · recallToHand=26 · discardFromHand=28 · manaBuff=34 · forceAttack=42 · disappearUnit=44 · autoPlayFromHand=46 · miss=50 · recallToDeck=51 · disappearFromHand=56 · displayFromHand=57 · followAfterResolution=58 · removeFromCemetery=60 · forceAttackFriendly=66 · redirectedAttack=74 · executeSwarm=79 · resetActions=41 · playerAction(resign)=54。
2. **判据取法**：`decomp_full/BattleManager__AddXxx.c` 比 ilspy 干净得多（ilspy 这几段被 `Cpp2ILHelpers.NoteDecompilerIssue` 刷成噪音、且**会丢调用点**）。出队体去 `BattleManager__ResolveXxx.c`，`Resolve*` 里往往只有一个具名调用 ⇒ 语义就在那个 `CardScript.Xxx` 方法里。
3. **可达性用 chosenAbility 定**：`AbilityLogic__PlayAbility.c` 里 `Add*` 的唯一调用点都是某个 `case 0x..` / `if (iVar == 0x..)` 分支。把该数除以 16 后与 `Resources/cards_truth.json` 里 `chosenAbility` 的出现次数对一遍即可判「池内 0 入口 ⇒ D + 时间限定」。
   已核：**165 / 220 / 240 / 290 各 0 行**（⇒ AddDiscardFromHand / AddResetActions 的 220 支 / AddDisappearUnit / AddMissAction）。
4. **`ManaBuff` 的 offset 序**：`+0x10 manaAmount / +0x14 friendlyPlayer / +0x15 enemyPlayer / +0x16 createMana / +0x17 refillMana / +0x18 untilEndOfTurn / +0x19 psychicMana / +0x1a plasmaMana / +0x1b spiritStoneMana / +0x1c faithMana / +0x1d questPoints`；`ProcessManaBuff` **只读 +0x16/17/1b/1c/1d**。
5. **落点写法**：`路径:行（裸声明名）（等价实现 = …）`，且**那一行必须是声明**（注释行/语句行/case 标签一律被闸判失败）。区间写法 `:a-b` 会被闸按「那一行」判 ⇒ 起点必须是声明行。
6. **闸自检**：`python -I -X utf8 工具/check_ledger_points.py 资料/完全复刻/账/_parts/B9_E5.tsv`（带显式文件路径，**别不带参**——不带参是整目录只读巡检，`--fix` 会写主表）。

**踩到的坑**：

- `ls` 在这台机器上遇到 `--f__AnonymousType0.cs` 会当参数解析报错（用 `find` 或 `grep -r` 代替）。
- 写补丁时**别用 `bash` 的 heredoc 里带反引号/引号的 python 字符串**替换整段（我第一次替换失败、断言挡住了；改用「按 tab 切列再改第 7 列」一次过）。
- `decomp_full` 里的算子名是 `BattleManager__Xxx`（双下划线），协程是 `BattleManager._ResolveXxx_d__NNN__MoveNext.c`（单下划线 + `_d__`），两种命名都要试。

**下一批建议**：账行 169 起仍是 `Add*` 家族（`AddResetDuty` / `AddResignAction` / `AddRevealOrders` / `AddScriptedAttack` / `AddScriptedForceAttack` / …），规律 1/2/3 可直接套；但**疑点 1（续攻）优先**，它可能是 P0。
