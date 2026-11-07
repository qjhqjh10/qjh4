# B166 · 诊断：任务 Re-Roll 按钮 NRE（`OFF-38` 新残留）

> 2026-11-07 · **只读诊断**（未改代码 / 配置 / 任务表 / mod，未启游戏 / 服务端 / Unity，未动 git）
> 输入：`E:/warpforge/game/MelonLoader/Latest.log:448-462`（12:08:41.635 栈）·
> `d:/4/_tmp_view/shots/1107_verify/04_rewards.png` ·
> `E:/warpforge/05_工具/server/tools/add_all_configs.py` ·
> `E:/warpforge/05_工具/server/responses/ExecuteCloudScript.InitializeLiveOps.json`（现读）·
> 判据源：`d:/2/tools/decomp_full/*.c`（第一）· `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs` ·
> `d:/2/tools/il2cpp_out/dump.cs` · `d:/2/tools/assetripper_out/ExportedProject/Assets/{Daily Mission Container,Missions Tab}.prefab`
> **§五 列了没查清的部分，⛔ 没有拿猜测填。**

---

## 〇、一句话结论

| # | 现象 | 根因一句话 | 归属 |
|---|---|---|---|
| **OFF-38-4** | 奖励页打开抛 `MissionReRollButton.Setup` NRE | 我们喂的 `HandlerData.Missions.RawEvents[0].Data`（`MainMenuMissionData`）**没有 `rerollPrice` 字段** ⇒ `MissionData.rerollPrice`（`+0x90`）是 **null**，而 `Setup` 对它是**无条件解引用**（`data.RerollPrice.currency.Reference` / `.amount`）。`MissionData` 的 ctor **不建**这个对象（见 §二-2） | **我们的数据缺陷**，不是原版行为 |
| **OFF-38-3（仍开）** | 英文 `Reset / Re-Roll / +1 / +5 / Complete` + `00:00` | **同一 NRE 的连带**：那一组挂在 `Mission Debug Buttons` 节点上，`MissionDebugButtons.Setup()` 的第一句就是 `gameObject.SetActive(false)`，而它和 ReRoll 按钮**在同一个 `MissionInfoDisplay[]` 循环里**，NRE 把循环打断 ⇒ 它没跑到（`MissionInfoDisplay.cs:47-52`） | 连带 |
| **OFF-38-2′** | 右上角仍 `0 Disponible` | **同一 NRE 的连带**：`MissionsTab.CreateMissions()` 的**最后一句**就是 `RefreshRefillCount()`（`MissionsTab.cs:490`），NRE 在它之前的 `InstantiateNormalMissions` 里抛出 ⇒ 这句从未执行 ⇒ 标签停在 prefab 烤死的西语占位（`Missions Tab.prefab:36218`） | 连带 |

> ⚠️ 这一条**不是**「原版自身行为」：`rerollPrice` 是 `MissionData` 的**序列化字段**（`dump.cs:60452`），原版服务器一定会下发它；prefab 上 ReRoll 按钮的 `button`/`reRollPopup` 也都是填好的（§二-4）。缺的只是**我们的服务端数据**。

---

## 一、五个问题的直接回答

1. **`MissionReRollButton.Setup` 解引用的是哪个 null？**
   = **`IMission.GetBaseData()` 返回的 `MissionData` 上的 `rerollPrice`（`+0x90`）**，也就是 `Price` **对象本身**是 null。
   不是 `progress`（`Setup` 里只把它存进闭包，不读）、不是 `challenge`（本方法不碰）、也没有 `rerollCost` 这个字段（原版字段叫 `rerollPrice`，里面才有 `currency` / `amount`）。
   证据链见 §二-1～§二-2。

2. **我们哪一格缺了？**
   `MISSION_DAILY_EVENT["Data"]`（`add_all_configs.py:812-843`）里**整条 `rerollPrice` 都没有**（现读生成物 `ExecuteCloudScript.InitializeLiveOps.json` 也一样没有）。
   原因写在 `add_all_configs.py:784-786` 的字段清单里 —— 那份清单**漏列了 `rerollPrice`**（`施工_奖励与邮件.md:309` 的字段表里是有的）。

