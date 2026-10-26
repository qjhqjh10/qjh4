# 执行代理 `B8_B` —— `RuleCore` 一族 `B → A` 升档报告

> 会话 2026-10-26 · 白名单 = `RuleEngine/Core/RuleCore.cs` · 账补丁 = `账/_parts/B8_B.tsv`（11 行：10 A + 1 B 落点更新）
> 门槛口径 = `判据/战斗逻辑驱动_B2A_口径.md`（G0–G8）+ `完全审查与完全复刻_方案.md` §4.1

---

## ① 结论

| 项 | 数 |
|---|---|
| 真升 **A** | **10 条** |
| 保持 **B**（但补了落点 / 修了实现） | 1 条（`ResolvePlayCardFromHand`） |
| 代码改动 | `RuleCore.cs` **+70 行**，全部在类尾（2 处改行不动行数）+ 0 行为变化（指纹未变） |
| typecheck | **0 / 0** |
| `ruleprobe.sh check` | rc=0 · 1121 张 · 解析差异 0 · 哨兵失败 0 |
| `ruleprobe.sh play check` | rc=0 · 结算指纹 **a93ffb8f344517a9ac611ef06de17a3a**（与基线一致，**未重钉**） |

**升 A 的 10 条**：`AddToCemetery` · `DealMultiDamage` · `HealMultipleCharacters` · `HealOneCharacter` ·
`RemoveCardFromDeck` · `ResolveDrawCard(BattleAction)` · `RemoveBastionDamage` · `SetCardStateInCemetery` ·
`Stun` · `RemoveCardFromHand`。

> 没做到 12 条的**原因写在 §四**（都是「可观察量/边界在离线夹具里造不出来」这一类，不是没查）。

---

## ② 逐条证据（G8 格式：判据出处 / 落点 / 观测量 + 两侧数值 / 差异 / 还剩什么没核）

### 1. `CardScript.RemoveBastionDamage(int)` → A
- **判据**：`decomp_full/CardScript__RemoveBastionDamage.c:17-58`（反序逐条吃 `activeEffects` 里 trait `0x352` 那几条的 `+0x60`）· `:54-58`（剩余落到基础 trait 值 `+0x1c`）· `:59-65`（基础值钳 0；返回值取**钳之前**的 `CurrentBastion`，可为负 = 溢出量）。
- **落点**：`RuleCore.cs:8204-8729（ApplyDamageCore）`，堡垒那一段 **`:8493-8540`**。
- **观测量**：同一张卡（`SW17 Wulfen`，用 `WFPROBE_POOL` 注入 Bastion）的事件电池行 `selfChip=`（= `ApplyDamage` 的实得伤害），自伤 2 点：

  | 注入 | `selfChip` | 期望（由 `.c:35-50` 推） |
  |---|---|---|
  | 无堡垒 | **2** | 2 全落血 |
  | `Bastion 1` | **1** | 吃 1、溢出 1 |
  | `Bastion 2` | **0** | 刚好打平、血不动 |
  | `Bastion 3` | **0** | 整份吃下 |

  原始输出（`WFPROBE_POOL=… bash 工具/ruleprobe.sh play SW17`）：`电池的步骤/返回码 : play=0,…,selfChip=0/1/2,…`
- **差异**：原版分「条目层 + 基础值层」两级扣减，我们只有一份 `KwValue(Bastion)` ⇒ 把两层折成一个数；净结果（先吃堡垒→溢出进血→堡垒钳 0）同形。逐句对过 30 句，未发现净差异。
- **还剩**：无。

