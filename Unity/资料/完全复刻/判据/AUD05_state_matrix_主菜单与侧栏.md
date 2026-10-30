# AUD05 · 状态 × 参数矩阵 ——【第二窗】**主菜单 + 左侧导航 + 顶栏 + 子菜单**

> 代理：`B103_A05-W4`（只读查判据） · 日期：2026-10-30 · 只读 `d:/2` 与 `Unity/MyGame/Assets`，未跑 Unity、未动 git、未派子代理。
> 触发口径：铁律 5·c「一个值 ≠ 全部情况」。**接续** `AUD05_state_matrix_卡组编辑.md` §3·4（那份点名了主菜单的 `FlexibleGridLayout` + `AdaptFlexibleGridLayoutByAspectRatio`，**没有展开**；本文件把它展开）。
> 结构 / 口径 / 红线照模板；模板已确认的通用结论（`smallScreenUI`、CanvasScaler ref 1920×1080 **Expand**、`GameWindow.extraScaleSmallScreen` 恒 1、`AutoGridLayout`/`FlexibleGridLayout` 类名在 monoscripts 里查不到不等于本地没有）**直接引用，不重查**。

---

## §0 结论摘要（先看这 7 行）

| # | 结论 | 强度 |
|---|---|---|
| 1 | **主菜单会改布局参数的运行时状态只有 3 个**：① **屏幕纵横比**（`AdaptFlexibleGridLayoutByAspectRatio`，只改 `itemSize.y`）② **侧栏显隐**（`NavigationPanelController.Toggle`，改 `anchoredPosition.x`）③ **卡片自己的 `FlexibleLayoutSizeOption`（1×1 / 1×2）**。其余全是「不改几何、只改显隐/内容」 | ✅ 逐方法实读 |
| 2 | 纵横比公式只有一条：**`itemSize.y = 414.4 × max(1, (16/9) ÷ (Screen.width ÷ Screen.height))`**，`itemSize.x` **不变**；**只有窄于 16:9 才变高**；≥16:9 一律 414.4。**一段式、无门槛、无第二档** | ✅ `.c` + `out_il2csharp` + `out_cpp2il_dev_ilspy` 三方一致 |
| 3 | 卡片尺寸**由布局组写**（`SetChildAlongAxis(rect, axis, pos, size)`，`size = sizeY×itemSize.y + (sizeY−1)×spacing.y`）⇒ **非 16:9 时卡片跟着变高**；prefab 里的 `535×414` / `535×848.8` 只是**编辑器模板值**，运行时会被覆写 | ✅ `FlexibleGridLayout__CalculateLayoutInputHorizontal.c:145-152` |
| 4 | 屏幕比例被 `ScreenAspectRatioController`（主菜单场景 **MB1987**）夹在 **[4:3, 22:9]**（相机 `rect` 黑边）；**但纵横比公式读的是未夹的 `Screen.width/height`** ⇒ 区间外两套口径分家（见 §1·5 脚注） | ✅ `.c` 实读；⚠️ **无实拍** |
| 5 | 侧栏 3 档：显示/隐藏（`hiddenPosition.x = −1162.77`、`animationTime = 0.3`）+ 选中（`ToggleGroup` 单选，`Selected highlight` 是 **Toggle 的 `graphic`**，`toggleTransition = 1` 淡入淡出 `fadeDuration 0.1`）+ 悬停（`m_Transition = 1` ColorTint，tint **图标**，色 `(0.7217, 0.8152, 1.0)`） | ✅ 场景 MB 逐字段 |
| 6 | **小屏 `smallScreenUI` 对主菜单零影响**：`extraScaleSmallScreen = 1.0` ⇒ `GameWindow.Open` 不加缩放器（数值条件不成立，不是「加了但没效果」）；场景 19 颗 TMP 里只有 4 颗 `smallScreenMaxFontSize ≠ 0`，**全在 toast prefab**（`CollectorLevelUp` 那族） | ✅ 逐条实读 |
| 7 | 我方：**非 16:9 一档没做**（常量 + 自己在注释里写明「固定用 1920×1080」）· **比例夹取 [4:3,22:9] 没做**（只有 `Battle/` 有）。见 §4 | — |

---

## §1 权威公式（原版）

### 1·1 `AdaptFlexibleGridLayoutByAspectRatio` —— 逐句

```c
// D:/2/tools/decomp_full/AdaptFlexibleGridLayoutByAspectRatio__ModifyFlexibleLayout.c（全 30 行，逐句）
iVar3 = Screen.width;                              // int
iVar4 = Screen.height;                             // int
fVar6 = DAT_1834b32d0 / ((float)iVar3 / (float)iVar4);   // DAT_1834b32d0 = 1.7777778f = 16/9
fVar8 = DAT_1834b2bb8;                             // DAT_1834b2bb8 = 1.0f
if (DAT_1834b2bb8 <= fVar6) { fVar8 = fVar6; }      // fVar8 = max(1.0f, 16/9 ÷ aspect)

fVar6 = *(float*)(param_1 + 0x28);                  // this.originalItemSize.x
fVar8 = *(float*)(param_1 + 0x2c) * fVar8;          // this.originalItemSize.y × fVar8
lVar1 = *(longlong*)(param_1 + 0x20);               // this.flexibleGridLayout（FlexibleGridLayout 实例）
fVar7 = *(float*)(lVar1 + 0x68) - fVar6;            // 当前 itemSize.x − 新 x
fVar5 = *(float*)(lVar1 + 0x6c) - fVar8;            // 当前 itemSize.y − 新 y
if (DAT_1834b32f8 < fVar6*fVar6 + fVar7*fVar5 ...)   // DAT_1834b32f8 = 0.0001f（变化阈值）
    LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
*(float*)(lVar1 + 0x68) = fVar6;                     // itemSize.x ← originalItemSize.x（不变）
*(float*)(lVar1 + 0x6c) = fVar8;                     // itemSize.y ← originalItemSize.y × fVar8
```

**读法三条（必须一起读，单看任一条会做错）：**

1. **`(float)iVar3 / (float)iVar4` 是浮点除法**。⚠️ `out_cpp2il_dev_ilspy` 与 `out_il2csharp` 都把这一段打成 `num1 / num2`（int），**照抄会得到 1920/1080 = 1 ⇒ 系数 1.7778 ⇒ itemH 736.7**（网格自然高 1493，比 Content 的 945.6 还高）——**错**。以 `.c` 的 `cvtsi2ss` 为准（同族先例：`ScreenAspectRatioController__AdjustCameraViewport.c` 也是 `(float)iVar1 / (float)iVar2`）。
2. **只写 y**（`+0x6c`）。`+0x68` 那一句写的是 `originalItemSize.x` 本身 ⇒ **x 恒不变**。
3. **偏移量语义**（`FlexibleGridLayout` 实例内）：`+0x68/+0x6c = itemSize.x/.y`，`+0x70/+0x74 = spacing.x/.y`，`+0x78 = grid`（同一方法族里 `+0x74` 当间距用、`+0x78` 当二维数组用已交叉验证）。

**三个入口，方法体逐句相同**（`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AdaptFlexibleGridLayoutByAspectRatio.cs:14-111`）：

| 入口 | 触发 | 额外动作 |
|---|---|---|
| `OnEnable` | 组件启用 | 先判 `originalItemSize ≈ Vector2.zero`（阈 `9.9999994e-11f`）⇒ **成立才**把它缓存成 `flexibleGridLayout.itemSize`（本场景 = 535×414.4）；再 `Signal.Register<ScreenResolutionChangeSignal>` |
| `ResolutionHasChanged` | 分辨率变化信号 | 无（`ResolutionManager.cs:47-66` 每 `1f` 秒轮询 `Screen.width/height`，变了就 `Raise`；`ResolutionManager.cs:68`） |
| `ModifyFlexibleLayout` | **ILSpy 全树 0 调用点**（只有定义）；体与上两者相同 | 无 ⇒ 即使它是配置上的私有回调，效果与上面一致 |

