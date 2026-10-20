# W58 诊断 · `BattleScene.Run` 50 条失败 + 1 次崩溃（只读诊断）

> 诊断代理 `W58-DIAG-BT`。⛔ 未跑 Unity / `*.Run` / player / git 写操作；只读日志、源码与 `git diff`。
> 输入日志：`d:/4/_tmp_view/battle2.log`（50 条 `BT ✗` + `BattleScene.cs:21740` 崩溃）。
> 分类口径：**(α) 断言过期 · (β) 本批实现缺陷 · (γ) 既有回归（本批前就红）· (δ) 夹具前提不成立**。
> 「本批」= 工作区未提交改动；「HEAD」= `5f211be6`（第五十七会话）。

---

## 〇、一句话结论（先看这张表）

**50 条里只有 8 条是【本批新增缺陷】（T12 开场运镜 4 条 · T09-C3 气泡宽度 4 条），
1 条崩溃是本批新测试触发的既有健壮性缺陷（`OpenCommand` 裸下标）。**
其余 41 条 = **既有回归（γ）/ 夹具前提不成立（δ）/ 过期断言（α）** ——
主要是 **2026-10-19 那批「排时间线不插队 + 待播队列尾」(`_timelineTailAt`)**
和 **2026-10-20 本批新加的开场运镜** 两件事，让一批「假定 `RefreshAll()` 之后事件立刻播完」的旧夹具失效。

| # | 组（失败条数） | 分类 | 根因一句话 | 优先级 |
| --- | --- | --- | --- | --- |
| A | 崩溃 `KeyNotFound '1'`（1 次崩溃） | **β（既有）+ δ** | `OpenCommand` 裸下标 `_myUnits[slot]`；视图被「待播登场」压着还没建 | **P0** |
| B | 联机席位 2 条 | δ（+β 既有） | `DeployFree` → `RefreshAll` 时 Deploy 事件排在**未来**（队尾）⇒ 视图按设计延后建；且 `SetMySeat` 中途翻座位不重映射视图表 | P0 |
| C | **T12 开场运镜**：数值探针 2 + 落点往返 1 + 两行在屏 1 | **β（本批）** | 新 `CameraIntro` 在 `Begin` 把战场相机摆到 clip 的 t=0（−7.14,1.61,1.07），批处理不再有帧循环 ⇒ 相机**不回本位**，所有走 `boardCam` 的屏幕量测全错 | **P0** |
| D | 攻击/命中/阵亡手感 7 条 | δ | 攻击那一批事件实际起播在 `clock+0.503`（队尾），测试按 `clock+0.3` 采样 ⇒ 全部采在事件之前 | P1 |
| E | 回手动画 6 条 | δ | `Return` 事件没轮到（队尾）⇒ `PlayReturnFeel` 没跑 | P1 |
| F | oath 3 + bloodthirst 2 + rally 1 | δ | 同上：`Trigger`/`Deploy` 事件在 `Step(0.6/0.2)` 内没轮到；`Step` 里播的反而是**更早排的旧事件**（日志里冒出 `AttackHitSmall`） | P1 |
| G | 无换牌开场白 3 条 | **β（测试/既有顺序）** | 上一局第二条气泡还亮着（`TickIntroMonologue` 在「气泡还在」之前就把 stage 判 −1）⇒ 新局先手 0 被跳过 | P1 |
| H | 聊天/气泡 4 条（Scout Bike 3 + 自收 1） | δ | 上一节的 `Concede` 气泡残留 ⇒ `seen` 误判 + `LastEvent/Clip` 是旧值；`Step(5)` 内的 `Advance` 早于事件播放 ⇒ 新气泡不会在同一 `Step` 内收掉 | P1 |
| I | **T09-C3 气泡宽度**：world 1 + C3③ 3 | **β（本批）** | `RefreshLayout()` 新加的 `if (_ugui) return;` 把**唯一**一次 `SetAspect(BubbleW/BubbleH)` 删了 ⇒ 宽度取贴图比例 646.996（原版 648.774） | **P0** |
| J | desc hash 1 | α（HEAD 已 stale） | 期望 `0xDD0B8C87` 是第五十五/五十六会话的数据；第五十七会话就变了（`0x661B0746`），本批又改 2 张（`0x091039AB`） | P2 |
| K | 中文残留 `[[…]]` 1 | γ | `GOF27` 的官方 zh 正文有四个**中文 trait token**（`盔甲/震荡的/不稳定/集结`），`Card_Trait/*` 里没有、也没有图 ⇒ 按原版规则原样保留 | P2 |
| L | T16 输入 2 | δ（+γ） | 判据从 `Contains` 换到真 `BoxCollider2D` 后边界余量只剩 1.5%（文档已记 ±10° 倾摆可差 2.6%）；且没断「按下的那张卡就是 `cvT16`」 | P1 |
| M | T03 换牌行 y 边界 1 | γ（测试 bug） | 二分助手 `edgeM03` **只返回 x**；纵向两条的 x 恒 0 ⇒ 实得 0.0000 | P2 |
| N | 候选 6 张 2 | γ（断言过期） | `LeviathanEffectPool` 自第五十五会话 E10 起就是 **6 项**（SO 真值），测试仍写 3 | P2 |
| O | 抽卡落点 2 | γ | 断言比 `DefaultBaselineY`，而 `HandRow01` 返回的是**末尾那张卡的 `SlotPosition`**（基线 + 弧高） | P2 |
| P | 日志面板滑入 2 | γ | 同节调了两次 `drv.ShowBattleLog()`；第二次 `Show()` 里 `SnapSlide()` 把正在滑的补间**一把推到底** | P2 |
| Q | 换牌面板前提 1 | γ | `RunCore` 是 `BuildScene` 新建 driver（`AddComponent`），`mulliganEnabled` 默认 **false**；场景里那份 `1` 只在 `BuildAndSaveScene` 才有 | P2 |
| R | 敌方飘字比 1 | γ（测试 bug） | 分母 `pop1` 在上一条 assertion 里**已经被 `DestroyImmediate`**；Unity 的 `!= null` 对已销毁对象为 false ⇒ `got = 0` | P2 |

