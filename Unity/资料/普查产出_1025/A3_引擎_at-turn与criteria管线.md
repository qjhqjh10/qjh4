# 执行代理 `A3` · 引擎线三件 · 执行报告（2026-10-25 · 第七十五会话）

> 白名单使用：`MyGame/Assets/RuleEngine/**`（`Core/` + `Editor/RuleEngineTest*`）· `工具/gen_cards_engine.py`**没动**（理由见 §A3-2）。
> 红线：⛔ 没跑 Unity（任何 `-executeMethod` / `Unity.exe`）· ⛔ 没动 git · ⛔ 没改 `CLAUDE.md` / `项目任务.md` / 任何 `任务总表.md` ·
> 改文件一律 Edit/Write（没碰 `sed -i`）· 行尾逐文件核过（见 §四）。

| # | 活 | 结果 |
|---|---|---|
| **A3-1** | at-turn 卡片的 criteria 贴片（`deal`/`give` 那一族） | ✅ **已做**：新增总入口 `AttachAtTurnBodyCriteria` + `AttachAtTurnDealGiveCriteria`；**5 张**真卡 + **1 张**手牌陷阱真贴上，**5 张判据不足的按「说不清就不贴」钉住** |
| **A3-2** | ability 级 criteria 的 36 格数据管线 | ✅ **判据 + 方案（⛔ 未落地）** —— 不在「`abilityTargetCriteria` 那一列」，是 `CardAbility.targetCriteria`（另一层）；缺 **4 步**、影响 **98 abilities / 87 张卡** |
| **A3-3** | `CardDef` ctor 的静默陷阱 | ✅ **已做**：判定「不是原版语义」⇒ **出声**（两个方向）+ 测试计数判别式 + `AbilityTriggerCount` |

**验收读数（本批最后一次现跑，2026-10-25 01:19）**

```
TMPDIR=/tmp/wf_a3 bash d:/4/Unity/工具/typecheck.sh
  → 运行时错误数: 0 · 编辑器错误数: 0
bash d:/4/Unity/工具/ruleprobe.sh check
  → 全池 1121 张 · 解析差异 0 行 · 哨兵合计 失败 0 条 → rc=0
bash d:/4/Unity/工具/ruleprobe.sh play check
  → 结算指纹 87209677070c8421b8479e3bcc0effab == 基线（一致）
    ①-a 主档「1127 张逐字节一致」· ①-b 电池「586 张逐字节一致」· ② 哨兵 ✓ → rc=0
python 工具/gen_diag_runner.py            （Run() 333 条 → 产物重写了 333 条）
python 工具/gen_diag_runner.py --check    → ✅ 逐字节相同（45,485 B）
【离线真跑】d:/4/_tmp_view/a3probe（临时 csproj，编 Core + Data/CardDatabase + Stub；跑完已删）
  → `=== A3 离线复核：pass 109 · fail 0 ===`
```

🔴 **`PinnedPlayMd5` / 卡池一个字节没动** —— 本批对 `play` 基线**零可观测差异**（**这不是「没验」，是「按设计应当是零」**：理由见 §A3-1·4）。
⚠️ `RuleEngineTest.Run` 没跑（子代理禁止跑 Unity）⇒ 新增的那 4 组断言（**静态 58 处 `Check`/`CheckTrue` 调用**、
动态约 100 条）**只过了 typecheck**，
但**我用一个临时离屏探针把同一批逻辑在 net8 上真跑了一遍**（**109/109 通过**，探针目录已删、`git status` 里没有它）。

---

## A3-1 · at-turn 卡片的 criteria 贴片

### 1 · 先把名单列清楚（A2 说的「12 张」= 现读 **11 张 `deal`/`give`** + `GOF86` 那张 forceattack）

`cards_engine.json` v25 现读 **22 张**带 at-turn 从句的卡。**逐张**跑一遍生产解析路
（`EffectText.AtTurnClauses` → `Parse(cl.Body)`，**不带 card**，与 `RuleCore.ResolveAtTurn` 同一个口），
按正文动词分三档：

