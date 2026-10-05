# H1 · A175（`CombatAutoZoom`）+ W4 两条断言（2026-10-12）

> ⚠️ **本文件由调度台代落盘**：写手 H1 本轮被系统规则禁止写 `.md`，正文是它交回的原文（内容一字未改，只去了包装）。

## 摘要（12 行）

1. **A175 做完**：新建 `Battle/CombatAutoZoom.cs`（459 行，8 个原版方法逐句复刻），挂在 `BattleDriver` 上，`Auto Zoom` 开关**真起作用**。
2. **判据齐到指令流级**：反编译器吃掉的 2 个操作数（`Evaluate` 实参 / 给 setter 的值）用 `工具/disasm_va.py … 0x18060ce40` **读指令流**拿回来了 —— 开关假 ⇒ 常量 `DAT_1834b2bb8` = **1.0**（= 不缩放），上夹取也是它。
3. 🔴 **新硬知识**：事件载荷是 **`max(左半, 右半)`**（不是总人数）—— `MinionManager__RefreshOccupationSlots.c` 是全反编译里 `OnMinionAddedOrRemoved` 的**唯一 Invoke 点**。
4. 🔴 **新硬知识**：`GameStaticData` 的真名是 **`useCombatAutoZoom`(+0x125)** / **`autoCombatChosenManually`(+0x12f)**；`autoZoom` 只是 `GraphicsTab` 上那个 toggle **组件**的字段名（旧文档全写成 `GameStaticData.autoZoom` ⇒ 已就地订正）。
5. 🔴 **新硬知识**：`CombatCameraZoom.Initialize` 会**反解**初始 zoom（拿 `CalculateFraming(0)/(1)` 的两个 sensorSize 反推 t）⇒ 开局不跳画面；另外 **`smallScreenUI`(+0x11c)** 会把 zoom 上界压到 ~0.305（16:9）—— 这是**真会生效的第二个状态**。
6. 取景那一跳（`lensShift.y`）**判据收成一处**：`Editor/BattleScene.cs` 的 `BoardFramer` 现在转调 `CombatAutoZoom.LensShiftY(aspect, zoom)`（原来只看 zoom = 1）。
7. **W4 两条断言全落地**（A387 按原文；A388 的 (a)/(b) 都补了原文没写的**前提**，否则是恒真）。
8. 类型检查 **运行时 0 / 编辑器 0**（中途两次红都**只在别人的在飞文件**上：`ScenarioBlendables.cs`、`RewardWindow.cs`，随后自愈；我的文件全程 0 条）。
9. 行尾**一处没翻**（`BattleDriver/BattleScene` CRLF、`SettingsWindow/SettingsScene` LF、新文件 LF）。
10. ⛔ 没跑 Unity 自检（简报口径）· ⛔ 没动 git · ⛔ 没改两张正本 · ⛔ 没越白名单。
11. ⏭ **新开一笔账**：`sensorSize.x`（`cameraSizeXTable`）那半**现在会真的生效**（老结论「zoom=1 时恒等 ⇒ 16:9 不生效」不再成立），见「没查清」。

---

## 一、结论

| 件 | 结论 |
|---|---|
| **A175** | ✅ **做了**。原版 `CombatAutoZoom`（TypeDefIndex 669）**8 个方法体逐句复刻**，`Auto Zoom` 从「值会存、点了不产」变成**真消费者的开关**；开关**当场读**（原版读静态字段）⇒ 战斗中途翻也即时生效。 |
| **W4 · A387** | ✅ 按原文落地（`BattleScene.cs:9392`），含「哨兵条不算主力」的如实标注。 |
| **W4 · A388** | ✅ (a)(b) 都落地，但**都补了原文没写的前提**（见 §三 的「照抄会恒真」两条）—— 这是本轮最值得看的一处改动。 |
| 顺带（铁律 5） | ✅ 三处旧字段名 `GameStaticData.autoZoom` / `autoZoomChosenManually` 就地订正（`SettingsWindow.cs:854` · `SettingsScene.cs:1113-1114` · `:1144/1146`）。 |

---

## 二、改动清单（文件:行号 = **本件改完之后的现读值**）

### 🆕 `Battle/CombatAutoZoom.cs`（新建，459 行，LF）

