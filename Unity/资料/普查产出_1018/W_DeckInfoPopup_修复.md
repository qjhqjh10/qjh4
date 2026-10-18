# W — `Deck info Popup` 视觉与层序修复（执行代理 · 2026-10-18）

> 只改了 **`Unity/MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs`** 一个文件（+241 / −48 行）。
> 判据 = `R_DeckInfoPopup_对账.md`（逐节点表 + 最该先改的 7 条）· `R_底纹红黑_根因.md` §五·1 · 用户实拍。
> 真机验收**没做**（用户口径：主对话重建 player 后拍）。

---

## 一、结论

**做完的（施工单 A–G 全部 + 主对话追加的两条）：**

| # | 项 | 状态 |
|---|---|---|
| A | 行底板三层（`40k_deck_cardlist_bg` / `…_rarityColorGradient` / `…_border`） | ✅ 三张全上，白色 tint（稀有度 tint 未核，见 §四） |
| B | 费用圈 40×40 → **64×64**、中心 (x+30.12, y+29)；`Cost` 文本框 48.82×49.66 同中心 | ✅ |
| C | 三颗钮文案走词条（`MenuDeck/HUD/Practice` · `…/MenuButtons/EditDeck` · `…/MenuButtons/Select`） | ✅ 三条键现查都在 `official_loc_13lang.json` 里；`SelectButtonText` 覆盖口保留 |
| D | `Game Mode Icon` / `Separator` **有图才画** + 隐藏时 Army/两行名左移 108.87 | ✅（classic 无图标 / skirmish 有；SO 不在本地 ⇒ 按实拍推，见 §四） |
| E | 督军名本地化 | ✅ 走 `CardText.Name(CardDef)` |
| F | `share in chat` → `share_in_chat` + 取不到图名出声 | ✅（顺带查出**五颗圆钮全空的真因**是撞档，见 §三·1） |
| G | 队列拆档 | ✅ 但**档数比施工单多 4 档**，见 §二·0 的理由 |
| 追加 1 | 行 `Card Name` 框 x+64.29→x+328.73 · 色 (.934,.934,.934)；`Count` 框宽 22.87、右沿 x+351.60、色 (.933,.933,.933) | ✅ |
| 追加 2 | `Warlord Image` 层序（红底之上 / Info Panel 之下）+ 拉伸 | ✅ 层序改了；**拉伸那一半改前就已经是对的**（见 §二·6） |

**没做 / 别当已复刻的：** 稀有度六档 tint（§四·1）· `GameModesIconCollection`（§四·2）· 报告第 7 条「按钮发灰」
（不在施工单 A–G 里；判据仍是 `SoftDisable`，见 §三·4）· 真机对比。

---

## 二、改动清单（`Shell/DeckInfoPopup.cs`，行号为改后）

### 0) 队列常量 `:169-204`（**与施工单给的那组号差两档，先看理由**）

改前 `QDI 3120 / QDIRow 3121 / QDIText 3122 / QDIHit 3123`（四档，三层挤在 3120）
→ 改后**一档一层、共十档**：

```
3120 QDI        压暗（+ 点窗外关窗的命中区）
3121 QDIBg      红底 UI_Deck_Information_Back（不透明 α254）
3122 QDIWarlord 督军立绘（红底之上、面板之下）
3123 QDIPanel   Info Panel（半透明 α0.52）
3124 QDIRow     行底 / 大钮底 / 圆钮底 / Game Mode·Army·Separator
3125 QDIRowArt  行 Rarity Gradient / 圆钮 Face
3126 QDIRowEdge 行 Background Border / 圆钮 Icon
3127 QDIRowIcon 行费用圈
3128 QDIText    全部文字
3129 QDIHit     全部命中区（吸收层 = QDIHit−1）
```

