# S8b · TransformScalerBySmallScreenUI 查清

> 2026-10-17 · 只读摸底代理 · 判据 = 反编译方法体 + `dump.cs` + 解包场景 JSON + `.rdata` 常量直读 + 我们自己的 `.cs`（全程只读）
> 目的：给主对话一个能下判断的结论（它是什么 · 影响面 · 要不要做 · **对 `T09` 步 8 有没有影响**）。

## 一、结论摘要

1. **它是什么**：一个**窗口根 Transform 的「整扇乘一个固定倍数」缩放器** —— `LateUpdate` 把窗根 `localScale`
   逐分量 × `menuScale`（并回读缓存防重复乘）；触发条件**与屏宽/宽高比无关**。
2. **是不是原版「处理非 16:9」的手段** ⇒ 🔴 **不是**。它的开关是 **`GameStaticData.smallScreenUI`**
   —— 一个**设置项**（设置→图形页那颗「Small Screen UI」开关，`GraphicsTab.SmallScreenToggleClick` 写它），
   **出厂 = 0（关）**，默认值 = `IsSteamRunningOnSteamDeck()`。**全反编译里没有任何一处拿 `Screen.width/aspect/dpi` 去算它**。
   原版真正处理非 16:9 的是**另一件**：`ScreenAspectRatioController`（按屏比例把两台相机的 `Camera.rect` 夹成黑边盒）。
3. **我们有没有** ⇒ ✅ **有，而且已经做完**（`Shell/TransformScalerBySmallScreenUI.cs`，A165/2026-10-06 落地；
   类名/字段名照原版，方法逐句照反编译）。普查 `EW3` 那句「我们一扇都没实现」**是 2026-10-06 之前的旧账**（已过期）。
4. **对 `T09` 步 8 有没有影响** ⇒ 🔴 **没有**（判据见 §七）。它与「三档宽高比验收」**正交**：它按**用户开关**放大、不按 aspect；
   而步 8 跑的是**默认设置**（开关关）⇒ 缩放器全窗 `enabled=false`，三档下都不动作。
   真正与步 8 相关的是 `ScreenAspectRatioController`（我们也有）+ `T09` 自己的 uGUI 迁移（`补⑨`）。

## 二、它是什么（反编译方法体逐段 + 出处）

类 = `d:/2/tools/il2cpp_out/dump.cs:113778-113807`（TypeDefIndex 3045）；字段 `float menuScale @0x20`（SerializeField）·
`Vector3 lastUsedScale @0x24`（非序列化）。

- **`SetScale(float x)`**（`TransformScalerBySmallScreenUI__SetScale.c`）：`menuScale = x; Initialize();`
- **`Initialize()`**（`__Initialize.c`）：`enabled = (menuScale != 1.0f) && *(char*)(GameStaticData.staticFields + 0x11c)`
  ⇒ **裸 `!=`**（不是 `Mathf.Approximately`）；那个 `+0x11c` = **`GameStaticData.smallScreenUI`**（静态 bool，`dump.cs:119489`）。
- **`OnEnable()`**（`__OnEnable.c`）：只是再调一次 `Initialize()`。
- **`LateUpdate()`**（`__LateUpdate.c`）：
  `d = 当前 localScale − lastUsedScale`；**`if (d.sqrMagnitude < 0.0001f) return;`**（防重复乘的守卫）
  否则 `transform.localScale = 当前 localScale × menuScale`（逐分量），再**回读** `transform.localScale` 存进 `lastUsedScale`。
- **`.ctor`**（`TransformScalerBySmallScreenUI__.ctor.c`）：`menuScale = 1.0f`；`lastUsedScale` 零初始化。
- **谁挂它 / 什么时候** = `GameWindow__Open.c`（窗口基类的开窗入口，逐句）：
  `if (GameStaticData.smallScreenUI) { if (!Mathf.Approximately(extraScaleSmallScreen, 1f)) { (GetComponent<>() ?? AddComponent<>()).SetScale(extraScaleSmallScreen); } }`
  ⇒ 乘在**窗口根 Transform**（整扇窗一起放大）。`extraScaleSmallScreen` = 窗口的 `SerializeField float @0x3c`

**常量直读**（`工具/read_literal.py`，`.rdata`）：`0x1834b2bb8` = **1.0f**（`menuScale` 比的那个）·
`0x1834b2f48` = **0.0001f**（守卫阈值）· `0x1834b2e60` = `0x7FFFFFFF`（取绝对值的掩码）。

**那个开关从哪来**：`GraphicsTab__SmallScreenToggleClick.c` 明确写
`smallScreenUI(+0x11c) = 参数; smallUIChosenManually(+0x12e) = 1;`。
全反编译里写 `+0x11c` 的**只有 5 处**：`GameStaticData..cctor`（=**0**，出厂关）·
`PlayerDataManager.Load{PlayerData,StarterData,SharedDefaultValues}`（**读玩家存档**；`SharedDefaultValues` 那句
`= get_DefaultTouchInput()`，而 `get_DefaultSmallScreenUI` / `get_DefaultTouchInput` **同一 RVA `0x87E570`**
⇒ 值 = `Steamworks.SteamUtils.IsSteamRunningOnSteamDeck()`)· 上面那颗设置开关。

