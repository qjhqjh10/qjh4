# B9 · Q5 · `战斗逻辑驱动` 剩余 B 的路线图（到 A 的路径）

> **快照**：2026-10-26，账 `资料/完全复刻/账/战斗逻辑驱动.tsv`。
> **账在动**：本次分析开始时会话口径是 **697 条 B**；分析过程中另有代理在同一张表上重写/升级，现读 **686 条 B**（16 行离开 B、5 行进入 B；其中 **34 行正文被重写**，本表已按**重写后**的内容重判）。
> **行号 = 当前文件的物理行**；稳定键 = `原版类 + 方法`。若账再变，**重跑分类规则**即可（规则见 §六），不要按行号硬搬。
> **配套**：逐条分类 → `资料/完全复刻/判据/B9_剩余B_路线图.tsv`（686 行 + 表头）。

---

## 一 总表（B1–B7）

| 类 | 含义（离 A 还差什么） | 条数 | 占比 | 权重 | 代价 |
|---|---|---:|---:|---:|---|
| **B1** | 落点已是方法级且判为等价，只差 `判据要点` 逐条 / 观测量 ⇒ **改账** | **229** | 33.4% | 304 | 零代码 |
| **B2** | 落点是占位/文件级/共享/「同上」 ⇒ 补方法级行号范围（必要时拆共享落点） | **148** | 21.6% | 160 | 零代码 |
| **B3** | 落点指到字段/注释/候选/挂错符号 ⇒ 改到方法级声明 | **27** | 3.9% | 28 | 零代码 |
| **B4** | 有已知行为差（顺序/时机/数值/缺支） ⇒ **先改码** | **78** | 11.4% | 112 | 改码 |
| **B5** | 原版判据读不到/失真（形参丢失、系数摊成 `*0`、文件名与内容不符） | **9** | 1.3% | 14 | 补判据 |
| **B6** | 我们没有对应实现 ⇒ **应判 C**（补实现后再升 A） | **12** | 1.7% | 14 | 改码 |
| **B7** | 本域到不了 A：纯表现层（口径六）**166** + 原版死/恒常量/0 调用 ⇒ 判 D **17** | **183** | 26.7% | 203 | 移交/判 D |
| **合计** | | **686** | 100% | **835** | |

**一句话读数**：

- **零代码可升 A = B1+B2+B3 = 404 条（58.9%）** —— 这是最快的一段。
- **要改码 = B4+B6 = 90 条（13.1%）**。
- **判据侧 = B5 9 条（1.3%）**。
- **域外/判 D = B7 183 条（26.7%）** —— 不是「没做完」，是**不属于本域**或**原版无行为**。
- 若把 B7 剔出分母：可做 **503 条**，其中账 404（80.3%）、码 90（17.9%）、判据 9（1.8%）。

> 🔴 **`B7` 不是「剩下 27% 的活」，是 27% 的「不该算在本域」**。其中 166 条按 `判据/战斗逻辑驱动_B2A_口径.md` §六「不覆盖纯表现层」**本来就不该记 B**，只是落点文字提到 CardPresentation 才被切片命中 —— 建议**一次裁定**（移交 `战斗界面_HUD` / `特效动画` 线），不要派写手逐条「补落点」。详见 §五。

---

## 二 按「可批量块」列

> 块 = 一次能派给一个写手的单位。**账批**只改账（写 `账/_parts/B9_Q5_<块>.tsv` 补丁，主表由调度台合并），**不编辑 `.cs`**；**代码批**才需要文件所有权。

### A. 账批（零代码，B1 229）

| 块编号 | 条数 | 权重 | 是什么 | 目标文件（读侧） | 顺序 |
|---|---:|---:|---|---|---|
| `X-入队同步改账` | 23 | 29 | 原版「入队 + 协程逐拍」vs 我们「同步结算」 | `RuleCore.cs` 12 · `EffectResolver.cs` 10 · `ActionPump.cs` 1 | **1**（口径已裁定，见 §六） |
| `X-广播族改账` | 37 | 57 | `Broadcast*` 的跳数/预调/自排除/trigger 名 | `EffectResolver.cs` 19 · `RuleCore.cs` 13 · `Net/NetBattle.cs` 3 | 2 |
| `X-账-CardScript` | 41 | 54 | CardScript 族（已核，补判据要点+观测量） | `RuleCore.cs` 14 · `UnitState.cs` 7 · `EffectResolver.cs` 5 | 3 |
| `X-账-BattleManager` | 72 | 83 | BattleManager 族（含 49 条 R1 模板句） | `RuleCore.cs` 36 · `EffectResolver.cs` 17 · `ActionQueue.cs` 4 | 4 |
| `X-账-其它` | 56 | 81 | 其余类（RawCardScript / MinionManager / PlayerHand / AI …） | `RuleCore.cs` 14 · `EffectResolver.cs` 12 · `CardDef.cs` 9 | 5 |

