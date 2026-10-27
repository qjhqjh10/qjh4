# `cardDescriptionTextReferenceId` —— 10 张池卡印旧版正文：逐卡真值、名字兜底核查与施工规格

> 查判据代理 `W64DESC` · 2026-10-27 · **只读**（未改任何 `.cs` / `.py` / `.json` / 账 / 正本 / 资产；未跑 Unity、未动 git、`d:/2` 只读）
> 数据源：`资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张卡 SO，取 `full_raw.cardDescriptionTextReferenceId`）
> ＋ `MyGame/Assets/CardPresentation/Resources/official_loc_13lang.json`（官方 13 语言表，**列式存储**：`keys` + 13 个平行数组）
> ＋ `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（池 = **1,128** 张 · `version 34`）
> 复现脚本：`d:/4/_tmp_view/w64/desc/e{1..6}_*.py`（只读，`python -I -X utf8` 跑；命令见文末）

---

## §〇 一句话结论（三件会改变既有判断的事）

1. **「10 张印旧版」确认成立**，且**比派活包说的更彻底**：15 张的旧/新正文**在中英两档 15/15 都不同**（不是 10/15），
   其中 5 张（`ASH48` `SAU58` `SAU15` `TAU65` `SOR25`）只是**碰巧**躲在 `OfficialFaceTextWhitelist` 后面。
2. **白名单不是「免疫机制」，它是在补数据选择的 bug**：那 5 张的 `<id>_new` 官方正文**恰好就是**白名单注释里写着「官方缺」的那半句
   ⇒ **该字段是根因，白名单是临时创可贴**。6 条白名单里**只有 `AM72` 仍必需**（它的 `descRef` 为空、没有 `_new` 键）。
   ⇒ **是否顺手摘掉那 5 条白名单 = 调度台裁定**（会改 5 张卡的显示措辞），见 §三。
3. **名字兜底这条【今天在池内一次都不会触发】**：15 张的 `Card_Name/<ref>` 在**全部 13 个语言列里都不存在**，
   而 `Card_Name/<自己 id>` **全部存在**；全池 1,128 张里「主名字键缺失」的只有 1 张（`9218576f… Pray Enchantment`，
   它的 `descRef` 是空串）⇒ **卡名不会印错**，但**原版那条 `Card_Name/<descRef>` 回退我们根本没实现**（见 §二·3）。

---

## §一 15 张逐卡新旧对照（含中文）

### 1·0 字段与消费链（判据出处）

| 事 | 出处 |
|---|---|
| 字段声明 | `d:/2/tools/il2cpp_out/dump.cs:23330` → `public string cardDescriptionTextReferenceId; // 0x250`（属 `RawCardScript`） |
| **正文**取键 | `RawCardScript.GetLocalizedCardDesc()` —— `cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:2317-2350`：**`ref` 非空且不含 `((` ⇒ 键 = `Card_Description/<ref>`；否则 = `Card_Description/<uniqueId>`**。⚠️ **没有「ref 缺键 ⇒ 退回自己 id」这一支** —— 取不到就是取不到 |
| `GetDescAndAbilityRaw` | 同文件 `:2259-2311`（同一套 `ref ?? uniqueId`，再拼 `Card_Abilities/<id>`） |
| **名字兜底** | `GetLocalizedCardName` `:2135-2156` / `GetLocalizedCardNameFull` `:2211-2240`：**主名字键取不到时**才用 `Card_Name/<ref>` / `Card_FullName/<ref>` |
| 我们的现状 | `cards_engine.json` 该列 **0 命中**（`grep -c` = 0）· `CardDef` 无该字段 · 全仓 `.cs` 只在 `RuleEngine/Core/OffensiveCardData.cs:33` 一句注释里提到 |

### 1·1 覆盖面（本次独立复算，与 `W55ALTREF` §1·3 一致）

| 量 | 值 |
|---|---|
| 1,351 张 SO 里**非空** | **44** |
| 在池（1,128 张） | **15** |
| 池内 15 张的取值形状 | **15/15 全是 `<自己 id>_new`**（无例外） |
| 池内 15 张里 `Card_Description/<ref>` 存在 | **15/15**（且 **13 个语言列全非空**，见 §1·3） |
| 池内 15 张里「旧 == 新」（en 与 zh 同时相等） | **0 张** —— 差异率 **15/15** |
| 不在池的 29 张 | 取值**指向别的卡**（`GOF29` / `BL37` / `UM14` … 教程副本、附魔、异画）⇒ 名字兜底那类用户，见 §二 |

### 1·2 逐卡差异（**这 10 张今天印的是左边，应为右边**）

> 下面「今天印的」= `Card_Description/<自己 id>`（现 `FaceTextFull` 恒用卡 id）；「应为」= `Card_Description/<id>_new`。
> **列出来的都是英文列**；中文列逐张另列（差异点不一定同形）。`((…))` / `[[…]]` / `$$…$$` 上屏前由 `CardIcons.RewriteMarks` 展开（§1·4 已核 **所有 token 都能解**）。

