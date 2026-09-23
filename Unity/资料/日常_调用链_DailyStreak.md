# 日常连登 `Daily Streak Popup`（`DailyStreakWindow`）调用链 —— 运行时行为

> **只写运行时行为**（谁开它 / 它调谁 / 谁听它 / 两态互斥）。**版面/节点/rect/图名/字号见 `资料/日常_原版规格.md` §五（:368-400）**，本文件不重抄。
> prefab 根 pid `2821652005965210283` · `DailyStreakWindow` MB pid `8654310213240890027`（`d:/2/解包整理/03_界面UI/菜单/MonoBehaviour/MonoBehaviour_8654310213240890027.json`）。

### ◇ 本轮挖到的判据（下次直接用）
1. **vtable 偏移 → 方法名**：`d:/2/tools/il2cpp_out/il2cpp.h` 给每类生成了 `__VTable`（`_N_方法名`），**N = `dump.cs` 的 `Slot:` = 绝对槽号**；运行时 `vtable[0]` 在 **`klass + 0x138`**（`VirtualInvokeData` 16B：`+0`=methodPtr、`+8`=MethodInfo\*）⇒ **槽号 =（偏移 − 0x138）/ 16**。本轮据此把 `0x178 / 0x1b0 / 0x1b8 / 0x438` 定名（`Open`/`ToFocus`/`Close`/`LiveOpsEvent.IsActive`），并被 `LiveOpsEventWindow_VTable._4_Open · _7_ToFocus · _8_Close` **三重独立核对**。
2. **`DAT_xxxxxxxx` → 方法名 / 字符串**：`script.json` 的 `ScriptMetadataMethod`（MethodInfo 地址 ⇥ 方法名）与 `ScriptString`（地址 ⇥ 字面量）。⚠️ `d:/4/Unity/工具/resolve_va.py` 只吃前者，**字符串要另查 `ScriptString`**（本轮定名 30+ 常量）。
3. **`.c` 里看不见的实参 → `工具/disasm_va.py`**：靠它判定 `Enumerable.Any` 走的是**无谓词**重载（r8 从未被赋值），并从产物缺失的 `TryCollect` 里读出 PlayFab 调用。
4. **产物缺口（实测，别再找）**：`LiveOpsEventWindow<T>` 的 `Close` / `SetupData` / `OnRefresh` / `InitializeWindow` / `.ctor` **没有产物文件**（`find d:/2/tools -name "*LiveOpsEventWindow*"` 只有 4 个非泛型文件）；`LiveOpsEventWindow__InitializeWindow.c` **是 61 字节占位**（内容 = `NetworkingPeer__OnMessage`）⇒ 这两处只能写「查不到」。

---

## A 打开它的入口 —— 两条互不相干，都落在 `WindowsManager.OpenWindow`

**(1) 用户点（主菜单左栏条目，即时）**：`MissionMenuContainer.OnClick()` → `LiveOp.OpenWindow()`（**虚调用 slot 58** = `0x4d8/0x4e0`）— `MissionMenuContainer__OnClick.c:24-25`（同款 `LiveopMenuContainer__OnClick.c:17`）→ `Everguild.LiveOps.LiveOpsEvent.OpenWindow()`：按 `LiveOpsEventData.GetState(TimeHelper.ServerDateTime)` 选下标 —— **state 1 → 0x14(20) · state 3 → 0x15(21) · 其它 → 10**，再 `LiveOpsAssetUtility.GetComponentReference(this, idx)`；取不到就 `CustomDebug.LogError("Event {0} has no reference to {1} window")`（字面量 `ScriptString 0x42a84c0`），取到就 `WindowsManager.OpenWindow(comp, this, 0, data[0x58])` — `Everguild.LiveOps.LiveOpsEvent__OpenWindow.c:20-45`。
🔴 **更正**：那 10/20/21 **不是 `MainWindow(10)` 槽**，是**按 Int32 枚举键的组件引用表下标**（`LiveOpsAssetUtility.GetReference<Int32Enum, object>(event, event.<0x38>.<0x18>, idx)`，RVA `0xd12880`）；`dump.cs` 里**没有**叫 `MainWindow` 的成员。

