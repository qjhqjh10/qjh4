# B139 诊断 · `BattleScene.Run` 9 红（只读诊断，未改任何代码）

日志 = `d:/4/_tmp_view/unity_b139_battlescene.log`（1,728,705 行）· 合计行 `:1728608` `=== 合计：11855 通过 / 9 失败 ===`
9 条失败与任务书给的 9 条逐条对上（行首标记 `^<名字>\s+✗` 全表只有 9 条，已核）。

## §0 结论一句话 + 根因分组

**9 条里只有 1 条（#4）是 B139 造成的；其余 8 条全部来自已提交的 `81ea4fbb`（B125–B137，两处都记在 `B130` 名下）—— 6 条是 `_command` 起手改用 `CurrentAttackType`，2 条是 `TapThresholdPx` 10→5。B139 的相机改动（透视/tag 对调）不是任何一条的原因。**

| 组 | 根因 | 条数 | 成员 |
| --- | --- | --- | --- |
| **G1** | `B130`：`OpenCommand` 把 `_command` 从 `u.LastAttackType`（部署初值 0 ⇒ `None`）改成 `u.CurrentAttackType`（`(近战<远程)+1`，**永不 0**）—— 见 `BattleDriver.cs:15325`。三处旧断言/夹具都建立在"开选择器 ⇒ `_command == None`"上 | 6 | #1 #2 #3 #5 #6 #7 |
| **G2** | `B130`：`CardInteraction.TapThresholdPx` 由 `10f` 改成 `AttackSelector.PopDragThresholdPx`（=5）— 见 `CardInteraction.cs:110` ⇒ 批处理 480p 下 `TapThreshold` 从 0.2083 掉到 **0.1042**，夹具靠"横向补足 2×阈值"凑出来的拖距反而落到 0.0849 | 2 | #8 #9 |
| **G3** | `B139-E`：伤害飘字由 `"-"+量` 改成原版的纯整数（`PopNumber.cs:236`）⇒ 断言拿的样本 `-1` / `-123` 实际已变成 `1` / `123`，3 位数不再溢出 80.9 px 的框 | 1 | #4 |

**四类占比**：α 2 条（#1 #4）· β 1 条（#2）· γ 0 条（B139 本体没造成红）· δ 6 条（#3 #5 #6 #7 #8 #9）。
注：#2 的**触发**是 `81ea4fbb` 的回归，但它暴露的是**产品侧一处真偏离**（我方卡自己的打法底光在"选的档 = 当前档"时永远不亮），故判 β 并把回归记在根因列。

## §1 逐条

