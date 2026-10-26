# B9 · J9SC · 社交/聊天域 48 条 C 行的施工单（只读判据代理报告）

> 执行代理 **J9SC** · 2026-10-26（第八十四会话「完全复刻」）
> 输入（**未重做**，只补证据）：`账/社交_聊天.tsv`（`状态==C` = **48 条**；逐行核过「账行号 ↔ 原版类 ↔ C」missing 0 / extra 0）· `资料/普查产出_0927/社交_联盟与好友页.md` · `资料/普查产出_0927/多人界面_入口与调用.md` · `普查产出_第八十会话/G2_未进账类全量.tsv` · `判据/B9_菜单商店C_施工单.tsv:103-105`
> 第一权威 = `d:/2/tools/decomp_full/`（逐方法 `.c`），第二 = `cpp2il/out_cpp2il_dev_ilspy`（交叉）/ `Assembly-CSharp` 签名桩 / `bundle_menus_assets_all` 序列化字段
> 纪律：⛔ 未改任何 `.cs` / 资产 / 主账表 / 正本；⛔ 未跑 Unity；⛔ 未动 git。行号一律按**二进制**读源码后换算
> 交付：`判据/B9_社交C_施工单.tsv`（12 列 × 48）· `判据/B9_社交C_派活包.md` · 本报告

---

## ① 结论

### 1·1 一句话

48 条 C 行**全部**落到了「原版逐句步骤 + 具体目标文件 + 可执行验收口」；按「本地 / 远端」切完是：

| 分类 | 条数 | 含义 | 派活含义 |
|---|---:|---|---|
| **纯本地可做** | **28** | 判据 + 数据 + 资源全在本地，能直接做到 A | 直接照施工单做 |
| **机制本地可做 + 真实数据需远端** | **18** | UI/状态机/换色/排序等**全部可落地**；真实组数据/配置/LiveOps 缺源 ⇒ 喂本地表即可验 | **照做**（⛔ 不是「不做」）；数据到了自动生效 |
| **只有远端入口** | **2** | `Inbox.RefreshHandler` / `Inbox.ValidateData`（P3） | 只做「不静默」的桩；396 的版本闸拆出来可做 |

⇒ **本地可做 28 条 · 需远端数据 20 条（其中 18 条现在就能照原版落地）**。

优先级重判（按 `CLAUDE.md §四`）：**P0 = 1 条**（账 187 战斗内联盟窗）· **P1 = 1 条**（账 283 开场白语音）· **P2 = 44 条** · **P3 = 2 条**（⛔ 不判 D）。

### 1·2 现读推翻了主账 6 处旧说法（详见 §五，⛔ 别照着旧说法开工）

1. 账 63/65：『开箱图本仓 `Resources/` 没有』⇒ **10 张（开/闭各 5）都在** `Resources/Art/ui_menu/`，`CardArt.MenuUi` 的取图根正是它。
2. 账 291/302：『15 种系统消息模板』⇒ 原版只有 **5 个模板类型 + DeckShare**，其余类型返回空串；6 条词条**本地官方表全有**。
3. 账 26③：『isPlayerEntryColor 本地读不到值』⇒ prefab 序列化字段**实读** `(0.15258682, 0.48235294, 0, 1)`，作用对象 = `background` + `Image`。
4. 账 88/73：『徽标图本地没有』⇒ 要分三层（**图在** / **映射不在** / **Default 兜底在**）。
5. 账 460：『OfferBadge 全为编译器样板』⇒ 它有 `storeId` 字段 + 单参构造，**不是纯样板**。
6. 账 459：`BoosterPackBadge` **与商店线施工单重复**（`B9_菜单商店C_施工单.tsv:103-105`），跨域只能一个写手。

### 1·3 给调度台的三条请求

1. **账 63/65 复核改 A**：请按 §五 5·1 的两条现读（图在 + 逻辑已落地）核一次；通过就在账里改 A 并把「图在 `Resources/Art/ui_menu/`」写进备注，否则下轮又会照旧说法判缺。
2. **账 459 去重**：`BoosterPackBadge` 的**类型**归本域（`Shell/BadgeNotifications.cs`）、**provider** 归商店线（`Shell/Inventory.cs`），请指定唯一写手。
3. **账 128 出厂态变更**：照原版 `leaderBoardID` 为空时两颗榜钮应 `SetActive(false)`；这会改观感 ⇒ 若同意，请同步允许改 `Editor/MainMenuScene.cs` 里相应断言。

---

## ② 逐条证据

> 每条只给**判据出处 + 关键读数**；完整 12 列（含施工步骤/验收口/风险）在 `判据/B9_社交C_施工单.tsv`。
> 下面按 6 个族分组，族内行号即账行号。

### 2·1 联盟列表行（14 / 21）

