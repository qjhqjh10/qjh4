# B97_RC · `RuleCore.cs` 五件（2026-10-29）

> 执行代理 `B97_RC`（有写权限）· 白名单文件 = `RuleEngine/Core/RuleCore.cs`（＋新建断言 `CardPresentation/Editor/B97_RC_Assert.cs`、账补丁 `账/_parts/B97_RC.tsv`、本报告）
> 判据输入（**已读**）：`判据/B97_可推A七条_审查.md`（审查 #4）· `判据/B97_结构差与休眠_再裁.md`（#30 / #91 / §三·9c / #5 / #62）
> ⛔ 没碰：`Editor/RuleEngineTest.cs` · `EffectResolver*.cs` · `UnitState.cs` · `SimpleAI.cs` · `TutorialScript*.cs` · 账主表 · 正本 · `Pinned*` · `gen_cards_engine.py` · git · Unity 批处理。

---

## 〇 · 一句话

**4/5 落地**（#1 ambush⇒FaceDown · #2 Strike 闸 · #3 目标表次序〔含顺手发现的倒序口〕· #5 回合开始逐条入队）；
**#4 结构性阻塞**（`targetCard` 是手牌 `CardInstance`，而 `ctx.EventTarget` 只收 `UnitState` —— 载体在 `BattleContext`/`EffectResolver`，两个都不在本代理白名单），**写反证交回**。

| # | 活 | 结果 | 改在哪（现读行号） |
|---|---|---|---|
| 1 | `DeployFreeCore` 补 `ambush ⇒ FaceDown` | ✅ **做了** | `RuleCore.cs:4838` |
| 2 | Strike 闸用「待结算量」 | ✅ **做了** | `RuleCore.cs:6816`（+ 注释 6809-6812） |
| 3 | 目标表次序（正序 + 倒序两口） | ✅ **做了** | `:17129` `ValidTargetSlots`（PlayOrder 升序）· `:17173` **新** `AvailableAttackTargetsDesc`（倒序）· `:1500` `HasAnyValidAttackTarget` 转调 |
| 4 | `BroadcastQuestResolved` 传 `targetCard` | ⛔ **阻塞（反证见 §四）** | 未改（`RuleCorePartial_B94B8.cs` 原样） |
| 5 | `OnTurnSetup` 逐条入队 | ✅ **做了** | `:1914` 调用点 · `:17204` **新** `EnqueueTurnSetupEffectRemovals` |

---

## 一 · 逐件

### #1 `ambush ⇒ FaceDown`（账 `战斗逻辑驱动:500`；审查 #4）

- **判据（现读 `.c`）**：
  - `decomp_full/BattleManager._ResolveCardAutoSummoned_d__511__MoveNext.c:163-170` —— state1/2 里
    `if (HasCurrentTrait(card, 0x4b0, 0)) CardScript__SetAmbush(card, 1, 0);`
  - 同形第二处（我另外查到的）：`BattleManager._ResolveSummonUnit_d__510__MoveNext.c:93-96` —— **同一个闸、同一个调用**。
  - 全库 `SetAmbush(…,1,…)` 只有三处：`_ResolvePlayCardFromHand…:197`（= 我们的 `PlayCard`）· `_d__510:95` · `_d__511:169`；
    `ResolveTransformUnit.c:1825` 是第四处（**变身后**那一跳，`TransformUnit_B94B12.cs` 已单独处理）。
    ⇒ **`_d__510` 也有 ⇒ 不能只给 `fromHand: true` 那一档**（审查文只点了 `_d__511`，这里按 `.c` 补齐两档）。
- **改动**（零行数变化，就地扩写 `RuleCore.cs:4838` 那一行）：

  ```csharp
  ps.Board[dst] = unit; unit.SetOwnerSeat(owner); if (deploySource != DeploySource.Reanimate && PlaysFaceDown(card)) { unit.FaceDown = true; ctx.Log(…); }
  ```
