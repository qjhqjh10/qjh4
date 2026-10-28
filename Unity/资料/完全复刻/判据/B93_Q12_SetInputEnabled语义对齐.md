# B93_Q12 —— `SetInputEnabled` 与原版 `eventSystem.enabled` 的语义对齐（设计 + 施工单）

> 只读判据产出。⛔ 本文件不改任何源码 / 账 / 正本；施工由调度台按「批次」派写手。
> 上游 = `判据/B93_Q11_弹窗禁输入41处.md` / `.tsv`（44 行）。本文件只补 **Q11 §四·4 那条「语义差」**，
> 并**订正 Q11 的计数口径**（见 §七·1：按符号 grep 必然漏掉内联调用点）。

---

## 一 判定（差在哪，一句话）

**原版 `EventSystemController.Toggle(bool)` 写的是 `eventSystem.enabled` = 停掉整个 UGUI 输入模块
（点击 / 拖拽 / 滚轮 / 悬停 / 方向键 / 回车 / 文本框全停、并清掉选中与指针缓存）**；
而我们的 `PointerLayer.SetInputEnabled` 目前**只被两处 ESC 门槛读**
（`GameWindow.cs:911` + `DraftModeWindow.cs:1049`，都是「按 ESC 时」才问）
⇒ 照抄那 44 处落点只能做到「弹窗期间 **ESC** 不关窗」，**鼠标点击 / 滚轮 / 键盘照样穿过去**。

---

## 二 原版语义（逐条 + 反编译证据）

### 2·1 它到底写哪一位

- `d:/2/tools/decomp_full/EventSystemController__Toggle.c:2-10`（首行名是 `ChatPreviewMessage__Enable` = **ICF 折叠**，真身仍是它）：
  `if (*(param_1 + 0x20) != 0) { Behaviour.set_enabled(*(param_1 + 0x20), option); return; } else throw;`
- 那个 `+0x20` 就是 `eventSystem` 字段 —— 判据 = 同类访问器 `EventSystemController__get_EventSystem.c:5`
  返回的正是 `*(param_1 + 0x20)`。
- 所以 `Toggle(false)` == **`EventSystem.enabled = false`**，没有别的副作用。

### 2·2 `EventSystem.enabled = false` 之后，什么还跑、什么停了

本机 UGUI 源码 `Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/` 现读：

| # | 事实 | 判据（文件:行） |
|---|---|---|
| 1 | **`Update()` 不再被调用** ⇒ `TickModules()` 与 `m_CurrentInputModule.Process()` 都不跑 | `EventSystem.cs:472-531`（`TickModules` :481、`Process()` :515）。Unity 只对 `isActiveAndEnabled` 的 `MonoBehaviour` 调 `Update` |
| 2 | ⇒ **一整帧没有任何指针事件**：点击 / 拖拽 / 滚轮 / 悬停 enter-exit 都在 `Process()` 里 | `StandaloneInputModule.cs:294-315` 那四条支路：`SendUpdateEventToSelectedObject`(:299) · `ProcessTouchEvents`(:305) · `ProcessMouseEvent`(:306) · `sendNavigationEvents` 那一支 = move/submit(:308) |
| 3 | **`OnDisable()` 会主动收摊**：`DeactivateModule()` → `ClearSelection()` | `EventSystem.cs:429-444`；`StandaloneInputModule.DeactivateModule` :288-292 |
| 4 | `ClearSelection()` 干三件事：① 对每个 pointer 调 `HandlePointerExitAndEnter(pointer, null)` = **对正在悬停的件派 `OnPointerExit`** ② `m_PointerData.Clear()` = **指针缓存清空（停用前按住的拖拽不会在恢复后接上）** ③ `SetSelectedGameObject(null)` = **选中被清掉** | `PointerInputModule.cs:394-406` |
| 5 | **`EventSystem.current` 变成 `null`**（`OnDisable` 把实例从表里摘了） | `EventSystem.cs:441`（`m_EventSystems.Remove(this)`）＋ `:34`（`current` = `Count > 0 ? [0] : null`） |
| 6 | **重新启用**：`OnEnable` 回表 → 下一帧 `Update` 走 `m_CurrentInputModule == null` 那一支 → `ChangeEventModule` → `ActivateModule()` | `EventSystem.cs:409-427` / `:500-512` / `:533-544`；`StandaloneInputModule.cs:269-283` |
| 7 | `ActivateModule()` 重置 `m_MousePosition / m_LastMousePosition`（**不把停用期间的位移当成一次鼠标移动**），并 `SetSelectedGameObject(currentSelected ?? firstSelectedGameObject)` —— currentSelected 刚被清成 null ⇒ **落 `firstSelectedGameObject`** | `StandaloneInputModule.cs:275-282` |
| 8 | **恢复后有一帧完全不吃输入**：那一帧 `changedModule == true` ⇒ `Process()` 被跳过 | `EventSystem.cs:514`（`if (!changedModule && m_CurrentInputModule != null)`） |
| 9 | **ESC 不走 EventSystem**：原版 ESC = `WindowsManager.Update` 里的 `Input.GetKeyDown(Escape)`，它**照常能跑**；挡它的是 `GameWindow.ESCPressed` 的**显式读位** | `WindowsManager__Update.c`；`GameWindow__ESCPressed.c` 第一道门槛（反汇编 VA `0x180835ab0`，Q11/既有判据已收） |
| 10 | ⚠️ **`MainMenuWindow.ESCPressed` 覆写里 `base.ESCPressed()` 之后照样 `SettingsMenu.ExitGamePopup()`**（全文两行）⇒ **输入停用期间主菜单按 ESC 仍会弹「退出游戏」窗** | `MainMenuWindow__ESCPressed.c:5-6` |

