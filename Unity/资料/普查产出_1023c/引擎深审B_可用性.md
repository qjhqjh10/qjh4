# 引擎深审 B · 可用性（只读审查 · 2026-10-23）

> **范围**：`Unity/MyGame/Assets/RuleEngine/**`（61 个 `.cs` · 10.8 万行，其中 Core+Data ≈ 6 万行）
> ＋ 相关联的记账口 `CardPresentation/Battle/{BattleDriver,ReplayStore}.cs`、`CardPresentation/Net/NetApply.cs`。
> **方法**：静态阅读 + `grep` 普查；**没有跑 Unity / player / `-executeMethod` / `ruleprobe`**（红线：后台有批处理）。
> **判据**：`CLAUDE.md` §三 三条红线（不许静默失败 / 对局必须可复现 / 两处写同一条规则迟早不一致）
> 和 `ReplayStore.cs` 文件头的回放契约（起始条件 + 动作流，三个记账口）。
> **读数（引用）**：`RuleEngineTest 6761/6761` · `BattleScene 4574/0`。
> ⚠️ 本报告**没有改任何文件**，也没动 git。

---

## 〇 一句话结论

**能拿来打完一整局** —— 对局状态机收敛（泵点硬闸 4096 · AI 拒绝上限 8 · 疲劳必然终止）、
棋盘不变量与手牌上限都只有**一处实现**、Core/Data 里**零 `UnityEngine.Random` / 零 `DateTime` / 零并发**；
但**「无静默失败」不成立**（效果不结算只进战斗日志、面板缺答引擎静默代挑）、
**回放的「自证」口径偏弱且跨进程不可信**（`DeepHash` 用 `string.GetHashCode()`、`StateHash` 维度不全），
**AI 的手牌上限读的是经典常量而不是本局 `ctx.Vars`**。这三类都不是「打不完」，是「打完了你未必知道哪里不对」。

---

## 一 确定性 / 可复现

### 1·1 已核通过的（正项，别再重复查）

| 项 | 结论 | 证据 |
|---|---|---|
| 引擎随机源 | **只有 `System.Random`**，Core/Data 全文 `grep UnityEngine.Random` = 0 个调用点（只有注释） | `Core/BattleContext.cs:598-603` |
| 三路互不干扰 | `Rng` / `ShowRng(seed^0x5EED5EED)` / `AiRng(seed^0x0A1A1A1A)` 同种子派生、互不挪动序列 | 同上 |
| AI 不吃对局随机流 | `Data/**` 全文 `grep ctx.Rng` = 0（只有一句注释）；AI 只吃 `ctx.AiRng` | `Data/SimpleAI.cs:1283-1294` |
| 洗牌 | Fisher–Yates，显式传 `rng` | `Core/RuleCore.cs:1066-1073` |
| 时间/并发/Guid | Core/Data 无 `DateTime`、无 `Thread/Task/lock`；`Guid` 只在 `Data/DeckLibrary.cs:498`（卡组命名，非对局状态） | grep |
| 字典/HashSet 遍历序 | **不参与任何判据**：`_triggerOps`/`_triggerPriority` 只 `TryGetValue`；`_abilityTriggers`/`DyingThisAction` 只 `Contains`；`_createdBy`/`_keywordsPact` 只查表 | `Core/CardDef.cs:1266-1300/1742`、`Core/BattleContext.cs:681`、`Core/EffectResolver.cs:9192/9401` |
| 前缀匹配顺序 | `Prefixes` 是**有序数组**不是字典，先到先得稳定 | `Core/CardDef.cs:5611` |
| 排序 | 全部带显式比较器（`CompareOrdinal` / `RecordId` / 分数）；`List.Sort` 不稳定但**同输入确定** | `RuleCore.cs:842/416/11257`、`Data/DeckBuilder.cs:369`、`Data/AiTargets.cs:663-675` |
| 实例号 | 计数器挂在 `ctx` 上（不是静态），同种子同次序 | `Core/BattleContext.cs:611/648`、`Core/CardInstance.cs:48` |
| 浮点累积 | 引擎状态全是 `int`；`float` 只在 AI 打分与动画时序（`CardDef` 的 `AnimTime`） | grep |

### 1·2 问题

