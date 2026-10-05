# H8 · A421（`sensorSize.x` 那一半）+ A424（战斗内设置面板的 `Auto Zoom` 行）（2026-10-12）

> 本件 = 写手代理 H8。**没跑 Unity 自检**（简报口径：A 表清完再跑）· ⛔ 没动 git · ⛔ 没改两张正本。
> 类型检查（`TMPDIR=/tmp/wf_h8 bash 工具/typecheck.sh`）**运行时 0 / 编辑器 0**（共 6 遍，最后一遍落地后）。

## 一、结论

| 件 | 结论 |
|---|---|
| **A421** | ✅ **做了**。`CameraVerticalFramer.CalculateFraming` 的 `sensorSize.x` 那一半（曲线 `cameraSizeXTable`）从「没接」变成**真落进相机**；自变量 = **现量**的「敌方区下沿 ↔ 我手牌区上沿」世界高差，经 `Mathf.Min(\|Δ\|, 7.0)` 与 `(origX − curve) × clamp01(zoom) + curve`。**判据全是实读**：13/13 场 `cameraSizeXTable`/`maxVerticalSizeInViewPort` **逐字节相同**（md5 `ea3a0df859c4`，已核）· 公式与 `z` 的取法来自**指令流**（Ghidra 的 `.c` 在这里把两个操作数认错了）。 |
| **A424** | ✅ **做了**。`Battle/SettingsPanel.cs` 补上原版那一行（照原版几何/文案/图/染色），点它 = **与菜单那颗逐字同一条链**（`AutoZoom.Set` + `FindFirstObjectByType<CombatAutoZoom>().ForceRefresh()`）。 |
| 顺带（铁律 5·b） | 🔴 **查出一条真缺陷**（`CombatAutoZoom` 的两条曲线切线与原版资源不符，连带 A175 那两条期望值也是照错曲线算的）—— **只报不改**，见 §五·1。 |
| 顺带（铁律 8） | 为了让那一行在新机器上也有图，往 `工具/import_original_art.py` 的 `MENU_FROM_ART` **加了 1 条**（见 §二·5 的理由：`Resources/Art/` 整块在 `.gitignore` 里）。 |

---

## 二、改动清单（行号 = **本件改完之后的现读值**）

### 1. `Battle/CombatAutoZoom.cs`（LF；文件已存在，H1 新建，尚未进 git）

| # | 行号 | 为什么 |
|---|---|---|
| 1 | `:39-33`（文件头 B 条） | 把「`sensorSize.x` 仍然没接」改成**已接**，并写清判据（6 键曲线 / `+0x78` / 指令流地址）。 |
| 2 | `:336-362` | `ApplyFraming()`：原来只写 `lensShift.y`，现在**也写 `sensorSize.x`**（只写 `.x`，`.y` 一个字节不碰 —— 原版 `newSensorSize` 就是从 `get_sensorSize()` 拿的当前值、只改 `.x`）。 |
| 3 | `:478-505` | 🆕 「A421」那一节的**判据头**：那一段的指令流逐条（abs 掩码 / `Math.Min` / 存 `+0x7c` / 取 `+0x38` / 夹 zoom / 写回 `*out.x`）+ 两个 helper 的世界 Y + `z` 的取法（`InverseTransformPoint(场地根)`）。 |
| 4 | `:510` | `MaxVerticalSizeInViewPort = 7f`（原版 `+0x78`；13/13 相同）。 |
| 5 | `:517-528` | `OriginalCameraSizeXCurve()`：原版 **6 个键**，**连切线一起**照抄 `MonoBehaviour_4697.json`；显式 `WrapMode.Clamp`（= 原版 `m_PreInfinity/m_PostInfinity = 2`）。 |
| 6 | `:533-552` | `EvaluateCameraSizeX` / `BoundsVerticalSizeInViewport`（= `Min(\|Δ\|, 7)`）/ `FrameSensorSizeX`（= `(origX − curve) × clamp01(zoom) + curve`）—— **三个纯函数**，判据都标了指令流地址。 |
| 7 | `:560-580` | `OriginalSensorSizeX()`：= 原版 `framer+0x98` 那格**缓存值**，含「≈ 0 才从相机重抓」那条原版规则（阈值 `1e-10` = `DAT_1834b2ba8`）；**外加**一条我们自己的「换相机就重抓」（原版一局一份场景，没这个问题）。 |
| 8 | `:588-600` | `CurrentSizeMultiplier` = **1.0**（原版 `PlayerHand.get_CurrentSizeMultiplier` 的「`smallScreenUI` 关」那一支）—— 🔴 **如实标了「我们战斗 HUD 没有那个手牌放大档」**，⛔ 不拿 `SmallScreenUI.Enabled` 冒充 M。 |
| 9 | `:607-655` | `MeasureCardAreaGapWorld()`：那两条 viewport 点的完整链（两个 helper 边 → `LayoutSpace.Cam.WorldToViewportPoint` → `boardCamera.ViewportToWorldPoint(vp, z)` → 相减）。🔴 **量之前先把相机换成 `originalSensorSize`、量完还原** —— 不还原会**自激**（`sensorSize.x` 改世界尺度 ⇒ `bounds` 变 ⇒ `sensorSize.x` 又变，那一圈不收敛）。 |

