# W2 — N66（模式卡滚轮步长）· N67（模式窗 ESC 关不掉）执行报告

> 🔴 **2026-10-18 主对话补记（铁律 5）**：`Shell/MenuScroll.cs:99-107` 的 `NotchPx` doc **已由主对话补改**（+6/−1，纯注释，零行为变化）——本报告 §3/§5 把它列为「不在白名单、没改」是**当时**的口径，⛔ 别当现状。


> 执行代理 · 2026-10-18 · 白名单：`Shell/{MainMenuRuntime,GameWindow,PointerLayer,LiveOpsEventWindow,MenuWindowBase}.cs`
> 只改了 **3 个文件**：`MainMenuRuntime.cs` · `PointerLayer.cs` · `GameWindow.cs`。**没跑 Unity、没起 player、没动 git。**

---

## ① N66 —— 模式卡横向滚动的「一格 1 px」

### 改了什么

`Unity/MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs:2181`

```csharp
// 改前
_modeScroll = new MenuScroll(ModeViewport, CardCol0, ModeContentX2, 1f);
// 改后
_modeScroll = new MenuScroll(ModeViewport, CardCol0, ModeContentX2, 120f);
```

同一处上方补了一段长注释（就地留痕），要点：

- 原版 `MainMenu` 的 `GameModes` 那条 `ScrollRect` 是 **horizontal-only**（`m_Horizontal=1 / m_Vertical=0`，
  `资料/主菜单_原版规格.md:173`）⇒ **纵向滚轮在原版本来就不参与**（uGUI `ScrollRect.OnScroll` 只把量喂给允许的那根轴）
  ⇒ 原版这一段**靠拖拽**；`m_ScrollSensitivity = 1.0` 是**横向**灵敏度，**不是**我们这条列表「一格走多少」的判据。
- 照 `1.0` 传进来 ⇒ `MenuScroll.NotchK = 1/120` ⇒ 一格滚轮（`WHEEL_DELTA = 120`）只走 **1 px**，
  滚完整条行程要几百格（= 实际滚不动）。
- 现在 `120 px/格`（一屏 1080 的 1/9）。**这是我们挑的手感**，注释里已写明「⛔ 别写成原版值」。
- 行程按本文件几何 = `ModeContentX2 − ModeViewport.x2` = **2405 − 1920 = 485 px**
  ⇒ **485 ÷ 120 ≈ 4 格到底**。
  ⚠️ 派单里写的「447 px」与这里算出的 485 差 38 px（应是按另一档视口/内容算的）—— 已在注释里记明「按本文件实际几何记 485」，两者都 ≈ 4 格，不影响改动。
- **拖拽一点没动**：`MenuScroll.BeginDrag/DragTo` + `PointerLayer` 的 10px 阈值照旧。
- ⛔ 没有改原版字段 `m_ScrollSensitivity = 1.0` 那条记录（`MenuScroll` 的 `NotchPx` 文档仍写「填原版灵敏度」——
  本处是**已知的偏离**，已在调用点注明；`MenuScroll.cs` 不在白名单，没去改它的 doc）。

---

## ② N67 —— 模式窗打开后 ESC 关不掉

### 结论（先说）：**不是 `closeOnESC` 没设，也不是派发没走到那扇窗 —— 那一次真机的 ESC 键根本没进进程。**

### 判据链（逐条有出处）

1. **原版 prefab 值 = `closeOnESC = 1`**（不是 0）。
   `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1807133450305712782.json`
   现读：`type=1` · `windowsPlacement=5` · `closeOnESC=1` · `updateNavPanel=0` · `useDefaultCloseSoundIfNull=1`
   （同表：`RankedEventWindowV2` 的 MB `-7893144455456258128` 也是 `closeOnESC=1`；判据表 → `资料/阶段二_战斗入口_原版规格.md:44-46`）。
2. **我们的窗参逐值一致**：`Shell/LiveOpsEventWindow.cs:351-360` 的 `Init<>`：
   `type=Popup` · `placement=Canvas` · **`closeOnEsc = true`** · `extraScaleSmallScreen=1`（`updateNavPanel` 默认 `false` = 原版 0）。
