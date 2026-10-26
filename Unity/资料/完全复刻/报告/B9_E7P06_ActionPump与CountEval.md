# B9 · P-06 执行报告 —— `ActionPump.cs` + `CountEval.cs`（执行代理 `E7P06`）

> 会话：2026-10-26（第八十四会话 · 批次 9）· 简报 = `判据/B9_剩余C_派活包.md` §2「P-06」
> 独占文件：`Unity/MyGame/Assets/RuleEngine/Core/ActionPump.cs` · `Unity/MyGame/Assets/RuleEngine/Core/CountEval.cs`
> 账补丁：`账/_parts/B9_E7P06.tsv`（6 行，11 列，LF，无 BOM；`check_patch_conflicts.py` = 无跨补丁重复键）

## 〇 摘要（逐条对照派活单）

| # | 条目 | 结论 | 读数 |
|---|---|---|---|
| ① | `Courtesy`/`Relentless` 两族（原版四个方法） | **映射 + Add/Resolve 两段入口已落地**；剩两处**白名单外**的缺口（`KeywardTable` 常量/前缀、`EffectResolver` 的 `chosenAbility 370/400` 分派） | 池内 0/1128 可达 ⇒ **指纹逐字节不变**（隔离实测 = 基线 `af8e8178…`, rc=0） |
| ② | `MeetsCriteria` ④`maxTargets` 截断 / ⑤`playerTurn` 出声 | **④⑤已做**（截断含「加权档先选后加」）；③ `RandomChance(8)` 按简报**等 P-04**；⑥ 卡级 `conditionsFilter` **仍缺**（要四处一起动，全在白名单外） | 离屏探针 **33 条全绿**；截断/`cond`/`playerTurn` 四类读数见 §四 |
| ③ | 669 `BroadcastWhileInPlay` 接线 | **已打开**（`ActionPump.cs:143`，1 行换 1 行、零行漂移） | **指纹 `af8e8178…` → `16db33fa…`**（rc=1）· ①-a **0 张**变、①-b **2 张**（`BL18` `netFull` / `UM18` `raw 46→50`、`net 32→36`）⇒ 逐条解释见 §三 |
| ④ | `selectFromChooseResolution` | 按简报**没做**；🔴 **但「判据读不出」那句是错的，已就地订正**（三个消费点，语义可读到「候选 = `manager.cardChosenInSelection`」） | 见 §五；池内唯一一行 = `ASH82`，而它同时卡在 `ta=50`（手牌族）⇒ **先做手牌族再回来** |

**L0 类型检查**：`TMPDIR=/tmp/wf_E7P06 bash 工具/typecheck.sh` ⇒ **运行时 0 错误 / 编辑器 0 错误**（并发期间首次跑出过 17 个 `BattleDriver.cs` 的错 —— 别人正在写，与本次改动无关，重跑即 0/0）。

---

## 一 `Courtesy(43)` / `Relentless(230)`（本批最值钱的一条）

### 1·1 判据（第一权威 `d:/2/tools/decomp_full/`，四个方法体逐个现读）

- **`BattleManager__AddTriggerCourtesy.c:20-79`** 与 **`…AddTriggerRelentless.c:20-79`** —— **逐字节同形**，只差 actionType（`0x41 = 65 triggerCourtesy` / `0x3e = 62 triggerRelentless`）。体内三句：
  1. `BattleAction___ctor(action, <type>, 0, param_3, param_5, 0)`（`:27`）⇒ `casterCard(+0xD8) = actingCard` · `blockPlayerActions(+0xF4) = param_5` · `checkIfCasterInPlay(+0xD0) = 0`；
  2. `local_1f8[0] = param_2`（`:28`）⇒ `targetCard(+0x10) = targetCard`；**`+0x08`（`actingCard` 槽）不写**；
  3. `AddAutoActionToQueue(manager, action, param_4, 0)`（`:77`）⇒ **纯入队、不判任何前置**、不读卡状态；道 = 调用方给的 `priority`。
