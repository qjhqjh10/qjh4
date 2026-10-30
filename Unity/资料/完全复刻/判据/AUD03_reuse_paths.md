# AUD03 · 复用 / 生命周期路径（摘要）

> 立项与判据正本 → `资料/完全复刻/判据/非方法体审查_任务清单.md` 的 **AUD03** 一节。
> **逐条表 = `判据/AUD03_reuse_paths.tsv`（119 行 × 10 列，裸 tab）**；本文件只是摘要，⛔ 别在别处再抄一份表。
> 审查日期 2026-10-30 · 静态判（**未跑 Unity / 未跑 typecheck / 未动 git**，真机验证留给调度台）。

## 覆盖边界（先看这节，⛔ 别把「表里有行」当成「整条链已查」）

- **覆盖面**：12 扇窗 + 窗口栈本体（`WindowsManager` / `GameWindow` / `ShellRuntime` / `MainMenuRuntime`）+ 战斗整场（`BattleDriver` + 12 个战斗面板）+ 活动/联盟/聊天。
- **没覆盖**：① 各窗的**视觉层**（AUD01/AUD02 管）② **可点/可拖/可悬停**（AUD04 管）③ 同一组件的**多套状态**（AUD05 管）④ **原版方法体本身对不对**（那是账的事）。
- **口径**：判档只看「**第二次走这条路时，上一趟的状态清没清**」；「原版本身就这样」不算缺陷（例：`TabButtons` 同实例重复 `Initialize` 幂等是真的）。
- **判据三句**：① 上一次登记的表清了吗 ② 订阅解了吗 ③ 对象销毁了吗。
- **本批不真机**：每条「建议断言」写的是**能咬住的断言**，不是已跑过的结果。

## 摘要

**合计 119 行：❌ 25 行 / ？ 13 行 / ✅ 81 行**（❌ 去重后约 20 个根因；其中「关窗不销毁」一族占 5 行、同一处代码）。按窗合计见下表——**下行数字与 TSV 逐行可对上**：

| 窗 | ❌ | ？ |
|---|---|---|
| 卡组编辑（DeckRuntime / DeckEditingWindow） | 6 | 2 |
| 收藏（CollectionWindow） | 5 | 0 |
| 商店（ShopWindow / ShopTabPage） | 1 | 0 |
| 锻造（ForgeTab） | 0 | 1 |
| 战役（CampaignTab） | 1 | 0 |
| 主菜单 / 外壳 / 窗口栈 | 3 | 4 |
| 战斗（BattleDriver + HUD + 面板） | **4 行 = 3 根因**（`_pendingPlayMode` 报两次） | 3 |
| 设置（SettingsWindow） | 0 | 0 |
| 日常 / 奖励（Rewards / Daily / Inbox / RewardWindow） | **4 行 = 3 根因**（`OnTabChanged` 两组各报一次） | 3（`_cachedTabs` 两组各报一次 + `RewardWindow.Close`） |
| 活动（LiveOps / Expansion / Draft / Gacha / Raid / Parent） | 1 | 0 |
| 联盟 | 0 | 0 |
| 聊天 | 0 | 0 |
| **合计** | **25** | **13** |

### 卡组编辑（DeckRuntime / DeckEditingWindow）—— ❌ 6 行 / ？ 2 行
1. **每次进编辑都 new 一扇窗 + 新 DeckRuntime**，关窗只 `SetActive(false)`；旧实例的 `Inventories.Wildcards.Updated` 只在 `OnDestroy` 退 ⇒ 每开一次留一具尸体 + 一条永久订阅（`Shell/DeckEditingWindow.cs:71-92,180-184`）。
2. **二次 Build 的开窗复位只回 tab0**（`Deck/DeckRuntime.cs:1397` 直接写 `_tab=0`、不走 `SetTab`）⇒ 四路滚动位（`:1014-1015`）与 `_filtersOpen`（`:7043`）跨窗保留。
3. **`_dragSlotView` 存的是「视图槽」不是「数据行」** ⇒ 拖拽中自动滚 / 拖拽中滚轮后，被保护的是别的行、被拖那行被裁没（`:5342` 置 · `:4411` 用 · `:9222`/`:7161` 改滚动 · `:5353` 不复位）—— 与 `CollectionDisplay` 那条**同根**。
4. **扫光层惰性 `AttachNine`（已记录缺口①）**：开窗直落 Cards 页且不切页时，库存更新也永不播（`:9625` 只 Hook · `:9043` 只在切页那拍 Attach · `:9138` 无层直接 return）。
5. **`_cosmCells` 只 `Add(null)` 不清**：每次 Build 追加 `CosmoRows*CosmoCols` 条（`:3430` vs `:3530`），读侧跳过所以不崩、只是无界增长（不在 `ClearBuildRegistrations` 里）。
6. **断言缺口**：`Editor/DeckScene.cs` 全文件没有「滚动位复位 / `_cosmCells` 不增长 / 拖拽+滚动后保护槽」三条。

