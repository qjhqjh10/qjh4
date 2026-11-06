# B152 · LoginManager 状态机与打桩点判据

> **一句话结论**：原版登录链 = `PlayerDataManager.Awake → LoginManager.Initialize → CheckDataLoaded(等 Steam) → CheckInternetConnection → TryLoginToPlayfab(状态 10) → PlayFab LoginWithSteam → ProcessLoginResult → ProceedSuccessfulLogin(65) → FinishLogin → CheckNewVersion(15→17→20) → UnpackPlayerData(30→40→50) → AsyncInitialization → successfulLogin(13)`。
> **必须打桩的服务端调用只有 3 个**：`LoginWithSteam` · `onUserConnected`(CloudHandle 10) · `GetReadOnlyPublisherData`(CloudHandle 1121)，其中**只有前两个阻塞主链**（1121 是 fire-and-forget）。
> **最大的坑**：`ProcessLoginResult` 调 `ProceedSuccessfulLogin` 时**不 await**（`LoginManager__ProcessLoginResult.c:147-149`）⇒ 整条后续链上任何异常都被**静默吞掉**，表现是**卡在某个状态不动、没有弹窗**。

---

## 0. 数据源与口径

| 用途 | 路径 |
|---|---|
| 真方法体（主力） | `d:/2/tools/decomp_full/` |
| 字段偏移 + 枚举值 | `d:/2/tools/il2cpp_out/dump.cs`（IL2CPP dump，**带 `// 0xNN` 偏移**） |
| 签名桩 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`、`d:/2/Warpforge_code/Scripts/PlayFab/` |
| 文案 | `d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json` |
| 常量（等待秒数） | `d:/4/Unity/工具/read_literal.py` 读 `GameAssembly.dll` |

⚠️ 任务书里写的第二源 `d:/2/tools/out_cpp2il_dev_ilspy/` **本机不存在**（`ls` 报 No such file or directory）；`d:/2/tools/il2csharp_src/` 只有源码 tar，未解。本次全部结论以 `decomp_full` + `dump.cs` 为准。

⚠️ **更正 B151（铁律 5）**：`B151_原版离线版_可行性方案.md:55-58` 写「卡在 `ConnectingSteam = 1`，该状态对应文案 `MenuLogin/Intro/Connecting`（更新数据板...）」——**映射写错了**。`LoginScreenManager.SetProgress` 里 **状态 1 的 key 是 `MenuLogin/Intro/ConnectingToSteam`（等待Steam）**，`MenuLogin/Intro/Connecting`（更新数据板...）是**状态 0**（`LoginScreenManager__SetProgress.c:46-70`，key 解析见 §1·b）。Steam 没开时看到「更新数据板...」，说明停的是**状态 0（默认值）**，不是状态 1。

---

## 1. 状态枚举全表

枚举定义：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/LoginManager.cs:15-33`

