# `BroadcastHand*` 早退洞 · 普查（`E6ENG` · 2026-10-27 · **只读**，⛔ 本批未改任何 `.cs`）

> 起因：第三批接 `trap + armed ⇒ 250` 时撞到 —— `EffectResolver.BroadcastHandWhen` 开头的「廉价值预扫」
> 只认「**会听的单位卡**」，而**陷阱是 tactic（`CanListenForEvents => IsUnit` 为假）** ⇒ 只要两边手牌里
> 没有躺着一张会听 `draw` 的单位卡，这个函数会在预扫就 `return`，**被抽的武装陷阱永远走不到自己的那一跳**。
> 本普查把这条「按卡型早退」系统查一遍：**改了哪一处、原版会不会同样跳过、今天到底有几族被它挡住**。
>
> ⛔ 白名单：只读 `RuleEngine/Core/**` + `d:/2/tools/decomp_full` + 判据/报告。**没有改代码、没跑 Unity、没动 git/账主表。**

---

## §0 三句话结论

1. **`BroadcastWhen` 的一条广播共有 5 个「手牌侧/非棋盘侧」跳**，其中**只有 `BroadcastHandWhen` 一处的早退是按卡型的**
   （`CanListenForEvents => IsUnit`）；`BroadcastCostWhen` / `BroadcastHandTrapWhen` / `BroadcastPersistentWhen` 都**不按卡型**。
2. **原版的手牌跳一律不按卡型**：`BroadcastCardDrawn` 与 `BroadcastCardPlayed` 的 `GetCardsInHandRef(...)` 循环
   对**列表里每一张** `CardScript` 调接收函数，接收函数的第一道闸只有「有 `rawScript`」/「`cardState != 5`」——
   **没有**任何「是不是单位」的判据。（逐句证据见 §2。）
3. **今天被这条早退挡住、而原版会响的族 = 2 个**：① 武装陷阱被抽自己的 `250`（**第三批已接**、池内 0 张）·
   ② `ArmedTrap == -1`（判不了）时那条「出声」路径（预扫只认 `== 1`，**无单位监听时不出声** · 池内 0 张）。
   **另有一个与本洞相邻、但根因不同的族**：非单位卡的**多段**正文里的 `When …`（`SplitHandTrapWhen` 要求 `segs.Count == 1`）——
   全池只有 **1 张**（`DA42`），而且它那条是「When played」钩子（另一条机制），**不是**这条早退造成的。

---

## §1 我方全部「手牌侧 / 非棋盘侧」跳的早退与预扫（逐条）

`BroadcastWhenCore` 的 ③ 段按固定次序调这五个（`EffectResolver.cs`）：

| # | 函数 | 行（现读） | 早退 / 预扫条件 | 语义 | **按卡型门控？** |
|---|---|---|---|---|---|
| ① | `BroadcastCostWhen` | `:10474` | **无预扫**；逐卡 `c == null \|\| c.CostWhens.Count == 0` ⇒ `continue` | 「事件触发式降费」：登记一条 `CostMod` 到**那一份**手牌实例上 | ❌ 不按卡型（单位 / 战术都能有 `CostWhens`） |
| ② | `BroadcastPersistentWhen` | `:10348` | `ctx.PersistentEffects.Count == 0` ⇒ return；再 `if (!any) return`（`:10364`），`any` = 「表里有 `pe.Trigger=="when" ∧ pe.Ev.Kind==kind ∧ ops 非空 ∧ Matches`」 | 「本场余下，当…时」的**常驻效果**（来源卡可能早已进弃牌堆） | ❌ 不按卡型（筛的是**效果表**，不是手牌） |
| ③ | `BroadcastHandTrapWhen` | `:10545` | **无预扫**；逐卡 `c == null \|\| c.HandTrapWhens.Count == 0` ⇒ `continue` | 手牌**非单位卡**的 `When <事件>, …`（破坏/陷阱族） | ❌ 不按卡型（但**注册端**只收非单位 + **单段**正文，见 §3·F3） |
| ④ | `BroadcastHandWhen` | `:10647` | 🔴 **`if (!any) return;`（`:10664`）**，`any` = 任一方手牌里有 `c0.CanListenForEvents && c0.WhenTriggers.Count > 0`（第三批又 `‖ (drawMode && h[i].ArmedTrap == 1)`） | 手牌**单位卡**的 `When <事件>, …`（`WhenTriggers`） | ✅ **是**（`CanListenForEvents => IsUnit` = `Type=="unit" \|\| "hero"`） |
| ④′ | 同函数**逐卡闸** | `:10686` | `c == null \|\| !c.CanListenForEvents \|\| c.WhenTriggers.Count == 0 \|\| (drawMode && !DrawHandHopTakes(...))` ⇒ `continue` | 同上 | ✅ **是**（同一个洞的另一半） |

