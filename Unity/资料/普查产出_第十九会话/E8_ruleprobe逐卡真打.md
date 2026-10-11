# E8 · `ruleprobe` 逐卡真打（`A1431` 主笔 ＋ `A1382` / `A1437` 判定）

> **执行写手交件**（2026-10-11）。**不跑 Unity** · **不动 git** · **没碰 `MyGame/Assets/**` 任何 `.cs`** ·
> **没动任何正本**（`项目任务.md` / `CLAUDE.md` / `资料/*.md` 正本一字未改）。
> 白名单内改的文件：`工具/ruleprobe/{PlayProbe.cs(新),Program.cs,WillRun.cs,ruleprobe.csproj,README.md}`。
> 临时件全在工程外：`D:/4/_tmp_probe/{play_all.tsv,play_all.txt,willrun.tsv,scan_now.txt,baseline_mutated.txt}`。
> 判据出处 = `资料/普查产出_第十四会话/逐卡效果检查_方法与边界.md` §1 / §2 / §4 ＋ `资料/任务_卡组卡牌战斗.md` 的 `A1431` / `A1382` / `A1437` 行。

---

## 一、改动清单

| 文件:行 | 改了什么（≤40 字） | 服务 |
|---|---|---|
| `工具/ruleprobe/PlayProbe.cs`（**新**，956 行） | 新增 `play` —— 逐卡真打 + 局面指纹对照 + 全池 sweep | `A1431` |
| `工具/ruleprobe/Program.cs:148` | 挂上 `case "play"`（`play <id>` / `play all [dump <out>]`） | `A1431` |
| `工具/ruleprobe/Program.cs:40-92` | 新增「基线指纹」哨兵⑤的判据与两个**钉住**的 md5 常量 | `A1382` |
| `工具/ruleprobe/Program.cs:575-617` | 哨兵⑤「卡池 md5 × 基线 md5 配对」实现（计入 rc） | `A1382` |
| `工具/ruleprobe/Program.cs:468` | `Sentinel.Run` 加 `baselinePath` 形参（只哨兵⑤用） | `A1382` |
| `工具/ruleprobe/WillRun.cs:600-613` | 抽出 `IsPaidHead` / `HeadOfBody` 成**公开**（避免第二份判据） | `A1431` |
| `工具/ruleprobe/WillRun.cs:583` | `Classify` 改调 `IsPaidHead`（**行为逐字不变**） | `A1431` |
| `工具/ruleprobe/ruleprobe.csproj:21` | 加 `<Compile Include="PlayProbe.cs" />` | `A1431` |
| `工具/ruleprobe/README.md` | 新增 `play` 一节 + 哨兵⑤一节 + 真池读数 | `A1431`/`A1382` |

**行尾**：全部文件实测**纯 LF**（`CRLF 0`）—— 没用过 `sed -i`；`.cs` 全部用 Edit 工具改。

---

## 二、实测对照（**真实命令 + 真实输出**）

### 2·1 `A1431`：这条尺子**原来没有**

```bash
git show HEAD:Unity/工具/ruleprobe/Program.cs | grep -c 'case "play"'
# 0        ← HEAD 版**没有** play 分支；`Main` 的 switch 落到 `default: Dump(args[i])`
#          ⇒ 旧版敲 `play SOR6` 会把 `play` / `SOR6` 当**卡面原文**去解析（打印 `SEG = [play]`），
#            既不报错、也不给任何读数（一声不响地做了另一件事）
```

### 2·2 `A1431`：**同一张卡，四条尺子的读数**（`BL68 Black Crusade`）