**为什么 `z` 用 `InverseTransformPoint(Vector3.zero)`**：原版那一格字面量是 `DAT_1834b2e08 = **100.0**`（`0x180608A0E` 直读）而 100 = 原版场地根 x；我们整体平移了 −100（`BattleScene.ArenaOriginX`）⇒ 等价点 = `Vector3.zero`；相机朝向不变 ⇒ z 恒等（两边都 13.57198）。

### 2. `Battle/SettingsPanel.cs`（LF）

| # | 行号 | 为什么 |
|---|---|---|
| 10 | `:12-17`（文件头） | 补上那一行的出处（原版节点名 / 字段名 / 逐份实读的 JSON）。 |
| 11 | `:52-58` | 新字段 `_azBox` / `_azCheck` / `_azLabel` / `_autoZoomHeld`。 |
| 12 | `:107-152` | 新常量：行 `ap(−282.42, 249.48)` `sd(472.46, 75.641)` `pivot(0,0.5)` · 勾选框中心 `(−245.389, 249.4795)` / `74.0616×57.6656` · 文字左中 `(−203.42, 249.4795)` / 宽 `229.291` · 染色 `(0.2862745,0.9647059,0.6862745)`（`Selectable.m_Colors.m_NormalColor`）· fs `42` · **命中区两块** `AzHitPx`。 |
| 13 | `:304-343` | `Build()` 里建那一行：底图 `CardArt.MenuUi("40k_dropdown_bg")` + 勾 `CardArt.MenuUi("40K_settings_icon_checkmark")`（两张都按原版那格 `preserveAspect` **内接**）+ 文字 `"Auto zoom"`（原版 TMP 原文，**小写 z**）fs42 按拉丁大写高度定字号。 |
| 14 | `:404-434` | `PointerFrame()`：**按下那一帧**命中这一行 ⇒ 翻值并**接住**这一帧（不再当点击转给按钮）。理由与「为什么不在 `BattleDriver.SettingsClickAt`」写在注释里（那个文件不在本件白名单）。 |
| 15 | `:397-403`（`SetDifficulty` 之后新增的一段） | `ToggleAutoZoomFromPanel()`（= 写值 + `ForceRefresh()` 那一跳，**与菜单那颗同源**）· `RefreshAutoZoomCheck()` · `HitAutoZoom()`。 |
| 16 | `:559-575`（`SetActive`） | 面板开关时带上 `_azBox` / `_azLabel`；**勾那一层**走 `RefreshAutoZoomCheck()`（= 面板开着 **且** 开关开着）。 |
| 17 | `:604-630` | `HasArt` 加上那两张图；新增自检口 `AutoZoomRowBuilt` / `AutoZoomCheckShown` / `AutoZoomLabelText` / `AutoZoomBoxWorldPos` / `AutoZoomLabelWorldPos` / `AutoZoomBoxDrawnSize` / `AutoZoomCheckDrawnSize` / `AutoZoomBoxTint`；两个公开常量 `AutoZoomLabelEn` / `AutoZoomTermKey`。 |