| # | 卡 | 卡名 | 今天印的（旧，en） | 应为（`_new`，en） | 差在哪一句 |
|---|---|---|---|---|---|
| 1 | **`TL66`** | Neurogaunt | `[[synapse]]. [[swarm]]. [[tide]] 4` | `[[synapse]]. [[swarm]]. [[tide]] 2` | **Tide 数值 4 → 2** |
| 2 | **`SOR24`** | Simulacrum Celestian | `[[pray]]: ((Give_x_melee_and_y_ranged_to_a_friendly_unit,+1,+1))` | `[[pray]]: ((Give_x_melee_and_y_ranged_to_all_friendly_units,+1,+1))` | **「一支友方单位」→「所有友方单位」** |
| 3 | **`SOR30`** | Celestian Sacresant | `[[pray]]: ((Gain_x,[[armour]] 1))` | `[[pray]]: ((Heal_x_to_a_friendly_unit,3))` | **祈祷效果 获得护甲1 → 治疗3** |
| 4 | **`SOR39`** | Prayer | `… If the unit is Praying gain ((Faith_cost_x,1))` | `… If the unit has [[pray]] gain ((Faith_cost_x,1))` | **触发条件「正在祈祷」→「有祈祷」** |
| 5 | **`UM72`** | Predator Annihilator | `[[armour]] 1. [[codex]]: Deal 4 damage …` | `[[codex]]: Deal 3 damage …` | **伤害 4 → 3**，且**整句 `[[armour]] 1.` 被删掉**（旧版多印了一条能力） |
| 6 | **`SOR47`** | Miraculous Feat | `… ((Faith_cost_x,5)): Extend effect …` | `… ((Faith_cost_x,6)): Extend effect …` | **信仰费 5 → 6** |
| 7 | **`TL24`** | Harpy | `[[flying]]. When a friendly $$TL37$$ dies, deploy another one` | `[[flying]]. When an enemy attacks, deploy a $$TL37$$` | **触发条件整条换掉**（友方死 → 敌方攻击） |
| 8 | **`TL44`** | Hardened Biology | `Give [[armour]] 2 and +2 Health to a friendly troop` | `Give [[armour]] 2 and +1 Health to a friendly troop` | **+2 生命 → +1 生命** |
| 9 | **`UM57`** | Primaris Invader | `[[codex]]: Give ((Ranged_attack_icon_x,+1)) to all your Vehicles` | 同上 **＋ ` this turn`** | **缺「本回合」时限** |
| 10 | **`UM68`** | Primaris Techmarine | `[[codex]]: Give [[armour]] 1 and heal 3 to your Vehicles` | `… and heal 2 …` | **治疗 3 → 2** |

> ⚠️ **`UM72` 那一条要特别说**：旧版官方正文里那句 `[[armour]] 1.` **我们的 `CardDef.Desc` 里没有**
> （`cards_engine.json` = `Deal 3 damage to the enemy with highest Attack`）⇒ 今天卡面**既印错数字、又多印一条本卡没有的能力**。

### 1·2b 另外 5 张（在白名单里 ⇒ **今天不印旧版**，但它们是同一批 `_new` 卡）

> 这 5 张也 **15/15 旧新不同**；只是 `FaceTextFull` 跳过官方表、用我们自己的 `desc`（= 新版语义）⇒ 表面正常。
> 摘不清白名单见 §3·2（调度台裁）。

| 卡 | 今天印的 = 我们的 `desc`（新版语义） | 旧键 `Card_Description/<id>`（**若摘了白名单就会印它**） | `_new` 键 |
|---|---|---|---|
| `ASH48` | `Trigger the abilities requiring Spirit Stones of all your troops. 2 [Spirit Stone]: Repeat this effect` | 只有前半句（**缺 Repeat 那半句**） | 全句（`((Spirit_x,2)) ((Repeat_this_effect))`） |
| `SAU15` | `Remnant. Sniper. Rally: If you control no other troops, gain Stealth` | `[[remnant]]. [[sniper]]`（**缺 Rally 整句**） | 全句 |
| `SAU58` | `Lower the cost of all troops in your deck by 2. Draw a troop` | 只有前半句（**缺 Draw a troop**） | 全句（`((Draw_a_troop))`） |
| `TAU65` | `Strike: Heals 1. Talent: Grisly Feast` | 只有 `[[talent]]`（**缺 Strike 整行**） | 全句（`[[strike]]: ((Heal_1))`） |
| `SOR25` | `Pray: Give +1 [Attack], +1 [Ranged] and Flank to a friendly troop.` | `[[pray]]: Give [[flank]] to another friendly troop`（**缺 +1/+1**，且措辞是「另一个」） | 全句（`+1 melee, +1 ranged and flank to a friendly troop`） |

> 中文列同样逐张不同（例 `SAU58` 旧 zh 缺「抽一张军队牌」、`_new` zh 是「从己方卡组中抽一张军队卡牌」）。
> 完整 en+zh 原件 → `e4.out` 的 `ASH48` / `SAU15` / `SAU58` / `TAU65` / `SOR25` 五段。

### 1·3 中文列逐卡（同一批 10 张）—— **顺带抓到一条独立的语种错**

| # | 卡 | 今天印的（旧 zh） | 应为（`_new` zh） |
|---|---|---|---|
| 1 | `TL66` | `[[synapse]]。[[swarm]]。[[tide]] 4` | `… [[tide]] 2` |
| 2 | `SOR24` | `[[pray]]：((Give_x_melee_and_y_ranged_to_a_friendly_unit,+1,+1))` | `[[pray]]: ((Give_x_melee_and_y_ranged_to_all_friendly_units,+1,+1))` |
| 3 | `SOR30` | `[[vanguard]]。[[armour]] 1。[[pray]]：((Gain_x,[[armour]] 1))` | `[[vanguard]]. [[armour]] 1<br>[[pray]]: ((Heal_x_to_a_friendly_unit,3))` |
| 4 | `SOR39` | `给一个友方单位 [[shield]]。如果该单位正在祈祷，获得 ((Faith_cost_x,1))` | `((Give_x_to_a_friendly_unit,[[shield]])). 如果该单位有[[pray]]，获得((Faith_cost_x,1))` |
| 5 | `UM72` | `[[armour]] 1。[[codex]]：对攻击力最高的敌方单位造成4点伤害` | `[[codex]]：对攻击力最高的敌方单位造成3点伤害` |
| 6 | `SOR47` | `… ((Faith_cost_x,5))：延长效果至你的下一个回合` | `… ((Faith_cost_x,6))：…` |
| 7 | **`TL24`** | 🔴 **`[[flying]]. 아군 $$TL37$$이 죽을 때, 또 다른 하나를 배치합니다`（朝鲜语混进中文列）** | `[[flying]]。当一个敌方单位攻击时，部署一个$$TL37$$` |
| 8 | `TL44` | `… [[armour]] 2 和 +2 生命值` | `… 和 +1 生命值` |
| 9 | `UM57` | `[[codex]]：向你所有的载具赠送 ((Ranged_attack_icon_x,+1))` | `[[codex]]：本回合使你的所有载具获得((Ranged_attack_icon_x,+1))` |
| 10 | `UM68` | `… 并治疗你的载具3点生命值` | `… 并治疗你的载具2点生命值` |

