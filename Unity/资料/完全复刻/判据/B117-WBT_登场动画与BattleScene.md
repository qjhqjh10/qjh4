# B117-WBT · 登场动画时序 ＋ `BattleScene.Run` 那条红（执行代理 · 2026-11-02）

> 白名单内只改了三个 `.cs`：`Board/DeploySequence.cs` · `Core/EventTiming.cs` · `Editor/BattleScene.cs`。
> 未动 git、未跑 Unity 批处理（编辑期间等 `Unity.exe` 归零才动手）。判据一律来自
> `判据/B117-GD_动画与卡组组装.md` 与 `判据/B117_G4G6_战斗卡牌链与节奏.md`，没有自己重查原版。

---

## 零、一句话

**四件全做完**：登场交接 **0.4 → 0.95 s**、整条序列 **0.75 → 1.3 s**（= 原版）；入场旋转改 `DOLocalRotate`；
`TriggerHold` 判死值；`B117F11` 那条「敌方场上有 0 个单位」的**夹具**补好（断言期望值一个字没动）。
`typecheck 0/0`，聚合行自检 PASS，三个文件**行数守恒**。

---

## 一、逐条改动

### 1️⃣ 登场整条少等 0.55 s（`B117-R15` 的根因）

| 位置 | 旧 | 新 |
|---|---|---|
| `Board/DeploySequence.cs:112` | `HandoffTime = MoveTime + TimeToScaleIntoMoving`（= **0.4**） | `HandoffTime = MoveTime + CardFeel.HandToBoardDone + TimeToScaleIntoMoving`（= **0.95**） |
| `Board/DeploySequence.cs:206`（`PlayQuad`） | `seq.AppendInterval(TimeToScaleIntoMoving)`（0.1） | `seq.AppendInterval(TimeToScaleIntoMoving + CardFeel.HandToBoardDone)`（0.65） |
| `Board/DeploySequence.cs:284`（`Play`） | 同上 | 同上 |
| `Board/DeploySequence.cs:123` | `TotalTime = HandoffTime + HoldTime + LandTime + LandTailTime` | **未改**（它由 `HandoffTime` 推）⇒ 自动 0.75 → **1.3** |

- 🔴 **真正让交接落到 0.95 的是 `:206` / `:284` 那两句 `AppendInterval`**，不是 `HandoffTime` 常量本身 ——
  `seq.InsertCallback(HandoffTime, …)` 只是「绝对时刻」，序列**游标**得先在 0.95 上，否则落下段会提前起步。
  两处必须同源，已在注释里写死。
- 用的是 `CardFeel.HandToBoardDone`（= **0.55**，`Core/CardFeel.cs:562` 的 doc 写明它就是 clip 的
  `CardHandToBoardAnimationFinished` 事件时刻）⇒ **没有在 `DeploySequence` 里再写一个 0.55 字面量**，
  也没有新增 `HandToBoardAnimEventTime` 常量（新增会给本文件加行、撞账的落点）。
- ⛔ `CardFeel.HandToBoard` 的 17 条曲线**一个字没动**（clip 起播点 0.3 与全部内部关键帧本来就是对的）。
- 文件头 `:38-51` 的五段表 + 全文件所有 `0.4` / `0.75` 文案**就地改写**（不增行）。

### 2️⃣ 入场旋转改局部角

- `Board/DeploySequence.cs:221` 与 `:310`（同一句复制过两处）：`tr.DORotate(Vector3.zero, MoveTime)` → `tr.DOLocalRotate(...)`。
- 判据：`decomp_full/MinionManager__MoveMinionToConversionPoint.c:41-47` 是 **`DOLocalRotate`**。
  今天父链无旋转 ⇒ 零可见差；同坑 `Hand/CardInteraction.cs:929-935` 已踩过。判据已写进 `:309` 那行注释（不增行）。

### 3️⃣ `TriggerHold` 死值

- `Core/EventTiming.cs:953` 首句：`本常量【不再】用作触发前置 … 保留只为记账` →
  **`死值 · 无任何生产消费者`**（`DelayBetween(next: Trigger)` 已是 0，`:424`；全仓只剩定义 + 注释）。
- `Core/EventTiming.cs:11` 文件头原写「挖到了**两条**真正的动作间隔（`AttackWindUp` 和 `TriggerHold`）」——
  同步改成「`TriggerHold` 已判死值」。常量本身**保留**（账的落点候选，删了会动行号）。

