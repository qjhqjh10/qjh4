# B121-COLL-C · 卡组编辑窗（`Deck Editing Menu` / `DeckEditingWindow`）只读审计

> 代理 B121-COLL-C · 2026-11-03 · 只读（未跑 Unity / 未动 git / 未改任何源码）。判据源 = 反编译三份 + prefab 原文（`menu_dump.py` 现跑一次 + `Deck Editing Menu.prefab` YAML 原文）。
> 范围 = 卡组编辑窗本体（不含收藏窗卡池/卡背/异画页、卡组详情弹窗、选卡组弹窗、导入弹窗）。

## ① 结论

**这扇窗的复刻度很高**：48 条审计项里 39 条 ✅、7 条 ⚠️、1 条 🟠、1 条 ❓ —— 几何/排序/校验/保存/离场/交互主体都已逐值对齐，剩下的偏差集中在「**状态联动的小件**」和「**小屏档一格的系数**」。最值得先修的两条：**Done 钮文案不随合法性切换**（原版 Done↔Discard）与**卡背页小屏系数用了 1.5（原版 1.2）**。
**父任务的两条前提需要更正**：① 本窗页签是 **3 个**（Cards / Deck info / Cosmetics），**没有「异画」页**；② 左侧卡组列表 **不是** `GridLayoutGroup 360×58 / 3 列`（那是 `Deck info Popup.prefab` 里的同名抽屉），本窗是 **`VerticalLayoutGroup` 单列 / 行 309×55.7 / spacing 2 / pad 8**。

**四档计数**：`✅符合原版 39` · `⚠️实现了但参数或行为不符 7` · `🟠我们没实现 1` · `❓判据查不到 1`。

---

## ② 主表（逐项一行）

### A. 窗结构（8）

| # | 是什么 | 是否实现 | 原版参数（出处） | 我们参数（文件:行） | 差异 | 档位 |
|---|---|---|---|---|---|---|
| A1 | 窗根 + `Content Area`（四周内缩 167.175/70.94）+ 背景 | ✅ | prefab：根 1920×1080；ContentArea 167.2,71→1920,1080（`menu_dump` 第 3 行 · `D10_prefab树.md`） | `DeckEditingWindow.Create` 显式 `type=Fullscreen / placement=World`；`DeckRuntime` 同矩形（`DeckRuntime.cs:1470` `BuildAreaBackground`） | 无 | ✅ |
| A2 | 页签**数量与内容** | ✅ | **3 个**：`Sidebar/Window Options/Buttons/{Cards,Info,Cosmetics}`（`DeckEditingStateController.cs:9-14` 三个 toggle） | `_tab = 0/1/2`（`DeckRuntime.cs:1012` · `SetTab` `:7059`） | 无。**无「异画」页**：异画在卡牌详情窗里选（`DeckEditingWindow.cs:200-236` `DisplayClass29_0` → `SelectedAlternateArt` 换卡） | ✅ |
| A3 | 页签几何 / 文案 / 默认页 | ✅ | 每格 108.96×150 @ y156；文案键 `MenuShop/ShopItemType/Cards` `MenuDeck/HUD/DeckDescription/DeckInfo` `MenuShop/ShopItemType/Cosmetics`（prefab `Localize.mTerm`）；默认 = Cards（`DeckEditingStateController.Initialize` `deckListToggle.isOn=true`） | `TabCellW=108.96667` `TabsH=150`（`:186,195`）；三键同（`:1838-1852`）；开窗 `_tab=0`（`:1397`） | 无 | ✅ |
| A4 | Header「返回」钮（原版节点名叫 `Close`，印 **Back**） | ✅ | 150×60 @192.2,83.5；`Localize.mTerm = MainMenu/MainButtons/ButtonLabel/Back`；点击 = `TryClose`（`DeckEditingWindow.cs:79-81` → `:613`） | `hdr_back`（`:1565-1590`）→ `TryClose()`（`:5506`） | 无 | ✅ |
| A5 | Header 筛选开合钮 + 清除筛选钮 | ✅ | `Header/Filters`（50×50，Toggle）+ `Filters/Generic Simplified UI Button_updated` 250×60，键 `MenuDeck/Filters/ClearFilters` | `hdr_filters` `hdr_clear`（`:1620-1660`） | 无 | ✅ |
| A6 | 页眉通配符条（4 档 + 阵营徽记），Cosmetics 页隐藏 | ✅ | `Header/WIldcard Counter`：Common/Rare/Epic/Legendary 各 30×44 + `Army Icon`；`ToggleDisplay(showCards)` 里 `wildcards.SetActive(showCards)`（`DeckEditingWindow.cs:777-785`） | `_wcTxt[4]` + `hdr_army`（`:1680-1774`）；`_cardsInfoOnly` 只 tab0/1（`:6156`） | 无（数字读本工程万能卡库存 `CardProgress.Wildcards(army,rarity)`；2026-10-29 已把恒 `99` 改回真库存，`DeckRuntime.cs:4879-4883`） | ✅ |
| A7 | 卡组名输入框（含清空钮 / 占位符 / 改名标脏） | ✅ | `Window Options/Deck Name`：`EverguildInputField` 307.72×50；占位符键 `MenuDeck/HUD/EditDeckName`；改名 = 无条件写 `deckName` + `syncedToServer=0`（`DeckEditingPanel.cs:263-272`） | `name_box/name_t/name_h/name_clear`（`:1958-2004`）；`BeginNameEdit/ClearDeckName` | 无 | ✅ |
| A8 | ESC = 保存（不是关窗）；开窗收起 / 关窗放回左导航 | ✅ | `ESCPressed` 全文 `TrySaveDeck(this,0)`（`DeckEditingWindow__ESCPressed.c`）；`TryOpen` 末 `NavigationPanelController.Toggle(0,1,0)`、`Close.c:19-20` 先 `Toggle(1,1,0)` | `Shell/DeckEditingWindow.cs:161-173` → `DeckRuntime.EscPressed`（输入框 / 模态窗 / 保存 三档让位）；导航栏 `Open/Close`（`:131/197`） | 无 | ✅ |

