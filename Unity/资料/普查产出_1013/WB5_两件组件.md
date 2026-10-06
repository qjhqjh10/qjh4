# WB5 · 两件组件（A463）

> 本件 = 写手代理 WB5。**没跑 Unity 自检**（简报口径：A 表清完再跑）· ⛔ 没动 git · ⛔ 没改两张正本。
> 类型检查（`TMPDIR=/tmp/wf_wb5 bash 工具/typecheck.sh`，共 5 遍）**运行时 0 / 编辑器 0**（最后一遍落地后）。

---

## 一、结论

| 件 | 结论 |
|---|---|
| **`TouchInputManager`**（TypeDefIndex 2299） | ✅ **做了**。新建 `Battle/TouchInputManager.cs`（**449 行**）：8 个静态属性 + `Instance` + `Toggle` + `Awake`/`Update`/`UpdateDrag` 逐句复刻。**并且真的接上了消费者**（`CombatCameraZoom.PollPointerSource` 改成纯转发）。 |
| **`BattleCameraSreenSize`**（TypeDefIndex 493） | ✅ **做了**。新建 `Battle/BattleCameraSreenSize.cs`（**445 行**）：4 个方法 + 2 个 `Action` + 8 个字段（含 ctor 值）逐句复刻；`vcam` 用 `CombatCameraZoom.virtualCameraLensShift` 当等价物；由 `CombatCameraZoom.Initialize` 建出来并接齐引用。 |
| **那 12 个消费点** | ✅ **逐个盘完**（§二）：**只有 1 个（`CombatCameraZoom`）真的接上了**；其余 12 处**我们都有自己的一条路**（`BattleDriver.WorldPointer` / `PointerHeld` / `AttackSelector` …），⛔ **不接**（接了就是两套输入源）。其中 **1 条查出一处真偏离**（`AttackSelector` 的入场动画驱动量，见 §九·1）。 |
| **`vcam` 那一格** | `CinemachineVirtualCamera` 的等价物 = **`CombatCameraZoom.virtualCameraLensShift`**（同一个存储、同一批读者），三条理由写在 `BattleCameraSreenSize.cs` 文件头那一整段。 |
| **断言** | ✅ **27 条**落在 `Editor/BattleScene.cs` 的 `★ A463` 段（`:10805-11241`）。每条带 **🧨 改坏法**；两条「前提」+ 两条「自证」。 |
| 🔴 **越出简报字面白名单的改动** | ⚠️ 我改了 **`Battle/CombatCameraZoom.cs`**（112/86）。它**不在简报的 ⛔ 名单里**（⛔ 名单只点了 `BattleDriver`/`Label`/`ScenarioBlendables`），而块3 §A463「要动的文件」**明确列了它**。理由：**不接上就只能是空壳**（见 §五·1）。改动是「转发 + 补建 + 就地订正旧注」，一条逻辑都没另写。 |

---

## 二、`TouchInputManager` 那 12 个消费点逐个盘

口径：**按静态字段 `Instance`（`DAT_1842bd2e8` + 静态块 `0xB8` + `0x30`）逐文件扫**（比按名字可靠）。
`grep -rl DAT_1842bd2e8 d:/2/tools/decomp_full/` = **37 个文件** —— 17 个是它自己、4 个是 `CombatCameraZoom`、
**剩下 12 个就是下面这张表**（与 块3 §A463 / H20 §五·4 的数逐一对上）。

