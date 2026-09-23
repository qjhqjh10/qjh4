# Daily Reward Popup —— 调用链与运行时行为（只读调查）

> 2026-09-23。**版面 / 节点 / rect / 字体表见 `资料/日常_原版规格.md` §四**（本文不重复）。出处代号：`DF:` = `d:/2/tools/decomp_full/`（Ghidra 伪码）；`dump:` = `d:/2/tools/il2cpp_out/dump.cs`（字段偏移 + `Slot:`）。
> 🔧 **本轮新增的判据（下个会话可直接复用）**：
> ① **vtable 槽 = `klass + 0x138 + Slot*0x10`**（`methodPtr` +0 / `methodInfo` +8）。三处独立标定：`Close`=Slot8→0x1C0 · `ToFocus`=Slot7→0x1B0 · `IsActive`=Slot48→0x438；接口方法调用也用同一公式（`…*0x10 + 0x138 + klass`，见 `DF:DailyRewardTrackPanel__HandlePurchase.c:43-44`）。⇒ 伪码里 `(**(code**)(*klass+0xNNN))(...)` 能直接换成方法名。
> ② `DAT_184xxxxxx` 是**元数据用法槽**，值 = `(kind<<29)|index`（1=类型/3=MethodDef/5=字符串/6=MethodRef）；用 `script.json` 的 `ScriptMetadata`（`"Address"` 是十进制 **RVA**）按槽地址反查真名。
> ③ **共享泛型方法把 `MethodInfo*` 当额外末位实参传** ⇒ 反汇编里 2 个实参可能只是「1 个真参数 + 1 个 methodinfo」（判 `Any()` vs `Any(pred)` 就靠这个）。

## A. 打开它的入口

**A-1 完整链（全库只有这一条；`grep -rl DailyRewardPopup` 在 25,096 个 `.c` 里只命中它自己）**

| # | 环节 | 结论 | 出处 |
|---|---|---|---|
| 1 | 触发 | `MainMenuMission.TrySchedulePopup()`（虚方法 **Slot 74**）被 `Initialize()` 与 `UpdateSaveGame()` **尾调用**（`jmp [rax+0x5d8]`）；**非协程、不在 `Start` 里** | `DF:MainMenuMission__Initialize.c:12`、`DF:MainMenuMission__UpdateSaveGame.c:12`、`dump:60389` |
| 2 | 门 | `challenges = MissionPersistence.challengeProgress`（**裸数组**，不是 `CurrentChallenges`）；`if (!challenges.All(p => !p.CanCollect()))` 才排 —— **至少一条挑战可领**。（`CanCollect()` = `MissionChallengeProgress` 虚方法 **Slot 6**，VA 0x18069F0D0） | `DF:MainMenuMission__TrySchedulePopup.c:25,42`、`DF:MainMenuMission.__c___TrySchedulePopup_b__11_0.c:8`、`dump:61222` |
| 3 | 幂等 | `menuAction`(0x38) 若已在静态 `actions` 列表里 ⇒ **直接 return**（不重复排队） | `DF:MainMenuMission__TrySchedulePopup.c:44-59` |
| 4 | 排队 | `MenuActions.Instance.QueueAction(cb, 0,0, null, false,false,false, Array.Empty<Type>())`，返回值存回 `menuAction`。**是排队不是立即执行**：塞进静态 `List<Action> actions`；消费者是 `MenuActions.Routine()` **协程**（**不是 Update/LateUpdate**——`WindowsManager.Update` 只处理 ESC） | `DF:MainMenuMission__TrySchedulePopup.c:61-68`、`DF:MenuActions__QueueAction.c:87`、`DF:MenuActions._Routine_d__10__MoveNext.c:47,72,176,228` |
| 5 | **执行前的隐藏门**（源码里看不见） | `QueueAction` 把**空 menuFilter 改写成 `new[]{ typeof(MainMenuWindow) }`**（disasm 0x18069E413-0x18069E487，typeof 的 Il2CppType = `MainMenuWindow_var`）；而 `TryTriggerAction` 要求 `MenuFilter.Contains(currentWindow.GetType())` ⇒ **该 action 只在当前窗口恰好是 `MainMenuWindow` 时才会开火** | `DF:MenuActions__TryTriggerAction.c:63`；反汇编 0x18069E41A |
| 6 | 执行体 | `MainMenuMission.<>c.<TrySchedulePopup>b__11_1`：**先重检** `!challenges.All(x => !x.canCollect)`（这次读**裸字段 0x48**）→ `LiveOpsAssetUtility.GetComponentReference(mission, 0xC)` → `WindowsManager.Instance.OpenWindow(ref, mission, false, null)` | `DF:MainMenuMission___TrySchedulePopup_b__11_1.c:34,36,42`、`DF:MainMenuMission.__c___TrySchedulePopup_b__11_2.c:6` |
| 7 | `0xC` 是什么 | `EventComponentReferenceType.Popup = 12` ⇒ 去 **该 LiveOps 事件配置 `drawData(0x38)` 的 `<list@0x18>` 里按 `Popup` 键取 `AssetReference`**（**数据驱动**：预制体挂在 MainMenuMission 的事件配置里，不写死在代码里） | `dump:125603-125611`、`DF:Everguild.LiveOps.LiveOpsAssetUtility__GetComponentReference.c:10-11` |
| 8 | 开窗 | `WindowsManager.OpenWindow`：**先查 `automaticallyLoadedWindows` 缓存复用**（命中就**不重新 Instantiate**）→ 否则 `ComponentReference.Load` + `Instantiate(prefab, GetWindowAnchor(prefab.windowsPlacement@0x24))` → `SetActive(false)` 入缓存 → `OpenWindow(GameWindow,…)` → `OpenWindowCO` 协程（`Popup` 类型会把 `currentWindow.ToBackground()`；`openWindows` 尾元素相同则不重复入栈）→ `window.TryOpen(data, options)`（GameWindow Slot 6）。**没有 `CanOpen` 门、也没有「已开着就早退」**——重开已有窗口仍会再跑一次 `TryOpen` | 反汇编 0x180875990（**该地址没有独立 `.c`**）；`DF:WindowsManager__OpenWindowCO.c:29,32,38,50,80-89`；`dump:115570-115578`（`windowsPlacement`：`Popup=15`） |