**门槛**（`判据/战斗逻辑驱动_B2A_口径.md` §一，三件缺一不可）：
① `判据要点` 写「原版做什么 → 差异在哪 → 我们等价在哪」；② 至少一个**可观察量**（`ruleprobe` 两侧数值）；③ **还有一段没读的就别升**。
另：**行号会漂** —— 本次抽 6 处方法级落点，**5 处偏了几行**（`RuleCore.cs:6152` 应在 6160 附近、`:2271` 应在 2276、`:3190` 应在 3197、`:4660` 应在 4666、`ActionPump.cs:1379/847` 同）⇒ 每批升完后在**无 `.cs` 写手窗口**跑一次 `check_ledger_points.py --all-A --fix`（当前 A 档：526 条 · 通过 520 · 漂移 6 · 失败 0）。

### B. 落点批（零代码，B2 148 + B3 27）

| 块编号 | 条数 | 权重 | 是什么 | 目标文件 | 顺序 |
|---|---:|---:|---|---|---|
| `X-BM落点-规则` | 64 | 64 | `BattleManager` 116 条的「同族落点（未逐条核）」占位；**其中 9 条原版是协程 kickoff**（真体在 `_003C…_003Ed__NN__MoveNext`，只读 `BattleManager.cs` 会误判「没逻辑」） | 待定位：RuleCore / EffectResolver / ActionPump / BattleContext | **6**（最重，读原版体） |
| `X-落点补行号` | 51 | 60 | 文件级 / 「同上」/「RuleCore 的 X」无行号 | 待定位 | 7 |
| `X-AddTrigger族` | 21 | 21 | 落点仍写「我们无动作队列」（**已过期**）⇒ 定位 `FireTriggerAt`/`BroadcastWhen` 行号 | `RuleCore.cs` / `EffectResolver.cs` | 2（与 `X-入队同步改账` 同族） |
| `X-Resolve共享落点` | 12 | 15 | 「§4.1 共享落点 ⇒ B」：`DeclareAttackCore` / `CleanupDeaths` / `EndTurnCore` 一段代码承接多个 `Resolve*` ⇒ **给行号范围 + 写清如何分别满足**即算 A | `RuleCore.cs` | 5 |
| `X-字段落点-引擎` | 26 | 27 | 落点是**字段**（`PlayerState.Hand/Board/Deck/Warlord/Energy` 等）⇒ 改到方法级（`Units()` / `IsInDeck()` / `Count()` …） | `PlayerState.cs` 22 · `Aura.cs` 1 | 1（最快） |
| `X-字段落点-其它` | 1 | 1 | 候选注释行 | `TraitFrames.cs` | 1 |

### C. 代码批（B4 78 + B6 12 = 90）

> **文件所有权**（同文件只能一个写手，**跨块也要合并给同一人**）：`RuleCore.cs` ≈ **48** · `EffectResolver.cs` ≈ **27** · `UnitState/CardDef/Aura/BattleContext/TutorialScript/AiTargets` ≈ **9** · `CardPresentation`（`BattleDriver/MulliganPanel`）≈ **4** · `ActionPump.cs` 1。

