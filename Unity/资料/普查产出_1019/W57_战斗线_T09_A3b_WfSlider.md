# W57 · 战斗线 `T09` 步骤 A3-b —— `WfSlider` 拖动输入切 uGUI

> **日期** 2026-10-20（第五十七会话）· **代理** `T09-A3b`（执行）
> **判据入口** `资料/普查产出_1019/W57_战斗线_T09迁移计划.md` §2 步骤 A3-b（`:178`）· A1 报告 `W57_战斗线_T09_A1.md`
> **白名单** = `Unity/MyGame/Assets/CardPresentation/Battle/WfSlider.cs`（**唯一改动文件**，+236 / −1）
> **本波口径 = 只做不验**：未跑 Unity / 未起 player / 未跑 `*.Run` / 未跑 `_run_8_checks.sh`；只跑了秒级 typecheck。
> 未动 git、未改正本、未删文件；**未改** `SettingsPanel` / `BattleDriver` / `HudCanvas`（A2 拥有）。

---

## 〇、一句话

**画布档**（`uguiHost != null`，= 战斗设置面板那三根）现在由 **uGUI 事件**驱动：
三张图成为可射线件、滑块根上挂 `EventTrigger`，`PointerDown → Drag → PointerUp`（+ `PointerEnter/Exit`）
走 `EventSystem` 送达；坐标换算**只走** `RectTransformUtility.ScreenPointToLocalPointInRectangle`。
**世界档**（`Shell/SettingsWindow` 那三根，`uguiHost == null`）**一个字没动** —— 仍由宿主喂
`Contains` / `SetFromPointer`。数值算式收成**唯一一份** `SetFromLocal`，`SetFromPointer` 转调它
⇒ 值域 / 步进（`1e-4` 阈值）/ `OnChanged` / 悬停态**逐位不变**。

---

## 一、改前 / 改后对照

