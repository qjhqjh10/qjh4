# E2 · ability 级 `targetCriteria` 的 36 格：数据管线 + 触发前闸（执行报告）

> **执行代理 `E2`** · 2026-10-25 · 第七十五会话 · 判据源 = 代理 `A3`
> （`资料/普查产出_1025/A3_引擎_at-turn与criteria管线.md` §A3-2，**没有重新考据**）
> 白名单使用：`MyGame/Assets/RuleEngine/**`（`Core/` + `Data/` + `Editor/RuleEngineTest*`）· `工具/gen_cards_engine.py`
> ⛔ 没碰：`CardPresentation/Battle/**` · `Core/`（表现层那个）· `Shell/**` · `Editor/BattleScene.cs` ·
> `工具/ruleprobe/**`（**只运行，没改一个字节**）· 三份正本与任何 `任务总表.md`。
> `git` 一个字没动（不 commit / 不 checkout / 不 stash）。

---

## 〇 · 一句话结论

`CardAbility.targetCriteria`（**ability 级**，`+0x30`）那 36 格从「数据里有、管线没落」补到了
**引擎读得到 + 一道真闸**：新增卡表列 **`abilityLevelCriteria`（v26）** ·
`CardDef.AbilityLevelCriteriaForTrigger`（**唯一入口**）· `ActionPump.EnqueueAbility` 的
触发前闸（**今天只接 `Attack` / `Die` 两个事件 = 22 行 / 22 张卡**）。
`ruleprobe check` rc=0（解析差异 0 行；**池指纹哨兵按设计报「卡池变了」**，**没有重钉**）·
`ruleprobe play check` rc=0（主档 1127 张 + 电池 586 张**逐字节零差异**）·
离线探针 **33 条断言全过**。

🔴 **一条判据订正（与简报不同，以反编译为准）**：这道闸**筛的是 `targetCard`，不是 `actingCard`**。
详见 §一·1。

---

## 一 · 判据（逐条，第一权威 = 全量反编译）

### 1 · 🔴 订正：被筛对象是 **`targetCard`**

简报（与 `A3-2` §1）写的是「触发前闸（筛 `actingCard`）」。**反编译逐句读下来不是这样**：

```
CardAbility__CanTriggerAbility.c:127-135
  CheckIfMeetsCriteria(thisCard, actingCard, targetCard, *(ability+0x28), 0)   // 第一道闸
  CheckIfMeetsCriteria(thisCard, targetCard,  targetCard, *(ability+0x30), 0)   // ★ 本闸
  BoardAnalysis.MeetsCriteria(bm, *(ability+0x38), thisCard, actingCard, targetCard, …)
```

- 形参表（`d:/2/tools/il2cpp_out/dump.cs:20360`）：
  `CanTriggerAbility(AbilityTrigger trigger, BattleManager manager, CardScript thisCard,
   CardScript actingCard, CardScript targetCard, bool ignoreRandomTrigger)`
  ⇒ 反编译里的 `param_4 = thisCard` · `param_5 = actingCard` · `param_6 = targetCard`。
- 第二道闸把 **`param_6` 同时放在第 2、3 个实参**（`CheckIfMeetsCriteria` 的 `param_2`／`param_3`）。
- `FilterMethods__CheckIfMeetsCriteria.c:50` `*(lVar9 + 0x10) = param_2` —— `param_2` 就是**被筛的那个**；
  随后 `:216-263` 拿它和 `param_1`（thisCard）比阵营、身份。
- `Callers`：`RawCardScript__TriggerAbilities.c:108,207` 是**唯一**的广播入口。

⇒ **`actingCardCriteria`（`+0x28`）筛 `actingCard`；`targetCriteria`（`+0x30`，本笔）筛 `targetCard`。**
两闸候选**不同**。本笔的消费点按这个实现（合成探针里有一条**灭自证**：把候选接成 `actingCard`
⇒ 原本「满足」的正例变成「不满足」）。

### 2 · `targetsAffected == 0 (any)` = 零信息