```bash
# ① 旧尺子 A —— `check`（与基线比）：那一行**没变**，摘要行只说「解析差异 N 行」
# ② 旧尺子 B —— 基线里 BL68 那一行（完全解析，unparsed/partial 两列都空）：
BL68|Black Crusade|||deploy/pool///4/troops/0/0/…{T:null};
# ③ 旧尺子 C —— `willrun` 逐卡 TSV（runOps=1 · oldPass=1 · faceBodies=0 · orphans=0 · emptyKind 空）
BL68  Black Crusade  tactic  1  1  0  0        0  0    ← **不在「收漏/付费段/无消费点」任何一栏**
# ④ 🆕 本件 —— `bash 工具/ruleprobe.sh play BL68`
#    实得变化（除付费·出牌本身·自身落场）: 0 条
#    ── 引擎自己的事件日志（末尾 14 条）──
#    战术卡：「Deploy 4 random troops from your deck」部署不了 —— 按兵种「troops」在卡池里一张都没筛到（**这条没生效**）
```

⇒ 前三条**全绿**，第四条把「它没结算」摆了出来。**这就是 `A1431` 要的那一层。**

### 2·3 `A1431`：全池 sweep（**改后读数**，2026-10-11 实测）

```bash
bash d:/4/Unity/工具/ruleprobe.sh play all dump D:/4/_tmp_probe/play_all.tsv
# real 0m5.0s   rc=0
```

```text
=== play all · 逐卡真打（不跑 Unity）· 池 1126 张 ===
  真打成功（rc=0）        1115 张
  打不出去                11 张（分因：ErrNoTargetAvailable 11）
    ⚠️ 其中 `ErrUnimplemented` **0 张**
  引擎抛异常              0 张
  督军档（另法测）        56 张
  --- ② 候选（卡面写着正文 · 打得出去 · 局面一个数字都没动 = 411 张）---
     ── A 打出即结算 · 41 张  ← 🔴 这一档最可疑
     ── C 部署时该发生 · 96 张
     ── B 等事件/等主动用 · 274 张
```

### 2·4 `A1382`：哨兵⑤的**构造性实测**（仓库外的基线副本，只改 1 个字节）

```bash
cp 工具/ruleprobe/基线/out_baseline.txt D:/4/_tmp_probe/baseline_mutated.txt
# 把第一行第一个字节改成 'X'（= 模拟「重生成基线抹平一次真回归」）
bash d:/4/Unity/工具/ruleprobe.sh check D:/4/_tmp_probe/baseline_mutated.txt
```
```text
  ✓ [索引] …   ✓ [索引·scan 路] …   ✓ [货币名] …   ✓ [目标列] …
  ✗ [基线指纹] 🔴 **卡池没动、而基线文件被改写了** —— 这正是 `A1382` 那条洞：「任何真回归 ＋ 重生成基线 ⇒ 静默变绿」。
             卡池 md5=cfc13db1e5b96fa231a106d1de1ead6a（与钉住的一致 ⇒ **解析器的输入没变**）
             · 基线 md5=0903f8d39fc87f3277d1fb6c90806a82（钉住的是 22ea1cde5e3990615eb6a63becd86e94）
=== 哨兵合计：失败 1 条 ===
```
```bash
# 原基线（未改） ： ✓ [基线指纹] 卡池与基线的指纹都与钉住的一致（基线没被静默改写） · 哨兵 0 条 · rc=0
# 变异基线       ： ✗ 哨兵 ⑤ 红 1 条 · **rc=1**   ✅（这就是 A1382 要的：基线可被重写，哨兵不能被）
```
🔴 **重钉只能手改源码**（`Program.PinnedPoolMd5` / `PinnedBaselineMd5`）—— ⛔ 没有「一条命令重钉」的口子。

### 2·5 回归（**改了 `WillRun.cs` ⇒ 必须复跑 `willrun`**）

```bash
bash 工具/ruleprobe.sh b19test     →  ==> PASS 45 · FAIL 0      （与改前一致）
bash 工具/ruleprobe.sh willrun     →  收漏 6 · 未归因 24 · 段级没收下来 13 条 · ⑤ 无消费点 14 条
                                      （与本次改动前的读数**逐项一致** ⇒ `IsPaidHead` 抽取没改行为）
bash 工具/ruleprobe.sh check       →  哨兵 0 条失败 · 但**解析差异 1 行**（见下「没做的」）
```

---

## 三、这把尺子**现在能回答什么 / 还不能回答什么**（`A1431` 的核心交付）

### ✅ 能回答（本仓第一次有）

