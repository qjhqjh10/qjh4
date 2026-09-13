# 选牌 handler（`Choose a …`）：数据与设计

> 2026-09-13 第二十六轮开工前的**数据收集**产物。数据全部来自**实跑 + 实测**，逐条标了出处。
> 工作清单是**自动生成**的（`_tmp_view/tactic_unparsed.txt` ①栏），本文档只记**结论与出处**，不抄清单。

---

## 一、规模（实测）

| | 数 | 出处 |
|---|---|---|
| `Choose …` 句（战术卡上） | **30 种 / 33 次** | `_tmp_view/tactic_unparsed.txt` ① 栏 |
| 占剩余「完全不认识」 | **88 种里的 34%**（91 次里 36%） | 同上 |
| 涉阵营 | **12 个** | `_tmp_view/tactic_by_faction.md` |
| 只在**单位/英雄/防御卡**上、不在这 30 里的 | **+7 种**（顺手也解锁） | 见 §四 |

**这批现在是「干净地打不出去」** —— 全在 ①「完全不认识」栏，**不在 ②「半懂」栏**。
所以不存在「认了一半、静默错结算」的风险，是纯粹的覆盖率增益。
（对照：②栏那 6 条才是危险的，见 `卡牌效果管线_计划与交接.md` 的两数口径。）

⚠️ **文档更正（2026-09-13）**：`资料/阵营推进_清单与交接.md:11-20` 原文写「**以 `Choose a …` 开头的卡点 29 条、涉及 11 个阵营**」——
实跑后是 **30 种句 / 33 次 / 12 个阵营**（29 是「按卡计」的旧口径，且没算 Genestealers 那 2 张 Sabotage 卡）。**以自检产物为准，别手抄数字。**

---

## 二、`rule_core.gd` 的蓝图照抄，但有 **5 处要按实况修正**

权威源：`d:/warpforge/scripts/rule_core.gd` —— `:1157 _resolve_choose` · `:925 _choose_cand_match` · `:991 _choose_candidates`。
三个维度：**来源 src × 筛选 what × 动作 act**。实测分布：

```
src : pool 17 · deck 6 · dead 4 · hand 2 · enemy_hand 1
act : to_hand 13 · draw_it 5 · deploy_it 3 · return_to_deck 2 · to_enemy_hand 2
      lower_cost 1 · top_of_deck 1 · to_deck 1 · 【判不出】2
```

**照抄会错的 5 处**（都是我们数据比它全，或它自己会退化）：

| # | 问题 | 实况 | 我们怎么做 |
|---|---|---|---|
| 1 | `what` 里的 `stratagem` 它按 `type ∈ {tactic,defence}` 判（`:954`） | 卡池里 **`Stratagem` 是 `subtype`**（6 张） | 用 `subtype`，别用 `type` |
| 2 | `genomic enhancement` 它按**卡名前缀 `enhanced `** 判（`:971`） | `subtype` 里就有 `Genomic Enhancement`（1 张） | 用 `subtype` |
| 3 | `rune` 它按 `subtitle`/`name` 判（`:968`） | **`Rune` 就是 `subtype`**（3 张） | 用 `subtype` |
| 4 | 动作判定**搜全 desc**（`:1208` 有意为之）→ `Choose a troop in your hand` 会被后一句 `Lower its cost by 2` 判成 `lower_cost` | 那是**另一段**的活 | 只在本段判 act；跨段的活交给**已有的后续句解析**（见 §三） |
| 5 | act 判不出时**默认 `to_hand`**（`:1193`） | 会把 `Choose an effect…`（Leviathan 2 张）静默当成「进手牌」 | **判不出就报 unparsed**，不默认 |

---

## 三、🔴 最大的简化：**后续句现在就已经解析得了**

`EffectText.Split`（`Core/EffectText.cs:281`）**只按 `.` 切**。所以「动作在下一句」的卡天然分成两段：

```
Choose a troop from your deck.        ← 只有这一段解析不了（本轮的活）
Draw it and create a copy of it in your hand   ← 已经能解析
```

**实测：下面这些后续句都不在未覆盖清单里（= 现在就能解析）**

| 后续句 | 出处卡 | 状态 |
|---|---|---|
| `Draw it and create a copy of it in your hand` | Dimensional Corridor | ✅ 已能解析 |
| `It costs 2 less`（含 `[1] ` 前缀） | Webway Gate | ✅ 已能解析 |
| `Lower its cost by 1 / by 2` | Faeburn Vanquisher / Rapid Deployment | ✅ 已能解析 |
| `Give it Stealth` | Rogue Informant | ✅ 已能解析 |
| `Lower its Health to 1` | Eternal Servitude | ✅ 已能解析 |
| `Draw it and lower its cost by 3` | Emergency Dispensation | ❌ **仍不解析**（`lowercost` 尾句锚点问题，**独立小件**） |

