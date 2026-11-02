# B121-COLL-A · 收藏窗（壳 + 卡池 / 筛选 / 卡背 / 异画 / 头像）逐项审计

> 代理：`B121-COLL-A`（只读审计）· 日期 2026-11-03 · 只读 `d:/2` 与 `Unity/MyGame/Assets`，未跑 Unity、未动 git、未改任何源码/账。
> 范围：`Collection Menu Variant`（`CollectionScreen`）的**窗壳与四页签 · 卡池页 · 筛选面板 · 卡背页 · 异画页 · 头像项**。
> 卡组页 / 卡组编辑 / 卡组弹窗**不在本报告**（别的代理在做）。

## 结论

窗壳、四页签、Back 钮、ESC、左导航高亮、卡池网格/滚动/计数条/ban 徽标/详情点击、卡背网格与点击、异画网格/箭钮/单档禁用、异画页出厂筛选 —— **基本对齐原版**。
量出 **10 条不符**，其中 **3 条用户可见**：① **卡背页左抽屉两行上下颠倒了**（原版 Owned 在上、Army 在下；且缺 `Army` 小标题）② **筛选 chip 没有 5 个上限** ③ **未拥有/禁卡卡面的灰度没画**（旧注释的理由已过期 —— `CardView.SetGrey` 现成可用）。其余 7 条是树形层级、懒建、等价实现与潜在项。

覆盖：**1 扇窗 · 4 页签 · 4 页 · 6 组筛选 · 52 项**。
档位计数：**✅符合原版 41 · 🟠我们没实现 1 · ⚠️实现了但参数/行为不符 9 · ❓判据查不到 1**。

## §主表（一行一项）