| # | 问题 | 位置 | 触发条件 | 玩家会看到什么 | 严重度 |
|---|---|---|---|---|---|
| **D1** | **回放轨迹哈希用 `string.GetHashCode()`** —— 与项目自己的规则正面冲突（`EffectResolver.cs:9767` 白纸黑字「**不能用 `string.GetHashCode`（.NET 里按进程随机化，同一局面两次跑会不同）**」）。`NetProtocol.Hash()` 就是那份 FNV，但 `DeepHash` 没用它 | `BattleDriver.cs:459-495`（`GetHashCode` 在 `:475/:480/:486`） | **换一个进程**放旧录像（从对局历史点「回放」） | 同一个局面在第 0 条就报 `[Replay] 分叉点 = 第 0 条`（5 行红字），并 **`break` 掉整条动作流**（`:490-520`），于是终局指纹也不等 ⇒ **回放直接失败**（Unity Mono 今天字符串哈希是稳定的，所以自检全绿；换 CoreCLR / 换运行时即现形） | **P1** |
| **D2** | **`StateHash` 维度不全**：只含 `Turn/Active/Winner` + 双方 `Deck/Hand/Discard` 张数 + `Energy/MaxEnergy` + 督军四围 + 手牌≤12 张 + **牌库每 7 张取 1** + 棋盘每格 4 围/疲劳位 | `NetProtocol.cs:462-517`（`PlayerHash:478-516`） | 任何一局 | **明明演错了也可能判「同一局」**：`Faith`/`SpiritStones`/`QuestPoints`/`Fatigue`/`CostMods`/`TempControls`/`HandEffectRecords`/`SecretsPlayed`/附魔 都不进哈希；牌库中间张不同也看不出来 ⇒ 录像「自证」是**弱证据** | **P1** |
| **D3** | **确定性只有 `NewBattle` 一级的断言**（同种子 → 起手/牌库逐张一致 + 换种子不同），**没有**「同 seed + 同动作流跑两遍 ⇒ 终局 `StateHash` 相同」 | `Editor/RuleEngineTest.cs:28070-28103` | 任何改动 | 无直接可见；但 RNG 流错位（例如多抽一次）今天只能靠人手比对发现 | **P2** |
| **D4** | `_keywordsPact` 是**静态字典**，全仓**没有任何 `Clear()`/`Remove()`** | `EffectResolver.cs:9299` | 长会话连打多局 | 不可见：`UnitState` 是键 ⇒ 上一局的单位/卡表**永远不被 GC**（内存随局数线性涨） | **P2** |
| **D5** | 静态去重集「同一原因只喊一次」是**进程级**的 | `ConditionEval.cs:129/517-524`、`EffectResolver.cs:10108-10116` | 同进程第 2 局及以后 | 不可见：第 1 局会 `LogWarning`，之后同一条判不了的条件**再也不出声**（属于「可接受」但要知道） | P3 |
| **D6** | `QuestInsertSeed` 用的是「**首次触发那一刻的局面快照**」（先手座 + 双方督军 id + 牌库/手牌每份 `Id`），不是开局种子 | `EffectResolver.cs:9773-9805` | 任何局 | 不可见：同一动作流可复现 ✔；但**两局不同的对局走到同一局面**会共用同一个插入种子（不是缺陷，但别当成「由开局种子决定」） | P3 |
| **D7** | 新开局的种子来自时钟（`ForceSeed ?? DateTime.Now.Ticks`），只保证「**同 seed 重放**」可复现 | `BattleDriver.cs:3473`；录像头 `RecBegin:550-590` | 每局 | 不可见（符合预期）；⛔ 别把它当「每局都可复现」 | P3 |

**结论（确定性问题）**：同进程、同 seed、同动作流 ⇒ **逐字节可复现**（引擎侧没有漏网的随机/时间/遍历序）。
跨进程的唯一破口是 D1；回放判据的强度问题是 D2。

---

## 二 边界与不变量

### 2·1 已核通过的