- `AllianceInvitationEntry__Initialize.c:2-117`：① null ⇒ `LogError+Destroy`（:102-113）② 存字段（:35-37）③ `ConfigManager.GetConfig<AlliancesConfig>()` 取 `maxMemberCount`（:42）④ `nameText`（:44-45）⑤ `regionText = GetTermTranslation(前缀+region)`（:46-54）⑥ `Format("{0}/{1}", MemberCount, max)`（:55-64）⑦ **`joinButton.interactable = MemberCount < max`**（:65-70）⑧ `rankingValue = rating.ToString()`（:71-77）⑨ `contentFixer.FixLayout()`（:78-79）⑩ `badgeDrawer.Draw(Group.get_Badge())`（:80-85）。
- `AllianceListEntry__Initialize.c:2-117`：比上一条多一段**隐私闸** —— `privacy(+0x40)==0` 时 `interactable = MemberCount < max`，否则**保持 false**（:83-88）；`privacy==1` ⇒ Join 钮文案 = `GetTranslation("SocialMenu/Alliances/Privacy/Private")`（:89-101）。
- 🔴 我们缺：`SocialData.AllianceListing` **没有 `Privacy` 字段** ⇒ 21 的 ②③ 无从表达（派活前置 → J-01）。
- 🔴 **`AlliancesConfig` 资产本地不存在**（已逐 bundle 核 `assets_full`）⇒ `maxMemberCount` / `createAlliancePrice` 只能取行数据或 prefab 样例值；⛔ 别用 `SocialData.MemberCap = 20`（那是 prefab 样例串 `Members: --/20` 的读数）顶替 `maxMemberCount`。

### 2·2 联盟成员行与成员页（26 / 30 / 53）

- `AllianceMemberEntry__Initialize.c:4-239`：`indexText`（:44-47）· `AvatarDisplay.Initialize`（:56-75）· `ToggleShowInfoOnClick(false)`（:76-78）· `playerName`（:79-83）· **`playerRole = GetTranslation("…Role/" + ((GroupRole)role).ToString())`**（:84-108）· 两处评级（:110-150，上游 = `group.currentSeason` / `TimedDraftEvent`）· 在线状态三档换色（:151-189，`ChatGlobalManager.GetPlayerStatus`）· 行钮（:190-196）· **IsPlayer 换色**（:197-216）。
- 🔴 **IsPlayer 色值已实读**（推翻旧说法）：`bundle_menus_assets_all/MonoBehaviour_-4586463415933622350.json:44-49` ⇒ `isPlayerEntryColor = (0.15258681774139404, 0.48235294222831726, 0.0, 1.0)`；另一份 `…_-2068273713529560925.json`、`…_4905926295992066211.json` **同值**。作用对象 = `graphicsToChangeColorIfIsPlayer` 的两颗 PPtr（反查 = `GameObject/background_5687845496662894514.json` 与 `GameObject/Image_-2532075377729034318.json`）⇒ **`background` + `Image`**。
- `AllianceMemberList__HandleMemberClick.c:2-17`：**整方法只有一句转发**（触发 `OnMemberClicked`，三参 = `(this, member, memberTransform)`）——开窗在订阅方，账 30 的「开窗已接上」与这条一致。
- `AllianceMemberTab__Initialize.c:2-74`：`SetActive(true)` → 按 `+0x40`（= 页签记忆）二选一（:13-64）→ `AlliancesManager.GetCurrentGroup(callback)` **重拉再画**（:66-69）。
- 我们缺：`ToFocus` 覆写（父类空实现已核 `Shell/WindowTabBase.cs:82`）、页签记忆、成员 `Role` 的 int 形态（牵动 `AllianceMemberOptionsPopup` 的 outrank）。
- ⚠️ 我们已有的正确件（⛔ 别重做）：`SortedMembers()` 已按 `OrderByDescending(Rating)` + 稳定比较器；`Rating()` 已落 `disableIfZero`（账 60①）；`RoleText` 已走 4 条官方词条。

### 2·3 联盟页签 / 详情 / 编辑态（68 / 70 / 93 / 95 / 97 / 100 / 101）

