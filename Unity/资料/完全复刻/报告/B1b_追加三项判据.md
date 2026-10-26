# B1b · 追加三项判据（第八十三会话 · 完全复刻）

> 调度台追加的三条「战斗逻辑驱动 P0 级该做没做」。本报告只做**判据裁定 ＋ 施工方案** ——
> 三条的实现落点**全部在 `RuleEngine/Core/EffectResolver.cs` 之外**（`WhenEvent.cs` / `RuleCore.cs` /
> `CountEval.cs`），按简报「若缺口的实现不在 EffectResolver.cs，先写报告问调度台，⛔ 别硬改别人的文件」
> ⇒ 本报告成文时**一行代码都没改**，账已更新（`账/_parts/B9_B1b.tsv`）。
>
> 🔴 **调度台 2026-10-26 已逐条裁定**（本文件已按裁定更新）：
> ① `BroadcastCardCreated` ⇒ **D ＋ 时间限定，不做发送侧**（只加发送口 = 死代码）·
> ② `BroadcastTrapResolved` ⇒ **D ＋ 时间限定**（同意原判断）·
> ③ `BoardAnalysis.MeetsCriteria` ⇒ **保持 C**，但**把 19 条 count 行的筛维度缺口接着做**（白名单扩到 `CountEval.cs`）。
> ③ 的落地过程与读数全部在 **`报告/B1c_CountEval筛维度落地.md`** —— 本文件 §③ 只留裁定后的结论。
>
> 判据优先级：🥇 `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**/*.cs` · 🥈 `D:/2/tools/decomp_full/**/*.c`。
> 本轮**没有碰** `RuleCore.cs`（只读它拿行号），也没有碰 `WhenEvent.cs`；
> `CountEval.cs` 是 ③ 落地时按调度台扩的白名单改的（+112/−27）。

---

## ① `BattleManager.BroadcastCardCreated(CardScript createdCard)` —— **判 D ＋ 时间限定（真缺，但按裁定不做发送侧）**

### 结论
**(a) 我们真没实现**：发送侧、消费侧都没有。`CardCreatedInHand` / `210` 在 `Assets/` **全仓 0 命中**
（`CardDef.cs:1926` 只是 trigger id 的**名字表**，没有任何消费者）。

### 原版体（逐句）
| 位置 | 内容 |
|---|---|
| `out_cpp2il_dev_ilspy/BattleManager.cs:27832-27857` | ① 棋盘一跳：`unitsInPlay` 循环，`if (cardScript.IsInPlay())` → `ReactToCardCreated(createdCard)`。② **一次**手牌跳：`GetCardsInHandList(…)` → 逐个 `ReactToCardCreated(createdCard)` |
| `out_cpp2il_dev_ilspy/CardScript.cs:15998-16004` | `ReactToCardCreated(CardScript createdCard)`：**只发** `OnTrigger(210 CardCreatedInHand, mgr, this, actingCard = default, targetCard = default)`。🔴 **`createdCard` 实参在体内根本没用**、且**没有自排除** |
| `out_cpp2il_dev_ilspy/RawCardScript.cs`（OnTrigger 表） | 210 = `CardCreatedInHand` |
| `.c` `decomp_full/BattleManager__BroadcastCardCreated.c:41` | **`GetCardsInHandList(param_1, 1,1,1,1,1,1, 0)`** —— 六个 bool **全 true** |

### 🔴 简报点名的陷阱（已核实并写进账）
`GetCardsInHandList` 的签名是 **六个** bool（`inclMinions, inclSpells, inclWhispers, inclWeapons,
inclPlayer, inclEnemy`），ILSpy 把**后三个**反编译成 `default`(=false)。`.c` 里六个全是 `1`
⇒ **双方手牌 × 全卡种（含 weapons）**。**⛔ 不能照 ILSpy 判「我们缺就是缺/原版扫得少」** ——
原版扫的是**最多**的那一档。旧账三行都写着「七个 true」，本轮一并订正为 **六个 true**。

### 本 build 的真实调用点（`decomp_full` 全树 grep，只有 2 处）
1. `BattleManager._ResolveCreateHandCard_d__512__MoveNext.c`（= 造牌**进手牌**）
2. `BattleManager._ResolveRecallToHand_d__542__MoveNext.c`（= **墓地→手牌**那一支，紧跟在 `BroadcastCardReturnedHand` 之后）

⚠️ 旧账写的 `_ResolveCreateDeckCard` 在本 build **不存在**（那是别的版本的记忆）。

