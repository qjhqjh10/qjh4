# N1 · 引擎：`GSC55` 真语义修正 + 三件小账（2026-10-24）

> 执行代理 **N1**（有写权限）。白名单内只改了：
> `Unity/MyGame/Assets/RuleEngine/**` · `Unity/工具/ruleprobe*`（**本批实际没动**）· 本报告。
> ⛔ 未跑 Unity（未跑 `RuleEngineTest.Run`）· ⛔ 未动 git · ⛔ 未改 `CardPresentation/**` / `WarpforgeVFX/**` /
> `工具/` 下任何非 `ruleprobe*` 文件 · `CLAUDE.md` / `项目任务.md` / 任何 `任务总表.md` / `资料/历史/**` / `Pinned*Md5`。
> 行尾：`EffectResolver.cs` / `RuleEngineTest.cs` **CRLF 未翻**（改后逐文件二进制数过：
> `EffectResolver.cs` 12582/12582 · `RuleEngineTest.cs` 36657/36657）；`EffectText.cs` / `RuleCore.cs` /
> `CardDef.cs` / `RuleCodes.cs` / `RuleEngineTest_EntryPump.cs` / 新文件 = **纯 LF**（CRLF = 0）。

## 〇 · 本轮实跑读数

| 项 | 读数 | 判据 |
|---|---|---|
| `TMPDIR=/tmp/wf_n1 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（全部改完后的最后一次） | 每批改完都跑 |
| `ruleprobe play all dump` **改前 vs 改后** | **1127 行里只有 `GSC55` 一行不同**（其余逐字节相同） | `D:/tmp/n1_before.tsv`（改前）vs `D:/tmp/n1_final.tsv`（**全部改动之后的最后一次**）；`n1_after.tsv` / `n1_after2.tsv` / `n1_final.tsv` 三份也互相逐字节相同 |
| `ruleprobe check`（解析层） | **解析差异 0 行 · 哨兵失败 0 条** · 输出指纹 `31d298eb…` 与确认版一致 | 现跑（加 `giantkiller` 前缀**没有**改动任何现有解析结果） |
| `ruleprobe play check`（结算层） | 主档 **8 张**（= J2 报告里的 **7 张改前就红** + 本笔的 `GSC55`）· 电池 **463 张**（与 J2 报告同数）· 结算指纹 `6d4639fc4802b7b2f13212c47e21d62e` ≠ 基线文件 `5d81171a…` | ⚠️ **仍红是预期的**（见 §一·5：`PinnedPlayMd5` 是红线，本笔不重钉） |
| `gen_diag_runner.py --check` | ✅ **逐字节相同** · `Run()` 用例表 **308** ↔ 用例方法 **308** | 本笔**没有**新增 `Test*` 方法（理由见 `RuleEngineTest_N1.cs` 文件头） |

---

## 一 · `GSC55`「本局已死」修正（最重要的一件）

### 1·1 结论

**已落。** `Return up to 5 friendly troops that **died this game** to your deck …` 现在
**从墓地取件**（`BattleContext.DeadUnits`），**不再扫棋盘**；棋盘上活着的友方部队**一张都不会被收走**。

### 1·2 证据（判据 = 第一权威）

| 判据 | 内容 |
|---|---|
| 卡数据 | `cards_engine.json` 的 `GSC55.abilityTargets[0].ta = [**60**, 150, 150]`（`60 = TargetsAffected.inCemeteryFiltered`）· `abilityMultipliers[0].fm = [**5**, 0, 0]`（`up to 5` 的 `maxTargets`） |
| 目标解析 | `AbilityLogic__GetTargets.c:949-960`：`*(int*)(criteria+0x10) == 0x3c` ⇒ `BattleManager.GetCardsInCemetery(manager, includeMinions, includeSpells, includeWhispers, includeWeapons, …)` —— **候选取自墓地列表**，不是场上 |
| 候选顺序/过滤 | `BattleManager__GetCardsInCemetery.c`：走 `playerObject + 0x110` 那个列表的 `iVar5 = 0..n` **正序**（= **最早进墓的在前**）；排除 `cardType == 10`（`CardTypeOptions.Hero`）与 `*(char*)(u + 0x65) != 0`（`isRemnant`） |
| 为什么用 `DeadUnits` | 它与「本局阵亡」逐字对应（`Owner` / `DeathTurn` / `Instance` 都在）；`Discard` 混装打出的战术卡、也没有死亡时间 |
| 全池可达性 | `cards_engine.json` 1127 张现扫：`return` 动词 ＋ 目标短语含 `died` 的**只有 `GSC55`**（`UM61 The Chapter's Due` 那句 `return` 走 `choosecard` 的 `act=return`，**不经过 `DoReturn`**） |