- **判据共用一份**：判别式仍只用 `PlaysFaceDown(card)`（全工程唯一，`RuleCore.cs:3812`）—— 与 `PlayCard:4077` 同一句。
- **`Reanimate` 档不判**：判据 = `DeployFreeCore` 里那段长注释列的 `___ResolveTransformFromRemnant_b__0.c:7-31` 回调清单**没有** `SetAmbush`。
- **可观测**：`ruleprobe play check` ①-b 事件电池 **+2 张**，`GSC31` / `GSC52` —— 两张的夹具都摆/改 `GSC5 5/0/0/1 {ambush1, cantattack1}`（见 §三）。

### #2 Strike 闸改「待结算量」（账 `战斗逻辑驱动:476`；再裁 #30 判乙）

- **判据**：`CardScript__ResolveUnitAttacked.c:99-101` 的闸是 `!EnoughPendingDamageToDie(attacker)`（读**已登记的待结算量**，含反击）；
  我们原来用 `DamageAfterReduction(attacker, counterAtk) >= attacker.Health` —— **漏 幸存者 / 堡垒 / 空投舱** 三档。
- **改动**（`RuleCore.cs:6816`，零行数变化）：

  ```csharp
  bool attackerDiesFromPending = !noCounterEarly && counterAtk > 0
                                 && WouldKillByEntries(attacker, new[] { counterAtk });
  ```
  `WouldKillByEntries`（`:7789`）已含 护甲 / 盾 / 闪避 / 无敌 / 幸存者 / 堡垒 / 空投舱 七档，
  是 `EnoughPendingDamageToDieWithDamageValues` 的等价实现（doc 逐句照 `.c`）。
- ⚠️ **同族的另一半没动**（如实记）：`targetWillDieFromThis`（`:6815`）仍用 `DamageAfterReduction(target, atk)`，
  而原版对应的是 `EnoughPendingDamageToDie(target)`。再裁 #30 **只点了 Strike 那一条**，故本批不动 ⇒ 进「待接线 / 待裁」。

### #3 目标表次序（账 `战斗逻辑驱动:1248`；再裁 #91 + §三·9c）

两件事、**两个原版函数、两个轴**，⛔ 不混：

1. **`ValidTargetSlots`（= `RawCardScript.GetValidTargets`）改成进场追加序**
   - 判据：`RawCardScript__GetValidTargets.c:26-45` 遍历 `mgr+0x470`（`unitsInPlay`，**进场追加序**）。
   - 实现：先把敌方非空格按 `UnitState.PlayOrder` 做稳定排序（`StableSortByPlayOrder`，没取号的排末尾），再逐格 `IsValidTarget`。
   - 旧注释里那句「⛔ 别改成原版追加序」**已就地改写**（`RuleCore.cs:17113-17119`）+ `:17126` 的 summary 一起改。
2. **新增 `AvailableAttackTargetsDesc`（= `BattleManager.GetAvailableAttackTargets`，【倒序】）** ｜ 🔴 **2026-10-29 订正（`B97_AI` + `B97_RC2` 双路独立反证）**：本句「AI 选靶原版走 `GetAvailableAttackTargets`（倒序）」**不成立** —— ① `GetAvailableAttackTargets`（确为倒序）只有两个调用方：`EndTurnClick.c:78`（**教学箭头**，只取 `[0]`）与 `GetRandomAttackTarget.c:15`（三源 0 调用方 = 死代码）；② AI 的真实轴 = `AI.PlayTurn`/`SelectBestAttackAction` → `GetAvailableActions.c:145-155` → `GetAvailableAttackActions.c:100-190`（`iVar7 = 0; while (iVar7 < count(0x470))` = **正序**），`SelectBestAction` 严格 `>` ⇒ 平手留**先进场**的。⇒ AI 与 berzerk **都是正序**；落点统一走 `RuleCore.StableSortByPlayOrder`。
   - 判据（§三·9c 的顺手发现，本笔现读复核）：`BattleManager__GetAvailableAttackTargets.c:23-33`
     `iVar1 = list.count; do { iVar1--; … get_Item(list, iVar1) … }` ⇒ **从末尾往前**枚举。
   - 与 `ValidTargetSlots` **不是同一个轴** ⇒ 单列一个口，doc 里写死「⛔ 别合并 / 别互相顶替」。
   - **消费者**：`HasAnyValidAttackTarget`（`:1500`）已转调它 —— 只取 `Count` ⇒ **结果集与次序无关、逐字节不变**
     （顺带让这个新口不是死代码；它的 doc 本来就写着「原版 `GetAvailableAttackTargets` 非空」）。

