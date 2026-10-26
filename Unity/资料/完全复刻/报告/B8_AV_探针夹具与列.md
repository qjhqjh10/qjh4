# B8_AV · `ruleprobe` 补四件读数能力（多路代理共同卡住的「量具盲区」）

> 执行代理 **B8_AV**（完全复刻批次 8）· 2026-10-26 · 白名单：`工具/ruleprobe/**` · `工具/ruleprobe.sh` · `账/_parts/B8_AV.tsv` · 本文件
> 门禁：`ruleprobe check` **rc=1（既有红，与本人改动无关：解析差异 1 行 = `f826…` Ethereal Supreme，`B8_AE` 有意改的那一张；输出指纹早就与钉住的不同）** · `play check` **rc=1（既有红：结算指纹 `e11aa1c2…` ≠ 钉住 `a93ffb8f…`，由本会话其他人的引擎改动造成）**
> 账补丁：**本次不写**（理由见 §③ —— 每条 B 行除了「没读数口」之外都还有别的开口项，⛔ 不替调度台判 A）

---

## ① 四件各做到什么

| # | 需求 | 结论 | 改前 → 改后（真实输出见 §②） |
|---|---|---|---|
| 1 | `LoadPool` 没传 `targeted` ⇒ `play` 结构上看不到卡级 `cardtarget` | ✅ **修了【两处】**（不是一行）：`Program.LoadPool` 透传 `targeted`（缺列 `-1` 并出声）＋ `PlayProbe` 自己算 ops 那两处 `EffectText.Parse` **补上 `card:`**；另把 `Blank()` 的替身契约补上 `targeted` | `play 507f79fa…` 的 `[目标条件]` 由 **`null`** → **`cond=276 ex=False role=cardtarget`**；⚠️ **`check` 输出指纹不变**、**`play all dump` / `pf` 逐字节不变**（原因见 §④·1） |
| 2 | 「手牌态监听卡 ＋ 触发事件」夹具 | ✅ 新增 `preset hand=<卡id\|卡名\|self>[,handn=N][,event=<5 个广播之一>][,amount=N][,legacy=1]` | `play SOR38 preset hand=self,event=gainfaith`：**1 条**变化（只信仰 12→13）；加 `,legacy=1`（改前形状）：**3 条**，多出 **敌督军 30→26** ＋ 伤害累计 0→4 —— 正是 `B8_AE` 说的那条「改前会 -4」 |
| 3 | 棋盘/单位 `PlayOrder` 列 | ✅ `play all dump` **末尾新增 `playOrder` 列**（按号升序 ⇒ 字面次序 = 谁先响）＋ `play <id>` 打一行 `进场序(PlayOrder)` | `play BL51`：`P0[4]=0 P1[4]=1 P0[3]=2 P0[5]=3 P0[6]=4 P1[3]=5 P1[5]=6 P1[6]=7` |
| 4 | `ai actions <卡id>` | ✅ 新增子命令：`SimpleAI.EnumerateActions` 逐条动作 ＋ `ScoreAction` ＋ `SelectBestAction` ＋ `NextAction(Normal)` | 见 §②·4（`playCard` 12 · `attackMelee` 6 · `attackRanged` 6 · `endTurn` 1） |

### ①·1 新增/改动的读数口（一行一个，方便别人直接用）

```bash
# 2) 手牌态监听卡 + 事件（产品形状 vs 改前形状）
bash d:/4/Unity/工具/ruleprobe.sh play SOR38 preset hand=self,event=gainfaith
bash d:/4/Unity/工具/ruleprobe.sh play SOR38 preset hand=self,event=gainfaith,legacy=1

# 3) PlayOrder（单卡档一行；全池档是 dump 的最后一列）
bash d:/4/Unity/工具/ruleprobe.sh play BL51
bash d:/4/Unity/工具/ruleprobe.sh play all dump <out.tsv>     # 末列 = playOrder

# 4) 动作表
bash d:/4/Unity/工具/ruleprobe.sh ai actions "Path of the Warrior"
```

---