### 1·3 改动

| 文件（符号名，⛔ 不按行号找） | 改了什么 |
|---|---|
| `Core/EffectText.cs` → `EffectText.CemeteryTargetScope(string)` | **新判据的唯一落点**：目标短语指向墓地时返回死亡时间窗（`"all"` / `"since_last_turn"` / `"this_turn"`），否则 `null`。⚠️ **刻意不加 `EffectTargetSpec` 字段** —— 那条结构体会被 `ruleprobe` 的 `TargetCols` 逐列打进基线，加一列 = 全池基线漂移、还要动 `Pinned*Md5`（红线）。 |
| `Core/EffectResolver.cs` → `DoReturn` 里那条 `foreach (var spec in specs)` | 进来先问 `CemeteryTargetScope(spec.Raw)`；非 `null` ⇒ 走新分支 `ReturnFromCemetery(...)` 并 `continue`（**排在 `ResolveTargets` 之前**） |
| `Core/EffectResolver.cs` → `ReturnFromCemetery(...)`（新方法） | ① 从 `ctx.DeadUnits` **正序**挑（`Owner` / `IsUnit` / 死亡窗口 / `CardCriteria.FromTarget(spec)` 三道筛），上限 = `op.Amount`（`up to 5`）；② 逐张 `TakeFromGraveyard(owner, instance)`（`Discard` 与 `DeadUnits` **一起**撤）→ 按 `op.Dest` 搬进 `hand` / `decktop` / `deck`（`deck` 按规则书 `:455` 洗入）；③ 发 `EvtKind.Return`（`Slot = -1`，因为它不在格位上）；④ **完全不碰棋盘**（不 `RemoveAt`、不 `Auras.Recompose`） |

`DoReturn` 末尾原有的 `ctx.LastCreated = moved` 一行不用改 —— 后面那句 `lower their cost by 3`
（`DoLowerCost` 的 `(指代上一张)` 分支）与 `EmitShowCardsWindow` 都从它取「刚回去的那批」。

### 1·4 断言（改坏就红）

| # | 断言 | 写在哪 | 🧨 怎么红 |
|---|---|---|---|
| a | ★ **棋盘上有活人时，一张都不许被收走**（3 活人 + 空墓地 ⇒ 3 张仍在场、0 张进牌库） | `TestW64ShowCardsWindow` ⑤ | **把 `DoReturn` 换回棋盘扫描** ⇒ 两条同时红（这就是改前的行为） |
| b | ★ **只有墓地里那 3 张回牌库**（活人留在场上、活人不在牌库里） | `N1Gsc55CemeteryReturn`（混场） | 同上；「墓地分支判断条件写反」也会红 |
| c | ★ 墓地里那 3 条记录**一起被撤掉**（`DeadUnits` = 0、`Discard` 里也没有） | ③④ ＋ 混场 | 只移一边 ⇒ 红（同一张卡能被捞两次） |
| d | ★ 摊开的窗 = **恰好那 3 张**、顺序 = **死亡先后** | ③④ ＋ 混场（顺序） | 改成倒序扫 `DeadUnits` / 从 `Discard` 按名字取 ⇒ 红 |
| e | ★ `lower their cost by 3` **按实例**钉在那 3 份上（5 费 → **2**） | 混场（`RuleCore.CostOf(ctx, p, inst)`） | `DoReturn` 忘填 `LastCreated` / 填成卡模板 / 退回「按卡 id 匹配全部副本」⇒ 红 |
| f | 前提类：那 3 张**真的进了 `DeadUnits`**、棋盘真的只剩督军 | ③④ | 夹具改回「摆活人」/用 `ApplyDamage`（不含死亡链）⇒ 红（防止假绿） |

