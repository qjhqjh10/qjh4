# AUD05 · 状态 × 参数矩阵 ——【第二窗】**商店窗**（`Shop Menu Variant` / `ShopWindow`）

> 代理：`B103_A05-W2`（只读查判据） · 日期：2026-10-30 · 只读 `d:/2` 与 `Unity/MyGame/Assets`，未跑 Unity、未动 git、未改任何 `.cs`/账/正本。
> 触发口径：铁律 5·c「一个值 ≠ 全部情况」（模板 = `判据/AUD05_state_matrix_卡组编辑.md`）。
> **本文件只覆盖「商店窗」这一件**：窗根 / 左栏页签 / 各页的货架栅格 / 商品格 / 价格钮 / 倒计时 / VIP 态 / 限购态。
> 同族的**商品条目族 19 变体**（`General Basic Offer Container *`）与 **Booster Info/Open 两扇窗**只在 §0/§3 点名，不展开 —— 它们的判据正本仍是 `资料/阶段二_商店_原版规格.md` §五。

---

## §0 结论摘要（先看这 9 行）

| # | 结论 | 强度 |
|---|---|---|
| 1 | 🔴 **商店格是 4 列，不是 2 列。** `GridLayoutGroup.m_Constraint = 0`（**Flexible**）⇒ `m_ConstraintCount: 2` 是**死值**；真实列数 = `floor(Content 宽 ÷ cellSize.x)` = `floor(1590.24 ÷ 335.6)` = **4**。⚠️ **我们按 `m_ConstraintCount` 做成了 2 列**（`ShopTabPage.cs:113`）——这是本文件最重要的一条缺口 | ✅ 已核实（UGUI 源码 `GridLayoutGroup.cs:237,262` + prefab 实读） |
| 2 | 商店页有**两个结构完全不同的参数族**：**Grid 族**（Card / Item / VIP / 母版 `Shop Tab`）= 纵向滚动 + 4 列 335.6×475 + 小屏 ×**1.18**；**Flexible 族**（`Packs Tab`=`BoosterShopTab` / `Generic Shop Tab`）= **横向滚动 + 2 行货架** + 小屏 ×**1.05** | ✅ 已核实（8 份 prefab 逐个读 `contentLayoutElementsSizeMultiplierSmallScreens`） |
| 3 | `Daily Shop Tab` 的**根上没有脚本**（只有 RT + 一个未导出的组件）⇒ 它**不建商品、不管倒计时、不排序**；原版这个 build 里 Daily 页的驱动类**不存在** | ✅ 已核实（prefab `m_Component` 只有 2 项；全库无 `DailyShopTab` 类） |
| 4 | 小屏系数 **1.18 是算出来的**：`4 × (335.6×1.18) = 1584.03 ≤ 1590.24` —— 乘完仍然是 **4 列**，只是格子变大。⇒ 这是「4 列」的**第二条独立佐证**（若真是 2 列，1.18 这个值毫无意义） | ✅ 算术（反证） |
| 5 | 价格钮 = **4 档态**：`Open ({n})`（已拥有）/ 正常价 / 限购售罄·货币不足（`interactable=false`）/ 真钱无报价（`--`）；另有 `Owned` 文案档（格子层面）与 `Free` / `<s>` 删除线 | ✅ 已核实（`PriceDisplayButton.cs:52-145` + `CatalogItemContainer.cs:240-283`） |
| 6 | 倒计时 **4 档**：隐藏（`CheckTimer` 假）/ 正常 / **最后 5 分钟**（`MainMenu/LastMinutes`）/ 已过期（整块 `SetActive(false)` + `OnRunOutOfTime` 只发一次） | ✅ 已核实（`TimerDisplay__UpdateTimer.c` 逐句 + 常量 5.0） |
| 7 | VIP 页两态由 **`Store.CanPurchase`（本地逻辑）**推开，不是服务端：`VIP Status` / `No VIP` 遮罩；**VIP 到期那一刻**由 `vipTimer.OnRunOutOfTime` 立刻切档 | ✅ 已核实（`VIPCardShopTab.cs:213-277`） |
| 8 | `Empty Collection Warning` 在**商店这条链上没有任何写入点** ⇒ 原版**恒不显示**（我们恒隐藏 = 与原版同形，**不是缺口**） | ✅ 已核实（`ShopTab.*` 全方法 + prefab 出厂 INACT） |
| 9 | **`CardShopFilter` 不是 UI 筛选面板** —— 它只是一个 `ScriptableObject` 黑名单（`itemBlacklist` / `blacklistedArmies` / `blacklistedExpansions`），**全库唯一引用方** = `DailyStoreData.blackListedFilter`（每日商店选品） | ✅ 已核实（全库 grep 只命中 2 个文件） |

---

## §1 权威公式（原版）

### 1·1 列数 —— `GridLayoutGroup` 的 **Flexible** 支

```csharp
// D:/4/Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/GridLayoutGroup.cs
// :237
float width = rectTransform.rect.size.x;          // = Content 自己的 rect 宽（本件 = Viewport 宽 = 1590.24）
// :243-256  FixedColumnCount ⇒ cellCountX = m_ConstraintCount;  FixedRowCount ⇒ cellCountY = m_ConstraintCount;
// :258-263  else（= Flexible / 我们的 m_Constraint = 0）
cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
```

代入本件（`padding.horizontal = 0+0 = 0` · `spacing.x = 0` · `cellSize.x = 335.6` · `width = 1590.24`）：

```
cellCountX = floor((1590.24 + 0.001) / 335.6) = floor(4.7385) = 4
```

- 🔴 **`m_ConstraintCount: 2` 只在 `FixedColumnCount`/`FixedRowCount` 两档被读**（`GridLayoutGroup.cs:245,253`）⇒ **在 Flexible 下是死值**。我们的实现把它当成了列数（`ShopTabPage.cs:113` `GridCols = 2`）。
- 左右留白：`4 × 335.6 = 1342.4`，`1590.24 − 1342.4 =` **右剩 247.84 px**（`m_ChildAlignment = 0 UpperLeft`，不留左侧）。
- **小屏档**：`cellSize.x × 1.18 = 396.008` ⇒ `floor(1590.24 / 396.008) = 4` ⇒ **仍是 4 列**（`4 × 396.008 = 1584.03`，只余 6.21 px）。这一格是「4 列」的独立反证。

### 1·2 行数 / 内容高

`Content` 挂 `ContentSizeFitter`（`m_HorizontalFit = 0`（不受限）· `m_VerticalFit = 2`（PreferredSize），`MonoBehaviour_-5152081396385324882.json`）
⇒ 高由 `GridLayoutGroup` 的 preferred height 给（同一条式子，`GridLayoutGroup.cs:184-185`）：

```
rows     = ceil(商品数 ÷ 4)
contentH = rows × 475 + padding.vertical(= top 7 + bottom 0)      // 原版就是这么算的
contentW = 不受限 ⇒ 恒 = Viewport 宽 1590.24（anchors (0,0)-(1,1)、sizeDelta.x ≈ 0.000122）
```