### 2·3 「读这一位」的全量清单（本轮新做，Q11 没有）

判据 = 同一文件里既出现 `EventSystemController` 类指针 `DAT_1842bdc50`、又出现
`Behaviour.get_enabled(... + 0x20)`。全 `decomp_full` 命中 **3 个**：

| 文件 | 干什么 | 我们对位到哪 |
|---|---|---|
| `GameWindow__ESCPressed.c` | ESC 第一道门槛 | ✅ `Shell/GameWindow.cs:911` |
| `EverguildTooltipManager__Update.c:16-18` | **提示框逻辑整段闸在这一位上**（`get_enabled(eventSystem)` 为真才走；它自己用 `Input.GetMouseButtonDown` 轮询，绕过 EventSystem） | ⚠️ **未对位**（见 §六·2） |
| `SearchingMatchWindowDemo__ESCPressed.c:13-14` | 另一个 `ESCPressed` 覆写，同样两道门槛 | 未核（见 §六·3）；我们的 `SearchingOpponentWindow.cs:333` 明写「两道门槛仍只有基类那一份」⇒ 走基类对位 ✔ |

### 2·4 ⚠️ 「EventSystem 关了」≠「整个游戏一个字节都不吃输入」

原版还有一批**直接轮询设备**、完全绕过 uGUI 的路径（现读 `decomp_full`，
`UnityEngine_Input__GetMouseButton*` / `GetKeyDown` 命中 20+ 文件），例：
`CursorManager__Update` · `BattleManager__Update` · `CardScript__IsRightButtonOrTwoFingerTouch` ·
`GameBootController__Update`（开场跳过）· `EverguildTooltipManager__Update` 等。
⇒ 这一位停的是 **uGUI 那一条输入路**。
**但**：壳侧那几件**交互**（滑块拖拽 / 文本框输入）在原版里**是**走 uGUI 的
（`Slider.OnDrag` / `InputField.OnUpdateSelected`）⇒ 原版会停、我们不会（见 §七·5）。

---

## 三 我们现状（文件:行 + 逐条）

以下行号全部现读 `MyGame/Assets/CardPresentation/Shell/PointerLayer.cs`（1720 行）。

