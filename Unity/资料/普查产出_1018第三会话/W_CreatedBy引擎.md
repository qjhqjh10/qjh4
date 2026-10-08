# W_CreatedBy引擎 —— 引擎侧断言（`A985④`）+ ruleprobe 基线重建（2026-10-18 第三会话）

**只动一个文件**：`RuleEngine/Editor/RuleEngineTest.cs`（本笔 **+134 / −0** = 新用例 130 行 + `Run()` 注册 4 行；该文件 `git diff` 里那 1473 行是**本会话前四位写手**留下的，不是本笔）。另重建旁挂产物 `Unity/工具/ruleprobe/基线/out_baseline.txt`（103/103 行，见 §二）。⛔ 没跑 Unity、没动 git、没改正本、`Core/**` 全程只读。

---

## ① 断言逐条

**新增用例** `TestCreatedBy()`（定义紧跟 `TestCompanion()` 之后，`:11558`）；`Run()` 注册在 `A985⑥` 那一节之后（`:198`）。

**夹具**（一张对局跑完八条）：`BattlePool({poke,tide,host}, foeDeck 10, pool={host,drone,tide,poke}, "TauEmpire")` → `ToP1Turn(ctx,4)`。
`poke` = `Tactic("T_CbPoke",0,"Deal 1 damage to a friendly unit")` = **「上一张」**（战术必走 `ResolveOps(..., sourceCard: card)`）；`tide` = `FixtureCbTide`（`Tide 1`·1 费）；`host` = `FixtureCbHost`（`Companion 2: FixtureCbDrone`·2 费）；`Place(ctx,0,3,Unit("CbDummy",1,0,9))` = 战术靶子（9 血挨 1 点不死）；起手 3 张 = `hand0` 顺序。

| # | 断言 | 说明 |
|---|---|---|
| 1 | `!ctx.HasCreatedBy(hand0["T_CbPoke"])` | **两态（反）**：抽到的那份没有来源 |
| 2 | `ReferenceEquals(ctx.PlayingCard, poke)` | **夹具前提**（读内部字段只为坐实）；不成立则夹具退化成「上一张==null」 |
| 3 | `ctx.HasCreatedBy(tideCopy)` | 潮涌复制品被盖章 |
| 4 | `ReferenceEquals(ctx.CreatedByOf(tideCopy), tide)` | **来源 = 刚打出的那张**（`SpawnTideCopies` 那一跳）|
| 5 | `!ReferenceEquals(ctx.CreatedByOf(tideCopy), poke)` | 🔴 **灭自证 1**：来源**不是上一张** |
| 6 | `!ctx.HasCreatedBy(tidePlayed)` | 「挪一份 ≠ 造一份」：`PlayCard` 原样搬上场、没走 `NewInstance` |
| 7 | `ReferenceEquals(ctx.CreatedByOf(comp), host)` | **伴生**（`PlayCompanions`，**另一条**造牌路）也记刚打出的那张 |
| 8 | `ctx.CreatedByCount == cbBase + 2` | 账：全局只 2 份有来源；`cbBase` 在 `Place` 之后取（`Place` 自己也走 `NewInstance`）|

**「伴生 / 潮涌」那一跳怎么钉的**：两条造牌路都在 `PlayCard` 里、**`NotePlayed` 之后**、**早于任何 `ResolveOps`** —— `SpawnTideCopies`（`RuleCore.cs:1654` → `:1763 ctx.NewInstance`）· `PlayCompanions`（`:1661` → `:1929`）。⇒ 只靠 `ResolveOps` 入口写 `PlayingCard` 的话来源会停在**上一张**。夹具**故意先打一张战术**（它的 `ResolveOps(..., sourceCard: card)` 把 `PlayingCard` 钉在战术上），再打潮涌 / 伴生 —— **两条路各钉一次**。

**🧨 改坏法**（两条各自能红、互不覆盖）：① 删 `BattleContext.NewInstance` 的 `if (creator != null) _createdBy[inst.Id] = creator;` ⇒ **②③ 全红**（①④⑤⑥ 绿）；② 删 `BattleContext.NotePlayed` 的 `PlayingCard = inst != null ? inst.Card : null;` ⇒ **只有 ③ 红** ← 它就是单独存在的理由。

**灭自证**（两条，都结构性）：① **③ 的两半** —— 来源在实现里是**一个对象引用** ⇒「同一引用既是 `tide` 又是 `poke`」不可能成立 ⇒ 退回旧写法时 4 与 5 **一起红**，不会只红一条被顺手改绿；② **⑥ 挡退化实现** ——「按卡定义盖章 / 谁上场都盖章」那种写法下 ③ 可以全绿、**⑥ 必红**（同一个 `CardDef`、两份实例，一份该有来源、一份不该有）。

**⚠️ 这两条从没跑过**（我不许跑 Unity）：夹具前提是**读代码推的**（`PlayingCard` 残留 · 起手 3 张 · 能量够不够）。能量：`MaxEnergy = startingMana(1) + TurnCount(4) × 1 = 5 ≥ 3`（`RuleCore.cs:744-745`）⇒ 够。**收口必须跑 `RuleEngineTest.Run` 才算数。**

