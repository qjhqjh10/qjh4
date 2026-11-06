# B152 · PlayFab DTO 形状判据

> **这份文件是干什么的**：让另一位工程师**不看别的资料**，就能写出三个假服务端响应
> （`LoginWithSteam` / `ExecuteCloudScript[onUserConnected]` / `ExecuteCloudScript[GetReadOnlyPublisherData]`）以及
> `GetPlayerCombinedInfoResultPayload` 里客户端真正会读的每个子结构。
>
> **判据源（全本地，只读）**：
> - **`SDK/`** = `E:/warpforge/03_反编译/code_stubs/PlayFab/PlayFab/`（PlayFab SDK 的**签名桩**：字段/枚举完整、方法体空）。
>   ⚠️ 与 `d:/2/Warpforge_code/Scripts/PlayFab/PlayFab/` **逐字节相同**（`diff -rq` 返回 0）。
> - **`ILSPY/`** = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`（Assembly-CSharp 的 ILSpy 全文，**方法体可读但不完整**）。
> - **`DEC/`** = `d:/2/tools/decomp_full/`（真方法体，26,282 个；**遇到 ILSpy 缺 null 检查/缺常量时以它为准**）。
> - **`P0/`** = `E:/warpforge/07_产物/phase0/`（2026-11-06 用真客户端跑出来的**实测记录**，不是推理）。
>
> **SDK 版本 = `2.206.241122`**（`SDK/PlayFabSettings.cs:15` `SdkVersion`；运行时 URL 里也是 `sdk=UnitySDK-2.206.241122`，`P0/playfab.tsv:3`）。
> ⚠️ **不要用「最新 PlayFab SDK 文档」来填** —— 本 build 的模型是这一版，字段可能比新版少。
>
> **一句话结论**：客户端对 JSON 的要求分三层 —— ① **HTTP 信封**必须 `{"code":200,"status":"OK","data":{...}}`；
> ② data 里的字段名 = **C# 字段名原样（PascalCase）**，未知名会被忽略；
> ③ `UserData`/`TitleData`/`UserReadOnlyData` 的**键 = 枚举名字符串**、**值 = 一段 JSON 文本**（客户端会再反序列化一次）。
> 另有 **4 个「必须非 null」**（`EntityToken`、`InfoResultPayload`、`TitleData`、`PlayerStatistics`）和
> **3 个「会炸/会静默跳过」的坑**，都在 §2.9 / §3.5 列了。

---

## 0. 传输层：HTTP 信封（**先说，否则下面全部无效**）

| 项 | 值 | 出处 | 置信度 |
|---|---|---|---|
| 成功响应 = 信封 + data | `{"code":200,"status":"OK","data":<本文档所有 §1/§3 的 body>}` | `SDK/SharedModels/HttpResponseObject.cs:5,7,9`（`int code` / `string status` / `object data`）；**实测**：`P0/playfab.tsv:10` 与 `P0/playfab.tsv:9` 都标 `AFTER_ERROR (null = 反序列化成功)` | **高（实测）** |
| 裸对象（没有 `code`） | **失败**：客户端判成 `ServiceUnavailable / http=400` | `E:/warpforge/05_工具/server/server.py:449-457` 的注释（2026-11-06 阶段 1 实测）；同一结论见 `E:/warpforge/05_工具/server/README.md:119-123` | **高（实测）** |
| 错误响应字段名（小写） | `code` / `status` / `error` / `errorCode` / `errorMessage` / `errorDetails` | `SDK/PlayFabError.cs:9-19`（`HttpCode`/`HttpStatus`/`Error`/`ErrorMessage`/`ErrorDetails`/`RetryAfterSeconds`）；字符串表 `d:/2/tools/all_strings.txt:21378`（`errorDetails`）、`:21388`（`errorMessage`） | 中高 |
| HTTP 状态 | 成功用 **200**；PlayFab 也认 4xx/5xx 并把 body 当错误 JSON 解析 | `E:/warpforge/05_工具/server/README.md:107-117`（503/500 两种实测） | 中高 |
| URL | `http://<host>:<port>/Client/<Method>`（方法名从 URL 取） | `P0/playfab.tsv:3`（真客户端实际请求的 URL 形态） | **高（实测）** |
| 顶层大小写 | **只有信封是小写**；`data` 里全部 PascalCase | 见 §1/§2/§3 每张表 | 高 |

> ⚠️ **信封里的 `data` 是「方法返回对象本身」**（`LoginResult` / `ExecuteCloudScriptResult`），**不是** `{…: {"value": …}}` 那种套娃。
> ⚠️ 服务端**不要**在 `data` 里塞 `_comment` / `_todo` 之外的非模型字段——客户端会忽略，但会让「形状」变脏。

---

## 1. LoginResult（登录响应）

**模型**：`SDK/ClientModels/LoginResult.cs:7-24`
**基类**：`PlayFabLoginResultCommon : PlayFabResultCommon : PlayFabBaseModel`（`SDK/SharedModels/PlayFabLoginResultCommon.cs:3`、`PlayFabResultCommon.cs:3`），基类字段 **不来自 JSON**，不用写。

| JSON 字段名 | 类型 | 客户端读它的地方（文件:行号） | 用途 | 置信度 |
|---|---|---|---|---|
| `EntityToken` | `EntityTokenResponse` | `ILSPY/PlayerDataManager.cs:10994`（`PlayerDataManager.FinishLogin`）；真体 `DEC/PlayerDataManager__FinishLogin.c:20-25,33` | ⇒ `PlayerDataManager.entityKey`（0x330）取它的 `.Entity`。**null ⇒ 直接抛异常**（`DEC/…FinishLogin.c:33` 的 `if (*(param_2+0x28) != 0)`，否则落到 `FUN_1803f47a0()` = NRE） | **高（真体）** |
| `EntityToken.Entity` | `EntityKey`{`Id`,`Type`} | `ILSPY/PlayerDataManager.cs:10995-10996`；`DEC/…FinishLogin.c:20-22` | 存进 `PlayerDataManager.entityKey`（**整个 EntityKey 对象，不是 Id 字符串**）。⚠️ 简报里写的「+0x330 = EntityId」是**误读**，见 §7 | **高（真体，含写入屏障 `FUN_1803f3730` = 引用存）** |
| `EntityToken.EntityToken` | `string` | **客户端一行都不读**（全仓 grep `EntityToken.EntityToken` 0 命中） | —— | 高 |
| `EntityToken.TokenExpiration` | `DateTime?` | **不读** | —— | 高 |
| `InfoResultPayload` | `GetPlayerCombinedInfoResultPayload` | `ILSPY/PlayerDataManager.cs:11002`；`DEC/…FinishLogin.c:29,33` | 存进 `PlayerDataManager.userInfo`（0x2a8），**整条链都从它读**。**null ⇒ 抛异常** | **高（真体）** |
| `LastLoginTime` | `DateTime?` | **主流程不读**（只有 `ILSPY/PlayfabTester.cs:615-621` 这个调试工具打印） | —— | 高 |
| `NewlyCreated` | `bool` | `ILSPY/PlayerDataManager.cs:11003`；`DEC/…FinishLogin.c:31` | ⇒ `newlyCreatedAccount`（0x340）。**恒 false 更安全**（否则新号流程会走） | **高（真体）** |
| `PlayFabId` | `string` | `ILSPY/PlayerDataManager.cs:10993`；`DEC/…FinishLogin.c:20` | ⇒ `PlayerDataManager.playFabId`（0x328）；被当 `GetReadOnlyPublisherData` 的 payload、排行榜行、日志 | **高（真体）** |
| `SessionTicket` | `string` | `ILSPY/PlayerDataManager.cs:10998`；`DEC/…FinishLogin.c:25` | ⇒ `PlayerDataManager.sessionToken`（0x338）。⚠️ 简报里写「+0x58 = EntityToken」是**误读**，见 §7 | **高（真体）** |
| `SettingsForUser` | `UserSettings` | **主流程不读**（只有 `PlayfabTester`） | —— | 高 |
| `TreatmentAssignment` | `TreatmentAssignment`{`Variables`,`Variants`} | **主流程不读**（只有 `PlayfabTester`） | —— | 高 |