| # | 消费者 | 读哪一格 | 原版用它做什么 | 我们这边等价的有没有 | 要接吗 |
|---|---|---|---|---|---|
| 1 | `BattleManager.GetBoardMousePos` | `TouchPosition`(`+0x0`) | `ScreenPointToRay(TouchPosition)` → `Physics.Raycast`（LayerMask）→ `Bounds.center` | ✅ `BattleDriver.WorldPointer()`（`BattleDriver.cs:9042-9050`） | ⛔ **不接**（两条路：原版射 3D 射线到战场物件；我们是 2D 布局换算，见 §八·1） |
| 2 | `BattleManager.GetBoardCardMousePos` | `TouchPosition` | 同上，但起点先按一个缩放后的偏移挪一下（`Vector2(+0.10,+0.14) × param_3`） | ✅ 同上 | ⛔ 不接（同上） |
| 3 | `BattleManager.GetMousePerspectivePos` | `TouchPosition` | **先判在不在屏幕内**（`0 ≤ x < Screen.width` 且 y 同）；在 ⇒ `ScreenToWorldPoint(x, y, −cam.pos.z)`；不在 ⇒ **`Vector2.zero`** | ✅ 有等价物，但**少了那道屏幕内外的守卫**（`Core/LayoutSpace.cs:93-100` 直接外推） | ⛔ 不接（**但那条守卫是真偏离** → §九·2） |
| 4 | `BattleManager.GetTargetInPos` | `TouchPosition` | 先 `Physics2D.GetRayIntersectionNonAlloc`（2D 层），失败再 `ScreenPointToRay` + `RaycastNonAlloc`（按层名）→ `GetComponentInParent<T>` | ✅ `BattleDriver` 的 `HitSlot` / `CardView.Contains(world)`（按世界矩形命中，不走物理） | ⛔ 不接（同上） |
| 5 | `BattleManager.ChoiceOfCardPlayer` | `Instance.Toggle(**true**)` | 打开选牌菜单那一刻**把输入层打开** | ⚠️ 我们**没有这一层**：`BattleDriver.HandleChoose` 自己判命中（`ClickedThisFrame` + `ChoosePanel.Hits`） | ⛔ 不接（我们不是「全局开/关」模型） |
| 6 | `BattleManager.StopDisplayCardDescription` | `Instance.Toggle(true)` | 停掉卡面说明那一刻把输入层打开 | ⚠️ 同上 | ⛔ 不接 |
| 7 | `BattleManager._CloseBattleDoors` 协程 | `Instance.Toggle(**false**)` | **结算门动画期间把输入层关掉**（`Toggle(false)` 后 `yield WaitForSeconds`） | ⚠️ 我们有 `BattleDoors`（`Battle/BattleDoors.cs`），但**没有「动画期间冻住输入」这一手** | ⛔ 不接（本账不扩面 → §九·3 记一笔） |
| 8 | `BattleSettingsWindow.Open` | `Instance.Toggle(**false**)` | 设置窗开着时**关掉输入层**（`Update` 停跑 ⇒ 8 格冻在上一帧 ⇒ 底下棋盘不吃指针） | ⚠️ 我们靠**逐控件**判：`SettingsPanel.PointerFrame` 开头 `if (!Visible) return false;`（`SettingsPanel.cs:409`） | ⛔ 不接（等价，机制不同） |
| 9 | `BattleSettingsWindow.CloseWindow` | `Instance.Toggle(true)` | 关窗恢复 | ⚠️ 同上（`Hide()`） | ⛔ 不接 |
| 10 | `BattleSettingsWindow.ESCPressed` | `Instance.Toggle(true)` | ESC 关窗恢复（看一个标志） | ⚠️ 同上 | ⛔ 不接 |
| 11 | `AttackTypesButtonsController.MoveButtons` | 🔴 `TouchDragDeltaViewport`(`+0x18/+0x1C`) | `accumulatedDrag += TouchDragDeltaViewport;` 然后 `t = clamp01(−accumulatedDrag.y ÷ accumulatedDragForMinDistance)` 当**三钮入场动画的进度**（`AttackTypesButtonsController__MoveButtons.c:22-34`） | ✅ `Battle/AttackSelector.cs`（整件都在），但**入场进度是按时间驱动**的（`AttackSelector.cs:99-103` 自己如实标了「判不出那到底是 −deltaTime 还是拖拽速度」） | ⛔ **本账不接**（跨文件：要动 `AttackSelector` + `BattleDriver` 的拖拽判据）→ **如实记进 §九·1（真偏离）** |
| 12 | `EnemyInfoTouch.Update` | `TouchPressed`(`+0x8`) | 没按着就把某个 GO `SetActive(false)`（敌人信息浮窗跟着按压隐去） | ❌ **没有等价物**（我们 HUD 里没有 `EnemyInfo` 那件浮窗；`BattleDriver` 里 `EnemyInfo` 只出现在坐标注释里） | ⛔ 不接（没有宿主；**如实记：这一格我们没有对应件** → §八·2） |

**合计：0 条要接。** 唯一「必须接」的是第 13 个消费者 —— `CombatCameraZoom`（块3 §A463 单列的 4 个文件）：
它读 `ScrollDelta`(`+0x20`) / `TouchPressedSecondary`(`+0x9`) / `TouchDragDelta`(`+0xC`)，**已接**（§五·1）。

> 📌 **这一格的最初判断（简报里的「最大未知量」）**：答案是 **「做了一半、另一半没有宿主」不成立** ——
> 原版那 12 处是**那一款游戏自己的输入链**；我们**每一处都有自己的一条路**，只是**驱动量/机制不同**。
> ⇒ 硬接过来 = 一个棋盘上两套输入源（工程红线「两处写同一条规则」）⇒ **正确做法就是只接 `CombatCameraZoom` 那一条**。

---

## 三、`BattleCameraSreenSize` 做了什么（`vcam` 的等价物是哪个 · 为什么等价）

### 3·1 它在原版里的**唯一生产路径**（全反编译实读）

```
Start()  ──注册──▶  Scaffold.Core.Events.Signal.Register<ScreenResolutionChangeSignal>(
                       new UIGenericEventCatcher.SourceDelegate(this, ResolutionHasChanged))
   │
   └─▶ 屏幕分辨率一变 ⇒ Signal 抬 ⇒ ResolutionHasChanged()
                                     └─▶ Initialize(instant: true)        ← 那件文件头 ①：整整两行
                                           ├─ zoom = combatCameraZoom.GetMaxZoomLevel(1.0f)      // DAT_1834b2bb8 = 1.0f
                                           ├─ cameraVerticalFramer.CalculateFraming(zoom, out newSensorSize, out desiredLensShift)
                                           ├─ instant?  OnCameraSensorSizeChanged(newSensorSize)        ← 抬
                                           │  否则      DOTween.To(boardCamera.sensorSize → newSensorSize, animTime).SetEase(InOutCubic)
                                           └─ instant?  OnCameraShiftChanged(new Vector2(0, desiredLensShift.y))  ← 抬
                                              否则      DOTween.To(vcam.m_Lens.LensShift → (0, y), animTime).SetEase(InOutCubic)
```

**两条硬事实**：
- 🔴 **`Initialize` 在全反编译里只有一个调用点**（`ResolutionHasChanged`），而它传的是 **`instant: true`**
  ⇒ **实况走的是「一步到位 + 抬 Action」那条**；`animTime = 3.0` 那条补间路**本 build 没有调用点**。
- 🔴 **`DoLensShift(float, bool)` 零调用点**（它的体被内联进 `Initialize` 后半段）—— 照原版**留成 public**，⛔ 不删。
- 🔴 **`instant` 那条路【不写 vcam】、补间那条路【不抬 Action】** —— 两个 `if/else` 只走一支（照抄，见断言 ㈤/㈥）。

