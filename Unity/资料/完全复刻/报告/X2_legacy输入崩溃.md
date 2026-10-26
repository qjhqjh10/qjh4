# X2 · 生产代码读 legacy `UnityEngine.Input` 的运行时崩溃

> 执行代理 `X2` · 2026-10-26 · 第八十三会话「完全复刻」
> 范围：**只改「legacy `Input` 调用」这一族**。⛔ 未跑 Unity、未动 git、未碰 `Scenes/`、未动任何 `账/*.tsv`。
>
> **自检读数（本代理跑的）**：`TMPDIR=/tmp/wf_x2 bash d:/4/Unity/工具/typecheck.sh`
> ⇒ **运行时错误数 0 / 编辑器错误数 0**。
> `check_ledger_points.py`（只读，2026-10-26 现读）：`A 3221 · 通过 3166（告警 130）· 漂移 21 · 失败 34` ——
> 逐条核过，**没有一条落在 `CollectionWindow.cs` / `PopUpGameWindow.cs`**（21+34 全在 `Shell/SceneInitializer.cs` 与
> `WarpforgeVFX/Runtime/WFEffectExtras.cs` = 本批**另一个代理正在拆类**的那两个文件，按简报**我没碰**）
> ⇒ 本次改动**没有引入账漂移**。

---

## ① 崩溃点 + 根因一句话

**崩溃点**（一手证据 = `wf_CollectionScene.Run.log:151611-151616`）：

```
InvalidOperationException: You are trying to read Input using the UnityEngine.Input class,
but you have switched active Input handling to Input System package in Player Settings.
  at CardPresentation.CollectionWindow.get_HoverSuppressed () in ...\Shell\CollectionWindow.cs:5043
  at CardPresentation.CollectionWindow+<>c__DisplayClass425_0.<AttachCardHover>b__0 () in ...\Shell\CollectionWindow.cs:5052
  at CollectionScene.RunCore () in ...\Editor\CollectionScene.cs:8258
```

**根因一句话**：`ProjectSettings.asset:936` 是 `activeInputHandler: 1`（**只启用 Input System 包**），
而 `CollectionWindow.HoverSuppressed` 的 getter 里写着 `Input.GetMouseButton(0)` ——
**legacy `UnityEngine.Input` 在这个配置下一读就抛**，因此 `CollectionScene.Run` 在中途异常终止，
后面一大段断言根本没跑到；**玩家侧**鼠标一移进卡格（`onEnter`）同样会抛。

---

## ② 改成什么（输入源 / 为什么等价 / 批处理下为什么不抛）

### 输入源 = 新输入系统，**照抄本工程已有的做法**（⛔ 没有新造一套）

先读了 `Battle/TouchInputManager.cs`（文件头 A 已写明「输入源换了」）：
它的 `TouchPressed` 等属性是**每帧 `Update()` / `Tick()` pump 出来的静态格**
（`ReadRaw()` → `Mouse.current` / `Touchscreen.current`），**在收藏窗这种没有该组件的场景里永不刷新** ⇒
直接拿它当读数口 = 「悬停抑制在真机上永远不生效」（虽然不崩，但是**行为回归**）。⇒ 改用**别的生产代码共用的那条口径**：

| 现有实现 | 位置 | 写法 |
|---|---|---|
| `BattleDriver.PointerHeldRaw()` | `Battle/BattleDriver.cs:14487-14491` | `Mouse.current.leftButton.isPressed` **或** `Touchscreen.current.primaryTouch.press.isPressed`（各自先判 `!= null`） |
| `BattleDriver.PointerScreen()` | `Battle/BattleDriver.cs:31507-31512` | `Mouse.current` → `Touchscreen.current` → `Vector2.zero` |
| `CardInteraction.PointerScreen()` | `Hand/CardInteraction.cs:1626-1630` | 同上（第二份副本） |

### 改动 1 — `Shell/CollectionWindow.cs`（2 处）

