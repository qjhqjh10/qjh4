# N11 · K9 —— `choiceEnchantment(16)` 普查与结论（第 1 轮）

> 代理：**N11（执行）** · 2026-10-25 · 完全复刻 ● 战斗逻辑驱动（`K9`）
> **本轮性质 = 判据 + 现状普查，⛔ 不改代码**（结论：主干已被另一条路覆盖；强行再加一条会「双问」，已把试做整段回滚）。
> 只改了 `账/战斗逻辑驱动.tsv`（第 771 行）与本报告。

---

## ① 先回答调度台那个问题：**这条链是纯引擎侧，还是必须有 UI？**

**答：原版是「玩家点选一张牌」的 UI 选择，但我们的 UI 早就有了 —— K9 不需要新造界面。**

| 问题 | 判据 | 结论 |
|---|---|---|
| 原版怎么选 | `BattleManager.SelectCard`（`BattleManager.cs:25340-25430`）：玩家走 **`GetChoiceOptions`（面板列表）**，AI 走 **`GetChoiceFullPool`（自动挑）**，结果写 `bm.cardChosenInSelection(+0x2e8)`；随后 `AddEffect(case 0x10)` 把它当 `newEffect.enchantment` 用 | **是玩家可见的选择** |
| 我们有什么 | `ctx.ChooseCardIds` / `ChoosePicks`（表现层 `CardChoicePanel` 在**动作发起前**把答案按结算序排好）+ `TakePickCard`（有 Id 用 Id、有下标用下标、都没有 ⇒ 等概率 `Rng` + `ChooseSites/ChooseAnswered` 记账） | **同形通道已存在**（`choosecard` 那一族在用） |
| 所以 | —— | ✅ **纯引擎侧可做，⛔ 不需要新做选牌界面** |

---

## ② 普查结果：13 张 `buffType 16` 的卡，今天各自的落点（探针 `K`，现读）

数据侧：`cards_engine.json` 已经把 `choicesDefinition` 解成三列 —— `choicesTargetsAffected` / `choicesMaxTargets` / **`choicesSpecificCardIds`（50 张卡有值）**，⛔ 不用再去解 `cards_raw.json`。

| 卡 | 卡面 | 今天的解析 | 判断 |
|---|---|---|---|
| `BL13 Chosen` / `BL4 Chaos Sergeant`（Rally: Gain a Dark Pact） | 没写种类 | **注入了 `choosecard`（候选 = 4 张 `Mark of X`）+ `gain`** | ✅ 玩家能挑 |
| `BL37 Abaddon's Chosen` / `BL38 Dark Oratory` / `BL57 Helfire Torch` / `Unhallowed Gifts`（Give a Dark Pact…） | 没写种类 | **注入了 `choosecard`（4 张 Mark）+ `give`** | ✅ 玩家能挑 |
| `EC60 Armoury of Excess`（give a Dark Pact of **Excess**） | **种类写死** | `deal` + `give`（不需要选） | ✅ 一致（它的 `fs=4` 对应的 `targetsAffected=0`，**不是**选择用） |
| `TL20 Carnifex` / `DA39 Ancient Reliquary` | Choose … a bonus | `chooseone`（三选一） | ✅ 净效果等价（差别只在「原版选中的是一张附魔卡」这层记账） |
| `TL49 Hyper-adaptation` / `TL50 Infinite Biomorphologies` | Choose an effect | `chooseeffect` | ✅ 同上 |
| `ASH57 Webway Entrance` | Choose a Saim-Hann Infantry… | 自带 `choosecard`（文本层） | ✅ 同上 |
| `GSC64 Rusted Vents` | Create a random troop… | `create` + `lowercost` | ✅ 一致 |

**判据链**：`EffectText.InjectChoicesDefinitionOp`（`EffectText.cs:2463`，四道闸=数据齐/正文没有 choose/下游真的消费/ops 里确有那条授予）+ `GrantDarkPact` 读 `ctx.LastChosenCard`（`A1960`/`W5` 那批建的）。
⇒ **`AddEffect` 分派器 §K9 里那句「`16` 未建」是错的**（已在账里就地订正为 🟡「主干已覆盖、记账层未建」）。

---

## ③ 那 K9 还剩什么（铁律 11：要做，只是排先后）

