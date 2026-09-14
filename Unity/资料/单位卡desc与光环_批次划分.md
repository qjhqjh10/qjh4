# 单位卡 desc 缺口 + 光环族：批次划分（2026-09-14）

> 子代理只读普查的产物，**给下一个会话照着排批次**。
> 权威清单仍是 **`_tmp_view/unit_desc_unparsed.txt`**（自检每次重写）—— **数字别抄这里**。
> ⚠️ 这份是**分类与优先级**，不是「已做完」。

---

## 一、结论先行（三件改变批次的事）

### ① 光环 **29 张**，而设计稿对它**零覆盖**
`光环 10 张 + 同族「己方全体 have Y」19 张 = 29 张`，**是同一件事**
（筛选维度从「相邻」换成兵种 / 关键词 / 敌方），**该一起做**。

🔴 但 `资料/常驻效果_数据与设计.md` 全文检索「光环 / 相邻 / adjacent」= **0 命中** ——
它覆盖的是**另三支**：回合起止（§一/§二）、部署时给（§六/§七）、持续改费。
**部署时给 ≠ 光环**：前者是**事件**驱动（一次性 `give`），后者是**读时**驱动。
⇒ **A7 不是「照已有设计做」，得先给它新开一节。**

### ② 交接文档点名的两份反编译，**第二份不是光环**
- ✅ `CardScript__HasWhileInPlayAdjacentEffect.c` —— 是个**谓词**
  （`HasCurrentTrait(0x2f8)` 或「有一条 WhileInPlay 能力、其 affected 列表第一项 == 100 = `Self`」）。
  ⚠️ **在 1800 个 `.c` 里没有任何调用者** —— 「原版拿它干什么」**查不到**。
- ❌ `BattleManager__ShouldTriggerAdjacentDamage.c` —— 整个方法体是
  `return 0 < EntityScript.get_CurrentBlast(target)`，**那是爆裂（Blast）的判据**，
  我们**已经实现**（`RuleCore.cs:1213`）。**别把它当光环证据。**
- 旁证：`CardScript__HasWhileInPlayAbility.c` 认 ability type `0x14`/`0xf0` + trait `0x82`，
  `BattleManagerSupport` 的 48 个广播里有独立的 `WhileInPlay(...)` ⇒
  **原版确有一条「在场时」通道，和部署那条是两条。**

### ③ 最大的两条**是假缺口**（一行代码都不用写）
`Talent: <名>` **35 次** + `Companion N: <名>` **8 次** = **43 次**，
自检缺的是**白名单**，不是 handler —— 两条路**都已实现**：
`CardDef.TalentName` + `RuleCore.SpawnTalents`（规则书 `:218`）·
`CardDef.CompanionName` + `RuleCore.PlayCard`。
⇒ **把这两族放进自检白名单**，别再去实现一遍。

### ⑤ 🔴（2026-09-14 A4 批 4 实测补）**下面这张表里的「现成的层 ✅」那一列，要重新理解**

表里凡标 **「✅ 现成的层」** 的族（`When <事件>, deal …` / `When <事件>, give/heal …` /
部署时给 —— 合计 **≈31 次**），**机制已经在跑**，缺的**不是机制、是报表**：

- 单位卡的 `When <事件>, <正文>` **不走 `EffectText.Parse`**：它走 `CardDef.AddWhenTrigger`
  （`WhenEvents.ParseAll(事件短语)` + `EffectText.Parse(正文)`）——**只看正文那半句，不看壳**。
  两张都成功就注册成监听器（`WhenTriggers`），`BroadcastWhen` 在事件发生时消费它。
- 实据：`_tmp_view/when_unparsed.md` 里 **「认不出的**事件短语** 0 种」**、带 `When` 的卡 78 张点亮 73 张；
  自检 `TestWhenEvents` 里有**结算级**断言（友方 troop 死 → 监听器真的改攻）。
