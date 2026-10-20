# W58 战斗结算 · 反噬 / Slay 逐死者交错（W58-INTERLEAVE）

> 执行代理：`W58-INTERLEAVE`（引擎线）。接 `W58_战斗结算_slay修复.md` §六·2 留下的最后一处：
> 把「全部反噬跑完 → 统一 Slay」改成原版的**逐死者交错**。
> **未跑 Unity**（⛔ 无 `*.Run` / 无截图）· **未动 git** · **未重钉任何基线/指纹**。
> 验证 = 秒级 typecheck ＋ 纯 dotnet `工具/ruleprobe`（`play check` / `play all dump`）＋ 临时 scratch 探针（系统临时目录，未写入仓库）。

---

## 〇、结论

| 项 | 状态 |
|---|---|
| **1** `PendingDeath` 补 `ActingCard`（致死伤害来源卡） | ✅ **已加**，五条入队路径按原版 deathType 填对 |
| **2** `FlushDeaths` 改**逐死者交错** | ✅ **已改**：死者 → 反噬 → 该死者记账（Slay / `Kills`）→ 下一个死者 |
| **3** 「每个死者只记一次」/ 督军 / 残骸 / 复活 / 凶手离场 | ✅ **未动**，仍由 `TryCreditKill` 同一判定口挡 |
| **4** 测试 + 灭自证 | ✅ `TestW58SlayInterleave`（①正例 ②反例），已注册 `Step(...)`；两个方向都用临时实验实证「改回去必红」 |
| **5** 既有读数 | ✅ `TestW58BlastStompSlay` 9 组语义**全部保持**（scratch 复刻逐条同值）；全池 `play` dump **逐字节不变** |
| **6** 基线与指纹 | ⛔ **没重钉**（`0aa5c844…` vs 基线 `08d51f6d…`，与上一笔 `W58-SLAYFIX` 结束时**相同**） |

---

## 一、`PendingDeath` 改法

文件：`RuleEngine/Core/BattleContext.cs`

```csharp
public struct PendingDeath
{
    public int Player;
    public UnitState Unit;
    public int Killer;
    public int SlotAtDeath;       // 只作诊断留痕（SLAYFIX 已不再排序）
    public UnitState ActingCard;  // 🆕 致死伤害的来源卡；null = 这一条死亡不记击杀
}
public void AddPendingDeath(int p, int slot, int killer, UnitState actingCard = null)
```

**判据**：原版死亡 action 在判死那一刻就把 `actingCard`（= `DamageInfo.actingCard`）一起打包
（`d:/2/tools/decomp_full/CardScript__TriggerUnitBacklashActions.c:44-50`，入队 `:181`）。
`ResolveDeadCard.c:248` 的 Slay 闸读的正是 `actingCard == this`。所以「谁算这一场击杀」是
**随死亡一起走的**，不能在出批时统一猜。

**填法（按原版 deathType 两档 `combatDefender(15)` / `ability(20)` 放行）**：

| 路 | 入队处 | `ActingCard` |
|---|---|---|
| 攻击主伤害 / 星镖 / 标记光 / 爆裂 / 践踏 | `DeclareAttack` 的那几发 `Hurt(..., actingCard: attacker)` | 攻击者 |
| 效果 `deal`（`DoDeal`） | `Hurt(..., actingCard: ctx.ActingUnit)` | 效果来源 |
| 每友军各开一火（`DoDamageEach`） | `Hurt(..., actingCard: u)` | 开火的那个单位 |
| 效果「摧毁」（`DoDestroy`） | `CleanupDeaths(..., actingCard: ctx.ActingUnit)` | 效果来源 |
| 封闭文法 `damage`（`ResolveEffect`） | `Hurt(..., actingCard: source)` | 效果来源 |
| **反击**（`combatAttacker(10)`） | 不传 | `null`（不记） |
| **哨戒**（原版 actingCard 未钉死） | 不传 | `null`（沿用旧口径） |
| **不稳定自爆 / 毒（`poison(40)`）** | 不传 | `null`（不记） |

- 易伤（`vulnerable`）不是独立入队路径：它已被折进**同一次** `Hurt`
  （`DamageAfterReduction` / `VulnerableDamagePart`），所以来源卡就是这一下 `Hurt` 的来源卡。