`FilterMethods__CheckIfMeetsCriteria.c:56-59`：`iVar17 = *(criteria+0x10)`，`== 0` ⇒ **直接 `return 1`**
⇒ 其余 35 格全部失效。⇒ 生成器**不落**这种行（口径复用 `criteria_carries_targets`，
与 v22 的 `mult`/`multi`/`avail`/`choice` 同一份判据）。现读池内 1127 张里 **1185 条 ability 的
`targetCriteria` 是 `ta = 0`（全池 1496 条中 1405 条）⇒ 只有 85 条落行**。

### 3 · 与 v25 那一列**不是同一层**

| 列 | 原版对象 | 偏移 | `lg` | 消费点 |
|---|---|---|---|---|
| `abilityTargetCriteria`（v25） | `AbilityLogic.targetCriteria` | `+0x28`（`AbilityLogic`） | `>= 0` | `AbilityLogic__PlayAbility.c:262` → **建目标池** |
| **`abilityLevelCriteria`（v26 · 本笔）** | **`CardAbility.targetCriteria`** | **`+0x30`（`CardAbility`）** | **恒 `-1`** | `CardAbility__CanTriggerAbility.c:131-135` → **触发前闸** |

⛔ 不许合进一个字典：`lg = -1` 与 `lg >= 0` 会撞键（`CardDef` 的 `CriteriaKey(ab, lg)` 用的是同一段
`long` 键空间）。本笔**另开** `Dictionary<int, CountSetSpec> _abilityLevelCriteria`（键 = `ab`）。

---

## 二 · E2-1 数据管线（必做）

### 1 · 新增列 `abilityLevelCriteria`（卡表 v25 → **v26**）

形状（`JsonUtility` 不支持字典 ⇒ 数组；36 格装子对象）：

```json
"abilityLevelCriteria": [{"ab": 0, "trigger": 50, "criteria": {36 格 + playerTurn}}]
```

- `ab` = `cardAbilities` 下标（**唯一**对齐键；`trigger` 只作可读性 —— 实测同卡两条 ability
  共用同一个 `trigger` 的情形存在，⛔ 别只按 trigger 对齐）；
- **`lg` 不落**（本列恒 `-1`；DTO 的字段初值就是 `-1`）；
- 36 格 = 直接复用 `criteria_set_of()`（**同一份字段表**，⛔ 不另抄）；
- `playerTurn` 结构上不适用 ⇒ `-1`、**不进 `set_bad`**（同 v22 那四格）；
- **每张卡都落**（没有行 = `[]`）—— 「缺列」与「空数组」必须分得开。

现读：**1127/1127 张都落列**（`[]` 1052 张 + 有行 75 张 · **85 行**）·
`ta` 分布：`10 allFiltered` 62 · `30 target` 8（等下 · 见下）· `90` 7 · `20 self` 7 · `55` 1。

### 2 · DTO / `CardDef`

| 文件 | 改动 |
|---|---|
| `Data/CardDatabase.cs` | `CardDto.abilityLevelCriteria`（复用 `TargetCriteriaRowDto`）· `MapAbilityLevelCriteria`（**四道行级闸**：缺 `ab` / `ab<0` / `lg != -1` / `criteria == null` ⇒ **出声丢弃那一行**）· `Parse` 传参 + `abilityLevelCriteriaVersioned: dto.version >= 26` · `version < 26` 守卫出声 · `ReportTargetCriteriaColumns` 多报一段读数 |
| `Core/CardDef.cs` | ctor 两个尾参（`abilityLevelCriteria` / `…Versioned`）· `_abilityLevelCriteria`（键 = `ab`，撞键出声）· `AbilityLevelCriteriaVersioned` · `AbilityLevelCriteriaRowCount` |
| `Core/CardDef.cs` | **唯一入口**：`AbilityLevelCriteriaForTrigger(triggerName, out why[, out ambiguous])` = 「按 trigger 名定位唯一 ability」+「取那一行的 36 格」；底层 `UniqueAbilityIndexForTrigger(...)`（数据源 = `AbilityTargets`，**下标 = `cardAbilities` 下标**）· `AbilityLevelCriteriaAt(ab)` |

