# E3 · `actingCardCriteria` 数据列 + 最小消费点（执行报告）

> 2026-10-25 · 执行代理 **E3** · 判据来源 = `资料/普查产出_1025/E2_ability_criteria管线.md` §六（**已查清，本报告不重做考据**）
> ＋ 第一权威 `d:/2/tools/decomp_full/` 逐句复核（**§一 更正了 E2 的一处**）。
> 结论一句话：**数据列已落（v27 · 83 行 / 80 张）· 唯一入口三态已建 · 触发前第一道闸已挂（Attack / Die）·
> 断言 4 组已写（本代理**不能跑 Unity**，`RuleEngineTest` 未执行）· `typecheck` 0 错 ·
> `ruleprobe check` **rc=0（重钉后连跑两次）** · `play check` **rc=0** 逐字节零差异。
> 卡池 md5 已按主对话裁定**重钉**为 `5a17f6af49ead1829b067bd9e9082118`（见 §五·b）。
> 追加三件（E3-A/B/C）见 §五·b（重钉）· §六·a（`DA22` 歧义：判据不足 ⇒ 停手）· §七·7（`SporeMine_Explosion` 仍开）。**

---

## 一 · 🔴 判据订正（铁律 5）：`Die` 的 `actingCard` = **凶手**，不是死者

`E2` 报告 §六 写「`Attack` 的 `actingCard` = 攻击者、`Die` 的 = **死者** —— 都在我们的 `subject` / `actor` 上」。
**前半句对，后半句错**（照它接会**筛错人**）。逐句判据：

| 判据 | 原文 | 说明 |
|---|---|---|
| `d:/2/tools/il2cpp_out/dump.cs:23526` | `OnMinionDeath(BattleManager manager, CardScript thisCard, CardScript **actingCard**, CardScript **deadCard**, UnitDeathType deathType, bool skipBacklash)` | **`actingCard` 与 `deadCard` 是两个不同的形参** |
| `CardScript__TriggerOnMinionDeath.c:97-99` | `OnMinionDeath(raw, manager, thisCard, param_1, param_2, param_3, …)` | `thisCard` = 观察者、`actingCard` = 调用方给的来源卡、`deadCard` = 死者 |
| `CardScript__TriggerOnMinionDeath.c`（`OnMinionDeath` 体） | `op_Equality(param_3 /*thisCard*/, param_5 /*deadCard*/)` ⇒ 真走 **80/81 `ThisUnitDeath`**（`actingCard` 传 **thisCard**）；假走 **90 `OtherUnitDeath`**（`actingCard` 传 `param_4`） | ⇒ **只有 80/81 的 acting = 自己（死者）；90 的 acting = 凶手** |
| `CardScript__ResolveDeadCard.c:236` → `BattleManagerSupport__BroadcastDeadUnit.c` | 遍历场上每张牌 `ResolveDeadCard(card)`，`BattleAction.actingCard` / `DamageInfo.actingCard`（`dump.cs:119230`，偏移 `+0x10`）一路带下去 | ⇒ 我们这一路对应 **`CleanupDeaths` 的 `actingCard` 形参**（`DamageInfo.actingCard`） |
| `BattleManager._ResolveAttack_d__438__MoveNext.c:988` | `BroadcastUnitAttacked(manager, attacker, target, …)` ⇒ `ResolveUnitAttacked(param_3=target, actingCard=param_2=attacker, targetCard=param_3)` / 再 `(attacker, attacker, target)` | ⇒ **`Attack` 两跳的 `actingCard` 都是攻击者**（= 我们的 `subject`，`RuleCore.cs:6300`）✅ E2 这一半对 |

**落地**：`Die` 的候选取 `CleanupDeaths(...)` 的 `actingCard` 形参（`RuleCore.cs:9131` 传给 `BroadcastWhen`），
**不是** `subject`。拿不到（毒 / 反噬 / 反击 / 不稳定 / 回合开始清场这些**没有来源**的死）⇒
**判不了 ⇒ 出声并跳过那道闸（放行）**（⛔ 不拿 `subject` 顶）。
另：`FlushDeaths` 出批那一跳原来**没回放** `pd.ActingCard`（它只喂击杀记账）⇒ 现在补上（`RuleCore.cs:9414`）；
行为不变（那次调用显式传 `holdNestedDeaths: true` ⇒ 方法尾段 `&& actingCard == null` 那条 flush 闸被短路）。

---

## 二 · 数据侧（卡表 v26 → **v27**）