> 🔴 **`TL24` 的旧中文列是朝鲜语**（`아군 … 이 죽을 때, 또 다른 하나를 배치합니다`）——
> 今天中文档的 `TL24` 卡面正文 = `飞行. 아군 孢子雷이 죽을 때, 또 다른 하나를 배치합니다`（**中朝混排**）。
> 这是**独立于语义的第二处缺陷**，改走 `_new` 键会**一并治好**。
> 复现：`e1_pairs.py` 打出的 `OFFICIAL OLD zh` 那一行。

### 1·4 上屏安全性（改走 `_new` 会不会新冒记号 / 新冒缺词）—— **不会**

- **`_new` 正文用到的记号，逐类核过全部可解**（`e3_tokens.py`，源 = 官方 13 语言表）：
  - `((Key,…))` → `Modular_Description/<Key>`：**12 个不同 Key，12/12 在中英两列都有值**
    （`Draw_a_troop` · `Faith_cost_x` · `Gain_x` · `Give_x_melee_and_y_ranged_to_a_friendly_unit` ·
    `Give_x_melee_and_y_ranged_to_all_friendly_units` · `Give_x_to_a_friendly_unit` · `Heal_1` ·
    `Heal_x_to_a_friendly_unit` · `Melee_attack_icon_x` · `Ranged_attack_icon_x` · `Repeat_this_effect` · `Spirit_x`）
  - `[[trait]]` → `Card_Trait/<trait>`：**17 个，17/17 存在**
  - `$$id$$` → `Card_Name/<id>`：**2 个（`TL37` / `b3735ef3…`），2/2 存在**
- **走哪条渲染支**：`CardIcons.Rewrite`（`Core/CardIcons.cs:326`）在 `HasOfficialMarks(text)` 为真时改走
  `RewriteMarks`（`:647` 四支）⇒ **图标计划表 `card_icon_plan.json` 根本不参与**（10 张新正文全带记号）
  ⇒ **不会出现「换了文本、图标计划对不上」**。
- **关键词段**：`CardText.KeywordSegment(c.Keywords, zh, body, …)` 拿**新 body** 判「这个词印过没有」。
  对 `TL66` 是**改善**（今天 body 写 `Tide 4`、`keywords` 是 `Tide 2` ⇒ 判不出已印、会再补一遍；新 body 写 `Tide 2` ⇒ 不重复）。

### 1·5 「应为」的旁证：**这 10 张我们自己的 `desc` 早就是新版语义**

`cards_engine.json` 逐张现读（`e4_engine_vs_official.py`）—— **10/10 与 `_new` 语义一致**，与旧版相反：

| 卡 | `CardDef.Desc`（我们） |
|---|---|
| `TL66` | `Synapse. Swarm. Tide 2` |
| `SOR24` | `Pray: Give +1 [Attack] and +1 [Ranged] to all friendly units` |
| `SOR30` | `Pray: Heal 3 to a friendly unit` |
| `SOR39` | `Give Shield to a friendly unit. If the unit has Pray gain 1 ☀` |
| `UM72` | `Deal 3 damage to the enemy with highest Attack` |
| `SOR47` | `Give [Invulnerable] Invulnerable to a friendly unit this turn.\n6 ☀: Extend effect until your next turn` |
| `TL24` | `Flying. When an enemy attacks, deploy a Spore Mine` |
| `TL44` | `Give Armour 2 and +1 Health to a friendly troop` |
| `UM57` | `Codex: Give +1 [Ranged] to all your Vehicles this turn` |
| `UM68` | `Give Armour 1 and heal 2 to your Vehicles` |

⇒ **是我们的「卡面正文取键」落后于自己的卡数据**，不是卡数据错。改 `_new` 键 = 让卡面与引擎/卡面其它入口自洽。

---

## §二 卡名兜底核查（结论：**池内不会印错，但原版那一支我们没实现**）

### 2·1 原版的确切语义（**是「回退」，不是「覆盖」**）

`RawCardScript.GetLocalizedCardName()`（`out_cpp2il_dev_ilspy/RawCardScript.cs:2135-2156`）：

```
translation = GetTranslation(GetCardNameTerm())
if (translation 为空)                       // ← 只有主键取不到才走
    if (cardDescriptionTextReferenceId 非空)
        text = GetTranslation("Card_Name/" + cardDescriptionTextReferenceId)
```

其中 `GetCardNameTerm()`（`:2171-2188`）= **① `referenceCard != null` ⇒ `Card_Name/<referenceCard.uniqueId>`；
② `refNameId` 非空 ⇒ `Card_Name/<refNameId>`；③ 否则 ⇒ `Card_Name/<uniqueId>`**。
`GetLocalizedCardNameFull()`（`:2211-2240`）同形，只是键族换成 `Card_FullName/`。

### 2·2 15 张的对照表（**`Card_Name/<ref>` 一条都不存在**）

| 卡 | `refNameId` | `referenceCard` | `Card_Name/<自己 id>`（13 列） | `Card_Name/<ref>`（13 列） | 显示名是否受影响 |
|---|---|---|---|---|---|
| `ASH48` `SAU15` `SAU58` `SOR24` `SOR25` `SOR30` `SOR39` `SOR47` `TAU65` `TL24` `TL44` `TL66` `UM57` `UM68` `UM72` | **全 `""`** | **全 null PPtr** | **15/15 × 13 列全部存在** | **15/15 × 13 列全部不存在** | **不受影响** |

- ⇒ 主名字键（= `Card_Name/<自己 id>`）**全都在** ⇒ 原版**不会**去查 `Card_Name/<ref>`；而就算去查，`<ref>` 那条键**本身也不存在**。
- 我们的显示名现读也与官方 `Card_Name/<id>` **15/15 逐字相同**（en + zh；`e2_langcols.py` 末段）。

