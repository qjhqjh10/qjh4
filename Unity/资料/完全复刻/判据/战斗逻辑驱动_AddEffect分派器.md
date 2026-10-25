# 战斗逻辑驱动 · `CardScript.AddEffect` 分派器 —— 可认领判据块

> **产出**：F 判据代理（第 2 轮），2026-10-25。只读代码 + 只写本文件，⛔ 没改 `.cs` / `.tsv` / 账。
> **用途**：把账里 `战斗逻辑驱动.tsv:770`（`CardScript.AddEffect(CardEffect, int)`，状态 **B**）这一行拆成 **12 个互不重叠的判据块**，供下一批执行代理各自认领升 A。
> **对应账行**：`战斗逻辑驱动.tsv:770`（1 行，`B`，`P2`，轮次 `R7`）。拆块后建议在**备注**列写「K0–K11 见判据文件」，不必新增行（避免一处两账）。
>
> 🔴🔴 **2026-10-25 第二轮更正（Q1 复核 · 铁律 5）—— 本文件第一版有三处硬错，已就地改完，下面正文都是改后的**：
> ① **池内计数整体是 2 倍** —— 第一版把 `cards_raw.json` 的 `full_raw`（同一棵 SO typetree 的副本）与摘要树各数了一遍 ⇒ **每格恰好翻倍**。第二版曾「逐格除 2」（`markOfChaos` 58→**29** · `choiceEnchantment` 26→**13** · `addTrait` 738→**369** · 合计 2,386→**1,193**）——**但那仍有残差，见 ④**。
> ② **`11 removeExtrinsic` 不是「池内 0」，是可达** —— 它**不经 SO 数据**，是**代码构造**（`AbilityLogic__PlayAbility.c:1246`，门 = `trigger=240 WhileInPlayVariable ∧ chosenAbility=30 buffOthers`），**池内 32 条**。⇒ 它在 §三 的排序从**第 10 位提到第 2 位**（见 K11）。
> ③ **`20 doubleAttack` 的措辞改了** —— 不是「一定会被改写成 addStats」（那暗示可达），而是「**全库没有任何地方产出 20**」⇒ `.c:198-207` 那条改写在原版里**永不触发**。
> ④ 🆕 **第三版（Q2 复核 · 本轮）：统计源整体换成 `cards_truth.json`** —— `cards_raw.json` 每张卡**同时存两份同构技能树**（解析视图 `cardAbilities` ＋ `full_raw.cardAbilities`）⇒ **任何递归扫整份卡的统计都恰好 ×2**。第二版「逐格除 2」**只压掉主项、仍有残差**：`addStats` **501→507** · `addTrait` **369→368** ⇒ **合计 1,193→1,198**。**本文件所有池内次数已按 `cards_truth.json` 重算。**
> 📌 **改完之后**：12 档「0」不受影响；**K 块的相对顺序不变**；**引用池内次数一律以 §〇·三 为准**（合计 **1,198**）。
> 📌 **方法学见 §〇·五** —— 「两条路都要走」＋「统计一律用 `cards_truth.json`」＋「三个张数口径别互相校验」。

---

## 〇、三份反编译怎么配合（先读这节，别重复踩）

| 角色 | 文件 | 行号 | 能不能当判据 |
|---|---|---|---|
| 🥇 **第一权威（分支 + 数值 + 字段偏移）** | `D:/2/tools/decomp_full/CardScript__AddEffect.c`（**974 行**） | **本文件全部 `.c:NNN`** | ✅ 能用。`switch` 用 **buffType 原值**，常量保留（`0x28`/`0x3cf`/`0x60`…），且**我们工程既有注释引的全是这套行号**（`AddEffect.c:403/475/635/680/776/823/826` 逐条对得上） |
| 🥈 **分支骨架（可读的 C#）** | `D:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/CardScript.cs` `:10663-11746`（**1084 行**） | 本文件 `i2c:NNNN` | ⚠️ **只作骨架**。`switch (buffType - 1)` ⇒ **case 号 = buffType − 1**（`.c` 的 case 号 = buffType）。枚举映射局部丢失、条件里出现未赋值局部变量 |
| 🥉 **别用**（这块丢条件） | `out_cpp2il_dev_ilspy/CardScript.cs` `:17407-18231`（**825 行** = 任务说的「824 行」） | `ilspy:NNNN` | ❌ 已成 **goto + 二叉 if 树**，条件被吃掉（`:17586` 那段连 `addStats` 都判反了）；**只在要交叉核对某一个 if 时读单行** |

**读常量**：`_DAT_xxxxxxxx` → `d:/4/Unity/工具/read_literal.py`（RVA = 地址 − `0x180000000`，PE 段表，4/8 字节两档都打；宽度按上下文选）。用法与自检见 `资料/战斗规则与数值_出处.md` §三。
**字段偏移**（`.c` 里全是裸偏移，先按此表读）：`param_1` = 被加 buff 的 `CardScript`，`param_2` = `CardEffect`。

| `CardEffect` 偏移 | 字段 | `.c` 里的写法 |
|---|---|---|
| `+0x18` | `enchantingCard` | 施放者 `CardScript*` |
| `+0x30` | `extrinsic` | `*(char*)(param_2+0x30)` |
| `+0x31` | `extrinsicVariable` | `:0x31` |
| `+0x38` | **`buffType`** | 分派键 |
| `+0x3c` | `trait`（`DefinedTrait`） | 见 §〇·一 |
| `+0x40` | `traitValue` | |
| `+0x48` | `meleeAttackChange` | |
| `+0x4c` | `rangeAttackChange` | |
| `+0x50` | `armourChange` | |
| `+0x54` | `healthChange`（addStats 用） | |
| `+0x58` | `costChange` | |
| `+0x60` | `healthChange`（setHealth/setCurrentHealth/swapStats 用） | 与 `+0x54` **同名不同位**，勿混 |
| `+0x88` | `enchantment`（`RawCardScript*`） | |
| `+0x90` | `enchantment.spellType` | `:853`/`:856`/`:878`/`:895`/`:916` |
| `+0x98` | `baseEnchantmentCard` | |

被加方 `CardScript`：`+0x108` = `activeEffects`（`List<CardEffect>`）· `+0x110` = `activeEnchantments` · `+0x118` = `activeAbilityRef` · `+0x220` = `hasActiveAbility`+`hasChangedActiveAbility`（两字节，`= 0x101` 一起置 1）· `+0x228` = `cardState` · `+0x290` = `CardUI` · `+0x600` = `BattleMgr` · `+0x68..0x78` = `currentHealth`（`ObscuredInt` 5 字）· `+0xcc..0xdc` = `currentDropPodHealth` · `+0xe0..0xf0` = `currentDropPodAttack`。

### 〇·一 `DefinedTrait` 常量真值（`.c:475-645` 全是裸数字）
判据 = `out_cpp2il_dev_ilspy/DefinedTrait.cs`（**十进制枚举，137 行**）。本方法用到的：

| 十六进制 | 十进制 | 名 | 十六进制 | 十进制 | 名 |
|---|---|---|---|---|---|
| `0x28` | 40 | `fast` | `0x3ca` | 970 | `pindown` |
| `0x1cc` | 460 | `flank` | `0x3cf` | 975 | `blind` |
| `0x46` | 70 | `stealth` | `0x492` | 1170 | `markerlight` |
| `0x50` | 80 | `shield` | `0x49c` | 1180 | `companion` |
| `0x82` | 130 | `jam` | `0x4ec` | 1260 | `huntMark` |
| `0xe6` | 230 | `dropPod` | `0x262` | 610 | `pack` |
| `0x1ae` | 430 | `survivor` | `0x258` | 600 | `resistant` |
| `0x38e` | 910 | `tide` | `0xc8` | 200 | `poisoned` |

