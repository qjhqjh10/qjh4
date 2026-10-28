# B94 · R-Y 只读诊断：`play all` ⑧ 事件电池自检「敌方阵亡（⑥ 真击杀）」红

会话 2026-10-29 · 代理 `R-Y` · **只诊断、零改动**（没改源码 / 账 / 正本，没跑 Unity，没动 git）

---

## §0 结论先行（TL;DR）

| 问 | 答 |
|---|---|
| 这条红是真的吗？ | **真**。而且它报的是**真问题**：`When an enemy dies` 这一族在电池里**已经抬不起来了**（池内 `DA19` / `SAU32` / `TAU70` 的电池净差现读 = **0**）。 |
| 是本会话引入的吗？ | **不是。** 引入点 = **`B93_F7`（第九十三会话 · 2026-10-28 · 已在 HEAD `6877d563`）**在 `RuleCore.cs:10055/10063` 给死亡广播加了 `preCalled: actingCard`（原版棋盘跳「排除凶手」）。本会话 8 条改动**没有一条**落在死亡广播那条链上（§三 逐条）。 |
| 为什么当时没发现？ | 🔴 **前提要纠正**：⑧ **只在 `play all` 里跑**，`play check` **根本不调** `EventBatterySelfCheck`（也不调 `BucketSelfCheck`）⇒ 「同步点 `play check` rc=0」**证明不了这条自检是绿的**。第九十三会话的同步点五闸里**没有 `play all`**（交接表五闸 = typecheck / `play check` / `ruleprobe check` / A 落点 / Unity 20 宿主）⇒ 这条红随 `B93_F7` 一起静默进了 HEAD。 |
| 该怎么修？ | **改夹具，不要回退引擎**（`RuleCore.cs:10063` 那条有 `.c` + `out_il2csharp` 双证，且已被 `RuleEngineTest.cs:39465` 的双断言钉住）。最小改法见 §五（推荐方案 A：只在自检这一对让「凶手不是被测卡」⇒ 池内 1073 条 `E\|` 行不动 ⇒ **不必重钉基线**）。 |

---

## §一 这一条自检到底测什么（逐字抄代码 + 行号）

### 1·1 自检本体

`Unity/工具/ruleprobe/PlayProbe.cs:3968` `static int EventBatterySelfCheck(List<CardDef> pool)`。

- `:3975-3976` **夹具卡在内存里现造**（⛔ 不在卡池里，所以**没有池内 id 可以 `play <id>`**）：

```csharp
Func<string, string, string[], CardDef> fx = (id, desc, kw) =>
    new CardDef(id, id, "unit", desc, "common", "Fixture", 1, 3, 20, 2, kw, subtype: "Troop");
```

（费用 1 · 近战 **3** · 血 **20** · 远程 2 · `subtype=Troop`；卡 id 固定 `FX-EBA`）

- `:4001` **被测的那一对**（逐字）：

```csharp
new object[] { "敌方阵亡（⑥ 真击杀）",   "When an enemy dies, deal 3 damage to an enemy troop",   "When an enemy dies, deal 8 damage to an enemy troop",   new string[0] },
```

- `:4022-4032` **判别式**（两条只差一个数字，净差必须**不同**）：

```csharp
var ra = MeasureEvents(pool, fx("FX-EBA", a, kw));
var rb = MeasureEvents(pool, fx("FX-EBA", b, kw));
string na = Norm(ra), nb = Norm(rb);
bool diff = na != nb;
```

- `:4052-4056` `Norm()` = `"净差条数|排序后的逐条原文"`（⛔ 不带卡 id，否则恒真 = 自证）。
- `:4030-4043` 红判据：`!diff` ⇒ `fail++`，文案「期望两条净差**不同** —— 相同 = **电池没抬起这个事件**」。

`MeasureEvents`（`:2851`）：电池局（被测卡真打出去 + 走完整电池）vs **空正文对照局**（`Blank(...)`），
`r.Net = a.Changes.Where(x => !b.Changes.Contains(x))`（`:2876`）—— 即「这张卡的正文造成的局面变化」。
⇒ **净差 = 0 就是「这段正文一个数字都没动过」**。