| 环节 | 改前（现读） | 改后 | 现读行号 |
|---|---|---|---|
| 输入来源（画布档） | **没有任何 uGUI 事件入口**；`WfSlider` 不是 MonoBehaviour、也不挂任何组件。真实拖动靠宿主 `SettingsPanel.PointerFrame` 每帧读世界指针 → `Contains(world)` → `SetFromPointer(world)` | 根上挂 `EventTrigger`，把 `PointerDown / Drag / PointerUp / PointerEnter / PointerExit` 接到本件。拖动是**持续状态**，Drag 一直送到 `PointerUp`（与 EventSystem 的 `pointerDrag` 语义一致，指针离开矩形后仍继续） | `WfSlider.cs:1012` `SetupUguiInput()` · `:1050-1054` 五条 Entry · `:1072-1103` 五个处理函数 |
| 事件接口 | 无 | uGUI 自带的 `EventTrigger` 实现 `IPointerDownHandler / IDragHandler / IPointerUpHandler / IPointerEnter/ExitHandler` 整族（本机源码 `…/com.unity.ugui/Runtime/UGUI/EventSystem/EventTrigger.cs:143-160`），由它把事件转给本件的委托 | 同上 |
| 为什么不用自定义 MonoBehaviour | —— | 本波是**单文件白名单**，而本仓硬规矩 = **`MonoBehaviour` 类名必须 = 文件名**（2026-10-13 `!u115` 根治；现读全仓「一文件多 MonoBehaviour」= 0）。在 `WfSlider.cs` 里再写一个 `WfSliderPointerInput : MonoBehaviour` 会破坏那条；`EventTrigger` 是 uGUI 自带组件、不新增脚本资产 | 文件头 `:298-333` |
| 事件怎么到根上 | —— | 射线打到三张子图里的任意一张 ⇒ `ExecuteEvents.GetEventHandler<T>` **沿父链冒泡**（本机源码 `ExecuteEvents.cs:359-372`）⇒ 落到根上的 `EventTrigger`（根自己不需要 `Graphic`）。⛔ 不是三张图各挂一个 | 文件头 `:313-317` |
| 可射线件（轨道 / 填条） | `HudCanvas.MakeSlicedPiece` 建的 `Image`，`raycastTarget = false` | `raycastTarget = true`、`raycastPadding = 0`（= 原版 `Background` MB_4355 / `Fill` MB_4553 的 `m_RaycastTarget = 1` / `m_RaycastPadding = 0`） | `:1019-1020` |
| 可射线件（手柄） | `HudCanvas.MakeImageInParent` 建的 `Image`，`raycastTarget = false` | `raycastTarget = true`；`raycastPadding = (padX, HandlePadPx, padX, HandlePadPx)`，`padX = HandlePadPx + (_handlePx − HandleFrameW)/2` = **−31.2025**（战斗那三根） | `:1022-1037` |
| 手柄命中框的对齐理由 | 原版手柄命中框 = 节点 rect `46.811 × 运行期框高` 四边按 `m_RaycastPadding = −25` **外扩** ⇒ `96.811 × 84.406`（= 本件 `HitHandlePadded` 算的同一块） | 我们的手柄节点 rect 是**画出来的正方** `34.406 × 34.406` ⇒ 横向把差的 `(46.811 − 34.406)/2 = 6.2025` 补进 padding：`34.406 + 2×31.2025 = 96.811`、`34.406 + 2×25 = 84.406` ⇒ **与 `Contains` 的命中框逐位相同** | `:1022-1030` |
| 坐标换算 | 宿主：屏幕 → 世界（`LayoutSpace.ScreenToWorld`）→ `InverseTransformPoint` | `RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, e.position, e.pressEventCamera, out local)` —— **只此一条**（⛔ 未混用 `LayoutSpace.ToPixel`）。相机取 `pressEventCamera`（`PointerEventData.cs:315-323`；`ScreenSpaceCamera` 画布 = 该相机、Overlay = null，与 `Slider.UpdateDrag` 同一句） | `:1113-1128` |
| 数值算式 | `SetFromPointer` 里内联：`t = Clamp01((l.x + _w/2)/(_w − _slideInsetU))` → `Lerp(Min,Max,t)` → `1e-4` 阈值 → `Value/Layout/OnChanged` | 搬进 `SetFromLocal(Vector2 l)`（**唯一一份**）；`SetFromPointer` = `InverseTransformPoint` 后转调它；uGUI 路径也转调它 | `:974-1001` |
| 按下 | 宿主 `Contains` → `SetFromPointer`（按下即跳到指针） | `OnUguiPointerDown`：原版 `MayDrag` 两条闸（`!_fbInteractable` / 非左键直接返回）→ `SetPressed(true)` → 换算 → `SetFromLocal`（**同样按下即跳到指针**） | `:1072-1079` |
| 拖动 | 宿主每帧 `_dragSlider.SetFromPointer(world)` | `OnUguiDrag`：`_dragging && _fbInteractable && 左键` → 换算 → `SetFromLocal` | `:1083-1088` |
| 抬起 | 宿主 `!down` 清 `_dragSlider` + `SetPressed(false)` | `OnUguiPointerUp`：`_dragging = false; SetPressed(false);` **不改值**（照原版 —— `Slider` 没有自己的 `OnPointerUp`，`Selectable.OnPointerUp` 只清按下态） | `:1092-1098` |
| 悬停 | 宿主每帧 `SetHover(Contains(world))` | `OnUguiPointerEnter/Exit` → 本件既有的 `SetHover`（状态优先级仍只在 `FbSetTarget`：禁用 > 按下 > 悬停） | `:1102-1103` |
| 隐藏时清态 | `SetVisible(false)` → `SetHover(false) / SetPressed(false)` | 同一句之前多清 `_dragging = false; _pointerHover = false;`（窗在拖动中途被关掉时 `PointerUp` 可能到不了 ⇒ 不清会「下次打开仍是拖动中」） | `:1139` 起 |
| 自检探针 | 无 | **Editor-only 只读**：`ValueForTest` / `DraggingForTest` / `PointerHoverForTest` / `PressedForTest` / `PointerInputForTest` / `Track\|Fill\|HandleRaycastTargetForTest` / `HandleRaycastPaddingForTest`（`#if UNITY_EDITOR` 段 `:1401-1435`，播放器里不存在） | `:1396-1435` |
| 世界档 | `Contains` / `SetFromPointer` 由宿主喂 | **逐位相同**（`SetupUguiInput` 在 `_ugui == false` 时直接返回；`SetFromPointer` 只改成转调同算式） | `:1014` |
| 建件收尾 | `Layout(); FbInit(); return` | `Layout(); FbInit(); if (_ugui) SetupUguiInput(); return` | `:706` |

---

## 二、API 与数值行为不变的证据

### 2·1 对外 API 一个字没改

- **没有删除 / 改签名 / 改可见性**任何既有成员。新增的只有：4 个私有字段（`:335-342`）、
  9 个私有方法、`#if UNITY_EDITOR` 里的只读探针。
