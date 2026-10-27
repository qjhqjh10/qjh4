# 战斗逻辑驱动 · 402 条 B 行重裁（W48BROW · 2026-10-27）

> **代理**：W48BROW（只读为主，产出落文件）
> **对象**：`账/战斗逻辑驱动.tsv` 的 **402 条 `状态=B`**（现读总表：A 711 / B 402 / C 45 / D 218，共 1,376 数据行）。
> **口径依据**：`方案 §4.1`（A 双侧方法级 / 场字段不当落点 / 死类判 D）+ `判据/战斗逻辑驱动_B2A_口径.md`（G0–G8 九门）。
> **复现**：脚本在 `d:/4/_tmp_view/w48/`（`cluster_b.py` · `cluster2.py` · `classify2.py` · `markers.py` · `mini15.py`）。核心分类逻辑见 §六 附录，可逐条复算。

---

## §一 分层表（402 条按「备注里的 B 理由」聚成 13 族）

> 方法：对 `备注 + 差异` 列做**优先级单标签**关键词分类（优先序见 §六），每族给出条数、代表行号、该族的 B 理由一句话。
> 复算命令：`python -I -X utf8 d:/4/_tmp_view/w48/classify2.py`。

| 族 | 条数 | 代表行号 | B 理由（族特征） |
|---|---:|---|---|
| **F1 P3 服务器半** | 7 | 219 354 395 590 1065 1228 1355 | 本地半已等价、缺服务器上报/远端配置那半 |
| **F2 判据不可读（需 VA / 异步体）** | 10 | 294 333 373 381 409 457 607 681 1114 1366 | Ghidra 摊平 / 字符串拼法失真 / 异步状态机体未读 |
| **F3 数据侧常量·分派未落** | 11 | 77 79 178 183 324 552 558 668 770 994 1026 | `ChooseValueOptions` 大 switch / `KeywordTable` 数据映射未落 |
| **F4 真实机制缺口** | 36 | 136 172 193 246 248 270 403 405 439 477 514 524 | 交换/变形/复制墓地/谓词/计数器/ReleaseCardbacks 等未实现 |
| **F5 零可达·休眠·原版 0 调用** | 33 | 93 144 180 191 238 460 489 521 642 656 799 985 | 机制在但池内 0 入口 / 原版零调用点 / 恒 false 数据 |
| **F6 D→B 复核（J12DK 等）** | 20 | 986 989 996 1031 1094 1140 1162 1180 1221 1234 1279 | 原判 D 被推翻（活方法），我们已有等价物但**未逐方法核** |
| **F7 语义已核·仅机检未过** | 15 | 83 118 156 194 280 322 326 350 355 371 372 380 388 397 422 | 审计已判等价，**仅落点不是可机检的方法声明**（字段/注释/无落点） |
| **F8 结构差异·净结果等价** | 33 | 76 204 212 227 504 674 762 893 897 900 902 947 1078 1170 1374 | 共享代码段 / 状态位无对象 / 可空 vs 定长等「机制不同、净结果同」 |
| **F9 观测·边界未跑（G3△/G4△/G5✗）** | 94 | 64 169 279 476 482 516 522 527 546 576 628 660 | 机制已核（G1✓G2✓），但**可观察量/差分/边界实验没跑** |
| **F10 未核验·没读全** | 57 | 80 82 96 158 233 254 292 308 345 361 469 490 | 方法体没逐句读 / 逐码对表没做 / 口径缺 |
| **F11 类型·数据模型不对位** | 3 | 348 369 1007 | 无 `DeckManager` / `PlayerHand` / `DamageInfo` 类型 |
| **F13 自称等价·未归因** | 25 | 125 304 334 335 340 344 365 392 624 730 898 1166 1242 | 写了「等价」但没给归因族；含 3 条调试串构造器（可能 D） |
| **F14 其它（混合）** | 58 | 101 103 115 205 236 342 360 446 480 517 604 782 806 1209 | 结构差异/落点内联/部分缺口/零可观测混合 |
| **合计** | **402** | | |

⚠️ **分类是关键词自动聚的，不是人工逐条裁定**。F4/F14 里混着「真缺口」和「结构等价」，F9 与 F10 边界犬牙交错 —— §二 逐族抽样回源核后给出**结论口径**，逐条最终档位仍需按 §三 清单逐条落。