🔴 **为什么不照施工单的 `QDIText=3124 / QDIHit=3125`**：那一组把「行内三张图 + 费用圈」当成**同一层**，
而它们**互相重叠**（渐变压行底、边框压渐变、费用圈压在行底左端并上下各探出 3px）⇒ 同号仍是「谁盖谁随机」，
只是把撞档从面板搬到了行内。按**实际层数**顺延即可；整条梯子仍落在 **3120–3129**：
`QBase=QDIOver(3130)` 那一支占 **3130–3139**，仍严格低于 `PromptPopup`(3140+)，且高于
`DeckSelectionPopup`(3124–3129，主对话刚下沉过压暗层)。
⚠️ 撞档如实记（已写进代码注释）：3130–3139 与 `ImportDeckPopup.QImp..QImpHit`(3130–3133) 同号，两窗不同屏。

### 1) 行底板 `:1629-1645`
| | 改前 | 改后 |
|---|---|---|
| 底 | `Solid(1,1,1,0.08)` 满 360 宽 | `Nine("40k_deck_cardlist_bg", border (150,0,150,0), tex 318×54)` **x+20.10 → x+360**, y 0→58, `QDIRow` |
| 渐变 | — | `Img("40k_deck_cardlist_bg_rarityColorGradient")` **x+225.54 → x+360**, `QDIRowArt`（Simple） |
| 边框 | — | `Nine("40k_deck_cardlist_border", border (5,5,5,5), tex 11×11)` **x+22.74 → x+360**, `QDIRowEdge` |

三张图**先全上白**（原版 prefab 那三颗 `Image` 的 `m_Color` 就是 (1,1,1,1)）；稀有度 tint 未核。

### 2) 费用圈 + Cost 文本 `:1651-1652` / `:1685-1688`
- 费用圈：`x1+6 … x1+46`（40×40）→ **`x1−1.88 … x1+62.12` / `y1−3 … y1+61`（64×64）**，队列 `QDIRowIcon`。
- `Cost` 文本：矩形 `x1+6,y1+9 … x1+46,y1+49` → **中心 (x+30.12, y+29)、48.82 × 49.66**；`boxH` 47.88 → **49.66**。
  （47.88 与 49.66 都 > `Pragati` 行盒 47.37 ⇒ 字号都停在 50，画面同值；按报告口径统一成 49.66。）

### 3) 三颗大钮文案 `:1283-1296`
`string[] btnTerms = { "MenuDeck/HUD/Practice", "MenuDeck/MenuButtons/EditDeck", "MenuDeck/MenuButtons/Select" }`；
`labelText = (Select && SelectButtonProvided && 覆盖串非空) ? 覆盖串 : Loc.T(btnTerms[i])`。
`btns[]`（内部键）**没动** —— `Track("Btn:"+key)` / `OnButton(key)` 认它。

### 4) Game Mode Icon / Separator `:1152-1199`
- `bool showGmIcon = info.GameMode == (int)GameMode.Skirmish;`（= 13；`GameplayVariables.cs:51`）
- 有图标才建 `40k_gamemode_icon_skirmish`；**`Separator` 显隐随它**（隐藏态整颗不建）。
- `float ddShift = showGmIcon ? 0 : -108.87f;` → `Army Icon` 左右各 +ddShift（767.9/867.9 → 659.03/759.03），
  `Deck Name` / `Warlord Name` 只左沿 +ddShift、右沿不动（872.9 → 764.03）。
- 隐藏这一态**说一次**日志（`_gmIconHiddenLogged`，别每次 Build 刷屏）。

### 5) 督军名 `:1224-1226`
`wl != null ? wl.Name : …` → **`wl != null ? CardText.Name(wl) : …`**（`CurWarlord` 是 `RuleEngine.CardDef`
⇒ 命中 `CardText.Name(CardDef)` 那个重载，中英按语档）。

### 6) Warlord Image（调用点 `:1118`，注释 `:1100-1117`）
- 队列 `QDIRow` → **`QDIWarlord`(3122)**：红底(3121) 之上、面板(3123) 之下 = **原版根子件序**
  （`Menu Dark → Red Bg → **Warlord** → Deck Details → Buttons → Deck Options → Info Panel → Close`）。
- **拉伸那一半：改前就已经是对的，没动**。`keepAspect` 传的是 `!(方形)`，而 `CardArt.DeckHero` 这一族
  **63 张逐张量过全是 2048²**（本次现量）⇒ 方形 ⇒ `keepAspect=false` ⇒ 按 1108² 框拉满 = 原版 `m_PreserveAspect=0`。
  只有「立绘取不到、退回卡面插图（671×1024）」那条**我们自己的兜底**仍等比内接（原版没有这条路）。
