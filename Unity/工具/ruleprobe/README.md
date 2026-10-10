# ruleprobe —— 引擎离线对拍探针

> 🔴 **它是【快速内环 / 旁证】，正式验收仍然是 `RuleEngineTest.Run`（断言的真宿主）。**
> 措辞照本家族先例 `D:/tmp/wf_b29_probe/Program.cs:2`。
> 长文档（原理 / 桩的边界 / 已知的坑）→ `资料/引擎离线对拍_探针.md`。

## 它解决什么

`RuleEngine/Core/` 按工程**成文规矩**只碰 `UnityEngine.Debug` / `UnityEngine.Random`
（`TutorialScript.cs:344-352` 把它写成判据，Core 各文件头注逐条重申）⇒ 三十行桩就能把**整个 Core**
编成控制台程序，**不跑 Unity、不占 Unity 实例、不用等 1–9 分钟编译**。

实测：**重编 ≈ 1.5 秒**，`check` 全池一趟 **≈ 2 秒**。对比 `_run_8_checks.sh` 的 1–9 分钟 / 条。

## 怎么跑

```bash
bash d:/4/Unity/工具/ruleprobe.sh            # check：与基线对拍，打一行摘要（默认）
bash d:/4/Unity/工具/ruleprobe.sh b19test    # 45 条 B19 断言
bash d:/4/Unity/工具/ruleprobe.sh willrun    # 🆕 「卡面正文收到没有」全池读数（见下节）
```

判据 = **看那行 `=== 全池 … 解析差异 N 行 … ===`**：

| 摘要 | 含义 | 绿红 |
|---|---|---|
| `解析差异 0 行` | 解析器没动 | ✅ 绿 |
| `改名 K 行`（解析差异 0） | 本工程每批都在改卡名 / 重跑产物 —— **通常无害** | 🟡 黄，看一眼 |
| `解析差异 > 0` | 引擎解析器真变了 | 🔴 红，**逐行看**（是修基线还是修解析器） |
| 🆕 `哨兵合计：失败 N 条`（`N>0`） | **与基线无关**的自证失败了（`A1348`）—— ⛔ **重生成基线消不掉它**，必须去看那句 `✗` | 🔴 红 |

⚠️ **退出码把两者合并**：基线绿但哨兵红 ⇒ 仍然 `rc=1`（⛔ 不许出现「哨兵红、界面全绿」）。

```bash
# 手搓一条语句看解析结果
dotnet bin/Debug/net8.0/wfprobe.dll seg "<卡面英文原文>"
# 看某张卡
dotnet bin/Debug/net8.0/wfprobe.dll card "<卡名>"
# 生成一份全池 dump（基线就是这么来的）
dotnet bin/Debug/net8.0/wfprobe.dll scan "基线/out_baseline.txt"
```

## dump 的列（`A1368` 四列 ＋ `A1374` 三列，2026-10-10）

一条记录 = `id|name|unparsed|partial|op;op;…`，每条 op =
`verb/chooseSrc/chooseWhat/chooseAct/amount/payload` **＋ 尾部七列**：

| 列 | 从哪来 | 形状 |
|---|---|---|
| `cost` | `EffectOp.Cost` | 整数，`0` = 不付费 |
| `shared` | `EffectOp.CostShared` | **`1`/`0`**（`1` = 这份代价是同组**别人**付的，本 op 不再收钱） |
| `condKind` | `EffectOp.ConditionKind` | 条件的**规范名**，空 = 没条件/认不出 |
| `countRef` | `EffectOp.CountRef` | `for each …` 数谁，空 = 不计数 |
| `costKind` | `EffectOp.CostKind` | **货币名**（`energy` / `faith` / `spirit` / `oath` / 空）🆕 `A1374` |
| `countScope` | `EffectOp.CountScope` | 计数**从哪儿来**（`board` / `draw` / `died` / `played` / `spiritspent` / `darkpact` / 空）🆕 `A1374` |
| `condition` | `EffectOp.Condition` | 条件的**原文**（`you have less than 6 ☀`），空 = 真的没条件 🆕 `A1374` |