> ⚠️ 另：**日志面板 48114 那条「气泡 648.77×236.50 px」也属于 I 组**（同一个宽度根因），所以 I 组实际是 **4 条**：
> `48114`（`BubbleSizePxOf`）+ `48179/48257/48335`（C3③ 三颗气泡）。

---

## 一、逐组：根因 / 修法 / 证据

### A. 崩溃 —— `BattleDriver.OpenCommand` `_myUnits[1]` KeyNotFound

- **现象**：`BattleScene.cs:21740`（宿主视角 `SimulateOpenCommand(foeSlot)`）抛出 `KeyNotFoundException: '1'`，整条 `BattleScene.Run` 终止。
- **根因**：
  1. `OpenCommand` 末尾 `_myUnits[slot].SetHighlight(...)`（`Battle/BattleDriver.cs:10244`）**裸下标**。这里能走到 `_myUnits[slot]` 的条件只有「`Ctx.Players[_me].Board[slot] != null`」，**没有保证视图存在**。
  2. 视图不存在是**设计使然**：`DeployFree` 发了 `Deploy` 事件，`PlaySignals` 里 `NotePendingDeploy`（`BattleDriver.cs:14770-14790`）把这一格记成「登场还没演到」；`SyncBoard` 的 `IsPendingDeploy` 闸（`BattleDriver.cs:17455`）**不建视图**，等事件演到那一拍才建。事件排在 `max(_clock, _timelineTailAt)`（`:13635`）—— 队尾在前面 ⇒ 本帧不播 ⇒ 视图没有。
  3. 测试随后 `Check(driver.SimulateOpenCommand(foeSlot), …)`（`Editor/BattleScene.cs:21740`）恰好点了**这一格**。
- **修法**：
  - **实现（推荐，P0）**：`OpenCommand` 里 `_myUnits[slot]` 改成 `TryGetValue` + 空判（视图不在 = 这一下什么也不演，⛔ 不要抛）；`SetSelectedSlot`/`ClearSelection` 一族的其它裸下标顺手核一遍。
  - **夹具（同批）**：`SimulateOpenCommand` 前先 `Step` 到 `At`（见下面 §二 的通用修法），或直接断言视图存在再点。
- **证据**：日志 `70387` 附近调用栈（`OpenCommand:10244` ← `SimulateOpenCommand:22524` ← `BattleScene.cs:21740`）；`SyncBoard` 闸 `BattleDriver.cs:17455`；`NotePendingDeploy` `:14770-14790`。

### B. 联机席位 2 条（客机/主机「两边的场上视图都建出来了」）

- **分类**：δ（夹具前提）+ β（既有 `SetMySeat` 缺陷）。
- **根因**：
  1. 测试在 `RuleCore.DeployFree(ctx,1,…)` / `(ctx,0,…)` 之后**直接** `SetMySeat(1); RefreshAll();`（`Editor/BattleScene.cs:21655`）就 `Lookup(driver.MyUnits, mineSlot)`。`Deploy` 事件被排到 `max(clock, tail)`；若 `tail > clock`，`AdvanceTimeline(0f)` 不播 ⇒ `_pendingDeploy` 压着 ⇒ 两边都 `null`。这与 A 是**同一个根**。
  2. 即使事件当期播了，`SetMySeat` 中途翻座位也**不重映射** `_myUnitViewByUnit`/`_foeUnitViewByUnit`（`BattleDriver.cs:987`）与 `_myUnits`/`_foeUnits` —— 两张身份表**按座位语义命名**、而 `UnitViewMap(owner)` 用 `owner == _me` 选表；翻完之后「我的」那张表里装的是**对方**的映射。生产路径里 `SetMySeat` 只在 `Begin` 之前调（联机 attach），所以没暴露；测试在**对局中途**翻，属于未支持场景。
- **修法**：
  - 夹具：别在中途翻座位验「视图归属」，改为**重开一局**（`Begin` 前 `SetMySeat(1)`）再摆单位；或者翻完先 `Step` 到事件演完。
  - 实现（若要支持中途翻）：`SetMySeat` 里交换两张身份表 + 两张格号表（`_myUnits`/`_foeUnits`、`_myFading`/`_foeFading`、手牌视图归属），或**显式声明不支持**并在 `SetMySeat` 里出声 + 清空视图重建。
