# W · 槽数与胜利码（`A985⑨` + `A985⑩`）—— 写手交件

> 写手：`W_槽数与胜利码` · 2026-10-18（第三会话）· 白名单内改动，**没跑 Unity**（秒级类型检查 + 离线探针已跑，见末节）

---

## ① `A985⑨` 原版 `CardEffectItem` 每场几颗 —— **一致（5 = 5），无缺口**

### 判据（原版读数：**没有循环体、没有常量，是序列化 5 个实例**）

| 读到什么 | 出处 | 读数 |
|---|---|---|
| `CardDisplayWindow` 的**序列化字段** `cardEffectSlots` | `assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4750.json` | **长度 5**（pid **5092 / 4024 / 4663 / 4402 / 4029**） |
| 那 5 个 pid 各自是啥 | 同包 `MonoBehaviour_4024/5092/4663/4402/4029.json` | 每个都只有 `enchanterText` + `effectText` 两个字段、`m_Script` 同一个 pid `-1195602372414045925` ⇒ **`CardEffectItem`** |
| 挂在哪 | 同包 `GameObject/EffectElement{,(1),(2),(3),(4)}.json` | **5 个 GameObject**，各挂 1 颗（组件 pid 与上表**逐一对上**） |
| 全库核一遍 | `assets_full/*/GameObject/` 全扫 | **只有 14 个包有 `EffectElement`**（13 个 arena + `mainmenuwarpforge`），**每个恰好 5** |
| 菜单场景那份 | `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2033.json` | `cardEffectSlots` 也是 **5** |
| 方法体侧有没有运行时建槽 | `decomp_full/CardDisplayWindow__DisplayCardEffects.c` | **没有** `Instantiate` / `AddChild` —— 只**遍历**那个数组，循环上界 = 数组长度（`*(int *)(lVar5 + 0x18)`） |
| 唯一的另一个消费者 | `decomp_full/CardEffectsService__.ctor.c` | 数组是**外面传进来**的（`CardEffectsService(CardEffectItem[] cardEffectSlots)`），它自己不建 |

⇒ **「每场 5 颗」= 场景/prefab 里序列化的 5 颗**；原版**没有**「槽数上限」这种运行时常量（⛔ 上一批「只找到循环体、没数出来」那个说法，根因是**它本来就不在代码里**）。

### 我们这边
`CardWinBox.EffSlots = 5`（`Core/CardWinBox.cs:71`）·`EffRowCy` **5 项**（:69）·
`Battle/CardDisplayWindow.Build()` 按 `EffSlots` 建 5 组数组（`CardDisplayWindow.cs:265-269`，循环 `:274`）·
显示时 `Mathf.Min(rows.Count, EffSlots)` + **超限告警**（`:433-435`，比原版「静默截断」更出声）。

**结论：一致、无缺口 ⇒ 本笔代码零改动。**

### 断言：**本批没写**（宿主在黑名单）
唯一自然的宿主是 `CardPresentation/Editor/BattleScene.cs`（那里已经断 `CardDisplayWindow.RowsOf/EffectWhat/EffectWhatLeftPx/EffectTextInFrontOfBg`，见 `:4227` / `:4241`），
而它在**黑名单（热）**里 ⇒ 按简报「宿主若只在热的里面 ⇒ 只落代码、不写断言」处理。
**⏭ 需要主对话派的宿主 = `Editor/BattleScene.cs`**，建议两条：
① 纯常量那半（`CardWinBox.EffSlots == 5 && CardWinBox.EffRowCy.Length == 5`，判据 = 上面那张表）；
② 运行期那半（开窗后**真的建了 5 个槽**，`driver.CardDisplay` 上加一个 `EffectSlotCountForTest` 读口）——②才是「不是自证」的那一条。

---

## ② `A985⑩` 码 `BattleVictory`(1) —— **做完了（只进日志）**

