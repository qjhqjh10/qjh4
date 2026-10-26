# B9 · W12MN —— 基础设施 P0 · `PlayerManager` mana 族（第八十四会话 · 2026-10-26）

> **身份**：执行型写手 `W12MN`。**只碰**：`RuleEngine/Core/PlayerState.cs`（类尾追加）+ `账/_parts/B9_W12MN.tsv` + 本文件。
> ⛔ 未碰：`RuleCore.cs` / `EffectResolver.cs` / 其余 `RuleEngine/Core/*.cs` / `RuleEditor/RuleEngineTest.cs` / `CardPresentation/**` / `Shell/**`（都在别人手上）。
> ⛔ 未跑 Unity 宿主、未动 git、未改主账表、未改两张正本。
> **判据源**：`d:/2/tools/decomp_full/PlayerManager__*.c`（第一）· `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy`（第二，交叉）· `d:/2/新解包资源/assets_full`（第三：UnityEvent 挂接）· `cards_truth.json`（现读卡池数据）。

---

## 一 结论（一句话 + 数字）

**这 34 条不是「34 件要做的事」** —— 逐条对完账后的真实形状是：

| 半 | 条数 | 本批定案 | 代码 |
|---|---|---|---|
| **`PlayerState.cs` 那半**（纯取值口） | **16** | **A 7 · B 1 · C 1 · D 7** | ✅ 新增 **7 个转发取值口**（类尾，+90 行，**纯加法、零行为变化**） |
| **`RuleCore.cs` 那半**（改状态/结算） | **18** | 建议 **A 2 · B 12 · C 1 · D 3**（见 §五） | ⏸ **本批未动**（`RuleCore.cs` 是最热的共用文件，且在别人手上；动手清单已写死 → §五） |

> **一句话**：这 34 条里**没有一条需要「新建一整套 mana 系统」** —— 引擎侧状态（Energy/MaxEnergy/Faith/SpiritStones/QuestPoints/SecretsPlayed/ManaCarry）与结算（`SpendNormalMana` / `QuestPointThreshold` / `DoFactionResource` / `DoEnergy` / `DoRefill`）**早就都在**，缺的是**逐方法落点**；而其中 **7 条是原版自己的死代码/死功能**（不该做，⛔ 别去建演出或死成员）。

**本批唯一要写的代码 = 7 个转发取值口**，理由三条（也写进代码注释）：
1. 施工单 W22 ③ 明写「13 个 getter 直接转发」；
2. 账的 **A 档要求落点是方法级声明** —— `工具/check_ledger_points.py` 的 `looks_like_decl` **不认** `public int Faith;` 这种「无 `(` / 无 `{` / 无 `=>`」的字段行（先例 `PlayerState.cs:19（Deck）` 能过全因 `= new List<CardInstance>()` 带括号）⇒ 只有转发方法能让那 7 条成为**可机检的 A**；
3. 原版这 7 个名字是对局内读这几条计数的**唯一官方口**，将来把 `EffectResolver` / `RuleCore` / `AiTargets` 里那些直读字段的调用方迁过来时一次改一处。

⚠️ 这 7 个方法**今天没有调用点**（消费者仍直读字段）—— 它们是**取值口**、不是「死机制」：自己不做任何判断，⛔ **别往里面加规则**。这一点已在代码注释里写死。

---

## 一·b 插单（coordinator 中途加急）：`pendingHealing` 的**清空点**（✅ 已做完）

> 这条**不是** mana 族，但它配的是 `E11RC` **刚接上的**那一笔；不配 ⇒ **真实对局里会算错**（`damaged(40)` 被旧账压成假）。

### 1b·1 判据（我现读复核了 coordinator 给的链，**成立**）
- `decomp_full/CardScript._Heal_d__468__MoveNext.c` 是 `CardScript.Heal` 的**协程状态机**，三个状态：
  · 状态 0 = 加血；· 状态 **1 = 收尾**；· 状态 2 = 结束。