- ⇒ 所以 `unit_desc_unparsed.txt` ① 栏里那些 `When …` 句子是 **`EffectText` 认不出「那句壳」**，
  **不是「这效果不生效」**。
- ⇒ 而**卡面不会因此骗人**：`*` 那条装饰只打在**战术卡**上
  （`BattleDriver.cs:2222` 明写 `c.Type == "tactic" && !IsFullyParsed(...)`）；
  单位卡的 `*` 只跟**没实现的关键词**走。

🔑 **⇒ 批 1 的真正工作只剩三件（口径变了，按这个做）**：
1. **把报表判据改对**（像 2026-09-14 给手牌陷阱做的那样）—— 让「单位卡的 `When …` 已由事件层接手」
   这件事被**数出来**，而不是继续挂在「完全不认识」里虚低。
   ✅ **2026-09-14 A5 批 1 做完了**：`CardDef.HandledByOtherLayer(c, seg)`（判据全部**转调**
   那三个采集器用的抽取函数，**一行新文法都不写**）+ `EffectText.Coverage` 里按**分句**过一遍。
2. **`Heals N` / `Heal N`（无目标 = 自己）** —— 这一族是真缺。
   ✅ **2026-09-14 A5 批 1 做完了**：`TryHeal` 里补 `Subjectless` 那一支
   （⚠️ **只在「本句没有尾句」时套**，否则 `Heal 5 and give Camouflage to a friendly unit`
   会从「治它」变成「治自己」；⚠️ `SplitAndTail` 给 `tail` 的初值是**空串不是 null**）。
3. **`Stealth (1)` / `Has Flying during your turn`** 这种**带后缀/括号的纯关键词声明**剥不干净。
   🔴 **照卡图核过之后降级**（2026-09-14，铁律 7）：
   - 全池 `词 (数字)` 这种写法**只有 2 处**：`Stealth (1)`（`Death Spinner Warp Spider`，[unit]）·
     `Gain (1)`（`Martial Superiority`，DarkAngels 战术卡 —— 那是**切分残渣**，不是关键词）。
   - **卡面长什么样**（`d:/2/Warpforge部队卡片/Aeldari/3部队/Zrzut ekranu 2026-04-16 o 18.41.16.png`
     —— ⚠️ **文件名是波兰语截图名，按卡名搜不到**）：橙字卡名 `Death Spinner Warp Spider` ·
     `[眼图标] Stealth.` **后面跟一个绿色圆徽标 `1`** · 下一行 `Gain [⟳图标] Flank` ·
     橙字兵种行 `Infantry` · 费 1（右上蓝六边形）/ 近战 1 / 远程 3 / 生命 1。
     ⇒ **那个 `1` 是挂在 `Stealth` 上的关键词值**（`Stealth 1`），不是 `Gain Flank` 的费用。
   - **语义上等价**：规则书 `:211` 潜行 =「一回合内或本单位攻击前，不能被任何方式选中」——
     **没有数字参数**；而本引擎的口径是「**无数字的关键词存在即真、取 1**」。
     ⇒ `Stealth (1)` ≡ `Stealth`。**我们的卡表里本来就存的是裸 `Stealth`** ⇒ **机制零差别**。
   - ⇒ **唯一没解析的只是 desc 文本里那个 `(1)`**，而它的下游影响是**零**：
     单位卡不打 `*`、不参与牌组校验；`Stealth` 已在 `keywords` 里 ⇒ 机制照常跑。
     **为一个不改行为的收益去动 `IsKeywordOnly`（1130 张卡每一句都走它）= 不值**。
     ⇒ **不做**；真要收就一起收那 4 处，并把「`(1)` = 关键词值、与无数字等价」写明。
   ⚠️ **同一格里那句 `Has Flying during your turn` 是另一回事，别混**（`Commander O'Maisos` ·
     `Valius Paxor`，**两张督军**）：那是「带**时长限定**的关键词声明」——
     `during your turn` **不是废话**（飞行只在自己回合有效），按静态关键词收**会丢语义**。
     ⇒ 它**归到督军那 13 张**（§一⑥ 末），**要单独定「时长限定怎么表达」，不能顺手当关键词吞掉**。
   外加 **`Talent:` 35 次 + `Companion N:` 8 次进白名单** ✅ 同一批做完（`HandledByOtherLayer`）。
   ⚠️ **光环 29 张是 A7**，不在批 1 里。