---

## §二 逐族结论（族内抽 3–5 条回源核）

判据 = `d:/2/tools/decomp_full/*.c`（🥇）＋ 我们代码**现读**。每条给：升级 A / 转 C / 维持 B + 证据 `文件:行号`。

### F7 · 语义已核·仅机检未过（15 条）— 本域**最高价值**的升级面

**机械化验证**：把 15 条在临时副本里翻成 `A` 后跑 `check_ledger_points.py`（只读、无 `--fix`）⇒ **通过 0 / 失败 15**，失败原因全部是「落点不是可机检的方法声明」。逐条复算：

```bash
python -I -X utf8 d:/4/_tmp_view/w48/mini15.py
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py d:/4/_tmp_view/w48/mini15.tsv
# 结果：A 15 · 通过 0 · 失败 15
```

**回源核（3 条真方法落点 + 2 条 .c 体）**：

| 行 | 原版方法 | 判据（.c 现读） | 我们侧 | 结论 |
|---|---|---|---|---|
| **326** | `BattleManager.GetAvailableAttackTargets` | `BattleManager__GetAvailableAttackTargets.c`（扫 `+0x470` 全场） | 真声明 **`RuleCore.cs:5374（IsValidTarget）`**（现读已确认是这个签名） | **升级 A 可行**：把落点从注释行 `5267-5510` 改指 `5374（IsValidTarget）` |
| **280** | `BattleManager.DealMultiDamageVariable` | cpp2il `BattleManager.cs:40802-40837` | 真声明 **`RuleCore.cs:8431（ApplyDamage）`**（`:8442 ApplyDamageCore`） | **升级 A 可行**：落点改指 `8431（ApplyDamage）` |
| **380** | `BattleManager.GetStartingMana` | `BattleManager__GetStartingMana.c:1-26`（`playerGoesSecond XOR isPlayer` ⇒ +0x30 否则 +0x2c） | 选择式内联 **`RuleCore.cs:1754`**（`p.MaxEnergy = p == ctx.Players[ctx.SecondSeat] ? startingManaSecond : startingMana`；在 `BeginTurn`，真声明 `:1662`）。⚠️ **账里写的 `:1738` 已漂到 `:1754`**（1738 今天是 `DrawnThisTurn` 清理语句） | **升级 A 可行**：落点改指 `1662（BeginTurn）` + 判据要点写明「XOR 选值在 1754」 |
| 118 | `BattleManager.ActionsLeftInQueue` | `BattleManager__ActionsLeftInQueue.c:1-20`（四条 List `+0x18` 求和，四格非空才返回） | 等价物 = `ActionQueue.cs:209（Count）`，是**多行属性** `public int Count { get…}`（`:233 CountOf(lane)` 是分道口，非总和） | **维持 B**：属性 getter 按 §4.1 不得当落点；无返回总和的公开方法 |
| 355 | `BattleManager.GetHero` | `BattleManager__GetHero.c`（真→玩家 `+0xC0`／假→敌 `+0xC8`→`+0x28`，空则抛 NRE） | 等价物 = 字段 **`PlayerState.cs:72 Warlord`** | **维持 B**：字段级，§4.1 禁；语义已等价（null 处理我们主动出声） |

**F7 结论**：15 条**语义全部已核等价**（R1T3A 审计 + 本轮抽核一致）。其中 **3 条（280/326/380）能给方法级落点 ⇒ 直接升 A**；其余 **12 条**等价物是我们侧的**字段/无对象**（`Board` / `Warlord` / `CardsPlayedInTurn` / `preMulligan` / 属性 `Count`），按 §4.1「字段级不当落点」机械上**只能留 B**。
⇒ **→ 比例：A 3 · C 0 · B 12**。

### F4 · 真实机制缺口（36 条）— 转 C 主来源

**现读 grep（`Assets/` 全树）**：`exchangeMinion` / `ExchangeMinion` / `DoTransformUnit` / `ResolveCopyUnitInCemetery`（RuleEngine 内）**全部 0 命中**；`ReleaseCardbacks` **只有一句注释**（`BattleDriver.cs:32159`），无实现；`MissActions` **0 命中**。⇒ 抽样确认**是真缺口**。

