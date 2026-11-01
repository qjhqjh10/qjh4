# `B115_S2` · 我们自己的引擎里有没有「卡被打出 → 逐 `trigger 0` 能力派发」

> 只读查证代理 `B115_S2`｜日期：2026-11-01｜会话：第一百一十五会话
> ⛔ 未跑 Unity、⛔ 未动 git、⛔ 未改任何源码/账/正本。本文件是**唯一**产出。
> 数据面口径：`cards_engine.json` **v47 · 1,129 卡 · `triggerAbilityIndices` 1,622 行**（本次现读，与 `B114_R6` 一致）。

---

## 〇、判决（一句话）

**没有。**

我们有两条「打出时跑东西」的路 —— ① **按卡面关键词/文本**跑一次（单位走 `rally`，战术/防御走 `desc` 正文），
② **按 `trigger == 0` 行挂附魔**（只覆盖「打出即挂一件附魔」那一类）——
但**两条都不是**原版那条「打出 → 遍历 `cardAbilities[trigger == 0]` 的**每一条** ability → 每条跑它自己的 `abilityLogic`」。

行级的派发口（`FireTriggerRowsByChosenAbility`）**存在**，但它只能被 `FireTriggerByNumber` 叫到，
而后者在生产上只收到 **`60 / 185 / 187 / 210 / 410`** 五个字面量 ⇒ **`trigger 0` 从来没进过这条口**。

⇒ `AM54` / `GOF93` / `GOF104` 的 `ca=380` **不是「执行了但没有执行器」**，而是**层①（没有调用方）+ 层③（`380` 被分类成不执行）+ 两条结构差**（见 §2·3 / §2·4）。
（⚠️ 初稿曾把「数值路的整桶闸」也列为拦路石，**已由探针实测推翻** —— 见 `## 探针读数`。）

---

## 一、我们的调用链（逐跳 `文件:行号`，全部现读）

### 1·1 单位卡：`打出手牌 → Rally 正文`（`trigger 0` 的**文本**路）

```
RuleCore.PlayCard                                 RuleCore.cs:3941 / :3947 / :4018
  └─ PlayCardCore                                 RuleCore.cs:4038
       └─ FireTriggerOnBoard(ctx, unit, KeywordTable.Rally, chosen)   RuleCore.cs:4222   ← 单位侧唯一的 trigger 0 入口
            └─ FireTriggerOnBoard                 RuleCore.cs:13196
                 └─ FireTriggerAt                 RuleCore.cs:12705
                      ├─ ops = u.FxOps("rally")   RuleCore.cs:12716-12717   ← **按关键词取文本 op**（不是按 ability 取）
                      ├─ AbilityFamilyGateOpsForTrigger   RuleCore.cs:12843  （这是**闸**，剔 op，不是派发）
                      ├─ EnqueueAbility(... body = FireTriggerDeferred)   RuleCore.cs:12897-12902
                      └─ FireTriggerDeferred       RuleCore.cs:12914
                           ├─ ResolveOps(op 批, keyword)   RuleCore.cs:12933   ← **一批 op 一次性跑**
                           └─ followUp 那一跳       RuleCore.cs:13032-13036
                                EmitFollowUpResolution + FireTriggerByNumber(…, 410, …)
```

关键：`:4222` 那一行的注释**自己**就把这一跳认成原版 `RawCardScript__OnCardPlayedWithTarget.c:43`（`TriggerAbilities(ThisCardPlayed)`）。
**认的位置对，实现的口不对** —— 它喂进去的是一个**关键词**，不是 `cardAbilities` 的下标。

### 1·2 战术 / 防御卡：`打出 → desc 正文`（同一条 `trigger 0` 的**文本**路）

```
RuleCore.PlayCard → PlayCardCore   RuleCore.cs:4044-4046（非单位卡分流）
  └─ EffectResolver.PlayTactic     EffectResolver.cs:2579
       └─ PlayTacticCore           EffectResolver.cs:2596
            └─ EnqueueAbility("tactic-body:"+name, PlayTacticBodyDeferred)   EffectResolver.cs:2802-2805
                 └─ PlayTacticBodyDeferred   EffectResolver.cs:2815
                      ├─ ResolveOps(..., abilityFamilyCard: card,
                      │             abilityFamilyTrigger: "ThisCardPlayed")   EffectResolver.cs:2832-2835
                      │     └─ ApplyAbilityFamilyGate   EffectResolver.cs:458   ← **闸**（按族剔 op），不是逐 ability 派发
                      └─ followUp 那一跳        EffectResolver.cs:2917-2928
                           只 EmitFollowUpResolution（纯表现事件）
                           🔴 **没有** FireTriggerByNumber(410) —— 与单位路（`RuleCore.cs:13036`）**不对称**
```

