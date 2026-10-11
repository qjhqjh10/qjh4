# `E9` —— `WhenEvent.cs` 的**关键词触发族·自排除**守卫（`A1562` = `A1399` + `A1397①` 收口）

> 写手：E9 笔（2026-10-11 第十九会话）· 独占文件 = `Unity/MyGame/Assets/RuleEngine/Core/WhenEvent.cs`
> ⛔ 未跑 Unity、未跑 `typecheck.sh`、未动 git、未改任何正本、未碰白名单外的任何文件。
> `git diff --numstat WhenEvent.cs` = **92 / 0**（只此一个文件）· 行尾 = **纯 LF**，改完仍 LF（没整篇重写）。

---

## 一、改动清单

| 行号 | 改了什么（≤40 字） |
|---|---|
| `WhenEvent.cs:1203-1226` | `Matches` 里新增「关键词触发族·自排除」守卫（`SelfOnly` 那一段之后） |
| `WhenEvent.cs:1325-1391` | 新增 `SelfExcludesOnKeywordTrigger(kind)` + 例外表 `KeywordTriggerIncludesSelf` |

两条改动的语义：**`triggers:<kw>` 这一族、且不是 `SelfOnly` 时，不许 `ReferenceEquals(listenerUnit, subject)`**
—— 但**不是**一刀切，见 §三。

---

## 二、判据（原版权威 = 机器码，逐条数过）

工具：`工具/rva_of_method.py` 拿 RVA（表在 `d:/2/tools/all_methods.txt`，`script.json` 里**没有**
`BattleManagerSupport.*` 这些方法）→ `工具/disasm_va.py` 反汇编 →
在**函数体字节范围内**数 `UnityEngine.Object.op_Inequality`（`0x182FFB920`）的 `call` 条数。
base `0x180000000`，DLL = `D:/2/unity_run_ref/GameAssembly.dll`（`BroadcastUnitUsedSacrifice`
的 RVA 与 `E7` 报告里那个 `0x9BA580` **逐字节吻合**，可反证这套算法对）。

| 广播函数 | RVA | 函数体字节 | `op_Inequality` 条数 | 自排除 |
|---|---|---|---|---|
| `BattleManagerSupport.BroadcastUnitUsedSacrifice` | `0x9BA580` | 576 | **3** | ✅ |
| `…BroadcastUnitSwarm` | `0x9BA150` | 576 | **1** | ⚠️ 见下 |
| `…BroadcastUnitMob` | `0x9B8570` | 512 | **1** | ✅ |
| `…BroadcastUnitSummoned` | `0x9B9F30` | 544 | **1** | （另一族） |
| `…BroadcastUnitLanded` | `0x9B8230` | 416 | **0** | ✅（守卫在接收方） |
| `…BroadcastUnitFerocity` | `0x9B7C50` | 496 | **0** | ❌ |
| `…BroadcastUnitDuty` | `0x9B7A60` | 496 | **0** | ❌ |
| `…BroadcastUnitSynapse` | `0x9BA390` | 496 | **0** | ❌ |

**逐条结论**：

1. **`sacrifice` ⇒ 自排除**（`E7` 的结论成立，本笔复算同）：
   `0x9BA625-62B` / `0x9BA64E-657` / 手牌两跳各一条 —— `xor r8d,r8d` + `rdx = 触发源` + `rcx = 卡`
   → `call 0x182ffb920` → `test al,al / je 跳过`，**三跳同形**。
2. **`OtherCard*` 那一族 ⇒ 自排除**（守卫写在**接收方体内**）：
   `CardScript__TriggerOtherCard{Landing,Mob,Codex,SpiritStone,Requiem,Regiment}.c:14` 全是
   `op_Inequality(param_2, param_1)`（`E7` 读的，本笔采信 —— 那是**两个具名实参**的调用，
   不受「Ghidra 掉 `rdx`」那个坑影响）；`BroadcastUnitMob` 的棋盘跳**另外**还有 1 条（双重）。
