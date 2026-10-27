# `activeAbilityTextReferenceId` 与 ability 级 `targetsAvailableCounter` —— 真值与施工规格

> 查判据代理 `W55ALTREF` · 2026-10-27 · **只读**（未改任何 `.cs` / `.py` / `.json` 数据 / 账）
> 数据源：`D:/2/关服前存档/新建文件夹/Everguild_Warpforge/54bdb7b1114a2de41519cac560a4bce4/ea8bca4f5c6909d3124fd4975dc96173/__data`
> （现 build 的 allcards，**1,351 张卡 SO**）—— 逐对象 `read_typetree()`，**与 `cards_raw.json` 零差异**（见 §二·0 三源核验）。
> 复现脚本：`d:/4/_tmp_view/w55/extract{1..20}.py`（只读，`python -I -X utf8` 跑）。

---

## §〇 一句话结论（先说三件会改变既有判断的事）

1. **`activeAbilityTextReferenceId` 全池只有 3 张卡非空**（`SOR30`→`SOR30_new` · `SOR25`→`SOR25_new` · 一张**不在池里**的 GUID 卡→`AM68`）。
   ⇒ 派活包说的「**34 条拼不出来** 里 30 条 `Effect<名>` 卡在这一列」**不成立** ——
   **那 30 条 `Effect<名>` 一个都不来自这个字段**，它们来自**另一条完全不同的链**：
   `abilityLogic[].buffEffect.effectText`（buff/效果标题）→ `GameStaticData.GetEffectDesc`（见 §三）。
2. 补上这一列能救的**只有 2 张池内卡**（`SOR30` / `SOR25` 的技能面板正文）。但**同一族缺列**里还有一条**大得多的**：
   `cardDescriptionTextReferenceId`（44 张非空 / **15 张在池**）—— 其中 **10 张池内卡今天在卡面上印着「旧版正文」**（§五·3）。
3. **ability 级 `targetsAvailableCounter` 原版确实有这一格（每卡 2 份），但 1,351 张的值 100% 全 0** ⇒
   今天「恒 false」**是对的**。**不需要补数据**；`cards_truth.json` 0 条属于「没抽」，但抽了也不改行为（§四）。

---

## §一 字段定义与出处

### 1·1 `activeAbilityTextReferenceId`（卡 SO 上的一格 `string`）