**按登录方式还会额外读**（不一定是你这条链）：

| 位置 | 客户端读的地方 | 作用 |
|---|---|---|
| `InfoResultPayload.AccountInfo.SteamInfo.SteamId` | `ILSPY/PlayerDataManager.cs:10889-10905`（`NotLinkedSteamAccount`）+ 调用点 `DEC/PlayerDataManager.__c__DisplayClass510_0___LoginToPlayFab_b__0.c:26-40` | **Steam 登录路径必填**：为 `null` 或空串 ⇒ 客户端**不调登录成功回调**，改去调 `CloudscriptHandler.LinkSteamAccount`（多打一次 CloudScript，且主线卡住） |
| `InfoResultPayload.PlayerProfile.DisplayName` | `ILSPY/LoginWithEmailHelper.cs:65-77,130-142` | **邮箱登录路径**：DisplayName 为空 ⇒ `BlackBoard.skipTutorial = true`（跳过新手引导） |

---

## 2. GetPlayerCombinedInfoResultPayload

**模型**：`SDK/ClientModels/GetPlayerCombinedInfoResultPayload.cs:8-35`
**客户端登录时请求的开关**（决定服务端该回哪些段）：`ILSPY/CloudscriptHandler.cs:3881-3914`（`GetInfoRequestParams`）
⇒ 只置 `GetPlayerStatistics` / `GetTitleData` / `GetUserAccountInfo` / `GetUserData` / `GetUserInventory` /
`GetUserReadOnlyData` / `GetUserVirtualCurrency` / `GetPlayerProfile` 八项 = true，ProfileConstraints = `ShowTags`+`ShowAvatarUrl`+`ShowLastLogin`+`ShowLocations`。
⇒ **`CharacterInventories` / `CharacterList` 根本没请求**（客户端也不读）⇒ 回 `[]` 或 `null` 都行。

| JSON 字段名 | 类型 | 客户端读它的地方 | 用途 | 置信度 |
|---|---|---|---|---|
| `AccountInfo` | `UserAccountInfo` | 见 §2.1 | 显示名 / 邮箱 / Steam 绑定 | 高 |
| `CharacterInventories` | `List<CharacterInventory>` | **不读**（未请求） | —— | 高 |
| `CharacterList` | `List<CharacterResult>` | **不读**（未请求） | —— | 高 |
| `PlayerProfile` | `PlayerProfileModel` | 见 §2.2 | 头像 / 国家 / 公会标签 / 累计消费 | 高 |
| `PlayerStatistics` | `List<StatisticValue>` | 见 §2.3 | 排行榜自位列 | **高（且必须非 null）** |
| `TitleData` | `Dictionary<string,string>` | 见 §2.4 | 版本号 / 连接设置 / 功能开关 | **高（且必须非 null）** |
| `UserData` | `Dictionary<string,UserDataRecord>` | 见 §2.5 | 玩家公开存档（卡组/教程/计数器…） | 高 |
| `UserDataVersion` | `uint` | **不读** | —— | 高 |
| `UserInventory` | `List<ItemInstance>` | 见 §2.6 | 背包（货币/卡包/皮肤…） | 高 |
| `UserReadOnlyData` | `Dictionary<string,UserDataRecord>` | 见 §2.5 | 玩家只读存档（**优先于 UserData**） | 高 |
| `UserReadOnlyDataVersion` | `uint` | **不读** | —— | 高 |
| `UserVirtualCurrency` | `Dictionary<string,int>` | 见 §2.7 | 货币余额 | 高 |
| `UserVirtualCurrencyRechargeTimes` | `Dictionary<string,VirtualCurrencyRechargeTime>` | **不读** | —— | 高 |

### 2.1 `AccountInfo`（`UserAccountInfo`）

模型：`SDK/ClientModels/UserAccountInfo.cs:8-55`

| JSON 字段名 | 类型 | 客户端读它的地方 | 用途 | 置信度 |
|---|---|---|---|---|
| `TitleInfo` | `UserTitleInfo` | `DEC/PlayerDataManager__UnpackUserProfile.c:26-36`（`+0x10`→`+0xa8`→`+0x20`）、`:38-46` | 见下三行 | **高（真体）** |
| `TitleInfo.DisplayName` | `string` | 同上（`DEC/…UnpackUserProfile.c:32`） | ⇒ `PlayerDataManager.playerName`（0x208）：存档里的玩家名、排行榜行刷新、聊天名 | **高** |
| `TitleInfo.AvatarUrl` | `string` | **不读**（用的是 `PlayerProfile.AvatarUrl`） | —— | 中 |
| `PrivateInfo` | `UserPrivateAccountInfo`{`Email`} | `DEC/…UnpackUserProfile.c:38-46`（`+0x88`→`+0x10`） | ⇒ `PlayerDataManager` +0x348 | **高** |
| `SteamInfo` | `UserSteamInfo`{`SteamId`,`SteamName`,…} | `ILSPY/PlayerDataManager.cs:10886-10910` | **Steam 路径必填非空 `SteamId`**（见 §1 末表） | 高 |
| `PlayFabId` / `Username` / 其余 20 个平台字段 | —— | **不读** | —— | 高 |

> ⚠️ `TitleInfo` / `PrivateInfo` 为 `null` **不会炸**（`DEC/…UnpackUserProfile.c` 里每层都有 `!= 0` 判空，ILSpy 把它渲染丢了 —— 这是本任务最容易误判的地方）。

### 2.2 `PlayerProfile`（`PlayerProfileModel`）

模型：`SDK/ClientModels/PlayerProfileModel.cs:8-47`（**字段顺序 = 声明顺序 = 偏移顺序**，已用 `+0x28`/`+0x58`/`+0x70` 三处偏移交叉验证）

