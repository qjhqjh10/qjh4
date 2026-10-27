# B9_C3T4b · 社交/聊天线 B 行逐条核账 —— 报告

> 子代理 **C3T4**（查判据型 · 只读）· 2026-10-27
> 范围：`d:/4/Unity/资料/完全复刻/账/社交_聊天.tsv` 里 **状态 = B 的全部 45 行**（现读：该表共 466 行，A 158 / B 45 / C 8 / D 255）。
> 产出：`判据/B行逐条核_社交.tsv`（8 列，45 行）· `账/_parts/B9_C3T4b.tsv`（11 列，45 行）· 本报告。

## 一、做了什么（方法）

1. 逐行取 `判据出处` / `判据要点` → 到 `d:/2/tools/decomp_full/` 找原版**真方法体**（45/45 全部找到 `.c`；#22/#43/#45 是带命名空间的 `Everguild.LiveOps.*` / `Everguild.UI.*`，`.c` 均在）。
2. 逐行解析 `我们的落点` 的 `文件:行`，**打开该行看它是不是一个实现方法**（这是「落点闸」的可操作判据）。
3. 结合行内已有的 `差异`（历轮代理逐句读过原版方法体写下的）判 **A / B / 落点失效 / 判据缺**；对少数「等价」候选（#7/#8/#43）另读了本地实现方法核对。
4. 铁律 11：休眠机制（现池无来源）**不判不做**；只有「原版本身不可达 / 判据空」才可 D —— 本批**没有出现可判 D 的行**。

## 二、结论分布

| 建议等级 | 条数 | 含义 |
|---|---|---|
| **B（真偏离）** | **25** | 落点是真实实现方法，但 `差异` 里有**实打实的缺口**（少分支 / 少数据 / 少刷新口） |
| **落点失效** | **18** | 引用的 `文件:行` 不是实现方法（注释行 / 字段 / 属性 / 语句 / `{` / 类声明），或「无方法落点」——**按方案 §4.1 一律不判 A**，需重指落点 |
| **A（等价）** | **2** | #7 `SetScoringMilestones` · #43 `MessageDisplay.Update`（行为等价；分别还差一条断言 / 一个 dormant 逐帧机制） |
| 判据缺 | **0** | 45/45 原版方法体都在 `decomp_full` |

## 三、18 条「落点失效」逐条（都是行号/落点问题，不是判「没做」）

> 判据 = 打开该 `文件:行` 读到的内容。这些行**状态仍 B**（原值），但账的落点必须重指，否则落点闸会打回。

| # | 方法 | 落点指向的行实际是 | 真实现方法在哪 |
|---|---|---|---|
| 5 | AllianceMemberTab.Initialize | `Debug.LogWarning(...)`（:273） | `Build()` 在 `AllianceMemberTab.cs:281` |
| 8 | AllianceScoreBarLine.ShowChestOpen | `bool loggedExcess = false;`（:204，语句） | `SwapChestOpen(int)` 在 `AllianceEventScoreInfo.cs:248` |
| 16 | AllianceView.OnEditCancel | 注释行（:1406） | 无对应（Cancel 只 Say） |
| 17 | AllianceView.OnToggleEditClicked | 注释行（:1406） | 无对应（Edit 只 Say） |
| 18 | AllianceView.Start | `}`（:1388） | 下拉在 `Dropdown(...)`（:1489）+ `:1360/:1363` 调用 |
| 20 | AlliancesManager.GetCurrentPlayerAllianceName | **无 file:line**（数据在 `SocialData.AllianceName` **字段**） | 需新增真实取值方法 |
| 21 | AlliancesManager.GetCurrentTrophies | **无 file:line**（数据在 `SocialData.AllianceTrophies` **字段**） | 需新增真实取值方法 |
| 22 | BaseInboxEvent.GetMessageContent | `public sealed class Msg`（:75，类型声明） | 取值在 `InboxData` 的字段对位，无方法 |
| 24 | ChatManager.GetChatSoundAsset | 注释行（:9846） | `SpeakIntro` 的真声明在别处 |
| 25 | ChatMessageData.GetMessage | `{`（:107） | 无对应方法（模板未实现） |
| 29 | ChatMessageUI.SetMessageText | `{`（:107） | 无对应方法 |
| 30 | ChatMessageUI.UpdateTimer | `public class ChatMessage`（:106，类声明） | 无对应方法 |
| 31 | ChatPlayerOptionsPanel.SetPositionInScreen | `static readonly PxRect OptPanelR = ...`（:98，**字段**） | 无对应方法（面板恒序列化矩形） |
| 32 | ChatPreview.Show | 注释行（:2348） | 无对应方法（只落终态） |
| 33 | ChatPreview.UpdatePreviews | `TickNavPanelTween();`（:2255，语句） | 无对应方法（两行写死占位串） |
| 34 | ChatPreviewMessage.Set | `TickNavPanelTween();`（:2255，语句） | 无对应方法 |
| 38 | FriendsData.IsFriend | `public static List<Friend> Friends {...}`（:47，**属性**） | 无 IsFriend（且 Friend 无 id） |
| 44 | ProfileAllianceDisplay.Initialize | 注释行（:661） | 只落地「恒关」一支 |