### 照谁抄 · 记在哪
* **形状照 `RuleCore.Forfeit`**（`RuleCore.cs:5060`：`ctx.Log($"[BattleResult] {reason}({(int)reason}) seat={player}")`）。
* **判据 = 原版 `BattleManager__FinishResolvingAction.c:84`**（本轮重读那句 = `BattleManager__DeadHero(param_1, …, 1)`，第三个实参就是 `BattleVictory`）。
* **落点 = `RuleCore.CheckWinner`**（`RuleCore.cs:5141-5143`）：算完 `d0/d1` 之后、教程 `playerAlwaysWins` **覆盖之前**（原版顺序就是 `DeadHero(_,_,1)` 在 `FinishResolvingAction`、`GetWinnerAfterBattleEnd` 在后面）：
  ```csharp
  if (d0 != d1)
      ctx.Log($"[BattleResult] {BattleResult.BattleVictory}({(int)BattleResult.BattleVictory}) seat={(d0 ? 0 : 1)}");
  ```
  `seat` = **倒下的那一位**（与 `Forfeit` 的 `seat=` 同义）。
* **平局那一支（`d0 && d1`）不记码**，另加**一行出声**（`RuleCore.cs:5172`）—— 原版那一跳只对应「**某一位**督军倒下」，两位同时倒**没有**对应调用点 ⇒ **不猜**（⛔ 不拿 `BattleVictory` 顶替）。
  ⚠️ 那一行**故意不以 `[BattleResult] ` 开头**：该前缀在本工程的含义定成「**这一局给出了一条理由码**」（自检按它计数），说明行不能混进那个计数。

### 三个「不许」怎么保证的
| 不许 | 怎么保证 |
|---|---|
| 不进 `ctx` 状态 | 只多一行 `ctx.Events`；`Winner` / `ForfeitedBy` / 双方棋盘**一行没动**（离线探针逐条比过） |
| 不进 `Fingerprint` / `StateHash` | 码**不是任何字段**；录像两头用的都是 `StateHash`（`BattleDriver.cs:546` 写 / `:677` 比）**不含事件**，`DeepHash`（`:380`）也不含 ⇒ **旧录像零风险** |
| 不上网 | `MsgAction` / `Fingerprint` / `NetProtocol` **一个字节没碰** |

⚠️ **如实报一个副作用（提请主对话裁）**：`NetProtocol.Fingerprint`（`NetProtocol.cs:337`）里**有 `ctx.Events.Count` 这一项**，所以「多一行 `ctx.Log`」会让它 +1。
它只在对局**已经结束**那一瞬记、且**两端走同一条确定性路径**都记 ⇒ 联机对账仍是**两边同值**（不会分叉）。
⚠️ 但本会话 `A985⑥` 刚在 `RuleCore.cs:5105` 留过一条口径「**⛔ 不许补 `ctx.Log`** —— 那会改 `Events.Count` ⇒ 动到 `Fingerprint`」（那条讲的是**局中每次抽牌**的日志）。
两条口径**有张力**：若主对话要「零 `Events` 变化」，本行应改走 `ctx.Emit(...)`（`A985⑥` 那条路，不进任何哈希），代价 = 断言要换读口（`Forfeit` 那套 token 是 `ctx.Events`）。

### 断言（`RuleEngine/Editor/RuleEngineTest.cs`，新增 `TestBattleVictoryCode`，`:18555`；注册 `:387`）
| # | 断的是什么 | 反面 / 🧨 改坏法 |
|---|---|---|
| ① | P1 督军被打死 ⇒ 含 `[BattleResult] BattleVictory(1) seat=1`，且**不含** `[BattleResult] Forfeit(2)` | 删掉那句 `ctx.Log`、或改码 ⇒ 红 |
| ①′ | 该局 `[BattleResult] ` 计数 **== 1** | 每回合记一次 / token 与措辞各记一条 ⇒ 红 |
| ② | 换成 **P0** 督军被疲劳打死 ⇒ 含 `seat=0`、**不含** `seat=1` | ①+② = **灭自证**：`seat` 写死成任何常数必红一条 |
| ③ | 投降那一路 ⇒ 含 `Forfeit(2) seat=1`、**不含** `BattleVictory(1)` | 「两档都写一条 / 无脑全记」⇒ 红 |
| ④ | 平局 ⇒ `[BattleResult] ` 计数 **== 0**，且日志里**出声**了「不记理由码」 | 「凡分出胜负就记」⇒ 红；⛔ 不许静默 |
| ⑤ | 再调一次 `CheckWinner` ⇒ 仍只 1 条 | 「Winner != 0 早退」被删 ⇒ 红 |