| # | 现状 | 落点 |
|---|---|---|
| 1 | 位 + 读口 + 写口 | `:746 static bool _inputEnabled = true;` · `:759 InputEnabled` · `:763-769 SetInputEnabled` |
| 2 | **唯一行为消费点** = ESC 第一道门槛 | `GameWindow.cs:911`（另有同一面的 `DraftModeWindow.cs:1049`，是「预览层」那条 ESC 子闸） |
| 3 | **指针两条命中口都不问这一位** | `HitButton` `:1562`，首句 `:1564 if (InputBlocked) return null;`；`HitScroll` `:1704`，首句 `:1706` 同 |
| 4 | **所有指针入口都汇到那两口** ⇒ 只改这两行就覆盖全部指针路 | `ClickAt:1361` · `ButtonAt:1370` · `ScrollUnder:1373` · `WheelAt:1321/:1344` · `HoverAt:1414` · `PressAt:1429` · `ReleaseAt:1476` · `Update` 的 `:446/:450-506`。（`HitLines:630` 是**日志口**，走 `CollectHits`，不影响行为） |
| 5 | **键盘三跳都不问** | `KeySubmit:922`（回车）· `KeyMove:938`（方向键）· `HandleTyping:196`（`Update:387` 调）；汇总口 `KeyFrame:851` |
| 6 | `KeyCancel:881` **本身不问** —— 门槛在那扇窗的 `ESCPressed` 里 | ✅ 这是原版的分法（`:905-912` 注释写着），**保持** |
| 7 | **停用时不清任何状态** | `_hover:100` · `_pressed/_down/_dragScroll/_dragItem/_itemDragging/_dragging`:93/101/117/118/103 · `_sel:778`（`Select:808`）|
| 8 | 已有的另一把闸 = `InputBlocked:1542`（黑场/遮罩挡**指针**），`:1535-1537` 明写「**只拦指针**，键盘不动」 | 已被 `HitButton`/`HitScroll` 消费 ⇒ 证明「这一口挡得住」，但**语义不同**（那是射线靶盖住，这是输入模块停用） |
| 9 | 壳里**两条绕过 PointerLayer 的直接设备轮询**（本闸管不到） | `SettingsWindow.cs:4957 UpdateFpsDrag`（位置 + 左键态）· `TextInputField.cs:181/203/225`（`Keyboard.current`）。`ShellRuntime.cs:1094 TickIntro` **不算差**（原版 `GameBootController__Update` 也直接轮询） |
| 10 | `PointerLayer` 是 **`DontDestroyOnLoad`** 的常驻实例，`_inputEnabled` 是 **static** | `:58-80`（`Instance` 的 doc 里写着那条 A1491 的病史）⇒ 停用期间切场景会把「停用」带到新场景 —— **原版同病**（`EventSystemController` 是常驻单例、那台 `EventSystem` 在 `level0`） |

**已接的 6 条调用**（Q11 已核，本轮逐条看过宿主）：`GachaEventWindow.cs:1402/:1416/:1477`（开包）·
`RankedDivisionChangeWindow.cs:229/:1435/:1462`（排位升段）。

---

## 四 候选方案（代价 / 风险 / 对既有 6 条已接窗的影响）

### 方案 A（✅ 推荐）：整位对位 —— 接进「两条命中口 + 键盘三跳」+ 停用时清状态

**改什么**：`HitButton:1564` / `HitScroll:1706` 首句加 `|| !_inputEnabled`；
`KeySubmit` / `KeyMove` / `HandleTyping` 各加一条早退；`SetInputEnabled(false)` 里清
`_hover` / `_sel` / `_pressed` 那一组。全部落在 `PointerLayer.cs` **一个文件**。

- **代价**：5 处早退 + 1 段状态清理（约 10 行）＋ 3 处 doc 订正。无新文件、无新类。
- **风险**：
  1. ⚠️ **两张闸别合并** —— 把 `_inputEnabled` 塞进 `InputBlocked` 的 getter 会让
     `:1535-1537` 那段「只拦指针、键盘不动」的判据**变成假话**，也会让黑场那条诊断日志误报。**各判各的**。
  2. ⚠️ **闸可能挡住自己的关窗路** —— 逐条核过（见下表），**已接的 6 条里没有一条**是
     「停用期间必须靠一次点击才能解禁」⇒ **无死锁**。
  3. ⚠️ 静态位跨场景（§三·10）—— **照抄原版、不复位**，但要出声（施工单 ④）。
- **对既有 6 条已接窗的影响（逐条）**：

