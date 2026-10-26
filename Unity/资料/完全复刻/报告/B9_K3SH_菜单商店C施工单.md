# B9 · 菜单 / 商店 / 锻造 / 战役 C 行 · 施工单与派活包（判据代理 K3SH）

> 第八十四会话 · 2026-10-26 · **只读核**：未改 `.cs` / 未改资产 / 未跑 Unity / 未动 git / 未改主账表与正本。
> 产出：`判据/B9_菜单商店C_施工单.tsv`（12 列 × **122** 行）· `判据/B9_菜单商店C_派活包.md` · 本报告。
> 判据顺序：`d:/2/tools/decomp_full/*.c`（第一）→ `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs`（第二）→ `d:/2/新解包资源/assets_full/`（字段真值）→ `d:/2/tools/assetripper_out/ExportedProject`（prefab 归属）→ 我们仓库（落点现状）。

---

## 一、结论（五条）

1. **规模对上了**：四份账现读 C 行 = **122 条**（`菜单_外壳` 50 · `商店` 54 · `锻造` 2 · `战役` 16），与任务简报的「约 122」一致。现等级 = **P0 ×1 / P1 ×19 / P2 ×102**；我建议 **P0 ×1 / P1 ×14 / P2 ×107**（3 条降档、2 条改档，理由逐条写在施工单第 12 列）。

2. 🔴 **本批 122 条里有 68 条（56%）整批挂在「活动线暂停」上，今天不该派活**。
   - 组成：**商店 39 条**（Gacha / ExpansionPass / Campaign 付费点数 / LiveOps 容器事件 / 主菜单报价格）+ **菜单 18 条**（`Liveop*` 17 + Gacha Tab 的 `AutoLayoutSpacing` 1）+ **战役 11 条**（`RankedDeckSelector` 窗内轮播）。
   - 三个理由（每个都能指到行）：① 用户 **2026-10-25** 暂停活动线；② 这批活动**已过期**且用户拍板「过期的活动不做」；③ 判据只到反编译 + 过期 SO 快照，**没有可跑的运行时数据**（做了只能靠夹具）。
   - ⇒ **可派 = 54 条**，切成 **8 个可派包**（A–G + K，其中 C 含特效 I）+ **1 个跨域包**（J 战斗线）+ **1 个整批不派**（H）；另 **F 是跨域包**（卡组编辑 / 收藏线，含 13 条）。

3. **这批 C 行不是「补功能」，主要是三类**：
   - **口径待定类（4 条，P1）**：`AdaptFlexibleGridLayoutByAspectRatio` —— 原版公式我已核到与 `.c` 逐字一致，且**我们仓库 2026-10-19 就把它与实拍对上了**，但**我们固定 16:9**。所以这 4 条的真正阻塞是「要不要支持非 16:9」，不是技术。
   - **机制不同类（2+10 条）**：`LayoutGroupContentFixer`（我们**没有 uGUI 布局系统**，「建类」= 死代码，真正要的是**时机覆盖审计**，733 份实例）· `DragAndDropItem`（我们拖拽是另一套，真正缺的只有「边缘自动滚」与「近垂直起拖让路」两条交互）。
   - **真缺口类（其余）**：错误弹窗的 `CustomError→词条` 入口、`ScrollViewFocusFunctions.FocusOnItemCoroutine`（锻造奖励选取不滚动）、逐格阵营徽记、锻造页头四件、战役点按阵营画等 —— 都是**小而清**的活。

4. **共用件是这批的唯一硬约束**：`Shell/MainMenuRuntime.cs`（27 处引用）· `Shell/BadgeNotifications.cs` · `Shell/WindowsManager.cs` · `Shell/Inventory.cs` · `Shell/ItemDrawer.cs` · `Shell/MenuScroll.cs` · `Shell/CollectionTabPage.cs` · **`Deck/DeckRuntime.cs`（8838 行，跨域热点）**。派活包 §2 给了逐件的排程。