### 2·3 全池扫描：**回退在池内触发 0 次**；我们的实现里**压根没有这一支**

| 量（1,128 张池卡） | 值 |
|---|---|
| 主名字键（`Card_Name/<refNameId ?: id>`）**缺失** | **1** 张 —— `9218576fd3e26554db5b4c239a36dbc7`（`Pray Enchantment`，我们自己加的隐藏附魔；官方表里没有它的 `Card_Name`） |
| 其中 `descRef` 非空 ⇒ **回退会触发** | **0 张**（那张 `descRef` = `""`） |
| `Card_Description/<id>` 官方缺条目 | **3** 张：`ASH61` · `TL61` · `9218576f…` —— **三张 `descRef` 全 `""`** ⇒ 与本字段无关 |
| 池内 `descRef` 非空但 `Card_Name/<ref>` 存在的 | **0**（15 张全不存在） |

⇒ **结论：卡名不会因为这条字段印错，池内也不存在「回退该救而没救」的卡。**
但**实现层面**：我们的 `CardText.Name(CardDef)`（`Core/CardText.cs:884-898`）三档（中文 `NameZh` / 英文 `Name` /
其余 10 语言 `Card_Name/<c.Id>`）**都没有 `Card_Name/<descRef>` 回退** —— 这是**已知偏离**，
今天**零可见影响**（0 张卡走得到），登记在 §六·1，⛔ 别在这个批次里顺手加（加了也没对象，反而多一条无测路径）。

### 2·4 `Card_FullName/<ref>` 同样不存在

15 张的 `Card_FullName/<自己 id>` 与 `<ref>` **两边都是 `None`**（`e1_pairs.py` 的 `OWN/REF full` 两行）
⇒ 全称这条链对这 15 张**无对象**（`GetLocalizedCardNameFull` 在账里本来也是 D 档：0 外部调用点）。

---

## §三 `OfficialFaceTextWhitelist` 那 5 张为什么「免疫」，以及这个机制能不能被绕过

### 3·1 现机制的精确形状

`BattleDriver.FaceTextFull`（`BattleDriver.cs:27984-28056`）：

```
27995  bool zh = CardText.Zh && !string.IsNullOrEmpty(c.DescZh);   // CardText.Zh = 字体可用 ∧ 当前语档=中文
27996  string body = zh ? c.DescZh : c.Desc;
28010  if (id 非空 && !OfficialFaceTextWhitelist.Contains(c.Id)) {   // ← 白名单只挡这一块
28013      zh                ⇒ Loc.OfficialCurrent("Card_Description/" + c.Id)
28015      Current==中文∧无字体 ⇒ Loc.OfficialEnglish("Card_Description/" + c.Id)
28017      Current==英文     ⇒ Loc.OfficialCurrent("Card_Description/" + c.Id)
28018      off 非空 ⇒ body = off
       }
28023  if (!zh && Current ∉ {中文,英文}) ⇒ body = Loc.Lang13Current("Card_Description/" + c.Id) ?? body   // ← 不挡白名单
```

- 白名单 = **`{ASH48, SAU58, SAU15, TAU65, AM72, SOR25}`**（`:27975-27976`）。
- 那 5 张「免疫」的**真正原因**：它们**跳过官方表、直接用我们自己的 `Desc`/`DescZh`**，
  而我们的 `Desc` 恰好是新版语义（§1·5）⇒ 表面正常。**它们不是「官方表没问题」，是「没读官方表」。**

### 3·2 关键新发现：**白名单注释写的「官方缺这句」已被 `_new` 键推翻**

| 白名单项 | 注释说「官方缺什么」 | `_new` 键**有没有** | 判定 |
|---|---|---|---|
| `ASH48` | 缺 `2 [Spirit Stone]: Repeat this effect` | ✅ 有（`((Spirit_x,2)) ((Repeat_this_effect))`） | **根因已找到 ⇒ 白名单可由 ref 列顶掉** |
| `SAU58` | 缺 `Draw a troop` | ✅ 有（`((Draw_a_troop))`） | 同上 |
| `SAU15` | 缺 `Rally: If you control no other troops, gain Stealth` | ✅ 有（原文逐字相同） | 同上 |
| `TAU65` | 缺 `Strike: Heals 1` | ✅ 有（`[[strike]]: ((Heal_1))`） | 同上 |
| `SOR25` | 只写 `flank`，缺 `+1 近战/+1 远程` | ✅ 有（`((Melee_attack_icon_x,+1)), ((Ranged_attack_icon_x,+1))`） | 同上 |
| **`AM72`** | 官方把 `Strike` 写成 `[[slay]]` | ❌ **`AM72.descRef` 是空串、没有 `AM72_new` 键** | **必须保留**（与 `UM72` 无关，见 §3·4） |

⇒ **两个选项（调度台裁）**：
- **A（零可见变化，推荐先这样）**：白名单**原样保留 6 条**。改 ref 键后这 5 张仍走我们自己的 `desc` ⇒ 玩家看到的字**一个都不变**，风险最小。
- **B（更贴原版）**：摘掉那 5 条、只留 `AM72`。这 5 张会改印官方 `_new` 正文（措辞/标点会变，
  例：`SOR25` 由「另一支友方军队」变「一支友方军队」；`SAU58` 由中文「抽一张军队牌」变官方「从己方卡组中抽一张军队卡牌」）。
  ⚠️ 会把 `Editor/BattleScene.cs:25164-25293` 那一片 ③ 断言**逐条改红**，要同批改断言。

**⛔ 不论选哪个，`AM72` 都必须留** —— 它是**另一张卡**（`Vitus Gryf`，在池，`descRef` 空，
`Card_Description/AM72` = `… [[slay]]: Deploy a $$AM71$$`；我们 `desc` = `… Strike: Deploy a Tempestus Aquilon`）。
`W55ALTREF` §六·4 那条「`AM72` 疑似应为 `UM72`」**已否掉**：那是**两个不同的白名单理由**，不是笔误。

### 3·3 这个机制**能**被绕过 —— 而且今天就绕过了

