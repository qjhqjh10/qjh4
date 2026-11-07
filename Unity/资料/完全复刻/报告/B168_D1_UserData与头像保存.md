# B168 D1 · UserData 三键 / DemoData 落盘 / UpdateAvatarUrl 落盘与回灌

> 执行写手：D1（第一百四十二会话，2026-11-07）。**只改假服务端，不起游戏、没跑 Unity、没动 git。**
> 判据输入：`报告/B168_判据_账户页_资料头像头衔.md` §五 A13 / §四 B3·B4 / §五 C4 / §六 R1·R2·R5。
> ⛔ 本文件只写「做了什么 + 判据」，**状态**见 `资料/完全复刻/活状态表.md`。

---

## 一、结论（三件各做了什么 / 没做什么）

| # | 施工项 | 结果 | 一句话 |
|---|---|---|---|
| 1 | `_inject_userdata` 补 `UserData` 三键 | ✅ 做了 | `VictoryCounters` / `BattleLogData` 注进 **`UserReadOnlyData`**（这才是客户端真正的读口，见 §二-1 的订正）；`DemoData` 注进 `UserData`。只补缺键，不动已有值。 |
| 2 | `responses/UpdateAvatarUrl.json` | ✅ 做了 | 回包 = **`EmptyResponse`（空对象 `{}`）**，不是 `FunctionResult` 包；`ImageUrl` 落 `state.avatarUrl`，登录回灌到 `InfoResultPayload.PlayerProfile.AvatarUrl`。 |
| 3 | `UpdateUserData` 的 `DemoData` 落盘 | ✅ 做了 | 请求 `Data` 全部原样落 `state.userData`（键 → JSON 文本），`KeysToRemove` 也处理；登录时按 `INJECT_STORED_USER_DATA_KEYS` 回灌（本批只有 `DemoData`）。`DataVersion` 保持模板行为恒 1。 |

**没做 / 明确不做**：

- ❌ `TutorialV2` 的回灌：客户端 31 条 `UpdateUserData` 里 19 条是它，**我落盘但不回灌**（`INJECT_STORED_USER_DATA_KEYS` 只放 `DemoData`）。理由：施工单范围是 R5（DemoData），回灌教程进度会改变还没验收的教程链。放开只需把 `"TutorialV2"` 加进那个元组。
- ❌ `DataVersion` 递增（原版每次写 +1）：按施工单「按现有模板行为」保持恒 `1`，只在模板 `_todo` 里写清。
- ❌ 没碰 R2 的文件（`InitializeLiveOps.json` / `playfab_profile.json` / `add_all_configs.py`）、没碰 mod、没碰 R1 的 `ChangePlayerName.json`。

---

## 二、关键字段与判据（`文件:行号`）

### 2.1 🔴 订正：`VictoryCounters` / `BattleLogData` 的读口是 **`UserReadOnlyData`**，不是 `UserData`

P1 判据 §A13 写「这两个值只在 `UserData` 的 `VictoryCounters`/`BattleLogData` 里来」——**这一句不成立**。三份独立证据：

| 证据 | 位置 | 读到的 |
|---|---|---|
| ILSpy 版 | `cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:11548` / `:11557` | `OnlineData.GetPlayerOnlineData<object>(userInfo, PlayerDataKey.BattleLogData)` / `(…VictoryCounters)` —— **`checkPublicData` 取默认值 false** |
| il2csharp 版（显式写出来） | `cpp2il/out_il2csharp/Assembly-CSharp/PlayerDataManager.cs:4794` / `:4802` | `GetPlayerOnlineData<List_1<BattleLogMatch>>(this.__field_userInfo, PlayerDataKey.BattleLogData, false)` —— **第三实参就是 false** |
| 机器码 `.c` | `decomp_full/PlayerDataManager__UnpackRawUserData.c:162` / `:170` | `GetPlayerOnlineData<object>(userInfo, 0x1a /*26 BattleLogData*/, **0**, …)` / `(…, 0x2c /*44 VictoryCounters*/, **0**, …)`；对照 `:97` 的 DemoData 是 `(userInfo, 0xf /*15*/, **1**, …)` |

