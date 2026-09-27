```
# MonoScript 类名索引：5175 条
# （已沿 `m_Father` 爬父链：被查节点的父 = 「Tab Content」 351.03,118.92 → 1746.97,962.00）
# sprite 索引：真包 3316 条 + 切片缓存补齐 ⇒ 共 3316 条
# sprite 尺寸/九宫格索引：3200 条
| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 锚点 min→max | pivot | anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | 贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | Profile Tab | 351.03,118.92→1746.97,962.00 | 1395.94×843.08 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | **F** | ProfileTab |  |  |  | 字段: changeNameButton,changeNameWindow,consecutiveLoginCount,eventSection,infoSection,inviteToAllianceButton,playerIdDisplay,rankingSection |
| ·1 | Invite to alliance | 1532.42,865.87→1746.97,911.00 | 214.54×45.14 | (1,0)→(1,0) | (1,0) | (0,51) | (214.543,45.135) | T | Image,EverguildButton,EverguildButtonMaterialModifier | 40k_menu_bt_general_bg 51×52 九宫15,15,15,15 | Sliced (0.212,0.0941,0.098,1) |  | trans=1 target=9033955621091441202 interactable=1 ; 字段:  |
| ··2 | Button Outline | 1532.92,865.87→1746.47,911.00 | 213.54×45.14 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-1,0) | T | Image,EverguildButtonMaterialModifier | 40k_menu_bt__general_outline 51×52 九宫15,15,15,15 | Sliced (0.945,0.842,0.0314,1) fillCenter=0 |  | 字段:  |
| ··2 | Text | 1544.52,874.46→1734.87,902.41 | 190.36×27.95 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-24.1858,-17.19) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,Localize |  |  | 'Invite to Alliance' 字号=29.450000762939453 auto[18.0~45.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段:  ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary |
| ·1 | PlayerId | 351.03,867.38→913.41,907.38 | 562.38×40.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-416.777,-346.916) | (562.382,40) | T | EverguildButton |  |  |  | trans=1 target=-4834945724435891662 interactable=1 |
| ··2 | Image | 350.67,867.88→377.85,906.87 | 27.18×39.00 | (0,0)→(0.162563,1) | (0.5,0.5) | (-32.481,-9.91821e-05) | (-64.242,-1.003) | T | Image,EverguildButtonMaterialModifier | 40k_profile_icon_copy 27×34 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ··2 | playerIdText | 387.03,867.38→906.14,907.38 | 519.11×40.00 | (0,0)→(1,1) | (0.5,0.5) | (14.365,0) | (-43.271,0) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Player id: gdajhahjaihj' 字号=32.0 auto[10.0~32.0] 对齐=Left/Middle 折行=0 色=(1,1,1,0.631) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize |
| ·1 | Consecutive login days | 1258.04,867.37→1747.96,902.35 | 489.93×34.97 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (454,-344.4) | (489.93,34.9744) | **F** | EverguildButton |  |  |  | trans=1 target=0 interactable=1 |
| ··2 | playerIdText | 1258.04,867.37→1747.96,902.35 | 489.93×34.97 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier |  |  | 'Consecutive login days: 312 days' 字号=30.0 auto[23.0~30.0] 对齐=Right/Middle 折行=1 色=(1,1,1,0.631) | 字段:  ; 字段:  |
| ·1 | Player Info | 351.03,168.16→1186.98,320.54 | 835.95×152.39 | (0,0.5)→(0,0.5) | (0,0.5) | (0,296.107) | (835.952,152.385) | T | PlayerInfoDisplay |  |  |  | 字段: avatarDisplay,nameTitleSection,nameTitleSectionWithAlliances,playerLevel |
| ··2 | Avatar Item Small | 351.03,168.16→510.19,320.54 | 159.15×152.39 | (0,0)→(0,1) | (0,0.5) | (0,-3.05176e-05) | (159.154,0) | T | EverguildButton,AvatarDisplay,ItemDrawerComponents |  |  |  | trans=1 target=-2195724460908250574 interactable=1 ; 字段: avatarHolder,avatarImage,avatarName,button,highlight,inspectSound,showItemInfoOnClick ; 字段: background,claimedWarning,conversionDisplay,convertedItem,convertedLabel,ephemeralDisplay,ephemeralText,image,label,premiumBadge,premiumHighlight,quantity |
| ···3 | Raycast Target | 341.16,128.01→520.06,342.45 | 178.90×214.44 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-9.15527e-05,9.1215) | (178.904,214.439) | T | Image,EverguildButtonMaterialModifier | <无图> | Simple (1,1,1,0) |  | 字段:  |
| ···3 | Image Container | 351.03,168.16→510.19,283.18 | 159.15×115.02 | (0,0)→(1,1) | (0.5,0.5) | (0,18.6839) | (0,-37.3678) | T |  |  |  |  |  |
| ····4 | Highlight | 351.03,164.63→513.52,281.50 | 162.49×116.87 | (0,0)→(1,1) | (0.5,0.5) | (1.6692,2.6) | (3.3384,1.8547) | T | Image,EverguildButtonMaterialModifier | Player_Avatar_selected 256×256 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ····4 | Border | 351.03,179.66→510.19,294.68 | 159.15×115.02 | (0,-0.1)→(1,0.9) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier | Player Profile Border 256×286 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ····4 | Image | 351.03,165.46→510.19,280.48 | 159.15×115.02 | (0,0)→(1,1) | (0.5,0.5) | (0,2.7) | (0,0) | T | Image,EverguildButtonMaterialModifier | <无图> | Simple (1,1,1,1) preserveAspect **m_Enabled=0** |  | 字段:  |
| ···3 | Avatar Name | 351.03,320.54→510.19,361.86 | 159.15×41.31 | (0,0)→(1,0) | (0.5,1) | (0,0) | (7.62939e-06,41.3144) | **F** | TextMeshProUGUI,EverguildButtonMaterialModifier |  |  | 'Avatar name' 字号=36.0 auto[12.0~36.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段:  |
| ··2 | Info Section with Alliance | 510.18,168.16→1405.87,320.54 | 895.69×152.38 | (0,0.5)→(0,0.5) | (0,0.5) | (159.15,0.002677) | (895.69,152.38) | **F** | ProfileNameTitleSection |  |  |  | 字段: allianceDisplay,changeNameButton,playerLevelText,playerName,playerTitle |
| ···3 | Name and Title Holder | 510.19,168.16→1374.52,217.99 | 864.34×49.83 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-15.6721,51.276) | (864.337,49.828) | T | HorizontalLayoutGroup ⚠️unk |  |  |  | spacing=5.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 |
| ····4 | Edit Name Button | 510.19,168.16→563.29,217.99 | 53.10×49.83 | (0,1)→(0,1) | (0.5,0.5) | (26.55,-24.914) | (53.1,49.828) | T | Image,EverguildButton,EverguildButtonMaterialModifier,LayoutElement | 40k_menu_bt_general_bg 51×52 九宫15,15,15,15 | Sliced (0.212,0.0941,0.098,1) |  | trans=1 target=6994077500716055090 interactable=1 ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Button Outline | 510.69,168.16→562.79,217.99 | 52.10×49.83 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-1,0) | T | Image,EverguildButtonMaterialModifier | 40k_menu_bt__general_outline 51×52 九宫15,15,15,15 | Sliced (0.945,0.842,0.0314,1) fillCenter=0 |  | 字段:  |
| ·····5 | Icon | 510.19,168.16→563.29,217.99 | 53.10×49.83 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier | 40k_general_bt_yellow_edit 71×71 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ····4 | Player Name | 568.29,168.16→568.29,217.99 | 0.00×49.83 | (0,1)→(0,1) | (0.5,0.5) | (58.1,-24.914) | (0,49.828) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,LayoutElement |  |  | 'Player Name' 字号=40.70000076293945 auto[23.0~45.0] 对齐=Left/Capline 折行=0 色=(0.98,0.686,0.169,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | Player Title | 573.29,168.16→573.29,217.99 | 0.00×49.83 | (0,1)→(0,1) | (0.5,0.5) | (63.1,-24.914) | (0,49.828) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,LayoutElement |  |  | 'Player Title' 字号=35.0 auto[20.0~35.0] 对齐=Left/Capline 折行=0 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ···3 | Alliance Info | 510.19,220.54→1388.84,286.53 | 878.66×65.99 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-8.51181,-9.1854) | (878.657,65.9909) | T | ProfileAllianceDisplay |  |  |  | 字段: allianceName,playerRating |
| ····4 | Alliance Name | 510.19,212.13→1388.84,251.06 | 878.66×38.93 | (0,0)→(1,0.5) | (0,0.5) | (0,38.4368) | (0,5.9326) | T | EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,EverguildTextMeshPro |  |  | 'Alliance Name' 字号=31.799999237060547 auto[23.0~37.400001525878906] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ····4 | Alliance Rating Display | 510.19,251.06→774.12,286.53 | 263.93×35.47 | (0,0.5)→(0,0.5) | (0,0.5) | (0,-15.26) | (263.929,35.471) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·····5 | Secondary Icon | 510.19,251.06→554.59,310.23 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Main Icon | 510.19,246.94→559.50,290.64 | 49.31×43.70 | (0,1)→(0,1) | (0.5,0.5) | (24.6547,-17.7355) | (49.3095,43.7) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Individual rating value | 559.50,251.06→774.12,286.53 | 214.62×35.47 | (0,1)→(0,1) | (0.5,0.5) | (156.619,-17.7355) | (214.62,35.471) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '------' 字号=37.400001525878906 auto[18.0~40.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ··2 | Info Section without Alliance | 510.18,168.16→1405.87,320.54 | 895.69×152.38 | (0,0.5)→(0,0.5) | (0,0.5) | (159.15,0.002677) | (895.69,152.38) | T | ProfileNameTitleSection |  |  |  | 字段: allianceDisplay,changeNameButton,playerLevelText,playerName,playerTitle |
| ···3 | Name and Title Holder | 510.19,168.16→1374.52,217.99 | 864.34×49.83 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-15.6721,51.276) | (864.337,49.828) | T | VerticalLayoutGroup |  |  |  | spacing=0.0 align=0 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=1 expandH=1 scaleW=0 scaleH=0 |
| ····4 | NameHolder | 510.19,168.16→1127.11,217.99 | 616.92×49.83 | (0,1)→(0,1) | (0.5,0.5) | (308.46,-24.914) | (616.92,49.828) | T |  |  |  |  |  |
| ·····5 | Edit Name Button | 510.19,168.16→563.29,217.99 | 53.10×49.83 | (0,1)→(0,1) | (0.5,0.5) | (26.55,-24.914) | (53.1,49.828) | T | Image,EverguildButton,EverguildButtonMaterialModifier,LayoutElement | 40k_menu_bt_general_bg 51×52 九宫15,15,15,15 | Sliced (0.212,0.0941,0.098,1) |  | trans=1 target=-2370220974041171406 interactable=1 ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Button Outline | 510.69,168.16→562.79,217.99 | 52.10×49.83 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-1,0) | T | Image,EverguildButtonMaterialModifier | 40k_menu_bt__general_outline 51×52 九宫15,15,15,15 | Sliced (0.945,0.842,0.0314,1) fillCenter=0 |  | 字段:  |
| ······6 | Icon | 510.19,168.16→563.29,217.99 | 53.10×49.83 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier | 40k_general_bt_yellow_edit 71×71 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ·····5 | Player Name | 563.29,170.74→805.71,217.99 | 242.42×47.24 | (0,1)→(0,1) | (0.5,0.5) | (174.31,-26.207) | (242.42,47.242) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,LayoutElement |  |  | 'Player Name' 字号=38.599998474121094 auto[23.0~45.0] 对齐=Left/Midline 折行=0 色=(0.98,0.686,0.169,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | Player Title | 510.19,217.99→1127.10,267.81 | 616.92×49.83 | (0,1)→(0,1) | (0.5,0.5) | (308.458,-74.742) | (616.917,49.828) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,LayoutElement |  |  | 'Warrior of the raging winds' 字号=35.0 auto[20.0~35.0] 对齐=Left/Middle 折行=0 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ··2 | Player Level | 453.15,259.19→506.27,312.31 | 53.12×53.12 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-289.3,-41.4) | (53.12,53.12) | T | Image | UI_Button_Round_background 237×237 否 | Simple (1,1,1,1) |  |  |
| ···3 | Player Level Text | 460.01,266.05→499.41,305.45 | 39.40×39.40 | (0,0)→(1,1) | (0.5,0.5) | (-3.05176e-05,0) | (-13.722,-13.722) | T | EverguildTextMeshPro |  |  | '-' 字号=37.20000076293945 auto[18.0~37.20000076293945] 对齐=Center/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·1 | Ranking | 351.02,327.70→1137.12,857.21 | 786.10×529.51 | (0,0.5)→(0,0.5) | (0,0.5) | (-0.0100098,-52) | (786.1,529.51) | T | ProfileRankingSection |  |  |  | 字段: currentRankDisplay,highestRankDisplay,legendaryDisplay,legendaryRankCount |
| ··2 | Current Rank | 351.03,327.71→776.28,857.22 | 425.26×529.51 | (0,0.5)→(0,0.5) | (0,0.5) | (0.00500488,-0.00200653) | (425.257,529.508) | T | RankingDisplay |  |  |  | 字段: displayPosition,displayRating,displaySeals,divisionImage,divisionText,positionDisplay,ratingDisplay,ratingHolder,sealCountDisplay,timerDisplay |
| ···3 | LeaderboardButton | 418.18,189.56→709.13,242.79 | 290.95×53.23 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,376.287) | (290.946,53.2275) | **F** | Image,EverguildButton,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | UI_Button_Mulligan 410×124 九宫333,96,333,96 | Simple (1,1,1,1) |  | trans=2 target=-887699881743058382 interactable=1 ; 字段:  ; 字段:  |
| ····4 | Button Text | 431.51,194.78→694.86,237.58 | 263.34×42.79 | (0.0355112,0.099)→(0.961262,0.903) | (0.5,0.5) | (0,-0.0606194) | (-6,0) | T | TextMeshProUGUI,Localize,AspectRatioFitter,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Leaderboard' 字号=36.0 auto[10.0~36.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: m_AspectMode,m_AspectRatio ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  ; 字段: maxFontSize,minFontSize |
| ···3 | Generic Window Red Background Small | 351.03,327.71→776.28,857.22 | 425.26×529.51 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | UI_Deck_Selection_Back_simple 440×656 九宫197,0,199,0 | Sliced (1,1,1,1) |  |  |
| ···3 | Content | 375.74,339.65→748.34,829.13 | 372.60×489.48 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.6145,8.0702) | (372.603,489.479) | T | VerticalLayoutGroup ⚠️unk |  |  |  | spacing=0.0 align=1 pad=0,0,0,0 ctrlW=0 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 |
| ····4 | Title | 381.55,339.65→742.53,387.65 | 360.98×48.00 | (0,1)→(0,1) | (0.5,0.5) | (186.301,-24) | (360.98,48) | T | TextMeshProUGUI,EverguildTextController,Localize,LayoutElement |  |  | 'Current Rank' 字号=40.0 auto[18.0~40.0] 对齐=Center/Middle 折行=0 色=(0.961,0.914,0.737,1) 字距=-2.6 | 字段: maxFontSize,minFontSize ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | RankTitleBG | 374.42,387.65→749.67,447.65 | 375.25×60.00 | (0,1)→(0,1) | (0.5,0.5) | (186.301,-78) | (375.25,60) | T | Image,LayoutElement | 40K_main_rank_display 315×64 否 | Simple (1,1,1,0.918) |  | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | DivisionText | 384.34,387.65→739.74,447.65 | 355.39×60.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,0) | (355.393,60) | T | TextMeshProUGUI,EverguildTextController |  |  | 'Division V' 字号=36.0 auto[18.0~36.0] 对齐=Center/Midline 折行=0 色=(0.961,0.914,0.737,1) 字距=-2.6 | 字段: maxFontSize,minFontSize |
| ····4 | Timer | 195.25,829.13→556.23,829.13 | 360.98×0.00 | (0,0)→(0,0) | (0.5,0.5) | (0,0) | (360.98,0) | **F** | HorizontalLayoutGroup,LayoutGroupContentFixer,TimerDisplay,LayoutElement |  |  |  | spacing=5.840000152587891 align=4 pad=0,0,5,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: fixOnEnable,layouts ; 字段: extraText,lastMinutesText,layoutGroupContentFixer,timer,timerTextDescription,useExtraText,useLastMinutesText ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Timer Icon | 272.50,815.13→305.50,848.13 | 33.00×33.00 | (0,1)→(0,1) | (0.5,0.5) | (93.75,-2.5) | (33,33) | T | Image | WF_icon_clock 64×64 否 | Simple (1,1,1,1) |  |  |
| ·····5 | Timer | 311.34,803.68→478.98,859.58 | 167.64×55.90 | (0,1)→(0,1) | (0.5,1) | (199.91,25.4525) | (167.64,55.905) | T | EverguildTextMeshPro,ContentSizeFitterMinMax |  |  | 'Ends in: 23d 5h' 字号=32.0 auto[18.0~32.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段: clampHeight,clampWidth,heightMax,heightMin,m_HorizontalFit,m_VerticalFit,widthMax,widthMin |
| ····4 | spacing | 424.94,447.65→699.14,457.65 | 274.20×10.00 | (0,1)→(0,1) | (0.5,0.5) | (186.301,-113) | (274.2,10) | T | LayoutElement |  |  |  | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | DivisionImage | 366.58,457.65→757.50,457.65 | 390.92×0.00 | (0,1)→(0,1) | (0.5,0.5) | (186.301,-118) | (390.922,0) | T | Image,DivisionImageDisplay,LayoutElement | <无图> | Simple (1,1,1,1) preserveAspect |  | 字段: divisionImage,rankImage ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | RankImage | 522.95,461.65→601.13,453.65 | 78.18×-8.00 | (0.4,0.6)→(0.6,0.7) | (0.5,0.5) | (0,0) | (0,-8) | T | Image | Roman V 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | footer | 357.04,457.65→767.04,637.65 | 410.00×180.00 | (0,1)→(0,1) | (0.5,0.5) | (186.301,-208) | (410,180) | T | VerticalLayoutGroup,LayoutElement |  |  |  | spacing=11.199999809265137 align=1 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Highest Faction Rating | 179.32,617.75→534.76,657.55 | 355.44×39.80 | (0,0)→(0,0) | (0.5,0.5) | (0,0) | (355.439,39.7997) | **F** | Image,HorizontalLayoutGroup,LayoutElement ⚠️unk | 40K_main_rank_display 315×64 否 | Simple (1,1,1,1) |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Rating Text | 357.04,608.07→357.04,667.23 | 0.00×59.17 | (0,1)→(0,1) | (0.5,0.5) | (177.719,-19.8999) | (0,59.167) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 357.04,637.65→417.04,637.65 | 60.00×0.00 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (60,0) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 417.04,637.65→477.04,637.65 | 60.00×0.00 | (0,1)→(0,1) | (0.5,0.5) | (90,-29.5835) | (60,0) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 477.04,637.65→477.04,637.65 | 0.00×0.00 | (0,1)→(0,1) | (0,0.5) | (120,-29.5835) | (0,0) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '4879' 字号=40.0 auto[18.0~40.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | MainRating | 368.85,457.65→755.23,517.98 | 386.37×60.33 | (0,1)→(0,1) | (0.5,0.5) | (205,-30.1626) | (386.373,60.3253) | **F** | Image,HorizontalLayoutGroup,LayoutElement ⚠️unk | 40K_main_rank_display 315×64 否 | Simple (1,1,1,1) **m_Enabled=0** |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=0 expandW=1 expandH=1 scaleW=0 scaleH=0 ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Mission Milestones Progress | 368.85,444.56→562.04,531.07 | 193.19×86.51 | (0,1)→(0,1) | (0.5,0.5) | (96.5934,-30.1626) | (193.187,86.5143) | T | MilestonesStepDisplay |  |  |  | 字段: stepPrefab,stepsHolder |
| ·······7 | counter | 373.15,217.13→453.15,269.36 | 80.00×52.22 | (0,1)→(0,1) | (0.5,0) | (44.3,175.2) | (80,52.2235) | **F** | TextMeshProUGUI,LayoutElement,EverguildTextController |  |  | '16' 字号=40.0 auto[15.0~40.0] 对齐=Center/Midline 折行=1 色=(0.569,0.573,0.588,1) | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth ; 字段: maxFontSize,minFontSize |
| ·······7 | steps | 368.85,444.56→562.04,531.07 | 193.19×86.51 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | HorizontalLayoutGroup |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 |
| ········8 | RankedSealStep | 368.85,457.65→417.15,517.98 | 48.30×60.33 | (0,1)→(0,1) | (0.5,0.5) | (24.1483,-43.2571) | (48.2967,60.325) | T | MilestoneStep,EverguildButtonMaterialModifier,Image,LayoutElement | Rank Skull 128×128 否 | Simple (1,1,1,0) preserveAspect |  | 字段: alwaysShowEmptyMark,animationBetweenStepsDelay,animationScaleInitialMultiplier,animationStartDelay,animationTime,checkMark,doEnterAnimation,emptyMark,soundOnAppear ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ········8 | RankedSealStep (2) | 417.15,457.65→465.45,517.98 | 48.30×60.33 | (0,1)→(0,1) | (0.5,0.5) | (72.445,-43.2571) | (48.2967,60.325) | T | MilestoneStep,EverguildButtonMaterialModifier,Image,LayoutElement | Rank Skull 128×128 否 | Simple (1,1,1,0) preserveAspect |  | 字段: alwaysShowEmptyMark,animationBetweenStepsDelay,animationScaleInitialMultiplier,animationStartDelay,animationTime,checkMark,doEnterAnimation,emptyMark,soundOnAppear ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ········8 | RankedSealStep (3) | 465.45,457.65→513.74,517.98 | 48.30×60.33 | (0,1)→(0,1) | (0.5,0.5) | (120.742,-43.2571) | (48.2967,60.325) | T | MilestoneStep,EverguildButtonMaterialModifier,Image,LayoutElement | Rank Skull 128×128 否 | Simple (1,1,1,0) preserveAspect |  | 字段: alwaysShowEmptyMark,animationBetweenStepsDelay,animationScaleInitialMultiplier,animationStartDelay,animationTime,checkMark,doEnterAnimation,emptyMark,soundOnAppear ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ········8 | RankedSealStep (4) | 513.74,457.65→562.04,517.98 | 48.30×60.33 | (0,1)→(0,1) | (0.5,0.5) | (169.038,-43.2571) | (48.2967,60.325) | T | MilestoneStep,EverguildButtonMaterialModifier,Image,LayoutElement | Rank Skull 128×128 否 | Simple (1,1,1,0) preserveAspect |  | 字段: alwaysShowEmptyMark,animationBetweenStepsDelay,animationScaleInitialMultiplier,animationStartDelay,animationTime,checkMark,doEnterAnimation,emptyMark,soundOnAppear ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Global Rating | 562.04,459.14→755.23,516.49 | 193.19×57.36 | (0,1)→(0,1) | (0.5,0.5) | (289.78,-30.1626) | (193.187,57.356) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 562.04,516.49→562.04,516.49 | 0.00×0.00 | (0,0)→(0,0) | (0,0.5) | (0,0) | (0,0) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 562.04,487.81→622.04,487.81 | 60.00×0.00 | (0,1)→(0,1) | (0.5,0.5) | (30,-28.678) | (60,0) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 622.04,487.81→755.23,487.81 | 133.19×0.00 | (0,1)→(0,1) | (0,0.5) | (60,-28.678) | (133.187,0) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '32' 字号=40.0 auto[18.0~40.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ··2 | Highest Rank | 777.72,327.71→1137.12,857.22 | 359.40×529.51 | (0,0.5)→(0,0.5) | (0.5,0.5) | (606.4,-0.00200653) | (359.4,529.508) | T | RankingDisplay |  |  |  | 字段: displayPosition,displayRating,displaySeals,divisionImage,divisionText,positionDisplay,ratingDisplay,ratingHolder,sealCountDisplay,timerDisplay |
| ···3 | LeaderboardButton | 811.95,189.56→1102.90,242.79 | 290.95×53.23 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,376.287) | (290.946,53.2275) | **F** | Image,EverguildButton,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier | UI_Button_Mulligan 410×124 九宫333,96,333,96 | Simple (1,1,1,1) |  | trans=2 target=4316085880413911602 interactable=1 ; 字段:  ; 字段:  |
| ····4 | Button Text | 825.28,194.78→1088.62,237.58 | 263.34×42.79 | (0.0355112,0.099)→(0.961262,0.903) | (0.5,0.5) | (0,-0.0606194) | (-6,0) | T | TextMeshProUGUI,Localize,AspectRatioFitter,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Leaderboard' 字号=36.0 auto[10.0~36.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: m_AspectMode,m_AspectRatio ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  ; 字段: maxFontSize,minFontSize |
| ···3 | Generic Window Red Background Small | 777.72,327.71→1137.12,857.22 | 359.40×529.51 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | UI_Deck_Selection_Back 439×664 九宫0,325,0,35 | Sliced (1,1,1,1) |  |  |
| ···3 | Content | 798.26,340.49→1113.41,806.70 | 315.14×466.21 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.5867,18.8632) | (315.143,466.212) | T | VerticalLayoutGroup ⚠️unk |  |  |  | spacing=0.0 align=1 pad=0,0,0,0 ctrlW=0 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 |
| ····4 | Title | 805.84,340.49→1105.84,388.49 | 300.00×48.00 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-24) | (300,48) | T | TextMeshProUGUI,EverguildTextController,Localize,LayoutElement |  |  | 'Highest Rank' 字号=40.0 auto[18.0~40.0] 对齐=Center/Middle 折行=0 色=(0.961,0.914,0.737,1) 字距=-2.6 | 字段: maxFontSize,minFontSize ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | RankTitleBG | 727.24,388.49→1184.44,448.49 | 457.20×60.00 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-78) | (457.2,60) | T | Image,LayoutElement | 40K_main_rank_display 315×64 否 | Simple (1,1,1,0.918) |  | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | DivisionText | 778.14,399.29→1133.53,437.69 | 355.39×38.40 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0,0) | (355.393,38.4) | T | TextMeshProUGUI,EverguildTextController |  |  | 'Division V' 字号=31.350000381469727 auto[18.0~36.0] 对齐=Center/Midline 折行=0 色=(0.961,0.914,0.737,1) 字距=-2.6 | 字段: maxFontSize,minFontSize |
| ····4 | Timer | 617.77,806.70→978.75,806.70 | 360.98×0.00 | (0,0)→(0,0) | (0.5,0.5) | (0,0) | (360.98,0) | **F** | HorizontalLayoutGroup,LayoutGroupContentFixer,TimerDisplay,LayoutElement |  |  |  | spacing=5.840000152587891 align=4 pad=0,0,5,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: fixOnEnable,layouts ; 字段: extraText,lastMinutesText,layoutGroupContentFixer,timer,timerTextDescription,useExtraText,useLastMinutesText ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Timer Icon | 695.02,792.70→728.02,825.70 | 33.00×33.00 | (0,1)→(0,1) | (0.5,0.5) | (93.75,-2.5) | (33,33) | T | Image | WF_icon_clock 64×64 否 | Simple (1,1,1,1) |  |  |
| ·····5 | Timer | 733.86,781.25→901.50,837.16 | 167.64×55.90 | (0,1)→(0,1) | (0.5,1) | (199.91,25.4525) | (167.64,55.905) | T | EverguildTextMeshPro,ContentSizeFitterMinMax |  |  | 'Ends in: 23d 5h' 字号=32.0 auto[18.0~32.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段: clampHeight,clampWidth,heightMax,heightMin,m_HorizontalFit,m_VerticalFit,widthMax,widthMin |
| ····4 | DivisionImage | 760.37,448.49→1151.30,448.49 | 390.92×0.00 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-108) | (390.922,0) | T | Image,DivisionImageDisplay | <无图> | Simple (1,1,1,1) preserveAspect |  | 字段: divisionImage,rankImage |
| ·····5 | RankImage | 916.74,456.49→994.93,448.49 | 78.18×-8.00 | (0.4,0.6)→(0.6,0.7) | (0.5,0.5) | (0,-4) | (0,-8) | T | Image | Roman V 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | footer | 750.84,448.49→1160.84,589.39 | 410.00×140.90 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-178.45) | (410,140.9) | T | VerticalLayoutGroup,LayoutElement |  |  |  | spacing=-3.4000000953674316 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Highest Faction Rating | 778.12,464.80→1133.55,464.80 | 355.44×0.00 | (0,1)→(0,1) | (0.5,0.5) | (205,-16.3099) | (355.439,0) | **F** | Image,HorizontalLayoutGroup,LayoutElement ⚠️unk | 40K_main_rank_display 315×64 否 | Simple (1,1,1,1) |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Rating Text | 955.84,446.85→955.84,482.75 | 0.00×35.90 | (0,1)→(0,1) | (0.5,0.5) | (177.719,0) | (0,35.9) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 955.84,464.80→955.84,464.80 | 0.00×0.00 | (0,1)→(0,1) | (0,0.5) | (0,-17.95) | (0,0) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 955.84,464.80→955.84,464.80 | 0.00×0.00 | (0,1)→(0,1) | (0.5,0.5) | (0,-17.95) | (0,0) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 955.84,464.80→955.84,464.80 | 0.00×0.00 | (0,1)→(0,1) | (0,0.5) | (0,-17.95) | (0,0) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '4879' 字号=37.849998474121094 auto[18.0~40.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | MainRating | 762.65,497.39→1149.02,540.49 | 386.37×43.10 | (0,1)→(0,1) | (0.5,0.5) | (205,-70.45) | (386.373,43.1) | T | Image,HorizontalLayoutGroup,LayoutElement ⚠️unk | 40K_main_rank_display 315×64 否 | Simple (1,1,1,1) |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=0 expandW=1 expandH=1 scaleW=0 scaleH=0 ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Mission Milestones Progress | 762.65,475.68→1118.09,562.20 | 355.44×86.51 | (0,1)→(0,1) | (0.5,0.5) | (177.719,-21.55) | (355.439,86.5143) | **F** | MilestonesStepDisplay |  |  |  | 字段: stepPrefab,stepsHolder |
| ·······7 | counter | 766.95,248.26→846.95,300.48 | 80.00×52.22 | (0,1)→(0,1) | (0.5,0) | (44.3,175.2) | (80,52.2235) | **F** | TextMeshProUGUI,LayoutElement,EverguildTextController |  |  | '16' 字号=40.0 auto[15.0~40.0] 对齐=Center/Midline 折行=1 色=(0.569,0.573,0.588,1) | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth ; 字段: maxFontSize,minFontSize |
| ·······7 | steps | 762.65,475.68→1118.09,562.20 | 355.44×86.51 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | **F** | HorizontalLayoutGroup |  |  |  | spacing=12.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 |
| ········8 | RankedSealStep | 890.37,496.75→990.37,541.14 | 100.00×44.39 | (0,1)→(0,1) | (0.5,0.5) | (177.719,-43.2571) | (100,44.3929) | T | MilestoneStep,EverguildButtonMaterialModifier,Image,LayoutElement | Rank Skull 128×128 否 | Simple (1,1,1,0) preserveAspect |  | 字段: alwaysShowEmptyMark,animationBetweenStepsDelay,animationScaleInitialMultiplier,animationStartDelay,animationTime,checkMark,doEnterAnimation,emptyMark,soundOnAppear ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Global Rating | 762.65,501.74→1149.02,536.14 | 386.37×34.40 | (0,1)→(0,1) | (0.5,0.5) | (193.187,-21.55) | (386.373,34.4) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 762.65,536.14→762.65,536.14 | 0.00×0.00 | (0,0)→(0,0) | (0,0.5) | (0,0) | (0,0) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 762.65,496.44→822.65,541.44 | 60.00×45.00 | (0,1)→(0,1) | (0.5,0.5) | (30,-17.2) | (60,45) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 822.65,518.94→1149.02,518.94 | 326.37×0.00 | (0,1)→(0,1) | (0,0.5) | (60,-17.2) | (326.373,0) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '32' 字号=36.29999923706055 auto[18.0~40.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | legendary title | 818.74,589.39→1092.94,619.39 | 274.20×30.00 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-263.9) | (274.2,30) | T | TextMeshProUGUI,Localize,EverguildTextController,LayoutElement |  |  | 'Legendary Points' 字号=28.0 auto[18.0~28.0] 对齐=Center/Middle 折行=0 色=(0.961,0.914,0.737,1) 字距=-2.6 | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | Legendarey Counter | 789.43,619.39→1122.24,619.39 | 332.80×0.00 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-278.9) | (332.805,0) | T | Image,HorizontalLayoutGroup | 40K_main_rank_display 315×64 否 | Simple (1,1,1,1) |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=1 expandH=1 scaleW=0 scaleH=0 |
| ·····5 | Rating Text | 790.50,601.25→1121.17,637.54 | 330.67×36.29 | (0,1)→(0,1) | (0.5,0.5) | (166.402,0) | (330.668,36.2908) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 790.50,637.54→790.50,637.54 | 0.00×0.00 | (0,0)→(0,0) | (0,0.5) | (0,0) | (0,0) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 790.50,596.89→850.50,641.89 | 60.00×45.00 | (0,1)→(0,1) | (0.5,0.5) | (30,-18.1454) | (60,45) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 850.50,619.39→1121.17,619.39 | 270.67×0.00 | (0,1)→(0,1) | (0,0.5) | (60,-18.1454) | (270.668,0) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '32' 字号=32.0 auto[18.0~32.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | spacing | 818.74,619.39→1092.94,641.69 | 274.20×22.30 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-290.05) | (274.2,22.3) | T | LayoutElement |  |  |  | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ··2 | Legendary Display Profile | 777.72,327.71→1137.12,857.22 | 359.40×529.51 | (0,0.5)→(0,0.5) | (0.5,0.5) | (606.4,-0.00200653) | (359.4,529.508) | T | LegendaryRankAllTrophiesDisplay |  |  |  | 字段: ratingDisplay,trophyDisplayBronze,trophyDisplayGold,trophyDisplaySilver |
| ···3 | Generic Window Red Background Small | 777.72,327.71→1137.12,857.22 | 359.40×529.51 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | UI_Deck_Selection_Back 439×664 九宫0,325,0,35 | Sliced (1,1,1,1) |  |  |
| ···3 | Content | 798.26,340.49→1113.41,806.70 | 315.14×466.21 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.5867,18.8632) | (315.143,466.212) | T | VerticalLayoutGroup |  |  |  | spacing=11.550000190734863 align=1 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 |
| ····4 | DivisionImage | 760.37,340.49→1151.30,671.45 | 390.92×330.96 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-165.482) | (390.922,330.963) | T | Image,LayoutElement | 07-Legend 512×512 否 ppu=50 | Simple (1,1,1,0.306) preserveAspect |  | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | RankImage | 916.74,447.78→994.93,472.88 | 78.18×25.10 | (0.4,0.6)→(0.6,0.7) | (0.5,0.5) | (0,-4) | (0,-8) | **F** | Image | Roman V 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | legendary title | 818.74,683.00→1092.94,717.26 | 274.20×34.26 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-359.643) | (274.2,34.26) | T | TextMeshProUGUI,Localize,EverguildTextController,LayoutElement |  |  | 'Legendary Points' 字号=28.0 auto[18.0~28.0] 对齐=Center/Middle 折行=0 色=(0.961,0.914,0.737,1) 字距=-2.6 | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | Legendary Counter | 789.43,728.81→1122.24,771.71 | 332.80×42.89 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-409.77) | (332.805,42.894) | T | Image,HorizontalLayoutGroup | 40K_main_rank_display 315×64 否 | Simple (1,1,1,1) |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=1 expandH=1 scaleW=0 scaleH=0 |
| ·····5 | Rating Text | 790.50,732.12→1121.17,768.41 | 330.67×36.29 | (0,1)→(0,1) | (0.5,0.5) | (166.402,-21.447) | (330.668,36.2908) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 790.50,768.41→790.50,768.41 | 0.00×0.00 | (0,0)→(0,0) | (0,0.5) | (0,0) | (0,0) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 790.50,727.76→850.50,772.76 | 60.00×45.00 | (0,1)→(0,1) | (0.5,0.5) | (30,-18.1454) | (60,45) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 850.50,750.26→1121.17,750.26 | 270.67×0.00 | (0,1)→(0,1) | (0,0.5) | (60,-18.1454) | (270.668,0) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '32' 字号=32.0 auto[18.0~32.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | Player Profile Ranked Trophies Gold | 799.33,783.26→1112.35,890.14 | 313.02×106.88 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-496.206) | (313.02,106.878) | T | PlayerProfileTrophiesDisplay |  |  |  | 字段: trophiesCountText |
| ·····5 | Background | 824.71,783.72→1106.14,890.60 | 281.43×106.88 | (0,1)→(0,1) | (0.5,0.5) | (166.1,-53.9) | (281.431,106.878) | T | Image | WF_UI_Ranked_Background_Gold 352×116 九宫20,20,20,20 | Simple (1,1,1,1) |  |  |
| ·····5 | Icon | 767.37,780.24→881.22,894.08 | 113.85×113.85 | (0,1)→(0,1) | (0.5,0.5) | (24.972,-53.9) | (113.845,113.845) | T | Image | WF_UI_Trophy_Gold 124×124 否 | Simple (1,1,1,1) |  |  |
| ·····5 | Victories number | 861.90,798.79→1081.22,844.33 | 219.32×45.54 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (15.724,15.1388) | (219.32,45.5355) | T | EverguildTextMeshPro |  |  | '0' 字号=36.0 对齐=Center/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | Victories text | 861.90,832.86→1081.22,882.86 | 219.32×50.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (15.7238,-21.1612) | (219.321,50) | T | EverguildTextMeshPro,Localize |  |  | 'Trophies' 字号=36.0 对齐=Center/Capline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary |
| ····4 | Player Profile Ranked Trophies Silver | 799.33,901.69→1112.35,1008.56 | 313.02×106.88 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-614.633) | (313.02,106.878) | T | PlayerProfileTrophiesDisplay |  |  |  | 字段: trophiesCountText |
| ·····5 | Background | 824.71,902.15→1106.14,1009.03 | 281.43×106.88 | (0,1)→(0,1) | (0.5,0.5) | (166.1,-53.9) | (281.431,106.878) | T | Image | WF_UI_Ranked_Background_Silver 352×116 九宫20,20,20,20 | Simple (1,1,1,1) |  |  |
| ·····5 | Icon | 767.37,898.66→881.22,1012.51 | 113.85×113.85 | (0,1)→(0,1) | (0.5,0.5) | (24.972,-53.9) | (113.845,113.845) | T | Image | WF_UI_Trophy_Gold 124×124 否 | Simple (1,1,1,1) |  |  |
| ·····5 | Victories number | 861.90,917.22→1081.22,962.75 | 219.32×45.54 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (15.724,15.1388) | (219.32,45.5355) | T | EverguildTextMeshPro |  |  | '0' 字号=36.0 对齐=Center/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | Victories text | 861.90,951.29→1081.22,1001.29 | 219.32×50.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (15.7238,-21.1612) | (219.321,50) | T | EverguildTextMeshPro,Localize |  |  | 'Trophies' 字号=36.0 对齐=Center/Capline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary |
| ····4 | Player Profile Ranked Trophies Bronze | 799.33,1020.11→1112.35,1126.99 | 313.02×106.88 | (0,1)→(0,1) | (0.5,0.5) | (157.571,-733.061) | (313.02,106.878) | T | PlayerProfileTrophiesDisplay |  |  |  | 字段: trophiesCountText |
| ·····5 | Background | 824.71,1020.58→1106.14,1127.45 | 281.43×106.88 | (0,1)→(0,1) | (0.5,0.5) | (166.1,-53.9) | (281.431,106.878) | T | Image | WF_UI_Ranked_Background_Bronze 352×116 九宫20,20,20,20 | Simple (1,1,1,1) |  |  |
| ·····5 | Icon | 767.37,1017.09→881.22,1130.94 | 113.85×113.85 | (0,1)→(0,1) | (0.5,0.5) | (24.972,-53.9) | (113.845,113.845) | T | Image | WF_UI_Trophy_Gold 124×124 否 | Simple (1,1,1,1) |  |  |
| ·····5 | Victories number | 861.90,1035.65→1081.22,1081.18 | 219.32×45.54 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (15.724,15.1388) | (219.32,45.5355) | T | EverguildTextMeshPro |  |  | '0' 字号=36.0 对齐=Center/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·····5 | Victories text | 861.90,1069.71→1081.22,1119.71 | 219.32×50.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (15.7238,-21.1612) | (219.321,50) | T | EverguildTextMeshPro,Localize |  |  | 'Trophies' 字号=36.0 对齐=Center/Capline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary |
| ·1 | Events | 1047.88,327.71→1730.12,857.22 | 682.24×529.51 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (340,-52.001) | (682.24,529.51) | T | VerticalLayoutGroup,ProfileEventSection |  |  |  | spacing=5.0 align=8 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: campaignContainer,forgeContainer,warlordContainer |
| ··2 | Warlord  Mastery Container | 1175.12,327.71→1730.12,502.71 | 555.00×175.00 | (0,1)→(0,1) | (0.5,0.5) | (404.74,-87.5) | (555,175) | T | ProfileContainerComponents,WarlordMasteryContainer |  |  |  | 字段: armyIcon,armyName,background,level,title ; 字段: backgroundButton,components |
| ···3 | Player Profile Container Base | 1175.12,327.71→1730.12,502.71 | 555.00×175.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) preserveAspect |  |  |
| ···3 | Title | 1195.12,340.78→1475.52,401.72 | 280.40×60.93 | (0,1)→(0,1) | (0,0.5) | (20,-43.546) | (280.4,60.9322) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Highest Warlod Mastery' 字号=25.0 auto[12.0~25.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize |
| ···3 | Badge | 1397.12,94.39→1837.12,534.39 | 440.00×440.00 | (1,0)→(1,0) | (0.5,0) | (-113,-31.68) | (440,440) | T | Image | <无图> | Simple (1,1,1,1) |  |  |
| ···3 | ArmyName | 1195.12,401.72→1484.58,447.24 | 289.46×45.53 | (0,1)→(0,1) | (0,0.5) | (20,-96.776) | (289.464,45.527) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Ultramarines' 字号=35.0 auto[14.0~35.0] 对齐=Left/Bottom 折行=1 色=(0.992,0.647,0.188,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize |
| ···3 | Level | 1195.12,444.07→1507.39,476.07 | 312.27×32.00 | (0,1)→(0,1) | (0,0.5) | (20,-132.37) | (312.27,32) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Level: 0' 字号=27.0 auto[23.0~27.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize |
| ··2 | Forge Profile Container | 1175.12,507.71→1730.12,682.71 | 555.00×175.00 | (0,1)→(0,1) | (0.5,0.5) | (404.74,-267.5) | (555,175) | T | ProfileContainerComponents,ForgePlayerContainer |  |  |  | 字段: armyIcon,armyName,background,level,title ; 字段: backgroundButton,components |
| ···3 | Player Profile Container Base | 1175.12,507.71→1730.12,682.71 | 555.00×175.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | 40K_profile_ForgeLevel_bg 631×194 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ···3 | Title | 1194.38,509.48→1677.43,569.48 | 483.05×60.00 | (0,1)→(0,1) | (0.5,0.5) | (260.783,-31.7787) | (483.051,60) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Current campaign' 字号=25.0 auto[18.0~25.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize |
| ···3 | Badge | 1198.59,560.73→1321.65,665.02 | 123.07×104.28 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-192.5,-17.6701) | (123.065,104.285) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ···3 | ArmyName | 1330.87,565.64→1571.32,627.24 | 240.45×61.60 | (0,1)→(0,1) | (0.5,0.5) | (275.977,-88.738) | (240.452,61.603) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Ultramarines' 字号=35.0 auto[13.0~35.0] 对齐=Left/Bottom 折行=1 色=(0.992,0.647,0.188,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize |
| ···3 | Level | 1330.88,624.07→1643.14,656.07 | 312.27×32.00 | (0,1)→(0,1) | (0.5,0.5) | (311.89,-132.37) | (312.27,32) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Level: 0' 字号=27.0 auto[23.0~27.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize |
| ··2 | Campaign Profile Container | 1175.12,687.71→1730.12,862.71 | 555.00×175.00 | (0,1)→(0,1) | (0.5,0.5) | (404.74,-447.5) | (555,175) | T | ProfileContainerComponents,CampaignProfileContainer |  |  |  | 字段: armyIcon,armyName,background,level,title ; 字段: backgroundButton,components |
| ···3 | Player Profile Container Base | 1175.12,687.71→1730.12,862.71 | 555.00×175.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) preserveAspect |  |  |
| ···3 | Title | 1194.38,689.48→1677.43,749.48 | 483.05×60.00 | (0,1)→(0,1) | (0.5,0.5) | (260.783,-31.7787) | (483.051,60) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Current campaign' 字号=25.0 auto[18.0~25.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize |
| ···3 | Badge | 1198.59,740.73→1321.65,845.02 | 123.07×104.28 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-192.5,-17.6701) | (123.065,104.285) | T | Image | <无图> | Simple (1,1,1,1) preserveAspect |  |  |
| ···3 | ArmyName | 1330.87,745.64→1571.32,807.24 | 240.45×61.60 | (0,1)→(0,1) | (0.5,0.5) | (275.977,-88.738) | (240.452,61.603) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Ultramarines' 字号=35.0 auto[23.0~35.0] 对齐=Left/Bottom 折行=1 色=(0.992,0.647,0.188,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize |
| ···3 | Level | 1330.88,804.07→1643.14,836.07 | 312.27×32.00 | (0,1)→(0,1) | (0.5,0.5) | (311.89,-132.37) | (312.27,32) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Level: 0' 字号=27.0 auto[23.0~27.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize |
| ·1 | ChooseNameWindow | -0.13,0.00→1920.13,1080.00 | 1920.25×1080.00 | (0,0)→(1,1) | (0.5,0.5) | (-89,0.459015) | (524.318,236.916) | **F** | UIAnchorAvoidSafeArea,ChangeNameWindow |  |  |  | 字段: applyHeight,applyWidth,autoFindWidthAndHeightApplyOptions,rectTransform,tempPivotX,tempSizeDeltaX ; 字段: changeNameButton,closeButton,closeButtonBackground,closeOnESC,closeSound,extraScaleSmallScreen,freeTextInButton,nameInput,openSound,priceDisplayInButton,type,updateNavPanel,useDefaultCloseSoundIfNull,windowsPlacement |
| ··2 | Dark Background | -620.63,-213.74→2540.63,1563.74 | 3161.26×1777.49 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.65701e-05,-135) | (3161.26,1777.49) | T | Image,BackgroundCloseButton | <无图> | Simple (0,0,0,0.694) |  | 字段: onClick,window |
| ··2 | Generic Popup Background | 519.49,395.00→1400.51,696.03 | 881.02×301.03 | (0,0)→(1,1) | (0.5,0.5) | (0,-5.513) | (-1039.23,-778.974) | T | Image | 40k_popup 359×336 九宫169,160,169,160 | Sliced (1,1,1,1) |  |  |
| ···3 | Mask | 529.89,404.44→1390.64,686.22 | 860.75×281.78 | (0,0)→(1,1) | (0.5,0.5) | (0.262024,0.178986) | (-20.268,-19.245) | T | Image,Mask | 40k_popup 359×336 九宫169,160,169,160 | Sliced (1,1,1,1) |  | showGraphic=0 |
| ····4 | Background fill | 529.89,404.44→1390.64,686.22 | 860.75×281.78 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | 40k_popup_texture 128×128 否 | Tiled (1,1,1,1) ppuMul=2.0 |  |  |
| ··2 | Choose Name Input Field | 540.13,496.00→1376.29,556.00 | 836.16×60.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.794,14) | (836.16,60) | T | Image,EverguildInputField | 40K_dropdown_bg 119×102 九宫23,20,23,20 | Sliced (0.286,0.965,0.686,1) ppuMul=1.2000000476837158 |  | 字段: enableScroll,isAlert,m_AnimationTriggers,m_AsteriskChar,m_CaretBlinkRate,m_CaretColor,m_CaretWidth,m_CharacterLimit,m_CharacterValidation,m_Colors,m_ContentType,m_CustomCaretColor,m_GlobalFontAsset,m_GlobalPointSize,m_HideMobileInput,m_HideSoftKeyboard,m_InputType,m_InputValidator,m_Interactable,m_KeepTextSelectionVisible,m_KeyboardType,m_LayoutGroup,m_LineLimit,m_LineType,m_Navigation,m_OnDeselect |
| ···3 | Text Area | 550.13,503.00→1366.29,550.00 | 816.16×47.00 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.5) | (-20,-13) | T | RectMask2D |  |  |  | 字段: m_Padding,m_Softness |
| ····4 | Placeholder | 550.13,503.00→1366.29,550.00 | 816.16×47.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI,LayoutElement |  |  | '' 字号=18.0 auto[18.0~40.0] 对齐=Center/Middle 折行=0 色=(0.22,0.22,0.22,0.5) | 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | Text | 550.13,503.00→1366.29,550.00 | 816.16×47.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI |  |  | '\u200b' 字号=40.0 auto[18.0~40.0] 对齐=Left/Middle 折行=3 色=(1,1,1,1) |  |
| ··2 | MessageText | 540.13,426.87→1376.28,501.13 | 836.15×74.26 | (0,0.5)→(1,0.5) | (0.5,0.5) | (-1.79712,76) | (-1084.1,74.261) | T | TextMeshProUGUI,Localize |  |  | 'Choose your player name' 字号=40.0 auto[4.0~40.0] 对齐=Center/Midline 折行=1 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary |
| ··2 | Change Name Button | 822.82,578.22→1097.18,645.78 | 274.36×67.56 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (6.10352e-05,-72) | (274.358,67.561) | T | PriceDisplayButton |  |  |  | 字段: blinkEffect,button,priceDisplay |
| ···3 | Generic UI Button | 822.82,578.22→1097.18,645.78 | 274.36×67.56 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButton,EverguildButtonMaterialModifier,UIEffect,UIEffectTweener | 40K_button 489×107 九宫234,46,234,46 | Simple (0.369,0.894,0.587,1) preserveAspect ppuMul=0.009999999776482582 |  | trans=2 target=7743085296280631858 interactable=1 ; 字段:  ; 字段: m_AllowToModifyMeshShape,m_BlendType,m_ColorFilter,m_ColorGlow,m_ColorIntensity,m_CustomRoot,m_DetailColor,m_DetailFilter,m_DetailIntensity,m_DetailTex,m_DetailTexOffset,m_DetailTexScale,m_DetailTexSpeed,m_DetailThreshold,m_DstBlendMode,m_EdgeColor,m_EdgeColorFilter,m_EdgeColorGlow,m_EdgeMode,m_EdgeShinyAutoPlaySpeed,m_EdgeShinyRate,m_EdgeShinyWidth,m_EdgeWidth,m_Flip,m_GradationColor1,m_GradationColor2 ; 字段: m_CullingMask,m_Curve,m_Delay,m_Direction,m_Duration,m_Interval,m_OnChangedRate,m_OnComplete,m_PlayOnEnable,m_ResetTimeOnEnable,m_ReverseCurve,m_SeparateReverseCurve,m_UpdateMode,m_WrapMode |
| ····4 | Button Text | 835.82,587.84→1084.18,636.16 | 248.36×48.31 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-26,-19.2477) | T | TextMeshProUGUI,Localize,AspectRatioFitter,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'Free' 字号=50.0 auto[12.0~50.0] 对齐=Center/Midline 折行=0 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: m_AspectMode,m_AspectRatio ; 字段:  ; 字段: maxFontSize,minFontSize |
| ····4 | Price Display | 833.20,586.77→1084.64,638.24 | 251.44×51.47 | (0.0378438,0.108477)→(0.954309,0.876549) | (0.5,0.5) | (0,0) | (0,-0.417889) | **F** | HorizontalLayoutGroup,LayoutGroupContentFixer,AspectRatioFitter,PriceDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=1 expandW=0 expandH=1 scaleW=1 scaleH=1 ; 字段: fixOnEnable,layouts ; 字段: m_AspectMode,m_AspectRatio ; 字段: contentFixer,icon,text,useStrikeThrough |
| ·····5 | icon | 887.08,586.77→938.55,638.24 | 51.47×51.47 | (0,1)→(0,1) | (0.5,0.5) | (79.6097,-25.7369) | (51.4738,51.4738) | T | Image,EverguildButtonMaterialModifier,AspectRatioFitter,LayoutElement | <无图> | Simple (1,1,1,1) |  | 字段:  ; 字段: m_AspectMode,m_AspectRatio ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | text | 938.55,586.77→1030.77,638.24 | 92.22×51.47 | (0,1)→(0,1) | (0.5,0.5) | (151.457,-25.7369) | (92.22,51.4738) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,ContentSizeFitter,EverguildTextController |  |  | '300,00' 字号=40.0 auto[13.460000038146973~40.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段:  ; 字段: m_HorizontalFit,m_VerticalFit ; 字段: maxFontSize,minFontSize |
| ··2 | Generic Close Button Green | 1358.30,362.50→1433.30,437.50 | 75.00×75.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (435.8,140) | (75,75) | T | Image,EverguildButton,EverguildButtonMaterialModifier | UI_Button_Round_background 237×237 否 | Simple (1,1,1,1) preserveAspect |  | trans=2 target=-5517809430966076878 interactable=1 ; 字段:  |
| ···3 | Icon | 1367.62,372.75→1423.98,427.25 | 56.37×54.50 | (0.126634,0.13672)→(0.873366,0.86328) | (0.5,0.5) | (0,0) | (0.364201,0.00779724) | T | Image,EverguildButtonMaterialModifier | 40k_bt_close 175×174 否 | Simple (1,1,1,1) |  | 字段:  |

⚠️ 布局组（子节点位置**由布局算**，上面已是**布局跑之后**的值）：
          Name and Title Holder → HorizontalLayoutGroup [unk]
            Alliance Rating Display → HorizontalLayoutGroup [ok]
          Name and Title Holder → VerticalLayoutGroup [ok]
          Content → VerticalLayoutGroup [unk]
            Timer → HorizontalLayoutGroup [ok]
              Highest Faction Rating → HorizontalLayoutGroup [unk]
                Rating Text → HorizontalLayoutGroup [ok]
              MainRating → HorizontalLayoutGroup [unk]
                  steps → HorizontalLayoutGroup [ok]
                Global Rating → HorizontalLayoutGroup [ok]
          Content → VerticalLayoutGroup [unk]
            Timer → HorizontalLayoutGroup [ok]
            footer → VerticalLayoutGroup [ok]
              Highest Faction Rating → HorizontalLayoutGroup [unk]
                Rating Text → HorizontalLayoutGroup [ok]
              MainRating → HorizontalLayoutGroup [unk]
                  steps → HorizontalLayoutGroup [ok]
                Global Rating → HorizontalLayoutGroup [ok]
            Legendarey Counter → HorizontalLayoutGroup [ok]
              Rating Text → HorizontalLayoutGroup [ok]
          Content → VerticalLayoutGroup [ok]
            Legendary Counter → HorizontalLayoutGroup [ok]
              Rating Text → HorizontalLayoutGroup [ok]
      Events → VerticalLayoutGroup [ok]
            Price Display → HorizontalLayoutGroup [ok]

🔴 **下面这些布局组的主轴尺寸算不准**（子节点里有文字/嵌套布局件/ScrollRect，首选尺寸要 Unity 的字体度量）—— 表里那几个子节点的值**别照抄**：
          Name and Title Holder
          Content
              Highest Faction Rating
              MainRating
          Content
              Highest Faction Rating
              MainRating
```