---

## ② 探针基线（ruleprobe）

**旧基线备份** → `d:/4/_tmp_view/ruleprobe_out_baseline_旧_1018第三会话.txt`（71,476 B · md5 `1b744777585b911793fa0b2c82c9e939`，与备份前**逐字节相同、已核**）。中间产物也在：`_tmp_view/ruleprobe_scan_new_1018c.txt`（新 dump，md5 `1966edd2…` = 重建后基线）。

**重建前**：`全池 1120 张 · 解析差异 103 行 · 改名 0 · 新增 0 · 消失 0`（rc=1）
**重建后**：`全池 1120 张 · 解析差异 0 行 · 改名 0 · 新增 0 · 消失 0`（rc=0）✅

**尺子 A（差异来源）**：拿 `git show HEAD:…cards_engine.json` 逐条比 `desc` —— **100/103 行是卡面文本自己改了**（desc `+2 Melee` → `+2 [Melee]` 这种方括号化；全池 **180 张** desc 被改，其中 100 张落进探针）⇒ 解析产物跟着变、**不是解析器动的**；**3/103 行是纯解析器侧**（desc 未变）：`DA11` · `DA86` · `UM84`。

**尺子 B（变了什么）**：

| 类 | 行数 | 长相 |
|---|---|---|
| A 载荷属性词规范化 | 57 | `N melee/ranged attack`→`N melee/ranged`·`weapon`→`ranged`·裸 `+N`→`+N attack` |
| D1 载荷文本里补属性词 | ~24 | `+1 and flank`→`+1 attack and flank`；`give +1 to your units`→`… +1 attack …` |
| B 只动 `unparsed`/`partial` 第 2/3 列 | 14 | `+1 Attack`→`+1 [Attack]`；`[Oath]`→`Oath`（载荷一字未变）|
| D2 词条/属性词被纠正 | 5 | 见下表 |
| C 载荷叠词 | 4 | `invulnerable invulnerable`·`stomp stomp`·`shield shield`·`a dark pact dark pact` |

**D2 那 5 条对过卡面英文原文（= 新基线读的那份 `desc`）—— 新的那侧才对、旧基线是错解析**：

| 卡 | 旧 | 新 | 卡面原文 |
|---|---|---|---|
| `DA11` Sergeant Naaman | `gainenergy/pool///0/1 energy` | `gainquest/pool///1/` | `Slay: Gain 1 Quest Point` |
| `DA86` | `… gains 2"` | `… gains 2 quest points"` | （同） |
| `EC32` Dual Screamer | `-4 attack and -4 health` | `-4 attack and -4 ranged` | `-4 [attack] and -4 [Ranged]` |
| `UM_Inceptor_Sergeant` | `+1 attack` | `+1 ranged` | `Give +1 [Ranged] to your other troops` |
| `UM_Primaris_Inceptor` | `+1 attack` | `+1 ranged` | `Codex: Gain +1 [Ranged] this turn.` |

（`EC33`/`SOR44` 的 `+1 weapon`→`+1 ranged` 落在 A 类，同理 —— 与铁律 7 那条「`[weapon]` 是**紫圈枪=远程**」一致。）

**C 那 4 条不是缺陷**：卡面**本来就写着**「记号 + 本地词」`[Invulnerable] Invulnerable` / `[Stomp] Stomp` / `[Shield] Shield` / `[Dark Pact] Dark Pact`（逐张核过 `desc`）⇒ 新解析只是**忠实回声**。⚠️ 载荷变成叠词（下游按词表匹配、**无语义差**），但**要不要在载荷里去重**是一笔**还开着的账**，不在我白名单。

**结论**：103 行**全部**由本会话**未提交**改动引起（`Core/**` 解析器 + `Resources/cards_engine.json` 卡面文本），**改名 0 · 新增 0 · 消失 0**，抽查的 5 条语义变化**方向全对** ⇒ 重建基线成立。

---

## 类型检查 · 行尾 · 没查清

- **秒级类型检查**（`TMPDIR=/tmp/wf_cbe`）：`运行时错误数: 0` · `编辑器错误数: 0` ✅（两处编辑之后跑）
- **行尾**：`RuleEngineTest.cs` = **纯 CRLF 21847/21847 未翻**（numstat `1603 15`）；重建的基线 = **纯 LF 1120/1120**（生成器写 `\n`、与旧基线同形；numstat `103 103`）
- **没查清 / 不是我的活**：① **断言从未执行**（见 §一末）—— 收口跑 `RuleEngineTest.Run` 才算数；② `cards_engine.json` 那 180 张 desc 的方括号化是**别人那一笔**，我只判「它是不是探针差异的来源」、**没裁断它对不对**（抽查 5 条与铁律 7 一致）；③ 伴生那条**没再钉**「卡池同名多份 ⇒ 出声」（`TestCompanion` ④ 已覆盖）。