- **证据**：`BattleScene.cs:21655-21679`；`SetMySeat` `BattleDriver.cs:2804`（只有一句 `_me = seat`）；`UnitViewMap` `:987`；日志 `70192/70218`。

### C. **T12 开场运镜让所有 `boardCam` 量测失真（本批 β，4 条）**

- **现象**：`4075/4089` 数值探针卡四格跑到屏外（stat2@(1088,57)、stat4@(1453,34)）；`46978` 落点往返 **0/18**；`47041` 敌方行 **−32.5% 距顶**（跑出屏幕）。
- **根因**：本批新增 `Battle/CameraIntro.cs`（**untracked，新文件**），`Begin` 在战场载入后 `StartCameraIntro(key, loader.Current)`（`BattleDriver.cs:3738`），`Play()` 里 `Sample(0f)`（`CameraIntro.cs:288`）把 **BoardCamera 直接摆到 clip 的 t=0 关键帧**；之后只靠 `AdvanceTimeline` 的 `Tick(dt)`（`:293` / 驱动 `:14334`）推进。**批处理没有帧循环**，而自检的 `Step` 只在需要处调 ⇒ 相机停在 t=0：
  - `battlearena1` 的 t=0 = `(-7.14, 1.61, 1.07)`（`Resources/camera_intro_clips.json` 的 `battlearena1.position[0]`）。
  - 日志 `47067` 实测相机 `pos=(-7.14, 1.61, 1.07)` —— 与关键帧**逐位相同**，不是本位 `(0, 2.222, −13.572)`。
  - 相机在这个位姿下很多格位的 `WorldToScreenPoint.z <= 0`（在相机后方）⇒ `BoardLayout.DropTargetWorld` 退回 `SlotPosition`（2D），而 `TryResolveSlot` 走 3D ⇒ 往返 **0/18**；投影诊断的两行一个跑到屏上一个跑到屏外。
- **为什么是本批**：`git show HEAD:…/BattleDriver.cs | grep StartCameraIntro` = **0 命中**；`CameraIntro.cs` 是新增未跟踪文件。HEAD 里相机恒在本位，这些量测全绿。
- **修法（择一，P0）**：
  1. **批处理档不进运镜**：`StartCameraIntro` 首句 `if (Application.isBatchMode) { /* 出声一次 */ return; }`；T12 自检自己显式 `CameraIntro.Play(...)` 验（现在它本来就是自建相机验的）。
  2. 或给 driver 加 `SettleCameraIntroForTest()`（`_cameraIntro.Stop(resetHome:true)`），在 `BattleScene` 的建场 `Begin` 之后**立刻**调一次；并在所有依赖 `boardCam` 的小节前调。
  3. 顺带：`FindBoardCamera()` 只按 `depth < 0` 找**第一台**——T12 自检自建的三台临时相机要确保 `depth >= 0` 或验完销毁，否则后续 `Shot`/量测可能拿错相机。
- **证据**：`CameraIntro.cs:241/288/293`；`BattleDriver.cs:3738/13317-13345`；日志 `47067`（pos 逐位 = t=0）、`47004/47016`（22.91% / −32.50%）、`4075`（屏外两格）。

### D. 攻击/命中/阵亡手感 7 条（`51401…51631`）

- **分类**：δ（夹具时间假设）。
- **根因**：日志 `51335-51360` 自己打了时间线：`Attack@153.65 / Hit@153.75 / … / Death@155.35`，而 `t0 = drv.Clock = 153.147`。测试按 `hitAt = t0 + 0.30`（`AttackWindUp 0.2 + AttackStepMelee 0.1 + MeleeImpactLag 0`）采样（`Editor/BattleScene.cs:16114`），实际起播比 `t0` **晚 0.503 s**（队尾 `_timelineTailAt = 153.45`）⇒：
  - 采样点 153.413 / 153.477 都还在 `Attack@153.65` **之前** ⇒ 位移 0 / 方向 0 / 抖动 0 / z 0 / 飘字没出；
  - `deathAt` 推出的时刻也整体早了 0.5 s ⇒ `DyingCount==0`、0.5 s 后视图已销毁。
- **修法（夹具）**：在取 `t0`/静止位之前先把队列排空到队尾：`while (drv.Clock < drv.TimelineTailAtForTest) Step(1f/60f);`（`TimelineTailAtForTest` 是现成的自检口，`BattleDriver.cs:14134`）。或从 `drv.TimelineDump()` 里取实际 `Attack` 的 `@t` 当 `t0`，⛔ 别再写 `t0 = Clock`。
- **业务代码**不动（`Mathf.Max(_clock, _timelineTailAt)` 是 A1875「新一批不许插队」的既定口径，HEAD 就有）。
- **证据**：日志 `51347-51358`（TimelineDump）、`51389`（t=153.413 时 Attack 仍待播）；`BattleDriver.cs:13635`。

### E. 回手动画 6 条（`15453…15612`）