| 数值 | 枚举名 | 含义 | 谁设置它（文件:行号） |
|---|---|---|---|
| 0 | `notStarted` | 未开始（**字段默认值**，先于一切显示） | `LoginManager._CheckDataLoaded_d__19__MoveNext.c:32-37`（0.02 s 等待后无条件设 0） |
| 1 | `ConnectingSteam` | 等 Steam 初始化（Steam 没开就停在这里） | `LoginManager._CheckDataLoaded_d__19__MoveNext.c:43-48`（`SteamManager.Initialized == false` 时） |
| 10 | `downloadingPlayerData` | 正在连 PlayFab / 拉玩家档案 | `LoginManager__TryLoginToPlayfab.c:23-29` |
| 12 | `loginWithDevice` | 设备登录 | **无写入点**（见 §1·c） |
| 13 | `successfulLogin` | 登录链全部走完（**最后一步**） | `LoginManager._ProceedSuccessfulLogin_d__32__MoveNext.c:140-147` |
| 14 | `loadingAnalytics` | FinishLogin 之后、版本检查/解包/AsyncInit 全程 | `LoginManager._ProceedSuccessfulLogin_d__32__MoveNext.c:60-67` |
| 15 | `checkingVersion` | 版本比较 + 账号版本比较 | `LoginManager._CheckNewVersion_d__21__MoveNext.c:40-47` |
| 17 | `deserializingData` | 解包存档 / 新版本数据迁移 / ProcessData | `LoginManager._UnpackPlayerData_d__24__MoveNext.c:176-183` |
| 18 | `checkingPreviousMatch` | 检查上一场对局 | **无写入点**（见 §1·c） |
| 20 | `processingPlayerData` | 处理玩家数据（`onUserConnected` 就在这一步里） | `LoginManager._ProcessData_d__36__MoveNext.c:40-47` |
| 30 | `downloadingStoreData` | 下载商店数据（**实际只置一下就走**） | `LoginManager._UnpackPlayerData_d__24__MoveNext.c:249-255`；`LoginManager__DownloadInAppProducts.c:7-13` |
| 40 | `waitingPlugins` | 等插件/内购/卡组检查 | `LoginManager._UnpackPlayerData_d__24__MoveNext.c:256-262`；`LoginManager__DownloadInAppProducts.c:14-20`；`LoginManager__CheckPlayerDeck.c:8-14` |
| 50 | `finished` | 登录完成（**触发 `OnLoginComplete`**） | `LoginManager__TryCompleteLogin.c:15-21`；`LoginManager__ProgressUpdateCallback.c:28-34`（资源下载进度到顶时） |
| 60 | `serverFailed` | PlayFab 登录失败 | `LoginManager__ProcessLoginResult.c:61-68` |
| 65 | `processingLogin` | 收到 LoginResult，正在落地 | `LoginManager._ProceedSuccessfulLogin_d__32__MoveNext.c:46-53` |
| 70 | `downloadingBundles` | 下载资源包 | **无写入点**（见 §1·c） |

**统一写入口**：`LoginManager.SetProgress(LoginState)` = `LoginManager__SetProgress.c:7-16`：`if (currentState != new) { currentState = new; OnLoginStateChanged?.Invoke(new); }`。
- `currentState` = **对象 +0x44**（`dump.cs` 字段 `LoginManager` 对应 `LoginManager.cs:366`；由 `LoginManager__SetProgress.c:7-9` 的 `+0x44` 实证）。
- `OnLoginStateChanged` = **+0x48**（`LoginManager__SetProgress.c:8-14` 的委托调用实证）；界面侧订阅者 = `LoginScreenManager.SetProgress`。

### 1·b 状态 → 界面文案（`LoginScreenManager__SetProgress.c`，key 由 `stringliteral.json` 逐个解析）

| 状态 | 文案 key | 中文 | 进度% |
|---|---|---|---|
| 0 | `MenuLogin/Intro/Connecting` | 更新数据板... | 10 |
| 1 | `MenuLogin/Intro/ConnectingToSteam` | 等待Steam | 10 |
| 10 | `MenuLogin/Intro/Downloading` | 侦查敌人位置... | 20 |
| 12 | `MenuLogin/Intro/LoginWithDevice` | 识别人员... | 25 |
| 13 | `MenuLogin/Intro/SuccessfulLogin` | 检查凭证... | 40 |
| 14 | `MenuLogin/Intro/LoadingAnalytics` | 更新数据板... | 35 |
| 15 | `MenuLogin/Intro/CheckingGameVersion` | 检查凭证... | 50 |
| 17 | `MenuLogin/Intro/Deserializing` | 检查凭证... | 60 |
| 18 | `MenuLogin/Intro/CheckingPreviousMatch` | 计算导航坐标... | 55 |
| 20 | `MenuLogin/Intro/Processing` | 侦查敌人位置... | 65 |
| 30 | `MenuLogin/Intro/DownloadingStore` | 更新数据板... | 75 |
| 40 | `MenuLogin/Intro/WaitingPlugins` | 计算导航坐标... | 95 |
| 50 | `MenuLogin/Intro/Ready` | 进入战斗区... | 100 |
| 60 | `CustomErrors/ErrorSavingMatch`（**SetProgress 里没有专属分支，落到兜底**） | 连接服务器出错。请检查您的互联网连接并点击重试。 | — |
| 65 | `MenuLogin/Intro/ProcessingLogin` | 让我们开始吧。 | 30 |