5. **P3 候选 7 项**（PlayFab 错误族 / CloudScript / 登录账户 / 匹配排行 / 远端商店目录与 liveops 数据 / 推荐码 / 遥测）已单列在派活包 §5，**只给证据 + 时间限定，⛔ 没有判 D**。⚠️ 商店的「真实经济」**不在 P3**（口径 = 钱包固定 2000、卡池全解锁、照常能买）。

---

## 二、逐条证据（按包；每条都给到能复读的位置）

### 2·A 主菜单几何与背景（9 条）

- **`AdaptFlexibleGridLayoutByAspectRatio`（菜单 2/3/4/5）**
  - 出处：`decomp_full/AdaptFlexibleGridLayoutByAspectRatio__{ModifyFlexibleLayout,OnEnable,ResolutionHasChanged,OnDisable}.c` + `cpp2il_dev/AdaptFlexibleGridLayoutByAspectRatio.cs:20-92`。
  - 实例**只有 1 份**：`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1962.json`（`flexibleGridLayout` → pid 1961）。⚠️ 该组件的 `originalItemSize` 是**私有无 `[SerializeField]`** ⇒ 全解包 `originalItemSize` **0 命中**（这是正常的，不是「资源缺失」）。
  - 公式（`.c` 逐句）：`aspect = (float)w/(float)h` → `k = max(1f, 1.7777778f/aspect)` → `itemSize = (orig.x, orig.y*k)` → 与旧值平方差 > `1e-4` 才 `MarkLayoutForRebuild` → 无条件写回。
  - ⚠️ **ILSpy 把 `aspect` 读成整数除**（`int num4 = width/height`），`.c` 是浮点除 ⇒ **按 `.c`**。
  - 我们侧：`Shell/MainMenuRuntime.cs:2384-2398`（A1635，2026-10-19）已把同一公式与用户实拍（1430×883，aspect≈1.62）逐项对上（预测卡顶 148.5 / 实测 149；卡底 844.6 / 实测 845），并**明确写「我们固定 1920×1080（16:9）」**。