| JSON 字段名 | 类型 | 客户端读它的地方 | 用途 | 置信度 |
|---|---|---|---|---|
| `Locations` | `List<LocationModel>` | `DEC/PlayerDataManager__FinishLogin.c:34-40`（`payload+0x28`→`PlayerProfile+0x70`→`PlayerDataManager.userLocations` 0x3e0）；消费 `ILSPY/PlayerDataManager.cs:10088-10103`（`GetLastUserLocation`） | 取 `[0].CountryCode`（国家/时区）。⚠️ **简报写的「时间戳」是误读**，见 §7 | **高（真体）** |
| `AvatarUrl` | `string` | `ILSPY/PlayerDataManager.cs:11376-11420`（`UnpackUserProfile`） | `"<assetGUID>#<sub>"` 用 `#` 切两段；缺省用写死的两个 GUID（`d151b095…` / `36ae26bb…`） | 中高 |
| `DisplayName` | `string` | `ILSPY/LoginWithEmailHelper.cs:65-77` | 邮箱登录：空 ⇒ 跳过新手引导 | 中高 |
| `Tags` | `List<TagModel>`{`TagValue`} | `ILSPY/OnlineData.cs:397-451`（含 `"NotInv"`）；`:453-532`（`GuildId` + `B` 徽章 / `R` 区域）；`:615-689`（`GuildId` + `N` 名字） | 邀请开关 / 公会徽章 / 公会区域 / 公会名。`TagValue` 用 `.` 切段 | 中高 |
| `TotalValueToDateInUSD` | `uint?` | `ILSPY/GameSunsetPopup.cs:216-232` | 停运补偿窗的「可换多少金币」 | 中高 |
| `PlayerId` / `TitleId` / `Created` / `LastLogin` / `Statistics` / `LinkedAccounts` / `Memberships` / `Origination` / 其余 | —— | **未读到主流程消费者**（`Statistics` 是 profile 内部的，榜单读的是顶层 `PlayerStatistics`） | —— | 中 |

> `PlayerProfile` 整体可以为 `null`（各处都判空）—— 但那样就没有头像/国家/公会标签。

### 2.3 `PlayerStatistics`（**必须非 null**）

模型：`SDK/ClientModels/StatisticValue.cs:7-14` → `{ "StatisticName": string, "Value": int, "Version": uint }`

| 项 | 值 | 出处 | 置信度 |
|---|---|---|---|
| 谁读 | `PlayerDataManager.PreloadLeaderboards`（`ILSPY/PlayerDataManager.cs:11644-11648`）→ `LeaderboardManager.Initialize` → `UnpackPlayfabValues` | `DEC/PlayerDataManager__PreloadLeaderboards.c:17-21` 直接把 `payload+0x30` 传下去；`DEC/Everguild.LiveOps.LeaderboardManager__UnpackPlayfabValues.c:30-33` | **高（真体）** |
| 怎么用 | 逐条建一条**自己**的 `LeaderboardEntry`：`position = Value`（`DEC/Everguild.LiveOps.LeaderboardManager__UnpackPlayfabValues.c:60`）、`playFabId`、`playerName`、`avatar`，按 `StatisticName` 存进 `leaderboardCache` | 同上 `:56-95` | **高** |
| **null 后果** | `UnpackPlayfabValues` 第一句就是 `if (param_2 == 0) FUN_1803f47a0();` ⇒ **直接抛 NRE** | `DEC/…UnpackPlayfabValues.c:29-32`；`PreloadLeaderboards` 没有判空 | **高（真体）** |
| 键名（`StatisticName`）取值域 | `OnlineStat` 枚举名：`Xp` `Rating` `RatingA` `RatingB` `RatingC` `MaxRating` `MaxFastRating` `EventRating` `LegendPoints` `Veteran` `BossScore` `AchievementPoints`（`ILSPY/OnlineStat.cs:1-15`） | ⚠️ `OnlineData.GetOnlineStat`（`ILSPY/OnlineData.cs:691,730`）**全仓 0 调用者** ⇒ 真实名字域**只能从这个枚举推**，未实测 | 中 |
| 最少给什么 | `[]`（空数组）**能过**，只是排行榜里自己没有位次 | —— | 高 |

### 2.4 `TitleData`（**必须非 null**；值 = JSON 文本）

模型：`Dictionary<string,string>`（`SDK/ClientModels/GetPlayerCombinedInfoResultPayload.cs:20`）

**读法**：`OnlineData.GetTitleData<T>`（`ILSPY/OnlineData.cs:96-123`；真体 `DEC/OnlineData__GetTitleData_object_.c`）
= `TitleData[枚举名]` → 取**字符串** → `JsonWrapper.DeserializeObject<T>(字符串)`（Newtonsoft）。
`HasKey` 还要求 **值非空串**（`ILSPY/OnlineData.cs:367-388`）。

| 键（= `TitleDataKey` 枚举名） | 登录链上谁读 | 缺键后果 | 置信度 |
|---|---|---|---|
| `ServerVersion` | `OnlineData.GetServerVersion`（`ILSPY/OnlineData.cs:26-68`；真体 `DEC/OnlineData__GetServerVersion.c:26-46`，**枚举常量 = 1 = ServerVersion**）→ `PlayerDataManager.UnpackOnlineVersionData`（`ILSPY/PlayerDataManager.cs:8016-8017`）→ `CheckUpdateRequired`（`DEC/PlayerDataManager__CheckUpdateRequired.c:26-56`） | 走 legacy 分支读 `AndroidVersion` / `iOSVersion` / `LanguageVersion`，都缺 ⇒ `pcVersion` 为 null ⇒ `CheckUpdateRequired` 返回 0（**不要求更新**）⇒ **不致命** | **高（真体）** |
| `ConnectionSettings` | `UnpackConnectionSettings`（`ILSPY/PlayerDataManager.cs:11171`，调用点 `:4460`） | 得到 `null` ⇒ 用 `new ConnectionSettingsData()` 兜底 | 中高 |
| `OverrideConfig` / `CloudscriptRestrictedVersionConfig` | `SetupOverrides`（`ILSPY/PlayerDataManager.cs:11188-11215`；调用点 `ILSPY/LoginManager.cs:984`） | 得到 null ⇒ 不设 Override / 不设 CloudScript revision | 中高 |
| `AddressablesConfig` | `UnpackAddressables`（`ILSPY/PlayerDataManager.cs:4331-4341`）→ `AddressablesManager.Init(config,…)` | **缺键 ⇒ null 传进 Init**，是否炸**未验**（见 §6） | 中（未验） |
| `ServerFlags` | `OnlineData.GetServerFlags`（`ILSPY/OnlineData.cs:784-799`） | 缺键 ⇒ 返回空 `List<string>`（安全） | 高 |
| `AndroidVersion` / `iOSVersion` / `LanguageVersion` | `GetServerVersion` 的 legacy 分支（`ILSPY/OnlineData.cs:45-56`） | 只在 `ServerVersion` 缺时用 | 高 |

> **`TitleData` 整体为 null 会炸**：`GetServerVersion` 开头就是 `userInfo.TitleData.ContainsKey(...)`，
> 真体里 `if (lVar4 != 0)` 为假时直接落到函数尾部的 `FUN_1803f47a0()`（`DEC/OnlineData__GetServerVersion.c:44-48,66-70`）。
>
> **`ServerVersion` 的值形状** = `ServerVersionConfig` 的 JSON 文本（`ILSPY/ServerVersionConfig.cs:45-60`，字段顺序与偏移吻合）：
> `iosVersion` / `androidVersion` / `pcVersion` / `macVersion` / `replaysVersion`（string）· `textVersion`（int）·
> `useNewMultiplayerVersion`（bool）· `multiplayerVersions`（list）· `saveGameForceCompatibility`（list）·
> `currentTitleDataRevision` / `minimumTitleDataRevision`（int）· `photonServerRegion` / `photonChatServerRegion`（枚举，给 0）。
> ⚠️ `pcVersion` **必须 ≤ 客户端 `Application.version`**，否则 `CheckUpdateRequired` 判「要更新」。

### 2.5 `UserData` / `UserReadOnlyData`（键 = 枚举名；值 = JSON 文本）