> **「更新数据板...」一共 4 个来源**：`MenuLogin/Intro/Loading`（**登录场景的初始文案**，`LoginScreenManager__Start.c:27` 直接写死在 label 上，不对应任何 `LoginState`）+ 状态 0 / 14 / 30。⇒ **只看到这四个字，判不出停在哪一步**（这条与 B151 一致）。

### 1·c 三个「有枚举、无写入点」的状态

对 `d:/2/tools/decomp_full/` 全目录做 `grep "+ 0x44) = 0x"`，`LoginManager` 的写入点**只有**：`0`、`1`、`0xa`、`0xd`、`0xe`、`0xf`、`0x11`、`0x14`、`0x1e`、`0x28`、`0x32`、`0x3c`、`0x41`。
⇒ **12（`loginWithDevice`）、18（`checkingPreviousMatch`）、70（`downloadingBundles`）在本 build 没有任何写入点**（界面文案与进度还留着，属遗留枚举）。

---

## 2. 启动 → 各结局的逐跳路径

### 2·a 入口
```
PlayerDataManager.Awake                                   PlayerDataManager__Awake.c:118
  └ LoginManager.Initialize()                             LoginManager__Initialize.c:24-38（async void 启动 _Initialize_d__14）
      ├ 语言兜底 + 挂两个错误回调（OnCustomErrorFallback/OnPlayfabErrorFallback）
      └ StartCoroutine(CheckDataLoaded)                   LoginManager._Initialize_d__14__MoveNext.c:118-123
```

### 2·b 正常路径（Steam 在跑、服务端可达）
```
CheckDataLoaded                                           _CheckDataLoaded_d__19__MoveNext.c
  ├ WaitForSeconds(0.02)                                  :22-27   （0x1834b3290 = 0.02 s，read_literal 实读）
  ├ currentState = 0 (notStarted)                         :32-37
  ├ if (!SteamManager.Initialized) currentState = 1       :42-49
  ├ while (!Steam initialized) WaitForSeconds(0.3)        :60-68   （0x1834b2dc8 = 0.3 s，read_literal 实读）
  ├ SteamManager.RequestUserStats()                       :73
  └ CheckInternetConnection()                             :76

CheckInternetConnection                                   LoginManager__CheckInternetConnection.c
  ├ InternetReachability != NotReachable                  :19-20
  ├ PurchasesManager.Init()                               :28
  └ TryLoginToPlayfab()                                   :29

TryLoginToPlayfab                                         LoginManager__TryLoginToPlayfab.c
  ├ currentState = 10 (downloadingPlayerData)             :23-29
  └ PlayerDataManager.LoginToPlayFab(steamId, ticket, cb) :30-36
      └ CloudscriptHandler.LoginWithSteamAccount          PlayerDataManager__LoginToPlayFab.c:38-40
          └ PlayFabClientAPI.LoginWithSteam               CloudscriptHandler__LoginWithSteamAccount.c:45
              └ 成功回调 → cb(true, LoginResult, null)     ..._LoginWithSteamAccount_b__0.c:19-27

ProcessNormalLoginResult → ProcessLoginResult             LoginManager__ProcessNormalLoginResult.c:22
  └ success == true → ProceedSuccessfulLogin(loginResult)  LoginManager__ProcessLoginResult.c:147-149（**不 await**）

ProceedSuccessfulLogin                                    LoginManager._ProceedSuccessfulLogin_d__32__MoveNext.c
  ├ currentState = 65 (processingLogin)                   :46-53
  ├ PlayerDataManager.FinishLogin(loginResult, false)     :59
  ├ currentState = 14 (loadingAnalytics)                  :60-67
  ├ await CheckNewVersion()                               :90-98
  ├ await PlayerDataManager.AsyncInitialization()         :119-135
  └ currentState = 13 (successfulLogin)                   :140-147

CheckNewVersion                                           LoginManager._CheckNewVersion_d__21__MoveNext.c
  ├ currentState = 15 (checkingVersion)                   :40-47
  ├ PlayerDataManager.UnpackOnlineVersionData()           :52
  ├ CheckUpdateRequired() 为真 → loginFailure=2 → TryCompleteLogin → return   :57-63
  ├ CheckAccountUserInNewerVersion() 为真 → loginFailure=3 → TryCompleteLogin → return  :68-73
  └ await CheckNewVersionSucceed()                        :96-98

CheckNewVersionSucceed → await UnpackPlayerData           _CheckNewVersionSucceed_d__23__MoveNext.c:65-66

UnpackPlayerData                                          LoginManager._UnpackPlayerData_d__24__MoveNext.c
  ├ currentState = 17 (deserializingData)                 :176-183
  ├ DoNewVersionOperations / SetupOverrides               :188-194
  ├ await ProcessData()                                   :217-234
  │   └ currentState = 20 (processingPlayerData)          _ProcessData_d__36__MoveNext.c:40-47
  │       └ await PlayerDataManager.ProcessNewLogin()     :91
  │           └ GenericCloudScriptHandlerAsync<object>(10)  PlayerDataManager._ProcessNewLogin_d__533__MoveNext.c:29
  │               └ ProcessNewLoginResult                 PlayerDataManager__ProcessNewLoginResult.c
  ├ UnpackUserInfo / SetTutorialState / FixSkirmishDeck   :56,85,137
  ├ currentState = 30 (downloadingStoreData)              :249-255
  ├ currentState = 40 (waitingPlugins)                    :256-262
  ├ TryCompleteLogin()                                    :263
  │   └ currentState = 50 (finished) + OnLoginComplete    LoginManager__TryCompleteLogin.c:15-37
  └ SetupHelpshift / AttributionManager                   :268-275

AsyncInitialization                                       PlayerDataManager._AsyncInitialization_d__514__MoveNext.c
  ├ SetupPlayfabPublisherVariables()                      :31
  ├ GetAllPublisherPlayerData()  ← CloudHandle 1121，**不等**  :32
  ├ await UnityServicesManager.InitializeUnityServices()  :33-51
  └ GameAnalytics.SetupAnalytics()                        :61-64
```

