# Block12 · `BattleDriver` 族 4 条（A531 / A651 / A659 / A660）

> 写手：Block12（2026-10-14）· 独占文件 = `Battle/BattleDriver.cs` + `Core/LayoutSpace.cs`（⛔ 未碰任何别的文件）
> 判据原文：`资料/普查产出_1013/批次计划_1013.md` §六·B（A531 行 218 · A651 行 338 · A659 行 346 · A660 行 347）
> 类型检查：**0 / 0**（运行时 0 · 编辑器 0）· ⛔ 未跑 Unity（按铁律 12 攒批，`BattleScene.Run` 由调度台在同步点跑）

---

## 一、逐条结论

| 编号 | 结论 | 改了什么 |
|---|---|---|
| **A531** | ✅ **做完**（10 件全部换成原版未取整值 + 顺手订正 1 处真偏离 + 1 处自相矛盾的注释）· ⚠️ **细口径断言没落**（宿主不在我白名单） | `Battle/BattleDriver.cs`（`BuildHudExtras` 12 处字面量） |
| **A651** | ✅ **做完**（改名 + 两个名字共用同一份设备读取） | `Battle/BattleDriver.cs`：`PointerDown()` → `PointerHeldRaw()`（定义 + 2 个调用点 + `PointerHeld()` 改成一句转发） |
| **A659** | ✅ **做完**（守卫落在 `WorldPointer()`；`ScreenToWorld` 保持纯换算器） | `Core/LayoutSpace.cs` 新增 `IsInsideScreen` · `Battle/BattleDriver.cs` 的 `WorldPointer()` 加一句 |
| **A660** | ✅ **做完**（原版那一句 `Toggle(false)` 补上 + 复位口）· ⚠️ 同协程另有 2 步**没做**（A660 没点名 → §四·4） | `Battle/BattleDriver.cs`：结算面板起播处 `Toggle(false)` · `Begin()` 里 `Toggle(true)` |

---

## 二、A531 —— `BuildHudExtras` 同族取整字面量（10 件）

### 2.1 方法（照 A513 那一套）
① 原版直读 `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_<pid>.json`；
② **父链走完**（脚本 `/d/4/_tmp_view/b12_chain.py`，**先在两颗已知件上验证过**：
`ChatButton`(RT 2984) 复算出 `x[50.9201946,115.3631954] y[880.154,942.0]`（A513 的判据值 `50.9201953125…`）、
`CenterCameraButton`(RT 3487) 复算出中心 `(50.1507568, 599.0970230)`（A423 的判据值 `50.1508/599.097`）
—— **两颗都逐位吻合**，算法可信）；
③ 写成 `HudAbs(…x,y,w,h…)` 那四个 px。

⚠️ **父链算法的两个要点**（这次现查的，写下来给下一个用同一把尺子的人）：
- 顶到「裸 `Transform`」那一层时，**整条 RT 链按 `1920×1080` 画布帧**算（那些顶层节点自己的
  `m_SizeDelta` 是 `0×0` 的模板值，照它算会把整条链压成零尺寸 ⇒ 例如 `Right Anchor` 的
  `anchorMax=(1,0.5)` 会塌到底左边）。**0827 报告那份 `chain_rect` 用的就是这个口径**（两边对得上：10 件逐一吻合到它印的那 0.1 px）。
- 链上有 `m_LocalScale ≠ 1` 时**rect 不变、实绘才变**（`AvatarItemSmall` 的 `Border` 1.25 就是这一档）。

### 2.2 逐件（旧值 → 新值 · 旧值偏多少）
> 单位：px（1920×1080，y 从上）；括号里是世界单位（**1 px = 1/108 wu**；A423/A513 那一档阈值 = **1e-4 wu ≈ 0.0108 px**）。