| # | id | 卡 | at-turn 正文 | op 动词 | ability / `ta` | 本批 |
|---|---|---|---|---|---|---|
| 1 | **AM18** | Master of Ordnance | `deal 3 damage to a random enemy for each friendly Vehicle in play` | `deal` | 1 条 / `[10]` | ✅ **贴上** |
| 2 | **AM67** | Aquilon Servo-Sentry | `deal 1 damage to a random enemy` | `deal` | 1 条 / `[10]` | ✅ **贴上** |
| 3 | **TAU6** | Gun Drone | `deal 2 damage to a random enemy` | `deal` | 1 条 / `[10]` | ✅ **贴上** |
| 4 | **TAU7** | Marker Drone | `give Markerlight 1 to a random enemy` | `give` | 1 条 / `[10]` | ✅ **贴上** |
| 5 | **TAU13** | DS8 Support Turret | `deal 2 damage to all enemies` | `deal` | 1 条 / `[10]` | ✅ **贴上** |
| 6 | **`a7bb23…`** | Poisoned Supplies（**手牌陷阱**） | `your troops take 1 damage` | `deal` | 2 条 ability，`trigger 40` **唯一** / `[10]` | ✅ **贴上**（走 channel ①） |
| 7 | GOF82 | Beast Snagga Nob | `give +1 Attack to all Beasts in your hand` | `give` | 1 条 / `[50]` | ⛔ **判据不足**（hand 池） |
| 8 | TAU10 | Stealth Drone | `give Stealth to an adjacent troop` | `give` | 1 条 / `[100]` | ⛔ **判据不足**（`ta=100`） |
| 9 | TL37 | Spore Mine | `destroy this unit and deal 3 damage to a random enemy` | `destroy` + `deal` | 1 条 / `[10,20]` | ⛔ **判据不足**（**行序反**） |
| 10 | GOF90 | Wurrboy | `deal 1-2 damage to a random enemy for each friendly troop in play` | `deal` | **2 条 ability**（at-turn 是 `ab=1`）/ `[10,10]` | ⛔ **判据不足**（条数/下标） |
| 11 | GSC5 | Concealed Explosives | `Takes 1 damage` | `deal`（**无主语**） | 2 条 ability / `[20]` | ⛔ **判据不足**（self） |
| — | **GOF86** | Unleashed Tramplasquig | `attack a random non Flying enemy` | `forceattack` | 1 条 / `[10]` | ✅ 已由 `A2` 做 |

另外 **10 张** at-turn 卡的正文不是 `deal`/`give`（`create` / `deploy` / `reanimate` / `return` / `gain` /
`Adjacent Remnants do not disappear`）：`BL67` · `EC17` · `SAU20` · `SAU36` · `GOF80` · `SOR66` · `SOR70` ·
`TL6` · `TL8` · `Cult Propaganda`（陷阱，动词 `create`）。它们的 `ta` 是 `0 (any)` / `20 (self)` / 没有单位候选
⇒ **贴上去也是零筛**（`TargetCriteriaEval.Matches` 对 `ta=0` 直接 `true`），`A3` **不碰**。

### 2 · 判据（为什么 at-turn 也要贴 criteria）

- **原版回合起止这一拍走的是同一条 ability 链**：
  `CardScript__OnTurnStart.c:180-190`（`RawCardScript.OnTrigger(0x1e = 30 / 0x1f = 31 …)`）·
  `CardScript__OnTurnEnd.c`（`0x28 = 40 / 0x29 = 41`）→
  `RawCardScript__TriggerAbilities.c:200-250`：遍历 `rawCard.cardAbilities`（`+0x290`）→
  `CardAbility__CanTriggerAbility(ability, trigger, …)` 过闸 → 取**这条 ability** 的 `abilityLogic` →
  `AbilityLogic__GetTargets.criteria`（`+0x10` 分发）→ 建池 → **`:1086-1095` 逐候选
  `FilterMethods__CheckIfMeetsCriteria`**。
  ⇒ **回合起止与打出族用的是同一个 `AbilityLogic.targetCriteria`**，没有「at-turn 另有口径」这回事。
- **我们为什么之前没跑到**：`ResolveAtTurn` 的棋盘单位那条（`EffectResolver.cs:7782`）与手牌陷阱那条
  （`:7734`）都是 `EffectText.Parse(正文)`（**不带 card**）⇒ `AssignAbilityTargets` 那条贴片路根本没进。
  `A2` 只给 `forceattack` 补了一个 helper；`deal`/`give` 仍然是空的。
- **消费端不需要改**：`ResolveTargets` 已经对**任何**带 `op.TargetCriteriaSet` 的 op 调
  `ApplyTargetCriteriaFilter`（`EffectResolver.cs:1551`）；at-turn 的 `deal`/`give` 都走 `ResolveTargets`
  （`DoDeal` / `DoGive`）。⇒ 缺的只是「贴」这一步。