### 2. `BattleManager.DealMultiDamage(...)` → A
- **判据**：`cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:40746-40801`（🥈 唯一有真体的那份）—— 建 `BattleAction(doMultiDamage)` 入队（目标表拷一份），随后**逐目标** `RecordPendingDamage(damageInfo)`。
- **落点**：`RuleCore.cs:8193-8203（ApplyDamage）`（逐目标各调一次 → `ApplyDamageCore` 8204-8729）。
- **观测量**（play「Smothering Decree」Deal 3 damage to all enemies）：`FIX_E1 6→3 · WARLORD 30→27 · FIX_E2 6→3 · FIX_E3 6→3`，`伤害累计 P0 0→12`；第二次读数（play「Cleansing Flames」，2 目标）：`P1[3] 6→3`＋`WARLORD 30→27`、累计 `0→6`。
- **差异**：原版入队 + 先记 pendingDamage、异步逐目标结算 ⇒ 「队列未结算时 `EnoughPendingDamageToDie` 为真」这个中间态我们表示不出来；净结果同。
- **还剩**：空目标 / 0 伤害两条边界夹具造不出（这两张卡正文必带目标、伤害恒 3）。

### 3/4. `BattleManager.HealMultipleCharacters(...)` / `HealOneCharacter(...)` → A
- **判据**：`decomp_full/BattleManager__HealMultipleCharacters.c:51-70,129-145` · `decomp_full/BattleManager__HealOneCharacter.c:48-59,117-124`；实际治疗式 = `decomp_full/CardScript._Heal_d__468__MoveNext.c:94`（`0 < healValue`）· `:106-108`（`corrupt` 闸）· `:109-112`（实际量 = `min(请求, MaxHealth − Health)`）· `:136`（实际量 < 1 ⇒ 连字都不显示）。
- **落点**：`RuleCore.cs:8989-9021（HealUnit）`（逐目标各调一次；外周 `EffectResolver.DoHeal`）。
- **观测量**：
  - play `AM48`（Heal 2 to all your units）⇒ `FIX_A 2→4 · FIX_B 3→5 · FIX_C 1→4 · WARLORD 28→30`，日志「治疗 4 个目标各 2 点（实际合计 8）」。
  - play `ASH50`（Heal 5 and give Camouflage）⇒ `FIX_A 2/0/0/2 → 2/0/0/4`，日志「治疗 1 个目标各 5 点（实际合计 **2**）」= 溢疗封顶。
  - 边界（注入 `Heal 0`）⇒ `Tried to heal FIX_A with value 0（≤ 0）—— 不治`，生命 `2→2` 不动。
- **差异**：原版入队 + 逐目标 `RecordPendingHealing`、异步起 `Heal` 协程；我们同步逐目标治疗。「同一个量、逐目标各自封顶」两侧同形。
- **还剩**：`corrupt` 闸（`.c:107`）池内 `corrupt` 现读 **0 张** ⇒ 逐字节跳过。

### 5. `BattleManager.RemoveCardFromDeck(bool, CardScript)` → A
- **判据**：`decomp_full/BattleManager__RemoveCardFromDeck.c:11-19`（按 `isPlayerDeck` 选 `+0x310/+0x318`，`List.Remove(实例)`）。
- **落点**：`RuleCore.cs:3037-3103（MoveDeckTopToHand）`，取牌两句 **`:3094-3095`**。
- **观测量**：play `AM54`（Draw 3）⇒ `库=29 → 26` 且 `🂠 牌库 P0 -「filler26/filler27/filler28」×1` 三条；第二次读数 = 各卡日志里每次「抽 1 张」后库数同步 −1（回合抽牌与效果抽牌共用同一函数）。
- **差异**：原版 `List.Remove(实例)`（线性找、找不到静默）vs 我们按下标删已知顶张 — 净结果同。⚠️ 已入账的另一条形状差：原版取 `List[0]` 当顶、我们取末尾（镜像约定），装载口已倒序喂修过。
- **还剩**：牌库空 ⇒ 走疲劳支（不是本方法）；「找不到那张」在我们这条路上不会发生。