| 问题 | 读数在哪 |
|---|---|
| 这张卡**打出去之后，局面的数字动了没有** | `play <id>` 的「变化逐条」+「实得变化」那一行 |
| 动了**哪几个具体数字** | 逐条：棋盘每格 `攻/远/甲/血 {关键词}` · 手牌/牌库/弃牌堆**逐张 id** · 手牌总价 · 伤害累计 · 出兵数 · **信仰/灵魂石/任务点** |
| 引擎**自己**对「什么都没发生」怎么解释 | 「引擎自己的事件日志」段（例：`部署不了 —— 按兵种「troops」在卡池里一张都没筛到（这条没生效）`） |
| 全池里**有多少张**属于这一类 | `play all`：1115 真打 / 0 抛异常 / **411 候选**（A 41 · C 96 · B 274），**≈5 秒** |

### ❌ **不能**回答（如实标着）

1. **对错** —— 判据在**卡面**上（`desc`/`descZh`/PnP 成品卡图）。本工具只摆**实得数字**，
   ⛔ 绝不用我们自己的实现反填期望值（那就是「拿我们的实现证明我们的实现」）。
2. **档 B 那 274 张**（`Strike:`/`Slay:`/`Duty:`/`When …`/`Teleport:`/`Ambush:`/`Survivor:` …）——
   **本夹具产生不了那些事件** ⇒ `other=0` 是**预期的**。要测它们得另写「打一发 / 让它死 / 让它抽到」的专属夹具。
3. **只改后续回合的**（`For the rest of this battle, at the start of your turn …`）·
   **要靠跨句承接的**（`A1428` 那族）—— 会落进候选，但**是量具盲区，不是证据**。
4. **牌库「顺序」**（`put it at the top of your deck`）只按 id 多重集比 ⇒ 只看得出「换了哪几张」，
   看不出「放到第几张」。
5. **档 A 的 41 张里仍有一部分是夹具盲区**（逐张看过那一屏）：
   `Reanimate a friendly Remnant` 那族 **5 张**（`SAU26`/`SAU46`/`SAU49`/`SAU52`/`SAU67`）—— 夹具里**没有 Remnant**；
   `Choose a friendly troop that died …` 那族 **5 张**（`EC47`/`SAU47`/`SAU64`/`SOR53`/`GSC62`）—— 夹具里**没人死过**；
   `GSC_Improvised_Barricade` —— **卡面自己就写着 `Does nothing`**。
   ⇒ **候选 ≠ 缺陷清单**（41 张里这几族先划掉，剩下的才值得逐张对卡面）。

---

## 四、`A1437` / `A1382` 的归属判定（按判据原文）

### `A1382` —— ✅ **属于本文件的活，已做**

判据原文（`资料/任务_卡组卡牌战斗.md:36`）：**「先查证『本仓有没有可当参照物的东西』」** ＋ 出处
`资料/普查产出_第十三会话/W5d_A1339探针口径.md:173` 的「**这是文档级的防呆，不是机器级的**」。

**查证结果（「本仓有没有参照物」）**：

| 候选参照物 | 在不在 | 能不能用 |
|---|---|---|
| `RuleEngineTest.Run`（Unity 断言宿主） | 在 | ✅ 真参照物，但**跑不起内环**（1–9 分钟、且本件禁令不跑） |
| **`cards_engine.json`（卡池文件）** | 在 | ✅ **作者是另一个工具**（`工具/gen_cards_engine.py`），探针**只读** ⇒ **正是「不在本工具写权限内」** |
| 基线文件本身 | 在 | ❌ 本工具能重写 ⇒ 自己不能当自己的参照物 |
| `git` 里那份基线 | 在 | ✅ 等价、但要起进程跑 git（重）；改为**把基线 md5 钉进源码**，同效且更轻 |

⇒ **做的是**：把「**卡池 md5 × 基线 md5**」这一对钉进 `Program.cs` 的**源码常量**，每次 `check` 现算。
「输入（卡池）没变、而基线变了」⇒ 出声并计入 rc。**构造性实测见 §2·4**。
⚠️ **已知的假阴**：真回归 ＋ 重生成基线 ＋ **卡池也在同一批里变了** ⇒ 看不见（卡池那一半只打印、不失败）。如实标着。

