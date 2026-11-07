# B164 · 诊断：社交按钮不可点（`OFF-15`）+ DLC 消息没进收件箱（`OFF-06`）

> 2026-11-07 · **只读诊断代理**（⛔ 未改任何代码/配置/任务表/mod，⛔ 未起游戏/服务端/Unity，⛔ 未动 git）
> 输入 = `E:/warpforge/07_产物/phase0/`（本轮 10:47–10:58 的实机产物）· `E:/warpforge/game/MelonLoader/Latest.log` ·
> `E:/warpforge/05_工具/server/logs/live-20261107-104758/`（本轮假服务端收发原文）· `E:/warpforge/05_工具/mod/WfOfflineProbe/`
> 判据 = `d:/2/tools/decomp_full/`（Ghidra C，**优先**）· `E:/warpforge/03_反编译/cpp2il/out_cpp2il_dev_ilspy/` ·
> `E:/warpforge/03_反编译/code_stubs/Assembly-CSharp/`（字段/枚举）· `E:/warpforge/03_反编译/assetripper_out/ExportedProject/`（场景/prefab 实例）

---

## ① 结论（两条各一行）

| ID | 结论 |
|---|---|
| **`OFF-15`** | 🔴 **根因找到、且是数据侧的确定性缺陷**：`ExecuteCloudScript.onUserConnected.json` 的 `FunctionResult.Segmentation` 给的是 **`{}`**，而 `PlayerDataManager.ProcessNewLoginResult` 会把它的 `ToString()` **覆盖写进** `userInfo.UserReadOnlyData["Segmentation"].Value`，且这一步发生在 `SegmentationController.Unpack` **之前** ⇒ `Segmentation.Tags` 被解析成**空表**（profile 里那条 `{"Tags":["CollectedFirstDeck"]}` 被冲掉）⇒ `AlliancesManager.IsAlliancesUnlocked == false` ⇒ 社交 toggle `interactable = false`。**改 1 个 JSON 字段即可。** |
| **`OFF-06`** | 🟡 **数据层已经成立、卡在显示层**：`DlcMessageProvider.GetEvents()` 本轮**真的产出了一条 DLC 消息**（判据 = 本轮服务端收到的 `WritePlayerEvent{Action:"DlcStatus", …}`，该请求只在「新建了一条消息」那条分支里才发）。但 ① 两条本地模板（`dlc_message_offline_demo` / `referral_message_offline_demo`）**完全没有 `drawData`**，而 `LocalInboxMessage.GetContainer()` → `LiveOpsAssetUtility.GetComponentReference` 在 `drawData == null` 时**直接抛 NRE**；② 收件箱左列**现在一行都建不出来**（`MessageList.Initialize` NRE，见 `OFF-36`）。⇒ **要给模板补 `drawData.EventComponents[Container]`**；补完若列表仍 0 行，NRE 就来自另一条消息/另一处加载，按 `OFF-36` 续查。 |

> ⚠️ **对任务描述的一处订正（铁律 5）**：`OFF-06` 原来写「收件箱只显示 `Inbox.RawEvents` 那条」——
> **不准确**。现读 `02_mail.png`：**左侧消息列表是 0 行**（放大 2.2 倍亮度后仍全空，见 §二·2·3），
> 右边详情区显示的是「欢迎来到离线版」。所以「只显示一条」其实是「**列表一条都没建出来、详情区落在那一条上**」。

---

## ② 逐条根因与证据

### ②·1 `OFF-15` 社交按钮不可点

#### 2·1·1 先坐实「点击确实落在社交按钮上、且确实什么都没发生」

1. **没有点击日志 = toggle 的 `onValueChanged` 没触发**。`WindowProbe` 对 `OpenWindowButton.ToggleValueChange(bool)` 挂了 prefix
   （`E:/warpforge/05_工具/mod/WfOfflineProbe/Probes/WindowProbe.cs:50-69`），任何导航 toggle 翻面都会落 `windows.tsv` 一行 `OWB ToggleValueChange`。
   本轮 `windows.tsv` 在 **10:49:49（奖励，guid `32024b16…`）** 与 **10:50:27（收藏，guid `6b5ba7fb…`）** 之间
   —— 正好覆盖两次社交点击（截图 `08_social.png` 10:49:57 / `09_social2.png` 10:50:12）—— **一条 `ToggleValueChange` 都没有**
   （同段里 10:50:20 齿轮 `ButtonClick` 是正常的，说明不是探针整体失灵）。