### 1·2 「⑥ 真击杀」这一步（`PlayProbe.cs:2541-2589`）

- 目标：`int kv = PickKillTarget(ctx, 1);`（`:2556`）—— 现读 `tgt5`。
- 先把目标**削到 1 血**（`:2562`）：`RuleCore.ApplyDamage(...)`（**只扣血、不处理死亡**）。
- 凶手：`int k0 = PickKiller(ctx, 0, slot, out kRanged);`（`:2566`）⇒ `RuleCore.DeclareAttack(ctx, 0, k, 1, kv, ranged, forced: true)`（`:2576`）。
- `:2589` `ep.Steps.Add("kill=" + killStep)`。

### 1·3 🔴 凶手**就是被测卡自己**（这是整件事的关键）

`PlayProbe.cs:2767-2791`：

```csharp
static int PickKiller(BattleContext ctx, int p, int measuredSlot, out bool ranged)
{
    ...
    if (BoardSpec.IsValid(measuredSlot) && b[measuredSlot] != null && !b[measuredSlot].IsWarlord)
    {
        var m = b[measuredSlot];
        if (Math.Max(m.Attack, m.RangedAttack) > 0)   // 夹具 3/2 ⇒ 恒真
            { ranged = m.RangedAttack > m.Attack; return measuredSlot; }   // ← 优先返回【被测单位】
    }
```

夹具近战 3 > 0 ⇒ **`k0` 永远是被测单位** ⇒ ⑥ 的击杀**永远由被测卡自己完成**
（实测 `kill=0@2M/gone@tgt5`，而 `✎ 改动 P0[2] FX-EBA` 说明被测卡就落在 P0[2]）。

---

## §二 现在的实测读数

### 2·1 复现（`bash Unity/工具/ruleprobe.sh play all dump _tmp_view/ry_play_events.tsv`）

```
  --- ⑧ 🔴 事件电池判据自检（`A1431` 第三笔 · **灭自证**：同卡改一个数，净差必须不同）---
    🔴 敌方阵亡（⑥ 真击杀）  净差 0 条 vs 0 条（**两条都是空的**）
        真：`When an enemy dies, deal 3 damage to an enemy troop`  ⇒ 0|
        改：`When an enemy dies, deal 8 damage to an enemy troop`  ⇒ 0|
        steps play=0,oath=14,selfChip=2,softAtk=0@3/survived,chip2=2,5,tactic=0@2,troop2=0@1,end0=0,enemyTactic=0@3,friendDies=0@1/atk@3,end1=0,collect=0@1/rem,draw=1,abil=14,atkM=0,atkR2=0,kill=0@2M/gone@tgt5
        🔴 期望两条净差**不同** —— 相同 = **电池没抬起这个事件**…
  ⇒ 判据自检（`A1431` 第三笔 · 事件电池）红 1 条 🔴 —— 它也计入 rc。
=== 结束（rc=1）===
```

**读这三格就能定性**：

| 读数 | 含义 |
|---|---|
| `kill=0@2M/gone@tgt5` | 🔴 那一步**返回码 0 且受害单位真的离场**（`gone`）⇒ **「敌方阵亡」这件事本身抬起来了** |
| 两条净差都是 `0\|`（空） | ⇒ 抬不起来的是**监听者**，不是事件 |
| 其余 22 对全 `✅`（含 `友军阵亡（friendDies）` 净差 16 vs 17） | ⇒ 「死亡广播 + `When … dies` 解析 + 结算」这条链**整体是通的** ⇒ 只有「**凶手自己当监听者**」这一档听不见 |

### 2·2 池内的同族读数（这条红不是孤例）

`When an enemy dies` 的池内卡（`cards_engine.json` 现读 6 张）：`BL7` / `DA19` / `DA55` / `SAU32` / `TAU70` / `TAU57`。
现读 `_tmp/view/ry_pf.txt` **与基线 `基线/out_play_baseline.txt` 逐字节相同**，其中