`OnlineData.GetPlayerOnlineData`（`cpp2il/out_cpp2il_dev_ilspy/OnlineData.cs:145-192`）的分支是：

```
if (!HasKey(UserReadOnlyData, key)) {
    if (!checkPublicData || !HasKey(UserData, key)) return null;   // ← checkPublicData=false 时永远走这里
    dictionary = UserData;
} else dictionary = UserReadOnlyData;
```

⇒ **`checkPublicData == false` ⇒ 只认 `UserReadOnlyData`，`UserData` 里放了也读不到。**
旁证：现成的卡组链也是这么走的 —— `_inject_decks` 把 `PlayerDecks` 注进 `UserReadOnlyData`（`server.py:1273-1293`）才能重启后还在。

**所以本批落点**：`VictoryCounters` / `BattleLogData` → `UserReadOnlyData`；`DemoData` → `UserData`（它读的时候 `checkPublicData: true`，见上表第三节证据）。

### 2.2 🔴 副产物：P1 「`UserData={}` ⇒ 两个都是 null ⇒ NRE」这条归因**不完整**

`UnpackRawUserData` 对这两个键**有兜底**（`PlayerDataManager.cs:11548-11619`）：

- `BattleLogData` 缺键 ⇒ `new List<BattleLogMatch>()`（空表，**非 null**）；
- `VictoryCounters` 缺键 ⇒ 从 `battleLogData` 现算：`Where(matchWin && !matchLose)` → `GroupBy(ownHeroName)` → `ToDictionary`（空表 ⇒ **空字典，非 null**）。

⇒ 只要 `UnpackRawUserData` 跑到底，`PlayerInfo.GetVictoryCounters()`（`PlayerInfo.cs:419`）与 `WarlordMasteryContainer.OnInitialize`（`:81`）**都不会因为「键缺失」拿到 null**。所以 §A13「必崩」的**根因还需要再查**（可能是 `UnpackRawUserData` 自己中途抛、或别的字段）。

⚠️ 这**不改变本批做法**：注入真实数据本来就是原版服务端会有的状态，而且能绕开那条兜底（兜底依赖 `UnpackRawUserData` 走完）。但它意味着：**实机验收时如果 Warlord Mastery 还崩，别再把锅甩给这两个键**。

### 2.3 `DemoData` 最小合法形状

类定义：`cpp2il/out_cpp2il_dev_ilspy/DemoData.cs:6-20`（9 个字段）＋ `decomp_full/DemoData__.ctor.c`（ctor 只把 `tutorialsCompleted` 初始化成空字典）。

| 字段 | 类型 | 默认值（本批注入） | 备注 |
|---|---|---|---|
| `tutorialsCompleted` | `Dictionary<string,bool>` | `{}` | 🔴 **不能是 null** —— `DemoData.IsTutorialCompleted`（`DemoData.cs:23-55`）在它为 null 时直接抛 NRE |
| `gameAfterTutorialPlayed` | bool | `false` | = `new DemoData()` 的值 |
| `suscriptionPopupShowed` / `suscribed` | bool | `false` | |
| `currentDeck` | string | `""` | 原版新档是 null；唯一读点 `PlayerDataManager.cs:10711`（`!= deckID`）与 `PracticeModePopup.cs:654`（`string.IsNullOrEmpty`）对 `""`/null **行为一致**，用空串只为免下游空引用 |
| `canEnterDraftMode` | bool | `false` | |
| `playerTitle` | `LoadableReference<CosmeticItem>` | `"370bd66e623781d42a1e39cad3563a7f"` | **在线 JSON 里是裸字符串**（见 2.4） |
| `feedbackSeen` / `feedbackSent` | bool | `false` | |

