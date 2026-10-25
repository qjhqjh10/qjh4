# V · 轮抽窗 `Card List` 行模板 `Deck Selector Card Info button` —— 执行报告

- 日期：2026-10-25 · 执行代理 **V**
- 范围：`Shell/DraftModeWindow.cs`（行模板 + 滚动偏移夹法的就地订正）· `Editor/DraftAssertD3.cs`（断言）
- 状态：**改动已落盘 · 秒级类型检查绿（运行时 0 / 编辑器 0）· ⛔ 没跑 Unity**（红线①）

---

## 一、原版逐子件表（判据 = prefab 字段 + `menu_dump` 现读）

**行根**：`Cards in deck panel / Scroll View / Viewport / Content / Deck Selector Card Info button`
**这一颗的 `RectTransform` pid = `-6068752977877108027`**（⚠️ 全库有 8 颗同名 —— 卡组编辑 / `Deck info Popup` /
练习窗 / 限时轮抽各一颗，**按父链认人**，⛔ 别拿别的顶）。

复现命令（**逐值都是它印出来的**）：

```text
python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all \
  --rt -6068752977877108027 --depth 8 --relative --md
```

父 = `Content`（本行高 45 · 行宽 = `Scroll View` 宽 **330.97**，VLG `childControlWidth=1` 拉满）。
下表「行内」= 相对**行节点左上角**（画布 px，y **向下**）；「绝对」= 第 0 行时（行顶 = 270）。