---

# 附：`Profile Tab` 的判据（2026-09-27 只读普查；上面那张表是工具原文，下面是补工具给不了的）

## A.1 `activeSelf` 的出现条件 —— **字段↔节点绑定（权威，不是推断）**

`d:/2/tools/il2cpp_out/dump.cs:83445-83463` 给出 `ProfileTab` 8 个字段的**字节偏移**；
`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_3020493274243496498.json` 给出**字段→组件 pid**；
组件 pid→节点名见下表。两者逐位吻合，故下列映射可直接施工。

| 字段(偏移) | 组件 pid | 节点名 | 节点 RT pid |
|---|---|---|---|
| `infoSection` (0x30) | 8480540536928893490 | **Player Info** | 4482408997560220210 |
| `rankingSection` (0x38) | -8890416321618347470 | **Ranking** | -1261423525344413134 |
| `eventSection` (0x40) | -3139430380039931342 | **Events** | -5749883658092905934 |
| `playerIdDisplay` (0x48) | 2762637106463144498 | **PlayerId** | -4022454382704297422 |
| `consecutiveLoginCount` (0x50) | 5528547971332733490 | **`Consecutive login days` 的子节点 `playerIdText`** | 593800766499617330 |
| `inviteToAllianceButton` (0x58) | 7147149353371007538 | **Invite to alliance** | -4684866084660413902 |
| `changeNameButton` (0x60) | 5212599085320665650 | **`Info Section with Alliance`/Name and Title Holder/Edit Name Button** | 5897212879450372658 |
| `changeNameWindow` (0x68) | -2935183011022079438 | **ChooseNameWindow** | 8940782185331128882 |