- **清空点在状态 1**（`:73-81`）：`lVar2 + 0x100`（= `pendingHealing` 那张表）→ `Count = 0` ＋ `Array.Clear`（= `Clear()`）；随后 `UpdateHealthText`。
- **还有第二条会清的出口**（coordinator 的简述里没有，但同一个判据里有）：状态 0 里 `if (local_res8[0] < 1) goto LAB_1806112ce;` —— 即**实际治疗量 < 1**（满血 / 溢疗归零）时**跳到同一段清空尾**（`Array.Clear` 在 `LAB_1806112ce`）。
- **不清的两支**：`amount ≤ 0`（`:105` 的 `Tried to heal with negative value`）与 `corrupt`（`:107` 的 `Tried to heal corrupt unit`）都是**直接 `return 0`**，**不清表** ⇒ 我们**照抄**（⛔ 别「顺手修好」，铁律：原版自己也有 bug 就照抄）。
- ⚠️ 所以比"只在 `return healed;` 前加一句"要**多一条**（`healed <= 0` 那支也要清）—— 否则「先溢疗、后挨打」这条序列仍然错（`pending=3` 残留 ⇒ 下次 `damage 3` 被判 `damaged = 假`）。这是**判据驱动的一处偏离**，已在代码注释里写明理由。

### 1b·2 改动（3 处 + 类尾 1 个新成员）
| 位置 | 内容 |
|---|---|
| `RuleCore.cs:9212`（`HealUnit` 的 `healed <= 0` 支） | 补 `ClearPendingHealingEntry(ctx, u);`（原版那一支是共享清空尾） |
| `RuleCore.cs:9228`（`return healed;` 之前） | 补 `ClearPendingHealingEntry(ctx, u);` |
| `RuleCore.cs:14911`（**类尾**） | 新增 `static void ClearPendingHealingEntry(BattleContext, UnitState)`：`PendingHealingAux` 的 **`SumByInstance` 与 `CountByInstance` 两个字典都 `Remove`** |
| `RuleCore.cs:9184-9190` | 注释就地订正：「⛔ 永不清理（原版行为）」→ **在途治疗，治疗协程收尾即清** + 清空点出处 |
| `RuleCore.cs:14500-14507`（`PendingHealing` 判据块） | 就地订正：「全树没有任何 Clear / reset ⇒ 粘性累计预测表」**不成立**；写明清空点 = `_Heal_d__468__MoveNext.c:73-81`，`B9_C2` 当年只 grep 了 `RecordPendingHealing` 本身 |
| `RuleCore.cs`（`RecordPendingHealing` 的 doc 与日志串） | 「永不清理 / **粘性、永不清理**」→「清空点在**治疗协程收尾**」 |
| `RuleCore.cs:3676-3677` | 「`UnitState` 都是新造的」这个理由对**残骸翻回**已不精确（`W10WR` 起走 `ReanimateRemnantInPlace` **原地翻**、复用同一实例；归零点 = `ResetToValuesInHand` 的 `AttacksThisTurn = 0`）⇒ **结论不变、理由改准**（**零行数变化**） |

### 1b·3 副作用 / 需要同步点知道的两件事
1. **`RuleCore.cs` 净 +48 行**（14873 → **14921**，行尾仍纯 **LF**：`CRLF 0 / LF 14921 / 裸 CR 0`）。
   ⇒ 账里指向 `RuleCore.cs` 的 **460 条**落点里，`9181`（`HealUnit` 的**声明行本身未动**）之后的会漂：
   · 全表 `> 9217` 的 **173 条** → **+11**（前段注释 +4 / `healed<=0` 支 +6 / `return` 那一句 +1）；
   · 其中**只有 `A` 行参与机检**（`B`/`C`/`D` 行不查落点）⇒ 若这 173 条里既有 A 也有 B，混着看即可；
   · 别人那两条相关行：`CardScript.Heal`（落点 `RuleCore.cs:9181`）**未漂**；`CardScript.RecordPendingHealing`（原 `:14514`）→ **`:14532`**，但它是 **B** 行（不参与机检）⇒ **不需要**为它改账，**但请它的所有者把「粘性累计」那半句备注按本节改掉**（铁律 5）。
   ⇒ **不是失败**（工具口径：行号是快照），同步点 `check_ledger_points --all-A --fix` 按**符号名**自愈即可。
2. **`play check` 逐字节没变**（A/B 隔离结论，见 §四）：改前改后都是 `1b40f42a…`、差异都只有 4 张（BL47 / BL18 / SW25 / UM18 = 别人的活）⇒ **这条修正不被 `play` 夹具覆盖**（夹具里没有「先溢疗、后挨打」的序列），所以**必须**靠 `RuleEngineTest` 断言兜（建议断言见 §五·4）。

---