模型：`Dictionary<string, UserDataRecord>`（`SDK/ClientModels/GetPlayerCombinedInfoResultPayload.cs:22,28`）；
`UserDataRecord` = `{ "LastUpdated": DateTime, "Permission": enum?, "Value": string }`（`SDK/ClientModels/UserDataRecord.cs:7-14`）

**读法**：`OnlineData.GetPlayerOnlineData<T>`（`ILSPY/OnlineData.cs:145-233`；真体 `DEC/OnlineData__GetPlayerOnlineData_object_.c`）：
1. 先查 **`UserReadOnlyData[键]`**；2. 没命中且 `checkPublicData=true` 才查 **`UserData[键]`**；3. `HasKey` 要求 `Value` **非空串**；4. `JsonWrapper.DeserializeObject<T>(Value)`。

| 项 | 结论 | 出处 | 置信度 |
|---|---|---|---|
| 键名格式 | **枚举名字符串**（`Enum.ToString()`），不是数字 | `DEC/OnlineData__GetPlayerOnlineData_object_.c`（先 `Enum.ToString` 再 `ContainsKey`）；`DEC/OnlineData__HasKey.c` | **高（真体）** |
| 值 | **一段 JSON 文本**（字符串），客户端会再反序列化 | 同上 + `ILSPY/OnlineData.cs:183,224`（`DeserializeObject<T>(userDataRecord.Value)`） | **高** |
| `LastUpdated` / `Permission` | **不读**（只读 `Value`） | `ILSPY/OnlineData.cs:180-183` | 高 |
| 两个字典都缺这个键 | 打一条 `"No player data found for key X"` 然后返回 `null`/默认值 | `ILSPY/OnlineData.cs:168,209,263,305` | 高 |

**登录时 `UnpackRawUserData` 会读的键**（`ILSPY/PlayerDataManager.cs:11428-11638`，调用点 `:4466`）：

| 键（`PlayerDataKey` 枚举名，`ILSPY/PlayerDataKey.cs:1-54`） | 是否 `checkPublicData` | 反序列化成的类型 | 行号 |
|---|---|---|---|
| `"PlayerDecks"`（**字符串字面量**，走 `GetPlayerOnlineData<T>(…, string, …)`） | 否 | `List<AzureDeckInfo>` | 11454 |
| `Decks` | 是 | 同上（兜底） | 11459 |
| `CurrentDeck` | 是 | `string`（**原样**，不再反序列化） | 11472 |
| `TipsData` | 是 | `TipsData` | 11475 |
| `DemoData` | 是 | `DemoData` | 11482 |
| `TutorialV2` | 是 | `TutorialV2SaveData` | 11492 |
| `FriendsData` | 是 | `FriendsData` | 11502 |
| `DailyCounters` | 否 | `DailyCounters` | 11509 |
| `CrateCounters` | 否 | `Dictionary<string,CrateInfo>` | 11516 |
| `DropTableCounters` | 否 | `Dictionary<string,Dictionary<string,int>>` | 11526 |
| `NameChanges` | 否 | `NameChangeRecord` | 11536 |
| `BattleLogData` | 否 | `List<BattleLogMatch>` | 11548 |
| `VictoryCounters` | 否 | `Dictionary<string,int>` | 11557 |
| `LastMessageReadTime` | 是 | `long` | 11606 |
| `ModTools` | 否 | `ModTools` | 11608 |
| `RedeemedData` | 否 | `List<RedeemedItem>` | 11618 |
| `CollectorLevel` | 否 | `int`（只在 `CurrentXp <= 0` 时用） | 11630 |
| `EphemeralInventory` | 否 | `RewardInfo[]`（走 `GetOnlineData<T>`） | 11656 |

**其它登录链上会读的键**：

| 键 | 谁读 | 行号 |
|---|---|---|
| `ProfileVersion` | `OnlineData.GetPlayerGameVersion`（按空格切，取第一段 `Trim()` 后 `new Version(...)`） | `ILSPY/OnlineData.cs:70-94` |
| `ActivateDevTools` / `ChatBanned` / `GlobalChatBanned` | `PlayerDataManager.UnpackOnlineVersionData` | `ILSPY/PlayerDataManager.cs:8023-8032`，调用点 `ILSPY/LoginManager.cs:242` |
| `Segmentation` | `SegmentationController` | `ILSPY/SegmentationController.cs:57` |
| `GameModes` | `CheckForTutoriaDoneAndDataFix`（**注意：这里直接读 `UserReadOnlyData["GameModes"]`，不走 OnlineData**，值按 `JsonUtility.FromJson<List<GameModesEventItem>>` 解） | `ILSPY/PlayerDataManager.cs:11242-11256` |
| `Achievements` | `AchievementsManager` | `ILSPY/AchievementsManager.cs:266` |
| `BlockedPlayers` / `ReferralData` | 各自管理器 | `ILSPY/ChatGlobalManager.cs:1034`、`ILSPY/ReferralManager.cs:97` |
| `PlayerDecks` / `PlayerDecksFast` | `DeckInventory` | `ILSPY/DeckInventory.cs:209,216` |