> ⚠️ **夹具的诚实标注**：`TestW64ShowCardsWindow` 的 ③④ 原来摆的是**活着的**部队并在注释里写着
> 「我们引擎走棋盘扫描、⛔ 别改回去」—— 那是**按缺陷行为写的**。本笔就地改成「先弄死再回牌库」
> （`N1Kill` 走真入口 `RuleCore.Hurt`），并留了订正痕迹。

### 1·5 `play all dump` 的那 1 行差异（逐字节）

```diff
441c441
< GSC55	Eve of the Uprising	tactic	A 打出即结算	0	0	OK	1	1	0	3	6	1	-1	-1	0	10				0
---
> GSC55	Eve of the Uprising	tactic	A 打出即结算	0	0	OK	1	1	0	3	0	1	-1	-1	0	4				0
```

（列含义见 `工具/ruleprobe/PlayProbe.cs` 的表头：`mech` 6 → **0**、`changes` 10 → **4**。
夹具里**没有阵亡单位** ⇒ 现在这条效果**什么都不做** —— 与卡面「本局死过的那批」自洽。
其余 **1126 行逐字节相同**，两次 `after` 也互相相同。）

⚠️ **`ruleprobe play check` 仍然红**（**预期**）：
- 主档 **8 张** = 改前就红的 7 张（`AM43` / `BL64` / `DA28` / `EC58` / `GOF42` / `SW53` / `TAU58`，与 J2 报告 §〇 的「7 张」同数）**＋ 本笔的 `GSC55`**；
- 电池 **463 张**（与 J2 报告的改前读数**同数**）；
- 结算指纹 `6d4639fc4802b7b2f13212c47e21d62e`（基线文件是 `5d81171a…`）。
⇒ 要它变绿必须**重钉 `PinnedPlayMd5` 并重生成基线**，而 `Pinned*Md5` 是本笔的**红线**（⛔ 不许动）
⇒ **如实留在红**，请调度台在下次「准备提交」那一批里统一重钉（J2 报告 §〇 记的是同一件事）。

---

## 二 · `T03·b`：`initialHandModifier` / `initialManaModifier`

### 2·1 结论

**这两条【早就做完了】，不是缺项。** 2026-10-21 的 `T03·b` 已按 C2 的 F-04 / F-05 落地，
并且带着断言（`Editor/RuleEngineTest_T03b.cs` 的 `TestW61bInitialModifiers`）。
**本笔没有改任何代码**（只按 C2 的两条判据**复核**了一遍），因为改动会变成「同一件事的第二份实现」。

### 2·2 证据（逐条对上 C2 的判据）

| C2 条目 | 我们的落点（符号名） | 复核结论 |
|---|---|---|
| **F-04** 起手 `base = mod >= 0 ? base : base + mod`、`extra = mod >= 0 ? mod : 0`、后手再加 `secondExtraCards(+0x20)`、`param_3 != 0` 整份覆盖 | `Core/CardDef.cs` 的 `InitialHandModifier` doc ＋ `RuleCore.NewBattle` 的 `seatHand[seat] = Math.Max(0, startHand + mod)` ＋ 后手 `extraCards` 那一段；教程档由 `tutorial != null` 那一支整份替换 | ✅ 与 C2 的式子**总数等价**（C2 原文：「**两种情况下总数都是 `startingHand + mod`**」） |
| **F-05** `初始能量 = 模式基数 + hero.raw+0x11C`（**无条件加、无符号闸**） | `Core/RuleCore.cs` 的 `MaxEnergy` 式子里的 `+ p.Warlord.Card.InitialManaModifier` —— 落成**常量加项** | ✅ 与「原版 `totalMana` 一次性抬高、之后逐回合累加」等价（能量每个回合重算，加项恒定） |
| 数据侧「1,351 张全 0」 | `Data/CardDatabase.cs` 的 `initialHandModifier` / `initialManaModifier` / `initialSpiritStoneModifier` 三个字段（缺列口径 = **0**） | ✅ 现读 `cards_engine.json` 1127 张**一个键都没有**（生成器只在非 0 时落列）⇒ 读进来全是 0 ⇒ **零行为变化** |
| 顺带：`initialSpiritStoneModifier`（同一个 `ResetMana` 的第 4 个实参） | `NewBattle` 里的 `SpiritStones += sp` | ✅ C2 没点名，但同一份判据里有；已实现 |