**一句话链**：
`0 →（Steam 未就绪时 1）→ 10 → 65 → 14 → 15 → 17 → 20 → 30 → 40 → 50 → 13`

### 2·c 失败路径 A：PlayFab 登录失败（关服后的实际结局）
```
LoginWithSteam 失败 → errorCb(0, 0, PlayFabError, "")   ..._LoginWithSteamAccount_b__1.c:11-21
  → ProcessNormalLoginResult → ProcessLoginResult(success=false, error)
      ├ currentState = 60 (serverFailed)                LoginManager__ProcessLoginResult.c:61-68
      ├ 账号类错误码 → 清登录态 + SavePlayerData + ShowPlayfabError   :84-107
      └ 其它错误码   → LogWarning + ShowPlayfabError                  :109-118
```
⇒ 这台机器（Steam 在跑、PlayFab 不可达）**实测可见序列应为 `0 → 10 → 60`**（本文未跑实机，是代码读出的结论）。

### 2·d 失败路径 B：Steam 没开
```
SteamManager.Awake → SteamAPI_RestartAppIfNecessary 失败 → 进程停住
=========================================================
（若进程还活着且没走到 PlayerDataManager.Awake ⇒ currentState 保持默认 0，界面显示「更新数据板...」）
（若走到了 CheckDataLoaded 而 Steam 仍不可用 ⇒ currentState = 1，界面显示「等待Steam」）
```

### 2·e 卡住不动（无弹窗）—— 异常被吞的唯一位置
`ProcessLoginResult` 里 `AsyncTaskMethodBuilder.Start<ProceedSuccessfulLogin>(...)` 之后**只 `get_Task()` 一次、没人 await**（`LoginManager__ProcessLoginResult.c:147-149`）⇒ 这条 Task 上任何异常都变成 **unobserved task exception（静默）**。
`FinishLogin` / `CheckUpdateRequired` / `CheckAccountUserInNewerVersion` / `ProcessNewLoginResult` 都是 **`FUN_1803f47a0()` = `throw`** 结尾 ⇒ 命中就是**卡在当前状态、没有弹窗**。

---

## 3. 最小打桩点清单

> 前提：只求「进主菜单」，不求任何在线数据。**按这张表从上到下打，前 3 条是硬阻塞，第 4 条按「伪造 payload 的方式」决定要不要打。**