3. **派发链完整**（唯一一份，没有第二处 ESC 实现）：
   `PointerLayer.Update`（`Shell/PointerLayer.cs:269+`）轮询 `Keyboard.current.escapeKey.wasPressedThisFrame`
   → `KeyFrame`（`:698+`）→ `KeyCancel`（`:723+`）→ `WindowsManager.Instance.TopWindow`
   → `w.IsOpen()` → `w.ESCPressed()`（`Shell/GameWindow.cs:850+`：输入层 + `closeOnEsc` 两道门槛）→ `Close()`。
   `TopWindow` = `currentWindow`（`WindowsManager.cs:330-336`），而 `OpenWindow` 对弹窗也写 `currentWindow = win`
   （`WindowsManager.cs:286-293`）⇒ 模式窗打开后它就是顶窗。
4. **真机那次的日志 = 零条 ESC 相关行**（`%LocalAppData%Low/Unity Technologies/com.unity.template.urp-blank/Player.log`，12:13–12:19 那段）：
   - 若是「窗不在 Open 态」⇒ 有 `[Key] ESC 不关 …`；
   - 若是「输入层停用」/「`closeOnEsc=false`」⇒ 有 `[Win] …`；
   - 若是「没有窗、落到主菜单」⇒ 有 `[Menu] 主菜单按 ESC ⇒ 开「退出游戏」弹窗`（还会真的弹窗）；
   - 若是「成功关窗」⇒ 必有 `LiveOpsEventWindow.Close()` 打的 `[Searching] 取消搜索` + `[Event] 取消匹配`
     —— 这两句在**点左上返回箭头**时每次都出现（12:15:48 / 12:18:09 / 12:18:35 三次点击记录里逐条都在），
     **ESC 那一段一条都没有**。
   ⇒ 上述每一条可能的分支都至少会留一行日志；**一行都没有 ⇒ `KeyCancel` 从未被调用**。
5. **跑的那份 player 里确实有这条代码**：`_tmp_view/player/.../Assembly-CSharp.dll`（11:36 构建）
   用 Mono.Cecil 反查 `PointerLayer::Update` 的 IL —— 依次 call `HandleTyping` → `Keyboard::get_current`
   → `Gamepad::get_current` → `ButtonControl::get_wasPressedThisFrame`（`escapeKey`）→ **`PointerLayer::KeyFrame`**
   ⇒ 不是「旧包没带键盘代码」。
6. 剩下的**静默**入口只有三处（都已在本批补出声，见下）：
   (a) `Keyboard.current == null`（整块键盘静默跳过）；(b) 文本框有焦点（`_editing`）把 ESC 吃掉；
   (c) `MainMenuRuntime.EscapePressed()` 里 `mm == null` 静默 `return false`。
   其中 (c) 在那一局不可能（主菜单就是这台 runtime 建的）；那一局也没点过任何文本框 ⇒ (b) 也不成立
   ⇒ **最可能是 (a) 或「键根本没到进程」**：Input System 的 `backgroundBehavior` 会在**窗口失焦**时把非后台设备置灰
   （`Keyboard.current` 仍非空、但 `wasPressedThisFrame` 恒 false），而 **`工具/wf_click.py key` 不会先 `win` 置前**
   （`key()` 只 `SendInput`；`click()` 是靠坐标点到游戏窗口才顺带把焦点给回去的 —— `工具/wf_click.py:134-141,180-201`）。
   🔴 **本工程的 player 设置把这一档放大了**：`ProjectSettings.asset:89` **`runInBackground: 0`**
   ＋ `:114` **`fullscreenMode: 1`（FullScreenWindow，无边框全屏）** ⇒ 游戏**失焦即暂停**，
   而无边框全屏下**别的窗口拿到前台时游戏仍然铺满整屏**（截图照旧是游戏画面，看不出来）。
   ⇒ **re-test 时先 `python -I d:/4/Unity/工具/wf_click.py win urp-blank` 再 `key esc`**（这也是 `资料/命令速查.md:1171` 的既有纪律）。

### 改了什么（**只补「出声」，零行为变化**）

1. `Shell/PointerLayer.cs`
   - `Update`：`Keyboard.current == null` 时**一次性** `LogWarning`（原来整块静默跳过）。
   - `HandleTyping`：ESC 用来「取消文本框编辑」时出声（原来静默，与「ESC 没进来」分不开）。
   - `KeyFrame`：`_editing` 把 ESC 吃掉时出声（并注明 `_editing` 是进程级状态、关窗不清它）。