**绕过口：`BattleDriver.cs:28023-28027` 的「其余 10 种语言」分支 —— 它不查白名单。**

- 现读（`e2_langcols.py` + 直接读德语列）：`ASH48` 的德语旧值 =
  `Löst die Fähigkeiten all deiner Trupps aus, für die Seelensteine benötigt werden`（**没有** Repeat 句），
  德语 `_new` 才有第二句。
- ⇒ 德语/法语/俄语… 档下，**白名单 5 张会印官方旧正文**（= 白名单只覆盖中/英两档）。
- ⚠️ **这不算缺陷**：原版在那些语言里印的就是官方正文（原版没有白名单）。
  这是「我们的白名单是**有意的自我偏离**、而它按设计只作用于中/英」。
  **但它决定了施工规格**：`Lang13Current` 那一路**也必须换成 ref 键**，否则 15 张在 10 种语言里**继续印旧版**。

**其它可能被绕过的路径（逐条查过，均不成立）**：

| 路径 | 会不会绕过 | 依据 |
|---|---|---|
| `CardData.keywords`（`BattleDriver.cs:27907`）/ `CardView.cs:14743` / `RelatedCards.cs:108` | 否 | 全部**转发**到 `FaceTextFull`，是唯一渲染路 |
| `SkillPanel` | 不适用 | 它读的是 `Card_Abilities/`（另一族键），**不读** `Card_Description` |
| `SkillPanel` 类尾 `AbilityRaw` → `Loc.T` | 否 | 只用于 `Card_Abilities/*` |
| `Loc.OfficialCurrent("Card_Description/…")` 直接被别处调用 | 否 | 全仓现读**只有** `FaceTextFull` 4 处 + `Editor/BattleScene.cs` 断言 |
| 13 语言表 `Lang13Current` | **是**（见上） | `BattleDriver.cs:28025` |

### 3·4 顺带坐实：`AM72` 那条白名单**不是**笔误

`e2_langcols.py` 现读：`AM72` = `Vitus Gryf`，**在池**，`descRef = ''`，`Card_Description/AM72_new` **不存在**；
`UM72` = `Predator Annihilator`，`descRef = 'UM72_new'`，**不在白名单**。
⇒ 白名单的 `AM72` 与本次的 `UM72` 是**两张不同的卡、两条不同的理由**。`W55ALTREF` §六·4 结案。

---

## §四 施工规格（**只写规格，本次一个字没改**）

### 4·1 生成器 `工具/gen_cards_engine.py` —— `A1799_FIELDS` 加一列

**位置**：`gen_cards_engine.py:3428` 起的 `(我们的列名, 真值字段, 缺值, 说明)` 表，**表尾追加一项**：

```python
    ("cardDescriptionTextReferenceId", "cardDescriptionTextReferenceId", "",
     "卡面正文取键的 id 那一半：键 = `Card_Description/` + (本列 ?: id)（**没有**『ref 缺键退回自己 id』这一支，"
     "`RawCardScript.cs:2317-2350`；字段 `dump.cs:23330 @+0x250`）。"
     "**还兼任** `Card_Name/<ref>` / `Card_FullName/<ref>` 的名字回退（`:2143-2156` / `:2224-2237`），"
     "但那是**主名字键取不到才走**、且池内 0 张会触发（判据 §二）。"
     "非空 44（全快照）/ **池内 15**，池内 15 张全部形如 `<自己 id>_new`；旧新正文 15/15 实质不同。"
     "判据 → 资料/完全复刻/判据/卡面描述引用id_判据.md"),
```

- **缺值必须用 `""`**（`_dflt == ""` 会让主循环走 `truth_ref_name(r, tf)`，不是 `truth_int`）。
- **两条主循环都会自动带上**：`gen_cards_engine.py:4900-4903`（正常卡）与 `:4264-4265`（`X20` 隐藏附魔的特判路径）
  **都是同一句 `for _col, _tf, _dflt, _doc in A1799_FIELDS`** ⇒ **只改表、不用改循环**（两处会自动一致）。
- **取值源已核实可达**：`load_truth_cards()`（`:1115-1159`）读 `TRUTH_CARDS_SRC`（`cards_raw.json`）并把 `full_raw` 展平
  ⇒ `cardDescriptionTextReferenceId` **在**展平后的行里（`e6_recheck.py` 从同一份文件逐卡读到了 44/15）。
  ⚠️ **它不是** `cards_truth.json` 的字段（那份只有 `abilities` 那套）—— 别去改 `gen_cards_truth.py`。
- **`version` 34 → 35**：`gen_cards_engine.py:5572` 的 `"version": 34`，按既有惯例追加 v35 的注释块。
- ✅ **同批建议**：把 §五 的 `activeAbilityTextReferenceId`（和可选的 `targetingTextReferenceId`）**放进同一次重生成** ——
  每重生成一次就要重钉一次 `PinnedPoolMd5`，分两次 = 白付一次成本。

### 4·2 `RuleEngine/Data/CardDatabase.cs` + `RuleEngine/Core/CardDef.cs`

| 改哪 | 怎么改 |
|---|---|
| `CardDto`（`CardDatabase.cs:33` 起那个类） | 在 `targeted`/`expansionPack` 那一批 `A1799` 字段旁加 `public string cardDescriptionTextReferenceId;`。**⛔ 不加 `[Obsolete]`**（它会有真消费者，且现池 15 张非空） |
| `CardDef` 属性 | **追加到类尾**（⛔ 别插进已有成员之间 —— 会推下游行号、`check_ledger_points` 立刻报漂移）：`public string CardDescriptionTextReferenceId { get; private set; }` |
| `CardDef` 构造 | 既有惯例是**可选形参追加在参数表末尾**（`Core/CardDef.cs:990-1005` 那一串 `int expansion = -1, bool cantBeRandomlyCreated = false, int specialAttackScoring = 0` 就是这么加的）：加 `string cardDescriptionTextReferenceId = null`，体内 `CardDescriptionTextReferenceId = cardDescriptionTextReferenceId ?? "";` |
| `CardDatabase.Parse` 透传 | `Data/CardDatabase.cs:1554` 那条 `targeted: c.targeted,` 旁加 `cardDescriptionTextReferenceId: c.cardDescriptionTextReferenceId,` |
| 旧表守卫 | 照 `:1104-1200` 的惯例加一条 `if (dto.version < 35) Debug.LogWarning("… 没有 cardDescriptionTextReferenceId 一列 ⇒ 卡面正文一律用卡 id 取键（= 今天的行为）");`。**这一列不需要三态**（`""` 与原版「没有 ref」同义） |
| ⛔ 不许做 | 别给它设 `-1` 哨兵（string）、别加 `[Obsolete]`、别改 `cards_truth.json` |