- `Hurt` 新增可选形参 `UnitState actingCard = null`，一路传到 `RemoveIfDead → CleanupDeaths → AddPendingDeath`。
  **批外**（没有延后）的死亡这条路**不触发任何结算** —— 仍由调用点当场记账（行为不变）。
- `EffectResolver` 的三处效果路径加了 `if (!ctx.DeathsDeferred) TryCreditKill(...)`：
  死亡若被延后，记账改由 `FlushDeaths` 做一次；不加这道闸会**记两遍**（Slay 双响）。

---

## 二、交错实现

文件：`RuleEngine/Core/RuleCore.cs` · `FlushDeaths`

```
foreach (var pd in pending)            // 入队顺序 = 原版 death action 的 FIFO（SLAYFIX 已改）
{
    if (!FindSlot(ctx, pd.Unit, ...)) continue;
    CleanupDeaths(ctx, nowP, nowSlot, pd.Killer, maySurvive: true);  // 这个死者的反噬/死亡监听器
    if (pd.ActingCard != null)
        TryCreditKill(ctx, pd.ActingCard, pd.Player, pd.Unit);       // 该死者的 Slay + Kills
}
```

**判据链**（`d:/2/tools/decomp_full/`）：

1. `CardScript__TriggerUnitBacklashActions.c:44-50,181` —— 判死那一刻把
   `targetCard=死者 / actingCard=伤害来源 / deathType` 包成一条 action，`AddAutoActionToQueue`；
2. `BattleManager._ResolveBacklash_d__451__MoveNext.c:88`（死者 `ResolveDeadCard`）→
   `:94`（`TriggerUnitDeathActions`）→ `:144`（`BroadcastDeadUnit`）；
3. `BattleManagerSupport__BroadcastDeadUnit.c` 对场上每张牌 + 双方手牌逐个 `ResolveDeadCard`；
4. `CardScript__ResolveDeadCard.c:242-263` —— 观察者就是凶手（`actingCard == this`）时触发 Slay。
   ⇒「死者1 反噬 → 死者1 的 Slay → 死者2 反噬 → 死者2 的 Slay」。

**为什么记账必须在 `CleanupDeaths` 之后**：原版 Slay 在 `BroadcastDeadUnit`，而它排在死者自己的
`ResolveDeadCard` / `TriggerUnitDeathActions` 之后；`:248` 的 `cardState != 5` 闸正是为
「凶手在这一死者的反噬里已经被打死/将死」准备的。放到 `CleanupDeaths` 之前会多算（见 §四 ②）。

**`DeclareAttack` 尾段那两句 `TryCreditKill` 已删**（主目标 + `splashKills`）。`splashKills`
那张局部表一并删掉 —— 它的唯一用途就是喂那两句。**次数没有变**（每个死者一次、按入队顺序），
只是 Slay 被插回它该在的那一拍。

⚠️ **同一处还收掉了一个旧的「尾段兜底」副作用**：改前只要主目标在**出批后的 flush 期间**
死于别人的反噬/自爆/任何非本攻击来源，尾段那句都会把它算成攻击者的击杀（`actingCard` 根本没参与）。
改后这类死亡要么记给真实来源（效果路），要么不记（反噬/自爆的 actingCard = 将死的死者，原版
`cardState == 5` 同样挡掉）—— 这是**修对了**，不是回归。

---

## 三、测试与灭自证

文件：`RuleEngine/Editor/RuleEngineTest.cs` · 新增 `TestW58SlayInterleave`（注册在
`TestW58BlastStompSlay` 之后）。

**共同夹具**：P0 攻击者 1 攻 / 3 血 / `Blast 2` + `Slay: Damage 2 EnemyWarlord`；
P1 主目标 9 血（打不死）；相邻 A(2 血)、B(2 血) 都被 Blast 溅死。