### 〇·二 `BuffType` 全表（判据 = `out_cpp2il_dev_ilspy/BuffType.cs`）

`0 none · 1 addStats · 2 changeCost · 3 addTrait · 4 removeTrait · 5 swapStats · 6 changeRace · 7 skipCardDraw · 8 markOfChaos · 9 setHealth · 10 changeAbilityCost · 11 removeExtrinsic · 12 enchantment · 15 singleUseEnchantment · 16 choiceEnchantment · 17 lastMarkOfChaos · 20 doubleAttack · 25 ephemeralCopy · 26 tideCopy · 30 addRace · 35 daemonHost · 40 replaceAbility · 45 addStatsDropPod · 70 setMeleeAttack · 85 setRangeAttack · 90 addTraitRemnant · 95 setCost · 100 setCurrentHealth`

> ⚠️ 全表 **28 个成员**（含 `none = 0`）⇒ **非 none 共 27 个**。
> 🔴 **账 `战斗逻辑驱动.tsv:770` 的备注写「`BuffType` 全表 **29 值**」是错的**（多算了 2 个，`BuffType.cs` 只有 28 行成员）—— 本文件按 **28 / 27** 写。

### 〇·三 🔴 **池内可达性（2026-10-25 **第三版** —— 改用 `cards_truth.json` 重算）**

> 🔴 **更正 ①（Q1 复核）**：第一版把 `cards_raw.json` 的 `full_raw`（**同一棵 SO typetree 的完整副本**）与摘要树**各数了一遍** ⇒ **每一格恰好翻倍**。第二版「逐格除 2」**只压掉了主项、还有残差**。
> 🔴 **更正 ④（Q2 复核 · 本轮）**：**根因比「各数一遍」更彻底** —— 每张卡**同时存了两份同构技能树**：**解析视图 `cardAbilities`** **和** **`full_raw.cardAbilities`** ⇒ **任何【递归扫整份卡】的统计都恰好 ×2**（实证：`buffType==95` 递归数出 **6** = 解析视图 3 + `full_raw` 3；`cards_truth.json` 是 **3**）。
> ⇒ **本表已改用 `MyGame/Assets/RuleEngine/Resources/cards_truth.json`（每卡一份、无 `full_raw`）重算**。残差 = `addStats` **501→507（+6）** · `addTrait` **369→368（−1）** ⇒ 合计 **1,193→1,198**。
> 🔴 **更正 ②**：`11 removeExtrinsic` 原记「池内 0」—— **错**。它**不经 SO 数据**，而是**代码构造**（`AbilityLogic__PlayAbility.c:1246` 写 `buffType = 0xb`，门 = `trigger=240(WhileInPlayVariable)` ∧ `chosenAbility=30(buffOthers)`）⇒ **池内 32 条**（UM107 / TAU19 / SAU36 / UM14 / DAU27…）。见下表末行。
> 🔴 **更正 ③**：`20 doubleAttack` **不是「一定会被改写成 addStats」**（那措辞暗示它可达）—— **全库没有任何地方产出 `buffType = 20`** ⇒ `.c:198-207` 那条改写在原版里**永不触发**。

判据 = **`cards_truth.json`（1,351 张，每卡一份）** ＋ **代码构造点**（Q1 取证）。
**非 0 共 1,198**（= 表内逐格之和，已两次独立核对：Q2 报 1,198 · 本地重算 1,198）。⚠️ `cards_truth.json` 里**没有 `buffType = 0` 的条目**（只存有意义的 buff）⇒ 不存在「一万个空占位」那一档。

> 🔴 **统计口径（2026-10-25 `N13` 抽核后补，已现算复核 14/14 吻合）**：必须扫 **整卡** ——
> `buffType` 不只藏在 `abilities` 里：`activeAbility`（**143 张**卡有）与 `alternativeAbility`（**2 张**）里也有。
> 只扫 `c["abilities"]` 会**少 73 条**（`addStats` 少 46 · `changeCost` 少 5 · `addTrait` 少 21 · `removeTrait` 少 1），
> 合计会从 **1,198 掉到 1,125** ⇒ **别据此判「本表错了」**。
> 复现：对 `cards_truth.json` 的**每个 card 对象**递归，不是只对 `c["abilities"]`。

| buffType | 命中 | 示例卡 | buffType | 命中 | 示例卡 |
|---|---|---|---|---|---|
| 1 addStats | **507** | EC16 Screamer Kakophonist | 16 choiceEnchantment | **13** | TL49 Hyper-adaptation |
| 2 changeCost | **139** | BL27 Venomcrawler | 17 lastMarkOfChaos | **1** | BL72 Accursed Helbrute |
| 3 addTrait | **368** | ASH12 Swooping Hawk | 25 ephemeralCopy | **1** | TL69 Neurotyrant |
| 4 removeTrait | **14** | DA27 Ravenwing Talonmaster | 70 setMeleeAttack | **1** | SAU27 Scarab Swarm |
| 8 markOfChaos | **29** | EC41 Internal Rivalry · BL30 Chaos Lord | 90 addTraitRemnant | **2** | SAU36 Nemesor Zahndrekh |
| 12 enchantment | **75** | AM51 Cadia Stands | 95 setCost | **3** | GSC39 Lying in Wait · TAU68 Trail Shaper · TL69 Neurotyrant（**三条 `costChange` 全是 1**） |
| 15 singleUseEnchantment | **44** | DA42 Reconnaissance Mission | 100 setCurrentHealth | **1** | EC39 Eternal Servitude |
| **11 removeExtrinsic** | **32**（🔴 **代码构造，非 SO**） | UM107 · TAU19 · SAU36 · UM14 · DAU27 | —— | —— | —— |

**池内 0 命中（12 个）**：`5 swapStats · 6 changeRace · 7 skipCardDraw · 9 setHealth · 10 changeAbilityCost · 20 doubleAttack · 26 tideCopy · 30 addRace · 35 daemonHost · 40 replaceAbility · 45 addStatsDropPod · 85 setRangeAttack`

⚠️ **三条必须同时记住的限定**：
1. **`20 doubleAttack` 的 0 是真 0** —— 全库**没有任何地方产出 20**（Q1 实测），所以 `.c:198-207` 的改写在原版里**永不触发**。⇒ 我们的 `double` op（`DoDouble`，语义是「×2」）**不是**它的对应物，别混。
2. **两条路都要查**：① **SO 数据**（`abilities[].logic[].buff` 这一路，本表大部分）；② **代码构造**（`new CardEffect{…}` / `AbilityLogic.PlayAbility` —— `11 removeExtrinsic` 就是典型）。**只查 ① 会漏掉 11**。
3. 这张表是**可达性**，不等于「玩家一定会遇到」—— 但按铁律 11，「有卡用 = 要做」，只有先后之分。

### 〇·四 🔴 账里已经有一条**错**（就地记，供落盘时改正）
`战斗逻辑驱动.tsv:770` 的备注写「**8 markOfChaos ⛔ 未建（池内 0 张）**」。
**实据**：卡 SO 去重后 `buffType == 8` 命中 **29 次**（原写 58 是「含 `full_raw` 重复」的两倍口径 — 见 §〇·三 更正①），示例 `EC41 Internal Rivalry` / `BL30 Chaos Lord` / `BL72` / `Warhammer` 族多张。
⇒ 一半对（**我们确实未建**）、一半错（**池内不是 0 张**）。同类还有 **16 choiceEnchantment（13 次）**、**11 removeExtrinsic（32 条，代码构造）** 也只写了「未建」没写可达。

