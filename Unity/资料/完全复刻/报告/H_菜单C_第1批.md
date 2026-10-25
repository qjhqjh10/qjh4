# H · 菜单域 C 第 1 批（2026-10-25）

> 代理：H（菜单/外壳域）　范围：`账/菜单_外壳.tsv` 的 C 行，按 P1 → P2。
> 🔴 **实际数是 136 条 C、P1 = 0**（不是 182：R3 那两轮已把 59 行 C→A/B，之后又交了 17 行）。
> ⛔ 活动线（`Daily*` / `LiveOps*` / `Mission*` / `Inbox*` / `Reward*` / `Gacha*` / `Ranked*` / `Raid*` /
> `Skirmish*` / `EnergySinglePlayerOnlyEventWindow` / `ExpansionPassEventWindow`）**只记账、未动工**。

---

## §一 本轮做完的（5 件实功能 + 1 个虚方法口）

| # | 原版 | 判据 | 落点 | 账 |
|---|---|---|---|---|
| 1 | `UIResourceCounter.SetQuantity(quantity, animated=1)` —— **货币数字 0.5 s 滚动** | `UIResourceCounter__SetQuantity.c` animated 支：`DOTween.Kill` → 浮点从当前值推到目标 → `OnUpdate` 里 **`ceil`** 写文本（`…b__0.c:16` / `b__1.c`）→ `OnComplete` 定到终值（`…b__2.c:8-10`）；0.5 s（`报告/D_未分类_第1轮.md:105`） | `Shell/TopBar.cs`：`CounterRef{Value,Target,From,T,Animating}` + `StepCounters()` + `CounterAnimTime=0.5`；`MainMenuRuntime.Update` 每帧调；批处理/不在屏上⇒直落 | `ResourcesBarController.OnCurrencyChange` 更新备注 |
| 2 | `ResourcesBarController.ToggleOverlay(bool)` —— 卡片详情窗打开时资源条**抬到它上面** | `ResourcesBarController__ToggleOverlay.c`（Canvas `overrideSorting=1` + 同 layer + `order+1`）；调用点 = `CardDisplayWindow.ShowCard/Close/ToBackground/ToFocus` + 两个暂停活动线 | `Shell/TopBar.cs:ToggleOverlay`（无 Canvas ⇒ **渲染队列整体 +134**：2986→3120，高于详情窗最高 3118；逐件记旧值、关掉逐件还原）+ `Shell/CardDetailPopup.cs` 的 ShowCard/**Open**/Close/Hide/ToFocus/ToBackground（`Open` 是为「重开不经过 ShowCard」那条路补的等价跳） | **A** |
| 3 | `EverguildButton.OnTransformChildrenChanged()` —— 子树变了**立即重收**图形件 | `EverguildButton__OnTransformChildrenChanged.c` = `GetGraphicsInChildren(gameObject, ref list, …)` | `Shell/WindowButton.cs`：重收并**保留已有件基色**（新件以当前色为基色；否则会把悬停色当基色乘错） | **A** |
| 4 | `WindowsManager.GetOpenWindow<T>() / CloseWindow<T>() / RefreshWindow<T>()` | `WindowsManager__GetOpenWindow_object_.c`（`openWindows` 上 `FirstOrDefault(is T)`）· `__CloseWindow_object_.c` · `__RefreshWindow_object_.c`（**只认 currentWindow**，`is T` ⇒ 虚表 +0x1a8 回前台） | `Shell/WindowsManager.cs` 三个泛型帮手；`RefreshWindow` 的「回前台」用 `TryOpen()` 的 Background 支（同净效果） | **B**（已建、暂无生产调用点） |
| 5 | `MainMenuWindow.ToggleScroll(bool)` —— 教程期间关掉主菜单滚动 | `MainMenuWindow__ToggleScroll.c` = `scrollList.enabled = option`；真实调用点 = `TutorialStagePlayRanked`（ShowCursor / CheckForCompletion） | `Shell/MainMenuRuntime.cs:ToggleScroll` + `MenuScroll.Enabled` + `PointerLayer` 的 **命中 / 滚轮 / 惯性**三处 `if (!s.Enabled) continue` | **B**（已建；教程线未接） |
| 6 | `MainMenuWindow.ToFocus()` —— 回前台：`SetActive(true)` + 左栏放回 + 关烘焙 + 档案 Initialize + 活动容器 | `MainMenuWindow__ToFocus.c` | `Shell/MainMenuWindow.cs:ToFocus`（`SetActive(true)` + `MainMenuRuntime.SetNavPanelVisible(true, true)`；档案无单独口、活动线暂停 —— 已注明） | **A** |
| 7 | `GameWindow.ToFocus()` —— 原版虚表 `+0x1a8`，**`TryOpen` 的 Background 支会调它** | `GameWindow__TryOpen` Background 支 = `state=Open` + `call [rax+0x1a8]` | `Shell/GameWindow.cs`：新增 `public virtual void ToFocus()` + Background 支调用；`GameWindowWithTabs.ToFocus` 改 **override** | **A**（三行：GameWindow / GameWindowWithTabs / WindowTabBase） |

---

## §二 判死的 C 行（给了证据，⛔ 没有「不做了」）