### 3 · 实现（同一套窄闸，四条，每条都对应一个「说不清」）

`Core/EffectText.cs` 新增：

- `AttachAtTurnBodyCriteria(card, ops, phase)` —— **总入口**（棋盘 / 手牌陷阱两条路都只调它）。
- `AttachAtTurnDealGiveCriteria(card, ops, phase)` —— 窄闸：
  ① `ops.Count == 1` 且动词是 `deal`/`give`；
  ② `op.Target != null` 且**不是** `Subjectless` / `HandOnly`（那两族的目标不是 `ResolveTargets` 的棋盘池）；
  ③ 卡表 v17+，且相位对应的 ability **唯一**（`turn_start` ⇒ 30/31 · `turn_end` ⇒ 40/41 ——
     判据 `CardAbility__CanTriggerAbility.c:71` 的 `ability.trigger == trigger`）；
  ④ 那条 ability 的 `targetsAffected` **恰好 1 格且 = 10 (`allFiltered`)** —— 只有这一档的语义是
     「在 `ResolveTargets` 建好的棋盘候选池上逐候选筛」。其余档是**别的建池器**（`20 self` 身份比较 /
     `50 inHandFiltered` / `100 adjacentToSelfIfMeetsCriteria`），本版没接。
- `UniqueAtTurnAbilityIndex(abil, phase)` —— 相位 → **唯一**那条 ability 的下标；同相位落两条 ⇒ `-1`（⛔ 不猜）。
- 调用点两处：`EffectResolver.cs:7788`（棋盘那趟，原来调 `AttachAtTurnForceAttackCriteria`，改成总入口）·
  `:7734` 之后（**手牌陷阱**那趟，本批新增）。

### 4 · 🔴 今天的行为面（如实标着 —— 这就是 `play check` 零差异的原因）

命中的 5 张棋盘卡的 **36 格内容全等于「随机一个敌人」这件事**：
`enemyUnits=1 + includeHero/includeMinions/includeRemnants=1 + maxTargets=1`（`ta=10`）。
其中 3 张（`AM67` / `TAU6` / `TAU7`）另有 `excludeCondition = 220`，而 **220 的枚举名是 `dealtKillDmg`、
语义是「将死」**（`ConditionEval.cs:308-316` → `RuleCore.EnoughPendingDamageToDie` = `Health<=0 && Survivor<=0`）。

⇒ **我们从 `AddSide`（`EffectResolver.cs:2513-2525`）起就不收 `!IsAlive` 的候选**，
所以「排除将死的敌人」在我们的池里**恒不成立**（那个人早被 `AddSide` 挡掉了）。
⇒ 这 5 张今天 **零可观测差异**。本笔修的是**结构缺口**（数据不流到消费端），
将来若按 `A1426`（原版目标解析层**不按生死筛**）放开 `AddSide`，这一格才会变承重件。

**唯一真能改行为的是手牌陷阱 `Poisoned Supplies`**：它的 criteria 是
`friendlyUnits=1 + includeMinions=1 + includeRemnants=0` —— 最后那格**文本层没有**
（「your troops」不区分残骸）⇒ 持有者的**残骸不再挨这 1 点**。
`play check` 看不到它（`play` 只走「打出去那一拍」，手牌陷阱的结算是回合末），所以我在**离线探针**里
真跑了一拍：持有者部队 −1、对方部队 −0（见 §五 的 `③`）。

### 5 · 判据不足的 5 张 —— 「还差什么」（⛔ 不许贴）