- **`BattleManager__ResolveTriggerCourtesy.c:28-264`** 与 `…Relentless.c` —— 实测 `diff` **只剩**函数名 / 一个 `LAB_` 标签 / trigger id（`0x2b = 43` vs `0xe6 = 230`）：
  1. `targetCard(+0x10) != null` 且 `CardScript.IsInPlayOrDying()` **为假** ⇒ `FinishResolvingAction(<type>)` 后**直接返回**；
  2. `raw = targetCard.rawCardData(+0x28)`；`raw != null` ⇒ `RawCardScript.OnTrigger(<43/230>, manager, thisCard = targetCard, actingCard = casterCard(+0xD8), targetCard = null)`（`:217`）；
  3. `FinishResolvingAction(<type>)`；⚠️ `targetCard == null` / `raw == null` ⇒ 原版 **NRE**（`FUN_1803f47a0` 不返回）。
- **唯一调用点** = `AbilityLogic__PlayAbility.c:2006`（`chosenAbility == 0x172 = 370` ⇒ Relentless）与 `:2102`（`0x190 = 400` ⇒ Courtesy）：都对**目标列表逐个**入队一条，实参 `(manager, targetCard, actingCard = abilityData.thisCard(+0x10), priority = ability.priority(+0x44), blockPlayerActions = 1)`。

### 1·2 落地（两段映射；⛔ 别在别处再写一份）

| 原版 | 我们 |
|---|---|
| `AddTrigger*`（纯入队） | `EnqueueAbility(..., actor: target, requireActorInPlayOrDying: true)`，`abilityContext` 留未知 ⇒ 入队那一刻不判 context |
| `ResolveTrigger*` 的出队闸 `CardScript.IsInPlayOrDying` | 泵点 `PumpActionQueues` 的 `RequireActorInPlayOrDying → IsOnBoardOrDying`（**判据只此一份**，本件不另写） |
| `OnTrigger(43/230, thisCard=target, actingCard=casterCard, targetCard=null)` | 队列体 `FireTriggerAt(target, keyword, …, chosen: null, subject: actingCard)`（两槽模型 → 本文件类尾 §四） |
| 关键词 → `AbilityTrigger` 名 | `ExtraTriggerName`：`courtesy` → `Courtesy`(43) · `relentless` → `Relentless`(230) |

改动（`ActionPump.cs`，**全部追加在类尾 / 类尾前的原地改写，零行漂移**）：

- `:1894-1911` `ExtraTriggerName` 加两行映射（+10 行，在**所有账落点之后**）；
- `:1913-2010` 新增类尾 **§九**（判据全文 + 两段映射 + 残余 + 池内可达性）；
- `:2013` 新增 `public static bool EnqueueGrantedTrigger(BattleContext, string keyword, UnitState target, UnitState actingCard, ActionPriority lane, string label = null)`；
- `:1817-1826` / `:1831-1835` 把 §六 里 189/605 与 194/610 那两段「整条没有」的旧状态**就地改写**成「已落地 + 残余」（**行数 6→6 / 5→5，零漂移**）。

### 1·3 残余（两处，都在别的写手的文件里）

1. **数据侧「认得出」**：`Core/CardDef.cs` 的 `KeywordTable` 没有 `Courtesy`/`Relentless` 常量、`Prefixes` 没有这两个前缀 ⇒ 卡面/卡数据真写这两个词会被 `Normalize` **静默丢弃**（同 `summonrift` / `requiem` 那两格的形状）—— 落点 = **P-07**。
   ⇒ 所以本件的映射**先用字符串字面量** `"courtesy"` / `"relentless"`，注释写明「等 `CardDef` 加常量后改成引用，⛔ 别两处各留一份字面量」。
2. **调用点**：`chosenAbility 370 / 400` 的 op 分派在 `EffectResolver`（**P-02**）。

### 1·4 池内可达性 + 零可达验证