### 4·3 消费端 `BattleDriver.FaceTextFull`（**4 处**，一处也不能漏）

```csharp
// 建议加在 `FaceTextFull` **同一个类里**、且放在类尾（避免推 28013/28015/28017/28025 的行号之外的行）：
public static string OfficialDescKey(string id, string descRef) =>
    string.IsNullOrEmpty(descRef) ? id : descRef;     // 原版 `RawCardScript.cs:2337-2346` 的三元，逐字照抄
```

| 行 | 今天 | 改成 |
|---|---|---|
| `BattleDriver.cs:28013` | `Loc.OfficialCurrent("Card_Description/" + c.Id)` | `Loc.OfficialCurrent("Card_Description/" + OfficialDescKey(c.Id, c.CardDescriptionTextReferenceId))` |
| `:28015` | `Loc.OfficialEnglish("Card_Description/" + c.Id)` | 同上换键 |
| `:28017` | `Loc.OfficialCurrent("Card_Description/" + c.Id)` | 同上换键 |
| `:28025` | `Loc.Lang13Current("Card_Description/" + c.Id)` | 同上换键（**这条最容易漏**：白名单不挡它，10 种语言全靠它） |

- 🔴 **行号是 2026-10-27 19:31 现读的**。⚠️ 落笔时**必须现 `grep`**：
  `grep -n 'Card_Description/" + c.Id' BattleDriver.cs` —— 本仓是**多代理并行**，
  本次查判据期间 `BattleDriver.cs` / `CardDatabase.cs` 的 mtime 就在变（别的写手在改），行号会漂。
  **落点以符号为准**：`BattleDriver.FaceTextFull` 里那 4 句 `Card_Description/" + c.Id`。
- ⚠️ **原版没有「ref 缺键 ⇒ 退回自己 id」**（`RawCardScript.cs:2317-2350`）⇒ 我们的 `OfficialDescKey` 只做 `ref ?: id`，
  **不要**再加「ref 键查不到就回自己 id」那一层（今天 15/15 ref 键都在，加了只是**掩盖**将来数据缺口的静默失败）。
- ⚠️ **`SkillPanel.cs` 那侧不要跟着改**（它用 `Card_Abilities/` 那一族，见 §五）。
- ⚠️ **白名单逻辑不动**（除非调度台选 §3·2 的 B 方案）。

### 4·4 验收口

| 层 | 口 / 怎么断 |
|---|---|
| **数据侧（硬前提）** | 重生成后 `cards_engine.json` 的 `cardDescriptionTextReferenceId` 非空必须 = **15**（池口径）/ 全快照 44；`version` = **35** |
| **正例（新语义，10 张各一条）** | `BattleDriver.FaceTextFull(card).body` **含** `_new` 独有片段、**且不含**旧版独有片段。推荐这 4 条**判别力最强**（片段在旧版里逐字存在）：<br>· `TL66`：含 `Tide 2` / **不含** `Tide 4`<br>· `UM72`：含 `Deal 3 damage` / **不含** `Deal 4 damage`、且**不含** `Armour 1`（旧版多印那条）<br>· `TL44`：含 `+1 Health` / **不含** `+2 Health`<br>· `UM68`：含 `heal 2` / **不含** `heal 3`<br>（其余 6 张同理：`SOR30` `Heal 3` vs `Gain`；`SOR47` 6 vs 5；`TL24` `When an enemy attacks` vs `When a friendly`；`UM57` `this turn`；`SOR24` `all friendly units` vs `a friendly unit`；`SOR39` `has [[pray]]`/`有[[pray]]` vs `is Praying`） |
| **中文档同批** | 同上 10 张在 `Loc.RestoreForTest(Chinese)` 下各断一条中文片段（`+1 生命值` / `heal 2`→`治疗你的载具2点` / `Tide`→`潮汐 2` …）；`TL24` 额外断 **`不含 "아군"`**（朝鲜语残留那条） |
| **反例 ①（ref 为空必须仍用卡 id）** | 取一张 `descRef == ""` 的卡（例 `UM44`，现有断言 `BattleScene.cs:25102` 已在用）：正文必须仍等于 `Card_Description/UM44` 的渲染值 |
| **反例 ②（白名单仍生效）** | `ASH48` 英文正文仍含 `Repeat this effect`；`AM72` 仍含 `Tempestus Aquilon` 且不含 `Slay`（现有 `:25172` / `:25254`） |
| **灭自证（必须有）** | 对那 10 张**再断一条相反的**：用**旧键**算出来的正文（`CardIcons.Rewrite(c.Id, "desc", Loc.OfficialEnglish("Card_Description/"+c.Id))`）**必须 ≠** 实得 body。否则「把 `OfficialDescKey` 改回 `c.Id`」这种情况会**静默变绿**（旧键文本仍在官方表里，`⑤⑥` 全池覆盖照旧会红，但局部抽查不会） |

### 4·5 🔴 **改这 4 处会打红 3 处既有断言 —— 必须同批改**