**另有两条不是卡型、但同族的闸**（列出来免得下一个人把它当成洞）：

- **`EffectChain` 上限**：`BroadcastWhenCore` 入口 `ctx.EffectChain >= MaxEffectChain` ⇒ 整条不播；逐卡处 `continue`。**与卡型无关**。
- **`boardAndPersistentOnly: true`**：把 ①③④ **三跳整体关掉**（`GainFaith` / `GainQuest` / `EarnedSkull` 三个广播器原版就**没有**手牌跳，判据 `BroadcastCollectFaith.c` / `BroadcastCollectQuestPoints.c` / `BroadcastEarnedSkull.c` 全文 0 处 `GetCardsInHandRef`）。**这是照原版，不是洞**。

**唯一真正的「按卡型」结构**：`CardDef.CanListenForEvents => IsUnit`（`CardDef.cs:6016`），
它同时决定 ④ 的预扫、④′ 的逐卡闸，以及**注册端**「单位卡的 `When …` 进 `WhenTriggers`、非单位卡的进 `HandTrapWhens`」这条分岔（`EffectText.SplitHandTrapWhen:5731` 的第一句就是 `if (… || c.IsUnit) return null;`）。

---

## §2 原版对应路径：会不会因卡型不同而跳过？—— **不会**（判据逐句）

| 广播器 | 手牌跳 | 判据 |
|---|---|---|
| `BroadcastCardDrawn` | `GetCardsInHandRef(IsPlayerTurn())` 两趟，逐张 `CardScript.CardDrawn(...)` | `decomp_full/BattleManagerSupport__BroadcastCardDrawn.c:36-76` · ILSpy `BattleManagerSupport.cs:718-743`（**循环体里只有 `op_Inequality(≠drawnCard)`，**没有**任何卡型/区域判据） |
| `BroadcastCardPlayed` | 三跳：棋盘 `:35` → 当前手牌 `:44-46` → 另一侧手牌 `:49-58` | `decomp_full/BattleManagerSupport__BroadcastCardPlayed.c`（三趟都调 `CardScript__ResolveCardPlayed`，**循环体只有 `!=` 判据**） |
| 接收端 `CardScript.CardDrawn(this, drawnCard)` | 第一道闸 = `rawScript(+0x28) != null` | `decomp_full/CardScript__CardDrawn.c:16-31`（**没有** `cardState` / 区域 / 卡型闸） |
| 接收端 `CardScript.ResolveCardPlayed` | 第一道闸 = `rawScript != null` | `decomp_full/CardScript__ResolveCardPlayed.c:20-24`（**没有**区域闸） |
| 接收端 `CardScript.ResolveUnitSummoned`（对照） | 闸 = `cardState != 5`（= `waitingToDie`）⇒ **`inHand` 放行** | 已在 `BroadcastHandWhen` 的 doc 里引用过（`CardScript__ResolveUnitSummoned.c:21/:36/:55`） |

⇒ **原版的手牌跳是「整手牌、不分卡型」的**：任何一张躺在手里的 `CardScript`（单位 / 战术 / 防御 / 武装陷阱 / 隐藏附魔模板）
都会被叫到，**能不能响只由它自己有没有那条 trigger 决定**。我们按 `IsUnit` 分成两条路（`WhenTriggers` / `HandTrapWhens`）
是**实现分层**，本身不等价于原版 —— 只要有一族非单位卡的监听者落不进 `HandTrapWhens`，它就会**静默失声**。