⚠️ 我们写的是 `contentW = GridCols × CellW`（`ShopTabPage.cs:406` = 671.2）—— **原版 Content 是拉伸的**，宽不由格数决定。

### 1·3 小屏系数（`ShopTab.Setup`，两支互斥）

```csharp
// D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ShopTab.cs:186-235
if (!GameStaticData.smallScreenUI) return;                       // :186-189  非小屏 ⇒ 直接返回，一格都不改
if (gridLayoutContent) {                                        // :190  Grid 族
    if (originalGridLayoutGridSize ≈ zero) originalGridLayoutGridSize = gridLayoutContent.m_CellSize;  // :203-209 首次缓存
    gridLayoutContent.cellSize = originalGridLayoutGridSize * contentLayoutElementsSizeMultiplierSmallScreens;  // :210-211
}
if (flexibleGridLayoutContent) { … flexibleGridLayoutContent.ItemSize = itemSize * multiplier … }           // :213-235
```

- **只乘一次、只在小屏**；缓存字段 `originalGridLayoutGridSize` / `flexibleGridLayoutItemSize` 防止反复放大。
- 乘数**按 prefab 不同**（不是全局常量）：Grid 族 = **1.18**、Flexible 族 = **1.05**（§2 逐份实读）。

### 1·4 Flexible 族：**2 行货架、横向滚动**

`FlexibleGridLayout`（`bundle_menus_assets_all` 里 `m_Script` → 类名 = `FlexibleGridLayout`，**不是** UGUI 的 `GridLayoutGroup`）：

```
Content 宽 = (itemSize.x + spacing.x) × 用到的列数 − spacing.x + pad.left + pad.right   // FlexibleGridLayout.cs:270-280
子件位置   = (pad.left + col × (itemSize.x + spacing.x), pad.top + row × (itemSize.y + spacing.y))
rows = 2（序列化） · columns = 200（序列化，= 上限）⇒ 列数由**商品件数**决定，不由视口宽决定
```

代入 `Packs Tab`：`itemSize 372.9×427.9` · `spacing (28,32)` · `pad (50,25,25,0)` ⇒ 两行高 = `2×427.9 + 32 + 25 = 912.8 ≤ Viewport 高 1009.06`（正好放得下）⇒ **ScrollRect 走横向**。

> ⚠️ 子件坐标那条是本类 `CalculateLayoutInputHorizontal` 的算式推得（`.c` 有混淆、`& 0` 那处被吃掉了），**位置未逐位核**；**内容宽/行数**两条已核实。

### 1·5 倒计时（`TimerDisplay.UpdateTimer`）

```c
// D:/2/tools/decomp_full/TimerDisplay__UpdateTimer.c
if (target == null) return;                                   // :25  没目标 ⇒ 不更新（保留出厂）
ts = target.Value - PlayerDataManager.CurrentServerDateTime();
prefix = useExtraText ? extraText : "";
if (useLastMinutesText && ts > 0 && ts < TimeSpan.FromMinutes(5)) {   // :100-119  ← 常量 5.0（ilspy `FromMinutes(5.0)`）
     timer.text = lastMinutesText;  FixLayout();  return;      // :120-136  只换整句，不拼数字
}
if (ts > 0) { timer.gameObject.SetActive(true); timer.text = prefix.Contains("{0}") ? Format(prefix, GetTimeString(ts)) : prefix + GetTimeString(ts); }
else        { timer.text = ""; timer.gameObject.SetActive(false); if (!hasRunOutOfTime) { hasRunOutOfTime = true; OnRunOutOfTime?.Invoke(); } }
```

- 商店四份 `TimerDisplay` 的序列化值**全都一样**：`useExtraText = 0` · `extraText.mTerm = ""` · **`useLastMinutesText = 1`** · `lastMinutesText.mTerm = "MainMenu/LastMinutes"`（官方表 EN = `Last Minutes`）。
- `OnRunOutOfTime` **只发一次**（`hasRunOutOfTime` 闸）。商店里**订阅它的只有 `VIPCardShopTab.vipTimer`**（→ `CheckVIPState`）与 `ShopOfferContainer.timer`（→ `RefreshExpire`）；**页头的 `TimeCounter` 没有任何订阅方**（`ShopTab.Setup` 只 `Initialize`，不订阅）。

### 1·6 限购文案（`ShopOfferContainer.SetAvailableText` / `CatalogItemContainer.Refresh`）

```
hasLimit = availability > 0                            // ShopOfferContainer__Refresh.c：`0 < iVar1`
文字     = $"{Loc("MenuShop/Available")}:{max(0, availability − 已购)}/{availability}"
显隐     = availableCount.gameObject.SetActive(hasLimit)      // ⚠️ 只有 ShopOfferContainer 有这一句
```

⚠️ **`CatalogItemContainer.Refresh` 里【没有】这句 `SetActive`** —— 它只写文字（`ilspy CatalogItemContainer.cs:272-281`）。见 §6·2。

### 1·7 价格钮 4 档（`PriceDisplayButton`）

```csharp
// D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/PriceDisplayButton.cs
public bool Interactable { set { button.interactable = value;
                                 if (blinkEffect) blinkEffect.enabled = value; } }   // :42-49  ← 不可点时「停闪」
// Setup(IShopOffer)                                                                  // :52-145
if (已拥有数 > 0)        { Interactable = true; priceDisplay.Setup(Format(Loc("MenuShop/ShopContainer/OpenButton"), n)); return; }  // ⇒ "Open ({n})"
if (availability > 0 && 已购 >= availability)  flag = false;                          // 限购售罄
if (!price.HasEnoughCurrency())                flag = false;                          // 货币不足
Interactable = flag;  priceDisplay.Setup(price 或 IAP 的 Price ?? "--");
```

- 🔴 **`blinkEffect` 在本 build 恒 null**：`bundle_menus_assets_all` 里 **69/69** 个 `PriceDisplayButton` 的 `blinkEffect` PPtr = 0 ⇒ 「不可点 ⇒ 停闪」这半句**不会生效**（我们没做闪烁件 = 不算缺口）。
- 已拥有那条 `obj` 的**字段名读不到**（`.c` 里没有这个重载的产物，只有 ilspy 版；`ShopItemContainer` 的 +0x20 槽位未解）。判读 = `InventoryManager.GetOwnedCount(Item)`（与 `CatalogItemContainer.Refresh` 里算 `x{owned}` 是同一个量）。

### 1·8 格子层面的「已拥有」档（`CatalogItemContainer.Refresh`）

```csharp
// D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CatalogItemContainer.cs:240-283
interactable = (未售罄 && HasEnoughCurrency() && !已拥有) ? IsPurchasable() : false;
backgroundButton.interactable = interactable;   priceDisplayButton.Interactable = interactable;
if (!已拥有) priceDisplayButton.Setup(Item);                       // 正常价
else         priceDisplayButton.priceDisplay.Setup(Loc("MenuShop/Owned"));   // ⇒ 价格区印 "Owned"
availableCount.text = $"{Loc("MenuShop/Available")}:{max(0, avail−已购)}/{avail}";
ownedCount.Set($"x{ owned }");                                    // 卡/外观两型：GetOwnedCount / HasItem?1:0
drawer.Refresh();
```