3. 🔴 **`ferocity` / `duty` / `synapse` ⇒ 【不】自排除**（本笔新查出，与简报前提不符）：
   三个广播的**三跳**都**原样**把触发源 `rdx = param_2` 传下去（`0x1809B7D5A` / `0x1809B7DB5` /
   `0x1809B7E0E` 那些 `mov rdx, rbp`），**一条 `op_Inequality` 都没有**；
   接收方 `CardScript__TriggeredFerocity/Duty/Synapse`（13 条指令的桩）体内也**没有**。
   调用点：`CardScript__UsedActiveAbility.c:32`（Duty）/ `:55`（Ferocity）传的是 **`param_1` = 用能力那张卡自己**；
   `CardScript__TargetedSpellPlayed.c:72`（Synapse）同形 ⇒ **那张卡自己会被自己叫醒**。
4. ⚠️ **`swarm` ⇒ 原版有守卫，但比的不是我们那个 `subject`**：
   `BroadcastUnitSwarm` 棋盘跳 `0x1809BA26C mov rdx, rbp` → `op_Inequality(卡, param_2)`
   → `:0x1809BA2A1 call 0x1806010A0 (TriggeredSwarm)`。
   而 `param_2` = **被并掉那张**：唯一调用点
   `CardScript.<>c__DisplayClass401_0.<ExecuteSwarm>b__0.c:51` 先
   `DestroyUnitSilent(*(param_1+0x10))`、`:54` 再用**同一个** `param_1+0x10` 当 `param_2` 广播
   （`param_1+0x18` = 留下那张，`:45` 收 `AddEffect`）。上游
   `BattleManager__AddExecuteSwarm.c:80` ← `CardScript__ResolveCardPlayed.c:79`
   传的是 `(bm, 刚打出那张, 右边紧邻格, 3, 1)`；而 `TriggeredSwarm` 的 `OnTrigger(0x26c)` 又被
   `RuleCore.TrySwarmMerge` 对应成**留下那张**（`FireTriggerAlways(ctx, host, …)`）。
   ⇒ **照抄原版那条守卫会把「留下那张」误挡**。

---

## 三、🔴 本笔与简报的**唯一**差别（请调度台裁）

简报要我加的是「**`triggers:*` 且非 `SelfOnly` ⇒ 一律不许 `ReferenceEquals`**」。
§二 第 3 条把那个前提**推翻了**：原版这一族**本来就分两半**，`ferocity`/`duty`/`synapse`
那三个词**恰恰不自排除**。⇒ 本笔落成**「默认自排除 + 一张例外表」**：

```
SelfExcludesOnKeywordTrigger(kind):
    kind 不是 triggers:*            → false（本守卫只管这一族）
    kw ∈ {ferocity, duty, synapse, swarm} → false（原版不自排除）
    其余                            → true （自排除）
```

**为什么不能用 blanket 版**（三条，都是实据）：
- `Bjorn the Fell-Handed`（卡池真卡，kw `Ferocity`）卡面 =
  `Ferocity: Deal 2-3 damage to all enemies` + `When a friendly unit uses Ferocity, it stays in play`
  —— **「it stays in play」指的是用 Ferocity 的那个单位**，blanket 版会让它**永远留不下来**；
- `Zoanthrope`（kw `Synapse`，`When a friendly unit triggers Synapse, deal 1-3 damage to a random enemy`）同；
- 反过来，**今天卡池里能走到 `sacrifice` 这条链的一张都没有**
  （`cards_engine.json` 1126 张整条记录扫 `sacrific` 只命中卡名 `TAU65 Valued Sacrifice`）
  ⇒ blanket 版**只会错、不会对**。

**今天的行为变化面 = 0 张**（`duty`/`mob` 在排除表里，但没有卡同时带那个词 + 那个监听器；
`ferocity`/`synapse`/`swarm` 在例外表里 ⇒ 一格没动）。⇒ 这是一笔**潜伏修复**（与 `E7` 对
`sacrifice` 的判断一致），真正落地的判别力在**自检夹具**上（§四）。

⚠️ 若调度台仍要 blanket 版：把 `KeywordTriggerIncludesSelf` 清空即可（**一行**），
但那样同时会改错上面两张真卡 —— 本笔按「一切照原版」先落了例外表。

