# W_A · `TimerDisplay` 等效件 + 五处倒计时接线

> 2026-10-17 · 写手代理 W_A（界面线）· 白名单见简报。
> 判据 = `d:/2/tools/decomp_full/TimerDisplay__*.c` + `SupportMethods__GetTimeString.c` +
> `d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json` + `资料/普查产出_1017/UI/U2_左栏六个内容页.md` §二。

## 1. 结论 + 改动清单

**结论**：新增 `Shell/TimerDisplay.cs`（原版 `TimerDisplay` 的等效件：每秒重算、`GetTimeString` 逐句移植、
描述串拼接/格式化、最后 5 分钟档、到点清空、目标未知出声 + `--`），并把商店 / 战役 / 日常三条任务卡 / 周常 /
每日奖励 / 连登共 **8 颗文字**接上去；两处目标时刻本地不存在（战役、周常）按用户口径**不编日期**，出声一次并稳定显示 `--`。
typecheck：**运行时 0 错 / 编辑器 0 错**。

| 文件 | 行 | 改了什么 |
|---|---|---|
| `Shell/TimerDisplay.cs`（**新建**） | 1–279 | 等效件本体：`Attach` / `Update`（1 秒 tick）/ `Refresh` / `FormatTimeString` / `Compose` / `ShopRefreshAt` / `ShopRefreshText` / `TextFor` / `WarnUnknownOnce` |
| `Shell/ShopData.cs` | 241–248 | `RefreshTime`：死串 `"12h 24m"` → `TimerDisplay.ShopRefreshText()` |
| `Shell/ShopTabPage.cs` | 311–327 | `TimeCounter/Time` 那一颗挂 `TimerDisplay`（9 点规则 + 本机时钟 + 最后 5 分钟 `Last Minutes` 档） |
| `Shell/CampaignTab.cs` | 1035–1056 | `Premium Panel/Timer Text`：死串 `"Siguiente: 5d 20h 15m"` → `TimerDisplay.Attach(..., null, "Siguiente: ", …)`（未知 ⇒ `Siguiente: --` + 出声） |
| `Shell/DailyData.cs` | 147–154 | `DailyTimer(i)`：`"Available in 64h"` → 目标 = `NextResetAt` 的真倒计时 |
| `Shell/DailyData.cs` | 666–679 | `WeeklyEndsIn()`：`"Ends in 12h 34 m"` → **未知目标**（`Ends in --` + 出声）；`ResetIn()`：`"Resets in 12h 34 m"` → 目标 = `NextResetAt` |
| `Shell/DailyData.cs` | 754–757 | `RewardTimerText()` 注释改真（实现仍转发 `ResetIn()`，现在是活值） |
| `Shell/DailyData.cs` | 972–979 | `StreakTimerText()`：`"19h 23m"` → 目标 = `NextResetAt` 的真倒计时 |
| `Shell/MissionsTab.cs` | 554–563 | 每日任务行 `timer` 挂组件（目标 `NextResetAt`，前缀 `Available in `） |
| `Shell/MissionsTab.cs` | 849–857 | 登录卡 `Timer` 挂组件（`Resets in `） |
| `Shell/MissionsTab.cs` | 1055–1061 | 骷髅卡 `Timer` 挂组件（`Resets in `） |
| `Shell/MissionsTab.cs` | 1194–1199 | 周常 `Timer` 挂组件（目标未知 ⇒ `Ends in --` + 出声） |
| `Shell/DailyStreakPopup.cs` | 423–428 | 连登窗 `Timer Text` 挂组件（目标 `NextResetAt`，无前缀） |

## 2. 每处消费点：原版 → 我们

