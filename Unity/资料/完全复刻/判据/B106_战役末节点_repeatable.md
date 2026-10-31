# B106_战役末节点_repeatable — 三问核实

> 只读核实代理 `B106_R12` · 2026-10-31 · **没改任何源码 / 账 / 正本**。
> 判据源：① `d:/2/tools/decomp_full/`（第一权威）· ② `d:/2/tools/cpp2il/out_il2csharp/`（对照）·
> ③ `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/`（**数据资产，只读**）。

## 结论速览

| # | 问题 | 结论 |
|---|---|---|
| 1 | 原版 `CampaignNodeData.IsRepeatable` 的语义 | **「这一格可以无限重复领取」**——不是「只是个标记」：`Collect` 的闸放行 `{unlocked, repeatable}` 两态；`GetNodeState` 把已领的 20/30 **覆盖成 40(repetable)**；`AvailableNodes` 对 repeatable **不再按「已领」过滤** |
| 2 | 原版 `UM47` 的 `Repeatable` 值 | **`IsRepeatable = 1`（true）**。47 个节点里**唯一**一个；且是**末节点**（`nexts = []`）·`Cost = 1100`·奖励 1×`Booster Pack Ultramarines` |
| 3 | 红点遇到 repeatable 怎么算 ·「全领完 ⇒ 红点灭」可达吗 | 红点 = `CampaignHandler.CheckNotification()` = **`AvailableNodes.Any()`** ⇒ 只要还有**一个可负担的**可领节点（含 repeatable）就发**一条** `CampaignBadge`。**「全领完 ⇒ 红点灭」不可达**：UM47 永久留在 `AvailableNodes` 里（只要余额 ≥ 1100） |

---

## 问 1 · `IsRepeatable` 的语义

字段本身是光秃秃的布尔：`out_il2csharp/Assembly-CSharp/CampaignNodeData.cs:17` `public bool IsRepeatable; // 0x58`
（运行期 DTO 侧是自动属性：`Everguild/LiveOps/CampaignNodeInfo.cs:19` `private bool __field_IsRepeatable; // 0x40`
→ `:114-125` 属性 get/set 只读写该字段 → `:180` `this.__field_IsRepeatable = data.IsRepeatable;` 由 `CampaignNodeData` 直接拷贝）。
**语义要看消费它的三处代码：**

### ① 领取闸：`CampaignNode.Collect` 只放行 `{unlocked(10), repeatable(40)}`

- `out_il2csharp/Assembly-CSharp/CampaignNode.cs:217-260`（`Collect`）：
  `if (state == unlocked) goto L; ... else if (state == repetable) goto L; else Debug.Log("Trying to collect node that is either locked or already collected");`
  落在 `L` 上做的是 `state = (tier != 0) ? 30 : 20; SetNodeStyle(); 把每个 locked 的相邻节点置 10`。
- 机器码同结论：`decomp_full/CampaignNode__Collect.c:28`
  `if ((*(int *)(param_1 + 0x38) == 10) || (*(int *)(param_1 + 0x38) == 0x28))`（`0x28 = 40`；`+0x38 = State`）。

⇒ **已领过的 repeatable 节点（state=40）仍可再领**；其余已领态（20/30）会被 `Collect` 拒绝并打日志。

### ② 状态折算：`CampaignTrack.GetNodeState` 把 20/30 覆盖成 40

- `out_il2csharp/Assembly-CSharp/CampaignTrack.cs:137-163`：
  `if (premiumCollected.Contains(node)) { num1 = 30; state = IsRepeatable ? 40 : num1; }`
  `else if (collected.Contains(node)) { num1 = 20; state = IsRepeatable ? 40 : num1; }`
  `else if (UnlockedBy.Count <= 0) state = 10; else state = Any(collected, pred) ? 10 : locked;`
- 机器码同结论：`decomp_full/CampaignTrack__GetNodeState.c` 的 `LAB_1806b9a30`：
  `if (*(char *)(lVar4 + 0x40) != '\0') uVar3 = 0x28;`（`lVar4 = CampaignNodeInfo`，`+0x40 = IsRepeatable`）。
- 枚举值：`out_il2csharp/Assembly-CSharp/CampaignNode.cs:402-409`
  `locked=0, unlocked=10, baseCollected=20, allCollected=30, repetable=40`。

⇒ 「领过」这件事对 repeatable 节点**不表达成 20/30，而恒表达成 40** ⇒ UI 上它永远停在「可再领」那一态。

