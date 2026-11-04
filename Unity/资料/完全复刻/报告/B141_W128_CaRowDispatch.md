# B141_W67 · 手牌上下文（`abilityContext = 10 Hand`）的行级 `ca` 派发

- 批次 / 会话：`B141` / 第一百二十八会话（W128）· 任务行 `B141-CA-ROW-DISPATCH`（`B-211` 一族）
- 写手：执行代理 `B141-W67` · 日期：2026-11-04
- 白名单实改：`RuleEngine/Core/EffectResolver.cs`（**零行数变化**）·
  `RuleEngine/Core/CardAbilityDispatch_B116.cs`（**只在类尾追加**）· `RuleEngine/Editor/B141_W67_Assert.cs`（新建）
- ⛔ 未跑 Unity / 未动 git / 未动账主表 / 未改 `d:/2` / 未改 `活状态表`与所有权表

---

## 〇、一句话结论

**手牌触发路（`EffectResolver.FireHandAbilityTrigger`）过去只走「文本 op」一条取法、不按 `chosenAbility` 数字分派
⇒ `ca 160 (discardFromHand)` 这样的行到不了执行器。本笔补上「**文本为空 ⇒ 走行级 `ca` 派发**」这一支，
判据、分类口、双发闸全部复用主路既有的那一份（`TriggerRowKindOf` / `CardAbilityRowCoveredByTextLayer` /
`DiscardHandCardCore`），⛔ 没有第二个 `chosenAbility` 分类表。**

现池**零可达**（`cards_engine.json` 1,142 张里 `ca == 160` = **0 行**）⇒ `ruleprobe check` / `play check`
**逐字节不变**；真行为用**离线真跑**（同一份 `Core/` 源码编成控制台程序）验过：**32 条夹具断言全绿**，
两次变异实验（拆接线 / 拆 `ta` 闸）分别红 **6 条** / **3 条**。

---

## 一、原版派发点（判据，全部现读 `d:/2/tools/decomp_full/`）

| 判据 | 内容 |
|---|---|
| `AbilityLogic__PlayAbility.c:286` | `iVar8 = *(int *)(param_1 + 0x10)` = `AbilityLogic.chosenAbility` —— 之后整个巨型 switch 由它决定「这一行执行什么」 |
| `AbilityLogic__PlayAbility.c:588-603` | `else if (iVar8 == 0xa0)`（= 160）：`while` 遍历 `possibleTargets(param_1 + 0x78)` ⇒ 逐个 `BattleManager__AddDiscardFromHand(bm, 目标, ability+0x44, abilityData+0x48, 0)` |
| `AbilityLogic__GetTargets.c:184-192` | `ta = 0x14 (20 self)` ⇒ 清空池子、把 `param_3`（`thisCard`）放进去 ⇒ **目标 = 能力所在那张卡自己** |
| `RawCardScript__TriggerAbilities.c` | 选「哪条 ability」只看 `ability.trigger`（`CanTriggerAbility.c:71`），**不看 `chosenAbility`** ⇒ 手牌上下文（`IsRightContext` 只放 `10 Hand`）与 ca 派发是**两层**，都要走 |
| 数据实证 | 唯一持有 `160` 的 SO = 教程起手占位卡 `T1 BlackLegion-Abaddons Chosen_startinghand`（`uniqueId f3df0cfc…`，`trigger 30` / `ctx 10` / `ta 20 self`），判据与反证 → `报告/B141_W128_BL37Discard.md` §一 |

⇒ 原版形状 = 「**手牌里这张卡自己那条 ability 被 trigger 命中 → 按 `chosenAbility` 执行那一行**」。
我们的缺口只在**第二层**（ca 派发），第一层（trigger + context 两闸）改前就有。

---

## 二、改法（逐处）

### 2.1 `EffectResolver.cs`：`FireHandAbilityTrigger` 补「文本空 ⇒ 行级派发」这一支（**零行数变化**）

4 处**同一物理行**改写（改前改后 `wc -l` 都是 **22,096** —— 本笔操作期间实测；之后别人又加了 9 行，见 §三·⑤）：

