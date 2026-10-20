# W58-BTFIX-TEST · `BattleScene.Run` 夹具/断言侧修复（P1 + P2）

> 代理 `W58-BTFIX-TEST`（战斗线自检执行代理）。
> 白名单：**只改** `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`；其它文件只读。
> ⛔ 未跑 Unity / 未跑任何 `*.Run` / 未动 git；验证只做**秒级类型检查**（Roslyn `csc`）。
> 判据 = `资料/普查产出_1020/W58_诊断_BattleScene50条.md`（下称「诊断」）。

---

## 结论

- **P1（D/E/F/G/H/B）与 P2（J/K/L/M/N/O/P/Q/R）全部在夹具/断言侧落地**，共 15 处。
- 核心是新增**唯一一个**夹具助手 `DrainQueue(BattleDriver)`（`BattleScene.cs:33663`）：把「已经排上时间线的事件」
  推到队尾之前，解决 2026-10-19「新一批事件不许插队（`max(_clock, _timelineTailAt)`）」让旧夹具
  「排一批事件 → 立刻断言」集体失效的问题。
- **没有把任何断言改成恒真 / 放宽语义**；有改动的断言全部是**收紧**（G 先手侧、H 的 `seen`、
  L 的两条前提、N 的六项文字、O 的精确落点）。
- 生产侧（C 运镜 / I 气泡宽度 / A `OpenCommand` 裸下标 / G 的 `TickIntroMonologue` 顺序与 `Begin` 清气泡）
  由另一代理负责；其中 **G 的生产修复已经出现在工作区**
  （`Battle/BattleDriver.cs:3350` = `Begin` 里 `_unitChat.HideAll() + StopVoice()`；
  `:6345-6350` = 收尾判定挪到「气泡还在 ⇒ return」之后）。本节夹具按此对齐并另加双保险。
- `typecheck`：**运行时 0 / 编辑器 0**（两次，最后一次在全部改动之后）。

---

## P1 逐组（改前 → 改后 + 诊断出处）

### 通用助手 · `DrainQueue`（诊断 §二）

- **改前**：没有；各夹具「`RefreshAll()` 之后立刻 `Step(0.6/0.3/5)` 再断言」。
- **改后**：`BattleScene.cs:33663` 新增

  ```csharp
  static float DrainQueue(BattleDriver d, float maxSec = 20f)   // 逐 1/60 步推，TimelinePending==0 且 Clock>=Tail 就退出
  ```

  口径：**在「排下一批事件」之前**调（排空旧队），之后新事件从当前 `Clock` 起排。
  ⛔ 不在「已排被测事件之后」调（那会把中间态整段播完）。推不完时**照旧返回**、让断言自己红（不静默）。
- **出处**：诊断 §二；`BattleDriver.cs:13635`（起算点）/ `:14134`（`TimelineTailAtForTest`）/ `:14125`（`TimelinePending`）。

### D · 攻击/命中/阵亡手感 7 条（诊断 §一·D）

- **改前**：`Check(atkView != null)` 之后直接 `SimulateOpenCommand/Command/Resolve`，再 `t0 = drv.Clock` 采样
  ⇒ 事件实际起播在 `t0 + 0.503`（队尾），采样点全落在 `Attack` 之前（`battle2.log:51335-51360`、
  `t0=153.147` / `Attack@153.65`）。
- **改后**（`BattleScene.cs:16135` 附近）：在发起攻击**之前** `DrainQueue(drv);` —— 攻击事件从当前 `Clock`
  起排，`hitAt/deathAt` 的推导（本来就由 `EventTiming` 推）恢复成立。`t0 = drv.Clock` 与静止位取样都在排空之后。
- ⛔ 没动 `Mathf.Max(_clock, _timelineTailAt)`（既定口径）。

### E · 回手动画 6 条（诊断 §一·E）

- **改前**：`ctx.Signals.Add(Return)` + `RefreshAll()` 后立刻断 `ReturningCount == 1` ⇒ `Return` 在队尾没轮到
  ⇒ `ReturningCount==0`、材质/alpha/抬升全停在初始态（`battle2.log:15453-15612`）。
- **改后**（`:6852`）：摆好 Tide Minion 并 `RefreshAll()` 之后、发 `Return` 之前 `DrainQueue(driver);`
  ⇒ `Return` 从当前 `Clock` 起排、`PlaySignals` 末尾的 `AdvanceTimeline(0f)` 当场起播
  ⇒ `Step(0.35)` 量到的是「已起播 0.35 s」。`rv` 也在排空之后才取（旧事件可能重建视图）。
