# `W6b` —— 引擎侧四笔落地（`A1342` · `A1346` · `A1357`引擎侧 · `A1358`）

会话：第十三会话 · 执行写手 `W6b`。**只碰了这三个文件**（白名单内）：

- `RuleEngine/Core/CardDef.cs`（LF）
- `RuleEngine/Core/EffectText.cs`（LF）
- `RuleEngine/Core/EffectResolver.cs`（**CRLF**）

⛔ 没碰 `RuleEngineTest.cs` / `cardface_fixes.json` / `cards_engine.json` / `CardPresentation/**` / 两张正本 / git；
⛔ 没跑 Unity（只跑「秒级类型检查」＋**离线对拍探针** `工具/ruleprobe`，两者都不占 Unity 实例）。

> ⚠️ **开场时 `cards_engine.json` 正被另一个写手改**：`SOR72` 的 `desc` 在本会话进行中从
> `… less than 6 Energy, gain 1 Energy …` 变成了 `6 ☀ Gain Flank and Shield. Rally: If you have
> less than 6 ☀, gain 1 ☀ for each enemy unit.`（19:06 那次全池对拍还是 0 差异，19:11 已是 1 行差异）。
> ⇒ 本报告里所有「改前/改后」都以 **19:07 拍下的那份解析快照**为基线，**Core 侧改动与数据侧改动已分开**。

---

## 逐笔交付（4 段）

### `A1342` — **做了**（`KeywordTable.Parse` 取值的那个数字只从「冒号前」找）

- **改了什么**
  - `Core/CardDef.cs:2995-3001` 新增 `public static string HeadOf(string item)` ——
    「`':'` **之前**那半段」的**唯一**切分口（`':'` 之后是效果正文，不是关键词的一部分）。
  - `Core/CardDef.cs:2983`（`Normalize`）· `:3071`（`HasNumber`）· `:3031`（`Parse`）三处**改成读同一个口**
    —— 原来这三处**各切一次**，正是本仓「两处写同一条规则 = 迟早不一致」的现场（`Parse` 那一处还在切
    **整串**）。顺手把三份切分收成一份，**净减两处重复**。
  - `Core/CardDef.cs:3023-3030` 就地改掉那段「理由」注释（铁律 5）：原来写「找的是原始串里的第一个数字，
    我们上一版 Godot 复刻就是这么做的」——**那条旁证不成立**，且与同级的 `Normalize`/`HasNumber` 本就打架。
  - 🔴 **`return 1` 兜底一个字没动**（`FirstNumber` 的 `:3053`）：几十处把「值 ≥ 1」当「有这个关键词」用。