- **`AutoLayoutSpacingByItemSize`（菜单 29）** ★实证：真身是 **`Gacha Tab`**
  - 实例 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_5343527243449095809.json`（GO「Content」），用 `m_Script` guid 在 AssetRipper 导出工程里定位到 **`Assets/Gacha Tab.prefab`**（同 prefab 里 `spacingAt16_9: -40` / `spacingAt4_3: 84.76` 逐字相同）。
  - 序列化真值（首次取到）：`spacingAt16_9 = -40.0` · `spacingAt4_3 = 84.76000213623047` · 曲线 2 键 `(1.33, 0, slope 1.9839270, outW 0.2134457) → (1.7769999504089355, 1.0, slope 2.1977012, inW 0.1768976, outW 0.33333334)` · `Pre/PostInf=2` · `RotOrder=4`。
  - 公式：`t = (float)w/(float)h`（**曲线自变量就是宽高比**）→ `k = clamp01(curve.Evaluate(t))` → `spacing = 84.76 + (−40 − 84.76)·k` ⇒ 16:9 ≈ **−40**、4:3 ≈ **83.9**。
- **`MenuSprites`（菜单 471/472）**：`cpp2il_dev/MenuSprites.cs:37-50` + `decomp_full/MenuSprites__GetWorldSprite.c`（越界分支 = `LogError("Player tier bigger than worlds array")` 后 `return worlds[last]`）。
- **`BoostMenu`（菜单 43/44/45）**：`cpp2il_dev/BoostMenu.cs:42/247/301`。`ShowAllBoosters` = 取加成列表 → 按 `BoostItem.Type` 分组 → 每组一份 `BoosterPanelPrefab`；`UpdateAllPanelsTimers` 第一句是 `if (!AllBoostersPanel.activeInHierarchy) return`。

### 2·B 外壳共用件（3 条）
- **`ScrollViewFocusFunctions.FocusOnItemCoroutine`（菜单 612）**：`decomp_full/ScrollViewFocusFunctions._LerpToScrollPositionCoroutine_d__4__MoveNext.c` 逐句 ——
  `t += Time.unscaledDeltaTime × speed`；`t ≥ 1` ⇒ `onFinish?.Invoke()` + `set_normalizedPosition(**target**)`（**精确贴**，不是 Lerp(…,1)）；否则 `e = 1 − (1−t)²`（**二次缓出**）+ `Lerp(start, target, e)`。`speed = 1.5`（锻造奖励选取 `:416/:438`）⇒ 时长 ≈ **0.667 s**。
  ⚠️ `CalculateFocusedScrollPosition` 的 **Vector2 重载体未产出**（`decomp_full` 只给了 `(ScrollRect, RectTransform)` 那一支，且它自己递归到一个 3 参版本）⇒ 我们的 `MenuScroll` 应按自己的 offset 模型换算，不照搬。
- **`TransitionUIAnchor`（菜单 720/721）**：`cpp2il_dev/TransitionUIAnchor.cs:8-13`（`EnableAnchor` = `WindowsManager.anchorOverride = transform` 一行）+ `DisableAnchor` 首句 `UnregisterAnchorOverride()` 与「遍历子件，命中 `GameWindow` ⇒ `SetParent(GetWindowAnchor(placement))`」那条链。
  我们侧：`Shell/WindowsManager.cs:210-247` 已有 `RegisterAnchorOverride` / `UnregisterAnchorOverride` / **覆盖读口**（2026-10-25 N5），**缺**组件与回挂循环。

### 2·C 徽记 + 战役内容 + 锻造页头（9 条）
- **战役 4（`ArmySelector.Initialize`）**：`cpp2il_dev/ArmyItemContainer.cs:202-366` —— `parentType == Neutral ⇒ return`；Campaign ⇒ `new CampaignBadge(army)`；Forge ⇒ `new ForgeBadge(army)`；两者都是**单元素数组** ⇒ `notificationBadge.AddNotificationType(array)`。枚举 `ArmySelectorType{Neutral=0, Forge=1, Campaign=2}`。
  ⚠️ **ILSpy 把 switch 读坏**（Forge 支落进不可达 `else`）—— 真形状按两支各单元素数组处理（依据：`Campaign` 支的数组长度 = `parentType-1` = 1）。
  我们侧：`Shell/BadgeNotifications.cs:110-471` 已有 `UiBadgeNotificationManager` / `BadgeNotificationDefinition{useFilter,filter}` / `CampaignNotificationProvider` / `ForgeNotificationProvider` / `BadgeWiring.Attach(quad, rect, textParent, qText, tint, defs, log)`。**真缺口 = `CampaignBadge` 没有 `army` 字段、provider 按「当前所选阵营」而非按格**。
- **战役 19（`CampaignArmyUIDecorator.OnObjectUpdated`，P0）**：`cpp2il_dev/CampaignArmyUIDecorator.cs:61-75` = `if (currentAmount != 新值) { currentAmount = 新值; shinyEffect.Play(resetTime:true); CheckFillColor(); }`。shader/贴图参数（prefab 实读，账里已有）：`UIEffectTweener` duration **1.0 s** · `TransitionFilter 4(Shiny)` · rotation **120** · width **0.25** · softness **0.5** · Blend 1/Src 1/Dst 10 · `m_TransitionTex` pid `-6023190078349834598`。`completedColor` 实读 **(0.02002, 0.88679, 0.01255, 1)**。
- **战役 53（`CampaignPointDrawer.DrawForArmy`）**：`cpp2il_dev/CampaignPointDrawer.cs:26-46` = 写 `CardArmyToString(army)` + `image.sprite = GetArmyIcon(army)` + `image.enabled = (sprite != null)`。我们侧 `Shell/ItemDrawer.cs:414` **已注册** `CampaignPoints / CampaignPointDrawer` ⇒ 不是没接，是缺「按阵营画」这一支。
- **战役 55（`CampaignPoints.GetIcon`）**：`cpp2il_dev/CampaignPoints.cs:46-70` = `Small ⇒ smallIcon.Load()` / `Large ⇒ bigIcon.Load()` / 其它 ⇒ null。
- **战役 12（`Campaign.AddPoints`）**：`cpp2il_dev/Everguild.LiveOps/Campaign.cs:1652-1660` = `<points> += amount` + `Refresh<CampaignBadge>()`。⚠️ **`decomp_full` 里 `Campaign` 0 个 `.c`**。
- **锻造 73（`ForgePlayerContainer.OnInitialize`）**：`cpp2il_dev/ForgePlayerContainer.cs:9-71` —— 四件（标题/军队图标/军队名/等级）由**同一个 flag** 控显隐；词条 `PlayerProfile/HighestForgeLevel`（"Highest Forge Level"/"最高锻造厂等级"）与 `PlayerProfile/LevelDisplay`（"Level: {0}"/"等级：{0}"）**在官方表里**。
- **商店 239/240/241（`ShopContainerInventory`）**：`cpp2il_dev/ShopContainerInventory.cs` —— 类声明 = `Inventory<ShopContainerBase>, INotificationProvider<BoosterPackBadge>`；`CheckNotification` = 过滤 `GetID().Contains("Booster", InvariantCultureIgnoreCase)` 后 `Repeat(new BoosterPackBadge(), Σquantity)`。

### 2·D 背包 / 货币层（5 条）
- **商店 54（`CurrencyCondition.Validate`）**：`cpp2il_dev/CurrencyCondition.cs:11-22` = `InventoryManager.Instance.HasEnough(targetCurrency, minimumAmount)`，**0 处 PlayFab/云脚本**（旧理由「服务端条件」与方法体不符）。
- **商店 59–62（`CurrencyExtensions`）**：`AddCurrency` = 取条目 → `+= amount` → 走 `Add` 触发 `OnItemCountModify`；`CanGetMore` = `GetOwnedCount < maxAmount`（`maxAmount<0` 视为无上限）；`ToCurrency` = `GameCurrency` 枚举 → `Currency` SO 查表。
- 我们侧：`Shell/Inventory.cs` **这一层已经在**（`Inventory<T>` / `ItemInfo<T>` / `Inventories` / `OnItemCountModified` 钩子，2026-10-26 Q1）；`Wallet` 在 `Shell/DailyData.cs:2292-2313`（`StartAmount = 2000`）。**缺 `Inventory<Currency>`**。

### 2·E 商店货架 / 报价数据（7 条）
- **商店 94（`FixedStoreData.AddItem`）**：`cpp2il_dev/Everguild.LiveOps/FixedStoreData.cs:150-166` = **`items.Add(itemToAdd)` 一句**；`get_Items`（同文件 `:100-149`）= `items` ∪ `offerLists.Select(x=>x.Reference).SelectMany(x=>x.offers)`。
- **商店 163（`OfferData.ToRewards`）**：`cpp2il_dev/Everguild.LiveOps/OfferData.cs:632-640` + 状态机 `:213-470` —— Containers 按 `TargetId` GroupBy ⇒ 每组一条 `RewardInfo(ref, group.Count(), RewardTier.Basic, null, null)`；IndividualItems / DropTables 同形；Currencies 逐个 `CurrencyValue` 在已加载 `Currency` 里按 `type == Currency` 匹配。
- **商店 42–45（`CarouselOfferPopupDrawer`）**：`DoReset` = 释放资源 + 销毁全部 drawer；`DrawItemReward` = `ItemDrawer.Draw(contentAnchor, item, qty)` + `ToggleShowInfoOnClick(false)`；`DrawRewards` 里那条 `Where` 谓词**判据不可及**（`ObtainableItem` 虚调用没解出）。
- **商店 93（`FixedDateDailyStoreItem.OnAfterWrite`）**：把 `item.Reference` 的 rarity/itemType 写进 JToken，**本地序列化回调、0 处后端**。

### 2·F 卡组格 / 收藏格（13 条，跨域）
- **`DragAndDropItem.CheckScrollDrag`（菜单 101）**：阈值 **50f** / 速度 **10f** 两个**默认字段值**（`cpp2il_dev/DragAndDropItem.cs` 字段声明）＋ `:361-442` 逻辑：`inner = rect.height*0.5 − threshold`；`local.y ≥ inner ⇒ verticalNormalizedPosition += deltaTime*speed`；`local.y ≤ −inner ⇒ −=`；末尾 `clamp01`（**只垂直、双侧**）。
- **`OnBeginDrag`（菜单 106）**：先算 `|atan2(Δy,Δx)·Rad2Deg|`，落在 **30°–150°**（近垂直）⇒ `scrollRect.OnBeginDrag(e); beingDragged = false; return`（把拖拽让给滚动）；否则 `startParent = transform.parent`、建占位并 `SetSiblingIndex(原 sibling)`、挂到 `transform.root`。
- **`OnEndDrag`（菜单 109）**：在 `startParent` 的全部子件里找**距自己最近**的一个 ⇒ `SetParent(startParent)` + `SetSiblingIndex(最近下标 + 1)`。
- **`CreateEmptyObject`（菜单 102）**：`new GameObject("Scroll Empty Space")` + RectTransform + CanvasRenderer，**`static` 单件、只建一次**，`OnDestroy` 里销毁。
- **新卡角标（菜单 54/60）**：`cpp2il_dev/BasicCardUI.cs:713-731` —— `SetActive(option)` 后 `if (withAnimation && option) DOScale(badge.transform, **1.1f**, **0.35f**).SetEase(**OutBounce**)`；`SetRawCardData`（`:290-292`）先 `SetActive(false)`。
- **banned（菜单 58）**：`cpp2il_dev/CardDrawer.cs:72-86` —— `IsCardBanned(card)` ⇒ `ToggleGreyScale(**!banned**)` + `bannedIcon.SetActive(banned)`。

### 2·G 错误弹窗 / 提示 / 对决闸（5 条）
- **菜单 157（`ErrorPopupWindowController.ShowErrorMessage(CustomError)`）**：`cpp2il_dev/ErrorPopupWindowController.cs:31-77` + `decomp_full/ErrorPopupWindowController__ShowErrorMessage.c`（`.c` 的 `ShowPopUp(manager, body, 1, 0, submitText, action, 0)` ⇒ **ILSpy 把第 4/5 实参读成 default**，按 `.c` 取）。步骤 = `customError.ToString()` → `"CustomErrors/"+name` → `Loc.GetTranslation` → 空 trace 走 `CustomErrors/ErrorCloudscript` + `"\n<size=3>("+trace+")"` → 提交钮 `MenuLogin/Intro/Retry` → `ShowPopUp(body, localizeTexts:false, closeOnEsc:false, submit, actionAfterError)`。
  - 枚举真值：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CustomError.cs` = **28 个具名值**（`Undefined=0` … `PlayerNotFound=203`）。
  - 词条真值：`d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms` 里 `CustomErrors/*` = **41 条**（已逐条读过）；⚠️ **`CustomErrors/Undefined` 不在表里**。