- `AllianceSearchTab__Initialize.c:2-82`：`SetActive(true)` → 读 `+0x58`（onCreateTab）二选一：**Join 支会写 `joinButtonText = GetTranslation("SocialMenu/Alliances/Join")`**（:12-51）、Create 支不写（:53-78）。
- `AllianceSearchTab__ShowCreateAllianceMenu.c:2-35`：`state==0` ⇒ **直接 return**（:7-9）；否则 Toggle/视图三支 + `CreateAllianceMenu.Initialize()` + `onCreateTab=1`，**不碰 joinButtonText**。
- `AllianceView__Initialize.c:2-39`：`SetActive(true)` → `allianceView` 先 `SetActive(false)`（:17-18）→ `groupId==null` 走 null 回调（:23-27）、否则 `AlliancesController.GetGroupById`（:28-31）。
- `AllianceView__Draw.c`：badge（:73）· region = `Format(GetTranslation(...), region)`（:88）· `toggleEditButton` 显隐 = `isCurrentGroup && role>=Admin`（:92-116）· 成员列排序（:131-148）· 两处 `AllianceRatingDisplay.Initialize`（:154 / :185）· `allianceGO.SetActive(true)`（:193）。
- `AllianceView__OnEditCancel.c:2-58` / `__OnToggleEditClicked.c:2-102`：**九句对九句**的状态机（cancel: 两个 `interactable=false` + 五颗关 + 两颗开；edit: 两个 `interactable=true` + `descriptionText.interactable = !blockDescriptionChange` + 两个下拉开且 `.value` 复位 + Edit/regionText 关）。
- `AllianceView__Start.c:2-155`：语言下拉 = `GameStaticData.GroupLanguages.ToList()`（:33-57）· 隐私下拉 = `Enum.GetNames(typeof(GroupPrivacy)).ToList()`（:58-88）· 三颗钮的 onClick（:89-106）· `+0x80` 的 `onTranslationReady` 多播（:107-131）· `descriptionText` 的 DropDelegate（:132-140）。
- 🔴 **两处口径别混**：行内 Join 钮用**词条** `SocialMenu/Alliances/Privacy/Private`（本地有值 `Private`/`私人`），两个下拉用**枚举名** `Open`/`Private`（原版就是 `Enum.GetNames`）。
- 词条可用性（本地官方表实读）：`SocialMenu/Alliances/Role/{Member,Moderator,Admin,Leader}` 4 条 · `Privacy/{Open,Private}` 2 条 · `Trophies/{Count,Count/Singular,CurrentFeatured}` 3 条 · `Messages/{Join,Leave,KickPlayer,Promoted,Demoted,DeckShare}` 6 条 —— **全有值**。
- 本地缺：`GameStaticData.GroupLanguages`（stub 只有 `public static string[]`，无赋值点 ⇒ 远端配置灌）· `FeatureConfig.IsArmyHidden` 的配置资产 · `AlliancesConfig`。

### 2·4 记分条 / 奖杯 / 排行 / 积分面板（63 / 65 / 73 / 88 / 90 / 128）

- `AllianceScoreBar__SetScoringMilestones.c:2-79`：三态逐句（**换开箱图** :67-73 · **alpha×0.5** :50-64，常量 `DAT_1834b2bb4` · **不动**）· 行数 > 阈值数 ⇒ `CustomDebug.LogError("[ERROR] Alliances scoring bar: There are more scoring lines that thresholds")`（:31-34）。
- `AllianceScoreBarLine__ShowChestOpen.c:2-12`：**一句** `Image.sprite = chestOpen`。
- 🔴 **图在本仓**：`Resources/Art/ui_menu/` 实列 `40k_Crate_Tier1_Iron(.open)` … `Tier5_Warp(.open)` = **10 张**；`Shell/AllianceEventScoreInfo.cs:70-72` 的 `CrateOpenArt` 名字逐字一致，`Art()` → `CardArt.MenuUi` → 该目录。
- `AllianceTrophyEntry__Initialize.c:2-79`：进度 `SetProgress(value,max,fmt,true)`（:42，`target==0 ⇒ (1,1)`）· `IsFilled`/`CurrentTier>=tiers.Count` ⇒ 整条关（:43-59）· `badgeDrawer.Draw(GetCurrentStatus())`（:60-64）。
- `AllianceTrophiesView__Draw.c`：badge（:115）· 三条词条（:120-142）· `IsArmyHidden`（:175）· 逐枚 Initialize（:188）· `ToggleSelected`（:195）。
- `AllianceUIRankingRow__Config.c:2-43`：`base.Config(rowData)`（:13）→ 类型校验（:31-42）→ `guildNameText = rowData[3]`（:19-21）→ `badgeDrawer.Draw(rowData[7])`（:22-24）。⚠️ 父类 `UIRankingRow__Config.c` 本代理**未读**（决定 90② 能否落）。
- `AlliancesEventScorePanel__Initialize.c:2-149`：`IsInGroup` 两支（:20-63）· 记分条四句（:73-88，含 `InterpolateMilestones`）· **`leaderBoardID` 两态**：空 ⇒ 两颗榜钮 `SetActive(false)`（:123-137）；非空 ⇒ 挂 `LeaderboardButtonClick` + 存 `leaderboardToOpen`（:91-115）。
- 🔴 **徽标族三层结论**（73/88/90/187 共用）：图在（22 张：18 军团 + 4 TIER + Default）· 映射不在（`AllianceBadge` SO 全库 0 个）· Default 兜底在。

### 2·5 控制器缓存 / 战斗内联盟窗（117 / 123 / 187）