### 3·2 `vcam` 那一格的等价物

| | 原版 | 我们 |
|---|---|---|
| 字段 | `+0x28 [SerializeField] CinemachineVirtualCamera vcam` | `[System.NonSerialized] public CombatCameraZoom vcamAsCombatCameraZoom` |
| 真碰的**唯一一格** | `m_Lens.LensShift`（对象偏移 **`+0xD0`/`+0xD4`**） | **`CombatCameraZoom.virtualCameraLensShift`** |
| 两个闭包 | `<Initialize>b__11_0` 读 / `b__11_1` 写 `boardCamera.sensorSize`<br>`<DoLensShift>b__12_0` 读 / `b__12_1` 写 `vcam+0xD0` | `boardCamera.sensorSize`（真 · `Camera`）<br>`VcamLensShift` get/set（= `b__12_0` / `b__12_1`） |
| 「虚拟镜头 → 真相机」那一跳 | Cinemachine 的 pipeline（每帧） | `CombatCameraZoom.ApplyVirtualCameraLensShift()`（**改成 public**，同一跳） |

**为什么等价（三条，写在 `BattleCameraSreenSize.cs` 文件头）**：
1. **同一个存储**：`CombatCameraZoom` 早把 `virtualCameraLensShift` 定义成 `vcam.m_Lens.LensShift` 的替身
   （它文件头 ⑥），量纲/语义/初值全一致（初值判据 = 同场景 `MonoBehaviour_4328.json` 的 `m_Lens.LensShift = (0.0, −0.205)`）。
2. **同一批读者**：`ApplyZoom` 从头到尾读它/写它；本件写它 ⇒ 下一帧 `ApplyZoom` 照样把它推给真相机。
3. **没有第二个候选**：我们这一档没有别的「虚拟镜头」概念；工程 16 个依赖里**没有 `com.unity.cinemachine`**。

**如实标（⛔ 不当原版）**：
- `DOTween.Kill(boardCamera)` / `DOTween.Kill(vcam)` 那两句**在原版是空操作**（`DOTween.To(getter,setter,…)` 造出来的补间
  `target == null`，而 `Kill(target)` 杀的是「target 等于它」的补间）⇒ **照抄**（原版行为 = 不杀），⛔ 不「顺手修好」。
- 「谁在抬 `ScreenResolutionChangeSignal`」**查不到**：`grep -rl ScreenResolutionChangeSignal decomp_full` = **0**
  （那个类只有 `TypeDefIndex 2714` 与一个共享空 ctor，发动者只经 `TypeInfo` 引用）
  ⇒ 等价物 A = **本件自己比 `Screen.width/height`**（`Tick()`），另留 `NotifyScreenResolutionChanged()` 显式抬法。
- 我们**加了 `OnDestroy`**（原版那 4 个方法里没有它）：那条信号是**静态**的，不摘就永远攥着一个已销毁的组件。

### 3·3 序列化值（13 场逐值相同）

判据 = `assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4208.json`（挂 GO `539`）：

| 字段 | ctor（`__.ctor.c` 直读） | 场景 | 我们取 |
|---|---|---|---|
| `sensorSizeXSmall` `+0x30` | `0x42140000` = **37.0** | 37.0 | 37.0 |
| `sensorSizeXBigScreen` `+0x34` | `0x42260000` = **41.5** | **41.0** | 🔴 **41.0（场景那一档）** |
| `animTime` `+0x38` | `0x40400000` = **3.0** | 3.0 | 3.0 |

> 🔴 **`sensorSizeXBigScreen` 是又一处「ctor ≠ 场景」**（铁律 5·c）—— 断言 ㈠ 专门咬它。
> 🔴 **但这两个 `sensorSize` 字段在本 build 里【一个方法都不读】**（我把那 9 个 `.c` 逐份看过：`+0x30`/`+0x34`
> 只在 `__.ctor.c` 里被**写**，四个方法体 + 四个闭包里**一次都没出现**）⇒ **保留字段 + 保留场景值**，
> 但**不假装它们在起作用**（本件行为里没有它们 —— 原版也没有）。⛔ 别拿它俩写个「小屏/大屏取 sensorSize」的分支，那是发明。

---

## 四、`TouchInputManager` 做了什么（8 个静态属性各自对应什么）

判据 = `TouchInputManager__{Awake, Update, UpdateDrag, Toggle}.c` + `dump.cs` 的静态字段偏移
+ `MonoBehaviour_4011.json`（`forceMobileInput: 0`）。

| 静态格 | 偏移 | `Update` 里由哪一句写 | 我们这一档的来源（等价物 A） |
|---|---|---|---|
| `TouchPosition` | `+0x00` | ⑧ `Input.mousePosition`（无条件）；触摸路里再被覆盖成 `touch.position` / 两指中点 | `Mouse.current.position.ReadValue()` |
| `TouchPressed` | `+0x08` | ⑥ `Input.GetMouseButton(0)`；单指路里再置 `true` | `mouse.leftButton.isPressed` |
| `TouchPressedSecondary` | `+0x09` | ① 先置 `false`、⑦ 再 `Input.GetMouseButton(1)`；双指路里置 `true` | `mouse.rightButton.isPressed` |
| `TouchDragDelta` | `+0x0C` | ③ 先清 `Vector2.zero`；`UpdateDrag` 里 = 差分；单指路里 = **`Touch.deltaPosition`** | 同 |
| `IsDragging` | `+0x14` | ⑤ 先 `false`；`UpdateDrag` 里 = `0 < \|delta\|` | 同 |
| `TouchDragDeltaViewport` | `+0x18` | ④ 先清；`UpdateDrag`/单指路里 = 各轴 ÷ `Screen.width`/`Screen.height` | 同 |
| `ScrollDelta` | `+0x20` | ② = `mouseScrollDelta.`**`y`**；双指路里 = **`−(本帧两指距离 − 上一帧)`** | `.y ÷ 120`（量纲换算，见下） |
| `TwoFingerMidPoint` | `+0x24` | 双指路 = `(p0 + p1) × 0.5` | 同 |
| `Instance` | `+0x30` | `Awake`：`if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this;` | 同 + `Ensure()`（等价物 B） |

