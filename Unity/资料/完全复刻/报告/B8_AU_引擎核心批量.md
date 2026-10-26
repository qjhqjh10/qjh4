# B8_AU · 引擎核心（`RuleCore.cs` / `EffectResolver.cs`）B→A 批量报告

> 执行代理 **AU**（代号 `B8_AU`）· 2026-10-26 · 白名单内**零 `.cs` 改动**（本轮是「读判据 + 定观测 + 落账」）
> 账补丁 → `资料/完全复刻/账/_parts/B8_AU.tsv`（10 行 = **8 条 A** + **2 条 C**，11 列 / LF / 无缺口词命中）

---

## ① 结论

**8 条真升 A**（每条都过了 G0–G8，观测量两侧有数值）：

| # | 方法 | 我们落点 | 观测量（卡） |
|---|---|---|---|
| 1 | `CardScript.SetCardTurnDrawn()` | `RuleCore.cs:2928-2945` → `:3026` ＋ `:1680-1740`（清零 `:1720-1722`） | `play DA32` 主档 vs V2 |
| 2 | `CardScript.ShouldTriggerTeleport()` | `RuleCore.cs:3882-4232` 传送支 `:4177-4188`（闸 `:4182`） | 同上（DA32） |
| 3 | `CardScript.TriggerTeleport()` | `RuleCore.cs:3882-4232` 传送支 `:4186` ＋ `FireTriggerAt:12190` | 同上（DA32） |
| 4 | `CardScript.TriggerUprising()` | `RuleCore.cs:13019-13057`（`FireTriggerOnSide`）＋ 调用点 `:4110` | `play GSC8` |
| 5 | `CardScript.ShouldTriggerEcastasy(...)` | `RuleCore.cs:8735-8745`（`CheckEcstasyCrossing`） | `play EC25` |
| 6 | `CardScript.TriggerRegiment()` | `RuleCore.cs:6207-7161` 远程段 `:6699-6700` | `play AM6` |
| 7 | `CardScript.SetAmbush(bool)` | `RuleCore.cs:6207-7161` 伏击支 `:6245-6268` | `play GSC16` |
| 8 | `PlayerHand.RemoveCardFromHand(CardScript)` | `RuleCore.cs:3882-4232` 摘手牌那一跳 `:3908` | `play ASH50` ＋ `play SW17` |

**2 条判 C**（有明确缺口，判据已钉死、差的是一小块实现）：

| # | 方法 | 缺口 |
|---|---|---|
| 9 | `BattleManager.PayCardCost(...)` | 数值侧全对（扣额/不足清零/花光触发都对上了）；差 `EnergySpentSignal`（`Scaffold` 信号，本仓无等价事件） |
| 10 | `CardScript.RemoveAttackThisTurn()` | 全仓 `AttacksThisTurn` **只有 `++`、没有 `--`**（`RuleCore.cs:6296/11272/11699`）⇒ 原版「强制攻击退还一次额度」这一档我们做不到 |

---

## ② 逐条证据（G8 格式：块号 · 方法 · 判据出处 · 落点 · 观测量两侧数值 · 差异 · 还剩什么没核）

### 1 · `SetCardTurnDrawn` · `decomp_full/CardScript__SetCardTurnDrawn.c:17-30`（🥇 35 行）
- 落点：`RuleCore.cs:2928-2945`（`Draw`）→ `:3026`（`inst.DrawnThisTurn = true`）；清零 `RuleCore.cs:1680-1740`（`BeginTurnCore`）的 `:1720-1722` 三循环。
- 观测量（**同卡两态**，`bash 工具/ruleprobe.sh play DA32`）：
  - 主档（夹具直接注入手牌、没走抽牌）：`P0 棋盘 [.][.][DA32 6/0/1/7 {armour1, teleport1}]` —— **无** `fast1`、攻击仍是印刷 6。
  - V2（把这张放牌库顶 → `RuleCore.Draw` → 再打出）：电池净差首行 `✚ 落场/生成 P0[2] . → DA32 7/0/1/7 {armour1, fast1, teleport1}` —— 攻击 6→7、`fast1` 到手。
  - 卡面 `Armour 1. Teleport: Gain +1 [Attack] and Fast.` ⇒ 期望 = 7 + `fast1`，与 V2 实测逐值相同；主档零变化反向证明这个标记是**逐份 + 逐回合**的。