- **菜单 134（`DuelPopupWindow`）**：`decomp_full/DuelPopupWindow__CanPlay.c` 逐句 = `playEvent.GetEventDeck()`（接口槽 9）非空 ⇒ true；否则 `UIMessageController.ShowError("MainMenu/General/DontHaveDeckForGameMode", localize:1)` ⇒ false。`ConfigureButtonEvent` = `SetActive(playEvent != null && IsActive())` + `SoftDisable(GetEventDeck() == null)`。词条在官方表："You don't have a deck for this game mode."/"你没有这个游戏模式的卡组。"
- **菜单 236（`EverguildTooltipCollectorLevel`）**：`cpp2il_dev/EverguildTooltipCollectorLevel.cs:16-70` —— `p = clamp01((CurrentXp − xp0)/(xp1 − xp0))`，`xp1 == xp0 ⇒ 0`（防除零）。
- **菜单 245（`EverguildTooltipTrigger.OnPointerClick`）**：`if (!preventPassingClickEventToParent) return; EventSystem.SetSelectedGameObject(null)`（**不收起 tooltip**）；241 颗实例里 `(1,1)` 只有 **1 颗**。
- **菜单 563（`ReferralPopupWindow.OnSetReferrer`）**：`cpp2il_dev/ReferralPopupWindow.cs:159-186` = `interactable=false` → `ReferralManager.SetReferrer(text, onComplete: Refresh, onError: err => {errorMessage.text = Loc.T("CustomErrors/"+err); interactable=true;})`。