⇒ **本轮只需让 `Choose …` 那一句本身解析出来**，并在结算时把选中的卡写进**已有的引用位**，后续句自动接上。

### 引用位（关键 —— 基础设施已经在了）

| 引用位 | 类型 | 现有用途 | 选牌要不要用 |
|---|---|---|---|
| `ctx.LastTarget` | `UnitState` | `it` / `the target`（**场上单位**） | 场上单位可写；选**手牌/牌库**的卡写不了 |
| `ctx.LastTargets` | `List<UnitState>` | `them`（一批单位） | 否 |
| `ctx.LastCreated` | `List<CardDef>` | 刚造出来的卡；`lowercost` 的 `(指代上一张)` 走它（`EffectResolver.cs:1489`） | ✅ **要写** —— 见下 |

**`Lower its cost by N` / `It costs N less` 已经映射成 `Payload = "(指代上一张)"`**（`EffectText.cs:1420-1439`），
结算时读 `ctx.LastCreated`。⇒ 选牌只要**照 `rule_core:1151` 把选中的卡同时写进引用位**，这一整类后续句**零改动接通**。

> `rule_core.gd:1151-1152` 原文：
> `ctx["last_created"] = cd` / `ctx["_chosen_card"] = cd` —— **两个都写**。

⚠️ **一个如实标着的既有近似**（`EffectResolver.cs:1474-1477`）：降费按**卡名**匹配，不是按卡实例
（「我们没有卡实例这个身份」）。手里两张同名卡会一起降价。**本轮沿用，不扩大**。

---

## 四、筛选词词表（实测，卡池 1130 张）

`CreatePool.KindWords`（`Core/CreatePool.cs:331`）**已有**：troop / unit / vehicle / infantry / battlesuit /
beast / drone / monster / daemon / structure / spell / tactic / combat elixir / elixir / sabotage / secret。

**要补 6 行**（全部是真实 `subtype`，卡池里数得出）：

| 卡面词 | `subtype` 值 | 卡数 |
|---|---|---|
| `Rune` | `Rune` | 3 |
| `Invocation` | `Invocation` | 3 |
| `Overlord Power` | `Overlord Power` | 3 |
| `Psychic Power` | `Psychic Power` | 7 |
| `Genomic Enhancement` | `Genomic Enhancement` | 1 |
| `Stratagem` | `Stratagem` | 6 |

（`Codicil` 3 张 —— 出现在半懂句 `choose a Codicil`，**不在本轮 30 条里**，但顺手一起加。）

卡池里 **50 种 subtype**（`cards_engine.json`），另有 `unit`/`Unit`、`spell`/`Spell`、`mission`/`Mission` 这类**大小写重复**。

---

## 五、⚠️ 更正：那 7 条「只在单位/英雄上」的**没有**被解锁（2026-09-13 查实）

**原来这里写的是「顺手也解锁」—— 错的。** 实测结论：

| | 数 | 说明 |
|---|---|---|
| 单位卡总数 | **586** | `cards_engine.json` |
| 效果写在 `keywords` 里（**引擎读这条**） | **25** | 形如 `'Rally: All enemies lose Stealth and Camouflage'` |
| 效果写在 `desc` 里、`keywords` 只有裸词条 | **266** | 形如 `desc='Rally: Stun an enemy'` + `kw=['Waystone','Flank']` |

**根因**：`EffectResolver.cs:437` —— `CanPlayTactic` 第一句就是
`if (card.IsUnit) return RuleCodes.ErrBadHand;  // 单位卡走 PlayCard，不是这条`，
所以 **`CardDef.Desc` 从来没进过 `EffectText.Parse`**。单位的效果只从
`CardDef` 构造时那圈 `keywords`（`CardDef.cs:88-110`）里来。

⇒ 下面这 7 条**今天仍然完全不生效**（卡面会显示，引擎不认），**与本轮的选牌 handler 无关**：

```
Choose a Black Legion troop and put it in your hand            (unit)
Choose a Combat Elixir and create it in your hand              (unit)
Choose a Saim-Hann Infantry and put it in your hand            (unit)
Choose a card in the enemy hand and shuffle it into their deck (unit)
Choose a non-Legendary Dark Angels card and add it your hand   (unit)  ← 原文就少个 to
Choose a troop from your deck and put it at the top of your deck (unit)
Choose an Astra Militarum troop and put it in your hand        (unit)
```

**这是一个独立的、比选牌大得多的缺口**（266 张单位的效果文字没接进引擎）。
不在本轮范围内，**单列一笔**——见交接文档。

---

## 六、两个**查不到**的（如实记，不猜）