⚠️ 两条容易踩的：
① `consecutiveLoginCount` 绑的是 **`Consecutive login days` 的子节点 TMP**，不是那个 `EverguildButton` 节点；
② `changeNameButton` 绑的是 **with-Alliance 那一份** `Edit Name Button`（全包 6 个同名节点）。
而 `ProfileTab` 的**任何**方法体里都**没有读过 0x60**（`Start` 用 0x30/0x58/0x68，
`Initialize` 用 0x30/0x38/0x40/0x48/0x50/0x58，`OnChangePlayerNameButtonClick` 用 0x68）
⇒ **这个字段是「只序列化不读」的遗留字段**。

### 出厂就 active / 出厂就 inactive 的判定

工具表 `act` 列 = prefab 出厂值。**全树 171 个节点里 `act=F` 的有 24 个**，逐个的切换点如下（"无" = 反编译里找不到切换点）：

| 节点（RT pid） | 出厂 | 谁切 | 条件（出处） |
|---|---|---|---|
| **Profile Tab** (-8094654694055052750) | **F** | `WindowTabBase.TryOpenTab` / `CloseTab` | 开 = `SetActive(true)` + 调 slot 0x1b8(`OnOpen`)（`WindowTabBase__TryOpenTab.c:9,12`）；关 = `SetActive(false)`（`WindowTabBase__CloseTab.c:9,12`）。调用者 = `GameWindowWithTabs__ChangeTabCO.c:37,51` |
| Consecutive login days (-7254068589549028814) | **F** | **无** | 全包无任何 MonoBehaviour 字段指向它；`Initialize` 只改它子节点的文字 |
| Avatar Name (3784387683955145266) | **F** | **无** | 只被 `AvatarDisplay.avatarName`(0x78) 做 `set_text`（`AvatarDisplay__ChangeAvatar.c:73-85`），**全无 SetActive** |
| **Invite to alliance** (-4684866084660413902) | **T** | `ProfileTab.Initialize:126-131` | 唯一显式 `SetActive(false)` 在函数末尾；**保持 T 只有一条窄路**：`PlayerInfo.AllianceInfo.AllianceName` 为空 **且** `AlliancesManager.IsInGroup` **且** `CurrentPlayerData.<0x48> > 0` **且** 群成员 `Any(m => …)` 为假 **且** `Group.MemberCount < ConfigManager.GetConfig<T>().<0x10>`。**其余所有情况（含被看的人有联盟名、你自己不在联盟、名单已达上限）都 `SetActive(false)`**。文字 = `HasBeenInvited(0xA8) ? 'SocialMenu/Alliances/CancelInvitation' : 'SocialMenu/Alliances/InvitePlayer'` |
| Info Section with Alliance (-3737462385680942542) | **F** | `PlayerInfoDisplay.Initialize` | 与 without-Alliance **互斥二选一**：`AllianceName` 非空 **且** `FeatureConfig.CheckFeature(...)` → 这份 ON、without OFF（`PlayerInfoDisplay__Initialize.c:87-91`）；否则相反（`:119-122`）。**出厂态 = with-OFF / without-ON** |
| Alliance Rating Display/Secondary Icon (-6690235967224055246) | **F** | `AllianceRatingDisplay.Initialize:49` | 无 ratingIcon 图时关 |
| Current Rank/LeaderboardButton (-2635690614688941518) | **F** | **无** | 无字段引用 |
| Current Rank/Timer (-2307440796202141134) | **F** | **无** | `RankingDisplay.timerDisplay`(0x30) 只 `TimerDisplay.Initialize` 其内容（`RankingDisplay__Initialize.c:251-255`），**从不 SetActive 它** |
| Current Rank/footer/Highest Faction Rating (1988919471508322866) | **F** | **无** | 无字段引用（连它的子 `Rating Text` 也没被引用） |
| Current Rank/MainRating (5086365602354723378) | **T** | `RankingDisplay.Initialize` | = `ratingHolder`(0x38)：`displaySeals`(0x5A)=true → ON（Current Rank 是 1）；false → OFF（Highest Rank 是 0）（`RankingDisplay__Initialize.c:114-115`） |
| Highest Rank/MainRating (6688856231989705266) | **F** | 同上（另一实例） | 同上，`displaySeals=0` |
| Current Rank 与 Highest Rank 的 `counter` (-980707202110948814 / 949999161511803442) | **F** | **无** | 无字段引用 |
| Highest Rank/Mission Milestones Progress (1631788998445464114) | **F** | `RankingDisplay.Initialize` | = `sealCountDisplay`(0x50)：`SetActive(go, activeSelf & param_6)`（`:245`），且 `displaySeals` 分支里置 0（`:134`）；**Current Rank 那份 = T**（displaySeals=1） |
| Highest Rank/…/steps (2779539381472950834) | **F** | **无** | `MilestonesStepDisplay.stepsHolder`(0x20) 持有它，但 `MilestonesStepDisplay__Initialize.c` 里**没有 SetActive**（Current Rank 同名节点是 T） |
| Highest Rank/Global Rating/Secondary Icon (7823287605963225650) | **F** | `AllianceRatingDisplay.Initialize:49` | 无 ratingIcon |
| Highest Rank/Legendarey Counter/Rating Text/Secondary Icon (-283158505017673166) | **F** | 同上 | 无 ratingIcon |
| Legendary Display Profile/DivisionImage/RankImage (7619425932004915762) | **F** | `DivisionImageDisplay.Initialize:52` | 该实例的角色图未填 → 关 |
| **ChooseNameWindow** (8940782185331128882) | **F** | `ProfileTab.Start:19`（显式 `SetActive(false)`） | 打开 = `WindowsManager.OpenWindow(changeNameWindow)`（`ProfileTab__OnChangePlayerNameButtonClick.c:13`） |
| Change Name Button/Price Display (-3104970532793386446) | **F** | `ChangeNameWindow.Open:36-48` | `PlayerDataManager.TimesNameChange >= 1` → PriceDisplay **ON**、`freeTextInButton`('Free') **OFF**；`< 1` 反之（**两套参数，不是一套**） |