| 现读行 | 改法 | 理由 |
|---|---|---|
| `:19055-19060` | 原 6 行（`// ③ 正文` + 5 句）改成 6 行：`var carrier …; string kw …;`（两句合一行）＋ `bool rowLane = (ops == null \|\| ops.Count == 0) && spec == null;` ＋ `if (rowLane && !HandAbilityTriggerHasRows(card, triggerName)) return false;` | 「文本空 ⇒ 改走行级派发」的**唯一闸**；`⛔ 别把它写成两处判据** |
| `:19074` | 入队 lambda 追加 `, triggerName` | 队列体要知道是哪条 trigger 的行 |
| `:19092` | `HandAbilityDeferred` 形参表追加 `, string triggerName = null` | 默认 `null` ⇒ 既有调用点零影响 |
| `:19101` | 末尾追加 `else if (triggerName != null) DispatchHandAbilityRows(ctx, owner, inst, triggerName, by);` | **泵点**才结算（⛔ 不在广播循环里就地改手牌 —— 那会在 `for (i<Hand.Count)` 里边遍历边删） |

✅ 已复刻标记打在 `:19055` 那条注释行上（铁律 20，零行数变化）。

### 2.2 `CardAbilityDispatch_B116.cs`：类尾追加「五、手牌上下文的行级 `ca` 派发」（334 → 518 行）

| 成员（现读行） | 作用 |
|---|---|
| `:365` `enum HandRowKind` | `Unknown`（`ca < 0` = 改前行为）· `Owned`（本层有执行器）· `Unsupported`（原版会跑、我们没有 ⇒ **出声**） |
| `:381` `HandAbilityRowKindOf` | **唯一分类口**；🔴 在**共用的** `TriggerRowKindOf(ca)` 之上再判一格（⛔ 不另开一张 `ca` 表）：`ta = 20 self` ∧ `ca 160` ⇒ Owned，其余 ⇒ Unsupported |
| `:406` `HandAbilityTriggerHasRows` | 决定「要不要入队」；与派发口用**同一个**分类口 + **同一条** B7 判据；对 Unsupported 行**顺手出声一次**（否则「返回 false ⇒ 调用方静默返回」= 静默失败） |
| `:429` `DispatchHandAbilityRows` | 遍历 `card.TriggerAbilityRowsOf(trigger 数值)`：`Unknown` 跳过 / B7 命中跳过 / `Unsupported` 出声 / `ca 160` 走执行体 |
| `:459` `RunDiscardFromHandRowOfHandCard` | 执行体：`ta = 20` ⇒ `DiscardHandCardCore(ctx, owner, inst, …)`（= `DoDiscardFromHand` 的结算核心，含 `RemoveCostEffects` / `AddToCemetery`）；`ta ≠ 20` ⇒ **出声、不猜** |
| `:490` `NoteHandAbilityRowUnsupported` | `Debug.LogWarning`（**不进 `ctx.Log`** —— 那会改 `ctx.Events.Count` ⇒ 动联机指纹），同 `(卡, trigger, ab, lg, ca)` 只喊一次 |
| 类尾两个 `…ForTest` | 自检门（不改行为，只让 Editor 程序集调得到） |

### 2.3 `Editor/B141_W67_Assert.cs`（新建，346 行）

见 §四。

---

## 三、验证读数

| 闸 | 改前 | 改后 |
|---|---|---|
| `TMPDIR=/tmp/wf_b141_w67 bash 工具/typecheck.sh` | 运行时 **0** / 编辑器 **0** | 运行时 **0** / 编辑器 **0**（最终态再跑一次仍 0/0） |
| `bash 工具/ruleprobe.sh check` | `rc=0` · 解析差异 **0** 行 · 哨兵失败 **0** · 输出指纹 **`77a3c2bc…`** | **完全一致**（同指纹 / 同差异 0 / `rc=0`） |
| `bash 工具/ruleprobe.sh play check` | `rc=0` · 结算指纹 **`a0e9c63d…`** · 主档/电池逐卡全等 | **完全一致**（同指纹；最终态又跑一次仍 `rc=0`、同指纹） |
| 账闸 `python 工具/check_ledger_points.py`（⛔ 未 `--fix`） | ——（未在改前跑：并发写手多，读数会被别人搅动） | `A 5966 · 通过 5258（告警 960）· 漂移 633 · 失败 77`；**本笔两处改动结构性零影响**：`EffectResolver.cs` 4 处全为同一物理行改写（`wc -l` 前后都是 22,096），`CardAbilityDispatch_B116.cs` 只在类尾追加（账里**没有任何** A 行指向该文件） |
| 离线真跑（自建探针，见 §四·三） | —— | **32 条全绿**；变异 ①（拆掉 `rowLane` 接线）⇒ **红 6 条**；变异 ②（`ta` 一律当 self）⇒ **红 3 条** |

**指纹为什么不变（如实标）**：现池 `cards_engine.json`（1,142 张）**`ca == 160` = 0 行**、
`abilityContext == 10 Hand` 的 13 张卡的 trigger（`0 / 10 / 40 / 90 / 190 / 240 / 630 / 650`）
**一个都不在**生产调用点传给 `FireHandAbilityTrigger` 的那 12 个 trigger 名里
（`TurnStart 30` · `TurnStartPlayer 31` · `Relentless 230` · `Talent 46` · `TurnStarted 160` ·
`UnitPoisoned 290` · `OtherCardRegiment 646` · `UnitDamaged 60` / `185` / `187` / `Cruelty 720` · `CardCreatedInHand 210`）
⇒ 那 13 张在闸①（`AbilityTriggerPresence`）就被拒，**走不到本笔的新代码**。
⇒ **`play check` 绿 ⛔ 不能当「新路径被执行过」** —— 真行为读数在 §四·三。

### 三·⑤ 并发写（如实记）

本笔操作期间 `EffectResolver.cs` **被别人同时改过**（Edit 工具报过一次「文件已在磁盘上被修改」）：
我四处改完时 `wc -l = 22,096`，最终态 `22,105`（+9 行，来自别人）。
账闸里 `EffectResolver.cs` 的 A 行显示 **+10 行整体偏移**（`8816→8826` · `10093→10103` · `13481→13491`，
全部在**我的段落之上**）⇒ 那是并发的另一半，**不是本笔**（本笔零行数）。
最终态我又跑了一遍 `typecheck`（0/0）与 `play check`（`rc=0`、指纹 `a0e9c63d…`）确认没被撞坏。

---

## 四、断言：`Editor/B141_W67_Assert.cs`

### 四·一 六组（每条都带「🧨 怎么才能红」）

| 组 | 盯什么 |
|---|---|
| **A**（4 条） | 纯函数 `HandAbilityRowKindOf`：`160 + ta=20` ⇒ Owned · `160 + ta=30` ⇒ Unsupported · `ca=-1` ⇒ Unknown · `ca=100` ⇒ Unsupported |
| **B**（9 条） | **端到端**：占位卡形状（`trigger 30` / `ctx 10 Hand` / `ta 20 self`）放进手牌（另放两张填充牌）⇒ 触发口**入队** ⇒ 泵队后**手牌少的就是它自己**、两张填充牌没动、它进**弃牌堆** |
| **C**（4 条） | 负例 `ta=30`：没有可派的行 ⇒ 触发口回 `false`（⛔ 不静默入队一条空转能力）⇒ 手牌/弃牌堆都不动 |
| **D**（7 条） | `TriggerAbilityRowsOf` 三态（列未知 `null` / 明确没有 `[]` / trigger 对不上）＋ trigger 名 → 数值那一跳（`TurnStart=30` / `TurnStarted=160` / `UnitPoisoned=290` / 认不出 ⇒ `-1`） |
| **E**（5 条） | **B7 双发闸**：正文 op 已覆盖 `ability#0` ⇒ 本层让开（派发口回 0、手牌不动）；**摘掉正文桶 ⇒ 又变 true 且跑 1 行**（同一夹具两个方向，⛔ 不是恒 `false`） |
| **F**（3 条） | 现池可达性：`ca == 160` = **0 行** ⇒ 本笔零可达；池里确有 `ctx == 10 Hand` 的卡（现读 13 张） |