### 2·3 断言（已有，本笔未改）

`TestW61bInitialModifiers`：① `+1` 起手 ⇒ 多 1 张、且**只影响自己那一侧**；② `+2` 灵魂石；③ `+1` 法力 ⇒ 第 1 回合 `MaxEnergy` +1。
🧨 删掉 `seatHand` 里的 `+ mod` / `MaxEnergy` 里那一项 / `SpiritStones += sp` ⇒ 各自红。

### 2·4 没查清 / 交出去的

- ⚠️ **唯一的口径差（不影响引擎语义）**：原版把 **正修正** 拆成「额外牌」单独一档
  （`PlayerHand._AddCardsToMulligan…` 里 `+0x34 = max(0, mod)`），换牌时给它们**不同的摆位档**
  （`SetupCardInMulligan(card, 20/10/0)`）。我们只算**总数** ⇒ 牌数一致、**摆位档不建模**。
  摆位是表现层的事（`CardPresentation` 不在白名单）；若界面要按「哪几张是督军额外牌」分组，得另开一笔。

---

## 三 · `T03·d` 的 `P2-8`：`berzerk` 选靶

### 3·1 结论

**已按原版补齐「候选口径」那一半**（本笔的改动）；**选靶评分那一半本来就已经是原版那一份**
（2026-10-23 `W64·B-01` 接的 `AiScorerHook` → `Data/SimpleAI` 的 `AI` 同一条评分链）。

### 3·2 证据

- **候选 = 黑名单两档**（判据 `BattleManager__GetPendingBerzerkUnit.c:49-57`，逐句现读）：
  `cardType != 0x14(20 Tactic)` ∧ `!= 0x28(40 Whispers)` ∧ `isRemnant(+0x65) == 0` ∧ `IsInPlay()`；
  再叠 `HasCurrentTrait(0x168 berzerk)` ∧ `CanAttackCard`。
  `CardTypeOptions`（`dump.cs:45710-45717`）= `Minion 0 / Hero 10 / Tactic 20 / Whispers 40`。
- **选靶**（C2 D-03 已闭合）：`BattleManager__ResolveEndTurn.c:44-59` 对每个待攻击单位调
  `AI.SelectBestAttackAction(…, unit, 0, 0)`；反编译逐句在 `RuleCore.ResolveBerzerkAttacks` 的 doc 里。

### 3·3 改动

`Core/RuleCore.cs` → `PendingBerzerkUnits`：`!u.Card.IsUnit`（白名单 `Type ∈ {unit, hero}`）
**换成** `u.Card.Type == "tactic" || u.Card.Type == "defence"` 就 `continue`（**黑名单**）。
doc 就地订正（原来写着「`IsUnit`（含 hero）⇒ 口径一致」，那是**不等价**的口径）。

⚠️ **行为差 = 0**：本池 `type` 只有 `unit 586 / tactic 446 / hero 56 / defence 39` 四种，
而 `defence` 正是原版那一档 `Whispers(40)` ⇒ 两种写法在池内**逐张等价**；
且池内带 `berzerk` 的卡 **0 张**（1127 张现扫）。`play all dump` 改前改后**逐字节相同**已实测。

### 3·4 断言（`N1BerzerkCandidateFilter`，由 `TestBeginTurn` 调用）