### 2·J 锻造 65（`ForgeHandler.CollectPoints`）
`cpp2il_dev/Everguild.LiveOps/ForgeHandler.cs:324-365` = `FirstOrDefault(forges, f => f.<campaign>.<army> == point.cardArmy)` → 点数 += amount → `if (GetRewardsToCollect().Any()) Refresh<ForgeBadge>()`。

### 2·K 布局时机（403/404）
`decomp_full/LayoutGroupContentFixer__FixLayout.c` = 逐 `layouts[]` 跳过 null 后 `LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>())`；`OnEnable` = `if (fixOnEnable) FixLayout()`。
**实例总数现读 733**（按 `fixOnEnable` 字段全解包扫）：`bundle_menus_assets_all` **708** + `mainmenuwarpforge` 场景 **12** + 13 个战场各 **1** —— **与账里的 733 逐数吻合**（账里没写构成，我补上了）。

---

## 三、没查清 / 判据不完整（⛔ 别当成已核）

| # | 条目 | 缺什么 | 下一步 |
|---|---|---|---|
| 1 | 战役 19 `CampaignArmyUIDecorator.Start()` / `CheckFillColor()` | ILSpy 把中间量读成 0（`currentAmount` 的赋值来源、满档判据的**两个嵌套条件**都读不出来） | 走 **VA 级反汇编**（该文件在 `decomp_full` 有 `.c`，但 `Start`/`CheckFillColor` 的常量仍是 `DAT_`） |
| 2 | 商店 163 `OfferData.ToRewards` 四支的**先后顺序** | ILSpy 按状态机重排；`decomp_full` **无 OfferData**（该类在 `Everguild.LiveOps`，全量导出没覆盖） | 反汇编 / 或按「①Containers ②IndividualItems ③DropTables ④Currencies」先实现并在注释标「待复核」 |
| 3 | 菜单 474/477 `NavigationPanelController.BuildEventButtons` 的过滤谓词 | `_003CBuildEventButtons_003Eb__13_0` 里全是 0/0 链（ILSpy 没解析出任何字段访问） | VA 级反汇编取谓词体 |
| 4 | 菜单 43/44 `BoostMenu.ShowAllBoosters/HideAllBoosters` 完整体 | 只读到「按 Type 分组 + 每组一份面板」+「销毁清表」的结构，逐字段（面板内怎么填）未读 | 需要时再读（活动线恢复前不急） |
| 5 | 菜单 236 那个 `string.Format` 实参 | 是「距下一级还差多少」还是「总 XP」——ILSpy 读成 default | 读 `.c` 或按卡面/实拍定 |
| 6 | `CardTierConfig.GetAccumulatedXPForLevel` 的累计 XP 表 | `Shell/CardTierConfig.cs` 存在，但**表内容是否本地化为原版值**未核 | 核表；若是我方编的 ⇒ **必须打 `🔶 自己设计`** |
| 7 | `MenuSprites` 的 `worlds[]/friendlyWorld` 具体指向哪几张 sprite、本地有没有图 | 未解 `AssetReference` | 解 SO + 查图 |
| 8 | `CampaignPoints.smallIcon/bigIcon` 两张图本地有没有 | 同上 | 解 `AssetReferenceTyped<Sprite>` |
| 9 | 我方卡面有没有 `New Card Badge` 层 | `CardView` 层表里未列，未逐层核 | 读 `Core/CardView.cs` 层清单 |
| 10 | 菜单 721 `TransitionUIAnchor.DisableAnchor` 对**非 GameWindow 子件**的处理 | ILSpy 半可读；我只取了「置 null + GameWindow 回挂」这一层 | 需要时 VA 复核 |
| 11 | 商店 39 条活动线的**过期 SO 快照**本地是否齐 | 未解 LiveOps 活动 SO（`PlayEventData`/`DropTableContainer` 等） | 活动线恢复时整批核 |
| 12 | 菜单 157 的 `Undefined` 兜底口径 | 41 条词条里没有 `CustomErrors/Undefined`；原版 `GetTranslation` 取不到时返回什么（空串还是键名）未验 | 读 I2 的 `GetTranslation` 默认行为，或按实机 |
| 13 | 我方卡组格拖拽 vs `DragAndDropItem` 的**逐条对账** | 只确认了「缺边缘滚 / 缺近垂直让路」两条；其余 8 条是否等价**未逐条核** | 包 F 的第一件事就是出对账表 |

