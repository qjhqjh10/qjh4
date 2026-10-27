# B88_E4 · 引擎·小件（卡定义 / 单位状态 / 光环）施工报告（2026-10-27 · 第八十八会话 · 波 1）

> 批次 = 波 1 执行代理 **E4**。独占文件 = `RuleEngine/Core/CardDef.cs` · `Core/UnitState.cs` · `Core/Aura.cs` · `Data/CardDatabase.cs` ＋ 新建 partial 文件。
> 补丁 = `账/_parts/B88_E4.tsv`（11 列表头逐字照主表；**7 条** = 战斗逻辑驱动 5 + 特效动画 2）。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改账主表 / 正本 · 每改完 `.cs` 跑了 `TMPDIR=/tmp/wf_s88e4 bash 工具/typecheck.sh`。

---

## 〇 一句话结论

**「costs 1 less」的 CardDef 侧已落地 = `CardDef.CostPerKeywords`（`RuleEngine/Core/CardDefCostPerKeyword.cs`）；另把 7 条 C 行各自的「本文件那一半」落下**（Aura 统一摘入口 · UnitState 移出游戏状态位 · CardDef 按阵营推导主动能力 trait · TempBuff 扩档 + EnableEffects 过滤判据）。**每条 C 行的消费端（CostOf / ActionPump / RuleCore / 表现层）都不在本批白名单**，已逐条给出移交落点。`typecheck` **0/0**；`ruleprobe check` **rc=0**；`ruleprobe play check` **rc=1（差异来自并发 E1/E2/E3 的在飞改动，非本代理，见 §五·①）**。

---

## 一 🔴 costs 1 less 的 CardDef 侧：属性名 = `CardDef.CostPerKeywords`

> **属性名（调度台转给 E1/E2 用这一个）：`CardDef.CostPerKeywords`** —— 类型 `IReadOnlyList<CardDef.CostPerKeyword>`，实现文件 = **新建的 `RuleEngine/Core/CardDefCostPerKeyword.cs`**（`CardDef` 的 partial 半边，**零行号漂移**）。

**语义**：卡面 `This card costs N less for each <关键词> on <谁> <units/troops>` —— 这张卡**在手牌里**就随盘面状态**自己变便宜**（不是「打出去时降一次」）。

**`CardDef.CostPerKeyword` 的成员**：

| 成员 | 含义 |
|---|---|
| `int Delta` | 每个计数降多少（**负数**，与 `CostIfControl.Delta` 同号）⇒ 消费方写 `v += r.Delta * r.Count(ctx, owner)`，**别自己再取负** |
| `string Keyword` | 被数的关键词（`KeywordTable` 名，`darkpact` / `huntmark`）；按 `UnitState.KwValue` **求和**（层数可叠加） |
| `string Side` | `own`（卡面 `friendly`/`your`）/ `enemy` |
| `bool TroopsOnly` | 卡面写 `troops` ⇒ true（**督军不计**，同 `EffectResolver.CountFor` 的 `kind=="troop" && IsWarlord`）；写 `units` ⇒ false（督军算） |
| `string Body` | 整句原文（对账/日志） |
| `int Count(BattleContext ctx, int owner)` | 数出几个 = 该侧棋盘上带 `Keyword` 的单位其 `KwValue` 之和 |

**数据来源**（现读 `cards_engine.json` v35 / 1129 张实核）：本格按**卡面原文**自解析（懒算一次），形状正则 `^this card costs (\d+) less for each (.+)$`；关键词只认 `dark pact` / `hunt mark`（这两个是「可叠加层数」标记，`KwValue` 语义正确）。**池内命中 2 张**：

- `BL52 Unholy Smite` → `Delta=-1, Keyword=darkpact, Side=own, TroopsOnly=false`
- `SW58 Fenrisian Wolfpack` → `Delta=-1, Keyword=huntmark, Side=enemy, TroopsOnly=true`