| 消费点 | 原版字段 / 行为（出处） | 我们改成了什么 |
|---|---|---|
| 商店 `TimeCounter/Time` | `ShopTab.Setup`：`CheckTimer` 为真 ⇒ `SetActive(true)` + `TimerDisplay.Initialize(target, "")`；目标 = `CardShopTab.CheckTimer` 的 `< 9 点取当天、否则次日` + `+9h`（`CardShopTab__CheckTimer.c:20-35`，常量 `DAT_1834b32c0` 实读 **9.0**）。该实例 `useExtraText=0` · `useLastMinutesText=1` · `lastMinutesText=MainMenu/LastMinutes`（`MonoBehaviour_-5363483528564307958.json` 实读）。 | `TimerDisplay.Attach(tm, () => ShopRefreshAt(DateTime.Now), "", "商店 TimeCounter", null, LastMinutesText())`。文本 = 时间串；最后 5 分钟前缀换成 `Last Minutes`/`最后时刻`。**本机时钟**（原版是 `PlayerDataManager.CurrentServerDateTime`，我们无服务器 —— 已标）。 |
| 商店头 `RefreshText` | 独立 TMP，词条 `MenuShop/RefreshCounter`（`RefreshTab` 前那一颗）；与 TimerDisplay 的 `timerTextDescription` 不是同一件（该实例的 `timerTextDescription` 为 **null**）。 | 未动（`ShopTabPage.cs:275` 已走 `Loc.T("MenuShop/RefreshCounter")`）。 |
| 战役 `Premium Panel/Timer Text` | `CampaignPremiumPointsPanel.Initialize` → `TimerDisplay.Initialize(timeToNextCollect, "")`（`CampaignPremiumPointsPanel__Initialize.c`）。实例 `useExtraText=1` · `extraText=MainMenu/General/NextWithTwoPoints` · `useLastMinutesText=0`（`MonoBehaviour_-4260101405004392705.json` 实读）。真实截图（`R4_A380` §8）：`下一个：4分钟 4秒`。 | **目标不存在**（见 §4）⇒ `Attach(..., null, "Siguiente: ", …)`：出声一次 + 稳定 `Siguiente: --`。描述串保留 prefab 出厂占位 `Siguiente: `（`NextWithTwoPoints` 正文官方表里查不到，⛔ 不猜）。 |
| 日常行进/登录卡/骷髅卡 `Timer` | `MissionTimerDisplay.Update`：目标 = 挑战自己的 `refillTime` / `endTime`（`MissionTimerDisplay__Update.c`）；文本 = `string.Format(Loc("Missions/CollectCounter"` / `"Missions/CompleteCounter"), GetTimeString(剩余))`（`onlyShowNumber=0`，实读 22 个实例全是 0）。 | 目标 = 我们自建的**日界** `DailyData.NextResetAt`（本机时间，见 §4「我们挑的」）；前缀保留 prefab 出厂串（`Available in ` / `Resets in `）。组件每秒 tick。 |
| 周常 `Timer` | `MissionTimerDisplay`，prefab 占位 `Ends in 12h 34 m`。目标由服务端下发（周常挑战的 end/refill）。 | **目标不存在** ⇒ `Ends in --` + 出声一次。 |
| 每日奖励窗 `Timer Text` | `TimerDisplay` 运行时填（占位 = `Resets in 12h 34 m`）。 | 走 `DailyData.RewardTimerText()`（= `ResetIn()`，目标 `NextResetAt`）——**建窗那一刻**是活值；`DailyRewardPopup.cs` 不在白名单 ⇒ 没挂每秒 tick（见 §4）。 |
| 连登窗 `Timer Text` | prefab 占位 `19h 23m`；语义 = 下一档连登奖励的倒计时。 | 目标 = `NextResetAt`（**我们挑的**，理由见 §4）；`DailyStreakPopup.cs:427` 挂组件，每秒 tick。 |

### 等效件本身（`TimerDisplay.cs`）照原版实现的行为

- `Update`：原版 `frameCount % Application.targetFrameRate == 0`（约 1 秒一次）⇒ 我们按 `Time.unscaledDeltaTime` 累计 1 秒。
- `Attach` **当场算一次** —— 批处理没有帧循环，存盘的场景/自检看到的就是正确文本。
- 文本 = 描述串 + 时间串；描述串含 `{0}` ⇒ `string.Format`，否则**直接拼接**（原版那一对分支；拼接不加空格）。
- 最后 5 分钟档：`remaining < TimeSpan.FromMinutes(5)` 时描述串换成 `lastMinutesPrefix`（常量 `DAT_1834b3380` 实读 = **5.0**）。
- 到点（`remaining <= 0`）：原版 `timer.gameObject.SetActive(false)` + `OnRunOutOfTime` 只发一次 ⇒ 我们清空同一颗 `Label` 的文本（视觉等价；组件继续 tick，目标滚到下一档时自己回来）。
- 目标未知：`Debug.LogWarning` **按 `what` 去重一次** + `<prefix>--`，绝不编日期。

## 3. `SupportMethods.GetTimeString` 的格式规则（逐句移植）

签名（类桩 `SupportMethods.cs:126`）：
`GetTimeString(TimeSpan ts, bool eventTimer = false, bool clampTo0 = false, bool hide0Values = false)`
`TimerDisplay.UpdateTimer` 的调用点实读 = `(remaining, eventTimer: true, clampTo0: false, hide0Values: false)`。

分支（`SupportMethods__GetTimeString.c` 全文逐句）：

