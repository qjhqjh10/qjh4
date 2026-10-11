# `E1` · `ruleprobe` 仪器四笔（`A1466` · `A1477` · `A1380` · `A1383`）

> 执行写手代理交件，2026-10-11 · 第十九会话。
> ⛔ 没跑 Unity、没碰 `MyGame/Assets/**` 任何 `.cs`、没动 git、没改正本、没越白名单。
> 碰过的文件（`git status --short -- 工具/` 逐项核过，**只有这 6 个**）：
> `工具/ruleprobe.sh` · `工具/ruleprobe/{Program.cs,Scan.cs,FilterAdd.cs,README.md,基线/out_baseline.txt}`。
> 判据出处逐条读过：`资料/任务_卡组卡牌战斗.md:37 / :39 / :62 / :68` ＋
> `资料/普查产出_第十四会话/{W_ruleprobe三笔.md 顺手发现 1·2, W_A1467ReLose.md §2·3/§2·4, W_A1447A1448尾句与have.md 顺手发现 7}`。
> 复现夹具全部在仓库外：`D:/tmp/rp_e1/`（⚠️ `D:/tmp` 重启可能被清）。

---

## ① 四笔结论（各自满足自己那一行的判据）

| 账 | 结论 | 一句话判据 |
|---|---|---|
| `A1466` 🔴 | **做了** | 脚本拿 `dotnet build` 的**退出码**判成败；非 0 ⇒ **出声并停**（`rc=1`），⛔ 不再「dll 在就接着跑」 |
| `A1477` | **做了** | `scan` 的每条 op 末尾追加目标块 `/{T:…}`（28 个字段）＋ `/{DW:…}`；改目标解析**现在 `check` 看得见** |
| `A1380` | **做了** | 缺省基线**按名字往上找**；不带参数也能跑（原来是 `bin/基线/…` ⇒ `rc=2`） |
| `A1383` | **做了** | `filter` 对「没 op / 有 ask op 但不是 `choosecard`」**都出声**；查清 `Primaris Intercessor` 属**① 本来就没 op** |

---

## ② 改动清单

| 文件:行 | 改了什么（≤40 字） | 账 |
|---|---|---|
| `工具/ruleprobe.sh:18-47` | 用 `dotnet build` 退出码判成败，非 0 ⇒ 报错落点 ＋ `exit 1` | `A1466` |
| `工具/ruleprobe.sh:29-31` | 构建日志改 `mktemp`（失败退回本地固定名，⛔ 不静默丢日志） | `A1466` |
| `工具/ruleprobe.sh:35-40` | 失败消息改**单引号**（双引号里的反引号会变命令替换，实测见 §③） | `A1466` |
| `工具/ruleprobe/Scan.cs:123-128` | 每条 op 末尾追加 `/{T:…}`（＋ `DeathWatchTarget` 的 `/{DW:…}`） | `A1477` |
| `工具/ruleprobe/Program.cs:185-290` | 新增 `TargetCols`(28) / `TargetText` / `TargetBlock` / `TargetValue` / `TargetColsDrift` | `A1477` |
| `工具/ruleprobe/Program.cs:167-172` | `Dump`（`seg`/`card`）打**同一批**目标字段（两入口同口径） | `A1477` |
| `工具/ruleprobe/Program.cs:349-364` | `Sentinel.Legacy()` 先摘目标块、`Tail` 再倒数（块里可含 `/`） | `A1477` |
| `工具/ruleprobe/Program.cs:477-495` | 新增哨兵 ④：`TargetCols` 与 `EffectTargetSpec` 一致（机器级守卫） | `A1477` |
| `工具/ruleprobe/Program.cs:512-556` | `DefaultBaseline()` 按名字往上找；报错点明「没传参数」 | `A1380` |
| `工具/ruleprobe/FilterAdd.cs:8-71` | 无 op / 非 `choosecard` 的 ask op **出声**；同名卡全部列出 | `A1383` |
| `工具/ruleprobe/基线/out_baseline.txt` | **重生成**（口径变了基线必须跟；**922 行纯追加**，见 §③） | `A1477` |
| `工具/ruleprobe/README.md:23-56,76-135,295-345` | 四笔各写一节（构建闸 / 目标块 / 哨兵④ / `filter` / 缺省基线） | 四笔 |

**行尾**：6 个文件改前改后一律 **CRLF=0 / 纯 LF**，一行没翻（逐字节数过，非 `file` 目测）。

---

## ③ 实测对照（每笔**改前 → 改后**，全是真跑出来的读数）

### `A1466` —— 构建失败时会不会拿旧 dll 跑