| # | 夹具差异 | 断言 | 改回旧实现会怎样 |
|---|---|---|---|
| ① | B 带 `Backlash: Deal 4 damage to all enemies`（会打死 P0 攻击者） | 敌方督军 **-2**；`Slay` **恰好 1 条**；`Slay` 事件下标 **<** B 的阵亡事件下标 | **实测**（临时把记账挪到循环之后）：督军 **-0**、`Slay` **0 条**、顺序 false ⇒ **三条全红** |
| ② | A（只有它，且它自己带上述反噬） | `Slay` **0 条**、督军不掉血 | **实测**（临时把记账挪到 `CleanupDeaths` 之前）：`Slay` **1 条**、督军 -2 ⇒ **红** |

- ① 钉「交错」：A 的 Slay 必须落在 B 的反噬之前（计数 + 事件先后两条，不是同义反复）。
- ② 钉「先反噬、后记账」：死者自己的反噬打死凶手时，这一条死亡**不算击杀**
  （原版 `ResolveDeadCard.c:248` 的 `cardState != 5`）。
- 既有 `TestW58BlastStompSlay` 的 9 组语义**没有改期望值**：用 scratch 探针复刻后逐条读数与
  `W58-SLAYFIX` 报告**完全一致**（见 §四·2）。

---

## 四、ruleprobe / scratch 读数（改前 → 改后）

### 4·1 全池对拍（纯 dotnet，未重钉）

| 项 | 改前（本笔开始时的 SLAYFIX 工作树） | 改后 |
|---|---|---|
| `play check` 结算指纹 | `0aa5c8442664796fd63814cec3388511` | **同一个**（`0aa5c844…`） |
| 基线文件指纹 | `08d51f6d0b749e511f53088ef7fdd80f`（**没动**） | 同左（**没动**） |
| ①-a 主档差异 | 3 张卡变了 | **3 张卡变了**（同一批） |
| ①-b 事件电池差异 | 3 张卡变了 | **3 张卡变了**（同一批） |
| `play all dump` 全池 1128 行 | —— | **与改前逐字节一致**（`diff` 无输出） |

⇒ 本笔**不改变任何卡的既有 `play` 读数**；交错只在「一次结算里死多个 / 死者反噬会反打凶手」
这类**探针夹具到不了的**局面里生效（本笔新增的两组 scratch 夹具专门造它）。

### 4·2 既有 `W58-SLAYFIX` 夹具（scratch，复刻 `TestW58BlastStompSlay`）

```
D2     adjDead=True mainAlive=True      lord 20→18  slay=1
D2'    lDead=True rDead=True            lord 20→16  slay=2
D4     allDead=True                     lord 20→14  slay=3
VULN   vDead=True pAlive=True pHp=1     lord 20→18  slay=1
STOMP  mDead=True aDead=True            lord 20→16  slay=2
COUNTER atkrDead=True bruteAlive=True   lord 20→20  slay=0
TACTIC preyDead=True                    slay=0  hand 2→1
WARLORD lordAlive=False over=True       slay=0  hand 0→0
REMNANT remInDiscard=True remAlive=False slay=0  hand 0→0
HUNT  Blast 溅死 Hunt Mark 2 → 20→22 / 30→28；Stomp 溅死 Hunt Mark 1 → 20→21 / 30→29
live  Helspear（手牌注入 + 能量）→ blast=2 slay=1
```

**与 `W58_战斗结算_slay修复.md` §五·3 逐条相同** ⇒ 既有断言语义保持。

### 4·3 交错专项（scratch，复刻本笔的 `TestW58SlayInterleave`）

| 实现 | ①（A 的 Slay vs B 的反噬） | ②（死者反噬打死凶手） |
|---|---|---|
| **本笔（逐死者交错、记账在反噬之后）** | lord `20→18` · slay **1** · slayAt=5 < bDeathAt=7 ✅ | slay **0** · lord `20→20` ✅ |
| 临时改回「全部反噬后统一 Slay」 | lord `20→20` · slay **0** · order=False ❌（测试红） | slay 0（这一维测不到，由 ② 的下一行挡） |
| 临时改成「先记账、后反噬」 | lord `20→16` · slay **2** ❌（测试红） | slay **1** · lord `20→18` ❌（测试红） |

> scratch 探针放在 `C:/Users/qjh36/AppData/Local/Temp/w58interleave/`，**没有写进仓库**；
> 两个「临时改回」都是在 `RuleCore.cs` 里改完、跑完、**立刻用 Edit 还原**（已 grep 确认无 `TEMP-` 残留）。