`cards_truth.json` 1,351 张逐张扫：`chosenAbility ∈ {370, 375, 390, 400}` **各 0**（`chosenAbility` 全池只有 55 个不同值）；`cards_engine.json` v32 的 1,128 张：`keywords` 含 `courtesy` / `relentless` **各 0**（唯一 `relentless` 命中是 `SAU36` 的**天赋名** `Talent: Relentless March`）。

**隔离实测**（本批其余改动全开、只把 §三 那一行 `Auras.RefreshInPlayNumericAndKeywords` 临时注释掉）：
`play check` ⇒ **指纹 = `af8e817839fa1dd199d33320f6bf1b2e` = 基线，rc=0**，①-a 1,128 张逐字节一致、①-b 586 张逐字节一致。

---

## 二 `MeetsCriteria` ④⑤⑥（账 676）

### 2·1 ④ `maxTargets` 截断（已做）

**判据**：
- **为什么要截**：`BoardAnalysis__MeetsCriteria.c:101` 给 `GetTargets` 的第 6 实参传 **`1`**（= `checkMaxTargets`；ILSpy 签名 `GetTargets(BattleManager, TargetCriteria, AbilityData, bool checkMaxTargets = true)`）⇒ 数的是**截断之后**的列表（`:103 iVar6 = list._size`）。
- **截断本体** = `AbilityLogic__GetTargets.c:1164-1365`：`if (!checkMaxTargets || maxTargets < 1) skip`（`:1164`）· `if (pool.Count <= maxTargets) skip`（`:1169`）· `switch (targetSelectMethod)`（`:1171`）：
  `1 highestAttack` = `OrderByDescending(GetHighestAttackValue)` ＋次键 `CurrentIntrinsicHealth` · `2/3 valueInPlay` = `OrderByDescending/OrderBy(GetValueInPlay())` · `5 lastAdded` = 从表尾取 N · `6 lowestHealth` = `OrderBy(CurrentIntrinsicHealth)` · `7 firstAdded` = 从表头取 N · `default`（= `0 random` / `4 cardTarget`）= 逐个 `GetRandomInt(0, pool.Count)` 抽并删；非 default 的六档统一 `Enumerable.Take(pool, maxTargets)`（`:1365`）。
  顺序键语义 = ILSpy 闭包体 `_003CGetTargets_003Eb__26_1..4` + `CardScript__GetHighestAttackValue.c:5-13`（= `max(CurrentMeleeAttack, CurrentRangeAttack)`）+ `EntityScript__get_CurrentIntrinsicHealth.c`（血量**减去** intrinsic 修饰）。

**🔑 关键结论（决定了实现能有多简）**：纯计数档（`countOptions == 0`）**每个分支都恰好剩 `maxTargets` 个**（排序+Take / 头尾取 N / 随机抽 N 都是 N）⇒ 个数 = `min(count, maxTargets)`，**不需要知道顺序**；只有 `countOptions == 10`（同方 −1 / 异方 +1）**必须先选出是哪几个**再累加。

**实装**（`CountEval.cs`）：
- `:924 ApplyMaxTargets` —— 上面那条结论 + 四档顺序（`7 firstAdded` · `5 lastAdded` · `1 highestAttack`（稳定排序、平手按池序）· 其余 `null` + 出声）；
- `:267 MeetsBoardCriteria` 数完就调它（`countOptions == 10` 时按需进「收集档」）；`:158 CountTargets` 也调（原版 `AbilityLogic__PlayAbility.c:185/:262` 的 `GetTargets(…, 1, 0)` 同一个 `checkMaxTargets`）；
- `CountSet` 加两个**可选**收集形参：收集档只记「命中个数 + 候选」、**不在里面加权**（加权必须在截断之后）。
- ⚠️ **`Unknown(-1)` 按「不限」处理**（与本文件其它布尔格 `Unknown ⇒ 不筛` 同一条约定；原版 `maxTargets` 是 int、`<= 0` 即不限，两者同向）。数据侧现读全部落确定 `0` ⇒ 生产路径走不到。
- ⛔ **`selectMethod ∈ {random(0), cardTarget(4), valueInPlay(2,3), lowestHealth(6)}` 一律 `null` + 出声**：`random` 要 P-04 的随机源（⛔ 不拿 `ctx.Rng.Next` 顶）；`cardTarget` 的真实口径未取到；`GetValueInPlay()` / `CurrentIntrinsicHealth` 本版**没有等价字段**（`Health` / `MaxHealth` 都不是 `CurrentIntrinsicHealth`，⛔ 不冒充）。