🔴 **为什么加**（台账 `A1368`）：前四样原来**一个字都不打** ⇒ 凡改「付费 / 条件 / 计数」三族的活，
`check` 都给不出信号。第十三会话 `W6b` 撞过一次：它写的一个回归让 **28 张**卡的 `cost` 全变 0，
**`check` 全绿**，是它另建的一次性副本才看出来的。
**构造性实测（2026-10-10）**：拿一份**仓库外**的 Core 副本、只把主分支那句 `StampPaidCost` 的费用改成 0
（**只动 `Cost` 一列**）⇒ **旧 dump 逐字节零差异（旧 `check` 完全瞎）**；**新 dump 81 行差异（`check` 红）**。

🔴 **`A1374` 那三列为什么也要加**（同一族、补上剩下的盲区）：

- `costKind` —— 「**只改货币名**（`energy` ↔ `faith`）」原来同样进不了 `check`。
  ⚠️ **`A1357` 正是这种改动**（`SOR72` 的付费前缀 `Energy` → `☀`）：它当时被看见**纯属侥幸**，
  只因动词也跟着从 `gain` 变成了 `gainfaith`。**只改货币、动词不动 ⇒ 旧 `check` 全绿。**
  （`Program.Dump` 一直打 `ck=`、`scan` 不打 ⇒ 两个入口口径不一致，两个入口现在一致了。）
- `countScope` —— 单看 `countRef=own|unit|all` 能猜到是 `board`，但 `countRef=board`
  （`darkpact` 那支）与「**不计数**」在 dump 里看起来只差一个字。
- `condition` —— `condKind` 为空时分不出「**没条件**」与「**有条件但认不出**」
  （后者是已知的静默风险，`EffectText.cs:1595-1599`）。

**构造性实测（2026-10-10 · `A1374`）**：仓库外一份**池副本**，把 `SOR72` 的付费前缀 `6 ☀` 改回 `6 Energy`
（**只改货币名**，`seg` 对照确认其余字段逐字不变）⇒ **摘掉尾部三列（= 旧格式）A/B 零差异（旧 `check` 完全瞎）**；
**新格式 1 行差异**，端到端 `check` 报 `解析差异 1 行` + `rc=1` ✅

⚠️ **`countRef` 与 `condition` 都原样打、不做转义**：`countRef` 的 `board` 族取值自带两个 `|`
（`enemy|unit|all`）—— 全池 **27 个 op** 的 `countRef` 带 `|`（落在 **25 行**上）⇒ 新基线里 `|` 数 >4 的行共 **44 行**
（旧基线 **19 行**）。`condition` 实测**全池不带** `|` / `;` / `/`（已逐 op 数过），但**将来可能有**。
⛔ **别据此判「格式坏了」**：`chooseone` 的 `chooseWhat` 早就这样（旧基线那 19 行就是它），
而 `check` 只认**前两个** `|`（`KeyOf`/`DefOf`，`Program.cs` 的 `CheckMain`）⇒ 不受影响。
⛔ **也不许把 `|` 换成 `,`** —— 那会让 dump 的值与引擎字段**不同**，下一个人照 dump 抄进断言就抄错了
（正是本笔要治的那一族）。渲染只有一处：`Program.CountRefText`（`Program.cs`）。

⚠️ **取尾部列必须【从后往前】数** —— `payload` 里**可以**含 `/`（实测 `ASH65` 的
`<i>shuriken</i> 2` 就是，那条 op 有 **14** 个 `/` 字段，别的都是 13）⇒ 从前往后数下标会**静默拿错列**。
`Sentinel` 里用的是 `f[f.Length - fromEnd]`。

## `willrun` —— 「卡面正文**收到没有**、收到的是不是空的」（🆕 `A1432` / `A1433`，2026-10-10）

```bash
bash d:/4/Unity/工具/ruleprobe.sh willrun                  # 全池读数（≈2 秒，不跑 Unity）
bash d:/4/Unity/工具/ruleprobe.sh willrun dump <out.tsv>   # 附带一份逐卡 TSV
```

### 它修掉的是哪一种「假绿」