- **分类**：δ（同 D）。
- **根因**：`ctx.Signals.Add(Return)` → `RefreshAll()` 只是**排期**；`Return` 事件没到点 ⇒ `PlayReturnFeel` 没跑 ⇒ `ReturningCount==0`、未换溶解材质、`DissolveAmount 0`、卡面 alpha 1、没抬升；最后「倒放走完 ⇒ 视图销毁」**因为从头就没开始而假绿**，而「视图真的被销毁」红。
- **修法**：加 `ctx.Signals.Add(...)` 后 `Step` 到该事件的 `at`（同 §二 的通用修法），再断 `ReturningCount==1`。⛔ 别为了绿去把 `ReturningCount` 的判据改宽。
- **证据**：日志 `15453-15612`；`PlayReturnFeel` `BattleDriver.cs:16761-16787`（HEAD 一致）。

### F. oath 3 + bloodthirst 2 + rally 1（`15930/15943/15975`、`16363/16441`、`20299`）

- **分类**：δ（同 D）。
- **根因**：
  - oath：`ctx.Signals.Add(Trigger oath)` → `RefreshAll` → `Step(0.6)`。日志显示这 0.6 s 里播的是**更早排的旧事件**（`AttackHitSmall` @ 15900/15961），而 oath 的 `Trigger` 仍在队尾之后 ⇒ 三次都 `TriggerPlayCount=0`；随后「演了 3 次但没写去重表」这条**因为一次都没演而假绿**。
  - bloodthirst：`btPlays0=0`，`Step(0.6)` 里补播了**两条迟到的 oath** ⇒ 读成「演过 2 次」；bloodthirst 自己的 `Trigger` 还没到 ⇒ `TriggerCount=0`。第 2 次同理。
  - rally（`20299`）：`SimulatePlay` 只走 `RuleCore.PlayCard + RefreshAll`，`Deploy` 在队尾之后 ⇒ `SyncBoard` 被 `IsPendingDeploy` 压着不建视图 ⇒ `MyUnits[rallySlot]` 为 null（「没视图」）。测试在 `RefreshAll` 之后**没有 Step** 就读视图。
- **修法**：三处都先「排空到队尾」（`TimelineTailAtForTest`）再断言；rally 那条改成 `Step` 到 Deploy 演完再读 `MyUnits`（或先 `Step` 到 `TimelinePending==0`）。
- **证据**：日志 `15900/15961`（Step 里冒出旧事件）、`16363`（0 张 / 演过 2 次）、`20299`（没视图）；`SyncBoard` 闸 `BattleDriver.cs:17455`。

### G. 无换牌开场白 3 条（`49427/49440/49453`）

- **分类**：β（本批新测试 + 既有状态机顺序）。
- **根因**：上一节（换牌路开场白）最后一步「两条都播完 → 状态机自己收尾」只等 `IntroStage < 0`。而 `TickIntroMonologue`（`BattleDriver.cs:6332-6341`）**先判 `_introStage > 1` 就置 −1**，再判气泡是否还在 ⇒ 第二条气泡/音频还亮着，状态机已经「收尾」。随后新局 `Begin`（no-mulligan 分支 `StartIntroMonologue`）**不清聊天气泡**（`Begin` 没有 `_unitChat.HideAll()`），`TickIntroMonologue` 被 `_unitChat.IsSpeaking || ShownSide != -1` 挡住 ⇒ 先手 0 一直没开口；测试只 Step 1 s，等到的是**上一局后手（Goff）的残留气泡** ⇒ `ShownSide=1`、`GOF77-Chat0`、`VO_Ork_Beastboss Morgrim_intro.ogg`。测试里 `Check(firstNM == 0…)` 先过，只是「先手侧定下来了」，不是「先手开过口」。
- **修法（实现，推荐）**：
  1. `TickIntroMonologue` 把 `if (_introStage > 1) { _introStage = -1; return; }` **移到**「气泡/音频还在 ⇒ return」之后（原版是协程 `yield return`，播完才算完）。
  2. `Begin` 开头调 `_unitChat.HideAll()`（并停音频），等价原版「每局是新场景」。
  - 夹具侧同批补一条：新局前把上一局气泡推完（`while (chat.ShownSide != -1) Step(1/30)`），并让 `seen` 条件带上 `LastEvent=="Intro"` 与 `LastCardId`。
- **证据**：`TickIntroMonologue` `:6332-6341`（HEAD 同样顺序）；`Begin` 无 `_unitChat.HideAll`；日志 `49427`（ShownSide=1 / GOF77）。

### H. 聊天/气泡 4 条（`48452/48465/48491` + `48604`）

- **分类**：δ（残留状态 + `Step` 内 `Advance` 早于事件播放）。
- **根因**：
  1. 前面「设置面板投降」那一节走 `Forfeit()` → `SpeakConcede(_me)`（`BattleDriver.cs:8389/4794`）留下一个 **Concede** 气泡（Ventris 那一句）。到本节时它还在（节的间隙只推了不到一个气泡时长）。
  2. 测试的 `seen` 循环只认 `chat.ShownSide == 0`（`BattleScene.cs:14902`）⇒ **立刻**被旧气泡满足，循环不推任何一步；`LastEvent/LastCardId/LastClipName` 都是旧 Concede 的值。
  3. `48604`：`Step(5f)` → `AdvanceTimeline` 先 `_unitChat.Advance(5)`，**再**播时间线事件；本步里播到的 `Deploy` 会新建气泡（`_left` 满），所以同一 `Step` 内**永远不会**变成 `ShownSide == -1`。
