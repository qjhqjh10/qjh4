# B114 · W-22 · `GOF12 Spanner` 的 `TriggerOpsByNumber(300)` 逐条读数

> 写手 `W-22` · 2026-11-01 · 活状态表 `B-251`（`W-19` 留下的未尽读数）
> ⛔ 本报告**只给数**，不改任何生产代码（`Assets/**` 一个字节未动）。

---

## 〇、一句话结论

`TriggerOpsByNumber(300)` = **1 条**（`chooseone` + `RandomPick=true`，5 个候选装在一条 op 的 `Payload` 里），
`LogicIndex = 0`（**不是 -1**）。⇒ `RandomAbilityChoiceOps` 在 `ops.Count < 2` 那一步就返回，
**`LogicIndex < 0` 那一档在现池上结构上够不到** ⇒ `B-251` 属**前提不成立**，不是缺陷。

---

## 一、怎么复现（新探针，可重跑）

```bash
cd /d/4/Unity
bash 工具/ruleprobe.sh triggerops GOF12          # 单卡逐条表（默认就是 GOF12）
bash 工具/ruleprobe.sh triggerops GOF12 scan     # ＋ 全池两段扫（见 §四）
```

新增文件（本批唯一改动，全在 `工具/ruleprobe/`）：

| 文件 | 改动 |
|---|---|
| `d:/4/Unity/工具/ruleprobe/TriggerOpsProbe.cs` | **新建**（`triggerops` 读数口；`CardDatabase.Parse` 真实装载链） |
| `d:/4/Unity/工具/ruleprobe/Program.cs` | **只加**一个 `if (args[0] == "triggerops")` 分支（放在 `traits` 分支之后，未改既有行） |
| `d:/4/Unity/工具/ruleprobe/ruleprobe.csproj` | **只加**一行 `<Compile Include="TriggerOpsProbe.cs" />` ＋ 一段说明注释 |

`dotnet build` 成功；探针 `rc=0`，**未跑 Unity**、未动 git、未碰 `Assets/**`。

🔴 **它走 `CardDatabase.Parse(File.ReadAllText(PoolPath))`，不用 `Program.PoolList`** ——
原因见 §五·①（`Program.PoolList` 那条手读 JSON 的路**不挂** `AttachTriggerOpsByNumber`，
`TriggerOpsByNumber` 会静默落进「卡面关键词」退回档，量出来的不是产品口径）。

---

## 二、`GOF12 Spanner` 的 ops 逐条表（产品口径，`HasTriggerOpsByNumber = True`）

### 2·1 `TriggerOpsByNumber(300)` —— **1 条**

| # | Verb | Tail | AbilityIndex | LogicIndex | 其它（读数） |
|---|---|---|---|---|---|
| 0 | `chooseone` | `null` | **0** | **0** | `AbilityBlockSize=1` · `AbilityMap="block"` · `ActiveAbilityRow=-1` · `Amount=5` · **`RandomPick=True`** |

`Source` = `"Give a random bonus to a friendly Vehicle"`
`Payload` = 5 个候选（`|` 分隔）：
`give +2 melee to a friendly vehicle` ·
`give +2 ranged attack to a friendly vehicle` ·
`give +2 health to a friendly vehicle` ·
`give +2 melee and +1 health to a friendly vehicle` ·
`give +2 ranged attack and +1 health to a friendly vehicle`

### 2·2 `TriggerOps("mob")` —— **1 条**，与上面**同一条 op、同形状**

| # | Verb | Tail | AbilityIndex | LogicIndex | 其它 |
|---|---|---|---|---|---|
| 0 | `chooseone` | `null` | **0** | **0** | 同 §2·1（`Amount=5` · `RandomPick=True`） |

⚠️ 键是**规范小写**：`TriggerOps("Mob")` = `null`（0 条），`TriggerOps("mob")` = 1 条。
（`AbilityTriggerNumberOfName` 的比较器是 `OrdinalIgnoreCase`，所以 `AbilityTriggerNumber("Mob")` / `("mob")` 都是 **300**。）

### 2·3 一共几条

**`TriggerOpsByNumber(300)` = 1 条 · `TriggerOps("mob")` = 1 条**（同一条）。

### 2·4 数据侧背景（同一张卡，供对照）