### #4 `BroadcastQuestResolved` 传 `targetCard` —— ⛔ **阻塞，交回反证**

- 判据（再裁 #5 判乙）：原版 `CardScript__RemoveEffect.c:240-247` → `BroadcastQuestResolved` →
  手牌跳把 **`questCard` 当 `targetCard`** 传进 `OnTrigger(490, …)`。
- 现状：`RuleCorePartial_B94B8.cs:98` 调 `RunAbilityOfAnotherCard(…, targetCard: null)`。
- **为什么做不了（不是「不做」）**：
  1. `RunAbilityOfAnotherCard` 的 `targetCard` 形参类型是 **`UnitState`**（`EffectResolver.cs:18046`）；
  2. 它把值种进 `ctx.EventTarget`（`EventSlotsScope.TargetCardAndActing`，`EffectResolver.cs:9705`），
     而 **`BattleContext.EventTarget` 的声明类型就是 `UnitState`**（`BattleContext.cs:1169`）；
  3. 手牌里的那张任务卡是 **`CardInstance`**，它**没有 `UnitState`**（`UnitState` 只由入场部署构造）。
  ⇒ 要忠实传过去，必须**新增一个「非单位目标」载体**（`BattleContext` 加字段 + `EventSlotsScope` 开户 + 所有消费 `EventTarget` 的地方判空）
  —— 这三个文件（`BattleContext.cs` / `EffectResolver*.cs`）**都不在本代理白名单**，而且会牵动整条触发链。
- **零可观测**：池内 `spellType == 120` = **0/1129**（再裁 #5 已核）⇒ 今天改了也看不见。
- **建议**：单开一条「非单位 `targetCard` 载体」的件（`EffectResolver` 写手 + `BattleContext` 所有者同批），
  或者按 `B97_STRUCT30` §一 #5 的口径先把「载体不存在」记进账的差异列。⛔ **别拿 `UnitState` 硬顶**（原版宾语是那张卡，不是某个单位）。

### #5 `OnTurnSetup` 逐条入队（账 `战斗逻辑驱动:816`；再裁 #62 判乙）

- **判据**（`CardScript__OnTurnSetup.c:21-102` 现读全文）：正序扫 `activeEffects(+0x108)` ——
  `+0x33` 且「施放者 == 本回合方」⇒ `SendRemoveEffect(effect, globalVars + 0xb8)`；
  `+0x34` 且「本回合方 != 施放者」⇒ 同上；`+0x35` ⇒ `+0x35=0 / +0x32=1`（转回合末，那一档已由 `due` 等价）。
- **`globalVars + 0xb8` = `instantPriority`** ⇒ lane = `ActionPriority.Instant`
  （判据 = `ActionPump.cs:1770` 那张五道优先级表 `+0xb4/+0xb8/+0xbc/+0xc0/+0xc4`）。
- **改动**：`BeginTurnCore` 那次整批 `RevertBuffs(false, ctx.Active)` → `EnqueueTurnSetupEffectRemovals(ctx, ctx.Active)`（`:1914`）；
  新方法在类尾（`:17204`），形状与 `EnqueueEndOfTurnEffectRemovals`（账 722 的回合末版）**同一条修法**：
  每条到期 effect **各入队一条 `actionType: 16`**，撤除规则仍只有 `UnitState.RemoveTempBuff` 一份。
- ⚠️ **到期谓词逐字复制**了 `UnitState.RevertBuffs(atTurnEnd:false, player)` 的表达式（`UnitState.cs:1700`）——
  那个文件本轮不在白名单 ⇒ **没能收成一处**，代码注释里把「改任一侧都要同步另一侧」写死了（待接线 ④）。

---

## 二 · 行数变化（🔴 零漂移证据）