| 行 | 缺什么 | 证据 | 结论 |
|---|---|---|---|
| 136 / 514 | `exchangeMinion` 换位 | 全树 0 命中 | **转 C**（休眠机制也要做，铁律 11） |
| 172 | `transformUnit` 变形 | 全树 0 命中 | **转 C** |
| 270 / 504 | `copyUnitInCemetery` | RuleEngine 0 命中（仅 `CardPresentation/EventTiming.cs` 注释提到） | **转 C** |
| 439 | `LeaveBattle` 缺两跳：`ReleaseCardbacks` + 重开教程局 | `ReleaseCardbacks` 仅注释 | **转 C**（两跳） |
| 524 | `MissActions` 计数器 | 全树 0 命中 | **转 C** |
| 405 / 403 / 406 / 414 / 423 | 谓词 `IsAgainstAI/Human/OnlineBot·IsDuringBattle·IsReplayMatch` | 引擎无方法（表现层有近似 `BattleDriver.cs:33513 IsReplayMatch`） | **转 C**（小量补） |

⚠️ **F4 内约 1/3 不是真缺**：如 L1101 `MinionManager.Awake`（18 格 xyz 18/18 一致）、L1102 `BroadcastActivateMinion`（结构差异）、L1144 `SlotPosToBoardPos`（y 恒 0）——它们是**结构等价/观测未钉 ⇒ 维持 B**，不是 C。逐条落档要按 §三 剔除。
⇒ **→ 抽样比例：C ≈ 0.8（~29 条）· B ≈ 0.2（~7 条）· A 0**。

### F3 · 数据侧常量·分派未落（11 条）— 转 C 次来源

- **L77 / L79**：`ChooseValueOptions`（原版 40+ 档运行时大 switch）—— `grep ChooseValueOptions` 全树 **0 命中** ⇒ 解析期算值 vs 运行期 switch，**结构性缺口 ⇒ 转 C**（补齐分派或写明等价边界）。
- **L178 / L183**：`KeywordTable` 类**存在**（`RuleEngine.KeywordTable`，`AttackSelector.cs:1365` 起在用），但 `${…} 数据常量/前缀 + chosenAbility 400 op 分派未落` ⇒ 是**数据映射层**未落 ⇒ **转 C**（数据侧），非类缺失。
- 其余（552/558/668/770/994/1026）抽样多为「表/映射未逐档核」⇒ **C/B 各半**，需逐条确认是「数据在资源侧（B）」还是「解析层缺（C）」。
⇒ **→ 抽样比例：C ≈ 0.65（~7 条）· B ≈ 0.35（~4 条）**。

### F5 · 零可达·休眠·原版 0 调用（33 条）— A / D / B 三分

- **机制在、我们数据 0 入口 / 原版 0 调用**：如 L521 `ResolveJamUnit`（jam 由 0 张卡授予）、L180 `AddTriggerPlayerCodex`（原版入队函数零调用）⇒ 需按 `方案 §4.1` 二选：**原版零调用 ⇒ D（带三源证据 + 时间限定）**；**机制在、只是不可达 ⇒ A（函数级等价物在）**。
- ⚠️ 先例（就在本账第 1、2 行 A 档）：「仅 0 调用点不足以判 D；函数级等价物在 ⇒ A」。⇒ 判 D 只给「原版零调用 **且** 我们无等价」或「数据恒 false（`preMulligan` 6 关全 false 那类）」。
⇒ **→ 抽样比例：A ≈ 0.4（~13）· D ≈ 0.3（~10，需调度台核 D 证据）· B ≈ 0.3（~10）**。

### F6 · D→B 复核（20 条，J12DK）

- 全部是「原判 D 被推翻（活方法 / 有调用点）」，语义多为**纯取值器**（`NumCardsInHand`→`PlayerState.Hand.Count`、`GetCardInPos`→下标直取、`GetTrait`→`Badges`）。
- **L1180 `NumCardsInHand`** 现读 `.c` = `*(int*)(handList+0x18)`（List.Count），我们 = `PlayerState.Hand.Count`（字段/属性）⇒ **字段级 ⇒ 按 §4.1 只能 B**。
- 例外：**本地化族**（L1221/1229/1230/1232/1233/1234/1239/1241）我们走 **`Loc.T`（`Loc.cs:3421` 真方法）** ⇒ **若逐方法核映射成立，可点 `Loc.T` 升 A**（需核 key 对应）。
⇒ **→ 抽样比例：A ≈ 0.2（~4，本地化族）· B ≈ 0.8（~16）· C 0**。

