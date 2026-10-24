# E1 · `chosenAbility` 列 + 按能力种类归位 + `ta` 来源错修复（2026-10-24）

> 执行代理 `E1`（有写权限）· 上游：`资料/普查产出_1024/D11_activeAbility列.md`（v23 数据侧）、
> `资料/普查产出_1024/G12_activeAbility消费.md`（v23 消费侧 · 只接 38/87）。
> 判据第一权威 = 全量反编译 `d:/2/tools/decomp_full/`；第二 = 枚举表
> `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{AbilityEffect,PlayersAffected}.cs`；
> 第三 = 卡图（本件未用到）。⛔ 粉丝规则书不是判据。
> 类型检查：`TMPDIR=/tmp/wf_e1 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（末次）。
> ⛔ 没跑 Unity 批处理 / 没跑 ruleprobe / 没动 git / 没改正本。

---

## 〇 · 一句话结论

1. **`activeAbilityLogic` 每行新增三格 `ca`（`chosenAbility`）/ `pa`（`playersAffected`）/ `dmg`（`damageCriteria`）**，
   卡表 `version 23 → 24`；生成器 `--check` 一致。
2. **按「能力种类」归位**（`CardDef.AttachByAbilityKind`）：`gate=1` 的 **87 张里挂上 84 张**（v23 只挂 38 张）；
   剩 3 张如实留 `-1`（各有明确原因，见 §四）。**58 个 op** 的 `ta`/`fm` 发生变化（全表见 §五）。
3. 🔴 **14 个替代行动 op 上「`cardAbilities` 的 `ta`」已从源头修掉** —— 判据点就是
   `EffectText.AssignAbilityTargets` 的**片段分支**（新增 `alternativeActionBody` 闸），
   不再靠下游覆盖。`SOR66`（唯一「有 active 行 ∧ 无替代关键词」）也挂上了（**唯一一段正文**那条路）。

---

## 一 · 数据侧（生成器 + 卡表）

| 位置 | 内容 |
|---|---|
| `工具/gen_cards_engine.py:1688-1780` | `active_ability_logic_of()` 每行多落 `ca` / `pa` / `dmg`（缺键/坏值逐格 `-1`），读数 `:1684`、`:5519` |
| `工具/gen_cards_engine.py:4353` | `"version": 24`（v24 注释在 `:4520` 一带） |
| `MyGame/.../Resources/cards_engine.json` | 1,453,225 B → **1,463,954 B**；**只有 `activeAbilityLogic` 一个字段变**（逐卡逐字段程序化比对：132 张，其它字段 0 处变化；卡 id 集合 0 增 0 减） |

**列形状**（仍是一个能力对象一行，逐格对齐 `ta`/`fm`）：

```json
{"ab":-1,"gate":1,"trigger":400,"needsLoading":0,
 "ta":[30,20],"fm":[0,1],"ca":[10,462],"pa":[0,0],
 "dmg":[{"min":2,"max":2,"opt":0,"type":0},{"min":1,"max":1,"opt":0,"type":0}]}
