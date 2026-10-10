# `W` · `ruleprobe` 三笔 —— `A1374` · `A1348` · `A1349`

> 执行写手代理交件，2026-10-10。⛔ 没跑 Unity、没动 git、没改正本、没碰 `RuleEngine/**` 与 `CardPresentation/**`。
> 台账行 = `项目任务.md` §29·b 的 `A1374` / `A1348` / `A1349`（现读 `:507` / `:508` / `:524`）。
> 支撑报告 = `资料/普查产出_第十三会话/W9_A1368探针补列.md`（`A1374` 的形状）· `W5d_A1339探针口径.md`（`A1348`/`A1349` 的顺手发现）。
> 🔴 **顺序是硬的**：`A1374` 先于 `A1348`（后者第三条哨兵要用前者补的 `costKind` 列）。

**碰过的文件（= 白名单全集，`git status` 逐项核过）**：

| 文件 | numstat |
|---|---|
| `Unity/工具/ruleprobe/Program.cs` | 176/3 |
| `Unity/工具/ruleprobe/README.md` | 77/10 |
| `Unity/工具/ruleprobe/Scan.cs` | 30/2 |
| `Unity/工具/ruleprobe/BattleProbe.cs` | 6/3 |
| `Unity/工具/ruleprobe/基线/out_baseline.txt` | 922/922 |

**`BattleProbe.cs` 的完整路径**（简报说主对话没现核过）：**`D:/4/Unity/工具/ruleprobe/BattleProbe.cs`**
（与 `Program.cs`/`Scan.cs`/`FilterAdd.cs`/`Stub.cs` 同级；`ruleprobe.csproj` 显式 `<Compile Include="BattleProbe.cs" />`）。

⛔ **卡池没被动过**：`Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` 的 md5 全程
= **`b4d9684d6794fcd8efcafd4c6f8ebb03`**（与 `W9` 报告那次读数**逐字相同** —— 两条都证明这池自 `W9` 起没变）。
所有变异都在**仓库外**（`D:/tmp/rp_a1374/`）的副本上做。

---

## ① `A1374` —— `scan` 补 `CostKind` / `CountScope` / `Condition` 三列

### 结论

**做了，判据满足。** `scan` 的 op 尾部由**四列**变**七列**（`cost/shared/condKind/countRef` ＋
**`costKind/countScope/condition`**），基线已重生成；`旧行前N列 == 新行前N列` 且 `旧行 ops == strip3(新行 ops)`
⇒ **反例 0 条**（922/922 全过）。**「只改货币名」这类改动现在 `check` 抓得到了**（端到端实测见证据 ④）。

### 改动清单

| 文件:行 | 改动 | 为什么 |
|---|---|---|
| `工具/ruleprobe/Scan.cs:16,37-56` | 文件头：把「七列」的新形状、三列各自的台账出处、以及**为什么仍是「只追加」**写死 | 下一个改探针的人要能一眼看出「尾部为什么是 7 列、`costKind` 为什么没挨着 `cost`」 |
| `工具/ruleprobe/Scan.cs:103-105` | ops 序列化**末尾再追加** `.Append("/").Append(op.CostKind ?? "")` / `op.CountScope` / `op.Condition` | 🔴 **病灶本体**：这三样原来一个字都不打 ⇒ 「只改货币名 / 计数范围 / 条件原文」三类改动对 `check` 全瞎 |
| `工具/ruleprobe/Program.cs:132-136` | `Dump`（`seg`/`card` 的解析体）加 `countScope=[…]` / `cond=[…]` | **两个入口口径不一致**正是本笔的病灶之一（`ck=` 只在这边打）；现在两边打印**同一批值** |
| `工具/ruleprobe/README.md:42-89` | 「dump 的列」一节：四列 → 七列表格、`A1374` 三条动机、两条构造性实测、`|`/`/` 两条取舍 | 形状与判据只留一处 |
| `工具/ruleprobe/基线/out_baseline.txt` | **重生成** | 口径变了基线就得跟着变（⛔ 不许两份口径并存） |