- `SetFromPointer(Vector3) → bool` 签名与返回值语义不变（`true = 值变了`）；方法体从
  「内联算式」改成「`InverseTransformPoint` + 转调 `SetFromLocal`」——**同一个 `l.x`、同一个算式**。
- `Contains / SetHover / SetPressed / SetInteractable / TickFeedback / SetValue / SetVisible` 全部原样。

### 2·2 数值行为逐位不变（逐句对照）

`SetFromLocal`（`:992-1001`）的四个数值步骤与改前 `SetFromPointer` **逐字相同**：

```csharp
float t = Mathf.Clamp01((l.x + _w * 0.5f) / (_w - _slideInsetU));   // 与改前同一句
float v = Mathf.Lerp(Min, Max, t);                                  // 同一句
if (Mathf.Abs(v - Value) < 1e-4f) return false;                     // 同一个步进阈值
Value = v; Layout(); if (OnChanged != null) OnChanged(Value);       // 同一组副作用
```

- **值域**：`Min/Max` 不参与任何新逻辑；`Lerp` + `Clamp01` 对 `t` 的处理与改前一致。
- **步进**：`1e-4` 阈值原样保留 —— 也正好让**过渡期两套路径**不重复回调（见 2·3）。
- **回调**：`OnChanged` 只在「值真变了」时触发一次，与改前同一判据。
- **悬停 / 按下**：仍只走 `SetHover/SetPressed`，状态优先级（禁用 > 按下 > 悬停）只在 `FbSetTarget` 一份，
  uGUI 处理函数**不直接改色**。
- **`SetInteractable(false)` 的语义顺带对齐原版**：`OnUguiPointerDown/Drag` 有 `_fbInteractable` 闸
  （原版 `Slider.MayDrag` 的 `IsInteractable()`）。**现状**：生产代码没有任何一处把滑块设为不可交互
  （`SetInteractable` 只在 `Editor/BattleScene.cs:26788` 的反馈自检里用过、随即还原）⇒ 对现有行为零影响。

### 2·3 过渡期（宿主手搓口仍活着）为什么不会双触发

宿主 `SettingsPanel.PointerFrame`（`SettingsPanel.cs:1981-1992`）现在仍每帧喂 `Contains` / `SetFromPointer`。
两条路对**同一个屏幕点**算出**同一个值**：

- 宿主：屏幕 → 世界（相机投影）→ `InverseTransformPoint`；
- 本件：屏幕 → 画布局部（`ScreenPointToLocalPointInRectangle`）；
- 两者数学上是同一点，浮点误差 < 1e-3 px ⇒ 值差 ≈ `1e-3 / 551.08` ≈ **2e-6** ≪ `1e-4`
  ⇒ 第二次调用直接 `return false`，**`OnChanged` 只响一次**。
- `SetPressed` 两边写的是同一个布尔值（幂等）；`SetHover` 同理。

⚠️ **这是过渡态**：A2/A3 那一批把宿主的手搓口删掉之后，本段就是唯一入口。⛔ 本波不改宿主（不在白名单）。

### 2·4 命中几何的逐值证据

| 件 | 我们的 rect | uGUI padding | 合成命中框 | 原版 / `Contains` 的同一块 |
|---|---|---|---|---|
| 轨道（`slider_bg`） | 561.08 × 12（`sizeDelta`） | `(0,0,0,0)` | 561.08 × 12 | 原版 `Background` `m_RaycastPadding = 0` |
| 填条（`slider_fill`） | `值 × 561.08` × 12 | `(0,0,0,0)` | 值 × 561.08 × 12 | 原版 `Fill` `m_RaycastPadding = 0` |
| 手柄（`slider_handle`） | 34.406 × 34.406（画出来的正方） | `(−31.2025, −25, −31.2025, −25)` | **96.811 × 84.406** | `HitHandlePadded`：`HandleFrameW 46.811 + 2×25` × `handlePx 34.406 + 2×25` ⇒ **96.811 × 84.406** |

符号口径 = uGUI `m_RaycastPadding`「**正值缩小、负值扩大**」（判据 → `MenuDraw.PaddedRect` 的注释，
含 `Culling/Clipping.cs:26-30` 与 `GraphicRaycaster.cs:327`）。

### 2·5 改动规模 / 行尾

```
git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Battle/WfSlider.cs
236     1       Unity/MyGame/Assets/CardPresentation/Battle/WfSlider.cs
```
- 行尾：**LF 1443 / CRLF 0**（改前 1208 行、同为纯 LF）⇒ **没有整篇翻**。
- 唯一删除的一行是 `SetVisible` 里那句 `if (!on) { SetHover(false); SetPressed(false); }`（被替换成延长版）。