| 件（RT pid） | 旧字面量 | 新字面量（原版精确） | 旧值偏差 |
|---|---|---|---|
| `TitleBackground_Me` (3560) | 54.2 / 1028.5 / 311 / 42 | 54.2001953125 / **1028.5051536560059** / 311 / 42 | y 0.0052 px（4.8e-5） |
| `TitleBackground_Foe` (3437) | 54.5 / **92.8** / 311 / 42 | 54.5 / **92.75257110595703** / 311 / 42 | y **0.0474 px（4.4e-4）**❌超阈 |
| `TitleText_Me` (2893) | 中心 227.1 / 1044.05 | 中心 **227.10895892232656 / 1044.0677461922169** | 0.0177 px（1.6e-4）❌超阈 |
| `TitleText_Foe` (3134) | 中心 227.4 / 108.3 | 中心 **227.40876360982656 / 108.31516364216805** | 0.0152 px（1.4e-4）❌超阈 |
| `AvatarItemSmall_Me` (Border 2691) | 1.71 / 947.61 / 110.88 / 123.88 | **1.6965386574734538 / 947.5795302567823 / 110.90930610790465 / 123.90649041742475** | 0.0305 px（2.8e-4）❌超阈 |
| `AvatarItemSmall_Foe` (Border 3263) | 3.01 / 12.11 / 110.88 / 123.88 | **2.9963433449734396 / 12.126946943793968 / 110.90930610790468 / 123.90649041742478** | 0.0170 px（1.6e-4）❌超阈 |
| `OffensiveButton` (3161) | 0 / 446.9 / 109.01 / 106.94 | 0 / **446.85699939727783** / **109.00800323486328** / **106.94300079345703** | 0.0430 px（4.0e-4）❌超阈 |
| `QPText_Me` (3483) | 中心 1865.85 / 644.0 | 中心 **1865.8381719470285 / 643.9898812899978** | 0.0118 px（1.1e-4）❌超阈 |
| `QPText_Foe` (2607) | 中心 1865.0 / 198.7 | 中心 **1864.9734582742763 / 198.7184759276015** | 0.0265 px（2.5e-4）❌超阈 |
| `EnergyAccumulation_Foe` (2939) | 1746.7 / 247.9 / 77.8 / 80.1 | **1746.6571888152716 / 247.9047392226712 / 77.78600692749023 / 80.11302185058594** | 0.0428 px（4.0e-4）❌超阈 |
| **`EnergyAccumulation_Me` (2719)** | 1746.7 / 515.6 / 77.8 / 80.1 | **1748.5645137293218** / 515.5400514454191 / 77.78600692749023 / 80.11302185058594 | **1.8645 px（1.73e-2）**🔴**真偏离**（见 2.3） |
| `OvertimeIndicator` (3565) | 1718.9 / 341.5 / 68.62 / 70.99 | **1718.8823928833008 / 341.50400161743164 / 68.625 / 70.99199676513672** | 0.0176 px（1.6e-4）❌超阈 |
| `ChatButton` / `CenterCameraButton` | —— | A513 / A423 上一轮已收口，**本次未动** | —— |

**⇒ 10 件里有 9 件旧值是「取整后超阈」**（A531 说「靠 1.5 px 粗容差容忍着」，实际是**超了细口径那一档**、不是超了粗口径）。

### 2.3 🔴 顺手查出的**真偏离**：我方那盏能量灯照抄了敌方的 x
- 原版两颗的 `anchoredPosition` **不一样**：敌 `(-81.30000305175781, 0.5)`（`RectTransform_2939.json`）、
  我 **`(-80.19999694824219, 0.5)`**（`RectTransform_2719.json`）。
- **两证**：① `子代理读报_back右区_0827.md:162` 自己印的就是「我 x[1748.6,1826.4]」；
  ② **实况 dump**（`runtime_ui_dump_drive_0912.tsv`）`:287` 我 `pos(-80.2,0.5)` / `:258` 敌 `pos(-81.3,0.5)`。
- 我们原来**两侧都写 1746.7**（= 敌方的值）⇒ 我方那盏灯**偏左 1.864 px**。已订正。
- ⚠️ 同一段注释里那句「与我方那个是**同一个相对位置**（holder 中心的 (-81.3,+0.5)）」**是错的**，
  已按铁律 5 就地订正（保留更正痕迹）。

### 2.4 换算链的**回归验证**（静态，可复现）
用 float32 走一遍**我们自己的**换算（`HudAbs`：`cx=(x+w*0.5)/1920`、`cy=1-(y+h*0.5)/1080`
→ `ToWorld` → `ToNormalized` → `HudExtraPosPx` 的 `n.x*1920 / (1-n.y)*1080`）：