### 〇·五 方法学（Q1 这套**可复用** —— 查「某个 `CardEffect` 字段的值从哪来」）

1. **先问「谁写这个字段」**：全库只有 `CardEffect` 一个类有 `BuffType` 字段（`CardEffect.cs:28`）⇒ 只有**两条路**：**SO 数据** 或 **代码 `new CardEffect{…}` 构造**。⛔ **只扫数据就说「池内 0」= 必然漏**（本轮 `11 removeExtrinsic` 就是这么漏的）。
2. **数据那条路：统计一律用 `cards_truth.json`；用 `cards_raw.json` 时必须【跳过 `full_raw`】。**
   `cards_raw.json` 每张卡**同时存两份同构技能树**（解析视图 `cardAbilities` ＋ `full_raw.cardAbilities`）⇒ **任何递归扫整份卡的统计都恰好 ×2**（实证：`buffType==95` 递归数 6 = 3 + 3，`cards_truth.json` 是 3）。
   🔴 **三个张数口径别互相校验**：**SO 总数 1,351** / **`cards_engine.json` 的 `count` 1,128** / **`ruleprobe check` 基线 1,121** —— 三者本来就不是同一个集合（SO 含未进池的、engine 是筛过的、基线是能离线跑通的）。拿它们互相「对不上就报错」会一直假报警。
3. **代码那条路**：`.c` 里用**类型指针 `DAT_1842d44f8`**（`CardEffect` 的 il2cpp 类型）能定位出 **14 个分配点**；其中**真正写 `buffType` 的只有** `= 1 / 3 / 8 / 0xb / 0xc / 0x19`（其余是纯拷贝）。⇒ **这 6 个值一定可达**，与 SO 数据是否存在无关。
4. 判「池内 0」之前，**两条路都要走完**，并写清「搜过哪条、关键词是什么」（铁律 2 / 方案 §三·2）。
5. **凡是「枚举有几个值」**：读**签名桩**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs`，带显式 `= 值`）或 ILSpy 里带值的形态。⛔ 别数 ILSpy 的**无值裸列表** —— 本轮 `PlayerDataKey` 就是数裸列表多数了一个（52 成员 / 51 数据 key）。

---

## 一、分派总图（`.c` 行号）

```
67-78   ① 前置闸 A：cardState ∈ {5, 6, 0xb(removedFromGame)} → LogWarning + return false
80-93   ② 前置闸 B：newEffect == null → 跳到尾部（CheckPossibleCombinations + return true）
94-100  ③ 前置闸 C：enchantingCard ≠ null ∧ HasCurrentTrait(jam 0x82) ∧ extrinsic → return false
100-157 ④ 前置闸 D：extrinsic == true → ContainsExtrinsicEffectFrom(...) 的整棵去重树
158-170 ⑤ 前置闸 E：残骸门（isRemnant +0x65）—— 只放行 addStats(带 healthChange) 与 addTraitRemnant(90)
171-195 ⑥ 前置闸 F：施放者不在场门（Minion 且 cardState ∉ {2,15,3,17}）→ LogWarning + return false
198-207 ⑦ doubleAttack(0x14=20) → 就地改写 buffType=1 + meleeAttackChange=CurrentMeleeAttack（<0x3fffffff 才写）
208-247 ⑧ buffType > 0x2d(45) 的二叉 if：70/85 → 90 → 95 → 100 → 其余 no-impl
248-275 ⑨ 0x23(35) < buffType ≤ 45：40 replaceAbility / 45 addStatsDropPod / 其余 no-impl
330-969 ⑩ switch (buffType)：见下表逐块
970-974 尾：UpdateFigures → CheckPossibleCombinations → return true
```

`switch` 的 case 号 = **buffType 原值**（`.c:330` 是 `switch (iVar16)`，`iVar16 = *(int*)(param_2+0x38)`）。
⚠️ `i2c` 版是 `switch (buffType - 1)`，**行号不能直接互抄**（见 §〇 表）。

---

## 二、12 个可认领块

> 每块列：**分支条件（原文摘录 + `.c` 行号）｜枚举/常量真值｜我们引擎落点（`文件:行号`）｜现状｜置信度｜归属**
> 现状四档：`✅已对齐（判据逐句核过）` / `🟡有实现未逐句核` / `⛔缺` / `⚪原版自身无行为`（后者可记 D，必须引用「原版这里也 LogWarning」那一行）

### K0 —— 前置五道闸 + extrinsic 去重树（所有 effect 的必经之路）
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:67` `if ((*(int*)(param_1+0x228) - 5U < 2) \|\| (*(int*)(param_1+0x228) == 0xb))` → `LogWarning` + `return 0`（**收牌状态 5=waitingToBePlayed / 6=waitingToDie / 11=removedFromGame 不受 buff**）。`.c:94-98` `if (HasCurrentTrait(enchantingCard, 0x82 jam) && extrinsic) return 0`（**被 jam 的施放者不能再发 extrinsic 效果**）。`.c:100-190` `extrinsic` 整棵去重树：`ContainsExtrinsicEffectFrom(self, enchantingCard, buffType, baseEnchantmentCard)` 为真时——① `buffType == 0xb(removeExtrinsic)` 走移除，② `!extrinsicVariable` → **return false（重复不叠）**，③ `extrinsicVariable` → 扫 `activeEffects` 找同 `enchantingCard` 的条目，`IsSameEffect` 为真则 return false、否则 `RemoveExtrinsicEffectsFrom` 再往下走。**`LAB_1805e50e7`（`.c:158`）起还有两道闸、对「非 extrinsic」这一路同样生效**：`.c:159-170` **残骸门** —— `*(char*)(param_1+0x65) != 0`（= `isRemnant`）时，**只有** `buffType == 1 addStats` 且 `healthChange(+0x60) != 0`、**或** `buffType == 0x5a addTraitRemnant` 放行，其余 `LogWarning`（"… which is remnant and the effect is not health buff"）+ `return 0`；`.c:171-195` **施放者不在场门** —— 施放者是 `Minion`（`cardType == 0`）且 `cardState ∉ {inPlay(2), inPlayTransforming(0xf), inPlayAttacking(3), inPlayAminingAttack(0x11)}`、又不是自己/自己也不在 {4,9,12,10,8,1} 时 → `LogWarning`（"… from card no longer in play"，字面量 `DAT_184234028`）+ `return 0`（i2c 对应 `:11610-11645`） |
| **枚举/常量真值** | `CardStateOptions`（`inDeck 0 · inHand 1 · inPlay 2 · inPlayAttacking 3 · waitingToBePlayed 4 · waitingToDie 5 · inCemetery 6 · drawing 7 · inHandShowing 8 · inHandMoving 9 · inHandPlaying 10 · removedFromGame 11 · inHandJustCreated 12 · inMulliganSelected 13 · inMulliganDiscarded 14 · inPlayTransforming 15 · inDeckDrawing 16 · inPlayAminingAttack 17`）⚠️ **`.c:67` 的 `-5U < 2` = 状态 5,6**，而 i2c 版写成 `cardState - 5 <= 1` —— 同一件事，别读成「4,5」。`jam = 130(0x82)` |
| **我们落点** | 没有对应前置闸。`EffectResolver.ResolveOps`(`:334`) 入口只清窗口；`Enchantment.Apply`(`:239` 附近) 才 `ActiveEffects.Add`。extrinsic 语义落在 `CardInstance.HandEffect.Extrinsic`(`CardInstance.cs:211`) + `EffectResolver.AttachHandEffect`(`:5288`) / `Aura.Recompose`(`Aura.cs:611`) |
| **现状** | 🟡 —— **五道闸**（卡状态 / jam / 重复不叠 / **残骸门** / 施放者不在场）**都没有等价物**；`isRemnant` 限定「只收生命 buff」这条尤其会让我们**静默**给残骸挂上不该有的增益 ⇒ 这是**行为差异候选** |
| **置信度** | 高（`.c:67-190` 逐行读过）；**缺**：`ContainsExtrinsicEffectFrom` 自己的方法体（`.c` 只调不展开）⇒ 要另读 `CardScript__ContainsExtrinsicEffectFrom.c` |
| **归属** | `战斗逻辑驱动.tsv:770` 备注列写 K0 |