**「唯一入口」怎么保证不在两处各算一次**：全仓**只有** `UniqueAbilityIndexForTrigger` 会扫
`AbilityTargets` 做「trigger 名 → 下标」；`CardDef.AbilityLevelCriteriaForTrigger` 是唯一的
「定位 + 取值」组合口；`RuleCore.AbilityLevelCriteriaAllows` 是唯一的**消费实现**。
（`ActiveAbility.targetCriteria` **不在本列** —— 见 §五·2。）

**取不到时「明确空」而不是默认值**（三态，探针逐档验过）：

| 档 | 返回 | `why` |
|---|---|---|
| 有数据 | `set` | `null` |
| **明确没有这一格条件**（`ta = any` / 没落行） | `null` | **`null`** |
| 列没落（旧表 `< 26`） | `null` | 「没落」 |
| 认不出 trigger 名 | `null` | 「认不出」 |
| 这张卡没有这条 ability | `null` | 「没有」 |
| 🔴 **定位不唯一**（同卡 ≥2 条同 trigger） | `null` | 「不唯一」+ `ambiguous = true` |

### 3 · 生成器幂等（证据）

```
改前 cards_engine.json  md5 = 860044b773370da0b70a0134d3a7463d   2,929,811 B   (version 25)
改后 cards_engine.json  md5 = af0303790316bc9cd5a5b3f458697988   3,024,683 B   (version 26)  +94,872 B
连跑两次 `--write`：md5 逐字节相同  af0303790316bc9cd5a5b3f458697988  ✅
python -I -X utf8 工具/gen_cards_engine.py --check  → 一致 ✅
```

行尾：`cards_engine.json` **本来就没有换行符**（紧凑单行；CRLF 0 / LF 0），写出来仍是（CRLF 0 / LF 0）。
生成器自己那段 `v25 自检`（criteria 键集/键序）已把本列一起算进去：**1894 行 × 37 键全部一致**
（1809 + 本笔 85）。

生成器读数（新加的一行）：

```
目标判据③    `abilityLevelCriteria` 落值 1127/1127 张 · 有行的 75 张 · 85 行
             36 格「有信息」的行 85 行 · `ta == 0 (any)` **不落** 1185 行
             按 targetsAffected：10:62 · 30:8 · 90:7 · 20:7 · 55:1
```

> ⚠️ 与 `A3-2` 的读数对账：`A3` 写「98 条 / 87 张（池内）」；本笔现读 **85 行 / 75 张**。
> 差额 = `A3` 把 **13 条 `ta = 0`** 也算进了「非平凡」，而它们按原版 `return 1` 是**零信息**
> ⇒ 本笔按 `criteria_carries_targets` **不落**（85 = 98 − 13，行数对得上）。
> 卡数 87 vs 75 的差额同理（那 13 行分布在不会再被计数的卡上）。**这是口径差，不是数据差。**

---

## 三 · E2-2 消费点（最小可验证版）

### 1 · 闸在哪 / 怎么接

`Core/ActionPump.cs` 的 `RuleCore.EnqueueAbility`（原版「广播那一刻、入队之前」的等价位置）
新增两个**缺省关闭**的尾参：

```csharp
string abilityTrigger = null,   // 原版 AbilityTrigger 枚举名（⛔ 不是引擎关键词）
UnitState abilityTarget = null  // 原版 targetCard（🔴 不是 actingCard）
```

两者都给了才启用。判定走 `RuleCore.AbilityLevelCriteriaAllows`：

- `true` / **明确没有条件** ⇒ 照常入队（不筛）；
- `false`（**确定不满足**，或**定位不唯一**）⇒ **不入队 + `Debug.LogWarning`**；
- `null`（判不了：列没落 / 认不出 / 本路径没有 `targetCard` / 该 `ta` 档本版没建池器）
  ⇒ **照常入队 + `Debug.LogWarning`**（⛔ 不把「不知道」当「不满足」）。

「出声」的判别式 = `RuleCore.AbilityLevelCriteriaWarningsForTest`（静态计数，**只给自检**；
`Debug.LogWarning` 的输出流在自检/探针里抓不到 —— 同 `A3` 的 `AbilityVersionFlagWarningsForTest` 形状）。

