# B89 执行代理 `E5b` —— R9 批次 9「BattleDriver 表现族」（Chain D 第 2 棒）

- 独占文件：`MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（**只改这一个**）
- 账行：`L298` · `L392` · `L584` · `L585` · `L682` · `L782` · `L806` · `L930` · `L103`
- 补丁：`账/_parts/B89_E5b.tsv`（11 列 × 9 行，列数已逐行核）
- 行尾 / 行数：`\r\n` 全篇一致（`crlf 36700 / lines 36700 / bare_cr 0`）；**既有行号零漂移**（全部改动都是「就地改写同一行」，新增成员全在上一批类尾 `:36566` 之后）

---

## §1 逐条结论

| 账行 | 原版判据（现读） | 改动（文件:符号名 + 现读行号） | 状态 |
|---|---|---|---|
| **L298** `DisplayHurryUpChatMessage` | `BattleManager.cs:21334-21338` · `VoiceLinesController.cs:567-573` · `__DisplayLocalChatMessage.c` · `__CanChat.c` | **零改动**：`SpeakHurry` `:7832` 第一道闸就是 `CanLocalChat()`（`:35145`），`isPopUpShown(+0x78)` 写点已在（`_chatPopUpBlocked` + `ChatPopup.Visible`） | **A** |
| **L392** `GetViewportPointFromBoardPos` | `BattleManager__GetViewportPointFromBoardPos.c:1-15` | 新增 `BoardViewportPoint()` `:36583` + 自检读口 `:36598` | **A** |
| **L584** `SetupReplayMulligan` | `BattleManager.cs:18554-18602` + `PlayerHand__SetMulliganCardsToDiscard.c` | 新增 `ReplayShowMulliganMarks()` `:36676`；调用点 `:359`（ApplyLoggedAction 的 Mulligan 支） | **A** |
| **L585** `SetupReplayMulliganStart` | `BattleManager.cs:18526-18552` + `PlayerHand__SetMulliganCardsToDiscardStart.c` | 同上（同一实现的两半：Start = 打标记 / Commit = 面板收掉） | **A** |
| **L682** `ActivateMinion` | `CardScript.cs:6655-6710` · `CardScript__ActivateMinion.c:82-96` | **零改动**：`canAttackAnim` = `v.SetCanAct(...)` `:26706`；第二句 = **空桩** | **A** |
| **L782** `HeroLandIntoField` | `CardScript._HeroLandIntoField_d__318__MoveNext.c:35-429` | 无代码：showStats 分支**本轮解出形状**，但分组对象在卡面层 | **B** |
| **L806** `MoveBackToHand`（+`OutFromPlayingAreaAnim`） | `CardScript__MoveBackToHand.c:12-30` · `CardScript__OutFromPlayingAreaAnim.c:1-33` | **零改动**：等价物 = `CardFeel.cs:3709` + `ResolvePlayTargetByUnit` `:8235` + `SyncHand→RefreshHandPlayable` | **A** |
| **L930** `StopAnimsInPlay` | `CardScript__StopAnimsInPlay.c:5-6` · `BattleCardUI__StopAnimsInPlay.c:1-60` | 无新代码：已接 6 个，余 4 个中 1 个已等价、**3 个前提缺失** | **B（残差收窄 10→3）** |
| **L103** `GetTipPosition` | `AiScripted.cs:3114-3466` · `AiScripted__GetTipPosition.c`（274 行逐 case） | 新增 `TutorialAnchorExtra()` `:36624`；`TutorialAnchor` 的 `default:` 行 `:17117` | **A** |

### 三个关键判据发现
1. **`GetViewportPointFromBoardPos` 与 `IsPlayingCardState` 同族：全库 0 调用者**（三源只命中定义）⇒ 死的读口。我们只提供读口、不接闸。
2. **`ActivateMinion` 末尾第二句是空桩**：`@1804B33B0` = ImageBase + **RVA 0x4B33B0** = 本 build 的**全局共享空方法桩**（`ret 0`，dump.cs 里 1,815 处引用；Ghidra 把同址标成 `NetworkingPeer__OnMessage` 是**同址折叠**）。⇒ `CanUseAbilityNow()` 算出来没有任何消费者，**照抄会得到死代码**。
3. **`OutFromPlayingAreaAnim` 的全部四步在我们这边都已覆盖**（含最容易被漏的最后一句 `LightPlayableHand` ≡ `SyncHand` 的 `RefreshHandPlayable`）。它存在的理由是原版有「出牌区」容器；我们没有 ⇒ 不建同名空壳。

---

## §2 验证读数

- `TMPDIR=/tmp/wf_B89E5b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**（最后一次；中途曾出现 4 条 + 2 条别人正在写的文件里的错：`RuleEngine/Data/CardDatabase.cs` 与 `Editor/GameStaticDataCheck.cs`，均**不是本代理的文件**，未去改，重跑后归零）。
- 行尾：`b.count(b'\n') = 36700`、`b.count(b'\r\n') = 36700`、裸 CR = 0（二进制读）。
- 零漂移自检：全部 5 处产品改动都**只改同一行**（`default:` 行 / `m.kind == Mulligan` 行 / `TickMulligan` 首行 / 键盘闸首行 / 抬起闸那一行），行数逐处核对未变。
- **未跑** Unity / 批处理 / ruleprobe（红线）。

