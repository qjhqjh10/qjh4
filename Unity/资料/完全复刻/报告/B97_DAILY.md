# B97_DAILY · `账/日常_奖励.tsv` 37 条 B 的处置（第九十七会话 · 2026-10-29）

> 执行代理 `B97_DAILY`（有写权限）。**只写这三个文件**：`CardPresentation/Shell/ToastNotifications.cs`（1 行接线）·
> 新建 `CardPresentation/Editor/B97_DAILY_Assert.cs` · `CardPresentation/Editor/ShellScene.cs:11023`（同一行追加 1 个调用，行数零变化）
> ＋ 本报告 ＋ `账/_parts/B97_DAILY.tsv`。⛔ 未动 `Editor/RuleEngineTest.cs` · 账主表 · 正本 · `Pinned*` ·
> `gen_cards_engine.py` · git · Unity 批处理 · 别人名下的文件（`GachaEventWindow/RankedTab/FactionScore/UnitState/Enchantment/
> TutorialScript*/SimpleAI/BattleContext/CampaignScriptRunner/TransformUnit_B94B12/RuleEngineTest*/B94B11_Assert/B97_*_Assert/
> EffectResolver/RuleCore`）。
> 判据来源 = `资料/完全复刻/判据/B96_日常53_分诊.md`（先读）＋ 本轮**现读**的源码 / prefab / 反编译。

---

## 一、结论（37 条 B 的去向）

| 档 | 条数 | 账行 | 本轮做了什么 |
|---|---|---|---|
| **判 A（真做完）** | **2** | `745` · `510` | `745` 补 1 行接线；`510` 把最后那格判据读出来（prefab 现读） |
| **判 A（休眠口径）** | **2** | `492` · `728` | 代码侧零缺口、残差全在服务端数据 ⇒ 按「休眠 A」记（同 `PlayerManager.RefillCustomMana` 那类） |
| **维持 B（判据/落点齐、缺一跳但**不在本代理文件**）** | 8 | `39` · `272` · `676` · `681` · `621` · `622` · `623` · `624` | 逐条现核残差 + 给出**可粘贴补丁/落点**（§四） |
| **维持 B（判据空，给反证）** | 6 | `159` · `160` · `394` · `732` · `733` · `734` | 独立复核「视觉件本机确实没有」（§五） |
| **维持 B（结构差，等调度台拍）** | 3 | `55` · `107` · `474` | 口径已齐，只差拍板 |
| **维持 B（REMOTE，留调度台）** | 16 | 见 §六 | 零改动、⛔ 未发明行为 |

> 账里 2 条 **C**（`155 EndBattleRewardHolder.SetReward` · `515 Missions.GetPointsPerArmyOnDailyLogin`）**不在本批 37 条内**，未动。

**账补丁**：`账/_parts/B97_DAILY.tsv`（11 列 · 首行表头 · 37 行）。
`merge_ledger_parts.py --dry --only B97_DAILY` ⇒ **更新 37 · 新增 0 · 无变化 0**；`check_patch_conflicts.py` ⇒ **无跨补丁重复键**。

---

## 二、判 A 的 4 条（逐条证据）

### 1. `日常:745` `ToastNotificationMission.Initialize(object obj)` → **A**（真的动了代码）

* 残差一直是**一跳接线**：`SupportMethods.ModifyLocalization` 早在（`Core/SupportMethods.cs:347`，B96_WB 加在类尾），
  但生产入口 `Shell/ToastNotifications.cs:861` 还是 `c.Text = d.ProgressText`（直传）。
* 原版那一句（`decomp_full/ToastNotificationMission__Initialize.c`）：
  `text.text = SupportMethods.ModifyLocalization(d.progress.GetLocalizedDescription());`
* 本轮改动（**同一行替换，行数零变化**，1561 行 → 1561 行）：
  `c.Text = SupportMethods.ModifyLocalization(d.ProgressText);`
  同处两段已过期的注释一并就地订正（铁律 5：「我们没有等价物 ⇒ 直传」已不成立）。