**两组「出厂 T / 运行期互斥」**（铁律「一个值 ≠ 全部情况」的两处实例）：

- `Highest Rank` + `Legendarey Counter/Rating Text` ↔ `Legendary Display Profile`：`ProfileRankingSection.Initialize:103-111`
  走「非传奇」支 → 前者 ON / 后者 OFF；`:146-154` 走「传奇」支 → 相反。**出厂两份都是 T ⇒ 出厂态不是任何一个运行态**。
- `Info Section with Alliance` ↔ `Info Section without Alliance`（见上）。
- `Button Text('Free')` ↔ `Price Display`（在 ChooseNameWindow 里，见上）。

**`extraScaleSmallScreen`（小屏第二套参数）**：这个属性在**窗口根**上，不在页签里 ——
`PlayerProfileMenu` = **1.075**、`ChangeNameWindow` = **1.2**。
本表所有坐标是**参考分辨率（1920×1080、无小屏缩放）那一套**，**只覆盖这一种**；小屏那套没算。

## A.2 `ppu ≠ 100 / 九宫格 ≠ 全 0` 的图（逐条点名）

**九宫格 ≠ 全 0（11 张 sprite，跨 16 个节点）**：

| sprite | 原尺寸 | 九宫格 | 出现在 |
|---|---|---|---|
| `40k_menu_bt_general_bg` | 51×52 | 15,15,15,15 | Invite to alliance；Edit Name Button ×2 |
| `40k_menu_bt__general_outline` | 51×52 | 15,15,15,15 | 上述三处的 Button Outline |
| `UI_Button_Mulligan` | 410×124 | **333,96,333,96** | LeaderboardButton ×2 |
| `UI_Deck_Selection_Back_simple` | 440×656 | **197,0,199,0** | Current Rank/Generic Window Red Background Small |
| `UI_Deck_Selection_Back` | 439×664 | **0,325,0,35** | Highest Rank 与 Legendary Display Profile 的 Generic Window Red Background Small |
| `40k_popup` | 359×336 | **169,160,169,160** | ChooseNameWindow/Generic Popup Background + 其 Mask |
| `40K_dropdown_bg` | 119×102 | 23,20,23,20 | Choose Name Input Field |
| `40K_button` | 489×107 | **234,46,234,46** | Change Name Button/Generic UI Button |
| `UI_Deck_Information_submenu_Back` | 69×63 | 18,18,18,18 | Warlord 与 Campaign 的 Player Profile Container Base |
| `WF_UI_Ranked_Background_{Gold,Silver,Bronze}` | 352×116 | 20,20,20,20 | 三个 Player Profile Ranked Trophies * |