### 四·二 接线（调度台统一做，本写手白名单不含宿主）

`Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（落在该行第一个 `//` **之前**）：

```csharp
Guard("B141W67_Assert", B141_W67_Assert.Run); _pass += B141_W67_Assert.Pass; _fail += B141_W67_Assert.Fail;
```

### 四·三 本笔**真跑过**（离线，不占 Unity）

`工具/ruleprobe` 那套壳能把整个 `Core/` 编成控制台程序（`Stub.cs` 桩）。本笔在 `_tmp_view/` 里复制了一份探针工程
（**⛔ 未改仓库里的 `工具/ruleprobe/`**），加了一个 `w67` 子命令，把 §四·一的 A/B/C/D/E 五组夹具**原样**跑了一遍：

```
=== 全绿 ===   （32 条 OK / 0 条 XX）
  OK  A1 ta=20 self ⇒ Owned ／ OK  A2 ta=30 ⇒ Unsupported ／ OK  A3 ca=-1 ⇒ Unknown ／ OK  A4 ca=100 ⇒ Unsupported
  OK  B1 触发口入队 ／ OK  B2 入队计数 1 ／ OK  B3 入队那一刻还没弃
  OK  B4 泵队后手牌 -1 ／ OK  B5 少的就是它自己 ／ OK  B6 两张填充牌没动
  OK  B7 弃牌堆 +1 ／ OK  B8 进弃牌堆的就是它
  OK  C0..C3 ／ OK  D1..D7 ／ OK  E1..E5
```