### 3. `Editor/BattleScene.cs`（CRLF 11301/11301，**一处没翻**）

| # | 行号 | 为什么 |
|---|---|---|
| 18 | `:6544-6640` | **A424 断言一节**（14b3，插在既有 14b2 之后、14c 之前）。五条 + 收尾。 |
| 19 | `:9665-9800` | **A421 断言一节**（A175 那个块里的第 ⑧ 组），纯函数三条 + 现量两条 + 两态一条。 |

### 4. `工具/import_original_art.py`（CRLF 1511/1511）

| # | 行号 | 为什么 |
|---|---|---|
| 20 | `:708-714` | `MENU_FROM_ART` 加 `'40K_settings_icon_checkmark'`（源 = 已进 git 的 `Art/原版/去重资源/40K_settings_icon_checkmark.png` → `Resources/Art/ui_menu/`）。🔴 **必须走这里**：`Resources/Art/` **整块在 `.gitignore` 里**（`.gitignore:91`）⇒ 光把 PNG 拷进去，新机器上 `CardArt.MenuUi` 会取到 null、那一行画不出来。底图不用加（`ui_menu/40k_dropdown_bg.png` 早就在，且与切片库那张 **同 md5** `2e40bc4d1923680c93fef6d42445aea4`）。 |

### 5. 资源（**未进 git，gitignore**）

- 新落 `Resources/Art/ui_menu/40K_settings_icon_checkmark.png`（4433 B，源同 §二·4）+ 一份 `.meta`（guid `fe103852c30f4b26a012d2ef14bddf6f`）。
  ⚠️ **先落进了 `Resources/Art/ui/` 又撤掉**（走了 5 分钟弯路）：`CardArt.Ui` 走的是 `Art/ui/`（**战斗那批**），而这张图属于「工程切片库 → 菜单那批」那条路；按本仓既有分工，`Art/ui/` 里的图由 `UI_IMAGES`（源 = `battleatlasui/sliced`）产，**没有**这张。

---

## 三、断言（逐条 + 改坏法）

### A421（`Editor/BattleScene.cs:9665-9800`）—— 期望值**手算自原版那 6 个键**

算式（断言里自己写了一遍 Hermite，⛔ 不读被测实现）：键 `(4.930829, 41.084930, m=-8.180681) (5.591578, 35.679554, -8.840356) (5.856215, 33.340069, -4.210847) (7.665596, 25.721041, -2.655147) (8.940310, 22.336491, -1.935942) (11.454760, 17.468660, -2.361029)`（原版这 6 键满足 `outSlope[i] == inSlope[i+1]` ⇒ 一段一个切线）。**`cameraSizeXTable(7.0) = 28.523765`**。

| # | 断什么 | 🧨 改坏了会不会红 |
|---|---|---|
| ① | 曲线 = 原版 **6 个键**（逐键比原版字面量；两端 `Evaluate(0) = 41.08493` / `Evaluate(20) = 17.46866` = Clamp）+ `MaxVerticalSizeInViewPort == 7` | 任何一个键的 time/value/切线抄错 ⇒ 红 |
| ② | `Bounds` = `Min(Abs(Δ), 7)`：`7.6837 → 7` · `−3.2 → 3.2` · `7 → 7` | 去掉上夹 ⇒ 第 1 条红；忘记取绝对值 ⇒ 第 2 条红 |
| ③ | `FrameSensorSizeX`：`zoom = 1` **恒等** · `zoom = 0` = 曲线值（= **28.523765**，手算）· `zoom = 2 / −1` 被 `Clamp01` | 曲线错 / 忘夹 zoom ⇒ 红 |
| ④ | **现量**：`MeasuredWorldDeltaY` ≈ 本文件独立算一遍的链（`z` · `2·z·(sx/aspect)/2/28` · Δvp，`sx` 从**清单**读原版值）+ `Bounds == Min(\|Δ\|, 7)` | `MeasureCardAreaGapWorld` 没接 / `z` 取错 / 拿修改后的 `sensorSize` 去量 ⇒ 红 |
| ⑤ | **两态**：关 ⇒ 相机 `sensorSize.x` = 原版那一档（41.5 / 37.2，且 `sensorSize.y` 仍是 24）· 开 + 人少 ⇒ `= 曲线值`（与原档差 > 0.5）· 再关 ⇒ 回到原档 | `ApplyFraming` 只写 `lensShift` 不写 `sensorSize.x` ⇒ 红；`zoom = 1` 那支不恒等 ⇒ 红 |