| # | 位置 | 为什么 |
|---|---|---|
| 1 | 文件头 `:1-56` | 判据链（8 个方法体 · 指令流 · 13 份序列化 · 调用时机 · 谁抬事件）+ **我们没做的 5 件（A~E）**如实列在这里 |
| 2 | `:64-80` | 4 个原版序列化字段逐条照抄带偏移（`+0x20/+0x24/+0x28/+0x30`）；⚠️ `+0x30` 原版是 `CombatCameraZoom` 组件（我们没有）⇒ 落成 `public Camera boardCamera` 并写明为什么是同一台相机 |
| 3 | `:86-103` | 原版私有状态（`currentEnemySize` `+0x38` / `currentPlayerSize` `+0x3c` / `targetZoomLevel` `+0x84` / `manualCamera` `+0x30`）；`targetZoomLevel` 出厂 = **1** 并写明「那是原版 `CombatCameraZoom.Initialize` 反解的结果」 |
| 4 | `:128-146` | `OnMinionAddedOrRemoved`（= 原版 `MinionManager.OnMinionAddedOrRemoved`，静态偏移 `0x0`）+ 抬的入口；注释里写清**载荷 = max(左半,右半)** 与「督军不在那两条 List 里」的推导 |
| 5 | `:174-198` | `Initialize()`（挂 `BattleHud.ResetCameraZoom`）+ **幂等守卫**（`Delegate.Combine` 不幂等，原版靠「一局一次」；我们 `Begin` 会被调几十次） |
| 6 | `:200-209` | `ResetForBattle()`（**我们加的**，原版靠「一局一份场景」等价）—— 走 **setter**，所以 `smallScreenUI` 那道夹取在开局也生效 |
| 7 | `:219-244` | `OnEnable/OnDisable` + `AttachMinionEvent/DetachMinionEvent`（**幂等**）—— 理由写在注释：批处理下 `AddComponent` 跑不跑 `OnEnable` 本工程**未定论**（A321 那条订正），不能赌生命周期 |
| 8 | `:251-289` | `OnMinionNumberChanged` / `SetZoomLevel`（逐句；含 `force` 清 `manualCamera`、`if (!manualCamera)` 那道闸） |
| 9 | `:291-319` | `TargetZoomLevel`（= 原版 `CombatCameraZoom.set_TargetZoomLevel` 的 `smallScreenUI` 夹取）+ `OffZoomLevel = 1f` |
| 10 | `:325-459` | 取景：`ApplyFraming()` + 两条原版曲线（`unitsZoomCurve` 5 键 · `maxZoomForSmallScreenDevicesByAspectRatio` 5 键）+ `LensShiftY(aspect, zoom)`（原 `BoardFramer` 的公式与三条曲线**搬进来**，出处注释一并搬） |

### `Battle/BattleDriver.cs`

| # | 行号 | 为什么 |
|---|---|---|
| 11 | `:55-70` | `_autoZoom` / `AutoZoom`（自检口）/ `_autoZoomSeen`（**初值 0,0**，与原版新场景零初始化一致 —— 否则换牌阶段就抬事件、开局取景会跳）+ 那段「原版组件在战场场景里，我们挂 driver 上」的落点论证 |
| 12 | `:2204` | `SetupAutoZoom();`（`Begin` 里、战场那一段之后、换牌那条 `return` 之前 —— `boardCam` 到这里才定） |
| 13 | `:2225` | 无换牌那条路也叫一次 `InitializeAutoZoom()`（原版两个调用点都是「真开打那一刻」） |
| 14 | `:2882` | `BeginBattleAfterSetup()` 里那次（= 原版 `BattleManager._FinishMulliganFinalPhase:232`） |
| 15 | `:5717-5786` | `SetupAutoZoom` / `InitializeAutoZoom` / `TickAutoZoom` 三个方法 + `RefreshAll()` 里那一句 `TickAutoZoom()`（**放这里而不是钉 9 个棋盘写入点**：写入点散在全工程，漏一个就是静默 —— 同 `Core/Aura.cs` 光环钩子的理由） |

### `Shell/SettingsWindow.cs`

| # | 行号 | 为什么 |
|---|---|---|
| 16 | `:854-858` | 字段名订正（`GameStaticData.useCombatAutoZoom` / `autoCombatChosenManually`，留更正痕迹） |
| 17 | `:867-889` | `ToggleAutoZoom`：**删掉那条 `LogWarning`**、改成真生效 —— 写值（照 `GraphicsTab.AutoZoomClick`）+ `FindFirstObjectByType<CombatAutoZoom>().ForceRefresh()`（= 原版 `BattleSettingsWindow__OnAutoZoomChanged` **同一个取法**，因为**我们战斗内那扇窗没有这一行**） |
| 18 | `:1793-1800` | `AutoZoom` 类注释：把「我们还没做那个组件」改成「消费者 = `Battle/CombatAutoZoom.cs`」 |
| 19 | `:1844-1852` | 🆕 `RestoreForTest(bool, bool)`（`ResetForTest` 只能放回出厂值，自检收尾还原不了玩家原值） |