3. **最小修法**：给 `OfflineDailyMission.Data` 补
   `"rerollPrice": {"currency": "C1", "amount": 0}`
   —— 数据侧一处，⛔ 不动 UI prefab、⛔ 不动 mod。详见 §三。

4. **`0 Disponible` 与英文按钮是否同因？**
   **是**，同一条被中断的 `Open` 链（`CreateMissions` 的最后一句 `RefreshRefillCount()`；`MissionDebugButtons.Setup` 在同批 `Initialize` 循环里）。
   判据：`MissionsTab.cs:490`（`RefreshRefillCount()` 是 `CreateMissions` 尾句）· `MissionDebugButtons.cs:40`（`SetActive(false)`）· `MissionsTab.cs:668`（`enabled = (sum > 0)`，我们的 `maxStoredRefill=0` ⇒ 修好后这个标签应当**消失**）。

5. **是不是原版自身行为？**
   **不是。** 原版 `MissionData.rerollPrice` 是 `[SerializeField]` 的服务端字段，原版服务器必然下发；prefab 的按钮引用也齐全。⇒ **属于「我们没复刻到的那一格」**，按铁律 11 要补，**不能写「照抄原版、不改」**。
   ⚠️ 只有**具体金额/货币**本身是服务端数据（关服，判据拿不到）⇒ 那一格是 `🔶 自己设计`。

---

## 二、根因与证据

### 2·1 栈与现场

`E:/warpforge/game/MelonLoader/Latest.log:448-462`：

```
System.NullReferenceException
  at MissionReRollButton.Setup (IMission mission, MissionChallengeProgress progress)
  at MissionInfoDisplay.Initialize (...)      ← 已过 SetActive(true)，见 2·5
  at MissionContainer.SetChallenge (...)
  at MissionsTab.InstantiateNormalMissions (...)
  at MissionsTab.CreateMissions ()
  at BaseParentEventWindow.Open ()
  at MainMenuRewardsWindow.Open ()
  at GameWindow.TryOpen(...) → WindowsManager.OpenWindowCO → WindowsManager.OpenWindow
```

（`MissionsTab.OnOpen` 帧不在栈里 = 被内联进 `BaseParentEventWindow.Open`；`OnOpen` 的本体只有一句 `CreateMissions()`，`MissionsTab.cs:254-257`。）

### 2·2 `Setup` 的语义：解引用 `MissionData.rerollPrice`

**判据 A — C 反编译**（`d:/2/tools/decomp_full/MissionReRollButton__Setup.c:29-59`，逐句直译）：

```c
if (*(longlong *)(lVar2 + 0x18) != 0) {              // closure.mission != null
  lVar3 = FUN_1800021f0(0, DAT_1842a8580);           // ← mission.GetBaseData()（数据对象）
  lVar4 = UnityEngine_Component__get_gameObject(param_1,0);
  if ((lVar3 != 0) &&
      (*(longlong *)(lVar3 + 0x90) != 0) &&          // ← data.rerollPrice（Price 对象）!= null
      (lVar5 = *(longlong *)(*(longlong *)(lVar3 + 0x90) + 0x10), lVar5 != 0)) {  // rerollPrice.currency != null
        lVar5 = LoadableReference<object>__get_Reference(lVar5);   // currency.Reference
        if (lVar5 == 0) bVar1 = false;                             // ← 唯一"不算错"的 null 分支
        else { bVar1 = 0 < *(int *)(*(longlong *)(lVar3 + 0x90) + 0x18); }  // rerollPrice.amount > 0
        ...
        UnityEngine_GameObject__SetActive(lVar4, bVar1, 0);
        ...
        UnityEngine_Events_UnityEvent__AddListener(lVar3, uVar6, 0);        // button.m_OnClick.AddListener
  }
}
FUN_1803f47a0();   // ← :57-59，= il2cpp_codegen_raise_null_reference_exception
```