原来的全池尺子只有**一种**问法：**拿 `EffectText.WillRunOps(c)` 返回的那批 op，逐条问
`OpHasMechanism`**（`RuleEngineTest.ReportWillRunMechanism:8452-8495` 的计数形状 ·
`EffectText.Coverage` 第二层 `:1789-1834`）。那是一种 **【空转 ⇒ 全绿】** 的形状：

```csharp
foreach (var op in ops)          // ← ops 是空表时，这个循环一次都不进
    if (!OpHasMechanism(op, …)) { 卡点++; break; }
// ⇒ 空表 = 0 个卡点 = 这张卡算「全通」
```

🔴 **实测代价（`A1386`）**：`CardDef.RoutableTriggers` 缺 `survivor` / `sacrifice` 时，
卡面印着的正文**根本收不下来**、`FireTriggerAt` 恒空转，而报表照样 **586/586 全绿**。
**不是漏报一张 —— 是这一整类「正文没收下来」永远报不出来。**
（本仓明令要防的【自证】形状：被测实现和它的检测器**用同一个口**。）

### 怎么修：两条【独立的腿】+ 空表三分类

| 腿 | 从哪来 | 借什么 |
|---|---|---|
| **腿 A（执行层）** | `EffectText.WillRunOps(c)` | 引擎的注册结果 |
| **腿 B（卡面）** | `desc` 分句 ∪ `keywords` **原文条目**里 `Head: body` 且 `body` 主解析器解得出 op 的段 | 只借**归一函数**（`EffectText.Split` / `StripLeadingIcons`），**不借注册结果** |

两头对账 ⇒ 一张卡收 0 条 op 时，三种可能**分开报**：

| 档 | 判据 | 是缺陷吗 | 计入 rc |
|---|---|---|---|
| **① 本来没正文** | 卡面上没有任何「可执行正文段」（`desc` 空 / 整条裸关键词） | ❌ 不是。但**必须报出来**，⛔ 不许混进「全通」 | 否 |
| **② 收漏了** | 卡面**写着**可解析的正文，执行层**一条都没收到** | 🔴 **是**（`A1386` 那种） | **是** |
| **③ 未归因** | 收 0 条、卡面**没有** `Head:` 段，但主解析器对整条 `desc` 解得出 op | ⚠️ **不判**（多半由 `WillRunOps` 之外的层消费，例 `EffectResolver.ResolveAtTurn:4792` 扫 `u.Card.Desc`） | 否 |

**段级**再分三档（一张卡可以「收了一半」—— 那一半**三张表全绿**而卡面那半句就是不发生）：

| 档 | 判据 | 计入 rc |
|---|---|---|
| **`收漏`** | 触发头没进本卡的 `TriggerTexts`（`AddTriggerOp:1320` 要求头在 `CardDef.RoutableTriggers` 里）—— **`A1386` 是这一族** | **是** |
| **`付费段`** | `N ☀:` / `N [Energy]:` / `N [Spirit Stone]:` 形状的**付费能力段** —— `WillRunOps` 的 unit/hero 支**压根不看这类段** | **是**（⚠️ 另一族病灶：**覆盖面**，不是「表里缺词」） |
| **`同头两写`** | 同一个头**注册过**，但 `desc` 与 `keywords` 两份正文**不一样**、后者被「先到先得」丢掉 | 否（⚠️ **数据不一致**，另行处置） |

**判据**：**② 收漏了 = 0 张**、且段级 `收漏` / `付费段` 都为 0、**且 ⑤「无消费点」= 0 条** ⇒ `rc=0`；
否则 `rc=1`（⛔ 不许静默绿）。
它每次都会**同时**打一行「老口径」供对照 —— 两行并排就是「空转 ⇒ 全绿」的现场。

### 🔴 ⑤「无头正文」逐句报账 —— 四档**全部以 `Head:` 为锚**，这一档治的就是那道缝（🆕 `A1459`，2026-10-10）

**病灶**：①②③④ 四档的入口**全都是 `Head: body`**（腿 B 的 `AddFaceBody` 在 `col <= 0` 时**直接 return**）
⇒ 「**卡面没有头、又没被消费**」的句子**哪一档都进不去**：它**有**正文，所以不是 ①；
③ 的判据是「**腿 A 收到 0 条**」，而它多半出现在**有别的 op** 的卡上 ⇒ 也进不去。