## 三、状态 → 参数对照表

| 状态 | 触发条件 | `enabled` | 效果 |
|---|---|---|---|
| **出厂 / 玩家没动过** | `GameStaticData..cctor` 写 0 | **false** | **不缩放**（窗根 localScale 原样） |
| **在 Steam Deck 上（存档默认）** | `SharedDefaultValues` = `IsSteamRunningOnSteamDeck()` | true（若 `menuScale≠1`） | 该窗 × `menuScale` |
| **设置里手动开** | `GraphicsTab.SmallScreenToggleClick(true)` | true（若 `menuScale≠1`） | 该窗 × `menuScale` |
| **手动关** | 同上传 false | false | 不缩放 |

**缩放值 `menuScale` 有两档来源**（取决于窗口）：
① 窗的 `extraScaleSmallScreen != 1` ⇒ `Open()` **用 `extraScaleSmallScreen` 覆盖**；
② `extraScaleSmallScreen == 1` ⇒ **prefab/场景里序列化的 `menuScale` 原样生效**（不覆盖）。
⚠️ **只有一种「值」的说法是错的** —— 同一组件 `menuScale` 逐节点不同（见 §四），且运行时可被覆盖。

## 四、那 10 个节点（`battlearena2` 全场景普查 · 逐字节读 `MonoBehaviour/*.json`）

组件 = `MB m_Script.m_PathID = 3361136396530371980`。名字 = 反查该 `MB` pid 落在哪个 `GameObject/*.json` 的
`m_Component` 里；位置 = `资料/说明书/01_战斗_对战/2D层_battlearena2全树.md`（行号）。

| MB 文件 | 节点名 | `menuScale` | 在树里的位置 |
|---|---|---|---|
| `MonoBehaviour_4447` | **ChatPopup** | **1.30** | `:445`（Unit Chat 家族，`inactive`） |
| `MonoBehaviour_4514` | **CemeteryLogPanel** | **1.30** | `:185`（HUD 左区，`inactive`） |
| `MonoBehaviour_5469` | **Alliance Panel** | **1.30** | `:1155`（顶层，`inactive`） |
| `MonoBehaviour_4634` | **EnemyChatDisplay** | **1.25** | `:438`（`Unit Chat` 下） |
| `MonoBehaviour_4664` | **RadioChat** | **1.25** | `:479`（`inactive`） |
| `MonoBehaviour_4763` | **PlayerInfo** | **1.25** | `:169`（HUD 玩家信息面板） |
| `MonoBehaviour_4993` | **EnemyInfo** | **1.25** | `:157`（`Safe area BackCanvas/LeftArea` 下） |
| `MonoBehaviour_5369` | **PlayerChatDisplay** | **1.25** | `:431`（`Unit Chat` 下） |
| `MonoBehaviour_5176` | **TutorialTip** | **1.20** | `:1143`（Tutorial 家族） |
| `MonoBehaviour_4659` | **LowerSection** | **1.12** | `:578`（`Card Display Window` 内） |

⇒ 值分布 = **1.12×1 · 1.20×1 · 1.25×5 · 1.30×3**（与 `EW3` 报的一致，逐文件复核无误）。
它们全是**战斗场景的 2D HUD/弹窗件**；这 10 颗是**场景里预烤**的（`m_Enabled=1`），开关关着时 `Initialize` 把它们关掉。

## 五、我们现状对照（有 · 是不是同一件事 · 差在哪）

- **有**：`Shell/TransformScalerBySmallScreenUI.cs` —— ① `static class SmallScreenUI`（= `GameStaticData.smallScreenUI`，
  唯一来源，`PlayerPrefs` 持久化「我们挑的」）；② `class TransformScalerBySmallScreenUI`（类名/字段名/方法逐句照原版）。
  生产调用点 = `Shell/GameWindow.cs:473/514 ApplySmallScreenScale()`（照 `GameWindow.Open()` 那一段；两处 `Mathf.Approximately` 语义核过）。
- **是不是同一件事** ⇒ ✅ **是**：同一开关、同一倍数语义、同一 `LateUpdate` 守卫（`0.0001`）。
- **差在哪（如实）**：
  ① **持久化**用 `PlayerPrefs`（原版走服务器存档）—— 我们挑的；
  ② `ChosenManually` 只做写入那一半（原版还有谁读它**未查清**）；
  ③ `AddComponent<>()` 的生产调用点**只在菜单窗口那几处**（`Shell/GameWindow`）—— §四 那 10 个**战斗场景 2D 件
     我们还没建成同名类**（`ChatPopup`/`PlayerInfo`/… 全工程无同名类），所以**它们在我们工程里拿不到这个组件**。
     ⚠️ 影响面 = **仅在「用户手动开了 Small Screen UI」这一档**；默认关 ⇒ 无可见差别（见 §六判据）。