要点：`data` / `price` / `price.currency` 三处是**硬 null 检查（失败即抛）**；
只有 `Reference == null` 会走成 `bVar1 = false`（按钮隐藏，**不抛**）。
⇒ **只要 `data.rerollPrice` 是 null 就必抛 NRE**，与我们看到的现象一致。

**判据 B — 字段偏移**（`d:/2/tools/il2cpp_out/dump.cs`）：

| 位置 | 内容 |
|---|---|
| `dump.cs:60424` | `public abstract class MissionData : LiveOpsEventData` |
| **`dump.cs:60452`** | **`protected Price rerollPrice; // 0x90`** ← 就是 C 里那个 `+0x90` |
| `dump.cs:60478` | `public Price get_RerollPrice()` |
| `dump.cs:56611-56615` | `class Price { public LoadableReference<Currency> currency; // 0x10 ; public int amount; // 0x18 }` ← 与 C 的 `+0x10` / `+0x18` 逐一对上 |
| `dump.cs:60393` / `61629` | `MainMenuMissionData : TimedMissionData : MissionData`，**两个子类都没有自有字段** ⇒ `+0x90` 就是 `rerollPrice` |

**判据 C — `rerollPrice` 不会被自动建出来**：
`d:/2/tools/decomp_full/MissionData__.ctor.c:16-22` 只写了 `+0x60` / `+0x68` / `+0x8a`（两个 List + `repeatable`），
**全篇 26 行没有一处给 `+0x90` 赋值**；`MissionData__get_RerollPrice.c:2-6` 就是裸读 `return *(undefined8 *)(param_1 + 0x90);`
⇒ JSON 不给 ⇒ **null**。
（`TimedMissionData__.ctor.c` / `MainMenuMissionData__.ctor.c` 都只是转调基类，也不会补。）

**判据 D — C# 反编译同形**（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/MissionReRollButton.cs:51-118`）：
`:68-70` 闭包里存 `mission` / `progress`；`:71-95` 是那次「取 Price → 取 currency → 取 Reference → 比较 amount」；
`:96` `gameObject.SetActive(currency != null && amount > 0)`；`:117` `m_OnClick.AddListener(...)`。
（Cpp2IL 在这里丢了 `GetBaseData()` 的调用名，标成 `NoteDecompilerIssue("Method not found @1800021F0")`；
`0x1800021F0` 在 `script.json` 里**不是任何脚本方法**（RVA 0x21F0 查询结果为空）⇒ 是 IL2CPP 运行时/泛型分派辅助，不影响上面这条链。）

### 2·3 我们的数据：`rerollPrice` 根本没写

**现读**（`E:/warpforge/05_工具/server/responses/ExecuteCloudScript.InitializeLiveOps.json` → `FunctionResult.HandlerData.Missions.RawEvents[0].Data`）：

```json
{"$type":"MainMenuMissionData, Assembly-CSharp","eventId":"OfflineDailyMission", ...,
 "missionType":1,"maxActiveCount":4,"maxStoredRefill":0,"delayedRefill":0,"refreshTime":0,
 "autoReset":false,"preventComplete":false,"repeatable":false,"keepOrder":false,
 "challenges":[],"challengeLists":[]}
```

⇒ 没有 `rerollPrice`（也没有 `premiumOffer` / `premiumItem`，那两个在 `Setup` 里不用）。

**源脚本同一处**：`E:/warpforge/05_工具/server/tools/add_all_configs.py:812-843`（`MISSION_DAILY_EVENT["Data"]`），
`:831` `missionType` … `:839` `keepOrder` … `:840` `challenges`。
**为什么会漏**：`:784-786` 的字段清单写成
`missionType / maxActiveCount / maxStoredRefill / delayedRefill / refreshTime / autoReset / preventComplete / repeatable / keepOrder / challenges / challengeLists`
—— **漏了 `rerollPrice`**；而判据正本 `E:/warpforge/资料/离线原版/判据/施工_奖励与邮件.md:297-313` 的字段表里**有**
（`:309 rerollPrice Price`）。⇒ 写手照了脚本里那份缩略清单，没照正本。

