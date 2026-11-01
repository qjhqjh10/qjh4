# `B116_W19` · `ENG-07` `jam` 四类真缺口

> 执行代理 `B116_W19` · 2026-11-02 · 第一百一十六会话。施工面 = `资料/完全复刻/判据/B116_R7_ENG07剩余四条施工面.md` §1·2。
> 判据一律以 `D:/2/tools/decomp_full/**.c` 为准（本次逐份现读，非引用）。
> ⛔ 未改活状态表 / 账本 / 基线；未跑 Unity；未动 git；未联网。

## §0 一句话结论

**4 条：3 条做完并配断言，1 条（④ 誓约闸）只做到「AI 直调口」这一层 —— 中央那个方法 `RuleCore.CanPayOathChoice` 在 `EffectResolver.cs`，归 `B116_W6` 独占 ⇒ 停手交回（补丁文本见 §5）。**
另发现施工面 §1·2④ 的根因描述**不成立**（见 §4）—— AI 路径**今天不漏**，已在报告里如实标。

## §1 逐条：原版判据 → 我们落点 → 改前/改后 → 断言

### ① `cardTraits` 口径缺口（横跨 6 个读点）—— ✅ 做了

| 项 | 内容 |
|---|---|
| 原版判据 | `CardScript__SetBaseCardTraits.c:37-42`：`rawCard.CardTraits(+0x230)` **整表**拷进 `this+0x128`；`EntityScript__HasCurrentTrait.c:17-33` 扫的就是这张表（正常档＝当前表 ∨ `activeEffects` 的 `buffType 3`；残骸档 `:54-69` 只认 `buffType 90`） |
| 我们缺什么 | `UnitState.Has()`（`UnitState.cs:968`）只读 `_keywords`（= `keywords` 列）；`cardTraits` 走另一本账 `CardDef._cardTraits` ⇒ 只在 `cardTraits` 里写 `jam` 的卡，**一个读点都发现不了** |
| 改哪 | `UnitState.cs` **类尾**（`:4351` 之后，账最深落点 = `:4274`）新增 `public bool HasCurrentTrait(string)`；新字段 `bool _baseTraitsStripped` 同处 |
| 读点换口（本批白名单内，行数零变化） | `RuleCore.cs:10943`（施放者被 jam ⇒ 摘 extrinsic 手牌效果）· `RuleCore.cs:16881`（被 jam 不能取消攻击）· `SimpleAI.cs:1536`（`HasSpecialAttackScoring`）· `UnitState.cs:2398`（`HasWhileInPlayAbility` ⑤ pack） |
| 读点**没**换（文件不归本批，⛔ 未碰） | `ConditionEval.cs:501` · `EffectResolver.cs:11867` · `EffectResolver.cs:18595` · `EffectResolverPartial_B88E2.cs:205` —— **交回调度台**（各是 1 行 `Has(` → `HasCurrentTrait(`，行为今天等价，池内 0 张卡差异） |
| 🔴 补的一处**连带**语义 | 原版 jam 的那一句是对**当前表里每一条**调 `RemoveTrait`（`ReactToUnitJammed.c:96-113` 先快照 `+0x128` 再逐条摘）⇒ 卡面那批也会被摘、`SetBaseCardTraits`（`ResetToValuesInHand.c:154`）再灌回。我们的 `RemoveAll` 只清 `keywords` 列 ⇒ 用 `_baseTraitsStripped` 一位补：`ApplyJam` 置真（`:2234` 同一物理行）· `ResetToValuesInHand` 置假（`:2643` 同一物理行）。**没有它，`HasCurrentTrait` 在 jam 之后会比 `Has` 更松** |
| 断言 | A1（`cardTraits` jam ⇒ 命中；前提先断 `Has` **不**命中）· A2（keywords 档照旧）· **A3 灭自证（没 jam ⇒ 假）** · **A4 灭自证（残骸档 ⇒ 假）** · **A5 灭自证（`MarkTraitRemoved` 后 ⇒ 假）** · A6（jam 后卡面 `pack` ⇒ 假，且 `jam` 本身 ⇒ 真） |

### ② `HasWhileInPlayAbility` 判反 —— ✅ 做了（本件最要紧的一条）