### 4️⃣ `BattleScene.Run` 唯一那条红（`B117F11` E2）

- 现读：`（前提）敌方场上至少 3 个单位（实测 0）—— 不够 ⇒ E2 这一条没验`。
- 根因：`CheckManaBuffHud(driver, Check)` 在这条之前刚 `driver.Begin(...)` 开了**新的一局** ⇒
  敌方棋盘上只有督军（`BoardSpec.WarlordSlot = 4`，**不是可部署格**）⇒ E2 数出 0。
- 改法（**在调用点补夹具，⛔ 没动断言期望值**）：`Editor/BattleScene.cs:45280`（方法尾聚合行，**同一物理行内插入**）
  在 `B117F11_Assert.RunWithDriver(driver)` 之前把敌方**空的可部署格**补满 3 个引擎单位
  （`CardDatabase.Load()` 里第一张 `Type == "unit"` 的卡 → `ctx.NewInstance` → `new UnitState(..)`，
  补 `DeployedTurn` / `PlayOrder`）；`RunWithDriver` 第一句 `driver.RefreshAll()` 会把视图同步出来。
- 修完 E2 的 **4 条断言会真的跑**：①前提 ②单目标 `span ≈ HitWaitSeconds 1.0` 对照
  ③3 目标 `span ≈ 单目标`（= AoE 同刻起播）④`WaitingForBlockingAction` 仍锁（`Frozen` 不随条数线性增长）。
- ⚠️ 夹具用的是 `/* … */` 块注释 —— **这一行上任何 `//` 都会把后半行全部代码吞掉**（本项目一天踩过 6 次）。

---

## 二、新时序的三个数（验收口径）

| 拍 | 旧 | 新 | 原版 |
|---|---|---|---|
| 交接（换 3D 体 / 层 / 缩放） | 0.40 | **0.95** | 0.95 |
| 落地（`LandOnGround`） | 0.65 | **1.20** | 1.20 |
| 收尾 / `onDone`（引擎结算） | 0.75 | **1.30** | 1.30 |
| 落下起点 | 0.45 | **1.00** | 1.00（= clip 里 `Board Elements` 从 −1.347 开始回到 0） |

⇒ 自洽关系全部成立：`0.3 + 0.55 + 0.1 = 0.95` · `0.95 + 0.05 = 1.0 = 0.3 + 0.7` · `1.3 ≥ 0.3 + 0.9167`。

---

## 三、断言

**新增 3 条**（`Editor/BattleScene.cs:1372`，与原有那条**同一物理行**追加；三条都断「原版关系式」，不是断我们自己的常量）：

1. `HandoffTime == MoveTime + HandToBoardDone(0.55) + TimeToScaleIntoMoving` —— 交接 = 飞 + clip 事件 + 等；
   🧨 退回只等 0.1 ⇒ 0.4 s ⇒ 红。
2. `HandoffTime + HoldTime == MoveTime + HandToBoardDissolveEnd` —— 落下起点 = `Board Elements` 开始回到 0 的时刻
   ⇒ **落地与浮起同相位**；🧨 交接早 0.55 ⇒ 落地之后再浮 0.35 s ⇒ 红。
3. `TotalTime >= MoveTime + HandToBoardEnd` —— 整条覆盖 clip 全长、零尾巴。

**改写 1 条**：`:1370` 原来是 `HandoffTime < CardFeel.HandToBoardDissolveEnd`
（**绝对时刻**比 **clip 局部时刻**，口径本来就是混的）⇒ 改成 `HandoffTime < MoveTime + HandToBoardDissolveEnd`（= 1.0）。
`0.95 < 1.0` 成立，正是「交接落在 `2DCard.m_IsActive →0`（绝对 1.0）之前 0.05 s」那条原版关系。

**文案**：`:22985` 原写「四段之和」（`TotalTime` 早就是五段）⇒ 改成「五段之和」。

---

## 四、跑了什么

| 检查 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_wbt bash Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `python -I -X utf8 Unity/工具/check_aggregate_lines.py Editor/BattleScene.cs` | **PASS**（1 聚合行 / 55 调用 / 0 违规） |
| 手工核 `:45280` 的插入点 | `var _e2ctx` 在**首个 `//` 之前**（`//` 在 2208 字符处，夹具在 981） |
| 行数守恒 | `DeploySequence.cs` 336 · `EventTiming.cs` 1503 · `BattleScene.cs` 51301（**与改前一致**） |
| 行尾 | 三个文件 `crlf == lines`（前两个 LF、`BattleScene.cs` CRLF），`bareCR = 0` |
| `git diff --numstat` | 49/49 · 2/2 · 7/7 —— 全是**逐行替换**，零行数漂移 |
| `check_ledger_points.py` 全表 | 没有一条「漂移 / 失败」指向这三个文件 |