## ② 逐件改前 / 改后真实输出

### ②·1 任务 1 — `targeted` 透传

**改前**（`B8_AH` 报告 §五 第 2 条已记录同一病灶；本人复现路径一致）：

```
[目标条件] op[0] `give` TargetCondition = null · gap=``
```

**改后**：

```
$ bash d:/4/Unity/工具/ruleprobe.sh play 507f79fa60945d04e93fe806aca912b7 preset legal=1
  怎么打的    : `RuleCore.PlayTactic`（目标侧=`own` 格=5）
     ✎ 改动 P0[5] FIX_B 3/0/0/3 → FIX_B 3/5/0/3
     [合法目标池] `RuleCore.IsLegalPlayTarget` ⇒ **1** 格：格5 FIX_B 3/0/0/3
     [目标条件] op[0] `give` TargetCondition = cond=276 ex=False role=`cardtarget` · gap=``
```

为什么是**两处**而不是一行（`A1911` 同族第 5/6 例）：

| 处 | 文件:行 | 不修的后果 |
|---|---|---|
| ① `CardDef.Targeted` 恒 `-1` | `工具/ruleprobe/Program.cs` `LoadPool`（`initiative:` 那一行之后） | `EffectText.AssignCardTargetFallback` 的闸 `card.Targeted != 1` 恒成立 ⇒ **卡级条件永远不挂** |
| ② 探针自己算 ops 时不传 `card:` | `工具/ruleprobe/PlayProbe.cs` `MeasurePlay`（原为 `EffectText.Parse(card.Desc, out _, out _)`） | 只修 ① 仍然瞎：探针拿来打 `[目标条件]` 与喂 `PickSlot` 的那份 ops **不是**带 `card:` 解析出来的 ⇒ 引擎侧每条结算路径都是 `Parse(…, card: card)`（`EffectResolver.cs:1947`/`:2041`/`:2434`/`:5098`） |

另有第三处（**替身契约**）：`PlayProbe.Blank()` 补 `targeted: c.Targeted` —— 它的契约是「除 `desc`/`descZh` 外逐格相同」（`A1942`），源卡有真值而替身落 `-1` 会让两道闸在两局里不一致。

**读数影响（如实）**：
- `ruleprobe check` 的**输出指纹不变**：`18439d965d14288fe28cfbe5986dc445`（改前改后同一个）—— `check` 那条 dump 早就在用 `card:`（`Program.cs:1486`），所以它本来就看得见卡级条件。
- `play all dump` / `play all pf` **逐字节不变**（0 行不同）：现有夹具给战术卡特指一个目标格（`PickSlot` 只取「第一个非督军格」），**根本不问 `TargetCondition`** ⇒ 这一笔在既有读数上**零可观测**，它解锁的是 `legal=1` 那一档（`[目标条件]` 行）。

### ②·2 任务 2 — 手牌态监听卡（**这是本批唯一「改前/改后读数不同」的一条**）

```
$ bash d:/4/Unity/工具/ruleprobe.sh play SOR38 preset hand=self,event=gainfaith
=== play SOR38 · Paragon Warsuit [unit] ‖ 手牌态监听夹具（`B8_AV`）===
  卡面（英文）: When you gain Faith, deal 4 damage to the enemy Warlord.
  放了几张    : 1 张（都走 `NewInstance` → `Hand.Add` → `SetupCardInHand`，**手牌态**）
  事件        : gainfaith × 1  （产品形状）
  走的哪条路  : **产品路径**：`RuleCore.ResolveOps` → `DoFactionResource`（广播由产品自己发）

  ── 事件之后，那张卡还在手牌里吗 ──
     在（手牌下标 2 · 手牌增益条数 0）
  ── 变化逐条（1 条）──
     🔮 信仰 P0 12 → 13
```