- **原版判据**：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTraitData.cs` 的
  `_trait` / **`_traitValue` 是独立字段**（值不写在文本里、更不会从正文里抠数字）；
  仓内同口径铁证两处：`Core/CardDef.cs:3066`（`HasNumber` 的注释「判的是 `':'` 之前那半段」）·
  `Core/CardDef.cs:602` 一带。⇒ 「`Rally: Deal 2 damage` 的 `2` 不是 `Rally` 的参数」**直接可判**。
- **该补什么断言**（`RuleEngineTest.cs`，由另一批写）
  - 夹具/输入：`KeywordTable.Parse(new[]{ "Rally: Deal 2 damage to an enemy" })`
    · 正例 `Parse(new[]{ "Blast 3: Deal 9 damage" })` · 兜底 `Parse(new[]{ "Rally: Deal 2 damage" })`。
  - 期望：`["rally"] == 1`（**不是 2**）· `["blast"] == 3`（**不是 1 也不是 9**）· 兜底仍是 1（**不是 0**）。
  - 🔴 **灭自证那一半**：只断「`Rally` 那条 = 1」时，把 `FirstNumber` 改成**恒返回 1** 也全绿
    （「正文数字不算」这条判据会被顺手实现成「所有数字都不算」）。⇒ **必须成对**
    （`Rally:…` 取 1 **且** `Blast 3:…` 取 3），两条方向相反，改任何一边必红。
    再补一条 `HasNumber("Blast 3: Deal 9 damage") == true` / `HasNumber("Blast: Deal 9 damage") == false`
    —— 把 `HasNumber` 与 `Parse` 钉在**同一个切分口**上（否则又是两份切分）。
- **没做的 + 卡在哪**：无。

### `A1346` — **做了**（①甲 解析侧跨句承接 ＋ ②乙 一次激活只付一次）

- **改了什么**
  - **(甲) 解析侧 · 付费前缀跨句承接**
    - `Core/EffectText.cs:1382-1405` `SegResult` 新增三个字段：`PaidCost` / `PaidKind` /
      `HasTriggerHead`（**为什么收在 `SegResult` 上、不收局部量**见 `:1898-1900`：
      本方法有 6 个 `return r;`，收在局部量里就得每处记得抄一遍，漏一处 = 静默）。
    - `Core/EffectText.cs:1101-1155` `Parse` 的 `Split(desc)` 循环里加 `carry`（三条按序判）：
      ① 本段自带付费前缀 ⇒ **换**；② 本段自带触发头 ⇒ **清零**；③ 否则把 `carry` 贴到本段产出、
      **还没有代价**的 op 上（承接来的标 `CostShared = true`）。
    - `Core/EffectText.cs:1977-1992` 触发头那一支把 `HasTriggerHead = true` 与自己的前缀一并报出去。
  - **(乙) 引擎侧 · 一次激活只付一次** —— 🔴 **做法与 `R6` 的建议不同，理由写在下面**
    - `Core/EffectText.cs:124-145` `EffectOp` 新增 `bool CostShared`（判据、原文与「为什么」全在那段注释里）。
    - `Core/EffectText.cs:1409-1430` 新增 `static void StampPaidCost(ops, cost, kind)` ——
      **贴付费前缀只此一处**：组里**第一条**照旧是「付钱的那一条」（`CostShared = false`），
      其余全标 `true`。三个贴点（触发头支 `:1990` · `, plus` 两支 `:2089`/`:2123` · 主分支 `:2145`）**全走它**。
    - `Core/EffectResolver.cs:337-341` `ResolveOneCore` 的付费支新增 **`CostShared` 分支**：
      **不查余额、不再扣**（查了会把刚扣过的第二条误判成「付不起」）；
      `:381` 灵魂石那支的世界事件改成 `kind == "spirit" && !op.CostShared`（**一次激活只广播一次**）。
  - **为什么不做成 `R6` 建议的「`ResolveOathAbility` 里按 `OathCost` 先收一次」**：
    `R6` 自己写明「`A1358` 与 ②(乙) 是同一个根因」，而 `A1358` 走的是**战术卡**那条路
    （`EffectResolver.cs:38` 那个重载），**根本不过 `ResolveOathAbility`**。做成 `CostShared` 是
    **一处判据同时覆盖两条路**（誓约 ＋ 战术），而且**不在第二处再写一份付费逻辑**
    （本仓红线：两处写同一条规则迟早不一致）。⇒ 详见本报告「顺手发现 4」。
- **原版判据**（一条 ability = 一个 `manaCost` ＋ 一串 `logic`）
  - `CardAbility.cs` = `{ AbilityTrigger trigger; …; List<AbilityLogic> abilityLogic }`；
    主动付费那一档 `ActiveAbility.cs` = `{ int manaCost; …; List<AbilityLogic> activeAbilityLogic }`。
  - `d:/2/tools/decomp_full/BattleManager__PayActiveAbilityCostOath.c:22` —— 取誓约值之后
    **一次激活只调一次 `PlayerManager__UseMana`**；`…/BattleManager__CanUseActiveAbility.c` 整条 ability 判一次余额。
- **该补什么断言**
  - (甲) 夹具：真卡 `UM81 Phobos Librarian`（`EffectText.Parse` 出来的 op 列表 + `CardDef.OathCost`）。
    - 期望：**2 条 op**；两条 `Cost == 3` 且 `CostKind` 归一后都是 `"oath"`；
      **第一条 `CostShared == false`、第二条 `true`**；`OathCost == 3`。
    - 反向沙包（⛔ 不许清过头）：`Parse("Oath 1: Gain Stealth. Slay: Create a random card in hand")`
      ⇒ **第二条 `Cost == 0`**；`Parse("3 [Icon]: Gain +1 Health. Pray: Deploy a Sister Novitiate")`
      ⇒ 第二条 `Cost == 0`（两条都是真卡 `UM_Phobos_Lieutenant` / `SOR7` 的原文）。
  - (乙) 夹具：真打一局，场上放一个 `UM81`，记 `Energy` before/after，调 `RuleCore.UseOathAbility`。
    - 期望：`Energy` **恰好 −3**（不是 −6）· 该敌人**真的挨了 2-4 点** · 它死了的话**真的拿到 `Shield`**。
  - 🔴 **灭自证那一半（两处都要）**
    - (甲)：只断「`UM81` 第二条有 3 费」不够 —— 把 `carry` 改成**无条件继承上一段**也能绿
      （全池只有 `UM81` 吃第 ③ 条）。**必须配上面那两条「触发头清零」的反向沙包**，
      这样「继承」与「清零」两个方向同时被钉住，改任一边必红。
    - (乙)：只断「只扣 3」不够 —— 把 `_oathOps` 改回**只收一条**（旧行为）也扣 3。
      ⇒ **必须同时断「两条效果都发生了」**（伤害 ＋ Shield 都在），
      再补 `UM93 Fall Back` 恰好 −2（不是 −4）。
- **误伤面（现读）**：**恰好 1 张**（`UM81`）—— 见「验证」那一节的离线对拍读数。
- **没做的 + 卡在哪**：无（(甲)(乙) 两个落点都落地）。

### `A1358` — **做了**（与 `A1346` ②(乙) **同一处修法，一笔覆盖**）

- **改了什么**：就是上面那个 `StampPaidCost`（`EffectText.cs:1409-1430`）＋
  `EffectResolver.cs:337-341` 的 `CostShared` 分支。
  `Finish`（`EffectText.cs:2438-2490`）把 `op.Tail` 递归解成第二条 op 之后，
  **整批**一起贴前缀 ⇒ 第二条自动拿 `CostShared`。
- **原版判据**：同 `A1346`（`BattleManager__PayActiveAbilityCostOath.c:22` 一次 `UseMana`）。
- **该补什么断言**
  - 夹具：`UM93 Fall Back`（`Oath 2: Lower its cost by 2 and give it Flank`）·
    `SOR6 Righteous Repugnance`（`4 Energy: Give +2 … instead and Heal them 1`）。
  - 期望（解析）：各 **2 条** op、同价同货币、**第二条 `CostShared == true`**。
  - 期望（结算）：`Energy` **只掉一次**（−2 / −4），而且**两条效果都发生**
    （`lowercost` 真降了 2 费 ＋ `Flank` 真挂上了 / 两条增益 ＋ `Heal 1` 都发生了）。
  - 🔴 **灭自证那一半**：「只扣一次」单独一条**能被「干脆不扣」蒙过**（把 `Cost` 清零同样绿）。
    ⇒ 必须与「**两条效果都发生**」成对；再加一条**反向**：把 `CostShared` 抹掉（回到旧写法）时，
    `Energy` 必须掉两次 —— 用「同一次激活只收一份」与「两条效果都在」的**联合**把两边都钉死。
- **误伤面（现读，`R6` 未验的那一半由本报告补齐）**：`R6` 筛出的 5 张候选里**真出 2 条 op 的只有 2 张**
  —— `UM93 Fall Back` · `SOR6 Righteous Repugnance`；
  `ASH_Farseer` / `ASH_Farseer_Skyrunner` / `UM_Suppressor` 都只有 **1 条** op（**不会**重复收费，实测读数见下）。
- **没做的 + 卡在哪**：无。

### `A1357`（**引擎侧一半**）— **做了**（`☀` 走 `faithcheck`）；数据侧那半**不是我做**（另一个写手）

- **改了什么**
  - `Core/EffectText.cs:7812` `EffectCondition.Normalize` 新增一条：
    `if (c.Contains("you have") && EffectText.MentionsFaith(c)) return "faithcheck";`
    —— 排在原来那条 `… && c.Contains("energy") ⇒ "energycheck"`（`:7801`）**后面**（两条互斥，顺序无影响）。
  - `Core/EffectText.cs:6265-6276` 新增 `public static bool MentionsFaith(string s)`
    （`faith` 那个词 / `☀` 那枚太阳）—— 🔴 **与 `CostKindOf` 同源、只此一处**：
    `:6285` 把 `CostKindOf` 原来的 `s.Contains("faith") || s == "☀"` 也改成调它，
    ⇒ 「`☀` = 信仰」这**一份判据**同时管「货币名」与「条件句」。**行为逐个 token 等价**（`☀` 整词、`[faith icon]` 等情形都验过）。
  - `Core/EffectResolver.cs:7030-7047` `ConditionHolds` 新增 `case "faithcheck"`：
    `holds = ctx.Players[owner].`**`Faith`**` < n`（阈值仍**从条件原文里读**，不写死 6）。
  - `Core/EffectResolver.cs:7020-7023` 就地订正 `energycheck` 那段**已不成立**的注释
    （原来写「（`SOR72 Adelaide the Serene`）—— 全池就这一张」）。
    ⚠️ **`energycheck` 这一支保留、一行没删**：`less than N Energy` 是合法写法，
    删了会让 `RuleEngineTest.cs:8258` 的 `CanJudgeCondition("energycheck")` 那条断言红。
- **原版判据**：卡图亲读（`R6` 的座标，本轮**转引**、没重开图）
  `d:/2/Warpforge部队卡片/Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png` =
  `Rally: If you have less than 6 ☀, gain 1 ☀ for each enemy unit`（两个都是**金太阳**＝信仰）；
  `descZh` 同（「若你的信仰少于 6 点…便获得 1 ☀」）。`☀` 的字面判据：`Core/EffectText.cs:6285`
  （`CostKindOf`，与 `:6214` 的两条付费正则同源）。
- **该补什么断言**
  - 解析：`EffectCondition.Normalize("you have less than 6 ☀")` ⇒ **`"faithcheck"`**；
    `Normalize("you have less than 6 Energy")` ⇒ **`"energycheck"`**（两个方向都要）。
  - 结算（**这一对才是关键**，照 `TestConditionKindFamily` 的 `withBeast/withoutBeast` 形状）：
    - 信仰 **5** / 能量 **99** ⇒ 那半句**执行**（`gainfaith` 真发生、信仰 +1×敌人单位数）；
    - 信仰 **6** / 能量 **0** ⇒ **不执行**。
  - 🔴 **灭自证那一半**：**能量与信仰必须反向**（5/99 与 6/0）。
    只断一侧、或两档里两个字段同向变化时，「读错字段」（改成读 `Energy`）照样全绿 ——
    因为 SOR72 原来就是读 `Energy`（`R6` 静态证过那半句「按能量判」）。
  - 另补一条正向的：`RuleCore.CanJudgeCondition("faithcheck") == true`（今天它**不在**
    `UnjudgeableConditions` 里，`RuleEngineTest.cs:8016-8033` 那条「全池出现的条件种类都必须判得了」
    会自动兜住 —— **现读全池 `faithcheck` 只出现 1 次**）。
- **`gain 1 ☀` 那一半走不走得通（题面要求核的）**：**走得通**。
  离线探针实测 `SOR72` 的第二条 op = `verb=gainfaith amount=1 target=[(玩家信仰)]`，
  而 `gainfaith` 在结算表里有实现（`Core/EffectResolver.cs:437` →
  `DoFactionResource(..., "faith", ...)`）。
  ⚠️ **但「`for each enemy unit` 的遍数真的挂上了」这一半没验** ——
  `op.CountRef` / `CountScope` **探针不打印**（`工具/ruleprobe/Program.cs` 的 `Dump` 没有这两列），
  而「按 N 遍结算」是主循环里的**动词无关**机制（`EffectResolver.cs:241` 的 `repeats`）⇒
  代码上通、**没有观测证据**。如实标在「没查清」一节。
- **没做的 + 卡在哪**：`desc` 里那两个字**不是我做**（`cardface_fixes.json` / `cards_engine.json` 有另一个写手，
  白名单外）。我落地时数据侧**已经**改成 `☀` 了（见开头那段）⇒ 两侧今天已经对上。

---

## 全量改动清单（逐 hunk）

| 文件:行 | 改了什么 | 属哪笔 |
|---|---|---|
| `Core/CardDef.cs:2983` | `Normalize` 的切分改调 `HeadOf` | `A1342` |
| `Core/CardDef.cs:2995-3001` | **新增** `HeadOf`（"冒号前那半段"的唯一判据） | `A1342` |
| `Core/CardDef.cs:3021-3031` | `Parse` 取值改 `FirstNumber(HeadOf(item))` ＋ 就地订正理由注释 | `A1342` |
| `Core/CardDef.cs:3071` | `HasNumber` 改调 `HeadOf`（去掉第二份切分） | `A1342` |
| `Core/EffectText.cs:124-145` | **新增** `EffectOp.CostShared`（＋29 行判据注释） | `A1346`②/`A1358` |
| `Core/EffectText.cs:1101-1155` | `Parse` 循环里的 `carry`（换 / 清零 / 承接） | `A1346`① |
| `Core/EffectText.cs:1382-1405` | `SegResult` ＋`PaidCost`/`PaidKind`/`HasTriggerHead` | `A1346`① |
| `Core/EffectText.cs:1409-1430` | **新增** `StampPaidCost`（贴前缀只此一处） | `A1346`②/`A1358` |
| `Core/EffectText.cs:1898-1911` | 付费前缀改收在 `r.PaidCost`/`r.PaidKind`（不是局部量） | `A1346`① |
| `Core/EffectText.cs:1942-1943` | 同上（`Oath N:` 那一支） | `A1346`① |
| `Core/EffectText.cs:1977-1992` | 触发头支：报 `HasTriggerHead` ＋ 前缀，贴取代 `foreach` | `A1346`①② |
| `Core/EffectText.cs:2021-2031` | `paidmod` 支改读 `r.PaidCost` | `A1346`① |
| `Core/EffectText.cs:2085-2090` · `:2119-2124` | `, plus` 两支：换 `r` 前先存前缀 ＋ `StampPaidCost` | `A1346`② |
| `Core/EffectText.cs:2132-2148` | 主分支：`r = Dispatch(...)` 前先存前缀（**离线探针抓到的回归**，见「验证」） | `A1346`② |
| `Core/EffectText.cs:6265-6276` | **新增** `MentionsFaith`（与 `CostKindOf` 同源） | `A1357` |
| `Core/EffectText.cs:6285` | `CostKindOf` 改调 `MentionsFaith`（行为等价） | `A1357` |
| `Core/EffectText.cs:7812` | `Normalize` 新增 `⇒ "faithcheck"` | `A1357` |
| `Core/EffectResolver.cs:315-341` | 付费支新增 `CostShared` 分支（不查余额、不再扣） | `A1346`②/`A1358` |
| `Core/EffectResolver.cs:378-381` | 灵魂石世界事件 `&& !op.CostShared` | `A1346`② |
| `Core/EffectResolver.cs:7020-7047` | `energycheck` 注释就地订正 ＋ **新增** `case "faithcheck"` | `A1357` |

行尾：`EffectResolver.cs` 纯 **CRLF 7918/7918**（改完仍是纯 CRLF，没翻）；另两个纯 LF。
`git diff --numstat`：`CardDef.cs 265/11` · `EffectText.cs 273/19` · `EffectResolver.cs 121/10`
⚠️ **这三个数不是我这批的全部** —— 开场时它们**已经带着前几批未提交的改动**（`M` 状态），
本表上面的 hunk 清单才是我这趟的。

---

## 验证

### 1. 秒级类型检查（`TMPDIR=/tmp/wf_w6b bash d:/4/Unity/工具/typecheck.sh`）

- 第一趟：**运行时错误数 1** —— `EffectText.cs(7803,43): CS0103 MentionsFaith 不在当前上下文`
  （`EffectCondition` 是本文件里**另一个顶层类**，看不见 `EffectText` 的私有静态方法）。
  ⇒ 改成 `public static` ＋ 调用点写 `EffectText.MentionsFaith(c)`。
- 第二趟（终）：**运行时错误数 0 · 编辑器错误数 0** ✅

### 2. 离线对拍探针（`bash d:/4/Unity/工具/ruleprobe.sh`，不占 Unity 实例）

- **全池 `check`**：`全池 1120 张 · 解析差异 **1 行** · 改名 0 · 新增 0 · 消失 0`，
  那 1 行 = `SOR72|Adelaide the Serene`。
  🔴 **这一行不是我造成的、可逐字证**：基线行 =
  `…;gain/pool///1/1 energy;` · 现在 =
  `…;gainfaith/pool///1/;` —— 差的是**动词与载荷**（由 `desc` 里 `Energy`→`☀` 决定）。
  把**旧 `desc` 原文**喂给**新代码**：`seg "6: Gain Flank and Shield. Rally: If you have less than 6 Energy, …"`
  仍然解出 `gain/…/1 energy`（与基线逐字相同）⇒ **我的 Core 改动在全池 scan 列上 0 差异**。
  （`check` 只比 `unparsed`/`partial`/`ops` 三列，**不含 `Cost`/`CostShared`/`ConditionKind`** ——
    所以它证明不了我这三笔；那三笔由下面 3、4 两条单独证。）