- **修法**：
  - 本节开头 `chat.HideAll()`（或 `while (chat.ShownSide != -1 && t<10) Step(1/30)`）；
  - `seen` 条件改成 `LastEvent=="Deploy" && LastCardId==card.Card.Id && ShownSide==0`；
  - 「停留时间走完」改成 `Step(5f); Step(5f);`（或先 `Step` 到事件播完，再单独 `chat.AdvanceForTest(10f)`）。
- **证据**：日志 `48452-48491`（LastEvent=Concede / Ventris clip）；`SpeakConcede` `BattleDriver.cs:8389`；`AdvanceTimeline` 里 `_unitChat.Advance` `:14325` 早于时间线泵。

### I. **T09-C3 气泡宽度（本批 β，4 条：`48114/48179/48257/48335`）**

- **现象**：`BubbleSizePxOf` 三颗都读成 `646.996×236.500`，期望 `648.774×236.505`（差 1.778 px = 0.27%）。中心/锚点/`anchoredPosition` 全对。
- **根因**：本批给 `UnitChatPanel.RefreshLayout()` 加了 `if (_ugui) return;`（`Battle/UnitChatPanel.cs:698`）。而**全件唯一**调用 `SetAspect(BubbleW/BubbleH)` 的地方在 `Place()`（`:709`），且 `Place` 只由 `RefreshLayout` 调。于是 uGUI 档的气泡宽度永远停在建件时的**贴图比例**：`wPx = hPx × tex.width / tex.height`（`:603`）⇒ 236.5 × 2.7357 = 646.996。`BubbleW/BubbleH` 正确的 `2.7432` 不生效。
- **为什么是本批**：`git show HEAD:…/UnitChatPanel.cs` 的 `RefreshLayout()` **没有** early-return（会 `Place` ⇒ `SetAspect`）；本批 diff 新增了那行。
- **修法**：在建件处把三条 `SetAspect` 各做一次（与 `Place` 的判据同源）：
  - `MakeBubble` 里 `b.bg.SetAspect(BubbleW / BubbleH)`、`b.portrait.SetAspect(PortraitW / PortraitH)`、`b.wave.SetAspect(WaveW / WaveH)`；
  - `Place` 里那三句保留（幂等；世界档仍走它）。
  ⛔ 不要为了绿把 `RefreshLayout` 的 early-return 撤掉——C3 的「幂等空转」两条断言（⑤⑥）正是要它不写几何。
- **证据**：`UnitChatPanel.cs:603/698/709`；日志 `48114/48179/48257/48335`；本批 diff `+ if (_ugui) return;`。

### J. `desc`/`descZh` hash（`53963`）

- **分类**：α（断言过期；HEAD 就已经 stale）。
- **现算**（FNV-1a 32，1127 张，文件序）：
  - 第五十五/五十六会话（`962933d4` / `8f77ff7a`）的 `cards_engine.json` = `0xDD0B8C87`（与断言逐位相同）；
  - **HEAD**（`5f211be6`）= `0x661B0746`（**第五十七会话就变了**）；
  - **当前工作区** = `0x091039AB`（与日志一致）。
- **本批改的 2 张 desc**：`SAU28`（`+ Flying.`）· `UM80`（`+ Vanguard.`）；`descZh` 未变。另 17 张 keywords 重排**不进这个 hash**（只吃 `id/desc/descZh`），但会让 `card_vfx_by_card.json` 的 `events` 顺序变化（已见 `UM80/GOF26/TL68/SOR16/GSC32` 等 10 条事件重排）。
- **修法**：把期望改成 `0x091039AB`，并在注释里写清「第五十七会话 + 本批两张 desc（SAU28/UM80）」——断言自己的注释就写着「卡表若有意更新 ⇒ 按算法重算即可」。
- **证据**：`BattleScene.cs:17368-17383`；本次离线复算（`git show HEAD/…` 三份 JSON 逐字节算 hash）。

### K. 中文卡面残留 `[[…]]` 1 张（`54041`）

- **分类**：γ（既有数据缺口；断言本身基本正确）。
- **根因**：**`GOF27`**（Orks）。官方 zh `Card_Description/GOF27` =
  `[[盔甲]] 2<br>[[震荡的]]<br>[[不稳定]] [[集结]]: ((Stun_a_random_enemy))`。
  `CardIcons.RewriteMarkTraits`（`Core/CardIcons.cs`）只认「`Card_Trait/<名>` 有词条 **或** `Atlas_trait_icon_<名>` 有图」；这四个是**官方表里已经写成中文的 token**，`Card_Trait/*` 与图集都没有 ⇒ 按原版规则**原样保留 + 出声**。日志 `27814/27841/27868/27895` 就是这四条 `[CardIcons] … 认不出` 警告（只是 `WarnMarkOnce` 在那之前已经报过，⑥ 循环里不再刷）。
  English 档 0 残留，因为 en 列用的是 `[[armour]]` 这类能认的枚举名。
