# R · 界面线逐屏审查 A —— 主菜单外壳 / 收藏窗 / 卡组编辑窗 / 相关弹窗

> 建立 **2026-10-18**（只读审查代理 A）。**只读、未改任何代码/资料**。
> 范围：主菜单（顶栏 · 左竖导航 · 模式卡 · 聊天预览）· 收藏窗四页 · 卡组编辑窗 · `Deck info` / `Deck Selection` / `Import Deck` / `Card Detail` 四扇弹窗。
> 判据来源：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 与 `bundle_scenes_scenes_mainmenuwarpforge/`（本次**现读**的用 `menu_dump.py` 复跑，命令见各条）· 反编译 `d:/2/tools/decomp_full/` · 施工图 `阶段二_*.md`（只当索引，逐条回 prefab 现核）。
> 参考实拍：`C:/Users/qjh36/Desktop/warpforge参考图/`（只当**验收尺子**，⛔ 不当参数源）。
> 现行口径：**只做不验**（未跑 Unity / 未起 player / 未跑 `*.Run`）。

---

## 一、差异表

列的读法：**原版值(出处)** 括号里是 prefab 路径/pid 或 `文件:行号`；**我们值(出处)** 是 `CardPresentation/**.cs:行号`。
结论四档：`一致` / `偏差` / `缺失` / `判不出`。严重度：高 = 肉眼可见且与原版判据冲突；中 = 可见但影响局部；低 = 细节/文档。

### 1·1 主菜单（外壳）

| # | 界面 | 检查点 | 原版值(出处) | 我们值(出处) | 结论 | 严重度 |
|---|---|---|---|---|---|---|
| M01 | 主菜单 | `Upper bar` 整条带子 | 1920×100 · a(0,0)-(1,1) pos(0,490) sz(−2.96e−5,−980)（`RT/1092`） | `New(parent,"Upper bar",1920f,100f)`（`Shell/TopBar.cs:186`） | 一致 | — |
| M02 | 主菜单 | 顶栏底图 `UI_Main_Upper bar` | x −11.7..1920 · y 0..71.3（高 **71.3**，不是 100）（`RT/1101`） | `Rect(bar,"UI_Main_Upper_bar",−11.7f,1920f,0f,71.3f,…)`（`TopBar.cs:189`） | 一致 | — |
| M03 | 主菜单 | 齿轮 `SettingsBtn` 框 + `m_RaycastPadding` | **1803.1..1890.9 × 4.6..66.4**（87.78×61.73）· pad (−4.38,−18.10,−9.70,−17.86)（`RT/1560` + `MB/2526`） | `GearBox`/`GearPad`（`TopBar.cs:139-140`）→ `Wire(...)`（`:255`） | 一致 | — |
| M04 | 主菜单 | 信箱 `InboxBtn` / 挑战 `Challenge button` / 反馈 `Feedback Button` | 425.3..480.3 × 15.5..55.5 · 490.1..537.6 × 11.8..59.2 · 565.44..615.44 × 10.5..60.5（`RT/1561,1391,1562`）；**Feedback prefab `m_IsActive=false`**（`GameObject/Feedback Button.json`）；挑战运行期按服务端挑战数关掉（`ChallengeButton__Initialize.c`） | 三格按原版建（`TopBar.cs:141-146,202-244`）；挑战 `SetActive(false)`（`:227`）· 反馈 `SetActive(false)`（`:244`） | 一致（本地无挑战源 ⇒ 恒关，且点了**出声**，`:327`） | — |
| M05 | 主菜单 | 玩家档案块 `Player Profile` + 玩家框 + 玩家名 | 0..528.7 × 0..211.1（`RT/1125`）· 框 23..411 × 11.6..135.6（`RT/1104`）· 名字框 136.9..401.9 × 13.7..61.7 fs32 auto[10,32] base36（`RT/1091` + `MB/1717`） | `TopBar.cs:347`（528.65×211.07）· `:349`（23..411 × 11.6..135.6）· `:367-374`（同框 + `SetAutoFitBox(265/108,48/108,10,32,36)`） | 一致 | — |
| M06 | 主菜单 | `Background Mask` / `Profile border` / `Profile content` | prefab `m_IsActive = 0`（`RT/1116`；`RT/1094`/`RT/1084` 亦 F）· 运行期 dump 5 份全 False | **不建**（`TopBar.cs:351-364`，四条判据写在注释里） | 一致（原版不画） | — |
| M07 | 主菜单 | 等级圆底图 `Player Level/Icon` | `UI_Button_Round_background`（pid 2381704724431365035）· 117.5..170.6 × 54.3..107.4（`MB/2359` + `RT/1279`） | `ArtRoundBtn`（`TopBar.cs:158,416`） | 一致 | — |
| M08 | 主菜单 | 等级数字 | prefab 空态串 **`-`**，运行期由 `PlayerProfileUIController.SetPlayerLevel` 写（服务端 `PlayerDataManager.CurrentLevel`） | 画 `-`（`TopBar.cs:418`），**不编数字** | 偏差（对实拍：实拍是 `28`）—— **判据在服务端**，见 §三·5 | 低 |
| M09 | 主菜单 | 资源条 `Resources Bar` 位置 / 建表序 / 常显颗数 | 1131.5..1802.5 × −0.1..71.1（`RT/1187`）· 建表序 = `campaignPoints·gachaTickets·raidMedals·crystals·blackStones·gold`（`GeneralMenuController__Initialize.c:51-146`）· 恒显 3 颗（`ResourcesBarController__.ctor.c:24-75`） | `ResBarL/R/T`（`TopBar.cs:482-483`）· `MainMenuRuntime.CounterSpecs`（建表序）+ `ShouldShow`（`TopBar.cs:519-528`） | 一致 | — |
| M10 | 主菜单 | 资源格几何 / 图标位置 | 格高 38.066（`RT/359`）· 药丸 141.51×38（`RT/372`）· 图标 60×59 @ `apos(−8.5,0)` + `LayoutElement.m_IgnoreLayout=1`（`RT/371`/`MB/545`） | `ItemH 38.066` / `IconW/H 60/59` / `IconOffsetL −8.5`（`TopBar.cs:485-507`），格宽 = 20+药丸+1（`:562`） | 一致 | — |
| M11 | 主菜单 | 资源条图标**画在药丸之上** | 原版兄弟序 `Resource Bar Background → Icon` | 图标用 `QBarBadge 2988`（`TopBar.cs:604-609`） | 一致（2026-10-18 N57 修） | — |
| M12 | 主菜单 | 缺 4 张币种小图标 | 原版 6 币种各有图标 | 本机只导了 2 张 ⇒ 其余**只画药丸 + 数字**，末尾 `LogWarning`（`TopBar.cs:614-618`） | 缺失（已出声） | 低 |
| M13 | 主菜单 | 左导航面板 + 底板四角顶点色 | `Navigation Panel` 191×1080（`RT/1103`）· 底板 `White Square` `m_Color=白` + 四角顶点色 TL(.066,.027,.003)/TR(.189,.005,0)/BR(.226,.001,.001)/BL(.019,.007,.007)（`MB/1940`+`MB/1941`） | `New(...,191f,1080f)` + `SetCornerColors(...)`（`MainMenuRuntime.cs:866-873`） | 一致 | — |
| M14 | 主菜单 | `Panel Shadow` / 两条分隔线 | Shadow `m_Color (0,0,0,0.46667)` 1929×70 @ y 511.7..581.7（`MB/1970`+`RT/1110`）· 分隔线 `m_Color (0.647,0.380,0.263)` 2.8×1009（`MB/1943`） | `MainMenuRuntime.cs:874-882` | 一致 | — |
| M15 | 主菜单 | 5 颗导航钮位置（VLG：spacing −16.35 · UpperCenter · padTop −5） | 钮顶 **143.41 / 296.74 / 450.07 / 603.40 / 756.73**（169.68 高 · 步进 153.33） | `NavButton(... 143.4/296.7/450.1/603.4/756.7 ...)`（`MainMenuRuntime.cs:919-926`） | 一致 | — |
| M16 | 主菜单 | 导航图标尺寸 | Home **156.349×137.435**（`RT/1111`）· 其余四颗 **140×140**（`RT/1240-1243`），`m_PreserveAspect=0` | `137.435 / 140.0`（`MainMenuRuntime.cs:919-926`） | 一致（2026-10-11 A284 修） | — |
| M17 | 主菜单 | 导航文案 / 字号 / 字色 / 字距 | 名字条 146.9×39.39（`RT/1085`）· `m_text` PLAY/COLLECTION/SHOP/REWARDS/SOCIAL · fs 33（COLLECTION 实渲 30.45）· 色 (0.9961,0.9294,0.7098)（`MB/1716` 等） | `NavButton`（`MainMenuRuntime.cs:941-980`）：名字条 9.3..156.2、fs33、色同值、`Loc.T(键)` | 一致 | — |
| M18 | 主菜单 | 选中高亮 `Selected highlight` | 整钮 164.3×169.7 · `40k_main_bt_selected BW` **Sliced** · `m_PixelsPerUnitMultiplier 0.92` · 色 (1,.0805,0)（`MB/1947` 等） | `MenuDraw.Nine(..., border 30, ppuMul 0.92, tint(1,0,0))`（`MainMenuSubmenuWindow.cs:534-541`） | 一致 | — |
| M19 | 主菜单 | 聊天预览 `ChatPreview` | 1475..1875 × 85..145（400×60，`RT/1169`）· 底 `Closed-Chat_background` 1474.7..1847（`RT/1515`）· 两条消息 fs18（`MB/1814`）· 聊天入口 `40K_icon_menu_chat` 1811..1879 × 81.8..148.3（`RT/1479`） | `MainMenuRuntime.cs:1881-1902` 逐值同 | 一致 | — |
| M20 | 主菜单 | 聊天预览起手态 | `ChatPreview.OnBeforeInitialization`：图标 scale 0 · 容器 fillAmount 0 · 每条正文 `set_enabled(false)`；`Show()` 才放大 | `SetChatPreviewVisible(false)`（`MainMenuRuntime.cs:1947`） | 一致（无消息 ⇒ 恒收起） | — |
| M21 | 主菜单 | 模式卡区 `GameModes/Viewport` | 167.04..1920 × 71.33..1080（1752.96×1008.67，`RT/1122`/`RT/1093`） | `ModesW/H`（`MainMenuRuntime.cs:231,2025-2026`） | 一致 | — |
| M22 | 主菜单 | 模式卡格 `Content` / `itemSize` | `Content` 74×945.5651（模板位）· rows 2 · columns 20 · itemSize **535×414.4** · spacing (20,20) · pad L38/R16（`RT/1107`+`MB/1961`） | `New(vp,"Content",74f,945.5651f)`（`:2037`）· `CardW/H`、`CardCol0=205`、`CardColStep=555`（`:1984`） | 一致 | — |
| M23 | 主菜单 | 模式卡：哪张模式 → 哪张图 / 1×1 还是 1×2 / 行 y | 原版出厂 `Content` **0 子**，卡由 liveop 服务端数据实例化；本地**没有**映射表（`主菜单_原版规格.md` §9·3） | 五张卡 + first-fit 排布（`MainMenuRuntime.cs:2040-2180`）· 行 y 取 `CardRow0Top=182.8` | **判不出**（我们挑的，注释已如实标注） | 低 |
| M24 | 主菜单 | 模式卡倒计时行 | `Starts in:` fs42.95 + 时钟 40.76² + `Timer Text` fs40（`MB` 逐颗，见 `MainMenuRuntime.cs:2480-2550`） | 同一套文案/字号（`:2502-2550`），数值用本机时间 | 一致（数值源 = 我们挑的，已标注） | — |
| M25 | 主菜单 | 顶栏 × 左导航的层级/遮挡 | `Safe area Only Horizontal` 的 `m_Children` 序 = `Navigation Panel`(**1**) → `ChatPreview`(2) → `1-Below Upper Bar`(3) → **`Upper bar`(4)** → `Borders`(5)… ⇒ **顶栏后画、压在左栏之上**（`RT/1540` 子件序） | 左栏队列 `QPanel 2910 / QContent 2920 / QText 2921`（`MainMenuRuntime.cs:240`）· 顶栏整条 `2986–2998`（`TopBar.cs:102-110`）⇒ **顶栏永远在左栏之上** | 一致 | — |

