# R_底纹红黑_根因（2026-10-18 · 只读普查）

> 只读代理产出（本 agent 无写文件工具 ⇒ 由主对话原样落盘）。
> 所有颜色均由 python 直接读 PNG / 实拍采样得出，非猜测。

## 一句话根因

1. **`DeckInfoPopup` 把「黑压暗层 / 红底 / `Info Panel`」三层压在同一个渲染队列 `QDI`=3120，且 z 都是 0**
   （`Shell/DeckInfoPopup.cs:991`、`:1006`、`:1128`）。同队列同 z 时 Unity 按「每个 quad 到相机的距离」排序
   ⇒ 三层先后不确定，甚至同一层的九宫块互相穿插。四张同窗实拍出现四种次序：
   `Info Panel` 被红底盖住（03）、黑压暗层反盖红底（06）、中段/右段穿插（04）、正确（05）。
2. **次因（不是"发黑"，是"不发红"）**：卡组行底是 `Solid(白 α0.08)`（`DeckInfoPopup.cs:1477`），
   线性空间下渲染成中性灰 `#515050`；原版是 `40k_deck_cardlist_bg`（`#0C0C0A` α254）+
   `40k_deck_cardlist_border`（稀有度上色）。

## 一、原版底图真值（python 读 PNG，非猜）

`UI_Deck_Information_Back`：**1100×701**，border `(42,363,655,81)`，**整图 mean `#30130D`**
（premult `#210609`，alpha 均值 0.908）。

| 采样点 | #RRGGBB | α |
|---|---|---|
| TL(55,55) | `#110306` | 255 |
| TR(1044,55) | `#46060B` | 255 |
| BL(55,645) | `#0B0001` | 255 |
| BR(1044,645) | `#080804` | 255 |
| 中心 | `#20050B` | 254 |
| 左中(60,350) | `#120508` | 255 |
| 右中(1040,350) | `#3F060B` | 254 |
| 上中(550,40) | `#3D060A` | 254 |

⇒ **贴图本身就是「左/中近黑、右亮」**（九宫右带 655px 1:1 落屏，中带 403px 拉成 1008px）。
同类底图：`UI_Deck_Information_submenu_Back` 69×63、border 18、中心 `#1B0305` **α133(0.52)**；
`Menu Dark Background` 纯黑 **α0.773**；`Generic Window Red Background Big` 的 Image `m_Color=(1,1,1,1)`、
无材质（MB `5827096351411693563`），`Info Panel` 同样 `m_Color=(1,1,1,1)`（MB `8341120595536284072`）。
参考图里「整块暗红」正是这层 α0.52 的 `Info Panel` 把近黑中段提亮/压平出来的：
模型（Info 叠在红底上）(700,224)=`#130202` vs 参考 `#190406`；(1700,224)=`#340407` vs 参考 `#330806`。

## 二、逐层对账

| 窗 / 层 | 原版值（采样 #RRGGBB） | 我们值（文件:行号 + 常量） | 差 |
|---|---|---|---|
| 红底 `UI_Deck_Information_Back` 本身 | 右带 `#30090B`、中带 `#140409` | `DeckInfoPopup.cs:1005-1006`，同图 `Resources/Art/ui_deck/` | **0**：贴图(900,300)=`#3A060A` → 实拍(2186,801)=`#3A060A`，比值 1.00 ⇒ 不是 tint/材质吃掉红 |
| `Info Panel`（`submenu_Back` α0.52） | 参考图 (700,224)=`#190406`、(1700,224)=`#330806` | `DeckInfoPopup.cs:1128` `Nine(..., QDI, "Info Panel")` | 03=`#070101`/`#48070B`（整层被盖）；04 只盖住中段；05=`#1F0105`/`#330305`（正确） |
| `Menu Dark Background`(0,0,0,.773) | 应在红底之下 | `DeckInfoPopup.cs:991` `Solid(..., QDI, …)` | 06 整块 `#43060B`→`#140101`（≈0.3×），即黑压暗层盖在红底上 |
| 行底 `Row Bg` | `40k_deck_cardlist_bg` `#0C0C0A` α254 + 红边；参考图行内 `#0E0205…#1F1A13` | `DeckInfoPopup.cs:1477` `Solid(白 α0.08, QDIRow)` | 实拍恒 `#515050` 中性灰（linear 下 8% 白） |
| 收藏/奖励/商店 内容底 | 参考图 (700,700)`#180104`、(1000,700)`#210104`、(1600,700)`#310304` | `MainMenuSubmenuWindow.cs:443` `Gradient(..., 188f)`（W2 已修） | 实拍 `#1A0104`/`#210105`/`#2E0205` ⇒ **一致** |
| `DeckSelectionPopup` 压暗+红底 | 同图 | `DeckSelectionPopup.cs:478` 与 `:495` **同为 QDs=3125** | 同型隐患（本轮无该窗实拍） |