**构造性实测**（仓库外 `D:/tmp/rp_e1/mut/`，向 `Stub.cs` 追加一行 `#error …` = 模拟「别人半成品」；
两个脚本只差 `cd` 那一行，其余逐字取自 HEAD 与工作区）：

```text
########## [改前 · HEAD 版脚本] 构建坏的、旧 dll 在 ##########
Stub.cs(37,8): error CS1029: #错误:“故意打断…”      ← error 打出来了
=== 19:07:04 ruleprobe check ===
  ✓ [索引] … ✓ [索引·scan 路] … ✓ [货币名] … ✓ [目标列] …
=== 哨兵合计：失败 0 条 ===
=== 全池 1120 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===   ← 假绿
rc=0

########## [改后 · 工作区版脚本] 同一断构建 ##########
Stub.cs(37,8): error CS1029: #错误:“故意打断…”
!! 构建失败（dotnet build rc=1）—— **停下**：⛔ 不用旧 dll 接着跑（那会给出假绿读数）。
   台账出处 = `资料/任务_卡组卡牌战斗.md` 的 `A1466`。
   构建输出（末 40 行）：… 1 个错误 …
rc=1
```

**真实世界同款（本次会话撞上的，不是我造的）**：19:06 另一个写手的
`MyGame/Assets/RuleEngine/Core/CreatePool.cs:901` 引用了**还没写出来的** `TalentCutAt` ⇒ `CS0103`。
改后的脚本当场停下并打出错误落点（`rc=1`）；约 1 分钟后他们补上（`CreatePool.cs:942`）构建即恢复。
⛔ **不是我的文件、我一处没碰**。

🔴 **自己踩的一条**（留在注释里了）：失败消息原来写成
`echo "!! … 见 \`A1466\` …"` ⇒ 双引号里的反引号变成**命令替换**，
实测 `工具/ruleprobe.sh: line 34: A1466: command not found`，消息还被吃掉半句
⇒ 现在那几行**一律单引号**。这是「不许静默失败」在 shell 里最容易忽略的一处。

### `A1477` —— 改目标解析时 `check` 看不看得见

**构造性实测**（仓库外池副本，把 `AM48 Recon Operation` 的 desc 句首 `All` 去掉
= **倒放 `A1467` 那次观测**；`seg` 对照确认只动目标块）：

```text
(0) 先证明「摘掉目标块 == 真·旧格式」：摘过的未变异 dump 与【真旧基线】diff = 0 行
(1) 旧格式（摘掉目标块）未变异 vs 变异 …… diff = 0 行        ← 旧 check 完全瞎（= A1477 的病灶）
(2) 新格式 未变异 vs 变异 ………………… diff = 1 行 ['AM48|Recon Operation']
      < … {T:Raw=all enemies|Side=enemy|Kind=any|Count=0|…
      > … {T:Raw=enemies    |Side=enemy|Kind=any|Count=1|…
    端到端 `check`（基线=未变异新格式）
      ⇒ === 全池 1120 张 · 解析差异 1 行 · 改名 0 行 · 新增 0 · 消失 0 ===
      ⇒ ⚠️ 解析差异（前 1 张）：AM48|Recon Operation      >>> rc=1
```

**纯追加自证**（新基线 vs 改前那份基线的副本）：`变化行 922 · 纯追加 922 · 反例 0 · op 逐字不符 0`；
`legacy 段字段数分布 {13: 1258, 14: 1}`（14 = 预存的 `ASH65`，payload 自带 `/`，与 `A1374` 记录同形）；
`带 /{DW: 的 op 数: 1`（`BL77` 的 `DeathWatchTarget`，`Raw=another random enemy troop|Count=1|Random=1`）。
⚠️ 目标块内**自带 `|`** 的 op 实测 **2 个**（`SubtypeFilter=Infantry|Beast` / `Battlesuit|Vehicle`）
—— **原样打、不转义**（取舍照 `A1374`：照 dump 抄进断言的值必须与引擎字段逐字相同）。

**哨兵 ④ 是活的**（构造性：从 `TargetCols` 里删掉 `PickMost` ⇒ 模拟「类里加了字段、清单没跟上」）：

```text
✗ [目标列] `EffectTargetSpec` 与 dump 的目标列清单**对不上**（1 处）：类里有、清单没有: PickMost
   ⇒ 对不上的字段**在 `check` 里看不见**（= A1477 那条缝又开了）。
=== 哨兵合计：失败 1 条 ===     >>> rc=1
```

