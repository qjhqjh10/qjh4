# A6 · B03「引擎 · 动作泵与 AI」施工报告（2026-10-27 · 第八十七会话）

> 批次 = `判据/C档施工批次表.tsv` 的 **B03**（8 行，全在 `账/战斗逻辑驱动.tsv`）。
> 独占文件 = `RuleEngine/Core/ActionPump.cs` + `RuleEngine/Data/SimpleAI.cs`（本报告**只**动了这两个中的 ActionPump，且只动 `IsRightContext`）。
> 补丁 = `账/_parts/B87_A6_B03.tsv`（11 列表头逐字照主表 · `域`=战斗逻辑驱动 · `轮次`=B87-A6）。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未重钉 `Pinned*Md5` · ⛔ 未改任何正本 / 账主表 / 白名单外的 `.cs`。

---

## 〇 一句话结论

**8 行里：1 行真改完（679 IsRightContext：fail-open → 原版 fail-closed + `-1` 打 `🔶` 标记）· 3 行复核后按裁定结案（1062/1063/1064 的 `🔶` 标记在位、数值未动）· 4 行仍有残余、但残余全部落在我这批量**碰不到的文件**里（172 / 178 / 183 / 669），已逐条给出移交落点与可达性读数。**
其中 **669 的「手牌半今天不可达」这条判据已过期** —— 现读有 4 张卡带 `WhileInPlayVariable(240)+context=10`，且 `BL52`/`SW58` 的手牌降价确实没被覆盖（见 §四 顺手发现①）。

| 账行 | 方法 | 本轮结论 | 状态 |
|---|---|---|---|
| 172 | `AddTransformCardWithBuff` | 做不到（缺核心口，三处都白名单外）⇒ 移交 B01+B02+B05 | C |
| 178 | `AddTriggerCourtesy` | 本件侧逐句对上；残余 = `courtesy` 前缀（B05）+ `chosenAbility 400` 分派（B02） | C |
| 183 | `AddTriggerRelentless` | 同上（同一入口）；`relentless` 前缀 **B8_D 已登记** ⇒ 只余常量 + 370 分派 | C |
| 669 | `BroadcastWhileInPlay` | 场上半复核在位（`play check` rc=0）；**手牌半可达性判据过期**，重排为 B05+B01 | C |
| **679** | **`IsRightContext`** | **已改：未定义档改 fail-closed（`LogWarning`+`false`）；`-1⇒true` 保留并打 `🔶 自己设计`** | **A** |
| 1062 | `GetBotConfigMaxSkips` | `🔶` 标记在位、数值未动（P-09） | A |
| 1063 | `GetBotConfigMinSkips` | 同上（恒 0 的理由仍在 doc 里） | A |
| 1064 | `GetBotConfigSkipChance` | 同上；比较口径（`>` + `Next(0,100)`）未动 | A |

---

## 一 逐条做了什么

### 1·1 679 `CardAbility.IsRightContext`（**唯一的代码改动**）

判据（第一权威，本轮全文重读）= `d:/2/tools/decomp_full/CardAbility__IsRightContext.c:14-90`：
六档 `0 / 10 / 0x14(20) / 0x1e(30) / 0x28(40) / 0x32(50)`，**其余值（含负数）⇒ `CustomDebug.LogWarning` + `return 0`**（fail-closed；`param_2 == 0` 走 `FUN_1803f47a0` = NRE 桩，不复制）。

改动（全部在 `Core/ActionPump.cs`，**零行数变化**：改前/改后都是 **2048 行**，账里的落点 `1273` 未漂）：

1. `default:` 支 —— `LogError` + `return true`（fail-open）⇒ **`LogWarning` + `return false`**（原版 fail-closed）；日志文案改成带判据出处（`:84-90`）的出声口。
2. `-1 ⇒ true` —— **保留**（不改行为），按铁律 18 就地打标：doc 头改成 `🔶 自己设计（非原版）：… · … · 调度台 2026-10-27 裁定「保留 + 打本标记」（账 679）`，代码行注释同步。
3. doc 里「其它值（数据坏）⇒ 出声（`LogError`）后按未知处理（返回 `true`）」⇒ 改成「`LogWarning` 后 `return false`（原版 fail-closed）」。

