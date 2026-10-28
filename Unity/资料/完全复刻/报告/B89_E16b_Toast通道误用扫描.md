# B89-E16b · Toast 通道误用全仓扫描（2026-10-28）

> 起因 = `B89-E16` 在个人资料线查到：原版 `Achievement.OnReachMilestone` 走
> `ToastNotificationController.ScheduleNotification`，而我们接的是 `MessageToast`（= `UIMessageController`）。
> 本批按**行为**把全仓「显示一句话」的调用点扫一遍，判定「原版走的是哪套 / 我们接的是哪套」。

---

## §1 两条（其实是三条）通道的语义对照

| # | 原版类 | 我们仓的类 | 主管什么 | 视觉 / 时序 | 判据 |
|---|---|---|---|---|---|
| ① | **`ToastNotificationController`** | `Shell/ToastNotifications.cs` | 任务进度 / 收藏经验 / 收藏等级 / **成就** 这一族**带图标的卡片**；`notificationTypes` 四档 → 四个 prefab（`Toast` / `Toast Collection Get XP` / `Toast Collection Level Up` / `Mission Toast Notification`） | 右下角**堆叠**，入场 1.0s → 停 3.0s → 出场 1.0s，**自己消失**；`ShowNotification` = 立刻，`ScheduleNotification` = 推迟一拍（`MenuActions.QueueAction`） | 文件头 ①②③；`ToastNotificationController__{Show,Schedule,ShowInternal}Notification.c` |
| ② | **`UIMessageController`** | `Shell/MessageToast.cs`（宿主）+ `Battle/ErrorMessageBanner.cs`（实现；壳侧取 `MenuPreset`） | 屏幕**上方**一条**横幅**：错误（`ShowError` → 红 `(0.8,0,0,1)`）/ 普通消息（`ShowMessage` → 白 `messageColor`）。调用族 = 卡组编辑拦截、删档失败、分享成功、疲劳扣血、决斗缺卡组… | 5 条一池**轮转**，入场 0.15s / 停 2.0s / 淡出 0.25s，**自己消失**，**没有按钮** | `MessageToast.cs` 文件头 ①②③④；`UIMessageController__{ShowMessage,ShowError}.c` |
| ③ | `WindowsManager.ShowPopUp` / `ShowMessagePopUp`（`PopUpGameWindow` = prefab `MessagePopupWindow`） | `Shell/WindowsManager.cs` / `Shell/PopUpGameWindow.cs` | **模态**消息框（一钮/两钮，**要玩家点**）；`ErrorPopupWindowController.ShowErrorMessage(CustomError)` 的载体 | 压暗 + 居中窗 + OK 钮，**不点不走** | `ErrorPopupWindowController.cs` 文件头 ⑥ |

🔴 **③ 不是 ② 的等价物。** 本批就抓到两处「原版走 ②、我们走了 ③」—— 玩家可见差别是
**「自己消失」vs「必须点一下」**，不是实现风格。

---

## §2 全调用点表

**我们侧一共 16 个「显示一句话」的生产调用点**（下表按链路归成 15 行；⛔ 不含自检宿主 `Editor/*.cs` 的夹具调用）。

