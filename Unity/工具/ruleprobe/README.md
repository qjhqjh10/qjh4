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
bash d:/4/Unity/工具/ruleprobe.sh play SOR6  # 🆕 逐卡真打：这张卡打出去，局面的数字动了没有（见下节）
bash d:/4/Unity/工具/ruleprobe.sh play all dump /tmp/play.tsv   # 全池逐卡真打
```

判据 = **看那行 `=== 全池 … 解析差异 N 行 … ===`**：

| 摘要 | 含义 | 绿红 |
|---|---|---|
| `解析差异 0 行` | 解析器没动 | ✅ 绿 |
| `改名 K 行`（解析差异 0） | 本工程每批都在改卡名 / 重跑产物 —— **通常无害** | 🟡 黄，看一眼 |
| `解析差异 > 0` | 引擎解析器真变了 | 🔴 红，**逐行看**（是修基线还是修解析器） |
| 🆕 `哨兵合计：失败 N 条`（`N>0`） | **与基线无关**的自证失败了（`A1348`）—— ⛔ **重生成基线消不掉它**，必须去看那句 `✗` | 🔴 红 |

⚠️ **退出码把两者合并**：基线绿但哨兵红 ⇒ 仍然 `rc=1`（⛔ 不许出现「哨兵红、界面全绿」）。

🔴 **构建失败会【出声并停】，不再往下跑**（`A1466`，2026-10-11 修）：

```text
!! 构建失败（dotnet build rc=1）—— **停下**：⛔ 不用旧 dll 接着跑（那会给出假绿读数）。
   台账出处 = `资料/任务_卡组卡牌战斗.md` 的 `A1466`。
   构建输出（末 40 行）：…
