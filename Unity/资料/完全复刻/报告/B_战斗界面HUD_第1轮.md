# B · 战斗界面 / HUD —— 完全审查 + 完全复刻 第 1 轮

> 执行代理 **B**，范围 = 战斗界面 / HUD 域（66 类 / 679 条方法）。
> 判据按顺序：🥇 `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**.cs` → 🥈 `D:/2/tools/decomp_full/**.c` → 🥉 解包资源字段 → ④ 原版 oracle（本轮未跑）。
> ⛔ 未跑 Unity 批处理 · ⛔ 未动 git · ⛔ 未改 `CLAUDE.md` / `项目任务.md`。
> ✅ 改完 `.cs` 跑过 `TMPDIR=/tmp/wf_b bash 工具/typecheck.sh`：**运行时 0 / 编辑器 0**（末次）。
> 🔴 账骨架按调度台通知**重开磁盘那份**（679 行、命名空间子目录路径），未用早先的旧内容覆盖。

---

## ① 结论（数字）

| 项 | 数 |
|---|---:|
| 账内方法（重建后的骨架，11 列） | **679** |
| `A 已对齐` | **353** |
| `B 已实现未核` | **101** |
| `C 该做没做` | **32**（P1 = **5** · P2 = 27；**P0 = 0**） |
| `D 不适用`（每条带理由） | **192** |
| `E 未读` | **1**（`CardDisplayWindow.CardUIClicked` —— ILSpy 报 `Invalid jump target`，方法体读不出） |
| **非 E = 678 / 679（99.85%）** | ≥ 250 目标达成 |

**八块点名清单**（逐块结论见 §②·A）：

| # | 块 | 结论 |
|---|---|---|
| 1 | AttackSelector | ✅ 一致（本轮**新增一条硬判据**，把「点底板=取消」这个悬了两轮的「自造口」证成**原版就这样**）；残留 1 条 B（拖到目标释放） |
| 2 | HUD 布局 | ✅ 一致（AU2 31 件逐值；本轮抽核根 canvas/锚点/档案/进攻钮/联盟窗）；2 条进账（P1 三张牌计数灯、B 抽牌起点） |
| 3 | Tips | ✅ 一致（队列 3 / 去重 / 1.7 s / 两档底色 0.6·1.0）；`TipPrebattle` 战前提示整件未建（P2 ×10） |
| 4 | EndPanel | ✅ 骷髅行 + 三段视频一致；奖励/评分行按用户 2026-09-17 拍板**不建**（D）；骷髅上跳动画 P2 |
| 5 | 技能面板 | ✅ 几何/三态色/捕获区/淡入 0.2·淡出 0.1 一致；P2 2 条（acting 单独淡出、拖到目标释放） |
| 6 | 战斗日志 | ✅ 10 行 / 20 条词条 / 行内链接 / 悬停卡一致；P2 2 条（行首动作图标、行内小卡+伤害角标） |
| 7 | 计时 / 倒计时 | ✅ 一致（240/600 s、35 s hurry、15 s、4× 无行动加速、掉线暂停、TimerExpired 顺序）；P2 2 条（切后台补时、演出暂停表） |
| 8 | 投降 / 退出 | ✅ 一致（设置窗 Resign → `Forfeit`；Skip 仅教程局；ESC 同帧闸）；**P1 1 条**：回放局按钮文案应换 `Battle/Settings/Exit` |

**K2 三条尾巴**（§②·B 详述）：
1. **目标型卡两段式手势** —— 🔴 **仍不一致（真缺口，P0 级行为）**，本轮把原版整条链读到确定，但落地跨 `BattleDriver` + 引擎 ⇒ 出**改动请求**（§③·R1）。
2. **技能友方目标** —— ✅ **可达卡上已经一致**（友方目标全走替代行动 `Duty/Pray/Agenda/Oath`，`AlternativeTargetSide` 已按正文分侧）；`AbilityPickOwner` 那三档 Friendly/Any **现池 0 张可达**（60 行友方 criteria 的卡**正文都没有 `Ability:` 段** ⇒ 永远走不到）⇒ 降为 **P2 防御性**，不是玩家可见缺口。
3. **攻击点自己人要不要出声** —— ✅ **我们已与原版一致（静默）**；K2 的原判断**不成立**（见 §⑥·1，铁律 5 就地订正）。

