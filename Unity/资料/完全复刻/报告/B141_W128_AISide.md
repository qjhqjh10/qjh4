# B141 · W69 · 账 L29 `AI.ScoreFromManaBuff` ⑤⑥⑦ —— **侧别 / 残骸 / 手牌**口径回源核

> 执行代理 `B141-W69`（第一百二十八会话 · W128 波次）· 2026-11-04
> 施工单来源：`报告/B141_W128_AImanaCost.md` §8 ❓2（W59 自己标「未回源核」的那条）
> 涉及文件：`RuleEngine/Data/AiScoring.cs`（唯一改动）

---

## 1. 一句话结论

**三条口径回源核完：侧别、残骸、手牌全部与原版一致**，唯一实修的是 ⑦ 漏了原版的 `IsInPlay()` 闸（本次补上，**今天零可观测差异**）。
另外**就地纠正了施工单的一处说法**：⑦ 那两张表**不是「两份不同形状」**，它们逐句同构（只是 `isPlayer` 判断一个正写、一个反写）。

---

## 2. 三条原版口径（逐条，第一权威 + 第二权威都读过）

判据文件：
- 第一权威 `d:/2/tools/decomp_full/AI__ScoreFromManaBuff.c`（全文 101 行，逐句读）
- 第二权威 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AI.cs:2052-2110`（**两份逐句一致**）
- 被调方：`BattleManager__GetCardsInHandRef.c` · `BattleManager__GetHero.c` ·
  `BattleManager__GetPlayerMinionsAndRemnantInPlay.c` · `BattleManager__GetEnemyMinionsAndRemnantInPlay.c`
  （+ ILSpy `BattleManager.cs:41514-41690 / 41872-41880`）
- `CardScript.IsInPlay` 的真值 = `CardScript__IsInPlay.c`（`cardState ∈ {2,3,15,17}`）

| # | 原版口径 | 证据行 | 我们现读 | 判定 |
|---|---|---|---|---|
| ⑤ 手牌 | `manager.GetCardsInHandRef(**thisCard.isPlayer**)` → 该侧 `PlayerHand.currentHand`（**无卡种闸**，所有手牌都按 `CurrentCost` 数） | `.c:30`（调用）· `.c:35-47`（循环）· ILSpy `AI.cs:2066` · `BattleManager__GetCardsInHandRef.c:7-14`（`isPlayer` 假取 `enemyHand`(+0xd8)、真取 `playerHand`(+0xd0)，返回 `currentHand`(+0x50)）· ILSpy `BattleManager.cs:41874-41877` | `ctx.Players[p].Hand`（`Core/PlayerState.cs:20` = 该侧手牌表）· 判据 `AiScoring.cs:1082-1088` | ✅ **一致**（同侧、同为 currentHand、同样无卡种闸） |
| ⑥ 督军 | `manager.GetHero(**thisCard.isPlayer**)` 那一侧的督军 | `.c:48` · ILSpy `AI.cs:2086` · `BattleManager__GetHero.c`（`isPlayer` 假取 `+0xC8`、真取 `+0xC0`，返回 `+0x28`） | `ctx.Players[p].Warlord`（判据 `AiScoring.cs:1092`） | ✅ **一致** |
| ⑦ 场上 | `isPlayer` 真 ⇒ `GetPlayerMinionsAndRemnantInPlay()`；假 ⇒ `GetEnemyMinionsAndRemnantInPlay(manager)`。**两条函数体逐句同构**（见 §3），内容 = `unitsInPlay` 里「**同侧** ∧ `cardType ∉ {10 Hero, 20 Tactic, 40 Whispers}` ∧ **`IsInPlay()`**」的那些 ⇒ **残骸照样计入** | `.c:61-66`（两条分支）· `.c:67-91`（逐个同判）· ILSpy `AI.cs:2095` · `BattleManager__GetPlayerMinionsAndRemnantInPlay.c:48-64` / `…GetEnemyMinionsAndRemnantInPlay.c:48-64`（唯一差别是 `.c:48` 的 `!= '\0'` vs `== '\0'`）· ILSpy `BattleManager.cs:41558-41592` | **侧别 / 残骸 / 卡种 三种都一致；`IsInPlay()` 闸缺**（判据 `AiScoring.cs:1093-1098`） | 🟠 **侧别✅ 残骸✅ 卡种✅ · `IsInPlay()` ❌ → 已补** |

### 2.1 「侧别」为什么等价（`thisCard.isPlayer` ↔ 我们的 `p`）

原版三处**全部读 `thisCard.isPlayer`（`+0x40`）= 那张正被打出的卡自己的所属方**，不是「当前回合方」。
我们是形参 `p`，唯一传入点 = `ScoreFromCriteria` 的 `thisSide` = `ctx.Active`（`AiScoring.cs:89`、`:350`）。
两者在**所有生产路径**上相等：

1. `SimpleAI.ScoreFromPlayingCard`（`Data/SimpleAI.cs:533-549`）只评 **`ctx.Players[ctx.Active].Hand` 里的卡**
   ⇒ 被评的卡自己的所属方 = `ctx.Active`。
2. 唯一的嵌套调用 `HandBuffValue → ScoreFromPlayingNow`（`AiScoring.cs:645`）的候选来自
   `targetCriteria = {targetsAffected:50, friendlyUnits:true, …}`（现读：全卡池只有 1 条 option 12，
   见 `Resources/ai_scoring.json`）⇒ **只取友方**，仍然同侧。
3. `AiScoring.ScoreFromPlayingNow` 的其余调用点 = 自检/探针（`RuleEngineTest*`），不进产品。

⇒ **结论：等价**。不为此改签名（改签名会波及 `ScoreFromPlayingNow/ScoreFromCriteria` 的公开口，收益为零）；
改用 **断言 ⑭/⑮ 把「`p` 就是取哪一侧的开关」钉死**（见 §5）。

---

## 3. 就地纠正施工单的一处说法（⑦ 的「两份不同形状的表」）

施工单（W59 §8 ❓2）写的是「⑦ 走 `GetPlayerMinionsAndRemnantInPlay()` / `GetEnemyMinions(manager, 0)`
—— **两份不同形状**的表」。**回源核：不是。**

- 原版 `.c:62` 的调用是 `BattleManager__GetEnemyMinionsAndRemnantInPlay(param_2, 0)`，**不是** `GetEnemyMinions`（那个名字的函数在 `decomp_full` 里根本不存在）。
- 两条函数体**逐句同构**：都是「清空结果表 → 清空临时表 → 扫 `unitsInPlay(+0x470)` → 按 `isPlayer(+0x40)` 正/反筛 →
  再筛 `cardType ∉ {10, 0x14, 0x28}` → `CardScript.IsInPlay()` → `tempUnitsInPlayList.Add` → `AddRange` 进结果表」。
  **唯一差别**是 `.c:48` 的 `!= '\0'`（player 版）与 `== '\0'`（enemy 版）。
- `.c:65` 那句 `GetPlayerMinionsAndRemnantInPlay()` **看起来「没有实参」**，那是反编译器把 `this` 吃掉了；
  ILSpy 同一处是 `manager.GetPlayerMinionsAndRemnantInPlay()` ⇒ **它照样是这一方的表**，不是全局表。

⇒ 所以 ⑦ **不存在「用错了一张表」的风险**，只存在「表格的**过滤条件**有没有照做」的问题 —— 也就是下面 4.1。

---

## 4. 改动（`RuleEngine/Data/AiScoring.cs`，零行数变化）

### 4.1 唯一实修：⑦ 补 `IsInPlay()` 闸

原版 ⑦ 只收 `CardScript.IsInPlay()`（`cardState ∈ {2 inPlay, 3 inPlayAttacking, 15 inPlayTransforming, 17 inPlayAminingAttack}`，
**不含 5 waitingToDie**）的单位。我们原来**遍历整张棋盘、不过滤**。

```diff
-                if (AbilityManaCostInRange(board[s], manaLeft, limit)) { sum += 1f; minionHits++; }
+                if (board[s] != null && board[s].IsInPlayState(ctx) && AbilityManaCostInRange(board[s], manaLeft, limit)) { sum += 1f; minionHits++; }
```

- `IsInPlayState` = 原版那个谓词的直译（`Core/UnitStateCardState.cs:156-164`，注释里写明「⛔ 不含 `waitingToDie(5)`」）。
- **残骸不会被这道闸误杀**：棋盘上的残骸是 `RuleCore.cs:10023` 的 `new UnitState(u.Instance, false) { IsRemnant = true, … }`，
  `_cardStateMark` 默认 `inPlay(2)` ⇒ `CardStateOf` 返回 2 ⇒ `IsInPlay()` **真** ⇒ 与原版函数名
  `…MinionsAndRemnantInPlay` 的语义一致（断言 ⑰ 钉住）。
- **今天零可观测差异**：AI 打分发生在**动作之间**，那时 `ctx.IsPendingDeath` / `DyingThisAction` 都是空的。
  这道闸是**结构忠实 + 防将来回归**（有人把打分挪进结算窗口时不会静默多数一个待死单位）。

### 4.2 顺带订正的三处**已过期**注释（同一行内改，行数 0）

| 位置 | 原文（过期） | 改后 |
|---|---|---|
| `:1101` | 「⑥⑦ 要等 `activeAbility.manaCost` 列落进来才会出非 0」 | 「算式是**活的**；现实多为 0 是**原版数据本身**（87 张 gate=1 但 manaCost=0、34 张 manaCost=2 但 gate=0）」 |
| `:1104` | 「🔴 主动技 manaCost 列未落 ⇒ ⑥⑦ 恒 0」 | 「🔴 钩子被置 null ⇒ ⑥⑦ 恒 0」（产品默认接线，只有自检会置 null） |
| `:1042-1047` | ⑤⑥⑦ 三条 `<item>` 没写侧别 | 写明 `GetCardsInHandRef/GetHero(…, thisCard.isPlayer)` 与 ⑦ 的两条表 + 三重过滤（**6 行 → 6 行**，⛔ 不是 7 行 —— 第一次改写成 7 行会让 `:1063` 等处整体 +1，已回正并现读确认） |

### 4.3 行数变化与账落点

`RuleEngine/Data/AiScoring.cs`：**1,334 → 1,397 行（+63）**，新增的 63 行**全部在 `:1331-1391`（断言块）**，
即账的 27 处落点（最大 `:1274`）**下方**。

现读确认（`python` 按 `\r\n` 切行，**无裸 CR**：`count(\r\n)=0` · `bareCR=0`）：

| 行 | 内容 | 结论 |
|---|---|---|
| `:1063` | `public static float ScoreFromManaBuff(BattleContext ctx, int p, AiScoringEntryDto e)` | 未挪 ✅ |
| `:1120` | `public static Func<CardDef, int> ActiveAbilityManaCost = …` | 未挪 ✅ |
| `:1127` | `static bool AbilityManaCostInRange(UnitState u, int manaLeft, int limit)` | 未挪 ✅ |
| `:1132` | `if (u.Card.HasActiveAbility != 1) return false;` | 未挪 ✅ |
| `:1146` | `AiScoringSelfTest.Run(…)` 的接线说明 | 未挪 ✅ |
| `:1274` | `AiScoring.ActiveAbilityManaCost = c => c != null ? c.ActiveAbilityManaCost : -1;` | 未挪 ✅ |

账闸（**只读**，⛔ 没跑 `--fix`）：`python -I -X utf8 工具/check_ledger_points.py` 的告警/漂移/失败清单里
**`AiScoring` 命中 = 0 条**（`grep -ci AiScoring` = 0）。

---

## 5. 断言（8 条，接在**已接线**的 `AiScoringSelfTest` 里，⛔ 没建孤儿文件）

`AiScoringSelfTest.Run` 由 `RuleEngineTest.cs` 调用（W59 已核），新增 ⑫～⑲ 随之自动跑到。

| # | 断言 | 灭自证（怎么才会红） |
|---|---|---|
| ⑫ | ⑤ **只看 p 自己的手牌**：对手手牌里有一张 cost 4 ∈ (3,5] ⇒ **0** | 侧别写反 / 恒读 `1-p` ⇒ 红 |
| ⑬ | ⑤ **无卡种闸**：p 手牌里**战术卡** cost 4 也 +1 | 有人给 ⑤ 擅自加「只数单位」⇒ 红（原版没这道闸） |
| ⑭ | ⑥⑦ **只看 p 自己这一侧**：对手督军 + 对手场上单位都够格 ⇒ 仍只有 ⑤ 那 1 分 | 侧别写反 ⇒ 变 3 ⇒ 红 |
| ⑮ | **同一局面换 `p = 对手` ⇒ 3** | 与 ⑭ 构成「结构上不可能同时被糊过去」的一对：恒读某一侧 ⇒ 必有一条红 |
| ⑯ | ⑥ 同侧督军 manaCost=5 ∈ (3,5] ⇒ **+1** | 督军那半没接 / 侧别错 ⇒ 红 |
| ⑰ | ⑦ **残骸计入**：`IsRemnant = true` 的单位 manaCost=5 ⇒ **+1** | 有人把残骸当「不在场」剔掉 ⇒ 红 |
| ⑱ | ⑦ **`IsInPlay()` 闸**：残骸进 `DyingThisAction`（⇒ `cardState = waitingToDie(5)`）⇒ **不计入** | **删掉本次补的那半条闸 ⇒ 变 3 而非 2 ⇒ 红** |
| ⑲ | 同上退回（移出待死集合）⇒ 又是 3 | 闸写成恒假（永远排除）⇒ 红 |

⚠️ ⑱/⑲ 是一对「闸活着的正反两面」；⑭/⑮ 是一对「侧别开关的正反两面」。

---

## 6. 验证读数（本机现跑）

| 项 | 命令 | 结果 |
|---|---|---|
| 类型检查 | `TMPDIR=/tmp/wf_b141_w69 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 解析层对拍 | `bash 工具/ruleprobe.sh check` | **rc=0** · 解析差异 **0 行** · 哨兵失败 **0** · 输出指纹 **`77a3c2bc…`**（未变） |
| 结算层对拍 | `bash 工具/ruleprobe.sh play check` | **rc=0** · 基线指纹 **`a0e9c63d…`**（未变） |
| 账落点闸 | `python -I -X utf8 工具/check_ledger_points.py` | `AiScoring` 相关 **0 条**（全仓那 592 漂移 / 76 失败是**既有的**，与本次无关） |

