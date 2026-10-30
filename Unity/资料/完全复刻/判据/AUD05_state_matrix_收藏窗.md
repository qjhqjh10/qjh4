# AUD05 · 状态 × 参数矩阵 ——【第二窗】**收藏窗**（`Collection Menu Variant` / `CollectionScreen`）

> 代理：`B103_A05-W1`（只读查判据） · 日期：2026-10-30 · 只读 `d:/2` 与 `Unity/MyGame/Assets`，未跑 Unity、未动 git、未派子代理。
> 触发口径：铁律 5·c「一个值 ≠ 全部情况」。
> **模板** = `资料/完全复刻/判据/AUD05_state_matrix_卡组编辑.md`（§1 的通用公式与 §5·6 的四页网格表直接复用，**但 §5·6 有两格数字被本文件推翻，见 §0-1/§0-2**）。
> 本文件覆盖 `Collection Menu Variant` 整扇窗的**状态 → 参数**；**不**重做卡组编辑窗。

---

## §0 结论摘要

| # | 结论 | 强度 |
|---|---|---|
| 1 | 四页网格是**四套参数**（四个 `RecyclableScrollRect` 实例逐字段读），**`_mobileSizeScale` 不是同一个值**：Cards `1.5` · Alternate Art `1.5` · **Cardback `1.2`** · **Deck `1.25`** ⇒ **小屏列数 = 4 / 4 / 5 / 5**（模板 §5·6 写的「四页都 6→4」在 **Deck 与 Cardback 两格是错的**） | ✅ 已核实（prefab 逐字段 + `RecyclableScrollRect.cs:133-141`） |
| 2 | 四页**视口宽只有三档**：Cards 与 AltArt **同为 1589.95**（同一个 `sizeDelta = −163.05`、同一父宽 1753）、Deck 1589.29、Cardback 1584.73 ⇒ 模板 §5·6「四个视口宽互不相同」**不成立**（Cards/AltArt 是同一个数）；但「别把 Deck/Cardback 收口给它俩」这一条**仍然成立** | ✅ 已核实（`RectTransform_3034975885635478229` 与 `RectTransform_9034532822577439445` 的 `m_SizeDelta.x` 都是 `−163.05`） |
| 3 | 四个页签徽标（`Badge Highlight` → `UiBadgeNotification`）在本窗 **`notificationDefinitions = []` 全空** ⇒ **恒不显示**（不是我们没做，是原版没有来源） | ✅ 已核实（4 份 MB 逐份读） |
| 4 | 原版**开窗默认页 = Deck（卡组）** —— `Tab Buttons/Deck` 的 `EverguildToggle.isOn = 1`，Cards/CardBacks/Alternate Art 都是 0 | ✅ 已核实 |
| 5 | Deck 页 `_poolSize = 114`（其余三页 `0`）⇒ 池恒 114 格（桌面 6 列 = 19 行 · 小屏 5 列 = 23 行），**不随「你有几个卡组」变** | ✅ 已核实（prefab 字段 + `VerticalRecyclingSystem.cs:460-465` 的 `max(…)`） |
| 6 | 我们缺的档 = **Deck/Cardback 的小屏系数（1.25/1.2，现在四页一律 1.5）** + **AltArt 页 `Owned only` 初值（原版 `false`）** + **Deck 空态 EN 文案缺 `or game mode`** | ⚠️ 见 §4·3 |

---

## §1 权威公式（原版）

### 1·1 列数（四页共用同一条）

```c
// D:/2/tools/decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__ConfigureColumnNumber.c
_coloumns = floor(Content.rect.m_Width / (_spacingX + _cellWidth));   // 仅在 _useFixedSize != 0 时算
```

- 四页 `_useFixedCellSize` 全 = **1** ⇒ 四页都走这一条。
- 量的是 `Content` 的 rect 宽；本窗四个 `Content` 全是 `anchors (0,1)-(1,1)` + `sizeDelta.x = 0` ⇒ **Content 宽 ≡ Viewport 宽 ≡ Scroll View 宽**。

### 1·2 格尺寸 × 小屏系数（**四页四个值**）

```csharp
// D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/PolyAndCode.UI/RecyclableScrollRect.cs:133-141
float num  = _cellWidth;   if (GameStaticData.smallScreenUI) num  *= _mobileSizeScale;
float num2 = _cellHeight;  if (GameStaticData.smallScreenUI) num2 *= _mobileSizeScale;
```

| 页 | `_cellWidth` | `_cellHeight` | `_spacingX` | **`_mobileSizeScale`** | `_segments` | `_poolSize` | 组件（pathID） |
|---|---|---|---|---|---|---|---|
| **Deck** | 225 | 364.5 | **20** | **1.25** | 5 | **114** | `MonoBehaviour_8316412282010198741` |
| **Cards** | 262.5 | 384 | 0 | **1.5** | 4 | 0 | `MonoBehaviour_-1327870198181403947` |
| **Cardback** | 250 | 405 | 0 | **1.2** | 5 | 0 | `MonoBehaviour_7909812686046290645` |
| **Alternate Art** | 262.5 | 384 | 0 | **1.5** | 4 | 0 | `MonoBehaviour_7174171443954770645` |

> ⚠️ **`_spacingX` 不乘系数**（代码只缩放 `_cellWidth/_cellHeight`）⇒ Deck 页小屏 = `225×1.25 + 20`，**不是** `(225+20)×1.25`。

### 1·3 居中量

```
offset = ((Content.rect.width − (spacingX + cellWidth) × coloumns) + spacingX) × 0.5
x(col) = (spacingX + cellWidth) × col + offset
y(row) −= (spacingY + cellHeight)      // 四页 _spacingY 全 0
```
（`VerticalRecyclingSystem` 的 fixed-size 分支；模板 §1·3 同一式。）

### 1·4 池格 / 行数（**`_poolSize` 是下限**）