**A-2 `DailyRewardPopup.Open()`（Slot 4）—— 两道自关 + 给谁赋了什么值**

```
GameWindow.Open(this,false);                                                      // :20 基类
if (!LiveOp.IsActive())            { base.Close(); return; }                      // :23-27
if (!LiveOp.CurrentChallenges.Any()){ base.Close(); return; }                     // :29-32
rewardsTrack.Initialize(LiveOp,false);                                            // :34-35
army = LiveOpsManager.GetHandler<CampaignHandler>().<int@0x7C>;  armySelector.Initialize(FeatureConfig.GetValidArmies(),army,false,false);  RefreshOnArmyChange(army);  // :39-46
```

- 🔴 **修正：判据①不是 `IsPremium()`**。`(**(code**)(*LiveOp+0x438))(...)` = **vtable Slot 48 = `LiveOpsEvent.IsActive()`**（VA 0x1808B3670）；`IsPremium` 是 **Slot 71** → 偏移 0x5A8/0x5B0，本路径没用到它。语义：`return GetBaseSave().IsActive`（= `MissionPersistence.IsActive`，`dump:` 偏移 **0x1A**），**例外**：`GetBaseData().activationType == OneTime(2)` 且 `save.IsCompleted` ⇒ 返回 false。`DF:Everguild.LiveOps.LiveOpsEvent__IsActive.c:9-18`、`dump:126224`、`dump:126485-126487`、`dump:126589`
- 判据② = **1 参 `Any()`（无谓词）**：「一条 current challenge 都没有」就自关。证据：反汇编 `:1806DCE36-40` 只写 `rcx`(source) 与 `rdx`(=元数据槽 `Method$System.Linq.Enumerable.Any<MissionChallengeProgress>()`)；被调体 `0x180c8c9f0` 把 `rdx` 当 `MethodInfo*` 解引用（`[rdx+0x38]`）而**从不调用它** ⇒ 不是委托。`DF:DailyRewardPopup__Open.c:31`（Ghidra 把实例化名打成了 `Any<int>`，那是共享泛型的代表名）
- **自关走的是 `LiveOpsEventWindow<MainMenuMission>.Close()`**（VA 0x181016820，`DF:DailyRewardPopup__Open.c:26`），**不是** 被覆盖的 `DailyRewardPopup.Close()`（那条会先 `TryCollect`）。
- **赋值的字段**（偏移 `dump:68853-68878`）：`rewardsTrack`(0x88)=`Initialize(mission,false)`；`armySelector`(0x80)=`Initialize(FeatureConfig.GetValidArmies(), handler.SelectedArmy, false,false)`；`header`(0x78) 由 `RefreshOnArmyChange` 填 —— `header.armyNameText`(0x20) `= I2 LocalizationManager.GetTermTranslation(<前缀字符串> + army.ToString())`、`header.armyIcon`(0x28) `= ArmyUtilities.Instance.armyIcons(0x28).GetArmyIcon(army)`，随后 `rewardsTrack.RefreshData(mission, refreshAll:true)`（`DF:DailyRewardPopup__RefreshOnArmyChange.c:34-50`）。⚠️ `ArmyHeader` 自己有个 `Initialize(CardArmy)` 干同样的事，这里**没调它**（`dump:68639-68648`）。
- `timerText`(0xA0) / `trackSidePanel`(0x98) **不在 `Open()` 里赋**，在 `ToFocus()`；`closeButton`(0x90) / `armySelector` / `rewardsTrack` / `timerText` 的事件订阅在 `Start()`。
- `LiveOp`(0x70) 由基类填：**`InitializeWindow(ILiveOps)`（Slot 14，VA 0x181016910）里 `this.LiveOp = liveOpsEvent`**（按 VA 反汇编 `:18101699B` = `mov [rsi+0x70], rax`）；`SetupData`（Slot 5，VA 0x181016C10）**无 `.c` 产物、反汇编只剩 null 检查 + 一次字段写入 + 尾调空存根** ⇒ 那一半**没读到**（见文末）。⚠️ `InitializeWindow` 的 `.c` 是 61 字节占位。