**`PlayFabReadOnlyDataKeys`（另一套字符串常量，共 6 条）**：
`"AchievementsData"` / `"CurrentSelectedObjects"` / `"DailyCounters"` / `"PlayerDecks"` / `"StarterDeckGranted"` / `"EventSaveGames"`
—— `SDK` 外的 Assembly-CSharp：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/W40k/AzureObjects/PlayFabReadOnlyDataKeys.cs:5-15`。

### 2.6 `UserInventory`（`List<ItemInstance>`）

模型：`SDK/ClientModels/ItemInstance.cs:8-39`（14 个字段）
**客户端只读两个**：`ItemId` 和 `RemainingUses`。

| JSON 字段名 | 类型 | 客户端读它的地方 | 用途 | 置信度 |
|---|---|---|---|---|
| `ItemId` | `string` | `ILSPY/InventoryManager.cs:1349`（`AssetLocator.GetAsset<IIdentifiable>(itemInstance.ItemId)`）；真体 `DEC/InventoryManager__Add.c` | **必须能被本地 `AssetLocator` 认出来**，否则打 `"error adding item to inventory, couldn't find item <DisplayName> (<ItemId>)"` 并**跳过** | **高（真体）** |
| `DisplayName` | `string` | `ILSPY/InventoryManager.cs:1353-1360` | **只用于上面那条错误日志** | 高 |
| `RemainingUses` | `int?` | `ILSPY/InventoryManager.cs:1352-1355`（`itemInstance + 120` = 0x78 = `RemainingUses`，`?? 0`） | 入库数量（`Add(item, amount)`） | **高（真体+偏移）** |
| 其余 11 个字段 | —— | **不读** | —— | 高 |
| （整个数组为 null） | `List<ItemInstance>` | 容忍：`DEC/InventoryManager__DeserializeInventory.c` 里 `if (param_2 != 0)` 判空 | 背包为空 | 中高 |

### 2.7 `UserVirtualCurrency`（`Dictionary<string,int>`）

| 项 | 结论 | 出处 | 置信度 |
|---|---|---|---|
| 消费点 | `InventoryManager.DeserializeInventory(items, virtualCurrencies)` 的第二个形参 | `ILSPY/PlayerDataManager.cs:11655`、`:10652`；`ILSPY/InventoryManager.cs:286`；真体 `DEC/InventoryManager__DeserializeInventory.c:170-183` | **高（真体）** |
| 键 | **`Currency.code`**（货币资产的 `code` 字段），不是 `GameCurrency` 枚举名 | 真体：`AssetLocator.GetAsset<IIdentifiable>(KeyValuePair.Key)` 然后 `Add(货币资产, 值)`（`DEC/…DeserializeInventory.c:174-182`）；`OnlineData.GetCurrency` 也直接用 `currency.code` 当键（`ILSPY/OnlineData.cs:771`） | **高** |
| 已知的 code 值 | **`"C2"` = crystals · `"C3"` = blackStones · `"C5"` = gachaTickets** | 我们工程已复刻并写死：`d:/4/Unity/MyGame/Assets/CardPresentation/Shell/CurrencyCatalogSeed_B142E65.cs:52-54`（注释写明「三对 code ↔ type 是 SO 字段实读」） | 中高（本地 SO 实读，非本次复算） |
| 值 | `int` 余额 | 同一处 | 高 |
| 字典为 null | 容忍（`if (param_3 != 0)`） | `DEC/InventoryManager__DeserializeInventory.c:170` | 中高 |

> ⚠️ `OnlineData.GetCurrency(userInfo, currency)`（`ILSPY/OnlineData.cs:766-782`）**全仓 0 调用者** —— 真正生效的是 `DeserializeInventory` 那条路。

### 2.8 小结：`GetPlayerCombinedInfoResultPayload` 的最小可用形状

`AccountInfo`（非 null）+ `PlayerProfile`（可空）+ `PlayerStatistics: []`（**必须存在**）+ `TitleData: {}`（**必须存在**）
+ `UserData: {}` + `UserReadOnlyData: {}` + `UserInventory: []` + `UserVirtualCurrency: {}`。

### 2.9 空值容忍度总表（哪一格 null 会炸 / 会静默）

| 字段 | null 的后果 | 判据 |
|---|---|---|
| `LoginResult.EntityToken` | **抛异常**（登录直接失败） | `DEC/PlayerDataManager__FinishLogin.c:33,50-51` |
| `LoginResult.InfoResultPayload` | **抛异常** | 同上 `:33,50-51` |
| `EntityToken.Entity` | **抛异常**（`+0x10` 无判空直接取） | 同上 `:21` |
| `payload.TitleData` | **抛异常** | `DEC/OnlineData__GetServerVersion.c:44-48,66-70` |
| `payload.PlayerStatistics` | **抛异常** | `DEC/Everguild.LiveOps.LeaderboardManager__UnpackPlayfabValues.c:29-32` |
| `payload.UserReadOnlyData` + `onUserConnected.Segmentation != null` | **抛异常**（真体里 `payload+0x58 == 0` ⇒ `goto` 抛） | `DEC/PlayerDataManager__ProcessNewLoginResult.c:84-98` |
| `payload.UserData` / `UserInventory` / `UserVirtualCurrency` / `PlayerProfile` / `AccountInfo` / `CharacterList*` | 容忍（有判空） | `DEC/OnlineData__GetPlayerOnlineData_object_.c`、`DEC/InventoryManager__DeserializeInventory.c`、`DEC/PlayerDataManager__UnpackUserProfile.c` |
| `ExecuteCloudScriptResult.FunctionResult` | **不报错但会把 `null` 交给下游**（`onComplete(null)`）⇒ 下游 NRE | `DEC/PlayfabWrapper__DeserializePlayfabResult_object_.c:33-37` |

---

## 3. ExecuteCloudScriptResult

### 3.1 响应壳（`SDK/ClientModels/ExecuteCloudScriptResult.cs:8-33`）

| JSON 字段名 | 类型 | 客户端读它的地方 | 用途 | 置信度 |
|---|---|---|---|---|
| `Error` | `ScriptExecutionError`{`Error`,`Message`,`StackTrace`} | `ILSPY/PlayfabWrapper.cs:2431-2439`；`ILSPY/CloudscriptHandler.cs:210,2581-2620` | **必须是 `null`**：`Error != null && Error.Message != null` ⇒ 直接走 `onError`（`FunctionResult` 根本不看） | **高** |
| `Logs` | `List<LogStatement>`{`Data`,`Level`,`Message`} | `ILSPY/CloudscriptHandler.cs:2581-2635` | **最后一条的 `Level == "Error"` ⇒ 当成错误**（会把 `Data` 当 `PlayfabError` 解析）。给 `[]` 最安全 | 高 |
| `FunctionResult` | `object`（PlayFab 会解成 `JsonObject`） | `ILSPY/PlayfabWrapper.cs:2455-2530`（`DeserializePlayfabResult`） | **业务数据全在这里**，见 §3.2 | **高（真体辅助）** |
| `FunctionName` | `string` | `ILSPY/CloudscriptHandler.cs:2550-2560`（拼错误文案） | 只用于日志/错误串。**不校验**：请求里是枚举名，响应里可以原样回来 | 高 |
| `ExecutionTimeSeconds` / `APIRequestsIssued` / `HttpRequestsIssued` / `MemoryConsumedBytes` / `ProcessorTimeSeconds` / `Revision` / `FunctionResultTooLarge` / `LogsTooLarge` | —— | **不读** | —— | 高 |

### 3.2 `FunctionResult` 的三条消费规则（这是最容易搞错的地方）

真体：`DEC/PlayfabWrapper__DeserializePlayfabResult_object_.c`（+ 泛型版 `…___Il2CppFullySharedGenericType_.c`），
ILSpy 对照：`ILSPY/PlayfabWrapper.cs:2455-2530`。

1. **`FunctionResult` 是一个 `JsonObject`（`IDictionary<string,object>`）时**，先 `TryGetValue("Success", out v)`：
   `v` 是 `bool` 且为 **false** ⇒ 调 `onError(FunctionResult.ToString())` 并 **return**（`ILSPY/PlayfabWrapper.cs:2497`；
   真体 `DEC/…DeserializePlayfabResult_object_.c:44-72`）。**没有 `Success` 键 ⇒ 正常继续**；值为 `true` ⇒ 正常继续。
2. **然后整份 `FunctionResult` 直接反序列化成 `TOut`**：先试类型转换，失败就
   `JsonWrapper.DeserializeObject<TOut>(FunctionResult.ToString())`（`DEC/…DeserializePlayfabResult_object_.c:96-115`）。
   ⇒ **`TOut` 的字段必须写在 `FunctionResult` 的**顶层**，没有 `Data` / `Result` 这一层套娃**（除非那个函数自己的 DTO 长那样）。
   `JsonWrapper` = Newtonsoft，设置只有 `ReferenceLoopHandling.Ignore` + `TypeNameHandling.None` + 两个自定义 converter
   （`ILSPY/Everguild.Utils/JsonWrapper.cs:45-77`）⇒ **未知字段被忽略、大小写不敏感**。
3. **`CheckCustomError`（在 `DeserializePlayfabResult` 之前就跑）**：把 `FunctionResult.ToString()` 解析成 `JToken`，
   - 是 `JObject` ⇒ 看键 **`"Error"`**；是 `JArray` ⇒ 反向找最后一个含 `"Error"` 的对象（`ILSPY/CloudscriptHandler.cs:2440-2580`）
   - 一旦该值非空白 ⇒ `onError`，**`onSuccess` 不会调**（`ILSPY/CloudscriptHandler.cs:316-340`）
   ⇒ **`FunctionResult` 里不要出现 `Error` 键**（哪怕值是空串也最好别写）。

> 另有 `CloudscriptHandler.ProcessResultData` 读 `FunctionResult["Result"]` 并 `bool.TryParse`
> （`ILSPY/CloudscriptHandler.cs:2930-2970`）—— 那是**别的** CloudHandle 的用法，`onUserConnected` / `GetReadOnlyPublisherData` 不走它。

### 3.3 `onUserConnected`（CloudHandle = **10**）→ `TOut = OnUserConnectResult`

**调用点**：`ILSPY/PlayerDataManager.cs:3765`（`PlayfabWrapper.GenericCloudScriptHandlerAsync<OnUserConnectResult>(CloudHandle.onUserConnected, null)`）
**消费点**：`DEC/PlayerDataManager__ProcessNewLoginResult.c`（ILSpy `ILSPY/PlayerDataManager.cs:11744-11800`）
**DTO**：`ILSPY/OnUserConnectResult.cs:8-20`

| JSON 字段名 | 类型 | 客户端读它的地方（DEC 行号 = 偏移） | 用途 / 必需性 | 置信度 |
|---|---|---|---|---|
| `ServerTime` | `long` | `DEC/…ProcessNewLoginResult.c:43-56`（+0x10） | ⇒ `serverTimeOnLogin`。**单位 = Unix 毫秒**（对照 `PlayerDataManager.CurrentMillis()` = UTC 1970 起的毫秒，`ILSPY/PlayerDataManager.cs:7747-7757`） | **高（真体）** |
| `Achievements` | `Dictionary<string,AchievementPersistence>` | `DEC/…ProcessNewLoginResult.c:74-81`（+0x18） | **null ⇒ 只打一条 `LogWarning("Failed to initialize Achievements, missing Achievement key on onUserConnected result")`**（不致命）；非 null ⇒ 存 `PlayerDataManager.rawAchievementsData`（0x200） | **高（真体）** |
| `Segmentation` | `JObject`（Newtonsoft） | `DEC/…ProcessNewLoginResult.c:83-100`（+0x20） | 非 null ⇒ 把 `ToString()` 写进 **`UserReadOnlyData["Segmentation"].Value**；⚠️ 此时 `payload.UserReadOnlyData` **必须非 null**（`:97-98` 否则抛） | **高（真体）** |
| `grantedItems` | `RewardInfo[]` | `DEC/…ProcessNewLoginResult.c:102-118`（+0x28，`Length > 0` 才排队） | 非空 ⇒ 排一个「领奖」UI 动作（`RewardService.Collect`）。给 `[]` 就没有弹窗 | **高（真体）** |
| `OverrideXp` | `int?` | `DEC/…ProcessNewLoginResult.c:70-73`（+0x30） | 有值 ⇒ 覆盖 `CurrentXp` | **高（真体）** |