- 差异：原版存**回合号 int**（`+0x310`）并与 `manager.turnCounter(+0x3f8)` 比；我们存**布尔**并在 `BeginTurnCore` 清当前行动方手/库/墓三处 ⇒ 同回合内等价，「对方回合抽到、我方回合再用」两边都判否、依据是两套。
- 没核：清零**只扫行动方名下三处**，`Removed` / `PendingDeath` 里那一份不清（今天那里无读点，休眠风险）。

### 2 · `ShouldTriggerTeleport` · `decomp_full/CardScript__ShouldTriggerTeleport.c:16-38`（🥇 46 行）
- 落点：`RuleCore.cs:3882-4232`（PlayCardCore）传送支 `:4177-4188`，闸在 `:4182`。
- 观测量：同 1（DA32 主档 / V2）。
- 差异：**本行原记的「我们读 `unit.Has`（当前）」已不成立** —— 2026-10-26 批次 4（`F1`）已改成 `unit.Card.Has`（**印刷**），与 `HasDefaultTrait(0x4ce)` 同口径（铁律 5 就地订正，账里已改）。剩下的差异只是「回合号比 vs 布尔+清零」。
- 没核：无（4 句全读）。

### 3 · `TriggerTeleport` · `decomp_full/CardScript__TriggerTeleport.c:11-18`（🥇 23 行）
- 落点：`RuleCore.cs:4186`；共用口 `RuleCore.cs:12190`（`FireTriggerAt` 声明）。
- 观测量：同 1（V2 里那 `DA32 6→7` + `fast1` 就是这一跳抛出的正文在跑；对照局空正文净差 0）。
- 差异：原版 `OnTrigger` 的第 4/5/6 实参逐字都是 `this`（acting=subject=target）；我们只传单位与格位、acting/target 由动作上下文给 ⇒ 对自指动词等价（本族 6 张全是自指）。
- 没核：无。

### 4 · `TriggerUprising` · `decomp_full/CardScript__TriggerUprising.c:11-18`（🥇 23 行）＋驱动 `CardScript__ResolveUnitSummoned.c:82-105`
- 落点：`RuleCore.cs:13019-13057`（`FireTriggerOnSide`，单卡那一跳 `:13052`）＋调用点 `:4110`。
- 观测量（`play GSC8`，`Neophyte Trooper`，卡面 `Uprising: Gain Flank and +1 [Ranged]`，印刷 `1/2/0/3`）：电池净差 11 条，首行
  `✎ 改动 P0[2] GSC8 2/2/0/1 {uprising1} → GSC8 2/3/0/1 {flank1, uprising1}` ⇒ ranged 2→3、`flank1` 到手，与卡面逐值相同；对照局（同卡空正文）净差 **0**。
- 差异：① 遍历源 = 原版 `allCardsInPlay`（进场序），我们 `StableSortByPlayOrder` 后按 `PlayOrder` 升序（2026-10-21 `T03·c` 已对齐）；② `requireDeployedThisTurn` 读 `DeployedTurn == ctx.Turn`，原版读 `playedIntoFieldTurnCounter == turnCounter` ⇒ 同义。
- 没核：无。

### 5 · `ShouldTriggerEcastasy` · `decomp_full/CardScript__ShouldTriggerEcastasy.c:23-53`（🥇 58 行）
- 落点：`RuleCore.cs:8735-8745`（`CheckEcstasyCrossing`：闸 `:8738`、越线 `:8741`、触发 `:8743`）＋调用点 `EffectResolver.cs:14207`。
- 观测量（`play EC25`＝`Terminator`，卡面 `Armour 1. Ecstasy 2: Gain +2 [Attack]`，印刷 `5/0/1/4`）：电池净差 8 条，首行
  `✎ 改动 P0[2] EC25 5/0/1/3 {armour1, ecstasy2} → EC25 7/0/1/2 {armour1, ecstasy2}` ⇒ 血 3→2 越线、攻击 5→7（+2）；**主档（血 4 没越线）零变化** ⇒ 反向对上「只在越线那一次触发」。
