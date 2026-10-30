# AUD05 · 状态 × 参数矩阵 ——【第一窗】**卡组编辑的卡池**

> 代理：`B103_R2`（只读查判据） · 日期：2026-10-30 · 只读 `d:/2` 与 `Unity/MyGame/Assets`，未跑 Unity、未动 git。
> 触发口径：铁律 5·c「一个值 ≠ 全部情况」（先例：卡池 6 列 vs 筛选战将 4 列）。
> **本文件只覆盖「卡组编辑窗 · 卡池网格」这一个件**。同屏其它网格（卡组列表 / 费用曲线 / 卡背页）只在 §0 点名，不展开。

---

## §0 结论摘要（先看这 5 行）

| # | 结论 | 强度 |
|---|---|---|
| 1 | 列数**不是常量**，公式 = `floor(Content.rect.width ÷ (_spacingX + _cellWidth))`；本界面 = `floor(1589.7 ÷ 262.5)` = **6 列** | ✅ 已核实（`.c` 逐句） |
| 2 | 唯一**会改列数**的运行时状态 = `GameStaticData.smallScreenUI`（小屏 UI 开关）：`_cellWidth × _mobileSizeScale(1.5)` ⇒ `floor(1589.7 ÷ 393.75)` = **4 列**，同时行数 4→5 | ✅ 已核实（ilspy + 我方实现已接） |
| 3 | **筛选面板开/关不改列数** —— `CollectionFilterController<T>.Toggle` 只做 `SetActive` + `DOAnchorPosX` 滑动，**没有任何尺寸写入** | ✅ 已核实（方法体已找到，见 §5 顺手发现①） |
| 4 | 全反编译树里**没有任何一处**运行时写 `_cellWidth` / `_cellHeight` / `_segments` | ✅ 已核实（全库 grep） |
| 5 | **「筛选战将 → 4 列」在代码/预制体里找不到路径** ⇒ 本文件判 `?`，候选解释见 §6 | ⚠️ **未查清**（不顶替） |

---

## §1 权威公式（原版）

### 1·1 列数

```c
// D:/2/tools/decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__ConfigureColumnNumber.c
if (*(char*)(this + 0x49)) {                        // 0x49 = _useFixedSize
    rect   = Content.get_rect();                    // this + 0x20 = Content
    fVar1  = *(float*)(this + 0x4c);                // _spacingX
    fVar2  = *(float*)(this + 0x40);                // _cellWidth
    fVar3  = *(float*)(rect + 8);                   // Rect.m_Width
    _coloumns = (int)floor(fVar3 / (fVar1 + fVar2));
}
```

- 量的是 **`Content` 的 rect 宽**（不是 Viewport 宽）；本界面两者相同（Content 左右拉伸、`sizeDelta.x = 0`）。
- 本界面 `_useFixedSize = 1` ⇒ 走上面那条；`_useFixedSize = 0` 的另一支是「`cellWidth = (内容宽 − (列数−1)×spacing) ÷ 列数`」，**本界面不命中**。

### 1·2 格宽/格高 × 小屏系数

```csharp
// D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/PolyAndCode.UI/RecyclableScrollRect.cs:133-141
float num  = _cellWidth;
if (GameStaticData.smallScreenUI) num  *= _mobileSizeScale;   // :134-136
float num2 = _cellHeight;
if (GameStaticData.smallScreenUI) num2 *= _mobileSizeScale;   // :139-141
```

### 1·3 居中量

```csharp
// VerticalRecyclingSystem.GetFixedSizeOffset()
num  = _spacingX + _cellWidth;
偏移 = (Content.rect.width − num × _coloumns + _spacingX) × 0.5f;
```
⇒ 桌面 `(1589.7 − 262.5×6 + 0) / 2` = **7.35 px**；小屏 `(1589.7 − 393.75×4 + 0) / 2` = **7.35 px**（两档相等是算出来的巧合，⛔ 别写死）。

### 1·4 行数（池子铺几行）

```
行数 = max( ceil(MinPoolSize ÷ 列数), ceil(Viewport.h × MinPoolCoverage ÷ cellH) )
MinPoolCoverage = 1.5f（RecyclableScrollRect.cs:200）· MinPoolSize = 20（同 :201）
```

---

## §2 卡池的**原始序列化值**（未乘任何系数的那一档）