### `A1437` —— ⚠️ **不属于本文件的活；且按判据已可判为「原版本身就没有」**

判据原文（`:51`）：「**先查证『原版哪些卡真的有 ability』（判据 = PnP 卡图 ＋ 反编译），再定是我们漏建还是本来就没有**」。

三条判据**逐条查过**：

| # | 判据 | 查法 | 结果 |
|---|---|---|---|
| ① | 原版反编译 | `grep -rn "ability" d:/2/tools/decomp_full/` | **16 处，全部**是 `Reliability` / `Reachability` / `Scrollability`（Photon / Ads / ScrollRect 那些第三方类）⇒ **`Ability:` 关键词零命中** |
| ② | 解包资源字段 | 原始卡表 `d:/4/Unity/数据/游戏数据/card_stats.json`（1213 张，OCR 产物）的 `keyword`/`keywords` 两列 | 含 `abilit` 的 **0 张** |
| ③ | 卡面文字 | `cards_engine.json` 1126 张：`keywords` 含 `ability` = **0 张**；`desc`/`descZh` 提到 ability 的 **5 张** —— `ASH_Bright_Lance_Vyper`（`Spirit Stone ability`）· `AM2`（`reload its Duty ability`）· `UM_Author_of_the_Codex`（`Codex ability`）· `UM95` · `UM_Oath_of_Moment` | **5 张说的都是【别的系统的能力】**，没有一张把 `Ability:` 当关键词印 |

🔴 **结论**：**原版本身就没有 `Ability:` 这个关键词** —— 与 `RuleEngine/Core/CardDef.cs:2570-2574` **自己的注释**逐字一致
（「**这是本工程自己定的关键词** —— 原版的『替代行动』（职责 Duty / 狂暴 Ferocity / 祈祷 Pray / 议程 Agenda）
是一族用法各异的东西，v1 先收成一条……**不是原版关键词**」）。
⇒ `A1437` 落 **`CLAUDE.md` 铁律 11 的例外①（原版本身就没有）**，**不是我们漏建**。
⇒ 而「主动技能」这个概念在原版**确实有对应物**（替代行动那一族），那一族**已经实现**
（`CardDef.RoutableTriggers` 收了 `Duty`/`Pray`/`Ferocity`/`Agenda`，消费点 = `RuleCore.UseAlternative`）。
⇒ **本工具能做到的只有「把这条判据查实」**；「要不要把 `UseAbility` 那条死路删掉」是**引擎侧的活**，⛔ 不在本件白名单。

---

## 五、没查清 / 没做的（如实）

1. 🔴 **`check` 现在红 1 行 —— 不是本件造成的，是另一个写手的活**：
   `解析差异 1 行`，那张卡 = **`SOR6`**，diff 落在它**第二条 op 的目标块**上：
   `{T:Raw=(未写目标：有施放者就是施放者自己，否则己方全体)|Auto=1|Subjectless=1…}`
   → `{T:Raw=your units|Auto=0|Subjectless=0…}`。
   ⇒ **有人正在做 `A1428`（跨句承接）**，改的是 `MyGame/Assets/RuleEngine/Core/{EffectText,CardDef,EffectResolver,RuleCore,CreatePool}.cs`
   （`git diff --numstat` 现读：`CardDef 186/22` · `CreatePool 123/63` · `EffectResolver 187/14` · `EffectText 109/1` · `RuleCore 255/36`）。
   ⛔ **我没碰那些文件、也不该碰**。⚠️ 这一条**不影响**本件的读数（`play` 走的是同一份 Core，但读数自带前后对照）。
   🔴 **副作用**：谁若为了把它变绿而重跑 `scan 基线/out_baseline.txt`，**哨兵⑤会当场红**（正是本件做的第二件事）。
2. ⚠️ **夹具的 11 张「打不出去」全是 `ErrNoTargetAvailable`**（夹具里没有合法目标），
   **没有一张 `ErrUnimplemented`** ⇒ 今天拿不到「卡面写着正文而打不出去」的实例。