### 2 · 接到哪一条路（**只接已查清 `targetCard` 的两族**）

`Core/EffectResolver.cs` 的 `FireBoardWhenListener` → `EnqueueAbility`（棋盘监听器那一跳）传参，
由两个小函数决定：

| 事件 | 我们的 `targetCard` | 判据 |
|---|---|---|
| **`Attack`** | `BroadcastWhen` 的 `target`（**被打者**） | `CardScript__ResolveUnitAttacked.c:28` `OnTrigger(0x32, bm, thisCard, param_2, param_3)`；`BattleManagerSupport__BroadcastUnitAttacked.c:22/:29` 两跳的 `param_3` 都是被打者 ⇒ `RuleCore.cs:6300` 传的 `target` |
| **`Die`** | `subject`（**刚死的那个**；我们这一跳没有 `target`） | `CardScript__TriggerOnMinionDeath.c:97-99` → `OnMinionDeath(…, thisCard, actingCard, **targetCard=deadCard**)`（⚠️ 2026-10-25 订正：原句写的 `actingCard=deadCard` 是错的 —— `actingCard` 是**凶手**，见 §六 的订正框；`targetCard=deadCard` 这一半不变，本闸结论照旧） |
| ⛔ 其余（含 **`Damaged`**） | **不启用** | `Damaged` 的 `targetCard` 是 `ResolveDamageDealt` 的 `param_2`（**伤害来源**），而我们那一跳 `BroadcastWhen(Damaged, …, subject: 被打者)` **没有来源那一格** ⇒ 拿 `subject` 顶就是**筛错人**。判据没查清就先不接（铁律 2） |
| ↳ ⚠️ **2026-10-25 订正（执行代理 `E5` 现核 · 铁律 5）** | —— | **上面那一格对 `Damaged` 的判据读错了**：`CardScript__ResolveDamageDealt(param_1 = 观察者, param_2 = **被打的那张**, param_3 = DamageInfo)`（`BattleManagerSupport__BroadcastUnitDamaged.c:19` 对被打者自己调 `ResolveDamageDealt(param_2, param_2, param_3)`、`:59/:70/:82` 对每张在场卡调 `(lVar3, param_2, param_3)`）⇒ 触发 60 那一跳是 `OnTrigger(0x3c, …, thisCard=param_1, actingCard=DamageInfo.actingCard(+0x10), targetCard=**param_2 = 被打者**)`（`CardScript__ResolveDamageDealt.c:161-163`）。⇒ **`targetCard` = 被打者 = 我们的 `subject`（不是伤害来源）**，所以**目标闸那一道是可以接的**（唯一真正缺的是 `actingCard` = 伤害来源，见 `E5_引擎剩余小缺口.md` §E5-5）。⛔ 别再把这条当「拿 `subject` 顶就是筛错人」。 |

### 3 · 覆盖面（如实）

`abilityLevelCriteria` 现读 **85 行 / 75 张卡**，本轮真接上的：

| 档 | 行数 | 说明 |
|---|---|---|
| ✅ **已接**（`Attack` 6 + `Die` 16） | **22 行 / 22 张卡** | 判据已在反编译里逐句核过 |
| ⚠️ `TriggerNameOfWhenKind` 认得出、但 `targetCard` 没查清 | 11 行（`Reanimated` 5 · `UnitReceivesMarkOfChaos` 4 · `UnitReceivesMarkerlight` 1 · `UnitDamaged` 1） | 补判据后可以直接接（同一套参数） |
| ⛔ 根本不在这个入队点（别的触发路径） | 52 行（`ThisCardPlayed` 40 · `FollowUpResolution` 5 · `Strike` 2 · `UnitStunned` 2 · `AboutToAttack` 1 · `TriggeredSwarm` 1 · `OnSabotageCreated` 1） | 要去各自的入队点接（`ThisCardPlayed` 那 40 行最大宗） |

### 4 · 🔴「定位不唯一」按简报处置 = **不触发**