### 1 · 列名 / 形状（**结论 + 为什么这么选**）

**列名 = `abilityActingCriteria`**（JSON 键；DTO 同名字段）。理由：

- 与 v26 的 `abilityLevelCriteria` **成套**：前缀 `ability` = 「**ability 级**（`CardAbility` 这个对象）」，
  尾巴点出**被筛的候选**（`Acting` = `actingCard`）—— 两列是**同一层的两道闸**。
- 另一个自然名 `abilityTargetCriteria` **已被 v25 的 logic 级列占用** ⇒ 不能再拿它当「ability 级 + targetCard」。
- 原版字段名 `actingCardCriteria` 逐字写在生成器注释、`CardDatabase.CardDto` 与 `CardDef` 的 doc 里（可追溯）；
  ⛔ 不并进 v26 那一列（**不同候选**），也不并成一个字典（**同 `ab` 键会静默覆盖**）。

形状（与 v26 逐字同形，`JsonUtility` 不支持字典 ⇒ 数组）：

```json
"abilityActingCriteria": [{"ab": 0, "trigger": 50, "criteria": {36 格}}]
```

- `ab` = `cardAbilities` 下标（唯一对齐键）；`trigger` 只作可读性；`lg` **不落**（恒 `-1`）；
- 36 格 = **直接复用 `criteria_set_of()`**（⛔ 不另抄字段表）；`playerTurn` 恒 `-1`（结构上没有）、不进 `set_bad`；
- **每张卡都落**（没有行 = `[]`）——「缺列」与「空数组」分得开；
- 载体 = 生成器一直在用的 `TRUTH_CARDS_SRC`（`资料/普查产出_1018/allcards_1350/cards_raw.json`）的
  `cardAbilities[i].actingCardCriteria`（**不是**另开一份 `cards_truth.json`）。

### 2 · 落行口径与读数（现读 v27 池）

| 项 | 值 | 判据 |
|---|---|---|
| 有 `actingCardCriteria` 字典的 ability 行 | 1270 行（1008 张卡） | 绝大多数是**全 0**（`ta=any` = 零信息） |
| 「36 格非平凡」 | **93 行 / 86 张**（与 E2 §六 对得上） | 任一格 ≠ 0 |
| **实际落行（`ta != 0`）** | **83 行 / 80 张** | `FilterMethods__CheckIfMeetsCriteria.c:56-59`：`ta == 0` ⇒ **直接 `return 1`** ⇒ 零信息（口径 = `criteria_carries_targets`） |
| 被丢掉的那 10 行 | `ta = 0` 但别的格有值（`ASH81` / `AM56`×2 / `ASH78` / `GSC14` / `GSC68` / `GOF82` / `TAU57` / `BL30` / `ASH80`） | 同上（原版不看它们） |
| `ta` 分布（落行的） | `10 allFiltered` **53** · `20 self` **30** | 只有这两档 ⇒ **没有「本版没建池器」的洞**（不像 v26 那 1 行 `ta=55`） |
| 触发分布（落行的） | `50` **14** · `90` **12** · `190` 11 · `10` 7 · `410` 4 · `180` 4 · `695/660/630` 各 3 · 其余 1–2 | 见 §三 覆盖面 |

生成器读数（新加的一行，`gen_cards_engine.py` 的「目标判据④」）：

```
目标判据④    `abilityActingCriteria` 落值 1127/1127 张 · 有行的 80 张 · 83 行
             `targetsAffected == 0 (any)` 不落 1187 行（原版直接 return 1 = 零信息）
             按 targetsAffected（行数）：10:53 · 20:30
```

### 3 · 幂等证据（`--check` + md5 前后）

```
改前 cards_engine.json  md5 = af0303790316bc9cd5a5b3f458697988   3,024,683 B   (v26)
改后 cards_engine.json  md5 = 5a17f6af49ead1829b067bd9e9082118   3,119,176 B   (v27)  +94,493 B
连跑两次 `--write`：md5 逐字节相同  5a17f6af49ead1829b067bd9e9082118  ✅
python -I -X utf8 工具/gen_cards_engine.py --check  → 一致 ✅（rc=0）
```

- 行尾：`cards_engine.json` 本来就没有换行符（CRLF 0 / LF 0），写出来仍是（**CRLF 0 / LF 0**）。
- 生成器自己的「criteria 键集」自检把本列一起算进去：**1977 行 × 37 键全部一致**（1894 + 本笔 83）。
- `+94,493 B` 全部来自这一列（既有列一个字节没动 —— `--check` 除了 version 之外还逐卡比内容，见 `main()` 末段）。