### 2·4 排除「prefab / button 为 null」这一支

`d:/2/tools/assetripper_out/ExportedProject/Assets/Daily Mission Container.prefab`（= `EventComponents[key=0]` 指的 `b07892dd…`）：

| 行 | 内容 |
|---|---|
| `:3769-3773` | `MissionReRollButton` 组件：`displayRule: 1`（WhenActive）、`button: {fileID: 114271925019662038}`（**非 null**）、`reRollPopup: {m_AssetGUID: 9771e6c66b9b65e4ea4223bbb436d6a4}`（非空） |
| `:3813` | 被引用的那个 `EverguildButton` 实例确实存在 |
| `:588-608` | 按钮 GameObject `m_Name: Trash mission`，`m_IsActive: 1`（prefab 里默认显示 —— 截图里卡右侧那个垃圾桶图标就是它） |
| `:4119-4136` | `MissionDebugButtons` 组件（`debugResetButton` / `debugRerollButton` / … 全非空），挂在 GO `1875609422105747` = `Mission Debug Buttons`，`m_IsActive: 1` |

⇒ prefab 侧没有 null；**唯一缺的就是数据里的 `rerollPrice`**。

### 2·5 为什么「卡片还渲染出来了」而按钮炸了

`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/MissionInfoDisplay.cs:13-52`：

```csharp
bool flag2 = <displayRule 判断>;        // displayRule=1(WhenActive) + 未完成 ⇒ true
GameObject gameObject = base.gameObject;
gameObject.SetActive(flag2);            // :47  ← 先把按钮显示出来
if (flag2) { Setup(mission, progress); } // :50  ← 在 Setup 里才炸
```

⇒ 按钮先被 `SetActive(true)`（prefab 本来就 active），`Setup` 抛在 **`SetActive` 之后、`AddListener` 之前** ⇒
画面上按钮在、但**没挂点击回调**；同时异常从 `MissionContainer.SetChallenge` 的 `for` 循环里冒出去
（`decomp_full/MissionContainer__SetChallenge.c:66-79` 是那个 `infoDisplays` 循环），
循环后面那些 display（含 `MissionDebugButtons`）**一个都没跑**。

---

## 三、最小修法（数据侧一处）

### 3·1 改什么

在 `E:/warpforge/05_工具/server/tools/add_all_configs.py` 的 `MISSION_DAILY_EVENT["Data"]`
（`:839` 的 `"keepOrder": False,` 之后、`:840` 的 `"challenges": [],` 之前）插一行：

```python
        "repeatable": False,
        # 🔶 自己设计（非原版）：重掷价格 · 服务端数据，判据在远端（关服）·
        #   `amount=0` ⇒ `Setup` 里 `0 < amount` 为假 ⇒ 按钮隐藏（不暴露未实现的 PaidRerollMission 链）
        "rerollPrice": {"currency": "C1", "amount": 0},
        "keepOrder": False,
```

**为什么是 `C1`、为什么是 `0`**：

- `Price.currency` 是 `LoadableReference<Currency>`，JSON 走**裸字符串 = `Currency.code`**
  （判据：`施工_奖励与邮件.md` 段 12 的 `Price` 说明 + 现成先例 `add_all_configs.py:624`
  `"createAlliancePrice": {"currency": "C1", "amount": 0}`）。
- `C1` 就是我们 `CurrencyConfig.currencies` 里那条 `code="C1"` / `type=40(free)` 的货币（`add_all_configs.py:942-953`，`code` 在 `:945`）。
  `CurrencyAssetProvider.GetAssets()` = `ConfigManager.GetConfig<CurrencyConfig>().currencies`
  （`decomp_full/CurrencyAssetProvider__GetAssets.c:15-17`）⇒ `AssetLocator` 能按 `C1` 找到它。