字段名与顺序取自**客户端自己上传的那一份**：`logs/live-20261106-191814/0065_UpdateUserData.req.json`
`{"tutorialsCompleted":{},"gameAfterTutorialPlayed":true,"suscriptionPopupShowed":false,"suscribed":false,"currentDeck":"UM_SK_1_2","canEnterDraftMode":false,"feedbackSeen":false,"feedbackSent":false}`
（`playerTitle` 为 null 时客户端整个省掉这个键 —— 所以「有值」与「没值」两种形状都合法。）

### 2.4 `playerTitle` 的序列化形状 = **裸字符串**

`cpp2il/out_cpp2il_dev_ilspy/LoadableReference.cs:780-798`：

```csharp
public JToken WriteJson()      => new JValue(targetId);          // 写：就是那个 uniqueId 字符串
public void OnBeforeRead(JToken rawJson) {
    if (rawJson.Type == JTokenType.String) targetId = rawJson.Value<string>();
}
```

⚠️ **别拿资产格式的 `{genericObjectReference,targetId,reference}` 三键去填**（那是 Unity 序列化，例：`02_解包资源/assets_full/bundle_prebuiltdecks_assets_all/MonoBehaviour/ASH_BOT_FTUE_Deck1.json:70-82`）——在线 JSON 走的是 `CustomObjectFormat` 这条自定义路。

### 2.5 起始头衔 id

`370bd66e623781d42a1e39cad3563a7f` = `Title_All_Starter_Recruit`
（`02_解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/Title_All_Starter_Recruit.json:11` 的 `uniqueId`），
也是 `PlayerDataManager.PlayerTitle` 在 `demoData.playerTitle` 为空时回落的 `GameStaticData.starterTitleId`
（`cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:7123-7160`）。⇒ **注入它与「不注入」在当前实现下显示同一个头衔**，不是新行为。

### 2.6 头像：请求字段名 / 回包类型 / 回灌字段

| 环节 | 判据 | 结论 |
|---|---|---|
| 请求字段名 | `code_stubs/PlayFab/PlayFab/ClientModels/UpdateAvatarUrlRequest.cs:7` | **`ImageUrl`**（施工单里写的 `AvatarUrl` 是错的；服务端两种都收，以 `ImageUrl` 为准） |
| 值的格式 | `PlayerAvatarDataManager.PackPlayfabAvatarAndBorderData`（`:1655-1679`） | 永远是 `"<avatarId>#<borderId>"`（取不到就用当前值） |
| 回包类型 | `code_stubs/PlayFab/PlayFab/PlayFabClientAPI.cs:626` | **`EmptyResponse`**（`ClientModels/EmptyResponse.cs` 空类）——⚠️ 不是 `UpdateAvatarUrlResult`，也**不是** `{FunctionResult: …}`（它不是 CloudScript） |
| 客户端拿回包做什么 | `PlayerAvatarDataManager.cs:1700-1710` / `:1743-1753` | 只打 `"Avatar changed"` + 调 `m_OnAvatarChanged` / `m_OnAvatarBorderChanged` + 回调 |
| 登录回灌字段 | `PlayerDataManager.UnpackUserProfile`（`PlayerDataManager.cs:11391-11440`） | `InfoResultPayload.PlayerProfile.AvatarUrl` → `Split('#')`：两段 = (头像, 头像框)；一段 = 头像 + 缺省框；空 = 缺省头像 `d151b0956783643be93696e46e711de6` + 缺省框 `36ae26bb6bd304e79bd17fe02fbd3725` |
| 设置调用点 | `PlayerAvatarDataManager.cs:1710-1712` | `SetCurrentItemNoSave<PlayerAvatar>(null)` + `SetCurrentItemNoSave<CosmeticItemAvatarBorder>(borderId)` |

### 2.7 `UserDataRecord` 的形状

