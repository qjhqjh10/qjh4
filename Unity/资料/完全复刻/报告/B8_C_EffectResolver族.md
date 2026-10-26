# B8_C · `EffectResolver` 一族 B→A（批次 8 · 执行代理 `B8_C`）

> 白名单：`RuleEngine/Core/EffectResolver.cs` · `WhenEvent.cs` · `EffectText.cs`（`EffectText.cs` **本批未改**）。
> 账补丁：`资料/完全复刻/账/_parts/B8_C.tsv`（11 行 = 10 A + 1 C）。
> 日期：2026-10-26 · 判据口径：`判据/战斗逻辑驱动_B2A_口径.md`。

---

## ① 结论

| 项 | 数 | 说明 |
|---|---|---|
| **升 A** | **10** | `AddDrawCard` · `GetAdjacentUnits` · `ProcessManaBuff` · `ResolveRecallToDeck` · `ResolveRecallToHand` · `ResolveStealMinion` · `ResolveStun` · `CardScript.Stun` · `TriggerLoseStealth` · `PlayerHand.AddHandEffect` |
| **新判 C** | **1** | `AbilityLogic.AdjustManaEffect` —— **查出真差异**（乘值 vs 重复 N 遍 ⇒ 事件/广播次数 N vs 1），见 §五·⑤ |
| **留 B** | 其余 | 见 §四；主因两类：**池内 0 张可达**（Jam / Transform* / Requiem / EarnedSkull / UnitHealed / TriggerOtherCard*）与 **观测量取不到**（`PreviousAbilityTargets` 的 `ta=150` 消费行 0 条 · `GetRandomDeckIndex` 调用点方法体缺失） |
| **任务② `WhenEventKind`** | 2 补 + 3 判「原版也没有」 | `kills`→`Slay`(120) · `losestealth`→`UnitLoseStealth`(647) 已补；`getsshield` / `becomesremnant` / `unitjammed` 判**原版也没有 AbilityTrigger 号**，改成信息级出声（见 §三·C） |

验证：`typecheck 0/0`（运行时 + 编辑器）· `ruleprobe check` rc=0（解析差异 0 行 · 哨兵失败 0）· `ruleprobe play check` rc=0（**指纹未变** `a93ffb8f344517a9ac611ef06de17a3a`）。

---

## ② 逐条证据（G8 格式）