**ppu / ppuMul ≠ 默认（4 个节点）**：

| 节点 | sprite | 值 |
|---|---|---|
| `40k_popup_texture`（Background fill） | 128×128 | **Tiled + ppuMul=2.0** |
| Choose Name Input Field | `40K_dropdown_bg` | **ppuMul=1.2** |
| Generic UI Button（ChooseNameWindow） | `40K_button` | **ppuMul=0.01** |
| Legendary Display Profile/DivisionImage | `07-Legend 512×512` | **ppu=50**（不是 100） |

**关于「offset ≠ 0」**：**工具输出里没有 offset 这一列** ⇒ 按铁律 3 不猜，记入「查不到」。
可替代的近似信息：`preserveAspect` 开的节点与 `ppuMul` 已在上面。

**另外 3 处「尺寸反算出来是 0 或负」的节点**（不是 offset，是布局结果，**别照抄**）：
`DivisionImage` 高 = 0.00（390.92×0.00）· `RankImage` 高 = **−8.00** ·
`Highest Faction Rating`/`MainRating` 的各子件高 = 0.00 —— 这些父级就是工具标 `⚠️unk` 的那几个布局组。


## A.3 `ChooseNameWindow` —— 内嵌的**全屏改名窗**

它是 **`Profile Tab` 的直接子节点**（`m_Father` = `RectTransform_-8094654694055052750`），
但 **rect 比父还大**：父 1395.94×843.08，它 1920.25×1080.00、`anchoredPosition=(-89,0.459)`、
`sizeDelta=(524.318,236.916)`、锚点 (0,0)→(1,1) ⇒ **展开后正好盖满整屏**（x −0.13→1920.13，y 0→1080）。
它由 `UIAnchorAvoidSafeArea` + `ChangeNameWindow` 两个组件驱动（`extraScaleSmallScreen=1.2`、
`closeOnESC=1`、`windowsPlacement=0`），是 `GameWindow` 子类。
⇒ **页签里嵌着一个全屏窗口，不是子面板。**