- 🔴 **前提已核**：`Info Panel` 贴图 `UI_Deck_Information_submenu_Back` 中心像素 = `(27,3,5,**133**)`
  = **α0.52**（本次直接读 PNG 像素）；`MenuDraw.Nine` 的 `tint` 传 null ⇒ 一个颜色都不设，
  而本仓 `Shell/` 里**没有**任何窗口级 `ApplyTint` / `SetAlpha`（grep 过）⇒ 半透明原样保留
  ⇒ 立绘压在面板下**看得见、被压暗一半**，正是原版的样子。
  ⚠️ 账 `N100` 的「抬到 `QDIRow` 修好」是当时面板不透明的权宜；那时立绘其实是**掉到了不透明的红底之下**
  （红底左沿 134.50 之外那条露出来 = 用户看到的「只剩左下角一条」）。拆档后红底严格在立绘之下，不需要再压过面板。

### 7) 五颗圆钮 / 关窗钮（`F` + 拆档的连带）
`:1382-1406`（五颗）· `:1466-1484`（关窗钮）：
- `"40k_general_bt_yellow_share in chat"` → **`"40k_general_bt_yellow_share_in_chat"`**（工程文件名带下划线）。
- 🔴 **三层拆档**：`Bg` → `QDIRow` · `Face` → `QDIRowArt` · `Icon` → `QDIRowEdge`
  （关窗钮同）。改前三层**全在 `QDIRow`** —— 这才是「五颗只剩金环、脸和字都没了」的真因（见 §三·1）。
- 新增「取不到图」出声：`Face` / `Icon` / `Close Face` / `Close Icon` 各打一条 `[DeckInfo] 图取不到：…`（带图名）。

### 8) `ReassertButtonQueues` `:912-924` + 新表 `_wbTargetQ` `:621-628`
变灰（`RefreshGray`）换材质会把队列打回 shader 的 3000，所以那里要重钉。原来**硬写 `Q(QDIRow)`** ——
拆档后会把圆钮 `Face` 从 3125 打回 3124、**与 `Bg` 重新撞档**。
⇒ 新增 `Dictionary<string,int> _wbTargetQ`（建窗时记「这颗钮可见那张的档」，`Build()` 里一起 `Clear`），
`ReassertButtonQueues` 按它重钉。

### 9) 行文字（主对话追加 1）`:1699-1703` / `:1718-1723`
- `Card Name`：`x1+52 … x1+RowW−52`（52→308）→ **`x1+64.29 … x1+328.73`**；色白 → **(.934,.934,.934)**（`Label.SetColor`）。
- `Count`：`x1+RowW−52 … x1+RowW−8`（308→352，44 宽）→ **`x1+328.73 … x1+351.60`（22.87 宽）**；色白 → **(.933,.933,.933)**。
  字号 fs38 / fs32 没动。

### 10) 顺带订正的过期注释
`:404`（`QDIHit` 3123→3129）· `:859`（红底/钮的档号）· `:1002`（选卡组弹窗 3125–3129 → 3124–3129）·
`:1049`（`QDIHit` 3123→3129）· `:1067`（标注那段描述的是「改前」）。

---

## 三、顺手发现（都没越界改）

1. 🔴 **「五颗圆钮全空 / 关窗钮没脸」的真因 = 撞档，不是丢图。**
   `Bg`(74.386²) / `Face`(56.86²) / `Icon`(56.86²) 三层**全在 `QDIRow`**、z 全 0 ⇒ 尺寸大的 `Bg` 有一半概率落在最上面。
   已按 §二·7 拆档修掉。⚠️ **未真机验证**（本轮不跑 Unity），判据 = 「同队列同 z ⇒ 排序退化」+ 实拍只见金环。