---

## 四、判别式（**交给断言写手**；`RuleEngineTest.cs` 不在本笔白名单）

> 现成夹具：`RuleEngineTest.cs:17230` 的 `TestKeywordTriggeredEvents`（**本笔改动不会弄红它** ——
> 它那条端到端里 `mobber` / `watcher` 是**两张不同的卡**，且手牌那一跳 `listenerUnit = null`）。
> 下面四条**挂在同一个测试里**或另开一节都行。

**① 正例（新守卫要挡的那一半）**
· 夹具：手工卡 A 带 `KeywordTable.Sacrifice`（`Implemented` ✓ `HasTriggerMoment` ✓，
  见 `CardDef.cs:3171` / `RuleCore.cs:5492`）+ `WhenTriggers` 上一条
  `WhenEvent{ Kind = WhenEventKind.Triggers("sacrifice"), OwnerIs = -1, SelfOnly = false }`（正文取可观测的）；
  卡 B 带同一条监听器但**不带** `sacrifice`。
· 断言：A 献祭（`FireTriggerAlways` 那一跳）⇒ **B 响一次**（正文结算 / `EvtKind.Trigger` 出现）。
  🧨 把广播那一跳关掉 ⇒ 本条红。

**② 反例 = 新守卫本身**
· 断言：**A 自己那条监听器一次都不响**（数 `RuleCore.BroadcastWhen` 写的那行
  `—— 事件「triggers:sacrifice」触发：…` 日志为 0）。
  🧨 删掉 `Matches` 里 `if (!ev.SelfOnly && SelfExcludesOnKeywordTrigger(ev.Kind))` 那一段
  ⇒ **②红、①仍绿**（正例与反例结构上不同源）。

**③ 反向沙包（灭自证，**必须同时钉**）**
· 夹具：卡 C 带 `Ferocity` 词 + 一条**非 `SelfOnly`** 的 `triggers:ferocity` 监听器
  （形状 = 真卡 `Bjorn the Fell-Handed`）。
· 断言：C 触发 `ferocity` ⇒ **C 自己那条监听器要响**（结算 1 次）。
  🧨 把 `"ferocity"` 从 `KeywordTriggerIncludesSelf` 里删掉（= 退回 blanket 版）
  ⇒ **③红、②仍绿**。⇒ ②③ **不可能被同一个实现同时满足**。

**④ `SelfOnly` 那一支不许误伤（专门钉 `!ev.SelfOnly` 这个前缀）**
· ⚠️ 本笔实测：**光靠 `RuleEngineTest.cs:17261-17265` 那条钉不住它** ——
  那条用的是 `synapse`，而 `synapse` **在例外表里** ⇒ 守卫本来就不适用，删掉 `!ev.SelfOnly` 它**照样绿**。
· 夹具：卡 D 带 `sacrifice` + 一条 `SelfOnly = true` 的 `triggers:sacrifice` 监听器。
· 断言：D 自己献祭 ⇒ **D 自己那条要响**。
  🧨 删掉 `!ev.SelfOnly &&` ⇒ **④红、②仍绿**。

**⑤（顺手钉，非必需）两个判空**：`listenerUnit != null && subject != null`
· 删掉它们 ⇒ `FireCostWhen`（`CardDef.cs:2231`，`EffectResolver.cs:5792` 调）那条路
  `listenerUnit = subject = null` ⇒ `ReferenceEquals(null, null) = true` ⇒ **手里那族的降费监听器被静默挡掉**。
  今天唯一那条真卡（`When you trigger Ferocity, … costs 1 less`）恰好在例外表里躲过去了；
  **新加一个不在表里的词就会中招** —— 所以这两个判空是**load-bearing**，不是装饰。

---

## 五、没查清 / 没做的 + 顺手发现