- **修法（二选一，按铁律 11 都属「要做」）**：
  1. 数据侧补 `Card_Trait/盔甲` / `震荡的` / `不稳定` / `集结`（+ 对应 `Atlas_trait_icon_*`），这是官方 zh 模板的「中文枚举名」缺口；
  2. 或在断言里把 **`GOF27` 记白名单**并**写清理由**（同 `OfficialFaceTextWhitelist` 的做法），同时保留 `[CardIcons]` 警告作为「数据缺口」唯一的可见出口。
  ⛔ 不要在 `RewriteMarkTraits` 里加中文→英文别名表（本仓明确禁止「把缺口藏起来」）。
- **证据**：官方表 `武器`/`官方本地化_13lang.json` 的 `Card_Description/GOF27`（本次离线扫描）；日志 `27814-27920`；`CardIcons.RewriteMarkTraits` 的 doc。

### L. T16 输入 2 条（`63347/63360`）

- **分类**：δ（夹具前提）+ γ（既有；`Contains → HitByCollider` 的切换在 HEAD）。
- **根因**（两条都同一个）：
  - 测试在卡局部 x=0.96/0.98/1.00 取点（`Editor/BattleScene.cs:19749-19751`），假定 `BoxCollider2D` 半宽 0.985 ⇒ 1.00 在框外。
  - 实际判据是 `CardInteraction.TickPressCandidate` 的 `card.HitByCollider(world)`（`Hand/CardInteraction.cs:405`），即真 `Physics2D`。文档已记 `BoxCollider2D` 只跟 Z 旋转、**不跟 `AutoCardRotation` 的 X/Y ±10°**，实测最大差 **~2.6% 卡高**（`Core/CardView.cs:5944-5947`）。**1.5% 的边界余量小于这个已知偏差** ⇒ 1.00 可能仍被判在框内（`cos 10° = 0.9848 < 0.985`）。
  - 另外：`Frontmost` 取的是**那个点上的最前卡**，而测试没有断「按下那张就是 `cvT16`」。若指针在边缘压到邻卡，`_pressInside` 锁存在**另一张卡**上，`outB` 仍可能在它的框内 ⇒ 同样红。
- **修法（夹具）**：
  1. 边界点用**真 collider 的实测矩形**反算（`cvT16.ColliderForTest` / `ColliderSizeForTest` 已有口），或把余量拉到 ≥5%（例如 1.06–1.10 卡单位）并说明「避开已记录的 2.6% 倾摆差」；
  2. 加一条**前提**：`itT.SimulateHover(inB)` 后断「hover 的卡 == cvT16」（需要 `HoveredForTest` 一类的只读口，现无）。
  - ⛔ 不要把 `TickPressCandidate` 的判据改回 `Contains`——那是 W57 B2a 有意换的口。
- **证据**：`Editor/BattleScene.cs:19729-19766`；`Hand/CardInteraction.cs:405/436`；`CardView.cs:5938-5947`（已知偏离）。

### M. T03 换牌行 y 边界（`65211`）

- **分类**：γ（测试辅助函数的 bug；HEAD 同形）。
- **根因**：`edgeM03`（`Editor/BattleScene.cs:20190-20199`）的二分返回 `ix + (ox - ix) * lo` —— 它**只返回 x**。纵向两条传 `ix = ox = 0` ⇒ 无论 `lo` 多少都返回 **0**；于是 `eT03m/eB03m` 恒 0.0000，`|0 − 1.51| <= 1px` 必红。二分过程中的 `hitM03` 用的是 `iy + (oy-iy)*t`，**命中判断本身是对的**（横向两条绿说明整条链没问题）。
- **修法**：`edgeM03` 返回插值后的**那个轴**（例如再传一个 `bool vertical`，或写两个 helper）；期望/±1px 检查改成用 `eT/eB` 的 y 值去比。
- **证据**：`Editor/BattleScene.cs:20190-20223`；日志 `65211`（x 绿 / y 0.0000）。

### N. 候选/池子 2 条（`67603/68641`）

- **分类**：γ（断言过期于第五十五会话 E10）。
- **根因**：`EffectResolver.LeviathanEffectPool`（`RuleEngine/Core/EffectResolver.cs:3786-3806`）已是 **6 项**：`+1 Armour / +2 Melee / +2 Ranged / +2 Health / Stealth and +1 Armour / Vanguard`（SO 的 `T6/T12 …_enchantment1..6`），而测试仍写 `ChooseOptionCount == 3`（`Editor/BattleScene.cs:20953/21244`）。「三项文字」那条**前 3 项恰好仍对**，所以它绿、数量红。HEAD 里池子同样是 6 项 ⇒ 不是本批。
- **修法**：断言改成 6，并逐项核 `0..5` 的 Label（或按 E10 的报告只断前 3 + 后 3 各自的文字）。同时把「LeviathanEffectPool」那段注释里的「三等分/六等分」口径与测试对齐。
- **证据**：`EffectResolver.cs:3786-3806`；日志 `67603`（摆着 6 张）/`68641`（实得 6）。