？ ① Build 的「按下态」清理只清 `_cosmArmName`/`_cardArmDef`，漏 `_dragging`/`_dragQuad`/`_dragText`/`_dragRow`/`_dragSlotView`/`_dragPieces`（`:1290-1292`，同 idiom 在 `:6788`/`:6876` 是三件一起清）—— 可达路径（拖拽中途被全屏窗 Hide）没找到实证。② `Close()` 无条件 `SetNavPanelVisible(true,true)`（`:182`）而 `base.Close()` 在「不活」时早退 ⇒ 重复 Close 会把导航「放回」两次。

### 收藏（CollectionWindow）—— ❌ 5 行（6 个缺陷）
1. **Build 可重入但两张卡池表不清**：`_cellPool/_cellKeyOrder/_cellUsed` 与 `_stylePool/…` 只在 `RebuildCardsPage:1519`/`RebuildStylesPage:2482` 清，`Build:3797`/`BuildCardsPage:1002`/`BuildStylesPage:2337` 都不清 ⇒ 重建后池里全是 Unity 假 null、复用恒 miss，且 `_cellKeyOrder` 同键重复 `Add`（`:1636`）。
2. **`OnTabChanged` 每次重建 +1 个死 handler**（与设置/奖励同根，见「跨窗根因」）。
3. **`RebuildCardsPage` / `RebuildStylesPage` 拆整页却不 `PointerLayer.UnregisterOwnedBy`** ⇒ 旧 `MenuScroll`（Owner = 窗根）永不回收、仍能命中，旧 `OnChanged` 指向已销毁 holder（`:1514/:2476`；`Build` 那处已修 `:3806-3811`、这两处漏了）。
4. **`Close()` 只 `SetActive(false)`** + 不解自己的滚动登记 ⇒ 每次开/关泄漏整棵子树 + 2～5 条 Owner=旧窗根（`:6347`）。
5. **`CardsState`/`CosmoState`/`StylesState` 是 static**（`:886/:915/:2295`）⇒ 筛选状态跨窗口实例保留（原版关窗 Destroy ⇒ 新实例回「不限」），低危行为差异。
6. **四个主页滚动区 `Owner = 窗根`**（`:1148/:1864/:2370/:4210`）而指针层靠 `Owner.activeInHierarchy` 跳过切走的页 ⇒ 隐藏页仍吃滚轮；Cards 与 Deck 视口只差 0.7px ⇒ 最后登记的那个恒赢（静态可证，未真机；面板那份写法是对的、可照抄）。

### 商店（ShopWindow / ShopTabPage）—— ❌ 1 行
1. **`_openData` 从不清**（`Shell/ShopWindow.cs:322/332-341`）：`Close()` 只清基类 `Data`；而全屏窗会把商店 `Hide()`、随后 `ShowPreviousWindow → TryOpen()`（无参）→ `Open()` 整窗重建 ⇒ `Context` 仍返回第一次那份 army ⇒ 强制落 **Crates 页**并再跑一次 `TryOpenContainerArmy`（`ShopTabPage.cs:283`）。触发例：带 army 进商店 → 换页签 → 买一包（开包全屏把商店藏起来）→ 关包窗带回。

### 锻造（ForgeTab）—— ❌ 0 行 / ？ 1 行
？ `Build()`（`:383`）顶部没有像 `ShopTabPage:240` 那样的幂等守卫（今天靠「每实例只 `Setup` 一次」不叠两层），方法却是 `public` 无守卫 ⇒ 将来任何一处重入会静默叠双层；没有能红的观测面。
（裁切三件套已迁到视口节点 `ViewportClip`，`TrackPad=(10,0,0,0)` 由节点承载 ⇒ 之前担心的「污染下一页」**不成立**。）