| # | 它是什么 | 功能是否实现 | 是否符合原版 | 原版参数（出处） | 我们参数（文件:行） | 差异 | 档位 |
|---|---|---|---|---|---|---|---|
| **窗壳与页签** | | | | | | | |
| 1 | 窗根参数（type / placement / closeOnESC / extraScaleSmallScreen / 关窗音） | 是 | 是 | Fullscreen(0) · Canvas(5) · 1 · 1.0 · `useDefaultCloseSoundIfNull=0`（prefab 根 MB + `CollectionScreen` 序列化） | `Shell/CollectionWindow.cs:171-177` | 无 | ✅ |
| 2 | 左栏 `Tab Buttons` 容器（顶边 158.6 · VLG padTop 30 · 四键各 180 高） | 是 | 是 | 现读 `Tab Buttons` = 167.2,158.6→332.2,1080 · 四键 180 | `CollectionWindow.cs:138`（BarTop 158.64）· `:143`（padTop 30）· `MainMenuSubmenuWindow.cs:21`（TabBtnH 180） | 无 | ✅ |
| 3 | 四页签文案 `Deck / Cards / Cosmetics / Styles` | 是 | 是 | 四颗 `TabButtonLabel.m_text` 逐颗现读；小写源码 `Deck`，`m_fontStyle=16`(UpperCase) ⇒ 屏上 `DECK` | `CollectionWindow.cs:118-128`（含 `term`，走 `Loc.T`） | 无 | ✅ |
| 4 | 四页签图标 sprite | 是 | 是 | `40k_collection_bt_{decks,cards,cosmetics,style}`（`Icon` 126×126 · Simple · PA=1） | `CollectionWindow.cs:120-127` | 无 | ✅ |
| 5 | 页签文字样式（fs36 · auto[5~36] · base23 · 色(0.957,0.882,0.675) · 字距 −3.5 · Center/Midline · 折行0） | 是 | 是 | 四颗逐颗同值（`menu_dump` 现读） | `MainMenuSubmenuWindow.cs:827-846`（色/字号在 `Text(...)` 实参，`SetCharSpacing(-3.5)` + `SetVAlign(Midline)`） | 无 | ✅ |
| 6 | `Badge Highlight` 页签徽标 | 建了节点、恒不显示 | 等价 | 四份 `UiBadgeNotification.notificationDefinitions` **全 = []**（MB `-426951423847111979`/`-5943276368383515947`/`1285835444701881045`/`297651604455482069` 现读） | `MainMenuSubmenuWindow.cs:848-866`（建节点、alpha 0；`BadgeWiring.DefinitionsForTab` 对这四颗无定义） | 原版恒 alpha 0，我们恒 alpha 0 | ✅ |
| 7 | 默认页 = Deck | 是 | 是 | `Tab Buttons/Deck` 的 `EverguildToggle.isOn = 1`，其余 0 | `CollectionWindow.cs:3425-3427`（`StartVisualIndex()`，新实例 `CurrentTab==None` ⇒ 0） | 无（重开保留当前页 = 原版 `GameWindowWithTabs.Open` 字段 0x12 语义） | ✅ |
| 8 | 页容器显隐 / 切页 | 是 | 是 | 四页节点 `Select Deck Tab`/`CardsTab`/`Cardback Tab`/`Alternate Art Tab`，运行时由 `GameWindowWithTabs` 收口 | `Shell/GameWindowWithTabs.cs:268-300`（`ChangeTab` 先全关再开目标） | 我们**懒建**未访问页（原版 prefab 页全在）— 性能取向，切页首帧多一次建树 | ⚠️（见问题 5） |
| 9 | `Shared/Close Button`（150×60 · 文案 `Back` fs40 auto[10~40] base12 · Center/Capline · 折行0） | 是 | 是 | 现读 192.2,83.4→342.2,143.4；`Button Text` 200.5,89.3→333.4,137.5（132.86×48.24） | `CollectionWindow.cs:3371-3393`（常量）· `:3895-3962`（建树 + 文案 + 折行 + 纵向档），父链 `Tabs/Shared/Close Button/Button Text` | 无 | ✅ |
| 10 | Back 钮点击行为（Deck 页签且已选模式 ⇒ 回模式页；否则关窗） | 是 | 是 | `CollectionScreen.BackButtonClick`（ILSpy `:83-125`）：当前页是 `SelectDecksTab` ⇒ 转 `BackButtonClicked()`，否则 `ESCPressed()` | `CollectionWindow.cs:4655-4666`（`OnBackClicked` + `ShouldReturnToDeckModePage`） | 无 | ✅ |
| 11 | ESC 关窗 | 是 | 是 | `closeOnESC = 1` | `CollectionWindow.cs:173` + `Shell/PointerLayer.cs:869-912`（ESC → `w.ESCPressed()`，门槛读 `closeOnEsc`） | 无 | ✅ |
| 12 | 左侧导航高亮（COLLECTION） | 是 | 是 | `CollectionScreen.Open` 调 `NavigationPanelController.SetToggle(Collection)`（`CollectionScreen.cs:51-56`；`CollectionScreen__Open.c:14`） | `Shell/MainMenuRuntime.cs:1463-1473`（`OpenCollection()` 后 `SelectNav` → `SetNavToggle`） | 无 | ✅ |
| **卡池页（Cards）** | | | | | | | |
| 13 | 视口 / 网格（桌面 6 列 262.5×384 · 小屏 4 列 393.75×576 · 居中 PadX 7.475） | 是 | 是 | RSR `_cellWidth 262.5` `_cellHeight 384` `_spacing 0` `_mobileSizeScale 1.5`；视口 330.2,155.9→1920,1080（1589.78×924.06） | `CollectionWindow.cs:193`（视口）· `:211`（格）· `:220`（列数 floor 式子）· `:228`（PadX） | PadX 我们 7.39（原版 7.475，视口差 0.17px 引起） | ✅ |
| 14 | 卡格内容（整张卡面 = `CardView` 全卡） | 是 | 是 | 原版格 = `Collection Card`（`CardUI/2DCard` + 卡框/立绘/文字/数值） | `CollectionWindow.cs:1597-1712`（`CardView.Create` + `SetPose` + `SetClipFromTree`，池化复用） | 无 | ✅ |
| 15 | 张数条 `xN` | 是 | 部分 | 原版 = `"x{GetOwnedCount(card)}"`（`CardCollectionDisplay__SetCell.c`）；未拥有 ⇒ 连条一起灰（`ToggleGreyScale`） | `CollectionWindow.cs:320-339`（`min(owned, 卡组上限)`）· `:1689`（`BuildCardsCounter`） | **印的值不是原始拥有数**（已知 🔶 自己设计，`CollectionWindow.cs:307` 已打标） | ⚠️（已裁定，仅供记账） |
| 16 | 禁卡 ban 徽标（`CardUI/Ban Icon` + `Banned Text`，默认关） | 是 | 是 | `BasicCardUI.ToggleBanned` = `SetActive(banned)`；sprite `40k_Cross_icon_cross_big Banned card`；源 `PlayEventData.IsCardBanned`（无活动恒 false） | `CollectionWindow.cs:6625-6685`（几何/文案/显隐全在） | 无（本 build 无活动 ⇒ 恒关 = 原版空态） | ✅ |
| 17 | 卡面灰度（未拥有 / 禁卡 ⇒ 整卡去饱和；未拥有 ⇒ 张数条也灰） | **否** | **否** | `CardCollectionDisplay__SetCell.c` 末段：`grey = !owned \|\| isBanned` ⇒ `greyscaleController.ToggleGreyScale(grey, 0f)`；计数条另走带补间那一档 | `CollectionWindow.cs:312-315`（**只记不画**）；`Core/CardView.cs:8335-8367`（`Grey` / `SetGrey(g,time)` 现成，`_GreyScale` 同原版材质属性） | 一格都没灰。**旧理由「ImageQuad 没有判据灰值」已过期** —— 卡面本来就是 `CardView`，它自带 `_GreyScale` | 🟠 |
| 18 | 空格点击 ⇒ 卡片详情窗（列表拖动中不响应） | 是 | 是 | `CollectionDisplay<T>.Select` 有「列表忙」闸；点击开 `CardDetailPopup` | `CollectionWindow.cs:1694-1697`（`ListBusy` + `PunchCardCell` + `OpenCardDetail`） | 无 | ✅ |
| 19 | 纵向滚动（Elastic · sens 100 · 视口裁切） | 是 | 是 | `ScrollRect m_MovementType=1`、`m_ScrollSensitivity=100`、`Viewport` 带 `RectMask2D` | `CollectionWindow.cs:1140-1159`（`MenuScroll` + `Elastic` + `ViewportClip`） | 无 | ✅ |
| 20 | `Reference Card Pointer`（隐形锚点 402.3,236.4→596.1,281.4，active=1） | 是 | 是 | 现读 `CardsTab ▸ Collection Display` 直接孩子；`m_IsActive=1`、无 Image | `CollectionWindow.cs:1133-1134`（`WcRefL/T/R/B` + `MenuTutorialFlow.RegisterUnique`） | 我们挂 `CardsTab` 下（原版挂 `Collection Display` 下） | ⚠️（见问题 5） |
| 21 | `WIldcard Display`（400×85 · 四组 65×44 · 阵营旗 80×85） | 是 | 是 | 1470,70.9→1870,155.9；`Counters` 1555..1870（四组 1565/1640/1715/1790，各 65 宽）；`Army Icon` 1470..1550 | `CollectionWindow.cs:1029-1119` | 无 | ✅ |
| 22 | 阵营旗随滚动焦点换图（`CheckFocusedArmy`，取第一个命中的格） | 是 | 是 | `CardCollectionDisplay__OnListDrag` → `CheckFocusedArmy`；`RectExtensions.OverlapsAny` 取枚举序第一个 | `CollectionWindow.cs:5081-5149`（滚动 `OnChanged` 与重建尾巴两处都指向同一纯函数） | 未命中不还原（原版同） | ✅ |
| 23 | 焦点换图前 0.1s 延时 | 否（直接算） | 等价 | `CardCollectionDisplay.RefreshCollection` 尾 `DOVirtual.DelayedCall(0.1f, CheckFocusedArmy)` | `CollectionWindow.cs:1736-1741`（同帧直接调，注释已写明「批处理无帧循环」） | 真 Play 下少等 0.1s；布局是同步的 ⇒ 可见结果同 | ✅（等价） |
| 24 | 空态警告 + 文案（`There are no cards in your collection for the selected filters`，fs36） | 是 | 是 | 135.2,70.9→1970,1080；`filteredCollection.Count < 1 ⇒ SetActive(true)` | `CollectionWindow.cs:1188-1200`（走 `Loc.T("MenuCollection/NoCardsFound")`） | 父节点是页（原版挂 `Scroll View` 下） | ⚠️（见问题 5） |
| 25 | 页头（`Filter Toggle` 50×50 + label fs42 + `icon detail` 30×30 + 分隔线 + `Clear filters` 250×60 @612.2） | 是 | 是 | 现读 367.2,88.4 / 437.2..587.2 / 612.2..862.2 | `CollectionWindow.cs:3513-3659`（四页共用；字号按页 42/42/35/42） | `ClearFltY` 我们 83.5（原版 83.4，0.1px） | ✅ |
| **筛选面板（Cards / Styles 共用 `Card Filters`）** | | | | | | | |
| 26 | 面板矩形 / 阴影 / 滚动视口 | 是 | 是 | 0.3,155.9→335.6,1080（335.31×924.06）；`Shadow` 宽 152.82 α0.314；`Scroll View` sens 50 | `CollectionWindow.cs:420-421` · `:448` · `:2772-2799` | x1 我们 0.25（原版 0.3，0.05px） | ✅ |
| 27 | 七行结构与行高（Name 79.02 · Owned 50 · Upgradable 50 · Army · Rarity 280 · Cost 230 · Type 150） | 是 | 是 | `Filters` VLG：0/79.02/129.02/179.02/329.02/609.02/839.02；`Army Filter` 预制高 150 但 `CSF MinSize` + `Content` CSF 会在 13 格就位后长到 550 | `Core/FilterPanelModel.cs:42-48` · `:63-87`（`ArmyRowH` 由格数算 = 50+5×100 = 550） | **预制模板总高 989.018，运行期应为 1389.02**（我们按后者）——两者都 > 924 ⇒ 都可滚 | ✅（见 §查不到 1） |
| 28 | 抽屉内容可滚 | 是 | 是 | 内容 989.018（模板）> 视口 924.06 | `CollectionWindow.cs:2794`（`MenuScroll.TopAligned(FltView, ContentHFor(state), 50)`） | 我们内容更高（见 27） | ✅ |
| 29 | 抽屉开合动画（0.3s · 关时滑出 · 幂等早退） | 是 | 是 | `CollectionFilterController.Toggle`：`hiddenPosition(-550,0)` · `animationTime 0.3` · `DOAnchorPosX` + `OnComplete` | `CollectionWindow.cs:446-447`（行程 −385 = −550 −(−165)）· `:623-670` · `:2816-2826` | 行程按「位移量」算（几何等价），非照搬绝对锚点 | ✅ |
| 30 | `Clear filters` 显隐（有任意筛选选中才显示） | 是 | 是 | `NotifyFilterChange` 末句 `clearFiltersButton.SetActive(0 < toggleOptions.Count)` | `CollectionWindow.cs:3716-3752`（判 `!State.Filter.IsEmpty`，不含 Owned/Upgradable） | 用 `SetActive` 而非原版「平移到 hiddenPosition」（我们有静态命中框，必须同时关闸） | ✅（等价，已注明） |
| 31 | `Clear filters` 实际清什么 | 是 | 是 | `ClearFilters`：逐个 filter 调 `ClearFilters()`；`CollectionFilter.ClearFilters()` 基类**空实现**，`UpgradableCardFilter` 不覆写 ⇒ **两个开关不动** | `CollectionWindow.cs:2841-2853`（显式保留 `Owned`/`Upgradable`） | 无 | ✅ |
| 32 | 选中项 chip 上限 5 | **否（无上限）** | **否** | `NotifyFilterChange` 调 `FillToggleList(toggleOptions, **5**)`（`decomp_generic_trial/out/CollectionFilterController_object___NotifyFilterChange__15f01a0.c:110`），页头那一轮再判 `if (5 < toggleOptions.Count) break`（同文件 `:141`） | `Core/FilterPanelModel.cs:831-853`（`AppendChips(..., int listLimit = int.MaxValue)`）+ 四行调用点都没传第 10 参 | 选 6 个及以上时我们全画，原版只画 5 | ⚠️ |
| 33 | 页头 chip（`filterIconHolder`） | **否** | 未知 | `filterIconHolder` 的 PPtr 现解到页头 `Filters` 节点（MB `-3404574427676351787` → RT `6123318184248140501` → GO `Filters_1419059127639990997`，GridLayoutGroup 85×70）；`NotifyFilterChange.c:154` 把克隆件 `set_parentInternal` 到它 | `CollectionWindow.cs:3598-3601`（`Filters` 只放了 `Clear Filter Button`，不接 chip） | 我们只有行内 chip，页头空 | ❓（见 §查不到 2） |
| 34 | 卡名搜索：六项子串 · 结果反序 · 空白直通 | 是 | 是 | `CardNameFilter.CheckInputTextSearch`：六项 `Contains`（卡名/效果(剥<>)/阵营/种族/资料片/稀有度），`i = size-1; i--` ⇒ 结果反序；空白直接返回全集 | `Deck/DeckEditorState.cs:629-640`（`if (needle != null) outList.Reverse();`）· `:660-690`（`NameSearchMatch`） | 多查一条裸阵营 id（只会多命中，已注明） | ✅ |
| 35 | `Owned only`（出厂开、本作恒真） | 是 | 等价 | `CardCollectionTab.showOnlyOwned = true`；本作全解锁 ⇒ 筛不掉任何一张 | `CollectionWindow.cs:1239-1240` · `DeckEditorState.cs:611-617` | 开关图形对、结果集无差 | ✅ |
| 36 | `Upgradable only`（恒空） | 是 | 等价 | `UpgradableCardFilter.Filter` = `Where(CheckForCardUpgrade)`；本作无升级系统 ⇒ 恒 false | `DeckEditorState.cs:617`（`if (f.Upgradable) continue;`） | 打开必然空 = 原版等价（已备注） | ✅ |
| 37 | 选项表（Army 13 / Rarity 5 / Cost 8 区间 / Type 3） | 是 | 是 | 各 `Card*Filter.options` 实读；Cost 是 `1-/2..7/8+` 区间；Type 只 3 档 | `FilterPanelModel.cs` 的 `Build(...)` 四段 | 无 | ✅ |
| **卡背页（Cosmetics）** | | | | | | | |
| 38 | 网格（桌面 6 列 250×405 · 小屏 5 列 300×486 · PadX 42.29） | 是 | 是 | RSR `_mobileSizeScale 1.2`；视口 335.4,155.9→1920,1080（1584.56×924.06） | `CollectionWindow.cs:1792-1807` | PadX 42.29 vs 42.365（视口差 0.01px） | ✅ |
| 39 | 格内容（`Cardback Shadow SDF` + `Cardback`，各自 PA=1 内接） | 是 | 是 | 两层兄弟序 + 各自 `m_PreserveAspect = 1` | `CollectionWindow.cs:1907-1988` | 无 | ✅ |
| 40 | 点格 ⇒ `Item Display Window`（卡背） | 是 | 是 | `CardbackCollectionTab__OnItemSelected.c:17-24` 构造 `ItemDisplayWindowContext(item,1)` | `CollectionWindow.cs:1999-2003`（`OpenItemDisplay(ItemDisplayWindow.ForCardback(...))`） | 无 | ✅ |
| 41 | 空态 + 文案（`There are no cardbacks…`） | 是 | 是 | 170.4,70.9→1970,1080；`MenuCollection/NoCardbackFound` | `CollectionWindow.cs:1874-1885` | 父节点是页（原版挂 `Scroll View` 下） | ⚠️（见问题 5） |
| 42 | `Your cosmetics collection` 标题（fs38 · 右对齐到 1821.01 · y 80.94..140.94） | 是 | 是 | 现读 `Cardback Tab ▸ Header ▸ label`（`m_HorizontalAlignment=4`、宽 0） | `CollectionWindow.cs:1836-1846`（`Loc.T("MenuCollection/Label/Cosmetics")` + `AlignRightOn(1821.01)` + 兄弟序 2） | 无 | ✅ |
| 43 | `Cosmetic FIlter` 抽屉：两行**顺序**与 `Army` 小标题 | **是（顺序错、缺标题）** | **否** | 现读 `Cosmetic FIlter ▸ Filters` 孩子序 = `Spacing(15) → Owned Toggle (1)(50) → Army Filter(345)`；`Army Filter ▸ Title` = `'Army'` fs32（225.9..275.9） | `Core/FilterPanelModel.cs:1131-1160`（`armyTop = CosmoSpacing1` 先画 13 格，`Owned` 画在 `CosmoOwnedTop` = 15+550+12.81 ≈ 578）；`CollectionWindow.cs:3023-3035`（cosmo 分支**不建**标题） | **两行上下颠倒 + 少一颗 `Army` 小标题**，另有原版没有的 12.81 间距（那是卡组编辑窗那一份的） | ⚠️（用户可见） |
| 44 | `Cosmetic FIlter` 起手关 / 不滚动 | 是 | 是 | prefab `act=F` + `Initialize` 里一律 `SetActive(false)`；内容 15+50+345 = 410 < 924，**无 Scroll View** | `CollectionWindow.cs:2014-2056` | 无 | ✅ |
| **异画页（Styles）** | | | | | | | |
| 45 | 网格（桌面 6 列 262.5×384 · 小屏 4 列 · 视口 330.2,287.7→1920,1080） | 是 | 是 | RSR `_mobileSizeScale 1.5`；视口高 792.33 | `CollectionWindow.cs:2166-2181` | 无 | ✅ |
| 46 | 左右箭钮（74.386×75.605 · y190.7..266.3 · x683.4 / 1320.4 · 左钮镜像） | 是 | 是 | 现读 `Select Art Button {Left,Right}`；左钮 `Icon` 带 `UIFlippable(m_Horizontal=1)` | `CollectionWindow.cs:2184-2191` · `:2422-2458`（`SetUvRect(1,0,-1,1)` 镜像） | 无 | ✅ |
| 47 | `Art Style Logo` 512×128（787.6..1299.6 × 155.9..283.9） | 是 | 是 | 现读 `Art Style Logo`（`m_Sprite=null`，运行时 `styleImage` 喂 `AlternateArtStyleIconsSO`） | `CollectionWindow.cs:2187` · `:2505-2550`（走 `StyleLogo(style)` → `Cell_Shaded_…` / `WarpForgeLogo_Style`） | 无 | ✅ |
| 48 | 风格切换（idx±1 回绕 · 单档时两钮 `interactable=false`） | 是 | 是 | `AlternateArtCardCollectionTab.cs:193-197,105-146`（`idx<0⇒Count-1`，`≥Count⇒0`） | `CollectionWindow.cs:2457`（单档 `Interactable=false`）· `:2461-2473`（回绕 + 单档早退） | 无（本机 1 档 ⇒ 常态是禁用） | ✅ |
| 49 | 切换风格后**真的换异画图** | 部分 | 部分 | 原版 `AlternateArtCard.style` + `cardImage` 逐条绑定 ⇒ 取图按「卡 + 风格」 | `Core/CardArt.cs:1035-1039`（`AltArt(cardId)` **只按卡 id**）· `CollectionWindow.cs:2648` | 同一张卡若有两种风格 ⇒ 两档会取到同一张图（本机只有 `HammerAndBolter` 一档 ⇒ 今天不可见） | ⚠️（潜在） |
| 50 | 异画页出厂 `Owned only = false`（另三页 true） | 是 | 是 | `AlternateArtCardCollectionTab` 的 `showOnlyOwned` **无字段初值**（`.ctor.c` 不写该偏移） | `CollectionWindow.cs:6846-6872`（`AltArtInitialFilter` + `ResetPageFiltersToFactory`） | 无 | ✅ |
| 51 | 异画页空态（`…cards…`，330.2,287.7→1920,1080） | 是 | 是 | 现读 `Alternate Art Tab ▸ Collection Display ▸ Scroll View ▸ Empty Collection Warning` | `CollectionWindow.cs:2379-2390` | 父节点是页（原版挂 `Scroll View` 下） | ⚠️（见问题 5） |
| **头像** | | | | | | | |
| 52 | 收藏窗的「头像」页 / 入口 | **原版无此页，本项不适用** | — | `Collection Menu Variant ▸ Tab Buttons` 的直接孩子**只有 4 颗**（Deck / Cards / CardBacks / Alternate Art，现读 4/4）；头像页属于**玩家档案窗**：`AvatarTab : ProfilePage`（`Shell/AvatarTab.cs:1-40`，`WindowTabType.ProfileAvatar`，挂 `PlayerProfileWindow`，`ProfilePage` 基类） | 我们也没有头像页（同构） | 无 | ✅（不适用） |

