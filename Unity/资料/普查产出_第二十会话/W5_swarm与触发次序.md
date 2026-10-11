# `W5` —— `A1574`（虫群 `subject`）＋ `A1576`（献祭/幸存者次序）

> 写手：`W5` 笔（2026-10-11，第二十会话）· 独占白名单 = `RuleEngine/Core/{RuleCore,WhenEvent}.cs`（**本笔只动了这两个**）＋ 本报告。
> ⛔ 未动 git 的**写**操作 · 未改任何正本 · 未碰 `RuleEngine/Editor/**` · 未碰 `工具/ruleprobe/**` · 未碰 `d:/2/**`（只读）。
> ✅ 改完**每个 `.cs` 都跑了秒级类型检查**（两次，均 0 错）· 跑了 `ruleprobe check` / `b19test` / 自建离线夹具（见 §三）。

---

## 一、`A1574`（`RuleCore.TrySwarmMerge` 传的 `subject`）

### 一·一 判据：原版把【被并掉那张】当这一跳的 `actingCard`（**我独立复核过，与 `E9` 同向**）

第一权威 = 反编译真身，逐跳读过（行号 = 现读）：

| # | 出处 | 读出的事实 |
|---|---|---|
| 1 | `CardScript__ResolveCardPlayed.c:79` | `AddExecuteSwarm(bm, param_1 /*刚打出那张*/, lVar4 /*= GetAdjacentUnitRight*/, 3, 1)` |
| 2 | `BattleManager__AddExecuteSwarm.c:28-32` | `BattleAction___ctor(…, 0x4f, 0, param_2, …)` + `uStack_200 = param_2`（= 结构体 **+0x8**）· `local_1f8[0] = param_3`（= **+0x10**）——偏移由 `&local_208` 起算、结构体长 `0x100`（`:27` 那段 `FUN_180482360(&local_208,0,0x100)`）⇒ `+0x8` = 打出的卡、`+0x10` = 右邻 |
| 3 | `BattleManager__ResolveExecuteSwarm.c:81/87/90-95` | `+0x28`（= 拷贝基准 `lVar13+0x18` 再加 `0x10`）先 `IsInPlay`、`+0x20`（= 拷贝 +0x8）后判，然后 `CardScript__ExecuteSwarm(param_1 = lVar13+0x20, param_2 = lVar13+0x28, cb)` ⇒ **`ExecuteSwarm.param_1` = 刚打出那张 · `param_2` = 右邻** |
| 4 | `CardScript.__c__DisplayClass401_0___ExecuteSwarm_b__0.c:45 / 49-51 / 52-55` | 闭包 `+0x18` 收数值抽屉（`:45 AddEffect`）· `+0x10` 先 `DestroyUnitSilent`（`:51`）**再**当广播的第 2 个实参（`:54-55 BroadcastUnitSwarm(bm, 闭包+0x10, 闭包+0x18)`）⇒ **`+0x10` = 被并掉那张（打出的那张）· `+0x18` = 留下那张** |
| 5 | `BattleManagerSupport__BroadcastUnitSwarm.c:44` | `CardScript__TriggeredSwarm(卡, param_2, param_3)` —— 棋盘那一跳；机器码 `0x1809BA26C mov rdx, rbp`（`rbp` = 入口 `mov rbp, rdx` = `param_2`）⇒ **自排除比的是 `param_2` = 被并掉那张** |
| 6 | `CardScript__TriggeredSwarm.c:7` | `RawCardScript__OnTrigger(…, param_1 /*thisCard*/, param_2, param_3, …)`；形参名出处 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:373`：`OnTrigger(trigger, manager, thisCard, **actingCard**, **targetCard**)` ⇒ **`actingCard` = 被并掉那张 · `targetCard` = 留下那张** |

🔴 **「it」绑的是 `actingCard`**（**成对证据，不是靠猜**）—— 同族的 `ResolveUnitAttacked`
（`CardScript__ResolveUnitAttacked.c:28`：`OnTrigger(0x32, bm, thisCard, actingCard = 攻方, targetCard = 被打者)`）
底下就有**两张真卡**用**两个不同的词**写同一件事，正好把两个卡槽分开：

* `Doomstalker`（`cards_engine.json`）：`When an enemy attacks, deal 2-3 damage to **it**` → **`it` = 攻方 = `actingCard`**
* `Valtus`：`deal 3 damage to **the target of the attack**` → **`targetCard`**

⇒ 虫群这一句 `When a friendly unit triggers Swarm, give **it** +1 …` 里的「it」= `actingCard` = **被并掉那张**。
⇒ **我们原来传 `host`（留下那张）确实是错的**（`E9` 的结论成立，本笔复算同）。

**旁证（第二来源，⛔ 不当判据）**：粉丝实体规则书 `资料/规则书/…中文翻译.md:216`「打出在同名部队**左侧**时：
合并（**置于其下**，攻击生命相加）」—— 与「打出的那张被吸收」自洽。

### 一·二 改法（`RuleCore.cs`）

| 落点（改后现读） | 改了什么 |
|---|---|
| `RuleCore.cs:2770` | `FireTriggerAlways(ctx, host, KeywordTable.Swarm, p, hostSlot, **just**)` —— 新增第 6 个实参 |
| `RuleCore.cs:7236-7248` | `FireTriggerAt(…, UnitState chosen = null, **UnitState subject = null**)` —— 新增可选形参 + `<param>` 说明（**默认 `null` ⇒ 等价于旧行为**，既有调用点一个都不用改） |
| `RuleCore.cs:7310-7312` | `FireTriggerAt` 尾部广播：`… owner, u.Card, **subject ?? u**` |
| `RuleCore.cs:7341-7347` | `FireTriggerAlways(…, **UnitState subject = null**)`（doc 里写明「两条路要用同一个值」） |
| `RuleCore.cs:7357` | `FireTriggerAlways` 没正文那一支的广播：`…, **subject ?? u**` |
| `RuleCore.cs:7360` | `FireTriggerAlways` 转发：`FireTriggerAt(ctx, u, keyword, owner, slot, **null, subject**)` |
| `RuleCore.cs:2698-2748` | `TrySwarmMerge` 尾部**那段判据注释整段重写** —— 原文写着「触发者是**合并后还活着的那一个**」，是**反的**，就地订正（铁律 5）并把上面 §一·一 的六条出处写进去 |
| `WhenEvent.cs:1355-1409` | `SelfExcludesOnKeywordTrigger` 的说明里，原来那条「**`swarm` 归在「不自排除」那一边，因为比的是被并掉那张、而我们传的是留下那张**」—— 那是**拿我们自己的旧 bug 当判据**（典型自证）⇒ 改成「**`swarm` 也自排除，照抄即可**」 |
| `WhenEvent.cs:1342`（表格）/ `:1406-1408`（名单） | `KeywordTriggerIncludesSelf`：`{ferocity, duty, synapse, **swarm**}` → `{ferocity, duty, synapse}`；表格里 `swarm` 那一格的「⚠️ 见下」→「✅（比的是**被并掉那张**）」 |

🔴 **`⛔ 别只改一半`**：`subject` 只在 `FireTriggerAlways` 那一支生效、`FireTriggerAt` 那一支仍用 `u` 的话，
就是「两处写同一条规则 ⇒ 迟早不一致」（铁律）⇒ 两条路**都**接上了同一个形参。

**行为面（如实标着）**：

* 今天 `swarm` **不在** `CardDef.RoutableTriggers` 里 ⇒ 这一跳**必走「没正文」那一支**（既有注释已写）。
* `EvtKind.Trigger` 的 `owner`/`slot` **仍传 `host` 的格号**（表现层拿格号反查**还活着**的视图 ——
  代码里记着「写死 `right` / 写死 `slot` 各错过一次」）⇒ **`TestSwarm` 那条「事件格位 = 3」不受影响**。
* 把 `swarm` 移出「不自排除」名单**今天不可观测**：自排除比的是「监听者 == 触发源」，而修改后触发源
  是 `just`，它**在广播前就已经 `BoardSlots.RemoveAt` 出场** ⇒ 从来不在 `BroadcastWhen` 的快照里。
  这一条是**口径对齐**（让守卫与 `param_2` 比同一个单位），**不是行为变更**。

### 一·三 断言（★ 会红 —— 见 §三 的变异实跑）

```csharp
// ★ A1574：合并那一跳的「it」= **被并掉那张**，不是留下那张
Check(host.Attack, 4, "★ A1574：留下那张 = 2 + 2 = 4，**没有**再多 1 点｜🧨 把 FireTriggerAlways(…, just) 的 just 去掉 ⇒ 实得 5");
// 附带·单元级（钉 WhenEvent 那张表）：监听者 == 触发源 ⇒ Matches 必须 false
CheckTrue(!WhenEvents.Matches(evSwarm, 0, 0, swarmerA, host, host),
          "★ A1574 附带：swarm 这一族自排除｜🧨 把 \"swarm\" 加回 KeywordTriggerIncludesSelf ⇒ 实得 true");