**三条 `Update` 里的顺序与门（照抄）**：
1. 桌面路在**前**：`if (!forceMobileInput && !isMobilePlatform) { if (TouchPressed || TouchPressedSecondary) UpdateDrag(TouchPosition); }`
2. 触摸路在**后**：`if (isMobilePlatform || forceMobileInput) { 1 指 / 2 指 / else }`
   ⇒ 🔴 **`forceMobileInput = true` 时桌面路【整段被跳过】**（不是「两条都走」）—— 断言 ⑤ 咬它。
3. 差分基准重置（`.c` 里是个 `goto` 汇合点）：`GetMouseButtonDown(0/1)` **或** `触摸 0 的 phase == Began` ⇒ `lastTouchPosition = TouchPosition`。

**🔴 本件按指令流取回的两处（`.c` 里看不出来，照抄会静默错）**：

| `.c` 里长这样 | 实际是什么 | 判据 | 照抄的后果 |
|---|---|---|---|
| `FUN_180789a80(0)`（被判成「void 调用」） | **`get_TwoFingerMidPoint()`** | RVA `0x789A80` 的指令流：读 `[class+0xB8]` 的 `+0x24`/`+0x28`，`unpcklps` 组 `Vector2` 返回 | 两指 Begin 那一帧的基准重置**丢掉** |
| `FUN_180789ad0(x, 0)`（两处） | **`set_ScrollDelta(x)`** | RVA `0x789AD0` 的指令流：`movaps xmm6,xmm0`（收 float）→ `movss [rcx+0x20],xmm6`（**不返回值**） | **整个捏合缩放丢掉**（`ScrollDelta` 恒 0） |
| `…get_Count`（`WebSocketSharp…`） | **`Touch.get_phase`** | 拿返回值与 `0` 比 = `TouchPhase.Began` | — |
| `…NativeArray<Vector2>.Enumerator…get_Current` | **`Touch.get_deltaPosition`** | 指令流 `0x79C598` 的 `call 0x183a330`；`all_methods.txt` 里那是 `UnityEngine.Touch$$get_deltaPosition` | **把绝对位置当位移**（静默错） |
| `System.Nullable<Vector2>…GetValueOrDefault` | **`Touch.get_position`** | 同上（同一段的 `0x79C4E9` / `0x79C543` / `0x79C693`） | — |

**等价物 A（输入源）的逐格对应**：legacy `Input.*` **不可用**（`ProjectSettings.asset:932 activeInputHandler: 1`
⇒ 只有 Input System 包），⇒ `ReadRaw()` 一处分判：

| legacy | 我们 |
|---|---|
| `Input.mousePosition` | `Mouse.current.position.ReadValue()`（同为**屏幕像素、原点左下**） |
| `Input.mouseScrollDelta.y` | `Mouse.current.scroll.ReadValue().y ÷ 120`（Windows 一格 legacy 是 ±1、新输入系统是 ±120 —— 同源口径 `Shell/MenuScroll.cs:200`） |
| `Input.GetMouseButton(0/1)` | `mouse.leftButton/rightButton.isPressed` |
| `Input.GetMouseButtonDown(0/1)` | `….wasPressedThisFrame` |
| `Input.touchCount` | `Touchscreen.current.touches` 里 `press.isPressed` 的个数 |
| `Input.GetTouch(i).{position,deltaPosition,phase}` | `Touchscreen.current.touches[i].{position,delta,phase}` —— 🔴 **两套 `TouchPhase` 枚举值不同序**（legacy `Began=0`；Input System `Began=1`）⇒ **显式映射**，⛔ 不是强转 |

**等价物 B（惰性自建）**：原版那件是**摆在战场场景里的一件组件**（`MonoBehaviour_4011.json`，挂 GO `712`）。
我们的战场是**自己搭的树**，没有那件 ⇒ `Ensure()` 第一次有人读它时建一份（幂等）。
⚠️ `Instance` 是静态的 ⇒ 指向被销毁对象时 Unity 的「假 null」会让 `Instance != null` 求值成 `false` ⇒ 会再建一份（正是要的）。

**等价物 C（自检口）**：`RawOverride`（钉一帧的原始读数）· `MobilePlatformOverride`（钉 `Application.isMobilePlatform`）·
`Tick()`（= `Update` 的体，显式推一帧）· `UpdateDragForTest` / `LastTouchPositionForTest` / `LastTwoFingerDistanceForTest` /
`ForceMobileInputForTest` / `ResetStaticsForTest` / `Bootstrap()`。**⛔ 生产路径一律不碰。**

> 🔴 **`Awake` 拆出 `Bootstrap()` 的理由**：批处理下 `AddComponent` 跑不跑 `Awake` **本工程没有定论**
> （`Editor/BattleScene.cs` 那条 A321 订正）⇒ 一律**显式补一次**（同族先例 = 那条订正的「一律显式补一次 `Build()`」）。
> `Awake` 现在只有一句转发。⛔ 不是第二条实现。