它的子树（根 + 直接子节点 + 第二层，逐行抄自工具输出）：

| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 锚点 min→max | pivot | anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | 贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| ·1 | ChooseNameWindow | -0.13,0.00→1920.13,1080.00 | 1920.25×1080.00 | (0,0)→(1,1) | (0.5,0.5) | (-89,0.459015) | (524.318,236.916) | **F** | UIAnchorAvoidSafeArea,ChangeNameWindow |  |  |  | 字段: applyHeight,applyWidth,autoFindWidthAndHeightApplyOptions,rectTransform,tempPivotX,tempSizeDeltaX ; 字段: changeNameButton,closeButton,closeButtonBackground,closeOnESC,closeSound,extraScaleSmallScreen,freeTextInButton,nameInput,openSound,priceDisplayInButton,type,updateNavPanel,useDefaultCloseSoundIfNull,windowsPlacement |
| ··2 | Dark Background | -620.63,-213.74→2540.63,1563.74 | 3161.26×1777.49 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.65701e-05,-135) | (3161.26,1777.49) | T | Image,BackgroundCloseButton | <无图> | Simple (0,0,0,0.694) |  | 字段: onClick,window |
| ··2 | Generic Popup Background | 519.49,395.00→1400.51,696.03 | 881.02×301.03 | (0,0)→(1,1) | (0.5,0.5) | (0,-5.513) | (-1039.23,-778.974) | T | Image | 40k_popup 359×336 九宫169,160,169,160 | Sliced (1,1,1,1) |  |  |
| ···3 | Mask | 529.89,404.44→1390.64,686.22 | 860.75×281.78 | (0,0)→(1,1) | (0.5,0.5) | (0.262024,0.178986) | (-20.268,-19.245) | T | Image,Mask | 40k_popup 359×336 九宫169,160,169,160 | Sliced (1,1,1,1) |  | showGraphic=0 |
| ····4 | Background fill | 529.89,404.44→1390.64,686.22 | 860.75×281.78 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | 40k_popup_texture 128×128 否 | **Tiled ppuMul=2.0** |  |  |
| ··2 | Choose Name Input Field | 540.13,496.00→1376.29,556.00 | 836.16×60.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.794,14) | (836.16,60) | T | Image,EverguildInputField | 40K_dropdown_bg 119×102 九宫23,20,23,20 | Sliced (0.286,0.965,0.686,1) **ppuMul=1.2** |  | （`m_*` 输入框全套字段） |
| ···3 | Text Area | 550.13,503.00→1366.29,550.00 | 816.16×47.00 | (0,0)→(1,1) | (0.5,0.5) | (0,-0.5) | (-20,-13) | T | RectMask2D |  |  |  | 字段: m_Padding,m_Softness |
| ····4 | Placeholder | 550.13,503.00→1366.29,550.00 | 816.16×47.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI,LayoutElement |  |  | '' 字号=18.0 auto[18.0~40.0] 对齐=2/512 色=(0.22,0.22,0.22,0.5) | 字段: m_FlexibleHeight,… |
| ····4 | Text | 550.13,503.00→1366.29,550.00 | 816.16×47.00 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | TextMeshProUGUI |  |  | '\u200b' 字号=40.0 auto[18.0~40.0] 对齐=1/512 色=(1,1,1,1) |  |
| ··2 | MessageText | 540.13,426.87→1376.28,501.13 | 836.15×74.26 | (0,0.5)→(1,0.5) | (0.5,0.5) | (-1.79712,76) | (-1084.1,74.261) | T | TextMeshProUGUI,Localize |  |  | 'Choose your player name' 字号=40.0 auto[4.0~40.0] 对齐=2/4096 色=(1,1,1,1) | （Localize 全套） |
| ··2 | Change Name Button | 822.82,578.22→1097.18,645.78 | 274.36×67.56 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (6.10352e-05,-72) | (274.358,67.561) | T | PriceDisplayButton |  |  |  | 字段: blinkEffect,button,priceDisplay |
| ···3 | Generic UI Button | 822.82,578.22→1097.18,645.78 | 274.36×67.56 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButton,EverguildButtonMaterialModifier,UIEffect,UIEffectTweener | 40K_button 489×107 九宫234,46,234,46 | Simple (0.369,0.894,0.587,1) preserveAspect **ppuMul=0.01** |  | trans=2 target=7743085296280631858 interactable=1 |
| ····4 | Button Text | 835.82,587.84→1084.18,636.16 | 248.36×48.31 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,-19.2477) | T | TextMeshProUGUI,Localize,AspectRatioFitter,… |  |  | 'Free' 字号=50.0 auto[12.0~50.0] 对齐=2/4096 色=(1,1,1,1) |  |
| ····4 | Price Display | 833.20,586.77→1084.64,638.24 | 251.44×51.47 | (0.0378438,0.108477)→(0.954309,0.876549) | (0.5,0.5) | (0,0) | (0,-0.417889) | **F** | HorizontalLayoutGroup,LayoutGroupContentFixer,AspectRatioFitter,PriceDisplay |  |  |  | spacing=0 align=4 pad=0 ctrlW=0 ctrlH=1 expandW=0 expandH=1 |
| ·····5 | icon | 887.08,586.77→938.55,638.24 | 51.47×51.47 | (0,1)→(0,1) | (0.5,0.5) | (79.6097,-25.7369) | (51.4738,51.4738) | T | Image,… | <无图> | Simple (1,1,1,1) |  |  |
| ·····5 | text | 938.55,586.77→1030.77,638.24 | 92.22×51.47 | (0,1)→(0,1) | (0.5,0.5) | (151.457,-25.7369) | (92.22,51.4738) | T | TextMeshProUGUI,ContentSizeFitter,… |  |  | '300,00' 字号=40.0 auto[13.46~40.0] 对齐=2/8192 色=(1,1,1,1) |  |
| ··2 | Generic Close Button Green | 1358.30,362.50→1433.30,437.50 | 75.00×75.00 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (435.8,140) | (75,75) | T | Image,EverguildButton,EverguildButtonMaterialModifier | UI_Button_Round_background 237×237 否 | Simple (1,1,1,1) preserveAspect |  | trans=2 target=-5517809430966076878 interactable=1 |
| ···3 | Icon | 1367.62,372.75→1423.98,427.25 | 56.37×54.50 | (0.126634,0.13672)→(0.873366,0.86328) | (0.5,0.5) | (0,0) | (0.364201,0.00779724) | T | Image,EverguildButtonMaterialModifier | 40k_bt_close 175×174 否 | Simple (1,1,1,1) |  |  |