2. **点击坐标确实在按钮矩形内**（不是点偏了）。场景里 `Main Menu Navigation Button - Social` =
   GameObject `&174` / RectTransform `&1244`（`MainMenu Warpforge.unity:42151`），`sizeDelta = 164.31 × 169.68`，父 `&1112`
   （`Buttons Container`，`VerticalLayoutGroup &1973`：`m_Padding.m_Top = -5` · **`m_Spacing = -16.35`** · `m_ChildAlignment = 1 (UpperCenter)`）。
   按 1920×1080 参考分辨率、`Buttons Container` 高 `1080 − 148.34 = 931.66` 推算，5 个按钮自上而下中心
   ≈ `304 / 509 / 713 / 918 / 1122` px（@1440p 实际像素），按钮高 `169.68 × 1.3333 = 226` px
   ⇒ **社交按钮 ≈ y∈[1009,1235]、奖励 ≈ y∈[805,1031]**。
   两次点击 `(64,979)`（落在**奖励**上 —— 那一个本来就是 on 的，`toggleGroup` 下点它天然无反应）
   与 `(64,1114)`（**落在社交上**）。截图实测图标带位置（`01_menu.png` 竖向亮度剖面）：玩 272 / 收藏 464–505 / 商店 693 / 奖励 884–929 / **社交 1082（标签 1134）**
   —— 与推算一致。⇒ **`(64,1114)` 稳稳在社交按钮里**。

3. **不是「窗口盖住导航」**：10:49:42 商店窗开着的时候，10:49:49 点奖励照样翻面；说明导航列在窗口外侧、照常吃点击。

⇒ 结论：**社交 toggle 当时 `interactable == false`**（Unity `Selectable.OnPointerClick → Press()` 第一句就 `if (!IsActive() || !IsInteractable()) return;`）。

> 顺带解释「看起来没变灰」：这颗 toggle 的 `m_DisabledColor = {1,1,1,1}`、`m_ColorMultiplier = 1`
> （`MainMenu Warpforge.unity:71712` 起的 `&2041` 块）—— **禁用态和常态颜色一模一样**，所以从截图上完全看不出被禁用。

#### 2·1·2 `interactable` 是谁设成 false 的

`AlliancesManager`（ILSpy `E:/warpforge/03_反编译/cpp2il/out_cpp2il_dev_ilspy/AlliancesManager.cs`）：

```csharp
// :635-655（Ghidra AlliancesManager__Initialize.c 尾部 QueueAction 的那个 lambda；
//           同一段代码在 :1798-1817 的 _003CInitialize_003Eb__27_0 里也有一份）
NavigationPanelToggle toggle = navigationPanelController.GetToggle(NavigationPanelToggleType.Social);   // :635
navToggle.toggle.interactable = isAlliancesUnlocked;      // :639 ← 无条件赋值
if (!IsAlliancesUnlocked) { CustomDebug.Log("Disabling Alliances before tutorial ends"); /* :642 */ Signal.Register(...); }
```

```csharp
// :435-451
public static bool IsAlliancesUnlocked =>
    SingletonBehaviour<SegmentationController>.Instance.segmentation.Tags.Contains(PlayerTag.CollectedFirstDeck);
```
（`PlayerTag.CollectedFirstDeck = 0`，见 `code_stubs/Assembly-CSharp/PlayerTag.cs:3`；Ghidra `AlliancesManager__get_IsAlliancesUnlocked.c` 里那句
`List<Int32Enum>__Contains(lVar1, 0, …)` 的 `0` 就是这个枚举值。）

- 这个 lambda 是 `MenuActions.QueueAction(action, ActionRule.DontBlock, 0, null, **triggerBeforeOnboardingEnds: 1**, false, false, [])` 排进去的
  （Ghidra `AlliancesManager__Initialize.c` 末尾 `MenuActions__QueueAction(lVar3,uVar4,0,0,0,**1**,0,0,uVar13,0)`；
  形参序见 `code_stubs/Assembly-CSharp/MenuActions.cs:159`）⇒ **它不受教程闸影响，一定执行**。
- ⚠️ 它没有 `Debug.Log` 可当读数：`CustomDebug.Log` 只在 `LogLevel <= Warning` 时输出，默认档位是 `Error`
  （`out_cpp2il_dev_ilspy/CustomDebug.cs:26-36`）⇒ **「日志里没有 Disabling Alliances」不能当「没被禁用」的证据**（本轮我一开始差点因此误判）。
