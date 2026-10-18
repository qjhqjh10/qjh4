# R — `Deck info Popup` 逐节点对账（只读 · 2026-10-18）

> 只读代理产出（本 agent 无写文件工具 ⇒ 由主对话原样落盘）。
> 原版值 = 今日现核 `menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 4`、
> `_probe_deckinfo.py … --class DeckInfoPopup / --class AspectRatioFitter`、`DeckInfoPopup__Open.c`、`il2cpp_out/dump.cs`。
> 我们值 = `Unity/MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs`（下表简写 `DIP.cs`）。
> 参考图 1577×888 = 1920×1080 ×0.82135；模板匹配：阵营图标 SaimHann corr **0.88**、gamemode_classic corr ~0.2。

## 表（节点路径 | 原版值 | 我们值 | 差 | 严重度）

| 节点 | 原版值（现核） | 我们值（文件:行） | 差 | 级 |
|---|---|---|---|---|
| `.` 根 | 0,0→1920,1080 · anchors 0,0→1,1 · pivot .5,.5 · pos 0,0 · sizeΔ 0,0 · type=1/placement=15 | 运行时 GO + AttachToAnchor（DIP.cs:917-928），设计空间 1920×1080 | 结构不同，无序列化 anchors；视觉等价 | ⚪ |
| `Menu Dark Background` | sprite 无 · −1327.3,−746.2→3247.3,1826.2 · 色 (0,0,0,.773) · Simple · 带 BackgroundCloseButton | Solid :991 · 常量 :201-203 · ShadeHit :1002 | 一致 | ⚪ |
| `Generic Window Red Background Big` | `UI_Deck_Information_Back` 1100×701 · 九宫 L42,B363,R655,T81 · Sliced · 色白 · 134.5,82→1839.5,1032 | Nine :1005-1006 · :204,:372-373 | 一致 | ⚪ |
| `Deck Details` 容器 | 659.0,106.1→1329.84,217.3 · HLG spacing0 align MiddleLeft | 无容器，子件按「布局跑后」绝对摆 :1082-1124 | 视觉等价（仅双图标态） | ⚪ |
| `Deck Details/Game Mode Icon` | 100×110 @ 659,106.7→759,216.7 · sprite=0，运行期 `GameModesIconCollection.GetIcon(deck.GameMode)` · preserveAspect · **sprite==null 时整颗 SetActive(false)** | :1087-1088，纹理 `40k_gamemode_icon_{classic|skirmish}`（mode13→skirmish，**其余全 classic**），**恒画** | 原版可整颗隐藏（参考图就是隐藏态）；我们恒多一颗 | 🔴 |
| `Deck Details/Game Mode Separator` | 8.87×110 @759→767.9 · `40k_Generic Smooth line` 113×32 · 九宫55,15,55,15 · 色(.42,.157,.137,1) · ppuMul .76 · rot −10.60° · **act 随 Game Mode Icon** | :1095-1097 · :215-219 | 除「恒画」外一致 | 🟠 |
| `Deck Details/Deck Details/Army Icon` | 100×110 @767.9→867.9 · sprite=0，运行期 `ArmyUtilities.GetArmyIcon(deck.Army)`（=`40k_DeckSelection_icon_Faction*` 族；Aeldari=`_FactionSaimHann`） | :1098-1100 `DeckRuntime.FactionIcon` | 有阵营时一致；**Game Mode 隐藏时原版 HLG 会把它移到 659**，我们固定 767.9 | 🔴 |
| `…/Deck Name` | 485.6×54.5 @872.9,114.4→1358.5,168.9 · fs44.5 auto10-45 base42 · Left/**Bottom** · 折行0 · 白 | :1115-1117 · :225 | 一致（A1 旧表写 Center 是过期值） | ⚪ |
| `…/Warlord Name` | 487×50 @872.9,166.7→1359.9,216.7 · fs40 auto10-40 base38 · Left/**Middle** · 折行0 · 白 · **运行期=`GetLocalizedCardName(督军卡)`** | :1122-1124 用 `wl.Name`（英文） | 未本地化：截图 “Anvir Keltoc” vs 参考 “安维尔·凯尔托克” | 🔴 |
| `Buttons` 容器 | 669.3,885.8→1770.7,965.9 · HLG spacing36 align MiddleRight · **右距149.30 / 下距114.09** | :1132-1196 · :236 | 一致 | ⚪ |
| `Buttons/{Practice,Edit,Select} Deck` | `UI_Button_Mulligan` 410×124 · Simple · 色白 · 3×**324.5×80.1** @x 725.2/1085.7/1446.2,y885.8→965.9 | :1157-1158 | 几何/图一致；截图整块灰白=**Greyscale 材质**（disabled/soft-disable，:794/:800 + `WindowButton.RefreshGray`），不是换图（亮度相关 0.62 @精确 rect 967,1181） | 🟠 |
| `Buttons/*/Button Text` | fs40 auto10-40 base12 · Center/**Capline** · 折行0 · 白 · 框294.41×76.70 · prefab 文本=西语占位 · **Localize mTerm：`MenuDeck/HUD/Practice` · `MenuDeck/MenuButtons/EditDeck` · `MenuDeck/MenuButtons/Select`** | :1164-1186 写死英文 `Practice Deck`/`Edit Deck`；Select 用 selectButtonText | **未走词条**（截图英文）；字号/框/色一致 | 🔴 |
| `Deck Options` 容器 | 1263.74,93→1783.62,243 · HLG spacing**−50** align MiddleRight **reverse=1** · 右距136.38 | :1227-1296 · :243 | 一致 | ⚪ |
| 五颗圆钮 | 74.386×75.605,y130.2→205.81；左→右 **Delete 1333.33 · ShareOnChat 1427.31 · Share 1521.28 · Duplicate 1615.26 · Switch 1709.23**（步进93.976） | :1232-1245 | 一致（含倒排） | ⚪ |
| 圆钮三层 | Bg `UI_Button_Round_background` 237×237；Face `40k_general_bt_yellow` 71×71；Icon `40k_general_bt_yellow_{seedeck|duplicate|share|share in chat|delete}` 71×71；Face/Icon 56.86×58.13 @父+(8.151,7.978)；色全白；uGUI 序 Bg→Face→Icon | :1246-1260 · :245-276 | 名字/层序一致。**截图 5 颗全空**（只见金环；第4颗是 hover 高亮，hover 模板 corr 0.37、四个 icon 全负）；`share in chat` 带空格 vs 工程文件 `_share_in_chat.png` ⇒ **这张必 null** | 🔴 |
| `Info Panel` | `UI_Deck_Information_submenu_Back` 69×63 · 九宫18 · Sliced · 白 · 659,218.1→1799,868.1 | :1127-1128 · :277,:374-375 | 一致 | ⚪ |
| `Deck List/Content` | Grid cell **360×58** · spacing**(11,4.5)** · pad**(L15,R0,T15,B0)** · UpperLeft · StartAxis **Vertical** · Flexible ⇒ rowCap=floor((650−15+4.5)/62.5)=**10**、列数=ceil(n/10) | :1449-1474 · :290-292,:404-442 | 一致（14 张=2 列，截图同） | ⚪ |
| 行模板 | `Deck Selector Card Info button` 0×0（运行时实例化），Content 撑满 cell | Row_i 360×58 :1472-1477 | 尺寸等价 | ⚪ |
| 行 `Background` | `40k_deck_cardlist_bg` 318×54 · 九宫**150,0,150,0** · Sliced · 白 · **x+20.10→x+360**,y0→58 | Solid (1,1,1,0.08) 满 360 宽 :1476-1477 | **红底渐变条→半透明白灰**；左沿少 20.10；整块列表观感差 | 🔴 |
| 行 `Rarity Gradient` | `40k_deck_cardlist_bg_rarityColorGradient` 114×54 · Simple · 白 · x+225.54→x+360 | 无 | 缺一层 | 🟠 |
| 行 `Background Border` | `40k_deck_cardlist_border` 11×11 · 九宫5,5,5,5 · Sliced · 白 · x+22.74→x+360 | 无 | 缺边框 | 🟠 |
| 行 `Cost Image` | `Card Frame Cost Icon` 248×244 · Simple · ARF 高控宽 ratio**1.0** ⇒ cell 内 **64×64**，中心 **(x+30.12, y+29)** ⇒ x −1.88→62.12,y −3→61 | :1478-1479 40×40 @x+6→46,y+9→49 | **小 24px（37.5%）**，中心左 4.1，不再上下/左探出 | 🟠 |
| 行 `Cost` | fs50 auto18-50 base32 · Center/Capline · 折行1 · 白 · 框48.82×49.66 中心 x+30.12 | :1506-1508 框40×40 中心 x+26 | 字号一致；框小/中心左 4.1 | 🟡 |
| 行 `Card Name` | fs38 auto2-38 base27.69 · Left/Capline · 折行0 · 色**(.934,.934,.934)** · 框 x+64.29→x+328.73 | :1515-1517 框 x+52→x+308；色白 | 左沿 −12.3 / 右沿 −20.7 / 色偏白 6.6% | 🟡 |
| 行 `Count` | fs32 auto2-32 base36 · **Right/Midline** · 折行0 · 色**(.933,.933,.933)** · 22.87 宽，右沿 x+351.60 | :1528-1530 框 x+308→352；色白 | 右沿 −0.4（对）；框宽 44 vs 22.87；色偏白 | 🟡 |
| `Generic Close Button Orange` | 74.386×75.605 @1782.81,63.20→1857.19,138.80 · Bg `UI_Button_Round_background`+Face `40k_general_bt_yellow`+Icon `40k_general_bt_yellow_close` · 子件 1791.0,71.2→1847.8,129.3 · 白 · **右距62.81/上距63.20** | :1298-1331 · :293,:276 | 一致；截图 Face 未显（与五圆钮同症状） | 🟠 |
| `Warlord Image` | 1108² @−108.98,−33.99→999.02,1074 · sprite=0 运行期 full-hero · **preserveAspect=0（拉伸）** · 根子件序 2（在 Info Panel/Buttons/Deck Options/Close **之下**） | :1048-1049 抬到 QDIRow(3121) **高于 Info Panel(3120)**；非方图 keepAspect=true | 层序与拉伸语义偏离（文件内已记账）；观感影响小 | 🟠 |
| 兄弟序 | Menu Dark → Red Bg → Warlord → Deck Details → Buttons → Deck Options → Info Panel → Close | 压暗/hit→红底/absorb→warlord/hit→header→Info Panel→Rows→Buttons→Deck Options→Close | Info Panel 与 Buttons/Deck Options 互换；二者无重叠 ⇒ 无观感影响 | 🟡 |
| 图标是否被底板盖住 | 原版 `Deck Details`(3) 在 Buttons/Deck Options/Info Panel/Close **之下**，但底板都在它们之下；无「徽章被底板盖住」 | 图标/文字全 QDIRow/QDIText，面板 QDI ⇒ 不盖 | 层序无此迹象（五圆钮空圈不是这条能解释的） | ⚪ |

## 最该先改的 7 条（按视觉影响）

1. **卡列表行底板**：把 `DIP.cs:1476-1477` 的半透明白板换成 `40k_deck_cardlist_bg`（九宫 150,0,150,0，Sliced，白），
   **条左沿 = x+20.10、右沿 x+360**；再补 `40k_deck_cardlist_border`（x+22.74→x+360，九宫 5,5,5,5）与
   `rarityColorGradient`（x+225.54→x+360）。判据：行 cell (674+360c, 233.1+62.5r)。
2. **费用圈**：`DIP.cs:1478-1479` 改 **64×64、中心 (x+30.12, y+29)**（ARF 1:1），现在 40×40 @x+6..46
   —— 参考图里费用圈明显大于行文字。
3. **按钮文案走词条**：`DIP.cs:1164-1186` 三条 key = **`MenuDeck/HUD/Practice` / `MenuDeck/MenuButtons/EditDeck` /
   `MenuDeck/MenuButtons/Select`**（后两条在 `official_loc_13lang.json` idx 6642/6687，第一条 6717）。
4. **Game Mode Icon + Separator 按 `GetIcon != null` 显隐**（`DIP.cs:1087-1088` 现在恒画 classic）；
   隐藏时把 Army Icon 与两行名字整体**左移 108.87**（Army 767.9→659、Deck Name 872.9→764）
   —— 参考图正是这一态（单颗 SaimHann 徽记 + 标题左移）。
5. **军阀名本地化**：`DIP.cs:1122` 换 `CardText.Name(wl.Name, wl.NameZh)`（原版 `GetLocalizedCardName`）。
6. **五圆钮图标**：`DIP.cs:1237` 的 `"…share in chat"` 改工程名 `"…share_in_chat"`；
   其余四张（seedeck/duplicate/share/delete）文件都在，需用真机日志 `[DeckInfo] 图取不到：Icon …` 定位为何不画
   （只读无法起 Unity）。
7. **按钮灰不是贴图错**：`DIP.cs:794/:800` 的 `SoftDisable/Interactable` 把材质换成 `Everguild/UI/Greyscale`
   （观测：按钮区 0 个饱和像素；亮度模板 0.62 @精确 rect）。原版这副牌若合法，Practice 应显金色 `UI_Button_Mulligan`；
   Edit 因 `CanImportDeck` 恒 false 本来就该灰。

## 查不到 / 未证实（铁律 2）

- **`GameModesIconCollection` SO 本体查不到**：`assets_full` 全盘 grep `gameModesIcon` = **0 命中**；
  `bundle_armyicons_assets_all` 只有 Sprite/Texture2D、无 MonoBehaviour。⇒ 「哪些 PlayModes 有图标」无法枚举；
  只坐实了机制（`GetIcon(playMode)` 无匹配项返回 **0**，仅 playMode 为空值才走 `defaultIcon`）与两个 sprite 名
  （`40k_gamemode_icon_classic` / `_skirmish`，均在 `bundle_armyicons_assets_all/Sprite`）。我们的 `13→skirmish、其余→classic` 是自定映射。
- **五圆钮 Face/Icon 为何不显**：代码路径、层序、工程图都存在；只读下无 player 运行日志
  （`[DeckInfo] 图取不到` 是唯一判据）。已坐实的一条是 `share in chat` 文件名不符；其余未证实，不猜。
- **参考图与 bundle 的版本差**：参考图实际只显 1 颗徽记且标题左移，与「Game Mode Icon 隐藏 ⇒ HLG 重排」逐位吻合；
  但该图可能来自别的 build，结论以代码机制为准。