3. ⚠️ **SOR6 的 `4 Energy:` 付费前缀按「应付 = 卡费 2 ＋ 前缀 4 = 6」算** —— 依据是
   `EffectOp.Cost > 0 && !CostShared` 那两条（`seg` 实测 `cost=4 ck=energy shared=0` ＋ 下一条 `shared=1`）。
   **原版有没有「付费前缀另算」这条口径，本件没查**（那是结算层/反编译的账）。
4. ⚠️ **没有做「逐卡真打」的并行切块**（`A1431` 推荐路线的 ③）。
   实测**整趟 5 秒**（1126 张全跑）⇒ **切块没有意义**，故收口成一条 `play all`（= 推荐路线的 ④）。
   ⛔ 这不是「省了」，是把「按阵营 × 类型切 39 块」这件事**证伪了**（全部一起跑比切块更快）。
5. ⚠️ **`play` 无断言**（它只摆读数、不判对错）⇒ 它**不进任何 Unity 自检**，也**不该进**
   （判据在卡面上，机器判不了）。⚠️ 它的**退出码**只认硬失败（抛异常 / `ErrUnimplemented`）。
6. ⚠️ **本件没跑 Unity**（禁令）⇒ `play` 的读数与 `RuleEngineTest.Run` 那份报表**没对过账**。

---

## 六、顺手发现的

1. 🔴 **`BL68 Black Crusade` 真打时什么都没发生** —— 引擎自己的日志：
   `战术卡：「Deploy 4 random troops from your deck」部署不了 —— 按兵种「troops」在卡池里一张都没筛到（**这条没生效**）`。
   出处：`bash 工具/ruleprobe.sh play BL68`（当日实测）。⛔ 判决归调度台；**本件只报**。
2. 🔴 **临时卡（`ephemeral`）在「先手打出一张卡」的夹具里会凭空消失** ——
   `RuleCore.cs:1340` 的 `SweepEphemeral(ctx, ctx.Active)` 在 `EndTurn` 把手牌里的临时卡弃掉。
   **这是引擎对的行为**（`资料/临时卡Ephemeral_设计与实现计划.md`），但它是**量具**的一号坑：
   不照顾它，**98 张 `tactic`** 全都读不到数（实测：A 档候选一度虚高）。
3. 🔴 **防御卡（39 张）不在先手手里** —— `RuleCore.BuildPlayer` 把 `type == "defence"` 从牌库**分流出去**，
   由 `GrantDefenceCard` 只发给**后手**。⇒ 任何「先把卡摆进牌库、再等它上手」的夹具
   **对 39 张防御卡 + 98 张临时卡 = 137 张**是**结构性失效**的（本件已修，形状照 `RuleCore.SetDefensiveCard`）。
4. ⚠️ **`Regiment`（团）不是部署时触发，是【攻击时】触发** —— `RuleCore.cs:4027-4031`
   （`团（Regiment）：和 Mob 成对，差别只在远程`；消费点 `FireTriggerAt(ctx, attacker, KeywordTable.Regiment, p, atkSlot)`）。
   ⛔ 拿「卡面有正文 + 部署后没变化」直接当缺陷会**误报一整族**（`AM12`/`AM14`/`AM26`/`AM70` …）。
5. ⚠️ **`gameplayvariables.MaxEnergy` 只要 ≥ 10 就进加时** —— `RuleCore.cs:1009-1034`
   （「后手玩家最大能量达 10 时进入」）。夹具把上限设成 30 是为了让 `Refill all your Energy` 看得见，
   **代价是夹具局从第 2 回合起多抽一张**。已写进 README 的夹具表。
6. ✅ **`GSC_Improvised_Barricade` 的卡面就是 `Does nothing`** —— 它会出现在候选清单里，
   但那是**卡面自己说的**（出处：`bash 工具/ruleprobe.sh play GSC_Improvised_Barricade`）。
   ⇒ 造候选清单时**别拿「候选 == 缺陷」冒充结论**的又一个实例。