```
$ bash d:/4/Unity/工具/ruleprobe.sh play SOR38 preset hand=self,event=gainfaith,legacy=1
  事件        : gainfaith × 1  ⚠️ **legacy=1 —— 改前形状**（广播四跳，会把手里那些监听者叫醒）
  走的哪条路  : ⚠️ **改前形状复现**：先加资源，再 `BroadcastWhen(…)` **不带** `boardAndPersistentOnly`（= 四跳：事件型降费 / 手牌陷阱 / **手牌单位**）

  ── 变化逐条（3 条）──
     ✎ 改动 P1[4] WARLORD 2/0/0/30 → WARLORD 2/0/0/26
     🔮 信仰 P0 12 → 13
     ☠ 伤害累计 P0 0 → 4
```

⇒ **敌督军 30→26 = 那 4 点伤害**，正是 `B8_AE` §②·1 说的 `SOR38` 那条 bug。两次读数**不同** ⇒ 本夹具**有鉴别力**；产品当前**没有**多跳（0 条）。

**5 个广播全跑了一遍**（每个监听卡都取自 `B8_AE` 报告里那张「可达监听卡」名单）：

| 事件 | 监听卡（卡面） | 产品形状 | `legacy=1`（改前形状） |
|---|---|---|---|
| `gainfaith` | `SOR38 Paragon Warsuit`（`When you gain Faith, deal 4 damage to the enemy Warlord`） | **1 条**：`信仰 12→13` | **3 条**：`敌督军 30→26` ＋ `伤害累计 0→4` ＋ `信仰` |
| `gainquest` | `DA5 Apothecary`（`… it heals 2`） | **1 条**：`任务点 0→1` | **2 条**：`我方督军 28→30`（`it heals 2`）＋ `任务点` |
| `gainspirit` | `ASH80`（手牌增益那一族） | **1 条**：`灵魂石 5→6` | **3 条**：`手牌 P0 -「ASH80」×1` · `手牌 P0 +「ASH80*1」×1`（被挂了一条手牌增益）＋ `灵魂石` |
| `createssabotage` | `GSC68 Atalan Jackal`（`deal 2 damage to a random enemy`） | **0 条** | **2 条**：`FIX_E2 6→4` ＋ `伤害累计 0→2`；引擎日志自己写出「`Atalan Jackal` 在 P1 手里监听 → 友方 createsabotage → deal 2 damage」（⚠️ 日志的是 **1-based** 玩家号 = 我们的 `Players[0]`） |
| `createssecret` | `DA15 Ravenwing Champion`（`deal 3 damage to a random enemy`） | **0 条** | **2 条**：`FIX_E2 6→3` ＋ `伤害累计 0→3` |

⇒ **三个资源族走的是真产品路径**（`RuleCore.ResolveOps` → `DoFactionResource`），**两个造牌族是直调广播**（标在读数里）。
⚠️ 三个资源族里「产品形状 = 只有资源 +1」这一格，就是「手牌里的监听者**没**被叫醒」的正面证据。

设计上的三条取舍（都要看得见）：

| 取舍 | 为什么 |
|---|---|
| `event=gainfaith/gainquest/gainspirit` **默认走真产品路径** `RuleCore.ResolveOps`（构造一条 `EffectOp{Verb=…}`） | 这样「资源真的加、信号真的发、广播由产品自己按当前形状发」——⛔ 不是探针替产品发广播 |
| `legacy=1` 才直调 `BroadcastWhen(…)`（**不带** `boardAndPersistentOnly`） | 它**只服务复现改前形状**（2026-10-26 那 9 个发射点加 flag 之前的行为）。产品代码里⛔ 别照抄 |
| `createssecret` / `createssabotage` **只能直调广播**（`boardAndPersistentOnly` 跟 `legacy` 走），读数里**明说这是直调** | 产品那两处发射点埋在 `DoCreate` / `CreateCard` 的造牌流程里（`EffectResolver.cs:4020`/`4030`/`4848`/`12993`），要真造出一张牌才走得到；`DoCreate` 的候选域是卡池/牌库，本夹具不造牌 ⇒ `subject=null`（如实标注） |

### ②·3 任务 3 — `PlayOrder`