- `AlliancesController__InvalidateLocalGroup.c:2-18`：**三句内存清零**（`cachedAt = default(DateTime)` · `cachedGroups = null` · `currentGroup = null`），**零后端调用**。
- `AlliancesController__SetCurrentGroupCached.c:2-75`：`currentGroup = g` · `cachedGroups = g.members.FirstOrDefault(pred)` · `cachedAt = TimeHelper.ServerDateTime()` · 列表里 `FindIndex` 替换或追加。
- `BattleAlliancePanel__Open.c:2-154`：`localScale` 首记（:29-37）· 名/称号（:38-49）· 评级（:50-79，图标走 `LiveOpsAssetUtility`）· 头像（:80-81）· **联盟名两支显隐 + `NotInAnAlliance` 文案**（:82-115）· `SetActive(true)`（:116-119）· **出场 `alpha 0→1` + `scale ×0.75→1`，各 0.35s**（:121-138）。
- 我们的调用点：`Battle/BattleDriver.cs:5867` `_alliancePanel.Open(AlliancePanelName, "", "")` —— 称号/联盟名本地无源（照原版单机）；**但玩家分数没传**（`Ctx` 里有）⇒ 评级格恒 `RateEmpty`。
- 词条：`Battle/AlliancePanel/{PlayerLabel,TitleLabel,AllianceLabel,NotInAnAlliance}` 4 条本地有值。

### 2·6 聊天 / 好友 / 收件箱（283 / 291 / 296 / 298 / 299 / 302 / 305 / 328 / 347 / 348 / 352 / 369 / 372 / 373 / 388 / 395 / 396 / 407 / 408 / 410 / 429 / 438）

- `ChatManager__GetChatSoundAsset.c:2-51`：`UpdateCurrentWarlords` → Intro 分支（同督军 + 非玩家 ⇒ `GetChatSound(1)`；否则 `GetCustomIntro` → `GetCustomIntroByArmy` → `GetChatSound(0)`）→ **命中条目只在 `customSound` 非空时 Load，否则静音不回退**（:33-37）→ 其它 index ⇒ `GetChatSound(index)`。
- `ChatMessageData__GetMessage.c:2-160` + `cpp2il_dev ChatMessageData.cs:138-256`：**只有 5 个模板类型**（20/30/60/80/90）+ `Announcement(100)`/`Message(50)` = `translated ?? msg` + `DeckShare(150)` = `Format(词条, playerName, deckName)` + **其余一律 `""`**（.cs:174/:193/:229）。
- `ChatMessageUI__Awake.c:2-33`：`portraitButton.onClick += ShowOptions`（:14-19）· `messageButton.onClick += OnMessageClicked`（:20-25）—— **两颗钮语义不同**。
- `ChatMessageUI__OnEnable.c:2-10` / `__UpdateTimer.c:2-49`：`OnEnable` = `if (message != null) UpdateTimer()`；`UpdateTimer` = `FromUnixTimeMilliseconds(ts)` → `UtcNow - t` → `GetTimeStringWithSingleDigit(span,true)` → 写 `timeText`。
- `ChatMessageUI__OnMessageClicked.c:2-61`：`type∈{50,100}` ⇒ `TranslateMessage()`；`type==150` ⇒ `Split("\n").Last()` → `DeserializeDeckString` → `DeckInfoContext` → `OpenWindow<DeckInfoPopup>`。
- `ChatMessageUI__SetMessageText.c:2-73`：`GetMessage()` + 三态 fontMaterial（0/1/2）+ 1 档拼「翻译中」尾缀。
- `ChatPlayerOptionsPanel__SetPositionInScreen.c:2-27`：**保留自己 x/z、只换 y**（`CONCAT44(param_2, x)`）。
- `ChatPreview__Show.c:2-34`：`SetActive(true)` + 三条 tween（`DOScale(icon,1,0.6).From(0).OutBack` / `DOFillAmount(container,1,0.4)` / `DOFade(container,1,0.2)`）。
- `ChatPreview__UpdatePreviews.c:2-98`：频道匹配 + `Count>=1` → `receivedMessages=1` → `TakeLast(previews.Count)` → 逐条 `ChatPreviewMessage.Set(playerName, GetMessage())`。
- `ChatPreviewMessage__Set.c:2-71`：`body.enabled = !IsNullOrEmpty(message)` + `<color=#{ToHtmlStringRGB(nameColor)}>` 富文本。
- `FriendUIElementWarpforge__Initialize.c:2-61` / `__UpdateStatusColor.c:2-54`：**同一段四档逻辑的两个入口**（2=Away 橙 · 3=Connected 绿 · 1/0 ⇒ 离线图不改色）。
- `FriendsData__IsFriend.c:2-29`：`friends.Any(f => f.playFabId == playerId)` —— 一句 LINQ。
- `FriendsTab__ToFocus.c:2-8`：`FillInFriendsContainer()`。
- `Inbox__RefreshHandler.c:2-45` → `Inbox._RefreshHandler_d__9__MoveNext.c:35,57`：`await handler.RefreshHandler(data)` + `Refresh<Badge>()`。
- `Inbox__ValidateData.c:2-52` → `Inbox._ValidateData_d__7__MoveNext.c`：类型守卫（:240-243）· **minVersion 闸**（`Version.TryParse` 失败 ⇒ LogError；`Application.version < min` ⇒ `List.Remove`，:244-295）· **maxVersion 同形**（:296-358）。
- `JoinAllianceMenu__FillGroupInvitations.c:2-37`：`loadingGuard` 早退 + 置 1 → 关 invitationsRoot → 清子 → `FetchGroupInvitations`。
- `JoinAllianceMenu__FillOpenAlliances.c:2-43`：`searchButton.interactable=false` → `query = Trim(text)` → `ShowLoading` → `GetGroupList(query, cb)`。
- `JoinAllianceMenu__Initialize.c:2-68`：**`!initialized` 才拉邀请**（:17-33）+ **无条件**重拉公开表（:41-61）。
- `MessageDisplay__OnPointerClick.c:2-7`：一句 `TextLinkProcessing.TryProcessText(body, eventData)`。
- `ProfileAllianceDisplay__Initialize.c:2-33`：有名 ⇒ 名字 + `playerRating.Initialize(rating, null, false, false, true)`；无名/ null ⇒ 整块 `SetActive(false)`。⚠️ 该方法零调用点，真跑的是 `ProfileNameTitleSection.cs:129-146` 内联同款（该块实读：`AllianceRatingDisplay.Initialize(Rating, null, disableIfZero:false, useSealSystem:false, zeroBasedRank:false)` ⇒ **零值也显示**）。