### 施工方案（3 处，2 处不在本代理白名单）
| # | 文件 | 做什么 | 在白名单内？ |
|---|---|---|---|
| ① | `RuleEngine/Core/WhenEvent.cs` | 加 `WhenEventKind.CardCreatedInHand = "cardcreatedinhand"` ＋ 卡面短语解析 ＋ `EffectResolver.TriggerNameOfWhenKind` 补 `→ 210` | ❌ |
| ② | `RuleEngine/Core/EffectResolver.cs` **:3995** | 造牌进手那一支：`foreach (m in landed)` 之后逐个 `BroadcastCardCreated`（原版 `ResolveCreateHandCard` 一个动作一张） | ✅ **可直接做** |
| ③ | `RuleEngine/Core/EffectResolver.cs` **:6675** | 墓地回手那一支（`ReturnFromCemetery` 的 `case "hand"`）：`AddCardToHandWithinLimit` 成功后发一次 | ✅ **可直接做** |

### 裁定（调度台 2026-10-26）—— **D ＋ 时间限定，不做发送侧**
- **不做发送侧**的理由（与本报告原建议一致）：**只加发送口 = 死代码**。要做必须三处一起：
  发送口 ＋ `WhenEvent` 的 Kind ＋ **短语表**；而短语表**定不了**（池内 `abilityTriggers` 含 210 = 0 张
  ⇒ 没有任何卡面文字能定 210 的解析短语）⇒ 属于**铁律 11 例外①「判据是空的」**。
- **差异列写法**：`原版有实现、本 build 出货数据 0 入口（池内 abilityTriggers 含 210 = 0 张；
  原版真实调用点只有 _ResolveCreateHandCard 与 _ResolveRecallToHand 两处）—— 发送侧也未做，
  但 0 张监听 ⇒ 可观察差异 = 0`
- **备注列写法**：`时间限定 = 若将来出现 abilityTriggers 含 210 的卡，需三处一起补
  （发送口 + WhenEvent Kind + 短语表）`
- ⛔ **别只做发送侧两处**（那会留下 0 调用点的死代码，下一批还得再挖一遍）。
- 判据侧已查清到可施工（两个发送落点 = `EffectResolver.cs:3995` 造牌进手 / `:6675` 墓地回手），
  已写进账的备注列，留给将来那次三处一起补。

---

## ② `BattleManager.BroadcastTrapResolved(CardScript trapCard)` —— **判 D ＋ 时间限定（真缺，且上游整条链都不存在）**

### 结论
**(a) 真没实现**，而且比 ① 更彻底：**我们连「抽到陷阱 → 陷阱结算」这条链都没有**，
所以「发送口该插在哪」都不存在（只有监听侧 `HandTrapWhens`，`CardDef.cs:5462`）。

### 原版体（逐句）
| 位置 | 内容 |
|---|---|
| `out_cpp2il_dev_ilspy/BattleManager.cs:27859-27888` | ① 棋盘一跳（`:27866` `IsInPlay` → `:27869`）。② 一次手牌跳（`:27876`，六个 true；**排除 `trapCard`** —— `:27881` `cardScript3 != trapCard`） |
| `out_cpp2il_dev_ilspy/CardScript.cs:12093-12096` | `TrapResolved(actingCard)` → `OnTrapResolved(mgr, this, actingCard)` |
| `out_cpp2il_dev_ilspy/RawCardScript.cs:1927-1932` | `OnTrapResolved` → `OnTrigger(270 TrapResolved, mgr, thisCard, actingCard = default, targetCard = default)` —— ⚠️ **传进来的 `actingCard` 也没往下用** |
| 唯一调用点 | `decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c`（= 抽卡那条链里） |

### 与 ① 的两处结构差异（值得记）
1. ①的**手牌跳不自排除**；②的**手牌跳排除 `trapCard`**（棋盘跳两边都不排除）。
2. ②的触发源实参 `actingCard` 在接收方体内被丢掉，和 ① 一样。

### 建议：**按 D ＋ 时间限定**（与原账 `CardScript.TrapResolved` = D 一致）
依据（`docs/方案 §4.1` 的 D 档：原版有实现、数据 0 入口）：
- 池内 `abilityTriggers` 含 **270 的 = 0 张**；
- 陷阱机制本身 **0 入口**：`spellType 70` = 0 张 · 卡面 `desc` 含 `trap` = 0 张（旧账同口径）。

若调度台要求按铁律 11 把休眠机制建起来，**三处必须一起做**（缺任一处就是死代码）：
`RuleCore.Draw`（抽卡链，❌ 不在白名单）补「抽到 `spellType==70` ⇒ 当陷阱结算」整条链 →
`WhenEvent.cs` 加 Kind ＋ 短语（同样**无卡面短语判据**）→ `EffectResolver.cs` 加发送口。