## 二 34 条逐条对账表

> 「本批做」= 状态与落点已进 `账/_parts/B9_W12MN.tsv`；「留给下一路」= 状态结论已定、代码与账行留给 RuleCore 那批。
> 落点里的行号是**2026-10-26 现读**（`PlayerState.cs` 现 438 行；类尾追加后**旧行号全部未动**，最大旧落点 328 < 346）。

### 2·1 `PlayerState.cs` 那半（16 条 · 本批已定案）

| 账行 | 方法 | 结论 | 落点 / 说明 |
|---|---|---|---|
| 564 | `GetCurrentFaith()` | **A** | `PlayerState.cs:384（GetCurrentFaith）` → `return Faith;` |
| 566 | `GetCurrentQuestPoints()` | **A** | `PlayerState.cs:399（GetCurrentQuestPoints）`（**余数**语义；减阈值在 `EffectResolver.cs:13046`） |
| 567 | `GetCurrentSpiritStone()` | **A** | `PlayerState.cs:391（GetCurrentSpiritStone）` |
| 574 | `GetSavedMana()` | **A** | `PlayerState.cs:416（GetSavedMana）`；值语义 = `UpdateManaLeftAccumulation.c:51-57` 的 `min(manaAccumulation, unused)` |
| 575 | `GetSecretsPlayed()` | **A** | `PlayerState.cs:407（GetSecretsPlayed）`；唯一写点 `EffectResolver.cs:2517` |
| 577 | `GetTotalMana()` | **A** | `PlayerState.cs:425（GetTotalMana）` → `MaxEnergy`（累加器 vs 公式重算，逐项等价） |
| 578 | `GetUnusedMana()` | **A** | `PlayerState.cs:433（GetUnusedMana）` → `Energy` |
| 579 | `HasEnoughMana(required, psychic, plasma, spiritStone)` | **B** | 两处内联可负担闸：`RuleCore.cs:3301（CanPlayCard）` + `EffectResolver.cs:1964（CanPlayTactic）`。**给 B 不给 A**：我们侧没有「四选一取池」形状，只有两处内联比较，另三支未建模（见 §三·3） |
| 563 | `GetCurrentMaintenance()` | **D** | 原版 private + **三源零调用**；同一条「维护费合计」的活路径内联在 `PlayerManager__TurnStart.c:99-124` ≡ 我们的 `RuleCore.cs:1802`。⛔ 别建成员 |
| 568 | `GetHeroFullPosition(CardScript)` | **D** | 原版零调用（`SetupFullHero` 直接调 `GetHeroBoardPos`）；我们的棋盘几何在 `BoardSpec/BoardSlots` |
| 569 | `GetNextToManaTransform()` | **D** | 原版零调用（同族 5 条都有调用点，只有 `+0x60` 这一条没有） |
| 570 | `GetNextToPlasmaManaTransform()` | **D**（时间限定） | plasma 池**从无入账** |
| 571 | `GetNextToPsychicManaTransform()` | **D**（时间限定） | psychic 池从无入账 |
| 572 | `GetPlasmaManaTransform()` | **D**（时间限定） | 同上 |
| 573 | `GetPsychicManaTransform()` | **D**（时间限定） | 同上 |
| 575+ | `GetSpiritStoneManaTransform()` | **C · P1** | HUD 有件（`BattleDriver.cs:2005 / :2685`），但 FX 锚点没挂（`AnimAnchors.cs:127` 声明了、没人赋值 ⇒ `:356` 落「取不到」） |

### 2·2 `RuleCore.cs` 那半（18 条 · 留给下一路，结论已定）