（同一段 `.c` 版本：`D:/2/tools/decomp_full/CatalogItemContainer__Refresh.c:40-160`，可逐句复核。）

### 1·9 VIP 两态（`VIPCardShopTab.CheckVIPState`）

```csharp
bool can = Store.CanPurchase;                       // DailyStore.get_CanPurchase = requiredItem == null || 背包里有它（**本地**，不是服务端）
playerIsVIPObject   .SetActive( can);               // = daily shop header/VIP Status
playerIsNotVIPObject.SetActive(!can);               // = Packs Scroll View/No VIP（整块遮罩）
if (can) vipTimer.Initialize(InventoryManager.GetExpirationDate(Store.RequiredItem));
```

- 时机：`OnOpen` / `ToFocus` / **`vipTimer.OnRunOutOfTime`**（VIP 到期那一刻立刻切到「非 VIP」）。
- `DebugGrantVIPButton`（= 页头那颗 `Reset`，文案 `Grant VIP (1 day)`）只在 `!EnvironmentHelper.IsPlayerFacingEnv && SettingsDebugButtons.DebugUIEnabled && Store.RequiredItem != null` 时 `SetActive(true)` ⇒ **正式版恒隐藏**。

---

## §2 原版序列化值（未乘任何系数的那一档）

### 2·1 窗根 `Shop Menu Variant`

| 件 / 字段 | 值 | 出处 |
|---|---|---|
| 根 RectTransform | `0,0 → 1920,1080`（整屏） | `GameObject/Shop Menu Variant.json` → RT `-2839345888552073092` |
| `ShopWindow` | `type 0`(Fullscreen) · `windowsPlacement 10`(World) · `closeOnESC 1` · `updateNavPanel 1` · **`extraScaleSmallScreen 1.0`** · **`tabs []`** · `openSound/closeSound = null` · `useDefaultCloseSoundIfNull 0` | `MonoBehaviour_6184803956894681212.json` |
| `TabbedWindowComponents` | `tabButtons 764348890710493308` · `tabPrefab 4778154276333570172`(= `Shop Tab` 母版) · `tabHolder 5477108019884345468` | `MonoBehaviour_-1210933846193584004.json` |
| `TabButtons` | `tabButtonPrefab -5188521163706513284` · **`options []`（运行期填）** · `group`(ToggleGroup) | `MonoBehaviour_764348890710493308.json` |
| `Content Area` | `167.17,70.94 → 1920.00,1080.00`（1752.83 × 1009.06） | menu_dump |
| `Tab Buttons` 的 VLG | `spacing −20` · `align 1` · `pad (0,0,20,0)` · `ctrlW 1 / ctrlH 0` · `expandW 1 / expandH 0` | 同上 |
| 首键 `Shop Icon` | `167.2,90.9 → 332.2,270.9`（**165 × 180**）⇒ 首键顶边 = 条顶 + **20**、步长 **160**（相邻重叠 20） | 同上 |
| `Shop Icon` 的 `EverguildToggle` | `isOn 0` · `onColor (1,1,1,1)` · **`offColor (0.75,0.75,0.75,1)`** | 同上 |
| 键内 `Highlight` | `Sliced (1,0,0,1)` · `ppuMul 0.92`（⇒ 九宫 30 ÷ 0.92 = 32.61） | 同上 |
| 键内 `Label` | `TMP fs36 auto[5,36]` · 色 `(0.957,0.882,0.675)` · `charSpacing −3.5` · rect `172.2,215.2 → 327.2,253.1` | 同上 |
| 键内 `Badge Highlight` | `283.9,115.5 → 318.9,150.5`（35×35）· `UiBadgeNotification`（`counterText` + `notificationDefinitions`）· 子 `OneText` `fs34 base36 auto[18,34] Center/Middle` | 同上 |
| `Shadow` / `Shadow (1)` | 47.64 宽（`Simple (0,0,0,0.475)`）/ 49.43 宽 **出厂 INACT** | 同上 |

> `ShopWindow.CreateStoreTab` 里还挂着两件事：① 徽记 `UiBadgeNotification.AddNotificationType(tab, store.ShopBadge)` + `store.extraNotificationsToWatch`；② **`if (store 上 offset 0x22 的 bool == 0) TabButtons.ToggleInteractable(tab, false)`**（`ShopWindow__CreateStoreTab.c:95-98`）。②的效果 = 把该 `TabButton` 的 `Selectable.interactable` 置 false（`TabButtons.cs:468-490`）⇒ **灰 + 点不动**。⚠️ 那个 bool 的**字段名读不到**（见 §6·4）。

### 2·2 五个「页」prefab —— 逐份参数（8 个 `ShopTab` 实例全覆盖）

| prefab | 根脚本 | 布局类 | 小屏乘数 | `itemOrder` | `timer` | 滚动 |
|---|---|---|---|---|---|---|
| `Card Shop Tab` | `CardShopTab` | **Grid** 335.6×475 | **1.18** | **10** | ✔ 文字 `Refreshes in:` | 纵向 `h=0 v=1` |
| `Item Shop Tab` | `ItemShopTab` | Grid | 1.18 | **10** | ✔ **时钟图标** `WF_icon_clock` | 纵向 |
| `Item Shop Tab No Automatic Ordering` | `ItemShopTab` | Grid | 1.18 | **0** | ✔ | 纵向 |
| `Card Shop VIP Tab Variant` | `VIPCardShopTab` | Grid | 1.18 | **10** | ✔ + **`vipTimer`** | 纵向 |
| `Shop Tab`（母版，窗内 `Tabs/Shop Tab`） | `ShopTab` | Grid | 1.18 | **0** | ✘（`timer` PPtr = 0） | 纵向 |
| `Daily Shop Tab` | **（无脚本）** | Grid | **?**（无脚本 ⇒ 乘数根本不跑） | **?** | **?** | 纵向 |
| `Packs Tab` | **`BoosterShopTab`** | **Flexible** 372.9×427.9 · `rows 2` `columns 200` · `spacing (28,32)` · `pad (50,25,25,0)` | **1.05** | **0** | ✘ | **横向 `h=1 v=0`** |
| `Generic Shop Tab` | `ShopTab` | Flexible（同上） | 1.05 | 0 | ✘ | **横向** |

- 出处：`MonoBehaviour_{5704525872763429038, -510282498869753846, 3317690853460206890, -1432077903862721639, -2782358438697285508, 1775925680991623303, -4402154304938917641}` 的 `contentLayoutElementsSizeMultiplierSmallScreens / itemOrder / gridLayoutContent / flexibleGridLayoutContent / timer` 字段；根脚本名 = `menu_dump.py --depth 0` 的「组件（类名）」列。
- **Grid 族 5 份的 `GridLayoutGroup` 逐字段相同**：`padding (0,0,7,0)` · `childAlignment 0` · `startCorner 0` · `startAxis 0` · **`cellSize 335.6×475`** · `spacing (0,0)` · **`m_Constraint 0`** · `m_ConstraintCount 2`（死值）。
- **Flexible 族 3 份逐字段相同**（`2574735992316745863 / -6901504666030194441 / 7842029754233656060`）。
- ⚠️ **原版没有 `Cosmetics Shop Tab` / `Gold Shop Tab` / `Booster Shop Tab` 这三个 prefab 文件**（类在、prefab 不在 `menus` 包；全库 `GameObject/*shop*` 只有上表这 8 个页）。⇒ 那两页的版面 **查不到**。

