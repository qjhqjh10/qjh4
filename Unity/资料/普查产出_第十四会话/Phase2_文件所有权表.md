# Phase2 · 文件所有权表（15 笔账 · 2026-10-10）

> 只读盘点产出。用途：主对话按【文件】切写手 —— **一个文件同一时刻只有一个写手**。
> ⚠️ 行号是**现读**、会漂（改一次正本就漂）⇒ 复核时**按 A 号 `grep`**，别信行号。
> 「要改的文件」= 动手时**会碰**的那几个（含生成物 / 基线 / 文档）；只被**读**的不列。

## 表一：账 → 文件所有权

| 编号 | 一句话 | 要改的文件（逐个列全） | 动数据/JSON | 动 `工具/` | 判据出处（文件:行号） | 组 |
|---|---|---|---|---|---|---|
| `A1374` | `scan` 还差三列 `CostKind` / `CountScope` / `Condition` | `工具/ruleprobe/Scan.cs` · `工具/ruleprobe/Program.cs`（`Dump` 已有 `ck=`，两入口要一致）· `工具/ruleprobe/README.md` · `工具/ruleprobe/基线/out_baseline.txt`（重生成） | 否 | 是 | `项目任务.md:524` · `普查产出_第十三会话/W9_A1368探针补列.md` | 1 |
| `A1349` | `BattleProbe.cs` 三处不过 `LoadPool` 的入口 | `工具/ruleprobe/BattleProbe.cs`（`:149`/`:155`/`:290`）· `工具/ruleprobe/_专题/seg_any_1016.cs.txt`（未跟改，只有恢复成 `.cs` 编进去才要动） | 否 | 是 | `项目任务.md:508` · `W5d_A1339探针口径.md` 顺手发现 3 | 1 |
| `A1348` | `check` 能被「重生成基线」静默抹平 | `工具/ruleprobe/Program.cs`（`CheckMain.Run`，加索引自证）· 备选 `工具/ruleprobe/README.md`（文档级哨兵已补） | 否 | 是 | `项目任务.md:507` · `W5d_A1339探针口径.md` 顺手发现 2 | 5 |
| `A1352` | `RuleCore.cs:2750` 的 `CS0162` ⇒「场上没空格了」永远不打印 | `MyGame/Assets/RuleEngine/Core/RuleCore.cs` | 否 | 否 | `项目任务.md:510` | 2 |
| `A1353` | `OtherCardSacrifice`(225) 广播整条没做 | `.../RuleEngine/Core/RuleCore.cs`（广播点注释在 `:5117`）· `.../RuleEngine/Core/WhenEvent.cs`（新 `WhenEventKind`；常量表 `:346`） | 否 | 否 | `项目任务.md:511` | 2 |
| `A1354` | `dropPod` 的「开舱」那一半 | `.../RuleEngine/Core/RuleCore.cs`（池被打空那条路，`:4725-4750` 一带） | 否 | 否 | `项目任务.md:512` · `CardDef.cs:2144-2165` | 2 |
| `A1356` | `EmitBloodThirst` / `TrySwarmMerge` 各缺「没正文那半边」 | `.../RuleEngine/Core/RuleCore.cs`（`:6636` / `:2613`；形状照 `:6735 FireTriggerAlways`） | 否 | 否 | `项目任务.md:514` | 2 |
| `A1369` | 「钱与 op 不是一对一」的三个洞 | `.../RuleEngine/Core/EffectText.cs`（`:2089`/`:2123` 两支）· `.../RuleEngine/Core/EffectResolver.cs`（`:155-186` / `:261-262`） | 否 | 否（⚠️ 验收要 `A1374` 三列先到位） | `项目任务.md:519` · `R7_A1366到A1369查证.md:125-192` | 3 |
| `A1366` | `Stun` 那枚金螺旋全池一条都没画 | `工具/gen_icon_plan.py`（`BARE_TOKEN_BY_CARD:415`；⚠️ 顺手订正 `:508-510` 那条被 6 张卡图证伪的注释）· `数据/游戏数据/card_icon_plan.json` · `MyGame/Assets/CardPresentation/Resources/card_icon_plan.json`（后两份 = `--write` 生成物） | 是（两份 json，皆生成物） | 是 | `项目任务.md:517` · `R7_A1366到A1369查证.md:14-62` | 5 |
| `A1371` | `对照与缺口.md` §七「73+5」是硬编码 | `工具/gen_icon_doc.py`（`:180` 一带）· `资料/卡面图标_对照与缺口.md`（重生成） | 否 | 是 | `项目任务.md:521` | 5 |
| `A1340` | 卡名索引建得太晚 ⇒ 缓存里卡名目标恒 `null` | `MyGame/Assets/RuleEngine/Data/CardDatabase.cs`（`:113` 调用序）· `.../RuleEngine/Core/CreatePool.cs`（`BuildNameIndex:614` 的入参/重载）· **只有走「两段式构造」时**才加 `.../Core/CardDef.cs` · ⚠️ `.../RuleEngine/Editor/RuleEngineTest.cs:2418-2423`（注释订正，账自称「归断言批」） | 否 | 否 | `项目任务.md:504` · `D_A1334诊断.md:130-133` | 5 |
| `A1375` | `Rally.` vs `Rally:` —— ~200 张卡差一个冒号 | `MyGame/Assets/CardPresentation/Core/CardText.cs`（`:263` 连接符；按 `CardDef.BodyKeywords` 分流） | 否 | 否 | `项目任务.md:525` · `R7_A1366到A1369查证.md:111-121` | 5 |
| `A1370` | icon 链没有任何「盘上有 vs 真用过」的可观测点 | `MyGame/Assets/CardPresentation/Editor/IconSetup.cs`（`Verify` 加一条对账）· **备选落点** `工具/gen_icon_doc.py`（判据没定在哪一侧 ⇒ 写手现核） | 否 | 看落点 | `项目任务.md:520` · `W8_A1365与A1361清理.md` 顺手发现 3 | 4 |
| `A1372` | `Core/CardIcons.cs:7` 文件头例子与现读不一致 | `MyGame/Assets/CardPresentation/Core/CardIcons.cs` | 否 | 否 | `项目任务.md:522` | 4 |
| `A1341` | `A1332` 只接了「引擎当前值 ≥ 2」那档，`numericKeys` 那档仍没接 | `.../CardPresentation/Core/CardText.cs`（`KeywordSegment:213` 加带默认值的可选形参）· `.../CardPresentation/Battle/BattleDriver.cs`（`:11648` 传 `c.NumericKeywords`） | 否 | 否 | `项目任务.md:505` | 波2 |