> ⚠️ **`ServerTime` 的类型是 `long`**（不是 `ObscuredLong`）—— 偏移 0x10 上 `ObscuredLong.op_Implicit` 立刻把它转成 `long`
> （`ILSPY/PlayerDataManager.cs:11757-11761`），所以 JSON 里要写**整数**，不要写日期串。

### 3.4 `GetReadOnlyPublisherData`（CloudHandle = **1121** = 0x461）→ `TOut = Dictionary<string,string>`

**调用点**：`DEC/PlayerDataManager._GetAllPublisherPlayerData_d__515__MoveNext.c:40-44`
（`PlayfabWrapper__GenericCloudScriptHandler<object>(0x461, payload = new { itemId = PlayFabId }, …)`；
真泛型 = `Dictionary<string,string>`，见本地函数签名 `ILSPY/PlayerDataManager.cs:15759`）
**消费点**：`DEC/PlayerDataManager__ProcessPublisherPlayerData.c`（ILSpy `ILSPY/PlayerDataManager.cs:11040-11056`）

| 形状 | 说明 | 出处 | 置信度 |
|---|---|---|---|
| `FunctionResult` = **一个扁平对象，键 → 字符串** | `"Sunset": "{\"totalUSDSpentInWarpforge\":0,…}"`（值必须是**字符串**，因为客户端 `Dictionary<string,string>.TryGetValue` 拿到后还要再 `DeserializeObject<T>` 一次） | `DEC/PlayerDataManager__ProcessPublisherPlayerData.c:44-56`（`TryGetValue` + `JsonWrapper.DeserializeObject<SunsetPublisherData>`） | **高（真体）** |
| 唯一的键 = **`"Sunset"`** | `PlayerPublisherDataKey { Sunset = 0, WarpforgeLivePlayed = 1 }`；真体里枚举值 `0` ⇒ `"Sunset"` | 枚举：`ILSPY/PlayerPublisherDataKey.cs:1-5`；枚举常量 `0`：`DEC/PlayerDataManager__ProcessPublisherPlayerData.c`（`local_18 = 0` 的 boxed 枚举） | **高** |
| `Sunset` 的值形状 | `SunsetPublisherData` = `{ totalUSDSpentInWarpforge:int, goldToCollectInLegions:int, hasSentRequest:bool, hasBeenClaimed:bool }` | `ILSPY/SunsetPublisherData.cs:4-12` | 高 |
| `"WarpforgeLivePlayed"` | 客户端**不读**（全仓只有枚举定义，没有第二个消费点） | 同上 grep | 高 |
| `FunctionResult` 里加 `"Success": true` | **会出问题**：它会被当成字典的一项（值为 bool，Newtonsoft 转 `Dictionary<string,string>` 可能失败/变成 `"True"`）。**别加** | §3.2 规则 2 | 中高 |

### 3.5 `ExecuteCloudScriptResult` 的「不要写」清单

| 别写 | 为什么 | 出处 |
|---|---|---|
| `Error` 非 null 且 `Message` 非 null | 直接 `onError`，`FunctionResult` 不解析 | `ILSPY/PlayfabWrapper.cs:2431-2439` |
| `Logs` 最后一条 `Level == "Error"` | 被 `CheckApiError` 当错误 | `ILSPY/CloudscriptHandler.cs:2581-2635` |
| `FunctionResult.Error`（键名 `Error`） | 被 `CheckCustomError` 当业务错误 | `ILSPY/CloudscriptHandler.cs:2440-2580` |
| `FunctionResult.Success = false` | 被 `DeserializePlayfabResult` 当失败 | `ILSPY/PlayfabWrapper.cs:2497` |
| `FunctionResult` 再套一层 `data` / `Result` | `onUserConnected` / `GetReadOnlyPublisherData` 都是**整份**反序列化 | §3.2 规则 2 |

---

## 4. `UserData` / `TitleData` 的键名口径 —— 判定

**判定：键 = 枚举成员名字符串（`Enum.ToString()`），不是数字。值 = 一段 JSON 文本（字符串），不是对象。**