### F8 · 结构差异·净结果等价（33 条）

- 抽核 L1093 `ActivateMinions`（原版回合起逐张激活 vs 我们 `RefreshForNewTurn` + 现算 `Exhausted`）：我们的 `RuleCore.cs:2075 u.RefreshForNewTurn()` 存在 ⇒ **净效果同、时机不同**。
- L893 `SetCardState`（原版 18 档状态机 vs 我们归属驱动现算）、L897/900/902（残骸位恒 false 但终局同值，且**断言已排 §RuleEngineTest**）。
- 这些**判据已读、机制等价、有边界差异** ⇒ 若可观察量能钉（或原版 0 调用 / 状态位无外部读），**可升 A**；否则维持 B。
⇒ **→ 抽样比例：A ≈ 0.4（~13）· B ≈ 0.6（~20）**。

### F9 · 观测·边界未跑（94 条）— 最大一族

- 典型：L516 `ResolveFatigue`（`.c` 在、机制对，但「疲劳是否吃护甲/盾」未跑边界 G5✗）、L522 `ResolveManaBuff`（G3✗G4✗G5✗）、L169 `AddSummonUnit`（中间态可观察性未定）。
- **口径明文**：A 要求「至少一个可观察量钉住等价」（`B2A 口径 §一②`）。这些机制已核但**没跑差分/边界** ⇒ **维持 B**（下一步就是跑 `ruleprobe play <卡>` 两次读数）。
⇒ **→ 抽样比例：B ≈ 0.9（~85）· A ≈ 0.1（~9，可观察量本就 n/a）**。

### F10 · 未核验·没读全（57 条）

- L80 `AbilityLogic.PlayAbility`（2878 行主分流未通读，且是 Add* 那一族 34 行的**共同被调方**）、L76 `GetTargets`（1427 行只核 6 支）、L233/234 等「未逐条核，差异列空」。
⇒ **维持 B 100%**（下一步：逐句读 / 逐码对表）。

### F2 · 判据不可读（10 条）

- L381 `GetTargetInPos`：`.c` 现读 **271 行**（并非整段空），但求交系数「读不出」（Ghidra 表达式失真）；L457 `PlayAi`：**MoveNext 体存在**（`BattleManager._PlayAi_d__399__MoveNext.c`）⇒ 是「没读」不是「读不到」，应归 F10。
- L1366 `TurnScriptedData.UpdateName`（拼串段 `default` 失真）⇒ 真不可读。
⇒ **维持 B**（L457 可读 ⇒ 下一步读 MoveNext）。

### F1 · P3（7 条）· F11 · 类型不对位（3 条）· F13 自称等价（25 条）

- **F1**：L395 `HandleOfflineBattleResult`（本地结算在、P3 上报缺）、L1065 `GetBotDeck`（原版远端 config vs 我们本地库）、L354 `GetGameState`（状态串已落成纯函数）⇒ **本地半 B / 服务端半可判 D（P3，带时间限定）**。
- **F11**：L348 `GetDeckManager` / L369 `GetPlayerHand`（无对应类型）⇒ B（架构差异，非缺口）。
- **F13**：L334/335/340 = 原版 **`private string` 调试摘要构造器**（不是取列表），我们无对应物 ⇒ **可判 D（诊断专用，非离线行为）** 或 B；其余 L365（无 peek 函数）、L1242（异画不建档，无对象）⇒ B/D 待核。
⇒ **→ F13：B ≈ 0.6 · D ≈ 0.2（调试串 3 条）· A ≈ 0.2**。

---

## §三 可执行清单（按「升级 A」优先排序）

### 3·1 直接升 A（证据已硬，只需改落点/补观测）

