# A168 · 设置窗「FPS 上限」照原版改成【滑块】

2026-10-06 · 执行子代理 · **SettingsScene 这一类的最后一件**
· 改了 2 个文件（`Shell/SettingsWindow.cs` · `Editor/SettingsScene.cs`）· **没跑 Unity / 没跑自检**（自检由主对话在同步点跑）
· 没动 git · 没改两张正本 · `d:/2/` 只读

**判据来源**（全部当场实读，不引用别人转述）：
· 反编译 `d:/2/tools/decomp_full/{GraphicsTab__FPSLimitValueChanged,GraphicsTab__OnEnable,GraphicsTab__OnSetup,PlayerDataManager__ApplySettingsOptions,PlayerDataManager__ApplyGraphicsQuality,PlayerDataManager__SetTargetFramerate}.c`
· 签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{GraphicsTab,GameStaticData}.cs`
· 解包 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`：`MonoBehaviour_5246199328614809510.json`（= `GraphicsTab` 实例）·
  `MonoBehaviour_5646828332852936614.json`（= `fpsLimit` 那颗 Slider）· `GameObject/FPS Slider.json` ·
  `RectTransform_{466260660320960422,8773905529524617126,4475875889173987238,-7007267354949943386,2192545133913604006,7067171244513066918,-7534402245209653338}.json` ·
  四颗 Image（`-3786787164003336282` Background · `-1239832872394260570` Fill · `-3648122393879609434` Handle）
· `python 工具/menu_dump.py bundle_menus_assets_all "Graphics Tab" --depth 6 --relative`（行/视口/内容）与
  `… "FPS Limit" --depth 3 --relative --no-ancestor-scale`（行内摆法）
· 旁证：Unity 自带源码 `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UGUI/UI/Core/{Slider.cs,Image.cs}`

---

## 结论

**判据查全了，做完了**：原版这一格是 **`UnityEngine.UI.Slider`**（不是勾选行）——
`m_MinValue 0` · `m_MaxValue 2` · `m_WholeNumbers 1` ⇒ **只有 0/1/2 三档**，刻度就是它子件上印的 `30` / `60` / `Unlimited`；
值经 `GameStaticData.FPSLimit` 再映射成 **30 / 60 / −1（不限帧）** 写进 `Application.targetFrameRate`。
本件把原来「点击在 60/30/无限之间循环的勾选行」换成**照原版几何与贴图的滑块**（轨道/填条/手柄 + 三个刻度 + 行标题），
点击（含拖）取值规则逐句照 uGUI `Slider.UpdateDrag` + `m_WholeNumbers`。

🔴 **一处必须声明的偏离**：原版那一列是**可滚的 `Scroll View`**，FPS 那一行**要滚到底才看得见**（不滚时滑块整根在视口外）；
我们这一页**没有滚动视图** ⇒ 本件把第 **5、6** 两行整体**上移 67.34 设计 px**（= 原版那一列的**滚动范围**，内容高 588.84 − 视口高 521.50）
＝「按原版滚到底的那一帧摆」。⛔ 不上移的话滑块会画到**弹窗底边之外**（旧勾选行**今天就已经掉出去**，见 §顺手发现 ⑥）。
完全复刻 = 给这一页补 `Scroll View`（另开一条，判据已在本报告里备齐）。

---

## 步骤 0：原版滑块的判据（逐字段 + 出处）

### ① 组件与层级

| 项 | 值 | 出处 |
|---|---|---|
| 类 | `GraphicsTab : WindowTabBase<SettingsMenu>` | `Assembly-CSharp/GraphicsTab.cs:5` |
| 字段 | `[SerializeField] private Slider fpsLimit;` | `GraphicsTab.cs:31-32` |
| 回调 | `FPSLimitValueChanged(float fpsValue)`，`OnEnable` 里挂到 `fpsLimit` 的 `m_OnValueChanged` | `GraphicsTab.cs:70-72` · `GraphicsTab__OnEnable.c:77-84`（`fpsLimit`@`+0x70` → `*(+0x128)` = `m_OnValueChanged`） |
| 实例 | `MonoBehaviour_5246199328614809510.json` 的 `fpsLimit` = PathID **5646828332852936614** | 实读 |
| GO 名 | **`FPS Slider`**（GO 的两个组件 = RectTransform `466260660320960422` + 这颗 Slider） | `GameObject/FPS Slider.json` |
| 父链 | `FPS Slider` ← **`FPS Limit`**（行）← `Content`(VLG) ← `Viewport`(RectMask2D) ← `Scroll View`(ScrollRect) ← `Graphics Tab` | `menu_dump … "Graphics Tab" --depth 6` |