简报写「歧义例（定位不唯一 ⇒ 不触发 + 出声）」，照办。现读池内**只有 1 张卡**会走到这一档：
**`SW9 Fenrisian Wolf`**（2 条 ability 都是 `trigger 410 FollowUpResolution`，2 行都有 criteria）。
⇒ 生产影响面 = **1 张卡**；且 410 那一族**不在**本轮接上的两个事件里 ⇒ **今天实际上一次都不会触发**。
⚠️ 如果将来把 410 也接上，这张卡的 410 会被整条掐掉 —— 到那时要么补「op ↔ ability 下标」的判据，
要么改这一档的处置（**这是主对话要裁的一条**，不是我自己改的）。

### 5 · 断言（新文件 `Editor/RuleEngineTest_EngineE2.cs`，`Run()` 里加一行）

正例（满足 ⇒ 触发）/ 负例（不满足 ⇒ 不触发 + 出声）/ 歧义例（不唯一 ⇒ 不触发 + 出声）/
「没有 `targetCard` ⇒ 判不了 ⇒ 放行 + 出声」/ 旧表（列没落 ⇒ 整道闸不启用）/ **灭自证**
（把候选接成 `actingCard` ⇒ 正例变负例，证明筛的是 `targetCard`）/ 真卡 `SW16` 的 36 格逐格对账。

---

## 四 · 验收读数（如实）

| 命令 | rc | 读数 |
|---|---|---|
| `TMPDIR=/tmp/wf_e2 bash 工具/typecheck.sh` | 0 | 运行时 0 · 编辑器 0 |
| `bash 工具/ruleprobe.sh check` | **0** | **解析差异 0 行 · 哨兵合计失败 0 条** · ⑦ 输出指纹与确认版一致（`af836aa0…`，与卡池无关） |
| ↳ 哨兵 ⑤ `[基线指纹]` | — | ✅ **重钉后通过**（`PinnedPoolMd5` 已由主对话裁定重钉 → 见 **§八**）：`✓ 卡池与基线的指纹都与钉住的一致`。`PinnedBaselineMd5` / `PinnedParseMd5` **没动**（基线文件与解析输出都没变） |
| `bash 工具/ruleprobe.sh play check` | **0** | 主档 **1127 张逐字节零差异** · 事件电池 **586 张逐字节零差异** · 结算指纹 `87209677070c8421b8479e3bcc0effab`（与基线一致） |
| 离线探针（`_tmp_view/e2probe`，临时，已清） | 0 | **33 条断言全过**（数据 13 + 三态 6 + 闸 14） |

> 🔴 **`play check` 零差异 ≠ 「验过了」**：那 22 张卡的 `Attack`/`Die` 事件在探针的电池里
> **一次都没被抬起来**（探针输出里 `[ActionQueue] … targetCriteria` 的告警 **0 条** ⇒ 闸**根本没跑**）。
> 这一层的真实行为证据 = **离线探针那 14 条**（真跑 `EnqueueAbility`），**不是** `play check`。
> 正式验收仍然要 `RuleEngineTest.Run`（需要 Unity，本代理不跑）。

---

## 五 · 没查清 / 残余（如实标着）

1. **`activeAbility.targetCriteria` 是另一个对象、另一个消费点**（不在本笔）：
   `RawCardScript__IsValidActiveAbilityTarget.c:103-107` 读 `activeAbility(+0x260)` 内部 **`+0x38`**，
   筛的是**主动能力的目标候选**（不是触发闸）。现读全池 `activeAbility.targetCriteria` **非平凡 132 条**
   ⇒ 这是一条**独立**的 36 格缺口（比本笔那 85 行还大）。**建议另账**，形状可照本笔（`ab = -1` / `-2`）。
2. **63 行没接**（见 §三·3 的表）—— 其中 `ThisCardPlayed` 40 行最大宗，要回到「打出那一刻」的入队点。
3. **「该 `ta` 档本版没建池器」**：`TargetCriteriaEval` 对 `ta = 50/100/105/106/110/113/115` 返回 `null`
   （判不了 ⇒ 放行 + 出声）。本笔这 85 行里 `ta` 只出现 `10/20/30/55/90`，其中 **`55 inPlayOrDyingFiltered` 1 行**
   本版**没有**建池器 ⇒ 那一行永远走「判不了」档（放行 + 出声）。