## §问题清单（用户可见优先）

1. **卡背页左抽屉两行上下颠倒，且缺 `Army` 小标题**（用户一眼可见）
   - 原版判据：`menu_dump.py bundle_menus_assets_all "Cardback Tab" --depth 5/7` 现读 —— `Cosmetic FIlter ▸ Filters` 孩子序 = `Spacing 15 → Owned Toggle (1) 50 → Army Filter 345`；`Army Filter ▸ Title`（`'Army'` fs32，225.9..275.9）；`Content` 0.1,285.9→335.6,565.9（Grid 100×100 · 3 格/行 · pad L14 · spacing 7/0）。
   - 我们现状：`Core/FilterPanelModel.cs:1131` 把 13 阵营格画在 `CosmoSpacing1`(15) 起，`Owned` 画在 `CosmoOwnedTop` = 15+550+12.81 ≈ 578（`CollectionWindow.cs:1160` 一带调用）；`CollectionWindow.cs:3023-3035` 的 cosmo 分支**不建标题**。
   - 建议：给 `BuildCosmetics` 加「收藏窗口径」—— Owned 在 `15`、Title 在 `70`、Content 在 `130`；`CosmoSpacing2 = 12.81` 与「Army 在前」是**卡组编辑窗**那一份的值，按窗分参数（与 A247/D39 同一套做法）。