### 战役（CampaignTab）—— ❌ 1 行
1. **`ClickNodeForTest` 每点一个节点就 `CampaignRewardWindow.Create` 一扇新窗**（`Shell/CampaignTab.cs:1254-1264`），而 `Close()` 只 `SetActive(false)`、全仓无 `Destroy` ⇒ 点 47 个节点留 47 扇尸体。

### 主菜单 / 外壳 / 窗口栈 —— ❌ 3 行 / ？ 4 行
1. **`OpenByRef` 走新建支时旧 GameObject 无任何销毁调用**（`Shell/WindowsManager.cs:928-939`；全类唯一销毁点 `DestroyWindowObject` 只被消息弹窗用 `:1455`）⇒ 每「关掉再开」一轮锚点下多一具 inactive 尸体。
2. **`GameWindow.Close()` 只 `SetActive(false)`、从不销毁**（`Shell/GameWindow.cs:714-806`），而同文件 `:456` 注释写着「关掉就是销毁」——自相矛盾（老账 `A123`，本条把「哪些窗受影响」列全）。
3. **`SetBaseMenu` 清 `openWindows` 却不 Hide/不关**（`:467-477`）⇒ 主菜单重建那一拍，桌上开着的窗变成「`activeSelf` 真、却不在册」的孤儿，ESC / `CloseAllWindows` / `ShowPreviousWindow` 全够不到；且全屏支 `if (openWindows.Count > 0) HideAllWindows()` 因表已清而不触发。

？ ① `MainMenuRuntime` 三个 static（`Window`/`_pendingBaseMenu`/`_pendingRoot`，`:663-668`）从不置空，`OnDestroy(:63)` 只清 `Instance` 与语言订阅。② 切场景不收窗（`PracticeModePopup.cs:3135/3150` 点 Battle! 直接 `LoadSceneWithFade`，只有正常收局 `BattleDriver.cs:32272-32275` 会 `CloseAllWindows`）。③ `MainMenuWindow.ToBackground` 只 guard `Background`（`Shell/MainMenuWindow.cs:77-82`），baseMenu 处于 `Closed` 时被弹窗支叫到会造出 `(state=Background + activeSelf=false)`。④ 锚点表 `_anchors`（static）靠 `WindowHolder.OnDisable` 撤、**没有 `OnDestroy` 兜底**。
（🔴 **就地订正（铁律 5）**：分组代理原写「`ClearAnchorsForTest` 全仓不存在」——**错**。它实际在 `Shell/WindowsManager.cs:256`，只是**全仓 0 个调用点**。表里已改。）

### 战斗（BattleDriver + HUD + 面板）—— ❌ 3 根因 / 4 行 / ？ 3 行
1. **`_pendingPlayMode` 通道在教程局 / 联机局提前 return 时不消费**（`Battle/BattleDriver.cs:4524` 教程 · `:4535` 联机 ⇒ 都在 `:4570 TakePendingPlayMode()` **之前**）⇒ 残留的 `Tutorial(4)` 被下一局普通对战当入口模式号（`:5507` 写 `Ctx.PlayMode` · `:5411` 筛 bot 预组 · `:38836` 喂 MissionChallenge 闸）。
2. **自检档销毁不完整**：`Editor/BattleScene.cs:45075 DestroyImmediate(driver)` 只销毁组件，编辑模式不派 `OnDestroy`（驱动无 `ExecuteAlways`）⇒ `DetachStaticHooks()` 从不自动跑、19 条静态钩子仍绑已销毁 driver。
3. **`CardDisplayWindow.SolidTexFor` 换宽高比时旧 `Texture2D` 被直接覆盖、从不销毁**（`Battle/CardDisplayWindow.cs:1785-1798`，小泄漏）。

？ ① `PopNumber.LastPopTween` 跨局不清（`:212`，只有自检读，读已销毁目标会抛）。② `TipPrebattle` 静态 `Texture2D` 永不销毁，而注释写着「别做成 static」（`Battle/TipPrebattle.cs:130-142`）——代码与注释自相矛盾。③ `ChoosePanel.Terms` 静态词表永不清（`Battle/ChoosePanel.cs:197`，生产只读、只自检注入）。
（✅ 重开一局、`BuildHud`、换牌/选牌/多卡展示/战斗日志/错误横幅单例/事件系统单例/根画布都判对上。）

### 设置（SettingsWindow）—— ✅ 全绿
十六张登记表在 `Build()` 首句全清（`:1655-1690`）、**不订 `Loc` 静态事件**、浮层（语言列表/三颗下拉/登录弹窗）显式收起、`Close()` 脏位先清零 ⇒ 重复 Close 无害。（换语言那条缺陷落在**别的窗**上，见下。）

