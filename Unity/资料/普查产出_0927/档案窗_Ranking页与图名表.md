# ⚠️ 交付说明（先看这段）

> 🔴 **2026-09-27 落盘时改过两处（主对话做，不是子代理做的）**：
> ① **文件已落盘**（子代理是只读的、写不了文件），内容除下表两处外**一字未动**；
> ② **§A 那张表【用修好的工具重新生成过】** —— 子代理报了一条**真缺陷**（见下面第 3 条），
>    主对话当场修进 `工具/menu_dump.py`（`apply_layout_to_children` 先滤掉 `m_IsActive=0` 的子节点），
>    然后**重跑同一命令**换来新表（181 行 → 179 行）。
>    ⇒ **本文的表已经是修过之后的值**；子代理在 §A·4 里点名的那几个「表值偏了」的布局组，
>    **现在表里是对的**，但 §A·4 的**说明文字仍然成立**（它讲的是原理与受影响范围）。
>    自检 `python 工具/menu_dump.py --verify-layout` 仍逐位绿（`Tab Buttons` 六个键全 active，本来就抓不到这条 —— 所以那条自检**对这条免疫**）。

我（本次子代理）是**只读**的：工具集里没有 Write/Edit，且被明令禁止创建任何文件（含 `/tmp`）。
⇒ **产出文件我没能落盘**。下面是**完整文件正文**，请**原样**保存到
`d:/4/Unity/资料/普查产出_0927/档案窗_Ranking页与图名表.md`（该目录已存在，里面有 3 个同批产物）。

- 全文 **438 行**（≤450 上限）。
- 正文里第 3–183 行是 `工具/menu_dump.py` 的**逐行照抄**（未删行、未概括），可直接粘贴。
- 🔴 **一个必须先知道的环境事实**：`工具/menu_dump.py` 在 **2026-09-27 07:51 被另一个并行子代理改过**（`git status` = ` M Unity/工具/menu_dump.py`）。07:38 那版**不报 `m_Enabled=0`**，且会把 `m_Enabled=0` 的布局件照跑。**本文用的是 07:51 新版的输出**，所以表里会多出 `**m_Enabled=0**` 标记、对齐列是 `Center/Middle` 这种名字而不是 `2/512`。
- 顺手发现的**工具级缺陷**（不是本页特有，已写进 §A·4）：`工具/menu_dump.py:428` 取布局组子节点时**没有按 `activeSelf` 过滤**，而 Unity 的 `LayoutGroup.rectChildren` 只收 `activeInHierarchy==true` 的子节点 ⇒ **本页凡是有 inactive 子节点的布局组，表值都偏**（本页有 3 类，共 11 个布局组受影响）。这条会影响**全部**档案窗普查产出，建议在正本里记一笔。
  ✅ **2026-09-27 已修**（见上面那条横幅）。**这条自检抓不到**（`Tab Buttons` 六个键全 active）
  ⇒ **自检绿 ≠ 这条对** —— 记进 `项目任务.md` §四 `menu_dump.py` 那一行。

---

# 档案窗 `Ranking` 页（`RankedTab`）+ §2·1 十二图 PathID→图名 — 只读普查

> 产出：2026-09-27 只读子代理。**未改任何工程代码。**
> 工具：`工具/menu_dump.py`（**2026-09-27 07:51 版本**）。自检 `python 工具/menu_dump.py --verify-layout` 通过（§A·0）。
> 命令：`python 工具/menu_dump.py bundle_menus_assets_all --rt -1610774836786529742 --depth 8 --md`
> 边界：`菜单全树.md:16352–16485`（134 行）。⚠️ 那份的坐标是**模板位**；本文所有坐标是**回原始 JSON 的布局后真值**。
> ⚠️ `:16352` 的 `Ranking Tab`（本页内容根，RT pid `-1610774836786529742`）与 `:15944` 的 `Ranking`（Profile 页里的排行块，类 `ProfileRankingSection`）是**两个不同节点** —— 后者见 `档案窗_Profile页.md`。

---

## §A 「层 × 参数」表（工具输出，逐行照抄，未删行未概括）