### B. 左侧卡组列表（`Deck List drawer`，8）

| # | 是什么 | 是否实现 | 原版参数（出处） | 我们参数（文件:行） | 差异 | 档位 |
|---|---|---|---|---|---|---|
| B1 | **容器类型与排布** | ✅ | `…/Deck List drawer/Scroll View/Viewport/Content` 的 `VerticalLayoutGroup`：`m_Padding(8,0,8,0)` · `m_Spacing=2` · `m_ChildAlignment=1` · `ControlWidth=1/ControlHeight=0` + `ContentSizeFitter(V=MinSize)`（`Deck Editing Menu.prefab` fileID `114335036306139620`） | 单列 + `RowSpacing=2` + 行 x = `ListX+8`（`:244,502`） | 无。⚠️ **父任务简报写的 `GridLayoutGroup 360×58·spacing(11,4.5)·pad(15,0,15,0)·3 列` 属于 `Deck info Popup.prefab`**（`grep "m_CellSize: {x: 360"` 只命中该 prefab） | ✅ |
| B2 | 行几何（两档：督军行 / 卡行） | ✅ | 行节点 309×55.7（`Deck Selector * Card Info button` RT 8.4,366→317.4,421.7）；卡行 `Background` 25.7→317.4 = 291.75 | `RowH=55.7` `RowNodeW=309` `RowBgHeroX/W=8.4/309` `RowBgCardX/W=25.655/291.745`（`:233-254,502-530`） | 无 | ✅ |
| B3 | 行内容（费用圆 / 卡名 / 份数 `xN` / 稀有度色条 / 描边 / ban 叉 / 空槽底+空槽字） | ✅ | 子件 = `Background` `Rarity Gradient` `Background Border` `Cost Image`+`Cost` `banned Icon` `Text fill(Card Name, Count)`；空槽 = `Background Slot Free` + `Slot Free Text`（键 `MenuCollection/WarlordDeckSlot` / `DefensiveDeckSlot`） | `row_*/row_g*/row_b*/row_c*/row_n*/row_x*` + 类尾 `ApplyRowExtras`（`:2023-2200`，空槽字 `:9942`） | 无 | ✅ |
| B4 | 行排序 + 同名合并 | ✅ | `DeckListDrawer.Reorder` = 费用 → 本地化名（`OrderBy/ThenBy`，无 comparer ⇒ 文化序）；`Initialize` 里同名只加 `Count`；督军 `SetSiblingIndex(0)`、防御卡 `1`；实例字段 `showWarlord=1 · stackCopies=1 · checkOwnership=0`（prefab 第 10402-10404 行） | `DeckEntries()`：督军 / 防御卡固定在前，其余 费用→名（`StringComparison.CurrentCulture`）→Id（`:4190-4220`） | 无 | ✅ |
| B5 | 滚动 | ✅ | `ScrollRect` v=1 · `m_ScrollSensitivity=50`；`Content` 高由 CSF 现算（`n×57.7−2`） | 12 槽复用 + `firstRow=floor(_deckScroll/RowPitch)` + 裁切（`:4291-4300`）；50 px/格（`:7110`） | 无 | ✅ |
| B6 | 点击行 = 开卡牌详情窗 | ✅ | `deckList.OnCardSelected` → `DeckEditingWindow.OpenCardInformation`（`DeckEditingWindow.cs:99-100` · `:263`） | 松手未越阈值 ⇒ `OpenCardWindow`（`EndDrag` `:5350-5359`） | 无 | ✅ |
| B7 | 拖出行 = 删牌 | ✅ | 拖起即 `RemoveCard`（`DeckEditingPanel.CheckCardSlotDrag` `:274-299`）；落在面板上则 `Drop`→`AddCard` 加回 | `_draggingMoved` + 拖出 `ListX+RowW`(325.4) 才删（`EndDrag` `:5361-5375`） | 阈值取 325.4（抽屉右缘）而非 317.4（行节点右缘），原版比的是哪个 rect **本地读不到**；我们已如实标注并保持旧值 | ✅（附注） |
| B8 | **行内拖动 = 重排** | ⚠️ | **原版这扇窗没有重排**：本 prefab 全树无 `DragAndDropItem`（`menu_dump` 组件列只有 `Draggable` / `CardDraggingController`），卡组行拖拽只走 `CheckCardSlotDrag`（拖起即删） | `EndDragAt` → `State.TryMove` 并 `Say("已调整位置")`（`:9178-9205`） | **多出来的一支**；依据（注释里写的 `DragAndDropItem.OnEndDrag`）属于 `Collection Deck` 那一族，不在本窗。可见影响小（列表按费用排序，重排后画面不变），但会改写 `CardIds` 顺序 | ⚠️ |