### 日常 / 奖励（RewardsWindow / Daily 两弹窗 / Inbox / RewardWindow）—— ❌ 4 行（3 根因）/ ？ 3 行（2 个问题）
1. **`OnTabChanged` 每次整窗重建 +1 个死订阅者**（`TabButtons.cs:75-96` + `MainMenuSubmenuWindow.cs:693`）。
2. **整族窗口 GameObject 关后不销毁**（`A123` 同一根因，非新账）。
3. **换语言后整族窗内文案不刷新**：`SettingsWindow.ChooseLanguage` 逐个调 `openWindows` 的 `OnLanguageChanged()`（`:3634-3642`），但**全仓只有 `ShopWindow.cs:192` 覆写** ⇒ Rewards / Inbox / Daily 两弹窗 / RewardWindow 收到调用什么都不做，停在旧语言直到重开（页签文字同样是 Build 时取词条）。
？ ① `_cachedTabs` 二次 Open 后仍指第一棵树里已销毁的页（`_tabsInitialized` 从不重置；生产切页走 `FindTab` 活表所以没炸，只有 `ChangeTab<T>`/`GetTab` 会读到死对象，而它们今天无生产调用点）。② `RewardWindow.Close` 把 `_ctx.OnClose` 写在 `base.Close()` 的 `!activeSelf` 守卫**之前**（`:886-892`）⇒ 重复 Close 会二次回调；原版那条回调挂在哪一步本机没定位到（还差 `RewardWindow__Close.c` 与 `RewardService.Collect` 的回调点）。

### 活动（LiveOps / ExpansionPass / Draft / Gacha / Raid / Parent）—— ❌ 1 行
1. **四扇全屏活动窗首开建树两遍、已开窗再入被强制重建**：`Gacha:353` / `Raid:412` / `ExpansionPass:504` / `Parent:566` 的 `Create` 内部都自调 `mgr.OpenWindow(win)`，而它们**唯一调用方是 `OpenByRef`**（`:936 create` → `:940 再 OpenWindow`）⇒ 全屏支 `HideAllWindows()`（`:316`）把刚开的本窗打回 `Closed` ⇒ `Open()`/`Build()` 跑两遍；同一扇已开时再点入口也被打回 `Closed` 整窗重建（原版 `state==Open` 直接 return）。无内存泄漏（Build 幂等清子件），差在重复建树 + 重复副作用 + 再入行为。
（`LiveOpsEventWindow` 是模板：`Build:598` 首句 `UnregisterOwnedBy` + 逐子件 `DestroySafe`，`Close:580` 撤 `_search`/`_expiryRefresh`；Ranked 继承即得。`DraftMode` 的 `_run` 是**存档型静态**，不是泄漏。）

### 联盟（AlliancePanelWindow / AllianceMemberTab / AlliancesTab / SearchTab / OptionsPopup）—— ✅ 全绿
切页先 `DestroySafe` 再建、滚动区只在 `Build` 登记一次、`Initialize`/`OnEnable` 幂等、整屏底 `Push/Pop` 成对且 `PurgeDestroyed` 兜死件。

### 聊天（ChatPanel / ChatTab / ChatCache）—— ✅ 全绿
`Build` 首句 `ClearChildren` + `tabs.Clear()`、`ChatTab.Setup` 先 `UnregisterOwnedBy` 再销毁行、`ChatCache` 单次读闸 + `ResetForTest`。

## 跨窗根因（去重，改一处能关掉多行）