- **prefab 默认值是 `m_Interactable: 1`**（`MainMenu Warpforge.unity` `&2041` 块）⇒ 只能是**运行时**被设成 false 的。
- **没有第二道开关**：同 GameObject 上的 `&1958` 是 `FeatureCheck{featureKey: "Alliances"}`，回调是 `SetActive(bool)`。
  我们的 `FeatureConfig.features` 里**没有 `Alliances` 这个 key**，而 `FeatureConfig.CheckFeature`（ILSpy `FeatureConfig.cs:329-392`）
  对「找不到的 key」走 `obj3 = obj2 + 1`（非 null）⇒ **默认 true（启用）** ⇒ 按钮保持 active（与截图一致）。
  `IsAlliancesEnabled` 只被 `ChatPanel`/`PlayerInfoDisplay` 用，**不挡社交窗**。

#### 2·1·3 为什么 `Segmentation.Tags` 是空的（本轮的真正根因）

这是本轮最关键的发现，**和任务简报里假设的方向不同**：`fragments/playfab_profile.json` 里那条 `Segmentation` **写了也没用，会被覆盖掉**。

1. **客户端确实收到了我们写的那条**：本轮 `LoginWithSteam` 响应 `InfoResultPayload.UserReadOnlyData` 里
   `Segmentation.Value = "{\"Tags\": [\"CollectedFirstDeck\"], \"Attributes\": {}}"`（现读
   `05_工具/server/logs/live-20261107-104758/0001_LoginWithSteam.resp.json` 的 `body.InfoResultPayload.UserReadOnlyData`）。

2. **`onUserConnected` 把它冲掉了**。`PlayerDataManager.ProcessNewLoginResult`（Ghidra
   `d:/2/tools/decomp_full/PlayerDataManager__ProcessNewLoginResult.c:83-105`）：

   ```
   if (payload.Segmentation /*+0x20*/ != 0) {
       uVar12 = userInfo /*+0x2a8*/ .UserReadOnlyData /*+0x58*/;
       lVar14 = <dict[key "Segmentation"] 或 dict[key] 的新记录>;
       uVar12 = payload.Segmentation.ToString();     // ← 我们给的是 {} ⇒ "{}"
       *(lVar14 + 0x20) = uVar12;                    // ← UserDataRecord.Value = "{}"
       <dict.set_Item(key, record)>;
   }
   ```
   我们那份 `responses/ExecuteCloudScript.onUserConnected.json` 的 `FunctionResult.Segmentation` 就是 **`{}`**（现读原文）。

3. **顺序：写入在 `SegmentationController.Unpack` 之前**（这是致命的一点）。
   - `ProcessNewLoginResult` 由 `PlayerDataManager._ProcessNewLogin_d__533__MoveNext.c:50` 调；
   - `SegmentationController.Unpack(userInfo)` 由 `PlayerDataManager._UnpackUserInfo_d__522__MoveNext.c:113` 调（`:97` 是 `UnpackConnectionSettings`，`:114` 是 `UnpackRawUserData`），
     而 `_UnpackUserInfo_d__522` ← `PlayerDataManager.UnpackUserInfo` ← `LoginManager._UnpackPlayerData_d__24__MoveNext.c:56` ← `LoginManager._CheckNewVersionSucceed_d__23__MoveNext.c:65`；
   - **实测顺序**（同一客户端上一轮 `07_产物/phase0/steps.tsv`，本轮 `STEPPROBE` 未开所以该文件还是 23:21 的）：
     `PlayerDataManager.ProcessNewLoginResult` 的 enter/exit **先于** `PlayerDataManager.UnpackRawUserData`
     —— 而 `UnpackRawUserData` 就在 `SegmentationController.Unpack` 的**下一句**（`_UnpackUserInfo_d__522:113 → :114`）。
   ⇒ **先写 `"{}"`、后 Unpack** ⇒ `Unpack` 读到的是 `"{}"`。

4. **`"{}"` 被解析成空 Tags**：`SegmentationController.DeserializeFromUserData`（ILSpy `SegmentationController.cs:47-101`）
   `JToken.Parse("{}")` → `jToken["Tags"]` = **null** → `ParseTags(null)` 走 `:558` 的 `return new List<PlayerTag>()`（**空表、不抛**）
   ⇒ `segmentation.Tags = []` ⇒ `Contains(CollectedFirstDeck)` = false。
   （`SegmentationController` 的 ctor `:516-523` 会 `new Segmentation()`，所以不会 NRE，只是 Tags 空 —— 与「点不动但不报错」的症状完全吻合。）

#### 2·1·4 就地订正一条旧判据（铁律 5）

`E:/warpforge/资料/离线原版/判据/施工_社交设置资料.md:67` 现在写的是：

> ⚠️ 不要改成 `OnUserConnected.Segmentation` —— 那条写入 `UserReadOnlyData` 发生在 `SegmentationController.Unpack` 之后，晚了。