`ResolveOps` 拿到的是**整张卡 `desc` 解析出来的一整批 op**（`EffectText.Parse`），一次跑完；
`abilityFamilyTrigger` 只用来「在定位不唯一的卡上按能力族剔 op」，**不是**「一条 ability 跑一次」。

### 1·3 关键词 → trigger 名的映射（存在，但**不参与选 ability**）

```
ActionPump.TriggerNameOfKeyword        ActionPump.cs:1081
  case KeywordTable.Rally: return "ThisCardPlayed";   ActionPump.cs:1099   // 0
```
它只喂给 `abilityContext` / 附魔位 / `FollowUpSchedulerOf` 的名字比对；
**没有任何地方**拿这个名字去 `cardAbilities` 里挑行。

### 1·4 唯一的「按 trigger 数值派发」口（原版 `TriggerAbilities` 的对应物）

```
RuleCore.FireTriggerByNumber                  RuleCore.cs:17666
  └─ EffectResolver.FireTriggerByNumberWithSink   EffectResolver.cs:21067
       ├─ TriggerOpsByNumber(triggerNumber)          CardDef.cs(CardDefPartial_B96CA.cs):102
       │     └─ 数据半 = MapTriggerOpsByNumber       CardDatabasePartial_B98H7.cs:134
       │        （按 `op.AbilityIndex == row.Ab` 分桶、**按 trigger 归并**，CardDatabasePartial_B98H7.cs:205-212）
       └─ ops 为空时才 FireTriggerRowsByChosenAbility   EffectResolver.cs:21078 → :21385
            └─ TriggerRowKindOf(ca)                      EffectResolver.cs:21368
                 `320 ⇒ FollowUp · 325/340/380/480 ⇒ OwnLane · 其余 ⇒ BodyOp(出声)`
```

**生产调用点全表（现读，逐处核过）：**

| trigger 数值 | 调用点 | 场景 |
|---|---|---|
| **60** | `EffectResolver.cs:20605` | 单位受伤（`FireBoardDamagedByNumber`） |
| **185 / 187** | `EffectResolver.cs:20606` / `:20607` | 同上（按伤害类型分岔） |
| **210** | `EffectResolverPartial_B97W2.cs:451` | 造牌 |
| **410** | `RuleCore.cs:13036`（关键词路 tail）· `EffectResolver.cs:20679`（`RunFollowAfterResolutionRow`） | 后续结算 |

⇒ **`trigger 0` 一次都没有。** 这是「行级派发口存在但够不到 trigger 0」的直接证据。

### 1·5 唯一「逐 `trigger == 0` 行」的派发：**附魔挂载**（窄口，不是通用能力派发）

```
Enchantments.AttachGrantedByPlayedCard      Enchantment.cs:1624
  闸：if (g.Trigger != TriggerThisCardPlayed) continue;   Enchantment.cs:1639
调用点：RuleCore.cs:4213（单位）· EffectResolver.cs:2733（战术/防御）
数据源：enchantment_templates.json —— 85 张模板 / 135 行（B114_WB2）
```

它只服务「`chosenAbility ∈ {30,40}` + 有 `enchantmentId`」那一类（打出即挂一件附魔），
**不是**「跑这条 ability 的效果」。同族的 `Enchantments.OnCarrierPlayed`（`Enchantment.cs:1190`）是 `DA42` 专用硬编码。

---

## 二、`trigger 0` 的 `ca` 行今天走不走得到

### 2·1 规模（v47 现读，本次重扫，与 `B114_R6` 一致）

| 量 | 值 |
|---|---|
| `triggerAbilityIndices` 总行数 | **1,622** |
| `trigger == 0` 的行 | **936**（涉及 **587** 张卡：tactic 442 · unit 105 · defence 39 · enchantment 1） |
| `trigger == 0` ∧ `ca >= 320` | **60** 行 |
| `trigger == 0` ∧ `ca ∈ {320,325,360,380}`（**结构上文本表达不了**的） | **37**（320×15 · 325×1 · 360×18 · 380×3） |
| 其余 23 行（有文本对应物的可能性高） | 420×5 · 430×6 · 410×2 · 411×1 · 440/460/466/468/470 各 1 · 475×3 · 340×1 |
| 105 张 unit 里带 `rally` 关键词的 | **105/105**（⇒ 单位侧**不会**有「整张卡走不到 trigger 0」的情况） |

