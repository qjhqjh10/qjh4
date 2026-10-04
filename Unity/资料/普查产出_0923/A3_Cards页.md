# A3 — `Collection Menu Variant` / **Cards 页**（卡池图鉴）「层 × 参数」表

> 只读普查，2026-09-23。**未改任何工程文件、未跑 Unity。** 坐标系：绝对像素 · 1920×1080 · 左上原点 · y 向下（由 `RectTransform` 五元组机械算出）。窗根 pid = **`2716193042033795797`**。
> ⚠️ 数字都重算过：**上一版按 1920×1080 直接喂子树，把 `Content Area` 的 167.2/70.94 内缩漏了** ⇒ 视口会算成 1757×995。**正确视口 = 1589.7×924.06**（=`Content Area` 1752.8×1009.06 再减 163.1/85）。

---

## 一、页在窗根下的路径

```
Collection Menu Variant [2716193042033795797]
└ Content Area [8619238770403107541]
  ├ Background · Tab Buttons(4 个 tab 钮 + Shadow) · Shadow(1)[act=F]
  └ Tabs [-3296323895330151723]           ← 无组件，纯容器
    ├ Select Deck Tab   [-8946133251602193707]  act=F
    ├ ★ CardsTab        [-930198234167581995]   act=F   ← 本页
    ├ Cardback Tab      [-4885685338377493803]  act=T   ← **出厂激活的是这个**（不是 Cards）
    ├ Alternate Art Tab [-7382280184577334571]  act=F
    └ Shared            [6273146933576852181]  act=T（Close Button）
```
- 左栏 tab 钮 4 个（`Deck` / `Cards` / `CardBacks` / `Alternate Art`），**Cards 钮 GO pid = `8534546818437735125`**。
- 页脚本：`CardsTab` = **`CardCollectionTab : CollectionTab<RawCardScript>`**（字段 `ownedToggle` / `wildcards`）。

## 二、Cards 页逐节点表

| 路径 | 名字 | pid | rect[x,y,w,h] | sprite / 文字 | 锚点五元组 a / pv / pos / sd | 字号·色 | 出厂 act | 组件 |
|---|---|---|---|---|---|---|---|---|
| /Content Area | Content Area | 8619238770403107541 | 167.2,70.94,1752.8,1009.06 | — | (0,0)-(1,1) (.5,.5) (83.59,-35.47) (-167.2,-70.94) | — | T | — |
| ../Background | Background | 3567167793339228885 | 167.2,70.94,1752.8,1009.06 | **sprite=0** Sliced | (0,0)-(1,1) (0,.5) (0,0) (0,0) | 白 | T | Image |
| ../Tabs/CardsTab | CardsTab | -930198234167581995 | 167.2,70.94,1752.8,1009.06 | — | (0,0)-(1,1) (.5,.5) (0,0) (0,0) | — | **F** | CardCollectionTab |
| ../../Collection Display | Collection Display | -4264613399673117995 | 167.2,70.94,1752.8,1009.06 | — | 同上 | — | T | CardCollectionDisplay + EventRelay |
| ../../../Scroll View | Scroll View | -4620204337388920107 | **330.2,155.9,1589.7,924.06** | — | (0,0)-(1,1) (.5,.5) (81.53,-42.5) (-163.1,-85) | — | T | **RecyclableScrollRect** |
| ../../../../Viewport | Viewport | -1451385205046648107 | 330.2,155.9,1589.7,924.06 | `UIMask` Sliced | (0,0)-(1,1) (0,1) (0,0) (0,0) | **col a=0** | T | Image（**没有 Mask 组件**） |
| ../../../../../Content | Content | 5337514856272421589 | 330.2,155.9,1589.7,**300** | — | (0,1)-(1,1) (0,1) (0,0) (0,300) | — | T | **只有 RectTransform（见 §三）** |
| .../Empty Collection Warning | Empty Collection Warning | -8921315904893689131 | 135.2,70.94,1835,1009.06 | — | (0,0)-(1,1) (.5,.5) (-72.5,42.5) (245,85) | — | **F** | — |
| ...../Warning | Warning | -9176733857726078251 | 同上 | TMP「There are no cards in your collection for the selected filters」 | (0,0)-(1,1) (.5,.5) (0,0) (0,0) | **fs36** auto关(18-72) 白 hAlign=Right valign=Middle | T | TMP |
| ../../Card Filters | Card Filters | -8460121208602172715 | **0.2512,155.9,335.3,924.06** | `40k_main_tab_background` Simple | (0,0)-(0,1) (0,.5) (-166.9,-42.5) (335.3,-85) | 白 | T | **CollectionFilterController** + Image |
| ../../Reference Card Pointer | Reference Card Pointer | -3352757861266890027 | 402.3,236.4,193.8,45.09 | — | (.5,.5)-(.5,.5) (.5,.5) (-544.4,316.6) (193.8,45.09) | — | T | MB[2244189844683285205] |
| ../Header Filters | Header Filters | -4090297309531217195 | 167.2,70.94,1752.8,**85** | sprite=0/白 | (0,1)-(1,1) (.5,1) (0,0) (0,85) | 白 | T | Image（`m_Sprite` 空） |