**A-3 `ToFocus()`（Slot 7）—— 倒计时与侧栏的重算点**

- `trackSidePanel.Initialize(LiveOp)` → `rewardsTrack.RefreshData(LiveOp, refreshAll:true)` → 取 `CurrentChallenges.FirstOrDefault()` 的 `Challenge`，**若 `challenge is DailyLoginChallenge`**，算 `next = dailyLoginChallenge.GetNextUpdate(progress)`；`next > TimeHelper.ServerDateTime()` ⇒ `timerText.Initialize(next, <字符串>)`，否则 `timerText.gameObject.SetActive(false)`。`DF:DailyRewardPopup__ToFocus.c:21-57`
- 倒计时**由 `TimerDisplay` 自己驱动**（不是 Signal）：`TimerDisplay.Update()` 里 `if (Time.frameCount % Application.targetFrameRate == 0) UpdateTimer();` ⇒ **1 Hz**；`UpdateTimer` 用 `target − PlayerDataManager.CurrentServerDateTime()`，到点后把文字清空、**把自己那个 GO `SetActive(false)`**、用 `hasRunOutOfTime`(0x98) 闩住并触发 `OnRunOutOfTime` **一次**；`DF:TimerDisplay__Update.c:13-21`、`__UpdateTimer.c:29,41,56-76`。**订阅者 = `DailyRewardPopup.ToFocus()`（vtable Slot 7，`klass+0x1B0`）** ⇒ 到点后自己重进 `ToFocus`（重算下一次刷新：还在未来就重开计时、否则隐藏计时器），`DF:DailyRewardPopup__Start.c:57-62`、`dump:68872`（`ToFocus() Slot: 7`）
- `Start()` 四个订阅：`closeButton.onClick += Close()`（Slot 8，`klass+0x1C0`）· `armySelector.OnArmySelected += SetArmy` · `rewardsTrack.OnCollect += <Start>b__6_0`（→ `LiveOp.TryCollect(null)`）· `timerText.OnRunOutOfTime += ToFocus`。`DF:DailyRewardPopup__Start.c:22-62`