| # | 断言 | 分类 | 根因 | 证据（文件:行号 / 日志行号） | 怎么修（方向，不实施） |
| --- | --- | --- | --- | --- | --- |
| 1 | R20②（如实记）：AM69 的 `Card.Ability == null` ⇒ `CommandTargetSide` 在到候选侧那一支之前就回 `-1` | **α** | `CommandTargetSideFor()` 先判 `cmd == None ⇒ -1`（`BattleDriver.cs:15468`）、再判 `cmd != Ability ⇒ 1-_me`（`:15469`）；`_command` 现在开窗即 `Melee` ⇒ 走第二条，回 1 而不是 -1 | 断言 `BattleScene.cs:2423-2425`；判据 `BattleDriver.cs:15325`、`:15468-15469`；日志 `:660856`；上一轮同一条 ✓ `battle.log:660821` | 把这条"如实记"改成先 `SetCommandForTest(AttackKind.Ability, s)` 再读（它验的是"AM69 走不到候选侧那支"，与起手 `_command` 无关）；文案同步改成"Ability 档下" |
| 2 | FP-HUD-003：选完打法那一刻（结算前）`LastAttackType` 已写成近战 | **β** | `CommitCommand` 第一句 `if (kind == _command) return;`（`BattleDriver.cs:15646`）在"选的档 = 起手档"时提前返回 ⇒ **`picked.LastAttackType = pickedType`（`:15691`）与 `ApplyOwnAttackType` 全被跳过**；而起手档现在恒 = 本单位的当前攻击型（该单位只有近战一档，见本轮日志 `:675228`）⇒ 这条路径成了**常态** | 断言 `BattleScene.cs:9441`；早退 `BattleDriver.cs:15646`；写点 `:15691`；显示消费 `OwnAttackTypeOf` 读的是 `LastAttackType`（`BattleDriver.cs:15965`，`ApplyOwnAttackType` 在 `:15978`）；日志 `:675603`；上一轮 ✓ `battle.log:675568` | 二选一：① 把 `LastAttackType` 的镜像写在早退**之前**（`kind == _command` 也要镜像成 `pickedType`）；② 更彻底 —— `OwnAttackTypeOf` 改读 `CurrentAttackType`（原版 `HighlightAttackTypes` 打的就是 `+0x120`），`LastAttackType` 只留审计。⚠️ 断言文案里的 `BattleDriver.cs:15677` 是旧行号（现为注释行），实际写点是 `:15691`（坑 ④） |
| 3 | 悬停不再改变按钮大小（1.00 → 1.33） | **δ** | 开窗时 `selectedType = u.CurrentAttackType`（`BattleDriver.cs:15412`）⇒ `Show()` 里 `_selected = Ranged`（`AttackSelector.cs:1166`）⇒ `RefreshPicked()`（`:1266`）判 `changed` 为真 ⇒ `ApplyPickScale` 起一条 **0.3 s OutBack** 到 1.3（`:1733`）。`before` 在 t=0 读到 1.00，`Step(0.15f)` 后正是 OutBack 半程 ≈ **1.326**（日志 1.33）—— 与悬停本身无关 | 断言 `BattleScene.cs:9560-9563`；`Show` 的 selectedType 传参 `BattleDriver.cs:15412`；补间 `AttackSelector.cs:1733`；日志 `:676078`；上一轮 1.00→1.00 `battle.log:676043`（那一轮 `LastAttackType==0` ⇒ `_selected=None` ⇒ 不起补间） | 断言测"悬停不改缩放"要挑**非已选**那一格，或在 `Step` 前把补间走完再取 `before`；另见 §4 ① —— 开窗是否该跑 0.3 s OutBack 本身要回原版核 |
| 4 | 3 位数会缩：`-1` 55.52px vs `-123` 55.52px | **α** | 飘字已无符号（`PopNumber.cs:236`）⇒ 实际文本 `1` / `123`。`-123`（4 字符）才溢出 80.9 px 框；`123`（3 字符）按 55.52 px/em 约 1.18 em ≈ 65 px ⇒ **不溢出、不缩**，这正是原版 autoSizing 的行为（`PopNumber.cs:296-302` 的自动框与 0.35 下限都没动） | 断言 `BattleScene.cs:23446-23448`；文本 `PopNumber.cs:236`；框 `PopNumber.cs:296`；日志 `:742757`；上一轮 `-123` = 53.46px `battle.log:743060`（53.46/55.52 = 0.963 ⇒ 原始宽 ≈ 84 px，与"4 字符才溢出"自洽） | 样本换成 4 位数（如 1234）并同步文案里的 `-1`/`-123`；判据仍是"溢出才缩"，别改成"3 位必缩" |
| 5 | A462：这一帧没有选中任何打法（指针压着也不动） | **δ** | `DrivePlayerTurn` 里选择器那一支的早退 `if (_command == AttackKind.None) return;`（`BattleDriver.cs:15011`）现在**不再成立** ⇒ 流程落到 `ReleasedThisFrame()`（`:15057`）——而这条抬起沿是**上一段多卡窗夹具留下的脏沿**（`BattleScene.cs:34531-34532` 置 `PointerHeldForTest=false` + `PollInputEdgesForTest()`，之后没人消费；对拍 `TickMultiCards` 不再耗沿）⇒ 进 ④ `HitCommandTargetSlot(world) < 0` ⇒ `CancelTargetChoosingByInvalidClick()`（`:15097`）⇒ `_command != Ability` ⇒ `ClearSelection()`（`:14891`）⇒ `selector.Hide()` | 断言 `BattleScene.cs:34609-34611`；早退 `BattleDriver.cs:15011`；抬起沿 `:15057`；取消 `:15097` → `:14891`；`Hide()` 清 `Hovered/_hoverWas/_safeLeft` `AttackSelector.cs:1123-1130`；脏沿来源 `BattleScene.cs:34531`；日志 `:865812`；上一轮 ✓ `battle.log:866178` | 夹具侧：进这一段前把脏抬起沿吃掉（`ReleasedThisFrameForTest()` 消费一次或再 `PollInputEdgesForTest()`），并在断言里显式 `Check` 选择器仍开着；产品侧：`_command` 起手非 None 后，这一支的"松手即结算/取消"是否该在**没有真按下**时触发，值得单独核（见 §4 ③） |
| 6 | A462：安全窗走完（0.1s）⇒ 可以发（实得 0.000s） | **δ** | 连带 #5：选择器已 `Hide()`，`TickForTest` 第一句 `if (!Visible) return false;`（`AttackSelector.cs:436-438`）⇒ 安全窗根本没被推；`_safeLeft` 停在 `Hide()` 写的 0 ⇒ 读数 0.000、`HoverPickReady` 也恒假 | 同上；`TickForTest` 早退 `AttackSelector.cs:436`；`Hide()` 复位 `:1130`；日志 `:865838` | 同 #5 修完后本条自然恢复；另加一条"前提：选择器仍 Visible"的硬断言，挡"隐藏后被早退"的假绿 |
| 7 | A462：悬停即选中（选择器仍开着 False / 打法 None） | **δ** | 连带 #5（同一次 `ClearSelection` 把 `_command` 也清成 None） | `ClearSelection` 清 `_selectedSlot=_command=None` 并 `selector.Hide()` `BattleDriver.cs:16620-16632`；日志 `:865851` | 同 #5 |
| 8 | B121_Q4B（前提）C2 这一拖超过轻点阈值（0.0849 ≥ `TapThreshold` 0.1042） | **δ** | 夹具 `needTravel = TapThreshold * 2`，再 `inHand += (needTravel,0,0)`（`B121_Q4B_Assert.cs:352-354`）—— 这是**带正负号的横向补足**，卡 home 与 `inHand` 恰好横差 ≈0.1234 ⇒ 落点 = 2T − 0.1234。T 从 0.2083 掉到 0.1042 后：2T = 0.2083 ⇒ 落点 0.0849 < 0.1042（上一轮 2T = 0.4167 ⇒ 0.2921 ≥ 0.2083 ✓，日志对得上） | 夹具 `B121_Q4B_Assert.cs:348-360`；阈值源 `CardInteraction.cs:107-114`、`:110`；日志 `:1722549`；上一轮 0.2921 `battle.log:1722370`；阈值改动的提交 = `81ea4fbb`（`git diff a88c1ebd 81ea4fbb -- …/Hand/CardInteraction.cs`：`10f` → `AttackSelector.PopDragThresholdPx`） | 夹具改成**绝对落点**（`inHand = home + new Vector3(TapThreshold*2f, 0, 0)`）而不是叠加，杜绝正负相消；注释里"批处理 480p ⇒ 0.2083"已过期，同步改 0.1042 |
| 9 | B121_Q4B ★C2 仍走原 OutFromPlayingAreaAnim 那一半（`OnLightPlayableHand` 0 / `OnReturned` 1） | **δ** | 直接由 #8 连带：`ApplyOutFromPlayingArea` 有闸 `_travel >= TapThreshold`（`CardInteraction.cs:1550`），拖距 0.0849 不过闸 ⇒ `LightPlayableHand` 没派发 ⇒ lights=0 | 断言 `B121_Q4B_Assert.cs:367-369`；闸 `CardInteraction.cs:1550`；日志 `:1722631`；上一轮 lights=1 `battle.log:1722493` | 同 #8；建议这条也加一条"前提：`_travel >= TapThreshold`"的硬断言（现在只靠前一条失败来间接暴露） |

