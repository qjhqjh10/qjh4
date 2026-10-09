# P-D · `A1176` —— 震荡「残骸档」少晕一处（`targetDied` 那一段）

> 执行代理 P-D · 2026-10-10 · 白名单两个文件：`RuleEngine/Core/RuleCore.cs` · `RuleEngine/Editor/RuleEngineTest.cs`
> 红线：**没跑 Unity**（断言只写不跑）· **没动 git** · **没碰两张正本** · `d:/2/**` 只读。
> ⚠️ **简报给的自检文件路径不存在**：`CardPresentation/Editor/RuleEngineTest.cs` 没有这个文件，
> 真身在 **`d:/4/Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`**（**CRLF**，`b.count(b'\r\n') == b.count(b'\n') == 23497`，现核）。
> 我改的是真身。**简报里那条路径是错的**（报告给调度台，别让它进正本）。

---

## 1. 一句话结论

**差异坐实、方向也坐实（原版会晕、我们原来不晕），已按「只加一格」的形状修掉**：
`targetDied` 的**定义一个字节都没动**，改的是震荡那一格的判别式 —— 新判定口 `RuleCore.StunStillOnBoard`，
它把「被打到 ≤0、但**翻面成残骸**」那一档**算作还在场上**，并把眩晕挂在**棋盘上现在占位的那一个 `UnitState`** 上
（不是被作废的旧 `target` —— 挂错对象 = 静默丢掉）。

🔴 **「另三个用户」逐条核过：原版那三处的判别式【都不是】「会不会翻面」** ⇒
**如果照简报的第一种形状（改 `targetDied` 的定义）动手，会一次性引入三处新偏离**（详见 §2）。
⇒ 我选了简报给的第二种形状（**只加一格**），并把它收口成一个具名判定口。

---

## 2. 🔴 「共用 `targetDied` 的那几处」逐条核证表（全部现读 `d:/2/tools/decomp_full/`）

