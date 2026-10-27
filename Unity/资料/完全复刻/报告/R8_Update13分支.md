# R8 报告 —— 原版 `BattleManager.Update` 逐分支核完（第八十七会话 · 2026-10-27）

**产出三件**
1. 判据正本：`资料/完全复刻/判据/BattleManager_Update_13分支_判定.md`
2. 账补丁：`资料/完全复刻/账/_parts/B87_R8_Update分支.tsv`（1 行，改主表 `战斗界面_HUD.tsv:741` 那一格）
3. 本报告。

**一句话结论**：`Update` 不是「13 条分支」，是 **18 段**（原版 `switch` 只有 9 个 `case` 覆盖 10 个 `UIstate` 值，另 8 段在 switch 之外）。
逐段核完：**11 段 A（已复刻且一致）· 4 段 B（待核）· 4 段 C（真缺口）**（含 B04 按桌面档计入 A；B04 的移动档那一半另记 = B）。
最贵的那条（B05 牌数收起）之前一直卡在「`FUN_180617800` 没有反编译产物、判不死」—— **本轮用机器码坐实了它 = `TouchInputManager.get_TouchPressed`**。

---

## 一、18 段各一行结论

| # | 分支 | 判定 | 一句话 |
|---|---|---|---|
| B01 | 初始化闸 + `gameTime` 累加（`:97-103`） | **A** | `Ctx == null` 返回已对；我们**没有 `gameTime` 累加器**，但该字段在反编译全树 `BattleManager` 之外**0 消费者** ⇒ 无行为差异 |
| B02 | 连接状态整帧闸（`:104-115`） | **A** | `PlayerInputFrozen && !Ctx.IsOver`（`BattleDriver.cs:14189`），判据逐句已写在 `:14163-14188` |
| B03 | 赛后「点一下就离开」（`:116-122`） | **A** | `HandleEndBattleExit`（`:12620`） |
| B04 | 每帧鼠标世界坐标 + `targetCard = GetTargetInPos(指针 + targetYOffset)`（`:123-139`） | **A**（桌面档）· **B**（移动档） | 桌面档 `targetYOffset = 0.0`（现读 `VarsDesktop.json`）⇒ 零差异；移动档 0.2 未做。我们**不存 `targetCard` 字段**，逐消费者各查一次 |
| B05 | 牌数/手牌数「按住才亮」的收起（`:140-151`） | **A** | 收起的另半个条件 = `!TouchPressed`，**本轮坐实**；现存 `CardHoldPeek`（`:17063-17116`）与判据**同义**。之前记的「另半个条件没反编译出来」可以划掉 |
| B06 | 音效队列泵 `PlayNextSoundInQueue`（`:152`） | **A** | `BattleSfx.Tick/DrainOne`（`BattleSfx.cs:290/295`）+ PlayerLoop 泵 |
| B07 | 取手牌上限线（`:153-158`） | **A** | 我们用常量 `BoardLayout.HandLimitY`（= 300/1080，`A2104①` 核过）；原版取场景节点 `HandLimitArea` |
| B08 | case 1 `normalBattle`（`:160-190`） | **C**（最低危） | 缺 `AiScripted.CheckIfWaitingToUseAbility` 那条诊断。**原版这一支通篇只有一条 `LogWarning`、零局面影响** |
| B09 | case 2 `movingHandCard`（`:191-349`） | **B** | 缺 `moveCardYOffset = 0.2`（我们用抓取偏移）；出声时机不同（原版拖动中、我们松手时） |
| B10 | case 3 `playingMinion`（`:350-625`） | **B** | 同缺 `moveCardYOffset`；**蜂群预览**（trait `0x28A`）待核 |
| B11 | case 4 `playingTargetedSpell`（`:626-830`） | **C** | **拖目标型法术时原版每帧画「督军 → 指针」准星，我们一根都没有**（`UpdateReticle` 要求 `_selectedSlot >= 0`，而拖拽时 `DrivePlayerTurn` 先 return） |
| B12 | case 5 `playingNonTargetSpell`（`:831-985`） | **B** | 同缺 `moveCardYOffset` |
| B13 | case 6 `minionAttacking`（`:986-1096`） | **A** | 三条路（主动技 / 攻击 / 取消）都有；三处结构性差异（抬起沿、自造「点槽外取消」、`isAttack` 实参）已记 |
| B14 | case 8 `postBattle`（`:1097-1102`） | **A** | 我们多一道 `ExitReady`（= 开门演出闸），原版状态 8 本来就在演出之后 ⇒ 等价 |
| B15 | case 9 + 13 选单位 / 选技能目标（`:1103-1185`） | **B** | **状态 9 的「点一下只是筛 `targetedUnitTarget`」这层我们没有**（带 `CanBeTargetedBy` 谓词，本仓 0 命中）—— 待核它影响哪张卡 |
| B16 | case 10 `targetedActiveAbility`（`:1186-1248`） | **A** | `CancelTargetChoosingByInvalidClick`（`:14715-14738`）逐句照 `FinishedChoosingAbilityTargetByClick.c`；只缺准星终点的 `moveCardYOffset` |
| B17 | 四道动作队列泵（`:1250-1301`） | **A**（等价） | `ActionPump.ActionScope`（`RuleEngine/Core/ActionPump.cs:75-193`）+ `ActionQueue` 消费序 2→1→3→0。「每帧一条」vs「动作边界排空」是已记的设计等价（`ActionPump.cs:20-40`）。**`duringBattle/duringScriptedActions` 那道外层闸我们没有**（等价物 = 开局前没人入队） |
| B18 | ESC ⇒ `ClickChat`（`:1303-1335`） | **C** | **缺「ESC = 按一下聊天按钮」**（含状态掩码 `0x402C`）。ESC 在原版有**两个**消费者，我们只做了 `BattleHud.Update` 那条（开设置窗） |
| B19 | 回放局键位（`:1336-1413`） | **C** | `A`/`←` 重播、`D`/`→` **15× 快进**、`Space` = 自动播放 —— 三条都没有（全仓 `Time.timeScale` **0 处**）；我们的 `Space`(暂停)/`.`/`R` 是自设计键 |