CheckTrue(WhenEvents.Matches(evSwarm, 0, 0, swarmerA, sub, host),
          "★ 同源对照：换个 listenerUnit ⇒ true｜🔑 与上一行结构上不可能同时满足");
```

---

## 二、`A1576`（`FireTriggerAlways`(献祭) 排在 `UseSurvivor` **之前**）

### 二·一 🔴 **结论：原版次序与我们**现在**逐行相同 ⇒ 本笔不是偏离、无需改动**

**原始证据（`d:/2/tools/decomp_full/CardScript__CheckIfDead.c`，现读的原文片段）**：

```c
if (iVar3 < 1) {                                  // :110  生命 < 1
  iVar3 = *(int *)(param_1 + 0x228);
  if (((iVar3 == 2) || (iVar3 == 0xf)) || (iVar3 == 3 || iVar3 == 0x11)) {   // = IsInPlay()
    iVar3 = EntityScript__get_CurrentSurvivor(param_1,0);
    if (iVar3 < 1) { …真死 / 变残骸那两支（各自 return）… }        // :113-:151
    cVar2 = EntityScript__HasCurrentTrait(param_1,0x1d6,0);        // :152  sacrifice
    if (cVar2 != '\0') {
      cVar2 = BattleManager__IsPlayerTurn(*(longlong *)(param_1 + 600),0);
      if (cVar2 == *(char *)(param_1 + 0x40)) {
        CardScript__TriggerSacrifice(param_1,0);                    // :157  ★ 广播在这里
      }
    }
    …CurrentSurvivor/卡名 的 LogWarning…                            // :160-164
    CardScript__UseSurvivor(param_1,0);                             // :165  ★ 治疗在这里
  }
}
```

* `:157` → `CardScript__TriggerSacrifice.c`：`:19` `DisplayTriggerAnim`（**无条件**）·
  `:41-43` `RawCardScript.OnTrigger(0xdc, …, self, self, self)`（**自己那条 `Sacrifice:` 正文**）·
  `:45` `BattleManager__BroadcastSacrificeResolved(bm, self)`
  → `BattleManager__BroadcastSacrificeResolved.c` 全文一句：**同步**调 `BroadcastUnitUsedSacrifice`。
* `:165` → `CardScript__UseSurvivor.c`：`:73-75` `OnTrigger(0x1a4 = Survivor)` · `:87` `BroadcastUnitHealed`。

⇒ **原版次序 = 献祭广播（含 `triggers:sacrifice` 那一族） → LogWarning → 治疗（`UseSurvivor`）**。
我们的 `CleanupDeaths` 是**同一序**：
`RuleCore.cs:5603`（`FireTriggerAlways(…, Sacrifice, …)`，在 `u.Survivor >= 1` 那一支内、`ctx.Active == p` 与之同条件）
→ `:5607`（`u.UseSurvivor()`）→ `:5627`（`FireTriggerAlways(…, Survivor, …)`）→ `:5631`（`EmitUnit(Hit, -healed)`）。

🔴 **所以简报里那句「若原版先扣血再广播，我们的次序就是真偏离」的答案是：原版没有「先扣血」这一步** ——
扣血（`ApplyDamage`）早就发生过了，`CheckIfDead` 只是在**扣完血之后**判「要不要献祭 / 要不要治疗」；
**广播排在治疗之前**，与我们所做一致。⇒ **本笔【不改代码】**。

### 二·二 那「端到端断不出来」的真正原因（`E12` 的实测已定性，本笔补上机器码侧的判据）

`E12` 实测：删掉 `A1562` 那条守卫，`sacrifice` 那格的端到端**照样是 0**，根因写的是
「触发源在广播那一刻已经不在活体名单里（`BroadcastWhen` 先按 `u.IsAlive` 筛）」。**本笔复核后同意，并补两点**：

1. **原版棋盘那一跳的筛选谓词不是「活着」、是「在场」**：`BroadcastUnitUsedSacrifice.c:29-34` 的形状是
   `IsInPlay(卡)` → `op_Inequality(卡, 被献祭那张)` → 派发。`CardScript__IsInPlay.c` 全 10 条指令只看
   `card + 0x228` 那个**状态字**（∈ `{2, 0xf, 3, 0x11}`），**完全不看生命** ⇒ 一张刚被打到 ≤0、
   还在 `CheckIfDead` 里没走完的卡，在原版里**仍然是被枚举的听众**（能不能响交给守卫判）。
2. **但对我们这一格没有可观测差异**：原版能在「≤0 但在场」这个窗口里被枚举到的卡，
   **就是正在跑 `CheckIfDead` 的那一张**（别的人要么活着、要么已经置 state 5 = 不在场）。
   而那张正是 `BroadcastUnitUsedSacrifice` 用 `op_Inequality` **排除掉**的那一张
   ⇒ 「自己听自己」在原版也是 **0**。⇒ **两端同结果**，只是机制不同（原版=守卫，我们=`IsAlive` 筛）。
   ⚠️ 严格说这是**两处不同的实现**、在**别的广播**上可能分叉（见 §五·2），本笔**只报不改**（判据不足：要动 `EffectResolver` 的共用筛，射程远大于本笔）。

### 二·三 断言（★ 会红 —— 钉的是原版 `:157`/`:165` 的先后）

```csharp
// ★ A1576【次序钉子】献祭的**广播**排在 UseSurvivor（治疗）**之前**（原版 CheckIfDead.c:157 vs :165）
int lSac = LogIdx(ctx, "触发 SACRIFICE"), lSur = LogIdx(ctx, "Survivor");
CheckTrue(lSac >= 0 && lSur >= 0 && lSac < lSur,
          "★ A1576：献祭广播 → 治疗｜🧨 把那句 FireTriggerAlways(…, Sacrifice, …) 挪到 u.UseSurvivor() 之后 ⇒ 本行红");