> ⚠️ **踩到并写进注释的一个坑**：④ **不许用裸词 `BattleVictory` 当过滤词** —— 平局那一支的**说明行**里就写着这个词（那是给人看的一句话、不是码）。本仓已有同族教训（「被测代码自己会打印的东西别当断言过滤词」）。

---

## 类型检查 · 行尾 · 没查清

* **秒级类型检查**（`TMPDIR=/tmp/wf_ce2 bash d:/4/Unity/工具/typecheck.sh`，每改完一个文件立刻跑，共 3 次）：`运行时错误数: 0` / `编辑器错误数: 0`。
* **离线旁证**（⛔ 不替代 Unity）：`bash 工具/ruleprobe.sh b19test` ⇒ `PASS 45 · FAIL 0`；
  另建**临时 net8 探针**（`/tmp/wf_vic_probe/`，**未进仓**，编 `RuleEngine/Core/*.cs` + 现成 `Stub.cs`）跑端到端 5 组 14 条 ⇒ **PASS 14 · FAIL 0**（含 ①seat=1 且只 1 条 · ②seat=0 · ③投降只有 `Forfeit` · ④平局 0 条且出声 · ⑤重复调用不翻倍）。
* **行尾**（改前量、改后复量，⛔ 全程只用 Edit 工具、没用 `sed -i`）：`RuleCore.cs` `\r\n`=0 / `\n`=**5204**（改前 5173）**纯 LF**；`RuleEngineTest.cs` **21495 / 21495**（改前 21417）**纯 CRLF** ⇒ 一处没翻。
* **`git diff --numstat`（⚠️ 里面有别人的活）**：`RuleCore.cs` +300/−31 · `RuleEngineTest.cs` +1249/−13。
  **我的 hunk 单独数（按标记串归并）** = `RuleCore.cs` **+31/−0**（`:5124-5144`、`:5164-5173`）· `RuleEngineTest.cs` **+78/−0**（`:387`、`:18536-18612`）。

**没查清 / 差什么**：
1. **平局在原版有没有对应的 `DeadHero` 调用点** —— 没查清（本轮只重读了三处调用点）。差什么：`DrawButton`(6) 是从哪一跳发出的（`DebugDrawBattle.c:14` 是调试入口、正式版被裁）。⇒ 按「没判据就不猜」处理 + 日志出声。
2. **`CardEffectsService` 在正式版里谁 `new` 的** —— `decomp_full` 里**零调用点**（`grep -rn CardEffectsService *.c` 只命中它自己那 4 个文件）。差什么：IL/thunk 表（与 `A913` §6④ 同族）。旁证：菜单那份 prefab **有**那 5 个槽，但驱动它的 `BattleManager.DisplayCard` 在菜单里不存在 ⇒ 我们菜单版不建**已判「与原版一致」**（`Shell/CardDetailPopup.cs:47`）。
3. **`RuleEngineTest.Run` 那套新断言的正式验收还没跑**（红线：写手不跑 Unity）—— 由主对话在同步点按覆盖面跑 `RuleEngineTest.Run`。
4. **顺手发现（另一笔，不在本笔白名单内、我一字未改）**：`Battle/ReplayStore.cs:66` 的注释说 `finalHash` 是 `NetProtocol.Fingerprint(Ctx)`，**实际两头都是 `StateHash`**（`BattleDriver.cs:546` 写、`:677` 比），而同文件 `:28` 写的又是 `StateHash` —— **同文件两说打架**。这条差点把我带错（按注释推 = 「旧录像会假红」；按代码才是零风险）。
5. **⚠️ 并发写手**：本笔开工期间 `RuleCore.cs` 被**别的写手**又加了约 32 行（我改完时 5204 行 → 复量 5236 行），我的两处在 `:5124-5144` / `:5164-5173`，**行号会继续漂** ⇒ 按标记串找：`A985⑩` / `不记理由码` / `BattleResult.BattleVictory` / `TestBattleVictoryCode`。