> `CollectionDisplay` 出厂字段：`autoSelect=0` · `allowItemDrag=0` · `emptyWarning`→`Empty Collection Warning` · `filters`→`Card Filters` · `filterToggle`→`Filter Toggle` · `listView`→上面那个 `RecyclableScrollRect` · `referenceCardPoint`→`-1444448670045642027`（= 该节点的 **RectTransform** pid）· `wildcards`→`WIldcard Display`。

---

## 三、🔴 网格：**没有 `GridLayoutGroup`**，是代码摆的

### 3·1 事实
`Content`（`5337514856272421589`）**出厂 `m_Component` 只有 1 个 `RectTransform`**，`m_Children = []`。
⇒ 卡池的「列/格/间距」**不在预设里**，由 **`PolyAndCode.UI.RecyclableScrollRect`**（第三方回收滚动插件）在运行时算。
**同类组件在 bundle 里共 9 个实例、字段各不相同**（见 §3·4）—— 这正是铁律 5·c 说的「一个值 ≠ 全部情况」。

### 3·2 `ScrollRect` 子类字段原文（卡池那一个，`MonoBehaviour_-1327870198181403947.json`）
```
m_Content                     -7665346550864223531   m_Viewport  -1726725416529960235
m_Horizontal 0   m_Vertical 1   m_MovementType 1(Elastic)  m_Elasticity 0.1
m_Inertia 1   m_DecelerationRate 0.135   m_ScrollSensitivity 100.0
m_HorizontalScrollbar 0 / m_VerticalScrollbar 0   （两条 ScrollbarVisibility 都是 0=AutoHide）
--- 子类自有字段（同一份 JSON 尾部）---
IsGrid              1
PrototypeCell       -914581999851801256     ← 见 §四
SelfInitialize      0
Direction           0 (Vertical)
_segments           **4**      ← 死值，见 §3·5
_controlSegmentSize **1**
_spacingX           0.0        _spacingY  0.0
_useFixedCellSize   1
_cellWidth          **262.5**  _cellHeight **384.0**
_mobileSizeScale    **1.5**    _poolSize 0     Dragging 0
```

### 3·3 **「状态 → 参数」对照表**（同一套 prefab，两套数值）

| 状态 | 判据（谁在读） | 格宽×格高 | 列数 = `floor(content.w ÷ (格宽 + _spacingX))` | 间距 | 起点 |
|---|---|---|---|---|---|
| **A. 默认**（`GameStaticData.smallScreenUI = false`） | `_cellWidth` / `_cellHeight` **原值** | **262.5 × 384** | `floor(1589.7 / 262.5)` = **6** | 0 / 0 | 左上、**整体居中**；首格左边缘 = 330.2 + **7.5** = **337.7**，右边缘 1912.7 |
| **B. 开了「小屏 UI」**（`smallScreenUI = true`） | 同上 **× `_mobileSizeScale`(1.5)** | **393.75 × 576** | `floor(1589.7 / 393.75)` = **4** | 0 / 0 | 同上：`6×262.5 = 4×393.75 = 1575` ⇒ **居中外边距同为 7.5** |

