# G1b · `StatusApplied` 重播 + `ASH80` 的 610（执行报告 · 2026-10-24）

> 执行代理 **G1b**（第二批）。判据来源 = `资料/普查产出_1024/C2_战斗线判据总表.md` §2 / §6
> （第一入口）+ `d:/2/tools/decomp_full/`（第一权威）+ `d:/2/新解包资源/assets_full/`。
> 未跑 Unity（红线）· 未动 git · 未改 `项目任务.md` / `CLAUDE.md`。
> ✅ `TMPDIR=/tmp/wf_g1b bash 工具/typecheck.sh` **两轮全绿**（运行时 0 / 编辑器 0）。

---

## 〇 · 一句话结论

| 件 | 状态 | 一句话 |
|---|---|---|
| **1 · D8 `ASH80` 的 610** | ✅ 做完 | `TriggerNameOfWhenKind` 补 `GainSpirit → OnCollectSpiritStone`，`VfxMap` 三处同批（正向 `case` / 反向闸删项 / 两张正向锁）；**4 张 / 5 条**能播，断言全覆盖 |
| **2 · `DisplayStatusAnim` 重施加** | ✅ 做完 | 新增 `EvtKind.StatusApplied`（`CardId` = 关键词），5 个 `AddKeyword` 语义点发；`PlaySignal` 收到 ⇒ `TraitFrames.DisplayStatus`；`EventTiming` 显式登记为零时长 |
| 指纹 | ⚠️ **推断不需要重钉** | 两件都**不改任何局面**（只多事件、不写状态）；理由见 §四。建议主对话跑一次 `ruleprobe play check` 复核 |

---

## 一 · 逐条判据（我照它落地的，逐条给出处）

### 1·1 D8 · `ASH80 Farseer` 的 610（C2 §2）

| 事实 | 出处 |
|---|---|
| 收集路标石：先 `AddSpiritStoneMana(+1)` → `BroadcastCollectedSpiritStone` → 再销毁残骸 | `BattleManager._ResolveUseWaystone_d__480__MoveNext.c:129-131` |
| 广播：**先**对收集者那张卡 `ReactToCollectSpiritStone`，**再遍历全场在场卡**、跳过收集者 | `BattleManagerSupport__BroadcastCollectSpiritStone.c:22-38` |
| 接收：`OnTrigger(0x262 = 610, thisCard, thisCard, thisCard)` | `CardScript__ReactToCollectSpiritStone.c:5-9` |
| 610 的动画仍是 ability 的一步（`OnTrigger` → `TriggerAbilities` → `PlayAbility` → `PlayAnim`） | `CardAbility__CanTriggerAbility.c:126-140` |
| 我们的事件源**唯一**：`BroadcastWhen(WhenEventKind.GainSpirit, …)` | `RuleEngine/Core/RuleCore.cs:10673`（`CollectWaystoneCore`） |
| 断点：`TriggerNameOfWhenKind` 的 `switch` **没有 `GainSpirit` 分支** ⇒ `tn == null` ⇒ `EmitListenerTrigger` 直接 return | 改前 `EffectResolver.cs` 的 `switch`（`Play/Deploy/Die/Attack/Damaged/Draw/Reanimated/GetsDarkPact/GetsHuntMark/GainFaith/Prays/SpiritAbility`） |
| 候选（`cards_engine.json` 现读，判据 = `animTimes[].tn == "OnCollectSpiritStone"`） | **4 张 / 5 行**：`ASH80` 1（`PathOfTheSeer`）· `ASH28` 1（`PathOfTheSeerGreen`）· `ASH78` 2（`Buff_Purple` + `Buff_Eldar_Warp Spider`）· `ASH25` 1（`BulletImpact_Shuriken_Short_twinlink`），**全 SaimHann**，四张 `abilityTriggers` 都含 610 |
| `CrossTriggerDrops` **不用动**：只在 `evt == Ability` 时跑 | `VfxMap.cs:1618`：`bool[] drop = (!perCardOnly && evt == Ability) ? CrossTriggerDrops(...) : null;` |