```
$ bash d:/4/Unity/工具/ruleprobe.sh play BL51
  ── 打出去之后 ──
     P0 棋盘 [.][.][.][FIX_A 2/0/0/2][WARLORD 2/0/0/28][FIX_B 3/0/0/3][FIX_C 1/0/0/2][.][.]
  进场序(PlayOrder): P0[4]=0 P1[4]=1 P0[3]=2 P0[5]=3 P0[6]=4 P1[3]=5 P1[5]=6 P1[6]=7
     ⚠️ 按号升序；`-1` = 从没取过号（排末尾）。督军也在里面（`RuleCore.BuildPlayer` 给它发了第一个号），⛔ 别拿它跟 `UnitsByPlayOrder()` 比张数。
```

dump 的新列（`play all dump` 文件头最后一列 = `playOrder`）：

```
id	name	type	…	fixtureFit	injected	playOrder
BL51	Hosts of Chaos	tactic	…		0	P0[4]=0 P1[4]=1 P0[3]=2 P0[5]=3 P0[6]=4 P1[3]=5 P1[5]=6 P1[6]=7
```

口径（三条，⛔ 别改）：

1. **按 `PlayOrder` 升序**，`-1` 排末尾 —— 与 `PlayerState.UnitsByPlayOrder()` / `RuleCore.StableSortByPlayOrder` **同口径** ⇒ **列里字面从左到右就是原版 `allCardsInPlay` 的遍历序（谁先响）**。
2. **督军也在列里**（`RuleCore.cs:649` 给它发第一个号），而 `UnitsByPlayOrder()` **刻意不含督军**（它仿原版 `GetMinionList`）⇒ ⛔ 别拿这一列跟 `.Count` 比张数。
3. 它**不进** `Fingerprint` / `FingerprintRow` ⇒ 加这一列**不需要**重钉 `PinnedPlayMd5`（实测 pf 逐字节不变，见 §④·3）。

### ②·4 任务 4 — `ai actions`

```
$ bash d:/4/Unity/工具/ruleprobe.sh ai actions "Path of the Warrior"
=== ai actions 1bfe0893592c9dd40bed8d68d5d2b516 "Path of the Warrior" · 动作表（原版 `BattleManager.GetAvailableActions` 的等价物）===
[夹具] ctx.Active=0（本模式量的是**行动方**的动作表）· 被测卡**在手牌里** · 我方棋盘上有 1 张探针单位（`Exhausted=false`，为了量到攻击行）· 敌方 2 张（无甲 / 2 甲） · 能量 20/20
[动作表] 共 **25** 条（`EndTurn` 恒为最后一条、分数恒 0）
  [  0] playCard(0)     hand#= 0 slot= 5 →P-@ - ranged=0 mana=1 score=3  card=AVFILL
  [  3] playCard(0)     hand#= 3 slot= 0 →P-@ - ranged=0 mana=2 score=0  card=1bfe0893592c9dd40bed8d68d5d2b516 ← **被测卡**
  …
  [ 12] attackMelee(1)  hand#= - slot= 3 →P1@ 3 ranged=0 mana=0 score=1.075
  [ 13] attackRanged(2) hand#= - slot= 3 →P1@ 3 ranged=1 mana=0 score=0
  [ 14] attackMelee(1)  hand#= - slot= 3 →P1@ 4 ranged=0 mana=0 score=3.1
  …
  [ 24] endTurn(4)      hand#= - slot= - →P-@ - ranged=0 mana=0 score=0
[按 Kind 计数] playCard(0) 12 · attackMelee(1) 6 · attackRanged(2) 6 · endTurn(4) 1
[最高分] [14] attackMelee(1) = 3.1
[SelectBestAction] attackMelee(1) @3 →P1@3 = 1.075
[NextAction(Normal)] 有一条  ⇒ attackMelee(1) @3 →P1@3 = 1.075
   ⚠️ 它带 `TweakAvailableActions` 的难度旋钮（`Normal`：`minSkips/maxSkips/skipChance`）⇒ 与上面 `SelectBestAction`（**不带旋钮**）不同是**预期的**，⛔ 不是缺陷。
```