---

## ③ `BoardAnalysis.MeetsCriteria(…)` —— **保持 C；(b) 实现了、口径对齐，缺口不在随机档而在「筛维度」**
> ✅ **调度台 2026-10-26 裁定**：这一段不动代码（写进差异列即可）；
> 🔴 但**「19 条 count 行 / 14 张卡被 `SetGap` 挡成 null」这一格接着做** ——
> **已落地**，白名单扩到 `RuleEngine/Core/CountEval.cs`。
> 五类筛维度的「值 → 语义」映射、三处改动、可达性、灭自证读数、指纹，
> **全部在 `报告/B1c_CountEval筛维度落地.md`**（本文件不再重复）。

### 结论
**不是「真没实现」，也不是「判据读不出」** —— 主结构都在 `RuleEngine/Core/CountEval.cs`，
`ignoreRandomTrigger` 也**真的**当第 6 实参接进去了。⇒ **保持 C**（方案 §4.1「行为不同 ⇒ C」），
但缺口应该**重新收敛**（下面是本轮新增的可达性普查）。

### 原版体（`decomp_full/BoardAnalysis__MeetsCriteria.c`，逐段）
| 行 | 内容 |
|---|---|
| `:44-49` | `boardCriteria == null` ⇒ **true**；`countCriteria(+0x18) == null` ⇒ **true** |
| `:50-64` | `playerTurn(+0x10)`：`10(Self)` 要 `IsPlayerTurn == thisCard.isPlayer`；`20(Enemy)` 要**不等** ⇒ 判假整条 `false` |
| **`:65-96`** | **`ignoreRandomTrigger == false && triggerIf(+0x24) == 8 && referenceValue(+0x28) > 0`** ⇒ `roll = GetRandomInt(0,100)`、打一行 log（卡名 + ref + roll）、**`return roll < referenceValue`** |
| `:97-131` | 否则：`countCriteria.container(+0x10) != 0` ⇒ `AbilityLogic.GetTargets(…, checkMaxTargets:1)` 数个数；`countOptions(+0x20) == 10` ⇒ 同方 −1 / 异方 +1 累加；再按 `triggerIf`：`1 EqualTo` / `2 EqualOrHigher` / `3 EqualOrLower` |
| `:132-155` | `triggerIf` 其它值（4-7 / 9-11）⇒ `LogError` 后**落到 `return true`**；`container == 0` ⇒ `return true` |

### 我们的对应物
`CountEval.MeetsBoardCriteria`（`CountEval.cs:234`，含 `ignoreRandom` + `cond`）·
`CountEval.Meets`（`:56`/`:63`，`role="count"` 那条）·
`CountEval.SetGap`（`:365`）· `CountEval.IncludeUnit`（`:678`）· `CountEval.Compare`（`:697`）。
调用方：`ActionPump.BoardLevelCriteriaAllows:1606`（传 `ignoreRandom: false`）· `CardDef.UsesAlternativeAbility:300`。

### `ignoreRandomTrigger` 的裁定（简报点名要判的那一格）
- 原版这一支**有实体意义**：`ignoreRandom == false` 时真掷 `GetRandomInt(0,100)` 并比 `referenceValue`。
- 我们**未实现掷法** ⇒ `triggerIf == 8 && refValue > 0 && !ignoreRandom` 返回 `null` + 出声
  （= 「判不了 ⇒ 放行」，**等价于 `ignoreRandom = true`** 的语义）。
- 🔴 **但它今天零可达**：全池 `role="count"` **99 行** 的 `triggerIf` 分布 =
  **`{0:32, 1:17, 2:36, 3:14}`**（逐行数，`cards_engine.json`）⇒ **`triggerIf == 8` = 0 行**；
  卡级 `normalAbilityBoardCriteria` 只有 2 张（SW9 `triggerIf=1` · SW21 `triggerIf=1`）⇒ 也没有 8。
  ⇒ **这一格不是「P0 该做没做」的可观测项**（仍按铁律 11 要做）。
  真要实现，必须照 `CLAUDE.md` 那条红线：`MatchData` **开局预烧 100 个整数 + 按下标取** ——
  ⛔ 不能用 `ctx.Rng.Next` 现场掷（分布不同）。

### 🔴 本轮新发现：**真正可达的缺口是「筛维度」——19 行 / 14 张卡**
`CountEval.SetGap`（`:365`）对下列字段一律返回 gap ⇒ `null` ⇒ 退正文正则兜底 + 出声：