**(2) 主菜单自动弹（连登窗真正那条，排队）**：`MainMenuMission.Initialize()`（Slot 41）**末尾**调 `TrySchedulePopup()`（**虚调用 slot 74** = `0x5d8/0x5e0`）— `MainMenuMission__Initialize.c:11-12`；**同一条也在** `UpdateSaveGame()`（Slot 47）里 — `MainMenuMission__UpdateSaveGame.c:12`。
`TrySchedulePopup()` 先 `Enumerable.All(challenges, <b__11_0>)` 判一次，**不满足才** `MenuActions.Instance.QueueAction(<MainMenuMission.<TrySchedulePopup>b__11_1>, …)`，句柄写进 `MainMenuMission.menuAction`(0x38) 做**幂等**（已在列表里就直接 return）— `MainMenuMission__TrySchedulePopup.c:47-63`。
排队体 `<TrySchedulePopup>b__11_1()` **再判一次** `All(challenges, x => !x.canCollect)`（谓词 `MainMenuMission.__c___TrySchedulePopup_b__11_2.c`：`param_2[0x48] == 0`；`canCollect` 0x48 见 `dump.cs:61190`），通过才 `GetComponentReference(this, 0xC = 12)` + `WindowsManager.OpenWindow(comp, this, 0, 0)` — `MainMenuMission___TrySchedulePopup_b__11_1.c:26-33`。
⇒ **两条入口索引不同（自动弹 12 / 用户点 10·20·21），但都是同一张引用表的下标。** 且 `grep -rl "DailyStreakWindow" d:/2/tools/decomp_full` 只命中 `DailyStreakWindow*.c` 自己 + 兄弟 `DailyRewardPopup*` ⇒ **没有第三条入口**。

### A.2 `DailyStreakWindow.Open()`（Slot 4）判据的完整展开
```csharp
public override void Open() {
  base.Open();                                          // GameWindow.Open：只做小屏缩放（见下）
  var m = LiveOp;                                       // 0x70
  if (m == null) return;
  if (!m.IsActive())              { base.Close(); return; }   // slot 48 = LiveOpsEvent.IsActive()，无参 bool
  if (!m.CurrentChallenges.Any()) { base.Close(); return; }   // Enumerable.Any 的「无谓词」重载
  bool failed = m.HasFailed;                            // MainMenuMission 0x30
  streakFailedPanel.SetActive(failed);                  // 0xC0
  if (failed) streakLost.text = string.Format(Loc("Rewards Menu/DailyStreak/StreakLost"), m.FailedValue); // 0xB8 / 0x34
  streakSuccessPanel.SetActive(!failed);                // 0xB0
  m.HasFailed = false;                                  // 一次性消费 ⇒ 第二次 Open 必进「连胜态」
  header.Initialize(m, CloseButtonClicked);             // 0x90
}
```
- 指令级出处（`disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x1806DF230`）：`IsActive` `2E3/2E5/2E7`；`Any` `310/315/317`（**r8 从未赋值 ⇒ 无谓词重载**）；`HasFailed` 读 `32A`；失败面板 `34C`；`streakLost` 格式化 `3B6-3D4`；连胜面板 `41E`；`HasFailed = 0` `42C`；`header.Initialize` `470`；两处早退的 `base.Close()` = `497`（`call 0x181016820`）。
- 🔴 `HasFailed` / `FailedValue` 的宿主是 **`MainMenuMission` 本体**（`dump.cs:60340` 段，0x30 / 0x34），**不是窗口字段** ⇒ 「断签」是**存档数据**，`Open()` 读完即抹掉。
- 🔴 两处早退调的是 **`base.Close()`（`LiveOpsEventWindow<T>.Close`）而不是 `Close()` 覆写** ⇒ **不触发 `TryCollect`**（见 B.4）。
- `base.Open()`（`GameWindow.Open`）只干一件与版面无关的事：小屏时才 `AddComponent<TransformScalerBySmallScreenUI>()` + `SetScale(extraScaleSmallScreen)`（= 1.0，本窗不缩放）— `GameWindow__Open.c:17-59`。

### A.3 `Open` / `Awake` / `Start` / `ToFocus` / `RefreshRewards` 给哪些字段赋了值

