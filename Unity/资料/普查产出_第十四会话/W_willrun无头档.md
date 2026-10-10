# W_willrun 无头档 —— 给 `ruleprobe willrun` 补第五档「无头正文逐句报账」（`A1459`）

> 写手报告（执行代理）。**只动了 `Unity/工具/ruleprobe/**`**：没碰 `MyGame/Assets/**`（`Core/` 此刻有别的写手）、
> 没碰别的 `工具/*`、**没跑 Unity**、**没动 git**、没改 `CLAUDE.md` / `项目任务.md`。
> 临时件在**仓库外** `D:/4/_tmp_probe3/`（三份副本，见 §三·2）。
> 行尾：`WillRun.cs` / `README.md` 改完**仍是 LF**（`CRLF 0`，见 §三·3）。

---

## 一、新档的判据

### 它治的是哪道缝

`willrun` 原来四档（① 本来没正文 / ② 收漏 / ③ 未归因 / ④ 段级三档）**入口全部以 `Head:` 为锚** ——
腿 B 的 `AddFaceBody` 在 `s.IndexOf(':') <= 0`（= 这一句**没有头**）时**直接 return**。

⇒ 「**卡面没有头、又没被消费**」的句子**哪一档都进不去**：
它**有**正文，所以不是 ①；③ 的判据是「**腿 A 收到 0 条**」，而这一族偏偏出现在**卡上还有别的 op** 的卡上
⇒ ③ 也进不去（`SW23` 的腿 A = 1，是那条 `Rally`）；腿 B 结构上不成段 ⇒ ④ 也照不到。

### 第五档的四道闸（缺一条都会误报）

| # | 闸 | 判据 |
|---|---|---|
| 1 | **无头** | `StripLeadingIcons(seg.Trim())` 之后 `IndexOf(':') <= 0` —— 🔴 **与 `AddFaceBody` 拒收用的是同一个表达式**（⛔ 别在探针里另写一套「什么叫头」） |
| 2 | **解得开** | `EffectText.Parse(seg)` 出得来 ≥1 条 op |
| 3 | **腿 A 没收到** | 那批 op **一条都不在** `EffectText.WillRunOps(c)` 里，按 `WillRun.Sig`（`verb/amount/payload/target`）比 —— **与腿 B 同一把尺子**、⛔ 不比 `Source` |
| 4 | **没有任何别的层认领** | `CardDef.HandledByOtherLayer(c, seg)` 空 ∧ `EffectText.AtTurnClauses(seg)` 空 ∧ `CardDef.CostWhens` 里没有 `Body == seg` |

四道闸全过 ⇒ 标 **`无消费点`**（**计入 rc**）。

### 为什么不会误报 —— 每一条都有「不设闸就会误报」的实测

| 闸 | 少了它会误报什么（**实测数**） | 为什么它不是「猜」 |
|---|---|---|
| `HandledByOtherLayer` | 事件层 / 天赋 / 伴生 / 开局上手 / 光环 / 静态改战斗规则 **25 条** | **转调引擎那一处**（`CardDef.cs:1921`），⛔ 探针不另写词表 |
| `AtTurnClauses` | **回合起止从句 19 条**（`AM15 Master of Ordnance` / `TAU10 Gun Drone` …） | 它们**不在** `WillRunOps` 的六个来源里，而 `EffectResolver.ResolveAtTurn:4792` **直接扫 `u.Card.Desc`** 消费 ⇒ 无头 + 解得开 + 腿 A = 0，**但确实在跑** |
| `CostWhens` | **降费触发器 1 条**（`TL83 Norn Emissary` 的 `Lower cost by 2 every time …`） | `_costWhens` **不经过 `WillRunOps`**（那个方法只收 `WhenTriggers`/`TriggerTexts`/`SpiritOps`/`OathOps`/`AttackedOps`/`AuraSpecs` **六个**来源），而 `FireCostWhen` 在吃它 |

🔴 **闸 4 不是「可选的保险」，是这档成立的前提** —— §三·2 的 `copy_nogate` 把 `AtTurnClauses` 那一格抽掉之后，
「无消费点」当场从 **15 条** 涨到 **35 条**（那 20 条全是良性的回合起止从句）。

### 为什么敢「判」（③ 就不敢）