| 项 | 内容 |
|---|---|
| 原版判据 | `CardScript__HasWhileInPlayAbility.c:10-22`：`cVar2 = HasCurrentTrait(0x82); if (cVar2 == 0) { 查 ability 20 / 240 }` ⇒ **jam 只跳过**这两查，**不早退**；随后 `:23-25` 继续判 `HasEnchantment(20/240) ∨ HasCurrentTrait(610 pack) ∨ activeEffects`，全不成立 `:54-56 return 0` |
| 改前 | `UnitState.cs:2372` `if (Has(KeywordTable.Jam)) return true;` —— 把「跳过」读成了「命中」 |
| 改后 | `:2371-2375` `bool jammed = HasCurrentTrait(KeywordTable.Jam);`（**零行数变化**，`:2375` 改成 `if (!jammed && Card != null)`）；`why` 语义不变 |
| 影响面 | 唯一生产消费者 = `HasWhileInPlayAdjacentEffect` ① (`:2429`)；池内 0 张带 jam ⇒ 今天零行为变化 |
| 断言 | B1（未 jam ⇒ 真）· **B2（jam ⇒ 假；改前恒真 ⇒ 红）** · **B3 灭自证（jam 后挂 `pack` ⇒ 仍为真，「jam ⇒ 一律 false」过不了）** · B4（无能力 ⇒ 假） |

### ③ `GetAbilitiesForTrigger` 的 jam 闸（R4）—— ✅ 做了（doc + 断言，**零代码**）

| 项 | 内容 |
|---|---|
| 原版判据 | `RawCardScript__GetAbilitiesForTrigger.c:26-46`：`HasCurrentTrait(jam)` 为真 ⇒ **不走**「按 `trigger(+0x10)` 过滤自己 `cardAbilities(+0x290)`」那一支，只走 `:47-88` 的附魔支 |
| 为什么本层加不了 | `CardDefPartial_B94B16.cs:181 AbilitiesForTrigger(int)` 是**纯 `CardDef`**、拿不到实例（施工面 §1·2③ 已判） |
| 改哪 | 同文件 `:177` **同一物理行追加** doc：写死「本口 = 原版的**非 jam** 那一支；jam 那支的等价位在调用方 `TriggerAbilitiesPassGates` ①（`EffectResolver.cs:18595`）」，并写清「`AbilitiesForTrigger` 现读**生产调用点 = 0**（只有 `RuleEngineTest_EngineB94B16.cs` + 注释引用）⇒ 属「原版有、按 18·0 照补」」 |
| 断言 | C1（未 jam ⇒ `FireTriggerAt` 入队 = true，夹具照 `EngineB94Z2.cs:118-128`）· **C2（jam ⇒ 整趟不跑 = false）** · **C3 灭自证（jam 前后 `AbilitiesForTrigger(580)` 逐值相同 ⇒ 证明闸在调用方、没装错层）** |

### ④ `CanPlayWithOathNowInHand` 的 jam 闸 —— ⚠️ **只做到 AI 直调口，中央那半句停手交回**

| 项 | 内容 |
|---|---|
| 原版判据 | `CardScript__CanPlayWithOathNowInHand.c:25`：法力比较**之前** `if (HasCurrentTrait(0x82 jam)) return false`（`:26-36` 才是 `CurrentCost + oathValue <= ManaLeft`） |
| 真缺口在哪 | **`EffectResolver.cs:18058 CanPayOathChoice`** —— 它只镜像了原版的**法力那一条**，缺 `:25` 的 jam 闸 |
| 🔴 为什么停手 | 该文件归 **`B116_W6` 独占（在飞）** ⇒ 本批白名单 ⛔。**补丁文本见 §5，请调度台转交 W6** |
| 本批实际改的 | `Data/SimpleAIPartial_B111E2.cs:106` 之前补上**同一道闸**：`if (card.HasDefaultTrait(KeywordTable.Jam, out _)) return false;`（原版那两个闸本来就是**互相独立**的两句，我们这边 `DecideOathForAi` 是直调口） |
| 断言 | 前提（`OathChoiceNeeded` + 卡面真有 jam）· D0（池闸判否）· **D1（AI 不掷、不摆答案）** · **D1-b（一格预烧随机数都不消耗）** · **D2/D2-b 灭自证（同夹具去掉 jam ⇒ 会掷、净消耗正好 1 格）** |

## §2 三条闸读数（本批收工）

| 闸 | 命令 | 读数 |
|---|---|---|
| 秒级类型检查 | `bash Unity/工具/typecheck.sh` | 运行时 **0** / 编辑器 **0** |
| 账落点闸 | `python -I -X utf8 Unity/工具/check_ledger_points.py` | 21 份 tsv **全 OK**：**漂移 0 · 失败 0**（`战斗逻辑驱动.tsv` A=1133 通过=1133） |
| 结算层对拍 | `bash Unity/工具/ruleprobe.sh play check` | **rc=0**（①-a / ①-b 逐卡全等 + 哨兵 ✓；基线**未改**） |

断言条数：**32 条**（`Check` / `CheckTrue`），全在 `Editor/RuleEngineTest_B116W19.cs`（261 行），入口 `CheckB116W19Jam()`，
已在 `Editor/RuleEngineTest.cs:1306` 的**同一物理行**接上 `Guard(...)`（插入点在第一处 `//` 之前，`RuleEngineTest.cs` 仍是 **39,737 行**、1/1）。

## §3 行数 / 行尾纪律