| 块编号 | 条数 | 权重 | 是什么 | 目标文件 | 与谁同写手 |
|---|---:|---:|---|---|---|
| `X-差异-RuleCore` | 40 | 58 | 结算/攻击/回合/卡牌状态的真差异（疲劳绕开减伤、pack 多算自己、`RemoveTraitSilently` 合并字典、Resolve 缺支…） | `RuleCore.cs` 35 · `EffectResolver.cs` 3 | 与 `X-差异-洗牌随机`（4）、`X-休眠缺口` 的 RuleCore 2 条**同一写手** |
| `X-差异-洗牌随机` | 4 | 5 | Fisher–Yates vs 原版「按名次挑选」+ RNG 消费形状（预烧 100 + 下标） | `RuleCore.cs` | 同上 |
| `X-差异-EffectResolver` | 22 | 31 | 广播跳数/预调、DoGive/DoJam/DoReturn 缺格、mulligan/手牌语义 | `EffectResolver.cs` 21 | 与 `X-休眠缺口` 的 EffectResolver 1 条**同一写手** |
| `X-差异-引擎数据` | 9 | 13 | `_keywords` 合并 vs 分层、`Has` vs `HasCurrentTrait`、成就统计归属、默认值缺口 | `UnitState.cs` 2 · `CardDef.cs` 2 · `Aura/BattleContext/TutorialScript/AiTargets/Enchantment/AchievementTally/ConditionEval` 各 1 | 独立写手 |
| `X-差异-表现件` | 2 | 3 | `BattleDriver` 侧的演出/目标池差异（含「目标池在落位前算」） | `BattleDriver.cs` · `MulliganPanel.cs` | 独立 |
| `X-差异-ActionPump` | 1 | 2 | `CanTriggerAbility` 六道闸只落了一部分 | `ActionPump.cs` | 独立 |
| `X-休眠缺口` | 7 | 8 | **确缺实现但今天 0 可达**（TurnSetup/TurnStarted/Poisoned 三个广播从未发、ta=230 未实现、`duplicitous` 整条关键词缺…） | `RuleCore/EffectResolver/AiTargets/UnitState` | 并入对应文件写手 |
| `X-真缺口` | 5 | 6 | 有可达性、确实没有实现（换位 `ResolveExchangeMinion`、`SetHealthToZero`、`ChangeMaxHealth`、`WarlordMastery`） | `RuleCore` / `UnitState` / `EffectResolver` / Shell | 并入对应文件写手 |

> **B4 里 21 条今天 0 可达**（`0 张`/`池内 0`/`不可达`）—— **仍要做**（铁律 11），但排在**可达的 57 条之后**。

### D. 判据批（B5 9）

| 块编号 | 条数 | 权重 | 是什么 |
|---|---:|---:|---|
| `X-判据读不到` | 9 | 14 | `SetNewData`（三个形参被反编译成 `default`）· `GetTargetInPos`（求交系数摊成 `*0`）· `GetAdjustedDamage`（伪码把 dropPod/dodge 反成死存）· `SetPray`（清零点 0 命中，可能在 724 条无方法体里）· `GetMinionList`（排序键读不出）· `RemoveSwarm`（回滚点未查到）· `GetWhispersCardsCount`（**文件名与内容不符**）· `UpdateManaLeftAccumulation`（规则书未写死）· `UpdateName`（字符串拼法失真） |

派**只读**代理：VA 级反汇编（`工具/read_literal.py` / Ghidra `.c`）+ 另一份反编译交叉；产出写 `判据/B9_判据补齐.md`，再回 A 批升级。

---

## 三 到 100% 的路径（顺序与理由）

```
W0 前置（调度台，0.5 天）
  ├─ ① 裁定 B7-166 的口径：移交 CardPresentation 线（不是本域的 A）
  ├─ ② 订正旧措辞：R1 那批「入队层缺席」已过期 —— ActionQueue.Enqueue + ActionPump:562
  │     已存在（4 道 + 泵点 + 非法道丢弃，27 个生产调用点）；缺的只是「把 Add* 族接线」
  │     ⇒ 这是一条独立施工项，不是 B 升 A 的阻塞（见 §六）
  └─ ③ 在「无 .cs 写手」窗口跑一次 check_ledger_points --all-A --fix（重钉漂移行号）

W1 账批（3–4 个写手并行；每批只写自己的 _parts 补丁）
  ① X-入队同步改账 23 → ② X-广播族改账 37 → ③ X-账-CardScript 41
  → ④ X-账-BattleManager 72 → ⑤ X-账-其它 56        （共 229）
  每批必须带 ruleprobe 观测量；升完立即 --fix 一批

W2 落点批（3 个写手并行）
  X-字段落点-引擎 26 + 其它 1（最快，先做）
  X-AddTrigger族 21（与 W1-① 同判据，可同一人）
  X-BM落点-规则 64 + X-落点补行号 51 + X-Resolve共享落点 12（读原版体，最重）
                                                    （共 175）

W3 代码批（按文件所有权 4–5 个写手；与 W1/W2 可并行，但观测量等落地后再补）
  RuleCore.cs（40+4+2） → EffectResolver.cs（22+1）
  → 引擎数据 9 → 表现件 2 → ActionPump 1 → 休眠缺口 7 → 真缺口 5   （共 90）
  ⚠️ 代码改动会改变 W1 的观测量 ⇒ W1 的「差异类」行等 W3 落地后补读数再升

W4 判据批（只读代理，与 W1–W3 并行）
  B5 9 → 反编译/VA 补判据 → 回 W1 升级

W5 域外处置（调度台一次裁定）
  B7-166 移交 · B7-17 判 D（带时间限定） —— 本清单一次关闭 183 行
```