- **`card` 逐张（86 张带付费前缀的卡，改前/改后逐字节比）**：
  **只有 1 张卡变** —— `UM81 Phobos Librarian`，第二句从 `cost=0 ck=` 变成 **`cost=3 ck=oath`**；
  其余 85 张（含 `ASH42 Avatar of Khaine` 的 `Talent:` 尾句、`SOR27` 的 `2 ☀/4 ☀`、
  `SOR68` 的 `2 ☀/4 ☀/Pray:`、`SOR44` 的 `4 ☀/7 ☀`、`SOR7` 的 `3 [Icon]/Pray:`、
  `UM_Phobos_Lieutenant` 的 `Oath 1/Slay:`）**逐位零变化** ⇒ 承接规则没有外溢。

| 卡 | 现在的读数（`card`） | 对应哪一笔 |
|---|---|---|
| `UM81 Phobos Librarian` | `deal cost=3 ck=oath` ＋ **`gain cost=3 ck=oath`**（第二句接上费） | `A1346`① |
| `UM_Phobos_Lieutenant` | `Oath 1` op `cost=1 ck=oath` · **`Slay:` op `cost=0`**（触发头清零） | `A1346`① 反向 |
| `SOR7 Sister Novitiate` | `3 [Icon]` op `cost=3 ck=faith` · **`Pray:` op `cost=0`** | `A1346`① 反向 |
| `SOR44 Beacon of Faith` | `4 ☀`→`cost=4` · `7 ☀`→`cost=7`（自带前缀**换**掉 carry） | `A1346`① 反向 |
| `ASH42 Avatar of Khaine` | 只有 1 条 op `cost=2 ck=spirit`，`Talent:` 段**零 op**（泄漏无害） | 误伤面 |
| `SOR72 Adelaide the Serene` | `gain cost=6 ck=faith` ＋ `gainfaith`（**给信仰那一半通**） | `A1357` |