**这句是反的**。按 §2·1·3 的三条判据：写入（`ProcessNewLoginResult`）在 `Unpack` **之前**。
⇒ **「把 tag 放进 `onUserConnected.Segmentation`」恰恰是唯一有效的做法**；而「只改 profile 的 `UserReadOnlyData`」才是会被冲掉的那个。
（我没有改这份文件 —— 不在只读白名单里。建议调度台按本条就地订正，并把 §1·1 的 A1 改成「**首选改 `onUserConnected`**，profile 那条只作冗余」。）

---

### ②·2 `OFF-06` DLC 消息没进收件箱

#### 2·2·1 数据链**已经跑通**（逐跳，都有本轮读数）

| 跳 | 证据 | 状态 |
|---|---|---|
| ① Steam 桩 | `phase0/steamoffline.tsv` `10:48:14.875 DLC GetAvailableDlcs 1 条 dlcId=9000001 … 字段写入=True/True 列表类型=List\`1` | ✅ |
| ② `GetEvents` 被调用 | `SteamManager.GetAvailableDlcs` **全库唯一调用点** = `DlcMessageProvider.GetEvents`（`grep -rln SteamManager__GetAvailableDlcs d:/2/tools/decomp_full/*.c` 只有 `DlcMessageProvider__GetEvents.c` 与它自己）⇒ 那次调用就是它 | ✅ |
| ③ `ShopConfig.GetDlcBundle` 命中 | 🔑 **间接但确定**：本轮服务端收到 `WritePlayerEvent{ "Action": "DlcStatus", "dlcsToRedeem": [ … ], "dlcsRedeemed": [] }`（`05_工具/server/logs/live-20261107-104758/0005_WritePlayerEvent.req.json`，时间 `10:48:14.888`）。按 `DlcMessageProvider__GetEvents.c`，这个 `LogActionToPlaystream` **只在 `list2.Count > 0` 时发**，而 `list2` **只在「新建了一条消息」那条分支里**才 `Add`（`FUN_1800069d0(lVar6, *puVar1)`）。⇒ **至少 1 条消息被造出来了** | ✅ |
| ④ 模板解析 / `Clone` / 日期 | 同 ③：能走到 `list2.Add` 就已经过了 `MessageTemplate != null`、`IsDlcRedeemed==false`、`RedeemedData.Any(...)==false`、`ReflectionHelper.Clone` 四道 | ✅ |
| ⑤ 进 `Inbox.events` | `Inbox.ValidateData`（Ghidra `Everguild.LiveOps.Inbox._ValidateData_d__7__MoveNext.c`）对每个 provider `await GetEvents()` 后 `List.AddRange(this.events /*+0x18*/)`；`LiveOpsHandler` 的第一个字段就是 `events`（`code_stubs/…/LiveOpsHandler.cs:164`）⇒ 目标就是 `events` | 🟡 **静态成立，运行时未直接读数** |
| ⑥ 显示 | ❌ **卡在这里**，见 2·2·2 / 2·2·3 | ❌ |

> ⚠️ `dlcsToRedeem` 的 JSON 形状是 `[[]]`（一个元素、该元素序列化成空数组），不是 `[9000001]`。这是客户端 `PlayFab` 序列化匿名类型 `<>f__AnonymousType37<string, List<uint>, List<uint>>` 的产物，
> **条数（=1）才是我们用的那个信号**；具体数字没渲成数字这件事**不影响「列表非空」这个判据**，但值得记一笔（下次谁要拿这个事件做读数时注意）。

#### 2·2·2 缺陷 A（确定）：两条本地消息模板**没有 `drawData`**

`Templates` 现读（`05_工具/server/responses/ExecuteCloudScript.InitializeLiveOps.json` → `TitleData.Templates`）：

```json
"dlc_message_offline_demo": { "$type": "Everguild.LiveOps.DLCMessageData, Assembly-CSharp",
  "eventId": …, "messages":[…], "dlcID":"offline_demo_dlc", … }        ← 没有 drawData
"referral_message_offline_demo": { "$type": "LocalInboxMessageData, Assembly-CSharp", … }   ← 也没有 drawData
```

而收件箱列表建行时**必须**拿到 container 引用：