### 2·2 结论：**走了，但走的是「文本批」，不是「逐 ability」**

- **有文本的行** —— 会跑。单位经 `rally`、战术/防御经 `desc`，**两次都是把该 ability 的 op 混成一批跑**
  ⇒ 原版的 `(ab, lg)` 行序、逐行自己的目标表/条件表**在这一层已经丢失**。
- **没有文本的行（37 行）** —— **一次都跑不到**。`380` 尤其：它的语义是**纯记账**（写 `referencedCards`），
  卡面上根本不存在对应句子，文本解析器**永远解不出它**。

### 2·3 为什么 `AM54` / `GOF93` / `GOF104` 的 `ca=380` 仍然没效果 —— **层① + 层③ + 结构差**（层② 已被探针推翻）

**层①：没有调用方。** `FireTriggerByNumber` 只收 60/185/187/210/410（§1·4 表）⇒
`FireTriggerRowsByChosenAbility` 永远看不到 `trigger 0`。

**层②：🔴 探针实测后【已被推翻】—— 原稿的「桶非空」不成立。**
`FireTriggerRowsByChosenAbility` **只在** `TriggerOpsByNumber(0)` 为空时才被调（`EffectResolver.cs:21078`）。
原稿按「`op.AbilityIndex == row.Ab` 分桶」推断三张卡的 `trigger 0` 桶非空 —— **实测是空的**：

| 卡 | trigger 0 的行 | `HasTriggerOpsByNumber` | `TriggerOpsByNumber(0)` | 对照 `TriggerOps("rally")` |
|---|---|---|---|---|
| `AM54` | `(ab0,lg0,ca=0)` · `(ab0,lg1,ca=380)` · `(ab0,lg2,ca=320)` | **false**（归位失败⇒不挂表） | **null** | null（战术正文走 `desc`） |
| `GOF93`（unit） | `(ab0,lg0,ca=250)` · `(ab0,lg1,ca=380)` · `(ab0,lg2,ca=320)` | **false** | **null** | **2 条**（正文在，但数值路看不到） |
| `GOF104` | `(ab0,lg0,ca=380)` · `(ab1,lg0,ca=260)` · `(ab1,lg1,ca=320)` | **false** | **null** | null（战术正文走 `desc`） |

**为什么**：`MapTriggerOpsByNumber` 的 `body` **只从 `CardDef._triggerOps` 取**（遍历 `RoutableTriggers`，`CardDatabasePartial_B98H7.cs:154-172`）——
而**战术/防御的正文根本不在那张表里**（它们在 `desc`，由 `PlayTacticBodyDeferred` 现解析），
单位的 `Rally:` 正文则在 `TriggerOps("rally")` 那一格。全池实测（§探针读数）：
**587 张有 `trigger 0` 行的卡里，桶非空只有 14 张 · 桶空 0 张 · 其余 573 张连表都没挂**（退回档查 `TriggerOps("thiscardplayed")`，恒 null）。

⇒ **层②这条拦路石【不存在】**：只要有人接上 `FireTriggerByNumber(0)`，兜底**会被叫到**。
（`380` 真正卡住的是层③ + §2·4 的结构差，不是「轮不到兜底」。）

**层③：就算进去了，`380` 也被分类成「不执行」。** `TriggerRowKindOf(380)` 返回 `OwnLane`
（`EffectResolver.cs:21372`），`FireTriggerRowsByChosenAbility` 对 `OwnLane` 是 **`continue`**（`:21399`）。
理由写在该块头注释 ③：`320/325/340/380/480` 的归属「在别处」。

**380 的执行体确实在，但它只接在另一条路上：**

| 件 | 位置 | 生产可达性 |
|---|---|---|
| `SetReferenceCards` | `EffectResolver.cs:1366` | 执行体已落齐（含 `:2132` 的 `count>0` 闸、`B102_EXEC` 核过） |
| `RunSetReferenceCardRow`（`ca==380` 行执行器） | `EffectResolver.cs:20882` | 只接在 **主动能力行** 那一趟（`EffectResolver.cs:18782` 的 `if (ca == 380) …`）；而池内 `activeAbilityLogic` 的 `ca 380/480` = **0 行** ⇒ **生产 0 次执行** |