- `:5043` getter：`_hoverSuppressedForTest || Input.GetMouseButton(0)` → `_hoverSuppressedForTest || PointerHeldForHover()`
- `:6383-6404` **类尾新增** `static bool PointerHeldForHover()`（doc 6383-6396 · 方法 6397-6404）
  - 体 = `PointerHeldRaw()` 的**逐字口径**（`Mouse.current.leftButton.isPressed` → `Touchscreen.current.primaryTouch.press.isPressed`），
    外加 **`if (!Application.isPlaying) return false;`** 这道闸；
  - 用**全限定名** `UnityEngine.InputSystem.Mouse.current`（**不加 `using` 行** —— 见下面「为什么零行数变化」）。

**语义为什么等价**：原版是 `CollectionItem.OnPointerEnter` 第一句
`if (eventData.pointerDrag != null) return;`（**拖拽进行中不亮悬停**）。
本工程没有全局拖拽标志（`Core/DraggableController.Dragging` 是实例字段），
所以沿用它原来的等价物 = **鼠标主键按住**（任何拖拽期间必然按住）；本次**只换输入源、不动语义**。
`_hoverSuppressedForTest` 仍在 OR 的最左边 ⇒ 自检强开口语义不变。

**批处理/编辑模式为什么不抛**：
- `Application.isPlaying == false` ⇒ 直接 `return false`（批处理没有帧循环，设备读数无意义）；
- 即便走到设备那一支，`Mouse.current` / `Touchscreen.current` 为 `null` 时**先判空再读**，
  `isPressed` 本身**任何环境下都不抛**（抛的只有 legacy `Input` 类）。
- 因此 `CollectionScene.Run`（`Application.isPlaying == false`）里 `HoverSuppressed` **恒等于** `_hoverSuppressedForTest`（默认 `false`）
  ⇒ `CollectionScene.cs:8258` 的 `hb0.onEnter()` **确定性地**点亮悬停，后面那一组描边断言恢复可跑。
- ⛔ 没有 `try/catch`（本工程红线：不许静默失败）。

### 改动 2 — `Shell/PopUpGameWindow.cs`（2 处，同类真调用）

`CheckLinkText()` 里 `Vector3 m = Input.mousePosition;` —— 真调用，**点带 `<link>` 的弹窗正文必抛**（同一条根因）。

- `:547` → `Vector3 m = PointerScreenForLink();`
- `:553-565` **新增** `static Vector3 PointerScreenForLink()`（doc 553-558 · 方法 559-565）：`Mouse.current` → `Touchscreen.current` → `Vector3.zero`
  （= `BattleDriver.PointerScreen()` 的同一口径；批处理无设备 ⇒ `(0,0)`，与原注释「批处理里恒 `(0,0)`」一致）。
- `:540` 的 doc 就地订正（原文写「批处理里 `Input.mousePosition` 恒为 `(0,0)`」）。

### 为什么「零行数变化」（铁律 18·b / `check_ledger_points`）

`资料/完全复刻/账/*.tsv` 里有 **170 条 A 行**按 `CollectionWindow.cs:<行号>` 落点，其中
`:5047`（`AttachCardHover`）/ `:6035` / `:6217` / `:6358` 等**都在 5043 的下游** ⇒ 在 getter 处插行会整片漂移。
做法：**改的都是已有行**（getter 1 行、doc 1 行），新增方法**只追加在类尾**（`:6386`，所有落点之后）。
`PopUpGameWindow.cs` 的落点最远只到 `:542`，helper 插在 `:553`（也在落点之后）。
✅ 事后核过：`HoverSuppressed` 仍在 **5041**、`SetHoverSuppressedForTest` 仍 **5045**、`AttachCardHover` 仍 **5047**；
`PopUpGameWindow.CheckLinkText()` 仍 **542**。文件行尾仍是**纯 LF**（`CRLF 0 / bareCR 0`），没有被翻。

---

## ③ 全仓同类扫描结果

扫描命令（`d:/4/Unity/MyGame/Assets`，含 `Editor/`）：

```
grep -rn --include=*.cs -E "\bInput\." .
```

| 口径 | 数 |
|---|---|
| `\bInput\.` 总命中 | **43** |
| 其中**真调用** | **2**（本次全修） |
| 其中**注释 / doc / 字符串** | **41** |

补充扫描（**0 命中**）：`\bInput\.` 在 `d:/4/Unity/工具/**/*.cs`（工具侧无 C# 脚本读输入）。

