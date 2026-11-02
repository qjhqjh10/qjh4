# B117 · `B117-WREPLAY2` · 录像完整性 / 环境回落 三条缺口（执行代理报告 · 2026-11-02）

> **口径**：三条全部落在 `Battle/BattleDriver.cs`（＋ `RuleEngine/Core/RuleCore.cs` 一个返回类型）。
> 判据 → `判据/B117-G10-16_回放与录像.md` §二 G1 / G2 · `判据/B117-G10-02a_场地环境效果卡.md` §五 D6。
> ⛔ 本代理没跑 Unity 批处理、没动 git、没改账主表、没重钉指纹。
> 状态（哪条关了）**只看 `活状态表.md`**；本文件只放判据与做法。

---

## 一 三条各自的改动（`文件:行号` · 旧 → 新）

### ① `B117-R62` · `ApplyTutorialSkip` 绕过全部三个记账口

| 位置 | 旧 | 新 |
|---|---|---|
| `Battle/BattleDriver.cs:18276` | `RuleCore.CheckWinner(Ctx);` | `RuleCore.CheckWinner(Ctx); RecRaw(RecKindSkipTutorial, _me);` |
| `Battle/BattleDriver.cs:1274`（回放侧分派） | `if (ApplyTutorialRaw(Ctx, m)) { … return true; }` | 同一行末尾追加 `if (ApplyReplayPseudoRaw(m)) { ObserveReplayFullTrace(i, m); return true; }` |
| 类尾（`:40706-40757`） | —— | 新增常量 `RecKindSkipTutorial = 203` ＋ 回放落点 `ApplyReplayPseudoRaw`（判死 → `CheckWinner`，与录制端同一句） |

🔴 **就地订正施工单的一处**：施工单写「在 `CheckWinner` **之前** `RecRaw` 记一条」——**那一句会做错**。
判据：`RecRaw` 同时采 `DeepHash` 和**全量指纹** `NetProtocol.StateHash(Ctx)`，而 `StateHash` **含 `ctx.Winner`**；
记在判负之前 ⇒ 采到「还没判负」那一格，而回放侧 `ApplyReplayPseudoRaw` 是**判负之后**才采样 ⇒
每一份以「跳过教程」结束的录像都会在**最后一条**假报分叉（这正是 `RecKindForfeit` 在 2026-10-24 `W9` 里
把记账挪到判负**之后**的同一个理由，见 `NetRemoteResign` 那一段注释）。⇒ **记在 `CheckWinner` 之后**，
方向与「学现成的 `RecKindForfeit 200`」一致。

**为什么不改成 `RuleCore.Forfeit(Ctx, _me, BattleResult.WinButton)`**（`A913` 的既有裁定，沿用）：
`Forfeit` 会额外写 `Ctx.ForfeitedBy`，而原版 `ClickSkip.c` 只有那一次 `DeadHero` ⇒ 结算面板会说错话。

### ② `B117-R63` · 回合初抽牌「推迟」与录像指纹分叉

| 位置 | 旧 | 新 |
|---|---|---|
| `Battle/BattleDriver.cs:14680`（录制端补跑口） | `RuleCore.ResolvePendingTurnDraw(Ctx);` | `if (RuleCore.ResolvePendingTurnDraw(Ctx)) RecRaw(RecKindTurnDrawDeferred, Ctx.Active);` |
| `Battle/BattleDriver.cs:1095`（回放端开局） | `_replaySession = true;` | 同一行追加 `if (ReplayHasDeferredTurnDraw(rec)) { Ctx.DeferTurnDraw = true; Ctx.DeferSeat = _me; }` |
| `Battle/BattleDriver.cs:1274` | 同上 | 同一个 `ApplyReplayPseudoRaw` 里第二支：`CompleteReplayBeginTurn(); RuleCore.ResolvePendingTurnDraw(Ctx);` |
| `RuleEngine/Core/RuleCore.cs:2377-2401` | `public static void ResolvePendingTurnDraw` / Core 两个早退 `return;` / 末尾 `BeginTurnTail(...)` | `public static bool …`；早退 `return false;`；末尾 `BeginTurnTail(...); return true;`（**行数 ±0**） |
| `Battle/BattleDriver.cs:40711-40728` | —— | 新常量 `RecKindTurnDrawDeferred = 204` ＋ `ReplayHasDeferredTurnDraw(rec)`（按 kind 扫录像） |