- 差异：原版四条守卫（`damaged==this` ∧ `HasDefaultTrait(0x4e2)` ∧ `!EnoughPendingDamageToDie` ∧ `X<当前血` ∧ `结算后血≤X`）与我们逐条同义；`GetDefaultTraitValue(0x4e2)` 原版取两次、我们 `u.Card.EcstasyX` 取一次复用 ⇒ 数值同。**逐句对过 5 条，未发现差异**。
- 没核：原版 `EnoughPendingDamageToDie` 的函数体我没逐句读（引的是我们同名函数的等价说法 + 账里已有的判据）。

### 6 · `TriggerRegiment` · `decomp_full/CardScript__TriggerRegiment.c:11-18`（🥇 23 行）＋驱动 `CardScript__ResolveUnitAttacked.c:131-146`
- 落点：`RuleCore.cs:6207-7161`（`DeclareAttackCore`）远程触发段 `:6699-6700`；共用口 `:12190`。
- 观测量（`play AM6`＝`Ratling`，卡面 `Regiment: Deal 1 damage to a random enemy troop`）：电池净差 7 条，首行
  `✎ 改动 P1[6] FIX_E3 1/0/0/6 → FIX_E3 1/0/0/5` ⇒ 一个**敌方部队** −1 血；对照局（同卡空正文）同类攻击的伤害被抵消 ⇒ 这 1 点只出自 Regiment 能力本身。
- 差异：原版判 `attackTypes == Ranged`，我们判 `ranged`（本次打法，来源同一处 `CurrentAttackIsRanged`）⇒ 同口径。
- **没核**：我们额外要求「攻击者仍活着且仍在原格」两个条件 —— 这两个条件**没核到**原版 `OnTrigger(301)` 之前是否也有同样的闸（见 §④）。

### 7 · `SetAmbush` · `decomp_full/CardScript__SetAmbush.c:69-133`（🥇 翻开支 65 行）
- 落点：`RuleCore.cs:6207-7161` 伏击支 `:6245-6268`（`FaceDown=false` `:6248` · `RemoveAll(Ambush)` `:6260` · `EmitUnit(AmbushExit)` `:6265` · 闸 `p == ctx.Active` `:6266` · `FireTriggerAt(Ambush)` `:6267`）；另一条翻开出口（挨伤害）在 `:8578` 附近。
- 观测量（`play GSC16`＝`Acolyte Heavy`，卡面 `Ambush: Gain +2 [Attack]. Strike: …`，印刷 `5/2/0/5`）：电池净差 15 条，首行
  `✎ 改动 P0[2] GSC16 5/2/0/5 {ambush1, strike1} → GSC16 7/2/0/3 {strike1}` ⇒ `ambush1` 摘掉 + 攻击 5→7（+2），与卡面 `+2 [Attack]` 逐值相同（同一行里血 5→3 那 2 点是电池别的步造成的，**不作为本行证据**）；主档（没攻击 ⇒ 不翻开）零变化。
- 差异：六跳里 ②摘 trait 与 ⑥闸+触发 逐条对上。**另有两处属表现/HUD 侧（不在本域判，已转派）**：① `AddExitAmbushActionToCemetery`（`:105` 往墓地动作日志记一笔「退出伏击」）；② `:98-104` 的 `ToggleCurrentMana` 是 HUD 能量显隐。
- 没核：`:71` 那个 `+0x138` 委托本轮**没核清**是什么回调。

