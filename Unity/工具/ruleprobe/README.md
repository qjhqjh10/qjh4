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
bash d:/4/Unity/工具/ruleprobe.sh play all pf /tmp/play.pf.txt  # 🆕 写结算指纹基线（`A1431` 第二笔）
bash d:/4/Unity/工具/ruleprobe.sh play check                    # 🆕 结算层对拍（逐卡差异 + 哨兵）
bash d:/4/Unity/工具/ruleprobe.sh faith      # 🆕 信仰付费段的【两把尺子】对账 + 真打（见下节）
bash d:/4/Unity/工具/ruleprobe.sh nonkw      # 🆕 `non <关键词>` 的目标筛选专用探针（见下节）
```

判据 = **看那行 `=== 全池 … 解析差异 N 行 … ===`**：
⚠️ 🆕 **2026-10-12（`A1382`）：它只说了一半** —— 除「与基线比」之外还有一组
**与基线无关的哨兵**（`=== 哨兵合计：失败 N 条 ===`），**那 N 条也计入 rc**
⇒ `解析差异 0 行` **而** `哨兵合计 > 0` = **仍然红**（⛔ 别只看前一行）。

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
| 🆕 **第二目标块** `/{T2:…}` | `EffectOp.Target2`（同一个类） | 同上那份字段，`Target2 == null` 时**整块不打**（⛔ 不是「打空块」） 🆕 `A1462` |

🔴 **`A1462` 为什么还要补【第二目标块】**（同一族、第四条缝，2026-10-11）：

`forceattack`（强制攻击）那一族把**「谁去打」与「打谁」分成两栏**：`Target` = 攻击者（**可空**）、
`Target2` = 被打的那个。而 dump 原来只打 `Target`。症状是最坏的一种 —— **看起来像「没有目标」**：
`SW12 Hunting Wolf` 卡面 `Ferocity: Attack a random non Flying enemy troop` 在 dump 里
**只**显示 `T={T:null}`（`Target` 真的是 null），`Target2` 一个字都不打。
`A1462`（「`non <关键词>` 的目标筛选**疑似整个丢了**」）当时**判不了**，原因就是这条缝；
同一天 `A1477` 补的只是 `Target` 那一栏。

⚠️ **块名必须是 `/{T2:`**（`T` 后面那一颗是 `2`、不是 `:`）—— `Sentinel.Legacy` 靠
`LastIndexOf("/{T:")` 找起点，两者**不互相包含** ⇒ 切点不会挪。
⚠️ **只在非空时打**（与 `/{DW:` 同一条取舍）。**自证判据**（⛔ 别写成「旧行是新行的前缀」——
块是插在 op 分隔符 `;` **之前**的，`;` 会往后挪）：**把新行里的 `/{T2:…}` 块删掉之后必须与旧行逐字节相同**。
本轮实测：扫描前后 **8 行**变化（全是 `forceattack` 那一族），**8/8** 满足；其余 1112 行逐字节没变。

⚠️ **`seg` / `card` 那两个入口还会多打一段【嵌套 op】**（`NestedText`，同一个 `A1462`）——
`atturn`（`At the start of each turn …`）把正文解析在 `AtTurnOps` 里，顶层 op 的 `Target`/`Target2`
**都是空的**，只看顶层那两栏会得出「这句话没有目标」的错结论（`GOF85` 就是这种）。
`scan` / `check` 那条路**不打**嵌套（怕基线噪声）—— 要看嵌套用 `nonkw` 或 `seg`。

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
| **`付费段`** | **付费能力段**（判据 = **引擎自己那次 `ParseSegment` 的 `PaidCost > 0`**，覆盖 `N <货币>:` **与** `Oath N:` 两种写法）—— **它自己那条通道**没收到它 | **是**（⚠️ 另一族病灶：**覆盖面**，不是「表里缺词」） |
| **`同头两写`** | 同一个头**注册过**，但 `desc` 与 `keywords` 两份正文**不一样**、后者被「先到先得」丢掉 | 否（⚠️ **数据不一致**，另行处置） |

🔴 **`A1593`（2026-10-11）：段级对账那道「腿 A 收没收到」的闸已改成【按通道比】**
（`WillRun.ClaimChannel`）—— 原来是「**全卡** `op` 签名并集里有没有同签名的」（`Sig()` **不带**
「属于哪条 ability」那一维）⇒ 同一张卡**别的** ability 里出现过的同签名 `op` 会把这**一段**
顶成「已收到」。**影响所有拿 ④ 当射程的账**，不止信仰。两条规则（判据全转调引擎公开面）：

| 这一段长什么样 | 认它的**通道** |
|---|---|
| **自带付费前缀**（`PaidCost > 0`） | 只认同货币那条：`faith` → `CardDef.FaithOps` · `spirit` → `CardDef.SpiritOps` · `oath` → `CardDef.OathOps`；**别的货币（`energy` / `might` …）在 `WillRunOps` 的 unit/hero 支里没有通道 ⇒ 这一段收不到**（④ 该点的名） |
| 其余 | 只认**本段那个触发头自己的通道**（`TriggerTexts[头]` → `TriggerOps(头)`）；头不在字典里 ⇒ 没有通道 |

⚠️ **无头段（⑤）【不】走这条规则**（没有头 ⇒ 没有通道，按通道比会让 ⑤ **整族误报**）——
它继续用全卡并集，**只是下界**。⇒ **⑤ 与 ④ 的严格程度不同**，⛔ 别把两者的条数放一起比。

**判据**：**② 收漏了 = 0 张**、且段级 `收漏` / `付费段` 都为 0、**且 ⑤「无消费点」= 0 条**、
**且 ⑦ 判据自检（`A1593`）= 0 条红**、**且 ⑧ 头→通道键近似自检（`A1598②`）= 0 条红**
⇒ `rc=0`；否则 `rc=1`（⛔ 不许静默绿）。
它每次都会**同时**打一行「老口径」供对照 —— 两行并排就是「空转 ⇒ 全绿」的现场。

### 🔴 ⑦ ④ 档那道闸的判据自检（🆕 `A1593`，2026-10-11）—— **含一条「旧口径必红」的断言**

**为什么单开一栏**：④ 的第三道闸原来按全卡签名并集比（见上表下面那段），而**已查实的两个实例**
（`SOR28 Dominion Superior` = `Rally: Deal 1 damage. 2 ☀: Deal 1 additional damage` ——
`deal` 撞上同一张卡的 `Rally:`；`SOR36 Celestian Superior` = `Pray: Deploy a Celestian. 6☀: Deploy a Celestian`
—— 两段正文**逐字相同**）**从没进过 ④ 的名单**。⇒ 台账 `A1452` 的「11 条 / 9 张」是**低报**，
形状口径 = **13 条 / 11 张**。

**三条断言**（夹具卡在内存里现造，⛔ 不进卡池）：

| # | 断言 | 为什么它非有不可 |
|---|---|---|
| **A** | 形状**逐字照抄**上面两个实例、**只把付费货币换成没有通道的那一种**（`2 [Might]:` / `6 [Might]:`）⇒ 付费那一段**必须**被报成孤儿 | 🔴 **旧口径必红**：它按全卡并集比，`deal` / `deploy` 撞上 `Rally:` / `Pray:` ⇒ 判成「收到了」⇒ 报 **0 条**。**这一条在旧口径下结构上不可能绿** |
| **B** | 池里的 `SOR28` / `SOR36`：付费段**必须由【信仰通道】认领**（不是被 `Rally:` / `Pray:` 顶上） | 旧口径**没有「通道」这一维** ⇒ 它连「是哪条 ability 收的」都答不出；这条钉的就是**答得出** |
| **C** | 夹具 `Rally: Deal 1 damage. Rally: Deal 3 damage` ⇒ 第二条必须报 `同头两写` | 挡住「只要头注册了就一律算收到」那种改法 |

⚠️ **它计入 rc** —— 那条闸一旦退回「全卡并集比」，A 立刻红。⛔ 别把这三条断言删了换成一个数。

### 🔴 ⑧ 头→通道键 那道近似的判据自检（🆕 `A1598②`，2026-10-12）—— **探针侧 vs 引擎私有 `StripParamSuffix`**

**病灶**：`WillRun.ResolveTriggerKey`（`ClaimChannel` 用它把卡面那个触发头对到 `TriggerTexts` 的键）
是探针写的，而**真正那份判据是引擎的私有方法 `CardDef.StripParamSuffix`** ⇒ 这一处**没有任何断言**，
引擎以后给别的触发词加参数时宽度会**悄悄变**。

🔴 **`A1598②` 一并查出并修掉的那条**：旧的近似只问「**尾部是纯数字 + 前半截正好是字典键**」，
而引擎剥参数**还有第二道闸** —— `StripParamSuffix` = `ReTriggerHead` **且 `CardDef.IsParamTrigger(组1)`
（今天只有 `ecstasy`）**。少了第二道闸就**比引擎宽**：卡面若真写 `Rally 2:`，探针会把它接到
`TriggerTexts["rally"]`（**假收到**），而引擎 `IsRoutableTrigger("rally 2")` = false ⇒ 根本没登记。
⚠️ 池侧今天没有这种写法（`rally 2` → 0 处），所以它**一直没露出来** —— 那正是「悄悄变宽」。
⇒ 判据改成「形状 + `CardDef.IsParamTrigger`」两道（**转调引擎公开面**，⛔ 不另抄一张表）。

**两条断言**：

| # | 断言 | 为什么它非有不可 |
|---|---|---|
| **A** | **六个形状逐条对拍**，**期望值手写**：`ecstasy 5`→`ecstasy` · `ecstasy`→`ecstasy` · **`rally 2`→`null`** · `rally`→`rally` · `strike 3`→`null` · `ecstasy x`→`null`。引擎那一列用**反射**读私有静态 `StripParamSuffix` | 🔴 `rally 2 → null` 那一条**在 `A1598②` 修掉之前结构上必红**（旧实现给 `rally`）。⚠️ 期望值是**手写**的、⛔ 不是拿引擎现算值反填 ⇒ 引擎改了 `IsParamTrigger` / `ReTriggerHead` **这里立刻红**（= 回来重读这一处口径、然后**手改**期望值） |
| **B** | **池侧钉子**：真正走这条近似的头**必须非空**、且**基词只能是 `{ecstasy}`**（现读 **9 处**，全在 `EC*`）；并把「有尾数字但不该被认」的 `oath N`（28 处）并排打出来当反面对照 | 空集 = 这条断言是**空话**；基词多出来 = 引擎给别的触发词加了参数、卡面已出现那种写法 ⇒ 红 |

⚠️ **它计入 rc**。⛔ 别为了让它绿而放宽 `ResolveTriggerKey` 或删掉形状表。

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
bash d:/4/Unity/工具/ruleprobe.sh play all                  # 全池 sweep（≈5 秒）
bash d:/4/Unity/工具/ruleprobe.sh play all dump <out.tsv>   # 附带逐卡 TSV（1126 行）
# 🆕 `A1431` 第二笔（2026-10-12）：**结算层对拍**
bash d:/4/Unity/工具/ruleprobe.sh play all pf <out.txt>     # 写【结算指纹】基线（= 被确认的状态）
bash d:/4/Unity/工具/ruleprobe.sh play check                # 逐卡比 + 哨兵 ⇒ 差异/哨兵红 ⇒ rc=1
bash d:/4/Unity/工具/ruleprobe.sh play check <out.txt>      # 指定另一份指纹基线
```

🆕 **故障注入口**（只为**验尺子**用）：`WFPROBE_POOL=<另一份池.json>` 可以把探针指向**卡池的副本**
（工程里那一份归 `工具/gen_cards_engine.py`，探针**只读**、⛔ 不许改）。
⚠️ 它**一律出声**（`LoadPool` 打横幅）—— ⛔ 不许静默换池，也⛔ 不许拿覆盖后的读数去重钉常量。

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
| 补手牌 | 🔴 被测卡**没活到这一手**时按引擎形状补进手牌（`NewInstance`→`Hand.Add`→`SetupCardInHand`，≡ `RuleCore.SetDefensiveCard`）。**不补会漏掉 137 张**：**临时卡 98 张**（`EndTurn:1341` 的 `SweepEphemeral` 会在回合末把手牌里的临时卡**移出游戏** —— 这是引擎**对的**行为）＋ **防御卡 39 张**（`BuildPlayer` 把它从牌库分流出去、只发给后手）。⇒ **两族 = 本夹具【结构上不适用】的那两族**，可执行的过滤口径与逐条读数见下面「本夹具【结构上不适用】的两族」（`A1572`）那一节；TSV 里标在 `fixtureFit` / `injected` 两列 |
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

### 🆕 `play check` —— **结算层**的「漂移可得见」（`A1431` 第二笔，2026-10-12 写手 `E5`）

🔴 **它补的是哪一半**：上面那些读数**原来没有任何留痕** ——
「这张卡**昨天**实得 3 条变化、**今天**实得 0 条」**没有任何东西记得住**，
而结算层的**静默漂移**（`A1388`/`A1407`/`A1426`/`A1428` 那一族**修完又坏回去**）正是本仓最贵的一类亏。
`play all` 的 `rc` 又**只认「崩了 / `ErrUnimplemented`」** ⇒ 它在**结算层**是**只摆读数、永不红**的。

⇒ 做法（`A1382` 那个形状，**一层一层搬下来**）：

| 件 | 是什么 |
|---|---|
| **逐卡结算指纹** | 把每张卡的读数压成一行**只含数值**的摘要（`id/type/…/changes/crash/档` ＋ **变化逐条全文**）。写盘 = 基线文件，全池一份算 md5 |
| `play all pf <out.txt>` | 写这份指纹文件（= 「**被确认的状态**」的快照） |
| `play check [<pf.txt>]` | ① 与基线**逐卡**比（出「**哪张卡、哪一列、从什么变成什么**」）；② **哨兵**：基线文件内容的 md5 必须 = 源码常量 `PlayProbe.PinnedPlayMd5` |

🔴 **为什么必须有②**：「与基线文件比」这条判据，**基线文件同一个工具就能重写**
（`play all pf` 一条命令）⇒ 光靠① = **自证**。②的参照物是**源码里的常量** ⇒
`play all pf` **抹不掉它**（重钉只能**手改源码** = 显式动作、会出现在 `git diff` 里）。
**这与 `check` 的 ⑤/⑦ 是同一个道理**（→ 下一节）。

⛔ **指纹【不含】**卡名（改名的产物）· `note`（口径散文）· 引擎日志（措辞会变）——
那三样进来会**天天红**，把「红了不用管」训练成习惯。
✅ **含**「变化逐条全文」—— 少了它，「条数一样而内容变了」就照不到
（同族坑：只比张数会被抵消，见 `Snap` 的注释）。

**构造性实测（2026-10-12 · 全在仓库外做）**：

```text
# 注入：一份卡池副本，改两张卡的 desc（UM9 `Deal 1-3 damage`→`1-9`；
#       GSC_Improvised_Barricade `Does nothing`→`Deal 3 damage to an enemy`）
                        ┌── 旧版 `play all` ─────────────┬── 新版 `play check` ────────────┐
rc                      │ 0（**不出声**）                │ 1                                │
它说了什么              │ 什么也没说（只把数字摆出来）   │ 指名到卡到列：                   │
                        │                                │   GSC_Improvised_Barricade       │
                        │                                │     other 0→2 · dmg 0→3 · changes 4→6 │
                        │                                │   UM9                            │
                        │                                │     other 2→3 · dmg 3→7 · changes 6→7 │
攻击（拿回归后的 live   │ ——（没有「基线」这个概念）      │ ① 被抹平（逐卡全等）             │
去重生成基线）之后      │                                │ ② 🔴 红：基线文件 md5 ≠ 钉住值  │
```

| 场景 | ① 逐卡 | ② 哨兵 | rc |
|---|---|---|---|
| 常态（无人动过） | ✅ 全等 | ✅ | `0` |
| **读数漂了、基线没重生成** | 🔴 指名到卡到列 | ✅ | `1` |
| **重生成基线把漂移抹平**（`A1382` 那条洞的结算层版） | ✅（**被抹平**） | 🔴 | `1` |
| 基线文件读不到 | —— | —— | `2` |

⚠️ **与 `check` 的已知差别（如实标着）**：结算读数**依赖卡池内容**（改一张卡的 `desc` 就改它的读数）
⇒ 卡池一改，本条**就会红**（⛔ **不像** `check` 的 ⑤ 那样「此时不判」—— 那正是 `A1382` 假阴的来源）。
**这是有意的**：红了先跑 `play check` 看逐卡差异（**全池 ≈5 秒**）。
⛔ **它仍不判对错**（判据在卡面上）—— 它判的是「**结算结果有没有离开被确认的那一版**」。
⚠️ **已知盲区照旧**：这一步跑的仍是**同一个夹具** ⇒ **`档 B` 那 274 张「等事件」的卡在这里照样看不见**
（那是**夹具盲区**，⛔ 不是「没有差异」）—— 见下一节。

### 判据 / 退出码

- `play all`：**抛异常 = 0 张 ⇒ rc=0**，否则 `rc=1`。
  「候选」与「打不出去」**不计入 rc** —— 它们要**对着卡面**逐张判，本工具不代替判决
  （⛔ 但也不静默：两栏都逐条打了出来）。
  🆕 **⑥ 档位判据自检（`A1599`）= 0 条红** 才算过（见下节）—— **计入 rc**。
- `play <id>`：崩了 / `ErrUnimplemented`（卡面上写着正文、`CanPlayTactic` 就把它挡回去）⇒ `rc=1`。
  ⚠️ `ErrNoTargetAvailable`（`A985⑧` 拆出来的那一档）**不算** —— 那是**夹具里没有合法目标**，
  是**量具的限制**，不是卡的缺陷。
- 🆕 `play check`：**① 逐卡全等 且 ② 哨兵过 ⇒ rc=0**；有差异或哨兵红 ⇒ `rc=1`；基线读不到 ⇒ `rc=2`。

### 🆕 ⑥ `Bucket` 付费前缀判据的自检（`A1599`，2026-10-12）—— **含一条「旧口径必红」的断言**

**病灶**：档位（`A` / `C` / `B`）里那条「这一段是不是**付费能力**」原来用 `WillRun.IsPaidHead`
（正则 `^\d+\s*[\[\(]?\s*[a-z☀]`）—— 它**只认 `N <货币>:` 那半截**，而引擎认的付费前缀有**两条**
（`EffectText.RePaid` / `ReOathPaid`）⇒ **`Oath N:` 认不出** ⇒ 那些段落到「认不出的头」那一支
⇒ **整张卡被归进 `C 部署时该发生`**（= 报成「部署那一拍就该有变化」，而它其实是**主动用**）。
🔴 **别把它读成「誓约能力没收下来」** —— 那批收得到（`CardDef.OathOps`）；这一条只治**档位**。
⇒ 判据**转调引擎自己那次 `ParseSegment`**（`PaidCost > 0`，与 `WillRun.AddFaceBody` / `Classify` ② 同源）。

| # | 断言 | 怎么才能红 |
|---|---|---|
| **A** | 夹具 `Oath 2: …` / `Oath 1: …`：**新口径 = B / 旧口径 = C**，并排打出「引擎 `PaidCost` = 1」与「`IsPaidHead("Oath 2")` = **false**」 | 🔴 **旧口径下结构上不可能绿**（只能给 C）；新口径若也给 C ⇒ `Bucket` 又拿形状正则当付费判据了 |
| **B** | `2 ☀: …` / `2 [Spirit Stone]: …`：**两个口径都必须给 B** | 挡住「把付费判定整个删掉」（那种改法会让 A 变绿、B 变红） |
| **C** | 池侧三条：① 含「引擎认付费、旧正则认不出」的段的卡 **28 张**（> 0，否则断言是空话）② 新旧口径**档位不同**的卡 **16 张**（> 0）③ 每一张都**含**这样的段（变化**由这一处引起**） | ⛔ 一旦有人把判据改回那条半截正则，② 立刻 0 ⇒ 红 |

⚠️ **它计入 rc**。现场读数（池 1126 张）：那 16 张**全部**是 `UM*` 的 `unit`，
**`C 部署时该发生` 90 → 75 · `B 等事件/等主动用` 275 → 290**（`A` 档 41 没动）。

### 真池读数（2026-10-11 首跑 · 1126 张 · 不跑 Unity）

> ⚠️ **这几个数【随卡池变】** —— 它们是「**那一刻**的卡池」跑出来的快照。
> 实测同一天里就动过：并行写手改了一次 `cards_engine.json` ⇒ 候选 **411 → 410**
> （`C 部署时该发生` 96 → 94、`B 等事件/等主动用` 274 → 275；`A` 档 41 没动）。
> ⇒ 要**现值**就跑一次 `play all`（≈5 秒），⛔ 别拿下面这几行当判据。

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

> 🆕 **2026-10-12（`A1431` 第二笔）补一句**：下面第 1 条**仍然成立**（对错判不了），
> 但「**读数不可比 / 漂移没人记得住**」那一半**已经补上**了 —— 见上面 `play check` 那一节。

1. **对错** —— 判据在**卡面**上（`desc`/`descZh`/PnP 成品卡图），本工具只摆**实得数字**。
   ⛔ 绝不用我们自己的实现反填期望值（那就是「拿我们的实现证明我们的实现」）。
2. **档 B 那一族**（`Strike:`/`Slay:`/`Duty:`/`When …`/`Teleport:`/`Ambush:`/`Survivor:` …）——
   要测它们得另写「打一发 / 让它死 / 让它抽到」那种**专属夹具**，本夹具产生不了那些事件。
3. **只改后续回合的**（`For the rest of this battle, at the start of your turn …`）·
   **要特定关键词/兵种的**（场上三张是 步/载具/兽、**都无关键词**；垫牌另有无人机与战术卡）·
   **要靠跨句承接的**（`A1428` 那族）—— 都会落进候选，但**是量具盲区，不是证据**。
4. **费用之外的手牌外区域**（牌库顶顺序那类）只按 id 多重集比 ⇒ 只看得出「换了哪几张」。

### 本夹具【结构上不适用】的两族 —— 两条**可执行的过滤口径**（🆕 `A1572`，2026-10-11）

🔴 **为什么单列**：`other == 0` 问的是「打出去之后局面动没动」，而这两族**在本夹具里到不了「打出去」那一步**
（不是卡的毛病、也不是引擎的毛病，是**夹具的形状**决定的）⇒ 和自然上手那批混读会**误报一整族**。

| 口径 | 可执行的谓词 | 为什么（引擎侧的判据） |
|---|---|---|
| **① 临时卡** | `CardDef.Has("ephemeral")`（与 `BattleContext.IsEphemeral` 的前半同一处语义，`RuleCore.cs:1171`） | `RuleCore.EndTurn` 会调 `SweepEphemeral`（`RuleCore.cs:1341`）：回合结束时把**该方手牌**里的临时卡**移出游戏**（规则书 `:229`「非弃置」）⇒ 凡「先摆牌库、等它自然上手」的夹具，跨一个来回之后它**必然不在手里**（实测 **98 张全部如此**） |
| **② 防御卡** | `CardDef.Type == "defence"` | `RuleCore.BuildPlayer`（`RuleCore.cs:506/553`）把 `defence` 从**牌库**里**分流**出去（`defenceCardOut`），发牌由调用方按**座位**做、只发给**后手**（`GrantDefenceCard`/`SetDefensiveCard`）⇒ 本夹具的 **P0 是先手**，手里永远没有它（实测 **39 张全部如此**）。⚠️ 补进来之后还有一层不等价：**它被测量时站在先手这一侧**，而原版只会出现在后手手里 |

**现行读数（实测 · 池 1126 张 · 候选 410 —— ⚠️ 这几个数**随池变**，现值跑 `play all`）**：两族共 **137 张**（98 ＋ 39），
**137/137 都靠【按引擎形状补手牌】才测到**（TSV 的 `injected=1` 那一列就是这么来的）——
两族与 `injected=1` 的行**互为同一集合**（`play all` 的 ③ 节每次现算并打出来；哪天不再相等，
说明补牌/分流的形状改了，**回来重读这一节**，⛔ 不是自动豁免）。

🔴 **⛔ 但这两族【不整族从候选里删除】** —— `A1572` 的账文原写「必须从候选里排除」，
**实测与本夹具的实际形状不符**：落进候选的只有 **7 张**（3 临时 ＋ 4 防御），
而它们**各有别的原因**，其中 **3 张是【真缺陷】**：

| 卡 | 族 | 引擎自己那句话 | 性质 |
|---|---|---|---|
| `TAU27` Homing Beacon | ephemeral | `Deploy a Crisis Battlesuit` 部署不了 —— 按兵种「crisis」在卡池里一张都没筛到 | 🔴 **真缺陷**（`Crisis Battlesuit`=`TAU15` **在池里**） |
| `TAU67` Tidewall Droneport | defence | `Deploy a Guardian Drone` —— 按兵种「guardian」一张都没筛到 | 🔴 **真缺陷**（`Guardian Drone`=`TAU8` **在池里**） |
| `GSC68` Rusted Vents | defence | `Create a random troop with Ambush in your hand` 造不出牌 | 🔴 **真缺陷**（同族；池里有 11 张带 `ambush`） |
| `SAU6` Master Chronomancer | ephemeral | 从牌库选了一张 → 放到牌库顶 | ⚪ 量具盲区（牌库**顺序**不在指纹里，见上面第 4 条） |
| `GOF60` Extractor Rig | defence | 「vehicle」里的手牌与牌库每张 -2 费，登记 6 张 | ⚪ 量具盲区（**牌库逐张费用**不在指纹里） |
| `SAU26` Reanimate · `SAU67` Resurrection Vault | ephemeral · defence | 「要翻残骸，但场上没有残骸、墓地里也没有单位」 | ⚪ 夹具没有残骸/墓地单位 |

⇒ **整族删除会把那 3 张真缺陷一起盖掉**（本仓红线「不许静默失败」＋ 铁律 11「全部都要做」）。
**可执行的用法**：按 TSV 的 `fixtureFit` 列**标注**这两族、把它们的 `other=0` **单列出来逐条看原因**
（`play all` 的 ③ 节就是干这个的：逐条附上**引擎自己**那句最能解释「为什么没变化」的话），
而不是把它们从候选里抹掉。

**TSV 新增两列**（**只加在末尾**，前面的列一个字没动）：

| 列 | 值 | 含义 |
|---|---|---|
| `fixtureFit` | 空 / `ephemeral` / `defence` / `ephemeral+defence` | 本夹具对它**结构上不适用**（空 = 适用）。纯 ASCII，好 `grep` |
| `injected` | `1` / `0` | 这一张是**夹具补进手牌**才测到的（`1` = 量具干预过）⇒ 与自然上手那批**不等价** |

```bash
bash d:/4/Unity/工具/ruleprobe.sh play all dump /tmp/play.tsv
grep -P '\tephemeral|\tdefence' /tmp/play.tsv        # 只看这两族
grep -P '\t1$'                 /tmp/play.tsv        # 只看「补手牌才测到」的那 137 张
```

## `nonkw` —— `non <关键词>` 的目标筛选**专用探针**（🆕 `A1462`，2026-10-11）

```bash
bash d:/4/Unity/工具/ruleprobe.sh nonkw    # 四节读数 + rc（有「没承载」的 ⇒ rc=1）
```

**它治的是什么**：`A1462` 那条账原文是「`non <关键词>` 的目标筛选**疑似整个丢了** · ⛔ 未坐实」——
而它**判不了**的原因有两条，**都在探针这一侧**（⛔ 不是引擎）：① dump 不打 `Target2`；
② `atturn` 的目标解析在**嵌套 op**（`AtTurnOps`）里。两条都已在同一天补上
（`/{T2:…}` 块 ＋ `NestedText`），本子命令再把「谁写了 · 承载在哪个字段 · 真打会怎样」逐条摆出来。

**四节**：① **全池普查**（扫 `desc` ＋ `keywords` 原文，递归进 `AtTurnOps`/`RepeatOps`/`BaseOps`）
· ② **判据与承载字段** · ③ **行为对照**（判定层 + 真路径） · ④ **结论**（`Lost > 0` ⇒ `rc=1`）。

**判据（三层，全部是外部的，⛔ 不拿我们自己的实现反填）**：

| 层 | 内容 |
|---|---|
| **卡面**（铁律 7：PnP 成品卡图） | `Orks/3部队/Warpforge_11_Unleashed-Tramplasquig.png` = `〔Stomp〕 Stomp. At the start of each turn attack a random non 〔Flying〕 Flying enemy`；`Space Wolves/3部队/Warpforge_12_Hunting-Wolf.png` = `Ferocity: Attack a random non 〔Flying〕 Flying enemy troop`（两张都**亲读**过） |
| **原版**（`d:/2/tools/decomp_full/`） | `TargetCriteria.cs` 有 `traitsFilter` ＋ `excludeTrait`（`True => without trait`）；消费点 `FilterMethods__MeetsTraitCriteria.c`：`bVar2 = (param_3 ^ 1) & bVar2` ⇒ **取反真的会生效** |
| **我们这一侧** | 承载「排除」的**唯一**字段 = `EffectTargetSpec.NotKeyword`（消费点 `RuleCore.ApplyTargetFilters`）⇒ 判据 = 「卡面写了 `non X` ⇒ `NotKeyword` 必须是 `x`」 |

**现行读数（2026-10-11 · 池 1126 张 · 本文件写下的那一刻）**：目标短语里写了 `non X` 的 **2 处**
（`GOF85` · `SW12`），**两处的 `NotKeyword` 都是空的** ⇒ 🔴 **丢了**，而且**丢在解析层**：
`EffectText.ParseTarget` 里**没有** `non <词>` 这一支（它只认 `with <词>` ⇒ `ReTargetWith` → `KeywordFilter`）
⇒ `non flying` 两个词**整段丢掉**，`Raw` 留着原文（`Raw` 只是排查用字符串、**不参与筛选**），
而 `Side`/`Kind`/`Count` 照常解出来 ⇒ **句子报 OK、卡面不打 `*`**（静默）。
⚠️ 同池另有 **4 处 `non X` 不在目标短语里**（`ChooseWhat`：4 张 `Choose a non-Legendary …`）——
本子命令**单列**它们、⛔ 不混进上面那一栏（那是**另一条**路，`B19Test` 有断言看着）。
⚠️ **`non-` 在别处【有】实现**（⛔ 别把它读成「全仓没有」）：**卡筛选**那一路
（`CreatePool.FilterChoose`）认死两个前缀 `non-legendary` / `non-ephemeral`（`CreatePool.cs:303/305`）——
缺的**只有目标短语**这一路（`EffectText.ParseTarget`）。

🔴 **后果已量过（§③ 真路径）**：`GOF85` 走真实触发（`EndTurn`→`BeginTurn`），
8 次抽样里 **1 次空过** —— 随机挑中了飞行单位（`Count=1` ＋ `Random` ⇒ `TryAttackOnce` 只拿到**那一个**候选，
近战打不到飞行、**没有别的候选可退**），而场上**明明有**可打的非飞行敌人。
§③(a) 还给了判定层的对照：**把 `NotKeyword` 补上，候选里就没有飞行单位** ⇒
**结算层是好的**，⛔ 别去改 `ApplyTargetFilters`，修在**解析层**。

🔴 **`check` 的哨兵 ⑥ 就是钉这一条的**（`Sentinel`，**计入 rc**）：它**现在就是红的**、
且**修好之后会自动变绿**。⛔ 看到它红**不是**「探针坏了」—— 读那句 `✗` 的正文，
它逐条列出「哪张卡 · 哪个 op 栏 · 卡面写了什么」并指名修法。

⚠️ **顺手量出来的另一条（与 `A1462` 无关，别混）**：`§③` 的**原样**跑法 **8/8 全落 `ErrExhausted`**
—— `BeginTurn` 里 `ResolveAtTurn("turn_start")`（`RuleCore.cs:1110`）**排在【解疲劳】
`u.RefreshForNewTurn()`（`:1161`）之前**，而原版是**先解疲劳再触发**
（`CardScript__OnTurnStart.c`：`ActivateMinion` 在 `:99`/`:118`，`RawCardScript__OnTrigger` 在 `:137+`）
⇒ **回合开始触发的强制攻击在我们这儿永远打不出去**。探针要量 `A1462` 只能在跑法②里
**替它先调一次引擎自己的【解疲劳】**（如实标明是量具干预）。

## `faith` —— 信仰付费段的【两把尺子】对账（🆕 `A1452` / `A1453` / `A1565`，2026-10-11）

```bash
bash d:/4/Unity/工具/ruleprobe.sh faith                      # 六节读数 + rc
bash d:/4/Unity/工具/ruleprobe.sh faith dump /tmp/faith.tsv  # 附带逐段 TSV
```

**它治的是什么**：同一个池上，「信仰（`N ☀:`）付费段有几条」**有两个数**，
而两边**都是同一份源码跑出来的**、都「看着有依据」：

| 尺子 | 判据 | 卡型范围 | 段要有冒号 | 正文要解得开 | 别的层没认领 |
|---|---|---|---|---|---|
| **P（解析层）** | 逐段 `EffectText.ParseSegment`，取 `PaidCost > 0 && CostKindOf(PaidKind) == "faith"` | **全池四种卡型** | ❌ 不要求 | ❌ 不要求 | ❌ 不要求 |
| **Q（探针 ④「付费段」档）** | `WillRun.AddFaceBody` + `Classify` ② | **只 unit/hero** | ✅ 要求 | ✅ 要求 | ✅ 要求 |

⇒ **Q ⊂ P 按构造成立**，两把尺子**不是「打架」，是「问的不是同一个问题」**。
`faith` 子命令把两边逐条摆出来、**给每条差集指名差因**，并回答**真正那个问题**：

- **射程 = 收集层 `CardDef.FaithOps` 到底收到几条**（§④，逐卡比 **cost 多重集**）；
- **④「付费段」那一档必须被关掉**（§⑤，收进 `WillRunOps` 之后 ④ 应为 **0 条**）。

### 现行读数（2026-10-11，池 1126 张）

| 项 | 值 |
|---|---|
| 尺子 P（解析层，逐段） | **25 条 / 22 张** |
| 尺子 Q（探针 ④ 形状） | **13 条 / 11 张** |
| P − Q | **12 条**：11 条是 `tactic`（④ 只对 unit/hero）＋ `SOR72`（**段里没有冒号**） |
| Q − P | **0 条**（Q ⊂ P） |
| 收集层覆盖（`FaithOps` vs P，逐卡 cost 多重集） | **22 / 22 张全中**，`漏` 0 |
| `willrun` ④「付费段」（**货币 = `faith`** 的那些） | **0 条**（改动前 11 条；`A1593` 起 ④ 也会收别的货币 ⇒ §⑤ 只比 `faith` 这一档） |
| `willrun` ⑤「无消费点」 | **12 条**（改动前 13 —— `SOR72` 那一条被收集层吃掉了） |

🔴 **两条必须记住的口径**：

1. ✅ **④ 那一档「少报」的那道闸已经修掉（`A1593`，2026-10-11）** —— 它原来多一道「腿 A 一条都没收到」
   的闸，而那道闸**按 op 签名比、不认是哪条 ability** ⇒ 同一段正文在**别的** ability 里出现过
   就会被判成「收到了」。实测两例：`SOR28 Dominion Superior`（`Rally: Deal 1 damage` 与
   `2 ☀: Deal 1 additional damage` 的 `deal` 签名撞上）· `SOR36 Celestian Superior`
   （`Pray:` 与 `6☀:` 两段正文**逐字相同**）。
   ⇒ 台账里 `A1452` 的「11 条 / 9 张」**偏低**，形状口径实为 **13 条 / 11 张**。
   🔴 **现在按【通道】比**（`WillRun.ClaimChannel`，判据与自检见 `willrun` 那两节）——
   那两个实例**仍然不在 ④ 的名单里，但理由变了**：`CardDef.FaithOps` 已接进 `WillRunOps`
   ⇒ 它们的**信仰通道确实收到了它们**（自检 = `willrun` ⑦ 断言 B）。
   ⇒ 本子命令的 ⑤ 因此是**子集检查**（`PaidSegments ⊆ Q`，且只比**货币 = `faith`** 的那些），
   **不是相等检查** —— 写成相等会把「④ 与 Q 覆盖的货币不同」误判成本命令写歪了。
   ⚠️ 这也是**为什么本文件重写了一遍 `AddFaceBody` 的判据**（`FaceBody` 是 `WillRun` 的私有嵌套类型）——
   所以 ⑤ 那条子集检查就是它的自检，**不通过就整份读数作废**（rc=1，⛔ 不许静默）。

2. 🔴 **别拿旧探针的 `19 条 / 17 张` 当第二个口径** —— 那是**探针工装**的产物，不是解析器的：
   `D:/tmp/wf_e5/texts.tsv`（`E5` 那笔的输入）把**多行 `desc` 按真换行**写进了 TSV
   ⇒ `File.ReadAllLines` 之后，**第一行之后的部分变成了独立行**、被 `Split('\t')` 判成「字段不足」丢掉
   ⇒ 全池 **24 张卡**的 `desc` 被截断（`SOR2` / `SOR4` / `SOR40` / `SOR44` / `SOR59` …）。
   同一份源码喂**真 `desc`** 得到的是 **25 条 / 22 张**（本仓 `faith` 子命令直接读 `cards_engine.json`，
   **不经 TSV**，就是为了不再吃这一口）。
   ⚠️ 教训：**探针的【输入工装】和探针的【判据】是两件事** —— 输入错了，读数照样「看着有依据」。

### 输出六节

| 节 | 内容 |
|---|---|
| ① | 尺子 P 逐条（卡 / 卡型 / 段 / cost / ops / 谁认领） |
| ② | 尺子 Q 逐条 |
| ③ | 差集 + **每条的差因**（卡型 / 没冒号 / 正文解不开 / 别层认领 / 头形状） |
| ④ | **收集层覆盖**（`FaithOps` vs P，逐卡 cost 多重集）—— **这一栏才是射程** |
| ⑤ | 与 `willrun` ④ 档的关系（子集检查，**只比货币 = `faith` 的那些** + **④ 里 faith 那部分必须 0 条**） |
| ⑥ | **真打**：门槛过不过 + **信仰前后必须相等** |

✅ **退出码可信**：`rc=0` 的判据 = ③ 每条差集都有差因 · ④ 无 `漏` · ⑤ 子集成立且 ④（**`faith` 那一档**）为 0 · ⑥ 真打全对。

### 🧨 ⑥ 那一栏为什么必须存在（改哪两处会一起变绿）

§①②③④⑤ 量的全是**文本 / 收集**，**没有一条**回答「打出去之后按不按阈值算、信仰有没有被扣掉」。
而 `A1453` 要的恰恰是后者（`N` 是**阈值**不是开销 —— 我们原来在扣 `ps.Faith`）。
只断「效果落地了」是不够的：**把扣款那一行加回去，效果照样落地** ⇒ 那一半仍然绿。
⇒ ⑥ 的判别式是 **`信仰 前后相等`**（`SOR27` 0/2/6 · `SOR11` 2/3 · `SOR68` 4 · `SOR12` 4/5 ·
`SOR72` 6 · `SOR40` 6 共 10 格）。

---

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

🔴 **改【探针自己输出的格式】时**（例：`A1462` 给 dump 补 `/{T2:…}` 块）—— **要走三步，缺一不可**：
① 重生成基线（上面那条命令）；② **同时重钉两个常量**
（`Program.PinnedPoolMd5` ＋ `Program.PinnedBaselineMd5` —— 只钉一个会让哨兵 ⑤ 掉回「此时不判」那一支，
也就是说**那条「基线被静默改写」的守卫会失效**）；③ **在源码里写清「有意重钉」的理由** ——
重钉只能**手改源码**（那才是显式动作、才会出现在 `git diff` 里），⛔ 没有「一条命令自动重钉」的口子。
**自证判据（格式改动专属）**：变化行必须**只是「多了一段尾部块」** —— 把新块删掉之后与旧行**逐字节相同**
（`A1462` 实测：8 行变化、8/8 满足、其余 1112 行逐字节没变）。
⛔ 别用 `sed -i` 改这些常量（会把文件行尾翻掉）—— 用编辑器改。

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
| 🆕 **`non 排除`**（`A1462`，2026-10-11） | 卡面写了 `non <关键词>` 的目标规格 ⇒ `NotKeyword` 必须承载它。**现在【就是红的】**（`GOF85` / `SW12`，共 2 处）—— 那是**已知缺陷的钉子**，⛔ **不是探针坏了**；**修好之后这条会自己变绿**（⛔ 别删它）。判据 = 卡面（PnP 卡图亲读）＋ 原版 `TargetCriteria.excludeTrait`（`FilterMethods__MeetsTraitCriteria.c`）；逐条读数 ⇒ `nonkw` |
| 🆕 **输出指纹**（`A1382` 的**一般情形**，2026-10-12 · 写手 `E5`） | **当次现算的 dump**（规范化：**吃掉卡名**、只留 `id` ＋ `unparsed/partial/ops`、按 id 排序）算 md5，必须 = `Program.PinnedParseMd5`。🔴 **它既不读卡池、也不读基线文件** ⇒ ① **卡池变了也照样在岗**（上面那条「基线指纹」的假阴正是它补的）② `scan` 重生成基线**动不了它**。重钉 = `check` 打出的一整行**手抄进源码**。判据 → 下面那一节 |

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

**构造性实测（2026-10-11）**：拿一份**仓库外**的基线副本、**只改第一行一个字节**
（= 模拟「重生成基线抹平一次真回归」）⇒ `check` 报 **哨兵 ⑤ 红 1 条 · `rc=1`** ✅
（卡池那一半仍报「与钉住的一致」）。

### 🆕 `A1382` 的**一般情形**：⑦ [输出指纹]（2026-10-12 写手 `E5`）

🔴 **⑤ 那条【已知的假阴】不再是敞开的了**（原文留着，见上面一节末行）：
真回归 ＋ 重生成基线 ＋ **卡池也在同一批里变了** ⇒ ⑤ **看不见**。
而**卡池天天在变**（作者 `工具/gen_cards_engine.py`）⇒ ⑤ 常常走「**此时不判**」那一支
（**出声、不算失败**）⇒ **那条洞在⑤这一侧是长期敞开的**（`A1619` 现读就是这个状态）。

⇒ ⑦ 的判据见上面哨兵表最后一行（**不读卡池、不读基线文件**，读的是**当次现算的 dump** 与**源码常量**）。

🔴 **当场可复现的实测（2026-10-12，真工程状态，`E5`）**：

```bash
bash 工具/ruleprobe.sh scan d:/4/_tmp_e5/live_dump.txt     # ① 拿「当次 live」当一份「重生成后的基线」
bash 工具/ruleprobe.sh check d:/4/_tmp_e5/live_dump.txt    # ② 拿它去 check
```

| | 加 ⑦ **之前** | 加 ⑦ **之后** |
|---|---|---|
| 全池摘要 | **解析差异 0 行** | 解析差异 0 行 |
| 哨兵 | ⚠️⑤「卡池 md5 与钉住的不同 ⇒ **此时不判**」（现读 `3ffab02f…`）· **合计 0 条** | 🔴⑦ 红：现算 `38f38061…` ≠ 钉住 `f350e247…` · **合计 1 条** |
| `rc` | **`0` —— 全绿**（= 洞） | **`1`** |

⇒ 这就是 `A1382` 那条洞**在真工程上**的样子，以及它现在为什么不再绿。
⚠️ **另一条独立性质（已实测）**：⑦ 把**卡名吃掉** ⇒ 只改卡名 / 只重生成一次基线**不会**让它红
（`live_dump` 与「把第 1 行卡名改成 `ASH54_RENAMED`」两份的规范化 md5 **完全相同** = `38f38061…`）——
那是 ⑤ 盯的那一种。**两条各盯一种，⛔ 别拿一条去替另一条。**

🔴 **重钉（⛔ 没有「一条命令重钉」）**：`check` **会把该抄的整行打出来**（`public const string PinnedParseMd5 = "…";`），
**手抄进源码** = 显式确认。**重钉前必须先确认这批变化是对的**（`RuleEngineTest.Run` 或真机验过）；
**为了把红抹平而重钉 = 正是它在抓的那件事**。
⚠️ **⑦ 现在就是红的**（钉住的是 `HEAD` 那一版基线 = 上一次被确认的状态；本会话另有写手在改解析器）
—— **那是设计**，⛔ 不是探针坏了。

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