---

## 五、改动清单（`文件:行`）

> 行号 = **本件改完之后的现读值**。

### 5·1 🔴 越出简报**字面**白名单的一处（理由见 §一 末行）

| # | 文件:行 | 为什么 |
|---|---|---|
| 1 | `Battle/CombatCameraZoom.cs:277-286` | `PollPointerSource()` **改成纯转发**：`TouchInputManager.Ensure()` + 读那三个静态格。原来它**自己读一遍鼠标**（那是那件组件缺席时的替身）⇒ 现在那件有了 ⇒ 全工程「读鼠标」只剩 `TouchInputManager.ReadRaw` **一处**（工程红线「两处写同一条规则」）。**同时删掉两个只服务于旧写法的字段**（`_lastPointerPosition` / `_pointerInited`）。<br>⚠️ **时序**：生产路径 `Update`（那件）→ `LateUpdate`（本件）⇒ 与「两个组件的 Update/LateUpdate 次序」等价。⛔ 这里**刻意不主动 `Tick` 那件**（一帧推两次会把 `UpdateDrag` 的差分基准踩平 ⇒ 拖拽恒 0）。 |
| 2 | `Battle/CombatCameraZoom.cs:347-362` | 🆕 `EnsureScreenSizeComponent()`：建 `BattleCameraSreenSize` + 接齐四格引用 + 订它那两个 `Action` + `RegisterResolutionSignal()`。原版那件是**场景里序列化好的**；我们在这条链「引用齐了」的地方补建。 |
| 3 | `Battle/CombatCameraZoom.cs:478` | `Initialize()` 里调上面那一句 —— ⚠️ **必须在末尾那三格 `originalLensShift/targetLensShift/targetOriginalLensShift` 赋值之前**（原版顺序是后手覆盖）。 |
| 4 | `Battle/CombatCameraZoom.cs:364-389` | `SubscribeScreenSizeSource` / `UnsubscribeScreenSizeSource` 改订**真组件**的两个 `Action`（= 原版 `Awake`/`OnDestroy` 四段 `Delegate.Combine`/`Remove`）；删掉原来那句「那件组件我们没做 ⇒ 没有生产者」的出声（**已不成立**）。 |
| 5 | `Battle/CombatCameraZoom.cs:840` | `ApplyVirtualCameraLensShift()` **private → public**：本件那条补间的 setter 写完虚拟位移也要有**同一跳**推给真相机（原版那一下在 Cinemachine 的 pipeline 里）。⛔ 全类写 `targetCamera.lensShift` 仍然只此一处。 |
| 6 | `Battle/CombatCameraZoom.cs:144-152` | `battleCameraScreenSize` 字段类型 `MonoBehaviour → BattleCameraSreenSize`（原版 `+0x38`），注释就地订正。 |
| 7 | `Battle/CombatCameraZoom.cs:71-76`（文件头 ⑦）· `:95-102`（文件头 D） | **铁律 5 的就地订正**：⑦ 原来写「我们也没有那件组件 ⇒ 那三格从新输入系统现取」、D 原来写「`BattleCameraSreenSize` 整件没做 ⇒ 没有生产者（触发源缺）」——**两条都已不成立**，改成 ✅ + 保留更正痕。 |
| 8 | `Battle/CombatCameraZoom.cs:259-261` | `ScrollUnitsPerNotch` 改成 `= TouchInputManager.ScrollUnitsPerNotch`（⚠️ 常量转发 —— 见断言 ⑦ 那条如实标）。 |
| 9 | `Battle/CombatCameraZoom.cs:250-253` · `:406-418` · `:937-944` | 三处注释随改动订正（输入源那一节标题 · `Tick`/`TickInjected` 的时序说明 · `OnCameraShiftChanged` 的触发源）。`_screenSizeSourceNoted` 字段删（已不用）。 |

### 5·2 ✅ 白名单内

| # | 文件:行 | 为什么 |
|---|---|---|
| 10 | 🆕 `Battle/TouchInputManager.cs`（**449 行**，LF） | 整件。文件头 = 判据链 + 三处等价物 + 两处「没做/做不到」的如实标。 |
| 11 | 🆕 `Battle/BattleCameraSreenSize.cs`（**445 行**，LF） | 整件。文件头 = 判据链 + `vcam` 等价物那一整段 + 三处等价物 + 三处如实标（含 `DOTween.Kill` 是空操作）。 |
| 12 | `Editor/BattleScene.cs:10805-11241`（**只加了这一段**） | `★ A463` 断言段。落点 = **A514 段之后、整轮收尾之前**（与 A514 段自己那条「落点必须在这」的理由一致：收尾会把 `SmallScreenUI`/`AutoZoom` 两格还回去，本段不碰它们）。 |

**行尾**（python **二进制**读）：`BattleScene.cs` **CRLF 12683 / bareLF 0** · `CombatCameraZoom.cs` **LF 988** ·
两个新文件 **LF 449 / 445** —— **一处没翻**。`git diff --numstat`：`CombatCameraZoom.cs` **112/86** ·
`BattleScene.cs` **716/2**（含 W-B1/W-B2 前几轮的 279 行）。
⚠️ 两个新 `.cs` 目前是 **untracked 且没有 `.meta`** —— 下一次 Unity 导入时会自动生成（本件⛔不许跑 Unity）。

---

## 六、断言清单（断什么 · 期望值来源 · 改坏法 · 落点）