### 8 · `RemoveCardFromHand` · `decomp_full/PlayerHand__RemoveCardFromHand.c:11-19`（🥇 24 行）
- 落点：`RuleCore.cs:3882-4232`（PlayCardCore）摘手牌那一跳 `:3908`（`ps.Hand.RemoveAt(handIdx);`）。
- 观测量（**两张牌各一次**）：`play ASH50` ⇒ `✋ 手牌 P0 -「ASH50」×1`；`play SW17`＝`Wulfen` ⇒ `✋ 手牌 P0 -「SW17」×1`。
- 差异：① 原版 `List.Remove(card)` **按引用找**（找不到静默返回 false），我们 `RemoveAt(handIdx)` **按下标摘**；两条路在「牌确实在手里」时等价，「不在手里」那一态两边都到不了（出牌前 `CanPlayCard` 已挡）。② 原版附带两跳 UI（缩略视图 + 重排位）归表现层 ⇒ **分层取舍，不是规则差**。
- 没核：无。

### 9 · `PayCardCost` → **C** · `decomp_full/BattleManager__PayCardCost.c:16-37`（🥇 43 行）
- 落点：`RuleCore.cs:3882-4232` 扣费段 `:3900-3905` · `RuleCore.cs:12719-12746`（`SpendNormalMana`）· `RuleCore.cs:989-996`（`CurrentCostOf`）。
- 观测量（两张不同费用，`⚙ 能量` 行）：AM42（印 cost=2）`20 → 18（-2）`；ASH50（印 cost=4）`20 → 16（-4）`。
- 差异：数值侧逐句对过 22 句、等价（扣额=CurrentCost、不足⇒余额置 0、扣到 0 ⇒ 双方督军各触发一次 `SpentAllMana`）。**缺 2 号那一跳 `EnergySpentSignal`**（`Scaffold` 信号，带 card/isPlayer/cost；`BattleEvent.cs` 里没有任何 EnergySpent 事件）。另 `UseMana` 第 4 实参 `hasAmbush` 已核只走 `UseNormalEnergy:106-119` 的显隐支、不改数值。
- 剩：只差一条事件 ⇒ 若调度台裁定该信号属表现/成就侧，可直接升 A。

### 10 · `RemoveAttackThisTurn` → **C** · `decomp_full/CardScript__RemoveAttackThisTurn.c:5-7`（🥇 10 行）
- 判据：`if (attacksThisTurn(+0x48) > 0) attacksThisTurn--`；两个调用点都在强制攻击里（`AddForceAttack.c:41-43` 与 `AddScriptedForceAttack.c:46-48`），且都带 `param_7` 闸、同时记 `wasFree = (attacksThisTurn == 0)` ⇒ **强制攻击退还一次攻击额度**。
- 落点：`UnitState.cs:645`（字段）· `RuleCore.cs:6296-6297`（全仓**唯一**自增点）· 另两处「算已行动」`:11272 / :11699`。
- 缺口：全仓 `AttacksThisTurn` **没有任何 `--`** ⇒ 「强制攻击退还额度」这一档做不到；后果是卡面写「this troop attacks it」「attack by itself」「Rally: attack」那一族在**我们这儿会吃掉单位本回合的行动、原版不会**。
- 为什么不当场改：修复要动 `DeclareAttackCore` 的配额记账段，且「我们的 `forced` 是否逐字等价原版 `param_7`」没核（`param_7` 在 `AddForceAttack` 里还同时喂了 `wasFree`）。**留给专门批次**（见 §⑥）。

---

## ③ 改了哪些文件、哪些行、新增行数

| 文件 | 改动 |
|---|---|
| `资料/完全复刻/账/_parts/B8_AU.tsv` | **新建**，10 行（1 表头 + 10 数据行，11 列，LF） |
| 任何 `.cs` | **零改动**（本轮全部是读判据 + 定观测 + 落账） |

自查（都跑过）：
- `python -I -X utf8 工具/verify_annot.py`（主表）⇒ `同名 719 · 异名已注解 2448 · 缺注解 0`。
- 同一套判定逻辑对**本补丁**复跑 ⇒ 8 条 A 行 **bad = 0**（每条落点的首个 `文件:行` 都是**方法声明行**，且都带「（等价实现 …）」注解）。
- `python -I -X utf8 工具/check_a_vs_diff.py`（主表）⇒ 强 0 / 弱 39（**全是别人的存量**，本补丁不在扫描范围）；本补丁按同一词表复扫 ⇒ 8 条 A 行**全部 CLEAN**。
- 行尾：本补丁 `CRLF 0 / LF 10`（纯 LF）· 每行 11 字段。