③ 只敢「如实标出、不判」，因为它只有一条阴性证据（腿 A = 0）。
第五档多了一条**穷举式**的阳性证据：**全仓读 `u.Card.Desc` 去「执行」的地方只有 `EffectResolver.cs:4792`**
（`grep -rn "\.Desc" Core/*.cs` 的其余命中逐个看过：`CardDef` 自己那一族；
`CreatePool:947` 是**名字**匹配；`EffectResolver:1102/1262` 在 `CanPlayTactic`/`PlayTactic` 里**只服务 `tactic`/`defence`**；
`EffectResolver:3189` 是「按卡名造出来的那张牌」（`deploy <卡名>` 那条路，玩家看不出正文）；
⚠️ `EffectResolver:3086/3095` 的 `PlayerChooseOps` **也**读任意卡的 `desc`，但它是**表现层**问「要弹几个选择面板」，
**不是执行** —— 且真正的执行仍走 `ctx.ChoosePicks`）。
⇒ 四闸全空的句子 = **没有任何一层会执行它**，不是「查不到」。

---

## 二、改动清单

| 文件:行 | 做了什么 |
|---|---|
| **`工具/ruleprobe/WillRun.cs`** 头注 `:26-32` | 类头补上「四档全部以 `Head:` 为锚」这段病灶 + 第五档一句话（**含出处**） |
| `WillRun.cs:75-80` | 新增三个常量 `KHNone`（无消费点 · **计入 rc**）/ `KHAtTurn`（回合起止）/ `KHCostWhen`（降费触发器） |
| `WillRun.cs:93` | `Row` 加 `List<FaceBody> Headless`（含被认领的那些 —— 它们只计数、不进账） |
| `WillRun.cs:415-436`（`Measure` 内） | 新增无头段的**采集 + 对账**一趟：`desc` 分句 ∪ `keywords` 原文条目 ⇒ 再按 §一 的闸 1–4 过滤 |
| `WillRun.cs:501` · `:530` | 新增 **`AddHeadless`**（闸 1、2 + 按段原文去重）与 **`ClassifyHeadless`**（闸 4 的三格 + `无消费点` 定性），判据全在引擎公开面上 |
| `WillRun.cs:241-291` | 新增输出段 **⑤「无头正文」逐句报账**（原 ⑤ 换算段顺移成 **⑥**，见 `:294`）：总数 / 四档计数 / 逐条点名（含 op 形状） |
| `WillRun.cs:318-321` · `:368-372` | `(c)` 换算行加「⑤ 无头/无消费点 N 条」；**判据行 + `return` 加上 `headNone.Count > 0`** |
| `WillRun.cs:347-359` | dump 加三列 `headless` / `headlessNone` / `headlessDetail`（TSV 12 → **13** 列） |
| **`工具/ruleprobe/README.md`** `## willrun` 一节 | 新增 `### ⑤「无头正文」…（🆕 A1459）`：病灶 / 四道闸表 / 真池读数 / 逐张着落表；判据行补 `⑤ = 0`；(c) 行补读数；「三条别推翻的取舍」→ **四条**（加第 4 条：只对 unit/hero + 必须先过闸 4） |

⛔ **没改**：`Program.cs` / `Scan.cs` / `BattleProbe.cs` / `csproj` / `基线/`（本笔不需要动它们，⛔ 基线也没重生成）。
⛔ **没改引擎**（`A1458` 那一层不在本笔：本笔只做「让它看得见」）。

---

## 三、读数

### 3·1 真池实跑（仓库现状 · `cards_engine.json` 1126 张）· `rc=1`

```text
  --- ⑤ 🔴 「无头正文」逐句报账（`A1459`）—— 腿 B **结构上抽不到**的那一半 ---
    无头段（卡面没有 `Head:`、而**主解析器**解得出 op、且**腿 A 一条都没收到**）共 **59 条**，落在 **57 张卡**上：
      · 🔴 **无消费点**  14 条（12 张卡） ← **计入 rc**：这三道闸全空 ⇒ **没有任何一层会执行它**
      · ⚠️ **回合起止**  19 条 ← `EffectText.AtTurnClauses` 认领  ⛔ 不计入 rc
      · ⚠️ **降费触发器**  1 条 ← `CardDef.CostWhens` 认领           ⛔ 不计入 rc
      · ⚠️ **事件层（WhenTriggers）**  25 条 ← `HandledByOtherLayer` 认领 ⛔ 不计入 rc
    🔴 「无消费点」逐条：
       AM16  Minka Lesk          [unit] `Friendly Infantry costs 1 less`            → op lowercost friendly infantry 1
       AM41  Maelon Dhrost       [unit] `Draw a Stratagem`                          → op drawtype stratagem 1
       AM41  Maelon Dhrost       [unit] `Lower the cost of Stratagems in your hand by 4` → op lowercost stratagems in your hand 4
       BL41  Venomcrawler        [unit] `Your Stratagems cost 1 less`               → op lowercost stratagems 1
       DA25  Techmarine          [unit] `If it survives, heal 3 to it`              → op heal 3
       GSC36 Metamorph Leader    [unit] `Your troops cost 1 less`                   → op lowercost troops 1
       GSC71 Atalan Leader       [unit] `Friendly Vehicles cost 1 less`             → op lowercost friendly vehicles 1
       SOR72 Adelaide the Serene [unit] `6 ☀ Gain Flank and Shield`                 → op gain flank and shield
       SW23  Hrolf the Ironhowl  [unit] `Friendly Beasts cost 1 less`               → op lowercost friendly beasts 1
       SW36  Morkai Eliminator   [unit] `Give Hunt Mark to a random enemy troop`    → op give hunt mark
       SW36  Morkai Eliminator   [unit] `Attack a random enemy with Hunt Mark`      → op forceattack
       TAU20 Sniper Drone        [unit] `Gains Long Range when an enemy gains Markerlight` → op gain markerlight
       TAU34 Aunshi Ethereal     [unit] `Your troops cost 1 less`                   → op lowercost troops 1
       TAU25 Piranha             [unit] `Your Drones cost 1 less`                   → op lowercost drones 1
```