`code_stubs/PlayFab/PlayFab/ClientModels/UserDataRecord.cs:8-13`：`{ LastUpdated: DateTime, Permission: UserDataPermission?, Value: string }`。
`Value` 是**一段 JSON 文本**，客户端会再 `JsonWrapper.DeserializeObject<T>` 一次（`OnlineData.cs:145-192`）。

---

## 三、改动清单

### `E:/warpforge/05_工具/server/server.py`

| 行 | 内容 |
|---|---|
| `:104-142` | 新增常量块：`STARTER_TITLE_ID` · `DEFAULT_DEMO_DATA` · `USER_DATA_READONLY_DEFAULTS` · `INJECT_STORED_USER_DATA_KEYS`（每条都带 `文件:行号` 判据） |
| `:421-428` | `State.__init__`：新增 `self.user_data` / `self.avatar_url` |
| `:451-460` | `State.load_state`：读回 `userData` / `avatarUrl`；`state_extra` 排除这两个键 |
| `:482-484` | `State.save_state`：写 `userData`；`avatarUrl` 仅在非 None 时写 |
| `:556-588` | 新增 `State.set_user_data()` / `State.set_avatar_url()`（都 `save_state()` + `log.note`） |
| `:623-624` | `health()` 增 `user_data_keys` / `avatar_url`（`/__health` 可观测） |
| `:886-890` | `do_POST`：`UpdateUserData` → `_apply_user_data_write`；`UpdateAvatarUrl` → `_apply_avatar_url_write`（在模板解析**之前**，与卡组写的范式一致） |
| `:920-930` | `do_POST`：登录/档案类响应的 deepcopy 块里追加 `_inject_userdata`；新增 `GetUserData` 的 `_inject_get_user_data` |
| `:1121-1268` | 新增 6 个方法：`_user_data_record` · `_apply_user_data_write` · `_apply_avatar_url_write` · `_public_user_data_values` · `_inject_userdata` · `_inject_get_user_data` |

### `E:/warpforge/05_工具/server/responses/UpdateAvatarUrl.json`（新建）

空对象 + `_comment/_sources/_todo`（发送前 `strip_meta` 剥掉 `_` 键 ⇒ 客户端只看到 `{}`）。

### `E:/warpforge/05_工具/server/responses/UpdateUserData.json`（改注释）

只改 `_sources`/`_todo`：把「本响应【不落盘】」「落盘 TODO（阶段 3）」这两句**过期说明**就地订正成「已落盘 + 回灌范围」。`DataVersion: 1` 未动。

### 自测脚本（不在工程里，仅留档）

`E:/warpforge/_tmp_view/b168_d1/d1_selftest.py`（临时端口 8797 · 临时 `state.json` · `--no-log`）。

---

## 四、自测结果（不启游戏）

**24 条断言，0 失败。** 分段：

1. **冷启动（state 为空）**
   - 登录 200；`UserReadOnlyData.VictoryCounters.Value` = `"{}"` 且 `json.loads` 得 `{}`；
   - `UserReadOnlyData.BattleLogData.Value` = `"[]"` 且得 `[]`；
   - `UserData.DemoData.Value` 可解析，`tutorialsCompleted == {}`，`playerTitle == 370bd66e…`；
   - `PlayerProfile.AvatarUrl` **缺省不注入**（没改过头像就不伪造）；
   - `GetPlayerCombinedInfo` 同样拿到三键；`GetAccountInfo` 仍 200（无 `InfoResultPayload` ⇒ 跳过）；`GetUserData.Data.DemoData` 与登录一致；
   - 冷启动登录**不写 state**（默认值是「补缺」不是「落盘」）。
2. **写**
   - `POST UpdateUserData {Data:{DemoData:"…"}}` → 200 + `{DataVersion:1}`；
   - `POST UpdateAvatarUrl {ImageUrl:"<头像>#<框>"}` → 200 + `data == {}`；
   - `state.userData.DemoData` **逐字节**等于请求里的 JSON 文本；`state.avatarUrl` 落对。