| # | 调用点 `文件:行号` | 我们走的通道 | 原版该走哪套 | 判据出处 | 结论 |
|---|---|---|---|---|---|
| 1 | `Shell/AchievementMilestoneToast.cs:116` | ① `ToastNotifications.Schedule(Achievement, …)` | ① `ScheduleNotification` | `decomp_full/Everguild.LiveOps.Achievement__OnReachMilestone.c` | ✅ 一致（B89-E16 本波刚从 ② 改过来） |
| 2 | `Shell/DailyData.cs:2493` | ① `ShowMission(idx)` → `ShowNotification` | ① 任务进度（`type = 3`）、**立刻** | `Missions__ShowToastNotification.c:98-102`（ctor 实参里那个 `3` = `MissionProgress`；签名 = `(text, icon, onClick, data, controllerID, notificationType)`） | ✅ 一致 |
| 3 | `Shell/PlayerProfileState.cs:279` | ① `ShowCollectionLevelUp`（`type = 2`） | ① 升级（`type = 2`）、`ShowNotification` | `PlayerDataManager__AddCollectorXp.c:64-68` | ✅ 一致 |
| 4 | `Shell/PlayerProfileState.cs:281` | ① `ShowGrantedXpToast`（`type = 1`） | ① 加 XP（`type = 1`）、`ShowNotification` | 同上 `:89-93` | ✅ 一致 |
| 5 | `Shell/PlayerProfileState.cs:304` | ① `ShowGrantedXpToast`（`type = 1`） | ① `PlayerDataManager.ShowGrantedXpToast` | `PlayerDataManager__ShowGrantedXpToast.c:28-32` | ✅ 一致 |
| 6 | `Shell/DeckInfoPopup.cs:2546` | ② `MessageToast.Show`（白） | ② `ShowMessage(键, localize: 1)` | `DeckInfoPopup__ShareDeck.c:22` | ✅ 一致（A1050 已改对） |
| 7 | `Shell/DeckInfoPopup.cs:2730` | ② `MessageToast.Show`（白） | **原版无此句**（它发一条聊天消息 `ChatGlobalManager.ShareDeck`，P3） | `DeckInfoPopup__ShareDeckOnChat.c` | ⚠️ 通道无误；**文案是我方自拟**的「发送口不在」说明（未打 `🔶`，见 §5·4） |
| 8 | `Shell/ExpansionPassEventWindow.cs:2455` | ② `MessageToast.ShowError` | ② `ShowError(键, localize: 1)` | `ExpansionPassProgressClaimButtonController__BuyPremiumClick.c:14` | ✅ 一致 |
| 9 | `Shell/ExpansionPassEventWindow.cs:2006` / `:2036` | ② `MessageToast.ShowError` | 判不清（原版购买失败链 5 个 `.c` 里**0 个**消息调用） | 见 §4·1 | 判不清 ⇒ **不动** |
| 10 | `Deck/DeckRuntime.cs:4067`（起拖拦截）· `:8284`（点卡加入） → `SayError` → `ErrorMessageBanner.ShowShellError` → `MessageToast.ShowError` | ② 红横幅 | ② `ShowError` | `DeckEditingWindow__CheckCardDrag.c:92` · `DeckEditingWindow__OnCardClick.c:27` · `DeckEditingPanel__AddCard.c:26` | ✅ 一致 |
| 11 | `Battle/BattleDriver.cs:30788` `_errorBanner.ShowError(...)`（**战斗那颗**，`BattlePreset`） | ② 红横幅 | ② `ShowError` | `BattleManager._ResolveFatigue_d__587__MoveNext.c:129` | ✅ 一致 |
| 12 | **`Shell/DuelPopupWindow.cs:319`** → `ErrorPopupWindowController.ShowError` → `WindowsManager.ShowPopUp` | ③ **模态弹窗 + Retry 钮** | ② `ShowError("MainMenu/General/DontHaveDeckForGameMode", 1)`，**瞬时横幅** | `DuelPopupWindow__CanPlay.c:24` | ❌ **通道误用**（§3·1） |
| 13 | **`Shell/DeckInfoPopup.cs:2210`** → `Manager.ShowPopUp(msg, OK, null)` | ③ **模态弹窗 + OK 钮** | ② `ShowError(Format("MenuDeck/CantImportDeck", 阵营名), 0)`，**瞬时横幅** | `DeckInfoPopup__EditDeck.c:21-45` | ❌ **通道误用**（§3·3） |
| 14 | `Shell/DeckInfoPopup.cs:2442`（删卡组失败） | **什么都不上屏**（只 `Debug.LogWarning`） | ② `ShowError("MenuDeck/Error/FailedToDelete", 1)` | `DeckInfoPopup___DeleteDeck_b__27_3.c:18` | ❌ **缺显示**（通道本身对，见 §3·4） |
| 15 | `Shell/ProfileTab.cs:1582`（点 Player ID） | **什么都不上屏**（连剪贴板都没写） | ② `ShowMessage("Settings/CopiedToClipboard", 1)`（白档） | `ProfileTab.__c__DisplayClass11_0___Initialize_b__0.c:23` | ❌ **缺**（数据侧 P3，见 §3·5） |