| 字段 | 值 | 出处 |
|---|---|---|
| `IsGrid` | 1 | `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_345129740864909092.json`（脚本 `RecyclableScrollRect`，pathID `-8692637367597550244`） |
| `Direction` | 0（= Vertical） | 同上 |
| `_cellWidth` / `_cellHeight` | **262.5 / 384.0** | 同上 |
| `_spacingX` / `_spacingY` | **0.0 / 0.0** | 同上 |
| `_useFixedCellSize` | 1 | 同上 |
| `_segments` | 4 | 同上（**语义见 §6·3：在本 build 是死值**） |
| `_mobileSizeScale` | **1.5** | 同上 |
| `_poolSize` / `_controlSegmentSize` | 0 / 1 | 同上 |
| `m_Content` / `m_Viewport` | RT `3954412250944761636` / RT `-7699901740728258780` | 同上 |
| Viewport 尺寸 | `sizeDelta=(0,0)` · anchors `(0,0)-(1,1)` ⇒ 视口 **1589.7 × 924.06** | `RectTransform/RectTransform_-7699901740728258780.json` |
| Scroll View 尺寸 | `sizeDelta=(-163.05,-85.0)` · anchors `(0,0)-(1,1)` · `anchoredPos=(81.525,-42.5)` ⇒ 宽 = 1752.8 − 163.05 = **1589.75** | `RectTransform/RectTransform_8276914638760963876.json` |
| Content 尺寸 | `sizeDelta=(0,300)` · anchors `(0,1)-(1,1)` ⇒ 宽 = **1589.75**（高由 Recycler 覆写） | `RectTransform/RectTransform_3954412250944761636.json` |
| 视口遮罩 | `RectMask2D`，`m_Softness=(0,0)` · `m_Padding=0`（硬边、不内缩） | Viewport GO `6684382002724663076` |
| 窗口根缩放 `extraScaleSmallScreen` | **1.0** ⇒ 该窗 `TransformScalerBySmallScreenUI` **恒禁用** | `MonoBehaviour_5918047927837519652.json`（`DeckEditingWindow`） |
| CanvasScaler | `UiScaleMode=1`（ScaleWithScreenSize）· ref **1920×1080** · `ScreenMatchMode=1`（**Expand**）· `MatchWidthOrHeight=0` | `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2577.json` |

> 🔴 **`extraScaleSmallScreen = 1.0` 这一格很关键**：它说明**本窗在小屏档下「窗口本身不缩放」**，小屏只改**格子**（`_cellWidth/_cellHeight ×1.5`）与 TMP 那一侧，不改视口宽。
> 对照：全库 `GameWindow.extraScaleSmallScreen` 默认就是 `1f`，且**没有任何子类覆写**（ilspy 全库 grep 只有 `GameWindow.cs:52,94,96,117` 四处）⇒ `TransformScalerBySmallScreenUI` 只在「prefab 里烤了 ≠1 的 `menuScale`」的那 3 扇窗上生效，**本窗不在其中**。

---

## §3 状态 × 参数矩阵（**主表**）

> 每一格后面写出处；**没查到的写 `?`**，⛔ 不拿默认值顶替。
> 所有数值都在「画布参考单位」（CanvasScaler ref 1920×1080）下，未乘任何窗口缩放。

### 3·1 会改「列数 / 格尺寸 / 行数」的状态

| 状态 | 判定条件 | 列数 | 格宽×格高 | spacing | 居中量 PadX | 行数 / 池格 | 出处 |
|---|---|---|---|---|---|---|---|
| **S0 桌面（默认）** | `GameStaticData.smallScreenUI == false`（出厂值 = `false`） | **6** | **262.5 × 384** | 0 | **7.35** | **4 行 / 24 格** | 公式 §1·1–1·4 + §2 序列化值；出厂 `false` ⇒ `GameStaticData__.cctor`（ilspy `GameStaticData.cs:2357`） |
| **S1 小屏** | `smallScreenUI == true`（设置→图形页那颗开关 `/ GraphicsTab.SmallScreenToggleClick`） | **4** | **393.75 × 576**（×`_mobileSizeScale` 1.5） | 0 | **7.35** | **5 行 / 20 格** | `RecyclableScrollRect.cs:134-141` + §1·1 · §1·4 |
| **S2 UI 开关「手动改过」** | `smallUIChosenManually == true` | 同 S0/S1（**不影响列数**） | 同上 | — | — | — | 该字段全反编译**只有写入点**，无读取点（ilspy 全库 grep；我方 `Shell/TransformScalerBySmallScreenUI.cs:80` 已如实记「没查清」） |
| **S3 非 16:9 / 其它分辨率** | `ScreenMatchMode = Expand` + ref 1920×1080 | `floor((Wcanvas − 330.3) ÷ 262.5)` | 262.5 × 384（**不变**） | 0 | 按式现算 | 按式现算 | **Canvas 宽度只会 ≥1920**（Expand 取 `min(W/1920, H/1080)` 缩放 ⇒ 画布 = 屏幕 ÷ 该系数）⇒ **列数只增不减**：16:9→6 · 16:10→6 · 4:3→6 · 21:9(2560×1080，画布 2560)→`floor((2560−330.3)/262.5)`=**8** · 32:9→15。⚠️ 这是我按 CanvasScaler 语义推的**算术**，**没有实拍** |
| **S4 Tab = Cosmetics** | `DeckEditingWindow.ToggleDisplay(bool)` 第二参 | **6** | **250 × 405**（另一颗 RSR：`_cellWidth=250`,`_cellHeight=405`,`_segments=4`） | 0 | `(1589.7−250×6)/2` = **44.85** | `?`（未算；同 §1·4 式可算：`max(ceil(20/6)=4, ceil(924.06×1.5/405)=4)` = **4 行 / 24 格**，⚠️ 未实读该件 Viewport 高度，**标 `?` 的只有这格的池高**） | `MonoBehaviour_-1923093106525475036.json`；切换 = `DeckEditingWindow__ToggleDisplay.c`（`+0x78` cardDisplay / `+0x80` cosmeticDisplay 互斥 `SetActive`） |

### 3·2 **不改**列数、但改显隐 / 观感 / 可交互的状态