| 筛维度 | 命中行数 | 涉及卡（`ab` 序号） |
|---|---|---|
| `keywordFilter` | **12** | AM76(0,1) · DA44(1) · EC54(0,1) · SAU57(0,1,2,3,4) · GOF79(0) · GOF94(1) · GOF5(0) · `8f35df05…`(1)；值 ∈ {5,10,35,40} |
| `traitsFilter` | **2** | ASH85(0) = 70 · **SW9 卡级** = 1260 |
| `currentHealthFilterOption` | **2** | AM54(1) = 3 · GOF105(1) = 2 |
| `filterSpecificCards` | **1** | GOF15(0) = `[GOF12, GOF21]` |
| `selectFromChooseResolution` | **1** | ASH82(0) |

（同一批普查里：`countOptions` **99 行全 0** ⇒ `EnemiesMinusAllies(10)` 零可达；
`maxTargets` **全 0** ⇒ 「④ 未截断」零可达；`playerTurn` 我们有 `ctx`/`owner` ⇒ ⑤ 不触发。）

另有卡级两张：**SW21**（`hasAlternativeAbility=1`，"Deal 3 damage to a random enemy troop, or 5 if you
control no other troops"）的 `normalAbilityBoardCriteria` 全部落在已实现档
（`playerTurn 10 / ta 10 / triggerIf 1 / ref 0`）⇒ **可真判、已生效**；
**SW9** 的 `traitsFilter=1260` ⇒ 判不了（且它 `hasAlternativeAbility` 缺列，先被上一句挡掉）。

### 施工方案 —— ✅ **已落地**（`RuleEngine/Core/CountEval.cs`，+112/−27）
1. ✅ `SetGap` 放开 `keywordFilter` / `traitsFilter` / `currentHealthFilterOption` /
   `filterSpecificCards` / `excludeRace` / `excludeTrait` 六格。
2. ✅ 棋盘跳 + `ta=150` 改走 **`TargetCriteriaEval.Matches`**（全仓唯一那份 36 格判据，
   ⛔ 没有在 `CountEval` 里另写一份）。
3. ✅ 墓地跳（`ta=60`）新增 `CemeteryCandidatePasses`（候选是 `DeadUnit`、没有运行期状态
   ⇒ 只判静态的 `keywordFilter` / `filterSpecificCards`，其余 `null` + 出声）。
4. ⏭ **仍未做**：`triggerIf == 8` 的掷法（必须先建「`MatchData` 预烧 100 + 下标」那张表，
   ⛔ 不能用 `ctx.Rng.Next`）· `maxTargets` 截断（池内全 0）。
   —— 逐条细节、值→语义映射、可达性、灭自证读数 → **`报告/B1c_CountEval筛维度落地.md`**。

### 没查清的
- `keywordFilter` / `traitsFilter` / `currentHealthFilter` 的**值→语义映射**没读（原版是
  `KeywordFilter` / `TraitsFilter` / `StatFilter` 三个小结构体，`dump.cs` 里各有 `+option` 字段）。
  这一格必须在动手前再读一次 dump（`FilterMethods__CheckIfMeetsCriteria.c:300-420`），
  **本轮只做到「哪 19 行被挡住」**，没有做「值怎么解」。
- 旧账那句「这 99 行的 ability trigger ∈ {0,40,610,30,410,300,240,600,10,41,31}，都不在今天
  接线启用的七族里 ⇒ 今天生产零命中」**沿用 G17 的读数，本轮没复算**。

---

## ④ 验（①② 未改代码；③ 的落地读数在 `B1c` 报告 §五，A/B 实测改前＝改后）
- `typecheck` ⇒ **运行时 0 / 编辑器 0**（③ 落地后复跑）。
- `ruleprobe check` ⇒ **rc = 0**（解析差异 0 · 哨兵 0）。
- `ruleprobe play check` ⇒ 指纹 改前 = 改后 = **`af8e817839fa1dd199d33320f6bf1b2e`**
  （基线 `6e888bbe…` 早就不符，A/B 隔离已证是并发写手造成）。电池段 5 张卡同前。
- **没有可观测前后对比**：①② 零可达（0 张监听 / 上游整条链不存在）；
  ③ 是判据闭合（board 闸今天没接到那 19 行的 trigger 族），灭自证那一步见 `B1c` §四。

## ⑤ 有没有撞到 `RuleCore.cs` 的活
**没有。** 本代理对 `RuleCore.cs` **只读**（取 `:12345` / `:12605` 两条行号当判据），
`git status` 里 `RuleCore.cs` 的改动全部来自别的写手（`A3`），我没有写过一个字节。
也未动 `ActionPump.cs` / `BattleDriver.cs` / `Editor/*.cs` / `Scenes/` / `CLAUDE.md` / `项目任务.md` / 主账表。