| 优先 | 行 | 改动 | 改成什么 |
|---:|---|---|---|
| 1 | **326** | 落点 | `RuleCore.cs:5267-5510（IsValidTarget 全函数）` → **`RuleCore.cs:5374（IsValidTarget）`** |
| 2 | **280** | 落点 | `RuleCore.cs:8143-8250（ApplyDamage 逐目标不同量调用）` → **`RuleCore.cs:8431（ApplyDamage）`** |
| 3 | **380** | 落点 | `GameplayVariables.cs:215-225（…）` → **`RuleCore.cs:1662（BeginTurn）`**（判据要点注明「XOR 选值在 `:1754`」） |
| 4 | F6 本地化 8 条（1221/1229/1230/1232/1233/1234/1239/1241） | 落点 + 核 key | 指 **`Loc.cs:3421（T）`**，并核「原版 term/卡名 → 我们的 key」映射（逐方法核后才能升 A） |
| 5 | F8 约 13 条（如 1093/893/897/900/902） | 跑一次差分或确认观测 n/a | 能钉可观察量（或原版 0 调用）⇒ A；否则留 B |

### 3·2 转 C（真缺口，按 §4.1 判据落「差异 + 等级」）

| 行 | 缺口一句话 | 判据出处 |
|---|---|---|
| 136 / 514 | `exchangeMinion` 换位未实现 | 全树 grep 0；`.c` = `BattleManager__ResolveExchangeMinion.c` |
| 172 | `transformUnit` 变形未实现 | 全树 grep 0 |
| 270 / 504 | `copyUnitInCemetery` 未实现 | RuleEngine grep 0 |
| 439 | `LeaveBattle` 缺 `ReleaseCardbacks` + 重开教程局 | `BattleDriver.cs:32159` 仅注释 |
| 524 | `MissActions` 计数器未实现 | grep 0 |
| 403/405/406 | `IsAgainstAI/Human/OnlineBot` 谓词未实现（引擎侧） | 表现层有近似 `BattleDriver.cs:33513` |
| 414/423 | `IsDuringBattle` / `IsReplayMatch` 引擎谓词未实现 | 同上 |
| 77/79 | `ChooseValueOptions` 运行时大 switch 未落 | grep 0 |
| 178/183 | `KeywordTable` 数据映射 + 400 op 分派未落 | 类在、映射未落 |
| 193 | `AllowResolveAttack` 六段闸只建前五段 | 账差异列 |

### 3·3 维持 B（写明「还差哪一份数据 / 哪一层」）

- **F9（~85 条）**：差「跑 `ruleprobe play` 两次读数 + 边界反例（G3/G4/G5）」。
- **F10（57 条）**：差「逐句读方法体 / 逐码对表」。
- **F7 的 12 条字段/无对象**：差「§4.1 允许的方法级落点；等价物在我们侧是字段」——**这是口径问题，见 §五 顺手发现 ①**。
- **F2（10 条）**：差「VA 反汇编求交系数 / `UpdateName` 拼串」。
- **F5（~10 条）**：差「可达性判定（三源）→ 再决定 A/D」。

### 3·4 可判 D（仅限 §4.1 例外，须带证据 + 时间限定）

- F13 调试串 3 条（334/335/340）：原版 `private string` 诊断构造器，非离线行为。
- F5 里「原版零调用 **且** 我们无等价」者（需三源核实后逐条判）。
- F1 服务端半（395/354/590/1228/1355 的 P3 部分）：P3 按 2026-10-26 授权由调度台判，写「原版调用链断在这儿」。

---

## §四 比例预估（按族抽样推，合计 402）

| 族 | 条数 | 预估 A | 预估 C | 预估 D | 预估 B |
|---|---:|---:|---:|---:|---:|
| F1 P3 | 7 | 0 | 1 | 2 | 4 |
| F2 不可读 | 10 | 0 | 0 | 0 | 10 |
| F3 数据侧 | 11 | 0 | 7 | 0 | 4 |
| F4 真缺口 | 36 | 0 | 29 | 0 | 7 |
| F5 零可达 | 33 | 13 | 0 | 10 | 10 |
| F6 D→B | 20 | 4 | 0 | 0 | 16 |
| F7 机检未过 | 15 | 3 | 0 | 0 | 12 |
| F8 结构等价 | 33 | 13 | 0 | 0 | 20 |
| F9 观测未跑 | 94 | 9 | 0 | 0 | 85 |
| F10 未核验 | 57 | 0 | 0 | 0 | 57 |
| F11 类型 | 3 | 0 | 0 | 0 | 3 |
| F13 自称等价 | 25 | 5 | 0 | 5 | 15 |
| F14 其它 | 58 | 3 | 6 | 0 | 49 |
| **合计** | **402** | **≈50（12%）** | **≈43（11%）** | **≈17（4%）** | **≈292（73%）** |