- `amount = 0` ⇒ `0 < 0` 为假 ⇒ `MissionReRollButton` 的 GameObject 被 `SetActive(false)`，**不进任何后续路径**。
  ⛔ **不要给 `amount > 0`**：那样按钮会显示并可点，点了会走 `PaidRerollMission`（CloudHandle 904，
  见 `施工_奖励与邮件.md:370`），而**我们没有这个 CloudScript 模板**（`responses/` 现读无
  `ExecuteCloudScript.PaidRerollMission.json`，会落到兜底 `ExecuteCloudScript.json` 的 `Error.NotImplemented`）
  ⇒ 变成「按钮能点但点了报错」的**新缺陷**。想让按钮真可用是**另一件事**（要服务端随机换 challenge + 模板），记在 §四。

### 3·2 ⚠️ 只改脚本**不够** —— 必须先手工补 JSON

`add_all_configs.py:1047-1060` 的事件合并是**按 `eventId` 幂等**的（`:1057-1058`「已存在（跳过）」）⇒
⚠️ **重跑 `--write` 不会更新已有的那条 `OfflineDailyMission`**，改动会被静默吞掉。

所以两条都要做（顺序无所谓，但**两处都改**，否则下次重生成又丢）：

1. **脚本**：按 3·1 插那一行（保持源头一致）。
2. **现网模板**：`E:/warpforge/05_工具/server/responses/ExecuteCloudScript.InitializeLiveOps.json`
   → `FunctionResult.HandlerData.Missions.RawEvents[0].Data` 里加同一个键：
   ```json
   "rerollPrice": {"currency": "C1", "amount": 0},
   ```
   （写盘前按规矩先留 `.bak_before_<批次>`；写盘用 `io.open(...,'w',encoding='utf-8',newline='\n')` + `json.dump(..., ensure_ascii=False, indent=2)`，
   与脚本 `write_json()` 一致；⛔ 别用 `python -c "…"` 拼含中文的字符串，见 CLAUDE.md §二。）

   > 想省掉手改 JSON 也可以：把 `HandlerData.Missions.RawEvents` 里那条 `OfflineDailyMission` **删掉**再 `--write`
   > （会按新内容重新 append）。但 `--write` 同时会用 `MINIMAL` **整体重写 TitleData 与 profile 片段**，
   > 之前手调过的值会被打回脚本值 ⇒ **不推荐**，除非确认没有别的手工修订。

**服务端不用重启**：模板按 mtime 热重载（`server.py:173-231`）。**游戏要重启**（任务 handler 在登录时 `InitializeLiveOps` 里建）。

### 3·3 验收判据（三条硬判据）

| # | 判据 | 怎么验 |
|---|---|---|
| **1** | 数据里有这一格 | `python -I -X utf8 -c "import json;d=json.load(open(r'E:/warpforge/05_工具/server/responses/ExecuteCloudScript.InitializeLiveOps.json',encoding='utf-8'));print(d['FunctionResult']['HandlerData']['Missions']['RawEvents'][0]['Data'].get('rerollPrice'))"` ⇒ `{'currency': 'C1', 'amount': 0}` |
| **2** | **不再有这条 NRE** | 起游戏 → 开奖励页 → `grep -c "MissionReRollButton" E:/warpforge/game/MelonLoader/Latest.log` = **0**，且这一段 `Open` 之后没有新的 `NullReferenceException` |
| **3** | 三条连带一起消失 | 同一张截图（`_tmp_view/shots/<日期>/`）：① `Reset / Re-Roll / +1 / +5 / Complete` 与 `00:00` **不见**（`Mission Debug Buttons` 被 `SetActive(false)`）② 右上 `0 Disponible` **不见**（`RefreshRefillCount` 跑到了，`sum=0 ⇒ enabled=false`）③ 卡右侧那个垃圾桶图标（ReRoll 按钮，`Trash mission`）**不见**（`amount=0`）④ 任务卡仍是「玩 1 场游戏 0/1」 |

