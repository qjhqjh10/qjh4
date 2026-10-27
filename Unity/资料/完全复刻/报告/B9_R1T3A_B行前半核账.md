# B行逐条核_R1（前半 165 条）—— R1T3A 报告

> 任务：`账/战斗逻辑驱动.tsv` 状态=`B` 的行，按文件顺序取**前 165 条**，逐条判「已实现且一致（建议升 A）/ 真偏离（保留 B）/ 落点失效 / 判据缺」。
> 产出：`判据/B行逐条核_R1_前半.tsv`（8 列逐条结论）＋ `账/_parts/B9_R1T3A.tsv`（11 列补丁，`等级` 保持原值，只改 `差异/备注/轮次`）。

## ① 核了多少条 / 建议分布

- **已完成：R1–R165，共 165 条**（= 主表前 165 个 B 行，覆盖到 `ResolveExchangeMinion`）。
- 建议分布：**A（建议升）32 条 · B（保留）125 条 · 落点失效 8 条 · 判据缺 0 条**。
- 说明：本批以「主表已有证据 + `.c` 原文抽查 + 落点存在性/方法名核对」三路交叉。**125 条 B 里大多数是主表已写明「机制缺口 / 结构差异无法证明等价 / 未逐条核」，我复核后维持 B**；价值集中在 32 条可升 A 与 8 条落点失效。

## ② 最有价值的发现（带 文件:行号）

1. **8 条落点失效（原落点指向错对象，非本方法等价）**：
   - `GetCardsDeckPlayer` / `GetCardsHandPlayer` / `GetCardsPlayPlayer` 原版签名是 **`private unsafe string` 调试摘要构造器**，不是取列表 —— 旧账把它们等价成 `PlayerState.Deck/Hand/Board` 是**错误等价**（`BattleManager.cs:20075/20301/20399`）。我们无对应物，诊断为各调用点现拼。
   - `GetCurrentPlayerId(bool)` 返回的是 **`matchData.playerId/opponentId` 字符串**，不是回合方 0/1；唯一调用点只喂一句 `CustomDebug.Log` ⇒ 无可观察行为，原落点 `BattleContext.Active` 应撤。
   - `GetTotalMana` 落点应为 **`PlayerState.MaxEnergy`（`PlayerState.cs:75`）**，原落点 `Energy` 是「剩余」(= 原版 `unusedMana`)，是另一个量。
   - `GetHighestHealthWarlord` 原落点 `CountEval` 无此档；真落点 = `AiTargets.cs:303` 的 230 档（**未实现**，池内 ta=230 零行）。
   - `IsThereValidAbilityTarget` / `IsThereValidSpellTarget` 原落点 `CanUseAbilityTarget` **方法已不存在**；正确落点 = `RuleCore.cs:11020`(`ResolveAbilityTargetOwner`) / `EffectResolver.cs:2140`(`IsLegalPlayTarget`)。