- 行距 = `_spacingY + _cellHeight` = **384**（B 状态 **576**）；视口高 924.06 ⇒ **一屏 2.4 行**（2 满行 + 第 3 行露头）。
- **切换点（唯一一处，硬证据 = 直读机器码）**：私有 `RecyclableScrollRect.Initialize()`，
  RVA `0x89F460` / VA `0x18089F460`：
  ```
  movss xmm7,[rbx+0x164]      ; _cellWidth
  cmp   byte[rax+0x11c],0     ; GameStaticData.smallScreenUI   （静态字段，dump.cs:119489）
  mulss xmm7,[rbx+0x16c]      ; × _mobileSizeScale
  ...  xmm6 = [rbx+0x168]     ; _cellHeight，同样 × [rbx+0x16c]
  ```
  ⚠️ **这个方法的 `.c` 产物读不到** —— 反编译**按方法名存盘**，公开重载
  `Initialize(IRecyclableScrollRectDataSource)`（VA `0x18089F420`）**把同名私有方法的文件覆盖了**
  ⇒ 只 grep 文本会得到「`_mobileSizeScale` 全库零读取点」的**错误结论**。用 `工具/disasm_va.py` 才拿得到。
- 列数公式出处：`decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__ConfigureColumnNumber.c` 与
  `..._CreateCellPool.c:127-167`（同一段被**内联**了一份）：
  `_columns = floor(content.rect.width / (_cellWidth + _spacingX))`，**只取 `_cellWidth` 原值**。
  居中量 `_hSpacingOffset(0x58) = ((content.w − (spX+cellW)*cols) + spX) × 0.5`，
  常量 `0x1834b2bb4 = **0.5**`（`工具/read_literal.py` 读的）；格坐标
  `anchoredPosition = ( (spX+cellW)*col + 0x58 , 逐行 −(spY+cellH) )`，格 pivot/锚点 = (0,1)。

### 3·4 同 bundle 9 个实例（**按宿主认实例，别按字段值猜**）
| 宿主（父链） | 段 | 格 | 间距 | ×scale | 原型 |
|---|---|---|---|---|---|
| **Collection Menu Variant / CardsTab / Collection Display**（本页） | **4** | 262.5×384 | 0 | 1.5 | Collection Card |
| Collection Menu Variant / Alternate Art Tab | 4 | 262.5×384 | 0 | 1.5 | Collection Card |
| 卡组编辑菜单 / Card Display | 4 | 262.5×384 | 0 | 1.5 | Collection Card |
| **Deck Selection Popup / Deck Display** | 6 | 225×364.5 | 20 | 1.25 | -1941218197625697718 |
| Deck Selection Popup with Tabs / Deck Display | 6 | 225×364.5 | 20 | 1.25 | 同 |
| Deck Editing Menu / Cosmetic Display | 4 | 250×405 | 0 | 1.2 | 4971272959345466742 |
| Cardback Tab（本窗） | 5 | 250×405 | 0 | 1.2 | 同 |
| Select Deck Tab（本窗） | 5 | 225×364.5 | 20 | 1.25 | -7203563689306584020（pool 114） |
| Social / Alliances Tab | 非网格 | 1505×110 | 0/5 | 1.0 | 471114273799851884 |

### 3·5 `_segments = 4` 是**死值**
`_controlSegmentSize = 1` ⇒ `CreateCellPool` 每帧按宽度**重算列数**并覆盖该槽位；
且 `PolyAndCode.UI.RecyclableScrollRect__set_Segments.c` 在 **25,096 个反编译文件里 0 个调用点**
（`grep -rl set_Segments` 只命中它自己 + `UILineRenderer` 的同名不同属性）。

### 3·6 筛选栏里的 GridLayoutGroup（**这几个才是预设里真有的**）
| 宿主 | 字段原文 |
|---|---|
| Army Filter / Content | `cell 100×100` `spacing 7/0` `startCorner 0` `startAxis 0` `childAlign 0` `constraint 0(Flexible)` `count 2` `pad 14/0/0/0` |
| Rarity Filter / Content | 同上（`cell 100×100` `spacing 7/0` `pad 14/0/0/0`） |
| Cost Filter / Content | `cell 65×65` `spacing 15/20` `constraint 0` `count 2` `pad 15/0/0/0` |
| Header Filters / Filters | `cell 85×70` `spacing 5/0` `constraint 2(FixedColumnCount)` `count 1` `childAlign 3(MiddleLeft)` `pad 0` |
> Flexible 模式下实排 = `floor((335.3 − 14 + 7) ÷ 107) = **3 格/行**`（Army/Rarity）；
> Cost = `floor((335.3−15+15) ÷ 80) = 4 格/行`。