1. 🔴 **`A1399` 账上那句「原版这一族自身不一致」—— 对了一半、错了一半**（请调度台落到正本）：
   · **错**：`ReactToUnitUsedSacrifice` 的棋盘跳**有**自排除（`E7` 已订正，本笔复算同）；
   · **对**：这一族**确实不一致**，只是分法不同 ——
     **`TriggeredFerocity/Duty/Synapse` 那一支真的没有自排除**（§二·3）。
   ⇒ `E7` 报告 §2.2 第 4 条那句「原版这一族**统一**自排除」**不成立**，本笔把它按实际情况落进了代码注释。
2. ⚠️ **没查清**：`TriggeredCodex = 650` 的接收方函数叫 `TriggerOtherCardCodex`（有守卫），
   而 `TriggeredSwarm = 620` 的接收方 `TriggeredSwarm` 没守卫 —— **原版这两支为什么这么分**，
   **没查清**（只查了「有没有」，没查「为什么」）。本笔按「有/没有」落，没有去猜设计意图。
3. ⚠️ **顺手发现（新账，归 `RuleCore.cs`，不在本笔白名单）**：
   `RuleCore.TrySwarmMerge` 的 `FireTriggerAlways(ctx, host, …)` 把**留下那张**当 `subject`，
   而原版 `BroadcastUnitSwarm` 的 `param_2` 是**被并掉那张**（§二·4 的实据）⇒
   `When a friendly unit triggers Swarm, gain +1 [Ranged]` 的「it」**指错了那张牌**。
   今天能观测到的只有 `Termagant Brood` / `Tyranid Prime` 两张的监听器。**建议单开一笔**
   （要动 `RuleCore.cs` + 可能牵动 `RuleEngineTest` 里 swarm 那几条）。
4. ⚠️ **`A1461`（`TAU20 Sniper Drone` 的 markerlight 事件时机）—— 本笔【没做】，判据不齐**：
   `grep markerlight Core/WhenEvent.cs` = **0**（与账上一致）；但这一条**不止解析层**：
   · 事件层要新增一个 `Kind` + 一条 `enabled markerlight` 短语（本文件）；
   · **广播点不在本文件** —— markerlight 的授予在 `EffectResolver.ApplyOneGain` 那一侧
     （`RuleCore.cs` / `EffectResolver.cs`，**两处都有人在改**）；
   · 另一层（目标段把从句吞掉）在 `EffectText.cs`。
   ⇒ **只加解析半层 = 注册一条永远不响的监听器**（本工程红线的静默失效）⇒ 本笔**不碰**，
   留给拿到 `EffectText.cs` + 广播点的那一笔一起做。
5. ✅ **顺手发现（好消息）**：`CardDef.cs:2639` 那段 `Sacrifice` 的 doc 早就写着
   「对场上所有卡 + 两手牌（**除被献祭那张自己**）各发一次 `OnTrigger(0xe1 / OtherCardSacrifice = 225)`」
   —— 与 §二·1 的机器码结论**独立吻合**（两处判据同向）。

---

## 六、验证（本笔做了哪些、没做哪些）

· ✅ **做了**：`git diff --numstat`（`WhenEvent.cs` = **92 / 0**，**只此一个文件**）；
  行尾 `b.count(b'\r\n')` = **0** / `b.count(b'\n')` = 1448（改前 1356）⇒ 纯 LF、没翻；
  `{`/`}` · `(`/`)` · `[`/`]` 三个计数差**全为 0**（粗结构检查）；
  机器码逐条复核（8 个广播函数，见 §二表）；卡池实扫两遍（监听器短语 12 处 / 能收下的 10 处）。
· ⛔ **没做**（按简报）：**没跑 Unity**、**没跑 `typecheck.sh`**（编译由主对话统一跑）、
  **没动 git 的写操作**、**没改任何正本**、**没碰白名单外的任何文件**、**没碰 `d:/2`**。
· ⚠️ **一次只改一个变量**：两处改动（守卫 + 例外表）是**同一件事的两半**，
  例外表清空 ⇒ 退回简报要的 blanket 版；守卫整段删掉 ⇒ 逐字节退回改前。
  两处都不动别的判据（`SelfOnly` / `ActorSelf` / `Criteria` / `Target*` 一格没碰）。