### 2·3 页内固定件（Grid 族）

| 件 | 值 | 出处 |
|---|---|---|
| `Packs Scroll View` | 相对 `162.6,56.7 → 1752.8,1009.1`（绝对 `329.77,127.64 → 1920,1080`）= **1590.24 × 952.38** | `menu_dump --depth 3 --relative` |
| `ScrollRect` | `h 0 / v 1` · **`movementType 1`（Elastic）** · `inertia 1` · `elasticity 0.1` · `decelerationRate 0.135` | 同上 |
| `Viewport` | 同父；`Image(UIMask, Sliced (1,1,1,0))` + **`RectMask2D` `m_Softness (0,25)`** `m_Padding 0` | 同上 |
| `Content` | anchors `(0,0)-(1,1)` · `sizeDelta (0.000122, 0)` · `pivot (0,1)` ⇒ **拉伸**；挂 `GridLayoutGroup` + `ContentSizeFitter(H=0, V=PreferredSize)` | `RectTransform_-4349573501082541906.json` · `MonoBehaviour_-5152081396385324882.json` |
| `daily shop header` | 高 **85**；`Line` `y 80..90` **出厂 INACT**；`TimeCounter` `200.3,0 → 511.7,80`（**311.4 × 80**，HLG `spacing 5 align 3 pad 0 ctrlW/H=1 expandW=0 expandH=1`） | menu_dump |
| `RefreshText` | `fs30 auto[10,30]` · `charSpacing −3.5` · 白 · 文本 = **`Refreshes in:`**（Card / Daily / Item 三份；见 §6·5 的葡语那一格） | 同上 |
| `Time` | `fs30 auto[15,30]` · `LayoutElement.minWidth 111.31` · `charSpacing −3.5` | 同上 |
| `Clock Icon`（仅 Item 页） | `preferredWidth 26` | 同上 |
| `Empty Collection Warning` | `−2.4,−28.3 → 1802.8,1009.1`（1805.24 × 1037.38）**出厂 INACT**；子 `Warning` `TMP fs36` 色 `(0,0,0,1)`，原文 = 收藏窗那条 `MenuCollection/NoDecksFound` | 同上 |
| `TimerDisplay`（4 份） | `useExtraText 0` · `useLastMinutesText 1` · `lastMinutesText "MainMenu/LastMinutes"`；**`timerTextDescription` = null** | `MonoBehaviour_{4867360967595586457,-5363483528564307958,1580637533877626154}` |

### 2·4 页内固定件（Flexible 族）

| 件 | 值 |
|---|---|
| `Packs Scroll View` | `163.5,0 → 1752.8,1009.1` = **1589.31 × 1009.06** · `ScrollRect h 1 / v 0` · 同样 `mode 1 / inertia 1 / 0.1 / 0.135` |
| `Scrollbar Horizontal` | `337.2,988.5 → 1417.3,993.9` · **出厂 INACT**（+ `Sliding Area`/`Handle`/四支箭头全 INACT） |
| `Viewport` | `RectMask2D` **`m_Softness (0,0)`**（与 Grid 族的 (0,25) **不同**） |
| `Content` | `FlexibleGridLayout`（`rows 2` `columns 200` `itemSize 372.9×427.9` `spacing (28,32)` `pad (L50 R25 T25 B0)`） |
| `Header` | `0,0 → 1752.83,85` **出厂 INACT**（挂 `FlexibleLayoutSizeOption`） |

### 2·5 商品格（三种世代）

| 世代 | prefab | 关键件 | 备注 |
|---|---|---|---|
| **抽屉商品卡**（商店三页在用） | `Catalog Item Shop Container`（6 份同构）· `Cosmetic Item Shop Container` · `Comsetic Item Shop Container`（**原版拼写错**，是另一个 prefab） | `background(UI_Deck_Selection_Back_simple + Mask)` · `Available Counter` · `Counter`(CollectionCardCounter) · `DrawerHolder` · `PriceDisplayButton` · `WebShop Button Square Variant` | 格内几何见 `资料/阶段二_商店_原版规格.md` §三（逐件表） |
| **平铺卡** | `Shop Item Container` · `Offer Container` · `Daily Bonus Shop Container` · `Booster Offer Container` · `Webshop Offer Container` · `InApp Shop Container` / **`… Disabled`** | `background`+`foreground`+`name-bg`→`name`/`type`+Price+WebShop+`Badge`+`Timer`+`Available Counter` | 🔴 `InApp Shop Container` 与 `… Disabled` 是**一对**（内购商品的可购/不可购两版，**结构差未核**） |
| **抽屉报价** | `General Basic Offer Container *`（19 文件 / 17~19 种结构） | 骨架同上，只差 `Dynamic Content` 挂的 11 种抽屉 | 正本 §五·一 |

---

## §3 状态 × 参数矩阵（**主表**）

> 每格后面写出处；**没查到的写 `?`**，⛔ 不拿默认值顶替。数值都在画布参考单位（CanvasScaler ref 1920×1080）。

### 3·1 会改「列数 / 格尺寸 / 行数 / 滚动方向」的状态

| 状态 | 判定条件 | 列数 | 格宽×格高 | 行 / 内容高 | 滚动 | 出处 |
|---|---|---|---|---|---|---|
| **S0 桌面（默认）** | `GameStaticData.smallScreenUI == false` | **4** | **335.6 × 475** | `ceil(n/4)` 行 · `行×475 + 7` | 纵向 Elastic，`elasticity 0.1` `decel 0.135` | §1·1 · §1·2 · §2·3 |
| **S1 小屏（Grid 族）** | `smallScreenUI == true` | **4**（不变） | **396.008 × 560.5**（×1.18） | `ceil(n/4)` 行 · `行×560.5 + 7`（⚠️ padding **不乘**） | 纵向 | `ShopTab.cs:210-211` · 算术 |
| **S1′ 小屏（Flexible 族）** | 同上 | 列数由件数定（`ceil(n/2)`） | **391.545 × 449.295**（×1.05） | **恒 2 行** | **横向** | `ShopTab.cs:233-234` · §2·2 |
| **S2 非 16:9 / 其它分辨率** | `CanvasScaler`：`ScaleWithScreenSize` ref 1920×1080 · `ScreenMatchMode 1`(**Expand**) · `MatchWidthOrHeight 0` | `floor((画布宽 − 329.77) ÷ 335.6)`（= Content 宽 ÷ 335.6）⇒ **画布宽只会 ≥1920 ⇒ 列数只增不减**：16:9→**4** · 16:10→4 · 4:3→4 · **21:9(2560×1080，画布 2560)→`floor(2230.2/335.6)`=6** · **32:9(3840×1080，画布 3840)→`floor(3510.2/335.6)`=10** · 2560×1440(16:9)→画布仍 1920→4 | 335.6×475（不变） | 按式现算 | 纵向 | **算术推算，⚠️ 无实拍**（判据 = CanvasScaler 语义 + `MonoBehaviour_2577.json`，与卡组编辑窗同一份） |
| **S3 页 = Card / Item / VIP / 母版** | 页签点击 | 4 | 335.6×475 | 同上 | 纵向 | §2·2 |
| **S4 页 = Packs(Booster) / Generic** | 页签点击 | 件数 ÷ 2 | 372.9×427.9 | 恒 2 行（≤ 1009.06 高） | **横向** | §2·2 · §2·4 |
| **S5 页 = Daily** | 页签点击 | **?** | **?** | **?** | 纵向（结构同 Grid 族） | **根无脚本** ⇒ 乘数/排序/倒计时**都不跑**；商品一个都不会建（见 §6·1） |