---

## §3 今天被「按卡型」结构挡掉、但原版会响的族（含可达性）

> 可达性口径：现读 `Resources/cards_engine.json`（池 **1,128** 张）+ `cards_truth.json`（SO **1,351** 张）。
> `type` 分布（池）：`unit 586` · `hero 56` · `tactic 446` · `defence 39` · `enchantment 1`。

### F1 · 武装陷阱被抽到 ⇒ `250 ThisCardDrawnAsTrap`（**第三批已接**）
- **机制**：`CardScript__CardDrawn.c:16-31`，`drawnCard == this ∧ isTrapCard ∧ armedTrap(+0x54)` ⇒ `OnTrapDrawn` → `OnTrigger(0xfa=250)` 并**取代** 260。
- **被哪个早退挡**：④ 的预扫（陷阱是 tactic ⇒ 不进 ④′ 的逐卡闸）→ 第三批已用 `‖ (drawMode && ArmedTrap == 1)` 补掉。
- **可达性**：`spellType == 70`（`SpellType.Trap`）在 SO 1,351 张里 **0 张** · 池内 **0 张**；`abilities[].trigger == 250` **0 行**。⇒ **0 张**。
- **要不要改**：**已改**（第三批）。**没有残余**。

### F2 · `ArmedTrap == -1`（判不了）时那条**出声**路径
- **现状**：第三批的预扫只加 `ArmedTrap == 1` ⇒ 当「两边手牌没有会听的单位卡」且「被抽那张 `ArmedTrap == -1`」时，
  `BroadcastHandWhen` 仍然**早退**，`EmitTrapDrawnInsteadOfCardDrawn` 里那条 `LogWarning`（「判不了 ⇒ 按 260、没按 250」）**不会打出来**。
- **原版对照**：原版没有「判不了」这一态（它读的就是实例上的 `+0x54` 布尔）；**这一态是我们自己引入的三态**（`CardInstance.ArmedTrap: -1`）。
- **可达性**：池内 **0 张**（`cards_engine.json` 1,128/1,128 都有 `cardType` 列，`CardDatabase` 会 `AttachCardType` ⇒ `IsTrapCard` 恒可判）。
- **要不要改**：**要（低优先）**。改法 = 预扫那一格从 `== 1` 放宽成 `!= 0`（即 `1 或 -1` 都进），**只多进一次「可能会打一行 warning」**，行为面为零。
  ⚠️ 但这会让**每一张 `ArmedTrap == -1` 的被抽卡**都进 ④ —— 池内 0 张，所以今天零影响；卡表再旧一档就会变成「每抽一张打一行 warning」（可接受，因为那正是「表是旧的」要喊的）。

### F3 · 非单位卡的**多段**正文里的 `When …`（**与本洞相邻、但根因不同**）
- **根因**：`EffectText.SplitHandTrapWhen:5729-5743` 要求 **`segs.Count == 1`** 且整段以 `When ` 开头；
  单位卡那条路（`CardDef` 的 `whenSegs` 循环 → `AddWhenTrigger`）**允许多段**。⇒ 「非单位卡的多段 `When …`」**两条路都不收**。
- **可达性**：池内**非单位**且 desc 含 `when` 的 **10 张**，剔除已被别的通道接住的：
  - 「`Lower cost by 1 when …`」**4 张**（AM55 · AM56 · TAU57 · UM40）→ 走 `BroadcastCostWhen`（`CostWhens`）✔
  - 「`For the rest of this battle, when …`」**4 张**（BL75 · DA55 · SW46 · SAU53）→ 走 `BroadcastPersistentWhen`（`PersistentEffects`）✔
  - 「以 `At the start/end of your turn` 开头」**2 张**（`7c666a04…` · `a7bb2335…`）→ 走 `ResolveAtTurn` 的手牌扫描 ✔
  - 「**单段** `When …`」**1 张**（`68909cf02f472734ea391665e0d28e15` = 「When you play a Stratagem, your Warlord takes 1 damage」）→ **已收**进 `HandTrapWhens` ⇒ 走 ③ ✔（**这张是正对照：机制本身没坏**）
  - 剩下的 **1 张** = **`DA42 Reconnaissance Mission`**「Choose a card in your opponent's hand. **When played**, gain 3 Quest Points」
    ⇒ 它是「给对手手里那张牌**挂号**：那张被打出时我 +3 任务点」，是一条 **in-hand 挂载 + 打出钩子**，**不是**「我在手里监听事件」。
