# B141_W128_RowAbility（`ROWABILITY-EARLYRET` + `SAN27-UNION`）

> 执行写手 `B141-W37` · 2026-11-04 · 批次 `B141` · 会话第一百二十八。
> 白名单：`RuleEngine/Core/RuleCore.cs` · 新建 `RuleEngine/Editor/B141_W37_Assert.cs` · 本报告。
> ⛔ 未改 `BattleDriver.cs` / `BattleEvent.cs` / `EffectResolver.cs` / `CardAnimOrder.cs` / 生成器 / 账主表 / 活状态表 / 所有权表；⛔ 未跑 Unity；⛔ 未动 git / `d:/2`。
> 行号一律本文写作时**现读**。

---

## 〇 · 一句话结论

| 任务 | 判定 | 改在哪 |
|---|---|---|
| **1. `ROWABILITY-EARLYRET`**（效果层按行重挂对主动能力恒早退） | ✅ **已修** | `RuleCore.cs:18577`：`if (step[2] < 0 ‖ step[3] < 0) return;` → `if (step[2] < 0) return;`（**同一物理行改写 = 零净行数**，文件仍 18,748 行） |
| **2. `SAN27-UNION`**（事件按行承载） | ❓ **本包做不到，已定位为跨 4 处文件、另立** | 证据见 §五：`BattleEventTarget` 只有四格、`CardAnimBeat.ca` 是 **GUID 字符串**（不是 chosenAbility 号）、消费端 `BattleDriver.ExpandAbilityBeats` 逐卡摊平；**数据侧生成器也把 `logicIndex` 摊掉了**（但中间结构里已有，可低成本补回） |

**修复的净可观测影响 = 0**（`play check` 指纹不变、`play all dump` 1143 行**逐字节相同**），原因已逐卡归因（§四）：
**今天没有任何一张卡的「可发布行（白名单 ta）」与「随机型 op」同时成立** ⇒ 唯一消费点 `EffectResolver.cs:1925` 进不去。
本修的意义 = **把那条被早退吃掉的通路接上**（`B140_WA` 的 C3/C4 断言此前会红），并给「按行目标」这条链留下可验的锚点。

**验证**：`typecheck` 运行时 **0** / 编辑器 **0** · `ruleprobe play check` **rc=0**，结算指纹 **a0e9c63d7bfa33735f5dad8230a26edd**（**与基线一致、未变**）· `play all dump` 改前/改后**逐字节相同**（各 1,143 行）· `ruleprobe check` **rc=0**（全池 1134 张 · 解析差异 0 行 · 哨兵失败 0 条）。
**未执行断言**（无 Unity）：断言文件见 §三，**尚未接进 `RuleEngineTest.cs`**（白名单外，调度台代接）。

---

## 一 · 修复本体（`ROWABILITY-EARLYRET`）

### 改前 / 改后（`RuleCore.cs:18574-18587` 现读）

```csharp
// 改前（:18577）
if (step[2] < 0 || step[3] < 0) return;   // `ca == 462` 那类行没有 (ability, logic) 二元组
// 改后（:18577）
if (step[2] < 0) return;                  // ✅ 已复刻（2026-11-04 · B141-W37 …）
```

### 为什么改后仍然安全

| 事实 | 判据 |
|---|---|
| `step[3] = row.Ability` = **能力对象**，主动能力恒 `-1`、替代行动恒 `-2` | `CardDef.cs:3078/3080`（`ActiveAbilityObject = -1` / `AlternativeAbilityObject = -2`）；数据 `cards_engine.json` v49 现读 `activeAbilityLogic[].ab` = **`-1`×132 / `-2`×1** |
| `ActiveAbilityTargetCriteriaAt(abilityObject, logic)` **本来就接受** `-1/-2`，只有 `logic < 0` 才返回 `null`（其余 abilityObject 也一样返 `null`） | `CardDef.cs:2621-2627`（`if (!Versioned ‖ logic < 0) return null;` 之后才判 `abilityObject != -1 && != -2`） |
| `ca == 462`（`triggerPray`）那类行没有 logic 二元组 ⇒ 由 `step[2] = -1` 挡住 | `RuleCore.cs:15588`（`ca == 462 ⇒ plan.Add(new[]{ ca, ta, -1, row.Ability })`）；`CardDef.cs:2623` 双保险 |
| 原版语义 = **每一行**各一次 `GetTargets + PlayAnim`，效果层吃**那一行**的表 | `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:262/273`（第二处 `:1524-1531`） |
| 唯一产品调用点 = `UseActiveAbilityCore` 的行循环 | `RuleCore.cs:15632`（`grep PublishRowAbilityTargetTable` 全仓只有它 + 自检门 `:18591`） |