---

## ④ 没查清的部分（如实标，⛔ 没拿猜测填空）

1. **`ResolveUnitAttacked` 里 `OnTrigger(301 Regiment)` 之前是否也有「IsInPlay / 仍在原格」的闸** —— 我只读到 `:131-146` 那个近战/远程互斥点，没读到 `OnTrigger` 直前。我们的 `attacker.IsAlive && Board[atkSlot]==attacker` 是 2026-10-21 `W59` 补的 ⇒ 可能**比原版窄一点点**。
2. **`SetAmbush.c:71` 那个 `+0x138` 委托是什么回调** —— `(**(code**)(lVar4+0x18))(*(undefined8*)(lVar4+0x40), param_1, *(undefined8*)(lVar4+0x28))`，两个字段都没解出用途。
3. **`AddExitAmbushActionToCemetery` 的消费端** —— 只确认它往 `CemeteryManager` 记一笔；到底是纯表现（墓地/动作日志）还是参与规则，没核。
4. **`play all dump` 里 32 张 `energyDelta != −expectedPay`** —— Sororitas 16 / SaimHann 12 / Ultramarines 2 / AstraMilitarum 1 / DarkAngels 1，**没逐张核**。所以 `PayCardCost` 的 G3 我改用**单卡两次读数**（AM42 / ASH50），没拿池级统计当证据。
5. **`ShouldTriggerEcastasy` 引的 `EnoughPendingDamageToDie`** —— 我引的是我们同名函数的等价说法 + 账里已有判据，**没**把原版那个函数体读进来逐句对。
6. **`UseMana` 的 `hasAmbush` 显隐** —— 已核到它只走 `UseNormalEnergy:106-119`、不改数值，但**没**核 `AreMinionsWithAmbush()` 在 13 场里实际显不显能量（那是 HUD 域）。

---

## ⑤ 顺手发现（现场才有、只读代理看不见的）

### 5·1 🔴🔴 **`BattleManager.Resolve*` 那 82 行全是【闭包构造器】，体内零游戏逻辑**
⚠️ **先说规模对账**：简报里写「`RuleCore.cs` 现读 133 条 B / `EffectResolver.cs` 82 条 B」，而 `ledger_slice.py` 现读是 **450 / 379**（两者交集 279 ⇒ 并集 **548** 个 distinct「类+方法」）。**差值很大，请调度台核一下 133/82 是哪个口径**（我猜是只数了「落点里第一个 `文件:行` 指向该文件」的那些行），否则后续代理按 133 估工时会严重低估。

实测 `BattleManager__ResolveHeal.c` / `ResolveCardAutoSummoned.c` / `ResolveMinionDeath.c` / `ResolveDrawCard.c` / `ResolveDiscardFromHand.c` **五份全部 82 行、逐行同构**：
```
<>f__AnonymousType7<Int32Enum>___ctor(lVar13,0,0);   // 建匿名类型
*(longlong*)(lVar13 + 0x120) = param_1;              // 捕获 manager
… 两轮 64 字节拷贝把 BattleAction 结构体搬进去 …
FUN_1803f3730(...); return lVar13;                   // 返回闭包，交给协程
```
⇒ **判据出处只写 `BattleManager__Resolve*.c` 的行，引的是一段没有游戏逻辑的体**（G0 名义上有体、实质是空）。
`RuleCore` 侧的 **450** 条 B 里 `BattleManager` 占 **330 条**，其中 `Resolve*` 一族 **43 条**；`EffectResolver` 侧 379 条里 `BattleManager` 占 **295 条**。
**出路**：要么改成引状态机 `decomp_full/BattleManager._Xxx_d__NNN__MoveNext.c`（`ls | grep -E "^BattleManager\." ` 有 481 个文件），要么引**驱动它的那条业务方法**（例如 `CardScript__ResolveUnitAttacked.c` / `CardScript__Heal.c` 那一层）。
**同族**（也是闭包，⛔ 别再浪费时间往上读体）：`CardScript__PrepareAttack`(22) · `CardScript__ReceiveDamage`(26) · `CardScript__UnitDeath`(25) · `PlayerHand__AddCardNotDrawnToHand`(20) · `PlayerHand__AddCardNotDrawnToHandAtIndex`(22)。