**我照 C2 §2·4 的 4 步做，一步不多一步不少。** 那个「prefab 锚点是不是 self」的尾巴也顺手核了：
`PathOfTheSeer_Self` 的 `o = "1.0.0"`（`startPosOption/screenSpace` 族），走 `EvtKind.Trigger` →
`FireEventAll` 的格位点播法 —— 与 `PathOfTheSeer` 这一族的**其余三张同形**（都 `o = 0.0.0/0.0.1`），
**不另开技能动画口**（那是 D3 的坑，610 不在攻击演出链上）。⚠️ 这一条仍**没有运行时比对**，
只能由主对话的真机/截图验收（见 §五）。

### 1·2 `DisplayStatusAnim` 同关键词再次施加（C2 §6）

| 事实 | 出处 |
|---|---|
| `DisplayStatusAnim(ui, traitId)` **没有去重闸**：`TriggerType == 0 (Start)` ⇒ 直接 `GetTraitEffect` → `ResolveTraitAnim` → `StartCoroutine`（不进表、不查重） | `BattleCardUI__DisplayStatusAnim.c:78-119` |
| `TriggerType == 2 (OnPlay)` ⇒ 字典里已有 ⇒ **只归位 + `SetActive(true)`、不重播** | `…:121-198` |
| 调用点（5 处）：`CardScript__Stun.c:125` · `CardScript__AddEffect.c:678/:890` · `ActivateTraitsOnSummonOrEnchantment.c:67` · `ActivateTraitAnim.c:25` · `CardSetup.c:193` | 各 `.c` 现读 |
| 数据侧：**`stun` / `blind` / `stealth` / `shield` / `pindown` 只有 OnPlay(2)**；**13 个 trait 有 Start(0)**（`armour/cantAttack/companion/fast/ferocity/firstStrike/flank/huntMark/markerlight/pack/piercing/rally/tide`） | `Resources/trait_effect_bindings.json` 现读（75 行 / 62 trait；`huntMark` 的 Start(0) = `HuntMark_StackEffect`） |
| 我们现状：唯一入口 = `TraitFrames.SyncKeywords` 的**集合对差**（`if (_prev.Contains(k)) continue;`） | `Core/TraitFrames.cs:745-760` |
| `TraitFrames.DisplayStatus` 本体**已经与原版逐档等价**（Start 每次都播 / OnPlay 去重） | `Core/TraitFrames.cs:779-812`、`PlayStartRows :820-848` |

⇒ 缺的**只有「谁来调」**。`TraitFrames.cs` 本批**一个字没改**（C2 §6·3 的判断成立）。

---

## 二 · 改动清单（逐条 · 带现读行号）

### 2·1 `RuleEngine/Core/BattleEvent.cs`（**⚠️ 白名单外的一处必要改动**）

> 🔴 **为什么动了它**：`EvtKind` 枚举**全仓只在这里定义**（`BattleEvent.cs:25`），
> 而任务简报明确要求「引擎新增 `EvtKind.StatusApplied`」。这是**追加一个枚举值**（该文件自己写着
> 「枚举值只许追加」），`git status` 显示本文件**没有别的代理在改**（改动前是 clean 的）。
> 已按铁律 5 在报告里标出来 —— 若主对话认为越界，回滚这一处即可（其余改动会编不过，需一起回滚）。

| 行 | 改动 |
|---|---|
| `:367` | 枚举**末尾**追加 `StatusApplied`（含 doc：`CardId` = 关键词 · `Amount` 语义 · 判据三条出处） |

**`Amount` 的约定（本批的防双播设计，⛔ 别改）**：
- `0` = 这次是**首次获得** ⇒ 表现层**不**由本事件演（既有的 `SyncKeywords`「新出现」路演）；
- `> 0` = 施加**之前就已经在身上**（重施加）⇒ 只有这一档由本事件演。

⇒ 两条路**结构上互斥** ⇒ 与 `PlaySignals` / `SyncBoard` 谁先跑无关，**一次施加恰好演一次**。
（为什么必须这么分：`PlaySignal` 与 `SyncBoard` 的先后**不固定** —— 同一批的第一条事件 `at == _clock`
会在 `PlaySignals` 里**当场**播、早于 `SyncBoard`；后面的排在队尾、**晚于** `SyncBoard`。
両档都演的话，无论哪种先后都会有一边多播一次。）

### 2·2 `RuleEngine/Core/EffectResolver.cs`