```
# MonoScript 类名索引：5175 条
# （已沿 `m_Father` 爬父链：被查节点的父 = 「Tab Content」 351.03,118.92 → 1746.97,962.00）
# sprite 索引：真包 3316 条 + 切片缓存补齐 ⇒ 共 3316 条
# sprite 尺寸/九宫格索引：3200 条
| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 锚点 min→max | pivot | anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | 贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | Ranking Tab | 351.03,118.92→1746.97,962.00 | 1395.94×843.08 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | **F** | RankedTab |  |  |  | 字段: divisionImage,factionListHolder,factionScorePrefab,globalRating,infoSection,top4Factions |
| ·1 | Profile Player Info | 351.03,168.16→1186.98,320.54 | 835.95×152.39 | (0,0.5)→(0,0.5) | (0,0.5) | (0,296.107) | (835.952,152.385) | T | PlayerInfoDisplay |  |  |  | 字段: avatarDisplay,nameTitleSection,nameTitleSectionWithAlliances,playerLevel |
| ··2 | Avatar Item Small | 351.03,168.16→510.19,320.54 | 159.15×152.39 | (0,0)→(0,1) | (0,0.5) | (0,-3.05176e-05) | (159.154,0) | T | EverguildButton,AvatarDisplay,ItemDrawerComponents |  |  |  | trans=1 target=6879724527869852210 interactable=1 ; 字段: avatarHolder,avatarImage,avatarName,button,highlight,inspectSound,showItemInfoOnClick ; 字段: background,claimedWarning,conversionDisplay,convertedItem,convertedLabel,ephemeralDisplay,ephemeralText,image,label,premiumBadge,premiumHighlight,quantity |
| ···3 | Raycast Target | 341.16,128.01→520.06,342.45 | 178.90×214.44 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-9.15527e-05,9.1215) | (178.904,214.439) | T | Image,EverguildButtonMaterialModifier | <无图> | Simple (1,1,1,0) |  | 字段:  |
| ···3 | Image Container | 351.03,168.16→510.19,283.18 | 159.15×115.02 | (0,0)→(1,1) | (0.5,0.5) | (0,18.6839) | (0,-37.3678) | T |  |  |  |  |  |
| ····4 | Highlight | 351.03,164.63→513.52,281.50 | 162.49×116.87 | (0,0)→(1,1) | (0.5,0.5) | (1.6692,2.6) | (3.3384,1.8547) | T | Image,EverguildButtonMaterialModifier | Player_Avatar_selected 256×256 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ····4 | Border | 351.03,179.66→510.19,294.68 | 159.15×115.02 | (0,-0.1)→(1,0.9) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier | Player Profile Border 256×286 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ····4 | Image | 351.03,165.46→510.19,280.48 | 159.15×115.02 | (0,0)→(1,1) | (0.5,0.5) | (0,2.7) | (0,0) | T | Image,EverguildButtonMaterialModifier | <无图> | Simple (1,1,1,1) preserveAspect **m_Enabled=0** |  | 字段:  |
| ···3 | Avatar Name | 351.03,320.54→510.19,361.86 | 159.15×41.31 | (0,0)→(1,0) | (0.5,1) | (0,0) | (7.62939e-06,41.3144) | **F** | TextMeshProUGUI,EverguildButtonMaterialModifier |  |  | 'Avatar name' 字号=36.0 auto[12.0~36.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段:  |
| ··2 | Info Section with Alliance | 510.18,168.16→1405.87,320.54 | 895.69×152.38 | (0,0.5)→(0,0.5) | (0,0.5) | (159.15,0.002677) | (895.69,152.38) | **F** | ProfileNameTitleSection |  |  |  | 字段: allianceDisplay,changeNameButton,playerLevelText,playerName,playerTitle |
| ···3 | Name and Title Holder | 510.19,168.16→1374.52,217.99 | 864.34×49.83 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-15.6721,51.276) | (864.337,49.828) | T | HorizontalLayoutGroup ⚠️unk |  |  |  | spacing=5.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 |
| ····4 | Edit Name Button | 510.19,168.16→563.29,217.99 | 53.10×49.83 | (0,1)→(0,1) | (0.5,0.5) | (26.55,-24.914) | (53.1,49.828) | **F** | Image,EverguildButton,EverguildButtonMaterialModifier,LayoutElement | 40k_menu_bt_general_bg 51×52 九宫15,15,15,15 | Sliced (0.212,0.0941,0.098,1) **m_Enabled=0** |  | trans=1 target=2434526262644144690 interactable=1 ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Button Outline | 510.69,168.16→562.79,217.99 | 52.10×49.83 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-1,0) | T | Image,EverguildButtonMaterialModifier | 40k_menu_bt__general_outline 51×52 九宫15,15,15,15 | Sliced (0.945,0.842,0.0314,1) fillCenter=0 |  | 字段:  |
| ·····5 | Icon | 510.19,168.16→563.29,217.99 | 53.10×49.83 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier | 40k_general_bt_yellow_edit 71×71 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ····4 | Player Name | 510.19,168.16→510.19,217.99 | 0.00×49.83 | (0,1)→(0,1) | (0.5,0.5) | (0,-24.914) | (0,49.828) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,LayoutElement |  |  | 'Player Name' 字号=40.70000076293945 auto[23.0~45.0] 对齐=Left/Middle 折行=0 色=(0.98,0.686,0.169,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | Player Title | 515.19,168.16→515.19,217.99 | 0.00×49.83 | (0,1)→(0,1) | (0.5,0.5) | (5,-24.914) | (0,49.828) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,LayoutElement |  |  | 'Player Title' 字号=35.0 auto[20.0~35.0] 对齐=Left/Middle 折行=0 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ···3 | Alliance Info | 510.19,220.54→1388.84,286.53 | 878.66×65.99 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-8.51181,-9.1854) | (878.657,65.9909) | T | ProfileAllianceDisplay |  |  |  | 字段: allianceName,playerRating |
| ····4 | Alliance Name | 510.19,212.13→1388.84,251.06 | 878.66×38.93 | (0,0)→(1,0.5) | (0,0.5) | (0,38.4368) | (0,5.9326) | T | EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,EverguildTextMeshPro |  |  | 'Alliance Name' 字号=31.799999237060547 auto[23.0~37.400001525878906] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ····4 | Alliance Rating Display | 510.19,251.06→774.12,286.53 | 263.93×35.47 | (0,0.5)→(0,0.5) | (0,0.5) | (0,-15.26) | (263.929,35.471) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·····5 | Secondary Icon | 510.19,251.06→554.59,310.23 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Main Icon | 510.19,246.29→570.19,291.29 | 60.00×45.00 | (0,1)→(0,1) | (0.5,0.5) | (30,-17.7355) | (60,45) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | Individual rating value | 570.19,251.06→774.12,286.53 | 203.93×35.47 | (0,1)→(0,1) | (0.5,0.5) | (161.964,-17.7355) | (203.929,35.471) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '------' 字号=37.400001525878906 auto[18.0~40.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ··2 | Info Section without Alliance | 510.18,168.16→1405.87,320.54 | 895.69×152.38 | (0,0.5)→(0,0.5) | (0,0.5) | (159.15,0.002677) | (895.69,152.38) | T | ProfileNameTitleSection |  |  |  | 字段: allianceDisplay,changeNameButton,playerLevelText,playerName,playerTitle |
| ···3 | Name and Title Holder | 510.19,168.16→1374.52,217.99 | 864.34×49.83 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-15.6721,51.276) | (864.337,49.828) | T | VerticalLayoutGroup |  |  |  | spacing=0.0 align=0 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=1 expandH=1 scaleW=0 scaleH=0 |
| ····4 | NameHolder | 510.19,168.16→1127.11,217.99 | 616.92×49.83 | (0,1)→(0,1) | (0.5,0.5) | (308.46,-24.914) | (616.92,49.828) | T |  |  |  |  |  |
| ·····5 | Edit Name Button | 510.19,168.16→563.29,217.99 | 53.10×49.83 | (0,1)→(0,1) | (0.5,0.5) | (26.55,-24.914) | (53.1,49.828) | **F** | Image,EverguildButton,EverguildButtonMaterialModifier,LayoutElement | 40k_menu_bt_general_bg 51×52 九宫15,15,15,15 | Sliced (0.212,0.0941,0.098,1) **m_Enabled=0** |  | trans=1 target=-2808983208499250638 interactable=1 ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Button Outline | 510.69,168.16→562.79,217.99 | 52.10×49.83 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-1,0) | T | Image,EverguildButtonMaterialModifier | 40k_menu_bt__general_outline 51×52 九宫15,15,15,15 | Sliced (0.945,0.842,0.0314,1) fillCenter=0 |  | 字段:  |
| ······6 | Icon | 510.19,168.16→563.29,217.99 | 53.10×49.83 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier | 40k_general_bt_yellow_edit 71×71 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ·····5 | Player Name | 510.19,169.45→752.61,216.69 | 242.42×47.24 | (0,1)→(0,1) | (0.5,0.5) | (121.21,-24.914) | (242.42,47.242) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,LayoutElement |  |  | 'Player Name' 字号=38.599998474121094 auto[23.0~45.0] 对齐=Left/Midline 折行=0 色=(0.98,0.686,0.169,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | Player Title | 510.19,217.99→1127.10,267.81 | 616.92×49.83 | (0,1)→(0,1) | (0.5,0.5) | (308.458,-74.742) | (616.917,49.828) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildButtonMaterialModifier,EverguildTextController,LayoutElement |  |  | 'Warrior of the raging winds' 字号=35.0 auto[20.0~35.0] 对齐=Left/Middle 折行=0 色=(1,1,1,1) | 字段:  ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ··2 | Player Level | 453.15,259.19→506.27,312.31 | 53.12×53.12 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-289.3,-41.4) | (53.12,53.12) | T | Image | UI_Button_Round_background 237×237 否 | Simple (1,1,1,1) |  |  |
| ···3 | Player Level Text | 460.01,266.05→499.41,305.45 | 39.40×39.40 | (0,0)→(1,1) | (0.5,0.5) | (-3.05176e-05,0) | (-13.722,-13.722) | T | EverguildTextMeshPro |  |  | '-' 字号=37.20000076293945 auto[18.0~37.20000076293945] 对齐=Center/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial |
| ·1 | Top4 | 351.03,333.95→1186.99,886.97 | 835.95×553.02 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-279.99,-70) | (835.95,553.016) | T |  |  |  |  |  |
| ··2 | bg | 351.03,333.95→1186.99,886.97 | 835.95×553.02 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  |  |
| ··2 | content | 367.75,345.01→1170.27,875.91 | 802.51×530.90 | (0.02,0.02)→(0.98,0.98) | (0.5,0.5) | (0,0) | (0,0) | T | HorizontalLayoutGroup |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 |
| ···3 | left-side | 374.36,345.01→584.36,875.91 | 210.00×530.90 | (0,1)→(0,1) | (0.5,0.5) | (111.607,-265.448) | (210,530.895) | T | VerticalLayoutGroup ⚠️unk |  |  |  | spacing=50.0 align=3 pad=10,10,10,10 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 |
| ····4 | #1 FactionScoreBig | 384.36,355.01→384.36,585.46 | 0.00×230.45 | (0,1)→(0,1) | (0.5,0.5) | (10,-125.224) | (0,230.448) | T | Image,VerticalLayoutGroup,FactionScore | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=1 expandH=0 scaleW=0 scaleH=0 ; 字段: factionImage,highestRatingDisplay,ratingDisplay |
| ·····5 | icon | 302.36,363.45→466.36,509.26 | 164.00×145.80 | (0,1)→(0,1) | (0.5,0.5) | (0,-81.343) | (164,145.802) | T | Image | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Alliance Rating Display | 302.36,509.26→466.36,550.26 | 164.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (0,-174.744) | (164,41) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 302.36,509.26→346.76,568.42 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 364.36,509.26→404.36,550.26 | 40.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (82,-20.5) | (40,41) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 404.36,509.26→404.36,550.26 | 0.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (102,-20.5) | (0,41) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '3000' 字号=40.0 auto[18.0~40.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | MaxRating | 302.36,550.26→466.36,577.02 | 164.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (0,-208.625) | (164,26.7615) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 302.36,550.26→346.76,609.42 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 364.36,550.26→404.36,577.02 | 40.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (82,-13.3808) | (40,26.7615) | T | Image,EverguildButtonMaterialModifier,LayoutElement | Menu_Icon_Galon 64×64 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 404.36,550.26→404.36,577.02 | 0.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (102,-13.3807) | (0,26.7615) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '3000' 字号=28.200000762939453 auto[18.0~40.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | #2 FactionScoreBig | 384.36,635.46→384.36,865.91 | 0.00×230.45 | (0,1)→(0,1) | (0.5,0.5) | (10,-405.671) | (0,230.448) | T | Image,VerticalLayoutGroup,FactionScore | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=1 expandH=0 scaleW=0 scaleH=0 ; 字段: factionImage,highestRatingDisplay,ratingDisplay |
| ·····5 | icon | 302.36,643.90→466.36,789.70 | 164.00×145.80 | (0,1)→(0,1) | (0.5,0.5) | (0,-81.343) | (164,145.802) | T | Image | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Alliance Rating Display | 302.36,789.70→466.36,830.70 | 164.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (0,-174.744) | (164,41) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 302.36,789.70→346.76,848.87 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 364.36,789.70→404.36,830.70 | 40.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (82,-20.5) | (40,41) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 404.36,789.70→404.36,830.70 | 0.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (102,-20.5) | (0,41) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '3000' 字号=40.0 auto[18.0~40.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | MaxRating | 302.36,830.70→466.36,857.46 | 164.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (0,-208.625) | (164,26.7615) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 302.36,830.70→346.76,889.87 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 364.36,830.70→404.36,857.46 | 40.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (82,-13.3808) | (40,26.7615) | T | Image,EverguildButtonMaterialModifier,LayoutElement | Menu_Icon_Galon 64×64 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 404.36,830.70→404.36,857.46 | 0.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (102,-13.3807) | (0,26.7615) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '3000' 字号=28.200000762939453 auto[18.0~40.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ···3 | center | 584.36,345.01→953.66,875.91 | 369.30×530.90 | (0,1)→(0,1) | (0.5,0.5) | (401.256,-265.448) | (369.298,530.895) | T | VerticalLayoutGroup ⚠️unk |  |  |  | spacing=0.0 align=4 pad=0,0,10,10 ctrlW=1 ctrlH=1 expandW=1 expandH=1 scaleW=0 scaleH=0 |
| ····4 | DivisionText | 584.36,355.01→953.66,599.34 | 369.30×244.33 | (0,1)→(0,1) | (0.5,0.5) | (184.649,-132.166) | (369.298,244.332) | T | TextMeshProUGUI,EverguildTextController,LayoutElement,Localize,Localize |  |  | 'Global Rating' 字号=38.0 auto[18.0~38.0] 对齐=Center/Midline 折行=0 色=(0.961,0.914,0.737,1) 字距=-2.6 | 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary |
| ····4 | DivisionImage | 584.36,599.34→953.66,732.62 | 369.30×133.28 | (0,1)→(0,1) | (0.5,0.5) | (184.649,-320.973) | (369.298,133.282) | T | Image,DivisionImageDisplay | 04-Admiral 512×512 否 ppu=50 | Simple (1,1,1,1) preserveAspect |  | 字段: divisionImage,rankImage |
| ·····5 | RankImage | 732.08,639.33→805.94,652.66 | 73.86×13.33 | (0.4,0.6)→(0.6,0.7) | (0.5,0.5) | (0,0) | (0,-2.38419e-07) | T | Image | Roman V 128×128 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ····4 | footer | 584.36,732.62→953.66,865.91 | 369.30×133.28 | (0,1)→(0,1) | (0.5,0.5) | (184.649,-454.254) | (369.298,133.282) | T | VerticalLayoutGroup,LayoutElement |  |  |  | spacing=11.199999809265137 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | MainRating | 591.29,769.10→946.73,829.43 | 355.44×60.33 | (0,1)→(0,1) | (0.5,0.5) | (184.649,-66.6408) | (355.439,60.3253) | T | Image,HorizontalLayoutGroup,LayoutElement ⚠️unk | 40K_main_rank_display 315×64 否 | Simple (1,1,1,1) **m_Enabled=0** |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=0 expandW=1 expandH=1 scaleW=0 scaleH=0 ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Global Rating | 591.29,770.59→946.73,827.94 | 355.44×57.36 | (0,1)→(0,1) | (0.5,0.5) | (177.719,-30.1626) | (355.439,57.356) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=0 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 591.29,827.94→591.29,827.94 | 0.00×0.00 | (0,0)→(0,0) | (0,0.5) | (0,0) | (0,0) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 591.29,799.27→649.89,799.27 | 58.60×0.00 | (0,1)→(0,1) | (0.5,0.5) | (29.3,-28.678) | (58.6,0) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 649.89,799.27→946.73,799.27 | 296.84×0.00 | (0,1)→(0,1) | (0,0.5) | (58.6,-28.678) | (296.839,0) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '32' 字号=40.0 auto[18.0~40.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ···3 | right-side | 953.66,345.01→1163.66,875.91 | 210.00×530.90 | (0,1)→(0,1) | (0.5,0.5) | (690.905,-265.448) | (210,530.895) | T | VerticalLayoutGroup ⚠️unk |  |  |  | spacing=50.0 align=0 pad=10,10,10,10 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 |
| ····4 | #3 FactionScoreBig | 963.66,355.01→963.66,585.46 | 0.00×230.45 | (0,1)→(0,1) | (0.5,0.5) | (10,-125.224) | (0,230.448) | T | Image,VerticalLayoutGroup,FactionScore | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=1 expandH=0 scaleW=0 scaleH=0 ; 字段: factionImage,highestRatingDisplay,ratingDisplay |
| ·····5 | icon | 881.66,363.45→1045.66,509.26 | 164.00×145.80 | (0,1)→(0,1) | (0.5,0.5) | (0,-81.343) | (164,145.802) | T | Image | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Alliance Rating Display | 881.66,509.26→1045.66,550.26 | 164.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (0,-174.744) | (164,41) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 881.66,509.26→926.06,568.42 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 943.66,509.26→983.66,550.26 | 40.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (82,-20.5) | (40,41) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 983.66,509.26→983.66,550.26 | 0.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (102,-20.5) | (0,41) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '------' 字号=40.0 auto[18.0~40.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | MaxRating | 881.66,550.26→1045.66,577.02 | 164.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (0,-208.625) | (164,26.7615) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 881.66,550.26→926.06,609.42 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 943.66,550.26→983.66,577.02 | 40.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (82,-13.3808) | (40,26.7615) | T | Image,EverguildButtonMaterialModifier,LayoutElement | Menu_Icon_Galon 64×64 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 983.66,550.26→983.66,577.02 | 0.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (102,-13.3807) | (0,26.7615) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '3000' 字号=28.200000762939453 auto[18.0~40.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ····4 | #4 FactionScoreBig | 963.66,635.46→963.66,865.91 | 0.00×230.45 | (0,1)→(0,1) | (0.5,0.5) | (10,-405.671) | (0,230.448) | T | Image,VerticalLayoutGroup,FactionScore | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=1 expandH=0 scaleW=0 scaleH=0 ; 字段: factionImage,highestRatingDisplay,ratingDisplay |
| ·····5 | icon | 881.66,643.90→1045.66,789.70 | 164.00×145.80 | (0,1)→(0,1) | (0.5,0.5) | (0,-81.343) | (164,145.802) | T | Image | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ·····5 | Alliance Rating Display | 881.66,789.70→1045.66,830.70 | 164.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (0,-174.744) | (164,41) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 881.66,789.70→926.06,848.87 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 943.66,789.70→983.66,830.70 | 40.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (82,-20.5) | (40,41) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 983.66,789.70→983.66,830.70 | 0.00×41.00 | (0,1)→(0,1) | (0.5,0.5) | (102,-20.5) | (0,41) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '3000' 字号=40.0 auto[18.0~40.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | MaxRating | 881.66,830.70→1045.66,857.46 | 164.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (0,-208.625) | (164,26.7615) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=4 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ······6 | Secondary Icon | 881.66,830.70→926.06,889.87 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Main Icon | 943.66,830.70→983.66,857.46 | 40.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (82,-13.3808) | (40,26.7615) | T | Image,EverguildButtonMaterialModifier,LayoutElement | Menu_Icon_Galon 64×64 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Individual rating value | 983.66,830.70→983.66,857.46 | 0.00×26.76 | (0,1)→(0,1) | (0.5,0.5) | (102,-13.3807) | (0,26.7615) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '3000' 字号=28.200000762939453 auto[18.0~40.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·1 | AllFactions | 1302.77,264.81→1746.97,886.97 | 444.20×622.15 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (475.87,-35.431) | (444.199,622.154) | T |  |  |  |  |  |
| ··2 | Faction Ranking Points | 1302.77,200.13→1672.07,260.13 | 369.30×60.00 | (0,1)→(0,1) | (0.5,0.5) | (184.65,34.6805) | (369.297,60) | T | TextMeshProUGUI,EverguildTextController,LayoutElement,Localize,Localize |  |  | 'Faction Rating' 字号=38.0 auto[18.0~38.0] 对齐=Left/Midline 折行=0 色=(0.961,0.914,0.737,1) 字距=-2.6 | 字段: maxFontSize,minFontSize ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary |
| ··2 | info | 1686.86,205.89→1737.87,254.37 | 51.01×48.48 | (1,0.5)→(1,0.5) | (1,0.5) | (-9.1,345.76) | (51.014,48.48) | T | Image,EverguildTooltipTrigger | 40K_generic_bt_info 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段: localize,offset,preventPassingClickEventToParent,registerEvents,text,title,tooltipAnchor,tooltipPrefab |
| ··2 | bg | 1302.77,264.81→1746.97,886.97 | 444.20×622.15 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (-6.10352e-05,0) | T | Image | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  |  |
| ··2 | scroll rect | 1305.29,288.62→1746.97,864.38 | 441.68×575.76 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (1.2595,-0.6085) | (441.68,575.762) | T | ScrollRect |  |  |  | h=0 v=1 mode=1 inertia=1 elasticity=0.10000000149011612 decel=0.13500000536441803 |
| ···3 | viewport | 1305.29,288.62→1746.97,864.38 | 441.68×575.76 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (0.0012178,-4.1008e-05) | (441.68,575.76) | T | Image,RectMask2D | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,0) |  | 字段: m_Padding,m_Softness |
| ····4 | content | 1305.29,288.62→1735.56,759.46 | 430.27×470.84 | (0.5,1)→(0.5,1) | (0.5,1) | (-5.7069,-0.000274658) | (430.27,470.844) | T | VerticalLayoutGroup,ContentSizeFitter |  |  |  | spacing=0.0 align=0 pad=0,0,0,0 ctrlW=0 ctrlH=0 expandW=1 expandH=0 scaleW=0 scaleH=0 ; 字段: m_HorizontalFit,m_VerticalFit |
| ·····5 | FactionScoreSmall | 1305.29,288.62→1744.03,406.33 | 438.74×117.71 | (0,1)→(0,1) | (0.5,0.5) | (219.37,-58.8554) | (438.74,117.711) | T | Image,FactionScore | Background 32×32 九宫10,10,10,10 ppu=200 | Sliced (0.00943,0.000934,0.00187,0.098) |  | 字段: factionImage,highestRatingDisplay,ratingDisplay |
| ······6 | icon | 1305.29,277.83→1448.39,406.33 | 143.10×128.50 | (0,1)→(0,1) | (0.5,0.5) | (71.5529,-53.461) | (143.098,128.5) | T | Image | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ······6 | Alliance Rating Display | 1295.14,273.32→1744.03,384.23 | 448.89×110.91 | (1,0.5)→(1,0.5) | (1,0.5) | (0,18.7) | (448.89,110.91) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 1295.14,273.32→1339.54,332.49 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 1295.14,273.32→1446.96,384.23 | 151.82×110.91 | (0,1)→(0,1) | (0.5,0.5) | (75.91,-55.455) | (151.82,110.91) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 1446.96,273.32→1744.03,384.23 | 297.07×110.91 | (0,1)→(0,1) | (0.5,0.5) | (300.355,-55.455) | (297.07,110.91) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '4879' 字号=90.0 auto[18.0~90.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Alliance Rating Display (1) | 1295.14,339.11→1744.03,411.44 | 448.89×72.34 | (1,0.5)→(1,0.5) | (1,0.5) | (0,-27.8) | (448.89,72.3365) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 1295.14,339.11→1339.54,398.27 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 1295.14,339.11→1446.96,411.44 | 151.82×72.34 | (0,1)→(0,1) | (0.5,0.5) | (75.91,-36.1683) | (151.82,72.3365) | T | Image,EverguildButtonMaterialModifier,LayoutElement | Menu_Icon_Galon 64×64 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 1446.96,339.11→1744.03,411.44 | 297.07×72.34 | (0,1)→(0,1) | (0.5,0.5) | (300.355,-36.1683) | (297.07,72.3365) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '5000' 字号=76.3499984741211 auto[18.0~90.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | FactionScoreSmall (1) | 1305.29,406.33→1744.03,524.04 | 438.74×117.71 | (0,1)→(0,1) | (0.5,0.5) | (219.37,-176.566) | (438.74,117.711) | T | Image,FactionScore | Background 32×32 九宫10,10,10,10 ppu=200 | Sliced (0.00943,0.000934,0.00187,0.098) |  | 字段: factionImage,highestRatingDisplay,ratingDisplay |
| ······6 | icon | 1305.29,395.54→1448.39,524.04 | 143.10×128.50 | (0,1)→(0,1) | (0.5,0.5) | (71.5529,-53.461) | (143.098,128.5) | T | Image | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ······6 | Alliance Rating Display | 1295.14,391.03→1744.03,501.94 | 448.89×110.91 | (1,0.5)→(1,0.5) | (1,0.5) | (0,18.7) | (448.89,110.91) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 1295.14,391.03→1339.54,450.20 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 1295.14,391.03→1446.96,501.94 | 151.82×110.91 | (0,1)→(0,1) | (0.5,0.5) | (75.91,-55.455) | (151.82,110.91) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 1446.96,391.03→1744.03,501.94 | 297.07×110.91 | (0,1)→(0,1) | (0.5,0.5) | (300.355,-55.455) | (297.07,110.91) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '4879' 字号=90.0 auto[18.0~90.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Alliance Rating Display (1) | 1295.14,456.82→1744.03,529.15 | 448.89×72.34 | (1,0.5)→(1,0.5) | (1,0.5) | (0,-27.8) | (448.89,72.3365) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 1295.14,456.82→1339.54,515.98 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 1295.14,456.82→1446.96,529.15 | 151.82×72.34 | (0,1)→(0,1) | (0.5,0.5) | (75.91,-36.1683) | (151.82,72.3365) | T | Image,EverguildButtonMaterialModifier,LayoutElement | Menu_Icon_Galon 64×64 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 1446.96,456.82→1744.03,529.15 | 297.07×72.34 | (0,1)→(0,1) | (0.5,0.5) | (300.355,-36.1683) | (297.07,72.3365) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '5000' 字号=76.3499984741211 auto[18.0~90.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | FactionScoreSmall (2) | 1305.29,524.04→1744.03,641.75 | 438.74×117.71 | (0,1)→(0,1) | (0.5,0.5) | (219.37,-294.277) | (438.74,117.711) | T | Image,FactionScore | Background 32×32 九宫10,10,10,10 ppu=200 | Sliced (0.00943,0.000934,0.00187,0.098) |  | 字段: factionImage,highestRatingDisplay,ratingDisplay |
| ······6 | icon | 1305.29,513.25→1448.39,641.75 | 143.10×128.50 | (0,1)→(0,1) | (0.5,0.5) | (71.5529,-53.461) | (143.098,128.5) | T | Image | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ······6 | Alliance Rating Display | 1295.14,508.74→1744.03,619.65 | 448.89×110.91 | (1,0.5)→(1,0.5) | (1,0.5) | (0,18.7) | (448.89,110.91) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 1295.14,508.74→1339.54,567.91 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 1295.14,508.74→1446.96,619.65 | 151.82×110.91 | (0,1)→(0,1) | (0.5,0.5) | (75.91,-55.455) | (151.82,110.91) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 1446.96,508.74→1744.03,619.65 | 297.07×110.91 | (0,1)→(0,1) | (0.5,0.5) | (300.355,-55.455) | (297.07,110.91) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '4879' 字号=90.0 auto[18.0~90.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Alliance Rating Display (1) | 1295.14,574.53→1744.03,646.86 | 448.89×72.34 | (1,0.5)→(1,0.5) | (1,0.5) | (0,-27.8) | (448.89,72.3365) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 1295.14,574.53→1339.54,633.69 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 1295.14,574.53→1446.96,646.86 | 151.82×72.34 | (0,1)→(0,1) | (0.5,0.5) | (75.91,-36.1683) | (151.82,72.3365) | T | Image,EverguildButtonMaterialModifier,LayoutElement | Menu_Icon_Galon 64×64 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 1446.96,574.53→1744.03,646.86 | 297.07×72.34 | (0,1)→(0,1) | (0.5,0.5) | (300.355,-36.1683) | (297.07,72.3365) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '5000' 字号=76.3499984741211 auto[18.0~90.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·····5 | FactionScoreSmall (3) | 1305.29,641.75→1744.03,759.46 | 438.74×117.71 | (0,1)→(0,1) | (0.5,0.5) | (219.37,-411.988) | (438.74,117.711) | T | Image,FactionScore | Background 32×32 九宫10,10,10,10 ppu=200 | Sliced (0.00943,0.000934,0.00187,0.098) |  | 字段: factionImage,highestRatingDisplay,ratingDisplay |
| ······6 | icon | 1305.29,630.96→1448.39,759.46 | 143.10×128.50 | (0,1)→(0,1) | (0.5,0.5) | (71.5529,-53.461) | (143.098,128.5) | T | Image | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) preserveAspect |  |  |
| ······6 | Alliance Rating Display | 1295.14,626.45→1744.03,737.36 | 448.89×110.91 | (1,0.5)→(1,0.5) | (1,0.5) | (0,18.7) | (448.89,110.91) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 1295.14,626.45→1339.54,685.62 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 1295.14,626.45→1446.96,737.36 | 151.82×110.91 | (0,1)→(0,1) | (0.5,0.5) | (75.91,-55.455) | (151.82,110.91) | T | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 1446.96,626.45→1744.03,737.36 | 297.07×110.91 | (0,1)→(0,1) | (0.5,0.5) | (300.355,-55.455) | (297.07,110.91) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '4879' 字号=90.0 auto[18.0~90.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ······6 | Alliance Rating Display (1) | 1295.14,692.24→1744.03,764.58 | 448.89×72.34 | (1,0.5)→(1,0.5) | (1,0.5) | (0,-27.8) | (448.89,72.3365) | T | HorizontalLayoutGroup,AllianceRatingDisplay |  |  |  | spacing=0.0 align=3 pad=0,0,0,0 ctrlW=1 ctrlH=1 expandW=0 expandH=1 scaleW=0 scaleH=0 ; 字段: ratingIcon,ratingText |
| ·······7 | Secondary Icon | 1295.14,692.24→1339.54,751.41 | 44.40×59.17 | (0,1)→(0,1) | (0,0.5) | (0,-29.5835) | (44.4,59.167) | **F** | Image,EverguildButtonMaterialModifier,LayoutElement | 40k_UI_icon_ranked_Skirmish 128×128 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Main Icon | 1295.14,692.24→1446.96,764.58 | 151.82×72.34 | (0,1)→(0,1) | (0.5,0.5) | (75.91,-36.1683) | (151.82,72.3365) | T | Image,EverguildButtonMaterialModifier,LayoutElement | Menu_Icon_Galon 64×64 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |
| ·······7 | Individual rating value | 1446.96,692.24→1744.03,764.58 | 297.07×72.34 | (0,1)→(0,1) | (0.5,0.5) | (300.355,-36.1683) | (297.07,72.3365) | T | EverguildTextMeshPro,EverguildButtonMaterialModifier,LayoutElement |  |  | '5000' 字号=76.3499984741211 auto[18.0~90.0] 对齐=Left/Midline 折行=1 色=(1,1,1,1) | 字段: checkPaddingRequired,fontPreset,m_ActiveFontFeatures,m_EmojiFallbackSupport,m_HorizontalAlignment,m_IsTextObjectScaleStatic,m_StyleSheet,m_TextStyleHashCode,m_TextWrappingMode,m_VertexBufferAutoSizeReduction,m_VerticalAlignment,m_baseMaterial,m_charWidthMaxAdj,m_characterSpacing,m_colorMode,m_enableAutoSizing,m_enableExtraPadding,m_enableKerning,m_enableVertexGradient,m_faceColor,m_fontAsset,m_fontColor,m_fontColor32,m_fontColorGradient,m_fontColorGradientPreset,m_fontMaterial ; 字段:  ; 字段: m_FlexibleHeight,m_FlexibleWidth,m_IgnoreLayout,m_LayoutPriority,m_MinHeight,m_MinWidth,m_PreferredHeight,m_PreferredWidth |

⚠️ 布局组（子节点位置**由布局算**，上面已是**布局跑之后**的值）：
          Name and Title Holder → HorizontalLayoutGroup [unk]
            Alliance Rating Display → HorizontalLayoutGroup [ok]
          Name and Title Holder → VerticalLayoutGroup [ok]
        content → HorizontalLayoutGroup [ok]
          left-side → VerticalLayoutGroup [unk]
            #1 FactionScoreBig → VerticalLayoutGroup [ok]
              Alliance Rating Display → HorizontalLayoutGroup [ok]
              MaxRating → HorizontalLayoutGroup [ok]
            #2 FactionScoreBig → VerticalLayoutGroup [ok]
              Alliance Rating Display → HorizontalLayoutGroup [ok]
              MaxRating → HorizontalLayoutGroup [ok]
          center → VerticalLayoutGroup [unk]
            footer → VerticalLayoutGroup [ok]
              MainRating → HorizontalLayoutGroup [unk]
                Global Rating → HorizontalLayoutGroup [ok]
          right-side → VerticalLayoutGroup [unk]
            #3 FactionScoreBig → VerticalLayoutGroup [ok]
              Alliance Rating Display → HorizontalLayoutGroup [ok]
              MaxRating → HorizontalLayoutGroup [ok]
            #4 FactionScoreBig → VerticalLayoutGroup [ok]
              Alliance Rating Display → HorizontalLayoutGroup [ok]
              MaxRating → HorizontalLayoutGroup [ok]
            content → VerticalLayoutGroup [ok]
                Alliance Rating Display → HorizontalLayoutGroup [ok]
                Alliance Rating Display (1) → HorizontalLayoutGroup [ok]
                Alliance Rating Display → HorizontalLayoutGroup [ok]
                Alliance Rating Display (1) → HorizontalLayoutGroup [ok]
                Alliance Rating Display → HorizontalLayoutGroup [ok]
                Alliance Rating Display (1) → HorizontalLayoutGroup [ok]
                Alliance Rating Display → HorizontalLayoutGroup [ok]
                Alliance Rating Display (1) → HorizontalLayoutGroup [ok]

🔴 **下面这些布局组的主轴尺寸算不准**（子节点里有文字/嵌套布局件/ScrollRect，首选尺寸要 Unity 的字体度量）—— 表里那几个子节点的值**别照抄**：
          Name and Title Holder
          left-side
          center
              MainRating
          right-side
```