### 3. `CostShared` 的观测（**股票探针打不出来，另造了一个一次性探针**）

> ⚠️ `工具/ruleprobe` 的 `Dump` **不打印 `Cost` / `CostShared` / `ConditionKind`**（只有 verb/amount/payload/target/tail/cost/ck/…），
> 而这三样正是本批的全部内容 ⇒ 我在**仓库外**建了一个一次性副本 `D:/tmp/wf_w6b_probe/`
> （拷 `工具/ruleprobe` 的壳 ＋ 同一个 `RuleEngine/Core/*.cs` 通配，加一个 `cost` / `cond` 模式），
> **没动仓库里的任何文件**。它同时也能复跑 `ruleprobe` 的全部既有模式（同一份源码）。
> 用完可整个删掉 `D:/tmp/wf_w6b_probe/`。

读数（`dotnet … cost`）：

```
== 卡数=1120 带付费 op 的卡=83 含 SHARED 的卡=3 「多 op 同价却无 SHARED」=0 ==
SOR6|Righteous Repugnance|paid=2|oath=0|spirit=0|      give:4:energy:PAY heal:4:energy:SHARED
UM81|Phobos Librarian   |paid=2|oath=3|spirit=0|      deal:3:oath:PAY  gain:3:oath:SHARED
UM93|Fall Back          |paid=2|oath=2|spirit=0|      lowercost:2:oath:PAY give:2:oath:SHARED
```