| 不变量 | 结论 | 证据 |
|---|---|---|
| 棋盘「连续无洞」 | 增删只有 `Insert`/`RemoveAt` 两个口；`Resolve` 满侧换对侧、有洞时**不覆盖不推人**；洞只可能由夹具造，且 `WarnHoley` 出声 | `Core/BoardSlots.cs:144/307/362/280` |
| 手牌上限 | **唯一实现** `AddCardToHandWithinLimit`：先判上限再 `Add`，满了把**刚进手牌那张**送弃牌堆并打日志 | `RuleCore.cs:2812-2828` |
| 牌库空 | `ps.Fatigue++` → 督军挨伤害 → `CheckWinner` → 返回 `null`；**不洗回**（用户 2026-10-20 拍板） | `RuleCore.cs:2737-2759` |
| 满场 | 出牌 `CanPlayCard → BoardSlots.HasRoomFor` → `ErrNotEnoughRoom`；效果召唤满场打日志后 `return false` | `RuleCore.cs:2982`、`RuleCore.cs:4153-4166` |
| 无合法目标 | `DoDeal` 显式打「没有合法目标，空过」并 `return true`（正常空过、非失败）；`DoDestroy/DoBlind` 也会打「0 个目标」 | `EffectResolver.cs:2260-2268/3279/3441` |
| 自我指向 | 攻击 `tgtP == p ⇒ ErrSelf`；治疗自己按 `MaxHealth` 封顶、`healed<=0` 不发事件 | `RuleCore.cs:4712`、`HealUnit` |
| 0 血 / 负血 | `IsAlive = Health > 0`；督军**留在 4 号格**、`IsDefeated = Health <= 0`；`CleanupDeaths` 对督军早退，胜负交 `CheckWinner`（入口各自调） | `UnitState.cs:1256`、`PlayerState.cs:164`、`RuleCore.cs:8524-8528` |
| 光环重算钩子 | 全仓 `Board[..] =` 的**引擎写入点逐处有** `Auras.Recompose`（或由死亡记账队列体兜底）；`RemoveAt` 7 处亦然 | grep：`Auras.Recompose` 14 个调用点；`EffectResolver.cs:5541/6965/11439`、`RuleCore.cs:2191/4006/4207/8557/8945` |
| 动作泵上限 | 4096 条触发环硬闸 → `LogError` + 清空四道 | `ActionPump.cs:58/668-675` |

### 2·2 问题

| # | 问题 | 位置 | 触发条件 | 玩家会看到什么 | 严重度 |
|---|---|---|---|---|---|
| **B1** | **手牌上限两个来源**：引擎结算读 `ctx.Vars.handLimit`，AI 打分读 `RuleCore.HandMax`（**写死经典 10**）。遭遇局实际上限 = 8 | `RuleCore.cs:98`；`Data/AiScoring.cs:262/375`；`GameplayVariables.cs:372` | 遭遇（Skirmish）局 | 不可直接看见：AI 会**高估抽牌价值到 10 张**（只影响 AI 决策质量） | **P2** |
| **B2** | `Hurt` **不挡非正数伤害**：`amount <= 0` 一路走到 `u.Health -= actual` ⇒ 负数「伤害」变成**回血**（而且不发 `Heal` 事件）；配套 `Attack += v` 也不夹 0 | `RuleCore.cs:7902/7919/7874`；`UnitState.cs:1008-1009` | 只有卡数据/效果链路算出**负伤害或负攻击**时 | 不可见：目标血会**变多**，战报写「造成 -2 点伤害」 | **P2**（正规攻击路径有 `FieldAttack<=0` 闸 `RuleCore.cs:5024` 与反击 `counterAtk>0` 闸 `:6201`，所以**可达性未证**） |
| **B3** | `ApplyDamage` 是 **public 入口**但不判胜负（契约靠调用方） | `RuleCore.cs:7441` | 新写一个直接调它的伤害点 | 打死督军后 `Winner` 仍是 0 ⇒ **对局不结束**（今天的调用方都自己补了 `CheckWinner`） | **P2** |
| **B4** | `PlayCardCore` 注释断言「`HasRoomFor` 已判过 ⇒ `Insert` 不会失败」；真返回 -1 时 unit **不在盘上**，但 `TroopsPlayed++` / `Deploy` 事件（Slot=-1）/ 手牌已移除都照做 | `RuleCore.cs:3427-3434` | 只在**夹具造洞 + 该侧满**时可能 | 卡「凭空消失」 | P3（`CanPlayCard` 与 `Insert` 今天同判据，生产不可达） |
| **B5** | 疲劳分支的 `EmitHit`（会**入队**监听器）发生在 **`ActionScope` 之外**（`MoveDeckTopToHand` 在 `using` 之前） | `RuleCore.cs:2630-2636` vs `:2747-2755` | 牌库空 + 有「督军受伤时…」类监听器 | 不可见/未查清：监听器被推到**下一个** scope 入口才结算（`ActionScope` 构造时先泵一次），可能与同回合后续抽牌/能量结算的先后错开 | P3（**未查清**是否可观测） |
| **B6** | `EnforceHandLimit` **已无任何调用方**（抽牌/造牌都改走先判上限那条） | `RuleCore.cs:2886-2910` | —— | 无（死代码；但文档还说它「兜底」，会误导下一个人接回去） | P3 |