⚠️ **一条必须记住的实现细节**（我踩了又改）：`EnumerateActions` **不填 `a.Score`**，而 `SelectBestAction` 读的是 `a.Score`。第一版我没写回分数 ⇒ 它看到一堆 0、按「先出现的赢」挑了回去（最高分明明是 3.1 的攻击，它却挑了 1.075 那条）。产品那条路（`NextAction`）里有一句 `foreach (var a in list) a.Score = ScoreAction(ctx, a);` ⇒ 探针必须照做，否则 `SelectBestAction` 的读数**不可信**。

`[SelectBestAction] ≠ [最高分]` 是**引擎自己的行为**（不是 bug）：`SelectBestAction` 从 `endTurn` 起手、**严格大于才换**；而 `ScoreAction` 对每条单独算。两者一致与否要逐条看 `Score`，⛔ 别拿「最高分」当「AI 会选它」。

---

## ③ 哪些 B 行因此可升 A

**结论：一条都没升，所以没有补丁**（`账/_parts/B8_AV.tsv` 未创建）。原因逐行写清（⛔ 不替调度台判 A，也不拿「读数口有了」当成「判据已闭合」）。

### ③·1 我解锁掉的「卡点」（这些行的备注里点名的就是本批这几件）

| 行 | 原版类·方法 | 该行原来的卡点原文 | 本批给了什么 |
|---|---|---|---|
| `战斗逻辑驱动.tsv:357` | `BattleManager.GetAvailableActions` | 「G3✗ —— **结构性**：AI 动作表没有读数口…⇒ 需调度台给 `ruleprobe` 加 `ai actions`」 | ✅ `ai actions <卡id>` |
| `战斗逻辑驱动.tsv:632` | `BattleManager.SetUnitOrderInPlay` | 「`PlayOrder` 不在可观察量清单里，且 `play`/`ai` 两个探针都不打印它 ⇒ 需调度台加列」 | ✅ `playOrder` 列 + 单卡一行 |
| `战斗逻辑驱动.tsv:1258` | `MinionManager.GetMinionList` | 「还差什么：…② `ruleprobe` 给棋盘/单位加一列 `PlayOrder`」 | ✅ 同上 |
| `战斗逻辑驱动.tsv:217/218/219` | `BattleManager.BroadcastCollected{Faith,QuestPoints,SpiritStone}` | 「观测量 = `play all dump` 改前/改后逐字节相同 ⇒ 现有夹具测不到；需要新夹具：把监听卡留在手牌再 +1」 | ✅ `preset hand=…,event=…` |
| `战斗逻辑驱动.tsv:709/710/711` | `BattleManagerSupport.BroadcastCollect{Faith,QuestPoints,SpiritStone}` | 同上（+ 明写「可达 665 = 1 张 `SOR38`」） | ✅ 已用 `SOR38` 跑出改前/改后两读数 |
| `战斗逻辑驱动.tsv:225/227/716/717` | `…{Sabotage,Secret}Created`（`BattleManager` + `Support` 共 4 行） | 「需要『监听卡留在手牌 + 造隐秘/破坏』的夹具」 | ⚠️ **部分**：夹具给了，但 `createssecret`/`createssabotage` 那一档是**直调广播**（`subject=null`），⛔ 还不能当「造牌链已验」 |

### ③·2 为什么仍不算 A（每行至少还差一项，逐行点名）