1. `clampTo0 && ts < 0` ⇒ `ts = TimeSpan.Zero`。
2. **天档**：`ts.TotalDays > 1` **且** `eventTimer` ⇒ `DH(Days, Hours)`；`hide0Values && Hours == 0` ⇒ `D_Only(Days)`。
3. **小时档**（否则）：`ts.TotalHours > 1` ⇒
   `!hide0Values && Minutes == 0` ⇒ `H_Only((int)TotalHours)`；其余 ⇒ `HM((int)TotalHours, Minutes)`。
4. **分钟档**（否则）：`!hide0Values` ⇒ `MS(Minutes, Seconds)`；`hide0Values && Seconds == 0` ⇒ `M_Only(Minutes)`；其余 ⇒ `MS(Minutes, Seconds)`。

**边界（照原版一个都不改）**：
- `TotalDays > 1` 与 `TotalHours > 1` 都是**严格大于** ⇒ 正好 24h00m00s 会落到小时档、画成 `24h 0m`。
- `Minutes == 0` 只在**小时档**才折成 `H_Only`；分钟档不折。
- `D_Only` / `M_Only` 只在 `hide0Values = true` 时可达；`TimerDisplay` 恒用 `hide0Values = false` ⇒ 实际只会遇到 `DH` / `H_Only` / `HM` / `MS`。
- 0 值：`ts = 0` 走到分钟档 ⇒ `0m 0s`；负数（不 clamp）⇒ `TimeSpan` 分量为负，原版也照算（我们这一族在到点前就清空，不会画负数）。

格式串（`官方本地化_en+zh-CN.json` 原文）：

| key | en | zh-CN |
|---|---|---|
| `MainMenu/Time/DH` | `{0:0}d {1:0}h` | `{0:0}天 {1:0}小时` |
| `MainMenu/Time/HM` | `{0:0}h {1:0}m` | `{0:0}小时 {1:0}分钟` |
| `MainMenu/Time/MS` | `{0:0}m {1:0}s` | `{0:0}分钟 {1:0}秒` |
| `MainMenu/Time/D_Only` | 官方表**没有** ⇒ 我们拼 `{0:0}d` | ⇒ `{0:0}天` |
| `MainMenu/Time/H_Only` | 官方表**没有** ⇒ `{0:0}h` | ⇒ `{0:0}小时` |
| `MainMenu/Time/M_Only` | 官方表**没有** ⇒ `{0:0}m` | ⇒ `{0:0}分钟` |

⚠️ `_Only` 三条是**我们挑的**（按 DH/HM/MS 里出现的单位机械拼），⛔ 不是原版文案。
⚠️ 官方表里另有 `MainMenu/Time/D` / `H` / `M`，但它们是 `{0:0}d ago` / `…ago`（“多久以前”），**不是倒计时那一族**。

## 4. 没能判 / 没做的（逐条：为什么 + 搜过什么）

1. **战役 `timeToNextCollect` 不存在** —— 简报说“我们自建数据里的字段”，但实测**没有**：
   `Shell/CampaignData.cs` 全文 0 个 `DateTime` / 0 个 `*Time*` 字段；全仓
   `grep -rn "timeToNextCollect\|TimeToNextCollect\|NextCollect\|CollectAt" d:/4/Unity` = **0 命中**
   （只有 `资料/` 规格文档提到它）。原版由服务端下发（`CampaignWindowTab.SetActiveCampaign`）⇒
   按用户口径**不编日期**：目标 `null` ⇒ 出声一次 + `Siguiente: --`。
2. **周常刷新时刻不存在** —— `DailyData.TryDailyReset` 明确“只清当日那一族 ⇒ 周常/连登/奖励抽屉不在内”；
   全仓没有 `weeklyReset` / `nextWeekly*` 字段；原版由服务端下发 ⇒ `Ends in --` + 出声一次。
3. **每日任务自己的 refill/end 时刻不存在** —— 原版 `MissionTimerDisplay` 的目标是挑战的
   `refillTime` / `endTime`（`MissionTimerDisplay__Update.c`），我们的三条日常任务是自建 mock、没有这个字段。
   ⇒ **我们挑的**：统一用我们自建的日界 `DailyData.NextResetAt`（它确实是这些任务的清空时刻，见 `TryDailyReset`）。
   已写进代码注释，⛔ 不是原版值。
4. **连登 `Timer` 的目标** —— 原版由服务端的 LiveOp 到期时间决定，本地查不到。连登语义 = “下一天开始”
   ⇒ **我们挑的**：`NextResetAt`。已写进代码注释。
5. **`DailyRewardPopup.cs` 不在白名单** ⇒ 每日奖励窗只拿到“建窗那一刻”的活值，
   **没有挂每秒 tick 的组件**。要补：在该文件 `BuildTimer` 里给 `tm` 挂
   `TimerDisplay.Attach(tm, () => DailyData.NextResetAt, "", "每日奖励窗 Timer", null)`（一行）。
