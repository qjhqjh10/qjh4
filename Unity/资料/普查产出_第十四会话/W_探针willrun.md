# W_探针 willrun —— 修掉「空转 ⇒ 全绿」，给 `ruleprobe` 加 `willrun`（`A1432` / `A1433`）

> 写手报告（执行代理）。**只动了 `Unity/工具/ruleprobe/**`**：没碰 `MyGame/Assets/**`、没碰别的 `工具/*`、
> **没跑 Unity**、**没动 git**、没改 `CLAUDE.md` / `项目任务.md`。
> 临时件在**仓库外** `D:/tmp/wf_willrun/`（两份副本，见 §三·2）。
> 行尾：`Program.cs` / `README.md` / `WillRun.cs` / `ruleprobe.csproj` 全部 **LF**（改动前后逐字节数过）。

---

## 一、修了什么

| 文件:行 | 做了什么 |
|---|---|
| **`工具/ruleprobe/WillRun.cs`**（**新建**，427 行） | 新子命令 `willrun` 的全部逻辑：**两条独立的腿** + 空表三分类 + 段级三分类 |
| `工具/ruleprobe/Program.cs:22-36` | 加 `PoolList` / `PoolRawKeywords` 两个公开出口（见下） |
| `工具/ruleprobe/Program.cs:33-34` | usage 行补上 `willrun` |
| `工具/ruleprobe/Program.cs:56-64` | `willrun [dump <out>]` 分支（`Environment.Exit(WillRun.Run(...))`，**退出码可信**） |
| `工具/ruleprobe/Program.cs:~100` | `LoadPool` 顺手留一份 `keywords` **原文条目**（`rawKw.Add(kws);` + `PoolRawKeywords = rawKw;`） |
| `工具/ruleprobe/Program.cs:395-421` | **`A1433` ②**：`check` 摘要行后面补一段**绝对读数**（原来只打 diff） |
| `工具/ruleprobe/ruleprobe.csproj:14` | `<Compile Include="WillRun.cs" />` |
| `工具/ruleprobe/README.md` | 新增 `## \`willrun\`` 一节（含 `A1433` 的口径换算表）；「怎么跑」一节加一行 |

### 「空表」为什么现在能被区分出来

原来的全池尺子**只有一种问法**（`RuleEngineTest.ReportWillRunMechanism:8452-8495` 的计数形状，
`EffectText.Coverage:1789-1834` 第二层也是同一个形状）：

```csharp
foreach (var op in ops)              // ← ops 是空表时，这个循环一次都不进
    if (!OpHasMechanism(op, …)) { 卡点++; break; }
// ⇒ 空表 = 0 个卡点 = 这张卡算「全通」                       ← 【空转 ⇒ 全绿】
```

修法 = **两条【独立的腿】** 对着账，**空表时把它们分开**：

- **腿 A（执行层）**：`EffectText.WillRunOps(c)` —— 引擎的注册结果；
- **腿 B（卡面）**：`desc` 分句 ∪ `keywords` **原文条目**里 `Head: body` 且 `body` 主解析器解得出 op 的段
  —— **只借归一函数**（`EffectText.Split` / `StripLeadingIcons`），**不借任何注册结果**。

⚠️ **原来那条口径的两条腿共用一个口**（都看 `WillRunOps`）⇒ 空表时**两边一起空转**，所以它看不见 `A1386`。
现在腿 B **完全绕开注册表**，所以「卡面写着、执行层没收」这件事**能被看见**。

**空表三分类**（`A1432` 要的「两者必须能分开」）：

| 档 | 判据 | 计入 rc |
|---|---|---|
| ① **本来就没正文** | 卡面上没有任何「可执行正文段」 | 否（但**必须报出来**，⛔ 不许混进「全通」） |
| ② **收漏了** | 卡面**写着**可解析的正文、执行层**一条都没收到** | **是** |
| ③ **未归因** | 收 0 条、卡面**没有** `Head:` 段，但主解析器对整条 `desc` 解得出 op | 否（**如实标出**，不判） |

