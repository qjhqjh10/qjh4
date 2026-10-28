# B89-E2b · R9 批次 3「RuleCore 触发/相位族」（Chain R 第 2 棒）—— 执行报告

- 代理：`B89` 第 1 波执行代理 `E2`（续派）· 2026-10-28
- 授权文件：`RuleEngine/Core/RuleCore.cs`（本批**只改这一个**；`BoardSlots.cs` / `BattleContext.cs` 本批未再动）
- 账补丁：`账/_parts/B89_E2b.tsv`（11 行 = 表头 + 10 条，11 列 / LF / 无裸 CR 已校验）
- 判据：`判据/B88_R9_主线68条分批.md` §「批次 3」（第 81–99 行）+ 每条的 `.c` 现读

---

## §1 逐条结论

| 账行 | 结论 | 状态建议 |
|---|---|---|
| **L207** `BroadcastCollectedSpiritStone` | ✅ **已由 `B88-E1`（2026-10-27）落地**，本笔复核后**无需再改**。预调实参 = 那颗残骸（`RuleCore.cs:12403`），棋盘跳排除它自己（`preCalled: u`），手牌跳已关 | **A** |
| **L214** `BroadcastSacrificeResolved` | ✅ 待死那一半**已由 `E1P01`（2026-10-26）落地**（`RuleCore.cs:12638` 传 `includePendingDeath: keyword==Sacrifice`）。残下的「无卡单位」**不可达**（没有 `CardDef` 就没有监听器）⇒ 不是差异 | **A**（建议，见下） |
| **L681** `ActivateBloodThirst` | 🔧 **本笔补上缺的 ③④ 两闸**（`RuleCore.cs:12461`）：`p == ctx.Active` · `!ctx.IsPendingDeath(u)` | **A** |
| **L696** `CanPlayWithCodexNow` | 🔧 **本笔新建**（`RuleCore.cs:15816`）：四道闸逐条照 `.c` | **A** |
| **L702** `CancelAttack` | ⛔ **无代码改动**：① pendingDamage 我们**没有台账**（现算）⇒ 「清空」无对象；③ `+0x350` 已由 `B9_E5ENG` 查清「引擎侧 8 个调用点全传 0 ⇒ 恒无差」；④ `PrepareAttack(false)` 属表现层（批 9） | **B**（无可落点） |
| **L815** `OnTurnEnd` | 🔧 **本笔补上 Courtesy(43) / Resolution(45) 两跳**（`RuleCore.cs:2512` 调用、`:15835` / `:15859` 实现），位置按 `.c` **前后夹住** `TurnEnd(40)`。⚠️ 未做：`+0x230` 那道读不出语义的闸、`AddDisappearFromHand` 的**次序**、`CancelAttack(0)` 半 | **B**（4 缺 → 已补 2） |
| **L818** `OnTurnStarted` | 🔧 **本笔补上 ②③ 两跳的【识别】**（`RuleCore.cs:2262` 调用、`:15891` 实现、`:15917` 出声）。⚠️ **执行口仍无**（手牌卡没有 `UnitState`）⇒ 命中时只 warn，不发事件 | **B** |
| **L863** `RemoveTraitSilently` | ⛔ **无代码改动**（`UnitState.cs` 是 E4 的文件）。本笔给出**「结构差、净等价」的证明**（§3-c）⇒ **请调度台裁定，不自行降 C** | **B**（等裁定） |
| **L871** `ResolveDeadCard` | ⛔ **无代码改动**：① 入口已在效果层、接线**等批次 6**；③ `skipBacklash` 两闸**未建模**（本笔未做） | **B** |
| **L982** `UsedActiveAbility` | ⛔ **无代码改动**：④⑤ 两条 Scaffold 信号（705 / 738）是**另一套枚举**（我表里连 738 都没有）⇒ 不臆造映射；⑦ 的「按 `+0x36` 标记摘 effect」循环未落 | **B** |

---