**为什么让 `ResolvePendingTurnDraw` 返回 `bool`**：录制端必须知道「这一趟**真的**补跑了」才记伪动作。
不返回就得在 `HoldForTurnHeadsUp` 里另加一行判断 —— 那会让**下游 4 万行**整体漂移（账落点极多）。
`bool` 的另一个好处是**幂等可见**：回放里「抬头排期」与「伪动作」两处都会调它，第二处返回 `false` ⇒ 不会多记。

**回放端为什么要「按录像内容」开推迟**：老录像（这条伪 kind 之前录的）里没有 204，
若**无条件**打开 `Ctx.DeferTurnDraw`，那些「批处理里录的、两端本来都不推迟」的录像会**反而**分叉。
⇒ `ReplayHasDeferredTurnDraw(rec)` 扫一遍 `actions`，有 204 才开。

### ③ `B117-R65` · 空卡（`Normal Conditions`）也要应用本阵营默认环境

| 位置 | 旧 | 新 |
|---|---|---|
| `Battle/BattleDriver.cs:9818` | `… ，环境也不动");` | `… ；但按原版环境**仍要切到本阵营的默认环境**（`GetEnviromentalEffect` 的空卡分支 → `DefaultEnviromentalEffectVFX`，`_d__337__MoveNext` state 2 无条件调 `ApplyEnvEffect`）");` |
| `Battle/BattleDriver.cs:9819` | `return;` | `if (string.IsNullOrEmpty(so)) { var _oc = OffensiveCards.For(FactionOf(Ctx.SecondSeat)); so = _oc != null ? _oc.defaultEnvSO : ""; }` |

- **删掉那条早退** ⇒ 空卡这一支之后照样走到 `ApplyEnvWarlordAbilityOnce()`（`:9829`）与
  `_envApplier.Apply(envItem, instant: false)`（`:9837`）—— 与原版 `ApplyEnvEffect.c:14-15`
  把「督军触发 + 换环境」**包在同一个 `if` 里**同形（`B94_B22` 那道总闸不变）。
- **回落判据**与 `EnvSOFor` 的空卡分支**同一条**：`OffensiveCards.For(FactionOf(Ctx.SecondSeat)).defaultEnvSO`
  （生产路径 `Ctx.OffensiveEnvSO` 本来就已经是这条 default 名；回落只服务自检里 `ChooseOffensiveCard(..., -1, "")` 那一档）。
- ⚠️ 空卡走一遍 `ChosenOffensiveCardDef()` 会命中「第二张表里没有 `(faction, -1)` 那行」那条 **`Debug.Log`**
  （`:37520-37527`，`_envCardNoDefWarned` 只喊一次）—— 那是**如实说明**（原版那一跳今天没有可执行的 ability 树），
  ⛔ 不是回归、也不是「静默失败」。

---

## 二 为什么录像两端现在一致（逐拍推理）

设本机座位 `_me`，AI 交回合那一拍（**单机真机**，`_net == null`、非回放）：