| 方法 | 判据出处 | 我们落点（行号范围） | 观测量 + 两侧数值 | 差异 | 还剩什么没核 |
|---|---|---|---|---|---|
| `AbilityLogic.AddDrawCard` | `decomp_full/AbilityLogic__AddDrawCard.c:1-74`（🥇 全文） | `EffectResolver.cs:3450-3499`（`DoDraw`）· `1121-1173`（`DoDrawType` = 非 Minion 支的等价物）· `13544-13631`（fm 守卫） | 手牌/牌库张数：`play UM36` 库 29→27、手牌 +2；原版 `GetNextCardsInDeck(…,2)` ⇒ +2/−2 | 原版入队再取牌库顶 N；我们同步 `Draw`×N | 非 Minion 支本批没取读数（池内可达但需「定向翻找」卡） |
| `BattleManager.GetAdjacentUnits` | `decomp_full/BattleManager__GetAdjacentUnits.c:1-19` + `MinionManager__GetAdjacentUnits.c:1-110`（🥇 全文） | `EffectResolver.cs:1701-1771` | `play AM74` 锚点 FIX_A(3 号格) → **1 个目标**、远程 0→2；`play ASH73` 锚点刚死仍取邻居（WARLORD 28→26） | 原版两列部队 List 按下标（`i==0` ⇒ 英雄）；我们格号±1（槽 4 = 督军）⇒ 同构。我们多一道「邻居 `IsAlive`」 | 下标↔格号的映射是**推出来的**（`i==0` ⇔ 槽 3/5），没有逐场 dump 佐证 |
| `BattleManager.ProcessManaBuff` | `decomp_full/BattleManager__ProcessManaBuff.c:1-140`（🥇 全文） | `EffectResolver.cs:12505-12824` | faith：`play SOR50` 信仰 12→15（一行 +3）；quest：`play DA4` 任务点 0→1；spirit：`play ASH82` 灵魂石 5→3（付费，反证该支不广播） | 四支逐条对上（faith 一次 / quest 逐点 / spirit 不广播）；`createMana` 我们记永久增量而非逐点 `AddNewMana` | 能量支（`+0x16/+0x17`）的「加完再拆」CFG 尾巴读着像死代码，未定论 |
| `BattleManager.ResolveRecallToDeck` | `decomp_full/BattleManager._ResolveRecallToDeck_d__543__MoveNext.c`（🥇 逐段） | `EffectResolver.cs:6383-6593` | `play ASH82` P1 手牌 3→2 / 牌库 27→28；反例 `play GSC55` 墓地空 ⇒ 出声 + `unresolved` | 原版「插牌库」不在 `MoveToDeck`（纯 DOTween 动画）；我们收在 `DoReturn`（`deck` 洗 / `decktop` 不洗） | — |
| `BattleManager.ResolveRecallToHand` | `decomp_full/BattleManager._ResolveRecallToHand_d__542__MoveNext.c`（🥇 逐段） | `EffectResolver.cs:6383-6593` | `play ASH36` / `play UM110`：FIX_A 离场→手牌、−2 费（UM110 另 +flank） | 原版回手后 `ResetToValuesInHand` + `UpdateCardEffects`；我们沿用同一 `CardInstance`。原版墓地来路会 `BroadcastCardCreated`，我们只发 `CardReturnedHand` | 手牌上限那一支夹具构造不出 ⇒ 静态 |
| `BattleManager.ResolveStealMinion` | `decomp_full/BattleManager._ResolveStealMinion_d__551__MoveNext.c:28-119`（🥇） | `EffectResolver.cs:9134-9241` | `play GSC49`：P1[3] FIX_E1 → P0[2]，带 `{fast1}`，日志「把 FIX_E1 抢过来了…本回合结束归还」 | 原版翻 `isPlayer` + List 搬迁；我们挪 `Board[]` + `TempControls`。落点/召唤病同判据 | 满场 / 同阵营两支夹具不可控 ⇒ 静态 |
| `BattleManager.ResolveStun` | `decomp_full/BattleManager__ResolveStun.c:1-222`（🥇 状态机） | `EffectResolver.cs:4066-4192` | `play DA35` FIX_E1 得 `{stun1}`；`play ASH39`（`or` 随机支）同样得 `{stun1}` | 原版解包一条 action；我们同步 ⇒ 净结果同 | — |
| `CardScript.Stun` | `decomp_full/CardScript__Stun.c:1-170`（🥇 全文） | `EffectResolver.cs:4066-4192`（判定口 `RuleCore.StunBlockedByTraits`） | 同上一行（同一落点，两条账行共享实现） | 三闸（在场 / `unstunnable` / 残骸门）逐条对上；原版施加前 `RemoveActiveEffects(4,100)`，我们的等价物在 `UnitState.AddKeyword` 的 `_removedTraits` 去重 | `AddKeyword` 内部未逐句核（他人文件） |
| `CardScript.TriggerLoseStealth` | `decomp_full/CardScript__TriggerLoseStealth.c:1-16`（🥇 全文） | `WhenEvent.cs:229`（常量）· `EffectResolver.cs:10727-10770`（广播）· `WhenEvent.cs:2087-2096`（**本批新增**名字映射）· `EffectResolver.cs:7392`（兜底接线） | `play ASH79` 事件电池：`{stealth1,…} → {shuriken1,…}`（隐身被摘 + 监听者给 Shuriken 1），随后攻击 2→3；反例 `play AM43` 4 个目标 0 摘除 ⇒ 走「If no enemy is affected, draw 2」 | 原版卡自发 647（`targetCard`=0）；我们走通用事件族 + 名字映射 | — |
| `PlayerHand.AddHandEffect` | `decomp_full/PlayerHand__AddHandEffect.c:1-139`（🥇 逐段） | `EffectResolver.cs:5383-5529`（`AttachHandEffect`）· `5531-5565`（`RegisterHandEffectRecord` = 先登记再贴） | `play UM53`：「Your next card this turn costs 1 less」手牌总价 2→0、日志「登记 30 张」 | 记录挂手牌 vs `HandEffectRecords`；我们贴到**手牌 + 牌库** | 原版 `:112-139` 的长尾（是否也遍历牌库）**未读完** ⇒ 见 §五·③ |