🔴 **11/14 条与 `诊断_未归因24张.md` §二/§三·1 那份「真缺陷」清单逐张对得上**
（`AM16` · `AM41`×2 · `BL41` · `SW36`×2 · `TAU20` · **`SW23`** · **`GSC71`** · **`GSC36`** · `TAU25` · `TAU34`），
**另外 2 张（`DA25` / `SOR72`）是这一档新查出来的**（见 §五）。

**验收标准 ① 达成**：`SW23` / `GSC71` / `GSC36` 三张**被点名**（`willrun dump` 那三行：
`headless=1 · headlessNone=1 · 无消费点:Friendly Beasts cost 1 less`）。

### 3·2 构造性实测（**都在仓库外的副本上做**，`D:/4/_tmp_probe3/`）

副本布局（`Program.ResolveOutDir` 从程序集往上找 `MyGame/Assets` ⇒ 副本自带池、自带 `Core`）：

```text
D:/4/_tmp_probe3/copy_new/    {工具/ruleprobe/*, MyGame/Assets/RuleEngine/Core/*.cs,
                               MyGame/Assets/RuleEngine/Resources/cards_engine.json}   ← 修后（= 仓库现读）
D:/4/_tmp_probe3/copy_old/    同上 + **把「采集无头段」那一段整段删掉**  ← 「修前等价」
D:/4/_tmp_probe3/copy_nogate/ 同上 + **抽掉 `AtTurnClauses` 那道闸**        ← 灭自证
```

三份副本的池里**各加了 5 张合成卡**（`D:/4/_tmp_probe3/mutate.py`，**逐条都在探针里验过**）：

| 合成卡 | `desc` | 期望 | 实测（副本池 1131 张） |
|---|---|---|---|
| `ZZHL01` | `Friendly Beasts cost 1 less.` | 🔴 **点名** | ✅ 点名（`无消费点`） |
| `ZZHL02` | `At the end of your turn, deal 1 damage to a random enemy` | ⛔ 不点名 | ✅ 不点名（归「回合起止」） |
| `ZZHL03` | `Lower cost by 2 every time a friendly unit triggers Synapse` | ⛔ 不点名 | ✅ 不点名（归「降费触发器」） |
| `ZZHL04` | `When a friendly Vehicle attacks, gain 1 Quest Point` | ⛔ 不点名 | ✅ 不点名（归「事件层」） |
| `ZZHL05` | `Rally: Stun an enemy` | ⛔ 不点名 | ✅ 不点名（**有头** ⇒ 归腿 B） |

#### 实测 A：修前 vs 修后（**本笔的核心验收**）

```text
                                    修前（copy_old）        修后（copy_new）
  「无消费点」条数                    0 条（0 张卡）          15 条（13 张卡）
  「无消费点」逐条清单                不打印（0 条）          打印，15 行
  `^ +SW23 ` 逐条清单行数             0                       1     ← 🔴 修前【一条都不点】
  `^ +GSC71 ` 逐条清单行数            0                       1
  `^ +GSC36 ` 逐条清单行数            0                       1
  `^ +ZZHL01 ` 逐条清单行数           1（只在 ③ 的逐卡行）     2（③ 逐卡 + ⑤ 逐句）
  回合起止                            0 条                    20 条（19 + ZZHL02）
```

🔴 **修前那三张卡为什么「不出现」**：它们各**还有一条别的 op**（`Rally`/`Strike`/`Uprising`）
⇒ 腿 A ≠ 0 ⇒ **③「未归因」也进不去**；腿 B 又抽不到无头段 ⇒ ④ 也照不到。
**唯一能看见它们的入口，就是本笔新加的这一档。**