⛔ **没造的**（如实）：原版 `CalculateFraming` 还会把 `originalSensorSize` / `originalLensShift` **写回相机**（`0x1806089DB/0x180608A05`）并在结尾把进函数时那个 `sensorSize` 还原 —— 我们的等价物只做**量之前换、量完还原**（`lensShift` 那半在差分里抵消，所以没做），**没有**照抄「把缓存值写进相机」那一半（那是给 `CombatCameraZoom` 的平滑用的，我们没做平滑 —— 文件头 D）。
⛔ **`x` 与 `y` 的差别**：原版缓存的是 `Vector2`，我们只缓存 `.x`（`.y` 我们从不改）。

### A424（`Editor/BattleScene.cs:6544-6640`）—— 期望值 = 原版面板内 px 字面量

| # | 断什么 | 🧨 改坏法 |
|---|---|---|
| ① | 那一行三件都建出来了、文字 = `"Auto zoom"`（原版 TMP `m_text`，**小写 z**） | 文案写成 `Auto Zoom` ⇒ 红 |
| ② | 两块的世界位置（**只比面板局部 x/y**）= 面板内 px `(−245.389, 249.4795)` / `(−203.42, 249.4795)`，容差 `1e-4` 世界单位（≈ 0.0108 px） | 少乘/多乘一个偏移 ⇒ 红 |
| ③ | 两张图都**内接**进原版那格 `74.0616 × 57.6656`（「放得进」**且**「至少贴满一边」= uGUI `preserveAspect`） | 只给高不内接（`ImageQuad.Create` 直传高）⇒ 底图会宽出 74.06（实测 67.28 vs 74.06 那一档）⇒ 红 |
| ④ | 命中：勾选框中心 ✓ · 文字中心 ✓ · **两块中间那道 4.94px 的缝 ✗** | 命中区写成「整行」⇒ 缝那条红 |
| ⑤ | **点一下** ⇒ 值真的翻了（`AutoZoom.Enabled` false → true）、**按住不放不重复翻**、勾那一层跟着亮 | `PointerFrame` 里不接这一行 ⇒ 红；不写 `_autoZoomHeld` 的边沿 latch ⇒ 「不重复翻」那条红 |
| ⑥ | **当场重算**：`CombatAutoZoom.FramingApplyCount` 增长（= `ForceRefresh()` 真的被调了） | 那一行只写值不重算 ⇒ 红（**这条是 A424 的命门**） |
| ⑦ | 收尾：放回玩家原值 + **显式 `ForceRefresh()` 把相机放回那一档**（本节后面还有一大堆量同一台相机的断言） | 忘了还原 ⇒ 13d 那组「卡在屏内」会随「这次点到哪一档」红绿 |

⚠️ **`BattleDriver.SettingsClickAt` 那条链没被用到**（见 §四·2）：本件那一行的指针入口是 `SettingsPanel.PointerFrame`。

---

## 四、没查清 / 待判据（⛔ 不猜）