| 行 | 还差什么（原文就在该行里，我没有新证据能销） |
|---|---|
| 357 `GetAvailableActions` | 「原版另外用 `+0x244/+0x246` 两个『本回合手牌闸』字节 …**那两位的写点本轮没核**」 ⇒ 判据侧还有开口 |
| 632 `SetUnitOrderInPlay` | 「**没核**：`+0x3a8` 五 dword 里哪几个是 fakeValue/inited」 ⇒ 判据侧还有开口 |
| 1258 `GetMinionList` | 「① keySelector == orderInPlay 的独立证据（`out_cpp2il_dev_ilspy/MinionManager.cs` 或 VA）」 ⇒ 只交了 ②，① 仍空 |
| 217/218/219（`BattleManager` 三条） | 该行「差异」栏仍写着**改前**的状态（「我们的 `BroadcastWhen` 走通用四跳 ⇒ 我们会叫醒、原版不会」）—— 但 `B8_AE` 当天已经改掉、本人实测也是 0 伤害 ⇒ **这三行是过期文案，不是判据缺口**；升 A 前要先让调度台把「差异」栏改成已收窄（见 §⑤·1） |
| 709/710/711/225/227/716/717 | 这些行的「差异」已由 `B8_AE` 更新为「✅ 已收窄」，但各自都留着**结构性残余**（原版 `:22` 对收集者预调一次而我们不排除 subject / 原版 `Signal.Raise(CreateSecretSignal)` 我们无对应物 / 隐秘-破坏那一档没有 `subject`）⇒ 不是「全部对齐」 |

**我可以具名给的两侧数值**（供调度台重判时直接用）：

| 读数 | 原版侧（判据） | 我们侧（实测） | legacy（改前形状） |
|---|---|---|---|
| `gainfaith` × Paragon Warsuit 躺手里 | 敌督军**不动**（`BroadcastCollectFaith.c` 全文 `GetCardsInHandRef` = **0**） | 敌督军 **30 → 30**（变化只有 `信仰 12→13`） | 敌督军 **30 → 26** + `伤害累计 0→4` |
| `PlayOrder`（夹具 BL51 那一局） | 原版是运行时 `orderInPlay(+0x44)` 计数器，**离线包取不到数值** | `P0[4]=0 P1[4]=1 P0[3]=2 P0[5]=3 P0[6]=4 P1[3]=5 P1[5]=6 P1[6]=7` | — |
| `cardtarget`（`507f79fa Deploy Anchors`） | 原版 `RawCardScript__IsValidTarget.c:5-44` 走 `cardTargetCriteria(+0x280)` | `TargetCondition = cond=276 role=cardtarget`；`legal=1` 的合法池 = **1 格**（`FIX_B` 载具） | 改前 `null`（条件根本没挂） |

---

## ④ 没查清 / 边界（⛔ 别当成「已验」）

### ④·1 任务 1 在既有读数上**零可观测** —— 这不是「没改对」
`play all dump` 与本批之前**逐字节相同**（0 行不同）、`check` 输出指纹也不变。原因是**夹具不碰 `TargetCondition`**：战术卡那一路由 `PickSlot`/`legal=1` 显式给目标格，`ResolveTargetsCore` 拿到的就是「玩家点的那个」，不会再去筛。⇒ 这一笔解锁的是 `play <id> preset legal=1` 的 `[目标条件]` 行，**不是**全池指纹。**因此本批不需要为它重钉任何指纹。**

### ④·2 手牌态夹具的三条边界
1. **不是「真打一发」**：本档**不把被测卡打出去**（`BuildFixture(target: null)`）。所以它**不能**回答「这张卡打出后有没有效果」—— 那是主档 `Measure` 的事。
2. **`createssecret` / `createssabotage` 是直调广播**（`subject=null`，无真造的牌）⇒ 只能验「手牌监听者会不会被叫醒」，**验不到**造牌链本身。
3. **变化 0 条 ≠ 这张卡不监听**：也可能是它本来就不监听这个事件。⛔ 必须配一次 `legacy=1` 对照，或先 `traits <id>` 看它的触发头。
4. **卡名里带逗号的两张用不了 `hand=` / `sub=`**：`preset` 是**用逗号分项**的，而池内有两张卡名带逗号
   （`Varius, Lord Kakophonist` / `Xarahan, Daemon Prince`，现读 `cards_engine.json`）⇒ 会被切成两项。
   读数是**出声报错 `rc=2`**（⛔ 不静默），错误信息里已写明「改用卡 id」。

### ④·3 指纹（⛔ 本人一个 `Pinned*Md5` 都没改）