### 5·2 🔴 `play` 夹具的三个盲区（本次实测的新数字）
1. **事件电池的覆盖面**：`play check` 的 ①-c 现读 —— 分母 **362 张**候选（卡面有正文 · 打出去 · 主档实得 0 条），电池净差 > 0 的 **281 张**、**净差 = 0 的 81 张**（仍只被「读数的存在」看着、没被「事件的结算」验过）。
2. **keyword 驱动的效果在对照局里被抵消**：对照局是「同卡、把 `desc` 清空」，**关键词照留**（`PlayProbe.cs:997` 的 `kw` 参数）。所以 `Blast` / `Stomp` / `Regiment` 这类**纯关键词**能力在 `净差` 里被两边同时抹平 ⇒ `play <带 blast 的卡>` 永远显示「净差 0」。⇒ blast/stomp 那 8 条 B 行今天**拿不到观测量**。
3. **`Companion` 在探针里恒不触发**：`PlayCompanions` 的闸①要 `RelatedCardData.Get(card.Id)` 非空，而 `RelatedCardData` 在 `RuleEngine/Core/` 里、**探针没喂数据** ⇒ `hasRelated1` 恒 false。实测 `play TAU27`（`Companion 2: Missile Drone`，`Missile Drone` 在池内）：主档手牌 `[TAU27, filler29] → [filler29]`，**没有**衍生物进手牌，电池净差 0。⇒ 8 张 Companion 卡在离线探针里读不出观测量（要么给探针灌 `related_cards.json`，要么这一族只能靠 `RuleEngineTest`）。

### 5·3 **无卡可观测的休眠族**（G3 结构性做不到，不是没查）
现读 `cards_engine.json`（1128 张）关键词计数：`requiem 0` · `sacrifice 0` · `maintenance 0` · `bloodthirst 0` · `survivor 0`（`spiritstone` / `faith` 走另一套资源字段，也是 0）。
⇒ 对应 B 行（Requiem 触发/`ShouldRemnantDestroyOnTurnEnd` 相关 · `TriggerSacrifice` · `hasMaintenanceEffects` · `ActivateBloodThirst` · `UseSurvivor`）**今天不可能有两侧数值**。
⚠️ `hasMaintenanceEffects` 的 42 行体我读了（三段：`CurrentMaintenance>0` ∨ `HasDefaultTrait(0x172)` ∨ 扫效果表找 `trait 0x172`），第三段「效果表」我们**没有**对应表示（我们只有 `KwValue(Maintenance)`）⇒ 即便将来有卡也要先补那一层。

### 5·4 **`CardScript.RemoveAttackThisTurn` = 真缺口**（见 §②·10）
`AddForceAttack.c:41-43` 与 `AddScriptedForceAttack.c:46-48` 这一对是本轮最有价值的单条发现：**强制攻击在原版退还额度、在我们这儿吃掉额度**。影响「this troop attacks it」「attack by itself」「Rally: attack」那一族 + 教程脚本攻击。

### 5·5 **`shuffle` 四行（`ShuffleDeck` / `GetRandomOrder` / `ShuffleCardList` / `ShuffleRawCardList`）是实质差异、且不可能 A**
四份都读了全文（74 / 98 / 93 / 72 行）。原版**不是 Fisher-Yates**：
- `GetRandomOrder(n, matchData, excluded)`：先标 excluded（`ShuffleCardList` 的 `checkIfEmpty` 会把 `<rawcard+0x228 != 0>` 的卡**钉到最前**、保持原相对序），然后每步 `GetRandomInt(0, remaining)` → **线性扫第 r 个未用槽**（`while (iVar10 != iVar5)`），**选位法**而不是交换法；
- 随机源 = `MatchData.GetRandomInt`（= 开局预烧 100 个整数按下标取），`matchData == null` 时才退 `UnityEngine.Random.Range`；
- `ShuffleCardList` **返回新 List**（不改入参）。
我们 = 降序 Fisher-Yates（`RuleCore.cs:1184-1191`）、`ctx.Rng.Next(i+1)`、原地洗、**没有**「钉住前 N 张」这一档。
⇒ 分布同为均匀，但**同一随机序列得到的排列不同**；且「`checkIfEmpty` 钉前 N 张」我们**没有**。**建议**：这四行落成「已知差异 + 保持 B」，并把「钉前 N 张」单列一条 C（要判它有没有调用者拿 `checkIfEmpty=1`）。