### ⑥ 🔴（2026-09-14 A5 批 1 实做后实测）**单位卡的数字跳了一大截，跳的是「尺子」不是「机制」**

`HandledByOtherLayer` 接上之后（同一批代码，**一行机制都没新写**）：
`[unit]` 完全解析 **429 → 516/586** · `[hero]` **0 → 43/56** · `[defence]` 39/39 不变。
（那两组数是**批 1 那一刻**的快照；**现状**是 `[unit]` **531/586** —— 批 2/3 又前进了一截，
同样**机制与尺子都有**。当前值只认 `_tmp_view/unit_desc_unparsed.txt`。）
⚠️ **别把这个跳跃读成「实现了 87 句」** —— 它是**报表判据改对**（那三族本来就在跑）。
⚠️ 而且它**顺带推翻本节下面那句**：原来写「**[hero] 是完全解析 0/56，那是结构性缺口** ——
督军卡 desc 整条没走通」——**不成立**。实测督军卡 desc 的主体现在也是**那三族**
（`Talent: <名>` / `When …` / 纯关键词），所以它**不是结构性缺口**，
是**同一个「壳认不出」的问题**（现状 43/56）。
🔑 仍然成立的那半：督军卡**还有** 13 张没走通，而那 13 张缺的是**别的东西**（值得单独核一遍）。

### ⑦ ✅（2026-09-14 A5 批 2/3 收工）**「裸写效果、没有触发点」那一族 —— 27 句全部定案**

做批 1 时顺手扫出来的 **27 句**（`[unit]/[hero]` 里 **desc 裸写效果句、又没有任何 `X:` 前缀**），
2026-09-14 派了 **3 路子代理**逐条查证（产出 `资料/查证_裸写触发点_{SaimHann,EC,Leviathan}.md`），
**已全部定案**：

| 族 | 张数 | 结论与落点 |
|---|---|---|
| **`Codex` 裸写**（原【A】） | **10 张**（全是 Ultramarines） | 修法 = `codex` 加进 `CardDef.BodyKeywords`。⚠️ **文档原写「9 张」是错的 —— 漏了 `Sergeant Allectius`**（`Blind a random enemy`，而 `blind` 是**已实现**的动词）⇒ **实测 10 张**（`cards_engine.json` 可复算：声明 `Codex` 的 20 张里 `desc` 无前缀的正好 10 张）。**顺带查出更根本的一件事**：Codex 的**自动触发点整条不存在**（参考实现 `rule_core.gd:2397 _check_codex` 有**三个**调用点）⇒ 另补了 `RuleCore.CheckCodex` |
| **「被这套打过的单位」**（原【B】一半） | 6 张 | 原版 `AbilityTrigger.UnitAttack = 50`（与 Slay/Strike **同一个函数**）。**已实现**（`CardDef.AttackedOps`）。见 `资料/查证_裸写触发点_EC.md` / `_Leviathan.md` |
| **`Stimulation`**（`Lord Exultant`） | 1 张 | 卡面印着 `[图标] Stimulation:`，我们卡表 `keywords` 为空 ⇒ 走**数据修正**（`cardface_fixes.json` 的 `_manual_keywords`） |
| **灵族「花 N 颗灵魂石激活」**（原【B】另一半） | 3 张（`Autarch` / `Wraithblade` / `Wraithlord`） | 原版 `UseSpiritStone = 600`；**同一族共 17 张**（`【绿圈N】` 是灵族阵营货币）。见 `资料/查证_裸写触发点_SaimHann.md` —— ⛔ **未做**（要连「玩家主动花石激活」这个动作一起做，是独立一轮） |
| **`Ecstasy N`**（`Tormentor Obsessionist` 等） | — | 归到「未实现关键词 `ecstasy`」那一档（⛔ 挂起） |