| 行 | 改动 |
|---|---|
| `:6473` | 新增常量 `TN_OnCollectSpiritStone = "OnCollectSpiritStone"; // 610`（**不是** `KeywordTable` 常量 —— 它是原版 `AbilityTrigger` 名，同 `TN_OnCollectFaith` 的形状；注释里写清判据三条 + 候选规模 + 安全边界） |
| `:6553` | `TriggerNameOfWhenKind` 的 `switch` 新增 `case WhenEventKind.GainSpirit: return TN_OnCollectSpiritStone;` |
| `:6756` | 新增 `static void EmitStatusApplied(BattleContext ctx, UnitState u, string keyword, bool reapply)` —— `FindSlot` 成功才发；`ctx.Emit(EvtKind.StatusApplied, p, slot, keyword, amount: reapply?1:0, instanceId: u.InstanceId)` |
| `:3677` | `DoStun`：`AddKeyword(Stun,1)` 之后 `reapply: false`（该支已被 `if (t.IsStunned) continue;` 把着 ⇒ 一定首次） |
| `:3735` | `DoBlind`：施加前取 `hadBlind = t.KwValue(Blind) > 0`，施加后 `EmitStatusApplied(..., hadBlind)` |
| `:10047` | `ApplyOneGain` 的关键词支：施加前取 `hadKw = u.KwValue(p.Keyword) > 0`；`if (val > 0)` 才发（`+0` 是空给，同既有广播那一族的口径） |
| `:10127` | `ApplyOneGain` 的 `armour` **属性**增量支：`delta > 0` 时 `EmitStatusApplied(..., before > 0)` |

**挑的「最小集」= 5 个语义点**（10 处 `AddKeyword` 里）：
`DoStun` · `DoBlind` · `ApplyOneGain(关键词)` · `ApplyOneGain(armour 增量)` · `RuleCore` 的震荡支。
**没接的 5 处及理由**（⛔ 不是「影响小不做」，是**原版没有对应框**或缺 `ctx`）：

| 没接的 | 理由 |
|---|---|
| `GrantEmbeddedCore`（`EffectResolver.cs:9438` 一带） | 那是**触发关键词**（`Give "Slay: …"`），`slay` 在 `trait_effect_bindings.json` 里**没有任何 TraitEffect 行** ⇒ `DisplayStatus` 恒 no-op（发了也是空转）。 |
| `GrantDarkPact` 的 `AddKeyword(DarkPact)`（`:10230`） | `darkpact` **不在** 62 个 trait 里（契约给的是 `camouflage/regeneration/…`，那几条走 `ApplyOneGain` 已被覆盖）⇒ 空转。 |
| `RuleCore` 的 `DropPod`（`:7724` / `:7781`） | `dropPod` **不在**绑定表里 ⇒ 空转。C2 提到的 `CardScript__CardSetup.c:193`（`dropPod 230`）那一处在我们的数据里**没有对应的 `TraitEffect`**。 |
| `UnitState.AddKeyword` 本体 | **没有 `ctx`**（C2 §6·4 已点名）⇒ 不可能在这里发；而 `ApplyGrant`/`AddAuraKeyword` 那一族**故意不发**（光环不是「施加一次」的语义，见下）。 |
| 光环（`Aura`） | 不经过 `ApplyOneGain`（现核 3 个调用点：`DoGive` 目标循环 / `GrantDarkPact` / 本方法自身）⇒ **不会因为每次 `Recompose` 刷一串事件**。 |

### 2·3 `RuleEngine/Core/RuleCore.cs`

| 行 | 改动 |
|---|---|
| `:6439` | 震荡（`StunBlockedByTraits` 的 else 支、`!stunVictim.IsStunned` 里）`AddKeyword(Stun,1)` 之后 `reapply: false` |

### 2·4 `CardPresentation/Core/EventTiming.cs`

| 行 | 改动 |
|---|---|
| `:246` | `ZeroDurationKinds` 加 `EvtKind.StatusApplied`（原版 `DisplayStatusAnim` 当场 `StartCoroutine`、不 `yield`）⇒ `IsRegisteredEventKind` / `IsSourced` / `SourceOf` / `DurationOf` / `DelayBetween` 五处一起对上 |

### 2·5 `CardPresentation/Battle/BattleDriver.cs`