### 5·6 **`Blast` 门那一格（`ShouldTriggerAdjacentDamage`）改法已想好，但不可观测**
原版闸 = `get_CurrentBlast(dealer) > 0`；我们 = `attacker.Has("blast")` 然后才取 `KwValue`（`RuleCore.cs:6836`）。池内 41 张带 blast 的卡**值全 ≥1**（`ASH26` 卡面写裸 `Blast`、数据是 `Blast 2`）⇒ 今天不可观测。
建议：把闸改成 `int blast = attacker.KwValue("blast"); if (blast > 0)`（顺带消掉一次重复查表）——**行为不变**，但这条命令我没执行（改了就没观测量、也拿不到「改对了」的证据；留给 blast/stomp 那一批连观测量一起做）。

---

## ⑥ 给调度台的请求

1. **重钉指纹**（本轮**没改任何 `.cs`**，以下是「我交活那一刻」的读数）：
   - 解析层：`ruleprobe check` ⇒ `全池 1121 张 · 解析差异 1 行`（`f8260229254a09746af6d9ff7a9b0b54|Ethereal Supreme`）· `哨兵合计：失败 1 条`（⑦ 输出指纹漂移）⇒ 与我会话开始时的基线**逐字相同**（不是我造成的）。
   - 结算层：`ruleprobe play check` ⇒ 结算指纹**当次现算 = `e11aa1c263e933dbdd67718133c8e5b2`**，基线文件 = `a93ffb8f344517a9ac611ef06de17a3a`（不同）；**①-a 主档 / ①-b 电池逐卡差异都是 0 行**（1128 + 586 行全等）。
2. **转派 HUD / 表现域**：`SetAmbush` 的 `AddExitAmbushActionToCemetery`（墓地动作日志记一笔「退出伏击」）与 `ToggleCurrentMana`（能量显隐）。
3. **转派「强制攻击额度」**：`RemoveAttackThisTurn`（本报告 §②·10 / §5·4）。修复点在 `DeclareAttackCore` 的配额段，**先核 `forced` ⇔ 原版 `param_7`** 再动。
4. **夹具盲区（影响一大片 B 行）**：
   - `PlayProbe` 的对照局保留关键词 ⇒ **纯关键词能力**（blast/stomp/regiment…）永远净差 0。「对照局清正文也清关键词」或「另开一档只清关键词」能立刻解出 blast 41 张 / stomp 9 张的读数。
   - `RelatedCardData` 没灌进探针 ⇒ Companion 一族（8 张）离线读不出。
   - `play` 的 `steps` 只回返回码、不落 `attacksThisTurn` / `Exhausted` ⇒ 验不了「退还额度」这类改动（建议在 `steps` 里加一格 `at=(attacksThisTurn,exhausted)`）。
5. **`Batch` 建议**：`Resolve*` 一族 41 条 + EffectResolver 侧 295 条 `BattleManager` 行，**别按方法名逐个查** —— 先花一轮把「481 个 `BattleManager._*_d__NNN__MoveNext.c`」按 `BattleActionType` 建一张索引表（哪个状态机接哪个 `type`），之后那 300+ 行就能成批改引判据，而不是 41 次重复的「读到闭包 → 卡住」。
6. **`EnergySpentSignal`（`PayCardCost` 的 C）** 需要一条裁定：属**表现/成就**侧（那就升 A）还是属规则侧（那就进 C 等实现）。