- **要不要改**：**先别改 `SplitHandTrapWhen`**。放宽 `segs.Count == 1` 会让「非单位卡正文里任何一句 `When …`」都注册成监听者，
  与 ② / ① / `ResolveAtTurn` **可能重复触发**（那是**行为变化**，且今天只有 1 张候选、还是另一条机制）⇒ 建议**另立一条账**：
  「`DA42` 型 in-hand 挂载 + when-played 钩子」单独立，判据按 `inHandFiltered(50)` / `PlayerHand.AddHandEffect` 那条链查。
  ⛔ 不要用「放宽 `SplitHandTrapWhen`」当它的实现。

### F4 · 非单位卡的**结构化** listeners（除 250）—— **本轮没逐族判，如实记**
- **已知**：非单位卡（`cardType == 20`）的 `abilities[].trigger` 里**大量是事件 trigger**（现扫 SO 1,351 张）：
  `90 OtherUnitDeath 17 行` · `190 OtherUnitSummoned 9` · `640 EarnedSkull 17` · `605 UseFaith 15` · `600 UseSpiritStone 13` ·
  `410 FollowUpResolution 19` · `145 Codex 6` · `120 Slay 6` · `60 UnitDamaged 1` · `31/30 TurnStart…` · `240 WhileInPlayVariable 4` …
- **但「这些是不是手牌态触发的」本轮没查清**：其中至少三族已经被**别的容器**接住（`For the rest of this battle…` → `PersistentEffects`；祈祷附魔 → `Enchantments`；`costless` 光环 → `AuraSpecs`）。
  ⇒ **这一格记「待查」**，不猜。**判据入口**（给下一个人）：
  `PlayerHand__AddHandEffect.c` / `PlayerHand__SetupCardInHand.c`（in-hand 挂载）· `AbilityLogic__PlayAbility.c:1193-1197`（`extrinsic` 才是「在场上才成立」）
  · 卡面 `targetCriteria` 的 `targetsAffected`（`20 self` / `100 adjacent` … ⇒ 决定这条 ability 到底挂在哪）。
- **要不要改**：**先查清再谈**。⛔ 不要因为「非单位卡有很多事件 trigger」就整体放开 ④′ 的 `CanListenForEvents` —— 那会把
  **所有非单位卡**塞进单位手牌那条路（`FireWhen` 用 `WhenTriggers`，而非单位卡的 `WhenTriggers` 是**空的**），等于白改。

### F5 · `defence`(39) / `enchantment`(1)
- 这两类在我们的 `type` 里**都不是单位** ⇒ 走 ③（`HandTrapWhens`）。现扫：**没有任何一张** desc 以单段 `When ` 开头 ⇒ **0 张可达**。
- **要不要改**：不用（机制已在，数据为 0）。

### F6 · **武器卡 / 低语卡**（调度台点名的两族）
- **我们的数据里没有这两种 `type`**：池内 `type` 只有 `unit / hero / tactic / defence / enchantment`（1,128 张），
  `cardType` 只有 `0 Minion / 10 Hero / 20 Tactic` —— **没有 30 / 40**（`CardDef.CardTypeWhispers = 40` 这个常量存在，但**0 张卡**）。
- ⇒ **查不到（不是「没找到」）**：`cards_engine.json` 的 `type`/`cardType` 两列全量分布 + `cards_truth.json` 的 `spellType` 分布都扫过，**没有 Weapon / Whispers 卡型**。
- **要不要改**：**不改**。将来若出现 `type == "weapon"/"whisper"`，它们会落进「非单位」那一侧 ⇒ 自动继承 ③（单段 `When` 收得下）+ F3 的限制。

---