| 依据 | 出处 |
|---|---|
| `GetPlayerOnlineData<T>(userInfo, PlayerDataKey key, …)` 第一句就是 `string text = ((Enum)&obj).ToString();`，随后 `HasKey(dict, text)` / `dict[text]` | `ILSPY/OnlineData.cs:154-180`；真体 `DEC/OnlineData__GetPlayerOnlineData_object_.c` |
| `GetTitleData<T>` 同理，且 `HasKey(Dictionary<string,string>, TitleDataKey)` 也是 `Enum.ToString()` | `ILSPY/OnlineData.cs:96-123,367-388` |
| 真体里枚举是 **boxed 后 `Enum.ToString()`**（结构：`local_38 = 类型token` / `local_30 = -1` / `local_28 = 枚举整数值`），据此读出 `GetServerVersion` 用的键 = **1 = `ServerVersion`** | `DEC/OnlineData__GetServerVersion.c:31-40`（`local_28 = 1`）；枚举表 `ILSPY/TitleDataKey.cs:1-33` |
| 同族：`PlayerPublisherDataKey` 的 `Sunset = 0` ⇒ 键 `"Sunset"` | `DEC/PlayerDataManager__ProcessPublisherPlayerData.c` |
| 值再反序列化：`JsonWrapper.DeserializeObject<T>(userDataRecord.Value)` | `ILSPY/OnlineData.cs:183,224` |
| 值必须**非空串**才认（`HasKey` 里 `string.IsNullOrEmpty(Value)` 就当中不存在） | `ILSPY/OnlineData.cs:338,359,383` |

**例外（记得别踩）**：`CheckForTutoriaDoneAndDataFix` 读 `UserReadOnlyData["GameModes"]` 用的是**字符串字面量**，而且用
`JsonUtility.FromJson`（Unity，不是 Newtonsoft）解析（`ILSPY/PlayerDataManager.cs:11242-11256`）。
另 `PlayerDataManager` 里还有一处 `GetPlayerOnlineData<…>(…, "PlayerDecks", …)` 也是字符串字面量（`:11446`）。

---

## 5. 可直接粘贴的最小响应 JSON（三份）

> 三份都**已经过真客户端实测**：`P0/playfab.tsv:9 / :10 / :11` 的 `AFTER_ERROR` 列都是 `null`（= 反序列化成功）。
> 用的时候**记得套 §0 的信封**：`{"code":200,"status":"OK","data":<下面这份>}`。
> 时间一律 ISO-8601 UTC（`"2026-01-01T00:00:00.000Z"`）；`ServerTime` 例外，要 **Unix 毫秒整数**。

### 5.1 `POST /Client/LoginWithSteam` → `LoginResult`

```json
{
  "InfoResultPayload": {
    "AccountInfo": {
      "PlayFabId": "OFFLINE0000000001",
      "TitleInfo": {
        "DisplayName": "WarpforgeOffline",
        "Created": "2026-01-01T00:00:00.000Z",
        "FirstLogin": "2026-01-01T00:00:00.000Z",
        "LastLogin": "2026-01-01T00:00:00.000Z",
        "isBanned": false,
        "Origination": "Steam"
      },
      "PrivateInfo": { "Email": "" },
      "SteamInfo": { "SteamId": "76561190000000000", "SteamName": "WarpforgeOffline" }
    },
    "PlayerProfile": {
      "PlayerId": "OFFLINE0000000001",
      "DisplayName": "WarpforgeOffline",
      "TitleId": "OFFLINETITLE",
      "Created": "2026-01-01T00:00:00.000Z",
      "LastLogin": "2026-01-01T00:00:00.000Z",
      "AvatarUrl": "",
      "Statistics": [],
      "Locations": [],
      "LinkedAccounts": [],
      "Tags": [],
      "Memberships": []
    },
    "PlayerStatistics": [],
    "CharacterInventories": [],
    "CharacterList": [],
    "TitleData": {
      "ServerVersion": "{\"iosVersion\":\"1.35.0\",\"androidVersion\":\"1.35.0\",\"pcVersion\":\"1.35.0\",\"macVersion\":\"1.35.0\",\"replaysVersion\":\"1.35.0\",\"textVersion\":1,\"useNewMultiplayerVersion\":false,\"multiplayerVersions\":[],\"saveGameForceCompatibility\":[],\"currentTitleDataRevision\":1,\"minimumTitleDataRevision\":1,\"photonServerRegion\":0,\"photonChatServerRegion\":0}"
    },
    "UserData": {},
    "UserDataVersion": 1,
    "UserReadOnlyData": {},
    "UserReadOnlyDataVersion": 1,
    "UserInventory": [],
    "UserVirtualCurrency": {},
    "UserVirtualCurrencyRechargeTimes": {}
  },
  "EntityToken": {
    "Entity": { "Id": "OFFLINEENTITY0001", "Type": "title_player_account" },
    "EntityToken": "OFFLINE-ENTITY-TOKEN",
    "TokenExpiration": "2099-01-01T00:00:00.000Z"
  },
  "LastLoginTime": "2026-01-01T00:00:00.000Z",
  "NewlyCreated": false,
  "PlayFabId": "OFFLINE0000000001",
  "SessionTicket": "OFFLINE-SESSION-TICKET",
  "SettingsForUser": { "GatherDeviceInfo": false, "GatherFocusInfo": false, "NeedsAttribution": false }
}
```

> ⚠️ 现有的 `E:/warpforge/05_工具/server/responses/LoginWithSteam.json` 里 `TreatmentAssignment` 写的是
> `{"Variables": [], "VariantIds": []}` —— **`VariantIds` 这个键在 SDK 里不存在**（`SDK/ClientModels/TreatmentAssignment.cs:7-13`
> 是 `Variables` / `Variants`）。客户端不读它所以没炸，但**这是错的键名**，建议改成 `"Variants"` 或整段删掉（本文档示例已删）。

### 5.2 `POST /Client/ExecuteCloudScript`，`FunctionName = "onUserConnected"`

```json
{
  "APIRequestsIssued": 0,
  "Error": null,
  "ExecutionTimeSeconds": 0.002,
  "FunctionName": "onUserConnected",
  "FunctionResult": {
    "ServerTime": 1767225600000,
    "Achievements": {},
    "Segmentation": {},
    "grantedItems": [],
    "OverrideXp": null
  },
  "FunctionResultTooLarge": false,
  "HttpRequestsIssued": 0,
  "Logs": [],
  "LogsTooLarge": false,
  "MemoryConsumedBytes": 0,
  "ProcessorTimeSeconds": 0.0,
  "Revision": 1
}
```

⚠️ **`Segmentation` 非 null 的前提**：登录响应里 `UserReadOnlyData` 必须非 null（§2.9）。若你的登录响应里 `UserReadOnlyData` 可能是 null，就改成 `"Segmentation": null`。

### 5.3 `POST /Client/ExecuteCloudScript`，`FunctionName = "GetReadOnlyPublisherData"`

```json
{
  "APIRequestsIssued": 0,
  "Error": null,
  "ExecutionTimeSeconds": 0.002,
  "FunctionName": "GetReadOnlyPublisherData",
  "FunctionResult": {
    "Sunset": "{\"totalUSDSpentInWarpforge\":0,\"goldToCollectInLegions\":0,\"hasSentRequest\":false,\"hasBeenClaimed\":false}"
  },
  "FunctionResultTooLarge": false,
  "HttpRequestsIssued": 0,
  "Logs": [],
  "LogsTooLarge": false,
  "MemoryConsumedBytes": 0,
  "ProcessorTimeSeconds": 0.0,
  "Revision": 1
}
```

---

## 6. 没查清的部分（搜过什么、还差什么）

### 6.1 已查清但**依赖推理**的一条（不是没查，是缺一个直接证据）