### `Editor/BattleScene.cs`

| # | 行号 | 为什么 |
|---|---|---|
| 20 | `:215-234` | **自检开头**把 `Auto Zoom` 与 `Small Screen UI` 两格压成「关」（全程 `PersistOverride`、只改内存）—— 它们从 A175 起**真的会改战场相机取景**，不压就是「断言随玩家偏好变红变绿」。放在 `BuildScene` **之前** |
| 21 | `:10256-10278` | `BoardFramer`：公式/常量/三条曲线**搬进运行时** `CombatAutoZoom`，这里**转调**（Editor 程序集引不到运行时的那份 ⇒ 判据只能放运行时）。`Zoom = 1f` 保留（烘场景那份永远是不缩放档） |
| 22 | `:9384-9400` | **A387**（W4 原文）+ 落点订正（`deckNote` 不进对局记录、原版只到「回放是独立的一档」）+ 哨兵条与改坏法 |
| 23 | `:9478-9606` | **A175 断言一节**（7 组，见 §三） |
| 24 | `:9608-9637` | **A388(a)**（W4 原文 + 一段「只会看不会验」的如实声明） |
| 25 | `:9618-9642` | **A388(b)**（W4 原文 + **两处前提**，见 §三·B）+ `AutoZoom`/`SmallScreenUI` 真设置放回 |

### `Editor/SettingsScene.cs`

| # | 行号 | 为什么 |
|---|---|---|
| 26 | `:1110-1125` | 判据注释两处订正（字段名 + 「我们没做 `CombatAutoZoom`」→ ✅ 已做，断言在 `BattleScene.Run` 的 A175） |
| 27 | `:1144/1146` | 两条断言**文案**里的旧字段名订正（只有文案，判定没动） |

---

## 三、逐条断言（含「改坏会不会红」）

### A175（`BattleScene.cs:9478-9606`）—— 期望值**手算自原版曲线**，⛔ 不读被测实现

算式（写在代码里）：`minY = 249.9/1080 = 0.2313889`；`maxY(zoom) = 0.9104630 + PadMod(aspect)×PadByZoom(zoom)`；
`arg = (maxY+minY)/2`；`lensShift.y = ViewShift(arg)` ⇒ **zoom = 1 → −0.207943**、**zoom = 0 → −0.250551**（差 0.0426）。

| # | 断什么 | 🧨 改坏了会不会红 |
|---|---|---|
| ① | 曲线 = 原版那 5 个键（`Eval(0)=0 · (2)=0 · (3)=0.105257757 · (4)=1 · (9)=1`，期望值是**原版字面量**） | 改 keyframe/切线 ⇒ 红 |
| ② | （前提）宽高比在 [1.333, 1.77]（只有这一档 `PadMod ≡ 1`，上面两个数才成立） | 换靶子条件 ⇒ 红（本工程 13d 踩过同款坑） |
| ③ | 棋盘一变 ⇒ **真的收到人数事件**（`ZoomEventCount` 增长）；**关着 ⇒ zoom = 1** 且 `lensShift.y == −0.207943` | 把「开关假 ⇒ 1.0」那支删掉 ⇒ 红 |
| ④ | 开着 + 较忙那侧 1 人 ⇒ **zoom = 0** 且 `lensShift.y == −0.250551`（两档差 > 0.02 也一起断）；`FramingApplyCount` 增长 | 删 `ApplyFraming()`（只算不落）⇒ 红 |
| ⑤ | 开着 + 一侧 4 人 ⇒ **zoom = 1** 且取景**回到** −0.207943（两态真的翻得动） | 曲线映射接反 ⇒ 红 |
| ⑥ | `manualCamera` 为真时**人数事件不动取景**（先把棋盘改成「本该给 0」的样子 —— 否则这条是恒真） | 删 `if (!manualCamera)` ⇒ 红 |
| ⑦ | `ForceRefresh()`（force 那条路）**清掉手动档并重算** | 删 `manualCamera = false` ⇒ 红 |
| ⑧ | 收尾：开关放回「关」⇒ 取景回不缩放档 | —— |