| 指纹 | 本批之前（07:52 现读） | 本批之后 | 差因 |
|---|---|---|---|
| 解析输出 `PinnedParseMd5` | `18439d965d14288fe28cfbe5986dc445` | **`18439d965d14288fe28cfbe5986dc445`（不变）** | 任务 1 不进 `check` 的 dump（它早就带 `card:`） |
| 结算 `PinnedPlayMd5`（主档 + 电池合成） | `e11aa1c263e933dbdd67718133c8e5b2` | **`e11aa1c263e933dbdd67718133c8e5b2`（不变）** | 本批 2/3/4 三件都不进 `FingerprintRow`（实测 `play all pf` 逐字节相同） |
| 结算指纹·主档分项 | `b381c23e10e4bbc6a6d82380614a01b6` | **`b381c23e10e4bbc6a6d82380614a01b6`（不变）** | 同上 |
| 结算指纹·电池分项 | `49180bf710f5ba6d9028546e2f97a9c6` | **`49180bf710f5ba6d9028546e2f97a9c6`（不变）** | 同上 |
| `play all dump` 文件 | 1,128 行 × **21** 列 | 1,128 行 × **22** 列（末列 `playOrder`） | 有意（任务 3）；逐行核过 **除新列外 0 行不同** |
| `play all` 的 rc | 0 | **0** | —— |
| `play check` 的 rc | 1（`e11aa1c2…` ≠ 钉住 `a93ffb8f…`） | **1（同一个值）** | 既有红，由本会话别人的引擎改动造成，⛔ 与本批无关 |
| `check` 的 rc | 1（解析差异 1 行 + 输出指纹红） | **1（同一个值）** | 既有红（`B8_AE` 有意改的那 1 张） |

### ④·4 本批期间**别人**正在改引擎（读数不可比）
`Assets/RuleEngine/Core/CardDef.cs` 在 08:00–08:02 之间被**别的代理**改成编不过（`CS0708 TraitReachability.IsWarlordEquivalent`）。按既定判据（错误**全部**集中在不是自己负责的文件上 ⇒ 不是我的问题），我等它落地再取最终读数；`ruleprobe.sh` 的构建失败分支会把这类情况如实喊出来（`A1466`）。
⇒ **最终指纹必须由调度台在同步点现读**，⛔ 别用我这里任何带时间戳的数去重钉。

---

## ⑤ 顺手发现（不属于本批四件，但下个会话要看）

1. 🔴 **`战斗逻辑驱动.tsv` 第 217 / 218 / 219 行是【过期文案】**（`BattleManager.BroadcastCollected{Faith,QuestPoints,SpiritStone}` 三条）：
   它们的「差异」栏还写着「我们的 `BroadcastWhen` 走通用四跳 ⇒ 我们会叫醒、原版不会（真差异，今天不可观测）」，
   而 `B8_AE` **当天已经修掉**、本人本次实测也是 0 伤害（见 §②·2）。同批的 `Support` 那三条（709/710/711）已更新为「✅ 已收窄」⇒ **同一件事在同一张表里有两种说法**（铁律 5 那一类）。
   ⇒ 请调度台把这三行的「差异」栏改成「✅ 已收窄（`boardAndPersistentOnly: true`，判据 `B8_AE`）」，并顺手核它们的落点行号（217 行写的是 `EffectResolver.cs:12633-12638`，实际是 `:12688-12689`；218 行 `:12627` → `:12673`；219 行 `:12045-12070` 现在是 `RuleCore.cs:12050-12075` 那一带）。
   ⛔ 我没改（主表不归我）。

2. **`EnumerateActions` 不填 `Score`，而 `SelectBestAction` 读 `Score`** —— 见 §②·4 那条警告。任何新写的 AI 读数口都会踩；
   产品那条路（`NextAction`）自己补了写回，所以线上没问题，但**外部调用者很容易无声地拿到错的选择**。

3. **`play` 的 `[目标条件]` 日志只有 `legal=1` 才打**（`preset.Legal > 0` 那个分支里）—— 不传 `legal=1` 时这一批的「卡级条件挂上了」看不见。
   要不要把它提到 `legal` 分支外面，涉及既有输出的形状，**我没动**（留给调度台定）。