### ② 滑块的字段（`MonoBehaviour_5646828332852936614.json` 逐字段实读）

| 字段 | 值 | 含义 |
|---|---|---|
| `m_MinValue` / `m_MaxValue` | **0 / 2** | 两档刻度 + 一档中间 |
| `m_WholeNumbers` | **1** | ⇒ 值只能是 0、1、2（uGUI `Slider.Set`：`Mathf.Round` 再 `Clamp`） |
| `m_Direction` | **0** = LeftToRight | 左 = 0 = `30` |
| `m_Value` | 0.0 | 出厂停在最左 |
| `m_Transition` / `m_Colors` | 1(ColorTint) / 默认白 | 手柄按下会变色（我们不做悬停变色，同本窗其它件） |
| `m_TargetGraphic` | `-3648122393879609434` = **Handle 那颗 Image** | —— |
| `m_FillRect` / `m_HandleRect` | `8773905529524617126`(Fill) / `4475875889173987238`(Handle) | —— |
| `m_Interactable` / `m_OnValueChanged` | 1 / 空持久调用（运行时由 `OnEnable` 挂 `FPSLimitValueChanged`） | —— |

### ③ 行内几何（**未缩放**设计 px；`--no-ancestor-scale` 的 dump + 原始 RT 互校）

| 节点 | 行内矩形（相对 `FPS Limit` 左上角） | 尺寸 | 出处 |
|---|---|---|---|
| `FPS Limit`（行） | [0,0]–[455.21,105] | 455.21 × **105** | RT 实读；屏上 409.69 × **94.50** |
| `Title` | [16,−14]–[325.6,48] | 309.55 × 62 | 🔴 **顶是负的**（探出行顶 14 px） |
| `FPS Slider` | [266,84.2]–[757.2,97.2] | **491.18 × 13** | 锚 `(0.5,0.5)`、`m_AnchoredPosition (284,−38.2)` |
| `Background` | 同 `FPS Slider`（锚 0,0–1,1） | 491.18 × 13 | —— |
| `Fill` | 同（锚 `(0,0)–(值,1)`，运行时驱动） | 值 0 时宽 **0** | 原始 `m_SizeDelta (0,0)`、`anchorMax.x` 由 Slider 写 |
| `Handle Slide Area` | [266,84.2]–[747.2,97.2] | **481.18** × 13（`m_SizeDelta.x = −10`） | —— |
| `Handle` | 框 46.811 × 35.406（锚 y 0→1 被轨道撑开 + `22.406`） | 实画 **35.406 见方** | `preserveAspect=1` + 110×110 方图 ⇒ `min(46.811,35.406)` |
| `30 FPS` / `60 FPS` | x **163.0** / **403.9**，y **27.1**，框 228.02 × 62 | 居中 fs 42 灰 (0.745,0.745,0.745,1) | TMP `m_text` = `'30'` / `'60'` |
| `Unlimited` | x **640.0**，y **22.2** | 同上 | TMP `m_text` = `'Ilimitado'`（**本地化词条** + `Localize` 件） |

🔴 三个刻度的 x 是**相对行左沿**的（不是相对滑块左沿）；滑块自己在行内是 266.0（两个数不同源，⛔ 别混）。
🔴 `Unlimited` 那个框比前两个**高 4.9 px**（`m_AnchoredPosition.y` 37.49998474 vs 32.60000229）—— **原版自己就不齐**，照抄。

### ④ 贴图与切片（四颗 Image 的字段实读）

| 件 | sprite | `m_Type` | `m_PixelsPerUnitMultiplier` | border（贴图 px） | 画出来的端帽 |
|---|---|---|---|---|---|
| `Background` | `Volume_bar_inactive` 400×31 | 1 = Sliced | **2.0** | 184,0,184,0 | **92** 设计 px（184÷2） |
| `Fill` | `Volume_bar_active` 64×31 | 1 = Sliced | **2.0** | 30,0,30,0 | **15** 设计 px（30÷2） |
| `Handle` | `Volume_button` 110×110 | 0 = Simple | 1.0 | — | `preserveAspect=1` ⇒ 35.406 见方 |
| （`m_Color` 三件全白 · `m_RaycastTarget` 全 1） | | | | | |