**为什么 `-1` 必须保留而不是一起 fail-closed**（这一条是我这轮核出来的，不是照抄）：

- `Core/Enchantment.cs:906` 把 `AbilityContextOfAbilityV14(...)` 的返回值**不经任何闸**直接喂进 `RuleCore.IsRightContext`；那个方法在「旧卡表 / 该行没落 / 认不出 trigger 名 / `AbilityTriggerPresence != 1`」时返回 `-1`（`ActionPump.cs:709-722`）。
- 若 `-1` 也 fail-closed ⇒ 这批附魔的 `AboutToAttack` **不再取消攻击** = 把一个「数据缺口」变成「行为改动」，方向是改坏。
- 既有用例的文档也写着同一条口径：`RuleEngine/Editor/RuleEngineTest_EngineA1.cs:411-413`「`-1` 不是『认不出的值』，是『我们没有那一列』，所以走回退而不是判假」。

### 1·2 1062 / 1063 / 1064 `MatchData.GetBotConfig{Max,Min}Skips / GetBotConfigSkipChance`

- 复核 `Data/SimpleAI.cs:1421`（`KnobsOf` 的方法 doc 头）= 完整的 `🔶 自己设计（非原版）：<这是什么> · <为什么必须自己定> · <谁定的+日期>` 三要素；四个 `case`（`:1440-1443`）行尾另有 `🔶 自定值（非原版）`。
- **数值一格没动**（P-09）；`MinSkips 恒 0` 的理由、`skipChance > roll` 的比较口径（原版 `> GetRandomInt(0,100)` vs 我们 `> ctx.AiRng.Next(0,100)`）都还在 doc 里。
- ⇒ 按 2026-10-26 裁定「保持现状 + 打标记」**结案**（账升 A；值仍是自设计，⛔ 别当原版值）。

### 1·3 178 / 183 `AddTriggerCourtesy` / `AddTriggerRelentless`（复核 + 移交）

- 本件侧（`ActionPump.cs:2013 EnqueueGrantedTrigger` + `:1894 ExtraTriggerName` 的 `courtesy→Courtesy/43`、`relentless→Relentless/230`）**逐句对上**两份 `.c`：
  - `Add*` 段 = 纯入队、不判前置、道 = 调用方 `priority`（`BattleManager__AddTriggerCourtesy.c` 体内三句：ctor(65,…) / `+0x10=targetCard` / `AddAutoActionToQueue`）。
  - `Resolve*` 段 = 出队闸 `IsInPlayOrDying`（泵点 `RequireActorInPlayOrDying` → `IsOnBoardOrDying`）＋ `OnTrigger` 三槽 `this=target / acting=casterCard / target=null`（`ResolveTriggerCourtesy.c:28-264`）。
- 残余（**都不在本批文件**）：
  - `courtesy`：`CardDef.Prefixes` 无该前缀、`KeywordTable` 无 `Courtesy` 常量 ⇒ 数据真写会被 `Normalize` 静默丢弃 —— **B05**。
  - `relentless`：**前缀 2026-10-26 已由 `B8_D` 登记**（`CardDef.cs` 的 `Prefixes` = `new[]{"relentless","relentless"}`）⇒ 只剩常量 + 功能上已通（`ExtraTriggerName` 用字面量比对）。
  - 两族的产生口 = 原版 `AbilityLogic__PlayAbility.c` 的 `chosenAbility 400 / 370` 分派 —— **B02**。
- 🔴 顺手按铁律 5 就地订正了 `ActionPump.cs` 类尾 §三 的一句过期话（原文「`Prefixes` 也没有这两个前缀」）⇒ 改成「`relentless` 已登记、`courtesy` 仍缺」，**零行数变化**。

### 1·4 172 `AddTransformCardWithBuff`（做不到，移交）

