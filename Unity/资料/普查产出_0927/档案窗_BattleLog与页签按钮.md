# 档案窗 · `Battle Log Tab` 与 `Tab Buttons` · 原版普查（层 × 参数）

- 日期 **2026-09-27** · 只读普查（`资料/普查产出_0927/`，本文件是该页开工的**唯一尺子**）
- 正本骨架层 = `资料/阶段二_多人界面_原版规格.md` §2·1（**不重复**）
- 表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt <pid> --depth <n> --md`（**表体逐行抄录，未删行**）
- ✅ 自检：`python 工具/menu_dump.py --verify-layout` → `布局算法与正本 §2·1 的手算值逐位一致`
- 两处布局组均 `[ok]`（**没有 `⚠️unk`**）⇒ 表内布局值可直接照抄。

## ⚠️ 两条工具显示陷阱（本次踩到，写下来免得再踩）

1. **工具「字段:」那一列的名字是排序过的，不是声明顺序。**
   判据：`TabButtons` 显示成 `group,options,tabButtonPrefab`，而原始 JSON 键序是
   `tabButtonPrefab, options, group`（= ILSpy 桩的声明顺序）。**要声明顺序必须看原始 JSON 键序。**
2. **「贴图模式/颜色」列里 `Sliced (1,1,1,0)` 的 `(1,1,1,0)` 是【颜色】（此处 a=0），不是九宫格。**
   九宫格在 **sprite 那一列**（`九宫18,18,18,18` 或 `否`）。

---

## A. 层 × 参数

### A·1 `Battle Log Tab`（RectTransform pid `-574318498314159566`，depth 6）

```
# MonoScript 类名索引：5175 条
# （已沿 `m_Father` 爬父链：被查节点的父 = 「Tab Content」 351.03,118.92 → 1746.97,962.00）
# sprite 索引：真包 3316 条 + 切片缓存补齐 ⇒ 共 3316 条
# sprite 尺寸/九宫格索引：3200 条
| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 锚点 min→max | pivot | anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | 贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | Battle Log Tab | 351.03,118.92→1746.97,962.00 | 1395.94×843.08 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | **F** | BattleLogTab |  |  |  | 字段: holder,logPrefab |
| ·1 | Matches | 326.03,162.84→1771.97,904.48 | 1445.94×741.64 | (0,0)→(1,1) | (0.5,0.5) | (0,6.80032) | (50,-101.441) | T | Image,ScrollRect | Background 32×32 九宫10,10,10,10 ppu=200 | Sliced (1,1,1,0) |  | h=0 v=1 mode=1 inertia=1 elasticity=0.10000000149011612 decel=0.13500000536441803 |
| ··2 | Viewport | 326.03,162.84→1771.97,887.48 | 1445.94×724.64 | (0,0)→(1,1) | (0,1) | (0,0) | (0,-17) | T | Image,Mask | UIMask 32×32 九宫10,10,10,10 ppu=200 | Sliced (1,1,1,1) |  | showGraphic=0 |
| ···3 | Content | 326.03,162.84→1771.97,366.04 | 1445.94×203.20 | (0,1)→(1,1) | (0,1) | (0,0) | (0,203.2) | T | VerticalLayoutGroup,ContentSizeFitter |  |  |  | spacing=25.0 align=0 pad=0,0,0,0 ctrlW=1 ctrlH=0 expandW=1 expandH=1 scaleW=0 scaleH=0 ; 字段: m_HorizontalFit,m_VerticalFit |
| ····4 | Match Log | 326.03,162.84→1771.97,366.04 | 1445.94×203.20 | (0,1)→(0,1) | (0.5,0.5) | (722.968,-101.6) | (1445.94,203.2) | T | Image,BattleLogItem | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | 字段: defeatKey,defeatMaterial,drawKey,drawMaterial,enemyInfo,mode,pinButton,playerInfo,replayButton,result,victoryKey,victoryMaterial |
| ·····5 | Result | 924.00,178.84→1174.00,258.84 | 250.00×80.00 | (0.5,1)→(0.5,1) | (0.5,1) | (0,-16) | (250,80) | T | TextMeshProUGUI |  |  | 'Victory' 字号=65.3499984741211 auto[40.0~75.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) |  |
| ·····5 | Mode | 346.03,187.84→556.03,264.44 | 210.00×76.60 | (0,1)→(0,1) | (0,1) | (20,-25) | (210,76.6) | T | TextMeshProUGUI |  |  | 'Ranked mode' 字号=40.0 auto[20.0~40.0] 对齐=Left/Top 折行=1 色=(1,1,1,1) |  |
| ·····5 | ReplayButton | 1675.47,188.86→1740.47,253.86 | 65.00×65.00 | (1,0.5)→(1,0.5) | (0.5,0.5) | (-64,43.08) | (65,65) | T | Image,EverguildButton,EverguildButtonMaterialModifier | 40k_general_bt_yellow 71×71 否 | Simple (1,1,1,1) preserveAspect |  | trans=2 target=886216011377375794 interactable=1 ; 字段:  |
| ······6 | replayicon | 1675.47,188.86→1740.47,253.86 | 65.00×65.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,0) | (65,65) | T | Image,EverguildButtonMaterialModifier | 40k_general_bt_yellow_replay 71×71 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ·····5 | PinButton | 1597.60,188.86→1662.60,253.86 | 65.00×65.00 | (1,0.5)→(1,0.5) | (0.5,0.5) | (-141.87,43.08) | (65,65) | T | Image,EverguildButton,EverguildButtonMaterialModifier | 40k_general_bt_yellow 71×71 否 | Simple (1,1,1,1) preserveAspect |  | trans=2 target=-5713820606737909198 interactable=1 ; 字段:  |
| ······6 | pinicon | 1597.60,188.86→1662.60,253.86 | 65.00×65.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,0) | (65,65) | T | Image,EverguildButtonMaterialModifier | 40k_general_bt_yellow_pin replay 71×71 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ·····5 | Player Info | 326.03,162.84→1049.00,366.04 | 722.97×203.20 | (0,0)→(0.5,1) | (0.5,0.5) | (0,0) | (0,0) | T | BattleLogPlayerDisplay |  |  |  | 字段: armyImage,clanName,heroName,playerClickable,playerName,playerXp,ratingImage,skullCounter,skullImage |
| ······6 | Hero Name | 561.00,194.44→901.00,244.44 | 340.00×50.00 | (1,0.5)→(1,0.5) | (1,0.5) | (-148,45) | (340,50) | T | TextMeshProUGUI |  |  | 'Gazkull Thraka' 字号=45.0 auto[18.0~45.0] 对齐=Right/Middle 折行=0 色=(1,1,1,1) |  |
| ······6 | Player Name | 593.96,279.44→899.00,329.44 | 305.04×50.00 | (1,0.5)→(1,0.5) | (1,0.5) | (-150,-40) | (305.036,50) | T | TextMeshProUGUI |  |  | "Player's name" 字号=38.0 auto[12.0~38.0] 对齐=Right/Middle 折行=0 色=(1,1,1,1) |  |
| ······6 | Alliance Name | 636.00,316.44→886.00,366.44 | 250.00×50.00 | (1,0.5)→(1,0.5) | (1,0.5) | (-163,-77) | (250,50) | T | EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,EverguildTextMeshPro |  |  | '' 字号=30.0 auto[10.0~30.0] 对齐=Right/Midline 折行=1 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | Score | 454.54,279.44→570.99,329.44 | 116.45×50.00 | (0,0.5)→(0,0.5) | (0,0.5) | (128.51,-40) | (116.45,50) | T | TextMeshProUGUI |  |  | 'Gold IV' 字号=46.25 auto[12.0~50.0] 对齐=Left/Middle 折行=0 色=(1,1,1,1) |  |
| ······6 | Score Icon | 389.54,271.94→454.54,336.94 | 65.00×65.00 | (0,0.5)→(0,0.5) | (0,0.5) | (63.51,-40) | (65,65) | T | Image | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ······6 | Score Icon (1) | 335.71,277.52→389.54,331.35 | 53.83×53.83 | (0,0.5)→(0,0.5) | (0,0.5) | (9.68039,-40) | (53.8296,53.8295) | T | Image | 40k_DeckSelection_icon_FactionSororitas 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ······6 | Skulls | 887.52,254.44→987.52,354.44 | 100.00×100.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (250,-40) | (100,100) | T | Image,UIFlippable | 40K_missions_icon_Daily skulls 222×198 否 | Simple (1,1,1,1) preserveAspect |  | 字段: m_Horizontal,m_Veritical |
| ······6 | skullCounter | 872.41,325.79→934.00,354.44 | 61.59×28.65 | (1,0.5)→(1,0.5) | (1,0.5) | (-115,-75.677) | (61.5863,28.6458) | T | TextMeshProUGUI |  |  | 'x3' 字号=30.200000762939453 auto[18.0~45.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) |  |
| ·····5 | Enemy Info | 1049.00,162.84→1771.97,366.04 | 722.97×203.20 | (0.5,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | BattleLogPlayerDisplay |  |  |  | 字段: armyImage,clanName,heroName,playerClickable,playerName,playerXp,ratingImage,skullCounter,skullImage |
| ······6 | Hero Name | 1197.00,194.44→1557.00,244.44 | 360.00×50.00 | (0,0.5)→(0,0.5) | (0,0.5) | (148,45) | (360,50) | T | TextMeshProUGUI |  |  | 'Gazkull Thraka' 字号=45.0 auto[18.0~45.0] 对齐=Left/Middle 折行=0 色=(1,1,1,1) |  |
| ······6 | Player Name | 1214.00,279.44→1490.45,329.44 | 276.45×50.00 | (0,0.5)→(0,0.5) | (0,0.5) | (165,-40) | (276.45,50) | T | TextMeshProUGUI |  |  | 'Marneus Calgar' 字号=38.0 auto[12.0~38.0] 对齐=Left/Middle 折行=0 色=(1,1,1,1) |  |
| ······6 | Alliance Name | 1214.00,316.44→1552.00,366.44 | 338.00×50.00 | (0,0.5)→(0,0.5) | (0,0.5) | (165,-77) | (338,50) | T | EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,EverguildTextMeshPro |  |  | '' 字号=30.0 auto[10.0~30.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ······6 | Score | 1513.42,279.44→1629.87,329.44 | 116.45×50.00 | (1,0.5)→(1,0.5) | (1,0.5) | (-142.1,-40) | (116.45,50) | T | TextMeshProUGUI |  |  | '987 (+12)' 字号=35.849998474121094 auto[12.0~50.0] 对齐=Right/Middle 折行=0 色=(1,1,1,1) |  |
| ······6 | Score Icon | 1635.87,271.94→1700.87,336.94 | 65.00×65.00 | (1,0.5)→(1,0.5) | (1,0.5) | (-71.1,-40) | (65,65) | T | Image | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ······6 | Score Icon (1) | 1700.87,277.52→1754.70,331.35 | 53.83×53.83 | (0,0.5)→(0,0.5) | (0,0.5) | (651.87,-40) | (53.8296,53.8296) | T | Image | 40k_DeckSelection_icon_FactionSororitas 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ······6 | Skulls | 1110.48,254.44→1210.48,354.44 | 100.00×100.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-250,-40) | (100,100) | T | Image,UIFlippable | 40K_missions_icon_Daily skulls 222×198 否 | Simple (1,1,1,1) preserveAspect |  | 字段: m_Horizontal,m_Veritical |
| ······6 | skullCounter | 1162.38,325.79→1223.97,354.44 | 61.59×28.65 | (1,0.5)→(1,0.5) | (1,0.5) | (-548,-75.677) | (61.5863,28.6458) | T | TextMeshProUGUI |  |  | 'x3' 字号=30.200000762939453 auto[18.0~45.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) |  |
| ······6 | GameObject | 1225.48,264.44→1569.03,336.94 | 343.55×72.50 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-13.225,-36.25) | (343.55,72.5) | T | UIGenericEventCatcher,NonDrawingGraphic |  |  |  | 字段: OnClick,OnEnter,OnExit ; 字段:  |
| ·····5 | Details | 326.03,162.84→1771.97,366.04 | 1445.94×203.20 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T |  |  |  |  |  |
| ······6 | Sword | 999.00,254.44→1099.00,354.44 | 100.00×100.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,-40) | (100,100) | T | Image | 40k_main_bt_play 124×109 否 | Simple (1,1,1,1) preserveAspect |  |  |