### C. 卡池（9）

| # | 是什么 | 是否实现 | 原版参数（出处） | 我们参数（文件:行） | 差异 | 档位 |
|---|---|---|---|---|---|---|
| C1 | 网格几何（桌面） | ✅ | `RecyclableScrollRect` `_cellWidth/Height=262.5/384` · `_spacing=0` · `_segments=4`；视口 1589.78×924.06 ⇒ `floor(1589.78/262.5)=6` 列 · PadX 7.35 · 4 行（`MonoBehaviour_345129740864909092.json` · AUD05 §1-§3） | `PoolMetrics Pool`（`:816-833`）：6 列 · 262.5×384 · PadX 7.35 · 4 行 | 无 | ✅ |
| C2 | 网格几何（小屏档） | ✅ | `_mobileSizeScale = **1.5**` ⇒ 393.75×576 · 4 列 · 5 行（同上 json + `RecyclableScrollRect.cs:134-141`） | `k = SmallScreenUI.Enabled ? 1.5 : 1`（`:819`） | 无 | ✅ |
| C3 | 卡池排序 | ✅ | `GetCardCollection` 链 = `OrderBy(名)` → `OrderBy(cost)` → `OrderBy(cardType!=Hero)` → `OrderBy(cardArmy)`（最后一个键为主序 ⇒ 阵营→非督军→费用→名）；卡名搜索那一支整表反序（`DeckEditingWindow.cs:339-423`） | `SortForDisplay`（`DeckEditorState.cs:830-870`）+ 搜索反序（`:639`） | 无（含「收藏窗才夹 inventory 键」那一条也已分开） | ✅ |
| C4 | 左键 = 详情 ★右键 = 加牌 ★拖拽 = 加牌 | ✅ | `OnCardClick`：`button != Right` ⇒ `OpenCardInformation`；`Right` ⇒ `CanAddCard` 过则 `editingPanel.Drop`，否则 `UIMessageController.ShowError`（`DeckEditingWindow.cs:241-260`）；拖拽同判定（`CheckCardDrag` `:1403-1436`） | `HandlePoolClick`（`:5162-5205`）+ `BeginCardDragAt`（`:4058-4070`） | 无 | ✅ |
| C5 | 显示闸（`showInCollectionOptions` / `IsHidden` / `inventoryOptions==5` / 异画 / 拥有） | ✅ | `GetCardCollection` 五层 `Where`（`DeckEditingWindow.cs:339-380`） | `VisibleCards` 同形（`DeckEditorState.cs:566-640`，`PassesShowInCollection` `:752`、`PassesEditModeInventory` `:808`） | 无 | ✅ |
| C6 | **卡池按督军分流** | ⚠️ | 机制 = ① `FilterToDeck` → `CardArmyFilter.SetFilterToDeck`：有督军/有牌 ⇒ `FilterToArmy(deckArmy)`；空卡组 ⇒ `ClearFilters`（`.c:15-45`）② `CardTypeFilter.SetFilterToDeck`：**没督军 ⇒ `ShowOnlyHero()` 且把卡类那一行 `SetActive(false)`**（`CardTypeFilter.cs:20-40`）③ `CardCostFilter` 只在有督军时显示（`:15-19`） | 换成**内容硬闸** `WarlordGatedPool`：无督军 ⇒ 只列督军；有督军 ⇒ 只列该阵营（`DeckEditorState.cs:578-592`，用户 2026-09-27 口径） | 稳态结果相同；**两处边角不同**：① 有督军时在筛选栏点别的阵营，我们恒出空（原版会切到那个阵营、加时再报 `InvalidArmy`）② 无督军时原版把「卡类」整行隐藏，我们仍显示（我们的注释在 `FilterPanelModel.cs:585-594` 写「主对话已裁定现有形状等同原版」） | ⚠️ |
| C7 | 卡池格「张数 n/max」+ 同名上限 | ✅ | `DrawCell`：`text = "{0}/{1}"`（`Count(卡组内该卡)` / `min(owned, GetMaxCopiesInDeck(rarity,type))`）（`DeckEditorCollectionDisplay.cs:119-141`）；上限 = Hero 1 · 其余 `numberOfCopiesOtherRarities` · 传奇 `numberOfCopiesLegendary`（`GameplayVariablesData.cs:30-50`） | `DeckRuntime.cs:3269-3300`（`GetMaxCopies` 带卡型）；`DeckRules.CopyLimit`（`RuleEngine/Core/DeckRules.cs:165`） | 无 | ✅ |
| C8 | 卡面灰 / 禁卡叉 / 禁卡不可拖 | ✅ | `CollectionCard.UpdateCardVisuals(canAdd, owned>0, text)` → 灰 = 「未拥有**或**被禁」；`ToggleBanned` + `ToggleDrag(banned==0)`（`DeckEditorCollectionDisplay.DrawCell` + `CollectionCard__UpdateCardVisuals.c:19-47`） | `DeckRuntime.cs:3108-3140`（含 ban 徽标 `SetPoolBanIcon`） | 禁卡表数据源在远端（`PlayEventData` 缓存 null ⇒ 恒 false），与原版空态一致 | ✅ |
| C9 | 空结果提示 + 悬停格阵营跟随页眉徽记 | ✅ | `Empty Collection Warning`（键 `MenuCollection/NoCardsFound`）；`CheckFocusedArmy` 用固定参考矩形 417.7,261→611.5,306 与在屏格求交（`DeckEditorCollectionDisplay.cs:181-203`） | `_emptyWarn`（`:2200`）；`CheckFocusedArmy`（`:4905-4935`，含「池序 vs 可见序」的如实标注） | 无 | ✅ |