| # | 我方落点（现行号） | 原版判据（**方法体逐行读**） | 与「会不会翻面」是否一致 | 结论 |
|---|---|---|---|---|
| ① **Stomp** | `RuleCore.cs:3534` `if (targetDied && attacker.Has("stomp") && dealt > hpBefore && adjUnits.Count > 0)` | `BattleManager__ShouldTriggerStompDamage.c:20-36`：`HasCurrentTrait(**攻方**, 0x4d8 /*stomp*/)` ∧ `target != null` ∧ **`*(int*)(target + 0x68) < 0`（裸血严格 < 0）** ∧ `param_4 > 0`。调用点 = `_ResolveAttack…:1059`（`param_4` = 相邻表长度）。⇒ **全程只有「裸血 < 0」，一个字都没提残骸/`EnoughPendingDamageToDie`** | ❌ **不一致**（原版是纯裸血） | ⛔ **不能改 `targetDied` 的定义**：排除残骸后，溅射会**不再触发**，而原版**照样触发**（血 < 0 就触发）= 新偏离。⚠️ 顺带核到：原版传给 `ResolveTraitStompDamage` 的伤害是 **`|目标血|`**（`:1090` 的 `(v ^ v>>31) - (v>>31)`），与我们的 `dealt - hpBefore` **恒等**（`= -health`）⇒ 这一格**今天是对的** |
| ② **Sniper** | `RuleCore.cs:3555` `bool sniperKill = ranged && attacker.Has("sniper") && targetDied;` | `EntityScript__ActivatesNoReturnSniperAttack.c`（调用点 `_ResolveAttack…:879`）：`HasCurrentTrait(**攻方**, 0x3de /*sniper*/)` ∧ `CardScript.IsUnit(target)` ∧ `param_4 == 2（远程）` ∧ **`EntityScript__DamageKillsTarget(攻方, target, 攻方.CurrentRangeAttack)`** ∧ `CardScript__IsProtectedFromDamageOrSurvivor(target) == 0`。 `DamageKillsTarget.c` 的body = `target.health(0x68) <= Math.Max(1, dmg − target.armour(0xb8))` ⇒ **「现血 ≤ 这一下的调整后伤害」= 一个【预测】，只读血与护甲**；`IsProtectedFromDamageOrSurvivor.c` 查 5 个 trait（`0xf0/0x50/0x136/0xe6/0x1ae=430 survivor`） | ❌ **不一致**（原版是「血 ≤ 伤害」的预测 + 保护词） | ⛔ **不能改定义**：排除残骸后，远程击杀（残骸档）会**开始挨反击**，而原版 `DamageKillsTarget` 判「会打死」⇒ **不挨**。⚠️ **顺带如实记着（不是本笔的账）**：我们这一格用的是**结果**（`targetDied`），原版用的是**预测**（还多一道 `IsProtectedFromDamageOrSurvivor`）—— 形状本就不同，今天不展开 |
| ③ **Markerlight** | `RuleCore.cs:3587` `if (!targetDied && ranged && target.Has("markerlight"))` | `_ResolveAttack…:1251-1254`：`CurrentMarkerlight(**目标**) > 0` ∧ `attackType == 2` ∧ **`CardScript__EnoughPendingDamageToDie(target) == 0`**（非零 ⇒ **整段跳过、连标记光都不摘**）。 `EnoughPendingDamageToDie.c`：`cardState(0x228) == 5 ⇒ return 1`；否则 `health(0x68) ≤ 0 且 CurrentSurvivor == 0 ⇒ return 1`（`0 < health \|\| survivor != 0` 才去逐条算 pending damage）⇒ **判据是「血/待结算伤害」，与残骸无关** | ❌ **不一致**（原版是 `EnoughPendingDamageToDie`） | ⛔ **不能改定义**：排除残骸后我们会**多打一段标记光**并**把标记光摘掉**，而原版（血 ≤ 0 ⇒ `EnoughPendingDamageToDie` 真 ⇒ 跳过）**不做**。今天两边**一致（都跳过）** |
| ④ **`sniperKillEarly`**（`D28`） | `RuleCore.cs:3408` `sniperKillEarly = ranged && attacker.Has("sniper") && (targetDied \|\| DamageAfterReduction(target, atk) >= target.Health)` | 它是**我们**用来模拟原版 `EnoughPendingDamageToDie(attacker)` 那个**预测**的（原版 `CardScript__ResolveUnitAttacked.c:99-101` 判的是**攻方**会不会死），原版**不涉及**目标的「翻面」 | ❌（且**它自己就带了预测那一半**） | 改定义**不影响结果**（`targetDied \|\| 预测` 里预测对血 ≤ 0 的目标恒真）—— 但它是「`targetDied` 还被别处用」的第 4 处，**一并核过、一并列出** |
| ⑤ **星镖档跳主伤害** | `RuleCore.cs:3463`（`if (!targetDied) { dealt = Hurt(…) }`；`targetDied` 由 `:3335` 的星镖置） | `_ResolveAttack…:616/:632` 那条 2 字节双标志：`:616 *(u16*)(+0x138) = 0x0001` ⇒ `+0x138=1, +0x139=0`；`:632 = 0x0100` ⇒ **`+0x139=1`**，置位条件是 **`EnoughPendingDamageToDieWithDamageValues(param_1 + 0x30 /*目标*/, 星镖伤害表, …)`**；`:863` 的主伤害块闸 = `+0x13a == 0 && +0x139 == 0` | ❌ **不一致**（原版 = 星镖的**逐条预测**，`EnoughPendingDamageToDieWithDamageValues` 只读血/护甲/无敌/闪避，**与残骸无关**） | ⛔ **不能改定义**：排除残骸后我们会**对着一个血 ≤ 0 的目标再打一次主伤害**，而原版**跳过整段**。今天两边**一致（都跳）** |

> 📌 **上表 5 行就是 `targetDied` 的全部读写点**（`grep -n "targetDied" Core/*.cs` 现核：`:3354`/`:3408`/`:3479`(定义)/`:3500`/`:3534`/`:3555`/`:3587`/`:3616`/`:3679` 那几处；
> 写点只有三处：星镖 `:3354`、主伤害 `:3500`、标记光 `:3616`）。

**⇒ 裁定：`targetDied` 的定义【不能动】。** 它在那 5 处都是「**这一下把它打到 ≤0 了**」的意思，
**恰好**与那 5 处原版判据（裸血 < 0 / 血 ≤ 伤害 / `EnoughPendingDamageToDie` / 星镖逐条预测）**都对得上**；
**只有第 6 处用途（震荡那一格）**的原版判据是 `IsInPlay()` 而不是「死了没有」。

---

## 3. 实现形状与理由

**选了「只加一格」**（简报的第二种形状），并把这一格收成**具名判定口**：