### 6. `BattleManager.ResolveDrawCard(BattleAction)` → A
- **判据**：`decomp_full/BattleManager__ResolveDrawCard.c:25-30`（建协程壳）+ `decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c:55-68`（库空⇒疲劳 / 回填）· `:197-204`（`DrawCardFromDeck`）· `:201-217`（`IsDuringMulligan` + 陷阱卡支）· `:218`（`AddDrawnCardToHand`）。
- **落点**：`RuleCore.cs:2923-2944（Draw）· :3037-3103（MoveDeckTopToHand）· :13738-13763（ReshuffleCemeteryIntoDeck）`。
- **观测量**：play `AM54` ⇒ `库=29→26`（−3）· `手牌 P0 +「filler26/27/28」×1` 三条 · `弃牌 +「AM54」×1`；对照 play `AM48`（不抽牌）库停在 29。
- **差异**：原版是协程（取牌与进手之间 `WaitForSeconds`）＋「抽到陷阱卡走 `AddActiveTrapToHand`」支路；我们同步取牌+进手。🔴 **陷阱卡支池内不可达**：`isTrapCard = cardType==20 且 spellType==70`（`decomp_full/EntityScript__get_isTrapCard.c:14-17`），而 `cards_engine.json` 现读 `spellType==70` = **0 张**（分布 0/90/150/180/190/210/220/230/240/260/265/270）⇒ 逐字节跳过（数据若出现需重跑）。
- **还剩**：库空⇒疲劳支在夹具里造不出来（起手 29 张、`play` 只打一张）。

### 7. `BattleManager.AddToCemetery(CardScript)` → A（**同时消掉账里那条「5 处散写」**）
- **判据**：`decomp_full/BattleManager__AddToCemetery.c:5-6` → `decomp_full/CemeteryManager__AddCardToCemetery.c:16-37`（列表 Add）· `:38-42`（挂到墓地节点）· `:47-54`（`SetAsCemetery(下标)` + `SetActive(false)`）；`SetAsCemetery` 体内就是 `decomp_full/CardScript__SetCardStateInCemetery.c:5` 的 `cardState = 6`。
- **落点**：`RuleCore.cs:14024-14027（AddToCemetery）` —— **本轮新建的唯一写点**，6 处原散写（`:3136 / :3211 / :9492 / :9925 / :9930 / :13953`）全部改道到它。
- **观测量**：play `AM48` ⇒ `弃牌 P0 +「AM48」×1`（打前 `(空)`）；play `ASH50` 同形。**并且** `play check` 指纹 `a93ffb8f…` 不变 ⇒ 纯提取、零行为变化。
- **差异**：「卡在墓地」两边编码载体有别 —— 原版写 `cardState=6` + 关渲染，我们以「实例进 `PlayerState.Discard`」表示同一事实；列表写入那一段逐句对过 6 行，未发现差异。
- **还剩**：「手牌超上限弃牌」与「虫群下层一起进墓地」两条边界路径夹具造不出。

### 8. `CardScript.SetCardStateInCemetery()` → A
- **判据**：`decomp_full/CardScript__SetCardStateInCemetery.c:5`（方法体只有一句 `cardState(+0x228) = 6`）；调用点 `decomp_full/CemeteryManager__AddCardToCemetery.c:47-49`。
- **落点**：`RuleCore.cs:9233-9903（CleanupDeaths）`（进墓地那一句 `:9492`）· `:9904-9952（MinionDeathBookkeepingDeferred）`（`:9925`）。
- **观测量**：play `AM48` ⇒ `弃牌 P0 +「AM48」×1`；play `AM54` ⇒ `弃牌 P0 +「AM54」×1`（打前都 `(空)`）。
- **差异**：状态位写入 vs 集合成员关系 —— 不存在「写着 6 但不在列表里」的中间态可错。方法体 1 句，逐字对过。
- **还剩**：`ephemeral` 不进墓地那一档（`RuleCore.cs:9492` 的 `!u.Has("ephemeral")` 闸，池内现读 **103** 张）夹具造不出「ephemeral 单位死亡」。