⇒ **全池只有这 3 张卡有「一条前缀盖住 ≥2 条 op」的形状，且每张都恰好标对了**；
「多 op 同价**却没有任何一条 SHARED**」（= 还在重复收费）的卡 = **0 张**。
`UM81` 的 `oath=3` 也顺手证了 `CardDef.OathCost` 没被改坏。

### 4. `faithcheck` 的观测（`dotnet … cond`，全池条件归一）

```
== ConditionKind 分布 ==
   targetdies × 23 · targethaskw × 23 · energyzero × 26 · ownnoothertroops × 10 · controlcount × 10
   targetsurvives × 7 · targethasarmour × 4 · alreadyhas × 3 · deaths × 2 · warlordlowhp × 1
   noeffect × 1 · rangedzero × 1 · damaged × 1 · **faithcheck × 1**
SOR72|Adelaide the Serene|kind=faithcheck|cond=[you have less than 6 ☀]
```

⇒ 全池 `faithcheck` **只 1 处**（就是那唯一一张），`energycheck` **今天 0 处**（与「SOR72 是唯一带
`less than` 的卡」这条现读吻合）；**其余种类的名字与计数都没动**。

### 5. `A1342` 的全池数值复算（python，逐 (卡,键) 比改前/改后门槛）

```
关键词项总数 1021 · 卡数 1126 · 值变化处数 0 · HasNumber 变化处数 0
冒号后带数字的候选项 5（ASH79 / DA9 / GOF33 / GOF24 / UM11）—— 改前改后**全是 1**
```
⇒ 与 `W5b` 的现读一致（**收口，不是行为改动**）。