**原版侧一共 15 个对应调用点**：`ToastNotificationController` 5 个（Achievement / RewardEvent / Missions /
`AddCollectorXp` / `ShowGrantedXpToast`·`ShowNewLevelToast`）+ `UIMessageController` 10 个
（`DeckEditingPanel.AddCard` · `DeckEditingWindow.{OnCardClick,CheckCardDrag}` · `DeckInfoPopup.{EditDeck,ShareDeck,<DeleteDeck>b__27_3}` ·
`DuelPopupWindow.CanPlay` · `ExpansionPassProgressClaimButtonController.BuyPremiumClick` · `ProfileTab.<Initialize>b__11_0` ·
`BattleManager._ResolveFatigue`）。
> 取数方法：`grep -ln "ToastNotificationController__Schedule\|…__ShowNotification" d:/2/tools/decomp_full/*.c` 与
> `grep -ln "UIMessageController" d:/2/tools/decomp_full/*.c`（各自剔除控制器自己的文件）。

⚠️ **两个原版入口没有生产路径**：`ToastNotificationController__Test*`（4 个调试按钮）—— 不列。

---

## §3 改了哪些

### §3·0 本批我改了 0 个 `.cs` —— 🔴 **2026-10-28 转岗后已由 `B89-E16c` 全部落地**

三处**必须改**的调用点（§2 的 12 / 13，以及 §3·1 的实现体）当时**全部落在我的白名单之外**
（`Shell/DuelPopupWindow.cs` · `Shell/ErrorPopupWindowController.cs` · `Shell/DeckInfoPopup.cs`），
按简报「若某个必须改的调用点在别人的文件里 ⇒ 写成施工指令，⛔ 别抢」写成了施工指令。
**调度台随即转岗授权** ⇒ **§3·1 ~ §3·5 已全部执行完毕**，现状一律以
**`报告/B89_E16c_通道误用修正.md`** 为准（含行号订正：`DeckInfoPopup` 那一处真身是 **:2209**，本文件 §3·3 写的 2210 错位一格）。
`Shell/AchievementMilestoneToast.cs` 的那一处**已由 B89-E16 改好**（现在走 ①），本批只复核、未再动。

### §3·1 【施工指令·必修】`Shell/DuelPopupWindow.cs:319`

```csharp
// 改前
ErrorPopupWindowController.ShowError("MainMenu/General/DontHaveDeckForGameMode", true);
// 改后
MessageToast.ShowError("MainMenu/General/DontHaveDeckForGameMode", true);
```

* 判据 = `decomp_full/DuelPopupWindow__CanPlay.c:24` —— `UIMessageController__ShowError(instance, key, 1, 0)`
  （第 3 实参 `1` = `localize`；尾参 `0` 是 IL2CPP `MethodInfo*`）。
* 词条 `MainMenu/General/DontHaveDeckForGameMode` 在官方 13 语言表里现读**命中** ⇒ 传 `localize: true` 即可。
* **观测口**：从主菜单开决斗窗、把所有预组卡组删空（或临时把 `CanPlay` 的查找表清空）⇒ 点模式钮，
  应当出现**屏幕上方那条红横幅**（`MessageToast.Instance.ShowCount` +1，`LastShownText` = 官方值），
  **不再**出现带 Retry 钮的模态窗（`WindowsManager` 的 `ShowPopUp` 计数不变）。
* ⚠️ 顺带订正 `DuelPopupWindow.cs:310` 那句注释「⇒ 交给 `ErrorPopupWindowController.ShowError`（同一条消息弹窗）」——
  它把「换载体」写成了等价，是错的。

### §3·2 【施工指令·必修】`Shell/ErrorPopupWindowController.cs:127-134` 的 `ShowError(string, bool)`

* 这是一个**我们自建、原版没有**的方法（原版同名类里只有三个 `ShowErrorMessage` 重载）。
  它的 doc（`:122-126`）写着「我们**没有**那套 toast 组件 ⇒ 等价物 = 消息弹窗」「**这不是缺功能**，是同一件事换了载体」
  —— **两句都不成立**：`Shell/MessageToast.cs`（2026-10-09 起）就是 `UIMessageController` 的宿主，
  `DeckRuntime.SayError` / `ExpansionPassEventWindow.BuyPremiumClick` 已经走它。