| 件 | 新值残差（px） | 旧值残差（px） |
|---|---|---|
| 10 件全表 | **最大 1.01e-4 px**（= 9.4e-7 wu，比阈值低两个数量级） | 0.0118 ~ 1.8645 px |
| 4 颗文字中心（`TitleText_*` / `QPText_*`） | 最大 5.2e-5 px（4.9e-7 wu） | 0.0118 ~ 0.0265 px |

⇒ 改完**细口径那一档可以绿**；改之前**10 件里有 9 件会红**（A513 只钉了其中一件）。

### 2.5 ⚠️ 没做完的那一半：**细口径断言**（A513 的另一半）
- **宿主 = `Editor/BattleScene.cs`**（`At(…)`（1.5 px 粗口径）在 `:8551-8564`、A513 的细口径在 `:8566-8588`）
  —— **不在我白名单**（简报明确列为「别的代理正在改」）⇒ ⛔ 一行没碰。
- **可直接粘贴的期望值**（照 A513 的形状：`LayoutSpace.ToWorld(中心x/1920f, (1080−中心y)/1080f)`，
  与 `drv.HudExtraWorldPos(name)` 比、阈值 `1e-4` wu）：

  | 名字 | 原版中心 px（y 从上） |
  |---|---|
  | `TitleBackground_Me` | (209.7001953125, 1049.5051536560059) |
  | `TitleBackground_Foe` | (210.0, 113.75257110595703) |
  | `AvatarItemSmall_Me` | (57.15119171142578, 1009.5327754654946) |
  | `AvatarItemSmall_Foe` | (58.45099639892578, 74.08019215250636) |
  | `OffensiveButton` | (54.50400161743164, 500.32849979400635) |
  | `EnergyAccumulation_Foe` | (1785.550192279, 287.961250148) |
  | `EnergyAccumulation_Me` | (1787.457517193, 555.596562371) |
  | `OvertimeIndicator` | (1753.1948928833008, 377.0) |
  | `ChatButton`（已有） | (83.14169570922852, 911.0769996643066) |
  | `CenterCameraButton`（已有） | (50.1507568359, 599.0970230103) |

### 2.6 🔴 **会变红的既有断言（1 条）—— 宿主不是我的**
`Editor/BattleScene.cs:8563` 的 `At("EnergyAccumulation_Me", 1785.6f, 555.65f);`
—— 它钉的是**我们自己那个错的 x**（旧中心 1785.6），改对之后实测中心 = **1787.4575**
⇒ `|Δx| = 1.8575 > 1.5` ⇒ **这条会红**。
**改法（一行）**：`At("EnergyAccumulation_Me", 1787.46f, 555.6f);   // 我 x[1748.6,1826.4] y[515.5,595.7]`
（其余 9 条粗口径**全部仍绿**：最大 |Δ| = 0.047 px）。
⛔ **别反过来把实现改回 1746.7** —— 那是错的（§2.3 两证）。

---

## 三、A651 —— `PointerDown()` 与 `PointerHeld()` 同体两名

| | |
|---|---|
| 判据 | `资料/普查产出_1013/WB4_输入触发沿.md:81-85` + §八·2（「建议谁动那一族谁合并」）+ §八·3（`ClickedThisFrame` 的歧义，**那是另一笔**） |
| 原名的问题 | `PointerDown()` 与「**按下沿**」（`ClickedThisFrame` / `ReleasedThisFrame`）、以及原版 `EventTriggerType.PointerDown`（`:24`）= 三者撞名；它的体**一直是 `isPressed`（按住）** |
| 改法 | ① 定义 `PointerDown()` → **`PointerHeldRaw()`**（补 doc：它是「按住」、与 `PointerHeld` 的分工、调用点只有两处）；② 两个调用点 `BoardPress(world, …)` / `skillPanel.SetPointer(world, …)` 跟着改；③ `PointerHeld()` 改成**一句转发** `return PointerHeldRaw();`（原来那段 `isPressed` 是**抄的第二份**） |
| ⛔ 刻意**没有合并成一个** | 理由写在 `PointerHeldRaw` 的 doc 里：`PointerHeld` 多一个自检钉死口（`PointerHeldForTest`），合并会让「自检钉死的值」**多影响两条调用链**（`BoardPress` / 技能卡面板）⇒ 那是**行为变化**，而 A657② 明确写着「`PointerDown()` 不要动」（= 它的语义别动）。⇒ 只收「重名 + 抄两份体」，**语义与调用点行为逐字不变** |
| 牵连面 | **只在本文件**（全仓 grep `PointerDown()` 只剩两处**新注释**里提到「原来叫这个名字」）· `ChatPopupPanel.PointerDownAt/UpAt` 是**另一族**（原版 `EventTrigger` 的按/抬，WB4 拆的）**没动** |