- `triggerAbilityIndices` = **5 行**，全部 `ab=0` · `lg=0..4` · `trigger=300` · `triggerValue=0` · `ca=30`
  ⇒ 原版这张卡的 ability[0] 有 **5 条 abilityLogic**。
- `animTimes` = **5 行**，全部 `trigger=300` · `tn=Mob` · **`rc=1`**（`Order` = `0.0.0` … `0.4.0`）⇒
  `CardDef.TriggerRandomChoice("Mob") = True`。
- 卡面 `desc` = `Give a random bonus to a friendly Vehicle` ／ `descZh` = `[[mob]]：给一个友方载具一个随机的加成`。

---

## 三、对「两档判定」的结论

按简报的两档判：

| 档 | 判据 | 本卡实得 | 结论 |
|---|---|---|---|
| **A（只有 1 条 ⇒ 够不到）** | `ops.Count < 2` 在 `RandomAbilityChoiceOps` 的**第一道闸**就返回 | `ops.Count = 1` | ✅ **命中** |
| B（≥2 条 ∧ `LogicIndex == -1` ⇒ 才谈得上修） | —— | 不成立（1 条，且 `LogicIndex = 0`） | ✗ |

**入口闸逐项（静态读数，代码 `Core/EffectResolver.cs:18638` 起）**：

1. `ops == null || ops.Count < 2` ⇒ **命中，原样返回**（后面每一档都跑不到）；
2. `TriggerRandomChoice(tn)` = `True`（这一道过得去，但已经到不了）；
3. `LogicIndex < 0` ⇒ 本卡 **0 条** -1（唯一那条是 `0`）⇒ 即便上一步放行，这一档也不命中。

⇒ **`B-251` 应按「判据本身错 / 前提不成立」核销，不是缺陷**（这正是简报给的第一档）。
⛔ 本报告不改代码、不擅自核销 —— 核销动作归调度台。

**追加一条独立证据（这条比上面更强）**：那 5 个「随机选项」**不是 5 条 abilityLogic 的正文**，
而是**一条 `chooseone` op 的 5 个 `Payload` 候选**，且 `RandomPick = True`；
消费端 `EffectResolver.DoChooseOne`（`Core/EffectResolver.cs:4654`）在 `RandomPick` 为真时
**本来就 `ctx.GetRandomInt(0, opts.Length)` 随机挑一项**。
⇒ 这张卡的「随机」在**我们的模型里已经在 payload 层实现了**，与 `abilityLogic` 层那一跳无关。

---

## 四、全池扫（两段）

### 4·1 `rc = 1` 的卡：**1 张**（只有 `GOF12`）

| 卡 | rc 行 | 触发名 | trigger | `TriggerOpsByNumber` 条数 | LogicIndex -1 条数 | 判定 |
|---|---|---|---|---|---|---|
| `GOF12 Spanner` | 5（同一触发名 `Mob`） | `Mob` | 300 | **1** | 0 | 够不到（`ops.Count < 2` 返回） |

**小计：`rc=1` 的卡 1 张 · `ops.Count >= 2` 的 (卡, 触发名) 对 = 0 个。**
⇒ 现池里**没有任何一张卡**能走到「按 `LogicIndex` 分组 / 随机抽一条」那一段。

### 4·2 附加扫：若「将来某张卡也拿到 `rc=1`」，`LogicIndex == -1` 会不会被撞上

**全池「某个 trigger 上 `TriggerOpsByNumber` 条数 >= 2」的卡 = 20 张**，其中 **17 张**含 `LogicIndex == -1` 的 op：