| # | 名字（prefab） | 行内矩形 | 绝对（row 0） | anchor min→max / pivot / apos / sizeDelta | sprite（名字 · 原尺寸 · 九宫 · 类型 · PathID） | 颜色 / 字体 / 显隐 | 兄弟序 |
|---|---|---|---|---|---|---|---|
| — | **Deck Selector Card Info button**（行根） | 0,0 → 330.97,45 | 1530,270 → 1860.97,315 | aMin(0,1) aMax(0,1) piv(.5,.5) apos(165.485,−22.5) size(330.97,45) | — | `EverguildButton`(`trans=1` ColorTint，`target=Background` 那颗 Image) · `UICardInfoItem` · `LayoutElement{m_MinHeight=45}` | — |
| 1 | `Content` | 0,0 → 330.97,45 | 同上 | (0,0)→(1,1) piv(.5,.5) apos(0,0) size(0,0) | — | `CanvasGroup`（本窗 `checkOwnership=0` ⇒ alpha 恒 **1**） | 0 |
| 2 | `Background` | **18.48**,0 → 330.97,45 | 1548.48,270 → 1860.97,315 | aMin(0.055841296911239624,0) aMax(1,1) piv(.5,.5) apos(0,0) size(3.81e−6,0) | `40k_deck_cardlist_bg` 318×54 · 九宫 **(150,0,150,0)** · **Sliced** · PathID `-434601559970338398` (ext2) | 白 (1,1,1,1) · **不参与稀有度染色** | 0（`Content` 的独子） |
| 2·1 | `Rarity Gradient` | **207.32**,−0.00 → 330.97,**44.89** | 1737.32,270 → 1860.97,314.89 | aMin(0.6057502627372742,0) aMax(1,1) piv(.5,.5) apos(−0.22641,**+0.05442**) size(0.4528,−0.1035) | `40k_deck_cardlist_bg_rarityColorGradient` 114×54 · **Simple** · PathID `-131217731439433243` (ext2) | **吃稀有度色**（白 → `CardUIDisplayColorsSO` 该档） | `Background` 的 0 |
| 2·2 | `Background Border` | **19.59**,0 → 330.97,45 | 1549.59,270 → 1860.97,315 | aMin(0.055841296911239624,0) aMax(1,1) piv(.5,.5) apos(**−8.17211**,0) size(16.3442,0) | `40k_deck_cardlist_border` 11×11 · 九宫 **(5,5,5,5)** · **Sliced** · PathID `-5227914696666208855` (ext3) | **吃稀有度色** | `Background` 的 1 |
| 2·3 | `Cost Image` | **2.18**,**−3.00** → **53.18**,**48.00**（51×51） | 1532.18,267 → 1583.18,318 | aMin(0.03,0) aMax(0.03,1) piv(.5,.5) apos(−0.17999,0) size(51,**6**) | `Card Frame Cost Icon` 248×244 · **Simple** · PathID `-3526939114648998153` (ext3) · **`AspectRatioFitter` m_AspectMode=2(高控宽) m_AspectRatio=1** | 白 · **`cost < 0` 时整颗 `SetActive(false)`** | `Background` 的 2 |
| 2·3·1 | `Cost`（TMP） | 3.27,2.71 → 52.09,42.29（48.82×39.58） | 1533.27,272.71 → 1582.09,312.29 | aMin(0.5,0.112) aMax(0.5,0.888) piv(.5,.5) apos(0,0) size(48.8191,≈0) | — | 字号 **41.75**（现场值）/ 基准 **32** / auto **18–50** / **Center + Capline** / 白 / margin (0,0,1.83,0) | `Cost Image` 的 0 |
| 2·4 | `banned Icon` | −0.74,**−6.11** → 50.26,**44.89**（51×51） | 1529.26,263.89 → 1580.26,314.89 | aMin(0.03,0) aMax(0.03,1) piv(.5,.5) apos(−3.10001,**+3.1062**) size(51,6) | `40k_Combat_Icon_Cross` 128×128 · Simple · **PA=1** · PathID `4627540309930320646` (ext13) · ARF 1:1 | 白 · **隐藏判据**：`LiveOpsManager.GetActiveEvent(deckMode)?.IsCardBanned(card)` ⇒ 轮抽无限时活动 / 本机无 LiveOps ⇒ **恒 false** | `Background` 的 3 |
| 2·5 | `Text fill` | 59.10,3.15 → 322.85,41.85（263.74×38.70） | 1589.10,273.15 → 1852.85,311.85 | aMin(0.13,0.07) aMax(0.99,0.93) piv(.5,.5) apos(−2.5,0) size(−5,0) | — | `HorizontalLayoutGroup`：spacing 0 · align **MiddleLeft(3)** · padding 0 · `childControlWidth/Height=1` · `childForceExpand=1` | `Background` 的 4 |
| 2·5·1 | `Card Name`（TMP） | 左沿 59.10 → 右沿 299.98（框），y 3.15 → 41.85 | 1589.10,273.15 → 1829.98,311.85 | 布局子件（aMin(0,1) aMax(0,1)，位置由 HLG 给） | — | 字号 **38** / 基准 **27.69** / auto **2–38** / **Left + Capline** / 色 **(0.934,0.934,0.934,1)** / `EverguildTextController{max 38, min 2}` | `Text fill` 的 0 |
| 2·5·2 | `Count`（TMP） | 左沿 299.98 → 右沿 **322.85**（框 22.87），y 3.15 → 41.85 | 1829.98,273.15 → 1852.85,311.85 | 布局子件 · piv(1,0.5) · `LayoutElement{m_MinWidth=22.87, m_PreferredWidth=0, m_FlexibleWidth=−1}` | — | 字号 **32** / 基准 **36** / auto **2–32** / **Right + Midline** / 色 **(0.933,0.933,0.933,1)** / `EverguildTextController{max 32, min 2}` · **`SetActive(1 < count)`**（≤1 张不显示） | `Text fill` 的 1 |

**兄弟序 ⇒ 描画序**（原版靠 uGUI 树序；我们靠**渲染队列**，见 §三）：
`Background` → `Rarity Gradient` → `Background Border` → `Cost Image` → `banned Icon` → `Text fill`。

**`Text fill` 两端锚点怎么来的**（我们这套没有 uGUI，必须自己算）：
两颗子件都是 `childControlWidth=1` + `childForceExpandWidth=1` ⇒ uGUI `GetChildSizes` 里
`if (childForceExpand) flexible = Max(flexible, 1)` ⇒ **两颗都算弹性的**，最后一个子件的**框右沿 = 容器右沿**；
`Count` 的 `m_MinWidth = 22.87` ⇒ `Card Name` 的框右沿 = `Text fill` 右沿 − 22.87。
（同一条推导在本仓的**唯一一份**在 `Deck/DeckRuntime.cs` 的 `RowCountRightInset` 段；
`DeckInfoPopup.BuildDeckList` 用的是同一套数：名框 `x+64.29 → x+328.73`、份数框 `x+328.73 → x+351.60`。）