| # | 录制端（真机） | 状态 `StateHash` 采在 | 回放端（改前） | 回放端（改后） |
|---|---|---|---|---|
| 1 | `EndTurnAndAdvance`：`EndTurn` → `PrepareTurnDrawDeferral`（`DeferTurnDraw=真`）→ `BeginTurn` ⇒ **pre-wait 跑、post-wait 推掉**（`PendingTurnDraw=真`） | `RecAct(EndTurn)` 在**推掉之后**采 ⇒ **pre-tail** | `BeginTurn` 见 `DeferTurnDraw` 假 ⇒ **tail 当场跑完** ⇒ 采到 **post-tail** | `ReplayHasDeferredTurnDraw(真)` ⇒ `DeferTurnDraw=真` ⇒ `BeginTurn` 同样推掉 ⇒ 采到 **pre-tail** ✅ |
| 2 | 抬头 1.5 s 到点 ⇒ `ResolvePendingTurnDraw` 跑完 tail（`turn_setup` / `BroadcastTurnSetup` / 抽牌 / 光环重算 / 逐卡 `OnTurnStart` / `turn_start` / `ActivateMinions` / 天赋）⇒ 返回 **true** ⇒ `RecRaw(RecKindTurnDrawDeferred)` | **post-tail** | （录制端根本没有这一格 ⇒ `traceFull` 少一格 ⇒ `Finish(ObservedCount)` 报条数不符） | 先 `CompleteReplayBeginTurn()`（把「换牌之后那次 `BeginTurn`」兑现，这一条动作排在 `ApplyLoggedAction` 之前 ⇒ `NetApply.Apply` 开头那个消费点够不着它），再 `ResolvePendingTurnDraw` ⇒ 跑完 tail ⇒ 采 **post-tail** ✅ |

**为什么中间不会插进别的动作**：那 1.5 s 是 `BeginBlockingWindow`，
`CanPlayCard` 在 `WaitingForBlockingAction` 为真时直接拒（`BattleDriver.cs:5677`）⇒ 玩家出不了牌；
AI 那一侧此刻不是行动方。⇒ 伪动作在动作流里的位置**正好**是「EndTurn 之后、下一条真动作之前」，两端同序。

**两条兜底也两端同形**（所以即使某一拍走了别的路，也不会静默分叉）：
① `RuleCore.EndTurnCore:2420` 的 `if (ctx.PendingTurnDraw) ResolvePendingTurnDraw(ctx);` —— 换边之前补上；
② 回放端 `ApplyLoggedAction` 的 `NetApply.EndsTurn` 支会调 `ShowYourTurnHeadsUp()` → `HoldForTurnHeadsUp`，
排期那一下同样调 `ResolvePendingTurnDraw`（幂等；先跑到就变成伪动作那一格的 no-op，`StateHash` 仍然相等）。

**批处理自检为什么以前抓不到**：`TurnDrawDeferralEnabled = !Application.isBatchMode` ⇒ 自检里恒关
⇒ 两端都不推迟 ⇒ 从来没有分叉。这条修复只能靠「显式开推迟」的自检或真机录像验证（见 §四）。

---

## 三 验收

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_wreplay2 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0**（第一次跑红 2 条，全在别人正在写的 `Battle/Sfx.cs`：`CS0103 _lastAt`，与本次改动无关；60 秒后复跑 0/0） |
| `python -I -X utf8 d:/4/Unity/工具/check_aggregate_lines.py MyGame/Assets/CardPresentation/Editor/{BattleScene,ShellScene}.cs` | **PASS**（2 个聚合行 / 117 个调用 / 违规 0） |
| `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` | 漂移 / 失败段里 **`BattleDriver.cs` 0 行 · `RuleCore.cs` 0 行**（其余文件的漂移是别人未提交的活） |
| 行数守恒 | `BattleDriver.cs` 40781 → 40842 行（**+61 = 类尾新成员**，最后一条账落点是 `:40001`，未漂）；`RuleCore.cs` **18406 → 18406（±0）** |
| 行尾 | `BattleDriver.cs` 纯 CRLF（40841 / 裸 CR 0）；`RuleCore.cs` 纯 LF；新断言文件纯 LF（与 `Editor/*_Assert.cs` 同） |
| ⛔ 未跑 | Unity 批处理（红线）⇒ 新断言**只编译、没执行**；真机录像实测（红线） |