```
ContainerBuilder<MessageContainer>.MakeContainer(evt /*IGameNews*/, messageHolder)
  → evt.GetContainer()                                    // LiveOpsEventData.cs:162-165
  → LiveOpsAssetUtility.LoadComponent<UiContainer>(liveops, EventComponentReferenceType.Container)
  → GetComponentReference(liveops, type)                  // LiveOpsAssetUtility.cs:255-266
        EventElements drawData = liveops.drawData;
        if (liveops.drawData != null) return GetReference(liveops, drawData.EventComponents, type);
        return (AssetReference)(object)new NullReferenceException();   // ← ILSpy 对 throw 的译法 = 这里 NRE
```

`drawData` 是 `LiveOpsEventData.drawData`（`code_stubs/…/Everguild/LiveOps/LiveOpsEventData.cs:40`）—— 模板里没给就是 null/空，
**`GetContainer()` 拿不到 container** ⇒ `MakeContainer` 返回 null ⇒ `MessageList.Initialize` 里
`if (lVar9 == 0) FUN_1803f47a0();`（Ghidra `Everguild.LiveOps.MessageList__Initialize.c:88-90`）**抛 NRE**。

对照组：`HandlerData.Inbox.RawEvents[0].Data`（那条「欢迎来到离线版」）**是有 `drawData` 的**，形状可以逐字抄：

```json
"drawData": {
  "EventAssets":     [ { "key": 3, "value": { "m_AssetGUID": "a1206b5e0a0544b0c846a74118249827" } } ],
  "EventComponents": [ { "key": 0, "value": { "m_AssetGUID": "b243406ae98236d4f9c1d1ddfbbad620" } } ],
  "EventLabels":     []
}
```
（`key: 0` = `EventComponentReferenceType.Container`，见 `code_stubs/…/EventComponentReferenceType.cs`；
元素类型 `EventComponent : EventReference<EventComponentReferenceType>` 是**具体类**，所以**不需要 `$type`**；
`key: 3` = `EventAssetReferenceType.HeaderImage`，可选。）

#### 2·2·3 缺陷 B（`OFF-36`，挡住了**所有**消息的显示）

`Latest.log` `10:49:01.827`（点开邮件那一刻）：

```
[ERROR] [Il2CppInterop] … System.NullReferenceException
  at Everguild.LiveOps.MessageList.Initialize (List`1[T] messages, IGameNews currentMessage)
  at Everguild.LiveOps.InboxWindow.Open ()
  at GameWindow.TryOpen …