---

## 二、行为判据（反编译 + 组件字段）

| 事项 | 判据（逐句） |
|---|---|
| 谁画行、怎么分组 | `DeckListDrawer__Initialize.c`：遍历卡组列表，`stackCopies`(0x48) 为真时命中已建的那一行就 `UICardInfoItem__SetCount(count+1)`，否则 `SpawnCardInfo`。本窗那颗 `DeckListDrawer`(`MB_1819977727535512261`) 实读 **`stackCopies = 1` · `checkOwnership = 0` · `showWarlord = 0`** |
| 卡名 | `UICardInfoItem__Initialize.c`：`RawCardScript.GetLocalizedCardName(card)`（异画再 `Concat` 一个字面量后缀 — **那个字符串没解出来**，见 §五） |
| 费用 | 同文件 `SetCost`：`SetActive(cost >= 0)`（`cost>>31 ^ 1`）+ `cost.ToString()` |
| 份数 | `SetCount`：`SetActive(1 < count)` + `String.Format("x{0}", count)`（字面量在 `all_strings.txt:6629` 现核 = **`x{0}`**） |
| alpha | `Initialize`：`checkOwnership==0 ⇒ CanvasGroup.alpha = 1f`；否则 `owned >= count ? 1 : 0.5`（`DAT_1834b2bb8 = 1.0` / `DAT_1834b2bb4 = 0.5`，**从 `GameAssembly.dll` 现读**） |
| 稀有度染色 | `Initialize`：`CardRarityColorsSO.GetColor(colorsByRarity, PlayerItem.get_Rarity(card))` → `foreach (img in imagesToChangeColorByRarity) img.color = c`。**哪两颗** = `[-4589288686163035451(Rarity Gradient), -5011364358911591739(Background Border)]`；**哪张表** = `colorsByRarity` → pid `-2272266055686004801` = **`CardUIDisplayColorsSO`**（⛔ **不是** 同包那份 `CardRarityColorsSO`，那是宝箱 `CardInBoosterPack` 用的）⇒ 全工程唯一实现 = `Core/RarityColors.cs`，本批**转调**它 |
| 点一行 | `DrafModeDeckInfoCardListPanel__DeckListOnOnCardSelected.c`：函数体只有 `CardDisplayWindow.Instance.ShowCard(card)` ⇒ 与「点战将名」**同一扇窗**（= 我们的 `CardDetailPopup`） |
| 悬停/按下色 | `MB_6514008942570470085`（那颗 `EverguildButton`）：`m_Transition = 1`(ColorTint) · Normal (1,1,1,1) · **HL (0.6886792×3)** · **Pressed (0.7843137×3)** · Selected (0.9607843×3) · Disabled (0.7843137×3) · `m_ColorMultiplier 1` · `m_FadeDuration 0.1` · `m_TargetGraphic` = `Background` 的 Image · `m_RaycastPadding (0,0,0,0)` |
| 可点区域 | 行子树里**只有 `Background`** 的 `m_RaycastTarget = 1`（色条/描边/费用圈/`banned Icon`/两颗 TMP 全 0）⇒ 命中区 = 行内 **18.48..330.97 × 0..45**（左边那 18.5 是费用圈探出去的那一段，点不到） |

**为什么轮抽里 `banned` 恒 false**：判据是 `LiveOpsManager.GetActiveEvent(deckMode)` 再 `IsCardBanned(card)`；
轮抽不是限时活动、本机也没有任何 LiveOps 事件 ⇒ 取到 null ⇒ 恒 false。节点照建、`SetActive(false)`（树形与 prefab 一致）。

---

## 三、改动清单

### 1. `MyGame/Assets/CardPresentation/Shell/DraftModeWindow.cs`

**(a) 新增行模板常量块**（紧跟 `DeckScrollNotchPx`）：`RowBgOff` / `RowGradOff` / `RowBorderOff` / `RowCostOff` /
`RowCostTxOff` / `RowBannedOff` / `RowTextFillOff` / `RowCountW = 22.87` / `RowNameBoxOff` / `RowCountBoxOff` /
四条渲染队列 `QRowBg 4143 · QRowGrad 4144 · QRowBorder 4145 · QRowCost 4146`（**4155/4160 之前没有别的常量占位**，
逐条核过本文件的 4130–4194 全表）/ 五个图名常量。每一条的出处写在常量 doc 里。