### 1·2 `FlexibleGridLayout` 的排布（**卡片尺寸是布局写出来的**）

```c
// D:/2/tools/decomp_full/FlexibleGridLayout__CalculateLayoutInputHorizontal.c:145-152
SetChildAlongAxis(child, 0,                       // x
   (spacing.x * col + padLeft − padRight) + col * itemSize.x,
   (sizeX − 1) * spacing.x + sizeX * itemSize.x);
SetChildAlongAxis(child, 1,                       // y
   (rowStep + alignOffset + padTop − padBottom) + row * itemSize.y,
   (sizeY − 1) * spacing.y + sizeY * itemSize.y);
```

- `sizeX / sizeY` 来自子件上的 **`FlexibleLayoutSizeOption`**（`sizeX/sizeY`，默认 1/1）。
- `alignOffset`（`fVar21`）= 由 `m_ChildAlignment` 算：本场景 = **3（MiddleLeft）** ⇒ `(Content.rect.height − ((rows−1)×spacing.y + rows×itemSize.y)) × 0.5`（`DAT_1834b2bb4 = 0.5f`；其余 1/2/4/5/7/8 走 `Debug.LogWarning` 支、6 走顶对齐）。
- **Content 的宽会被改写**（同一方法尾部）：`sizeDelta.x = (spacing.x + itemSize.x) × 用到的列数 − spacing.x + padLeft + padRight`（`GetMaxWidth`）⇒ 那个 74 是**模板值**，运行时变成内容宽（这才是能横滚的原因）。
- ⇒ **非 16:9 ⇒ `itemSize.y` 变 ⇒ 卡片高变 ⇒ 行 y / 网格自然高 / 居中偏移全都变**；`itemSize.x` 不变 ⇒ **列位置与列宽不变**（但见 §1·5 的 `GameModes` 左缘随画布宽变）。

### 1·3 `CanvasScaler`（主菜单场景 MB2577）

```
m_UiScaleMode = 1（ScaleWithScreenSize）· m_ReferenceResolution = 1920×1080
m_ScreenMatchMode = 1（Expand）· m_MatchWidthOrHeight = 0（Expand 档下不参与）
m_ReferencePixelsPerUnit = 100 · m_ScaleFactor = 1.2（ScaleWithScreenSize 档下不参与）· m_DynamicPixelsPerUnit = 1
```

`Expand` ⇒ `scaleFactor = min(W/1920, H/1080)` ⇒ **画布 ≥ 1920×1080，缺的那一边长**：
- 比例 **≤ 16:9** → 画布 = `1920 × (1920 ÷ aspect)`（16:9 1080 · 16:10 1200 · 4:3 1440）
- 比例 **≥ 16:9** → 画布 = `1080 × aspect × 1080`（21:9 2520 · 32:9 3840）

### 1·4 屏幕比例夹取（`ScreenAspectRatioController`，主菜单场景 **MB1987**）

```c
// D:/2/tools/decomp_full/ScreenAspectRatioController__AdjustCameraViewport.c
aspect = (float)Screen.width / (float)Screen.height;
if (maxAspect < aspect)      { rect = ((1 − max/aspect)/2, 0, max/aspect, 1); }      // 左右黑边
else if (minAspect <= aspect){ rect = (0, 0, 1, 1); }                                 // 满屏
else                         { dy = (min/aspect − 1)/2; rect = (0, dy, 1, 1−2·dy); }  // 上下黑边
```

`MonoBehaviour_1987.json`：`targetCameras = [Cam 602]` · `maxAspectRatio = 2.4444439`（=22:9） · `minAspectRatio = 1.3333330`（=4:3）。
⚠️ **它和 §1·1 不是同一把尺子**：夹取改的是**相机视口**（uGUI ScreenSpaceCamera 画布尺寸跟着变），**纵横比公式读的是原始 `Screen.width/height`**。⇒ 只有在 `[4:3, 22:9]` 区间内两者一致。

### 1·5 合成：各纵横比的**算出来**的期望值

> ⚠️ **全部是「按 §1·1+§1·3+§1·4 的语义推的算术，没有实拍」**（本机 `runtime_ui_dump_*.tsv` 只覆盖战斗/Intro，`grep "Main Menu"` 0 命中）。
> 记法：`itemH` = `itemSize.y`；`natH` = 网格自然高 = `2×itemH + 20`；`row0top` = 第一行卡顶到**画布顶**的距离。
> 🔧 **精度**：下表算的时候把 `GameModes.sizeDelta.y(−0.049)` 与 `Content.sizeDelta.y(1.449)` 两小项按量级省略 ⇒ **每格 ±0.1 px 以内**（精确式见 §2 的两条 RT：`h = 0.934×画布高 − 0.049`、`h = 0.936×父高 + 1.449`）。
> 推导链：`GameModes` anchors `(0.087,0)-(1,0.934)` → `Viewport` 同矩形 → `Content` anchors `(0,0)-(0,0.936)` `sizeDelta.y 1.449` `apos.y 0.724` `pivot(0,0.5)` → 网格在 `Content` 里 `MiddleLeft` 居中。

| 比例 | 画布（Expand） | 夹取后可视 | 系数 `max(1,(16/9)÷a)` | **itemH** | **natH** | Content 高 | **row0top** | 卡底（row0） | 可见整列 |
|---|---|---|---|---|---|---|---|---|---|
| **16:9** (1920×1080) | 1920×1080 | 满屏 | 1.0000 | **414.40** | 848.80 | 945.61 | **182.79** | 597.19 | **3 整列 + 第 4 列被切**（col3 右 2405 > 1920） |
| **16:10** (1920×1200) | 1920×1200 | 满屏 | 1.1111 | **460.44** | 940.89 | 1050.52 | **204.29** | 664.73 | 3 整列 + 第 4 列被切 |
| **4:3** (1600×1200) | 1920×1440 | 满屏 | 1.3333 | **552.53** | 1125.07 | 1260.33 | **247.30** | 799.83 | 3 整列 + 第 4 列被切 |
| **21:9** (2560×1080) | 2560×1080 | 满屏 | 1.0000 | **414.40** | 848.80 | 945.61 | **182.79** | 597.19 | **4 整列**（col3 右 2460.7 < 2560）· 左缘随画布右移（见注②） |
| **32:9** (3840×1080) | ⚠️ 2640×1080（夹到 22:9） | 左右黑边 | ⚠️ 见注① | **414.40** | 848.80 | 945.61 | **182.79** | 597.19 | 夹取后 ≈4 整列 |
| **5:4** (1280×1024) | ⚠️ 1920×1536（夹到 4:3） | 上下黑边 | ⚠️ 见注① | 414.4×1.4222 = **589.36**（按**未夹**屏比例算） | 1198.72 | ≈1343.6 | ≈270.2 | 859.6 | 3 整列 + 第 4 列被切 |

**注①（区间外必读）**：比例 < 4:3（如 5:4）或 > 22:9（如 32:9）时，**画布按夹取后的比例算、`itemH` 按原始 `Screen` 比例算** ⇒ `itemH` 与画布**不自洽**（这是原版代码的既有形状，⛔ 别「顺手修」成一致；判据 = §1·1 与 §1·4 读的是两个不同来源）。⚠️ 「ScreenSpaceCamera 画布尺寸取相机视口」这一条是按 Unity 语义推的，**没有实拍**。
**注②（横向也会变）**：`GameModes` 左缘 = `0.087 × 画布宽`。16:9/16:10/4:3 画布宽恒 1920 ⇒ 列 0 左 = **205.04**；21:9 画布宽 2520 ⇒ 列 0 左 = **257.24**（整体右移 52.2）。**列宽/列距恒 535 / 555**。

### 1·6 侧栏（`NavigationPanelController`，场景 MB1955）