**灭自证（两次变异，都在 `Core/` 的**副本**里做，⛔ 没碰仓库源码）**：

| 变异 | 结果 |
|---|---|
| 把 `:19060` 换成 `if (rowLane) return false;`（= 拆掉行级派发接线） | **红 6 条**：B1 · B2 · B4 · B5 · B7 · B8（E 组仍绿 —— 它直调 `DispatchHandAbilityRowsForTest`，绕开接线，符合预期） |
| 把 `HandAbilityRowKindOf` 的 `if (ta == 20)` 换成 `if (true)`（= 忽略 `ta`） | **红 3 条**：A2 · C0 · C1（C2/C3 仍绿 —— 执行体 `RunDiscardFromHandRowOfHandCard` 里还有第二道 `ta` 校验，属**有意**的双保险） |

> ⚠️ 探针是**自建、一次性**的，跑完已删除（`_tmp_view/w67probe/` 整目录）。要复现：
> 复制 `工具/ruleprobe/{*.cs,ruleprobe.csproj}` 到空目录 → csproj 里 `../../MyGame/` 改成 `../../Unity/MyGame/` →
> 加一个 `W67Probe.cs`（夹具同 `B141_W67_Assert.cs` 的 A–E 组）＋ `<Compile Include="W67Probe.cs" />` →
> `Program.cs` 里加 `if (args[0] == "w67") { Environment.Exit(W67Probe.Run(PoolList)); }` → `dotnet build && dotnet run -- w67`。

---

## 五、可达性与边界（如实）

1. **今天零可达**：池内 `ca == 160` = 0 行；唯一持有者是教程起手占位卡（不在 `cards_engine`）。
2. **只实现 `ta = 20 self`**：`ca 160` 其余档要在**手牌里**按 criteria 筛（`GetTargets`），
   我们这层手牌不是 `UnitState`、没有实例级 criteria 收口口 ⇒ **出声 + 不结算**（⛔ 不猜 = 不弃错牌）。
3. **其它 `ca` 档在手牌上下文没有执行器**：主路的执行器（`70 summonUnit` / `100 createCardInHand` /
   `30 buffOthers` …）**全部挂在 `UnitState` 上**，手牌没有那个对象 ⇒ `Unsupported` + 出声。
   ⚠️ 这包括占位卡的**第二个 logic 行**（`ca 100 createCardInHand`，造真 `BL37`）—— 见 §六·1。
4. **不双发**：只在「文本 op 为空 **且** 没有 `EffectSpec`」时进本层；进了之后再过**同一条** B7
   （`TriggerOpsByNumber(trigger)` 已覆盖该 `ab` ⇒ 让开）。⛔ 没有第二份「谁覆盖谁」的判定。
5. **结算时机 = 泵点**（与文本 op 同一个队列体）：⛔ 不在广播循环里就地改手牌
   （`BroadcastTurnStartHandHops` 那种 `for (i < ps.Hand.Count)` 里删牌会跳过下一张）。

---

## 六、❓ / 顺手发现（请调度台分流，⛔ 本笔未改）

1. 🔴 **占位卡「整张卡」仍然只完成一半**：它的 `abilityLogic` 有两行 —— `ca 160`（弃自己）**和**
   `ca 100 createCardInHand`（造真 `BL37`）。本笔只让第一行到得了执行器；第二行到手牌上下文仍是
   `Unsupported`（出声）。⇒ **数据真进池那天**，这张卡会「自己没了、真卡没造出来」。
   要补的是 `ca 100` 的**手牌执行器**（判据 `AbilityLogic__PlayAbility.c:986-1037` + 五支取牌
   `GetCardsToSummon`，现成实现在 `EnchantmentExecutor_B115.cs:1550`，但它**要求非空 `UnitState carrier`**）
   ⇒ 需要一次「没有载体单位时怎么办」的裁断，⛔ 不是本笔能顺手做的。