⚠️ **修前那 5 张「整卡空表」的卡（`AM16` / `AM41` / `TAU25` / `TAU34` / `BL41`）是「看得见、但看不见病灶」**：
它们在 ③ 里只打**整条 `desc`**（`desc=「Your Drones cost 1 less.」`），而 `SW23` 那种
「一卡两句、坏的是第一句」的形状在 ③ 里**连卡都看不见**。

#### 实测 B：闸 4 是承重的（**灭自证**）

```text
                                    copy_new（有闸）     copy_nogate（抽掉 AtTurnClauses 那一格）
  「无消费点」                        15 条 / 13 张卡     35 条 / 33 张卡
  「回合起止」                        20 条               0 条
  被误报的样例                        ——                  AM15 `At the end of your turn, deal 3 damage …`（op atturn）
                                                          TAU10 `At the start of your turn, deal 2 damage …`
                                                          ZZHL02（合成负例）
```

⇒ **35 = 15 + 20**：抽掉那一格，整整一族良性的回合起止从句**全被报成缺陷** ——
这条实测就是「为什么不能省那一道闸」的现场证据。

#### 副本自证（变异**没有**扰动别的读数）

把三份输出里的 ⑤ 段切掉之后逐行比：**只有两处不同**，且都不是实质差异 ——
① 第一行「卡池路径」（三份副本目录不同，按设计）；② ⑥ 段 `(c)` 行里**本笔新加的那一个字段**
（`⑤ 无头/无消费点 N 条`）。①②③④ 与 ⑥ 的其余部分**逐字节相同** ⇒ 变异只落在 ⑤ 这一档上。

### 3·3 三条标准验收（改完现跑）

| 项 | 读数 |
|---|---|
| `dotnet build`（仓库 + 三份副本） | **0 个警告 / 0 个错误**（四份都 0/0） |
| `check` | **哨兵合计：失败 0 条** ✅ · **解析差异 1 行（`SAU67`）⇒ `rc=1`** ⚠️ **不是本笔的**（见下） |
| `b19test` | **PASS 45 · FAIL 0** · `rc=0` ✅ |
| `willrun` | `rc=1`（**今天本来就是 1**：② 收漏 6 张 / 段级付费段 11 条；本档再加 14 条） |
| 行尾 | `WillRun.cs` **CRLF 0 / LF 611** · `README.md` **CRLF 0 / LF 351**（本报告 **CRLF 0 / LF 238**） |
| `git diff --numstat`（`工具/ruleprobe/README.md`） | `230 / 10` —— **不是整篇重写**（整篇会是 ~351/351）；📌 其中 ~163/10 是**上一会话**的改动，本笔只占增量 |
| dump | 1127 行（表头 + 1126）· **列数恒 13**（已逐行数过） |

⚠️ **`check` 那 1 行差异是【外来】的，三条证据**：

1. 它落在 **`SAU67 Resurrection Vault`**，差的是**引擎解析结果**（baseline 末段是
   `reanimate/pool///0//0/0/////;`，现读多了一条 `give/pool///0/+1 health/0/0/////;`）——
   **本笔一行引擎代码都没碰**（`Core/` 此刻有别的写手）。
2. `Core/EffectText.cs` / `CardDef.cs` 的 mtime 是 **21:25:55**（我 21:26 之后才开始动探针），
   与简报预告的「另一个写手正在改解析器」一致。
3. 🔴 **最强的隔离实测**：**同一份 `Core`** 放进两个副本，一个**把本档整段删掉**（`copy_old`）、一个开着（`copy_new`）
   —— 两边报的都是 **哨兵失败 0 条 · 解析差异 1 行（`SAU67`）· rc=1**，**逐项相同** ⇒ 与本笔无关。
   （`willrun` 这一档也**进不了** `scan`：`WillRun.cs` 只由 `Program.cs` 的 `willrun` 分支调用。）

⇒ **本笔的验收判据取「哨兵 0」**；`check` 的 rc 要等那位写手落地后复跑一次才回绿（⛔ 本笔没重生成基线）。

---

## 四、我没做的 / 判不了的

1. 🔴 **没修引擎**（`A1458` 那一层不在本笔）—— 本笔只做到「**让它看得见**」：那 12 张卡的句子**今天依然一句都不发生**。
   验收标准 ①②③ 三条都做到了。