1. 🔴 **`ActiveEnchantment` 记账层**（原版 case 16 的头半段）：建 `{enchantmentCard, enchantmentCardID = bm.enchatmentsUsed}` → `UsedEnchantment()` → `activeEnchantments`/`activeEffects` 两条 list 各 Add → `ActivateTraitsOnSummonOrEnchantment(false, true)`。
   · 对 **190（MarkOfChaos）**这一支**无所谓**：它的 Tail = `SpendEnchantment` + `lastMarkOfChaos = 附魔卡` + `TriggerEnchantment` + `BroadcastMarkResolved` ≈ 我们的 `GrantDarkPact` / `ApplyMarkBody`（K7 已落，且判据逐条对过）。
   · 对 **60 SecretOrder（`secretBuff=1`）/ 120 Quest（清掉其它 Quest 附魔 + `questBuff=1` + `questCounter=0`）** 这两支**是必需的**。
2. 🟡 **五子支的数据可达性**（Q2 已解）：`60/120/130/140` 池内 **0 张** ⇒ 今天不可达；`190` 有（28 条）。按铁律 11 四支仍要做，排后。
3. 🟡 **`SpendEnchantment`** 的语义（130/140/190 各一次；「选中的附魔卡用完就花掉」）。

---

## ④ 本轮为什么**不改代码**（一次「修一处坏一处」的现场记录）

我一度按「没写种类 ⇒ 用选牌通道问玩家」写了第二套：`GivePayload.ExtractVariant` 区分 `""`（没写 random）与 `"random"` + `EffectResolver.PickPactKind`（`TakePickCard` 走 4 张 Mark）。
**普查后发现注入那条已经在问**（`InjectChoicesDefinitionOp` 会插一条 `choosecard`，`GrantDarkPact` 里的 `fromChoice` 先命中 `ctx.LastChosenCard`）⇒ 我那条在真局里**永远进不去**，而在「注入没触发」的边角（夹具、`card == null` 的解析）会**再问一遍** ⇒ **`ChoosePicks`/`ChooseCardIds` 双出队 = 后面每一处错位**（静默选错）。
⇒ **整段回滚**（`git diff --numstat GivePayload.cs` = 空 = 与 HEAD 逐字节相同；`EffectResolver.cs` 的 `PickPactKind` 已删，只留一处无害的 `FindPactCard` 抽取复用）。
**回滚后的读数与 K7 收工逐字节相同**：`ruleprobe check` 解析差异 **1 行 = BL46**（K7 那条有意的）· `play check` 指纹 `374ae74827e3210666c443e96a247677` · 主档 13 张 / 电池 20 张。

---

## ⑤ 判据与证据（文件:行号）

- 原版：`decomp_full/CardScript__AddEffect.c:826-829`（case 12/15 = `AddEnchantment`）· **`:830-932`（case 0x10 = 16，全文逐句读过）**。
- 选择从哪来：`out_cpp2il_dev_ilspy/BattleManager.cs:25340-25430`（`SelectCard`：玩家 `GetChoiceOptions` / AI `GetChoiceFullPool` → `cardChosenInSelection`）· `:25621-25720`（`GetChoiceFullPool` 方法体：`targetsAffected == 180 ? filterSpecificCards : 目标原始卡`）· `:36427-36435`（`choiceEnchantment ⇒ newEffect.enchantment = cardChosenInSelection`，Q2 已核）。
- 数据：`cards_truth.json` 的 13 张 `buffType 16`（`enchantment` 全 null）；`cards_engine.json` 的 `choicesSpecificCardIds`（50 张，BL37/BL38 = 4 张 Mark、TL20 = `Crushing Claws`/`Devourer Cannon`/`Armoured Exoskeleton`）。
- 我们侧：`EffectText.InjectChoicesDefinitionOp`（`EffectText.cs:2463-2477`）· `HasUnspecifiedDarkPactGrant`（`:2494-2512`）· `CardDef.HasSpecificCardChoice`（`CardDef.cs:790-798`）· `TakePickCard`（`EffectResolver.cs:4225`）· `GrantDarkPact` 的 `fromChoice`（`:11788-11796`）· `Enchantment.cs`（`ActiveEnchantment` / `Apply`）。

## ⑥ 没查清 / 留给下一批

1. **`bm.enchatmentsUsed(+0x4e4)` 用在哪**（`UsedEnchantment()` 的读点）：决定「记账层」除了 60/120 之外还有没有别的可见后果 —— **没核**。
2. **120 Quest 支的 `DisplayStatusAnim(0x2ee = 750)`** 在表现层怎么落（`CardPresentation/Battle/**`，不属于我）—— **没核**。
3. **`ActiveTraitsOnSummonOrEnchantment(false, true)`** 的参数语义（两个 bool 各管哪一半）—— **没核**。
4. **`GetChoiceFullPool` 的 `cardsTargetedInChosenResolution` 谁填**（`targetsAffected == 180` 且 `filterSpecificCards` 为空时的那条路）—— 对我们无影响（13 张里该档的卡都由文本层覆盖），但**没核**。