### D. 战将 / 防御卡槽（3）

| # | 是什么 | 是否实现 | 原版参数（出处） | 我们参数（文件:行） | 差异 | 档位 |
|---|---|---|---|---|---|---|
| D1 | 战将槽 = 第 0 行，空态显示 `-- Warlord --` | ✅ | `Deck Selector Hero Card Info button`（309×55.7）+ `ShowEmptySlotsIfNeeded`（`DeckListDrawer.cs:666-680`）；键 `MenuCollection/WarlordDeckSlot` | 槽 0 两套件（督军档 + 卡行档）+ 空槽字（`:2020-2060` · `:9942`） | 无 | ✅ |
| D2 | 换督军 = 从卡池直接加另一张战将 | ✅ | `AddCard`：`cardType==Hero` 且槽里有人 ⇒ 先 `RemoveCard(旧)` 再 `TryAddCard`（`DeckEditingPanel.cs:175-242`）；**无二次确认**；`AddHero` 只写 `deckHero/deckArmy`，**编辑期不清旧卡**（清卡在下次开窗的 `RemoveInvalidCards`） | `DeckEditorState.CanAddHero/TryAdd`（`:408-500`），注释写明「直接替换、不确认、旧卡留着由 Validate 熄 Done」 | 无 | ✅ |
| D3 | 防御卡槽 = 第 1 行，只一张，加新卡替换 | ✅ | `CardType Tactic + spellType Defensive` ⇒ 写 `defensiveCard` 单槽；`CanAddDefensive` 比阵营 | 同上（`:432-440` · 槽位 `DefensiveId`） | 无 | ✅（注：用户口径「防御卡是我们补的」，原版卡表防御卡恒 null） |