| 状态 | 判定条件 | 卡池网格 | 显隐 / 观感 / 目标值 | 出处 |
|---|---|---|---|---|
| **S5 筛选面板·关** | `CollectionFilterController<T>.Toggle(false, withAnim)` | **不变（仍 6 列）** | `SetActive(false)`（无动画时立刻）；有动画时先 `DOAnchorPosX(hiddenPosition=−550, animationTime=0.3)` 到位置再 `OnComplete→SetActive(false)` | 方法体 = `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CollectionFilterController.cs:159-207`（逐句）；参数 = `MonoBehaviour_-5979220709542203612.json`（`hiddenPosition{x:-550,y:0}`,`animationTime:0.3`） |
| **S6 筛选面板·开** | `Toggle(true, withAnim)` | **不变（仍 6 列）** | `SetActive(true)` + `DOAnchorPosX(originalAnchorPosition = prefab 现值 (−165, −42.5))` | 同上；`RectTransform_-719853638878302428.json`：`sizeDelta=(331.728,−85)` · anchors `(0,0)-(0,1)` · `anchoredPos=(−165,−42.5)` · pivot `(0,0.5)` ⇒ 面板占屏幕绝对 x **2.0 … 333.7**（**盖住 Sidebar 那 335**） |
| **S7 筛选后结果为空** | `Filter(...).Count < 1` | 网格仍在，池里 0 格（`ReloadData` 传空表） | `emptyWarning.SetActive(count < 1)` ⇒ **Empty Collection Warning** 显示 | `D:/2/tools/decomp_generic_trial/out/CollectionDisplay_object___RefreshCollection__15ecb70.c`：`SetActive(*(+0x30), *(int*)(list+0x18) < 1)` 紧接 `ReloadData(+0x20)` |
| **S8 筛选后有结果** | `Count ≥ 1` | 同上 | `emptyWarning.SetActive(false)` | 同上 |
| **S9 卡面**：未拥有 或 禁卡 | `!owned \|\| banned` | — | **卡面** `UIImageGreyscaleController.ToggleGreyScale(true, 0.25s)`（该值见 `普查产出_1024d/G1`） | `DeckEditorCollectionDisplay__DrawCell.c` → `CollectionCard__UpdateCardVisuals(canAdd, owned>0, str)`；`CollectionCard__UpdateCardVisuals.c:44-47` |
| **S10 卡面**：卡组已满（`!canAdd`，含 30/30） | `cardDeck.CanAddCard(...) == false` | — | **只有底部「张数条」底板变灰**，数字恒白；**卡面一张都不灰** | `CollectionCard__UpdateCardVisuals.c:19-20`（我方已照改：`任务总表` W6） |
| **S11 张数文案** | 始终 | — | `string.Format("{0}/{1}", Count(library), min(owned, maxCopiesInDeck))` ⇒ `CollectionCard.UpdateCardVisuals(canAdd, owned>0, text)` | `DeckEditorCollectionDisplay__DrawCell.c`：`GetMaxCopiesInDeck` + `Enumerable.Count` + `System_String__Format` |
| **S12 禁卡标记** | `PlayEventData.IsCardBanned(card)` | — | `CollectionCard.ToggleBanned(bool)`；同时 `CollectionItem.ToggleDrag(banned == 0)` ⇒ **禁卡不可拖** | 同上（`DrawCell` 尾部） |
| **S13 拖拽可用性** | `allowItemDrag`（预置 = **0**） ∧ `filtered.Count == 全集.Count` | — | 只有两条都成立才 `DragAndDropItem.Initialize(父 ScrollRect)` + `TouchInputManager.Toggle(true)` | `CollectionDisplay_object___SetCell__15ee7d0.c`（`+0x49` = `allowItemDrag`；`bVar8 = list.Count == filtered.Count`）；预置值 = `MonoBehaviour_-504617444672082140.json` 的 `"allowItemDrag": 0` |
| **S14 卡位内布局（格内）** | 始终 | 格 262.5×384 | 卡高 = `CellH − 38.272` = **345.728**；张数条底图框 x `75…187.5`、字框 y `358.29…380.98`（两档同比例 ×k） | 预置 `RectTransform_9015809549177451864.json`（anchors `(0.2857143,0.002)`/`(0.7142857,0.09966714)`）；我方已实现 `DeckRuntime.cs:762-800` |

### 3·3 反查：**列数会变的唯一入口**（这一格是本次查证的核心）

| 探针 | 结果 | 出处 |
|---|---|---|
| 全反编译树里写 `_cellWidth` / `_cellHeight` / `_segments` 的地方 | **0 处**（除 `PolyAndCode.UI` 自身在 `Initialize` 里读 + `ConfigureCellSize` 写**自己的** `_cellWidth`） | `grep -rn "cellWidth\|Segments *=" D:/2/tools/cpp2il/out_cpp2il_dev_ilspy --include=*.cs`（排除 `PolyAndCode.UI/`） |
| 继承 `RecyclableScrollRect` 的子类 | **0 个** | `grep -rln ": RecyclableScrollRect"` |
| 引用了 `RecyclableScrollRect` 的类 | `CardCollectionDisplay` · `CollectionDisplay<T>` · `CollectionItem` · `CollectionTab` · `DeckEditorCollectionDisplay` · `JoinAllianceMenu` —— **全部只调 `Initialize/ReloadData/Cells`，无一处改格尺寸** | 同上 |
| 改 `Card Display` / `Scroll View` 的 `sizeDelta` 的代码 | **0 处**（`DeckEditingWindow__ToggleDisplay.c` 只 `SetActive`；`DeckEditingPanel` 只 `SetActive`） | 逐方法读 |