2. **筛选 chip 没有 5 个上限**
   - 原版判据：`FillToggleList(toggleOptions, 5)`（`decomp_generic_trial/out/CollectionFilterController_object___NotifyFilterChange__15f01a0.c:110`）+ `if (5 < toggleOptions.Count) break;`（同文件 `:141`）。
   - 我们现状：`Core/FilterPanelModel.cs:831` 的 `listLimit` 缺省 `int.MaxValue`，四个调用点都没传 ⇒ 选 6 个阵营会画出 6 颗 chip（还会把行撑更高）。
   - 建议：`AppendChips` 四行调用点统一传 `5`；`listLimit` 缺省改成 5 并删掉「原版实参未解」的旧脚注（现在已经解出来了）。

3. **卡面灰度（未拥有 / 禁卡）一格都没画**
   - 原版判据：`CardCollectionDisplay__SetCell.c` 末段 `grey = !owned || isBanned ⇒ ToggleGreyScale(card, 0f)`；未拥有时另有计数条灰（带补间）。
   - 我们现状：`Shell/CollectionWindow.cs:312-315` 写着「`ImageQuad` 没有对应的判据灰值 ⇒ 只记不画」。**该理由已过期**：卡池格画的是 `CardView`，它自带 `Grey` / `SetGrey(g, time)`，材质属性就是原版那个 `_GreyScale`（`Core/CardView.cs:8335-8367`，去饱和口径见 `:8308-8332`）。
   - 建议：`RebuildCardsCells` 里按 `!owned || IsCardBanned(def)` 调 `v.SetGrey(1f, 0f)`；计数条那一档另议（要 `Label`/`ImageQuad` 的灰，判据值 `DAT_1834b2e7c` 仍未知）。原版异画那支极性看着是反的（`HasItem(altArt)==0 ⇒ 不灰`），照抄别修。