## §4 汇总：改不改、怎么改（建议顺序）

| 族 | 今天可达 | 建议 |
|---|---|---|
| **F1** 武装陷阱 `250` | 0 | ✅ 第三批已改（预扫 `‖ ArmedTrap == 1` + `hop 0` 分叉） |
| **F2** `ArmedTrap == -1` 的出声路径 | 0 | 🟡 **建议改**：预扫那一格 `== 1` → `!= 0`（零行为、只让 warning 可达）。一行。 |
| **F3** 非单位「多段 `When`」 | 1（DA42，另一条机制） | 🔴 **另立一条账**（in-hand 挂载 + when-played）；⛔ **不要**放宽 `SplitHandTrapWhen` |
| **F4** 非单位结构化 listeners | **待查** | 🔵 先按 §3·F4 给的入口把「哪些是手牌态」查清，再决定要不要 ④′ |
| **F5** defence / enchantment | 0 | 不改 |
| **F6** weapon / whisper | 数据里不存在 | 不改 |

**一句话给调度台**：这条早退洞**今天只挡了第三批那一个（已补）**，其余各族的「静默」根因各不相同（注册端单段限制 / 另一条机制 / 数据不存在）——
**别把 ⑥ 个族打成一包改**，`CanListenForEvents` 那一处一旦整体放开，会把非单位卡塞进一条 `WhenTriggers` 永远是空的路。

---

## §5 查不到 / 搜过哪里（诚实清单）

1. **非单位卡的「结构化 trigger 是不是手牌态」** —— 未查清（§3·F4）。搜过：`cards_truth.json` 全量 `abilities[].trigger` 分布（按 `cardType==20` 分组）· `EffectResolver` 里所有 `boardAndPersistentOnly` 调用点。**没有**逐条回原版读 `AbilityLogic.PlayAbility` 的 `targetsAffected`。
2. **`DA42` 的实现路** —— 未查（不在本批白名单，且它属于另一条机制）。搜过：`账/战斗逻辑驱动.tsv` 里 `DA42` **0 命中** ⇒ **它没有任何账**（这本身值得立一条）。
3. **`weapon` / `whisper` 卡型** —— 按数据判「不存在」（§3·F6），**不是**「没找到」：扫了池 1,128 张的 `type`+`cardType` 全分布、SO 1,351 张的 `spellType` 全分布。
4. **`ArmedTrap == -1` 时原版会怎样** —— **原版没有这一态**（它读实例上的 `+0x54` 布尔）；三态是我们自己引入的（`CardInstance.ArmedTrap` 的 doc 已写明「`-1` = 判不了」）。

---

## §6 第二轮补查（2026-10-27 · 第五批 · `E6ENG`）

> 调度台裁决：F1 已改 · **F2 改** · **F3 立账（只写判据）** · **F4 查清**。本节就是那三件的结论。

### F2 ✅ 已改（1 处条件 + 1 个日志闩）

- **预扫**：`BroadcastHandWhen` 的 `any` 里 `h[i].ArmedTrap == 1` → **`!= 0`**（`EffectResolver.cs` 现读数 `:10661`）⇒ `ArmedTrap == -1`（判不了）的被抽卡也能进 `hop 0`，那条 `LogWarning` 才**可达**。
- **日志闩**：`EmitTrapDrawnInsteadOfCardDrawn` 的三条告警改成走 `NoteTrapDrawnOnce(card, reason, msg)`（新方法 + `HashSet<string> _trapDrawnNoted`，**追加在类尾**）⇒ **同一张卡的同一类告警只喊一次**（形状照 `NoteAbilityWithoutAnimRow`；⛔ 只闩日志、不闩事件 —— 账 703 的验收读数就是数 250 事件条数）。
- **零可观察**：池内 `ArmedTrap != 0` = **0 张** ⇒ 本改只影响日志。两态指纹实测见报告「第五批」表（本笔**不是** `79057e33…`→`0766fbe1…` 那个跳变的成因，隔离实验已证）。

### F3 · `DA42 Reconnaissance Mission` —— 判据（⛔ 本批**没有写代码**）