| 行 | 改动 |
|---|---|
| `:18934` | `PlaySignal` **最前段**新增 `if (e.Kind == EvtKind.StatusApplied) { if (e.Amount > 0) { var v = ViewAt(e.Player,e.Slot); var tf = v?.GetComponent<TraitFrames>(); if (tf != null) tf.DisplayStatus(e.CardId); } return; }` |

⚠️ 用的是 `GetComponent<TraitFrames>()` **不是** `TraitFrames.Attach(v)` —— `Attach` 会挂一个
`_prev` **为空**的新实例 ⇒ 下一次 `SyncKeywords` 会把**所有**关键词的 OnPlay 重演一遍（静默多播）。

### 2·6 `CardPresentation/Core/VfxMap.cs`

| 行 | 改动 |
|---|---|
| `:2430` | `IsRoutedTriggerName` 加 `case "oncollectspiritstone":`（小写；同 520/665 那一块） |
| `:2874` | `NoEmitTriggerNames` **删掉** `"OnCollectSpiritStone"`（留痕说明它当初为什么在） |
| `:2898` | `MustRouteTriggerNames` 加 `"OnCollectSpiritStone"`（排在 `OnCollectFaith` 后） |
| `:2928` | `MustRouteTriggerKeywords` 加 `"OnCollectSpiritStone"`（PascalCase —— `EmitCardTrigger` 发的是原版枚举名） |
| `:2344` / `:2840` / `:3007` | 三处**就地订正**的 doc（铁律 5）：原来写「这四个名字不许加」的清单里去掉 610；`NoEmit` 的 bullet 改成留痕；`SelfCheckRoutedTriggers` 的规模表补 `4 张/5 行` |

### 2·7 `CardPresentation/Core/TraitFrames.cs`

**没改。** `DisplayStatus` / `PlayStartRows` / `StartPlayCountOf` 都已存在且与原版逐档等价（C2 §6·3 的判断成立）。

### 2·8 测试

| 文件 | 改动 |
|---|---|
| **新建** `RuleEngine/Editor/RuleEngineTest_G1b.cs`（+ `.meta`，guid `73fbe0e652b042fe9f6a7bcac8b834f1`） | `TestG1bSpiritStone`（`:68`）· `TestG1bStatusApplied`（`:223`） |
| `RuleEngine/Editor/RuleEngineTest.cs` `:993-994` | `Step(TestG1bSpiritStone);` / `Step(TestG1bStatusApplied);` |
| `RuleEngine/Editor/RuleEngineTest_Diag.cs` `:91` / `:550-551` | `RunCaseNames` 与 `RunSafe()` **同位置追加**这两个名字（`VerifyDiagCoverage` ① 比的是**顺序**，两处必须同索引） |
| `RuleEngine/Editor/RuleEngineTest.cs` `:15531` | **`EvtKind` 计数护栏 18 → 19**（那一条自己写着「加新事件时同步改这一条」；不加会**必红**） |
| `CardPresentation/Editor/BattleScene.cs` `:8788-8860` | 新增 §⑧：`StatusApplied` 重施加 ⇒ `Start(0)` 重播（走 `Signals → RefreshAll → PlaySignals → PlaySignal` 真链） |
| `CardPresentation/Editor/BattleScene.cs` `:1614` / `:1619` | 「18 档全部登记」→ **19 档**（就地订正） |

---

## 三 · 断言逐条 + 灭自证

### 3·1 `RuleEngineTest.TestG1bSpiritStone`（引擎 + `VfxMap`）

| # | 断言 | 🧨 怎么才会红 |
|---|---|---|
| 前提 | `ASH80/28/78/25` 在池里；四张 `AbilityTriggerPresence("OnCollectSpiritStone") == 1`；`ASH80.HasAnimTrigger(...)` | 数据侧丢了 610 |
| ① | 真入口 `RuleCore.CollectWaystone` ⇒ `FindSignal(Trigger,"OnCollectSpiritStone") != null` | 删掉 `TriggerNameOfWhenKind` 的 `GainSpirit` 支 ⇒ null（**改前正是这一态**） |
| ① | 每张 **恰 1 条**（`ASH78` 也是 1，尽管它 610 有 2 行动画） | 按动画行数发事件 ⇒ `ASH78` 变 2 |
| ① | 全场合计 **4 条** | 漏一张 / 整场广播到没有 `WhenTriggers` 的卡 ⇒ 变 5+ |
| ② | 事件带格位（`Player=0 / Slot=1` = `ASH80` 自己那一格） | `Slot` 写成 -1 ⇒ 表现层不播 |
| ③ | **反向**：`Gain 3 ☀`（信仰）⇒ `OnCollectSpiritStone` **0 条** | 把映射挂到 `GainFaith` / 挂成「任何资源变化」⇒ 红 |
| ④ | 件级：`ASH80` **1 件 `PathOfTheSeer`** · `ASH28` **1 件** · `ASH78` **2 件** · `ASH25` **1 件** | 不放行 ⇒ 掉回 `ability` 粗桶（`ASH80` 变 3 件，`WaystoneTrigger`/`Waystone UI Spend` 是错件） |
| ⑤ | `VfxMap.SelfCheckRoutedTriggers()` = **0 违规**（名字级 + 件级 + 两张正向锁同长） | 只补映射不删反向闸 / 只加名字不加关键词 ⇒ 至少一条红 |