| 窗 | 现行为 | 改后 | 判定 |
|---|---|---|---|
| `RankedDivisionChangeWindow`（4 条） | `Open()` 关输入，但**动画期间点 `Menu Dark Background` 仍能立刻关窗**（`:261` 的 `ShadeHit(..., () => Close())`） | **点不动**，只能等 clip 的 `AnimationFinished`（`_promoteDelay 1.40s`）或 9s 兜底（`_inputSafetyLeft = 10.40s`）⇒ 与**原版 `Open()` 关 / `AnimationFinished`+安全协程开** 完全对齐 | ✅ **修正**（不是回归）。必须保留 `Close()` 覆写里那句无条件解禁（`:1462` 已有：ESC / `CloseAllWindows` / 暗底任一路都解禁） |
| `GachaEventWindow`（3 条） | `OpenCrateButtonClick` 关，**同一次调用里** `TryPurchaseCrate()` 恒 false（`:1425-1430`）⇒ `:1416` 立刻解禁 | **观测不到变化**（disable/enable 同帧）。若将来接上真回包，视频播放 + 等回包期间玩家会真的点不动 —— **那也是原版形状** | ✅ 不变 |
| 断言侧 | `ShopFamilyAssertE13.cs:207/208/215`（D 组）只读 `InputEnabled` 这个 bool、**不点按钮** | 不受影响 | ✅ |
| 断言侧 | `MainMenuScene.cs:20351-20384` / `20590-20627`、`ShellScene.cs:3676-3761` 同样只读位 + 走 ESC | 不受影响 | ✅ |

### 方案 B（⛔ 不推荐）：只接指针、不接键盘（最小改动，2 行）

- **代价**：2 行。**风险**：`KeySubmit:922` 会 `b.Click()` ⇒ **「点得动」从键盘那条缝漏回来**
  （Tab+Enter 就能操作一个「被挡住」的窗）⇒ 同一把闸出现两条互斥结论，**比不改更糟**。
- 对 6 条已接窗：同上表，但键盘那一面永远对不上。

### 方案 C（⛔ 不推荐）：走 `CanvasGroup.blocksRaycasts` / 逐窗禁用交互

- **代价**：每个宿主窗都要动（44 处 × 逐窗）。**风险**：`blocksRaycasts` **只在 uGUI 射线下有意义**，
  而壳里**没有 uGUI `EventSystem`**（`PointerLayer` 自己扫 `WindowButton`）⇒ **对壳完全无效**，
  是最典型的「看着已经做了、实际零效果」。判据同 `InputBlocked` 那段 doc（`:1523-1527`：原版靠射线靶、我们不走射线）。

---

## 五 推荐的施工单

**唯一写手文件：`d:/4/Unity/MyGame/Assets/CardPresentation/Shell/PointerLayer.cs`**
⛔ 不要动 `GameWindow.cs` / `GachaEventWindow.cs` / `RankedDivisionChangeWindow.cs` / `MainMenuRuntime.cs`。
⚠️ 新增成员一律**追加到类尾**（本文件被账按 `文件:行号` 引用，插成员会让下游行号整体漂移 —— CLAUDE.md 已记此坑）。

### ① 两条命中口（判据 = §二·2 第 1/2 条：`Process()` 整段不跑）

- `:1564`　`if (InputBlocked) return null;` → `if (InputBlocked || !_inputEnabled) return null;`
- `:1706`　同上（`HitScroll`）。
- ⛔ **不要**把 `_inputEnabled` 塞进 `InputBlocked`（理由见 §四·方案 A 风险 1）。

### ② 键盘三跳（判据 = `StandaloneInputModule.Process()` 那四条支路整段不跑，`StandaloneInputModule.cs:294-315`）

- `KeySubmit`（`:922`）首句加：`if (!_inputEnabled) { <出声> return false; }`
- `KeyMove`（`:938`）首句加：同上
- `HandleTyping`（`:196`）首句加：同上（文字输入在原版走 `SendUpdateEventToSelectedObject` :299）
- ⛔ **不动** `KeyCancel:881` —— ESC 门槛在原版就在 `GameWindow.ESCPressed` 里，我们已在 `GameWindow.cs:911` 对位；
  两层会让同一次「ESC 没反应」打两行日志。
- ⛔ **不动** `MainMenuRuntime.EscPressed:1696` —— 原版 `MainMenuWindow__ESCPressed.c:5-6` 在 `base` 早退之后
  **照样**弹退出窗 ⇒ **不挡才是原版**（见 §七·6）。

### ③ 停用时清状态（判据 = §二·2 第 3/4 条：`DeactivateModule` → `ClearSelection`）

在 `SetInputEnabled` 的 `_inputEnabled = on;`（`:766`）**之后**，只在**真的翻成 `false`** 时执行：