4. **Cards / Cardback / Styles 三页缺原版页容器层级**
   - 原版判据：`CardsTab ▸ Collection Display`（`CardCollectionDisplay`）→ `Scroll View` / `Empty Collection Warning` / `Card Filters` / `Reference Card Pointer`；`Cardback Tab ▸ Cardback Display` → …；`Alternate Art Tab ▸ Collection Display` + `Header`(332.3,155.9→1920,283.9)。
   - 我们现状：只有 Deck 页建了 `Collection Display`（`CollectionWindow.cs:4197`）；另三页的 `Scroll View` / `Card Filters` / `Empty Collection Warning` / `Reference Card Pointer` 全部直接挂页节点；异画页的箭钮条也**没有** `Header` 容器。
   - 影响：渲染逐位不变（都走绝对设计矩形），但**树形与自检路径**和原版不同，后续按原版路径取节点会取不到。建议建容器（空节点即可，零像素变化）。

5. **未访问页懒建**（`CollectionWindow.cs:3981-3989`）
   - 原版 prefab 四页全在、`Open` 只切页。我们首开只建第 0 页，其余页在第一次切换时建。可见后果 = 首次切到卡池/异画页那一帧有建树停顿（注释实测首开省 ~71s）。**这是性能取向的偏离，不是错**，但属「与原版不同的行为」，登记备查。