2. `Shell/GameWindow.cs`：`ESCPressed()` **真正关掉窗的那一支**补一行 `[Win] ESC ⇒ 关掉 …`
   （原来三道门槛都有日志，唯独成功那一支一声不响 ⇒ 真机验不出「到底走没走到」）。
3. `Shell/MainMenuRuntime.cs`：`EscapePressed()` 里 `mm == null` 的静默 `return false` 补一行解释性日志
   （只在「当前场景没有常驻主菜单」时走到 = 正常路径）。

**行为一个字没改**：没有新增第二份 ESC 派发、没有改 `closeOnEsc`、没有动 `IsOpen` / `InputEnabled` / `Close()` 的任何判据；
`deck 编辑`（`DeckEditingWindow.ESCPressed` 覆写）与 `SettingsWindow` 走的是各自原有的路，不受影响。

---

## ③ typecheck

```text
TMPDIR=/tmp/wf_n66 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

（两轮：N66 改完一次、N67 的 `MainMenuRuntime` 那一条补完再跑一次，均 0/0。）

---

## ④ 没做 / 判不出的

- **没能在真机上复验 ESC**（本任务红线：不跑 Unity、不起 player）。N67 的「键没进进程」是由
  「所有分支都会出声、而日志零行」推出来的，**不是**在真机上抓到的 —— 下一次真机请按 ②·6 的姿势
  （先 `win` 再 `key esc`）复验；若这次仍无任何 `[Key]/[Win]` 行，那就是设备/注入层的问题，需要另开一条账。
- **`_editing` 的「进程级残留」只加了出声、没有修**：某个文本框所属的窗被关掉时 `PointerLayer._editing`
  不会被清（下一次 ESC 只会取消那个看不见的编辑）。真正的修法是「关窗时把属于它的文本焦点一起收掉」，
  但 `BeginText` 没有记录宿主、且调用点散在 `CollectionWindow`/`SettingsWindow`/`ProfileTab`/… （不在白名单）
  ⇒ 未动，建议单开一条账。
- **原版横向灵敏度 `m_ScrollSensitivity = 1.0` 没有改**（按派单要求保留；我们改的只是传给 `MenuScroll` 的「每格像素」）。

---

## ⑤ 顺手发现（没改，交调度台）

1. 🔴 **同一扇模式窗重复打开会叠窗**：`MainMenuRuntime.OpenMode` 每次都 `Create` 一扇**新的**
   `SkirmishEventWindow`/`RankedEventWindow`（`MainMenuRuntime.cs:2226-2239`），而 `OpenWindow` 对旧的那扇只
   `ToBackground()`（`WindowsManager.cs:289` = 只写 `CurrentState`，**不 `SetActive(false)`**，`LiveOpsEventWindow`
   也没覆写 `ToBackground`）⇒ **旧窗仍在屏上、仍占渲染队列**，只是指针命中和 ESC 被新窗接管。
   真机日志 12:14–12:19 连开 4 次 SKIRMISH ⇒ 台上叠了 4 扇（内容一样所以看不出来）。
   可能的修法 = 照 `OpenByRef` 的「已经开着就复用」那条路（`WindowsManager` 的 `automaticallyLoadedWindows` 语义）。
2. `Shell/MenuScroll.cs:99-103` 的 `NotchPx` doc 仍写「= 原版 `m_ScrollSensitivity`」；
   `MainMenuRuntime` 现在传 120 是**我们挑的**（已在调用点注明）。若将来有别人照 doc 传原版值，会再次出现「1 px 一格」
   —— 建议在 `MenuScroll` 那边补一句「横向视图原版值 1.0 不适合作纵向列表步长」。
3. `PointerLayer.Update` 里 `kb == null && gp != null` 时方向键/ESC 仍然静默不可用（手柄还能用）——
   本批只补了 kb 为 null 的一次性警告，没有补手柄上的"取消"到 ESC 语义（原版 `Cancel` 的 alt 本来就是 `joystick button 1`，
   已接 `buttonEast`，不算缺）。