⇒ **结论：`smallScreenUI` 是唯一会改列数的开关**；其余全是「不改网格、只改显隐/观感」。

### 3·4 （旁注）同屏**别的**网格 —— 免得下次拿错件

| 件 | 参数 | 出处 |
|---|---|---|
| 左侧卡组列表（`Deck List drawer`） | `GridLayoutGroup`：`cellSize 360×58` · `spacing (11,4.5)` · `pad(15,0,15,0)` · `StartAxis=Vertical` ⇒ **3 列** | `任务线/界面线/任务/T01_卡组编辑窗.md` §2·6 |
| 卡背/异画网格 | RSR `_cellWidth=250` `_cellHeight=405` `_segments=4` | `MonoBehaviour_-1923093106525475036.json` |
| 主菜单内容网格（**不是卡池**） | `AutoGridLayout`（`rows=2` `columns=20` `itemSize 535×414.4` `spacing 20`）+ 一个同节点的 `AdaptFlexibleGridLayoutByAspectRatio`（按屏幕纵横比**改 GridLayoutGroup 的 `m_CellSize.y`**） | `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1961.json` · `MonoBehaviour_1962.json` · `AdaptFlexibleGridLayoutByAspectRatio__ModifyFlexibleLayout.c` |

> ⚠️ `AdaptFlexibleGridLayoutByAspectRatio` 全库**只有 1 个实例**（主菜单场景 `Background` 下的 `Content_28`）⇒ **它不作用于卡组编辑卡池**。卡池走的是 `RecyclableScrollRect`，不是 `GridLayoutGroup`。

---

## §4 我们代码里对应哪一行 · 现在是哪一档 · 缺哪几档

### 4·1 卡组编辑窗卡池（`CardPresentation/Deck/DeckRuntime.cs`）

| 状态 | 我方落点 | 现在哪一档 | 判 |
|---|---|---|---|
| S0 桌面 6 列 | `DeckRuntime.cs:808-833`（`PoolMetrics Pool` 属性：`m.CellW = PoolCellW0 * k` · `m.Cols = floor(PoolW / (CellW+Spacing))` · `m.PadX = (PoolW − Cols×CellW)/2`） | 6 列 · 262.5×384 · PadX 7.35 | ✅ **对**（逐式同 §1） |
| S1 小屏 4 列 | 同上，`k = SmallScreenUI.Enabled ? 1.5 : 1`（`:819`） | 4 列 · 393.75×576 · PadX 7.35 | ✅ **对**（开关本体 = `Shell/TransformScalerBySmallScreenUI.cs:56-95` 的 `SmallScreenUI`，全工程唯一来源） |
| 行数 4/5 | `PoolRowsFor`（`:858-864`）+ `MinPoolCoverage/MinPoolSize`（`:753-757`） | 4 / 5 | ✅ 对 |
| S4 卡背页 250×405 | `DeckRuntime.cs:3395-3400`（`CosmoCellW=250` `CosmoCols = floor(1589.78/250)`） | 6 列 | ✅ 对（但**没乘小屏系数**，见 4·3） |
| S5/S6 筛选面板滑动 | `DeckRuntime.cs:2751-2790` 一带 | 已实现滑动 + 显隐 | ✅ 对（且**不改列数**，与原版一致） |
| S7/S8 空结果 | `RefreshPool` 里 `idx < all.Count` 不建 | — | ✅ 等价 |
| S9/S10/S12 灰度与禁卡 | `DeckRuntime.cs:3099-3103` 一带 | 已照 `G1` 判据改 | ⏳ 待验收（W6） |

### 4·2 收藏窗卡池（`CardPresentation/Shell/CollectionWindow.cs`）

| 状态 | 落点 | 现在哪一档 | 判 |
|---|---|---|---|
| S0 桌面 | `:211` `CardsCellW = 262.5f, CardsCellH = 384f` · `:220` **`CardsCols = 6`（编译期常量）** · `:228` `CardsPadX = (CardsViewport.W − CardsCols×CardsCellW)×0.5` | 6 列 | ✅ 桌面档对 |
| **S1 小屏** | **全文 0 处 `SmallScreenUI`**（`grep -n SmallScreenUI Shell/CollectionWindow.cs` = 空） | **恒 6 列** | 🔴 **缺这一档** |

### 4·3 缺口清单（一句一档）