### K1 —— 数值增减族：`addStats(1)` / `changeCost(2)` / `changeAbilityCost(10)` ＋ `doubleAttack(20)` 的前置改写
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:198-207` **先**：`if (buffType == 0x14)` → `buffType = 1;` + `if (CurrentMeleeAttack < 0x3fffffff) meleeAttackChange = CurrentMeleeAttack;` + `Log("Convering doubleAttack buff into +N")`。`.c:331-333` `case 1: case 2: case 10:` **三档共用一块**。`.c:335-460` 主体：① 扫 `activeEffects` 找**同源去重键**（`enchantingCard` 相同 ∧ `enchantmentCardId` 相同 ∧ `buffType` 相同 ∧ `markOfChaosType` 相同 ∧ `daemonHostType == none` ∧ `mutationType == none` ∧ `!doNotStack` ∧ `SameTimingAsEffect`）→ 命中则**四个增量字段各自累加**（`meleeAttackChange/rangeAttackChange/healthChange/costChange`）；② 未命中 → `List.Add(newEffect)`；③ `healthChange != 0` → `currentHealth` 加（正）/ **clamp 到 `currentMaxHealthProcessed`**（负）；④ `armourChange != 0` → `AddArmour(armourChange)`；⑤ `UpdateFigures()` |
| **枚举/常量真值** | `SameTimingAsEffect` 的判据 = `CardEffect.cs:142-167`：比 `untilEndOfTurn` ∧ `untilPlayerTurnStart` ∧ `untilEnemyTurnStart` ∧ `untilFollowingPlayerTurnStart` ∧ `untilNextAttack` **五个时长旗标**，且 `until*TurnStart` 那一档还要 `SameIsPlayerEffect`（比 `originalIsPlayer`）。`0x3fffffff` = int 上限一半？**⚠️ 这是个魔数，未读 `_DAT_`，按语义当「防溢出保护」记，待核** |
| **我们落点** | `EffectResolver.DoGive`(`:10984`，注册在 `EffectDispatch` `:1069` 的 `give/gain/lose`) · `ApplyOneGain`（`DoGive` 内）· `DoLowerCost`(`:14119`) / `DoCostMore`(`:6666`) · `ctx.CostMods`(`BattleContext.cs:1200`) · `CardInstance.CostDelta`(`:393`) · `RuleCore.CostOf`（唯一读点） |
| **现状** | 🟡 —— **四字段累加口径我们是这样吗？未逐句核**。已知两处**必须核**：① 原版**按五个时长旗标分桶**再累加，我们的 `HandEffect.Expire`(`CardInstance.cs:194`) 是单值枚举；② `healthChange` 负值 **clamp 到 `currentMaxHealthProcessed`**（不是 0、不是 MaxHealth）—— 我们 `ApplyOneGain` 是否同口径**待核** |
| **置信度** | 高（`.c:331-459`）；**缺**：`0x3fffffff` 的来源、`SameTimingAsEffect` 我们侧等价判定 |
| **归属** | K1；账 `:770` 的 ✅/🟡 里「1 addStats ✅」**需要复核**（它标 ✅ 但没写四字段累加与 clamp） |

### K2 —— `addTrait(3)` 主体：挂载去重 + 三种叠层规则
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:460-461` `case 3:` / `switchD_..._caseD_3:`（**`90 addTraitRemnant` 也跳到这里**，见 K10）。`.c:461-475` 先 `Log("Add Trait")` + `CardTraitCollection.GetTrait(trait)`。`.c:475` `if (trait != 0xc8 poisoned)` → 若身上有 `resistant(0x258)` ⇒ **取消这次中毒** + `HighlightTraitIcon` + `DisplayStatusAnim` + break。`.c:~480` `if (trait != 0x1ae survivor)` → `RemoveActiveEffects(4, trait)`（**授某关键词前先清掉同名的 removeTrait 记录**）→ `blind` 特例（见 K3）→ **挂载去重循环**：从 `activeEffects` **末尾向前**扫，命中条件 = `buffType == 3`（或 `== 90`）∧ `trait` 相同 ∧ `markOfChaosType` 相同 ∧ `daemonHostType` 相同 ∧ `SameTimingAsEffect` ∧ 两侧 `extrinsic` 都为假 ⇒ 按 `CardTrait._stackable` **三选一**：`replace` → `Math.Max(旧, 新)`；`add` → 累加；`count` → `+1`；`_stackable` 其它值 → 只认旧值。`.c:~655` 未命中 → `if (_stackable == count) newEffect.traitValue = 1;` + `List.Add` + **`AddTraitSilently(trait, false, 0)`**。`.c:678` `break` |
| **枚举/常量真值** | `TraitStackType`（`replace = 0 / add = 10 / count = 20`，`dump.cs:21302-21308` 带显式值现读）。🔴 **2026-10-25 就地订正（`Q1` 第 2 轮，铁律 5）**：本格原来写「该表把值都写成 `10`，那个 10 是『未解』的占位编码」—— **不成立**。**那个 `10` 就是枚举值 `add`**：73 个 `CardTrait` SO **逐对象**用原始字节解析、`bytesRead == byte_size` **73/73 全通过**（`工具/export_trait_collection.py --check`，md5 `cbb9809d…`），真值 = **74 条 `add(10)` · 1 条 `count(20)`（`MarkOfChaos`） · `berzerk`/`dodge` 两个 pair 无 SO**。逐条表 → **`判据/CardTrait_stackable_真值表_第2轮.md`**（⛔ 别再回 `ttread.py`，那个工具全仓 0 调用者） |
| **我们落点** | `UnitState.AddKeyword`(`UnitState.cs:954`) · `UnitState.RemoveBuffedTrait`/`RemoveTrait` · `UnitState.Has`(`:806`) / `Keywords`(`:816`) · 挂载去重**未找到**（grep `_stackable` / `stackable` 无实现） |
| **现状** | 🟡 → 部分 **⛔** —— 关键词是 `Dictionary<string,int>`，**没有「同源/同计时」去重键**、**没有三种叠层语义**。判据已定（`.c` 那一段），**可以直接认领去做** |
| **置信度** | 高；~~**缺**：`_stackable` 的逐 trait 真值（`CardTrait词条权威表.md` 的 10 是占位）~~ 🔴 **2026-10-25 订正（`Q1` 第 2 轮）：这一格不再缺** —— 真值已实读定案，见 `判据/CardTrait_stackable_真值表_第2轮.md`（74×`add` / 1×`count` / 2 无 SO） |
| **归属** | K2；叠层规则**单独**可给一个代理（它是纯数据 + 纯逻辑、文件互不相交） |