### 证据

**判据（台账原文）**：`旧行前N列 == 新行前N列` 且 `旧行 ops == stripN(新行 ops)` ⇒ 反例必须为 0。
**脚本** `D:/tmp/rp_a1374/verify_a1374.py`（现跑，读数照抄）：

```
旧行数 1120 · 新行数 1120 · 变化行 922 · 纯追加 922 · 反例 0
op 的 / 字段数分布: {13: 1257, 14: 1}          ← 那 1 条是预存的 ASH65（见顺手发现 3）
countScope 取值: {'': 1211, 'board': 27, 'draw': 7, 'darkpact': 6, 'died': 5, 'played': 1, 'spiritspent': 1}
costKind  取值: {'': 1169, 'oath': 30, 'spirit': 28, 'faith': 25, 'energy': 6}
condition 里含分隔符的计数: {}                 ← 全池 `condition` 不带 `|` / `;` / `/`
```

（`922 = 922 纯追加 + 0 反例`；另 **198 行一字未动** = `desc` 非空但解析不出 op 的卡，ops 列为空 ⇒ 没处可追加。
op 总数 **1258** = 1257 + 1，与 `W9` 那份读数的 1258 **逐字吻合**。）

**④ 构造性实测（本笔的核心证据：旧格式瞎、新格式看得见）**
仓库外一份**池副本** + 一个只指向它的探针副本（`D:/tmp/rp_a1374/mut/`），
把 `SOR72` 的 desc 从 `6 ☀ Gain Flank…` 改成 `6 Energy Gain Flank…` —— **只改货币名，一句里只动一个词**。
（先验过这一改**只**动 `ck` 一列：`seg "6 ☀ Gain…"` 与 `seg "6 Energy Gain…"` 打印的其余字段**逐字相同**。）

```
(1) strip3(新·未变异) 与 【真·旧工具产出】的差异行数 = 0     ← 先证明「摘掉尾部三列 == 旧四列格式」
(2) 旧格式（摘掉尾部三列）A vs B 差异行数 = 0                 ← 旧 check 对「只改货币名」完全瞎
(3) 新格式 A vs B 差异行数 = 1   ['SOR72|Adelaide the Serene'] ← 新 check 抓得到
    OLD: …/flank and shield/6/0///faith//;gainfaith/…/faithcheck/enemy|unit|all//board/you have less than 6 ☀;
    NEW: …/flank and shield/6/0///energy//;gainfaith/…/faithcheck/enemy|unit|all//board/you have less than 6 ☀;
```
脚本 `D:/tmp/rp_a1374/verify_constructive.py`。端到端：同一副本
`dotnet wfprobe.dll check "工具/ruleprobe/基线/out_baseline.txt"`
⇒ `=== 全池 1120 张 · 解析差异 1 行 … ===` · **`rc=1`** ✅

**其他读数（现跑）**：`dotnet build` → `已成功生成`（**0 警告** —— `W9` 记的那个 `RuleCore.cs(2750,13) CS0162`
已被别的写手修掉）；`bash 工具/ruleprobe.sh check` → `解析差异 0 行` · `rc=0` ✅；
`b19test` → `PASS 45 · FAIL 0` ✅；`seg`/`card`/`jackal`/`emergency`/`suppressor`/`played` 全部照旧正常。

### 还差什么

- ⛔ **没跑 Unity 自检**（红线）—— 本笔改的是探针壳，`RuleEngineTest.Run` 与它无关。
- ⚠️ **`condition` 今天干净不代表永远干净**：它现在是**原样打**，将来某张卡的卡面若带 `|` / `;`，
  那一行会多出分隔符。已把这条取舍写进 `Scan.cs` 文件头与 `README`（`check` 只认前两个 `|` ⇒ 不受影响），
  **没有做转义**（转义会让 dump 与引擎字段不同 ⇒ 照抄进断言就抄错，那正是本笔要治的那一族）。