**(b) `RebuildDeckListRows()` 重写上半段**：
- **按卡 id 去重 + 计数**（原版 `stackCopies=1`）；
- 行数 / 内容高改用**去重后的行数**（此前按张数 ⇒ 重复卡会多画几行、内容高偏大）；
- 🔴 **就地订正一处真缺陷**：原来**无条件**把 `Offset` 夹回 `[ClampLo, ClampHi]`。本方法是
  `MenuScroll.OnChanged`（**拖拽/回弹每一帧都回调**）⇒ 内容短于视口时（本窗常态）**弹性越界被当场抹平**
  （`m_MovementType = 1`(Elastic) 的橡皮筋一点都不剩，静默表现为「拖不动」）。现在**只在内容两端真的变了**
  （`ContentX1/X2` 与上次不等）时才夹 —— 那是它的本职（内容变短后旧偏移必须收回）。
  `ContentX1/X2` 照旧**每次写**（`BattleLogPopup` 那条纪律不变）。

**(c) 新增 `BuildCardListRow(rr, def, cardId, count)`**：逐件照 §一 的树形与矩形画 ——
行根 `Deck Selector Card Info button` / `Content` / `Background`(九宫) / `Rarity Gradient`(Simple+稀有度色) /
`Background Border`(九宫+稀有度色) / `Cost Image`+`Cost` / `banned Icon`(建了但关着) / `Text fill`+`Card Name`+`Count`；
外加**命中区**（`Background` 矩形 · `QHit` · 悬停色逐颗覆盖成 prefab 那组，`TintOn` 只染行底那一族 quad）。
🔴 折行档照 prefab 还原：`Card Name` / `Count` 的 `m_TextWrappingMode = **0**(NoWrap)`（`MB_-4537744533788788027` /
`MB_476039404897204933` 实读）⇒ 在 `SetAutoFitBox`（`MenuDraw.Text` 内部）之后调 `SetWrapping(false)`；
`Cost` 是 **1**(Normal) ⇒ 保持 `Text` 给的那一档。

**(d) 新增 `Off(PxRect, PxRect)`**：行内偏移 → 绝对矩形（唯一一份换算）。

**(e) 新增两条卡片详情入口**：`ShowCardDetail(CardDef)`（私有共用）/ `OpenCardRowDetail(string)`；
`OpenWarlordCard()` 改成转调 `ShowCardDetail`；字段 `_warlordDetail` 改名 `_cardDetail`（两条入口共用一扇窗）。

**(f) 新增自检读口**：`DeckListRowCountForTest` · `DeckListRowForTest(i)` ·
`DeckListRowChildForTest(row, name)` · `DeckListRowTextForTest(row, name)` · 九个 `DeckRow*OffForTest`。

**⚠️ 影响面**：本文件只被 `Shell/` 内部使用；`BuildCardListRow` / `Off` / 常量全是新增私有件。
`RebuildDeckListRows` 的签名与调用点不变。**没有改 `MenuDraw`**（白名单里那份**没用上**）。

### 2. `MyGame/Assets/CardPresentation/Editor/DraftAssertD3.cs`

- 新增四个断言小工具：`SoRarity`（**自检侧独立抄的** `CardUIDisplayColorsSO` 六档色）/ `SoRarityTier` /
  `ColorNear` / `FirstQuadIn` / `UnionQuadsPx` / `InkSpanPx`。
- ⑤b 那两条「行数 / 内容高」的期望值从「张数」改成「**去重卡种数**」（原版 `stackCopies=1`）。
- 新增 **⑤c** 整段（见 §四）。

---

## 四、断言清单（`DraftAssertD3` ⑤c；宿主 = `MainMenuScene.Run`，它已调 `DraftAssertD3.Run()`）