🔴 **实测后果**（出处 `资料/普查产出_第十四会话/诊断_未归因24张.md` §④㈡）：`SW23 Hrolf the Ironhowl`
（`Friendly Beasts cost 1 less.`）/ `GSC71 Atalan Leader`（`Friendly Vehicles cost 1 less.`）/
`GSC36 Metamorph Leader`（`Your troops cost 1 less.`）—— **三张真缺陷正是靠这道缝躲开清单的**：
它们各自**还有一条别的 op**（`Rally` / `Strike` / `Uprising`）⇒ **腿 A ≠ 0** ⇒ 连 ③ 都进不去；
而**那句无头的**在腿 B 里**结构上不成段**（⚠️ 这三张的 `faceBodies` 是 **1**，是它那条**有头的** `Rally:`；
「`FaceBodies=0`」那个数出自诊断文档 §三·1，**不成立**）⇒ 段级那一趟（④）**也照不到**。

**这一档的判据（四道闸，缺一条就会误报）**：

| # | 闸 | 判据（**全部转调引擎的公开面，探针里一行新文法都不写**） |
|---|---|---|
| 1 | **无头** | `StripLeadingIcons(seg)` 之后 `IndexOf(':') <= 0` —— **与 `AddFaceBody` 拒收用的是同一个表达式** |
| 2 | **解得开** | `EffectText.Parse(seg)` 出得来 ≥1 条 op |
| 3 | **腿 A 没收到** | 那批 op **一条都不在** `WillRunOps` 里（按 `Sig` 比 —— 与腿 B **同一把尺子**，⛔ 不比 `Source`） |
| 4 | **没有别的层认领** | `CardDef.HandledByOtherLayer` **空** ∧ `EffectText.AtTurnClauses(seg)` **空** ∧ `CardDef.CostWhens` 里**没有** `Body == seg` |

⛔ **第 4 道闸是防误报的关键，三格各有实据**（少一格就误报一整族）：

- **`HandledByOtherLayer`** —— 事件层 / 天赋 / 伴生 / 开局上手 / 光环 / 静态改战斗规则（实测认领 **25 条**）；
- **`AtTurnClauses`** —— **回合起止从句**：`WillRunOps` 的六个来源里**没有它**，
  由 `EffectResolver.ResolveAtTurn:4792` 直接扫 `u.Card.Desc` 消费 ⇒ 它「无头 + 解得开 + 腿 A = 0」，
  **但确实在跑**（`AM15 Master of Ordnance` / `TAU10 Gun Drone` 那一批，实测 **19 条**）；
- **`CostWhens`** —— **降费触发器**（`Lower cost by 2 every time …`，`TL83 Norn Emissary`）：
  `_costWhens` **不经过 `WillRunOps`**（那个方法只收 `WhenTriggers`/`TriggerTexts`/`SpiritOps`/
  `OathOps`/`AttackedOps`/`AuraSpecs` **六个**来源）⇒ 同样「无头 + 解得开 + 腿 A = 0」，
  **而 `FireCostWhen` 在吃它**（实测 **1 条**）。

**为什么敢判它**：第 4 道闸三格全空 ⇒ **全仓没有任何一层会执行这句**
（不是「查不到」—— 读 `u.Card.Desc` 的地方只有 `ResolveAtTurn:4792`，而它只收回合起止从句）⇒ **计入 rc**。

⚠️ **⑤ 与 ③ 不是两笔账** —— 同一件事的两种粒度（③ 按**卡**、⑤ 按**句**）：
「整张卡空表」的卡（`AM16` / `AM41` / `TAU20` / `TAU25` / `TAU34` / `BL41` / `SW36`）会**同时**出现在两边，
⛔ **别把两个数相加**；而**卡上还有别的 op** 的那一批（🔴 `SW23` / `GSC71` / `GSC36`）**只有 ⑤ 看得见**。

**真池读数（2026-10-10 · 1126 张）**：