**本轮代码改动**：1 个文件 / +22 行：`Battle/CardChoicePanel.cs` 补**键盘确认**（空格/回车 = 继续，原版 `ChooseCardMenu__Update.c`）。

---

## ② 判据

### A. 八块逐块（`文件:行号` / 资产路径+字段）

**1) AttackSelector**
- 悬停拾取 + 0.1 s 安全窗 + 1.3 倍 OutBack —— 已对齐：`dc/CardDisplayAttackTypeButton__Update.c:20-30`、`__HighlightIfNeeded.c:50-70`、`<StartSafeTouch>d__37__MoveNext.c:17,31`；我们 `Battle/AttackSelector.cs:482 HoverPickReady / :496 MarkHoverPicked / :537 TickHover / :1262 SetSelected / :1730 ApplyPickScale`。
- 🔴 **「点底板（不在钮上）= 取消」证成原版行为**（本轮新增，闭合 `A462` §四·3 的长期疑问）：
  ① 拖动起手 → `dc/CardScript__TryDraggingFromBoard.c:69` 调 `StartAttackFrom(..., param_4 = 1)`；
  ② `dc/BattleManager__StartAttackFrom.c:36-52`：`param_4 != 0` ⇒ **`UIstate = 6 (minionAttacking)`**；
  ③ `dc/BattleManager__Update.c:986-1096`（case 6）松手：`target == null` ⇒ **静默** `CancelAttack` + `StopTracking`；
  ④ `dc/BattleManager__StopTracking.c:8` 顺手 `UnitOnBoardAttackTypeSelector__Toggle(false)` ⇒ **选择器收起**。
  ⇒ 原版就是「在底板空白处点一下 = 取消这次攻击」。我们 `BattleDriver.cs:13852 !ContainsBar ⇒ 取消` 与之**同结果**。
- 底板不是按钮：`bundle_scenes_scenes_battlearena2/GameObject/Select Attack Background.json` 五件 = RectTransform / CanvasRenderer / **Image**（`MonoBehaviour_4692`：`m_RaycastTarget = 1`、`m_Sprite = null`、**无 EventTrigger**）/ LayoutElement / `SelectAttackBackgroundController`（`MonoBehaviour_5417`）⇒ 底板自身**没有任何点击动作**，且 `BattleManager` 全库**不用** `EventSystem.IsPointerOverGameObject`（`grep` 仅教程/点击粒子用）⇒ 那次点击由棋盘输入那条路吃下。

**2) HUD 布局** —— 见 `资料/普查产出_1025/AU2_战斗界面_审查与修复.md` §1（31 件逐值）+ 本轮抽核：根 canvas `HudCanvas.cs:90 Ensure`（ScreenSpaceCamera/1920×1080 Expand/子画布无 Scaler）；锚点 `BattleDriver.cs:3765 UiAnimAnchor` + `HudCanvas.cs:744 EnsureAnimAnchors`（4 颗，值照 `bundle_scenes_scenes_battlearena2` 的 `MonoBehaviour_5150.uiAnimAnchors`）；档案 `BattleDriver.cs:10638-10664`；进攻钮 `:4945`；联盟窗 `:5627`；HUD 显隐 8 条 clip `Battle/BattleHudAnim.cs`。

**3) Tips**
- 队列/上限/去重：`dc/BattleTipController__AreTipsOnQueue.c`、`AreTooMuchTips.c`、`TipSpamming.c` + `VarsGlobal.tipQueueSize = 3`；我们 `BattleDriver.cs:1886 _tipQueue / :1911 TipQueueMax=3 / :15231 去重 / :15266 TickTipBanner`。
- 时长/底色：`dc/BattleTipController._DisplayTip_d__14__MoveNext.c`（`WaitForSeconds(GlobalVars+0x1C)` = 1.7）；α 两个常量从 `GameAssembly.dll` 实读 `0x1834b2bb8 = 1.0f` / `0x1834b3180 = 0.6f` ⇒ 我们 `TipBgColor(…,0.6)` / `TipBgOpaqueColor(…,1.0)`。
- `NotifyCantDoAction` 一次两样（语音 + 文字）：`dc/BattleTipController__NotifyCantDoAction.c:5-14`；我们 `BattleDriver.cs:7419`。

