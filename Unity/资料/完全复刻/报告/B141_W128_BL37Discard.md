# B141_W58 · CHK-05 `L130`「BL37 的弃牌代价」——反证报告（⛔ 无实现）

- 批次 / 会话：`B141` / 第一百二十八会话（W128）
- 任务行：`报告/B141_CHK05_分域与首批定性.md:45`（`L130`）＋ `账/战斗逻辑驱动.tsv:130`
- 结论档位：**⛔ 判据本身错 / 前提不成立**（不是「🟠真没做」）

---

## 〇、一句话结论

**池内 BL37（Abaddon's Chosen）没有「弃牌代价」。**
全库（`cards_truth.json` 1,351 张 SO）唯一带 `chosenAbility 160 (discardFromHand)` 的，是一张
**教程起手占位卡** `T1 BlackLegion-Abaddons Chosen_startinghand`（`uniqueId f3df0cfc…`，`pathID -189149361816600990`）。
它的语义是「**在手牌里、回合开始时，先弃掉它自己，再 `createCardInHand` 造出真 `BL37`**」——
占位卡把自己换成真卡，**不是玩家付的费用**。
按 `L130` 的描述去实现「BL37 弃一张手牌」= **造出原版没有的行为**，故按施工单「判据不足 / 会造假 ⇒ 停下写反证」停手。

---

## 一、原版语义（逐条判据，全部现读）

### 1. `160 = discardFromHand`（枚举本体）

| 值 | 名字 | 出处 |
|---|---|---|
| 100 | `createCardInHand` | `d:/2/tools/il2cpp_out/dump.cs:20669` |
| **160** | **`discardFromHand`** | `dump.cs:20677` |
| 162 | `disappearFromHand` | `dump.cs:20678` |
| 165 | `discardWholeHand` | `dump.cs:20679` |

### 2. `AbilityLogic.PlayAbility` 里 160 那一支怎么走（把现有注释的出处逐行核了一遍）

| 行 | 内容 | 含义 |
|---|---|---|
| `AbilityLogic__PlayAbility.c:286` | `iVar8 = *(int *)(param_1 + 0x10)` | 取 `AbilityLogic.chosenAbility`（`dump.cs:19456`，`// 0x10`） |
| `:257` | `uVar12 = *(undefined8 *)(param_1 + 0x28)` | 取 `targetCriteria`（`dump.cs:19461`，`// 0x28`） |
| `:262` | `AbilityLogic__GetTargets(param_2, uVar12, param_3, 1, 0)` | 按那 36 格筛目标 |
| `:263` | `puVar1 = (param_1 + 0x78)` | 结果存进 `possibleTargets`（`dump.cs:19481`，`// 0x78`） |
| **`:588-603`** | `else if (iVar8 == 0xa0)` → `while` 遍历 `possibleTargets` → 逐个 `BattleManager__AddDiscardFromHand(param_2, 目标, abilityData+0x44 优先级, abilityData+0x48 blockPlayerActions, 0)` | **160 的全部语义就在这里** |

⇒ 160 = 「**把 `targetCriteria` 筛出来的每一个目标从手牌丢掉**」，**没有别的分支**。
结算体 = 压一条 `BattleAction(0x1c = 28 discardFromHand)`（`BattleManager__AddDiscardFromHand.c:26`；
与 `disappearFromHand(0x38)` 的区别 = 去向：**弃牌堆** vs 移出游戏 —— 账 `:128` / `:130` 已经分别记过）。

### 3. `ta = 20 (self)` 是「能力所在的那张卡自己」，不是「玩家/施放者」

- `AbilityLogic__GetTargets.c:184-192`：`if (iVar6 == 0x14)` ⇒ 清空池子、把 `param_3`（五参重载的 `thisCard`）放进去
- 枚举 `TargetsAffected.self = 20`（`dump.cs:20783`）

### 4. 唯一带 160 的那张 SO 到底是什么（现读 `cards_raw.json`，2026 build allcards · 1,351 条）