```csharp
// RuleCore.cs:3685
var stunVictim = StunStillOnBoard(ctx, tgtP, tgtSlot, target, targetDied);
if (stunVictim != null && attacker.Has("concussion")) { … }
```

`RuleCore.cs:2892` 新增（就在 `A1166` 那段注释正下方）：

```csharp
static UnitState StunStillOnBoard(BattleContext ctx, int tgtP, int tgtSlot, UnitState target,
                                 bool targetDied)
{
    if (target == null) return null;
    // ① 常规：没被打死 ⇒ 就是它自己（判据一个字节都没动，只是从调用点搬进来）
    if (!targetDied && target.IsAlive) return target;
    // ② 被打到 ≤0 —— 唯有它**翻面成残骸**时才还在场上
    var occ = ctx.Players[tgtP].Board[tgtSlot];
    if (occ != null && occ.IsRemnant && ReferenceEquals(occ.Instance, target.Instance)) return occ;
    return null;
}
```

**四条理由：**

1. **原版判据只在那 6 处之一不同**（§2）⇒ 定义不能动，只能在不同的那一处**另给判据**。
   ⚠️ 这是「两处写同一条规则 = 迟早不一致」的**例外**，因为它**本来就是两条不同的规则**（`IsInPlay()` vs 「死了没有」）。
2. 🔑 **判别式没有第二份**：`StunStillOnBoard` **不使用**「会不会翻面」这条预测，而是**读棋盘事实**
   （`Board[tgtSlot].IsRemnant` + **实例相等**）—— 而棋盘事实是 `CleanupDeaths` 那一支**唯一**写出来的东西
   （`IsRemnant = true` 全仓**只有 `RuleCore.cs:4465` 一个写点**，现核 `grep`）⇒
   **「什么算翻面成残骸」这条规则仍然只有一处**，⛔ 不构成第二份判别式（不违反铁律）。
3. 🔴 **必须返回「棋盘上现在占位的那一个」而不是 `target`**：`CleanupDeaths` 翻面时**新建**一个 `UnitState`
   （`rem`，`IsRemnant = true`、`Health = 1`、**Keywords 是空的一份**），旧 `target` 当场作废。
   `UnitState._keywords` 是**每个实例自己一份**（`UnitState.cs:545` `Has` = `_keywords.ContainsKey`）⇒
   把 `stun` 挂在旧对象上 = **对局里完全看不出来**（玩家看不到、断言也看不到）= 静默失败。
   ⇒ 这一条决定了「加一格」不能只是把 `target.IsAlive` 换成 `Board[tgtSlot].IsAlive`（那样会晕到一个**对象不对**的东西上）。
4. **为什么不按格号随便取**：同一批里**别的单位真死**会让那条连续列表**内移**
   （`BoardSlots.RemoveAt` 会搬格、`BoardSlots.cs:210-231`）⇒ 加 **`ReferenceEquals(occ.Instance, target.Instance)`** 挡掉
   「搬进来的是别人」。⚠️ 反转的情形（残骸自己被打死）是安全的 —— `RemoveAt` 会把 `Board[slot]` **置 null**（同一段现读）⇒ 找不到。

**⛔ 明确否掉的两种写法**（写进注释了，防后人改回去）：
- 「加一条『还在棋盘上』（`Board[slot] != null`）」—— **方向反了**（原版会晕、会更少晕），`R3` 已判它不成立；
- 「改成 `Board[tgtSlot] != null && Board[tgtSlot].IsAlive`」—— 同上，而且会**晕到刚搬进来的别人身上**。

---

## 4. 逐处改动清单