### 同一物理行的第二处订正（铁律 5，就地改掉过期描述）

`RuleCore.cs:18579` 原注释写「保持上一张表（**与改前同向**）」—— 改前这张表**从来没被挂过**，所以那句不成立。已就地改写为
「**保持上一张表（只在同一 ability 的前一行发布过时才存在）**」，并把「对随机型 op 是错的来源」这条**已知近似**与「今天零可观测」的量化证据写进注释（⛔ 不是留一份新账了事）。

### 零行数纪律（铁律 18·b / 账落点）

| 文件 | 改前 | 改后 | 说明 |
|---|---|---|---|
| `RuleCore.cs` | 18,748 | **18,748** | 两处改动都是**同一物理行改写**（`:18577` / `:18579`）⇒ 下游账落点零漂移；`b.count(b'\n')` 前后相同、行尾仍纯 LF（`crlf=0` / `bareCR=0`） |
| `RuleEngine/Editor/B141_W37_Assert.cs` | —— | 250（新） | 新文件（类名 == 文件名，`Editor/` 下，编辑器程序集已编过） |

---

## 二 · 验证原始读数

```
$ TMPDIR=/tmp/wf_b141_w37e bash 工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```

⚠️ **如实记**：本次有**一次** typecheck 读数不是 0/0 —— 运行时 **25** 条全落在 `Assets/CardPresentation/Deck/DeckRuntime.cs`
（`CS0103 DrawerTw / DeckCountInFull / _armPrevPx`）＋ 编辑器 1 条（`CS0006` 找不到 `WFCheck.dll`，= 运行时失败后的级联假错，
见 CLAUDE.md「旧 DLL 级联假错」那条）。那是**别的写手的在飞文件**（`git status` 里 `DeckRuntime.cs` 是 M），**与本件无关**；
20 分钟后复跑即 **0/0**。

```
$ bash 工具/ruleprobe.sh play check
结算指纹：当次现算 = a0e9c63d7bfa33735f5dad8230a26edd · 基线文件 = a0e9c63d7bfa33735f5dad8230a26edd（一致）
--- ①-a 逐卡差异（主档）---（空）      --- ①-b 逐卡差异（事件电池）---（空）
=== 19:20:26 结束（rc=0）===

$ bash 工具/ruleprobe.sh play all dump <out.tsv>    # 改前 / 改后各一次
$ diff before.tsv after.tsv   ⇒ 空（IDENTICAL）   # 各 1,143 行 = 表头 + 1,142 张

$ bash 工具/ruleprobe.sh check
=== 哨兵合计：失败 0 条 ===
=== 全池 1134 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
=== 19:27:56 结束（rc=0）===

$ python -I -X utf8 工具/check_ledger_points.py     # 只读，未 --fix
合计：A 5966 条 · 通过 5570 · 漂移 347 · 失败 51
```

⚠️ 账闸这一组数**不是本件的读数**：本件两处改动都是**同一物理行改写**（行数 18,748 不变）⇒ **结构上不可能引入漂移**；
实测漂移还比本件开工时（`B141_W22` 报告里的 A 5966 / 漂移 399 / 失败 38）**少**，那是**别的写手**同时在改引擎文件（`git status` 里 `EffectResolver.cs` / `GameplayVariables.cs` / `DeckRules.cs` … 都是 M）带来的涨落。

---

## 三 · 断言（`RuleEngine/Editor/B141_W37_Assert.cs`，250 行）