6. **异画取图不分风格**（潜在）
   - `CardArt.AltArt(cardId)`（`Core/CardArt.cs:1035`）只按卡 id。原版 `AlternateArtCard` = (卡, 风格, 图) 三元。今天只有 `HammerAndBolter` 一档（`CollectionWindow.cs:2193-2230`，7 张全同风格）⇒ 不可见；一旦出现第二种风格，两档会显示同一张图。建议把 `altarts` 的键改成「卡 id + 风格」。

## §查不到清单

| # | 问题 | 搜过什么 | 还差什么 |
|---|---|---|---|
| 1 | 筛选内容总高到底是 **989.018**（预制模板）还是 **~1389**（13 个阵营格就位后） | `menu_dump` 的 `CardsTab ▸ Card Filters ▸ Filters`（含 `CSF v:MinSize=989.018` 与 `⛔GRP-off` 标记）、`Army Filter` 的 `CSF v:MinSize=150`、`Content` 的 `CSF v:MinSize？`、`FilterPanelModel` 的 `ArmyRowH` 推导 | 跑一次原版把筛选栏展开、13 个阵营格就位后量 `Filters` 的 `sizeDelta`。本机无菜单运行期 dump（`runtime_ui_dump_*.tsv` 只覆盖战斗/Intro）。**两支都 > 924 ⇒ 「可滚」这条不受影响**，只影响「滚动距离 64.92 还是 465」 |
| 2 | 原版页头 `filterIconHolder` 上到底挂什么、长什么样 | `NotifyFilterChange.c`（`:110/141/154`）、`filterIconHolder` 的 PPtr 现解（→ 页头 `Filters` 节点，GridLayoutGroup 85×70 + `Clear Filter Button`）、`FillToggleList`（克隆到**同一父件**，即行内 chip）、`CollectionFilterToggle.Initialize`、`filterIconToggle` 相关源码 | 一张原版实拍：收藏窗筛选栏里选中 2~3 项后，页头 `Filters` 位置是否出现图标/块。`.c` 里那两轮循环（行内克隆 vs 页头 reparent）读不出「同一个 chip 被搬走」还是「额外复制一份」 |