### 2·2 ⑤ `playerTurn` 缺 `ctx`/`owner`（已做，出声放行）

原版 `BoardAnalysis__MeetsCriteria.c:40-41` 在 `manager == null` 或 `thisCard == null` 时 **goto NRE 桩**（崩溃）。我们**不复制崩溃语义** ⇒ `null` + `gap`（调用点据此出声 + 放行）。
🔴 **改前是一个静默错**：`CountEval.Meets` 里 `isMyTurn = ctx.Active == owner` 对 `owner < 0` 恒假 ⇒ `pt == 10` 时**静默返回 false**（= 把「不知道」当成「不是自己回合」）。顺手把 `ctx == null` 与「没有 countSet」**分成两句 gap**（改前共用一句「没有 countSet」，指向错的东西）。

### 2·3 ⑥ 卡级 `conditionsFilter`（**仍缺**，四处交接）

`normalAbilityBoardCriteria.countCriteria.conditionsFilter` 要**四处一起动**，**全在本件白名单外**：

| # | 文件:行 | 要做什么 |
|---|---|---|
| ① | `Core/CardDef.cs:267`（`BoardGateSpec`） | 加一格 `ConditionsFilter`（默认 `Unknown`，与其它格同契约） |
| ② | `Core/CardDef.cs:300`（`UsesAlternativeAbility`） | 调 `MeetsBoardCriteria` 时传 `cond: NormalAbilityBoardCriteria.ConditionsFilter` |
| ③ | `Data/CardDatabase.cs:2303`（`MapBoardGate`）＋ `:604`（`AbilityGateDto`） | 映射 `normalAbilityBoardCriteria.countCriteria.conditionsFilter` |
| ④ | `工具/gen_cards_engine.py:2467`（`board_gate_of`） | 从 `countCriteria.conditionsFilter` 取值并 emit |

求值侧**已经就绪**（`cond` 形参在批次 6 `G17` 就有了，本批只是把交接写全在 `MeetsBoardCriteria` 的 `<param name="cond">` 上）。
⚠️ **现读池内 2 张卡（`SW9` / `SW21`）与池外 1 张（`Fenrisian Wolfpack`）该值全是 `0`**（`cards_raw.json` 的 `full_raw` 逐张读）⇒ 落上也零行为变化 —— **但⛔ 不能因此不做**（铁律 11）。

### 2·4 四条「灭自证」读数（一次性离屏探针 `_tmp_view/E7P06/probe`，**33 条全绿**）

| 读数 | 结果 |
|---|---|
| 截断：3 个敌方目标 + `maxTargets=1` | `≥2` 由 **真 → 假**；`EqualTo 3` 由真→假、`EqualTo 1` 由假→真 |
| 截断**在加权之前** | 2 个敌方部队：不截断净和 `+2`（`op==2` 真）；`firstAdded`+`max=1` 净和 `+1`（`op==1` 真、`op==2` 假）⇒ 若把截断放到加权之后，两条会一起翻过来 |
| `cond=10`（canAttack） | 1 活力/1 疲劳 ⇒ 数出 **1**；把疲劳那个恢复 ⇒ 数出 **2**（`cond=0` 恒 2）⇒ **cond 真的进了被数集合** |
| board 条件 false / true / 去掉 count 行 true | `≥5`⇒假 · `≥1`⇒真 · `gate==null`⇒真 · `countCriteria==null`⇒真 |
| `playerTurn` 缺 `owner` | `null` + gap（改前 **false**）· `ctx==null` 也 `null` + gap · 正常路径 `owner=0/Active=0`⇒真、`owner=1`⇒假 |
| `lowestHealth(6)` 加权档 | `null` + gap（`CurrentIntrinsicHealth` 无等价字段） |