判据要的三样**全在本批白名单之外**：

1. 出队体 `ResolveTransformUnit` 的等价函数（换卡面 / 换定义 + `buffEffect` / `keepEffects` 两参语义）= 「把一张在场单位换成另一张卡」的核心口 ⇒ `RuleEngine/Core/RuleCore.cs`；
2. 产生这条动作的动词分派 ⇒ `RuleEngine/Core/EffectResolver.cs`；
3. 变身关键词/字段 ⇒ `RuleEngine/Core/CardDef.cs`。

⇒ 移交 **B01 + B02 + B05**。⛔ 没有在 `ActionPump` 里加一个「没有发射口的 `TransformUnit` 解析器」—— 那正是本文件类尾 §八 `synapse` 那条先例明确禁止的死代码（且会插行导致账漂移）。

### 1·5 669 `BroadcastWhileInPlay`（复核 + 可达性重排）

- 场上半**在位**：`ActionPump.cs:143` 的 `Auras.RefreshInPlayNumericAndKeywords(_ctx)`（在 `EmitWhileInPlayPass`(`:157`) 之前 · `ActionScope.Dispose` 里 = 原版 `FinishResolvingAction` 尾部）。
- 机械复核：`ruleprobe play check` **rc=0**（1129 张逐字节对基线全等 ⇒ 这次刷新对结算零副作用，含 `BL18`/`UM18` 的电池读数）。
- 手牌半：**账里「池内 0 张 ⇒ 今天不可达」已过期**，详见 §四 ①。

---

## 二 验收读数

| 项 | 命令 | 读数 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_a6 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 · 编辑器 0**（跑了两遍：代码改完一次、注释改完一次） |
| 结算层对拍 | `bash d:/4/Unity/工具/ruleprobe.sh play check` | **rc=0**；①-a 主档逐卡全等 · ①-b 电池逐卡全等 · ② 基线内容钉住一致（`139e1046…`）；行内出声 0 张 |
| 行数守恒 | `io.open(...,'rb')` 数行 | `Core/ActionPump.cs` **2048 → 2048**；落点 `143 / 157 / 1273 / 1477 / 1894 / 2013` 逐个回读，内容全部未变 |
| 行尾 | 同上 | `CRLF 0 · LF 2048 · 裸 CR 0`（改前改后一致） |
| `ai` 探针 | 未跑（本轮**没有改任何打分**：`SimpleAI.cs` 一个字节没动；`KnobsOf` 的旋钮只影响 `TweakAvailableActions`、不进 `ScoreAction` 分解） | — |
| 卡面解析旁证 | `ruleprobe card "Fate Inescapable" / "Unholy Smite" / "Fenrisian Wolfpack"` | `ASH85` ⇒ `costifcontrol amount=1 payload=[stealth]`；`BL52` ⇒ `lowercost amount=1 payload=[this card] countRef=[board] countScope=[darkpact]`；`SW58` ⇒ `lowercost … countRef=[enemy\|troop\|all] countScope=[board]` |
| 补丁自检 | `python d:/4/Unity/工具/check_patch_conflicts.py` | ✅ 没有跨补丁重复键（当前 `_parts/` 只有这 1 份） |

---

## 三 没做的（+ 卡在哪、回来的条件）

1. **172 `transformUnit` 整条机制** —— 卡在文件所有权（RuleCore / EffectResolver / CardDef）。回来的条件 = B01/B02/B05 任意一批开工。
2. **178 / 183 的两块残余** —— `courtesy` 前缀 + `KeywordTable.Courtesy/Relentless` 常量（B05）· `chosenAbility 400/370` 分派（B02）。今天 0 张可达（见 §四 ②），属休眠档。
3. **669 手牌半** —— 需要 CardDef 收 per-count 自降费列（B05）+ `RuleCore.CostOf` 读它（B01）。
4. **669 的基线重钉 / `Pinned*Md5`** —— 只能是调度台；而且按 2026-10-27 的读数（`play check` rc=0）**当前不需要重钉**。
5. **fail-closed 支的断言** —— 测试在 `RuleEngine/Editor/`（不在本批白名单）⇒ 建议由「自检断言线」补一条「非法 `abilityContext` ⇒ `IsRightContext` 返回 false」。
6. **`BL52`/`SW58` 手牌价的端到端实打验证** —— 要跑 Unity 或改 B01/B05；我只做到机制层（见 §四 ①），**没做**实打。