4. **`unitConditions` 里 `role="target"` + `lg=-1` 的那 27 行**：它们是**同一批 ability 级 criteria 的
   `cond`/`ex` 两格**（`A3-2` 已写明）。本笔的 36 格**不**与它们合并（那张卡两处各读一份）——
   将来若要「拼回一个完整 `TargetCriteria`」，合成点在消费端（`cond` 从 `unitConditions` 取）。
   今天这 27 行里与本笔 85 行**有交集多少没逐个核**（两边的落行门槛不同：`cond != 0` vs `ta != 0`）。
5. **歧义档的处置**（§三·4）：`SW9` 1 张卡，等主对话裁。

---

## 六 · 顺手核的一件：`actingCardCriteria`（要不要照同一套补）

**结论：要补，判据与 E2-1 同源，但它是「另一闸的候选」，形状必须另开一列；影响面比本笔还大一点。**

| | `actingCardCriteria`（`+0x28`） | 本笔 `targetCriteria`（`+0x30`） |
|---|---|---|
| 消费点 | `CardAbility__CanTriggerAbility.c:127-130` | 同函数 `:131-135` |
| 被筛对象 | **`actingCard`**（`param_5`） | **`targetCard`**（`param_6`） |
| 现读（池内 1127 张） | **93 行 / 86 张卡**（全部 36 格非平凡） | 85 行 / 75 张卡 |
| 按本笔的落行规则（`ta != 0`）会落 | **83 行 / 80 张卡**（`ta`：`10` 53 · `20 self` 30 · `0` 10 ⇒ 那 10 行零信息不落） | 85 行 / 75 张 |
| 已落的 `cond`/`ex` | `unitConditions` 的 `role="acting"` 只有 **1 行**（because 「缺行 = 没有条件」那条闸要求 `cond != 0`） | `role="target"` + `lg=-1` 27 行 |
| 触发分布（前几档） | `90` 14 · `50` 14 · `190` 11 · `10` 7 · `410` 6 · `0` 4 · `180` 4 | `0` 40 · `90` 17 · `50` 7 · `410` 5 |

**怎么做（建议，与本笔同一套四步）**：
① 生成器加一列 `abilityActingCriteria`（形状与 `abilityLevelCriteria` 逐字同形，只读 `cardAbilities[i].actingCardCriteria`）；
② DTO 复用 `TargetCriteriaRowDto`；③ `CardDef` 另开 `_abilityActingCriteria`（**⛔ 不要与本笔共用键**）；
④ 闸挂在同一个 `EnqueueAbility`（可以合成一把闸：两个候选各判一次，短路次序照原版
`acting → target → board`）。

**⚠️ 真正的难点不在数据，在「`actingCard` 我们有没有 `UnitState`」**：本笔接的两个事件里
`Attack` 的 `actingCard` = 攻击者、`Die` 的 = **凶手** —— 都在我们的 `subject` / `actor` 上，**拿得到**；
> ⚠️ **2026-10-25 订正（执行代理 `E3` · 铁律 5）**：本行原来写「`Die` 的 = **死者**」—— **错了**。
> 判据（第一权威 = 全量反编译）：`RawCardScript.OnMinionDeath(BattleManager manager, CardScript thisCard,
> CardScript **actingCard**, CardScript **deadCard**, …)`（`d:/2/tools/il2cpp_out/dump.cs:23526`）
> —— **`actingCard` 与 `deadCard` 是两个形参**；`CardScript__TriggerOnMinionDeath.c` 里只有
> **80/81 `ThisUnitDeath`** 那一支把 `thisCard` 当 `actingCard` 传，**90 `OtherUnitDeath`** 传的是调用方给的来源卡
> （= `BattleAction.actingCard` / `DamageInfo.actingCard`，**凶手**）。⇒ 我们这一路 = `CleanupDeaths` 的
> `actingCard` 形参，**⛔ 不是 `subject`（死者）**；拿不到（毒 / 反噬 / 反击 / 无来源清场）⇒ 判不了 ⇒ 出声并跳过。
> 归因与落地 → `资料/普查产出_1025/E3_actingCardCriteria.md` §一 / §三。