```csharp
// out_cpp2il_dev_ilspy/NavigationPanelController.cs:472-519（逐句）
Toggle(option, withAnimation):
  if (activeSelf == option) withAnimation = false;     // 已经是目标态 ⇒ 不动画
  DOTween.Kill(rectTransform);
  if (!option) {
      if (withAnimation) { pos = originalAnchorPosition; DOAnchorPos(hiddenPosition, animationTime); OnComplete → SetActive(false); return; }
      SetActive(false); pos = hiddenPosition; return;
  } else {
      pos = hiddenPosition; SetActive(true); pos = originalAnchorPosition;
      if (withAnimation) DOAnchorPos(originalAnchorPosition, animationTime);
  }
Awake(): originalAnchorPosition = rectTransform.anchoredPosition;   // (0,0)（RT1103）
          rectTransform.anchoredPosition = hiddenPosition;  gameObject.SetActive(false);
```

序列化（`MonoBehaviour_1955.json`）：`hiddenPosition = (−1162.77001953125, 0)` · `animationTime = 0.3000000119` · `buttonContainer = RT1112` · `toggleGroup = MB2616（m_AllowSwitchOff = 0）` · `navToggles = [2199, 2256, 2255, 2257, 2258]` · `eventButtonReference.m_AssetGUID = ae2560f5eb7d1634d8fc917de4f80369`。

### 1·7 聊天预览（`ChatPreview` 两态动画，主菜单右上）

| 态 | 值 | 出处 |
|---|---|---|
| 出厂（`OnBeforeInitialization`） | `icon.localScale = 0`（收成 0）· `messageContainer.fillAmount = 0` · 两条 `Message Preview` 行 `enabled = false` | `ChatPreview.cs:45-77` |
| **Show()**（有新消息 / 聊天窗关闭回来） | `SetActive(true)` · icon `DOScale 1` @**0.6s** `From(0)` `OutBack` · `fillAmount → 1` @**0.4s** · fade → 1 @**0.2s** `From(0)` `SetDelay 0.2` | `ChatPreview.cs:193-208` |
| **OpenChat()**（点聊天钮） | icon `DOScale 0` @**0.3s** `InBack` · `fillAmount → 0` @**0.2s** `SetDelay 0.1` · fade → 0 @**0.15s** `SetDelay 0.1` ⇒ 完成时 `SetActive(false)` + 开聊天窗 | `ChatPreview.cs:99-192` |
| 只显示 `channelType == Global` 且 `messages.Count > 0` | 取**最后 N 条**（N = `previews.Count`，场景里 2 行） | `ChatPreview.cs:209-262` |

---

## §2 prefab / 场景**原始序列化值**（未乘任何系数的那一档）

| 件 | 字段 | 值 | 出处 |
|---|---|---|---|
| `CanvasScaler` | 见 §1·3 | — | `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2577.json` |
| `Game UI` 根 RT | `a(0,0)-(0,0)` `sz(0,0)`（Canvas 撑满） | — | `RT/1314` |
| `GameModes` RT | `a(0.087,0)-(1,0.934)` `pos(2.44e-4,−0.0239)` `sz(6.1e-4,−0.049)` `p(.5,.5)` | 16:9 ⇒ **167.04..1920 × 0..1008.72** | `RT/1122` |
| `Viewport` RT | `a(0,0)-(1,1)` `sz(0,0)` `p(0,1)`（**无 Mask/RectMask2D**，只有 CanvasRenderer） | = GameModes | `RT/1093` |
| `Content` RT | `a(0,0)-(0,0.936)` `pos.y 0.724121` `sz(74, 1.448975)` `p(0,.5)` | 16:9 ⇒ **x 167.04..241.04（宽 74 模板值，运行时被布局改写）· y 134.42..1080（高 945.57）** | `RT/1107` |
| `FlexibleGridLayout` | `m_Padding L38/R16/T0/B0` · `m_ChildAlignment 3(MiddleLeft)` · `rows 2` · `columns 20` · `itemSize(535, 414.4)` · `spacing(20,20)` | — | `MonoBehaviour_1961.json`（script `FlexibleGridLayout` = `7184420427299685613`） |
| `AdaptFlexibleGridLayoutByAspectRatio` | `flexibleGridLayout → MB1961`（`originalItemSize` **未序列化 ⇒ 运行时缓存 535×414.4**） | — | `MonoBehaviour_1962.json`（script `2460128468273128290`） |
| `GameModes` ScrollRect | `horizontal 1` · `vertical 0` · `movementType 1(Elastic)` · `elasticity 0.1` · `inertia 1` · `decelerationRate 0.135` · `scrollSensitivity 1.0` · 无滚动条（visibility 2） | — | `MonoBehaviour_1982.json` |
| `MainMenuWindow`（内层窗控制器） | `type 0` · `windowsPlacement 0(None)` · `closeOnESC 0` · `updateNavPanel 0` · **`extraScaleSmallScreen 1.0`** · `priority 2` · `liveopContainersAnchor → RT1107` · `scrollList → MB1982` · `screenBaker → MB1976` | — | `MonoBehaviour_1927.json` |
| `NavigationPanelController` | §1·6 | — | `MonoBehaviour_1955.json` |
| 5 颗导航 Toggle | 全部 `m_Transition 1(ColorTint)` · `toggleTransition 1(Fade)` · `fadeDuration 0.1` · `allowClickWhenAlreadySelected 0` · `m_Group → 2616` · **`m_IsOn`：Home=1，其余 4 颗=0** | `m_HighlightedColor = (0.7216981, 0.8151793, 1.0)` · `m_PressedColor = 0.7843 灰` · `m_SelectedColor = 0.9608 灰` | `MB2199/2256/2255/2257/2258` → toggle `MB2098/2039/2040/2038/2041` |
| 5 颗 Toggle 的 `graphic`（= 选中高亮） | `40k_main_bt_selected BW`（pid `503137736542995528`）· `m_Type 1(Sliced)` · `m_Color = (1, 0.0805369, 0, 1)`（正红，**5 颗同值**） | 高亮 = `graphic` 的 alpha（Toggled 才 1） | `MB1947`（Home）+ `MB2127/2126/2129/2130` |
| 5 颗 Toggle 的 `m_TargetGraphic`（= 悬停染色对象） | **图标那张 Image**（Home = `MB1972`，sprite `40k_main_bt_play`） | 悬停把**图标**染成 `(0.7217,0.8152,1.0)` | `MB1972 / 2120 / 2119 / 2121 / 2118` |
| 导航钮 RT | 5 颗全 `a(0,0)-(0,0)` `p(.5,.5)` `pos(0,0)` `sz(164.31, 169.68)`（位置由 VLG 算） | — | `RT/1123,1246,1247,1245,1244` |
| `Buttons Container` VLG | `spacing = −16.35` · `padding.top = −5` · `m_ChildAlignment 1(UpperCenter)` · **不控子尺寸** | 步进 = 169.68 − 16.35 = **153.33** | `RT/1112` + `MB(−4621643977240678714)`；步进值另见 `资料/主菜单_原版规格.md` §五 C |
| `Event Navigation Button`（第 6 颗，运行时实例化） | RT `sz(164.31, 169.68)` `a(.5,.5)` `pos(−0.94, 375.03)` · `EverguildToggle m_IsOn 0` · `OpenEventButton` · **`NavigationPanelToggle.toggleType = 5(Event)`** | — | `bundle_menus_assets_all/GameObject/Event Navigation Button.json` + 它的 4 个组件 |
| 5 颗导航钮的点击件 | `OpenWindowButton`：`windowToOpenScene → MB1927`（Home）/ `windowToOpenPrefab`（其余 4 颗 GUID）· `closeOtherMenus 1` · `closeOnToggleChangeToFalse 1` | — | `MB2556`（Home）/ 同族另 4 份 |
| `ScreenAspectRatioController` | `targetCameras [Cam602]` · `maxAspectRatio 2.4444439` · `minAspectRatio 1.3333330` | — | `MonoBehaviour_1987.json` |
| 模式/offer 卡模板（**会被布局组覆写**） | 1×1 = `sz(535,414)`、`a(.5,.5)`、`pos(0,−43.8868)`；1×2 = `sz(535,848.8)`、`a(0,0)`、`pos(0,0)` | — | 各 prefab 根 RT（下表） |

