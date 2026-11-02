# B121-COLL · 收藏界面逐项审计（主报告）

> 会话 2026-11-03 · 只读审计（未跑 Unity、未动 git、未改任何源码/场景/资产/账）· 代理 `B121-COLL` + 4 个并行子代理（A/B/C/D，分工见 §七）。
> 判据一手源：`d:/2/tools/decomp_full/*.c`（第一权威）· `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs` · `out_il2csharp`（逐方法，常更干净）· `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（prefab 字段）· `assetripper_out/ExportedProject`（`.prefab` 根与 YAML）。
> 全量逐项表在四份分报告里（§七 有路径）；本文件是合并结论 + 问题清单 + 卡组专章。

---

## 〇、一句话结论与计数

**收藏窗（四页签）与卡组三扇窗的「版面参数」已基本逐位复刻（✅181 / 222 条），但有 7 条真缺口 + 30 条行为/参数不符，且集中在「链路层与状态联动」——最要紧的是：导入窗实际粘不进卡组串、点一格卡组就悄悄换掉对战用卡组、Done 钮在不合法时仍印「完成」。**

| 块 | 覆盖 | ✅ | 🟠 | ⚠️ | ❓ |
|---|---|---:|---:|---:|---:|
| A 收藏窗壳 + 卡池/筛选/卡背/异画/头像 | 52 项 | 41 | 1 | 9 | 1 |
| B 收藏窗「卡组页」+ 列表/重排/规则校验 | 71 项 | 60 | 4 | 6 | 1 |
| C 卡组编辑窗 | 48 项 | 39 | 1 | 7 | 1 |
| D 卡组三弹窗（详情 / 选卡组 / 导入） | 51 行 | 41 | 1 | 8 | 1 |
| **合计** | **222** | **181** | **7** | **30** | **4** |

> ⚠️ 口径：✅ = 「我们代码取的值/行为 == 原版判据」；**✅ 里约一半是几何参数**，不等于「这一页复刻完了」。⛔ 别把本表当验收（缺并排实拍）。

---

## 一、主表（非 ✅ 逐项；✅ 项按组卷起）

| 界面/按钮 | 实现 | 符合 | 原版参数（出处） | 我们（文件:行） | 差异 | 档 |
|---|---|---|---|---|---|---|
| **窗壳 · 四页签 · Back · ESC · 左导航**（页签 167.2,158.6→332.2,1080；四键各 180；文案 Deck/Cards/Cosmetics/Styles；图标 126×126；默认页 Deck；Back 150×60 fs40；页签徽标原版恒空） | ✅ | ✅ | 见分报 A §主表 1–12 | `CollectionWindow.cs:118-177,3371-3393,3425-3427,3895-3962` | 无 | ✅ |
| 未访问页**懒建** | ✅ | ⚠️ | 原版四页全在 prefab，`Open` 只切页 | `CollectionWindow.cs:3981-3989` | 首次切页那一帧多一次建树（性能取向，非错） | ⚠️ |
| **卡池页**（视口 1589.78×924.06 / 格 262.5×384 / 6 列·小屏 4 列 ×1.5 / PadX 7.475 / Elastic sens100 / 卡格=整张 `CardView` / ban 徽标 / 空态 / 点格开详情 / Wildcard 400×85 / Reference Pointer 402.3,236.4→596.1,281.4） | ✅ | ✅ | RSR `MonoBehaviour_-1327870198181403947`；`CardCollectionDisplay__SetCell.c` | `CollectionWindow.cs:193,211,220,228,1029-1119,1133-1134,6625-6685` | PadX 7.39 vs 7.475（视口舍入） | ✅ |
| 卡面**灰度**（未拥有或禁卡 ⇒ 整卡去饱和；未拥有时张数条另灰） | ❌ | ❌ | `CardCollectionDisplay__SetCell.c` 末段：`grey = !owned 竖线 isBanned ⇒ ToggleGreyScale(grey, 0f)` | `CollectionWindow.cs:312-315`（只记不画）；`Core/CardView.cs:8335-8367` 有现成 `SetGrey` | **一格都没灰**；旧理由「ImageQuad 没有判据灰值」**已过期**（卡面是 `CardView`，属性就是 `_GreyScale`） | 🟠 |
| 张数条 `xN` | ✅ | ⚠️ | `x{GetOwnedCount}` | `CollectionWindow.cs:320-339` = `min(owned, 卡组上限)` | 值不同；已打 `🔶 自己设计`（`:307`）且用户拍板，仅记账 | ⚠️ |
| Cards/Cardback/Styles 三页**缺页容器层级**（`Collection Display` / `Cardback Display` / 异画第二个 `Header`）；`Empty Collection Warning`、`Reference Card Pointer` 直接挂页节点 | ✅ | ⚠️ | `Collection Menu Variant` 子树 | `CollectionWindow.cs:4197` 只有 Deck 页建了容器 | 渲染逐位不变；树形与按原版路径取节点取不到 | ⚠️ |
| **筛选面板七行**（Name 79.02 / Owned 50 / Upgradable 50 / Army / Rarity 280 / Cost 230 / Type 150；内容 > 视口 ⇒ 可滚；滑动 0.3s hidden(−550,0)；Clear 显隐；Clear 不重置 Owned/Upgradable；六项子串 + 结果反序 + 空白直通；Army13/Rarity5/Cost8/Type3） | ✅ | ✅ | `CollectionFilterController.cs:159-207` · `CardNameFilter.cs` · `NotifyFilterChange.c` | `Core/FilterPanelModel.cs:42-87,831-865`；`DeckEditorState.cs:629-690` | 无（内容总高 989 vs ~1389 待实测定案，见 §五） | ✅ |
| 选中 chip **5 个上限** | 有 chip | ⚠️ | `FillToggleList(toggleOptions, 5)`（`…NotifyFilterChange__15f01a0.c:110`）+ `if (5 < count) break`（`:141`） | `FilterPanelModel.cs:831` `listLimit = int.MaxValue`，四调用点都没传 | 选 6 个以上会全画（行被撑高） | ⚠️ |
| 页头筛选 **chip**（`Header/Filters`：Grid 85×70 · spacing 5,0 · FixedRowCount） | ❌ | ❌ | `filterIconHolder` → 页头 `Filters`（GO pid `1419059127639990997` 等 5 份） | `CollectionWindow.cs:3598-3601` 只放 `Clear Filter Button` | 四页都没有 chip | 🟠 |
| **卡背页**（250×405 / 6 列·小屏 5 列 ×1.2 / PadX 42.29 / 两层 SDF+Cardback / 点格开 `Item Display Window` / `Your cosmetics collection` fs38 右对齐 1821.01 / 起手关、无滚动区） | ✅ | ✅ | RSR `MonoBehaviour_7909812686046290645`；`CardbackCollectionTab__OnItemSelected.c` | `CollectionWindow.cs:1792-1807,1836-1846,1907-2003` | 空态父节点同上一格 | ✅ |
| 卡背页 `Cosmetic FIlter` **两行顺序 + `Army` 小标题** | 有 | ❌ | `Filters` 孩子序 = `Spacing 15 → Owned Toggle (1) 50 → Army Filter 345`；`Army Filter ▸ Title = 'Army'` fs32 | `FilterPanelModel.cs:1131-1160`（Army 先画，Owned 落到 ≈578）；`CollectionWindow.cs:3023-3035` 不建标题 | **两行上下颠倒、少一颗 `Army` 小标题**，还多出原版没有的 12.81 间距（那是卡组编辑窗那份值） | ⚠️ |
| **异画页**（262.5×384 / 6 列·小屏 4 列 / 视口高 792.33 / 左右箭钮 74.386×75.605 / Logo 512×128 / idx±1 回绕 / 单档禁用 / 出厂 Owned=false / 空态） | ✅ | ✅ | `AlternateArtCardCollectionTab.cs:105-197` | `CollectionWindow.cs:2166-2191,2422-2473,6846-6872` | 无 | ✅ |
| 异画**取图不分风格** | 部分 | ⚠️ | `AlternateArtCard` = (卡, 风格, 图) 三元 | `Core/CardArt.cs:1035-1039` 只按卡 id | 出现第二档风格时会两档同图（本机 1 档 ⇒ 今日不可见） | ⚠️ |
| 收藏窗「头像」页 | 不适用 | ✅ | `Tab Buttons` 只有 4 颗；头像属玩家档案窗 `AvatarTab : ProfilePage` | 我们也没有 | 无 | ✅ |
| **Deck 页页头**（Filter Toggle 50×50 / Control Buttons 740×60 反排 Unlock→Import→Create / Create 245 / Import 245 / Unlock 186 三组件全 disabled / Clear 250×60） | ✅ | ✅ | `menu_dump` 现读；`SelectDecksTab.cs` il2csharp | `CollectionWindow.cs:3354,3513-3659,4053,4082-4166` | 无 | ✅ |
| `Import` 的 **interactable 闸（卡组满 114）** | ❌ | ❌ | `importDeck.interactable = 已拥有卡组数 < GameStaticData.totalCustomDecks(114)`（`SelectDecksTab__Setup.c:44-53`；`GameStaticData.cs:2419`） | `CollectionWindow.cs:4084/4139` 无 interactable | 满 114 套时原版灰掉、我们仍可点 | 🟠 |
| Deck 页 `Clear filters` **显隐** | ❌ | ❌ | `clearFiltersButton.SetActive(toggleOptions.Count > 0)` | `:3734 RefreshClearFilter(0)` 读 `PageDrawer(0).State`，而 `_deckFltSlide`（`:4834`）**没有 State** ⇒ 恒 false | **筛完清不掉**（钮永不出现） | ⚠️ |
| **卡组格**（225×364.5 + spacing 20 / 6 列·小屏 5 列 ×1.25 / PadX 69.55 / 主图=卡面插图 / 卡框 246×368 / 卡名 fs32 Capline 字距−2 / 阵营+模式图标 84.5×85.75 / 悬停 1.05 / 点击音 DeckClick 1 / 难度标不画 / 选中金环不画 / **原版卡组格没有「张数」**） | ✅ | ✅ | `CollectionDeck.cs` il2csharp `Config`；MB `-594148595981120468` | `Shell/MenuDraw.cs:3109-3212,3414-3589` | 无 | ✅ |
| 点一格卡组 = **改成「当前卡组」并写盘** | 多做 | ❌ | `CollectionDisplay.Select` 只写 `SelectedItem` + `OnItemSelected`，不落任何状态；改当前卡组只在弹窗 `DeckInfoControls.OnSelect` | `CollectionWindow.cs:5346 CollectionData.Select(i)` + 回滚 + `:5359` 开窗 | **玩家点开看看就把对战用卡组换了** | ⚠️ |
| 排序模式 `sortDeckMode` | 不可达 | ✅ | 原版字段是 `readonly bool`、ctor 只调 base ⇒ **恒 false，`SetSortMode` 是死支** | 生产无入口（只有 `Editor/ShellScene.cs:10708` 自检传 true） | 一致（都没有「进出排序模式」） | ✅ |
| **拖拽重排**（`allowItemDrag ∧ filtered.Count == 全量.Count`；最近格 → `SetSiblingIndex` → Remove+Insert；拖动中不悬停；有筛选整页失效） | ✅ | ✅ | `CollectionDisplay_object___SetCell__15ee7d0.c` · `__ReorderCollectionByHierarchy__15ed470.c` | `CollectionWindow.cs:6123,6196,6244,5068` | 无 | ✅ |
| 重排**落盘时机** | ✅ | ⚠️ | ① 普通模式点格时 ② `OnDisable` 关页时，且两表**不同才**写 | 松手即写（`:6310`） | 结果同；原版「拖完直接退出」不落盘 | ⚠️ |
| Deck 筛选抽屉（几何 335.50×924.06 不可滚 / Name Filter 行 80 / Army 345 / 名称 Contains IgnoreCase） | ✅ | ✅ | 子树现读 | `CollectionWindow.cs:4696-4834,4936` | 无 | ✅ |
| Deck 页**阵营筛选单/多选** | 单选 | ❌ | `DeckArmyFilter : ToggleCollectionFilter`，`Dictionary<CardArmy,bool>` 可多选同时生效（`ToggleCollectionFilter.cs:135`） | `_deckFacFilter` 是单 string（`:4876`） | 原版可同时筛多个阵营 | ⚠️ |
| Deck 空态 + 文案 + 活动/模式过滤 | ✅ | ✅ | `NoDecksFound` = `…for the selected filters or game mode`；`currentEvent` 过滤 | `:4932,4976`；`Core/Loc.cs:793` | null 档没有原版那条 `CustomDebug.Log` | ✅ |
| **模式子页**（页/Title fs72/Scroll 802.30/Content HLG spacing50；模式页关 Decks·Unlock·Create·Filter，开 Import；返回链） | ✅ | ✅ | `SelectDecksTab` il2csharp | `CollectionWindow.cs:4292-4432,4548,4640-4672` | 无 | ✅ |
| 模式子页的**卡内容** | 自定 2 张 | ❓ | `SelectGameModeDeckTab.Initialize` 按 LiveOps `IOwnDeckPlayEvent` 逐颗 instantiate（本地 0 事件 ⇒ 原版 `First()` 会抛） | `:4335 DeckModeCards` 硬编码 Skirmish/Classic（尺寸 505.93×802.67 有逐值来源） | 离线等价物；**未打 `🔶 自己设计` 标记** | ❓ |
| 进模式页的条件 | ❌ | ❌ | `ChooseTabToOpen`：事件数 >1 才进模式页，≤1 直接进列表 | 恒先显示模式页 | 0 事件下不可观测；接 LiveOps 要补 | ⚠️ |
| **卡组规则校验**（总张数 30/遭遇 12；同名上限 督军1·传奇1·其余2；传说总数；阵营限制无中立例外；督军位；防御卡；禁卡；持有数；准入 `inventoryOptions/showInCollectionOptions`） | ✅ | ✅ | `DeckUtility__ValidateDeckCards.c` · `CardDeck.cs:539-715` | `RuleEngine/Core/DeckRules.cs:165-500` | 无（`MissingCopies` 单机不可达已标注） | ✅ |
| `DeckError` 枚举粒度 | ✅ | ⚠️ | 原版 6 值 | 我们 ~15 值，全部映射同族 I2 键 | 判定结果一致，只文案更细 | ⚠️ |
| 卡组格 **Ban Icon + 名字变红 + 灰度 0.25** | ❌ | ❌ | `CollectionDeck.Config`：`bannedImage.SetActive(Any(...IsCardBanned))`；名色 (1,0.3,0.3)；`ToggleGreyScale(true, **0.25f**)` | `MenuDraw.DeckCell` 无 ban 层 | 本地 `CurrentEventData==null` ⇒ 原版不可达；接 LiveOps 必须补 | 🟠 |
| **卡组编辑窗结构**（3 页签 Cards/Deck info/Cosmetics，各 108.96×150；默认 Cards；Back 印 Back；Header 筛选+清空；通配符条；卡组名框；**ESC=保存**；开窗收导航/关窗放回） | ✅ | ✅ | `DeckEditingStateController.cs:9-14` · `DeckEditingWindow.cs:79-81,565-593,777-785` · `__ESCPressed.c` | `DeckRuntime.cs:1012,1397,1565-1774,1958-2004,5506` | 无（**没有「异画」页**：异画在卡牌详情窗里选） | ✅ |
| 左侧卡组列表（**`VerticalLayoutGroup` 单列** pad8 spacing2 居中；行 309×55.7；行内 费用圆/卡名/份数/稀有度条/ban/空槽底+字；费用→文化名排序；同名合并；sens 50；点击开详情；**拖出=删**） | ✅ | ✅ | `Deck Editing Menu.prefab` VLG fileID `114335036306139620` · `DeckListDrawer.cs:666-680` | `DeckRuntime.cs:233-254,502-530,4190-4220,4291-4300` | 无。⚠️ 简报里「Grid 360×58/3 列」属 `Deck info Popup.prefab`，不是本窗 | ✅ |
| 卡组行**行内拖动 = 重排** | 多做 | ❌ | 本 prefab 全树**无 `DragAndDropItem`**；原版行拖起即 `RemoveCard` | `DeckRuntime.cs:9178-9205` `State.TryMove` | 多出来的一支（依据疑似错挂 `Collection Deck` 那族）；会改写 `CardIds` 顺序 | ⚠️ |
| 卡池（262.5×384 / 6 列·小屏 4 列 ×1.5 / PadX 7.35 / 4–5 行；排序 阵营→非督军→费用→名；左键详情·右键加牌·拖拽加牌；五层显示闸；格内 `n/max`） | ✅ | ✅ | `MonoBehaviour_345129740864909092.json` · `DeckEditingWindow.cs:241-260,339-423` · `DeckEditorCollectionDisplay.cs:119-141` | `DeckRuntime.cs:816-833,3269-3300,5162-5205`；`DeckEditorState.cs:566-870` | 无 | ✅ |
| 卡池**按督军分流机制** | ✅ | ⚠️ | 机制 = Army 筛选预选 + `ShowOnlyHero` 且**无督军时把「卡类」整行 `SetActive(false)`**（`CardArmyFilter__SetFilterToDeck.c:15-45` · `CardTypeFilter.cs:20-40`） | 换成**内容硬闸** `WarlordGatedPool`（`DeckEditorState.cs:578-592`） | 稳态同；边角：①选别的阵营我们恒出空 ②无督军时卡类行仍显示 | ⚠️ |
| 战将槽/换督军/**防御卡槽** | ✅ | ✅ | `DeckEditingPanel.cs:175-242`（换督军直接替换、无确认、旧卡留到下次开窗清） | `DeckEditorState.cs:408-500` | 无 | ✅ |
| 统计/校验/保存/放弃/离场（`n/30`；费用曲线 9 行 分母10 末档 `8+`；Minions/Spells；`ValidateDeck` 假 ⇒ 错误窗不落盘；脏则「丢弃改动?」；离场**按来路回**） | ✅ | ✅ | `DeckEditingWindow.cs:613-731,787-854` · `DeckEnergyCostDrawer` | `DeckRuntime.cs:2440-2489,4481-4500,5300-5335,5550-5590` | 无 | ✅ |
| **Done 钮文案随合法性 Done↔Discard** | ❌ | ❌ | `UpdateDoneButton`：`valid ? MenuDeck/MenuButtons/Done : …/Discard`（il2csharp `DeckEditingWindow.cs:565`；官方表两键都在） | `DeckRuntime.cs:2489` 建一次后全文无改文处 | **不合法时仍印「完成」** | ⚠️ |
| 合法性「由假变真」的反馈 | 部分 | ⚠️ | 合法 ⇒ `Play2D(doneButtonEnabled)` + `Toggle(true,0.3)` + `DOPunchScale(0.05×,0.5s,5,1.0)`；首帧 `Toggle(valid, **0**)` 硬切 | `ToggleDoneHighlight` 只有 0.3s 淡入（`:8893-8905`） | 缺音效 + 弹跳；首帧也淡入 | ⚠️ |
| 「干净但内容不合法」的卡组按 Done/ESC | 部分 | ⚠️ | `syncedToServer!=0` 且库里有 ⇒ 直接 `Close()`（不校验） | `SaveAndSay` 不看脏标记，一律 Validate | 原版静默关，我们弹错误窗 | ⚠️ |
| 费用曲线柱宽动画 | ✅ | ⚠️ | 每格 `DOValue(…, 0.2s)` | 瞬时赋值（`ApplyCurveBarWidth`） | 缺一段增长动画 | ⚠️ |
| 卡背页（编辑窗内，250×405 / 6 列 / 装备=**右键**或拖到侧栏 / 侧栏已装备抽屉 / Owned 初值 false） | ✅ | ✅ | `DeckEditingWindow.cs:226-239` · `DeckEditingPanel.cs:160-173` | `DeckRuntime.cs:3395-3432,3670-3705` | 无 | ✅ |
| 卡背页**小屏系数** | ✅ | ⚠️ | `_mobileSizeScale = **1.2**`（`MonoBehaviour_-1923093106525475036.json`）⇒ 300×486 · **5 列** | `DeckRuntime.cs:3396` 乘 **1.5** ⇒ 375×607.5 · 4 列 | 开 Small UI 后才可见，格与列数都偏 | ⚠️ |
| 错误横幅 + 点击音（12 处 m_onClickSound；卡池格与卡组行无点击音） | ✅ | ✅ | `DeckEditingWindow.cs:251-252` · prefab `m_onClickSound` | `DeckRuntime.cs:5464-5471,7777,7941,8862-8866` | 无 | ✅ |
| `Upgradable only` 筛选 | ✅ | 🟠 | `Where(CheckForCardUpgrade)` | 依赖未实现的卡牌升级系统 ⇒ 恒空 | 跨线依赖（界面已出声） | 🟠 |
| **DeckInfoPopup**（窗参/压暗/红底/立绘/标题 Left/列表排序+防御卡钉首+叠块+份数/Edit·Select·Share·ShareOnChat·Delete·Duplicate·Practice·Switch 八钮及其显隐禁用） | ✅ | ✅ | `DeckInfoPopup__*.c` · `DeckInfoControls.cs:77-243` | `DeckInfoPopup.cs:700-983,1408-1500,1836-2031,2218-2756` | 无 | ✅ |
| Deck Info 抽屉缺 `Cardback Front` 一层 | 部分 | ❌ | 父 `Cardback`(↻+1.60°,×1.85) + 子 `Cardback Front`(↻−3.43°,209.56×302.33,中心偏 +11.2/−4.9)，两 Image 同贴图（`DeckInfoDrawer.cs:14-36`） | `DeckInfoPopup.cs:2109-2156` 只画父层 | 少一层（参数已在注释里，可直接补） | 🟠 |
| `Edit Deck` 在 `state==Import` 时换文案 | ❌ | ⚠️ | 键由 `state==1` 决定：`MenuDeck/MenuButtons/ImportDeck`（`DeckInfoControls.cs:84-95`） | `:809-819` 注释说「键未解出」——**已过期**，键本地就有 | Import 态仍印 Edit Deck | ⚠️ |
| `Share` 写剪贴板的内容 | ✅ | ⚠️ | `MakeDeckString` = 名 / 督军 / 每张 `Nx 卡名` / 末行 base64（`__MakeDeckString.c`） | `DeckRuntime.cs:6044-6050` 只写 base64 一行 | 玩家看到一长串乱码；载荷格式一致、互通不受影响 | ⚠️ |
| **DeckSelectionPopup**（窗参/两页签 260×67.64/`Random` 212.24×63.92/卡格 225×364.5 spacing20 6 列/选中态/点格开信息窗/Select 关窗回调/我的卡组按模式筛） | ✅ | ✅ | `DeckSelectionTabController__*.c` · `EverguildToggle` MB | `DeckSelectionPopup.cs:135-235,725-980,1255-1279` | 无 | ✅ |
| 选卡组空态文案 | ✅ | ⚠️ | 原版硬编码英文 `There are no deck in your collection for the selected filters`（无 `Localize`） | `:1006` 用官方 `MenuCollection/NoDecksFound`（多 s + or game mode + 随语言变） | 文案不符 | ⚠️ |
| 预组页内容范围 | 部分 | ⚠️ | 原版列 `practiceDecks` 全部合法项（含遭遇 12 张） | 只列经典 29 副（2026-09-26 用户拍板，因对战不支持 12 张） | 已知偏离 | ⚠️ |
| **ImportDeckPopup**（窗 560,234→1360,686/压暗 .396/标题 fs50 `Paste your deck`/输入框 MultiLine 无限长/解析 = 取最后一行 base64/取消与点背景关） | ✅ | ✅ | `Import Deck Popup.prefab` · `__TryImport.c:14-18` · `CardDeck__DeserializeDeckString.c` | `ImportDeckPopup.cs:49-320,367-397` | 见下行 | ✅ |
| **粘贴通道（Ctrl+V / 换行）** | ❌ | ❌ | 原版 `m_LineType=1 (MultiLineSubmit)` 靠平台粘贴喂 1–2 KB base64 | `Shell/PointerLayer.cs:169-232` 只吃 `Keyboard.onTextInput`；全仓**无 `systemCopyBuffer` 读点**（只有 `DeckRuntime.cs:6048` 的写） | **实际粘不进来、回车也输不进** ⇒ 导入功能实质不可用；`Share`→粘贴闭环断 | 🟠 |
| `Confirm` 钮的禁用态 | ❌ | ⚠️ | 出厂 `interactable=false`，`onValueChanged` 非空才亮并隐藏错误行（`__OnEnable.c`/`__ValidateField.c`） | `ImportDeckPopup.cs:241-261` 恒可点 | 空串点下去才报错 | ⚠️ |
| 导入**错误行文案** | ✅ | ⚠️ | 只用一条 `MenuDeck/Share/InvalidDeckString`（`Invalid deck string`） | `CollectionData.cs:278-296` 三条自拟键 `MenuDeck/Error/Import*` | 原版一条官方键，我们三条自拟 | ⚠️ |
| 导入**成功后的落点** | 部分 | ⚠️ | 解析成功 ⇒ 开 `DeckInfoPopup(State=Import)` 再关自己（**不落库**）；玩家在信息窗点「Import Deck」→ 编辑器才保存 | `ImportDeckPopup.cs:393-396` 直接 `CollectionData.ImportDeck` 落库 + 关窗 | **少一站**：看不到导进来的是什么、也没得改 | ⚠️ |
| 空串/空白导入 | 更严 | ❓ | 原版无空串守卫 ⇒ 会造出一个「无名无卡无督军」的畸形 `CardDeck` 并开信息窗 | `CollectionData.cs:283` 直接报 `ImportEmpty` | 我们有意的偏离，**需拍板**（§四 P3 口径） | ❓ |

---

## 二、问题清单（用户可见优先）

> 每条 = 一句话问题 + 原版判据 + 我们现状 + 建议改法。⛔ 未改任何代码。

1. 🔴 **导入窗「粘不进卡组串」（功能实质不可用）**
   原版：`Import Deck Popup.prefab` 的 `TMP_InputField m_LineType=1(MultiLineSubmit)`，靠 Ctrl+V 喂 1–2 KB base64。
   我们：`Shell/PointerLayer.cs:169-232` 文本焦点只吃 `Keyboard.current.onTextInput`，`TypeChar`/`OnText` 对 `char.IsControl` 直接丢；全仓 `Assets/CardPresentation` **没有任何 `systemCopyBuffer` 读点**（只有 `DeckRuntime.cs:6048` 的写）。
   改法：在文本焦点里补 `GUIUtility.systemCopyBuffer` 的 Ctrl+V 支 + 允许 `\n`；并把 `Share` 复制的串接回本窗（否则分享闭环断）。

2. 🔴 **点一格卡组就改「当前卡组」并写盘**
   原版：`CollectionDisplay_object___Select__15ed9c0.c` 只写 `SelectedItem` + `OnItemSelected`；改当前卡组只在 `DeckInfoControls.OnSelect`。
   我们：`CollectionWindow.cs:5346 CollectionData.Select(i)` 先写盘再开窗。
   改法：`SelectDeck(i)` 去掉 `CollectionData.Select`，只留 `OpenDeckInfo`；写入只保留 `DeckInfoPopup.cs:2245` 那一处。

3. 🔴 **Deck 页 `Clear filters` 钮永不出现**
   原版：`NotifyFilterChange` 末句 `SetActive(0 < toggleOptions.Count)`。
   我们：`RefreshClearFilter(0)` 读 `PageDrawer(0).State`，而 `_deckFltSlide`（`:4834`）没有 `State` ⇒ 恒 false；`ClearDeckFilters()`/`ApplyDeckFilterChanged()` 也不刷。
   改法：给 page==0 一个显式谓词（`_deckFacFilter != "" || _deckNameFilter != ""`），在三处各刷一次；⛔ 别给 `_deckFltSlide` 塞假 `State`（会把卡牌页筛选模型混进来）。

4. 🔴 **Done 钮在不合法时仍印「完成」**
   原版：`valid ? MenuDeck/MenuButtons/Done : …/Discard`（il2csharp `DeckEditingWindow.cs:565`）。
   我们：`DeckRuntime.cs:2489` 建一次后无改文处。
   改法：在已有的 `RefreshHeader`（已调 `State.Validate()`）里按结果 `SetText`。

5. 🔴 **卡背页左抽屉两行上下颠倒 + 缺 `Army` 小标题**
   原版：`Cosmetic FIlter ▸ Filters` = `Spacing 15 → Owned Toggle (1) 50 → Army Filter 345`；`Army Filter ▸ Title = 'Army'` fs32。
   我们：`FilterPanelModel.cs:1131-1160` Army 先画、Owned 落到 ≈578；`CollectionWindow.cs:3023-3035` 不建标题；`CosmoSpacing2=12.81` 其实是**卡组编辑窗**那份值（同文件 `:1072-1074` 父链写着 `Deck Editing Menu`）。
   改法：给 `BuildCosmetics` 加收藏窗口径（Owned 15 / Title 70 / Content 130），按窗分参数。

6. 🔴 **页头筛选 chip（四页）整块没建**
   原版：`Header/Filters` 是 GridLayoutGroup 85×70 · spacing 5,0 · FixedRowCount(1)，已选项挂 chip、**最多 5 个**。
   我们：`CollectionWindow.cs:3598-3601` 只有 `Clear Filter Button`；行内 chip 也没有 5 上限（`FilterPanelModel.cs:831` 缺省 `int.MaxValue`）。
   改法：先补行内上限（四调用点传 5、缺省改 5），再按原版建页头 chip。

7. 🔴 **卡面灰度（未拥有/禁卡）一格都没画**
   原版：`CardCollectionDisplay__SetCell.c` 末段 `grey = !owned 竖线 isBanned ⇒ ToggleGreyScale(grey, 0f)`；未拥有时张数条另灰（带补间）。
   我们：`CollectionWindow.cs:312-315`「只记不画」，理由是「`ImageQuad` 没有判据灰值」——**已过期**：卡面是 `CardView`，自带 `SetGrey(g,time)` / `_GreyScale`（`Core/CardView.cs:8335-8367`）。
   改法：`RebuildCardsCells` 里按 `!owned 竖线 IsCardBanned(def)` 调 `v.SetGrey(1f,0f)`。⚠️ 异画那支原版极性看着是反的（`HasItem(altArt)==0 ⇒ 不灰`），两个反编译源一致 ⇒ **照抄别修**。

8. 🔴 **卡背页 `Clear filters` 钮位置错 876 px（本条是本报告的裁定，见 §六-0）**
   原版：四颗序列化值相同（`pos(20,0)` / `anchor(1,.5)` / `pivot(0,.5)` / `250×60`），但父 `Filters` 的 `sizeDelta.x` 不同：Deck/Cards/AltArt = **−876.415**（宽算成 0）、**Cardback = 0**（宽 876.415）⇒ 绝对左缘 Deck/Cards/AltArt ≈ **612.2**、**Cardback = 1488.6**。
   我们：`CollectionWindow.cs:3403 ClearFltX = 612.2f` 四页共用 ⇒ 卡背页那颗钮偏左 876.4 px。
   改法：`BuildFilterHeader` 的 `clearPx` 参数目前是字号，需另加「清除钮 x」形参（page==2 传 1488.59）。

9. ⚠️ **卡背页 / 卡组编辑窗卡背页的小屏系数：编辑窗仍用 1.5（原版 1.2）**
   判定：`DeckRuntime.cs:3396` 乘 1.5 ⇒ 375×607.5 · 4 列；原版 `_mobileSizeScale=1.2` ⇒ 300×486 · **5 列**（收藏窗那一份 `CollectionWindow.cs:1793` 已是对值）。
   改法：`DeckRuntime` 那处改 1.2。

10. ⚠️ **`Import` 缺 interactable 闸（卡组满 114 套）**
    原版 `importDeck.interactable = 已拥有 < totalCustomDecks(114)`（`SelectDecksTab__Setup.c:44-53`）。
    我们无闸。改法：`BuildDeckHeader` 收尾 + 每次重建列表后按卡组数设 `WindowButton.Interactable`。

11. ⚠️ **阵营筛选只能单选（原版多选）** —— `_deckFacFilter` 单 string（`:4876`）vs `ToggleCollectionFilter` 的 `Dictionary<CardArmy,bool>`。改法：换 `HashSet<string>`（可复用 `DeckEditorState.cs:63` 那套集合口径）。

12. ⚠️ **选卡组 / 导入的文案三处不符** —— ① `Edit Deck` 在 Import 态应换 `MenuDeck/MenuButtons/ImportDeck`（键本地已有，`:809-819` 的「未解出」已过期）② 导入错误行原版只一条 `MenuDeck/Share/InvalidDeckString` ③ 选卡组空态原版是硬编码英文。

13. ⚠️ **`Confirm` 钮没有禁用态**（原版出厂 `interactable=false`、输入非空才亮）—— `ImportDeckPopup.cs:241-261`。

14. ⚠️ **导入成功后少一站**（原版先 `DeckInfoPopup(Import)` 再进编辑器保存；我们直接落库关窗）—— `ImportDeckPopup.cs:393-396`。另：聊天 `DeckShare` 原版合法卡组给一颗直加钮，我们 `ChatPanel.cs:955-958` 传 `selectButton=null`。

15. ⚠️ **`Share` 剪贴板缺可读头部**（原版四段 + 末行 base64；我们只有 base64）—— `DeckRuntime.cs:6044-6050`。

16. ⚠️ **卡组编辑窗 Done↔Discard 那一拍的反馈缺音效与弹跳**，首帧也用 0.3s 淡入（原版首帧 0）—— `DeckRuntime.cs:8893-8905`。

17. ⚠️ **「干净但不合法」的卡组：原版 Done/ESC 直接关窗，我们弹错误窗** —— `SaveAndSay` 不看脏标记。

18. ⚠️ **卡组格 Ban Icon 档未建**（本地不可达）—— 接 LiveOps 时是**一批**要补的（卡池 ban 徽标已有、卡组格没有），⛔ 别当已做完。

---

## 三、§卡组专章（用户点名重点）

### 3·1 卡组相关的真缺口（按影响排序）

| # | 缺口 | 归属 | 判据 | 落点 |
|---|---|---|---|---|
| K1 | 点格即改「当前卡组」并写盘 | 收藏 · Deck 页 | `CollectionDisplay__Select.c`（不落状态） | `CollectionWindow.cs:5346` |
| K2 | Deck 页 `Clear filters` 恒不显示 | 收藏 · Deck 页 | `NotifyFilterChange` 末句 | `:3734` + `:4834` |
| K3 | 卡背页 `Clear filters` x 错 876 px | 收藏 · 卡背页 | prefab 父 `Filters` sizeDelta（§六-0） | `:3403` |
| K4 | Done 钮不换 `Discard` | 卡组编辑 | il2csharp `DeckEditingWindow.cs:565` | `DeckRuntime.cs:2489` |
| K5 | 卡组编辑窗卡背页小屏 1.5→1.2 | 卡组编辑 | `MonoBehaviour_-1923093106525475036.json` | `DeckRuntime.cs:3396` |
| K6 | 导入窗粘贴通道缺失 | 卡组三弹窗（跨件） | prefab `m_LineType=1` | `Shell/PointerLayer.cs:169-232` |
| K7 | `Import` 无 114 上限闸 | 收藏 · Deck 页 + 选卡组页 | `SelectDecksTab__Setup.c:44-53` | `:4084/4139` |
| K8 | 卡组格 Ban Icon 档未建 | 收藏 · Deck 页 | `CollectionDeck.Config` | `MenuDraw.DeckCell` |
| K9 | 页头 chip（四页）未建 + 无 5 上限 | 收藏 · 四页 | `NotifyFilterChange.c:110/141/154` | `:3598`；`FilterPanelModel.cs:831` |
| K10 | Deck 阵营筛选单选 | 收藏 · Deck 页 | `ToggleCollectionFilter.cs:135` | `:4876` |
| K11 | 导入成功少一站 / Confirm 无禁用 / 错误行三条自拟键 | 卡组三弹窗 | `__TryImport.c:20-40`、`__ValidateField.c` | `ImportDeckPopup.cs`、`CollectionData.cs:278-296` |
| K12 | `Cardback Front` 缺一层 | 卡组详情弹窗 | `DeckInfoDrawer.cs:14-36` | `DeckInfoPopup.cs:2109-2156` |

### 3·2 卡组编辑窗：三条「看似不符、其实已判死」的澄清（⛔ 别照着去补）

1. **`sortDeckMode` 是死支** —— 原版字段 `readonly bool`、ctor 只调 base ⇒ 恒 false，`SetSortMode`/`ToggleReorder` 不可达。我们生产也没入口（只有自检传 true）。**不算缺口**。
2. **卡组格没有「张数」** —— `CollectionDeck` 无该字段、prefab 树无该节点、用户实拍 4 格也没有。原任务书里的「张数」是清单作者的误解。
3. **空池格不会显示 `Create New Deck`** —— `createObject` 只有 `Config(null)` 才开，而 `SetCell` 只对 `< count` 的格调（推断，见 §五 ?4）。
4. **左侧卡组列表不是 `GridLayoutGroup 360×58/3 列`** —— 那是 `Deck info Popup.prefab` 的同名抽屉；本窗是 `VerticalLayoutGroup` 单列、行 309×55.7，我们逐值一致。
5. **卡组编辑窗只有 3 个页签（Cards / Deck info / Cosmetics），没有「异画」页** —— 异画在卡牌详情窗里选。

### 3·3 卡组规则校验：逐条已对齐（无需改）

总张数 30（遭遇 12）· 同名上限（督军 1 / 传奇 1 / 其余 2）· 传说总数（经典 0 = 无上限）· 阵营限制（**无中立例外**）· 督军位（必须 Hero 且同阵营）· 防御卡（`ValidateDeck` 不查张数，带则必须 Defensive）· 禁卡 · 持有数 · 准入（`inventoryOptions` / `showInCollectionOptions`）—— 全部 ✅。`DeckError` 我们细分到 ~15（原版 6），**判定结果一致**，只是文案更具体，不建议收敛（会在账里留一条，免得后人以为没复刻）。

---

## 四、§查不到清单

| # | 问题 | 搜过什么 | 还差什么 |
|---|---|---|---|
| ?1 | 筛选内容总高是 **989.018**（预制模板）还是 **~1389**（13 阵营格就位后） | `menu_dump` 的 `Card Filters ▸ Filters`（`CSF MinSize=989.018`）、`Army Filter`（150 + CSF）、`FilterPanelModel.ArmyRowH` 推导 | 一次原版实跑量 `Filters.sizeDelta`（本机无菜单运行期 dump）。**两支都 > 924 ⇒「可滚」不受影响**，只影响滚动距离 |
| ?2 | 页头 `filterIconHolder` 上到底挂什么 | `NotifyFilterChange.c:110/141/154`、`filterIconHolder` PPtr 现解（→页头 `Filters`，Grid 85×70）、`FillToggleList`、`CollectionFilterToggle.Initialize` | 一张原版实拍：选中 2–3 项后页头是否出现 chip。`.c` 的两轮循环读不出「搬走」还是「复制一份」 |
| ?3 | 模式子页卡片的 prefab 真身 | `assets_full/.../GameObject/` 按名搜 `SelectDeckGameModeMenuContainer` = 0；`deckMenuContainer` 是 `SelectGameModeDeckTab` 的序列化引用 | 追 `deckMenuContainer` 的目标 bundle；今天两张卡是离线等价物 |
| ?4 | 空池格是否可能显示 `Create New Deck` | prefab `Create` 节点 INACT；`createObject.SetActive(item==null)` 是唯一写点 | `VerticalRecyclingSystem.OnScroll/UpdateCell` **方法体**（ilspy 只有字段与 `InitCoroutine`；`decomp_full` 无该命名空间）⇒ 现结论是推断 |
| ?5 | `CollectionDisplay<T>.OnListDrag` 基类方法体 | `out_il2csharp/CollectionDisplay.cs:132-135` 是 `NotImplementedException("Method body was not recovered.")`；`decomp_full`/`decomp_generic_trial` 均无 | 无法确认拖动列表时 `Reference Card Pointer` 是否跟随指针（我们按固定矩形 + 同帧求交实现） |
| ?6 | 卡组行拖出的判定矩形 | `DeckEditingPanel__CheckCardSlotDrag.c` 读到 `IsScrollDragThreshold` + `RemoveCard`，但**比的是哪个 rect** 读不到 | 需 VA 级反汇编或实拍；今天取 325.4（抽屉右缘） |
| ?7 | 卡背页 Viewport 高（编辑窗） | AUD05 §6·4 已记「未实读」，本次沿用 924.06 | 父链现算 |
| ?8 | `DeckInfoPopup.deckSelectionPopup` 指哪一扇 | `Deck info Popup.prefab` 的 `m_AssetGUID=e724bbe6…` 在 ExportedProject `.meta` **无对应**（addressable guid ≠ 导出工程 guid）；两个变体根字段都读了；`catalog.bin` 有 guid 无名字 | 若原版走**无页签**那扇，练习对手列的是「自己卡组同模式那几副」，我们落在预组页 |
| ?9 | `ReleaseFullHeroSprite()` / `gameModeIcons.Release()` 的等价物 | 原版是 Addressables 租/还；我们 `CardArt` 是常驻缓存 | 机制不同、未验（不是「漏了释放」） |

---

## 五、§顺手发现（只报不改）

0. 🔴 **两家代理结论打架，已裁定**：B 报「`Clear Filter Button` 的 x 四页共用 612.2，而 Cards/Cardback 应为 1488.6」；A 报「Cards 页 612.2 正确」。**本次用 `Collection Menu Variant.prefab` 现算裁定** —— 四颗按钮序列化值完全相同，差异来自父 `Filters` 的 `sizeDelta.x`（Deck/Cards/AltArt = −876.415 ⇒ 宽 0 ⇒ 左缘落到 592.2+20 = **612.2**；**Cardback = 0 ⇒ 宽 876.415 ⇒ 右缘 1468.59 + 20 = 1488.6**）。**结论：只有卡背页错，Cards/AltArt/Deck 都对**（B 把 1488.6 归给 Cards 是错的）。落点见问题 8。
1. 🔴 **`CollectionWindow.cs:2497` 与 `:312-315` 的「`ImageQuad` 没有判据灰值」已过期** —— `Core/CardView.cs:8335` 的 `SetGrey` 就是原版 `_GreyScale`。同族旧注释建议全仓按铁律 5 重核（不只是收藏窗）。
2. 🟡 **`DeckInfoPopup.cs:17` 文件头摘要仍写「Deck Name / Warlord Name 居中」**，而 `:286` 与代码（`:1471-1486` `Align.Left`）都已订正 ⇒ 摘要过期。
3. 🟡 **`FilterPanelModel.cs:585-594` 注释写「阵营那一格 …… 自身隐藏」在 `.c` 里不成立** —— 空卡组支是 `SetActive(true)`，真正隐藏的是「卡类」那一行（`CardTypeFilter.cs:26-27`）。
4. 🟡 **`DeckEditingHeader.SetInstructions` 全反编译 0 调用点** ⇒ `Header/Instructions` 与 `Header/label('Edit your deck')` 恒 INACT 是**原版就这样**，别按截图去点亮。
5. 🟡 **`FilterPanelModel.cs:1030` 自陈「Army 在前、Owned 在后」** 描述的是卡组编辑窗的卡背抽屉，被当成两窗共用常量落进收藏窗 —— 问题 5 的根因。
6. ⚪ **模式子页两张卡未打 `🔶 自己设计（非原版）` 标记**（铁律 18 要求机器可 grep）—— `CollectionWindow.cs:4335` 一带就地改注释，⛔ 别加行（该文件有账落点）。
7. ⚪ **`ViewDeckInfoPopup`（只读卡组窗）在本 build 不存在可用实例** ⇒ 不复刻是**正确的**：反编译只有它自己 5 个方法、**0 个外部调用点**；`bundle_Waprforge_monoscripts` 里**没有它的 MonoScript**（同批另 4 个全在）；ExportedProject 无同名 prefab。判据为空（铁律 11 例外①）。
8. ⚪ **`DeckSelectionTabController.searchBar` 是「做了但从未启用」的件** —— `Awake` 挂了监听、`Filter()` 也在用，但输入框出厂 `act=N` 且该类 6 个方法零 `SetActive` ⇒ 我们不建搜索框成立。
9. ⚪ **`GameStaticData.totalCustomDecks = 114` 同时是 Deck 页 `_poolSize` 的值** —— 是否「同一件事写了两遍」无证据，不下结论。
10. ⚪ **`SelectDecksTab.Setup()` 每次 `ToggleSelectDeck` 都重新 `AddListener`**（UnityEvent 不去重）—— 原版自己的小毛病，不建议照抄。
11. ⚪ **`40k_collection_bt_avatars.png` 在工程里、但菜单 bundle 不引用它** ⇒ 别据此给收藏窗加头像页。

---

## 六、§七 · 子代理分工与产出

| 代号 | 范围 | 文件（全路径） | 行数 | 结论 |
|---|---|---|---|---|
| **A** | 收藏窗壳 + 四页签 + 卡池 + 筛选 + 卡背 + 异画 + 头像 | `d:/4/Unity/资料/完全复刻/报告/B121_COLL_A_收藏窗壳与卡池.md` | 119 | 52 项 · ✅41/🟠1/⚠️9/❓1 · 窗壳与网格基本对齐，问题集中在卡背抽屉顺序、chip 上限、卡面灰度 |
| **B** | 收藏窗 Deck 页 + 卡组列表/重排 + 规则校验 | `d:/4/Unity/资料/完全复刻/报告/B121_COLL_B_卡组列表与规则.md` | 140 | 71 项 · ✅60/🟠4/⚠️6/❓1 · 版面逐位吻合，缺口全在交互与规则闸 |
| **C** | 卡组编辑窗 | `d:/4/Unity/资料/完全复刻/报告/B121_COLL_C_卡组编辑窗.md` | 142 | 48 项 · ✅39/🟠1/⚠️7/❓1 · 主体逐值对齐；两条真问题 = Done 文案、卡背小屏系数 |
| **D** | 卡组详情 / 选卡组 / 导入 三弹窗 | `d:/4/Unity/资料/完全复刻/报告/B121_COLL_D_卡组弹窗三扇.md` | 142 | 51 行 · ✅41/🟠1/⚠️8/❓1 · 几何文案交互几乎全落地，缺口在链路层（粘贴通道、导入落点、禁用态） |

> 扇出只有一层（A/B/C/D 均未再派子代理）；四块文件所有权互不相交（每块只写自己那份报告，源码一律只读）。
> 本主报告的三处**裁定/合并**：① `Clear filters` x 值冲突（§五-0，我用 prefab 现算）；② 卡背抽屉「Army 在前」的归属（A 与 C 均指向 `FilterPanelModel.cs` 的卡组编辑窗常量）；③ 「Import interactable 闸」B、D 重复命中，合并为一条（K7）。

---

## 附录 · 覆盖边界

- **读透**：`CollectionScreen` · `Card/Deck/Cardback/AlternateArtCollectionTab` · `CollectionDisplay<T>` · `CardCollectionDisplay` · `CardbackCollectionDisplay` · `CollectionFilterController` · `CardNameFilter` · `UpgradableCardFilter` · `SelectDecksTab` · `DeckCollectionTab` · `CollectionDeck` · `DeckCollectionDisplay` · `DeckEditingWindow` · `DeckEditingPanel` · `DeckEditingStateController` · `DeckListDrawer` · `DeckEditorCollectionDisplay` · `DeckEnergyCostDrawer` · `DeckInfoPopup` · `DeckInfoControls` · `DeckInfoDrawer` · `DeckSelectionPopup` · `DeckSelectionTabController` · `ImportDeckPopup` · `CardDeck` · `DeckUtility` · `DeckError` · `PolyAndCode.UI.RecyclableScrollRect/VerticalRecyclingSystem`。
- **prefab**：`Collection Menu Variant` 整棵树（含 4 颗 `Clear Filter Button` 父链现算）· `Deck Editing Menu.prefab` YAML · `Deck info Popup.prefab` · `Deck Selection Popup{ with Tabs}.prefab` · `Import Deck Popup.prefab` · 6 个 RSR 实例字段。
- **没跑**：Unity 批处理 / 真机 / `typecheck`（只读红线）。
- ⚠️ **「参数对上了」≠「复刻完了」** —— 本报告只答「原版哪个状态是什么参数、我们代码取的是不是它」，不答「并排看像不像」（属调度台验收）。