### O. 抽卡落点 2 条（`2403/2416`）

- **分类**：γ（断言口径错；HEAD 同形）。
- **根因**：`HudAnchor01(Draw, …)` → `HandRow01`（`BattleDriver.cs:16331-16344`）在**手牌非空**时返回 `LayoutSpace.ToNormalized(hl.SlotPosition(idx, count))` = **基线 + 弧高**（我方 ≤0、敌方 ≥0），不是 `HandLayout.DefaultBaselineY`/`EnemyBaselineY`。开局 `Begin` 发了起手牌 ⇒ 实得 `0.1037` / `1.0043`，与基线差 0.0063 / 0.0046（正好是末尾那张的弧高）。测试拿「基线」当期望（`Editor/BattleScene.cs:1106-1110`），只在 `count==0` 时成立。
- **修法**：期望值改成同一条算式（`LayoutSpace.ToNormalized(HandOf(side).SlotPosition(count-1,count))`），或退一步断「落在手牌行带内（基线 ± 最大弧高）且不在棋盘行」。
- **证据**：`BattleDriver.cs:16331-16344`；日志 `2403/2416`；`HandLayout.cs:106/128/560`。

### P. 日志面板滑入 2 条（`9659/9672`）

- **分类**：γ（HEAD 同形）。
- **根因**：同一节存在**两次** `drv.ShowBattleLog()`：
  1. 合成 `#i` 事件那一块里先调一次（`Editor/BattleScene.cs:4466` 附近，为了让日志重新灌一遍）；
  2. 真正「点开」再调一次（`:4472`）。
  而 `BattleLogPanel.Show()` 第一句是 `SnapSlide()`（`Battle/BattleLogPanel.cs:1119-1127`）：第一次 `Show` 起滑后 `_slideDir=+1`、`_slideT` 还 0；**第二次 `Show` 的 `SnapSlide()` 看到 `_slideDir>0` 就把 `_slideT` 推成 1** ⇒ 检查时已在终位 87 px（进度 1.00），后两条断言（初始 −1200 / 半程）必红，最后一条（终位 87）绿。
- **修法**：
  - 夹具：合成事件那一块改用**只刷内容**的口（如 `drv.RefreshBattleLog()`），不要 `ShowBattleLog()`；或把那块挪到滑入断言**之后**；
  - 或实现：`Show()` 里 `if (_visible) { ApplyEntries(); return; }`（已经开着就别 Snap）。
- **证据**：`BattleLogPanel.cs:1119-1135`；`BattleScene.cs:4442-4472`；日志 `9659/9672/9687`（终位 87 绿）。

### Q. 换牌面板前提（`41805`）

- **分类**：γ（HEAD 同形）。
- **根因**：`BattleScene.RunCore` 用 `BuildScene` **新建** driver（`Editor/BattleScene.cs:32510 sceneRoot.AddComponent<BattleDriver>()`），而 `mulliganEnabled` 的字段初值是 **false**（`BattleDriver.cs:5167`）。`mulliganEnabled = true` 只在 `BuildAndSaveScene` 里（`:179`），那一路只影响**存盘场景**，不影响自检。于是 `drvT7.Begin(...)` 走「无换牌」支 ⇒ `Ctx.MulliganOpen=false` ⇒ 面板不开。
- **修法**：该测试自己 `bool was = drvT7.mulliganEnabled; drvT7.mulliganEnabled = true; try { Begin… } finally { restore }`（与文件里其它几节的写法一致）；或把它挪到本文件那些 `mulliganEnabled=true` 的换牌小节里复用同一个 driver。
- **证据**：`BattleScene.cs:12691-12698`；`BattleDriver.cs:5167`；`BattleScene.cs:32510`。

### R. 敌方飘字比 0.0000（`50804`）

- **分类**：γ（测试 bug，HEAD 同形）。
- **根因**：`④‴` 那一段为了不残留，已经在末尾 `DestroyImmediate(pop1.gameObject)` / `(pop123…)`（`Editor/BattleScene.cs:15820`）；紧接着 `④⁗` 用 **`pop1` 当分母**（`:15828-15831`）。Unity 对已销毁对象的 `!= null` 返回 false ⇒ 三元表达式直接给 `0f`，与 `popFoe` 无关。
- **修法**：把「玩家/敌方字号比」那一段移到销毁之前；或为它单独新建一个玩家档 `popPlayer` 再比（分母不复用被销毁的那一颗）。
- **证据**：`Editor/BattleScene.cs:15813-15831`；日志 `50804`。

---

## 二、通用修法（一次解决 D/E/F/G/H 半批）

这些失败共享同一个形状：**「排一批事件 → 立刻断言」**，而 2026-10-19 起新事件排在
`max(_clock, _timelineTailAt)`（`BattleDriver.cs:13635`），批处理的 `_clock` 只有在 `Step` 里才走。
建议在 `BattleScene` 里加一个**唯一的**夹具助手：