| 卡 | 卡在哪 | 还差什么 |
|---|---|---|
| `GOF82` | `ta = 50 (inHandFiltered)`：手牌池**不是** `ResolveTargets` 的棋盘池（`spec.HandOnly` 直接早退），真正走的是 `DoGive` 的手牌 buff 支；原版那条 criteria 属于 **`HandEffect.targetCriteria`**（另一个对象） | 要让 `DoGiveHandBuff` 那支**按 criteria 筛手牌**（今天我们只有 `spec.SubtypeFilter = Beast`，已覆盖「Beasts」这条文本） |
| `TAU10` | `ta = 100 (adjacentToSelfIfMeetsCriteria)`：`TargetCriteriaEval` 对它**明确返回 `null`**（`TargetCriteriaEval.cs:138-140`，`:407-425` 的「相邻里有满足者」本版未实现） | 先实现 `ta=100` 那一档；否则贴上去只会在每次结算时**出一次声、零筛**。它的实质内容是 `excludeTrait = 70 (stealth)` = 「别给已经带 Stealth 的」 |
| `TL37` | 正文 **2 条 op**（`destroy this unit` → `deal 3`），criteria **2 行**：`lg0 = ta10 enemy` / `lg1 = ta20 self` —— **文字序与行序相反** | 要有「op ↔ logic」的**非位置**对齐判据（按 `ta` 语义配对或读 `cardAnimations` 序）。今天任何按位置贴的写法都会把「摧毁自己」贴上敌人池 |
| `GOF90` | **2 条 ability**（`Unstable(81)` + `TurnEnd(40)`），at-turn 是 `ab=1`，且 `ta = [10,10]` **2 格** vs 正文 **1 条 op** | 同 `TL37`：条数不等就没有对齐判据；另需「相位落两条 trigger 时怎么选」的判据（这里 40 唯一，能选；但 `ta` 两格说不清） |
| `GSC5` | 正文是 `Takes 1 damage`（**无主语**，`ta` = `[20] self`）——`ResolveTargets` 在 `Subjectless` 那支**早退**，根本不建池、走不到 `ApplyTargetCriteriaFilter` | 这一档本来就不该按「候选池筛」实现（原版 `ta=20` 是身份比较 `candidate == thisCard`）；要接得在 `Subjectless` 那支里判，是另一条账 |

### 6 · 断言（`Editor/RuleEngineTest_EngineA3.cs`，4 组 · 静态 58 处调用 · 动态约 100 条）

- **① 每张贴子都覆盖到**：5 张真卡 + 陷阱，逐张
  「生产路径解析 ⇒ **不跑 helper 时 `TargetCriteriaSet == null`**（灭自证判别式）⇒ 跑 helper ⇒
   `ta=10` / `(ab,lg)=(0,0)` / cond-ex 行；（有 220 的那 3 张）`Condition=220` ∧ `Exclude`」。
- **② 行为探针（用挂上去的那个对象，不是手搓 set）**：`TargetCriteriaEval.Matches`
  —— 敌方候选 `true` / 己方候选 **`false`** / 把敌方 `Health` 打到 0 ⇒ **`false`**（220 那一支）。
  🔴 **灭自证**：不贴时 `set == null` ⇒ `Matches` **恒 `true`** ⇒ 那几条「false」必红。
- **③ 端到端真广播**：`Poisoned Supplies` 真跑一拍（持有者 −1 / 对方 −0）；`TAU6` 真跑一拍（只打 2 点、只落一个）。
- **④ 窄闸负例**（每张「故意不贴」都钉住，附前提断言，防止「什么都没验到」变假绿）：
  相位不符（合成卡，正反各一条）· `GOF82`(50) · `TAU10`(100) · `TL37`(2 op) · `GSC5`(Subjectless) · `GOF90`(2 ability/2 格)。

---

## A3-2 · ability 级 criteria 的 36 格：**在哪一层 / 缺哪一步 / 影响多少**

### 1 · 结论一：它**不在** `abilityTargetCriteria` 那一列 —— 那是**另一层**的同一个字段名