⚠️ 布局组（子节点位置**由布局算**，上面已是**布局跑之后**的值）：
          Content → VerticalLayoutGroup [ok]
```

### A·2 `Tab Buttons`（RectTransform pid `1299990831746480690`，depth 5）

```
# MonoScript 类名索引：5175 条
# （已沿 `m_Father` 爬父链：被查节点的父 = 「Menu Area」 0.00,0.00 → 1920.00,1080.00）
# sprite 索引：真包 3316 条 + 切片缓存补齐 ⇒ 共 3316 条
# sprite 尺寸/九宫格索引：3200 条
| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 锚点 min→max | pivot | anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | 贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | Tab Buttons | 95.48,180.24→273.48,885.76 | 178.00×705.51 | (0,1)→(0,1) | (1,0.5) | (273.48,-533) | (178,705.512) | T | VerticalLayoutGroup,ToggleGroup,TabButtons |  |  |  | spacing=10.0 align=5 pad=0,0,0,0 ctrlW=0 ctrlH=1 expandW=1 expandH=1 scaleW=0 scaleH=1 ; 字段: m_AllowSwitchOff ; 字段: group,options,tabButtonPrefab |
| ·1 | Profile Button | 108.48,180.24→273.48,289.50 | 165.00×109.25 | (0,1)→(0,1) | (0.5,0.5) | (95.5,-54.626) | (165,109.252) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ··2 | button_bg | 108.48,180.24→273.48,289.50 | 165.00×109.25 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,1) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  |
| ··2 | Icon | 120.28,175.20→261.68,269.34 | 141.41×94.15 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (3.0169e-07,12.6) | (141.406,94.146) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_Profile_icon_profile 134×133 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段:  |
| ··2 | Label | 113.48,246.57→268.48,286.57 | 155.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0) | (0,-51.7) | (155,40) | T |  |  |  |  |  |
| ···3 | Tab Toggle Title | 113.48,246.57→268.48,286.57 | 155.00×40.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Profile' 字号=35.0 auto[10.0~35.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  ; 字段: maxFontSize,minFontSize |
| ·1 | Avatar Button | 108.48,299.50→273.48,408.75 | 165.00×109.25 | (0,1)→(0,1) | (0.5,0.5) | (95.5,-173.878) | (165,109.252) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ··2 | button_bg | 108.48,300.29→273.48,409.54 | 165.00×109.25 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.796906) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,0.71) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  ; 字段:  |
| ··2 | Icon | 113.48,293.46→268.48,389.58 | 155.00×96.11 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.4285e-06,12.6) | (155,96.115) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_Profile_icon_avatar 134×133 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段:  ; 字段:  |
| ··2 | Label | 113.48,364.12→268.48,404.12 | 155.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0) | (0,-50) | (155,40) | T |  |  |  |  |  |
| ···3 | Tab Toggle Title | 113.48,364.12→268.48,404.12 | 155.00×40.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Avatar' 字号=35.0 auto[10.0~35.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ·1 | Title Button | 108.48,418.75→273.48,528.00 | 165.00×109.25 | (0,1)→(0,1) | (0.5,0.5) | (95.5,-293.13) | (165,109.252) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ··2 | button_bg | 108.48,419.54→273.48,528.80 | 165.00×109.25 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.796906) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,0.71) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  ; 字段:  |
| ··2 | Icon | 120.83,407.62→261.13,493.06 | 140.30×85.44 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,23.035) | (140.296,85.438) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_Profile_icon_title 134×88 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段:  ; 字段:  |
| ··2 | Label | 113.48,493.06→268.48,524.98 | 155.00×31.92 | (0.5,0.5)→(0.5,0.5) | (0.5,0) | (0,-51.602) | (155,31.918) | T |  |  |  |  |  |
| ···3 | Tab Toggle Title | 113.48,493.06→268.48,524.98 | 155.00×31.92 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Title' 字号=33.650001525878906 auto[10.0~35.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ·1 | Battle Log Button | 108.48,538.00→273.48,647.25 | 165.00×109.25 | (0,1)→(0,1) | (0.5,0.5) | (95.5,-412.382) | (165,109.252) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ··2 | button_bg | 108.48,538.80→273.48,648.05 | 165.00×109.25 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.796906) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,0.71) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  ; 字段:  |
| ··2 | Icon | 120.28,532.86→261.68,626.20 | 141.41×93.34 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (3.0169e-07,13.1) | (141.406,93.341) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_Profile_icon_battlelog 134×133 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段:  ; 字段:  |
| ··2 | Label | 113.48,602.63→268.48,642.63 | 155.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0) | (0,-50) | (155,40) | T |  |  |  |  |  |
| ···3 | Tab Toggle Title | 113.48,602.63→268.48,642.63 | 155.00×40.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Battle Log' 字号=35.0 auto[10.0~35.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ·1 | Trophies | 108.48,657.25→273.48,766.50 | 165.00×109.25 | (0,1)→(0,1) | (0.5,0.5) | (95.5,-531.634) | (165,109.252) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ··2 | button_bg | 108.48,658.05→273.48,767.30 | 165.00×109.25 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.796906) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,0.71) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  ; 字段:  |
| ··2 | Icon | 120.28,653.41→261.68,746.75 | 141.41×93.34 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (3.0169e-07,11.8) | (141.406,93.341) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_Profile_icon_Trophies 134×133 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段:  ; 字段:  |
| ··2 | Label | 113.48,721.88→268.48,761.88 | 155.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0) | (0,-50) | (155,40) | T |  |  |  |  |  |
| ···3 | Tab Toggle Title | 113.48,721.88→268.48,761.88 | 155.00×40.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Achievements' 字号=32.75 auto[10.0~35.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ·1 | Ranked | 108.48,776.50→273.48,885.76 | 165.00×109.25 | (0,1)→(0,1) | (0.5,0.5) | (95.5,-650.886) | (165,109.252) | T | EverguildToggle |  |  |  | isOn=0 onSprite=-2307655919992762574 offSprite=1956647257489494794 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1) |
| ··2 | button_bg | 108.48,777.30→273.48,886.55 | 165.00×109.25 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.796906) | (0,0) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40K_settings_button 168×156 否 | Simple (1,0.572,0,0.71) ppuMul=0.9200000166893005 |  | 字段:  ; 字段:  ; 字段:  |
| ··2 | Icon | 120.28,777.30→261.68,864.41 | 141.41×87.11 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (3.0169e-07,10.276) | (141.406,87.1054) | T | Image,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段:  ; 字段:  |
| ··2 | Label | 113.48,841.13→268.48,881.13 | 155.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0) | (0,-50) | (155,40) | T |  |  |  |  |  |
| ···3 | Tab Toggle Title | 113.48,841.13→268.48,881.13 | 155.00×40.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI,Localize,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Ranking' 字号=35.0 auto[10.0~35.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |

⚠️ 布局组（子节点位置**由布局算**，上面已是**布局跑之后**的值）：
    Tab Buttons → VerticalLayoutGroup [ok]
```

**`Tab Buttons` 自身三个组件（原始 JSON 值）**：

- `VerticalLayoutGroup`：`m_ChildAlignment=5`、`m_Spacing=10`、`m_ChildControlWidth=0 / Height=1`、
  `m_ChildForceExpandWidth=1 / Height=1`、`m_ChildScaleWidth=0 / Height=1`、`m_Padding` 全 0。
- `ToggleGroup`：`m_AllowSwitchOff=0`。
- `TabButtons`：`tabButtonPrefab = PathID 0`（**空**）、`group = PathID 0`（**空**）、
  `options` = **6 条**（原始 JSON 键序 = 声明序 `tabButtonPrefab, options, group`）：

| # | tab（`WindowTabBase` MB pid） | tab 所在 GO | toggle（`EverguildToggle` MB pid） | toggle 所在 GO |
|---|---|---|---|---|
| 0 | 3020493274243496498 | `Profile Tab` | -3406534988526421454 | `Profile Button` |
| 1 | 7708307686690355762 | `Avatar Tab` | -7075133360644457934 | `Avatar Button` |
| 2 | -8260100404985038286 | `Title Tab` | 2788063858998802994 | `Title Button` |
| 3 | **-5860888418599994830** | **`Battle Log Tab`** | 5131782245350275634 | `Battle Log Button` |
| 4 | -4927132572019295694 | `Trophies Tab` | 9160916159536724530 | `Trophies` |
| 5 | 1724627060727839282 | `Ranking Tab` | -7088136748495242702 | `Ranked` |

同一份 6 元列表在 `PlayerProfileMenu.tabs`（MB pid `-4653119414410905038`，挂在 `Player Profile Window` 上）
里**逐位一致**（顺序 Profile→Avatar→Title→BattleLog→Trophies→Ranking）。

### A·3 六个键「结构是否完全一致」—— 逐个核过

**结构完全一致**：每键都是 `EverguildToggle` → 3 个子节点 `button_bg` / `Icon` / `Label`，
`Label` 下挂 `Tab Toggle Title`；组件种类与数量**除一处外**全同。**参数有 8 处不同**：

| 属性 | Profile Button | Avatar Button | Title Button | Battle Log Button | Trophies | Ranked |
|---|---|---|---|---|---|---|
| RT pid | -321469377245251022 | -6534062891430610382 | -3619431218348000718 | -9066613338944996814 | 8063267356681075250 | -4409287478960817614 |
| `button_bg` **m_Color.a** | **1.0** | 0.7098 | 0.7098 | 0.7098 | 0.7098 | 0.7098 |
| `button_bg` **`EverguildButtonMaterialModifier` 组件数** | **2** | **3** | 3 | 3 | 3 | 3 |
| `button_bg` anchoredPosition | **(0,0)** | (0,-0.796906) | 同左 | 同左 | 同左 | 同左 |
| `Icon` sizeDelta | 141.406×94.146 | **155×96.115** | 140.296×85.438 | 141.406×93.341 | 141.406×93.341 | 141.406×87.105 |
| `Icon` anchoredPosition.y | 12.6 | 12.6 | **23.035** | 13.1 | 11.8 | 10.276 |
| `Label` sizeDelta | 155×40 | 155×40 | **155×31.918** | 155×40 | 155×40 | 155×40 |
| `Label` anchoredPosition.y | **-51.7** | -50 | -51.602 | -50 | -50 | -50 |
| **`colorTintGreyOnAlpha`** | **1.0** | **1.0** | **1.0** | **0.5** | **0.5** | **0.5** |
| 标题文本 / Localize `mTerm` | Profile / `PlayerProfile/Profile` | Avatar / `PlayerProfile/Avatar` | Title / `PlayerProfile/Title` | Battle Log / `PlayerProfile/BattleLog` | Achievements / `Achievements/Achievements` | Ranking / `Ranked/RankingTab` |
| 字号（**autosize 缩后**，非手写值） | 35.0 | 35.0 | **33.65** | 35.0 | **32.75** | 35.0 |

**六位全同**（可当常量）：

- `button_bg`：`m_Sprite=1956647257489494794`（`40K_settings_button` 168×156）· `m_Type=0(Simple)` ·
  `m_PixelsPerUnitMultiplier=0.92` · `m_PreserveAspect=0` · `m_RaycastTarget=1` · `m_Maskable=1` · `m_Material=0`。
- `Icon`：`m_Type=0` · `m_PreserveAspect=1` · `m_PixelsPerUnitMultiplier=1.0` · `m_Color=(1,1,1,1)`；
  anchors 全 `(0.5,0.5)→(0.5,0.5)`、pivot `(0.5,0.5)`。
- `Label`：`m_AnchorMin=Max=(0.5,0.5)` · `pivot=(0.5,0)` · 宽恒 155；节点上**只有 `CanvasRenderer`**。
- `Tab Toggle Title`：anchors `(0,0)→(1,1)` · pivot `(0.5,0.5)` · pos `(0,0)` · sizeDelta `(0,0)` ·
  `enableAutoSizing=1` · `fontSizeMin=10` · `fontSizeMax=35` · `halign=2(Center)` · `valign=512(Middle)` ·
  **`m_TextWrappingMode=0`（不折行）** · `m_overflowMode=0` · `m_margin` 全 0 · `fontColor=(1,1,1,1)`。
- `EverguildToggle`：`m_IsOn=0` · `m_Interactable=1` · `m_Transition=2(SpriteSwap)` · `m_Navigation.m_Mode=3` ·
  `m_Colors.m_NormalColor=(1,1,1,1)` · `toggleTransition=1` · `graphic=0` ·
  **`m_Group=0（空，没连上 `Tab Buttons` 的那个 `ToggleGroup`）`** · `onValueChanged` 持久调用**空** ·
  `onColor=(1,1,1,1)` · `offColor=(0.75,0.75,0.75,1)` ·
  **`changeSpriteOnValueChange=1`** · **`colorTintOnValueChange=0`** · `colorTintGreyOnDisable=1` ·
  `onSprite=-2307655919992762574`（`40K_settings_button_hover` 168×156）·
  `offSprite=1956647257489494794`（= `button_bg` 当前用的那张）·
  `useDefaultAudioClipIfNull=1` · `allowClickWhenAlreadySelected=0` · `graphicsToAvoidSetMaterials=[]` ·
  `m_onClickSound` 六键指向**同一份**（`m_FileID=6, m_PathID=8881786964274599484`，**名字解不出**，见 §D5）·
  `label=0（空）` · `spriteToChange == m_TargetGraphic == 该键 button_bg 上的 Image 组件` ·
  `extraIcon == 该键 Icon 上的 Image 组件`。

> ⚠️ **别把两个 alpha 混成一条**：`button_bg.m_Color.a` 是 **1 vs 5** 的分法（只有 `Profile` 是 1.0）；
> `colorTintGreyOnAlpha` 是 **3 vs 3** 的分法（前三个 1.0、后三个 0.5）。**两组不是同一件事。**

### A·4 工具给不了的那部分（补列）

**① `activeSelf` 的出现条件（谁在运行期切）**

- 这两棵树里**唯一 `act=F`** 的是 `Battle Log Tab` 自己（pid `-574318498314159566`）。
- 切换者只有两处，都在 `WindowTabBase`：
  - `WindowTabBase__TryOpenTab.c`：`gameObject.SetActive(true)` 后调 vtable 上的 `OnOpen`（= `BattleLogTab.OnOpen`）。
  - `WindowTabBase__Toggle.c`：`gameObject.SetActive(参数)`。
    ⚠️ 该文件被反编译工具**贴错符号名**（内容与 ILSpy 桩 `WindowTabBase.Toggle(bool)` 对应，**以文件名/桩为准**）。
- 上游：`GameWindowWithTabs__OpenTabs.c`（`CurrentTab == null` 时先 `GetStartingTab()` 再走 vtable `+0x1a8` → `TryOpenTab()`）·
  `GameWindowWithTabs__ChangeTabCO.c` · `GameWindowWithTabs__SetupTabs.c` 结尾**无条件调 `ForceCloseTabs()`**
  ⇒ **原包里 `Battle Log Tab` 序列化成 F 是「`SetupTabs` 之后全关掉」的结果，不是美术标错。**
- 另：`GameWindowWithTabs__SetupTabs.c` 还会 `Components.TabHolder.SetActive(false)`；
  `TabButtons__Initialize.c` 开头会 `tabButtonPrefab.SetActive(false)`（**本包该字段为空，走不到**）。

**② `ppu ≠ 100 / ppuMul ≠ 1` 的图**

- 唯一一处：**六个键的 `button_bg`** `m_PixelsPerUnitMultiplier = 0.92`。
- 其余整两棵树里的 Image 全是 `1.0`；查到的 15 张 sprite 的 `m_PixelsToUnits` 全是 **100.0**。

**③ 九宫格 ≠ 全 0 的图**

- **只有一张**：`UI_Deck_Information_submenu_Back`，`m_Border=(18,18,18,18)`，用在 **`Match Log` 行根**上
  （`m_Type=1 Sliced`）。
- `40K_settings_button` / `_hover` / `40k_general_bt_yellow` / `..._replay` / `..._pin replay` /
  `40k_UI_icon_ranked_Skirmish` / `40k_DeckSelection_icon_FactionSororitas` / `40K_missions_icon_Daily skulls` /
  `40k_main_bt_play` / 五张 `40K_Profile_icon_*` —— **全部 border 全 0**。
- `Matches` / `Viewport` 两张的 sprite 没解出 ⇒ **九宫格无从判定**，只知道 `m_Type=1 (Sliced)`。

**④ `m_Offset ≠ 0` 的图：没有。** 上述 15 张 sprite 的 `m_Offset` 全 `(0,0)`。

---

## B. `logPrefab` 追查（**这一节对后面的「对局历史」最要紧**）

### B·1 它指向谁

`BattleLogTab` 组件（MonoBehaviour pid `-5860888418599994830`）的序列化值：

```
logPrefab = {"m_FileID": 0, "m_PathID": 8607776031950241599}
holder    = {"m_FileID": 0, "m_PathID": 51921009397299762}
```

- `logPrefab` **不是一个 GameObject，是一个 MonoBehaviour**：
  `MonoBehaviour_8607776031950241599.json`，其 `m_Script` → `MonoScript_2634013110552706461.json` 的
  **`m_ClassName = "BattleLogItem"`**（类型对得上 ILSpy 桩 `[SerializeField] private BattleLogItem logPrefab;`）。
- 该 MB 的 `m_GameObject = -1368012925348282561`，其 **RectTransform = `-5579061773596819649`，`m_Father = 0`**
  ⇒ **它是一棵「独立 prefab 资产」的根**。
- 🔴 **同名两份，别取错**：本包里有**两个**都叫 `Match Log` 的 GameObject ——
  · `GameObject/Match Log_-1368012925348282561.json` ← **prefab 资产根**，RT `-5579061773596819649`，`m_Father=0`。
    **这才是 `logPrefab` 指的那个。**
  · `GameObject/Match Log.json` ← **`Content` 底下的场景实例**，RT `-207524765956736462`，`m_Father = 51921009397299762`。
  **两棵树的节点 pid 完全不同**（不是同一个对象）。
- `holder` = `51921009397299762` → RT，GO 名 = **`Content`**（也就是 §A·1 那棵的 `Content`），
  **与 `ScrollRect.m_Content` 是同一个 pid** ⇒ 确认 `holder == Content`。

### B·2 那个 prefab 自己那棵树

`python 工具/menu_dump.py bundle_menus_assets_all --rt -5579061773596819649 --depth 5 --md`
（**就在同一个 bundle**；跨 84 个 bundle 核过：该 MB 与那个 RT 都**只在 `bundle_menus_assets_all`**。）

```
Match Log                       [Image(UI_Deck_Information_submenu_Back, Sliced 九宫18,18,18,18), BattleLogItem]
├─ Result                       TextMeshProUGUI   'Victory' 字号=65.35 auto[40~75] 对齐 2/512
├─ Mode                         TextMeshProUGUI   'Ranked mode' 字号=40 auto[20~40] 对齐 1/256
├─ ReplayButton                 Image(40k_general_bt_yellow 71×71) + EverguildButton + MM
│   └─ replayicon               Image(40k_general_bt_yellow_replay 71×71) + MM
├─ PinButton                    Image(40k_general_bt_yellow 71×71) + EverguildButton + MM
│   └─ pinicon                  Image(40k_general_bt_yellow_pin replay 71×71) + MM
├─ Player Info                  BattleLogPlayerDisplay（字段 armyImage,clanName,heroName,playerClickable,playerName,playerXp,ratingImage,skullCounter,skullImage）
│   └─ Hero Name / Player Name / Alliance Name / Score / Score Icon / Score Icon (1) / Skulls / skullCounter
├─ Enemy Info                   BattleLogPlayerDisplay（同上字段）
│   ├─ …同上八个
│   └─ GameObject               UIGenericEventCatcher + NonDrawingGraphic   ← 挂在 Enemy Info 下
└─ Details
    └─ Sword                    Image(40k_main_bt_play 124×109)