落点 = `Editor/BattleScene.cs` 的 `★ A463` 段（`:10805-11241`），共 **27 条**（2 条「前提」+ 2 条「自证」）。
**两件各起一个临时探针**（一个 GO + 一个相机 rig），收工 `DestroyImmediate` + 把静态事件/静态格还回去并**回读自证**。

### 六·一 `TouchInputManager`（15 条）

| # | 断什么 | 期望值来源 | 🧨 改坏法 |
|---|---|---|---|
| 1 | （前提）清静态格后 `AddComponent` ⇒ `Instance` 就是它 | 原版 `Awake` | —— （消息里如实带出「`Awake` 自己跑过没有」，没跑就显式补 `Bootstrap()`） |
| 2 | 第二个实例**抢不走** `Instance` | 原版 `Awake` 的 `Destroy(gameObject); return;` | 删那两行守卫 ⇒ 红（⚠️ 原版那句 `Object.Destroy` 批处理下不生效 ⇒ 只断 `Instance`，如实标） |
| 3 | `Toggle(false/true)` = `enabled` 翻动 | 原版一句 `Behaviour.set_enabled` | 把 `Toggle` 写成「顺带清 8 格」（⚠️ 这条本身验不出来，如实标） |
| 4 | （前提）`Screen.width != Screen.height` | —— | 它是 ③ 那条可分性的前提 |
| 5 | `UpdateDrag` = 差分 + `IsDragging` + **viewport 那一除** + 基准前移 | 断言里独立算 `(30/W, 40/H)` | 两个分母对调 ⇒ 红 |
| 6 | 原地不动 ⇒ `IsDragging == false` | 原版 `0 < \|delta\|` | —— |
| 7 | 桌面路：左键按着 ⇒ `TouchPressed` 真 + delta = 指针 − 基准 | 原版第 ⑩ 句 | —— |
| 8 | **没人按键 ⇒ 不更新拖拽**（`TouchPosition` 照更新、delta 归零） | 原版那道 `if (TouchPressed \|\| TouchPressedSecondary)` | 删那道门 ⇒ 红 |
| 9 | `forceMobileInput` 真 ⇒ **桌面路整段跳过** | 原版两层 `if` 的嵌套结构 | 去掉 `!forceMobileInput` ⇒ 红 |
| 10 | `ScrollDelta` = **`.y`**（喂 x=5,y=7 ⇒ 得 7） | 指令流 `0x79BDDC movss xmm0,[rbp+0x124]` | 换成 `.x` ⇒ 得 5 ⇒ 红 |
| 11 | 滚轮量纲 = 120，且两处口径一致 | `MenuScroll.cs:200` 同源 | 任一处改成别的数 ⇒ 红（⚠️ 分不出「转发」与「各写一份」，如实标） |
| 12 | 双指张开 ⇒ `ScrollDelta` = **−(距离差)** = −3 · 中点 (1.5,2) · `TouchPressedSecondary` 真 · 基准更新成 5 | 原版双指段逐句 | ① 符号取反 / `PinchSign` 改 +1 ⇒ 红；② **照 `.c` 把它当「void 调用」丢掉 ⇒ 得 0 ⇒ 红** |
| 13 | 基准是 0（第一帧双指）⇒ **不产** `ScrollDelta` | 原版 `if (0 < lastTwoFingerDistance)` | 删那道门 ⇒ 红 |
| 14 | 单指 ⇒ `TouchDragDelta` = **`Touch.deltaPosition`**（不是 `position`） | 指令流 `0x79C598 call 0x183a330` = `Touch$$get_deltaPosition` | 换成 `Touch0Pos` ⇒ 红（**这是本件最大的那处反编译坑**） |
| 15 | （自证）静态格 + 两个注入口全部还回出厂、`Instance` 归 null | —— | 本段收工不还 ⇒ 红 |

### 六·二 `BattleCameraSreenSize`（12 条）

| # | 断什么 | 期望值来源 | 🧨 改坏法 |
|---|---|---|---|
| 16 | 三个序列化值取**场景那一档**（37 / **41** / 3） | `MonoBehaviour_4208.json` | 照 ctor 抄（`sensorSizeXBigScreen = 41.5`）⇒ 红 |
| 17 | 四格引用逐格接齐 **且** `CombatCameraZoom` 真订到那两个 `Action` 上 | 原版 `Awake` 两段 `Delegate.Combine` | 少接一格 / 少订一条 ⇒ 红（**这条是「有/没有」那一档**） |
| 18 | （前提）取景器把 `lensShift.x = 0.5` 原样带出来 | 断言里独立算 | 它是 ⑲ 可分性的前提（不摆非 0 值，⑲ 恒真） |
| 19 | `Initialize(instant: true)` 抬 `OnCameraShiftChanged`，且 **shift.x 被强制成 0** | 原版 `(ulonglong)y << 0x20`；`.y` 用断言里独立算的 `CalculateFraming` 结果 | 写成 `new Vector2(y, y)` ⇒ 红 |
| 20 | 同一条路抬 `OnCameraSensorSizeChanged(newSensorSize)`，载荷 = 取景器给的 | 同上 | 删那句 `Invoke` ⇒ 红 |
| 21 | **`ResolutionHasChanged()` = `Initialize(instant: true)`**（抬一次信号 ⇒ 计数 +1 **且** Action 再抬一次） | 原版那个方法整整两行；`Signal.Register<ScreenResolutionChangeSignal>` | 改成 `Initialize(false)` ⇒ Action 不涨 ⇒ 红 |
| 22 | （负例）`instant: false` ⇒ **不抬 Action**、但**真建了补间** | 原版 `if/else` 两支只走一支 | 两支写反 ⇒ 红 |
| 23 | 补间终值 = `(0, want.y)` **且推到了真相机**（起点摆 `(0.9,0.9)` ⇒ 可分） | 原版 `DOTween.To(…, shift, animTime).SetEase(InOutCubic)` | setter 里删 `ApplyVirtualCameraLensShift()` ⇒ 后半段红 |
| 24 | `VcamLensShift`（= `b__12_0`/`b__12_1`）读写都落在 `virtualCameraLensShift`、且推到真相机 | 原版 `vcam+0xD0` | setter 少写一半 ⇒ 红 |
| 25 | `DoLensShift(y, true)` 抬的也是 `(0, y)` | 原版两份同样的体 | 写成 `new Vector2(y, 0)` ⇒ 红 |
| 26 | **等价物 A**：屏宽高没变 ⇒ 不抬；变一格 ⇒ 抬一次 | ⚠️ **不是原版行为**（原版那条信号源查不到） | 删 `Tick` 里那句 `NotifyScreenResolutionChanged()` ⇒ 红 |
| 27 | （自证）那条静态分辨率信号上收工时**一个订阅者都不剩** | —— | 不摘（`UnregisterResolutionSignal` 没生效）⇒ 红 |

