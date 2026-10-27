# B9_W29BDR2 —— `BattleDriver.cs` 三处接线（虫群藏体错对象 / 挨打震屏方向 / `unitsShake`）

> 执行代理 `B9_W29BDR2` · 2026-10-27 · 白名单内改动**只**落在
> `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（+ 本报告 + `账/_parts/B9_W29BDR2.tsv`）。
> `TMPDIR=/tmp/wf_w29 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（2026-10-27 现跑）。
> ⛔ 未跑 Unity（无 `-executeMethod` / 无 `_run_8_checks.sh`）· 未动 git · 未改正本 / 账主表 / 别人的报告。
> 🔴 **行号守恒**：三处中途改动全部是 **1:1 / 12:12 / 6:7+1 的行数对冲**（注释就地压缩）⇒ `wcs -l` 全程 **35,376 行不变**；
> 新成员（3 个）**追加在类尾**（`35,381` 起）⇒ **下游账零漂移**。

---

## 一、结论

| # | 这件活 | 改前 | 改后 | 状态 |
|---|---|---|---|---|
| ① | 虫群合并那一拍的 `ToggleBody3D(false)` | `PlaySwarmMergeFeel` 里 `v.SetBody3DVisible(false)`，而 `v` = **存活那张** ⇒ 3D 档下**存活小兵的体被永久藏掉**（直到下一次 `SetFace(Board)` 重刷） | **那句已删**（⛔ 也没「改藏被并掉那张」——见 §三·1，事件里拿不到那张） | ✅ 真缺陷已修 |
| ② | 命中帧震屏的方向链 | `CardFeel.ShakeCameraForHit`（喂 preset 自带的 `(0,0.2,0.2)`，屏幕上只有竖直分量） | `CardFeel.ShakeCameraForAttack(cam, hudRoot, 受击视图.transform.position, 攻击方近战)`（原版 `SetDirection(目标棋盘坐标)` 那一档） | ✅ 接上（**量级仍待真机核**，见 §三·3） |
| ③ | `unitsShake`（271 条已备好、0 调用点） | 0 调用点 | 两个落点：`ScheduleAttackPresentation` 的攻击件循环 + `PlayPlacedOne`（`play`/`trigger`/`death` 桶） | ✅ 接上（**验收口径**：今天可达的全走**击退支**，不是「抖」，见 §三·2） |

一句话：①**是本轮唯一的行为修复**（3D 档下「活着的小兵没体」）；②③ 是「实现早就写好、只差一行调用」的两处接线。

---

## 二、证据（文件:行号）

### 2·1 ① 的对象是**被并掉那张**（三条反编译互证）

| 证据 | 内容 |
|---|---|
| `decomp_full/CardScript__ExecuteSwarm.c:39-80` | `lVar7 = get_transform(**param_1**)` → 在 `param_1.transform.position / rotation` 上 `Instantiate(customObject)`，`localScale` 也取 `param_1` 的；随后 `GetComponent<SwarmVFXController>().Initialize(**param_1**, uVar9)`（`:63-80`） |
| `decomp_full/SwarmVFXController__Initialize.c:15-26` | `CardMaterialHelper.ConfigureMaterial(renderer材料, **param_2**)` + `lVar1 = *(param_2 + 0x290)`（= `CardUI`）→ `BattleCardUI__ToggleBody3D(lVar1, **0**, …)` ⇒ **藏的是传进来的那张卡** |
| `decomp_full/CardScript.__c__DisplayClass401_0___ExecuteSwarm_b__0.c:23-55` | 闭包 `+0x10` 读 `CurrentMeleeAttack` / `CurrentRangeAttack`、写 `DestroyUnitSilent(+0x10)`；`+0x18` 收 `AddEffect` ⇒ **`+0x10` = 被并掉那张**（与 `RuleCore.cs:4624-4656` 那段既有判据同一口径） |

### 2·2 我们这一跳拿到的是**存活那张**

- `RuleEngine/Core/RuleCore.cs:4702`：`FireTriggerAlways(ctx, **host**, KeywordTable.Swarm, p, **hostSlot**, just)` —— `subject` 是 `just`（被并掉那张）但**格号是 `hostSlot`**。
- `BattleDriver.cs:24798` `PlaySwarmMergeFeel`：`v = ViewAt(e.Player, e.Slot)` ⇒ **存活那张**。
- 自检为什么没抓到：`Editor/BattleScene.cs:43288` 先存 `bodyWasSwarm`、`:43329` 断「被藏掉」、`:43334` **立刻还原** ⇒ 「没人复原」这个真缺陷**结构上照不到**。