⛔ **没造出来的那一半（如实）**：原版 `force` 那条路还会 `BattleHud.ToggleResetAutoCameraZoom(false)`（收起「重置镜头」钮 + `DOPunchScale`）—— **我们 HUD 没有那颗钮**，只留了钩子 `ToggleResetCameraUiAction` 并在第一次走到时出声一次。**不假称验过。**

### W4 · A387（`:9384-9400`）

- 主力条 = `driver.RawDeckNoteForTest == null` ⇒ 换回 `"联机局"`（或任何非 null）**必红**，与「录的那局带不带卡组」无关。
- 第 2 条（提示行不含那两句）= **哨兵、不是主力**，已按 W4 的原文如实标注「单独不构成判据」。

### W4 · A388 —— ⚠️ 原文两段**照抄会恒真**，各补了一处

- **(a)**：比 W4 原文多了两条「前提」（`hookedBefore >= 16` 与**账上点名的三条非 null**）—— 原文只有后半句，缺前提时「摘前 0 → 摘掉 0」也会绿。🧨 清单漏一条 ⇒ 第 3 条红。
- **(b)**：🔴 **两处必须补，否则恒真**：
  1. W4 说「放整轮自检的最后」—— 若真放在 `CheckSavedScene` **之后**，那一句里的 `EditorSceneManager.OpenScene` **已经把 driver 拆了**（`OnDestroy` 跑过、计数早就是 0）⇒ 恒真。⇒ 放在**它之前**并写明理由。
  2. (a) 已经把计数清成 0 ⇒ 直接 `DestroyImmediate` 就是「0 → 销毁 → 还是 0」。⇒ (b) 里**先 `driver.Begin(...)` 把钩子接回来**（`Begin` 里调 `HookAnimFxShake/HookAnimFxCards`，实读确认），并断一条 `hookedBeforeB > 0` 的前提。
- 两条**共通**的如实声明：把 `OnDestroy` 里那句 `DetachStaticHooks();` 删掉，(a) **不会红** ⇒ 那半**只能代码审查**（已写进注释）。

---

## 四、没查清 / 待判据（⛔ 不猜）

1. 🔴 **`sensorSize.x`（`CameraVerticalFramer.cameraSizeXTable` 那半）仍然没接** —— 而现在**它会真的生效**：`Editor/BattleScene.cs:10167` 那条老结论「zoom = 1 时该式恒等 ⇒ 16:9 下不生效、只有窄屏才要它」**从 A175 起不再成立**（自动缩放开着且某一侧 ≤2 人 ⇒ zoom 会走到 0）。⇒ **建议单开一笔账**（不是 A175 的残件，是 `CameraVerticalFramer` 那条线：要 `framer+0x38` 那条曲线 + `Math.Min(|viewport 高差|, framer+0x78)` 那一段 + `PlayerHand.CurrentSizeMultiplier`）。
2. **原版 `CombatCameraZoom` 整件没做**：手动缩放（滚轮/双指 `zoomSensitivity 0.1`/`0.005`）· 拖拽平移（`dragSensitivity 0.01`）· 世界边界 · `SmoothDamp` 平滑（`zoomSpeed 12.5`）· 回弹（`snapBackSpeed 5`）· `sensorSize` 重算。本件只复刻它**被自动缩放用到的那两格**（`manualCamera` / `TargetZoomLevel` 的 setter）。**我们直接落到目标值 = 无平滑**（批处理没有帧循环，且这是唯一可断言的形态）。
3. **HUD 那颗「重置自动镜头」钮没有**（原版 `BattleHud.resetCameraZoomButton` +0xa8，全仓 `ResetCamera` 0 命中）⇒ `Initialize()` 订阅的那条链**触发源缺**（只有 `ForceRefresh()` / 自检能走到）。钩子 `ToggleResetCameraZoomUi` / `RaiseResetCameraZoom()` 都留好了。
4. **战斗内设置面板（`SettingsPanel`）没有 Auto Zoom 那一行**（原版 `BattleSettingsWindow` 有，它才是「点了立刻重算」那条路）⇒ 把那一跳挪到菜单那颗开关上（**用原版同一个取法** `FindObjectOfType`），代码里写明这是「把对面那一跳挂过来」而不是另发明一条链路。**要不要在 `SettingsPanel` 里补那一行 = 待调度台拍。**
5. **真 Play 没跑**：①「离开战场场景 → 再进一次」那条真卸载路径（A388 的 b 半只覆盖了 `DestroyImmediate`）② 缩放的实际手感/画面。建议进 `真Play待验清单`（**那个文件不归我改**）。
6. **`useTestMode` / `testNumberOfUnits` 是死字段**：原版 8 个方法体里**一个都没读**（`+0x20/+0x24` 全反编译无读取点）⇒ 照抄字段与出厂值（4），**不发明用途**。