* 二选一（推荐 ①）：
  1. **改成一行转调**：`public static void ShowError(string termOrText, bool localize) { MessageToast.ShowError(termOrText, localize); }`
     （保留签名 ⇒ 0 调用点受影响；删掉 `WindowsManager.ShowPopUp` 那一支与 `Loc` 那两行自算）。
     返回类型若要保留 `void` 就照上；`MessageToast.ShowError` 返 `bool`，可忽略。
  2. 删除该方法，调用点（唯一 = `DuelPopupWindow.cs:319`，本批 §3·1 已改走 `MessageToast`）随之消失。
* ⛔ **`ShowErrorMessage(CustomError, …)` 三兄弟不要动** —— 原版它们就是 `WindowsManager.ShowPopUp`（模态），我们逐句对齐过。

### §3·3 【施工指令·必修】`Shell/DeckInfoPopup.cs:2210`

```csharp
// 改前
if (Manager != null) Manager.ShowPopUp(msg, Loc.T("MainMenu/General/OK"), null);
// 改后
MessageToast.ShowError(msg, false);      // msg 上一行已本地化（CantImportDeckMessage）
```

* 判据 = `decomp_full/DeckInfoPopup__EditDeck.c:21-45`：`Format(GetTranslation("MenuDeck/CantImportDeck"), CardArmyToString(army))`
  → `UIMessageController__ShowError(instance, 串, 0, 0)`（`localize = 0` = 已经是明文）→ `return`（**不进编辑器**）。
  原版**没有**按钮、没有 `ShowPopUp`。
* 同段注释（`:2207-2208`「弹窗唯一那颗钮走词条 … 是 `OK` **不是** `Ok`」）随之作废，一并改成横幅口径。
* ⚠️ 本 build `state == Import` **无调用点**（原版那条是 `ChatMessageUI.OnMessageClicked`）⇒ 玩家面不可达；
  按铁律 11 仍要改（判据清楚、成本 1 行）。
* **观测口**：自检里把 `State = DeckInfoState.Import` 造出来、点 `Edit Deck` ⇒ `MessageToast.Instance.LastRawText` = `CantImportDeckMessage(...)`、
  `ShowCount` +1，且**没有**新模态窗（`WindowsManager` 的窗计数不变）、**不**进编辑器。

### §3·4 【施工指令·必修】`Shell/DeckInfoPopup.cs:2442`（删卡组失败支）

```csharp
// 改前
else Debug.LogWarning("[DeckInfo] 删卡组失败：" + CollectionData.LastDeleteError);
// 改后（保留原日志，另起一行上屏；⛔ 键别自拟）
else { MessageToast.ShowError("MenuDeck/Error/FailedToDelete", true);
       Debug.LogWarning("[DeckInfo] 删卡组失败：" + CollectionData.LastDeleteError); }
```

* 键的判据：`DeckInfoPopup___DeleteDeck_b__27_3.c:18` 的 `DAT_1842cf1f0` → `0x42CF1F0` =
  **`MenuDeck/Error/FailedToDelete`**（`d:/2/tools/il2cpp_out/stringliteral.json` 现读）；
  该键在 `Resources/official_loc_13lang.json` 现读**命中**。
* 原版同一支还会把卡组道具退回库存（`InventoryManager__Add(...)`）—— 本地没有「卡组道具」这一层库存 ⇒ **另记**（§5·2）。
* **观测口**：把 `CollectionData.DeleteDeck` 打成恒 false ⇒ 点删除并确认 ⇒ 屏幕上方出红横幅一条，同时日志仍在。

### §3·5 【施工指令·记录口径】`Shell/ProfileTab.cs:1582`（点 Player ID）

* 原版（`ProfileTab.__c__DisplayClass11_0___Initialize_b__0.c:20-23`）= `GUIUtility.systemCopyBuffer = PlayfabId` +
  `UIMessageController__ShowMessage("Settings/CopiedToClipboard", localize: 1)`（**白档**、不是红）。