1. 🔴 **收藏窗 Cards 页 + Styles 页**：`CardsCellW/CardsCellH/CardsCols`（`CollectionWindow.cs:211,220`）与 `CosmoCellW/CosmoCols`（`DeckRuntime.cs:3396-3398`）都是**编译期常量**，小屏档下仍是 6 列 262.5 —— 而原版走同一条 `_mobileSizeScale` 式子（两处 RSR 的 `_mobileSizeScale` 都是 1.5）。**缺「小屏 4 列 / 393.75×576」这一档。**
2. 🟡 **`Pool.W = 1589.8` 与 `CollectionWindow.CardsViewport.W = 1589.7` 是两个常量**（`DeckRuntime.cs:713` vs `CollectionWindow.cs:191`）—— 原版是同一个 prefab 字段 `1589.75`。差 0.05 不影响列数（6×262.5=1575，两档都落 6），但**是两份来源**（违反「两处写同一条规则 = 迟早不一致」）。
3. ⚪ **`_segments = 4` 在两端都没用**：我方两处都按 `floor(W/cellW)` 现算、没读 `_segments`（这与 §6·3 的结论一致 —— 它是死值）。**不算缺口。**
4. ⚪ **`smallUIChosenManually`** 我方只做写入那一半（`TransformScalerBySmallScreenUI.cs:80` 已如实标注）。原版有无读取点：**全反编译 0 处读** ⇒ **不算缺口**（我们也没多写行为）。

---

## §5 按文件分组的施工建议（调度台按文件切给写手）

| # | 文件 | 要做什么 | 判据（本文件哪一节） | 风险 |
|---|---|---|---|---|
| **A** | `CardPresentation/Shell/CollectionWindow.cs` | 把 `CardsCellW/CardsCellH/CardsCols/CardsPadX`（`:211,220,228`）改成**按 `SmallScreenUI.Enabled` 现算**，与 `DeckRuntime.Pool` **同一套式子**（建议把 `DeckRuntime` 的 `PoolMetrics` 摘成共用件，或两边调同一个静态函数） | §1·1–1·3 · §4·3 缺口① | 中：`CardsCols` 被多处引用（`:233` `CardsCell`、`:999` 注释、命中判据），换档后要 `Refresh` |
| **B** | `CardPresentation/Deck/DeckRuntime.cs` | 卡背页 `CosmoCellW/CosmoCols/CosmoPadX`（`:3395-3400`）同样接小屏系数 | §1·1–1·3 · §3·1 S4 | 低 |
| **C** | `CardPresentation/Deck/DeckRuntime.cs` | 把 `PoolW = 1589.8f` 与 `CollectionWindow.CardsViewport.W = 1589.7f` **收口成一个常量**（原版 prefab 值 `1589.75`） | §4·3 缺口② | 低（但要同步两处断言里写死的期望值） |
| **D** | `CardPresentation/Deck/DeckRuntime.cs`（只改注释） | `:2751` 那句「`CollectionFilterController<T>.Toggle` 的方法体在泛型里、`decomp_full` 无产物、我们没核过」**已过期** —— 方法体在 `decomp_generic_trial/out/` 与 `out_cpp2il_dev_ilspy/CollectionFilterController.cs:159-207` 都读得到，且**确认不写尺寸** | §5 顺手发现① | 无 |
| **E** | `资料/任务线/界面线/任务/T01_卡组编辑窗.md` | `:94` 那条「⑥ 卡池列数（原版 6 列、**我们 4 列**）」**已过期**：A870/D24 之后 `DeckRuntime.Pool` 桌面档 = 6 列 | §4·1 | 无（文档） |
| **F** | 断言（`Editor/DeckScene.cs` / `CollectionScene.cs`：**调度台独占**） | 补「两档各是各的」断言：桌面 `Cols==6 && CellW==262.5 && PadX==7.35` / 小屏 `Cols==4 && CellW==393.75 && PadX==7.35`；收藏窗同四条。⚠️ 期望值**不许从 `Pool` 读回来**（自证） | §3·1 S0/S1 | 低 |

> ⛔ 本波**不要**动 `_segments = 4`：它不是列数来源（§6·3），改了没有效果、还会误导后来者。

---

## §5·5 **不做 / 为什么**（已裁定「故意不接」的清单）

> 本节收「查出来该有、但**判定不该做**」的项 —— 每条必须带**判据 + 反证**，⛔ 不许留口头结论（铁律 5·b / 11 例外①）。

### N-1 ⛔ **`Shell/SettingsWindow.ToggleSmallScreenUI` 里【不】**补「切档后重排已开窗口的卡池」

**调度台 2026-10-30 裁定：不接**（采纳 `B103_R2` 的反证）。四条理由：

| # | 理由 | 判据 |
|---|---|---|
| ① | **原版就不是实时生效的** —— 我们自己在同一段里写了两遍 | `Shell/SettingsWindow.cs:4604-4605`「⚠️ **已经开着的窗不会跟着变** —— 原版那一段在 `GameWindow.Open()` 里、只在**开窗那一刻**挂/覆盖缩放器」；同方法末 `Debug.Log(… "⚠️ 只对**之后打开**的窗口生效 …")` |
| ② | **卡池列数只在 `RecyclableScrollRect.Initialize / ReloadData` 那一刻算** —— 原版没有任何「设置变了 ⇒ `ReloadData`」的钩子 | `PolyAndCode.UI.VerticalRecyclingSystem__ConfigureColumnNumber.c` 在 `InitCoroutine → CreateCellPool` 链上；`TransformScalerBySmallScreenUI__Initialize.c` 只在 `OnEnable` 跑 |
| ③ | **`RefreshPool()` 是 `DeckRuntime` 的私有方法，`SettingsWindow` 够不到**；要接就得在 `WindowsManager` 层广播 —— 那是**给原版加一条不存在的联动** | `Deck/DeckRuntime.cs:2981` `void RefreshPool()`（无 `public`）；全库无「设置变更 ⇒ 通知各窗」的通道 |
| ④ | **常见路径本来就对**：关掉卡组编辑 → 改设置 → 再开卡组编辑，`Pool` 属性每次访问都现算 ⇒ 自动变 4 列 | `DeckRuntime.cs` 的 `PoolMetrics Pool` 属性（每次 get 现算，`:808-833`） |