### 2·3 ② 原版喂进 `SetDirection` 的是**目标的棋盘坐标**

- `CardFeel.cs:4160-4186`（`ShakeCameraForAttack` 的 doc 与实现）+ `报告/B9_W11SHAKE_震屏方向.md` §2·2 / §六（屏幕通道映射表、`OrigBoardPosOf` 的 +100 换轴）。
- 我们的调用点：`BattleDriver.cs:22664` `PlayHitFrameShake(CardView target)` ← `:22264` `if (feel) PlayHitFrameShake(tgtView);`（`tgtView = ViewAt(e.TargetPlayer, e.TargetSlot)`，`:22159`）。

### 2·4 ③ 原版三处调用点 + 枚举→卡那一跳

| 原版调用点 | 形状 | 我们的对应落点 |
|---|---|---|
| `_ResolveAnimInfoRangeAttack_d__468__MoveNext.c:62-77` | `shk` preset 之后**紧接**一层 `ush` 循环（`if (ush != null) … else if (tweenAnim != null)`），**不带 ranged/melee 闸** | `BattleDriver.cs:22232`（攻击件循环尾） |
| `_ResolveAnim_d__466__MoveNext.c:110-125` | 同上 | `BattleDriver.cs:23863`（`PlayPlacedOne`） |
| `_ResolveTraitAnim_d__465__MoveNext.c:130-145` | 同上 | 同 `PlayPlacedOne`（`trigger` 桶那一族）—— **⚠️ 没单独核「特性动画拍」是否真走这条**，见 §三·2 |

- `BattleManagerSupport__GetTargetUnit.c:13-50` 逐字：`10 ⇒ param_2(acting)` · `0x14(20)` 与 `0x1e(30)` ⇒ `param_3(target)` · `0x28(40) ⇒ GetHero(行动方 isPlayer)` · `0x32(50) ⇒ GetHero(!行动方 isPlayer)` · **其余 `return 0`**。
- 落点：`BattleDriver.cs:35429` `ResolveUnitShakeTargetUnit`（`10⇒actor` / `20,30⇒target` / `40⇒ViewAt(actorSeat, WarlordSlot)` / `50⇒ViewAt(另一侧, WarlordSlot)` / `default⇒null`）。
- 数据：`Resources/cardanim_clips.json` 的 `ush` 列 —— 实测 **257 行 / 271 条**；`t` 分布 = `(0,30) 188` · `(30,10) 35` · `(0,0) 26` · `(0,10) 13` · `(0,40) 6` · `(0,50) 2` · `(10,30) 1` ⇒ **有效 36 条 / 35 个 CardAnim**。

---

## 三、没查清的部分（逐条写「还差什么」）