**为什么是这个顺序：**

1. **先账面后代码**：404 条零代码行里没有「顺序依赖」，先做能立刻把 58.9% 变成 A；
2. **代码批不阻塞账面**：除 B4 自己的 78 行外，只有「B1 里带差异说明」的少数行需要等代码落地后补观测量；
3. **B5 必须最后回填**：判据没补齐之前改账就是「拿我们的实现证明我们的实现」（§二 自证）；
4. **W5 放最后但收益最大**：一条裁定关 183 行（不减真实工作量，但让「本域完成率」口径变干净）。

---

## 四 哪些「根本到不了 A」

| 类别 | 条数 | 为什么到不了 A | 出路 |
|---|---:|---|---|
| **B7 · 纯表现层** | 166 | `判据/战斗逻辑驱动_B2A_口径.md` §六明写「不覆盖纯表现层」；落点里提到 `CardPresentation` 只是切片命中 | **移交** `战斗界面_HUD` / `特效动画` 线；本域改记 D（「不归本域」也是 D 的理由之一，见方案 §4.1） |
| **B7 · 原版死/恒常量/0 调用** | 17 | 原版体为空或恒 `false`/`null`（`GetCampaignScriptedData` 返回 `null`、`ShouldUseEnviromentalEffects`/`ShouldFadeMulliganCards` 恒 `false`、`RunCheckAchievements` 空体）；或 0 调用/0 读者（`GetMarksOfChaos`、`AdjustMinionBoardPos`、`GetDummyDistance`）；或远端 liveops 数据（bot 卡组 3 条） | **判 D**（必须写「查过哪几处、0 命中」+ 时间限定「当前 build；数据出现需重跑」） |
| **B5 · 判据读不到** | 9 | 反编译产物丢值/文件名与内容不符；**不是不能做，是「现在没有可核的判据」** | 补 VA 反汇编后可升 A（不是永久到不了） |
| **B6 · 无实现** | 12 | 我们确实没有对应实现 | 补实现后**能**升 A；其中 7 条今天 0 可达（休眠） |

> **结论**：**永久到不了 A = B7 的 183 条**（除非把本域口径改成「含表现层」，那就该换线做，而不是在这本账里升 A）。B5/B6 是「暂时」，不是「永远」。

---

## 五 最大的块（单独展开）

### 5.1 条数最大：`X-表现层移交` 166 条

- 组成：`BattleManager` 79 · `CardScript` 56 · `MinionManager` 8 · `ActiveWarlordAbility` 7 · `RawCardScript/RemnantBody/CardTraitCollection/EntityScript/MatchData/…` 16。
- 形态：`AddCardAnim` / `StartSpellFrom` / `StopHandAnim` / `PlayAnim` / `RemnantBody.*` / `CardTraitCollection.GetTrait*ForIcons` / `MinionManager` 的影子与几何 / `CardScript` 的 `Display*/Destroy*/Anim` 族。
- **它不是「活」，是一次口径裁定**：按 §六 的 B2A 口径，它们**不属本域**。建议调度台一次性决定：
  - **首选**：在 `战斗逻辑驱动.tsv` 里改 `D`（备注写「本域不覆盖纯表现层（口径六）⇒ 移交 `战斗界面_HUD`/`特效动画` 线」），并把清单复制给对应线；
  - 若用户要求「表现层也要 A」，那就**换线做**（那边的判据是预制体/序列化字段 + 实拍，不是本账的反编译方法体）。
- ⛔ 不要派写手逐条「补落点」——166 条里 80 条压根没有 `.cs` 落点（落点写「—（表现层）」），补不出来。

### 5.2 工作量最大（真实要读判据）：`X-BM落点-规则` 64 条 + `X-账-BattleManager` 72 条