### ③ 可用集合：`Campaign.AvailableNodes` 对 repeatable 不再看「已领」

- `out_il2csharp/Assembly-CSharp/Everguild/LiveOps/Campaign.cs:71-82`（`AvailableNodes`）= `Nodes.Where(p0).Where(p1)`
- `:140-151`（`_get_AvailableNodes_b__7_0`）：
  `if (!x.__field_IsRepeatable) return !collected.Contains(x.__field_NodeId); return true;`
  **⇒ repeatable 节点永远返回 true（不看 collected）。**
- `:153-160`（`_get_AvailableNodes_b__7_1`）：
  `return x.__field_NodeCost <= AvailablePoints;`
- `:84-92`（`AvailablePoints`）= `Handler.NeutralPoints + persistence.ArmySpecificPoints`；
  `decomp_full/CampaignPersistence__get_ArmySpecificPoints.c` = `*(int*)(this+0x44) - *(int*)(this+0x40)` = **已获得 − 已花费 = 当前余额**
  ⇒ 这里的 `AvailablePoints` 是**可花的余额**，不是累计获得。

---

## 问 2 · 原版 `UM47` 的 `Repeatable` 值

**判据（数据资产，逐字段现读）**：
`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/Campaign Node Data_4535086074611093881.json`

| 字段 | 值 |
|---|---|
| `NodeId` | `"UM47"` |
| `Cost` | `1100` |
| `IsRepeatable` | **`1`** |
| `nexts` | `[]`（**末节点**，没有后继） |
| `unlocks` | 1 条 → `m_PathID 565739088098152825`（= 节点 `UM46`） |
| `Rewards[0]` | `targetId "Booster Pack Ultramarines"` · `quantity 1` · `rewardTier 0` |

**全量对账（47 个 `Campaign Node Data*.json`，脚本现读）**：
- 只有 `UM47` 的 `IsRepeatable = 1`，其余 46 个都是 `0`；
- `Cost` 范围 100…1100，`UM47` 是最贵的一格（1100）；
- 47 个节点 `NodeId` 前缀全为 `UM`（缺 `UM14`，原版就没有这个 NodeId）；
- `find /d/2/新解包资源/assets_full -name "Campaign Node Data*"` ⇒ **只在 `bundle_menus_assets_all/MonoBehaviour` 一处，47 个文件**；`grep -rl UMCampaign` 全库 ⇒ 只有同一目录的 `Ultramarines Campaign.json`。**本地只有 Ultramarines 一套战役**。

**它确实是「末节点」——两条独立证据**：
1. `Campaign.IsComplete` = `basicList.Contains(Last(GetCampaignNodes()).NodeId)`
   （`out_il2csharp/.../Campaign.cs:34-45`；`decomp_full/Everguild.LiveOps.Campaign__get_IsComplete.c` 里 `Enumerable.Last(nodes)` + `List.Contains(...)`）。
2. 编辑器图资产 `bundle_menus_assets_all/MonoBehaviour/UM.json`（`CampaignGraph : NodeGraph`，`m_Name "UM"`，`nodes` 47 项）
   —— 按 `m_PathID → NodeId` 映射后**表末项就是 `UM47`**（现读：`last12 = [UM41, UM42, UM43, UM44, UM45, UM46, UM5, UM6, UM7, UM8, UM9, UM47]`）。
   ⚠️ 该顺序即 `CampaignEventData.OnBeforeSerialize()` 用的 `Graph.nodes` 顺序（`out_il2csharp/.../CampaignEventData.cs:26-40`）。

---

## 问 3 · 红点 / 可领判定遇到 repeatable 怎么算 ·「全领完」可达吗

### 3·1 红点 = `AvailableNodes.Any()`，发【一条】`CampaignBadge`

- `out_il2csharp/Assembly-CSharp/Everguild/LiveOps/CampaignHandler.cs:398-448`（`_CheckNotification_d__21.MoveNext`）：
  ```
  Campaign campaign1 = GetCampaign(SelectedArmy);
  Campaign campaign2 = campaign1;
  if (campaign1 == null) campaign2 = GetCampaign(campaign1 + 0xa);   // 反编译噪声：实参是字面量 10
  var q = Nodes.Where(campaign2._get_AvailableNodes_b__7_0).Where(campaign2._get_AvailableNodes_b__7_1);
  if (!q.Any()) return false;                                        // 没有可领 ⇒ 不产出
  yield return new CampaignBadge(SelectedArmy);                      // 有可领 ⇒ 只发一条
  ```