判据（端帽 = `border ÷ ppuMul`）= uGUI `Image.GenerateSlicedSprite`：`GetAdjustedBorders(border / multipliedPixelsPerUnit, rect)`（`Image.cs:1157`）。
`preserveAspect` 的算法 = `Image.GetDrawingDimensions`：`spriteRatio = 1 < rectRatio = 46.811/35.406` ⇒ 取 else 分支 `r.width = r.height × spriteRatio`。

### ⑤ 取值集合与「值 → 帧率」

* `GraphicsTab__FPSLimitValueChanged.c`（**全文 20 行**）只做两件事：`GameStaticData.FPSLimit = (int)param_2`（字段在 `GameStaticData` 实例 `+0x128`）+ 置存盘脏位。
* 真正的映射在 **`PlayerDataManager__ApplySettingsOptions.c`**（`__ApplyGraphicsQuality.c` 里那一段**逐字节同形**）：

```c
iVar1 = *(int *)(GameStaticData_inst + 0x128);          // FPSLimit
if (iVar1 != 0) {
    if (iVar1 != 1 && iVar1 == 2) { SetTargetFramerate(0xffffffff); return; }   // −1 = 不限帧
    SetTargetFramerate(0x3c); return;                                          // 60
}
SetTargetFramerate(0x1e);                                                      // 30
```

⇒ **0→30 · 1→60 · 2→−1**，🔴 **其它任何非 0 值都落到 60**（⛔ 不是「不是 1 就给无限」）。
* `PlayerDataManager__SetTargetFramerate.c` 最后一行 = `Application.targetFrameRate = 值`（带一行 `CustomDebug.Log`）。
* 开窗时的初值：`GraphicsTab__OnSetup.c` 结尾拿 `fpsLimit` 走一个 vtable 调用把当前值填上（与它旁边 `Toggle.SetIsOnWithoutNotify` /
  `Dropdown.SetValueWithoutNotify` 同族 ⇒ 应是 `SetValueWithoutNotify`，⚠️ 详见 §没查清 ①）。

### ⑥ 行所在的那一列（为什么必须动位置）

`Scroll View` [551.52,432.50]–[1538.89,954.00] → `Viewport`(RectMask2D) 内层 = **432.50–954.00**（高 **521.50**）；
`Content`(VLG, spacing 5) 的 7 行 = 6 × 75.64 + **105** + 6 × 5 = **588.84** ⇒ **滚动范围 = 67.34 设计 px**。
行顶（相对 `Graphics Tab` 左上，屏 px）= 278.5 / 351.0 / 423.6 / 496.2 / 568.8 / 641.3 / **713.9**（第 6 行底 808.4）。
FPS 那一行的**块**（`Title` 顶 行顶−14 → 滑块底 行顶+97.2）在原版**不滚动时整块在视口外**：视口底 954.00 < 行顶+97.2 = 1021.42。

---

## 🔴 修正（2026-10-06 首次复跑之后 · 3 条红全在「档 1」）

**复跑结果**：`SettingsScene.Run` → **通过 203 / 失败 3**，3 条全落在「档 1（`60 FPS`）」那一档。
（每条在日志里出现两次 = `Check` 的**行内 `LogError`** + 收尾那张失败清单，不是两条断言 —— 见 `Editor/SettingsScene.cs:35-45`。）

**根因（实现错，一行）**：`Shell/SettingsWindow.cs` 的 `PlaceFps` 里

```csharp
float n = (idx - FpsMin) / (FpsMax - FpsMin);      // ❌ 三个都是 int ⇒ **整数除法**
```

`FpsMin/FpsMax` 是 `int` 常量、`idx` 是 `int` ⇒ `(idx-0)/(2-0)` **按整数除**：

| idx | 整数除法的结果 | 应该的值 | 后果 |
|---|---|---|---|
| 0 | `0/2 = 0` | 0 | ✅ **碰巧对**（所以档 0 那几条绿） |
| **1** | `1/2 = **0**` | 0.5 | ❌ `w = 0` ⇒ **Fill 不画**；手柄 `x = 轨道左 + 12 + 0` ⇒ **停在档 0 的位置** |
| 2 | `2/2 = 1` | 1 | ✅ **碰巧对**（所以档 2 那几条绿） |

**证据（三条红的实得值与「`n = 0`」逐一吻合，且互相印证）**：