---

## 三 静默失败

> 本仓的「不静默」标准 = **写进 `ctx.Log` / `Debug.Log*`**。下面逐条标注**玩家实际能不能看到** ——
> 战斗日志在 HUD 里可翻，但「牌已经打出去、费用已经扣了、局面没变」这条主信息**没有任何即时 UI 反馈**。

| # | 路径 | 位置 | 触发条件 | 玩家会看到什么 | 严重度 |
|---|---|---|---|---|---|
| **S1** | 未实现动词：记日志 + `unresolved`，**卡照打、费照扣** | `EffectResolver.cs:906-911` | 卡表里出现 `EffectDispatch` 没有的动词 | 牌打出去了，什么都没发生；只有战斗日志一行「本版还没实现」 | **P1**（可见性） |
| **S2** | `ReportUnresolved` 只写 `ctx.Log`；而 `Events` 上限 2000，**最旧的会被截掉** | `EffectResolver.cs:5962-5967`、`BattleContext.cs:561/1661` | 一局日志超过 2000 行 | 同上，且**过几回合就翻不到了** | **P2** |
| **S3** | 条件判不了（`ConditionEval` 返回 `null`）⇒ **整条效果不结算** + 日志 | `EffectResolver.cs:888-897` | 结构化条件命中「本版没判据」那一档 | 效果整条没发生 | **P2** |
| **S4** | 面板没答 ⇒ `TakePick` 直接 `ctx.Rng.Next(count)` **等概率挑**，当场**不打日志**（只有越界才打） | `EffectResolver.cs:3455-3479` | 选牌/三选一面板没弹/被关/队列没喂上 | 引擎替玩家随机选了一项；事后由驱动层补一条汇总（`BattleDriver.cs:10957-10967`）——**仍是日志** | **P2** |
| **S5** | `OwnerOf` 找不到返回 `-1`（不报） | `EffectResolver.cs:8418-8426` | 击杀记账时目标已经不在盘上 | 击杀/阵营极性静默不记（`TryCreditKill` 少一笔） | P3 |
| **S6** | `ResolveEndTurnEnchantment` **没有 `BeginAction`**（它会走 `ResolveOps`） | `RuleCore.cs:2487-2520` | 战役局（今天 `CampaignStage` **恒 null** ⇒ 不可达） | 若将来接战役：队列残留、触发不结算 | P3 |
| **S7** | **死亡记账**入队 `EnqueueAbility` 的**返回值没检查** ⇒ 失败则死者**不进弃牌堆/`DeadUnits`、也不 `Auras.Recompose`** | `RuleCore.cs:8862-8875` | `Enqueue` 拒绝（今天只有非法 lane / null，且两条都 `LogError`） | 卡留在棋盘幽灵位、光环不收 | P3（今天不可达，但形状是吞错） |
| **S8** | 卡组串导入 `catch { return null; }`（不报原因） | `Data/DeckLibrary.cs:391` | 玩家粘贴非法串 | UI 只能显示「导入失败」；`LastDroppedIds` 记丢掉的单卡 | P3 |
| **S9** | 解析层坏输入**静默跳过**（`KeywordTable.Parse` 自陈） | `CardDef.cs:5944-5960` | 卡表脏词 | 有留痕：构造函数走 `NoteDropped`；卡面 `*` 走 `UnimplementedNotes` | P3 |

**另外两条「不是静默、但只到日志」的设计口径**（不算缺陷，列出来免得下一个人当漏洞改）：
`DoNoEffect`（卡面就是 `Does nothing`）· `costwhen`/`whenclause`/`costifcontrol` 三个**标记 op**（机制在别层）。