### K3 —— `addTrait(3)` 的 trait 特例族（11 条，逐条独立可核）
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:475` `poisoned(0xc8)`：身上有 `resistant(0x258)` ⇒ 取消 + 高亮 + 状态动画（**这是 11 条里唯一「不挂」的**）。`.c:~480` `survivor(0x1ae)`：`healthChange == 0` ⇒ `LogWarning("Ignoring survivor trait granted with 0 health change")` 不挂；非 0 才继续。`.c:~487-500` `blind(0x3cf)`：**强制改写时长** `untilEnemyTurnStart=false / untilFollowingPlayerTurnStart=true / untilEndOfTurn=false / untilNextAttack=false`，且若 `currentAttackType != Melee` ⇒ 改成 Melee + `CardUI.AttackTypeChanged` + `Play2D(changeToMeleeAttackCUE)` + `blindedAtStartOfTurn = false`。`.c:570` `fast(0x28) \|\| flank(0x1cc)` → `UpdateSummonSicknessAnims()`。`.c:573` `dropPod(0xe6)` → `currentDropPodHealth = GameStaticData.dropPodMaxHealth`、`currentDropPodAttack = GameStaticData.dropPodAttack`。`.c:606` `survivor(0x1ae)` → `RemoveTraitSilently(survivorSpent 0x1b8=440)`。`.c:615` `tide(0x38e)` ∧ `traitValue != 0` → `tideCounter = CurrentTide`。`.c:619` `companion(0x49c)` ∧ `traitValue != 0` → `companionCounter = CurrentCompanion`。`.c:624` `pindown(0x3ca)` → 强制 `currentAttackType = Ranged` + `AttackTypeChanged` + `Play2D(changeToRangeAttackCUE)`，**且 `num20 = 0`（跳过 UpdateFigures）**。`.c:635` `markerlight(0x492)` → `BroadcastMarkerlightResolved(this)`。`.c:639` `huntMark(0x4ec)` → `CardUI.DisplayTriggerAnim(huntMark, true)` + `BroadcastHuntMarkResolved(this)`。`.c:646` 最后按 `cardState ∈ {2, 3, 15, 17}` 且在 `CardTrait._hasInPlayAnim` 为真时 `DisplayStatusAnim`，再 `CleanStatusAnims(false)` + `UpdateTraitIcons()` |
| **枚举/常量真值** | 见 §〇·一。`survivorSpent = 440`、`CurrentTide/CurrentCompanion` 是 `EntityScript` 的派生属性（当前关键词值），**要从 `EntityScript__get_CurrentTide.c` 等读，别猜** |
| **我们落点** | `EffectResolver.DoBlind`（blind 那条**已逐句核过**，注释自引 `AddEffect.c:475-487`，见 `EffectResolver.cs:4130-4145`）· markerlight/huntMark 广播 = `EffectResolver.cs:11306-11360`（注释自引 `:635/:637` 与 `:639/:643`）· dropPod = `RuleCore.OpenDropPod` + `CardDef.cs:6360-6390` · survivor = `UnitState.Survivor`(`:392`) / `UseSurvivor`(`:437`) · tide/companion = `RuleCore.SpawnTideCopies`(`:4112`) / `PlayCompanions` · pindown = `KeywordTable.Pindown` |
| **现状** | 🟡 —— **11 条里有 3 条明确已核（blind / markerlight / huntMark）**，其余「有同族实现但没逐条对条件」。⚠️ **`poisoned` 与 `survivor` 两条池内无入口**（`cards_raw.json` 里 `trait ∈ {200, 430}` 的 buff **0 次**）⇒ 仍要做（原版有行为），但**先证明**它们的入口在哪（可能是 `CardScript.AddPoison` 这类专门方法，未核） |
| **置信度** | 高（条件）；中（`CurrentTide`/`CurrentCompanion` 取值、`dropPodMaxHealth/Attack` 静态值 —— 都要另读） |
| **归属** | K3；**建议拆 3 个代理**：① poisoned/survivor/blind ② fast/flank/dropPod/pindown ③ tide/companion/markerlight/huntMark（三条互不碰文件） |

### K4 —— `removeTrait(4)`
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:680` `case 4:` → `.c:681` `if (trait == 0x46 stealth && HasCurrentTrait(stealth))` → `BroadcastUnitLoseStealth(this)`。`.c:691` `if (trait == 0x3cf blind)` → `blindedAtStartOfTurn = false`（**只有 blind 额外清这一格**）。中间：`RemoveActiveEffects(4, trait)` → `List.Add(newEffect)` → **`RemoveBuffedTrait(trait)`** → **`RemoveTrait(trait)`** → `UpdateFigures()` |
| **枚举/常量真值** | `stealth = 70(0x46)` · `blind = 975(0x3cf)` |
| **我们落点** | `UnitState.MarkTraitRemoved`(`:733`) / `RemovedTraits`(`:739`) / `HasRemovedTrait`(`:726`) · `ConditionEval.cs:455`（注释自引 `AddEffect.c:680-694` 的 `RemoveActiveEffects(4)`） · `EffectResolver.cs:11313-11320`（注释自引同一段） |
| **现状** | 🟡 —— **记住加了一条**（`removedTraits` 黑名单），**「先摘记录再摘关键词」的顺序与 `BroadcastUnitLoseStealth` 是否同形未核** |
| **置信度** | 高（`.c:680-694` 共 15 行，读完即可）；池内 **14 次**（stealth 9 / camouflage 4 / invulnerable 1 —— ⚠️ 2026-10-25 更正：原写 28/18/8/2，那是**含 `full_raw` 重复**的 2 倍口径） |
| **归属** | K4（小、可与 K2 合并给一个代理） |