**改前/改后摘要行**：`解析差异 922 行`（口径刚变、基线未重生成）→ 重生成基线后 `解析差异 0 行 · 哨兵 0 条 · rc=0`。
基线体积 87,013 B → **415,672 B**（×4.8），全池一遍仍 ~2 秒。

### `A1380` —— 不带参 `check`

```text
[改前] $ dotnet bin/Debug/net8.0/wfprobe.dll check
!! 没有基线：D:\4\Unity\工具\ruleprobe\bin\基线\out_baseline.txt        ← 落在 bin/ 下（假路径）
rc=2

[改后] $ dotnet bin/Debug/net8.0/wfprobe.dll check
=== 全池 1120 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
rc=0
```

失败路径也各测一遍（仓库外）：**只挪走基线文件**（留 `基线/` 目录）与**整个目录改名**
⇒ 都报 `!! 没有基线：D:\tmp\rp_e1\mut\工具\ruleprobe\基线\out_baseline.txt（**没传参数** ⇒ 缺省值 = 程序集目录往上找到的第一条 `基线/out_baseline.txt`）` · `rc=2`
—— ⛔ 不再指到不存在的 `bin/基线/`。
🔴 **台账口径订正**（铁律 5，正本不归我改）：原文写「**少两级**」，实测**差一级**
（`net8.0 → Debug → bin → ruleprobe` 是**三级**；原来只上两级 ⇒ 落在 `bin/基線`）。

### `A1383` —— `filter` 对「没有 `choosecard` op 的卡」

```text
[改前] $ dotnet … filter "Primaris Intercessor"
（零输出，rc=0）                       ← 看着像「命令根本没跑」

[改后] $ dotnet … filter "Primaris Intercessor"
!! `Primaris Intercessor(UM14)` **没有任何会问玩家的 op** —— 它的 `desc` 是**空的**
   （`PlayerChooseOps` 对空 `desc` 直接返回空表）⇒ 【本来就没 op】，⛔ 不是过滤条件写窄了。
```

**判据原文两问都答**：① **本来就没 op** —— `UM14` 的 `cards_engine.json` 里 `desc` = `""`（逐字核过，
`PlayerChooseOps` 见到空 `desc` 直接返回空表，`EffectResolver.cs:3094` 一句就是那个短路）；
② **过滤条件写窄了** —— 那一半**也真的存在**：`PlayerChooseOps` 会返回**三种** ask 动词，
而 `filter` 只处理 `choosecard` ⇒ 另外两种**一条都不打**。两条路现在都出声：

```text
$ dotnet … filter "Craftworld Convergence"      # ASH83，ask op = chooseone
-- `Craftworld Convergence(ASH83)` 有 ask op `chooseone` —— 它**不是** `choosecard` ⇒
   本子命令**没有候选池可滤**（池子在别处：`chooseone` 在 `ChooseWhat`、`chooseeffect` 在 `Payload`）
$ dotnet … filter "Hyper-adaptation"            # TL51，chooseeffect → 同形
$ dotnet … filter "Rapid Deployment"            # 主路径照旧：FILTER Rapid Deployment(UM75) src=hand what=[troop] → 586 张
```

**附带查出**：卡名**不是唯一键**（全池 3 个重名：`Terminator` / `Terminator Champion` / `Maulerfiend`）
⇒ 原来 `FirstOrDefault` **静默丢掉**其余同名卡，现在同名全部列出（`filter "Maulerfiend"` ⇒ `BL46` ＋ `EC39` 两张都打）。

### 其余模式回归（都是改后现跑）

`seg` / `card`（`target=[…]` 那一格**逐字未动**，只在其后追加目标块）· `b19test` `PASS 45 · FAIL 0` ·
`jackal` / `emergency` / `suppressor` / `played` 全 `rc=0` · `check` 哨兵 **4 条全绿** · `rc=0`。

---

## ④ 没查清 / 没做的

1. ⛔ **没跑 Unity**（红线）· **没跑 `工具/typecheck.sh`**（红线：固定路径、会与别的写手互相覆盖）。
   本笔只动探针壳，与 `RuleEngineTest.Run` 无关；探针自身的编译由 `dotnet build` 覆盖（**0 告警 0 错**），
   它**连带**编 `RuleEngine/Core/*.cs`（相对路径）⇒ 引擎侧能不能编也顺带验到了。
2. `A1477` 的构造性反例用的是**池副本**（改 `desc` 文本），**不是**改引擎解析器（`RuleEngine/**` 不在白名单）
   ⇒ 观测到的字段变化与 `A1467` 那次**逐字同形**（`Raw` / `Count`），但**没有**端到端验「引擎侧新写一类目标解析」。