| # | 文件:行号 | 改前 → 改后 | 判据 / 为什么 |
|---|---|---|---|
| 1 | `RuleCore.cs:3679`（改后 `:3685`） | `if (!targetDied && target.IsAlive && attacker.Has("concussion"))` → `var stunVictim = StunStillOnBoard(ctx, tgtP, tgtSlot, target, targetDied);` + `if (stunVictim != null && attacker.Has("concussion"))` | `CardScript__CheckIfDead.c:110-147`（残骸支路**不置 `waitingToDie(5)`**）+ `CardScript__Stun.c:44-45`（闸 = `IsInPlay()`）+ FIFO（stun 入队早于翻面入队） |
| 2 | `RuleCore.cs:3713-3725` | 块内 `target.IsStunned / target.AddKeyword / target.StunnedAtStartOfTurn / BroadcastKeywordEvent(…, target)` → `stunVictim.*` | 见 §3 理由 3（挂错对象 = 静默丢） |
| 3 | `RuleCore.cs:3727-3729` | 日志尾加一句：命中残骸档时**出声说明**（「它是刚由 X 翻面成的残骸」） | 原版那一跳会 `BroadcastUnitStunned`，我们**没有那条广播** ⇒ 照本仓「不许静默」用日志顶 |
| 4 | `RuleCore.cs:3705`（`StunBlockedByTraits` 那一行） | **不动代码**，只加注释：**挡词查 `target`（挨打那一刻那张卡）、落点用 `stunVictim`** | 原版这一跳打的是**同一张卡**，而翻面 action 排在它**后面** ⇒ `unstunnable` 该取挨打那一刻的值；今天两种写法结果相同（全池 0 张卡带 `unstunnable`，且残骸不带关键词），照原版写 |
| 5 | `RuleCore.cs:2892-2905` | **新增** `StunStillOnBoard`（判定口 + 长注释：原版判据链 / 为什么不能挂 `target` / 为什么不按格号取） | 同上；同时把「还在不在场上」这件事**收口到一处** |
| 6 | `RuleCore.cs:2732-2740` | `StunBlockedByTraits` 的文档：「见下方 `A1166` 那段（**已判等价**）」→「见 `A1166` 那段 + 判定口 `StunStillOnBoard`（**2026-10-10 起『已判等价』已被更正为『差一档』**）」 | 铁律 5（记错的就地改掉、留更正痕迹） |
| 7 | `RuleCore.cs:2746-2759` | `A1166` 块头的裁定行：「🔴 **判定：在我们这边【等价】—— ⛔ 不新加判据**」→ **就地更正**：差的就是**残骸那一档**，写清**错因**（「血 ≤ 0」≠「这一批必死」） | 同上。**这是本笔改动里最要紧的一条文档更正** |
| 8 | `RuleCore.cs:2813` | 小标题「【**为什么在我们这边等价**】」→「【**当年判「等价」的那条推理**（2026-10-10 更正：它漏了一档，留着好认这个坑）】」 | 那段推理本身有价值（`CardStateOptions` 全表、`IsInPlay` 与 `IsInPlayOrDying` 的对照），**留知识、改结论** |
| 9 | `RuleCore.cs:2829-2851` | 删掉错结论那两句、补上**两条不置 5 的支路**（① 残骸/路标石 —— 本笔；② `survivor` —— 0 张卡今天不可达）；「今天为什么看不出来」后半句更正；「**没查清的那一点**（`CheckIfDead` 与眩晕结算的先后）」→ **已查清**（`CheckIfDead` 更早：`_ReceiveDamage…:310` 在攻击协程里，而 stun 要等主循环 `BattleManager__Update.c:1284-1299` → `NextActionInQueue` → `ResolveAction` `case 0x11:1740`） | 铁律 5 + 5·b（把新查实的写进文档，别再挂「没查清」） |

**没碰的东西**：`targetDied` 的定义（`:3479` 那行**一个字没改**）· `CleanupDeaths` 的残骸条件
（`:4465` 附近，判据与 `StunStillOnBoard` 的观察对象**不重复**，不需要改）· 任何别的 `.cs`。

---

## 5. 断言（`RuleEngine/Editor/RuleEngineTest.cs`，加在 `TestAttackKeywords` 的 ③ 震荡 后面）

⚠️ **只写、没跑**（红线：不跑 Unity）。全部走**现有夹具形状**（`Battle` / `ToP1Turn` / `Place` / `Board` / `LogTail`），
与同一函数里 ③ 那格逐字同构，没有新夹具机制。

| 格 | 行号 | 断言（原文摘要） | 性质 |
|---|---|---|---|
| ③′ | `:4463-4492` | 5 攻 `Concussion` 打死一个带 `Remnant` 的 5 血单位 ⇒ ①`Board(ctx,1,3).IsRemnant` **真**（先确认它真翻面了）②🔴 **`Board(ctx,1,3).IsStunned` 真**（**改之前这里是假**）③🔴 `!killed.IsStunned`（`killed` = `Place` 返回的那一份旧 `UnitState`） | 正例 + **灭自证的一半** |
| ③″ | `:4493-4513` | **同一发攻击**打一个**不带** `Remnant`/`Waystone` 的普通 5 血单位 ⇒ 格位**空**、**敌方场上一个被晕的都没有**（遍历整张棋盘） | **灭自证的另一半**（见下） |
| ③‴ | `:4514-4530` | 同 ③′ 但关键词换 **`Waystone`** ⇒ 同样「翻面**且**被晕」 | **覆盖整个条件**（原版 `HasToTransformIntoRemnant = remnant ∨ waystone`，`SupportMethods__HasToTransformIntoRemnant.c`）—— ⛔ 只修死灵那一半 = 静默偏一半 |