```

**prefab 根 RT 参数**：anchors `(0,1)→(0,1)` · pivot `(0.5,0.5)` ·
`anchoredPosition=(737.5,-101.6)` · `sizeDelta=(1400,203.2)`。

**与场景实例的差异**：场景里 `Match Log` 的 `sizeDelta.x = 1445.94`（= `Content` 的宽）、
`anchoredPosition.x = 722.968` —— 那是 `Content` 的 `VerticalLayoutGroup`（`ctrlW=1, expandW=1`）
**把子宽强制拉满**的结果；`1400` 只是模板里的手写宽度。
**`ctrlH=0` ⇒ 行高由 prefab 自己的 `203.2` 决定**，不受布局影响。`spacing=25` ⇒ **行距 25**。

> ⚠️ 施工提示：`BattleLogTab.OnOpen` 会先 `DestroyAllChildren(holder)` ⇒
> **场景里 `Content` 底下那个 `Match Log` 实例在运行期必被销毁**。它只是美术的原位参照，
> **真正可见的行来自 `Instantiate(logPrefab)`**。

### B·3 `BattleLogTab__*.c` 怎么用它生成行

该类的反编译产物**只有 5 个**：`__.ctor.c` · `__OnOpen.c` · `__c__.cctor.c` · `__c__.ctor.c` ·
`__c___OnOpen_b__2_0.c`。

伪码（字段偏移按 `logPrefab@0x30 / holder@0x38` 对齐）：

```
uVar1 = PlayerDataManager.battleLogData;          // 单例 static 字段 @+0x2c0
SupportMethods.DestroyAllChildren(holder, false); // 先清空 Content
foreach (var m in battleLogData.Where(<OnOpen>b__2_0))
    Instantiate(logPrefab, holder);               // Instantiate(original, parent)
    BattleLogItem.Initialize(instance, m);