* 断言：`Editor/B97_DAILY_Assert.cs ②` —— **IL 层**数「`InitializeMissionToast` 方法体里 `call SupportMethods.ModifyLocalization` 的次数 == 1」
  ⇒ 把 `:861` 改回直传必红。`①` 另锁「`ModifyLocalization` 必须转调 `CardIcons.RewriteMarks`」（⛔ 不许抄第二份四支）＋ `null/"" ⇒ ""`（原版 `.c:489-493`）。
* 接线：已接在 `Editor/ShellScene.cs:11023`（合计行之前，同一行追加 ⇒ 行数零变化）。

### 2. `日常:510` `MissionTimerDisplay.Update()` → **A**（判据补齐，零代码改动）

唯一挂着的那格「周常那颗 `displayRule` 判不了」**现读解出**。判据 = **AssetRipper 导出工程**（`d:/2/tools/assetripper_out/ExportedProject`，
带真 pathID 的 Unity YAML；`assets_full` 的 GameObject json 不带 pathID，所以之前判不了）：

| prefab | `MissionTimerDisplay` 实例数 | `displayRule` |
|---|---|---|
| `Daily Login Bonus Container` | 1 | `2` |
| `Daily Mission Container` | 1 | `2` |
| `Daily Skulls Mission Container{,-Small,-Variant Expansion Pass}` | 各 1 | `-1` / `-1` / `-1` |
| `Expansion Pass Missions Tab` | 2 | `2` · `-1` |
| `Mission Container Vertical` | 1 | `-1` |
| `Missions Tab` | 6 | `2`（登录卡）· `-1`（骷髅卡）· `2`·`2`·`2`（每日行 3 颗）· **`-1`（`Missions Tab/Weekly Mission Holder/Weekly Mission/background/footer/TimerHolder/Timer`）** |
| `Rewards Base Submenu Variant` | 6 | 同上分布 |
| `Weekly Mission Container` | 1 | `-1` |
| **合计** | **21** | **11×`2` + 10×`-1`**（与 `B96_WA` 的 21 实例/11×2/10×-1 完全吻合） |

* `DisplayRule` 是 `[Flags]`：`WhenActive = 1` · `WhenComplete = 2`（`Shell/MissionInfoDisplays.cs:74`；原版 `MissionInfoDisplay.cs:5-9`）
  ⇒ **`-1` = 每一位都置 ⇒ 恒显示**。
* 我们的实现：周常 `Timer` **无条件建**（`MissionsTab.BuildWeekly:1528`）⇒ 与 `-1` **逐位一致**；
  每日行 / 登录卡 / 骷髅卡按 `IsComplete` 分档（B94_B4 修过的那三处）⇒ 与 `2` 一致。
* 断言 = `Editor/B97_DAILY_Assert.cs ③`：`-1` 两态都显示 · `2` 只在已领取 · `0` 恒藏（原版 `.c` 第二支）＋
  `Description(false,false) != Description(false,true)`（两条模板不同）＋ `Description(true,_) == "{0}"`。
* ⚠️ 那条 `willEndAt==0 && completedAt==0 ⇒ GetNextRefreshTime()` 支**我们不需要**（三处目标时刻由调用点给），
  槽号仍未坐实——**不影响本行判 A**（该支恒不命中）。

### 3. `日常:492` `MissionHeaderDisplay.Setup(IMission, MissionChallengeProgress)` → **A（休眠口径）**

两条残差**都在数据侧**（`IMission.get_IsPremium`、每任务 `Description` 词条 = 服务端下发），代码侧零缺口：
premium 渐变（六键按 prefab 落地 `MissionsTab.cs:1637`）· `isPremium` 形参（`:1538`，caller `:390` 传 `false` =
原版「非 premium 走默认渐变」）· tooltip 节点身份（就是 `info` 那颗，B94_B4 已订正）。本地任务 **0 条**带 `IsPremium`
⇒ 该支恒不命中，行为与原版在同样数据下**逐位一致**。⛔ 别为了让分支「可达」自造 `IsPremium` 数据。

### 4. `日常:728` `SkirmishEventWindow.RefreshScore()` → **A（休眠口径）**

