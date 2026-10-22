# W63 · 「Your Turn」回合抬头 施工简报（只读代理产出，2026-10-22）

> 来源：只读代理对 `BattleManager._NextTurn_d__395__MoveNext.c` + `BattleTipController` + 我们 `BattleDriver` 的逐行核对。
> 状态：**判据已闭合、待施工**。原版有、我们现在完全没有（W62 删掉自造常驻回合行之后）。

## 1. 结论

- 原版在**只属于本地玩家的回合分支**里播：`StartTurn` 音效之后 →（教程局第 1 回合整段跳过）→ `ShowHeadsUpMessage(GetTranslation("Battle/HUD/YourTurn"), opaqueBackground:1, bgColorRed:0)`。
- 全仓 6 个 `ShowHeadsUpMessage` 调用点里，**只有这一处**传 `opaqueBackground=1`。
- 抬头**显示时长 = 1.7 s**（`VarsGlobal.tipDuration`，与我们 `TipDuration` 同值）；`_NextTurn` 里的 `WaitForSeconds(1.5)` 是协程自己停顿，**不是横幅时长**。
- 我们只差两样：① 一句 `SetHintOpaque(Loc.T("Battle/HUD/YourTurn"))`；② 通道缺「不透明档」。词条 `Core/Loc.cs:1468` 已是官方两列，**不用改表**。
- 挂点 = `ResetClock()`（原版 `ClockManager.StartTimer` 的等价物，已有 `Ctx.Active == _me` 闸，`StartTurn` 音效就在同一行旁边）。
- ⚠️ **联机局拿不到**：本地回合在 `ApplyLoggedAction` 的 `EndsTurn` 支里开始，那条路**从不调用 `ResetClock`**（同一个坑也吃掉了「每回合重置时钟」与 `StartTurn` 音效）。

## 2. 证据

1. `decomp_full/BattleManager._NextTurn_d__395__MoveNext.c:444-491`（`param_1+0x28 != 0` = 本地玩家分支）：`:450` `Play2D(StartTurn)` → `:480-481` `GetTranslation(DAT_184288a48 /*Battle/HUD/YourTurn*/)` → `:483` `ShowHeadsUpMessage(...,1,0,0)` → `:485` `WaitForSeconds(1.5f)`。
2. 教程跳过：同文件 `:451-475` —— `IsTutorialMatch` ∧ `turnCounter == 1` ⇒ 跳过抬头。
3. 参数语义：`ShowHeadsUpMessage.c:3` 形参 `(this, string, byte, byte)`；`_DisplayTip` 协程 `+0x29`=param_3、`+0x28`=param_4；`+0x28!=0` 底色改红；`+0x29==0` 用 α=`0.6`，否则 α=`1.0`。
4. 常量现读（`GameAssembly.dll`，RVA=addr−`0x180000000`，`.rdata`）：`0x1834b3090 = 1.5f` · `0x1834b2bb8 = 1.0f` · `0x1834b3180 = 0.6f`。
   ⇒ **原版所有普通提示 α=0.6，Your Turn 这一条 α=1.0（不透明）**。
5. 我们这侧：`BattleDriver.cs:10442-10457 ResetClock()` 同一行已有 `StartTurn` 音效；`:11506 SetHint` / `:11528 TickTipBanner` / `:1176 TipDuration=1.7f` / `:1187 TipBgColor`（α=0.596078 序列化值；原版运行时永远覆盖成 0.6 或 1.0 ⇒ `:1184-1186` 注释「常量读不出」可订正）。
6. 联机缺口：`BattleDriver.cs:310 NetApply.Apply` → `NetApply.cs:97-99 case EndTurn: EndTurn + BeginTurn`；`BattleDriver.cs:324` 只重排 `_aiTimer`，**无 `ResetClock`**。
7. 待办出处：`普查产出_1022/W62_战斗非原版元素清理.md:162-166`；删除依据 `W62_战斗非原版元素审计.md:47`。

## 3. 建议改动清单

**A. `Battle/BattleDriver.cs`（纯 CRLF，⛔ 禁 `sed -i`）**
1. `:1176` 附近新增 `YourTurnTerm = "Battle/HUD/YourTurn"` 与 `TipBgOpaqueColor = rgba(0.368627,0.894118,0.587437,1)`。
2. `:1184-1186` 注释就地订正；`TipBgColor` 的 α `0.596078f` → **`0.6f`**。
3. `:1166` `List<string> _tipQueue` → `List<TipEntry>` + `struct TipEntry { string Text; bool Opaque; }`。
4. `:11506 SetHint` 拆两参：`SetHint(s)` / `SetHintOpaque(s)` / `SetHint(s,bool)`；去重比 `.Text`，入队 `TipEntry`。
5. `:11528 TickTipBanner`：两处 `SetText` 改 `.Text`，并在同处加 `_tipBg.SetTint(entry.Opaque ? TipBgOpaqueColor : TipBgColor)`（`HudPiece.SetTint` 两档都支持）。
6. `:23895 TipPending`：`_tipQueue[i] == s` → `.Text == s`。
7. `:10442 ResetClock`：闸提成 `bool myTurnNow`，`StartTurn` 音效之后调私有 `ShowYourTurnHeadsUp()`：
   `if (Ctx == null || Ctx.IsOver || Ctx.Active != _me) return;`
   `if (MatchTypes.IsTutorialMatch(Ctx.MatchType) && Ctx.Turn == 1) return;`
   `SetHintOpaque(Loc.T(YourTurnTerm));`
