# B141_W128_TriggerKey — `KeywordFromTriggerName`（trigger 名 → 引擎关键词 的反查）

> 批次 **B141** · 执行写手 `B141-W86` · 第一百二十八会话 · 2026-11-04
> 任务 = `B141-HAND-TRIGGER-KEY`（输入 = `报告/B141_W128_CaRowDispatch.md` §六·❓2）
> 一句话：**给「正向映射」（`Keyword → TriggerName`）补上了它的逆**，反查表**由正向函数在运行时推导**（⛔ 不是第二份手抄表）；改后 `typecheck 0/0` · `ruleprobe check rc=0` · 指纹不变。

---

## 一、结论

| 项 | 值 | 证据 |
|---|---|---|
| 反查函数 | `RuleCore.KeywordFromTriggerName(string triggerName)` | `RuleEngine/Core/ActionPump.cs:2695` |
| 覆盖对数 | **31**（= 正向表全部非 null 输出：主 switch **22** + `ExtraTriggerName` **9**） | `ActionPump.cs:1099-1121` / `:1894-1912` |
| 双向往返 | **31/31** 通过 `TriggerNameOfKeyword(KeywordFromTriggerName(x)) == x` | 本报告 §二 表 + 断言 B3/B6 |
| 重名歧义 | **0**（没有任何两个关键词落到同一个 trigger 名） | 断言 B4 |
| 「名字 ≠ 关键词」的条目 | **4**（`UseSpiritStone`→`spiritstone` · `ThisCardPlayed`→`rally` · `ThisUnitDeath`→`backlash` · `Rage`→`penitence`） | 断言 A2/C1–C4 |
| 端到端接线 | ⛔ **本笔未做**（`EffectResolver.cs` 被 `W77` 占用）→ 补丁见 §五 | —— |

⚠️ **这不是「手牌触发路已修好」**：本笔只补了映射层。**接线那一句没落**，所以手牌路的 `kw` 兜底**今天仍然是 `triggerName`**、`TriggerOps` 仍然恒空。要它生效 = 把 §五 那个补丁派给 `EffectResolver.cs` 的写手。

---

## 二、映射形状（31 对，现读自源码，2026-11-04）

| # | trigger 名 | trigger 数 | 引擎关键词 | 正向表位置 |
|---|---|---|---|---|
| 1 | `ThisCardPlayed` | 0 | `rally` | 主 switch |
| 2 | `OtherCardPlayed` | 10 | `othercardplayed` | `ExtraTriggerName` |
| 3 | `Courtesy` | 43 | `courtesy` | `ExtraTriggerName` |
| 4 | `ThisUnitDeath` | 80 | `backlash` | `ExtraTriggerName` |
| 5 | `Slay` | 120 | `slay` | 主 switch |
| 6 | `Rage` | 130 | `penitence` | `ExtraTriggerName` |
| 7 | `Codex` | 145 | `codex` | 主 switch |
| 8 | `TriggerOnceIfBoardCriteria` | 200 | `triggeronceifboardcriteria` | `ExtraTriggerName` |
| 9 | `Sacrifice` | 220 | `sacrifice` | 主 switch |
| 10 | `Relentless` | 230 | `relentless` | `ExtraTriggerName` |
| 11 | `AboutToAttack` | 280 | `abouttoattack` | 主 switch |
| 12 | `Mob` | 300 | `mob` | 主 switch |
| 13 | `Regiment` | 301 | `regiment` | 主 switch |
| 14 | `Survivor` | 420 | `survivor` | 主 switch |
| 15 | `Requiem` | 430 | `requiem` | `ExtraTriggerName` |
| 16 | `Landing` | 440 | `landing` | 主 switch |
| 17 | `OtherCardSpiritStone` | 455 | `othercardspiritstone` | `ExtraTriggerName` |
| 18 | `Artifice` | 560 | `artifice` | 主 switch |
| 19 | `Strike` | 580 | `strike` | 主 switch |
| 20 | `UseSpiritStone` | 600 | `spiritstone` | 主 switch |
| 21 | `SpentAllMana` | 641 | `spentallmana` | `ExtraTriggerName` |
| 22 | `Pray` | 655 | `pray` | 主 switch |
| 23 | `Duty` | 662 | `duty` | 主 switch |
| 24 | `Ambush` | 670 | `ambush` | 主 switch |
| 25 | `Uprising` | 680 | `uprising` | 主 switch |
| 26 | `Teleport` | 690 | `teleport` | 主 switch |
| 27 | `Agenda` | 705 | `agenda` | 主 switch |
| 28 | `Stimulation` | 710 | `stimulation` | 主 switch |
| 29 | `Ecstasy` | 715 | `ecstasy` | 主 switch |
| 30 | `Cruelty` | 720 | `cruelty` | 主 switch |
| 31 | `Ferocity` | 725 | `ferocity` | 主 switch |