**⇒ 一句话：这是「根本没进这条路」，不是「进了但 380 没有执行器」。**
两者差别很大 —— 如果只是「没有执行器」，补一个 `RunSetReferenceCardRow` 的接线就行；
实际情况是**我们连「逐 ability 派发」这层结构都没有**（`380` 只是这条岔路上最容易命名的一格）。

### 2·4 另一条硬拦路石（`B114_R6` §4·3 已记，本次复核成立）

- `AM54` / `GOF93` 的 `380` 行 `ta = 150 (previousAbilityFiltered)`；
  而 `ResolveTargets` **只特判 200/201/60**（`EffectResolver.cs:1446` / `:1501`）⇒ **`ta=150` 的目标集算不出来**。
  （`GOF104` 的 `380` 行是 `ta=30`，用「玩家点定的那一个」，**它是三张里唯一不受这条拦的**。）
- `SetReferenceCards(ctx, owner, **UnitState acting**, …)` 要一个 `UnitState`；
  战术/防御卡在我们模型里**没有** `UnitState`（`EffectResolver.cs:1496-1508` 如实标注）⇒
  **AM54 / GOF104 这种战术卡结构上接不上**；`GOF93` 是单位，不受这条限制。
- AM54 的引用表**必须**装「刚抽进手牌的 3 张卡」，而 `UnitState.ReferencedCards` 只装 `UnitState`
  （`UnitState.cs:642` / `:813`）⇒ 要单独立账做「`CardDef`/`CardInstance` 级引用表」。

---

## 探针读数

> 新增只读读数口：`Unity/工具/ruleprobe/TriggerOpsProbe.cs` 的 **`trigger0` 模式**
> （`bash d:/4/Unity/工具/ruleprobe.sh triggerops trigger0`；`rc` 恒 0，不碰基线/三指纹/`check`·`play`·`ai` 输出）。
> 🔴 它**走 `CardDatabase.Parse` 真实装载链**（与 `triggerops` / `traits` / `dbparse` 同一路）⇒ 读数是**产品口径**，
> ⛔ 不是 `Program.PoolList` 那种手读 JSON（那样 `AttachTriggerOpsByNumber` 根本不会被调、读数会落进退回档）。

**实跑**（2026-11-01，`cards_engine.json` v47 · 1,129 卡）：

```
[triggerOpsByNumber] 卡 1129｜列未知 0｜列空 119｜已挂表 44｜归位失败(不挂) 966
  ｜行 1622（已归位 44；跳过 lg=-1 1 · ab 坏 0 · trigger 坏 0 · 无正文 op 1129 · 该 ab 无 op 448）
```

| 读数 | 值 | 含义 |
|---|---|---|
| `trigger 0` 有行的卡 | **587**（936 行，`lg=-1` 1 行） | 与 §2·1 一致 |
| 列已知 ∧ 桶**非空** ⇒ ca 兜底**进不去** | **14** 张 | 只有这 14 张会被「整桶闸」挡住 |
| 列已知 ∧ 桶**空** ⇒ ca 兜底**会**被叫到 | **0** 张 | — |
| 列未知（`HasTriggerOpsByNumber == false`，退回档） | **573** 张 | 退回档查 `TriggerOps("thiscardplayed")` ⇒ **恒 null** ⇒ 实际等价「桶空」 |
| 行级：自己的 `Ab` 在桶里有 op / 没有 op | **16 行 / 919 行** | 「这一行有没有文本对应物」的真读数 |
| `AM54` / `GOF93` / `GOF104` | `HasTriggerOpsByNumber` **全 false** ⇒ `TriggerOpsByNumber(0)` = **null** | §2·3 层② 的推理**被推翻** |

**「没有 op」那 919 行按 `ca` 分布**（前几档）：`30×367` · `10×130` · `0×64` · `70×56` · `100×53` · `60×35` ·
`20×29` · `130×18` · **`360×18`** · `50×16` · **`320×15`** · `75×14` · `110×14` · `250×12` · `260×10` · `3×9` ·
`140×6` · `430×6` · `420×5` · **`380×3`** · `410×2` · `340×1` · `325×1` · `470×1` · `475×3` 其余略。

**三张焦点卡**（逐行读数，`桶内 op 数` 一律 0 —— 因为表压根没挂）：