> ⛔ **没造的（如实）**：① **双指/单指那两条移动端路在真机上的手感**（本段只用 `RawOverride` 走通了逻辑）；
> ② **`.y` 是从新输入系统哪个字段来的那一跳**（批处理里 `Mouse.current == null` ⇒ 永远走注入那条）—— 只有代码审查 / 真 Play 能验；
> ③ **`Update()` 的屏宽高比对在真分辨率变化下那一趟**（本段钉的是 `SetScreenSizeBaselineForTest` 那条纯判据）。

---

## 七、没做的 / 出声的地方（⛔ 不许静默）

| 件 | 状态 | 出声在哪 |
|---|---|---|
| `BattleCameraSreenSize.Initialize` 的前置引用没接齐 | **早退 + `LogError`**（原版是 `FUN_1803f47a0` 直接抛 NRE） | `BattleCameraSreenSize.cs` 的 `Initialize`（点名是哪一个引用缺） |
| 取景器算不出来（`CalculateFraming` 回 false） | **早退 + `LogWarning`** | 同处（原因由 `CombatAutoZoom` 那边自己出过一次声） |
| `CombatCameraZoom` 拿不到取景器 | 保持 H20 原有的 `LogError` | `CombatCameraZoom.Initialize` |
| `DOTween.To` 在批处理里可能不推进 | **不是失败**：`CompleteTweensForTest()` 用 `SetUpdate(Manual)` + `ManualUpdate` + `Complete()` 推到终点（判据 = `Editor/DOTweenSmokeTest.cs`） | 断言 ㈥ 直接断终值（推不动就红，⛔ 不静默放过） |
| `TouchInputManager` 的两个 `sensorSize` 字段 | **本 build 无人读** —— 保留字段与场景值，但**不假装在起作用** | 文件头 D + 断言 ㈠ 的消息里如实写 |
| `DoLensShift` 零调用点 | **照原版留成 public**，⛔ 不删 | 文件头 E + 断言 ㉕ |

---

## 八、没查清（如实写，⛔ 不许猜）

1. **原版那 12 个消费点「原版到底用哪一格」我逐条读了，但「我们的等价物与它在数值上等价到什么程度」只对第 3 条做完了**（见 §九·2 —— 那条我们**少了屏幕内外的守卫**）。其余 3~4 条走的是**完全不同的机制**（我们 2D / 原版 3D 物理射线）⇒ **不是「差一个常量」那种**，逐值比对**没有意义**，也就没有逐值查。
2. **`EnemyInfoTouch` 那一格我们没有对应件**（我们 HUD 里没有 `EnemyInfo` 那件浮窗）⇒ 只能记「原版有、我们没有」，**没有宿主可接**。
3. **「谁在抬 `ScreenResolutionChangeSignal`」查不到**（全反编译 0 命中；那个类只经 `TypeInfo` 被引用）。⇒ 我们的等价物 A（自己比屏宽高）是**猜一个最接近的语义**，如实标在文件头与断言 ㉖ 的文案里。
4. **`DOTween.Kill(boardCamera)` 是空操作这一条**：我按「`DOTween.To(getter,setter,…)` 的 `target` 是 null」推的，**没有在运行期量过**（原版那个 build 跑不起来）⇒ 我们**照抄它**（原版行为 = 不杀），所以**无论这个推断对不对，行为都与原版一致**。
5. **`awakeRan`（批处理下 `AddComponent` 跑不跑 `Awake`）** 本工程**仍未坐实** ⇒ 本件按 A321 订正的惯例**显式补一次**，并把「跑没跑」如实打进断言消息里（那一条**不判红**）。

---

## 九、顺手发现（⛔ 别自己顺手改）

1. 🔴 **`AttackSelector` 的三钮入场动画是【真偏离】，而本件刚好把判据补齐了。**
   原版（`AttackTypesButtonsController__MoveButtons.c:22-34` 逐句）：
   `accumulatedDrag += TouchInputManager.TouchDragDeltaViewport;` → `t = clamp01(−accumulatedDrag.y ÷ accumulatedDragForMinDistance(+0x30))`。
   **驱动量是「累计的视口拖拽」**（`+0x50`/`+0x54`）。我们 `Battle/AttackSelector.cs:99-103` 自己写着
   「**判不出那到底是 −deltaTime 还是「拖拽速度」** ⇒ 按时间驱动，如实标注」—— 现在**判据有了**：
   就是 `TouchDragDeltaViewport`（**速度**那一档，不是 `−deltaTime`）。
   ⇒ **建议单开一笔账**（要动 `AttackSelector.cs` + `BattleDriver` 的拖拽判据 ⇒ 跨文件、且与 A462 那族相邻）。
   ⛔ 我**没改**（不在本账白名单）。