```text
    无头段（无 Head、解得开、且腿 A 一条都没收到）共 59 条，落在 57 张卡上：
      · 🔴 无消费点   14 条（12 张卡） ← 计入 rc
      · ⚠️ 回合起止   19 条 · 降费触发器 1 条 · 事件层（WhenTriggers）25 条 ← ⛔ 不计入
```

那 14 条 = 12 张卡，与 `诊断_未归因24张.md` §二那份清单**逐张对得上、不重不漏**：

| 卡 | 卡面那句（无头） | 着落 |
|---|---|---|
| `AM16` `BL41` `TAU25` `TAU34` | `Friendly Infantry / Your Stratagems / Your Drones / Your troops cost 1 less` | 诊断 §二 1–4 |
| `AM41` ×2 | `Draw a Stratagem` · `Lower the cost of Stratagems in your hand by 4` | 诊断 §二 5 |
| `SW36` ×2 | `Give Hunt Mark to a random enemy troop` · `Attack a random enemy with Hunt Mark` | 诊断 §二 6 |
| `TAU20` | `Gains Long Range when an enemy gains Markerlight` | 诊断 §二 7 |
| 🔴 **`SW23` `GSC71` `GSC36`** | `Friendly Beasts / Friendly Vehicles / Your troops cost 1 less` | **正是本档要捞的三张**（诊断 §三·1 明写「它们躲开了清单」） |
| ⚠️ `DA25 Techmarine` | `If it survives, heal 3 to it`（`When …` 那条的**尾句**） | **本档新查出** —— `TriggerBodyAt` 的跨句并接只走**带正文关键词**那条路，`When …` 那条没有 |
| ⚠️ `SOR72 Adelaide the Serene` | `6 ☀ Gain Flank and Shield` | **本档新查出** —— 它是**付费能力段**，但**卡面没印冒号** ⇒ ④「付费段」那档（判据要求 `:`）看不见它 |

**构造性实测（仓库外副本，两趟都验过）**：见 `资料/普查产出_第十四会话/W_willrun无头档.md` §三。

### 四条别推翻的取舍（都是实测撞出来的）

1. 🔴 **只对 `unit` / `hero` 做段级对账**。`tactic` / `defence` 的 `WillRunOps` **就是** `Parse(c.Desc)`
   （整条 desc 一次解析，子效果藏在 `chooseone` 的 `ChooseWhat` 或 `give "…"` 的引号里）
   ⇒ **按「段」对账会产生假警报**：实测 `ASH83`（`Choose one: …`）、`BL4`（`Your Warlord gains "Blast 2 and Slay: …"`）
   两族被逐条误报过。那两类卡只报「整条 desc 解不出 op」那一档。
2. 🔴 **op 的身份【不含 `Source`】**（`WillRun.Sig`）。同一个正文，**不同采集点解析出来 `Source` 不同**：
   `AddTriggerOp:1340` 是 `Parse(body)`（`Source` = 正文那半句）；`CollectSpiritOps:1000` 是 `Parse(Desc)`
   再挑 `CostKind == spirit` 的 op（`Source` = 整句）⇒ 逐字比 `Source` **会把 50 条已经收到的段误报成「收漏」**
   （实测：ASH 那一族灵魂石卡全中招）。⇒ 只比 `verb / amount / payload / target`。
3. 🔴 **抽卡面段之前先过 `CardDef.HandledByOtherLayer`** —— 否则有假警报：`Talent: Deploy Anchors`
   （`TAU45 Stormsurge Battlesuit`）里 `Deploy Anchors` **恰好解得出一条 `deploy` op**，
   而它是**天赋名**、归 `TalentName` 那一层，**根本不是正文**（实测被误报过）。
   同族还有 `Companion N:` / 光环 / 开局上手 —— 引擎自己有「谁接手」的判据，⛔ 别在探针里另写一套。
   ⚠️ **`desc` 与 `keywords` 里同一段正文各印一遍是常态** ⇒ 按「头 + 正文」去重（否则同一个洞数两次）。