**段级再分三档**（一张卡「收了一半」时，那一半**三张表全绿**而卡面那半句就是不发生）：
`收漏`（头没进 `TriggerTexts`，`A1386` 那一族）· `付费段`（`N ☀:` 形状，`WillRunOps` **压根不看**）·
`同头两写`（`desc` / `keywords` 两份正文不一致，后者被「先到先得」丢掉）。

---

## 二、`willrun` 怎么用

```bash
bash d:/4/Unity/工具/ruleprobe.sh willrun                  # 全池读数（≈2 秒，不跑 Unity）
bash d:/4/Unity/工具/ruleprobe.sh willrun dump <out.tsv>   # 附带一份逐卡 TSV
```

**判据**：**②「收漏了」= 0 张**、且段级 `收漏` / `付费段` 都为 0 ⇒ **rc=0**；否则 **rc=1**（⛔ 不许静默绿）。

### 输出五段，每段回答什么

| 段 | 内容 | 怎么读 |
|---|---|---|
| ① 按卡类型 | 卡数 / 收到正文 / 空表 / 本来没正文 / 收漏了 / 未归因 | 「空表」这一列**原来根本不存在** |
| ② **老口径 vs 新口径** | 同一批卡、**同一趟**算出来两行 | 🔴 **两行并排就是「空转 ⇒ 全绿」的现场** |
| ③ 空表的卡逐张 | ② 收漏逐张点名 + ③ 未归因逐张点名 | 这一栏**老口径看不见** |
| ④ 段级总账 | 段数 / 已收到 / 没收下来 + 三档 + **逐条前 60 条** | 「半漏」只有这里看得见 |
| ⑤ 三个口径的换算 | (a) `scan` 列 · (b) `Coverage` · (c) `willrun` · (d) 未实现关键词 | **`A1433` ① 要的换算关系就在这一段** |

### 两个口径的换算关系（`A1433` ①）

| 口径 | 量什么 | 今天读数（2026-10-10 · 真池） |
|---|---|---|
| **(a) `scan` 的列** | `EffectText.Parse(desc)` 的原始残渣，**不过滤** | `unparsed` 非空 **176** 行 · `partial` 非空 **10** 行（合计 186 / 1120；整条 desc 都是残渣的 62 行） |
| **(b) `EffectText.Coverage`** | (a) **先筛掉**「已由别的层接手」的句子（`EffectText.cs:1755` 转调 `CardDef.HandledByOtherLayer`：`When <事件>,` / `Talent:` / `Companion N:` / 光环 / 开局上手） | `tactic`/`unit`/`hero`/`defence` 四类 **部分 0 / 完全不懂 0** |
| **(c) `willrun`** | **执行层**收到没有（腿 A）+ 卡面写着没有（腿 B） | 空表 **167**（本来没正文 137 / **收漏 6** / 未归因 24）· 段级没收下来 **13** 条 |

🔴 **换算关系三句话**：
1. **(a) ≠ 缺陷数** —— (a) 那 176 行里绝大多数是**正常**类（整条 `desc` 都是关键词/天赋/`When` 壳）。
   **两个口径并排会看着像「176 张卡坏了」**（`A1433` ① 的病灶）。
2. **(b) 那四栏全 0 也 ≠ 没问题** —— 它**看不见** `A1386`（正文在 `keywords` 里、整条 `desc` 可能就是空的）。
3. **(b) 与 (c) 是两条独立的腿** ⇒ **互相看不见对方的洞，两条都要看**。

👉 **`A1433` ② 也一起做了**：`check` 的摘要行后面现在紧跟一段**绝对读数**
（`全池 1120 行 · unparsed 非空 176 行 · partial 非空 10 行`）+ 上面这套换算的提示。
原来那行只有 diff，「解析差异 0 行」被读成「没问题」，其实只是「**与基线一致**」。

---

## 三、读数

### 3·1 真池实跑（仓库现状 · `cards_engine.json` 1126 张）