| 卡 | 电池净差列（现读 = 基线） |
|---|---|
| `DA19` / `SAU32` / `TAU70`（`abilityTriggers` 含 90 的三张部队） | **空 = 净差 0** |

⇒ **「敌方阵亡」这一族在电池里现在已经完全没被验到**（正是 ⑧ 想守的那件事）。所以这条红**不该被抹平**。

### 2·3 `play check` 的现状（另一件事，别和 ⑧ 混）

`play check` 现读 **rc=1**：①-a 主档 **28 张卡变**，①-b 电池同 28 张。
逐张看，全部是「**多付一次能量**」（`other +1`、`energyDelta` 再 −1/−2、`expectedPay` 多出 `⇒ 照卡面应付 N · 除付费外净 -1`），
名单 = `UM89-UM118` 共 26 张 + `4de4fcfb…`（Angel's Wrath，tactic，`Oath 2:`）+ `8482077f…`（Catechism of Death，tactic，`Oath 3:`）
⇒ 形态与**本会话 `B94_F1` 新增的 `RuleCore.PayCardPlayedCostOath`**（类尾，`RuleCore.cs:16960`）+ `B94_Z3` 的 `chosenAbility 360` 分支吻合。
**与 ⑧ 那条红无关**（⑧ 的那一对不含 oath、也没有 UM 类费用）。
⚠️ 这是**在飞改动**，不是缺陷判定 —— 本报告只记录读数。

---

## §三 本会话 8 条改动逐条「会不会影响那一对」

判据：**能不能改变「被测卡（= 凶手）是否听见那次死亡」**。这一对的净差要能不同，
唯一的路是「让 `When an enemy dies, deal N damage` 这段正文真的结算一次」。

| # | 改动 | 会/不会 | 理由（有代码落点） |
|---|---|---|---|
| 1 | **Y1** `TroopKilled` 计数从 `TryCreditKillCore` 挪到 `CleanupDeaths`（`:6239` / `:9786` / `:9908`） | **不会** | 只写 `ctx.Tally.AddTroopKilled(...)`（成就计数），**不碰广播、不碰 `ctx.Log`、不碰棋盘**；而且**两条变体都会同样多记一次** ⇒ 净差不变（实测两条都是空）。 |
| 2 | **B22 A-5** `DeathTypeOfSource` 整张表 + `killedByVulnerable`（`ApplyDamageCore` out 参数 · `RuleCore.cs:9163`） | **不会** | `killedByVulnerable` 只改传给 `RemoveIfDead` 的 **`deathType`**，而 `deathType` 只影响 **175/180/170 死因三族**（`RuleCore.cs:10076-10088`，只发 `EvtKind.Trigger`、**不结算效果**）。`WhenEventKind.Die` 那一跳在 `:10055`，**与 `deathType` 无关**。另外夹具那一下 `kind = Unknown` ⇒ `vulnKilled` 恒 false ⇒ 连 `deathType` 都没变。 |
| 3 | **B22** `SeatOf` / `EventActingCard` / `OwnerSeat`（`RuleCore.cs:17060`、`EffectResolver.cs:9653/9683`、`ActionPump.cs:392/472`） | **不会** | `SeatOf` 的调用点只有 `:9786` / `:9908`（Y1 的计数）、`:16131`（syphon）、`:17080`（`VulnerableCombatDeathType`）—— **都不在 Die 广播链上**。`EventActingCard` 的读点 = `EffectResolver.cs:1669`（`ta=80 actingCard` 那一族目标）；本对正文的目标是 `an enemy troop`，**不是 actingCard**。`ActionPump:392/472` 只给「已离场的 actor」补座位（夹具里凶手活着）。 |
| 4 | **B14 546** `BroadcastWhen(Reanimated)` 时机上移（`EffectResolver.cs:15557/15600`） | **不会** | 另一个 `WhenEventKind`（`Reanimated`），本对完全够不到。 |
| 5 | **B14 521** jam 时补清 `ActiveEffects`/`ActiveEnchantments` | **不会** | jam（`DoJam`）不在夹具电池的步骤里，且与死亡广播无关。 |
| 6 | **B21** 新增 `BroadcastTurnSetup` / `BroadcastSupportOnSummon`（未接线） | **不会** | 无调用点（新增未接线），零行为。 |
| 7 | **Z2** 手牌 `abilityTriggers` 执行口 + `BroadcastWhen` 加 `damageType` 形参 | **不会** | 新形参只被 **`Damaged` 那一跳的手牌两跳**读（`RuleCore.cs:9396`；`EffectResolver.cs:9883` 的 doc 明写）。本对监听的是 **`Die`**；手牌执行口只增加「手牌卡被叫醒」的路，夹具的被测卡**在场上**、手上没有监听卡。 |
| 8 | **B13** `EmitHit` 加 `DamageKind` + `DamageTypeOfKind` 映射 | **不会** | 同 #7：只影响 `Damaged` 广播携带的 `damageType`（185/187 两条手牌触发）。与 `Die` 无关。 |

