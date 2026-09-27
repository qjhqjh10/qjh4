# 档案窗 · `Avatar Tab` / `Title Tab` · 原版普查（层 × 参数 + 判据）

- 日期 **2026-09-27** · 只读普查（`资料/普查产出_0927/`，本文件是这两页开工的**唯一尺子**）
- 正本骨架层 = `资料/阶段二_多人界面_原版规格.md` §2·1（**不重复**）；工具 = `工具/menu_dump.py`
- 表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt <pid> --depth 8 --md`（**逐行抄录，未删行**）
- ✅ 自检：`python 工具/menu_dump.py --verify-layout` → 与正本 §2·1 的 6 个页签键**逐位一致**
- ⚠️ 表里 `[!]grid?` = 工具原本的警告（`GridLayoutGroup` 不在它那套 uGUI 布局算法里，
  **那几行的子节点坐标不是「布局后」值** —— 格子尺寸/列数是另按网格参数手算的，见 §A-补 3）

## 0 共有前提（骨架层已定，不复查）

| 事实 | 值 | 出处 |
|---|---|---|
| 两页签的父 | `Tab Content` RT pid `-5566042365552723406`，1395.94×843.08 | `RectTransform_4150415914498688462.json` 的 `m_Father` |
| `Tab Content` 的 6 个子节点顺序 | Profile / **Avatar** / **Title** / Battle Log / Trophies / Ranking | 该 RT 的 `m_Children`（6 个 pid，按序） |
| Avatar Tab RT pid | `-4150415914498688462`，act=**F** | `GameObject/Avatar Tab.json: m_IsActive=false` |
| Title Tab RT pid | `6261725956636310066`，act=**T** | 同上 |
| 页签根比父级宽 33 | 锚点 (0,0)→(1,1)、`sizeDelta.x=32.6588`、`anchoredPosition.x` = −16.3294(Avatar) / −16.3293(Title) ⇒ **两侧各溢 16.33** | 两张表的首行。**按真值保留，未对齐** |
| 🔴 **两页签各自还有【内层】一套溢出** | `Item Drawer` 锚 (0,1)→(1,1)、`sizeDelta.x=18.6941`、`anchoredPosition.x=+9.34698` ⇒ 对 `Scroll Rect` 两侧各溢 **9.347** | 两表 `Item Drawer` 行。**这是与「宽 33」无关的第二套数，别混用** |

⚠️ **同名多份（必报）**：全包有 **2 个 `Tab Content`**（另一个 RT `-3896449222272450650`，子是
General/Media/Account/Support/Graphics，属**设置窗**）；**3 个 `Toggle borde`**；**9 个 `Avatar Item Small*`**；
**4 个 `Item Drawer`**。⇒ 本文件一律以**你给的 RT pid + GameObject 反查**锚定，**不按名字**。

---

## A1 `Avatar Tab`（pid `-4150415914498688462`）层 × 参数 —— 工具逐行原文

```
# MonoScript 类名索引：5175 条
# （已沿 `m_Father` 爬父链：被查节点的父 = 「Tab Content」 351.03,118.92 → 1746.97,962.00）
# sprite 索引：真包 3316 条 + 切片缓存补齐 ⇒ 共 3316 条
# sprite 尺寸/九宫格索引：3200 条
| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 锚点 min→max | pivot | anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | 贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | Avatar Tab | 318.37,118.92→1746.97,962.00 | 1428.59×843.08 | (0,0)→(1,1) | (0.5,0.5) | (-16.3294,0) | (32.6588,0) | **F** | AvatarTab |  |  |  | 字段: contentHolder,itemPrefab,mainDisplay,selectAvatarButton,toggleAvatarBorderIcon |
| ·1 | Selected Item Panel | 318.37,292.02→587.32,758.89 | 268.95×466.87 | (0,0.5)→(0,0.5) | (0,0.5) | (0,15) | (268.949,466.871) | T |  |  |  |  |  |
| ··2 | Avatar Menu Item | 318.37,360.15→585.45,633.73 | 267.08×273.58 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-0.934555,28.5158) | (267.08,273.577) | T | EverguildButton,AvatarDisplay,ItemDrawerComponents |  |  |  | trans=1 target=4779540784910268978 interactable=1 ; 字段: avatarHolder,avatarImage,avatarName,button,highlight,inspectSound,showItemInfoOnClick ; 字段: background,claimedWarning,conversionDisplay,convertedItem,convertedLabel,ephemeralDisplay,ephemeralText,image,label,premiumBadge,premiumHighlight,quantity |
| ···3 | Raycast Target | 362.46,380.60→541.37,595.04 | 178.90×214.44 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-9.91821e-05,9.1215) | (178.904,214.439) | T | Image,EverguildButtonMaterialModifier | <无图> | Simple (1,1,1,0) |  | 字段:  |
| ···3 | Image Container | 318.37,360.15→585.45,573.73 | 267.08×213.58 | (0,0)→(1,1) | (0.5,0.5) | (0,30) | (0,-60) | T |  |  |  |  |  |
| ····4 | Highlight | 318.37,355.60→588.79,571.03 | 270.42×215.43 | (0,0)→(1,1) | (0.5,0.5) | (1.6692,3.6273) | (3.3384,1.8547) | **F** | Image,EverguildButtonMaterialModifier | Player_Avatar_selected 256×256 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ····4 | Border | 318.37,381.51→585.45,595.09 | 267.08×213.58 | (0,-0.1)→(1,0.9) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier | Player Profile Border 256×286 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ····4 | Image | 318.37,357.45→585.45,571.03 | 267.08×213.58 | (0,0)→(1,1) | (0.5,0.5) | (0,2.7) | (0,0) | T | Image,EverguildButtonMaterialModifier | <无图> | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ···3 | Avatar Name | 303.37,253.13→600.45,321.03 | 297.08×67.90 | (0,0)→(1,0) | (0.5,1) | (0,380.6) | (30,67.9) | T | TextMeshProUGUI,EverguildButtonMaterialModifier,EverguildTextController |  |  | 'TEST NAME' 字号=35.0 auto[15.0~35.0] 对齐=Center/Middle 折行=0 色=(1,0.693,0.00784,1) | 字段:  ; 字段: maxFontSize,minFontSize |
| ··2 | Select Avatar Button | 343.49,670.43→562.21,736.48 | 218.73×66.05 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.52588e-05,-178) | (218.726,66.0515) | T | Image,EverguildButton,EverguildButtonMaterialModifier | UI_Button_Mulligan 410×124 九宫333,96,333,96 | Simple (1,1,1,1) |  | trans=2 target=5915135562796857906 interactable=1 ; 字段:  |
| ···3 | Button Text | 354.25,677.86→550.74,729.05 | 196.49×51.19 | (0.0355112,0.099)→(0.961262,0.903) | (0.5,0.5) | (0,-0.0606194) | (-6,-1.9192) | T | TextMeshProUGUI,AspectRatioFitter,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Selecionar' 字号=36.0 auto[10.0~36.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段: m_AspectMode,m_AspectRatio ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ··2 | Toggle borde | 343.49,771.43→562.21,837.48 | 218.73×66.05 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.52588e-05,-279) | (218.726,66.0515) | T | Image,EverguildButton,EverguildButtonMaterialModifier | UI_Button_Mulligan 410×124 九宫333,96,333,96 | Simple (1,1,1,1) |  | trans=2 target=841544596736735794 interactable=1 ; 字段:  |
| ···3 | Button Text | 354.25,778.86→550.74,830.05 | 196.49×51.19 | (0.0355112,0.099)→(0.961262,0.903) | (0.5,0.5) | (0,-0.0606194) | (-6,-1.9192) | T | TextMeshProUGUI,AspectRatioFitter,EverguildButtonMaterialModifier,EverguildTextController,Localize |  |  | 'Toggle Border' 字号=36.0 auto[10.0~36.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段: m_AspectMode,m_AspectRatio ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary |
| ·1 | Item Display Panel | 632.79,210.69→1701.49,868.61 | 1068.70×657.92 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (134.47,0.80423) | (1068.7,657.92) | T |  |  |  |  |  |
| ··2 | Select Item | 654.16,147.51→1166.61,210.70 | 512.45×63.18 | (0,0.5)→(0,0.5) | (0,1) | (21.374,392.14) | (512.448,63.1804) | T | TextMeshProUGUI,Localize,EverguildTextController |  |  | 'Select your avatar' 字号=35.0 auto[18.0~35.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: maxFontSize,minFontSize |
| ··2 | Background | 632.79,210.69→1701.49,868.61 | 1068.70×657.92 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  |  |
| ··2 | Scroll Rect | 654.16,210.69→1680.12,855.46 | 1025.95×644.76 | (0.02,0.02)→(0.98,0.98) | (0.5,0.5) | (0,6.57919) | (0,13.158) | T | ScrollRect,RectMask2D,Image | <无图> | Simple (1,1,1,0) |  | h=0 v=1 mode=1 inertia=1 elasticity=0.10000000149011612 decel=0.13500000536441803 ; 字段: m_Padding,m_Softness |
| ···3 | Item Drawer | 654.16,210.70→1698.81,430.70 | 1044.65×220.00 | (0,1)→(1,1) | (0.5,1) | (9.34698,-0.000427246) | (18.6941,220) | T | GridLayoutGroup,ContentSizeFitter ⚠️grid? |  |  |  | **【GridLayoutGroup】** cellSize=180×180 spacing={'x': 25.0, 'y': 50.0} pad=13,0,40,0 corner/axis=0/0 align=0 constraint=0(2) ; 字段: m_HorizontalFit,m_VerticalFit |
| ····4 | Avatar Item Small_Ref | 667.16,250.70→847.16,430.70 | 180.00×180.00 | (0,1)→(0,1) | (0.5,0.5) | (103,-130) | (180,180) | T | EverguildButton,AvatarDisplay,ItemDrawerComponents |  |  |  | trans=1 target=-6349672842827892174 interactable=1 ; 字段: avatarHolder,avatarImage,avatarName,button,highlight,inspectSound,showItemInfoOnClick ; 字段: background,claimedWarning,conversionDisplay,convertedItem,convertedLabel,ephemeralDisplay,ephemeralText,image,label,premiumBadge,premiumHighlight,quantity |
| ·····5 | Raycast Target | 667.71,224.35→846.62,438.79 | 178.90×214.44 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-9.91821e-05,9.1215) | (178.904,214.439) | T | Image,EverguildButtonMaterialModifier | <无图> | Simple (1,1,1,0) |  | 字段:  |
| ·····5 | Image Container | 667.16,250.70→847.16,393.33 | 180.00×142.63 | (0,0)→(1,1) | (0.5,0.5) | (0,18.6839) | (0,-37.3678) | T |  |  |  |  |  |
| ······6 | Highlight | 667.16,247.17→850.50,391.65 | 183.34×144.49 | (0,0)→(1,1) | (0.5,0.5) | (1.6692,2.6) | (3.3384,1.8547) | T | Image,EverguildButtonMaterialModifier | Player_Avatar_selected 256×256 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ······6 | Border | 667.16,264.96→847.16,407.59 | 180.00×142.63 | (0,-0.1)→(1,0.9) | (0.5,0.5) | (0,0) | (0,0) | T | Image,EverguildButtonMaterialModifier | Player Profile Border 256×286 否 | Simple (1,1,1,1) preserveAspect |  | 字段:  |
| ······6 | Image | 667.16,248.00→847.16,390.63 | 180.00×142.63 | (0,0)→(1,1) | (0.5,0.5) | (0,2.7) | (0,0) | T | Image,EverguildButtonMaterialModifier | <无图> | Simple (1,1,1,1) preserveAspect **m_Enabled=0** |  | 字段:  |
| ·····5 | Avatar Name | 667.16,430.70→847.16,472.01 | 180.00×41.31 | (0,0)→(1,0) | (0.5,1) | (0,0) | (7.62939e-06,41.3144) | **F** | TextMeshProUGUI,EverguildButtonMaterialModifier |  |  | 'Avatar name' 字号=36.0 auto[12.0~36.0] 对齐=Center/Middle 折行=1 色=(1,1,1,1) | 字段:  |

⚠️ 布局组（子节点位置**由布局算**，上面已是**布局跑之后**的值）：
          Item Drawer → GridLayoutGroup [grid?]
```

## A2 `Title Tab`（pid `6261725956636310066`）层 × 参数 —— 工具逐行原文

```
# MonoScript 类名索引：5175 条
# （已沿 `m_Father` 爬父链：被查节点的父 = 「Tab Content」 351.03,118.92 → 1746.97,962.00）
# sprite 索引：真包 3316 条 + 切片缓存补齐 ⇒ 共 3316 条
# sprite 尺寸/九宫格索引：3200 条
| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 锚点 min→max | pivot | anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | 贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | Title Tab | 318.37,118.92→1746.97,962.00 | 1428.59×843.08 | (0,0)→(1,1) | (0.5,0.5) | (-16.3293,0) | (32.6588,0) | T | TitleTab |  |  |  | 字段: contentHolder,mainDisplay |
| ·1 | Selected Item Panel | 318.37,292.02→587.32,758.89 | 268.95×466.87 | (0,0.5)→(0,0.5) | (0,0.5) | (0,15) | (268.949,466.871) | T | ProfileItemDisplay |  |  |  | 字段: image,selectButton,text |
| ··2 | Avatar Name | 322.54,516.41→587.32,631.55 | 264.78×115.14 | (0,0)→(1,0) | (0.5,1) | (2.08258,242.486) | (-4.17078,115.141) | T | TextMeshProUGUI,EverguildButtonMaterialModifier |  |  | 'Warrior of the Raging Winds' 字号=43.349998474121094 auto[15.0~50.0] 对齐=Center/Middle 折行=1 色=(1,0.693,0.00784,1) | 字段:  |
| ··2 | Select Avatar Button | 343.49,670.43→562.21,736.48 | 218.73×66.05 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.52588e-05,-178) | (218.726,66.0515) | T | Image,EverguildButton,EverguildButtonMaterialModifier | UI_Button_Mulligan 410×124 九宫333,96,333,96 | Simple (1,1,1,1) |  | trans=2 target=1165199501509884466 interactable=1 ; 字段:  |
| ···3 | Button Text | 354.25,676.90→550.74,730.01 | 196.49×53.11 | (0.0355112,0.099)→(0.961262,0.903) | (0.5,0.5) | (0,-0.0606194) | (-6,0) | T | TextMeshProUGUI,AspectRatioFitter,EverguildButtonMaterialModifier,EverguildTextController,EverguildButtonMaterialModifier |  |  | 'Selecionar' 字号=36.0 auto[10.0~36.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段: m_AspectMode,m_AspectRatio ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段:  |
| ··2 | Toggle borde | 343.49,771.43→562.21,837.48 | 218.73×66.05 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (-1.52588e-05,-279) | (218.726,66.0515) | **F** | Image,EverguildButton,EverguildButtonMaterialModifier | UI_Button_Mulligan 410×124 九宫333,96,333,96 | Simple (1,1,1,1) |  | trans=2 target=-5786353538390328782 interactable=1 ; 字段:  |
| ···3 | Button Text | 354.25,778.86→550.74,830.05 | 196.49×51.19 | (0.0355112,0.099)→(0.961262,0.903) | (0.5,0.5) | (0,-0.0606194) | (-6,-1.9192) | T | TextMeshProUGUI,AspectRatioFitter,EverguildButtonMaterialModifier,EverguildTextController,Localize |  |  | 'Toggle Border' 字号=36.0 auto[10.0~36.0] 对齐=Center/Capline 折行=0 色=(1,1,1,1) | 字段: m_AspectMode,m_AspectRatio ; 字段:  ; 字段: maxFontSize,minFontSize ; 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary |
| ··2 | Image | 344.32,295.19→565.54,516.41 | 221.21×221.21 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (2.0826,119.66) | (221.213,221.213) | T | Image | 40k_DeckSelection_icon_FactionUM 256×256 否 | Simple (1,1,1,1) |  |  |
| ·1 | Item Display Panel | 632.79,210.69→1701.49,868.61 | 1068.70×657.92 | (0.5,0.5)→(0.5,0.5) | (0.5,0.5) | (134.47,0.80423) | (1068.7,657.92) | T |  |  |  |  |  |
| ··2 | Select Item | 654.16,147.51→1166.61,210.70 | 512.45×63.18 | (0,0.5)→(0,0.5) | (0,1) | (21.374,392.14) | (512.448,63.1804) | T | TextMeshProUGUI,Localize,EverguildTextController |  |  | 'Select your title' 字号=35.0 auto[18.0~35.0] 对齐=Left/Middle 折行=1 色=(1,1,1,1) | 字段: AddSpacesToJoinedLanguages,AllowLocalizedParameters,AllowParameters,AlwaysForceLocalize,CorrectAlignmentForRTL,IgnoreNumbersInRTL,IgnoreRTL,LocalizeCallBack,LocalizeEvent,LocalizeOnAwake,MaxCharactersInRTL,PrimaryTermModifier,SecondaryTermModifier,TermPrefix,TermSuffix,TranslatedObjects,mGUI_ShowCallback,mGUI_ShowReferences,mGUI_ShowTems,mLocalizeTarget,mLocalizeTargetName,mTerm,mTermSecondary ; 字段: maxFontSize,minFontSize |
| ··2 | Background | 632.79,210.69→1701.49,868.61 | 1068.70×657.92 | (0,0)→(1,1) | (0.5,0.5) | (0,0) | (0,0) | T | Image | UI_Deck_Information_submenu_Back 69×63 九宫18,18,18,18 | Sliced (1,1,1,1) |  |  |
| ··2 | Scroll Rect | 654.16,210.69→1680.12,855.46 | 1025.95×644.76 | (0.02,0.02)→(0.98,0.98) | (0.5,0.5) | (0,6.57919) | (0,13.158) | T | ScrollRect,RectMask2D,Image | <无图> | Simple (1,1,1,0) |  | h=0 v=1 mode=1 inertia=1 elasticity=0.10000000149011612 decel=0.13500000536441803 ; 字段: m_Padding,m_Softness |
| ···3 | Item Drawer | 654.16,210.70→1698.81,210.70 | 1044.65×0.00 | (0,1)→(1,1) | (0.5,1) | (9.34698,-0.000427246) | (18.6941,0) | T | GridLayoutGroup,ContentSizeFitter |  |  |  | **【GridLayoutGroup】** cellSize=325.9×130 spacing={'x': 15.0, 'y': 50.0} pad=7,0,40,0 corner/axis=0/0 align=0 constraint=0(2) ; 字段: m_HorizontalFit,m_VerticalFit |
```

### A-补 1 `activeSelf` 的出现条件

出厂 `act=F` 的**只有 4 个节点**（其余全 T）：

| 节点 | 运行期由谁切 | 出处 |
|---|---|---|
| `Avatar Tab`（根） | 页签开/关：`WindowTabBase.TryOpenTab` → `SetActive(true)`；`WindowTabBase.CloseTab` → `SetActive(false)` | `WindowTabBase__TryOpenTab.c:9` · `WindowTabBase__CloseTab.c:9` |
| `Avatar Menu Item > Image Container > Highlight` | `AvatarDisplay.ToggleHighlight(bool)`（虚表 **0x1c8**，由 `AvatarDisplay.Initialize` 调用） | `AvatarDisplay__ToggleHighlight.c:19`（字段 `+0x70`=highlight）· `AvatarDisplay__Initialize.c:9` |
| `Avatar Item Small_Ref > Avatar Name` | **查不到激活点**（见 §C1）。可确证的是 `AvatarDisplay` 自己不切它（该类的 `SetActive` 只在 `ToggleHighlight` 一处） | `grep -l SetActive AvatarDisplay__*.c` 仅 1 命中 |
| `Title Tab > Selected Item Panel > Toggle borde`（GO `Toggle borde_-2155043435931731406`） | **查不到激活点**（见 §C2） | 该 GO 全组件 = Image / EverguildButton / EverguildButtonMaterialModifier，**无 Animation** |

其余 `act` 的切换点（逐条）：

1. **页签根**：`TabButtons.Toggle` → `GameWindowWithTabs.ChangeTab` → `ChangeTabCO` → 旧页 `CloseTab()` /
   新页虚表 `0x1a8`(= `TryOpenTab`)。出处：`TabButtons__Toggle.c:19` ·
   `GameWindowWithTabs__ChangeTabCO.c:27`（关旧）/`:51`（开新）· `GameWindowWithTabs__OpenTabs.c:37`。
   Tab 按钮的两条 lambda（`TabButtons.__c__DisplayClass6_0___Initialize_b__0.c` 与 `..._b__1.c`）
   **两文件方法体逐字相同**（反编译器去重），都转调 `TabButtons__Toggle`。
   `GameWindowWithTabs.CreateTab<T>` 只在建页时 `SetActive(true)`，但**本窗不走这条路**：
   `TabbedWindowComponents_-4937971811140019470.json` 的 **`tabPrefab.m_PathID = 0`** ⇒ 页签是预制体里
   **预置的子节点**，`m_IsActive` 就是初值（Avatar F / Title T）。
   `GameWindowWithTabs__ForceCloseTabs.c` 关窗时对当前页 `CloseTab()`，并对 `tabs` 列表逐项 `SetActive(false)`。
   `PlayerProfileMenu.Open()` 只关 `Components.tabButtons`，**不切页签**。
2. **`Highlight`（大图 `Avatar Menu Item`）实际不会亮**：两个调用点都写死 `highlight=false` ——
   `AvatarTab__RefreshDisplays.c:47` 传 `0,0`；`AvatarTab__OnAvatarClick.c:38` 第 4 参 `0`。
   `grep AvatarDisplay__ToggleHighlight` 的调用点只有 `AvatarDisplay__Initialize.c:9` 与
   `AvatarDisplay__Draw.c:6`（后者也传 0）⇒ **除非别处直调，它不亮**。
3. **格子里的 `Highlight`**（Avatar Tab 预制体那 1 个模板里是 T）：由 `AvatarTab.InitializeGameObjects`
   逐格调 `0x1c8`=`ToggleHighlight(selected)`（`AvatarTab__InitializeGameObjects.c:196`）；
   `AvatarTab__OnAvatarClick.c:25/28` 负责「取消旧选 + 选新」。Title 侧同构：
   `TitleTab__Initialize.c:244` · `TitleTab__OnItemClick.c:22/25`。
4. **锁定态**用虚表 **0x208**（= `ToggleLock`）：Avatar 侧它调 `EverguildButton.SoftDisable`
   （**不改 activeSelf**，`AvatarDisplay__ToggleLock.c`）；Title 侧 `TitleDrawerHorizontal.ToggleLock`
   **才** `SetActive`（字段 `+0x98`）⇒ **同一个「Lock」语义在两个页签里落点不同**。
5. `AvatarTab.InitializeGameObjects` 进 `OnOpen` 第一件事就是 `itemPrefab.gameObject.SetActive(false)`
   （`:60`），随后逐格 `Instantiate` 出克隆并 `SetActive(true)`（`:206`）
   ⇒ **预制体里那个 act=T 的 `Avatar Item Small_Ref` 是模板本体，运行期被关掉**。
6. `AvatarTab.Awake` 与 `CheckEnableAvatarBorder` 都会按 `FindActiveAvatarBorder() != null`
   切 `toggleAvatarBorderIcon.gameObject`（`AvatarTab__Awake.c:39` · `AvatarTab__CheckEnableAvatarBorder.c:21`）。

### A-补 2 图参数（ppu / offset / 九宫格）

工具**不打印 ppu**（`menu_dump.py` 只在 `m_PixelsPerUnitMultiplier != 1` 时打 `ppuMul`），故另查 Sprite JSON：

| sprite | 用在 | m_PixelsToUnits | m_Offset | m_Border | 贴图模式 |
|---|---|---|---|---|---|
| `Player_Avatar_selected` 256×256 | 两处 `Highlight` | 100 | (0,0) | (0,0,0,0) | Simple + preserveAspect |
| `Player Profile Border` 256×286 | 两处 `Border` | 100 | (0,0) | (0,0,0,0) | Simple + preserveAspect |
| `UI_Button_Mulligan` 410×124 | `Select Avatar Button`、`Toggle borde`（两页签共 4 处） | 100 | (0,0) | **333,96,333,96** | **Simple** |
| `UI_Deck_Information_submenu_Back` 69×63 | 两页签 `Background` | 100 | (0,0) | **18,18,18,18** | Sliced |
| `40k_DeckSelection_icon_FactionUM` 256×256 | Title Tab `Image` | 100 | (0,0) | (0,0,0,0) | Simple |

- 出处：`bundle_cosmeticavatarsimages_assets_all/Sprite/{Player_Avatar_selected,Player Profile Border}.json` ·
  `bundle_duplicateassetisolation_assets_all/Sprite/UI_Button_Mulligan.json` ·
  `bundle_atlasindividual_assets_0_mainmenu/Sprite/UI_Deck_Information_submenu_Back.json` ·
  `bundle_armyicons_assets_all/Sprite/40k_DeckSelection_icon_FactionUM.json`。
- **5 张全部 ppu=100、offset=(0,0)** ⇒ 没有 ppu≠100 的图。
- **九宫格 ≠ 全 0 的只有 2 张**：`UI_Button_Mulligan(333,96,333,96)`、`UI_Deck_Information_submenu_Back(18,18,18,18)`。
- ⚠️ `UI_Button_Mulligan` **虽有九宫格，但 4 处 Image 的 `m_Type` 都是 `Simple` ⇒ 九宫格不生效**
  （照原样抄，**不要**自作主张改成 Sliced）。`Background` 是 **Sliced** ⇒ 69×63 的图被拉到 1068.70×657.92，
  四边各 18px 不拉伸。
- 两表无 `ppuMul=` ⇒ **全为 1**（判据：`menu_dump.py` 只在 ≠1 时打印）。
- 文字两处同名不同参：`Avatar Menu Item > Avatar Name` 字号 35 auto[15~35]；
  Title Tab `Selected Item Panel > Avatar Name` 字号 **43.35** auto[15~50]；颜色同为 `(1,0.693,0.00784,1)`。
  `Select Item` 标题两页签同为 35 auto[18~35]，只文案不同（`Select your avatar` / `Select your title`）。
- 两页签同名节点的 Image **不是同一份实例**：`Select Avatar Button` 的 target 分别是
  `5915135562796857906`(Avatar) / `1165199501509884466`(Title)；`Toggle borde` 是
  `841544596736735794` / `-5786353538390328782` ⇒ **改一处不会连带改另一处**。
- 工具**不打印** `m_RaycastTarget` 与 `m_Enabled`（都在 `SKIP_KEYS`）⇒ **不对「能不能点」下结论**。
  能说的只有：两处 `Raycast Target` 节点**无 sprite**、Image 颜色 a=0（表内 `(1,1,1,0)`）。

### A-补 3 两个 `contentHolder` 的格子怎么排 / 一屏几个 / 格子多大

**字段落点（MonoBehaviour 反查 GameObject，不是按名字猜）**

| 字段 | 组件 pid | 指向 GameObject |
|---|---|---|
| `AvatarTab.contentHolder` | RT `-3096826980488349134` | `Item Drawer` |
| `AvatarTab.itemPrefab` | AvatarDisplay 组件 `880497312993081906` | `Avatar Item Small_Ref`（RT `-1677058983019185614`，180×180） |
| `AvatarTab.mainDisplay` | `8761896063462177330` | `Avatar Menu Item_5415412471778343474` |
| `AvatarTab.selectAvatarButton` | `-1358823701211940302` | `Select Avatar Button` |
| `AvatarTab.toggleAvatarBorderIcon` | `-9096026474060481998` | `Toggle borde`（⚠️ **原名拼写就是 `borde`**，不是笔误） |
| `TitleTab.contentHolder` | RT `197494230510959154` | `Item Drawer_4972767040676198962` |
| `TitleTab.mainDisplay` | `3430062419348060722` | `Selected Item Panel_-3830409081675285966`（`ProfileItemDisplay.selectButton=-1170228847511438798 → Select Avatar Button`、`.image=-8642638617522570702 → Image`、`.text=-5489547645705160142 → Avatar Name`） |
| 出处 | `MonoBehaviour/MonoBehaviour_{7708307686690355762, -8260100404985038286, 3430062419348060722}.json` | |

**格子是布局组排的（非手摆）**

| | Avatar Tab | Title Tab |
|---|---|---|
| 布局组组件 pid | `-2486840606177199566` | `9039161019885910578` |
| `m_CellSize` | **180 × 180** | **325.9 × 130** |
| `m_Spacing` | (25, 50) | (15, 50) |
| `m_Padding` | L13 R0 T40 B0 | L7 R0 T40 B0 |
| `m_ChildAlignment` / `m_StartCorner` / `m_StartAxis` | 0 / 0 / 0（UpperLeft 起，水平优先） | 同 |
| `m_Constraint` / `m_ConstraintCount` | 0（Flexible）/ 2（**Flexible 下不生效**） | 同 |
| `ContentSizeFitter` | `m_HorizontalFit=0`(Unconstrained) · `m_VerticalFit=1`(PreferredSize)，组件 `7676054268895197746` | 同，组件 `5050004126812699186` |

**格数换算**（列数按 uGUI `GridLayoutGroup` 柔性约束：`floor((usable + spacing.x) / (cell.x + spacing.x))`）：

- Avatar：usable = 1044.65 − 13 − 0 = 1031.65 ⇒ `floor(1056.65 / 205)` = **5 列**；
  首格实测左上 = (654.16+13, 210.70+40) = **(667.16, 250.70)**，与工具表 `Avatar Item Small_Ref` 行**逐位一致** ✔
- Title：usable = 1044.65 − 7 = 1037.65 ⇒ `floor(1052.65 / 340.9)` = **3 列**。

**一屏几个**（视口 = `Scroll Rect` 的 rect，高 644.76，行距 = cell.y + spacing.y）：

- Avatar：行距 230 ⇒ 行 0 (40~220)、行 1 (270~450) **完整**，行 2 (500~680) 只露 144.76/180
  ⇒ **10 个完整 + 5 个半露**。
- Title：行距 180 ⇒ 行 0/1/2 (40~170 / 220~350 / 400~530) **完整**，行 3 (580~710) 只露 64.76/130
  ⇒ **9 个完整 + 3 个半露**。

**模板实例在哪**：

- **Avatar Tab** —— 预制体里**有 1 个实例**，就是模板本体 `Avatar Item Small_Ref`（180×180），
  它同时是 `itemPrefab`。运行期在 `OnOpen` 被 `SetActive(false)`（`AvatarTab__InitializeGameObjects.c:60`），
  再按道具数量 `Instantiate(itemPrefab, contentHolder)` 克隆（`:152`）+ `SetActive(true)`（`:206`）。
- **Title Tab** —— **预制体里 0 个实例，只有 `contentHolder` 引用**。判据三连：
  ① 工具表 `Item Drawer` 下**无任何子行**；② 其 RT `sizeDelta.y = 0.00`（`ContentSizeFitter` 在 0 子节点下的结果）；
  ③ `RectTransform_197494230510959154.json` 的 `m_Children` 为空。
  **⇒ 格子尺寸只能取网格参数 325.9×130；实例尺寸在原版里不存在，不编。**
- Title 侧生成代码：`SupportMethods.DestroyAllChildren(contentHolder)`（`TitleTab__Initialize.c:74`）→
  逐项 `ItemDrawer.Draw(contentHolder, item, 1, 15)`（`:195`，第 4 参 `0xf=15` 是 `DrawerOverride` 枚举值）→
  `LayoutRebuilder.ForceRebuildLayoutImmediate`（`:289`）→ 逐格 `SetActive(false)` 后 `SetActive(true)`
  （`:310`/`:316`，同一对象，用于强刷）。
- Avatar 侧：`Linq.Where×2 → OrderBy(0x188) → ThenBy → ToList`（`AvatarTab__InitializeGameObjects.c:64` 起），
  命中数量变化时才重建数组。

---

## B1 `AvatarTab` 三清单

### B1-a 入口

- 🔴 **按名字 grep 不到任何外部调用者**：`grep -rl "AvatarTab" decomp_full | grep -v "^./AvatarTab"` = **0 命中**。
  判据：反编译器把类型写成静态类指针 `DAT_xxxx`，**类名不落调用点**。
  对照实验：`ProfileItemDisplay` 只在作**类型**出现时被写进别人文件（`TitleTab__Awake.c:6` 等 6 处），
  而 `AvatarTab` **从不作类型出现在别人文件里** ⇒ **不能据此断言「没人调它」**。
- 实际入口只有一条链（页签按钮 → 开页）：
  1. **预置 + 注册**：`PlayerProfileMenu.tabs[1] = 7708307686690355762`（= AvatarTab 组件 pid）、
     `tabs[2] = -8260100404985038286`（TitleTab）—— 出处 `MonoBehaviour_-4653119414410905038.json`
     （`tabs` 数组 6 项，顺序 = `Tab Content` 的 6 子节点顺序）。
  2. `GameWindowWithTabs.SetupTabs` 遍历 `tabs` → 按 `GetType()` 存进 `cachedTabs`(`+0x80`) →
     调 `WindowTabBase.Setup`（`GameWindowWithTabs__SetupTabs.c:78`）。
  3. **开页**：Tab 按钮 → `TabButtons.Toggle`（`TabButtons__Toggle.c:19`）→ `GameWindowWithTabs.ChangeTab`
     → `ChangeTabCO`（`:51`）→ 页签虚表 `0x1a8` = `TryOpenTab`（`SetActive(true)`）→ `AvatarTab.OnOpen`
     → `InitializeGameObjects` + `RefreshDisplays`（`AvatarTab__OnOpen.c:5-6`）。
  4. **首次进窗**：`GameWindowWithTabs.OpenTabs`（`:37`）取 `GetStartingTab()` ——
     `PlayerProfileMenu` **没有**该方法的覆盖产物 ⇒ 用基类实现，**起点页查不到**（见 §C6）。
  5. **关页**：`WindowTabBase.CloseTab`（`:9` `SetActive(false)`）。
- ⚠️ 疑似入口但**无法定名**：`ProfileTab.<Start>b__9_0` 与 `RankedTab.<Initialize>b__10_0` 都调
  `GameWindowWithTabs.ChangeTab<T>(DAT_1842d1918)`；该 DAT 全目录**只出现这 2 处**，类名不可得（见 §C4）。
  **不能断言它 = AvatarTab 或 TitleTab。**

### B1-b `AvatarTab` 自己调了谁（分 17 个方法体）

| 方法 | 调用的外部方法（归类） |
|---|---|
| `Awake` | `EverguildButton.onClick(+0x100).AddListener` ×2（`:20/:26`）；`UIGenericEventCatcher.SourceDelegate..ctor`；`FindActiveAvatarBorder` → `GameObject.SetActive`（`:39`） |
| `OnDestroy` | `UnityEventBase.RemoveAllListeners` ×2（同一个 onClick 重复解绑） |
| `OnOpen` | `InitializeGameObjects` + `RefreshDisplays`（`:5-6`） |
| `InitializeGameObjects` | 数据：`PlayerAvatarDataManager.get_PlayerAvatarID`（`:54`）· `.GetAllItems<T>`（`:64`）；Linq：`Where`×2 / `OrderBy(0x188)` / `ThenBy` / `ToList`；对象：`Object.Instantiate(itemPrefab, contentHolder)`（`:152`）· `Object.Destroy` · `GameObject.SetActive`（`:60/:206`）；表现：`AvatarDisplay.ChangeAvatar` / `ChangeBorder` / `ToggleHighlight(0x1c8)`（`:196`）/ `ToggleLock(0x208)`（`:197`）/ `add_OnClick` |
| `RefreshDisplays` | `PlayerAvatarDataManager.get_{PlayerAvatarItem,PlayerAvatarBorder,DefaultAvatarItem}`；`AvatarDisplay.Initialize`（`:47`）；`Linq.First`；→ `OnAvatarClick` |
| `OnAvatarClick` | `AvatarDisplay.Initialize`（`:38`）· `ToggleHighlight(0x1c8)`（`:25/:28`）· `IsAvatarLocked` · `ToggleSelectButton` |
| `SetAvatar` | `Selectable.set_interactable`（`:21`）；`PlayerAvatarDataManager.SetCurrentItem(item, callback)` |
| `ToggleSelectButton` | `Selectable.set_interactable`（`:50`）；`PlayerAvatarDataManager.get_PlayerAvatarID`；TMP `set_text`（虚表 0x558）；`LocalizationManager.GetTranslation`（3 个 key，与 Title 侧 `ProfileItemDisplay.ToggleButton` **同一组**） |
| `OnChoseAvatar` | `AvatarDisplay.get_Avatar(+0x80)` · `DoShine` · `ToggleLock(0x298)`；`Selectable.set_interactable`；`PlayerAvatarDataManager.SetCurrentItem` |
| `CheckEnableAvatarBorder` | `FindActiveAvatarBorder` → `GameObject.SetActive`（`:21`）。**零调用点**（见 §C3） |
| `FindActiveAvatarBorder` | `SingletonBehaviour<InventoryManager>.Instance` → `GetInventory<T>` → Linq 遍历 → `CosmeticItemAvatarBorder.IsParentEventActive` |
| `ToggleAvatarBorder` | `FindActiveAvatarBorder`；`PlayerAvatarDataManager.IsDefaultBorderEquipped` / `get_DefaultAvatarBorderItem` / `SetCurrentItem` |
| `IsAvatarLocked` | `PlayerAvatarDataManager.get_PlayerAvatarID`；`String.op_Inequality`；实体 `IsUnlocked`(虚表 0x298) |
| `DoBorderRefresh` | `GameObject.get_activeInHierarchy` → `RefreshDisplays` |

### B1-c 谁监听它 / 它监听谁 / 写哪些静态量 / 谁 Poll

- **它监听**：① `selectAvatarButton.onClick` → `OnChoseAvatar`（`Awake.c:20`）；
  ② `toggleAvatarBorderIcon.onClick` → lambda（`Awake.c:26`，产物 `AvatarTab___ToggleAvatarBorder_b__20_0.c`，
  **方法体与 `AvatarTab__DoBorderRefresh.c` 逐字相同** = 反编译器去重）；
  ③ 每一格的 `AvatarDisplay.OnClick` → `OnAvatarClick`；
  ④ `PlayerAvatarDataManager.SetCurrentItem` 的完成回调 → `ToggleSelectButton`。
- **它触发什么**：`PlayerAvatarDataManager.SetCurrentItem`（`SetAvatar`/`OnChoseAvatar`/`ToggleAvatarBorder` 三处）。
  该管理器有 `OnAvatarChanged` / `OnAvatarBorderChanged` 两个事件，**订阅者是别人**：
  `PlayerProfileUIController__Initialize.c:112/118` · `AlliancesManager__Start.c:18` —— 都不是这两个页签。
- **写哪些静态量**：**无游戏状态静态量**（`AvatarTab__*.c` 里的 `DAT_18453b03x` 全是一次性初始化标志）。
- **谁 Poll 它**：**没有**。没有 `Update`/`LateUpdate`/协程产物，`RefreshDisplays` 的调用者只有
  `OnOpen` / `DoBorderRefresh` / lambda。

## B2 `TitleTab` 三清单

### B2-a 入口

同 B1-a 的链条（`tabs[2] = -8260100404985038286`）。`grep -rl "TitleTab" | grep -v "^./TitleTab"` = **0 命中**
（同 B1-a 的判据）。`TitleTab.OnOpen` → `TitleTab.Initialize`（`TitleTab__OnOpen.c:5`）。

### B2-b `TitleTab` 自己调了谁（8 个方法体）

| 方法 | 调用的外部方法 |
|---|---|
| `Awake` | `ProfileItemDisplay.get_OnSelect()` → `UnityEvent.AddListener`（`:14-17`） |
| `OnDestroy` | `ProfileItemDisplay.get_OnSelect()` → `RemoveAllListeners`。⚠️ **只对 `mainDisplay` 解绑这一条，drawer 上的 `OnClick` 未解绑** |
| `OnOpen` | `Initialize`（`:5`） |
| `Initialize` | `PlayerDataManager.get_PlayerTitle`（`:68`）；`SingletonBehaviour<T>.Instance` → 成员 `+0x38` → `Linq.OfType<T>`（`:82`）；`SupportMethods.DestroyAllChildren(contentHolder)`（`:74`）；Linq `Where×2 / OrderBy / ToList`；`ItemDrawer.Draw(contentHolder, item, 1, 15)`（`:195`）；drawer 虚表 `0x1f8` / `0x208` / `0x1c8`；`List<ItemDrawer>.Add`；`TitleDrawerHorizontal.add_OnClick`；`LayoutRebuilder.ForceRebuildLayoutImmediate`（`:289`）；`GameObject.SetActive`（`:310`/`:316`）；`ProfileItemDisplay.Initialize`（末尾） |
| `OnItemClick` | 旧选 `ToggleHighlight(false)`（`:22`）、新选 `ToggleHighlight(true)`（`:25`）；`ProfileItemDisplay.Initialize` / `.ToggleButton` |
| `SetTitle` | 写 `PlayerDataManager` 实例 `+0x250 → +0x30`；`PlayerDataManager.UploadOnlineDemoData`（`:29`）；`ProfileItemDisplay.ToggleButton` |
| `OnSelect` | `ProfileItemDisplay.set_Interactable(false)`；`.GetItem<T>()`；写 title 字段 + `UploadOnlineDemoData`（`:38`）；`.ToggleButton` |

### B2-c 谁监听它 / 它监听谁 / 写哪些静态量 / 谁 Poll

- **它监听**：① `mainDisplay.OnSelect`（`ProfileItemDisplay` 的 `UnityEvent`）→ `TitleTab.OnSelect`；
  ② 每个 drawer 的 `TitleDrawerHorizontal.OnClick` → `OnItemClick`。
- **它触发什么**：`PlayerDataManager` 实例的标题字段（`+0x250 + 0x30`）+ `UploadOnlineDemoData`。
  之后**读该字段的其它系统** = 它的下游：`ProfileItemDisplay__ToggleButton.c:33` ·
  `ProfileNameTitleSection__Initialize.c:60/70` · `LocalPlayerInfo..ctor.c:59` ·
  `PlayerDataManager__CreatePlayerBattleData.c:74`。**没有 `OnTitleChanged` 事件**
  （`PlayerDataManager` 只有 `OnPlayerNameChanged`，订阅者 `PlayerProfileUIController__Initialize.c:127`）。
- **写哪些静态量**：**无**（ctor 只 `new List<ItemDrawer>()` 存进 `+0x48`）。
- **谁 Poll 它**：**没有**。

---

## C 查不到的（每条写「查什么 + 搜过哪些词 + 搜过哪些目录」）

1. **`Avatar Item Small_Ref > Avatar Name`（act=F）的激活点**。搜词：`ItemDrawerOptions__get_ShowName`（0 命中）·
   `ItemDrawer__Draw` · `SetItemName` · `avatarName` · `ItemDrawer*` 前缀文件名。搜目录：`decomp_full/`（25096 文件全量 grep）·
   `Warpforge_code/Scripts/Assembly-CSharp/`。判据：`ItemDrawer<T>` 的密封 `Draw(ObtainableItem,int,ItemDrawerOptions)`
   **没有产物文件**（`ls | grep -i "^ItemDrawer"` 只有 37 个，全是非泛型成员与 `ItemDrawerComponents`/`ItemDrawerConfig`），
   `ItemDrawerConfig.GetDrawingOptions` 只回传 options 对象 ⇒ **「预制体里 F 就一直是 F」这个结论下不了**
   （`Options.ShowName` 通路极可能就写在这里）。可确证的只有：`AvatarDisplay` 自己不切它。
2. **Title Tab 的 `Toggle borde` 激活点**。搜词：`Toggle borde` · `-6725776329459926478`（EverguildButton 组件 pid）·
   `toggleAvatarBorderIcon`。搜目录：`bundle_menus_assets_all/{GameObject,MonoBehaviour}` **全量反查**
   （35,014 个 MonoBehaviour JSON）⇒ 该组件的**唯一**引用者是它自己的 GameObject 文件；
   `decomp_full` 里 `TitleTab` 的 8 个方法体 + 桩 `TitleTab.cs` 只有 `contentHolder` / `mainDisplay` 两字段。
   旁证：该 GO 全组件 = Image + EverguildButton + EverguildButtonMaterialModifier，**无 Animation**；
   本包 19 个 legacy `Animation` 组件全部挂在 Skull / Cardback / Booster Pack / Ranked Division Change Window，
   **没有一个在这两个页签子树里**。⇒ **在 Title Tab 内它是死节点（出厂 F 且无激活点）**；
   保留项：不排除父级 Animator（未查）。
3. **`AvatarTab.CheckEnableAvatarBorder` 的调用点**（0 命中）。搜词：`AvatarTab__CheckEnableAvatarBorder`。
   目录：`decomp_full/` 全量。判据：`AvatarTab` 有 12 个 lambda 产物，**没有**一个是它的壳；
   它和 `Awake` 的那段实现逐句等价 ⇒ 属 **产物缺失** 而非「没写过」。
4. **`DAT_1842d1918` 到底是哪个页签类**（`ProfileTab___Start_b__9_0.c:15` · `RankedTab___Initialize_b__10_0.c:11`）。
   搜词：`DAT_1842d1918`（只这 2 文件 3 处）· `ChangeTab<`（24 处调用点，其余都是别的窗）。目录：`decomp_full/`。
   判据：`DAT_xxx` 是 `Il2CppClass*` 静态，反编译器不落类名。**不猜。**
5. **Title 网格格子的预制体具体是哪一份**（哪些 `Title Drawer Horizontal Variant*`）。
   搜词：`ItemDrawerConfig__GetReference` / `GetDrawingOptions` / `ItemDrawer__GetDrawerConfig`
   （后者走 `AssetLocator.GetUniqueAsset` + `Addressables.ComponentReference.Load`，**配置是资产不是代码**）。
   目录：`decomp_full/` · `bundle_menus_assets_all/GameObject`。旁证：同族 GO 的 RT `sizeDelta` 至少两套
   （315.408×111.009、245.255×101.458），**都与格子 325.9×130 不等**（格子尺寸被 `GridLayoutGroup` 覆盖）。**不编。**
6. **本窗的起始页（`GetStartingTab`）**。`PlayerProfileMenu` 无该方法的覆盖产物 ⇒
   无法判定首次进窗显示哪一页（页面初值：Avatar F、Title T）。**我们按「唯一 active 的页签根 = Title」实现**
   （正本 §2·1 的口径）。
7. **`extraScaleSmallScreen = 1.075` 如何作用到这两个页签**。搜词：`extraScaleSmallScreen`（decomp 全量 **0 命中**，
   判据：il2cpp 按偏移寻址，字段名不落码）。相关类：`TransformScalerBySmallScreenUI__*` ·
   `GameStaticData__get_DefaultSmallScreenUI`。⇒ **「只有一套坐标」下不了结论**；但可确证：
   尺寸换算的 1920×1080 基准是唯一的，且**两个页签本身都不带 `TransformScalerBySmallScreenUI`**
   （两个根 GO 全组件 = RT + 脚本，各 2 个）。
8. **两页签 `Select Avatar Button` 的三个文案 key 的原文**（与 Avatar 页共用同一组）。本地化表不在本次范围，未查。
9. **`TitleTab.Initialize` 里 drawer 虚表 `0x1f8`（`:224`，传参 0）对应哪个抽象方法**。可定名的是
   0x1c8=`ToggleHighlight`、0x208=`ToggleLock`（由 `AvatarDisplay__Initialize.c:9/11` 的两处同形态调用 +
   `AvatarDisplay.Initialize(avatar,border,highlight,isLocked)` 参数序锁定）。0x1f8 夹在中间、签名同形，
   候选：`ToggleShowInfoOnClick` / `ToggleClaimedWarning` / `SetEphemeralDisplay`。**未定名，不冒充。**
10. **工具本身不输出的列**：`m_RaycastTarget` · `m_Enabled` · `m_PixelsToUnits`（都在 `SKIP_KEYS`/分支里）。
    ppu 已另查 Sprite JSON 补齐；`m_RaycastTarget` **未查、不下结论**。
    工具对 `GridLayoutGroup` 走的是 `'grid?'` 早退分支 ⇒ **那几行的子节点坐标本就不该当「布局后」值**；
    本文件的格子尺寸/列数是按 `m_CellSize`/`m_Spacing`/`m_Padding` 手算的，并用首格实测位 (667.16,250.70) 对过。
11. **`Avatar Item Small_Ref` 有 2 份、`Avatar Item Small*` 有 9 份**：本次已用 RT pid 锁定 Avatar Tab 里那一份
    （RT `-1677058983019185614`）；另一份（RT `-656636088478491783`，sizeDelta 0×0）属别的窗，未追。

---

## D 三条给施工的提醒（逐条都有实据）

1. **`Toggle borde` 在 Title Tab 里是死节点**（出厂 F、全包无引用、无 Animation）；
   **在 Avatar Tab 里是活的**（`AvatarTab` 序列化字段 + 运行期 `SetActive`）
   ⇒ **别因为名字一样就当成同一份**。
2. **Avatar 的 `Highlight` 大图实际不亮**：两个调用点都写死 `highlight=false`。
3. **两个页签各有【两套】溢出数**：根 +32.66（两侧 16.33）与 `Item Drawer` +18.69（两侧 9.35）
   —— **别合并成一个数**。