```

脚本现在拿 **`dotnet build` 的退出码**判成败（非 0 ⇒ 打印错误落点 ＋ `exit 1`），
⛔ 不再「**dll 在就接着跑**」。
**为什么必须这样**：`ruleprobe` 是**所有引擎改动的验收工具**（`A1369`/`A1435`/`A1439`/`A1447`/`A1452` … 全靠它出读数），
而**构建会被别人半成品的源码打断**（实测一次 `CS1525`，来源 = 当时另一个写手正在写的 `工具/ruleprobe/WillRun.cs`）
—— 旧版脚本那时会拿**上一次的 dll** 跑，照样打出「**解析差异 0 行**」这种**看起来全绿**的读数，
而读数会被当成证据往台账上写 ⇒ **假绿比没有读数更糟**。
⚠️ 见到 `!! 构建失败` 时**先看 error 落在哪个文件**：若是**别人正在写的**（本工程多写手并行），
那不是你的问题 —— 但**本次读数一律不可信**，等构建通过再跑。

```bash
# 手搓一条语句看解析结果
dotnet bin/Debug/net8.0/wfprobe.dll seg "<卡面英文原文>"
# 看某张卡
dotnet bin/Debug/net8.0/wfprobe.dll card "<卡名>"
# 看某张卡的 `choosecard` 会滤出哪些候选
dotnet bin/Debug/net8.0/wfprobe.dll filter "<卡名>"
# 生成一份全池 dump（基线就是这么来的）
dotnet bin/Debug/net8.0/wfprobe.dll scan "基线/out_baseline.txt"
```

⚠️ **`filter` 现在对「没有 `choosecard` op」也会出声**（`A1383`，2026-10-11 修）——
原来 `filter "Primaris Intercessor"` **一行都不打**（看着像「命令根本没跑」），
而现在三种情形都能分开读：① **本来就没 op**（`desc` 为空 ⇒ `!! …`）·
② **有 ask op 但不是 `choosecard`**（`-- … chooseone/chooseeffect ⇒ 本子命令没有候选池可滤`，
⚠️ 这是原来**第二条静默路**：`PlayerChooseOps` 会返回三种动词，本子命令只处理第一种）·
③ 正常打候选。⚠️ 卡名**不是唯一键**（全池 3 个重名）⇒ 现在**同名全部列出**。

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
| 🆕 **目标块** `/{T:…}` | `EffectOp.Target`（整个 `EffectTargetSpec`） | `{T:Raw=all enemies\|Side=enemy\|Kind=any\|Count=0\|…}`；`Target == null` ⇒ `{T:null}`。`EffectOp.DeathWatchTarget` 非空时再跟一个 `/{DW:…}` 🆕 `A1477` |

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

🔴 **`A1477` 为什么还要补【目标块】**（同一族、第三条缝，2026-10-11）：

`dump` 原来**一个字都不打 `Target`** —— 而 `A1467` 改的**正是「目标怎么解」**
（`All enemies lose …` 句首那个 `All` 被正则吃掉 ⇒ `T=[enemies]…n=1` 变成 `T=[all enemies]…n=0`）
⇒ **`check` 一行都没多**、「解析差异 0 行」照旧 ⇒ **凡改目标解析的笔都会静默过关**，
`A1467` 写手只能**自建**一份带 `Target.*` 的签名 dump 才拿到读数（`D:/tmp/wf_a1467/sig_desc_{before,after}.txt`）。
⚠️ 与 `A1459`（`willrun` 补「无头」档）、`A1466`（`ruleprobe.sh` 拿旧 dll 跑）**同一条链**
——「**探针看不见哪一类改动**」这件事本身还有缝。

**读法**：块里是 `名称=值`，用 `|` 分隔；`-` = `null`（⚠️ 与**空串**不同 —— `PrevSide` 那类
「判不出就留 `null`」的字段，`-` 才是「不知道」）；`bool` 打成 `1`/`0`；枚举打名字（`Anchor=Unset`）。
打哪 28 个字段 = `Program.TargetCols`（顺序写死，基线才稳）。

🔴 **清单是「手写 ＋ 机器级守卫」**，⛔ 别只改一边：`EffectTargetSpec` 是**别人在改**的类
（`A1311` 刚加过 `PrevSide`/`ResolvedSide`），新字段**不会自己长进清单**
⇒ `check` 的哨兵 ④（`Program.TargetColsDrift()`）**每次现算**，对不上就打 `✗` ＋ 算进退出码。

**构造性实测（2026-10-11 · `A1477`）**：仓库外一份**池副本**，把 `AM48 Recon Operation` 的 desc
句首 `All` 去掉（= **倒放 `A1467` 的观测**）⇒ **摘掉目标块（= 旧格式）A/B 零差异（旧 `check` 完全瞎）**；
**新格式 1 行差异**（那行里 `Raw=all enemies|Count=0` → `Raw=enemies|Count=1`），
端到端 `check` 报 `解析差异 1 行 · ⚠️ 解析差异（前 1 张）：AM48|Recon Operation` + **`rc=1`** ✅

⚠️ **`countRef` / `condition` / 目标块都原样打、不做转义**：`countRef` 的 `board` 族取值自带两个 `|`
（`enemy|unit|all`）—— 全池 **27 个 op** 的 `countRef` 带 `|`（落在 **25 行**上）；
目标块里 `SubtypeFilter` 也会自带 `|`（实测 2 个 op：`Infantry|Beast` / `Battlesuit|Vehicle`）。
`condition` 实测**全池不带** `|` / `;` / `/`（已逐 op 数过），但**将来可能有**。
⛔ **别据此判「格式坏了」**：`chooseone` 的 `chooseWhat` 早就这样（旧基线那 19 行就是它），
而 `check` 只认**前两个** `|`（`KeyOf`/`DefOf`，`Program.cs` 的 `CheckMain`）⇒ 不受影响。
⛔ **也不许把 `|` 换成 `,`** —— 那会让 dump 的值与引擎字段**不同**，下一个人照 dump 抄进断言就抄错了
（正是本笔要治的那一族）。渲染只有一处：`Program.CountRefText` / `Program.TargetText`（`Program.cs`）。

⚠️ **取尾部列必须【从后往前】数，而且数之前要先摘掉目标块** ——
`payload` 里**可以**含 `/`（实测 `ASH65` 的 `<i>shuriken</i> 2` 就是，那条 op 有 **14** 个 `/` 字段，
别的都是 13）⇒ 从前往后数下标会**静默拿错列**；而目标块里是**卡面原文**（`Raw`），
它**自己也能含 `/`** ⇒ 尾部又多 28 个字段，不摘的话「从后往前数」同样会**静默拿错列**。
`Sentinel.Legacy(op)` 按 `/{T:` 把 legacy 段切出来，`Tail` 再用 `f[f.Length - fromEnd]`。

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

## `play` —— 「这张卡打出去，局面的**数字**动了没有」（🆕 `A1431`，2026-10-11）

```bash
bash d:/4/Unity/工具/ruleprobe.sh play SOR6                 # 单卡读数
bash d:/4/Unity/工具/ruleprobe.sh play all                  # 全池 sweep（≈3 秒）
bash d:/4/Unity/工具/ruleprobe.sh play all dump <out.tsv>   # 附带逐卡 TSV（1126 行）
```

### 它修掉的是哪一种「结构性失明」

`A1431` 的判据原文 → `资料/普查产出_第十四会话/逐卡效果检查_方法与边界.md` §1 / §2 / §4。
那一趟盘清了：**本仓 9 类检查手段**（`check`/`scan`/`seg`/`card`/`b19test`/`jackal` ·
`RuleEngineTest.Run` 的四份 `_tmp_view` 报表 · `Coverage`+`WillRunOps`+`UnimplementedKeywords` ·
`zh_crosscheck.py` · `check_pnp_cards.py` · `gen_icon_plan.py`）**全部只回答「这句英文今天解析成什么样」**，
**没有一条回答「这张卡打出去数字对不对」** ⇒ 「**解析过、结算错**」那一整类
（`A1388`/`A1407`/`A1426`/`A1428` 那一族）**任何全池尺子都照不到**：
`check` 报「解析差异 0 行」、`OpHasMechanism` 全通、`Coverage` 四栏全 0。

⚠️ 与 `willrun`（`A1432`）的分工：那条量「**卡面正文收到没有**」（注册层）；
本条量「**收到之后打出去，局面的数字动了没有**」（结算层）。**两条腿互相看不见对方的洞。**

### 夹具（**全池同一份 ⇒ 读数能横向比**）

| 项 | 值 |
|---|---|
| 双方督军 | 各 1（P0 的督军 = 夹具 `FIXWarlord`；**被测卡本身是 hero 时另走一档**） |
| 双方部队 | 各 3 张固定卡：我方 `FIX_A` 步 2/4 · `FIX_B` **载具** 3/5 · `FIX_C` **兽** 1/4；敌方 `FIX_E1/E2/E3` 步/载具/兽 各 6 血 |
| 落格 | **3 / 6 / 7**（贴着督军格 4、且有相邻对 6-7） |
| 手牌 | 4 张 = 3 张摆场部队 + **被测那一张**（`startingHand=4`，⛔ 其余全照 `GameplayVariables.Classic`） |
| 垫牌 | 30 张：**兵种轮着来**（步/载具/兽/无人机）＋ 每 5 张有一张**战术卡** —— `Draw a Beast` / `The next Vehicle` / `Draw a Stratagem` 那族才找得到东西 |
| 能量 | 起始 **20** / 上限 **30**（上限比起始高 ⇒ `Refill all your Energy` 看得见；全池最高卡费现读 14） |
| 预扣血 | 摆完场后**我方三张部队 ＋ 督军**各扣 **2**（走引擎 `RuleCore.ApplyDamage`）⇒ `Heal …` 那族才看得见 |
| 补手牌 | 🔴 被测卡**没活到这一手**时按引擎形状补进手牌（`NewInstance`→`Hand.Add`→`SetupCardInHand`，≡ `RuleCore.SetDefensiveCard`）。**不补会漏掉 137 张**：**临时卡 98 张**（`EndTurn:1340` 的 `SweepEphemeral` 会在回合末把手牌里的临时卡弃掉 —— 这是引擎**对的**行为）＋ **防御卡 39 张**（`BuildPlayer` 把它从牌库分流出去、只发给后手） |
| 随机性 | `seed=0`、`shuffle:false`、换牌关 ⇒ 同一张卡每次读数逐字一致 |

**量的是什么**：把被测卡**真打一发**（单位卡 `PlayCard`、战术/防御卡 `PlayTactic`），
比打前/打后的**局面指纹** —— 能 · **棋盘每格** `攻/远/甲/血 {关键词}` · 手牌/牌库/弃牌堆**逐张 id** ·
**手牌总价**（逐张走 `CostOf`，让「手牌减费」那族看得见）· 伤害累计 · 出兵数。

**变化逐条**按四类分开（🔴 这是 `other` 这一列能用的前提）：

| 类 | 例 | 算不算「实得」 |
|---|---|---|
| 付费 | `⚙ 能量 P0 20 → 14（-6）`，**恰好等于照卡面应付** | 否 |
| 出牌本身 | `✋ 手牌 P0 -「SOR6」×1` · `🗑 弃牌 P0 +「SOR6」×1` · `🎖 出兵数` · 手牌总价减去本牌价 | 否 |
| 自身落场 | `✚ 落场/生成 P0[2] . → SOR23 2/5/0/4` | 否 |
| **其它** | 其余**全部** | ✅ **是 —— 这一格就是「有没有实得」** |

⚠️ **「照卡面应付」≠ 卡的 `cost`**：卡面上 `N Energy:` 那种**付费能力前缀**是**另付**的
（`EffectOp.Cost > 0 && !CostShared` 那几条；实测 `SOR6` = 卡费 2 ＋ 前缀 4 = **6**）。

### 督军档（`hero`）—— 没有「打出去」这个动作

`PlayCard` / `PlayTactic` 只处理手牌里的非督军卡 ⇒ 督军**只能另法测**：
「**在场 + 一个来回**」（P0 结束 → P1 开始+结束 → P0 开始）的实得变化，
**减掉一局【空督军对照】**（同种子、同摆场，只把 P0 督军换成 `FIXWarlord`）；
督军自己那一格（`攻/远/甲/血 {关键词}`）再**单独与对照局比**（它在同一局里从头到尾都在场 ⇒ 永远不会出现在 diff 里）。
⛔ 这不是「我们挑的做法」，是督军**确实**只能这样测 —— 如实打在读数里。

### 候选清单**分档**（🔴 不分档就是一屏噪声）

`other == 0` 这个判据**本身不区分**「该发生而没发生」与「**本夹具产生不了那个事件**」。
实测反例：`AM12 Autocannon Squad` 的 `Regiment: Gain +1 [Ranged]` —— `Regiment`（团）
**不是部署时触发**，是**攻击时**触发（`RuleCore.cs:4027-4031`；消费点
`FireTriggerAt(ctx, attacker, KeywordTable.Regiment, p, atkSlot)`）⇒ 本夹具不让单位攻击
⇒ **它不该有变化**，报成候选就是**误报**。⇒ 分三档（判据**全部转调引擎公开面**，⛔ 探针不另写关键词表）：

| 档 | 判据 | `other == 0` 意味着 |
|---|---|---|
| **A 打出即结算** | `type ∈ {tactic, defence}`（这两类的 `WillRunOps` **就是** `Parse(Desc)`） | 🔴 **最可疑** —— 打出**就是**触发，「等事件」解释不通 |
| **C 部署时该发生** | `unit`，卡面里存在**不带触发头**的正文段（`Head:` 缺失），或头是 `rally` | 🔴 该查 —— 出牌那一拍就该有变化 |
| **B 等事件/等主动用** | `unit`，每一句都挂在**别的时机**的头上（`Strike`/`Regiment`/`Duty`/`Agenda`/`When …` …） | ⚪ **预期就是这样**，⛔ 不是缺陷 |
| H 督军档 | `type == "hero"` | 另法测（见上） |

### 判据 / 退出码

- `play all`：**抛异常 = 0 张 ⇒ rc=0**，否则 `rc=1`。
  「候选」与「打不出去」**不计入 rc** —— 它们要**对着卡面**逐张判，本工具不代替判决
  （⛔ 但也不静默：两栏都逐条打了出来）。
- `play <id>`：崩了 / `ErrUnimplemented`（卡面上写着正文、`CanPlayTactic` 就把它挡回去）⇒ `rc=1`。
  ⚠️ `ErrNoTargetAvailable`（`A985⑧` 拆出来的那一档）**不算** —— 那是**夹具里没有合法目标**，
  是**量具的限制**，不是卡的缺陷。

### 真池读数（2026-10-11 首跑 · 1126 张 · 不跑 Unity）

```text
=== play all · 逐卡真打 · 池 1126 张 ===        （整趟 ≈ 5 秒）
  真打成功（rc=0）        1115 张
  打不出去                11 张（分因：ErrNoTargetAvailable 11）
  引擎抛异常              0 张
  督军档（另法测）        56 张
  --- ② 候选（卡面写着正文 · 打得出去 · 局面一个数字都没动 = 411 张）---
     ── A 打出即结算 · 41 张  ← 🔴 这一档最可疑
     ── C 部署时该发生 · 96 张
     ── B 等事件/等主动用 · 274 张
```

🔴 **这 411 之前是【看不见的】** —— 同一张卡在旧的三条尺子上都是绿的：

| 尺子 | 对 `BL68 Black Crusade`（A 档候选）的读数 | 能不能看出它没结算 |
|---|---|---|
| `check`（与基线比） | 那行**逐字节没变**（卡名/desc 都改过 ⇒ 不在差异里） | ❌ 看不见 |
| `willrun`（正文收到没有） | 不在「收漏 / 付费段 / 无消费点」任何一栏里 | ❌ 看不见 |
| `Coverage` / `OpHasMechanism` | 完全解析 · 载荷有机制 | ❌ 看不见 |
| 🆕 **`play BL68`** | `实得变化 = 0 条`，而引擎自己的日志写着：<br>`战术卡：「Deploy 4 random troops from your deck」部署不了 —— 按兵种「troops」在卡池里一张都没筛到（**这条没生效**）` | ✅ **看见了** |

⇒ 这就是 `A1431` 那句「**全池尺子对『解析过、结算错』这一类结构性失明**」的正面回答。

### ⛔ 它**不能**回答什么（如实标着，别拿它当万金油）

1. **对错** —— 判据在**卡面**上（`desc`/`descZh`/PnP 成品卡图），本工具只摆**实得数字**。
   ⛔ 绝不用我们自己的实现反填期望值（那就是「拿我们的实现证明我们的实现」）。
2. **档 B 那一族**（`Strike:`/`Slay:`/`Duty:`/`When …`/`Teleport:`/`Ambush:`/`Survivor:` …）——
   要测它们得另写「打一发 / 让它死 / 让它抽到」那种**专属夹具**，本夹具产生不了那些事件。
3. **只改后续回合的**（`For the rest of this battle, at the start of your turn …`）·
   **要特定关键词/兵种的**（场上三张是 步/载具/兽、**都无关键词**；垫牌另有无人机与战术卡）·
   **要靠跨句承接的**（`A1428` 那族）—— 都会落进候选，但**是量具盲区，不是证据**。
4. **费用之外的手牌外区域**（牌库顶顺序那类）只按 id 多重集比 ⇒ 只看得出「换了哪几张」。

## 基线怎么重建（**重要**）

⛔ **别拿「逐字节相同」当判据** —— 卡名是**会变的产物**（B16/B20 两批共改 6 张卡名），
而 `scan` 的第一列含卡名 ⇒ 拿它当基线，**第一天就红、而且红的是噪声**，会把「红了不用管」训练成习惯。
所以摘要行**把两个数分开**：第 1 列（`id|name`）变 = 改名；第 2 列往后变 = 解析差异。

**确认解析器的改动是对的之后**（用 `RuleEngineTest.Run` 或真机验过），才重建基线：

```bash
cd d:/4/Unity/工具/ruleprobe
dotnet bin/Debug/net8.0/wfprobe.dll scan "基线/out_baseline.txt"
```

⚠️ **`check` 不带参数也走得通了**（`A1380`，2026-10-11 修）：缺省基线原来写的是「程序集目录往上**两级**」
（`…/bin/Debug/net8.0` 两级只上到 `bin`）⇒ 算出来是 **`…/ruleprobe/bin/基线/out_baseline.txt`**（不存在）
⇒ 直接敲 dll 一律 `!! 没有基线 …` · `rc=2`（日常躲过它只因 `ruleprobe.sh` **总是显式传**路径）。
现在缺省值是**按名字往上找**出来的：先找 `基线/out_baseline.txt` 文件本身，再找 `基线/` 目录，
都没有才回落到「按规则算的那一条」——**报错信息也指到该在的位置**（⛔ 不静默）。
⚠️ 台账原文说这条「少两级」，实测**差一级**（`net8.0 → Debug → bin → ruleprobe` 是**三级**）。

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
| 🆕 **目标列**（`A1477`） | `Program.TargetCols`（dump 目标块打哪 28 个字段）与 `EffectTargetSpec` 的公开实例成员**一致** —— 对不上就说明「**类里加了字段、dump 没跟上**」= `A1477` 那条缝又开了（或清单过期、那几列恒打 `?`）。判据在**源码**里，重生成基线抹不掉 |
| 🆕 **基线指纹**（`A1382`，2026-10-11） | **卡池 md5 与基线 md5 配对**：`现算卡池 md5 == PinnedPoolMd5`（输入没变）**且** `现算基线 md5 != PinnedBaselineMd5`（基线被改写）⇒ 🔴 出声并计入 rc。参照物 = **卡池文件**（作者是 `工具/gen_cards_engine.py`，本探针**只读**）—— 这就是 `A1382` 要的「**不在本工具写权限内的参照物**」 |

### 🔴 `A1382`：`check` 的「真回归 ＋ 重生成基线 ⇒ 静默变绿」（2026-10-11 修）

**病灶**：`check` 的判据是「拿当前输出与**基线**比」，而**基线同一个工具就能重写**
（`scan 基线/out_baseline.txt` 一条命令就覆盖它）⇒ 谁红了顺手重跑一次 `scan`，
真回归被**静默抹平**、界面还显示全绿。`A1348` 那组哨兵只封了「**已知的**几类回归」，**一般情形没封死**。

**为什么必须有【本工具写不动】的参照物**：哨兵写在同一份源码里、基线写在同一个目录里，
两者**都在本工具的写权限内** ⇒ 光靠「两者互相印证」是**自证**（本仓红线）。
⇒ 参照物取**卡池文件**（`cards_engine.json`）—— 它的作者是**另一个工具**，本探针只读。

```text
✗ [基线指纹] 🔴 **卡池没动、而基线文件被改写了** —— 这正是 `A1382` 那条洞：
            卡池 md5=…（与钉住的一致 ⇒ **解析器的输入没变**） · 基线 md5=…（钉住的是 …）
            ⇒ 先看 `git diff 工具/ruleprobe/基线/out_baseline.txt`：
               · **有意**重生成（解析器真改对、已验过）⇒ 把 `Program.PinnedBaselineMd5` 改成上面的现值；
               · **为了把红抹平** ⇒ 那是真回归，去修解析器（⛔ 别改这个常量）。
```

🔴 **重钉只能【手改源码】**（`Program.PinnedPoolMd5` / `PinnedBaselineMd5`）—— ⛔ **没有**
「一条命令重钉」的口子，那等于把这条洞又开回来。**改源码是显式动作、会出现在 `git diff` 里** ——
这正是本哨兵要的「**让静默动作变响**」。
⚠️ 卡池 md5 对不上时**只打印、不算失败**（卡池本身变了 ⇒ 两个指纹**都该重钉**；
⛔ 不在这里判「卡池变没变坏」—— 那是别的工具的账）。
⚠️ **已知的假阴**：真回归 ＋ 重生成基线 ＋ **卡池也在同一批里变了** ⇒ 本哨兵**看不见**。如实标着。

**构造性实测（2026-10-11）**：拿一份**仓库外**的基线副本、**只改第一行一个字节**
（= 模拟「重生成基线抹平一次真回归」）⇒ `check` 报 **哨兵 ⑤ 红 1 条 · `rc=1`** ✅
（卡池那一半仍报「与钉住的一致」）。

退出码：**基线绿但哨兵红 ⇒ 仍然 `rc=1`**（⛔ 不许出现「哨兵红、界面全绿」）。

**构造性实测（2026-10-10，两次都验过）**：

| 变异（都在**仓库外**的副本上做） | 拿旧基线比 | **重生成基线之后**再比 |
|---|---|---|
| `SOR72` 付费前缀 `☀` → `Energy`（= 倒放 `A1357`） | 解析差异 1 行 · 哨兵 1 条红 · `rc=1` | 解析差异 **0** 行 · **哨兵仍 1 条红 · `rc=1`** ✅ |
| `Program.LoadPool` 尾部 `BuildNameIndex` 删掉（= 重演 `A1339`） | 解析差异 1 行 · 哨兵 2 条红 · `rc=1` | 解析差异 **0** 行 · **哨兵仍 2 条红 · `rc=1`** ✅ |
| 🆕 从 `Program.TargetCols` 里删掉 `PickMost`（模拟「类里加了字段、清单没跟上」· `A1477`） | —— | 解析差异 691 行 · **哨兵 ④ 红 1 条 · `rc=1`** ✅（那句 `✗` 把缺的名字点出来了） |

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