### 3·2 `RuleEngineTest.TestG1bStatusApplied`（引擎）

| # | 断言 | 🧨 怎么才会红 |
|---|---|---|
| ① `huntmark` 第 1 次 | **1 条** `StatusApplied`，`Amount = 0` | `Amount` 写成恒 1 ⇒ 首次会被演两遍 |
| ① 反向 | `Amount = 1` 的条数 **0** | `reapply` 判据恒真 |
| ② `huntmark` 第 2 次 | **1 条** `Amount = 1` | **删掉引擎那一句 emit ⇒ 0（改前正是这一态：第二次以后一次都不演）** |
| ② 反向 | `Amount = 0` 的条数 **0** | `reapply` 恒假（表现层永远不重播） |
| ③ 载荷 | `Player=0 / Slot=3`（= 被施加那张卡）· `CardId = "huntmark"` | `CardId` 写成卡名 / `Slot` 取成施放者 ⇒ 表现层 `ViewAt` 找不到 |
| ④ 反向 | 玩家资源（信仰）⇒ `StatusApplied` **0 条** | 把发射口挂到 `DoGain` 那一层 ⇒ 红 |
| ⑤ `stun` | 施加 ⇒ 1 条 `Amount=0`、`Amount=1` 档 0 条；**并明写「⛔ 不拿它验重播」** | 漏掉 `DoStun` 那一处 ⇒ 0 |

### 3·3 `BattleScene.Run` §⑧（表现层 · **端到端**）

夹具 = 状态框那一节那张 `Tide Minion`，先 `AddKeyword("huntmark",1)` + `RefreshAll`（`SyncKeywords` 那条路演一次
⇒ 基线 `h0 >= 1`），然后逐条往 `Ctx.Signals` 塞 `StatusApplied`、走 `RefreshAll` + `StepThrough`：

| # | 断言 | 🧨 怎么才会红 |
|---|---|---|
| ① | 重施加 1 次 ⇒ `StartPlayCountOf("huntmark")` = `h0 + 1`，且 `PlayCount` **不变** | 不接 `StatusApplied` ⇒ 恒 `h0`；把 Start 塞进 `_play` ⇒ 第二次被去重挡住 |
| ② | **同一批连续 2 次** ⇒ `h0 + 3`（再演两次） | 事件层去重（只演首次）⇒ 少一次；按 `RefreshAll` 次数演 ⇒ 少一次 |
| ③ **灭自证** | `Amount = 0`（首次）⇒ **不演**（计数不变） | 两档都演 ⇒ 首次会演两遍（本行红） |
| ④ | `EventTiming.IsRegisteredEventKind(StatusApplied)` · `DurationOf == 0f` | 漏登记 ⇒ `IsSourced` 回 false、`unsourced == 2` 那条变红 + 每次会话出声 |

### 3·4 断言为什么**不会**自证

- 期望值全是**字面量**（`4` / `1` / `2` / `"OnCollectSpiritStone"` / `"PathOfTheSeer"` / `"huntmark"`）；
  候选规模从 `cards_engine.json` 现算（`4 张/5 行`），**不从 `VfxMap` 读回**。
- 每件都有**结构上不可能同时满足**的反例：`Amount` 两档互斥（3·2 ①② / 3·3 ③）·
  信仰不发 610（3·1 ③）· 资源变化不发 `StatusApplied`（3·2 ④）· `ASH78` 的「2 行动画 / 1 条事件」。
