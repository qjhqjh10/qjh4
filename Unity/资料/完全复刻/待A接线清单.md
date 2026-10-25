# 待 A 接线清单（`BattleDriver.cs` 及其域）

> 来源：代理 E（HUD/卡牌面/战役/轮抽）与 D（特效/联机）已把**判据与方法级实现**做完，
> 但落点在 `BattleDriver.cs`（属 A），按单写手规则没动。
> **每一条都是「一行到三行」的接线，判据已在报告里，⛔ 不用重新查。**
> 更新于 2026-10-25 第七十八会话收尾。

| # | 要接什么 | 落点 | 判据 / 参数 | 出处 |
|---|---|---|---|---|
| **1** | **选牌面板倒计时喂时钟** | `BattleDriver`：`_choosePanel.Visible && _clockInCountdown` 时**按秒**喂 `_clockLeft` | 面板侧四方法（`SetTimer` / `CountdownTrigger` / `TickOpenDelay` / `ConfirmNow`）E 已做完，只差驱动喂 | `报告/E_HUD_B升A_第1轮.md` §11.2③ |
| **2** | **抬牌态不开窗** | `OnCardTapped`（约 `:12633`）`SetHint("")` 之后加：`if (interaction != null && interaction.CardDisplaySuppressedFor(card)) return;` | 判据口 E 已实现（`Hand/CardInteraction.cs:549-566`）；原版 `ShowCardInHand:10484 → ToggleOpenCardDisplayOnTouch(false):10597` | 同上 §11.1 |
| **3** | **抽牌终点锚** | 用常量 **`CardDrawPositions = (0,490,100,100)` / `PlayerDrawnPos = (1350,490,100,100)` / `EnemyDrawnPos` 与它同值** | ✅ **已逐场算过、不是猜的**；公式与验证表在 `判据/HUD光标与抽牌锚.md` §二·订正。⛔ 别按场分支，也别给对手挪一格 | D 的 `工具/scene_rect_to_hud.py` |
| **4** | **能量数字弹跳** | `localScale = one` 复位 → `DOPunchScale(one×0.2, 0.5s, 10, 1.0)` | E 报告 §11.2② | `报告/E_HUD_B升A_第1轮.md` |
| **5** | **设置面板两行** | `SettingsPanel.Create(root, Forfeit, Ctx != null ? Ctx.MatchType : (MatchType?)null)` + `_settingsPanel.OnSkipTutorial = SkipTutorialFromSettings;` | 面板侧早已做完（`SetMatchType:2468` / `SkipWanted:2456` / `SetResignText:2482` + 4 条断言）；**漏传 `matchType` 会出声，不静默** | 同上 §8.4④ |
| **6** | **联机表现宿主** | 实现 `INetFoePresentationHost` 三方法（`NetRemoteCardSeen` / `NetRemoteCardReorder` / `NetRemoteMatchFinished`，**最后一条不判胜负**） | **不实现也不影响对局**（局面由主机定序 + 指纹保证），只少三处表现，且会出声 | `报告/D_联机_第1轮.md` §⑧ |
| **7** | **本机打完的收尾等待** | `SendMatchFinished()` → `BeginFinishWait()` → 每帧 `TickFinishWait(dt)`，**返回 true 期间不显示结算** | 条件 = `matchFinished \|\| ActionsLeftInQueue()>0`；`FinishWaitStep=2.0f`（`GameAssembly.dll` `0x1834b2bbc`）；上限 12.5 s。⚠️ `ActionsLeftInQueue()` 用的是 `Session.Inbox.Count` **代理量**，沿用别另造 | 同上 §③ |
| **8** | **`CardTrait._hide`** | 无需改代码（已判「原版零可达」） | 5 个生产调用点全传 `hideTrait=0` ⇒ 我们 `ShowingTrait` 行为等价 | `账/战斗逻辑驱动.tsv` row 1189 |

**已由 A 做完、不用再接的**（记录以免重复）：
- 389/401/414 **倒计时遇演出顺延**（`_clockInCountdownAnimDepth` + `TickClock` 闸）
- 455 **结算收窗**（`ShowEndPanelAndFreezeInput()` 第一句 `CloseAllWindows()`）
- 63 **抽牌 tooltip 跟鼠标**（`Tooltip.ShowAtMouse(body, MouseWorld, …)`）
- 逐卡 tooltip 开关的**消费侧**（`TickTraitTip`/`TickCardTip` 的 `if (!v.TooltipsEnabled) return false;`）
- **`manaAccumulation` → `EnergyAccumulationOn`**（`Ctx.Vars.addedManaSecond > 0` + `RefreshEnergyAccumulation()`）

> 📌 **A 每改一次 `BattleDriver.cs`，E 那四份账的行号会漂** ⇒ 交活前跑
> `python -I -X utf8 工具/check_ledger_points.py --fix <那些账>` 再报（E 已养成这个习惯）。