---

## 五、没做成 / 需要别人接的（🔴 第 1 条是真的跨文件连带）

1. 🔴 **AI 登场 clip 的起播会跟着晚 0.65 s** —— `BattleDriver.cs` **三处**把 `StartHandToBoardClip(v)` 写在
   `Handoff.Apply` 里（`:25928` 手牌载体 · `:26023` 兜底载体 · `:37923` 另一条），而 `Apply` 现在在 **0.95 s** 才跑
   ⇒ 这三条 AI/兜底路径的 `Card Hand To Board` 会 0.95 起播、跑到 **1.867**，比序列 1.3 还长，
   内部相位（3D 体 0.4667 出现、2D 1.0 关、`Board Elements` 1.0→1.2167 浮回）整体后错 0.65 s。
   原版 AI 走**同一个** `_MinionPlayedIntoField`，clip 应当在**飞完那一刻（0.3）**起播（玩家侧就是这么排的，`:13842`）。
   **不在我的白名单（`BattleDriver.cs` 禁写）** ⇒ 未动。建议两个修法二选一：
   ① 把这三处改成 `ScheduleAction(DeploySequence.MoveTime, …)` 起播（要处理「AI 场上视图此时还藏着」）；
   ② 给它留一个「从 clip 的 0.65 偏移开始播」的窄口。**取哪个要按 AI 卡背/场上视图的可见性判，我无法验证。**
2. `BattleDriver.cs` 的**文档漂移**（行为无关）：`:13802` / `:13808`（0.75）· `:13845`–`:13855`（0.4 s）·
   `:13860` · `:13884` · `:13900` · `:21477`（缺 0.55 那一项）· `:25926`（0.4 s）。
3. `判据/B114_R2_DOTween_API清单.md:101` 把 `DORotate` 列在 `DeploySequence.cs:221 / :310` —— **已过期**（现为 `DOLocalRotate`）。
4. `Editor/CardBaseDemo.cs:212` 注释仍写「原版三段：飞 0.3 + 悬停 0.05 + 垂直落 0.2 = 0.55 s」—— 早就是五段。
5. **运行时一条都没验**（按指令不跑 Unity）：`BattleScene.Run` 里与落位相关的几条
   （`:6896` `landSec <= TotalTime + 0.25` · `:22984` `Dur(Play) == TotalTime` · `:23419` 单位卡 ≠ 战术卡）
   都是**按符号取值或在余量内**，但只有真跑一次才知道；**E2 那 4 条同理**。
   ⚠️ 如果 E2 真跑出来是红的，那是 `B117-37`（AoE 并行）的实现问题，**不是我这条夹具**的锅 —— 但**不许**再把期望值改掉糊过去。
6. `B117-41`（`DeathHold` 无第二消费点）我没动账 —— 那是 `B117-GD` 的 A4，只提示。

---

## 六、顺手发现 / 坑（记下来）

- 🔴 **往 `BattleScene.cs` 的聚合行里加代码，`new_string` 必须是【单行字符串】。**
  我第一版用多行 `new_string` 插了 6 行 ⇒ 文件 **+5 行**（51301 → 51306）**且**新行是 LF（CRLF 文件混行尾）。
  已用二进制脚本压回同一物理行（`crlf == lines` 复验）。**Edit 的多行 new_string 不会自动跟随文件的行尾。**
- `Editor/BattleScene.cs:22985` 文案「四段之和」与 `TotalTime`（五段）不符 —— 已改（见 §三）。
- 判据里说的「交接 0.95 = 0.85 + 0.1」中那个 0.85 = `GetMinionConversionTime` = **飞 0.3 + clip 事件 0.55**
  —— 也就是说 `DOLocalMove(0.3)` 与那 0.85 的等待是**同一刻起算**的，所以序列里要写成「飞 0.3 → 再等 0.65」，
  不是「飞 0.3 → 再等 0.85」。这一条容易写错，已在 `DeploySequence.cs:41-43` 的三行注释里逐项拆开。