```
var pl = Instance;                       // 批处理/编辑态可能 null ⇒ 不能因此炸if (pl != null && !on)
{
    if (pl._hover != null) { pl._hover.Exit(); pl._hover = null; }   // = HandlePointerExitAndEnter(pointer, null)
    pl._pressed = false; pl._down = null;                            // = m_PointerData.Clear()
    pl._dragScroll = null; pl._dragItem = default(DragSource);
    pl._itemDragging = false; pl._dragging = false;
    pl.Select(null);                                                 // = SetSelectedGameObject(null)
}
```

⚠️ **不清** `_lastPx` / `_haveLastPx`：原版恢复后**有一帧不吃输入**（§二·2 第 8 条），我们没有帧节流口；
不清这一对更接近那个形状（见 §六·5，精确对齐未做）。
⚠️ 施工时注意 `_hover`/`Select` 都是**实例**成员，而 `SetInputEnabled` 是 **static** ⇒ 必须经 `Instance`，且 `Instance` 会
**惰性新建一台**（`:58-80`，含 `DontDestroyOnLoad`）—— 在编辑器/批处理下它会走 `isPlaying` 闸，别在这里触发它。
**建议**：改用已有的 `_inst`（私有静态字段，`Instance` 的缓存）判空，避免 `SetInputEnabled(false)` 顺手造一台 PointerLayer。

### ④ 出声（红线：不许静默失败）

`SetInputEnabled` 现在那条日志（`:767-768`）只解释「ESC 不关窗」。改后这一位**还管点击/滚轮/悬停/键盘** ⇒
日志必须写全，否则真机上「点了没反应」会被指到错的地方。建议签名扩成
`SetInputEnabled(bool on, string why = null)`（**零调用点改动**，44 处落点可以逐步填窗名），
并在 `false` 那一支用 `Debug.LogWarning` 打出「**整个输入层停用**（点击/滚轮/悬停/方向键/回车/打字全停）· 谁开的闸：`why`」。
⚠️ 这不是新行为，是红线的落地；不填 `why` 也能跑。

### ⑤ 三处 doc 就地订正（铁律 5，不是新行为）

- `:748-758`（`InputEnabled` 的 doc）：把「对位 `ESCPressed` 的第一道门槛」补成
  「**= 原版 `eventSystem.enabled` 整位**：ESC 门槛 + 整个输入模块停摆；读位 3 处 / 写位 50 处，见 `判据/B93_Q12…` §二」。
- `:761`（`SetInputEnabled` 的 doc）：「停用时 ESC 不关窗」→「停用时**整条输入路停摆**」。
- `:1535-1537`（`InputBlocked` 的 doc）：那段「只拦指针」**仍然成立**，但末尾要加一句
  「**另一把闸 `_inputEnabled` 才是连键盘一起停的那把**，别混」——否则下一个人会以为改 `InputBlocked` 就够了。

### ⑥ ⛔ 明确不碰的东西

`InputBlocked`（黑场那一路）· `ClickForTest`（本仓只在 `Battle/` 有，壳里零调用点 ⇒ 无需保护）·
`BattleEventSystem`（战斗侧有**真的** uGUI `EventSystem`；44 处落点**全是壳侧**，战斗 HUD 的点击本来就是战斗自己的闸）。

### ⑦ 断言（宿主 = `Editor/ShellScene.cs`，**另开一节**，紧跟 `:3658` 那一节之后）

照 **`:1262-1360`（N8 黑场那节）**的两态对照写法：探针 = `MakeNavButton(n8Hits, "N8Btn", 400f, 400f, 80f, 40f)`，
命中点固定 `(400,400)`，`probe.onClick = () => fired++;`。