---

## 二、C 清单（按严重度）

### C-1 `B11` 拖目标型法术时没有准星 —— **最会被玩家看见的一条**

- 原版 `:800-820`：状态 4（`playingTargetedSpell`）按住期间**每帧** `MoveCrosshair(playerManager.heroCard.transform.position, 指针 + moveCardYOffset)`。
- 我们：`DrivePlayerTurn` 在 `interaction.IsDragging` 时**早退**（`BattleDriver.cs:14774`），`UpdateReticle` 又要求 `_selectedSlot >= 0` 且锚点是**选中单位** ⇒ 屏上没有任何准星。
- 落点：`CardInteraction.UpdateDrag`（`CardInteraction.cs:973`）里对「目标型法术」每帧喂一次准星；锚点 = **我方督军卡**（`_myWarlordView` 那一族），终点 = `指针 + moveCardYOffset`。
- 验收口：`BattleScene.Run` 断言 —— 拖一张 targeted 战术卡过线后 `ReticleVisible == true` 且 `reticle.LastAim` 随指针变。

### C-2 `B18` ESC = 按聊天按钮（语音弹窗）

- 原版 `:1303-1335`：`ESC 按下沿 && UIstate != postBattle` 且 `!blockingOverlay.isSpinning` 且 `voiceLinesController.IsPopUpActiveAndEnabled`；
  状态 ∈ {1,6,9,10,13} ⇒ `VoiceLinesController.ClickChat()`；状态 ∈ {0,7,8,11,12} ⇒ 只打一条 `"ClickChat but not during battle"`。
- 我们：ESC 只开设置窗；ESC **关**聊天气泡有，**开**没有。
- 落点：`BattleDriver.Update` 里紧挨 `HandleSettingsEsc`（`:14277`）之后新开 `HandleChatHotkeyEsc()`，动作复用 `ClickChatHud()`（`:5891`，聊天按钮的唯一落点，别另写一份）。
- 验收口：钉死 `EscapePressedForTest` ⇒ 断言 `_chatPopup.Visible` 翻转；摆成 `postBattle` ⇒ 断言**不**翻转。

### C-3 `B19` 回放局键位（15× 快进 / 重播）

- 原版 `:1336-1413`（`matchType == 0xA0`）：`D`/`→` ⇒ `Time.timeScale = 15`（`DAT_1834b3164` 现读 = 15.0）+ 排下一条回放动作；`A`/`←` ⇒ 重载当前场景（重播同一份）；`Space` ⇒ 切自动播放。
- 我们：回放条四颗钮（Replay/Play/Pause/Step）有；键盘 `Space`(暂停)/`.`(单步)/`R`(重开) 是自设计；**速度档整条不存在**。
- 落点：`HandleTimeControlKeys`（`:8673`）加 `D`/`→` ⇒ 新的 `SetReplaySpeed(15f)`（`Time.timeScale` 的**唯一**读写口，退出回放必须复位 1）；`A`/`←` ⇒ 复用 `Restart()`。
- 需裁一句：`Space` 我们已经是「暂停/继续」，原版是「自动播放开关」—— 冲突，建议按原版、把暂停留在回放条 `Pause/Play` 钮上（原版本来就是钮）。
- 验收口：回放局按 `D` ⇒ `Time.timeScale == 15f`；退出 ⇒ 回 1；按 `A` ⇒ 起始条件指纹逐字相同。