## B. 它调用的东西

**B-1 领奖链（点 Entry 上的领取区 → 真有 PlayFab 调用）**

| # | 环节 | 出处 |
|---|---|---|
| 1 | `DailyRewardDrawerController.Start`：`claimButton`(0x58)`.onClick`(+0x100) += `<Start>b__15_0` | `DF:DailyRewardDrawerController__Start.c:13-18` |
| 2 | `<Start>b__15_0`：先 `claimButton.interactable=false`，再调自己的 `onCollectCallback`(0x68) | `DF:DailyRewardDrawerController___Start_b__15_0.c:11-16` |
| 3 | 0x68 = `DailyRewardItemContainer.OnCollect`（由 `ItemContainer.Initialize` 注入）→ 转发到 item 的 `onCollectCallback`(0x70) | `DF:DailyRewardItemContainer__Initialize.c:101-104`、`DF:DailyRewardItemContainer__OnCollect.c:7-11` |
| 4 | 0x70 = 闭包 `<>c__DisplayClass13_0.<Initialize>b__0` ⇒ `onCollect(challenge.Rewards[index])` | `DF:DailyRewardItemContainer.__c__DisplayClass13_0___Initialize_b__0.c:12-20` |
| 5 | 该 onCollect = `DailyRewardSelector.CollectRewardClicked` → 只转发到 `OnCollect`(0x58) 事件（纯 `Delegate.Combine`）；**唯一订阅者 = `DailyRewardPopup.<Start>b__6_0`（`milestone` 参数没用）→ `LiveOp.TryCollect(null)`** | `DF:DailyRewardSelector__CreateRewards.c:79-82`、`__CollectRewardClicked.c:7-11`、`DF:DailyRewardPopup__Start.c:33-56`、`___Start_b__6_0.c:10-12` |
| 6 | `MissionEvent<object,object>.TryCollect(Action)`（VA 0x18102F400，**无 `.c` 产物**，Ghidra 只给了 `FUN_18102f400`）→ … → `Collect(challenge,onComplete)`(VA 0x18102E250) → **`Missions.CollectChallenge`**：`WindowsManager.ShowPopUp(...)` + **`PlayfabWrapper.GenericCloudScriptHandler<object>(0x38E, req, onSuccess, onFail)`**（0x38E = **cloudscript #910**） | 反汇编/resolve_va；`dump:60876-60880`；`DF:Missions__CollectChallenge.c:44,74` |
| 7 | 成功回调 `<>c__DisplayClass11_0.<CollectChallenge>g__OnCollectSuccess_1`：`WindowsManager.HidePopUp(1)` → `UpdateMultiplePersistences` → `Signal.Raise<...>` → **`RewardService.Collect(rewards, openWindow:1, onComplete,…)`** → `GameAnalytics.RecordTransaction` | `DF:Missions.__c__DisplayClass11_0___CollectChallenge_g__OnCollectSuccess_1.c:25,27,72,77,82` |
| 8 | `RewardService.Collect`：抛全局 `CollectRewardsSignal` → 逐个入账 → 加 collector XP → 关掉已开的奖励窗后 **`WindowsManager.OpenWindow(RewardWindowContext)` = 奖励展示窗**，把 `onComplete` 挂到新窗 `OnCloseWindow`(0x60)；奖励含 live-ops 物品则 `LiveOpsHandler.RefreshHandler`。⚠️ **点关闭键也走同一条链**：`DailyRewardPopup.Close()` = `LiveOp.TryCollect(() => base.Close())` ⇒ **收完再关窗** | `DF:RewardService__Collect.c:64-66,116-117,141-143,167,190-205,214-217`；`DF:DailyRewardPopup__Close.c:15-19` |