## §2 三个假设的检验结果

### H1（包 S 把 HUD 相机改成透视 ⇒ 命中/屏幕换算全偏）—— **不成立**

- `RunCore` 的相机来自 `BuildScene(...)`（`BattleScene.cs:635`），**不是**存档场景；包 S 的 `ApplyToScene` / `CheckSavedScene` 只在 `BuildAndSaveScene` 里调（`BattleScene.cs:244` / `:254`，全仓只此两处）。
- `BuildScene` 建完 HUD 相机后**紧接着**就 `LayoutSpace.Apply(cam)`（`BattleScene.cs:47801`），而 `Apply` 的第一句就是 `cam.orthographic = true` + `orthographicSize = 5`（`LayoutSpace.cs:75-83`）⇒ 批处理宿主跑的是**正交**。`LayoutSpace.cs` 本批也只改了一行注释（`git diff` 实证）。
- 战场相机本来就是透视（`BuildBoardCamera`，`BattleScene.cs:47728-47739`），本批只给它加了 `tag = "MainCamera"`。
- 所以 #3/#5/#6/#7/#8 的机制里没有一条经过相机/屏幕换算（它们走 `ButtonWorld` / `PointerWorldForTest` / `TriggerForTest` / `ToWorld`，且 `LayoutSpace.Cam` 一直是正交那台）。
- ⚠️ 但 `tag` 对调**确实改了一个客观事实**：`Camera.main` 从 HUD 相机变成 3D 透视相机（见 §4 ④）。