### 3·2 商品格 / 价格钮的状态（**这一块是本窗的主战面**）

| 状态 | 判定条件 | 价格区 | 按钮可点 | 其它格内件 | 出处 |
|---|---|---|---|---|---|
| **S6 正常** | `未售罄 && 有钱 && 未拥有` | 价格（金币图标 + 数字） | ✔ | `x{owned}` 角标 · `Available` 文本（若 `avail > 0`） | `CatalogItemContainer.cs:240-283` |
| **S7 已拥有 → 开箱** | `已拥有数 > 0` | **`Open ({n})`**（键 `MenuShop/ShopContainer/OpenButton`，EN `Open ({0})`） | **✔ 强制 true** | — | `PriceDisplayButton.cs:91-101` |
| **S8 已拥有 → 文字** | 格子层面判「这件已拥有」（可拥有型：卡 / 外观） | **`Owned`**（键 `MenuShop/Owned`） | ✘（`interactable` 里已 AND 掉 `!已拥有`） | — | `CatalogItemContainer.cs:265-275` |
| **S9 限购售罄** | `availability > 0 && 已购 >= availability` | 价格照常印 | ✘（且换 `Everguild/UI/Greyscale` 材质 ⇒ **整钮+金币图标变灰**） | `Available: 0/{avail}` | `CatalogItemContainer__Refresh.c:45-51,65-77` |
| **S10 货币不足** | `!price.HasEnoughCurrency()` | 价格照常印 | ✘（同上变灰） | — | 同上 + `PriceDisplayButton.cs:152-164` |
| **S11 真钱且无 IAP 报价** | `price.Currency == realMoney && GetIapData() == null` | **`--`** | ✘ | — | `PriceDisplayButton.cs:113-143` |
| **S12 免费** | 价格为 0 / `SetupFree()` | **`Free`**（键 `MainMenu/Free`） | ✔ | — | `PriceDisplay.cs:198,250-258` |
| **S13 删除线** | `useStrikeThrough == 1` | 文本**前插 `<s>`**（原版**只插开头、不闭合**） | 不变 | — | `PriceDisplay.cs:216-221` · 全库 **644 份里只有 1 份 = 1** |
| **S14 限购有剩余** | `0 < 已购 < availability` | 价格 | ✔ | `Available: {max(0,avail−已购)}/{avail}` | §1·6 |
| **S15 购买成功** | `TryPurchase` 成功 | — | — | 回调 `ShopTab.RefreshAfterExpire` ⇒ **整页 `DestroyAllChildren` + 重建** | `ShopTab.cs:491-496` |
| **S16 传奇重复购买** | `Rarity 4 && 已拥有 1` | — | 先弹 **`MenuShop/ExtraLegendaryWarning`** 确认框（左 Cancel / 右 OK） | — | `CatalogItemContainer__TryPurchase.c` |
| **S17 商品流滚动** | 内容高 > 视口 | — | — | 纵向：内容 `行×475+7`；横向：内容 `(372.9+28)×列 −28 + 75` | §1·2 · §1·4 |

### 3·3 时间 / VIP / 页签的状态

| 状态 | 判定条件 | 表现 | 出处 |
|---|---|---|---|
| **S18 倒计时·隐藏** | `CheckTimer` 返回 false | 整个 `TimeCounter` `SetActive(false)` | `ShopTab.cs:170-183` |
| **S19 倒计时·正常** | 有目标时间且剩余 > 5 分钟 | `prefix + GetTimeString(ts, eventTimer:true)`（prefix = `""`） | §1·5 |
| **S20 倒计时·最后 5 分钟** | `0 < ts < 5min` | 文字**整句换成 `MainMenu/LastMinutes`**（`Last Minutes`），**不拼数字** | §1·5 |
| **S21 倒计时·已过期** | `ts <= 0` | **整块 `SetActive(false)`** + `OnRunOutOfTime` **只发一次** | §1·5 |
| **S22 哪几页有倒计时** | 见下 | `CardShopTab`/`CosmeticsShopTab`：`< 9 点取当天、否则次日` 的 **09:00**，且要求 `(target−now).TotalMinutes > 0`；`ItemShopTab`：**只有 `LiveOpsUtility.IsTimedEvent(store)` 为真**才显；基类 `ShopTab`：**恒 false**；`VIPCardShopTab : CardShopTab` **不 override** ⇒ 同 9 点规则 | `CardShopTab__CheckTimer.c:30-60` · `ItemShopTab__CheckTimer.c:20-40` · `ShopTab.cs:277-283` |
| **S23 VIP 已开通** | `Store.CanPurchase == true` | `VIP Status` 显示（内含 `Yes VIP` 文案 `You are VIP. Time Remaining:` + **`vipTimer` 显示 VIP 到期倒计时**）；`No VIP` 遮罩隐藏 | §1·9 · `VIPCardShopTab.cs:260-277` |
| **S24 未开通 VIP** | `CanPurchase == false` | `VIP Status` 整块隐藏；**`No VIP` 遮罩整块显示**（`Block Input` `(0,0,0,0.22)` + `Shadow drop` + 大字 `fs51.11` `You have to become VIP to access this store`）；页头 `Reset` 按钮仍在（但正式版隐藏，见 S25） | 同上 + `menu_dump "Card Shop VIP Tab Variant"` |
| **S25 调试钮** | `!IsPlayerFacingEnv && DebugUIEnabled && RequiredItem != null` | `Reset`（`Grant VIP (1 day)`）`SetActive(true)`；**正式版恒 false** | `VIPCardShopTab.cs:213-239` |
| **S26 页签禁用** | `store` 上 offset `0x22` 的 bool == 0 | 该 `TabButton.Selectable.interactable = false`（灰 + 点不动） | `ShopWindow__CreateStoreTab.c:95-98` · `TabButtons.cs:468-490` |
| **S27 页签选中 / 未选中** | Toggle 状态 | `onColor (1,1,1,1)` / **`offColor (0.75,0.75,0.75,1)`**；底图 `Highlight` 由选中态换 | §2·1 |
| **S28 页签徽记** | `store.ShopBadge` 非空 或 `extraNotificationsToWatch` 非空 | `Badge Highlight` 的 `UiBadgeNotification`（`counterText` + 计数） | `out_cpp2il_dev_ilspy/ShopWindow.cs:708-764`（`GetComponentInChildren<UiBadgeNotification>` → `AddNotificationType(store.ShopBadge)` → `AddNotificationType(+0x68 extraNotificationsToWatch)`）；`.c` 版 = `ShopWindow__CreateStoreTab.c:31-63` |
| **S29 空列表** | 商品数 == 0 | `Empty Collection Warning` **恒 INACT**（链上无写入点） | §0-8 · §6·3 |
| **S30 页切换页** | 页签点击 | `OnOpen`/`ToFocus` → `RefreshOffers()`（`containers.RemoveAll(c==null)` + 逐个 `Refresh`）；VIP 页额外 `CheckVIPState()` | `ShopTab.cs:265-275` · `VIPCardShopTab.cs:253-258` |