---

## ③ 改了哪些文件、哪些行（**新增行数**）

| 文件 | 改动 | 行数变化 | 漂移影响 |
|---|---|---|---|
| `RuleEngine/Core/WhenEvent.cs` | 在 `WhenEvents` **类尾**（`:2047-2103`）追加 `ExtraTriggerNameForKind` + `KindsAbsentInOriginal` + 判据长注释（`kills`→`Slay`/120 · `losestealth`→`UnitLoseStealth`/647 · 另三种为什么原版也没有） | **+57**（2048 → 2105） | 最高账点 `WhenEvent.cs:1886-1902` **不受影响** |
| `RuleEngine/Core/EffectResolver.cs` | ① `:7392` 单行**就地替换**：`return null;` → `return WhenEvents.ExtraTriggerNameForKind(kind);`（**0 行数变化**，专为躲开下游账点）② `:15763-15822` 重写「未映射事件要出声」那段注释 + `WarnUnmappedListenerKind` 分两档（原版也没有 ⇒ `Debug.Log` 信息级；真缺口 ⇒ `LogWarning`） | **+22**（15808 → 15830） | 改动全在**最高账点 `:15715` 之后**（另加 `:7392` 的零行长替换）⇒ **不应产生任何 A 行落点漂移** |
| `RuleEngine/Core/EffectText.cs` | **未改** | 0 | — |

行尾：`EffectResolver.cs` 全 CRLF（15830/15830，裸 CR 0）· `WhenEvent.cs` 全 LF（本来就是 LF，`git diff --numstat` = 57/0 ⇒ 整篇未被翻）。

---

## ④ 没查清的部分（如实列出）

1. `CardScript__Stun.c:125` 的 `RemoveActiveEffects(buffType 4, trait 100)` → 我们 `UnitState.AddKeyword` 里的对应实现**未逐句核**（`UnitState.cs` 属他人文件）。
2. `MinionManager__GetAdjacentUnits` 的两条部队 List 下标 ↔ 我们的格号是**推出来的**（`i==0` ⇔ 槽 3/5），没有原版逐场 dump 佐证。
3. `ResolveRecallToHand` 的**手牌上限**支、`ResolveStealMinion` 的**满场 / 同阵营**支：`ruleprobe play` 的夹具造不出这两种局面 ⇒ 只有静态判据（代码 `to < 0` 支 / `fromP == owner` 支各有一条 `ctx.Log`）。
4. `BattleManager.GetRandomDeckIndex` **未升 A**：方法体读过（`decomp_full/BattleManager__GetRandomDeckIndex.c`，`GetRandomInt(0, Count+1)`，`Count==0` 时不掷），但**调用点在 `decomp_full` 里 0 处**（属「724 条方法体缺失」那一档）⇒ 无法把卡面路径与方法对上。我们 `InsertIntoDeckRandom`（`EffectResolver.cs:12825-12832`）的差异：用 `aux.InsertRng` 独立流 + **镜像下标**（`Count - origIndex`），原版吃的是开局预烧的 `randomNumbers`。**待查清：原版牌库哪一端是「顶」**。
5. 以下行**保持 B**，原因已在各自备注里：`Jam`（池内 0 张卡面写 jam）· `Transform*`/`ReplaceUnit`（全池 0 张「transform/become」）· `TriggerOtherCard{Codex,Mob,Regalia,Requiem}`（430/435/640/70 各 0 张）· `ReactToUnitHealed` / `TriggeredEarnSkull`（70 / 640 各 0 张）· `PreviousAbilityTargets` 读写两条（`ta=150` 消费行 0 条 ⇒ G3 无观测量）· `PlayAbility`（2878 行只读了入口）。

---

## ⑤ 顺手发现（**报上来，本批没改**）