> ⚠️ 这一族**不在** `unit_desc_unparsed.txt` 的 ① 栏里（那 27 句**解析得出来**、载荷也**有机制**）
> ⇒ 报表**看不见它们**，而它们**永远不会发生**。这正是「覆盖率绿了、机制没跑」那一类 —— **别只盯 ① 栏**。
> （这一族 2026-09-14 已清空，但**同类问题还会有**；判据是「解析得出 + 有机制 + **没有触发点**」。）

### ④ 🆕（2026-09-14 A4 批 4 补）单位卡 `desc` 的 4 句**不是靠做 A5 修好的**

`EffectText.SplitAndTail` 原来只看**第一个** ` and `，载荷里自带 ` and ` 时切不开 ⇒
**后半句被吞掉而整句还判「认了」**。改成往后扫第一个「动词开头」的 ` and ` 之后，
`[unit]` 完全解析 **425 → 429**（`Pray: Gain +1 Attack and +1 Ranged Attack and heal 1` 那类）。
⇒ **下面这张表的「卡数」是按这一轮之前的清单分的，做 A5 时以
`_tmp_view/unit_desc_unparsed.txt` 为准**（自检每次重写，这里的数字会过期）。

---

## 二、单位卡 desc 缺口：按**句型族**归类（族 ≈ 20 ⇒ 4 个可做批次 + 3 条挂起）

> 🔴 **2026-09-14 A5 批 2/3 收工后的进度**（下表按**当时的清单**分族，做没做以这里为准）：
> - ✅ **批 1 做完**（`Heal N` 无目标=自己 · 报表判据 · `Stealth (1)` 照卡图定案「不做」）
> - ✅ **批 2 做完**：`At the start/end of **each** turn`（含 `you`/`each` 两种视角与**后缀式**）·
>   `When …, deploy/create`（含 `Whenever` / **缺逗号** / `After receiving a Dark Pact` 无 `When`）·
>   `Takes N damage at the start of your turn`（**主语省略 = 本卡自己**）
> - 🟡 **批 3 做了 4 条**（`Returns to your hand` · `create … in hand` · `Draw the next <类型> in your deck` ·
>   `[Talent]:` 方括号）；**剩 5 条**（`costs 2 more this turn` / `next Stratagem this turn costs 0` /
>   `Choose and gain a bonus (…)` / `Each of your units deals …` / `Other friendly X deal …`）
>   —— 逐条的**卡点**在 `资料/阵营推进_清单与交接.md` §一之四
> - ⬜ **批 4 未动**（静态改战斗规则 5 · 督军专有 5 · 13 张督军）
> - 🔴 **另有一条批次外的**：**跨句触发正文**（参考实现的分段规则和我们不一样，实测影响 **42 张**）
>   —— 见 `资料/阵营推进_清单与交接.md` §一之四