🔴 **灭自证那一对（③′ ② + ③″）为什么成立**：③′ 与 ③″ **只差「目标会不会翻面成残骸」一个变量**，
而两条断言要求**相反**的结果 ⇒
**把这一格改回旧写法（`!targetDied && target.IsAlive`）会让 ③′ 红**；
**放宽成「血 ≤ 0 也晕」会让 ③″ 红**；**把眩晕挂回被丢弃的 `target` 会让 ③′ 的 ②/③ 同时红**。
⇒ 三处（实现 / 「挂谁身上」/ 判别式）**不可能一起改回旧形状还全绿**。
（另有一条**结构性判别式**做兜底：②查的是 `Board(...)` 上**现在占位的那一个对象**，
不是 `killed` —— 两个对象在改前**不相等**，所以「挂在旧对象上」这种改法**过不了 ②**。）

---

## 6. 验证

| 项 | 读数 | 备注 |
|---|---|---|
| 类型检查（运行时程序集） | **`运行时错误数: 0`** | `TMPDIR=/tmp/wf_pd bash d:/4/Unity/工具/typecheck.sh` |
| 类型检查（编辑器程序集） | **`编辑器错误数: 0`** | 同上（断言改完**又跑了一次**，两次都干净；⛔ 无一条错落在别人的文件上） |
| `git diff --numstat` | `RuleCore.cs` **117 / 31** · `RuleEngineTest.cs` **67 / 0** | 6409 行的文件 117/31 ⇒ **行尾没被翻**（整篇翻会是 ~6400 行）；两个文件都是 **LF**（`b.count(b'\r\n') == 0`），改动只用 Edit 工具，⛔ 没用 `sed -i` |
| Unity 自检 | **一条都没跑** | 红线 + 铁律 12（手头待办没做完不跑；主对话在收口时统一跑 `RuleEngineTest.Run`） |

🔴 **请调度台在同步点跑 `RuleEngineTest.Run`**（本笔动的是 `RuleEngine/Core/` ⇒ 按覆盖面判据必须跑这一条）。
⚠️ **我没跑，所以「断言本身能不能过」我一次都没验证过** —— 尤其 ③′ 里「5 攻打死 5 血是否正好
`dealt = 5`、`targetDied` 是否照预期置上」这一串时序是按**读代码**推的，**没实跑**。

---

## 7. 没查清 / 停手的部分

1. ⚠️ **`CardScript__CheckIfDead.c:123` 那个 `+0x65` 是哪个字段** —— `R3` 没查清，**我也没查**。
   语义上它是「非零 ⇒ 走**真死**支路（不翻面）」，所以**不影响本笔**（本笔只用到 `HasToTransformIntoRemnant` 为真且
   它为零的那一支）。⇒ **如实挂着**，要收口得去 `dump.cs` 里按 CardScript 的 `0x60-0x68` 字段区间反查。
2. ⚠️ **`HasToTransformIntoRemnant` 的 `HasCurrentTrait` vs 我们的 `u.Has(...)`** ——
   原版是**运行时 trait**（含 buff/授权），我们读的是 `UnitState._keywords`。
   若某天出现「**运行时**授予 `Remnant`/`Waystone`」的效果，两边的边角可能不同。
   **今天不可达**（全池 `Remnant.` 只出现在 Sautekh 36 张、`Waystone.` 只在 SaimHann 24 张的**印刷词**上）。
   **没查**「有没有效果卡会运行时授予 `remnant`/`waystone`」。
3. ⚠️ **`survivor(430)` 那条支路**（`CheckIfDead.c:152` `UseSurvivor` ⇒ **不置 5、留在场上 ⇒ 原版也会晕**）：
   今天**全池 0 张卡**带它 ⇒ 不可达；我**没做**，也**没查**「有没有效果卡运行时授予 `survivor`」。
   （`R3 §1·6` 也挂着同一条。）⇒ 这是**已知的第二档同类差异**，**不是本笔能收的**（判据为空 ⇒ 铁律 11 例外①），
   **建议调度台立一条「原版有、我们没做、今天 0 张卡」的账**。