- 来源：`BattleManager` 116 条「（原版 `X`，判据见 `BattleManager.cs` 方法体）」占位行。本次已把 **52 条**分流到 B7（纯表现/UI 31 条 + 体为空/弱谓词 9 条 + 逐条复核后再移 12 条），剩 **64 条规则侧**。
- **坑（已实测）**：这 116 条里 **9 条原版是协程 kickoff**（`_003C<Method>_003Ed__NN`）——只读 `BattleManager.cs` 会看到 `return (IEnumerator)new NullReferenceException();` 这种假象，**必须读 `decomp_full/<Method>_d__NN__MoveNext.c` 或 cpp2il 的状态机体**；另有若干条是纯转调（`CancelEffectsFromDisappearingUnit` → 四次 `…FromQueue`）或恒常量（已在 B7）。
- 建议**一个写手一次只做 8–10 条**（读体 + 定位 + 改账一条龙），64 条约 7 轮；不要和 `X-账-BattleManager` 拆成两个人（同一批原版方法，判据读一次两组都用）。

### 5.3 代码瓶颈（与 `B9_J1` 施工单对得上）

- `RuleCore.cs`：**42 条** B4/B6 落点（+ 4 条洗牌 + 2 条休眠，同文件 ⇒ **必须同一个写手或严格串行**）。
- `EffectResolver.cs`：**27 条**。
- 其余合计 21 条（UnitState/CardDef/Aura/BattleContext/TutorialScript/AiTargets/ConditionEval/Enchantment/AchievementTally/WhenEvent/ActionPump/BattleDriver/MulliganPanel）。
- `J1` 报的「RuleCore 74 / EffectResolver 54」是**全量落点计数**（含 A/B 全部）；本表是**只算 B4+B6** 的 42/27 —— 口径一致，量级同向。
- **并行约束**：`RuleCore.cs` 与 `EffectResolver.cs` 各只能一个写手；建议 W3 先派这两个，其余 4 个文件写手并行，最后合 `X-休眠缺口`/`X-真缺口` 的零散落点。

---

## 六 口径与已知坑（给下一个代理）

1. **「入队 vs 同步」已由调度台裁定**（`调度台日志.md:234`）：这是引擎架构级差异、**允许升 A**，条件 = ①`判据要点`写机制差异与等价依据 ②**至少一个可观察量两侧数值** ③没核完的不许归进这一族。⛔ 不是通用后门。
2. **「入队层缺席」是旧措辞，已过期**（`调度台日志.md:4623-4624`）：`ActionQueue.Enqueue` 有 4 道 + 泵点 + 非法道号丢弃，生产唯一入口 `Core/ActionPump.cs:562`（`EnqueueAbility` 尾部，27 个生产调用点）。**缺的只是把 `Add*` 族逐条接进去**（结构化载荷 + `ResolveAction` 式分派）—— 那是一条独立施工项，**不是 B→A 的阻塞**。W1-① 那 23 条按「等价 + 观测量」写即可。
3. **B2A 口径只适用 `Add*`/`Resolve*` 族**；其它方法走方案 §4.1（共享落点可用，但必须给行号范围 + 说明如何分别满足）。
4. **口径六**：`判据/战斗逻辑驱动_B2A_口径.md` §六「不覆盖纯表现层」—— B7-166 的依据。
5. **行号漂移**：B 行落点未受 A 闸维护，抽 6 处 5 处偏几行。升 A 后立刻 `--fix`；⚠️ `--fix` 只能在「无 `.cs` 写手 + 补丁已合并」时跑一次（并发会丢手工修订，见 CLAUDE.md 铁律 16·8）。
6. **34 行在本次分析期间被并发代理重写**（128–168/61/62/66/133/136/138–145/148/150–153/155/157–161/163–168）。本表按**重写后**内容重判；若再变，重跑分类规则（本文件 §一 的七类定义即为规则），⛔ 不要照搬行号。
7. **抽样深核记录**（B2/B4 抽查）：`RuleCore.cs:6152/2271/3190/4660`、`EffectResolver.cs:1714/11718/3885`、`ActionQueue.cs:230/298`、`PlayerState.cs:19/172`、`UnitState.cs:942`、`ActionPump.cs:847/1379`、`SimpleAI.cs:316`、`BoardSlots.cs:144` —— 其中 5 处需重钉（见 §二 A 门槛段）。
8. **本文件的分类是「离 A 还差什么」，不是「价不值得做」**：按铁律 11，B4/B6 **全部要做**，只有先后（B4 里 21 条 0 可达排后面）。

---

## 七 交付物

- 本文件：`资料/完全复刻/报告/B9_Q5_剩余B路线图.md`
- 逐条：`资料/完全复刻/判据/B9_剩余B_路线图.tsv`（686 行 + 表头，列＝账行号 / 原版类 / 方法 / 分类 / 离A还差什么 / 块编号 / 建议动作 / 目标文件 / 权重）
- 未改任何 `.cs`、未改主账表、未跑 Unity、未动 git。
