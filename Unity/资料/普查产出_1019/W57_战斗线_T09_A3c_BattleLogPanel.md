# W57 · 战斗线 `T09` 步骤 A3-c —— `BattleLogPanel` 输入层换 uGUI

> **日期** 2026-10-20（第五十七会话）· **代理** `T09-A3c`（执行）
> **判据入口** `资料/普查产出_1019/W57_战斗线_T09迁移计划.md` §2 步骤 **A3-c**（`:179`）· `W57_战斗线_T09_A1.md`
> **白名单** 只改了 `Unity/MyGame/Assets/CardPresentation/Battle/BattleLogPanel.cs` **一个文件**。
> **本波口径 = 只做不验**：未跑 Unity / 未起 player / 未跑 `*.Run` / 未跑 `_run_8_checks.sh`；只跑了秒级 typecheck。
> 未动 git、未改正本、未删文件、未改 `Editor/**`。

---

## 〇、先订正派工单里三处「按原版」的假设（铁律 5 / 10：先查全量反编译与解包资源）

本波逐组件读了 `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`，三条都**核对出了与派工单不同的事实**：

| # | 派工单写的 | 现读事实（证据） | 本波怎么做 |
|---|---|---|---|
| 1 | 背板 `shade` 用 `IPointerDownHandler` | ✅ **语义对**，但**原版用的组件不是自定义接口实现**，而是 **`UnityEngine.EventSystems.EventTrigger`**（`m_Script` PathID `-78715595128852713` → `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-78715595128852713.json`：`m_ClassName: "EventTrigger"` · `m_Namespace: "UnityEngine.EventSystems"` · `m_AssemblyName: "UnityEngine.UI"`），`m_Delegates[0].eventID = 2`（= `EventTriggerType.PointerDown`，枚举原文见本机 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventTriggerType.cs`） | 挂**同一个类、同一个事件号**（`EventTrigger` + `PointerDown`）。语义 = `IPointerDownHandler`，但连组件类型都与原版同构 |
| 2 | 行点击：`IPointerClickHandler`（或 `Button`，按原版） | 🔴 **两条都不成立**：① 10 颗 `CemeterySliderUI` 的 **`Image.m_RaycastTarget` 全是 0**（`MonoBehaviour_4768` 等逐颗核过）⇒ 行**根本不是射线靶**；② 行上的 `EventTrigger` 只有 `EndDrag(14)/PointerEnter(0)/BeginDrag(13)` → `OnDragEnd`/`OnSliderChanged`/`OnDragStart`，而那三个方法在元数据里**不存在**（残留链，见本文件 `Z2`/`Z3`）。**发行版真正跑的行点击 = `BG` 的 `EventTrigger PointerDown(2) → CemeteryManager.ClickCardLink`**（`MonoBehaviour_4070`，宿主 `GameObject/BG_622.json`） | 落在 **`BG` 的 `PointerDown`** 上（不是行上的 Click）。`ClickCardLink` 的职责 = 逐行 `TMP_TextUtilities.FindIntersectingLink` → 取链接 → `DisplayCard`（反编译全文 → `d:/2/tools/decomp_full/CemeteryManager__ClickCardLink.c`） |
| 3 | 关闭钮：`IPointerClickHandler` | 🔴 **原版没有关闭钮**：`RectTransform_2936` 的 `m_Children` 只有 **5 个**（`shade` / `BG` / `Frame` / `CemeteryActions` / `CardUI (1)`）；整棵子树逐节点扫过（`Frame` 那 5 个节点 · 10 行 · 10 个 `ActionText`），**除 `shade` / `BG` 外没有任何 `Image.m_RaycastTarget = 1`、也没有任何按钮** | **不建关闭钮**（建了就是无中生有）。关闭入口只有 `shade` 的 `PointerDown` 一条 —— 与原版同构 |

> 🔑 一句话：**这扇面板在原版里只有两个射线靶（`shade` / `BG`），两个都是 `PointerDown`。**
> 本波照抄的就是这个形状；派工单里「行用 Click、另有关闭钮」两条**按原版核下来不成立**，如实记在这里。

---

## 一、改前 / 改后对照

### 1·1 输入路径

| 入口 | 改前（本波之前） | 改后（本波） |
|---|---|---|
| **背板 `shade` 按下 ⇒ 关面板** | 只有手搓那条：`BattleDriver.HandleBattleLog()` 里 `ClickedThisFrame()` → `TryClickLogRow(WorldPointer())` 不成 → `_logPanel.Hide(); HideLogCard(); _swallowNextRelease = true;`。`shade` 本身 **`raycastTarget = false`**、身上没有任何组件 | `shade` 置 **`Image.raycastTarget = true`** + 挂 **`EventTrigger{ eventID = PointerDown }`** → `OnShadePointerDown`（计数 + 挂起关闭）→ 本帧 **`LateUpdate`** 里 `Hide()`（见 §一·2 为什么要晚这一拍） |
| **行点击 ⇒ 弹/选行** | 手搓：`BattleDriver.TryClickLogRow(WorldPointer())` → `_logPanel.RowAt(wp)` → `SelectRow` + `ShowLogCard` | `BG` 置 **`raycastTarget = true`** + 挂 **`EventTrigger{ eventID = PointerDown }`** → `OnBgPointerDown` → `RowAtScreen(eventData.position, pressEventCamera)`（**只用 `RectTransformUtility`**）→ 命中行号 → 发 `RowClicked(row)` 事件。⚠️ **面板不自己 `SelectRow` / 不弹卡**（`hideCemetery` 闸与 `ShowLogCard` 都在驱动里，跨过来就是第二份判据） |
| **关闭钮** | 不存在 | **仍然不存在**（原版没有，见 §〇·3） |
| **悬停行内链接 ⇒ 弹卡** | `BattleDriver.TickLogCard` → `LinkKeyAt(wp, cam)`（TMP `FindIntersectingLink`） | **一个字没动**（这是 TMP 字形级判据，uGUI 的矩形射线做不了；原版 `Update()` 里也是每帧 `CheckCardLink` 这个形状） |
| **开面板** | `BattleDriver` 的墓地钮（A2 已走 uGUI） | 未动 |

### 1·2 新增的对外面（全部是**加法**，无一条改动既有签名）

**两个回调口（给 W3/D 合并用）**

```csharp
public event System.Action<int> RowClicked;   // 行被点中（uGUI 那条路交出来的行号）
public event System.Action CloseRequested;    // 压暗层被按下（面板已关完，宿主收尾：HideLogCard）
```

**只读探针（自检用）**

| 成员 | 读什么 |
|---|---|
| `ShadePointerDownCountForTest` / `BgPointerDownCountForTest` | 两个 `EventTrigger` 各收到几次 `PointerDown`（灭自证：点一次 = 1） |
| `LastBgRowForTest` / `LastBgRowKeyForTest` | 最近一次 `BG` 按下命中的行号 / 那一行提到的卡 key（`-1`/`null` = 没命中） |
| `ClosePendingForTest` | 有没有「挂起的关闭」还没落 |
| `ShadeEventTriggerForTest` / `BgEventTriggerForTest` | 两颗 `EventTrigger`（`null` = 没挂上）；断言可用 `ExecuteEvents` 直接派 `PointerDown` |
| `ShadeRaycastTargetForTest` / `BgRaycastTargetForTest` / `RowRaycastTargetForTest(i)` | 读 `Image.raycastTarget`（原版期望：shade **1** · BG **1** · 10 行 **0**） |
| `RowAtScreen(Vector2, Camera)` | 屏幕点 → 行号（uGUI 换算口，公开给断言复用） |
| `ApplyPendingCloseForTest()` | 批处理没有帧循环 ⇒ `LateUpdate` 不跑，手动落一次挂起的关闭 |

### 1·3 建件处新增的两跳（`Build()` 里）

- `_shade` 建完 → `HudCanvas.Reorder(_shade.Rect, ZShade)` 之后：`_shadeTrigger = AddPointerDown(_shade, OnShadePointerDown)`；失败 `LogError`（⛔ 不静默）。
- `_bg` 建完 → `bg.SetAspect(BgW / BgH)` 之后：`_bgTrigger = AddPointerDown(bg, OnBgPointerDown)`；失败 `LogError`。
- `AddPointerDown` 内部：`piece.SetRaycastTarget(true)`（**世界空间件会在 `HudPiece` 那边 `LogError` 并返回 false**）→ `GetComponent<EventTrigger>()` 没有就 `AddComponent` → `triggers.Add(new Entry { eventID = PointerDown })`。

**没有动**：`HudCanvas` 的队列/Z 序（`PopUps` / `LogSortingOrder = −1` / `Z*` 那组数**一个都没改**，遵守「它压在还没迁的世界卡下面」的红线）· `_root` 的滑动 · `SetEntries` / `ApplyEntries` · 所有几何常量 · `BattleDriver` / `HudCanvas` / `WfSlider` / `Editor/**`。

---

## 二、`shade` 用 `PointerDown` 的证据（逐组件原文）

### 2·1 `shade` 的组件表（`GameObject/shade.json`，PathID **548**）

`m_Component` = `[2677, 1924, 4611, 4625, 1558]`：

| 组件 | 是什么 | 关键字段 |
|---|---|---|
| `RectTransform_2677` | 面板子节点 | `anchor (0,0.5)` · `ap (1067.049, 11.696)` · `sizeDelta (7020.687, 4543.930)` · `pivot (0.5,0.5)`（**远大于屏幕 ⇒ 铺满**） |
| `CanvasRenderer_1924` | 渲染器 | — |
| **`MonoBehaviour_4611`** | **`UnityEngine.UI.Image`**（`m_Script` `350208831926335389` → MonoScript `m_Name: "Image"`） | **`m_RaycastTarget: 1`** · 无 sprite · `m_Color` rgba(0, 0, 0, **0.5529412031173706**) |
| **`MonoBehaviour_4625`** | **`UnityEngine.EventSystems.EventTrigger`** | **`m_Delegates[0].eventID = 2`** → `m_Target` **4491**（`CemeteryManager, Assembly-CSharp`）· `m_MethodName = "HideCemeteryLogBtn"` · `m_Mode = 1` |
| `BoxCollider2D_1558` | 2D 碰撞体（物理射线面用） | `m_Size (5000, 5000)` |

### 2·2 `eventID = 2` 就是 `PointerDown`

- 本机 uGUI 源码 `.../com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventTriggerType.cs`：
  `PointerEnter = 0` · `PointerExit = 1` · **`PointerDown = 2`** · `PointerUp = 3` · `PointerClick = 4` …
- `EventTrigger.cs:260`：`public void OnPointerDown(PointerEventData eventData) { Execute(EventTriggerType.PointerDown, eventData); }`
  ⇒ 序列化里写 `eventID 2` = **按下那一帧**。

### 2·3 `HideCemeteryLogBtn` 的行为（反编译，`d:/2/tools/decomp_full/CemeteryManager__HideCemeteryLogBtn.c`）

`+0x90` 的字段名（`dump.cs:37068`）是 `showingSliders`，但**语义是「日志面板正开着」**（三条互证）：
`ShowCemeteryLogBtn` 打开时把它置 1、关闭时置 0；`Update()` 里 `if (showingSliders) CheckCardLink()`（悬停弹卡的**唯一**闸）。
`HideCemeteryLogBtn` 在它为 1 时执行：`DOAnchorPosX(rt, initialX +0x48 = −1200, 0.3s)` · `shade.SetActive(false)` · `showingCardUI.SetActive(false)` · 置 0。
⇒ **`shade` 的 `PointerDown` = 关面板 + 收压暗层 + 收那张卡**，与我们的 `Hide()` + `CloseRequested`（宿主收卡）逐项对齐。

### 2·4 `BG` 的对应证据（`GameObject/BG_622.json`，PathID **622**）

`m_Component` = `[2944, 2417, 4620, 1564, 4070]`：`MonoBehaviour_4620` = **`Image`（`m_RaycastTarget: 1` · 色 (0, 0.0754717, 0.0082015, 1)）** · **`MonoBehaviour_4070` = `EventTrigger`，`eventID = 2`（PointerDown）→ `CemeteryManager.ClickCardLink`** · `BoxCollider2D_1564`。

---

## 三、行为不变的证据

### 3·1 最强的一条：**0 行删除**

```text
git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Battle/BattleLogPanel.cs
195     0      Unity/MyGame/Assets/CardPresentation/Battle/BattleLogPanel.cs
```

**195 行全是新增、0 行删除** ⇒ 既有的每一条语句、每一个 public 成员（`PanelW` / `CardCenterPx` / `RowCount` / `SelectRow` / `RowAt` / `LinkKeyAt` / `Show` / `Hide` / `Toggle` / `SetEntries` / `ApplySlide` 那一条链 …）**逐字节未动**。
`Editor/**` 里引用本类的那几十处断言（`A218` / `A940` / `G9` …）**没有一处需要改**。

### 3·2 新行为只可能从 uGUI 派发进来

- 两个 `EventTrigger` 只在 `EventSystem` 的射线命中时才有回调；批处理 / 自检宿主里**一次都不会自动触发**（要断言就 `ExecuteEvents` 或 `ApplyPendingCloseForTest`）。
- `BattleDriver.HandleBattleLog()` 那条手搓链**一个字没动**（本波白名单只许改本文件）⇒ 真实鼠标那条路**仍然先走驱动**。

### 3·3 两条路并存为什么不会「点一下做两件事」

| 动作 | 重复执行时 | 结论 |
|---|---|---|
| 行点击 | 驱动 `TryClickLogRow` 做 `SelectRow` + `ShowLogCard`；uGUI 只发 `RowClicked`（**当前无订阅者**） | 幂等（同一个行号 / 同 key 的 `ShowLogCard` 直接早退） |
| 关面板 | 驱动 `Hide()`；uGUI 走 `ApplyPendingClose()` | `Hide()` 幂等（`SnapSlide` + 置 `_slideDir`），第二次调用画面无差别 |

**关闭为什么必须挂起到 `LateUpdate`（本波唯一一处「延迟」）**：
`EventSystem.Update` 与 `BattleDriver.Update` **都是默认执行顺序**（本工程没有给这两者写 `[DefaultExecutionOrder]`，现核 `grep DefaultExecutionOrder` 只命中 `Shell/PointerLayer.cs:51` 与 `Deck/DeckRuntime.cs` 的注释）。
若 `EventSystem` 先跑、回调里**当场** `Hide()` ⇒ 驱动里的 `HandleBattleLog()` 会看到 `Visible == false` ⇒ **它不再吃掉这一下** ⇒ 同一次按下继续落到 `DrivePlayerTurn()`（棋盘/手牌）= 静默的「点一下做了两件事」。
Unity 保证**所有 `Update` 跑完**才跑 `LateUpdate` ⇒ 两种顺序都安全：
- 驱动先跑 ⇒ 它自己已经关了（`ApplyPendingClose` 空转）；
- 驱动后跑 ⇒ 它照旧吃掉这一下（`Visible` 仍为 true），随后 `LateUpdate` 关掉（幂等）。

且 `LateUpdate` 与 `Update` **同帧**，不是「延迟一帧」。

### 3·4 一处**真实的行为差异（如实记，不是 bug）**

`BattleDriver.Update` 里有几条**早退**排在 `HandleBattleLog` **之前**（`PlayerInputFrozen` `:9438` · `Ctx.IsOver` `:9466` · `TickMultiCards`/`TickCardDisplayClick`/`HandleReplayBar`/`TickMulligan`/`HandleMulligan`/`HandleChoose`/`HandleSettings`/`HandleChatPopup`）。
在那些状态里点 `shade`：**改前**不关（驱动根本没走到 `HandleBattleLog`）；**改后**会关（uGUI 不看驱动状态）。
判据侧的态度：**原版的 `EventTrigger` 也不看 `BattleDriver` 的任何状态** ⇒ 改后**更接近原版**；但这确实是本波引入的、可观测的行为差异，**记在这里**由主对话裁（要收回的话，一行 gate 就能加，但 gate 值只有驱动拿得到 ⇒ 归 W3 合并）。

---

## 四、没查清的部分（⛔ 不猜，给「还差什么」）

1. **`EventSystem.Update` 与 `BattleDriver.Update` 在真机上的实际先后**：本波不能跑 Unity ⇒ **没测**。
   本波的设计（关闭推到 `LateUpdate`）**不依赖**这个顺序，两种都安全；但「哪一种是真机现状」仍然未知（影响 §五·3 那条顺手发现的严重程度）。
2. **`RectTransformUtility.ScreenPointToWorldPointInRectangle(bg.Rect, screen, pressEventCamera, out wp)` 与 `BattleDriver.WorldPointer()`（`LayoutSpace.ScreenToWorld`）在非 16:9 下是否逐位同点**：两者都映射到 HUD 平面、`RowAt` 只比 xy，理论上同点；**没跑过**（批处理 4:3 与 `cam.aspect` 16:9 的档位要实例测）。
3. **`EventTrigger` 挂在运行时建的件上**：本工程的 `!u!115` 坑只影响「**被存进场景**的 MonoBehaviour」。这两颗是 **Unity 自带组件**（有真脚本资产），且 HUD 是运行时建的（`BattleScene.Run` 不 `SaveScene`）⇒ 预期无风险，但**没实跑验证**。
4. **`_bg` 的顶边比最上面一行高 1.73 px**：`BG` 高 454.4 / 中心自顶 539.069 ⇒ 顶边 311.87；行容器顶边 = `525.815 − 431.344/2 = 310.14` ⇒ **最上一行的顶 1.73 px 落在 `BG` 外**（那一条会打到 `shade` ⇒ 关面板）。
   ⚠️ 这**不是我们摆错**：原版 `BG`（`RectTransform_2944`）与 `CemeteryActions`（`RT_2935`）的相对几何本来就是这两个数 ⇒ **原版同样存在这 1.73 px**。如实记，不动。
5. **原版行点击**是否也接受「整行矩形」：原版是 `FindIntersectingLink`（只认字形），我们是整行（`G9` 已记的「比原版宽」）。本波**沿用既有口径**，未改。

---

## 五、顺手发现（⛔ 本波不改白名单外的东西，建议主对话记账）

1. 🔴 **原版「点行弹卡」之后会【关掉日志面板】，我们不会**：
   `CemeteryManager__ClickCardLink.c` 末尾是 `BattleManager__DisplayCard(...); CemeteryManager__HideCemeteryLogBtn(param_1, 0);`
   —— 而那时面板正开着（`showingSliders == 1`）⇒ **`HideCemeteryLogBtn` 走的是「真关」那一支**（滑动回收 + 压暗层关 + 卡关）。
   我们的 `BattleDriver.TryClickLogRow` 只 `ShowLogCard(key, row)`、**面板留着**。
   ⇒ **这是一条尚未记账的真偏离**（判据 = 上面那段反编译 + `HideCemeteryLogBtn` 的 `+0x90` 闸语义，见 §二·3）。
   修在 `BattleDriver`（W3/D 的活）⇒ 本波只报不改。
2. **`BattleDriver.HandleBattleLog()` 的手搓链退场配方**（W3/D 用，逐句）：
   - 面板开着那一支：删 `ClickedThisFrame()` 里面那三句（`TryClickLogRow` / `Hide()+HideLogCard()` / `_swallowNextRelease`），**只保留 `return true`**（模态闸，挡住底下 HUD 钮与棋盘）；
   - `BuildHud` 里给 `_logPanel` 接两个回调：`RowClicked += r => TryClickLogRow(row 口径);` · `CloseRequested += HideLogCard;`
   - 之后 `RowAt` / `RowCardKey` / `SelectRow` 仍是判据正本（`RowClicked` 的处理直接复用它们）。
3. **`HudClickBlocked` 的时序缺口（既有问题，不是本波引入）**：`HudClickBlocked` 在**点击动作那一刻**求值，而它的 `HudButtonsBlocked` 读 `_logPanel.Visible`。
   若驱动先跑（面板当帧被关掉）、`EventSystem` 后跑 ⇒ 射线命中底下的 HUD 钮时 `_logPanel.Visible` **已经是 false** ⇒ 那道闸放行 ⇒ 「关面板 + 顺手开了设置窗」。
   本波把 `shade` 变成射线靶**只在 `EventSystem` 先跑的顺序下**能挡住这一下；**驱动先跑的顺序下依旧挡不住** ⇒ 真正的解法是在 `shade` 还活着的时候把射线吃掉（原版就是这么做的），或者让 `HudClickBlocked` 记住「这一帧刚关过面板」。**归 W3/A4**。
4. **`HudCanvas` 的子画布 `GraphicRaycaster` 已就位**（`HudCanvas.cs:542`），本波直接可用，无需新建。
5. **`BattleLogPanel` 的文件头没写「原版输入口径」**：本波把判据写进了 `Build()` 上方那一大段（含三条反编译/序列化证据），将来若有人问「为什么是 PointerDown」直接在源码里就有答案。

---

## 六、typecheck 与行尾

```text
$ TMPDIR=/tmp/wf_t09a3c bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

行尾核对（二进制计数，改前 / 改后）：

| 时点 | 字节 | CRLF | LF |  lone LF |
|---|---:|---:|---:|---:|
| 改前 | 88,621 | 1,062 | 1,062 | **0** |
| 改后 | 104,186 | 1,257 | 1,257 | **0** |

⇒ 仍是**纯 CRLF**，没有翻行尾（`git diff --numstat` = 195/0 也印证：没有整篇重写）。

---

## 七、一句话给主对话

**`BattleLogPanel` 的输入层已换成 uGUI，形状与原版逐组件同构（`shade` / `BG` 各一颗 `EventTrigger + PointerDown`，两件 `raycastTarget = true`，10 行保持 0），
`195 / 0` 全是新增、既有 API 与手搓链一行未改；`ROW点击` 与 `关闭` 各留了一个回调口给 W3/D 合并；
派工单里「行用 IPointerClickHandler」「有关闭钮」两条经原版核对**不成立**，已按原版落地并记明；
typecheck 运行时 0 / 编辑器 0。**