### §A·0 工具自检（本页布局算法的可信度）

```
python 工具/menu_dump.py --verify-layout
  ✅ Tab Buttons 绝对矩形 95.48,180.24→273.48,885.76（要 95.48,180.24→273.48,885.75）
  ✅ Profile Button / Avatar Button / Title Button / Battle Log Button / Trophies / Ranked
     165.00×109.252，x 108.48→273.48，顶 180.24 / 299.50 / 418.75 / 538.00 / 657.25 / 776.50
✅ 布局算法与正本 §2·1 的手算值逐位一致
```
⇒ 本页所有**子节点全部 active、且布局件 `m_Enabled=1`** 的组，表值可信。**有例外，见 §A·4。**

---

## §A·补 工具给不了的那部分

> 下面每条都写「哪一行、补什么」。行号按上面表内「缩进+名字」定位。

### A·1 `activeSelf` 的出现条件（谁在运行期切）

| 表里哪一行 | 出厂 | 运行期谁切 | 出处 |
|---|---|---|---|
| `0 Ranking Tab` | **F** | `WindowTabBase.TryOpenTab()` → `GameObject.SetActive(true)`；`WindowTabBase.CloseTab()` → 关；`WindowTabBase.Toggle(bool)` → `SetActive(bool)` | `d:/2/tools/decomp_full/WindowTabBase__TryOpenTab.c:7-8`、`WindowTabBase__CloseTab.c`、`WindowTabBase__Toggle.c:9-11` |
| `··2 Info Section with Alliance` | **F** | `PlayerInfoDisplay.Initialize(PlayerInfo)` **二选一**：`allianceName`(+0xa0→+0x10) 非空 **且** `FeatureConfig.CheckFeature(...)` 为真 ⇒ 开这个 / 关 `without`；否则反过来 | `PlayerInfoDisplay__Initialize.c:87`、`:91`、`:119`、`:122` |
| `··2 Info Section without Alliance` | T | 同上 | 同上 |
| `···4 Edit Name Button`（×2 行） | **F** | `ProfileNameTitleSection.Initialize`：`GameObject.SetActive(PlayerInfo.<bool @0x10>)` —— 同一个 bool 也喂头像的 `Selectable.interactable` ⇒ 语义 = **是不是本人**（看别人档案时不显示改名键） | `ProfileNameTitleSection__Initialize.c:120`、`PlayerInfoDisplay__Initialize.c:66` |
| `···3 Avatar Name` | **F** | **查不到**（反编译里没有代码点它）⇒ 见 §D·2 | `grep -n SetActive AvatarDisplay__*.c` 只命中 `ToggleHighlight` |
| `·····5/6/7 Secondary Icon`（**全 18 处**） | **F** | **查不到**。`AllianceRatingDisplay.Initialize(value, overrideIcon, disableIfZero, useSealSystem, zeroBasedRank)` 只做两件事：① `disableIfZero && value==0` 时**把自己整个节点关掉**；② `Image.set_sprite(ratingIcon, overrideIcon)`。**没有**任何 SetActive 碰 `Secondary Icon` | `AllianceRatingDisplay__Initialize.c:47-49`、`:154-159` |