| 原版 | 判据（全量反编译 + 解包） | 结论 |
|---|---|---|
| `ResourcesBarController.Toggle(bool)` | `grep ResourcesBarController__Toggle(` = **0 调用点**；唯一实例 `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour_2491.json` 的 `m_PersistentCalls` **空** | **D** |
| `Shade.SwitchBlack()` / `SwitchBlackAnim(bool)` | `grep Shade__SwitchBlack` = **0 调用点**；13 个实例（全在战场场景）`m_PersistentCalls` 空 | **D** |
| `ScrollViewFocusFunctions` 三个 `*Coroutine` | 三个 kickoff 全量 **0 调用点**；实际被用的是**瞬跳**的 `FocusOnItem(ScrollRect, RectTransform)`（我们的等价物 = `MenuScroll.FocusOn`）。顺带记下 coroutine 的算法：`t += unscaledDeltaTime × speed`、`k = 1 − (1−t)²`、到 1 吸附 | **D** |
| `GraphicsTab.ExtendedCompatibilityClick(bool)` | 0 调用点 + 唯一实例 `MonoBehaviour_5246199328614809510.json` persistentCalls 空 | **D** |
| `MainMenuWindow.DelayedDisable()` | 0 调用点 + 主菜单实例 `MonoBehaviour_1927.json` persistentCalls 空 | **D** |
| `ObjectTogglerByQuality.OnEnable()` | 全库 **2 个实例，都在 `bundle_scenes_scenes_battlearenaaeldari`**（`MB_3983/3991`）；**菜单侧 0 实例** | **D（对菜单域）**，归战斗线 |

---

## §三 断言

| 宿主 | 新增 | 内容 |
|---|---|---|
| `Editor/MainMenuScene.cs` | **9 条** | 货币滚动算式：半程 `= ceil(10+(30−10)×0.5)`、0.5 s 到点、自己停（直接驱动 `StepCounters`，批处理不靠帧循环）；`ToggleScroll(false)` ⇒ `MenuScroll.Enabled=false` 且 `PointerLayer.ScrollUnder(视口中心)` **不再是它**、开回来立刻又能命中 |
| `Editor/ShellScene.cs` | **5 条（⑦）** | `TopBar.ToggleOverlay(true)` ⇒ 有件被抬高、队列 `+134`；关掉 ⇒ 态清掉、**逐件还原**（🧨 不还原 ⇒ 资源条永远压在窗上） |

---

## §四 账的变动

| 状态 | 批前 | 批后 |
|---|---:|---:|
| A | 332 | **347** |
| B | 43 | **79** |
| C | **136** | **120** |
| D | 316 | **327** |
| E | 0 | 0 |

> 行数 873（+表头 = 874 行）、纯 LF、列数 11/12 无越界。

---

## §五 剩下的 120 条 C —— 下一批的候选（活动线已排除）

按「玩家可见 × 在白名单 × 判据已清」排：

1. **`UICulling`（5 条 · 6 个菜单实例）** —— `OnEnable/LateUpdate/UpdateIsVisible/CalculateVisibility/RaiseEvents` 是 **Unity 消息，活的**（不是死代码）。我们目前没有「屏外剔除」；实现 = 按 `Viewport` 矩形算可见性并 **SetActive/队列** 切换。⚠️ 我们的滚动区很多已经「视口外不建」，要先确认这 6 个实例挂在哪一类节点上，别与既有裁剪重复。
2. **`NestedShadowGroup*`（25 条：Base 13 + Group 5 + SpriteRenderer 3 + TMP 3 + Retarget 1）** —— 原版 UI 的「嵌套阴影组」；我们目前是零散手写阴影。做成一件通用件（按 `NestedShadowGroupBase` 方法体）工作量中等、收益是多个窗的阴影一致。
3. **`TMProWarpText`（6 条）** —— 文本扭曲效果；我们有 `Label/TMP` 但没有 warp。判据要读方法体与材质参数。
4. **`TitleTab` / `TitleDrawerHorizontal`（5 + 3 条）** —— 称号列表页/装备入口：**整页功能**，不是小件。
5. **`MenuActions.QueueAction` 族（7 条）** —— 动作队列（窗关后执行）；我们目前「当场生效/被闸掉」。
6. **`DragAndDropItem`（10 条）** —— 拖拽数据层；**等 `PointerLayer`/`DeckCell` 的拖拽接线一起做**（卡面代理收口后再排）。
7. **`WindowsManager` 剩余（`OpenWindow<T>` / 锚点覆盖 / 场景卸载钩子）** —— 泛型开窗需要「窗口工厂」表（我们没有），要先定口径。
8. **`EverguildButton` 剩余（事件出口 / `SetToStateActiveOrDisabled` / `UpdateChildrenComponentsInteractable`）** —— 事件出口是 inspector 委托的等价物（我们已有 `onClick/onEnter/onExit`，缺 `press/state/source` 四口）。

> ⚠️ 下一批开工前先扫一眼 `资料/可并行任务清单.md`，并在账里先把「要动哪几行」圈出来（避免又碰「判死」行）。

---

## §六 顺手发现 / 口径订正

- **C 的条数**：调度台给的 182 是 R3 之前的数；**现读 136（P1=0）**，本轮后 **120**。⛔ 别再按 182 排计划。
- **`ResourcesBarController__Toggle.c` 的文件名与方法不符**：里面装的是 `Card2DController__Toggle` 的方法体（又一处「文件与方法撞名」）—— 引用这份反编译时别被名字骗。
- **`Shade` 的实例全在战场场景（13 个）**，菜单域那条 `Shade.cs` 账只是「类在这里」，具体行为归战斗线；`SwitchBlack*` 两方法本身死。
- **`ObjectTogglerByQuality` 全库只有 2 个实例，都在 `battlearenaaeldari`** ⇒ 低画质档换材质那条缺口**不在菜单域**（D 报告把它列在通用缺口，但实例位置已查明）。