**不重复降费**：`GSC37 Patriarch` 的 `Costs 1 less for each card in enemy hand` 主语是 `Costs`（无 `this card`）⇒ 形状正则**不吃**，仍走已有 `CostPerEnemyHandCard`；`ASH85` 的 `costs 1 less if you control…` 归 `CostIfControls`、也不吃。

**E1 的接线（`RuleCore.CostOf`，本批不动）**：在 `CostMods.Count == 0` 的**早退之前**、与 `CostIfControls` / `CostPerEnemyHandCard` 并排加一段：

```csharp
for (int i = 0; i < c.CostPerKeywords.Count; i++)
{ var r = c.CostPerKeywords[i]; v += r.Delta * r.Count(ctx, owner); }
```

`EffectResolver`（E2）**不消费本格**（已在付费之后结算 `lowercost` 那条是「打出去时」的语义，别混）。`CardDef.UnmodeledCostPerCountBodies` 是「认得出句式、认不出关键词」的口子（正常应为空）。

---

## 二 逐条证据（7 条 C 行各自落下了什么）

| 账行 | 原版方法 | 本批落下（本文件半边） | 残留（归属） |
|---|---|---|---|
| `战斗逻辑驱动:482` | `BattleManager.RemoveExtrinsicEffectsFrom` | **`Auras.RemoveExtrinsicBySource(ctx, sourceCardName)`**（`Aura.cs` 类尾）：遍历双方棋盘逐卡 `UnitState.RemoveExtrinsicEffectsFrom`，**只摘不重挂**（⛔ 不做 `Recompose` 替身——E1 已证有副作用） | 「每动作结束」触发面 → `ActionPump`（E3）；来源账只到 `TempBuff.SourceCard`/`_statMods.Source`（UnitState 已有口径） |
| `战斗逻辑驱动:636` | `BroadcastRemoveEffectsFrom` | 同上 `RemoveExtrinsicBySource`（**统一入口**，聚合条数） | 手牌半（手牌是 `CardInstance`、无 UnitState）；调用点动作类型 47 → `RuleCore`/`EffectResolver`（E1/E2） |
| `战斗逻辑驱动:637` | `BroadcastRemoveFromGame` | **`UnitState.RemovedFromGame`**（`UnitState.cs` 类尾，= `cardState 11` 引擎状态位；doc 钉死「置位只由 `RemoveFromGameCard(self)`、清除走通用 `SetCardState`、池内 0 入口 = 休眠」） | `RuleCore.BroadcastRemoveFromGame` 三跳（E1）；视图隐藏/摘碰撞（`BattleDriver`） |
| `战斗逻辑驱动:669` | `BroadcastWhileInPlay`（手牌半） | **`CardDef.CostPerKeywords`**（§一）—— 手牌半的**数据侧** | `RuleCore.CostOf` 读它（E1 · B01） |
| `战斗逻辑驱动:993` | `CardTraitCollection.GetTraitForActiveAbilityRawCard` | **`CardDef.ActiveAbilityTrait`** + 纯映射 `ActiveAbilityTraitForFaction`（六档：Ultramarines 1275 oath · Sororitas 1185 pray · AstraMilitarum 1220 duty · DarkAngels 1235 agenda · SpaceWolves 1265 ferocity · 其余 400/null；`HasActiveAbility==1` 为闸、`AlreadyOnCard` 标卡面是否已有） | 三处表现层调用点（`ActivateIconAnim`/`ActivateTriggerTraitAnim`/`Toggle`）改读它（CardPresentation） |
| `特效动画:195` | `CardEffectItem.LoadEffect` | **`UnitState.TempBuff`** 扩档（`UnitStateEffectDisplay.cs` partial）：`BuffType`（派生自 `Mod.BuffType`/`TraitRec.BuffType`）· `TraitValue`（派生自 `TraitRec.TraitValue`）· `EffectName` · `SecretBuff` · `QuestBuff` · `DisplaySourceName` | `SecretBuff`/`QuestBuff` **无生产者**（需 E2 在施加那刻从密令/任务附魔带下来）；文案/双行合成/专名分派 = E6 |
| `特效动画:198` | `CardEffectsService.EnableEffects`（输入过滤） | **`UnitState.IsEffectListEntry(buffType, hasQuests)`**：`buffType ∈ {6 changeRace, 35 daemonHost, 30 addRace}` 或（`16 choiceEnchantment` 且 `!hasQuests`）⇒ 不列 | 空文本占槽 / 收尾那一跳 = E6 |