```
前提（硬检查，⛔ 不写成 if(前提) Check(结论)）:
  ① SetInputEnabled(true) ⇒ InputEnabled == true
  ② p.ButtonAt(400,400) == probe                             // 命中表是活的
  ③ p.ClickAt(400,400) == true && fired == 1                 // 点击真的会派发
  ④ p.Select(probe); p.KeySubmit() == true && fired == 2     // 键盘那条也活着
翻位:
  ⑤ p.SetInputEnabled(false) ⇒ InputEnabled == false
  ⑥ p.ButtonAt(400,400) == null                              // ★ 指针命不到
  ⑦ p.ClickAt(400,400) == false && fired == 2                // ★ 且没派发（返回 false 但 fired 不动 = 假绿）
  ⑧ p.WheelAt(400,400,-1f) == false                          // ★ 滚轮也停
  ⑨ p.Selected == null                                       // = ClearSelection
  ⑩ p.HoveredForTest == null && probe.Hovered == false        // = HandlePointerExitAndEnter(pointer, null)
  ⑪ p.Select(probe); p.KeySubmit() == false && fired == 2     // ★ 键盘那条也停（挡「只挡 ESC」）
翻回:
  ⑫ p.SetInputEnabled(true)
  ⑬ p.ButtonAt(400,400) == probe                             // ★ 同一坐标恢复命中
  ⑭ p.ClickAt(400,400) == true && fired == 3
收尾: Object.DestroyImmediate(probeGo); p.SetInputEnabled(true);
```

**灭自证（结构上不可能同时满足）**：⑥/⑦/⑪ 与 ②/③/④/⑬/⑭ 用的是**同一颗钮、同一个坐标**，
**只翻 `InputEnabled` 一位**，期望值相反：

| 坏实现 | 会红的那条 |
|---|---|
| 让 `HitButton` 恒返回 null（命中表坏掉） | ② / ⑬ |
| 闸压根没接（现状） | ⑥ / ⑦ / ⑪ |
| 只挡指针、不挡键盘（= 方案 B） | ⑪ |
| 只挡 ESC（= 现状口径） | ⑥ / ⑦ |
| 只加两行 `return null`、不做状态清理 | ⑨ / ⑩ |

⚠️ **别新写「ESC 那一面没被改坏」的断言** —— `ShellScene.cs:3697-3705` 已有（`SetInputEnabled(false)` ⇒ `KeyCancel()` false；
恢复 ⇒ true），两处写同一条规则 = 迟早不一致。
⚠️ 探针用完必须 `DestroyImmediate`（批处理没有帧循环，`Destroy` 不生效 —— 本文件 `:1311-1312` 已记）。

### ⑧ 验收（按铁律 12 现行口径）

动了**共用件** `Shell/PointerLayer.cs` ⇒ 受影响宿主 = **`ShellScene.Run`**（新断言在这儿，必跑）+
**`MainMenuScene.Run`**（`MainMenuScene.cs:20351/20590-20627` 有 `InputEnabled` 断言）+
**`ShopFamilyAssertE13`** 所在宿主（D 组）。
⚠️ 但**完全复刻阶段「验收自检」是暂停的**（CLAUDE.md 铁律 12 现行口径）⇒ 写手只跑
`bash d:/4/Unity/工具/typecheck.sh`；上面三条留到用户恢复验收时再跑。

---

## 六 没查清的部分

1. **`EventSystem.firstSelectedGameObject` 的原版值取不到** —— 恢复时 `ActivateModule` 会
   `SetSelectedGameObject(firstSelectedGameObject)`（`StandaloneInputModule.cs:278-282`），
   而本机**没有那台 `EventSystem` 的实例**（`assets_full` 24.7 万文件 grep `m_FirstSelected` 零命中）
   ⇒ **「恢复后选中落到谁」无判据**。我们现在的做法（**不恢复**）是自选，要标 `🔶 自己设计`
   （`PointerLayer.cs:709-720` 已有同族标注，可挂在那一段上）。
2. **`EverguildTooltipManager` 那一面我们没对位** —— 原版提示框逻辑整段闸在 `eventSystem.enabled` 上
   （`EverguildTooltipManager__Update.c:16-18`）；我们的提示框走 `WindowButton.onEnter/onExit`
   （`MenuDraw` 建的钮 + 逐窗挂，例 `CampaignTab.cs:1687-1690` / `ExpansionPassEventWindow.cs:1538-1540`）。
   闸生效后**新的悬停起不来**（`HoverAt` 被挡）、**已经开着的靠 ③ 的 `_hover.Exit()` 收起** ⇒ 行为上覆盖了；
   但**是否还有不经 `_hover` 开提示框的路**未逐条核（壳侧看起来只有这一条）。