### K5 —— 身份改写族：`swapStats(5)` / `changeRace(6)` / `addRace(30)`
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:695` `case 5:` → `if (IsWarlordEquivalent())` ⇒ **只 LogError 不做事**（`"Trying to swapStats of warlord. Player … enemy …"`）；否则：取 `CurrentIntrinsicHealth`（= 当前内在最大生命）与 `CurrentIntrinsicMeleeAttack`，**再扫一遍 `activeEffects`，对 `buffType == 1 && extrinsic` 的条目把 `healthChange(+0x60)` 累加到近战那一侧**，然后 `ChangeBaseAttack(内在生命)` + `ChangeMaxHealth(内在近战)` + `currentHealth = 内在近战`。⚠️ **只换近战攻击与生命，不换远程、不换护甲、不换当前最大生命以外的量**。`.c:750-752` `case 6:` → `uVar9 = 6; goto …` ⇒ `RemoveActiveEffects(6)` + `RemoveAllExtrinsicEffects()` + `List.Add`。`.c:951-964` `case 0x1e(30):` → 同形，参数 `30`。**两者都不写 `newRace` 字段** —— `newRace` 是靠「`activeEffects` 里有一条 `buffType=6/30` 的记录」被别处读出来的 |
| **枚举/常量真值** | `swapStats = 5` · `changeRace = 6` · `addRace = 30`。`IsWarlordEquivalent` 判据 = `CardScript__IsWarlordEquivalent.c` |
| **我们落点** | **未找到**。grep `swapStats` / `changeRace` / `addRace` / `newRace` / `Race`（`CardDef` 只有 `ExcludeRace` 一个目标筛选字段 `:2352`）⇒ **0 实现** |
| **现状** | **⛔ 缺**（三条都缺）。**池内可达性 0**（`cards_raw.json` 0 次）⇒ 按铁律 11 仍要做（原版有行为），按铁律 11 例外① 需要**再证一次入口**：`newRace` 字段是否由别的数据路径写（`RawCardScript` / `CardAbility` 的 `raceChange`？）—— **未核，别急着记 D** |
| **置信度** | 高（`.c` 三段共 ~45 行已读完）；中（「池内 0」只查了 `buffType` 一路） |
| **归属** | K5。⚠️ **它是「原版有、我们整条没有」** ⇒ 即使池内 0 也要入账（铁律 11），先记 C |

### K6 —— 原版自称**未实现**的那一档（`caseD_7` 集合）
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:753-766` `case 7: case 0xd: case 0xe: case 0x12..0x1d:` → `.c:767` `switchD_..._caseD_7:` `type = typeof(BuffType); LogWarning("No implementation for buffType " + …)` → `UpdateFigures()`。`.c:965-971` `default: if (iVar16 != 0x23) goto caseD_7;`（`0x23 daemonHost` 走 `LogError("[ERROR] Bufftype daemonHost not implemented")`）。**`0x2d(45)` 与 `0x28(40)` 之外的 >45 值也 goto 这里** |
| **枚举/常量真值** | 被点名 no-impl 的 buffType：`7 skipCardDraw` · `13` · `14` · `18` · `19` · `20 doubleAttack`（可达但已被 `.c:198` 改写成 1）· `21` · `22` · `23` · `24` · `26 tideCopy` · `27` · `28` · `29` · `35 daemonHost`（LogError）· `45` 与 `40` 之外的 >45 值 |
| **我们落点** | 不需要落点 —— **原版自己就没有实现** |
| **现状** | ⚪ **原版自身无行为** ⇒ 可记 **D**，但**必须引用** `.c:767` 或 `.c:969` 那一行（§4.1 D 规则）。⚠️ **只有「原版也 LogWarning/LogError」的才可判 D**；`7 skipCardDraw`（我们**已建** `RuleCore.SkipCardDrawVerb` `:1258`）与 `26 tideCopy`（我们**已建** `SpawnTideCopies`）**不能**因为原版 AddEffect 不管就判 D —— 它们的真实实现可能在**别的方法**里（`PlayerHand` / `MinionManager`），**要另查** |
| **置信度** | 高（这一段 15 行，读完即可） |
| **归属** | K6（最小的一块，可与 K0 合并） |

### K7 —— `markOfChaos(8)` / `lastMarkOfChaos(17)`
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:776` `case 8:` `Log("Appliying mark of chaos to " + cardName)` → `raw = BattleMgr.enchantments.GetMarkOfChaos(newEffect.markOfChaosType)` → **`raw.TriggerEnchantment(BattleMgr, this, this, 0)`** → `BattleMgr.lastMarkOfChaos = raw` → **`BattleMgr.BroadcastMarkResolved(this)`** → `UpdateFigures()`。`.c:933` `case 0x11(17):` `Log("Appliying mark of chaos to …")` → **不取新的**，直接**用 `BattleMgr.lastMarkOfChaos.TriggerEnchantment(...)`**（`+0x2f0` 那条） |
| **枚举/常量真值** | `markOfChaos = 8` · `lastMarkOfChaos = 17`。`MarkOfChaosType` 枚举（`MarkOfChaosType.cs`）+ `Enchantments` SO 的映射表 |
| **我们落点** | `RuleCodes.cs:74`（注释）· `CardDef.cs:6971`（注释，自引 `AddEffect.c:776-792` case 8 → `Enchantments.GetMarkOfChaos`）· `EffectResolver.cs:10545` / `:11553` / `:11683`（注释）· **真正实现**：`Enchantment.cs` 的附魔挂载（`Apply` `:239`）+ `Enchantments.GetMarkOfChaos` 若存在要 grep 确认 |
| **现状** | 🟡 —— 注释齐、广播点可能在 `EffectResolver.cs:11306` 附近，但**「取 lastMarkOfChaos 那条链」未建**（grep `LastMarkOfChaos` **0 命中**）。**池内 29 + 1 次 ⇒ 可观测**（`EC41 Internal Rivalry` / `BL30 Chaos Lord` / `BL72 Accursed Helbrute`） |
| **置信度** | 高（两条 case 共 22 行）；**缺**：`GetMarkOfChaos` 的 SO 映射表（`MarkOfChaosType` → `RawCardScript`）在哪、我们有没有 |
| **归属** | **K7（第一优先，见 §三）** |

### K8 —— 「设为固定值」族：`setHealth(9)` / `setCurrentHealth(100)` / `setMeleeAttack(70)` / `setRangeAttack(85)` / `setCost(95)`
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:794` `case 9:` → `RemoveActiveEffects(9)` → `ShouldTriggerEcastasy(this, healthChange)` ⇒ `RawCardData.OnTrigger(0x2cb /*Ecstasy*/, …)` → `List.Add` → **`currentHealth = currentMaxHealthProcessed`**（**回满血**，⚠️ **不用 `healthChange`**）→ `UpdateHealthText(true)`。`.c:229-247` `if (iVar16 == 100)` → 同样先 Ecstasy 检查 → 然后 **`currentHealth = healthChange(+0x60)`**（**这是真正「设为 N」的那一档**）。`.c:208-228` `if (iVar16 == 0x46 \|\| iVar16 == 0x55)`（70/85）→ `RemoveActiveEffects(buffType)` + `List.Add` + **`UpdateAttackText()`**（⚠️ **不直接写 `currentMeleeAttack`**，靠 `EntityScript.CurrentMeleeAttack` 把 `activeEffects` 里的 setMeleeAttack 记录算进去）。`.c:229-242` `if (iVar16 == 0x5f)`（95 setCost）→ `RemoveActiveEffects(95)` + `List.Add` + `UpdateCostText()`（同形，靠 `CurrentCost` 现算） |
| **枚举/常量真值** | `setHealth = 9` · `setCurrentHealth = 100` · `setMeleeAttack = 70` · `setRangeAttack = 85` · `setCost = 95`。`0x2cb = 715 = AbilityTrigger.Ecstasy` |
| **我们落点** | `EffectResolver.DoSetHealth`(`:13729`) —— ⚠️ **它做的是「降到 N 且比当前小才改」+ Invulnerable 拦截**，与原版两档**都不一样**：原版 `9` 是回满、`100` 是无条件设为 N、**且两档都不查 Invulnerable**。`DoDouble`(`:13789`) · `CardDef.MeleeEqualsHealth`(`:4253`，`Scarab Swarm` 的 70 特判) · `DoLowerCost`(`:14119`) + `ctx.CostMods`(`BattleContext.cs:1200`) |
| **现状** | **🟡 + 明确差异** —— ① `setHealth(9)` **池内 0**（无卡），但原版行为＝回满血；② `setCurrentHealth(100)` **池内 1**（`EC39 Eternal Servitude`），我们的 `DoSetHealth` 语义（只降、查 Invulnerable）**与原版不同** ⇒ 这是一条**可直接落盘的真差异**；③ `setMeleeAttack(70)` **池内 1**，只有 `MeleeEqualsHealth` 一个特判，**通用支未建**；④ `setRangeAttack(85)` **池内 0**、未建；⑤ `setCost(95)` **池内 3**（`GSC39` / `TAU68` / `TL69`，**`costChange` 全是 1**）、未建（`DoLowerCost` 是减量，不是设值）。🔴 **语义判据（2026-10-25 Q2 订正）**：`EntityScript__get_CurrentCost.c:66-73` —— **有 `setCost` 记录时，现值 = `setCost` 存的那个值（⛔ 不含 `manaCost` 基数）＋ 全部 `changeCost` 增量** ⇒ **别实现成「`N − manaCost`」**（那是错口径） |
| **置信度** | 高（五档共 ~70 行已读）；**缺**：`CurrentMeleeAttack` / `CurrentCost` 的现算公式（`EntityScript__get_CurrentMeleeAttack.c` / `CurrentCost.c`）—— 我们照抄时必须一起搬 |
| **归属** | **K8 可拆 2**：① setCurrentHealth + setHealth（生命两档）② setMeleeAttack/setRangeAttack/setCost（进攻与费用三档） |