### 3·4 反查：**谁在运行时改这些参数**（这一格是本次查证的核心）

| 探针 | 结果 | 出处 |
|---|---|---|
| 写 `GridLayoutGroup.cellSize` / `FlexibleGridLayout.ItemSize` 的地方 | **只有 1 处** = `ShopTab.Setup`，且**只在 `smallScreenUI` 时** | `grep -rn "cellSize\s*=" out_cpp2il_dev_ilspy --include=*.cs` |
| 写 `Content.sizeDelta` / 改 `Content` 宽的地方 | **0 处**（`ContentSizeFitter` 只控高；`ScrollRect` 只移 `anchoredPosition`） | 逐方法读 |
| 改 `Packs Scroll View` / `Viewport` 尺寸的地方 | **0 处** | 同上 |
| 改 `PriceDisplayButton.Interactable` 的地方 | 3 处：`CatalogItemContainer.Refresh` · `PriceDisplayButton.Setup(CurrencyValue,bool)` · `ShopOfferContainer.OnOfferTimerEnded` | 逐方法读 |
| 改 `Empty Collection Warning` 显隐的地方 | **0 处** | `ShopTab` 全方法 + `CatalogItemContainer` 全方法 |
| 改 `TimerDisplay` 显隐的地方 | 2 处：`ShopTab.Setup`（`CheckTimer` 假 ⇒ 关）· `TimerDisplay.UpdateTimer`（过期 ⇒ 关） | §1·5 |

---

## §4 我们代码里对应哪一行 · 现在是哪一档 · 缺哪几档

### 4·1 已对上的（不用动）

| 状态 | 我方落点 | 判 |
|---|---|---|
| 窗根四件（type/placement/closeOnESC/extraScaleSmallScreen + 关窗不出声） | `Shell/ShopWindow.cs:138-144` | ✅ 对（逐字段同 §2·1） |
| 左栏 20/160 步长、−20 spacing | `Shell/ShopWindow.cs:100-113` | ✅ 对 |
| 格内几何（CellBg / CellAvail / CellPrice / CellCount / CellDrawer） | `Shell/ShopTabPage.cs:140-150` | ✅ 对（照 `menu_rect --root-size 335.6x475`） |
| 纵向滚动 + Elastic + `RectMask2D` 软边 (0,25) | `Shell/ShopTabPage.cs:106,410-420` | ✅ 对 |
| 倒计时 4 档（含**最后 5 分钟**） | `Shell/TimerDisplay.cs:58-60,198-205,357,386` + `ShopTabPage.cs:355-357` | ✅ 对（2026-10-28 `B92_N8` 已按 `.c` 订正） |
| VIP 两态 + 到期即切 + 调试钮闸 | `Shell/ShopTabPage.cs:927-1010`（类尾那一段） | ✅ 对 |
| 免费 / `--` / 删除线三档 | `Shell/ShopTabPage.cs:679-684` | ✅ 对 |
| 售罄 ⇒ 灰（含金币图标一起灰） | `Shell/ShopTabPage.cs:700` | ✅ 对（且已标 `🔶 自己设计`：软禁用） |
| 传奇重复确认（左 Cancel / 右 OK） | `Shell/ShopTabPage.cs` `ShowLegendaryConfirm` | ✅ 对 |
| `Available: {n}/{m}` 文案与颜色 | `Shell/ShopTabPage.cs:596-620` | ✅ 文案对（**显隐闸是自设计**，见 §4·2 #9） |
| `x{owned}` 角标 | `Shell/ShopTabPage.cs:640-645` | ✅ 对 |

### 4·2 **缺的档（一句一档）**

| # | 缺什么 | 原版值 | 我方现状 | 落点 |
|---|---|---|---|---|
| **1** | 🔴 **列数 2 ≠ 4** | 4 列（§1·1） | `GridCols = 2`，且 Content 宽被写成 `GridCols × CellW = 671.2` | `Shell/ShopTabPage.cs:113`（常量）· `:406`（内容宽）· `:443`（摆位） |
| **2** | 🔴 **小屏档（Grid 族 ×1.18）** | 396.008 × 560.5，仍 4 列 | 注释明写「我们的版面固定 16:9 ⇒ 乘 1.0」；**全文 0 处 `SmallScreenUI`** | `Shell/ShopTabPage.cs:110-112` |
| **3** | 🔴 **小屏档（Flexible 族 ×1.05）** | 391.545 × 449.295 | 同上（且第 5 页根本没走 Flexible 族） | 同上 |
| **4** | 🔴 **`Open ({n})`（已拥有 ⇒ 开箱）** | `MenuShop/ShopContainer/OpenButton` = `Open ({0})`，强制可点 | **0 处**（价格区只有 Free / `--` / 数字三档） | `Shell/ShopTabPage.cs:679-684` |
| **5** | 🔴 **`Owned`（已拥有 ⇒ 文字）** | `MenuShop/Owned` = `Owned` | **0 处** | 同上 |
| **6** | 🔴 **「买不起 ⇒ 按钮不可点」** | `HasEnoughCurrency()` 参与 `interactable` | 只有 `SoldOut` 一个闸，**没有余额闸** | `Shell/ShopTabPage.cs:700` · `ShopData` 无 `CanAfford` |
| **7** | 🟡 **Flexible 族整条路（横向 + 2 行货架）** | `Packs Tab`/`Generic Shop Tab`：横向、2 行、`itemSize 372.9×427.9`/`spacing 28×32`/`pad 50,25,25,0` | 第 5 页（Crates）**复用 Cards 的竖向格 + 同一份商品数组** | `Shell/ShopData.cs:247-255`（`case 4: return _cards`）· `ShopTabPage.cs:399-451`（恒纵向） |
| **8** | 🟡 **页签禁用态** | `Selectable.interactable = false`（灰 + 点不动） | **0 处** | `Shell/ShopWindow.cs`（全文） |
| **9** | 🟡 **`Available` 文本的显隐闸** | `CatalogItemContainer` **没有** `SetActive` ⇒ 由 prefab 出厂决定；`ShopOfferContainer` 有（`hasLimit`） | 我们按 `AvailableMax > 0` 显示 —— **判据来源不是原版代码** | `Shell/ShopTabPage.cs:603`（🔶 应标「自己设计」） |
| **10** | ⚪ **页签徽记机制** | `UiBadgeNotification.AddNotificationType(store.ShopBadge 等)` | 我们自绘 `BadgeWiring`（注释自称「诊断名」） | `Shell/ShopWindow.cs:217,245` |

