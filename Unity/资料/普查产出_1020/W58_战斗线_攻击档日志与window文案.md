# W58-LOGFIX · 攻击档（`"attack"`）战斗日志与过期 `"window"` 文案

> 代理：`W58-LOGFIX`（战斗线）· 日期：2026-10-20
> 白名单：`Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（唯一改动文件）
> 判据：`W58_审查修复_A.md` / `W58_回合流程_伏击与洗回.md` / `RuleEngine/Core/BattleEvent.cs` 的
> `EvtKind.AmbushExit` doc / `RuleEngine/Core/RuleCore.cs` 两条发送点。

---

## 结论

1. **`"attack"` 档现在会印战斗日志了**：`case EvtKind.AmbushExit` 的可印档从
   `e.Effect == "damage"` 扩成 `"damage"` / `"attack"` 两档；两档印**同一行、同一个键**
   `Battle/Cemetery/ActionExitAmbush`（值 `{0} 从伏击中出现` / `{0} exits from Ambush`）。
   判据 = 原版 `CemeteryManager.AddExitAmbushActionToCemetery` 的**唯一**调用点是
   `CardScript.SetAmbush(card,false)` 的尾部，而 `SetAmbush(card,0)` 的三个调用点是
   **两条伤害路 + 一条攻击取消路**（`_ResolveAttack…:305`）⇒ 三条路都印同一行。
2. **本地化键不缺**：`Core/Loc.cs:1195` 已有 `Battle/Cemetery/ActionExitAmbush` 的 en/zh 两列
   （官方值）⇒ 走现有 `LogTerm` 即可，**未新增回落、也不需要出声**（缺键分支仍由 `LogTerm` 自己兜）。
3. **四处过期 `"window"` 文案已订正**（另顺带订正同文件里三处同源旧说法），全部保留
   「⚠️ 2026-10-20 就地订正」痕迹。`"window"` 的**行为过滤保留**（不可达值），只为读旧录像/旧断言。
4. **P3-C 未动行为**（判据未闭合）：取消的伏击攻击**仍不发** `EvtKind.Attack` —— 现状与所需判据见下。

---

## 日志改法

位置：`BattleDriver.cs` · `RefreshBattleLog` 的 `case EvtKind.AmbushExit`（改后约 `:8024`）。

```csharp
if (e.Effect != "damage" && e.Effect != "attack") { skipRow = true; break; }
line = LogTerm(CemeteryExitAmbushTerm, "{0} exits from Ambush", "{0} 从伏击中出现", card);
```

- 键 = `CemeteryExitAmbushTerm`（`Battle/Cemetery/ActionExitAmbush`，`BattleDriver.cs:7722` 常量；
  值 → `Core/Loc.cs:1195`）。**两档共用一行**，与原版「一个函数一个键、三条路都调」一致。
- `"window"`（以及其他任何历史值）仍走 `skipRow = true`：不占行、不落 `default:` 的裸行、不静默。
- 该支注释同步改写：原文「只印 `"damage"` 那一档」+「三个调用点全是伤害路」的错前提一并订正，
  并注明错因（把 `W58-AMBUSH` 已推翻的「全是伤害路」当判据）。

---

## `window` 逐处订正（都在白名单文件内）

| 位置（改后行号） | 原文（过期说法） | 订正后 |
|---|---|---|
| `:7839-7846`（`RefreshBattleLog` 上方总注） | `` `AmbushExit`（`:5832` 伤害路 / `:6065` 窗口路） `` | `RuleCore.ApplyDamage` 伤害路 / `RuleCore.DeclareAttack` 攻击取消路；并注明那两处行号已漂走、`RevealAmbush` 已删除、`"window"` 历史不可达 |
| `:7881-7886` | 「`AmbushExit` 只印 `"damage"` 那一档（`"window"` 是我们模型的产物、原版没有）」 | 印 `"damage"` / `"attack"` 两档；「只印 damage 不成立」+ 错因（三个 `SetAmbush(false)` 调用点被误读成全是伤害路）；`"window"` = 历史值、不可达 |
| `:7906-7910`（`skipRow` 注释） | 「有两档不产出日志行 —— `Draw` 与 `AmbushExit` 的**非伤害路**」 | `Draw` 与 `AmbushExit` 的**`"window"` 档**；`"attack"` 现在印行，真正不印的只有不可达的 `"window"` |
| `:8000-8002`（三支总注） | 「`AmbushExit` **只印** `"damage"`」 | 印 `"damage"` / `"attack"` 两档（`"window"` 历史值、不可达） |
| `:8024-8034`（`case` 本体注释） | 「只印 `"damage"`，⛔ 不印 `"window"`」「三个调用点全是伤害路」 | 两档都印；三个调用点 = 两条伤害 + 一条攻击取消；`"window"` 因 `RuleCore.RevealAmbush` 删除而不可达 |
| `:15456-15460`（`VfxMap`/特效归属注释） | 「两条出口（`Effect` = `"damage"` / `"window"`）播的是同一件」 | 今天两档 `"damage"` / `"attack"`；`"window"` 历史值、不可达 |

> 说明：任务点名四处（`:7875` / `:8007` / `:8011` / `:15436`），行号按内容定位；另三处
> （`:7839` / `:7896` / `:7988`）是同一过期事实的复制说法，按铁律 5「grep 一句话被复制到别处的地方一起改」
> 一并订正 —— 全部在本文件内，未越白名单。
> 改后文件内残留的 `"window"` 字样**全部位于订正痕迹里**（说明「原文写 X，实际是 Y」），没有活的旧口径。

---

## P3-C 现状与所需判据

**问题**：取消的伏击攻击（面朝下出手 ⇒ `CancelAttack` + `SetAmbush(false)`）**要不要先发 `EvtKind.Attack`？**

**现状（未改）**：`RuleCore.DeclareAttack` 的伏击支（`RuleCore.cs:5219-5243`）在
`ctx.Emit(EvtKind.Attack, …)`（`:5393`）与 `BroadcastWhen(WhenEventKind.Attack, …)`（`:5431`）**之前**
`return RuleCodes.OK` ⇒ 这一记攻击在表现层**完全不可见**，连带跳过：

| 通道 | 依赖 | 取消攻击时是否发生 |
|---|---|---|
| 战斗日志「`{0}` 对 `{1}` 进行近战/远程攻击」 | `EvtKind.Attack` → `BattleDriver.cs:7933` | **不印** |
| 攻击特效 `AttackMelee` / `AttackRanged` | `EvtKind.Attack` → `VfxMap`（`BattleDriver.cs:15404`） | **不播** |
| 攻击语音 `VoiceLines.ExactAttack` | `EvtKind.Attack`（`BattleDriver.cs:4759`） | **不播** |
| `PlayAttackFeel`（抬手/命中手感） | `EvtKind.Attack`（`BattleDriver.cs:16354`） | **不做** |
| 「When a unit attacks…」广播 | `BroadcastWhen(Attack)` | **不触发** |

**本轮只读核到的事实（供主对话收口，未据此改行为）**：
- 原版 `BattleManager._ResolveAttack_d__438__MoveNext.c` 的伏击支在 `:288-305`，之后 `goto` 收尾；
- **攻击日志** `CemeteryManager.AddAttackActionToCemetery` 在 **`:1244`**、`ChallengeLogMgr__LogAttack` 在 `:1247`；
- **攻击动画** `CardScript.AttackMeleeAnim` 在 **`:899`**、`ResolveAttackRangedAnim` 在 `:911`；
- 上述四处**都在伏击支之后** ⇒ 原版「取消的伏击攻击」同样**不印攻击行、不播攻击动画**。
- 伏击支**之前**只有一次 `BattleManager__CommunicateBattleAction`（`:286`）；读其方法体
  （`BattleManager__CommunicateBattleAction.c`）= **动作流水/网络同步**（`playerActionsLog` /
  `enemyActionsLog` + `BattleCommsManager__RemoteSendPlayerAction`），**不是**表现层的「攻击宣言」。

**因此「需要闭合的判据」已经缩小为两个具体问题**（仍以原版为准，⛔ 别按印象改）：
1. 我们模型里 `EvtKind.Attack` 到底对应原版的哪一层 —— 若对应「表现层攻击动作（动画/日志/语音）」，
   则上面四条证据已指向「**取消攻击不该发**」（现状即正确）；若还兼着「`CommunicateBattleAction`
   那一层动作流水/联机同步」，则**取消攻击也该被对端/回放看到**，需另找一个不驱动表现层的口，
   **不应该**复用 `EvtKind.Attack`（否则会凭空播一次攻击动画）。
2. 联机/回放一致性：`A1632`/`W58` 之后取消一记攻击时，peer 与本地录像是否需要这条「攻击尝试」记录
   （原版走 `RemoteSendPlayerAction`）——需读我方 `NetBattle`/`ReplayStore` 的动作流口径再定。
> ⛔ 结论：判据未闭合，**行为保持现状**；上面六条注释里也没有把它写成「已定」。

---

## 白名单外残留

1. `CardPresentation/Editor/BattleScene.cs:1209-1211` —— 断言**消息字符串**仍写
   「两条翻面出口（`damage` / `window`）」。只影响自检失败时的文案，不影响断言本身；
   **不在白名单，未改**，建议主对话归界面/自检线一并订正为 `damage` / `attack`。
2. `W58_回合流程_伏击与洗回.md` §未定项 3（`:125-127`）写的「表现层会漏 `"attack"` 的日志 ⇒ 未改，
   留给界面线」—— **本轮已改掉**（该报告是历史记录，按需由主对话合并更新）。
3. `W58_审查修复_A.md` §「只报告、未改的其他残留」第 1 条（`:150-152`）列的四行 —— **本轮已订正**；
   同文件第 2 条（`BattleScene.cs:1209`）**仍在**（见上）。

---

## typecheck

`TMPDIR=/tmp/wf_logfix bash d:/4/Unity/工具/typecheck.sh` ⇒
**运行时错误数 0 · 编辑器错误数 0**（改完全部编辑后跑的那一次）。

未跑 Unity 批处理、未跑任何 `*.Run`、未动 git。
行尾已核：`BattleDriver.cs` = **纯 CRLF**（`22819/22819`，裸 LF 0）；`git diff --numstat` =
`346 / 64`（无整篇翻行尾迹象；差额 = 本文件原有的未提交改动 + 本轮订正）。