---

## 四、A659 —— 「指针在屏幕外 ⇒ 归零」的守卫

| | |
|---|---|
| 判据（第一权威，逐句亲读） | `d:/2/tools/decomp_full/BattleManager__GetMousePerspectivePos.c:26-27`：`0.0 <= f1 && f1 < Screen.width && 0.0 <= f2 && f2 < Screen.height` 才 `Camera.ScreenToWorldPoint`（`:29-35`）；否则 `return Vector2.zero`（`:40-44`） |
| 🔑 一个**读法要点** | 那个 `zero` 是**世界坐标的零**（函数返回的 8 字节 = `ScreenToWorldPoint` 结果的前两格）—— **不是**「屏幕 (0,0) 换算过去的点」。⇒ 我们等价物 = 返回 `Vector3.zero` |
| 落点 | ① `Core/LayoutSpace.cs` 新增 `public static bool IsInsideScreen(Vector2)`（判据 + 「为什么**不**塞进 `ScreenToWorld`」都写在 doc 里）；② `BattleDriver.WorldPointer()` 在 `PointerScreen()` 之后加一句 `if (!LayoutSpace.IsInsideScreen(sp)) return Vector3.zero;` |
| ⛔ 为什么**不**改 `ScreenToWorld` | 它是**纯换算器**，十几个调用点（`Shell/PointerLayer` · `Deck/DeckRuntime` · `Hand/CardInteraction` · `Board/BoardLayout` …）**屏幕外外推是换算的正确行为**；原版那道守卫属于 `GetMousePerspectivePos` **一个函数**，它的等价物是 `WorldPointer`（W-B5 自己也是这么对应的：`资料/普查产出_1013/WB5_两件组件.md` §三·3）。在那里拦会把船坞/卡组/外壳的拖拽一起改掉 |
| **自检影响 = 零** | ① 批处理下 `Screen` = **640×480**（实测：`BattleScene` 日志里自检自己打的「前提：`Screen.width(640) != Screen.height(480)`」），`Mouse.current == null` ⇒ 屏幕点恒 `(0,0)` ⇒ **在屏幕内**；② 自检要钉指针都走 `PointerWorldForTest`（**在那句之前就 return 了**）或直喂世界坐标 |
| 真机上的意义 | 指针拖到窗口**外**时 `Mouse.current.position` 会报负值/超宽 ⇒ 原来会给出界外世界点（**下游拿它做命中判定 = 指着屏幕外却命中场上的东西**）。这正是 `Hand/CardInteraction.PointerWorldSafe` 那条「世界坐标超过可见区 ±1.5 倍就丢帧」启发式的**同族症状**（它的注释记着实测抓到过反推屏幕 x ≈ **-4127 px** 的一帧）——那边是**世界空间的事后过滤**，这边补的是**屏幕空间的源头守卫** |
| ⚠️ 顺手发现（**没动**） | 两者可以合并成一笔（`PointerWorldSafe` 的阈值启发式 → 源头守卫）——**但那个文件不在我白名单** ⇒ 只报 |

---

## 五、A660 —— 结算门动画期间冻住输入