### 2·1 offer 容器族（`bundle_menus_assets_all`，全部 `MainMenuOfferContainer : LiveopMenuContainer`）

| prefab | `FlexibleLayoutSizeOption` | 根 RT 模板尺寸 | 组件差异 | 子件（1×1 那族） |
|---|---|---|---|---|
| `Main Menu Offer Container 1x1` | **1 × 1** | 535 × 414 | + `GeneralOfferPopupDrawer` | Background Image / Foreground Image / TextDarkening / Border / Border Glow / **Dynamic Content** / Badge（7） |
| `Main Menu Offer Container 1x1 Premium_booster_title_avatarOrResource` | **1 × 1** | 535 × 414 | + `GeneralOfferPopupDrawer` | 同族 |
| `Main Menu Offer Container Carousel 1x1` | **1 × 1** | 535 × 414 | + **`CarouselOfferPopupDrawer`**（不是 General） | 同族 |
| `Main Menu Offer Container Static Image 1x1` | **1 × 1** | 535 × 414 | **无任何 Drawer**（纯图 + 按钮） | 同族 |
| `Main Menu Offer Container Static Image 1x2` | **1 × 2** | 535 × **848.8** | **无任何 Drawer** | 同族 |
| 公共件 | 每份都有 `EverguildButton`（点击）+ `RectMask2D`（裁切）+ `CustomLocalizeCallback` | | | |

> 🔴 **本仓只有一个 1×2 的 main-menu offer prefab（`Static Image 1x2`）** —— 任务简报里写的 `Main Menu Offer Container 1x2` **不存在**（`ls GameObject/ | grep "Main Menu Offer"` 现读 = 5 份，见上表）。1×2 的**模式卡**另有 `Draft/Ranked/Skirmish ... 1x2`，属于模式卡族、不是 offer 族。

### 2·2 子菜单窗（`bundle_menus_assets_all`）

| prefab | 根 RT | 组件 | 窗参 |
|---|---|---|---|
| `Base Submenu` | `a(0,0)-(1,1)` `sz(0,0)` | `TabbedWindowComponents`（`tabButtons / tabPrefab / tabHolder`） | — |
| `Social Submenu Variant` | `a(0,0)-(1,1)` `sz(0,0)` | `TabbedWindowComponents`（`tabPrefab = 0` ⇒ 无母版）+ **`SocialMenuWindow`**：`type 0` · `windowsPlacement 5(Canvas)` · `closeOnESC 1` · **`updateNavPanel 1`** · `extraScaleSmallScreen 1.0` · `tabs[2]` | 同上 |
| `Menu Navigation Panel Button` | `a(0,1)-(0,1)` `sz(162.167, 180)` `p(.5,.5)` | `EverguildToggle`（`m_IsOn 0` · `m_Group 0`（不在组里）· `m_HighlightedColor (0.6085,0.9426,1.0)` · `m_DisabledColor 0.8019 灰`） | 子菜单自己的竖导航按钮（**与主菜单那 5 颗不是同一件**：这里 162.17×180、无 Group；主菜单 164.31×169.68、在同组里） |

---

## §3 状态 × 参数矩阵（**主表**）

> 每格写出处；**没查到的写 `?`**，⛔ 不拿默认值顶替。所有数值在**画布参考单位（CanvasScaler ref 1920×1080）**下。

### 3·1 模式卡区（`GameModes/Viewport/Content`）—— **纵横比是全表唯一会改几何的全局状态**

| 状态 | 判定条件 | `itemSize.y` | 1×1 卡高 | 1×2 卡高 | 网格自然高 | row0top | 出处 |
|---|---|---|---|---|---|---|---|
| **S-G0 比例 ≥ 16:9** | `aspect ≥ 1.7778`（16:9 / 21:9 / 32:9…） | **414.4** | 414.4 | 848.8 | 848.8 | 182.79（16:9 画布） | §1·1 系数=1；§1·5 |
| **S-G1 比例 16:9 → 4:3** | `1.3333 ≤ aspect < 1.7778` | `414.4 × (1.7778 ÷ aspect)` **连续** | = itemH | `2×itemH + 20` | `2×itemH + 20` | 随画布高与 itemH 变 | §1·1 + §1·2 + §1·5 表 |
| **S-G2 比例 < 4:3** | `aspect < 1.3333`（相机被夹到 4:3，上下黑边） | **仍按原始 `Screen` 比例算**（如 5:4 ⇒ 589.36） | 同左 | 同左 | 同左 | ? （画布 1536 高、无实拍） | §1·1 + §1·4 注① |
| **S-G3 比例 > 22:9** | `aspect > 2.4444`（左右黑边） | 414.4（系数恒 1） | 414.4 | 848.8 | 848.8 | 182.79 | 同 S-G0 |
| **S-G4 无活动（Content 0 子）** | `GetEventsForDisplayingInMenu()` 为空 | 414.4（不变） | — | — | — | — | `MainMenuWindow__RefreshLiveOpContainers`：空表 ⇒ 一张不实例化，**没有任何空态提示** |
| **S-G5 有活动** | `LiveOpState ∈ {OnPreview(1), Active(2), Finished(3)}` ∧ `ShowInMainMenu` ∧ 按 `GetEventPriorityForMainMenu` 排序 | 同上 | 按容器的 `sizeY` | 同 | Content 宽 = 用到的列数决定 | 同 | `LiveOpsManager__GetEventsForDisplayingInMenu.c` + `__c___…_b__19_1.c`（状态过滤：`state−1 ≤ 1` 或 `state−3 == 0`）+ `LiveOpsEvent__get_ShowInMainMenu.c` |
| **S-G6 横向滚动** | 用户拖拽/滚轮 | 不影响 itemH | — | — | — | 偏移改的是 `Content.anchoredPosition` | ScrollRect `MB1982`（elasticity 0.1 / decel 0.135；`MainMenuWindow.ToggleScroll(bool)` = `scrollList.enabled = option`，**ILSpy 全树 0 调用者 ⇒ `?`**） |

> **卡片的「1×1 / 1×2」是容器自己带的**（`FlexibleLayoutSizeOption.sizeX/sizeY`），**不是模式的固有属性**（同一个 Practice 两档 prefab 都在）。⛔ 别把「某模式 = 某档」写进任何表。

### 3·2 offer 容器变体（`Content` 里的子件形态）

| 状态 | 判定条件 | 尺寸 | 动态内容 | 出处 |
|---|---|---|---|---|
| **1×1**（普通 offer） | 事件数据 `ContainerRef` 指向 `… 1x1` | `535 × itemH` | `GeneralOfferPopupDrawer.Initialize(shopOffer.GetBaseData())`（`MainMenuOfferContainer.OnInitialize`） | §2·1 · `MainMenuOfferContainer.cs` |
| **1×1 Premium_*** | 同上（同名变体） | 同上 | 同上 | §2·1 |
| **Carousel 1×1** | `… Carousel 1x1` | 同上 | `CarouselOfferPopupDrawer`（多张轮播，**内容结构未读**） | §2·1 |
| **Static Image 1×1 / 1×2** | `… Static Image 1x1/1x2` | `535×itemH` / `535×(2·itemH+20)` | **无 Drawer**（只有图 + 按钮） | §2·1 |
| **计时器显隐** | `LiveopMenuContainer.OnInitialize`：`active = (activationType > Permanent) ∨ (activationType == Permanent ∧ filter.hasDuration)` | `timer.gameObject.SetActive(active)` | ⚠️ `ActivationType` 枚举的字面顺序**没读到**（只有大小关系）⇒ **精确真值表记 `?`** | `LiveopMenuContainer.cs:59-84`（同体另一例 `RankedArmyBonusContainer.cs:62-75`） |
| **背景/前景图** | 始终 | `backgroundImage.sprite = LoadAsset(MainMenuContainerBackground)`（出厂 null）· `foregroundImage.sprite = LoadAsset(...Foreground)` 且 `enabled = (sprite 非空 ?)` | 图在 liveop 资产包里，**本地 `?`（数据在服务端）** | `LiveopMenuContainer.cs:86-99` |