2. **落点行漂移（方法在、行号错，已在表内订正）**：R3 `PickBotDeck` **BattleDriver.cs:33853**（原 33603 落在 `BoardHighlightOf`）、R13 `SideOfUnitType` **TutorialScript.cs:1589**、R14 `DriveTutorialScript` **BattleDriver.cs:17949**、R16 `StepTutorialScript` **BattleDriver.cs:18009** —— 全部实测核对。
3. **`AbilityData.SetNewData` 的 `.c` 三个形参其实读得到**（`AbilityData__SetNewData.c:15-17` 明确写 `+0x30/+0x44/+0x48`），旧账「三个形参被反编译成 default ⇒ 读不到」**已过期**；另 `.c:20` 是 `*(undefined2*)(+0x40)=0`（**2 字节**），旧账「`whileInPlayVariable` 不动」存疑，应核字段偏移。
4. **32 条可升 A**（逐句/结构核对等价）：R1/R2/R3/R4/R11/R17/R21/R22/R26/R33/R35/R40/R41/R64/R75/R76/R78/R79/R84/R85/R86/R92/R94/R95/R96/R98/R107/R108/R111/R118/R122/R132。典型：`ScoreFromManaBuff`（`RuleEngine/Data/AiScoring.cs:1063`）三段逐字等价；`GetStartingMana`（`RuleCore.cs:1738`）XOR 选值逐字对上；`GetCardsInHandRef`/`GetDeck` 等引用/取侧 getter 语义天然满足。
5. **`ScoreFromHandBuff` 尾部算式取自 R11 RVA `0x93B7A0`**（`.c` 把返回值丢成 void），我们 `AiScoring.cs:622` 的 `−3×playable` 与两项 counts 与 R11 转述一致 ⇒ 可升 A，但残余判据仍依赖 VA（已记）。
6. **§4.1 策略性 B 要区分**：`BroadcastUnitRegiment` / `BroadcastUnitSynapse`（R45/R46）因「同一 `文件:行` 覆盖多个方法」按方案 §4.1 只能记 B —— **语义其实逐句等价**，请在调度台决定是否保留策略性 B（我在表里标了「语义等价，按策略记B」）。
7. **`GetAllCardsInHand` / `GetAllCardsInPlay`**（R75/R76）同样：语义等价、0 调用点，按 §4.1「落点是字段」记 B；若要升 A 需调度台认可字段级等价。
8. **`IsPremulliganScriptAvailable` / `HasScriptedPremulliganActions` / `GetPremulliganScriptedData`**（R108/R122/R132）：原版三档门在 6 关 `scriptedTurn` **全 false** ⇒ **恒 null/恒假**，我们无执行代码反而是等价 ⇒ 建议升 A（原记 B 偏保守）。
9. **`ShallowCopy`（R11）= MemberwiseClone**，我们数据共享不可变即常态、0 调用点 ⇒ 等价可升 A。
10. **`GetTotalMana`/`GetStartingMana` 这类取侧 getter** 已核「侧映射/总量 vs 剩余」正确，落点订正后等价（见第 1 条）。

## ③ 没查清的部分（照实说）

- **125 条保留 B 中，有一部分我未逐个 `.c` 逐句重读**，而是复核主表已记录的证据（这些行本身带有具体 `.c` 行号）＋ 落点存在性。**需要「逐句」二次确认的**主要是：`GetValueFromCriteria`(R7)、`NewBuffStat`(R8)、`PlayAbility`(R9)、`GetTargets`(R6)、`ResolveAction`(R155) —— 这些是数千行的大方法，主表已明确「只核过部分支」，本批维持 B 未再展开。
- **判据丢值未复读的**：R1 尾部算式（`.c` 丢 `return` 值，靠 R11 RVA）、R112 `GetTargetInPos`（Ghidra 摊成 `num*0`，系数读不出，需 VA 反汇编）、R80/R126 `GetCampaignScriptedData`/`IsCampaignScriptAvailable`（调用点在异步状态机体，行为未核）。
- **「原版零调用点 ⇒ 建议 D」的几条（R19/R28/R30/R31/R45/R46/R108/R122/R132/R145/R153）**：主表已按「decomp 只覆盖约 29%、cpp2il 会丢调用点」保持 B。我没有对 724 条无方法体再扫一遍，故不改判 D。

## ④ 顺手发现

- **主表多个 `等级` 列值 ≠ 状态列**（如 `等级=P2/B9/...`），与「等级」这一词在本批口径下指 `状态(A/B/C/D)` 不同 —— 逐条核表的「原等级」我按**状态列=B** 填，避免歧义。
- **R3 `PickBotDeck` 落点漂移 90 行**是「行号会漂」的典型（`RuleCore/BattleDriver` 有多个写手在改）；建议后续账一律用「裸声明名 + 现读行号」双写（主表 L3 批已开始这么做）。
- **`IsThereValidAbilityTarget` 原落点 `CanUseAbilityTarget` 在 `RuleCore` 已不存在** —— 这类「方法被删/改名后账未跟」的落点，`check_ledger_points --fix` 修不了（名字已消失），只能人工改，值得在巡检里单列。
- **`GetTotalMana`(Energy vs MaxEnergy) / `GetCurrentPlayerId`(Active vs playerId)** 是同一类**错误等价**：旧账把「名字像」当「语义同」。建议对 `Get*` 家族统一做一次「签名/返回类型 vs 我们字段」的对照体检。

## 下一批从哪继续

- 本批止于 **R165 = `ResolveExchangeMinion`**。
- 主表共 **422 个 B 行**；**下一批从 R166 `ResolveFatigue(bool playerDrawing)` 继续**，剩余约 **257 条**。