**两个指纹为什么不该动**：`play all dump` 是结算层 sweep，**结构上看不见 AI**（既有事实，见 `CLAUDE.md` §三）；
本次只动 AI 打分的一条过滤，**逐字节不变是预期的**，⛔ 不能拿它当「改动生效」的证据。
AI 侧的可观测口仍是 `ruleprobe ai <卡>`。

---

## 7. ❓ 没做完 / 没查清的

1. **❓ 断言未在本会话跑过** —— 按红线不跑 Unity，`RuleEngineTest.Run` 未执行。`typecheck 0/0` 只证明**编得过**；
   ⑫～⑲ 要等下次同步点跑宿主才算绿。
2. **❓ 原版「hero/hand 非 null」那两处结构闸没复刻** —— `.c:34`（手牌表非 null 才进整段）与 `.c:49`（hero 非 null 才进 ⑥⑦）。
   那是反编译器的 null 保险，不是语义；**生产路径上两者恒非 null**（督军开局必设、手牌表恒存在）。
   我们保持「无这道闸」（fixture 里 `Warlord == null` 也能跑 ⑦）。**如实记着**，未改。
3. **❓ ⑦ 的 `cardType ∉ {10,20,40}` 是用「跳过督军格」等价表达的** —— 依据：棋盘只放 `unit`/`hero`
   （`CardDef.IsUnit`），Tactic(20)/Whispers(40) 在我们模型里是手牌卡 / 挂件（`Core/Enchantment.cs`），**不进 `PlayerState.Board`**。
   ⇒ 现读等价。若将来有「把战术/低语放进棋盘格」的新机制，这里要换成读 `CardDef.CardType` 过滤（现读 `_cardType` 有 `-1` 未知档，⛔ 别直接当 0 用）。
4. **❓ `p` 与 `thisCard.isPlayer` 的等价是「可达性论证」而不是「类型上不可能不同」** —— 若将来有人从
   `ScoreFromPlayingNow` 评**对方手牌**的卡（今天没有这条路径），`thisSide = ctx.Active` 就会与原版分叉。
   断言 ⑭/⑮ 只能钉「`p` 是开关」，钉不住「调用方传得对不对」。⇒ 真出现那条路径时，应把**卡的所属方**显式传进来。

---

## 8. 给调度台的交接

- 账 L29 的这一维（侧别/残骸/手牌）**已核完**，判据落点 = `RuleEngine/Data/AiScoring.cs:1093-1098`（⑦ 的过滤）
  ＋ `:1082-1088`（⑤）＋ `:1092`（⑥）；断言宿主 = `AiScoringSelfTest` 的 `:1331-1391`。
- 同步点要跑的那一条：**`RuleEngineTest.Run`**（确认 ⑫～⑲ 全绿）。
- 若以后 ⑦ 又多数了待死单位，先看 `:1097` 的 `board[s].IsInPlayState(ctx)` 是否被人删掉（断言 ⑱ 会先红）。