---

## 三 · 消费点（最小版：只接 `Attack` / `Die`）

### 1 · 唯一入口（`CardDef`）

`Core/CardDef.cs`：

- `_abilityActingCriteria`（**另开一个字典**，键 = `ab`，撞键出声）·`AbilityActingCriteriaVersioned` ·
  `AbilityActingCriteriaAt(ab)` · `AbilityActingCriteriaRowCount`（`CardDef.cs:2613-2631`）；
- **唯一入口** `ActingLevelCriteriaForTrigger(triggerName, out why[, out ambiguous])`（`CardDef.cs:2649/2657`）——
  三态与 v26 那道闸逐字同形：有数据 / **明确没有这一格条件**（`why == null`）/ 判不了（`why` 非空；
  其中**定位不唯一**另用 `ambiguous` 标出）。定位**复用** `UniqueAbilityIndexForTrigger`
  （全仓唯一的「trigger 名 → `cardAbilities` 下标」判据；⛔ 没有第二份扫描）。

### 2 · 闸 + 次序（`ActionPump.EnqueueAbility`）

`Core/ActionPump.cs` 新增两个**缺省关闭**的尾参：

```csharp
UnitState abilityActingCard = null,   // 原版 actingCard（🔴 不是 targetCard）
bool      abilityActingGate = false   // 这条入队路径要不要跑第一道闸（= 这一格已查清）
```