**八条里没有一条落在**：`BroadcastWhen(WhenEventKind.Die …)`（`RuleCore.cs:10055`）· `SnapshotBoardListeners` 的 `preCalled` 排除（`EffectResolver.cs:10090`）· `Hurt → ApplyDamage → HurtCore → RemoveIfDead → CleanupDeaths` 那条 `actingCard` 传递（`RuleCore.cs:7018` / `:9163` / `:9192`）· `WhenEvents.Matches` 的极性判定。

---

## §四 结论：是谁引入的（证据链）

### 4·1 直接原因（一行代码）

`Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs:10055-10063`：

```csharp
BroadcastWhen(ctx, WhenEventKind.Die, p, u.Card, u,
              ...
              actingCard: actingCard, preCalled: actingCard);
```

`preCalled` 在 `EffectResolver.SnapshotBoardListeners`（`:10065`）里是**从棋盘监听者快照里排除**：

```csharp
if (preCalled != null && ReferenceEquals(u, preCalled)) continue;   // EffectResolver.cs:10090
```

⇒ `actingCard` = **凶手**（`RuleCore.cs:7018` 攻击那一跳 `Hurt(..., actingCard: attacker)`）
⇒ **凶手不会被自己这一记的死亡唤醒** ⇒ 夹具的被测卡（= 凶手）听不见「敌方阵亡」⇒ 两条净差恒 0。

### 4·2 引入点 = 第九十三会话（已在 HEAD，⛔ 不是本会话）

- **`git show HEAD:...RuleCore.cs | grep -n "preCalled: actingCard"` ⇒ `10063`**（工作树里这一行 `git diff` **不显示** = 与 HEAD 一致）。
- `git show 6877d563 -- Unity/工具/ruleprobe/PlayProbe.cs` ⇒ **本会话之前的最后一次改动只改了 `PinnedPlayMd5` 一个常量**
  （`6cbb80d7…` → `2f901cc9…`），**夹具与判别式一个字都没动** ⇒ ⑧ 的输入没变。
- `B93_F7` 自己的报告 `资料/完全复刻/报告/B93_F7.md` §0 **逐字写着这件事的后果**：
  > 读数 指纹 `ef8a0249…`（基线）→ **`2f901cc9…`**；`①-a` 主档 **1129 张全等**；`①-b` 事件电池 **3 张变**：`DA19` / `SAU32` / `TAU70`
  > 归因（唯一一处改动） **`RuleCore.cs:10063`** … **加了 `preCalled: actingCard`**
  > 为什么正好是这 3 张 … 电池里那一步是「**被它自己那一记攻击打死**」…（net 1/6/8 → 0）
  ⇒ **同一个机制、同一批卡**：`When an enemy dies` 族的净差被这条排除打到 0，而这正是「敌方阵亡」那一对要测的东西。

### 4·3 这条排除是**判据成立**的（所以别当成回归去打回）

- 判据双证：`decomp_full/BattleManagerSupport__BroadcastDeadUnit.c` 的 `if (unitsInPlay[i] != battleAction.actingCard)`
  ＋ Ghidra 丢的第 2 个实参由 `out_il2csharp/Assembly-CSharp/BattleManagerSupport.cs:315` 的 `if (list11[num2] != object1)` 补上（`object1` = 结构体 `+0x08 actingCard`）。