2. ⚠️ **`tactic` / `defence` 没走这一档**（与腿 B 同一个范围，理由同 README 取舍①）：那两类卡的腿 A **就是**
   `Parse(c.Desc)` ⇒ 无头句早被腿 A 收到，走这一档只会**重复记账**。⇒ 「这两类卡内部有没有同类洞」**本档量不到**。
3. ⚠️ **「有头、但 `Head:` 之后的 `body` 解不出 op」仍然没有一档**（`AddFaceBody` 在 `ops.Count == 0` 时 return）——
   它和本档是**同一族的另一半缝**。本笔**没做也没查**（不在 `A1459` 的判据里）。
4. ⚠️ **`DA25` / `SOR72` 两条我只报到「没有任何一层认领」为止**，**没裁**「应该改成什么」
   （`DA25` 是数据侧还是引擎侧、`SOR72` 属不属于 `A1436` 的射程）—— 判决归调度台。
5. ⚠️ **没跑 Unity**（简报 ⛔ 不许跑）⇒ 本档是**离线旁证**，正式验收仍是 `RuleEngineTest.Run`。
6. ⚠️ **本档的 `rc` 语义是本笔新加的**（`⑤ 无消费点 = 0` 才绿）—— 若调度台认为它该像 ③ 一样**不判**，
   把那一个 `||` 去掉即可（`WillRun.cs` 判据行与 `return` 各一处，本报告已写明位置）。

---

## 五、顺手发现（一条一句话 + 出处）

1. 🔴 **`DA25 Techmarine` 的 `If it survives, heal 3 to it` 永不发生（本档新查出）** ——
   它是 `When a friendly Vehicle attacks, …` 那条的**尾句**，而 `AddWhenTrigger` 拿的正文是**单段**的
   `split[1]`（`CardDef.cs:1703`）；**跨句并接 `TriggerBodyAt:1355` 只服务于「带正文关键词」那条路**
   （它开头就写着 `if (first.IndexOf(':') <= 0) return first;`），`When …` 那条**没有**一步步并接的通道。
   ⇒ 那句 `heal 3`（`condKind=targetsurvives`）**一条 op 都没进卡表**。
   出处 = `willrun` ⑤ 逐条 · 探针 `card Techmarine`（**op 解得出来、腿 A 没有它**）。
2. 🔴 **`SOR72 Adelaide the Serene` 的付费能力段 `6 ☀ Gain Flank and Shield` 是「无冒号写法」（本档新查出）** ——
   ④「付费段」那一档的判据**要求 `:`**（`AddFaceBody` 先看 `col`，`Classify` 拿到的 `Head` 只在有冒号时才存在）
   ⇒ 它**结构上不可见**；而 `A1436`/`A1452` 那份「**11 条段 / 9 张卡**」的名单里**没有 `SOR72`**（出处
   `资料/普查产出_第十四会话/W_A1436付费能力.md:166-181`：`SOR27×2 · SOR11 · SOR12 · SOR23 · SOR68×2 · SOR24 · SOR7 · SOR42 · SOR40`）。
   ⇒ **同一族的第 12 条段 / 第 10 张卡**可能一直没进那份射程。**我没裁**它算不算同一族（形状同、印刷不同）。
3. ⚠️ **`诊断_未归因24张.md` §三·1 那句「`FaceBodies=0` ⇒ 段级那一趟也照不到它」里那个数不对** ——
   实测 `SW23` / `GSC71` / `GSC36` 的 `faceBodies` 都是 **1**（那条 `Rally:` / `Strike:` / `Uprising:` 段，
   且 `orphans=0`）。**照不到的是那句【无头的】**（它结构上不成段）。**结论对、数字不对**。
   出处 = `willrun dump` 的 `faceBodies` 列（`D:/4/_tmp_probe3/wr_real.tsv`）。
4. ⚠️ **「解析器认得」≠「卡表收着」—— `Coverage`（量解析器）四栏全 0 的老病今天又有一例**：
   `EffectText.Parse(整条 desc)` 会把 `When …` 的**尾句**也解出来（`DA25` 的 `heal` 就在里面），
   而**注册层不会**（见第 1 条）⇒ 拿 `Coverage` 当「能不能实现」的尺子照样会漏。
   出处 = 探针 `card Techmarine` 的两条 op vs ⑤ 的判定。
5. ⚠️ **`willrun` 的 ③ 与 ⑤ 对同一张卡是两种粒度**（③ 整卡、⑤ 逐句）—— 「整卡空表」的卡会同时出现在两边。
   已把这条写进取数输出与 README（⛔ 别把两个数相加），避免下一个人合计错。
   出处 = 本笔 §3·2 实测 A 的 `ZZHL01` 那一格（`③=1 行 · ⑤=1 行`）。