| # | 类.方法 | 签名 | 该返回什么 | 不改会怎样 | 出处 |
|---|---|---|---|---|---|
| 1 | `CloudscriptHandler.LoginWithSteamAccount` | `static void LoginWithSteamAccount(string steamTicket, Action<bool, LoginResult, PlayFabError> callback)` | `callback(true, <伪造 LoginResult>, null)` | 关服后走 error 回调 ⇒ 状态停在 **60 serverFailed** + 「连接服务器错误」弹窗 | 桩 `CloudscriptHandler.cs:275`；实现 `CloudscriptHandler__LoginWithSteamAccount.c:45`；回调 `..._b__0.c:19-27` |
| 2 | `PlayfabWrapper.GenericCloudScriptHandlerAsync<object>`（`handle == CloudHandle.onUserConnected` = **10**） | `static Task<T> GenericCloudScriptHandlerAsync<T>(CloudHandle handle, object payload)` | 返回一个**非 null** 对象；`+0x10` long / `+0x18` string / `+0x20` Dict / `+0x28` List / `+0x30` `Nullable<int>` | 返回 null ⇒ `ProcessNewLoginResult` 走到 `throw`（`PlayerDataManager__ProcessNewLoginResult.c:42,119-121`）⇒ **卡在状态 20 不动、无弹窗** | 桩 `PlayfabWrapper.cs:111`；调用 `PlayerDataManager._ProcessNewLogin_d__533__MoveNext.c:29`；消费 `PlayerDataManager__ProcessNewLoginResult.c:42-113` |
| 3 | `PlayerDataManager.FinishLogin` 的**入参**（不是改它，是要喂对） | `void FinishLogin(LoginResult loginResult, bool isGuestAccountFlag = false)` | 见 §4 的 6 个硬约束 | 返回 null → 只 `LogError` 后 return（状态不前进）；`EntityToken`/`InfoResultPayload` 为 null → **throw** | 桩 `PlayerDataManager.cs:1553`；实现 `PlayerDataManager__FinishLogin.c:14-51` |
| 4a | `PlayerDataManager.CheckUpdateRequired` | `bool CheckUpdateRequired()` | `false`（若 `TitleData["ServerVersion"]` 能给全 → 自然为 false，可不打桩） | `serverVersion == null` ⇒ **throw**（`PlayerDataManager__CheckUpdateRequired.c:48-49`）⇒ 卡状态 14 | 实现 `:26-47` |
| 4b | `PlayerDataManager.CheckAccountUserInNewerVersion` | `bool CheckAccountUserInNewerVersion()` | `false` | `serverVersion.saveGameForceCompatibility`（**+0x48**）为 null ⇒ `FindVersionOverride` **throw**（`ServerVersionConfig__FindVersionOverride.c` 末尾）⇒ 卡状态 15 | 实现 `PlayerDataManager__CheckAccountUserInNewerVersion.c:12-18` |
| 5 | `PlayfabWrapper.GenericCloudScriptHandler<object>`（`handle == 1121` `GetReadOnlyPublisherData`） | `static void GenericCloudScriptHandler<TOut>(CloudHandle handle, object payload, Action<TOut> onComplete, Action<string> onError = null)` | 可以不回（**没人 await**）；回了就解析成 `SunsetPublisherData` | **不阻塞**，失败只打一条 `LogError`；不改也能进主菜单 | 桩 `PlayfabWrapper.cs:120`；调用 `PlayerDataManager._GetAllPublisherPlayerData_d__515__MoveNext.c:39`；回调 `PlayerDataManager___GetAllPublisherPlayerData_g__OnSucess_515_0.c` |
| 6 | `UnityServicesManager.InitializeUnityServices` | `static Task InitializeUnityServices()` | 立即完成（返回已完成的 Task） | `await` 它（`PlayerDataManager._AsyncInitialization_d__514__MoveNext.c:33-51`）；离线时可能慢/抛 ⇒ 抛则**整条 ProceedSuccessfulLogin 静默失败**，**卡在状态 14** | `UnityServicesManager._InitializeUnityServices_d__0__MoveNext.c:60-105` |

**最小集合（不改代码、只伪造 payload）**：`#1` 给一个字段齐全的 `LoginResult` + `#2` 给一个非 null 的 onUserConnected 结果 ⇒ 就能走到主菜单；`#4a/#4b` 由 payload 里的 `TitleData["ServerVersion"]` 决定要不要额外打桩（见 §5·a）。
**最小集合（允许改代码）**：`#1 + #2 + #4a + #4b + #6` 五个方法直接返回，最稳。