## §2 typecheck 读数 + **每条该怎么验指纹**（调度台点名要的）

**读数**：`TMPDIR=/tmp/wf_B89E2b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑了两轮都 0/0）。

| 账行 | 指纹影响（预判） | 同步点怎么验（命令 + 期望方向 + 反向对照） |
|---|---|---|
| **L681** | **预期无** —— 只发 `EvtKind.Trigger` 事件（不进 `ctx.Events`） | `ruleprobe play check` 对指纹（应不变）。反向对照：把 `p != ctx.Active` 去掉，让**对手回合**打出/使用嗜血单位 ⇒ `play all dump` 的 `Events` 列应多出/少掉触发行（现池嗜血卡在对手回合拿不到 `AttacksThisTurn==1` ⇒ 预期无差，**不能在夹具上观测就不算验过**） |
| **L696** | **无**（纯查询、无调用方） | `ruleprobe` 直接调本口三档：①「自带 codex 且 费用 == 剩余法力」⇒ `true` ② 法力多 1 ⇒ `false` ③ `Active` 换对面 ⇒ `false` |
| **L815** | **预期无**（两跳各自只在 `AbilityTriggerPresence == 1` 时才调） | `play check` + `play all dump` 逐字节 diff（应不变）。**判别式**：临时把 `AbilityTriggerPresence("Courtesy")` 判据改成恒 `true` ⇒ 每张在场卡各多一条 `EvtKind.Trigger`（证明通路真的通着，不是死代码） |
| **L818** | **无**（只 `Debug.LogWarning`） | 造一张「手牌里带 `TurnStarted(160)` ability」的夹具卡 ⇒ 期望日志出现那条 warn、且**没有任何** `EvtKind.Trigger`；反证：把判据改成 `!= 1` ⇒ warn 消失 |
| L207/L214/L702/L863/L871/L982 | **无**（本笔未改代码） | —— |

🔴 **本批的指纹风险点（上一批通报的那条仍然成立）**：批 2 的 `TriggerCodex` 改实参（`RuleCore.cs:13274`）**才是**本会话唯一真正可能改指纹的一笔 ⇒ 同步点跑 `play check` 时请把它一起算进去。

---

## §3 顺手发现（⛔ 我没有顺手改别人的东西）

**a) 账 `L214` 的「还差」已经过期。** `E1P01`（2026-10-26）已经把 `triggers:sacrifice` 那一族的快照改成 `includePendingDeath: true`（`RuleCore.cs:12626-12641` 有完整判据摘录）。⚠️ 我在账补丁里填的状态建议是 **A**，但**主表那一行归调度台改**。

**b) `L815` 的 `+0x230` 那道闸读不出语义。** `CardScript__OnTurnEnd.c:55-68` 的那句 `ObscuredBool(card + 0x230)` 包住了 `OnTrigger(0x2b=43 Courtesy)`；`+0x230` 在 `OnTurnEnd` 之外的读点我没找到第二个可对照的 ⇒ **不猜**，本版只按「卡上真有 Courtesy trigger」发（池内 0 张）。谁要补，得先查 `+0x230` 的写点。

**c) `L863` 的「净等价」证明（请调度台裁）。**
- 原版 = **两张 `List<DefinedTrait>`**（`+0x128` 当前 / `+0x130` 默认，见 `CardScript__RemoveTraitSilently.c:1-55` 的 `List.Find` → `List.Remove` → `List.Contains`(默认表) → `List.Remove`），删的是**一条**；我们是**一张合并字典** ⇒ 同名全删。
- **唯一调用点** = `CardScript__TransformIntoRemnant.c:74`（倒序扫 `+0x128` 快照、逐条 `RemoveTraitSilently`）⇒ 摘完**表必空**，所以「删一条 vs 全删」的**终态相同**。
- 我们那条已按原语义接线：`RuleCore.cs:9877` 的 `rem.ClearAllKeywords()`（先取快照再删 = 原版倒序）。
- ⇒ **判「结构差、净等价」**；按调度台指令**不自行降 C**，请裁定。

**d) `L982` 的两条 Scaffold 信号（705 / 738）与我们的事件词表不在同一个枚举空间。**
`TriggerAbilitySignal` 的 id ≠ `AbilityTrigger` 的 id：我们的 `CardDef.AbilityTriggerNumberOfName` 最大只到 `Oath = 730`，**738 压根不在表里** ⇒ 本笔**拒绝臆造映射**（按红线「判据与实现冲突 ⇒ 停下给反证、宁可拒做」）。要接，只能先建一条 Scaffold 信号通道（表现层/Scaffold 线）。

---

## §4 没查清

1. **`L815` 的 `AddDisappearFromHand` 次序**：原版它在**逐卡** `OnTurnEnd` 的同步段（`:147`），而我们的 `SweepEphemeral` 排在**整段 turn_end 之前** ⇒ 与「再生 / 毒 / 残骸摧毁」的相对次序不同。**影响面没查**（手牌里的 `ephemeral` 卡与场上再生单位同时存在时才会现形）。
2. **`L815` 的 `CancelAttack(0)` 半**：原版逐卡判 `cardState ∈ {3,17}` 就撤攻击；我们的攻击没有「进行中」这个中间态（同步结算）⇒ 大概率**无对象**，但**没逐句证**。
3. **`L871③` 的 `+0x222`**：我按字段位置推断它是「跳过 Backlash」那一位（`ResolveDeadCard.c:233` 的 `*(char*)(dead + 0x222) == '\0'` 才进 `TriggerOnMinionDeath`），但**没有字段名正证**（没找到写点）。
4. **`L982⑦` 的 `+0x36`**：`activeEffects` 条目上的这个标记位语义未解，也没查我们的 `ActiveEnchantment` / `_buffs` 有没有对应位 ⇒ **没落**。

---

## §5 交接给批次 6 / 下一棒

**本笔新增/改动的符号（现读行号，⛔ 不需要重钉）**

| 文件 | 符号 | 行号 | 方式 |
|---|---|---|---|
| `RuleCore.cs` | `EmitBloodThirst` 的闸 | **12461** | 改一行（加 ③④ 两闸），行数不变 |
| `RuleCore.cs` | `EndTurnCore` 里那一行 | **2512** | 改一行（`EmitTurnEndCourtesy` + `ResolveAtTurn` + `EmitTurnEndResolution` 同行），行数不变 |
| `RuleCore.cs` | `BeginTurn` 里那一行 | **2262** | 改一行（+ `BroadcastTurnStartedToHands`），行数不变 |
| `RuleCore.cs` | `CanPlayWithCodexNow` | **15816** | 新增（类尾） |
| `RuleCore.cs` | `EmitTurnEndCourtesy` / `EmitTurnEndResolution` | **15835** / **15859** | 新增（类尾） |
| `RuleCore.cs` | `BroadcastTurnStartedToHands` / `NoteHandTurnStartedNoChannel` | **15891** / **15917** | 新增（类尾） |

- 文件总行数 **15780 → 15928**（+148 **全部追加在类尾**）；12262 之前的所有账落点**零漂移**（三个改动点都是「一行换一行」）。
- **给批次 6 的提醒**：① `L871①` 的接线点 = `CleanupDeaths`（`RuleCore.cs:10086` 一带）**+** `ResolveTriggerRequiem`（效果层），**必须等 230/435 分派**再动；② 类尾已被占用的方法名：`PeekNextCards` / `IsThereValidAbilityTarget` / `AddResetDuty` / `ResolveResetDuty` / `GetCardsInCemetery` / `CanPlayWithCodexNow` / `EmitTurnEndCourtesy` / `EmitTurnEndResolution` / `BroadcastTurnStartedToHands` / `NoteHandTurnStartedNoChannel` ⇒ 追加时别重名。