三种**「不是 activeSelf 的关」**，别混：
1. `···4 Image Container/Image`（`<无图>`）与 `·····5 MainRating`、`···4 Edit Name Button` 的 **Image 组件 `m_Enabled=0`** —— 组件级关，节点本身仍 active。`MainRating` 连 HorizontalLayoutGroup 一起 `m_Enabled=0` ⇒ 那个布局件**原版不跑**（工具 07:51 版已按此跳过，见 `工具/menu_dump.py:410-413`）。`Edit Name Button` 的 Image 关掉 = **按钮逻辑还在，只是不画底**。
2. `···3 Raycast Target` 的 `Simple (1,1,1,0)` = **alpha 0 的透明点击层**。
3. `·1 Profile Player Info/Avatar Item Small` 的 `Image` 子节点 `<无图>` + `m_Enabled=0` + `preserveAspect`：**空槽**，`AvatarDisplay` 运行期才填 `avatarImage`。

### A·2 ppu ≠ 100 / offset ≠ 0 / 九宫格 ≠ 全 0 的图（本页单点）

| 表里哪一行 | sprite | 异常项 | 值 |
|---|---|---|---|
| `····4 DivisionImage` | `04-Admiral` | **ppu≠100** | `m_PixelsToUnits = 50`（工具列印 `ppu=50`）。⇒ 512px 的图按 **2×** 画（512/50 = 10.24 世界单位） |
| `····5 FactionScoreSmall` / `(1)` / `(2)` / `(3)` | `Background`（Unity 内置） | **ppu≠100** | `ppu=200`，九宫 `10,10,10,10` |
| `··2 bg`（Top4）、`··2 bg`（AllFactions）、`··2 scroll rect/viewport` | `UI_Deck_Information_submenu_Back` | **九宫 ≠ 0** | `18,18,18,18` |
| `···4 Edit Name Button`（×2） | `40k_menu_bt_general_bg` | **九宫 ≠ 0** | `15,15,15,15`，`m_Type=1 (Sliced)` |
| `·····5/6 Button Outline`（×2） | `40k_menu_bt__general_outline` | **九宫 ≠ 0** | `15,15,15,15`，`Sliced` **且 `fillCenter=0`**（空心框） |
| — | 本页全部有图节点 | **offset ≠ 0** | **没有**。逐张核过：`UI_Deck_Information_submenu_Back` / `40k_menu_bt_general_bg` / `40k_menu_bt__general_outline` / `40k_general_bt_yellow_edit` / `40k_DeckSelection_icon_FactionUM` / `40k_UI_icon_ranked_Skirmish` / `Menu_Icon_Galon` / `UI_Button_Round_background` / `04-Admiral` / `Roman V` / `40K_generic_bt_info` / `Player_Avatar_selected` / `Player Profile Border` 的 `m_Offset` 全 `(0,0)` |
| `····4 DivisionImage` | `04-Admiral` | 另一个 `m_PixelsPerUnitMultiplier` 类异常 | 本页**没有**；同类异常在窗外：6 个页签键的 `button_bg` = `40K_settings_button` 带 `ppuMul=0.9200000166893005` |