| 字段 | 值 |
|---|---|
| `path_id` | `-189149361816600990` |
| `m_Name` | **`T1 BlackLegion-Abaddons Chosen_startinghand`** |
| `uniqueId` | `f3df0cfc57695654cb31a72b65470522` |
| `cardName` / `cardArmy` / `manaCost` / `cardType` | `Abaddon's Chosen` / `50` / `1` / `20`(tactic) |
| `cardAbilities[0]` | `trigger` **30 (TurnStart)** · `abilityContext` **10 (Hand)** · `priority` 2 |
| `abilityLogic[0]` | `chosenAbility` **160** · `playersAffected` 10 · `targetCriteria.targetsAffected` **20 (self)**（`summonCriteria` 那格也填着 BL37，但 **160 那一支根本不读它**） |
| `abilityLogic[1]` | `chosenAbility` **100 (createCardInHand)** · `unitsToSummon` = `pathID -2160489607866505058` = **BL37 本体** |

⇒ **净效果**：这张占位卡在手牌里等到回合开始 → **弃掉自己** → **造一张真 BL37 进手牌**。
它是「教程起手替身」，用完即弃（会进弃牌堆）。

### 5. 池内的 BL37 本体没有 160

- `cards_raw` 里 `uniqueId == BL37`（`pathID -2160489607866505058`）：`trigger 0 / ctx 0`，
  `chosenAbility 30 (buffOthers)`＋`buffType 16`（Dark Pact）＋`targetCriteria { ta: 30, selectFromChooseResolution: 1 }`
- `cards_truth.json` 的同名记录一致；`cards_engine.json`（可玩池 1,142 张）现扫：
  **`chosenAbility` 含 160 = 0 张 · 含 165 = 0 张**。

### 6. 「`choicesDefinition` 只挑种类、不弃牌」是**对的**，不是缺陷

BL37 的 `unitConditions[0].role = "choice"`，`choiceSet` 指定 4 张 `filterSpecificCards`；
这 4 张在 SO 里是 `Z-NC BlackLegion-Mark of {Khorne,Nurgle,Slaanesh,Tzeentch}_enchantment`。
官方本地化 `数据/游戏数据/官方本地化_en+zh-CN.json` 里 **两套名字都在**
（`Dark Pact of Blood/Excess/Fate/Resilience` 与 `Mark of Khorne/Nurgle/Slaanesh/Tzeentch`）
⇒ 我们卡表叫「Dark Pact of Excess」**不是错名**（先记一句，免得后来人再查一遍）。

---

## 二、我们的引擎现状（已有什么 / 缺什么 / 缺的那部分属于谁）

1. **160/165 的执行体 + 分派早就落地**（`B97_W5`，2026-10-29）：
   - `RuleEngine/Core/B97W5_RowsAndDedupe.cs:80` `RunDiscardFromHandRow` · `:113` `RunDiscardWholeHandRow`
   - `RuleEngine/Core/EffectResolver.cs:18780` 的 `if (ca == 160) …` / `if (ca == 165) …`
   - 计划表闸 `RuleEngine/Core/RuleCore.cs:17473`（`ActiveRowEntersPlan` 含 `ca == 160 || ca == 165`）
   ⇒ 账 `:130` 判 **A** 的那一半（动作槽有真落点）**成立**；本笔**没有**要补的动作槽。
2. **缺的是另一条路**：占位卡那一支走 `cardAbilities` 行 + `trigger 30` + `abilityContext 10 (Hand)`，
   归**手牌触发路**（`EffectResolver.FireHandAbilityTrigger`，闸 = `CardAbility.IsRightContext` 只放 Hand）。
   那条路的正文取法是 **文本 op**（`TriggerCarrier.FxOps`），**不按 `chosenAbility` 数字分派** ⇒
   若数据里真出现这张占位卡，我们这边**什么都不做、也不出声**。这是**真缺口**，但它属于
   「**ability 行级 ca 派发**」那一族（`B-211` 目前只认 320/325/360/380/480），**不是 `L130` 说的 BL37 弃牌**。
3. **今天零可达**：`cards_engine.json`（1,142 张）里没有 `f3df0cfc…`；`tutorial_decks.json` / `prebuilt_decks*.json` 也没有；
   2025-07-30 快照的 6 个 `TutorialStage` SO 里同样没有（`tutorial_stages.json` 现读 0 命中）。

---

## 三、本笔改了什么

**一条注释，零行数变化、零语义变化**（铁律 20「零行数变化」口径）：

- 文件：`Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs:18780`（在那一物理行的**行尾注释里**追加）
- 内容：指明真正持有 `160` 的是教程起手占位卡（给 `uniqueId` / `m_Name` / 语义），
  并写死「⛔ 别按 `L130` 的描述回填一张会弃牌的卡」，指向本报告。