trigger 数 = `CardDef.AbilityTriggerNumberOfName`（`CardDef.cs:1914-1949`）现读，逐个对上。

---

## 三、实现：怎么做到「一份映射」而不是两份

**反查表是运行时推导出来的，源码里没有任何一行写着「`Mob` → `mob`」这种对照。**

```
候选关键词 = KeywordTable.Prefixes 的 target 列（119 行 → 106 个去重候选；= Normalize 的唯一注册表）
           + KeywordCandidatesOutsidePrefixes（2 个不在 Prefixes 里的合成词：othercardplayed / othercardspiritstone）
            ↓ 逐个喂正向函数
       TriggerNameOfKeyword(候选词) → 名字 or null
            ↓ 只登记名字非 null 的
       Dictionary<名字, 关键词>（31 条，OrdinalIgnoreCase 键）
```

- **正向表增删一行 ⇒ 反查自动跟着变**，不存在「两份表打架」。
- `KeywordCandidatesOutsidePrefixes`（`ActionPump.cs:2687`）里**只有候选词、没有任何名字** —— 这是我唯一额外加的东西，它回答不了「`Mob` 叫什么」，只能回答「有哪些词可能是关键词」。
- 重名（两个关键词 → 同一个名字）= 正向表本身有歧义 ⇒ **先到先得 + `Debug.LogWarning` 出声**（⛔ 不静默挑一个）。现读 **0 条**。

### 改动的文件

| 文件 | 改了什么 | 行数变化 |
|---|---|---|
| `RuleEngine/Core/ActionPump.cs` | 类尾追加 **§十二**：`KeywordFromTriggerName` / `KeywordByTriggerNameTable` / `KeywordTriggerPairNonIdentityCount` / `KeywordByTriggerName()` / `RegisterKeywordTriggerPair()` / `KeywordCandidatesOutsidePrefixes` | **+107 行**（2657 → 2764），**全部落在文件末尾**（该文件现读最晚的账落点 = `:2135`，本段从 `:2656` 起 ⇒ 零漂移） |
| `RuleEngine/Core/CardDef.cs` | `static readonly string[][] Prefixes` → **`public`**（只加一个修饰词，同一物理行） | **0 行**（9750 → 9750） |
| `RuleEngine/Editor/B141_W86_Assert.cs` | 新建（独立类，类名 == 文件名） | 新增 263 行 |

为什么动 `CardDef.cs`：反查要枚举「引擎有哪些关键词」，而那份注册表是 `KeywordTable.Prefixes`，原来是 `private`。**加 `public` 是零行数变化、零语义变化**的最小改法；⛔ 没有把 `KeywordFromTriggerName` 写进 `KeywordTable`（那样必然插行 ⇒ 该文件 70+ 处账落点全体漂移）。

---

## 四、断言（`RuleEngine/Editor/B141_W86_Assert.cs`）

| 组 | 钉什么 |
|---|---|
| **A** | 表建得起来 · 覆盖数 **31** · 「名字≠关键词」**4** 条 |
| **B** | 候选词逐条双向往返（31/31）· **无重名歧义** · 表内对数 = 正向对数 · 表内每对都经得起正向复核 |
| **C** | 4 条硬映射（`UseSpiritStone`/`ThisCardPlayed`/`ThisUnitDeath`/`Rage`）+ 键匹配忽略大小写 |
| **D** | ★ **真实消费口**：池内找一张 `TriggerOps("mob")` 非空的卡 ⇒ `TriggerOps(KeywordFromTriggerName("Mob"))` 非空，而 `TriggerOps("Mob")` = **null** |
| **E** | 负例：`null`/`""`/`TurnStart`/`UnitPoisoned`/`CardCreatedInHand`/未知名 ⇒ 全 `null` |
| **F** | 手牌广播那 9 个名字的现状对照（`Cruelty`/`Relentless` **有**关键词；其余 7 个没有） |