- 已被**两条 Editor 断言**钉住：`RuleEngineTest.cs:39465` `CheckB93F7DeadUnitExcludesKiller`：
  - ① 凶手 = 监听者自己 ⇒ **不许听见**（攻 2 保持 2）—— 断言文案自己写着「🧨 去掉 `preCalled: actingCard` ⇒ 它听见、攻变 4，本行红」；
  - ② 正对照：同一局面、无凶手（`actingCard == null`）⇒ **必须听见**（攻 2 → 4）。

⇒ **`RuleEngineTest` 的 ① 与 ⑧ 的「敌方阵亡」那一对结构上互斥**：一个要求「凶手听不见」，一个要求「凶手（=被测卡）听见」。

### 4·4 为什么九十三会话的「五闸全绿」没抓到

`EventBatterySelfCheck` 的唯一调用点是 `PlayProbe.cs:3823`，它在 **`Sweep`**（`:3436`，= `play all`）里；
而 **`Check`**（`:4519`，= `play check`）**从头到尾没有** `EventBatterySelfCheck` / `BucketSelfCheck`（grep 全文件只有 `:3815` / `:3823` 两个调用点）。
`交接_第九十三会话.md` 的同步点表五闸 = `typecheck` / **`play check`** / `ruleprobe check` / A 落点 / Unity 20 宿主 —— **没有 `play all`**。
`调度台日志.md:8283`（第八十九会话）那句「`play all` rc=1 只剩 ⑧ `Mob` 一格」是 **B93_F7 之前**的读数；之后没有任何一处再记 `play all`。

---

## §五 修复建议（**只给建议，⛔ 我没改任何代码**）

### 5·1 推荐：改夹具，让 ⑥ 的凶手**不是被测卡**（引擎一个字不动）

需求：`When an enemy dies` 的监听者要在场、且**不是凶手** ⇒ 那一对的两个数字才有区别。

**方案 A（推荐 · 零基线重钉）**

- 落点：`PlayProbe.cs` 的 `EventBatterySelfCheck`（`:4001` 那一对的调用）+ `RunEventPass` 的 ⑥ 段（`:2564-2580`）。
- 做法：给这一对传一个「凶手不得是被测单位」的开关（例如在 `PresetSpec` 里加 `killbymate`，或给 `pairs` 的第 5 格塞一个 fixture/选项），
  ⑥ 里当开关为真时把 `measuredSlot` 从候选里剔掉，只留 `PickAttackerOnSide`（`:2568`，夹具里 P0 还有 `FIX_A` 2 攻 / `FIX_B` 3 攻 / `FIX_C` 1 攻）。
- 为什么「零基线重钉」：这个开关**只在自检那一对用**，池内 1129 张卡的 `steps` 一列**不动** ⇒ `基线/out_play_baseline.txt` 与 `PinnedPlayMd5` 都不用改。
- 建议同时在两个变体（3 / 8）上**保留**「凶手=被测卡时不许听见」的对照 —— 那是 `RuleEngineTest.cs:39465` ① 的产品侧等价物，⛔ 别一起删掉。

**方案 B（覆盖面更大，但要重钉）**

- 在 ⑥ 之后**再加一个共享步骤**（例如 `mateKill=`：让一个**非被测**的己方部队打死另一个敌方单位）。
- 好处：全池带 `When … dies` 头的卡都能被验到（不只是这一对）。
- 代价：会改**全池 1073 行 `E|` 的 `steps` 列** ⇒ 必须「先测后钉」：`play all pf` 重生成 + 改 `PinnedPlayMd5` + 归因写进常量注释（调度台串行做，⛔ 不许写手顺手钉）。

### 5·2 ⛔ 不推荐的两种「变绿」法

