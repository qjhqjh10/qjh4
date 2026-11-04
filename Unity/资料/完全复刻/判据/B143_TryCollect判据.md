# `B143_TryCollect判据` —— `B143-CHK05-TRYCOLLECT-POPUP` 的判据补齐（只读）

> 只读代理 `B143-W5`（2026-11-05）· **没改代码 / 没动 `d:/2` / 没跑 Unity**。
> 上一单 `报告/B143_W5_批1撤出界面.md` §三 记的是「判据体没定位到」—— 本单把它查清了。

## 0. 结论：✅ 判据找到

原版这一跳**确实是**「先弹一个『正在领取』模态、成功才关」：

```csharp
// out_cpp2il_dev_ilspy/FastModeBaseEvent.cs:1794  TryCollect（真体，亲读）
...
instance.ShowPopUp("Missions/CollectingRewards", localizeTexts: true, closeOnEsc: false, primaryButton, secondButton);
```

- 文案是 **I2 术语键 `Missions/CollectingRewards`**（不是明文），`localizeTexts: true`、`closeOnEsc: false`；
- 成功支 = **`WindowsManager.HidePopUp(…)` 先关，再 `RewardService.Collect(…)`**（见 §1 的 `g__OnCollectSuccess_2`）——**顺序是「先关窗、后发奖」**；
- 失败支 = `ErrorPopupWindowController.ShowErrorMessage(0xb4 = 180, …)`（见 §1 的 `b__20_1`）。

## 1. 那 4 个 lambda（逐个读体）

| 文件（`D:/2/tools/decomp_full/`） | 首行函数名（**能信吗**） | 体摘要 | 判定角色 |
|---|---|---|---|
| `FastModeBaseEvent.__c__DisplayClass20_0_object__object____TryCollect_b__0.c` | `bool FUN_180eb4e20(longlong, longlong)`（名字丢了，**体可读**） | `iVar3 = (*(*plVar2 + 0x758))(plVar2)`（=「当前分」）；`iVar1 = *(int*)(param_2 + 0x10)`（= 该条 `MilestoneReward` 的 milestone）；`return iVar3 < iVar1;` | **谓词** `CurrentScore() < milestone`；真体里被 `Enumerable.All(milestones, predicate)` 用 ⇒ **全部里程碑都还没到 ⇒ 直接 return**（正是 `if (!flag2) { …ShowPopUp… }` 的外层闸） |
| `…TryCollect_g__OnCollectSuccess_2.c` | `void FUN_180eb4e80(longlong, longlong, longlong)`（体可读） | **`WindowsManager.HidePopUp(instance, 1, 0)`** → 通过虚表 `+0x428` 写回 updated save → `RewardService.Collect(uVar2, 1, callback, 0, 1, 0, 1, 0)` → `GameAnalytics.RecordTransaction(0x5d = 93, 2, …)` | **成功支**：先 **关弹窗**、再发奖、再埋点 |
| `…TryCollect_b__20_1.c`（`__c` 单例那份） | `void FUN_180eaac10(void)`（体可读） | `ErrorPopupWindowController.ShowErrorMessage(0xb4 = 180, 0, DAT_1842b80e0, 0)` | **失败支**：走通用错误弹窗（码 180） |
| `…TryCollect_b__3.c` | ⚠️ **首行是 `UnityEngine_UIElements_SortColumnDescriptions__OnDescriptionChanged`**（**文件名与内容不符**，且体里带 `Could not recover jumptable … indirect jump`） | 只剩一句间接调用：`(**(code **)(lVar1 + 0x18))(*(lVar1 + 0x40), *(lVar1 + 0x28))` | ⚠️ **只能按签名判**：`out_cpp2il_dev_ilspy/FastModeBaseEvent.cs:342` = `_003CTryCollect_003Eb__3(IReadOnlyList<RewardInfo> _)`、`out_il2csharp/FastModeBaseEvent.cs:250` 同签名 ⇒ **「拿到奖励列表之后那一跳」**（本地等价 = 我们 `OnRewardsCollected` 那条）。**体本身读不出来**，如实记 |

## 2. 换源结果（三处都查了）

| 源 | 结果 |
|---|---|
| **`out_cpp2il_dev_ilspy/FastModeBaseEvent.cs:1794`** | ✅ **真体**（本单的主判据）。`ShowPopUp("Missions/CollectingRewards", localizeTexts: true, closeOnEsc: false, …)` 在 **`:1876`**；lambda 定义在 `:162 / :300 / :324 / :342` |
| `out_il2csharp/Assembly-CSharp/FastModeBaseEvent.cs:85` | ❌ 体 = `throw new NotImplementedException("Method body was not recovered.")`；**但 4 个 lambda 的签名齐全**（`:145 / :242 / :246 / :250`）⇒ 用它对签名、不用它取体 |
| `decomp_generic_trial` | ❌ **没有** `FastModeBaseEvent` 的产物（`grep -rl TryCollect` = 0）—— 这正是「泛型类方法体默认没人导」那类缺口，已由 `out_cpp2il_dev_ilspy` 补上 |