---

## 四、改动清单（只有这 3 个文件；都是**新建的判据产出**，未改任何既有文件）

| 文件 | 状态 | 内容 |
|---|---|---|
| `d:/4/Unity/资料/完全复刻/判据/B9_菜单商店C_施工单.tsv` | **新建** | 12 列 × 122 行（表头**逐字**照 `B9_剩余C_施工单.tsv`）；LF；每行 12 列（脚本校验过：0 个制表/换行混入字段） |
| `d:/4/Unity/资料/完全复刻/判据/B9_菜单商店C_派活包.md` | **新建** | §0 用法 · §1 包总表（10 可派 + 1 不派 + 2 跨域）· §2 共享瓶颈表（12 个共用件 + 排程）· §3 各包明细 · §4 跨域移交 · §5 P3 候选 7 项 · §6 三条提醒 |
| `d:/4/Unity/资料/完全复刻/报告/B9_K3SH_菜单商店C施工单.md` | **新建** | 本报告（五节） |
| `d:/4/_tmp_view/k3sh/gen_sheet.py` · `Crows.txt` | 临时 | 生成脚本与 C 行抽取（可复跑；`python -I -X utf8 d:/4/_tmp_view/k3sh/gen_sheet.py`，它会**校验 122 条全覆盖 + 无多余行**，不一致直接非 0 退出） |