// ★ 事件流上的同一件事：Trigger(sacrifice) → Trigger(survivor) → Hit(负数=治疗)
CheckTrue(iSac < iSur, "★ A1576：献祭的触发排在幸存者的触发之前");
CheckTrue(iSur < iHeal, "★ A1576：治疗事件排在 Trigger(survivor) 之后（UseSurvivor.c:73-75 vs :87）");
```

⚠️ **两条观测口的边界**（与 `MEMORY.md`「别把被测代码自己会打印的东西当断言口径」对齐）：
被测的是**两件事谁先发生**（`FireWhen` 的返回值不外露、`UseSurvivor` 也没有独立信号），
日志/信号是**唯一**能看出「谁先」的口；而且这里参加比较的两行来自**两个互相独立的打印点**
（`FireTriggerAt` 的 `ctx.Log` ↔ `CleanupDeaths` 的幸存者段），**不是同一处打印两遍**。

---

## 三、验了什么（命令 + 原始读数）

| # | 命令 | 读数（原文） | 判读 |
|---|---|---|---|
| 1 | `TMPDIR=/tmp/wf_w5 bash 工具/typecheck.sh`（改完两轮，各一次） | `运行时错误数: 0` · `编辑器错误数: 0` | ✅ 编得过 |
| 2 | `bash 工具/ruleprobe.sh check`（23:00，**改完；但工作区里有别的写手正在改解析侧**） | `哨兵合计：失败 0 条` · `全池 1120 张 · 解析差异 1 行`（`SOR6\|Righteous Repugnance`）· rc=1 | 差异在 **SOR6 第二条 op 的目标**：基线 = `(未写目标：…)`+`Subjectless=1`，当时 = `your units`+`Subjectless=0` ⇒ 那是**跨句承接**（`A1428`）那一类的**解析侧**差异，**不是本笔**（本笔没碰解析路径：`Program.cs` 全程只调一次 `RuleCore.PlayerChooseOps`，那不是我改的函数） |
| 3 | `bash 工具/ruleprobe.sh check`（23:05，**同一份源码、只隔 5 分钟**） | `哨兵合计：失败 0 条` · **`解析差异 0 行`** · `改名 0` · `新增 0` · `消失 0` · rc=0 | ✅ 绿。⇒ **反证 #2 那一行是别人半成品时的瞬时读数**（若是我改的，同一份代码不会自己变绿） |
| 4 | `bash 工具/ruleprobe.sh b19test` | `==> PASS 45 · FAIL 0` · rc=0 | ✅ 45 条 B19 断言全过（含虫群/触发那几族） |
| 5 | **自建离线夹具** `D:/tmp/wf_w5/verify/`（⛔ 不进工程 · 只 `Compile Include` 工程的 `Core/*.cs`；照 `E12` 的 `wf_a1562` 形状） | **`== 合计 23 通过 / 0 失败 ==`** rc=0 | ✅ 正跑 |
| 5a | 变异 `M1`：`TrySwarmMerge` 去掉 `just`（= 改前世界） | **`16 通过 / 1 失败`**，红的**只有** `host.Attack 期望 4 实得 5` | ✅ A1574 那条★断言**真有判别力** |
| 5b | 变异 `M2`：把 `FireTriggerAlways(…, Sacrifice, …)` 挪到 `FireTriggerAlways(…, Survivor, …)` 之后 | **`18 通过 / 2 失败`**，红的**只有**那两条次序断言（`iSac=3 > iSur=2` · `lSac=16 > lSur=15`） | ✅ A1576 的两条次序断言**真有判别力** |
| 5c | 变异 `M3`：把 `"swarm"` 加回 `KeywordTriggerIncludesSelf` | **`22 通过 / 1 失败`**，红的**只有**单元级那条 | ✅ 口径那条真有判别力 |
| 6 | 行尾 | `RuleCore.cs` `CRLF 0 / LF 7885` · `WhenEvent.cs` `CRLF 0 / LF 1466`（改前 7822 / 1453） | ✅ **纯 LF、没被翻**（三个变异**全部已还原**，文件里 `grep MUTANT` = 0 命中） |

📁 **断言代码落点**（可直接逐字粘贴）：
`D:/tmp/wf_w5/verify/W5Tests.cs` 的 `TestA1574SwarmSubject()` / `TestA1576SacrificeOrder()` ——
宿主 helper 的**签名与工程里那份一致**（`Check` / `CheckTrue` / `CheckCode` / `Unit` / `Tactic` / `Place` /
`ProbeBattle` / `PassTurn` / `ToP1Turn` / `HandIdx` / `Board` / `SlotOf`），**逐字粘回 `RuleEngineTest.cs` 即可**。
（`LogCount` / `LogIdx` / `SignalIdx` 是本笔新加的三个小 helper，⚠️ `LogCount` 与 `E12` 的 `A1562LogCount` 同形，
落地时**合并成一个**，别留两份。）

🔴 **为什么没有直接写进 `RuleEngineTest*.cs`**：本笔白名单**⛔ 明确列了** `RuleEngine/Editor/**`
（理由 = 写手 `W4` 正在改 `RuleEngineTest*.cs`，含 `_Diag` 产物），而验收标准①又要求写在那两个文件里
—— **两条指令打架**。按**铁律 13·3①「一个文件同一时刻只有一个写手」**取**白名单优先**，
断言改成「**离线跑通 + 逐字可粘**」交付。⇒ **请调度台把 §一·三 / §二·三 那几行转给 `W4`（或改完 `Editor` 后自己贴）**。

---

## 四、没查清（如实列，⛔ 没猜）

1. 🔴 **`Tyranid Prime` 那半句的效果落点，本地判不了**。
   卡面（`cards_engine.json` `TL22`）：`Synapse. When a friendly unit triggers Swarm, give it +1 [Melee] and +1 Health`。
   按 §一·一 的判据，「it」= `actingCard` = **被并掉那张**；而那张在**广播之前**就被 `DestroyUnitSilent`，
   我们引擎里也已经被 `BoardSlots.RemoveAt` 出场（只在 `host.SwarmUnder` 留一份 `CardInstance`，
   **没有任何代码从它重算数值**）⇒ **这半句的效果我们这边不可观测**。
   ⚠️ 要判「原版那张卡的 ability 到底把 `it` 绑在哪个 `TargetsAffected`」，需要**卡能力数据**
   （`CardAbility.actingCardCriteria` / `targetCriteria` / `AbilityLogic.targetCriteria`，
   形参名出处 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardAbility.cs:12-20`），
   而**卡能力数据不在本地**（`assets_full/` 91 个 bundle 里没有卡能力 SO；本工程一贯记着「卡数据在远端 CCD」，
   手牌注入也正是这么失败的）⇒ **判据在远端**。
   ⛔ **没猜**：本笔按**能读到的第一条判据**（`OnTrigger` 的具名形参 + `Doomstalker`/`Valtus` 成对证据）落成
   「`it` = `actingCard`」，并**在代码注释里把这个未决点写明了**。
2. ⚠️ **`TriggeredCodex = 650` 有守卫、`TriggeredSwarm = 620` 没守卫（接收方）** —— 原版为什么这么分，
   **没查清**（`E9` §五·2 也标着同一条）。本笔只按「有/没有」落，没去猜设计意图。
3. ⚠️ **`BroadcastWhen` 的听众筛（`u.IsAlive`）与原版 `IsInPlay()` 是两个谓词**（§二·二·1）。
   今天**能把两者分开**的场景我**只找到「正在跑 `CheckIfDead` 的那一张」这一个**，
   而它恰好被守卫排除 ⇒ 端到端同结果。**别的广播上会不会分叉、要不要统一口径 ⇒ 没查清**（射程远，见 §五·2）。

---

## 五、顺手发现（⛔ 只报不改，请调度台分流）

1. 🔴 **`RuleCore.cs` 的 `git diff --numstat` = `169 / 7`，其中约 69 行才是本笔加的** ——
   工作区里这个文件**开工前就带着别人的未提交改动**（`A1356` 那批 `FireTriggerAlways` 收口等）。
   ⚠️ 若**此刻仍有写手在动 `RuleCore.cs`**，我们就是**撞车**（本笔五次 `Edit` 的锚点**全部命中**、
   没有被覆盖的迹象，但请调度台按铁律 13·3① 核一次文件所有权）。
   同批：`Core/CardDef.cs` `70/0`（`A1452`/`A1453` 的信仰收集）· `Core/EffectResolver.cs` `104/9` ·
   `Core/EffectText.cs` `9/1` —— **都不是本笔**。
2. 🔴 **新账（建议立）**：`BroadcastWhen` 的棋盘听众筛 = `u.IsAlive`，原版 = `CardScript.IsInPlay()`
   （`CardScript__IsInPlay.c` 只看状态字、**不看生命**）⇒ **两处口径不同**。
   今天可观测面 ≈ 0（§二·二·2），但它是**共用件**（所有广播都经它）⇒ 真要改是**射程很远**的一笔，
   判据现在**齐**（`IsInPlay` 的 4 个状态值已读出），**建议单开一笔、由调度台决定先后**。
3. ⚠️ **`工具/ruleprobe/基线/out_baseline.txt` 在 SOR6 那一行上曾与工作区不符**（§三 #2/#3）：
   23:00 报 `解析差异 1 行`、23:05 同一份源码报 `0 行` ⇒ **是别人半成品时的瞬时读数**。
   ⛔ 提醒：`check` 的这条读数**会被并行写手污染**（构建会把别人正在写的 `.cs` 一起编进去）——
   见到非零差异**先看差异行是哪一类**（解析侧 / 目标列），再决定「修基线」还是「修解析器」。
4. ⚠️ **`E12` 那条「判别力不足」的标注，根因描述可以更准**（本条只是措辞）：
   它写「触发源在广播那一刻**已经被移出活体名单**」—— 更准确的说法是
   「**我们的筛是 `IsAlive`、原版的筛是 `IsInPlay`；这一格里两者恰好同结果**」（§二·二）。
5. ✅ **好消息**：`A1574` 的改法与 `TestSwarm`（`RuleEngineTest.cs:13957`）**不冲突** ——
   那一组夹具**没有监听者**，且 `EvtKind.Trigger` 的格位仍传 `hostSlot` ⇒ 它那三条断言（含「格位 = 3」）**不该动**。
   （本笔没法跑 Unity 验证这一句，属**推断**，请与 `Editor` 侧那条断言一起跑一次确认。）

---

## 六、改了什么（一表看完）

| 文件 | 现读行 | 内容 |
|---|---|---|
| `RuleEngine/Core/RuleCore.cs` | `:2698-2748` · `:2770` | `TrySwarmMerge` 尾：判据注释整段重写（**订正「触发者是留下那张」那句错的**）+ `subject` 传 `just` |
| 同上 | `:7236-7248` · `:7310-7312` | `FireTriggerAt` 新增可选 `subject` + 尾部广播用它 |
| 同上 | `:7341-7360` | `FireTriggerAlways` 新增可选 `subject` + 两支都用它 |
| `RuleEngine/Core/WhenEvent.cs` | `:1342` · `:1355-1409` | 表格 `swarm` 那格改 ✅ · 「不自排除」那条理由重写（**原理由 = 拿我们的旧 bug 当判据**）· `KeywordTriggerIncludesSelf` 去掉 `"swarm"` |
| 本报告 | `资料/普查产出_第二十会话/W5_swarm与触发次序.md` | —— |
| 离线夹具（**工程外**） | `D:/tmp/wf_w5/verify/{verify.csproj,Entry.cs,Stub.cs,W5Tests.cs}` | 断言代码正本（可逐字粘） |