**同一条链的另外三个调用点**（`out_il2csharp`，签名同形、可交叉验证）：
`Missions.cs:180` · `SinglePlayerScoringEvent.cs:55` · `TwoSidesEvent.cs:116` —— 全是 `windowsManager1.ShowPopUp("Missions/CollectingRewards", true, false, 0, 0);`
⇒ **同一个「正在领取」弹窗被 4 个活动族共用**，文案与开关参数**四处一致**。

## 3. 场外判据（资源 / 词条侧，反推它确实存在）

| 判据 | 结果 |
|---|---|
| I2 术语键 `Missions/CollectingRewards` | ✅ **在官方表里**：`Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 命中 **1** 处（不是我们自造的词条） |
| 我们有没有这颗弹窗的宿主 | ✅ **有**：`Shell/WindowsManager.cs:1452 ShowMessagePopUp(messageKey, primaryKey, onPrimary, secondaryKey, onSecondary, closeOnEsc)`（宿主 = 原版 `MessagePopupWindow{,_2Buttons}` = 类 `PopUpGameWindow`，2026-10-12 `A416` 收编）；另有 `:1390 ShowPopUp(text, okText, onOk, cancelText, onCancel)` |
| 失败支的出口 | ✅ **有**：`Shell/ErrorPopupWindowController.ShowErrorMessage(CustomError.…)`（生产已在用，`BattleDriver.cs:12852`） |

## 4. 该做什么（施工单）

落点：`Assets/CardPresentation/Shell/FastModeBaseEvent.cs:399 TryCollect`（现读：同步本地落地，`collectedRewards` 直接推到 `pending`）。

1. **进「云端调用」之前**（= 现在同步发奖那一段之前）：
   `var wm = WindowsManager.Instance; if (wm != null) wm.ShowMessagePopUp("Missions/CollectingRewards", null, null, null, null, closeOnEsc: false);`
   （取实例的统一写法 = `WindowsManager.Instance`，见 `Shell/AllianceEventScorePanel.cs:253` 等同族调用点；⛔ 不是 `InstanceOrDefault`）
   —— 文案键 + `closeOnEsc: false` 两个实参照原版；**按钮两个实参原版是 `default`**（cpp2il 是两个 `default` 局部）⇒ 我们不挂钮（`primaryKey/secondaryKey` 传 `null`，与 `ShowPopUp(…, 0, 0)` 那个重载等价，需按 `ShowMessagePopUp` 对 `null` 的处理核一眼）。
2. **成功支**：**先 `HidePopUp()`，再** `OnRewardsCollected(...)`（顺序照 `g__OnCollectSuccess_2`：`HidePopUp` 在第一句）。
3. **失败支**：`ErrorPopupWindowController.ShowErrorMessage((CustomError)180)`
   —— ⚠️ 本地没有服务器 ⇒ **这一支结构性不可达**；⛔ 别自造一个假的失败路径，按「如实记：本地无失败」处理（或接一条「本地等价失败」的显式说明）。
4. **一帧可见性**：本地是同步收，弹窗必须真的**画出来过**（真机至少有 1 帧）；批处理没有帧循环 ⇒ 自检里**不能**用「弹过没有」当断言，改断「`ShowMessagePopUp` 被调用过 + `HidePopUp` 在其后」（可加计数器）。

## 5. 还差什么（⛔ 别拿猜测填空）

1. **`b__3` 的体读不出来**（文件名与内容不符 + jumptable 失败）⇒ 它在「拿到 `IReadOnlyList<RewardInfo>` 之后」做什么，**只能按调用点与本仓等价物推断**（我们那条是 `OnRewardsCollected`）。
2. **`ShowPopUp` 两个按钮实参原版是 `default`** ⇒ 究竟是「1 按钮版（默认 OK）」还是「0 按钮、纯等文案」，要读 `WindowsManager.ShowPopUp` 的 5 参实现（`out_cpp2il_dev_ilspy/WindowsManager.cs` 或反汇编）才能钉死；本单**没读**。
3. **错误码 180** 在 `CustomError` 枚举里是不是同一个语义（编号对得上 ≠ 文案对得上）—— 未核。