```csharp
// 把当前时间线推到队尾（含已排队、还没起播的事件），再返回当前 clock
static float DrainQueue(BattleDriver d, float maxSec = 30f)
{
    for (float t = 0f; t < maxSec; t += 1f / 60f)
    {
        if (d.TimelinePending == 0 && d.Clock >= d.TimelineTailAtForTest) break;
        Step(1f / 60f);
    }
    return d.Clock;
}
```

用法：
- 采样前：`float t0 = DrainQueue(drv);`（D、E、F）
- 摆一个单位后要读视图：`DrainQueue(drv);`（F·rally、B、A 的 `SimulateOpenCommand` 前）
- 断言「气泡自己收起来」：先 `DrainQueue`，再 `chat.AdvanceForTest(10f)`（H）
- `battle.log`（早一轮，15 red）与 `battle2.log` 前 15 条**逐条相同** ⇒ 这些组在第一轮就存在，不是 `battle2` 独有的抖动。

---

## 三、没查清 / 需要探针的部分

1. **T16 两条**：到底是「倾摆/边界余量」还是「`Frontmost` 按到了邻卡」，日志里没有读数。建议加一行诊断（`inB` 上 `Frontmost` 返回的卡名 + `ColliderSizeForTest`），再决定是放宽余量还是加 `hover==cvT16` 前提。
2. **联机席位 B 组**：视图为 null 是「待播登场（`_pendingDeploy`）」还是「`SetMySeat` 翻表后身份对不上」，两者都可能；日志里没有 `_pendingDeploy` 的读数。建议在 `Lookup` 失败时打一行 `DeployPendingForTest(owner,slot)`（现成口，`BattleDriver.cs:14196`）即可一次分清。
3. **开场白 G 组**：`_unitChat` 是否在 `Begin` 被清过（本诊断按源码判「没有」）；若将来加了 `HideAll`，`TickIntroMonologue` 的顺序问题仍要一起改，否则两局之间的第二条气泡照样会留。

---

## 四、建议修复顺序

1. **P0**：C（T12 运镜批处理不进/建场后归位）→ I（C3 建件处补 `SetAspect`）→ A（`OpenCommand` 改 `TryGetValue`）。
2. **P1**：§二 的 `DrainQueue` 助手 + D/E/F 三组夹具改用它；G（`TickIntroMonologue` 顺序 + `Begin` 清聊天气泡）；H（清残留气泡 + `seen` 条件）；L（T16 夹具）。
3. **P2**：J（hash 重算）、K（GOF27 数据/白名单二选一）、M（`edgeM03` 返回轴）、N（6 项）、O（`SlotPosition` 期望）、P（别二次 `Show`）、Q（夹具开 `mulliganEnabled`）、R（分母别用已销毁对象）。

---

## 五、证据索引（文件:行号）

| 内容 | 位置 |
| --- | --- |
| 时间线起算 = `max(clock, tail)` | `Battle/BattleDriver.cs:13635` · 字段 doc `:13807` |
| `_timelineTailAt` 只增不减 / `DropSignals`/`Begin` 清 | `BattleDriver.cs:14964`（DropSignals）· `:3433`（Begin） |
| 待播登场闸 | `NotePendingDeploy` `BattleDriver.cs:14770-14790` · `SyncBoard` `:17455` |
| `AdvanceTimeline` 到点判定 `at <= _clock` | `BattleDriver.cs:14398-14410` |
| `_unitChat.Advance` 排在事件泵之前 | `BattleDriver.cs:14325` vs `:14401+` |
| 开场独白状态机 | `BattleDriver.cs:6279-6345` |
| 投降台词 | `BattleDriver.cs:4794-4805` · 调用 `:8389` |
| 开场运镜 | `Battle/CameraIntro.cs:241/288/293/326` · 起播 `BattleDriver.cs:3738/13317` |
| 相机查找（depth<0 第一台） | `Editor/BattleScene.cs:32395-32402` |
| 气泡宽度唯一来源 | `Battle/UnitChatPanel.cs:603/696-709`；C3 early-return `:698` |
| 气泡尺寸读回 | `UnitChatPanel.cs:312-318` |
| `OpenCommand` 裸下标 | `BattleDriver.cs:10244` |
| `SetMySeat`（只写 `_me`） | `BattleDriver.cs:2804-2808` |
| 身份表按 `_me` 选 | `BattleDriver.cs:987-990` |
| `mulliganEnabled` 默认 false | `BattleDriver.cs:5167`；存盘那一路 `BattleScene.cs:179` |
| 选效果池 6 项 | `RuleEngine/Core/EffectResolver.cs:3786-3806` |
| 日志面板 `SnapSlide` | `Battle/BattleLogPanel.cs:1119-1150` |
| T03 二分助手只回 x | `Editor/BattleScene.cs:20190-20223` |
| 敌方飘字分母已销毁 | `Editor/BattleScene.cs:15813-15831` |
| 数据 hash 期望 | `Editor/BattleScene.cs:17368-17383` |
| T16 边界点 | `Editor/BattleScene.cs:19749-19766`；判据 `Hand/CardInteraction.cs:405/436`；已知偏离 `Core/CardView.cs:5944-5947` |
| 中文残留 4 token | 官方 zh `Card_Description/GOF27`；警告日志 `battle2.log:27814/27841/27868/27895` |