| # | 断的是什么 | 期望值出处 |
|---|---|---|
| ⑤c·0 | 行数 = 去重后的卡种数 | `DeckListDrawer.stackCopies=1` |
| ⑤c·1 | 行根名 = `Deck Selector Card Info button` | prefab `m_Name` |
| ⑤c·2 | `Background` 左右上下沿 = 行 + (18.48 / 330.97 / 0 / 45) | 锚 0.05584 × 330.97（九宫格 → 取 9 块并集） |
| ⑤c·3 | `Rarity Gradient` 左沿 207.32 / 右沿 330.97 / 高 44.89 | 锚 0.60575 + apos −0.22641 + sizeDelta 0.4528 / −0.1035 |
| ⑤c·4 | 色条 tint = `CardUIDisplayColorsSO` 该稀有度的色（**另一份表**比） | `UICardInfoItem__Initialize.c` + SO 六档 |
| ⑤c·5 | `Background Border` 左沿 19.59 / 右沿 330.97 + tint 与色条同色 | 锚 0.05584 + apos −8.172 + sizeDelta 16.344 |
| ⑤c·6 | `Cost Image` 左沿 2.18 / 宽 51 / 上沿 −3 / 高 51；`Cost` 文本 = 费用数字；费用墨迹中心 27.68 | 锚 0.03 + ARF 1:1；`SetCost` |
| ⑤c·7 | `cost < 0` ⇒ `Cost Image` 不建 | `SetActive(cost >= 0)` |
| ⑤c·8 | `banned Icon` 节点在、**关着** | `SetActive(banned)`；轮抽恒 false |
| ⑤c·9 | `Card Name` 文本 = 本地化卡名；**墨迹左沿 = 59.10**；字色 (0.934,0.934,0.934) | `Text fill` 左沿 + `m_fontColor` |
| ⑤c·10 | `Count`：>1 张时文本 `x{n}` 且**墨迹右沿 = 322.85**；=1 张时**不建** | `SetCount` + `Text fill` 右沿 |
| ⑤c·11 | 命中区 = 18.48..330.97（= `Background` 矩形） | `m_TargetGraphic` + 全子树 `m_RaycastTarget` |
| ⑤c·12 | **滚动后**第 0 行色条上沿 = 270 − `Offset`；复位后回 270 | Elastic 拖动 + `OnChanged` 重画 |
| ⑤c·13 | **切到 Deck Info 页 ⇒ 行整棵不显示**（「新加一层配一条『该藏的时候藏住了吗』」） | 三页 `SetActive` 互斥 |

🧨 每条都带「怎么改会红」；**期望值是 prefab 数字，不是 `DraftModeWindow` 的常量**（⑤c·4 的色表刻意另抄一份）。

---

## 五、没查清 / 如实标注（⛔ 没有拿猜测顶替）

1. **异画卡名的后缀字符串**：`UICardInfoItem__Initialize.c` 里 `GetLocalizedCardName(card) + DAT_184272ad8`，
   而 `DAT_184272ad8` 落在 `.data` 的 metadata-usage 槽（值 `0xa0000c69`），**不是能直接读的字面量**
   ⇒ **没解出来**。影响面：只有「这张卡是异画」时名字尾巴会差一截；轮抽的卡来自卡包、**走不到异画**
   ⇒ 本批**不编**这个后缀（⛔ 不猜 `" (Alt)"` 之类）。
2. **`Text fill` 内部两颗的宽度分配**：原版是 uGUI `HorizontalLayoutGroup` 按 TMP 首选宽动态分；
   我们没有 uGUI ⇒ 只复刻**两端**（名左对齐到 `Text fill` 左沿、份数右对齐到右沿）+ 框宽取
   `Count` 的 `m_MinWidth 22.87`。**中间那段分配算法没有复刻**（画面在今天的卡池上与复刻结果同级）。
3. **`Content` 上的 `CanvasGroup`**：我们这套没有 CanvasGroup ⇒ 不建该组件（`checkOwnership=0` ⇒ 它恒 1，
   **画面上无差**）。节点 `Content` 本身照建。
4. **`Text fill` 的 `m_padding / spacing`**：prefab 实读全 0 / 0（对齐 MiddleLeft）⇒ 与「两端锚点」等价，无差。
5. `objectsWhenHasData` / `objectsWhenNoData` **两个数组都是空的**（`MB_-1598429920373146939` 实读）⇒ 无行为。
6. `EverguildTextController`（两颗 TMP 各挂一个）只有 `maxFontSize/minFontSize`，与 TMP 的 autosize 上下限同值
   ⇒ **只是夹取，没有额外行为**。