未改动：`账/*.tsv`（主表）· `CLAUDE.md` · `项目任务.md` · 任何 `.cs` / 资产 / 场景。

---

## 五、🔴 顺手发现（只报不改；前 3 条建议调度台优先落账）

1. **`Inventory<T>` 层已经存在 —— 商店 59–62 / 239–241 的账写「整层没有」已过期。**
   `Shell/Inventory.cs` 现读有 `Inventory<T>` / `ItemInfo<T>` / `Inventories`（= `InventoryManager.GetInventory<T>()`）/ `OnItemCountModified` 钩子（2026-10-26 批次 3 Q1 补的 `protected virtual` 扩展点）。**真缺口 = `Inventory<Currency>` 一份 + `BoosterPackBadge` 的 provider**，不是「从零建背包」。估工差一个量级。

2. **`AutoLayoutSpacingByItemSize` 的真身是 `Gacha Tab` 的「Content」** —— 账里只写「menus 包 1 处挂接」，E13 据此把 29 号补成 P1。实证（`m_Script` guid → `Assets/Gacha Tab.prefab`）⇒ **这条属活动线**，建议改 **P2 并挂到活动线**，与 Gacha 那 39 条同批。

3. **`AdaptFlexibleGridLayoutByAspectRatio` 的公式与分析我们早就有了**（`Shell/MainMenuRuntime.cs:2384-2398`，A1635 2026-10-19，含用户实拍复核）。E13 给这 4 条 P1 时漏了「我们固定 16:9」这一句 ⇒ **建议在账里补一句指针**，否则下一个会话会当成「完全没做」。