3. **重启后回灌**：`UserData.DemoData` = 落盘值；`PlayerProfile.AvatarUrl` = 落盘值；只读两键默认值仍在。
4. **真 state 未被改动**：`state/player_state.json` SHA256 前后一致（`9f229fba27c6c324…`）。

另：`python -m py_compile server.py` 通过；`responses/**/*.json` 48 份全部可解析（新增 1 份）。

---

## 五、实机验收要点（下一次起游戏时看这四条）

1. **档案页 `Events/Warlord Mastery`**：还崩不崩。⚠️ 按 §2.2，`VictoryCounters`/`BattleLogData` **可能不是**它的真因 —— 如果修完这三键仍 NRE，立刻转去查 `UnpackRawUserData` 是否中途抛（那是下一层）。
2. **头衔页点「选择」**：`TitleTab.OnSelect` 之前必抛在 `demoData + 0x30`；现在 `demoData` 一定有值、`playerTitle` 是合法 uniqueId ⇒ 按钮应能走完 `UploadOnlineDemoData`，`profilefix.tsv` 里应看到 `Uploading serializedDemoData: …`，且**重启后仍是那个头衔**（落盘回灌）。
3. **头像页点「选择」**：原来卡在缺 `responses/UpdateAvatarUrl.json`（无回包 ⇒ `resultCallback` 不触发 ⇒ 按钮停在 `interactable=false`）。现在应有回包；重启后 `PlayerAvatarID` 应是刚选的那颗（回灌 `PlayerProfile.AvatarUrl`）。⚠️ 如果列表还是空的，那是 B1（cosmetics 装载）那条，不是这一批。
4. ⚠️ **未解决的老疑点**：P1 §八-1 记着实机显示的头衔是 `Warrior of the Raging Winds`（`Title_ASH_Premium_2`），与「DemoData 空 ⇒ 起始头衔 Recruit」矛盾。现在注入的默认值是 **Recruit**，如果实机**仍**显示别的，说明**头衔显示源不在 `demoData.playerTitle`**（那条链要单独查），**别据此判本批做错**。

---

## 六、顺手发现（不属本批，交给调度台分流）

1. 🔴 **`_inject_decks` 会污染模板缓存**（`server.py:1273-1293`，2026-11-06 的旧代码）：`strip_meta` 只浅拷顶层，`_inject_decks` 直接改 `payload["InfoResultPayload"]["UserReadOnlyData"]` ⇒ 改的是 `TemplateStore` 缓存里的**同一个嵌套 dict**。首次登录之后，缓存里的 `LoginWithSteam` 就永久带着当时那份 `PlayerDecks`（后续每次登录会重新覆盖，所以自洽，但和同文件里其它注入点「先 deepcopy」的纪律不一致）。**本批新加的 `_inject_userdata` 走的是 deepcopy 之后的那份，没有这个问题。**
2. 🔴 **P1 §A13 的「必崩」归因需要复核**（详见 §2.2）——这是本批最重要的反证，建议在判据文件里就地订正（避免下一个会话照着错链条去改代码）。
3. `.c` 里 `System_Collections_Generic_List<ArraySegment<byte>>___ctor` 这个符号名是**误标**：`PlayerDataManager__UnpackRawUserData.c:166` 它是 `List<BattleLogMatch>` 的 ctor（同族误标在本项目已有多例），别按名字认类型。
4. `GetUserData` 在现网日志里**一次都没被调用过**（`logs/live-2026*` 全部请求清单里 0 条）——我顺手补了它的注入（防两个口打架），但它目前是死路。
5. 客户端 `UpdateUserData` 除 `DemoData` 外还写 **`TutorialV2`**（19/31 条）。这批落盘了但没回灌（`INJECT_STORED_USER_DATA_KEYS`）。如果教程线要做「进度跨重启」，**把 `"TutorialV2"` 加进那个元组就行**，不用再改落盘逻辑。