### E. 统计 / 校验 / 保存（11）

| # | 是什么 | 是否实现 | 原版参数（出处） | 我们参数（文件:行） | 差异 | 档位 |
|---|---|---|---|---|---|---|
| E1 | 页脚张数 `n/30`（自定义模式用 `gameplayVariables.deckSize`） | ✅ | `DeckEditingPanel.Initialize/UpdateDrawers`：`string.Format("{0}/30")`（`DeckEditingPanel.cs:111-140,301-316`） | `_counterTxt.SetText(DeckCount + "/" + MaxDeckCount)`（`:4853`） | 无（本工程无自定义模式活动数据） | ✅ |
| E2 | 费用曲线（9 行 · 按 10 张归一 · 末档 `8+` · 0.2 s 补间） | ✅ | `DeckEnergyCostDrawer`：`NUMBER_OF_CARDS_FOR_NORMALIZATION=10`；`cost >= rows.Length-1` 并入末桶；`clamp01(n/10)`；末档标签 `"8+"`；`DOValue(…,0.2f)` | `RefreshDeckList` 尾部（`:4481-4500`）+ `CurveLastBucket/CurveCostText`；`clamp01(n/10)*CurveBarW` | 数值/分档/末档 `8+` 全对；**柱宽是瞬时赋值**（`ApplyCurveBarWidth` 直接摆三块），原版每格走 `DOValue(…,0.2s)` 补间 ⇒ 缺一段增长动画 | ⚠️ |
| E3 | 类型计数 `Minions: n` / `Spells: n` | ✅ | `DeckTypeCounterDrawer`：`"{0}: {1}"`，`cardType==0` 计 Minions、`==20` 计 Spells，键 `…/DeckDescription/{Minions,Spells}`（`DeckTypeCounterDrawer.cs:19-40`） | `RefreshTypeCounter`（`:2440-2470`） | 无 | ✅ |
| E4 | 构筑校验规则集 | ✅ | `CardDeck.CanAddCard`：`InvalidCard` / `MaxCopiesReached` / `MissingCopies` / `InvalidArmy`（中立直通、空卡组直通）/ `MaxLegendayCopiesReached` / `DeckFull` / `ThisCardIsBanned`（`CardDeck.cs:539-715`）；`DeckUtility.ValidateDeck`：张数必须**正好** deckSize、空名补督军名 | `DeckRules.Validate` + `DeckEditorState.CanAddCore`（`:398-470`）；`Validate()` 带补名副作用（`:363-372`） | 无（`MissingCopies` 因单机全解锁不可达，已如实标注） | ✅ |
| E5 | Done = 保存；不合法 ⇒ 错误模态窗（Discard / Cancel），**不落盘** | ✅ | `TrySaveDeck`：`ValidateDeck` 假 ⇒ `ShowPopUp(ToRawLocalizationString(err), primary=Discard, second=Cancel)` 并 `return`（`DeckEditingWindow.cs:625-731` · `__TrySaveDeck.c:80-98`） | `SaveAndSay`（`:5550-5590`）+ `ShowInvalidDeckPopUp`（`:5925`） | 无 | ✅ |
| E6 | **Done 钮文案随合法性 Done ↔ Discard** | ⚠️ | `UpdateDoneButton`：`doneButton.text.text = valid ? doneKey : discardKey`（键 `MenuDeck/MenuButtons/Done` : `…/Discard`，官方表两条都在）（`DeckEditingWindow.cs:565`） | `foot_done_t` 只在 `BuildFooter` 建一次，**全文再无改文处**（`:2489`） | **缺**：不合法时我们仍印「完成」，原版印「丢弃」。修法 = 在 `RefreshHeader`（已有 `State.Validate()`）按结果 `SetText` 两个键 | ⚠️ |
| E7 | 合法性「由假变真」那一拍的反馈 | ⚠️ | `UpdateDoneButton(firstTime=false)`：合法 ⇒ `SoundManager.Play2D(doneButtonEnabled)` + `doneButtonTogglerEffect.Toggle(true,0.3)` + `DOPunchScale(0.05×,0.5s,5,1.0)`；首帧 `firstTime=true` ⇒ `Toggle(valid, **duration 0**)` 硬切（`DeckEditingWindow.cs:558-593`） | 只有 `ToggleDoneHighlight(on)`（`:8893-8905`，`UIEffectTogglerController.Toggle` 默认 0.3 s）；**无 Punch、无 enabled 音效**，首帧也是 0.3 s 淡入 | 缺两项反馈（音效 + 弹跳），首帧淡入时长与原版不同 | ⚠️ |
| E8 | 干净卡组按 Done/ESC ⇒ 原版**直接关窗**（不校验不写盘） | ⚠️ | `TrySaveDeck`：`syncedToServer!=0` 且库里有这副 ⇒ 直接 `Close()`（`DeckEditingWindow.cs:664-727`） | `SaveAndSay` 不看脏标记，一律先 `Validate`（`:5550-5560`） | 差异只在「干净但内容不合法」的卡组：原版静默关，我们弹错误窗。正常卡组等价 | ⚠️ |
| E9 | 返回钮查脏 → 「丢弃改动?」两钮窗 → 回滚副本 | ✅ | `TryClose`：脏 ⇒ `ConfirmDiscard`；`ShowPopUp("MenuDeck/HUD/DiscardChanges", primary=Discard→HidePopUp+Close, second=Cancel→HidePopUp)`（`DeckEditingWindow.cs:613-623,787-854`） | `TryClose`（`:5678-5683`）+ `ShowDiscardChangesPopUp`（`:5688`）+ `DiscardChangesAndLeave`（`:5713`）+ `RollBackEditingCopy` | 无（比原版多做了内存回滚，等价于原版「改副本、丢副本」） | ✅ |
| E10 | 保存成功 / 失败都关窗；失败不清脏、可重试 | ✅ | `UploadDeck` → `g__OnSave_0` 末句虚槽 `GameWindow.Close()`（成功失败同末句） | `SaveAndSay` 两个支都 `BackToMenu()`（`:5579-5590`），失败保 `DeckDirty` | 无 | ✅ |
| E11 | 离场「按来路回」 | ✅ | `Close.c:19-20`：放回导航栏 → `GameWindow.Close()` → 若 `currentWindow is CollectionScreen` ⇒ `ReturnFromImportDeck(deck.GameMode)` | `LeaveByHostWindow`（`:5300-5335`）：关宿主窗 + `CollectionData.TakePendingReturn()` 分源；收藏支显式 `ChangeTab(ret.Tab)`；LiveOps 支如实出声「数据缺失回不去」 | 收藏支等价；LiveOps 支原版是另一扇窗（本地无事件数据）—— 已如实标注 | ✅ |