**4) EndPanel**
- 压暗色 `(0.0283019,0.0283019,0.0283019,0.7529412)` + `Background`(Sliced) + `rt=1`：`bundle_scenes_scenes_battlearena2/MonoBehaviour/MonoBehaviour_5043.json`；我们 `Battle/EndPanel.cs:115 DimColor`。
- 骷髅行布局后位置：`dc/EndBattleDoors__ShowRewards.c:55/58-68` + `AllRewardsHolder` 的 VLG ⇒ 面板中心 −236.19；我们 `EndPanel.cs:75-82`。
- 三段视频按 `BattleWinner` 选：`dc/EndBattleDoors__SetupDoor`（`victoryVideo/defeatVideo/drawVideo`）⇒ `Battle/BattleDoors.cs`（`ExpectedLength` 字典 + `Result{Victory=10,Defeat=20,Draw=30}`）。
- 奖励/评分行不建 = 用户口径（`EndPanel.cs` 文件头 2026-09-17）。

**5) 技能面板**
- 几何五元组：`bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3104.json`（`aMin (0.665576696395874,0)` / `aMax (0.965788722038269,0.3040129840373993)` / `pivot (0.5,0.5)` / `ap (−606.110107421875,55.0008544921875)` / `sd (−0.0003662100061774254,−3.509999990463257)`）⇒ `Battle/SkillPanel.cs:194-202`。
- 三态色：`B2:74-76` ⇒ `SkillPanel.cs:399-401`；捕获区：`RT_3053`/`MB_4207` ⇒ `:244-245`。
- 淡入 0.2 / 淡出 0.1（`fadeTime × 0.5`，常量实读 0.5）：`dc/ActiveSkillDisplay__FadeOut.c:26` ⇒ `SkillPanel.cs:371-383`。

**6) 战斗日志**
- 词条枚举 10 档 + 组句：`dc/CemeteryManager__GetActionText.c` / `GetActionWithParams.c`；我们 `BattleDriver.cs:10849-10900` 词条键 + `:11138-11285` 组句（20 条全部对过）。
- 10 行硬编码：`bundle_scenes_scenes_battlearena2` 的 `CemeteryActions` 子件 10 颗 `CemeterySliderUI` ⇒ `Battle/BattleLogPanel.cs:145 RowCount=10`。
- 点击行内卡名 → 展示卡 + 收面板：`dc/CemeteryManager__ClickCardLink.c` ⇒ `BattleDriver.cs:11526 TryClickLogRow / :11455`。

**7) 计时 / 倒计时**
- 总时长三步：`dc/ClockManager__GetTotalTime.c:17-36`（240 = `0x1834b31c4`、600 = `0x1834b2ed8` 实读）⇒ `BattleDriver.cs:2699 TurnSecondsForThisMatch`。
- 35 s 催时 / 4× 无行动加速 / 15 s 倒计时 / 超时 `EndTurnClick(true)`：`dc/ClockManager__Update.c:53-60 / :110-141`、`EndCountdown.c:34,41` ⇒ `BattleDriver.cs:13617 TickClock`。
- 结束回合那颗 TMP 三档文案共享一颗、纯白：`dc/ClockManager__SetEndTurnText.c:88-104`、`Update.c:120-135` + `MonoBehaviour_3971.json`（`m_fontColor (1,1,1,1)`）⇒ `BattleDriver.cs:13730 UpdateClockLabel`。
- 掉线暂停：`dc/BattleManager__ShowDisconnectionPopup.c:27` → `ClockManager.PauseClock` ⇒ `BattleDriver.cs:254 NetClockPaused`。

**8) 投降 / 退出**
- `dc/BattleSettingsWindow__ResignButtonOnClick.c`：`CloseWindow()` → `BattleManager.ClickExitBattle()` ⇒ `Battle/SettingsPanel.cs:1297 OnResignClicked` → `BattleDriver.Forfeit`。
- `dc/BattleSettingsWindow__Open.c`：Skip 钮**只在 `matchType == 100`（Tutorial）**显示；**`MatchType.Replay` 时 Resign 文案换 `Battle/Settings/Exit`**。前两条我们一致（`BattleDriver` 建 Skip 钮的闸），**第三条未做** ⇒ P1（§③·R2）。
- ESC：`dc/BattleHud__Update.c:20-35`（`UIstate==1` ∧ ESC ∧ 设置窗没开）⇒ `BattleDriver.cs:5741 HandleSettingsEsc`（含「关窗同帧不重开」闸，`CloseAction` 的等价物）。