## §顺手发现（只报不改）

1. 工程里有一张 **`40k_collection_bt_avatars.png`**（`CardPresentation/Art/原版/0_mainmenu/`），但 `bundle_menus_assets_all` 里搜不到该 sprite 名、`Collection Menu Variant` 的四颗页签也没有它 ⇒ **原版美术里有这张图、这一版 UI 不用它**。别据此给收藏窗加头像页（第 52 项已给反证）。
2. `CollectionWindow.cs:2497` 那条「`ImageQuad` 没有判据灰值」的注释与 `Core/CardView.cs:8335` 的 `SetGrey` 直接矛盾 —— 同族旧注释建议一并按铁律 5 就地订正（这不只是收藏窗的问题：任何「卡面灰度做不了」的记录都要重核）。
3. `Cardback FIlter` 那棵树的**形状**与 `CardsTab` 不同：卡背页的 `Filters` 是 `Cosmetic FIlter` 的直接孩子（**没有 `Scroll View`**），而卡牌/异画页是 `Card Filters ▸ Scroll View ▸ Viewport ▸ Filters`。我们 `RebuildFilterRowsNow` 已经按「无滚动区」分支处理（`:3011-3020`），这条只是提醒：**别把两页的裁剪/滚动口径合并**。
4. `FilterPanelModel.cs:1030` 自陈「行序与卡牌那套不同：Army 在前、Owned 在后」—— 这句话描述的是**卡组编辑窗**的卡背抽屉（那棵树的父链 `… ▸ Deck Editing Menu`，见同文件 `:1072-1074`），被当成两窗共用常量落进了收藏窗。问题 1 的根因就在这一格。
5. `CollectionWindow.cs:3533-3539` 的 `Filters` 页头字走 `Loc.T("MenuDeck/Filters/Filters")`，而原版四页 `Filter Toggle ▸ label` 的卡面文案是英文 `Filters` —— 中英两档都对，仅提醒别把这条 key 按页拆。