| 断言 | 🧨 怎么红 |
|---|---|
| ★ `unit` 型狂暴单位在候选里（正对照） | 删掉 `Has(Berzerk)` 判据 ⇒ 红 |
| ★ **`token` 型**（**池里没有的类型**）狂暴单位**也在**候选里 | **换回 `Card.IsUnit` 白名单 ⇒ 这一行红**（这就是「黑名单 vs 白名单」的唯一判别式） |
| ★ `defence`（= 原版 `Whispers 40`）型狂暴单位**不在**候选里 | 黑名单只排 `tactic` ⇒ 红 |
| ★ 候选恰好 **2** 个（交叉校验） | 上面任何一条改了 ⇒ 红 |

---

## 四 · `AttackGiantKiller` 的 trait 建模

### 4·1 结论

**已建。** `giantkiller`（`DefinedTrait.giantKiller = 340` / 反编译 `0x154`）现在：
解析认得出（两种拼法）、进 `Implemented`、**在 `IsValidTarget` 里有一条真闸**、
拒码 `ErrAttackGiantKiller(28)` + 词条 `Battle/Tips/AttackGiantKiller`。

### 4·2 证据（判据 = `IsValidAttackTarget.c:182-305`，逐支现读）

```c
if (HasCurrentTrait(param_2 /*攻方*/, 0x154 /*giantKiller*/)
    && !CardScript.IsDefender(param_3 /*目标*/, …)
    &&  get_cardType(param_3) != 10 /*Hero = 督军*/)  ⇒ 不合法
```
- `CardScript__IsDefender.c` = `cardState ∈ {2,3,15,17}`（在场）∧ 非隐身 ∧ `HasVanguardTrait`
  （`EntityScript__HasVanguardTrait.c` = 非隐身 ∧ 带 `vanguard`）；
- 词条字面量 `DAT_184289890` → `stringliteral.json` = **`Battle/Tips/AttackGiantKiller`**，
  官方本地化 = `You must attack the Warlord or a Front Line unit` / **`你必须攻击战将或前线单位`**
  （我们 `CardPresentation/Core/Loc.cs` 同键同值 —— 那句文案早就有了，缺的只是引擎建模）。

### 4·3 改动

| 文件（符号名） | 改了什么 |
|---|---|
| `Core/CardDef.cs` → `KeywordTable.GiantKiller = "giantkiller"` | 新常量 + 判据全文（语义 / 反编译 / 官方文案 / 池内 0 张） |
| `Core/CardDef.cs` → `KeywordTable.Implemented` | 加 `GiantKiller`（判据 = 那个时机**真有人读它**） |
| `Core/CardDef.cs` → `KeywordTable.Prefixes` | 两条拼法 `"giant killer"` / `"giantkiller"`（同 `drop pod`/`droppod` 那一档）；⚠️ 前缀撞车已核（既有条目里**没有**以 `g` 开头的） |
| `Core/RuleCodes.cs` | `ErrAttackGiantKiller = 28` + `Names`（中文名）+ `Terms`（`Battle/Tips/AttackGiantKiller`）＋ **两处过期注释就地订正**（原写着「GiantKiller 没建模 ⇒ 不映射」/「该建码的三支」） |
| `Core/RuleCore.cs` → `IsValidTarget` | 新闸：`attacker.Has(GiantKiller) && !target.IsWarlord && !target.Has(Vanguard)` ⇒ `ErrAttackGiantKiller`。**位置照原版**：Flying 那一段之后、召唤病之前 |

⚠️ **行为差 = 0**：池内 **0 张**卡带该 trait（`cards_engine.json` 1127 张现扫 `giantkiller` / `giant killer` 各 0 命中；
`cards_raw.json` 1,351 张的 `CardTraits` 也 0 张）；`ruleprobe check` 解析差异 0 行、`play all dump` 逐字节相同。

### 4·4 断言（`N1GiantKiller`，由 `TestFlyingMeleeBlocked` 调用）