6. **`DailyData.RefillText()` 没动** —— 它不是倒计时，是 `Mission Header/Refill Counter` 的“剩余补充次数”
   （prefab 占位 `0 Disponible`，日常规格 §3·6）；原版的值由服务端下发，我们数据模型里没有 ⇒ 保持占位。
   ⛔ 把它改成倒计时是错的。
7. **商店 Items 页的可见性语义** —— 原版 `ItemShopTab.CheckTimer` 只在 `LiveOpsUtility.IsTimedEvent(store)` 为真时
   才显示计时器；我们没有 store 数据 ⇒ 三页统一按 Cards 页的 **9 点规则**（**我们挑的**，已在代码注释标明）。
8. **`Editor/RewardsScene.cs:2209` 会红** —— 那条断言把登录卡 `Timer` 的文案钉在出厂串
   `Resets in 12h 34 m` 上；现在是真倒计时。Editor 不在白名单，我**没有改** ⇒ 需要主对话更新那条断言
   （建议改成“文本以 `Resets in ` 开头且能解析出非零时长”，或直接比 `DailyData.ResetIn()`）。
9. **`Editor/ShopScene.cs:1450` 仍有意义但有一点点边界风险** —— 它比的是
   `TextOf(Time) == ShopData.RefreshTime`；两边现在都是“调用那一刻”的活值，正常情况下同一次同步执行里相等；
   若刚好跨分钟边界会差一分钟（概率极低，未改）。
10. **没有跑 Unity / 没有真机验收**（简报禁止）⇒ “组件在运行期每秒真的 tick”只做了静态核对
    （`Update` + `Time.unscaledDeltaTime`），**没有实机证据**。
11. **`useLastMinutesText` 的 5 分钟阈值**是常量 `DAT_1834b3380` 实读（= 5.0），
    但原版 `FromMinutes((int)constant)` 的 `(int)` 我也照 C 的截断写了（5.0 → 5）。
12. **新建的 `TimerDisplay.cs` 没有 `.meta`** —— 本批禁止跑 Unity ⇒ 下次 Unity 导入时自动生成。
    没有任何资产按 GUID 引用它（全部走 `AddComponent<TimerDisplay>()` 按类型挂），所以不影响编译/运行。

## 5. typecheck

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

命令：`TMPDIR=/tmp/wf_a_timer bash d:/4/Unity/工具/typecheck.sh`。

---

## 6. 顺手发现（⛔ 未改，由主对话分流）

1. **`Shell/BaseOfferPopup.cs:148`**：`DefTimerText = "23h 34m"`（礼包/Offer 的 `Timer Text`）；
   `BaseOfferPopup.cs:931` 直接画它，没有真倒计时。我们的 offer 数据里没有到期时刻 ⇒ 需要先补数据字段。
2. **`Shell/OfferContainer.cs:335`**：`DefTimerText = "5d 20h 15m"`（商品抽屉的 `Timer Text`），同上。
3. **`Shell/EnergySinglePlayerOnlyEventWindow.cs:682`**：`"Termina en: 23d 5h"` 写死；
   文件头 `:34` 自己写着原版是 `TimerDisplay.Initialize(endTime)`。活动数据本地没有 ⇒ 同“目标未知”那一类。
4. **`Shell/ProfileTab.cs:640-655`**：`Timer` 那一棵**出厂关着**（`RankingDisplay.timerDisplay` 从不 `SetActive`），
   文本现在是空串；原版占位是 `'Ends in: 23d 5h'`。我们**没有段位数据** ⇒ 保持不建内容是合理处置（不是缺陷）。
5. **两份 `GetTimeString` 实现**：`Shell/ItemDrawer.cs:1072` 的 `EphemeralTextOf` 已经有一份逐句移植
   （`24 hours` 那一条链），与 `TimerDisplay.FormatTimeString` **是同一份规则的第二份拷贝**。
   建议以后收口到 `TimerDisplay.FormatTimeString`（`ItemDrawer.cs` 不在本批白名单，未动）。
6. **`Shell/ShopData.cs:240` 的 `RefreshText`（`"Refreshes in:"`）现在没有任何调用点**
   （`ShopTabPage` 已改走 `Loc.T("MenuShop/RefreshCounter")`）—— 留作“prefab 英文原文留档”，
   不是缺陷；但要小心下一个会话把它当活口。
7. **`Editor/ShopScene.cs:1451` 的断言文案**还写着“值是我们挑的” —— 现在值已经是真倒计时了，
   那句话过期（Editor 不在白名单，未改）。