| 项 | 值 |
|---|---|
| `RuleCore.cs` 行数 | **17,149 → 17,241（+92）** |
| 第一处**行数**变化（旧行号） | **`17139`** —— 即该文件**最后一个方法体内**（`ValidTargetSlots` 的循环，7 行 → 11 行，+4） |
| 新增两个方法 | `+88` 行（追加在类尾，`ValidTargetSlots` **之后**，后面只有 `}` / `}`） |
| `17138` 行之前 | **逐行计数不变**（只就地改写内容，见下） |
| 就地改写（计数守恒）的行 | `1496-1500`（`HasAnyValidAttackTarget`）· `1914-1921`＋`1925`（回合开始）· `4838`（`DeployFreeCore`）· `6809-6812`＋`6816`（Strike）· `17113-17119`＋`17126`（`ValidTargetSlots` doc） |
| 账落点影响 | **零** —— 全账对 `RuleCore.cs` 的落点里**最高的一个是 `17119`**（账 1248），而 17119 **计数不变**、且内容仍是那一节；`check_ledger_points.py` 现跑输出里 **`RuleCore.cs` 命中 0 行**（无漂移/无失败） |

> ⚠️ 逐行比对里另有一处与 `HEAD` 不同但**不是本代理改的**：`RuleCore.cs:9881`
> （`PlayOrder = u.PlayOrder, DeployedTurn = u.DeployedTurn, …` 后面多了 `Atta…`）—— 工作树里本来就有的别人未提交改动。

---

## 三 · `play all dump` 差异表（重钉基线的输入）

- 命令：`bash 工具/ruleprobe.sh play all dump d:/4/Unity/_tmp_view/B97_RC_dump2.tsv`（rc=0；**收尾把 `AvailableAttackTargetsDesc` 改成转调正序口之后又复跑了一遍，数字完全相同**）
- 对拍基线：`_tmp_view/repin_1029/play_after_remfix.tsv`（**按列名**逐卡比，25 列 × 1129 行）
- **结果：`changed cells = 0`**（表头相同、行数相同 1130）⇒ **①-a 主档（打出去那一拍）逐卡零变化**。
- 但 `play check` 的**事件电池（①-b）**变了 ⇒ 我又跑了一遍 `play check` 把两半分开看：

| 半 | 基线（`playcheck_after_remfix.txt` @12:37） | 当次（`_tmp_view/B97_RC_playcheck2.txt` @13:16） | 判 |
|---|---|---|---|
| ①-a 主档 | 28 张卡变了（vs **钉住的** `out_play_baseline.txt`） | **28 张，同一批，逐条文案相同** | **本批零影响** |
| ①-b 事件电池 | 73 张卡变了 | **75 张** | **+2 张 = 本批 #1 的可观测面** |
| 结算指纹 | `99db8b32…`（当次现算） | **`e04907bc…`** | 变了（因 ①-b +2） |
| vs 钉住基线 | `228f4304…`（两边都不同） | `228f4304…`（同样不同） | 本批**没碰** `PinnedPlayMd5` |

**+2 张的逐卡（这是 #1 唯一可观测面）**：

| 卡 | 变化列 | 变化 |
|---|---|---|
| `GSC31` | `raw 52→54` · `net 8→23` · `netDmg 0→-7` | 夹具里 `GSC5 {ambush1, cantattack1}` 落场后**现在面朝下** ⇒ 挨伤/出手那一段的离场与伤害累计跟着变（`✎ 改动 P1[4] WARLORD 2/0/0/26 → 24`） |
| `GSC52` | `raw 42→45` · `net 28→39` · `netDmg 0→8` · `steps` 里 `mate=0M/5R@5→3` → `0M/0R@5→5` | 同一个 `GSC5`：面朝下期间它不再是「可被指定/可反击」的那一张 ⇒ 那一拍的攻击者/靶换了 |

> **#2 / #3 / #5 零可观测**（与预期一致，逐条理由）：
> · #2 —— sweep 里没有「带 survivor/bastion/dropPod 且会被反击打死」的攻击者；
> · #3 —— `ValidTargetSlots` 在今天唯一的消费者里只取 `Count`（次序不影响结果集），`ResolveBerzerkAttacks` 还没转调；
> · #5 —— 能看见这一拍的只有 155/160 两族，池内都 0/1129。