> ⚠️ **#1 与 #2 是同一处代码的两面**：把 `GridCols` 改成「按 Content 宽 ÷ 格宽现算」之后，小屏档才会有意义（4 列两档都成立）。建议同一个写手一次改掉。

---

## §5 按文件分组的施工建议（调度台按文件切给写手）

| # | 文件 | 要做什么 | 判据（本文件哪一节） | 风险 |
|---|---|---|---|---|
| **A** | `CardPresentation/Shell/ShopTabPage.cs` | 把 `GridCols`（`:113`）改成**运行时现算**：`Cols = max(1, floor((ScrollView.W − padH) / (CellW × k + Spacing)))`，`k = SmallScreenUI.Enabled ? 1.18 : 1`；`contentW`（`:406`）改成 **= ScrollView.W**（Content 拉伸）；`:443` 摆位跟着走 | §1·1 · §1·2 · §1·3 · §4·2 #1#2#3 | 中：`GridCols` 被 `contentH`/摆位/断言多处引用；换档后要 `RebuildForScroll` |
| **B** | `CardPresentation/Shell/ShopTabPage.cs` | 价格区补 **2 档**：`已拥有 ⇒ Open ({n})`（强制可点）· `已拥有 ⇒ Owned`；补 **余额闸**（`买不起 ⇒ 不可点 + 变灰`） | §1·7 · §1·8 · §4·2 #4#5#6 | 中：要先在 `ShopData` 加一个「已拥有数 / 余额」的读口；`WindowButton.onClick` 现有「软禁用」口径要一并想清楚（原版是**硬禁用**） |
| **C** | `CardPresentation/Shell/ShopData.cs` | 补 `OwnedCountOf(page,i)`（原版 = `InventoryManager.GetOwnedCount(item)`）与 `CanAfford(...)`；⚠️ 别把 `Available` 塞进 `ItemInfo`（文件里有旧纪律） | §1·7 · §1·8 | 低 |
| **D** | `CardPresentation/Shell/ShopTabPage.cs` | 第 5 页（Crates）改成 **Flexible 族**：横向 `MenuScroll`（h=1/v=0）+ 2 行 + `itemSize 372.9×427.9` / `spacing 28×32` / `pad (50,25,25,0)` / `Viewport` 软边 **(0,0)** | §1·4 · §2·4 · §4·2 #7 | **高**：会动 `MenuScroll` 的方向支持；第 5 页的商品表是自编的，**换布局后格数会变** |
| **E** | `CardPresentation/Shell/ShopWindow.cs` | 页签禁用态：给 `TabButton` 加一个 `SetInteractable(bool)`（灰 + 点不动），并在建键时按「store 不可用」置 false | §3·3 S26 | 低（但原版那个 bool 的**判据读不到**，见 §6·4 ⇒ 要么先查、要么标 `🔶`） |
| **F** | 断言（`Editor/ShopScene.cs`） | 补「两档各是各的」硬断言：桌面 `Cols==4 && CellW==335.6 && 内容宽==1590.24`；小屏 `Cols==4 && CellW==396.008`。⚠️ 期望值**不许从 `GridCols` 读回来**（自证） | §1·1–1·3 | 低 |
| **G** | `资料/阶段二_商店_原版规格.md` | 三处订正：① §二·一「`constraint 0 (Flexible)/count 2`」要补一句「**count 在 Flexible 下是死值，真实列数 4**」；② §二·二 / §六#12 的「Daily 页 RefreshText 是葡语 `Atualiza em:`」→ **葡语在母版 `Shop Tab`，Daily 页是 `Refreshes in:`**（§6·5）；③ §六#7b 那段「我们把 Available 抬进格子」要补「原版那件的显隐**没有代码闸**」 | §6·5 · §4·2 #9 | 无（文档） |

---

## §5·5 **不做 / 为什么**

> 每条必须带判据 + 反证。

### N-1 ⛔ **`blinkEffect`（价格钮不可点 ⇒ 停闪）不补**
**判据**：`bundle_menus_assets_all` 里 **69/69** 个 `PriceDisplayButton` 的 `blinkEffect` PPtr = 0（全库扫描）。原版那行 `if (blinkEffect) blinkEffect.enabled = value;`（`PriceDisplayButton.cs:45-48`）在本 build 恒不执行 ⇒ 补一个原版不存在的闪烁件 = **造行为**。

### N-2 ⛔ **`Empty Collection Warning` 不做显隐联动**
**判据**：§3·3 S29 —— 商店链上 0 个写入点；prefab 出厂 INACT。我们恒隐藏与原版同形，补联调 = 给原版加行为。

### N-3 ⛔ **`Daily Shop Tab` 页不按「有驱动的页」去造一套逻辑**
**判据**：根上**没有脚本**（`GameObject/Daily Shop Tab.json` 只有 2 个组件，其中 `-3022006593115493330` 在包里**没有导出的对象**）；全库无 `DailyShopTab` 类。原版这个 build 里 Daily 页**不建商品**。⇒ 我们自建的 Daily 页是 `🔶 自己设计`（数据自编，已在规格 §六 记）。**要不要把它做成「像原版那样空着」= 调度台裁断**（我建议保持现状并在注释里写明，因为「空页」对玩家=不可用）。

### N-4 ⛔ **`CardShopFilter` 不做成 UI 筛选面板**
**判据**：它是 `ScriptableObject` 黑名单，**唯一引用方** = `DailyStoreData.blackListedFilter`；没有任何 `CollectionFilterController`/按钮引用它。把它做成商店筛选 UI = 发明不存在的功能。

---

## §6 **未查清**（`?` —— 本文件的全部不确定项）

### 6·1 `Daily Shop Tab` 的驱动类 / 参数
- 根脚本**不存在**；该 prefab 的 `GridLayoutGroup` 仍在（我从 `menu_dump` 读到 cellSize 335.6×475 / pad T7，与 Grid 族同值），但**小屏乘数 / itemOrder / timer 三项无处可取**（这三个是脚本字段，没有脚本就没有值）。
- 还差什么：一条「原版 Daily 页实拍」或服务端 store 数据（本地只有一个 `Premium Campaign Store`，与本页无关 ⇒ 规格 §九 已确认）。

### 6·2 `Available Counter` 的显隐到底谁管（`CatalogItemContainer` 侧）
- 事实：`CatalogItemContainer.Refresh` **只写 `text`、没有 `SetActive`**（`ilspy :272-281` + `.c :120-150`）；而 prefab 里这个件的出厂 `m_IsActive` **按实例不同**（全库 48 份 `Available Counter`：**46 份 active / 2 份 INACT**；我读到的那一份 `Catalog Item Shop Container` 里是 **INACT**）。
- ⇒ **「什么时候显示」在本地没有代码判据**。我们按 `AvailableMax > 0` 显示 = 近似。
- 还差什么：确认服务端给的那批 cell prefab 到底是「带 Available 的版本」还是「不带」。文档化后应标 `🔶 自己设计`（现无标记）。