### 6. 没跑的（如实）

- ⛔ **Unity 自检一条都没跑**（红线；且按铁律 12「活没干完别自检」）。
- ⚠️ **(乙)/`A1358` 的「少扣的钱」只有代码级证据、没有真打一局的读数**：
  现有离线探针的场景（`jackal`/`emergency`/`suppressor`/`played`/`b19test`）**没有一个走付费前缀那条路**
  （`suppressor` 只查 `choosecard` 的候选域），而我不能改 `BattleProbe.cs`（白名单外）。
  ⇒ 那两条只能交给 `RuleEngineTest.Run`（`UM81` 的 −3 / `UM93` 的 −2 两条）。
- ⚠️ `SOR72` 的 `for each enemy unit` 遍数（`CountRef`）**没观测到**（原因见上）。

---

## 没查清 / 没做的部分（如实）

1. 🔴 **「一份前缀 + 一串 op」的收钱口径，我落在 `ResolveOneCore` 上，`R6` 建议落在 `ResolveOathAbility` 上** ——
   两处**可观测差别只有一种**：同一张卡有**两份不同价**的誓约前缀（`Oath 2: A. Oath 5: B`）时，
   我这边付 2+5=7、`R6` 那边付 5。**全池 0 张这种卡**（现读），而「一条 ability 一个 cost」在
   我这边是**每个前缀组各付一次** —— 与「两句 = 两条主动能力」更接近；但**哪一条才是原版行为没查**
   （原版的主动能力是**玩家选一条**用的，我们是一个按钮把所有 `Oath N:` 一起结算 —— 那是**既有的建模差**，
   不属于本批）。**判据未定 ⇒ 没动**。