| 卡 | `TriggerOpsByNumber(0)` | 行 | 对照 `TriggerOps("rally")` |
|---|---|---|---|
| `AM54` | **null** | `ab0/lg0 ca=0` · `ab0/lg1 ca=380` · `ab0/lg2 ca=320` | null（战术正文走 `desc`） |
| `GOF93` | **null** | `ab0/lg0 ca=250` · `ab0/lg1 ca=380` · `ab0/lg2 ca=320` | **2 条**（正文在，但数值路看不到） |
| `GOF104` | **null** | `ab0/lg0 ca=380` · `ab1/lg0 ca=260` · `ab1/lg1 ca=320` | null（战术正文走 `desc`） |

**⑤ `trigger 0` 桶非空的 14 张**（= 若接上 `FireTriggerByNumber(0)` 会**双发**的候选，补派发器时必须逐张挡）：
`AM19 Attilan Rough Rider` · `DA21 Land Speeder Vengeance` · `GSC35 Benefictus` · `SAU15 Deathmark` ·
`SAU17 Psychomancer` · `SAU16 Night Scythe` · `GOF16 Big Krumpaz` · `GOF15 Burna Boy` · `GOF5 Trukk Boy` ·
`GOF7 Warbiker` · `TAU66 Kroot Carnivore` · `TAU72 Lone-spear` · `TL62 Ravener` · `UM7 Tyrannic War Veteran`
（前 12 张各 1 条 op / 1 条 trigger 0 行；`TL62` 2 条/2 行；`UM7` 1 条/2 行）。
抽查 `AM19`：`Rally:` 单位、已挂表、`ca=40`，`TriggerOpsByNumber(0)` 非空 ⇒ 确实与 `rally` 文本路**同源** ⇒ 会双发。

**⇒ 判决更新：§2·3 层②【被推翻】。**「桶非空 ⇒ 兜底进不去」只对 **14/587** 张卡成立，
**对 `AM54`/`GOF93`/`GOF104` 不成立**（它们的桶是空的）。
`380` 卡住的是 **层①（没有调用方）+ 层③（`OwnLane` 不执行）+ §2·4 的结构差**，不是「轮不到兜底」。

⚠️ **一条顺带的结构事实**：`MapTriggerOpsByNumber` 的 `body` 只从 `CardDef._triggerOps` 取，
而**战术/防御正文与单位裸正文都不在那张表里** ⇒ `TriggerOpsByNumber` 这张表对 `trigger 0` 几乎是空的（586/587 卡为 null）。
⇒ 「数值路」这条设计**先天不适配 `trigger 0`**：就算把 5 个数值扩成全部，它也只能拿到一小撮卡的正文。
这一点要写进 `B-211` 的施工口径（补派发器**不能**靠扩数值路的调用点，必须**逐 ability 走行**）。

---

## 三、缺口清单（缺哪一跳 · 加在哪 · 最接近的现成代码）

| # | 缺哪一跳 | 要加在哪个文件 | 现有最接近的代码 |
|---|---|---|---|
| **1** | **打出时的逐 ability 派发（`trigger 0`）** —— 单位：`PlayCardCore` 的 Rally 之后；战术：`PlayTacticBodyDeferred` 的 `ResolveOps` 之后 | `RuleCore.cs:4222` 附近 · `EffectResolver.cs:2917` 附近（两处各接一次，共用同一个派发器） | `FireTriggerRowsByChosenAbility`（`EffectResolver.cs:21385`）；`CardDef.AbilitiesForTrigger`（`CardDefPartial_B94B16.cs:181` —— 原版 `GetAbilitiesForTriggerRawcard` 的等价物，**生产 0 调用点**，只有 Editor 断言在用） |
| **2** | **行级执行器：`380`** | 执行体已有（`RunSetReferenceCardRow` `EffectResolver.cs:20882`）⇒ 缺的是「从 trigger-0 行派发里叫到它」 | 同 #1 |
| **3** | **行级执行器：`360`** | `RunPlayChosenCardAbility`（`EffectResolverPartial_B88E2.cs:981`）同样只在主动行那串 `if (ca == …)` 之后（`EffectResolver.cs:18781-18782` 一长串） | 同 #1 |
| **4** | **`ta = 150 previousAbilityFiltered`** | `ResolveTargets` / `ResolveTargetsCore`（`EffectResolver.cs:1440` / `:1453`；现只特判 200/201/60） | `ta=200/201` 那一段（`EffectResolver.cs:1501-1529`）是**同族样板** |
| **5** | **手牌卡的引用表**（AM54 的 380 目标 = 刚抽进手的 3 张） | 需要 `CardDef`/`CardInstance` 级引用表（`UnitState.ReferencedCards` 装不下） | `UnitState.ReferencedCards`（`UnitState.cs:642`） |
| **6** | **与文本正文去重**（同一 ability 的 op 只跑一次） | 派发器自身 | `MapTriggerOpsByNumber` 的 `op.AbilityIndex == row.Ab` 口径（`CardDatabasePartial_B98H7.cs:208`）；`EffectOp` 还有 `LogicIndex`（`EffectText.cs:226`）可用 |