**🧨 灭自证**：
- 把实现换成 `triggerName.ToLowerInvariant()` ⇒ **A2 变 0**、**C1–C4 全红**、**E3–E5 从 null 变非 null**；
- 删正向表任一行 ⇒ **A1 / B2 的对数掉下来** + 对应 C 条红；
- **D4 是最强的一条**：`TriggerOps("Mob")` 必须为 null —— 证明 D3 的「能取到正文」不是「反正都能取到」。
- ⚠️ **断言抓不住的**：如果有人把反查改成「手抄一份反向表」，本文件仍然全绿。那一半只能靠**结构**判（§三），别指望断言。

**接线（调度台做）**：`RuleEngine/Editor/RuleEngineTest.cs` 聚合块语句**同一物理行**（落在该行第一个 `//` 之前，⛔ 别新增行）追加：

```
Guard("B141W86_Assert", B141_W86_Assert.Run); _pass += B141_W86_Assert.Pass; _fail += B141_W86_Assert.Fail;
```

---

## 五、🔴 接线补丁（交给 `EffectResolver.cs` 的写手 —— 本轮被 `W77` 占用，本笔⛔未改）

**落点**：`RuleEngine/Core/EffectResolver.cs:19056`（`FireHandAbilityTrigger` 的 ③ 正文段）

**现读**：

```csharp
var carrier = TriggerCarrier.From(inst, owner); string kw = !string.IsNullOrEmpty(keyword) ? keyword : triggerName;
```

### 变体 A（**推荐**，一行、零行数变化）

```csharp
var carrier = TriggerCarrier.From(inst, owner); string kw = !string.IsNullOrEmpty(keyword) ? keyword : (HandAbilityTriggerHasRows(card, triggerName) ? triggerName : (RuleCore.KeywordFromTriggerName(triggerName) ?? triggerName));
```

### 变体 B（更干净，但 **+1 行** ⇒ 该文件下游账落点会漂，需要同批 `check_ledger_points --fix`）

```csharp
var carrier = TriggerCarrier.From(inst, owner);
bool hasRows = HandAbilityTriggerHasRows(card, triggerName);
string kw = !string.IsNullOrEmpty(keyword) ? keyword : (hasRows ? triggerName : (RuleCore.KeywordFromTriggerName(triggerName) ?? triggerName));
if (ops == null) ops = carrier.FxOps(kw);
if (spec == null) spec = carrier.Effect(kw);
bool rowLane = (ops == null || ops.Count == 0) && spec == null;
if (rowLane && !hasRows) return false;
```

### 🔴 为什么要有 `HandAbilityTriggerHasRows` 那道守卫（⛔ 别照字面写「`?? KeywordFromTriggerName(triggerName)`」）

现在那三句是：

```csharp
if (ops == null) ops = carrier.FxOps(kw);
if (spec == null) spec = carrier.Effect(kw);
bool rowLane = (ops == null || ops.Count == 0) && spec == null;
```

⇒ **`ops` 非空就把 `rowLane` 关掉**。如果一个 trigger 名能反查出关键词、而那关键词又有正文，**文本路会抢在行级 `ca` 派发前面** —— 而行级派发（`DispatchHandAbilityRows`，`B141_W67` 建的）才是**照原版 `PlayAbility` 跑 `abilityLogic` 行**的那条路，文本 op 是我们自己的近似。**原版没有「正文通道」** ⇒ 有行时**行必须赢**。
（另外 `HandAbilityTriggerHasRows` 有「出声一次」的副作用，所以变体 A 会多调一次 —— 它内部是去重播报，实际只印一次。）

### 预期影响（如实记，⛔ 别当成「已修好」）

- **今天零可达**：W67 §六·❓4 现读 —— `abilityContext == Hand` 的 13 张卡，trigger 全在 `WhileInPlayVariable 240 / OtherUnitDeath 90 / TurnEnd 40 / ThisCardPlayed 0 / OtherCardPlayed 10 / OtherUnitSummoned 190 / TriggeredSynapse 630 / TriggeredCodex 650`，**一个都不在**手牌广播实际发出的那 9 个名字里（`TurnStart` / `TurnStartPlayer` / `Relentless` / `Talent` / `UnitDamaged` / `UnitDamagedByAbility` / `UnitDamagedByAttack` / `Cruelty` / `CardCreatedInHand`，另有 `UnitPoisoned`）。
- 但**广播名里 `Cruelty` / `Relentless` 是有关键词的** ⇒ 一旦出现「Hand 上下文 + `cruelty`/`relentless` 正文」的卡，这条路**会开始结算正文**。那是**修好**，不是回归；但接线批次必须知道「指纹有可能动」。
- `ruleprobe` 的 `play` 口径**不经过** `EffectResolver` 的手牌路 ⇒ 接线前后 `ruleprobe check` 预期**都不动**（本笔实测见 §六）。