| | |
|---|---|
| 判据（第一权威，逐句亲读） | `d:/2/tools/decomp_full/BattleManager._CloseBattleDoors_d__393__MoveNext.c`：`StopTracking(true)`（`:22`）→ 结果物件 `SetActive(true)`（`:26`）→ `BattleHud.ToggleWithAnimation(false,…)`（`:29`）→ `EndBattleDoors.SetupDoor(…)`（`:41`）→ **`TouchInputManager__Toggle(instance, 0, 0)`（`:50`）** → `yield WaitForSeconds(len)`（`:52`）→ （`:72`）`BattleManager.matchFinishedAndWaitingToLeave = true` |
| 改法 | ① 结算面板起播那处（`UpdateHud` 的 `_endPanel.Show(...)` 之后，**同一个 `!_settled` 闩**里）加 `if (TouchInputManager.Current != null) TouchInputManager.Current.Toggle(false);`；② `Begin()` 的「本局账清零」那一段后面加 `Toggle(true)`（**复位口**） |
| ⛔ 为什么不只靠 `Update()` 那道闸 | `BattleDriver.Update` 里 `if (Ctx.IsOver) { …只留按 R… return; }` **挡不住这一层**：`CombatCameraZoom.LateUpdate → TickBody` 是**它自己的 Update 循环**，每帧读 `TouchInputManager.ScrollDelta / TouchPressedSecondary / TouchDragDelta` ⇒ 不冻这一层的话**结算动画期间还能拖着镜头跑**（原版这时候已经冻了） |
| ⚠️ **我们加的**（原版没有的那一步） | ① **复位在 `Begin()`** —— 原版**不还**（`_CloseBattleDoors` 里只有 `Toggle(false)`），它靠「新一局 = 重进 `BattleScene` 场景」（`TouchInputManager` 是那个场景里的组件）自然复位；我们**复用同一个 driver**（`Restart()` 也只是再调一次 `Begin`）⇒ 不显式还的话「结算之后镜头永远拖不动」（静默）。**这一句是我们挑的做法，不是原版逐句** —— 已在注释里如实标注。<br>② 用 `Current`（只读口）+ 判空，**不用 `Ensure()`**：照原版那句 `if (instance != null)`（原版从不自己建），也免得在自检里凭空多出一个根物件 |
| ⚠️ 细节两条 | ① **不按 `video` 分支**：原版那句 Toggle 在 `SetupDoor` 之后、与片长无关 ⇒ 没有开门视频时**也冻**；② `Toggle` 的语义是「**停摆**」不是「清零」（`Battle/TouchInputManager.cs` 文件头 E）—— 我们照抄，⛔ 别顺手加清零 |
| ⚠️ **一支已知的连带（如实记，未改）** | 冻住之后那 8 个静态格**停在最后一帧的值**：若玩家在结算那一刻正按着键拖镜头，`TouchDragDelta` / `TouchPressedSecondary` 会停在非零值上 ⇒ `CombatCameraZoom.HandleManualControl` 每帧照用同一个 stale 值。**原版同构**（它也停 `Update`、也读同一批静态格）⇒ **不是我们引入的偏离**，但知道一下 |

---

## 六、顺手发现（⛔ 只报不改）

1. 🔴 **`BuildOvertimeSplash` 的 px 也是「0.1 px 精度」**（不在 A531 的点名范围、**也没动**）：
   它整段的数都来自**运行时 dump**（`runtime_ui_dump_drive_0912.tsv` 一行一位小数）——
   `716.6 / 699.9`（`:1011`，Background）· `2.0 / -981.0`（`:1012`，Text Container）· `80`（字号）·
   `181.3 / 187.3`（`:1014`，那颗图标）· `15`（图标左缘偏移）。**它是 `_hudExtras` 之外的另一件**
   （`HudExtraCount == 10` 里没有它）⇒ 按铁律 11 **要另立一笔账**；判据 = 原版那棵
   `FrontCanvas/Safe area FrontCanvas/AboveShader/OvertimeSplashText` 子树的 `RectTransform`（本件**没查 PathID**，如实标）。