- 行数：**22,095 行不变**；全 CRLF、裸 CR `0`。

**⛔ 没有打 `// ✅ 已复刻`**：本笔**没有复刻任何东西**（160 那条动作槽 2026-10-29 就已落地），
按铁律 20「⛔ 不许拿它当挡箭牌」不打。

---

## 四、验证读数

| 项 | 改前 | 改后 |
|---|---|---|
| `TMPDIR=/tmp/wf_b141_w58 bash 工具/typecheck.sh` | ——（注释前） | 运行时错误 **0** / 编辑器错误 **0** |
| `bash 工具/ruleprobe.sh check` | `rc=0`；`全池 1134 张 · 解析差异 0 行`；`哨兵合计：失败 0 条`；输出指纹 `77a3c2bc…` | `rc=0`，**除时间戳外与改前逐字节相同**（`diff` 只有 2 行时间戳） |
| `bash 工具/ruleprobe.sh play check` | `rc=0`；结算指纹 **`a0e9c63d7bfa33735f5dad8230a26edd`**；主档 1142 行 / 电池 1086 行；差异 0 | `rc=0`，**除时间戳外逐字节相同**，指纹仍 `a0e9c63d…`（与基线一致） |
| `bash 工具/ruleprobe.sh play BL37` | 见下 | 未再跑（只改注释；`play check` 已证指纹不变） |

`play BL37` 的关键读数（**正面证据：没有第二张手牌被弃**）：

```
── 打出去之前 ──  P0 手=[BL37,filler29] 手牌总价=2 弃=(空)
── 打出去之后 ──  P0 手=[filler29]     手牌总价=1 弃=[BL37]
  变化逐条（5 条）：⚙ 能量 -1 · ✎ FIX_A 2/0/0/2 → 4/2/0/2 {darkpact1, markofchaos1}
                    ✋ 手牌 -「BL37」×1 · 🗑 弃牌 +「BL37」×1 · 💰 手牌总价 -1
  日志：选一张牌 → 「Dark Pact of Excess」→ FIX_A 获得「纵欲」黑暗契约
```

⇒ 弃牌堆里**只有 BL37 自己**（任何战术卡打出都这样），**手牌只少了它**、别的牌一张没动。
这就是「原版本来就不弃」的正面读数。

---

## 五、为什么没有建断言文件

1. 本笔零行为变化 ⇒ **没有可断言的新行为**（断「原来就不发生的事」= 同义反复）。
2. 真要断「BL37 不弃牌」，得挂进 `RuleEngineTest.Run` 那种宿主，而**宿主不在我的白名单**；
   只建一个没人接线的 `Editor/B141_W58_Assert.cs`，正是本工程抓过多次的反模式
   （「断言文件建了但没人接线」；`B102_W1` 就是那笔账）。

**交调度台的两条可选后续（都不是 `L130`）**：
- ① **防回归**：在既有宿主里断 `PlayTactic(BL37)` 之后「弃牌堆只多 BL37 一张、手牌只少 BL37 一张」，
  并断 `choicesDefinition` 选中的是 4 张 Mark 之一。
- ② **补占位卡那条路**：那是「ability 行级 ca 派发」（`B-211` 的 B 表扩列 + 手牌触发路的正文取法），
  判据全在本报告 §一，需要**另开一条账**，⛔ 别塞进 `L130`。

---

## 六、❓ / 顺手发现（请调度台分流）

1. **`_startinghand` 是一族 SO，不止这一张**：另有 `Z-NC SpaceWolves-Tyrnak and Fenrir_startinghand`
   （`uniqueId 8f0302f1603cc954392d90d5dabd22d5`，`trigger 580 (Strike)` / `ctx 0 (Play)` /
   `chosenAbility 20 (doHeal)` / `ta 20 (self)`）⇒ 同一套「教程起手替身」机制的两个实例。
2. **消费者 = `TutorialStage.{player,enemy}StartingTroopsInHand`（强推断，⛔ 不是实证）**
   —— `dump.cs:42073-42074`（`// 0x98` / `// 0xA0`，`List<RawCardScript>`），是原版**唯一**一个「起手塞卡」的结构位；
   2025-07-30 快照里 stage 3/4/5/6 确实都填了它。
   **本地无法把「谁引用这张占位卡」解出来**：allcards（2026 build，`D:/2/关服前存档/…/ea8bca…/__data`）与
   `assets_full`（2025-07-30）**pathID 不互通** —— 控制实验：**BL37 自己的 pathID 在 `assets_full` 里也是 0 命中**，
   而占位卡的 8 字节 LE 模式（`62fe6d57a80160fd`）只在 cards 包自身出现
   ⇒ 不是「没找对」，是**跨 build 不可比**（铁律 2 的「不许拿 0 命中当不存在」）。