4. 🔴 **⑤「无头正文」也只对 `unit` / `hero` 做**（同第 1 条的理由），**而且必须先过第 4 道闸再过账**。
   `tactic` / `defence` 的腿 A **就是** `Parse(c.Desc)` ⇒ 那两类卡的「无头句」早就被腿 A 收到了，
   走这一档只会**重复记账**；而第 4 道闸（尤其 `AtTurnClauses` 那一格）**不加就是整整一族误报**
   （实测：加之前 19 条回合起止从句会全被报成「无消费点」）。

### 两个口径的换算（`A1433` ①）—— 读全池读数的人都会踩

| 口径 | 量什么 | 今天（2026-10-10）的读数 |
|---|---|---|
| **(a) `scan` 的 `unparsed` / `partial` 列** | `EffectText.Parse(desc)` 的原始残渣，**不过滤** | **176 / 1120 行非空**（其中整条 desc 都是残渣的 62 行） |
| **(b) `EffectText.Coverage`** | (a) **先筛掉**「已由别的层接手」的句子（`EffectText.cs:1755` 转调 `CardDef.HandledByOtherLayer`：`When <事件>,` / `Talent:` / `Companion N:` / 光环 / 开局上手） | 四类（`tactic`/`unit`/`hero`/`defence`）的 **部分 0 / 完全不懂 0** |
| **(c) `willrun`** | **执行层**收到没有（腿 A）+ 卡面写着没有（腿 B） | 空表 167 · 收漏 6 · 段级没收下来 13 条 · 🆕 **⑤ 无头/无消费点 14 条** |

🔴 **换算关系**（`A1433` ① 要的就是这一句）：

- **(a) ≠ 缺陷数** —— (a) 那 176 行里绝大多数是**正常**类（整条 `desc` 都是关键词/天赋/`When` 壳）。
  两个口径并排会看着像「176 张卡坏了」。
- **(b) 那四栏全 0 也 ≠ 没问题** —— 它**看不见** `A1386`（正文在 `keywords` 里、`desc` 可能是空的）。
- **(b) 与 (c) 是两条独立的腿** ⇒ **互相看不见对方的洞**，**两条都要看**。

🔴 **`check` 的摘要行原来只打 diff**（`A1433` ②）——「解析差异 0 行」是「**与基线一致**」、
⛔ **不是**「没问题」（一条**早就在**的缺口那里永远绿）。⇒ 现在 `check` 会紧跟一段
**绝对读数**（`全池 N 行 · unparsed 非空 M 行 · partial 非空 K 行`）+ 上面这套换算的提示。

## 基线怎么重建（**重要**）

⛔ **别拿「逐字节相同」当判据** —— 卡名是**会变的产物**（B16/B20 两批共改 6 张卡名），
而 `scan` 的第一列含卡名 ⇒ 拿它当基线，**第一天就红、而且红的是噪声**，会把「红了不用管」训练成习惯。
所以摘要行**把两个数分开**：第 1 列（`id|name`）变 = 改名；第 2 列往后变 = 解析差异。

**确认解析器的改动是对的之后**（用 `RuleEngineTest.Run` 或真机验过），才重建基线：

```bash
cd d:/4/Unity/工具/ruleprobe
dotnet bin/Debug/net8.0/wfprobe.dll scan "基线/out_baseline.txt"
```

🔴 **重生成之前先问一句：这个 diff 是「我改了解析器」还是「探针的**卡名索引**掉了」？**（`A1339`）
基线里**只有 1 行**与卡名索引有关 —— `UM_Angels_of_Death`（全池唯一一张「目标靠**卡名**指」且会被
`scan` 打出来的卡）：索引在 ⇒ 第 4 列（`partial`）为空；索引掉 ⇒ 第 4 列变成
`Codex: Give +1 [Ranged] to your Primaris Intercessor`。**这一行就是那条口径的哨兵** ——
见了它别顺手重生成基线，先去看 `Program.LoadPool` 尾部那句 `CreatePool.BuildNameIndex(list)` 还在不在。
（判据：`CreatePool.cs:613` —— 没建索引时 `MatchCardName` **一律返回 null**，探针就会与引擎**不同口径**，
凡「目标靠卡名指」的句子一律报 `target=[]`。这条已经产出过两次**假证据**。）