### 6·3 `Empty Collection Warning` 是否真的恒不显示
- 反编译里 0 个写入点 + 出厂 INACT ⇒ 结论成立；但 `UiContainer` / `WindowsManager` 一层我没全读。
- 还差什么：若哪天真出现空商店，需一条实拍。**当前判：恒不显示**。

### 6·4 `ShopWindow.CreateStoreTab` 里那个 `store + 0x22` 的 bool 叫什么
- `.c` 只给偏移（`ShopWindow__CreateStoreTab.c:95`）；`IStore` / `ShopStore` 的字段表里没有能唯一定位到 `0x22` 的 bool（`CanPurchase` 是虚属性、`ShopStore.cs:63`）。
- 还差什么：一条 VA 级反汇编（`IStore` 的 getter 表）或实拍「哪个页签会变灰」。**在此之前 S26 的触发条件写 `?`**。

### 6·5 「Daily 页 RefreshText 是葡语占位」—— **原文档记错了**
- 实测：`bundle_menus_assets_all` 里含 `Atualiza em:` 的 TMP **全库只有 1 份**（`MonoBehaviour_6081209623130884220.json`），它属于 **母版 `Shop Tab`**（`Tabs/Shop Tab/daily shop header/TimeCounter/RefreshText`）。
- `Daily Shop Tab` 那一份是 **`Refreshes in:`**（`menu_dump "Daily Shop Tab" --depth 3` 现读）。
- ⇒ `资料/阶段二_商店_原版规格.md` §二·二 与 §六 #12 那一句 **要订正**（见 §5 表 G）。
- 顺带：**三页的差**实际是「Card/Daily = 文字 · Item = 时钟图标 · 母版 = 葡语占位」——不是原文档写的「Daily 是葡语」。

### 6·6 非 16:9 的实拍
- §3·1 S2 是**按 `CanvasScaler` 语义推的算术**，**没有实拍**（`runtime_ui_dump_*.tsv` 只覆盖战斗/Intro；用户 16 张参考图里**没有商店**）。

### 6·7 `Cosmetics Shop Tab` / `Gold Shop Tab` 两页的版面
- 类存在（`CosmeticsShopTab` / `GoldShopTab : ShopTab`），**prefab 不在本地**（`GameObject/*shop*` 8 个页里没有）。⇒ 这两页的格尺寸/列数 **查不到**。

---

## 附录 A · 顺手发现（不属本窗结论，但值得记账）

1. 🔴 **`AUD05_state_matrix_卡组编辑.md` §3·1 的「`smallScreenUI` 是唯一改列数的开关」在商店窗同样成立，但乘数不同（1.18 / 1.05）** —— 别把卡组编辑窗的 `_mobileSizeScale 1.5` 套到商店上。
2. 🟡 **`m_ConstraintCount` 这个坑在别处也可能有**：凡是 `GridLayoutGroup.constraint == 0/1/2` 里读 `2`（`Flexible`）的 prefab，**`m_ConstraintCount` 都不能当列数**。建议全库扫一遍（本文件只覆盖商店）。
3. 🟡 **`PriceDisplay.useStrikeThrough` 全库 644 份里只有 1 份 = 1** —— 那 1 份在哪、是不是商店页，**没查**（我们的 `<s>` 实现目前无消费者，属「等数据」）。
4. 🟡 **`shopheader/Line` 与 `Packs Tab/Header`、`Daily/Packs Scroll View/No VIP` 的出厂 `m_IsActive` 不一致**：`Line`(INACT) · `Header`(INACT) · `No VIP` 遮罩(**ACTIVE**) —— VIP 页的 prefab 被存成「已开通 + 遮罩意外打开」的矛盾态；运行时 `CheckVIPState` 每开一次就修正一次，**不影响玩家**，但**别照 prefab 的 active 当判据**。
5. ⚪ `PriceDisplayButton.__ctor.c` 等 `.ctor.c` 内容错配是已知问题（规格 §四 已记），本文件未重复核。

---

## 附录 B · 覆盖边界（本文件到底查了多少）

| 维度 | 覆盖 |
|---|---|
| **读透的类** | `ShopWindow.cs`(1762，主力方法逐句) · `ShopTab.cs`(617 全) · `CardShopTab.cs`(49) · `ItemShopTab.cs`(184) · `VIPCardShopTab.cs`(278) · `CosmeticsShopTab.cs`(49) · `GoldShopTab.cs`(3) · `PriceDisplayButton.cs`(182 全) · `PriceDisplay.cs`(102) · `ShopItemContainer.cs`(69) · `CatalogItemContainer.cs`(354，Refresh 段) · `ShopOfferContainer.cs`(312 全) · `TimerDisplay.cs`(369 全) · `FlexibleGridLayout.cs`(557，只读算式段) · `TabButtons.cs`（只有 `ToggleInteractable` 段）· `CardShopFilter.cs`(17) · `WildcardDrawer.cs`(44) · `DailyStoreState/Item.cs` |
| **读透的 `.c`** | `PriceDisplayButton__Setup` · `CatalogItemContainer__Refresh` · `ShopOfferContainer__{Refresh,SetAvailableText,OnOfferTimerEnded}` · `TimerDisplay__UpdateTimer` · `CardShopTab__CheckTimer` · `ItemShopTab__CheckTimer` · `ShopTab__CreateOffers` · `ShopWindow__CreateStoreTab` · `BoosterShopTab__{OnOpen,TryOpenContainerArmy}` |
| **读的 prefab** | `Shop Menu Variant` 全树（depth 14）· 8 个页 prefab（depth 0/3/6）· 8 个 `ShopTab` MB · 5 个 `GridLayoutGroup` MB · 3 个 `FlexibleGridLayout` MB · 4 个 `TimerDisplay` MB · 窗根 2 个 MB · `TabButtons` MB · `ContentSizeFitter` MB · `Content` RT · 全库 69 个 `PriceDisplayButton` / 644 个 `PriceDisplay` / 91 个 `TimerDisplay` / 48 个 `Available Counter` 的**批量扫描** |
| **未读** | `ShopContainerV2` / `ShopContainerInventory`（卡包容器族，与格子态无关）· `ShopStore`/`DailyStore` 的 LiveOps 基类全量 · `ShopOfferData`/`ShopOfferEvent` 的数据形状（服务端侧）· `WildCardConfig`/`WildcardDisplay`（通配符抽屉的内容侧）· `ItemDrawer` 19 变体 |
| **`?` 的数量** | **4 格**：§6·1 Daily 驱动参数 · §6·2 Available 显隐闸 · §6·4 `store+0x22` 字段名 · §6·7 Cosmetics/Gold 页版面。另有 **1 格「无实拍」**（§6·6） |
| **没跑的东西** | Unity 批处理 / 真机 / `typecheck`（只读代理红线，一律没跑） |
| ⚠️ 提醒 | **「本文件把参数查清了」≠「原版这个件全复刻了」** —— 本文件只回答「哪个状态对应哪套参数」；「画出来像不像」要靠并排实拍（调度台验收）。 |