3. **`SearchingMatchWindowDemo__ESCPressed` 是哪个窗** —— 它也读这一位。名字带 `Demo` ⇒ 可能是演示场景专用类。
   我们的 `SearchingOpponentWindow.cs:333` 说「两道门槛仍只有基类那一份」⇒ 已对位；
   但**原版 `…Demo` 是否就是我们的 `SearchingOpponentWindow` 没核**（若它是独立一条链，那是一个未接点）。
4. **`CraftingPanel` / `UpgradePanel` 那 8 条内联落点，具体该落在我们哪一行没现读核** ——
   宿主已定位（`CraftingPanel` ↔ `Shell/CardDetailPopup.CraftNow()`；`UpgradePanel` ↔
   `Effects/CardDisplayLevelUp.cs` + `CardDetailPopup` 的升级链），但**逐行落点属于另一个批次**（见 §七·2、七·3）。
5. **「恢复后隔一帧才吃输入」这一点没有等价物** —— 原版 `changedModule == true` 会跳过那一帧 `Process()`
   （`EventSystem.cs:514`）。我们没有帧节流口；自检又全是直调（没有帧）⇒ **只能靠真机验**，本轮未做。
   影响：恢复后**同一帧内**的一次点击，我们命中、原版不命中。**低风险但如实记**。

---

## 七 顺手发现（都需调度台按铁律 5 / 5·b 落账）

1. 🔴 **Q11 的「41 文件 / 44 行」是按符号 `ChatPreviewMessage__Enable` grep 出来的，必然漏掉内联调用点。**
   本轮用第二个判据交叉（`DAT_1842bdc50` ∧ `Behaviour.set_enabled(... + 0x20)`）现读：
   - **写位 = 50 行**：Q11 那 42 条符号调用 **＋ 内联 8 条**（`CraftingPanel` 3 · `UpgradePanel` 5）。
     命中文件 10 个；另 2 个 `GachaCratePanel` 的 `+0x38` 是 `videoPlayer`（**不是**这一位，别数进来）。
   - **读位 = 3 个文件**：`GameWindow__ESCPressed.c`（已对位）· `SearchingMatchWindowDemo__ESCPressed.c`（见 §六·3）·
     `EverguildTooltipManager__Update.c`（**未对位**，见 §六·2）。
   ⇒ 建议 Q11 的 TSV **补 8 行**（`CraftingPanel` / `UpgradePanel`），别按 41 收口。
   🔑 **方法论**：这是 CLAUDE.md 已记「`cpp2il`/ILSpy 会静默丢东西」的**第四面** ——
   **按符号 grep 会漏掉被内联的调用点**（`Toggle` 只有 3 句，编译器把它整个塞进调用方）。
   判「某个 `Toggle`/`Enable` 一类的写位有多少」时，**必须再按「字段偏移 + 访问器」扫一遍**。

2. 🔴 **`Effects/CardDisplayLevelUp.cs` 的两处注释「原版 `UpgradePanel.TryUpgrade` 没有方法体产物，判据缺」是过期结论**
   （`:57` 文件头 ④ · `:429` 的 `AnimationWillRun` doc）。
   `decomp_full/UpgradePanel__TryUpgrade.c` **全文可读**，而且这一位就写在那儿：
   `+0xc0 = 2`（**两拍动画事件计数**）→ `eventSystem.enabled = false` → 起升级请求的两个回调；
   `UpgradePanel__AnimationFinished.c:10-15` = `+0xc0 -= 1; if (< 1) → enabled = true`；
   `UpgradePanel__CheckFinish.c:15` 也解禁。
   ⇒ 我们自己那条 `InputBlocked = true/false`（`CardDisplayLevelUp.cs:297/324`）**是 `🔶 自己设计`**：
   它只锁「升级」那一颗钮（`CardDetailPopup.cs:1631` 的 `UpgradeAnimLocked`），**原版锁的是整个输入层** ⇒ 差一面。

3. **`CraftingPanel.TryCraftingInternal` 也在这条链上**（`:21-23` 关 → `b__22_0` 成功开 / `b__22_1` 失败开），
   我们这边 = `CardDetailPopup.CraftNow()` ⇒ 现在是**零锁**（合成期间别的钮照常可点）。

4. 🔴 **`InputBlocked` 与 `InputEnabled` 是两把不同的闸，⛔ 别合并** ——
   前者 = 黑场/遮罩靠「射线靶盖住」（原版用 `Image.m_RaycastTarget = 1` 实现，判据 → `InputBlocked` 的 doc `:1523-1527`），
   后者 = 输入模块停用。合并会让 `:1535-1537` 那段「只拦指针、键盘不动」的判据变成假话。
   **这一条要进账**，否则下一个人「顺手统一」，两把闸的判据会一起塌。