| 账行 | 方法 | 建议档 | 我方现状 / 该做什么 |
|---|---|---|---|
| 557 | `ActivateHero(bool)` | **B** | 判据 = `PlayerManager__ActivateHero.c:1-12` → `CardScript__ActivateMinion.c:1-101`（**重算「召唤病/可行动」**写 `+0x230/+0x23c`，不是纯表现！）。我方 = `UnitState.Exhausted` + `RuleCore.HasDeployExemption`。要逐条核「**一个动作结束后重算英雄**」这件事（原版唯一调用点 = `BattleManager__FinishResolvingAction`） |
| 558 | `AddFaithMana(int)` | **B** | 我方 3 处内联 `ps.Faith += n`（`EffectResolver.cs:12676` / `:12846` · `RuleCore.cs:1631`）⇒ 建议在 `PlayerState` 加 `AddFaithMana(int)` 并把 3 处**转发**过去（⛔ 别只加不转 = 死码） |
| 559 | `AddNewMana(bool filled, bool untilEndOfTurn)` | **B** | ILSpy 实证实参 = `AddNewMana(manaBuff.refillMana, manaBuff.untilEndOfTurn)`。`totalMana++` 无条件；`filledMana && unused<total ⇒ unused++`。我方 = `EffectResolver.cs:12598（DoEnergy）`（createMana ⇒ `MaxEnergy += n` + `AddManaBonus`）+ `:12625（DoRefill）`（refillMana）。⚠️ `untilEndOfTurn` 支 = **现读 0 处** ⇒ 该子支 **D（时间限定）** |
| 560 | `AddQuestPoints(int, Action)` | **A** | 落点 = `EffectResolver.cs:13006（QuestPointThreshold）`；差异 = 原版延迟 + 协程回调，我们同步 |
| 561 | `AddSecretPlayed()` | **A** | 落点 = `EffectResolver.cs:2517`，所在方法 = `PlayTacticCore`（声明在 `:2462`）⇒ 落点写 `PlayTacticCore` |
| 562 | `AddSpiritStoneMana(int)` | **B** | 我方 5 处写（开局 `RuleCore.cs:294` · 路标石 `:12250` · 卡面 `EffectResolver.cs:12677` · 花费 `:928` · 花光 `:13138`）⇒ 建议同 558 收口 |
| 563 | `DestroyManaSlot()` | **D**（时间限定） | 唯一入口 = `ProcessManaBuff` 尾循环，且**只在 `manaAmount < 0` 时进入**；全池 `manaAmount` 分布 `{1:121, 2:5, 3:5, 5:4, 100:3}` ⇒ **0 入口**（详见 §三·2） |
| 581 | `RefillCustomMana(int, bool)` | **B** | 落点 = `RuleCore.cs:1837-1840`（`p.Energy += addedManaSecond`，**无封顶**，与原版同形）；值今天 = 0 ⇒ 休眠 |
| 582 | `RefillMana()` | **D** | 三源 0 调用点；同式已在 `RuleCore.cs:1765（p.Energy = p.MaxEnergy）` |
| 583 | `RefillOneMana()` | **B** | 落点 = `EffectResolver.cs:12625（DoRefill）`；差异：原版「`unused != total` 才 ++」vs 我们 `Min` 封顶 —— 只在 `unused > total` 这个**不可达**状态下不同 |
| 584 | `ResetMana(int×4)` | **B** | 落点 = `RuleCore.cs:287-296`（开局灵魂石=第 4 实参）+ `:1742`（MaxEnergy 公式=第 1 实参）；第 2/3 实参（plasma/psychic）**不建模**（0 数据） |
| 585 | `SetAccumulationMana(bool)` | **B** | 唯一调用点 = `BattleManager__SetupBoardPhase` ⇒ 「开局按模式设一次」；我方 = `GameplayVariables.manaAccumulation`（模式常量） |
| 586 | `SetQuestPointsWithDelay(float, Action)` | **B** | 纯时序（延迟后再刷 HUD + 回调）；我们同步 ⇒ 差异记「同步引擎无中间态」 |
| 587 | `SetSavedMana(int, bool)` | **A/B** | 两处写 `p.ManaCarry`（`RuleCore.cs:2754` / `:1764`）⇒ 建议先收口成 `PlayerState.SetSavedMana(int)` 再判 A |
| 588 | `SetupFullHero(CardScript)` | **B** | 引擎半边 = `PlayerState.Warlord` 绑定（`RuleCore.NewBattle`）；表现半边 = reparent / `GetHeroBoardPos` / `SetAsHero`（表现层） |
| 589 | `ToggleCurrentMana(bool)` | **C · P2** | 唯一调用点 = `CardScript__SetAmbush` ⇒ 埋伏时把「当前能量」那组 HUD 淡出/灰化；我们 HUD 未见同款（表现层） |
| 591 | `UseMana(int, ManaType, bool)` | **B** | normal 支 = `RuleCore.cs:12925（SpendNormalMana）`；`ManaType.SpiritStone(5)` 支 = `EffectResolver.cs:928` / `:13130-13138`。差异 = 我们没有 `ManaType` 分派这个形状（出牌恒 normal、灵魂石花费走另一条链） |
| 593 | `UseSpiritStoneEnergy(int)` | **D** | **0 调用点**（`UseMana` 把它那段内联了）；语义已由 `UseMana` 的 5 支承载。⚠️ 差异：原版「不够 ⇒ 清零」，我们 `:928` 是「不够 ⇒ 不扣、不结算」（闸在 `CanUseSpiritStone` 先判余额）⇒ 不可观察 |