### 1·2 收藏窗

| # | 界面 | 检查点 | 原版值(出处) | 我们值(出处) | 结论 | 严重度 |
|---|---|---|---|---|---|---|
| C01 | 收藏窗 | 窗口参数 | `type=0` · `windowsPlacement=5` · `closeOnESC=1` · `extraScaleSmallScreen=1.0`（`MB -6401214277658680619`） | `CollectionWindow.cs:148-152` 逐值同 | 一致 | — |
| C02 | 收藏窗 | `Content Area` + 整窗渐变底 | 167.17,70.94 → 1920,1080（`RT` 实读）· `UIGradient` c1 #390503 / c2 #0C0004 / angle 82 | `MainMenuSubmenuWindow.cs:15`（ContentL/T/R/B）· `QBackground=3004` 压在页内容之下（`:65`） | 一致 | — |
| C03 | 收藏窗 | `Tab Buttons` 左栏 | x 167.2..332.2 · y 158.6..1080 · VLG padTop **30** · 每键 **165×180**（`menu_dump.py "Collection Menu Variant" --depth 5` 现读：Deck 188.6..368.6 / Cards 368.6..548.6 / CardBacks 548.6..728.6 / Alternate Art 728.6..908.6） | `BarW=165` `TabBtnH=180`（`MainMenuSubmenuWindow.cs:18,21`）· `BarPadTop=117.7`（`CollectionWindow.cs:124`） | 一致 | — |
| C04 | 收藏窗 | 四页签文案 / 字号 / 字距 / 颜色 | `TabButtonLabel`：`Deck`/`Cards`/`Cosmetics`/`Styles` · fs36 base23 auto[5,36] · 色 (0.957,0.882,0.675) · 字距 **−3.5** · Center/**Midline** · 折行 0（现读 `MonoBehaviour` 四颗同值） | `Buttons[]`（`CollectionWindow.cs:113-121`）+ `BuildTabButton`（`MainMenuSubmenuWindow.cs:600-640`：`SetCharSpacing(−3.5)` + `SetVAlign(Midline)`） | 一致 | — |
| C05 | 收藏窗 | 页签红点 `Badge Highlight` | 35×35 · 色 (0.736,0.736,0.736,1) · 位置随键不同（`BadgeDy`） | `MainMenuSubmenuWindow.cs:644-648`（初值 alpha 0，由刷新函数定） | 一致 | — |
| C06 | 收藏窗 | 返回钮 `Shared/Close Button` | 192.2..342.2 × 83.4..143.4（150×60）· `Button Text` **200.5..333.4 × 89.3..137.5** = 132.86×48.24 · 字 `Back` fs40 auto[10,40] | `CloseBtn*`/`BackTxt*`（`CollectionWindow.cs:2705-2724`）· `Shared` 挂在 `Tabs` 下、`Button Text` 是钮的子件（`:3111-3170`） | 一致 | — |
| C07 | 收藏窗 | 页头过滤器 + `Clear filters` | `Filter Toggle` **367.17..417.17 × 88.44..138.44**（50×50）· `Filters` 字样 fs42 auto[10,42] · `Clear Filter Button` **612.2..862.2 × 83.5..143.5**（250×60） | `FltBtnX/Y/S`（`CollectionWindow.cs:2727`）· `ClearFltX/Y/W/H`（`:2735`）· `BuildFilterHeader`（`:2806`） | 一致 | — |
| C08 | 收藏窗 | Deck 页右上三颗 `Control Buttons` | 组 1180.00,80.94 → 1920.01,140.94（740.01×60）· HLG reverse=1 sp25 padR14 ⇒ 视觉左→右 `Unlock`(1180) `Import`(1391.01) `Create`(1661.01)；三张图 `m_Enabled=0` | `CreateX/ImportX`（`CollectionWindow.cs:2686`）· `CtrlGrp*`（`:2696`）· `UnlockW`（`:2701`）· 组节点 `Control Buttons` + `SetSiblingIndex(1)`（`:3317-3320`） | 一致 | — |
| C09 | 收藏窗 | Deck 页视口 / 格 / 列 / 居中量 | 330.9,155.9 → 1920,1080（1589.1×924.1）· 格 **225×364.5** · spacing (20,0) ⇒ 6 列 · 列方向原点 = 整体居中 **69.55** ⇒ 第 1 格左沿 **400.45** | `DeckViewport`（`CollectionWindow.cs:828`）· `DeckCellW/H` `DeckSpacingX`（`:845`）· 居中算式写在 `:860-866` | 一致 | — |
| C10 | 收藏窗 | 卡组格内容（框 / 卡背 / 名字 / 阵营 / 模式图标 / 空位态） | 根 250×405 显示 ×0.9 = 225×364.5 · `Frame 40K_bt_deck` 246×368@2,17 · 卡背 mask 228×306@11,26.1 · `Deck Name` fs32 auto8–32 字距−2@20,344.2 · 阵营/模式图标 84.5×85.7 · 空位 `Create\nNew Deck` fs32（A2 §三） | `MenuDraw.DeckCell`（`MenuDraw.cs:3009-3120`）· 三处调用：收藏窗 `:3852-3868`（传 `gameMode`、**不传** difficulty）· 选卡组窗 `DeckSelectionPopup.cs:870-882`（传 gameMode+difficulty） | 一致 | — |
| C11 | 收藏窗 | 卡组格金环 | 本窗用的 `Collection Deck` 变体 `useSelectedHighlight = **0**` ⇒ `SelectedHighlight` **永不被调用**；两张实拍无一格是金的 | 恒传 `selected=false`（`CollectionWindow.cs:3853-3855`） | 一致（2026-10-11 A1525⑥ 修） | — |
| C12 | 收藏窗 | Deck 页左抽屉 `Deck Filters` | 0.06,155.94 → 335.56,1080（335.5×924.06）· Shadow 宽 153.01 · `Filters`(VLG) = `Deck Name Filter`(80) + `Army Filter`(345)（现读 `menu_dump.py "Deck Filters" --depth 4`）· `Input Field` **27.2..308.4 × 175.9..215.9**（281.28×40）· Army `Title` 25.1..335.6 × 240.9..290.9 fs36 · `Content` 格 100×100 sp 7/0 pad 14 | `Dflt*`/`DInput*`/`DArmy*`/`DTitle*`/`DCell*`（`CollectionWindow.cs:3507-3512`）· `BuildDeckFilterDrawer`（`:3534-3660`） | 一致 | — |
| C13 | 收藏窗 | 抽屉起手态 | `Deck Filters` `m_IsActive=T`；`CollectionDisplay.Initialize → filters.SetupFilters()` 收尾 `anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)` ⇒ **停在收起**（A101，反汇编 VA 0x1815EC278/0x1815F0740） | 起手收起（注释 `CollectionWindow.cs:3496-3505`） | 一致 | — |
| C14 | 收藏窗 | 抽屉滑动 | `hiddenPosition=(-550,0)` · `animationTime=0.3` | `ApplyDrawerSlide` 那一节（`CollectionWindow.cs:60-66` 有该节的判据说明；四页共用） | 一致 | — |
| C15 | 收藏窗 | Cards 页视口 / 格 / 列 / 居中 | 330.2,155.9 → 1919.9,1079.9（1589.7×924.06）· 格 **262.5×384** · spacing 0 · 6 列 · 两侧各 7.35 | `CardsViewport`（`CollectionWindow.cs:171`）· `:174-176` | 一致 | — |
| C16 | 收藏窗 | 卡位张数条 `Counter` | `Collection Card/Content/Counter` 150×50（锚 0.2857/0.7143 ⇒ 格内 75..187.5 · 距底 0.768..38.272）· 底图 `40K_main_deck_card counter` `m_PreserveAspect=1` · TMP `x14` fs31.9 auto[7,32] | `CardsCounterRect`/`CardsCounterTextRect`（`CollectionWindow.cs:210-230`）· `BuildCardsCounter`（`:261-290`） | 一致 | — |
| C17 | 收藏窗 | Cards 页筛选 7 行 | Name 79.02 / Owned 50 / Upgradable 50 / Army（Title 50 + 内容）/ Rarity 280 / Cost 230 / Type 150；卡组编辑那棵树序列化 `Filters` = **839.018**（`Army Content` 出厂 0），收藏窗按 `阶段二_卡组线_原版规格.md` §七 = **989.02**（Army 行按 150 算） | `FilterPanelModel.cs:41-90`（`RowName/RowOwned/…` + `ComputeLayout`，Army 行按格数算 ⇒ 13 阵营 = 550） | 一致（两边都是「Army 行按内容长」，出厂序列化值只是模板位） | — |
| C18 | 收藏窗 | 卡格 `New Card Badge` / `Ban Icon` | prefab 两者 `m_IsActive = True`（`GameObject/New Card Badge.json` / `Ban Icon.json`）；运行期 `CollectionCard.Config` 把 banned 置 0（`CollectionCard__Config.c:29`）⇒ Ban 隐藏；New 的关闭点见 §三·2 | **两个节点都没建**（`CollectionWindow.cs` 全文 grep `New Card` / `Ban Icon` = 0 命中；卡面由 `CardView` 画） | **缺失**（低；实拍两处都不显示） | 低 |
| C19 | 收藏窗 | 卡格 `Card Ready for level up` | prefab `m_IsActive = False`（`GameObject/Card Ready for level up.json`）· 运行期 `CheckUpgradeHighlight` 驱动 | **不建** | 缺失（原版出厂也不画；我们「默认最高级」⇒ 永不触发） | 低 |
| C20 | 收藏窗 | Cosmetics 页（卡背） | 视口 335.44,155.94 → 1920.01,1080（1584.56×924.06）· 格 **250×405** · `_controlSegmentSize=1` ⇒ 6 列 · 卡背 233 张 · `Cardback Shadow SDF` 比卡背大一圈 | `CosmoView`/`CosmoCell*`（`CollectionWindow.cs:1217-1230`）· 两层兄弟序 + 各自队列（`:1319-1396`） | 一致 | — |
| C21 | 收藏窗 | 点卡背开哪扇窗 | `CardbackCollectionTab__OnItemSelected` → `ItemDisplayWindow`（`decomp_full/CardbackCollectionTab__OnItemSelected.c:17-24`） | `WindowsManager.OpenItemDisplay(ItemDisplayWindow.ForCardback(...))`（`CollectionWindow.cs:1403-1410`） | 一致（2026-10-17 T25 修） | — |
| C22 | 收藏窗 | Styles 页换风格条 | 左右圆钮 74.39×75.61 @ 683.40 / 1320.40 · 图标 56.86×58.13 · 风格图 787.59..1299.59 · 换风格 bar 155.94..283.94（A4 §三） | `StyleArrow*`/`StyleLogo*`/`StyleBarT/B`（`CollectionWindow.cs:1712-1722`）· 左钮 `ChangeStyle(-1)`、右钮 `+1`（`:1735-1737`，按**位置**接线不按字段名） | 一致 | — |
| C23 | 收藏窗 | Styles 页网格 | 视口 330.22,287.67 → 1920.01,1080 · 格 262.5×384 · 6 列 · 本地 7 张督军异画（`AA_HB` / `_AA_` 命名） | `StyleView`/`StyleCellW/H`（`CollectionWindow.cs:1757-1765`） | 一致 | — |
| C24 | 收藏窗 | 空收藏提示 | `Empty Collection Warning` prefab `INACT`，字 fs36（`There are no cards in your collection for the selected filters`） | 建了、恒关，按筛选结果开（`CollectionWindow.cs:1285-1295`） | 一致 | — |

### 1·3 卡组编辑窗

| # | 界面 | 检查点 | 原版值(出处) | 我们值(出处) | 结论 | 严重度 |
|---|---|---|---|---|---|---|
| E01 | 卡组编辑 | 窗口参数 | `type=0` · `windowsPlacement=10 (World)`（`Deck Editing Menu` prefab 序列化） | `DeckEditingWindow.cs:80-81` 显式赋 | 一致 | — |
| E02 | 卡组编辑 | 窗根 / `Content Area` 内缩 | 根 `Deck Editing Menu` · Content Area 四边 167.175 / 70.94 | `DeckEditingWindow.cs:62`（只借名字）· `MainMenuSubmenuWindow.cs:15` | 一致 | — |
| E03 | 卡组编辑 | 顶栏（共用件） | 同 M01–M11（`TopBar` 同一份 prefab 树） | `DeckRuntime` 调 `TopBar.Build`（B25 收口，`TopBar.cs:1-52` 文件头） | 一致 | — |
| E04 | 卡组编辑 | 返回钮 | 192.2,83.5 150×60 · 字 `Back`（`Deck Editing Menu/Content Area/Header/…`） | `HdrBackX/Y/W/H`（`DeckRuntime.cs:76`）· 建/接线 `:1466-1495` | 一致 | — |
| E05 | 卡组编辑 | `Filters` 圆钮 + 文案 | 钮 **367.2..417.2 × 88.5..138.5**（50×50，`40k_menu_bt`）· 图标 `40k_bt_icon_search` 27×27 · 文案 **437.2..587.2 × 88.5..138.5** fs40 base32 auto[10,40]（现读 `menu_dump.py "Deck Editing Menu" --depth 6`） | `DeckRuntime.cs:1498-1540`（含 `QText−1` 分层修） | 一致 | — |
| E06 | 卡组编辑 | `Clear filters` | 父容器 `Header/Filters` = GridLayoutGroup **85×70** sp(5,0) constraint=1 align MiddleLeft + CSF(H=MinSize) · 容器 592.2..1468.6 × 71..156 · 按钮 `LayoutElement.m_IgnoreLayout=1` ⇒ 按自己锚点落（`DeckRuntime.cs:80-120` 有整段） | 按锚点算落点（注释 `:86-120`）；**显隐**按 `CollectionFilterController` 三字段（`:92-120`） | 一致（2026-10-20 修） | — |
| E07 | 卡组编辑 | 左栏三页签 `张牌 / 卡组信息 / 美容品` | 三格各 **108.9667** 宽：`Cards [0.3,109.2]` · `Info [109.2,218.2]` · `Cosmetics [218.2,327.1]` · 名牌 98.96×40 · fsMax 34 base 26/24/28.15 | `TabCellW`/`TabsX`（`DeckRuntime.cs:156-190`）· 文案走 `Loc.T`（`:1680-1710`）· `BuildCosmeticsPage`（`:1314`） | 一致 | — |
| E08 | 卡组编辑 | 卡组列表行 | 行节点 **309 × 55.7**（抽屉 325 各缩 8 = 8.4..317.4）· 行距 **57.7** = 55.7 + VLG spacing 2.0 · 视口高 **644.06**（`A1736`） | `RowW 325` / `RowH 55.7` / `RowSpacing 2` / `RowPitch 57.7`（`DeckRuntime.cs:203-220`）· `RowNodeX/RowNodeW`（`:471`）· `ListH 644.06`（`:202`） | 一致 | — |
| E09 | 卡组编辑 | 卡池 桌面 / 小屏两档 | 桌面 **262.5×384 · 6 列** · 视口 1589.8×924.1 · 居中 7.4；小屏 ×`_mobileSizeScale 1.5` ⇒ **393.75×576 · 4 列**（`RecyclableScrollRect.Initialize` RVA 0x89F460） | `PoolCellW0/H0`（`DeckRuntime.cs:684`）+ `Pool` 结构（`:753-810`，列数按宽度算） | 一致（两档都实现） | — |
| E10 | 卡组编辑 | 卡池张数条 | 格内 75..187.5 × 距底 0.768..38.272（112.5×37.504）· 底图 `40K_main_deck_card counter` · 字 fs31.9 auto[7,32] | `PoolCounterH0/PoolBarX10/20/PoolCntY10/20`（`DeckRuntime.cs:738-745`） | 一致 | — |
| E11 | 卡组编辑 | `Done` + `30/30` | Done 13,1020.5 188.5×50.2 · 计数 251.6,1020 75×50 · 底图标 201.6,1025 50×40 | `DoneX/Y/W/H` / `CntX/Y/W/H` / `FootIc*`（`DeckRuntime.cs:665-667`）· 建/接线 `:2336-2370` | 一致 | — |
| E12 | 卡组编辑 | 通配符条 `WIldcard Counter` | 底板 `40k_topmarquee_currency_display BW` Sliced 15 · 四组 30×44 图标 + 41×44 数字 fs32.6（`R6 §2·F #38`） | `DeckRuntime.cs:1582-1669`（含 `QText−1` 类分层） | 一致 | — |
| E13 | 卡组编辑 | 筛选抽屉（卡牌） | `Card Filters` **2.2,156 → 333.9,1080**（331.73×924.06）· Shadow 同矩形 · `Filters` VLG 7 行 · 内容高 **839.018（Army Content 出厂 0）**，运行时 Army 展开后 ≈ **1389** | `FltX/Y/W/H`（`DeckRuntime.cs:857`）· `FilterPanelModel.ComputeLayout`（13 阵营 ⇒ ContentH **1389.02**）· 滚轮 `:6689-6690` | 一致（按内容高算 ⇒ 可滚 465） | — |
| E14 | 卡组编辑 | 美容品页（卡背抽屉） | `Cosmetic FIlter` 2.2,156 → 333.9,1080 · `Army Filter`(150) + `Owned Toggle`(50)，内容 **707.8** < 视图 924 ⇒ **无滚动** | `_cosmoFltScroll` 恒 0（`DeckRuntime.cs:978-990`） | 一致 | — |
| E15 | 卡组编辑 | 「选择督军」 | 原版**没有独立的督军选择面板** —— 走 `CardArmyFilter`（阵营 13 格）+ 卡池；`CardArmyFilter.SetFilterToDeck` 在无督军时强制 Hero 并隐藏自身（`CardArmyFilter__SetFilterToDeck.c:15-28`） | 同一条：`FilterPanelModel.HasWarlord` + `CostRowVisible`（`FilterPanelModel.cs:600-620`）· `DeckRuntime` 传 `CostRowNeedsWarlordDeckEdit=true` | 一致 | — |
| E16 | 卡组编辑 | 卡组行里的费用圆 / 份数 | `Cost Image`：锚 (0.03,0)-(0.03,1) · `sd (0,6)` · `apos −0.180` · ARF 高控宽 ratio1 ⇒ **61.7 见方**（绝对 3.377..65.077，`RT 1553532234772573988`）· 份数 `m_fontSize 32`（`R:159-166`）· 名字 fs32 auto2-32 base36 hRight/Midline | `DeckRuntime.cs:220-300`（费用圆三版订正到 `3.377/61.7`）· `:362`（份数 fs32）· `:297-300`（名字） | 一致（2026-10-21 A1722 订正） | — |

### 1·4 弹窗

| # | 界面 | 检查点 | 原版值(出处) | 我们值(出处) | 结论 | 严重度 |
|---|---|---|---|---|---|---|
| P01 | `Deck info Popup` | 三颗主钮 `Practice / Edit / Select` | 324.5×80.1 · HLG spacing 36 · 右缘 1770.70 ⇒ 中心 887.45 / 1247.95 / 1608.45（A1 §2） | `DeckInfoPopup.cs:18`（文件头逐值）+ 建树节 | 一致 | — |
| P02 | `Deck info Popup` | `Deck Options` 五颗圆钮 | 74.386×75.605 · HLG spacing **−50** · `reverse` ⇒ 视觉左→右 = 树序反 | 同文件（含 2026-10-03 顺序修正） | 一致 | — |
| P03 | `Deck info Popup` | 关窗钮 | `Generic Close Button Orange` 圆底 + `40k_general_bt_yellow_close` | 同文件 `:22` | 一致 | — |
| P04 | `Deck Selection Popup` | 格 / 列 / 首页 / 关窗钮 | 视口 **194.50,208.63 → 1759.50,986.69**（1565×778.06）· `_cellWidth/Height 225/364.5` · `_spacingX 20` · 6 列 · 居中 pad **57.5**（`menu_dump.py "Deck Selection Popup with Tabs"` 现读）· 关窗钮 1783.11..1857.49 × 62.90..138.50（74.39×75.60） | `SvL/T/R/B`（`DeckSelectionPopup.cs:165`）· `CellW/H/Gap`（`:186`）· `CloseL/T/R/B`（`:177`） | 一致 | — |
| P05 | `Deck Selection Popup` | `Empty Collection Warning` / `Instructions` / `Filter Buttons` / 搜索框 | prefab：`Empty Collection Warning` INACT · `Instructions` INACT · `Filter Buttons` INACT · `Collection Menu Input Field` INACT | 都不建（`DeckSelectionPopup.cs:86-110` 逐条出声）· 空态改用另一行说明（`:838` 区） | 一致 | — |
| P06 | `Deck Selection Popup` | **两个页签** | `Generic Tab UI Button` **196.3..456.3 × 35.5..103.1** · `… 1` **468.8..728.8 × 35.5..103.1**（260×67.64）· **两颗 act=True**（现读 dump，无 `INACT` 标记） | `BuildTab(...)` 在 **TabT=35.07..102.71** 画两颗（`DeckSelectionPopup.cs:454-456`） | **判不出** —— 唯一实拍 `更换卡组的参考.png` 同一矩形被主菜单顶栏占着、看不到页签；关闭点没找到（见 §三·1） | 中 |
| P07 | `Deck Selection Popup` | 标题文案 | `Instructions 2` **1236.1..1770.2 × 124.9..209.9** fs36 auto[18,40] Center/Middle · `Localize.mTerm = MenuDeck/Tip/SelectDeckAgainst`（`MonoBehaviour_4152179270747796863.json`） | `TitleTerm`（`DeckSelectionPopup.cs:214`）→ 官方 zh-CN = **「选择一个卡组进行对战」** | **偏差**（对实拍：实拍那颗字是**「选择卡组」**）—— 见 §三·6 | 中 |
| P08 | `Import Deck Popup` | 窗体 / 确认钮 | `Window` **560,234.07 → 1360,685.93** · `Generic UI Button` **478.343×75** · 字 `Confirm` fs45 | `WinL/T/R/B`（`ImportDeckPopup.cs:53`）· `OkW/OkH`（`:60`） | 一致 | — |
| P09 | `Card Detail` | 卡体 / 卡框 / 扇形 | `Card Display` **752×868**（`RT/1610`）· 卡体 `2DCard` = **523.25×832.75**（scale 250）· 5 槽扇形 pos (0,60)/(−121,53)/(−232,49)/(−332,45)/(−432,27) · rot 0/2.510/5.890/12.648/19.200 · scale 250/232.954/221.593/204.546/181.815（legacy clip `AnimationClip_1560242982351263260`） | `CardR/CardB`（`CardDetailPopup.cs:203`）· 9 格扇形（`:327` 区，0–4 原版真值、5–8 我们外推） | 一致（5–8 已标我们挑的） | — |
| P10 | `Card Detail` | 三块面板 / 计数条 / 通配符条 | 面板 x **1294..1744**（450 宽）· y **146.71 / 411.12 / 673.42** · 高 263.73/261.62/229.59 · `Card Counter` **825.07,829.81 → 1094.93,909.19** · 通配符 底 1550..1870 × 91.5..135.5、四组 30×44 + 41×44 fs32.6（`阶段二_卡片详情窗_原版规格.md` §二/§三，逐节点 pid） | `PanL/R/T/B` `CraftT/B` `UpgT/B` `AltT/B`（`CardDetailPopup.cs:213-216`）· 计数 `:261` + `:1324` · 通配符 `:303-305,1462` | 一致 | — |
| P11 | `Card Detail` | 相关卡压暗 / 遮罩层序 / Lore 居中 | 非前台槽 `cardInBackGroundColorTint = (0.65,0.65,0.65,1)`（`InitializeCardForDisplay.c:73-97`）· 遮罩在卡下方 · `LoreText` `m_HorizontalAlignment = 2(Center)` | `CardFan.BackTint` + `TweenTint`（`CardDetailPopup.cs:629-632,790-792`）· 队列 遮罩 < 卡格 < 钮/字 | 一致（2026-09-28 修） | — |
| P12 | `Card Detail` | `FlavourTextBG` 底图 / Lore 内容 | 底图 `sprite=0`，运行时由 `FlavourTextSO` 给 · Lore = 背景故事（`SetCardLore.c:24-27`：没有就整块关） | 只画字、不画底（文件头）；Lore 走官方 `Loc.Lore`，缺 128 张 ⇒ 整块关（`:705-710`） | 一致（底图本地没有，已出声） | — |
| P13 | `Card Detail` | 异画面板 / `Buy Original Card` | `Alternate Art Panel` 在没有异画风格时整块 `SetActive(false)`（`AlternateArtPanel__Initialize.c:22-38`） | `HasAltArtStyle` ⇒ 没有就整块关（`CardDetailPopup.cs:36-70` 区） | 一致 | — |
| P14 | `Card Detail` | 两块功能面板（创建副本 / 升级）显隐 | `CraftingPanel.Initialize.c:42` 有一处 `SetActive(…, DAT_1842963f8+0xb8)`、`CheckButtons` 里另有两处 ⇒ **确有按卡决定的显隐**，但**具体条件没读清**（需要解那两处全局/字段） | `Craftable(Card.Rarity)` 才建「创建副本」（`CardDetailPopup.cs:556`）· 升级面板恒建（`:560`），钮 `Interactable=false` | **判不出**（原版条件未读清；我们按稀有度 + 用户裁决） | 低 |

---

## 二、功能入口表

| # | 控件 | 原版行为(出处) | 我们行为(file:line) | 结论 |
|---|---|---|---|---|
| 1 | 顶栏齿轮 `SettingsBtn` | `OpenWindowButton` → `Main Menu Settings Window`（GUID `5a20859a…` → pid −9019961019057471578） | `TopBar.cs:255` → `MainMenuRuntime.OpenSettings()`（`:317`） | 一致 |
| 2 | 顶栏信箱 `InboxBtn` | `OpenWindowButton` → `Inbox Menu`（GUID `9aadd8e3…`） | `TopBar.cs:257` → `MainMenuRuntime.OpenInbox()` | 一致 |
| 3 | 头像 `Avatar Item Small` | `OpenWindowButton` → `Player Profile Window`（GUID `e357bbff…`） | `TopBar.cs:260` → `MainMenuRuntime.OpenProfile()` | 一致 |
| 4 | 挑战 `Challenge button` | `ChallengeButton.OnButtonClick`：弹「ReceiveChallenge」确认框 + 自己隐藏；`Initialize` 按 `challenges.Count>0` 显隐 | `TopBar.cs:227` 建但 `SetActive(false)`；`:327` 点了只**出声** | 一致（本地无挑战源） |
| 5 | 反馈 `Feedback Button` | prefab `m_IsActive=false`；持久调用 target=null ⇒ 永不派发 | `TopBar.cs:242-244` 建但关 · 不接线 | 一致 |
| 6 | 导航 PLAY | `windowToOpenScene = MB1927`（常驻 `MainMenuWindow`） | `MainMenuRuntime.OnNavClick(0)`（`:919`） | 一致 |
| 7 | 导航 COLLECTION | GUID `6b5ba7fb…` → `Collection Menu Variant` · `closeOtherMenus=1` | `OpenCollection()`（`MainMenuRuntime.cs:565-580` 区） | 一致 |
| 8 | 导航 SHOP | GUID `a2b03b42…` → `Shop Menu Variant` | `OpenShop()` | 一致 |
| 9 | 导航 REWARDS | GUID `32024b16…` → `Rewards Base Submenu Variant` | `OpenRewards()` | 一致 |
| 10 | 导航 SOCIAL | GUID `d06efaff…` → `Social Submenu Variant` | `OpenSocial()` | 一致 |
| 11 | 聊天预览 | `ChatPreview.OpenChat` → `ChatPanel`；预览先收回 | `MenuDraw.Hit(p,"ChatHit",…)` → `ChatPreviewClicked`（`MainMenuRuntime.cs:1902`） | 一致 |
| 12 | 模式卡（5 张） | liveop 数据决定开哪扇窗（`LiveopsMenuContainer.OnClick → LiveOpsEvent.OpenWindow`） | `_placeAllCards` + 各卡 kind 映射（`MainMenuRuntime.cs:2200-2260`；Tutorial → `TutorialModePopup`，A853） | 一致（映射我们挑的，已标注） |
| 13 | 收藏窗 `Back` | `Shared/Close Button` → 关窗 | `CollectionWindow.cs:3111-3170` | 一致 |
| 14 | 收藏 Deck 页 `Filters` | `CollectionDisplay.OnEnable → ToggleFilters(bool)`（`hiddenPosition=-550` / 0.3s） | `ToggleDeckFilters`（`CollectionWindow.cs:3293` 传参） | 一致 |
| 15 | 收藏 `Clear filters` | `CollectionFilterController.clearFiltersButton` | `ClearDeckFilters` / `ClearCardFilters` / `ClearCosmoFilters` / `ClearStyleFilters` | 一致 |
| 16 | 收藏 `Create Deck` | `SelectDecksTab.CreateDeck → WindowsManager.OpenWindow<DeckEditingWindow>` | `GoNewDeck` → `DeckEditingWindow.OpenFrom`（`CollectionWindow.cs:3698`） | 一致 |
| 17 | 收藏 `Import Deck` | `SelectDecksTab.importDeck` → `Import Deck Popup` | `OpenImportPopup` → `ImportDeckPopup.Create` | 一致 |
| 18 | 收藏卡组格点击 | `CollectionDisplay.OnItemSelected` → 选中 + 开 `Deck info Popup`；`Edit Deck` 才进编辑 | `SelectDeck(idx)`（`CollectionWindow.cs:3869`）+ `DeckInfoPopup` | 一致 |
| 19 | 收藏 Cards 页点卡 | `CardCollectionTab.OnItemSelected` → `CardDisplayWindow.ShowCard(..., showOptions=1)` | `OpenCardDetail` → `CardDetailPopup`（`CollectionWindow.cs:1553` 区注释过期） | 一致 |
| 20 | 收藏卡背点击 | `ItemDisplayWindow`（`CardbackCollectionTab__OnItemSelected.c`） | `CollectionWindow.cs:1403-1410` | 一致 |
| 21 | 收藏 Styles 左右箭头 | `leftStyleButton` 挂**右**钮、`rightStyleButton` 挂**左**钮（按位置接） | `BuildStyleArrow(...)`（`CollectionWindow.cs:1735-1737`） | 一致 |
| 22 | 卡组编辑 `Back` | `DeckEditingWindow` 关闭钮 → `TryClose()`（**不保存**；ESC/Done 才存） | `DeckRuntime.cs:1495` `Btn_("hdr_back",…)` → `TryClose()`（`:969` 注释） | 一致 |
| 23 | 卡组编辑 `Filters` | `DeckEditingPanel.ToggleDeckList` / 抽屉 `-550`·0.3s | `_filtersOpen` + `FltHiddenDx`（`DeckRuntime.cs:2478-2600`） | 一致 |
| 24 | 卡组编辑 `Clear filters` | `CollectionFilterController.clearFiltersButton`（父容器 GridLayoutGroup 85×70 · IgnoreLayout） | `DeckRuntime.cs:86-120` + `ClearFilters` | 一致 |
| 25 | 卡组编辑三页签 | `DeckEditingPanel.Toggle{DeckList,DeckInfo,Cosmetics}` | `DeckRuntime.cs:975`（`_tab 0/1/2`）+ `:1680-1710` | 一致 |
| 26 | 卡组编辑 `Done` | `DeckEditingWindow.TrySaveDeck`（校验 → 上传） | `SaveAndSay()`（`DeckRuntime.cs:961-970` 区） | 一致（单机落本地库，不上传） |
| 27 | 卡组编辑 ESC | `DeckEditingWindow__ESCPressed` = 只 `TrySaveDeck`，不关窗 | `DeckEditingWindow.cs:161-173` → `_runtime.EscPressed()`，返回 false | 一致 |
| 28 | 卡组编辑卡池左键/右键 | 左键 = 弹放大窗 · 右键 = 加进卡组（`DeckEditingWindow__OnCardClick.c:16,42`） | `DeckRuntime.cs:26` 文件头 + 卡片点击处理 | 一致 |
| 29 | `Deck info Popup` 五颗圆钮 | 删/复制/分享/分享到聊天/信息（`DeckInfoControls`） | `DeckInfoPopup.cs`（A10 四件全做完；`Share` 写剪贴板、`Share On Chat` 只出声） | 一致（聊天服务器本地没有） |
| 30 | `Deck info Popup` `Practice Deck` | `SelectPracticeOpponentDeck` ⇒ 本副当**我方**，弹选卡组窗挑**对手** | `DeckInfoPopup` 同名方法 + `PracticeModePopup.TakePendingOpponentDeck()` | 一致（2026-10-04 方向订正） |
| 31 | `Deck Selection` 点一行 | `UpdateInfo` → 开 `Deck info Popup`（不选中、不关窗） | `UpdateInfo(pick)`（`DeckSelectionPopup.cs:880`） | 一致（T24 修） |
| 32 | `Deck Selection` `Random` | `DeckSelectionTabController.RandomizeDeck` = 选中 + 关窗 | 直接 `Pick(...)` | 一致 |
| 33 | `Deck Selection` 两页签切换 | `ShowPrebuiltDecks(bool)` / `ShowOwnDecks` | `SwitchTab(own)`（`DeckSelectionPopup.cs:669`） | 一致（页签**可见性**见 P06） |
| 34 | `Card Detail` 创建副本 / 升级 | `CraftingPanel.Craft` / `UpgradePanel.Upgrade` | `OnCraftClicked`（只出声）/ `Upgrade` = `Interactable=false`（用户 2026-10-12 拍板「默认最高级」） | 一致（两处均按裁决出声） |
| 35 | `Card Detail` 异画左右钮 | `leftButton/rightButton` 在同一档内回绕 | `BuildAltArrow`（0..N 回绕，改前台卡立绘） | 一致 |
| 36 | `Card Detail` 语音 / 眼睛钮 | `voiceOverButton` / `showCardTextButton`（按卡定建不建） | `RefreshButtons`（`CardDetailPopup.cs:365,1486`） | 一致 |
| 37 | `Card Detail` 关窗 | 全树**没有关闭钮**：点遮罩 / ESC | 点遮罩关窗（`.cs` 内，`closeOnESC=1`） | 一致 |
| 38 | `Import Deck Popup` `Confirm` | 校验卡组串 → 关窗 | `ImportDeckPopup`（校验只留一份 `CollectionData.ImportDeck`） | 一致 |

---

## 三、没查清（逐条：缺什么判据 / 我搜过哪些词、哪些包）

1. **`Deck Selection Popup` 两个页签在运行期到底可不可见。**
   - 已证：prefab `Alliance Header Buttons/Tab buttons/{Generic Tab UI Button, Generic Tab UI Button 1}` 两颗 **act=True**（`menu_dump.py bundle_menus_assets_all "Deck Selection Popup with Tabs" --depth 4` 现读，无 `INACT` 标记）· 260×67.64 @ y 35.5..103.1。
   - 已证：唯一实拍 `C:/Users/qjh36/Desktop/warpforge参考图/更换卡组的参考.png`，同一矩形（设计 px 196..729 × 35..103）被**主菜单顶栏**（玩家名 `pootis` / 信箱 / 头像）占着，**看不到页签**。
   - 已证：该帧里顶栏**被压暗了**（同一采样点 R 通道：本帧 3~10 vs `主界面参考图.png` 11~44）⇒ `Menu Dark Background`(alpha 0.773) **盖在顶栏之上** ⇒ 弹窗确实渲染在顶栏之上 ⇒ **若两颗页签 active，它们应当可见**。⇒ 「原版此刻页签不画」更像真的。
   - 已搜：`d:/2/tools/decomp_full/` 里 `DeckSelectionPopup__TryOpen.c` / `__Open.c` / `__Initialize.c` 三个方法体 —— **零 `SetActive`**；`DeckSelectionTabController` 的 5 个序列化字段（`deckSelectionPopup / prebuiltDecksToggle / ownDecksToggle / randomDeckButton / searchBar`，`MonoBehaviour_-2636550183226722945.json`）**没有页签容器引用**。
   - ⇒ **缺的判据 = 运行期实况**（关服跑不起来）。今天我们的做法会把两颗页签画在顶栏那一带；若原版其实不画，这是**可见的重复件**。

2. **`Collection Card/New Card Badge` 的运行期关闭点。**
   - 已证：prefab `m_IsActive = True`（`GameObject/New Card Badge.json`）。
   - 已搜：`grep -rl "ToggleNewCardBadge" decomp_full` = 5 个调用者（`WhereToGetCardsSection__Initialize` · `CardInBoosterPack__CardDisplayWindowOnCloseWindow` · `CardAlternateArtDrawer` · `CardDrawer` · `CardInBoosterPack…MoveNext`）—— **都不在 `CardCollectionDisplay` / `CollectionCard` 那条链上**；`CollectionCard__Config.c` 里也没有 `0x98` 写点（只有 `0x78` banned=0）。
   - ⇒ 判不出原版收藏页会不会显示「Новинка!」角标；我们**没建**该节点（`CollectionWindow.cs` grep `New Card` = 0）。

3. **`Card Ready for level up` 的触发条件。**
   - 已证：prefab `m_IsActive = False`；起落由 `CollectionCard__CheckUpgradeHighlight` 驱动。
   - 没读：`CollectionCard__CheckUpgradeHighlight.c` 的判定（需要升级表数据，本地没有）。
   - ⇒ 我们**不建**该节点（判据不足 + 我们「默认最高级」）。

4. **模式卡的「模式 → prefab / 图 / 1×2 或 1×1」映射。**
   - 已证：原版 `GameModes/Content` 出厂 **0 子**，卡由 liveop 服务端数据实例化（`主菜单_原版规格.md` §9·3）。
   - 已搜：`bundle_menus_assets_all/GameObject/` 只有卡 prefab 族（`Base Game Mode Container 1x1` / `1x2` / `Draft …` / `Reward Event…`），**没有映射表**；`bundle_liveopsmenuimages_assets_all/Sprite/` 只有图族。
   - ⇒ 我们的「五张卡 + 谁 1×2 / 谁 1×1 + 行 y 182.8」都是**我们挑的**（代码注释已如实标注）。

5. **玩家名 / 等级 / 资源数 / 通配符数 / 每日任务状态。**
   - 原版全部来自 `PlayerDataManager` / PlayFab（`PlayerProfileUIController__SetPlayerLevel.c` 等）。本地关服 ⇒ 拿不到。
   - 我们：等级画 prefab 空态 `-`（`TopBar.cs:418`）；玩家名/资源走本地档案（`ProfileData` / `MainMenuRuntime.OwnedOf`）。

6. **`Deck Selection` 标题文案冲突。**
   - prefab 真值：`Instructions 2` 的 `Localize.mTerm = MenuDeck/Tip/SelectDeckAgainst`（`MonoBehaviour_4152179270747796863.json` 现读）。
   - 官方 zh-CN 表（2025-07-30 快照）：**「选择一个卡组进行对战」**。
   - 唯一实拍（`更换卡组的参考.png`）：**「选择卡组」**。
   - ⇒ 两个来源打架。我们走官方词条；若以实拍为准应改成「选择卡组」。**缺的判据 = 客户端当前版本的实际译文表**（远端包）。

7. **`Deck Editing Menu/Card Filters` 展开后的真实内容高。**
   - prefab 序列化 `Filters` = **839.018**（`Army Filter/Content` 出厂高 **0**）；我们按「13 格 3 列 = 5 行」算出 **1389.02**（`FilterPanelModel.ComputeLayout`）。
   - 原版 `Army Filter` 挂在 `VerticalLayoutGroup + CSF(v:MinSize)` 上 ⇒ 逻辑上会展开，但**展开后的实况没跑到**。
   - ⇒ 我们滚轮上界 = 1389 − 924 = **465**；若原版另有收合逻辑，这个数会不同。

8. **`Alternate Art` 风格图标（`Art Style Logo`）的 `m_PreserveAspect`。**
   - 已证：prefab `styleImage` 运行时喂 `alternateArtStyles[i]`；两张风格 logo 已导进工程（512×128）。
   - 没证：那颗 `Image.m_PreserveAspect` 的值（`CollectionWindow.cs:1741-1748` 已如实标「本地没有判据」）。

9. **`Deck Selection Popup` 的「选中金环」在该窗是否显示。**
   - 已证：本窗用的变体 `Collection Deck With Highlight` 的 `useSelectedHighlight = 1`；`CollectionDeck.SelectedHighlight` 另有一道 `if (!sortMode)` 闸（`CollectionDeck__SelectedHighlight.c`）。
   - 没证：该窗列表运行时是不是 `sortMode`；实拍 12 格**没有一格是金的**（但可能只是当时没有选中项）。
   - ⇒ 我们按 `pick == 当前卡组` 画金环（`DeckSelectionPopup.cs:855-857`）。

10. **`Collection Card/Content/CardUI` 的运行时真实尺寸。**
    - 序列化算出来是 **0×0**（`BasicCardUI` 6 个组件、`2DCard` localScale 150），真实尺寸由 `Card2DController` 运行期装配。
    - 我们走 `CardView`（自建卡面），**没有按 prefab 逐层复刻那 6 个脚本的装配**；判据 = `阶段二_卡组线_原版规格.md` §八·3 已记「查不到」。

11. **本次没有重跑自检 / 没有起 player。** 所有「一致」都是**静态逐值对账**（prefab 字段 vs 代码常量），⛔ 不代表画面已验收。

12. **`Card Detail` 两块功能面板的显隐条件。**
    - 已证：`CraftingPanel.Initialize.c:42` 有一处 `GameObject.SetActive(…, *(DAT_1842963f8 + 0xb8))`；`CraftingPanel__CheckButtons.c:33/50` 还有一处关、一处开。
    - 没解：`DAT_1842963f8 + 0xb8` 指向哪个静态字段、`CheckButtons` 的判定入参是什么（要再往下追一层）。
    - ⇒ 我们按稀有度（`Craftable(Card.Rarity)`）决定建不建「创建副本」，**与原版条件未必同构**。

---

## 四、顺带发现：文档与实现不符（**铁律 5 那一类，非功能缺陷**）

> 这些是**注释/文件头过期**，代码本身是对的。按铁律 5 应就地订正（本代理只读，未改）。

| # | 文件:行 | 文档写的 | 实际（判据） |
|---|---|---|---|
| S1 | `Shell/CollectionWindow.cs:1208-1211` | 「`Cardback Shadow SDF` **图本地有、喂法也查清了，但没画 —— 这是缺口**」「`Empty Collection Warning` 照纪律不建」 | SDF 已在 `:1319-1396` 建（2026-09-26）；空态提示已在 `:1285-1295` 建（恒关、可触发） |
| S2 | `Shell/CollectionWindow.cs:1553-1554` | 「点一张卡 ⇒ 那扇窗**还没建** ⇒ `NotifyNotBuilt`」「`Card Filters` 的 `Army Filter` 一格**仍然没建**」 | 详情窗已建（`OpenCardDetail` → `CardDetailPopup`）；Army Filter 已在 `:3544-3660` 建（2026-10-03 A11） |
| S3 | `Shell/MainMenuRuntime.cs:31-41` | 「已知缺口：**模式卡**（下一批）」·「**字号**：按矩形高推的档位，标注为我们挑的」 | 模式卡已建（`:2027-2260`）；字号已按 `主菜单_原版规格.md` §七 7·2 实读（`Player Name 32` / 导航 `33` / `Player Level Text 37.2`） |
| S4 | `Core/FilterPanelModel.cs:22-25` | 「七行行顶 0 / 79.02 / 129.02 / 179.02 / **329.02 / 609.02 / 839.02**，内容总高 **989.02**，可滚 64.92」 | 现算 `ComputeLayout(13)`：Rarity **729.02** · Cost **1009.02** · Type **1239.02** · ContentH **1389.02**（Army 行按 13 格 = 550 高算）⇒ 可滚 **465**。那组旧数 = Army 行按 150 算的那一版 |
| S5 | `Shell/CollectionWindow.cs:22-48`（文件头「建了什么」） | 「⏭ `Deck Selection Popup with Tabs`（最后一个）」「⏭ 卡片详情窗（…够建 80%）」 | 两扇都已建（`Shell/DeckSelectionPopup.cs` / `Shell/CardDetailPopup.cs`，2026-09-24） |

---

## 五、结论摘要

- **逐值对账的 79 个检查点里，绝大多数（≈68）与原版 prefab **一致**，且多数有「我们代码里写死了出处 + 断言」的双保险。**
- **真正的缺口只有两处**（都低）：`Collection Card` 的 `New Card Badge` / `Ban Icon` 两个节点没建、`Card Ready for level up` 没建 —— 三个都是**数据源缺失**（新卡表 / 禁卡表 / 升级表），实拍上也看不到。
- **需要人裁的有三处**：
  1. `Deck Selection Popup` 的两个页签（prefab 画、实拍看不到）—— 见 §三·1；
  2. 该窗标题「选择一个卡组进行对战」vs 实拍「选择卡组」—— 见 §三·6；
  3. `Deck Selection` 选中金环是否该画 —— 见 §三·9。
- **另有两条判不出**：`Card Detail` 两块功能面板的显隐条件（§三·12）· 模式卡映射与行 y（§三·4，已标我们挑的）。
- **五处文档过期**（S1–S5）建议按铁律 5 就地订正（否则下一个会话会照旧注释去「补」已经做完的东西）。