⚠️ **夹具坑（写给下一个写手）**：督军就在 **`Board[4]`**（`BoardSpec.WarlordSlot`）—— `Place(ctx, p, 4, …)` 会**静默把督军换掉**；数「敌方部队有几个」时要么避开 4 号格、要么 `includeHero = 0`（我第一次就栽在「1 个」数成「2 个」上，两条断言假红）。

---

## 三 669 `BroadcastWhileInPlay` 接线（指纹变化逐条解释）

### 3·1 改了哪一行

`ActionPump.cs:143`（`ActionScope.Dispose` 里、`EmitWhileInPlayPass`(`:157`) **之前**）：

```
- // Auras.RefreshInPlayNumericAndKeywords(_ctx);   // ⏸ …（批次 6 `G14` ①）**仍未打开**：…
+ Auras.RefreshInPlayNumericAndKeywords(_ctx);      // ✅ …本行【已打开】：…
```

**1 行换 1 行**（零行漂移）。判据 = `FinishResolvingAction` 尾部 `BroadcastWhileInPlay(this,false,null)` 连发 `WhileInPlay(20)` 与 **`WhileInPlayVariable(240)`** 两档（`RawCardScript.cs:1239-1253`），240 = 「先摘再重加」的快照重折叠。
⛔ 仍**不要**换成 `Auras.Recompose`（会重放夹血 / 重授誓约 ⇒ `BL18`/`UM18` 的 `net` 各 +4 是另一回事）。

### 3·2 指纹变化（**重钉基线只能由调度台做，本代理没有动 `PlayProbe.cs` / 基线文件**）

| 口径 | 改前（= 钉住的基线） | 改后 |
|---|---|---|
| `PinnedPlayMd5` 实测 | `af8e817839fa1dd199d33320f6bf1b2e` | **`16db33fa634ab9d958b968688def69ff`** |
| `rc` | 0 | **1** |
| ①-a 主档（1,128 张，打出去那一拍） | — | **0 张变**（逐字节一致） |
| ①-b 事件电池（586 张） | — | **2 张变** |

### 3·3 逐条解释（哪条改动导致、为什么）

| 卡 | 变了的列 | 改前 → 改后 | 为什么 |
|---|---|---|---|
| `UM18` | `raw` / `net` / `netFull` | `raw 46 → 50` · `net 32 → 36` | 光环卡：一条顶层动作结束 ⇒ 现在多跑一次 **240 档重挂** ⇒ `WhileInPlayVariable` 那族的外在效果被**重新加回一遍**（每次 +4 个单位），电池夹具里有 4 个事件 ⇒ +4。**不是**新的正文结算（`①-a` 0 张变可证） |
| `BL18` | `netFull` | 事件流长度变了（尾部多一段同样的重挂序列） | 同上（`BL18` 也是光环/电池卡；它 `net` 数值没变，变的是 `netFull` 那条事件流文本） |

⇒ **归因是唯一且闭合的**：把这一行临时注释掉、其余改动全开 ⇒ 指纹**逐字节回基线 `af8e8178…`、rc=0**（`_tmp_view/E7P06/play_check_isolated_AB.txt`）。
⇒ 结论：**本批 ①②④ 的改动零行为变化；指纹的全部差异来自 669 这一行。**

⚠️ **仍然成立的残余**：① **手牌那半没扫**（原版 `BroadcastWhileInPlay` 也扫双方手牌；池内 0 张手牌卡带 `WhileInPlay` 行 ⇒ 今天不可达）② **基线待调度台重钉**（上表那一对指纹）。