2. ⚠️ **`Talent:` / `Companion N:` / `When …` 这类「别的层接手的句子」不参与 carry 清零**
   （清零只认 `IsRoutableTrigger`）。今天**不可观测**：全池唯一「付费前缀段之后接 `Talent:`」的是
   `ASH42`，而 `Talent: Wrath of Khaine` 段**产出 0 条 op**（探针实测），泄漏没有落地处。
   **会出事的确切形状**：付费前缀段 → `Talent:`/`When …` 段 → **后面还有效果段**
   （今天 0 张）。要不要清零**没裁**（判据本来就在另一个层手里，`CardDef.HandledByOtherLayer` 需要 `CardDef` 实例，
   而 `Parse` 只吃 `string`）。
3. ⚠️ `SOR72` 的 `for each enemy unit` **遍数没验**（探针不打印 `CountRef`/`CountScope`）。
4. ⚠️ 结算层那三处读数（`UM81` −3 · `UM93` −2 · `SOR6` −4）**没有真跑**，理由见「验证 6」。
5. ⚠️ 我把 `MentionsFaith` 定成 `faith`/`☀` **两个记号**；`CostKindOf` 的 `icon` 那一支
   （`s.Contains("icon") ⇒ faith`）**没有并进去** —— 它是「只对那三张卡成立」的经验判据
   （见 `EffectText.cs:6286` 的原注释）。条件句里出现 `icon` 的卡今天 **0 张** ⇒ **没动**。