```text
$ bash d:/4/Unity/工具/ruleprobe.sh willrun      # rc=1

  --- ① 按卡类型 ---
  类型       卡数   收到正文 空表   本来没正文  收漏了  未归因
  [tactic]   445    444      1      1           0       0
  [unit]     586    447      139    109         6       24
  [hero]     56     29       27     27          0       0
  [defence]  39     39       0      0           0       0

  --- ② 🔴 老口径 vs 新口径（同一批卡、同一趟算出来）---
  老口径（= `RuleEngineTest` 报表的形状：只对 WillRunOps 返回的 op 逐条问 OpHasMechanism）
    tactic 445/445 · unit 586/586 · hero 56/56 · defence 39/39 —— 全通      ← ⚠️ 把「空表」也算全通
  新口径（本子命令）
    tactic  收到 444   空表 1    （本来没正文 1 / 收漏 0 / 未归因 0）
    unit    收到 447   空表 139  （本来没正文 109 / 收漏 6 / 未归因 24）
    hero    收到 29    空表 27   （本来没正文 27 / 收漏 0 / 未归因 0）
    defence 收到 39    空表 0    （本来没正文 0 / 收漏 0 / 未归因 0）

  --- ③ ② 收漏了：6 张（全是「付费段」那一档）---
    SOR27 Canoness   `2 ☀: Gain Armour 1` / `4 ☀: Gain +2 [Attack] and +2 [Ranged]`
    SOR11 Crusader   `3 ☀: Gain +1 [Attack] and Armour 2`
    SOR12 Preacher   `5 [faith]: Gain Shield`
    SOR23 Retributor `1 ☀: Deal 3 damage to a random enemy`
    SOR24 Seraphim   `3 ☀: Gain +1 [Ranged] and +1 Health`
    SOR42 Exorcist   `8 [faith]: Deal 5 damage to all enemies`

  --- ④ 段级总账 ---
    卡面带正文的段 323 条 · 已收到 310 条 · 没收下来 13 条（落在 11 张卡上）
      · 收漏      0 条
      · 付费段   11 条 ← 计入 rc（另有 SOR68 两条、SOR7 一条、SOR40 一条 · 见逐条表）
      · 同头两写  2 条（ASH79 `Strike:` / DA9 `Agenda:`）← 不计入 rc

  --- ⑤ (a) unparsed 非空 176 行 · partial 非空 10 行（合计 186 / 1120）
      (b) 四类 部分 0 / 完全不懂 0
      (c) 空表 167 · ① 137 · ② 收漏 6 · ③ 未归因 24 · 段级 13 条
      (d) 未实现关键词 = 0 个
```

### 3·2 构造性实测（**都在仓库外的副本上做**）

副本布局（`Program.ResolveOutDir` 从程序集往上找 `MyGame/Assets` ⇒ 副本**自带池、自带 Core**）：

```
D:/tmp/wf_willrun/copy/{工具/ruleprobe/*, MyGame/Assets/RuleEngine/Core/*.cs,
                        MyGame/Assets/RuleEngine/Resources/cards_engine.json}
D:/tmp/wf_willrun/copy2/…（同上，另一份变异）
```

**副本自证**：不打了变异的 `copy` 跑出来与仓库**逐项相同**（`[unit] 586 447 139 109 6 24`）⇒ 副本口径没问题。

#### 实测 A：`CardDef.RoutableTriggers` 去掉 `KeywordTable.Rally`（「缺词」那一类改动）

只删了 `RoutableTriggers` 里那一个词（⚠️ `BodyKeywords` 里同名的另一个**没动** —— 那两处是两张表，
按字节偏移只切第 1 处）。

```text
                        老口径（修前那条判据）            新口径（willrun）
  unit                  586/586 —— **全通** 🔴假绿      收到 379 · 空表 207（收漏 74）   ✅报出来
  段级                  ——（老口径没有段级）             没收下来 98 条（收漏 84 · 付费段 12 · 同头两写 2）
  rc                    ——                              rc=1
```

点名样例：`ASH20 Howling Banshee \`Rally: Stun an enemy\`` · `BL13 Chaos Sergeant \`Rally: Gain a Dark Pact\`` …

#### 实测 B：**照 `A1386` 原样重演**（`survivor` / `sacrifice` 两个词去掉 + 一张 `desc` 为空、正文只在 `keywords` 里的卡）

副本的池多加一张 `ZZSYN01`（`type=unit`、`desc=""`、
`keywords=["survivor 3","Sacrifice: Gain +1 Attack"]` —— **就是 `W_SurvivorSacrifice族.md` 那次实测卡**），
池变成 1127 张。