### 3·4 「危害面」全量核对：**`手牌总价` 现在一张都不变**（回应调度台口径纠正 ②）

调度台要求确认「副作用只落在『手牌总价』这一个量上」，并给「开了这一行 ⇒ 影响哪 9 张卡」的清单。**实测结论 = 那个 9 张清单是【修前】的历史，现读为空**：

- **全量逐卡逐列对拍**：`bash 工具/ruleprobe.sh play all dump`（1,128 张 × 23 列，含 **`handCostDelta` = 💰 手牌总价**）在「这一行 ON / OFF」两种状态下各 dump 一份 ⇒ **变化卡数 = 0，23 列一列都没差**（`_tmp_view/E7P06/dump_lineON.tsv` vs `dump_lineOFF.tsv`）。
- **电池那一档**（`play check` ①-b，`play all dump` 结构上看不见）⇒ 只有 **2 张**的 `raw` / `net` / `netFull` 变，**没有任何一张的 `handCostDelta` 变**（隔离日志里 `手牌总价` 这个串出现 **0 次**）。
- **那份「9 张」的来源**（`ActionPump.cs:131-133` 记的批次 4 `F1` 隔离读数，**修前**）：`①-a` 主档 2 张（`BL27` `Your Stratagems cost 1 less` / `SW18` `Friendly Beasts cost 1 less`）＋ `①-b` 电池 7 张（`AM15` / `GSC30` / `BL27` / `BL18` …，注释只列到 4 张），**全部坐在 `💰 手牌总价` 一个量上**（`7 → 0` 变 `7 → 1`）。根因 = `RecomposeCore` ①「整份摘掉」**无条件**清 `ctx.CostMods`，而重加那一半被 `numericOnly` 跳过 ⇒ **清了不重加**。
- **该根因已在批次 6（`G14`）修掉**（`Aura.cs` 那两行加 `!numericOnly` ⇒ 重算路径整层不碰费用档）⇒ 本批的隔离实测就是它的**回归证据**：`BL27` / `SW18` / `AM15` / `GSC30` **一张都没变**。

⇒ 所以现在挂在 `ActionPump.cs:143` 的副作用**只剩**「240 档重挂把 `WhileInPlayVariable` 那族快照重折叠一次」，它落在**事件电池的计数/事件流**上（`BL18` / `UM18`），**不落在任何一处费用/数值上**。

---

## 三·五 调度台口径纠正 ①：`PlayIconAnim` / `ResolveAbilityIconAnim` 零调用点 —— **本件无动作（已核）**

调度台要求「在 `ActionPump` 侧只留结构分派口 + 一条断言」。**核对结果：`ActionPump` 里结构上就没有 kind/actionType 这一维**，所以没有「分派口」可留、也**不发明**：

- `grep -n "PlayIconAnim|AbilityIconAnim|IconAnim|kind 21" RuleEngine/Core/ActionPump.cs` ⇒ **0 命中**；
- 根因是设计：我们的队列条目是 **`Core/ActionQueue.cs:57 QueuedAction`** —— 字段只有 `Priority` / `Seq` / `Label` / `Payload` / `Actor` / `RequireActorInPlay*` / `AbilityContext` / `ExtraTrigger`，**没有 `BattleActionType` / kind 这一格**；出队是执行闭包（`QueuedAction.Execute`），不是按类型分派表。⇒ **原版那套「21 号动作入队 → 分派到 `ResolveAbilityIconAnim`」在我们的形状里根本没有对应物**。
- 表现层那个口已经存在且**当前 0 引用**（= 与「原版零调用点」同形）：`CardPresentation/Battle/AbilityIconAnim.cs`（`E5` 的文件）的 `Enqueue(...)`；全仓 `AbilityIconAnim` 的引用只有它自己文件内部（另一处 `CardPresentation/Core/EventTiming.cs:954` 只是**注释**里提了一句）。
- ⛔ 按调度台指示：**不发明触发源**、**不挂 `EvtKind.Trigger`**（那条链已有 `PlayTraitPulse` = `ActivateTriggerTraitAnim`）。
- ⚠️ 另：本件**没有**断言宿主（`Editor/RuleEngineTest*.cs` 与 `工具/ruleprobe/**` 都在别人的白名单里）⇒ 那条「证明分派存在、且没有触发源」的断言应当落在**拥有 `AbilityIconAnim.cs` 的写手**那批（或调度台在同步点加）。

