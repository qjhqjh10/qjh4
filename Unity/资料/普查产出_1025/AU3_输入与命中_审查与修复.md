# AU3 · 战斗输入 / 选目标 / 命中手感 —— 审查与修复

> 代理 AU3（审查 + 修复）· 2026-10-25 · 用户口径：**先解决代码层面与原版的差异，⛔ 不用「需真机」当收尾理由**。
> ⛔ 未跑 Unity / 未动 git / 未改正本。只跑秒级类型检查（`TMPDIR=/tmp/wf_au3 工具/typecheck.sh` ⇒ 运行时 **0** / 编辑器 **0**，
> 改完共跑 **4 次**，每次都 0/0）。
> 行尾：`AttackSelector.cs` 仍是全 CRLF（1836/1836）、`TargetReticle.cs` / `ClickLog.cs` 仍是纯 LF（`bareLF/CRLF` 未翻），
> `git diff --numstat` = `275/66` · `110/37` · `4/1`。

**工程量**：审 **30 条**（逐条见 §二）· 改 **12 条**（§三）· 判据不足 **1 条**（§四）· 只能真机 **3 条**（§五）·
顺手发现（不归我，需别的线）**7 条**（§六）。

---

## 一、🔴 必须先改的一处断言（在别人文件里，我不改，但**不改就会红**）

`Editor/BattleScene.cs:8569`

```csharp
ctx.Players[0].Board[retAtk].LastAttackType = …;   // :8566
driver.SimulateOpenCommand(retAtk);                // :8568
Check(sel.ButtonScale(pickKind) > 1.2f, …);        // :8569  ← 会在 t=0 读到 1.0
```

**为什么**：原版那圈 1.3 倍是**补间**（`DOScale(原始scale × 1.3, 0.3 s)`，判据见 `AttackSelector.PickTweenSeconds`），
本轮已按判据把它从「瞬切」改成补间 ⇒ 开选择器那一拍 `localScale` 还在**起点 1.0**。

**改法（一行）**：在 `:8568` 与 `:8569` 之间插一句

```csharp
CardTween.Advance(0.35f);      // 原版那圈是 0.3 s 补间（`DAT_1834b2dc8` = 0.3）；⛔ 只泵补间、不推时间线
```

（同文件 `:8706` 已有同款用法；`CardTween.Mode` 在本宿主开头已置 `Manual`。）

**灭自证**：上面那句只让补间跑完，不改期望值；如果哪天有人把补间删回瞬切，这条仍然绿 ——
所以**同时**建议补一条「起点时刻」的断言（见 §七·断言 ②），它对「瞬切实现」是红的。

---

## 二、差异总表（逐条：判据 + 出处 | 我们改前 | 差异）