| # | 断言 | 🧨 怎么红 |
|---|---|---|
| ① | `GiantKiller` / `Giant Killer` 两种拼法都认得出（`CardDef.Has`） | 漏 `Prefixes` 登记 ⇒ 构造期静默丢弃 ⇒ 红 |
| ② | 进了 `Implemented` | 不进 ⇒ 卡面白打 `*`（本行红） |
| ③ | ★ 打**普通部队** ⇒ `ErrAttackGiantKiller` | 删掉 `IsValidTarget` 那段 ⇒ 实得 `OK` |
| ④ | 打**前线（`vanguard`）**⇒ `OK` | 这一档一起拒掉 ⇒ 红 |
| ⑤ | 打**敌方督军** ⇒ `OK` | 去掉 `!target.IsWarlord` ⇒ 红 |
| ⑥ | 负对照：**没有**该 trait 的攻击者打普通部队 ⇒ `OK` | 写成「凡攻击者都要前线/战将」⇒ 红 |
| ⑦ | `TermKey(28) == "Battle/Tips/AttackGiantKiller"`、`Describe(28)` 有中文名（不是「未知错误码 28」） | 漏 `Terms` / `Names` 各自红 |

---

## 五 · 没查清 / 需要主对话在同步点确认的部分

1. 🔴 **本代理不能跑 Unity ⇒ `RuleEngineTest` 没有实跑过**（只在编译层验过：typecheck 0/0）。
   风险最高的四处（若红，优先按这四处判）：
   - `N1Kill` 走反射调私有 `RuleCore.Hurt`（签名/可见性一变就红，但**前提断言会先出声**，不会静默跳过）；
   - 混场夹具里「先杀 3 张、再摆 2 张活人」的次序（`CleanupDeaths` 会让棋盘补位 ⇒ 我把活人摆在**之后**，
     避免指错对象）；若 `Place` 与连续棋盘模型还有别的耦合，症状会是「前提：棋盘上 = 3 个」那条红；
   - `N1BerzerkCandidateFilter` 依赖「`BeginTurn` 之后 `ctx.Active == 0`」（与既有 berzerk 用例同一前提）；
   - `N1Gsc55CemeteryReturn` 的费用断言（5 → 2）依赖 `DoLowerCost` 的 `(指代上一张)` 分支真的拿到
     `ctx.LastCreated`（`DoReturn` 末尾那一句）。
2. ⚠️ **`play check` 仍红**（§一·5）：需要重钉 `PinnedPlayMd5` + 重生成基线 —— 那是**同步点**的事，本笔按红线**没动**。
3. ⚠️ **`giantkiller` 的两个未建模旁支**（都不影响本笔结论，如实记）：
   - `IsDefender` 里还有一道 `!HasCurrentTrait(0x280 = 640 precognition)`（**precognition 我们整条没建模**）；
     目标必须「在场」那一半在我们这里**恒成立**（`IsValidTarget` 早就拒了不在场的目标）。
   - 原版 `:211-222` 同族里还有 `summonRift` / `stun` / `pindown` 三档（`stun`/`pindown` 我们有码、`summonRift` 未建模）——
     **不在本笔任务里**。
4. ⚠️ **`GSC55` 的 `isRemnant(+0x65)` 闸没有对应物**：`DeadUnits` 只存卡模板 + 实例，分不出「它死时是不是残骸」。
   今天只有 `GSC55` 走这一支，且它写的是 `troops`（不是 `remnants`）⇒ 无行为差；已写进 `ReturnFromCemetery` 的 doc。
5. ⚠️ **`showCardsWindow` 的表现层那一半仍未接**（J2 报告 §2·5 已记）：`BattleDriver` 不订阅
   `EvtKind.ShowCardsWindow`，`CardPresentation/**` 不在本笔白名单 ⇒ 窗还开不出来。

---

## 六 · 顺手发现（本笔**已就地在文件里改掉**的过期记录，铁律 5）