2. **四张「本轮以为缺」的图其实都在**（`CardArt.MenuUi` 三段兜底 ui_menu → ui_deck → ui）：
   `40k_general_bt_yellow` 在 `Resources/Art/ui/`，`…_duplicate` / `…_share` 在 `ui_deck/`，`Card_Frame_Cost_Icon` 在 `ui_deck/`。
   ⇒ 「图不在工程里」那句**不成立**，别据此去补图。
3. **`Deck Info` 抽屉里 `Cardback` 与 `CostCurveDrawer` 的柱子同档**（都在 `QDIRow` / `Q(QDIRow)`）——
   与本次根因同型，但那一页**出厂关着**、且不在本次施工单里 ⇒ **没动**。下一个动那个抽屉的人先看这条。
4. 报告第 7 条「三颗大钮发灰」是 `SoftDisable` 的**预期行为**（`Edit Deck` 恒灰），
   而 `Practice Deck` 在实拍里也是灰的 —— 那取决于 `CanImportDeck` / `DeckRules.Validate`，
   **不在本次 A–G 里，没动**。
5. 🔴 **`Editor/CollectionScene.cs` 有 6 处断言会因本轮改动而红**（文件在白名单外，**没动**，请调度台另派）：
   - `:2422` `Army Icon` 期望 `767.9..867.9` → 隐藏态实际 **659.03..759.03**
   - `:2424` `Game Mode Separator` 期望存在 → **classic 下整颗不建**
   - `:2435` `Deck Name` 左缘期望 `872.9` → 隐藏态 **764.03**
   - `:2440` `Warlord Name` 左缘期望 `872.9` → 隐藏态 **764.03**
   - `:2444-2450` `Game Mode Icon` 期望存在且 = `40k_gamemode_icon_classic` → **classic 下不建**
   - `:1917-1918` `Warlord Name` 文本期望 `wlc.Name`（英文）→ 现在印 **`CardText.Name(wlc)`**
   （夹具 `CollectionData.DeckAt(0)` 是 **classic**，所以后 4 条一定红；若想让它们继续断原值，
   请把期望改成「按 `GameMode == 13` 分支」的两态断言 —— 那才是本轮语义。）
   队列类断言（`:1967-1990` 的「≤ 红底」）**不受影响**：红底 3121，钮 3124/3125 仍在上。

---

## 四、没查清 / 判不准（⛔ 别当已复刻）

1. **行底稀有度六档 tint**：`40k_deck_cardlist_border` / `…_rarityColorGradient` 按稀有度该染什么色
   **只核到图名与九宫**（`R_底纹红黑_根因.md` §六末条同此）。⇒ 三张**先全上白**，代码注释写明「tint 待核」。
2. **`GameModesIconCollection` SO 本地不存在**（`assets_full` 全盘 grep `gameModesIcon` = 0 命中）；
   只坐实**机制**（无匹配 ⇒ `SetActive(false)`）与两个 sprite 名。
   ⇒ 「classic 无图标 / skirmish 有」是**按用户实拍 + 机制推的**（不是查表）。实拍模板匹配：
   阵营图标 corr 0.88、`gamemode_classic` corr ≈0.2。代码里已注明「等 SO 到手把 `showGmIcon` 换成真查表」。
3. **`Info Panel` 半透明只在像素层核过，没在屏幕上核过**：PNG 中心 α133 是死的、代码里也没有改色的地方，
   但「渲染出来确实是 0.52」要等真机。若真机上立绘被面板**整块**吃黑，那就是这一条的前提不成立
   （第一嫌疑是 `ImageQuad` 的混合模式，不是本次改动）。
4. **classic 之外的其它 PlayMode**（Draft / Ranked / Campaign…）该不该有图标：不可知，今天一律按 classic 处理（隐藏）。
5. `Card Name` 右沿从 `x+308` 挪到 `x+328.73` 后，**长卡名会不会撞到 `Count`**（`x+328.73` 起）：
   左右沿现在是**相接**的（原版如此）。我们的自适应会在 264.44 宽里缩字号，理论上不溢出，**没渲图核过**。

---

## 五、typecheck 原文（最后一次，全部改完之后）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

命令：`TMPDIR=/tmp/wf_f1 bash d:/4/Unity/工具/typecheck.sh`