### 3·3 左侧导航（`Navigation Panel`）

| 状态 | 判定条件 | 参数 / 值 | 出处 |
|---|---|---|---|
| **S-N0 显示（回到主菜单）** | `MainMenuWindow.ToFocus()` | `NavigationPanelController.Toggle(true, withAnimation: true)` ⇒ `pos` 从 `(−1162.77,0)` 动画到 `(0,0)` @0.3s + `SetActive(true)` | `MainMenuWindow.cs:343-352` · `NavigationPanelController.cs:472-519` |
| **S-N1 隐藏（整屏窗盖住时）** | **由整屏窗自己调**，不是 `ToBackground` 调的（⚠️ `MainMenuWindow.ToBackground` **不动侧栏**，只 `screenBaker.Toggle(true)` + 自己 `SetActive(false)`） | `Toggle(false, true)` ⇒ 从 `(0,0)` 动画到 `(−1162.77,0)` @0.3s，完成后 `SetActive(false)`。实例：`DeckEditingWindow.TryOpen` → `Toggle(0,1,0)`，`Close()` → `Toggle(true,true)`；`DraftModeWindow` / `RankedEventWindow` 同 | `GameWindow.cs:354-363`（ToBackground 全句）· `DeckEditingWindow.cs:762,1016` |
| **S-N2 已隐藏且要求「不动画」** | `Toggle(option == 当前 activeSelf)` ⇒ `withAnimation = false` | 直接 `SetActive` + 写坐标 | 同 |
| **S-N3 选中（默认）** | `Home` Toggle `m_IsOn = 1` | `Selected highlight`（Toggle.graphic）alpha 1；其余 4 颗 alpha 0 | `MB2098` 的 `m_IsOn/toggleTransition/graphic` |
| **S-N4 选中（切页）** | `SetToggle(Collection/Shop/Rewards/Social)` 或 `SetMainToggle(...)` | `SetToggle` ⇒ `SetIsOnWithoutNotify(true)`；`SetMainToggle` ⇒ **`isOn = (toggleType == Main)`，与传入的窗口无关**（5 颗全写一遍） | `NavigationPanelController.cs:521-561` · `.c/NavigationPanelController__SetMainToggle.c` |
| **S-N5 组约束** | `ToggleGroup.m_AllowSwitchOff = 0` | 同一时刻**恰好一颗** isOn，点另一颗自动取消前一颗 | `MB2616` |
| **S-N6 点击已选中的钮** | `allowClickWhenAlreadySelected = 0` | **无反应**（不重开窗） | 5 颗 toggle MB |
| **S-N7 悬停** | `m_Transition = 1` ColorTint | **图标**染 `(0.7217,0.8152,1.0)`（淡入 0.1s）；按下 `0.7843` 灰 | 同 §2 |
| **S-N8 有活动 ⇒ 第 6 颗** | `ParentEventHandler` 有 `EventState == Active` 的事件 | `Instantiate(Event Navigation Button, buttonContainer)` → `OpenEventButton.Initialize` → `toggle.group = toggleGroup` → 加进 `navToggles`；`toggleType = 5` | `NavigationPanelController.BuildEventButtons` |
| **S-N9 无活动 ⇒ 第 6 颗隐** | `OnEventsRefreshed` 扫完没有 Active | `GetToggle(Event).gameObject.SetActive(false)`（**只关不销毁**） | `NavigationPanelController.OnEventsRefreshed`（`:457-465`） |
| **S-N10 5 颗 vs 6 颗的排布** | VLG 自动算 | 5 颗：首钮顶 **143.39** → 末钮底 **926.39**；**6 颗：143.39 → 1079.72**（容器底 1080.09，**刚好放得下**，无压缩/裁切） | §2 VLG 值 + `资料/主菜单_原版规格.md` §五 C（5 颗那列已逐颗核过） |

### 3·4 顶栏 / 聊天预览 / 背景层

| 状态 | 判定条件 | 值 / 行为 | 出处 |
|---|---|---|---|
| **S-T0 资源格颗数** | 恒显 `defaultCurrencies = {0(gold), 10(crystals), 0x14(blackStones)}`；其余 `defaultCurrencies.Contains(c) ∨ GetOwnedCount(c) > 0` | **3 颗恒显 · 最多 7 颗**（`+0x47 campaignPoints / 0x46 gachaTickets / 0x3c raidMedals / 0x48 energy(插到最前)`） | `资料/主菜单_原版规格.md` §六 第 6 条（`GeneralMenuController__Initialize.c:51-146,205` · `ResourcesBarController__Initialize.c:71-98`） |
| **S-T1 资源数字滚动** | `UIResourceCounter.SetQuantity(q, animated=1)` | 补间滚动；我们已接（`TopBar.cs:558-618`） | `decomp_full/UIResourceCounter__SetQuantity.c` |
| **S-T2 资源条 overlay** | `ResourcesBarController.ToggleOverlay(bool)` | 抬高相关件的渲染队列（我们已接 `TopBar.cs:618+`） | 同上 |
| **S-T3 顶栏按钮排** | `TopBarButtons` HLG `spacing 9.75` `MiddleLeft` 不控尺寸 | `Feedback Button` 出厂 `active = False` ⇒ **布局组跳过它**（所以只有 2 颗参与排布） | `RT/1120` + `RT/1562` |
| **S-T4 红点/角标** | `Badge Highlight` 5 颗 + `OneText` 出厂全 visible、`alpha 1`、**文本空** | 数量与显隐 **100% 运行时**（`UiBadgeNotificationManager`） | `资料/主菜单_原版规格.md` §三② B.6 |
| **S-T5 头像** | 运行时 `Avatar Item Small/Image Container/Image` **active 但 sprite 为空**（关服无头像） | 画出来 = **空黑盾**（这是原版实况，⛔ 不是缺陷） | `资料/原版实拍/menu_topbar_0927/`（运行时 dump，2026-09-27） |
| **S-T6 `Profile border` / `Profile content`** | 出厂 `active = False` | 运行时仍未激活（同一份 dump） | 同上 |
| **S-T7 聊天预览两态** | 见 §1·7 | — | `ChatPreview.cs` |
| **S-T8 背景模糊层** | `screenBaker.Toggle(true)`（`ToBackground` / `Close` / `Hide`）· `ToFocus` 时 `Toggle(false)` | 关掉主菜单/盖窗时把当前帧烘成 RT + `UI Blur` 铺满整屏 | `MainMenuWindow.cs:343-386` · `资料/主菜单_原版规格.md` §三① |
| **S-T9 `Background` 渐变** | 始终 | `c1 = (0.224,0.012,0.020)` `c2 = (0.047,0,0.016)` `angle = 82`（**我们的约定下传 188°**） | `MB1931` · §七 更正段 |
| **S-T10 Loading / Tutorial highlight** | `LoadingMenu` 出厂 `active`、内容 `Panel` 由 `GeneralMenuController` 控；`Tutorial highlight` 出厂 `False` | 加载黑场 / 教程高亮覆盖层 | `MB2578`（`loadingMenu → 1942`）· `RT/1088` |

### 3·5 **小屏 `smallScreenUI`** —— 对主菜单的**实测结论是「零影响」**（别按直觉去补）