### B. K2 三条尾巴（判据逐条）

**B1 · 目标型卡两段式手势（🔴 仍不一致）**
- 原版整链（本轮逐段读出）：
  1. 拖过阈值 ⇒ `CanPlayCard` → `CardScript.MoveIntoFieldFromHand`；`cardType == 0` ⇒ `UIstate = 3`；`cardType == 0x14(20) / 0x28(40)`：`targeted == 0` ⇒ `UIstate = 5`，**`targeted != 0` ⇒ 播 `PlayMinionFromHand` + `UIstate = 4 (playingTargetedSpell)`** —— `dc/BattleManager__Update.c:191-310`。
  2. 目标型法术进 `_ChooseUnitTarget` 协程：`StartTrackingFrom(cardPos, 3)` → `ClockManager.UnpauseForAnim` → **tooltip = `RawCardScript.GetLocalizedTargetted()`**（卡 2D 位 + 偏移）→ `ShowPotentialTargets(card, mode=1)` → **等 `UIstate == 9`**；若中途离开 9 ⇒ `StopTracking` + `EverguildTooltipManager.Hide` + `ClockManager.PauseForAnim` —— `dc/BattleManager._ChooseUnitTarget_d__780__MoveNext.c:24-75`。
  3. 单位卡的指向性另走：`dc/BattleManager__NeedToChooseMinionTarget.c`（`cardType == 0` ∧ `targeted` ∧ `matchType != 0xA0`）→ `_ResolvePlayCardFromHand` 里 `IsThereValidUnitTarget` 为真 ⇒ 记 `+0x338` + `UIstate = 9` —— `dc/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:370-395`。
  4. 点目标那一下（`UIstate == 9`）：点到 ≠ 自己 ⇒ 过 `castingCard.IsValidTarget(target)`，成功则 `+0x338 = target` —— `dc/BattleManager__Update.c:1103-1163`；协程续跑把 `action+0x20` 写成目标 —— `_ResolvePlayCardFromHand…:905-940`；最终 **`CardScript.TargetedSpellPlayed(card, target)`** —— 同文件 `:1263`。
- 我们现在：**一段式** —— `Hand/CardInteraction.cs:1304 Release` → `:1191 DropReject`（按 `EffectText.PickSide` 判侧）→ `BattleDriver.OnCardDeployed/DoPlay`。`grep pendingTarget/commit threshold` 全类 0 命中。
- 数据侧已就绪：`cards_engine.json` 的 `targeted` 列已有值（**unit 50 / tactic 181 / defence 15**）；`RuleEngine/Core/CardDef.cs:250 Targeted` 已解析。
- ⇒ 落地需要 `BattleDriver` 新增「已出手、等点目标」态 + 卡片视图从手牌搬到场上 + 点目标后带 targetSlot 结算；**不在本代理白名单** ⇒ §③·R1（P0）。

**B2 · 技能友方 / 任意目标**
- 原版候选侧 = `ActiveAbility.targetCriteria`（`friendlyUnits`/`enemyUnits` 两格；`includeSelf` 是过滤器、不是第三侧）；`dc/BattleManager__IsValidActiveAbilityTarget.c` **没有任何「候选恒敌方」的硬编码**，它只做：异侧 `stealth(0x46=70)` / `ward` 不能点（`:55-62`），其余交给 `CardScript.IsValidActiveAbilityTarget`（vtable `+0x328` → `AbilityLogic.GetTargets`）。
- **引擎侧 2026-10-25 已补**：`RuleEngine/Core/RuleCore.cs:10689 AbilityPickOwnersOf`（Friendly⇒`p` · Any⇒`1-p` 再 `p`）、`:11405-11415`（替代行动/主动能力按规格自己那一侧取）。
- **表现层仍恒敌方**：`BattleDriver.cs:14319 AbilityPickOwner` 三档一律 `1 - _me`（注释理由「引擎结算层只认敌方」——**已过期**，引擎已改）。
- **但现池可达性 = 0**：`activeAbilityPickCriteria` 共 107 行 —— **只友方 24 行 / 敌我双方 36 行 / 两侧都没标 (0,0) 35 行 / 只敌方 12 行**；其中 **`friendlyUnits == 1` 的 60 行**所挂的卡，**正文全部没有 `Ability:` 段**（逐卡 `grep "Ability:" desc` 全 False）⇒ `CanStartAbility` 先回 `ErrNoAbility`，永远走不到 `AbilityPickOwner`；玩家真正能点友方的是**替代行动**（Duty/Pray/Agenda/Oath），那条早已按正文分侧（`AlternativeTargetSide`）。
- ⇒ 结论：**玩家可见缺口 = 0**；`AbilityPickOwner` 三档是**防御性残项（P2）**，等出现「`Ability:` + 友方 criteria」的卡才变可见。