1. 🔴 `Core/BattleEvent.cs` 的 `EvtKind.ShowCardsWindow` doc 里写着
   「`RuleEngineTest.TestW64ShowCardsWindow` 的 ③④ 钉着『打 GSC55 的全池结算与**改前逐字相同**』」——
   **本笔之后不成立**（结算按原版改了）。已就地订正，并写明「本事件纯展示」这个结论**不变**、
   变的是「打这张卡的局面读数」。🧨 不订正的话，下一个人会拿它当「不许改 GSC55」的依据。
2. 🔴 `Core/RuleCodes.cs` 两处过期（同一件事的两份说法）：
   - 「没有建码的**三**支（GiantKiller / Tainted / Coward）」⇒ 现在是**两**支；
   - 「`AttackGiantKiller` / `CantAttackTainted` / `CantAttackCoward` —— **不映射**」⇒ 前者的映射**已建**。
   - 另有「`Terms` 从 4 条变 **11 条**」⇒ 现为 **12 条**。
3. ⚠️ **`RuleEngineTest_EntryPump.cs` 的 `TestW64ShowCardsWindow` doc 里**
   「④ **零行为变化**（那 3 张卡照旧回牌库）」是按**缺陷行为**写的，已就地改成
   「④ 那批卡**从墓地**回牌库」＋「⑤ 负对照：棋盘上有活人、墓地空 ⇒ 一张都不许收走」。
4. ⚠️ **并发写手**：本笔作业期间 `Core/EffectText.cs` / `Core/RuleCore.cs` / `Core/CardDef.cs` /
   `Editor/RuleEngineTest.cs` 都**另有别人在写**（同一批的其他代理）。本笔全部是**追加式**改动
   （`Edit` 精确替换，⛔ 没回退任何人的行），行尾逐文件二进制核过**没翻**；
   `git diff --numstat` 里那些文件的行数变化**不全是本笔的**。
5. ⚠️ **`berzerk` 的「黑名单」这条口径今天**在池内**不可观测**（四种 type 上两种写法等价）。
   ⇒ 本笔的判别式只能造一个**池里没有的类型**（`token`）来钉「黑名单 vs 白名单」。
   这不算「造数据」——它在 `CardDef.Type` 的**合法取值域**里，只是池里暂时没有。

---

## 七 · 交付物清单（本笔 touch 过的文件）

| 文件 | 性质 |
|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/EffectText.cs` | 改（+ `CemeteryTargetScope`，唯一判据） |
| `Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs` | 改（`DoReturn` +1 分支 · + `ReturnFromCemetery`；**CRLF 未翻**） |
| `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` | 改（`PendingBerzerkUnits` 黑名单 + doc；`IsValidTarget` + GiantKiller 闸） |
| `Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs` | 改（`GiantKiller` 常量 + `Implemented` + `Prefixes` 两条） |
| `Unity/MyGame/Assets/RuleEngine/Core/RuleCodes.cs` | 改（`ErrAttackGiantKiller = 28` + `Names` + `Terms` + 三处过期注释订正） |
| `Unity/MyGame/Assets/RuleEngine/Core/BattleEvent.cs` | 改（`ShowCardsWindow` 的过期 doc 订正） |
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EntryPump.cs` | 改（③④ 夹具改成「先死再回」· ⑤ 换成主判别式 · 调 `N1Gsc55CemeteryReturn`） |
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | 改（`TestBeginTurn` 调 `N1BerzerkCandidateFilter` · `TestFlyingMeleeBlocked` 调 `N1GiantKiller`；**CRLF 未翻**） |
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_N1.cs` + `.meta` | **新增**（本笔三组断言体 + `N1Kill` / `N1OnBoard` 夹具） |
| `Unity/资料/普查产出_1023e/N1_引擎GSC55与三小账.md` | 本报告 |

⛔ 未改：`CardPresentation/**` · `WarpforgeVFX/**` · `工具/**`（含 `ruleprobe*`，本批**没动**）·
`CLAUDE.md` / `项目任务.md` / 任何 `任务总表.md` / `资料/历史/**` / `Pinned*Md5` · git。