| 探针 | 结果 | 出处 |
|---|---|---|
| `GameWindow.Open()` 会不会挂 `TransformScalerBySmallScreenUI` | **不会**：`extraScaleSmallScreen = 1.0` ⇒ `num2 = 1e-6 > num = 0` ⇒ 条件不成立 | `GameWindow.cs:84-118`（逐句）· 场景 `MB1927.extraScaleSmallScreen = 1.0` |
| 菜单里的 TMP 会不会改字号 | 场景 **19 颗**带 `smallScreenMaxFontSize` 的 TMP 里 **15 颗 = 0**（不改），**4 颗 ≠ 0 全在 toast prefab**（`Collector Level Up!` / `+25 Collector points!` / 成就弹窗两行，40/40） | `MonoBehaviour_{498,507,514,523}.json` |
| `EverguildGridLayoutGroup` 的小屏放大 | 主菜单网格是 `FlexibleGridLayout`（**不是** `EverguildGridLayoutGroup`）⇒ 不命中 | `MB1961` 的 script = `7184420427299685613` |
| ⇒ 结论 | **主菜单在 S0 桌面 / S1 小屏两档下参数完全相同**；`smallScreenUI` 只影响卡面文字档（`CardTextsInHand`）与其它窗 | §1·1 无 `smallScreenUI` 项（已 grep `AdaptFlexible…` 全文件） |

### 3·6 子菜单窗（`Base Submenu` / `Social Submenu Variant` / 四扇功能窗）

| 状态 | 判定条件 | 值 / 行为 | 出处 |
|---|---|---|---|
| **窗参（逐窗）** | 见 `Shell/*.cs` 文件头逐条实读 | 奖励窗 `updateNavPanel 1` / `closeOnESC 0`；商店 `windowsPlacement 10(World)` / `updateNavPanel 1`；收藏 `windowsPlacement 5(Canvas)` / `updateNavPanel 1` / `closeOnESC 1`；社交 = `Social Submenu Variant`（§2·2） | `Shell/{RewardsWindow,ShopWindow,CollectionWindow,SocialWindow}.cs` 文件头 |
| **页签母版** | `TabButtons.tabButtonPrefab` 非 null 时，**末项是母版**（`Initialize` 一进来 `SetActive(false)`） | 奖励窗 / 商店 = **有母版**；收藏窗 / 社交窗 = `tabButtonPrefab = 0`（**末项也是真页签**） | `TabButtons.cs:104-150` · `资料/主菜单_原版规格.md` 对照 · 我方 `MainMenuSubmenuWindow.cs:614-692` |
| **页签组** | `TabButtons.Initialize` 把每个 `toggle.group = group`，并订阅 `window` 的 tab 变更 | 单选 | `TabButtons.cs:104+` |
| **可交互开关** | `TabButtons.ToggleInteractable(tab, bool)` | 逐页签置灰（`m_DisabledColor`） | 同文件 `_003CToggleInteractable_003Eb__0` |
| **头栏返回** | `WindowHeaderWithBackButton.Initialize(title, onBack)` / `Initialize(ILiveOps, onBack)` | 标题文字 + `backButton.onClick → OnBackButtonPressed`；`OnEnable` 挂、`OnDisable` 摘 | `WindowHeaderWithBackButton.cs`（全文 79 行） |
| **小屏** | 四扇窗 `extraScaleSmallScreen` 全 = 1.0 | 同 §3·5 ⇒ **不缩放** | 各窗文件头 |
| **子菜单竖导航按钮** | `Menu Navigation Panel Button`（162.167×180 · 不在 ToggleGroup 里） | 与主菜单 5 颗**不是同一件**，别混用 | §2·2 |

---

## §4 我们代码里对应哪一行 · 现在是哪一档 · 缺哪几档

### 4·1 模式卡区（`Shell/MainMenuRuntime.cs`）

| 状态 | 我方落点 | 现在哪一档 | 判 |
|---|---|---|---|
| S-G0（16:9） | `CardCol0 = 205` · `CardColStep = 555` · `CardW = 535` · `CardH = 414.4`（`:2389`）· `CardHTall = 848.8`（`:2402`）· `CardRow0Top = 182.8`（`:2423`）· `ModesW/ModesH`（`:230-231`） | **16:9 一档，逐位正确**（`205 = 167.04 + 38`、`848.8 = 2×414.4+20`、`182.8` 推导见 §1·5） | ✅ 16:9 对 |
| **S-G1 非 16:9（窄）** | **无**：三个常量都是 `const`；`:2418-2422` 自己写明「**我们固定用 1920×1080 这一档（16:9）**：非 16:9 时原版绝对行 y 会随 aspect 变，本常量不跟着变」 | 恒 414.4 / 182.8 | 🔴 **缺这一档**（`itemH`、`row0top`、1×2 高、Content 高全不跟） |
| S-G2 / S-G3（比例夹取） | **无**：`ScreenAspectRatioController` 只在 `Battle/ScreenAspectRatioController.cs`（`BattleDriver.cs:29453` 建），壳层不建 | 5:4 / 32:9 会被拉伸（不是黑边） | 🔴 缺（**壳层全局**，不只本窗） |
| S-G4 无活动 | `BuildGameModes` 固定摆 5 张（B95_MISC 起也支持按事件数据重摆，`:2575+`） | 常有 5 张 | 🟡 有活动时会走事件分支；**空态无提示**（与原版同） |
| S-G5 有活动 | `:2575-2580`（`RefreshLiveOpContainers` 等价物，B95_MISC） | 已接（`OfferContainer` 族） | ⏳ 待验收 |
| S-G6 横滚 | `MenuScroll` + `ModeViewport`（`:2440`）· `ModeContentX2`（`:2442`） | 已实现（4 列） | ✅（但 `ModeViewport` 左缘 167.04 也是 16:9 常量 ⇒ 21:9 下偏左 52） |

### 4·2 侧栏（`Shell/MainMenuRuntime.cs`）

| 状态 | 我方落点 | 现在哪一档 | 判 |
|---|---|---|---|
| S-N0/S-N1/S-N2 显隐 | `NavHiddenX = −1162.77001953125` · `NavToggleTime = 0.3`（`:1484-1485`）· `SetNavPanelVisible(bool, bool)`（`:1499-1520`） | 与原版三支逐一对应 | ✅ |
| S-N3/S-N4 选中 | `SelectNav(int)`（`:988-1023`）· `RepaintNavHighlight`（`:1030-1040`）· 开窗入口顺带 `SelectNav(idx)`（`:1457`） | 选中态**靠「只给选中那颗画高亮」**实现 | ✅ 等价；🟡 **没有 0.1s 淡入淡出**（原版 `toggleTransition = 1` + `fadeDuration 0.1`） |
| S-N6 点已选中 | `allowClickWhenAlreadySelected = 0` 的等价物 | `SelectNav` 有「`idx == SelectedNav` 早退」（`:1456-1460`） | ✅ |
| S-N7 悬停 | `Shell/WindowButton`（ColorTint 状态机，`:50-110`） | 已实现；5 颗传 `iconTint = null`（材质白 × 状态键） | ✅（逐颗色值见 `WindowButton.cs:88` 对 MB2098 的记载） |
| S-N8/S-N9 第 6 颗活动钮 | `NavButton(... timerEndUtc, overIconTex ...)`（`:1210-1240`）· `StepEventNavTimers()`（`:2276`） | 已建、带计时器 | ✅ |
| S-N10 5/6 颗排布 | VLG 等价物（153.33 步进，`:1318` 注释） | 6 颗刚好放得下（143.39 → 1079.72） | ✅（未单独断言 6 颗边界） |

### 4·3 顶栏 / 聊天 / 背景

| 状态 | 我方落点 | 判 |
|---|---|---|
| S-T0/S-T1/S-T2 | `Shell/TopBar.cs:786+`（`BuildResourcesBar`）· `:558-618`（滚动）· `:618+`（`OverlayOn`） | ✅（还差 4 张币种小图 —— 见 `资料/普查产出_1013/W374_资源计数器.md` §六） |
| S-T7 聊天两态 | `BuildChatPreview`（`MainMenuRuntime.cs:2283-2310`）· 动画常量 `:3742`（0.6 / 0.2+0.2 / 0.4 —— 与原版逐值同）· `TickChatPreviewTween`（`:2276`） | ✅ |
| S-T8 模糊层 | `Shell/UIBakeScreen.cs` + `GameWindow.cs:597-641` | ✅ |
| S-T9 渐变 | `CardArt.Gradient(c1, c2, 188f)`（见 `资料/主菜单_原版规格.md` §五 A 实测场） | ✅ |
| S-T10 Loading / Tutorial | 不建（`MainMenuRuntime.cs:19,28` 明确「出厂 False 的不建」） | 🟡 原版加载黑场/教程高亮我们没建（属状态层，非本批） |