---

## §3 顺手发现 + 真机待验

### 顺手发现（未改）
1. 🔴 **`L782` 的 `showStats` 分支本轮解出了形状**（旧账只写「TODO」）：`showStats==0` ⇒ 只 `WaitForSeconds(waitMult×0.3)`、**不碰图标**；`showStats==1` ⇒ 遍历 `BattleCardUI+0xc0` 与 `+0xa0` 两族子对象，逐项 `SetActive(flag)`，flag = 「这组动画的类型里有我们支持的」：int 命中 **0 / 15**（以及**设备相关**的 **5**）⇒ 开，命中 **10**（或 5 且设备不支持）⇒ 关。**卡面层建出这两族分组后，本方法只需按 flag 开关。**
2. **`L930` 的 10 个原版调用点已接 6 个**（`jam` / `UnitDeath`·`SendToCemetery` / 残骸翻面 / `ResetToValuesInHand` / `TransformUnitInPlayToCard` / **`MoveBackToHand`**）。余 4 个：`DestroyCardOffensive` 已在 `StartRevealFlight` 判为等价；`MoveToDeck` / `RemoveFromGameCard` / `DestroyCard` = **前提缺失**（本仓没有回牌库的视觉路、`EvtKind` 没有「移出游戏」档、没有对应销毁路）。
3. `ReplayShowMulliganMarks` 只在 `_replaySession` 下动作 ⇒ **活局 PvP 的 `ApplyLoggedAction` 走同一行但立即返回**（已核），不影响联机。

### 真机待验项（本批 2 条会改画面/输入）
1. **回放换牌面板**：放一份「录制时开着换牌」的录像 ⇒ 期望：换牌面板（压暗 + 你的手牌 + 每张牌上的「换」钮）**摆出来**，其中**录像里换掉的那几张带着「换」标记**；**点钮 / 按 Enter / 空格都不该有任何反应**；`MulliganDone` 那一条之后面板自动收掉、正常开打。
2. **换牌期（活局）回归**：正常开局换牌应**完全不变**（点「换」标记、点完成、倒计时 <10s 显示 `0:0N`、到 0 自动完成）—— 本批给三处入口都加了 `_replaySession` 前提，活局恒 false，属"应当零变化"，但要亲眼看一眼别被带坏。
3. **教程小提示锚点（50/70/100）**：数据里 0 条 ⇒ **正常游玩走不到**；要验只能临时把某条 `smallTipParams.positionReference` 改成 50/70/100 跑一次（不改数据则**无需验**）。

---

## §4 没查清 / 明确没做

1. **`L782` 的图标分组**：形状解出来了，但那两族子对象（按攻击类型的图标组）**必须先在 `CardPresentation/Core/CardView.cs` 建出来**（= E8 的文件，本波无权动）。⇒ 交接卡面层；建好后本方法只差「按 flag 开关」几行。
2. **`L930` 的三条前提缺失**：`MoveToDeck`（本仓没有回牌库视觉路）/ `RemoveFromGameCard`（引擎无「移出游戏」事件）/ `DestroyCard`（无对应销毁路）。三条都不是「忘了接」，是**路本身还没有**。
3. **`L103` 的 `ruleprobe` 锚点导出**：调度台 2026-10-28 明确归**工具线**，本批未做（⛔ 不塞进本批）。⇒ 那条读完这条线才算真收口。
4. **`EnemyCardInHand(50)` 的「哪一张」**：原版是 `GetChosenCard(scripted, 敌方手牌, referenceUnit)`（按脚本的 reference 认牌），我们复用了 60 那一档的 `_tutHandIdx`、认不出取中间那张。数据 0 条 ⇒ 无可观察差异，但**将来有数据时要照 `GetChosenCard` 补**。

---

## §5 交接（Chain D 第 3 棒）

### 本批动过的符号 / 行（现读）
| 符号 | 行 | 类型 |
|---|---|---|
| `ApplyLoggedAction` 的 `Mulligan` 支 | `:359` | 就地改写（一行内加 else 分支） |
| `TickMulligan` 首行闸 | `:8856` | 就地改写（加 `_replaySession`） |
| `HandleMulligan` 键盘闸 / 抬起闸 | `:8907` / `:8916` | 就地改写（加 `_replaySession`） |
| `TutorialAnchor` 的 `default:` 行 | `:17117` | 就地改写（改调 `TutorialAnchorExtra`） |
| `BoardViewportPoint()` / `BoardViewportPointForTest` | `:36583` / `:36598` | 新增（类尾） |
| `TutorialAnchorExtra()` / `ReplayShowMulliganMarks()` | `:36624` / `:36676` | 新增（类尾） |

### 给下一棒三条
1. **类尾新增区现在是 `:36566-36700`**（上一批 `:36432-36565` + 本批）。新成员继续往 `:36700` 之后追加，⛔ 别插中间。
2. **`ResolvePlayTargetByUnit` 的 `silent` 形参**（`bool silent = false`，上一批加的）仍然只有 `CancelActionStates` 传 `true`。
3. 回放换牌的**输入锁**靠三处 `_replaySession` 闸（`:8856` / `:8907` / `:8916`）+ `OnDone = null`。**别再给换牌面板开第二条输入路**（会绕过这道锁）。
