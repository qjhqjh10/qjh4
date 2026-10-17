# W_T27 — 卡牌来源窗 `WhereToGetCardsPopup`（界面线 T18 最后一件）

日期：2026-10-17 · 执行代理（T27）· 白名单：`Shell/WhereToGetCardsPopup.cs`（新建）· `Shell/WindowsManager.cs`（注册）· `Editor/MainMenuScene.cs`（本件断言）

## 一、结论

- 建出 **`WhereToGetCardsPopup`（原版 `WhereToGetCardsWindow`）**：整窗（压暗层 / 面板 / 大标题 / 分隔线 / 滚动区 / 段模板 / 关窗钮）+ `WindowsManager.OpenWhereToGetCards` 注册。
- **无数据那一档如实留空**：段列表 0 段（只保留原版模板节点、按原版 `SetActive(false)`）+ `Debug.LogWarning` 一次，⛔ 不编来源。
- **本地没有生产入口**：原版由事件页两个 tab 的 `getCardsWindow` AssetReference 开（`ExpansionPassEventTab.cs:25` / `GachaEventTab.cs:20`），承载窗 `Parent Event Window` 我们没建 ⇒ **只建窗 + 注册，不伪造入口**（同 `TutorialMessagePopup` / `T26` 口径）。
- 秒级类型检查 **0 / 0**（原文见 §六）。本会话按口径**未跑** Unity 自检、未起玩家。

