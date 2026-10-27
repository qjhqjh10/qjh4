# B9 · D6NB —— `NetBattleTest` 第 9 步两端分叉（只读诊断）

> 诊断代理 `D6NB`，2026-10-27。**只读**：没改任何 `Assets/**/*.cs` / 账 / 正本，没跑 Unity，没动 git。
> 输入：`_tmp_view/netbattle.log`（10-27 08:15，`通过 243 · 失败 5`，`netbattle.log:111401`）。
> 上一轮的定性见 `资料/完全复刻/报告/B9_D4NT_NetBattle重放不确定.md`（那一轮的日志 = 07:47 那跑，**已被 08:15 这跑覆盖，`_tmp_view/` 里没有副本**）。

---

## 〇、一句话结论

**不是"主机 Apply 了却没发出去"，也不是 `CaptureLocalAnswers` 污染了 net 账。**
是「**本机试了一条被引擎拒的动作** ⇒ 引擎在**拒的同时**往 `ctx.Events` 写了 **4 行日志**」，
而 `NetProtocol.Fingerprint` **把 `ctx.Events.Count` 算进指纹**、被拒那条**不广播**
⇒ 主机 55 条事件 / 客机 51 条 ⇒ **局面完全相同、只差这 4 行日志**，指纹差 3844。

**算得死**：`Fingerprint` 里 `Events.Count` 后面还乘两次 31（`PlayerHash(0)`、`PlayerHash(1)`）
⇒ 4 × 31² = **3844**，而实测两个指纹之差 `-1693663583 − (-1693667427) = 3844`，
**一字不差**。⇒ **局面哈希（两个 `PlayerHash`）完全相等**，分叉**只**在事件条数。

---

## ① 分叉的直接原因（`文件:行`）

### 1·1 事件链（谁写了那 4 行）

第 9 步主机的候选表里 `ActiveAbility @5 [oath]` 被试了一次就拒（`netbattle.log:98750`
  是全日志**唯一**一条「本机这条被拒了」，紧接着就退到 `EndTurn` 兜底 ⇒ 它是这一步唯一的非 `EndTurn` 候选）：
`Firestrike Servo-Turret`（`UM92`，`desc` = `Oath 3: Gain Sentry 2`，`oath` trait 值 = 3）**付不起 3 点能量**。
`NetApply.Apply` → `RuleCore.UseOathAbility` 走的正是"被拒但已经写了日志"那条路：

| # | 谁写的 | 行 | 内容（`ctx.Log` ⇒ `ctx.Events.Add`） |
| --- | --- | --- | --- |
| 1 | `EffectResolver.ResolveOneCore` 付费支 | `EffectResolver.cs:894` | `「Oath 3」需要 3 点能量才激活，不够 —— 这一条没生效` |
| 2 | `Auras.Recompose`（`ResolveOathAbility` 收尾那次） | `Aura.cs:730` | `光环（誓约）：… 誓约每回合上限 3 次 → 授予 N 个友方部队` |
| 3 | `EffectResolver.ResolveOathAbility` 收尾 | `EffectResolver.cs:9062-9068` | `Firestrike Servo-Turret 的誓约能力**没有生效**（见上面那行原因）——次数不消耗` |
| 4 | `RuleCore.UseOathAbilityCore` | `RuleCore.cs:11722` | `（这次激活没有生效 —— 次数没有消耗）` |

第 3、4 条**在日志里能直接看到**（`netbattle.log` 的 `[主机] 共 55 条事件，最后 8 条` 的第 48、49 条）。
第 1、2 条被 `DumpTail` 的「只打最后 8 条」截掉了 —— **条数由哈希算术证明 = 恰好 4**（见 〇）。
（`Aura.cs:730` 那行只在盘面上存在 `OathTripleActivation` / `OathInAllTurns` 来源时才打；
 这里必然是它，因为 1 + 1（`EffectResolver.cs:894`）+ 1 + 1 之外没有别的 `ctx.Log` 在这条路上 ——
 `ChargePaidGroups` 的「一并跳过」那行要求本组**还有** `CostShared` 跟随条，`UM92` 只有 1 条 op ⇒ **不打**。
 详见 §④「查不清的」第 1 条。）

`ctx.Log` 的落点：`BattleContext.cs:1855-1859` —— `Events.Add(message)`，**只有 2000 条上限、没有任何 drain**。

### 1·2 传上网络的那一段（谁把它变成"分叉"）

`NetProtocol.Fingerprint`（`NetProtocol.cs:555-569`）：