**⛔ 仍然不推荐「在出牌点补一句 `FireTriggerByNumber(ctx, u, 0, …)`」—— 但探针把理由改了三条。**
（原稿理由 ①「整桶⇒380 被跳过」与 ②「几乎全部 936 行双发」**均已被 §探针读数推翻**，铁律 5 就地订正。）
1. **它解决不了 `380`**：兜底里 `TriggerRowKindOf(380) == OwnLane` ⇒ `continue`（`EffectResolver.cs:21372` / `:21399`），
   `380` 需要**新的行执行器接线**，不是「接上数值路就自动好」；
2. **有 14 张卡会双发**（不是全部 —— 清单见 §探针读数 ⑤）：它们的 `trigger 0` 桶非空、且正文已由 `rally` 文本路跑过；
3. **战术卡没有 `UnitState`** ⇒ 那句在战术路上**编译不过**（`FireTriggerByNumber` 第 2 形参是 `UnitState`，`RuleCore.cs:17666`）；
4. **它给不了 `(ab,lg)` 的行序与逐行目标** —— 而 `AM54` 的 `380` **必须**排在 `lg0`（抽 3 张）之后，兜底那一路结构上表达不出这个次序。

---

## 四、施工建议（含要补的断言与灭自证）

### 4·1 建议的最小可验证格：`GOF104`（`ta=30`，不受 §2·4 三条拦路石影响）

它的 `380` 行目标集 = **玩家点定的那一个**（`ta=30`，`AbilityLogic__GetTargets.c:192-207`），
`RunSetReferenceCardRow` 的 `(ta == 30 || ta == 220) && chosen != null` 那一支（`EffectResolver.cs:20895`）**今天就能用**。

### 4·2 派发器形状（建议）

```
RunTrigger0RowsByAbility(ctx, card, owner, slotCarrier?, chosen, actingCard, targetCard)
  foreach row in card.TriggerAbilityRowsOf(0)   // 按 (ab, lg) 升序 —— 原版就是 cardAbilities × abilityLogic 的原序
    switch TriggerRowKindOf(row.Ca)
      BodyOp  : 若该 ab 的正文 op 已由文本路跑过 ⇒ **skip**（⛔ 打两次）
                否则走 FireTriggerRowsByChosenAbility 的出声档（= 已知缺口）
      FollowUp: RunFollowAfterResolutionRow
      OwnLane : ca==380 ⇒ RunSetReferenceCardRow ；其余（325/340/480）keep 现状
      Unknown : skip + 出声（`ca < 0`，现池唯一一行 `GOF97` ab0/lg-1/trigger0）
```

- 落点：单位侧在 `RuleCore.cs:4222` 那条**同一物理行**之后另起一句（⛔ 别插行 —— 本文件有账落点按行号引用）；
  战术侧在 `EffectResolver.cs:2917` 那段之前/之后同理。
- ⚠️ **「已由文本路跑过」不能拿 `TriggerOpsByNumber` 判**：那张表对 `trigger 0` 几乎全空
  （586/587 卡为 null，§探针读数）⇒ 判据要用**文本路自己的源**：单位 = `TriggerOps("rally")`、战术/防御 = `desc` 解析出的 `ops`；
  只有那 **14 张桶非空**的卡才需要额外用 `TriggerOpsByNumber(0)` 去重。
- ⚠️ 次序：`AM54` 的 `380` **必须排在 `lg0`（抽 3 张）之后** —— 它的目标集就是那 3 张
  （`B114_R6` §4·3 已立过反证：放在 `ResolveOps` 之前会读到**上一张卡的残值**）。

### 4·3 断言（`RuleEngineTest` / `RuleEngineTest_*`）