---

## 五、顺手发现（⛔ 只报不改）

1. 🔴 **`MinionManager.OnMinionAddedOrRemoved` 的载荷是 `max(左半, 右半)`，不是总人数**（`RefreshOccupationSlots.c` 逐句）。⇒ 想用这个事件做别的事时别按名字猜成「这一侧的总数」。
2. 🔴 **`CombatCameraZoom.Initialize` 会反解开局 zoom**（`CalculateFraming(framer,0)` / `(…,1)` 两个 sensorSize → `t = (现值−A)/(B−A)` 夹到 [0,1]，写 `targetZoomLevel` + `currentZoomLevel`）—— 这是「开局不跳画面」的做法；**且它与 `CombatAutoZoom.Initialize` 是同一行调用的**（`BattleManager._FinishMulliganFinalPhase:232` 附近两句挨着）。
3. 🔴 **`smallScreenUI`(+0x11c) 是自动缩放的第二层门**：`CombatCameraZoom.set_TargetZoomLevel` 里 `if (smallScreenUI) max = maxZoomForSmallScreenDevicesByAspectRatio.Evaluate(camera.aspect)`（13 场逐字节相同：1.333→0.574 · 1.6→0.535 · 1.77→**0.305**）⇒ **开着 Small Screen UI 时 zoom 永远到不了 1**。这条以前没在任何文档里出现过。
4. 🔴 **字段名**：`GameStaticData` 里叫 `useCombatAutoZoom`(+0x125) / `autoCombatChosenManually`(+0x12f)（`dump.cs` 逐条核过，该类 341 个字段）；`autoZoom` 只是 `GraphicsTab`/`BattleSettingsWindow` 上那个 **`EverguildToggle` 组件**的字段名。**全仓还有两处把组件名当成 `GameStaticData` 字段名的没改**（`资料/待办判据_1008.md:22` 与 `资料/普查产出_1007/*.md`，不是本件白名单里的正本）。
5. 🟡 **`Editor/SettingsScene.cs` 本轮有别的写手在动**：两次读之间涨了 **58 行**（2325 → 2383）。本件改动只有 3 处注释/文案（targeted edit，行尾 LF 没翻）。
6. 🟡 **类型检查中途两次红都只在别人的在飞文件上**（判据 = 诊断全部集中在那两个文件、本件文件 0 条）：`Battle/ScenarioBlendables.cs(2044,2045)` CS0246（缺 `using System.Collections.Generic;`）→ 自愈；`Shell/RewardWindow.cs(591,737)` CS0103（`TickAppearFx`/`ClearAppearFx` 找不到）→ 自愈。
7. 🟡 **新文件 `Battle/CombatAutoZoom.cs` 还没有 `.meta`**（Unity 下次导入时生成）。它只被 `AddComponent<T>()` 引用，**没有任何按 guid 的引用** ⇒ guid 什么时候定都不影响。
8. 🟡 **`资料/普查产出_1012/` 整个目录还是 untracked**（未进 git）。

---

## 六、跑过的检查

| 检查 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_h1 bash d:/4/Unity/工具/typecheck.sh`（4 次） | 最后一遍 **运行时 0 / 编辑器 0** ✅；中间两遍的红**只在别人的在飞文件**（§五·6） |
| 行尾（python **二进制**读） | `BattleDriver.cs` CRLF 9009 / bareLF 0 · `BattleScene.cs` CRLF 11082 / bareLF 0 · `SettingsWindow.cs` LF 2110 · `SettingsScene.cs` LF 2391 · 新文件 LF 459 —— **一处没翻** |
| `git diff --numstat` | `BattleDriver.cs` 466/73 · `BattleScene.cs` 619/42 · `SettingsScene.cs` 73/7 · `SettingsWindow.cs` 46/16（**含别人前几轮的改动**，本件自己那部分分不精确：改前没留副本、不许动 git）；新文件 `CombatAutoZoom.cs` 459 行（untracked） |
| Unity 自检 | ⛔ **一条都没跑**（简报口径：A 表清完再跑）。本件覆盖 **`BattleScene.Run`**（A175/A387/A388 全在它里面）+ **`SettingsScene.Run`**（只读了注释与文案、判定未动 ⇒ 逻辑上不受影响）⇒ 同步点跑**这两条** |
| git | ⛔ 没动（只读 `status` / `diff` / `numstat`） |