**B3 · 攻击点自己人要不要出声（✅ 已一致，K2 原判断不成立）**
- `dc/BattleManager__IsValidAttackTarget.c:123-124`：
  `if (*(char *)(param_3 + 0x40) == *(char *)(param_2 + 0x40)) return 0;` —— **同侧直接静默返回**，位置在**任何** `param_4`（notify）分支之前 ⇒ 原版**不出声**。
- `dc/CardScript__CancelAttack.c` 的 `param_2` 是状态闸（`cardState` 3/0x11），不是 notify ⇒ 整条路无声。
- 我们：`BattleDriver.cs:13770 CancelTargetChoosingByInvalidClick`（Ability ⇒ `InvalidTarget`；**攻击 ⇒ 静默**）⇒ **一致，不需要改**。

---

## ③ 改动清单

| # | 文件 | 改了什么 | `git diff --numstat` |
|---|---|---|---|
| 1 | `Unity/MyGame/Assets/CardPresentation/Battle/CardChoicePanel.cs` | 新增 `Update()`：空格/回车 = 「继续」（内含 Visible + interactable 闸，未选牌什么都不做）；加 `using UnityEngine.InputSystem;` + 判据注释 | **+22 / −0** |

**改动请求（不在本代理白名单，按「探针 → 执行代理」口径交调度台）**

- **R1（P0）两段式目标手势** —— 归 `BattleDriver.cs`（代理 A）+ `Hand/CardInteraction.cs`（本代理可配合）：
  判据 = §②·B1 四段；数据 = `cards_engine.json.targeted` + `CardDef.Targeted`。
  最小落地：① 拖过阈值且 `targeted==1` ⇒ 走「出手但不结算」：卡视图搬进场 + `UIstate=4/9` 等价态 + `ShowPotentialTargets(mode=1)` + tooltip `GetLocalizedTargetted`；② 下一次点目标 ⇒ `DoPlay(card, targetSlot)`；③ 点空白/非法 ⇒ `Cancel`（原版 `Update case 9` 失败支：`InvalidTarget` 词条，技能那档；法术档回 `StopTracking`）；④ 回放/联机 `matchType==0xA0` **不进**两段式（`NeedToChooseMinionTarget` 的 `!= 0xA0`）。
- **R2（P1）回放局设置窗按钮文案** —— 归 `BattleDriver.cs`（A）+ `Battle/SettingsPanel.cs`（本代理）：`dc/BattleSettingsWindow__Open.c` 在 `matchType == Replay` 时把 `resignButtonText.Term` 换成 `Battle/Settings/Exit`。我们 `SettingsPanel.cs:443 ResignTerm` 恒 `Battle/Settings/ResignButton`，需要 `SettingsPanel` 暴露 `SetResignTerm(...)`（或 `Create(..., bool replay)`），由 `BattleDriver.cs:27468 SettingsPanel.Create(root, Forfeit)` 那一行传 `Ctx.MatchType == MatchTypes.Replay`，并在进回放时刷一次。
- **R3（P1）`BattleHud.SetTurnPlayedCards`** —— 归 `BattleDriver` HUD：`dc/BattleHud__SetTurnPlayedCards.c:267-307`（三枚「本回合已出牌」灯，`offensiveCardTurnCardsObjs` 逐个 `SetActive`，`cards<=0` 全灭）；我们 `grep offensiveCardTurnCards` **0 命中**。
- **R4（P1）`BattleCardUI.UpdateBastionr`** —— 归 `Core/CardView`（E）+ `BattleDriver`：`dc/BattleCardUI__UpdateBastionr.c:1958-1978`（**不在手牌时**、`CurrentBastion > 0` ⇒ 显示数字，否则隐藏）；我们 `grep -i bastion` 在 `CardPresentation/*.cs` **0 命中**。
- **R5（P2）`AbilityPickOwner` 放开 Friendly** —— 归 `BattleDriver.cs:14319`：把 `AbilityPickSide.Friendly` 返回 `_me`（引擎 `RuleCore.ResolveAbilityTargetOwner` 已按 criteria 解 owner，`UseAbilityCore` 用 `pickedEnemySide`）；`Any` 需要「两侧都能点」的候选模型（现池 0 张，留到有数据再说）。⚠️ 先确认 `BuildAct` 的 `TargetP` 随之变 `_me`（录像/观战指向才对）。
- **R6（P2）选牌面板倒计时**（`ChooseCardMenu.CountdownTrigger/SetTimer/EndSelectionWithDelay`）—— 归 `BattleDriver` + `Battle/CardChoicePanel`（可配合）：判据 `dc/ChooseCardMenu__CountdownTrigger.c`（未选 ⇒ `Random.Range` 选一张；按钮文字 `"0:{秒:00}"`；`counter < 3` ⇒ `ProcessChooseDone`）+ `EndSelectionWithDelay.c`（停 0.5 s 同样处理）；驱动侧由 `ClockManager.CountdownTrigger`（`UIstate == 0xE`）喂。