| 字段（偏移） | 赋值点 | 赋的是什么 |
|---|---|---|
| `0x78 rewardContentAnchor` | `RefreshRewards:61,65` | `Instantiate(dailyStreakItemContainer, rewardContentAnchor)` 的**父**（GO `Rewards Content`） |
| `0x80 currentStreakText` | `RefreshRewards:47-50` | `challenge.currentValue(0x40).ToString()`（GO `Current Streak Value`） |
| `0x88 dailyStreakItemContainer` | `RefreshRewards:60,65` | 只当 `Instantiate<DailyStreakItemContainer>` **模板**（GO `Daily Streak Reward Popup Entry`） |
| `0x90 header` | `Open:63-68` | `WindowHeaderWithBackButton.Initialize(mission, CloseButtonClicked)` |
| `0x98 backgroundButton` | `Start:15-20` | `BackgroundCloseButton.onClick(0x28) += CloseButtonClicked` |
| `0xA0 timerDisplay` | `Start:27-32`；`ToFocus:30-37` | 挂 `OnRunOutOfTime += ToFocus`（虚槽 slot 7）；再 `Initialize(DailyLoginChallenge.GetNextUpdate(challenge))` |
| `0xA8 scaleMultiplierFirstElement` | **只读**（`RefreshRewards:96`），全类无赋值点 | 1.2（序列化）—— 见 A.4 |
| `0xB0 / 0xB8 / 0xC0` | `Open`（见 A.2） | 两态面板 + 断签文案 |
| `0xC8 resetStreakButton` | `Start:21-26` | `EverguildButton.m_OnClick(0x100) += ResetStreakAfterFail` |
| `0xD0 currentRewardItemContainers` | `RefreshRewards:72-92` | `List.Add(每个实例)`；`Reset()` 清空 |
- `Awake()` = `Reset()`：`DestroyAllChildren(rewardContentAnchor)` + `List.Clear()` — `DailyStreakWindow__Reset.c:14-27`。
- **`OnSetup` = 基类 `LiveOpsEventWindow<T>.SetupData`（Slot 5）：本类没重写，基类体产物缺失 ⇒ 查不到**（还差：该泛型实例的反编译；`GameWindow.SetupData` 只写 `0x48`(options) / `0x40`(data)，与 LiveOp 无关 — `GameWindow__SetupData.c`）。

### A.4 `scaleMultiplierFirstElement = 1.2` 用在哪
**唯一读取点** = `RefreshRewards:96-101`：循环 `for (i = challenge.collectedRewards(0x10); i < challenge.currentValue(0x40) + 7; i++)`（:51-52），**只在第一次迭代**（`iVar15 == *(int*)(challenge + 0x10)`，:93）把刚 `Instantiate` 那个条目的 `localScale` 三分量各乘一次 `*(float*)(this + 0xA8)`。⇒「第一个元素」= **本次生成的第一个奖格**（不是「列表里第一个已存在的」）。按名字 grep 全产物只有本类 ⇒ **与 `DailyStreakItemContainer` 无关**（容器自己不读它）。

---

## B 它调用的东西

