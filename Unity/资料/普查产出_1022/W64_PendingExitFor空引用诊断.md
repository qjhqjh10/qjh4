# W64 · `PendingExitFor` 空引用诊断（只读代理，2026-10-22 第六十四会话）

> 现场：真机打出 4 费 Dire Avenger（槽 3）后，AI 攻击 → `NullReferenceException at BattleDriver.PendingExitFor ← SyncBoard ← RefreshAll ← AfterAiAction`（`_tmp_view/w64_battle.log:378-386`）；异常后该兵一直 `face=Hand`（`:434`），场上无数值、无绿光。

## ① 根因

`PendingExitFor` 假定时间线每条都带事件；但 `_timeline` **合法地混有「非事件 cue」**（`cueVfx` / `cueAction`），其 `evt` 恒为 `null`（struct 字段默认值），`BattleDriver.cs:1096` 的 `e.Player` 直接解引用 ⇒ NRE。

证据：
- 主文件 `CardPresentation/Battle/BattleDriver.cs:1093-1097` 无空判；同文件 `:14633-14635` `struct PendingSignal { public BattleEvent evt; ... }`；`RuleEngine/Core/BattleEvent.cs:297` `public class BattleEvent` ⇒ `evt` 是**引用类型**、默认 null。
- 两个刻意不填 `evt` 的构造点：`BattleDriver.cs:16388-16391` `ScheduleVfxCue`、`:16405` `ScheduleAction`；自文档 `:14704`「cueVfx 非空 ⇒ evt 为 null」、`:5459` 同样写明。
- 日志栈 = `PendingExitFor ← SyncBoard ← RefreshAll ← AfterAiAction`；`:376` 正在排攻击特效（远程攻击），`:377` attacked 触发 ⇒ 时间线里确有攻击 cue。

## ② 触发条件

(a) `_timeline` 存在 `evt=null` 的 cue：任意 `EvtKind.Attack` 批在 `PlaySignals`（`:14397`）排「抬手音」`ScheduleAction`；攻击演出 `:16478/:16492`；延迟特效 `:16280/:16296`；出牌落地回调 `:9938` ⇒ **AI 一攻击就必然产生**。
(b) 同一帧 `RefreshAll → SyncBoard` 走到离场判定：直接调用 `:18614`（空格且 `animateFeel`），或经 `DetachStaleView` `:1108`（调用点 `:18557/:18634/:18649`）。
本日志栈里没有 `DetachStaleView` 帧 ⇒ 更可能是 `:18614`；若被内联则两种都可能。异常中断 `SyncBoard` ⇒ Dire Avenger 未完成 hand→board（`:430` 落位槽 3，`:434` face=Hand）。

🔴 **自检为什么没抓到**：调用都在 `animateFeel` 门内（`:1108/:18614`）；`animateFeel` 默认 false（`:17368`，批处理/自检档）⇒ `PendingExitFor` 在自检里根本不跑。

## ③ 排除项

- 不是结构体成员问题：`PendingSignal` 是 struct（`:14633`），`_timeline[i]` 不可能为 null；NRE 只能来自 `evt`（class）。
- 不是遍历中被 `Clear`/重建：`_timeline` 是 readonly，全仓只在 `:14713` new 一次（无 `_timeline =`）；`Clear` 仅 `:3073/:3803/:15730`，均不在本栈；本栈只读列表、无重入；且 List 越界抛 `ArgumentOutOfRangeException`，不是 NRE。
- 事件型 `Add` 不会塞 null：`:14426/:14550/:14566` 的 `e` 来自 `Ctx.DrainSignals`，而 `BattleContext.cs:1671-1673` 的 `Emit` 拒绝 null，且同循环已先解引用 `e.Kind`。null 只来自两个 cue 构造点。
- 同型隐患：`DeathPendingFor` `:17857-17865` 同样无空判（全仓无调用点，死代码）；`TimelineDump` `:15068-15079` 也无空判（自检口，遇 cue 会 NRE）——建议同批加守卫。

## ④ 原版判据（`d:/2/tools/decomp_full/`）

- 读得到：原版 `BattleManager` 有四条动作队列 `List<BattleAction>`（`+0x350/+0x358/+0x360/+0x368`，`BattleManager__ActionsInQueue.c:12-28`；`BattleManager__NextActionInQueue.c`；`BattleManager__WaitingForBlockingAction.c:33-88`）。
- 最接近 `PendingExitFor` 的是「单位消失时扫队列撤掉其待播动作」：`BattleManager__CancelAnimFromUnitFromQueue.c` / `BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c`；扫描时对元素内 Unity 对象引用**逐一判空后跳过**（`CancelAnimFromUnitFromQueue.c:42` `op_Inequality(...,0,0)`；`BattleManager__DebugQueue.c:36` 同）。
- 读不到：没有名为 `PendingExitFor` 的等价方法，也没有「某槽是否有待播离场」的直接等价；无法从伪代码确认原版同类扫描对空对象的完整语义。⛔ 不猜。

## ⑤ 候选修法（按风险从低到高）

1. **【推荐·不改现有语义】** 在 `PendingExitFor` 循环内加 `if (e == null) continue;`（或条件前加 `e != null &&`）。理由：cue 信号不是事件，永远不可能满足 `Kind==Death||Return`，跳过它们与「它们本就不该参与判定」等价，对 Death/Return 的判定结果**逐位不变**。同批给 `DeathPendingFor`、`TimelineDump` 加同样守卫。
2. 按 `cueVfx/cueAction` 判「是不是事件」再跳过 —— 语义同上，但多一条判据、将来新增 cue 字段会漂，维护风险略高。
3. 给 cue 塞哨兵 `BattleEvent` / 改 Add 点 —— 改变时间线数据与开销，不建议。
4. 遍历改反向或换 for 写法 —— 对 null 无效，属白改。

## ⑥ 没查清

- 日志无 `SyncBoard` 行号，无法确认四个入口（`:18614`；`:1108→:18557/:18634/:18649`）中具体哪一个先命中；不影响根因。
- 原版伪代码无法逐字段还原，未确认原版对「空动作对象」的完整处理。
- 未跑 Unity 验证修法（只读红线）。建议修后补一条 `animateFeel=true` 的自检：**时间线含 cue + 空格 `SyncBoard` 不抛**。