| 断言 | 盯什么 | 灭自证（怎么才能红） |
|---|---|---|
| A0/A0b | `ActiveAbilityObject == -1` / `AlternativeAbilityObject == -2`（早退的根因） | —— 前提 |
| A0c | 夹具棋盘：己方恰 3 个**非督军** | —— 前提 |
| **A1** | `step = {30, 10, 0, -1}` ⇒ 表**非空且 = 己方 3 个** | **把闸改回 `‖ step[3] < 0` ⇒ 表恒 null ⇒ 红** |
| A1b | 表主人 = 施放者 | 删 `AbilityTargetTableOwner = source` ⇒ 红（消费端 `EffectResolver.cs:1927` 靠它认主人） |
| A1c/A1d | `ta=10` 只收己方、两个陪练都在表里 | 把 `FriendlyUnits/EnemyUnits` 读反 ⇒ 红 |
| **A2** | `step = {10, 10, 0, -2}`（替代行动对象）也放行 | 闸写成 `step[3] != -1` ⇒ 红（数据现读正好 1 行 `ab=-2`：`SW21`） |
| A3/A3b/A3c | `logic = -1`（`ca=462` 那行）**不重挂**：契约 + 表**对象身份**不变 | ⚠️ **如实标：这不是灭自证** —— `ActiveAbilityTargetCriteriaAt(-1,-1)` 自己就返 null（`CardDef.cs:2623`），删掉 `if (step[2] < 0) return;` 它**不会红**；它的作用是**钉住这条正当用途**（⛔ 修 bug 时别当同类一起删） |
| A4 | `ta=20 self` **不顶掉**上一张表（白名单仍生效） | 把 `ta` 白名单顺手放宽 ⇒ 红（改前改后都绿 ⇒ 专盯「修 bug 时最容易顺手犯的错」） |
| **A5/A5b** | **逐行重挂**：行 0（`ta=10` 己方 3）→ 行 1（`ta=0 any` 双方）⇒ 表**换了**，敌方那张进了新表 | 闸改回旧式 ⇒ 两行都不挂 ⇒ 红；把「按行重挂」退化成「只挂一行」⇒ A5b 红 |

**接线（paste-ready，调度台在 `RuleEngineTest.cs` 聚合行同一物理行、第一个 `//` 之前追加）**：
```
Guard("B141W37_Assert", B141_W37_Assert.Run); _pass += B141_W37_Assert.Pass; _fail += B141_W37_Assert.Fail;
```

---

## 四 · 逐张归因：为什么指纹/逐卡读数**一点没变**

### 4·1 可达面（`cards_engine.json` v49 现读，1,142 张）

| 口径 | 数 |
|---|---|
| 有 `activeAbilityLogic` 的卡 | **132** 张 / **133** 条 logic（`ab` = `-1`×132 · `-2`×1） |
| 其中 `gate = 1`（真会跑）的 logic 条 | 88 条（`gate = 0` 的 45 条是**本 build 死数据**） |
| 落进 `PublishRowAbilityTargetTable` 白名单（`ta ∈ {0,10,110,140,150}` ∧ 不限张数）的 (ab, lg) 行 | **47** 行（含 gate=0 的 16 行）⇒ **可达 = 31 行 / 27 张卡** |
| 可达行按 `ta` | `ta=0` 15 · `ta=10` 12 · `ta=110` 3 · `ta=150` 1 |

可达的 27 张（`id` / 名字 / 行）—— 这一整张表就是「本修今天**可能**影响到的全部」：

| 卡 | 名 | 可发布行 |
|---|---|---|
| `AM2` | Ursula Creed | lg0:ta0 |
| `AM10` | Cadian Standard Bearer | lg0:ta10 |
| `AM11` | Shock Trooper Sgt. | lg0:ta0 |
| `AM12` | Flamer Trooper | lg0:ta10 |
| `AM16` | Scout Sentinel | lg0:ta0 |
| `AM23` | Bombast Field Gun | lg1:ta110 |
| `AM28` | Chimera | lg0:ta0 · lg1:ta0 |
| `AM30` | Manticore | lg0:ta10 |
| `AM34` | Valkyrie | lg0:ta10 |
| `AM69` | Tempestor Sergeant | lg0:ta10 |
| `AM73` | Taurox Prime | lg0:ta0 |
| `DA3` | Asmodai | lg0:ta0 |
| `DA7` | Ravenwing Bikes | lg0:ta0 |
| `DA27` | Ravenwing Talonmaster | lg0:ta10 · lg1:ta10 |
| `DA65` | Chaplain Gabutheron | lg1:ta0 |
| `DA71` | Watcher in the Dark | lg1:ta0 |
| `DA75` | Master Zacharial | lg1:ta0 · lg2:ta0 |
| `SOR13` | Simulacrum Bearer | lg0:ta10 |
| `SOR24` | Simulacrum Celestian | lg0:ta10 |
| `SOR26` | Aestred-Thurga | lg0:ta10 |
| `SOR34` | Celestian Superior | lg0:ta0 |
| `SOR35` | Immolator | lg1:ta110 |
| `SW8` | Blackmane Reiver | lg1:ta150 |
| `SW23` | Fenrisian Drop Pod | lg0:ta0 · lg1:ta0 |
| `SW36` | Bjorn the Fell-Handed | lg0:ta10 |
| `UM105` | Dreadnought | lg1:ta110 |
| `UM108` | Brutalis Dreadnought | lg0:ta10 |

（`GOF*` / `SAU*` / `TAU*` 那一批 16 行**全是 `gate=0` 死数据** ⇒ 本来就走不到。）