### H2（包 E 把伤害飘字改成无符号 ⇒ #4 的 `-1`/`-123` 样本不成立）—— **成立**

- `PopNumber.cs:236`：`heal ? ("+" + |amount|) : |amount|.ToString()`（原「`(heal?"+":"-") + |amount|`」）。断言拿 `PopDamage(…,1)` / `PopDamage(…,123)` 量宽度（`BattleScene.cs:23438-23447`），实际文本已变成 `1` / `123`。
- 数值自证：`123`（3 字符）在 55.52 px/em 下 ≈ 65 px < 框 80.9 px ⇒ 不缩（实得 55.52 = 标称值）；旧 `-123`（4 字符）≈ 84 px ⇒ 缩到 53.46（53.46/55.52 = 0.9629，84×0.963 ≈ 80.9 ✓）。
- 自适应框（80.9 px，`:23443`）与 0.35 下限（`PopNumber.cs:296-302`）**都没动**，所以是**样本失效**不是**实现坏了**。
- 同批把 `BattleScene.cs:23897` 那条改成"纯整数、无符号"（`git diff`）是对的，而且它**绿**（不在 9 条里）。

### H3（#1 / #2 本来就在、只是上一轮没覆盖）—— **不成立**

- `battle_b124b.log:661260`（11-03 13:48）与 `battle.log:660821`（11-03 19:21，= 11,793/0 那次）里，`R20②（如实记）` 与 `FP-HUD-003：选完打法那一刻` **都是 ✓**。
- 实证的引入点 = `81ea4fbb`（11-04 00:01）对 `BattleDriver.cs` 的 4 处改动；其中 `@@ -15321` 那一处正是 `u.LastAttackType` → `u.CurrentAttackType`（`git diff a88c1ebd 81ea4fbb` 在 **`BattleDriver` / `AttackSelector` / `BattleScene` / `CardFeel` / `PopNumber` / `LayoutSpace` 这 6 个文件**里只动了前两个、共 11 增 11 删；`CardInteraction.cs` 另由同一提交改 `TapThresholdPx`，见 #8）。

## §3 与上一轮（11,793 / 0）的差异归因

上一轮好读数 = `_tmp_view/battle.log`（11-03 19:21，`1727752` `BT === 合计：11793 通过 / 0 失败 ===`）。本轮 11855 / 9 ⇒ **总数 +71、通过 +62、失败 +9**。

- **总数 +71 的构成**（如实记，别再往下推）：本批新增的 `B139E` 套件在日志里有 **47 条结果行，全绿**（`grep -cE "^B139E +(✓|✗)"` = 47）；其余 **+24** 是**条数随状态变化**的既有循环/条件断言（同一份代码不同局面会跑不同条数），本报告**没有逐条去追**。⇒ **不要**把 +71 当成"多了 71 条断言"、更不要用它去反推失败数。
- **+9 失败** = 全部是旧断言/旧夹具，逐条只在 `81ea4fbb`（B130）与未提交的 B139 之间：
  - **6 条**（#1 #2 #3 #5 #6 #7）随 `_command` 起手改用 `CurrentAttackType` 一起翻红（同一行改动，见 §2 H3）；
  - **2 条**（#8 #9）随 `CardInteraction.TapThresholdPx` 10 → 5 翻红。⚠️ #9 在 13:48 那次（`battle_b124b.log:1722271`）**红过一次**（同一个 lights=0），19:21 之前被 `B124-W27` 的"横向补足"修绿（`battle.log:1722370` 0.2921 ✓）—— 本轮是**阈值减半把这个补丁又顶回去了**，属于"修过又破"的老账；
  - **1 条**（#4）是 B139 的飘字去符号（本批唯一由 B139 造成的红）。