---

## 三 判据新发现（会影响别人的活，⛔ 只报不改）

### 3·1 `AnimAnchors.cs:125-127` 那句话**至少有一半是错的**
它写「我们 HUD 里**没有** Psychic / SpiritStone / Plasma 这三种能量的件」。
实际：**SpiritStone 是有的** —— `BattleDriver.cs:2005（_myStoneText/_foeStoneText）` + `:2685` 的 `SpiritStoneHolder` 坐标 + `ShowsSpiritStone`（按 SaimHann 显示）。
⇒ 位置档 **61/71**（`friendlySpiritStoneMana` / `enemySpiritStoneMana`）**可以接**，只有 Psychic/Plasma 是真的没有。
判据：`PlayerManager__GetSpiritStoneManaTransform.c:1-15`（返回灵魂石计数器的 transform）+ `AnimFxModuleMoveParticlesToTarget__Initialize.c:63`（拿它当粒子飞行目标）。
➡️ 归**表现层批次**（`CardPresentation/**` 不在本代理白名单）。

### 3·2 `DestroyManaSlot` 在本 build **0 入口**（⛔ 别实现）
`ProcessManaBuff` 的尾循环（cpp2il `:113-135` / ILSpy `:34451-34474`）两源同形：
`while (i > manaBuff.manaAmount)`，`i` 初值 0 ⇒ **只有 `manaAmount < 0` 才进**。进循环后 `createMana ⇒ DestroyManaSlot()` / `refillMana ⇒ UseMana(1)`。
现读全池 `manaBuff.manaAmount` 分布 = `{1:121, 2:5, 3:5, 5:4, 100:3}` ⇒ **负值 0 张**。
⇒ 判 **D + 时间限定**（数据若出现负值需重跑），⛔ 别为它写「摧毁法力槽」的机制。

### 3·3 `HasEnoughMana` 的另外三支在本 build **都观察不到**
- `spiritStone` 实参：**四个调用点全传 false**（`BattleManager__CanPlayCard.c:87` 第 5 参被 `and 0xff…00` 清零；`CanUseActiveAbility.c:68` / `ReadyToUseActiveAbility.c:49` / `CanUseAbilityNow.c:52,59,104` 直接传 0）。
- `psychic` / `plasma`：两池**从无入账** —— `manaBuff.psychicMana` / `plasmaMana` 现读 **0 处**（138 个 manaBuff 实例），`CardDef.cs:455-458` 也记「两池是死功能」。
⇒ 活路径只有 normal 支 = `required <= unusedMana`，与我们两处 `CostOf(...) > ps.Energy` 逐字等价。

### 3·4 `manaBuff` 现读分布（给所有读卡数据的人一个准数）
`cards_truth.json`（无 `full_raw` 重复，单份统计）：**manaBuff 138** 个实例 · `createMana` **52** · `refillMana` **43** · `faithMana` **19** · `spiritStoneMana` **5** · `questPoints` **43** · `psychicMana` **0** · `plasmaMana` **0** · `untilEndOfTurn` **0** · `manaAmount` 分布 `{1:121, 2:5, 3:5, 5:4, 100:3}`（**无负值**）。
⚠️ `EffectResolver.cs:12592` 的注释写「createMana 49 处 / refillMana 40 处 / 两者都有 3 处」—— **现读是 52 / 43**（不是矛盾，是那天之后的卡表变化；引用时写「现读」）。

### 3·5 `GetSavedMana` / `ManaCarry` 的**判据源**该换一条
`RuleCore.cs:2748-2755` 那段注释把「存未用能量」的依据指向**非官方规则书 `:60`**。
真判据 = `decomp_full/BattleManager__UpdateManaLeftAccumulation.c:51-57`：`savedMana = min(gv.manaAccumulation(+0x40), GetUnusedMana)`，且 `+0x40 == 0` 时**整条早退不写**。
⇒ 我们的式子 `ManaCarry = (manaAccumulation > 0 && Energy > 0) ? manaAccumulation : 0` 与之**等价**（整数域下 `min(1, Energy>0)` ≡ 1）—— 但**注释的判据该改指**（留给 RuleCore 那批，铁律 5）。