```

| 格 | 语义 / 坏值契约 | 判据 |
|---|---|---|
| `ca[j]` | `activeAbilityLogic[j].chosenAbility`（`AbilityEffect` 枚举）；缺键/坏值 `-1` | `AbilityLogic.cs:9`；`AbilityLogic__PlayAbility.c` 首层 `switch (*(int*)(param_1+0x10))`（`:286`），`case 0x1ce(462)` → `AddTriggerPray`（`:1838-1855`） |
| `pa[j]` | `playersAffected`（`Both(0)/Self(10)/Enemy(20)`）；`-1` = 未知 | `PlayersAffected.cs` |
| `dmg[j]` | `damageCriteria` 原值 `{min,max,opt,type}`；缺对象/坏格逐格 `-1` | `DamageCriteria`（`AbilityLogic__PlayAbility.c:708` 伤害族用它） |

### 1·1 `damageCriteria` / `playersAffected` 要不要加 —— **结论：都加**（本件已加）

| 列 | 结论 | 理由（数据现读） |
|---|---|---|
| `chosenAbility` | **必加** | 没有它就没有「行 ↔ op」的判据（G12 §三·4）。加了之后 84/87 挂上（+46 张） |
| `damageCriteria` | **加**（但**只当旁证**） | ① 是伤害**数值**的唯一真值（正文可能含糊）；② 能在「同族多行」时做二次判别（`AM35` 两行 `ta` 相同、伤害 8/12 不同）；⚠️ **非伤害行的 `(1,1,0,0)` 是 SO 序列化默认值**（`ca=10` 的 70 行里 21 行是 `(1,1)`）⇒ ⛔ 不能拿它反推非伤害行的数值，本件的配 op **不读它** |
| `playersAffected` | **加**（`31` 格非 0） | 是「哪一方玩家受影响」，与 `ta` **正交**；⚠️ **不能**当「玩家行 ⇒ ta 无意义」的闸 —— 实测那样会**错杀 9 张**（`AM59` 的 `draw a Stratagem` `ta=65`、`SOR37` 的 `deal … equal to your Faith` `ta=30` 都是真目标集，只是受影响的是己方） |

---

## 二 · 消费侧：按能力种类配 op（`CardDef.ApplyActiveAbilityLogic`）

| 位置 | 内容 |
|---|---|
| `Core/CardDef.cs:2538` | `AttachByAbilityKind` —— 逐族把行配到 op |
| `Core/CardDef.cs:2606` | `ActiveAbilityKindFamily(ca)`：`AbilityEffect` → 族名（12 族） |
| `Core/CardDef.cs:2645` | `ActiveAbilityFamilyHasVerb(family, verb)`：族 → `EffectOp.Verb` 集合（**族之间不许交叉**） |
| `Core/CardDef.cs:2590` | `IsDerivedTargetSet(ta)`：`ta ∈ {100,105,106,110,113,115}`（`AdjacentAnchor` 六档）**不参与** |
| `Core/CardDef.cs:2487/2502` | `RowsHaveChosenAbility` / `AttachSingleActiveRow`（v23 那一档的旧口径保留） |
| `Core/CardDef.cs:2362` | `IsActiveActionKeyword(key)`（`Duty/Pray/Ferocity/Agenda/Oath` 的唯一名单） |
| `Core/CardDef.cs:2424-2460` | 关键词闸：替代行动关键词**恰好一个**；**没有时**退到「整卡只有一段正文」（`SOR66`） |

**为什么按种类可行**（判据）：原版 `PlayAbility` 的第一层 `switch` 就是按 `chosenAbility` 分派
（`AbilityLogic__PlayAbility.c:286`，`case 10` 伤害族 `:1461`、`case 0x1ce(462)` `AddTriggerPray` `:1838`、
`case 0x1e6(486)…` 另一批 `:2641+`）⇒ **同一种能力** ↔ **同一族动词** 是唯一对应，不需要「第 j 行 = 第 j 条 op」。

**闸门（任一不过 ⇒ 不挂 + 记 `ActiveAbilityMap`）**：① 只 `gate=1`；② 两个能力对象都存在时逐格一致；
③ 族内**所有行的 `(ta,fm)` 逐格相同**（否则 `gap:kind-ambiguous`，⛔ 不挑第一行）；
④ 该族的 op 至少一条（且**不被别的族先认领** `ActiveAbilityRow < 0`）。
`ta ∈ {100…115}` 的**相邻派生行不参与**（相邻已在 op 自己的 `Target.Adjacent/Anchor` 里）。

### 2·1 结果（`gate=1` 的 87 张）

| 结论 | 张数 | 说明 |
|---|---|---|
| `attached` | **84** | v23 的 38 ＋ 本件按种类挂上 46（含 `SOR66`） |
| `gap:kind-no-op` | 1 | `AM22`：1 条 `chooseone` op 里盖着「伤害 / 增益」两个分支（一条 op 两个目标集 ⇒ 贴哪个都错） |
| `gap:kind-ambiguous` | 1 | `SOR13`：同族两行 `ta=10`/`120`，正文也是两条 `give`（`your troops` / `your Warlord`）⇒ 种类分不开 |
| `gap:kind-unknown` | 1 | `SW9`：`ca=320 followAfterResolution`（语义是「跟着上一条结算」，**故意不映射**） |
| （另）`gate-0` | 45 | 本 build 死数据（不变） |

---

## 三 · 🔴 `ta` 来源错：14 个替代行动 op（从源头修）

**改点**：`Core/EffectText.cs:1981` —— `AssignAbilityTargets` 的**片段分支**加闸
`if (alternativeActionBody) return;`（签名与传参：`EffectText.cs:1377` / `:1580` / `:1939`；
`CardDef.cs:4200` 在 `AddTriggerOp` 里按 `IsActiveActionKeyword(key)` 传）。

**为什么是「来源错」**：片段分支原来假定「整卡只有一条 `cardAbilities` ∧ 片段 op 数 == 它的 `ta` 长度
⇒ 这段正文就是它的」。而 `Duty:`/`Pray:`/`Ferocity:`/`Agenda:` 的效果在
`activeAbility/alternativeAbility.activeAbilityLogic` 里（另一条链）⇒ 前提不成立。

**受影响逐条（改前 → 改后）**：

| 卡 | op | `ta` 原 → 新 |
|---|---|---|
| `AM28 Chimera` | deploy | 10 → **0** |
| `DA65 Chaplain Gabutheron` | heal / gainquest | 10 → **30** / 10 → **0** |
| `SOR6 Battle Sister Gunner` | deal | 20 → **30** |
| `SOR30 Celestian Sacresant` | heal | 20 → **30** |
| `SOR15 Celestian` | gain | 20 → **20**（值恰好相同；来源已换成 active 行） |
| `SOR18 Dialogus` | give | 20 → **30** |
| `SOR20 Hospitaller` | heal | 20 → **30** |
| `SOR25 Imagifier` | give | 20 → **30** |
| `SOR68 Ministorum Priest` | give | 20 → **30** |
| `SOR24 Simulacrum Celestian` | give | 20 → **10** |
| `SOR17 Sister Superior` | deal | 20 → **30** |
| `SOR69 Zephyrim Superior` | drawtype | 20 → **65** |
| `SOR35 Immolator` | deal | 20 → **30** |
| `SOR66 Armorium Cherub` | give（`Rally:`） | -1 → **30**（本件顺带挂上的那张） |

> ⚠️ 同一批里 3 张早已由 G12 纠正（`AM38` 100→30 · `AM36` 65→50 · `AM70` 30→60），本件未回退。

---

## 四 · 剩 3 张为什么还是 `-1`（如实留白，⛔ 不是漏做）

| 卡 | 原因 | 要补什么 |
|---|---|---|
| `AM22 Primaris Psyker` | `chooseone` 一条 op 盖两个分支（`deal …｜give …`），每分支各有目标集 | 要么把 `chooseone` 的 payload 拆成两条 op，要么给 op 加「分支目标集」结构（**都不是本件能改的层**） |
| `SOR13 Simulacrum Bearer` | 同族（`buffOthers`）两行 `ta=10`（your troops）/ `120`（your Warlord），正文也是两条 `give` | 需要按 **op 的 `Target`（`your troops` / `your warlord`）** 做二级匹配（本件没做 —— 那要先证明 `Target.Raw` 与 `targetsAffected` 的对应，判据不足） |
| `SW9 Fenrisian Wolf` | `ca=320 followAfterResolution` = 「跟着上一条结算」（`AbilityLogic__PlayAbility.c` 里不是一种效果，是一个顺序修饰） | 需要判「320 该不该贴到 `forceattack`」（只有 1 个样本，⛔ 不猜） |

---

## 五 · 逐条 before → after（58 个 op / 45 张卡）

> 口径：`ta 原` = 改前 op 上的 `SubjectTargetsAffected`（-1 = 读不到）；`fm 原` 同。
> `行号` = `ActiveAbilityRow`（改后的值）。

| 卡 | 关键词 | op(动词) | ta 原→新 | fm 原→新 | 行号 |
|---|---|---|---|---|---|
| AM59 Astropath | duty | op0 `drawtype` | -1 → **65** | -1 → 0 | 0 |
| AM59 Astropath | duty | op1 `lowercost` | -1 → **150** | -1 → 0 | 1 |
| AM68 Tempestus Scion | duty | op0 `deal` | -1 → **30** | -1 → 0 | 0 |
| AM68 Tempestus Scion | duty | op1 `stun` | -1 → **90** | -1 → 0 | 1 |
| AM23 Bombast Field Gun | duty | op0 `deal` | -1 → **30** | -1 → 0 | 0 |
| AM28 Chimera | duty | op0 `deploy` | 10 → **0** | 0 → 0 | 0 |
| AM35 Leman Russ Vanquisher | duty | op0 `deal` | -1 → **90** | -1 → 0 | 0 |
| AM35 Leman Russ Vanquisher | duty | op1 `deal` | -1 → **90** | -1 → 0 | 0 |
| AM73 Taurox Prime | duty | op0 `deploy` | -1 → **0** | -1 → 2 | 0 |
| AM34 Valkyrie | duty | op0 `deal` | -1 → **10** | -1 → 0 | 0 |
| AM66 Hektor Thenmann | duty | op0 `deal` | -1 → **30** | -1 → 1 | 0 |
| AM66 Hektor Thenmann | duty | op1 `stun` | -1 → **30** | -1 → 1 | 1 |
| DA18 Inner Circle Companion | agenda | op0 `gain` | -1 → **20** | -1 → 0 | 0 |
| DA13 Librarian | agenda | op0 `drawtype` | -1 → **65** | -1 → 0 | 0 |
| DA13 Librarian | agenda | op1 `gainquest` | -1 → **20** | -1 → 0 | 1 |
| DA20 Master Lazarus | agenda | op0 `triggerability` | -1 → **30** | -1 → 0 | 0 |
| DA20 Master Lazarus | agenda | op1 `gainquest` | -1 → **20** | -1 → 0 | 2 |
| DA29 Darkshroud | agenda | op0 `give` | -1 → **30** | -1 → 0 | 0 |
| DA14 Ravenwing Speeder | agenda | op0 `deal` | -1 → **30** | -1 → 0 | 0 |
| DA27 Ravenwing Talonmaster | agenda | op0 `deal` | -1 → **10** | -1 → 0 | 0 |
| DA27 Ravenwing Talonmaster | agenda | op1 `lose` | -1 → **10** | -1 → 0 | 1 |
| DA65 Chaplain Gabutheron | agenda | op0 `heal` | 10 → **30** | 0 → 0 | 0 |
| DA65 Chaplain Gabutheron | agenda | op1 `gainquest` | 10 → **0** | 0 → 0 | 1 |
| DA71 Watcher in the Dark | agenda | op0 `choosecard` | -1 → **180** | -1 → 0 | 0 |
| DA71 Watcher in the Dark | agenda | op1 `gainquest` | -1 → **0** | -1 → 0 | 1 |
| DA75 Master Zacharial | agenda | op0 `choosecard` | -1 → **50** | -1 → 0 | 0 |
| DA75 Master Zacharial | agenda | op1 `draw` | -1 → **0** | -1 → 0 | 1 |
| DA75 Master Zacharial | agenda | op2 `gainquest` | -1 → **0** | -1 → 0 | 2 |
| SOR26 Aestred-Thurga | pray | op0 `give` | -1 → **10** | -1 → 1 | 0 |
| SOR6 Battle Sister Gunner | pray | op0 `deal` | 20 → **30** | 0 → 1 | 0 |
| SOR5 Battle Sister | pray | op0 `gain` | -1 → **20** | -1 → 1 | 0 |
| SOR5 Battle Sister | pray | op1 `heal` | -1 → **20** | -1 → 1 | 1 |
| SOR30 Celestian Sacresant | pray | op0 `heal` | 20 → **30** | 0 → 1 | 0 |
| SOR34 Celestian Superior | pray | op0 `deploy` | -1 → **0** | -1 → 1 | 0 |
| SOR15 Celestian | pray | op0 `gain` | 20 → **20** | 0 → 1 | 0 |
| SOR18 Dialogus | pray | op0 `give` | 20 → **30** | 0 → 1 | 0 |
| SOR29 Ephrael Stern | pray | op0 `deal` | -1 → **30** | -1 → 1 | 0 |
| SOR20 Hospitaller | pray | op0 `heal` | 20 → **30** | 0 → 1 | 0 |
| SOR25 Imagifier | pray | op0 `give` | 20 → **30** | 0 → 1 | 0 |
| SOR68 Ministorum Priest | pray | op0 `give` | 20 → **30** | 0 → 1 | 0 |
| SOR67 Sacred Rose Sister | pray | op0 `gain` | -1 → **20** | -1 → 1 | 0 |
| SOR24 Simulacrum Celestian | pray | op0 `give` | 20 → **10** | 0 → 1 | 0 |
| SOR4 Sister Novitiate | pray | op0 `deploy` | -1 → **30** | -1 → 1 | 0 |
| SOR17 Sister Superior | pray | op0 `deal` | 20 → **30** | 0 → 1 | 0 |
| SOR37 Triumph of St. Katherine | pray | op0 `deal` | -1 → **30** | -1 → 1 | 0 |
| SOR69 Zephyrim Superior | pray | op0 `drawtype` | 20 → **65** | 0 → 1 | 0 |
| SOR35 Immolator | pray | op0 `deal` | 20 → **30** | 0 → 1 | 0 |
| SOR65 Celestian Sacresant Aveline | pray | op0 `give` | -1 → **30** | -1 → 1 | 0 |
| SOR1 Erika Luminas | pray | op0 `give` | -1 → **30** | -1 → 1 | 0 |
| SOR2 Junith Eruita | pray | op0 `heal` | -1 → **30** | -1 → 1 | 0 |
| SOR3 Morvenn Vahl | pray | op0 `deal` | -1 → **30** | -1 → 1 | 0 |
| SW8 Blackmane Reiver | ferocity | op0 `drawtype` | -1 → **65** | -1 → 0 | 0 |
| SW8 Blackmane Reiver | ferocity | op1 `give` | -1 → **150** | -1 → 0 | 1 |
| SW23 Fenrisian Drop Pod | ferocity | op0 `deploy` | -1 → **0** | -1 → 0 | 0 |
| UM96 Chapter Champion | oath | op0 `gain` | -1 → **20** | -1 → 0 | 0 |
| UM99 Phobos Librarian | oath | op0 `deal` | -1 → **30** | -1 → 0 | 0 |
| UM95 Attack Bike | oath | op0 `gain` | -1 → **20** | -1 → 0 | 0 |
| UM105 Dreadnought | oath | op0 `deal` | -1 → **30** | -1 → 0 | 0 |

（共 58 个 op 的目标集/倍数发生变化）
---

## 六 · 版本守卫（`CardDatabase.cs`）

| 位置 | 内容 |
|---|---|
| `Data/CardDatabase.cs:516-548` | `ActiveAbilityLogicDto` 新增 `ca` / `pa` / `dmg` ＋ 新类 `ActiveAbilityDamageDto`（四格 `MissingIndex(-99)` 缺键哨兵 —— 四格的 `0` **都是合法值**） |
| `Data/CardDatabase.cs:1089-1102` | **`if (dto.version < 24)` → `Debug.LogWarning`**（旧表 ⇒ 三格 `null = 未知` ⇒ 归位退回 v23 那一档；⛔ 不是「每行都没有种类」） |
| `Data/CardDatabase.cs:1740-1790` | `MapActiveAbilityLogic` 里三格的形状校验：逐格 `< -1` ⇒ 出声落 `-1`；**长度与 `ta` 不一致** ⇒ 出声把该格置 `null` |
| `Data/CardDatabase.cs:1288-1323` | 挂载读数那行改成**逐结论计数**（新取值没登记会被打出来） |

---

## 七 · 断言

**类名 / 入口**：`EngineE1Assert.Run()`（`MyGame/Assets/CardPresentation/Editor/RuleEngineTest_EngineE1.cs`，
482 行，`public static class EngineE1Assert`，⛔ 不是 `RuleEngineTest` 的 partial —— 与 `E3/E4/E5` 同形：
自带 `Run()` + `E1 ✓/✗` 行首标记 + 末行 `E1 断言合计: N 通过 / M 失败`）。
主对话挂进宿主时用：`-executeMethod EngineE1Assert.Run`（或从某个 Scene 宿主的 `Run()` 里调一行）。

| 段 | 条数 | 钉什么 | 灭自证 |
|---|---|---|---|
| A `E1KindColumnsReadings` | 12 | 193 行 · `ca`/`pa`/`dmg` 与 `ta` 同长 · `ca` 分布（10:70 / 30:27 / 462:31 / 0:7）· `pa=10` 31 格 · `dmg` 全读到 | 把三格写成**新行** ⇒ 行数变 ⇒ 红 |
| B `E1VersionGuard` | 15 | v23 合成表 ⇒ 三格 `null` + 恰好 1 条 `<24` 警告 + 归位 `gap:shape` + **op `ta` 仍 `-1`**（来源错不会再回来）；v24 同卡 ⇒ 有值 / `attached` / `ta=30` / 行号 0 | 「一律 null」和「一律空表」两侧互斥；462 行不参与配 op |
| C `E1Attribution` | 29 | 84 / 45 / 3 / 132 · `AM38`(30) · `AM28`(0) · `SOR6`(30,fm1) · `SOR25`(30) · `SOR66`(30,fm1) · `AM23`(30，110 那行不参与) · `AM35`(两条都 90) · `AM59`(65/150) · `UM105`(oath 30) · 反例 `SOR13`/`AM22`/`SW9` 仍 `-1` | ① `AM38` 自己 `cardAbilities[0].ta = 100`（≠30）；② 84+45+3 = 132；③ `AM23` 第 1 行确实是 `ta=110` |

**已做仓外 dry-run**：同一份源码 ＋ 同一份卡表，用 `d:/tmp/wf_e1assert`（net8 桩，不写仓库）跑
**56 条断言 ⇒ 56 通过 / 0 失败**。⚠️ 仍需主对话在 Unity 里跑一次（本代理 ⛔ 不许 `-executeMethod`）。

---

## 八 · 指纹 / 版本

| 项 | 结论 |
|---|---|
| `cards_engine.json` | `version 24` · `count 1127` · 1,463,954 B |
| **`PinnedPoolMd5`** | `b6035055b993b33e9bcfa93bbce97d13` → **`db11eb38b7da8c41a3214ecb91449d2f`** ⇒ **主对话必须重钉**（本代理不动 `工具/ruleprobe/Program.cs`） |
| sha256 | `56797845b101274752c86a78d6a902ec4dd4ed85d7d67f0b3db0fd4fc505cfd3` |
| `PinnedParseMd5` / `PinnedBaselineMd5` | **预期不动**：`工具/ruleprobe/Scan.cs:179` 只走 `EffectText.Parse(desc, …, card:)`（整条 desc ⇒ `IsWholeDesc` 为真 ⇒ 走 G1/G2 那道，**不经**本件新闸）；新闸只在**片段**分支 |
| `play` 指纹 | **预期不动**（判据：`SubjectTargetsAffected` 的唯一消费者 `EffectResolver.ResolveTargets` 的 `Subjectless` 分支要求 `source == null`；替代行动/誓约都带施放者。`fm` 只有 `>1` 才乘，本批挂上的 `fm` 只有 `AM73`/`AM16` 两处 = 2，而两处正文都写着 2 ⇒ 只对账不乘）⚠️ **未实跑**（⛔ 不跑 ruleprobe） |
| 既有断言会红（**要让主对话知道**） | `RuleEngineTest_G12.cs` 的 ① `G12Attribution` 汇总 `38/48/45/1/0` 与 `SOR17 = gap:shape`、`SOR66 = gap:no-keyword` —— 这三处**现在都变了**（84 / 3 / 45；`SOR17`/`SOR66` = `attached`）；② `G12OldTableWarns`：v22 表现在会出 **2** 条含 `activeAbilityLogic` 的警告（`<23` ＋ `<24`），v23 表会出 **1** 条（`<24`）⇒ 那两条 `Check(…,1)` / `Check(…,0)` 要改成按版本过滤。⛔ 本代理没改别人的文件 |

---

## 九 · 台账矛盾核对（简报第 3 件，**不改总表**）

**结论：台账那句「已核销」在代码侧成立，§一 行内那个 `✗` 已经被划掉（`~~✗~~ → ✅`），不是「说了却没做」。**

1. **`TargetsAffected = 150` 的 `CountEval` 真实现了**：
   `Core/CountEval.cs:421-475` 的 `ta == 150` 分支 —— 候选 = `ctx.PreviousAbilityTargets`（场上单位，逐条过
   阵营 / `include*` / 条件），**并且先判「上一跳是抽牌」**（`:446-455` → `CountDrawnAsPreviousTargets`，
   抽到的牌是手牌不是场上单位，结构上必须单独建模）。**不是空壳**：读不到时会 `null` + `gap` 出声
   （`:464` 不在场上的候选 / `:485` 手牌牌库族 / `:489` 其它档）。消费链：`CountTargets`（`:139-171`）
   ← `mult`（乘数）与 `multi`（整条 ability 触发 N 遍，`EffectResolver.cs:6426-6445` → `TryStructuredMultiTrigger:11718-11748`）。
   断言：`RuleEngineTest_EngineA1.cs:45-99`（150 的计数正例/反例）＋ `RuleEngineTest_E8.cs:29-82`（抽牌那一跳）。
2. **8 张 `multiCountCriteria.targetsAffected != 0` 确实逐张核过**（现读复核）：
   池内**正好 8 张**，与台账点名的完全一致 —— `AM18`(0/ta10) · `ASH52`(1/161) · `DA57`(0/10) · `GOF46`(1/10) ·
   `GSC56`(1/50) · `SOR45`(0/10) · `SW51`(1/150) · `SW58`(0/10)（括号 = `cardAbilities` 下标 / `ta`）。
   8/8 在卡表里都有 `unitConditions.role=="multi"` ＋ `multiSet`（`ab`/`ta` 与真值逐格相同）；
   逐张断言在 `RuleEngineTest_EngineA1.cs:199-340`（DA57/SOR45/SW58 等价 · GOF46 = 1+N · GSC56 `enemyhand/sabotage` ·
   SW51 `draw/any` · AM18 正文 `countRef` · ASH52 记录形状）。
   两处「判不准」也仍然是判不准（台账已如实写）：**`AM18`** 的回合末正文在 `ResolveAtTurn` 里单独解析 ⇒
   `AbilityIndex` 恒 `-1` ⇒ 走正文 `for each`；**`ASH52`** 的 `multiSet.ta = 161 currentSpiritStone` ——
   `CountEval` 里**没有 161 这一档**（落到 `:489` 的通用 `本版未实现`）⇒ `null` + 出声，退回正文口径。
3. ⚠️ **一个仍然开着的口子（不属于本件，登记）**：`ta=150` 的 `DrawnThisResolve` 窗口是「**一张卡**」而不是
   「**一跳**」—— 若同一张卡里抽牌之后又打了别的目标再用 `150`，原版取最后一跳、我们取抽到的牌。
   现池 0 张这种卡（`CountEval.cs:443-445` 已如实标注）。

---

## 十 · 没查清 / 没做

1. **`SOR13` 的二级匹配**（op `Target` ↔ `ta`）没做 —— 判据不足，宁可留白（§四）。
2. **`AM22` 的 `chooseone` 分支目标集**没建模（要改 op 的形状，不在白名单）。
3. **`ca=320 / 325`（`followAfterResolution*`）故意不映射** —— 它们是「顺序修饰」不是效果；`SW9` 因此留 `-1`。
4. **`Parse(Desc)` 整条 desc 那条路**：对替代行动块仍可能贴 `cardAbilities` 的 `ta`（G1/G2 分支未动，G12 判过
   那批 op 不是运行期执行对象）。**没查**这些 op 是否被 `PlayerChooseOps` 一类消费 —— 若被消费，是同一类
   「来源错」的残留。建议另立账（判据要从「谁在读 `Parse(Desc)` 的产物」查起）。
5. **`fm` 的语义仍未逐行解释**（G12 §六·6 同一缺口）：`fm=2` 是召唤两份还是伤害翻倍取决于 `chosenAbility`，
   本件只搬值。
6. **未进原版实机**（原版已关服），判据停在反编译 + 枚举表 + 数据三层。

---

## 十一 · 顺手发现（只报不改）

1. 🟡 **`SOR66` 的例外闸今天只命中 1 张**（`_triggerOps` 里恰好一个键有正文）。它之所以安全是因为
   「只有一个候选正文」；某天出现「卡上有 2 个触发正文、却没有替代行动关键词」的卡 ⇒ 自动退回 `gap:no-keyword`
   （不会猜），但那张卡就会掉到读不到 —— 届时要补的是「哪个正文属于主动能力」的判据，⛔ 不是放宽闸。
2. 🟡 **`AM73 Taurox Prime` 的 `give them Flank` 那条 op 仍是 `ta=-1`**：数据里「召唤 + 给它们 Flank」只占
   **一行**（`ca=75 summonBuffedUnit`，`fm=2`），所以 `give` 拿不到独立目标集。不是本件的缺陷，是**一行盖两个 op** 的形状。
3. 🟡 **`AM34 Valkyrie` 的 `deploy` op 仍 `ta=-1`**（"For each one that dies, deploy a Shock Trooper"）——
   原版那一半是 `summonCriteria` 里的事，`activeAbilityLogic` 行只描述 `Deal 2 damage to all enemies`。同一个「一行 ≠ 一条 op」家族。
4. 🟢 **`AbilityEffect (462, 467, 468)` 属 `AddTrigger*` 族**（`PlayAbility.c:1838/1894/1910` 分别调
   `AddTriggerPray/Slay/Teleport`）—— 这条**反证**了本件「`trigger` 族 → `triggerability` 动词」的映射是**原版真义**，
   不是我们编的（`DA20 Master Lazarus` 的 `triggerability` op 因此挂上 `ta=30`）。
5. 🟢 简报白名单写的是 `Core/CardDatabase.cs` / `Core/EffectOp.cs` —— **两个都不存在**：
   `CardDatabase.cs` 在 `Data/`；`EffectOp` 是 `EffectText.cs` 里的类。本件按实文件改。

---

## 十二 · 改动清单（全部在白名单内 ＋ 一处必需越界）

| 文件 | 改动 |
|---|---|
| `工具/gen_cards_engine.py` | `+` 三格落值 / 注释 / 读数 / `version 23→24`（CRLF 未翻，二进制核过） |
| `Resources/cards_engine.json` | 重新生成（1,463,954 B） |
| `RuleEngine/Data/CardDatabase.cs` | DTO 三格 ＋ `ActiveAbilityDamageDto` ＋ `<24` 守卫 ＋ 映射校验 ＋ 挂载读数改写 |
| `RuleEngine/Core/CardDef.cs` | `ActiveAbilityLogicSpec` 三格 ＋ `ActiveAbilityDamageSpec` ＋ `AttachByAbilityKind` 一族 ＋ 关键词例外（`SOR66`）＋ `IsActiveActionKeyword` ＋ `AddTriggerOp` 传闸 |
| `RuleEngine/Core/EffectText.cs` | ⚠️ **越界但必需**：`Parse(..., alternativeActionBody)` ＋ 片段分支 `return`（简报第 2 件点名「判断点在 `AssignAbilityTargets` 的片段分支」，而它在 `EffectText.cs`；不在 ⛔ 黑名单里） |
| `CardPresentation/Editor/RuleEngineTest_EngineE1.cs`（＋`.meta`） | 新建：`EngineE1Assert.Run()`，56 条断言 |
| `资料/普查产出_1024b/E1_chosenAbility与ta来源.md` | 本报告 |