### F. 卡背页（Cosmetics，4）

| # | 是什么 | 是否实现 | 原版参数（出处） | 我们参数（文件:行） | 差异 | 档位 |
|---|---|---|---|---|---|---|
| F1 | 卡背网格（桌面） | ✅ | `Cosmetic Display/Scroll View` 同一族 RSR：`250×405` · `_segments=4` · 视口 1589.78 ⇒ 6 列 · PadX 44.89 | `CosmoCellW/H` 桌面 250×405、`CosmoCols=6`、`CosmoPadX=44.89`（`:3395-3400`） | 无 | ✅ |
| F2 | **卡背网格小屏档系数** | ⚠️ | `_mobileSizeScale = **1.2**`（`MonoBehaviour_-1923093106525475036.json` 实读）⇒ 300×486 · `floor(1589.78/300)=**5**` 列 | `CosmoCellW/H` 乘 **1.5**（`:3396`，注释写「与卡池同一条」）⇒ 375×607.5 · **4** 列 | **系数错**：卡池 1.5 ✓、卡背 1.2（AUD05 §5·6 的收藏窗表也写着卡背 ×1.2）。小屏档下卡背页格尺寸与列数都偏 | ⚠️ |
| F3 | 装备卡背：★右键 装备 / 左键什么都不做 / 拖到侧栏也行 | ✅ | `OnCosmeticClick`：只有 `button==Right` 且拥有 ⇒ `editingPanel.Drop(cosmetic)` ⇒ `deck.cardbackId=id; syncedToServer=0`（`DeckEditingWindow.cs:226-239` · `DeckEditingPanel.cs:160-173`）；`CheckCosmeticDrag` 拖拽同效 | `HandleCosmeticClick`（`:3685-3705`）+ `EquipCardback`（`:3670-3684`，只标脏） | 无 | ✅ |
| F4 | 侧栏「已装备卡背」抽屉 + 卡背页 Owned 开关默认关 | ✅ | `Sidebar/Deck Details/Cosmetic Drawer` 0.25,485.5→335.56,885.5（`cosmeticImage`）；`showOnlyOwnedCosmetics` 初值 `false` | `cosm_drawer`（`:3403-3432`）；`_cosmoFilter` 默认 `DeckFilter.None`（`:2602`，`Owned=true` 但单机全解锁 ⇒ 无差别） | 无（卡背页 Owned 开关我们在 `DeckFilter.None` 里恒 true；原版该页初值 false —— 单机全解锁下画面无差） | ✅ |