---

## 四 ④ `selectFromChooseResolution`（按简报没做**实现**，但「判据读不出」这句是错的 → 已订正）

**现状**：`SetGap` 里继续挡着（非 0 ⇒ `null` + 出声），**本批没有实现语义**。

🔴 **就地订正（铁律 5）**：账 676 原来那句「`dump.cs` 只有字段声明、**全量反编译里没有任何消费点**」**不成立**。现读：

| 证据 | 位置 |
|---|---|
| 字段声明 | `TargetCriteria.cs:82`（ILSpy）· `il2cpp_out/dump.cs` 的 `TargetCriteria` `+0x80` |
| 消费点 ① | `cpp2il/out_cpp2il_dev_ilspy/AbilityLogic.cs:1075`（`GetTargets` 里、**maxTargets 截断之后**那一段） |
| 消费点 ② | `AbilityLogic.cs:1414`（`GetCardToSummon`：`if (!criteria.selectFromChooseResolution) { …正常牌池扫描… }`） |
| 消费点 ③ | `AbilityLogic.cs:1628`（`GetCardScriptToSummon`：同形，为真 ⇒ **跳过** `GetTargets`) |

**语义（现读能确定的）**：该位为真 ⇒ **候选池不是「按 criteria 从棋盘/牌库筛出来的」**，而是**本次「选牌结算」里选中的那一张** —— `AbilityLogic.cs:1077` 拿 `manager + 744`（= **`BattleManager.cardChosenInSelection`**，`dump.cs:30844` `// 0x2E8`）去**淘汰 `uniqueId` 不等于它的候选**。

**还差什么（所以本版仍判不了，⛔ 不猜）**：
1. 我们**没有** `cardChosenInSelection` 的等价物（`BattleContext` 上没有「本次选牌结算选中了哪张」的槽）⇒ 要先查它的**写点**（谁在选牌结算里写它）才能建模；
2. ILSpy 那三处落在**同一个巨函数**（`GetTargets` 1,427 行反编译）里，`else` 两支被 `NoteDecompilerIssue` 糊掉了 ⇒ 要按 `.c` 逐句核；
3. **池内唯一一行**用到它 = `ASH82`（`Farseer Skyrunner`，`role="count"`），而那一行 **`targetsAffected = 50`（手牌族）** ⇒ 本文件 `CountSet` ③ 本来就判不了（`ta ∈ {50,51,52,65}` 一律 `null` + 出声）⇒ **就算把这一格做掉，那张卡仍然判不了**。
   ⇒ **顺序**：先做手牌族（`ta=50`），再回来看 `selectFromChooseResolution`。

（订正已同时写进 `CountEval.cs` 的 `SetGap` doc 与账 676 的「差异」列。）

---

## 五 验证汇总 / 改动清单 / 顺手发现

### 5·1 验证