| 项 | 现在的结论 | 依据链 | 还差什么 |
|---|---|---|---|
| **`data` 内的 JSON 键名大小写 = C# 字段名原样（PascalCase）** | 高置信 | ① `PlayFab` 用自己的 simple-json（`SDK/Json/SimpleJsonInstance.cs:5-21`），命名由 `PocoJsonSerializerStrategy.MapClrMemberNameToJsonFieldName`（`SDK/Json/PocoJsonSerializerStrategy.cs:28-34`）决定；② **模型上没有任何 `[JsonProperty]`**（`SDK/ClientModels/*.cs` 全查过，只有 `[Serializable]`）⇒ 默认取 `memberInfo.Name`；③ 实测：按这份 PascalCase 写的响应**客户端反序列化成功**（`P0/playfab.tsv:9-11`，`AFTER_ERROR` 列 = null） | `MapClrMemberNameToJsonFieldName` 的**方法体**读不到（PlayFab.dll 是 IL2CPP 剥体的托管 dll；`decomp_full` / `out_cpp2il_dev_ilspy` 都**不含 `PlayFab.Json` 命名空间**，只含 Assembly-CSharp）。**关键词搜过**：`PocoJsonSerializerStrategy` / `MapClrMemberNameToJsonFieldName` / `SimpleJson` 在 `d:/2/tools/decomp_full/`（0 命中，只有 `I2.Loc.SimpleJSON` 同名干扰）、`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`（0 命中）、`d:/2/tools/decomp_generic_trial/out/`（0 命中） |

### 6.2 真没查清的（写清搜过哪）

| # | 问题 | 搜过什么 | 现状 |
|---|---|---|---|
| **U-A** | `TitleData.AddressablesConfig` 缺键时，`AddressablesManager.Init(null, …)` 会不会炸 | `ILSPY/AddressablesManager.cs:2101-2108`（只是状态机 kickoff）；真体状态机 `DEC/AddressablesManager._Init_d__11__MoveNext.c` **未逐行读**（体很长且大量 `NoteDecompilerIssue`） | **未验**。现有假服务端模板**没给**这个键，客户端能打到 `ExecuteCloudScript`，但「能不能进主菜单」没有证据 |
| **U-B** | 客户端 `Application.version` 的真值（决定 `ServerVersion.pcVersion` 上限） | `P0/playfab.tsv` / `P0/login_states.tsv`（`login_states.tsv` **只有表头、零行**，probe 没采到） | **未采到**。已知请求里的 `Version` 字段是 `"1.35.0 (8) LIVE - 0"`（`E:/warpforge/05_工具/server/logs/live-20261106-064836/0003_ExecuteCloudScript.req.json`），但那是 `gameVersionWithEnviromentAndBundles`，**不是 `Application.version`**（后者被 `new Version(...)` 直接构造，必须长得像 `1.35.0`） |
| **U-C** | `ItemInstance.ItemId` 与本地资产的对应关系（是不是卡的 `uniqueId`、要不要 `CatalogVersion`） | `DEC/InventoryManager__Add.c`（只看到 `AssetLocator.GetAsset<IIdentifiable>(ItemId)`）；`ILSPY/InventoryManager.cs:1349` | **未验**。只知道「认不出就 LogError 并跳过」，不会崩 |
| **U-D** | `UserInventory` 里货币之外的东西（卡包/皮肤/卡牌实例）真正的 `ItemId` 取值 | 同上 | **未验**（服务器已关，本地没有一份真实 `GetUserInventory` 响应）。全盘按 `"PlayFabId"` 搜过 `d:/2`（`grep -rl` 超时后单独跑过，0 命中）与 `E:/warpforge`（**只有我们自己造的假响应**，见 `E:/warpforge/05_工具/server/responses/*.json`） |
| **U-E** | `UserData`/`UserReadOnlyData` 里那些 key 的**值**（`Decks` / `CurrentDeck` / `TutorialV2` … 的 DTO 形状） | 只查到读取点（§2.5 表），没查各个 DTO 的字段 | **未查**（不在本任务范围：本任务只要求「子结构形状」，不要求业务 DTO） |
| **U-F** | `AchievementPersistence` 的字段 | `ILSPY/AchievementPersistence.cs` **未读** | **未查**（现有模板给 `{}` 就够，客户端只判 null） |
| **U-G** | 登录链上除这三条之外还会打哪些 API | `E:/warpforge/05_工具/server/logs/live-20261106-064836/_index.tsv` 实测：`LoginWithSteam` → `ExecuteCloudScript[onUserConnected]` → **`ExecuteCloudScript[InitializeLiveOps]`（CloudHandle 955）** → **`POST /Object/GetObjects`**（PlayFab **Data** API，不是 Client API，404） | ❗ **任务简报里的「只需 3 个调用」与实测不符**：至少还有 `InitializeLiveOps` 与 `Object/GetObjects` 两条。它们是否「必须成功才能进主菜单」**未验** |

### 6.3 本任务**没有**做的事（避免误解）

- 没有跑 Unity、没有起游戏、没有起/停任何进程、没有改 `d:/2` 与 `E:/warpforge` 任何文件（只读）。
- **没有**核实 `E:/warpforge/05_工具/server/` 那套假服务端代码的正确性 —— 只在 §5.1 指出了一处**键名错**（`VariantIds`）。
- 没有查 `RewardInfo` / `AzureDeckInfo` / `TutorialV2SaveData` 等业务 DTO 的字段。

---

## 7. 与既有资料的差异订正（铁律 5：就地改掉、留下痕迹）

本任务在核 `LoginResult` 写入偏移时，发现**任务简报**（不是仓库文件）里有三处误读，记在这里防止传染：

| 原说法 | 实际 | 判据 |
|---|---|---|
| `+0x28→+0x10`→`+0x330` = **EntityId** | `+0x28` = `EntityToken`；取它的 `+0x10` = **`EntityKey` 对象**（`{Id,Type}`）——存的是**引用**（后面跟写入屏障 `FUN_1803f3730`），不是 Id 字符串 | `DEC/PlayerDataManager__FinishLogin.c:20-22`；getter `DEC/PlayerDataManager__get_entityKey.c`；类型 `ILSPY/PlayerDataManager.cs:5244,6600` |
| `+0x58`→`+0x338` = **EntityToken** | `LoginResult+0x58` = **`SessionTicket`**（`EntityToken` 是 0x28，`SessionTicket` 是 0x58，因为 `+0x38` 的 `DateTime?` 占 16 字节） | `DEC/PlayerDataManager__FinishLogin.c:25`；getter `DEC/PlayerDataManager__get_sessionToken.c`；`ILSPY/PlayerDataManager.cs:5244-5246,6600-6619` |
| `+0x30→+0x28→+0x70`→`+0x3e0` = **时间戳** | `InfoResultPayload.PlayerProfile.Locations` → `PlayerDataManager.userLocations`（`List<LocationModel>`，**引用存**）；消费点是 `GetLastUserLocation()` 取 `[0].CountryCode` | `DEC/PlayerDataManager__FinishLogin.c:33-40`；getter `DEC/PlayerDataManager__get_userLocations.c`；`ILSPY/PlayerDataManager.cs:5302,6817,10088-10103` |

另（对既有产出的一处小订正）：`E:/warpforge/05_工具/server/responses/fragments/playfab_profile.json` 的 `_todo` 里写
「`ProcessNewLoginResult` 会把 `onUserConnected` 返回的字典 merge 进 `UserData`」——**不准确**：
真体只把 **`Segmentation` 一个键**写进 **`UserReadOnlyData`**（`DEC/PlayerDataManager__ProcessNewLoginResult.c:83-100`），
`Achievements` 写的是 `PlayerDataManager.rawAchievementsData`（0x200，`:80`），**不进任何字典**。