> 省写：`.../RuleEngine/Core/` = `MyGame/Assets/RuleEngine/Core/`；`.../CardPresentation/` = `MyGame/Assets/CardPresentation/`（工程根 `D:/4/Unity/MyGame`）。

## 表二：冲突矩阵（只列有交集的账对）

| 账 A | 账 B | 共有文件 | 结论 |
|---|---|---|---|
| `A1374` | `A1348` | `工具/ruleprobe/Program.cs` | **必须串行** |
| `A1375` | `A1341` | `MyGame/Assets/CardPresentation/Core/CardText.cs` | **必须串行** |
| `A1352`／`A1353`／`A1354`／`A1356` | 彼此（**两两**，共 6 对） | `.../RuleEngine/Core/RuleCore.cs` | **必须串行**（建议**一个写手独占**这份文件、四笔一笔一笔做） |

- 非文件冲突、但要排顺序的两条（**不算「共有文件」**）：
  · `A1369` 的验收需要 `A1374` 的三列先落到 `scan`（否则改了没信号）⇒ **`A1374` 先做**。
  · `A1366` 重生成 `card_icon_plan.json` 后 `资料/卡面图标_对照与缺口.md` 会过期；那份文档由 `A1371` 重生成 ⇒ **`A1366` 先、`A1371` 后**（否则 `A1371` 会照中间态铺一份）。
- ⚠️ **共享编译单元**：`工具/ruleprobe/ruleprobe.csproj:23` 直接 `Compile` 了 `MyGame/Assets/RuleEngine/Core/*.cs`
  ⇒ 引擎侧写手（组2/组3）**正写到一半**时，探针侧（组1/组5-A1348）的 build 会**报假错**（见 `CLAUDE.md` 铁律 13·3）。
  判据：错误全集中在**不是自己负责的文件**上 ⇒ 那是别人的活，隔会儿重跑，⛔ 别去改别人的文件。

## 表三：建议的分组

**波 1（5 组，组内文件互不相交 ⇒ 可同波开工；组内还可以再拆给不同写手。**组间**唯一一处相交见文末注）**

| 组 | 名字 | 账 | 组内独占的文件 |
|---|---|---|---|
| **1** | **探针侧** | `A1374` · `A1349` | `Scan.cs` · `Program.cs` · `README.md` · `基线/out_baseline.txt` · `BattleProbe.cs` |
| **2** | **引擎侧 · RuleCore 串行族**（一个写手独占 `RuleCore.cs`） | `A1352` · `A1353` · `A1354` · `A1356` | `RuleCore.cs` · `WhenEvent.cs` |
| **3** | **引擎侧 · 付费与条件** | `A1369` | `EffectText.cs` · `EffectResolver.cs` |
| **4** | **卡面侧 · 图标链** | `A1370` · `A1372` | `Editor/IconSetup.cs` · `Core/CardIcons.cs`（+ 备选 `工具/gen_icon_doc.py`） |
| **5** | **数据 / 生成器 / 卡面文本侧** | `A1366` · `A1371` · `A1340` · `A1348` · `A1375` | `gen_icon_plan.py` · 两份 `card_icon_plan.json` · `gen_icon_doc.py` · `卡面图标_对照与缺口.md` · `CardDatabase.cs` · `CreatePool.cs` · `ruleprobe/Program.cs` · `Core/CardText.cs` |

**波 2（等**组 5**落地后再发）**

| 组 | 名字 | 账 | 为什么必须等 |
|---|---|---|---|
| **6** | **卡面侧 · 角标判据收口** | `A1341` | 与 `A1375` 撞 `Core/CardText.cs` ⇒ 与**组 5** 串行 |

> ⚠️ 组 1 与组 5 里的 `A1348` 撞 `ruleprobe/Program.cs` ⇒ **组 1 先做完、`A1348` 再动**（组 5 可先做其余四笔）。
> ⚠️ 表三的组号 ≠ 表一的「组」列（表一那列标的是**它落在哪一组**；`A1348` 记作组 5 但须排在组 1 之后）。