判定走 `RuleCore.ActingLevelCriteriaAllows`（`ActionPump.cs:629/640`，两个重载，形状照 `AbilityLevelCriteriaAllows`），
闸块在 `ActionPump.cs:282-336`，位置 = **`abilityContext` 闸之后、`targetCriteria` 闸之前** ——
即原版 `CanTriggerAbility` 的短路次序 **`IsRightContext` → acting(`+0x28`) → target(`+0x30`) → board(`+0x38`)`**。
三档处置：

- `true` / 明确没有条件 ⇒ 照常入队（不筛）；
- **确定不满足** / **定位不唯一** ⇒ **不入队 + `Debug.LogWarning`**；
- `null`（列没落 / 认不出 / **本路径拿不到 `actingCard`** / 该 `ta` 档没建池器）⇒ **照常入队 + `Debug.LogWarning`**
  （⛔ 不把「不知道」当「不满足」）。

出声判别式 = `RuleCore.AbilityActingCriteriaWarningsForTest`（静态计数，只给自检）。

### 3 · 接线（`EffectResolver` → `RuleCore`）

| 位点 | 改动 |
|---|---|
| `EffectResolver.cs:6897` | `AbilityActingGateApplies(kind)` —— 只有 `Attack` / `Die` 返回 `true` |
| `EffectResolver.cs:6904` | `AbilityActingGateCard(kind, subject, actingCard)` —— `Attack` 取 **`subject`**（攻击者）、`Die` 取 **`actingCard`**（凶手）、其余 `null` |
| `EffectResolver.cs:9290-9291` | `FireBoardWhenListener` → `EnqueueAbility` 传这两格 |
| `EffectResolver.cs:8875 / 8906` | `BroadcastWhen` / `BroadcastWhenCore` 加尾参 `UnitState actingCard = null`（缺省 = 老行为），透传到棋盘那一跳 |
| `RuleCore.cs:9131` | `BroadcastWhen(Die, …)` 传 `actingCard: actingCard`（= `CleanupDeaths` 形参 = 凶手） |
| `RuleCore.cs:9414` | `FlushDeaths` 出批那一跳补 `actingCard: pd.ActingCard`（**行为不变**，理由见 §一） |

### 4 · 🔴 覆盖面（如实 —— 与简报的预期**不一样**，见 §六）

| `trigger` | 落行 | 这一跳真能到吗 | 说明 |
|---|---|---|---|
| **50 `UnitAttack`** | **14 行 / 13 张** | ✅ **能**（`FireBoardWhenListener` 的 Attack 那一跳） | 全是单位：`SW16` `EC1` `EC2` `GSC12` `SAU31` `GOF65` `GOF78` `SW26` `TL11` `TL24` `UM70` `UM83` + **`DA22` ×2** |
| **90 `OtherUnitDeath`** | **12 行** | ⚠️ **到不了** | 其中 **11 行是战术卡**（`GSC40` / `UM34` / `AM39` / `SOR52` / `TL46` / `Feeding Frenzy` / `Master Interromancer` / `Smothering Decree` / `Lord of the Storm` / `Rites of Possession` / `Deadliest Killer`）—— 战术卡**不是棋盘监听者**，这一跳只遍历棋盘单位；剩下 1 行 = `SOR10 Sister Repentia`（单位），但它的卡面是 `When this unit kills an enemy` = 我们的 **`Kills` 事件**（`RuleCore.cs:5695`），也不走 Die |
| **80 `ThisUnitDeath`** | 1 行（`GOF3`） | ⛔ 到不了 | 我们的 `WhenEventKind.Die` **恒映射 `OtherUnitDeath(90)`**（`EffectResolver.cs:6788`）⇒ 查 90 时定位不到那条 80 的 ability |
| 其余 21 个 trigger（`0` `10` `180` `190` `260` `280` `410` `455` `510` `520` `620` `630` `645` `647` `650` `660` `661` `665` `695` `700` `726`） | 56 行 | ⛔ 不到 | 各在**别的入队点**，本批一个都没接（与 E2 那批同样只做「已查清的两个事件」） |

⇒ **今天真正会被这道闸筛到的 = `Attack` 那 14 行**（`DA22` 的 2 行还会先被「定位不唯一」掐掉，见 §四·3）。
⚠️ 而且这 14 行**大体与卡面正文的极性过滤重合**（`UM70`/`SAU31`/`TL24` 的 `enemyUnits=1` 就是正文的 “an enemy attacks”）⇒
**真正能观察到的行为变化很小**（这是好事：说明口径对得上，不是闸没用；但它也意味着 `play check` 零差异**证明不了**这道闸活着 —— 见 §五）。

---

## 四 · 断言（新文件 `Editor/RuleEngineTest_EngineE3.cs`，`Run()` 里加一行）

落地：`RuleEngineTest.cs:1105` 加 `Step(TestE3AbilityActingCriteria)`；
测试体 `RuleEngineTest_EngineE3.cs`（479 行 · LF · 4 组）：

| 组 | 内容 |
|---|---|
| ① 数据管线（真卡池） | `SW16` 的 `ta=20 self` + `UM70` 的 `ta=10 / enemyUnits=1 / includeMinions=1`；**全池 83 行 / 80 张**写死（🧨 落行门槛被改成「36 格非平凡」⇒ 93/86 ⇒ 红） |
| ② 唯一入口三态 | 旧表（`why` 说「没落」）· 明确无条件（`why == null`）· 认不出 · `At(-1)` 恒 null · **两列不共用字典**（同 `ab` 位上 acting 仍 `ta=20`、level 仍 `ta=10`） |
| ③ 闸（直调 `EnqueueAbility`） | 正例（acting 敌方 ⇒ 触发、不出声）· 负例（acting 己方 ⇒ 不触发 + 出声）· **次序**（第一道闸拒绝时第二道闸的计数不动）· 「拿不到 actingCard ⇒ **出声但放行**」· 歧义（2 条同 trigger ⇒ 不触发 + 出声）· 不启用（`abilityActingGate=false` ⇒ 照放行）· 旧表 ⇒ 不筛 |
| ④ 接线（**真广播** `RuleCore.BroadcastWhen`） | **Attack**：监听者 = `When an enemy attacks, deal 2 damage to it`（与 `UM70`/`SAU31` **卡面同文**）+ `enemyUnits=1` ⇒ 敌方攻击**扣 2 血**、己方攻击**不扣**；**Die**：监听者 = `When another troop dies, deal 1 damage to a random enemy`（`SAU68` 同文）+ `ta=20 self` ⇒ acting = 自己 **结算** / acting = 另一个友方 **不结算** / **acting = null ⇒ 出声且照跑**（🧨 若有人把 `subject`（死者）顶成 actingCard ⇒ 这一档会变成「筛掉、不跑」⇒ 红 —— 这是「Die 用凶手不用死者」的**灭自证**） |

**灭自证**还各配了一条方向相反的结构断言：③ 的「第一道闸拒绝时第二道闸计数不动」（挡「两道闸跑成两个独立 if」）、
④a 的「己方攻击不扣血」（挡「候选接成 `target`/`thisCard`」）。

⚠️ **本代理不能跑 Unity**（红线 ①）⇒ **这 4 组一条都没执行过**。下一次 `RuleEngineTest.Run` 是它们的第一次运行；
其中最可能因**夹具前提**而红的是 ④ 的两条 `HasWhenKind(...)` 前提（正文解析），它们已经单列成断言（红得清楚，不会静默）。
⚠️ 另外 §「拿不到 actingCard」那一档，简报 ④ 写的是「**出声不触发**」，我按 **简报 ③（出声并跳过）+ 铁律（⛔ 不把不知道当不满足）**
实现成 **出声 + 放行**；两处口径冲突时我取 ③ —— 若主对话要的是「掐死」，改一行即可（`ActingLevelCriteriaAllows` 的 `candidate == null` 支）。

---

## 五 · 验收读数（如实）

| 命令 | rc | 读数 |
|---|---|---|
| `TMPDIR=/tmp/wf_e3 bash 工具/typecheck.sh` | **0** | 运行时 0 · 编辑器 0（改完 D4 那行后又跑一次，仍 0） |
| `bash 工具/ruleprobe.sh check`（**重钉后第 1 次**） | **0** | **解析差异 0 行** · 改名/新增/消失 0 · **哨兵合计失败 0 条** · ⑦ 输出指纹 `af836aa0…` ✅ |
| ↳ 哨兵 ⑤ `[基线指纹]` | — | ✅ **重钉后通过**：`✓ 卡池与基线的指纹都与钉住的一致`（主对话裁定后重钉 → 见 §五·b） |
| `bash 工具/ruleprobe.sh check`（**第 2 次**） | **0** | 与第 1 次逐条相同（`基线指纹 ✓` · 哨兵失败 0 · 解析差异 0） |
| `bash 工具/ruleprobe.sh play check`（重钉后） | **0** | 主档 / 事件电池 **逐字节零差异**（两段差异列表都是空的）· 结算指纹 `87209677070c8421b8479e3bcc0effab`（与基线一致） |
| `python -I -X utf8 工具/gen_cards_engine.py --check` | 0 | 一致 ✅（幂等见 §二·3） |

> 🔴 **`play check` 零差异 ≠ 这道闸被验过**：探针电池里那 14 张卡的 `Attack` 监听**没有一张被抬起来**（新计数器的告警 0 条）⇒
> 闸**根本没跑**。零差异只能说明「没把别的东西改坏」。真实行为证据 = `RuleEngineTest_EngineE3.cs` 那 4 组（**未执行**，需 Unity）。

### 五·b · `PinnedPoolMd5` 重钉（主对话裁定后执行 · E3-A）

| 项 | 值 |
|---|---|
| 旧 | `af0303790316bc9cd5a5b3f458697988`（卡表 v26 · 3,024,683 B） |
| 新 | **`5a17f6af49ead1829b067bd9e9082118`**（卡表 v27 · 3,119,176 B · +94,493 B） |
| 为什么 | v26→v27 **只加了一列** `abilityActingCriteria`（83 行 / 80 张卡；**既有列的字段一个都没改**）。+94,493 B 全部来自这一列 |
| 位置 | `工具/ruleprobe/Program.cs:241`（`public const string PinnedPoolMd5`）+ 上方归因注释块（**E9/E2 的旧归因保留**，只追加新段） |
| 未动 | `PinnedBaselineMd5` = `71cedcd7…`（`:275`）· `PinnedParseMd5` = `af836aa0…`（`:340`）· 基线文件 · `PlayProbe.cs` 逻辑 —— **一个字没动** |

`git diff --numstat -- Unity/工具/ruleprobe/Program.cs`：`+26 / −1`（只那一行 const 被替换 + 归因注释）；
行尾 CRLF 0 / LF 2314（纯 LF，未翻）。

---

## 六 · 顺手发现 / 与简报不同处（⛔ 只报，等调度台分流）

1. 🔴 **`DA22 Techmarine` 的攻击监听能力已经被「定位不唯一」整条掐掉了**（**不是本批引入**）：
   它有 **2 条 `trigger = 50`** 的 ability（`abilityTargets = [{50,ta:[20]}, {50,ta:[80]}]`）⇒
   `UniqueAbilityIndexForTrigger("UnitAttack")` = `-1` + `ambiguous` ⇒
   **v26 那道 target 闸（E2 那批）就已经按「歧义 ⇒ 不触发」把它拒了**，本批的 acting 闸只是同样拒一次。
   全池「`trigger 50` 有 ≥2 条」的卡 **只有它 1 张**（`trigger 90` 有 0 张）⇒ 影响面 = **1 张卡 / 2 条 ability**。
   **主对话裁定后已查判据** → 结论见 **§六·a**（判据不足 ⇒ 停手，未改行为）。
2. **`Die` 的 `actingCard` 判据已被就地订正**（见 §一）：`E2_ability_criteria管线.md` §六 与 §三·2 的表格行、
   `A3_引擎_at-turn与criteria管线.md` §A3-2 的 `+0x30` 那一行，都加了「⚠️ 2026-10-25 订正」留痕。
   另：`A3` 那一行原来把 **`+0x30` 的被筛对象也抄成了 `actingCard`**（与 E2 §七·1 那条顺手发现同源）—— 一并订正成 `targetCard`。
3. **本批的「可达覆盖率」远低于简报预期**：简报写「Attack / Die 拿得到（上一批 22 张）」——
   实际上 **Die 那 12 行一行都到不了**（11 战术卡 + 1 走 `Kills`），**Attack 14 行**才是本批的全部。
   含义：`actingCardCriteria` 要真正在 Die 上生效，前提是**战术卡的「目标阵亡」监听也得走同一条入队点**
   （那是另一笔：战术卡的 `If it dies` 现在靠正文后置条件 `targetdies`，见 `RuleCore.cs:9118-9148` 的注释）。
4. `RuleEngineTest_EngineD4.cs:84` 的「表版本 = **26**」已被本批 bump 顶红 ⇒ 就地改成 **27**（保留了 F1 的更正痕迹）。
   ⛔ 这条不是放松判据（仍钉确切版本号）。
5. `abilityActingCriteria` 也让「每张卡都落列」多一个键（`cards_engine.json` 体积 +94 KB）。`ruleprobe` 的读数段落**不认这一列**
   （不影响 rc）—— 与 v25/v26 两列同等待遇，不是缺陷。

### 六·a · `DA22` 的 trigger 50 歧义：**判据不足 ⇒ 停手**（E3-B）

#### ① 原版在「两条 ability 同 trigger」时**按什么选** —— 判据：**它不选，逐条独立判、独立结算**

`d:/2/tools/decomp_full/RawCardScript__TriggerAbilities.c` 两条支路都是**遍历 + 逐条判**，没有任何「选一条」的动作：

| 支路 | 原文 | 说明 |
|---|---|---|
| ① 附魔/效果来源（`param_4 + 0x110` → 每条效果的 `+0x290`） | `:108` `cVar1 = CardAbility__CanTriggerAbility(lVar2 /*= abil[iVar7]*/, …)`；`:184` `RawCardScript__TriggerAbility(param_1, uVar5, …)`；`iVar7++` 继续 | **每条 ability 各判一次、各触发一次** |
| ② **卡自己的** `cardAbilities`（`param_1 + 0x290`） | `:207` `cVar1 = CardAbility__CanTriggerAbility(lVar2 /*= cardAbilities[iVar7]*/, …)`；`:231` `RawCardScript__TriggerAbility(…)` | 同上 |

⇒ **没有可供复刻的「选择规则」**（`conditionalAbility` / `abilityLogic` 下标那类机制在 `CanTriggerAbility` 里也不存在：
它只读 `thisCard/actingCard/targetCard` + `*(ability+0x28/0x30/0x38)` 三块 criteria）。
⇒ 原版对 `DA22` 的正确语义 = **两条 ability 各自判各自的条件、各自结算各自的效果**；
我们「一个 trigger 名定位一条 ability」的模型是对这一步的**既有近似**，不是判据缺失的另一半。

#### ② 那要实现「op ↔ ability 下标」还差什么 —— **片段正文的 ability 归属键**

DA22 的两条 ability 是**触发正文片段**（`When a friendly Vehicle attacks, … . If it survives, …`），
不是「整条 desc = 能力块」那种（战术卡）。现仓对片段早有同一道闸，而且**明确拒绝猜**：

- `Core/EffectText.cs:1740` `gap = "fragment-multi-ability"`（触发正文片段 ∧ 卡上有 `ab > 0` 的行 ⇒ **一律不标下标**）；
  `:1746` `gap = "fragment-multi-block"`（片段 ∧ 整卡不止一个能力块 ⇒ 同样不标）。
  ⇒ 今天 DA22 的 op `AbilityIndex = -1`，消费端**没有任何合法输入**能说出「这条 op 属于 `ab=0` 还是 `ab=1`」。
- 为什么不能按「文字序 = ability 数组序」猜：**已有反向实证** `SOR65`（`desc` 印 `Pray: … . Talent: …`，
  而 `abilityTargets = [{46 Talent}, {655 Pray}]` —— 文字序与 `cardAbilities` 序**相反**，
  判据 → `资料/普查产出_1024b/E6_DiedLastTurn与desc路.md` §七·1）。同一纪律写在 `AlignAbilityIndexByBlockHead` 的 doc 里。

**缺的那个键（本代理实测两条路都空）**：

| 可能的数据键 | 实测 | 结论 |
|---|---|---|
| 每条 ability 的**展示文本键** `abilityLogic[].buffEffect.effectText`（原版用它拼 `Card_Abilities/<effectText>`） | `DA22` 两条 ability 的 `effectText` **都是空串**；官方本地化里 `Card_Abilities/*` 提到 `Vehicle` 的键 **0 条** | ❌ 拿不到 |
| 片段里的**能力块头**（`AlignAbilityIndexByBlockHead` 用的 `segLeadKeyword`） | DA22 第二句 `If it survives, …` **没有头**（不是 trigger 头 / 付费前缀）⇒ 整段只有 **1 块**，而 ability 有 **2 条** ⇒ G1 直接不过 | ❌ 拿不到 |

⇒ **结论：判据不足，本批不改行为**（歧义档仍是 **不触发 + 出声**，⛔ 不改成「歧义 ⇒ 触发」）。
要真做，需要下列**任一**前提先成立（都不是本批能造的）：
(a) 每条 ability 的**正文键**（`effectText` / 展示串）在快照里非空，且能对上卡面句子；或
(b) 原版/数据里存在「片段句子 → `cardAbilities` 下标」的显式映射；或
(c) 主对话另立口径（例如「两条 ability 的 36 格**逐格相同**时允许用那一份」—— 本代理**建议**但**未实现**：
    DA22 两条的 36 格确实完全相同（`ta=10 / friendlyUnits=1 / keywordFilter=10`），
    差别只在**不属于本列**的 `cond`/`ex`；这条口径会改掉 E2 那批已上线的歧义处置，属跨批决定）。

---

## 七 · 没查清 / 残余（如实标着，⛔ 不猜）

1. ~~`PinnedPoolMd5` 待重钉~~ → ✅ **主对话已裁定、已重钉**：`af030379…` → **`5a17f6af…`**（v27 · +94,493 B）；
   重钉后 `check` **连跑两次 rc=0** · `play check` rc=0 · 其余两颗钉子未动（见 §五·b）。
2. **其余 21 个 trigger 的 56 行**（`ThisCardPlayed` 2 · `OtherUnitSummoned` 11 · `UnitDeathByAbility` 4 …
   完整分布见 §二·2）—— 那些事件的 `actingCard` 是**哪一格**没查，本批**一个都没接**。
3. **`ThisUnitDeath(80/81)` 那 1 行（`GOF3`）**：我们的 `Die` 广播恒映射 90 ⇒ 这条 ability 查不到自己的行
   ⇒ 走「这张卡没有这条 ability」⇒ 判不了（放行）。要不要把 `WhenEventKind.Die` 拆出 80/81 那一路，是**另一笔**。
4. **`ta = 0` 的 10 行被丢**（与 v26 同口径）。若将来有人要「把 36 格拼回一个完整 `TargetCriteria`」，
   这 10 行仍然不该复活（原版 `return 1`）。
5. **`unitConditions` 的 `role="acting"`**：E2 报告说现读只有 **1 行**（`cond != 0` 那条闸）。本批**没有合并**它
   （本列只落其余 36 格；`cond` / `ex` 仍从 `unitConditions` 取，拼回点在消费端）—— 与 v25/v26 同一分工。
6. **`RuleEngineTest` 未执行**（无 Unity）⇒ 三态与四组断言的实际读数、以及
   「`Attack` 那 14 行会不会在真打里改变行为」都还没跑过。`play check` 那零差异**不能当证据**（见 §五末）。
7. 🔴 **仍开（E3-C · 主对话点名，⛔ 本批不做）**：**`SporeMine_Explosion` 的 trigger 事件不带目标** ⇒
   卡本体位移播不出（要**引擎侧** `trigger` 事件带 `target`）。这是表现层吃不到的字段缺口，
   与本批的 criteria 管线无关；记账即可，⛔ 本代理没动它。
8. **`DA22` 的歧义仍是不触发**（§六·a 判据不足）—— 需要 (a) 每条 ability 的正文键 / (b) 片段句子→下标映射 /
   (c) 主对话另立口径（本代理建议「两条 36 格逐格相同则用那一份」，但**未实现**）。

---

## 八 · 改动清单（**只列本代理改的**；工作区里别人的改动不在此列）

| 文件 | 改动 | 行尾 |
|---|---|---|
| `Unity/工具/gen_cards_engine.py` | 新增 `ability_acting_criteria_of()` + `ABILITY_ACTING_CRITERIA_REPORT`（并含「列名为什么带 `ability` 前缀 / 为什么不落 `lg` / 为什么 `ta=0` 不落」的长注释）· 卡表 build 处写 `abilityActingCriteria` · `version: 26 → 27` + v27 注释块 · 摘要行「目标判据④」· criteria 键集自检把本列并进去 · `set_bad` 出声列表加本列 | CRLF 6171 / LF 6171（未翻） |
| `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` | **重生成**（v27 · +94,493 B · md5 见 §二·3） | CRLF 0 / LF 0（原样） |
| `.../Data/CardDatabase.cs` | `CardDto.abilityActingCriteria`（`+474`）· `MapAbilityActingCriteria`（`+2574`，四道行级闸）· `Parse` 传参 + `>= 27` 版本位（`+1425`）· `< 27` 守卫出声 · `ReportTargetCriteriaColumns` 多一段读数 | CRLF 0 / LF 2965 |
| `.../Core/CardDef.cs` | ctor 尾参 ×2 · `_abilityActingCriteria` + 撞键出声（`+1117`）· `AbilityActingCriteriaVersioned` / `RowCount` / `At(ab)`（`+2613-2631`）· **唯一入口** `ActingLevelCriteriaForTrigger`（`+2649/2657`） | CRLF 0 / LF 7579 |
| `.../Core/ActionPump.cs` | `EnqueueAbility` 两个尾参（`+219`）· **第一道闸**（`+282-336`，在 target 闸之前）· `ActingLevelCriteriaWarningsForTest`（`+602`）· `ActingLevelCriteriaAllows` 两个重载（`+629/640`） | CRLF 0 / LF 1231 |
| `.../Core/EffectResolver.cs` | `BroadcastWhen` / `BroadcastWhenCore` 尾参 `actingCard` · `FireBoardWhenListener` 形参 + 传到 `EnqueueAbility`（`+9290`）· `AbilityActingGateApplies` / `AbilityActingGateCard`（`+6897/6904`）＋ 那两段判据长注释 | CRLF 14037 / LF 14037（未翻） |
| `.../Core/RuleCore.cs` | `BroadcastWhen(Die, …)` 传 `actingCard`（`+9131`）· `FlushDeaths` 回放 `pd.ActingCard`（`+9414`） | CRLF 0 / LF 12327 |
| `.../Editor/RuleEngineTest_EngineE3.cs` | **新增**（479 行 · 4 组断言 + 灭自证） | 新文件，LF |
| `.../Editor/RuleEngineTest_EngineE3.cs.meta` | **新增**（guid `73906b449ceb77fc904407372b8ad595`） | Unity meta 格式 |
| `.../Editor/RuleEngineTest.cs` | `Run()` 加一行 `Step(TestE3AbilityActingCriteria)` + 5 行注释（`+1105`） | CRLF 37038 / LF 37038（未翻） |
| `.../Editor/RuleEngineTest_EngineD4.cs` | 版本断言 `26 → 27` + 更正痕迹（`+84`） | 未翻（改动 1 行块） |
| `Unity/工具/ruleprobe/Program.cs` | **只重钉 `PinnedPoolMd5` 这一颗**（`af030379…` → `5a17f6af…`，`:241`）+ 归因注释（E9/E2 的旧段保留）。见 §五·b | CRLF 0 / LF 2314（纯 LF，未翻） |
| `Unity/资料/普查产出_1025/E2_ability_criteria管线.md` | **订正 2 处**（主对话授权）：§三·2 表格里 `actingCard=deadCard` 那半句 · §六「`Die` 的 = 死者」→ **凶手**；都带「⚠️ 2026-10-25 订正」留痕 | 文档 |
| `Unity/资料/普查产出_1025/A3_引擎_at-turn与criteria管线.md` | **订正 1 处**（主对话授权）：§A3-2 的 `+0x30` 那行被筛对象 `actingCard` → **`targetCard`** + 订正框 | 文档 |

**没改**：`工具/ruleprobe/**` 里**除 `PinnedPoolMd5` 那一行与归因注释之外**的所有内容
（`PinnedBaselineMd5` / `PinnedParseMd5` / `PlayProbe.cs` 逻辑 一个字没动）· 基线文件 ·
`CardPresentation/**`（除白名单外的全部）· 三份正本 · 任何 `任务总表.md` · git。