1. **回退 `RuleCore.cs:10063` 的 `preCalled: actingCard`** —— 与 §4·3 的双证冲突，会打回 `DA19/SAU32/TAU70` 的读数、推翻 `CheckB93F7DeadUnitExcludesKiller` ①，还要重钉两颗基线。**不建议**。
2. **重钉基线 / 删掉这一对 / 放宽 `diff` 判据** —— 那正是这条自检存在的意义（`A1431` 第三笔「灭自证」），删了 = 把「档 B 没被验到」这件事永久静音（违反 CLAUDE.md「不许静默失败」与铁律 11）。

### 5·3 流程建议（比修这一条更值钱）

1. **同步点五闸补一条 `play all`**（或至少 `play all dump` 的 ⑧ / ⑨ 两节）—— 否则同类「自检红」会继续藏在 `play check rc=0` 后面。
   成本实测：`play all dump` 全池 ≈ 6 秒（不含 `dotnet build` 的 1.5 秒）。
2. 可选：把 `BucketSelfCheck` / `EventBatterySelfCheck` 也接进 `Check()`（它们**不读基线、不依赖池指纹**，只是跑十几次内存夹具局），
   这样「结算对拍」与「判据自检」同一次运行就都覆盖到。

---

## §六 判不了 / 还差什么（⛔ 不猜）

| # | 没查清 | 差什么 |
|---|---|---|
| 1 | ⑧ 里 **`Mob（攻击那一发）` 那一格是「什么时候转绿的」**：第八十九会话记录它是唯一的红（判为「夹具盲区」，`调度台日志.md:8283/8309`），现读**已绿**；第九十～九十三会话**没有任何一条记录**提及它。 | 一次 **HEAD 对照运行**（把工作树的引擎改动隔离掉）才能定它是 90–93 哪一轮转绿的 —— ⛔ 本代理**不许** `git stash`，所以没做。 |
| 2 | `play check` 现红 28 张的**完整归因**：形态全部是「多付一次能量」，名单全部带 `Oath N:`，与 `B94_F1`（`PayCardPlayedCostOath`，单位）＋ `B94_Z3`（`chosenAbility 360`）吻合。但其中 **2 张是战术卡**（`Angel's Wrath` / `Catechism of Death`），而 `B94_F1` 的注释明写「战术卡走 `PlayTactic`，本批**没**补那一处」⇒ 战术卡这次是谁付的，**没核**。 | 一次单卡 `play 4de4fcfb5d7e0554a88b9f419a2cd7c2` 的 `energyDelta`/`✎ 改动` 逐条对照，或读 `PlayTacticCore` 的 oath 付费点。 |
| 3 | 方案 A 的前提「⑥ 那一刻 P0 还有别的攻击者」**没读数验证**。结构上应该有（P0 的 `FIX_A` 步 2/4 · `FIX_B` 载具 3/5 · `FIX_C` 兽 1/4（攻/血），落在 3/6/7 三格 —— `PlayProbe.cs:3563`；`friendDies` 只打死 P0[1] 的 `troop2`），但 `steps` 里只印了**第一个**凶手（打死即 `break`，`:2579`），`PickAttackerOnSide` 当时是几**看不见**。 | 写手在 ⑥ 里临时加一格 `killers=0,x`（或先跑一张带 trigger 90 的卡看 `kill=` 有没有第二候选）确认后再落方案 A。 |
| 4 | 夹具 `fx` 的攻击值若改成 0（另一种「让 `PickKiller` 退到 mate」的写法）会不会破坏本对其它前置步骤（`selfChip` / `softAtk` 依赖被测单位能出手） | 需要写手按 §五方案 A 那种**开关式**改法（不改 `fx` 的公共值），或另跑两局对照确认。 |

---

## 附：本报告用到的读数命令（都可复跑）

```bash
bash d:/4/Unity/工具/ruleprobe.sh play all dump _tmp_view/ry_play_events.tsv   # ⑧ 自检 + 全池主档
bash d:/4/Unity/工具/ruleprobe.sh play all pf   _tmp_view/ry_pf.txt            # 主档 + E| 电池行
bash d:/4/Unity/工具/ruleprobe.sh play check                                   # 与基线逐卡对拍（⚠️ 不含 ⑧）
```