但 `ta = 20 self` 那 30 行（「acting 就是我」）会**直接跟 `thisCard` 比身份**，需要的是同一个 `UnitState`。
⇒ 先做 `Attack` / `Die` 两族（与本笔同一批 22 张卡），其余按事件逐族查清再扩。

**⛔ 未查清的部分**：`actingCard` 在 `ThisCardPlayed` / 战术卡 / 常驻那几条路上分别是哪一格
（`subject` ? `card` ? 都没有？）—— **没查**，所以不建议一次全铺。

---

## 七 · 顺手发现（⛔ 只报不改，供调度台分流）

1. **`A3-2` 的「筛 actingCard」是错的**（见 §一·1）。若有人照它做 `actingCardCriteria` 那一笔，
   会把两个候选搞反 —— **建议在 `A3` 那篇的 §A3-2·1 就地订正**（那是主对话的正本，我没改）。
2. **`ruleprobe` 的池指纹哨兵对「卡池变了」是「不判」而不是「红」**（本轮亲见：`rc` 仍 0，
   只在正文里 ⚠️ 一行）。⇒ 加列这类改动**不会被 rc 挡住**，只能靠人读那一行。
   （与 `A1619` 那条「三颗指纹」的注释一致，但「不判」这个措辞很容易被当成「过了」。）
3. **`activeAbility.targetCriteria` 的 132 条非平凡行**（§五·1）—— 比本笔大的一个独立缺口。
4. `AbilityLevelCriteriaWarningsForTest` 是**全局计数**（不区分卡/事件），自检里只能断「涨了没有」；
   要定位是哪张卡在刷，得看日志正文（今天只接了 2 个事件，噪声面很小）。

---

## 八 · 追加：`PinnedPoolMd5` 重钉（主对话裁定后执行）

> 主对话在同步点裁定：**允许只重钉 `PinnedPoolMd5` 这一颗**（差异全部来自本批新增列）。
> ⛔ 其余两颗（`PinnedBaselineMd5` / `PinnedParseMd5`）**一个字没动**。

| 项 | 值 |
|---|---|
| 旧 | `860044b773370da0b70a0134d3a7463d`（卡表 v25 · 2,929,811 B） |
| 新 | **`af0303790316bc9cd5a5b3f458697988`**（卡表 v26 · 3,024,683 B · +94,872 B） |
| 为什么 | v25→v26 **只加了一列** `abilityLevelCriteria`（85 行 / 75 张卡；**既有列的字段一个都没改**）。+94,872 B 全部来自这一列 |
| 位置 | `工具/ruleprobe/Program.cs:232`（`public const string PinnedPoolMd5`）+ 上方归因注释块（**旧的 `E9`/v25 归因保留**，只追加新段） |

**重钉后读数（两次独立运行，均取脚本自身 rc）：**

| 命令 | 1st | 2nd |
|---|---|---|
| `bash 工具/ruleprobe.sh check` | **rc=0** · 哨兵合计**失败 0 条** · 解析差异 **0 行** · ⑤ `[基线指纹]` **✓ 卡池与基线的指纹都与钉住的一致** · ⑦ `[输出指纹]` ✓ `af836aa0…` | **rc=0** · 同上（逐条相同） |
| `bash 工具/ruleprobe.sh play check` | **rc=0** · 主档 1127 张 + 电池 586 张**逐字节零差异** · 结算指纹 `87209677070c8421b8479e3bcc0effab`（与基线一致） | —— |
| `python -I -X utf8 工具/gen_cards_engine.py --check` | **一致 ✅**（幂等：两次 `--write` 逐字节相同） | —— |

`git diff --stat -- Unity/工具/ruleprobe`：`Program.cs +17 / −1`（**只那一行被替换 + 归因注释**）；
同目录 `PlayProbe.cs`(+20) 与 `基线/out_play_baseline.txt`(8) 的改动**在本次会话开始前就已存在**
（会话开头的 `gitStatus` 快照里就有，属同批 `A1/A2` 的重钉）—— **不是本代理改的**。
`Program.cs` 行尾：CRLF 0 / LF 2304（纯 LF，未翻）。

---

## 九 · 改动清单