> 注：`PlayerDataManager.LoginToPlayFab` 不用单独打 —— 它只是把回调转给 `CloudscriptHandler.LoginWithSteamAccount`（`PlayerDataManager__LoginToPlayFab.c:29-40`），在 #1 处截断即可。
> 注：`DownloadInAppProducts` / `CheckPlayerDeck`（都只设状态 30/40 再 `TryCompleteLogin`）**在 `decomp_full` 与 prefab 里都找不到调用点**（`grep -rl` 只命中 `LoginManager.cs` 自身 + `assetripper_out/.../LoginManager.cs` 桩）⇒ 本 build 是死方法，不用打桩。

---

## 4. `FinishLogin` 的读取顺序（逐字段 + 失败分支）

签名：`PlayerDataManager.FinishLogin(LoginResult loginResult, bool isGuestAccountFlag = false)`（桩 `PlayerDataManager.cs:1553`）
实现：`d:/2/tools/decomp_full/PlayerDataManager__FinishLogin.c`
`LoginResult` 布局：`d:/2/tools/il2cpp_out/dump.cs:613713-613726`

| 序 | 读 `LoginResult` 的哪个字段 | 偏移 | 落点（`PlayerDataManager`） | 出处 | 失败分支 |
|---|---|---|---|---|---|
| 0 | `loginResult == null` 检查 | — | — | `:14-17` | **只 `LogError` 后 return**（不崩、也不前进） |
| 1 | 先打日志读**旧的** `playFabId` | — | `+0x328`（旧值） | `:18-19` | — |
| 2 | **`PlayFabId`** | `+0x50` | `+0x328 playFabId` | `:20` | — |
| 3 | **`EntityToken`**（判空闸） | `+0x28` | — | `:22` | **为 null ⇒ `throw`** |
| 4 | `EntityToken.Entity`（`EntityKey`） | `EntityToken+0x10` | `+0x330 entityKey` | `:23`（`EntityTokenResponse` 布局 `dump.cs:611500-611512`） | — |
| 5 | **`SessionTicket`** | `+0x58` | `+0x338 sessionToken` | `:25` | — |
| 6 | **`InfoResultPayload`** | `+0x30` | `+0x2a8 userInfo` | `:29` | — |
| 7 | **`NewlyCreated`** | `+0x48` | `+0x340 newlyCreatedAccount` | `:31` | — |
| 8 | 形参 `isGuestAccountFlag` | — | `+0x3d8 isGuestAccount` | `:32` | — |
| 9 | 再判 **`InfoResultPayload`** 非空 | `+0x30` | — | `:33` | **为 null ⇒ `throw`** |
| 10 | `InfoResultPayload.PlayerProfile`（判空） | `+0x28` | — | `:34-38` | 为 null ⇒ 跳过（**不抛**），`userLocations` 保持原值 |
| 11 | `PlayerProfile.Locations` | `PlayerProfile+0x70` | `+0x3e0 userLocations` | `:37-40` | — |
| 12 | `DateTime.Now` | — | `+0x1a0 localLoginTime` | `:44-45` | — |
| 13 | `PlayerDataManager.SavePlayerData()` | — | 落本地存档 | `:46` | — |

> **伪造 `LoginResult` 的 6 个硬约束**：`PlayFabId` 非空 · `EntityToken` 非空 · `EntityToken.Entity` 非空 · `SessionTicket` 非空 · `InfoResultPayload` 非空 · `UserData`/`TitleData` 见 §5·a。
> ⚠️ `EntityToken.Entity` 只有 `LoginWithSteam` 会下发；**用假 payload 时务必把这三个对象都 new 出来**，否则第 3 / 9 步直接 `throw`，而异常被 §2·e 吞掉。

---

## 5. 版本检查 / 「连不上」的判定逻辑

### 5·a 版本检查（**方向已确认**）