**结论**：补了 = **偏离**（铁律 11 例外②的镜像面：原版没有的行为不做，§18·0）。若将来真要接，**必须同时改掉 `:4604-4605` 与末尾那句 `Debug.Log`**，否则文档与行为打架。

### N-2 ⛔ **`_segments = 4` 不接、不改**

见 §6·3：纵向网格里它**不参与列数**（`_rows` 在 `VerticalRecyclingSystem` 里 0 处使用）。改了没有效果，还会误导后来者以为「列数来自它」。

### N-3 ⛔ **`smallUIChosenManually` 只写不读 —— 不算缺口、不补**

全反编译**只有写入点**、**0 处读取**（我方 `Shell/TransformScalerBySmallScreenUI.cs:80` 已如实标注）。我们照「只做写入那一半」实现 = 与原版同形，⛔ 别补一个原版没有的消费者。

---

## §5·6 收藏窗**四页网格** —— 桌面 ↔ 小屏 **期望值总表**（**断言期望值来源**）

> 🔴 **这张表就是断言该写的数字**。⛔ **不许从 `CardsCols` / `CosmoCols` / `DeckCols` / `StyleCols` 读回来自证** —— 来源是 prefab 序列化字段（`_cellWidth/_cellHeight/_spacingX/_mobileSizeScale`）+ 原版式子 `floor(视口宽 ÷ (_cellWidth + _spacingX))`（`PolyAndCode.UI.VerticalRecyclingSystem__ConfigureColumnNumber.c`）。
> 与 §3·1 的区别：**本表是收藏窗**（`CollectionWindow`）的四页；§3·1 是**卡组编辑窗**的卡池。

| 页 | 视口宽（px） | 格（桌面档） | spacing | **列数·桌面** | 格（小屏档，×`_mobileSizeScale`） | **列数·小屏** | 居中/偏移量 | 我方落点 |
|---|---|---|---|---|---|---|---|---|
| **卡组** `Deck` | **1589.1**（`DeckViewport` 330.9→1920） | **225 × 364.5** | X **20** · Y **0** | `floor(1589.1 ÷ 245)` = **6** | **281.25 × 455.625**（**×1.25**） | `floor(1589.1 ÷ 301.25)` = **5** | **69.55** / **51.43**（`DeckGridOffsetX`） | `CollectionWindow.cs` `DeckCellW/DeckCellH`（属性）· `DeckCols`（属性）· `RebuildDeckListNow()` |
| **卡池** `Cards` | **1589.7**（`CardsViewport` 330.2→1919.9） | **262.5 × 384** | **0** | `floor(1589.7 ÷ 262.5)` = **6** | **393.75 × 576**（**×1.5**） | `floor(1589.7 ÷ 393.75)` = **4** | **7.35** / **7.35**（`CardsPadX`） | `CardsCellW/CardsCellH`（属性）· `CardsCols`（属性）· `RebuildCardsPage()` |
| **卡背** `Cosmo` | **1584.57**（`CosmoR − CosmoL` = 1920.01 − 335.44） | **250 × 405** | **0** | `floor(1584.57 ÷ 250)` = **6** | **300 × 486**（**×1.2**） | `floor(1584.57 ÷ 300)` = **5** | **42.285** / **42.285**（`CosmoPadX`） | `CosmoCellW/CosmoCellH`（属性）· `CosmoCols`（属性）· `RefreshCosmoAfterFilter()` |
| **异画** `Styles` | **1589.79**（`StyleView.W` 330.22→1920.01） | **262.5 × 384** | **0** | `floor(1589.79 ÷ 262.5)` = **6** | **393.75 × 576**（**×1.5**） | `floor(1589.79 ÷ 393.75)` = **4** | **7.395** / **7.395**（`StylePadX`） | `StyleCellW/StyleCellH`（属性）· `StyleCols`（属性）· `RebuildStylesPage()` |

**读表三条**：

1. ⚠️ **2026-10-30 就地订正（铁律 5）**：原写「**四个视口宽互不相同**（1589.1 / 1589.7 / 1584.57 / 1589.79）⇒ ⛔ 不许收口成一个常量（原版是四个不同 prefab 的 `Content` 宽）」—— **前半句不成立**：prefab 里**卡池与异画是同一个** `sizeDelta.x = −163.05`（视口同 **1589.95**）；我方那两个常量 1589.7 / 1589.79 的 0.09 差是两次独立实测的**舍入差**，不是两个不同 prefab ⇒ 「Cards 与 Styles 必须分开」**没有判据**。✅ **仍然成立的是**：卡组 **1589.1** 与卡背 **1584.57** 确实各自独立 ⇒ ⛔ 别把它们收口给另外两页（判据 → `判据/AUD05_state_matrix_收藏窗.md` §0-2）。
2. **`spacingX` 不乘系数** —— 原版 `RecyclableScrollRect.Initialize` 只缩放 `_cellWidth/_cellHeight`；`_spacingX` 用原值（卡组页那 20 就是 20）。
3. ⚠️ **2026-10-30 就地订正（铁律 5）**：原写「桌面↔小屏的列数都是 **6→4**」—— **只有卡池/异画是 6→4**；**卡背 6→5**（`_mobileSizeScale` **1.2**）、**卡组 6→5**（**1.25**）⇒ 四页小屏列数是 **4 / 4 / 5 / 5**（判据 → `判据/AUD05_state_matrix_收藏窗.md` §1·2，prefab 逐字段复验）。四页仍各是独立算式；断言要么逐页写四条字面量，要么用本表的算式现算，⛔ 别用一页的结果推另一页。