---

## 顺手发现（⛔ 只报不改，一行一条）

1. ⚠️ **`工具/ruleprobe` 的 dump 不打印 `Cost` / `CostShared` / `ConditionKind` / `CountRef`** ——
   而这四样正是「付费 / 条件 / 计数」三族改动的**全部可观测面**。本轮为了验自己现造了一个一次性副本；
   建议把 `Program.Dump` 补上这四列（⚠️ 会让 `scan` 基线两侧都变 —— 那是**工具输出**，不是引擎行为，
   重建基线即可；`扫描列加列` 这件事本身要按 `Scan.cs` 头那两条教训来）。
2. ⚠️ `Core/EffectText.cs` 里 `ParseSegment` 的「`, plus`」两支有个**既有的**取舍：
   `r = rHeadB / r = rHead` 换掉 `r` 之后，**付费前缀只贴 `rPlus` 那一批**，`rHead` 那批 op 的 `Cost` 是 **0**
   （= 前半句**白给**）。今天**不可观测**（那两支要 `SplitPlusClause` ＋ `ContainsTo`/`an additional N` 的形状，
   全池只有 `Alpha Warrior` 一张、而它没有付费前缀）。⛔ 我没改（改了会动全池行为，属另一笔账），
   但**新账值得立**：`付费前缀 + , plus 尾句` ⇒ 前半句不带价。
3. ⚠️ **`for each` 计数型 op 仍会按 `repeats` 遍重复付费**（`EffectResolver.cs:244` 那个
   `for (k < repeats) ResolveOne(op)`）—— `CostShared` 挡不住它。今天**没有**「付费前缀 ＋ for each」
   的卡（`SOR72` 的两半句各自不带对方的前缀）⇒ 不可观测。**同一族的第三个漏点**，值得与新账一起看。
4. ℹ️ **本批的修法把 `R6` 的「一处判据」原则推进了一步**：`A1346`②(乙) 与 `A1358` 用**同一个**
   `CostShared` 覆盖两条路（誓约走 `ResolveOathAbility`、战术走 `EffectResolver.cs:38` 那个重载），
   而不是在 `ResolveOathAbility` 里再写一份付费。⇒ 建议**不要**再按 `R6` 原文去改 `ResolveOathAbility`
   （那会变成第二份付费判据）。
5. ℹ️ `R6` 的 `A1358` 候选清单 5 张 → **实读只有 2 张**（`UM93` / `SOR6`）真的出 2 条 op；
   `ASH_Farseer` / `ASH_Farseer_Skyrunner` / `UM_Suppressor` 各只有 1 条。
6. ℹ️ `EffectCondition`（`EffectText.cs:7704`）是**另一个顶层类**，看不见 `EffectText` 的私有成员 ——
   本批第一趟类型检查就撞在这上面（`CS0103`）。**往那儿加判据时要记得跨类调用。**
7. ⚠️ **`CostShared` 堵不住「付钱那条被跳过」的第三个洞**：`ResolveOps` 的 `instead` 机制
   （`EffectResolver.cs:125-199`）会把配对里**输的那一条整条跳过** ——
   若被跳过的恰是**组里第一条（付钱那条）**，同组其余 op 会因为 `CostShared` 而**一分钱不掏**地结算。
   **今天 0 张可观测**：现读付费卡里含 `instead` 的**只有 `SOR49 Trial of Suffering`**，
   而它那条 `2 ☀:` 段**只有 1 条 op**（`paid=1`，探针读数），构不成一个组。
   ⇒ 与上面那条「`for each` 重复付费」是**同一族的两个洞**（钱与 op 的对应不是一对一），**留账别忘**。
   ⛔ 我没改（改法要先定「谁付」的口径，属另一笔）。