| 句子 | 卡 | 查过哪 | 结论 |
|---|---|---|---|
| `Choose an effect and give it to a friendly troop` | `Hyper-adaptation`（Leviathan, tactic, 2 费） | 卡面文字 · `cards_engine.json` · 反编译 `d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out`（1800 个带方法体文件，按卡名/类名零命中） | **候选效果池不在任何文本里** —— 「选一个效果」是**另一套机制**，不是选牌。单列，别混进本轮 |
| `Choose an effect and give it to all troops in your hand` | `Infinite Biomorphologies`（Leviathan, tactic, 2 费） | 同上 | 同上 |

⚠️ 这两条**绝不能**按 rule_core 的默认值当成 `to_hand` —— 那是静默的错误语义。

---

## 七、本轮不动、但要点名的

- `Choose a card in your opponent's hand`（`Reconnaissance Mission`）—— 原文整句是
  `Choose a card in your opponent's hand. When played, gain 3.`，**选牌本身没有后续动作**。
  大概率是「看对手一张牌」的信息型效果，但要**单独定语义**，别默认。
- `Give it Stealth`（`Rogue Informant`）要作用在**手牌里的卡**上，
  而 `ctx.LastTarget` 是 `UnitState` —— 这 1 张要么补「手牌卡关键词」的存储，要么如实报 unresolved。
- `Draw it and lower its cost by 3`（`Emergency Dispensation`）—— `lowercost` 尾句锚点，独立小件。

---

## 八、本轮结果（2026-09-13 第二十六轮收工）

**做了什么**：

| 文件 | 改动 |
|---|---|
| `Core/CreatePool.cs` | `KindWords` **+7 行**（stratagem / rune / invocation / overlord power / psychic power / genomic enhancement / codicil）；新增 `FilterChoose`（候选筛选，**复用 `Resolve`**，不另写判据） |
| `Core/EffectText.cs` | `EffectOp` **+5 个字段**（`ChooseSrc/What/Act/DeadScope/Copies`）；新增 `TryChooseCard` + 2 条正则 |
| `Core/EffectResolver.cs` | 新增 `DoChooseCard` / `DeadCandidates` / `RemoveFromSource` / `RemoveRef`；派发表加一行 `choosecard`；`DoCreate` 的 `CopyOfPrev` 优先用 `LastChosenCard` |
| `Core/BattleContext.cs` | 新增 `LastChosenCard`、`DeadUnits`（阵亡登记表）、`TakeFromGraveyard`、`DeadUnit` 类 |
| `Core/PlayerState.cs` | 新增 `LastTurnStartMark`（`since your last turn` 的窗口起点） |
| `Core/RuleCore.cs` | `BeginTurn` 写窗口标记；`KillUnit` 写阵亡登记 |
| `Editor/RuleEngineTest.cs` | 新增 `TestChooseCard`（**+249 条断言**）；实战检查加「选牌结算成功」计数 |

**数字**（四条自检全绿）：

> ⚠️ **这是第二十六轮那一轮的快照**（改前 / 改后对比），**不是当前值**。
> 当前数字**只在** `资料/阵营推进_清单与交接.md` §五 一处 —— 2026-09-13 A3/A4 收工时
> 战术卡完全解析已经是 **425/449**（本表里的 383 是那一轮的）。

| | 改前 | 改后 |
|---|---|---|
| 战术卡完全解析 | 354/449 | **383/449** |
| ①「完全不认识」 | 88 种 / 91 次 | **60 种 / 60 次** |
| `RuleEngineTest` | 856/856 | **1106/1106** |
| `BattleScene` / `DeckScene` / `CardBaseDemo` | 356/0 · 61/61 · 全过 | 不变 |

**又一处「照抄 rule_core 会错」（实测新增，补进 §二 那张表）**：
`Choose a Genomic Enhancement` 它整体**排除**（`:1163`，因为自己判不准）；
我们**不排除** —— `subtype` 里就有 `Genomic Enhancement`（1 张），判得准。

**如实标着的局限**：

1. 选法是 `ctx.Rng` 从**全集**等概率取 1，与原版「洗牌取前 3、玩家选 1」**分布相同但随机数消耗序列不同**
   —— 别拿它和 `rule_core` 逐帧对随机。表现层的选牌 UI 是**另一件**（`阵营推进_清单与交接.md` §四已点名）。
2. ~~`FilterChoose` 的兵种/阵营判据**拿全卡池当判据集**（复用 `CreatePool.Resolve`），
   代价：**不在卡池里的卡筛不出来**。~~ 
   ⚠️ **2026-09-13 第二十七轮已修**：改成**逐候选直接判**（`type`/`subtype`/`faction` 本来就是
   卡自己的字段），池子只剩「解析阵营词」一个用处。原因与代价见
   `资料/回手与指代_数据与设计.md` §五 —— 那条局限在自检夹具里真撞到了，而且报错很难查。
3. `Lower its cost by N` 按**卡名**匹配（既有限制，`EffectResolver.cs:1474` 早就标着），
   手里两张同名卡会一起降价 —— 本轮**沿用，没扩大**。