- ⛔ 没放宽 `ReturningCount` 的判据。

### F · oath 3 + bloodthirst 2 + rally 1（诊断 §一·F）

- **改前**：`Step(0.6)` 里播的是**更早排的旧事件**（日志 `15900/15961` 冒出 `AttackHitSmall`），
  oath/bloodthirst 的 `Trigger` 仍在队尾之后 ⇒ `TriggerPlayCount==0`；rally 的 `Deploy` 在队尾 ⇒
  `SyncBoard` 被 `IsPendingDeploy` 压着不建视图 ⇒ `MyUnits[rallySlot]` 为 null（日志 `20299`）。
- **改后**：
  - oath（`:6985`）：给单位加 `oath` 之后、进入 3 次触发循环**之前** `DrainQueue(driver);`，
    并在排空之后重新取 `tv2/tf2`（旧事件可能重建过视图，提前取的组件会失效）。
  - bloodthirst（`:7042`）：加 `BloodThirst` 之后、记 `btPlays0` 之前 `DrainQueue(driver);`。
  - rally（`:8833`）：`SimulatePlay` 之后、读 `rallyView` 之前 `DrainQueue(driver);`。
- 证据：诊断 §一·F；`SyncBoard` 待播登场闸 `BattleDriver.cs:17482`。

### G · 无换牌开场白 3 条（诊断 §一·G）

- **改前**：上一局（换牌路）后手 Goff 的**残留气泡**还在；新局 `Begin` 不气泡 ⇒ `TickIntroMonologue`
  被「气泡还在 ⇒ return」挡住；测试只 `Step(1s)` 且 `spokeNM` 只认 `LastEvent=="Intro" && ShownSide==firstNM`
  ⇒ 读到的是上一局的 `GOF77-Chat0` / `VO_Ork_…`（日志 `49427/49440/49453`）。
- **改后**（`:15372` 起）：
  1. 新局 `Begin` **之前** `chat.HideAll(); chat.StopVoice();`（夹具双保险；生产 `Begin` 现已同样清）。
  2. `Begin` 之后新增前提：`chat.ShownSide == -1 && !chat.IsSpeaking`（新局没有残留）。
  3. 先手侧**钉死为 0 侧**：`Check(firstNM == drv.MySeat && drv.MySeat == 0, …)`。
     依据（离线读数据）：`MakeDeck` 取该阵营卡池第一张 `type=="hero"` ⇒ 我方 **UM76 Lieutenant Titus** /
     敌方 **GOF28 Ghazghkull Thraka**；`card_chat_intro.json` 里 `UM76.talksFirst = null` ⇒
     原版 `IsPlayerFirstIntro` 落「玩家先说」（`UnitChatPanel.PlayerTalksFirst` 兜底 `return 1`）。
  4. 等待预算 1 s → **10 s**（只是等它开口，不改语义）。
  5. 期望值**钉在原版数据**上：文本键 `Chat_Messages/UM76-Chat0-Goff`、音频 `VO_UM_Titus_vsOrk`
     （`card_chat_intro.json` 的 `introByArmy.Goff`；本次已离线核对）。
- ⚠️ 未跑 Unity ⇒ 实际读数拿不到；若生产修复没落地，本节会按上面 5 条给出具体红点。

### H · 聊天/气泡 4 条（诊断 §一·H）

- **改前**：上一节 `Forfeit → SpeakConcede` 的 Concede 气泡还在；`seen` 只认 `ShownSide==0` ⇒ 被旧气泡
  立刻满足（`LastEvent/LastCardId` 全是旧的）；`Step(5f)` 里 `_unitChat.Advance` 排在事件泵**之前**
  ⇒ 本步新建的气泡同一 `Step` 永不收起（日志 `48452-48491` / `48604`）。
- **改后**：
  1. 本节开头 `chat.HideAll(); chat.StopVoice();`（`:14936`）。
  2. `seen` 改为 **`ShownSide==0 && LastEvent=="Deploy" && LastCardId==card.Card.Id`**（`:14964`）。
  3. 收尾：`DrainQueue(drv); chat.Advance(10f);` 再断 `ShownSide == -1`（`:14991`）。
     气泡停留 = `clamp(文本长,1,4)+0.4 ≤ 4.4 s` ⇒ 10 s 足够。
- 证据：`BattleDriver.cs:14325`（`_unitChat.Advance` 早于事件泵）/ `:4794-4805`（`SpeakConcede`）。