🔴 **同一控件两套图（一个值 ≠ 全部情况）** —— 18 个 `AllianceRatingDisplay` 的 `Main Icon` 有**两种** sprite：
- 值那一行（`Alliance Rating Display`，指"当前分"）→ `40k_UI_icon_ranked_Skirmish`（本页 10 处）
- 最高分那一行（`MaxRating` 与 `Alliance Rating Display (1)`）→ **`Menu_Icon_Galon`**（本页 8 处）
⇒ 施工时 `MaxRating`/`(1)` 那 8 个位置**必须用 `Menu_Icon_Galon`**，别图省事全填一个。`Secondary Icon` 18 处全是 `40k_UI_icon_ranked_Skirmish`（且全 F）。

### A·3 `divisionImage`（段位图）—— 一套几张、叫什么、怎么选；`WF_UI_Ranked_Background_*` 是不是它

**结论：不是 `WF_UI_Ranked_Background_*`。**

**① 节点与出厂占位**
- `Top4/center/DivisionImage`，父 = `center`（584.36,345.01→953.66,875.91）；RT 上的 MB pid = `-6424707991535519182`，类 `DivisionImageDisplay`，GO 名 `DivisionImage`。
- `DivisionImageDisplay` 字段（签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DivisionImageDisplay.cs`）：`divisionImage`(Image) · `rankImage`(Image) · `divisionImageRef` · `rankImageRef`（后两个是 `AssetReferenceTyped<Sprite>`）。
- 预制体里**烧死的占位图**：`divisionImage` = **`04-Admiral`**（pid `5906162482326914306`，512×512，`m_PixelsToUnits=50`）；子节点 `RankImage` = **`Roman V`**（pid `-5716310642658349672`，128×128）。

**② 运行期怎么选**（`DivisionImageDisplay__Initialize.c`）
```
divisionImage.sprite = RankedDivisions.Instance.GetDivisionData(rankedDivision.DivisionNumber).Image.Load()
rankImage.sprite     = RankedDivisions.Instance.GetRankData(rankedDivision.RankNumber).Image.Load()
```
- 两个都是 **Addressables 按号取**（`AssetReferenceTyped<Sprite>.Load`），不是本地 Resources。
- `RankedDivisions.GetDivisionData(n)`：`n = Math.Max(n,1)` → `FirstOrDefault(x => x.divisionNumber == n)`，**找不到就 `Last()`**（`RankedDivisions__GetDivisionData.c`）。`GetRankData` 同构（`RankedDivisions__GetRankData.c`）。
- 入参 `RankedDivision` 来自 `rankedMode.GetCurrentRankedDivision()`，`rankedMode` 是 `RankedV2Event`（`RankedTab.rankedMode`，`RankedTab__InitializeFactionScores.c` 里 `(**(*plVar3 + 0x7f8))(...)` 那个虚调用）。

**③ 这一套是几张**（`d:/2/新解包资源/assets_full/bundle_rankeddivisionicons_assets_all/Sprite/`，共 **13 张**）

| 用途 | 图名 | PathID | 原尺寸 |
|---|---|---|---|
| 段位大图（`divisionImage`） | `01-Rook` | `-4808416863998433128` | 512×512 |
| | `02-Veteran` | `6744171633208131159` | 512×512 |
| | `03-Commander` | `8470145278166150330` | 512×512 |
| | `04-Admiral` ← 预制体占位 | `5906162482326914306` | 512×512 |
| | `05-Conqueror` | `-6668320921570210241` | 512×512 |
| | `06-Galactic Threat` | `980477926258408883` | 512×512 |
| | `07-Legend` | `-4146824666992498464` | 512×512 |
| 名次罗马数字（`rankImage`） | `Roman I` | `3944707242663776026` | 128×128 |
| | `Roman II` | `-525802625147296967` | 128×128 |
| | `Roman III` | `-3759885777766932904` | 128×128 |
| | `Roman IV` | `-6832000380511524817` | 128×128 |
| | `Roman V` ← 预制体占位 | `-5716310642658349672` | 128×128 |
| | `Roman VI` | `-5671325333709710196` | 128×128 |

⇒ **7 个大段位 + 6 个名次数字**，**不是金/银/铜**。

**④ `WF_UI_Ranked_Background_{Gold,Silver,Bronze}` 是另一个东西 —— 已核实**
- 三张都是 **352×116**，pid 分别为 `-8727949198000309980`(Gold) / `1270463985169458172`(Silver) / `3725724769207586641`(Bronze)，在 **`bundle_atlasindividual_assets_0_mainmenu`（图集名 `0_MainMenu`，161 切片）**。
- 引用方（逐条爬到根）：`Background` 节点，父链 = `Player Profile Ranked Trophies {Gold,Silver,Bronze}` → `Content` → `Legendary Display Profile` → `Ranking` → `Profile Tab`。
- 组件 = `LegendaryRankAllTrophiesDisplay`（字段 `ratingDisplay,trophyDisplayBronze,trophyDisplayGold,trophyDisplaySilver`，见 `bundle_menus_assets_all` 骨架）。
⇒ 它是 **Profile 页「传奇段位三色奖杯」的底**，**与 Ranked 页的 `DivisionImage` 无关**。（旁证：`资料/普查产出_0917/菜单盘点_块4.md:114` 也把它记成「档案段位奖杯金/银/铜」。）

**⑤ 工程 `Resources/Art/` 现状**：`Roman_V.png` **有**（`ui_menu/`）；`04-Admiral` / `01-Rook` / `07-Legend` 等**缺**（见 §B）。
**⑥ 拿不到的**：`RankedDivisionsSO` 资产本体 —— 见 §D·1。

### A·4 🔴 工具的已知偏差（本页哪几行要改口）

**偏差 1（新发现，影响面大）：布局组里若有 `activeSelf=false` 的子节点，表值偏。**
- 判据：`工具/menu_dump.py:428` `kids = [b.rt.get(str(c)) for c in b.children(rtpid)]` 取的是**全部**子节点，**没有按 active 过滤**；而 Unity `LayoutGroup.rectChildren` 只收 `activeInHierarchy == true` 的子节点。
- 本页受影响的行（`Main Icon` / 文字 的位置会整体左移）：

| 布局组（表里的行） | 里面的 inactive 子节点 | 表里给的（错） | Unity 实际会给的 |
|---|---|---|---|
| `···4 Alliance Rating Display`（`Alliance Info` 下） | `Secondary Icon`(44.4 宽, F) | Main Icon x=**554.59**、文字 x=**614.59** | Main Icon x=**510.19**、文字 x=**570.19** |
| `····5 Alliance Rating Display` / `····5 MaxRating`（`#1/#2/#3/#4 FactionScoreBig`，共 8 组） | `Secondary Icon`(44.4 宽, F) | Main Icon x=**386.56**、文字 x=**426.56**（表里文字宽 0.00） | Main Icon x=**342.16**、文字 x=**382.16** |
| `·····6 Global Rating`（`MainRating` 下） | `Secondary Icon`(60 宽, F) | Main Icon x=**651.29** | Main Icon x=**591.29** |
| `·····6/7 Alliance Rating Display` / `(1)`（`AllFactions`，共 8 组） | `Secondary Icon`(44.4 宽, F) | Main Icon x=**1339.54**、文字 x=**1491.36** | Main Icon x=**1295.14**、文字 x=**1446.96** |
| `···3 Name and Title Holder`（`Info Section with Alliance` 下，⚠️unk） | `Edit Name Button`(53.1 宽, F) | `Player Name` x=**568.29** | `Player Name` x=**510.19** |

- ⚠️ **同一份数据还有第二重原因**：`Ranking Tab` 根 `m_IsActive=false` ⇒ 在**预制体资产**里整棵子树 `activeInHierarchy` 都是 false，Unity 打开 prefab 时那个布局**一个子节点都不收**，表里这些 `anchoredPosition` 只是**上次序列化下来的值**。运行期 `TryOpenTab()` 点亮本页 → `LayoutRebuilder` 重跑 → 才按上面「Unity 实际」那一列落位。**⇒ 施工时按右列摆，别按表值。**

**偏差 2（已被工具作者修掉，记一笔以免以后拿旧版对账）**：07:38 版会把 `m_Enabled=0` 的布局件照跑。本页 `MainRating` 就是 `m_Enabled=0` 的 HorizontalLayoutGroup ⇒ 旧版会把它 5 个子节点挪走。**本文用的是 07:51 版，已修。**

**偏差 3（工具自己标了，照抄即可）**：`⚠️unk` 的 5 个布局组 —— `Name and Title Holder`(HorizontalLayout) · `left-side` · `center` · `MainRating` · `right-side` —— 主轴尺寸算不准（子节点含文字 / 嵌套布局件 / ScrollRect，需要 Unity 的字体度量）。**受影响最明显的是 `#1..#4 FactionScoreBig` 的宽 `0.00`**：`left-side`/`right-side` 是 `ctrlW=1 expandW=0`，宽度 = 子节点的**首选宽**，而首选宽要文字度量 ⇒ **表里那个 0.00 不是真值**。

### A·5 多实例 / 重名（多份同名节点，用我给的口径区分）

**本页内重名（表里靠层级路径 + 布局件类型区分）**
- `Name and Title Holder` **×2** —— 一个挂 `HorizontalLayoutGroup`（With-Alliance），一个挂 `VerticalLayoutGroup`（Without-Alliance）。**布局件完全不同，别合并成一个 prefab。**
- `Edit Name Button` **×2**，是**两个实例**：`EverguildButton.target` 分别 `2434526262644144690` / `-2808983208499250638`。
- `bg` **×2**（`Top4/bg` 与 `AllFactions/bg`）、`content` **×2**（`Top4/content`(HorizontalLayout) 与 `AllFactions/scroll rect/viewport/content`(VerticalLayout+ContentSizeFitter)）—— 不同 RT。
  ⚠️ `RankedTab.factionListHolder` 指的是**后者**：MB pid `-8377326232796431822`，GO 名 `content`，**父 = `viewport`**（1305.29,288.62→1746.97,864.38）⇒ 唯一。