2. 🔴 **`PA=1` 的「画多大」我们和原版不一样**（**同族 2 件**，判据齐、**属另一笔账**）：
   我们 `HudAbs` 那句「图按**高度**摆放」（`h` = 原版 **rect** 的 h），而原版 `m_PreserveAspect=1` 画的是**内接**尺寸：
   · `EnergyAccumulation`：图 102×102 **正方形**、rect 77.786×**80.113** ⇒ 原版实绘 **77.786×77.786**（竖向居中），我们 **80.113×80.113**（**大 2.99%**）；
   · `OffensiveButton`：图 128×124、rect 109.008×106.943 ⇒ 原版实绘 109.008×**105.6016**，我们 **110.393×106.943**（**高 1.34 px / 宽 1.39 px**）。
   ⚠️ 同一族里 `OvertimeIndicator`（68.625×70.9914 vs rect 68.625×70.992）**差 0.0006 px**、可忽略 —— 因为它的 rect 本来就是从贴图比例来的。
   ⛔ 本次**没改**（A531 讲的是「取整」；改这个会动到**实绘尺寸** ⇒ 是另一笔账，要单独判）。
3. 🟡 `Hand/CardInteraction.cs`：`PointerWorld()` 是 `BattleDriver.WorldPointer()` 的**同式副本**
   （`LayoutSpace.ScreenToWorld(PointerScreen(), cam)`，`:569-572`）+ 另一套**阈值启发式**防守（`PointerWorldSafe`）。
   A659 补的源头守卫**没覆盖到它**（那个文件不在我白名单）⇒ 建议合并成一笔。
4. 🟡 **同协程还有两件我们没有**（A660 没点名，**未做**）：
   ① `BattleHud.ToggleWithAnimation(false, …)`（`:29`，把 HUD 动画收起）；
   ② 协程末尾 `BattleManager.matchFinishedAndWaitingToLeave = true`（`:72`，字段 = `BattleManager` `+0x510`，
   dump.cs 亲读 `:30916`）—— 我们全仓 grep **零命中**。⇒ **要不要立账请调度台裁**。
5. 🟡 **`Editor/BattleScene.cs:8566-8588` 那条 A513 注释里的一句不精确**（**不是我的文件，没动**）：
   它写「画出来的是 **64.443×61.846**（h 只动了 0.004 px），命中区就是这一整个 rect」——
   `40k_UI_bt_voicelines` 是 **128×128 正方形** + `PA=1` ⇒ **宽受限**，实绘是**内接的 61.846×61.846**
   （本文件 `:2495` 自己那句就写对了：**「画出来的是**内接**的 61.846×61.846」** ⇒ 两处打架）。
   `ImageQuad.Contains` 比的是 `WorldW/WorldH`，而 `WorldW = WorldH × 贴图比例`（`Battle/ImageQuad.cs:108,528-533`）⇒ **命中区也是 61.846²**。
6. ✅ 顺手**改了**（铁律 5，同一文件里自相矛盾的两句）：`BattleDriver.cs` 里 A513 那段的同一句注释
   已就地订正成「内接的 61.846×61.846（不是 64.443×61.846）」并写明与 `:2495` 的关系。

---

## 七、没做完的（+为什么）

| 项 | 为什么 |
|---|---|
| **A531 的细口径断言**（A513 的另一半） | 宿主 = `Editor/BattleScene.cs` —— **不在我白名单**（简报列入「别的代理正在改」）⇒ 一行没碰。**成品已给**：§2.5 的期望值表（10 件全表，可直接粘）。 |
| **`At("EnergyAccumulation_Me", …)` 那一行**（改完会红） | 同上（宿主）。**改法一行**见 §2.6。 |
| A660 同协程那两步（`ToggleWithAnimation` / `matchFinishedAndWaitingToLeave`） | A660 只点名「冻住输入」⇒ 按「一件活一个代理」没扩面（§六·4 已记，请裁）。 |
| 真 Play / Unity 自检 | 简报红线：⛔ 不跑 Unity（铁律 12 攒批）。本件落点覆盖 `BattleScene.Run` ⇒ **同步点跑这一条**。 |
| `BuildOvertimeSplash` 的精确化 / `PA=1` 内接尺寸 | 都是**新账**（§六·1、§六·2），不属于 A531/A651/A659/A660 任一条 ⇒ 记账不扩面。 |

---

## 八、类型检查（`TMPDIR=/tmp/wf_b12 bash d:/4/Unity/工具/typecheck.sh`）