> ⚠️ 「落点失效」**不等于**「行为已经对」——多数同时有真实缺口（见 8 列 TSV 的「一句结论/还差什么」）。它只是说：**账的落点这一格是坏的**，不能据此升 A。

## 四、两条 A 候选（供调度台裁）

- **#7 `AllianceScoreBar.SetScoringMilestones`** —— 落点 `AllianceEventScoreInfo.cs:181`（真方法）；三态逻辑（达成换开箱 / 越档 `alpha*=0.5` / 未达不动）+ 开箱图（`Resources/Art/ui_menu/` 10 张）都已在（历轮 E4/J9SC 已核）。**还差 = 一条断言**（达成档 `Chests[i].texture` 含 `_open` / 越档 alpha×0.5 / 未达不动）。→ 建议 A（补断言即可）。
- **#43 `MessageDisplay.Update`** —— 落点 `Shell/InboxWindow.cs:785` `ShowDetail(Message)`（真方法）。原版是**逐帧状态机**（含未到期倒计时文案），我们「选中那一刻算一次」；出厂两条消息 `RedeemableAtMs=0` ⇒ 倒计时支不触发 ⇒ **现观测等价**。**还差 = 若将来出现 `RedeemableAt>0` 的消息，需补逐帧**。→ 建议 A（dormant 残余，非现缺口）。

## 五、特别说明

- **#45 `CampaignBadge(CardArmy army = 0)`**：调度台 2026-10-27 已裁 **A→B（缺 `army` 字段，真缺口要做）**，本次**未判等价**，维持 B；补法见行内备注（`Shell/BadgeNotifications.cs:75` 加 `public int army;` + provider `:313` 传 `ArmyIcons.Id(CampaignData.Selected)`）。
- **#20 / #21**（`AlliancesManager`）：判据点明「方法体无后端调用（纯本地缓存读取）」⇒ 原判 D 是错的；但落点是**字段**（§四 不允许字段当方法落点）⇒ 归「落点失效」，需新增真实取值方法。属可做（非服务器）。
- **#39 `FriendsTab.ToFocus`**：落点 `OnOpen` 是真方法，但缺的是 **ToFocus() 覆写**（窗口回前台不重刷）⇒ 判 **B**（真偏离），不是落点失效。
- **#8**：行为等价（图在、换图逻辑对），但落点行 `:204` 是语句 ⇒ 归「落点失效」，落点改 `:248` + 补断言后可升 A。

## 六、验证深度与残余（如实）

- 45/45 的**原版方法体存在性**与**落点行内容**已机械核过（见 §三表）。
- 行为层的「逐句比」：`差异` 列是历轮代理（E4 / N9 / J9SC 等）逐句读过原版方法体写下的，本轮**复核其落点与结论一致性**，对 A 候选（#7/#8/#43）另读了本地实现；**未对 25 条 B 行重新逐句重读原版全文**（它们本就有「缺 X」的明写结论，且本轮落点复核与之一致）。
- `check_patch_conflicts.py` → **无跨补丁重复键（4 份补丁）**；补丁 45 行 `状态` 全保持原值 `B`、`轮次`=`B9_C3T4b`、11 列无坏行。
- 补丁只把审计结论**追加**到 `差异`/`备注`，未改 `判据出处`/`判据要点`/`状态`/`我们的落点`（落点重指留给调度台按 8 列 TSV 处理）。