| # | 根因 | 关掉哪几行 | 最小改法 |
|---|---|---|---|
| 1 | **关窗不销毁 GameObject**（`Close()` 只 `SetActive(false)`；原版 `CloseWindowCO` 是 `Object.Destroy`） | 卡组编辑① · 收藏④ · 战役① · 主菜单② · 日常② | 在 `GameWindow.Close()` 的 `Hide()` 之后（`Shell/GameWindow.cs:804`）按原版补 `DestroyWindowObject(gameObject)`；baseMenu 那一支（`:802`）另判 |
| 2 | **`OpenByRef` 走新建支不销毁旧实例** | 主菜单①（+ 上面 1 的下游表现） | `Shell/WindowsManager.cs:931` 那句 `Remove` 之前补 `DestroyWindowObject(cached.gameObject)`（与消息弹窗 `:1449-1457` 同形） |
| 3 | **`OnTabChanged` 每次重建 +1 死 handler** | 收藏② · 日常① | `TabButtons.Initialize`（`Shell/TabButtons.cs:75`）改成无条件先 `-= UpdateTab`；或 Build 前把 `Target` 已销毁的项清掉。断言：`OnTabChanged.GetInvocationList().Length == 1` |
| 4 | **`OnLanguageChanged` 全族零覆写** | 日常③ | 各窗照 `ShopWindow.cs:192` 形状覆写，调自己的重建/重设文本口 |
| 5 | **`_dragSlotView` 是视图槽不是数据行** | 卡组编辑③（+ 已知的 CollectionDisplay 缺口） | `RefreshDeckList` 内按 `_dragRow - firstRow` 现算被拖槽，或滚动后重算 |
| 6 | **`SetBaseMenu` 清表不关窗** | 主菜单③ | Clear 之前对每扇非 menu 窗调 `Close()` |
| 7 | **滚动区 `Owner` 给的是窗根**（应为该页节点） | 收藏⑥（+ 收藏③④ 的登记不撤） | 四个 Owner 改 `page.gameObject`（面板那份已经这么做）；`RebuildXPage`/`Close` 各补 `PointerLayer.UnregisterOwnedBy(gameObject)` |

## 已知缺口（按简报直接收表，⛔ 别再重查）

| 缺口 | 表里哪一行 | 判档 |
|---|---|---|
| `WildcardDisplay` 扫光层惰性 `AttachNine`（`DeckRuntime.cs` 的 `WildcardShineAfterTab`，约 `:9033-9075`） | 卡组编辑④ | ❌ 真缺口（开窗直落 Cards 页不播） |
| `CollectionDisplay` 复用 `_dragSlotView`（拖拽期间自动滚换绑另一副卡组） | 卡组编辑③（同根，在 `Deck/DeckRuntime.cs:4411/:9159`） | ❌ 真缺口 |
| `CardDetailPopup.Close()` 经虚表再调 `Hide()` ⇒ `ResetFanState` 跑两次 | 收藏 · `CardDetailPopup` 那一行（`Shell/CardDetailPopup.cs:948-966`） | **✅等价**（幂等、只多一条日志） |
| `DeckRuntime.Build()` 的 12 张登记表（已修） | 卡组编辑 ·「Build 登记表」与「Build 清子件」两行 | **✅对上**，并当**判据样板**（`ClearBuildRegistrations`，`Deck/DeckRuntime.cs:1284 → :9689`） |

## 怎么验（真机留给调度台）

- **通用两条断言**（每扇窗都能套）：
  1. 「**连开两次，第二次的读数与第一次相同**」——先取第一次可数读数（卡格数 / 行数 / 页签数 / 订阅数），关窗、再开、再取一次，两个数相等且**不随次数增长**。
  2. 「**关窗后订阅数归零 / 对象为空**」——`Close()` 之后：`gameObject == null`（根因 1 修好之后）、`Signal` 订阅数 == 0、`PointerLayer.ScrollCountForTest` 与开窗前相等。
- **各窗额外一条**：卡组编辑 = 第 1 次滚到 `firstRow==1` 且打开筛选抽屉 ⇒ 第二次 Build 后 `DeckScrollPx==0` 且 `FiltersOpen==false`；收藏 = `win.Build()` 两次后 `_cellKeyOrder` 无重复键；商店 = `Open→Hide→Open` 后不因旧 army 落 Crates 页；战斗 = 教程局之后普通局的 `Ctx.PlayMode != Tutorial`；活动 = `OpenEventHub` 后本窗 `Build` 只跑一次。
- **需要现加读口的地方**（表里已标）：`UiCosmCellCount` · `UiDraggingForTest` · `DeckScrollPx` / `FiltersOpen` · `StaticHookCount` · `HitScrollForTest`。没有读口的断言写不出来，先加读口再断言。

## 复跑办法

- 原始分组表在 `D:/4/_tmp_view/aud03/G1_deck.tsv … G7_events.tsv`（7 份，10 列），合并脚本 `D:/4/_tmp_view/aud03/merge.py`（**只读源、只写本 tsv**）。
- 重跑：`PYTHONIOENCODING=utf-8 python -I -X utf8 D:/4/_tmp_view/aud03/merge.py`。
- 脚本里带两条**铁律 5 就地订正**（`ClearAnchorsForTest` 存在性 · `TabButtons` 幂等只对同实例成立），改源表时别把它们丢掉。