---

## 四 顺手发现（⛔ 本批不改，请调度台分流）

### ① 669 的「手牌半今天不可达」判据过期，且 `BL52`/`SW58` 有一处可观察差异

- **事实 A（数据，现读 `RuleEngine/Resources/cards_engine.json` v35 / 1129 张）**：`abilityContext` 列里 `WhileInPlayVariable(240) + context=10` 的卡 = **4 张**：`ASH85 Fate Inescapable` · `BL52 Unholy Smite` · `GSC37 Patriarch` · `SW58 Fenrisian Wolfpack`（四张的卡面都是「`This card costs 1 less …`」）。⇒ 「池内没有手牌卡自己的 WhileInPlay ability 样本」这句话**只对旧池成立**。
- **事实 B（代码 + 探针）**：
  - `ASH85` → `RuleCore.CostOf` 的 `CostIfControls`（`Core/RuleCore.cs:1013-1019`；probe `costifcontrol amount=1 payload=[stealth]`）⇒ **手牌价有减**。
  - `GSC37` → `CostOf` 的 `CostPerEnemyHandCard`（`Core/RuleCore.cs:1025-1027`）⇒ **有减**。
  - `BL52` / `SW58` → 只解析成 `lowercost` op；`lowercost` **唯一消费点**是 `EffectResolver` 的 `DoLowerCost`（`:15147`），而它由 `ResolveOps` 在**付费之后**跑（`Core/EffectResolver.cs:1996` 判价 → `:2532` 扣费 → 之后才结算 ops）⇒ **手牌价不减**。
  - 原版等价物 = `BroadcastWhileInPlay` 的手牌扫描 → `UpdateWhileInPlay` → `AddHandEffect`（同源说明本来就在 `Core/Aura.cs:180-187`）。
- **定性**：这是「原版有、我们缺」—— 按铁律 11 **要做**。落点 = **B05**（`CardDef` 收 `This card costs N less for each <X>` 这一族字段）+ **B01**（`RuleCore.CostOf` 每次现算读它，和 `CostIfControls` / `CostPerEnemyHandCard` 同一条路、同一条「必须在 `CostMods.Count == 0` 早退之前判」的纪律）。
- ⚠️ **边界**：我只核到「机制层不到手牌价」，**没做**端到端实打（需要对局里摆 Dark Pact / Hunt Mark 再看挂牌价）。⇒ 建议**先记账**，由 B01/B05 修时一并验。

### ② `chosenAbility` 现读分布（给 178/183 的可达性一个可复算的口径）

`cards_engine.json` v35 全池 `chosenAbility` 值里 **没有 370、也没有 400**（分布从 0 到 480，逐值计数见下），`courtesy` / `relentless` **关键词**各 0 张（`Relentless` 字样出现 20 次全是卡名 / 动画名，逐条看过）。
⇒ 178 / 183 两族 = **休眠档**（时间限定 = 本 build）。

### ③ `ActionPump` 类尾 §三 的一句过期话（已就地订正）

「`Prefixes` 也没有 `relentless` 前缀」不成立（`B8_D` 2026-10-26 已登记）。已在原处改成「`relentless` 已登记、`courtesy` 仍缺」，零行数变化。

### ④ 口径提醒（给后面接手 679 的代理）

`IsRightContext` 的 `-1` 早退**不是**「无脑放行」：它保护的是 `Core/Enchantment.cs:906` 那条**不过闸**的调用点。谁要把它删掉，先改那个调用点（补 `!= AbilityContextUnknown` 的闸或回退旧口径），否则 = 附魔不再取消攻击。