🔴 **2026-10-10 `A1348` 起，这一段不再只靠文档** —— `check` 每次会跑一组**与基线无关的哨兵**
（见下节），索引掉没掉它自己会报 `✗`、并且**算进退出码**。所以「顺手重生成基线」这条捷径**堵死了**。

## `check` 的哨兵：与**基线无关**的机器级自证（🆕 `A1348`，2026-10-10）

🔴 **堵的是哪条洞**：`check` 的判据本来是「拿当前输出与**基线**比」，而**基线同一个工具就能重写**
（`scan 基线/out_baseline.txt` 一条命令就覆盖）。⇒ 谁红了顺手重跑一次 `scan`，**真回归被静默抹平**、
界面还显示全绿。文档级防呆（上一节那段）**拦不住一个正在乱敲命令的人**。

⇒ 做法：把几条判据**写死在源码里**（`scan` 重写得掉基线文件、重写不掉 `.cs`），每次 `check` 现算一遍。
判据读的是**当次现算的 dump**、⛔ 不看基线 ⇒ **重生成基线抹不掉它们**。

| 哨兵 | 判据（期望值的**原版出处**） |
|---|---|
| 索引（库函数） | `CreatePool.MatchCardName("Primaris Intercessor")` 非 null —— `CreatePool.cs:613`「没建索引一律 null」 |
| 索引（`scan` 那条路） | `UM_Angels_of_Death` 那行的 `partial` 列为空 —— 索引掉时它会变成 `Codex: Give +1 [Ranged] to your Primaris Intercessor` |
| 货币名 | `SOR72` 付费那条 op = `cost=6` `ck=faith` —— 卡图 `Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png` 上是**金太阳**（`☀`=信仰，`EffectText.cs:7802-7812`） |

退出码：**基线绿但哨兵红 ⇒ 仍然 `rc=1`**（⛔ 不许出现「哨兵红、界面全绿」）。

**构造性实测（2026-10-10，两次都验过）**：

| 变异（都在**仓库外**的副本上做） | 拿旧基线比 | **重生成基线之后**再比 |
|---|---|---|
| `SOR72` 付费前缀 `☀` → `Energy`（= 倒放 `A1357`） | 解析差异 1 行 · 哨兵 1 条红 · `rc=1` | 解析差异 **0** 行 · **哨兵仍 1 条红 · `rc=1`** ✅ |
| `Program.LoadPool` 尾部 `BuildNameIndex` 删掉（= 重演 `A1339`） | 解析差异 1 行 · 哨兵 2 条红 · `rc=1` | 解析差异 **0** 行 · **哨兵仍 2 条红 · `rc=1`** ✅ |

（右列就是 `A1348` 这条账的验收：**基线可以被重写，哨兵不能被重写**。）

⚠️ **哨兵集是【有意的极小集】** —— 一条会经常「合理地」变红的哨兵，会把「红了不用管」训练成习惯。
**要加之前先问：它的期望值有原版出处吗？** ⛔ 不许拿我们自己的 dump 反填（那是「拿我们的实现
证明我们的实现」）。⚠️ 哨兵卡**按 `id` 找、⛔ 不按卡名**（卡名已改过 6 张，会漂）。

### 卡名索引：探针必须与引擎同口径（`A1339`，2026-10-10）

引擎侧 `Data/CardDatabase.cs:113` 在读完卡表之后调 `CreatePool.BuildNameIndex(list)`；
探针**原来两处都不建** ⇒ `card`/`seg` 报 `target=[]`、`scan`/`check` 落 `partial` 列。现在：

- `card` / `filter` / `jackal` / `b19test` … —— 由 `Program.LoadPool()` 尾部建（**唯一**卡池入口）。
- `seg`（走 `Dump`）—— 同上，`Dump` 头部调了一次 `LoadPool()`。
- `scan` / `check` —— ⚠️ **这两条【不走 `LoadPool`】**（`Scan.ScanMain` 自己再读一遍 JSON）
  ⇒ 它**自己**调了一次 `Program.LoadPool()`。
- 🆕 **`BattleProbe.cs` 那三处 `ParseSegment`（`A1349`）** —— 走 `Program.ParseSeg(text)` 这道**闸**
  （闸里先 `LoadPool` 再解析）。⚠️ 它们**原来直调** `EffectText.ParseSegment`，是同一族的**第四种**入口。