原版有 **两个**都叫 `targetCriteria` 的 `TargetCriteria` 对象（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`）：

| 对象 | 字段位置 | 谁判它 | 语义 | 我们落了吗 |
|---|---|---|---|---|
| `AbilityLogic.targetCriteria` | `AbilityLogic.cs:16-17`（`+0x28`） | `AbilityLogic__GetTargets.c:1086-1095`（**逐候选筛**） | **目标池** | ✅ v25：`abilityTargetCriteria`（普通）/ `activeAbilityTargets`（主动），**只收 `lg >= 0`** |
| **`CardAbility.targetCriteria`** | `CardAbility.cs:16`（**`+0x30`**） | **`CardAbility__CanTriggerAbility.c:131-135`**（**触发前**的闸） | **触发条件**（不是目标池） | ❌ **只有 `cond`/`ex` 那一半**（`unitConditions` 的 `role="target"` + `lg = -1`，**27 行 / 23 张**）；**另外 36 格没有任何一列承载** |

**判据（逐句）**：`CanTriggerAbility` 三个闸依次是

```
FilterMethods__CheckIfMeetsCriteria(thisCard, actingCard, targetCard, *(ability+0x28), 0)   // actingCardCriteria
FilterMethods__CheckIfMeetsCriteria(thisCard, targetCard, targetCard, *(ability+0x30), 0)   // ★ targetCriteria
BoardAnalysis__MeetsCriteria(bm, *(ability+0x38), …)                                        // boardCriteria
```
> ⚠️ **2026-10-25 订正（执行代理 `E3` · 铁律 5）**：第二行原来照抄成 `(thisCard, actingCard, targetCard, *(ability+0x30), 0)`
> —— **被筛对象抄错了**。判据（第一权威 = 全量反编译，同一函数 `CardAbility__CanTriggerAbility.c:127-135`）：
> **第一道闸（`+0x28`）** 的候选 = `param_5 = actingCard`；**第二道闸（`+0x30`）** 的候选 = `param_6 = targetCard`
> （形参表 `d:/2/tools/il2cpp_out/dump.cs:20360`）⇒ 二行分别是 `actingCardCriteria` / `targetCriteria` 两块**不同的数据**。
> 订正前后**不影响**下面那条结论（两列都还没落 / 已由 v26·v27 分两列补齐），但照原句实现会把两个候选搞反。
> 详见 `资料/普查产出_1025/E2_ability_criteria管线.md` §一 与 `E3_actingCardCriteria.md` §三。
而 `CheckIfMeetsCriteria(param_1 thisCard, param_2 **被筛的那个**, param_3 targetCard, param_4 criteria)`
（`FilterMethods__CheckIfMeetsCriteria.c:1-58`：`iVar17 = *(int*)(param_4 + 0x10)` = `targetsAffected`，
`== 0 (any)` ⇒ **直接 `return 1`**）⇒ **ability 级 criteria 各自判「自己那个候选满不满足」，一个候选、不建池**：
第一道闸看 **`actingCard`**、第二道闸看 **`targetCard`**（⚠️ 上面那条 2026-10-25 订正）。
偏移自证：`multiCountCriteria` 在 `+0x40`（`RawCardScript__TriggerAbilities.c:230-250` +
`gen_cards_engine.py` 的 `CONDITION_ROLES` 注释），字段声明序 `actingCardCriteria / targetCriteria /
boardCriteria / multiCountCriteria` ⇒ `+0x28 / +0x30 / +0x38 / +0x40` 逐个对得上。

⇒ **A3-2 那条账的措辞要订正一格**：不是「36 格在 `abilityTargetCriteria` 那一列没落」，
而是「**它在 `abilityTargetCriteria` 那一列里根本不该出现**（那一列是 `lg >= 0` 的 logic 级），
真正的载体是 `CardAbility.targetCriteria`，而它只落了 cond/ex」。

### 2 · 结论二：数据**已经在** `cards_truth.json` 里（不是「本地没有」）

`Resources/cards_truth.json` 的 `cards[].abilities[]` 直接带
`targetCriteria` / `actingCardCriteria` / `boardCriteria` / `multiCountCriteria` 四列（现读 key 计数：
`targetCriteria` **106 条** · `actingCardCriteria` **132** · `boardCriteria` **130** · `multiCountCriteria` **18**）。
⇒ 缺的**不是**「拿不到」，是「没搬」。

### 3 · 结论三：缺的是 **4 步**，一步都不能省

| 步 | 现状 | 要补什么 |
|---|---|---|
| **① 生成器** `工具/gen_cards_engine.py` | 只读 `abilities[i].abilityLogic[j].targetCriteria`（`:1897-1930` `target_criteria_rows`）；`cardAbilities[i].targetCriteria` **一次都没读** | 照 `countSet`（`:2185-2200` `COUNT_SET_*` / `criteria_set_of`）那套，给 `role="target"` + `lg=-1` 的行加一个 `criteria` 对象（36 格逐格落，`filterSpecificCards` 走 `truth_by_path` 解 id） |
| **② DTO** `CardDatabase.CardDto` | 有 `unitConditions` / `abilityTargetCriteria` / `activeAbilityTargets`，**没有** ability 级 criteria | 复用 `TargetCriteriaRowDto`（`lg = -1`），或单开 `abilityLevelCriteria` |
| **③ `CardDef`** | `AbilityTargetCriteriaAt(ab, lg)` 只存 `lg >= 0`（`CardDef.cs:2310` 的 `_abilityTargetCriteria`）；注释白纸黑字写着「⛔ 不许合进任何 logic 行」 | 新字典 `_abilityLevelCriteria`（键 = `ab`）+ 访问器；**⛔ 不要塞进现有字典**（那会让 `lg=-1` 与 `lg=0` 撞键） |
| **④ 消费** | **没有** ability 级触发闸；最接近的位置 = `ActionPump.EnqueueAbility`（`abilityContext` / `IsRightContext` 已经在那儿判，`ActionPump.cs:189-246`） | 在入队那一跳加：`TargetCriteriaEval.Matches(ctx, owner, set, candidate: actingCard, cond, thisCard, targetCard)` —— 现有 `Matches` 的签名正好是「一个候选」，`candidate` 传 **`actingCard`**（判据见上表）；`ta=0` 它自己就 `true`，与 `CheckIfMeetsCriteria` 同口径 |

### 4 · 结论四：影响面（现读 `cards_truth.json` ∩ `cards_engine.json` 的 1127 张池）

- 带 ability 级 `targetCriteria` 的：**106 条 ability / 85 张卡**；
  其中 36 格**非平凡**（除 cond/ex 外有非零值）：**98 条 / 87 张**（剩下 13 条的 `ta` 键都没有 ⇒
  `targetsAffected = 0 (any)` ⇒ `CheckIfMeetsCriteria` 直接 `return 1` ⇒ **零信息**）。
- `ta` 分布（ability 级，池内）：`10 allFiltered` **62** · `30 target` 8 · `90` 7 · `20 self` 7 ·
  `55 inPlayOrDyingFiltered` 1 · **无键（= 0 any）13**。
- 已经有一行的（cond/ex 非 0）：**26 条 / 22 张**（27 行含一条重复 key 外的边角）⇒ 这 26 条**只需加 36 格**；
  另 **72 条 / 65 张**连行都没有（`conditionsFilter = 0`）⇒ 要么扩 `CONDITION_ROLES_OPTIONAL_COND`
  （把 `target` 也放进去，会让 `AssignAbilityIndices` 的「缺行 = 没有条件」前提松一格），
  **要么单开一列**（推荐后者 —— 不碰既有归位闸）。
- 内容长什么样（避免「只是许可证」的误判）：`friendlyUnits=1 + includeMinions=1`（**acting 是友方小兵**）·
  `enemyUnits=1 + conditionsFilter=220/ex=1`（**acting 不是将死**）· `traitsFilter=20 条` / `excludeTrait=6 条`。
  ⇒ 这是**真闸**，不是空转。

### 5 · 🔴 为什么本批**不落地**（两条硬理由）

1. **会改卡池指纹**：新增/改任何一列都改 `cards_engine.json` 的字节 ⇒ `ruleprobe check` 的**哨兵 ⑤
   （池 md5 与钉住的一致）** 立刻红；要重钉就得改 `工具/ruleprobe/**`（`Program.PinnedPoolMd5`），
   而那是**本批黑名单**（也只允许主对话动）。
2. **②③④ 是一串依赖**：只落数据、不写消费点 = **零可观测变化**（今天的 `play` / `check` 都不会动），
   等于把「一条看不见的数据」放进卡表；而写消费点要先定「`actingCard` 与 `(ab,lg)` 的对应」——
   `CanTriggerAbility` 是**逐 ability** 判的，而我们的 `ActionPump.EnqueueAbility` 拿到的常常只有「关键词」，
   **哪条 `(ab)` 现在不一定说得清**（`AbilityContextOfAbilityV14` 那条链已经在为此兜底）⇒
   接之前要先补一条「关键词 → `cardAbilities[i]` 下标」的判据，否则会重演 `SW9` 那种「贴到错的层」。

**建议的施工顺序**（一件一个代理，互不撞文件）：① 生成器 + DTO/`CardDef`（落数据，不接消费，**重钉一次哨兵**）
→ ② `ActionPump` 的触发闸（先只对**能唯一定位 `ab`** 的路径生效，判不了就出声 + 不筛）
→ ③ 逐卡对拍（这一层改的是**触发**不是目标，`play` 基线会动，要逐张看）。

### 6 · 顺手发现（比 A3-2 原先那条更大的一处）

`unitConditions` 的 10 个 `role` 里，**只有 5 个族落了 36 格**（现读行数）：

| role | 行数 | 36 格 |
|---|---|---|
| `mult`（`AbilityLogic.multiplier`） | 31 | ✅ `multiplierSet` |
| `multi`（`CardAbility.multiCountCriteria`） | 9 | ✅ `multiSet` |
| `avail`（`targetsAvailableCounter`） | 17 | ✅ `availSet` |
| `choice`（`choicesDefinition`） | 84 | ✅ `choiceSet` |
| `count`（`boardCriteria.countCriteria`） | 6 | ✅ `countSet` |
| **`target`** | 162（其中 **`lg=-1` 27 行**） | ❌ 无（`lg>=0` 那 135 行由 `abilityTargetCriteria` 承载） |
| **`acting`**（`actingCardCriteria`） | 1 | ❌ 无 |
| `cardtarget` / `nontarget` / `targetscore`（卡级 AI 评分） | 13 / 20 / 6 | ❌ 无 |

⇒ **`actingCardCriteria` 才是更大的那个洞**：池内 **93 条 ability / 86 张卡** 都有它、
**93 条全部 36 格非平凡**，而只有 **1 条** 落了行（cond/ex 非 0 才落）。
它同属 `CanTriggerAbility` 的第一道闸（`+0x28`，判 `actingCard`），与 A3-2 那条是**同一个坑的两半**。
卡级的三族（`cardtarget` / `nontarget` / `targetscore`，`cards_truth` 里 310 + 267 个对象）是 **AI 评分**用的，
不在战斗规则里，优先级低一档（但同样「与原版不符」⇒ 铁律 11 要记账）。

---

## A3-3 · `CardDef` 两列共用一个版本位

### 1 · 判定：**不是原版语义**

`AbilityTargetsVersioned = abilityTriggersVersioned`（`CardDef.cs:963`，改动前）里的两个「versioned」
是**我们数据管线**的正面信号（「这一列来自 v17 卡表」）。原版那边只有序列化字段
（`cardAbilities[].trigger` 与 `abilityLogic[].targetCriteria.targetsAffected`），**没有「版本」这个概念**。
⇒ 这是**我们这边的便利默认**（v17 一次加了两列，就共用了一个位），**不是原版语义**
⇒ 按 `A3-3` 的处置：**不改语义、加出声**。

### 2 · 病灶（`A2` 2026-10-25 踩过，写进它的「顺手发现 ②」）

合成夹具只给 `abilityTargets:` 而忘了 `abilityTriggersVersioned: true` ⇒ `AbilityTargetsVersioned` **静默 false**
⇒ `AssignAbilityTargets` / `AttachAtTurn*Criteria` 开头的 `if (!card.AbilityTargetsVersioned) return;` **整条跳过**：
夹具「看着给了数据」，引擎当「旧表整列没落」，**没有任何输出**。

### 3 · 改动

`Core/CardDef.cs`（ctor，`AbilityTargetsVersioned = …` 之后）：**两个方向各一条 `Debug.LogWarning`**

- 给了非空 `abilityTargets` 却没有版本位 ⇒ 说明「这列会被当旧表整列跳过」，并给出修法；
- 版本位是 true、`abilityTriggers` 非空、而 `abilityTargets` **整列空** ⇒ 说明是**数据缺列**（不是「没有 ability」）。

配套：`public static int AbilityVersionFlagWarningsForTest { get; private set; }`（**只给自检当判别式**；
`Debug.LogWarning` 的输出流在探针/自检里抓不到）+ `public int AbilityTriggerCount`（只给自检/诊断）。
⚠️ 逐次构造喊一次（**没有全局去重**）；现读 v25 卡池 **0 张不一致**（见下断言）⇒ 正式装载**零噪声**。

### 4 · 断言

- 合成卡 A（只给 `abilityTargets`）⇒ `AbilityTargetsVersioned == false` ∧ **计数 +1** ∧ 同一句正文的 criteria **贴不上**；
- 合成卡 B（补上版本位）⇒ `== true` ∧ **计数不变** ∧ criteria **贴得上**（A/B 互为灭自证，唯一变量 = 那一位）；
- 反方向（`abilityTriggers` 有、`abilityTargets` 空）⇒ **计数 +1**；
- 真卡池：`AbilityTriggerCount > 0` 的卡与 `AbilityTargets.Count > 0` 的卡**逐张同进同出**（现读 **0 张不一致**）
  ⇒ 那两条 warning 在正式装载时不会刷屏。

---

## 改动清单

| 文件 | 改了什么 |
|---|---|
| `MyGame/Assets/RuleEngine/Core/EffectText.cs` | 新增 `AttachAtTurnBodyCriteria`（总入口）· `AttachAtTurnDealGiveCriteria`（`deal`/`give` 窄闸四条）· `UniqueAtTurnAbilityIndex`（相位→唯一 ability） |
| `MyGame/Assets/RuleEngine/Core/EffectResolver.cs` | 棋盘 at-turn 那趟改调总入口（`:7788` 附近）· **手牌陷阱**那趟新增贴片调用（`:7734` 之后） |
| `MyGame/Assets/RuleEngine/Core/CardDef.cs` | ctor 两列版本位不一致时**出声**（两个方向）· `AbilityVersionFlagWarningsForTest` · `AbilityTriggerCount` |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineA3.cs` | **新增**（`TestA3EngineAtTurnCriteria`：4 组 · 静态 58 处断言调用，动态约 100 条） |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | `Run()` 加一行 `Step(TestA3EngineAtTurnCriteria)`（+3 行注释） |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_Diag.cs` | 用 `工具/gen_diag_runner.py` **重生成**（333 条用例，`--check` 逐字节一致） |

**没改**：`工具/gen_cards_engine.py` · 任何 `cards_engine.json` / 卡池 / 基线 · `工具/ruleprobe/**`
（`Pinned*Md5` 一个字没动）· 表现层 · git · 三份正本。
**临时产物已清**：`d:/4/_tmp_view/a3probe/`（离线复核探针，`git status` 里没有它）。

## 行尾核对（改完逐个现数）

```
Core/EffectText.cs          CRLF 0      LF 10579   （LF，未翻）
Core/CardDef.cs             CRLF 0      LF 7294    （LF，未翻）
Core/EffectResolver.cs      CRLF 13931  LF 13931   （CRLF，未翻 —— LF 计数 == CRLF 计数）
Editor/RuleEngineTest.cs    CRLF 37025  LF 37025   （CRLF，未翻）
Editor/RuleEngineTest_EngineA3.cs  CRLF 0  LF 502   （新文件，照 `_EngineA2` 用 LF）
```

## 没查清 / 还差什么（如实标着）

1. **`play check` 零差异 ≠ 「验过了」**：它只看得见「打出去那一拍」，**看不见回合起止**（at-turn 全在它盲区）
   ⇒ 本批的行为证据来自**离线探针**（109 条真跑）+ 主对话将来那次 `RuleEngineTest.Run`。
2. **`GOF90` 的「相位落两条 trigger 时怎么选」**：本轮靠「40/41 唯一」绕过去了，但它 `ta` 两格 vs 1 条 op
   **仍然没解**（同 `TL37`）。要把 at-turn 全覆盖，得先有「op ↔ logic 非位置对齐」的判据。
3. **`ta=50 / 100 / 105 / 106 / 110 / 113 / 115` 六档建池器本版都没有**
   （`TargetCriteriaEval` 只覆盖 `ta=0/10/20/30/40/70/80/90/140/150` 与「原版不筛」那批）。
   ⇒ 这几档的 at-turn 卡（`GOF82` / `TAU10`）在**贴片之前**就得先补建池。
4. **`actingCardCriteria` 的 36 格（93 条 / 86 张）完全没落**（见 §A3-2·6）—— 账要立，但不在本批。
5. **`CardDef` 那条 warning 没有全局去重**：夹具里若循环造很多缺位卡会刷屏。生产池现读 0 命中，本轮没做去重。
6. **`GOF97`** 有一条 ability 的 `targetsAffected` 是**空数组** `[]`（`abilityTargets[0].ta = []`）——
   现有闸（`ta.Length != 1`）会跳过它，但它本身也说明「`ta` 条数 = logic 条数」在这张卡上不成立，建议回数据侧核一眼。

## 顺手发现（⛔ 只报不改，供调度台分流）

1. **`unitConditions` 的 36 格覆盖是「按 role 分族」的不是全局缺**（`mult/multi/avail/choice/count` 已落，
   `target(lg=-1)/acting/cardtarget/nontarget/targetscore` 未落）—— 见 §A3-2·6。这条比 A3-2 原账更大。
2. **`TargetCriteriaEval` 对 `ta=100` 是「明确判不了」而不是「没实现」**：它返回 `null` ⇒
   调用方整条不筛 + 出声。⇒ 将来实现 `ta=100` 时，**要把那条 `null` 换成真判据**（`CheckIfMeetsCriteria.c:407-425`），
   ⛔ 别只把 `null` 删掉。
3. **`RawCardScript__TriggerAbilities.c:230-250` 的 `GetTargets` 只取 `ability+0x40` 的**第一条/整表**
   这一点我没读到逐句结论（`0x40` = `multiCountCriteria`？还是 `abilityLogic` 列表？两说）。
   本轮**不需要**它（我的窄闸只认「`ta` 恰好 1 格」），但 `A3-2` 落地②③④时要先把这一跳读死。