投影纯函数已 1:1（`HasMilestoneToCollect:936` / `MilestoneStates:955`）；数据源 FastMode 活动在本机 **13 份活动 SO 里 0 份**
⇒ 0 条数据走这条路，与原版「无活动 ⇒ 什么都不显示」一致。⚠️ 原版 `collectedRewards` = `*(int*)(ev+0x70)`，⛔ 别写死 0。

---

## 三、维持 B 但**残差已经具体化**的 8 条（每条都给了落点/补丁）

| 账行 | 现在的残差（本轮现核） | 卡在哪 |
|---|---|---|
| `39` `ChallengeLogMgr.LogPlayTactic` | ① 共享 tag-mask 落点（§4.1 ⇒ 只能 B）；② `CardPlayedSignal` 缺 **cost** 格 | ② 的**发点**在 `RuleEngine/Core/EffectResolver.cs`（`PlayTacticCore` 的 `AddCardPlayed`）＝ **`B97_ER` 名下，⛔ 未动**。补丁形状见 §四·1（形状需调度台拍） |
| `272` `FastModeBaseEvent.OnMatchProcessSuccess` | `ArmyStats` 支已在（`:343`）；**boosts 支无落点**：消费口是 `b.<+0x428>`，属**真身** `RankedScoreBoostEvent`（B11 排位加成线），本文件的 `:109` 是 🔶 最小替身（无 id、无消费口） | B11 真身 + 云脚本 `0x180`=P3 |
| `676` `RewardService.Collect` | `false` 分支实现齐（`DailyData.cs:2313`）；6 个既有 caller **全对应原版 `showAnimation:true`**（`Missions.cs:233/1345`）⇒ **忠实、不是缺陷** | 原版 `false`-caller 全在别域（`CardServices/ContainerService/ContainerOfferEvent/ExpansionPassEvent/PlayerDataManager.ProcessMatchResult·UnpackInventory/UpgradePanel`），本工程**那些域没有「发奖不开窗」的生产流**（`ExpansionPassEvent.cs:492/553` 只有注释）⇒ 无真 caller。⛔ 不在 `DailyData` 造假 caller |
| `681` `RewardService.CollectSingleReward` | ④ 联盟奖杯那支**判据本轮坐实**（见 §四·2）；**残差换成了 ⑤**：末尾统一跳 `item.HandleOnCollect(quantity)`（+0x1b8 = Slot 8）要**按类型分派的物品层**，我们只有 `CollectItemRewardById`（非货币一律出声） | 物品层缺口，**不是判据缺口** |
| `621/622/623/624` `RaidOrArmyReleaseMenuContainer.*` | 宿主 = `MainMenuRuntime.BuildModeCardForCard`（`:4050-4067`），今天只建 卡图/TextDarkening/标题/Border/倒计时，**没有 `Event Progression` 子树**；几何**已读全**（见 §四·3） | 卡在「根 535×848.8 vs 卡可见 535×414.4 的**坐标帧**」：`Event Progression` 的 `ap(0,122)` 从根底算会落到可见裁切之外，**无 live 验证判不了原版运行时是否换过帧** ⇒ ⛔ 不猜坐标 |

---

## 四、可粘贴补丁 / 落点（交调度台转派）

### 4·1 账 `39` 的 cost 格（落点 `RuleEngine/Core/EffectResolver.cs` = `B97_ER` 名下）

原版：`ChallengeLogMgr.LogPlayTactic` 发一次 `CardPlayedSignal`，**带 `EntityScript.get_CurrentCost`**（`decomp_full/ChallengeLogMgr__LogPlayTactic.c:370-408`）。
我们的事件流是「tag-mask 记账」、没有 signal 对象 ⇒ 等价做法 = 给记账口补一格 cost：