### C-4（最低危）`B08` 教程「等玩家用主动技」的诊断

- 原版 `:160-190` → `AiScripted.CheckIfWaitingToUseAbility.c`（返回真 ⇒ 一条 `LogWarning`）。
- **该支通篇零局面影响**。铁律 11 ⇒ 仍要补，排最后。
- 落点：`TutorialFrameGuard`（`BattleDriver.cs:16582`）旁；谓词照 `TutorialRules.IsWaitingChangeAttack`（`RuleEngine/Core/TutorialScript.cs:1828`）的形状新建。
- 验收口：`TouchPressedForTest` 钉「按住」+ 教程第 N 关 ⇒ 断言日志出现过那条 warning。

---

## 三、还差什么

1. **B 五条**（都写在判据文档 §四）：`moveCardYOffset = 0.2`（影响 8 个 case 的拖卡与准星终点，值已现读）· case 3 蜂群预览 · case 9 `CanBeTargetedBy` 筛选层 · case 2 两段式 `CanPlayCard` 出声时机 · 移动档 `targetYOffset = 0.2`。
2. **`CanBeTargetedBy` 的原版方法体还没读**（`decomp_full/RawCardScript__CanBeTargetedBy.c` 是否存在、体是什么）—— 这是 B15 唯一未定的东西。
3. **`HandLimitArea` 在 13 个战场是不是都等于 300** 未逐场核（我们已把它换成常量）。
4. 三份旧文档里「`FUN_180617800` 判不死」的说法已过期，**本批不动 `.cs`** ⇒ 需要另派一条「零行数变化」的注释订正（`BattleDriver.cs:8084` 的 `自己设计` 理由 + `:17062` 的 doc）。

---

## 四、顺手发现（只报不改）

1. **`out_il2csharp` 是这一族最好的控制流源**：`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:25427-26736` 有完整标签/字段名/化简条件；
   而 ILSpy 那份把整个主 `switch` 丢成 `Indirect jump … should have been resolved before IL gen`。
   ⇒ 建议口径补一句：**读控制流首选 `out_il2csharp`，读字段名查 `dump.cs`，读真常量回 `.c`**（这不推翻「只有 dev_ilspy 有真方法体」，只补「有体时哪份更好读」）。
2. **`FUN_180617800` = `TouchInputManager.get_TouchPressed`（= `Input.GetMouseButton(0)` 每帧快照）已经坐实**：
   两条 thunk（`0x180617800` / `0x180789A30`）读的都是**同一个 class_slot `0x1842C22E8`**，静态偏移 `+0x8`(bool) / `+0x0,+0x4`(Vector2)
   与 `dump.cs` 的 `TouchInputManager`（`TouchPosition@0x0` / `TouchPressed@0x8`）逐格吻合；写入侧 = `TouchInputManager__Update.c:128-136`。
   ⇒ `A2` / `AU1` / `AU2` 三处「判不死」的结论应删；`普查产出_1015战斗/B3:29` 的结论是对的（虽然它给的地址对不上，那是 IL2CPP 发的重复短体）。
3. **`VarsDesktop.json` / `VarsMobile.json` 本机有值**（`assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/`）：
   `targetYOffset 0.0 / 0.2` · `moveCardYOffset 0.2 / 0.7` · `cardInHandShownScale 1.3 / 1.7` · `cardInHandShownYOffset 30 / 25` · `warlordStartSizeFactor 1.3 / 1.5`。
   ⇒ `B9_剩余C_施工单` 第 39/102/443 行「两份 SO 未导入 / 取不到 ⇒ 值只能自定」**是错的**（同「翻过一个镜像目录就写『本地没有』」那一族）。
4. **简报里「13 分支」这个数字要停产**：`switch` 9 个 `case` / 10 个 `UIstate` 值 / 加上 switch 外 8 段 = **18 段**。已写进判据文档 §1.4。
5. 我们**没有 `duringBattleFlag` / `duringScriptedActionsFlag`** 这对字段，且 `gameTime` 也没建。前者建议**别补**（补了会让换牌期的入队不结算，见 `ActionPump` 那一段）；后者无消费者，可不建。