---

## 四、卡格 `Collection Card` 逐子件表

`PrototypeCell = -914581999851801256`（= 该 prefab 根的 RectTransform）→ `GameObject/Collection Card.json`（GO pid `-3932997974561655464`）。
**根 `sizeDelta = 350×512`**，锚点 `(0,1)-(0,1)`、pivot `(0,1)`。（⚠️ 运行时被 `set_sizeDelta(cell, _cellWidth,_cellHeight)` 改成 **262.5×384**。）

| 路径 | pid | 相对根 rect[x,y,w,h] | sprite / 文字 | 五元组 | 字号·色 | act | 组件 |
|---|---|---|---|---|---|---|---|
| 根 Collection Card | -3932997974561655464 | 0,0,**350,512** | — | (0,1)-(0,1) (0,1) (0,0) (350,512) | — | T | CollectionCard 等 3 个 MB |
| /Content | 7243044493727364440 | 0,0,350,512 | — | (0,0)-(1,1) (.5,.5) (0,0) (0,0) | — | T | MB[242087205420463448] |
| ../Counter | -5006247910548507304 | 100,460.97,**150,50.01** | `40K_main_deck_card counter` Simple **preserveAspect** | a(.286,.002)-(.714,.0997) pv(.5,**0**) (0,0) (0,0) | 白 | T | Image + CollectionCardCounter |
| ..../Text (TMP) | -4591212569639258792 | 100,477.72,150,30.25 | TMP **"x14"** | (0,0.06)-(1,0.665) (.5,.5) (0,0) (0,0) | **fs31.9** auto(7–32) 白 hAlign=**Right** | T | TMP |
| ../CardUI | -7807926136218454696 | 175,256,**0,0** | — | (0,0)-(1,1) (.5,.5) (0,0) (**-350,-512**) | **localScale = 150** | T | 6 个卡面 MB（`BasicCardUI`…） |
| ..../CreatedByText | -1041614623605983912 | 173.75,254.16,2.5,0.38 | TMP "Created by someone fancy" | (.5,.5)-(.5,.5) (.5,.5) (0,1.65) (2.5,0.38) | **fs2.45** auto(0.3–3) | **F** | TMP |
| ..../2DCard | 3916709029380617560 | 173.95,254.33,**2.093×3.331**（卡单位） | — | (.5,.5)-(.5,.5) (.5,.5) (0,0) (2.093,3.331) | scl 1 | T | 7 个 MB（3D 卡体） |
| ....../Front/…/Card Highlight And Shadow | 1851973423424927064 | 172.79,253.72,4.428,4.428 | **sprite=0** | (.5,.5)-(.5,.5) (.5,.5) (0,-0.01265) | — | T | Image |
| ....../Front/…/**CardImage** | 8415367752976823640 | 173.63,254.59,**2.748×2.748** | **sprite=0（运行时赋）** | (.5,.5)-(.5,.5) (.5,.5) (0,-0.044) | — | T | Image |
| ....../Front/…/**CardFrame** | 8025433799335146840 | 173.88,254.32,**2.245×3.257** | **sprite=0（运行时赋）** | (.5,.5)-(.5,.5) (.5,.5) (0,-0.03) | — | T | Image |
| ....../UI Collider | -8043715420289698472 | 174.05,254.57,1.893,2.891 | — | (0,0)-(1,1) (.5,.5) (0,-0.02) (-0.2,-0.44) | — | **F** | Collider+Mono |
| ....../Cardback Container | -5930532629846166184 | 174.5,255.5,1,1 | — | (.5,.5)-(.5,.5) (.5,.5) (0,0) (1,1) | — | **F** | 内含 `Cardback Shadow SDF` 2.921×3.812 + `Cardback` 2.174×3.136（均 sprite=0） |
| ..../Card Ready for level up | 229231744778958168 | 174.11,254.61,1.783,2.738 | `Card Ready For Level Up` Simple | (.5,.5)-(.5,.5) (.5,.5) (0,0.0168) | 白 | **F** | Image |
| ..../**New Card Badge** | 6136784773910492504 | 175.32,254.66,1.21,0.392 | `WF_Special offer_Value` **Sliced** | (0,.5)-(0,.5) (0,.5) (0.317,1.142) | **col(0.547,0.0876,0.0542,1)** 红 | T | Image |
| ....../Text | -3111381779724765864 | 118.32,244.14,115.15,21.43 | TMP **"Новинка!"**(俄语，=New) | (.5,.5)-(.5,.5) (.5,.5) (-0.0293,0) | **fs27.7** auto关(18–72) **col(1,0.78,0,1)** hAlign=Center | T | TMP（`scl=0.01`） |
| ..../**Ban Icon** | -9134304855324756648 | 174.14,255.12,1.75,1.75 | `40k_Cross_icon_cross_big Banned card` Simple **preserveAspect** | (.5,.5)-(.5,.5) (.5,.5) (0.01655,0) | 白 | T | Image |
| ....../Banned Text | 7902654549289858392 | 174.32,255.82,1.4,0.35 | TMP **"Запрещено"**(俄语) | (0.1,0.4)-(0.9,0.6) (.5,.5) (0,0) | fs0.25 auto(0.25–72) 白 hAlign=Right | T | TMP |

⚠️ **两点必须照原样记住**：
1. **预置里的 `CardUI.localScale = 150` 与 `2DCard` 的「卡单位」**（2.093×3.331 ⇒ ×150 = **313.95×499.65 px**）。
   按预置根 350×512 算卡框 2.245×150 = **336.75 × 488.55 px**（publish 前 96%/95% 满格，正好合理）；
   但运行时格子被改成 **262.5×384** ⇒ **卡框（336.75）比格子（262.5）还宽 1.28 倍**。
   **本表只给「读到的字段」，这个尺寸冲突「还没查清」**（可能是 3D 卡体另有缩放入口，见 §六）。
2. **预置里没有「稀有度宝石 / 费用 / 攻防血」节点** —— 那几件由 `CardUI` 上那 6 个卡面脚本在运行时装配
   （已知 sprite PathID：Rarity `-3980338515175932349` → GO 名 `Card Rarity Sprite`；费用 `-3526939114648998153` → `Cost Background`/`Cost Image`；
   护甲 `-4840078238721171920` → 节点名 `Image`）。**静态子件只有上表这些。**

---

## 五、筛选栏各控件一表 + 万能卡计数条

### 5·1 左栏 `Card Filters`（`CollectionFilterController<RawCardScript>`）
出厂字段：`filters[6]` = 下面 6 个 · `filterIconHolder`=拼图容器 · `clearFiltersButton` ·
**`hiddenPosition = (-550, 0)`** · `animationTime = 0.3`（收起时整栏滑到 x = 0.25−550 = **−549.75**）。

内层：`Scroll View`(1722872106324583125, sens **50**, v=1) → `Viewport`（Image `UIMask` Sliced **+ `Mask showGraphic=0`**）
→ `Filters`（**VerticalLayoutGroup** spacing 0 pad 0，`ctrlW=1 ctrlH=0 forceW=1 rev=0`）+ 6 行：

| 行 | pid | 绝对 rect[x,y,w,h] | 内件（sprite / 字号） |
|---|---|---|---|
| **Name FIlter**（搜索框） | 1762897899400585941 | 0.25,155.9,335.3,79.02 | `Input Field` [27.4,175.4,**281.3,40**] `InputFieldBackground` Sliced col(0.0627,0,0,1)；`Text Area`[37.4,182.4,231.3,27] → `Placeholder` TMP **"Search" fs30** auto(18–30)、`Text` fs30；尾图标 `Image`[268.6,180.4,**35,30**] `40k_icon_search` preserveAspect |
| **Owned Toggle** | 5701668532015981269 | 0.25,**235**,335.3,50 | `Image`[239.9,234.9,**70.59,50**] `40_main_bt_toggle_on` preserveAspect（a(0.7,0)-(1,1) pv(1,.5) pos(-25,0) sd(-30,0)）；`Label` TMP **"Owned only" fs32** auto(18–32) hAlign=**Left**（⚠️ **2026-10-03 更正**：原写 `Center` **是错的** —— 逐颗复读 `Owned only`/`Upgradable only` 的 TMP **全包 8/8** 都是 `m_HorizontalAlignment=1`(Left)+`m_VerticalAlignment=512`(Middle)，**两扇窗都是 Left**；该行 rect 对、只有对齐这个字错）[25.3,y,209.7,50] |
| **Upgradable Toggle** | -3918645900876324139 | 0.25,**285**,335.3,50 | 同上，文字 **"Upgradable only"** |
| **Army Filter** | -5484509600942398763 | 0.25,**335**,335.3,150 | `Title` TMP **"Army" fs32**；`Content`[0.25,385,335.3,100] Grid 100×100 见 §3·6；每格 `Toggle` 100×100 → `Background`(sprite **0**，运行时赋阵营图) + `Checkmark`(act=**F**) |
| **Rarity FIlter** | -5393211807834578219 | 0.25,**485**,335.3,280 | `Title` TMP **"Rarity" fs32**；`Content`[0.25,550,335.3,215] Grid 100×100 → `Background` **50×50** `4_40k_cardframe_rarity_legendary` + `Checkmark`(F) + `Label` TMP 例 **"Legendary" fs23.2** auto(10–27) valign=Bottom |
| **Cost Filter** | -491017638383000875 | 0.25,**765**,335.3,230 | `Title` TMP **"Energy Cost" fs32**；`Content`[0.25,830,335.3,165] Grid 65×65 sp15/20 → `Background` **65×65** `Card Frame Cost Icon` + `Checkmark`(F) + `Label` TMP 例 **"0" fs45** auto(25–45) |
| **Type Filter** | 5254198941213057749 | 0.25,**995**,335.3,150 | `Title` TMP **"Type" fs32**；`Content` a(0,0)-(1,1) **HLG** spacing 0 pad 15/0/0/0 **align 6 = LowerLeft** → `Toggle` **80×100** → `Background` **50×50** **`40k_menu_search_icon_warlord`** + `Checkmark`(F) + `Label` TMP **"Warlord" fs23.2** |
> ⚠️ **「筛选战将」这个开关确实存在**（Type 行第 1 格 = `Warlord`，图标 `40k_menu_search_icon_warlord`），
> 但 `decomp_full/CardTypeFilter__ShowOnlyHero.c` 只做 `SetActive` + 遍历 toggle，**不碰布局、不调 ReloadData/Segments**
> ⇒ **筛选不改变列数**。见 §六·1。

### 5·2 顶栏 `Header Filters`
| 节点 | pid | 绝对 rect | sprite / 文字 |
|---|---|---|---|
| Header Filters | -4090297309531217195 | 167.2,70.94,1752.8,85 | Image（**`m_Sprite` 空**） |
| /Filter Toggle | 6452191585835344597 | 367.2,88.44,**50,50** | `40k_menu_bt` Sliced；Selectable trans=1；→ `label` TMP **"Filters" fs42** auto(10–42) [437.2,y,150,50]；`icon detail` **30×30** `40k_bt_icon_search` preserveAspect（a(0,0)-(1,1) sd(-20,-20)） |
| /Separator Line | 8187550541849223893 | 167.2,150.9,1752.8,10 | `40k_main_line` Sliced，a(0,0)-(1,0) sd(0,10) |
| /**Filters**（清筛选钮容器） | 1419059127639990997 | **592.2,70.94,0,85** | Grid 85×70 FixedColumnCount 1 + **ContentSizeFitter (HorizontalFit=1 MinSize, VerticalFit=0)**；`Clear Filter Button`[612.2,83.44,**250,60**] `UI_Button_Mulligan` Simple trans=2(SpriteSwap) + `Button Text` TMP **"Clear filters" fs42** hAlign=Right |
| /WIldcard Display（**万能卡计数条**） | -2045166553178774827 | **1470,70.94,400,85** | `WildcardDisplay`（字段 `commonCount/rareCount/epicCount/legendaryCount/armyIcon/shinyEffect`） |

⚠️ **「Filters」那个容器是「布局组跑之前的模板位」**（宽 0）、子件 `pos.x=20` 落在父容器**外** —— 正是 `CLAUDE.md` 记的那个坑。**别照抄 612.2**：运行时 `ContentSizeFitter` 会把它撑到 85 宽，按钮实际落在容器右边缘 **+20** 处。

### 5·3 万能卡计数条（`WIldcard Display`）4 格
| 节点 | pid | 绝对 rect | 内件 |
|---|---|---|---|
| Background | 5282456591777456853 | 1550,91.44,**320,44** | `40k_topmarquee_currency_display BW` Sliced，**col(0.462,0.462,0.462,1)**，a(0,.5)-(1,.5) pos(40,0) sd(-80,44) |
| Counters | 4439991795801318101 | 1555,91.44,315,44 | HLG spacing **5** pad 10/10/0/0 forceW=1 forceH=1 |
| ../**Common** | -9191298788520631595 | 1565,91.44,**65,44** | `Icon` **30×44** `40k_general_wildcard_common_small` preserveAspect；`Counter` **41×44** TMP **"999" fs32.6** auto(10–38) hAlign=Right valign=**Midline(8192)** |
| ../Rare | 778671689001329365 | 1640,91.44,65,44 | `40k_general_wildcard_rare_small`，其余同上 |
| ../Epic | 4301400409985966805 | 1715,91.44,65,44 | `40k_general_wildcard_epic_small` |
| ../Legendary | -8251865850643946795 | 1790,91.44,65,44 | `40k_general_wildcard_legendary_small` |
| ../Army Icon | -5970655423457926443 | 1470,70.94,**80,85** | `40k_DeckSelection_icon_FactionBlackLegion` preserveAspect（a(0,0)-(0,1) pv(0,.5) sd(80,0)） |
> 每格内部还有一层 HLG（spacing 0、`ctrlH=1`、forceW/H=1）。

### 5·4 计数条**显示什么**（语义，值得抄进来）
`CardCollectionDisplay.CheckFocusedArmy`（`decomp_full/CardCollectionDisplay__CheckFocusedArmy.c`）：
拿 `Reference Card Pointer` 的 rect 去 `OverlapsAny` 卡位，命中后
`WildcardDisplay.Initialize(card.army)` → `armyIcon = ArmyUtilities.GetArmyIcon(army)` + 刷新 4 个计数
⇒ **这 4 个数字 = 「指针当前悬停那张卡所属阵营、玩家手里 4 个稀有度各有几张万能卡」**（= 玩家库存量**直出**）。
> 🔴 **2026-09-26 更正**：本节原来写「**升到 4 档各需多少万能卡**」—— **错**，把它当成「升级代价表」了。
> 反编译方法体（`decomp_full/WildcardDisplay__SetCountersText.c`，VA 直读指令流复核）显示**没有任何换算**：
> `Inventory<Wildcard>.ItemCollection` → `.Where(wc => wc.Item.Army == currentArmy)` →
> `.ToDictionary(wc => wc.Item.Rarity)` → 对 `CardRarity` **1/2/3/4** 各取一次
> `CollectionExtensions.GetValueOrDefault(dict, key)` → `?.Quantity ?? 0` → `.ToString()` 写进 4 个 TMP。
> 即 `commonCount = 该阵营 Common 万能卡的 Quantity`，其余三档同理。**错因**：只看字段名 + `Initialize(army)` 的签名，
> 没读 `SetCountersText` 的方法体。完整证据（VA / `all_methods.txt` 解析 / 字段偏移）：
> `decomp_full/WildcardDisplay__SetCountersText.c:28-88` · `WildcardDisplay___SetCountersText_b__11_0.c:14` ·
> `WildcardDisplay.__c___SetCountersText_b__11_1.c:14` · `dump.cs:75778-75793`（字段偏移）·
> `dump.cs:94063-94070`（`IItemInfo` 槽 0 = `int Quantity`）· `dump.cs:18771,18774`（`cardRarity`@0x28 / `cardArmy`@0x2C）。

---

## 六、查不到的 / 冲突的

1. **「筛选战将 ⇒ 4 列」复现不出来**（与 `项目任务.md` §三·13·1 已定案一致）。硬证据：① 列数只在 `Initialize()` 算一次（`set_Segments` **0 调用点**）；② `CardTypeFilter__ShowOnlyHero.c` 只 `SetActive`+遍历；③ `CardCollectionDisplay__RefreshCells` 复用已有卡位。
   **本项目已有记录**：`项目任务.md` §三 第 4 条（原文已按铁律 6 搬出）（含同一段 `0x89F460` 反汇编）与 `资料/卡组编辑界面_查证_0920.md:167-168,312-323`。**本次独立复核：与其结论一致，无冲突。** ⇒ 4 列的成因是 **`GameStaticData.smallScreenUI`**（图形设置里的「小屏 UI」），**不是筛选**。
2. 🔴 **「卡框 336.75px 比格子 262.5px 宽 1.28 倍」—— 还没查清**。预置 `Collection Card` 根 350×512、`CardUI.localScale=150`、`CardFrame` 2.245×3.257 卡单位。按格子 262.5×384 反推，`CardUI` 的 scale 该是 ~112.5 而不是 150 ⇒ **要么另有缩放入口（`BasicCardUI`/`Card2DController`），要么 `_cellWidth/_cellHeight` 不是最终格子尺寸**。本次**只查了 `RecyclableScrollRect` 这一条链，没查卡面脚本**。
3. **`Content`（网格容器）的「出厂 `m_Children=[]`、`sd=(0,300)`」是模板位**，运行时由插件清空重填；
   **预设里查不到任何一次布局结果**（列数/首格坐标全是算出来的，不是抄的）。
4. **`_mobileSizeScale` 的读取点**在文本反编译里**永远查不到**（同名覆盖，见 §3·3 的红字）。
   ⇒ 以后凡涉及这个字段，**只能走 `工具/disasm_va.py`**。
5. **本地化**：预置里 `New Card Badge/Text` = 俄语 `"Новинка!"`、`Banned Text` = `"Запрещено"`；而 `WIldcard Display` 的计数是 `"999"` 占位、`Empty Collection Warning` 是英文。**原版客户端没有中文表**（`CLAUDE.md` §七 已记），别把俄语串当默认。
6. **`Shadow (1)`（窗根下，`1029317144425521877`）出厂 `act=F`**、rect 宽 −1871（异常大），疑为另一态残留，未深究。

---

## 七、跑过的命令

```bash
PY="D:/2/Warpforge_tools/py312/python.exe"
$PY "d:/4/_tmp_view/卡组线普查/_dump.py" 2716193042033795797 9    # 全窗（正确视口 1589.7×924.06）
$PY "d:/4/_tmp_view/卡组线普查/_dump.py" -930198234167581995 8    # CardsTab 子树
$PY "d:/4/_tmp_view/卡组线普查/_dump.py" -8460121208602172715 12   # Card Filters 全展开
$PY "d:/4/_tmp_view/卡组线普查/_anc.py"  -4620204337388920107 949882691574492964 -4009189795535134557 \
    -713997075716459137 -6923878454973042908 5949301113732634416 -6593819954181119275 \
    2508552813234086613 -6487742763938749739                       # 9 个 ScrollRect 认宿主
$PY "d:/4/Unity/工具/menu_dump.py" bundle_menus_assets_all -3932997974561655464 --depth 8
$PY "d:/4/Unity/工具/menu_rect.py" bundle_menus_assets_all -3932997974561655464 --depth 8 --relative
cat d:/2/tools/decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__{ConfigureColumnNumber,ConfigureCellSize,CreateCellPool}.c
cat d:/2/tools/decomp_full/PolyAndCode.UI.RecyclableScrollRect__set_Segments.c
$PY "d:/4/Unity/工具/disasm_va.py"    "D:/2/unity_run_ref/GameAssembly.dll" 0x18089F460 150
$PY "d:/4/Unity/工具/read_literal.py" "D:/2/unity_run_ref/GameAssembly.dll" 0x1834b2bb4 0x1834b2bb8
grep -n "smallScreenUI" d:/2/tools/il2cpp_out/dump.cs    # → 119489: public static bool smallScreenUI; // 0x11C
grep -n "class RecyclableScrollRect" -A 60 d:/2/tools/il2cpp_out/dump.cs          # 字段偏移表
# 另：枚举同 bundle 的 GridLayoutGroup / RecyclableScrollRect **全部实例**（内联 python，见 §3·4）
```
```

---

> ⚠️ **2026-10-03 更正**：本文件原引的 `项目任务.md:<行号>` **早已漂了**（且该文件于同日瘦身 **170 KB→79 KB**、行号全变），
> 已改按 **§ 编号** 引用（正本 §五 早就规定：**新写的文档一律写「§三 第 N 条」，不要再引用行号**）。