---

## ④ 断言

- 本轮**没有新增战斗断言**（自检暂停；且我这条域内**没有 P0 改动**，新增行为只有键盘确认）。
- 建议随 R1~R6 一起落的断言（**待自检恢复时跑**，宿主 `Editor/BattleScene.cs`，本代理白名单内可写）：
  1. **B1 两段式**：拖过阈值后 `assert card 已进场 && 仍在等目标 && 引擎状态未结算`；点合法目标后 `assert 结算一次`；点空白后 `assert 未结算且回手/取消`。**灭自证**：把「出手即结算」改回去 ⇒ 第一条必红。
  2. **B3（已有行为，补钉住）**：攻击选目标时点自己人 ⇒ `NotifyCantDoAction` 调用计数**不变**（判据 `IsValidAttackTarget.c:123-124`）。灭自证：给攻击档补一声 `InvalidTarget` ⇒ 红。
  3. **R2**：`matchType==Replay` 打开设置窗 ⇒ `Resign` 文本 == `Loc.T("Battle/Settings/Exit")`；改回恒 `ResignButton` ⇒ 红。
  4. **本轮改动**（键盘确认）：`CardChoicePanel` 选中一张后模拟 `Space` ⇒ `DoneClicked` 触发一次；未选时按 ⇒ 0 次（对应原版 `ClickChooseDone.c:3-6`）。
- ⚠️ 断言只作「防回归」，**不当交付标准**（用户 2026-10-25 口径）。

---

## ⑤ 没查清（⛔ 没猜）

1. **`CardDisplayWindow.CardUIClicked()`** —— ILSpy 产物 `Invalid instruction: 30 Invalid "Jump target not found"`，方法体读不出来（唯一一条 E）。要补得走 VA 反汇编。
2. **`GetCardDrawPosition`（抽牌起点）** —— 原版读 `playerCardDrawTransform` / `enemyCardDrawTransform` 的 x/z；我们按牌堆位自算，**没逐字段比**（标 B）。
3. **`BattleTipController.SetBGColor` 的「红底」档** —— 常量与调用条件本轮没逐条核（原版 6 个 `ShowHeadsUpMessage` 调用点**都传 0**，所以玩家看不到红底；我们照此没实现红档）。
4. **技能面板「技能名 / 费用」** —— 原版 `ActiveSkillDisplay.SetupActiveSkill` 用 `Card_Abilities/Ability_Name_<id>` 与 `parentCard.currentAbilityCost`；我们的 `cards_engine.json` **没有这两个字段**（只有 `activeAbilityLogic` 等）⇒ 现在用卡名 + 费用固定 `0`（如实标 B，不是猜）。
5. **`TipPrebattle` 的锚点/尺寸** —— 整件未建，12 个字段（`openBgWidth 1.8` / `timeToOpenBg 0.3` / 文本层）都没抄进代码；这 10 条全标 C(P2)，不是「查不到」。
6. **`BattleCardUI.SetVanguardStealth` 的 0.4/1.0 两档 α** —— 我们的 `WarpforgeVatDriver` 有没有照这两档跑，**没逐字段核**（标 B）。
7. **原版「底板点击」那一刻是否还有一次性高亮/音效** —— 本轮只证到「结果 = 取消攻击 + 收选择器」，中间是否有音效没读（不影响结果）。