```csharp
// D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/PolyAndCode.UI/RecyclableScrollRect.cs:200-201,229
MinPoolCoverage = 1.5f;  MinPoolSize = 20;  RecyclingThreshold = 0.2f;
verticalSystem._poolSize = poolSize;            // 直接透传 prefab 的 _poolSize

// VerticalRecyclingSystem.cs:460-465
float num9  = Viewport.rect.m_Height * MinPoolCoverage;   // 需要铺满的高度
int   num10 = Math.Min(MinPoolSize, val);                 // val = 需要的格数
int   num11 = (num10 <= _poolSize) ? _poolSize : num10;   // ⇒ num11 = max(num10, _poolSize)
for (i = 0; i < num11 || coveredHeight < num9; i++) { 建一格; }
```

⇒ **池格数 = max( min(20, 需求格数), `_poolSize` )，再被「铺满 1.5 倍视口高」顶高**。

### 1·5 筛选抽屉开 / 关（**只动位置与显隐，不动任何尺寸**）

```csharp
// D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CollectionFilterController.cs:159-207（逐句）
public virtual void Toggle(bool option, bool withAnimation) {
    if (gameObject.activeSelf == option) return;          // 幂等
    DOTween.Kill(gameObject);
    if (!option) {
        if (!withAnimation) { gameObject.SetActive(false); return; }
        rectTransform.DOAnchorPosX(hiddenPosition.x, animationTime).OnComplete(() => gameObject.SetActive(false));
    } else {
        gameObject.SetActive(true);
        if (withAnimation) rectTransform.DOAnchorPosX(originalAnchorPosition.x, animationTime);
    }
}
```
- `CollectionDisplay<T>.ToggleFilters(bool)` 只是转发（`CollectionDisplay.cs:655`；`.c` 里是虚调用 `vtable+0x178`）。
- 调用点：`CollectionDisplay.Initialize` 里 **`filterController.gameObject.SetActive(false)`**（`CollectionDisplay_object___Initialize__15ec0a0.c`）⇒ **开窗时抽屉恒为关**，与 prefab 里 Deck/Cards/AltArt 那三份 `active=True` **不冲突**（运行时被代码关掉）。

### 1·6 三条「非网格」状态的触发式

| 状态 | 触发式 | 出处 |
|---|---|---|
| **空态警告** | `emptyWarning.SetActive(filteredCollection.Count < 1)`，紧跟 `ReloadData` | `decomp_generic_trial/out/CollectionDisplay_object___RefreshCollection__15ecb70.c` |
| **`Clear filters` 显隐** | `clearFiltersButton.gameObject.SetActive(toggleOptions.Count > 0)`（= **有任意一条筛选选中才显示**） | `CollectionFilterController_object___NotifyFilterChange__15f01a0.c` 末尾 `SetActive(lVar4, 0 < *(int*)(+0x48)+0x18)` |
| **选中筛选 chip 上限** | chip 被 reparent 进 `filterIconHolder`；**`if (5 < toggleOptions.Count) break;`** ⇒ **最多 5 个** | 同上，`NotifyFilterChange` 内 `if (5 < …) goto LAB(外层循环)` |

---

## §2 原版 prefab 原始序列化值

> 窗根 = `bundle_menus_assets_all/GameObject/Collection Menu Variant.json`；子节点经 `m_Children → GameObject/<name>.json`（**本 dump 的 `GameObject/` 按 `m_Name` 命名，不是 pathID** —— `RectTransform/` `MonoBehaviour/` 才是 pathID）。

### 2·1 窗根与画布