---

## 四 · 断言

- 新文件：`CardPresentation/Editor/B97_RC_Assert.cs` —— `public static partial class RuleEngineTest`（与 `RuleEngineTest_EngineB95WBD.cs` 同一种形状；全局命名空间，无 asmdef 分隔）。
- 入口：**`CheckB97RCGaps()`**（不以 `Test`/`Report` 打头 ⇒ 不会被 `RunCoverageCompiledCases()` 要求 `Step`）。
- 🔴 **建议插在**：`Editor/RuleEngineTest.cs` 里 **`Step(CheckB93G1PlayedCardTail);` 那一行之后** ⇒ `Step(CheckB97RCGaps);`
- 覆盖 4 件（#4 无可断言物），每条带「前提」与 `🧨`：
  - A 三档：`fromHand:true`+ambush ⇒ FaceDown / 无 ambush ⇒ 不面朝下 / `Reanimate` ⇒ 不面朝下；
  - B 判别式 = 幸存者：`DamageAfterReduction >= Health` 为真而 `WouldKillByEntries` 为假 ⇒ 两条一起断；
    外加一条**结构断言**（源码里有 `WouldKillByEntries(attacker, new[] { counterAtk })`）；
  - C 判别式 = **格号序 ≠ 进场序**（先摆 8 号格再摆 2 号格）：`ValidTargetSlots` ⇒ `[8,2]`、`AvailableAttackTargetsDesc` ⇒ `[2,8]`；
    ⚠️ 敌人 4 号格上**永远是督军**（`PlayerState.cs:68`）⇒ 夹具把攻击者标成「本回合刚上场」
    （`atk.SummonSickness = true`），走原版那道「召唤病 ⇒ 打不了敌方督军」的闸（`RuleCore.cs:5622`）把它挡掉，
    否则结果集是 3 个、次序断言失真（另有一条前提断言专门盯「4 号格上确实是督军」）；
  - D 四档 buff（`+0x33` / `+0x34` / 不到期 / 回合末）⇒ 入队 2 条、且到期那条**当场仍在**（= 入队而非整批撤）。

---