---

## 四 公开入口契约

### 4·1 `ActionScope` 入口（实测 **17 处**，不是 14）

`BeginTurn`(1481) · `EndTurn`(2042) · `Draw`(2633) · `PlayCard`(3353) · `DeployFree`(4132/4292) ·
`DeclareAttack`(5523) · `TryCancelAttackByOwnAboutToAttack`(7024) · `ApplyDamage`(7444) · `Hurt`(7914) ·
`UseAbility`(9772) · `UseOathAbility`(9960) · `UseFaithAbility`(10065) · `UseAlternative`(10170) ·
`CollectWaystone`(10456) · `PlayTactic`(EffectResolver:7420) · `GrantDarkPact`(EffectResolver:9159)。
（`Core/RuleCore.cs` / `Core/EffectResolver.cs`）

**未包 `ActionScope` 的公开变更入口**：`Mulligan`(393) · `EndMulligan`(446) · `ChooseOffensiveCard`(469) ·
`ChooseDefensiveCard`(484) · `SetDefensiveCard`(502) · `Forfeit`(11606) · `CheckWinner`(11716) ·
`ResolveEndTurnEnchantment`(2493) · `GrantSkipCardDraw`(11068)。
逐条核过：**前七个都不入队**（只改状态/手牌/日志）⇒ 不包是可接受的；`ResolveEndTurnEnchantment` 会入队但不可达（S6）；
`GrantSkipCardDraw` 只挂一条手牌记录、不入队。

### 4·2 录像记账口（契约 = 起始条件 + 动作流）

| 口 | 位置 | 覆盖 |
|---|---|---|
| ① 玩家动作 | `LocalAct` `BattleDriver.cs:276-291` | 出牌 / 技能族 / 攻击 / 收集灵魂石（落地成功才记） |
| ①′ 玩家结束回合 | `BattleDriver.cs:12677-12691` | 单独记一条（不走 `LocalAct`，已文档化） |
| ①″ 换牌 / 进攻卡 / 防御卡 / 投降 | `RecRaw` 调用点 `:379/410/6376/6427/6429/6930/9579` | 伪动作（`NetActionKind` + `RecKindForfeit`） |
| ② AI 动作 | `SimpleAI.Executed` `Data/SimpleAI.cs:1439-1450` → `OnAiExecuted:14866` | 只记 `ok == true` |
| ②′ 回合推进 | `EndTurnAndAdvance` `BattleDriver.cs:14849-14862` | 唯一一处（两个调用点共用） |
| ③ 教程脚本 | `TutorialScript.Executed` → `OnTutorialScriptExecuted:14575` | 含两条伪 kind（201/202） |
| ④ 权威流（联机对面那一半） | `RecMsg` `BattleDriver.cs:635-670` ← `ApplyLoggedAction` | `preApplied` 防双记 |

**问题**：

| # | 问题 | 位置 | 触发条件 | 玩家会看到什么 | 严重度 |
|---|---|---|---|---|---|
| **E1** | **AI 的进攻卡/防御卡挑选吃 `ctx.Rng`**（`Ctx.Rng.Next`），而回放侧 `NetApply` 落这两条**不掷骰** | 录制 `BattleDriver.cs:6966-6976/6981-7002`；回放 `NetApply.cs:48-63` | `offensiveCardEnabled` / `defensiveCardPanelEnabled` **被打开**时（`BattleScene.cs:23976-23984` 自检会开） | 回放**必然整条 RNG 流错位**（与 `SimpleAI.cs:103` 记过的坑同族）⇒ 分叉 + 终局指纹不等 | **P1**（今天两个字段默认 `false`：`BattleDriver.cs:6556/6583` ⇒ 生产不可达，是**埋着的雷**） |
| **E2** | 自检夹具直写 `Board[x] = new UnitState(...)` 造出的局面**不可能由动作流复现** | `BattleScene.cs:26170-26180`（文档化） | 拿这种「局」的录像去放 | 分叉（这是**正确行为**，已有负例断言） | P3（不是缺陷，但说明「录像只录动作流」的边界） |
| **E3** | `SetCurrentAttackType` 不单独记账 | `BattleDriver.cs:12740` | 玩家先换打法再攻击 | 无：攻击动作自带 `ranged`（`NetApply.cs:95`）⇒ 结果可复现；`UnitState.CurrentAttackType` 本身不进回放（只影响表现/AI） | P3 |
| **E4** | 回放 verdict 只看 `StateHash`（弱，见 D2）；`trace` 只在**同进程**可信（D1） | `BattleDriver.cs:459-540`、`NetProtocol.cs:462` | 每次回放 | 见 D1/D2 | （并入 D1/D2） |