逐条分类（按文件）：

| 文件 | 命中 | 性质 | 处理 |
|---|---|---|---|
| `Shell/CollectionWindow.cs:5043` | 1 | **真调用** `Input.GetMouseButton(0)` | ✅ 改成 `PointerHeldForHover()` |
| `Shell/PopUpGameWindow.cs:547` | 1 | **真调用** `Input.mousePosition` | ✅ 改成 `PointerScreenForLink()` |
| `Battle/TouchInputManager.cs` | 15 | 全是**注释**（文件头「输入源换了」+ `RawInput` 字段的 `≡ Input.…` 对照表 + `Tick` 的分支标注） | 不动（它们是判据文档，删了会丢「哪一格对应哪个 legacy API」） |
| `Battle/CombatCameraZoom.cs` | 5 | 注释（原版方法体摘录 + 量纲说明） | 不动 |
| `Battle/BattleDriver.cs` | 3 | 注释（原版 `Input.GetKeyDown` / `GetMouseButtonDown` 摘录） | 不动 |
| `Hand/CardInteraction.cs` | 4 | 注释（反编译摘录 / `touchCount` 等价物说明） | 不动 |
| `Shell/PopUpGameWindow.cs` | 4 | 注释（`:47` / `:529` 原版摘录…**注意含本次改后新增的 2 条说明**） | 已订正 |
| `Shell/PointerLayer.cs:671` | 1 | 注释（原版 `Input.GetKeyDown(Escape)`） | 不动 |
| `Shell/ShellRuntime.cs:1016` | 1 | 注释（原版 `anyKeyDown` 判据） | 不动 |
| `Battle/TapParticleController.cs:18` / `Battle/EndPanel.cs:332` / `Battle/CardChoicePanel.cs:635` / `Core/CardHighlight.cs:398` / `Editor/BattleScene.cs:6346` | 各 1 | 注释 | 不动 |

> ⚠️ 备注：`TapParticleController` 文件头写「本类自己读 `!Input.GetMouseButtonDown(0)`」是**原版逐句**，
> 它的**实现**走的是 `Mouse.current.leftButton.wasPressedThisFrame`（`:199`）—— 本来就对，⛔ 别照文件头去「修」。

---

## ④ 没查清的风险（如实）

1. **`Shell/CollectionWindow.cs` 只修了「报错那一处」这一族** —— 本文件里**其它**输入全是 `WindowButton` / `PointerLayer`
   那条 UGUI 式链（`PointerLayer.Update` 读 `Mouse.current`），本次**逐条核过没有 legacy `Input`**。
2. **`Application.isPlaying == false` 一律当「没按住」** 是**我们的口径**（原版没有「编辑模式」这一档）。
   理由：批处理没有帧循环、设备读数无意义。⚠️ 若将来出现「编辑模式下预览悬停」的用法，这条会把它挡掉 ——
   那时应改为显式注入而不是放开这道闸。**没有任何断言覆盖「真机上按住主键 ⇒ 悬停被抑制」**：
   自检口 `SetHoverSuppressedForTest(bool)` 存在但**全仓 0 调用者**（本次**没有**新增断言 —— 简报只要求修崩溃；
   要补的话应在 `Editor/CollectionScene.cs` 那一组悬停断言里先 `onExit()` → `SetHoverSuppressedForTest(true)` → `onEnter()` → 断 `alpha == 0`）。
3. **`PopUpGameWindow.CheckLinkText` 的链路仍只能在真机上点**（批处理没有点击）——本次只保证它**不再抛**；
   「点带链接的正文会打开正确 URL」**没验**。
4. `TouchInputManager` 文件头那句「⛔ 只有这一处知道新输入系统的名字 —— 别在别处再读一遍鼠标」**与现状不符**
   （`BattleDriver` / `CardInteraction` / `DeckRuntime` / `PointerLayer` / `ShellRuntime` / `TapParticleController` / `ClickLog`
   都在直接读 `Mouse.current`）。本次**沿用了现状**（简报要求照它们的做法），**没有**去把读数口收口 ——
   收口属于重构，超出本代理白名单；⚠️ 那条注释**已过期**，建议调度台另立账。