- `EffectOp.CountScope` 的一处**语义**没核：`spiritspent` / `darkpact` 这两个取值我**只从 `EffectText.cs:486-494`
  的注释读到**，**没去反编译核它们对应的原版行为**（本笔只要求「打得出来」，不要求核语义）。

---

## ② `A1348` —— `check` 加**与基线无关**的机器级哨兵

### 结论

**做了，判据满足。** `check` 现在除「与基线比」之外，每次还会跑 **3 条哨兵** ——
它们的**判据写死在源码里**（`scan` 重写得了基线文件、重写不掉 `.cs`），读的是**当次现算的 dump**、
⛔ 不看基线 ⇒ **重生成基线抹不掉它们**；哨兵失败**算进退出码**（⛔ 不会出现「哨兵红、界面全绿」）。

**构造性实测（两次，各自在「拿旧基线比」与「**重生成基线之后**再比」两个状态下都跑了）**：

| 变异（仓库外副本） | 拿仓库基线比 | **重生成基线之后**再比 |
|---|---|---|
| `SOR72` 付费前缀 `☀` → `Energy`（= 倒放 `A1357`） | 解析差异 1 行 · 哨兵 ✗ 1 条 · **rc=1** | 解析差异 **0 行** · **哨兵 ✗ 仍 1 条 · rc=1** ✅ |
| `Program.LoadPool` 尾部 `BuildNameIndex` 删掉（= 重演 `A1339`） | 解析差异 1 行 · 哨兵 ✗ 2 条 · **rc=1** | 解析差异 **0 行** · **哨兵 ✗ 仍 2 条 · rc=1** ✅ |

🔑 **右列就是本笔的验收**：修前那一版（`return parseDiff > 0 ? 1 : 0`）在这两种情形下**都会报绿**。

### 改动清单

| 文件:行 | 改动 | 为什么 |
|---|---|---|
| `工具/ruleprobe/Program.cs:194-331` | 新增 `class Sentinel`（3 条哨兵 ＋ 两个取列/取行的私有助手） | 🔴 **病灶本体**：基线可被同一个工具重写 ⇒ 真回归被静默抹平 |
| `工具/ruleprobe/Program.cs:362-365` | `CheckMain.Run` 里 `int sentFail = Sentinel.Run(newLines);`（**在算差异之前**） | 它必须读**当次现算的 dump**，不能读基线 |
| `工具/ruleprobe/Program.cs:404` | `return (parseDiff > 0 \|\| sentFail > 0) ? 1 : 0;` | 哨兵失败必须**算进退出码**，否则仍是静默 |
| `工具/ruleprobe/Program.cs:339-343` | `CheckMain` 头注：退出码语义改成「0 = 差异 0 **且** 哨兵全过」 | 文档与代码同口径 |
| `工具/ruleprobe/README.md:22-31,114-142` | 「判据」表加一行哨兵；新增「`check` 的哨兵」整节 | 形状/判据只留一处 |

### 三条哨兵分别是什么（期望值的**原版出处**都写在代码注释里）

| 哨兵 | 判据 | 出处 |
|---|---|---|
| ① **索引（库函数）** | `CreatePool.MatchCardName("Primaris Intercessor")` 非 null | `CreatePool.cs:613`「没建索引时一律返回 null」 |
| ② **索引（`scan` 那条路）** | `UM_Angels_of_Death` 那行的 `partial` 列为空 | `W5d` 实测：索引掉时那一列变成 `Codex: Give +1 [Ranged] to your Primaris Intercessor` |
| ③ **货币名** | `SOR72` **付费那条** op = `cost=6` `ck=faith` | **卡图** `Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png` 上是**金太阳**（`☀`=信仰），`EffectText.cs:7802-7812`（`A1357` 的裁定处） |