5. ⚠️ **壳里有两条直接设备轮询，本闸管不到；而它们在原版里是走 uGUI 的**（原版会停、我们不会）：
   - `SettingsWindow.cs:4957 UpdateFpsDrag`（读 `Mouse.current.position` + `leftButton`）—— 原版 `Slider.OnDrag`。
   - `TextInputField.cs:181/203/225`（读 `Keyboard.current`）—— 原版 `InputField.OnUpdateSelected`。
   `/Shell/ShellRuntime.cs:1094 TickIntro`（开场跳过）**不算差**（原版 `GameBootController__Update` 也直接轮询 `Input.anyKeyDown`）。
   ⇒ 要不要跟这一批同改，由调度台按铁律 14 的「解决成本」判；**先把账立上**。

6. 🔴 **输入停用期间，主菜单按 ESC 仍会弹「退出游戏」窗 —— 这是原版形状，⛔ 别「修」**。
   `MainMenuWindow__ESCPressed.c:5-6`（全文两行）= `GameWindow__ESCPressed(this)` 然后**无条件**
   `SettingsMenu__ExitGamePopup()`；`base` 那句只是自己早退，拦不住覆写。
   我们正好也是这样（`MainMenuRuntime.EscPressed:1696` 不问位）⇒ **对**。
   建议写进账，防止下一个代理把它当缺陷补上门槛。

7. 🔴 **原版「解禁」不全是「回调里开一句」** —— `UpgradePanel` 那族靠 `+0xc0` 这个**事件计数**
   （`TryUpgrade` 置 2 → 每收到一个动画事件 `-1` → `< 1` 才解禁），另有 `CheckFinish` 兜底。
   ⇒ 派写手做 §五 的落点时，**别把解禁写成 `Close()` 里的一句了事**（那会让动画没播完就解禁）。

---

## 八 出处（一览）

- 原版写入：`decomp_full/EventSystemController__Toggle.c:2-10` · `…__get_EventSystem.c:5`
- 原版读位：`decomp_full/GameWindow__ESCPressed.c` · `…/EverguildTooltipManager__Update.c:16-18` ·
  `…/SearchingMatchWindowDemo__ESCPressed.c:13-14`
- 原版内联写位（Q11 漏掉的）：`decomp_full/CraftingPanel__TryCraftingInternal.c:21-23` ·
  `CraftingPanel___TryCraftingInternal_g__OnFail_22_1.c:14` · `…_g__OnSuccess_22_0.c:16` ·
  `UpgradePanel__TryUpgrade.c:53-55` · `UpgradePanel__AnimationFinished.c:10-15` ·
  `UpgradePanel__CheckFinish.c:15` · `UpgradePanel___TryUpgrade_g__OnFail_24_1.c:20` · `…_g__OnSuccess_24_0.c:16`
- UGUI 语义：`Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/`
  `EventSystem.cs:32-34/409-444/472-531/533-544` · `InputModules/StandaloneInputModule.cs:269-315` ·
  `InputModules/PointerInputModule.cs:394-406`
- 我们：`Shell/PointerLayer.cs:58-80/93/100-103/117-118/196/384-506/746/759/763-769/778/808/851/881/922/938/1321/1344/1361/1414/1429/1476/1542/1562-1592/1704-1718` ·
  `Shell/GameWindow.cs:908-938` · `Shell/DraftModeWindow.cs:1046-1056` · `Shell/MainMenuRuntime.cs:1696-1714` ·
  `Shell/GachaEventWindow.cs:1398-1421/1462-1488` · `Shell/RankedDivisionChangeWindow.cs:220-231/1394-1470` ·
  `Effects/CardDisplayLevelUp.cs:57/213/297/324/429` · `Shell/CardDetailPopup.cs:1631/2855`
- 断言宿主：`Editor/ShellScene.cs:1262-1360`（N8 两态样板）· `:3658-3770`（ESC 三门槛那节）·
  `Editor/ShopFamilyAssertE13.cs:195-235`（D 组）· `Editor/MainMenuScene.cs:20327-20390/20590-20627`