### 9. `CardScript.Stun(bool, BattleAction)` → A
- **判据**：`decomp_full/CardScript__Stun.c:38-48`（日志 → 只对单位卡 → `cardState ∈ {2,15,3,17}` → 无 `0xfa` 才施加 → `AddTraitSilently(100)`）。
- **落点**：`EffectResolver.cs:4066-4192（DoStun）`（目标筛选 + `AddEffect` 四道闸 + prev 映射）· `RuleCore.cs:5071-5074（StunBlockedByTraits）`。
- **观测量**：play `ASH45`（Deal 3 damage and Stun it）⇒ `FIX_E1 2/0/0/6 → 2/0/0/3 {stun1}`；play `AM42`（Deal 2 damage and Stun it）⇒ `2/0/0/6 → 2/0/0/4 {stun1}`（两次读数、都带「眩晕了 1 个单位」日志）。
- **差异**：原版把「在不在场上」压成 `cardState ∈ 4 档`，我们无状态机 ⇒ 用「还在棋盘上且活着」＋ 残骸门等效（`A1176` 已就地订正过一档：血 ≤ 0 但会翻面成残骸的原版仍能晕）。逐段对过 11 句，未发现净差异。
- **还剩**：`unstunnable` 闸 —— 池内 `unstunnable` 现读 **0 张** ⇒ 该支逐字节跳过。

### 10. `PlayerHand.RemoveCardFromHand(CardScript)` → A
- **判据**：`decomp_full/PlayerHand__RemoveCardFromHand.c:11-19`（`List.Remove` + `ToggleSimplifiedCardView` + `MoveCardsInHandToPosition`）。
- **落点**：`RuleCore.cs:3877-3906（PlayCardCore）`，摘手牌那一句 **`:3903`**。
- **观测量**：play `ASH50`（战术卡）⇒ `✋ 手牌 P0 -「ASH50」×1`；play `SW17`（单位卡）⇒ `✋ 手牌 P0 -「SW17」×1`（出牌两条路各一次读数）。
- **差异**：原版按实例 `List.Remove`、我们按下标 `RemoveAt`（下标来自 `CanPlayCard` 校验过的同一张）⇒ 净结果同；视图复位那两句由事件驱动、属表现层。逐句对过 9 句。
- **还剩**：「牌不在手里」这条路夹具造不出（出牌前 `CanPlayCard` 已挡）。

### 11.（保持 **B**）`BattleManager.ResolvePlayCardFromHand(BattleAction)` —— 落点已更新 + 修掉一条实现缺陷
- **修的是什么**：`PlayCardCore` 的非单位分流原来是 `return PlayTactic(ctx, p, handIdx, slot);` ——**`chosen` 被整段丢掉**（`PlayCard(..., UnitState chosen)` 这个重载因此对战术卡形同虚设）。
- **改法（白名单内、零行数变化）**：那一句改成 `return PlayTactic(ctx, p, handIdx, ChosenTargetSlot(ctx, chosen, slot));`，新增的 `ChosenTargetSlot`（`RuleCore.cs:13993-14000`，追加在类尾、注释块 `.cs:13959-13992`）把「调用方点定的单位」翻成**棋盘格号**再交下去；翻不出来（单位已离场）就出声退回原格号。
- **判据**：`decomp_full/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:905-940` —— 玩家点中目标后协程把目标写进 `BattleAction +0x20`，之后 `TargetedSpellPlayed` / ability 结算**都读那一格**，⛔ 没有「再按格号回查一遍」这一步。
- **为什么不是彻底修**：`EffectResolver.PlayTactic` 没有 `chosen` 形参，本文件不在我的白名单 ⇒ 只能折成格号（`PlayTacticCore` 内部只用 `targetSlot` 做两件事：取目标 + 填事件 `Slot`，所以净结果等价）。**残留一格**：`PlayTacticCore` 仍按卡面 `PickSide` 决定去哪一侧棋盘取 ⇒ 只有当 `chosen` 本来就在卡面指定那一侧时才严格等价（把敌方单位传给「友方」卡这种调用本身不合法、`CanPlayTactic` 先拒）。
- **为什么仍记 B**：该行 1548 行协程里 `ChooseCardMethod` 交互段与网络取消支仍未逐句核完。