1. 🔴 **`PlayerHand.CurrentSizeMultiplier` 那一档（`smallScreenUI` 真时 `PlayerHand+0x3c` 的值）没查到** —— 我们战斗 HUD 也没有「手牌区放大」那条路（`TransformScalerBySmallScreenUI` 的生产 `AddComponent` 调用点只在菜单窗口那两处）⇒ 现在**两档都返回 1.0**，代码里如实标了。**留钩子**：等查到 M 时，改 `CombatAutoZoom.CurrentSizeMultiplier` 一处即可（`bounds` 与 `sensorSize.x` 自动跟着走）。
2. ⚠️ **A424 那一行的触发时机 = 按下那一帧**（原版 `Toggle`/`Selectable` 是 `IPointerClickHandler`，**抬起**时）。落点选在 `SettingsPanel.PointerFrame` 是**文件所有权**逼出来的（`BattleDriver.cs` 是 H7 的，⛔ 本件不碰）。**要挪到那条链只需一行**：在 `BattleDriver.SettingsClickAt` 里 `HitClose` 那几行旁边加 `if (_settingsPanel.HitAutoZoom(w)) { _settingsPanel.ToggleAutoZoomFromPanel(); return true; }`（**归 H7 / 调度台**；加了之后本件 `PointerFrame` 里那一段可以删）。
3. ⚠️ **`bounds` 的两条 helper 边用的是「原版运行时 dump 那两个矩形」（96.7 / 249.9 px）**，不是我们自己 HUD 的实测几何 —— 与原 `LensShiftY` 同源。原因：原版那两个 `RectTransform` 我们**没有**（全线是世界空间 mesh）。⇒ 若将来我们手牌区的位置/高度**偏离**原版那两条，这一格不会跟着发现（**同 `LensShiftY` 的既有风险**，不是本件新引入的）。
4. ⚠️ **`smallScreenUI` 与 `sensorSize.x` 的交互没在实况里看过**：`smallScreenUI` 开 ⇒ zoom 上界掉到 ~0.305（16:9）⇒ 这一半也会走到非 1 档。自检里 `SmallScreenUI` 被压成「关」⇒ **那条组合没验**。
5. ⚠️ **原版 `CombatCameraZoom` 整件仍然没做**（手动缩放 / 拖拽 / 世界边界 / 平滑 / 回弹）—— 本件只是把 `CalculateFraming` 的**两半都接上**。它每帧 `ApplyZoom` 的平滑（`SmoothDamp`）我们不做（批处理没有帧循环，直接落到目标值，同文件头 D）。

---

## 五、顺手发现（⛔ 只报不改）