## 三、实拍证据（同窗、同坐标 design(1700,500)）

- 03（14:45）：`#43060B` 红底全亮，Info Panel 不可见；
- 04（14:20）：x≤1100 有 Info Panel（(800,224)=`#16060A`），x≥1200 又被红底右带盖住（`#48070B`）——**同一帧内九宫块互相穿插**；
- 05（14:24）：Info Panel 盖住整块（`#1F0105…`）；
- 06（14:24）：整块被黑压暗 `#43060B→#140101`。
⇒ 层序非确定，实锤是队列撞档，不是颜色常量。

## 四、是不是全局性问题

**不是全局根因**。收藏窗实拍与参考图逐点吻合（上表最后一行），说明共享壳 `MainMenuSubmenuWindow`
的底纹链路目前是对的（W2 的 188° 修复没回退）。另外抽查 5 扇窗的队列都是「一层一档」：
`BoosterInfoPopup.cs:65-67`（3070/3071）、`BaseOfferPopup.cs:114-117`（3089/3090）、
`BattleLogPopup.cs:58-61`（3450/3451）、`CampaignRewardWindow.cs:213-214`（3110/3111）、
`SettingsWindow.cs:1318`（3130/3131）⇒ 不共享本根因。唯一同型的是 `DeckSelectionPopup`（见上表）。

## 五、最小修复方案（3 条）

1. **`Shell/DeckInfoPopup.cs:169` 拆档 + 两个调用点**：`QDI=3120`（压暗）· 新增 `QDIBg=3121`（红底）·
   `QDIPanel=3122`（Info Panel）· `QDIRow=3123` · `QDIText=3124` · `QDIHit=3125`；
   把 `:1006` 的 `QDI`→`QDIBg`、`:1128` 的 `QDI`→`QDIPanel`。
   影响：只本窗；`QDIOver=3130` 变成 3130–3135，仍低于 `PromptPopup`(3140+)；
   `Editor/CollectionScene.cs:3538/3632` 走常量自动跟随。
2. **`Shell/DeckSelectionPopup.cs:478/480`**：压暗改 `QDs - 1`（3124，或新增 `QDsShade`），红底留 `QDs`(3125)。
   影响：只该窗；与 `BaseEventPopup.QShade=3124` 撞号但两窗不同屏（仓内已有同类撞号注释）。
3. **（次因，可选）`Shell/DeckInfoPopup.cs:1477`**：行底改回原版 `Img("40k_deck_cardlist_bg")`（九宫 150/0/150/0）
   + `Img("40k_deck_cardlist_border")`（九宫 5/5/5/5，按稀有度 tint）。
   影响：只本窗卡组列表；两张图已在 `Resources/Art/ui_deck/`。

## 六、没查清

- 用户看到的那一帧是四种次序里的哪一种：层序本身非确定，单张截图无法反推运行时排序规则；
  只能确认「同队列同 z」是充分条件。
- 原版 `Info Panel` 节点是否还挂 `UIGradient`/`Mask`：只枚举了该 GO 的 3 个组件与 Image 字段
  （GO `Info Panel.json`、MB `8341120595536284072`），未全量扫组件表。
- `DeckSelectionPopup` 的同型隐患无实拍（提供的 shots 里没有该窗）。
- 行底 `40k_deck_cardlist_border` 的稀有度六档 tint 未逐档核（只核图名/九宫）。
- 搜过的位置/词：`assets_full/bundle_menus_assets_all/{GameObject,MonoBehaviour,RectTransform}`、
  `bundle_atlasindividual_assets_0_mainmenu/Texture2D`、`Sprite/UI_Deck_Information_Back*.json`、
  `UI_Deck_Information_submenu_Back*.json`、`Generic Window Red Background Big*.json`、`Info Panel.json`；
  仓内 `Shell/*.cs` 的 `QDI|QDs|Menu Dark Background|Generic Window Red Background Big`。