- 另有**别的** `SmallScreenUI` 消费者（都是**按开关、不按 aspect**）：`Hand/HandLayout.cs`（`maxLayoutSizeSmallScreen=0.51`）·
  `Deck/DeckRuntime.cs`（卡池 `PoolMobileSizeScale`）· `Battle/Label.cs` · `Battle/CombatAutoZoom.cs` 等。

## 六、要不要实现（**做 / 不做的判据分别是什么** —— ⛔ 不替用户拍板）

- **主体（缩放器本体 + 开关 + 菜单窗口接上）⇒ 已经做了**，所以「做不做」只针对**§四 那 10 个战斗场景件**。
- **「要补」的判据**：若采用**铁律 11「与原版不符全部要做，只有先后」** ⇒ **要补**（判据 = 那 10 颗原版确有其物、
  逐颗有 `menuScale` 实测值，不属「原版没有 / 判据是空的」）。
- **「可以不做」的判据**（任一条成立才成立，且要写进出处）：
  ① 用户**明确拍板**不做（`T09` 目标是「宽高比不坏 + 点得到」，与本件不同轴，可单独立账或缓做）；
  ② 判据在**远端/存档侧**（`ChosenManually` 的下游）、或**等拍板** ⇒ 进 `§29·c/§29·d` 挂起。
- ⚠️ **不能用的措辞**：「影响小所以不做」「小屏才用所以不值得」—— 那就违反铁律 11。**只有先后之分**。
- 建议（供裁）：本体已齐，**那 10 颗列成一条待办**（每条带：节点名 / `menuScale` 值 / 出处 MB 文件）即可，
  不必现在插队做 —— 但**要立账**（铁律 5·b）。

## 七、对 `T09` 步 8（三档宽高比统一验收）验收口径的影响

🔴 **没有影响**，理由三条（互相独立、任一条成立即够）：

1. **正交轴**：本件按 **`GameStaticData.smallScreenUI`（一个设置项）** 动作，**不读 `Screen.width/aspect`**；
   而步 8 验的是**随宽高比变化**的行为（`ScreenAspectRatioController` 的相机黑边 + uGUI `CanvasScaler(Expand)` 迁移）。
   两者不共享任何输入。
2. **默认档就是关**：`T09` 步 8 在**默认设置**下起 player（不手动拨那颗开关）⇒ 全窗 `enabled=false` ⇒
   三档下缩放器**都不动作** ⇒ 不影响任何一格的期望值。
3. **原版侧同理**：那 10 颗战斗件在默认（非 Deck / 没开开关）下 `enabled=false`，同档对照也是「不缩放」，
   **不会给步 8 制造原版 vs 我们的差异**。

⚠️ **唯一的例外（要写进验收口径的注意事项）**：**若步 8 那三张截图是在「Small Screen UI 开着」的状态下拍的**
（例如机器是 Steam Deck 存档，或验收前手动拨过开关），**弹窗族会整体 ×1.12~1.35**，那时三档截图里
**弹窗会比 HUD 大一圈** —— 那**不是**宽高比缺陷，**是另一条开关在起作用**。
⇒ 验收口径请**注明：三档截图必须在 `SmallScreenUI.Enabled == false`（出厂档）下拍**（或明确知道它开着）。

另注：`T09` 里真正「处理非 16:9」的两件是 `ScreenAspectRatioController`（已实现，`Battle/ScreenAspectRatioController.cs`，
夹 **4:3–22:9**）与 `T09` 自己的 uGUI 迁移 —— 与本件无关。

## 八、还没查清的（⛔ 不猜）

1. **`GameWindow__Open.c` 里 `Mathf.Approximately` 那一段**的**中间三条 `if` 夹逼**（`DAT_1842394f8` 那个静态 +
   `×8.0` / `×1e-06` 的 clamp）**没逐条解** —— 它看起来正是 `Mathf.Approximately` 的内联展开（`Abs(a-b) < Max(1e-6·Max(|a|,|b|), Epsilon·8)`），
   与我们 `GameWindow.ApplySmallScreenScale` 用 `Mathf.Approximately` **语义一致**，但**未逐字节证明**。
2. **`smallUIChosenManually(+0x12e)` 有谁读** —— 全反编译只见写入点，**没找到读者**。
3. **§四 那 10 颗战斗件在我们工程里最终该挂到哪个类 / 哪次建件** —— 我们这边它们**还没有同名类**，
   接线位置（`T09` 迁移时的宿主）**未定**。
4. **另外 13 个战场是否同值** —— 本次只逐字节核了 `battlearena2`（任务指定）；其余 13 场**未扫**（`EW3` 也只报了 arena2）。
5. **我搜过但没命中的词/位置**（供下一位接手）：`grep -rl "smallScreenUI" decomp_full/` 无命中（反编译用偏移不用名字）·
   `dump.cs` 无 `set_SmallScreenUI`（该字段非属性、直写）· `script.json` 里 `TransformScalerBySmallScreenUI` 只有那 6 个方法。