| 位置 | 为什么会红 | 怎么改 |
|---|---|---|
| **`Editor/BattleScene.cs:25106`** | `new[] { "TL24", "dies, deploy another one", … }` —— 它断「`FaceTextFull` 含 **旧**官方片段 `dies, deploy another one`」。改 ref 后 `TL24` 印 `_new`（`When an enemy attacks…`）⇒ **必红** | 把这一行换成 `_new` 独有片段（例 `"When an enemy attacks"`），并把 `row[2]` 的理由改成「官方 `_new` 键 vs 旧键」。⚠️ 同时注意 `:25115` 那条判别式断「该片段**不在** `CardDef.Desc` 里」—— `TL24` 的 `Desc` 恰好也是新语义（`When an enemy attacks, deploy a Spore Mine`）⇒ **`TL24` 不能再用作「官方 vs 我们」的判别式样本，建议整行换成别的卡，或把判别式改成「旧键文本 ≠ body」** |
| **`Editor/BattleScene.cs:25336-25339`（⑤ 英文档全池覆盖）** | `want = CardIcons.Rewrite(c.Id, "desc", Loc.OfficialEnglish("Card_Description/" + c.Id))` —— **拿旧键算期望**。10 张会 `body` 不含 `want` ⇒ **`covered != total` 红** | `want` 改用同一把键：`Loc.OfficialEnglish("Card_Description/" + BattleDriver.OfficialDescKey(c.Id, c.CardDescriptionTextReferenceId))`（**与生产代码共用同一个 helper，⛔ 别在断言里再写一遍三元**） |
| **`Editor/BattleScene.cs:25365-25368`（⑥ 中文档全池覆盖）** | 同上（`Loc.OfficialCurrent` + `c.Id`） | 同上 |

- ✅ **不会红的两处**：`④ desc/descZh 数据 hash = 0x152CA48F`（`:25308-25322` 只吃 `id/desc/descZh`，本次不动数据）；
  白名单 ③ 那 5 段（`ASH48`/`SAU58`/`SOR25`/`AM72` —— 它们绕过官方表，与键无关）。
- ⚠️ 白名单 ③ 里 `SAU58` 那条判别式 `OfficialEnglish("Card_Description/SAU58").IndexOf("Draw a troop") < 0`（`:25194`）
  **指的是旧键**，改 ref 后**仍然成立**（旧键确实缺那句）⇒ **别顺手把它改成 ref 键**，否则会红（`_new` 里有 `Draw_a_troop`，渲染后正是这句）。

### 4·6 🔴 重生成会动**池指纹** —— 只能调度台在「无 `.cs` 写手 + 无账写手」窗口做

| 指纹 | 现在 | 重生成后会怎样 |
|---|---|---|
| **`PinnedPoolMd5`**（`工具/ruleprobe/Program.cs:251`） | `c2ee753d96ad11ea548843c861fd8890` | **必变**（它按字节 md5 `cards_engine.json`；加一列 + 改 `version` ⇒ 整文件变）⇒ **必须重钉**，并在注释里写清「v34→v35 加了一列、纯数据面扩展」 |
| `PinnedParseMd5`（`:350`） | `c9a647982fad19bb65896011c281c93e` | **应不变**（解析只吃效果字段）；**若变 ⇒ 说明别的东西动了，停下查** |
| `PinnedBaselineMd5`（`:285`） | `ce3faf91f9634053e00f0c97e602bb54` | **应不变**（不重生成基线） |
| `PinnedPlayMd5`（`PlayProbe.cs:3852`） | `0766fbe1f712696f4e57d2b7cfeb8f38` | **应不变**（纯数据列，无引擎行为） |

⇒ **动作顺序**（铁律 13·3 / 16·8）：① 停所有 `.cs` 写手与账写手 → ② 改生成器 + `version` → ③ 重生成 →
④ `PinnedPoolMd5` 重钉 + 注释 → ⑤ `ruleprobe check` / `play check` 跑一遍确认另三颗指纹没动 → ⑥ 再放写手进来。
⚠️ **本次一个字都没改**，上面是规格。

### 4·7 明确**不在**本规格里

- ⛔ 不动 `desc` / `descZh` 数据与解析路径（`A1945` 纪律照旧：显示层只换渲染串）。
- ⛔ 不动 `OfficialFaceTextWhitelist` 成员（除 §3·2 那个要调度台裁的项）。
- ⛔ 不改 `SkillPanel`（那是 §五 的另一个批次）。
- ⛔ 不加 `Card_Name/<ref>` 名字回退（0 张卡走得到，见 §六·1）。

---

## §五 「同批」清单（同族、该一起做、什么时候做）

### 5·1 三条 `*ReferenceId` 的真实规模（本次独立复算，源同 §1·1）

| 字段 | 全快照非空 | **池内非空** | 消费点 | 我们的现状 |
|---|---|---|---|---|
| **`cardDescriptionTextReferenceId`** | 44 | **15** | `BattleDriver.FaceTextFull` **4 处** | 字段无、列无 ⇒ **本批** |
| **`activeAbilityTextReferenceId`** | 3 | **2**（`SOR30` / `SOR25`） | `SkillPanel` 的 `Card_Abilities/<id><suffix>` 三档链（`SkillPanel.cs:2133-2163` / `2187-2210`） | 字段无、列无 ⇒ **同批（同一次重生成）** |
| **`targetingTextReferenceId`** | 51 | **51**（可达 **50** = `cardType==0 ∧ targeted==1`） | `Loc.TargetedText` / `TargetedKey`（`Core/Loc.cs:3769-3800`，**API 已建好**） | 字段无、列无、**调用点 0** ⇒ 数据列同批、接线另批 |
| `cardRaceTextReferenceId` | **0** | **0** | — | **无对象，不做**（原版本身无值 = 铁律 11 例外①） |
| `chatMessagesReferenceId` | 8 | **0** | 聊天/召唤台词链 | 与聊天线绑定，**不是本批** |
| `customIntroReferenceId` | 7 | **0** | 对战开场白 | 同上 |

### 5·2 建议的批次划分