| 族（句型模板） | 卡数 | 例卡 | 同一个机制？ | 现成的层 | 建议 |
|---|---|---|---|---|---|
| `Talent: <名>` / `[Talent]: <名>` | 31 种 / 35 次 | `Reanimate` · `Serene Unifier` | ❌ **已实现** | ✅ `CardDef.TalentName` + `RuleCore.SpawnTalents` | **不做** → 进自检白名单 |
| `Companion N: <卡名>` | 7 种 / 8 次 | `Marker Drone` · `Gun Drone` | ❌ **已实现** | ✅ `CardDef.CompanionName` + `RuleCore.PlayCard` | **不做** → 同上 |
| `When <事件>, deal N damage to a random enemy` | 13 种 / 13 次 | `When a friendly unit Prays…` | ✅ 事件→伤害，**事件词表已全** | ✅ `WhenEventKind` + `BroadcastWhen` + `ReDeal` | **批 1** |
| `When <事件>, give/heal <目标>` | 8 种 / 8 次 | `When a friendly unit prays, give it Armour 1` | ✅ 同上一族，载荷换成 give/heal | ✅ `ReGive` / `ReHeal` | **批 1**（同一笔） |
| `Heals N` / `Heal N`（**无目标 = 自己**） | 7 种 / 7 次 | `Strike: Heals 4` · `Codex: Heal 1` | ✅ 隐式主语自愈 | ⚠️ 半有：缺「无目标 ⇒ 自己」那一支 | **批 1**（最便宜） |
| 部署时给 `When you deploy/play a <X>, give it <Y>` | 9 种 / 10 次 | `Antaro Chronus` · `Eliminator Sergeant` | ✅ 部署事件 + 卡面筛选 | ✅ **已建好**：`EffectTargetSpec.Deployed` + `ResolveDeploy` + `CardCriteria` | **批 1**（设计稿说「解封直接用」） |
| 纯关键词声明带后缀/括号 | 3 种 / 4 次 | `Stealth (1)` · `Has Flying during your turn` | ✅ | ⚠️ `IsKeywordOnly` 在，`(1)` / `during your turn` 剥不掉 | **批 1（顺手）** |
| `At the start/end of **each** turn, …` | 3 种 / 3 次 | `At the end of each turn, deal 1 damage…` | ✅ **`each turn` ≠ `your turn`** | ⚠️ `TryAtTurn` / `RePersistAtTurn` 只认 `your turn` | **批 2** |
| `When …, deploy/create <卡>` | 6 种 / 6 次 | `When an enemy attacks, deploy a Spore Mine` | ✅ 事件 + 造牌 | ✅ 大半有：`ReDeploy`/`ReCreate` + `CreatesSecret`/`CreatesSabotage` | **批 2** |
| `After receiving a Dark Pact, …`（**无 When 前缀**） | 2 种 / 2 次 | `After receiving a Dark Pact, deal 3 damage…` | ✅ 上一族变体 | ⚠️ `GetsDarkPact` 已有，缺「无 When」这一支 | **批 2** |
| `Takes N damage at the start of your turn`（反语序） | 1 种 / 1 次 | 同左 | 同 `each turn` 族 | ⚠️ `ReTakeDamage` 上轮已加，差与 `AtTurn` 复合 | **批 2** |
| 🔴 **光环 `Adjacent units/troops have X`** | 8 种 / 9 次 | `Baneblade Tank` · `Makari the Grot` | ✅ | ❌ **无** | **A7 本批** |
| 🔴 **己方全体光环 `Friendly/Your other/Other friendly X have Y` + `Enemies have Y`** | 19 种 / 19 次 | `Cadre Fireblade` · `Triarch Stalker` | ✅ **和上一行同一件事** | ❌ 无 | **A7 本批（同一笔）** |
| 前缀触发体（`Rally:`/`Backlash:`/`Agenda:`/`Codex:`）载荷怪 | 6 种 / 6 次 | `Backlash: Returns to your hand and costs 2 more` | ❌ 不是一族，载荷动词各异 | ⚠️ `ReReturn`/`ReDraw` 都在；差 `Returns` / `the next Stratagem in your deck` 这类变体 | **批 3（逐条）** |
| 「静态改战斗规则」 | 5 种 / 5 次 | `Any attack against your Warlord targets this troop instead` | ✅ 读时改战斗判据 | ⚠️ 半有：`FieldAttack` + `IsValidTarget` | **批 4** |
| 督军专有：裸专名 / `Start the game with X in hand` | 5 种 / 5 次 | `Ethereal Supreme` · `Chosen of the Four` | ❌ 是**开局长效** | ❌ 无 | **批 4** |
| 「改别的机制的规则」 | 5 种 / 5 次 | `When a friendly unit triggers Mob, it triggers an additional time` | ✅ 改**别的机制**的次数 | ❌ 无 | ⛔ **挂起** |
| `Ecstasy N: <正文>` | 7 种 / 8 次 | `Ecstasy 5: Double this troop's [Melee] and [Ranged]` | ✅ 血量阈值触发 | ❌ **`ecstasy` 是故意没机制的三个关键词之一** | ⛔ **挂起**（先补 `ecstasy`） |
| 事件触发式降费 | 3 种 / 3 次 | `Lower cost by 2 every time a friendly unit triggers Synapse` | ✅ **一整层** | ❌ 无 | ⛔ **挂起**（设计稿明说「别顺手，单独一轮」） |
| 坏数据 | 1 种 / 1 次 | `3 Gain` | —— 不是句子，是切分残渣 | —— | ⛔ **退回数据侧** |