⚠️ ①②是**两条**不是一条：①只证明「库函数调得通」，证明不了「**`scan` 自己读 JSON 那条路**也过了
`LoadPool`」（`A1339` 的病灶正是「只补一处 = 静默偏一半」）。②读数据、才管得住那条路。
⚠️ 哨兵卡**按 `id` 找、⛔ 不按卡名** —— 卡名是会变的产物（本仓已改过 6 张）。
⚠️ ③取 op 的方式是「`cost` 列非 0 的那条」，⛔ **不是下标 0**（下标会被将来插进来的 op 挪走）。
⚠️ 哨兵卡不在 dump 里（改名/删卡）时**出声**、不静默放过 —— 报「哨兵要跟着换」。

### 证据

```
$ bash d/4/Unity/工具/ruleprobe.sh check          # 干净池
=== 哨兵（与基线无关 · 重生成基线抹不掉）===
  ✓ [索引] 卡名索引在（`Primaris Intercessor` → `Primaris Intercessor`）
  ✓ [索引·scan 路] `UM_Angels_of_Death` 的 `partial` 列为空（卡名目标认出来了）
  ✓ [货币名] `SOR72` 付费那条 op = `cost=6` `ck=faith`（照卡图）
=== 哨兵合计：失败 0 条 ===
=== 全池 1120 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
=== 19:57:21 结束（rc=0）===
```

变异后的两条读数（`rc` 是**直接取 dotnet 的退出码**，⛔ 不是 `| tail` 之后的 —— 那个会被管道吃掉）：

```
1b) 变异①（只改货币名）·【重生成基线之后】
  ✗ [货币名] `SOR72` 付费那条 op 现在是 `cost=6` `ck=energy`，期望 `cost=6` `ck=faith` …
             现值 op = gain/pool///0/flank and shield/6/0///energy//
=== 哨兵合计：失败 1 条 ===
=== 全池 1120 张 · 解析差异 0 行 … ===          >>> rc=1

2b) 变异②（索引掉了）·【重生成基线之后】
  ✗ [索引] `CreatePool.MatchCardName("Primaris Intercessor")` == null ⇒ 卡名索引**没建** …
  ✗ [索引·scan 路] `UM_Angels_of_Death` 的 `partial` 列**非空** = `Codex: Give +1 [Ranged] to your Primaris Intercessor` …
  ✓ [货币名] …
=== 哨兵合计：失败 2 条 ===
=== 全池 1120 张 · 解析差异 0 行 … ===          >>> rc=1
```