---

## 五 覆盖盲区（本轮重点）

**结论：6761 条断言是「按人想到的局面手写」的，不是全池差分 —— 顺序类与数据兜底类回归天然漏网。**
本会话的两条真回归就是标本（判据 → `资料/普查产出_1023c/play基线归因签字.md` §〇/§二）：

| 盲区 | 为什么全绿也抓不到 | 实证 |
|---|---|---|
| **① 终态断言对「次序」无感** | `SW5 Hunting Wolf` 的回归是「狂暴正文 vs 洗回牌库**谁先**」：改前正文被取消、但**终态**（牌在牌库 + 对手少血？没有）看上去「差不多」。事后才补了「命中下标 < 离场下标」的**次序判别式** | `RuleEngineTest.cs:12976-13026`（这段注释自陈改前静默丢正文） |
| **② 数据行 × 代码兜底的组合没结构断言** | `targetdies/targetsurvives` 在「卡上没有对应行」时落进「零候选 ⇒ 无条件 ⇒ `holds=true`」；当时断言只断「条件认出来了」，没断「没有行时必须退回正文」 | `RuleEngineTest.cs:9440`；修法见 `引擎_R1R2修复_执行报告.md` §一 |
| **③ 广覆盖不在常规套件里** | 全池**结算层**对拍只有 `ruleprobe play check`（1121 张逐卡真打 + 基线指纹），而 `_run_8_checks.sh` **只有 12 个 Unity 入口**，不含它 | `工具/_run_8_checks.sh`；`工具/ruleprobe.sh` 头注释 |
| **④ 基线可被「签字」吸收** | `play check` 的期望值要人工重签；476 行里已有「🟡 不是缺陷但夹具覆盖不到」（`BL64`）与 **6 行未查清**（`GOF67/SW8/SW9/SW11/TL16`）⇒ 真回归可以被签成「预期变化」 | `play基线归因签字.md` §〇/§1·1 |
| **⑤ 回放自证弱** | 见 D1/D2/E4：`StateHash` 漏维度 + `DeepHash` 跨进程不可靠 + 只在同进程自检 | 同上 |
| **⑥ 确定性只验开局** | `TestDeterminism` 只比 `NewBattle` 的手牌/牌库，不比整局 | `RuleEngineTest.cs:28070-28103` |
| **⑦ 断言盯我们自己的常量** | 项目自己记过「一条老断言把错口径钉死」（督军/`unit` 那次）；这次 `targetdies` 同形 | `CLAUDE.md` §一·7 的追加段 |
| **⑧ 跨模式参数无断言** | `HandMax` 读经典常量、AI 在遭遇局用错（B1）——没有任何断言盯「AI 用的上限 == 本局 `Vars.handLimit`」 | 本报告 B1 |

**建议补的三类结构性断言**（都是「不可能被两边一起改回去」的形状）：
① 每个 `ConditionSpec` 种类 × 每张卡：**要么有数据行、要么走显式降级**（禁止「零候选 ⇒ 恒真」）；
② 每条会入队的死亡/离场记账：**入队返回值 + 出队必达**（`ActionQueue` 计数前后差）；
③ 同 seed + 同动作流跑两遍：**终局 `StateHash` + 每动作 `DeepHash` 全等**（补 D3，同时钉 D1）。

---

## 六 修复清单

### P0（阻塞「打完一整局」）
**无。** 对局收敛、边界闭合、棋盘/手牌不变量都成立。

### P1（会让「你以为验过了，其实没有」）
1. **`DeepHash` 换确定性哈希**：`Card.Id.GetHashCode()` → `NetProtocol.Hash()`（或与 `QuestHashMix` 同款 FNV）。
   位置 `BattleDriver.cs:475/480/486`。同时把**跨进程放旧录像**加进自检（现在只在同进程测）。