---

## 六、验证记录（本笔跑过的）

| 项 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_b141_w86 bash 工具/typecheck.sh` | **运行时 0 错 · 编辑器 0 错** |
| 解析闸 | `bash 工具/ruleprobe.sh check` | **rc=0** · 输出指纹 `77a3c2bc…`（**未变**）· 解析差异 0 行 · 哨兵失败 0 条 |
| 映射覆盖仿真 | 按源码正则复算「候选词 → 正向名 → 反查」 | **31 对 · 0 重名 · 0 条落空 · 4 条非小写同名**（与断言 A1/A2/B2-B4 的期望值逐个吻合） |
| 行尾 | 二进制计数 | `ActionPump.cs` 2764 行纯 LF / 0 裸 CR；`CardDef.cs` 9750 行**未变**；新文件 263 行纯 LF |

⚠️ **断言本身没跑过**（没接进 `RuleEngineTest`，且本批 ⛔ 不跑 Unity）—— 上面那张「映射覆盖仿真」是同一批判据的**离线等价物**，但它**不等于**断言跑绿。接线后第一次跑 `RuleEngineTest.Run` 才算。

---

## 七、❓ / 顺手发现

1. ❓ **反查表把 `ThisCardPlayed`(0) 映到 `rally`** —— 语义上是对的（trigger 0 = 打出时触发 rally，判据 = 正向表那条注释里 105 张卡的 `abilityTriggers` 全部有 0）。但 `ThisCardPlayed` 在 `TriggerSkipsAbilityContext` 的 **skip 表**里，⚠️ 接线后如果有人从 skip 那条路走到这里，拿到的是 `rally` 的关键词正文 —— **今天没有这种调用点**，留一句在这里备查。
2. ❓ **7 个手牌广播名永远拿不到正文**（`TurnStart` / `TurnStartPlayer` / `Talent` / `UnitDamaged` / `UnitDamagedByAbility` / `UnitDamagedByAttack` / `CardCreatedInHand`，另 `UnitPoisoned`）：它们在原版里**没有「关键词正文」通道**（原版只有 `PlayAbility` 跑行）。⇒ 接线**不会**把它们修好，也别为了让它们「看起来能跑」去凭空造 `talent` / `turnstart` 这类关键词（铁律 18·0：抄原版，⛔ 不自我设计）。它们的正确修法是**行级 `ca` 派发**（`DispatchHandAbilityRows`）。
3. 🟠 **`Prefixes` 被提成 `public`**：如果调度台不喜欢扩大公开面，等价替代 = 在 `KeywordTable` 里加一个 `internal static IReadOnlyList<string> AllKeywords`，但**必然插行**（该文件 70+ 处账落点）⇒ 我选了零行数的那个。
4. ⚪ **并发观察**：本笔操作期间 `RuleEngine/Editor/` 里同时有别的写手的 `B141_W36_Assert.cs` / `B141_W39_Assert.cs`（W36 的 C6/C7 是「`othercardplayed` 必须为 null」的旧钉子，W39 已经补上映射 ⇒ **C6/C7 按设计与事实相反**，属 W39 的账，不是我这一笔）。我**没有**碰那两个文件。

---

## 八、另一笔的边界（⛔ 别混淆）

| 是什么 | 谁做的 | 状态 |
|---|---|---|
| 正向 `Keyword → TriggerName`（含 `othercardplayed` / `othercardspiritstone` 两条） | `B141_W39` | ✅ 已在（`ActionPump.cs:1912`） |
| **反向 `TriggerName → Keyword`** | **本笔 `B141-W86`** | ✅ 已在（`ActionPump.cs:2695`） |
| 把反向接进 `FireHandAbilityTrigger` | —— | ⛔ **未做**（§五 补丁） |