```

`02_mail.png`（10:49:02，放大 2.2 倍逐像素看过）⇒ **左侧列表 0 行**，只有标题「收件箱」；右边详情区显示「欢迎来到离线版」。
也就是说 **NRE 发生在第一条消息上**，一行都没建出来。

- 已排除的嫌疑：`MessageList.messageContainers`（+0x30）不是 null —— ctor 里 `new List<…>()` 了
  （Ghidra `Everguild.LiveOps.MessageList__.ctor.c`），而且它**不是** `[SerializeField]`（`Inbox Menu.prefab` 里那段
  `&114967328832014496` 只有 `messageHolder: {fileID: …}` 一个字段，没有 `messageContainers`）。
- 剩下的就是 `MakeContainer` 返回 null（=缺陷 A 那条路），或者 container prefab 在离线环境下加载不出来。

#### 2·2·4 `GetCurrentMessage` 给出的旁证

`InboxWindow.Open`（Ghidra `Everguild.LiveOps.InboxWindow__Open.c`）：
```
list = handler.events.OfType<IInboxEvent>().Cast<IGameNews>().ToList();   // param_1[0xe]
if (list.Count < 1) { noNewsWarning.SetActive(true); display.SetActive(false); }
else {
    current = (param_1[8] as IGameNews) ?? GetCurrentMessage();
    MessageDisplay.Initialize(current);
    MessageList.Initialize(list, current);       // ← NRE
}
```
`GetCurrentMessage`（`InboxWindow__GetCurrentMessage.c`）= `events.LastOrDefault(谓词)`，谓词为假时 `FirstOrDefault()`。
右边详情显示的是「欢迎来到离线版」⇒ `current` 是欢迎那条。

⇒ 两种自洽解释，**都指向同一个修法**：
- **(甲)** DLC 消息在列表里但排在欢迎**之前** ⇒ `MessageList.Initialize` 在第 0 条（=DLC，无 `drawData`）就炸 ⇒ 0 行，而 `current`（=最后一条 new）=欢迎。**缺陷 A 就是全部原因。**
- **(乙)** DLC 没进 `Inbox.events` ⇒ 列表只有欢迎 ⇒ 炸在第 0 条（=欢迎）⇒ 那就是**欢迎这条的 container 也没加载出来**（`OFF-36` 的另一条支线）。

本轮日志**没有**能区分甲/乙的读数（`Inbox.events` 没有任何探针在打），我把「怎么一次分清」写进 §③·2 的验收里。

---

## ③ 最小修法（落点文件 / 行）

### ③·1 `OFF-15`（1 个字段，改完即通）

**落点**：`E:/warpforge/05_工具/server/responses/ExecuteCloudScript.onUserConnected.json`
→ `FunctionResult.Segmentation`

```diff
-    "Segmentation": {},
+    "Segmentation": { "Tags": ["CollectedFirstDeck"], "Attributes": {} },
```

- **为什么是这个落点**（不是 profile）：服务端是 `UserReadOnlyData` 的**唯一写入者**，客户端 `ProcessNewLoginResult` 每次登录都会拿
  `payload.Segmentation.ToString()` 覆盖那一格（§2·1·3）。`JObject.ToString()` 产出**紧凑 JSON** `{"Tags":["CollectedFirstDeck"],"Attributes":{}}`，
  正好是 `SegmentationController.DeserializeFromUserData` 想吃的东西。
- `fragments/playfab_profile.json` 那条 `Segmentation` **保持不动**（冗余兜底，无害）。
- ⚠️ 别改成 `"Segmentation": null` —— 那只是「不覆盖」，能生效但绕开了原版机制（服务器本来就该写这一格），不推荐当首选。
- **本轮实测无关的两处**：`onUserConnected` 每轮登录都会发（服务端 `_index.tsv` seq 2 = 200），**不需要**重建服务端（模板按 mtime 热重载），只需**重开客户端**。

**验收判据**（三条都要）：
1. `07_产物/phase0/windows.tsv` 出现 `OWB ToggleValueChange arg=True` + `open OpenWindow/4 … guid=d06efaff2b1e16f4d9f995c8cf4d51d5`
   + `open-ret GameWindow`（该 guid = `SocialMenuWindow` 的 prefab「Social Submenu Variant」，出处
   `E:/warpforge/资料/离线原版/判据/界面全量清单.md:68,138` 与 `MainMenu Warpforge.unity` 的 `&2497.windowToOpenPrefab`）。
2. 截图里出现社交窗（联盟 / 好友两个页签）。
3. `errs.tsv` / `unity.log` 无新增 `NullReferenceException`。

### ③·2 `OFF-06`（补 `drawData`，两处模板）

**落点**：`E:/warpforge/05_工具/server/tools/add_all_configs.py` 的 `TEMPLATES`（唯一来源）—— 给
**`dlc_message_offline_demo`** 和 **`referral_message_offline_demo`** 各加一段（形状逐字抄 §2·2·2 对照组的 `drawData`）：

```json
"drawData": {
  "EventAssets": [
    { "key": 3, "value": { "m_AssetGUID": "a1206b5e0a0544b0c846a74118249827" } }
  ],
  "EventComponents": [
    { "key": 0, "value": { "m_AssetGUID": "b243406ae98236d4f9c1d1ddfbbad620" } }
  ],
  "EventLabels": []
}
```
- `EventComponents[key=0]` = **必须**（`EventComponentReferenceType.Container`）；`EventAssets[key=3]`（HeaderImage）可选、建议给，否则消息没有头图。
- 然后 `python -I add_all_configs.py --write` 重新写两份 JSON（脚本自会同步
  `responses/ExecuteCloudScript.InitializeLiveOps.json` 与 `responses/fragments/playfab_profile.json` 的 `TitleData`）。
- **⛔ 不要在运行中的服务端目录里手改两份成品 JSON 而不改脚本** —— `add_all_configs.py` 是 `MINIMAL` 的唯一来源（铁律 6），下次一跑就被盖回去。

**验收判据**（这一次能一次分清 §2·2·4 的甲/乙）：
1. 重开客户端 → 点邮件 → 左列应出现 **≥2 行**（「欢迎来到离线版」+「Warpforge collection update」），`Latest.log` 里**没有**新的
   `MessageList.Initialize` NRE。
2. **若左列仍 0 行且 NRE 仍在** ⇒ 说明炸的是**欢迎那条**（§2·2·4 的乙），不是缺陷 A ⇒ 转 `OFF-36`，并加一条**只读探针**（下一句给了写法）：
   - 落点：`E:/warpforge/05_工具/mod/WfOfflineProbe/Probes/`（新文件或 `CardWindowProbe` 同级），prefix `MessageList.Initialize`，把
     `messages.Count` + 每条 `IGameNews.GetID()` 写进一个 tsv；再 prefix `LiveOpsAssetUtility.GetComponentReference`，把
     `type` 与返回的 `AssetReference.RuntimeKey` 打出来。**一次启动就能判死**：如果 `messages` 里**有** `dlc_message_9000001`，
     那就是缺陷 A（已修则说明没修到）；如果只有 `OfflineInboxWelcome`，那就是 provider 事件没进 `events`，要查
     `Inbox.ValidateData` 与本轮 `GetEvents` 的时间关系（本轮 `GetAvailableDlcs` 只在 `10:48:14.875` 出现**一次**，
     而 `Inbox.InitializeHandler` 在 `10:48:14.942` 才返回 —— 说明那次 `GetEvents` 是 `ValidateData` 里跑的，
     而 `ValidateData` 与 `InitializeHandler` 谁后谁前，现有探针都读不到）。

**顺带（不影响这两条）**：本轮 `WritePlayerEvent` 因为**没有模板**返回 **500**
（`_index.tsv` seq 5 = `WritePlayerEvent 500`）。客户端没崩，但每局登录都会多一条 500 噪声；
补一份 `responses/WritePlayerEvent.json`（`FunctionResult` 给 `{}` 即可，客户端不读返回值）能消掉它。**这条不在本次两条的因果链上**，别混为一谈。

---

## ④ 风险与回归面

### ④·1 `OFF-15` 打开 Segmentation 的连带影响

| 面 | 影响 | 依据 |
|---|---|---|
| `AlliancesManager.CheckNotification`（联盟红点） | 从「恒 0」变成真的走 `groupInvitationsCache.Count`（现在是 `FetchGroupInvitations: []` ⇒ 仍 0 条） | `AlliancesManager.cs:1755-1787` |
| `WelcomePopup.IsFirstWelcome = !CheckTag(CollectedFirstDeck)` | 目前**恒 true**（本该弹「首次欢迎」）；补上 tag 后变 false ⇒ **不再弹首启欢迎**。这是原版对「老玩家」的行为，但**是本轮唯一会被改变的用户可见行为** | `资料/历史/任务总表_第五十六会话_核销前/界面线.md:219`（T18 条） |
| `MenuActions.CanTrigger` | ⚠️ **仍然全部被挡**：它对 `triggerBeforeOnboardingEnds == false` 的动作要求 `CheckTag(0x14 = FinishedFTUETutorial)`（Ghidra `MenuActions__CanTrigger.c:16-25`）。只加 `CollectedFirstDeck` **不会**解开这道闸 | 同上 |
| 教程 / `MainTutorialManager` | 若**额外**再加 `FinishedFTUETutorial`，`MenuActions` 的大批 queued 动作会被放行、`MainTutorialManager.GetState` 相关分支也会变 —— **这是另一条口径，建议单独决定，不要和 `OFF-15` 捆在一起改** | `MainTutorialManager__GetState.c` |

⇒ **建议**：本轮只加 `CollectedFirstDeck`（最小、可解释、可回滚）。要不要再补 `FinishedFTUETutorial`，单独开一条账。

### ④·2 `OFF-06` 补 `drawData` 的连带影响

- 只动 `Templates` 里**两条本地消息模板**，不碰 `HandlerData.Inbox`、不碰欢迎那条。
- 若 `OFF-36` 的 NRE 修好后列表出现 2 行，**DLC 那条会带一个「领取」按钮**（`DlcMessageProvider.GetEvents.c` 里
  `*(undefined1 *)(clone + 0x88) = 1;` 把 `canClaimReward` **强制 true**，`Templates` 里写 false 也没用），而 `Rewards = []`
  ⇒ 点「领取」的行为仍未验（`B158` §四·3 已记）。**验收时别点它**，或单独开一条账验空奖励。
- 补 `EventAssets[key=3]`（头图）会复用欢迎那条的头图 `a1206b5e…`（`Inbox Image_Space Marine announce`）。若想要 DLC 专属头图，
  本地 `inboxmessagesheaderslocal_assets_all.bundle` 里应还有别的，但**这一轮没有挑图判据**，用同一张是安全的（不新增依赖）。
- `referral_message_offline_demo` 目前**没有任何消息在用它**（`ReferralConfig.messageTemplate` 指向它，但 `ReferralMessagesProvider`
  只在有推荐数据时出消息）。补 `drawData` 是**预防性**的，不会改变本轮画面。

---

## ⑤ 没查清（写清搜过什么）

1. **`Inbox.events` 里到底有没有 DLC 那条** —— 现有探针没有任何一个读它。
   查过的：`phase0/*.tsv` 全表 grep `Social|Dlc|Inbox|Message`（除 `steamoffline.tsv` 的 `DLC` 行外 0 命中）·
   `probe.log`（4.8 MB，按 10:4x/10:5x 时间过滤）· `taskslog.tsv`（只有 `InitializeHandler-enter/return` 两类）·
   `Latest.log`。⇒ **要么加 §③·2 那条只读探针，要么按 §③·2 的第 2 步验收时看结果**。
2. **`Inbox.ValidateData` 与 `Inbox.InitializeHandler` 的先后**（决定 §2·2·4 甲/乙）。
   静态读过 `Everguild.LiveOps.Inbox._ValidateData_d__7__MoveNext.c`、`_InitializeHandler_d__6__MoveNext.c`、
   `_RefreshHandler_d__9__MoveNext.c`、`LoginManager._ProcessData_d__36/_CheckNewVersion*/_UnpackPlayerData*`、
   `PlayerDataManager._ProcessNewLogin_d__533/_UnpackUserInfo_d__522`，**但 `LiveOpsManager` 里谁先 await 谁没有一轮能直接读出**；
   ⚠️ 另外 `LiveOpsHandler` 的 `events`（+0x18，`code_stubs/…/LiveOpsHandler.cs:164`）与 `blockedEvents`（+0x20）**偏移相邻**，
   Ghidra 的 `int*` 索引容易读错一格 —— 我按「`ValidateData` 的 AddRange 目标 = +0x18 = `events`」下的结论，**但这条是推断不是实测**。
3. **`Message Container`（`b243406a…`）在离线环境下能不能真的 `Load` 出来**。
   已确认 `menus_assets_all.bundle` / `generalshared` 一类包都在
   `E:/warpforge/game/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`（87 个包，含 `menus_assets_all.bundle`），
   但**没有一次运行时读数**说「这个 AssetReference 解析成功了」。`addressables.tsv` 只有 13 行（配置 + catalog 存在性），读不出单资产。
4. **`dlcsToRedeem: [[]]` 的形状**：按 `GetEvents` 的代码它应该是 `List<uint>`（→ `[9000001]`），实际发出去是 `[[]]`。
   没查 PlayFab SDK 那条序列化路（`CloudscriptHandler.LogActionToPlaystream`），但**条数 = 1 这个判据不受影响**。
5. **`Alliances` 这个 FeatureConfig key**：我们没配，代码对缺失 key 默认 **true**（`FeatureConfig.cs:364-392` 的 `obj3 = obj2 + 1`）。
   `obj2 + 1` 这个 ILSpy 表达式有点绕，我按「非 null ⇒ true」判的；**若原意是别的，社交按钮会变成 SetActive(false)（看不见）** ——
   而实机它是**看得见**的，这个观察本身就把这条假设证伪了，所以不影响结论。

---

## 附：本轮用到的关键读数（便于复核）

| 读数 | 位置 |
|---|---|
| `10:48:14.875 DLC GetAvailableDlcs 1 条 dlcId=9000001` | `E:/warpforge/07_产物/phase0/steamoffline.tsv` |
| `10:48:14.888 WritePlayerEvent{Action:"DlcStatus", dlcsToRedeem:[[]], dlcsRedeemed:[]}`（500 因无模板） | `E:/warpforge/05_工具/server/logs/live-20261107-104758/0005_WritePlayerEvent.req.json` |
| `10:49:01.827 MessageList.Initialize NRE` | `E:/warpforge/game/MelonLoader/Latest.log`（`InboxWindow.Open` 栈） |
| 社交点击期间 **零** `OWB ToggleValueChange` | `E:/warpforge/07_产物/phase0/windows.tsv`（10:49:49 → 10:50:27 之间只有 10:50:20 的齿轮 `ButtonClick`） |
| 收件箱左列 **0 行** | `d:/4/_tmp_view/shots/1107_off27/02_mail.png`（`C:/…/Temp/b164/mail_left.png` 放大 2.2× 复核） |
| 社交按钮 rect / prefab 默认 `m_Interactable: 1` / `m_DisabledColor` 白 | `E:/warpforge/03_反编译/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`（`&1244` / `&2041` / `&1973` / `&2497`） |
| `onUserConnected.FunctionResult.Segmentation = {}` | `E:/warpforge/05_工具/server/responses/ExecuteCloudScript.onUserConnected.json` |
| profile 里那条 Segmentation（被覆盖） | `05_工具/server/logs/live-20261107-104758/0001_LoginWithSteam.resp.json` → `body.InfoResultPayload.UserReadOnlyData.Segmentation` |
| 模板缺 `drawData` | `05_工具/server/responses/ExecuteCloudScript.InitializeLiveOps.json` → `TitleData.Templates` |