```csharp
h = h * 31 + ctx.Turn;
h = h * 31 + ctx.Active;
h = h * 31 + ctx.Winner;
h = h * 31 + ctx.Events.Count;      // ← 就是这一行（NetProtocol.cs:564）
h = h * 31 + PlayerHash(ctx, 0);
h = h * 31 + PlayerHash(ctx, 1);
```

而**被拒的那条动作一个字节都没广播**（`Editor/NetBattleTest.cs:395-403`：只有 `rc == OK` 才 `break`，
  被拒就换下一条；`NetBattle.OnLocalAction` 只在 `:413` 被调一次、给的是 `EndTurn`）
⇒ 客机从来没跑过那次失败的 `UseOathAbility` ⇒ 客机 `Events.Count = 51`、主机 `55`。
`NetBattle` 侧的对账点在 `NetBattle.SendFingerprint() :1377-1389` / `case NetKind.Hash :1590-1600`。

### 1·3 第 9 步这一手本身是**对的**

- 主机第 9 步 = `主机动作 #13 已广播（EndTurn）`（`netbattle.log:98761`），返回 `OK`；
- 客机收到 `Applied` 落地，`★ 权威动作流的条数两端一样（实际 14，应为 14）` 绿（`netbattle.log:98950`）；
- 两端终局的 `P0/P1` 四行（血/能量/手/库/弃）**逐字相同**（`netbattle.log:98780-98870` 两个 `DumpTail`）。

⇒ **动作流没有丢、局面没有错**，错的只有"诊断日志条数"。

### 1·4 5 条红是一个根因

| # | 断言（`netbattle.log` 行） | 为什么红 |
| --- | --- | --- |
| 1 | `第 9 步两端分叉`（:98872） | 上面这条 |
| 2 | `★ 打完两端状态指纹一致`（:98896） | 同上（`Fingerprint` 含 `Events.Count`） |
| 3 | `两端事件条数一致`（:98936） | 直接就是 51 vs 55 |
| 4 | `★ 重放之后客机的状态追平了主机`（:99479） | 客机 `Rebuild + 重放 14 条动作` 只会产出 51 条事件（那 4 行**不在动作流里**）⇒ 永远追不上主机的 55 |
| 5 | `★ 主机掉线这一支：重连后客机也追平了主机`（:100019） | 同 4，另一条重连支路 |

⇒ **修掉事件污染，这 5 条一起绿**（另：`★ 重放出来的状态与掉线前一致` 现在**是绿的**，
 因为它比的是"客机重放后 vs 客机掉线前"，两边都 51 ⇒ 相等。这条绿是"两边都缺同一段"的绿，别当证据）。

---

## ② 定性

**（γ）触发 · （β）根因 —— 不是（α）。**

1. **夹具这一改是对的，⛔ 别退回去。** `if (code == RuleCodes.OK) _net.OnLocalAction(act)` 正是生产口径
   （`BattleDriver.cs:308`：`if (_net != null && code == RuleCodes.OK) _net.OnLocalAction(act);`）。
   上一轮"被拒也照样广播"才是与生产不一致。
2. **根因是 (β)：引擎"拒一条动作"时**会**先往 `ctx.Events` 写若干行**，而 `ctx.Events.Count` 是联机指纹的一维。
   ⇒ 这不是这一轮才有的缺陷：上一轮（07:47）它被"两边都广播了同一条被拒动作"**抵消**掉了
   （两边各 +4 行 ⇒ 条数相等），真正报红的是 `AbortWithVictory` 那条**另一层**的问题（见 D4NT 报告）。
   这一轮把那一层修好了，**下面这条就一直露着**。
3. **还有生产可达性**（这条最重要，见 §④ 第 2 条）：它不是"夹具才会遇到"的形状 ——
   `BattleDriver` 里点一个**付不起的誓约/主动技能**就会走到同一条路。
4. 不是 (α)：`两端事件条数一致` 这条断言不 stale —— 它要钉的"两端引擎记账条数相同"是**真需求**
   （见 §①·2 的推理：`Events` 在生产里**没有任何 drain**，`Events.Count` 只在"两端执行了同一批动作"时才相等）。

### 顺手排除掉的三条嫌疑（都**否定**）