## 五 · typecheck / 闸

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_rc bash 工具/typecheck.sh` | 本代理改完后**运行时 0 / 编辑器 0**（含新建的 `B97_RC_Assert.cs`）。⚠️ 收尾时再跑一次读到 **2 条**，全部落在 `CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs`（`WireScoreBar` / `_scoreLines`）= **别的代理正在写**，`RuleCore.cs` 与 `B97_RC_Assert.cs` **0 条**（按铁律 13·3：错全集中在别人的文件 ⇒ 不是本批的问题，⛔ 不去改别人的文件） |
| `python 工具/check_ledger_points.py` | 输出里 **`RuleCore.cs` 命中 0 行** ⇒ 本批**零漂移**（全表仍是别人在飞的 239 漂移 / 15 失败，与本批无关） |
| `ruleprobe play check` | rc=1 —— 但 ①-a 与 12:37 那次**逐条相同**（28 张，同一批），差额**全部来自 ①-b 的 +2 张**（见 §三） |

---

## 六 · 账补丁

`资料/完全复刻/账/_parts/B97_RC.tsv` —— **11 列 + 首行表头**，4 行，`域 = 战斗逻辑驱动`：

| 主表物理行 | 原版类.方法 | 改了哪几列 |
|---|---|---|
| `:476` | `BattleManager.RecordAttackPendingDamage(…)` | 6 落点 / 7 差异 / 10 备注 |
| `:500` | `BattleManager.ResolveCardAutoSummoned(BattleAction battleAction)` | 4 判据要点 / 6 / 7 / 10 |
| `:816` | `CardScript.OnTurnSetup(bool playerTurn, bool isInHand)` | 6 / 7 / 10 |
| `:1248` | `RawCardScript.GetValidTargets(BattleManager manager, CardScript thisCard)` | 6 / 7 / 10 |

- ⛔ **状态列一律没动**（B 仍是 B —— 升 A 是审查/调度台的裁定，不是写手的）。
- 已做**逐列往返比对**：读回补丁 → 与主表逐格比 ⇒ 除上表列外 **其余 7 列逐字节相同**；无裸 CR、无 tab/换行注入。
- 合并请用 `merge_ledger_parts.py --only B97_RC`（⛔ 别整目录合并）。

---

## 七 · 待接线 / 没查清

**待接线（有明确消费者）**
1. 🔴 `ResolveBerzerkAttacks` 的选靶（`sc > bestScore` 平手提第一个）**仍按格号序** —— 账 1248 因此仍 B；
   转调 `ValidTargetSlots` 后平手判定会从「格号最小」变成「进场最早」⇒ **会动 `play all dump`**，要单独一批 + 重钉。
2. `RuleEngine/Data/SimpleAI` 的 AI 选靶（原版走 `GetAvailableAttackTargets`，**倒序**）**未转** ｜ 🔴 **2026-10-29 订正（`B97_AI` + `B97_RC2` 双路独立反证）**：本句「AI 选靶原版走 `GetAvailableAttackTargets`（倒序）」**不成立** —— ① `GetAvailableAttackTargets`（确为倒序）只有两个调用方：`EndTurnClick.c:78`（**教学箭头**，只取 `[0]`）与 `GetRandomAttackTarget.c:15`（三源 0 调用方 = 死代码）；② AI 的真实轴 = `AI.PlayTurn`/`SelectBestAttackAction` → `GetAvailableActions.c:145-155` → `GetAvailableAttackActions.c:100-190`（`iVar7 = 0; while (iVar7 < count(0x470))` = **正序**），`SelectBestAction` 严格 `>` ⇒ 平手留**先进场**的。⇒ AI 与 berzerk **都是正序**；落点统一走 `RuleCore.StableSortByPlayOrder`。
   `AvailableAttackTargetsDesc`（`SimpleAI.cs` 本轮不在白名单；转的时候照抄倒序，⛔ 别换成正序口）。
3. `targetWillDieFromThis`（`RuleCore.cs:6813`）仍是 `DamageAfterReduction`，原版对应 `EnoughPendingDamageToDie(target)` —— 需裁定（再裁 #30 只点了 Strike）。
4. `UnitState.RevertBuffs(atTurnEnd:false,…)` 的 due 表达式与 `RuleCore.EnqueueTurnSetupEffectRemovals` **各一份**（`UnitState.cs` 不在白名单）⇒ 建议下一个 `UnitState` 写手把谓词提成 `TempBuff.IsDueAtTurnStart(player)` 并两处共用（铁律 6）。
5. 断言宿主接线：`RuleEngineTest.cs` 加一行 `Step(CheckB97RCGaps);`（位置见 §四）。

**没查清（⛔ 不假装）**
a. `PlaysFaceDown(card)`（**卡面**关键词）vs 原版 `HasCurrentTrait`（**活跃实例**，含在手牌时被 `SetupCardInHand` 挂上的效果）——
   我按 `PlayCard` 的既有口径用了 `PlaysFaceDown`（判别式只有一份）。**「手牌加成能不能给出 `ambush`」没扫池** ⇒ 若有这种卡，两档会分叉。
b. `WouldKillByEntries` 与旧的 `DamageAfterReduction` 在 **易伤(vulnerable) + 护甲** 组合上是否严格同值：我按「单条 entry、index 0」传入，
   两者内部对易伤的位置判据不同（`WouldKillByEntries` 里易伤那一步在 `RuleCore.cs:7846-7852`，排在「第一层清零」之后）⇒ **结构上按原版口径是对的**，但没逐格对拍。
c. `#4` 的「非单位 `targetCard` 载体」到底该长什么样（`CardInstance` 槽？还是把 `EventTarget` 拆成 unit/card 两格）—— 需 `BattleContext` 所有者定，本代理只做了阻塞证明。
d. 新增 `AvailableAttackTargetsDesc` 的 `ranged` 由**调用方**算（原版 `.c:44` 传 `attType = 0`，函数内部换成当前攻击型）；
   今天唯一的调用方按该口径算好了，**将来的调用方必须照做**（doc 里已写）。
