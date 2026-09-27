# `Ranked Division Info`（实例 ③，`RankedEventWindowV2` 下）—— 只读普查

> **产出**：2026-09-27 只读普查子代理。**未改任何工程代码**（只落了本文件）。
> **命令（三条，同一棵树）**：
> ① `cd d:/4/Unity && python 工具/menu_dump.py bundle_menus_assets_all "RankedEventWindowV2" --depth 7 --md`
> ② 同 ① 但 `--depth 10`（**只多出 4 行**：两个 `RankedSealStep` 各自的 `Empty`+`Fill` —— depth 7 恰好看不到它们）
> ③ 同 ② 再 `--relative --md`（**与 ② 逐字节相同**：根 `RankedEventWindowV2` 的绝对矩形起点就是 `0,0`，所以「相对根」≡「绝对」⇒ 本页只需一张表）
> **工具自检**：`python 工具/menu_dump.py --verify-layout` → 14 条全 ✅，末行「✅ 布局算法与正本 §2·1 的手算值逐位一致（含纵轴对齐回归用例）」。
> ⚠️ 该自检**覆盖不到本页**（它验的是 `Tab Buttons` 那种样本，见 `工具/menu_dump.py:390` 那段注释）⇒ **自检绿 ≠ 本页全对**，本页专属的两条算不准在 §A·4。
> **工具版本**：2026-09-27「布局组先滤 `m_IsActive=0` 子节点」已修版 —— 本页 `footer` 下两个 inactive 子节点（`Highest Faction Rating` / `FactionScoreSmall`）**已正确排除**（否则 `footer` 的排布会全偏）。
> **数据源**：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（原始 JSON，逐条回读核对）。
> **边界**：本页只管 `Ranked Division Info` 那一支；`RankedEventWindowV2` 的其它 8 支（背景/页头/选卡组/选阵营/Battle!/DEBUG/搜索中/Help）**不在本文范围**。

---

## §0 怎么确认取的是实例 ③（不是 ①/②）

`菜单全树.md` 里同名三处：

| 行 | 缩进 | 矩形 | 谁是父 |
|---|---|---|---|
| `:4930` | 0（顶层根） | `[105,43 562x864]` | 无 |
| `:10183` | 2 | `[252,213 434x694]` | `RankedEventWindow` |
| `:13931` | 2 | `[0,147 638x812]` | `RankedEventWindowV2` |

**独立复算证明**（不依赖 `菜单全树.md`）：按名字取三条 GO，再解 `m_Father` 父链、按 `工具/menu_rect.py` 的 uGUI 公式（`rect_of`）算绝对矩形：

| GO pid | 父链（`m_Father` 爬上去） | 复算矩形 | 对上 |
|---|---|---|---|
| `-3383320362765802140` | （只有自己 = 顶层根） | —— | ① `:4930` |
| `-2472178969530752299` | `RankedEventWindow` | `251.86,213.07→686.14,907.49`（434.28×694.42） | ② `:10183` |
| **`2414930959176796080`** | **`RankedEventWindowV2`** | **`0.00,146.93→638.00,959.07`（638.00×812.13）** | **③ `:13931`** ✅ |

⇒ 本页一律用 **GO `2414930959176796080` / RT `-5443768692855109712`**（`m_IsActive=True`）。
旁证：根 GO `-5324570929900320848`（类 `RankedEventWindowV2`）的序列化字段 `rankingDisplay` 正指向这个 GO（同一个 GO 上只挂 `RectTransform` + 一个 `RankingDisplay` MonoBehaviour，pid `-2583548330168842320`）。

---

## §A 「层 × 参数」表（`Ranked Division Info` 那一支 = **55 个节点**；工具输出逐行照抄，未删行未概括）

照抄说明：`#` 三行是索引统计；`缩进 0` 那行是根；**中间省略的 8 支**不是本文范围，已删；`·1` 行起为本文主题。

```
# MonoScript 类名索引：5175 条
# sprite 索引：真包 3316 条 + 切片缓存补齐 ⇒ 共 3316 条
# sprite 尺寸/九宫格索引：3200 条
```

| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 锚点 min→max | pivot | anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | 贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | RankedEventWindowV2 | 0.00,0.00→1920.00,1080.00 | 1920.00×1080.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | RankedEventWindowV2 |  |  |  | 字段: armySelector,backgroundCloseButton,battleButton,boostedColor,buttonShieldIcon,buttonTrophyIcon,changeRankedToggle,closeOnESC,closeSound,debugPlayPrefab,debugRankedIdText,extraScaleSmallScreen,header,helpButton,leaderboardButton,openSound,playButtonDebug,rankedDeckSelector,rankingDisplay,rankingPrefab,rankingPrefabClassic,shieldedColor,type,updateNavPanel,useDefaultCloseSoundIfNull,windowsPlacement |
| ·1 | Ranked Division Info | 0.00,146.93→638.00,959.07 | 638.00×812.13 | (0,0.5)→(0,0.5) | (0.5,0.5) | (319,-13) | (638,812.134) | T | RankingDisplay |  |  |  | 字段: displayPosition,displayRating,displaySeals,divisionImage,divisionText,positionDisplay,ratingDisplay,ratingHolder,sealCountDisplay,timerDisplay |
| ··2 | Rank Title | 58.00,161.33→580.00,216.02 | 522.01×54.68 | (0.5,1)→(0.5,1) | (0.5,1) | (0,-14.4) | (522.005,54.683) | T | EverguildTextMeshPro,Localize |  |  | 'Rank' 字号=57.70000076293945 auto[18.0~72.0] 对齐=Center/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary |
| ··2 | info | 550.37,228.18→605.60,283.42 | 55.23×55.23 | (1,0.5)→(1,0.5) | (1,0.5) | (-32.4,297.2) | (55.2302,55.231) | **F** | Image,EverguildTooltipTrigger | 40K_generic_bt_info 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段: localize,offset,preventPassingClickEventToParent,registerEvents,text,title,tooltipAnchor,tooltipPrefab |
| ··2 | LeaderboardButton | 140.46,965.97→497.54,1040.82 | 357.09×74.85 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,-450.4) | (357.086,74.85) | T | Image,EverguildButton,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,FeatureChecker | UI_Button_Mulligan 410×124 九宫333,96,333,96 | Simple (1,1,1,1) |  | trans=2 target=4738450649869944752 interactable=1 ; 字段:  ; 字段:  ; 字段: OnFeatureChecked,featureKey |
| ···3 | Button Text | 156.14,973.30→480.71,1033.48 | 324.57×60.18 | (0.0355112,0.099)→(0.961262,0.903) | (0.5,0.5) | (0,-0.0606194) | (-6,0) | T | TextMeshProUGUI,Localize,AspectRatioFitter,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Leaderboard' 字号=36.0 auto[10.0~36.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: m_AspectMode,m_AspectRatio ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  ; 字段: maxFontSize,minFontSize |
| ··2 | Content | 65.05,222.75→572.95,886.57 | 507.90×663.82 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0.000106812,-1.659) | (507.9,663.824) | T | VerticalLayoutGroup ⚠️unk |  |  |  | spacing=0.0 align=1 pad=0,0,0,0 ctrlW=0 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 |
| ···3 | RankTitleBG | 90.40,222.75→547.60,296.45 | 457.20×73.70 | (0,1)→(0,1) | (0.5,0.5) | (253.95,-36.85) | (457.2,73.7) | T | Image,LayoutElement | 40K_main_rank_display 315×64 否 | Simple (1,1,1,0.918) |  | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | DivisionText | 141.30,225.61→496.70,293.59 | 355.39×67.98 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,0) | (355.393,67.98) | T | TextMeshProUGUI,EverguildTextController |  |  | 'Division V' 字号=42.0 auto[10.0~42.0] 对齐=Center/Midline 折行=0 色=(0.961,0.914,0.737,1) 字距=-2.6 | 字段: maxFontSize,minFontSize |
| ···3 | DivisionImage | 49.10,234.04→588.90,806.66 | 539.79×572.62 | (0,1)→(0,1) | (0.5,0.5) | (253.95,-297.6) | (539.795,572.62) | T | Image,DivisionImageDisplay,LayoutElement | <无图> | Simple (1,1,1,1) preserveAspect |  | 字段: divisionImage,rankImage ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | RankImage | 265.02,413.82→372.98,463.09 | 107.96×49.26 | (0.4,0.6)→(0.6,0.7) | (0.5,0.5) | (0,-4) | (0,-8) | T | Image | Roman V 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ···3 | Spacer | 240.20,296.45→397.80,717.95 | 157.60×421.50 | (0,1)→(0,1) | (0.5,0.5) | (253.95,-284.45) | (157.6,421.5) | T | LayoutElement |  |  |  | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ···3 | footer | 114.00,717.95→524.00,717.95 | 410.00×0.00 | (0,1)→(0,1) | (0.5,0.5) | (253.95,-495.2) | (410,0) | T | VerticalLayoutGroup,LayoutElement |  |  |  | spacing=11.199999809265137 align=1 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | Highest Faction Rating | 141.28,717.95→496.72,757.75 | 355.44×39.80 | (0,1)→(0,1) | (0.5,0.5) | (205,-19.8999) | (355.439,39.7997) | **F** | Image,HorizontalLayoutGroup,LayoutElement ⚠️unk | 40K_main_rank_display 315×64 否 | Simple (1,1,1,1) |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Rating Text | 319.00,708.26→319.00,767.43 | 0.00×59.17 | (0,1)→(0,1) | (0.5,0.5) | (177.719,-19.8999) | (0,59.167) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 319.00,737.85→379.00,737.85 | 60.00×0.00 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (60,0) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 379.00,737.85→439.00,737.85 | 60.00×0.00 | (0,1)→(0,1) | (0.5,0.5) | (90,-29.5835) | (60,0) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 439.00,737.85→439.00,737.85 | 0.00×0.00 | (0,1)→(0,1) | (0,0.5) | (120,-29.5835) | (0,0) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '4879' 字号=45.0 auto[18.0~45.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | FactionScoreSmall | 141.28,717.95→496.72,835.66 | 355.44×117.71 | (0,1)→(0,1) | (0.5,0.5) | (205,-58.8554) | (355.439,117.711) | **F** | Image,FactionScore | Background 32×32 九宫10,10,10,10 ppu=200 | Sliced (0.00943,0.000934,0.00187,0.098) |  | 字段: factionImage,highestRatingDisplay,ratingDisplay |
| ·····5 | icon | 139.00,717.95→319.00,835.66 | 180.00×117.71 | (0.5,1)→(0.5,1) | (0.5,0.5) | (-90,-58.855) | (180,117.71) | T | Image | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Alliance Rating Display | 106.72,702.65→496.72,813.56 | 390.00×110.91 | (1,0.5)→(1,0.5) | (1,0.5) | (0,18.7) | (390,110.91) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 106.72,702.65→151.12,761.81 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 106.72,702.65→258.54,813.56 | 151.82×110.91 | (0,1)→(0,1) | (0.5,0.5) | (75.91,-55.455) | (151.82,110.91) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 258.54,702.65→496.72,813.56 | 238.18×110.91 | (0,1)→(0,1) | (0.5,0.5) | (270.91,-55.455) | (238.18,110.91) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '4879' 字号=90.0 auto[18.0~90.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Alliance Rating Display (1) | 106.72,768.43→496.72,840.77 | 390.00×72.34 | (1,0.5)→(1,0.5) | (1,0.5) | (0,-27.8) | (390,72.3365) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 106.72,768.43→151.12,827.60 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 106.72,768.43→258.54,840.77 | 151.82×72.34 | (0,1)→(0,1) | (0.5,0.5) | (75.91,-36.1683) | (151.82,72.3365) | T | Image,EverguildButtonMaterialModifier,LayoutElement | Menu_Icon_Galon 64×64 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 258.54,768.43→496.72,840.77 | 238.18×72.34 | (0,1)→(0,1) | (0.5,0.5) | (270.91,-36.1683) | (238.18,72.3365) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '5000' 字号=76.3499984741211 auto[18.0~90.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | MainRating | 114.00,717.95→524.00,785.75 | 410.00×67.80 | (0,1)→(0,1) | (0.5,0.5) | (205,-33.8995) | (410,67.799) | T | Image,LayoutElement | 40K_main_rank_display 315×64 否 | Simple (1,1,1,1) |  | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Mission Milestones Progress | 114.00,708.59→524.00,795.10 | 410.00×86.51 | (0,1)→(0,1) | (0.5,0.5) | (205,-33.8995) | (410,86.5143) | T | MilestonesStepDisplay |  |  |  | 字段: stepPrefab,stepsHolder |
| ······6 | Background | 63.38,715.00→574.62,788.70 | 511.24×73.70 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (1.52588e-05,0) | (511.24,73.7) | T | Image,LayoutElement | 40K_main_rank_display 315×64 否 | Simple (1,1,1,0.647) |  | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | counter | 118.30,481.17→198.30,533.39 | 80.00×52.22 | (0,1)→(0,1) | (0.5,0) | (44.3,175.2) | (80,52.2235) | **F** | TextMeshProUGUI,LayoutElement,EverguildTextController |  |  | '16' 字号=40.0 auto[15.0~40.0] 对齐=Center/Midline 折行=1 色=(0.569,0.573,0.588,1) | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth ; 字段: maxFontSize,minFontSize |
| ······6 | steps | 44.13,708.59→593.87,795.10 | 549.74×86.51 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (139.744,0) | T | HorizontalLayoutGroup |  |  |  | spacing=-5.099999904632568 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 |
| ·······7 | RankedSealStep | 221.55,708.59→321.55,795.10 | 100.00×86.51 | (0,1)→(0,1) | (0.5,0.5) | (227.422,-43.2571) | (100,86.5143) | T | MilestoneStep,EverguildButtonMaterialModifier,Image,LayoutElement | Rank Skull 128×128 否 | Simple (1,1,1,0) preserveAspect |  | 字段: alwaysShowEmptyMark,animationBetweenStepsDelay,animationScaleInitialMultiplier,animationStartDelay,animationTime,checkMark,doEnterAnimation,emptyMark,soundOnAppear ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ········8 | Empty | 221.55,708.59→321.55,795.10 | 100.00×86.51 | (0,0)→(1,1) | (0.5,0.5) | (4.57764e-05,0) | (0,0) | T | Image | Rank Skull Empty 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ········8 | Fill | 221.55,708.59→321.55,795.10 | 100.00×86.51 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | Rank Skull 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·······7 | RankedSealStep (1) | 316.45,708.59→416.45,795.10 | 100.00×86.51 | (0,1)→(0,1) | (0.5,0.5) | (322.322,-43.2571) | (100,86.5143) | T | MilestoneStep,EverguildButtonMaterialModifier,Image,LayoutElement | Rank Skull 128×128 否 | Simple (1,1,1,0) preserveAspect |  | 字段: alwaysShowEmptyMark,animationBetweenStepsDelay,animationScaleInitialMultiplier,animationStartDelay,animationTime,checkMark,doEnterAnimation,emptyMark,soundOnAppear ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ········8 | Empty | 316.45,708.59→416.45,795.10 | 100.00×86.51 | (0,0)→(1,1) | (0.5,0.5) | (4.57764e-05,0) | (0,0) | T | Image | Rank Skull Empty 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ········8 | Fill | 316.45,708.59→416.45,795.10 | 100.00×86.51 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | Rank Skull 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Legendary Ratings | 114.00,723.17→524.00,780.52 | 410.00×57.36 | (0,1)→(0,1) | (0.5,0.5) | (205,-33.8995) | (410,57.356) | T | HorizontalLayoutGroup |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 |
| ······6 | Position | 196.62,723.17→258.42,780.52 | 61.79×57.36 | (0,1)→(0,1) | (0.5,0.5) | (113.52,-28.678) | (61.794,57.356) | **F** | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 197.88,723.17→237.88,780.52 | 40.00×57.36 | (0,1)→(0,1) | (0,0.5) | (1.252,-28.678) | (40,57.356) | T | EverguildButtonMaterialModifier,LayoutElement,EverguildTextMeshPro |  |  | '#' 字号=66.5999984741211 对齐=Center/Midline 折行=1 色=(1,1,1,1) | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth ; 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·······7 | Main Icon | 269.36,723.17→279.36,780.52 | 10.00×57.36 | (0,1)→(0,1) | (0.5,0.5) | (77.74,-28.678) | (10,57.356) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect **m_Enabled=0** |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 237.88,723.17→257.17,780.52 | 19.29×57.36 | (0,1)→(0,1) | (0,0.5) | (41.252,-28.678) | (19.29,57.356) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '2' 字号=45.0 auto[18.0~45.0] 对齐=Left/Capline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Global Rating | 258.42,723.17→441.38,780.52 | 182.96×57.36 | (0,1)→(0,1) | (0.5,0.5) | (235.897,-28.678) | (182.96,57.356) | **F** | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 258.42,723.17→313.42,780.52 | 55.00×57.36 | (0,1)→(0,1) | (0,0.5) | (0,-28.678) | (55,57.356) | T | Image,EverguildButtonMaterialModifier,LayoutElement | WF_UI_Trophy_Gold 124×124 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 334.47,723.17→344.47,780.52 | 10.00×57.36 | (0,1)→(0,1) | (0.5,0.5) | (81.05,-28.678) | (10,57.356) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect **m_Enabled=0** |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 313.42,723.17→441.38,780.52 | 127.96×57.36 | (0,1)→(0,1) | (0,0.5) | (55,-28.678) | (127.96,57.356) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '152' 字号=45.0 auto[18.0~45.0] 对齐=Left/Capline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ···3 | Timer | 7.33,717.95→630.67,717.95 | 623.34×0.00 | (0,1)→(0,1) | (0.5,0.5) | (253.95,-495.2) | (623.336,0) | T | HorizontalLayoutGroup,LayoutGroupContentFixer,TimerDisplay,LayoutElement |  |  |  | spacing=0.0 align=4 pad=0,0,5,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: fixOnEnable,layouts ; 字段: extraText,lastMinutesText,layoutGroupContentFixer,timer,timerTextDescription,useExtraText,useLastMinutesText ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | Timer Icon | 172.83,698.49→216.73,742.40 | 43.91×43.91 | (0,1)→(0,1) | (1,0.5) | (209.401,-2.5) | (43.906,43.906) | T | Image | WF_icon_clock 64×64 否 | Simple (1,1,1,1) |  |  |
| ····4 | Timer | 216.73,692.49→465.17,748.40 | 248.44×55.90 | (0,1)→(0,1) | (0,1) | (209.401,25.4525) | (248.44,55.905) | T | EverguildTextMeshPro,ContentSizeFitterMinMax |  |  | 'Termina en: 23d 5h' 字号=38.0 auto[10.0~38.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段: clampHeight,clampWidth,heightMax,heightMin,m_HorizontalFit,m_VerticalFit,widthMax,widthMin |
| ··2 | ChangeRankedToggle | 191.45,868.12→446.55,921.97 | 255.10×53.84 | (0.5,0)→(0.5,0) | (0.5,0) | (0,37.1) | (255.1,53.844) | T | Image,ToggleDisplay,ChangeRankedModeToggle | 40k_menu_bt 47×47 九宫10,10,10,10 | Sliced (1,1,1,0) |  | 字段: toggleImage,toggleSprite,toggleText,toggled,xOffset ; 字段: changeRankedButton,changeRankedToggle,toggleOffSprite,toggleOnSprite |
| ···3 | RankedText | 415.10,868.12→671.50,920.52 | 256.40×52.40 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (224.3,0.724529) | (256.4,52.395) | T | TextMeshProUGUI,Localize,EverguildTextController |  |  | 'Ranked' 字号=45.0 auto[10.0~45.0] 对齐=Left/Capline 折行=1 色=(1,1,1,1) 字距=-4 | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: maxFontSize,minFontSize |
| ···3 | UnrankedText | 12.30,868.12→268.70,920.52 | 256.40×52.40 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-178.5,0.724529) | (256.4,52.395) | T | TextMeshProUGUI,Localize,EverguildTextController |  |  | 'Unranked' 字号=45.0 auto[10.0~45.0] 对齐=Right/Middle 折行=1 色=(1,1,1,1) 字距=-4 | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: maxFontSize,minFontSize |
| ···3 | Image | 296.30,875.04→385.30,915.04 | 89.00×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (21.8,0) | (89,40) | T | Image | 40_main_bt_toggle_on 89×40 否 | Simple (1,1,1,1) |  |  |
| ···3 | ChangeRankedButton | 191.45,868.12→446.55,921.97 | 255.10×53.84 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-4.57764e-05,3.05176e-05) | (255.1,53.844) | T | Image,EverguildButton,EverguildButtonMaterialModifier | 40k_menu_bt 47×47 九宫10,10,10,10 | Sliced (1,1,1,0) |  | trans=0 target=11881104092923824 interactable=1 ; 字段:  |

⚠️ 表里 `字段:` 那一列是**字母序的前 26 个**（`工具/menu_dump.py:379` 的 `sorted(...)[:26]`）—— 本页两个类都没被截断（`RankingDisplay` 只有 10 个；`RankedEventWindowV2` 列尾是字母序靠后的 `windowsPlacement`）。本页的图名/尺寸是 **pid → 名字 →（按名字）→ 尺寸**反查来的，通用名要留心（§A·3）。

---

## §A·1 `RankingDisplay` 的字段 → 节点名对照表

**三路互证**：
- **权威①（序列化引用）**：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-2583548330168842320.json`（`m_GameObject` = `2414930959176796080`）—— 字段名 + `m_PathID`，**7 个引用全是 `m_FileID=0`（同文件内引用）**。
- **权威②（反编译）**：`d:/2/tools/decomp_full/RankingDisplay__Initialize.c` 的实例偏移与调用点。
- **字段的声明顺序/类型**：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/RankingDisplay.cs:5-35`。

| 声明序 | 字段（类型） | 序列化 PathID | **指向的节点**（本文表里的路径） | 反编译里的偏移 / 证据 |
|---|---|---|---|---|
| 1 | `divisionText` (`TextMeshProUGUI`) | `7569753382547457968` | `Content / RankTitleBG / DivisionText` | `+0x20`；`:105`、`:158`、`:210` 用虚表 `+0x558` 的 `set_text` |
| 2 | `divisionImage` (`DivisionImageDisplay`) | `2175362721869432752` | `Content / DivisionImage` | `+0x28`；`:76` `DivisionImageDisplay__Initialize` |
| 3 | `timerDisplay` (`TimerDisplay`) | `-53312969463986256` | `Content / Timer` | `+0x30`；`:255` `TimerDisplay__Initialize` |
| 4 | 🔴 **`ratingHolder` (`GameObject`)** | `-1511393920980777040` | 🔴 **`Content / footer / MainRating`** | `+0x38`；`:80/:88/:92/:114/:123/:164/:166/:190/:221/:236` 全部当 **GameObject** 直接进 `SetActive`（⇒ 类型必然是 GameObject） |
| 5 | `ratingDisplay` (`AllianceRatingDisplay`) | `-2106525578913544272` | `Content / footer / MainRating / Legendary Ratings / Global Rating` | `+0x40`；`:109`、`:141`、`:192` `AllianceRatingDisplay__Initialize` |
| 6 | `positionDisplay` (`AllianceRatingDisplay`) | `5076611844884826032` | `Content / footer / MainRating / Legendary Ratings / Position` | `+0x48`；`:224` 跳 `joined_r0x…` 后同样进 `AllianceRatingDisplay__Initialize` |
| 7 | `sealCountDisplay` (`MilestonesStepDisplay`) | `5578607000028612528` | `Content / footer / MainRating / Mission Milestones Progress` | `+0x50`；`:137-139` 5 参虚调用，与 `MilestonesStepDisplay.Initialize(int,int,Action,bool,bool)` 对得上 |
| 8 | `displayRating` (bool) | **`0`** | （开关，不指节点） | `+0x58`；`.ctor.c:5` 置 **1** |
| 9 | `displayPosition` (bool) | **`1`** | （开关） | `+0x59`；`.ctor.c` **没置**（= false） |
| 10 | `displaySeals` (bool) | **`1`** | （开关） | `+0x5a`；`.ctor.c:6` 置 **1** |

🔴 **两条最要紧的结论**：
1. **`ratingHolder` 指的是 `MainRating` 整块**（`Content/footer/MainRating`），**不是 `footer`**。⇒ `displayRating` / `displaySeals` 两个开关关的是 `MainRating` 这一支 —— 而 `Mission Milestones Progress`（里程碑）与 `Legendary Ratings`（名次/总评）**都在 `MainRating` 里面**。
2. 🔴 **ctor 默认值 ≠ 本实例的序列化值**（`CLAUDE.md` 铁律 5·c）：`RankingDisplay__.ctor.c:5-6` 把 `displayRating`/`displaySeals` 置 1、`displayPosition` 不置（=**false**）；而**场景里序列化的是 `displayRating=0` / `displayPosition=1` / `displaySeals=1`**。⇒ 照做要取**序列化值**；只读 `.ctor` 会把「显示评分 / 不显示名次」正好搞反。

**同表顺带解出的其它组件绑定**（同一份 JSON，全部 `m_FileID=0` 同文件引用）：

| 节点（类名） | 字段 → 指向 |
|---|---|
| `Content/DivisionImage`（`DivisionImageDisplay`） | `divisionImage`=`DivisionImage`(自己) · `rankImage`=`RankImage` |
| `Content/Timer`（`TimerDisplay`） | `timer`=`Timer`(子文本) · `timerTextDescription`=**null** · `layoutGroupContentFixer`=`Timer`(自己) · `useExtraText=1`(term `Event_Description/General/EndsInPlusOrangeTime`) · `useLastMinutesText=1`(term `MainMenu/RankedWindow/LastMinutes`) |
| `…/footer/Highest Faction Rating/Rating Text`（`AllianceRatingDisplay`） | `ratingText`=`Individual rating value` · `ratingIcon`=`Secondary Icon` |
| `…/footer/FactionScoreSmall`（`FactionScore`） | `factionImage`=`icon` · `ratingDisplay`=`Alliance Rating Display` · `highestRatingDisplay`=`Alliance Rating Display (1)` |
| `…/FactionScoreSmall/Alliance Rating Display`（`AllianceRatingDisplay`） | `ratingText`=`Individual rating value` · `ratingIcon`=`Secondary Icon` |
| `…/FactionScoreSmall/Alliance Rating Display (1)`（`AllianceRatingDisplay`） | `ratingText`=`Individual rating value` · `ratingIcon`=`Secondary Icon` |
| `…/MainRating/Mission Milestones Progress`（`MilestonesStepDisplay`） | `stepsHolder`=`steps` · `stepPrefab`=[`RankedSealStep`]（**数组里只填了 1 个**） |
| `…/steps/RankedSealStep` 与 `…/steps/RankedSealStep (1)`（`MilestoneStep`） | `checkMark`=`Fill` · `emptyMark`=`Empty` |
| `…/MainRating/Legendary Ratings/Position`（`AllianceRatingDisplay`） | `ratingText`=`Individual rating value` · **`ratingIcon` = null** |
| `…/MainRating/Legendary Ratings/Global Rating`（`AllianceRatingDisplay`） | `ratingText`=`Individual rating value` · `ratingIcon`=`Secondary Icon` |
| `ChangeRankedToggle`（`ToggleDisplay`） | `toggleImage`=`Image` |
| `ChangeRankedToggle`（`ChangeRankedModeToggle`） | `changeRankedButton`=`ChangeRankedButton` · `changeRankedToggle`=`ChangeRankedToggle`(自己) |
| `LeaderboardButton`（`EverguildButton`） | `text`=`Button Text` · `m_TargetGraphic`=`LeaderboardButton` |

**🔴 `LeaderboardButton` 与 `ChangeRankedToggle` 在哪一层 / 走哪个字段**（问的那一条）：
两个都是 **`Ranked Division Info` 的直接子节点（depth 1）**，由**根**（`RankedEventWindowV2`）的字段指向：
`RankedEventWindowV2.leaderboardButton` → `LeaderboardButton` · `RankedEventWindowV2.changeRankedToggle` → `ChangeRankedToggle`（`RankedEventWindowV2.cs:19-24` 的声明 + 本实例序列化实测）。`Button Text` 的文本就是 `'Leaderboard'`。

---

## §A·2 出场 `activeSelf`（逐条点名 + 绑在哪个字段）

表里 `act=F` 的**共 10 个节点**（已与原始 JSON 的 `m_IsActive` 逐条核过，**一致**）：

| # | 节点（路径） | **绑在哪个字段** | 证据 / 备注 |
|---|---|---|---|
| 1 | `Ranked Division Info / info` | **没有** | `Image`+`EverguildTooltipTrigger`，55.23²，`40K_generic_bt_info`。全 bundle grep 它的 GO pid `5484638253501876144`：**只命中它自己的组件** |
| 2 | `… / footer / Highest Faction Rating` | **没有** | grep GO pid `-2119772973454424144`：只命中自身组件（`Image`/`HorizontalLayoutGroup`/`LayoutElement`，**一个自定义脚本都没有**）。它**不在** `RankingDisplay` 的 7 个引用字段里，也不在 `RankedEventWindowV2` 的 16 个字段里 |
| 3 | `… / footer / FactionScoreSmall` | **没有** | grep GO pid `36076030783752112`：只命中自身组件（`Image` + `FactionScore`）。`FactionScore__Initialize` 的调用者只有 `RankedEventWindow__SetDivision.c:71` 与 `RankedTab__InitializeFactionScores.c` —— **另一个窗/另一页；`RankedEventWindowV2__SetDivision.c` 全文（92 行）只调 `RankingDisplay__Initialize`（`:79`）** |
| 4 | `…/FactionScoreSmall/Alliance Rating Display/Secondary Icon` | `AllianceRatingDisplay.ratingIcon` | 那个组件的 `ratingIcon` 就指它 |
| 5 | `…/FactionScoreSmall/Alliance Rating Display (1)/Secondary Icon` | `AllianceRatingDisplay.ratingIcon` | 同上 |
| 6 | `…/MainRating/Mission Milestones Progress/counter` | **没有** | grep GO pid `2324030675338692528`：只命中它自己的 `TextMeshProUGUI`/`LayoutElement`/`EverguildTextController` |
| 7 | `…/MainRating/Legendary Ratings/Position` | ✅ **`RankingDisplay.positionDisplay`** | `Initialize` 的 `+0x48` 那一支 |
| 8 | `…/Legendary Ratings/Position/Main Icon` | **没有**（且它的 `Image` 组件 `m_Enabled=0`） | |
| 9 | `…/MainRating/Legendary Ratings/Global Rating` | ✅ **`RankingDisplay.ratingDisplay`** | `Initialize` 的 `+0x40` 那一支 |
| 10 | `…/Legendary Ratings/Global Rating/Main Icon` | **没有**（`Image` 组件 `m_Enabled=0`） | |

**另有 5 处「节点 active、组件 `m_Enabled=0`」**（表里只在 `Image` 那列打 `**m_Enabled=0**`，容易漏，这里补全路径）：

| 路径 | 组件 | 后果 |
|---|---|---|
| `…/Highest Faction Rating/Rating Text/Individual rating value` | `LayoutElement` | 它的 `m_PreferredWidth=5000` **不生效** |
| `…/Position/Main Icon` · `…/Global Rating/Main Icon` | `Image` | 图不画 |
| `…/Position/Individual rating value` · `…/Global Rating/Individual rating value` | `LayoutElement` | `m_PreferredWidth=5001.38` **不生效** |

⚠️ **`RankingDisplay.Initialize` 里能直接看到的 `SetActive` 调用点**（`RankingDisplay__Initialize.c`，说明「出厂 inactive」运行时会被怎么改）：

- `:80` 数据为空 ⇒ `SetActive(ratingHolder, 0)`
- `:88` `SetActive(ratingHolder, displayRating)` · `:92` `SetActive(ratingDisplay.gameObject, displayRating)` · `:96` `SetActive(sealCountDisplay.gameObject, 0)`（这一支恒关）
- `:115` `SetActive(ratingHolder, displaySeals)` · `:123` `SetActive(ratingDisplay.gameObject, 分支值)` · `:134` `SetActive(positionDisplay.gameObject, 分支值)`
- `:221` / `:236` `SetActive(positionDisplay.gameObject, 1 / 0)`

⇒ **本实例 `displayRating=0`** ⇒ 无论走哪一支，`Global Rating`（= `ratingDisplay`）**都不会被打开**；`displayPosition=1` ⇒ `Position` 由数据决定。
⚠️ 我没有把「哪条数据走哪条分支」推完（分支条件挂在数据字段 `plVar6[0xb]` 上）⇒ **运行期最终态属于「必须再确认」**（§C）。

---

## §A·3 sprite / 九宫格 / ppu 特别的

- **`<无图>` 只有一处：`Content/DivisionImage`**，它的 sprite PPtr = `(m_FileID=0, m_PathID=0)` = **空**。它是**运行时从 Addressables 载进来的**：`DivisionImageDisplay__Initialize.c:36-42` 把 `divisionImageRef`（实例 `+0x30`）`Load` 出的 sprite `set_sprite` 给 `+0x20`（= 本节点）。⇒ 🔴 **段位大图不在 bundle 里，本地拿不到**（要复刻只能另找图/自画）。
- `RankImage` 的 sprite 是**跨文件引用**（`m_FileID=21, m_PathID=-5716310642658349672`）→ 真包 pid 反查得名字 `Roman V 128×128`。同节点由 `DivisionImageDisplay__Initialize.c:43-55` 按段位类型 `SetActive`（`+0x28`）。
- **通用名要留心的一处**：`Content/footer/FactionScoreSmall` 的 sprite 名是 **`Background`**（`(m_FileID=15, m_PathID=1660267235368898380)`，`Sliced`，原尺寸 `32×32`，九宫 `10,10,10,10`，`ppu=200`，颜色近乎全透明 `(0.0094,0.0009,0.0019,0.098)`）。尺寸/九宫格是**按名字**查的（`工具/menu_dump.py:43-46` 的告警），`Background` 是 UGUI 通用名 ⇒ **这两个数可能是另一张同名图**，要钉死得去真包按 pid 读那张 Sprite 本体。
- **`40K_main_rank_display 315×64 否`（无九宫格）被 4 个节点共用，透明度各不相同** ⇒ 别统一：
 `RankTitleBG` `a=0.918` · `Highest Faction Rating` `a=1` · `MainRating` `a=1` · `Mission Milestones Progress/Background` `a=0.647`。
- **`RankedSealStep` 自己的 `Image` 用 `Rank Skull` 但 `color.a = 0`**（纯命中框/逻辑体）；真正显示的是两个子节点 `Empty`（`Rank Skull Empty`，`a=1`）与 `Fill`（`Rank Skull`，`a=1`）—— 名字与 `MilestoneStep.emptyMark`/`checkMark` 的绑定一致（§A·1）。
- **`ChangeRankedToggle` 与 `ChangeRankedButton` 的 `Image` 都是 `40k_menu_bt`（`Sliced`，九宫 `10,10,10,10`）且 `color.a = 0`**；看得见的只有子节点 `Image`（`40_main_bt_toggle_on 89×40`）+ `RankedText` / `UnrankedText`。
- `LeaderboardButton` 用 `UI_Button_Mulligan 410×124 九宫333,96,333,96`（与 `To Battle Button` 同一张，这里是 `Sliced`）；`Button Text` = `'Leaderboard'`，字号 36 auto[10~36]，`Center/Capline`，不折行，`AspectRatioFitter`。
- `40k_UI_icon_ranked_Skirmish` 被 5 个 `Main Icon` 当占位（`Rating Text`/`Alliance Rating Display`/`(1)`/`Position`/`Global Rating`）；`Position` 与 `Global Rating` 那两个的 `Image` 已 `m_Enabled=0`。
- `info` / `LeaderboardButton` / `Timer Icon` 之外，本支的 `Image.m_RaycastTarget` 多为 1；`RankTitleBG` 与 `Mission Milestones Progress/Background` 是 **0**。
- ⚠️ **文本里烘的都是导出机的占位/西语值，别当文案抄**：`Rank Title`='Rank' · `DivisionText`='Division V' · `counter`='16' · `Timer`='Termina en: 23d 5h' · `Individual rating value`='4879'/'5000'/'2'/'152' · `DEBUG_Ranked_Id`='EventId'（在后一支）。

---

## §A·4 布局组：哪些值算不准

工具表尾自己点名的（本页**只有两个**）：

```
        Content → VerticalLayoutGroup [unk]
            Highest Faction Rating → HorizontalLayoutGroup [unk]
```

`unk` 的含义（`工具/menu_dump.py:405-425`、`:505-560`）：该组**有子节点的首选尺寸必须靠 Unity 字形度量 / 递归嵌套才算得出**（子节点里有 `TextMeshProUGUI`/`Text`，或有嵌套布局件、`GridLayoutGroup`、`ScrollRect`）⇒ 这一组算出来的**子节点位置/尺寸要留个心眼**。

**本页具体偏在哪、偏多少**（⚠️ 下面第 1、3 条是我按 uGUI 规则**推算**的，**不是 Unity 实测**）：

1. 🔴 **`footer` 的高度被算成 0 连带 `Timer` 的 y 也错** —— 表里 `footer` 与 `Timer` 都是 `717.95→717.95`（高 0），`Timer` 的两个子节点也吊在 `717.95`。工具对**嵌套布局组**给的是 `(min=0, pref=0, unknown=True)`（`_child_sizes` 不递归）。
 按 uGUI：`footer` 是 VLG、`m_ChildControlHeight=0` ⇒ 它的首选高 = **active 子节点的 `sizeDelta.y` 之和 + spacing**；`footer` 的 active 子节点**只有 `MainRating`**（另两个出厂 inactive，工具已正确滤掉），`MainRating.sizeDelta.y = 67.799` ⇒ **`footer` 真高 ≈ 67.8**，`Timer` 的 y 应当在 **≈ 785.75**（表里写 717.95，**少了 67.80**）。
 ✅ 不受影响的部分：`footer` **内部**的相对位置（`MainRating` 是唯一 active 子节点、在 `footer` 顶端，表里 `717.95` 已写对）；`Timer` 自身的 `x`（表里 `7.33→630.67` 是**居中**的结果，算式自洽）。
 ⚠️ `Timer` 自己的真高也要一起定（它是 HLG、`ctrlH=0`，首选高按「active 子节点 `sizeDelta.y` 取最大」= `55.905`，再加它自己的纵向 padding）—— 这个我没算实数，**必须再确认**。
2. ⚠️ **`Content` 那一组还带一个「文字类子节点没被识别」的漏洞**：`_child_sizes` 只认类名**恰好是** `TextMeshProUGUI` / `Text` 的（`menu_dump.py:418-419`），而本页文字全是 **`EverguildTextMeshPro`**（`TextMeshProUGUI` 的子类，类名不同）⇒ 这些组**没被打 `⚠️unk`，但横轴同样可能不准**：`Rating Text` / `Alliance Rating Display` / `Alliance Rating Display (1)` / `Position` / `Global Rating` / `Legendary Ratings`（全是 `ctrlW=1`）。
3. ⚠️ **三处溢出父框**（可能是刻意如此 —— `steps` 与 `Content/Timer` 的父链上**都没有 `RectMask2D`/`Mask`** ⇒ 复刻时**别给它们加遮罩**）：
 - `steps`（锚点 `(0,0)→(1,1)` + `sizeDelta.x=139.744`）宽 549.74，父 `Mission Milestones Progress` 只有 410 ⇒ **左右各溢出 69.87**（`114−44.13` = `593.87−524` = 69.87）
 - `Content/Timer` 宽 623.34，父 `Content` 只有 507.9 ⇒ **左右各溢出 57.72**（`65.05−7.33` = `630.67−572.95` = 57.72）
 - `Content/DivisionImage` 宽 539.795，父 `Content` 507.9 ⇒ **左右各溢出 15.95**（它 `m_IgnoreLayout=1`，见下）
4. `steps` 的 **`spacing = −5.1`（负间距）**：`RankedSealStep (1).x = 221.55 + 100 − 5.1 = 316.45` ✅ 自洽。

**表里这几个值可信、可以直接用**（不依赖上面任何前提）：
`Content` 自己的 rect（父 `Ranked Division Info` 没有布局组，`sizeDelta` 直接读）· `RankTitleBG` 高 `73.70`（= 它的 `m_MinHeight`）· `Spacer` 高 `421.50`（= `m_MinHeight`）· `MainRating` 高 `67.799` · `Mission Milestones Progress` 高 `86.5143` · `steps` 高（= 父高）· 各 `RankedSealStep` `100×86.51`（高被 `steps` 交叉轴按父高 clamp）· **`DivisionImage` 的整块 rect**（它 `m_IgnoreLayout=1` ⇒ **没有任何布局组会写它** ⇒ 序列化值就是运行值）。

---

## §A·5 重名节点 · 同类多实例

- **本支内重名（按 `m_Name` 数的实数）**：`Secondary Icon` ×5 · `Main Icon` ×5 · `Individual rating value` ×5 · `Empty` ×2 · `Fill` ×2 · **`Timer` ×2**（容器 `Content/Timer` 与它的子文本 `…/Timer/Timer` —— 这一对最容易搞混）。
 ⚠️ `RankedSealStep (1)` / `Alliance Rating Display (1)` 的 `(1)` **是原场景里的真名字**（`m_Name` 里就带，不是导出器加的计数器）⇒ 照原版命名就照抄。
- **跨实例重名**：同名 `Ranked Division Info` 有 3 份（§0），本页只认 ③。⚠️ 本支里所有 `Main Icon` / `Secondary Icon` / `Individual rating value` / `Timer` 在 ①/② 里**同样存在** ⇒ **按名字全局搜一定会串**，要按 GO pid 认。
- **同类多实例**：`AllianceRatingDisplay` 在本支共 **5 个**（`Rating Text`、`FactionScoreSmall` 下 2 个、`Position`、`Global Rating`）—— **除 `Position`/`Global Rating` 外都没被 `RankingDisplay` 引用**。
- `RankedSealStep` 在本支**只实例化了 2 个**，但 `MilestonesStepDisplay.stepPrefab` 是**数组且只填了 1 个** ⇒ 运行时按里程碑数 `Instantiate` 出更多 ⇒ **别把「2 个」当定长**（对照：该 prefab 在 bundle 里还有 `RankedSealStep (2)(3)(4)`、`RankedSealStep Animation Disabled`、`RankedSealStep Division Change Variant` 等其它实例，都不是本支的）。

---

## §B 查不到的（**不猜** · 写清搜过什么）

1. 🔴 **`Ranked Division Info/info` 这个 tooltip 按钮由谁打开** —— 搜过：bundle 全量 `MonoBehaviour/*.json` grep 它的 GO pid `5484638253501876144`（**只命中它自己的组件**）；`RankedEventWindowV2` 的 16 个声明字段（`RankedEventWindowV2.cs:6-70`）；`RankingDisplay` 的 10 个字段。**查不到**。
2. 🔴 **`footer/Highest Faction Rating` 整块（含 `Rating Text`）由谁打开** —— 同上搜法：GO pid `-2119772973454424144` 只命中自身组件；其 `AllianceRatingDisplay` 组件 pid `-1886386831821469776` **全库零命中**。代码侧：`RankedEventWindow__SetDivision.c` 里有 `FactionScore` 调用但**没有** `Highest Faction Rating` 任何字段；`RankedEventWindowV2__SetDivision.c`（92 行）只调 `RankingDisplay__Initialize`。⇒ 我**倾向**它是 V2 变体里的遗留件，但**没有确证** ⇒ 记「未查清」。
3. 🔴 **`footer/FactionScoreSmall` 在实例 ③ 里由谁打开** —— 同上（GO pid `36076030783752112`）。`FactionScore__Initialize` 的调用者只有 `RankedEventWindow__SetDivision.c:71` 与 `RankedTab__InitializeFactionScores.c`（**另一个窗 / 档案窗 Ranking 页**）⇒ 在本窗里**没有调用者**，但同样**没有确证**。
4. ⚠️ **`footer` 的真高 / `Timer` 的真 y、`Timer` 的真高** —— 工具给 0 / 717.95；我按 uGUI 规则推算 ≈67.8 / ≈785.75（§A·4 第 1 条），**没有 Unity 实测**。
5. ⚠️ **`Main Icon` 为什么出厂 inactive 且 `m_Enabled=0`** —— 没有任何字段指向它；`AllianceRatingDisplay` 只声明了 `ratingText` / `ratingIcon` 两个引用。可能靠 `Initialize(value, overrideIcon,…)` 里的 `overrideIcon` 走 `Find`/`GetChild`，**那一段我没读** ⇒ 记「未查清」。
6. ⚠️ **`DivisionImage`/`RankImage` 的 Addressables 地址（到底是哪张图）** —— `DivisionImageDisplay__Initialize.c` 里取的是 `RankedDivisions.GetDivisionData(...)` 返回对象的 `+0x18`，**不是字面量**，我没追到那张数据表 ⇒ **查不到**（这也是「段位大图本地取不到」的根因）。
7. ⚠️ **本支里我**没有**去跑原版实况**（本任务限定只读普查、不跑 Unity）⇒ 所有值都是**解包静态快照**，不是运行时实况。

---

## §C 可以直接当施工参数的 / 必须再确认的

**直接可用（不必再核）**

- **层级 + 名字 + 55 个节点的 `m_IsActive`**（§A 表 + §A·2 表）—— 已与原始 JSON 逐条核过。
- **`RankingDisplay` 的 7 个引用字段 → 节点**（§A·1）—— 序列化引用 + 反编译偏移**两路互证**。
- **三个 bool 的序列化值**：`displayRating=0` · `displayPosition=1` · `displaySeals=1`（⚠️ 与 `.ctor` 默认值不同，见 §A·1）。
- **`LeaderboardButton` / `ChangeRankedToggle` 是 `Ranked Division Info`（depth 1）的直接子节点**，由**根**的 `leaderboardButton` / `changeRankedToggle` 指向。
- **可用尺寸**：`Content` rect `65.05,222.75→572.95,886.57`（638 宽居中，左右各 65.05）· `RankTitleBG` 高 73.70 · `Spacer` 高 421.50 · `MainRating` 高 67.799 · `Mission Milestones Progress` 高 86.514 · `RankedSealStep` 100×86.51 · `DivisionImage` rect `49.10,234.04→588.90,806.66`（`m_IgnoreLayout=1`）。
- **布局组参数（表里逐字）**：`Content` VLG `spacing=0 align=1(UpperCenter) pad=0 ctrlW=0 ctrlH=1 expandW/H=0` · `footer` VLG `spacing=11.2 align=1 ctrlW=0 ctrlH=0` · `steps` HLG `spacing=−5.1 align=4 ctrlH=1` · `Legendary Ratings` HLG `align=4` · `Rating Text` HLG `ctrlW=1 ctrlH=1` · `Timer` HLG `pad=(0,0,0,5) align=4`。⇒ `Content` 的子节点**横轴居中、纵轴自顶往下堆**。
- **sprite 名 + 九宫格 + `Image.m_Color.a`**（§A·3）；通用名 `Background` 那一条要留个心眼。
- **`DivisionImage` 的 sprite 拿不到**（Addressables，§A·3/§B·6）—— 这条是**结论**，不是待办。

**必须再确认**

- `footer` 真高 / `Timer` 真 y（§A·4 第 1 条）—— 要么让工具支持「嵌套布局组递归算」，要么进原版实况量一次（`资料/真Play待验清单.md`）。
- 所有 `ctrlW=1` 文字行的**横轴**位置（§A·4 第 2 条：`EverguildTextMeshPro` 没被识别成文字）。
- `Highest Faction Rating` / `FactionScoreSmall` / `info` 三块**要不要建**：在 V2 实例里**既无字段绑定、也无本窗代码调用**（§A·2 + §B 1-3）⇒ 若按「照原版做」，建议**先按不显示处理**；但这是**结构判断不是数值**，要正本/用户裁一下。
- 「运行期最终态」（哪条数据走哪条 `SetActive` 分支）—— 我只列了调用点，没推完数据分支（§A·2 末）。