### 4·2 为什么这 31 行一行都没改读数

`ctx.AbilityTargetTable` 全仓**只有一处**消费：`EffectResolver.cs:1925`，而且只在
`spec.Random && spec.Count < pool.Count` 那支**才吃表**（`else` 全走「池序 / 随机抽」）。逐卡核过：

| 候选（全池扫 `desc` 含 `random` 且有主动能力的卡） | 为什么不进那支 |
|---|---|
| `SW21` Wulfen Pack Leader（`Deal 3 damage to a random enemy troop`） | 它的 `activeAbilityTargets` = `ta=10 ∧ maxTargets=1` ⇒ **被白名单挡掉**（`ta != 0 && MaxTargets >= 1`） |
| `SW5` Hunting Wolf / `SW29` Sons of Morkai Eliminator（`Attack a random …`） | 同上：`ta=10 ∧ maxTargets=1` ⇒ 挡掉 |
| `AM70` Scion Medic（`Deploy a random friendly Infantry …`） | `ta=60` ⇒ 不在白名单 |
| `BL36` Sylar Hexscorn / `SOR72` Castigator / `UM90` Phobos Lieutenant | `ta=30 / 30,20 / 20` ⇒ 不在白名单 |
| `AM69` Tempestor Sergeant | `random` 在它的 **Regiment** 正文里，**不是**主动能力那行（`Duty: Give +1 Health to all friendly Infantry`） |

⇒ **「能发布表的行」与「随机型 op」在全池上交集 = 0** ⇒ 修复后表虽然真被挂上了，但**没有消费点** ⇒
`play check` 指纹与 `play all dump` 逐卡读数**必然逐字节不变**。这**不是**「改动没生效」：
`B140_WA` 的 C3/C4（用 `CardDef.ActiveAbilityObject` 当 `step[3]`）此前会红，修复后才是绿（本件 §三 A1/A2 同形，且加了 `logic<0` 与白名单两道反证）。

---

## 五 · `SAN27-UNION`：为什么**必须**跨 `BattleDriver`（本包没做，❓另立）

### 5·1 「只改 RuleCore 侧让事件带行下标」走不通 —— 四处硬约束

| # | 事实 | 判据（现读） |
|---|---|---|
| ① | `BattleEventTarget` **只有四格**：`Player / Slot / CardId / InstanceId`，**没有行标识**（也没有任何空位可借） | `RuleEngine/Core/BattleEvent.cs:886-892`；`BattleEvent` 另外那些 int（`Amount` / `Priority` / `Turn` / `AbilityIndex`）**各自有主**（`Priority` = 动画道 `lane`，`AbilityIndex` = 原版 `cardAbilities[i]` 的下标，被 605/faith 那一族与 `Keyword#<i>` 后缀消费）⇒ ⛔ 借位就是语义踩踏。**而 `BattleEvent.cs` 不在本件白名单** |
| ② | 消费端**按整卡**摊平：`ExpandAbilityBeats` 取 `e.AbilityTargetList` 后用**同一个** `tgtList` 给**每一拍**、每一个目标排一条 | `CardPresentation/Battle/BattleDriver.cs:19811-19865`（`targetCount = tgtList.Count`；`:19846-19865` 的 `for (ti < nTgt)` 里 `var t = tgtList[ti]`）⇒ 两行都带 `abilityTarget` 拍时每拍播**并集**（over-play） |
| ③ | 拍上**没有行归属**：`CardAnimBeat.ca` 是 **`CardAnim` 资产 GUID 字符串**（诊断用），不是 chosenAbility 号 | `CardPresentation/Battle/CardAnimOrder.cs:71-74`（`public string ca; // CardAnim 资产 GUID`）；`BattleDriver.cs:19830` 把它当字符串塞日志 |
| ④ | **数据侧也摊平了**：生成器的中间结构**有** `logicIndex`，但最终 emit 时按 `abilities[]` 拍平、**丢掉** `logicIndex` | `工具/gen_card_anim_order.py:242-243`（中间结构 `{"path","abilityIndex","logicIndex",…}`）vs `:580-588`（`for ab in c["abilities"] … steps.append(b)` ⇒ 只剩 `id/name/steps[]`）；产物 `Resources/card_anim_order.json` 现读 `stats.cards = 93 / steps = 158` |