| 卡 | trigger | 条数 | `LogicIndex=-1` |
|---|---|---|---|
| `AM9 Kasrkin` | 301 | 2 | 0 |
| `BL32 Iskandar Khayon` | 560 | 2 | 2 |
| `DA37 Deathwing Knight Master` | 690 | 2 | 2 |
| `DA25 Deathwing Knight` | 690 | 2 | 2 |
| `EC18 Glittering Myriad Infractor` | 710 | 2 | 2 |
| `EC26 Terminator Champion` | 720 | 2 | 2 |
| `EC19 Threnodic Noise Marine` | 715 | 2 | 2 |
| `GSC28 Hulking Aberrant` | 670 | 2 | 2 |
| `GSC73 Spotter Ridgerunner` | 680 | 2 | 2 |
| `GOF96 Snakebite Battlewagon` | 81 | 2 | 0 |
| `SOR5 Battle Sister` | 655 | 2 | 2 |
| `SOR29 Ephrael Stern` | 655 | 2 | 2 |
| `SOR13 Simulacrum Bearer` | 655 | 2 | 2 |
| `SW34 Murderfang` | 120 | 2 | 2 |
| `TL62 Ravener` | 0 | 2 | 0 |
| `TL16 Mawloc` | 580 | 2 | 2 |
| `UM29 Captain Sicarius` | 145 | 2 | 2 |
| `UM26 Epistolary Librarian` | 145 | 2 | 2 |
| `UM68 Primaris Techmarine` | 145 | 2 | 2 |
| `UM86 Assault Doctrine` | 145 | 2 | 2 |

（20 张 · `LogicIndex` 全可读的只有 3 张：`AM9`(301) · `GOF96`(81) · `TL62`(0)）

⇒ **「`LogicIndex` 读不到」是这 20 张里的常态（17/20）**，但**它们全都没有 `rc=1`**
⇒ 今天这条「不猜」分支**一次都不会被走到**；它只是**堵形状**的桩。

---

## 五、还差什么 / 顺手发现

### ① 🔴 顺手发现（口径类，可能影响别的探针）：`Program.PoolList` 不挂 `AttachTriggerOpsByNumber`

`工具/ruleprobe/Program.cs` 的 `LoadPool()` 是 `System.Text.Json` 手读卡表、自己 `new CardDef(...)`，
**不经过** `Data/CardDatabase.cs` 的装载链 ⇒ `_triggerOpsByNumber` 恒 `null` ⇒
任何拿 `Program.PoolList` 量 `TriggerOpsByNumber(...)` 的读数都会**静默落进「卡面关键词」退回档**
（`CardDefPartial_B96CA.cs:102` 的第二级），**看起来和产品一样、其实不是同一条路**。
这与已记的 `A1911` / `A1959` / `B8_AV`（`inventoryOptions` / `choicesDefinition` / `targeted` 不传）是**同一族**。
本报告因此改用 `CardDatabase.Parse`（`TraitsProbe` 同款）。
📌 **建议**：以后凡要量 `TriggerOpsByNumber` / `HasTriggerOpsByNumber` 的读数口，一律走 `CardDatabase.Parse`；
是否把 `LoadPool` 补上这一跳，**属于别人白名单**（`Program.cs` 是全探针共用件），本批不动。

### ② 结构事实：`TriggerOpsByNumber` 是**按 ability** 收的，不是按 logic

`Data/CardDatabasePartial_B98H7.cs` 的 `MapTriggerOpsByNumber` 只按 `op.AbilityIndex == row.Ab` 归位，
同一 `ab` 的 5 条 `lg` 行共用**同一批 op 对象**（用 `HashSet<EffectOp>` 去重）。
⇒ 对 `GOF12` 这种「1 个 ability × 5 条 logic」的形状，**返回的列表永远是 1 条**，
`lg` 的信息**在返回值里根本不体现**。
⇒ 即便将来要修「按 abilityLogic 抽一条」，**光靠 `TriggerOpsByNumber` 也拿不到 per-logic 的 op 划分**
（这正是 `W-19` 说的「还得先证 op ↔ abilityLogic 的对应关系，本机没有那个判据」的**代码级原因**）。

### ③ 还差什么

- 无（本读数已闭合）。若调度台要**核销** `B-251`，可直接引用 §二·3 的「1 条」与 §四·1 的「全池 0 对」。
- 若反过来要**保留**这条代码（堵形状），它今天的行为是「原样返回全部」= 与 `DoChooseOne` 的 payload 层随机**不冲突**；
  ⛔ 但它**不是**原版 `ability+0x48` 的等价实现（原版抽的是 abilityLogic 层）。

### ④ 本批没做的事（明说）

- ⛔ 没跑 Unity、没跑 `check` / `play` / `ai`、没碰基线或三个指纹。
- ⛔ 没改 `Assets/**` 任何 `.cs`；`TriggerOpsProbe.cs` 的 `RandomPick` 只用于**打印**。