2. 🔴 **手牌触发路的「文本取法」在**所有**生产调用点上都恒空**（本笔现读，属**新发现**）：
   `FireHandAbilityTrigger` 的兜底是 `kw = triggerName` → `TriggerCarrier.FxOps(kw)` →
   `CardDef.TriggerOps(keyword)` / `CardDef.Effect(keyword)`，而这两张表**键是**引擎关键词（小写：
   `mob` / `strike` / `rally` / `getsmarkerlight` …，实测 `ruleprobe triggerops` 打印 `GOF12` 的 `TriggerOps("mob")`
   正常、`AbilityTriggerNumber("Mob") = 300`），**不是** `AbilityTrigger` 枚举名（`TurnStart` / `UnitPoisoned` …）。
   而 12 个生产调用点**全部**传 `keyword: null`（`HandHops` / `BroadcastTurnStartHandHops` /
   `BroadcastUnitDamagedHandHops` / `EffectResolverPartial_B97W2.cs:129` 现读）⇒ `ops` 恒 `null`。
   ⇒ 现状：**手牌能力从来跑不了正文**（只有 B94_Z2 的自检传了 `KeywordTable.Strike` 才跑得起来）。
   补法（**另一条账**）：在 `FireHandAbilityTrigger` 里把 trigger 名换成规范关键词
   —— 但全仓**只有** `TriggerNameOfKeyword`（关键词 → trigger 名，`ActionPump.cs:1081`），**没有反向表**；
   要么新建一张反查（数据正本在生成器 `ABILITY_TRIGGER_KEYWORD`），要么让调用点显式传关键词。
   ⚠️ 今天做它是**零可达**（13 张 Hand 卡的 trigger 都不在那 12 个名字里），⛔ 别当成紧急缺陷。
3. 🟠 **`ca 165 discardWholeHand` 在手牌上下文同样没有执行器**：它要 `playersAffected`
   （`RunDiscardWholeHandRow` 经 `PlayersAffectedOf` 读 `ctx.ActingUnit` / `ctx.PlayingCard`），
   手牌上下文这两个都是 `null` ⇒ 主表新增的 `TriggerAbilityIndexSpec.Pa`（v 新增格）
   **只有附魔模板第二张表填**，卡表主路 `MapTriggerAbilityIndices` 仍是 `-1` ⇒ 同样判不了。
4. 🟠 **`Cards_engine` 里 `abilityContext == 10 Hand` 的 13 张卡的 trigger** 是
   `WhileInPlayVariable 240`（4 张）· `OtherUnitDeath 90`（2）· `TurnEnd 40`（2）· `ThisCardPlayed 0`（4）·
   `OtherCardPlayed 10` · `OtherUnitSummoned 190` · `TriggeredSynapse 630` · `TriggeredCodex 650`
   —— **一个都不在**手牌广播实际发出的那 12 个 trigger 名里 ⇒ 这 13 张的手牌能力**今天整条链都到不了**
   （闸①就拒）。这解释了「为什么 `play check` 一行都不变」，也说明**手牌广播的覆盖面**本身可能还有缺口
   （另立账；⛔ 本笔不判）。
5. ⚪ 并发：`EffectResolver.cs` 在本笔操作期间被**别人**改过（见 §三·⑤）。本笔段落完整、零行数，未被撞坏。

---

## 七、出处清单（可直接复查）

| 用途 | 路径 |
|---|---|
| 原版 ca 派发 | `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:286` · `:588-603` |
| 原版 `ta=20 self` | `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:184-192` |
| 原版选 ability 不看 ca | `d:/2/tools/decomp_full/CardAbility__CanTriggerAbility.c:71` |
| 改前的手牌触发路 | `RuleEngine/Core/EffectResolver.cs:19055-19105`（`FireHandAbilityTrigger` / `HandAbilityDeferred`） |
| 本笔的分类口 / 派发口 / 执行体 | `RuleEngine/Core/CardAbilityDispatch_B116.cs:365 / 381 / 406 / 429 / 459` |
| 共用判据（⛔ 未改） | 同文件 `:70` `AbilityLayerOwnsRow` · `:100` `AbilityLayerOwns320Row` · `:256` `CardAbilityRowCoveredByTextLayer` · `EffectResolver.cs:21380-21395` `TriggerRowKindOf` |
| 结算核心（⛔ 未改） | `RuleEngine/Core/EffectResolverPartial_B88E2.cs:749` `DiscardHandCardCore` · `:681` `DoDiscardFromHand` |
| 断言 | `RuleEngine/Editor/B141_W67_Assert.cs`（346 行，六组 32 条） |
| 占位卡 / `BL37` 反证 | `资料/完全复刻/报告/B141_W128_BL37Discard.md` |
| `B-211` 施工面 | `资料/完全复刻/判据/B115_S6_四条引擎欠账施工面.md` §二 |