### G. 提示（2）

| # | 是什么 | 是否实现 | 原版参数（出处） | 我们参数（文件:行） | 差异 | 档位 |
|---|---|---|---|---|---|---|
| G1 | 错误横幅（加不进卡 / 拖拽被拦） | ✅ | `UIMessageController.ShowError(error, localize:false)`（`DeckEditingWindow.cs:251-252` · `:1430-1433`）；`ShowError` 只转调 `ShowMessage(msg, errorColor)` | `SayError` → `ErrorMessageBanner.ShowShellError`（`:8862-8866`）；`TryAddCard`（`:8276`）、`BeginCardDragAt`（`:4067`） | 无（文案走 `DeckRules.ErrorTextKey` → 原版 `MenuDeck/Error/*` 具名键） | ✅ |
| G2 | 点击音（按钮 / 筛选格 / 起拖） | ✅ | prefab 全树 `m_onClickSound` 非空 = **12 处**：`Header/Filters`(SelectToggle) + `ClearFilters`/`Close`/`Done`(Positive) + **7 颗筛选 toggle**(SelectToggle)；**卡池格与卡组行没有点击音**；起拖音 = `CardDraggingController.dragSound`（两实例共用同一 cue） | 7 颗按钮表（`:5464-5471`）+ 筛选格 `:7777`/`:7941` + 起拖音 `:3777`；卡池/行无音（与原版一致） | 无 | ✅ |

---

## ③ 问题清单（用户可见优先）

1. 🔴 **Done 钮在卡组不合法时仍印「完成」**（原版印「丢弃」）—— `DeckRuntime.cs:2489` 只建一次文案；判据 `DeckEditingWindow.UpdateDoneButton`（il2csharp `:565`）。两个键官方表都在（`MenuDeck/MenuButtons/Done` = 完成 / `…/Discard` = 丢弃），改法很小：`RefreshHeader` 已有 `State.Validate()`，按结果 `SetText`。
2. 🔴 **小屏档卡背页系数错（1.5 vs 1.2）** —— `DeckRuntime.cs:3396`。桌面无影响；开了「设置→图形→Small UI」后，卡背格 375×607.5 / 4 列（原版 300×486 / 5 列）。
3. 🟠/⚠️ **合法性变真那一下的反馈缺两项**：`DOPunchScale` 弹跳 + `doneButtonEnabled` 音效（`DeckEditingWindow.cs:574-586`）；我们只有 Highlight 淡入（`ToggleDoneHighlight`）。
4. ⚠️ **卡池「按督军分流」机制与原版不同**：我们是内容硬闸，原版是「Army 筛选预选 + 卡类 `ShowOnlyHero`」。边角差异两条（选别的阵营出空 / 无督军时卡类筛选行仍显示）。
5. ⚠️ **卡组行行内拖动 = 重排**，原版这扇窗没有重排（拖起即删）。依据疑似错挂到 `Collection Deck` 的 `DragAndDropItem`。
6. ⚠️ **干净但内容不合法的卡组**：原版 Done/ESC 直接关窗，我们会弹错误窗（`SaveAndSay` 不看脏标记）。
7. ⚠️（低）**费用曲线柱宽瞬时跳变**，原版每格 `DOValue(…,0.2 s)` 补间（`DeckEnergyCostDrawer.Initialize(…, instant:false)`）；我们 `ApplyCurveBarWidth` 直接摆位。
8. ⚠️（低）首开时 Done Highlight 用 0.3 s 淡入，原版 `firstTime` 用 `duration 0` 硬切。
9. 🟠 **`Upgradable only` 筛选恒空**：依赖尚未实现的卡牌升级系统（原版按「可升级」筛）。我们已在界面出声说明，属于跨线依赖。

---

## ④ 查不到清单（`❓`）