2. **`StateHash` 补维度**（`NetProtocol.cs:478-516`）：至少 `Faith/SpiritStones/QuestPoints/Fatigue/MaxEnergy/
   CostMods.Count/TempControls.Count/HandEffectRecords.Count/DeadUnits.Count`；牌库改为**全量滚动哈希**或隔张不动但加长度+首尾。
3. **env-pick 的 `ctx.Rng` 消耗对齐**：`PickOffensiveForAi/PickDefensiveForAi`（`BattleDriver.cs:6966/6981`）
   改吃一条**派生流**（照 `ShowRng`/`AiRng` 的先例），或让 `NetApply` 落那条时**按录制值补掷**；
   ⛔ 别只改一侧（那正是 `SimpleAI.cs:103` 记过的错位坑）。
4. **把 `ruleprobe play check` 纳入常规验收**（至少做成人手清单的一步）；并在文档里禁止
   「有差异 ⇒ 直接重签基线」当默认处置（`play基线归因签字.md` 的口径已经是这样，要落到套件里）。

### P2
5. `AiScoring.cs:262/375` 改读 `ctx.Vars.handLimit`（消灭 B1 的第二份来源）。
6. `Hurt` 加 `amount <= 0` 早退（或按原版：0 点不结算），`GivePayload` 的 attack 值夹 0 —— **先查反编译判据**再改（别猜）。
7. `ApplyDamage` 末尾统一 `CheckWinner`（或把契约写进 XML doc 并加断言）。
8. 战斗日志可见性：`unresolved` / 「引擎替玩家挑」这两类**汇到 HUD 可看点**（今天只在日志）。
9. `_keywordsPact` 加生命周期（开局/结算 Clear，或改 `ConditionalWeakTable`）—— D4。
10. 死亡记账 `EnqueueAbility` 的返回值**检查 + 出声**（`RuleCore.cs:8862`）—— S7。

### P3
11. 删/标死代码 `EnforceHandLimit`（B6）；给 `ResolveEndTurnEnchantment` 补 scope（S6）。
12. `PlayCardCore` 对 `Insert == -1` 加断言/回滚或至少 `LogError`（B4）。
13. 静态去重集（D5）改按 ctx 计数，或在消息里带「本进程第 N 次」。
14. 疲劳分支的 `EmitHit`（B5）挪进 scope，或写清「为什么可以在外面」。

### 没查清的（⛔ 不许当已核实）
- **B5**：疲劳 `EmitHit` 在 scope 外是否可观测 —— 需要一张「督军受伤触发」+ 同回合后续抽牌的真卡组合实测。
- **B2**：负伤害/负攻击的**可达性**未证 —— 需全池扫 `deal` 负值 / `-N Attack` 能否把 Attack 减到负数。
- **原版对 `Attack` 有无 0 下限**：本轮没查反编译（`GetCurrentMeleeAttack` 那一族），⛔ 不猜。
- `play基线归因签字.md` 自陈的 **6 行未查清**（`GOF67/SW8/SW9/SW11/TL16`）与 `BL64` 的夹具盲 —— 本报告未重复核。
- `D1` 在 Unity Mono 下的实际风险：**今天字符串哈希是稳定的**（所以自检全绿）；换运行时/平台才现形 —— 这一点是**读代码 + 项目自述**得出，没有跨进程实测（红线：不能跑）。

---

## 附：本轮读过的关键文件（方便下一个人复核）

`Core/{RuleCore,EffectResolver,BattleContext,BoardSlots,Aura,ActionPump,ActionQueue,PlayerState,UnitState,CardDef}.cs` ·
`Data/{SimpleAI,AiScoring,DeckLibrary,DeckStore}.cs` · `Editor/{RuleEngineTest,RuleEngineTest_Diag}.cs` ·
`CardPresentation/Battle/{BattleDriver,ReplayStore}.cs` · `CardPresentation/Net/{NetApply,NetProtocol}.cs` ·
`CardPresentation/Editor/BattleScene.cs`（回放段 26120-26400）· `工具/{_run_8_checks.sh,ruleprobe.sh}` ·
`资料/普查产出_1023c/{play基线归因签字,引擎_R1R2修复_执行报告}.md`。