- 机器码同结论：`decomp_full/Everguild.LiveOps.CampaignHandler._CheckNotification_d__21__MoveNext.c`
  （两次 `Enumerable.Where` 用的是 `campaign._get_AvailableNodes_b__7_0` / `_b__7_1` 两个委托，再 `Enumerable.Any`）。
- **两条 `AvailableNodes` 谓词就是本文件问 1·③ 那两条** ⇒ **repeatable 节点只要可负担，就永远是「可领」**，红点每次重算都会重新亮。

### 3·2 领取后会不会重算红点：会

`out_il2csharp/.../Campaign.cs:270-275`（`_CollectRewards_g__OnCollect_2` 末段）：
`Signal.Raise(new CampaignNodeCollectedSignal(...)); UiBadgeNotificationManager.Refresh<CampaignBadge>();`
⇒ 每次领取成功都重算一遍，而重算时 repeatable 节点仍然命中 ⇒ 红点仍然亮。

### 3·3 「全领完 ⇒ 红点灭」**不可达**

- 「全领完」在数据上 = 46 个普通节点都进了 collected + UM47 进了 basicList。
- 此时 `AvailableNodes` 里：46 个普通节点被 `!collected.Contains` 滤掉；**UM47 因为 `IsRepeatable=true` 直接返回 true**，
  只剩 `Cost <= AvailablePoints` 一道闸（1100）。
- ⇒ **只要余额 ≥ 1100，红点就还在**；再领一次 UM47 也只是把它再写进 basicList（**不会让红点灭**）。
  真正的「红点灭」只有两种，**都不是「全领完」**：
  ① **余额 < 最便宜的可领节点 `Cost`**（原版真实经济，点数是余额）——但此时普通节点也够不着，属于「点不够」，不是「都领了」；
  ② **`GetCampaign(SelectedArmy)` 与回退的 `GetCampaign(10)` 都取不到战役**。

### 3·4 ⚠️ 一个容易漏的原版行为：红点会**回退到 Ultramarines(10)**