| 事 | 出处 |
|---|---|
| 字段声明 + 偏移 | `d:/2/tools/il2cpp_out/dump.cs:23329` → `public string activeAbilityTextReferenceId; // 0x248`（属于 `RawCardScript`） |
| 相邻字段（本次一并核过） | 同处 `:23330` `cardDescriptionTextReferenceId // 0x250` · `:23331` `cardRaceTextReferenceId // 0x258` · `:23335` `targetingTextReferenceId // 0x278` · `:23341` 卡级 `targetsAvailableCounter // 0x298` |
| 4 个消费点（全部见 §1·2） | `RawCardScript.cs:2292/2364/2378/2410`（ILSpy 版 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs`） |
| 我们的现状 | `cards_engine.json` **0 命中**（`grep` 探针 = 0）；`CardDef.cs` 无该字段；全仓只有 `SkillPanel.cs:2173/2178` **注释**提到它 |

### 1·2 原版 4 条取键链（全部 `(activeAbilityTextReferenceId ?? uniqueId)`）

| # | 方法 | 键 | 行 |
|---|---|---|---|
| ① | `GetLocalizedAbilityName()` | `Card_Abilities/Ability_Name_<id>` | `RawCardScript.cs:2362-2364` |
| ② | `GetLocalizedAbilityDesc(suffix)` | `Card_Abilities/<id><suffix>`（**技能面板那条**） | `:2374-2405`；`term` 在 `:2396` |
| ③ | `GetLocalizedAbilityDescRemovingSiege(suffix)` | `Card_Abilities/<id><suffix>` | `:2408-2445`；`term` 在 `:2411` |
| ④ | `GetDescAndAbilityRaw()`（`Card_Description/<id>` **＋** `Card_Abilities/<id>` 拼接） | `Card_Abilities/<id>` | `:2259-2311`；`term2` 在 `:2309` |

**`Contains("((")` 短路支**（`:2384-2389` 与 `:2298-2303`）：
`activeAbilityTextReferenceId.Contains("((")` 为真时**不查本地化、直接把该字段当正文**返回（后面仍走 `ModifyLocalization`）。
👉 **现数据 0 张卡命中**（3 个非空值是 `SOR30_new` / `SOR25_new` / `AM68`，都不含 `((`）。
⚠️ 别把「**值里**带 `((`」和「**id 里**带 `((`」搞混：`Card_Abilities/SOR30` 的**值**是 `((Gain_x,[[armour]] 1))`，
那是 `ModifyLocalization` 解的另一套记号，**不触发**这个短路支。

### 1·3 同族另 4 个 `*ReferenceId`（同一张 SO，同一套「?? uniqueId」写法）

| 字段（`dump.cs` 偏移） | 原版链 | 键族 | 池内非空 |
|---|---|---|---|
| `cardDescriptionTextReferenceId`（`+0x250`） | `GetLocalizedCardDesc`（`:2317-2342`）· `GetDescAndAbilityRaw`（`:2259`）· **名字兜底** `Card_Name/<ref>`（`:2150`）/ `Card_FullName/<ref>`（`:2224`） | `Card_Description/…` | **15 / 1,128** |
| `targetingTextReferenceId`（`+0x278`） | `GetLocalizedTargetted`（`:2448-2456`）→ `Card_Targeted/<ref>` | `Card_Targeted/…`（31 键） | **51 / 1,128** |
| `chatMessagesReferenceId`（`+0x2D0` 区） | `GetLocalizedSummonChat` / `GetLocalizedChatMsg` → `Chat_Messages/<ref>` | `Chat_Messages/…` | **0 / 1,128** |
| `customIntroReferenceId` | 对战开场白 | — | **0 / 1,128** |
| `cardRaceTextReferenceId`（`+0x258`） | — | — | **0**（全库 0 非空） |

**这 6 个字段的 `((` 短路支全部命中 0 张**（逐字段扫过，`extract20.py`）。

### 1·4 ability 级 `targetsAvailableCounter`

| 事 | 出处 |
|---|---|
| 类型 | **`TargetCriteria`（一整条 criteria 结构，**不是 int**）** — `dump.cs:23764` |
| 声明 | `ActiveAbility.targetsAvailableCounter // 0x40`（卡级那格是 `RawCardScript.targetsAvailableCounter // 0x298`，`:23340`） |
| 原版唯一消费点 | `ActiveSkillDisplay.cs:160` 读 **ability 级**（`BattleDriver.cs:35015-35029` 那条注释已把两格分开写清） |
| 我们的现状 | **卡级已落**（`CardDef.AvailSet`，`role == "avail"`，`CardDef.cs:2273`；`cards_engine.json` 现读 **17 张卡有该行**）；**ability 级没落、生成器也没这条路径**（`gen_cards_engine.py:2946-2951` 只有 `add(-3, -1, "avail", …)` 一条 = 卡级） |

---

## §二 `activeAbilityTextReferenceId` 真值表

### 2·0 三源核验（先证「不是我解错了」）

| 源 | 结果 |
|---|---|
| ① 缓存解析 `资料/普查产出_1018/allcards_1350/cards_raw.json` | 1,351 张，该字段 `present=1351 / 非空=3` |
| ② **本次从 `__data` 二进制重新 `read_typetree()` 现解**（`工具/export_allcards.py --out d:/4/_tmp_view/w55/fresh`） | 1,351 张，**与①逐卡逐字段 0 差异**（`verify_fresh.py`） |
| ③ `--check` 模式（同工具，逐字段非空数对账） | `卡数一致 1351` · `uniqueId 集 0 差异` · `每卡字段非空数分布全部一致` · `顶层字段键集一致（116）` ⇒ **结论：一致 ✅** |

> `__data` 是压缩流，所以「在字节里 grep 字符串」= 0 命中是**正常的**（`verify_fresh.py` 里那 4 个 `bytes …: 0` 就是这个原因），⛔ 不能拿它当反证。

### 2·1 全部非空值（**3 条，全表就这 3 条**）

| # | 卡 id（`uniqueId`） | 卡名 | `activeAbilityTextReferenceId` | 官方表有 `Card_Abilities/<它>`? | 在池? | 备注 |
|---|---|---|---|---|---|---|
| 1 | **`SOR30`** | Celestian Sacresant | **`SOR30_new`** | ✅ 有 | ✅ | 池卡。**同时** `cardDescriptionTextReferenceId` 也是 `SOR30_new` |
| 2 | **`SOR25`** | Imagifier | **`SOR25_new`** | ✅ 有 | ✅ | 池卡。**同时** `cardDescriptionTextReferenceId` 也是 `SOR25_new` |
| 3 | `66486b3f61682ab42ba2ce0fa921002f` | Tempestus Scion | **`AM68`** | ✅ 有（`Card_Abilities/AM68`） | ❌ **不在池** | GUID 卡；池里另有一张同名卡 `AM68`（`AM68` 自己的 `altRef` 是**空**）⇒ **对池内无净影响** |

### 2·2 覆盖率

| 量 | 值 |
|---|---|
| SO 总数 | **1,351** |
| 字段**存在**（键在） | **1,351 / 1,351**（不是缺键，是空串） |
| **非空** | **3**（0.22%） |
| **空串** | **1,348** |
| 其中**在池**（`cards_engine.json` 1,128 张） | **2** |
| `is_uid`（引用自己） | **0 / 3** —— 3 条**全部**指向**别的 id** |
| 引用目标在官方表里存在 | **3 / 3** |

### 2·3 补这一列**实际会改什么**（现读，⛔ 不是估算）

| 卡 | 今天我们取（错） | 原版取（对） | 差异 |
|---|---|---|---|
| `SOR30` | `Card_Abilities/SOR30` = `((Gain_x,[[armour]] 1))` | `Card_Abilities/SOR30_new` = `((Heal_x_to_a_friendly_unit,3))` | **效果完全不同**（原版 `+护甲1` vs 现版 `治疗 3`） |
| `SOR25` | `Card_Abilities/SOR25` = `((Give_trait,[[flank]]))` | `Card_Abilities/SOR25_new` = `Give +1 <melee>, +1 <ranged> and [[flank]]` | **少了两条属性加成** |

> 旁证（两张卡的**引擎 `desc` 已经是新版语义**）：`cards_engine.json` 里 `SOR30.desc` = `Pray: Heal 3 to a friendly unit`、
> `SOR25.desc` = `Pray: Give +1 [Attack], +1 [Ranged] and Flank to a friendly troop.`
> ⇒ **是我们的技能面板在印旧版**，不是卡数据错。

### 2·4 补这一列**不会改什么**（避免过度期待）

- `Card_Abilities/Ability_Name_*`：官方表 **0 条该前缀的键** ⇒ `GetLocalizedAbilityName` 现 build **恒缺键**（`extract11.py`）。补列也不会有值。
- `AM68` 那张：需要它的 GUID 卡**不在池**，池里的 `AM68` 自己 `altRef` 是空 ⇒ **补列后依然取 `Card_Abilities/AM68`**，无变化。
- `SOR66`（唯一「有主动能力但没有 `Card_Abilities/<id>` 键」的池卡）：`altRef` **也是空** ⇒ 补列**救不了它**，原版在这一格会走到 `throw`（`RawCardScript.cs:2443`）。⇒ 见 §六·2。

---

## §三 「34 条拼不出来」的**真实**归属（30 条 `Effect<名>` 不来自 §二 那个字段）

### 3·1 先拆数

官方表 `Card_Abilities/*` 共 **120** 键 = **30 `Effect*`** + `ExperimentalDronesEffect` + `SaviorProtocolsEffect` + `SOR25_new` + `SOR30_new` + **86 张卡的卡 id**。
「34 条拼不出来」= 120 − 86 = **30 `Effect*` + 4 条**（`ExperimentalDronesEffect` · `SaviorProtocolsEffect` · `SOR25_new` · `SOR30_new`）。

### 3·2 真正的来源：`buffEffect.effectText` / `handBuff.cardEffect.effectText`

原版链（**与 `activeAbilityTextReferenceId` 毫无关系**）：

```
卡 SO `abilityLogic[].buffEffect.effectText`（或 `handBuff.cardEffect.effectText`）
  → CardEffect.effectText                                           [CardEffect.cs:12]
  → GameStaticData.GetEffectDesc(effect)                            [GameStaticData.cs:1639]
  → term = "Card_Abilities/" + effect.effectText                    [GameStaticData.cs:1772]
  → LocalizationManager.GetTranslation(term) → ModifyLocalization
  → 缺键时【原样返回 effectText 本身】                              [GameStaticData.cs:1778-1782]
```
> ⚠️ 最后那一跳很重要：`EffectArmouredSupport` 这种 id **缺键时会被原样印到屏幕上**，不是回空串。

**我们的现状**：`cards_engine.json` 里 `effectText` / `buffEffect` / `handBuff` / `cardEffect` **全部 0 命中**
（`extract16` 前那轮探针）⇒ **这条链我们整条没有**，跟 `activeAbilityTextReferenceId` 是**两个独立缺口**。

### 3·3 §三·甲 —— 30 条 `Effect*` 的归属（**29 条有主 + 1 条孤儿**）

| `Card_Abilities/<键>` | 归属卡（`uniqueId`） | 卡名 | 在池 | 卡上路径（同卡可能多处） |
|---|---|---|---|---|
| `EffectArmouredSupport` | `UM44` | Armoured Support | ✅ | `cardAbilities[0].abilityLogic[0].buffEffect.effectText` |
| `EffectBeastBoss` | `GOF91` | Beastboss on Squigosaur | ✅ | 同上 `[0][0]` |
| `EffectBjornShrine` | `SW40` | Bjorn's Shrine | ✅ | `[0].abilityLogic[1].buffEffect.effectText` |
| `EffectCadiaStands` | `AM51` | Cadia Stands | ✅ | `[0][0].buffEffect` |
| `EffectCoetrie` | `EC51` | Coterie of the Conceited | ✅ | `[0][0].buffEffect` |
| `EffectDisplayCardsAddToDeck` | `GSC55` | Eve of the Uprising | ✅ | `[0].abilityLogic[2].buffEffect.effectText` |
| `EffectDutysEnd` | `UM113` | Duty's End | ✅ | `[0][0].buffEffect` |
| `EffectEnhancedAggression` | `GSC53` | Enhanced Aggression | ✅ | `[0].abilityLogic[1].buffEffect` |
| `EffectFellhanded` | `SW36` | Bjorn the Fell-Handed | ✅ | `[0][0].buffEffect`（**该卡 `hasActiveAbility=1`**） |
| `EffectFerocious` | `84d4d192e0f57a34a90ac0f6172c3358` | Ferocious Rage | ✅（GUID 卡在池） | `[0].abilityLogic[1].buffEffect` |
| `EffectGracefulAvoidance` | `ASH72` | Graceful Avoidance | ✅ | `[0][0].buffEffect` |
| `EffectHallowed` | `SOR57` | Hallowed Martyrs | ✅ | `[0][0].buffEffect` |
| `EffectHelspear` | `61046837c330b48978b4af5201230b6e` | Helspear Assault | ✅ | `[0].abilityLogic[1].buffEffect` |
| `EffectHiveFleet` | `TL47` | Hive Fleet Leviathan | ✅ | `[0].abilityLogic[0]/[1]/[2]`（**同卡 3 处**） |
| `EffectHuntersHeretics` | `DA74` | Hunters of Heretics | ✅ | `[0].abilityLogic[1].buffEffect` |
| `EffectIdolatrous` | `BL75` | Idolatrous Despoilers | ✅ | `[0][0].buffEffect` |
| `EffectImmortalPride` | `SAU55` | Immortal Pride | ✅ | `[0].abilityLogic[2].`**`handBuff.cardEffect`**`.effectText` |
| `EffectKrump` | `GOF71` | Krump da Gitz | ✅ | `[0][0].buffEffect` |
| `EffectLitanyOfDispair` | `9176318eaf469354e858f51af2333aa3` | Litany of Despair | ✅ | `[0].abilityLogic[1].buffEffect` |
| `EffectLivingIcon` | `GSC51` | Living Icon | ✅ | `[0].abilityLogic[0]/[1]`（**同卡 2 处**） |
| `EffectMasterOutrider` | `c433e3dde4952cc448220908226127ae` | Master Outrider | ✅ | `[0].abilityLogic[1].buffEffect` |
| `EffectOath` | `UM73` | Oath of Moment | ✅ | `[0].abilityLogic[1].buffEffect` |
| `EffectPledge` | `EC38` | Pledge to the Dark Prince | ✅ | `[0][0].buffEffect` |
| `EffectPowerWaaagh` | `GOF106` | Power of the Waaagh! | ✅ | `[0][0].buffEffect` |
| `EffectRaidTactics` | `SW46` | Raid Tactics | ✅ | `[0][0].buffEffect` |
| `EffectUgeChoppa` | `6899b199f5e36144ebbdd7ed9d9e7afe` | Uge Choppa | ✅ | `[0].abilityLogic[1].buffEffect` |
| `EffectUndergroudndNetwork` | `GSC45` | Underground Network | ✅ | `[0].abilityLogic[1].`**`handBuff.cardEffect`** |
| `EffectUndyingLegions` | `SAU53` | Undying Legions | ✅ | `[0][0].buffEffect` |
| `EffectWagesRetribution` | `DA55` | Wages of Retribution | ✅ | `[0][0].buffEffect` |
| **`EffectBoltPistol`** | **（孤儿）** | — | — | **全快照 0 张卡引用**（`"BoltPistol"` 在 1,351 张卡里 **0 命中**）；1,351 张里所有非空 `effectText` 去重 = **31 个**，不含它 |
| `ExperimentalDronesEffect` | `TAU42` | Experimental Drones | ✅ | `[0][0].buffEffect` |
| `SaviorProtocolsEffect` | `TAU51` | Saviour Protocols | ✅ | `[0].abilityLogic[0]/[1]/[2]` 的 `buffEffect` **与** `handBuff.cardEffect`（**同卡 5 处**） |

> ⛔ **`EffectDisplayCardsAddToDeck` 那一条别当成缺口** —— 它已由我们另一条路（`EffectResolver.cs:7762` 的
> `"Card_Abilities/" + title`）在消费，且有用例（`RuleEngineTest_EntryPump.cs:364-366`），
> 与「技能面板正文」不是同一条链（`B9_W21SKILL` §五·1 已提过，本次**补齐了全部 30 条的归属**）。

### 3·4 §三·乙 —— 另外 4 条

| 键 | 归属 | 机制 |
|---|---|---|
| `SOR30_new` | `SOR30` | `activeAbilityTextReferenceId` **＋** `cardDescriptionTextReferenceId`（两格同值） |
| `SOR25_new` | `SOR25` | 同上 |
| `ExperimentalDronesEffect` | `TAU42` | `buffEffect.effectText`（**注意：它不以 `Effect` 开头，别被「30 条 `Effect<名>`」的措辞漏掉**） |
| `SaviorProtocolsEffect` | `TAU51` | 同上的 `buffEffect` / `handBuff` |

---

## §四 ability 级 `targetsAvailableCounter` 的结论

### 4·1 结论（一句话）

**原版有这一格（每张卡 `activeAbility` / `alternativeAbility` 各一份，1,351×2 = 2,702 个实例），
但 1,351 张卡的值 100% 全 0（连 `activeAbilityLogic` 里那 39 个子字段也全是 0 / 空表）。
⇒ 今天 `ShouldShowAbilityTargetsCounter` 恒 false 与**原版一致**；`cards_truth.json` 0 条是「没抽」，但抽了也不改行为。**

### 4·2 三源证据

| 源 | 结果 |
|---|---|
| ① 缓存解析 `cards_raw.json` | `activeAbility.targetsAvailableCounter` 1,351 张**全部 `allzero`**；`alternativeAbility.…` 同样 1,351 全 `allzero`（严格判据 = 39 格逐格 0 / `""` / `[]`，`extract15.py`） |
| ② 本次从 `__data` 现解（`w55/fresh`） | **与①完全一致**（1,351 / 1,351 / 0 非零） |
| ③ 既有独立结论（`BattleDriver.cs:35024` 那段注释，早前另一路查的） | 「`activeAbility.targetsAvailableCounter.targetsAffected != 0` 的卡 = **0 张**」 ⇒ **与本次逐卡扫描逐字吻合** |

### 4·3 顺带把**卡级**那格的真值表也给全（它是**另一格**，别混）

- 卡级 `RawCardScript.targetsAvailableCounter (+0x298)`：**30 张卡有任一非零格**，其中 **17 张 `targetsAffected != 0`**（= 「有信息」）。
- 我们**已经落了**：`cards_engine.json` 的 `role == "avail"` 行现读 **正好 17 张卡**（`extract14.py`）—— 与 `targetsAffected != 0` 的 17 张**逐一相同**。
- 那 30 张里剩下的 13 张只有 `friendlyUnits` / `includeMinions` 等非零、`targetsAffected == 0`（= `any`，其余 35 格全失效）⇒ 生成器的 `criteria_carries_targets` 闸（`gen_cards_engine.py:2638-2647`）正确地**不落行**。
- **30 张的 `hasActiveAbility` 全部 = 0**（旁证 `BattleDriver.cs:35026` 那句「那 17 张 `hasActiveAbility` 全 = 0」）。

| 卡级 `targetsAffected != 0` 的 17 张 | DA56 · DA73 · EC39 · GSC55 · SAU11 · SAU57 · SOR42 · TAU47 · UM61 · UM62 · UM63 · `20d0f445…` · `508002f5…` · `98d3c657…` · `9dcbe3d0…` · `9f4e1b7e…` · `c4ff2790…` |
|---|---|
| 只在其余格非零（13 张，不落行） | UM45 · UM60 · UM64 · `65b2be82…` · `6899b199…` · `6b945ed6…` · `74480125…` · `8482077f…` · `8f35df05…` · `97a4b83a…` · `0d2874f3…` · `4de4fcfb…` · `d0516815…` |

### 4·4 为什么不建议现在给 ability 级加生成器路径

`add()` 的闸是 `criteria_carries_targets`（`targetsAffected != 0`）。ability 级**全 0** ⇒ 加了路径也**一条行都落不出来**，
消费端仍拿不到任何东西；而它会让「`null` = 没落/未知」与「`null` = 原版就是 any」继续**不可区分**（`CardDef.cs:2265` 那条纪律）。
⇒ **做法**：把「ability 级恒 any」写成**判据**（本文件），`ShouldShowAbilityTargetsCounter` 维持恒 false **并附本文件指针**；⛔ 别加死列。

---

## §五 可施工规格（**只写规格，本次一个字没改**）

### 5·1 生成器 `工具/gen_cards_engine.py`

**（a）`activeAbilityTextReferenceId` —— 加一列**

- 位置：`A1799_FIELDS`（`gen_cards_engine.py:3428` 起那张 `(我们的列名, 真值字段, 缺值, 说明)` 表）**追加两项**：
  ```python
  ("activeAbilityTextReferenceId",   "activeAbilityTextReferenceId",   "",
   "技能面板取键的 id 那一半：`Card_Abilities/` + (本列 ?: id) + suffix "
   "（`RawCardScript.cs:2364/2374-2405/2407-2445/2259-2311`，字段 `+0x248`）。"
   "现读 1,351 张**只有 3 张非空**（SOR30→SOR30_new · SOR25→SOR25_new · 一张不在池的 GUID 卡→AM68）⇒ 池内命中 **2**。"
   "判据 → 资料/完全复刻/判据/activeAbilityTextReferenceId_真值.md"),
  ("cardDescriptionTextReferenceId", "cardDescriptionTextReferenceId", "",
   "卡面正文取键的 id 那一半：`Card_Description/` + (本列 ?: id)（`RawCardScript.cs:2317-2342`），"
   "**还兼任** `Card_Name/<ref>` / `Card_FullName/<ref>` 的兜底（`:2150` / `:2224`）。"
   "非空 44 / **池内 15**，全部形如 `<自己 id>_new`。判据同上一行那份文件 §五·3"),
  ```
- ⚠️ **这条会动池指纹** ⇒ 只能在**调度台的无写手窗口**做（派活包已写死，本次遵守）。
- ⚠️ 列名**与 SO 字段同名**是既有约定（`attackDamageType` / `refNameId` / `itemDescriptionKey` 都这样），⛔ 别另起缩写。
- 缺值用 `""`（不是 `-1`，这列是 string）。

**（b）`targetsAvailableCounter`（ability 级）—— 不加**。理由见 §4·4。

**（c）`effectText`（§三 那 31 条）—— 这是另一张工单，别混进本次**
- 需要的不是卡级列，而是 **`activeAbilityLogic[] / cardAbilities[].abilityLogic[]` 每一格上的 `buffEffect.effectText` + `handBuff.cardEffect.effectText`**（现读 31 个不同值、33 个挂点）。
- 现有 `activeAbilityLogic` 列（`cards_engine.json`）**不含** `effectText` ⇒ 要新开一个「逻辑格 → 效果标题」的映射列 / 或挂在 `abilityLogic` DTO 上。
- 本次**只登记，不出施工单**（依赖链更长，须先定 `CardEffect` 在引擎侧有没有对应物）。

### 5·2 `RuleEngine/Core/CardDef.cs` + `RuleEngine/Data/CardDatabase.cs`

| 加什么 | 怎么加 |
|---|---|
| `CardDef` 两个公有字段 | `public string ActiveAbilityTextReferenceId;` · `public string CardDescriptionTextReferenceId;`（⛔ **追加到类尾** —— 铁律：插进中间会推下游行号，`check_ledger_points` 立刻报漂移） |
| `CardDatabase` DTO | 同名字段（照 `refNameId` / `itemDescriptionKey` 的写法，`CardDatabase.cs:255/261`）。**这两列不是死列，⛔ 别加 `[Obsolete]`** |
| 反序列化 | 照现有 `A1799_FIELDS` 那一批的落法（缺键 ⇒ `""`） |
| 取键辅助（建议） | 在 `SkillPanel` 那一侧加一个私有口 `string AbilityTextId(CardDef c) => string.IsNullOrEmpty(c.ActiveAbilityTextReferenceId) ? c.Id : c.ActiveAbilityTextReferenceId;`，四处取键统一走它（**别在四个调用点各写一遍**——铁律「两处写同一条规则」） |

### 5·3 消费端（**这条比 §5·1 更要紧**：同族缺列正在让 10 张池卡印错正文）

`cardDescriptionTextReferenceId` 的消费者是 `BattleDriver.FaceTextFull`（`BattleDriver.cs:27991-28025`）：

- 现状：`off = Loc.OfficialCurrent("Card_Description/" + c.Id)`（`:28013/28015/28017/28025`）——**恒用卡 id**。
- 现读：**15 张池内卡**该列非空，**全部形如 `<id>_new`**，且**新旧两版描述内容实质不同（15/15 全不同）**。
- 其中 **5 张**已在 `OfficialFaceTextWhitelist`（`BattleDriver.cs:27975` = `{ASH48, SAU58, SAU15, TAU65, AM72, SOR25}`）⇒ 走我们自己的 `desc`，**恰好都是新版语义** ⇒ 这 5 张**表面正常**。
- ⇒ **真实受影响 = 10 张**：**`TL66` · `SOR24` · `SOR30` · `SOR39` · `UM72` · `SOR47` · `TL24` · `TL44` · `UM57` · `UM68`** —— 今天卡面印的是**旧版正文**。逐卡差异见下表。

| 卡 | 今天印的（旧） | 应为（`_new`，= 我们 `desc` 的语义） |
|---|---|---|
| `TL66` | `Tide 4` | **`Tide 2`** |
| `SOR24` | `…to **a** friendly unit` | **`…to **all** friendly units`** |
| `SOR30` | `[[pray]]: Gain armour 1` | **`[[pray]]: Heal 3 to a friendly unit`** |
| `SOR39` | `If the unit is **Praying**` | **`If the unit **has pray**`** |
| `UM72` | `[[armour]] 1. [[codex]]: Deal **4** damage` | **`[[codex]]: Deal **3** damage`** |
| `SOR47` | `**5** ☀: Extend effect` | **`**6** ☀: Extend effect`** |
| `TL24` | `When a **friendly TL37 dies**, deploy another one` | **`When an **enemy attacks**, deploy a TL37`** |
| `TL44` | `[[armour]] 2 and **+2** Health` | **`…and **+1** Health`** |
| `UM57` | `…to all your Vehicles`（无时限） | **`…to all your Vehicles **this turn**`** |
| `UM68` | `Give [[armour]] 1 and heal **3**` | **`…and heal **2**`** |

> 施工：把 `"Card_Description/" + c.Id` 换成 `"Card_Description/" + DescKey(c)`，`DescKey(c) = string.IsNullOrEmpty(c.CardDescriptionTextReferenceId) ? c.Id : c.CardDescriptionTextReferenceId`
> （**四处**：`:28013` / `:28015` / `:28017` / `:28025`）。⚠️ `SkillPanel.cs` 那侧**不要**跟着改（它用的是另一族键）。
> ⚠️ 白名单那 6 张照旧优先我们自己的 `desc`（不冲突：它们本来就是「官方表缺/分歧」）。
> ⚠️ `AM72` 这个白名单项在 1,351 张 SO 里**没有一张卡的 descRef 是它**（真正有 descRef 的是 **`UM72`**）——
> 本次**没有查**这是笔误还是另一张卡 ⇒ 见 §六·4，⛔ 别擅自改。

### 5·4 验收口

| 项 | 口 |
|---|---|
| 数据侧 | `python -I -X utf8 工具/gen_cards_engine.py` 重生成后，`cards_engine.json` 里 `activeAbilityTextReferenceId` 非空应 = **2**、`cardDescriptionTextReferenceId` 非空应 = **15**（现读池口径；全快照口径 3 / 44） |
| 技能面板（2 张） | `SOR30` 面板正文应含 `Heal 3`、**不含** `armour 1`；`SOR25` 应含 `+1` 两次 + `flank`。宿主 = `BattleScene.Run`（`SkillPanel` 只在战斗里开；`BattleScene.cs:12056` 那片已有同族断言区） |
| 卡面正文（10 张） | `BattleDriver.FaceTextFull` 的官方取值应改用 `_new` 键 ⇒ 断言「这 10 张的 `FaceBody` 正文 ≠ 旧版键的值」。宿主 = `BattleScene.Run`（`BattleScene.cs:25335/25364` 已在按 `IsOfficialFaceTextWhitelisted` 分流，断言就加在那两处旁边） |
| 回归闸 | 补列后 `Card_Abilities/SOR30`、`Card_Abilities/SOR25` 这两条键**不应再被技能面板取到**（可用「取到的串不含 `Gain_x` / `Give_trait`」反证） |

### 5·5 明确**不在**本规格里

- ⛔ 不改 `desc` / `descZh` 数据与解析路径（`A1945` 那条纪律照旧）。
- ⛔ 不动 `OfficialFaceTextWhitelist` 的成员（除 §六·4 那条待裁项）。
- ⛔ 不新增 `EvtKind` / 不改 `cards_truth.json` 口径。
- ⛔ 本次**没有**动生成器、没有重生成、没有跑 Unity。

---

## §六 还差什么（如实记）

1. **`effectText` 这条链的引擎侧对应物没查**：只证到原版「键 = `Card_Abilities/<effectText>`」与「缺键原样返回」
   （`GameStaticData.cs:1772-1782`）。**没查**：我们引擎/界面里「buff 效果标题」这一块现在由谁负责、该不该照抄
   （`CardEffect` / `CardEffectItem` 那套 UI 我们有没有对应物）。⇒ 要另开一件查。
2. **`SOR66`（Armorium Cherub）没有出路**：`hasActiveAbility=1`、`Card_Abilities/SOR66` 不存在、
   `activeAbilityTextReferenceId` 也是空 ⇒ 原版走到 `RawCardScript.cs:2443` 的 `throw`。补列救不了它。
   **原版真会崩还是那段是死代码 —— 没有第二个证据**（`B9_W21SKILL` §三·3 已记，本次复核仍成立）。
3. **`Card_Targeted/*` 这条链我们整条没有（51 张池卡）**：`targetingTextReferenceId` 池内 **51/1,128** 非空，
   原版消费点 = `BattleManager._ChooseUnitTarget_d__780__MoveNext`（`decomp_full/BattleManager._ChooseUnitTarget_d__780__MoveNext.c`
   + ILSpy `BattleManager.cs:1784` `GetLocalizedTargetted()`），用于**选目标提示语**（`Card_Targeted/chooseTarget` = `Choose a target`）。
   我们的 `.cs` 里 `Card_Targeted` **0 命中** ⇒ 这块 UI 文案完全没有。
   ⚠️ 是否已在别处以别的形式做过 —— **本次没查**（不该由数据判据代理裁）。
4. **`OfficialFaceTextWhitelist` 里的 `AM72` 疑似应为 `UM72`**：现读 1,351 张里 `AM72` 的 `descRef` 为空、
   `UM72` 的 `descRef` = `UM72_new`。**没查**是笔误还是另一张卡（`AM72` 是否存在于池也需另核）⇒ **留给调度台裁**。
5. **`Card_Abilities/EffectBoltPistol` 是孤儿键**（1,351 张 SO 里 0 引用）：可能是被删掉的卡/机制留下的。
   **没查**它是给谁的 ⇒ 若将来上 `effectText` 链，这条会永远取不到、需要显式决定要不要留。
6. **`GetLocalizedAbilityName` 恒缺键**（`Card_Abilities/Ability_Name_*` **0 条**）：这是**数据缺失**不是我们的实现问题，
   但「技能名」这一屏文案今天必然是空的/回退 —— **没查**我们拿什么显示。⇒ 另立。

---

## 附：复现命令

```bash
cd d:/4/_tmp_view/w55
python -I -X utf8 extract1.py    # §二 真值表 + 覆盖率
python -I -X utf8 extract15.py   # §四 ability 级严格全零 + 卡级 30/17 全表
python -I -X utf8 extract6.py    # §三 30 条 Effect<名> 的归属与挂点路径
python -I -X utf8 extract19.py   # 31 个 effectText 值的全量 + 池内命中
python -I -X utf8 extract12.py   # 90 条 id-ish 键 / cards_engine 列清单
python -I -X utf8 extract16.py   # 6 个 *ReferenceId 的池内覆盖
python -I -X utf8 extract17.py   # 15 张 descRef 卡 旧版 vs 新版 正文
python -I -X utf8 extract20.py   # (( 短路支逐字段命中数
python -I -X utf8 verify_fresh.py  # 缓存 vs 二进制现解 三源核验
# 二进制现解（只读 d:/2，写自己的 tmp）
python -I -X utf8 d:/4/Unity/工具/export_allcards.py --out d:/4/_tmp_view/w55/fresh
python -I -X utf8 d:/4/Unity/工具/export_allcards.py --no-write --check --out d:/4/_tmp_view/w55/allcards_check
```