- 表现层那三条走的是**生产链**（`Ctx.Signals → RefreshAll → PlaySignals → PlaySignal`），
  不是直调 `TraitFrames.DisplayStatus`（那样只能证明「函数能跑」，证明不了接线）。

---

## 四 · 哪些读数会变 / 需不需要重钉指纹

| 读数 | 预期变化 | 说明 |
|---|---|---|
| `RuleEngineTest.Run` | **7393 → 7393 + 本批新增断言数**（数 `✓` 行；`RuleEngineTest_G1b.cs` 里共 **46 处** `Check`/`CheckTrue`/`CheckCode` —— 其中一部分在 `if` 里、不执行，**以实跑为准**） | 新增 2 个用例方法（`TestG1bSpiritStone` / `TestG1bStatusApplied`），已在 `Run()` / `RunSafe()` / `RunCaseNames` 三处登记 |
| `BattleScene.Run` | **4805/0 → 4805/0 + 本批 §⑧ 的 11 处 `Check`**（3 处是前提） | 只加断言，不改既有断言 |
| `EvtKind` 档数 | 18 → **19** | `RuleEngineTest.cs:15531` 的护栏已同步改 |
| `unsourced` | 仍 **2** | `StatusApplied` 登记为零时长 ⇒ `IsSourced` = true |
| **`PinnedParseMd5`** | **不变** | 解析层一个字节没动（`EffectText` / `GivePayload` / `CardDef` 全没碰） |
| **`PinnedPlayMd5` / `PinnedBaselineMd5`** | **推断不变**（⚠️ 建议主对话跑一次 `ruleprobe play check` 复核） | 理由三条：① `StatusApplied` **只进 `Signals`**，不进 `ActionLog` 的判据、不进 `DeepHash`（哈希只读棋盘/牌库/资源）；② `EmitStatusApplied` **不写任何状态**；③ 610 的新映射只在**监听器已经结算完**之后多发一条 `EvtKind.Trigger`，**不触发任何新效果**（`EmitListenerTrigger` 的语义就是「这件事发生了」的信号）。⇒ 每张卡的 `Raw`/`Ctrl`/`Net` 三列读数**逐字不变** |
| `ruleprobe` 的 `E|`（事件电池）行 | 同上，不变 | 电池量的是「局面动没动」，不是「发了几条事件」 |

⚠️ **我没有跑 `ruleprobe`**（它在 `工具/**`，本批红线是不越白名单；跑它也不该由执行代理决定）。
⇒ 上面这一格是**推断 + 理由**，不是实测。请主对话在同步点跑 `play check` 收口。

---

## 五 · 没查清 / 没做

1. ⚠️ **610 的落点没有运行时验收**（`PathOfTheSeer` 那件的 `startPos/endPos` 是否与 `FireEventAll` 的
   格位点播法语义一致）。**判据：不能静态判定**（原版那件是 `AnimFX` 模块，锚点参数在 prefab 的
   `CardAnimController` 里，而 `EvtKind.Trigger` 通道**不吃** `startPosOption/endPosOption`）。
   与 D3 不同：610 **不在**攻击演出链上（原版 `ReactToCollectSpiritStone` 是独立协程）⇒ 「走 `Trigger` 通道」
   本身就与 D3 那条判据一致。**要不要改成技能动画口 = 需要一次真机/截图比对**（主对话）。
2. ⚠️ **`stun` 的「再晕」**：`DoStun` 里有 `if (t.IsStunned) continue;` ⇒ 我们的再晕**连事件都不发**。
   原版 `CardScript__Stun.c:125` 每次都调 `DisplayStatusAnim`，但 `stun` **没有 Start(0) 行**，
   OnPlay 那一档「已有 ⇒ 只归位 + `SetActive(true)`」；而我们 `CleanStatusAnims` 的谓词是
   「卡上**已经不再持有**的 trait」⇒ **还挂着的 stun 框永远不会被清** ⇒ 那次「重新激活」**没有可观测差异**。
   ⇒ 本批**不做**（不是「影响小」，是「结构上观察不到」）。**若将来出现「保留关键词却清框」的路径，必须回来补。**