### 2·7 徽章模型三件（459 / 460 / 461）

- `cpp2il_dev Everguild.UI.Notifications/`：`BoosterPackBadge.cs:8` = `public record BoosterPackBadge : InventoryBadge`（**无字段**）· `OfferBadge.cs:10-31` = `[Serializable] public record OfferBadge : ShopBadge` + **`public string storeId` + `OfferBadge(string storeId)`** · `ChallengeMatch.cs:8` = `public record ChallengeMatch : Notification`（无字段）。
- 基类 `Notification.cs:8-19` = `[Serializable] public abstract record Notification` + **`public virtual bool Validate(Notification other)`**。本仓 `Shell/BadgeNotifications.cs:60` 是 `abstract class BadgeNotification`（12 个子类），**没有 Validate**。

---

## ③ 没查清（如实记，⛔ 别当成已对齐）

1. **`AllianceMemberOptionsPopup__Open.c` / `__Awake.c` 的 outrank 判据**（账 30 的三颗钮显隐）。
2. **`AllianceView__Start.c` 两个 delegate 的目标方法体**（`GoogleTranslation.onTranslationReady` 那一跳 / `descriptionText` 的 DropDelegate = 「换行规整」）—— 只见委托构造（`.c:107-140`），体未读。
3. **`UIRankingRow__Config.c`（父类）** 是否无条件写三颗文本（账 90② 的前提）。
4. **`SupportMethods__GetTimeStringWithSingleDigit.c`** 的时间格式与进位规则（账 305）。
5. **`TextLinkProcessing` 三份 `.c`** 的实现细节（账 429）。
6. **`JoinAllianceMenu` 的 Fetch 回调体**（账 407/408 的『空表关整颗』『`Members>0` / `Take(200)` / `Contains` 过滤』都在回调里）。
7. **`AlliancesController.InvalidateLocalGroup` 的调用点**（账 117）。
8. **`AllianceMemberEntry` 两组在线状态 Color 常量**（`_DAT_1834b3040` / `_DAT_1834b2c00` 组各 4 字节 float ×4）—— 未用 `read_literal` 现读数值。
9. **`ProfileNameTitleSection.cs`** 只读了 `:129-146` 那段核心块（有名/无名两支 + 实参组合）；该方法**其余部分未读**（改名钮显隐、等级文本等）（账 438）。
10. **`AllianceView__Draw.c` 两处 `AllianceRatingDisplay.Initialize` 的实参组合**（useSealSystem / zeroBasedRank 的具体 true/false）。

> 这 10 条都写进了施工单对应行的「还差什么 / 风险」列；派活时告诉写手「**先读这几处再动手**」，⛔ 别自己发明口径。

---

## ④ 改动清单（本代理动过的文件）

**代码 / 资产 / 主账表 / 正本：0 处改动。**

本轮只**新建 3 份产出**（均在 `Unity/资料/完全复刻/` 下）：

| 文件 | 说明 |
|---|---|
| `判据/B9_社交C_施工单.tsv` | 12 列 × 48 行；表头与 `B9_剩余C_施工单.tsv` **逐字相同**；LF、无 BOM；账行号已逐行核过（48/48 命中 `状态==C` 且类名一致） |
| `判据/B9_社交C_派活包.md` | 14 包切片 + 本地/远端两栏 + P3 候选 + 文件所有权表 + 波次 + 读数口白名单 |
| `报告/B9_J9SC_社交C施工单.md` | 本文件 |

另有一个**临时生成脚本**（不在仓库交付物内、可删）：`d:/4/_tmp_view/b9_j9sc_gen.py`（把 48 行写成 TSV；`\t` 分隔，无制表符/换行混入单元格）。

---

## ⑤ 🔴 顺手发现（只报不改；全部有出处）

### 5·1 主账 6 处说法已被现读推翻（⛔ 别再照着开工）