### 4·4 缺口清单（一句一档）

1. 🔴 **非 16:9（窄）**：`MainMenuRuntime.cs:2389/2402/2423/230-231/2440` 全是 16:9 常量 ⇒ 原版 `itemSize.y × max(1, (16/9)/aspect)`、`natH`、`row0top`、1×2 高、Content 高、`GameModes` 左缘（21:9 起）**一整套都不跟**。
2. 🔴 **比例夹取 [4:3, 22:9]**：`Shell/` 里没有 `ScreenAspectRatioController`（只有 `Battle/`）⇒ 5:4 / 32:9 原版有黑边、我们拉伸。
3. 🟡 **侧栏选中没有 0.1s 淡入淡出**（原版 `toggleTransition = 1` + `fadeDuration 0.1`；我们是建/删节点，瞬间切换）。
4. 🟡 **`GameModes` 左缘/视口宽是 16:9 常量**（`ModeViewport = (167.04, 71.33, 1920, 1080)`）：21:9 下原版 = `0.087 × 2520 = 219.24`。
5. ⚪ **`ToggleScroll` 无调用者**（ILSpy 全树）：原版也是 0 ⇒ **不算缺口**，记进 §5·5。
6. ⚪ **小屏一档不需要补**（§3·5 已证原版对主菜单零影响）—— ⛔ 别按「小屏应该放大菜单」的直觉加。

---

## §5 按文件分组的施工建议（调度台按文件切给写手）

| # | 文件 | 要做什么 | 判据（本文件哪一节） | 风险 |
|---|---|---|---|---|
| **A** | `Shell/MainMenuRuntime.cs` | 把 `CardH / CardHTall / CardRow0Top / ModeViewport / ModeContentX2` 从 `const` 改成**按 §1·1+§1·2 现算的属性**：`itemH = 414.4×max(1,(16f/9f)/(Screen.width/(float)Screen.height))`；`natH = 2*itemH+20`；`contentTop = 1080 − (0.936*(0.934*1080)+1.449)`（16:9）→ 一般化用画布高；`row0top = contentTop + (contentH − natH)/2`；1×2 = `2*itemH+20` | §1·1 · §1·2 · §1·5 · §3·1 | **中**：`_placeAllCards` / 断言 / `DraftState*` 一串子常量都挂在卡高上（`DraftStateTop 630.88` 之类 = 1×2 卡内相对坐标 ⇒ 需要确认是相对卡顶还是绝对） |
| **B** | `Core/LayoutSpace.cs`（**或**壳的相机装配处） | 补 `ScreenAspectRatioController`（[4:3, 22:9] 夹取 + 黑边）——**但这是壳层全局改动**（`A2098` 那块），会牵动所有窗与非 16:9 命中换算 | §1·4 · §4·4 缺口② | **高**：`LayoutSpace.cs:88-105` 记着「改全局映射当天就回滚」的历史；建议**先裁断再动**（见 §6·1） |
| **C** | `Shell/MainMenuRuntime.cs`（侧栏高亮） | 给 `RepaintNavHighlight` 补 0.1s alpha 淡入（原版 `toggleTransition = Fade` + `fadeDuration 0.1`），或明确记为「接受差异」 | §3·3 S-N3 · `MB2098` | 低 |
| **D** | `Editor/MainMenuScene.cs`（**调度台独占**） | 补「两档各是各的」断言：16:9 `CardH==414.4 && Row0Top==182.8`；16:10 `CardH≈460.44 && Row0Top≈204.29`；4:3 `CardH≈552.53 && Row0Top≈247.30`。⚠️ 期望值**不许从被测属性读回来**（自证） | §3·1 · §1·5 | 低（但要能改 `Screen`/相机 `aspect` 才有意义 ⇒ 依赖 B 或夹具） |
| **E** | `资料/主菜单_原版规格.md` §五 E / §四 第 6 条 | 「模式卡的行 y 是 `?（运行时）`」**已过期**：16:9 = 182.79 可由 `Content` 锚点 + `MiddleLeft` 逐式推出（本文件 §1·5），且有实拍复核（`MainMenuRuntime.cs:2413-2421` 记的那张 1503×883 实拍） | §1·5 | 无（文档） |

> ⛔ 本波**不要**去动 `_segments`（那是卡池 `RecyclableScrollRect` 的词，与本窗无关）· ⛔ **不要**把 `CardH` 改成一档了事（1×1 与 1×2 同时存在）。

---

## §5·5 **不做 / 不属本窗**（每条带判据）

### N-1 ⛔ `FactionScoreSmall` / `FactionScoreBig` **不属本窗**

两者在 `bundle_menus_assets_all`，但消费方是**排位活动窗**：`RankedEventWindow.factionScore`（`RankedEventWindow.cs:33,203`）与 `RankedTab.top4Factions/factionScorePrefab`（`RankedTab.cs:108-114,318-339`）；`FactionScore.Initialize(faction, currentScore, highestScore)` 只是「阵营图标 + 两个 `AllianceRatingDisplay`」。**主菜单场景里没有这两个 prefab 的实例**（`grep` 场景 GO 列表 = 0）。⇒ 归排位活动窗那一条线。

### N-2 ⛔ `MainMenuMission` **不属本窗**

`MainMenuMission : MissionEvent<MissionPersistence, MainMenuMissionData>`，唯一消费方 = `DailyRewardPopup : LiveOpsEventWindow<MainMenuMission>`（`DailyRewardPopup.cs:11,139-174`）⇒ 是**日常奖励窗**那一条线（P1），不是主菜单外壳。

### N-3 ⚪ `MainMenuWindow.ToggleScroll(bool)` —— 原版也没有调用者，**不补**

ILSpy 全树 `grep "ToggleScroll"` = **1 处定义、0 处调用**；UnityEvent 侧也查不到（`m_OnClick` 的 `m_Calls` 都是空）。⇒ 只记「原版有这个方法但没人调」，⛔ 别自己加一个「开窗锁定滚动」的消费者。

### N-4 ⛔ `ModifyFlexibleLayout()` 0 调用点 —— **不当作独立状态源**

`.c`/ilspy 全树只有定义。实际生效路径 = `OnEnable` + `ResolutionHasChanged`（信号）。**三份方法体逐句相同** ⇒ 无需为它单独接线。

### N-5 ⚪ 小屏档 **不补任何东西**

§3·5 五条探针全 0（`extraScaleSmallScreen 1.0` · 15/19 颗 TMP 的 `smallScreenMaxFontSize = 0` · 网格类不是 `EverguildGridLayoutGroup` · `AdaptFlexible` 不读 `smallScreenUI`）。⇒ **主菜单在 S0/S1 两档参数相同**是**原版事实**，不是我们缺。

---

## §6 **未查清**（`?` —— 本文件的全部不确定项）

### 6·1 非 16:9 的**实拍**（最重要的一条）

§1·5 那张表是**按语义推的算术**：`Expand` 画布尺寸 + `itemSize.y` 公式 + `Content` 锚点 + `MiddleLeft` 居中。**没有实拍**（本机 `runtime_ui_dump_*.tsv` 只覆盖战斗/Intro）。
**还差什么**：跑一次原版非 16:9 窗口（4:3 / 16:10 / 21:9 各一张），量「卡宽 / 卡高 / 行顶」三个绝对量。
**注**：`MainMenuRuntime.cs:2413-2421` 记着一份 **aspect ≈1.62 的实拍**复核（预测 row0top 148.5px vs 实测 149）—— 那是**同一条式子在区间内的一次成功验证**，可以当「公式对」的旁证，但**不能替代 4:3 / 21:9 的实拍**。