### K9 —— 附魔三档：`enchantment(12)` / `singleUseEnchantment(15)` / `choiceEnchantment(16)`
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:826-829` `case 0xc: case 0xf:` → **`AddEnchantment(param_2)`** 一句话（细节在同名方法 `CardScript__AddEnchantment.c`）。`.c:830-932` `case 0x10(16):` 最长的一档 —— ① `activeEnchantments` 为 null 就建 `List`；② 造 `ActiveEnchantment{ enchantmentCard = newEffect.enchantment; enchantmentCardID = BattleMgr.enchatmentsUsed }` + `BattleMgr.UsedEnchantment()`；③ 两条 list 各 `Add`；④ `ActivateTraitsOnSummonOrEnchantment(false, true)`；⑤ 按 `newEffect.enchantment.spellType(+0x90)` 分支：**`0x3c(60) SecretOrder`** → `secretBuff = true`；**`0x78(120) Quest`** → 扫 `activeEffects` 把**其它 Quest 附魔全 `RemoveEffect`** + `questBuff = true` + `questCounter = 0` + `DisplayStatusAnim(quest)` + `UpdateTraitIcons`；**`0x82(130) Mutation`** → `SpendEnchantment` + `TriggerEnchantment` + `OnTrigger(Mutated)`；**`0x8c(140) Wing`** → `SpendEnchantment` + `lastMarkOfChaos = enchantment` + `TriggerEnchantment` + `BroadcastMarkResolved`；**`0xbe(190) MarkOfChaos`** → 同 Wing 那一套；⑥ 最后 `BattleMgr.cardChosenInSelection = 0` + `UpdateFigures` |
| **枚举/常量真值** | `enchantment = 12` · `singleUseEnchantment = 15` · `choiceEnchantment = 16`。`SpellType`：**`0x3c=60 SecretOrder` · `0x78=120 Quest` · `0x82=130 Mutation` · `0x8c=140 Wing` · `0xbe=190 MarkOfChaos`**（⚠️ 这几个数字是从 `.c:853/856/878/895/916` 读出的**裸常量**，名字对 `SpellType.cs` —— **必须去核一次名字，别照我这张表当权威**） |
| **我们落点** | `Enchantment.cs` 全文件（`ActiveCardEffect` `:67` · `ActiveEnchantment` `:106` · `Apply` `:239` · `Spend` · `Permanent` `:83`）· `CardInstance.ActiveEnchantments`(`:142`) · `EffectResolver.cs:11553` / `:11683`（注释自引 `.c:776-792`） |
| **现状** | 🟡 —— `12/15` **有实现**（`Enchantment.Apply`），`16 choiceEnchantment` **账里标「未建」但池内 13 次**（`TL49 Hyper-adaptation` / `ASH57 Webway Entrance` / `BL38 Dark Oratory` / `BL13 Chosen`）⇒ **必须核我们到底有没有**（`ActiveEffects.Add` `Enchantment.cs:239` 可能已覆盖 16，账里那条可能过期） |
| **置信度** | 高（`.c:826-932` 全读）；**缺**：`SpellType` 数字→名字的核对、`AddEnchantment` 自己（另一个方法） |
| **归属** | **K9（第二优先）**；可拆 2：① 12/15（`AddEnchantment`）② 16 的五个 `spellType` 子支 |

### K10 —— 残骸 / 复制 / 抽牌抑制族：`addTraitRemnant(90)` / `ephemeralCopy(25)` / `tideCopy(26)` / `addStatsDropPod(45)`
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:219` `if (iVar16 == 0x5a)`（90）→ **`goto caseD_3`（= 直接走 addTrait 那一整块，见 K2/K3）**，只是 `RemoveActiveEffects` 的键值仍是 90。`.c:943` `case 0x19(25):` `Log("Setting as reflectedCopy")` → `AddTraitSilently(5 /*ephemeral*/)` + `CardUI.ShowEphemeral()`。`.c:753-766` `26 tideCopy` 在 **no-impl 组**（原版 AddEffect 不管它，真实现应在 `MinionManager`/`PlayerHand`）。`.c:243-275` `if (iVar16 == 0x2d /*45*/)` → `List.Add` + `healthChange != 0` ⇒ 累加 `currentDropPodHealth(+0xcc..0xdc)` + `meleeAttackChange != 0` ⇒ 累加 `currentDropPodAttack(+0xe0..0xf0)`。⚠️ **注意 dropPod 的累加用的是 `ObscuredInt` 五字写回（`+0xcc` 起 5 个 int）—— 布局照抄，别只写一个 int** |
| **枚举/常量真值** | `addTraitRemnant = 90(0x5a)` · `ephemeralCopy = 25(0x19)` · `tideCopy = 26(0x1a)` · `addStatsDropPod = 45(0x2d)` · `ephemeral = DefinedTrait 5` |
| **我们落点** | `CardInstance.EphemeralMarked`(`:78`) · `BattleContext.MarkEphemeral`(`:1532`) / `IsEphemeral`(`:1526`) · `RuleCore.SpawnTideCopies`(`:4112`) · `RuleCore.OpenDropPod` + `CardDef.cs:6360-6390` · `UnitState.IsRemnant`(`:274`) |
| **现状** | 🟡 —— `25/26/45` 都有落点（账里标 ✅），但 `90 addTraitRemnant` **池内 2 次**（`SAU36 Nemesor Zahndrekh` / `Undying Legions`，trait = `notDestroyRemnant(1190)` 与 `shield(80)`）**是否真的走 addTrait 那一套未核** |
| **置信度** | 高（四段共 ~40 行）；**缺**：`GameStaticData.dropPodMaxHealth/Attack` 真值 |
| **归属** | K10 |