3. **我们的教程起手机制是有的**：`RuleCore.cs:345-346`
   `TutorialRules.SetupInitialHand(ctx, st, 0/1, st.playerStartingTroopsInHand, lookup)`；
   `tutorial_stages.json` 里 stage 3–6 的这两个列表**都有内容**（例：stage 3 我方 4 条 / 敌方 3 条）
   ⇒ 将来数据若带出占位卡，缺的是 §二·2 那一步，**不是**起手装配。
4. **`play BL37` 的一条既有出声（不是本笔引入）**：
   `ability 级 targetCriteria（trigger=ThisCardPlayed）判不了 ⇒ 本次不筛：abilityLevelCriteria 列没落（旧表 < v26）`。
   没验证过它是否改 BL37 的取目标（那一行是 `ta=30 target` + `selectFromChooseResolution=1`，且种类选择走 `unitConditions.choice`）
   ⇒ 记 ❓，归「卡片数据列」那条线。
5. **`B97W5_RowsAndDedupe.cs:24-25` 与 `:78` 的措辞会误导**（「唯一那一行落在 BL37 的**重复 SO**」——
   读起来像「BL37 的另一个副本」，实际是 `_startinghand` 教程占位卡、能力与本体完全不同）。
   该文件**不在我的白名单** ⇒ 可粘贴补丁（就地改注释、零行数变化）：

```csharp
// 原（两处，`B97W5_RowsAndDedupe.cs:24-25` 与 `:78`）：
//   「`cards_truth.json` 1,351 张里唯一那一行落在 BL37 的重复 SO `f3df0cfc…`，池内那张记录是 30」
// 改：
//   「唯一那一行在**教程起手占位卡** `T1 BlackLegion-Abaddons Chosen_startinghand`
//    （`uniqueId f3df0cfc…`，`m_Name` 自带 `_startinghand`；`trigger 30 / ctx 10`：
//    先 `ta=20 self` 弃自己、再 `ca 100` 造真 BL37）—— **不是** BL37 的副本。
//    池内那张记录（`uniqueId BL37`）是 `chosenAbility 30`。判据 → `报告/B141_W128_BL37Discard.md`」
```

---

## 七、出处清单（可直接复查）

| 用途 | 路径 |
|---|---|
| 枚举 | `d:/2/tools/il2cpp_out/dump.cs:20669/20677/20678/20679`（100/160/162/165）· `:20783`（`self = 20`） |
| `AbilityLogic` 字段 | `dump.cs:19456`（`chosenAbility //0x10`）· `:19459`（`playersAffected //0x20`）· `:19461`（`targetCriteria //0x28`）· `:19481`（`possibleTargets //0x78`） |
| `PlayAbility` 160 分支 | `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:257,262,263,286,588-603` |
| `ta=20 self` | `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:184-192` |
| 动作号 0x1c | `d:/2/tools/decomp_full/BattleManager__AddDiscardFromHand.c:26` |
| SO 真值 | `d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（`uniqueId f3df0cfc…` / `BL37`）＋ `Unity/MyGame/Assets/RuleEngine/Resources/cards_truth.json` |
| 可玩池 | `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（1,142 张，160/165 各 0） |
| 我们的执行体 | `RuleEngine/Core/B97W5_RowsAndDedupe.cs:80,113` · `RuleEngine/Core/EffectResolver.cs:18780` · `RuleEngine/Core/RuleCore.cs:17473` |
| 手牌触发路 | `RuleEngine/Core/EffectResolver.cs` 的 `FireHandAbilityTrigger`（判据 `CardAbility__IsRightContext.c`，只放 `10 Hand`） |
| 教程起手装配 | `RuleEngine/Core/RuleCore.cs:345-346`（`TutorialRules.SetupInitialHand`）· `Resources/tutorial_stages.json` |
| `TutorialStage` 起手位 | `dump.cs:42073-42074`（`playerStartingTroopsInHand //0x98` / `enemyStartingTroopsInHand //0xA0`） |