**B-2 两个类的分工**
- `DailyRewardSelector`（整条轨，挂在 `Rewards Scroll View`）：`Initialize` 清空 content → `RefreshData(null,true)` → 首个 Challenge 的里程碑 >5 时滚到第 (count−3) 项；`RefreshData(liveOp,refreshAll)` **换事件就回 `Initialize`，否则逐个 `containers[i].Refresh()`**；`CreateRewards` 逐条 `Instantiate(rewardItemContainer)` + `Initialize(...)`（回调传 `CollectRewardClicked`）；`Reset` 全销毁。`AdjustView` 与 `Initialize` 尾部逐字相同且**全库无调用者 = 死代码**。`DF:DailyRewardSelector__Initialize.c:14-56`、`__RefreshData.c:25-52`、`__CreateRewards.c:28-84`、`__Reset.c:28-62`
- `DailyRewardItemContainer`（一天 = 一个 Entry）：`Initialize` 存 0x78/0x80 + 建闭包 + 按 `GetCurrentState` 配 `progressSlider`/`progressStatusImage` + 把里程碑拆两半（**<10 = normal 抽屉、≥10 = premium 抽屉**，用 `Rewards.ToLookup(RewardTier)`）各喂给一个 `DailyRewardDrawerController`；`Refresh()` 重算 state 并 `SetState`。`DF:DailyRewardItemContainer__Initialize.c:32-135`、`__Refresh.c:19-87`、`__GetCurrentState.c:18-46`

**B-3 `DailyRewardTrackPanel.purchaseButton`（买 Premium 侧栏）**
- `Start`：`purchaseButton`(0x28)`.OnClick` UnityEvent += `HandlePurchase`。`DF:DailyRewardTrackPanel__Start.c:13-18`
- `HandlePurchase`：**已拥有就 return**（`InventoryManager.HasItem(premiumItem)`）；否则 `Interactable=false` → `liveOp.Data.PremiumOffer.TryPurchaseOffer(onComplete, onError:null, showRewardPopup:true)`（`IShopOffer` **Slot 9**，走手工接口派发：`…*0x10 + 0x138 + klass`）。成功回调 `<HandlePurchase>b__6_0(IReadOnlyList<RewardInfo> _)`：**忽略买到的物品**，只把「首个 `MissionChallengeProgress`」的 **`canCollect`(0x48) 置 true** ⇒ 让 premium 里程碑变可领（与 A-1 第 6 步的重检对上）。`DF:DailyRewardTrackPanel__HandlePurchase.c:23,28,43-52`、`___HandlePurchase_b__6_0.c:13-17`、`dump:61190`
- 显示条件（`Initialize`，`ToFocus` 也会跑）：`PremiumItem` 为空 ⇒ `premiumTrackHolder.SetActive(false)`；否则 `SetActive(!HasItem(premiumItem) && liveOp.ShowInMainMenu)`，并 `PriceDisplayButton.Setup(premiumOffer)`。`DF:DailyRewardTrackPanel__Initialize.c:30-63`

**B-4 `closeButton` → 哪个基类方法、关窗后谁被通知**
- `closeButton.onClick` → **`DailyRewardPopup.Close()`（Slot 8 覆盖）** → 不直接关，而是 `LiveOp.TryCollect(() => base.Close())`；`<Close>b__11_0` 里调 **`LiveOpsEventWindow<MainMenuMission>.Close()`**。`DF:DailyRewardPopup__Close.c:15-19`、`DF:DailyRewardPopup___Close_b__11_0.c:9`
- 真正关窗在 `GameWindow.Close()`：播关闭音效 → **`WindowsManager.CloseWindow(this)`**。**`GameWindow.Close()` 自己不触发 `OnCloseWindow`** —— `OnOpen`/`OnCloseWindow`（Action，0x58/0x60）是**由「打开方」挂的**（例：`RewardService` 打开奖励窗后挂它的 `OnCloseWindow` 回填 `onComplete`），`WindowsManager` 侧的通知面是 `OnWindowOrTabChanged`。`DF:GameWindow__Close.c:37-57`、`DF:RewardService__Collect.c:214-217`

## C. 谁在监听它