- **不属于归因范围**：`ArenaBuilder.cs` / `ArtBaker.cs` / 大量 `.meta` / `Battle.unity`（`RunCore` 不读存档场景，见 §2 H1）；`WarpforgeVFX/Runtime/*` 与 `PopNumber` 的生命周期改造（本轮各 VFX/飘字断言全绿）。
- ⚠️ 计数口径提醒：`11855 + 9 = 11864`，比上一轮 `11793` 多 71 —— 但**失败数 9 ≠ 新增断言数**（9 条全能在 `battle.log` 里找到同名 ✓）。**判"是不是新断言"只看文案能否在上一轮日志里逐字找到**，⛔ 别拿合计差反推。

## §4 顺手发现

① **开窗是不是该跑 0.3 s `OutBack` 补间，未核**（与 #3 相关）：`Show()` → `RefreshPicked()` → `ApplyPickScale(hot)` 会给"已选那一格"起一条 `DOScale(1.3, 0.3s, OutBack)`（`AttackSelector.cs:1266` / `:1733`）。原版 `UnitOnBoardAttackTypeSelector__Toggle.c:152-153` 把 `+0x120` 交给 `HighlightSelectedAttackTypeButtons` 的那一拍**是不是也起补间**，本地静态判据看不出来（`scaleMultiplierWhenSelected` 的补间只在 `AttackButtonClick` 那一路查到）。若是"打开即就位"，那我们现在每次开窗会多一下 0.33→1.3 的弹跳（画面可见，且正是 #3 量到的东西）。→ 建议单开一条判据核查。

② **A462「指针刚进入那一格 ⇒ 安全窗重开」这条现在空转绿**：`Check(!sel4.HoverPickReady, …)`（`BattleScene.cs:34616-34620`）在 `Visible == false` 时**恒真**（`HoverPickReady` 第一项就要求 `Visible`）。本轮它用 `实得 safeLeft=0.000s` 通过，而上一轮是 `0.100s` —— 读数已经说明它没在测东西。→ 按 §三「灭自证」补一条硬前提（`Check(sel4.Visible, …)`）。

③ **"脏抬起沿"是这套夹具的系统性风险**：`PollInputEdgesForTest()` 造出来的 `_upEdge` 是"用掉就没了"的（`BattleDriver.cs:33051-33056`），而 `TickMultiCards` / `TickChatInput` 这类模态吞掉**不消费**它（`BattleDriver.cs:14521-14537` 已核）。只要被测路径的早退条件变化（本次是 `_command != None`），一次陈旧的抬手就会在几百行之后触发一次真实的"取消/结算"。→ 建议在 `BattleScene` 的 A462 段每次 `DrivePlayerTurnForTest()` 之前显式断言"这一帧没有待消费的沿"，或在夹具收尾统一 `ReleasedThisFrameForTest()` 清沿。

④ **`Camera.main` 换了一台（包 S 的 tag 对调）**：`BuildBoardCamera` 现在 `tag = "MainCamera"`（`BattleScene.cs:47739`），HUD 相机改成 `"UICamera"`（`:47797`）。生产里仍靠 `Camera.main` 兜底的地方：`TargetReticle.cs:519`（`hudCam` 为 null 时）、`TapParticleController.cs:213`、`DraggableController.cs:190`（`pressEventCamera` 为 null 时）、`BattlePostFx.cs:253`、`LayoutSpace.cs:157`。本轮这 9 条都不经它们（准星 `hudCam` 在 `BattleDriver.cs:32733` 显式赋值），**但它把"兜底相机的投影"从正交换成了透视** —— 一旦哪天 `hudCam`/`pressEventCamera` 缺值，换算会整体偏。→ 建议把这几处兜底改成"显式取 `LayoutSpace.Cam`"，并补一条断言钉住"`Camera.main` 不再是 HUD 相机"。

⑤ **文案/注释里的行号与量纲已过期**（坑 ④ 的实例，一次性列出）：断言 #2 的文案写 `BattleDriver.cs:15677`（现为注释行，真写点 `:15691`、早退 `:15646`）；`B121_Q4B_Assert.cs:348` 注释写"批处理 480p ⇒ 0.2083"（现为 **0.1042**）；断言 #4 的文案仍写 `-1` / `-123`（实际文本已无符号）。

⑥ **`#9` 的暴露方式偏弱**：`★C2`（`B121_Q4B_Assert.cs:367`）只报 `lights` / `returns` 两个数，没有把"因为拖距没过闸所以压根没调 `ApplyOutFromPlayingArea`"这个前置说清；本轮是 #8 那条前提先红才看得出因果。→ 建议 #9 自带一条 `_travel >= TapThreshold` 的硬前提。