**① 原版怎么做（🥇 数据现读，`cards_truth.json` + `cards_engine.json` 两侧一致）**

两张 SO 配合，**没有一条是「手牌监听事件」**：

| 角色 | 对象 | 结构 |
|---|---|---|
| 施放卡 | **`DA42` `Reconnaissance Mission`**（`cardType 20` tactic · `spellType 0` · `targeted 0` · `maxTargets 1`） | `abilities[0]`：`trigger **0** (ThisCardPlayed)` · `logic[0]` = `chosenAbility **30**` · `targetCriteria { targetsAffected **50**, enemyUnits 1, includeMinions 1, includeSpells 1, maxTargets 1, selectFromChooseResolution 1 }` · `buff { buffType **15 singleUseEnchantment**, enchantment → PPtr(-1426406806713108198) }` |
| 附魔模板 | **`e8cf1eb55ca41534abe38efc90b302e9`**（同名 `Reconnaissance Mission`，`cardType 20`，`spellType 0`） | `abilities[0].logic` 两行：① `chosenAbility **60** (buffMana)` · `playersAffected 20 (enemyPlayer)` · `manaBuff { manaAmount 3, enemyPlayer 1, **questPoints 1** }`；② `chosenAbility **340** (removeThisEnchantment)` · `targetCriteria { targetsAffected 20 }` —— **两行都挂在 `trigger 0`** |

⇒ 机制（逐跳）：**打出 DA42 → 给「敌方手牌里挑中的那一张」挂一条 `singleUseEnchantment`；那张牌**被打出**时，附魔的 `ThisCardPlayed` 能力结算 = `buffMana{questPoints:1}`（`enemyPlayer` 相对**承载卡**那一方 = **我方**）→ 我方 +3 任务点 → 随后 `340` 把附魔自己摘掉。**
（`targetsAffected 50` 的枚举名 = `inHandFiltered`；本轮沿用的是 `判据/T7_引擎三条_判据.md` ① 里同一个枚举读法，**没有**重新反查 `TargetsAffected` 枚举表 —— 如实标。）

**② 我们要什么（三个前提，今天一个都不满足）**

1. **附魔能挂到「手牌实例」上**：`CardInstance.ActiveEnchantments` 是个 `List<ActiveEnchantment>`（`CardInstance.cs:142`），**容器层面装得下**；但 `Enchantment.cs` 里**没有任何一条把手牌实例当载体**的路（`grep Hand` 只有注释），且挂载入口都从场上单位来 ⇒ 今天挂不上去。
2. **附魔要有一条 `trigger 0 (ThisCardPlayed)` / `500 (TriggerEnchantment)` 的结算口** —— **这是真缺口**，正本账 **行 1287 `RawCardScript.TriggerEnchantment`（状态 C / P2）**：原版那方法是**两次** `TriggerAbilities(0 / 500)`，我们**两跳都没有**。
3. **`buffMana{questPoints, enemyPlayer}` 的极性映射**：附魔属主 = 承载卡那一方（敌方），`enemyPlayer` ⇒ **原施放者**。我们有 `ProcessManaBuff` 的等价物（`DoFactionResource` 任务点支），缺的只是「属主」这一格怎么传。

**③ 施工建议（顺序，⛔ 别一步到位）**

1. **先补 1287**（`TriggerEnchantment` 的两次派发），**在场上载体**上先跑通 —— 判据齐（`.c:1-22`）、影响面 = 池内所有带附魔的单位（已有账）。
2. 再补「附魔挂手牌实例」那条**挂载路**（`chosenAbility 30` + `targetsAffected 50`）：⚠️ 这一步会动 `DoGive` 的目标池（手牌目标），**必须全池对拍**。
3. 最后接 DA42 本身：`selectedFromChooseResolution 1` ⇒ 需要「从对手手牌里选一张」的**选择 UI**（我们今天有没有？`DoGive` 的手牌目标族是 `HandReferents`（自己手牌），**敌方手牌选择**要单独核）。
4. **验收**：夹具 —— 手牌放 DA42，场上有对手手牌 3 张 ⇒ 打出 DA42、选一张 ⇒ 那张牌带上 `ActiveEnchantment`；对手打出那张 ⇒ **我方 +3 任务点**、那张牌的附魔**消失**。