- **没有第三方监听**：`rewardsTrack.OnCollect` / `armySelector.OnArmySelected` / `timerText.OnRunOutOfTime` / `closeButton.onClick` 的**唯一订阅者都是 `DailyRewardPopup` 自己**（`DF:DailyRewardPopup__Start.c`）；全库 grep `add_OnCollect` 只命中 Selector 自身 + 无关的 `DraftRewardPanel`。
- **全局事件总线**是 `Scaffold.Core.Events.Signal`：领奖成功链里两次 `Raise` —— `Missions.<CollectChallenge>OnCollectSuccess`（`…OnCollectSuccess_1.c:72`）与 **`CollectRewardsSignal`**（payload `IReadOnlyList<RewardInfo> Items`，`DF:RewardService__Collect.c:64-66`、`dump:26813`）。跨目录 grep `CollectRewardsSignal` 只 3 处提到（ctor / `RewardService` / `CardServices` 的 craft，后者是订阅方）；**还有没有别的订阅者没穷举**（订阅点是类型常量，不是类名字符串）。
- **领奖后的刷新**：`DailyRewardPopup` **没有**覆盖 `ILiveOpsWindow.OnRefresh`（Slot 15）；它靠 `ToFocus()`（Slot 7）—— 而 `ToFocus` 正是 `OnRunOutOfTime` 的回调，**也是窗口再次聚焦时的入口**（`A-3`）。

## D. 三态（本窗「一个值 ≠ 全部情况」）

**状态枚举**（`DailyRewardDrawerController.State`）：`Locked=0 · Unlocked=1 · Collected=2 · PremiumLocked=3`。**每个抽屉各自持有自己的 State**（`CurrentState` 在抽屉的 0x70）。

**切换的唯一方法 = `DailyRewardDrawerController.SetState(state)`**（`DF:DailyRewardDrawerController__SetState.c:8-28`；字段偏移 `dump:68820-68836`）——**序列化里的出厂 active 只是 prefab 默认，运行时第一次 `Initialize` 就被它覆盖**：

| 字段(偏移) | 对应节点 | Locked 0 | Unlocked 1 | Collected 2 | PremiumLocked 3 |
|---|---|---|---|---|---|
| `premiumLockIndicator`(0x30) | **`Premium Indicator`** | 关 | 关 | 关 | **开** |
| `claimedIndicator`(0x38) | **`Gacha Reward Claimed`** | 关 | 关 | **开** | 关 |
| `shadow`(0x48) | **`Shadow`** | 关 | 关 | **开** | 关 |
| `highlight`(0x50) | **`Highlight`** | 关 | **开** | 关 | 关 |
| `claimButton`(0x58) | **`colider`**（真的点击区） | 关+不可点 | **开+可点** | 关+不可点 | 关+不可点 |

> 上表的「对应节点」是把 bundle `menus_assets_all` 里 12 个 `DailyRewardDrawerController` MB 的字段反解到 GO 名得到的（`GameObject/<名>_<pid>.json` 的文件名 + `m_Component` 反查），与 `日常_原版规格.md` §四 的节点名逐一吻合。另三个字段不属于三态：`rewardNameText`(0x20)=`EverguildTextMeshPro`（在 `Initialize` 里填 `string.Format("…", 数量, 名称)`）、`extraRewardIndicator`(0x28)=`Extra Reward Indicator`（**`rewards.Count > 1` 才开**）、`rewardDrawerHolder`(0x40)=`Reward Holder`。`DF:DailyRewardDrawerController__Initialize.c:44-47,132-137,179-182`

**状态怎么算出来 = `DailyRewardItemContainer.GetCurrentState(scoringEvent, challenge, progress, index)` → `(normal, premium)`**（`DF:DailyRewardItemContainer__GetCurrentState.c:18-46`）：
```
normal  = (index < progress.collectedRewards /*0x10*/) ? Collected
                                                    : (milestone.targetValue /*0x14*/ <= progress.CurrentValue ? Unlocked : Locked);
premium = !scoringEvent.IsPremium                                   ? PremiumLocked
        : (normal == Collected && progress.premiumCollectedRewards /*0x14*/ <= index) ? Unlocked
        : normal;
```
⇒ **PremiumLocked 只由「这条 mission 的整体 `IsPremium`」驱动**（与 A-2 的判据无关）；`Milestone` 的字段：`difficulty` 0x10 / **`targetValue` 0x14** / `rewards` 0x18；`ChallengeMilestone[]` 数组头在同名字段 0x18（长度 0x18、元素 0x20 起）。`dump:59666-59669`、`dump:59764-59767`