**自检口**：`CollectionWindow.UiSetSmallScreenUIForTest(bool on)`（临时置 `PersistOverride`，**不落盘**）→ 重排已建页（`Deck` / `Cards` / `Cosmo` / `Styles` 四页，各自用**已存在的刷新入口**；未建页按 scroll 字段为 null 跳过，不触发懒建）。

---

## §6 **未查清**（`?` —— 本文件的全部不确定项）

### 6·1 「筛选战将 → 4 列」到底是什么

> 🔴 **调度台 2026-10-30 裁断：先不按它改。** 三条理由（⛔ 别推翻）：
> ① 铁律 3 —— **截图只能验收、不能定参数**，而这两张都是**视频帧**、裁切缩放未知；
> ② 本文件 §3·3 的四条探针全 0（`ConfigureColumnNumber` 只在 `_useFixedSize≠0` 时现算 · **0 处**运行时写 `_cellWidth/_cellHeight/_segments` · `RecyclableScrollRect` **0 子类** · `Toggle` 只 `SetActive`+`DOAnchorPosX`）⇒ **代码层没有这条路径**，按截图硬做 = 把不存在的状态做进产品（§18·0）；
> ③ 但**要留一条待办**（见下面的 `待实拍` 格子）。

**待办（本格由 `?` 改成 `待同机同窗尺寸的筛选开/关成对实拍`）**

| 待办 | 要拿到什么 | 拿到后第一个验哪条 |
|---|---|---|
| **AUD05-?1 · 待实拍** | 一张**同一次录制、同一窗口尺寸**下「筛选开 / 筛选关」的**成对**实拍，并记录当时**设置→图形页 Small UI 档**；或者在同一帧上量出**卡位宽**与**视口宽**两个**绝对量**（不是卡片墨迹） | **候选③**：筛选把池压窄 ⇒ `1589.7 − 331.7 = 1258` ⇒ `floor(1258 ÷ 262.5) = 4`（**算术正好落 4**）。⚠️ 但预置里 `Card Filters` 是 `Card Display` 的**子件**、`Scroll View` 是**兄弟且恒拉伸**，且全库 **0 处**写 `Scroll View`/`Content` 的 `sizeDelta` ⇒ 要先证明「谁在运行时改了池宽」 |

**判据速算**：只要量出 `视口宽 ÷ 卡位宽` 落在 `(6,7)` 就是 S0（桌面 6 列）；落在 `(4,5)` 就是 S1（小屏 4 列）。**两者都不落** ⇒ 是真有第三条路径，再回来查。

- **观测事实**（用户实拍，桌面 `C:/Users/qjh36/Desktop/warpforge参考图/卡组编辑里选择督军（阵营）的参考.png`，1576×884）：卡组编辑窗在**筛选面板打开**、卡池里**只有战将**（乌列尔·文特里斯 / 副官提图斯 / 瓦利乌斯·帕克索 / 瓦罗·提古里乌斯 …）时，一行 **4 张**；同目录 `卡组编辑界面参考.png`（1578×884，筛选面板关）一行 **6 张**。
- **代码层结论**：**找不到任何一条路径能让筛选改列数**（§3·3 四条探针全 0）。
- **三条候选解释（均**未证实**，按可信度排）**：
  1. **两张图的 UI 缩放档不同**（截图 2 是另一次录制，`smallScreenUI` 可能开着）。**支持**：`smallScreenUI=true` 时 `floor(1589.7/393.75)=4.037` ⇒ **恰好 4 列**。**反对**：按像素量，两图同一件（如「返回」钮）只差 ~1.12–1.2 倍，不到 1.5 倍；但两张都是**视频帧**，裁切/缩放未知 ⇒ **像素比例不可当尺子**（铁律 3）。
  2. **该帧的分辨率/画布宽不同**。**反对**：`ScreenMatchMode = Expand` ⇒ 画布宽 ≥1920，只会让列数**变多**，不可能变 4。
  3. **筛选面板打开时卡池内容被压窄**（见上表：`1258 ⇒ 4`，**算术正好落 4**）。**反对**：预置中 `Card Filters` 是子件、`Scroll View` 是恒拉伸兄弟，`Toggle` 只移面板；**全库 0 处写池宽**。
- **还差什么**：见上表「待实拍」那一行。
- 🔴 **在拿到上面任一条之前，⛔ 不要照「筛选 ⇒ 4 列」改代码** —— 那会把一个不存在的状态做进产品。

### 6·2 非 16:9 的实拍

