
# 档案窗 · `Trophies Tab`（键名 `Trophies` / 文案 `Achievements` / 类 `AchievementsMenu`）· 原版普查

- 日期 **2026-09-27** · 只读普查（`资料/普查产出_0927/`，本文件是该页开工的**唯一尺子**）
- 正本骨架层 = `资料/阶段二_多人界面_原版规格.md` §2·1（**不重复**；页签按钮那条线见 `档案窗_BattleLog与页签按钮.md`）
- 表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt 8482975979751504434 --depth 8 --md`（**表体逐行抄录，未删行**）
- 页签根 = `GameObject/Trophies Tab.json`（RT pid `8482975979751504434`，GO pid `-1698997230529643982`，`m_IsActive=false`）
- 父链已复核：`Trophies Tab` 的 RT `m_Father` = `-5566042365552723406` = `Tab Content` ✅（与 §2·1 一致）
- 子树 **197 个节点**（自算，与 `菜单全树.md:16155–16351` 的 197 行**逐数吻合**）
- `python 工具/menu_dump.py --verify-layout` → `布局算法与正本 §2·1 的手算值逐位一致` ✅

---

## ⚠️ 本次踩到的三条工具陷阱（**先看这个再看表**）

> ✅ **2026-09-27 落盘时：坑 A 已修、本文件的表已用修好的工具重出**（主对话做）。
> `工具/menu_dump.py` 的 `align_on_axis` 纵轴公式原来是 `(2 - align//3)*0.5`（**反的**），
> 已改成 uGUI 的 `(align//3)*0.5`，并**把这一例子加成了 `--verify-layout` 的回归用例**
> （`Trophies.buttons` = VLG `align=0`，断 5 个 toggle 逐格贴组顶）。
> ⚠️ **它逃过了 §2·1 那条自检** —— `Tab Buttons` 的 `align=5`，两式**同值 0.5**，抓不到。
> ⇒ 下面「坑 A」的**说明与那张错值对照表仍然成立**（它讲的是原理与纠正过程），
> 但**本文件的表已经是正确值**，不用再手工改表。

### 坑 A：`buttons` 的五个分类页签 y 值**整体错了 +117.64**（工具把垂直对齐算反了）

- `buttons` 的 `VerticalLayoutGroup` 真值：`m_ChildAlignment = 0 (UpperLeft)` · `m_Padding = 0,0,0,0` · `m_Spacing = 20`
  · `childControlWidth=1` `childControlHeight=0` `childForceExpandWidth=1` `childForceExpandHeight=0`
  （出处 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_8349102416183261746.json`）
- Unity 原版算法 `GetStartOffset(axis=1)`：`alignmentOnAxis = ((int)m_ChildAlignment / 3) * 0.5f` ⇒ **UpperLeft = 0** ⇒ 子节点从父顶起排。
- 工具写的是 `align_on_axis(1, align) = (2 - align // 3) * 0.5`（`工具/menu_dump.py:272`-`274`）⇒ align=0 时算出 **1.0**，等于 Lower*。
- 后果：内容高 = 5×100 + 4×20 = **580**，父高 697.64 ⇒ 余 **117.64** 被**错加到顶部**。表里 5 个 toggle 的 y（348.28/468.28/…/828.28）**每个都多了 117.64**。
- **正确值 = 原始 JSON 里的值**（这棵子树是 UpperLeft + 无 padding ⇒ 布局跑完不动）：

| 槽位（= `m_Children` 序） | 节点名（`m_Name`） | 正确 `anchoredPosition` | 正确 rect（绝对 y） | 表里错值 |
|---|---|---|---|---|
| 1 | `Achievement Type Toggle`（文件 `Achievement Type Toggle_1330132040127904306.json`） | (142.055, **−50**) | 230.64→330.64 | 348.28→448.28 |
| 2 | `Achievement Type Toggle (1)` | (142.055, **−170**) | 350.64→450.64 | 468.28→568.28 |
| 3 | `Achievement Type Toggle (4)` | (142.055, **−290**) | 470.64→570.64 | 588.28→688.28 |
| 4 | `Achievement Type Toggle (2)` | (142.055, **−410**) | 590.64→690.64 | 708.28→808.28 |
| 5 | `Achievement Type Toggle (3)` | (142.055, **−530**) | 710.64→810.64 | 828.28→928.28 |

  ⚠️ 表的**行序是对的**（`buttons` 的 RT `880611429324913202`… 的 `m_Children` 序 = `[860611429324913202, 1809305899276401202, -421458122690889166, 8473533590962076210, -8255359254742533582]`，与表里的 5 行一一对应），**只有 y 要减 117.64**。
  ⚠️ `菜单全树.md:16158`-`16177` 那几个 y（231/351/471/591/711）**反而是对的**——它就是原始 JSON 的值。**这一处别迷信工具。**
- 为什么自检没抓到：§2·1 那个 `Tab Buttons` 的 `childForceExpandHeight=1` ⇒ `tot_flex>0` ⇒ 走 fmul 分支，**根本不经过这个函数**。⇒ 自检**覆盖不到**「UpperLeft + 子节点不撑满」这种情况。

### 坑 B：`ContainerHolder` 的 `GridLayoutGroup` 工具**根本没跑**（`[grid?]` = 早退）

- `工具/menu_dump.py:326`-`328`：见到 `m_CellSize` 就 `return 'grid?'` —— **直接原样返回原始 JSON 的模板位，一个字节都没算**。
- 本页**恰好没出事**：模板位 = 网格布局结果（预制体是「布局跑过之后存的」）。我手算复核过：
  列数 = ⌊(1111.82 + 10) / (520 + 10)⌋ = **2**；UpperCenter 横轴余量 (1111.82 − 1050) = 61.82 → 左内缩 **30.91**；
  `anchoredPosition.x` = 30.91 + c×530 + 260 → **290.91 / 820.91**；`anchoredPosition.y` = −(32 + r×160) − 75 → **−107 / −267 / −427 / −587 / −747 / −907**
  ⇒ 与表里 12 行**逐位一致**（`290.91,-107` / `820.91,-107` / `290.91,-267` …）。**这次的 grid 值可信，但它是「碰巧对」，不是算出来的。**
- 🔴 **`m_SizeDelta.y = 1014` 是「12 个子节点」时的烘焙值**（ContentSizeFitter `m_VerticalFit=1` 在编辑器里跑出来的）。
  换成就数就换高度：`H = 32(上) + 32(下) + ⌈N/2⌉ × 150 + (⌈N/2⌉ − 1) × 10`。**别把 1014 写死。**（N=12 ⇒ 1014 ✅）

### 坑 C：`字号=` 在 `auto[]` 存在时**不是渲染字号**

`工具/menu_dump.py:233`-`239`：`字号` 读 `m_fontSize`，`auto[]` 读 `m_enableAutoSizing`。本页**所有** TMP 都 `m_enableAutoSizing=1`
⇒ 表里的 `字号=12.0` / `字号=29.55` **都是编辑器最后一次烘焙值，不是运行时字号**（真值由文本 + `fontSizeMin/Max` 决定）。
`对齐=H/V` 是 **TMP 的 `HorizontalAlignmentOptions` / `VerticalAlignmentOptions` 枚举原值**（1=Left, 2=Center；512=Middle, 4096=Geometry, 8192=Capline）。
另：`Sliced (1,1,1,0)` 里的 `(1,1,1,0)` 是**颜色**不是九宫格；九宫格在**sprite 那一列**（`九宫18,18,18,18` 或 `否`）。这条与 `档案窗_BattleLog与页签按钮.md` §陷阱2 一致。

---

## A. 层 × 参数

### A·1 工具输出（**逐行抄录，未删行**）

```
# MonoScript 类名索引：5175 条
# （已沿 `m_Father` 爬父链：被查节点的父 = 「Tab Content」 351.03,118.92 → 1746.97,962.00）
# sprite 索引：真包 3316 条 + 切片缓存补齐 ⇒ 共 3316 条
# sprite 尺寸/九宫格索引：3200 条
| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 锚点 min→max | pivot | anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | 贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | Trophies Tab | 351.03,118.92→1746.97,962.00 | 1395.94×843.08 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | **F** | AchievementsMenu |  |  |  | 字段: categoryTogglePrefab,categoryToggles,containerPrefab,holder,pointsCounter |
| ·1 | bg | 635.16,213.16→1746.96,891.68 | 1111.80×678.52 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (142.06,-11.965) | (1111.8,678.52) | T | Image | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  |  |
| ·1 | buttons | 351.04,230.64→635.15,928.28 | 284.11×697.64 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-555.91,-38.999) | (284.11,697.64) | T | ToggleGroup,VerticalLayoutGroup |  |  |  | 字段: m_AllowSwitchOff ; spacing=20.0 align=0 pad=0,0,0,0 ctrlW=1 ctrlH=0 expandW=1 expandH=0 scaleW=0 scaleH=0 |
| ··2 | Achievement Type Toggle | 351.04,230.64→635.15,330.64 | 284.11×100.00 | (0,1)→(0,1) | (0.5,0.5) | (142.055,-50) | (284.11,100) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ···3 | button_bg | 351.04,231.43→635.15,331.43 | 284.11×100.00 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.796906) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,0.71) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  ; 字段:  |
| ···3 | Label | 415.59,260.64→570.59,300.64 | 155.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,0) | (155,40) | T |  |  |  |  |  |
| ····4 | Tab Toggle Title | 420.59,260.64→565.59,300.64 | 145.00×40.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-10,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Secret' 字号=35.0 auto[23.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ··2 | Achievement Type Toggle (1) | 351.04,350.64→635.15,450.64 | 284.11×100.00 | (0,1)→(0,1) | (0.5,0.5) | (142.055,-170) | (284.11,100) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ···3 | button_bg | 351.04,351.43→635.15,451.43 | 284.11×100.00 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.796906) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,0.71) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  ; 字段:  |
| ···3 | Label | 415.59,380.64→570.59,420.64 | 155.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,0) | (155,40) | T |  |  |  |  |  |
| ····4 | Tab Toggle Title | 420.59,380.64→565.59,420.64 | 145.00×40.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-10,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Secret' 字号=35.0 auto[23.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ··2 | Achievement Type Toggle (4) | 351.04,470.64→635.15,570.64 | 284.11×100.00 | (0,1)→(0,1) | (0.5,0.5) | (142.055,-290) | (284.11,100) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ···3 | button_bg | 351.04,471.43→635.15,571.43 | 284.11×100.00 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.796906) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,0.71) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  ; 字段:  |
| ···3 | Label | 415.59,500.64→570.59,540.64 | 155.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,0) | (155,40) | T |  |  |  |  |  |
| ····4 | Tab Toggle Title | 420.59,500.64→565.59,540.64 | 145.00×40.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-10,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Secret' 字号=35.0 auto[23.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ··2 | Achievement Type Toggle (2) | 351.04,590.64→635.15,690.64 | 284.11×100.00 | (0,1)→(0,1) | (0.5,0.5) | (142.055,-410) | (284.11,100) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ···3 | button_bg | 351.04,591.43→635.15,691.43 | 284.11×100.00 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.796906) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,0.71) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  ; 字段:  |
| ···3 | Label | 415.59,620.64→570.59,660.64 | 155.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,0) | (155,40) | T |  |  |  |  |  |
| ····4 | Tab Toggle Title | 420.59,620.64→565.59,660.64 | 145.00×40.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-10,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Secret' 字号=35.0 auto[23.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ··2 | Achievement Type Toggle (3) | 351.04,710.64→635.15,810.64 | 284.11×100.00 | (0,1)→(0,1) | (0.5,0.5) | (142.055,-530) | (284.11,100) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ···3 | button_bg | 351.04,711.43→635.15,811.43 | 284.11×100.00 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.796906) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,0.71) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  ; 字段:  |
| ···3 | Label | 415.59,740.64→570.59,780.64 | 155.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,0) | (155,40) | T |  |  |  |  |  |
| ····4 | Tab Toggle Title | 420.59,740.64→565.59,780.64 | 145.00×40.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-10,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Secret' 字号=35.0 auto[23.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ·1 | Scroll | 635.14,213.16→1746.98,891.69 | 1111.84×678.53 | (0,0)→(1,1) | (0.5,0.5) | (142.06,-11.966) | (-284.1,-164.55) | T | Image,ScrollRect | Background 32×32 九宫10,10,10,10 ppu=200 | Sliced (1,1,1,0) |  | h=0 v=1 mode=2 inertia=1 elasticity=0.10000000149011612 decel=0.13500000536441803 |
| ··2 | Viewport | 635.14,216.14→1746.98,891.69 | 1111.84×675.55 | (0,0)→(1,1) | (0,1) | (0,-2.98599) | (0,-2.986) | T | Image,RectMask2D | UIMask 32×32 九宫10,10,10,10 ppu=200 | Sliced (1,1,1,1) |  | 字段: m_Padding,m_Softness |
| ···3 | ContainerHolder | 635.14,200.27→1746.96,1214.27 | 1111.82×1014.00 | (0,1)→(1,1) | (0.5,1) | (-0.00805664,15.874) | (-0.016,1014) | T | ContainerHolder,ContentSizeFitter,GridLayoutGroup ⚠️grid? |  |  |  | 字段: maxAmount ; 字段: m_HorizontalFit,m_VerticalFit ; **【GridLayoutGroup】** cellSize=520×150 spacing={'x': 10.0, 'y': 10.0} pad=0,0,32,32 corner/axis=0/0 align=1 constraint=0(2) |
| ····4 | Achievement Container | 666.05,232.27→1186.05,382.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (290.91,-107) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 818.05,253.23→1177.01,285.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 818.05,285.93→1177.01,334.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1068.75,334.35→1177.01,363.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1040.22,329.54→1068.75,368.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 818.05,334.35→1038.74,363.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 818.05,326.09→1038.74,369.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 818.05,334.82→1038.74,361.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 818.05,336.77→1038.74,359.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 862.19,339.18→994.60,361.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 818.05,334.82→1038.74,361.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 681.05,242.27→811.05,372.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (1) | 1196.05,232.27→1716.05,382.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (820.91,-107) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 1348.05,253.23→1707.01,285.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 1348.05,285.93→1707.01,334.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1598.75,334.35→1707.01,363.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1570.22,329.54→1598.75,368.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 1348.05,334.35→1568.74,363.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 1348.05,326.09→1568.74,369.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 1348.05,334.82→1568.74,361.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 1348.05,336.77→1568.74,359.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 1392.19,339.18→1524.60,361.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 1348.05,334.82→1568.74,361.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 1211.05,242.27→1341.05,372.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (2) | 666.05,392.27→1186.05,542.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (290.91,-267) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 818.05,413.23→1177.01,445.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 818.05,445.93→1177.01,494.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1068.75,494.35→1177.01,523.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1040.22,489.54→1068.75,528.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 818.05,494.35→1038.74,523.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 818.05,486.09→1038.74,529.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 818.05,494.82→1038.74,521.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 818.05,496.77→1038.74,519.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 862.19,499.18→994.60,521.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 818.05,494.82→1038.74,521.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 681.05,402.27→811.05,532.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (3) | 1196.05,392.27→1716.05,542.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (820.91,-267) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 1348.05,413.23→1707.01,445.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 1348.05,445.93→1707.01,494.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1598.75,494.35→1707.01,523.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1570.22,489.54→1598.75,528.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 1348.05,494.35→1568.74,523.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 1348.05,486.09→1568.74,529.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 1348.05,494.82→1568.74,521.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 1348.05,496.77→1568.74,519.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 1392.19,499.18→1524.60,521.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 1348.05,494.82→1568.74,521.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 1211.05,402.27→1341.05,532.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (4) | 666.05,552.27→1186.05,702.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (290.91,-427) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 818.05,573.23→1177.01,605.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 818.05,605.93→1177.01,654.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1068.75,654.35→1177.01,683.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1040.22,649.54→1068.75,688.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 818.05,654.35→1038.74,683.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 818.05,646.09→1038.74,689.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 818.05,654.82→1038.74,681.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 818.05,656.77→1038.74,679.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 862.19,659.18→994.60,681.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 818.05,654.82→1038.74,681.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 681.05,562.27→811.05,692.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (5) | 1196.05,552.27→1716.05,702.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (820.91,-427) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 1348.05,573.23→1707.01,605.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 1348.05,605.93→1707.01,654.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1598.75,654.35→1707.01,683.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1570.22,649.54→1598.75,688.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 1348.05,654.35→1568.74,683.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 1348.05,646.09→1568.74,689.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 1348.05,654.82→1568.74,681.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 1348.05,656.77→1568.74,679.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 1392.19,659.18→1524.60,681.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 1348.05,654.82→1568.74,681.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 1211.05,562.27→1341.05,692.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (6) | 666.05,712.27→1186.05,862.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (290.91,-587) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 818.05,733.23→1177.01,765.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 818.05,765.93→1177.01,814.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1068.75,814.35→1177.01,843.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1040.22,809.54→1068.75,848.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 818.05,814.35→1038.74,843.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 818.05,806.09→1038.74,849.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 818.05,814.82→1038.74,841.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 818.05,816.77→1038.74,839.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 862.19,819.18→994.60,841.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 818.05,814.82→1038.74,841.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 681.05,722.27→811.05,852.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (7) | 1196.05,712.27→1716.05,862.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (820.91,-587) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 1348.05,733.23→1707.01,765.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 1348.05,765.93→1707.01,814.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1598.75,814.35→1707.01,843.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1570.22,809.54→1598.75,848.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 1348.05,814.35→1568.74,843.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 1348.05,806.09→1568.74,849.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 1348.05,814.82→1568.74,841.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 1348.05,816.77→1568.74,839.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 1392.19,819.18→1524.60,841.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 1348.05,814.82→1568.74,841.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 1211.05,722.27→1341.05,852.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (8) | 666.05,872.27→1186.05,1022.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (290.91,-747) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 818.05,893.23→1177.01,925.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 818.05,925.93→1177.01,974.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1068.75,974.35→1177.01,1003.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1040.22,969.54→1068.75,1008.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 818.05,974.35→1038.74,1003.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 818.05,966.09→1038.74,1009.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 818.05,974.82→1038.74,1001.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 818.05,976.77→1038.74,999.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 862.19,979.18→994.60,1001.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 818.05,974.82→1038.74,1001.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 681.05,882.27→811.05,1012.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (9) | 1196.05,872.27→1716.05,1022.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (820.91,-747) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 1348.05,893.23→1707.01,925.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 1348.05,925.93→1707.01,974.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1598.75,974.35→1707.01,1003.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1570.22,969.54→1598.75,1008.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 1348.05,974.35→1568.74,1003.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 1348.05,966.09→1568.74,1009.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 1348.05,974.82→1568.74,1001.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 1348.05,976.77→1568.74,999.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 1392.19,979.18→1524.60,1001.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 1348.05,974.82→1568.74,1001.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 1211.05,882.27→1341.05,1012.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (10) | 666.05,1032.27→1186.05,1182.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (290.91,-907) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 818.05,1053.23→1177.01,1085.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 818.05,1085.93→1177.01,1134.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1068.75,1134.35→1177.01,1163.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1040.22,1129.54→1068.75,1168.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 818.05,1134.35→1038.74,1163.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 818.05,1126.09→1038.74,1169.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 818.05,1134.82→1038.74,1161.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 818.05,1136.77→1038.74,1159.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 862.19,1139.18→994.60,1161.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 818.05,1134.82→1038.74,1161.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 681.05,1042.27→811.05,1172.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | Achievement Container (11) | 1196.05,1032.27→1716.05,1182.27 | 520.00×150.00 | (0,1)→(0,1) | (0.5,0.5) | (820.91,-907) | (520,150) | T | Image,AchievementContainer | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: backgroundButton,counter,description,image,reward,sliderBar,title |
| ·····5 | title | 1348.05,1053.23→1707.01,1085.92 | 358.96×32.69 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,37.691) | (-5.041,32.691) | T | EverguildTextMeshPro |  |  | 'Victorious 1/5' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | description | 1348.05,1085.93→1707.01,1134.35 | 358.96×48.42 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-2.867) | (-5.041,48.423) | T | EverguildTextMeshPro |  |  | 'Upgrade Ultramarines cards to tier 2' 字号=29.549999237060547 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | rewards | 1598.75,1134.35→1707.01,1163.81 | 108.26×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (246.7,-41.807) | (-255.741,29.457) | T | EverguildTextMeshPro |  |  | '2 points' 字号=12.0 auto[12.0~35.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | rewardIcon | 1570.22,1129.54→1598.75,1168.61 | 28.54×39.06 | (0,0.5)→(0,0.5) | (1,0.5) | (1.9073e-06,-4.76837e-07) | (28.536,39.065) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Progress | 1348.05,1134.35→1568.74,1163.81 | 220.68×29.46 | (0.3,0.5)→(1,0.5) | (0,0.5) | (-4,-41.807) | (-143.315,29.457) | T |  |  |  |  |  |
| ······6 | Slider | 1348.05,1126.09→1568.74,1169.73 | 220.68×43.64 | (0,0)→(1,1) | (0.5,1) | (0,8.2581) | (0,14.1818) | T | Slider,MissionProgressBarDisplay |  |  |  | 字段: m_AnimationTriggers,m_Colors,m_Direction,m_FillRect,m_HandleRect,m_Interactable,m_MaxValue,m_MinValue,m_Navigation,m_OnValueChanged,m_SpriteState,m_TargetGraphic,m_Transition,m_Value,m_WholeNumbers ; 字段: displayRule,progressSlider |
| ·······7 | Background | 1348.05,1134.82→1568.74,1161.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_bg 42×18 九宫20,0,20,0 | Sliced (0.299,0.289,0.689,1) |  | 字段:  ; showGraphic=1 |
| ········8 | Fill Area | 1348.05,1136.77→1568.74,1159.05 | 220.68×22.27 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,6.56498) | T |  |  |  |  |  |
| ·······7 | counter | 1392.19,1139.18→1524.60,1161.00 | 132.41×21.82 | (0.2,0.2)→(0.8,0.7) | (0,0.5) | (0,0) | (0,0) | T | EverguildTextMeshPro |  |  | '100/200' 字号=12.0 auto[12.0~35.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Outline | 1348.05,1134.82→1568.74,1161.00 | 220.68×26.18 | (0,0.2)→(1,0.8) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,Mask | 40k_campaign_bar_outline 46×22 九宫20,0,20,0 | Sliced (1,0.841,0,1) |  | 字段:  ; showGraphic=1 |
| ·····5 | Image | 1211.05,1042.27→1341.05,1172.27 | 130.00×130.00 | (0,0.5)→(0,0.5) | (0,0.5) | (15,0.000640869) | (130,130) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ·1 | Counter | 1568.89,161.10→1703.96,202.22 | 135.07×41.13 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (587.422,358.8) | (135.067,41.1279) | T | Image | Feedback Scoring Button 96×60 九宫38,20,38,20 | Sliced (0.653,0.0677,0.0677,1) |  |  |
| ··2 | EverguildTextMeshPro | 1580.52,165.13→1692.32,198.19 | 111.80×33.06 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,1.4782e-05) | (111.803,33.0629) | T | EverguildTextMeshPro |  |  | '300' 字号=34.849998474121094 auto[18.0~72.0] 对齐=Center/Capline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ··2 | Image | 1521.15,145.04→1586.49,224.07 | 65.34×79.03 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-82.6,-2.9) | (65.3353,79.0318) | T | Image | 40k_Achievements_icon_seal points 54×74 否 | Simple (1,1,1,1) preserveAspect |  |  |

⚠️ 布局组（子节点位置**由布局算**，上面已是**布局跑之后**的值）：
      buttons → VerticalLayoutGroup [ok]
          ContainerHolder → GridLayoutGroup [grid?]
```

**表里 `⚠️unk` 出现次数：0**（两个布局组一个 `[ok]` 一个 `[grid?]`）。`[grid?]` 的含义见坑 B：**不是「算不准」，是「没算」**。

### A·2 补列 1 —— `activeSelf` 的出现条件（**工具不给，回代码查的**）

**出厂态**：`Trophies Tab` 自己 `m_IsActive = false`；子树里**所有**节点 `act = T`（表里只有第 1 行是 **F**）。
出处：`bundle_menus_assets_all/GameObject/Trophies Tab.json` 的 `m_IsActive: false`，与全表唯一一个 `F` 吻合。

**运行期谁切它**（唯一入口，无反例）：

| 触发 | 动作 | 出处 |
|---|---|---|
| `GameWindowWithTabs.SetupTabs()` 遍历 `tabs[]` | 先 `tabPrefab.SetActive(false)`；再 `tabButtons.Initialize(window)`；再对每个 tab 调 `tab.Setup(window)`（vtable slot 5） | `d:/2/tools/decomp_full/GameWindowWithTabs__SetupTabs.c` |
| `GameWindowWithTabs.OpenTabs()` | `currentTab = GetStartingTab()`（首帧）→ `currentTab.TryOpenTab()` | `d:/2/tools/decomp_full/GameWindowWithTabs__OpenTabs.c` |
| `WindowTabBase.TryOpenTab()` | `gameObject.SetActive(true)` **再** `OnOpen()` | `d:/2/tools/decomp_full/WindowTabBase__TryOpenTab.c` |
| `WindowTabBase.Toggle(bool)` | `gameObject.SetActive(option)`（不调 OnOpen） | `d:/2/tools/decomp_full/WindowTabBase__Toggle.c` |
| `AchievementsMenu.OnOpen()` | **只**调 `Refresh()`，不自己 SetActive | `d:/2/tools/decomp_full/AchievementsMenu__OnOpen.c` |

🔴 **本页独有的「自毁」条件**（`AchievementsMenu.Setup` 里，**这是 activeSelf 之外更重要的一条**）：

```
base.Setup(window);                                  // WindowTabBase<PlayerProfileMenu>.Setup
if (AchievementsManager.Instance != null) {
  if (Instance.IsEnabled) {                          // 0x29
     cfg = ConfigManager.GetConfig<...>();
     if (cfg == null) goto DESTROY;
     if (FeatureConfig.CheckFeature(cfg, AchievementsManager.FeatureKey, out _)) return;  // 功能开 ⇒ 留
  }
  GameWindowWithTabs.DestroyTab(baseWindow, this);   // 否则 ⇒ 永久删掉这个页签
}
DESTROY: /* 同上 */
```
- `AchievementsManager.FeatureKey` 的真值是字符串 **`Achievements`**（`AchievementsManager__get_FeatureKey.c` → 字面量 `0x424cb50`；`d:/2/tools/il2cpp_out/stringliteral.json`）。
- `FeatureConfig.CheckFeature(config, key, out int)` 内部先做 `key.Replace("_dev", "")` 再查（`FeatureConfig__CheckFeature.c` 里 ``"_dev"` → `""`，字面量 `0x4259d60`/`0x42b80e0`）。
- `AchievementsManager.IsEnabled`（0x29）在 `Initialize()` 里、`CheckFeature` 返回真之后才置 1（`AchievementsManager__Initialize.c` 第 115 行；`IsInitialized`(0x28) 在第 183 行）。
⇒ **结论**：原版这页**不是「点开才亮」那么简单** —— 功能开关关掉时，`Setup` 阶段就把整个页签 `Object.Destroy` 掉。复刻时需要决定：我们是否也做这个「功能关掉就删页签」，还是留一个空页（**这是决策点，不是数据**）。

**另一条（窗口级）**：`PlayerProfileMenu.Open()` 在「看的不是自己」时把 `Components.tabButtons` 和 `tabContent`(0xB8) 都 `SetActive(false)`，加载完再在 `PlayerProfileMenu.OnComplete()` 里把 `tabContent` 设回 `true` 并调 `GameWindowWithTabs.Open()`（`PlayerProfileMenu__Open.c` / `__OnComplete.c`）。
⚠️ 但 `OnComplete` **没有**把 `Components.tabButtons` 设回来 ⇒ 看别人的档案时**左栏页签按钮是隐藏的**。⇒ **`Trophies Tab` 此时只有靠 `GetStartingTab()`（出厂 = `Title Tab`）才会被打开**。这条属于窗口层，`档案窗_BattleLog与页签按钮.md` 那边也该有；此处只记事实。
⚠️ `PlayerProfileMenu.Awake` 另挂了两个关窗入口：`closeButton.onClick` 与 `backgroundCloseButton.onClick` → `Close()`（`PlayerProfileMenu__Awake.c`）。

### A·3 补列 2 —— ppu ≠ 100 / offset ≠ 0 / 九宫格 ≠ 全 0 的图（**逐个点出**）

| 节点 | sprite 名 | 原尺寸 | 九宫格 | 判定 | 出处 |
|---|---|---|---|---|---|
| `bg` | `UI_Deck_Information_submenu_Back` | 69×63 | **18,18,18,18** | 有九宫；`m_Type=1 (Sliced)` | `bundle_atlasindividual_assets_0_mainmenu/Sprite/UI_Deck_Information_submenu_Back.json` |
| 12× `Achievement Container` 底板 | 同上 | 69×63 | **18,18,18,18** | 有九宫 | 同上 |
| `Counter` 底板 | `Feedback Scoring Button` | 96×60 | **38,20,38,20** | 有九宫；颜色 **(0.653, 0.0677, 0.0677, 1)**（暗红） | `bundle_atlasindividual_assets_0_mainmenu/Sprite/Feedback Scoring Button.json` |
| Slider `Background` | `40k_campaign_bar_bg` | 42×18 | **20,0,20,0** | 有九宫；颜色 **(0.299, 0.289, 0.689, 1)** | `bundle_atlasgroup_assets_all/Sprite/40k_campaign_bar_bg.json` |
| Slider `Outline` | `40k_campaign_bar_outline` | 46×22 | **20,0,20,0** | 有九宫；颜色 **(1, 0.841, 0, 1)**（金） | `bundle_atlasgroup_assets_all/Sprite/40k_campaign_bar_outline.json` |
| Slider `Fill` | `40k_campaign_bar_fill` | — | — | 名从工具缓存 `_tmp_view/_menu_sprite_idx.json` 解出；**尺寸/九宫格未取**（见 §D） | 表里 `Sliced` |
| Slider `end` | `40k_campaign_bar_end` | — | — | 同上；`m_Type=0 (Simple)` + `preserveAspect=1` | 表里 `Simple … preserveAspect` |
| `button_bg`（5 个 toggle） | `40K_settings_button` | 168×156 | **否（全 0）** | 🔴 **`m_PixelsPerUnitMultiplier = 0.92`**，全页**唯一**一个 ppuMul≠1 | `bundle_menus_assets_all/MonoBehaviour` 里 `button_bg` 的 Image（工具 `ppuMul=0.9200000166893005`）+ `bundle_duplicateassetisolation_assets_all/Sprite/40K_settings_button.json` |
| `Scroll` / `Viewport` 的图 | `1660267235368898380` = **`Background`** (32×32, border **10,10,10,10**) · `-426171492875694260` = **`UIMask`** (32×32, border **10,10,10,10**) | 32×32 | **10,10,10,10**（有九宫） | **Unity 内置 sprite**，`m_Sprite` 是 `m_FileID=15` **跨文件引用** ⇒ 本包解不出名，工具打 `<未解出>` | 已由兄弟普查解出：`资料/普查产出_0927/档案窗_BattleLog与页签按钮.md` §D1 与 `资料/普查产出_0917/菜单盘点_块4.md:127`；本页复核：`m_Sprite = {m_FileID:15, …}`、`m_Type=1` |
| **全部 `ppu = 100`、`offset = (0,0)`** | — | — | — | 已读的 6 张具名图（上表前 5 行 + `40k_Achievements_icon_seal points`）实测 `m_PixelsToUnits = 100.0`、`m_Rect.x/y = 0` ⇒ **没有 ppu≠100 / offset≠0 的图**；工具表里也从没打印 `ppu=` / `off=`（它只在 ≠100 / ≠(0,0) 时才打） | 各 Sprite JSON 的 `m_PixelsToUnits` / `m_Rect` |

📌 **ppuMul 的效力**：Unity 的 `PixelsPerUnitMultiplier` **只对 `Sliced` / `Tiled` 生效**；而 `button_bg` 是 `Simple` ⇒ **0.92 在本页实际不起作用**（除非有人调 `SetNativeSize`）。记下来是防「看到 0.92 就去乘」。

📌 **两处「同一控件两套 mask 参数」**（跨页对账时别混）：
- 本页 `Viewport` = `Image + **RectMask2D**`（`m_Padding = (0,0,0,0)`、`m_Softness = (0,0)`；`MonoBehaviour_2412232761782860338.json`）
- `Battle Log Tab` 的同名 `Viewport` = `Image + **Mask**`（`档案窗_BattleLog与页签按钮.md:32`）
- 两张图的 `m_Sprite`、`m_Type=1` 相同，**组件不同**。

### A·4 补列 3 —— 🔴 三个「运行时生成」的族（**逐个查清**）

#### ① `categoryTogglePrefab` + `categoryToggles` —— 分类页签

**结论：预制体里**有** 5 个实例，但它们在 `Awake` 里被**全部销毁并重建**；运行时是 **4 个**（= 枚举值个数），不是 5。**

- 字段引用真值（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-4927132572019295694.json`）：
  `holder = 5667521496207620658` · `categoryToggles = 2595449638871923250` · `containerPrefab = 1609980904319309702`
  · `categoryTogglePrefab = -3246518748154290405` · `pointsCounter = 4401987924990523954`
- `categoryToggles` 解到 **`GameObject/buttons_8428075580634790450.json`**（节点名就叫 `buttons`）上的 **`ToggleGroup`**（`MonoBehaviour_2595449638871923250.json`，`m_AllowSwitchOff = 1`）。
- `categoryTogglePrefab` 解到 **`MonoBehaviour_-3246518748154290405.json`**，类名 `EverguildToggle`，它挂在 **`GameObject/Achievement Type Toggle.json`** 上（**根对象，`m_Father = 0`**，RT `5825523976516345627`，**`m_SizeDelta = (333.301, 100)`**）。
- **有几个分类**：`AchievementsMenu.Awake` 走 `System.Enum.GetValues(typeof(Achievement.AchievementType))`
  ⇒ 枚举真值 `Battle = 1, Collection = 2, Victories = 4, Account = 8`（`d:/2/tools/il2cpp_out/dump.cs:124729`-`124736`；`d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/LiveOps/Achievement.cs`）
  ⇒ `GetValues` 按值升序 ⇒ **4 个，顺序 = Battle → Collection → Victories → Account**。
  ⚠️ **预制体里烘焙的 5 个 ≠ 运行时的 4 个**；第 5 个的来源查不到（§D）。
- **文案**：`Awake` 里对每个值做
  `LocalizationManager.GetTranslation(string.Format("Achievements/Types/{0}", 枚举值))`
  ⇒ 词条键 = **`Achievements/Types/Battle` / `…/Collection` / `…/Victories` / `…/Account`**
  （格式串字面量 `0x424d050` = `"Achievements/Types/{0}"`，`d:/2/tools/il2cpp_out/stringliteral.json`；`System.String.Format` 对枚举装箱值取 `ToString()` ⇒ 得到**枚举名**，不是数字）。
  ⚠️ **这 4 个 key 的译文在解包物里查不到**（§D）⇒ 预制体里看到的 `'Secret'` 是**占位符**，不是真文案。
- **怎么排**：不靠代码摆，靠 `buttons` 上的 **`VerticalLayoutGroup`**（见坑 A 的 6 个参数真值）+ `m_ReverseArrangement = 0`
  ⇒ 每个 toggle **284.11 × 100**（宽被 `childControlWidth=1 + childForceExpandWidth=1` **强制**成父宽 284.11，**不是 prefab 自己的 333.301**；高被 `childControlHeight=0` 保留 100），竖直间距 **20**。
- **选中态**：`Awake` 对每个新建 toggle 做 `toggle.isOn = (枚举值 == this.filter)`；`filter` 由**构造函数写死为 1（Battle）**（`AchievementsMenu__.ctor.c`：`*(param_1 + 0x58) = 1`）⇒ **出厂选中 Battle（第一个）**。
  ⚠️ 预制体里 5 个实例 `m_IsOn` 全 0 ⇒ **「哪个亮着」在数据里体现不出**（与 §2·1 对左栏 6 键的结论同型）。选中态靠 `EverguildToggle.changeSpriteOnValueChange`：`onSprite = -2307655919992762574`（= `40K_settings_button_hover`，取自工具缓存）、`offSprite = 1956647257489494794`（= `40K_settings_button`）、`onColor=(1,1,1,1)` `offColor=(0.75,0.75,0.75,1)`。
- **点了会怎样**：`Awake` 给每个 toggle 的 `onValueChanged` 加监听（`MonoBehaviour_-3246518748093790` 里 `m_IsOn` 旁的 `+0x118` = Toggle 的 `m_OnValueChanged`），回调是 `AchievementsMenu.<>c__DisplayClass6_0.<Awake>b__0(bool)`：
  `ToggleGroup.NotifyToggleOn(group, thisToggle, sendCallback:false)` → `filter = 该分类` → `Refresh()`
  （`d:/2/tools/decomp_full/AchievementsMenu.__c__DisplayClass6_0___Awake_b__0.c`）。
  ⚠️ `sendCallback = false`（第 3 个实参是 0）⇒ **不会递归触发别的 toggle 的回调**。

#### ② `containerPrefab` + `holder` —— 成就格子的容器

**结论：预制体里**有** 12 个实例，`Refresh()` 一进来**先全部销毁**再按筛选重建 ⇒ 运行时个数 = **命中筛选的成就数**，不是 12。**

- `holder` = **`GameObject/ContainerHolder.json`** 的 RectTransform（pid `5667521496207620658`），身上四件套：
  · `ContainerHolder`（`MonoBehaviour_3232089524897610290.json`，**`maxAmount = -1`**）
  · `ContentSizeFitter`（`MonoBehaviour_-4415185554644108750.json`，`m_HorizontalFit=0` `m_VerticalFit=1`）
  · **`GridLayoutGroup`**（`MonoBehaviour_6887013298322831922.json`）真值：
    `m_Padding = L0 R0 T32 B32` · `m_ChildAlignment = 1 (UpperCenter)` · `m_StartCorner = 0` · `m_StartAxis = 0 (Horizontal)`
    · **`m_CellSize = (520, 150)`** · `m_Spacing = (10, 10)` · **`m_Constraint = 0 (Flexible)`** · `m_ConstraintCount = 2`（Flexible 下**被忽略**）
- `containerPrefab` = **`MonoBehaviour_1609980904319309702.json`**（类 `AchievementContainer`），挂在 **`GameObject/Achievement Container_104989640328697734.json`**（**根对象**，RT `-4489455861874075770`，**`m_SizeDelta = (0, 150)`**、**`m_LocalScale = 0.99998927`**（全页唯一不是 1 的 scale，是 prefab 资产根上的烘焙残值））。
  ⚠️ **prefab 自己的宽是 0、不是 520** —— 520 是 GridLayoutGroup 的 `m_CellSize` 给的。
- **格子尺寸**：**520 × 150**（= cellSize）。
- **一屏几列几个**：`m_Constraint = Flexible` ⇒ 列数由宽度推：
  `列 = ⌊(1111.82 + 10) / (520 + 10)⌋ = 2`
  `行（可见）`：Viewport 高 675.55，holder 顶比 Viewport 顶**高 15.87**（`holder m_AnchoredPosition.y = 15.874`）⇒ 可见区间 = holder 局部 y ∈ [15.87, 691.42]
  每行步进 **160**（150 格 + 10 间距），首行从 **32**（= padding.top）起 ⇒ 行顶 = 32/192/352/512/672
  ⇒ **2 列 × 4 行 = 8 格完整可见，第 5 行露 ~19.4 px**（`671.42 − 672` 负数，即第 5 行顶在 691.42 之前、只露 691.42−672 ≈ 19.4）。
- **怎么排**：**布局组**（`GridLayoutGroup` + `ContentSizeFitter` 竖自适应），**不是代码摆**。
  `Refresh()` 只做 `Instantiate(containerPrefab, holder)`，**不设 `anchoredPosition` / `sizeDelta`**。
- 🔴 **`Refresh()` 绕开了 `ContainerHolder.TryAdd`**：全 25096 份产物里，`ContainerHolder.TryAdd/Clear/GetContainers` 的调用点**只在 `MissionsTab`**（`MissionsTab__CreateMissions.c` / `__InstantiateGridMissions.c` / `__InstantiateNormalMissions.c`），**`AchievementsMenu` 一个都没调** ⇒ 本页 `ContainerHolder` 只是个「带网格布局的父节点」，`maxAmount = -1` 对本页**无意义**。**复刻时别去实现 TryAdd**。

#### ③ `pointsCounter` —— 计数条

**结论：它就是 `Counter` 条里那个 TMP，显示「所有成就的 `CurrentPoints` 之和」的裸十进制，没有格式、没有本地化、没有「分」字。**

- `pointsCounter = 4401987924990523954` → **`MonoBehaviour_4401987924990523954.json`**（类 `EverguildTextMeshPro`），挂在 `Counter` 条的子节点 `EverguildTextMeshPro` 上（RT `-3273543992523326926` 的子… 实测：`GameObject/EverguildTextMeshPro_3754844496519461426.json`，RT 尺寸 **111.803 × 33.0629**，anchor/pivot 全 (0.5,0.5)，`pos = (0, 1.4782e-05)`）。
- `Counter` 条本体（`RT 8342848023885347378`，**135.067 × 41.128**，`Feedback Scoring Button` 九宫 38,20,38,20）+ 子 `Image`（`40k_Achievements_icon_seal points` 54×74，`preserveAspect`，rect 65.335×79.032，`pos=(-82.6, -2.9)`）。
- **显示什么**：`Refresh()` 末尾
  `local_res18[0] += achievement.CurrentPoints`（对**每一条**成就，**在筛选判断之前**）
  → `System.Int32.ToString()` → `pointsCounter.SetText(...)`
  ⇒ **是全量成就积分之和，不受当前分类筛选影响**；格式 = `ToString()` 默认（**无千分位、无后缀**）。
  出处 `d:/2/tools/decomp_full/AchievementsMenu__Refresh.c`；字段偏移 `Achievement.CurrentPoints` = **0x30**（`d:/2/tools/il2cpp_out/dump.cs:124759`-`124780`）。
- 预制体里的 `'300'` 是**占位数字**，`字号=34.85`（`auto[18~72]`，见坑 C：不是渲染字号），`对齐=2/8192` = 水平 Center / 垂直 **Capline**，色白。

#### ④ 顺带：容器内部 7 个字段各写什么（`AchievementContainer.OnInitialize`，一次查清）

字段偏移（`d:/2/tools/il2cpp_out/dump.cs` 的 `AchievementContainer`：title 0x40 / description 0x48 / reward 0x50 / image 0x58 / sliderBar 0x60 / counter 0x68），我按 `d:/2/tools/decomp_full/AchievementContainer__OnInitialize.c` 逐个解到**具体节点**：

| 字段 | 对应节点（表里的行） | 写什么（**格式串已解出**） |
|---|---|---|
| `title` (0x40) | `Achievement Container/…/title` | `string.Format("{0} {1}/{2}", achievement.LocalizedTitle, achievement.TargetTier, challenge.milestones.Count)`<br>格式串 `0x426b5d8` = **`"{0} {1}/{2}"`**；`0x28`=challenge，`challenge+0x18`=milestones 的 `List`，`list+0x18`=**Count**（IL2CPP List 布局 `_items 0x10 / _size 0x18`）<br>⇒ 预制体的 `'Victorious 1/5'` = 标题 + **第 1 级 / 共 5 级** |
| `description` (0x48) | `…/description` | `SupportMethods.ModifyLocalization(achievement.LocalizedText)` |
| `image` (0x58) | `…/Image`（**130×130**，prefab 里 `m_Sprite` 的 PathID = **0**，即**无图**） | `Image.sprite = AchievementsManager.GetIconByTier(achievement.TargetTier)`<br>候选图 = `40k_Achievements_icon_medal1`…`medal5`（各 **154×154**、border 全 0、ppu 100，在 `bundle_duplicateassetisolation_assets_all/Sprite/`，`GetIconByTier` 做 `tierIcons[tier-1]`） |
| `sliderBar` (0x60) | `…/Progress/Slider` | `set_wholeNumbers(true)` → `set_maxValue(…)` → `set_value(…)`（**后两个的实参被 Ghidra 丢了**，见 §D） |
| `counter` (0x68) | `…/Progress/Slider/counter` | `string.Format("{0}/{1}", achievement.CurrentValue, nextMilestone.targetValue)`<br>格式串 `0x426de28` = **`"{0}/{1}"`**；`targetValue` 在 `ChallengeMilestone` 的 **0x14** |
| `reward` (0x50) | `…/rewards` | `string.Format(LocalizationManager.GetTranslation("Achievements/Points"), First(milestone.rewards).<getter>())`<br>词条键 `0x424ce50` = **`"Achievements/Points"`**（词条文本里带 `{0}`，预制体渲染成 `'2 points'` 是**英文译文 + 2**）；`rewards` 是 `RewardInfo[]`（`ChallengeMilestone+0x18`），取 `First`
| `backgroundButton` | 字段在 JSON 里存在但 **`m_PathID = 0`（原版没接）** | 无 |

**另一处 `Achievements/Types` 前缀的同族键**（供做本地化时对齐）：
- `Achievement.LocalizedTitle` = `GetTranslation("Achievements/Title/" + GetID())`（前缀 `0x424cf50`）
- `Achievement.LocalizedText` = `GetTranslation("Achievements/" + …)`（前缀 `0x424cc50`）

---

## B. 入口 / 它调用的 / 谁监听它（照 `项目任务.md` §三 第 10 条 10·2）

### B·1 入口（谁开它 / 谁调它）

| # | 入口 | 证据 |
|---|---|---|
| 1 | **没有代码 `new AchievementsMenu()`，也没有任何 `AchievementsMenu.xxx` 的静态引用** —— 全 25096 份反编译产物里，`AchievementsMenu` 只出现在它自己的 6 个文件里（`grep -rln "AchievementsMenu" d:/2/tools/decomp_full` = 6 个，全是 `AchievementsMenu__*.c`）⇒ **它是预制体上挂的组件，由 Tab 框架驱动**，不是被代码构造的 | `d:/2/tools/decomp_full/` |
| 2 | 页签框架：`GameWindowWithTabs.SetupTabs()` → 对 `tabs[]`（`this+0x78`）每项 `tab.Setup(window)` | `GameWindowWithTabs__SetupTabs.c` |
| 3 | `GameWindowWithTabs.OpenTabs()` → `GetStartingTab()` → `currentTab.TryOpenTab()` → `SetActive(true)` + `OnOpen()` | `GameWindowWithTabs__OpenTabs.c`、`WindowTabBase__TryOpenTab.c` |
| 4 | 侧栏按钮创建：`GameWindowWithTabs.CreateTabButton(...)` → `TabbedWindowComponents.tabButtons.AddTabButton` + `AssignTabButton`（`AddTabButton` 里 `Instantiate(tabButtonPrefab, tabHolder)`、`SetSiblingIndex(0)`、`Toggle.set_group(...)`、`Image.set_sprite(icon)`）<br>⚠️ 本窗的 `Tab Buttons` 预制体里**已经有 6 个烘焙按钮**（§2·1），而框架**会在运行期再创建** ⇒ **两套并存**，复刻时要选一套 | `GameWindowWithTabs__CreateTabButton.c`、`TabButtons__AddTabButton.c` |
| 5 | 窗口根：`PlayerProfileMenu`（`GameWindowWithTabs` 子类）；`Awake` 挂 `closeButton.onClick` / `backgroundCloseButton.onClick` → `Close()`；`Open()` 决定自己/别人两种态 | `PlayerProfileMenu__Awake.c`、`PlayerProfileMenu__Open.c` |
| 6 | `GameWindowWithTabs.DestroyTab(baseWindow, this)` —— **唯一的「反入口」**：`tabs.Remove` + `TabButtons.RemoveTabButton` + `dict.Remove(type)` + `Object.Destroy(gameObject)` | `GameWindowWithTabs__DestroyTab.c` |

### B·2 它调用的（按类归）

**`AchievementsMenu` 自己（只有 4 个方法体，全部读完）**：

| 方法 | 调的外部方法（归类） | 出处 |
|---|---|---|
| `.ctor` | `OnClickDestroy..ctor` 链（基类） | `AchievementsMenu__.ctor.c` |
| `Awake` | **工具类**：`SupportMethods.DestroyAllChildren`<br>**反射**：`Type.GetTypeFromHandle` + `Enum.GetValues` + `Array.GetEnumerator`<br>**Unity 核心**：`Object.Instantiate`<br>**uGUI**：`Toggle.set_isOn` / `Toggle.set_group` / `ToggleGroup.NotifyToggleOn`（回调里）<br>**本地化**：`LocalizationManager.GetTranslation`<br>**字符串**：`System.String.Format` | `AchievementsMenu__Awake.c`、`AchievementsMenu.__c__DisplayClass6_0___Awake_b__0.c` |
| `Setup` | `WindowTabBase<T>.Setup`（基类）<br>**配置**：`ConfigManager.GetConfig<…>` + `FeatureConfig.CheckFeature`<br>**管理器**：`AchievementsManager.get_Instance` / `get_FeatureKey`<br>**框架**：`GameWindowWithTabs.DestroyTab` | `AchievementsMenu__Setup.c` |
| `OnOpen` | **只调 `Refresh()`** | `AchievementsMenu__OnOpen.c` |
| `Refresh` | **工具**：`SupportMethods.DestroyAllChildren`<br>**管理器**：`AchievementsManager.get_Instance` / `get_Achievements`<br>**Unity**：`Object.Instantiate`<br>**UiContainer**：`UiContainer<T>.Initialize`<br>**字符串**：`System.Int32.ToString`<br>**TMP**：`EverguildTextMeshPro.set_text`（vtable `+0x558`） | `AchievementsMenu__Refresh.c` |

**`AchievementContainer.OnInitialize`** 调的：`Achievement.get_NextMilestone / get_CurrentValue / get_LocalizedTitle / get_TargetTier / get_LocalizedText`、`SupportMethods.ModifyLocalization`、`AchievementsManager.get_Instance / GetIconByTier`、`Image.set_sprite`、`Slider.set_wholeNumbers / set_maxValue / set_value`、`String.Format`、`LocalizationManager.GetTranslation`、`Enumerable.First`。（`AchievementContainer__OnInitialize.c`）

**`SupportMethods.DestroyAllChildren(Transform)` 的语义**（关系到「烘焙实例还在不在」）：遍历 `Transform` 取每个子节点 → `Component.get_gameObject` → **`UnityEngine.Object.Destroy(go)`**
（`SupportMethods__DestroyAllChildren.c`，末尾 `UnityEngine_Object__Destroy(uVar5,0)`）
⇒ **是 `Destroy` 不是 `DestroyImmediate`**：同帧内 prefab 引用仍可用，实际销毁推迟到帧末。这也解释了为什么 `Refresh()` 能「先清空再 Instantiar」而不炸。

### B·3 谁监听它（触发什么 / 写哪些静态量 / 谁 Poll）

| 问题 | 结论 | 证据 |
|---|---|---|
| 本页订阅了谁的事件？ | **只有自己建的那 4 个分类 toggle** 的 `onValueChanged`（`UnityEvent<bool>.AddListener`）。**没有任何对 `AchievementsManager` / `Achievement` 的订阅** | `AchievementsMenu__Awake.c`（唯一的 `AddListener` 调用） |
| 触发什么 | `NotifyToggleOn(..., sendCallback:false)` → 写 `this.filter`（`this+0x58`）→ `Refresh()` | `AchievementsMenu.__c__DisplayClass6_0___Awake_b__0.c` |
| 有静态量吗 | **没有**。`AchievementsMenu` 无静态字段；`grep` 全产物无对 `AchievementsMenu` 静态量赋值处 | `d:/2/tools/il2cpp_out/dump.cs:67072`-`67100` |
| 谁 Poll 它 | **没有**。全产物里 `AchievementsManager.get_Instance / get_Achievements / GetIconByTier / GetValue / GetAchievement` 的消费点只有：`AchievementContainer.OnInitialize`、`Achievement.get_CurrentValue`、`Achievement.OnReachMilestone`、`PlayerDataManager.get_rankedMatchesWon`、`AccountTab.<Register>b__21_0`、`PlayerDataManager._SendAccountConfirmationEmail_d__509.MoveNext` —— **没有任何一处轮询本页** | `grep -rln` 上述 5 个方法名，`d:/2/tools/decomp_full/` |
| 它监听别人吗（被动刷新） | 🔴 **不监听** ⇒ **成就积分/进度变化时这页不会自己刷**，只有 `OnOpen` 与「切分类」两个刷新点。**复刻时若想做实时刷新，那是我们加的，不是复刻** | 同上 |
| 上级框架监听它吗 | `TabButtons.Initialize` 里对 `GameWindowWithTabs +0x98` 做 `Delegate.Combine`（订阅窗口的 tab 变化）；`TabButtons` 用 `List<TabButtons.TabButton>{ WindowTabBase tab; Toggle toggle; }` 做映射 | `TabButtons__Initialize.c`、`TabButtons__AssignTabButton.c`、`dump.cs:75351`-`75353` |
| ⚠️ 零调用点声明 | `WindowTabBase.TryOpenTab` 在**全产物里只出现在自己的定义文件**（`grep -rln "TryOpenTab"` = 1 个）。**这不是「没人调」** —— 证据：`GameWindowWithTabs__OpenTabs.c` 里有 `(**(*plVar1 + 0x1a8))(plVar1, *(plVar1 + 0x1b0))` 的 **IL2CPP 虚调用**（vtable 槽位 + MethodInfo），Ghidra 不会把它反成符号名。⇒ **判据：出现这种 `(**(*obj + N))(obj, *(obj + N + 8))` 形态 = 虚调用，符号 grep 必然 0 命中。** | `GameWindowWithTabs__OpenTabs.c`、`WindowTabBase__TryOpenTab.c` |
| ⚠️ 方法体缺失 | **`WindowTabBase<T>.Setup` 没有产物**（`d:/2/tools/decomp_full/WindowTabBase__Setup.c` 里是**非泛型**基类那份：`param_1[4] = param_2` + 一次间接跳转）。泛型版 RVA `0x1572630`（`dump.cs:110179` 附近）**对应文件不存在**。⇒ 它「写 `Window + 调 `OnSetup()``」这条是**从 RVA/槽位推的，不是读到的**；`AchievementsMenu` 的 `Setup` 只是 `base.Setup(window)` 后接功能开关判断。 | `d:/2/tools/il2cpp_out/dump.cs` 的 `WindowTabBase<object>$$Setup` RVA `0x1572630`；`d:/2/tools/all_methods.txt:17392` |

---

## C. 查不到的（**不猜**）

| # | 查不到什么 | 搜过哪些词 | 搜过哪些目录 / 判据 |
|---|---|---|---|
| C1 | **4 个分类的中文/英文真文案**（`Achievements/Types/Battle` 等 4 个 I2 词条的译文）；以及 `Achievements/Points`、`Achievements/Title/*`、`Achievements/*` 的译文 | `Achievements/Types`、`Achievements/Points`、`Achievements/Title/`、`Victorious`、`i2languages`、`Localization`（文件名） | `d:/2/新解包资源/assets_full/**`（84 个 bundle，**全 0 命中**）· `d:/2/新解包资源/assets_full/resources/TextAsset/`（只有 5 个 TMP/性能文本）· `d:/4/Unity/**`（0 命中）。**判据**：词条由 `AddressableLocalizationLibrary` 在运行期走 Addressables 拉（`d:/2/tools/decomp_full/AddressableLocalizationLibrary__*.c` 共 5 个文件），**本地解包物里没有词条表**。⇒ 预制体里那 4 个 `'Secret'` 是占位，**别当文案用**。 |
| C2 | **预制体里第 5 个分类 toggle 是哪来的**（枚举只有 4 个值，`buttons` 下却烘焙了 5 个） | 读 `Achievement.AchievementType` 枚举全文；对比 `GetValues` 的 4 个值；数 `buttons` RT 的 `m_Children` = 5 | `dump.cs:124729`-`124736`、`Achievement.cs`（桩）、`RectTransform/RectTransform_3302881417708993074.json`。**判据：多出来的那个在运行时会被 `DestroyAllChildren` 干掉**，所以**不影响施工**；但它的存在说明预制体比当前枚举旧一版。**不许编第 5 个分类名。** |
| C3 | ~~**成就条目本身**（有几条成就、标题/描述/奖励各是什么）~~ 🔴 **2026-09-27 已推翻：条目在本地，102 条** | `AchievementsList`、`Achievement`（**文件名**） | ~~`d:/2/新解包资源/assets_full/**`：**没有任何 `Achievement` 的 ScriptableObject**~~ —— **错因：只按「类名」找文件，而原版给它们的文件名是 `ACH##`，不是 `Achievement*`**（`eventId` 是 `Ach_N`）⇒ 搜 `achievement`/`achiev` 全 0 命中是**必然**，属「**搜过的词不够**」那一类误判。<br>✅ **实际位置**：`d:/2/新解包资源/assets_full/bundle_staticgeneralassets_assets_all/MonoBehaviour/` —— `AllAchievements.json`（= `AchievementsList` 本体，`m_Script.m_PathID = 2168181930003038902`，`achievements: [102 个引用]`）+ **`ACH1 … ACH102` 共 102 个 `Achievement` SO**。<br>**每条的真数据**（例 `ACH1 Slay the Warlord`）：`eventId = "Ach_1"` · `achievementType = 1 (Battle)` · `challenge`（类 `DamageDeltToEnemyUnit`、id `"Damage To Warlord"`、`reachMilestoneWhen 10`、`cummulative 1`）· **`rewards[].targetValue` = 5 档真实阈值 `[1000, 10000, 50000, 250000, 999999]`**（就是 `NextMilestone.targetValue` / `TargetTier` 要读的东西）。<br>· **类型分布**：`1 Battle` 36 · `2 Collection` 28 · `4 Victories` 30 · `8 Account` 8 = **102** ✅ 与位标志定义吻合。<br>· **档位**：100 条各 5 档、2 条各 1 档（`ACH21 Enlist` / `ACH22 Astropath`，Account 一次性）⇒ **「tier 上限 5」是数据里读出来的**，与 `medal1..5` 五张图交叉吻合。<br>· **16 套阈值组**（`(1,2,3,4,6)` · `(7,14,28,90,180)` · `(10,100,400,1000,2000)` · `(25,250,2000,10000,25000)` · `(1000,10000,50000,250000,999999)` …）。<br>· **18 个 `MissionChallenge` 派生类**（`GamesPlayed` 30 · `UpgradeCard` 27 · `TriggerAbility` 17 · …）⇒ 成就**确实建在任务系统上**，`MissionsTab` 那层能复用。<br>· 交叉验证：`数据/索引/enum_names.json` 里 `bundle:staticgeneralassets_assets_all.bundle` → `so_count 104 / listed 104`，非 ACH 的只有 2 个（`AllAchievements`、`CardFramesByArmy`）。<br>⚠️ **仍然查不到的**：`AchievementsManager.tierIcons` 那个序列化数组（全库 `grep tierIcons` 0 命中；`level0/MonoBehaviour/` 里没有它指向的 `MonoBehaviour_229/374.json`）⇒ **档位图与档位的对应关系**本地没有，但**档位数（5）有数据支撑**。 |
| C4 | **`Slider.maxValue` / `Slider.value` 的实参表达式** | 读 `AchievementContainer__OnInitialize.c` 对应段；查 `Slider.set_value` 的 RVA | `d:/2/tools/decomp_full/AchievementContainer__OnInitialize.c`：写作 `UnityEngine_UI_Slider__set_maxValue();` 与 `(**(code **)(**(longlong **)(param_1 + 0x60) + 0x428))();` —— **两个调用点都在，但 Ghidra 把 float 实参丢了**。⇒ 只能确定「调了、且 `wholeNumbers=1`」。**可确定的相关事实**（不是推断）：`counter` 那行用的是 `NextMilestone.targetValue`（`ChallengeMilestone+0x14`），预制体烘焙值 `m_MinValue=0 / m_MaxValue=6 / m_Value=6`。**「slider 的 max 就是 counter 的分母」是**推断**，不是证据 —— 落地前自己定。 |
| C5 | **`AchievementsMenu` 与 `Achievements Tab` 那个孤儿根的关系、以及 `TrophiesWindow` 是什么** | 读 `GameObject/Achievements Tab.json`、`GameObject/TrophiesWindow.json`；反查它们的父/子 | 见下面 §C-附。**能确定的是「无人引用」；不能确定它们的来历。** |
| C6 | 本页里 `Image`(130×130) 原版在**空数据**时显示什么（无图标？留白？） | 读预制体：`m_Sprite.m_PathID = 0`（无图）；读 `AchievementsManager.GetIconByTier` | `GetIconByTier` 走 `tierIcons[tier-1]`，越界直接抛（`AchievementsManager__GetIconByTier.c` 里 `FUN_1803f4790` = IndexOutOfRange）。**空态没有任何分支** ⇒ **原版没有空态**，空态要自己造（与 §三 排行榜「无空态」同型）。 |
| C7 | `40k_campaign_bar_fill` / `40k_campaign_bar_end` 的**原尺寸 / 九宫格 / ppu** | 按名字 glob `*/Sprite/40k_campaign_bar_fill.json` 等 | **名字**从工具缓存 `d:/4/_tmp_view/_menu_sprite_idx.json` 解出（pid → name）；**尺寸未取**（缺 `bundle_atlasgroup_assets_all/Sprite/` 下这两张的读取）。⇒ 表里那两行的九宫格**不许照抄**（工具也没打）。**要施工就补一次**。 |

### C-附：本包里的**同名多份 / 孤儿**（发现即报）

| 对象 | 事实 | 出处 |
|---|---|---|
| **两个 `AchievementsMenu` 实例** | ① `MonoBehaviour_-4927132572019295694.json` 挂在 `Trophies Tab` 上，**5 个引用全有值**（本文全篇用的就是它）<br>② `MonoBehaviour_1022222315664511037.json` 挂在 **`GameObject/Achievements Tab.json`** 上，**`holder / categoryToggles / containerPrefab / categoryTogglePrefab / pointsCounter` 五者 `m_PathID` 全 = 0**（**空引用**） | 两个 JSON + `dump.cs:67072` |
| **孤儿根 `Achievements Tab`** | RT `-7048126750119620547`，`m_Father = 0`（根），`m_IsActive = true`，anchor (0,0)-(1,1)，`sizeDelta = (0.4778, 1.7410)`；**子树只有 18 个节点**（Trophies Tab 是 197）。结构 = `[自身] → Scroll(8770476021501005885) → ContainerHolder_5619(5531725209171267645, sizeDelta.y = 0) → 1 个 Achievement Container(‑6857241059549441987) → 它的子节点`<br>**谁引用它：只有 `AssetBundle/AssetBundle_1.json` 的 preload 表**（`grep -rl "1022222315664511037"` 只命中它自己和 AssetBundle 表）⇒ **没有任何 GameObject/RectTransform 把它当子节点**，是**遗留/废弃资产**。⇒ **别拿它当第二套布局依据。** | `GameObject/Achievements Tab.json`、`RectTransform/RectTransform_-7048126750119620547.json`、`RectTransform/RectTransform_8770476021501005885.json` |
| **两个 `Scroll` GameObject** | ① 本页的 `Scroll`：RT `-8414093990250513870`，GO `-1935557922389919182`，`m_Father = 8482975979751504434`（= Trophies Tab）<br>② 孤儿那份：`GameObject/Scroll.json`，RT `8770476021501005885`，`m_Father = -7048126750119620547`（= `Achievements Tab`）<br>两者的 Image/ScrollRect 配置**相同**（同一张跨文件 sprite `1660267235368898380`、`m_Type=1`、`ScrollRect m_MovementType=2`） | `bundle_menus_assets_all/GameObject/Scroll.json`、`MonoBehaviour_-3371321235388925390.json`(Image) / `-8481327093540357582.json`(ScrollRect) |
| **两个 `ContainerHolder`** | ① 本页的：RT `5667521496207620658`（13 行 `m_Children`? 否 —— **12 个**子 `Achievement Container`），`sizeDelta.y = 1014`<br>② `ContainerHolder_5619714873142022205.json`：RT `5531725209171267645`，`sizeDelta.y = 0`，父是孤儿那份的 `Scroll` 下 | `RectTransform/RectTransform_5667521496207620658.json`、`RectTransform/RectTransform_5531725209171267645.json` |
| **14 个 `Achievement Container` GameObject 文件，只有 12 个在本页** | 本页 12 个 = `Achievement Container_8365984317912611378`（表里第 1 行 `Achievement Container`）+ `(1)`…`(11)`；另两个：`Achievement Container_104989640328697734.json`（= **`containerPrefab` 本体**，根对象）+ `Achievement Container.json`（在**孤儿那份** holder 下） | `GameObject/Achievement Container*.json` 全表 + 各 RT 的 `m_Father` |
| **6 个 `Achievement Type Toggle` GameObject 文件，只有 5 个在 `buttons` 下** | `Achievement Type Toggle.json`（RT `5825523976516345627`，**`m_Father = 0`**）= **`categoryTogglePrefab` 本体**，`sizeDelta = (333.301, 100)`；`Achievement Type Toggle_1330132040127904306.json`（`m_Father = buttons`，槽位 1）+ `(1)`(2)(3)(4) = 5 个烘焙实例 | `RectTransform/RectTransform_5825523976516345627.json` 及 5 个实例 RT |
| **`TrophiesWindow`** | `GameObject/TrophiesWindow.json`：`m_IsActive = false`，2 个组件（RT + 一个 bundle 外脚本），RT 父 = `8599407251513794723`，4 个子节点 —— **本包内没人引用它**。它的那个脚本 pid 在本包 `MonoScript/` 里**不存在**（属别的 bundle）⇒ **类名查不到**，**不猜**。 | `GameObject/TrophiesWindow.json` |

---

## 附：本次用到的原始出处一览

- 工具：`d:/4/Unity/工具/menu_dump.py`（坑 A 在 `:272`-`274`；坑 B 在 `:326`-`328`；坑 C 在 `:233`-`239`）
- 预制体与组件：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/{GameObject,RectTransform,MonoBehaviour}/…`（逐条 pid 已写在正文）
- 类名真值：`d:/2/新解包资源/assets_full/bundle_Waprforge_monoscripts/MonoScript/MonoScript_8318264607718582278.json`（`m_ClassName = AchievementsMenu`）
- 字段偏移 / 方法 RVA：`d:/2/tools/il2cpp_out/dump.cs`（`AchievementsMenu` 在 `:67072`-`67100`；`AchievementContainer` 在 `:67038`-`67062`；`Achievement.AchievementType` 在 `:124729`-`124736`；`Achievement` 在 `:124759`-`124780`；`AchievementsManager`、`ChallengeMilestone`、`WindowTabBase`、`TabbedWindowComponents`、`TabButtons` 同文件）
- 字符串字面量：`d:/2/tools/il2cpp_out/stringliteral.json`（`0x424cb50`= `Achievements`、`0x424cc50`= `Achievements/`、`0x424ce50`= `Achievements/Points`、`0x424cf50`= `Achievements/Title/`、`0x424d050`= `Achievements/Types/{0}`、`0x426b5d8`= `{0} {1}/{2}`、`0x426de28`= `{0}/{1}`）
- 反编译正文：`d:/2/tools/decomp_full/AchievementsMenu__{ctor,Awake,Setup,OnOpen,Refresh}.c` · `AchievementsMenu.__c__DisplayClass6_0___Awake_b__0.c` · `AchievementContainer__{ctor,OnInitialize}.c` · `Everguild.LiveOps.Achievement__get_{NextMilestone,TargetTier,CurrentValue,LocalizedTitle,LocalizedText}.c` · `AchievementsManager__{Initialize,get_Achievements,get_FeatureKey,GetIconByTier}.c` · `FeatureConfig__CheckFeature.c` · `SupportMethods__DestroyAllChildren.c` · `WindowTabBase__{Setup,TryOpenTab,Toggle,OnOpen}.c` · `GameWindowWithTabs__{SetupTabs,OpenTabs,ChangeTab,CreateTabButton,DestroyTab,GetStartingTab}.c` · `TabButtons__{Initialize,AddTabButton,AssignTabButton}.c` · `PlayerProfileMenu__{Awake,Open,OnComplete}.c`
- 类签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/{AchievementsMenu,AchievementContainer,ContainerHolder}.cs` · `…/Everguild/LiveOps/Achievement.cs` · **`ContainerHolder.cs` 里的 `TryAdd/Clear/GetContainers` 在本页是死代码**
- sprite 元数据：`bundle_atlasindividual_assets_0_mainmenu/Sprite/{UI_Deck_Information_submenu_Back,Feedback Scoring Button,40k_Achievements_icon_seal points}.json` · `bundle_atlasgroup_assets_all/Sprite/{40k_campaign_bar_bg,40k_campaign_bar_outline}.json` · `bundle_duplicateassetisolation_assets_all/Sprite/{40K_settings_button,40k_Achievements_icon_medal1..5}.json`
- 图名缓存：`d:/4/_tmp_view/_menu_sprite_idx.json`（pid → 名，1242 条）
- 兄弟普查（可直接引）：`资料/普查产出_0927/档案窗_BattleLog与页签按钮.md`（`1660267235368898380`= `Background`、`-426171492875694260`= `UIMask` 的判据在它的 §D1）；模板位总表 `资料/说明书/04_界面UI/菜单全树.md:16155`-`16351`


---

## 附：子代理在正文之外另写的几条（原样保留）

---
## 给父代理的三条执行提示
1. **文件我没能落盘**（只读模式无 Write/Edit/Bash 重定向）。请把上面两行标记之间的内容原样写入 `d:/4/Unity/资料/普查产出_0927/档案窗_Trophies页.md`（该目录已存在，已有 3 个同类文件）。
2. **两处工具 bug 值得回写进 `工具/menu_dump.py` 的注释或 README**：
   - `align_on_axis`（`:274`）垂直分支应改成 `(int(align) // 3) * 0.5`（Unity 原版），当前实现是它的**反数**；`--verify-layout` 因为 §2·1 那个用例 `childForceExpandHeight=1` 而**覆盖不到**这一类。
   - `GridLayoutGroup` 在 `:327` 是**早退不计算**，只在「模板位恰好等于布局结果」时看起来正确。
3. **本次普查里最需要你拍板的一条**：`AchievementsMenu.Setup` 在功能开关（`FeatureKey = "Achievements"`）关闭时会 `DestroyTab` **永久删掉页签** —— 这是原版行为，复刻要不要跟，是决策不是数据（我已把它标成「决策点」而不是填进表里）。
---

# 附：2026-09-27 补充验证（子代理回头做的**二次核**）

> 结论：**已交付的数值全部确认，无一处需改** —— 下面三条是**精度增补**，不是更正。
> ⚠️ 落盘时**主对话对第 3 条做了订正**（原因见该条）。

## 增补 1 —— `Counter` 底板颜色用**精确浮点**（工具表里那个是截断过的）

| 节点 | 工具表打的（**别当施工值**） | **精确值（原始 JSON 实测）** |
|---|---|---|
| `Counter`（RT `8342848023885347378`）的 `Feedback Scoring Button` | `(0.653, 0.0677, 0.0677, 1)` | **`(0.6528301239013672, 0.06774645298719406, 0.06774645298719406, 1.0)`** |

出处：`bundle_menus_assets_all/RectTransform/RectTransform_8342848023885347378.json` 上那个 Image 的 `m_Color`
（`m_Sprite = {m_FileID: 2, m_PathID: 7085310608559771025}` · `m_Type = 1` · `m_PixelsPerUnitMultiplier = 1.0`）。

同批钉死的另两条（都是 `Trophies Tab` 的**直接子节点**，`m_Type = 1`）：

- `Scroll`（RT `-8414093990250513870`）：`m_Sprite = {m_FileID: 15, m_PathID: 1660267235368898380}` ·
  **`m_Color.a = 0.0`**（全透明，但**仍是 ScrollRect 的接收面**）· `ppuMul = 1.0`
- `bg`（RT `-3273543992523326926`）：`m_Sprite = {m_FileID: 2, m_PathID: -2338345087259041511}` ·
  `m_Color = (1,1,1,1)` · `ppuMul = 1.0`

⚠️ **这条的教训**：工具表里的**颜色是 `%.3g` 打的**（`menu_dump.py` 的 `_c()`），
**要施工值就去原始 JSON 读全精度**。同族的还有 `button_bg` 的 `(1,0.572,0,…)` —— 那一处已经用的是全精度。

## 增补 2 —— `Viewport` 的 `RectMask2D`「零」是**有意留零**，不是「没人动过」

全包 150+ 个 `RectMask2D` 里**非零值是常态**：`m_Softness` 出现过 25 / 42 / 50 / 52 / 54 / 89 / 100 / 174 / 200 / 375；
`m_Padding` 出现过 `(-8,-5,-8,-5)` / `(10,0,0,0)` / `(0,-15,0,0)` / `(2.49,2.49,2.49,2.49)` 等。
⇒ 本页 `Viewport` 的 **`m_Padding = (0,0,0,0)` + `m_Softness = (0,0)` 可以当「原版有意」写死**。

出处：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_2412232761782860338.json`；
扫描确认**它的父 RT = `-8414093990250513870` = `Scroll`** ⇒ 反证 `Viewport` 是 `Scroll` 的直接子节点
（与工具表里缩进 `··2` 吻合）。

## 增补 3 —— ⚠️ **主对话订正：这一条的前提已经失效**

子代理原文：「`Viewport` 的 `RectMask2D` 与 `Counter`/`bg` 的 Image 都走 `m_FileID` 跨文件引用
（`m_FileID = 2` / `15`）⇒ **工具链不追跨文件引用**，这类 pid 必然解不出图名……
表里凡见 `<未解出>` + `<无尺寸记录>`，先看 `m_FileID` 是否为非 0，别当缺失件上报。」

🔴 **订正**：那是**它当时那一版** `工具/menu_dump.py` 的行为。同一轮里我已经把
**上一版（2026-09-23 建）独有、被我重写时弄丢的**那条路补回来了 ——
`sprite_pid_map()` 用 `UnityPy` 扫**原始 `.bundle`** 的 `o.path_id` 建表（缓存 `_tmp_view/sprite_pids_ALL.json`，3316 条）。
实测那两张现在都解得出来：

- `1660267235368898380` → **`Background`**（32×32 · 九宫 10,10,10,10）
- `-426171492875694260` → **`UIMask`**（32×32 · 九宫 10,10,10,10）

⇒ **「跨文件引用解不出」这句话不要再传下去**。
✅ **仍然成立的那半句**：`<未解出>` 时先看 `m_FileID` 是不是非 0（那是**跨包**的信号），
但**结论要改成「去查真包缓存」**，而不是「解不出」。
⚠️ 同理：`档案窗_BattleLog与页签按钮.md` 的 §D1 已于当天就地订正过同一件事。

## 附：子代理点名的两条工具缺陷 —— **都已修**

它写给父代理的第 2 条点名两个 bug，**当天都已修进 `工具/menu_dump.py`**：

1. `align_on_axis` 纵轴公式（原来 `(2 - align//3)*0.5`，Unity 是 `(align//3)*0.5`）—— **已修**，
   并把 Trophies 的 `buttons` 那例**加成 `--verify-layout` 的回归用例**（自检抓不到这条，见上面坑 A 的横幅）。
2. `GridLayoutGroup` 早退不计算 —— **保持早退**（Unity 的网格算法与 `HorizontalOrVerticalLayoutGroup` 不同，
   硬套会算错），但**现在会印出 `cellSize`/`constraint`**（原来被 `m_Spacing` 那条泛型分支吃掉了）；
   `⚠️grid?` 标记照旧，**提醒那几行的子节点坐标不是「布局后」值**。

它还提了第三条（`AchievementsMenu.Setup` 在功能开关 `FeatureKey = "Achievements"` 关闭时会
`DestroyTab` **永久删掉页签**）—— 那是**原版行为**，我们不做功能开关，**不适用**。