7. 本批**没跑 Unity**（红线①）⇒ ⑤c 那 13 组断言**未在编辑器里真跑过**；只过了秒级类型检查。

---

## 六、顺手发现（⛔ 未擅自扩大范围）

1. **`RebuildDeckListRows` 无条件夹偏移 ⇒ 弹性越界被抹平**（已在本次**就地修掉**，见 §三·(b)）。
   症状：`m_MovementType = 1`(Elastic) 的橡皮筋在内容短于视口时完全不生效（拖一下就回原位）。
   ⚠️ **同族写法可能还有别处**（`grep` 其它 `OnChanged` 回调里那句 `Offset = Mathf.Clamp(...)`）—— 本批没扫。
2. **滚动时每一帧整表重建**：D5 的既有设计（`OnChanged = RebuildDeckListRows`），本批给行加了 5 图 + 3 文本后，
   一屏 12 行 ⇒ 每次回调重建 ~96 个 GameObject（含 36 颗 TMP）。与 `CollectionWindow` 那些列表同一条路子，
   但**真机拖动时可能掉帧**。更省的做法是「建一次 + 搬行」（`DeckRuntime` 那套 `ClipQuadToBand`），
   但那要把视口裁切改成手动重裁 —— 属另一笔账，**没动**。
3. `MenuDraw.Rect` 给 `banned Icon` 传的是 `keepAspect=false`（原版 `m_PreserveAspect = 1`）——
   那张图 128×128、框 51×51 **正方形，画面上逐位相同**；本批照「隐藏件不折腾」处理，如实登记。

---

## 七、第二轮：`MainMenuScene.Run` 15 红诊断与修复（2026-10-25 · 协调者追加）

日志：`d:/4/_tmp_view/20261025/mainmenu.log`（行首 `[D3] ✗ ⑤c` / `[Menu] ✗ 头像网格` / `[U2b] FAIL (V5)`）。

### 7.1 ⑤c 行模板 —— 12 条（**全部是断言量错，实现没照错的判据**）

先说结论：**实现侧一个像素都没错**。12 条红全部来自「**断言量错了对象**」——三类量法坑，
每一类都能用「实得那几个数」反算出来，逐条对得上：

| # | 断言 | 实得 / 期望 | 根因（可反算验证） | 改了什么 |
|---|---|---|---|---|
| 1-3 | 第 0/1/2 行 **行底左沿** | 1530.00 / 1548.48 | `UnionQuadsPx` 用 `GetComponentsInChildren` **全收**，把 `Background` 底下**同父链的** `banned Icon`（x1 = 1529.26，被视口左沿 **1530** 裁过）也并了进来 | 换成 `OwnQuadsPx`：只收**名字 = `<根名>_ij` 的直接子件**（九宫格自己那 9 块，命名见 `ImageQuad.CreateNineSlice` 的 `$"{name}_{i}{j}"`） |
| 4-6 | 第 0/1/2 行 **行底高** | 48.00 / 54.11 / 54.11 vs 45 | 同上：并进了「探出行外 3px 的 `Cost Image`」（底 318）与 `banned Icon`（上 311.39）⇒ 高 = 318−270 = **48**（第 0 行，banned 上沿被视口顶 270 裁）/ 365.5−311.39 = **54.11**（第 1、2 行）。**这两个数逐位对得上** | 同上 |
| 7-8 | 第 1/2 行 **行底上沿** | 311.39 / 358.89 vs 317.50 / 365.00 | 同上（= banned Icon 的上沿 263.89 + 47.5 / +95） | 同上 |
| 9 | 第 0 行 **费用圈上沿** | 270.00 / 267.00 | 费用圈**上下各探出行外 3px**，第 0 行那 3px 被**滚动视口顶**（270）裁掉 —— 原版 `RectMask2D` 同一条 | 期望改成 `ClipView(…)`：**先与滚动视口求交再比**（完全在视口内的行逐位不变） |
| 10 | 第 0 行 **费用圈高** | 48.00 / 51.00 | 同上（267+3 → 270 ⇒ 51→48） | 同上 |
| 11 | **行上有命中区** | 未建 | `MenuDraw.Hit` 的 quad 挂在命中节点**底下那颗 `Hit`** 上（`MakeHitQuad`），节点自己身上**没有** `ImageQuad` ⇒ `GetComponent` 恒 null | 改 `GetComponentInChildren<ImageQuad>(true)` |
| 12 | **滚动后第 0 行色条上沿** | 270.00 / 253.95 | 行**已经重画了**（`Offset` 实得 16.05 ⇒ 期望 253.95 就是由它算的）——但往上拖时整行**移出视口顶**，量到的 quad 被视口裁成 270 ⇒ 量的是「裁过的那块」，断言等于空转 | 改成**往下拖**（Elastic 越界 ⇒ `Offset` 为负 ⇒ 行整体**下移**、整行仍在视口内），期望 = `270 − Offset`（`Offset` 负 ⇒ 285.95） |