| 批 | 内容 | 为什么同批 / 不同批 | 时机 |
|---|---|---|---|
| **A（数据列，一次重生成）** | ①`cardDescriptionTextReferenceId` ②`activeAbilityTextReferenceId` ③（可选）`targetingTextReferenceId` | 三列**都只改生成器 + `CardDatabase` DTO + `CardDef`**，共用同一个 `A1799_FIELDS` 表、**共用一次 `PinnedPoolMd5` 重钉**。分三次 = 白付两次重生成 + 两次停写手 | **本批**（调度台无写手窗口） |
| **B（消费端，`.cs`）** | ①`FaceTextFull` 4 处换键 + `BattleScene` 3 处断言（§4·5）②`SkillPanel` 把 `cardId` 换成 `altRef ?: cardId`（只有 `AbilityDescription` 一个入口，`SkillPanel.cs:2133/2187/2202` 三处拼键，**建议在 `AbilityDescription` 开头算一次**、别在三个拼键处各写一遍 —— 工程红线「两处写同一条规则」） | 与 A **可以并行**（文件不相交：A 动 `gen_cards_engine.py`/`CardDatabase.cs`/`CardDef.cs`，B 动 `BattleDriver.cs`/`SkillPanel.cs`/`BattleScene.cs`），但 **B 的断言要等 A 重生成完才能跑绿** | A 之后（或 A 重生成完再跑自检） |
| **C（`Card_Targeted` 接线）** | 把 `Loc.TargetedText` 接到「选择目标」那条 UI（`Loc.TargetedText` 现读 **0 调用者**） | 需要先在 `BattleDriver` 找到 `ChooseUnitTarget` 对应物并定「提示语放哪个控件」⇒ **有依赖链**，不是纯数据 | 另立（判据 → `B9_W61TARG_卡牌目标文本.md`） |
| **D（`effectText` 链，31 值 / 33 挂点）** | `abilityLogic[].buffEffect.effectText` / `handBuff.cardEffect.effectText` → `GameStaticData.GetEffectDesc` → `Card_Abilities/<effectText>` | **不是卡级列**（挂在每个 logic 格上）；且要先把 `CardEffect` 的引擎侧对应物定下来 ⇒ **依赖链最长，单独一批**。⛔ 别塞进 A（会让 A 的列结构变形） | 排在 C 之后 |

### 5·3 一句话
**A 与 B 是本轮「10 张卡面正文」的完整闭环；C/D 是同一族但**各自有独立依赖**，
⛔ 不要因为「都叫 `*ReferenceId`」就把它们塞进同一次重生成**（C/D 改动的是不同的列结构）。

---

## §六 还差什么（如实记）

1. **`Card_Name/<descRef>` 名字回退我们没实现**（`CardText.Name(CardDef)` 三档都没有这一跳）——
   今天池内 **0 张卡**走得到（§2·3），所以**不是当前缺陷**；登记为已知偏离。
   ⚠️ 将来若把「教程副本 / 附魔 / 异画」那 29 张 GUID 卡**放进池**，这条回退就会开始有对象（它们的 `descRef` 指向正规卡 id，
   且其中 **6 张的 `Card_Name/<ref>` 不存在**：`4604f2de…` / `da8a1ba8…` / `59a4d20e…` / `2aa80269…` / `d34f1b51…` / `9f7068f0…` —— 都是 `Infinite Biomorphologies` 那族）。
2. **§3·2 的 A/B 方案要调度台裁**（白名单 5 条摘不摘）。我**没有**擅自定：
   摘了会改 5 张卡的显示措辞、并打红 `BattleScene.cs:25164-25293` 一整片断言。
3. **`TL24` 旧中文列是朝鲜语**（§1·3）—— 这是**官方表的数据缺陷**，改走 `_new` 会一并绕开；
   ⛔ 但**不要**去改官方表那一格（`d:/2` 只读、而且那是「原版就是错的」一类，照抄原则见 CLAUDE.md §三 最后一条）。
4. **`SOR24` 的 SO `cardName` = `Simulacrum Imperialis`，官方 `Card_Name/SOR24` = `Simulacrum Celestian`** ——
   我们显示的是**官方那个**（`cards_engine.json` `name` = `Simulacrum Celestian`），
   而 PnP 目录里确有 `Sorotitas/3部队/Warpforge_30_Simulacrum-Celestian.png` ⇒ **我们是对的**。
   本条只为消歧（免得下个会话把它当缺陷），⛔ 不要改。
5. **§4·5 那 3 处断言的具体新文字**我只给了方向（``TL24` 那一行建议整行换卡）；
   写手落笔时要按「灭自证」重选判别式 —— **`TL24` 不能再当「官方 vs 我们」的样本**（两边都写 `When an enemy attacks`）。
6. **账里 `RawCardScript.GetLocalizedCardDesc()` 那一行（`账/战斗逻辑驱动.tsv:1232`）的落点写的是
   `CardPresentation/Core/Loc.cs:3421（T）`** —— 改完 `FaceTextFull` 后，这一行的落点应改指
   `CardPresentation/Battle/BattleDriver.cs` 的 `FaceTextFull`（B 档，等价实现）。
   ⚠️ 改账要**行号现读**（本次不动账）。同族还看 `:1221`（`GetCardNameTerm`，A 档）/
   `:1235`（`GetLocalizedCardNameFull`，D 档）/ `:1241`（`GetLocalizedTargetted`，B 档）。

---

## 附：复现命令（全部只读，路径写死在脚本里）

```bash
cd d:/4/_tmp_view/w64/desc
python -I -X utf8 e1_pairs.py           # §一 15 张旧/新 en+zh + 名字/全称两族 + 44 条全表
python -I -X utf8 e2_langcols.py        # §一·3 13 语言列覆盖 · §二 refNameId · §三 AM72/UM72 事实 · 名字对照
python -I -X utf8 e3_tokens.py          # §一·4 记号可解性（Modular_Description / Card_Trait / Card_Name）
python -I -X utf8 e4_engine_vs_official.py   # §一·5 我们的 desc vs 旧/新（含渲染模拟）→ e4.out
python -I -X utf8 e5_namefallback.py    # §二 名字回退扫描（池内 0 次触发）+ 非池 29 张 ref 键存在性
python -I -X utf8 e6_recheck.py         # §一/§五 六条 *ReferenceId 的池内规模 + Card_Targeted 31 键
```