---

# B. 入口 / 它调用的 / 谁监听它

## B.1 入口：谁构造 / 实例化 / 打开 `PlayerProfileMenu`

统一形态 = `new PlayerProfileMenuContext(id, ...)` → `WindowsManager.OpenWindow<PlayerProfileMenu>(ctx)`：

| # | 触发点 | 出处 |
|---|---|---|
| 1 | 聊天里点玩家头像选项「查看档案」 | `ChatPlayerOptionsPanel__ShowProfile.c:16-23` |
| 2 | 战斗日志里点玩家 | `BattleLogPlayerDisplay__OnPlayerClicked.c:21-38`（带可选回调 `Action`） |
| 3 | 联盟成员选项弹窗「查看档案」 | `AllianceMemberOptionsPopup__OnViewProfileClicked.c:16-30` |
| 4 | 好友列表条目点档案 | `FriendUIElementWarpforge__ClickProfile.c:16-24` |
| 5 | 排行榜行点档案 | `PlayerUIRankingRow__CheckProfile.c:16-25` |
| 6 | 聊天动作消息（点击昵称） | `ChatGlobalManager__ProcessActionMessage.c` |
| 7 | 联盟踢人后的链路 | `AlliancesManager.__c__DisplayClass38_0___KickMember_b__0.c` |
| 8 | 重启 session 时 | `SessionManager__RestartSession.c` |
| 9 | 自己（`OnComplete` 回调里转发） | `PlayerProfileMenu.__c__DisplayClass16_0___OnComplete_b__0.c` |