4. **`traits <id>` 走的是 `CardDatabase.Parse`，不是 `Program.LoadPool`** —— 所以它一直能读到 `Targeted` 真值（`B8_AH` 报告里 `Targeted=1` 就是这么来的）。
   ⇒ 「探针读不到 `targeted`」这句话**只对 `play`/`scan` 那条路成立**，对 `traits` 不成立。这正是 `A1911` 那一族最容易误判的地方：
   **同一个字段，两个装载口，一个有一个没有。**

---

## ⑥ 给调度台的请求

1. **重钉窗口**：本批**不需要**重钉 `PinnedParseMd5`（实测不变）。`PinnedPlayMd5` 本批也不改（2/3/4 三件都不进 `FingerprintRow`），
   但**当前它本来就红**（`e11aa1c2…` ≠ 钉住 `a93ffb8f…`，由本会话其他人的引擎改动造成）⇒ 请在同步点**统一现读**后一次性重钉，
   并顺手核 `PinnedPoolMd5`（卡表已到 v31，池 md5 现读 `d78f557e4272c2c571a9327139c7202a` ≠ 钉住 `721637d2…`）与 `PinnedBaselineMd5`。
2. **请把 §⑤·1 那三行过期文案**（217/218/219）合并进主表。
3. **`play all dump` 的列数从 21 → 22**（末列 `playOrder`）。若别的代理的脚本按「固定列数」解析这份 dump，请通知他们。

---

## ⑦ 改动清单（全部在 `工具/ruleprobe/**` ＋ `工具/ruleprobe.sh`；⛔ 没碰任何 `Assets/**` / 主表 / 正本；行号 = 2026-10-26 08:06 现读）

| 文件 | 行 | 改了什么 |
|---|---|---|
| `工具/ruleprobe/Program.cs` | `653` · `720` · `1089` · `1151-1155` | 任务 1 ①：`LoadPool` 读 `targeted`（缺列 `-1`＋出声）并透传给 `CardDef` |
| `工具/ruleprobe/Program.cs` | `args[0]=="ai"` 那一支 | 任务 4：`ai actions` 的第二参透传（老用法不变） |
| `工具/ruleprobe/PlayProbe.cs` | `898`（原 `EffectText.Parse(card.Desc, out _, out _)`）· 非单位卡 `expectedPay` 那一处 | 任务 1 ②：两处 `Parse` 补 `card:` |
| `工具/ruleprobe/PlayProbe.cs` | `Blank()` 里的 `initiative:` 之后 | 任务 1 ③：替身契约补 `targeted` |
| `工具/ruleprobe/PlayProbe.cs` | `342-404`（`PresetSpec` 新字段/`Any`）· `461-486`（`Parse` 五个新 token）· `678`（`RunHandListen`）· `784`（`FireHandEvent`）· `836`（`WhenEventKindOf`）· `223`（`Run` 的分支） | 任务 2 |
| `工具/ruleprobe/PlayProbe.cs` | `1980`（`PlayOrderCol`）· `Reading.PlayOrder` · `MeasurePlay` 里那一句 · `Sweep` 的 dump 头/值 · `Print` 的一行 | 任务 3 |
| `工具/ruleprobe/AiProbe.cs` | `using System.Linq` · `Run(spec, spec2)` 的切档 · `307`（`RunActions`）· `421`（`KindName`）· `444`（`BuildActionsFixture`） | 任务 4 |
| `工具/ruleprobe.sh` | 用法头（`play preset hand=…` / `ai actions` / `playOrder` 列三条） | 文档 |

**删掉的临时文件**：`工具/ruleprobe/_tmp_av/`（我用来存改前/改后 dump 的临时目录）—— 已删，⛔ 没在工程里留 `__pycache__`。
**⛔ 没改的**：`PlayProbe.PinnedPlayMd5` / `Program.PinnedParseMd5` / `PinnedPoolMd5` / `PinnedBaselineMd5` / `基线/out_*.txt` —— 一个都没动。