```csharp
// RuleEngine/Core/AchievementTally.cs（类尾，紧跟 TacticsPlayedOf 之后）
// 🆕 2026-10-29（B97_DAILY · 账 日常_奖励:39 残差 ②）：原版 `CardPlayedSignal` 每次打出一张卡都带 **cost**
//   （`EntityScript.get_CurrentCost`）。我们的记账口原来只有 (seat, tagMask) ⇒ 成本这一维丢了。
//   ⚠️ 形状待调度台拍：原版是「每次一条 signal」，我们这里有两档可选 ——
//     (a) 只留每方最后一笔 `int[2]`； (b) 每方一个 `List<int>` 全量。
public readonly int[] LastCardPlayedCost = new int[2];
public void AddCardPlayed(int seat, int tagMask, int cost)   // ← 扩既有签名（调用点只有 PlayTacticCore 一处）
{
    // …原体…
    if (SeatOk(seat)) LastCardPlayedCost[seat] = cost;
}
```
发点：`EffectResolver.PlayTacticCore` 里 `Tally.AddCardPlayed(p, RuleCore.CardPlayedTagMask(card, inst))` 那一句加上「实付 cost」
（该文件里的实付值已有旁证：`RuleCore.EnergySpentTotal` 记的就是同一个 `paid`）。

### 4·2 账 `681` 的 ④ 联盟奖杯支（判据逐字段坐实，可直接粘）

判据（`decomp_full/RewardService__CollectSingleReward.c:110-127`）：

```
lVar9 = AlliancesManager.Instance;  if (lVar9 == 0 || *(lVar9+0x28) == 0) → NRE 路径
uVar5 = *(*(lVar9+0x28) + 0x38);                                   // trophyController.trophies
lVar4 = FirstOrDefault(uVar5, x => x.Data.GetID() == 奖励.GetID()); // 按 id 命中
if (lVar4 != 0) {
    *(int*)(*(lVar4+0x20) + 0x38) = 1;          // save.groupScore  = 1
    *(int*)(*(lVar4+0x20) + 0x3c) = 1;          // save.playerScore = 1
    *(int*)(*(lVar4+0x20) + 0x40) = quantity;   // save.tier        = quantity
}
// 之后**仍**走 IL_066b：item.HandleOnCollect(quantity)
```
偏移与 `AllianceTrophySave : ChallengeProgress` 的字段序**完全吻合**（`collectedRewards 0x10 / premiumCollectedRewards 0x14 /
synced 0x18 / customData 0x20 / resetProgress 0x28 / id 0x30 / groupScore 0x38 / playerScore 0x3c / tier 0x40`，见
`cpp2il/out_cpp2il_dev_ilspy/{ChallengeProgress,AllianceTrophySave}.cs`）⇒ 三格语义**不是猜的**。

落点已存在（`Shell/AllianceTrophyModel.cs`，`Find(id)` + `_states`；今天只有 `GroupScore/SavedTier` 两格）：

```csharp
// Shell/AllianceTrophyModel.cs —— `AllianceTrophyState` 追加一格（结构体追加字段，源码兼容）
public int PlayerScore;      // 原版 `AllianceTrophySave.playerScore(+0x3c)`

// 类尾追加
/// <summary>= 原版 `RewardService.CollectSingleReward` 里「奖励是联盟奖杯 ⇒ 写三格」
/// （`decomp_full/RewardService__CollectSingleReward.c:120-126`：`groupScore=1 · playerScore=1 · tier=quantity`）。
/// ⚠️ 原版是**找到才写**（`FirstOrDefault` 命中）；本类的「奖杯表」默认空（`WhyNoDefs`）⇒ 与「公会里没有这件奖杯」同档。
public static bool MarkCollected(string trophyId, int quantity)
{
    if (string.IsNullOrEmpty(trophyId)) return false;
    var def = Find(trophyId);
    AllianceTrophyState st = StateOf(def);
    st.GroupScore = 1;                      // 原版 `.c:120` 写死 1（⛔ 不是 +1）
    st.PlayerScore = 1;                     // 原版 `.c:122` 写死 1
    st.SavedTier = quantity;                // 原版 `.c:126` = 奖励数量（照抄，⛔ 别改成 CurrentTier）
    if (def != null && def.Id != null) _states[def.Id] = st;
    return true;
}
```
接线（`CardPresentation/Shell/Inventory.cs:2087` 那段，**在** `CollectItemRewardById` **之前**，因为原版写完后**仍**走 `HandleOnCollect`）：
```csharp
// ④ 联盟奖杯（原版 `plVar7 is AllianceTrophy ⇒ +0x1e8(quantity)`；体见 .c:110-127）
if (ItemDrawer.Spec(reward.Id, null, null).Type == "AllianceTrophyData")
    AllianceTrophyModel.MarkCollected(reward.Id, reward.Quantity);   // 未命中定义表 ⇒ 什么都不写（与原版「找不到」同档）
```
⚠️ 分类器用的是**物品表**里的 SO 类型名 `AllianceTrophyData`（`Shell/ItemDrawer.cs:424` 的 `ItemTypeSets`），
原版判的是**运行时类型** `AllianceTrophy` —— 两者是同一族的「物品 SO 类」与「实例类」，如实记在这条补丁里。