---

## 三、没查清的部分（⛔ 不猜，给「还差什么」）

1. **批处理（编辑模式）下 `Image` 是否登记进 `GraphicRegistry`** —— 本波不跑 Unity，**没实测**。
   若编辑模式下 `AddComponent` 不派 `OnEnable`（本仓已知坑），未来 `EventSystem.RaycastAll` 的自检
   可能看不到这三张图（A1 报告 §三·3 记的是同一族风险，它用 `SyncRaycasters` + `ForceUpdateCanvases` 补偿）。
   ⇒ **A3-b 的几何断言第一次真跑 `BattleScene.Run` 时，要先确认 `RaycastAll` 能打中 `slider_bg`**；
   打不中属于「编辑模式补偿」缺口，不是本件逻辑错。
2. **手柄抓取偏移（原版 `m_Offset`）没实现** —— 原版 `Slider.OnPointerDown` 按在**手柄上**时记
   `m_Offset`（拖动保持抓取偏移、值**不跳**）；本件一直就是「按下即跳到指针」。本步口径是
   **数值行为逐位不变** ⇒ 有意保留现状。要改成原版那一条是**另一步**（会改数值行为，需单独拍板）。
3. **多显示器相对坐标**：原版 `Slider.UpdateDrag` 先走
   `MultipleDisplayUtilities.GetRelativeMousePositionForDrag`；本件直接吃 `eventData.position`
   （**单显示器下逐位同值**）。多显示器不在本项目验收范围。
4. **真 Play 没跑**：按下/拖动/悬停是不是真的接到（尤其 `ScreenSpaceCamera` 画布下 `pressEventCamera`
   的实际取值）—— 本波不验，留给统一验收。
5. **`EventTrigger` 会拦截它身上的全部事件**（uGUI 源码注释原话）—— 本件只登记了 5 类委托，
   其余类型「收到但不做事」。当前滑块的父链上**没有别的 uGUI 事件处理器** ⇒ 无影响；
   以后若给面板根加「点空白关闭」的 uGUI 处理器，要复核这一条。
6. **`HudPiece.SetRaycastPadding`（A2 2026-10-19 加的）已经存在** —— 本件**没有**用它，
   而是直接写 `Image.raycastPadding`：本波与 A2 并行、`HudPiece.cs` 不在本件白名单且正在被人改，
   直接写 `Image` 可避免依赖一个在飞的文件。收口时可以把这一处收编进 `HudPiece` 的口。

---

## 四、typecheck 结果

```bash
cd d:/4/Unity/MyGame
TMPDIR=/tmp/wf_t09a3b bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 本次编译时**其它代理的在飞文件也都编得过**（没有需要「等 60 秒重跑」的情况）。
- `UNITY_EDITOR` 已在两份 rsp 里定义（现核 `/tmp/wf_t09a3b/wf_csc.rsp`）⇒ Editor-only 探针**确实被编到了**。

---

## 五、顺手发现（记账，⛔ 本波不改白名单外的东西）

1. **画布档的两张条图 / 手柄此前都是 `raycastTarget = false`** —— `HudCanvas` 的建件口默认就不吃射线
   （那是给装饰件的正确默认）。滑块是**第一个「整件都在面板里、又需要拖动」的复合件**，
   所以它必须自己把三张图打开；以后迁别的滑块族（如外壳侧 FPS 滑块若上画布）会撞同一条。
2. **`SettingsPanel.PointerFrame` 的手搓滑块路径仍然活着**（`SettingsPanel.cs:1981-1992`）——
   A2/A3 收口时必须删，否则「两套并存」虽然今天因同一算式而幂等，但它是 `T09` §四 R1 点名的那类形态。
3. **建议后续断言的读口**（本件已备好）：
   `PointerInputForTest != null` · `Track/Fill/HandleRaycastTargetForTest == true` ·
   `HandleRaycastPaddingForTest == (−31.2025, −25, −31.2025, −25)` · 用 `ExecuteEvents` 喂
   `PointerDown/Drag/PointerUp` 后看 `ValueForTest` 与 `DraggingForTest` 的翻转、以及 `OnChanged` 次数 == 1。
   🧨 **灭自证**：把 `SetupUguiInput` 里 `raycastTarget = true` 删掉 ⇒ 射线断言必须红；
   把 `OnUguiDrag` 的 `_dragging` 闸删掉 ⇒ 「未按下时 Drag 不该改值」那条必须红。