**关键判据（第一权威）**：
- 993 = `decomp_full/CardTraitCollection__GetTraitForActiveAbilityRawCard.c:5-24`（六档逐句读过）；`CardArmy` 名值 = `dump.cs:45634-45652`。
- 195 = `CardEffect` 字段表 `dump.cs:119060`（`effectName +0x20` / `buffType +0x38` / `traitValue +0x40` / `secretBuff +0x90` / `questBuff +0x91`）＋ `CardEffectItem__LoadEffect.c:36-140`。
- 198 = `BuffType` 枚举名值 `dump.cs:45430-45462`（6 / 30 / 35 / 16）。
- 637 = `dump.cs:45480`（`removedFromGame = 11`）＋ `cpp2il CardScript.cs:16012` / `:9521`。
- 669 = `RuleCore.cs:1013-1019`（`CostIfControls`）、`:1025-1027`（`CostPerEnemyHandCard`）——「每次现算、早退之前」的同一纪律。

---

## 三 改动清单

| 文件 | 改动 | 行数 |
|---|---|---|
| `RuleEngine/Core/CardDefCostPerKeyword.cs`（**新建**，+ `.cs.meta`） | `CostPerKeywords` / `CostPerKeyword`(含 `Count`) · `ActiveAbilityTrait` / `ActiveAbilityTraitForFaction` | 新 266 行 |
| `RuleEngine/Core/UnitStateEffectDisplay.cs`（**新建**，+ `.cs.meta`） | `TempBuff` partial（BuffType/TraitValue/EffectName/SecretBuff/QuestBuff/DisplaySourceName）＋ `IsEffectListEntry` ＋ 四个 BuffType 常量 | 新 120 行 |
| `RuleEngine/Core/Aura.cs` | 类尾追加 `RemoveExtrinsicBySource` | 1019→**1064**（+45，全在类尾，无落点漂移） |
| `RuleEngine/Core/UnitState.cs` | 类尾追加 `RemovedFromGame`；声明加 `partial`（`UnitState` / `TempBuff`，**不改行数**） | 3190→**3216**（+26，全在类尾） |
| `RuleEngine/Core/CardDef.cs` | **净零**（曾误插入 `TraitReachability`，已完整回退：8869 行不变） | 8869→**8869** |

- 行尾：全部 **LF · 裸 CR 0**（二进制计数核实）。
- 未动 `RuleCore.cs` / `EffectResolver.cs` / `ActionPump.cs` / `SimpleAI.cs` / `BattleDriver.cs` / `CardDisplayWindow.cs`（黑名单）。

---

## 四 验证读数