- `Alliance Rating Display` **×18**（含 `(1)` 后缀 4 个）；`Secondary Icon`/`Main Icon`/`Individual rating value` 各 **×18**；`icon` **×8**（`#1..#4` 4 个 + `FactionScoreSmall ×4`）；`Player Name`/`Player Title` 各 **×2**。
- `#1/#2/#3/#4 FactionScoreBig` —— **不在本页 `factionListHolder` 下的 4 行**，是 `Top4` 的固定 4 格；`AllFactions` 的 4 行是 `FactionScoreSmall{,1,2,3}`（另一个 prefab 模板）。

**跨页重名（另一份实例，别串）**
- `Profile Tab/Player Info/Avatar Item Small`（`EverguildButton.target = -2195724460908250574`）与本页 `Profile Player Info/Avatar Item Small`（`target = 6879724527869852210`）——**两个实例**。
- `Info Section with Alliance` / `Info Section without Alliance` / `Player Level` / `Image Container` / `Avatar Name` 在 Profile 页与 Ranked 页**各一份**。
- ⚠️ 正本 §2·2 提醒的 `:15944` 的 `Ranking`（`ProfileRankingSection`）与本页 `:16352` 的 `Ranking Tab`（`RankedTab`）是两个节点 —— 已复核：类名、字段列表、RT pid 全不同。

### A·6 序列化引用（正本没记，工具也不打）

`RankedTab` 的 MB = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_1724627060727839282.json`（`m_Script.m_PathID = 2802945728285004666`）：

| 字段 | 目标 GO | 目标类 |
|---|---|---|
| `infoSection` | `Profile Player Info` | `PlayerInfoDisplay` |
| `divisionImage` | `DivisionImage` | `DivisionImageDisplay` |
| `globalRating` | `Global Rating` | `AllianceRatingDisplay` |
| `top4Factions[0]` | **`#1 FactionScoreBig`** | `FactionScore` |
| `top4Factions[1]` | **`#3 FactionScoreBig`** | `FactionScore` |
| `top4Factions[2]` | **`#2 FactionScoreBig`** | `FactionScore` |
| `top4Factions[3]` | **`#4 FactionScoreBig`** | `FactionScore` |
| `factionScorePrefab` | **`FactionScoreSmall`** | `FactionScore` |
| `factionListHolder` | `AllFactions/scroll rect/viewport/content` | `RectTransform` |

🔴 **`top4Factions` 的顺序不是树序**：数组是 `#1, #3, #2, #4`（= 左列上、右列上、左列下、右列下），**不是** `#1,#2,#3,#4`。照抄数组顺序会张冠李戴。
🔴 `factionScorePrefab` = **`FactionScoreSmall`**（不是 `#N FactionScoreBig`）—— 第 5 名以后的行是 `Instantiate(FactionScoreSmall, content)`。

---

## §B 🔴 12 个 sprite 的 PathID → 图名总表

> 解析办法：`d:/2/Warpforge_tools/data/ui_extract/*/Sprite/*.json` 里的 `"pathid"` ↔ `"m_Name"`，**按有符号 int64 精确匹配**（前缀 `-` 的也当负数）。全部命中，无歧义。
> 原尺寸/九宫格：`素材/Warpforge原版/UI图集/图集/*/Sprite/*.json` 的 `m_Rect`/`m_Border`，缺的退到 `assets_full/bundle_*/Sprite/<名>.json`。
> 工程口径：`CardArt.MenuUi(name)` = `Art/ui_menu/` → `Art/ui_deck/` → `Art/ui/`（`MyGame/Assets/CardPresentation/Core/CardArt.cs:449`），**文件名空格换下划线**。

### B·1 §2·1 骨架表里出现过的 PathID（**11 个**，去重后）+ 补全的 2 个被截断的

| 节点（全路径） | PathID | 图名 | 原尺寸 | 九宫格 border | 图集 / bundle | 工程 `Resources/Art/` |
|---|---|---|---|---|---|---|
| `Player Profile Window/Menu Dark Background` | **`0`** | **无图**（确认） | — | — | — | — |
| `…/Tab  Area/Generic Window Red Background Big` | `5230836453799319039` | `UI_Deck_Information_Back` | 1100×701 | **42,363,655,81** | `0_MainMenu` 图集（`bundle_atlasindividual_assets_0_mainmenu/Sprite/`） | ✅ `ui_deck/UI_Deck_Information_Back.png` |
| `…/Generic Close Button Orange` | `2381704724431365035` | `UI_Button_Round_background` | 237×237 | 否 | `bundle_duplicateassetisolation_assets_all`（去重资源） | ✅ `ui_menu/`（`ui/` 也有同名） |
| `…/Generic Close Button Orange/Background` | **`5693181797853584851`**（补全） | `40k_general_bt_yellow` | 71×71 | 否 | `bundle_duplicateassetisolation_assets_all` | ✅ `ui/40k_general_bt_yellow.png` |
| `…/Generic Close Button Orange/Icon` | **`-2367583692806092745`**（补全） | `40k_general_bt_yellow_close` | 71×71 | 否 | `bundle_duplicateassetisolation_assets_all` | ✅ `ui_deck/40k_general_bt_yellow_close.png` |
| `…/Tab Buttons/Profile Button/Icon` | `5856105142466294224` | `40K_Profile_icon_profile` | 134×133 | 否 | `0_MainMenu` 图集 | ✅ `ui_menu/` |
| `…/Avatar Button/Icon` | `-179571980242263158` | `40K_Profile_icon_avatar` | 134×133 | 否 | `0_MainMenu` 图集 | ✅ `ui_menu/` |
| `…/Title Button/Icon` | `1862605419236700721` | `40K_Profile_icon_title` | **134×88** | 否 | `0_MainMenu` 图集 | ✅ `ui_menu/` |
| `…/Battle Log Button/Icon` | `3152911835936020775` | `40K_Profile_icon_battlelog` | 134×133 | 否 | `0_MainMenu` 图集 | ✅ `ui_menu/` |
| `…/Trophies/Icon` | `-6736605493054965255` | `40K_Profile_icon_Trophies` | 134×133 | 否 | `0_MainMenu` 图集 | ✅ `ui_menu/` |
| `…/Ranked/Icon` | `-6552398156213418268` | `40k_UI_icon_ranked_Skirmish` | 128×128 | 否 | `bundle_armyicons_assets_all` | ✅ `ui_deck/` |
| 6 个页签键 `offSprite` | `1956647257489494794` | `40K_settings_button` | 168×156 | 否 | `bundle_duplicateassetisolation_assets_all` | ✅ `ui_menu/` |
| 6 个页签键 `onSprite` | `-2307655919992762574` | `40K_settings_button_hover` | 168×156 | 否 | `bundle_duplicateassetisolation_assets_all` | ✅ `ui_menu/` |

**核对结果**：给出的 10 个「已知」PathID **全部与实测一致**，无一处串号；两个被截断的也**取全并命中**：
- `569318179785…` → **`5693181797853584851`**（`40k_general_bt_yellow`）
- `-23675836928…` → **`-2367583692806092745`**（`40k_general_bt_yellow_close`）
- `Menu Dark Background` 的 sprite **确实是 `0`**：`RectTransform 2780416193164180018` → GO `Menu Dark Background`（pid `-4105880026997163470`）→ `MonoBehaviour/MonoBehaviour_-6056501761652655566.json` 里 `{"m_Sprite": {"m_FileID": 0, "m_PathID": 0}, "m_Color": {r:0,g:0,b:0,a:0.772549…}, "m_Type": 0}` ⇒ **原版就没图，纯黑 77.25% 遮罩**。

⚠️ 一个反直觉点：**§2·1 的 22 张「具名图」一张都不在这 11 个里** —— 复核 `ls bundle_menus_assets_all/Sprite/`（22 个文件：`40K_rewards_bt_missions` / `40k_Generic Smooth line` / `40k_popup_texture` / `Legions_Logo` / `Main Menu Fake Background` / `WF_UI_Trophy_Gold` …）⇒ 正本 `:108` 那条判断**成立**。

### B·2 整扇窗（根 pid `8897360498599033394`）出现过的 **全部 sprite 名**（54 个）

命令（用户指定 depth 6 + 我补跑到 depth 14 才收全）：
```bash
python 工具/menu_dump.py bundle_menus_assets_all --rt 8897360498599033394 --depth 14 --md --no-layout   # 627 行
```
> depth 6 只到「页签根」那一层，收不全（那时只 31 个）；**下表是 depth 14 的全量**。

**✅ 工程 `Resources/Art/` 有（39 个）**
`40K_Profile_icon_Trophies`(ui_menu) · `40K_Profile_icon_avatar`(ui_menu) · `40K_Profile_icon_battlelog`(ui_menu) · `40K_Profile_icon_profile`(ui_menu) · `40K_Profile_icon_title`(ui_menu) · `40K_button`(ui_menu) · `40K_dropdown_bg`(ui_deck) · `40K_generic_bt_info`(ui_menu，⚠️**工程文件是小写 `40k_generic_bt_info.png`**) · `40K_main_rank_display`(ui_menu) · `40K_missions_icon_Daily skulls`(ui_menu) · `40K_settings_button`(ui_menu) · `40k_Achievements_icon_seal points`(ui_menu) · `40k_DeckSelection_icon_FactionSororitas`(ui_deck) · `40k_DeckSelection_icon_FactionUM`(ui_deck) · `40k_UI_icon_ranked_Skirmish`(ui_deck) · `40k_bt_close`(ui) · `40k_campaign_bar_bg/end/fill/outline`(ui_menu) · `40k_general_bt_yellow`(ui) · `40k_general_bt_yellow_close`(ui_deck) · `40k_general_bt_yellow_edit`(ui_deck) · `40k_main_bt_play`(ui_menu) · `40k_popup`(ui_menu) · `40k_popup_texture`(ui_menu) · `Player Profile Border`(ui) · `Player_Avatar_selected`(ui_menu) · `Rank Skull`(ui_menu) · `Roman V`(ui_menu) · `UI_Button_Mulligan`(ui_menu) · `UI_Button_Round_background`(ui_menu) · `UI_Deck_Information_Back`(ui_deck) · `UI_Deck_Information_submenu_Back`(ui_menu) · `UI_Deck_Selection_Back`(ui_deck) · `UI_Deck_Selection_Back_simple`(ui_deck) · `WF_UI_Ranked_Background_Gold`(ui_menu) · `WF_UI_Trophy_Gold`(ui) · `WF_icon_clock`(ui_menu)