§3·1 S3 那行是**按 `CanvasScaler` 语义推的算术**，**没有实拍**（本机无原版运行期菜单 dump：`runtime_ui_dump_*.tsv` 只覆盖 `Battle_Arena_1` / `Intro` / `drive`，`grep "Deck Editing"` = **0 命中**）。要坐实需跑一次原版非 16:9 窗口。

### 6·3 `_segments = 4` 的语义

- `RecyclableScrollRect.Initialize` 里 `rows = 1; if (IsGrid) rows = segments; _rows = rows;`（ilspy `RecyclableScrollRect.cs:212-219`）。
- 但**纵向**系统 `VerticalRecyclingSystem` 的列数来自 `ConfigureColumnNumber`（§1·1），**`_rows` 在该类里除继承外 0 处使用**（`grep -n "_rows" VerticalRecyclingSystem.cs` 只命中 124 行一处、且是 `_coloumns`）。
- ⇒ 在本 build 的**纵向网格**里 `_segments` **不参与列数**。⚠️ 但它在**横向**系统（`HorizontalRecyclingSystem`）里是否是「行数」，**未查**（本窗 `Direction=0` 走纵向，不影响本结论）。
- 我方文档已记「`_segments` 是死值」（`任务线/界面线/任务总表.md:75`）—— **与本文件一致**。

### 6·4 卡背页（S4）的池高

`MonoBehaviour_-1923093106525475036` 的宿主 Viewport 高**未实读** ⇒ S4 的「行数」那格标了 `?`（用同一条式子估得 4 行，**不是判据**）。

---

## 附录 A · 顺手发现（不属本窗，但值得记账）

1. 🔴 **「`CollectionFilterController<T>.Toggle` 无产物」已过期** —— `DeckRuntime.cs:2751` 写着「方法体在泛型里、`decomp_full` 无产物、我们没核过」。**实际读得到**：`D:/2/tools/decomp_generic_trial/out/CollectionDisplay_object___ToggleFilters__15eec60.c` 与 `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CollectionFilterController.cs:159-207`。读出的结论 = **只 `SetActive` + `DOAnchorPosX`，不写任何尺寸** ⇒ 我们按「只滑动」实现是**对的**，且现在可以去掉「没核过」的免责。
2. 🟡 **`GameWindow.extraScaleSmallScreen` 恒 `1f`，全库 0 处覆写** ⇒ `TransformScalerBySmallScreenUI` 在**所有**代码建的窗上都是禁用的，只有 prefab 里烤了 `menuScale ≠ 1` 的那 3 扇（`Alliance Trophy Info Popup` / `Member Options Panel` / `Generic Options Panel`，均 1.35）会生效。这条与我方 `Shell/TransformScalerBySmallScreenUI.cs:30-33` 的记载**吻合**，无需改；但**「小屏窗会整体放大」这个直觉是错的**，别按它设计。
3. 🟡 **`AutoGridLayout`（原版自定义 `GridLayoutGroup`）在 `bundle_Waprforge_monoscripts` 里查不到 `MonoScript`** —— 但实例确实存在（主菜单场景 `MonoBehaviour_1961.json`，字段 `rows/columns/itemSize/spacing`）。⇒ 判「某类本地没有」时**别只查 monoscripts 包**（与 CLAUDE.md「按类名搜永远 0 命中」同族）。
4. ⚪ `AdaptFlexibleGridLayoutByAspectRatio` 的 `__ctor.c` 内容其实是 `PhotonHandler___ctor`（`decomp_full` 的文件名与内容不符的**又一例**，第 7 例）。不影响本文件结论（没用到它的构造）。

---

## 附录 B · 覆盖边界（本文件到底查了多少）

| 维度 | 覆盖 |
|---|---|
| **文件** | 读透：`RecyclableScrollRect.cs`(349) · `VerticalRecyclingSystem.cs`(1356，只读列数/居中/池格三段) · `CollectionDisplay.cs`(824，只读 Refresh/Toggle/SetCell) · `DeckEditorCollectionDisplay.cs`(472) · `CollectionFilterController.cs`(824) · `DeckEditingPanel.cs`(509) · `DeckEditingWindow__ToggleDisplay.c`；预制体 6 个 RectTransform + 8 个 MonoBehaviour |
| **未读** | `HorizontalRecyclingSystem.cs`（本窗不走横向）· `DeckEditingWindow` 其余 40+ 个方法 · 收藏窗的 `CardCollectionDisplay` 全量（只核了参数） |
| **反编译源** | ① `decomp_full/*.c`（Ghidra）② `cpp2il/out_cpp2il_dev_ilspy/*.cs`（真方法体，主力）③ `decomp_generic_trial/out/*.c`（泛型类方法体，**`CollectionDisplay<T>` 只有这里有**） |
| **`?` 的数量** | 3 格（§6·1 的「筛选战将」整行 · §6·2 非 16:9 实拍 · §6·4 卡背页池高） |
| **没跑的东西** | Unity 批处理 / 真机 / `typecheck`（按只读代理红线，一律没跑） |
| ⚠️ 提醒 | **「本文件把参数查清了」≠「原版这个件全复刻了」** —— 本文件只回答「哪个状态对应哪套参数」，不回答「我们画得对不对」（那要靠并排实拍，属调度台的验收） |