1. `档 1 的 Fill 画出来了 —— 期望 [True]，实得 [False]` ⇒ `w = 0` ⇒ `if (w > 0.5f)` 不成立 ⇒ 没建九宫格。
2. `档 1 的手柄中心 x ⇒ 实得 853.368` ⇒ **正好是档 0 的值**（`Screen(829.52 + 12)`，档 1 应为 `1069.899`）。
3. `那一「点」之后 Fill 的宽 ⇒ 实得 -1.00` ⇒ `-1` 是**我在那条断言里对「量不到 quad」打的哨兵**（`ok2 ? … : -1f`），与前两条同一个原因。
   🔑 **而这一条上方那条「点在 0.30 处 ⇒ 档 1」是绿的** ⇒ `win.FpsIndex == 1` **取对了** ——
   因为 `SetFpsFromCanvasX` 算值走的是浮点（`Mathf.Clamp01(x/areaW) * (FpsMax - FpsMin)` ⇒ 提升成 float）
   ⇒ **「点击坐标 → 值」这条路没问题，错的只是「值 → 画」那一步**。

**为什么只有档 1 会红**：整数除法的三个结果 `0 / 0 / 1` 与正确值 `0 / 0.5 / 1` **只有中间那一档不同**
—— 两端的档**碰巧**对，所以「一眼看不出」，只有 3 档里正中间那一档才会现形（这正是自检那条
「档 1 的 `Fill` 宽 = 221.03」存在的意义；它也**正是这次抓出它的那条**）。

**改了什么**：`Shell/SettingsWindow.cs:833` —— 分母转 `(float)`，并把这条坑写进注释：

```csharp
float n = (idx - FpsMin) / (float)(FpsMax - FpsMin);     // 0 / 0.5 / 1
```