| 字段 | 值 | 出处 |
|---|---|---|
| 根组件 | `TabbedWindowComponents` + `CollectionScreen` | `MonoBehaviour_2277236495604703957` / `MonoBehaviour_-6401214277658680619` |
| `CollectionScreen.extraScaleSmallScreen` | **1.0** ⇒ **本窗在小屏档下窗口本身不缩放** | 同上 |
| `CollectionScreen.closeOnESC` / `updateNavPanel` / `windowsPlacement` | 1 / 1 / 5 | 同上 |
| `TabbedWindowComponents` | `tabButtons = 1756822263936377557` · `tabHolder = 7557460012314976981` · `tabPrefab = 0` | 同上 |
| CanvasScaler | ScaleWithScreenSize · ref **1920×1080** · **Expand** · match 0 | 复用模板 §2（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour_2577.json`，同一 UI 根） |

### 2·2 四页网格（**断言期望值的来源**）

| 页 | Scroll View 宿主 | 视口 W×H（px） | Content `sizeDelta` | 格（桌面） | 列·桌面 | 居中 PadX | 池格/行（桌面） | 格（小屏，×系数） | 列·小屏 | 居中 PadX（小屏） | 池格/行（小屏） |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **Deck** | `Tabs/Select Deck Tab/Decks Tab/Collection Display/Deck Scroll View`（`sizeDelta (−163.707,−85)`） | **1589.29 × 924** | (0,200) | 225×364.5 | **6** | **69.645** | **114 / 19** | **281.25 × 455.625** | **5** | **51.52** | **114 / 23** |
| **Cards** | `Tabs/CardsTab/Collection Display/Scroll View`（`(−163.05,−85)`） | **1589.95 × 924** | (0,300) | 262.5×384 | **6** | **7.475** | 24 / 4 | **393.75 × 576** | **4** | **7.475** | 20 / 5 |
| **Cardback** | `Tabs/Cardback Tab/Cardback Display/Scroll View`（`(−168.2657,−85)`） | **1584.73 × 924** | (0,200) | 250×405 | **6** | **42.365** | 24 / 4 | **300 × 486** | **5** | **42.365** | 20 / 4 |
| **Alternate Art** | `Tabs/Alternate Art Tab/Collection Display/Scroll View`（`(−163.05,−216.7316)`） | **1589.95 × 792.27** | (0,300) | 262.5×384 | **6** | **7.475** | 24 / 4 | **393.75 × 576** | **4** | **7.475** | 20 / 5 |

算式（**照 §1 逐条现算，⛔ 别背数字**）：
- Deck 小屏：`floor(1589.29 ÷ (281.25+20)) = floor(5.275) = 5`；`PadX = (1589.29 − 301.25×5 + 20)/2 = 51.52`（**与桌面 69.645 不同** —— 这是四页里唯一「居中量随档变」的一页）。
- Cardback 小屏：`floor(1584.73 ÷ 300) = 5`；`PadX = (1584.73 − 1500)/2 = 42.365`（**与桌面相同** —— 6×250 = 5×300 = 1500，巧合，⛔ 别写死）。
- Cards / AltArt 小屏：`floor(1589.95 ÷ 393.75) = 4`；`PadX = (1589.95 − 1575)/2 = 7.475`（**与桌面相同** —— 6×262.5 = 4×393.75 = 1575）。
- 行数：`ceil(924×1.5 ÷ cellH)` 与 `ceil(792.27×1.5 ÷ cellH)`；Cards 桌面 `ceil(1386/384)=4`、AltArt 桌面 `ceil(1188.4/384)=4`（两者同 4 行，⛔ 别以为 AltArt 矮就少一行）。

### 2·3 页面结构（prefab 逐节点，含脚本类名）

| 节点 | 关键字段 | 出处 |
|---|---|---|
| `Tab Buttons` | `Image + VerticalLayoutGroup + TabButtons + ToggleGroup + Gradient2`；四个 `EverguildToggle`（`Deck / Cards / CardBacks / Alternate Art`，`sizeDelta.y = 180`）；每个下面 `Highlight / Icon / Label/TabButtonLabel / Badge Highlight/OneText` | `Collection Menu Variant.json` 子树直读 |
| 四页签文案 | `Deck` / `Cards` / **`Cosmetics`**（节点名 `CardBacks`）/ **`Styles`**（节点名 `Alternate Art`） | 四颗 `TabButtonLabel.m_text` |
| 四页签默认态 | `Deck.isOn = 1` · 其余 = 0 | 四份 `EverguildToggle` |
| 页容器 | `Select Deck Tab`(act **F**) / `CardsTab`(F) / `Cardback Tab`(**T**) / `Alternate Art Tab`(F) —— 运行时由 `GameWindowWithTabs` 收口 | 各 `m_IsActive` |
| `Empty Collection Warning` | 四页各一份、都 `act=F`；`sizeDelta`：Deck (215,85) · Cards (245,85) · Cardback (215,85) · AltArt (0, −6e−5 ≈ 拉伸) | 子树 |
| 空态文案（TMP `m_text`） | Deck：`There are no deck in your collection for the selected filters`（**原版自己写错单复数**）· Cards/AltArt：`…cards…` · Cardback：`…cardbacks…` | 五颗 TMP 原读 |
| 筛选抽屉 | `Deck Filters`(act T, `DeckCollectionFilterController`) · `Card Filters`(act T, `CardCollectionFilterController` + 内含 `Scroll View`，内容高 **989.018** > 924 ⇒ **可滚**) · `Cosmetic FIlter`(act **F**, `CardbackFilterController`，**无 Scroll View**) · AltArt 的 `Card Filters`(act T，同 Cards，也 **989.018**) | 子树 |
| Cards/AltArt 筛选项 | `Name FIlter`(79.018) → `Owned Toggle`(50) → `Upgradable Toggle`(50, `UpgradableCardFilter`) → `Army`(150) → `Rarity`(280) → `Cost`(230) → `Type`(150) | 子树 |
| Deck 筛选项 | 仅 `Deck Name Filter`(80, `DeckNameFilter`) + `Army Filter`(345, `DeckArmyFilter`) | 子树 |
| Cardback 筛选项 | `Spacing`(15) + `Owned Toggle (1)`(50) + `Army Filter`(345, `CardBackCosmeticArmyFilter`) | 子树 |
| `Header Filters`（Cards / AltArt） | `Filter Toggle`(50) + `Separator Line` + `Filters`(GridLayoutGroup → `Clear Filter Button` 250×60) + **`WIldcard Display`(400×85)** | 子树 |
| `WIldcard Display` | `Background`(−80,44) → `Counters`(315×44) → 四组 `Common/Rare/Epic/Legendary`（各 65×44 = `Icon` 30 + `Counter` 41）→ `Army Icon`(80) | 子树 |
| `Reference Card Pointer` | **402.3,236.4 → 596.1,281.4**（193.84×45.09），无 Image，`act=T` | 子树 / 我方 `CollectionWindow.cs:5132` |
| Deck 页 `Header` | `Filter Toggle` + `Control Buttons`(740×60：`Create` 245 / `Import` 245 / `Unlock` 186=调试) + `Separator Line` + `Filters/Clear Filter Button` | 子树 |
| Cardback 页 `Header` | `Filter Toggle` + `Separator Line` + `label`(`Your cosmetics collection`) + `Filters/Clear Filter Button` | 子树 |
| AltArt 页 `Header` | `Select Art Button Right`(74.386×75.605) · `Select Art Button Left`(同) · `Art Style Logo`(512×128) · `Separator Line (1)` | 子树 |
| `Shared/Close Button` | 150×60，文案 `Back` | 子树 |

### 2·4 `CollectionDisplay<T>` 的实例级开关（四页各不相同）

| 页 | `allowItemDrag` | `autoSelect` | `filters` / `filterToggle` 接没接 | 出处 |
|---|---|---|---|---|
| **Deck** | **1** | 0 | 都接 | `MonoBehaviour_3428528622247862997`（`Collection Display_4855765916858770133`） |
| Cards | 0 | 0 | 都接 | `MonoBehaviour_-4468620502451166507` |
| Cardback | 0 | 0 | 都接 | `MonoBehaviour_7338240347955913429` |
| Alternate Art | 0 | 0 | 都接 | `MonoBehaviour_3097075314877653717` |

---

## §3 状态 × 参数矩阵（**主表**）

> 每格带出处；查不到的写 `?`，⛔ 不拿默认值顶替。数值单位 = 画布参考单位（1920×1080）。

### 3·1 会改「列数 / 格尺寸 / 行数 / 居中量」的状态

| 状态 | 判定条件 | 页 | 列数 | 格宽×格高 | 居中 PadX | 池格 / 行 | 出处 |
|---|---|---|---|---|---|---|---|
| **S0 桌面（默认）** | `GameStaticData.smallScreenUI == false`（出厂 false） | Deck | **6** | 225×364.5 + 20 | 69.645 | 114 / 19 | §1·1–1·4 · §2·2 |
| | 同上 | Cards | **6** | 262.5×384 | 7.475 | 24 / 4 | 同上 |
| | 同上 | Cardback | **6** | 250×405 | 42.365 | 24 / 4 | 同上 |
| | 同上 | AltArt | **6** | 262.5×384 | 7.475 | 24 / 4 | 同上 |
| **S1 小屏** | `smallScreenUI == true`（设置→图形页那颗开关） | Deck | **5** | **281.25×455.625** + 20 | **51.52** | 114 / 23 | §1·2（1.25）· §1·1 |
| | 同上 | Cards | **4** | **393.75×576** | 7.475 | 20 / 5 | §1·2（1.5） |
| | 同上 | Cardback | **5** | **300×486** | 42.365 | 20 / 4 | §1·2（**1.2**） |
| | 同上 | AltArt | **4** | **393.75×576** | 7.475 | 20 / 5 | §1·2（1.5） |
| **S2 非 16:9 / 其它分辨率** | CanvasScaler = Expand + ref 1920×1080 | 四页 | `floor((Wcanvas − 左栏/留白) ÷ (cellW+spacing))`，**只增不减** | 不变 | 按 §1·3 现算 | 按 §1·4 现算 | **算术推断，无实拍**（同模板 §3·1 S3 的口径；本机无菜单运行期 dump）⚠️ |
| **S3 窗口缩放档** | `CollectionScreen.extraScaleSmallScreen = 1.0` | 四页 | **不变** | **不变** | **不变** | **不变** | §2·1 ⇒ **本窗在小屏档下「窗口」不缩放**，只改格子（与卡组编辑窗同一条结论） |

### 3·2 **不改**网格、只改显隐 / 观感 / 可交互的状态

| 状态 | 判定条件 | 效果 | 出处 |
|---|---|---|---|
| **S4 筛选抽屉·关（开窗默认）** | `Initialize` 里 `SetActive(false)` | 抽屉整块隐藏；网格参数不变 | `CollectionDisplay_object___Initialize__15ec0a0.c`（`param_1[7]` 即 `filters`） |
| **S5 抽屉·开（无动画）** | `Toggle(true, false)` | `SetActive(true)`，**不动位置** | `CollectionFilterController.cs:159-175` |
| **S6 抽屉·开（有动画）** | `Toggle(true, true)` | 先 `SetActive(true)` 再 `DOAnchorPosX(originalAnchorPosition.x, animationTime)`；参数 = `hiddenPosition(−550,0)` · `animationTime 0.3` · `originalAnchorPosition` 读自 prefab 现值 | 同上 + 模板 §3·2 S5/S6 |
| **S7 抽屉·关（有动画）** | `Toggle(false, true)` | `DOAnchorPosX(hiddenPosition.x)` → `OnComplete` 才 `SetActive(false)` | 同上 |
| **S8 重复同向 Toggle** | `activeSelf == option` | **直接 return（幂等，不打动画）** | 同上首三行 |
| **S9 抽屉内容溢出** | Cards / AltArt：`Filters` 高 **989.018** > 抽屉视口 **924** | 抽屉**自己可滚**（`Scroll View` + `Viewport(Mask)`） | §2·3 |
| | Deck / Cardback：`Filters` 无 Scroll View | **不可滚**（超出即被裁） | §2·3 |
| **S10 有任意筛选选中** | `toggleOptions.Count > 0` | `Clear Filter Button` **显示**；chip 最多 **5** 个（第 6 个起 `break`） | §1·6 |
| **S11 无筛选选中** | `toggleOptions.Count == 0` | `Clear Filter Button` **隐藏** | §1·6 |
| **S12 结果为空** | `filteredCollection.Count < 1` | `Empty Collection Warning.SetActive(true)`；文案随页（§2·3） | `CollectionDisplay_object___RefreshCollection__15ecb70.c` |
| **S13 结果非空** | `Count ≥ 1` | 同上 `SetActive(false)` | 同上 |
| **S14 卡面·未拥有 或 禁卡** | `!(owned) \|\| isBanned`（见下方逐字式子） | 卡面 `greyscaleController.ToggleGreyScale(grey, **0f**)`（**瞬时**，不是 0.25s） | `CardCollectionDisplay__SetCell.c` 末段 |
| **S15 计数条·未拥有** | `ownedCount < 1` | 只灰**计数条**（底图+字），时长常量 `DAT_1834b2e7c`（模板记为 0.25s，本文件未复读该常量） | 同上中段 `UIImageGreyscaleController__ToggleGreyScale(*(lVar1+0x28), iVar4<1, DAT_1834b2e7c, 0)` |
| **S16 禁卡** | `isBanned` | `ToggleBanned(true)` ⇒ **Ban Icon 显示**（默认关）＋ **`ToggleDrag(false)` ⇒ 该格不可拖** | 同上（`CollectionItem<object>__ToggleDrag(plVar8, cVar3=='\0')`） |
| **S17 可拖重排** | `allowItemDrag ∧ collection.Count == filteredCollection.Count` | 只有两真：`DragAndDropItem.Initialize(父 ScrollRect)` + `TouchInputManager.Toggle(item, true)` | `CollectionDisplay_object___SetCell__15ee7d0.c`（`this+0x49` / `+0x50` / `+0x58`） |
| **S18 有筛选时（Deck 页）** | `filtered.Count ≠ 全量` | **拖拽整页失效**（每格都不初始化 `DragAndDropItem`） | 同上 |
| **S19 计数条文案** | 始终 | `"x{ownedCount}"`（原版 `String_Format` 常量 `DAT_18425ce10`；ilspy 同为 `$"x{arg}"`） | `CardCollectionDisplay__SetCell.c` |
| **S20 AltArt 页只有一个"风格"档** | `alternateArtStyles.Count == 1` | 左右两颗箭钮 **`interactable = false`** | `AlternateArtCardCollectionTab.cs:193-197` |
| **S21 AltArt 切换风格** | `ChangeStyleButton(isLeft)`：`idx ±1`，`<0 ⇒ Count−1`，`≥Count ⇒ 0`（**回绕**） | 换 `styleImage.sprite` 并 `UpdateCollection(GetCollection())` | 同文件 `:105-146` / `:202-224` |
| **S22 Cards 页滚动后换「阵营旗」** | 列表拖动/滚动 → `OnListDrag` → `CheckFocusedArmy` | 拿 `Reference Card Pointer` 与在屏卡格求交，**取第一个**命中的 `cardArmy` → `WildcardDisplay.Initialize(army)`；**没命中就什么都不做（不还原）** | `CardCollectionDisplay.cs:379-461` |
| **S23 上一条的**延时** | `CardCollectionDisplay.RefreshCollection` 覆盖版末尾 | `DOVirtual.DelayedCall(**0.1f**, CheckFocusedArmy)`（先 `Kill` 上一发） | `CardCollectionDisplay.cs:463-474` |
| **S24 选中一张卡（Cards 页）** | `OnItemSelected` | 开卡片详情窗，并把 `WildcardDisplay` 初始化成 **`CardArmy.Neutral`** | `CardCollectionTab.cs:341-356` |
| **S25 页签徽标** | 4 颗 `UiBadgeNotification.notificationDefinitions` **全 = []** | **恒不显示**（无信号源） | §2·3 + `MonoBehaviour_-426951423847111979` / `-5943276368383515947` / `1285835444701881045` / `297651604455482069` |
| **S26 Deck 页按活动过滤** | `currentEvent != null` | 只留 `deck.CustomGameModeEvent == currentEvent` 的卡组；为 null 时打日志 `No current event defined … Using all` 并全给 | `DeckCollectionTab.cs:41-107` |
| **S27 Deck 页排序模式** | `sortDeckMode == true` | 点格子 = `item.SetSortMode(true)`（**不**开详情窗、**不** `SaveDeckOrder`） | `DeckCollectionTab.cs:109-156` |
| **S28 Deck 页普通模式** | `sortDeckMode == false` | 开 `DeckInfoPopup(Edit)` 后调 `DeckCollectionDisplay.SaveDeckOrder()` | 同上 |
| **S29 Deck 页难度标** | `DeckCollectionDisplay.displayDifficultyLabel` | 本窗实例 = **0**（不显示难度标） | `MonoBehaviour_3428528622247862997` |
| **S30 卡片名搜索（有内容）** | `!IsNullOrWhiteSpace(m_Text)` | 六项子串命中任一即留：卡名 / 效果文字（先剥 `<…>`）/ 阵营名 / 兵种种族名 / 资料片枚举名（≠Default）/ 稀有度词条；**结果 = 传入列的反序** | `CardNameFilter.cs:38-38`/`:60-170`；`.c` 同式 |
| **S31 卡片名搜索（空 / 全空白）** | `IsNullOrWhiteSpace` | **直接返回全集**（不过滤、**不反序**） | 同上 `Filter()` |
| **S32 `Upgradable only` 开** | `toggle.m_IsOn` | `Where(c => CardServices.CheckForCardUpgrade(c, out _))` | `UpgradableCardFilter.cs:50-60` |
| **S33 `Clear filters` 清空** | 按钮点击 | 逐个 filter 的 `ClearFilters()`（名字输入框走 `SetTextWithoutNotify`）＋ `NotifyFilterChange` | `CollectionFilterController_object___ClearFilters__15ef6b0.c` · `CardNameFilter.cs:44-52` |
| **S34 打开窗口** | `CollectionScreen.Open()` | `NavigationPanelController.SetToggle(NavigationPanelToggleType.Collection)`（左侧导航高亮） | `CollectionScreen.cs:51-56` |
| **S35 返回键 / 关闭** | `BackButtonClick` | 当前页是 `SelectDecksTab` ⇒ 转它自己的 `BackButtonClicked()`；否则 `ESCPressed()` | `CollectionScreen.cs:72-119` |

#### S14 的逐字判据（照抄，⛔ 别"修好"）

```c
// D:/2/tools/decomp_full/CardCollectionDisplay__SetCell.c
bVar10 = 0 < ownedCount;                              // 已拥有
if (IsAlternateArt(card)) {
    if (bVar10) bVar10 = true;
    else bVar10 = (HasItem(card.currentAlternateArt) == 0);   // ← 注意是"未拥有异画"
}
cVar9 = 1;                                            // 默认灰
if (bVar10) cVar9 = isBanned;                         // 只由禁卡决定
ToggleGreyScale(cardUI.greyscaleController, cVar9 != 0);   // 时长 0
```
⚠️ **异画那一支的极性看起来是反的**（"没有那张异画" ⇒ 认为已拥有 ⇒ 不灰）。**两个独立反编译源（ilspy `:184-300` 与 `.c`）逐字一致**，所以这是**原版代码形状**，不是我读错。**未证实**它是否会把异画页的「未拥有」画错 —— 见 §6 `?2`。按铁律 18·0：**照抄，⛔ 别改**。

### 3·3 反查：**会改网格参数的唯一入口**（核心）

| 探针 | 结果 | 出处 |
|---|---|---|
| 全反编译树里写 `_cellWidth/_cellHeight/_segments/_mobileSizeScale` 的地方 | **0 处**（除 `PolyAndCode.UI` 自己 `Initialize` 读它们） | 模板 §3·3 已做过的全库 grep，本文件复核四页实例值后结论不变 |
| 继承 `RecyclableScrollRect` 的子类 | **0** | 同上 |
| 改四个 `Scroll View` / `Content` 的 `sizeDelta` 的代码 | **0**（`Toggle` 只 `SetActive`+`DOAnchorPosX`） | `CollectionFilterController.cs:159-207` |
| `CardbackCollectionDisplay` 自己的逻辑 | **空**（`SetCell` 只调 `base`） | `CardbackCollectionDisplay.cs`（全文 9 行） |

⇒ **结论：本窗里「列数」只由 `smallScreenUI` 一个开关决定；其余状态一律不改网格。**

### 3·4 （旁注）同窗容易拿错的**别的**网格

| 件 | 参数 | 出处 |
|---|---|---|
| `Tab Buttons` 左栏 | `VerticalLayoutGroup`（不是 GridLayout）；四项 `sizeDelta.y = 180` | §2·3 |
| Deck 页「选游戏模式卡组」子页 | `Scroll View` 高 **802**、`Content` 高 802、`HorizontalLayoutGroup + ContentSizeFitter` | 子树（`Select Game Mode Deck Tab`） |
| `Counters` 四组 | 各 65×44（`Icon` 30 + `Counter` 41），`HorizontalLayoutGroup` | 子树 |

---

## §4 我们代码里对应哪一行 · 现在哪一档 · 缺哪几档

### 4·1 四页网格（`CardPresentation/Shell/CollectionWindow.cs`）

| 页 | 我方落点 | 现在哪一档 | 判 |
|---|---|---|---|
| Deck 桌面 | `:960` `DeckCellW/H`（`×1.5`）· `:974` `DeckCols` · `:984-986` `DeckGridOffsetX` | 6 列 · 225×364.5 · PadX 69.6 | ✅ 对 |
| **Deck 小屏** | 同上，系数写死 **1.5** | **4 列 · 337.5×546.75 · PadX 89.55** | 🔴 **错**（原版 1.25 ⇒ **5 列 · 281.25×455.625 · PadX 51.52**） |
| Cards 桌面/小屏 | `:211` `CardsCellW/H`（1.5）· `:220` `CardsCols` · `:228` `CardsPadX` | 6 / 4 列 · 7.475 | ✅ 对 |
| **Cardback 桌面** | `:1793-1798` `CosmoCellW/H`（1.5）· `CosmoCols` · `CosmoPadX` | 6 列 · 250×405 · 42.365 | ✅ 对 |
| **Cardback 小屏** | 同上，系数 **1.5** | **4 列 · 375×607.5** | 🔴 **错**（原版 1.2 ⇒ **5 列 · 300×486**；PadX 42.365 恰好不变） |
| AltArt 桌面/小屏 | `:2168-2173` `StyleCellW/H`（1.5）· `StyleCols` · `StylePadX` | 6 / 4 列 · 7.475 | ✅ 对 |
| 视口常量 | `:193` `CardsViewport`(1589.7×924.06) · `:943` `DeckViewport`(1589.1×924) · `:1792` `CosmoL/R`(1584.57×924.06) · `:2166` `StyleView`(1589.79×792.33) | 四个各自独立 | ✅ 对（与 prefab 差 ≤0.25 px，**不翻列数**） |

### 4·2 非网格状态

| 状态 | 我方落点 | 现在哪一档 | 判 |
|---|---|---|---|
| 默认页 | `StartVisualIndex()` `:3425-3445` | 新实例落第一页 = **Deck** | ✅ 对（与原版 `Deck.isOn=1` 一致） |
| 抽屉开关 | `_fltOpen` `:562` / `ToggleFilters` `:2818` · 卡背 `CosmoFiltersOpen` `:2014` | 滑进滑出 · 0.3s · 行程 −385 | ✅ 对 |
| `Clear filters` 显隐 | `RefreshClearFilters()` `:3748`（四页一起刷） | 按逻辑态 | ✅ 对（口径与 `toggleOptions.Count>0` 同义） |
| 空态警告 | `:1189` Cards · `:1875` Cardback · `:2382` Styles · `:4228` Deck | 四份都建了、四份矩形各不同 | ✅ 对 |
| **空态文案（Deck）** | `Loc.T("MenuCollection/NoDecksFound")` `:4237` | EN = `There are no decks in your collection for the selected filters` | 🟡 **缺 `or game mode`**（官方 I2 值见 §附录A-2） |
| 搜索 | `Deck/DeckEditorState.cs:603`（调用）/`:684` `NameSearchMatch` | 六项 + 反序 + 空白直通 | ✅ 对（已在 2026-10-25 订正过） |
| `Owned only` 初值 | `DeckEditorState.cs:58` `DeckFilter.None.Owned = true`（四页共用） | 四页全 **开** | 🟡 AltArt 页原版是 **关**（`AlternateArtCardCollectionTab.cs:99` 无初值 = false）；结果集无差（全解锁），但**开关图形**差这一档 |
| `Upgradable only` | `:2848` 传进筛选；`DeckEditorState` 里 `if (f.Upgradable) continue;` | 打开 ⇒ **一张不剩**（无升级系统，已拍板） | ✅ 对（与原版 `CheckForCardUpgrade` 恒 false 等效） |
| 禁卡 Ban Icon | `BuildCardsBanBadge` `:6616-6655` | 按 `IsCardBanned` 显示 | ✅ 对 |
| **卡面灰度（未拥有/禁卡）** | 未实现（`:312-315` 明记"只记不画"） | 未拥有档**不可达**；禁卡档**可达**但需 LiveOps | ⚪ 见 §5·5 N-2 |
| 计数条 | `CardsCounterText` `:318-339` | `x{min(owned, cap)}`（**不是** `x{ownedCount}`） | 🔶 已知偏离（`DeckRuntime.PoolCounterText` 同口径，2026-09-28 拍板）；**未打 `🔶 自己设计` 标记** |
| 拖拽重排 | `DeckDragAllowed()` `:6109-6113` · `:4991-4993` · `:6135` | `allowItemDrag ∧ 全量相等` | ✅ 对（含"有筛选就不可拖"） |
| AltArt 单档禁用箭钮 | `:2456-2457`（`AltStyles.Length == 1 ⇒ Interactable=false`）· `ChangeStyle` `:2462-2472` | 已禁用 + 早退 | ✅ 对 |
| 野卡计数条（`WIldcard Display`） | `:1029-1119` 建树 · `SetWildcardArmy` `:5078-5104` · `CheckFocusedArmy` `:5134-5146` | 旗随焦点格变；**数字恒 99**（用户 2026-09-28 拍板） | 🟡 数字档是 `🔶 自己设计`（缺发放源）；**0.1s 延时未实现**（`:1738` 明记：批处理无帧循环 ⇒ 直接调，不等 0.1s） |
| `Reference Card Pointer` | `:5126-5134`（`WcRefL/T/R/B`） | 402.3,236.4→596.1,281.4 | ✅ 对 |
| 页签徽标 | **未建** | —— | ✅ 等价（原版恒不显示，§5·5 N-1） |
| Deck 页 Create/Import/Unlock | 本轮**未核**（本次只 grep 了网格/筛选/野卡/禁卡那几族） | `?` | ⚠️ 见 §6 `?4` |

### 4·3 缺口清单（一句一档）

1. 🔴 **Deck 页小屏**：`CollectionWindow.cs:960` 用 `SmallScreenUI.Enabled ? 1.5f : 1f`，而原版该 RSR 的 `_mobileSizeScale = **1.25**` ⇒ 小屏应 **5 列 / 281.25×455.625 / PadX 51.52**，我们是 4 列 / 337.5×546.75 / 89.55。
2. 🔴 **Cardback 页小屏**：`:1793` 同样写死 1.5，原版 **1.2** ⇒ 应 **5 列 / 300×486**，我们是 4 列 / 375×607.5。
3. 🟡 **AltArt 页 `Owned only` 初值**：原版 `false`，我们四页共用 `DeckFilter.None.Owned = true`。
4. 🟡 **Deck 空态 EN 文案**：缺官方 I2 的 ` or game mode`（`Core/Loc.cs:776` 已知，未改）。
5. ⚪ **`xN` 计数**：原版填 `ownedCount`，我们填 `min(owned, cap)` —— 有记录、有拍板，但**没打 `🔶 自己设计` 标记**（铁律 18 的机器可 grep 形式）。

---

## §5 按文件分组的施工建议（调度台按文件切）

| # | 文件 | 要做什么 | 判据（本文件哪一节） | 风险 |
|---|---|---|---|---|
| **A** | `Shell/CollectionWindow.cs`（`DeckCellW/H` `:960` 一带） | 把系数从写死的 `1.5f` 改成 **按页取值**（Deck `1.25`），并让 `DeckCols` / `DeckGridOffsetX` 走 §1·1/1·3 现算 | §1·2 · §2·2 · §4·3 缺口① | **中**：`DeckCols/DeckCellW` 被卡组格、命中矩形、拖拽落点、断言多处引用 ⇒ 换档后必须整页重建 + 冒烟 |
| **B** | 同上（`CosmoCellW/H` `:1793` 一带） | 系数改成 **Cardback `1.2`** | §1·2 · §4·3 缺口② | 低（`CosmoCols` 本来就是属性） |
| **C** | 同上（AltArt 页筛选初值） | AltArt 页的 `Owned` 初值改 `false`（其它三页保持 `true`）—— 需要给「按页的初值」一个口 | `AlternateArtCardCollectionTab.cs:99` vs `CardCollectionTab.cs:125` / `CardbackCollectionTab.cs:66` | **低但易错**：`CardsState/CosmoState/StylesState` 是 **static**，改初值要连"关窗重开回初值"那条一起想（见 `AUD03` 第 28 行） |
| **D** | `Core/Loc.cs:776` | `MenuCollection/NoDecksFound` 的 **EN** 补 ` or game mode`（ZH 已对） | 官方 I2 表 `官方本地化_en+zh-CN.json` 的 `terms` 键 | 无（纯文案）；⚠️ 有断言/文档引用了旧串的话一起改 |
| **E** | `Shell/CollectionWindow.cs:318-339` + 那条 doc | 给"`min(owned,cap)`"打 **`🔶 自己设计（非原版)`** 标记（注释就地改写，**别加行** —— 铁律 18·b） | 铁律 18 · §4·3 缺口⑤ | 无 |
| **F** | `Editor/CollectionScene.cs`（断言宿主，**调度台独占**） | 补四条「两档各是各的」断言：Deck 桌面 `6/225/69.645` ↔ 小屏 `5/281.25/51.52`；Cardback 桌面 `6/250/42.365` ↔ 小屏 `5/300/42.365`。⚠️ 期望值**不许从属性读回来**（自证） | §2·2 | 低 |

> ⛔ 本波**不要**动 `_segments`（5/5/4）：纵向系统里它不参与列数（模板 §6·3 已定案）。

---

## §5·5 判定「不做」的项

### N-1 ⛔ **四页签的 `Badge Highlight` 不建**

| 项 | 判据 | 反证 |
|---|---|---|
| 四颗 `UiBadgeNotification.notificationDefinitions` 全是 `[]` | `MonoBehaviour_-426951423847111979`（CardBacks）· `-5943276368383515947`（Cards）· `1285835444701881045`（Alternate Art）· `297651604455482069`（Deck） | 同 bundle 里别的徽标**有**配置（例如 `Social Submenu Variant` 是 `FriendsBadge`、`Rewards Base Submenu Variant` 是 `CampaignBadge/MissionsBadge`）⇒ **"全空"不是解析失败，是这四颗本来就没接信号** |

按**铁律 11 例外①**（原版本身没有可显示来源）处置；⛔ 别自己编一个"有新内容就亮"的规则（那是 §18·0 禁止的自设计）。

### N-2 ⛔ **卡面灰度（未拥有 / 禁卡）本版不画，但【记账】**

- **未拥有**那一支：我们 `owned ≥ 卡组上限 + 9 ≥ 10` **恒真** ⇒ 原版 `grey = !owned || banned` 里的 `!owned` **不可达**（同 `:312-315` 的既有记录）。
- **禁卡**那一支：`IsCardBanned` 依赖 `DeckRuntime.CurrentEventData`，无活动时为 null ⇒ false。
- **为什么不画**：原版是 `UIImageGreyscaleController`（`Image` + 灰材质对），我们的 `ImageQuad` **没有对应灰值判据** ⇒ 编一个灰度 = 拿我们自己的常量冒充原版（§二自证）。
- **何时翻案**：一旦接上 LiveOps 活动（`CurrentEventData` 非空），禁卡变可达 ⇒ **必须回来补这一档**（记进待办，⛔ 别当"已做完"）。

---

## §6 未查清（`?` 清单）

| # | 问题 | 现在知道什么 | 还差什么 |
|---|---|---|---|
| **?1** | `_poolSize = 114` 会不会把 `Content` 撑高 / 多出来的空池格是否可见 | 循环条件 `i < num11 \|\| covered < viewport×1.5` 已读实；114 > 6 列×4 行的常规需求 | 跑一次原版或读 `VerticalRecyclingSystem.CreateCellPool` 后半段（本文件只读到 `:500` 一带），确认 `Content.sizeDelta` 是由「池格数」还是「数据条数」算 |
| **?2** | 异画卡在**异画页**里的灰度极性（§3·2 S14 的"反"） | 两个反编译源逐字一致（`bVar10 = (HasItem(altArt) == 0)`） | 原版运行期实拍一张「未拥有异画的卡」；或读 `InventoryManager.HasItem` 对异画的语义（是否对"已有基础卡"返 true） |
| **?3** | 非 16:9 的实拍 | 按 CanvasScaler Expand 推出的算术 | 跑一次原版非 16:9 窗口（本机无菜单运行期 dump：`runtime_ui_dump_*.tsv` 只覆盖战斗/Intro） |
| **?4** | Deck 页 `Create Deck` / `Import Deck` / `Unlock(Debug)` 三颗按钮的显隐条件 | prefab 三颗都 `act=T`；`Unlock` 文`Debug Unlock` 明显是调试件 | 反编译里 `DeckCollectionTab`/`SelectDecksTab` 对这三颗的 `SetActive` / `interactable` 写点（**本轮未读 `SelectDecksTab`**） |
| **?5** | `CollectionScreen.windowsPlacement = 5` 的语义 | 枚举值 5 | 未读 `WindowsPlacement` 枚举定义（模板同类问题未展开） |

---

## 附录A · 顺手发现（不属本窗或值得记账）

1. 🔴 **模板 §5·6 有两格数字被推翻**：`Deck` / `Cardback` 小屏列数 **不是 4，是 5**（`_mobileSizeScale` 1.25 / 1.2）。凡按模板写断言的地方要用本表 §2·2。
2. 🟡 **官方 I2 的 `MenuCollection/NoDecksFound` = `There are no decks in your collection for the selected filters or game mode`**（`数据/游戏数据/官方本地化_en+zh-CN.json` → `terms`），而 `Core/Loc.cs:776` 的 EN 少了 ` or game mode`。
3. 🟡 **`Core/Loc.cs:769` 那条注释已过期**：它写「`zh_CN.csv` 里没有 `MenuCollection/NoCardbackFound` 这个键 ⇒ 中文自拟」；**官方表里有**（`在您选择的过滤器中，您的收藏里没有任何卡背`），且我们写的中文与它**逐字相同** ⇒ 结论没错、**理由过期**（铁律 5 口径：就地改注释）。
4. ⚪ **prefab 里 `Cardback Tab` 的 `Cosmetic FIlter` 是 `act=F`，而其它三页的抽屉是 `act=T`** —— 运行时都会在 `Initialize` 里被 `SetActive(false)` ⇒ 行为无差；但**按 prefab 静态值做断言会误判**。
5. ⚪ **`GameObject/` 目录不是按 pathID 命名的**（`RectTransform/`、`MonoBehaviour/` 才是）—— 按 `<pid>.json` 去 `GameObject/` 找会得到"文件不存在"，进而误判"节点不在这份 prefab 里"。正确做法：先按 `m_Component` 建 pid→(文件,名字) 索引（本文件用的就是这个办法）。

---

## 附录B · 覆盖边界

| 维度 | 覆盖 |
|---|---|
| **反编译读透** | `CollectionScreen.cs`(168) · `CardCollectionTab.cs`(357) · `CardbackCollectionTab.cs`(179) · `DeckCollectionTab.cs`(202) · `AlternateArtCardCollectionTab.cs`(548，读 1-400 与 150-548) · `CollectionDisplay.cs`(1389，读字段/OnEnable/Refresh/ToggleFilters/SetCell 段) · `CardCollectionDisplay.cs`(714，读 184-480) · `CardbackCollectionDisplay.cs`(9 全文) · `CollectionFilterController.cs`(824，读 1-300) · `CardNameFilter.cs`(全文) · `UpgradableCardFilter.cs`(前 60 行) |
| **反编译 `.c` 读透** | `CollectionDisplay_object___RefreshCollection/UpdateCollection/ToggleFilters/Select×2/Initialize/OnEnable/OnDisable/SetCell` · `CollectionFilterController_object___Filter/SetupFilters/ClearFilters/NotifyFilterChange` · `CardCollectionDisplay__SetCell` · `RecyclableScrollRect.cs`(PolyAndCode.UI，读 110-240) · `VerticalRecyclingSystem.cs`(读 440-500) |
| **prefab** | `Collection Menu Variant` **整棵树**（255 节点，含脚本类名）＋ 4 个 RSR 全字段 ＋ 4 个 Viewport/Scroll View/Content rect ＋ 4 个 `CollectionDisplay` 实例字段 ＋ 窗根两份组件 ＋ 4 颗徽标 |
| **我方代码** | `Shell/CollectionWindow.cs`（参数/空态/筛选/拖拽/野卡/禁卡/计数 各段）· `Deck/DeckEditorState.cs`（Filter 定义 + `NameSearchMatch`）· `Core/FilterPanelModel.cs`（字段名层面） |
| **未读** | `SelectDecksTab` 全文 · `CollectionItem<T>.Config` · `CollectionCard.cs` 全文 · `WildcardDisplay.cs` · `UiBadgeNotificationManager` · `VerticalRecyclingSystem.CreateCellPool` 后半段 · `WindowsPlacement` 枚举 |
| **`?` 数量** | **5 格**（§6） |
| **没跑的东西** | Unity 批处理 / 真机 / `typecheck`（只读代理红线，一律没跑） |
| ⚠️ 提醒 | **「参数查清了」≠「这扇窗复刻完了」** —— 本文件只答"哪个状态对应哪套参数"；画得像不像要靠并排实拍。 |