### K11 —— extrinsic / 替换能力族：`removeExtrinsic(11)` / `replaceAbility(40)`
| 项 | 内容 |
|---|---|
| **分支条件（原文）** | `.c:823` `case 0xb(11):` → **`RemoveExtrinsicEffectsFrom(enchantingCard)`**（一句话）。`.c:243-251` `if (iVar16 == 0x28 /*40*/)` → `if (enchantment == null)` ⇒ `Log("Trying to replace Ability … but enchantment is empty")`；否则 **`hasActiveAbility = hasChangedActiveAbility = true`（`*(undefined2*)(param_1+0x220) = 0x101`）** + `activeAbilityRef(+0x118) = newEffect.enchantment` + `DestroyActivateAbilityAnim()` + `CanUseAbilityNow()` + `OnMessage(...)` + `List.Add` + `UpdateTraitIcons()` |
| **枚举/常量真值** | `removeExtrinsic = 11` · `replaceAbility = 40(0x28)` |
| **入口（🔴 2026-10-25 Q1 已证 —— 本条**推翻**我第一版「池内 0」的结论）** | **`11 removeExtrinsic` 不经 SO 数据，是【代码构造】**：`AbilityLogic__PlayAbility.c:1246` 直接写 `buffType = 0xb`；门 = **`trigger == 240`（`WhileInPlayVariable`）∧ `chosenAbility == 30`（`buffOthers`）**。⇒ **池内 32 条**（`UM107` / `TAU19` / `SAU36` / `UM14` / `DAU27` …）。⇒ **每局都可能跑到**，不是死代码。`40 replaceAbility` 仍池内 0（SO 与代码构造点都没产出）⇒ 那一半**仍需再证** |
| **我们落点** | `Aura.Recompose`(`Aura.cs:611`)（整体重算 = 等价于 `RemoveExtrinsicEffectsFrom` 的净效果，**账里标 ✅**）· `EffectResolver.CheckReplacedAbility` / `RuleCore.AbilityFamilyTargetAllows`（账里标 🟡）· `CardDef.Ability`(`UnitState.cs:819`) / `UnitState.GrantOps`(`:852`) |
| **现状** | 🟡 —— `11` 靠「整体重算」等价，**`extrinsicVariable` 那一支是否被覆盖未核**（这是**本轮第 2 优先**，见 §三）；`40` **池内 0**，但 `hasAlternativeAbility` / `activeAbility` 字段在卡上有 ⇒ **替换能力可能经别的路径进来**（`CardScript.AddEffect` 之外），**未核、别判 D** |
| **置信度** | 高（`.c` 两段共 ~15 行 + `PlayAbility.c:1246` 的构造点）；**缺**：`removeExtrinsic` 的 32 条**分别挂在哪几张卡上**（Q1 只给了 5 个示例 `uniqueId`）、`replaceAbility` 的真实调用点 |
| **归属** | K11（拆两半：11 已定案可做 · 40 先证入口） |

---

## 三、最该先做的 K 块（**只排先后，⛔ 不按成本筛**）

| 序 | 块 | 为什么排这儿 |
|---|---|---|
| **1** | **K7 `markOfChaos(8)` + `lastMarkOfChaos(17)`** | ① **池内 30 次**（29 + 1）、可观测（`EC41 Internal Rivalry` / `BL30 Chaos Lord` / `BL72 Accursed Helbrute`）；② 账里那句「池内 0 张」是**错的**（§〇·四）—— 不清掉它会一直骗下一个会话；③ 判据只有 22 行、`GetMarkOfChaos` 映射表是**唯一未定的一环** |
| **2** | 🆕 **K11 `removeExtrinsic(11)`**（**本轮上调**，Q1 已证入口） | ① **池内 32 条**，且是**代码构造**（`AbilityLogic__PlayAbility.c:1246` 写 `buffType=0xb`；门 = `trigger=240(WhileInPlayVariable)` ∧ `chosenAbility=30(buffOthers)`）⇒ **每局都可能跑到**；② 我们靠 `Aura.Recompose`（`Aura.cs:611`）**整体重算**，**`extrinsicVariable` 那一支是否被覆盖没人核过**；③ 判据只有 `.c:823-825` 三行 ⇒ **便宜 + 风险高，先做**。⚠️ 同块的 `replaceAbility(40)` 仍池内 0，**那一半**仍需先证入口 |
| **3** | **K9 `choiceEnchantment(16)` 的五个 spellType 子支** | ① 池内 **13 次**；② 账里标「未建」但 `Enchantment.Apply` 可能已覆盖 ⇒ **30 分钟内能收口**；③ 五个子支（SecretOrder/Quest/Mutation/Wing/MarkOfChaos）**各自是独立的规则**，漏一个就是一张卡静默错 |
| **4** | **K8 `setCurrentHealth(100)` + `setMeleeAttack(70)` + `setCost(95)`** | ① `DoSetHealth` 的语义（只降 + 查 Invulnerable）与**原版两档都不同** ⇒ **已是可落盘真差异**；② 池内 1 + 1 + 3 = **5 次**；③ 三档都是「设为固定值」，**共用一套 `Current*` 现算口径** ⇒ 一起做最省 |
| **5** | **K3 addTrait 特例族（11 条）** | ① **入口最多**：`flank 62 · vanguard 33 · fast 32 · shield 30 · blast 24 · invulnerable 21 · stealth 21 · vulnerable 19 · markerlight 18 · huntMark 14 · blind 12 · bloodThirst 10 · shuriken 10`（判据 = `cards_truth.json` 的 `buffType==3` 按 `trait` 分组）；② 11 条**各自独立**，天然可拆 3 个代理；③ 判据已读到行级，**缺的只是「逐条对我们落点」** |
| **6** | **K2 addTrait 主体（叠层三规则 + 去重键）** | ① **368 次**入口的**公共路径**；② 我们**结构上就没有**（`Dictionary<string,int>` + 无去重键）⇒ 改动面大，**必须早开工**；③ 与 K3 **文件重叠**（都在 `UnitState`）⇒ 排序上要和 K3 串行或由同一代理做 |
| **7** | **K0 前置五道闸** | ① 每一张卡的 effect 都过；② **五道闸我们一道都没有** ⇒ 会**静默**叠出重复 buff、并给**残骸**挂上不该有的增益；③ 工作量小（读 6 段 + 加 5 个判断） |
| **8** | **K4 `removeTrait(4)`** | 池内 **14 次**；判据 15 行；可与 K2 一起 |
| **9** | **K1 `addStats(1)/changeCost(2)/changeAbilityCost(10)`** | 入口最大（**646 次** = 507 + 139 + 0）但**最成熟**（`ApplyOneGain` 已跑了很久）⇒ 这一块是**「复核」而不是「新建」**：核四字段累加 + 负值 clamp + 五个时长旗标分桶 |
| **10** | **K10 残骸族 `addTraitRemnant(90)`** | 池内 **2 次**；`25/26/45` 已有落点，真正要补的只有 90 的「走 addTrait 那一套」 |
| **11** | **K5 `swapStats(5)/changeRace(6)/addRace(30)`** | 池内 0、我们 0 实现 ⇒ **按铁律 11 仍要做**，但要先证「真的没有数据入口」（两条路都要走，见 §〇·五），否则会做出**永远不触发的死代码**（`RuleCodes.cs` 里已有两条这样的记录） |
| **12** | **K6 no-impl 档** | 原版自己 `LogWarning` ⇒ 直接记 D（引用 `.c:767` / `.c:969`）；**唯一例外**是 `7 skipCardDraw` / `26 tideCopy` 我们要另查真实实现（我们已建，别被这块误导成 D） |

---

## 四、认领时必须带的四条硬约束

1. **行号一律用 `.c`**（`D:/2/tools/decomp_full/CardScript__AddEffect.c`）。`i2c` 的 case 号 = buffType − 1，**抄过去会全错一格**。
2. **枚举值不猜**：`DefinedTrait` 用 §〇·一那张表；`BuffType` 用 `BuffType.cs`；`SpellType` 的数字→名字**必须回 `SpellType.cs` 核一遍**（本文件 §K9 那五个数字是我从 `.c` 读出来再对名的，**未二次核**）。
3. **池内可达性用 §〇·三 的数字**，但**不许拿它当「做不做」的判据**（铁律 11：只决定先后）。任何「池内 0 ⇒ 不做」的写法都要被退稿。
4. **原版自身 LogWarning 的才可判 D**。`7 / 26 / 40` **不许**因为 AddEffect 不管就判 D。⚠️ **本文件第一版把 `11 removeExtrinsic` 也放进了「池内 0」名单，那是错的**（Q1 已证 32 条、代码构造，见 K11）—— 判「池内 0」必须**两条路都走完**（§〇·五）。
