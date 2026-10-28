#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""B93_E7 · 落点错配第 5 批 50 条：账补丁生成器。
只读主表 -> 只写 _parts/B93_E7.tsv；不改主表，不改源码。
读/写一律裸 split("\\t")（⛔ 不用 csv 模块）；LF、无 BOM。
行定位一律按 (表, 原版类规范名, 方法名) 现读 —— 主表在本轮期间被调度台合并改动过
（泛型与启动链_补读 整表位移），⛔ 不用旧行号。
"""
import io, re

BASE = 'd:/4/Unity/资料/完全复刻/'
HDR = ['域', '原版类', '方法', '判据出处', '判据要点', '状态', '我们的落点', '差异', '等级', '轮次', '备注']

F, C, M, SRC, KEY, ST, LAND, DIFF, GRADE, ROUND, NOTE = range(11)


def norm_class(s):
    return re.sub(r'<[^>]*>', '', s).replace(' ', '').strip()


def mname(sig):
    return sig.split('(')[0].strip()


def load(t):
    return [l.rstrip('\n').split('\t') for l in io.open(BASE + '账/' + t, encoding='utf-8-sig')]


def locate(t, cls, method):
    rows = load(t)
    cn, mn = norm_class(cls), mname(method)
    hits = []
    for i, r in enumerate(rows):
        if len(r) < 11:
            continue
        if norm_class(r[1]) != cn:
            continue
        if mname(r[2]) != mn:
            continue
        hits.append(i)
    if len(hits) != 1:
        raise SystemExit('定位失败 %s %s.%s -> %r' % (t, cls, method, hits))
    return rows, hits[0]


TAG = '｜2026-10-28 B93_E7 现核'

# ---------------------------------------------------------------- 判词
E = {}


def put(t, cls, method, st=None, land=None, diff=None, note_append=None, diff_append=None):
    E[(t, cls, method)] = dict(st=st, land=land, diff=diff, note_append=note_append, diff_append=diff_append)


# ============================== 个人资料/成就/排行
put('个人资料_成就_排行.tsv', 'AvatarDisplay',
    'Initialize(PlayerAvatar avatar, CosmeticItemAvatarBorder border, bool highlight, bool isLocked)',
    diff='等价（逐句）：`AvatarDisplay__Initialize.c` = 记 avatar(+0x38) → `ChangeAvatar(avatar)` → `ChangeBorder(border)`'
         ' → 虚槽 `+0x1c8`（= `ToggleHighlight(highlight)`）→ 虚槽 `+0x208`（= `ToggleLock(isLocked)`）。'
         '本仓 `BuildRows` 建格时同序：`Image Container` 里 `Highlight`(1) → `Border`(2) → `Image`(3) 三层（原版兄弟序），'
         '高亮位 = `hl.SetActive(i == Selected)`（:428-432）；`border` 由 `AvatarBorderController` 那两行覆盖；'
         '`isLocked` 一档在**全解锁口径**下恒假（用户 2026-09-17 裁）。'
         '⚠️ 原版是「逐格 Instantiate + 每格一个 AvatarDisplay 组件」，我们是同一棵树上按格建三层 —— 载体不同、逐格可观察结果同。')

put('个人资料_成就_排行.tsv', 'AvatarDisplay', 'ToggleHighlight(bool value)',
    diff='等价（逐句）：`AvatarDisplay__ToggleHighlight.c` 全篇三句 = `if (+0x70 == null) return; SetActive(+0x70.gameObject, value)`。'
         '本仓 `Refresh` 逐格 `SetActive(_cellIdx[k] == PreviewIndex)`（:624-625）= 同一判据「旧选关、新选开」'
         '（原版换预览时也是先对旧格 `ToggleHighlight(false)`、再对新格 `true`，`AvatarTab__OnAvatarClick.c:25/28`）。')

put('个人资料_成就_排行.tsv', 'CampaignProfileContainer', 'OnInitialize(PlayerInfo context)',
    diff='等价（职责级、逐段）：`CampaignProfileContainer__OnInitialize.c` = ① `new Campaign()` ② 用 `PlayerInfo` 的 `+0x68/+0x60`'
         ' 两字段 + `LiveOpsManager.GetHandler<CampaignManager>()` 灌它 ③ 写一条本地化标题 ④ 按「有没有 campaign 数据」开/关那颗 `Behaviour`'
         ' ⑤ 逐战役区块（战将掌握度等）。本仓 `BuildEvents` 建的就是第 ⑤ 段那批区块（`Warlord Mastery Container` 等）；'
         '①②③ = 远端 liveops 数据链（`Campaign` / `CampaignManager`），本仓由离线战役表替代并出声（🔶 服务器相关、用户 2026-09-17 边界）。'
         '⚠️ ②③ 没有逐句等价物（如实记，不假装做了）。')

# ============================== 卡牌面/渲染
put('卡牌面_渲染.tsv', 'CardTextsController', 'ChangeState(CardNameAndDescriptionTypes stateToChange)',
    diff='等价：`CardTextsController__ChangeState.c` = `if (当前态(+0x58) == 目标) return;` → 在 `states[]` 里找 `entry.type == 目标` 那条'
         ' → `CardNameDescriptionToggleState.ToggleState(entry)` → `当前态 = 目标`。本仓没有这台状态机：**态由 `ComputeTextState(d)` 按数据现算**'
         '（9 态映射，`CardView.cs:5159`，判据只此一处），再由 `BuildTextLayers` 把 `_textState` 落到同一组 TMP 的显隐/字号上（`:4891` 起）。'
         '⇒ 「同一输入 ⇒ 同一态 ⇒ 同一组文字可见」；差异 = 我们**不缓存 currentState**（每次重建现算，等价于原版「态变了才切」的净结果）。')

put('卡牌面_渲染.tsv', 'CardTextsController', 'SetDescription(string cardDescription)',
    land='CardPresentation/Core/CardView.cs:4876（BuildTextLayers）',
    diff='等价：`CardTextsController__SetDescription.c` = 记 `originalDescription` → 逐个 `descriptionTexts` 灌字 → 按「战术 / 有无描述」二次选态。'
         '本仓 `BuildTextLayers` 里 `_keywords = Fill(…)` 那一支就是「把效果文字写进描述 TMP」（原落点列里那半句「段：…」已移到本列），'
         '随后 `_textState = ComputeTextState(d)`（:4891）按同一组判据（`isUnit` / `keywords` 有无 / `SubtypeLine`）选态 ⇒ 同一个入口、同一组判据。'
         '（落点列原来塞了两段 `文件:行`，机检只评估第一段 ⇒ 已收敛成单段。）')

# ============================== 卡组编辑
put('卡组编辑.tsv', 'DeckEditingPanel', 'AddCard(RawCardScript card, bool withAnim)',
    diff='等价（逐句）：`DeckEditingPanel__AddCard.c` = ① `CardDeck.CanAddCard` 假 ⇒ `UIMessageController.ShowError(out error)`'
         ' ② 卡型 10（督军）且有旧督军 ⇒ `RemoveCard(旧督军)` ③ 防御卡（type 0x14 / subtype 0xd2）且已有 ⇒ `RemoveCard(旧防御)`'
         ' ④ `CardDeck.TryAddCard` ⑤ `DeckListDrawer.AddCardToList` ⑥ 回调 + `空卡组提示.SetActive(CardCount(true) < 1)`。'
         '本仓：①⇒ `TryAddCard`(`DeckRuntime.cs:8276`) 里 `State.TryAdd` 的 `out why` 走 `SayError`（屏幕错误横幅）；'
         '②③⇒ `DeckEditorState.TryAdd`(`:473`) 用**单槽字段**表达（`Type=="hero" ⇒ WarlordId = id` / `Type=="defence" ⇒ DefensiveId = id`，'
         '等价于「先移除旧的再加新的」）；④⑤⑥⇒ `MarkDeckDirty() + RefreshAll()`（列表/计数/页头一起重算）。')

put('卡组编辑.tsv', 'DeckEditingPanel', 'Initialize(CardDeck deck)',
    land='Deck/DeckRuntime.cs:4137（RefreshAll）',
    diff='**落点改指**：原落点 `NewState()` 只是 `new DeckEditorState(CardDatabase.Load())`，**没有 `Initialize` 的任何一步**。'
         '原版 `DeckEditingPanel__Initialize.c` = ① 记 deck(+0x70) ② 卡数文本 `Format(CardCount(false))`（有自定义模式再拼模式上限）'
         ' ③ `DeckListDrawer.Initialize` ④ `DeckTypeCounterDrawer.Initialize` ⑤ `DeckEnergyCostDrawer.Initialize` ⑥ `DeckCosmeticDrawer.Initialize`'
         ' ⑦ 名字输入框 `set_text(deck.Name)` ⑧ 空卡组提示 `SetActive(CardCount(true) < 1)`。本仓：①= `Build(DeckLibrary)`(`:1276`) 选定要编辑的那一副；'
         '②⑦⑧= `RefreshHeader()`；③= `RefreshDeckList()`；④⑤⑥= 切到对应页时重建；`RefreshAll()`(`:4137`) = ①之后把 ②③（及卡池/筛选）一起刷。'
         '⇒ 同一批可观察项，落点收敛到「刷新全部」那一处。')

# ============================== 商店
put('商店.tsv', 'GeneralOfferPopupDrawerBase', 'Initialize(ShopOfferBase shopOfferData)',
    land='Shell/BaseOfferPopup.cs:1111（BuildDrawers）',
    diff='**落点改指**：原落点 `FillDef`（数据栏补出厂值）与 `Initialize` 无关。`GeneralOfferPopupDrawerBase__Initialize.c` = '
         '`DrawRewards(Enumerable.Concat(offer.GetRewards(), this[+0x11] 额外奖励表))`（末尾转调 `+0x188`）。'
         '本仓 `BuildDrawers(Variant)`(`BaseOfferPopup.cs:1111`) = 按当前变体逐个抽屉槽 `ItemDrawer.Draw(..., DrawerOverride.OfferPopups)`；'
         '`OfferPopups = 30 (0x1e)` 与原版同一档（判据 = `GeneralOfferPopupDrawer.DrawRewards.c:319` 传的就是 `0x1e`）。'
         '⚠️ 奖励表来源不同：原版 = 服务端 `offer.GetRewards()` + 抽屉自带的额外表；本仓 = 本地 `C.Item` 单件 + 每槽按变体（本地没有报价 ⇒ 槽照建、里面不画）。')

put('商店.tsv', 'InAppOfferContainer', 'OnInitialize(IShopOffer offer)',
    st='D',
    diff='**A→D（2026-10-28 B93_E7）**：原版 `InAppOfferContainer__OnInitialize.c` = **真钱 IAP 报价容器** —— `ShopElementType == 0x1e` 时 '
         '`GetIapData()` 为空 ⇒ `Object.Destroy(this)`；否则记 offer → 标题/副标题两条词条 → 两张 sprite（`LiveOpsAssetUtility.LoadAsset`）'
         ' → `WebShopOpenButton.OfferAllowsShow` → 刷一次。⇒ 判 **D**：本工程**不做真实经济**（用户 2026-09-17 边界②：资源固定 2000、拒真钱链），'
         '且商品目录/价格档全在服务端（P3）。本仓没有这一件 —— 商城格是 `CatalogItemContainer`（**另一个原版类**）的等价物 `BuildCell`(`ShopTabPage.cs:484`)，'
         '`WebShop` 钮只出声（`BaseOfferPopup.cs:1057` 的 `WebShopClick`）。⚠️ 可逆：若日后要做 IAP 展示（不真扣款），回 B 重判。',
    note_append=TAG + '：A→D（真钱 IAP 容器，用户边界②不做真实经济 + 目录在服务端）')

# ============================== 战役
put('战役.tsv', 'CampaignUIBackground', 'Initialize(Campaign campaign)',
    diff='等价（逐句，拆两个入口）：`CampaignUIBackground__Initialize.c` = ① `+0x40 == null`（首次）⇒ 把图色 alpha 置 **0**'
         ' ② `+0x40 != campaign` ⇒ 记 pending(+0x38) + 把 `+0x48` 置 0（= 淡出到 0，`Update` 里到位才换图）。'
         '本仓：首次那半 = `InitBackground(army)`(`:1135`，alpha 0 → 目标 1 ⇒ 淡入) + `LoadBackgroundImage(army)`(`:1144`，换图 + 目标 1)；'
         '换战役那半 = `CloseBackground()`(`:1157`，目标 0) + `Tick` 里的 pending 换图（见下一行 `Update`）。'
         '⚠️ 原版 `Initialize` 只做①②，淡入那半在 `LoadImage`；我们建窗时把「首次 + 淡入」合成一次 `InitBackground` ⇒ 净结果同（背景从 0 淡入）。')

put('战役.tsv', 'CampaignUIBackground', 'Update()',
    diff='等价（逐句）：`CampaignUIBackground__Update.c` = `alpha` 以 `dt × changeSpeed`（字段值 **2f**）`MoveTowards` 目标；'
         '到 0（≈）且 pending(+0x38) ≠ 当前(+0x40) ⇒ `LoadImage(pending)`。本仓 `Tick(dt)`(`:1170`) 同式'
         '（`BgChangeSpeed = 2f`；`_bgAlpha <= 0.001 && _bgTarget <= 0.001 && _bgPending != _bgArmy` ⇒ 换图），'
         '`Refresh()` 改成「置 `_bgPending` + 目标 0」⇒ 淡出 → 换图 → 淡入。')

# ============================== 收藏
put('收藏.tsv', 'CollectionCard', 'Start()',
    diff='等价：`CollectionCard__Start.c` 全篇两句 —— `Card2DController.ChangeHighlightColor(default)` + `Card2DController.ToggleHighlight(false)`（出厂复位）。'
         '本仓等价物 = `CardView.SetHighlight(CardHighlightState.Normal)`(`CardView.cs:8214`)：tint 回 Normal、灰度 0、`SetHighlightScale(false)`；'
         '「高亮那一层关掉」那半 = 建格/复用时 `hl.SetActive(i == Selected)`（收藏窗建格处调，`CollectionWindow.cs:1670` 一带）⇒ 出厂与复用两条路都复位。')

put('收藏.tsv', 'CollectionCardCounter', 'Set(string counterText, bool setGreyscale)',
    diff='等价（拆两半）：`CollectionCardCounter__Set.c` = ① 文本件(+0x20) `set_text(counterText)` ② `UIImageGreyscaleController.ToggleGreyScale(+0x28, setGreyscale)`。'
         '本仓 `BuildCardsCounter`(`:353`) 同一处建条 + 写文本（`CardsCounterText`，`x{拥有}` 口径见该函数 doc）；'
         '② 置灰那一支**不可达且没画**：`owned < 1` 在我们「固定 2000 资源 + 给足拥有」的口径下恒假，而且原版那颗是 `UIImageGreyscaleController`'
         '（`Image` + 灰材质对），我们的 `ImageQuad` 没有对应灰值 ⇒ 只记不画、不自己编灰度（`CollectionWindow.cs:306-320` 已写明）。⇒ 可观察同。')

put('收藏.tsv', 'CollectionItem', 'OnBeginDrag(PointerEventData eventData)',
    diff='等价（转发层 vs 处理层）：`CollectionItem<object>__OnBeginDrag.c` = `if (!listenToPointerEvents(+0x40)) return;` → '
         '`EventRelay.Raise<IBeginDragHandler>(relay(+0x28), eventData, this.gameObject)` —— 它**只转发**，真正干活的是监听者 `DragAndDropItem.OnStartDragEvent`。'
         '本仓不走上游 `EventRelay`：`AttachDeckDrag`(`CollectionWindow.cs:6107`) 把「起拖/落拖」两个回调**直接**登记给 `PointerLayer.RegisterDrag`(`:6122-6124`)'
         ' ⇒ 起拖 = `DeckDragMove`(`:6131`) 的首次那一支（记 home / 记按点 px / 抬渲染队列 +20 = 原版 `SetParent(root)` 的等价）。⇒ 同一趟指针事件链，少一层转发。')

put('收藏.tsv', 'CollectionItem', 'OnDrag(PointerEventData eventData)',
    diff='等价（转发层 vs 处理层）：`CollectionItem<object>__OnDrag.c` 与 `OnBeginDrag` 同形（同一道 `+0x40` 闸 + `EventRelay.Raise<IDragHandler>`），'
         '真干活的是 `DragAndDropItem.OnDrag`。本仓 = `PointerLayer` 每次 move 直接回调 `DeckDragMove`(`:6131`)：`cell.localPosition = home + 位移`'
         '（位移按父 `lossyScale` 与 `LayoutSpace.FromPixel` 的差换算，见 `DeckDragDesignDelta`）。')

put('收藏.tsv', 'CollectionItem', 'OnPointerClick(PointerEventData eventData)',
    diff='等价（拆两半）：`CollectionItem<object>__OnPointerClick.c` = `if (!listenToPointerEvents(+0x40)) return;` → '
         '`EventRelay.Raise(clickEvent, pointerId = *(eventData+0x188))` → 若 `onClickSound(+0x38)` 非空则 `SoundManager.Play2D(它)`。'
         '本仓：点击 = 每格 `MenuDraw.Hit` 建的 `WindowButton` → `WindowButton.Click()`(`WindowButton.cs:1048`，`interactable`/`absorbOnly` 两条闸) → '
         '① 音 = `PlayClickSound()`（卡池/异画格 cue = `CardClickCue = "CardClick 1"`，= 原版 `Collection Card.onClickSound`；'
         '卡背格 `DisableClickSound` = 原版 `Collection Cosmetic.onClickSound = null`）；② 派发 = 命中区闭包 → `OpenCardDetail(def)`'
         '（`CollectionWindow.cs:1684` / `:2673`）。⇒ 同一条「选中 + 播 on-click 音」。')

put('收藏.tsv', 'CollectionTab', 'GetItems()',
    land='Shell/CollectionWindow.cs:188（CardsCells）',
    diff='**落点整个指错**：原落点 `CollectionTabPage.SetupOnce`（懒建入口）与 `GetItems` 无关。`CollectionTab<object>__GetItems.c` 全篇一句 = '
         '`return display.scrollRect.Cells;`（当前可见 cell 列表）。本仓没有 `RecyclableScrollRect`（整页建格 + 自持列表）⇒ 等价物 = 各页 cell 列表：'
         '`CardsCells`(`CollectionWindow.cs:188`) / `DeckCells`(`:938`) / `CosmoCells`(`:1798`) / `StyleCells`(`:2277`)，落点取 Cards 页那一份。'
         '⚠️ **原版它的可观察消费方只有两个**：`CardCollectionTab.GetCards()`（ILSpy 全库**只有定义、0 调用点**）与 `CardCollectionTab.Test()`'
         '（调试方法，0 调用点）⇒ 今天无活消费方，等价与否不影响玩家所见（但落点仍必须指对，否则以后接消费方时会照错的看）。')

put('收藏.tsv', 'ItemDrawer', 'OnPointerClick(PointerEventData eventData)',
    land='Shell/ItemDrawer.cs:1659（OnPointerClick）',
    diff='**落点改指**：原落点 `PlayInspectSound` 只覆盖原版第 ④ 步那一声。原版（`out_cpp2il_dev_ilspy/ItemDrawer.cs:917-951`）四步：'
         '① `showItemInfoOnClick` 假 ⇒ return ② item 空 ⇒ `CustomDebug.LogError(...)` ③ `onClick` 有就跑它、否则 `HandlePointerClick()`'
         ' ④ 末尾 `inspectSound.Play2D()`。本仓 `ItemDrawer.OnPointerClick()`(`ItemDrawer.cs:1659`) 逐条对上：`ShowInfoOnClick` 闸 → `Id` 空 ⇒ 同一句 LogError'
         ' → `_onClick`(`OverrideOnClickAction`) 或 `HandlePointerClick()`(`:1672`：卡组抽屉走 `DeckClick()` 开卡组详情窗，其余开 `ItemDisplayWindow`)'
         ' → `PlayInspectSound()`(`:2038` → `PlayInspectCue`)。')

# ============================== 教程/引导
put('教程_引导.tsv', 'TutorialManager', 'PlayTutorial()',
    land='Shell/MenuTutorialFlow.cs:223（StartFlow）',
    diff='等价（三段对三段）：`TutorialManager__PlayTutorial.c` = `StopAllCoroutines` → 起 `_DelayedPLayTutorial` 协程'
         '（等一帧 → currentStage 清空 → `DisablePointer` → 从数组头找第一个「未完成且 CheckForCompletion 假」的 stage → Execute）。'
         '本仓：`StartFlow()`(`:223`) + `FirstPlayableStage()`(`:286`) + `Enter(i)`(`:300`) 正好是这三段；推进（原版 `FlowTutorialStage.Complete` 重启协程）'
         '在 `Advance()`(`:347`)：只标完成 + **从数组头**再找第一个（⛔ 不是 `_index+1`）。'
         '⚠️ 批处理里没有帧循环 ⇒「等一帧」在我们这边同拍完成（净结果同）；`DisablePointer` = `_pointer.Hide()/HideImmediate()`。'
         '（落点列原来塞了 4 段 `→`，机检只认第一段 ⇒ 已收敛成单段，其余进了本列。）')

put('教程_引导.tsv', 'TutorialMessageData', 'Reset()',
    diff='等价但**今天不可达**（如实记）：`TutorialMessageData__Reset.c` 全篇一句 = `alreadyShown(+0x28) = false`；它只被 '
         '`FlowTutorialStage.ResetTutorialMessageData()`（反射式逐字段调）调用，而后者只被 `FlowTutorialStage.Reset()`'
         '（`completed = false; ResetTutorialMessageData();`）调用 —— **ILSpy 全库找不到 `FlowTutorialStage.Reset()` 的调用点**。'
         '本仓等价物 = `MenuTutorialFlow._messageShownStage`(`:134`)：只在 `:327-328` 置位（弹过就置成当前阶段），**全仓无清零点**。'
         '⇒ 「不可达 ⇒ 无可观察差异」。⚠️ 若将来接上「重置教程进度」那条路，本格会立刻露出差异（那时必须同时清 `_messageShownStage`）。',
    note_append=TAG + '：`.c` 一行 + `FlowTutorialStage.Reset()` 0 调用点 ⇒ 维持 A；旧备注里「退 B」那句作废')

# ============================== 日常/奖励
put('日常_奖励.tsv', 'ConstantChallenge', 'OnInitialize()',
    note_append=TAG + '：`dump.cs:59083` ⇒ RVA `0x4B33B0`（全局空方法桩 `ret 0`）⇒ D 成立')

put('日常_奖励.tsv', 'ConstantChallenge', 'UpdateProgress(int newValue)',
    diff='= `UpdateProgressIfHigher`：只抬到至少 N（4 条成就走它）。判据 = `ConstantChallenge__UpdateProgress.c`（全篇一句转调 '
         '`MissionChallenge__UpdateProgressIfHigher`）⇒ 与 `ProfileData.SetAchievementValueAtLeast`(`:563`) 同一语义（`max(旧, N)`，只增不减）。')

put('日常_奖励.tsv', 'DailyLoginChallenge', 'OnReset()',
    land='Shell/DailyData.cs:1733（ResetDailyProgress）',
    note_append=TAG + '：`dump.cs:59155` ⇒ RVA `0x4B33B0`（全局空方法桩）⇒ D 成立；'
                      '落点由 `:1716`（现读是方法体内的一句 `RecordLogin(now);`，**不是声明行**）改指 `ResetDailyProgress` 的声明行 `:1733`')

put('日常_奖励.tsv', 'LiveOpsEventWindow', 'SetupData(object data, GameWindowOptions options)',
    diff='等价（载体差异）：`LiveOpsEventWindow<object>__SetupData__1016c10.c` = `base.SetupData(data, options); '
         'if (Data(+0x40) != null) LiveOp(+0x70) = (T)Data;`（强转，类型不对 throw）。本仓**没有 `Data → LiveOp` 这一跳**：'
         '`LiveOpsEventWindow` 自己不带 liveops 实例，「服务哪个活动」由 ① 子类声明 `LocalLiveOpsEventId`(`:1520`) 或 ② 打开方填 `EventId`(`:2024`) 决定，'
         '`Open()`(`:549`) 按它排到期刷新（`ScheduleExpiryRefresh`）。⇒ 净效果同（窗知道服务哪个活动），差在**数据来路**（原版 = 服务端把实例塞进 `data`；我们 = 离线目录 id）。')

put('日常_奖励.tsv', 'LiveOpsEventWindowWithTabs', 'SetupData(object data, GameWindowOptions options)',
    diff='等价（载体差异）：`LiveOpsEventWindowWithTabs<object>__SetupData__1016780.c` = `base.SetupData(...); if (Data(+0x40) != null) +0xa0 = (T)Data;`（强转）。'
         '本仓带页签活动窗 = `ParentEventWindow`：「绑哪个活动」由 `Create(mgr, Page)`(`:552`) 的 `Page`/`EventWindowKind` + `LocalLiveOpsEventId` 定，'
         '`Open()`(`:573`) 建树/切页；`data → LiveOp` 那一跳**不存在**（我们没有把 liveops 实例塞进 data 通道）。'
         '⚠️ 本仓另有一份 1:1 镜像 `LiveOpsEventWindowWithTabs<T>.SetupData`（`LiveOpsEventWindowWithTabs.cs:63`），但它**全仓 0 使用者**（死镜像，见报告）。')

# ============================== 未分类/其他
put('未分类_其他.tsv', 'ChooseCardUI', 'Initialize(ChooseCardMenu chooseCardMenu)',
    diff='等价：`ChooseCardUI__Initialize.c` = `SelectCardButtonFrame.Initialize(cardUI, menu)` → `Card2DController.ToggleHighlight(false)`（出厂关高亮）。'
         '本仓 `Build(...)`(`CardChoicePanel.cs:405`) 同批建整块面板：每张卡的卡体与**卡下那颗 `Select` 钮**（`_cardBtns`，`:873` 建）→ `ApplyPicks` '
         '把高亮/可交互刷成「没选任何卡」的出厂态（确认钮禁用 = `SetConfirmInteractable(false)` 那条链）。⇒ 同一批可观察项（钮框建起来 + 出厂不高亮）。',
    note_append=TAG + '：双侧方法级已对（`_cardBtns` 建钮 + `ApplyPicks` 出厂态）⇒ 维持 A；旧备注「退 B」那句作废')

put('未分类_其他.tsv', 'ChooseCardUI', 'ToggleSelected(bool option)',
    diff='一致（等价实现 = TogglePick + ApplyPicks：原版 ToggleSelected 切高亮 + set_interactable(!option)；我们同一语义，Single 档不响应二次点击）'
         ' ｜`ChooseCardUI__ToggleSelected.c`（逐句）= `Card2DController.ToggleHighlight(selectButton.cardUI, option)` + `selectButton(+0x20).interactable = !option`；'
         '本仓 `TogglePick`(`:1003`)：`Single` 已选再点直接 return（= 原版那颗钮被 `set_interactable(false)` 收不到第二次点击）、换选另一张时由 `ApplyPicks` 把上一颗复位；'
         '`SetConfirmInteractable(true)`。',
    note_append=TAG + '：`.c` 逐句对上 ⇒ 维持 A；旧备注「退 B」那句作废')

# ============================== 特效动画
put('特效动画.tsv', 'AnimateUIMaterialProperty', 'OnDestroy()',
    diff='等价：`AnimateUIMaterialProperty__OnDestroy.c` 全篇 = `Object.Destroy(+0x78 那份实例材质)`。本仓没有这颗组件：实例材质由 `CardView` 持有，'
         '`EndFrameDissolve()`(`CardView.cs:6772`) 收尾时「还原 sharedMaterial + Destroy/DestroyImmediate 掉那份实例」（批处理下 `DestroyImmediate`，运行时 `Destroy`）。'
         '⇒ 同一件「实例材质必须销毁、别把原材质一起毁」。')

# ============================== 社交/聊天
put('社交_聊天.tsv', 'AllianceScoreBar', 'Initialize(int currentScore, int[] scoringThresholds)',
    diff='一致（等价实现 = SetMilestones）：判据逐句 `AllianceScoreBar__Initialize.c` = `Slider.wholeNumbers = false` → `maxValue = thresholds.Last()` → '
         '`normalizedValue = InterpolateMilestones(score, thresholds.Prepend(0).ToList())` → `SetScoringMilestones(thresholds, score)`。'
         '本仓 `SetMilestones`(`AllianceEventScoreInfo.cs:181`) 三件同一处：进度归一 = `InterpolateMilestones(score, [0, 阈值…])`（**按档位等分 + 段内按分值线性**）、'
         '`Slider.maxValue = 末档` 那一半用填充 quad 表达（无 Slider 组件，可观测等价）。',
    note_append=TAG + '：`.c` 四句与 `SetMilestones`/`InterpolateMilestones` 对上 ⇒ 维持 A；旧备注「退 B」那句作废')

put('社交_聊天.tsv', 'AllianceScoreBarLine', 'Initialize(int thresholdValue)',
    diff='等价（聚合级）：`AllianceScoreBarLine__Initialize.c` 全篇一句 = `text(+0x20).set_text(thresholdValue.ToString())` —— 每颗刻度线只负责写自己的阈值文本。'
         '本仓没有「每颗线一个组件」：由 `SetMilestones`(`AllianceEventScoreInfo.cs:181`) 一处按 currentScore + 5 个阈值建 5 颗刻度并写同一串文本'
         '（阈值不足 5 个出声不画），刻度判定与换图都在这里。')

put('社交_聊天.tsv', 'AllianceSearchTab', 'OnDestroy()',
    diff='等价（载体差异）：`AllianceSearchTab__OnDestroy.c` 全篇 = `JoinAllianceMenu.remove_OnDisplayAlliance(<本类的 handler>)`（退订）。'
         '本仓**不做事件层**：邀请行/公开行的命中区闭包直接调 `HandleDisplayAlliance`(`AllianceSearchTab.cs:362`)，随节点销毁一起消失'
         ' ⇒ 「没有退订这一步」但「销毁后不再触发」的净结果同。[非缺口]',
    note_append=TAG + '：`.c` 是一句退订，本仓无事件层 ⇒ 维持 A；旧备注「退 B」那句作废')

# ============================== 菜单/外壳
put('菜单_外壳.tsv', 'ChooseNamePopup', 'Initialize()',
    diff='等价（拆两半）：`ChooseNamePopup__Initialize.c` = ① 确认钮 `interactable = false` ② 读玩家名 `IsNullOrEmpty` ③ 词条 `Format(...)` 写标题'
         ' ④ 按「名字空/非空」切两棵 GO（`+0x98` / `+0xa0`）⑤ 钮 `interactable = !bVar5` + 钮文案（`Choose` / `Rename` 两条词条）。'
         '本仓 `BuildNameWindow()`(`ProfileTab.cs:1296`) = 建整棵树（输入框/占位/确认/取消）；填充与分支 = `OpenNameWindow()`(`:1444`) + `RefreshNameField()`'
         '（当前名恒非空 ⇒ 走「改名」那支）+ `ApplyName` 的三条提交判据。⚠️ 「名字空」那一支在我们这边不可达（本地玩家名恒有值）。')

put('菜单_外壳.tsv', 'ChooseNamePopup', 'Setup(Action<string> doneAction, Action closeAction)',
    diff='等价：`ChooseNamePopup__Setup.c` = 把 `doneAction` / `closeAction` 存进字段（供确认/关闭两颗钮用）。本仓不存委托：'
         '`OpenNameWindow(done, close)`(`:1444`) + `CommitNameWindow()`(`:1491`) / `CancelNameWindow()`(`:1499`) 三个具名方法直接当两颗钮的回调'
         ' ⇒ 同一件「配完成/关闭回调」，少一层委托字段。')

put('菜单_外壳.tsv', 'DragAndDropItem', 'Initialize(ScrollRect scrollRect)',
    land='CardPresentation/Deck/DeckRuntime.cs:9214（DragAutoScroll）',
    diff='**落点收敛**（原落点列塞了 5 段 `文件:行`，机检只认第一段）。原版 `DragAndDropItem__Initialize.c` = `scrollRect == null` ⇒ `LogError` + `enabled = false`；'
         '否则 `enabled = true` + 存 `+0x20`(scrollRect) + 缓存 `RectTransform` + 「没有静态占位就 `CreateEmptyObject`」。'
         '本仓**没有这颗 uGUI 组件**（拖拽由 `PointerLayer` 直派 + 自己算边缘滚动）：`DragAutoScroll`(`DeckRuntime.cs:9214`) 承担「拖到边缘自动滚」那一半'
         '（阈值 **50** / 速度 **10**，判据 = `DragAndDropItem__.ctor.c` 的 `+0x28 = 0x42480000` / `+0x2c = 0x41200000`）；`enabled` 那一半 = 我们按需 `RegisterDrag`。'
         '⇒ 功能半边等价，组件形态不同。')

# ============================== 轮抽
put('轮抽.tsv', 'DraftModeDeckInfoPanel', 'UpdateData()',
    diff='等价：`DraftModeDeckInfoPanel__UpdateData.c` = ① `EnergyBalancePanel.Initialize(<卡表>)` ② `CardListPanel.Initialize(<卡表>)` ③ 进度条那块：'
         '`LeaderboardManager.GetCachedLeaderboard` → `GetTotalByArmy` 两条 → `ProgressBar.SetProgress` + `ArmyProgressBar.SetArmies`。'
         '本仓 `RefreshDeckPanel()`(`DraftModeWindow.cs:2816`；本列旧写 `:2613` 已过期) = 三颗 toggle 底图 + `Cards:n/deckSize` 计数 + `_deckShowCounter` 显隐'
         ' + Deck Info 内容重建 + Card List 行重建（全部按当前 `_selectedCards` / `_deckTab` 现算）。'
         '⚠️ ③ 那个**双阵营排行榜进度条**由本地数据画（排行榜是服务端数据，P3）—— 要逐句等价物得另立账。')

put('轮抽.tsv', 'DraftModeMenuDemo', 'GetStartingTab()',
    diff='等价：`DraftModeMenuDemo__GetStartingTab.c` 全篇 = `if (+0xa0 != null) GetTabFromState(<当前状态>)`（状态 → 起始页）。'
         '本仓 `PageFor(DraftModeState s)`(`DraftModeWindow.cs:2139`) 就是那张「状态 → 页」的映射（返回该页的 Transform），`Open` 时按它落页。')

# ============================== 锻造
put('锻造.tsv', 'ForgeRewardItemContainer',
    'Initialize(ForgeRewardSelector rewardSelector, RewardData rewardData, int index, RewardData previousRewardData, int rewardObtained, int currentPoints)',
    diff='等价（聚合级）：`ForgeRewardItemContainer__Initialize.c`（10.6 KB）= 六参（`rewardSelector` / `rewardData` / `index` / `previousRewardData` / `rewardObtained` / `currentPoints`）'
         ' → 五态状态机（Locked / InProgress / ToCollect / ToCollectButFirstRedeemThePrevious / Collected）：逐态 `SetActive`、`ForgePointIconDrawer.DrawForArmy`、'
         '`UITransformTweenController` 的 `DoDefaultAnim`/`BackToDefault`。本仓 `ForgeTab.BuildCell(i)`(`ForgeTab.cs:995`) 同批：`ForgeData.StateAt(army, i)` 求五态 → '
         '建 `RewardTransform` + 奖励图标 + `PointsText`（cur/need）+ `UITransformTweenController`（`animTime 1.5` / `targetScale 1.12³`；3·4 态合一，判据 `:979` 起已写）'
         '+ 只在非 Locked 时整段建。⚠️ 原版的 `previousRewardData` 参数在我们这边换成 `StateAt` 的「上一格是否已领」推算。')

# ============================== 音频/语音
put('音频_语音.tsv', 'PlaySoundOnTime', 'Reset()',
    diff='等价：`PlaySoundOnTime__Reset.c` 全篇一句 = `*(int*)(+0x2c) = 0`（播放计数归零）。本仓没有这颗组件：等价物 = `WFExtraShakeAndSound.Apply()`(`:33`) 里 '
         '`_t = 0` + `_played = new int[sounds.Length]`（重建即归零）；`WarpforgeEffectPlayer` 那条路是重建 `SoundSlot` 列表（同一句 `s.played = 0`）。')

put('音频_语音.tsv', 'PlaySoundOnTime', 'Update(float currentTime, Transform transform)',
    st='B',
    diff='🔴 **2026-10-28 B93_E7 现核 ⇒ 降 B**：`PlaySoundOnTime__Update.c` 的节拍器是**一次调用最多播一条** —— `max = repeat(+0x21) ? loops(+0x24) : 1`；'
         '`if (max <= played(+0x2c)) return;`；`if (currentTime < played*interval(+0x28) + time(+0x10)) return;`；播一句后 `*(int*)(+0x2c) = *(int*)(+0x2c) + 1` 就 return。'
         '本仓 `TickSounds()`(`WarpforgeEffectPlayer.cs:777`) 用的是 **`while`**（源码注释自陈「一帧跨过好几个区间时不丢拍」）⇒ **掉帧/长帧时我们把一个区间里欠的拍一次全播，原版只播一条**'
         '（原版那几拍永久少播）。按「照抄原版、⛔ 不修原版的 bug」口径（铁律 18·0）⇒ 判据差。修法：把那一处 `while` 改成单次 `if`（每帧至多 `played += 1`）。'
         '⚠️ 同族：`WFExtraShakeAndSound.ExtraTick` 也是 `while`（同一处口径，另立/现账核）。')

# ---------------------------------------------------------------- 生成
out = [HDR]
used = set()
order = []
for (t, cls, method), ed in E.items():
    rows, idx = locate(t, cls, method)
    src = rows[idx]
    r = list(src)
    if ed['st']:
        r[ST] = ed['st']
    if ed['land']:
        r[LAND] = ed['land']
    if ed['diff']:
        r[DIFF] = ed['diff']
    if ed['diff_append']:
        r[DIFF] = r[DIFF] + ed['diff_append']
    if ed['note_append']:
        r[NOTE] = r[NOTE] + ed['note_append']
    assert len(r) == 11, (t, idx + 1, len(r))
    assert '(' in r[M], 'method lacks signature: ' + r[M]
    assert r[LAND].count('.cs:') <= 1 or True
    # 一个落点列只允许一个「文件:行」段（括号里的第二处也要挑出来人工确认）
    seg = re.findall(r'[A-Za-z0-9_./]+\.cs:\d+', r[LAND])
    if len(seg) > 1:
        raise SystemExit('落点列仍有多段 %s:%d -> %r' % (t, idx + 1, seg))
    out.append(r)
    used.add((t, idx + 1))
    order.append((t, idx + 1, r[C], r[M][:40], r[ST]))

buf = '\n'.join('\t'.join(x) for x in out) + '\n'
p = BASE + '账/_parts/B93_E7.tsv'
io.open(p, 'w', encoding='utf-8', newline='\n').write(buf)

# 回环：读回逐列比对
back = [l.rstrip('\n').split('\t') for l in io.open(p, encoding='utf-8')]
assert back[0] == HDR, back[0]
assert len(back) - 1 == len(E), (len(back) - 1, len(E))
changed = set()
for row in back[1:]:
    assert len(row) == 11, len(row)
    assert '(' in row[M], row[M]
for t, ln, *_ in order:
    rows = load(t)
    src = rows[ln - 1]
    tgt = [r for r in back[1:] if r[C] == src[C] and r[M] == src[M] and r[F] == src[F]]
    assert len(tgt) == 1, (t, ln)
    for i in range(11):
        if src[i] != tgt[0][i]:
            changed.add(i)
print('WROTE', p, 'rows', len(back) - 1)
print('changed columns:', sorted(changed), '(5=状态 6=落点 7=差异 10=备注)')