### 3·6 `AddNewMana` 的第三个实参有 ILSpy 直接实证
`out_cpp2il_dev_ilspy/BattleManager.cs:34432` 明写 `playerManager.AddNewMana(manaBuff.refillMana, manaBuff.untilEndOfTurn)` —— 即 `filledMana = ManaBuff.refillMana`、`untilEndOfTurn = ManaBuff.untilEndOfTurn`。
（cpp2il 的 `.c` 把后两个实参丢了，只剩 `AddNewMana(lVar3)` ⇒ **别只看 `.c` 就以为它只有一个参数**。）

### 3·7 `ActivateHero` **不是纯表现**（容易误判）
`CardScript__ActivateMinion.c:1-101` 写的是 `+0x230/+0x23c` 那两组「召唤病/可行动」布尔位（含 `HasCurrentTrait(0xdc/100/0x4f1/0x4fb)` 四道判据），不是动画。
⇒ 归 `UnitState.Exhausted` / `RuleCore.HasDeployExemption` 那一族核（见 §五 557）。

---

## 四 读数（本批实测）

| 检查 | 命令 | 结果 |
|---|---|---|
| **秒级类型检查（全部改完：`PlayerState` 7 个取值口 + `RuleCore` 的 `pendingHealing` 那一笔）** | `TMPDIR=/tmp/wf_W12MN4 bash 工具/typecheck.sh` | ✅ **运行时 0 / 编辑器 0** |
| **账落点机检** | `python -I -X utf8 工具/check_ledger_points.py 账/_parts/B9_W12MN.tsv --all-A --warn-getters` | ✅ **A 7 · 通过 7 · 漂移 0 · 告警 0 · 失败 0** |
| 注解齐备 | `python -I -X utf8 工具/verify_annot.py …` | ✅ 缺注解 **0** |
| **`ruleprobe check`（解析指纹 · 改完后）** | `bash 工具/ruleprobe.sh check` | ⚠️ rc=1：**输出指纹 现算 `c9a64798…` ≠ 钉住 `18439d96…`**；「解析差异 1 行 = **BL47｜Disgustingly Resilient**」。⛔ 未改 `PinnedParseMd5`。**归因**：BL47 = `E10` 在飞的改动；**我这两笔都不进解析链**（改前改后指纹一字不差） |
| **`play check`（结算指纹 · 改完后）** | `bash 工具/ruleprobe.sh play check` | ⚠️ rc=1：基线 `af8e8178…` vs 现算 `1b40f42a…`；逐卡差异 = **BL47**（①-a）· **BL18 · SW25 · UM18**（①-b）。**归因**：`E10` 的 BL47 · `P-06` 的 BL18/UM18 · `P-05` 的 SW25 ⇒ **本批 0 归因** |
| **`play check`（A/B 隔离 · `pendingHealing` 那一笔）** | 改前 / 改后各一次，**逐字节比** | ✅ **两次完全一样**（同指纹 `1b40f42a…`、同 4 张差异卡、无新增）⇒ ① 我的修正**在 `play` 夹具里零观察差异**（夹具不含「先溢疗后挨打」序列）；② 那 4 张差异**百分百**是别人的活 |
| （中途一次）`ruleprobe check` | 同上 | 🔴 曾**构建失败**：`RuleEngine/Core/Enchantment.cs(193/211)` `CS0103 UnknownWhileInPlayAnim` —— 那个文件 mtime `23:17:31`、**是别人正在改的**（按 §13·3 不算我的）；约 10 分钟后他们改完，构建恢复正常（现读数见上） |

**A/B 隔离说明**：`brief` 要求的隔离是「把你的改动换回 `git show HEAD:` 再跑」。
- 对 **`PlayerState` 那 7 个取值口**：**不需要** —— 它们是「`return <字段>;`」、**无调用点**、不进解析/结算链。
- 对 **`RuleCore` 的 `pendingHealing` 那一笔**：**用「改前 / 改后两次同一命令逐字节比」代替**（见上表最后两行）—— 结果完全相同，所以「那 4 张差异卡不是我造成的」是**实测结论**，不是推理。
- ⛔ 全程没改 `PinnedPlayMd5` / `PinnedParseMd5`。

