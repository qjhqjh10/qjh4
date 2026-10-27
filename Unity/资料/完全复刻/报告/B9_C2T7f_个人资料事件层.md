# B9 / C2T7f —— `个人资料_成就_排行.tsv` **事件回调 → 数据方法**层逐条核（27 条）

> 查判据型子代理（只读 + 落账补丁）。范围 = 前两批点名的「事件回调 → 数据方法」那层（27 条，按方法名从 `账/个人资料_成就_排行.tsv` 现读）。
> 产出：`判据/B行逐条核_个人资料_事件层.tsv`（8 列 · 27 行）· `账/_parts/B9_C2T7f.tsv`（11 列 · 27 行）· 本报告。

---

## 一、结论速览

- **建议 A = 0 · B = 23 · D = 4。**
- **0 A 是诚实结论**：这一层的本地语义**大多已被我方数据方法承接**（`ApplyMatchResult` / `ShouldShowSeasonChange` / `LeaderboardData.LocalScore`），但这些原版方法的**本体就是「服务器消息处理 / 服务器提交」**，没有任何一条能「整方法本地等价」⇒ 升不了 A（不是没做，是原版这条本身跨服务端）。
- **D = 4（纯服务器，写全证据 + 时间限定，供你裁）**：
  | 方法 | 证据（原版调用链断点） | 时间限定 |
  |---|---|---|
  | `RankedV2Event.RecordMatchFinished(int,int,...)` | `decomp_full/RankedV2Event__RecordMatchFinished.c` → `PlayfabWrapper.GenericCloudScriptHandler(0xaa/10, …)`（**纯云函数提交赛果**，无本地写入） | 远端 PlayFab 赛果提交云函数 |
  | `RankedV3Event.RecordMatchFinished(int,int,...)` | 同上（V3 版） | 同上 |
  | `RankedV2Event.OnSeasonCollected(CollectedRewardsResult)` | `RankedV2Event__OnSeasonCollected.c` → `AsyncTaskMethodBuilder.Start<d__14>`（**async 收取赛季奖励 = 服务器回执**） | 远端赛季奖励收取 API |
  | `RankedV3Event.OnSeasonCollected(CollectedRewardsResult)` | 同上（V3 版） | 同上 |

---

## 二、逐类判定（按两条硬口径）

### 硬口径①：状态转换逐档对（不只看名字像）
- `RankedV2Event.OnMatchFinished`（读 `.c` 逐档：`ResultCount` 写入；`matchWon==10` 支 / `==0x14` 支分别调 `GetLossStreak+1`；再 `GameModes.SetLossStreak`）—— 我方 `RankedData.ApplyMatchResult` 的 loss-streak 规则（胜⇒归零、负/平⇒+1、suspicious⇒不记）**逐档对上**；但原版结尾 `PlayfabWrapper.GenericCloudScriptHandler(0x17f,…)` 是**服务器提交**。⇒ **B（本地结算等价、服务器那半未做=P3）**。
- `Expire`（`.c`：`LastPlayedSeason==0` ⇒ `LeaderboardManager.GetLeaderboard(…)` ⇒ 开弹窗）：我方 `ShouldShowSeasonChange`（`_points!=0 && _lastPlayedSeason==0`）**判定那半等价**；拉榜=服务器。⇒ **B**。

### 硬口径②：只有纯服务器才判 D，本地保存那半不判 D
- **D（见上表）**：`RecordMatchFinished`×2、`OnSeasonCollected`×2 —— 纯云函数/服务器回执，无客户端写入。
- **B（含本地部分，服务器那半=P3 备注）**：`StartMatch`（本地 `ValidateDeck`+`CheckHiddenCardsInDeck`+`CreatePlayerBattleData` 都有；匹配 `MatchMakerManager.FindMatch`=服务器）· `MatchFoundCallback`（`LastOpponentInfo` 本地 + `Playfab 0x440` 对手信息=服务器）· `OnMatchProcessSuccess`（`LeaderboardManager.LocalScore` 本地模拟 + `GameAnalytics.RecordDivisionChange` **遥测**=P3）· `OpenSeasonChangePopup`（客户端弹窗，我方 `RankedNewSeasonWindow` 未定位到开窗接线）。