### 4·3 账 `621-624` 的落点（几何已读全，只差坐标帧）

宿主 = `Shell/MainMenuRuntime.cs` 的 `BuildModeCardForCard`（`:4050-4067`，`BuildModeCard(...)` 之后）。
prefab `Base Game Mode Container 1x2 Raid or Army Release`（**现读**，AssetRipper YAML）：

```
Event Progression   anchor(0.5,0) pivot(0.5,0)  ap(0,122)  size 386.781×59.39
├ ProgressBar       anchor(0.5,0.5)                          size 386.781×59.39
│ ├ Background      anchor y[0.2,0.8]
│ │ └ Fill Area     sizeDelta.y 6.5649834 → Fill → end(22×27.946, ap 3.300003,0.00014114, pivot(1,0.5))
│ ├ Outline         anchor y[0.2,0.8]
│ └ counter         anchor(0.2,0.2)-(0.8,0.7) pivot(0,0.5)
├ Scoring Icon Holder anchor(0,1)  ap(13.784508,-29.690613) size 102.307×102.307
│ └ Over Image      anchor[0.07857143,0.9214287]
└ Level             anchor(0.5,0.5) ap(188.87901,-2.1200562) size 53.263×55.143
  └ Current Milestone Level text  anchor[0.2063243,0.7937748]
```
⛔ **未落地**：`Event Progression` 的 `ap(0,122)` 是以根（535×848.8）的**底**为锚，而卡在菜单里可见 535×414.4
⇒ 按根底算它会落到可见裁切之外；**这是坐标帧问题、不是几何缺失**，无 live 验证判不了原版运行时用的是哪一帧。
⇒ 建议调度台：真机进一次带 Raid/ExpansionPass 事件的菜单（或给 player 注入一个在期 Parent 事件）拍一张，再落这 4 条。

---

## 五、判据空的 6 条（反证，独立复核）

| 账行 | 反证（本轮独立执行，不是抄 B96） | 结论 |
|---|---|---|
| `159` `EventBadge.ShowNotification` · `160` `EventBadgeController.Awake` | `grep -rl` 于 `ExportedProject/Assets/*.prefab`：**引用 `EventBadgeController`/`EventBadge` 脚本 guid 的 prefab = 0**（1,314 个 prefab 全扫）。`assets_full` 与安卓 1.35.0 包同样 0（B96 已三源否定） | `Image.m_FillMethod` 那格**判据不存在**（不是「没找到」）；措辞只能是「本机拿不到」 |
| `732/733/734` `SurvivalEventWindowEnemyMarker.{Awake,SetEnemyName,SetHiddenEnemy}` | 同一手法：引用该脚本的 prefab **= 0**；`assets_full` 全盘 `Survival*` 只命中 **3 个 MonoScript**（`MonoScript_2192 = SurvivalEventWindowEnemyMarker` · `_2465 = SurvivalEventData` · `_2467 = SurvivalEventSavegame`）⇒ **只有类、没有任何带该脚本的 GameObject** | 行为已 1:1，缺的是**资产** |
| `394` `LiveOpsHandler.RefreshHandler` | 两处残差都只能 VA 级反汇编：① `Task.Yield()` 三句折到 **`RVA 0x6FB110`**（全局 `return false` 桩，`ret 0` ⇒ 本身无体）；② `+0x2d8` 虚槽名与同文件 `0x2a8 ↔ Slot 23` 的 stride 校准**互斥**（必有一条读错） | 本轮**未做** VA 反汇编，不猜 |

