# WL1 · A251-L1（`BaseOfferPopup` 母版 + 20 变体）

> 现核时刻：**2026-10-13** 会话 · 写手 **WL1**（批次「清空 A 表」第四轮）
> 代码根 = `d:/4/Unity/MyGame/Assets/CardPresentation/` · 资料根 = `d:/4/Unity/资料/`
> 本件**没跑 Unity**、**没动 git**、**没改 `d:/2/`**（只按名字直读了解包资源）
> ⚠️ **判据一律现读**：21 份 prefab 逐个跑 `menu_dump` + 21 份 MB 逐个解释义 —— 本文里**没有一格是推的**
> （唯一两处**我们算的**已逐字标出，见 §三·补）

---

## 一、结论

- **母版建起来了**：`Shell/BaseOfferPopup.cs`（新文件，1084 行）—— 原版 `Base Offer Popup` 的 **37 个节点**
  逐节点照抄（名字 / 层级 / 矩形 / 旋转 / 字号 / `m_fontSizeBase` / 对齐 / 颜色 / 九宫格 / 出厂显隐）。
- **20 个变体按 `menu_dump` 逐个数差后建**：不是「20 份各写一遍」，而是**一张 21 条的变体表**
  （`BaseOfferPopup.Variants`）+ **一个骨架构造器**（`Build()`）。**每份的差**有三类：
  ① **几何档**（两档）② **`Artwork` / `foreground` 的出厂显隐**（各 5 份是 `F`、**不是同一批**）
  ③ **`window` 下的抽屉槽**（名字 / **兄弟序** / 矩形 / 绕 z 的旋转 / 出厂显隐 / 抽屉类）
  ⇒ 逐条差表见 **§四**。
- **注册进 `Shell/WindowsManager.cs`**：`PrefabRefBaseOfferPopup` 常量 + `OpenBaseOfferPopup(variant, content)`
  方法（**只加了我这一扇**，⛔ 没替 L2/L3/L4 写）。
- **断言**：`Editor/ShopScene.cs` 新加一节（**+245 行**，**唯一一个 hunk**），**66 处 `Check*` 调用点**。
  宿主 = `ShopScene.Run`（**现读确定**：`BoosterInfoPopup` 在该文件 **26 处**、`OfferContainer` **63 处**
  —— 商店族弹窗的既成宿主；见 §七·0）。