### B / 13 · 联机席位视图 null 2 条（诊断 §一·B / §三·2）

- **改前**：`DeployFree` 之后直接 `SetMySeat(1); RefreshAll()` 读 `MyUnits/FoeUnits` ⇒ `Deploy` 排在队尾、
  `IsPendingDeploy` 压着不建视图 ⇒ 两边都 null（日志 `70192/70218`）。诊断没分清「还是 pending-deploy」
  还是「中途翻座位没重映射身份表」。
- **改后**（`:21811` 起）：
  1. 两次 `DeployFree` 之后、翻座位之前 `DrainQueue(driver);` ⇒ 两条 `Deploy` 在这一步演到、视图建出来。
  2. 两处「两边视图都建出来了」的失败文案**加读数**：`MyUnits/FoeUnits` 各自拿到的 `Data.id` +
     `DeployPendingForTest(owner, slot)`（两个座位两格）⇒ 一次就能分清是哪一类。
- ⚠️ `SetMySeat` 中途翻座位不重映射 `_myUnitViewByUnit/_foeUnitViewByUnit`（`BattleDriver.cs:2804`
  只写 `_me`）仍是**生产侧未支持场景**（诊断 §B 已记）；本夹具只负责排空 + 把原因打出来，未改生产。

---

## P2 逐条

### J · `desc` hash（诊断 §一·J）

- **改前**：期望 `0xDD0B8C87`（第五十五/五十六会话口径，HEAD 已 stale）。
- **改后**（`:17472`）：期望 **`0x091039AB`**，注释写清来源：第五十七会话 + 本批 `SAU28`（`+ Flying.`）/
  `UM80`（`+ Vanguard.`）两张 `desc`。**离线复算**（同一算法、`cards_engine.json` 1127 张、逐字节）得
  `0x091039AB`，与诊断一致。

### K · 中文残留 1 张 = `GOF27`（诊断 §一·K）

- **改前**：⑥ 的 `residual` 计数把 `GOF27` 算进去 ⇒ 1 张。
- **改后**（`:17543`）：只在 ⑥ 的残留计数里把 **`GOF27` 记白名单**（覆盖检查不变），注释写清：
  官方 zh 正文的 `[[盔甲]]/[[震荡的]]/[[不稳定]]/[[集结]]` 是**原版规则内容里的中文 trait token**，
  `Card_Trait/*` 与 `Atlas_trait_icon_*` 都没有中文名 ⇒ 原样保留 + 出声（日志 `27814/27841/27868/27895`）。
  ⛔ 没加中文→英文别名表；数据侧「要做」= 补那四条 `Card_Trait/*`（**不是不做**）。
- ⚠️ 白名单真理源在 `BattleDriver.OfficialFaceTextWhitelist`（生产文件、非本代理白名单）；生产侧补进去之后幂等。

### L · T16 输入 2 条（诊断 §一·L）

- **改前**：写死卡局部 `x = 0.96 / 0.98 / 1.00`（假定半宽 0.985，余量 1.5%），且没断「按下的就是 `cvT16`」。
- **改后**（`:19865`）：
  1. 边界点改成**真 collider 实测矩形**：`halfW = ColliderSizeForTest.x/2`、`dy = ColliderOffsetForTest.y`；
     内侧 `0.90×halfW`、外侧 `1.10×halfW`（位移 `0.04×halfW`，仍 ≪ `TapThreshold` 0.3）。
  2. 前提①：`cvT16.HitByCollider(inB) && !cvT16.HitByCollider(outB)`（真 collider 自己的判据）。
  3. 前提②：`itT.HitCardForTest(inB) == kT16`（`Frontmost`/`HitTest` 命中的就是 `cvT16`，不是邻卡）。
  ⇒ 若还红，**红在上面前提里**，就能直接分清「倾摆/边界余量」还是「Frontmost 取到邻卡」（诊断 §三·1 要的读数）。
- ⛔ 没把 `TickPressCandidate` 的判据改回 `Contains`。

### M · T03 换牌行 y 边界（诊断 §一·M）

- **改前**：`edgeM03` 只返回 `ix + (ox-ix)*lo` ⇒ 纵向两条 `ix=ox=0`，恒 0.0000（日志 `65211`）。
- **改后**（`:20327`）：签名加 `bool vertical`，返回**插值后的那个轴**（`vertical ? ly : lx`）；
  四个调用点分别传 `false/false/true/true`。命中判断本身没动。

### N · 候选 6 张（诊断 §一·N）