---

## ③ 改了哪些文件哪些行（新增行数）

| 文件 | 改动 | 行数变化 |
|---|---|---|
| `MyGame/Assets/RuleEngine/Core/RuleCore.cs` | `:3885` 一句改写（`PlayTactic(..., ChosenTargetSlot(...))`） | **0**（同行改写） |
| 同上 | 6 处 `ps.Discard.Add(x)` → `AddToCemetery(ps, x)`（`:3136 / :3211 / :9492 / :9925 / :9930 / :13953`） | **0**（逐行改写） |
| 同上 | 类尾追加 `ChosenTargetSlot`（`.cs:13958-14000`，含 40 行注释块） | **+43** |
| 同上 | 类尾追加 `AddToCemetery`（`.cs:14001-14027`，含 22 行注释块） | **+27** |
| `资料/完全复刻/账/_parts/B8_B.tsv` | 新建（11 行 = 表头 + 10 A + 1 B） | 新文件 17.4 KB |

文件总行数 `13,959 → 14,029`（+70 行，**全部在类尾** ⇒ 既有行号一个都没动；行尾仍纯 LF，裸 CR = 0）。

---

## ④ 没查清的部分（每条写清卡在哪一段）

1. **`CardScript.RemoveAttackThisTurn()`**（账 `:960`）—— 原版方法体只有 7 行（`if (0 < attacksThisTurn) --`），但**我们全仓没有 `AttacksThisTurn--` 写点**（只有 `++`：`:6291 / :11267 / :11694`，重置 `:1749`）。原版唯二的调用者是 `AddForceAttack`（`decomp_full/BattleManager__AddForceAttack.c:40-43`）与 `AddScriptedForceAttack`（`:46-47`），语义 = **「强制攻击不消耗单位那次行动」**；我们 `DeclareAttack(..., forced:true)` 仍照常 `AttacksThisTurn++`（`:6291`）⇒ **强制攻击后该单位会被判为已行动**（疑似真差异）。**卡在**：没确认原版那记强制攻击自身到底有没有把计数 +1（`_ResolveForceAttack_d__504__MoveNext.c` 里 grep `0x48` = 0 命中，计数器可能在别的层），所以不敢判 C，也没法写 A。**建议**：派一个只读代理专查 `0x48` 的写点（VA 级扫偏移），再决定 C/A。
2. **`SupportMethods.ShouldRemnantDestroyOnTurnEnd`**（`:1509`）—— 判据 25 行读完了（`faction(+0x2c)==0x28` ∧ `isRemnant(+0x65)` ∧ `!HasCurrentTrait(0x4a6=1190)`），我们的等价（`RuleCore.cs:9129-9156 DestroyRemnants`）也逐句对过，**但可观察量造不出来**：需要「一个残骸在回合末被判死」的现场，而 `play` 夹具不杀自己的单位、`SAU14 Cryptothrall`（Remnant）打出去就被 `Cell()` 只印 `u.Attack` 不印残骸状态。⇒ 停在 G3。
3. **`BattleManager.ResolveFatigue`**（`:567`）—— 原版是协程壳（`.c` 只有 21 行的 thunk），真体在 `_ResolveFatigue_d__587__MoveNext.c:55-151`：`Debug.LogWarning` → 给该侧疲劳计数 `+0x38c/+0x378` **先 ++** → 读新值 → `ShowError(疲劳值)` → `GetHero` + `DamageInfo(量=疲劳值, 类型=2)` → `DealMultiDamage`。**我们（`RuleCore.cs:3041-3072`）绕开 `ApplyDamage` 直接 `Health -= Fatigue`** ⇒ 若原版那记 fatigue 伤害会走护甲/堡垒/盾，我们就是**真差异**；**卡在**：`DamageInfo` 的 `damageType=2` 是否在 `_ReceiveDamage` 里被特判跳过减免 —— 本地没读到那段判定。⇒ 不敢升 A（也不能直接判 C）。**另**：疲劳场景本身在 `play` 夹具里造不出（起手 29 张）。
4. **`CardScript.ActivateBloodThirst()`**（`:760`）—— 判据 22 行读完（`HasCurrentTrait(0xdc)` ∧ `attacksThisTurn==1` ∧ `isPlayer==IsPlayerTurn()` ∧ `cardState != 5`）；我们 `EmitBloodThirst`（`RuleCore.cs:12118-12153`）只实现了前两条：**后两条没有显式闸**（我们是在 `DeclareAttack` 里发事件，那一刻恒在本方回合、且状态位概念不存在）。⇒ 这是**查询式高亮 vs 事件式脉冲**的机制差异，**卡在 G3**：`EvtKind.Trigger` 不进 `play` 的 dump、日志里也没有对应行，离线读不到。
5. **`CardScript.HasPackBuffTrait` / `CheckAndApplyCurrentPack`**（`:880` / `:802`）—— 两个方法体都读完了。`CheckAndApplyCurrentPack` 的算术很清楚：`cardType==10(英雄) ⇒ count = unitList.Count`，否则 `count = unitList.Count − 1`；`count ≥ 0` 且与缓存 `+0x358` 不同才 `RemovePackBuffs` + `AddEffect(buffType 610, 两格量=count)`。而**我们** `FieldAttack` 的 pack 支（`RuleCore.cs:4950-4960`）是「数本侧**非督军**单位（含自己）+N」。⇒ 两者只在「`MinionManager.GetUnitList` 的列表是否含督军」这一点上可能差 1。**卡在**：那个列表（`+0x60`）到底含不含英雄 —— `GetUnitList`/`GetMinionList`（`decomp_full/MinionManager__*.c`）都只是遍历 `+0x60`，没直接答案；**没有决定性的反证之前不改代码**（改了可能把对的改错）。**另**：即便定了，`Cell()` 只印 `u.Attack` 不印 `FieldAttack`，pack 加成在 `play` 里读不到（`softAtk=skip(would-kill)`）⇒ 需要先给探针加一个读数口。
6. **`BattleManager.ShuffleDeck` / `SupportMethods.GetRandomOrder` / `ShuffleCardList` / `ShuffleRawCardList`**（`:664 / :1472 / :1512 / :1513`）—— 4 条都落到 `RuleCore.cs:1184（Shuffle）`（同一落点 ⇒ 按 §4.1 只能 B）。原始 4 个方法体都读了（`decomp_full/` 都在），但**观测量缺失**：`play` dump 只看牌库张数、不看顺序，洗牌结果从结构上读不到。（如果将来给探针加「牌库序列」列，这 4 条可以一次升 A。）