* 我们这一支只有一行 `Debug.Log`（连剪贴板都不写）—— 因为 **`PlayfabId` 在服务器（P3）**，本地没有可复制的串。
  ⇒ 数据缺口照实记；**通道口径现在就写死**：将来拿到 id 时走 `MessageToast.Show("Settings/CopiedToClipboard", true)`，
  ⛔ **不是** `ShowError`（原版是白档）、⛔ **不是** `ShowPopUp`（原版不点也能消失）。
* 词条 `Settings/CopiedToClipboard` 在官方表里现读**命中**。

---

## §4 判不清的（⛔ 没动）

1. **`Shell/ExpansionPassEventWindow.cs:2006` / `:2036`**（`OnRewardCollectError` / `OnLevelUpError` → `MessageToast.ShowError`）
   —— 这两支由我们**本地 liveops 桩**触发，原版对应链（`ExpansionPassEvent.PurchaseLevelUp` /
   `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick` 的失败回调）里
   `grep ShowError|ShowMessage|UIMessage|Toast` **0 命中**（5 个 `.c` 逐个查过）⇒ 原版这一处的**失败怎么上屏，判据为空**。
   **结论：不动**（现在的 `MessageToast.ShowError` 是我们挑的载体，属「判据空 ⇒ 按常理补」，应打 `🔶`——见 §5·4）。
2. **`Shell/DeckInfoPopup.cs:2730`**（分享到聊天发不出去时的说明）—— 原版这一跳是发聊天消息（P3），
   **它自己一个字都不弹**。我们弹的这句是自拟文案；通道（白档横幅）本身没冲突，但文案来源要标 `🔶`。
3. **`Shell/ErrorPopupWindowController.ShowError` 是否还有别的调用者**：全仓现读只有 `DuelPopupWindow.cs:319` 一处（生产）
   —— 已核 `grep -rn "ErrorPopupWindowController.ShowError"`。若将来有人再用，按 §3·2 改完就不会再误用。

---

## §5 顺手发现（不在本批范围内，但该记）

1. **`Everguild.LiveOps.RewardEvent.HandleRewardDisplay`（逐条奖励排飘窗）我们 0 调用点。**
   原版 = `for each reward ⇒ ToastNotificationContext(text, …) ⇒ ScheduleNotification`（`…HandleRewardDisplay.c:39-43`）。
   实参位序（ctor 签名 = `(text, icon, onClick, data, controllerID, notificationType)`，判据 =
   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/ToastNotificationContext.cs:18`）现读 = `(text, 0, 0, 奖励对象, param_1 上的 getter, 0)`
   ⇒ **图不是走 `Icon` 字段**（实参是 0）、`type = 0`（= Achievement 档 / 默认 `Toast` 模板），
   分流靠 `controllerID` + `RegisterNotificationHandler`（`__TryShowNotification.c` 那两分支）。
   我们 `ToastNotifications.Schedule` 的**唯一生产调用者**是成就跨档（`AchievementMilestoneToast.cs:116`）
   ⇒ 账 `日常_奖励.tsv:662` 的落点其实是一个**没人调的 API**，已在补丁里改写 判据要点 与 差异。
2. **删卡组的道具退回**（`DeckInfoPopup___DeleteDeck_b__27_3.c` 后半段 `InventoryManager__Add`）本地无对应层，另行记账。
3. **账里有两句错记录**（导致这次误用）：「我们**没有**那套 toast 组件」—— 出现在
   `账/菜单_外壳.tsv:134`（DuelPopupWindow 行）与 `:157`（ErrorPopupWindowController 行）的 `差异` 列。
   两句都在补丁里**就地订正**（铁律 5）。
4. **`🔶 自己设计` 打标候选**：`ExpansionPassEventWindow.cs:2006`/`:2036`（载体我们挑的，判据为空）与
   `DeckInfoPopup.cs:2730`（整句文案我们写的）—— 按铁律 18 该打标；不在本批白名单，只记。
5. **`Shell/MessageToast.cs` 文件头的分工段已是对的**（「两位在原版是两个类、两个节点、两套时序，⛔ 别合并」），
   本批全仓扫下来**没有一处**把两条通道合并/互相顶替的实现 —— 误用只出在「③ 模态弹窗冒充 ②」这一个方向上。