| 项 | 已搜/已读 | 还差什么 |
|---|---|---|
| ❓ `CollectionDisplay<T>.OnListDrag` 基类方法体 | `cpp2il/out_il2csharp/Assembly-CSharp/CollectionDisplay.cs:132-135`（`NotImplementedException("Method body was not recovered.")`）· `decomp_full` 无该泛型体 · `decomp_generic_trial/out/` 未见 | 无法确认拖动列表时 `Reference Card Pointer` 是否会**跟随指针**（我们按「固定矩形 + 0.1 s 后求交」实现，`DeckRuntime.cs:4925-4950`）。影响仅页眉徽记那一下 |
| ❓ 卡组行拖出的判定矩形 | `DeckEditingPanel__CheckCardSlotDrag.c` 读到调 `SupportMethods.IsScrollDragThreshold` 与 `RemoveCard`，但**比的是哪一个 rect** 在该方法体里读不到（我们注释已如实标） | 需要 VA 级反汇编或实拍；今天按 325.4（抽屉右缘） |
| ❓ 卡背页 Viewport 高 | AUD05 §6·4 已记「未实读」；本次 `CosmoH=924.06` 沿用同族值 | 若要坐实小屏 5 行，需要该件 RT 的父链现算 |

---

## ⑤ 顺手发现（不属本窗，但值得记账）

1. 🟡 **简报里那条「左侧卡组列表 = `GridLayoutGroup 360×58 / 3 列`」的判据来自 `Deck info Popup.prefab`**（`grep "m_CellSize: {x: 360"` 在 `ExportedProject/Assets` 下**只命中** `Deck info Popup.prefab`）。建议把该参数在 `任务线/界面线/任务/T01_卡组编辑窗.md §2·6` 的表述改清：那边写的本来就是「弹窗那一支」，但容易被后续简报误读成本窗。
2. 🟡 **`CardArmyFilter__SetFilterToDeck.c` 的空卡组支是 `SetActive(**true**)`**，而 `Unity/MyGame/Assets/CardPresentation/Core/FilterPanelModel.cs:585-594` 的注释写「阵营那一格 …… **自身隐藏**」—— 该「隐藏」在 `.c` 里找不到（`FilterToArmy` 也不动 `SetActive`）。**真正被隐藏的是「卡类」那一行**（`CardTypeFilter.cs:26-27`）。建议就地订正这句注释（铁律 5）。
3. 🟡 **`DeckEditingHeader.SetInstructions` 全反编译 0 调用点**（`DeckEditingHeader.cs:13`）⇒ `Header/Instructions` 与 `Header/label`（'Edit your deck'）在 prefab 里恒 `INACT` 是**原版就这样**，不是我们缺件。别按截图去点亮它。
4. ⚪ `DeckEditingWindow.UpdateDoneButton` 里 `doneButton.text.text = …` 之后还有一段 `if (flag) { }`；ILSpy 把赋值分支吞了，**il2csharp 那一份读得到三目**（`:565`）—— 判「这行到底印哪个键」时以 il2csharp 为准。
5. ⚪ 本窗 `DeckEditingPanel.Initialize` 写死 `"{0}/30"`，而 `DeckListDrawer.SetCount` 另有 `"0/{MaxDeckSize}"`（`:657-665`）—— 两处不同口径，**本窗走前者**（后者在别处用）。我们只用了 `DeckCount/MaxDeckCount` 一条出口，没有这个分叉，属于「少写一份」而非缺口。

---

## 附：本次覆盖边界

| 维度 | 覆盖 |
|---|---|
| prefab | `menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 40` 现跑一次（247 行全树）+ `Deck Editing Menu.prefab` YAML 原文（VLG/CSF/DeckListDrawer 实例字段/全部 `mTerm`） |
| 反编译 | il2csharp 主力：`DeckEditingWindow.cs`(1096) `DeckEditingPanel.cs`(410) `DeckEditingStateController.cs`(65) `DeckEditorCollectionDisplay.cs`(258) `DeckListDrawer.cs`(843) `DeckEnergyCostDrawer/DeckCostQuantityRowDrawer/DeckTypeCounterDrawer/CardDeck/GameplayVariablesData/CardTypeFilter/CardCostFilter`；`.c` 复核 `TrySaveDeck/TryClose/Close/ConfirmDiscard/CardArmyFilter.SetFilterToDeck` |
| 我们的代码 | `Deck/DeckRuntime.cs`(10284，按节读) · `Deck/DeckEditorState.cs`(1575) · `Shell/DeckEditingWindow.cs`(217) · `Core/FilterPanelModel.cs`（相关节） |
| 未读 | `DeckEditingWindow` 的 `UploadDeck` 全文（服务器路径，P3）· `CardbackCollectionDisplay` 全量 · 特效/音效实现细节 |
| 没跑的东西 | Unity 批处理 / 真机 / typecheck（只读代理红线） |
| ⚠️ 提醒 | 「参数查清了」≠「画面对了」：本表只答「原版哪个状态是什么参数、我们代码取的是不是它」，不答「并排看像不像」（那属调度台验收） |