> 「出处」里的反编译 = `d:/2/tools/decomp_full/<类>__<方法>.c`；解包 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/…`；
> 浮点常量 = `d:/4/Unity/工具/read_literal.py` 现读 `d:/2/unity_run_ref/GameAssembly.dll`。

### 2·1 攻击选择器（三钮 / 准星 / 起手亮）

| # | 交互 | 原版判据 + 出处 | 我们（改前） | 差异 |
|---|---|---|---|---|
| 1 | 三颗钮恒显 | `AttackTypesButtonsController__ToggleAttackTypesSelector.c:47-96` 对 `attackTypeButtons`（恰 3 颗）逐个 `SetActive(true)`；只有技能那颗 `CardDisplayAttackTypeButton__Toggle(btn, card!=null && icon!=null)` 可能被关 | `EnsureAttackTypeInvariant` + `Option.Hidden` | ✅ 一致（`W10` 已落） |
| 2 | **高亮圈显隐** | 同文件**第一句** `SetActive(highlightImage(+0x38), param_2 == attackType(+0x20))`（`…HighlightIfNeeded.c` 同句，`…HighlightSelectedAttackTypeButtons.c:50` 同） | 三格**全亮**，非选的压到 **α0.28** | 🔴 **已修** |
| 3 | **高亮缩放** | `DOScale(原始scale × 1.3 / ×1, DAT_1834b2dc8=**0.3**)`；选中那格再 `SetEase(0x1b)` = **`Ease.OutBack`**（枚举实读 26 InBack / **27 OutBack**），回落那格不 `SetEase` ⇒ 走默认 `OutQuad` | **瞬切** `localScale = 1.3` | 🔴 **已修** |
| 4 | **ValueText 跟着缩放** | `ValueText` 是按钮的**子件** ⇒ 跟着父 transform 一起 ×1.3 | 只有图标 + 圈缩放，`Badge_*` 漏 | 🔴 **已修** |
| 5 | **悬停受体（并集）** | 钮本体 `Image`（`MB_4206/4121/4054`：`m_RaycastTarget=1` · **pad (0,0,0,0)** · `PA=0` · `Type=Simple`，节点 rect `RT_3368/2804/2626` = **118.372×118.806**）∪ 子件 `Highlight`（`MB_5267/4108/4478` 同 pad=0；`RT_3028/2776/3512` = **176.23×176.875**），且**圈只在已选那格 active** | 只把 `EventTrigger` 挂在**圈**上 | 🔴 **已修**（钮 + 圈都挂；圈关着时由钮本体吃射线 = 原版） |
| 6 | 底板也吃射线 | `MB_4492`（`Select Attack Background` 的 Image）`m_RaycastTarget=1`、无 sprite、**无处理器** | 已 `SetRaycastTarget(true)`、不挂处理器 | ✅ 一致 |
| 7 | 悬停即选中三格条件 | `CardDisplayAttackTypeButton__Update.c:39-68`：`inputOverButton(+0x92) && sendInput(+0x90) && !isInputOverSent(+0x91)`；安全窗 `disableTimeAfterPointerExit` = **0.1**（三份序列化同值） | `Hovered` / `TickHover` / `HoverPickReady` | ✅ 一致（`A462` 已落） |
| 8 | 技能钮选中音 | 只有技能那颗 `soundOnSelect` 非空（GUID `f83d6aab…`）；`DoPointerOverButtonActions` 里发指令那拍播 | `MarkHoverPicked` → `PlaySelectSound` | ✅ 一致（`W10`） |
| 9 | 技能钮 tooltip | `HighlightIfNeeded.c:50-70`：`param_2==4` 且 `+0xb0` 为空才弹、`SetParent(按钮)`、`localPosition = 设置向量 × +0x60`（技能那颗 = **−100**） | `RefreshAbilityTooltip`（落点 70 px = **我们挑的**） | ✅ 主体一致；落点如实标「我们挑的」 |
| 10 | **底板实绘宽** | `SelectAttackBackgroundController__ChangeSizeByElements.c`：目标 = 原始 0.44 × `1+(1.5−1)×clamp01(count−2)`（`DAT_1834b2bbc`=**2.0**、`MB_5200` 两字段都 **1.5**）；`…MoveButtons.c` 每帧 `lerp(目标×inputScaleModifier(1.5), 原始 0.44, t)` ⇒ **t=0**：2 颗 **514.8** / 3 颗 **772.2**；**t=1**：**343.2 × 125.4** | **常量 514.8**（= t=0 ∧ 2 颗那一帧，当成常态） | 🔴 **已修**（按颗数 × 入场进度算） |
| 11 | `ToggleJustOne` | `UnitOnBoardAttackTypeSelector__ToggleJustOne.c`：三颗全关、只 `Toggle` 那一颗；触发面 `…Toggle.c:200-240`（`displaySummonSickness` ∧ (`ferocity` ∨ `oath`+`CanUseOathAbility`)） | `ShowJustOne`，**但造的 Option 不带 `Army`** ⇒ 技能钮拿不到「按军队查的图标」 | ⚠️ **本侧已补 `army` 形参**；调用点（`BattleDriver.cs:13654`）**没传** ⇒ 见 §六·1 |
| 12 | `pindown` ⇒ 近战那颗指令被吞 | `…AttackButtonClick.c:27-33`：`HasCurrentTrait(0x3ca) && param_2==1 ⇒ return`（**钮照样亮、不置灰**） | `LockMelee` → `Selectable` | ✅ 一致；`BattleDriver.cs:13664` **已接**（原 doc 说「待接」= 过期，已就地订正） |
| 13 | 起手就亮目标 + 准星 | `BattleManager__StartAttackFrom.c:20-40`：`selector.Toggle(true, card)` → `ToggleCrosshair(true, false)` → `ShowPotentialTargets`，**玩家还没选打法** | `PreviewTargets`（挂点 `BattleDriver.cs:25542`） | ✅ 一致（`W20`） |
| 14 | 已选打法什么时候写 | `AttackButtonClick.c:75-85`：`CardScript__ChangeAttackType(card, param_2, 1)` **在选定时**写 `+0x120`，再 `HighlightSelectedAttackTypeButtons` + `SetAttackType` | 我们只在**结算时**（`RuleCore.cs:5834/10157/10345/10556`）写 `UnitState.LastAttackType` | 🔴 **仍有差异** ⇒ §六·2（要动 `BattleDriver`） |
| 15 | 换打法时准星也换色 | `…AttackButtonClick.c` 收尾 `TargetReticleController__SetAttackType(card.attackType)` | 我们换档只换了**弧线**颜色（`Aim` 里现算 preset），准星本体 `CrossColor` 只在 `Show` 里写 | 🔴 **已修**（见 2·2 #16） |

### 2·2 准星 / 弧线（`TargetReticle`）

| # | 项 | 原版判据 + 出处 | 我们（改前） | 差异 |
|---|---|---|---|---|
| 16 | **颜色表只有 1/2/3/4** | `TargetReticleController__SetAttackType.c`：`for preset: if (preset.attackType(+0x10) == param_2) 写色`；`MB_5098.json` 实读 **4 条**（1/2/3/4，3 与 4 逐值相同）⇒ **0 一条都没有（不改色）** | `default: return Melee` ⇒ 起手（`_command=None`）**整颗准星变红** | 🔴 **已修**（`TryPresetOf` 返回 false；初值 = 序列化白） |
| 17 | 初值 | 准星 `SpriteRenderer_1754.m_Color=(1,1,1,1)`；弧线 `LineRenderer_1609.colorGradient` 前两色键 = 白 | `Color` 默认 (0,0,0,0) ⇒ 0 档会**全透明** | 🔴 **已修**（两条初值 = 白） |
| 18 | **弧线 alpha 方向** | `TargetReticleController__Update.c:39-68`：准星 `a += iVar7·dt·8`、弧线 `a += iVar10·dt·8`，而 `iVar7` / `iVar10` **同一个守卫**（`if (+0xa8 == 0) → −1`）⇒ **同向** | `1 − fadeAlpha`（**反向**）⇒ 稳定态弧线 alpha = **0**，整条弧线**始终看不见** | 🔴 **已修** |
| 19 | **淡入/淡出谁在泵** | 原版 `Update` 每帧一次；本工程约定 = `BattleDriver.AdvanceTimeline` 一个泵（同 `CameraIntro` / `BattleMusic`，它们都**没有** `Update`） | `TargetReticle.Update()` **和** `AdvanceTimeline` 都在调 `TickFade` ⇒ 真机 **2 倍速**（0.125 s → 0.0625 s） | 🔴 **已修**（删 `Update()`；测试看不出来因为批处理不跑 `Update`） |
| 20 | punch 时机 | `TargetReticleController__PrepareMovement.c` 收尾 = `SetPoints → **SetAttackType**` ⇒ **每次开始选目标都 punch**；`…AttackButtonClick` 换档再 punch | 只在「换档」时 punch（`_lastPunchKind` 闸），**重新开一次不弹** | 🔴 **已修**（`Show` 每次 punch；`Aim`/`AnimTo` 换档 punch） |
| 21 | punch 的守卫 | `…SetAttackType.c`：`IsTweening(transform) ⇒ DORestart(tween,true)`，否则先 `set_localScale(原始)` 再 `DOPunchScale(原始×0.2, 0.5, 0, 1)` | 恒「先归位再 punch」 | 🔴 **已修**（加 `DORestart` 支） |
| 22 | `immediate` 那一档 | `…ToggleCrosshair.c`：`param_3 != 0` 时 `SetActive(show)` + **准星与弧线 alpha 都写 `show`** | 隐藏时准星 0、弧线 **1**（反向的残留） | 🔴 **已修**（同向 ⇒ 都是 0） |
| 23 | 双平面求交 | `…UpdateTrail.c`：`Plane(+0x70 = up @floorReference)` 与 `Plane(+0x90)` 各打一次、**取近**；`Initialize.c:78/105/128` 三个平面 = `up@+0x40` / `forward@playerMinionManager(+0xE0)` / `back@enemyMinionManager(+0xE8)`（`dump.cs` 的 `BattleManager` 字段现核：**0xE0 = playerMinionManager · 0xE8 = enemyMinionManager**）⇒ `+0x90` 就是**敌方行**平面 | `Plane(up, y=floorY)` + `Plane(back, z=enemyRowZ)` 取近 | ✅ 一致（本轮把 `+0x90` 到底是谁**核实到字段级**，原先只是推断） |
| 24 | 弧线采样 | `CrosshairLineEffect__SetPoints.c`：`i = 0..curvePoints(10)` ⇒ **11 点**、`t = i/10`、`y += curve(t) × peak`、`peak = lerp(minH 0.2, maxH 2.0, clamp01(dist/8.0))` | 11 点 / t=i/10 / 同 lerp | ✅ 一致 |
| 25 | 曲线选择 | `CrosshairLineEffect__SetAttackType.c`：`==2 ⇒ range`，其余（含 0）⇒ melee（且对 0 会 `LogWarning`） | `kind==Ranged ? Range : Melee` | ✅ 一致（我们**不打**那条 warning：0 档是正常态，逐帧刷会淹告警 —— 已写进 doc） |
| 26 | 淡入速率 / 结束 | `colorChangeSpeed=8.0`（`MB_5098`）⇒ 0→1 用 0.125 s；到 1（或 0）就 `enabled=false`，淡出到底再 `ToggleCrosshair(false, immediate)` | `TickFade` 同 | ✅ 一致（我们夹 [0,1]，终值相同） |

### 2·3 拖拽（阈值 / 回弹 / 取消）

| # | 项 | 原版判据 + 出处 | 我们（改前） | 差异 |
|---|---|---|---|---|
| 27 | **弹出阈值** | 真入口 = `CardScript__OnTouchDrag.c:46` → `TryDraggingFromBoard.c`：第一闸 = `abs(pointer.position(+0x144) − 本次位置)² ≤ DAT_1834b2bb0`，该常量**实读 0.2** ⇒ 单位是**屏幕 px²** ⇒ 只是 **≈0.45 px 的去抖死区**；真门 = uGUI `IDragHandler` 要过 `EventSystem.pixelDragThreshold`（Unity 默认 **10 px**） | `AttackSelector.DragThresholdWorld`（= 0.085 屏高 ≈ **92 px**）当弹出阈值 | 🔴 **量级差 9 倍**。判据已闭、常量已加（`PopDragThresholdPx = 10`）；**改点在 `BattleDriver.cs:13464`** ⇒ §六·3 |
| 28 | 入场动画的驱动量 | `AttackTypesButtonsController__MoveButtons.c`：`accumulatedDrag(+0x50) += TouchDragDeltaViewport`，`t = clamp01(−acc.y / 0.085)` | `PushDrag` 同 | ✅ 一致（`DragThreshold01` 在**这一处**是**正确**用途，⛔ 别一起改掉） |
| 29 | 「点槽外取消」 | 原版**没有**等价物（`A462` §四·3 已记） | `ContainsBar` | ⚠️ 自造件；仍挂在 `BattleDriver` 那条链上（`T09` A4 一起收） |
| 30 | 卡组编辑拖拽 | `DraggableController<T>` 五个方法（VA 反汇编，`DraggableController.cs` 文件头逐条）：`SetDraggable` / `OnBeginDrag`（预览 SetActive + `Play2D(dragSound)`）/ `OnDrag`（`ScreenPointToLocalPointInRectangle` + `localPosition`）/ `OnEndDrag`（预览关 + `eventData.hovered` 上每个 `IDropHandler<T>` 投一次） | 逐句照抄（三处有意偏离：cue 名代替 AudioCue / 世界坐标代替 RectTransformUtility / 多一个 `Bind`） | ✅ 一致（偏离已写明判据） |

### 2·4 目标高亮 / 合法目标点亮

| # | 项 | 原版判据 + 出处 | 我们 | 差异 |
|---|---|---|---|---|
| 31 | 状态色只点「环」 | `CardHighlight__ChangeState.c` / `ChangeFrameColor.c`：只写 `FrameHighlight` / `FrameHighlightRemnant` 两个 SpriteRenderer 的 color（+0x70/+0x78），**不碰材质/非环图层** | `FrameColorOf` → `_rim`；`ColorOf` 对棋盘两态返回白 | ✅ 一致（2026-10-24 已改判） |
| 32 | 6 档补间时长 | 同上：`selected`(2) 与 `displayingActiveAbility`(5) 走 **0**，其余 `CardHighlightAnimTime = 0.1` | `TweenTimeOf` | ✅ 一致（我们没有 state 5） |
| 33 | 缩放 | 只有 case 0/3/4 调 `ChangeTargetScale`（×1.05） | `ScaleOf`（`Selected` = 1） | ✅ 一致 |
| 34 | `HighlightAttackTypes` | `CardHighlight__HighlightAttackTypes.c`：对 `+0x48` 列表逐个 `HighlightAttackType.HighlightIfNeeded(param_2)` —— **另一族组件**（不是本文件这五色） | 未实现这一族（`HighlightAttackType` 是卡上「攻击类型图标」高亮，归卡面线） | ⏳ 见 §六·4（跨线，不在本白名单） |

---

## 三、已修清单（文件:行号 = 改后行号）

### 3·1 `Battle/AttackSelector.cs`（+275/−66）

| # | 行 | 改了什么 | 判据 |
|---|---|---|---|
| 1 | `137` | 新增 `PickTweenSeconds = 0.3f` | `DAT_1834b2dc8` = 0.3 |
| 2 | `155-190` | 底板常量拆成 `BgBaseW`(343.2) / `BgH` / `InputScaleModifier`(1.5) / `ElementsHorizontalScaleModifier`(1.5)；删掉把 514.8 当常量的 `BgW` | `ChangeSizeByElements.c` + `MoveButtons.c` + `MB_5200` |
| 3 | `196-198` · `203` · `828` · `1309` | **引错的节点 id 就地订正**：`RT_3254`→`RT_3061`（底板）、`RT_3166`→`RT_2973`（条子本体） | `RectTransform_{3061,2973,3254,3166}.json` 现读 |
| 4 | `221-247` | `DragThreshold01` 的 doc 改写 + 新增 `PopDragThresholdPx = 10` / `PopDragThresholdWorld` | `TryDraggingFromBoard.c` + `DAT_1834b2bb0`=0.2 + uGUI 默认 10 px |
| 5 | `278-281` | `BgSizePx` 世界档改读 `WorldW`（宽不再靠 `localScale.x`） | 同 #2 |
| 6 | `349` | 世界档命中新增 `HitRect`（矩形） | 受体是矩形（见 #8） |
| 7 | `675` · `894-908` · `761` | 新增 `_iconTriggers`；**钮本体也挂 `EventTrigger`**（圈关着时靠它） | `MB_4206/4121/4054` raycastTarget=1 |
| 8 | `1545-1578` | `UpdatePointer` 世界档：**圆 → 两矩形（钮 ∪ 已选那格的圈）** | 同 #7 + `RT_3028/2776/3512` |
| 9 | `1335-1367` | `RefreshLayout`：底板宽 = `BgBaseW × lerp(targetMul×1.5, 1, enterT)`，颗数走 `ShownKinds()`；`SetAspect` 是唯一定宽口 | #2 |
| 10 | `1435-1460` | 新增 `ShownKinds()`（背景宽与排布共用一份）+ `IsPickedSlot()`（圈显隐唯一判据） | 2·1 #2 |
| 11 | `1462-1470` | `SetSlotActive`：圈改为**只在已选那格亮** | 同上 |
| 12 | `1580-1660` | `RefreshPicked` 重写：圈显隐 + **0.3 s 补间**（`Ease.OutBack` / 默认 `OutQuad`）+ `Badge_*` 也一起缩放；新增 `ApplyPickScale` 三个重载 / `ResetPickScales` | 2·1 #2/#3/#4 |
| 13 | `1106-1112` | `Hide()`：清 `_pickedApplied` + 缩放直接归位 | 同上（避免下一趟闪一下 1.3） |
| 14 | `1202` | `ShowJustOne(..., int army = 0)` | `UnitOnBoardAttackTypeSelector__Toggle.c`（按军队查图标） |
| 15 | `1800+` | 自检探针：`IconTriggerForTest(i)` / `HighlightActiveForTest(i)` | 供 §七 断言 |
| 16 | `1025` · `1043` · `1195` · `1226` | 四条**过期 doc 就地订正**（`LockMelee` / `PreviewTargets` / `ShowJustOne` 的「调用点待接」都已接上；`SetSelected` 仍无调用点） | 现核 `BattleDriver.cs:13654/13664/25542` |

### 3·2 `Battle/TargetReticle.cs`（+110/−37）

| # | 行 | 改了什么 | 判据 |
|---|---|---|---|
| 17 | `181` | `ApplyFade`：弧线 alpha `1−fade` → **`fade`**（同向） | 2·2 #18 |
| 18 | `137-160` · `166` | 删掉 `void Update()`（**唯一泵** = `AdvanceTimeline`）；doc 改成同向 + 记清这个 2 倍速坑 | 2·2 #19 |
| 19 | `235-247` | `PresetOf` → `TryPresetOf`（0 档**不写色**） | 2·2 #16 |
| 20 | `293-294` | `CrossColor` / `TrailColor` 初值 = **白**（原版序列化值） | 2·2 #17 |
| 21 | `637-672` | 新增 `ApplyAttackType`（色 + punch）/ `Punch`（`IsTweening ⇒ DORestart`）；`Show` 每次 punch、`Aim`/`AnimTo` 换档 punch；删 `PunchIfKindChanged` | 2·2 #20/#21 |
| 22 | `698-724` | `UpdateArc(from,to,kind)` 只吃 `kind`（曲线），颜色读当前 `TrailColor` | 2·1 #15 |
| 23 | `684` | 新增只读探针 `LineColorAlpha`（给 §七 断言 ④ 用） | 2·2 #18 |
| 24 | 文件头 `15-19` | 类名订正：`AttackTargetReticleController` → **`TargetReticleController`**（全量反编译里没有前者） | `MB_5098.m_Script` |

### 3·3 `Core/ClickLog.cs`（+4/−1）

| # | 行 | 改了什么 |
|---|---|---|
| 25 | `196-199` | `DryRun()` 的注释与代码不符（写着「干跑」，实际只回 `LastBlock`；全仓 0 调用点）⇒ 就地订正注释，不动结构 |

---

## 四、判据不足（1 条，⛔ 没猜）

1. **原版 `EventSystem` 的 `pixelDragThreshold` 序列化值** —— `level0` / `bundle_scenes_scenes_battlearena1` /
   `bundle_menus_assets_all` 三个包搜 `m_DragThreshold` **0 命中**（EventSystem 可能在别的常驻包，或吃 Unity 默认）。
   ⇒ 手感门按 **Unity 默认 10 px** 记（已写进 `PopDragThresholdPx` 的 doc），并如实标「未在实机上量过」。
   **还差**：整包（24.7 万文件）搜一次 `m_DragThreshold`，或用一次真机把「拖多少像素弹出选择器」量出来。

---

## 五、只能真机（3 条）

1. **悬停区从「圈」变成「钮 ∪ 已选那格的圈」之后的手感** —— 静态上等价于原版的射线并集，但「未选中两格的命中区变小
   （176.2² → 118.4²）」这一点只能靠手上感觉确认（会不会觉得要瞄得更准）。
2. **0.3 s `OutBack` 的观感** —— 曲线形状（先冲过去再回来）只能肉眼看；断言只断「起点 1.0 / 终点 1.3」。
3. **底板宽的补间（t=0 → t=1 由 514.8 收到 343.2）** —— 数值有判据，但「看起来是不是原版那条白带」要真机；
   ⚠️ 这条**同时**是我最没把握的一条：`ChangeSizeByInput` 那个 `+0x20`（`inputScaleModifier`）乘在**视觉 scale** 上
   看着别扭（更像是给输入区用的倍率）。判据链闭合（`Awake` 存 0.44 → `ChangeSizeByElements` 写目标 → `MoveButtons`
   逐帧 lerp），但**没有第二条独立来源**；若真机上「归位后底板比按钮窄」看着不对，先回来复核这一条。

---

## 六、顺手发现（都不归我 / 需要别的线，⛔ 我没越界改）

1. **`BattleDriver.cs:13654` `ShowJustOne(...)` 没传 `army`** —— 本侧已加 `army` 形参（默认 0）。
   改法一行：`selector.ShowJustOne(AttackKind.Ability, atWorld, u.LastAttackType, u.Card != null ? ArmyIcons.Id(u.Card.Faction) : 0)`。
   （`Show` 那条路已经这么传了，见 `:13692`。）
2. **`LastAttackType` 时序**（U3 §七·4 的老账，仍在）：原版 `AttackButtonClick` 在**选定时**写 `card.attackType`；
   我们只在**结算时**写（`RuleCore.cs:5834/10157/10345/10556`）⇒ 同一回合内改选再开选择器，高亮的还是上次真打出去那一档。
   落点 = hover 即选中那一拍：`u.LastAttackType = (int)kind; selector.SetSelected(kind);`（`SetSelected` 已就位）。
3. **弹出阈值要改用 `AttackSelector.PopDragThresholdWorld`**：`BattleDriver.cs:13464`
   `bool dragged = (world - _pressWorld).magnitude > AttackSelector.DragThresholdWorld;`
   → `> AttackSelector.PopDragThresholdWorld`（10 px）。⚠️ `BattleScene.cs:8135-8138` 那条断言的**文案**也要一起改
   （它现在把 0.085 说成「才弹出选择器」；数值断言 `DragThreshold01 == 0.085` 本身仍然成立 = 动画分母）。
4. **`CardHighlight__HighlightAttackTypes` 这一族没复刻**（卡上「攻击类型图标」的 `HighlightAttackType` 组件）：
   属于卡面/战斗视图线，不在本白名单。判据已读：对 `+0x48` 列表逐个 `HighlightIfNeeded(param_2)`。
5. **选择器的自造标题 / 副标题**（`_title = CardText.Phrase("CHOOSE ACTION")`、`_note = 卡名+技能正文`）：
   原版 `Drag Attack Selector` 子树里**只有** `Select Attack Background` + 三颗钮（+ 技能钮的 `ValueText`），
   **没有任何标题/正文**（`runtime_ui_dump_Battle_Arena_1.tsv:359-367` 逐行核过；技能正文原版走**技能钮 tooltip**）。
   ⇒ 我们这两行是自加的（`A1024③` 当年就标了「判不出」）。建议：删 `_note`（与 tooltip 重复）、`_title` 待主对话拍板。
   改点在 `BattleDriver.cs:13697-13699`（传参）+ 本文件 `BuildOnCanvas/BuildInWorld` 里那两件。
6. **`TargetReticle` 的两处「父链必须是单位阵」假设**：`_cross.transform.localPosition = aim`（aim 是世界点）与
   `_line.SetPosition(i, transform.TransformPoint(pt))`（pt 已是世界点）。两者在「根在原点、无缩放」时**同时成立且互相自洽**，
   但根一旦被搬走/缩放，两条会一起偏（现在 `BattleScene.cs:43213` 建在原点）。判据 = 原版那两件**本来就在不同帧**
   （准星 HUD 平面 / 弧线棋盘 3D），我们目前靠「根 = 单位阵」把它们压在同一帧。**没改**（改它会动 `:8602-8614` 两条断言）。
7. **`IMGUI/`**：无。`BattleEventSystem` 本身**没查出差异**（New-only 必须 `InputSystemUIInputModule`、只允许 1 只、编辑模式补注册 —— 三条都有判据与断言）。

---

## 七、建议补的断言（写清「断什么 / 期望值 / 怎么灭自证」；宿主 `Editor/BattleScene.cs` 不归我）

放在现有「攻击选择器」那一段（`:8440-8580`）附近：

1. **`★ AU3-1：圈只在已选那一格亮`**
   - 断：开选择器时 `LastAttackType = 0` ⇒ `foreach opt: !sel.HighlightActiveForTest(slotOf(opt))`；
     再给 `LastAttackType = <有攻击力的那一档>` 重开 ⇒ 只有那一格 `HighlightActiveForTest == true`，另两格 `false`。
   - 灭自证：① 判据是**两读**（0 档全灭 / 有档只亮一格），改回「全亮」⇒ 第一条红；改成「全灭」⇒ 第二条红。
2. **`★ AU3-2：0.3 s 补间（不是瞬切）`**
   - 断：给 `LastAttackType=pickKind` 重开 ⇒ **立刻**读 `ButtonScale(pickKind) < 1.05`；
     `CardTween.Advance(0.35f)` 后读 `ButtonScale(pickKind) ≈ 1.3`（±0.02）。再 `CardTween.Advance(0)` 不动。
   - 灭自证：瞬切实现在两次读上都给 1.3 ⇒ 第一条红；把补间时长改成 3 s ⇒ 第二条红。
3. **`★ AU3-3：底板宽 = f(颗数, 入场进度)`**
   - 断（2 颗钮的用例，比如 `Veteran`）：`Show` 后立刻 `BgSizePx.x ≈ 514.8`（±0.5；`t=0`）；
     `TickForTest(0.02f, (0,−DragThreshold01))` 推到 `t=1` 后 `≈ 343.2`（±0.5）。
     （可选第二条：技能图标可用时 = 3 颗 ⇒ 同一组读 `772.2 / 514.8`。）
   - 灭自证：常量 514.8 的实现 ⇒ 第二读红；只按颗数不按进度的实现 ⇒ 第一读红（2 颗时它是 343.2）。
4. **`★ AU3-4：弧线材质 alpha 与准星同向`**（新探针 `reticle.LineColorAlpha`）
   - 断：准星淡入推完（`FadeAlpha ≥ .999`）后 `LineColorAlpha ≈ 1`（±0.02）；`Hide()` + `Step(0.2)` 之后 `≈ 0`。
   - 灭自证：改回 `1−fade` ⇒ 第一读 ≈ 0（红）；把弧线 alpha 恒写 1 ⇒ 第二读红。
5. **`★ AU3-5：attackType 0 不改色`**
   - 断：`LastAttackType = 0` 开选择器并起拖（此时 `_command == None`）⇒ `reticle.CrossColor` 三分量都 **= 1**（白）；
     选近战之后 ⇒ 红（`r>0.8 && g<0.2`）。⚠️ 现有 `:8616` 只断「选近战之后是红」，对「0 档变红」无感。
   - 灭自证：`default: return Melee` ⇒ 第一条红。
6. **`★ AU3-6：弹出阈值 10 px`**（等 §六·3 改完之后）
   - 断：`AttackSelector.PopDragThresholdWorld` ≈ `10f/108`（±1e-4）；`BattleDriver` 的 `dragged` 用 9 px 位移时**不**弹、
     用 11 px 时**弹**（`SimulatePointerAt` 两档）。
   - 灭自证：改回 0.085 屏高 ⇒ 9 px 那档也弹 ⇒ 红。
7. **把现有 5 处「悬停驱动」的扳机换成 `IconTriggerForTest`（或两件都断）** —— 它们现在打的是
   `HighlightTriggerForTest(i)`（= **圈**上的 `EventTrigger`）：`BattleScene.cs:7928` · `:7991` · `:8111` ·
   `:30869` · `:34553`。本轮起**圈只在「已选打法」那一格才 active** ⇒ 生产里「未选中那两格」的悬停走的是**钮本体**，
   而这几处直接调圈上的回调 ⇒ **测的是一条生产里到不了的路**（测试仍会绿）。
   改法：把 `HighlightTriggerForTest(i)` 换成 `IconTriggerForTest(i)`（新增的只读口），
   或在每格上**两条都断**（进入/离开各一次）。灭自证：只留圈那一条时，把 `_iconTriggers` 的挂载删掉 ⇒ 整组**不会红**（这就是为什么要换）。