---

## 五 交给下一路（`RuleCore.cs` 那 18 条）

### 5·1 动手顺序建议（判据都已定，不必重查）
1. **先收口 3 个「写入侧」**（都在 `PlayerState` 加一个方法、把现有内联点改成转发）：
   `AddFaithMana(int)`（3 处：`EffectResolver.cs:12676` / `:12846` · `RuleCore.cs:1631`）·
   `AddSpiritStoneMana(int)`（5 处：`RuleCore.cs:294` / `:12250` · `EffectResolver.cs:12677` / `:928` / `:13138`）·
   `SetSavedMana(int)`（2 处：`RuleCore.cs:2754` / `:1764`）。
   ⇒ 收口后这 3 条从 B 升 A（落点 = 新方法 + 调用方逐处列名）。
2. **把 2 条已经是 A 的补账**：`AddQuestPoints`（`EffectResolver.cs:13006（QuestPointThreshold）`）· `AddSecretPlayed`（`EffectResolver.cs:2462（PlayTacticCore）`）。
3. **3 条判 D（⛔ 别做）**：`DestroyManaSlot`（0 入口，§三·2）· `RefillMana`（0 调用）· `UseSpiritStoneEnergy`（0 调用，语义已由 `UseMana` 承载）。
4. **`ActivateHero` 单独一条**（§三·7）：先核 `UnitState.Exhausted` 有没有「动作后重算」这条路，再决定 A/B。

### 5·2 该配的断言（`RuleEngineTest` —— ⛔ 本代理不跑，按 brief 交调度台）
> 这 18 条**全部是状态改/结算**，每条至少要一条「可观察量」断言；下面是我按判据推的**建议断言**（写的时候照 §四 口径：**先写前提断言、再写被测断言**，⛔ 别用 `if (x != null) Check(...)`）：

| 方法 | 建议断言 |
|---|---|
| `AddFaithMana(2)` | 信仰 `+2`；**再打一次 +3 ⇒ 无上限**（99+3=102 那种白盒已经有一条，可复用） |
| `AddSpiritStoneMana(3)` | 灵魂石 `+3`；**没有初始值/上限** |
| `AddNewMana(filled:0)` | `MaxEnergy +1`、`Energy` **不变**（= DA61 `Plasma Generator` 那一档） |
| `AddNewMana(filled:1)` | `MaxEnergy +1` **且** `Energy +1`；`untilEndOfTurn:1` 在本 build 0 入口 ⇒ **断「无人调用」即可**（或写 `D` 证据断言） |
| `AddQuestPoints(3)`（阈值 3） | 余数回 0 + `QuestMilestone +1` + 往牌库加 1 张隐秘（现有 `QuestPointThreshold` 断言应已覆盖，补一条**四选一阈值 3/4** 的 `alternativeTraitLogic` 支） |
| `AddSecretPlayed` | 打出 `subtype == Secret` 的卡 ⇒ `SecretsPlayed +1`；**生成**一张隐秘 ⇒ **不 +1** |
| `HasEnoughMana`（若收口） | 4 组组合：`Energy` 够/不够 · `SpiritStones` 够/不够；psychic/plasma 两支**断「不参与」**（传 true 也不改结果，因为 0 入账） |
| `RefillOneMana` | `Energy < MaxEnergy` ⇒ +1；`Energy == MaxEnergy` ⇒ 不变 |
| `RefillCustomMana(2)` | `Energy +2`（**不封顶**、`MaxEnergy` 不变） |
| `ResetMana(1,0,0,0)` | 四池归位：`MaxEnergy == Energy == 1`、`SpiritStones == 0` |
| `UseMana(3, Normal)` | `Energy -3`；扣到 0 ⇒ `SpentAllMana` 双方督军各一次（现有 `SpendNormalMana` 断言已覆盖） |
| `UseMana(3, SpiritStone)` | `SpiritStones -3`；**不触发** `SpentAllMana`；不够 ⇒ 清零 + 出声 |
| `SetSavedMana(2)` | 写 `ManaCarry == 2`；下回合 `BeginTurn` 兑现后 `ManaCarry == 0` |
| `SetupFullHero` | `PlayerState.Warlord` 指向该卡、`Board[WarlordSlot]` 是同一个对象 |