**一句话**：这 402 条**大部分（≈73%）是「已实现但没核完 / 没跑观测」**，不是缺口；**真缺口（C）≈11%** 集中在 F4/F3/F14；**能立刻升 A 的硬证据 ≈12%**，其中 F7 的 3 条 + F6 的本地化族是**零风险**（只需改落点）。
⚠️ **别把这张表当定案** —— 它是**抽样外推**，逐条最终档位以 §三 清单落地为准。

---

## §五 顺手发现

1. 🔴 **口径级问题（需调度台裁定）**：「原版是纯 getter / 我们侧是字段」的等价（F7 的 12 条 + F6 大部分 + F11）在 §4.1 下**永远过不了机检**（字段不算方法落点）。这不是「没复刻」，是我们把复刻做成了「字段直接读」。要么：①加一个薄的 `GetHero()`/`NumCardsInHand()` 转发方法当落点；②给 §4.1 加一档「纯取值器等价可用字段声明当落点（只报不失败）」。**当前它们是 B，不代表没做对**。
2. **F7=15 的机械证据已留档**：`mini15.tsv` 跑 `check_ledger_points.py` ⇒ 通过 0 / 失败 15，失败原因逐条在 §二 列出 —— 下次改落点后**用同一命令复验**即可（不要只看状态列）。
3. **F4 的「退 C」不能一刀切**：L1101 `Awake`（18 格 18/18）、L1102、L1144 是**结构等价**，误判 C 会让我们去「补」一个本来就对的东西。
4. **L457 `PlayAi` 的 MoveNext 体确实存在**（`BattleManager._PlayAi_d__399__MoveNext.c`）—— 归 F10「没读」，不归 F2「读不到」；别再写成「异步体缺失」。
5. **L381 `GetTargetInPos` 的 `.c` 有 271 行**（不是空壳）；「读不出」只指求交系数那一个表达式，别把整条判成「体被剥」。
6. **F6 本地化族是「零风险 A」**：`Loc.cs:3421 T()` 是真方法，把 `GetCardNameTerm`/`GetLocalized*` 指过去 + 核 key，就能整族升级；这是本域性价比最高的一批。
7. **账里存在「同名重载合并」**（如 `AI.CombatAttackWithType` 两个形参类型共一份体）⇒ 一落点覆盖多行，符合 §4.1「共享落点」，别当重复。

---

## §六 附录：可复现分类逻辑

```python
# 优先级单标签（classify2.py 核心）；s = 备注 + " || " + 差异
if P3信号(服务器/P3/PlayFab/GetGameState/远端config): "F1-P3服务器半"
elif 不可读(摊平/读不出/读不全/MoveNext未/体被/拼串失真): "F2-判据不可读"
elif ("裁 A" and ("机检未过" or "回 B")): "F7-语义已核·仅机检未过"
elif 数据侧(KeywordTable/ChooseValueOptions/op分派/400 op): "F3-数据侧"
elif 真缺口(机制缺口/未实现/缺半截/缺 Release/缺 MissActions/未建): "F4-真实机制缺口"
elif 零可达(池内 0/0 入口/休眠/零调用点/恒 null/恒假/建议D): "F5-零可达"
elif "J12DK": "F6-D→B复核"
elif 结构等价(§4.1/结构差异/形态差/净结果同/机制不同): "F8-结构等价"
elif 观测未跑(G5✗/G3△/G4△): "F9-观测未跑"
elif 未核验(未逐条核/没读全/未通读/未对账): "F10-未核验"
elif 类型不对位(无 DeckManager/无 PlayerHand/DamageInfo): "F11-类型"
elif 出域: "F12-出域"
elif 等价: "F13-自称等价"
else: "F14-其它"
```

> 复算：`python -I -X utf8 d:/4/_tmp_view/w48/classify2.py`（打印 13 族条数 + 每族行号）。
> ⚠️ 本机 `python -c "…中文路径…"` 会在 argv 解码处失败 ⇒ 路径写进 `.py` 源码（本批脚本都这么做）。