3. ⚠️ **`armour` 属性增量那一支的断言**：代码接了（`EffectResolver.cs:10127`），但**没有写断言**
   —— 池内**没有一张卡**用 `+N Armour` 的属性写法（`cards_engine.json` 现扫 0 处），
   自造一张 `Tactic(..., "Give +1 Armour to a friendly troop")` 会引入「词表解析对不对」这个
   **与本批无关的变量**（`armour` 在 `GivePayload` 里既是关键词（`:144`）也匹配属性正则（`:213`），
   走哪一支我没在运行时核过）。⇒ 如果主对话要验它，建议另开一条（先核 `GivePayload` 的分支优先序）。
4. ⚠️ **`Initiative` / 真机截图**：本批**没有**任何真机或像素验收（红线：不跑 Unity）。

---

## 六 · 顺手发现（⛔ 我只报、没顺手改）

1. 🔴 **`EffectResolver.cs` 有并发写手，我撞到过一次真实的坏行**：本批开始时该文件里
   `EmitListenerTrigger` 尾部出现**两行**相邻的
   `if (!EmitCardTrigger(...)) return;` + `if (!EmitCardTrigger(...)) return false;`（后者在 `void`
   方法里 ⇒ `CS0127`，**整棵运行时程序集编不过**）。`git show HEAD:` 那一行是 `return;`（没有 `false`）
   ⇒ 我删掉了那行**多余的 `return false;`**（净效果 = 回到 HEAD 的写法）。
   **建议**：如果是别的代理正在把 `EmitListenerTrigger` 改成返回 `bool`，请把 `return;` 那几处
   **一起**改（现在是不一致的中间态）。
2. ℹ️ **`VfxMap.IsCollectTrigger`（`:1900-1904`）早就把 610 归成「收集类」**（`tg == 610`），
   它只服务于 `CrossTriggerDrops`（只在 `evt == Ability` 跑）⇒ 与本次放行**不冲突**，两处判据一致。
3. ℹ️ **`RuleEngineTest.cs:15531` 那条护栏写着「加新事件时同步改这一条」** —— 这次正踩上，
   已改成 19。以后加 `EvtKind` 的人会再遇到同一处（这是**设计如此**的护栏，别删）。
4. ℹ️ **`TestG1bStatusApplied` 的 `stun` 反向断言**（「首次眩晕不许报成重施加」）在**当前实现下是恒真的**
   （`DoStun` 的 `!IsStunned` 守卫决定了永远是 `Amount = 0`）；它钉的是**将来**别把守卫绕过去。
5. ℹ️ **`RuleEngineTest_Diag.cs` 的 `RunCaseNames` / `RunSafe()` 是「生成物」**（文件里写着
   「⛔ 别手改，用 `工具/gen_diag_runner.py` 重生成」）。本批**因为 `工具/**` 不在白名单**，是**手工**
   在**同一个索引**追加的两个名字（顺序与 `Run()` 一致）。**建议主对话之后跑一次
   `PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`** 让产物与生成器真正对齐
   （现在两处**值一致**，只是不是生成器写的）。

---

## 七 · 红线自查

- ⛔ **没跑 Unity**（`-executeMethod` 一次都没有）；✅ 只跑了两轮 `工具/typecheck.sh`（均带独立 `TMPDIR=/tmp/wf_g1b`）。
- ⛔ **没动 git**（无 `add/commit/checkout/stash/reset`）。
- ⛔ **没改** `CLAUDE.md` / `项目任务.md` / 各线正本。
- ⛔ **没越白名单** —— 唯一例外是 `RuleEngine/Core/BattleEvent.cs`（§2·1，已单列理由）。
- ⛔ **没改** `核心正本` / `Shell/**` / `Net/**` / `工具/**` / `数据/**` / `Core/CardView.cs` /
  `Editor/CardBaseDemo.cs` / `Editor/CardFaceProbe.cs` / `AnimFXCheck.cs`。
- ✅ 行尾：CRLF 文件（`EffectResolver` / `BattleDriver` / `BattleScene` / `RuleEngineTest`）改完**仍是 CRLF**；
  LF 文件（`BattleEvent` / `RuleCore` / `VfxMap` / `EventTiming` / `RuleEngineTest_Diag`）**仍是 LF**；
  新建的 `RuleEngineTest_G1b.cs` / `.meta` 是 LF（与同目录 `RuleEngineTest_Engine*.cs` 一致）。用 `wb` 逐字节核过。