| # | 账行 | 旧说法（主账原文摘） | 现读事实 | 出处 |
|---|---|---|---|---|
| 1 | 63 / 65 | 「开箱图 5 张 40k_Crate_TierN_*_open **本仓 Resources/ 没有**（源在 boosterpacks 包）⇒ 实际只出声」 | **10 张全在**：`Resources/Art/ui_menu/40k_Crate_Tier1_Iron.png` … `Tier5_Warp_open.png`；`CardArt.MenuUi` 的取图根就是该目录 | `ls MyGame/Assets/CardPresentation/Resources/Art/ui_menu/ \| grep -i crate`（实列）· `Shell/AllianceEventScoreInfo.cs:70-72` 的名字逐字一致 · `Core/CardArt.cs:890-901`（`MenuUi` → `Resources/Art/ui_menu/`） |
| 2 | 291 / 302 | 「缺 **15 种**系统消息的本地化模板（Join/Leave/Promoted/Demoted/**InvitePlayer/Rally/ReplayShare/RewardShare**…）」 | 原版**只有 5 个模板类型**：Join(20)/Leave(30)/KickPlayer(60)/Promoted(80)/Demoted(90)；`InvitePlayer(70)`/`Rally(130)`/`ReplayShare(140)`/`RewardShare(160)`/`AskLeadership(40)`/`Practice*` **一律 `return ""`** | `cpp2il_dev ChatMessageData.cs:149-229`（ILSpy 分派）· `decomp_full/ChatMessageData__GetMessage.c:40-95` · 枚举 `Assembly-CSharp/ChatMessageType.cs:1-19` |
| 3 | 26③ | 「`IsPlayer → graphicsToChangeColorIfIsPlayer` 换色 —— 原版那组 `Color` 在序列化字段里，**本地读不到值**」 | **读得到**：`isPlayerEntryColor = (0.15258681774139404, 0.48235294222831726, 0.0, 1.0)`；三份实例同值；作用对象 = `background` + `Image` | `bundle_menus_assets_all/MonoBehaviour_-4586463415933622350.json:44-49`（另两份 `_-2068273713529560925.json` / `_4905926295992066211.json` 同值）· PPtr 反查 `GameObject/background_5687845496662894514.json` · `GameObject/Image_-2532075377729034318.json` |
| 4 | 88 / 73 | 「徽标图本地无 id→图 映射（**判据空**）」 | 要**分三层**：① **图在**（`bundle_alliancesbadgesimages_assets_all` 22 张：18 军团 + TIER1..4 + Default_v2；TIER1..4 已导入 `Resources/Art/ui_menu/`）② **映射不在**（`AllianceBadge` SO 全库 0 个）③ **Default 兜底在**（`AlliancesTrophyController.DefaultTrophy`）⇒ **Frame 可画、Badge 只有导入军团图后才能画** | `ls d:/2/新解包资源/assets_full/bundle_alliancesbadgesimages_assets_all/Texture2D/` · `decomp_full/AllianceBadgeDrawer__Draw.c:23-65` · `__SetTierFrame.c:43-64` · `Shell/AllianceTrophyArt.cs:1-70`（已在用 4 档） |
| 5 | 460 | 「OfferBadge — 9 个编译器样板方法，全为编译器样板」（G2 普查原文） | 它有**真实字段** `public string storeId` + **单参构造** `OfferBadge(string storeId)` | `cpp2il_dev Everguild.UI.Notifications/OfferBadge.cs:10-31` |
| 6 | 459 | （无旧说法，是本轮**新发现**）`BoosterPackBadge` 在本域与商店线**重复记账** | 商店线已把它连同 provider 一起排进施工单（落点 `Shell/Inventory.cs` + `Shell/BadgeNotifications.cs`）⇒ 跨域重复，必须只派一个写手 | `判据/B9_菜单商店C_施工单.tsv:103-105` vs `账/社交_聊天.tsv:459` |

### 5·2 方法论级（可复用，建议记进 `资料/工作准则_操作经验.md`）

1. **「枚举值个数 ≠ 实现分支个数」**：`ChatMessageType` 有 19 个值，但 `GetMessage()` 只对 **5 个**做模板、对其余**返回空串**。上一轮按「枚举有 19 个值 ⇒ 缺 15 个模板」记账，方向错了。⇒ 数「有几种情况」要**数分支**，别数枚举。
2. **「代码注释里写的缺口」也会过期**，而且比账更危险（账至少有轮次，注释没人回看）：本域 `AllianceEventScoreInfo.cs:67-72` 那句「本仓 Resources/ 里一张都没有」是**同一批人**写的、**图其实早就在** ⇒ 判「资源缺」时**先 `ls` 一次目录再引用注释**（同 `CLAUDE.md` 铁律 5「翻过一个镜像目录就写『本地没有』= 一定会错」）。
3. **PPtr 反查 prefab 里「哪两颗「节点」被换色/被画」是可行的、且很快**：`graphicsToChangeColorIfIsPlayer` 的 PathID 不是 GameObject 名，但 `grep` 一遍 `GameObject/*.json` 就能反查到归属（`background` / `Image`）。⇒ 以后遇到「原版这里改哪几颗」不必再写「本地读不到」。
4. **`AllianceBadge` 这种「SO 在远端」的结论可以有量化判据**：逐 bundle 扫 `MonoBehaviour/` 名单（本代理扫了全部 `bundle_*`），命中 0 ⇒ 才敢写「本地没有」；比「翻了两个目录没找到」硬得多。

### 5·3 代码侧（只报不改）

| # | 位置 | 现象 | 为什么值得记 |
|---|---|---|---|
| 1 | `Shell/AllianceSearchTab.cs:623` `RowButton(...)` | 不返回 `WindowButton`，调用方拿不到 `Interactable` | 账 14/21 都要设 `Interactable` ⇒ 要么改签名（4 个调用点）要么就地取组件；⛔ 别在 `RowButton` 外再建一颗钮 |
| 2 | `Shell/ChatPanel.cs` `ChatMessageRow.Build`（头像 Hit） | **两侧头像都挂了 `ShowOptionsFor`** | 原版 `portraitButton` 只属 `Friend Header`（别人的头像）⇒ 自己发的头像原版点不到；整行命中落地后**必须让头像优先**，否则会被行命中吞掉 |
| 3 | `Shell/SocialData.cs:148` `MemberCap = 20` | 是 prefab 出厂样例串 `Members: --/20` 的读数，**不是** `AlliancesConfig.maxMemberCount` | 账 14/21 的 `maxMemberCount` 与它不是同一个来源 ⇒ ⛔ 别互相顶替；已在代码注释里标了「prefab 样例值」 |
| 4 | `Shell/AllianceEventScoreInfo.cs:67-72` | 过期注释（见 5·1 #1） | 建议随 J-04 一并订正（带「⚠️ 2026-10-26 更正」痕迹） |
| 5 | `Shell/BadgeNotifications.cs:60` | `abstract class BadgeNotification`，**无 `Validate`** | 原版基类是 `abstract record Notification` + `virtual bool Validate(Notification)`（`cpp2il_dev Everguild.UI.Notifications/Notification.cs:19`）⇒ 账 461 若要对齐，等于给共用基类加虚方法（牵动 12 个子类）⇒ 建议**先只加类型**，`Validate` 单独立账 |
| 6 | `Editor/MainMenuScene.cs` | 有多条本域断言（联盟/好友/排行榜/奖章） | 账 128 改出厂态、账 407 改空态、账 70 改 Join 钮文案后，**断言要同步改**（是改期望，不是改实现） |
| 7 | `Shell/ProfileTab.cs`（`BuildInfoSectionWithAlliance`） | `Alliance Name` 恒占位串、评级恒 `------`，且整段 `sec.gameObject.SetActive(false)`（`ProfileTab.cs:725` 一带） | 原版**内联真跑版**是「有名就显示、零值也显示（`disableIfZero:false` 硬编码）」⇒ 记着这一处**不是**「原版就没有」，是我们「恒关」那一支；账 438 落地时别顺手把 `disableIfZero` 打开 |

---

## 附 · 调度台裁定、账补丁与断言改动（2026-10-26 追加，非第五节内容）

> 本节是**收口记录**：调度台对 §1·3 三条请求全部批准，并授权写 `_parts` 补丁 + 改 `Editor/MainMenuScene.cs` 相应断言。
> 纪律：⛔ 未改主表（只有补丁）；⛔ 未跑 `MainMenuScene.Run`（会存盘重写场景 —— 那是调度台的活）；只跑 `typecheck`。

### 附·1 三条裁定

| # | 裁定 | 落地 |
|---|---|---|
| 1 | 账 63/65 照实读改，但按**保守口径改 `B`**（不改 A），备注写「逻辑已逐句落地 + 图在 `Resources/Art/ui_menu/`（10 张，开/闭各 5）⇒ 复核后应升 A」 | 补丁已写（`状态 C→B`、`等级` 清空） |
| 2 | 账 459 去重：**类型归本域**（`Shell/BadgeNotifications.cs`）、**provider 归商店线**（`Shell/Inventory.cs`）⇒ 补丁只写去重说明、**不改状态** | 见附·3（这条**进不了自动补丁**，见附·3 的机械原因） |
| 3 | 账 128 出厂态照原版（`leaderBoardID` 空 ⇒ 两颗榜钮关），**授权改断言** | 已改 2 处期望串（3+/3−，**零行数变化**） |

### 附·2 账补丁（`账/_parts/B9_J9SC.tsv` · 7 行）

- **内容**：63·65（`状态`→B、`等级`→空、`差异`+`备注` 追加订正）· 291·302·26·73·88（只追加 `差异`/`备注`）。
- **改动列自检**：63/65 = `[5,7,8,10]`；其余 = `[7,10]` ⇒ 只落在授权列。
- **合并模拟（主表副本）**：`merge_ledger_parts.py --only B9_J9SC --parts <临时> --ledger <账副本>` ⇒ **更新 7 · 新增 0 · 无变化 0**；副本与主表逐行比 ⇒ **只 7 行变、行数不变（468）**、63/65 = `B` + 等级空。
- **三项自检（全部实跑）**：
  1. 每键唯一命中 —— `--dry` 报 **7 更新 / 0 新增 / 0 错误**；副本合并不报冲突。
  2. `verify_annot` —— 主表 **缺注解 0**；合并后的副本 **仍 0**（`同名 796 · 异名已注解 2783`）。
  3. `check_patch_conflicts.py` —— **无跨补丁重复键（当前 1 份补丁）**。
- ⚠️ **合并顺序**：一次 `--only B9_J9SC` 即可；⛔ 别用 `--all`。

### 附·3 🔴 459/460 **进不了自动补丁**（机械原因，需人工）

- 判据：`工具/merge_ledger_parts.py` 的 `looks_like_one_method()` 第一句就是
  `if "（" in name or "）" in name: return False`（「含全角括号（注记不是方法签名）」）。
  而主表这两行的 `方法` = `（数据模型 record：9 个编译器样板方法）` ⇒ 补丁行会被**整批拒收**（`errors` ⇒ 不写盘）。
- 实证：把 9 行（含 459/460）放进临时目录跑 `--dry` ⇒ 报
  `B9_J9SC.tsv:9 / :10：方法 不合规（含全角括号）` 两条，其它 7 行通过。
- 处置（两条路，任选；已写在 `账/_parts/B9_J9SC_需人工_459_460.md` 里，含**完整 11 列可粘**）：
  ① 调度台先把主表这两行 `方法` 改成合规签名（例 `record BoosterPackBadge` / `record OfferBadge(string storeId)`），再出补丁（键会变，需同步）；
  ② 调度台直接手工把该 `.md` 里那两行的 `差异`/`备注` 追加进主表。

### 附·4 `Editor/MainMenuScene.cs` 断言改动（账 128）

- 两处 `DirectKids` 期望串加 `*`（`*` = 自己 `activeSelf == false`）：
  `"Alliance Name|*View Leaderboard Button|Alliance Event Score Info"` · `"Background|Join Alliance text|Join Alliances Button|*Leaderboard Button"`。
- 🔴 **零行数变化**（`git diff --numstat` = **3 增 / 3 删**）：新说明**折进已有行**（行尾注释 + 断言文案），因为
  `账/菜单_外壳.tsv:816` 引用了本文件的 `:17410` —— 插行会把那条推成漂移（CLAUDE.md「新成员一律追加到类尾」同族纪律）。
  改后 `check_ledger_points.py 菜单_外壳.tsv --all-A` 里**没有** 17410 的漂移（19 条漂移全是既有的、与本次无关）。
- `typecheck`：**运行时 0 / 编辑器 0**。
- ⚠️ **J-05 落地前这两条会红（预期中的红）**：期望值 = 账 128 目标态，而 `Shell/AllianceEventScorePanel.cs` 的两钮**今天仍恒显**。
  请调度台**先派 J-05** 再跑 `MainMenuScene.Run`，否则会看到 2 条预期红（不是回归）。

### 附·5 收口时又抓到 4 条（只报不改）

1. 🔴 **账里 39 行「备注已明说『退 B』，`状态` 列却仍是 C」**（现读统计：`社交_聊天.tsv` 里同时满足「备注含『退 B』」+「状态 == C」的行 = **39**，即本域 48 条减去 9 条无此备注的）。这说明上一轮「调度台裁定退 B」只写进了备注、**没落 `状态` 列** ⇒ 「48 条 C」的真实构成可能是「**39 条已裁定退 B 未落列 + 9 条真 C**」。⛔ 我无权批量改（只授权 63/65），请调度台决定是否一次性把 39 行 `状态` 落成 B（那之后本域 C 只剩 9 条）。
2. ✅ **`Editor/MainMenuScene.cs:16235-16240` 早就在断言开箱图存在**：`Check(si.MissingArt.Count, 0, "★ 第 1 档已达标 ⇒ 开箱图**取到了**…")` + `Check(ch0Tex, "40k_Crate_Tier1_Iron_open", …)`，且注释写着「A808：这批手拷图…」⇒ **本轮 §5·1 #1 的结论有仓内独立佐证**（不是只有我的 `ls`）。
3. `check_ledger_points.py` 在本域**既有 6 条漂移**（`社交_聊天.tsv`：`ChatManager.UpdateCurrentWarlords` 10069→10143、`ChatPreview.Initialize/OnBeforeInitialization/OpenChat` 等 4 条 `MainMenuRuntime.cs` 行号）⇒ `--fix` 是调度台的活（须在无 `.cs` 写手 + 补丁合并完之后跑）。
4. `_parts/` 里另有两份**生成器脚本** `_gen_B9_E9ER.py` / `_gen_B9_E10TB.py`（非 `.tsv`，merge 不会捡），但说明**别的代理也用「脚本生成补丁」这条路**；建议统一约定：生成器留在 `_tmp_view/`，`_parts/` 只放 `.tsv`（避免以后有人误跑）。