| 嫌疑 | 结论 | 证据 |
| --- | --- | --- |
| `CaptureLocalAnswers` 有副作用、污染了 net 账 | **无副作用，否定** | `NetBattle.cs:1154-1158`：**只**写 `_pendingPicks` / `_pendingPickIds` 两个字段（`ToArray()` 读 `ctx`，**不清空** `ctx.ChoosePicks`）；不写队列、不写计数器、不发包。而且被拒那条留下的值会被 `NetBattleTest.cs:408` 那次（`EndTurn` 分支）**覆盖掉**，随后 `OnLocalAction :413` 取的是 `EndTurn` 那一刻的答案 |
| 主机本地 Apply 了但没发出去 | **否定** | 第 9 步发的是 `EndTurn`（`netbattle.log:98761`），客机收到并落地；`权威动作流的条数两端一样（14/14）` 绿；重连时"发过去/重放"都是 **14 条**（`netbattle.log:99219/99234/99249`） |
| 两端对同一条 wire 的 `OK` 判据不同 | **这一步不成立，但留了隐患** | `NetApply.Apply` 两端是同一份代码（`NetApply.cs:24-171`）；`EndTurn` 是无条件 OK（`NetApply.cs:160-163`），与局面无关。⚠️ 但"先试后拒"的写法本身会在**活 ctx** 上留下**不可回滚的副作用**（`u.LastAttackType = 4`、`RecomputeAttackTypeAfterActiveAbility`、`Auras.Recompose` 一整套重挂 —— 见 `CLAUDE.md` 那条「`Recompose` 幂等 ≠ 无副作用」）⇒ **换一条"试了会改局面"的候选，就可能让后面一条动作在一端 OK、另一端不 OK**。这次没发生（`PlayerHash` 相等已证），但这是同一个改法的下一个坑 |

---

## ③ 改法（按"先救红、再修真账"两刀）

### 刀 1（夹具 · 立刻 · 约 3 行）—— 试错不许在活 ctx 上留痕

在候选循环外记一次水位，试失败就撤回：

```csharp
int evBefore = ctx.Events.Count;                       // 试错前的诊断流水位
foreach (var cand in PickActions(ctx))
{
    ...
    int rc = NetApply.Apply(ctx, w, nb.MySeat);
    if (rc == RuleCodes.OK) { act = cand; code = rc; break; }
    // 🔴 「试了一条被拒的候选」不许留在诊断流里：这条不广播 ⇒ 对端不会有这几行
    //    ⇒ 而 ctx.Events.Count 是 NetProtocol.Fingerprint 的一维。判据 = 本报告。
    if (ctx.Events.Count > evBefore) ctx.Events.RemoveRange(evBefore, ctx.Events.Count - evBefore);
    Debug.Log(P + " 本机这条被拒了（换下一条）：" + RuleCodes.Describe(rc) + " " + cand);
}
```

- **只截 `Events`，⛔ 别去截 `Signals`**（`Signals` 是被 `DrainSignals` 抽走的，截了会丢表现事件）。
- 这套截断**只治症状**；它掩盖不了"活 ctx 已被试错改过"的那一半（`LastAttackType` 等）。
  ⚠️ **别在夹具里另写一份"余额判据"去预筛候选**（那会把引擎规则抄第二份 = `CLAUDE.md` §三「两处写同一条规则」）。

### 刀 2（生产 · 真账 · 要另派写手）—— 两件，缺一不可

**(2a) 本机动作被拒 ⇒ 撤回它写的那几行诊断日志，但照旧出声。**
落点：`BattleDriver.LocalAct`（`BattleDriver.cs:284-313`，**它是"会改引擎状态的唯一本地口"**，一次盖住全部入口）：

```csharp
int evBefore = Ctx.Events.Count;
int code = apply();
if (code != RuleCodes.OK && Ctx.Events.Count > evBefore)
{
    for (int i = evBefore; i < Ctx.Events.Count; i++) Debug.LogWarning("[Battle] " + Ctx.Events[i]);  // ⛔ 不许静默
    Ctx.Events.RemoveRange(evBefore, Ctx.Events.Count - evBefore);   // 这条不广播 ⇒ 对端不会有
}
```

**(2b) 缺的那一跳：原版"能不能用这个主动技能"**是**带余额**的。**
判据（`d:/2/tools/decomp_full/BattleManager__CanUseActiveAbility.c:52`）：
`PlayerManager__HasEnoughMana(...)` —— **整条 ability 判一次余额**，付不起 ⇒ 返回 0（钮不可点）。
而我们的两个闸**都没有余额那一跳**：

- `RuleCore.CanUseOathAbility`（`RuleCore.cs:11655-11669`）：只判 回合/眩晕/次数/本回合部署；
- `RuleCore.CanStartAbility`（`RuleCore.cs:11067-11078`）：只判 回合/有技能/耗竭/眩晕。
⇒ HUD 的"可点"判据偏宽（`BattleDriver.cs:16178` 的 `HasOath && CanUseOathAbility == OK`、
   `:15065` / `:15263` / `:16102` 同一份），玩家**能点**一个付不起的技能 ⇒ 落到 (2a) 那条路上。
⇒ 按原版补「余额」那一跳（**判据只写一处**，HUD 只转发）。⚠️ 补之前要先核
`CanUseActiveAbility` 的 `param_3`（0/1 两档）与 `param_4` 的语义，**别只抄 `HasEnoughMana` 那一句**。