---

## 六、REMOTE 16 条（留调度台，零改动）

`31 ChallengeList.GetID` · `32 ChallengeListLocator.GetAssets` · `43/46/52 ChallengeManager.{AcceptPracticeChallenge,
CancelPracticeInvitation,SendPracticeInvitation}` · `104 DailyRewardTrackPanel.HandlePurchase` · `270 FastModeBaseEvent.OnMatchFound` ·
`461 MissionChallengeProvider.GetAssets` · `520 Missions.UpdateMissionProgress` · `524 MissionsConfig.CleanUp` ·
`526 MissionsTab.InstantiateGridMissions` · `715 SinglePlayerOnlyEnergyEventWindow.TryOpen` · `722 SkirmishEventWindow.CollectClicked` ·
`771/772/774 TwoSidesEvent_OnGoingTab.{CollectClicked,InitializeMilestones,OnOpen}`

按 §18·0：**只有「与服务器有关」允许本地模拟 + 出声**，且代替物必须写全「原版那一跳在哪儿 + 覆盖了什么」。
这 16 条里 `104 / 520 / 715 / 722` 已在早前轮次按 🔶 模拟并出声，其余 12 条**等调度台定口径**。
⛔ 本轮**没有**为它们写任何代码 —— 尤其是没有为 `31/32/461/524/526` 造 `MissionsConfig`/`MissionChallenge`/`ChallengeList`
空壳（那会与 `DailyData` 的本地任务表形成**两套实现** = 铁律「两处写同一条规则」）。

---

## 七、断言与接线

* 新文件 `CardPresentation/Editor/B97_DAILY_Assert.cs`（`public static class B97_DAILYAssert` · `Run(Action<bool,string>) ⇒ int 条数`）。
* **已接线**：`CardPresentation/Editor/ShellScene.cs:11023`（合计行之前，**同一行追加**）：
  `B97_DAILYAssert.Run((ok, msg) => CheckTrue(ok, msg));` ⇒ 该文件 11109 行 → 11109 行（零变化，已二进制计数核过）。
  ⚠️ 这一行是多个代理共用的「追加点」；若调度台发现重复调用，删掉多余那一个即可。
* 覆盖：① `ModifyLocalization` 转调 `RewriteMarks` + `null/"" ⇒ ""` + 与 `RewriteMarks` 逐样本同值；
  ② `InitializeMissionToast` 方法体**恰好一处** call `ModifyLocalization`（灭自证：改回直传必红）；
  ③ `displayRule` 三档（`-1`/`2`/`0`）+ 两条描述模板不同 + `onlyShowNumber ⇒ "{0}"`。
* ⚠️ **未跑**（红线：子代理不许跑 Unity 批处理）⇒ `ShellScene.Run` 的实跑归调度台同步点；本代理只保证 `typecheck 0/0`。

---

## 八、验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_daily bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0**（最终一次） |
| `ToastNotifications.cs` 行数 | 1561 → **1561**（零变化；`\n` 1561 · CRLF 0 · 裸 CR 0） |
| `ShellScene.cs` 行数 | 11109 → **11109**（零变化；同上三项都核过） |
| `merge_ledger_parts.py --dry --only B97_DAILY` | 更新 **37** · 新增 0 · 无变化 0 · 未写盘 |
| `check_patch_conflicts.py` | ✅ 无跨补丁重复键（2 份：`B97_TUT` / `B97_US`） |

⚠️ 中途两次 `typecheck` 里出现过**别人**的错误（`Shell/LiveOpsFilter.cs` 3 条 `DynamicList` 转换、`Editor/B97_SHOP_Assert.cs` 6 条
`MainMenuRuntime` 成员不存在）—— 都是当时**正在写的别人文件**（`B97_SHOP` 在 `MainMenuRuntime` 上飞），最终一次已全部消失。
按铁律 13·3②：那不是本代理的问题，⛔ 也没有去改别人的文件。

---

## 九、还没查清 / 交给下一棒