---

## ⑤ 顺手发现（⛔ 我没改，报给调度台）

1. 🔴 **账里写「原版把卡移出游戏、我们只有 Discard」是误读**。`PlayerHand.RemoveFromGameCard`（账 `:1335`）的真实方法体（`decomp_full/PlayerHand__RemoveFromGameCard.c:16-40`）是**遍历手牌效果列表 `+0x48`、把来源 = 这张卡的手牌效果 `RemoveHandEffectAt(i)`** —— 它是「摘手牌效果」，**不是**「移出游戏」这个区域。⇒ 那一行的「判据要点/差异」要重写，而且它和 `RemoveEffectsFromCard`（`:1334`）**可能是同一件事的两份入口**，值得合并核一遍。
2. 🔴 **账里写 `708c0306` 这种 8 位 id，`play` 读不出来**：`ruleprobe play <id>` **不解析前缀**（实测 `!! 卡池里没有 708c0306`，而完整 id `708c0306e73fd494eb0f718ddaeea48a` 就是「Smothering Decree」）。⇒ 以后账里引卡一律写**完整 id 或卡名**，写前缀会让下一个人白跑一趟。
3. 🔴 **`RuleCore.DestroyRemnants` 的注释里两个数目过期**（`RuleCore.cs:9117-9119`）：写着「`Remnant.` 只出现在 Sautekh（**36 张**）、`Waystone.` 只出现在 SaimHann（**24 张**）」，现读 = **26 / 25**（`cards_engine.json` 的 `keywords` 计数）。**结论（等价性论据）仍然成立**（两个关键词各自只属于一个阵营），只是数字要订正。
4. **`RuleCore.cs` 里多处落点行号已漂移**：账里写的 `HealUnit 8939-8977` 实际 **8989-9021**、`RemoveHandEffectFromHand 10228` 实际 **10289**、`EffectResolver.DoStun 3987-4110` 实际 **4066-4192**、`Draw 2883-2905` 实际 **2923-2944**。⇒ 合并我这批时若跑 `check_ledger_points --fix`，会顺带把这些一起重定位（**建议合并后再跑，别在我这批之前跑**）。
5. **`RuleCore.MoveDeckTopToHand` 里有一行「为不动行号而写成同一行」的代码**（`:3093`：`if (ps.Deck.Count == 0) return null;    int last = ...`）。功能上没问题（那道 `Count==0` 闸是 E1 加的），但**两个语句挤一行**很容易被下一个人当死代码删掉半句 —— 建议某批专门给它让出一行的位置（那会整体 +1 行 ⇒ 要同时跑 `--fix`）。
6. **`cards_engine.json` 的 `spellType` 分布现读**：`0:1049 · 90:6 · 150:5 · 180:3 · 190:4 · 210:39 · 220:3 · 230:4 · 240:3 · 260:6 · 265:3 · 270:3`（**`70` = 0 张**）。⇒ `isTrapCard`（= cardType 20 且 spellType 70）在本 build 恒假，所有「陷阱卡」支路都不可达（已写进 `ResolveDrawCard` 的差异列）。