**批 1 覆盖了 `[unit]` ① 栏 146 次里的约 90 次** —— 先做它。

🔴 **2026-09-14 A5 批 1 更正（铁律 5）：这一段原来写的三条都不成立。**
原文是「**[hero] 是完全解析 0/56，那是结构性缺口** —— 督军卡 desc 整条没走通，
**不是「几句认不出」**……建议和 [unit] 的逐句缺口分开算账」。
实做之后（`HandledByOtherLayer` 接上，**一行机制都没新写**）：**[hero] 现在是 43/56**。
⇒ ① 它**不是**结构性缺口；② 督军 desc 的主体**也是**那三族（`Talent: <名>` / `When …` /
纯关键词）⇒ **恰恰就是「几句认不出」**；③ 所以**不该分开算账**，它和 [unit] 是同一个问题。
🔑 **仍然成立的**：还有 **13 张督军没走通** —— 那 13 张缺的是**别的东西**，
和 [unit] 剩下的 70 张同性质，**值得单独核一遍**（见 §一⑤ 的「三条账」表）。

---

## 三、光环族逐卡（29 张）

### 3a 「相邻」型（10 张，筛选维度 = 相邻格）

| 卡（阵营） | 卡面原文 | 给谁加什么 |
|---|---|---|
| `Baneblade Tank`（AM） | `Armour 2. Adjacent units have Armour 1. Duty: Deal 8 damage` | 相邻格 +1 护甲 |
| `Genestealer Familiar`（GS） | `Adjacent units have +1. Rally: Give Flank to a friendly Infantry.` | ⚠️ **`+1 什么不明`**（原文残缺、无属性词）**语义待查** |
| `Damaged Plasmacyte`（Sautekh） | `Adjacent units have Regeneration 2` | 关键词型 |
| `Nemesor Zahndrekh`（Sautekh·hero） | `Adjacent Remnants do not disappear at the end of your turn. Talent: Relentless March` | 🔴 **不加属性 —— 改残骸寿命**（和其余 9 张**不是同一个 handler**） |
| `Banner Nob`（Goff） | `Adjacent units have +1 [attack] and +1 [weapon]` | 相邻 +1 近战 +1 武器 |
| `Makari the Grot`（Goff） | `Adjacent units have +1 Attack. Backlash: Returns to your hand…` | 相邻 +1 近战 |
| `Wolf Guard Battle Leader`（SW） | `Adjacent units have Pack.` | 关键词型 |
| `Enforcer Battlesuit`（Tau） | `Vanguard. Adjacent units have +1 Ranged Attack.` | 相邻 +1 远程 |
| `Honoured Ethereal`（Tau） | `Shield. Adjacent **troops** have Vanguard.` | 关键词型 + **兵种筛** |
| `Honour Guard`（UM） | `Adjacent units have Armour 1` | 相邻 +1 护甲 |

### 3b 「己方全体」型（19 张，筛选维度 = 兵种 / 关键词 / 排除自己）