```text
                        老口径                            新口径
  unit                  587/587 —— **全通** 🔴假绿      收到 447 · 空表 140（收漏 7）    ✅报出来
  点名                  ——                              ZZSYN01 Synthetic Survivor Test [unit]
                                                        —— 收漏；卡面段：`Sacrifice: Gain +1 Attack`(keywords[1])
  段级                                                     收漏 1 条（= ZZSYN01）
  rc                    ——                              rc=1
```

🔴 **这一条就是 `A1432` 的现场**：正文在 `keywords` 里、`desc` 是空的 ⇒ `Coverage` 那四栏
（量的是 `desc`）**看不见它**，老口径把它算「全通」，**只有腿 B 从 `keywords` 原文抽段才看得见**。

### 3·3 三条标准验收（改完现跑 · 都没退步）

| 项 | 读数 |
|---|---|
| `dotnet build` | **0 个警告 / 0 个错误** |
| `check`（哨兵） | **哨兵合计：失败 0 条** · `解析差异 0 行 · 改名 0 行` · **rc=0** |
| `b19test` | **PASS 45 · FAIL 0** |
| 行尾 | `Program.cs` 1061 行 / `README.md` 198→约 300 行 / `WillRun.cs` 427 行 —— **CRLF 0**（全 LF） |
| `git diff --numstat`（`工具/ruleprobe/`） | `Program.cs 233/4` · `README.md 163/10` · `csproj 1/0` —— **不是整篇重写**（整篇会是 ~460/460） |

⚠️ `git diff --numstat` 里 `BattleProbe.cs 6/3` · `Scan.cs 30/2` · `基线/out_baseline.txt 922/922`
是**本会话之前就有的改动**（会话开头 `git status` 就是 `M`），**不是本笔动的**。

---

## 四、我没做的 / 判不了的

1. 🔴 **没改引擎**（`MyGame/Assets/**` 是黑名单，且 `RuleEngine/Core/**` 有别的写手）⇒
   **本笔只把「空表 / 收漏」报出来**，**判决（补哪个词、补哪个消费点）我没有做** —— 按 `A1432`
   的验收标准，本笔要的是「能被区分出来」+「一条命令出读数」+「构造性实测」，**这三条都做到了**。
   ⚠️ **`RuleEngineTest` 那边的报表生成器（`ReportWillRunMechanism:8452-8495`）本身还是老形状** ——
   它在 `Assets/` 里，**本笔动不了**。⇒ 需要另派一笔把它也改成「空表出声」（本报告 §五·1 已列为待办候选）。
2. ⚠️ **③「未归因」那 24 张我没定性**。已判明的只是「`WillRunOps` 收到 0 条 + 卡面没有 `Head:` 段」，
   它们**多半**由 `WillRunOps` 之外的层消费（唯一一处读 `u.Card.Desc` 的代码是
   `EffectResolver.ResolveAtTurn:4792` 的回合起止从句扫描；另有 `CollectStaticBattleRules` /
   `CollectAuras` 那一族不是 op）—— **但「哪几张是真缺陷」要另派只读诊断**，本笔**不判**（⛔ 不猜）。
3. ⚠️ **`tactic` / `defence` 没做段级对账**（取舍见 README「三条别推翻的取舍」1）。
   ⇒ 这两类卡只有「整条 desc 解不出 op」那一档，**它们内部「收了一半」我量不到**。
4. ⚠️ **没跑 Unity**、**没跑 `RuleEngineTest.Run`** ⇒ 我**没有**直接看到
   `_tmp_view/willrun_mechanism.md` 在变异后长什么样；「老口径全通」那一行是**我在同一趟里照
   `RuleEngineTest.ReportWillRunMechanism:8465-8475` 的计数形状复算的**（空表不进循环 ⇒ `OldPass=true`），
   **代码形状逐行核过**（该文件 `:8467` 那个 `foreach` 就是判据），但它**不是**那次真跑出来的产物。
5. ⚠️ **`基线/out_baseline.txt` 我没重生成**（本笔没改 `Scan.cs` 的打列行为）——
   `check` 仍是 `解析差异 0 行`。
6. ⚠️ `A1380`（`check` 默认基线路径少两级）**按简报要求没碰**。

---

## 五、顺手发现（一条一句话 + 出处）