`decomp_full/Everguild.LiveOps.CampaignHandler__GetCurrentCampaign.c`：
`lVar1 = GetCampaign(this, *(int*)(this+0x7c) /*SelectedArmy*/, 0); if (lVar1 == 0) { GetCampaign(this, 10, 0); return; }`
`CheckNotification` 的迭代器里是同一条（`decomp_full/..._CheckNotification_d__21__MoveNext.c` 的第二次 `GetCampaign(lVar2,10,0)`；
`out_il2csharp/.../CampaignHandler.cs:418` 的反编译写作 `GetCampaign(campaign1 + 0xa)` —— 因为 `campaign1` 是 null 引用，等价于字面量 `10`）。
`CardArmy.Ultramarines = 10`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs:1-17`）。
⇒ **原版：选中一个「没有战役」的阵营时，红点会拿 Ultramarines 那套战役来算**（`new CampaignBadge(SelectedArmy)` 仍打 `SelectedArmy` 的过滤标签）。
⇒ 因此「切到无战役阵营 ⇒ 红点 alpha 0」**不是原版行为**（只要 UM 战役存在且它有可领节点，红点照样亮）。

---

## 证据清单（可直接复查）

| 结论 | 出处 |
|---|---|
| `CampaignNodeData.IsRepeatable` 字段 | `out_il2csharp/Assembly-CSharp/CampaignNodeData.cs:17` |
| `CampaignNodeInfo.IsRepeatable` 只做拷贝 | `out_il2csharp/Assembly-CSharp/Everguild/LiveOps/CampaignNodeInfo.cs:19,114-125,180` |
| `Collect` 闸 = {10, 40} | `out_il2csharp/Assembly-CSharp/CampaignNode.cs:217-260` · `decomp_full/CampaignNode__Collect.c:29-33` |
| `GetNodeState` 20/30→40 | `out_il2csharp/Assembly-CSharp/CampaignTrack.cs:137-163` · `decomp_full/CampaignTrack__GetNodeState.c`（`LAB_1806b9a30`） |
| `AvailableNodes` 两条谓词 | `out_il2csharp/Assembly-CSharp/Everguild/LiveOps/Campaign.cs:71-82,140-160` |
| `AvailablePoints` = 余额 | `out_il2csharp/.../Campaign.cs:84-92` · `decomp_full/CampaignPersistence__get_ArmySpecificPoints.c` · `CampaignHandler.cs:157-161`（`SpendNeutralPoints` 做减法） |
| 红点 = `AvailableNodes.Any()` → 一条 `CampaignBadge(SelectedArmy)` | `out_il2csharp/.../CampaignHandler.cs:398-448` · `decomp_full/Everguild.LiveOps.CampaignHandler._CheckNotification_d__21__MoveNext.c` |
| 领取后重算红点 | `out_il2csharp/.../Campaign.cs:270-275` |
| 红点回退 `GetCampaign(10)` | `decomp_full/Everguild.LiveOps.CampaignHandler__GetCurrentCampaign.c` · `…_CheckNotification_d__21__MoveNext.c` · `CardArmy.cs:1-17` |
| `Campaign.IsComplete` = collected 含末节点 | `out_il2csharp/.../Campaign.cs:34-45` · `decomp_full/Everguild.LiveOps.Campaign__get_IsComplete.c` |
| `UM47` 数据（Cost 1100 · IsRepeatable 1 · nexts 空） | `bundle_menus_assets_all/MonoBehaviour/Campaign Node Data_4535086074611093881.json` |
| 47 节点 / 唯一 repeatable / 仅 UM 一套 | 同目录 `Campaign Node Data*.json`（47 个）+ `Ultramarines Campaign.json` + `UM.json` |
| 图顺序末项 = UM47 | `bundle_menus_assets_all/MonoBehaviour/UM.json`（`nodes` 47 项） |
| 每级 100 点 | `Ultramarines Campaign.json` 的 `PointsPerLevel = 100` |

---

## 还差什么判据

1. **服务端下发的 `CampaignEventData.Nodes`**：本地的 `Ultramarines Campaign.json` **没有 `Nodes` 字段**（`OnAfterDeserialize` 是空函数，`Nodes` 只在 `OnBeforeSerialize` 从 `Graph.nodes` 生成，见 `out_il2csharp/.../CampaignEventData.cs:26-55`）⇒ 线上真数据来自服务端。我们只能以**编辑器图 `UM.json` 的顺序/值**为准；服务器是否另有改动，**本地判不了**。
2. **其余 12 个阵营的战役节点表**：本地 `assets_full` 只有 Ultramarines 一套 ⇒「原版是否存在『选中某阵营却没有战役』这种状态」本地判不了（`CardArmy` 13 个阵营都在，但战役数据只导出了一套）。
3. **玩家点数余额 / 存档**：`ArmySpecificPoints` 与 `NeutralPoints` 都来自服务端存档，本地无真值 ⇒「余额不足 ⇒ 红点灭」只能从代码语义确认，**无法实测**。

---

## 对我方代码的观察（只读，**未改任何文件**）

| 我方位置 | 现状 | 与原版对照 |
|---|---|---|
| `Shell/CampaignData.cs:209-210`（`StateOf`） | 20/30 被 `Repeatable` 覆盖成 40 | ✅ 与原版 `GetNodeState` 一致 |
| `Shell/CampaignData.cs:217`（`Claimable`） | `State ∈ {Unlocked, Repeatable}`，**无点数闸** | 原版还有 `AvailablePoints >= NodeCost`；我们 `Points = 9999` 恒够 ⇒ **当前无观测差异**（🔶自设计已在源码注明） |
| `Shell/BadgeNotifications.cs:378-392`（`CampaignNotificationProvider`） | `if (!CampaignData.HasContent(Selected)) yield break;`，找任一 `Claimable(i)` 就发一条 | 原版**没有 HasContent 闸**，而是回退到 `GetCampaign(10)` ⇒ **「切到无战役阵营 ⇒ 红点 0」是我们本地的近似，不是原版**（如要照抄原版，需要补 UM 回退） |
| `Shell/CampaignData.cs:269-273`（`AllNodesClaimed`） | `_base[末项]` | ✅ 与原版 `IsComplete` 一致 |

---

## 附：`UM47` 的原始 JSON 关键片段（逐字）

```json
"NodeId": "UM47",
"nexts": [],
"Cost": 1100,
"Rewards": [ { "item": { "targetId": "Booster Pack Ultramarines", ... }, "quantity": 1, "rewardTier": 0 } ],
"IsRepeatable": 1
```