⇒ **要彻底逐行，最少四处一起动**（全在本件白名单外）：
1. `工具/gen_card_anim_order.py`：`steps[]` 带上 `li`（中间结构已有，**改动很小**）并重生成 `card_anim_order.json`；
2. `CardAnimOrder.cs`：`CardAnimBeat` 加 `int li`；
3. `BattleEvent.cs`：`BattleEventTarget` 加 `int Row`（或 `BattleEvent` 加 `List<List<BattleEventTarget>>`）—— ⚠️ 该文件在账里有 `文件:行号` 落点（最深 `:874`）⇒ 按铁律 18·b 只能**追加在命名空间末尾**；
4. `BattleDriver.cs`：`ExpandAbilityBeats` 按拍的 `li` 取**那一行**的子表（`li < 0` / 对不上 ⇒ 退回并集 + `BeatSoundOff` 一次，⛔ 不静默）。

### 5·2 即便四处都改，还有一个**歧义**要先裁定

「拍 → 行」只能靠 `li`（③ 的 `ca` 是 GUID，不行）。而 `activeAbilityLogic` 的多行里，**`ca` 会重复** ⇒ 用 `ca` 反查行是**不唯一**的：
现读 132 张主动能力卡里 **53 张有多行**，其中 **12 张跨行 `ca` 重复**（例：`AM28` 两行都是 `ca=70`；`DA18` 两行都是 `ca=60`；`SOR13` 两行都是 `ca=30`）。
⇒ 结论：**必须以生成器的 `logicIndex` 为准**（那才是原版 `abilityLogic[j]` 的下标），⛔ 别在运行期按 `ca` 猜。

### 5·3 本包**已经做掉**的那一半（`B141_W22`，事件层的最大改进）

`RuleCore.cs:15632` 行首调 `AppendOneRowAbilityTargetsToEvent` ⇒ 事件的 `AbilityTargetList` 从「只挑一行」变成**各行并集**（按 `InstanceId` / `Player+Slot` 去重、只收零随机消费档）。
⇒ 表现层「有几个目标就排几拍」这一层已经**不缺目标**；剩下的缺口**只是「这一拍该播哪一行的目标」**（上表 ②）。本件按简报的边界**没有再往前推**。

---

## 六 · 顺手发现（⛔ 只报不改）

1. 🔴 **「上一张表」的陈旧窗口（本修引入的新近似面，今天零可观测）**：
   `PublishRowAbilityTargetTable` 对**非白名单行**（`ta=20/30`、`maxTargets>=1`、`set==null`）是 `return` ⇒ 表停在**前一行**。
   原版每一行都吃**自己**那行的 `GetTargets` 表 ⇒ 对「先发一行白名单、后发一行随机 op」的卡，效果层会拿到**别人那行的目标**。
   **今天不可达**（§四·2 交集为 0），且已在 `RuleCore.cs:18579` 注释里写死。**建议**（另立，需裁定）：非白名单行**清表**（`ctx.AbilityTargetTable = null`）比「留上一行」更接近原版「没有这张表就走自己那条路」——
   ⚠️ 但那会改 `B140_WA` C4 的口径（它断言 `ta=20` **不顶掉**上一张表），要一起裁定。
2. 🔴 **`ta=20 self` / `ta=30 target` 这两档其实**可以**安全进发布白名单**（它们建池**不消费随机数**：self 直接放施放者、target 直接放 `chosen`，判据 `AbilityLogic__GetTargets.c:184-207`）——
   进了之后效果层拿到的就是**本行**的表，而不是「上一行」的。今天同样零可观测，但与第 1 条一起做才能把这条链**结构上**做对。⚠️ 与 `B140_WA` C4 冲突，需调度台裁定。
3. ℹ️ `activeAbilityLogic` 现读 **45 条 `gate = 0`**（占 133 条的 34%）⇒ 「主动能力」那一族有三分之一的**本 build 死数据**；本件的可达面统计已按 `gate=1` 分开给（47 行 → 31 行）。
4. ℹ️ 数据里 `ab = -2`（替代行动能力对象）**只有 1 行**（`SW21`），且那行的 `maxTargets=1` ⇒ 今天 `-2` 这条放行**只能靠夹具验**（本件 A2 就是这条夹具）。

---

## 七 · 还差什么 / 未闭合

- **断言未执行**（无 Unity）；`B141_W37_Assert` 未接进 `RuleEngineTest.cs`（白名单外，交调度台）。接线串见 §三。
- **`SAN27-UNION` 彻底逐行**：§五·1 的四处文件 + §五·2 的裁定，**另立一笔**（建议归到「战斗表现 · 多目标」那条线，与 `B141_W22` 的边界条目合并）。
- **陈旧窗口 / `ta=20/30` 可进白名单**两条（§六·1、§六·2）需要一次裁定（与 `B140_WA` C4 的口径冲突），本件未动。