### 其余 B（未定位 / 字段 / 判据缺）
- **判据缺（原版体空/丢）**：`RankedFastMode.GetBaseData` 的 `.c` 是 `{ }` **空体** · `RankedMatchResult.SetBattleResult` 的 `.c` 是 `return;` **空/丢** —— 需 VA 反汇编确认是真·空方法还是被剥。
- **字段级**：`RankedMatchResult.GetWinner`（原版按 `matchWon` 码 10/0x14 返回胜方；我方是 `BattleContext.Winner` 字段，形状不同）。
- **我方未建该管理器层**：`PlayerAvatarDataManager.*`（`GetAllItems/GetItemWithId/GetItemImageWithId/IsDefaultBorderEquipped/SetCurrentItem/SetCurrentItemNoSave`）—— `Shell/ProfileData.cs:6` 明注「本地**没有存档**」，头像/边框数据只在 `AvatarTab` 内联；`SetCurrentItem` 的本地装备保存等价，但原版带 `Action` **上传回调**（服务器/Steam）。
- `RankedEventBase.GetBaseData`：我方数据即 `RankedData` 字段，无 `GetBaseData` 方法。
- `CosmeticItemAvatarBorder.HandleOnCollect`（收下即 `SetCurrentItem` 自动装备）/ `IsParentEventActive`（`LiveOpsManager.GetEvent` 活动态）—— 本地语义在，方法未定位。

---

## 三、方法学 & 证据

- 读的原版体：`decomp_full/` 的 `RankedV2Event__{OnMatchFinished,OnMatchProcessSuccess,RecordMatchFinished,StartMatch,Expire,MatchFoundCallback,OnSeasonCollected,OpenSeasonChangePopup}.c` · `RankedV3Event__*` 同名 · `RankedFastMode__GetBaseData.c`(`{ }`) · `RankedMatchResult__{GetWinner,SetBattleResult}.c` · `PlayerAvatarDataManager__{SetCurrentItem,SetCurrentItemNoSave,GetItemWithId,GetItemImageWithId}.c` · `CosmeticItemAvatarBorder__{HandleOnCollect,IsParentEventActive}.c`。
- 读的我方：`Shell/RankedData.cs`（`DivisionOf/MatchWonValue/ApplyMatchResult/ToggleRankedMode/ShouldShowSeasonChange/MarkSeasonCollected/AdvanceSeason`）· `RuleEngine/Core/BattleContext.cs`（`Winner` 字段）· `Shell/ProfileData.cs:6`（头像存档注释）。
- ⚠️ 多条 `.c` 被 Cpp2IL 丢体/丢分支（`RecordMatchFinished` 只有 `PlayfabWrapper...` 一行能读），但**服务器断点（Playfab/Leaderboard/MatchMaker 调用）清晰**，足以判 B/D 的边界。

---

## 四、产物

- `判据/B行逐条核_个人资料_事件层.tsv`：8 列 × **27 行**（A=0 / B=23 / D=4，每行带一句结论+还差）。
- `账/_parts/B9_C2T7f.tsv`：11 列 × 27 行（状态列不动；只改 差异/轮次=`B9_C2T7f`/备注前缀 `[C2T7f 2026-10-27]`，原备注保留）。
  - 校验：`merge_ledger_parts.py --dry --only B9_C2T7f` → **更新 27 · 新增 0 · 无变化 0 · 0 报错**；`check_patch_conflicts.py` → 无跨补丁重复键；纯 LF、无裸 CR、全 11 列。
- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。

## 五、给小节的判断
这一层「事件回调 → 数据方法」**不会再出 A**（原版本体=服务器消息处理）。**能收的只剩 D（4 条纯服务器）** 与**本地那半已等价（B，备注写明『本地已等价、缺服务器=P3』）**。若后续要把「本地那半」正式记成已做，可把这些行拆成「本地子项 A / 服务器子项 D」，但那是**改账结构**的活，需你拍板（本轮不动）。