```

- **Instantiate 的位置**：对每个过滤后的条目 instantiate 一次，**无对象池、无复用**。
- **塞进 `holder` 的哪个子节点**：作为 `Content` 的**直接子节点**（落位由 `Content` 的 `VerticalLayoutGroup` 排）。
- **一行几个**：**每个 `BattleLogMatch` 一条行**，竖向顺序追加，**不限条数**（全量 `foreach`）。
- **过滤条件**（`BattleLogTab.__c___OnOpen_b__2_0.c`）：`BattleLogMatch.ownHeroName`（`+0x40`）
  为 null/空白 → 丢弃；再要求 `ownHeroName != <一个 System.String 字面量>`（**未能解出**，见 §D2）。
- `BattleLogTab` **只重写了 `OnOpen()`**，其余（`Awake/Setup/OnSetup/CloseTab/OnClose/Toggle/ToFocus`）全继承 `WindowTabBase`。

### B·4 行上挂了什么 / 哪类数据驱动

- 行根：`Image`（九宫格底）+ **`BattleLogItem`**。
- 行内：`BattleLogPlayerDisplay` ×2（`Player Info`/`Enemy Info`）· `EverguildButton` ×2（`ReplayButton`/`PinButton`，
  各自另挂 `EverguildButtonMaterialModifier`）· `UIGenericEventCatcher` ×1 · `UIFlippable` ×2（`Skulls`）·
  `TextMeshProUGUI` 若干。
- **驱动数据 = `BattleLogMatch`**（`[Serializable]` 普通类）：`season / enemyId / matchWin / matchLose /
  ownExp / enemyExp / ownTotalExp / enemyTotalExp / ownGlobalRating / enemyGlobalRating / ownHeroName /
  enemyHeroName / ownName / enemyName / ownSkulls / enemySkulls / recordingIndex / matchType /
  replaysVersion / pinned / playerClan / enemyClan / botAltid` + 两个计算属性 `RankedRating` / `EnemyRankedRating`。
  来源 = **`PlayerDataManager.battleLogData`**（单例字段 `+0x2c0`）。
- 每行的两个 `BattleLogPlayerDisplay` 收 `BattleLogPlayerData`（record struct），由 `BattleLogItem.Initialize`
  里两个 12 字段的临时结构体装填。
- `BattleLogItem.Initialize` 读的 `BattleLogMatch` 偏移：
  `+0x40`=ownHeroName · `+0x50`=ownName · `+0x88`=playerClan · `+0x2c`=ownTotalExp · `+0x60`=ownSkulls ·
  `+0x70`=matchType · `+0x80`=**pinned** · `+0x68`=**recordingIndex** · `+0x78`=replaysVersion。
- 行级交互：
  - `ClickReplayButton` → `PlayerDataManager.RecoverMatchRecording(recordingIndex, 回调)`
  - `ClickPinButton` → 先 `pinButton.interactable=false`，再 `PinMatchRecording(recordingIndex, !pinned, onPinned, onError)`
  - `SetPinState` → 设 `pinButton.image.color`：**未钉 = `(1,1,1,1)`，已钉 = `(0,1,0,1)`（绿）**
    （两个常量由 `工具/read_literal.py` 从 `GameAssembly.dll` 实读：
    `0x1834b2e50..5c = 1,1,1,1`；`0x1834b2c00..0c = 0,1,0,1`）
  - 结果色：`Initialize` 用 `pinned` 选 `victoryMaterial`/`defeatMaterial`/`drawMaterial` 之一 +
    `victoryKey`/`defeatKey`/`drawKey`（`LocalizedString`，mTerm = `Battle/BattleEnd/Victory` /
    `Battle/BattleEnd/Defeat` / `Battle/BattleEnd/Draw`）
  - `OnDestroy`：摘掉 `replayButton`/`pinButton` 上 `Initialize` 时加的监听

### B·5 第二个使用者：`BattleLogPopup`（做「对局历史」时多半要用它）

`d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleLogPopup.cs`：`class BattleLogPopup : GameWindow`，
字段 `logPrefab` / `holder` / `closeButton`。序列化值（`MonoBehaviour_-845058224705724360.json`）：

```
logPrefab   = {"m_FileID": 0, "m_PathID": 8607776031950241599}   ← 与 BattleLogTab 同一个！
holder      = {"m_FileID": 0, "m_PathID": 4975276729081765944}
closeButton = {"m_FileID": 0, "m_PathID": 8586477743801671736}
```

- 所在 GO `Battle Log Popup`（pid `7408828764512624696`），其 RT `-9126652862212956104`，**`m_Father = 0`** ⇒ 也是独立 prefab 根。
- `BattleLogPopup.holder` 的 `m_Children = []`（**空的**，运行期才填）。
- `BattleLogPopup__Open.c` 与 `BattleLogTab__OnOpen.c` **逐句同构**（`GameWindow.Open()` → 取 `battleLogData` →
  `foreach` `Instantiate` → `BattleLogItem.Initialize`）。
- **判决：行模板 `logPrefab` 全游戏只有一份，Tab 与 Popup 共用；区别只在 `holder` 与过滤条件
  （Popup 的 `Open` 里没有 `Where` 过滤，也没有 `DestroyAllChildren`）。**

---

## C. 入口 / 它调用的 / 谁监听它

### C·1 `BattleLogTab`

**入口（谁开它）**

- 序列化接线：`TabButtons.options[3].tab` = `-5860888418599994830`（`TabButtons` 组件 MB pid `-6958473972802684366`）；
  `PlayerProfileMenu.tabs[3]` = 同一个 pid。
- 运行期开：`WindowTabBase.TryOpenTab()`（`SetActive(true)` + 调 vtable `OnOpen`）
  ← `GameWindowWithTabs.OpenTabs()`（走 vtable `+0x1a8`）← `GameWindowWithTabs.Open()`；
  或 ← `TabButtons.Toggle(state, tab)` → `GameWindowWithTabs.ChangeTab(tab)` → `ChangeTabCO` → `TryOpenTab`。
- 关：`WindowTabBase.Toggle(false)` / `CloseTab()`；`CloseTab` 的调用者 =
  `GachaEventTab__Initialize.c` · `ExpansionPassMissionsTab__RefreshMissions.c` ·
  `SelectDecksTab__ToggleSelectGameModeDeck.c` · `GameWindowWithTabs__ChangeTabCO.c` ·
  `GameWindowWithTabs__Close.c` · `GameWindowWithTabs__ForceCloseTabs.c`。

**它调用的（归类）**：`Object.Instantiate(BattleLogItem, Transform)`（每行一次）·
`BattleLogItem.Initialize(BattleLogMatch)` · `SupportMethods.DestroyAllChildren(Transform, false)` ·
`Enumerable.Where<T>` + 捕获的静态委托 · `PlayerDataManager.battleLogData`（**只读**）。

**谁监听它**：`BattleLogTab` **不触发任何事件、不写任何静态量、没有 Poll 者**。
唯一静态副作用是 `BattleLogTab.__c__` 的 `<>c` 单例与缓存委托。

**入口侧的 `TabButtons`（页签容器）**

- 入口：`GameWindowWithTabs.SetupTabs()` → `Components.TabButtons.Initialize(window)`。
- `TabButtons.Initialize` 做三件事：① `tabButtonPrefab.gameObject.SetActive(false)`（本包该字段为空，跳过）；
  ② `window.OnTabChanged += <Initialize>b__0`；③ 遍历 `options`：给每个 `toggle.set_group(group)`，
  再 `onValueChanged.RemoveListener(<Initialize>b__1)` 然后 `AddListener(<Initialize>b__1)`。
- `TabButtons.Toggle(state, tab)`：`state && tab != null` → `window.ChangeTab(tab)`。
- `TabButtons.UpdateTab(tab)`：遍历 `options`，`toggle.isOn = (options[i].tab == tab)`。
- `TabButtons.ToggleInteractable(tab, state)`：**调用者只有 2 处**
  （`BaseParentEventWindow__CreateEventTab.c` · `ShopWindow__CreateStoreTab.c`）
  ⇒ **档案窗这 6 个键在运行期没人改 `interactable`**。
- `AddTabButton` / `AssignTabButton` / `RemoveTabButton` 的调用者只有 `BaseParentEventWindow__SetupTabs.c`
  与 `GameWindowWithTabs__CreateTabButton.c` ⇒ **档案窗不走动态建键那条路**（6 个键是手摆的）。

### C·2 `EverguildToggle`

**入口**

- `Awake()`：`base.Awake()` → `EverguildButtonHelper.GetGraphicsInChildren(...)` → 若 `colorTintOnValueChange`
  则 `Selectable.set_colors(...)` 并把 `ToggleTint` 挂到 `onValueChanged` → 若 `changeSpriteOnValueChange`
  则把 `ToggleSprite` 挂到 `onValueChanged` **并立即调一次**。
- `OnPointerClick(PointerEventData)`：`if (!interactable) return;` → `if (!allowClickWhenAlreadySelected && isOn) return;`
  → `base.OnPointerClick()` → 播 `m_onClickSound` → 最后 `invoke(OnClick 委托)`。
- `OnPointerEnter` / `OnPointerDown` / `DoStateTransition(SelectionState, bool)` 都是 override。

**它调用的**：`EverguildButtonHelper.GetGraphicsInChildren` / `DoMaterialRefresh` ·
`EverguildButtonMaterialModifier.get_MyGraphic` · `CanvasRenderer.SetColor` · `Image.set_sprite` ·
`Selectable.set_colors` / `set_isOn` / `set_interactable` · `SoundManager.Play2D` ·
自身 `ToggleTint(bool)` / `ToggleSprite(bool)` / `ToggleGreyscale(bool)` / `RefreshVisuals()`（公开）。

**谁监听它**

- 它**触发**两样：① `Toggle.onValueChanged` —— 监听者 = `TabButtons.Initialize` 装的闭包 + `Awake` 自装的
  `ToggleTint`/`ToggleSprite`；② `public event Action OnClick` ——
  文件名 grep `EverguildToggle__add_OnClick` 的调用者**只有** `BaseParentEventWindow__SetupTabs.c`
  ⇒ **档案窗这 6 个键没有任何 `OnClick` 订阅者**（点击行为完全由 `onValueChanged` 承担）。
- **不写任何静态量；没有 Poll 者**，全靠 `UnityEvent` 回调。

> ⚠️ **反编译产物缺失的判据**（10·2 要求）：
> `TabButtons.__c__DisplayClass6_0___Initialize_b__0.c` 与 `..._b__1.c` **内容逐字相同**，
> 而 `Initialize` 明显装了**两个不同**委托 ⇒ **`b__1` 的产物被 `b__0` 覆盖，方法体缺失**。
> 旁证：`grep -rl "TabButtons__UpdateTab"` 只命中它自己，而桩里 `UpdateTab` 是存在的
> ⇒ **它是只经委托调用的方法，不能下「没人调」的结论**。
> 本次同类「符号被贴错名」还有 3 处（**以文件名 / ILSpy 桩为准**）：
> `BattleLogTab__.ctor.c` 印成 `OnClickDestroy___ctor`；`WindowTabBase__Toggle.c` 印成 `Card2DController__Toggle`；
> `EverguildToggle__get_Label.c` 印成 `NetworkingPeer__get_LocalPlayer`（返回 `this+0x198`，即 `label`）。

---

## D. 查不到的（**不猜**）

| # | 查不到什么 | 搜过哪些词 | 判据 / 目录 |
|---|---|---|---|
| ~~D1~~ ✅**2026-09-27 已解出** | ~~`Matches` 与 `Viewport` 两张 sprite 的名字/尺寸/九宫格~~ **更正：这两张是 UGUI 内置图 —— `1660267235368898380` → `Background`、`-426171492875694260` → `UIMask`（都 32×32 · 九宫 10,10,10,10）。** **错因**：当时那版 `工具/menu_dump.py` 只有「读解包目录切片缓存」一条路，而这两张是**跨文件引用**（`m_FileID=15`）、不在本包 ⇒ 解不出。上一版（2026-09-23 建）本来有「UnityPy 扫真包 `o.path_id`」那条路，**被我 2026-09-27 重写时弄丢了，当天补回**（缓存 `_tmp_view/sprite_pids_ALL.json`，3316 条）。⚠️ 尺寸/九宫格是**按名字**再查的 ⇒ 通用名（`Background`/`UIMask`）有撞名风险，留个心眼。 | 两个 pid 字符串；按 `pathid` 字段匹配 | `d:/2/Warpforge_tools/data/ui_extract/*/Sprite/*.json` · `素材/Warpforge原版/UI图集/图集/*/Sprite/*.json` · `assets_full/bundle_*/Sprite/*.json`（含工具缓存 `_tmp_view/_menu_sprite_idx.json`）**全 0 命中**。判据：两者的 `m_Sprite` 都是 **`m_FileID=15`（跨文件引用）**，**不在本包** ⇒ 那是别的 bundle 的局部 pid，**本工具链不追外部引用**。`m_Type` 已知 = 1(Sliced)。 |
| D2 | `BattleLogTab.OnOpen` 过滤用的字符串常量 `DAT_1842b6638`（`ownHeroName` 要排除的那个值） | `DAT_1842b6638` / `1842b6638` / `ownHeroName` | `decomp_full/` · `Warpforge_code/Scripts/Assembly-CSharp/` · `d:/2/tools/all_strings.txt`。判据：`_DAT_` 是 `System.String*` 的 .data 槽位，运行期才由 il2cpp 元数据解析；`工具/read_literal.py` 只能读 4 字节数值（读出来是指针不是字符）；`all_strings.txt` **没有「地址 ↔ 字面量」映射** ⇒ **无法定位**。 |
| D3 | `BattleLogItem.Initialize` 里选 `victoryMaterial` 后如何落到文字上（`set_fontMaterial` 之前那次 `FUN_1814931f0(...)` 是什么） | `BattleLogItem__Initialize` / `FUN_1814931f0` | `decomp_full/`。判据：该 helper **无符号名**（未导出为具名方法），桩里也没有对应 C# 成员 ⇒ 只能确认它产出 `LocalizedString` 传给 `set_fontMaterial`，**中间步骤不可考**。 |
| D4 | `Content` 上 `ContentSizeFitter` 的 `m_HorizontalFit` / `m_VerticalFit` 的值 | 两个字段名 | `bundle_menus_assets_all/MonoBehaviour/*.json`。判据：序列化 JSON 里**这两个字段存在但没有可读值**；ILSpy 桩里 `ContentSizeFitter` 属 `UnityEngine.UI`，**不在 `Assembly-CSharp` 桩集内** ⇒ 原版值无法从解包确认。**不要照猜。** |
| D5 | 六个键的 `m_onClickSound`（`m_FileID=6, m_PathID=8881786964274599484`）是哪个 `AudioCue` 资产 | 该 pid / `AudioCue` | `assets_full/bundle_*/`（含 `bundle_audio*`）· `d:/2/Warpforge_tools/data/`。判据：`m_FileID=6` 是跨文件引用，**工具链不解析 `m_Externals` 表** ⇒ 只知「六个键共用同一份 cue」，**不知其名**。 |
| D6 | 六个键里为什么 `Profile` 的 `EverguildButtonMaterialModifier` 少一个、`colorTintGreyOnAlpha` 是 3/3 而非 1/5 | 组件计数 · `graphicsToAvoidSetMaterials` · `EverguildButtonHelper__GetGraphicsInChildren` | 判据：`EverguildButtonMaterialModifier` 的序列化 JSON **只有 `m_GameObject/m_Script/m_Enabled/m_Name`（0 个自定义字段，全默认值）** ⇒ 少挂一个**在当前数据下没有可观测差异**；**这是「作者漏挂/多挂」而非可推导的逻辑**，原版意图**查不到，不编**。 |

**同名多实例怎么确认没取错**：`Profile Tab` 在本包**只有一个**（`find_go_by_name` 返回 1 个）；
`ChooseNameWindow` 有 2 个（本页那个 `m_Father = Profile Tab`，另一个 `m_Father = 0`，与本窗无关）；
`Edit Name Button` 全包 6 个（本页 2 个）；`playerIdText` 全包 2 个**都在本页**。工具按 `--rt` 走，
**双向校验过 `m_GameObject`**。

---

## 附：本次用到的原始出处一览

- 树/参数：`python 工具/menu_dump.py bundle_menus_assets_all --rt <pid> --depth <n> --md`
- 类名：`assets_full/bundle_Waprforge_monoscripts/MonoScript/MonoScript_*.json` 的 `m_ClassName`
- 序列化 PPtr：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_{-5860888418599994830, -6958473972802684366,
  -845058224705724360, 8607776031950241599}.json`
- 类签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/{BattleLogTab,BattleLogItem,BattleLogPopup,
  BattleLogMatch,BattleLogPlayerDisplay,TabButtons,EverguildToggle,WindowTabBase,GameWindowWithTabs,
  EverguildButton,EverguildButtonMaterialModifier}.cs`
- 反编译正本：`d:/2/tools/decomp_full/` 下 `BattleLogTab__OnOpen.c` · `BattleLogTab.__c___OnOpen_b__2_0.c` ·
  `BattleLogPopup__Open.c` · `BattleLogItem__{Initialize,ClickReplayButton,ClickPinButton,SetPinState,OnDestroy}.c` ·
  `TabButtons__{Initialize,Toggle,UpdateTab,ToggleInteractable,AssignTabButton}.c` ·
  `EverguildToggle__{Awake,OnPointerClick,DoStateTransition,ToggleTint,ToggleSprite,RefreshVisuals}.c` ·
  `WindowTabBase__{TryOpenTab,Toggle}.c` · `GameWindowWithTabs__{SetupTabs,OpenTabs,ChangeTab_object_}.c`
- 数值常量：`工具/read_literal.py`（从 `D:/2/unity_run_ref/GameAssembly.dll` 实读）