| # | 断言 | 灭自证（必须成对写） |
|---|---|---|
| A0 | **前提**：`AM54.TriggerAbilityRowsOf(0)` 长度 = 3、`Ca` 依次 `0/380/320`；`GOF104` 的 `380` 行 `Lg=0`、`Ab=0`，且 `TriggerOpsByNumber(0) == null`（**探针已证实**，§探针读数） | 把 v47 卡表降成 v46（`Ca` 全 `-1`）⇒ 该断言必须红 |
| A1 | `GOF104` 打出（点定一个目标）⇒ 该目标进 `ReferencedCards` | 把 `380` 行的 `Ca` 改成 `-1` ⇒ 引用表为空 |
| A2 | `AM54` 打出 ⇒ 引用表 ⊇ 刚抽的 3 张 | 把 `380` 的派发点**提到 `lg0` 之前** ⇒ 引用表 ≠ 那 3 张（次序判据真的在起作用） |
| A3 | **双发灭自证**：`AM54` 打出后「抽牌」只发生一次（`DrawnThisResolve` / 手牌数增量 = 3 且恰好 3） | 把文本路对 `ab0` 的 op 再跑一遍 ⇒ 手牌 +6 ⇒ 红 |
| A4 | `GOF93`（单位）同理：`Stomp`/`Rally` 正文只跑一次 **且** `ReferencedCards` 有值 | 只删 `380` 那一跳 ⇒ A4 后半红、前半仍绿（证明两条腿各自独立） |
| A5 | **不得回归**：936 行里 `ca` 全 `BodyOp` 的卡（如 `AM47` 的 `475 resetDuty`）行为逐字不变 | 把 `BodyOp` 档改成「也跑一遍」⇒ `play check` 指纹变、A5 红 |

⚠️ `A0` 必须是**独立的硬断言**（`Check(前提)`，不要再写成 `if (前提) Check(结论)` ——
本项目已因这种写法空转绿过三次，见 CLAUDE.md「自检里的 `if (x != null) Check(...)`」那条）。

---

## 五、`B-211` 的剩余缺口到底是什么（本次结账）

| 层 | 状态 | 证据 |
|---|---|---|
| ① 数据面（`triggerAbilityIndices[].ca`） | ✅ **已落** | `cards_engine.json` v47 · 1,129/1,129 卡 · 1,622 行；`CardDef.TriggerAbilityIndexSpec.Ca`；`MapTriggerAbilityIndices` 的 `version >= 47` 闸（`CardDatabase.cs:4835`） |
| ② 派发口（`ca` 的消费者） | ✅ **已落，但够不到 `trigger 0`** | `FireTriggerRowsByChosenAbility`（`EffectResolver.cs:21385`）**生产**唯一调用点 = `FireTriggerByNumberWithSink`（`:21078`；另一处 `:21450` 是自检门）；后者生产 5 个数值，**无 0** |
| ③ 派发口的闸（整桶为空） | ✅ **对三张卡【成立】**（🔴 2026-11-01 探针实测推翻原判） | §探针读数：三张卡 `HasTriggerOpsByNumber == false` ⇒ `TriggerOpsByNumber(0) == null` ⇒ 兜底**会被叫到**；全池只有 **14/587** 张卡桶非空 |
| ④ `380` 的分类 | ⛔ **明确不执行** | `TriggerRowKindOf(380) == OwnLane`（`:21372`）+ `:21399 continue` |
| ⑤ `380` 的执行体 | ✅ 在（`RunSetReferenceCardRow` :20882），但只挂在**主动能力行**路（`:18782`），池内主动行 `380/480` = 0 行 | 现读 |
| ⑥ `ta=150`（`previousAbilityFiltered`） | ⛔ 缺 | `ResolveTargets` 只特判 200/201/60（`:1446` / `:1501`） |
| ⑦ 手牌卡引用表 | ⛔ 缺（结构差） | `UnitState.ReferencedCards` 只装 `UnitState`（`UnitState.cs:642`） |

⇒ **`B-211` 不能按「派发口已落」核销。** 真实剩余 = **①（没有逐 `trigger 0` 派发）× ④（`380` 被分类成不执行）× ⑥⑦ 两条结构件**；
~~③（整桶闸）~~ **已由探针排除** —— 三张卡的桶本来就是空的。
其中 **① 是本体**：**我们缺的不是 `ca` 的消费者，是「`cardAbilities` 逐条 ability 派发」这一层结构。**