---

## ⑥ 给调度台的请求

1. **合并我的补丁**：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_B_`（**必须带 `--only B8_B_`**，且我这批**只有一份**补丁，无跨补丁冲突）。合并前请先跑 `check_patch_conflicts.py`（只读）。
2. **`play check` 指纹：不用重钉** —— 现算 = 基线 = `a93ffb8f344517a9ac611ef06de17a3a`（我的两处改动都是零行为/等价改动：`chosen` 那条在夹具里 `chosen` 恒 `null`；墓地收口是纯提取）。
3. **该跑的自检**：我只跑了 `typecheck.sh`（0/0）+ `ruleprobe.sh check`（rc=0）+ `play check`（rc=0）。
   我**没有**跑 Unity 批处理（红线）。若这批要进同步点，建议跑 **`RuleEngineTest.Run`**（引擎侧唯一的断言宿主）—— 我改了 `RuleCore.cs`（核心文件，跨面）。
4. **`check_ledger_points`**：我这批 6 处改道 + 2 处尾部新增**都没动既有行号**（+70 行全在类尾），预期 **0 漂移**；但**账里本来就有一批漂移**（见 §五·4），合并后如果跑 `--fix` 会一次性把它们都重钉 —— 那是好事，但要等**没有别的 `.cs` 写手在飞**的时候做。
5. **需要下一个代理接的**：§④ 的 1（`RemoveAttackThisTurn` 的 `0x48` 写点，VA 级）· 4（pack 的 `GetUnitList` 是否含督军）· 3（`DamageInfo.damageType=2` 是否跳过减免）。这三条都是**判据缺一格**、不是实现缺一大块。