`PlayerDataManager.CheckUpdateRequired()`（`PlayerDataManager__CheckUpdateRequired.c`）：
```
current = new Version(Application.version)                       :23-25
required = serverVersion.pcVersion      // ServerVersionConfig +0x20   :27
if (string.IsNullOrEmpty(required)) return false                 :28-29,43-45
return current < new Version(required)                           :41   ← 方向：当前 < 要求 ⇒ true（要更新）
```
- `serverVersion` = `PlayerDataManager + 0x1d0`（`dump.cs` 字段 `serverVersion`）。
- 它由 `PlayerDataManager.UnpackOnlineVersionData()` 从**登录响应**里取：
  `serverVersion = OnlineData.GetServerVersion(userInfo)` `PlayerDataManager__UnpackOnlineVersionData.c:25-27`；
  `playerProfileVersion = OnlineData.GetPlayerGameVersion(userInfo)` `:28-30`。

`PlayerDataManager.CheckAccountUserInNewerVersion()`（`:12-18`）：
```
overrideVer = serverVersion.FindVersionOverride()
return new Version(overrideVer) < playerProfileVersion            ← 方向：覆盖版本 < 玩家存档版本 ⇒ 账号来自更新的版本
```

**`TitleData["ServerVersion"]` 缺了会怎样**（关键，容易踩）：
- `OnlineData.GetServerVersion` **不抛**，而是造一个**只填 3 个字段**的兜底 `ServerVersionConfig`（`OnlineData__GetServerVersion.c:62-84`）：`androidVersion`、`iosVersion`、`textVersion`；**`pcVersion`(+0x20) 与 `saveGameForceCompatibility`(+0x48) 保持 null**。
- ⇒ `CheckUpdateRequired` 因为 `pcVersion` 空返回 **false**（安全）。
- ⇒ `CheckAccountUserInNewerVersion` 因为 `+0x48 == null` 在 `FindVersionOverride` 里 **throw**（`ServerVersionConfig__FindVersionOverride.c:34-50`）⇒ 卡状态 15。

**`UserData["ProfileVersion"]` 缺了会怎样**：`OnlineData.GetPlayerGameVersion` 取不到 key 时返回**空串**，外层 `IsNullOrEmpty` 成立 ⇒ 回落 `new Version("0.0")`（`OnlineData__GetPlayerGameVersion.c:32-45`）⇒ **不抛**。

### 5·b 「连不上」的决定点与异常去向

| 层 | 判据 | 失败后 | 出处 |
|---|---|---|---|
| 1. Unity 网络可达性 | `Application.internetReachability == NotReachable` | `ShowErrorMessage(CustomError.InternetUnreachable = 10)` ⇒ 文案 key `CustomErrors/InternetUnreachable`（「连接服务器出错。请检查您的互联网连接并重试。」） | `LoginManager__CheckInternetConnection.c:19-38`；`CustomError` 枚举 `dump.cs:45985-46015`；文案同族机制 `CloudscriptHandler__HandleCustomError.c:98-107` |
| 2. PlayFab HTTP 请求 | SDK 在**回调**里回 `PlayFabError`（不发异常给上层） | 错误回调 `(0, 0, PlayFabError, "")` | `CloudscriptHandler.__c__DisplayClass40_0___LoginWithSteamAccount_b__1.c:11-21` |
| 3. 错误码分流 | `IsAccountLoginError(code)`：`1078 / 1000..1003 / 1322 / 1142 / 1026 / 1271 / 1275` 之外的走「仅提示」 | 两者都 `ShowPlayfabError(error)` | `LoginManager__IsAccountLoginError.c`；调用 `LoginManager__ProcessLoginResult.c:84-118` |
| 4. 弹窗文案 | `String.Format("PlayfabErrors/{0}", <PlayFabErrorCode 枚举名>)` | 例：`ServiceUnavailable` → 「连接服务器错误。请检查您的互联网连接并再试一次。…」 | `ErrorPopupWindowController__ShowPlayfabError.c:19-31`（`DAT_18425e0c8` = `PlayFabErrorCode` 类型 ⇒ `{0}` 是**枚举名**，不是数字）；`PlayfabErrors/*` 共 189 条 |
| 5. 超时在哪一层 | `PlayFabSettings.RequestTimeout`（SDK 全局，默认值不在本地源码里）；**Unity 侧没有任何自己的超时/重试计数** | 超时由 SDK 内部转成 `PlayFabError` 回调；**我们自己的代码不 catch 也不计时** | `d:/2/Warpforge_code/Scripts/PlayFab/PlayFab/PlayFabSettings.cs:103`（只有属性声明） |
| 6. 登录之后的异常 | 不在任何 try 里 | 见 §2·e：整个 `ProceedSuccessfulLogin` Task 被丢弃 ⇒ **静默** | `LoginManager__ProcessLoginResult.c:147-149` |