1. **`resetStreakButton`（`'Reset Streak'`）**：`EverguildButton.m_OnClick`（`EverguildButton : Button`，`Button.m_OnClick` 偏移 **0x100**，见 `dump.cs:921546-921551`；EverguildButton 自身字段从 0x108 起）→ `DailyStreakWindow.ResetStreakAfterFail()`。体 = `streakFailedPanel.SetActive(false)` + `streakSuccessPanel.SetActive(true)` + `RefreshRewards()`（`ResetStreakAfterFail.c:5-10`）。🔴 **不发 PlayFab / 不发任何网络请求**（整条链只有 SetActive / Instantiate / List.Add / 本地取挑战 / `TimerDisplay.Initialize`）。⚠️ prefab 上该钮 `m_OnClick.m_PersistentCalls.m_Calls` **是空数组**（`MonoBehaviour_7279079621080523435.json`）⇒ **100% 靠代码挂**；且 `ResetStreakAfterFail` **无静态调用者**（`grep -rl` 只命中自己）⇒ 唯一入口就是 `Start` 那次 AddListener。
2. **`Collect` 按钮**：那个 GO 名字叫 `Collect`（pid `-524221404242109311`），它是条目的 **`claimLabel`**；**真正可点的是 `claimButton`**（pid `102219725128080513`，`EverguildButton`，挂在 GO **`colider`** 上）。⚠️ `WF_Special offer_Value` **不是组件名**，是按钮 Image 的 **Sprite 资产名**。
链：`DailyStreakItemContainer.Start()` 把 `claimButton.m_OnClick(0x100)` 连到 `<Start>b__13_0`（`DailyStreakItemContainer__Start.c:14-18`）→ 该回调 `interactable = false` 后调 `onCollectCallback`(0x58)（`DailyStreakItemContainer___Start_b__13_0.c`）→ `onCollectCallback` 就是 **`DailyStreakWindow.CollectRewardClicked`**（`RefreshRewards:107-109` 传的 `DAT_1842b3620`）。体：`CollectRewardClicked()` = **`LiveOp.TryCollect(RefreshRewards)`**（`FUN_18102f400` = `MissionEvent<…>.TryCollect`；回调 `DAT_1842b3720` = `RefreshRewards` — `CollectRewardClicked.c:15-19`）。
✅ **`TryCollect` 确实发 PlayFab**：`TryCollect` 的 `.c` 产物缺失，改用反汇编读到 `call 0x180d620d0` = `PlayfabWrapper.GenericCloudScriptHandler<object>`，实参 `mov ecx, 0x38b` = **`CloudHandle.UpdateMissionPoints`(907)**（`dump.cs:77079`；指令 `0x18102FA46-4E`）⇒ **点 Collect = 「向服务端收这次任务点 + 回调里刷新这一屏」**。
3. **`DailyStreakItemContainer` 负责什么**：**一个奖格**（「第一个元素放大 1.2」**不是它**，见 A.4）。`Initialize(rewards, state, onCollect)`：在 `rewardDrawerHolder`(0x30) 下 `ItemDrawer.Draw(...)` 画奖励图标；给 `rewardNameText`(0x20) 写名字（本地化键 `PlayerProfile/Title` / `PlayerProfile/Avatar` 由 `String.Format("{0} {1}")` 拼）；奖励数 > 1 时显 `extraRewardIndicator`(0x28)；把 `onCollect` 存 0x58；最后 `SetState(state)`（`__Initialize.c`）。`SetState(state)`：`highlight`(0x38) / `claimLabel`(0x40) / `claimButton` 的 GO(0x48) **三者同开同关**，判据一律 **`state == 1`**，并写 `CurrentState`(0x60)（`__SetState.c:8-19`）；判据枚举 = **`DailyStreakItemContainer.State { Locked = 0, Unlocked = 1 }`**（`dump.cs:68455-68458`）——⚠️ 另有一个同构枚举 **`DailyStreakWindow.State { FailedDailyStreak = 0, SuccessDailyStreak = 1 }`**（`dump.cs:68558-68561`），别混。谁给 state：`RefreshRewards:104-109` 传 `milestone.targetValue(0x14) <= challenge.currentValue(0x40)`（`ChallengeMilestone.targetValue` 见 `dump.cs:59313`）⇒ **已达标的奖格才亮出 Collect 钮**。`Refresh(newState)` 先调 `currentDrawer`(0x50) 一个**无参虚方法（vt 偏移 0x198，未定名）**再 `SetState`（`__Refresh.c:6-10`）。容器上**没有 UnityEvent / event 字段**（字段全表 `dump.cs:68501-68521`，唯一委托是私有 `Action onCollectCallback` 0x58）。
⚠️ 容器上两个产物缺陷：`<>c.<.ctor>b__18_0` 是 **676 个 61 字节占位之一**；`<>c__DisplayClass14_0.<Initialize>b__0` **同名撞 RVA**（文件里装的是 `MissionMilestoneStep…<DisplayCheckmark>b__0`）⇒ 两者的体**查不到**。
4. **关闭 / 返回**：`Header With Back Button`(GO `-1476341376147774805`) → `WindowHeaderWithBackButton`：`OnEnable()` 挂 `backButton.m_OnClick(0x20→0x100) += BackButtonPressed`、`OnDisable()` 摘掉（方法名由 `ScriptString 0x4277748` 定名）— `__OnEnable.c` / `__OnDisable.c`；`BackButtonPressed()` 调用 `Initialize` 时存进 `0x38 OnBackButtonPressed` 的 `System.Action`（`__BackButtonPressed.c`），本窗存进去的是 **`DailyStreakWindow.CloseButtonClicked`**（`Open:63-68`）。`CloseButtonClicked()` 体 = **`Close()`**（虚调用 slot 8，偏移 `0x1b8/0x1c0`）—— ⚠️ 它的 `.c` **撞了 `TutorialTipScript.StopTipPermanent` 的 RVA `0x646E00`**（同体是巧合，但两处都读 0x1b8/0x1c0 互证）。`DailyStreakWindow.Close()`（Slot 8）= **`LiveOp.TryCollect(() => base.Close())`**（`DAT_1842b3420` = `<Close>b__23_0`，其体只调 `LiveOpsEventWindow<…>.Close`）— `__Close.c:12-19`；与兄弟窗 `DailyRewardPopup.Close` **完全同构** ⇒ **关窗也会先自动收取**（同一条 `CloudHandle.UpdateMissionPoints`）。⇒ **背景遮罩（`Menu Dark Background` 的 `BackgroundCloseButton.onClick`）与返回钮走同一个 `CloseButtonClicked`**；`LiveOpsEventWindow<T>.Close()` 的体**产物缺失 ⇒ 查不到**。