1. **`394` 的 VA 级反汇编**（`0x6FB110` 桩 + `+0x2d8` stride 校准）—— 需要三处 call 站点的实参装配，属专项。
2. **`621-624` 的坐标帧** —— 必须真机拍一张「带在期 Raid/ExpansionPass 事件的菜单」才能定（几何已全在 §四·3）。
3. **`272` 的 `b.<+0x428>` 消费口** —— 要 B11 的真身 `RankedScoreBoostEvent`（含 id、含 Save 计数器）。
4. **`681` 的 ⑤ 物品层** —— `item.HandleOnCollect(quantity)`（+0x1b8 = Slot 8）要按类型分派的物品注册表；
   这是本域之外的一整件（`Shell/RewardInfo.cs` / 物品层），⛔ 不是判据缺口。
5. **`39` cost 格的形状**（§四·1 的 (a)/(b) 两档）—— 需调度台拍，因为原版是 signal 字段、我们没有 signal 对象。

---

## 十、顺手发现（不在 37 条里）

1. 🔴 **`displayRule` 的完整实例表可以直接沉淀成判据**（§二·2 那张表，10 个 prefab × 21 个实例 × 值）。
   它一次解决了「哪颗 timer 用 2、哪颗用 -1」；建议调度台把它挂进 `资料/完全复刻/判据/` 作 `displayRule` 全表
   （本域以外还会有人问同一件事：`Missions Tab` 里 6 个 `MissionTimerDisplay` 分属哪张卡就是靠它定下来的）。
2. 🟠 **`assets_full` 的 `GameObject/*.json` 不带 pathID ⇒ 没法做「组件 → GameObject → 父链」反查**；
   同一件事改读 `d:/2/tools/assetripper_out/ExportedProject`（真 pathID + `m_Father`/`m_Children`）一次就通。
   这条是**通用手法**，本轮 510 就是靠它收口的（与 `资料/完全复刻/判据/B96_日常53_分诊.md` §六·1 记的「Sprite json 不带 pathID」同族）。
3. 🟡 **`ShellScene.cs:11023` 已成为 5+ 个断言集的共用追加行**（`B93S3Assert` / `B94Y6` / `B94B4` / `B94B6` / `B94B17` / `B97W2` / 本轮 `B97_DAILY`）。
   行尾注释已经很长；若继续加，建议调度台把它拆成「一个注册表 + 一行遍历」（⚠️ 那会**加行**，要同步改账落点）。
4. 🟡 `账/_parts/` 里 `B97_TUT.tsv` / `B97_US.tsv` 两份**未归档**（`_merged_B97/` 里没有它们）⇒ 合并时按 `check_patch_conflicts.py` 的顺序处理。

---

## 十一、账的覆盖边界（铁律 6）

- 本报告只覆盖 `账/日常_奖励.tsv` 的 **37 条 B**（该表现读：A 602 / B 37 / C 2 / D 132，共 785 行 → 合并本补丁后 4 条 B 转 A）。
  **不含** 602 条 A、2 条 C、132 条 D。
- 「判 A」的两种口径在本报告里**分开写**：`745/510` = 代码/判据真的补齐；`492/728` = **休眠 A**（残差全在服务端数据，
  行为与原版在同样数据下逐位一致）—— 若调度台不认休眠口径，这两行请直接打回 B，其余结论不受影响。
- 「判据齐」≠「已复刻」：`REMOTE` 那 16 条的判据**本身是闭合的**（原版那一跳在哪儿有出处），卡的是数据；
  `判据空` 那 6 条是**资产/方法体真的不存在于本地**（不是没找到）。
- 仍**未覆盖**：`MissionsConfig` 全部实例内容（本地 0 份）· `FastMode` / `Survival` / `TwoSides` 三族活动数据（本地 0 份）·
  `EventBadge` prefab（本机与安卓包都无）· `Event Progression` 的运行时坐标帧。
- 本轮**没动** Unity / `ruleprobe` / git / 账主表 / 正本；只读 `decomp_full` / `out_cpp2il_dev_ilspy` / `decomp_generic_trial` /
  `assets_full` / AssetRipper 导出工程 / 本仓源码。