| 项 | 命令 | 结果 |
|---|---|---|
| 类型检查 | `TMPDIR=/tmp/wf_E7P06 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 解析层对拍 | `bash 工具/ruleprobe.sh check` | **rc=0** · 全池 1,121 张 · 解析差异 0 行 · 哨兵失败 0 |
| 结算层对拍 | `bash 工具/ruleprobe.sh play check` | **rc=1** · 指纹 `16db33fa…` ≠ 基线 `af8e8178…` · ①-a 0 张 · ①-b 2 张（= §三，唯一来源） |
| 隔离（只差 669 那一行） | 同上（临时注释该行） | **rc=0 · 指纹 = 基线逐字节** · ①②④ 零行为变化 |
| 离屏探针（④⑤ + Courtesy 入口 + 映射） | `dotnet …/e7p06probe.dll` | **✓33 · ✗0**（读数见 §二·4 / §一） |
| 账补丁冲突 | `python 工具/check_patch_conflicts.py` | ✅ 无跨补丁重复键 |

### 5·2 改动清单（行号 = 2026-10-26 现读）

`RuleEngine/Core/ActionPump.cs`（1,900 → 2,048 行；**首 1,893 行的行号全部不变**）
- `:143` 669 接线（**1↔1**）
- `:1817-1826` · `:1831-1835` §六 的 189/605 · 194/610 状态**就地改写**（**6↔6 · 5↔5**）
- `:1894-1911` `ExtraTriggerName` 加 `courtesy`/`relentless` 两行映射
- `:1913-2010` 新增类尾 **§九**（判据全文 / 两段映射 / 残余 / 可达性）
- `:2013-2046` 新增 `EnqueueGrantedTrigger(...)`

`RuleEngine/Core/CountEval.cs`（794 → 1,006 行）
- `:63-105` `Meets`：⑤ `playerTurn` 缺 ctx/owner ⇒ `null` + gap；`ctx==null` 与「没有 countSet」两句 gap 拆开
- `:158-197` `CountTargets`：接 ④ 截断
- `:252-266` `MeetsBoardCriteria` 的 `<param name="cond">`：⑥ 的**四处交接**写全
- `:267-…` `MeetsBoardCriteria`：④ 截断（`countOptions==10` 时进收集档）
- `:423-443` `SetGap` 前的 doc：`selectFromChooseResolution` 的**订正 +「还差什么」**
- `:480` `CountSet`：加两个**可选**收集形参 + 三个分支的收集档
- `:924-997` 新增 `ApplyMaxTargets`；`:999` 新增 `HighestAttackKey`

### 5·3 顺手发现（按铁律 13·4⑧报上来，调度台分流）

1. 🔴 **账里的行号已经大面积漂移**：`判据/B9_剩余C_施工单.tsv` 写的「账行号 189/194/605/610」现在分别落在 `AddTriggerTeleport` / `AlreadyPlayedCardThisTurn` / `SortByCostAndName` / `StopBattleAfterBattleEnd` 上（简报说「别信行号」是对的）。我按**原版类 + 方法名**定位到真实行：**178 / 183 / 552 / 557 / 669 / 676**。
2. 🔴 **`check_ledger_points.py` 现状**：`战斗逻辑驱动.tsv` A=664（漂移 192 / 失败 3）、`战斗界面_HUD.tsv` 漂移 117 —— 是**并发写手**造成的大面积漂移；`--fix` 属于调度台的同步点动作，本代理**没有跑**。
3. 🟡 **`CountSetSpec.Unknown = -1` 与「合法的负阈值」撞车**：`referenceValue = -1` 会被当成「没落」⇒ 若将来出现 `countOptions == 10`（净差可为负）且阈值恰为 `-1` 的卡，会被判成「未知」。现读池内 `count` 99 行 `refValue` 无一为负、`opt` **全是 0** ⇒ 今天不可达，但这个哨兵值设计有隐患（记在这里）。
4. 🟡 **`maxTargets` 那一族里 4 档判不了**（random / cardTarget / valueInPlay / lowestHealth）：`random` 等 P-04；另三档缺 `GetValueInPlay()` / `CurrentIntrinsicHealth` 的等价字段 —— 要不要为它们建模，属于「字段层」的活（`UnitState` / 卡表），不在本件。
5. 🟡 **夹具坑**：督军在 `Board[4]`；`Place(..., 4, …)` 会静默换掉督军（§二·4 末尾那段）。
6. 🟡 669 接线后 `play check` **必红**直到调度台重钉基线（`af8e8178…` → `16db33fa…`）；本代理**没有**碰 `PlayProbe.cs` / `PinnedPlayMd5` / 基线文件。