⚠️ 判据 3 的 ②③ 是**可预期的外观变化**（隐藏），不是回归。若 ② 仍然在，说明 `Refresh`/`RefreshRefillCount` 还有别的 null（见 §五-3）。

---

## 四、风险与回归面

1. **脚本幂等陷阱**（最容易踩）：改完脚本重跑 `--write` **不会**更新既有事件（`add_all_configs.py:1057`）⇒ 必须同时手补 JSON（§3·2）。
2. **修好之后，同一个循环里另外 8 个 `MissionInfoDisplay` 子类会第一次真正跑起来**
   （`dump.cs:61638-62051`：`MissionBackgroundHighlighter` / `MissionCounterDisplay` / `MissionHeaderDisplay` /
   `MissionMilestonesDisplay` / `MissionProgressBarDisplay` / `MissionRewardsDisplay` / `MissionTimerDisplay` /
   `LoginBonusRewardDisplay`）。静态看它们对**我们这份数据是安全的**：
   · `MissionHeaderDisplay` 读 `mission` 的 `EventLabels`（我们给了 `Missions/Title/DailyMissions`）✓
   · `MissionRewardsDisplay` / `MissionMilestonesDisplay` / `MissionProgressBarDisplay` 走
     `progress.AvailableRewards()`，我们的 `rewards` 为空 ⇒ 不发任何奖励项 ✓
   · `MissionCounterDisplay` 走 `AssetLocator.GetAsset<MissionChallenge>(progress.challengeId)`，
     `Daily_Generic_1` 已注册在 `MissionsConfig.challenges` ✓
   ⇒ **但这是静态推断**，实机要盯下一条 NRE（若出现，优先怀疑上面这层）。
3. **`Refresh()` / `RefreshRefillCount()` 也会第一次跑**（`MissionsTab.CreateMissions` 尾句 → `MissionsTab.cs:490`；
   `Refresh` 见 `:680-782`、`RefreshRefillCount` 见 `:500-678`）。
   它们要求 `dailyMissionsHolder` / `missionsHolder` / `weeklyMissionHolder` 非空 ——
   prefab 里三个都填了（`Missions Tab.prefab:7946-7948`），通过。
4. **`C1` 必须继续留在 `CurrencyConfig.currencies`**。若哪天把这条 free 货币删了：
   `LoadableReference<Currency>.Reference` 会返 null ⇒ 按钮隐藏（**不会** NRE），影响可控。
5. **`amount>0` 是另一件事**：真要让「重掷」可用，需要 ① 服务端能随机换 `challengeId` 并写回 persistence
   ② 新增 `ExecuteCloudScript.PaidRerollMission.json`（响应 DTO `ReRollMissionResult{UpdatedPersistence, Cost}`，
   `施工_奖励与邮件.md:370`）③ `MissionReRollPopup` 的接受链验收。**不属于本次最小修法**，
   但按铁律 11 要记成待办（🔶 服务器相关，可自设计）。
6. **⛔ 不动 prefab**：`Daily Mission Container` 的 `button` / `reRollPopup` 引用是齐全的（§2·4）；
   改 prefab 反而会掩盖真正缺的数据。
7. **`0 Disponible` 会变成「隐藏」而不是「换个数字」**：`enabled = (Σ > 0)`（`MissionsTab.cs:668`），
   我们 `maxStoredRefill = 0` / `availableRefills = 0` ⇒ 和为 0 ⇒ 隐藏。这是原版代码的正常分支，不是我们新加的。

---

## 五、没查清（搜过什么 / 还差什么）