---

## ⑥ 顺手发现（⛔ 我没改别人的文件）

1. 🔴 **K2 #9 的判断不成立（铁律 5 就地订正）**：K2 写「攻击档点自己人，原版会经 `IsValidAttackTarget` 出声」——
   `dc/BattleManager__IsValidAttackTarget.c:123-124` 同侧**先 return 0**，在 notify 分支之前 ⇒ **原版静默**。
   ⇒ `AUD8 #16` 的「剩余半」可以**核销为「已一致」**，不用再为它动 `Resolve`（那是白改）。
2. 🔴 **AU1 留给 `AbilityPickOwner` 的理由已过期**：那条 doc 写「引擎结算层只认敌方（`UseAbilityCore` 的 `chosen = ctx.Players[1-p]`）」——
   引擎 **2026-10-25 当天**已被 `E8/E5` 改成按 criteria 解 owner（`RuleCore.cs:10689` / `:11405-11415`）。**doc 该订正**，否则下一个人会继续拿旧理由挡这条账（→ §③·R5）。
3. **「技能友方」类账的可达性口径**：K2/AU1 记「玩家点不到友方目标」听上去像玩家可见缺口；实测**现池 0 张可达**（60 行友方 criteria 的卡正文都没有 `Ability:` 段）。以后若卡表新增 `Ability:` + 友方卡，这条会**立刻**变成可见缺口 ⇒ 建议在卡表生成器加一条**告警**（「Friendly criteria 但正文无 Ability:」是数据异常还是设计？）。
4. **`BattleHud.GetCardDrawPosition` 的两个 anchor 节点**（`playerCardDrawTransform` / `enemyCardDrawTransform`）在原版场景里是**独立节点**，我们抽牌起点是「牌堆位 + 自算偏移」——若以后要精确对齐抽牌弧线起点，这是第一条要补的。
5. **`ClockManager.CreateInOutAnim` / `CreateNoActionsAnim` 两件的 prefab 13/13 场全 null**（`inOutAnimPrefab`/`inAnimPrefab`/`noActionsAnimPrefab` 的 `m_PathID` 一律 0，全量反编译**零赋值点**）⇒ 原版战斗里这两条**恒 early-return**；我们「没做」= 原版（例外①，已在账里写清，⛔ 别再当欠账）。
6. `CardChoicePanel` 的 `ShadeAlpha` 现在**转发** `MulliganPanel.ShadeAlpha`（Linear 工程 0.945，γ 口径 0.8）——两份值并存容易看混，已在两处注释里写明「⛔ 别把这行写成 0.8」。
7. `PracticeModePopup` 落点在 `Shell/PracticeModePopup.cs`（C 的文件），本轮只记不核（16 条里 15 条标 B）——**界面线复刻时要把它连同 `BattleDriver.cs:4397` 的 `TakePendingOpponentDeck()` 一起核**。

---

## ⑦ 跨账复核：A 账里 **75 行「归B」**（HUD / 表现层）

> 这些行在 `账/战斗逻辑驱动.tsv`（⛔ 别人的 TSV，我只读不改）。按简报「BattleManager 里 HUD/表现相关的行（归B）优先」逐组复核，结论如下。