**复验夹具**（都在仓库外）：`D:/tmp/rp_a1374/mut/probe_a1349/`（`A1349` 的）、
`D:/tmp/rp_a1374/mut/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（池副本）、
`D:/tmp/rp_a1374/mut/base_regen1.txt` / `base_regen2.txt`（两份「重生成后的基线」）。
⚠️ `D:/tmp` 重启可能被清。

### 还差什么

1. 🔴 **一般情形没有封死，只封了「已知的两类回归」** —— `A1348` 的病是「**任何**真回归 ＋ 重生成基线 ⇒ 静默」，
   而哨兵只能一条条点。**今天覆盖的是 `A1339`（索引）与 `A1357`（货币名）两类**。
   台账原文要的就是这个形状（「在 `check` 里加一条**索引自证**」），**但这不等于「基线可被重写」这条洞被普遍堵住**。
   要真普遍堵住，得有一个**不在这个工具写权限内的参照物**（例如把一个冻结基线放进别处、由别的流程管），
   **本仓今天没有这种东西** —— 这一句**如实记在这里**，别当成已解。
2. ⚠️ **哨兵集是有意的极小集**（宁可少而准）：一条会经常「合理地」变红的哨兵会把「红了不用管」训练成习惯
   （`CheckMain` 头注那条教训）。**加新哨兵前先问：期望值有原版出处吗？** ⛔ 不许拿我们自己的 dump 反填。
3. ⛔ **没跑 Unity 自检**（红线，且本笔没有对应的 Unity 宿主）。
4. ⚠️ **`check` 的临时文件路径是固定的** `Path.GetTempPath()/wfprobe_check.txt` ——
   两个 `check` 同时跑会互相覆盖。本笔没动它（超出范围，且实测本机没有并发跑探针的流程）。

---

## ③ `A1349` —— `BattleProbe.cs` 三处「不过 `LoadPool` 的解析入口」

### 结论

**做了，判据满足。** 三处 `EffectText.ParseSegment(字面量)` 全部改走新加的**闸** `Program.ParseSeg(text)`
（闸里先 `LoadPool` 建卡名索引、再解析）。**判据**：`grep -rn "EffectText\.ParseSegment(" 工具/ruleprobe/`
⇒ 只命中 `Program.cs` **闸内那一行**。

**构造性实测（直接演示「塞一句带卡名的进去」会怎样）**：
仓库外副本加一个临时模式 `segcard`（它**全程不碰别的装池点**，直接调闸 / 直调解析器）：

```text
句子 = Give +1 [Ranged] to your Primaris Intercessor
过闸（Program.ParseSeg）          → verb=give target=[your primaris intercessor]   ✅
直调（EffectText.ParseSegment）   → verb=give target=[]                            🔴 A1339 那条假象
```

### 改动清单

| 文件:行 | 改动 | 为什么 |
|---|---|---|
| `工具/ruleprobe/Program.cs:149-166` | 新增 `public static EffectText.SegResult ParseSeg(string seg)`（先 `LoadPool` 再解析） | 🔴 **病灶本体**：`BattleProbe.cs` 那三处是 `A1339` 那族的**第四种**解析入口 |
| `工具/ruleprobe/BattleProbe.cs:149-152` | `var seg = Program.ParseSeg("…return it to your hand")` | 同上（`BattleProbe19.Run` 第一处） |
| `工具/ruleprobe/BattleProbe.cs:158` | `var seg2 = Program.ParseSeg("…put it in your hand")` | 同上（`BattleProbe19.Run` 第二处） |
| `工具/ruleprobe/BattleProbe.cs:293` | `var op = Program.ParseSeg("…return it to your hand").Ops[0]` | 同上（`B19Test.Run`） |
| `工具/ruleprobe/README.md:144-168` | 「卡名索引」一节加第 4 条入口 ＋ `A1349` 判据 ＋ 那段对照 | 入口清单只留一处 |

### 证据

```
$ grep -rn "EffectText\.ParseSegment(" /d/4/Unity/工具/ruleprobe/
  Program.cs:165:        return EffectText.ParseSegment(seg);          ← 唯一一处，在闸里