### 5·3 文件所有权提醒
`RuleCore.cs` / `EffectResolver.cs` / `UnitState.cs` / `RuleEngineTest.cs` **都不在本代理白名单**，且 23:17 时 `Enchantment.cs` 仍在别人手上 ⇒ 这 18 条**必须派给一个独占这些文件的写手**，⛔ 别并行改。

### 5·4 **`pendingHealing` 那一笔必须配的断言**（`RuleEngineTest`，⛔ 本代理不跑）
`play` 夹具覆盖不到它（§四 实测：改前改后逐字节相同）⇒ **只能靠自检**。建议四条，**前两条是主判据、后两条是「照抄原版不清」的反向保护**：

1. **溢疗后不留账**（这是本次缺陷的原形）：单位满血 ⇒ `HealUnit(…, 3)`（实际 0）⇒
   `Check(EnoughPendingHealingToFull(ctx, u), false, "实际治疗量 < 1 也要清表")`；
2. **治疗成功后不留账**：单位被打掉 2 ⇒ `HealUnit(…, 3)`（实际 2）⇒ 同上 `EnoughPendingHealingToFull == false`；
   ⭐ **灭自证**再加一条直接读表：`SumByInstance` **与** `CountByInstance` **都**不含该 `InstanceId`
   （⛔ 别只断 `SumByInstance` —— 两个字典要一起清）；
3. **`amount ≤ 0` 不清**（照抄原版 `_Heal…:105` 的 `return 0`）：`HealUnit(…, 0)` ⇒ `EnoughPendingHealingToFull == true`
   （即那条记录**还在**）—— 这条是**反向断言**，防止后来人「顺手修好」；
4. **`corrupt` 不清**（照抄 `:107`）：给目标挂 `corrupt` 再 `HealUnit(…, 3)` ⇒ 记录**还在**。

> ⚠️ 写断言时按项目口径：**先写前提断言（如「目标确实满血 / 确实带 corrupt」），再写被测断言**，⛔ 别用 `if (x != null) Check(...)`（前提不成立会静默跳过、日志里只有 ✓）。

---

## 六 白名单与"我动过什么"（可对账）

**白名单（施工单「目标文件」列）**：`RuleEngine/Core/PlayerState.cs` + `RuleEngine/Core/RuleCore.cs`。
本批两个文件都用了；`RuleCore.cs` 只做了 **coordinator 中途插单的那一笔**（`pendingHealing` 清空点），**mana 族那 18 条一行没动**（`RuleCore.cs` 是全项目最热的共用文件、当时在别人手上 ⇒ 按 coordinator 口径留给下一路）。

| 动过的文件 | 性质 | 行数变化 |
|---|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/PlayerState.cs` | **类尾追加**（7 个转发取值口 + 一段判据注释）；**旧行一字未改**、行尾仍是 **LF**（核过 `CRLF 0 / LF 438 / 裸 CR 0`） | 348 → **438**（+90） |
| `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` | **只有 `pendingHealing` 那一笔**：`HealUnit` 两条「已结算」出口各补一句 + 类尾新增 `ClearPendingHealingEntry` + 4 处注释/日志就地订正（铁律 5）。行尾仍纯 **LF**（`CRLF 0 / LF 14921 / 裸 CR 0`） | 14873 → **14921**（**+48**） |
| `Unity/资料/完全复刻/账/_parts/B9_W12MN.tsv` | 新建（11 列表头 + 16 行） | 17 行 |
| `Unity/资料/完全复刻/报告/B9_W12MN_基础设施P0_mana族.md` | 本文件 | — |
| `Unity/_tmp_view/w12mn/gen_patch.py` · `pm16.txt` · `check2.txt` · `check3.txt` · `play2.txt` | 临时产物（可删） | — |

**没动**：`EffectResolver.cs` / 其它 `RuleEngine/Core/*.cs`（含 `UnitState.cs`）· `RuleEngineTest.cs` · `CardPresentation/**` · `Shell/**` · 主账表 · `CLAUDE.md` / `项目任务.md` · `PinnedParseMd5` / `PinnedPlayMd5` · git。

> ⚠️ `git diff --numstat` 对 `RuleCore.cs` 会打出 **664/67** —— 那是**工作树里别人未提交改动 + 我的 +48** 合在一起（`CLAUDE.md` 已写明此时 numstat 不可用）；我这 +48 行以本报告 §1b·3 的分段为准。