1. 🔴 **「藏被并掉那张的 3D 体（1.2 s）再销毁」那半拍我们做不到**（不是不想做）。原版：VFX 在**被并掉那张**的位姿上建 → `Initialize` 里 `ToggleBody3D(false)` 藏它的体 → 1.2 s clip 播完 → `OnAnimationEndEvent` 回调 → `DestroyUnitSilent(+0x10)`。我们：`RuleCore.SwarmMergeNow` 在本跳**之前**就 `BoardSlots.RemoveAt` 把那张摘掉，**事件里只剩存活那格的 `Player/Slot`** ⇒ 视图侧拿不到「被并掉那张」的 `CardView`（`host.SwarmUnder` 只留 `CardInstance`、没有实例→视图表）。它离场时的体由离场链**整件收回**（`SyncBoard` 的 `u == null` 支：`Kill(v.gameObject)`，或 `PendingExitFor` 时由阵亡演出收）。**还差什么**：引擎要么把「被并掉那张的格号/实例」带进这条触发器事件（动 `RuleEngine/RuleCore.cs`，不在本件白名单），要么给视图层补一张「实例 → 视图」表。⇒ 如实记为**未复刻的一小段**，⛔ 别拿「藏存活那张」冒充（那正是本轮修掉的缺陷）。
2. 🟡 **`_ResolveTraitAnim_d__465` 那一处调用点没单独核**。我们只给「攻击件循环」与 `PlayPlacedOne`（`play`/`trigger`/`death` 三桶）接了。若「特性动画」走的是 `PlayTraitPulse` 那条独立路（不是 `PlayPlacedPieces`），那一支**今天仍不消费 `ush`**。**为什么先不追**：现读 **有效 36 条全部是远程命中件**（`BulletImpact_*` / `NecronGauss*` / `MegaBlasta*` / `Plasma_*` / `Psychic_Lightning_*` / `Spore Explosion` 一类），trait/play 桶那 222 条 `t==0` 的行**全是空转** ⇒ 今天**零可观测差**。⚠️ 数据若变化（出现 `t≠0` 的 trait 行）需回来补。
3. 🔴 **② 的量级没有真机核过**（沿用 `B9_W11SHAKE` 的悬空项）：按机器码算 `|direction| ≈ 101 × 2/4/8 × 0.149 × 73.1/108` ⇒ 峰值 ≈ **10.2 / 20.4 / 40.8 我们世界单位（屏高 = 10）**，而 preset 那版只有 ≈3 px。若真机看到镜头「飞出去」，**只改 `CardFeel.OrigBoardPosOf` 一行**（⛔ 别动 2/4/8 幅度表）。**还差什么** = 一次真机（原版已关服）。
4. 🟡 **`tweenAnim(+0x80)`（与 `ush` 同一处 if/else 的另一支）仍未做** —— 本件只接了 `ush`。原文残差见账 `特效动画:492`。
5. 🟡 **「可达 28 条」与「数据侧 35 个 CardAnim / 36 条」的差额没重算**：交接简报给的是 28（与卡池求交后），我核的是数据侧 36 条（`(30,10)` 35 条 + `(10,30)` 1 条）⇒ 两个数**口径不同**，我没有重算「哪些 CardAnim 真的有卡用」。不影响的结论（两条口径下都走**击退支**）。
6. 🟡 **`PlayPlacedOne` 里那一路我按「当场求值」（`at = 0`）接**，而它的调用方 `PlayPlacedPieces` 已经把 `it.T + startDelay` 折进 `at` 了（`:23803-23804`）⇒ 两者不重复计时。没真机核过 `at` 非 0 时那条 `ScheduleAction` 的落点是否与原版同拍（差一帧量级）。

---

## 四、改动清单

### 4·1 `Assets/CardPresentation/Battle/BattleDriver.cs`（**行数全程 35,376 不变**，新成员追加类尾）

| # | 位置（改后行号） | 改动 | 行数 |
|---|---|---|---|
| 1 | `:24817-24828` | ②·b 那 12 行**就地重写**：删掉 `v.SetBody3DVisible(false);`（该行变成注释），把「原版对象 = 被并掉那张」的三条反编译链写进注释 | 12 → 12 |
| 2 | `:22653-22667` | `PlayHitFrameShake()` → `PlayHitFrameShake(CardView target)`，体内 `ShakeCameraForHit` → **`ShakeCameraForAttack`**；doc 就地补方向链判据 | 15 → 15 |
| 3 | `:22264` | `if (feel) PlayHitFrameShake();` → `PlayHitFrameShake(tgtView);` | 1 → 1 |
| 4 | `:22214-22222` → `:22214-22219` | `shk` preset 那段注释**压缩 3 行**（判据一条没删） | 9 → 6 |
| 5 | `:22230-22232` | 新增 ③ 的调用：`SchedulePieceUnitShakes(an, atkView, tgtView, e.Player, vfxAt);`（+2 行注释） | 新 3 行（与 #4 对冲） |
| 6 | `:23860-23863` | `PlayPlacedOne` 尾：注释 2 行压成 1 行、新增 `ScheduleUnitShakes(p.UnitShakes, self, target, e.Player, 0f);` | 4 → 4 |
| 7 | `:35377-35440` | 🆕 类尾：`SchedulePieceUnitShakes` / `ScheduleUnitShakes` / `ResolveUnitShakeTargetUnit` + 一段判据总注（含 🔶 自己设计的 `animateFeel` 闸） | 新 64 行（纯类尾） |

**为什么「删」而不是「改藏被并掉那张」**：判据说话 —— 事件里**没有**被并掉那张的任何标识（`RuleCore.cs:4702` 只给 `host/hostSlot`），选项 ⓑ 今天**没有实现载体**；⛔ 也**没有**「藏完再复原」（原版那张卡随后就被销毁，复原它反而更不像）。

### 4·2 账补丁 `Unity/资料/完全复刻/账/_parts/B9_W29BDR2.tsv`（4 行，11 列，LF，首行是表头）