🔑 **顺带一条实证**：第 12 条能算出「期望 253.95」本身就证明**上一轮那条 `Offset` 夹法的订正生效了**
（`Offset = 16.05` 在 `OnChanged` 重建之后**没被夹回 0**）—— 改前那些行的 `Offset` 会当场变 0。

### 7.2 `[Menu] ✗ 头像网格只建了看得见的格子`（1 条）+ `[U2b] FAIL (V5)`（2 条）

**先查清口径**（协调者要求的那一问）：

- `AvatarTab.BuiltCells` = `_cellArt.Count` = **469**（`BuildRows` 对**全表**逐格 `Add`，与 `ProfileData.Avatars.Count` 同源）；
- `ProfileAssertU2b` 的 `cells` = `Item Drawer` 下名字以 `Avatar Item Small_` 开头的子件数 —— **也是 469**；
- 旧的 `15` 是「**只建看得见的**」那套**自加口径**留下来的数（D7 删掉 `Intersects` 之后它必然红）；
- `nameNodes` 实得 **10/469**：`Avatar Name` 与立绘**走同一条闸** —— `MenuDraw.Text/Rect` 对「整块在视口外」
  的件**连节点一起不建**（A798，全工程通用）⇒ 视口外的格子里只有空壳节点。
  ⇒ **`nameNodes == cells` 在本工程做不到、也不该断**（那不是 `Intersects` 早退，是渲染管线的通用闸；
  与原版 `RectMask2D`「建了但看不见」视觉等价）。

改了什么（⚠️ 两个文件都**不在我原白名单**，按协调者 V-B 的明确指派改的，逐条列在这里备查）：

| 文件 | 改动 |
|---|---|
| `Editor/MainMenuScene.cs`（原 6768 行） | `BuiltCells > 0 && < 40`（「只建看得见的」）⇒ **`BuiltCells == ProfileData.Avatars.Count`**（全表 469 格节点全建）；🧨 写清「把 `Intersects` 加回来 ⇒ 红」 |
| `Editor/ProfileAssertU2b.cs` | ① `cells == 15` ⇒ **`cells == ProfileData.Avatars.Count`**；② `nameNodes == cells` ⇒ **`nameNodes > 0 && nameNodes <= cells`**（画出来的格子都带名字，视口外的由 A798 跳过）+ **新增**「第 0 格（一定在视口内）必有 `Avatar Name` 且关着」；③ 文件头 V5 的两段说明就地翻面（原文写着「建与不建 = 名字矩形与视口求交 / 15 格里第 3 行那 5 颗」= 旧口径） |

### 7.3 本轮的验证

- `TMPDIR=/tmp/wf_v1 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**；
- ⛔ **没跑 Unity**（红线①）⇒ ⑤c 与 V5 的新期望值**还没在编辑器里复跑过**，下一次 `MainMenuScene.Run` 才算数。

### 7.4 顺手发现

- `ProfileAssertU2b.Overlaps` 现在是**孤儿**（V5 不再求交）。**没删**（同族以后要用，删了等于丢判据），
  但在文件头写明「它不是 V5 的判据」。
- `AvatarTab.BuildRows` 里那句注释「这一屏建出来的格子数（自检用：469 条全建会卡，必须只建看得见的）」
  **已经过期**（D7 删了早退、口径改成全建）—— 属 `Shell/AvatarTab.cs`（**不在我白名单**，D7 的文件）⇒
  **只报不改**，请调度台转给 D7 或统一收拾。