8. 自检读口：`public Color TipBgTintForTest => _tipBg != null ? _tipBg.Tint : Color.clear;`
9. 联机（建议同批、只补抬头不动时钟）：`BattleDriver.cs:324` 在 `NetApply.EndsTurn(m)` 支里调 `ShowYourTurnHeadsUp();`。

**B. `Core/Loc.cs:1466-1467`（LF）**：注释末句「今天还没接那一跳」→「已接」。

**C. `Editor/BattleScene.cs`（CRLF）**：加断言，见 §4。

## 4. 断言建议

- 正例：`mulliganEnabled=false` + `Begin` ⇒ `:4123` 必调 `ResetClock`；`string want = zh ? "你的回合" : "Your turn";`（**字面量，不用 `Loc.T` 自证**）→ `Check(drv.Ctx.Active != drv.MyIndex || drv.TipPending(want))`。
- 反例（结构性，防「每回合都播」）：`SimulateEndTurn()` 后对手回合 `Check(!drv.TipPending(want))`。
- 不透明档：抬头播放时 `TipBgTintForTest.a ≈ 1f`；队列播空后 `a ≈ 0.6f`（字面量）。
- 教程跳过：`BeginTutorial(0)` 后 `Check(!drv.TipPending(want))`。
- 灭自证：期望值用官方表字面量 + 核 `Loc.HasEntry`；「对手回合不许有」挡住「实现和断言一起改成永远显示」。

## 5. 风险 / 冲突面

- **联机缺口最大**：`ApplyLoggedAction` 不调 `ResetClock` ⇒ 联机看不到抬头；补 §3-9 可解，但**不要顺手修时钟**（另一条账）。
- **重连/回放**：`ApplyLoggedAction` 也走 `:795`/`:913` ⇒ 会多入队几条；`SetHint` 只对队尾去重，可能重复亮。可接受但要如实记。
- `BattleDriver.cs` / `BattleScene.cs` 纯 CRLF，改完 `git diff --numstat` 对一眼。
- 既有断言 `BattleScene.cs:11701` / `:10889` 不受影响（1.7 s 在 5.1 s 上限内）。
- 原版 `WaitForSeconds(1.5)` 的停顿未复刻（抽牌在 `RuleCore.BeginTurn` 同步完成）—— 既有顺序差，另记一条账。

## 6. 没查清

- `param_3` 的参数名（`opaqueBackground`）是推断（行为 + 常量已实证），IL2CPP 桩里没有名字。
- 回放局 / 教程重放里是否也跳过第 1 回合：只核了 `IsTutorialMatch`（含 `TutorialReplay`），无实拍证据。
- 线上「本地回合不 `ResetClock`」是有意还是遗漏：全仓无记录，**没查清**。

## 7. 实现结果（2026-10-22 · 代理 L）

已按 §3 A/B 全部实现，typecheck **0/0**：
- `BattleDriver.cs`：`TipEntry` + `TipBgOpaqueColor` + `SetHintOpaque` + `TickTipBanner` 两处按 `Opaque` 调 `SetTint` + `TipPending` 比 `.Text` + `TipBgTintForTest` + `ResetClock → ShowYourTurnHeadsUp()` + 联机 `ApplyLoggedAction` 的 `EndsTurn` 支同补。
- `Loc.cs`：注释订正为「已接」。
- diff：`BattleDriver.cs 81+/13−` · `Loc.cs 2+/1−`；CRLF 未翻（24154/24154）。
- ⚠️ **简报 §5 有一条订正**：既有断言 `BattleScene.cs:11701` **会确定性变红**（`RunCore` 的 driver `mulliganEnabled=false` + `PinFirstSeat(driver,0)` ⇒ 开局必走 `ResetClock`；`Step(0.3f)` < 1.7 s）。修法 = `Step(2f)` 或改断 `!TipPending(...)`。
- 联机重复入队：`EndsTurn` 与 `MulliganDone` 互斥；每端每回合只落一次；回放/重连重放可能重复亮（队尾去重只挡连续重复），已如实记。