---

## 五、没查清的部分

1. **哨戒那一下的 `DamageInfo.actingCard`** 仍沿用旧口径「不记」—— 本次没有接哨戒路
   （与 `W58-SLAYFIX` 报告 §六·2 同一条未决项）。
2. **`waitingToDie(5)` 与 `IsAlive` 的边界**：`TryCreditKill` 用的是「凶手 `IsAlive`」，原版是
   `cardState != 5`。在「血 ≤ 0 但带 `Survivor`」那一档两者可能不等价 —— 本笔**没有动**
   `TryCreditKill` 的闸（同一份判定口，改了会牵动 `A1249/A1250` 的既有判别式）。
3. **效果伤害在「攻击批内」的死亡现在会在批末 flush 记账**（原来在 `DoDeal` 当场记）。
   这与原版「death action 入队、出批后逐条结算」一致，也能被上面的 scratch 覆盖；
   但**多目标效果在同一批内连杀多个**时的事件顺序**没有现成实况**可比，如实标着。
4. **未跑 Unity 断言**（按铁律 12 / 本笔指示）：`TestW58SlayInterleave` 只做了 typecheck +
   scratch 等价验证；`RuleEngineTest.Run` 的红绿要等统一验收那一次。
5. **`ActionLog` 事件顺序**：`Kills` 广播（`BroadcastWhen`）现在跟着每个死者的节奏发，
   不再是「所有死者反噬之后」。若某条既有断言读的是 `ActionLog` 的**跨死者相对顺序**，
   可能需要在统一验收时看一眼 —— 但本笔**没有为了让它绿去改任何期望值**。

---

## 六、改动清单

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/BattleContext.cs` | `PendingDeath.ActingCard`；`AddPendingDeath(..., actingCard = null)`；订正「格号只在排序时用」的陈旧注释 |
| `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` | `CleanupDeaths` / `Hurt` / `RemoveIfDead` 加 `actingCard`；`FlushDeaths` 逐死者交错；`DeclareAttack` 把主伤害/星镖/标记光/爆裂/践踏的 `actingCard` 传下去、删掉尾段统一记账与 `splashKills`；`ResolveEffect` 的 `damage` 支按批内/批外分流记账；`TryCreditKill` 的映射注释同步订正 |
| `Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs` | `DoDeal` / `DoDamageEach` / `DoDestroy` 传 `actingCard`，批内死亡不再当场记账（改由 `FlushDeaths`），防双记 |
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | 新增 `TestW58SlayInterleave`（①正例 / ②反例）+ `Step(...)` 注册 |

**为什么动了 `EffectResolver.cs`（白名单外的那一个，先报理由）**：效果路径的死亡也会进
`PendingDeath`；若「`AddPendingDeath` 记了 `actingCard`」而调用点仍当场 `TryCreditKill`，
**同一死者会记两遍**（Slay 双响、`Kills` 双播）。改法是**同一份判据的两半**，拆不开；
不传 `actingCard` 又做不到任务要求的「效果伤害路径填对」。

**参照文件**：`d:/2/tools/decomp_full/CardScript__TriggerUnitBacklashActions.c:44-50,181` ·
`BattleManager._ResolveBacklash_d__451__MoveNext.c:88,94,144` ·
`BattleManagerSupport__BroadcastDeadUnit.c` · `CardScript__ResolveDeadCard.c:242-263` ·
`BattleManager__ResolveTraitAdjacentDamage.c:18-24` · `BattleManager__ResolveTraitStompDamage.c`。

---

## 七、typecheck 结果

```
TMPDIR=/tmp/wf_interleave bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

`git diff --numstat`（本笔涉及的 4 个文件）：

```
28   2   BattleContext.cs
16   6   EffectResolver.cs
138  28  RuleCore.cs        （含上一笔 SLAYFIX 的未提交改动）
405  0   RuleEngineTest.cs  （含上一笔 SLAYFIX 的 308 行）
```

行尾核对：`BattleContext.cs` / `RuleCore.cs` 是 **LF**，`EffectResolver.cs` / `RuleEngineTest.cs`
是 **CRLF** —— 改完**各自保持原行尾**（`git diff --numstat` 无整篇翻转）。