**状态在运行时的应用点（谁把它刷进去）**：
- 首次：`ItemContainer.Initialize` → `GetCurrentState` → `drawer.Initialize(rewards, state, cb)` → 末尾 `SetState(state)`。
- 之后：`ItemContainer.Refresh()` → 重算 → `normalRewardDrawer.SetState(state.Normal)` / `premiumRewardDrawer.SetState(state.Premium)`（`DF:DailyRewardItemContainer__Refresh.c:64-87`）；其上游 = `DailyRewardSelector.RefreshData(liveOp,true)` ← `popup.Open()`（经 `Selector.Initialize`）/ `popup.ToFocus()` / `RefreshOnArmyChange()` ⇒ **每次窗口聚焦、切阵营、倒计时归零都会重算三态**。
- **特例（`progress == null`，当天进度没了）**：normal 抽屉**硬编码成 Collected**（与 `SetState(2)` 逐字段一致），premium 抽屉取 `(原 CurrentState == 3) ? PremiumLocked : Collected`。`DF:DailyRewardItemContainer__Refresh.c:22-57`
- **🔴 两处只在 `Initialize` 里设、`Refresh` 不管的**（三态里最容易复刻错的地方）：
  `progressStatusImage`(0x58) → `Personal Progression/Image` 的 **sprite 换图（不是显隐）**：`normal == Locked ? progressStatusOff : progressStatusOn`（`40k_missions_milestone_off` / `_on`）；
  `progressSlider`(0x50) → **`milestone.Index != 0` 才 `SetActive`**（第 0 天没有进度条）+ `wholeNumbers=true`。`DF:DailyRewardItemContainer__Initialize.c:55-78`；sprite 名见 `资料/日常_原版规格.md:362`

## 本轮没查到的

1. **`LiveOpsEventWindow<T>.SetupData`（Slot 5，VA 0x181016C10）到底做了什么**：无 `.c`；按 VA 反汇编只见 null 检查 + 一次字段写入 + **尾调到 0x18067EE70**，而该地址是**几十个类的 `..ctor` 共用的空存根**（`resolve_va` 一次列出 20+ 个名字）⇒ **判不出 `OnRefresh`（Slot 15）何时被调、写的是哪个字段**。已确证的只有 `LiveOp`(0x70) 的赋值点（见 A-2）。还差：这两个入口的完整指令流（`InitializeWindow` 尾部还有 `0x180866100`、`0x180001F90` 两个未识别调用）+ 一个能在实况里断点的探针。
2. **`TimerDisplay.Initialize(next, <字符串>)` 的第三个实参内容**：编码值是字符串字面量用法 `0xa0000001`，而 `stringliteral.json` 只按**数据地址**索引、`ScriptMetadata` 里**没有任何 `StringLiteral` 条目** ⇒ 值取不到。同理 `RefreshOnArmyChange` 里拼在 `army.ToString()` 前的 I2 前缀（`0xa0002105`）。还差：`metadataUsage` 的**字符串用法表 index→值**映射（本机没现成工具）。
3. **`progressSlider` 的 `minValue/maxValue` 实参**：Ghidra 丢在 XMM 里（`Slider__set_maxValue()` 无实参）。还差：反汇编 `DF` 对应 VA 0x1806DC560 的初始化段，把 `movss/cvtsi2ss` 的常量读回来（`工具/disasm_va.py` + `read_literal.py`）。
4. **`CollectRewardsSignal` 的订阅者没穷举**（订阅点用 `TypeInfo` 常量而非类名，需按类型常量继续追）；本类里 `DailyRewardDrawerController.__c___.ctor_b__20_0.c` 是 **61 字节占位**（内容抄的是 `NetworkingPeer__OnMessage`）—— 唯一读不到的产物，本轮没有据此断言「什么都不做」。