### 新增断言：`CardPresentation/Editor/B117_WREPLAY2_Assert.cs`（静态类 `B117_WREPLAY2_Assert` + `Run()`，**最多 35 条**）

- **A 组（14 条 · 真跑引擎，零 GameObject）**：`ResolvePendingTurnDraw` 的返回值 ——
  A2 ★「推迟后返回 **true**」· A3 ★「再调返回 **false**（幂等）」· A4「座位对不上 ⇒ false 且不多抽」·
  A6「对照：开关关着 ⇒ 当场 +1 且返回 false」· A5「空参 false」＋各条前提。
- **B 组（13 条 · 源级）**：两条伪 kind 的取值互不相同（203 / 204 ≠ 200/201/202）·
  B2 ★「录制端 `CheckWinner` → `RecRaw` 的**先后**」· B3「回放端同一句」· B4 ★「录制端 `if (ResolvePendingTurnDraw(...)) RecRaw(...)`」·
  B4b「补跑调用**唯一**一处、且就是带记录那行」· B5 ★「回放端按录像内容开推迟」· B6 ★「伪 kind 排在 `ApplyLoggedAction` 之前」·
  B7「常量在类尾」＋前提。
- **C 组（8 条）**：C2 ★「空卡支里 `return;` 0 处」· C3「回落 = `OffensiveCards.For(FactionOf(SecondSeat)).defaultEnvSO`」·
  C4「之后照样走督军触发 + 换环境」· C5 ★「**13 个阵营的 `defaultEnvSO` 全在 `EnvironmentConditions.json` 里**」
  （否则「回落」只是换成一个查不到的名字，仍然不换环境）＋前提。

**灭自证 5 条**（文件头 🧨 A–C）：返回值改回 `void` ⇒ A2 红；`CheckWinner`/`RecRaw` 反序 ⇒ B2 红；
`if (ResolvePendingTurnDraw(...))` 的条件去掉 ⇒ B4 红；回放端无条件开推迟 ⇒ B5 红；空卡支加回 `return;` ⇒ C2 红。

**挂法**（⛔ 本代理不改宿主）：宿主里加 `B117_WREPLAY2_Assert.Run(); _pass += B117_WREPLAY2_Assert.Pass; _fail += B117_WREPLAY2_Assert.Fail;`

---

## 四 没做完的 / 风险（如实记）

1. **没跑 Unity 批处理** ⇒ 新断言**没执行过**，`B117-R65` 的「空卡切默认环境」也**没有真机/宿主验证**。
2. **`BattleScene.cs:4719-4721` 那条快照会变**：它 snapshot `RenderSettings.fog / fogDensity / fogColor /
   ambientLight / _AmbientColorBlend`，而空卡那一步现在**真的会改一轮**（`B117-G10-02a` §五 已预警）。
   ⇒ 判据是「断言仍应绿」（它不断具体值），但**只能由跑宿主的调度台确认**。
3. **`B117-R63` 的端到端只在「真机录像」或「显式开推迟的自检」里才可见**
   （批处理 `TurnDrawDeferralEnabled` 恒关）。要让自检覆盖，需要宿主在录像夹具里
   `driver.SetTurnDrawDeferralForTest(true)` ＋ 推 `SimulateResetClockForTest()` 那 1.5 s —— 那是宿主侧的活，本代理没动。
4. **`RuleCore.cs` 里还有别人未提交的改动**（`B117-WMULL` 的换牌重排 / `B117-WREPLAY` 的一行注释订正）——
   与本次改动的行段（2377-2401）不重叠，`git diff` 里各自独立。
5. **一处观感副作用**：空卡那一支现在会走 `ApplyEnvWarlordAbilityOnce()`，空卡的 `ChosenOffensiveCardDef()`
   在第二张表里没有 `(faction, -1)` 行 ⇒ 会打一条 **一次性** `Debug.Log`（不是 warning）。
   ⛔ 不改：那是如实说明「原版那一跳今天没有可执行的 ability 树」，不是新造的行为。