---

## 六、顺手发现（⛔ 我没动手改，请调度台分流）

1. 🟡 **交叉引用行号过期（铁律 5 那一类）**：`RuleCore.cs:4213` 的注释写「与战术那条路（`EffectResolver.cs:2655`）同一句」，
   而 `Enchantments.AttachGrantedByPlayedCard` 在战术侧的实际调用点是 **`EffectResolver.cs:2733`**（现读，全文件只此一处）。
2. 🟡 **同名两份 `Drag it Down`**：`cards_raw.json`（`资料/普查产出_1018/allcards_1350/`）里有
   `uniqueId b2f3624dc0fdad84d9eacd4a3a3ff8ea`（`cardShortId 0`）与 **`GOF104`（`cardShortId 519`）** 同名，
   而两者的 `cardAbilities` **完全不同**（前者 `ab0/lg0 ca=13`，后者是 `380/260/320` 那条链）。
   **池内只有 `GOF104`** ⇒ 今天无行为影响，但**按名字找卡会拿错**（同「同名多份」坑，CLAUDE.md 已有先例）。
3. 🟡 **`AbilitiesForTrigger` / `AbilitiesForTriggerWithIndex` 生产 0 调用点**
   （`CardDefPartial_B94B16.cs:181/:194`，`grep "\.AbilitiesForTrigger"` 只命中 `RuleEngineTest_EngineB94B16.cs:134/138/142/147/151/156`）
   —— 它是原版 `GetAbilitiesForTriggerRawcard` 的**现成等价物**，本次施工**可以直接用**，不必新造。
4. 🟡 **单位路与战术路的 `followUp` 不对称**（`B114_R6` §六·5 已记，本次复核**仍成立**）：
   单位侧 `RuleCore.cs:13036` = 事件 + `FireTriggerByNumber(410)`；战术侧 `EffectResolver.cs:2925` = **只发事件**。
   ⇒ 17 张战术/防御的 `410` ability 今天**只播动画、正文没跑**。
5. 🟠 **`abilityEffectTexts` 这一列太薄，不能当「逐 ability 文本」的替代源**：
   现读全池只有 **31 张卡 / 38 行**（1,129 张里）。⇒ 想靠它给每条 ability 配文本、再按 `(ab,lg)` 逐行派发，**走不通**。
6. 🟠 **`TriggerOpsByNumber` 这张表对 `trigger 0` 先天不适配**（探针实测 586/587 卡为 null）：
   `MapTriggerOpsByNumber` 的 `body` 只从 `CardDef._triggerOps`（`RoutableTriggers` 那几格）取，
   而**战术/防御正文走 `desc`、单位裸正文也不在那张表**。⇒ 任何「扩数值路调用点」的方案都拿不到它们的正文；
   真实可得的是 **44 张卡**（全池「已挂表」数）。这条对 `B-211` 的施工口径有直接影响（§探针读数末段）。
7. 🟡 **`TriggerRowKindOf` 把 `340` 归进 `OwnLane`**，而原版 `340 removeThisEnchantment` 是**真有执行**的
   （`AbilityLogic__MatchingTrigger` 那一跳，`B114_R6` §1·1 已取证）。今天靠
   `FireEnchantmentAbilitiesByNumber` 的手写等价物承担（`Pray Enchantment` trigger 60 · ca 340）——
   本次只是**登记这条分类口径的边界**，不是新缺口。

---

## 七、没查清 / 查不到的部分

1. ~~**「每张卡的 `trigger 0` 桶是否为空」没法静态算出来**~~ ⇒ ✅ **已由探针解决**（§探针读数），
   结论是**桶为空**、原「层② 桶非空」的推理**被推翻**。
2. **`GOF104` 的 `ab0`（只有 `ca=380` 一条 logic、没有正文）在原版里 `380` 的目标从哪来** ——
   本次查实数据是 `ta=30`（点定目标），但**没读** `AbilityLogic__GetTargets.c` 的对应分支确认
   「`ta=30` 在能力自己点定的那一格」与我们的 `chosen` 同源（本次只复核了 `RunSetReferenceCardRow` 的现有实现口径）。
3. **`ca=360`（18 行）的执行器 `RunPlayChosenCardAbility` 的完整可达性** 本次没逐句复核
   （`B114_R6` §五·3 已标「没逐句复核」，本次**沿用**未查状态）。