| 类型 | 卡 | 卡面 |
|---|---|---|
| **排除自己** | `Cadre Fireblade`（Tau）· `Triarch Stalker`（Sautekh） | `Your other Infantry and Battlesuit troops have +2` · `Your other units have +2 Ranged Attack` |
| **带筛选** | `Company Ancient` · `Bladeguard Ancient`（UM）· `Winged Autarch`（ASH） | `Your other units have +1 Melee and +1 Ranged Attack` · `…+2 [strength]` · `Your other Flying units have +2 Melee and +2 Ranged Attack` |
| **关键词型（5 张同一句法）** | `Iron Priest`（SW）· `Ravenwing Talonmaster`（DA）· `Devilfish`（Tau）· `Vitus Gryf`（AM）· `Chimera`（AM） | `Friendly Vehicles have Regeneration 1.` · `…Flank.` · `…Flank.` · `Friendly troops with Flying have Flank.` · `Friendly Infantry troops have Armour 1.` |
| **带条件/带值** | `Fyrri Askar`（SW）· `Whirlwind`（UM） | `Friendly units with Pack have Invulnerable during your turn` · `Other friendly units have Sentry 2` |
| **敌方全体 / 小类** | `Bringer of decay`（BL）· `Rugged Disharmonist`（EC）· `Nob on Smasha Squig`（Goff）· `Grukk Face-Rippa`（Goff）· `Ramatekh The Cruel`（Sautekh）· `Alluress`（EC）· `Winged Daemon Prince`（EC） | `Enemies have Vulnerable 1` · `Enemy troops have -2 Attack` · `Other friendly Beasts have +1 [attack] and +1 [health].` · `Your troops with Tide have +1 Attack.` · `Your other Destroyers have +1 [Attack].` · `Friendly Daemonette have Flank.` · `Other friendly Daemons cost 2 less and have +2 [attack]`（**费用+属性合体，唯一一张**） |

---

## 四、A7 动手前必须定的事（都在只读范围内查到的）

1. **引擎里没有「常驻光环层」**。`ctx.PersistentEffects` 是**事件驱动**的
   （`Trigger="turn"|"deploy"`，登记后**没有移除路径**，也不问「来源还在不在场上」）
   ⇒ 直接拿它做光环 = **违反纪律①（光环是持续效果，人走效果没）**。
2. **该照「读时现算」的形状做**，仓里有三个先例：
   `RuleCore.FieldAttack`（**所有攻击力修正的唯一出口** —— `+N Attack` 光环的天然落点）·
   `RuleCore.DamageAfterReduction`（护甲唯一公式 —— `Armour 1` 光环的落点）·
   `RuleCore.CostOf` + `CostModApplies`。
3. **「来源离场就收回」最现成的机器是 `UnitState.RecordGrant` / `RevertGrantsFrom`** ——
   现在给「黑暗契约被替换」用，语义正好是「整份收回」。
4. 🔴 **最大的设计选择：关键词型光环没有单一读点** —— 全仓 ~20 处散读 `u.Has(kw)`。
   要么借 `AddKeyword`/`RemoveAll` 做「进入/离开相邻」的**增量维护**，
   要么**新开一个 `AuraKeywords` 读点**。
5. ⚠️ **「谁算相邻」只读 `BoardSpec.AdjacentSlots`**（全仓唯一）。原版
   `BattleManager.GetAdjacentUnits` 按**所属方**取那一方行内左右格 ⇒ **不跨排**，
   光环**只作用于己方相邻格**。
6. ⚠️ **两条待定语义**：`AdjacentSlots` **不含自己**（`Makari` 自己吃不吃 +1 要定）·
   `Armour` 是单值 `u.Armor`，`Armour 2` 的 `Baneblade` 旁边再给 `Armour 1` **叠不叠**要定。

---

## 五、一处需要更新的文档

`_tmp_view/adjacent_report.md` 第 3 行还写着「光环族（**本轮不做**）」——
现在**用户已定「要做」**，那份是自检重写的，**不用手改**；但引用它的文档要跟着更新。