1. 🔴 **`CardDef.RoutableTriggers` 里 `Rally` 同时出现在 `BodyKeywords`，两张表各有一份同名条目**
   —— 变异时按字符串切必须**按字节偏移只切第 1 处**（`CardDef.cs:173` vs `:306`），
   否则会**连带改掉 `BodyKeywords`**、把结论污染。出处：`MyGame/Assets/RuleEngine/Core/CardDef.cs:172` / `:305`。
2. 🔴 **6 张 Sororitas 单位卡的 `N ☀:` / `N [faith]:` 付费能力写在 `desc` 里，今天没有任何一层消费它**
   （`SOR27 Canoness` / `SOR11 Crusader` / `SOR12 Preacher` / `SOR23 Retributor` / `SOR24 Seraphim` / `SOR42 Exorcist`）。
   判据两条：① `WillRunOps` 的 unit 支六个来源里**没有**「付费能力段」；
   ② 全仓**唯一**读 `u.Card.Desc` 的代码是 `EffectResolver.cs:4792`（只收回合起止从句）。
   ⇒ **这是 `A1432` 那个形状在今天的真池上**（不是假想）。
3. 🔴 **全池 1126 张里，`keywords` 含 `ability` 的 = 0 张** ⇒ `CardDef.Ability`（`CardDef.cs:135`）
   **恒为 null** ⇒ `RuleCore.UseAbility` / `CanUseAbility`（`RuleCore.cs:6289` / `:6272`）那条
   「主动技能」路**今天对任何卡都走不到**。出处：`cards_engine.json` 全量扫 `ability` 前缀 = 0 命中。
   ⚠️ 与 §五·2 是**同一件事的两面**（SOR 那 6 张的付费技能本该走这条路）。
4. ⚠️ **`desc` 与 `keywords` 里同一个触发头写两份、正文不一样**（实测 2 张）：
   `ASH79 Howling Banshee Exarch`（`Strike:` 两处 `Give +1 [Attack] to your units` vs `Give +1 to your units`）·
   `DA9 Ravenwing Bikes`（`Agenda:` 两处 `Gain 1 Quest Point` vs `Gain 1`）。
   `AddTriggerOp` 是**先到先得**（`CardDef.cs:1321`），后者被**静默丢掉**。
   ⇒ 是**数据不一致**（该以卡面为准，要核 PnP），不是引擎 bug。
5. ⚠️ **`coverage` 与 `scan` 的差不是 176 条都在「别的层」里**：`scan` 的 `partial` 列 10 行
   与 `unparsed` 176 行**互不重叠**（合计 186）⇒ 两列要**分别报**，合成一个数会与 `check` 的绝对读数对不上。
   出处：`check` 的绝对读数段（新增）与 `willrun` ⑤(a) 现读。
6. ⚠️ **`EffectOp.Source` 在不同采集点不是同一个东西**：`AddTriggerOp:1340` 存的是
   `Parse(body)`（`Source` = 正文半句），`CollectSpiritOps:1000` 存的是 `Parse(Desc)` 挑出来的 op
   （`Source` = 整句）⇒ **任何拿 `Source` 逐字对账的探针都会把 ASH 那一族灵魂石卡误报成「收漏」**
   （我第一版就踩了，50 条假警报）。出处：`CardDef.cs:1000` vs `:1340`。

---

## 六、给调度台的下一步（⛔ 我没做，只列出来）

| # | 事 | 为什么 |
|---|---|---|
| 1 | **把 `RuleEngineTest.ReportWillRunMechanism:8452-8495` 也改成「空表出声」** | 它才是**断言的真宿主**；本笔只修了探针侧（`Assets/` 不在本笔白名单） |
| 2 | **`SOR27/11/12/23/24/42` 这 6 张的付费能力要有人接**（`N ☀:` 形状） | 卡面印着、今天一条都不发生（§五·2/3） |
| 3 | **③「未归因」24 张派只读诊断定性** | 它们**要么**由别的层正常消费、**要么**是同一族的洞，本笔不判 |
| 4 | **`ASH79` / `DA9` 的 `desc` × `keywords` 分歧核 PnP 卡图** | 数据不一致，得看卡面（铁律 7） |