> ⚠️ 关于用户看到的「连接服务器**出错**」（不是「错误」）：`CustomErrors/InternetUnreachable` 用的正是「出错」；`PlayfabErrors/ServiceUnavailable|InternalServerError` 用「错误」。两条都可能出现，**取决于停在第 1 层还是第 2 层**。本地关服实测时是第 1 层还是第 2 层，本次**没有跑实机**，无法区分（见 §6）。

---

## 6. 没查清的部分

1. **本地关服时实际停在哪一层**：`Application.internetReachability` 在有网卡但无出口时返回非 0（判为「在线」）⇒ 理论上走 PlayFab 失败；但**没跑实机取 `currentState`**，无法确认是 60 还是 0/14。搜过 `d:/4/Unity/资料/` 下的交接与实拍记录，只有文字描述，没有 `currentState` 数值记录。
2. **PlayFab SDK 的默认 `RequestTimeout`**：`PlayFabSettings.RequestTimeout` 在本机是**签名桩**（getter 返回 `0`），真值在 `Assembly-CSharp` 之外的 PlayFab DLL 里；`d:/2/tools/out_cpp2il_dev_ilspy/` 不存在、`il2csharp_src` 未解包 ⇒ **拿不到**。也**没有**在任何反编译文件里找到「有人写过这个属性」的赋值点（`grep RequestTimeout` 只命中桩本身）。
3. **`onUserConnected` 返回 JSON 的字段语义**：只知道消费端的 5 个偏移（`+0x10 long` / `+0x18 string` / `+0x20 Dict→合并进 UserData` / `+0x28 List` / `+0x30 Nullable<int>`，`PlayerDataManager__ProcessNewLoginResult.c:42-113`），**不知道服务端实际下发哪些键**（关服，远端拿不到）。⇒ 打桩时给 `{}` 应该能过（`+0x10` 为 0、`+0x18/-0x20/-0x28` 为 null 时只走 `LogWarning`，`:76-78`）；**但这一条未实测**。
4. **`UploadOnlineWarpforgePlayed` 具体发哪个请求**：`AsyncInitialization → SetupPlayfabPublisherVariables → UploadOnlineWarpforgePlayed`（`PlayerDataManager__SetupPlayfabPublisherVariables.c:3-6`），它是 fire-and-forget 协程，**不在主链上**；但它到底调 `UpdateUserPublisherData` 还是 CloudScript，`PlayerDataManager__UploadOnlineWarpforgePlayed.c` 里的 `FUN_18176cce0` 未能解出符号 ⇒ **记为「未查清」**（不影响进主菜单）。
5. **`CustomError` → 文案的拼接点**：`ErrorPopupWindowController.ShowErrorMessage(CustomError, ...)` 的**重载体没有单独反编译产物**（`decomp_full` 只有一份 `ErrorPopupWindowController__ShowErrorMessage.c`，内容是 `(string,bool,Action)` 那一版）。「`CustomErrors/` + 枚举名」这个拼法是从同族 `CloudscriptHandler__HandleCustomError.c:98-107` 推的，**不是该方法自身的直接证据**。
6. **`Application.version` 的真实值**：`CheckUpdateRequired` 拿它去比；本机 `Warpforge_Data/StreamingAssets/build_info` 写的是 `Build from EG-MARCOS-WIN at 09/02/2026 18:44:25`，没有形如 `x.y.z` 的版本号 ⇒ 若 `PlayerSettings.bundleVersion` 不是三段式，`new Version(...)` 会抛。**未核**。
7. **`LoginState` 12 / 18 / 70 的死活**：已确认 `decomp_full` 全目录无写入点；但**没查**「是否被 UnityEvent / 反射 / 我们没解出的 DLL 写」——`assetripper_out` 的 prefab 里只查到 `LoginManager.cs` 桩，`m_MethodName` 未逐条扫。