- **改前**：`ChooseOptionCount == 3`（`Hyper-adaptation` / `Infinite Biomorphologies`）。
- **改后**（`:21091` / `:21391`）：`== 6`，并逐项核 `ChooseOptionName(0..5)` =
  `+1 Armour / +2 Melee Attack / +2 Ranged Attack / +2 Health / Stealth and +1 Armour / Vanguard`
  （判据 = `RuleEngine/Core/EffectResolver.cs:3786-3794` 的 `LeviathanEffectPool` 六项 SO 真值）。
- ⚠️ `EffectResolver` 那段「三等分/六等分」**注释**在生产文件里（非本代理白名单）⇒ 未改，留给对应代理。

### O · 抽卡落点 2 条（诊断 §一·O）

- **改前**：期望 = 行基线 `DefaultBaselineY/EnemyBaselineY`；而 `HandRow01` 在手牌非空时返回
  `ToNormalized(SlotPosition(count-1,count))` = 基线 + 末尾那张的弧高（日志 `2403/2416`）。
- **改后**（`:1105`）：期望改成**同一个算式**（`driver.hand/foeHand` 的 `SlotPosition(count-1,count)`；
  `count==0` 才退回基线）。⛔ 不另算一份坐标。

### P · 日志面板滑入 2 条（诊断 §一·P）

- **改前**：本节**两次** `drv.ShowBattleLog()`（先合成 `#i` 事件刷内容、再真正点开）；第二次的 `SnapSlide()`
  把正在滑的补间一把推到底 ⇒ 「刚点开仍在 −1200 / 半程」两条必红（日志 `9659/9672`）。
- **改后**（`:4465` / `:4586`）：把合成 `#i` 事件那一块**整块搬到滑入三条断言之后**；搬完之后再
  `ShowBattleLog()` 一次把合成行刷掉（面板已停在 finalX 87，`SnapSlide` 是空转，不影响滑入断言，
  也保证后面「行内卡名是链接」读到的是真日志）。

### Q · 换牌面板前提 1 条（诊断 §一·Q）

- **改前**：`RunCore` 用 `BuildScene` 新建 driver，`mulliganEnabled` 字段初值 false ⇒ `Begin` 走无换牌支。
- **改后**（`:12742`）：`Begin` 前后 `mulliganEnabled = true`（`try/finally` 还原；该字段只在 `Begin` 里读一次）。

### R · 敌方飘字比 0.0000（诊断 §一·R）

- **改前**：分母 `pop1` 在 ④‴ 末尾已被 `DestroyImmediate` ⇒ Unity `!= null` 为 false ⇒ `got = 0`。
- **改后**（`:15905`）：销毁**之前**把玩家那档字号存进 `fPlayer1Px`，④⁗ 用 `fPlayer1Px` 当分母。

---

## 未查清 / 未做

1. **未跑任何 `*.Run`**（按指令）⇒ 以上全部是**离线判据 + 类型检查**，没有实测绿红。
2. **T16**：真 collider 的实测半宽与倾摆差没有运行时读数；两条前提会把原因打出来（见上）。
3. **联机 B**：`SetMySeat` 中途翻座位的身份表重映射仍是生产侧未支持场景；若排空后 `Data.id` 仍对不上，
   失败文案会打出两边字典与 `DeployPendingForTest`，需要生产侧决定「支持中途翻」还是「显式不支持 + 出声」。
4. **G**：生产修复是否已经生效只能等下一次 `BattleScene.Run`；期望值已按原版数据钉死
   （`Chat_Messages/UM76-Chat0-Goff` / `VO_UM_Titus_vsOrk`），不符合时红点会直接给出实际读数。
5. **K / N 的生产侧半句**：`BattleDriver.OfficialFaceTextWhitelist` 加 `GOF27`、
   `EffectResolver` 的「三等分/六等分」注释 —— 都在非本代理白名单的文件里，未改。
6. **`DrainQueue` 上限 20 s**：刻意保留（推不完就让断言红，不静默跳过）。
7. **C（T12 运镜）/ I（T09-C3 气泡宽度）/ A（`OpenCommand` 裸下标）**：诊断定为生产侧 P0，本代理未碰。

---

## typecheck

```text
TMPDIR=/tmp/wf_bttest bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

行尾核对：`BattleScene.cs` = **33702 行、CRLF 33702 / 孤 CR 0**（整篇仍是 CRLF）。
`git diff --numstat`：`1756 116`（基线 1497/55 ⇒ 本代理 +259/−61，其余为本批其它代理的既有改动）。