**④ 可达性**：**1 张**（`DA42`）。⚠️ 与行 1287 原备注「池内 0 张挂附魔卡」**冲突** —— 那句在**数据侧**已不成立（DA42 就是一条挂载路）；今天之所以仍看不到效果，是因为**我们挂不上去**（前提 1）+ **没有 `trigger 0` 结算口**（前提 2），**不是**「数据里没有」。

### F4 ✅ 查清 —— **没有被接住的族 = 0**

**扫描口径**：`cards_engine.json` 池 1,128 张 → 取 `type ∉ {unit, hero}`（**486 张**）→ 取 `abilityTriggers` 里含**任一事件 trigger** 的（75 张）→ 按 **desc 形态**分桶：

| 桶 | 张数 | 归谁 |
|---|---|---|
| `For the rest of this battle, …` | 0（这批里） | `BroadcastPersistentWhen`（`PersistentEffects`） |
| `At the start/end of your turn` | **2** | `ResolveAtTurn` 的手牌扫描 |
| `Lower cost by 1 when …` | **4** | `BroadcastCostWhen`（`CostWhens`） |
| 单段 `When …`（手牌陷阱） | 0（这批里） | `BroadcastHandTrapWhen`（`HandTrapWhens`） |
| **其它（逐张核过）** | **69** | 见下 |

**那 69 张逐张过了一遍，全部落在既有通道里，没有一张是「手牌态 listener」**：

| 它们的 trigger | 实际是什么 | 我们的通道 |
|---|---|---|
| `600 UseSpiritStone`（13 张） | 战术卡的「`1 [Spirit Stone]: …` **额外费用**那一句」 | 额外费用/正文解析（`EffectText` 的 `☀`/`[Spirit Stone]` 段） |
| `605 UseFaith`（15 张） | 同上，`4 ☀:` 那一族 | 同上 |
| `145 Codex`（6 张） | 「`Codex:` …」= 卡自己的**打出后追加** | `EffectText` 的 Codex 段 |
| `410 FollowUpResolution`（19 张） | 「**If … dies / If it's a Vehicle, …**」= 同一次结算里的**后续** | `EmitFollowUpResolution` |
| `90 OtherUnitDeath`（17 张） | 「`If the target dies, …`」= 自己那一发的**死亡观察**，**不是**在手里看别人的死亡事件 | 条件 op（`if … dies`） |
| `240 WhileInPlayVariable`（4 张：`ASH85` / `BL52` / `SW58` / 1 张 hash） | 「`This costs 1 less if you control …`」= **手牌降价** | 费用修正（`CostMod` / `CostWhens` 族） |
| `500 TriggerEnchantment`（4 张 Dark Pact 模板）+ `31/60`（`Pray Enchantment`）+ `280` | **附魔模板 SO**（不是手牌监听） | `Enchantments`（部分）/ 见 F3 |
| `300 Mob` / `580 Strike` / `640 EarnedSkull` / `726 TriggeredFerocity` / `130 Rage` / `120 Slay` / `15 PlayedThirdCardInTurn` 各 1–3 张 | 同上：**卡自己那一发的关键词/追加**（`Strike:` / `Slay:` 写在战术卡正文里） | 关键词触发族（`triggers:<kw>`）/ `FollowUp` |

⇒ **结论：`cardType==20` 的结构化 trigger 里，没有任何一族是「躺在手里听事件」的** —— 全池唯一那一种是 **`250 ThisCardDrawnAsTrap`**（**0 张**，第三批已接）。
⚠️ **口径如实标**：本结论是**按 trigger 值 + desc 形态**分类 + **抽样 6 张逐字读**（`AM39` / `UM64` / `ASH73` / `708c0306` / `ASH84` / `SOR53`）得出的，**没有**逐张回原版读 `abilityLogic[].targetCriteria.targetsAffected`；若将来出现「战术卡 + 事件 trigger + `targetsAffected` 指向手牌」的组合，需重扫（脚本口径已写在本节）。