⛔ **期望值一个字没动**（`1069.9` / `221.03` 仍是从原版字段 12 + 值/2×481.18、值/2×491.18 算出来的），
三条断言的「怎么改坏就红」**照旧成立**（改回整数除法 ⇒ 档 1 三条立刻红 —— **实测过一次**）。
同段另核了一遍**没有第二处**整数除法（`SetFpsFromCanvasX` 那处是浮点乘法、分母 `areaW` 是 float）。
改完 `TMPDIR=/tmp/wf_a168 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

---

## 改了什么（文件:符号名/行号）

### `Shell/SettingsWindow.cs`（**白名单文件**）

| # | 行 | 改动 |
|---|---|---|
| 1 | `:54` | 加 `using UnityEngine.InputSystem;`（滑块要读 `Mouse.current`，同 `Shell/PointerLayer.cs:39`） |
| 2 | `:156-159` | 新常量 **`GfxRowsShift = 67.34f`**（= 原版那一列的滚动范围；⛔ 第 0 行不参与） |
| 3 | `:161-208` | 新常量组 `Fps*`（行高 105 · Title/Slider/Tick 的行内框 · `FpsTrackInset 10` · `FpsHandleOffset 12` · `FpsHandleSquare 35.406` · `FpsBarCap 92` / `FpsFillCap 15` · 三个刻度的 x/y/名/字/色 · 三张图名）—— 每条带出处 |
| 4 | `:506` | 图像页那条出声日志补一句「`FPS Limit` 那一行自 A168 起是**滑块**」 |
| 5 | `:515-527` | `BuildGraphicsPage`：`VSync` 行传 `yShift: GfxRowsShift`（`:525`）；删掉 `BuildCheckRow(page, "FPS limit", …)`，改成 **`BuildFpsRow(page)`**（`:527`） |
| 6 | `:533-535` | `BuildCheckRow` 加形参 `float yShift = 0f`（`t = ChkT + row*ChkRowStep − yShift`；第 0 行走默认 0 ⇒ **A165 钉的那条绝对矩形一字不动**） |
| 7 | `:616-854` | 新段「图像页第 6 行：FPS 上限」= `BuildFpsRow`(`:676`) · `FpsClickAtPointer`(`:736`) · `UpdateFpsDrag`(`:744`) · `SetFpsFromPointer`(`:781`) · `SetFpsFromCanvasX`(`:786`) · `SetFpsIndex`(`:803`) · `PlaceFps`(`:831`) · `FpsOfIndex`(`:659`) · `FpsIndexOfTarget`(`:669`) · `FpsIndex`(`:819`) / `FpsSliderRoot`(`:821`) 两个只读口 · `FpsMin/FpsMax`(`:854`) |
| 8 | `:1176-1182` | `Update()` 开头加一句 `UpdateFpsDrag();`（真 Play 里的按/拖；批处理下 `Update` 不跑） |
| 9 | `:1222-1234` | 新 helper **`NineOut`**（= 本窗的 `Nine` + `borderOutPx`，九宫格端帽按**画布 px** 画 ⇒ 调用点要 `× RootScale`） |

**删掉的**：`CycleFps()`（「60 → 30 → 无限 → 60」那个循环）—— 全工程 0 个引用（已 `grep` 过）。`FpsText()` 留着（改成滑块后仍用于 flash 文案）。

🔴 **为什么不复用 `Battle/WfSlider`**（写进代码注释了）：它把三处**逐实例不同**的值写成了常量 —— ① 队列硬编码 `3000`
（本窗压暗层 3130 / 面板 3131 都在它上面 ⇒ 会被压暗一层）；② 手柄边长硬编码 `22.406`（那是**音频页**手柄框的高，本行原版是 **35.406**）；
③ 手柄中心少一个 `+12` 的 `m_AnchoredPosition` 偏移。而 `Battle/WfSlider.cs` **不在本件白名单**里。
⇒ 本行写了一份**专用**的（只画 + 只算值）。要合并 = 给 `WfSlider.Create` 补 `queue / handlePx / handleOffset` 三个参数（§顺手发现 ①②③）。

### `Editor/SettingsScene.cs`（**白名单文件**）

| # | 行 | 改动 |
|---|---|---|
| 1 | `:577-579` | 图像页那句日志补「`FPS limit` 自 A168 起是**滑块**」 |
| 2 | `:581-739` | 新增一节 **`Section("A168：图像页 `FPS Limit` 那一行 = 滑块…")`** —— **40 处断言调用**（其中两个循环各跑 3 次 ⇒ **实际执行 54 条**），逐条见下节 |

---

## 断言（每条：期望值来自哪个原版字段 / 怎么改坏就红）

宿主 `Editor/SettingsScene.cs` 的 `SettingsScene.Run`（`:596` 起，全在一个 `{ }` 块里）。期望值一律是**原版 prefab 手算的绝对画布 px 字面量**
（⛔ 不过 `Screen()` —— 它是被测实参，同 A131② / A165 的口径）；带 `+Z` 的 y 值 = 原版绝对 y 减 **60.61**（= 上移 67.34 × 0.9）。

| # | 断言 | 期望值来自 | 怎么改坏就红 |
|---|---|---|---|
| 1 | `FPS Limit` 那一行在 **且 `Toggle` 不在了** | 原版这一格是 `Slider`（步骤 0 ①） | 退回 `BuildCheckRow` ⇒ 冒出 `Toggle` ⇒ 红 |
| 2 | `FPS Slider` 节点在 | 原版 GO 名就叫 `FPS Slider` | 改节点名 ⇒ 红 |
| 3 | 轨道渲出 [842.57,893.66]–[1284.63,905.36]（442.06 × **11.70**） | 原版 491.18×13 经 0.9；左沿 842.57 = 原版绝对 x | `FpsSliderW` 传未缩放的 491.18 ⇒ 宽差 44px ⇒ 红 |
| 4 | 轨道的图 = **`Volume_bar_inactive`** | `Background.m_Sprite` | 换成 `Volume_bar_active` ⇒ 红 |
| 5 | 轨道九宫格**左端帽** = **82.8** | `m_Border 184 ÷ ppuMul 2` = 92 设计 px × 0.9 | 传 184（贴图 px 原样 = 音频页那三根现在的做法）⇒ 184；只过 0.9 没除 ppuMul ⇒ 165.6 ⇒ 都红 |
| 6 | 手柄的图 = **`Volume_button`** | `Handle.m_Sprite` | 换图 ⇒ 红 |
| 7 | 手柄**渲出来的边长** = **31.87** | 框 46.811×35.406 + 110×110 方图 `preserveAspect` ⇒ 35.406 × 0.9 | 跟音频页那根一样按 22.406 画 ⇒ 20.17 ⇒ 红 |
| 8 | 档 0/1/2 ⇒ `Application.targetFrameRate` = **30 / 60 / −1** | `PlayerDataManager__ApplySettingsOptions.c` 的 if 链 | 把 2 档映射成别的 ⇒ 红 |
| 9 | 档 0 的 `Fill` **不画** | 原版 `anchorMax.x = 值/2 = 0` ⇒ 宽 0 | 0 档也画 ⇒ 红 |
| 10 | 档 1/2 的 `Fill` 宽 = **221.03 / 442.06**、左沿 = 轨道左沿 | 原版 `Fill` 由 `anchorMax.x` 驱动（值/2 × 491.18 × 0.9） | 用「值 × 宽度」不除 2 ⇒ 442/442 ⇒ 红 |
| 11 | 档 0/1/2 的手柄中心 x = **853.37 / 1069.90 / 1286.43** | 轨道左 + (12 + 值/2 × 481.18) × 0.9；12 = `Handle.m_AnchoredPosition.x` | 漏掉那个 +12 ⇒ 每条差 10.8px ⇒ 红；把 481.18 写成 491.18 ⇒ 档 2 差 9px ⇒ 红 |
| 12 | 点轨道**左端之外** ⇒ 档 0（夹住） | uGUI `Slider.UpdateDrag` 的 `Mathf.Clamp01` | 去掉 clamp ⇒ 越界算负值 ⇒ 红 |
| 13 | 点在 **0.30** 处 ⇒ 档 1 | `m_WholeNumbers = 1` 的 `Mathf.Round` | 不取整 ⇒ 值 0.60 ⇒ 红 |
| 14 | ★ 上面那一「点」之后 `Fill` 宽 = **221.03**（不是 265.24） | 同上（**这条专盯「忘了取整」**） | 去掉取整 ⇒ 0.60 × 442.06 = 265.24 ⇒ 红 |
| 15 | 点在 **0.51** 处 ⇒ 仍是档 1（1.02 取整） | 同上 | 用 `Floor` ⇒ 0.51 处会变 0 ⇒ 红 |
| 16 | 点在 **0.80** 处 ⇒ 档 2，且 `targetFrameRate` = **−1** | 同上 + 映射 | —— |
| 17 | 点滑区**右端之外** ⇒ 档 2 | `Clamp01` | —— |
| 18-20 | `FpsOfIndex(0/1/2)` = **30 / 60 / −1** | `ApplySettingsOptions` 三段 | 写反 ⇒ 红 |
| 21 | `FpsOfIndex(3)` = **60** | 原版 `iVar1 != 0` 里**只有 `== 2` 走 −1** | 写成「不是 1 就给无限」⇒ 红 |
| 22 | `FpsOfIndex(−5)` = **60** | 同上（负数也走那一段） | 同上 |
| 23-26 | `FpsIndexOfTarget(30/60/−1/0)` = **0 / 1 / 2 / 2** | 开窗初值（30→档 0…；≤0 归 Unlimited，与旧勾选行同口径） | 改映射 ⇒ 红 |
| 27 | 自检跑完 `targetFrameRate` 回到原值 | 自检卫生（不许改运行时状态） | 不还原 ⇒ 红 |
| 28 | 改档**有话说**（`win.Flash` 含「帧率上限」） | 本窗惯例（不静默） | 不写 flash ⇒ 红 |
| 29 | 刻度 1/2/3 节点在（名 `30 FPS`/`60 FPS`/`Unlimited`） | 原版三个子件的 GO 名 | 改名 ⇒ 红 |
| 30 | 三个刻度的**字** = `30` / `60` / `Unlimited` | 原版 TMP `m_text`（第 3 个是本地化词条 `'Ilimitado'`，英文正式文案本地没有 ⇒ 我们写英文） | 抄错 ⇒ 红 |
| 31 | 三个刻度的**中心 x** = **852.48 / 1069.29 / 1281.78** | 原版框中心（行内 x 163/403.9/640 + 114.01）经 0.9 | 把 x 当「相对滑块左沿」⇒ 每个偏 239.4px ⇒ 红（**这正是我第一版写错的那个坑**） |
| 32 | 行标题 = `FPS limit` | 原版 TMP `'Límite de FPS'`（西语 + 无词条 ⇒ 照文件头 ② 写英文） | —— |
| 33 | 行标题**左沿** = **617.57** | 原版 `Title` 框左沿（行左 563.52 + 16）经 0.9 | 不 `AlignLeft` ⇒ 居中 ⇒ 差约 150px ⇒ 红 |
| 34 | ★ **整块（轨道/手柄/填条）底沿 ≤ 923.57** | 原版 `Menu Area` 底 966.19 经 0.9 | `GfxRowsShift` 改回 0 ⇒ 底沿 ~976 ⇒ 红（那一版会画到弹窗外） |
| 35 | ★ 命中区里有 `ImageQuad`（不是裸节点） | `PointerLayer` 只认 `ImageQuad`（A26 那个坑） | 把 `Hit` 换成裸 `Node` ⇒ 真鼠标点不动 ⇒ 红 |
| 36 | ★ 轨道正中央命中的是**这根滑块**（不是吸收层） | 同上 | 命中档低于吸收层 ⇒ 红 |

---

## 实测证据

1. **秒级类型检查**（改完每个文件都跑过，最后一次是全部改完之后）：
   ```
   TMPDIR=/tmp/wf_a168 bash d:/4/Unity/工具/typecheck.sh
   --- 运行时程序集 ---   运行时错误数: 0
   --- 编辑器程序集 ---   编辑器错误数: 0
   ```
   （中途抓到并修掉 3 处：`bgQ` 与外层重名 · 两处 `out` 变量在 `&&` 短路下「未赋值」）
2. **行尾**：两个文件 `HEAD` 都是纯 LF，改完仍是纯 LF（`b.count(b'\r\n') = 0`）—— 没被翻。
3. **没跑 Unity、没跑自检**（按简报；自检由主对话在同步点跑）。**上面 36 条断言一次都没实际执行过** —— 交付的是「编译通过 + 静态可推」，
   期望值全部按 `Screen()` 的算式手推过（`960 + (设计x − 960) × 0.9`），逐值与断言里的字面量核过（Δ ≤ 0.003 px）。
4. **反向自查**：写第一版时把三个刻度的 x 当成「相对滑块左沿」算（错 239.4 px）—— 是拿 `menu_dump --no-ancestor-scale` 的原始
   相对坐标复核时抓出来的（那一份 dump 的三行 x 是 163/403.9/640，滑块自己是 266 ⇒ 两者不同源）。

---

## 没查清的部分

1. **`GraphicsTab__OnSetup` 结尾那个 vtable 调用是不是 `SetValueWithoutNotify`**：反编译里参数位置的表达式被它写成了
   `GameStaticData` 的**类实例指针**（该是那个 int 字段），旁边同类件（Toggle/Dropdown）都解析成了 `SetXxxWithoutNotify`
   ⇒ 判断可信但**没有逐字节钉死**。**还差**：按 VA 反汇编那一条 call（`资料/全量反编译_入口与用法.md` 的办法）。
   ⚠️ 不影响本件的落法（我们开窗时只摆值、不写运行时帧率）。
2. **`Application.targetFrameRate` 的持久化**：原版把 `GameStaticData.FPSLimit` 存进**玩家存档**、启动时 `ApplySettingsOptions` 应用；
   我们只在运行时写 `Application.targetFrameRate`（与 `VSync`/画质那两行一样「重开游戏就回默认」，音频走 `PlayerPrefs`）。
   ⇒ **本件没做持久化**（要做得先决定设置该存哪，属另一条；不是本行的几何/语义问题）。
3. **手柄在 2 档时探出轨道右端 19.7 px 那一小块点不到**：原版点得到（手柄那颗 Image 也是 `m_RaycastTarget = 1`，
   射线冒泡到 `Slider`），我们的命中区只覆盖轨道那一块。观感一致（`Update` 那一路照样会取值），**没查清**要不要连手柄一起纳入命中区。
4. **三个刻度框也是原版的射线目标**（TMP 默认 `m_RaycastTarget = 1`，且它们是 `Slider` 的子件）⇒ 原版点刻度也会跳值；
   我们的 `Hit` 只覆盖轨道。同上，`Update` 那一路兜住了取值，但**点刻度不会「吃掉」这一下**（落到吸收层）。**没查清**是否要一并纳入。
5. **本窗文字字号整体比原版大 11%**（见 §顺手发现 ⑦）—— 本行照同口径给 42（未按 0.9 折），**没单独修正**。
6. **小屏缩放（A165）与滑块的关系没跑实机**：本窗根被乘 1.2 时，`ImageQuad.WorldW` / `PointerLayer.HitBoxPx` 都不含 `lossyScale`
   ⇒ 滑块的手柄位置/命中区会按 scale 1 那一帧算（这是 A154/A165 已记的**全窗性质**，不是本行新引入的）。

---

## 顺手发现（**只报，没改**）

1. 🔴 **音频页那三根 `WfSlider` 画在渲染队列 3000** —— 而本窗的**压暗层 3130 / 面板 3131 / 面板填色 3132 / 内容 3133** 全在它上面
   ⇒ 它们会被压暗层（`(0,0,0,0.7725)`）盖一层。`Battle/WfSlider.cs` 里 `QSlider = 3000` 是硬编码，
   且它当年是按**战斗内**面板的队列定的。`Editor/SettingsScene.cs` 音频那几条只量了几何（宽/高/Fill 层高），**没有一条盯队列**。
   **建议**：给 `WfSlider.Create` 补 `queue` 形参（或本窗调用点建完后重设队列）。
2. 🔴 **`WfSlider` 的端帽没除 `m_PixelsPerUnitMultiplier`** —— 实测音频页那三根 `Background`/`Fill` 的 Image **也是 ppuMul 2.0**
   （同一个数，和本行那两张一样），而 `WfSlider` 传给 `MenuDraw.Nine` 的 `border` 直接当**画出来的**端帽用
   （184 / 30 画布 px）⇒ 原版是 92 / 15 **设计** px（= 82.8 / 13.5 画布 px）。**端帽宽了 2.2 倍**、中段因此短了一半。
   本行已按 `borderOutPx = border ÷ ppuMul × 0.9` 画（自检第 5 条钉着）。
3. 🔴 **`WfSlider` 的手柄中心少一个 +12 的 `m_AnchoredPosition.x`** —— 实测**音频页那三根也有**这个偏移
   （`RectTransform_-6029089631055872090`：`m_AnchoredPosition.x = 11.99988`）⇒ `WfSlider` 的手柄整体比原版**偏左 12 设计 px**（10.8 画布 px）。
4. ℹ️ **`WfSlider` 的手柄边长硬编码 22.406**（= 音频页手柄框的高）；同一套 prefab 里本行那根是 35.406
   ⇒ 手柄大小不是常量，应随「框的高」走（`preserveAspect` 取短边）。
5. 🔴 **图形页缺 `Scroll View`**（原版那一列是 `ScrollRect` + `RectMask2D` + VLG `Content`，视口 432.50–954.00、内容高 588.84、滚动范围 67.34）
   ⇒ **FPS 那一行在原版要滚到底才看得见**，而我们现在整页不滚。本件用「第 5、6 两行上移 67.34」近似（= 原版滚到底那一帧）。
   **完全复刻**要补滚动视图 + 视口裁剪（命中区也要按 `clip` 收，`MenuDraw.Hit` 已有 `clip` 形参）；
   届时第 5、6 行应回原位、`GfxRowsShift` 删掉（自检第 34 条会红，正好提醒改期望）。
6. 🔴 **FPS 那一行原先就溢出弹窗**（旧勾选行：行顶 916.10 + 76 = **992.10** > 内层底 **956.39**，2026-10-06 之前在屏幕上压着弹窗下沿 ~32px）——
   本件顺带修掉了（上移后块底 945.96 ✓）。**这一条此前没有任何断言盯着**（旧断言只盯第 0 行的绝对矩形）。
7. 🔴 **本窗所有文字的字号都是「按原版未缩放值传进 `MenuDraw.Text`」** —— 而 `MenuDraw.Text` 内部 `LayoutSpace.Px(fontPx)`
   是按**画布 px** 折世界的（`ImageQuad` 那句「108 px = 1 世界单位」）⇒ **比原版大 11%**
   （例：`PageTitleFontPx = 55`，原版的 55 是**设计** px ⇒ 应画 49.5 画布 px）。**本行照同口径给 42**（原版 42 设计 px ⇒ 应为 37.8），
   **没有单独修正**（避免与同窗其它文字不一致）。要修就是**全窗一起修**（一处口径、十几处调用点）。
8. ℹ️ **设置没有持久化层**（FPS/VSync/画质都只在运行时生效；音频走 `PlayerPrefs`、联机走 `NetConfig`）。
   原版是整套存玩家存档。这是**整窗**的欠账，不是本行的。
9. ℹ️ `Application.targetFrameRate` 与 `QualitySettings.vSyncCount` 的相互作用（Unity 里 vsync 开着时 targetFrameRate 无效）
   **原版也没处理** —— `ApplySettingsOptions` 只写 targetFrameRate、`QualitySettingsManager.ApplyGraphicsQuality` 只写 vSyncCount
   ⇒ 我们**保持与原版一致**，不动。
