# Dg4 · `NetBattleTest` 本轮 7 红诊断（只读代理产出，主对话落盘）

> 代理 Dg4 为只读（Explore）⇒ 报告由主对话代存。日志 `d:/4/_tmp_view/20261025/net.log`（233 通过 / 7 失败）。日期 2026-10-25。

## 〇 一句话结论
7 条红 = **1 个根因、1 行代码**，**不是状态分叉、不是两端挑了不同目标**：
主机在自己的 `ctx.Events` 里比客机**多一条一次性诊断日志**（「Chaplain Letharius 的 `Talent` 触发已发出」），`NetProtocol.Fingerprint` 把 `ctx.Events.Count` 计入哈希 ⇒ 两端指纹差 **961 = 31²**（正好一条事件的权重）。局面（`Turn`/`Active`/`Winner`/`PlayerHash`×2）**逐位相同**。

## 一 逐条表

| # | 现象（日志） | 根因（文件:行号） | 判类 | 一句话修法 | 真缺陷? |
|---|---|---|---|---|---|
| 1 | 第 1 步两端分叉（`-381087003` / `-381087964`）`net.log:92920` | `RuleEngine/Core/EffectResolver.cs:7488`（进程级静态去重表 `_abilityNoAnimNoted`，`:7531`）+ `:7491`（`ctx.Log(msg)` 写进 `ctx.Events`）；`Net/NetProtocol.cs:476` 计 `Events.Count` | **γ**（根因是 β 性质缺陷；`C5·Q2` 落的，今天新调用点第一次踩到） | 删 `:7491` 的 `ctx.Log`（保留 `:7490` 的 `LogWarning`） | **是（低危：不改局面，但对联机自检/同进程重建对账是硬伤）** |
| 2 | 脚本对打只跑 1 步（回合 1）`net.log:92932` | 同 #1（`NetBattleTest.cs:393-399` 遇指纹不等即 `break`；`Ok(steps>5)` 在 `:403`） | γ | 同 #1 | 连带 |
| 3 | 打完两端指纹一致（`NetBattleTest.cs:404-405`） | 同 #1 | γ | 同 #1 | 连带 |
| 4 | 两端事件条数一致（实际 17，应 18）`net.log:92985` | 同 #1；主机多出的正是 `net.log:92833` 那条 note | γ | 同 #1 | 同根因 |
| 5 | 重放后客机没追平（`NetBattleTest.cs:436-437`） | 同 #1：`NetReplayFromNet` 重建的新 ctx 在同一进程里被 `:7488` 挡掉 | γ | 同 #1 | 同根因 |
| 6 | 主机掉线重连后没追平（`NetBattleTest.cs:459-460`） | 同 #1 | γ | 同 #1 | 同根因 |
| 7 | 引擎替玩家挑的两端不同（`-1902618710` / `-1902617749`，`NetBattleTest.cs:541-542`） | 同 #1，另一张卡：`Roboute Guilliman` 的天赋 note（`net.log:94631`） | γ | 同 #1 | 同根因 |

## 二 根因链
1. 今天新增 `EmitTalentTrigger`（`EffectResolver.cs:7610-7614`，调用 `RuleCore.cs:12320`；`git show HEAD` 0 命中）。
2. `EmitTalentTrigger` → `EmitCardTrigger` → `HasTriggerSource`（`:7454-7464`）：Talent 在 `animTimes` 没行 ⇒ 调 `NoteAbilityWithoutAnimRow`。
3. `NoteAbilityWithoutAnimRow`（`:7485-7492`）：`if (!_abilityNoAnimNoted.Add(key)) return;` —— **`static readonly HashSet`（`:7531`）⇒ 全进程只有第一个 context 能继续**；继续时 `Debug.LogWarning(msg)` **并** `ctx.Log(msg)`。
4. `BattleContext.Log` = `Events.Add`（`BattleContext.cs:1830-1833`）。
5. `NetProtocol.Fingerprint` 计 `ctx.Events.Count`（`:476`）。
6. 自检**同进程跑两个 context**（`NetBattleTest.cs:225/227`、`:489/491`）：主机先 apply ⇒ 拿到 note；客机被 `:7488` 挡掉。