### 6·2 区间外（<4:3 / >22:9）的合成行为

`ScreenAspectRatioController` 夹的是**相机视口**，而 `AdaptFlexible` 读**原始 Screen**。两者在区间外不自洽（§1·5 注①）。⚠️ 「ScreenSpaceCamera 画布尺寸 = 相机 `pixelRect`」是按 Unity 语义推的，**没实拍**。
**还差什么**：一次 5:4 或 32:9 的实拍（看图就知道卡片被缩到多小、黑边在哪）。

### 6·3 `LiveopsMainMenuData` 的优先级与「已结束」态的表现

`GetEventsForDisplayingInMenu` 至少三层：`GetAllEvents()` → `Where(ShowInMainMenu)` → `Where(state ∈ {OnPreview, Active, Finished})` → `OrderBy(GetEventPriorityForMainMenu)`。
- `GetEventPriorityForMainMenu` 的**具体取值表**：`?`（`LiveOpsEvent__GetEventPriorityForMainMenu.c` 未逐句读）。
- `Finished` 态事件在主菜单上是否显示计时器/文案如何变：`?`。
**还差什么**：读 `LiveOpsEvent__GetEventPriorityForMainMenu.c` 与 `LiveOpsEvent__GetMainMenuData.c`，或拿一份 liveop 数据跑一次。

### 6·4 `ActivationType` 枚举的字面顺序

`LiveopMenuContainer.OnInitialize` 的计时器显隐依赖 `activationType` 与 `Permanent` 的**大小比较**（`> Permanent` / `== Permanent ∧ hasDuration` / `< Permanent`），但**枚举成员与数值没读到**（`LiveOpsEventData` 的类型定义在 cpp2il 产物里没独立文件）。
**还差什么**：`dump.cs` 里搜 `LiveOpsEventData` 嵌套枚举，或读 `SinglePlayerOnlyEnergyEventWindow.cs:103` / `DebugEventTogglerEntry.cs:202` 的用法反推。

### 6·5 `CarouselOfferPopupDrawer` 的内容结构

只确认了「Carousel 1×1 挂的是 `CarouselOfferPopupDrawer` 而不是 `GeneralOfferPopupDrawer`」，**它内部怎么轮播 / 有几张 / 是否改尺寸 = `?`**（未读方法体）。

---

## 附录 A · 顺手发现（不属本窗，但值得记账）

1. 🔴 **`Main Menu Offer Container 1x2` 这个 prefab 不存在** —— 简报里当作已有。现读 `bundle_menus_assets_all/GameObject/` 只有 5 份 main-menu offer：`1x1` / `1x1 Premium_booster_title_avatarOrResource` / `Carousel 1x1` / `Static Image 1x1` / **`Static Image 1x2`**。⇒ 写施工单时**别按不存在的名字去找**（同族坑：「按名字搜不到 = 本地没有」）。
2. 🟡 **主菜单的 5 颗导航钮与 `Menu Navigation Panel Button` 是两件不同的 prefab**：前者 164.31×169.68、在 ToggleGroup 里、`toggleType 0~4`；后者 162.167×180、`m_Group = 0`（**不在组里**）、高亮色 `(0.6085,0.9426,1.0)`（主菜单那颗是 `(0.7217,0.8152,1.0)`）。⇒ 别拿一个当另一个的模板。
3. 🟡 **`ScreenAspectRatioController` 在主菜单场景里是「挂在 UI Camera 上」的**（`MB1987.m_GameObject → GO 47` = `UI Camera`，`targetCameras = [Cam602]`）—— 而 `资料/主菜单_原版规格.md` §三① 把 GO47 记作 `UI Camera`（正确）。⇒ 这一条**补充**那节，不是更正。
4. ⚪ **`AdaptFlexibleGridLayoutByAspectRatio` 的 `originalItemSize` 未序列化**（JSON 里没这个键）⇒ `OnEnable` 的「≈0 就缓存」分支**必然命中**，缓存值 = `(535, 414.4)`。另：`FlexibleGridLayout.itemSize` 的 ilspy 默认值显示成 `(Vector2)1120403456`（= `float 60f` 的位模式），**那是反编译把 `new Vector2(…,60f)` 打坏了**，⛔ 别当判据（本场景序列化值就是 535×414.4）。
5. ⚪ **`Content` 的 `sizeDelta.x = 74` 是模板值**，运行时被 `FlexibleGridLayout` 改写成「用到的列数」算出来的内容宽（§1·2 尾）。我方 `BuildGameModes` 注释已记「宽只有 74：模板位」（`MainMenuRuntime.cs:2476`）——**与本节一致**。

---

## 附录 B · 覆盖边界（本文件到底查了多少）

| 维度 | 覆盖 |
|---|---|
| **读透的反编译** | `AdaptFlexibleGridLayoutByAspectRatio`（`__OnEnable.c` / `__ResolutionHasChanged.c` / `__ModifyFlexibleLayout.c` / `__OnDisable.c` + ilspy 118 行 + il2csharp 95 行）· `FlexibleGridLayout__CalculateLayoutInputHorizontal.c`（只读排布与尺寸两段）· `NavigationPanelController`（ilspy 569 行全 + `__SetMainToggle.c`）· `MainMenuWindow.cs`(965) · `LiveopMenuContainer.cs`(158) · `MainMenuOfferContainer.cs`(58) · `ChatPreview.cs`(397 只读三态) · `OpenEventButton.cs`(383 只读字段) · `ScreenAspectRatioController__AdjustCameraViewport.c` · `ResolutionManager.cs` · `GameWindow.cs`（`Open` / `ESCPressed` 两段）· `TabButtons.cs`(504 只读 Initialize 段) · `WindowHeaderWithBackButton.cs`(79 全) |
| **读透的序列化** | 场景：`MB2577`(CanvasScaler) · `RT1122/1107/1093/1540/1092` · `MB1961/1962/1955/1927/1982/1987` · `MB2199/2556/2098/2039/2040/2038/2041/2616` · `MB1947/1972` · 19 颗带 `smallScreenMaxFontSize` 的 TMP（抽查 4 颗非 0）；prefab：5 份 main-menu offer 的根 RT + 组件类 · `Event Navigation Button` · `Menu Navigation Panel Button` · `Base Submenu` · `Social Submenu Variant` · `FactionScoreSmall/Big` |
| **我方代码（只读）** | `Shell/MainMenuRuntime.cs`(5444，只读 §坐标工具 / `New` / `NavButton` / `SelectNav` / `SetNavPanelVisible` / 模式卡 / `BuildChatPreview`) · `Shell/TopBar.cs`(1128 的部分) · `Shell/MainMenuWindow.cs` · `Shell/MainMenuSubmenuWindow.cs`(913 的 tab 段) · `Shell/WindowButton.cs`(状态段) · `Core/LayoutSpace.cs`(钉高模型) · `Shell/GameWindow.cs`(模糊层) |
| **没读** | `LiveOpsEvent__GetEventPriorityForMainMenu.c` · `LiveOpsEvent__GetMainMenuData.c` · `ContainerBuilder.MakeContainer`（产物不可读，只记语义）· `CarouselOfferPopupDrawer` 全量 · `BaseSubmenu` 的 tab 生成全流程 · `MainMenuMission`/`DailyRewardPopup`（已判不属本窗） |
| **`?` 的数量** | 5 组（§6·1 非 16:9 实拍 · §6·2 区间外合成 · §6·3 事件优先级/结束态 · §6·4 ActivationType 顺序 · §6·5 Carousel 内容） |
| **没跑的东西** | Unity 批处理 / 真机 / `typecheck`（按只读代理红线，一律没跑） |
| ⚠️ 提醒 | **「本文件把参数查清了」≠「原版这个窗全复刻了」** —— 本文件只回答「哪个状态对应哪套参数」，不回答「画得像不像」（那要靠并排实拍，属调度台验收） |