| # | 问题 | 搜过什么 | 还差什么 |
|---|---|---|---|
| 1 | `LoadableReference<Currency>.Reference` 对 `"C1"` 在**运行时**是否真能解析出对象 | `decomp_full/CurrencyAssetProvider__GetAssets.c`（= `CurrencyConfig.currencies`）、`AssetLocator__GetAsset_object_.c`、`LoadableReference<…>__get_Reference/GetReference/OnBeforeRead`（`decomp_generic_trial/out/`） | 泛型共享体用反射槽位读写，静态读不出「一定解析成功」。**只能实机看按钮是否隐藏来反推**。⇒ **不影响修复正确性**：解析失败时走 `bVar1=false`，同样不 NRE |
| 2 | 原版日常任务的**真实 reroll 价格**（货币 + 金额） | `施工_奖励与邮件.md:309`（只有字段名）；`E:/warpforge/02_解包资源`、`d:/2/新解包资源`（Currency 只有图标贴图，无数据 SO）；`d:/2/tools/assetripper_out/ExportedProject` 的 `*.asset` | 服务端数据，关服后本地无抓包 ⇒ **拿不到**，所以这一格按 `🔶 自己设计` 处理 |
| 3 | `MissionsTab.Refresh` 里对每个 `container` 调的那一句（ILSpy 显示 `missionsTab2.OnSetup()`，实际是间接调用）对我们的 container 是否安全 | `MissionsTab.cs:680-782`（ILSpy 被间接调用/泛型槽位糊住）、`decomp_full/MissionsTab__Refresh.c:60-90` | 只读到「取 container → 调虚方法 → 再 `RefreshRefillCount`」，**具体调的是哪个虚方法没读死**。风险低（容器由原版 prefab 实例化），实机看 §3·3 判据 2/3 |
| 4 | `GetComponentsInChildren<MissionInfoDisplay>()` 的返回顺序（决定「哪个 display 先炸」） | `decomp_full/MissionContainer__FetchInfoDisplays.c:12-13`（`UnityEngine_Component__GetComponentsInChildren<object>`）；prefab 层级 | 顺序由 Unity 运行时决定，静态不保证。**不影响结论**（无论顺序，NRE 都会打断循环） |
| 5 | `premiumOffer` / `premiumItem` 不给会不会在别的入口炸 | `MissionReRollButton__Setup.c`（不读）、`dump.cs:60454-60460`；全树搜 `PremiumOffer` 消费方 | 只确认 **ReRoll 这条链不读**；`IsPremium` / `premiumOffer` 的其它消费方（任务卡「高级」角标之类）**没逐个核** |
| 6 | `dates` 缺失（我们这条事件也没有 `dates`）是否会带来别的显示问题 | `LiveOpsEventData` ctor 会建默认 `EventTimeData`（B162 §1·2 已核）⇒ 非 null、值为 0 | 若将来做「任务倒计时」会显示 1970/空；**与本次 NRE 无关**，未展开 |

---

## 六、顺手发现（不影响本单，但要记）

1. **`add_all_configs.py:784-786` 的字段清单漏了 `rerollPrice`**（正本 `施工_奖励与邮件.md:309` 有）——
   这次就是照这份缩略清单写的。建议把那行补成「见正本 §2·2 全表」，避免下一个人再漏（这类「脚本注释里的缩略清单覆盖了正本」是同族坑）。
2. **`MissionsTab.CreateMissions` 的最后一句是 `RefreshRefillCount()`**（`MissionsTab.cs:490`）——
   B162 §1·5 C 已经写过这条，本轮再次确认：**任何**在 `Instantiate*Missions` 里抛的异常都会连带右上角计数不刷新。
3. **`MissionDebugButtons.Setup` 只是 `SetActive(false)`**（`MissionDebugButtons.cs:40`），不读 mission/progress ⇒
   它只要跑到就一定安全；所以英文 QA 按钮**永远**是「这轮 Initialize 有没有走完」的探针。
4. **`MissionData__get_RerollPrice.c` 的文件名与首行不符**（首行是 `BasicCardUI__get_GreyscaleController`、体是 `return *(param_1+0x90)`）——
   属于已知的「文件名与内容不符」族（CLAUDE.md §三），**但这一份体是对的**（偏移 0x90 与 `dump.cs:60452` 一致），可用。