| 文件 | 改动 |
|---|---|
| `工具/gen_cards_engine.py` | 新增 `ability_level_criteria_of()` + `ABILITY_LEVEL_CRITERIA_REPORT`（含长注释：两层的区别 / 偏移 / 为什么 `ta=0` 不落）· 卡表 build 处写 `abilityLevelCriteria` · `version: 25 → 26` + v26 注释块 · 摘要行「目标判据③」· v25 键集自检把本列并进去 |
| `MyGame/Assets/RuleEngine/Resources/cards_engine.json` | **重生成**（v26 · +94,872 B · md5 见 §二·3） |
| `MyGame/Assets/RuleEngine/Data/CardDatabase.cs` | `CardDto.abilityLevelCriteria` · `MapAbilityLevelCriteria` · `Parse` 传参 + 版本位 + `< 26` 守卫 · `ReportTargetCriteriaColumns` 多一段 |
| `MyGame/Assets/RuleEngine/Core/CardDef.cs` | ctor 尾参 ×2 · `_abilityLevelCriteria` + 撞键出声 · `AbilityLevelCriteriaVersioned` / `RowCount` / `At(ab)` / `UniqueAbilityIndexForTrigger` / `AbilityLevelCriteriaForTrigger` |
| `MyGame/Assets/RuleEngine/Core/ActionPump.cs` | `EnqueueAbility` 两个尾参 + 触发前闸 + `AbilityLevelCriteriaAllows`（两个重载）+ `AbilityLevelCriteriaWarningsForTest` |
| `MyGame/Assets/RuleEngine/Core/EffectResolver.cs` | `FireBoardWhenListener` → `EnqueueAbility` 传参 · `AbilityLevelGateTrigger` / `AbilityLevelGateTarget`（**只放行 `Attack`/`Die`**）+ 判据块 |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineE2.cs` | **新增**（3 组 · 真卡对账 + 三态 + 闸的四档 + 灭自证） |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | `Run()` 加一行 `Step(TestE2AbilityLevelCriteria)`（+5 行注释） |
| `工具/ruleprobe/Program.cs` | **只重钉 `PinnedPoolMd5` 这一颗**（`860044b7…` → `af030379…`）+ 归因注释（旧的 `E9`/v25 归因保留）。详见 §八 |

**没改**：`工具/ruleprobe/**` 里**除 `PinnedPoolMd5` 那一行以外的所有内容**（`PinnedBaselineMd5` / `PinnedParseMd5` /
`PlayProbe.cs` 逻辑 一个字没动）· 基线文件 `基线/out_play_baseline.txt` / `out_baseline.txt` ·
表现层（`CardPresentation/**`）· 三份正本 · 任何 `任务总表.md` · git。
> ⚠️ `git status` 里 `工具/ruleprobe/PlayProbe.cs`（+20）与 `基线/out_play_baseline.txt` 那两处
> **在本次会话开始前就已经是 M**（`gitStatus` 快照里就有，同批 `A1/A2` 的重钉），**不是本代理的改动**。
**临时产物已清**：`d:/4/_tmp_view/e2probe/`（离线探针，跑完删）。

## 行尾核对（改完逐个现数）

```
Core/ActionPump.cs            CRLF 0      LF 1080
Core/CardDef.cs               CRLF 0      LF 7468     （LF，未翻）
Core/EffectResolver.cs        CRLF 13975  LF 13975    （CRLF，未翻 —— 两数相等）
Data/CardDatabase.cs          CRLF 0      LF 2834     （LF，未翻）
Editor/RuleEngineTest.cs      CRLF 37031  LF 37031    （CRLF，未翻 —— 两数相等）
Editor/RuleEngineTest_EngineE2.cs  CRLF 0  LF 298     （新文件，照 `_EngineA3` 用 LF）
工具/gen_cards_engine.py      CRLF 6044   LF 6044     （CRLF，未翻 —— 两数相等）
工具/ruleprobe/Program.cs     CRLF 0      LF 2304     （LF，未翻 —— 只那一行 const 被替换）
Resources/cards_engine.json   CRLF 0      LF 0        （紧凑单行，原样）
```