| 文件 | git diff | 是否零行数变化 |
|---|---|---|
| `Core/UnitState.cs` | 45/7 | 两处改口 1→1、类尾新增 ≈38 行（在账最深落点 `:4274` 之后） |
| `Core/RuleCore.cs` | 11/11（含 `B116_W12` 的既有未提交改动） | ✅ 本批 3 处全 1→1 |
| `Core/CardDefPartial_B94B16.cs` | 1/1 | ✅ |
| `Data/SimpleAI.cs` | 1/1 | ✅ |
| `Data/SimpleAIPartial_B111E2.cs` | 7/0 | 无账按行号引用本文件（现核） |
| `Editor/RuleEngineTest.cs` | 1/1 | ✅ 仍 39,737 行 |

行尾：`RuleEngineTest.cs` 仍是 **CRLF 39,737/39,737**（无裸 CR）；其余 `.cs` 仍是 LF；新文件 LF（同兄弟件 `RuleEngineTest_B116W12.cs`）。

## §4 顺手发现（⛔ 已落账、本批不修）

1. 🔴 **施工面 §1·2④ 的根因描述不成立**：它说「`SimpleAIPartial_B111E2.cs:106` 直调 `CanPayOathChoice` ⇒ 那条路径**漏 jam**」。
   **现核**：同一方法 `:105` 先调 `RuleCore.NeedsToChooseFromPool(...)`，而它 ⑤ 里就有 jam 闸（`EffectResolver.cs:17946-17956`，
   `thisCard == null` 时退回 `card.HasDefaultTrait(jam)`）⇒ **AI 路径今天不漏**。真缺口只在 `CanPayOathChoice` **本身**不镜像原版 `:25`。
2. ⚠️ **`HasCurrentTrait` 的第二来源（`activeEffects.buffType 3 addTrait`）我们有承载**：`AddKeyword` 是唯一授予口（`_keywords`）；
   `buffType 90`（残骸）走 `_remnantTraits`。原版那两条 `activeEffects` 支因此**不需要**在 helper 里另读一次。
3. ⚠️ `HasWhileInPlayAdjacentEffect` ① 的 `Has(KeywordTable.Requiem)`（`:2427`）在原版同一方法里也是 `HasCurrentTrait` ——
   **不在本件（jam）范围**，未动；建议并进下一批「口径统一」扫一遍（`grep "Has(KeywordTable\."` 还有若干处）。
4. ⚠️ `_baseTraitsStripped` 的一个残余：原版对 `cardTraits` 那批也能逐条 `RemoveTrait`，我们只有 jam 这一个入口会摘它们；
   `lose <cardTraits 里那条>` 走 `RemoveAll` 只清 `_keywords` ⇒ 对 `cardTraits` 那批不生效（`EffectResolver.cs:12387-12388`，
   但那 `hadKw` 也是按 `Has` 判的）。**今天池内 0 张可达**（`cardTraits` 与 `keywords` 两列现读无 jam），已如实标。

## §5 交回 `B116_W6` 的一行补丁（④ 中央那半句）

`RuleEngine/Core/EffectResolver.cs:18058` `CanPayOathChoice` 方法体首部（`if (ctx == null ...) return false;` **之后**、
`int oath = ...` **之前**）插入：

```csharp
// 🔴 2026-11-02（`B116_W19` · `ENG-07` `jam` R12）：原版 `CardScript__CanPlayWithOathNowInHand.c:25` 在**法力比较之前**
// 有一句 `if (HasCurrentTrait(0x82 jam)) return false;` —— 本方法此前只镜像了 `:26-36` 的法力那一条。
// ⚠️ 手牌卡没有 `UnitState` ⇒ 只能读默认 trait（= 原版「手牌里被临时授予 jam」的已知差异，同口径见 `CanPlayWithCodexNow`）。
if (card.HasDefaultTrait(KeywordTable.Jam, out _)) return false;
```

配套断言（`CanPayOathChoice` 直测，施工面 §4 原样）：`CardDef` 带 `oath 2` + `jam`、`Energy = 99` ⇒ **false**；
去掉 `jam` ⇒ **true**（灭自证）。落点建议并进 `RuleEngineTest_B116W6.cs`。

## §6 还差什么

- ① 剩余 **4 个读点**没换口（`ConditionEval.cs:501` · `EffectResolver.cs:11867` · `EffectResolver.cs:18595` · `EffectResolverPartial_B88E2.cs:205`）—— 文件不在本批白名单。
- ④ 中央 `CanPayOathChoice` 的 1 行（§5）—— 等 `B116_W6` 释放 `EffectResolver.cs`。
- 本件的 32 条断言**未被执行过**（`RuleEngineTest.Run` 要 Unity，本批 ⛔ 不跑 Unity）；三道闸只证明「编得过 + 落点不漂 + 结算层不变」。
  下一次 `RuleEngineTest.Run` 是它们的第一次执行。