4. **`fixOnEnable` 的 733 是三项之和**（menus 708 + mainmenu 场景 12 + 13 战场各 1）。账里只有总数，没有构成；现在是**可逐源复核**的。

5. **`decomp_full` 里 4 个 `Everguild.LiveOps` 类 0 个 `.c`**：`Campaign` · `FixedStoreData` · `OfferData` · `ForgeHandler`。⇒ 这些类的「ILSpy 把实参/中间量读成 default」**没有第二源可校**，凡引用其细节的结论都要标风险（本批里 12/65/94/163 四条受影响）。

6. **`ContainerOfferEvent.GetBaseData/GetBaseSave` 只是类型收窄**（`base.X()` 后 `as` + 空判，两个 cast）。账里把它俩写成「LiveOps 存档层」整件 ⇒ 会高估工作量。

7. **`FixedStoreData.AddItem` = `items.Add` 一句**；而我们的 `Shell/ShopData.cs` 用 **`Dictionary<string, ShopItem> _shopItems`** ⇒ **丢顺序**。原版 `get_Items` 是「items 在前 + offerLists 展开在后」的**有序**合并 ⇒ 这是一个**潜在的真差异**（画面排列）。建议落账（不是我判的对错，但顺序肉眼可验）。

8. **`Shell/ItemDrawer.cs:414` 已注册 `CampaignPoints / CampaignPointDrawer`** ⇒ 战役 53 不是「整类没接」，只缺「按阵营画」一支。（同族的 55 缺 Small/Large 两档图。）

9. **`CustomErrors/*` 41 条官方词条已在本地**（清单：CantLoadMainMenuObjects / CantReferSelf / CloudscriptCallError / DataUpdateAvailable / DisabledFeature / DuplicateConnection / ErrorCancelPracticeMatch / ErrorCloudscript / ErrorCurrentEventOnTime / ErrorDataSync / ErrorFailedSubscription / ErrorItemsNotGranted / ErrorLoadDeck / ErrorLoadReplay / ErrorSavingMatch / ExcededMaxSessionLength / FailedToInitializeServices / GenericConnectionError / GuildPermissionError / InsuffientGems / InsuffientTickets / InternetUnreachable / InvalidGuildDescription / MaxPinnedMatches / MissingEventData / NameNotAvailable / NewerAccountVersion / PendingMatch / PlayerCantBeInvitedAlreadyInGuild / …AlreadyInvited / …FullGuild / …NotAccepting / PlayerNotFound / PracticeUserNotFound / PracticeUserNotOnline / RewardsAlreadyClaimed / SteamInitError / TwitchAccountNotLinked / Unauthorized / UnhandledError / UpdateAvailable）—— **但 `CustomErrors/Undefined` 不存在** ⇒ 兜底口径必须先定（见 §三·12）。

10. **`CampaignNotificationProvider` 与「逐格徽记」是两种模型**：我们按「当前所选阵营」发**一条**（`Shell/BadgeNotifications.cs:302-321`），原版是**逐格**注册带 `army` 过滤的 `CampaignBadge`（`ArmyItemContainer.cs:285`）。⇒ `CampaignBadge` 缺 `army` 字段这件事，账里没记，是战役 4 的真缺口核心。

11. **`Shell/` 现读 166 个 `.cs`**（不是 138；`.cs` + `.meta` 共 332 个条目）。⚠️ 条数别写死 —— 与本项目一贯口径一致。

12. `ItemDrawer.cs` 现读 **1900+ 行**、`DeckRuntime.cs` **8838 行**、`MainMenuRuntime.cs` 被本批 **27 处**引用 ⇒ 这三个是本批的写手瓶颈，派活时**先看 §2 的排程**再切块。

13. **`OfferContainer.Carousel` 的过滤口「可注入、默认 null=不过滤」是唯一诚实解**：原版 `DrawRewards` 里那条 `Where` 谓词的语义**在本机不可及**（`ObtainableItem` 的虚调用没解出）—— ⛔ 别为了「更像原版」硬猜一个过滤条件。