3. ⚠️ **`Target.Raw` 含 `;` 时会切错 op**（一行）——**没有做转义**（取舍照 `A1374`），
   **今天实测 0 例**（新基线每行的 `{T:` 次数与 op 数自洽、op 总数 1259 与旧基线逐字相同），
   并**没有**做机器级守卫。含 `/` / `|` 是**允许**的（`Legacy()` 已处理）。
4. `CheckMain` 的临时 dump 路径仍是固定的 `%TEMP%/wfprobe_check.txt`（两个 `check` 同时跑会互相盖）——
   **已知、未动**（超出本笔）；只把**构建日志**改成了 `mktemp`。

---

## ⑤ 顺手发现（一条一句 ＋ 出处；⛔ 一条都没顺手改）

1. 🔴 **同一族的第三条缝还没堵**：`EffectOp.Tail` 与 `EffectOp.ChooseDeadScope`
   **`Dump` 打了、`Scan` 不打** ⇒ 改这两样的笔 `check` 看不见。
   证据：`grep -o "op\.[A-Za-z0-9_]*" Scan.cs | sort -u` = 12 个，`Dump` 段 = 16 个，
   差集 = `{Tail, ChooseDeadScope}`（`Target` / `DeathWatchTarget` 本笔已补）。
   ⚠️ `Tail` 是 `Deal 3 damage to an enemy and stun it` 的 `and` 那半句，源码注释写着
   「**绝不能丢**：丢了就是『打伤害但不眩晕』的静默失效」（`RuleEngine/Core/EffectText.cs:112` 一带）
   ⇒ **建议紧接着 `A1477` 做**（判据形状现成：还是「只追加 ＋ 重生成基线 ＋ 纯追加自证」）。
2. 🔴 **`EffectOp` 有 50 个公开字段，dump 只覆盖 15 个**（`EffectText.cs` 方法体外的字段声明逐行数过）。
   未打进去的 35 个含 `AmountMax`(:69) `Duration`(:77) `Instead`(:183) `Dest`(:193) `AltAmount`(:388)
   `AltCondition`(:391) `RepeatOps`(:531) `Target2`(:559) `BaseOps`(:549) `PerCount`(:501) …（全表见脚本口径）。
   ⛔ 不是「35 个都该打」（有些是内部痕），但**「该打哪些」这条判据本身没有出处** ⇒ 交调度台裁。
3. ⚠️ **`工具/README.md:67` 与脚本头注两头打架**：那里写「看摘要行，**⛔ 不看 `$?`**」，
   而 `工具/ruleprobe.sh:6-8` 写「退出码现在是可信的」，本笔又加了**构建闸**（`rc=1` = 没编成、读数不可信）。
   ⛔ 该文件**不在我的白名单**（只许 `工具/ruleprobe/**` ＋ `工具/ruleprobe.sh`）⇒ 只报不改。
4. ⚠️ **`W_ruleprobe三笔.md`（`A1374`）里的 op 计数已过期**：它记
   `op 的 / 字段数分布: {13: 1257, 14: 1}`（总 **1258**）；**现核**（我改动前那份基线的副本）是
   `{13: 1258, 14: 1}` = **1259** ⇒ 那次之后有人再生成了基线（引擎侧多了 1 条 op）。
   出处 = `资料/普查产出_第十四会话/W_ruleprobe三笔.md` §① 证据 vs `D:/tmp/rp_e1/out_baseline.old.txt`。
5. ℹ️ **`willrun` 现在 `rc=1`**（② 收漏 **6 张** · ⑤ 无消费点 **14 条**）。
   **结构上与本次改动无关**（`WillRun` 不读 dump，本笔一个字没碰它）—— 是引擎/数据侧的既有缺口；
   ⚠️ **但它是不是本会话新出现的，我没查**（判据不足，如实记）。
6. ℹ️ **基线体积 ×4.8（87 KB → 416 KB）**：目标块让每条 op 多 ~250 B。全池一遍仍 ~2 秒、
   用途不受影响；留一笔供将来「列太多要不要收窄」的取舍（收窄的代价是又开一道缝）。
7. ℹ️ **复现夹具坐标**：`D:/tmp/rp_e1/`（`verify_a1477.py` · `strip_target.py` · `mutate_pool.py` ·
   `ruleprobe_OLD.sh` · `out_baseline.old.txt`）＋ 仓库外探针副本 `D:/tmp/rp_e1/mut/`（`OLD.sh` / `NEW.sh`）。
   ⚠️ `D:/tmp` 重启可能被清。