## 二、改了什么

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/WhereToGetCardsPopup.cs`（新建，约 560 行） | 整窗实现：窗参 / 节点树 / 矩形 / 字号 / 渐变 / 滚动 / 段模板 / 折叠 / 关窗 / 数据缺口出声；全套自检读口 |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | 加 `PrefabRefWhereToGetCards = "WhereToGetCardsPopup"` + `OpenWhereToGetCards(Context?)`（照 `OpenItemDisplay` 形状：`OpenByRef` + 显式 `Show`） |
| `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs` | 新增一节 `Section("T27 卡牌来源窗 …")`（插在 T26 那节之后、`=== 合计` 之前），共 56 条断言 |

未碰：`Shell/FTUESelectDeck*.cs` · `Shell/MenuScroll.cs` · `Shell/UIBakeScreen.cs` · `Shell/Ranked*.cs` · `Shell/GiveFeedbackPopup.cs` · `Core/**` · `Battle/**` · `Deck/**` · `Editor/{DeckScene,ShellScene,RewardsScene,CollectionScene}.cs` · `项目任务.md` · `CLAUDE.md` · `资料/任务线/**` · `d:/2/**` · git。

## 三、判据对照（逐值）

### 窗参（根 MB `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_6694678526659093809.json` 实读）

| 字段 | 原版 | 我们 |
|---|---|---|
| `type` | 1 (Popup) | `WindowType.Popup` |
| `windowsPlacement` | 15 (Popup) | `WindowsPlacement.Popup` |
| `closeOnESC` | 1 | `closeOnEsc = true` |
| `updateNavPanel` | 0 | `false` |
| `extraScaleSmallScreen` | 1.0 | `1f` |
| `useDefaultCloseSoundIfNull` | 1 | 默认 true（不赋 clip） |
| `openSound` / `closeSound` | 空 PPtr | 不赋值 ⇒ 关窗音走家族默认 `ButtonClick Back` |

### 节点 / 矩形（`menu_dump.py bundle_menus_assets_all "WhereToGetCardsPopup" --depth 11 --relative` 现读）

| 节点 | 原版（px） | 我们 |
|---|---|---|
| `Menu Dark Background` | 4574.6001×2572.3601 · (0,0,0,0.772549) · 中心 x=997（**不是 960**） | 同值，`ShadeTint` (0,0,0,0.773) |
| `Generic Window Red Background Big` | 1510.6368×990.6129 · `UI_Deck_Information_Back` 1100×701 · border (42,363,655,81) | 同值（`MenuDraw.Nine`） |
| `Title` | 1354.0997×109.57 · fs72 · base 36 · auto[18~72] · Center/Midline · 出厂字 `Where to get Beast Snaggas cards` | 同值（`TextBox` + `SetVAlign(Midline)`） |
| `TopBar` | 1354.10×6 · `40k_main_line` 171×6 · border (80,0,80,0) | 同值 |
| `Scroll View` | 1276.942×803.7156 · `h=0 v=1` · mode=1 Elastic · inertia 1 · elasticity 0.1 · decel 0.135 · **灵敏度 1.0** · 自身 `Image.m_Enabled=0`（不画） | `MenuScroll.TopAligned(..., 1.0f)` + `Elastic=true` + `Inertia=true`；底图不画 |
| `Viewport` | 同矩形 · `RectMask2D` padding 0 · softness (0,0) | `ViewportClip.Hang(..., Vector4.zero, Vector2Int.zero)` |
| `Content`（VLG） | x 248.9487..1637.0487（**1388.10**，比视口宽）· spacing 50 · pad (0,0,0,50) · CSF min 50 | 同值；高度按内容算、最小 50 |
| `Section For Reference` | 317.7523..1568.2451（**1250.4928**）· pad (0,0,2,5) · CSF min **59.496** | 模板节点同值、`SetActive(false)` |
| `Header` | 高 **52.496** | 同值 |
| `Header/Background` | `Image`(sprite=0) + `Gradient2`：type=0 横向 · blend=2 乘 · 2 色键 key0=(0.556863,0.110495,0.098039,1) · key1=(0,0,0,0) | `CardArt.Solid()` + `SetCornerColors(左→右)` |
| `Header/Title` | `Booster packs` · fs42 · Left/Midline · 宽 1219.69（框右沿超段右沿，原版如此） | 同值 + `AlignLeft` + `SetVAlign(Midline)` |
| `Header/Counter` | `5/5` · fs42 · Right/Baseline · 宽 1366.64 | 同值 + `AlignRight`；⚠️ `VAlign` 无 Baseline 档 ⇒ 居中（如实标注） |
| `Header/Toggle` | 42.2985×30.5648 · `EverguildToggle`：isOn=1 · onSprite `40K_dropdown_arrow_opened` · offSprite `40K_dropdown_arrow_closed` · onColor 白 · offColor (0.75,0.75,0.75,1) · onClickSound `ButtonClick` | 同值；点击 = 卡片区开/关 + 换图 + 换色，cue `ButtonClick` |
| 段内 `Content`（Grid） | cell 206.667×290.4 · spacing (25,35) · pad (0,0,40,0) · constraint=0 Flexible · 模板高 0 | 同值；列数按段宽算 = **5** |
| `Generic Close Button Orange` | 74.386×75.605 · 根图 `UI_Button_Round_background`（237×237 · PA · raycastTarget 0）· 子件同矩形 56.8636×58.1283（`40k_general_bt_yellow` / `..._close` · raycastTarget 1 · padding (-20)⁴） | 同值；命中区 = `PaddedRect(子件, (-20)⁴)` = **96.8636×98.1283** |

### 行为（第一权威 = `d:/2/tools/decomp_full/`，方法体都在）

- `WhereToGetCardsWindow__Open.c`：`GameWindow.Open()` → 有段则 `Reset()` → **`sectionReference.SetActive(false)`** → 标题 I2(context+0x10) → 逐段 clone + `Initialize`。本窗 `BuildSections` 逐句对位。
- `WhereToGetCardsSection__Initialize.c`：段标题 I2 / 逐卡引用 / 计数 `string.Format(格式串, 已拥有, 总数)` / `ToggleGreyScale(!owned)`。计数与「已拥有/总数」已对位；卡片格见 §四。
- `WhereToGetCardsSection__Awake/SetVisibility.c`：`toggle.onValueChanged` → 卡片区开/关。已对位（⚠️ 不重排，见 §七）。
- `WhereToGetCardsWindow__Close.c`：`base.Close()`（播关窗音）→ 逐段 `Release()`。我们走 `GameWindow.Close()`。

## 四、数据缺口（已查实，⛔ 不编）

- `CardsSpecialConfig.WhereToGetCardsData` 的属性体只有 `return *(this+0x28)`；全 `assets_full` 84 个 bundle 的 `MonoBehaviour/*.json` 逐字节搜 `CardsSpecialConfig` / `WhereToGetCardsData` **0 命中**（判据 = 本任务简报的现核结论）⇒ 「这张卡从哪些包/活动能拿到」的运行时数据**本地没有**。
- 处置：`Sections` 为空 ⇒ **0 段** + `Debug.LogWarning` 一次（`DataNote` 点名声明的两个关键词）。⛔ 不编来源、不拿别的表顶。
- 卡片格：原版是 `Card Drawer + ItemDrawerComponents`（ARF 高控宽 0.66），其卡片引用是 `LoadableReference<Card>`，本机**无解析源** ⇒ 只建到「卡片区容器」；若调用方真喂了卡片引用，本窗**出声**说「卡片格没画」。

## 五、断言（`Editor/MainMenuScene.cs` 的 T27 节，56 条）

1. 窗开得出来（`OpenWhereToGetCards`）。
2. 窗参 7 条（type / placement / closeOnESC / updateNavPanel / extraScaleSmallScreen / useDefaultCloseSoundIfNull / open+closeSound 空）。
3. `Menu Dark Background` 宽高 + tint (0,0,0,0.7725)。
4. 面板 1510.64×990.61 + 九宫贴图名 `UI_Deck_Information_Back`。
5. `Title` 文案（prefab 出厂字）+ 1354.10×109.57 + 自适应窗口 [18~72] + 实际字号 in range。
6. `TopBar` 1354.10×6 + 贴图名 `40k_main_line`。
7. `Scroll View` 1276.94×803.72 + **灵敏度 1.0** + Elastic + `RectMask2D` padding/softness 0 + Content 高 50 + `MaxOffset = 0`。
8. 段模板：在树里且**关着** + 1250.49 / 59.496 + Header 52.5 + `Header/Background` 顶点色（左暗红→右透明）+ Header 标题 `Booster packs` / fs42 + 计数 `5/5` + Toggle 42.30×30.56 + 出厂图 `40K_dropdown_arrow_opened` + onColor 白 + 段内 Grid 高 0。
9. **数据缺口**：`SectionsShown == 0` + `DataMissing` + `DataNote` 点名 `CardsSpecialConfig`。
10. 关窗钮 74.39×75.61 + 根图名 + 命中区 96.86×98.13。
11. **段克隆 + 折叠**（合成数据，只验模板机制）：1 段 / 段高 389.9 / 标题 / 计数 `3/5` / 卡片区初始开 / 点折叠 ⇒ 关 + 箭头换 `..._closed` + offColor。
12. 换回出厂内容 ⇒ 0 段。
13. 关窗 ⇒ `WindowSounds.LastCue == "ButtonClick Back"`。

## 六、typecheck 原文

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（命令：`bash d:/4/Unity/工具/typecheck.sh`）

## 七、没查清 / 已知偏离（如实标注）

1. **`Header/Counter` 的格式串读不到**：`WhereToGetCardsSection__Initialize.c` 里是 `System_String__Format(DAT_18426de28, 已拥有, 总数)`，那个 DAT 是元数据里的字符串 token ⇒ 字面量未解出。采用 prefab 出厂字 `5/5` 推得的 `{0}/{1}` 形式（**我们挑的形式**）。
2. **折叠不重排**：原版靠 VLG + CSF 收起后把下面的段向上挪；我们没有布局组、段高在 `BuildSections` 里一次算定 ⇒ 收起只开关卡片区（该区本来就按张数预留高度）。等卡片格真接上时一并补。
3. **点暗底 = 关窗**：原版 prefab 的 `BackgroundCloseButton.window` 是空 PPtr（严格照抄应「什么都不发生」）；本窗按同族弹窗（`ItemDisplayWindow` / `BattleLogPopup` / `T26`）的既有口径做成关窗 —— **我们挑的**，不是原版字段。
4. **`Counter` 垂直档**：原版 `m_VerticalAlignment = 4096`(Baseline)，`Label.VAlign` 没有这一档 ⇒ 保持默认居中。
5. **未跑 Unity 自检 / 未起玩家**（本会话口径）：断言只做了类型检查，**执行结果未知**；下一次「复刻收口」那一轮需要跑 `MainMenuScene.Run` 验这 56 条。
6. **原版入口链**只追到「两个事件 tab 持有 `AssetReferenceTyped<WhereToGetCardsWindow>`」；`Parent Event Window` 的 prefab / 打开时机**没在本轮查**（不在本件范围，也没有承载窗可接）。

## 八、可复跑命令

```
python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "WhereToGetCardsPopup" --depth 11 --relative
```