1. 🔴🔴 **`CombatAutoZoom` 的两条曲线切线与原版资源不符 ⇒ 取景本来就偏了一点；而 A175 那两条「手算」期望值正是照这两条错曲线算的。**
   - **判据（原版字面量，13/13 场逐字节相同）**：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4697.json`
   - 🔴 **两份解包**（旧 `d:/2/解包整理/07_场景/battlearena1/…` 与 `d:/2/新解包资源/…`）**逐值相同** ⇒ 这不是「新旧解包不一致」，是**当年照抄时抄错了**（旧解包那份也写着 `−0.674347 / −0.558506 / −0.910357 / −1.601710`）。
   - **`viewShiftModifier`**：原版 `outSlope[2]=−0.6743468` / `inSlope[3]=−0.6743468`、`outSlope[3]=−0.5585060` / `inSlope[4]=−0.5585060`、`outSlope[4]=−0.9103566` / `inSlope[5]=−0.9103566`、`outSlope[5]=−1.6017104` / `inSlope[6]=−1.6017104`；我们代码里写的是 `−0.972000 / −0.593500 / −0.997100 / −1.129900`。另外最后一键的 time 原版是 `0.7067446112632751`、我们写 `0.7067f`。
   - **`verticalPaddingModifierByAspectRatio`**：原版 `outSlope[2] = inSlope[3] = −0.6183817982673645`；我们写 `+0.051700f`（**符号也反了**）。这一档只在宽高比 > 1.77 时被采 ⇒ 16:9 看不出来。
   - **后果（实算）**：`LensShiftY(16:9, 1)` 原版曲线 = **−0.210265**、我们的曲线 = **−0.207943**（Δ = **0.00232**）；`LensShiftY(16:9, 0)` 原版 = −0.250793、我们 = −0.250551（Δ = 0.00024）。
   - 🔴 **A175 那两条期望值（`OffFrameA175 = −0.207943` / `OnFrameA175 = −0.250551`）= 照**我们这条错曲线**算出来的**（我在本件里用 Python 逐段重算过：拿 `CombatAutoZoom` 里那 5 组数算就先出 −0.207943）⇒ 那两条断言是**钉实现**、不是钉原版（形式上像「手算自原版」，实际抄的是实现里那份）。
   - **要不要改 / 为什么本件没改**：改 `ViewShift`/`PadModByAspect` 会让 `BoardLensShiftY()` 变 ⇒ **烘在 `Battle.unity` 里的 `lensShift.y` 对不上**（`BattleScene.cs:9788` 那条 `|bcam.lensShift.y − BoardLensShiftY()| < 0.001` 会红），必须先跑一次 `BattleScene.BuildAndSaveScene` 重烘场景；同时 A175 那两条期望值要一起改成原版曲线算出来的数。**这是一件要连场景一起重烘的活**（Unity 批处理全局串行、本件不跑），⇒ **单开一笔账**，判据就是上面这几个数。
2. 🟡 **`SettingsPanel.HitBlank()` 是死代码**：全仓 0 个调用点（`BattleDriver.SettingsClickAt` 走到最后直接 `return true`「点面板别处：吃掉」，没调它）。不是本件引入的，顺手报。
3. 🟡 **`Resources/Art/` 整块在 `.gitignore` 里**（`.gitignore:91`）⇒ **任何新加的战斗/菜单图都必须同时挂进 `工具/import_original_art.py` 的对应清单**，否则只在本机有效。本件踩过一次（先落 `Art/ui/` 再撤）。判据链：`CardArt.Root = "Art/"` + `Resources.Load`。
4. 🟡 **`40k_dropdown_bg`（`ui_menu/`，小写 k）与切片库里的 `40K_dropdown_bg.png` 逐字节相同**（md5 `2e40bc4d1923680c93fef6d42445aea4`）—— 所以那一张**不用重导**。
5. 🟡 **类型检查期间没出现过「只在别人文件里」的红**（本件 6 遍全 0/0）—— 与本轮 H1 报告的 §五·6 不同，说明那两处在飞文件已经落地。
6. 🟡 **`资料/普查产出_1012/` 目录仍是 untracked**（未进 git），本件报告也落在里面。

---

## 六、跑过的检查

| 检查 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_h8 bash d:/4/Unity/工具/typecheck.sh`（6 遍） | 最后一遍 **运行时 0 / 编辑器 0** ✅（中间一次红是**我自己**漏声明一个字段，当场修掉） |
| 行尾（python **二进制**读） | `SettingsPanel.cs` LF 635 · `CombatAutoZoom.cs` LF 660 · `BattleScene.cs` CRLF 11301 / LF 11301（**纯 CRLF，一处没翻**）· `import_original_art.py` CRLF 1511/1511 · 新 `.meta` LF 117 |
| 13 场资源核对 | **13/13 逐字节相同**（`cameraSizeXTable` + `viewShiftModifier` + 两条 padding 曲线 + `maxVerticalSizeInViewPort` 一起算 md5 = `ea3a0df859c4`；`maxVerticalSizeInViewPort = 7.0`；`m_PreInfinity = m_PostInfinity = 2`） |
| Unity 自检 | ⛔ **一条都没跑**（简报口径）。本件落点覆盖 **`BattleScene.Run`**（A421 在 A175 那个块里 · A424 在 §14）⇒ **同步点跑这一条** |
| git | ⛔ 没动（只读 `status` / `diff --numstat` / `ls-files` / `check-ignore`） |