```

**无回归**：`b19test` → `PASS 45 · FAIL 0` ✅；`played` 日志尾与 `W5d` 记的一致
（`B19Chooser 触发 RALLY → 选 B19StratC → 放入手牌`）✅；`seg` 走 `Dump`、本来就过 `LoadPool`，
输出与改前同形（只是多了 `countScope=`/`cond=` 两个标签）✅。

### 还差什么

- ⚠️ **那三句今天仍然没有卡名** —— 所以**今天的行为和改前逐字相同**（本笔是「堵将来的洞」，不是修今天的错）。
  复核办法：`bash 工具/ruleprobe.sh b19test` 前后都是 `PASS 45 · FAIL 0`。
- ⚠️ **闸只管 `ParseSegment`，不管 `EffectText.Parse`** —— `Scan.cs` 那条路仍直调 `EffectText.Parse`
  （它自己调了 `Program.LoadPool()`，`README` 里已列为「自己补一次」的那一条）。
  **没有把 `Parse` 也收进闸**（超出 `A1349` 的范围；见顺手发现 4，那里有一处更值得先看）。
- `_专题/seg_any_1016.cs.txt` **未跟改**（它不在 csproj 的 `Compile` 列表 ⇒ 不编进去）。见顺手发现 4。

---

## 顺手发现

> ⛔ 只报不改（全部在别的文件/别的范围里）。

1. 🔴 **`check` 的默认基线路径少了两级** —— `Program.cs:349`：
   `Path.GetDirectoryName(Assembly.Location)` 是 `bin/Debug/net8.0`，两个 `..` 只上到 `bin`，
   于是默认路径 = **`工具/ruleprobe/bin/基线/out_baseline.txt`**（不存在）。
   实测（不带参数）：`dotnet bin/Debug/net8.0/wfprobe.dll check`
   → `!! 没有基线：D:\4\Unity\工具\ruleprobe\bin\基线\out_baseline.txt` · `rc=2`。
   ⚠️ **它是「响的」不是「静的」**，所以**没顺手改**（也超出本笔三件）。
   日常路径躲过它的原因只有一个：`ruleprobe.sh` **总是**显式传 `"${2:-基线/out_baseline.txt}"`。
   ⇒ 谁直接敲 dll 就会撞上，且 `README` 的「怎么跑」那节**没写「必须带参数」**。
2. ⚠️ **`filter` 模式对「没有 `choosecard` op 的卡」零输出** —— `FilterAdd.cs` 只在 `op.Verb == "choosecard"`
   时 `Console.WriteLine`。实测 `filter "Primaris Intercessor"` **一行都不打**（看着像命令没跑）；
   而 `filter zzzznotacard` 反而会打 `!! no card zzzznotacard`。同族 = 本仓红线「不许静默失败」。
   （`W9` 顺手发现 4 已记过，**今天仍在**、⛔ 没动。）
3. ⚠️ **`ASH65` 那条 op 的 `payload` 里带 HTML 标签** ⇒ 它是**全池唯一**一条 `/` 字段数为 **14** 的 op
   （其余 1257 条都是 13）：`give/pool///0/<i>shuriken</i> 2/0/0//…`（`基线/out_baseline.txt` `ASH65` 那行）。
   来源是 `cards_engine.json` 的 `desc` 里留着 `<i>…</i>` 斜体标记。
   对 `check` 无影响（只认 `|`），但**「按 `/` 数列」的土办法会拿错列** ——
   已把「取尾部列必须从后往前数」写成 `README` 的判据（本笔的 `Sentinel` 就是这么取的）。
4. ⚠️ **`_专题/seg_any_1016.cs.txt:215` 直调 `EffectText.Parse(desc, out unparsed, out partial)`**
   （**3 参写法**）—— 它**既带旧口径、也不过 `A1349` 那道闸**，但它**不在 `csproj` 的 `Compile` 列表**
   （`.txt` 后缀）⇒ **今天不编进去、无影响**。台账 `A1349` 那句「未跟改」**今天仍然成立**，⛔ 没动
   （`W5d` 已判它是「一次性专题存档」，不是活代码）。
   ⚠️ **顺带一条知识**：`EffectText.Parse` **只有一个重载**（`EffectText.cs:1088`），
   第 4 个参数 `bool eventHasTarget = false` **有默认值** ⇒ 3 参写法一直编得过。
   将来若把这份存档恢复成 `.cs`，**得同时补闸**。
5. ℹ️ **卡池 md5 自 `W9` 起没变**（`b4d9684d6794fcd8efcafd4c6f8ebb03`）—— 这是本笔所有构造性读数
   与 `W9` 那份可交叉的基础，记在这里省得下一个会话重测。
6. ℹ️ **`ruleprobe` 全量（`check` + `b19test` + 6 个别的模式）一趟 ≈ 10 秒**（现跑），
   而 `_run_8_checks.sh` 是 1–9 分钟/条 —— 这条「快速内环」的量级今天仍然成立。