4. ⚠️ **反击方向的 `Concussive`**（`R3 §四·3` 的疑点）**我没核** —— 它不在本笔白名单的活儿里。
   （我方 `RuleCore.DeclareAttack` 的震荡只有 `:3685` **一处**，反击那一段 `:3562` 附近没有对应格。）
5. ⚠️ **`stun` 落在残骸上之后的连锁**（`BeginTurn` 的 `IsStunned ⇒ StunnedAtStartOfTurn = true`、
   `EndTurn` 的摘除、`SimpleAI.ScoreStun`）**我一条都没追**。今天看是安全的（残骸 `Exhausted = true`、不能再行动），
   但**没核**。⇒ 若 `RuleEngineTest.Run` 报出残骸相关的红，**先怀疑这里**。
6. ⚠️ **没做实况验证**（红线禁跑 Unity）—— 全部是静态判据；`R3` 那份也是。

---

## 8. 顺手发现（⛔ **一条都没改**）

1. 🔴 **`Slay` / `Kills` 可能也是同一族「把翻面当成死」** —— `RuleCore.cs:3754`：
   `bool killed = !target.IsWarlord && !target.IsAlive;`（**用的是 `target.IsAlive`，不是 `targetDied`**）
   ⇒ 一个**翻面成残骸**的目标会被算成「被摧毁」⇒ 会 `FireTriggerAt(Slay)` + `BroadcastWhen(Kills)`。
   **原版那一侧的线索**：`grep -rn "AddTriggerSlay" decomp_full` **全库只有一个调用点**
   （`AbilityLogic__PlayAbility.c:1903`），而 `BattleManager__ResolveTriggerSlay.c:128` 那道闸判的是
   **`CardScript__IsInPlayOrDying()`（判的是【触发者/凶手】还活着）**，**不是**「目标死了没有」
   ⇒ 原版对「目标真的死了吗」另有一道（大概在 `AbilityTrigger` 的 targetCriteria 那一层）。
   **置信度：中下**（我只读到「原版这一跳不判目标死活」这一步，**没读到**它到底在哪判「击杀成立」）。
   ⇒ **建议立账 + 先做一次只读查证**，⛔ 别照本笔的形状直接改。
2. 🔴 **`EffectResolver.DoStun` 那条路可能是同一族** —— `A1166` 的注释自己写着：那条路的目标走
   `AddSide`（`EffectResolver.cs:1296-1304`），而 `AddSide` **自己就挡 `!u.IsAlive`** ⇒
   「先打死后晕它」（同一段效果里）时，我们**静默不晕**、而原版按 `IsInPlay()` 那一档**会晕**。
   **置信度：低**（我没读 `DoStun` 的调用时序、也没查「有没有卡面是『造成伤害并眩晕同一目标』」）。
   ⇒ **建议立账**，⛔ 别顺手改（它不在 `A1176` 的判据里）。
3. ✅ **`BattleManager__ResolveStun.c` 全库没有直接调用者**（已被内联进 `ResolveAction` 的 `case 0x11`）——
   与 `R3 §四·5` 独立一致（我 grep 的结果相同：只命中它自己的文件）。
4. ✅ **星镖那个「双标志」`+0x138/+0x139` 的读法**（顺手记着，省得下次再猜）：
   `_ResolveAttack…:616` 与 `:632` 用的是 **2 字节写**（`*(undefined2 *)(param_1 + 0x138) = 1` / `= 0x100`）
   ⇒ `+0x139` **不是**独立字段，是那对里的**高字节**：`1` ⇒ (`0x138=1, 0x139=0`)，
   `0x100` ⇒ (`0x138=0, `**`0x139=1`**)。而 `:863` 的主伤害闸读的正是 `+0x13a`（哨戒打死攻方）与 **`+0x139`**
   （`EnoughPendingDamageToDieWithDamageValues(目标, 星镖表)` = **星镖打死目标**）。
5. ⚠️ **简报告诉我的自检文件路径是错的**（`CardPresentation/Editor/RuleEngineTest.cs` 不存在），
   真身在 `RuleEngine/Editor/RuleEngineTest.cs`。**简报里的路径类事实要现核**（这条本仓已经踩过三次）。