**❌ 工程里没有（15 个）**（口径：`os.walk` 整个 `CardPresentation/Resources/Art/`，名字做过空格→下划线 + 全小写两轮匹配）
| sprite | 谁用（节点） | 是不是 Ranked 页要用 |
|---|---|---|
| `04-Admiral` | `RankedTab/Top4/center/DivisionImage` | ✅ **要**（段位大图，至少 1 张） |
| `07-Legend` | 同族（段位图其余 6 张同缺：`01-Rook`…`06-Galactic Threat` 也不在） | ✅ 要做完整段位就要 7 张 |
| `Menu_Icon_Galon` | `RankedTab` 的 `MaxRating/Main Icon` ×4 + `AllFactions` 的 `Alliance Rating Display (1)/Main Icon` ×4 | ✅ **要（8 处）** |
| `40k_menu_bt_general_bg` | 两处 `Edit Name Button` 的底 | ✅ **要** |
| `40k_menu_bt__general_outline` | 两处 `Button Outline` 的框 | ✅ **要** |
| `40K_profile_ForgeLevel_bg` | Profile 页 Forge 容器 | ❌ |
| `40k_general_bt_yellow_replay` / `40k_general_bt_yellow_pin replay` | Battle Log 页 | ❌ |
| `Rank Skull Empty` | Trophies 页 | ❌ |
| `Feedback Scoring Button` | Trophies 页 `Counter` | ❌ |
| `40k_profile_icon_copy` | Profile 页 `PlayerId/Image` | ❌ |
| `WF_UI_Ranked_Background_Silver` / `_Bronze` | Profile 页传奇奖杯（Gold 那张有） | ❌ |
| `Background`(pid `1660267235368898380`) | `FactionScoreSmall` 底 / 结算黑幕等 | ❌ —— **Unity 内置 sprite，不是原版美术缺件**（`d:/2/解包整理/12_主程序资源/内置资源/Sprite/Background_1660267235368898380.json`，32×32，九宫 10,10,10,10） |
| `UIMask`(pid `-426171492875694260`) | 排行榜类 `Viewport` | ❌ —— **同上，Unity 内置**（`…/内置资源/Sprite/UIMask_-426171492875694260.json`，32×32，九宫 10,10,10,10） |

**⇒ Ranked 页要补的图 = 4 张：`Menu_Icon_Galon` · `40k_menu_bt_general_bg` · `40k_menu_bt__general_outline` · `04-Admiral`（+ 段位族其余 6 张，若要做段位切换）。**
⚠️ `40K_generic_bt_info` 的工程文件名是**小写 `40k_generic_bt_info.png`**，而 sprite 名是大写 `40K_generic_bt_info` —— 走 `CardArt.MenuUi("40K_generic_bt_info")` 在 Windows 上能取到（Unity `Resources.Load` 在 Windows 不区分大小写），但**别指望它跨平台**，建议按现有调用惯例传 sprite 名原样。

---

## §C `RankedTab` 的「入口 / 它调用的 / 谁监听它」三张清单

> 类：`public class RankedTab : WindowTabBase<PlayerProfileMenu>`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/RankedTab.cs:5`）
> 反编译：`d:/2/tools/decomp_full/RankedTab*.c`（共 **9** 个产物）
> 边界：**只算档案窗里的 Ranking 页**；外面那几扇排行榜弹窗（`RankedClassicLeaderboardPopup` / `RankedSkirmishLeaderboardPopup` / `DraftLeaderboardPopup` / `Ranked Leaderboard Display`，正本 §三）**不计入** —— 见文末「共用」一段。

### C·1 入口（谁进 `RankedTab`）

| # | 入口 | 链条 | 出处 |
|---|---|---|---|
| 1 | **框架开窗** | `GameWindowWithTabs.Open()` → `OpenTabs()` → `GetStartingTab()` → `ChangeTab(tab)` → `ChangeTabCO(from,to)` → `WindowTabBase.TryOpenTab()` → `GameObject.SetActive(true)` → **虚调用 `OnOpen()`** | `GameWindowWithTabs__Open.c:44-58`、`__OpenTabs.c`、`__GetStartingTab.c`、`__ChangeTabCO.c:44-58`、`WindowTabBase__TryOpenTab.c:7-11` |
| 2 | **用户点页签** | `TabButtons.Toggle(bool state, WindowTabBase tab)` → `GameWindowWithTabs.ChangeTab(tab)` | `TabButtons__Toggle.c:18` |
| 2b | ↳ 坑：`TabButtons` 有个**一次性闸门** `firstTime`（`+0x40`，`TabButtons..ctor` 初值 1）。`Toggle` 第一次进来**只把闸门清 0、不切页** | | `TabButtons__.ctor.c:16`、`TabButtons__Toggle.c:11/28` |
| 3 | **本页自己的 `Setup`** | `WindowTabBase__Setup` 写 `baseWindow` 并虚调用 `OnSetup()`；`RankedTab.Setup` 覆写：取 `LiveOpsManager.GetHandler<RankedV2Event>()` → `GameModes.GetActiveEvent<RankedV2Event>()` 存进 `rankedMode` | `WindowTabBase__Setup.c:5-8`、`RankedTab__Setup.c:40-46` |
| 3b | ↳ **本页可能整页不存在** | 事件拿不到 ⇒ `GameWindowWithTabs.DestroyTab(this)`（把本页从 `tabs` 列表摘掉 + `TabButtons.RemoveTabButton` + `Object.Destroy`） | `RankedTab__Setup.c:46`、`GameWindowWithTabs__DestroyTab.c:15-24` |
| 4 | **本页内部自走** | `<Initialize>b__10_0`（0 参数 lambda，挂在 `PlayerInfoDisplay.onAvatarClick`）→ `GameWindowWithTabs.ChangeTab<AvatarTab>()` ⇒ **点本页头像会跳到 Avatar 页** | `RankedTab___Initialize_b__10_0.c:9-11` |
| 5 | **`OnOpen` → `Initialize`** | 两个函数体**逐指令一致**（除各自的 `this` 空判），且 `Initialize` 是 `private`、无处显式调 ⇒ **编译器把 `Initialize()` 内联进了 `OnOpen()`**（判据：`RankedTab__OnOpen.c` 与 `RankedTab__Initialize.c` 除开头 `NetworkingPeer__OnMessage(param_1,0);` 那两行外完全相同） | `RankedTab__OnOpen.c` vs `RankedTab__Initialize.c` |

⚠️ `Setup` / `OnOpen` 里**都没有**碰 `GameWindowWithTabs.OnTabChanged`。

### C·2 它调用的（按类归类；含"没挂在本页树上但被它驱动"的）

| 类 | 方法 | 传什么 | 出处 |
|---|---|---|---|
| `PlayerInfoDisplay` | `Initialize(PlayerInfo)` | `Window.PlayerInfo`（`*(Window+0xa0)`） | `RankedTab__Initialize.c:22-23` |
| `PlayerInfoDisplay` | `add_onAvatarClick(...)` | 包了 `RankedTab.<Initialize>b__10_0` 的 `UIGenericEventCatcher.SourceDelegate`；**仅当 `PlayerInfo.<bool>` 为真时挂** | `RankedTab__Initialize.c:26-33` |
| `LeaderboardManager`（`SingletonBehaviour.Instance`） | `GetLeaderboard(<从 rankedMode 取>, Action<object> 回调)` | 回调 = 缺失的 `b__10_1` → `InitializeFactionScores(result)` | `RankedTab__Initialize.c:39-47`；回调判据见 §D·3 |
| `DivisionImageDisplay` | `Initialize(RankedDivision)` | `rankedMode.GetCurrentRankedDivision()`（虚调用 `*plVar3+0x7f8`） | `RankedTab__InitializeFactionScores.c:66` |
| `AllianceRatingDisplay` | `Initialize(int value, Sprite overrideIcon=null, bool disableIfZero=false, bool useSealSystem=<flag>, bool zeroBasedRank=true)` | 分数 = `result` 里那格的 `+0x18`；函数体把**第 4 个 bool 从栈上未初始化值传进去**（反编译读成 `in_stack_...`）⇒ 该 bool **原文是什么查不到** | `RankedTab__InitializeFactionScores.c:71-73`；签名 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AllianceRatingDisplay.cs:15` |
| `SupportMethods` | `DestroyAllChildren(Transform)` | `factionListHolder`（= `AllFactions/scroll rect/viewport/content`）⇒ **每次进来先清空 4 行** | `RankedTab__InitializeFactionScores.c:74` |
| `FeatureConfig` | `GetValidArmies()` | 然后 LINQ（`Select` → `OrderBy*` → `ToList`，`FUN_180c9b190/180c99380/180ca0360`） | `RankedTab__InitializeFactionScores.c:77-95` |
| `UnityEngine.Object` | `Instantiate(factionScorePrefab, factionListHolder)` | 仅当 `i >= top4Factions.Length`（前 4 个**复用**树上的 `#1..#4 FactionScoreBig`） | `RankedTab__InitializeFactionScores.c:110-118` |
| `FactionScore` | `Initialize(CardArmy faction, int currentScore, int highestScore)` | | `RankedTab__InitializeFactionScores.c:120`、`:135`；签名 `FactionScore.cs:13` |
| `LeaderboardEntry` | `GetScoreByArmy(entry, army, ...)` / `GetHighestScoreByArmy(entry, army, score)` | 在 `b__11_1` 的 `Select` 里 | `RankedTab.__c__DisplayClass11_0___InitializeFactionScores_b__0.c:22-25` |
| `UnityEngine.Object` | `set_name(string.Format(<fmt>, <boxed int>))` | 给每个 `FactionScore` **运行期改名** | `RankedTab__InitializeFactionScores.c:121-122`；格式串查不到（§D·5） |
| `System.Collections.Generic.List<FactionScore>` | `Add` | 新建的那几个进 `factionScores` | `RankedTab__InitializeFactionScores.c:126` |
| `GameWindowWithTabs` | `DestroyTab(this)` | `Setup` 里事件缺失时 | `RankedTab__Setup.c:46` |
| `GameWindowWithTabs` | `ChangeTab<AvatarTab>()` | 头像点击时（`b__10_0`） | `RankedTab___Initialize_b__10_0.c` |
| `GameModes` / `LiveOpsManager` | `GetActiveEvent<RankedV2Event>` / `GetHandler<RankedV2Event>` | `Setup` | `RankedTab__Setup.c:40-42` |

### C·3 谁监听它

- **`RankedTab` 自己不暴露任何 event/delegate**（签名桩 `RankedTab.cs:1-44` 没有 `event`、没有 `Action`/`UnityEvent` 字段）⇒ **「谁监听它」= 无**。它只**订阅别人**：`PlayerInfoDisplay.onAvatarClick`、以及 `LeaderboardManager.GetLeaderboard` 的回调。
- **谁调它的方法**：全工程**没有第二处直接点名 `RankedTab`**。
  - 判据（**防止把「产物缺失」误判成「零调用」**）：
    1. `grep -rln "RankedTab" d:/2/tools/decomp_full` ⇒ **9 个文件，全部是 `RankedTab` 自己的产物**（`.ctor` / `__c__.cctor` / 4 个方法体 / 3 个 lambda）。
    2. `grep -rn "RankedTab" d:/2/Warpforge_code/Scripts/Assembly-CSharp` ⇒ 只命中 `RankedTab.cs` 自身。
    3. **它的入口是虚调用，反编译里看不到符号名** —— `WindowTabBase__Setup.c:8` 走 `(**(code**)(*param_1 + 0x198))`（=`OnSetup()` 槽），`WindowTabBase__TryOpenTab.c:11` 走 `+0x1b8`（=`OnOpen()` 槽），`WindowTabBase__CloseTab.c` 走 `CloseTab`。`GameWindowWithTabs` 遍历 `tabs`（`param_1[0xf]` = `+0x78`）时对每个元素打同一批槽位 ⇒ **这才是「谁调它」的实际入口**，且调用点是**编译器生成的虚表**，不是源码里的 `RankedTab.Xxx()`。
    4. `Setup`/`OnOpen` 都是 `override`（`RankedTab.cs:29/33`），基类 `WindowTabBase` 声明为 `virtual` ⇒ **源码里本来就不该有直接调用**。
  - ⇒ 结论「零外部直接调用点」**可信**，且**原因已说明**（虚分派），不是产物缺失。