| 组 | 行数 | A 的标法 | 复核结论 |
|---|---:|---|---|
| `MinionManager` 摆位/让位补间（`AdjustMinionBoardPos` · `AreMinionsOutOfPos` · `FillMinionPositions` · `GetSlots` · `GetLeftSlotPos` · `GetClosestAvailableSlot` · `GetMinionConversionTime` · `MoveMinionToConversionPoint` · `ReassembleMinions` · `ReassembleMinionsWhilePlayingUnit`） | 10 | 3×B + 7×**D「纯表现层」** | 🔴 **D 用错**：表现层就是本域。我们的落点齐全 —— `Board/BoardLayout.cs`（格位/姿态）· `Core/CardFeel.cs` 的 `Reassemble`（让位补间）· `BattleDriver.OnDropPreview`（拖拽预览）· `BattleDriver PoseFor`。建议改判 **A/B**（`GetClosestAvailableSlot` 那条 A 已写了「满侧不进候选」的 P1 疑点，归 `CardInteraction.DropReject`，本轮核到它按 `BoardSpec.IsValid` + 最近格算，**未见满侧错位**，保留其 P1 待真机）。 |
| `PlayerHand` 手牌视图（21 行：`PositionCardInHand` · `SetupCardInHand` · `RemoveCard` · `ShowHandSize` · `StartMovingHandCard` · `MoveCardsInHandToPosition` · 换牌四件 `SetupCardInMulligan`/`MoveCardToMulliganPos`/`GetMulliganCardsToDiscard`/`FinishMulligan*` · `DisplayHandWithCardSpace` · `HandCardMovedToNewIndex`/`Released` · `LightPlayableHand` 等） | 21 | **全部 D「HUD：…」** | 🔴 **D 用错**：它们是手牌/换牌的表现层，落点全在 `Hand/HandLayout.cs` + `Hand/CardInteraction.cs` + `Battle/MulliganPanel.cs`。建议改判 **A/B**（`LightPlayableHand` 的「可出牌高亮」= `CardHighlight` 档，本轮在 `CardInteraction.UpdateHover` 里核到）。 |
| `CardTraitCollection` 三个图标口 + `EntityScript.IsShowingTrait` / `GetCardEffectsToShow` / `RawCardScript.CountShowTraits` / `ShowDefaultTrait` | 7 | D「纯表现层」 | 落点 = `Core/Badges.cs`（关键词/词条图标）· `Core/TraitFrames.cs`（触发框）· `CardDisplayWindow` 的 EffectList。建议 **A/B**。 |
| `AbilityLogic.CreateTempAbilityTransform` / `PlayAnim` | 2 | D | 落点 = `Core/CardView` 的 `effectsAnchor` + `WarpforgeVFX` 播放器。建议 **B**（锚点与播放链已存在，未逐条比参数）。 |
| `RawCardScript` 的声音/聊天/粒子取数（14 行：`GetAttackSound` `GetDeathSound` `GetSummonSound` `GetChatSound` `GetChatId` `GetLocalizedChatMsg*` `GetSummonChat` `GetRangedAttackParticles` `HasCustomRangedAttackParticles` `GetAvailableSounds` …） | 14 | D「纯表现层」 | 域归属 = **音频/语音线 + 特效线**（不在本域）。可保留 D，但理由应写「**归音频/特效线**」而不是「纯表现层」。 |
| `SupportMethods` 工具（`SetSpriteAlpha` ×2 · `SetTextAlpha` ×2 · `SetImageAlpha` · `SetAllChildrenAlpha` · `FadeAllChildren` · `DestroyAllChildren` · `GetChildGameObject` · `Multiply/Offset/With(Rect…)` · `GetUnitSize` · `GetPushBackFactor` · `InterpolateMilestones` · `IsScrollDragThreshold` · `ShowRacePlaque` · `ShouldShowOriginalRemnantCard` · `SetTerm` …） | 16 | D | 大多数是**纯工具**，替代物分别是 `HudPiece.SetTint/SetAlpha` · `Label.SetColor` · `CardText` · `HudCanvas` · `CardView`。保留 D 可以，但必须写**替代物**（plan §4.1 的 D 规则），现在写的是「纯表现层」。 |
| 其它（`MinionManager.BroadcastActivateMinion` · `MoveMinionToConversionPoint` 等） | 5 | D | `BroadcastActivateMinion` = 原版用 C# 事件刷 HUD；我们是驱动直接刷 ⇒ **D 成立（替代物 = BattleDriver 刷新）**，理由写对了。 |

**给调度台的一句话**：A 账里「归B」的 **75 行中约 40 行（MinionManager 摆位 10 + PlayerHand 21 + trait/effect 7 + AbilityLogic 2）被 D 掉了，理由「纯表现层」不成立** —— 它们正是本域的复刻对象；本轮已给出落点，建议 A 在下一轮把它们改判 A/B（⛔ 不改他的文件，我只报）。


---

**收工口径**：账 679 行已全部落盘（1 条 E 是工具读不出）；八块每块有结论；K2 三条尾巴全部有判据结论（1 条仍开 = R1）；`typecheck` 运行时 0 / 编辑器 0；行尾未翻。