| 键（原版类 / 方法） | 改了什么 |
|---|---|
| `SwarmVFXController` / `Initialize(...)` | 落点改成带行号（`BattleDriver.cs:24798（PlaySwarmMergeFeel）`）；差异写清「③ 的错落点已订正、那句已删、『藏被并掉那张』做不到、『藏体 1.2 s』无等价载体」；备注写清**那条自检断言现在恒红且钉的是错口径** |
| `BattleManager` / `ResolveAnimInfoRangeAttack(...)` | ③unitsShake 从「未接」改成「已接」+ 落点；`tweenAnim` 仍列残差；**状态仍 B**（④`tweenAnim` 未做） |
| `BattleManager` / `ResolveUnitShake(...)` | 落点补 3 个新成员；「仍 0 调用点」改成已接；顺手订正本条里**已过期**的「`PushBackDir` 现在是 `Vector3.up`」（`B9_W11SHAKE` 已改成 `Vector3.zero`）；**状态 B → A** |
| `CardScript` / `ResolveAttackAnimationEffects(...)` | 落点补调用点 `BattleDriver.cs:22664（PlayHitFrameShake）`；「还剩两件」里 ① 消掉、只剩 ② 量级真机核；备注写清 **R10③ 源级断言会红、该怎么改**；**状态仍 B** |

`python -I -X utf8 工具/check_patch_conflicts.py` ⇒ **没有跨补丁重复键（3 份补丁）**。

---

## 五、顺手发现（都能复核；⛔ 都不在本件白名单外改过）

1. 🔴 **`Editor/BattleScene.cs:43329` 那条自检断的是【错口径】，而且它自己也参与了掩盖这个缺陷**：
   `Check(!vSwarm.Body3DVisible, …)` + `:43334` 立刻 `SetBody3DVisible(bodyWasSwarm)` 还原。
   现在那句已删 ⇒ **这条断言必红**。正确改法（由 `BattleScene.cs` 的写手做，本波那文件归 W24）：
   ```csharp
   Check(vSwarm.Body3DVisible, "★ 虫群合并⑤：存活那张的 3D 体**仍然显示**（原版藏的是被并掉那张）"
       + "｜🧨 把 `PlaySwarmMergeFeel` 里 `v.SetBody3DVisible(false)` 加回去 ⇒ 红");
   ```
   并把 `bodyWasSwarm` 与那行还原一起删（前提断言 `Check(bodyWasSwarm, …)` 可留）。这正是 `CLAUDE.md` 那条「自检绿 ≠ 口径对」的又一实例。
2. 🔴 **`Editor/BattleScene.cs` 的 R10③ 是【源级字面串】断言**（`:1826` 找 `"PlayHitFrameShake();"`、`:1866-1869` 要求调用行**不含** `tgtView`/`target`、`:1859` 要求方法体含 `ShakeCameraForHit`）—— ② 一接**三格同时红**。正确改法：
   - `iShake` 的匹配串换成 `"PlayHitFrameShake(tgtView);"`；
   - `shakeCallsShake` 换成找 `"ShakeCameraForAttack"`；
   - 「震屏不依赖受击视图」这条**语义不变**，但判别式要换成「体内是 `target != null ? … : Vector3.zero` 的空转容忍、**且没有** `if (target == null) return;` 早退」。
   ⚠️ 建议顺手把这条断言从「字面串」升级成「查三元表达式」，否则下一次改签名还会假红（同族：`CLAUDE.md` 里「别按文件名判链有没有」）。
3. 🟡 **`CardFeel.cs:3896-3898` 的 doc 仍写着「它现在没有调用者」+「`card_anim_order.json` 没有 `unitsShake` 字段」** —— 两半都已过期（`B9_W12USHK` 补了数据、本件补了调用方）。⛔ 不在本件白名单（`Core/CardFeel.cs` 只调用不改）⇒ 由 `CardFeel.cs` 的写手改。
4. 🟡 **`CardAnimClips.cs:194-196` 的 doc 说「可达 28 条」**，而数据侧是 36 条 / 35 个 CardAnim —— 两个数**口径不同**（前者与卡池求交）。建议在那一行标明口径，免得下个会话拿 36 去对 28 判红。
5. 🟢 **`ScheduleAttackPresentation` 里 `shk` 的计时口径与 `PlayPlacedPieces` 不一致**（前者 `vfxAt + startDelay`、后者 `baseAt + it.T + startDelay`）—— 本次**没动**（`:22219-22220` 的注释明确说了为什么不是 `items[i].T`），但两处放在一起看容易让人以为其中一处写错；`unitsShake` 我按**同件同刻**照 `shk` 那一路（`vfxAt + startDelay`）。