| 项 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_s88e4 bash 工具/typecheck.sh` | **运行时 0 · 编辑器 0**（多次；中途几次红的全在别人在飞文件 `TutorialModePopup.cs` / `AllianceSearchTab.cs`，最后一次全绿 0/0） |
| 解析层对拍 | `bash 工具/ruleprobe.sh check` | **rc=0** · 全池 1121 张 · 解析差异 0 行 · 指纹 `c9a64798…` 与基线一致 |
| 结算层对拍 | `bash 工具/ruleprobe.sh play check` | **rc=1** —— 当次 `1ada22c2…` ≠ 基线 `03b6591f…`；①-a 变 1 张（GOF57）· ①-b 变 32 张（AM11/12/28/30/34…，`oath`/`duty`/`deploy` 相关）。**非本代理**（见 §五·①），本代理**未重钉**基线 |
| 补丁自检 | `python 工具/check_patch_conflicts.py` | 5 条跨补丁重复键（见 §五·⑤；本代理两条 195/198 已是 **E6 内容 + 本代理追加**的**超集**） |
| 行数/行尾 | `io.open(...,'rb')` | 见 §三；CardDef.cs 净零 |

---

## 五 没查清的部分 / 顺手发现（⛔ 本批不改，请调度台分流）

1. **`play check` rc=1 归因（不是本代理）**：本代理三处改动全是**新增、无调用方**（`CostPerKeywords` 无消费者 · `RemoveExtrinsicBySource` 无调用 · `RemovedFromGame` 无读写 · `TempBuff` 新属性无消费者），行为零变化。差异卡集中在 `oath`/`duty`/`deploy`（`GOF57` 落场能量 -4、`AM*` 电池 +若干），落在并发在飞的 `RuleCore.cs`/`EffectResolver.cs`/`SimpleAI.cs`。⇒ **请调度台在同步点（无 `.cs` 写手时）统一复跑并对账**。

2. **`UnitState.ActiveAbilitySuppressed` 是死格（影响账 783）**：`ApplyJam` 对非督军写 `ActiveAbilitySuppressed = true`（`UnitState.cs:2300`），但**唯一的主动能力闸 `RuleCore.CanStartActiveAbility:15304` 只读 `u.Card.HasActiveAbility`、不读它** ⇒ 「jam 后非督军不能用主动能力」原版有、我们没有（静默）。**账 783 `HideWarlordActiveAbilityForTutorial` 同理**：原版写 `hasActiveAbility=0`，我们的 `CardDef.HasActiveAbility` 是**共享不可变**属性、写了会污染整个卡池 ⇒ **783 的落点必须在 `UnitState` 加一个实例级 override + `CanStartActiveAbility` 读它**（后半段在 E1 的 `RuleCore`，本批白名单外）。⇒ 建议把 783 与 jam-抑制**合并成一条 C 行**交给 E1。

3. **`EffectText.ClassifyCount` 的 board 档丢关键词**：`Hunt Mark on enemy troops` 被归成 `countRef=enemy|troop|all` + `countScope=board`（`ruleprobe card "Fenrisian Wolfpack"` 现读），**Hunt Mark 整条丢了**；只有 `dark pact` 因专用分支才保留 `countScope=darkpact`。⇒ 任何想复用 `EffectResolver.CountFor` 数「enemy troops 上的 Hunt Mark」的消费方会**多数**（数成全部敌方部队个数）。**本代理的 `CostPerKeywords` 已绕开**（自带 `Count()`），但**别的将来消费方别直接拿 `CountFor` 数 huntmark**。`EffectText.cs` 不在本批白名单。

4. **`TempBuff.SecretBuff`/`QuestBuff` 无生产者**（账 195 ①）：`TempBuff` 是**文本派生**的，不带原版 `CardEffect.secretBuff/questBuff` 两位。要真实现「对面施加的效果走另一套遮蔽文案」，得由 `EffectResolver.ApplyOneGain` 在施加那刻**从密令/任务附魔带下来**（`EffectResolver.cs` = E2）。字段+语义已备，缺生产者。

5. **跨补丁重复键（调度台定序）**：`战斗逻辑驱动:637`（`B88_E2` vs `B88_E4`，E2 那份 = 主表原样、无内容变化 ⇒ E4 胜不丢内容）；`特效动画:195` / `:198`（`B88_E4` vs `B88_E6`）—— 本代理已把这两行做成 **E6 内容 + 本代理数据侧追加**的**超集**，故**任合并顺序都不丢内容**（建议先 E6 后 E4，让 E4 的超集胜）。

6. **不在本白名单、本批未动**（逐条移交）：`战斗逻辑驱动:573`（`inDeckDrawing` 呈现态，纯表现层 · CardView）· `:1282`（`chosenAbility` 分派执行器 · `RuleCore`）· `:500`/`:579`（`RuleCore`）· `特效动画:521`（`EffectsToShow.cs` 新文件 + `CardDisplayWindow`）。

7. **`ruleprobe.csproj` 的 `Data/` 显式列表坑（本代理撞到、已自愈）**：中途 `Data/SimpleAI.cs` 引用 `SimpleAI.ActiveEntry.cs`（新 partial）报 `CS0103`，因为 csproj 的 `Data/` 是**显式列出**、`Core/*.cs` 才是通配 ⇒ **新增 `Data/` 下的 partial 文件必须手动加进 `工具/ruleprobe/ruleprobe.csproj`**（E3 后来补上了）。提醒后续动 `Data/` 新文件的人。

---

## 六 交接给下一个代理（若本代理上下文告警）

- **本轮已完整收尾**（typecheck 0/0、补丁已生成、报告已落盘），**没有改到一半的活**。
- 下一批要接的（按归属）：① E1 在 `RuleCore.CostOf` 读 `CardDef.CostPerKeywords`（§一 那三行）＋ `CanStartActiveAbility` 读 `ActiveAbilitySuppressed`（§五·②）② E3 把 `Auras.RemoveExtrinsicBySource` 挂到动作边界 ③ E2 给 `TempBuff.SecretBuff/QuestBuff` 接生产者 ④ 表现层读 `CardDef.ActiveAbilityTrait` 与 `TempBuff.BuffType`。
- 判据全在上述引用；⛔ 别重查、别把 `Recompose` 挂动作边界。

---

# 第二批：整链缺口（`战斗逻辑驱动` 的「无落点 C」）

> 范围 = `判据/B88_无落点C_判据总表.tsv`（域=战斗逻辑驱动 且 判定=该做）**∩** `ledger_slice --domain 战斗逻辑驱动 --status C --nofile` = **16 条**（两表逐条对上）。
> 本批白名单只到 `CardDef*` / `UnitState*` / `Aura` / 新 partial ⇒ **16 条里只有 1 条能整链落下**（其余 15 条的非它不可落点全在 `RuleCore`/`EffectResolver`/`ActionPump`/`BattleDriver`/表现层，均黑名单）。见下表分流。
> 补丁 = `账/_parts/B88_E4b.tsv`（1 行）。

## 已落：`战斗逻辑驱动:1228 RawCardScript.GetExtraAbilityTraits()`（整链，数据侧）

**交付件**：新建 `RuleEngine/Core/CardDefExtraTraits.cs`（`CardDef` partial，零行号漂移）：
- `CardDef.ExtraAbilityTraits : IReadOnlyList<ExtraAbilityTrait>` —— 照搬：
  ① 25 对 `(trigger→trait)` 表（`.c` 里 25 个 `trait<<32 | trigger` 常量**逐条解码**；阵营 **20 Goff / 40 Sautekh 空表**），逐对看卡的 `abilityTriggers` 有没有该 trigger，命中收 `ExtraAbilityTrait{TraitValue, Keyword, TriggerName}`；
  ② `hasActiveAbility == 1` ⇒ 按六档阵营再补一条（复用第一批的 `ActiveAbilityTraitForFaction`）。
- `CardDef.FactionArmyValue(faction)` —— 卡面阵营 → `CardArmy` 数值（`dump.cs:45634-45652`）。
- 只出「派生集合」、**⛔ 不写 `_keywords`**（原版是追加进 SO 的 `CardTraits`；写进 `_keywords` 会污染 `Has(…)` 所有读点）。

**判据**：`decomp_full/RawCardScript__GetExtraAbilityTraits.c:1-120` ＋ `CardTraitCollection__GetTraitsFromAbilitiesRawCard.c:1-56`（25 对表）＋ `RawCardScript__CheckCurrentTraits.c:53-95`（追加语义）。**typecheck 0/0**。

**还差（归属）**：① 消费端 —— 原版唯一调用者 `CheckCurrentTraits` 自身也 **0 调用点**（「原版出货 0 入口」），我们这侧没有对应读取方 ⇒ 属休眠机制，等表现层/将来等价物读本属性；② **验收尺子口径没复现**（见下）。

### 🔴 没查清：`508 张 / 514 条` 那个验收尺子复现不出

账 1228 备注给的尺子是「按 `cards_truth` 的 `abilities[].trigger` + `hasActiveAbility` + 六档阵营表推 = **508 张 / 514 条**」。我用同一套 `.c` 逻辑在 Python 里复算，**三种口径都对不上**：

| 数据源 | 「派生但卡面本来没有」 |
|---|---|
| `cards_engine.json`（1129，用 `abilityTriggers` 列） | **426 张 / 432 条** |
| `cards_truth.json`（1351，用 `abilities[].trigger`） | **875 / 948** |
| `cards_engine ∩ cards_truth`（pool 内用 truth 的 abilities） | **752 / 825** |

⇒ 尺子的**分母与 `cardTraits` 口径**没复现（可能是 SO 原值 vs 运行时值的差别，或 `rage→penitence` 归一化不同）。**`.c` 逻辑本代理已逐句照抄**（25 对表、Goff/Sautekh 空表、hasActiveAbility 补充都在），差的是**断言尺子**。⇒ 建议 E3 断言线写这条时**先重算尺子**（⛔ 别直接拿 508/514 当期望）。

## 未落：其余 15 条（非它不可落点全在黑名单，逐一移交）

| 账行 | 方法 | 卡在哪（落点文件 → 归属） |
|---|---|---|
| 94 | `AiScripted.ExecuteAction` | 执行器已在 `CampaignScriptRunner`；宿主未接线 + **数据源本地不存在** ⇒ 零可观测，非数据侧 |
| 248 | `BattleManager.CheckForHeroDeckTransform` | 缺 `DeckRules.DeckMeetsCriteria`（新文件，非本白名单）+ 换卡组动作 + 3 调用点 |
| 455 | `PayCardPlayedCostOath` | `chosenAbility 360` 分派 + 战术卡 oath 付费/重放 → `EffectResolver`(E2) |
| 532 | `ResolveRemoveEffect` | 引擎输入已落地，仅差断言（`RuleEngine/Editor` = E3） |
| 573 | `SetNextCardAsDeckDrawing` | `inDeckDrawing` 呈现态 → `CardView`/`BattleDriver`（E5/E6） |
| 804 | `MinionPlayedIntoField` | 卡表 `summonDelayPrefab` + 表现层召唤消息出口（非引擎） |
| 835 | `ReactToUnitPoisoned` | 广播 290 + 监听端 → `RuleCore`(E1)，且池内 trigger290/poisoned 均 0 张 |
| 921 | `ShowCardBeingDrawn` | 结算那刻发表现事件 → `EffectResolver`/`RuleCore`(E1/E2) |
| 936 | `ToggleCombatState` | 消费者在 `CardHighlight`/`BattleDriver`（E5/E6） |
| 973 | `UndissolveToDeck` | 状态等价，无「停在半溶」视图；落点 `RuleCore` |
| 1037 | `EntityScript.GetThisCardPlayedDamage` | 预览取数 → `BattleDriver.BattleBoardPreview`（E5） |
| 1254 | `RawCardScript.IsCumulativeMutation` | 🔴 **数据侧阻塞**：`cards_truth.json` **无 `mutationType` 键**（真值只在 `cards_raw.json`）；要先扩 `gen_cards_truth.py` + `CardDatabase.Parse`（**CardDatabase.cs 不在本轮白名单**）⇒ 本代理只能记判据、无法把数据接进 `CardDef` |
| 1287 | `RawCardScript.TriggerEnchantment` | 两跳 `TriggerAbilities(0/500)` → `EffectResolver`(E2) |
| 1367 | `WarlordMasteryContainer.OnInitialize` | Shell 线（`ProfileTab`），非引擎 |
| (993) | `GetTraitForActiveAbilityRawCard` | **第一批已落** `CardDef.ActiveAbilityTrait`（三处表现层待接） |

> ⚠️ **`IsCumulativeMutation`（1254）是唯一「看起来能落在 CardDef 却落不了」的一条**：缺的是**数据源**（`mutationType` 没进 `cards_truth`），而把数据接进来必须动 `CardDatabase.cs`（本轮白名单外）。若后续放开 `CardDatabase.cs` 或 `gen_cards_truth.py`，可按「`CardDef` 加 `MutationTypes` 列 + `CardDatabase.Parse` 透传 + `IsCumulativeMutation` 静态判定（任一 buffEffect.mutationType == 20）」，判据已在 `decomp_full/RawCardScript__IsCumulativeMutation.c:17-47`。

---

# 第三批：`UnitState.ActiveAbilityHidden`（解账 783 的锁）

> 补丁 = `账/_parts/B88_E4c.tsv`（1 行）。白名单仅 `UnitState.cs` / `UnitStateEffectDisplay.cs` / 新 partial。

## 字段名 = `UnitState.ActiveAbilityHidden`

- **语义**：「**这一张实例**的主动能力不可用」（教程关闸）。默认 `false`。
- **为什么是实例位**：原版 `CardScript__HideWarlordActiveAbilityForTutorial.c:1-8` 写的是 `hasActiveAbility(+0x220)=0` —— 那是**单实例**（`CardScript`）。我们的 `CardDef.HasActiveAbility` 是**共享不可变**（全池同 id 共用，写它污染全池）。⇒ 实例位承载，字段 doc 已用 `🔶 表示法差异` 写明。
- **⛔ 不与 `ActiveAbilitySuppressed` 合并**：那个是 **jam** 语义（`ApplyJam` 对非督军置位），来源不同、将来要分开读。
- **配套只读合并视图**：`UnitState.ActiveAbilityAvailable` = `Card != null && Card.HasActiveAbility == 1 && !ActiveAbilityHidden`（方便 E1 一处判）。
- **读点（本批不动，交 E1）**：`RuleCore.CanStartActiveAbility`（`RuleCore.cs:15304` 现只读 `u.Card.HasActiveAbility`）→ 建议改成判 `u.ActiveAbilityAvailable`（或等价 `!u.ActiveAbilityHidden`）。
- **写点（本批不动）**：教程关闸那一跳（教程链 / `RuleCore`）。

## 验证

- `TMPDIR=/tmp/wf_s88e4c bash 工具/typecheck.sh`：**本代理文件 0 错**；当次运行时错误 **5 条全在 `CardPresentation/Net/NetSession.cs`（E8 在飞，`NetMatchmaking.InLobbyMatch` / `_wasInLobbyMatch`）+ 编辑器 `CS0006` 级联** ⇒ 属别人的文件、**非本代理**（⛔ 未碰）。

---

# 第四批：`VictoryCounters` 上游（DTO + 写入口）

> 补丁 = `账/_parts/B88_E4d.tsv`（2 行 · 域 `其他/基础设施`）。新建文件 = `CardPresentation/Shell/VictoryCountersStore.cs`（+ `.meta`）。⛔ 未碰 `ProfileTab.cs`（E10）。

## 4·1 DTO `VictoryCounter`（字段名/序逐字照原版）

```csharp
[Serializable] public class VictoryCounter {
    public int totalVictories;                         // 原版 +0x10
    public Dictionary<string,int> victoriesById;       // 原版 +0x18
    public string HighestCardId { get; }               // 原版只读属性（不是字段）
}
```
- `HighestCardId` = `victoriesById` 里**计数最大者的 key**（判据 = `VictoryCounter__get_HighestCardId.c` + lambda `b__3_0` 返回 `KeyValuePair+8` = value，`OrderByDescending(...).FirstOrDefault()`）；空表 ⇒ `null`；平手取枚举顺序第一个。
- ⛔ **没加原版没有的字段**（`HighestCardId` 原版就是 `{ get; }`，故我们也是计算属性、非序列化字段）。

## 4·2 写入口 `VictoryCountersStore`（落到 key 44）

- `Key = SaveKeys.VictoryCounters`（`SaveKeys.cs:193` 常量 / `:289` 枚举位 `// 44`，与原版 `PlayerDataKey` 第 44 项**同名同号**）。
- `Load()` / `Save(map)` / `RecordVictory(warlordCardId, defeatedCardIds, wins=1)`；`HasData()` 区分「没档」与「0 胜」。
- 🔴 **`JsonUtility` 不能序列化 `Dictionary<,>`**（会静默写 `{ }`）⇒ 落盘用 `[Serializable]` **展开壳**（`Blob/Group/Pair` 列表形态），对外仍是 `Dictionary<string,VictoryCounter>`。
- **旧档缺失** ⇒ `Load` **出声（`LogWarning`）+ 空表**；⛔ 不静默当「0 胜」（配 `HasData()` 分清）。
- 🔶 **`RecordVictory` 走本地累加**（原版是「服务端整份 `matchResult.VictoryCounters` **替换**」——`PlayerDataManager.ProcessMatchResult`(`cpp2il:9480`)）；本地无 PlayFab 下发（P3），只能累计，已在类头打 `🔶 自己设计`。

## 4·3 `victoriesById` 的 key 到底是什么（实读，不猜）

- **card id 字符串**（`AssetLocator` / `RawCardScript` 能认的 asset 名，形如 `UM47`），**⛔ 不是数字 uniqueId、也不是本地化卡名**。
- 证据：`WarlordMasteryContainer.OnInitialize`（`cpp2il/out_cpp2il_dev_ilspy/WarlordMasteryContainer.cs:120-230`）把 `highestGroup.Value.HighestCardId`（及战斗日志里的 hero 串）**直接喂** `AssetLocator.GetAsset<RawCardScript>(id)`，兜底还写字面量 `"UM47"`。
- ⚠️ **仍不可判定**：外层 key（属哪个督军）与 `victoriesById`（按什么卡分桶）的**语义分工**（自己督军 vs 击败的对手督军）——那份字典由**服务端**填、客户端只读，本地反编译读不出。⇒ 字段名照原版、语义注释写「未判定」，⛔ 不写死。

## 4·4 施工单（调用点，本批不接）

- **在哪调**：`RuleEngine/Core/RuleCore.cs:13885`（`RuleCore.CheckWinner` 定义；`ctx.Winner` 赋值在 `:13945` / `:13988` / `:14012`）判出**本地玩家获胜**那一处之后。
- **传什么**：`VictoryCountersStore.RecordVictory(本地督军卡 id, 本局击败的敌方卡 id 集合, wins: 1)`。
  本地督军卡 id = 本机玩家的 `p.Warlord.Card.Id`；击败集合 = 本局被击败的敌方卡 id（按 `victoriesById` 的「卡 id」口径）。
- ⚠️ `RuleCore.cs` 属 E1（本批黑名单）⇒ 只写施工单、⛔ 未加调用点。
- ⚠️ **不改** `ProcessMatchResult` 那一行（`其他_基础设施.tsv:452` = **D** 行，服务端 P3）。

## 4·5 验证

- `TMPDIR=/tmp/wf_s88e4d bash 工具/typecheck.sh`：**本代理新文件 0 错**；当次运行时错误 3 条全在别人在飞文件（`Shell/ItemDrawer.cs`(E8) `NodeName` · `Shell/ReferralPopupWindow.cs` `Apply` 实参 · `Battle/BattleDriver.cs`(E5) `VarsGlobal.PlayedCardDisplayOffsetPx`）+ 编辑器 `CS0006` 级联 ⇒ **非本代理**。
- `check_patch_conflicts.py`：B88_E4d 无重复键（两条都是**新行**，`get_HighestCardId` / `set_victoryCounters` 全账 0 命中）。