- **类型检查**：`TMPDIR=/tmp/wf_wl1 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- **一份可复现的对账**：`_tmp_view/wl1/check_table.py`（纯 python）把 `Variants[]` 与 21 份现读逐格比
  ⇒ **363 项对账 · 不符 0**（窗口矩形 / 出厂显隐 / 86 个抽屉槽的名字·序·矩形·旋转·出厂态 / `AvatarHelpText`）。

---

## 二、建了什么（文件清单 + 每个文件做什么）

| 文件 | 动作 | 内容 |
|---|---|---|
| **`Shell/BaseOfferPopup.cs`** | 🆕 新建（1084 行） | `BaseOfferPopup : GameWindow` —— 队列档 · 两档几何（`GeoSmall` / `GeoBig`）· 出厂文本常量 · **21 条变体表** · `Build()` 骨架 · 抽屉槽转调 `ItemDrawer.Draw` · `BlinkGraphic` 接线 · `Create/Show/Resolve/Find/DefContent/FillDef` · `Dump()` |
| **`Shell/WindowsManager.cs`** | ✏️ 改（**+39 行 / 2 个 hunk**） | ① `PrefabRefBaseOfferPopup` 常量（挨着另外三条 `PrefabRef*`）② `OpenBaseOfferPopup(variant, content)` 方法（走 `OpenByRef`） |
| **`Editor/ShopScene.cs`** | ✏️ 改（**+245 行 / 1 个 hunk**） | 一节 **66 处 `Check*`**（插在「实拍」之后、「收尾」之前 —— 不污染任何实拍） |
| `d:/4/_tmp_view/wl1/*` | 草稿（**产物，不入库**） | `check_table.py`（对账脚本）· 各种 dump 中间件 |

**它复用了什么**（⛔ 没有另立一套）：

| 复用对象 | 干什么 |
|---|---|
| `Shell/ItemDrawer.cs` 的 `Draw` / `SetPremium` / `SetEphemeral` / `SetConverted` | 抽屉往里画什么 —— **判据只此一份**，本件只**转调** |
| `Shell/OfferContainer.cs` 的 `Content`（报价内容结构） / `SlotTypes` / `DrawerClassOf` | 报价格式与「槽名 → 抽屉类」那张表（本件**不另抄一份**，只在断言里互核） |
| `Shell/BoosterInfoPopup.cs` 的建筑风格 | 同族近亲（`window` / 窗底 / 关闭钮 / `Artwork` / `Title` / `Category` / `Descripton` / `Purchase buttons` 的矩形**几乎逐位相同**） |
| `MenuDraw.{Node,Rect,Nine,Text,TextBox,Hit,ShadeHit,Absorb,AlignLeft,ClearChildren}` · `Shell/BlinkGraphic.cs` · `CardArt.MenuUi` | 全部建节点/画图/命中/闪烁的公共件 |
| `WindowsManager.OpenByRef` | 开窗与复用（= 原版 `automaticallyLoadedWindows`） |

---

## 三、逐节点对照表（母版 · 原版 `Base Offer Popup` · **37 节点**）

**出处命令**（**唯一一条**，全表由它出来）：
```bash
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Base Offer Popup" --depth 7 --relative --md
```
（`--relative` = 相对根左上角；⚠️ 不带它会踩到那 5 份根带偏移的 `Premium_*`）

| # | 原版路径（缩进层） | 原版值（矩形 / 字号 / 色 / 九宫格） | 我们的值 | 出处 |
|---|---|---|---|---|
| 1 | `Base Offer Popup` | 1920×1080 · `BaseOfferPopup` | 同名根节点 · 同尺寸 | dump `:1` |
| 2 | `Menu Dark Background` | `-1327.30,-746.18→3247.30,1826.18` · `m_Color (0,0,0,0.773)` · `BackgroundCloseButton` | 同矩形 · 同色（`CardArt.Solid()` + tint）· 命中共 `MenuDraw.ShadeHit` | dump `:2` |
| 3 | `window` | `395.72,188.35→1524.28,851.65`（1128.55×663.296） | 同 | dump `:3` |
| 4 | `··Generic Window Red Background Big` | `395.70,178.35→1547.30,895.80` · `UI_Deck_Information_Back` 1100×701 · 九宫 **42,363,655,81** · Sliced | 同（`MenuDraw.Nine`） | dump `:4` |
| 5 | `··Generic Close Button Orange` | `1487.08,159.85→1561.47,235.45` · `UI_Button_Round_background` 237² · PA | 同（一颗节点带 Image + 两个子件） | dump `:5` |
| 6 | `···Background` | `1495.24,167.83→1552.10,225.96` · `40k_general_bt_yellow` | 同 | dump `:6` |
| 7 | `···Icon` | 同矩形 · `40k_general_bt_yellow_close` | 同 | dump `:7` |
| 8 | `··Artwork` | `395.72,188.35→960.00,851.65` · **出厂 `act` 逐份不同**（见 §四） | 同矩形 · 出厂态照表 | dump `:8` |
| 9 | `···background` | `405.72,198.35→950.00,841.65` · `40k_shop_popup_info_bg` · **`BlinkGraphic`** | 同矩形 · **图工程里没有**（见 §八）⇒ 节点建、不画 · `BlinkGraphic` 已接 | dump `:9` |
| 10 | `···foreground` | `395.72,218.35→960.00,861.65` · sprite **空**（运行期灌） | 同矩形 · 只建节点 · 出厂态照表 | dump `:10` |
| 11 | `··Text` | `960.00,204.35→1508.28,835.65` | 同 | dump `:11` |
| 12 | `···Title` | `976.00,260.56→1492.72,312.56` · fs40 · **base 45.2** · auto[3~40] · Left/Middle | `MenuDraw.Text` + `AlignLeft` + `SetAutoFitBox(…,3,40,45.2)` | dump `:12` |
| 13 | `···Category` | `976.00,307.85→1492.72,352.85` · fs39 · base 39 · auto[3~39] · 字距 **−1.8** | 同 + `SetCharSpacing(-1.8f)` | dump `:13` |
| 14 | `···Descripton` | `976.00,356.64→1477.24,672.07` · fs35 · base 39 · auto[3~35] | 同（文本 = **prefab 出厂原文**，见 §九①） | dump `:14` |
| 15 | `···Available Counter` | `422.00,282.75→730.05,314.35` · fs30 · base 39 · auto[10~30] · Left/Bottom · **折行=0** | 同（**一颗 TMP 自己**）· **建成关着**（§五） | dump `:15` |
| 16 | `···Timer` | `1102.97,766.48→1300.64,793.24` · `TimerDisplay` + `HorizontalLayoutGroup` | 建节点 + 两个孩子 · **整件关着**（§五） | dump `:16` |
| 17 | `····Icon` | `1185.32,764.86→1218.28,794.86` · `WF_icon_clock` · PA | 同 | dump `:17` |
| 18 | `····Timer Text` | `1218.28,767.36→1218.28,792.36`（**宽量出来 0.00**）· fs30 · Left/Midline | 同矩形（0 宽）· 文本 = 出厂 `23h 34m` | dump `:18` |
| 19 | `···Offer Badge` | `403.50,217.35→809.49,296.20` · `WF_Special offer_Value` 324×87 · 九宫 **162,0,162,0** · tint (0.651,0,0,1) | 同 · **整件关着**（§五） | dump `:19` |
| 20 | `····Text (TMP)` | `419.50,233.35→798.73,280.20` · fs38 · Left/Midline | 同 | dump `:20` |
| 21 | `···Purchase buttons` | `975.00,701.91→1476.24,762.29` · `HorizontalLayoutGroup` spacing 15 · `TransformScalerBySmallScreenUI` | 同（**照布局跑完的实测矩形摆**，同 `BoosterInfoPopup` 那条纪律） | dump `:21` |
| 22 | `····Price Display` | `977.68,695.35→1217.32,768.86` · `PriceDisplayButton` | 同 | dump `:22` |
| 23 | `·····Generic UI Button` | 同矩形 · `40K_button` 489×107 · 九宫 **234,46,234,46** · tint (0.902,0.637,0.18,1) · PA | 同（一颗节点带 Image + 两个子件） | dump `:23` |
| 24 | `······Button Text` | `990.68,711.32→1204.32,752.88` · fs12 · **出厂 `act=F`**（**21/21 逐个核过**） | 建成关的 · 字串 = 出厂空串 | dump `:24` |
| 25 | `······Price Display` | `986.75,710.17→1206.37,755.13` · HLG | 同 | dump `:25` |
| 26 | `·······icon` | `1011.83,710.17→1056.79,755.13` · sprite **`<无图>`（21/21）** | 只建节点、不画 | dump `:26` |
| 27 | `·······text` | `1034.31,710.17→1158.81,755.13` · fs47.45 · base 39 · auto[12~54] | 同 | dump `:27` |
| 28 | `·····Offer Price Discount Badge` | `840.51,596.16→1019.48,775.14` · `40k_OfferBadge` 256² · **rot 17.17°** | 同矩形 + `Quaternion.Euler(0,0,17.17)` | dump `:28` |
| 29 | `······Discount Title` | `883.17,638.25→975.41,691.62` · fs28.15 · base 36 · auto[18~72] | 同 · 文本 = 出厂 `Previous Price` | dump `:29` |
| 30 | `······Discount Price` | `883.56,691.62→976.13,728.63` · fs39.05 · base 36 · auto[18~72] | 同 · 文本 = 出厂 `$19.99` | dump `:30` |
| 31 | `····WebShop Button` | `1232.32,706.16→1473.56,758.04` · `WebShopOpenButton` + HLG（**自己没 Graphic**） | 同 | dump `:31` |
| 32 | `·····Highlight` | `1195.32,667.55→1510.56,796.65` · `OctagonUI Filled Fade SDF` 128² · 九宫 52 · α 0.8 | 同 | dump `:32` |
| 33 | `·····Button Image` | `1232.32,706.16→1473.56,758.04` · `40K_button` · tint (0.333,0.878,0.336,1) | 同 | dump `:33` |
| 34 | `·····Icon` | `1261.00,706.16→1312.88,758.04` · `40K_Icon_Discount_Gold` · **`localScale 1.2`** | 同（按**视觉框** 62.26² 摆 —— 同 `BoosterInfoPopup` 那处） | dump `:34` |
| 35 | `·····Button Text` | `1286.94,706.16→1418.94,758.04` · fs34.2 · base 12 · auto[12~44] · **折行=0** | 同 + `SetWrapping(false)` | dump `:35` |
| 36 | `Preview`（**根级 · 在 `window` 之外**） | `422.00,795.00→592.00,845.00` · `40K_button` · tint (0.902,0.18,0.19,1) · `PreviewOfferButton` | 同 | dump `:36` |
| 37 | `·Button Text` | `431.04,800.29→582.41,839.73` · fs40 · base 12 · auto[10~40] · **折行=0** | 同 + `SetWrapping(false)` | dump `:37` |

### 三·补 —— **全文件唯一两处「我们算的，不是照抄」**（铁律 3）

`GeoBig` 那一档的 **`Price Display/text`** 与 **`WebShop Button/Button Text`**：dump 量出来的宽是 **0.00**
（它们带 `ContentSizeFitter(h:PreferredSize)` / `AspectRatioFitter`，而文字宽要 Unity 的字体度量）。
**判据（能反推、不是猜）**：`GeoBig` 里价签 `icon` 的 x1 恰好 = 「只按图标自己（文字宽 = 0）在 219.62 的框里居中」
（`1130.75 + (219.62−44.96)/2 = 1218.08` ✓ 与 dump **逐位相同**）；而 `GeoSmall` 同一格 = 「按图标 **+ 124.50** 居中」
（`986.75 + (219.62−169.46)/2 = 1011.83` ✓）⇒ 证明 dump 那一刻文字宽 = 0。
⇒ **`GeoBig` 那两格取 `GeoSmall` 的宽**（同一串字、同一档字号、同一档框宽 219.62/241.24 ⇒ 宽本来就该相同），
x 按「`Purchase buttons` 子树 Δx 恒 **+144.00**」平移（六个节点逐个对上）。
⛔ 这两格是**全文件唯一两处非逐字照抄**，改版式时**从这两格看起**。

---

## 四、20 个变体逐个差表（母版 → 变体）

**三类差，各自独立**（不是「每份一套版式」）：

### 差·一 —— 几何档（**两档，没有第三档**，21 份逐个 `--relative` 现读）

| 档 | `window` 矩形 | 份数 | 哪些 |
|---|---|---|---|
| `GeoSmall` | `395.72,188.35→1524.28,851.65`（1128.55×663.296） | **16** | 母版 / `Just Foreground` / `Booster_*` 六份 / `Deck_cardback_avatar` / `Expansion pass` / `Premium_Premium_cardback_avatar` / `Premium_booster_title_avatarOrResource` / `Single Item Type` / `avatarOrTitle_resource` / `cardback_premiumOrAvatarOrResource_titleOrResource` |
| `GeoBig` | `246.94,159.58→1673.06,880.42`（1426.11×720.843） | **5** | `Variant 2 Currencies` · `Variant Booster + 2 Currencies` · `Variant Premium_Booster_avatar_cardback_title` · `…_title_resource` · `Variant Premium_Resource` |

⚠️ **其余节点不是「跟着 window 平移」**（逐条核过）：`Purchase buttons` 整棵子树在 `GeoBig` 里
**恰好 = `GeoSmall` + (+144.00, 0)**（`Price Display` / `Generic UI Button` / `Offer Price Discount Badge` /
`Discount Title` / `Discount Price` / `WebShop Button` 及其三件，六个逐个对上）；但
`Available Counter` / `Offer Badge` 是 **Δ(−146.50, −28.77)**、`Timer` 是 **Δ(+152.01, −2.02)**、
`Preview` 是 **Δ(−131, +24)** ⇒ **各有各的锚点**，只能逐档存实读值。

### 差·二 —— `Artwork` / `foreground` 的出厂显隐（**各 5 份，而且不是同一批**）

| 变体 | `Artwork` | `foreground` |
|---|---|---|
| `Base Offer Popup` · `Just Foreground` | T | T |
| `Booster_CardOrAltArt` · `…_AvatarORTitle` · `…_Cardback_Avatar_Title` | **F** | T |
| `Premium_Premium_cardback_avatar` · `Single Item Type` | **F** | T |
| `Variant 2 Currencies` · `Booster + 2 Currencies` · `Expansion pass` · `Premium_Resource` · `avatarOrTitle_resource` | T | **F** |
| 其余 7 份 | T | T |

⇒ **两件不能合并成一个开关**（合成一个 ⇒ 上表里 10 行里有 5 行必错）。断言已按这条钉（§七·③）。

### 差·三 —— `window` 下的抽屉槽（**这是 20 份真正不同的地方**）

**21 份合计 86 个槽**（母版与 `Just Foreground` **一个都没有**）。每份的「槽数 / 名字 / 兄弟序 / 矩形 /
旋转 / 出厂显隐 / 抽屉类」**逐格在 `BaseOfferPopup.Variants[].Drawers` 里**，与现读**363 项对账 0 不符**。
先把**每一份的槽名与槽数**列出来（名字**逐字**，含 `" (1)"` 这类后缀；`*` = 出厂关）：

| 变体（去掉前缀 `General Basic Offer Popup `） | 节点数 | 槽数 | 槽（**兄弟序**） |
|---|---|---|---|
| `Base Offer Popup`（母版） | 37 | 0 | — |
| `Just Foreground` | 37 | 0 | — |
| `Variant 2 Currencies` | 61 | 2 | `Icon Currency Drawer Variant (1)` · `Icon Currency Drawer Variant` |
| `Variant Booster + 2 Currencies` | 73 | 3 | `Icon Container Drawer Variant` · `Icon Currency Drawer Variant (1)` · `Icon Currency Drawer Variant` |
| `Variant Booster_avatar_resource` | 75 | 3 | `Icon Container Drawer Variant` · `Icon Avatar Drawer Variant` · `Icon Currency Drawer Variant` |
| `Variant Premium_Resource` | 75 | 3 | `Icon Currency Drawer Variant` · `Icon Premium Campaign Drawer Variant` · `Icon Expansion Pass Premium Drawer Variant` |
| `Variant Booster_cardback_resource` | 83 | 3 | `Icon Container Drawer Variant` · `Cardback Drawer` · `Icon Currency Drawer Variant` |
| `Variant Booster_title_resource` | 89 | 3 | `Icon Container Drawer Variant` · `Icon Currency Drawer Variant` · `Title Drawer Horizontal Variant (1)` |
| `Variant avatarOrTitle_resource` | 91 | 3 | `Icon Currency Drawer Variant` · `Title Drawer Horizontal Variant (1)` · `Icon Avatar Drawer Variant` |
| `Variant Premium_Premium_cardback_avatar` | 99 | 4 | `Cardback Drawer` · `Icon Premium Campaign Drawer Variant` · `Icon Premium Campaign Drawer Variant 2` · `Icon Avatar Drawer Variant` |
| `Variant Deck_cardback_avatar` | 104 | 3 | `Deck Drawer` · `Cardback Drawer` · `Icon Avatar Drawer Variant` |
| `Variant Booster_avatar_cardback_title` | 113 | 4 | `Icon Container Drawer Variant` · `Cardback Drawer` · `Icon Avatar Drawer Variant` · `Title Drawer Horizontal Variant` |
| `Variant Premium_booster_title_avatarOrResource` | 116 | 5 | `Icon Container Drawer Variant` · `Title Drawer Horizontal Variant` · `Icon Avatar Drawer Variant` · `Icon Currency Drawer Variant` · `Icon Premium Campaign Drawer Variant` |
| `Booster_CardOrAltArt` | 120 | 3 | `Icon Container Drawer Variant` · `Card Drawer`**\*** · `Card Alternate Art Drawer` |
| `Variant Premium_Booster_avatar_cardback_title` | 126 | 5 | `Icon Container Drawer Variant` · `Cardback Drawer` · `Icon Avatar Drawer Variant` · `Title Drawer Horizontal Variant` · `Icon Premium Campaign Drawer Variant` |
| `Variant cardback_premiumOrAvatarOrResource_titleOrResource` | 138 | 6 | `Cardback Drawer` · `Title Drawer Horizontal Variant` · `Icon Avatar Drawer Variant` · `Icon Currency Drawer Variant` · `Icon Currency Drawer Variant (1)` · `Icon Premium Campaign Drawer Variant` |
| `Variant Premium_Booster_avatar_cardback_title_resource` | 151 | 7 | `Icon Container Drawer Variant` · `Cardback Drawer` · `Icon Avatar Drawer Variant` · `Title Drawer Horizontal Variant` · `Icon Premium Campaign Drawer Variant` · `Icon Currency Drawer Variant (1)` · `Icon Expansion Pass Premium Drawer Variant` |
| `Booster_CardOrAltArt_AvatarORTitle` | 184 | 6 | `Cardback Drawer` · `Icon Container Drawer Variant` · `Card Drawer`**\*** · `Card Alternate Art Drawer` · `Title Drawer Horizontal Variant` · `Icon Avatar Drawer Variant` |
| `Booster_CardOrAltArt_Cardback_Avatar_Title` | 184 | 6 | 同上（**矩形与旋转不同**，见下） |
| `Variant Expansion pass` | 195 | **7** | `Icon Avatar Drawer Variant`**\*** · `Icon Currency Drawer Variant` · `Card Drawer` · `Cardback Drawer` · `Avatar Border Drawer Shop Variant` · `Card Drawer (1)` · `Icon Expansion Pass Premium Drawer Variant Variant` · **另有根级 `AvatarHelpText`** |
| `Variant Single Item Type` | 204 | **10** | `Icon Container Drawer Variant` · `Title Drawer Horizontal Variant` · `Title Drawer Horizontal Variant (1)` · `Icon Avatar Drawer Variant` · `Icon Avatar Drawer Variant (1)` · `Icon Currency Drawer Variant` · `Icon Currency Drawer Variant (1)` · `Icon Currency Drawer Variant (2)` · `Cardback Drawer` · `Icon Premium Campaign Drawer Variant` |

**每一格的值**（矩形 / rot / act / 类）在源码表里逐条带注释；
`Booster_CardOrAltArt_AvatarORTitle` 与 `…_Cardback_Avatar_Title` **槽名/序完全相同、值完全不同**
（前者 `Cardback Drawer` x1=698.46 / 后者同样，但 `Icon Container Drawer Variant` 是 591.64 vs 550.87、
`Title Drawer Horizontal Variant` 是 684.30 vs 436.09）⇒ **「同名槽」不等于「同值」**，⛔ 别按名字去重。

**旋转是逐槽的**（`m_LocalRotation` 绕 z）：21 份里带旋转的槽 **63 个**（86 个里 63 个非 0），
角度从 **−10.38° 到 +11.85°**；
**逐份不同**（例：同一个 `Icon Currency Drawer Variant` 在 `Booster_avatar_resource` 是 −4.64°、
在 `2 Currencies` 是 −2.84°、在 `Booster + 2 Currencies` 是 +5.30°）⇒ 只能逐槽存。
另外 `Offer Price Discount Badge` 的 **17.17°** 是 **21/21 同值**（它在 `Purchase buttons` 里、不是抽屉槽）。

**6 个槽名是【本族独有】**（`OfferContainer.SlotTypes` 那张表里没有 ⇒ 类名只能本表自带）：
`Card Drawer (1)` · `Icon Currency Drawer Variant (2)` · `Icon Avatar Drawer Variant (1)` ·
`Icon Premium Campaign Drawer Variant 2` · `Avatar Border Drawer Shop Variant` ·
`Icon Expansion Pass Premium Drawer Variant Variant`。
其余 **80 个**槽名两边都有，且**类名逐个一致**（断言已互核，§七·⑤）。

---

## 五、状态 → 参数 表（哪些元素在什么条件下显示/隐藏）

**这一表是本件最重要的东西**（铁律 5·c）：同一个组件在 21 份里**逐份不同**，而**运行期还会再变**。
三列：**出厂（prefab）** · **缺报价时（我们的现状）** · **有报价时（原版行为 + 判据出处）**。

| 件 | 出厂（21 份实读） | **缺报价时（我们的做法）** | 有报价时（原版） | 判据 |
|---|---|---|---|---|
| `Timer` | **T (21/21)** | **关** | 计时活动 ⇒ 开 + `TimerDisplay.Initialize(endTime)`；非计时 ⇒ **关** | `SetTimer.c` / `SetEventData.c` |
| `Offer Badge` | **T (21/21)** | **关** | `GetLabel(offer,3)` 为空 ⇒ **关父件**；否则开 + 填字 | `SetBadgeText.c` |
| `Available Counter` | **T (21/21)** | **关** | 那一跳是**那颗 TMP 自己** `SetActive(可买?)`；字 = `$"Available: {max(0,n−used)}/{n}"` | `SetAvailableText.c` |
| `Artwork` | **T×16 / F×5**（见 §四·二） | **照出厂**（报价到达前原版一个字都不写） | `offer != null` ⇒ `SetActive(true)` + `foreground` 灌图 | `SetEventData.c` |
| `foreground` | **T×16 / F×5**（**与上一行不是同一批**） | **照出厂** | 同上（sprite = `LoadAsset(offer, 2)`，取不到 ⇒ `Image.enabled=false`） | `SetEventData.c` |
| 价签 `Button Text` | **F (21/21)** | **关**（照出厂） | 也不开（那是 `PriceDisplayButton` 的备用文本） | dump 21/21 |
| 价签 `Price Display/icon` | sprite **`<无图>` (21/21)** | 只建节点、不画 | `PriceDisplayButton.Setup(offer)` 灌货币图标 | dump 21/21 |
| `Offer Price Discount Badge` | **T (21/21)** | **开**（照出厂） | `offer` 那一支按报价的布尔位开关 + 填划线价 | `Refresh.c` |
| `WebShop Button` | **T (21/21)** | **开**（照出厂） | `Open()` 尾段 `WebShopOpenButton.OfferAllowsShow(offer 那一位)` 决定 | `Open.c` |
| 抽屉槽（86 个） | **逐份**（4 个出厂关：`Card Drawer`×3 + `Icon Avatar Drawer Variant`） | **照出厂 + 槽里不画**（`ItemDrawer.Draw` 第 ② 步） | `GeneralOfferPopupDrawer.DrawRewards` 按报价选池、**整池先全关**、命中那个再开 | `DrawRewards.c:112,363` |
| 抽屉里的装饰三层 | **全关** | 不画 | 按奖励状态叠：`TogglePremiumHighlight` / `SetEphemeralDisplay` / `SetConvertedItem` | `ItemDrawer.cs` 的 `Set*` 那条注释 |
| `AvatarHelpText`（根级） | 只有 `Expansion pass` 有 · 字串**空** | 建空节点 | 正文是 I2 词条（远端 CCD） | dump · §九② |

---

## 六、注册改动（`WindowsManager.cs`）

| 位置 | 内容 |
|---|---|
| `Shell/WindowsManager.cs:1111-1118`（hunk 1，+8 行） | `public const string PrefabRefBaseOfferPopup = "Base Offer Popup";` —— 挨着另外三条 `PrefabRef*`，注释写明「**变体不各占一个键**：复用键取母版名」 |
| `Shell/WindowsManager.cs:1184-1214`（hunk 2，+31 行） | `public static BaseOfferPopup OpenBaseOfferPopup(string variant = null, OfferContainer.Content? content = null)` —— 走 `OpenByRef(PrefabRefBaseOfferPopup, wm => BaseOfferPopup.Create(wm, variant, content))`，随后 `win.Show(variant, content)`（**复用那一支**要把变体/内容喂进去，因为 `TryOpen` 在 `StillOpen` 时照原版**早退**） |

🔴 **只注册了 `BaseOfferPopup` 一扇** —— L2/L3/L4 那六扇由各自写手加（本件**没有替他们写任何一行**）。

**`git diff --numstat`** ⇒ `WindowsManager.cs` **+39 / −0**（2 个 hunk）· `ShopScene.cs` **+245 / −0**（1 个 hunk）
⇒ **纯新增、没有翻行尾**（删 0 = 没有整篇重写）。

---

## 七、断言清单

### 七·0 **宿主怎么定的**（现读，不是猜）

`Editor/ShopScene.cs` —— 理由两条：
① **同族既成宿主**：`BoosterInfoPopup`（同族的商店详情弹窗）在该文件 **26 处**、
   `OfferContainer`（同族的商品容器）**63 处** ⇒ 商店族的弹窗断言就住在这里；
② **本批没人占**：`资料/普查产出_1013/批次计划_1013.md` **没有**把 `Editor/ShopScene.cs` 派给任何写手
   （波 7 的乙用 `ShopScene.Run`，但那条是 **A435 阶段 2**、不属本批）。
⚠️ **风险如实报**（§十·①）：L2/L4 的写手若也选它 ⇒ **会撞车**；本件那一节是**单独一个 hunk**，
合并成本低。

### 七·1 **66 处 `Check*` 调用点**（循环里那几条按份数展开 ⇒ 实际执行条数更多）

| 组 | 调用点 | 断什么 | 期望值来源（**都不是自证**） | 改坏法 |
|---|---|---|---|---|
| 头（窗口参数 + 前提） | 8 | `type=1` / `placement=15` / `closeOnESC=1` / `extra=1.2` · 注册键 · 根名 · 变体名 · 窗根 `lossyScale==1` · `window` 是 `RectTransform` | **21 份 MB 原文** + 量出来的 | `placement` 写成 10 ⇒ 红 |
| ① 母版骨架 | 16 | `window` 宽/高/中心 x/y（冻结字面量）· `Title`/`Category` 左沿+中心 y · `Title` 是真 TMP · 关闭钮中心 x/y + 两个孩子 | `menu_dump --relative` 现读的 px 字面量 | 改 `GeoSmall.Window` 任一分量 ⇒ 红（**断言不读 `GeoSmall`**） |
| ② no-data 分支 | 5 | `Timer`/`Offer Badge`/`Available Counter` **建出来了 + 建成关着** · 价签 `Button Text` 关 · 母版 0 槽 | 反编译 `Set*.c` + dump 21/21 | 把那三件改成「不建」⇒ 红 |
| ③ 出厂显隐 | 10 | 母版 (T,T) · `Booster_CardOrAltArt` (**F**,T) · `Big` 档 (T,**F**) · 换变体状态转移 | dump 逐份现读 | 两个开关合成一个 ⇒ **必有一条红** |
| ④ 两档几何 | 8 | 大档 `window` 1426.11×720.843 + 中心 x · 大档 `Title` 左沿 1120.00 · 两档中心同 960 | dump 现读 | 两档共用一档 ⇒ 红 |
| ⑤ 抽屉槽 + 表互核 | 12 | 10 个槽的**名字+兄弟序** · 两个**不同**的旋转 + 反例 · 第 1 槽中心 x/y · **86 槽 / 80 共有 / 6 独有 / 0 留空 / 共有名字类名 0 不符** | dump 逐槽现读 + `OfferContainer.SlotTypes`（**另一张独立的表**） | 序错 / 角度套同一个 / 抄错一个类名 ⇒ 红 |
| ⑥ 复用 / 关掉再开 | 7 | 同键+同变体 ⇒ **同一实例** 且 **内容不重建**（节点同对象）· 换变体 ⇒ **重建**（新对象）· 关过再开 ⇒ **新实例** · 新建的回到母版 | 原版 `automaticallyLoadedWindows` + `CloseWindowCO` | 删 `Show` 那道「没变⇒不重建」守卫 ⇒ 红 |

**另有一条「灭自证反例」**（在 ② 里）：同一棵树上 `Title` / `Preview` **必须是开着的** ——
否则「三件关着」那三条会被「整棵树都没建」蒙过去。

### 七·2 **派活必查行三条**（逐条自查结果）

| 检查 | 结果 |
|---|---|
| ① **断言自证 / 同义反复** | ✅ 期望值**全是冻结字面量**（`menu_dump` 现读的 px / 21 份 MB 原文 / 2 个反编译分支），⛔ **一处都不读** `BaseOfferPopup.GeoSmall` / `Variants[…]` / `Art*R` |
| ② **弱断言分不出两种状态** | ✅ 凡有「另一态」的地方**两态都断**：复用/新建 · 不重建/重建 · (T,T)/(F,T)/(T,F) · 小档/大档 · 两个不同角度 · 「三件关」/「Title 开」 |
| ③ **`!RectOfUnion` / 「一个 quad 都没有」式断言** | ✅ **一条都没有** —— 「关着」一律走 `activeSelf` 且**配了反例**（②那一条） |
| **灭自证**（防两边一起改回去） | ✅ 「不重建」用**节点对象同一性**（`ReferenceEquals(Title节点)`）—— 改坏实现必须同时改断言才能绿；「两档几何」用**两个档各自的冻结值**（改坏一档另一档会亮出差异） |
| 分层用**渲染队列**不是 z | ✅ `QShade=3089 < QHit=3099`，压暗命中区走公共件 `MenuDraw.ShadeHit`（它自带「`qShade >= qContentMin` 告警」）；`Absorb` 那一片也在同一不变量里 |
| 「新加一层就配一条该藏的时候藏住了吗」 | ✅ 三件 `SetActive(false)` 的件**逐条断**（②组） |
| 断言**落在正确的 `*Scene.Run`** | ✅ `ShopScene.Run`（§七·0） |

⚠️ **本节本次【没有跑】**（铁律 12：A 表清零前中途不跑 Unity 自检）——
按调度台的口径，**本节是否变红由同步点那次 `ShopScene.Run` 决定**。**如实记**，⛔ 不当「已验过」。

**建议怎么跑**（同步点）：`ShopScene.Run` 一条即可（**只动了一个宿主**）：耗时 ≈ **45 秒**。
⚠️ **本项目断言格式**：判绿红看「`=== 合计：N 通过 / M 失败 ===`」那一行（本宿主不用 `✗` 标记失败）。

---

## 八、缺的素材 / 要主对话跑的 Unity 腿（如实列）

| # | 缺什么 | 现在的处置 | 该谁做 |
|---|---|---|---|
| 1 | **`40k_shop_popup_info_bg`**（`Artwork/background` 的 prefab sprite）—— `Resources/Art/{ui_menu,ui_deck,ui}/` 三个目录**都没有** | 节点照建、**这一格不画** + 出声（`MissingArt`） | 主对话（`工具/import_original_art.py` 的 `MENU_IMAGES` **不在我白名单**） |
| 2 | **`40k_OfferBadge`**（`Offer Price Discount Badge` 的 sprite）—— **只躺在 `Art/原版/0_mainmenu/`**、没进 `Resources/` | 同上 | 同上 |
| 3 | **`ShopScene.Run` 那一跑**（本件所有断言的**唯一**鉴别力来源） | ⛔ 本件没跑（铁律 12） | 主对话（同步点，≈45 秒） |
| 4 | **真 Play 看旋转方向**（§九③ 那条静态推导） | 只到「推导成立」这一层 | 用户（`真Play待验清单`） |

---

## 九、没查清 / 没做的（⛔ 不许猜）

1. **`Descripton` 的运行期文本**：原版 `GetLabel(offer, 1)` 覆盖；本地没有报价 ⇒ **恒用 prefab 出厂原文**
   （它点名 `Sautekh` —— 那是**原版自己**印在那句占位文案里的）。**要换真文案只需改 `BuildTexts()` 一处**
   （同 `BoosterInfoPopup.DescFor` 那条先例）。
2. **`AvatarHelpText` 的正文**：原版是 **I2 词条**（`EverguildTextMeshPro` + `Localize`），
   **词条表在远端 CCD、本地一个 value 都没有** ⇒ 建**空**节点 + 出声（同 `BoosterInfoPopup.TipBody` 口径）。
   **没查清**：它对应的**词条键**是什么（该件在 prefab 里的 `mTerm` 我没读 —— 不属本件的判据链）。
3. **旋转的符号方向**：按「原版局部系与我们的局部系都是 Unity 标准 y 向上、绕 z 逆时针为正」推导
   ⇒ 同一个 `rot` 就是同一个朝向。⚠️ **本件没跑 Unity ⇒ 没有实拍复核**（文件头 ① 有完整推导）。
4. **`Preview` 钮的动作**：原版那颗字段的旧名是 `previewDebugButton`（`dump.cs` 的 `[FormerlySerializedAs]`）
   ⇒ 是**调试预览**；它的 `onClick` **挂在 prefab 的 UnityEvent 上**（`Open()` 里只拿它做非空判定）
   —— **那个 UnityEvent 指向谁、干什么，我没读**（不属本件的判据链）。我们的处置 = 建出来 + 点了出声。
5. **原版谁开它**：`ContainerService.OpenOfferContainer` 那一族（服务端链）—— **没有查到那一步的直接证据**
   （本件没查，⛔ 不编入口）。
6. **`ItemDrawer.Draw` 之后那三跳（premium / ephemeral / converted）本件没接** ——
   原版是「拿到报价之后」按奖励状态调的（`RewardWindow.Open` 那条链的判据在 `ItemDrawer.cs`）；
   本窗**没有报价** ⇒ 三跳都走不到。**要做**（铁律 11）：判据齐（`ItemDrawer.SetPremium/SetEphemeral/SetConverted`
   已经在），差的是「本窗的报价对象从哪来」—— 那是**数据源**问题，与 `OfferContainer` 同一格账。
7. **`Expansion pass` 那一份的第 8 个槽**：本件实读时该份**只有 7 个槽**（曾误加过一格，
   **已删**；`_tmp_view/wl1/check_table.py` 的 363 项对账就是防这类事的）。

---

## 十、顺手发现（⛔ 别自己顺手改）

1. 🔴 **`Editor/ShopScene.cs` 是多路写手的候选宿主** —— 本件的 66 处断言在**单独一个 hunk**（`+3629,245`），
   若 L2/L4 也选它，**请调度台按「一个文件一个写手」串行**（或合并）。⚠️ 我**没动**该文件其余任何一行。
2. 🔴 **`BoosterInfoPopup` 与本窗是同一棵树形的两个实例** —— 它的 `window` / 窗底 / 关闭钮 / `Artwork` /
   `Title` / `Category` / `Descripton` / `Purchase buttons` 的矩形与本窗**几乎逐位相同**，
   但它是**独立 prefab**（`Booster Info Popup`，节点数不同：本窗 37 / 它多一条保底进度条 + `Tooltip`）。
   ⇒ 「两扇窗能不能收口到一个共用骨架」是**一件值得裁的活**（本件按「不越白名单」**只如实报**）。
3. ⚠️ **`OfferContainer.SlotTypes` 与抽屉槽名对不上 6 格**（见 §四·三末）—— 那 6 个名字**只在本族的 prefab 里出现**。
   本件把它们**实读类名写进了自己的表**并**配了一条互核断言**；若将来要收口成一张表，**判据在**：
   `python 工具/menu_dump.py bundle_menus_assets_all "<变体名>" --depth 2 --relative`（抽屉槽那一行的组件列）。
4. ⚠️ **`MenuDraw.Absorb` 算出来的档 = `QHit − 1`**，本窗 `QHit = 3099` ⇒ **吸收层落在 3098**，
   而 `QBtn` 也是 3098（底图档）。**不冲突**（那一档上**没有**命中区：底图 quad 不带 `WindowButton`），
   但**将来谁把某颗钮的命中区放到 3089–3098** 就会与吸收层同档 ⇒ 症状是「点那颗钮有时什么都不发生」。
   `BoosterInfoPopup` 有同一个形状（它的 `QHit = QBase+9`、按钮底在 `QBase+7/+8`）。**如实报，⛔ 没改。**
5. ⚠️ **`Shell/WindowsManager.cs:1082-1087` 那段注释今天仍写着**「今天仓里仍是两份（`MainMenuRuntime._openByRef` …）
   —— **合并成一份要等调度台**」。本件**没有**动那一份（`MainMenuRuntime.cs` 在别的写手手里）。
   ⛔ 如实转述，**不是本件引入的**。
6. ⚠️ **原版 `BaseOfferPopup.Close()` 会 `Unload(assetGroup 2)` + `RemoveListener`**（两颗钮）；
   我们这边「关窗」走的还是 `SetActive(false)`（老账 A123），**没有资源卸载那一层**（本件**没做**、也没假装做）。

---

## 十一、类型检查结果

```
$ TMPDIR=/tmp/wf_wl1 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

**跑了 4 次**（每次都 `0/0`）：
① 只加 `BaseOfferPopup.cs` ② 加注册（`WindowsManager.cs`）③ 加断言（`ShopScene.cs`）
④ 修掉断言里 5 处 `CheckTrue` 当 `bool` 用的编译错之后。
⚠️ 全部 `0` —— **没有任何错误落在别人的文件上**（那一条「错误全集中在不是你负责的文件上 ⇒ 重跑 + 如实记」没用上）。

---

## 附：本件的可复现对账（下个会话可直接跑）

```bash
# ① 变体表 ↔ 现读，逐格对账（纯 python，不跑 Unity）
PYTHONIOENCODING=utf-8 python d:/4/_tmp_view/wl1/check_table.py
#   ⇒ 表里条数 21 · 真实 prefab 21 · 表里抽屉槽合计 86 · 对账项 363 · 不符 0

# ② 任意一份的整棵树
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Base Offer Popup" --depth 12 --relative --md
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "General Basic Offer Popup Variant Single Item Type" --depth 12 --relative --md

# ③ 窗口字段（21 份 MB 逐个）
#  筛法 = 同时含 availableCount + previewButton + badgeText（命中 21 份）
```