- **反向**：`RankedTab` 挂的 `PlayerInfoDisplay.onAvatarClick` 是**多播**（`System.Delegate.Combine`，见 `PlayerInfoDisplay__Initialize.c` 里 `+0x48` 的 Combine）⇒ 同一个 `Profile Player Info` 上**可能同时挂着别人**（Profile 页的 `PlayerInfoDisplay` 是**另一个实例**，两条链互不干扰）。

### C·4 与「外面那几扇排行榜弹窗」的关系

- **代码：零共用。** `RankedClassicLeaderboardPopup` / `RankedSkirmishLeaderboardPopup` / `DraftLeaderboardPopup` / `Ranked Leaderboard Display` 与本页**没有共同的基类、没有互相调用**（`grep -rln "RankedTab"` 已证）。
- **资源：共用 3 张图**（但节点树各建各的，别合并）：
  - `UI_Deck_Information_submenu_Back` —— 本页 `bg`/`bg`/`viewport` 与 §3·2 的行底同图不同节点。
  - `.css 40k_UI_icon_ranked_Skirmish`（pid `-6552398156213418268`）—— 本页的排名图标，也是 §3·2 `PlayerRankingRow/RankingIcon` 那张。
  - `Background`（pid `1660267235368898380`，Unity 内置）—— 本页 4 行 `FactionScoreSmall` 的底，也是 §3·2 `PlayerRankingRow/Background`+`BackgroundHighlight` 那张（正本 §3·2 已记）。
- ⚠️ **本页没有 `PlayerRankingRow`**：本页的行族是 `FactionScoreSmall`（`Image + FactionScore`，字段 `factionImage,highestRatingDisplay,ratingDisplay`），**不是**排行榜弹窗的 `PlayerRankingRow`（9 子节点）。别把两者混成一个 prefab。

---

## §D 查不到的（每条：查不到什么 + 搜过哪些词 + 搜过哪些目录）

### D·1 `RankedDivisionsSO` 资产本体（⇒「段位号 → 图名」的逐条对照表拿不到）
- **查不到**：`RankedDivisions` 这个 `ScriptableObject` 的**序列化内容**（`divisions` / `ranks` 两张表的每一项 `divisionNumber` + `AssetReferenceTyped<Sprite>`）。
- **搜过**：`find d:/2 -iname "*RankedDivision*"`（全盘，排除 `decomp_full`/`Warpforge_code`/`ui_extract`）⇒ 只命中 `bundle_rankeddivisionicons_assets_all` 目录与 4 个语义类文件；`grep -rl "RankedDivisionsSO" d:/2/新解包资源/assets_full` ⇒ 0；`grep -rl "divisionNumber" --include=*.json d:/2/新解包资源/assets_full` ⇒ 0；`find d:/2/新解包资源/assets_full -iname "*RankedDivision*"` ⇒ 只 1 个目录。
- **目录**：`d:/2/新解包资源/assets_full/`（84 个 bundle 全扫）、`d:/2/Warpforge_tools/data/ui_extract/`（不含 SO）、`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`。
- **只能反推**：`bundle_rankeddivisionicons_assets_all` 里 7 张 `01-Rook`…`07-Legend` 的**序号**≈`divisionNumber`，6 张 `Roman I–VI` ≈`RankNumber`。**这是推断，不是证据**，别写成真值。

### D·2 `Avatar Name` / `Secondary Icon` 的「谁把它 SetActive(true)」
- **查不到**：点亮这两类节点的代码。
- **搜过**：`grep -n "SetActive" d:/2/tools/decomp_full/AvatarDisplay__*.c`（**只有 `ToggleHighlight.c:23`**，切的是 Highlight，不是 `avatarName`）；`grep -n "SetActive\|get_gameObject" AllianceRatingDisplay__Initialize.c`（**只有 `:47-49` 关自己 + `:154-159` set_sprite**）；`FactionScore__Initialize.c` / `PlayerInfoDisplay__Initialize.c` / `ProfileAllianceDisplay__Initialize.c` / `ProfileNameTitleSection__Initialize.c` 全看过。
- **判据（为什么不是"查漏了"）**：这两类节点的组件是 `Image` + `EverguildButtonMaterialModifier`（`Secondary Icon`）与 `TextMeshProUGUI` + `EverguildButtonMaterialModifier`（`Avatar Name`）—— **没有专属脚本**；且它们的 `m_Enabled=1`（不是组件级关掉）⇒ **就是预制体作者留的 `activeSelf=false`**，本页这一版不用。可能是给别的调用方（如排行榜行 / 头像选择面板）复用的模板件。
- **目录/词**：`d:/2/tools/decomp_full/`（`AvatarDisplay__*`、`AllianceRatingDisplay__*`、`ProfileNameTitleSection__*`、`PlayerInfoDisplay__*`）、`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`。

### D·3 `RankedTab.<Initialize>b__10_1`（`GetLeaderboard` 的回调）**产物缺失**
- **查不到**：方法体。`ls d:/2/tools/decomp_full | grep -i "^RankedTab"` ⇒ 只有 `RankedTab___Initialize_b__10_0.c`，**没有 `b__10_1.c`**。
- **为什么不能下「零调用」结论**：`RankedTab__Initialize.c:39-47` 里明确有 `System_Action<object>___ctor(uVar3, param_1, DAT_1842c39f8, 0)` 被传进 `Everguild_LiveOps_LeaderboardManager__GetLeaderboard`；而 `InitializeFactionScores` 是 **`private`**（`RankedTab.cs:41`），全工程**没有第二处**能调它 ⇒ **那个缺失的 lambda 就是它的唯一调用点**。
- **⇒ 记法**：`InitializeFactionScores(LeaderboardResult)` 的触发 = **「排行榜拉回来」**，具体回调体因产物缺失读不到。

### D·4 `InitializeFactionScores` 里的 LINQ 具体算子与排序键
- **查不到**：`FUN_180c9b190` / `FUN_180c99380` / `FUN_180ca0360` 分别是哪个 `Enumerable` 方法、排序键是什么。
- **已知形状**：`FeatureConfig.GetValidArmies()` → `Select(b__11_1)`（`b__11_1` = `return *(int*)(param_2+8)`，即取元组第 2 个 int）→ 再一个算子 → `ToList/ToArray` → 12 字节/元素（`(int,int,int)` 或 `KeyValuePair<int,int>` + 1 int）。`FactionScore.Initialize(faction, currentScore, highestScore)` 三参正好对上「元组 = (army, currentScore, highestScore)」。
- **搜过**：`d:/2/tools/decomp_full/`（il2cpp 泛型实例名没落在产物里）；`d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 签名桩里 `InitializeFactionScores` 是空体。

### D·5 `FactionScore` 运行期改名的**格式串**
- **查不到**：`System_String__Format(DAT_1842b68e8, <boxed int>)` 里 `DAT_1842b68e8` 指向的字面量。
- **判据**：`DAT_1842b68e8` 落在 `0x1842…`（**il2cpp metadata 段**），不是 `0x1834…` 那种 PE 只读字面量 ⇒ `工具/read_literal.py`（它读的是 `_DAT_1834…`）**读不了**，别硬读。
- **已知**：被 box 的数是 `local_38 = (int)uVar1` = 元组的**第 1 个 int（阵营/军队 id）**，**不是行下标**。预制体里 `#1.. #4 FactionScoreBig` 是**作者手写的名字**，运行期会被覆盖。
- **搜过**：`d:/2/tools/decomp_full/RankedTab__InitializeFactionScores.c`；`d:/2/游戏素材`（无 metadata dump 工具）。

### D·6 `AllianceRatingDisplay.Initialize` 第 4 个 bool 的**传参原文**
- **查不到**：`RankedTab__InitializeFactionScores.c:71-73` 里第 4 个实参读成 `in_stack_ffffffffffffffa8 & 0xff…00`（**栈上未初始化**，反编译伪影）。同理 `RankedTab__Setup.c:43` 的 `FUN_1811856e0(...)`（`rankedMode` 的「有效性」判定）也读不出语义。
- **搜过**：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 里 `AllianceRatingDisplay.Initialize` 是空体；`RankedV2Event` 的方法体在 `d:/2/tools/decomp_full/` 下（`Everguild.LiveOps.*`），未见对应名称。

### D·7 工程缺的原版图（不是"查不到"，是**确认没有**）
- **搜过**：`os.walk("MyGame/Assets/CardPresentation/Resources/Art")` 全树（含 `ui_menu/`、`ui_deck/`、`ui/`、`ui_campaign/`、`altarts/`、`cardbacks/`、`cards/`、`traits/` …），名字做过**原样 + 空格→下划线 + 全小写**三轮匹配。
- **确认缺**（本页要用的 4 张 + 段位族）：`Menu_Icon_Galon` · `40k_menu_bt_general_bg` · `40k_menu_bt__general_outline` · `04-Admiral`（以及 `01-Rook`/`02-Veteran`/`03-Commander`/`05-Conqueror`/`06-Galactic Threat`/`07-Legend`、`Roman I`–`IV`/`VI`）。
- **确认有**：`Roman V`（`ui_menu/`）· `WF_UI_Ranked_Background_Gold`（`ui_menu/`）。
- **另外 11 个整窗缺件**（非本页）：`40K_profile_ForgeLevel_bg` · `40k_general_bt_yellow_replay` · `40k_general_bt_yellow_pin replay` · `Rank Skull Empty` · `Feedback Scoring Button` · `40k_profile_icon_copy` · `WF_UI_Ranked_Background_Silver` · `WF_UI_Ranked_Background_Bronze` · `Background` · `UIMask`（后两个是 **Unity 内置 sprite**，本就不该在 `Art/` 里找）。

### D·8 没查的（明说）
- **`PlayerProfileMenu` / `TabbedWindowComponents` 的完整方法体** —— 只读了签名桩与 `Open`/`OpenTabs`/`GetStartingTab`/`ChangeTabCO`/`DestroyTab` 五个（C·1 够用）。`PlayerProfileMenu.__OnComplete` / `__OnError` / `Open` 的细节属于 §2·1 骨架那一层，**按你给的边界没重做**。
- **`RankedV2Event` 的方法体** —— 只用到 `GetCurrentRankedDivision()` 与「有效性」两处虚槽位，未展开。

---

## 一句话给下一手

`Ranking` 页的**结构真值**已在本文件 §A；施工要点是 **§A·4 的两处偏差（inactive 子节点 + 预制体态不跑布局）** 和 **§A·6 的 `top4Factions` 乱序**；**§B·2 那 4 张图要先导**；代码侧照 §C 就能一次接对（入口全在 `GameWindowWithTabs`/`TabButtons` 的虚分派里，`RankedTab` 一个外部直接调用点都没有）。