---

## C 谁在监听它
**它自己挂出去的三条（全在 `DailyStreakWindow.Start()`，`__Start.c:15-33`）**：

| 被听者 | 连接的字段 | 回调 |
|---|---|---|
| `backgroundButton`(0x98) | `BackgroundCloseButton.onClick`(0x28) | `CloseButtonClicked` |
| `resetStreakButton`(0xC8) | `EverguildButton.m_OnClick`(0x100) | `ResetStreakAfterFail` |
| `timerDisplay`(0xA0) | `TimerDisplay.OnRunOutOfTime`(0x90，**C# `event Action`，不是 UnityEvent**) | **`DailyStreakWindow.ToFocus`**（虚槽 slot 7） |
外加第三处同一回调：`Open()` 把 `CloseButtonClicked` 交给 `header.Initialize`（B.4）。`OnDestroy()` **只摘前两条**（`0x98` 与 `0xC8`）—— **`timerDisplay.OnRunOutOfTime` 没有摘**（没有第三个 `RemoveListener`，`__OnDestroy.c:17-31`）；两者同属一个 prefab 所以不构成泄漏，但**这是原版实况**。`0xC8` 在 `Start` 用 `+0x100`、在 `OnDestroy` 用 `*(param_1+200)` ⇒ **同一字段**（`Button.m_OnClick`）。

**别的信号 / 外部监听方：没有**。`grep -rl "DailyStreakWindow" decomp_full` 命中的文件**全是它自己**；`LiveOpsEventWindow`（非泛型）**零字段**、`LiveOpsEventWindow<T>` 唯一字段是 `<LiveOp>`（`dump.cs:52884-52905`）⇒ **没有 `OnStreakFailed` 之类订阅**；全库搜 `StreakFailed` / `StreakLost` **0 命中**；`Everguild.LiveOps` 里带 `ValidateSignal` 的只有 5 个 Trigger 类，与 streak 无关。刷新靠基类 `OnRefresh`（Slot 15，**本类不重写**）+ 自己 `ToFocus()`（Slot 7）拉。

---

## D 两态：`Streak Successful` / `Streak Failed` 互斥
**驱动字段**：`MainMenuMission.HasFailed`（**0x30**）+ `FailedValue`（**0x34**）—— 宿主是**任务对象**，不是窗口。
**唯一切换点 = `DailyStreakWindow.Open()`**（`__Open.c:38-62`，3 步开关 + 1 次消费）：① `streakFailedPanel`(0xC0)`.SetActive(HasFailed)` —— 出厂 **`T`**（§五 行 5 / pid `-1036943510412844373`）；② `if (HasFailed) streakLost`(0xB8)`.text = string.Format(Loc("Rewards Menu/DailyStreak/StreakLost"), FailedValue)`（键 = `ScriptString 0x42c9c58`；出厂文本 `'Streak lost: 10'`）；③ `streakSuccessPanel`(0xB0)`.SetActive(!HasFailed)` —— 出厂 **`F`**（§五 行 6 / pid `5135664151136051883`）；④ **`HasFailed = false`（一次性消费）** ⇒ **第二次 `Open()` 一定进「连胜态」**，随后 `header.Initialize(...)`。

**显隐组合**（出厂这一种；逐件 `activeSelf` 见 §五 表）：断签态 = `Streak Failed` 亮 + `Streak Successful` 暗（两句 TMP 文案、`Info`、`Reset Streak` 钮都在断签面板里）；连胜态反之（`Fill Line` / `Current Streak` / `Rewards Scroll View`（内含 `0x78` anchor）/ `Info` / `Timer`）。🔴 **`Timer`（= `0xA0 timerDisplay`）在 `Streak Successful` 面板里** ⇒ **断签态下倒计时看不见**（`Start` 照样挂了监听，但那个 GO 属于隐藏面板）。
**另一条强制切态的路（不改数据）**：`resetStreakButton` → `ResetStreakAfterFail()` 直接把失败面板关、成功面板开 —— **既不写 `HasFailed` 也不减 `currentValue`**，只是换画面。

🔴 **序列化里查不到的，本文件不下结论**：`HasFailed` / `FailedValue` 的**写入点**（谁判定「断签」、`FailedValue` 装的是断掉时的天数还是别的）—— `MainMenuMission.set_HasFailed` 只是编译器生成的属性 setter（RVA `0x69DD70`），**语义在调用方，本轮没找到那个调用方**。

---

## 本轮没查到的

1. **`MenuActions` 队列的 Drain 时机**（自动弹入口最后一环）—— **部分查到**：`MenuActions.Start()` 把 `StopMenuActions`/`ResumeMenuActions` 挂在 `EverguildSceneManager.SceneTransitionStarted/Ended` 上（`MenuActions__Start.c:9-19`）；Drain 在协程 `MenuActions.<Routine>d__10.MoveNext()` 里，最终在 `MenuActions.TryTriggerAction` 里**调用排队那个 Action**（`(**(code**)(lVar4+0x18))(*(lVar4+0x40), *(lVar4+0x28))` = delegate invoke，`MenuActions__TryTriggerAction.c:81,109`）。**还差**：`CanTrigger` / `CheckActiveMenus` 那几道闸的具体条件（`TryTriggerAction.c` 4.4 KB 没逐行读），以及队列的优先级/重复规则。
2. **引用表里 10 / 12 / 20 / 21 各自对应哪个组件**。还差：`MainMenuMission`（`LiveOpsEvent`）那份**事件配置资产**里 `(Int32Enum, 引用)` 表的内容 —— 表在事件配置资产上，**不在 `Daily Streak Popup` prefab 里**。
3. **`MissionEvent.TryCollect` 的 payload 与云函数名**（handler = `GenericCloudScriptHandler` + `CloudHandle.UpdateMissionPoints(907)` 已确认）。还差：补该 RVA（`0x102f400`）的反编译产物，或读 `PlayfabWrapper.GenericCloudScriptHandler` 的参数字典构造。
4. **`LiveOpsEventWindow<T>.Close()` / `SetupData()` / `OnRefresh()` 的体**（产物缺失）。还差：补反编译，或运行时 dump 泛型实例的 vtable 目标。
5. **`MainMenuMission.set_HasFailed` / `set_FailedValue` 的调用方**。还差：补反编译后全库搜这两个 setter 的调用点，或在「真断签过的账号」上运行时 dump 一次。
6. **`DailyStreakItemContainer.Refresh` 里 `currentDrawer` 那个 vt@0x198 虚方法的名字**（槽号 = `(0x198 − 0x138)/16 = 6`）。还差：`il2cpp.h` 里 `ItemDrawer` 的 `__VTable._6_`。
7. **出厂 `scl=(1.2,1.2)` 的模板条目与运行时 ×1.2 会不会叠成 1.44**。还差：`Daily Streak Reward Popup Entry` 的 `RT.m_Father = 0`（**独立 prefab 根**）在 `Daily Streak Popup` 树里的实例化位置，以及 `Rewards Content` 的父链是否真在 `Streak Successful` 里。