2. 🔴 **我们少了 `GetMousePerspectivePos` 那道「在不在屏幕内」的守卫**（真偏离，小）：
   原版 `BattleManager__GetMousePerspectivePos.c:36-38`：`0 ≤ x < Screen.width` 且 `0 ≤ y < Screen.height` 才算，
   **否则返回 `Vector2.zero`**。我们的 `BattleDriver.PointerScreen()`（`BattleDriver.cs:9047-9052`）**没有这道判**，
   而 `Core/LayoutSpace.cs:93-100` 的 `ScreenToWorld` 会**外推**（屏幕外的点给出界外的世界坐标）。
   ⇒ 指针拖到窗口外时，我们的 `WorldPointer()` 会指向一个界外点，原版会指向 `(0,0)`。
   ⚠️ **要动的是 `BattleDriver` / `LayoutSpace`（本账白名单外）** ⇒ 只报不改。
3. 🟡 **`BattleManager._CloseBattleDoors` 那条「结算门动画期间冻住输入」我们没有**（`Instance.Toggle(false)` + `yield WaitForSeconds`）。
   我们有 `BattleDoors`，但**没有「动画期间不吃指针」这一手**（现在靠各控件自己判）。
   ⇒ 属于「`Toggle` 这个总开关的语义」那一族，**同一笔账**（要接得先决定我们这一档的输入层怎么表达「开/关」）。
4. 🟡 **本件发现 `CombatCameraZoom.ApplyZoom` 里 `originalLensShift`（`+0x90`）追完 `targetOriginalLensShift`（`+0x98`）之后**
   **不再被任何地方读**（`CombatCameraZoom__ApplyZoom.c` 里 `+0x90`/`+0x98` 只有三处：读 `+0x90`、读 `+0x98`、写 `+0x90`）。
   ⇒ 原版`BattleCameraSreenSize.OnCameraShiftChanged` 喂进去的那一格，**在 `ApplyZoom` 里是「写-only」的**。
   **照原版**（我们的实现也是写-only）—— 但这解释了为什么「分辨率变化」在画面上看不出变化。⛔ 别「顺手」把它接到 `smoothed` 上。
5. 🟡 **`TouchInputManager.Update` 的第一句（`TouchPressedSecondary = false`）在原版是【冗余】的**
   （第 ⑦ 句立刻用 `GetMouseButton(1)` 覆盖它）—— 我**照抄**了（⛔ 删了行为不变，但对不上原版）。
6. 🟡 **`TouchInputManager.Toggle` 在 `BattleSettingsWindow.Open/CloseWindow/ESCPressed` 里的语义是「停摆」不是「清零」**
   —— 停跑 `Update` ⇒ 8 格**冻在上一帧的值**上。⛔ 别「顺手」在 `Toggle(false)` 里加清零（那会把原版行为改掉）。
7. 🟡 **`sensorSizeXSmall`(37) / `sensorSizeXBigScreen`(41) 在本 build 里是死字段**（一个方法都不读，见 §三·3）。
   顺手核了一下：`grep -rl BattleCameraSreenSize decomp_full` = **9 个文件、全是以它自己命名的**（+ `CombatCameraZoom__Awake/OnDestroy` 那两处经字段偏移引用）
   ⇒ **全反编译里没有第二个类读它**。⛔ 别去给它俩找个用途。
8. 🟡 **`DOTween` 在批处理里要「手动推」**：`Editor/DOTweenSmokeTest.cs` 的文件头给了那套
   （`SetUpdate(UpdateType.Manual)` + `DOTween.ManualUpdate(dt)`），并提醒**判据用位置、不要用 `IsComplete()`**
   （autoKill 回收后它反而返回 false —— 他们踩过）。本件按这套走。

---

## 十、类型检查结果

| 轮次 | 结果 |
|---|---|
| 第 1 遍（写完两个新文件） | 运行时报 **11 条 CS1061** —— 全是**我自己的**：`SetEase`/`IsActive`/`Complete`/`Kill` 都是**扩展方法**，而我用的是全限定名 ⇒ 找不到。**当场加 `using DG.Tweening;` 修掉**（并在注释里写明为什么必须 `using`）。编辑器那条 `CS0006`（找不到 `WFCheck.dll`）是它的级联。 |
| 第 2 遍（修完 `using`） | **运行时 0 / 编辑器 0** ✅ |
| 第 3 遍（加完 `★ A463` 断言段） | **运行时 0 / 编辑器 0** ✅ |
| 第 4 遍（加 `Bootstrap()` / `OnDestroy` / `TweenCreatedForTest`） | **运行时 0 / 编辑器 0** ✅ |
| 第 5 遍（最后一遍，全部落地后） | **运行时 0 / 编辑器 0** ✅ |

⚠️ 五遍里**没有一遍**出现「错误集中在**别人的在飞文件**上」那一档（简报里点名要如实记的那一档）——
每一遍的红都只在我自己的文件上，且都当场修掉了。
⚠️ **跑类型检查期间我没有再改 `.cs`**（除第 1 遍那次修 `using` 之外，其余都是「改完 → 再跑」）。
⚠️ **Unity 自检一条都没跑**（简报口径：A 表清完再跑）。本件落点覆盖 **`BattleScene.Run`** ⇒ 同步点跑这一条。