1. 🔴 **`GOF97 Gargantuan Squiggoth` 的「Stun an enemy and adjacent units」把自己眩晕了。**
   `play GOF97` 日志：`（「相邻」：锚点 Gargantuan Squiggoth（0 方 2 号格）→ 1 个目标：Gargantuan Squiggoth）` + `RALLY：「Stun an enemy and adjacent units」眩晕了 1 个单位`，而**P1 棋盘一个数字都没变**、GOF97 自己的关键词里多出 `stun1`。
   ⇒ `ResolveAdjacentAnchor`（`EffectResolver.cs`，我的文件）在这类「**an enemy** and adjacent units」写法上把锚点选成了**施放者自己** ⇒ **打到自己人**，且卡面不打 `*`（静默）。
2. 🔴 **`ASH73 Whirling Death` 的「相邻」伤害打到了**自己**的督军**：变化里 `P0[4] WARLORD 2/0/0/28 → 26`，日志「If the target dies, deal 2 damage to adjacent units … 对 1 个目标造成 2 点伤害」。FIX_A 死在 0 方 3 号格 ⇒ 邻居是督军槽 4。若锚点是敌方单位，「相邻」应取敌方邻居 —— 与第 1 条**同族**（目标侧阵营没筛）。
3. **`UM53 Hive Factory` 把手牌**和牌库**全部登记**（30 张）。`PlayerHand__AddHandEffect` 我只读到 `:112-118`（遍历 `param_1 + 0x50` 的那个循环体），该文件的长尾没读完 ⇒ 「原版是否也遍历牌库」**未核**；本回合内不可观测（牌库的牌打不出）。
4. **`AM76 Armoured Lightning`**：3 个 Tempestus Scion 全部带 `{armour1, camouflage1, duty1, vanguard1}`。卡面是「**If you control a Vehicle**, give them Armour 1」= **条件**。需核 `AM68 Tempestus Scion` 自己印没印 armour1；若没印，说明条件支被无条件执行。**未核**（没查 AM68 的卡面）。
5. **`AdjustManaEffect` 真差异（已判 C）**：`play SOR49`（「for each Troop drawn, gain 1 ☀」，`fm=[3,0]`）实测**三行**「信仰 +1」，而原版 `AdjustManaEffect` 是**一次 +3** ⇒ 同一张卡**广播/信号次数 3 vs 1**。今天看不到差异只因为 `SOR38`（`trigger 665`「When you gain Faith」）不在夹具上；真打时它会响 3 次。
6. **`SW57 Birth of a Saga` 的读数拿不到**：那一刻另一个代理正在写 `RuleCore.cs`（`CS0103 AddToCemetery` ×6）⇒ `ruleprobe` 构建失败，读数作废（按工具自己的提示「先看 error 落在哪个文件」——全在别人的文件上）。

---

## ⑥ 给调度台的请求

1. **指纹**：`play check` 的结算指纹**未变**（`a93ffb8f344517a9ac611ef06de17a3a`，rc=0）· `ruleprobe check` rc=0（解析差异 0 行 · 哨兵失败 0）· `typecheck` 0/0。**无需重钉基线**。
2. **账漂移**：预期 **0 条**（§三 的「漂移影响」列已逐项说明为什么）。
3. **自检**：本批改了 `RuleEngine/Core/**` 两个文件 ⇒ 按批 8 约定**没跑宿主**；请在同步点跑 `RuleEngineTest.Run`（改了引擎行为一类的行只有 `TriggerNameOfWhenKind` 的映射，`play check` 已证结算层零变化，但 `RuleEngineTest` 里有专门钉 `WhenEvent` 的断言）。
4. **请把 `AdjustManaEffect`（C）排进修复队列**：修法方向 = 在 `TryComputeRepeats` 把「**值被乘**」与「**执行 N 遍**」分开（`amount` 类 op 乘值 ⇒ 一次加值一次广播；`deploy`/`damage`/`create` 类 op 继续重复）。⚠️ 这会改 `play check` 指纹。
5. **`WhenEventKind` 那 4 种在账里没有独立行**（全部 tsv 的 `原版类` 列里没有 `WhenEvent*`）⇒ 我把结论写进了 `TriggerLoseStealth`（1073，含 647）与 `TriggerNameOfWhenKind` 的代码注释。**若调度台需要独立一行，请新建**（内容可直接取本报告 §①/§三）。