| 轮次 | 结果 |
|---|---|
| 第 1 遍（A531 十件改完） | **运行时 4 条**（**全在别人的在飞文件上**：`Shell/SocialWindow.cs` 的 `SetClip/Clip/ClipNow` 4 处，与本件无关）＋ **编辑器 2 条**（`Editor/RewardsScene.cs:927` / `Editor/RewardWindowFixture.cs:31` 找不到 `RewardWindow`）——**我的两个文件零条** |
| 第 2 遍（A651+A659+A660 全落完） | **0 / 0** ✅（别人那 6 条这时已经好了） |
| 第 3 遍（A660 改成 `Current` 判空 + 注释订正后） | **0 / 0** ✅ |

⚠️ 第 1 遍那 6 条**一条都不是我造成的**（全是别人正在写的文件），按简报口径**没有去改**、隔一轮重跑即恢复。
⚠️ 跑类型检查期间没有同时改 `.cs`（每遍都是「改完 → 再跑」）。

---

## 九、改动清单（文件 : 行 — 行号是**改后逐条现读**、`grep -n` 出来的）

| 文件 | 落点（行号） | 内容 |
|---|---|---|
| `Battle/BattleDriver.cs` | `:2091-2099` | A660 复位：`TouchInputManager.Current?.Toggle(true)`（判空写法；`Begin()` 的 `_settled/_replaySession` 之后） |
| 同上 | `:5283` · `:5291` | A651：`PointerDown()` → `PointerHeldRaw()`（2 个调用点：`BoardPress` / `skillPanel.SetPointer`） |
| 同上 | `:5366-5380` | A651：定义改名 `PointerHeldRaw()` + 新 doc |
| 同上 | `:8108-8131` | A531：`TitleBackground_Me`(`:8129`)/`Foe`(`:8131`) 4 个字面量 + 判据注释 |
| 同上 | `:8142-8154` | A531：`TitleText_Me`(`:8152`)/`Foe`(`:8154`) 中心 + 判据注释 |
| 同上 | `:8162-8206` | A531：`AvatarItemSmall_Me`(`:8204`)/`Foe`(`:8206`) 四个字面量 + 换算链注释（含 1.25 直读证据） |
| 同上 | `:8224-8232` | A531 合并 **A513 注释订正**（内接 61.846²，见 §六·5/6） |
| 同上 | `:8265-8270` | A531：`OffensiveButton` + 判据注释 |
| 同上 | `:8294-8304` | A531：`QPText_Me`(`:8302`)/`Foe`(`:8304`) 中心 + 判据注释（含「两颗 raw 逐位相同、差别全来自 holder」） |
| 同上 | `:8318-8339` | A531：`EnergyAccumulation_Foe`(`:8338`)/`Me`(`:8339`)（**含 §2.3 那笔真偏离**）+ 订正「同一个相对位置」那句 |
| 同上 | `:8344-8355` | A531：`OvertimeIndicator`(`:8355`) + 判据注释 |
| 同上 | `:9040-9062` | A660：`_endPanel.Show(...)`(`:9040`) 之后 `Toggle(false)`(`:9062`) + 整段判据注释 |
| 同上 | `:9287-9304` | A659：`WorldPointer()`(`:9287`) 里的屏幕内守卫(`:9304`) + 判据注释 |
| 同上 | `:9424-9430` | A651：`PointerHeld()` 改成一句转发 + doc 订正 |
| `Core/LayoutSpace.cs` | `:92-98` | A659：`ScreenToWorld` 的 doc 补「纯换算器、⛔ 别在这儿拦」 |
| 同上 | `:107-122` | A659：新增 `IsInsideScreen(Vector2)`(`:122`)（判据 + 批量下的恒真说明） |

`git diff --numstat`：`BattleDriver.cs` **147/23** · `LayoutSpace.cs` **27/1**（行尾未翻：前者 CRLF、后者 LF，改前改后一致）。

---

## 十、一句话给调度台

**A531 的 10 件 + A651 + A659 + A660 都改完了、类型检查 0/0**；接下去只剩三件**不在我手上**的收尾：
① `Editor/BattleScene.cs` 加 10 条细口径断言（期望值表见 §2.5）；
② 同一文件把 `At("EnergyAccumulation_Me", 1785.6f, 555.65f)` 改成 `1787.46f, 555.6f`（**不改会红**，§2.6）；
③ 同步点跑 `BattleScene.Run`（本件全部落点都在它的覆盖面里）。