🔴 **往这条链上加新入口时，问一句「它过不过 `LoadPool`」** —— 不过的（像 `Scan` 那样自己读 JSON 的）
必须自己补一次，否则就是**只补一处 = 静默偏一半**。

**判据（`A1349`）**：`grep -rn "EffectText\.ParseSegment(" 工具/ruleprobe/` ⇒ 只该命中 `Program.cs`
里**闸内那一行**。⚠️ 那三处**今天句子里没有卡名 ⇒ 今天无影响**，但**塞一句带卡名的进去就会翻车** ——
**构造性实测（2026-10-10）**（仓库外副本，加一个 `segcard` 临时模式、全程不碰别的装池点）：

```text
句子 = Give +1 [Ranged] to your Primaris Intercessor
过闸（Program.ParseSeg）          → verb=give target=[your primaris intercessor]   ✅
直调（EffectText.ParseSegment）   → verb=give target=[]                            🔴 A1339 那条假象
```

## 桩够不够（2026-10-18 实测口径）

**真实可编译的 UnityEngine 面 = 4 个成员**（`Debug.Log` / `LogWarning` / `LogError` + 类本身，11 个调用点）。
`UnityEngine.Random` 有 10 处引用但**全是注释**（代码刻意走 `ctx.Rng`）。

🔴 **2026-10-18 更新（`A941` 之后，这条已闭环）**：Core 里**已经没有任何 Unity 类型**了 ——
原来 `Resources` / `TextAsset` / `JsonUtility` 那三处（在 `TutorialScript.cs` 的 `#if UNITY_5_3_OR_NEWER` 里）
已整段搬进 **`Data/TutorialDatabase.cs`**，换乘点是一个「**半个 `partial` 方法**」：
`Core/TutorialScript.cs` 只写 `static partial void LoadFromUnity();`（**无实现**）⇒
**C# 会把「只有声明的 `partial void`」连同调用点一起抹掉** ⇒ 探针侧自动落回兜底、Unity 侧真装载。
⇒ **两边都能验，而且不再靠 `#if`**。`Editor/RuleEngineTest.cs` 的 `TestCoreLayerPurity` 现在**守着这条边界**。

🔴 **但 `.csproj` 里仍然别加 `<DefineConstants>UNITY_5_3_OR_NEWER</DefineConstants>`** ——
定义它今天已不再有影响，可**探针要的就是「Core 能单独编」这条边界**；定义符号会让这条边界悄悄失效
（下次有人往 Core 里加 Unity 调用时就不会被发现）。

## 三个坑（收编时全部修掉了，别再踩回去）

| 坑 | 症状 | 修法 |
|---|---|---|
| `scan` 的 `unparsed` 列打成类型名 | `.Append(List<string>)` 命中 `Append(object)` ⇒ 每张卡都是 `System.Collections.Generic.List\`1[System.String]`，**「没解析出来的碎片」这列被整个丢掉** | `string.Join(",", unparsed)` |
| 中文输出 GBK | 本机默认编码 ⇒ 所有中文乱码、摘要行读不了 | `Console.OutputEncoding = UTF8` |
| 退出码恒 0 | `Main` 是 `void`、无 `Environment.Exit` ⇒ **FAIL 45 条也退 0** | `Main` 改 `int` + `return` |

## 来源与边界

- **收编自** `D:/tmp/wf_b14_probe/`（2026-10-18；那份住临时目录、**迟早被清**）。同族探针在 `D:/tmp/` 下曾有 **18 个**（每批重造一个）—— 这个就是用来止住那件事的。
- 卡池路径**由仓库根推出来**（`Program.ResolveOutDir()` 从程序集往上找 `MyGame/Assets`），⛔ 不写死盘符。
- `_专题/seg_any_1016.cs.txt` = 更早那份 `_tmp_view/seg_any_probe/`（`GivePayload.ParseInto` 的 `any` 语义影响面），
  ⚠️ **它是【一次性专题】，不是通用盘**；`_tmp_view/` 在 `.gitignore:154` 里被忽略 ⇒ 搬过来**先保命**。