## 三 两端事件序列（分叉点）
| 主机 # | 主机事件 | 客机 # | 客机事件 | 对齐 |
|---|---|---|---|---|
| 10 | 回合 1 开始 | 10 | 同 | 同 |
| **11** | **`[EffectResolver] 「Chaplain Letharius」的 Talent（46）触发已发出…`** | **—** | **（无）** | **← 分叉点：主机多发 1 条诊断** |
| 12–18 | 触发事件 / 临时卡 / STRIKE / 近战 / 反击 / 相邻 / 治疗 | 11–17 | 逐条同文 | 同 |

⇒ **主机多发一条诊断日志；客机没有少发任何对局事件**；`PlayerHash` 两端相同 ⇒ 局面无分叉。

## 四 为什么不是 E4/E6/E8/E9/CH/表现层
- 差 **961 = 31²** 精确命中 ⇒ 只有 `Events.Count` 差 1；若目标/结算/入队有分叉，`PlayerHash` 或事件文本会变。
- 分叉调用栈（`net.log:92800-92812`）= `SpawnTalents → EmitTalentTrigger → EmitCardTrigger → HasTriggerSource → NoteAbilityWithoutAnimRow`，无 E4/E8/E9 符号。
- `TriggerTargetSink` 只写事件对象字段、不写 `Events`；`AbilityPickSide` 等不碰 `Events`；CH 只在录像/驱动层；`CardView`/`BattleDriver`/`M1` 不写 `ctx.Events`。
- 项目自家纪律先例：`NoteAbilityPickOwnerGap`（`RuleCore.cs:10276`）/`NoteActivePickGap`（`:10508-10516`）/`StructuredConditionNotice`（`EffectResolver.cs:12071-12072`）都是「静态去重 + 只 `Debug.LogWarning`」⇒ `NoteAbilityWithoutAnimRow` 是唯一例外。

## 五 判类
**α 0 · β 1（根因）· γ 7（表象）· δ 0**。

## 六 修法与复跑（**主对话已执行**）
1. ✅ **P0 已改**：删 `EffectResolver.cs:7491` 的 `ctx.Log(msg)`（保留 `LogWarning`），并在 doc 里写明「⛔ 别再加回」+ 先例。
2. **P0 备选**（未采用）：去重表改成**按 context 去重**（若坚持把 note 留在战斗日志）。
3. ✅ **P1 已复跑**：`NetBattleTest.Run` **239 / 0 全绿**（本轮跑到结束，不止第 1 步）；`RuleEngineTest.Run` **8823 / 8823 全绿**（事件条数断言未受影响）；`check` / `play check` 均 rc=0。
4. **P2 回归护栏（建议下批）**：在 `NetBattleTest` 加「同进程两个 ctx 跑同一串动作 ⇒ `ctx.Events` 逐条相同」的断言；诊断类文案只走纯函数读口，⛔ 不读 `ctx.Events`。
5. **不建议**把 `Fingerprint` 换成 `StateHash` 绕开（会放过真实事件分叉）。

## 七 搜过的文件与词
代码：`RuleEngine/Core/{EffectResolver,RuleCore,BattleContext,ActionPump,ConditionEval,EffectText,TargetCriteriaEval}.cs` · `CardPresentation/Net/{NetProtocol,NetBattle,NetApply}.cs` · `Editor/NetBattleTest.cs` · `Battle/BattleDriver.cs`。
报告：`E4_…` `E6_…` `E8_…` `E9_…` `CH_…` `F3_…` `F4_…` `A_…` `A2_…` `Dg1_…`。
词：`触发**已发出` `talent:turnStart` `EmitTalentTrigger` `EmitListenerTrigger` `NoteAbilityWithoutAnimRow` `_abilityNoAnimNoted` `NoteActivePickGap` `NoteAbilityPickOwnerGap` `StructuredConditionNotice` `Events.Count` `TriggerTargetSink` `AbilityPickSide` `ResolveAbilityTargetOwner` `FriendlyUnit` `AbilityLevelConditionRow` `RecKindChat` `Fingerprint` `StateHash` `961`。