> **⛔ 不推荐的第三条路**：把 `Events.Count` 从 `Fingerprint` 里删掉（改用 `StateHash`）。
> 那会让"一端少落了一条动作"这类真分叉**从指纹里消失**；而且 `NetBattleTest.cs:436`
> 的「两端事件条数一致」这条断言就失去意义了。真要动指纹，得先论证"`Events` 与两端一致性无关"，
> 而现在的事实恰恰相反。

---

## ④ 查不清的 / 差什么

1. **那 4 行里第 1、2 行的原文没直接读到**：`DumpTail`（`NetBattleTest.cs:1947`，打日志在 `:1953`）只打最后 8 条，
   主机 46、47 两条被截掉。结论"恰好 4 行"由哈希算术（4×31²=3844）证明是死的；
   但"第 2 行是 `Aura.cs:730`"是**排除法**（这条路上没有别的 `ctx.Log`），**没逐字见到**。
   要坐实：把 `DumpTail` 的窗口临时放大（或按 `Events` 里含"誓约"过滤 dump）重跑一次即可。
2. **生产可达性的完整清单没点完**：我只核了 `UseOathAbility`（会）/`DeclareAttack`（拒的那几条早退**不打日志**，`RuleCore.cs:6243-6620` 扫过）。
   `UseAbility` / `UseAlternative` / `PlayCard` 的**失败早退**是否有 `ctx.Log`，**没逐条扫完** ⇒
   「哪些本地动作会让 `Events.Count` 分叉」这张表**还没有**。差：把 `RuleCore` 里 `return RuleCodes.Err*` 的
   早退点与 `ctx.Log` 的先后扫一遍（脚本可做，不必跑 Unity）。
3. **`NetProtocol.StateHash` 的注释与事实不符**（铁律 5 候选，⛔ 我没改）：
   `NetProtocol.cs:574` 写「`ctx.Events` 是**待播事件队列**，表现层（`PlaySignals` / `DropSignals`）随时在抽干它」。
   实际：`PlaySignals → Ctx.DrainSignals(_signalBuf)`（`BattleDriver.cs:19289`）抽的是 **`Signals`**；
   `ctx.Events` 全仓**没有任何 drain**（只有 `BattleContext.cs:1858` 的 2000 条上限）。
   ⇒ 那句话把 `Events` 与 `Signals` 搞混了。**结论（`StateHash` 不含 `Events`）仍然对**，
   但**理由要改**（应该写成：「`Events` 是**诊断流**，本地被拒的动作也会写它 ⇒ 两端不保证相同」）。
4. **参照日志**：`_tmp_view/` 里**没有** 07:47 那跑（244/3）的副本（被 08:15 这跑覆盖）；
   更早的 `netbattle_rerun1/2.log`（10-09）、`netbattle2_1020.log`（10-08）是**另一代夹具**
   （`通过 185 · 失败 0`、无脚本对打那一段），**形状不可比**。⇒ 上一轮的状态只能看 `B9_D4NT` 报告。

---

## ⑤ 顺手发现（⛔ 我没改，请调度台分流）

1. **（真账 · 高优先）P2P 里点一个"付不起"的主动技能 ⇒ 回合末被误判打岔、对局中止。**
   链路：HUD 可点（缺余额闸，§③·2b）→ `UseOathAbility` 返 `ErrUnimplemented` 且写 4 行 `Events`
   → 不广播 → 回合末 `SendFingerprint` → `case NetKind.Hash` 比对不等 → `Abort` +
   `LeaveWithoutResult`（`NetBattle.cs:1590-1600`）。**这不是夹具问题，是联机真路径。**
2. **（夹具 · 低）"先试后拒"在活 ctx 上做，会留下 `LastAttackType` / `CurrentAttackType` / 光环重挂的副作用。**
   这次 `PlayerHash` 相等证明没有可观测差异，但下一次换个候选就可能让**后续动作在两端的合法性不同**。
   ⇒ 刀 1 只截 `Events` 治不了这一半；长期应改成"先按 `Can*` 预筛、再落地一次"，
   而**预筛的判据必须来自引擎**（不许在夹具里抄一份余额规则）。
3. **（文档 · 低）`NetProtocol.StateHash` 的那句 `Events` = 待播队列 = 错的**（§④ 第 3 条）。
4. **（顺带确认，不是问题）两端的 `P0/P1` 四行 + `PlayerHash` 完全相等** ⇒ 第 9 步这一手本身**没有**打错，
   这一轮的 5 条红**不含任何局面错误**。别按"局面分叉"的思路去查引擎结算。