⚠️ **我们这条链上只有第 9 条那种「自己开」不适用**：我们是从**顶栏头像**直接开（原版头像上挂的是
`OpenWindowButton`），**上面 9 条都是「从别处看某个玩家的档案」** —— 那需要**有别的玩家**（服务器）。

**`ProfileTab` 各方法的调用者（谁调它）**：

| 方法 | 调用者 | 出处 |
|---|---|---|
| `OnOpen` | `WindowTabBase.TryOpenTab` 的虚调用（slot 0x1b8） | `WindowTabBase__TryOpenTab.c:12` ← `GameWindowWithTabs__ChangeTabCO.c:51` |
| `ToFocus` | `GameWindowWithTabs` 的虚调用（slot 0x228） | `GameWindowWithTabs__ChangeTabCO.c:53` |
| `Start` | Unity 引擎（MonoBehaviour 消息） | — |
| `Initialize` | 自身 `OnOpen` | `ProfileTab__OnOpen.c:6` |
| `OnChangePlayerNameButtonClick` | `PlayerInfoDisplay.OnChangePlayerNameButtonClick` 事件（`Start` 里挂） | `ProfileTab__Start.c:31-35` |
| `HandleInviteToAlliance` | **找不到调用点** | 见 §C |
| `<Initialize>b__0`（闭包） | `playerIdDisplay` 的 `EverguildButton.m_OnClick`（`+0x100`） | `ProfileTab__Initialize.c:51-56` |
| `<Initialize>b__1`（闭包） | `Enumerable.Any(成员表, predicate)` | `ProfileTab__Initialize.c:87-89`（体缺失，见 §C） |
| `<Start>b__9_0`（闭包） | `inviteToAllianceButton` 的 `m_OnClick` | `ProfileTab__Start.c:20-25` |
| `<HandleInviteToAlliance>b__13_0/1` | `AlliancesManager.InvitePlayer` / `CancelInvitation` 的回调 | `ProfileTab__HandleInviteToAlliance.c:39,52` |

**关键背面事实**：`ProfileTab` 子树里 `*m_On*` 事件字段共 **144 个**，
`m_PersistentCalls.m_Calls` 总数 = **0** ⇒ **prefab 里没有任何持久化点击绑定，
全部靠运行期 `UnityEvent.AddListener`**。

## B.2 `ProfileTab` 调用的外部方法（归类）

| 类别 | 具体（出处均在 `ProfileTab__*.c`） |
|---|---|
| **子页签组件** | `PlayerInfoDisplay.Initialize` / `add_onAvatarClick` / `add_OnChangePlayerNameButtonClick`；`ProfileRankingSection.Initialize`；`ProfileEventSection.Initialize` |
| **本地化 / 文本** | `I2.Loc.LocalizationManager.GetTranslation`，term 常量已解出：`PlayerProfile/PlayerIdDisplay` · `PlayerProfile/ConsecutiveLoginDays` · `SocialMenu/Alliances/CancelInvitation` · `SocialMenu/Alliances/InvitePlayer` · `MainMenu/GenericWait`；`String.Format`；`TMP.set_text`（走 `EverguildButton.text` 字段 **0x1A0**） |
| **联盟逻辑** | `AlliancesManager`：`get_Instance` / `get_IsInGroup` / `get_CurrentPlayerData` / `get_CurrentCachedGroupData` / `InvitePlayer` / `CancelInvitation` |
| **Linq / 配置** | `Enumerable.Any<GroupMember>` · `Group.get_MemberCount` · `ConfigManager.GetConfig<T>()`（成员上限在 `+0x10`） |
| **窗口管理** | `WindowsManager.OpenWindow(changeNameWindow)` · `WindowsManager.ShowPopUp(instance, 'MainMenu/GenericWait')` · `WindowsManager.HidePopUp` |
| **杂项** | `GUIUtility.set_systemCopyBuffer(PlayfabId)` + `UIMessageController.ShowMessage`（复制 Player ID） · `GameWindowWithTabs.ChangeTab<T>()` · `GameObject.SetActive` |

## B.3 谁监听它 / 它监听谁

**它监听谁**：① `inviteToAllianceButton.m_OnClick`；② `playerIdDisplay.m_OnClick`；
③ `PlayerInfoDisplay.onAvatarClick` 与 `PlayerInfoDisplay.OnChangePlayerNameButtonClick`；
④ 再上传一层：`PlayerInfoDisplay.Initialize` 把自己 `Combine` 到 `ProfileNameTitleSection.OnChangePlayerNameButtonClick`，
而 `ProfileNameTitleSection.Awake` 把它的 `changeNameButton`(0x38) 的 `m_OnClick` 接到自己的回调
⇒ **完整点击链**：

```
Edit Name Button.OnClick
  → ProfileNameTitleSection.OnChangePlayerNameButtonClick
  → PlayerInfoDisplay.OnChangePlayerNameButtonClick
  → ProfileTab.OnChangePlayerNameButtonClick
  → WindowsManager.OpenWindow(ChooseNameWindow)
```

**它触发什么 / 写什么共享状态**：

- 改**别人的** `PlayerInfo.HasBeenInvited`（`PlayerInfo` 字段 **0xA8**）：`HandleInviteToAlliance` 的 Bot 支直接写，
  两个回调也写。这是**对 `PlayerProfileMenu.PlayerInfo`（窗口字段 0xA0）的原地修改**，
  会被 `Initialize` 与 `HandleInviteToAlliance` 再读。
- 触发 `GameWindowWithTabs.ChangeTab<T>()`（条件 `PlayerInfo.IsPlayer(0x10)==true`）；
  **具体切到哪个页签没解出**（见 §C）。
- **无 `Update` / 无协程**：`ProfileTab` 只有 `OnOpen/Start/ToFocus/Initialize/OnChangePlayerNameButtonClick/
  HandleInviteToAlliance` + 3 个闭包 ⇒ **不 Poll 任何人**；状态刷新全靠 `OnOpen`/`ToFocus` 时重新 `Initialize`。

---

# C. 查不到的（**不猜**）

1. **`HandleInviteToAlliance` 的调用点** —— 搜词 `HandleInviteToAlliance`；目录 `d:/2/tools/decomp_full/`
   （25096 个 `.c`）—— 全目录只有 3 个命中，**全是它自己和它的两个闭包**。
   **判据不足以断言「没人调」**：本类已确证存在**产物错配/丢失**（见 2/3/4），
   且该按钮 prefab `m_OnClick` 的 `m_Calls` 为空 ⇒ 它必然靠某处运行期 `AddListener` 接入，
   而那一处的产物没落盘。**结论：未知，不是「零调用」。**
2. **`ProfileTab.<>c__DisplayClass11_0.<Initialize>b__1(GroupMember)` 的方法体** —— `dump.cs:83441`
   证明确有这个成员；但 `decomp_full/ProfileTab.__c__DisplayClass11_0___Initialize_b__1.c` 的内容
   **是另一个类的方法** ⇒ **产物错配**。只能从调用点形状（返回 bool、喂给 `Enumerable.Any`、比较字符串）
   推语义，**不写进结论**。
3. **`ProfileTab.<Start>b__9_1` / `<Start>b__9_2`** —— `Start` 里构造了 3 个委托，
   但 `dump.cs:83485-83496` 的方法表里**只有 `<Start>b__9_0`**，`decomp_full/` 里也只有它
   ⇒ 另两个闭包**不存在或未落盘**。因此「`onAvatarClick` 与 `OnChangePlayerNameButtonClick`
   分别接到哪个回调体」**查不到**。
4. **`ProfileNameTitleSection.<Awake>b__9_0` 的方法体** —— 文件名存在，内容却是
   `PlayerInfoDisplay.<Initialize>b__11_0` ⇒ **产物错配**。「Edit Name Button 点击后做什么」只能靠链条推断。
5. **`<HandleInviteToAlliance>b__13_0` / `b__13_1` 谁对应 Invite / 谁对应 Cancel** ——
   反编译里传入的 token 与文件名对不上语义；若信文件名，则「InvitePlayer 成功后 `HasBeenInvited=0`
   且文字变 'InvitePlayer'」，与 `Initialize` 的规则**矛盾**。两种可能（token 命名不可靠 / 两个 `.c`
   内容互换）**无法用现有材料分辨** ⇒ **只写规则、不写回调归属**。
6. **`Start` 里 `ChangeTab<T>()` 的目标页签类型** —— `DAT_1842d1918` 是 il2cpp 元数据指针，
   不是字符串字面量；`stringliteral.json` 查不到，`all_methods.txt` 只覆盖方法地址。**未解出**。
7. **`ConsecutiveLoginDays` 的 `String.Format` 参数** —— `Initialize` 显示参数与 `PlayerIdDisplay`
   那条取的是**同一个表达式**（`PlayerInfo + 0x20` = `PlayfabId`）。但 `PlayerInfo` 里
   **没有任何「连续登录天数」字段**，语义不通 ⇒ 疑为反编译寄存器复用/漏读。**未知**。
8. **Image / sprite 的 `offset ≠ 0`** —— 工具输出**没有 offset 这一列** ⇒ **不填**。
9. **小屏（`extraScaleSmallScreen`）下的第二套坐标** —— 只查出窗口根的倍率
   （`PlayerProfileMenu`=1.075、`ChangeNameWindow`=1.2），**页签内部的第二套 rect 没算**。
10. **`Avatar Name` / `Consecutive login days` / `Highest Faction Rating`(×2) / `counter`(×2) /
    `LeaderboardButton`(×2) / `Timer`(×2) 的激活点** —— 搜词：`SetActive`、`set_enabled`、`Transform.Find`、
    `GetChild`、`GetComponentsInChildren`；目录：`decomp_full/` 里那一批类 + **全包组件反向引用扫描**
    （`assets_full/bundle_menus_assets_all/{MonoBehaviour,RectTransform,CanvasRenderer,…}` 全 JSON 的
    `m_PathID` 反向索引）。**结果：这些节点只被 `m_Children`/`m_Father` 引用，
    没有任何 MonoBehaviour 字段指向它们，且上述类里没有按名 `Find` 的写法**
    ⇒ 给的是「**查不到激活点**」，**不是「确定永远 inactive」**。

**同名多实例怎么确认没取错**：

- **`Profile Tab`**：`find_go_by_name('Profile Tab')` 只返回 **1 个** pid `7232280650380376626` ⇒ 本包无重名。
- **`ChooseNameWindow`**：全包 **2 个** —— 本页那个（`m_Father = Profile Tab`）与
  `6641973667836381293`（`m_Father = 0`，独立根 RT，与本窗无关）。
- **`Edit Name Button`** 全包 **6 个**（本页内 2 个）；**`playerIdText`** 全包 **2 个，都在本页内**。
- **`MonoScript` 明文的 `m_ClassName` 有两个来源**（`bundle_Waprforge_monoscripts` 与 `globalgamemanagers`），
  同一类名各出现一次 ⇒ 为避免取错，用的是**组件里的 `m_Script.m_PathID` 直接反查**，不靠名字。
