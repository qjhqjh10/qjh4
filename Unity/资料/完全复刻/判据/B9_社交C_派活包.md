# B9 · 社交/聊天域 48 条 C 行 · 派活包（按目标文件切片）

> 判据代理 **J9SC**（第八十四会话「完全复刻」）· 2026-10-26 · **只读核**
> ⛔ 本轮**未改**任何 `.cs` / 资产 / 主账表 / 正本；⛔ 未跑 Unity；⛔ 未动 git。
> 配套：逐条施工单 = `判据/B9_社交C_施工单.tsv`（12 列 × 48 行，表头与 `B9_剩余C_施工单.tsv` 逐字相同）· 总表与判读 = `报告/B9_J9SC_社交C施工单.md`
> 判据输入（**未重做**，只补证据）：`账/社交_聊天.tsv`（`状态==C` = **48 条**，逐行核对 missing 0 / extra 0 / 行号↔类名一致）· `资料/普查产出_0927/社交_联盟与好友页.md` · `资料/普查产出_0927/多人界面_入口与调用.md` · `普查产出_第八十会话/G2_未进账类全量.tsv` · `判据/B9_菜单商店C_施工单.tsv:103-105`（跨域重复项）

---

## §0 用法（四句话）

1. **一个包 = 一个目标文件（或一组同族小文件）= 一个写手**；包内按「建议等级 P0 → P1 → P2」顺序做，做完一条在施工单里勾掉。
2. 🔴 **同一时刻一个文件只能一个写手**。本轮 48 条压在 **6 个瓶颈文件**上（见 §5），跨包撞车一律**串行**，别并行。
3. 每包做完先跑 **`bash 工具/typecheck.sh`**（几秒级，改过 `.cs` 就必须跑），再按「本包验收口」读数；⛔ 写手**不跑 Unity 批处理**、⛔ **不跑全量自检**（主对话在同步点统一跑）。
4. ⛔ **禁用的读数口**：任何截图并排比、任何像素结论（用户 2026-10-26 口径：画面像不像由用户自己看）。本文件的「怎么验」全部是**断言 / 反编译读数 / `menu_dump` 字段读数**。

### 0·1 「现等级 / 建议等级」两列的口径

- **现等级** = 主账 `账/社交_聊天.tsv` 的 `等级` 列**现值**（本域 48 条只有 `P1` / `P2` 两档）。它是**优先级**，不是 A/B/C/D 状态。
- **建议等级** = 本代理按 `CLAUDE.md §四` 的优先级总纲重判的**优先级**：
  · **P0** = 主场域（对战/卡组/卡牌/收藏）—— 本域**只有 1 条**（账 187 战斗内联盟窗）
  · **P1** = 音频/本地化/菜单外壳 —— 本域 **1 条**（账 283 开场白语音）
  · **P2** = **社交/联盟/聊天/排行榜/收件箱** —— 本域 **44 条**（含现等级 P1 的那 6 条：**社交域按总纲是 P2**，但它们多半是玩家一眼可见的行/空态，调度台可以保留 P1）
  · **P3** = 服务器/账户/云后端 —— 本域 **2 条**（账 395/396，⛔ **不判 D**，见 §4）
- 🔴 **状态（A/B/C/D）的改判建议**写在施工单「施工步骤」列首的方括号标签里（本域实际用到 12 种）：
  `【可派】`（28）· `【可派·极小】`（7）· `【可派·需前置】`（3）· `【可派·小】`（2）· `【可派·前置】` · `【可派·只补口】` · `【可派·三行以内】` · `【可派·拆两半】` · `【可派·需去重】` · `【可派·低优先】` · `【可派·P0 域】` · `【远端·只给证据与时间限定】`。
  `建议等级` 列**只写 P0/P1/P2/P3**（不写注释；注释在「风险」列）。`C→A` 的复核建议写在「风险」列（账 63/65）。
  ⚠️ 本代理**不改主表**；所有 `C→A` / `C→B` 建议都由调度台裁。

### 0·2 本轮最重要的四个结论（先看这个）

> 🔴 **2026-10-26 调度台已裁定（本包三处按此执行）**：① 账 63/65 照实读改，但**保守改 `B`**（备注写「复核后应升 A」）—— 账补丁已写；② 账 459 去重：**类型归本域**（`Shell/BadgeNotifications.cs`）、**provider 归商店线**（`Shell/Inventory.cs`）；③ 账 128 断言已在 `Editor/MainMenuScene.cs` 改好（两处 `DirectKids` 加 `*`，**零行数变化**）⇒ ⚠️ **J-05 落地前跑 `MainMenuScene.Run` 会看到 2 条预期红**，请先派 J-05。

1. 🔴 **本域一半以上是「机制本地可做、数据远端」**（48 条里 **28 纯本地 / 18 机制本地+数据远端 / 2 纯远端入口**，见 §3）。派活时**别把「数据在远端」当成「不做」**——UI 机制全部可照原版落地，喂 `SocialData` 就能验。
2. 🔴 **账 63/65 的 C 级理由（「开箱图本仓 Resources 没有」）不成立**：10 张箱子图（开/闭各 5）**都在** `Resources/Art/ui_menu/`。这两行逻辑已逐句落地 ⇒ 复核实图可取后应改 **A**。
3. 🔴 **账 291/302 的「15 种系统消息模板」是口径错误**：原版只有 **5 个模板类型**（Join/Leave/KickPlayer/Promoted/Demoted）+ DeckShare，其余类型（InvitePlayer/Rally/ReplayShare/RewardShare…）**返回空串**；而这 **6 条词条本地官方表全都有** ⇒ 这两行可**完整落地**，不是「缺 15 个模板」。
4. 🔴 **账 26③ 的「isPlayerEntryColor 本地读不到值」被推翻**：prefab 序列化字段实读 `(0.15258682, 0.48235294, 0.0, 1.0)`，作用对象 = `background` + `Image` 两颗（PPtr 反查）⇒ 可落地。

---

## §1 包总表

| 包名 | 文件 | 条数 | 写手独占? | 前置依赖 | 账行号（施工单） | 本包验收口（摘要） |
|---|---|---|---|---|---|---|
| **J-01** | `Shell/SocialData.cs` | **4 主 + 为 5 个包提供字段** | ✅ **独占 · 全场第一个做** | 无 | 117·123·291·373（+ 供 21/26/30/298/302/305/352/369/438） | 新字段全部**只加不删**；`IsFriend` 收口成一份；`InvalidateLocalGroup`/`SetCurrentGroupCached` 成对 |
| **J-02** | `Shell/AllianceSearchTab.cs` | **9** | ✅ 独占 | **J-01**（`AllianceListing.Privacy`） | 14·21·68·70·95·357·407·408·410 | 行内 `Join` interactable 三态；隐私文案；空表关整颗 Invitations；`_onCreateTab` 记忆 |
| **J-03** | `Shell/AllianceMemberTab.cs` + `Shell/AlliancesTab.cs` + `Shell/WindowTabBase.cs`（只读） | **10** | ✅ 独占（本域最大） | **J-01**（`Member.Role/Id`）· **J-02**（ToFocus 链的另一半，但**虚拟调用互不阻塞**） | 26·30·53·60·73·88·93·97·100·101 | 成员行 IsPlayer 换色 / role 走枚举；`SetEditing(bool)` 九句两向；奖杯 Frame = TIER{n}；Edit 钮显隐 |
| **J-04** | `Shell/AllianceEventScoreInfo.cs` | **2** | ❌ 任意（最小件） | 无 | 63·65 | `Chests[i].texture` 含 `_open`；alpha 0.5 压暗；LogError 支路 |
| **J-05** | `Shell/AllianceEventScorePanel.cs` | **1** | ❌ 任意 | 无 | 128 | `SetInAlliance(true, null)` ⇒ 两颗榜钮关；（非空）⇒ 开且 id 正确 |
| **J-06** | `Shell/LeaderboardRow.cs` | **1** | ✅ 独占（共用行模板） | **须先读** `UIRankingRow__Config.c` | 90 | 联盟行三颗文本「空串也建节点」；**玩家族逐位不变**对照断言 |
| **J-07** | `Shell/AlliancePanelWindow.cs` + `Battle/BattleDriver.cs`（调用点 :5867） | **1** | ✅ 独占（P0 域） | **J-01**（无硬依赖，但同批改 BattleDriver 时要让位） | 187 | 出场 alpha/scale 插值；称号/联盟名两支显隐；playerScore 传进评级格 |
| **J-08** | `Battle/BattleDriver.cs`（SemkIntro）+ `Battle/UnitChatPanel.cs` | **1** | ✅ **与 J-07 同一写手** | 无 | 283 | 「命中条目但 SoundGuid 空 ⇒ 静音」；无条目才走 intro/vs~ |
| **J-09** | `Shell/ChatPanel.cs`（含 `ChatMessageRow`） | **6** | ✅ 独占 | **J-01**（ChatMessage 新字段） | 296·298·299·302·305·328 | 头像只别人可点 / 整行走 299；选项面板跟行 y；时间现算；模板正文 |
| **J-10** | `Shell/MainMenuRuntime.cs` | **3** | ❌ 任意（本文件巨大，注意别碰别处） | **J-09**（352 与 302 同一套文本）· **J-01** | 347·348·352 | 两行预览按 `TakeLast(2)` 填充；空 message ⇒ `enabled=false`；有消息才显 |
| **J-11** | `Shell/FriendsTab.cs` | **3** | ✅ 独占 | **J-01**（`Friend.Status`）· **J-01**（`IsFriend`） | 369·372·388 | 四档状态色/图逐值；`ToFocus` 重画；一趟 `ApplyStatus` 两处共用 |
| **J-12** | `Shell/InboxData.cs` + `Shell/InboxWindow.cs` + **新** `Core/TextLinkProcessing.cs` | **3** | ✅ 独占 | 无（395/396 体量最小） | 395·396·429 | 版本闸纯函数（3 条样例只剩 1）；链接点击命中/不命中 |
| **J-13** | `Shell/ProfileTab.cs` | **1** | ❌ 任意 | **须先读** `ProfileNameTitleSection.cs:127-146` | 438 | 有名 ⇒ 段开且名字对；无名 ⇒ 段关 |
| **J-14** | `Shell/BadgeNotifications.cs` | **3** | ✅ 独占 · **与商店线去重** | 无 | 459·460·461 | 三个类型 + `OfferBadge.StoreId`；徽章族计数 +3 |

> 合计 **4+9+10+2+1+1+1+1+6+3+3+3+1+3 = 48**。

---

## §2 各包明细（要点 + 坑，逐条细节见施工单）

### J-01 `Shell/SocialData.cs` · 4 条主 · ✅ 独占 · **全场第一个做**
- **为什么第一个**：本域 48 条里有 **6 个包**要往 `SocialData` 加字段（`ChatMessage.Type/TargetPlayer/LocData/TimestampMs` · `Member.Role(int)/Id` · `Friend.Status` · `AllianceListing.Privacy` · `CachedAt`）。**数据件只有一个写手**，否则后写的静默盖掉先写的。
- 🔴 **只加不删**：`ChatMessage.Time`（预置串）保留 —— 账 305 是「有 `TimestampMs` 才现算、否则用预置串」，删了会把现有自检夹具打光。
- 账 373 的 `IsFriend` **必须收口**：`ChatPanel.IsFriend` 转调 `SocialData.IsFriend`（两处写同一规则 = 迟早不一致）。
- 账 117/123 的 `InvalidateLocalGroup` / `SetCurrentGroupCached` 成对做，且**调用点先查**（本代理未查，见 §8）。

### J-02 `Shell/AllianceSearchTab.cs` · 9 条 · ✅ 独占
- 三行（14/21）共用一段 badge 结论（见 §9）；21 要 `AllianceListing.Privacy`（J-01 先给）。
- 三行（68/70/407/408/410）是**同一个状态机**：`_onCreateTab` 记忆 + 两表重建 + 空表关整颗 Invitations + loading 守卫。**必须同批做完**（分开做会出现「记忆对了但文案被抹」这类半成品）。
- ⚠️ `RowButton(...)` 目前**不返回** `WindowButton`（`Shell/AllianceSearchTab.cs:623`）——要设 `Interactable` 得改签名或自己取组件；同文件 4 个调用点，一次改完。
- ⚠️ 原版 `ShowCreateAllianceMenu` **不碰** `joinButtonText`：现在 `ShowCreate()` 里那句 `SetJoinLabel(LabelJoin)` 就是偏离来源，删掉即可（账 70）。

### J-03 `Shell/AllianceMemberTab.cs` · 10 条 · ✅ 独占（本域最大）
- 依赖顺序：**账 26（Member.Role→int）必须与账 30 同批**（`OpenMemberOptions` 与 `RoleText` 是同一份数据的两端）。
- **账 97 依赖账 100**（`SetEditing(true/false)` 是同一状态机的两个方向）。
- 账 73/88 是同一徽标族（Frame 可画、Badge 无映射，见 §9）。
- ⚠️ `AllianceGeneralDetails.Build` 是**两棵树共用的 builder**（`Variant.Member` / `Variant.Search`）⇒ 只改 Member 支，Search 支必须有一条「逐位不变」对照断言。
- ⚠️ `AlliancesTab.cs` 的 `ToFocus` 转调**只有本包能写**（J-02 只在自己类里加 override，不改 AlliancesTab）。

### J-04 `Shell/AllianceEventScoreInfo.cs` · 2 条 · ❌ 任意（最小件）
- 只做两件事：**订正那段「本仓没有开箱图」的注释**（铁律 5）+ 一条取图/三态断言。逻辑本身 E4 已逐句落地。
- 🥇 结论：这两行**实质已可判 A**（逻辑 + 资源都齐）—— 交调度台复核。

### J-05 `Shell/AllianceEventScorePanel.cs` · 1 条 · ❌ 任意
- 出厂态由「两颗榜钮恒显」改为「`leaderboardId` 空 ⇒ 关」（照原版）⇒ **会改观感**，先 `grep MainMenuScene.cs` 里对这两颗钮的断言。

### J-06 `Shell/LeaderboardRow.cs` · 1 条 · ✅ 独占（共用行模板）
- ⚠️ 这是**玩家族与联盟族共用**的行模板。`base.Config` 那句「无条件写三颗文本」**必须先读 `decomp_full/UIRankingRow__Config.c`** 才能动（父类语义本代理未读）。
- 必须配「玩家族渲染逐位不变」的对照断言（灭自证）。

### J-07 `Shell/AlliancePanelWindow.cs` + `Battle/BattleDriver.cs:5867` · 1 条 · ✅ 独占 · **P0**
- 本域唯一 P0：这扇窗在战斗 HUD 里，属「对战界面」。
- 出场动画本仓无 DOTween ⇒ 帧循环插值；**三个常量先用 `工具/read_literal.py` 现读**（⛔ 别照抄注释）。
- 头像立绘的坑同 `ChatPanel` 的 `Profile border`：**框心是不透明黑**，立绘队列必须比 Border 高 1。

### J-08 `Battle/BattleDriver.cs`（SpeakIntro）+ `Battle/UnitChatPanel.cs` · 1 条 · ✅ 与 J-07 同一写手
- 只改一处语义：**命中条目但 `SoundGuid` 空 ⇒ 静音、不回退**（原版 `.c:33-37`）。
- `vs~`/阵营词那两跳是我们补的投影 ⇒ 按铁律 18 打 `🔶 自己设计` 标记，并挪到「无条目」那一支之后。

### J-09 `Shell/ChatPanel.cs` · 6 条 · ✅ 独占
- **一组按钮的三跳**（296 命中 → 299 语义 → 328 定位）+ 正文/时间两条（302/305）+ 重算时机（298）。六条同批。
- 🔴 **最大坑**：整行命中会**覆盖**头像命中区 ⇒ 必须保证「点头像」优先，否则「点头像开选项面板」退化成「点行翻译」。
- 账 299 的 DeckShare 要动 `Shell/DeckInfoPopup.cs`（卡组线共用件）⇒ **与卡组线排期**。

### J-10 `Shell/MainMenuRuntime.cs` · 3 条 · ❌ 任意
- 与 `A1639`（出厂预览关上）必须同时成立：**有消息才显**。
- 依赖 J-09 的文本拼装口径（账 352 与 302 是同一套富文本）。

### J-11 `Shell/FriendsTab.cs` · 3 条 · ✅ 独占
- 369/372 共用一份 `ApplyStatus(Transform,int)`（⛔ 两处各写一份）；388 的 `ToFocus` 是派发链最后一块（父类空实现已核 `Shell/WindowTabBase.cs:82`）。
- ⚠️ 四个状态色值**施工前用 prefab 序列化字段复核**（ledger 给的数值不要直接抄）。

### J-12 `Shell/InboxData.cs` + `Shell/InboxWindow.cs` + **新** `Core/TextLinkProcessing.cs` · 3 条 · ✅ 独占
- 395/396 是 P3（远端载荷入口）⇒ 只做「不静默」的桩；**396 的版本闸是纯本地函数，可做**（拆两半）。
- 429 要先读 `TextLinkProcessing` 三份 `.c`；本仓 **tooltip 线已有 `TMP_TextUtilities.FindIntersectingLink` 的先例** ⇒ **先查再复用，别写第二份**。

### J-13 `Shell/ProfileTab.cs` · 1 条 · ❌ 任意
- ⚠️ `ProfileAllianceDisplay.Initialize` **全库零调用点**；真跑的是 `ProfileNameTitleSection.cs:127-146` 的**内联同款** ⇒ **按那一份施工**（本代理未读全文，见 §8）。

### J-14 `Shell/BadgeNotifications.cs` · 3 条 · ✅ 独占 · **与商店线去重**
- 🔴 `BoosterPackBadge` 在 `判据/B9_菜单商店C_施工单.tsv:103-105` 里**已经排过**（连同 provider 一起，落点 `Shell/Inventory.cs` + `Shell/BadgeNotifications.cs`）⇒ **只派一个写手**，别两线各建一份。
- `OfferBadge` 有真实字段 `storeId` + 单参构造（G2 那句「全为编译器样板」对本类不成立）。
- 原版 `Notification` 是 `abstract record` 带 `virtual bool Validate(Notification)` —— 本仓 `BadgeNotification` 没有等价口；**先只加类型**，`Validate` 单独列账（动共用基类会牵动 12 个既有子类）。

---

## §3 「本地可做」vs「需要远端数据」两栏（**派活前必看**）

### 3·1 纯本地可做（28 条）—— 判据 + 数据都在本地，可直接做到 A

| 账行号 | 原版类.方法（简） | 本地凭什么能做 |
|---|---|---|
| 63 | AllianceScoreBar.SetScoringMilestones | 10 张箱子图在 `Resources/Art/ui_menu/`；逻辑已落地 |
| 65 | AllianceScoreBarLine.ShowChestOpen | 同上（一句换图） |
| 68 | AllianceSearchTab.Initialize | 页签记忆是本地布尔 |
| 70 | AllianceSearchTab.ShowCreateAllianceMenu | 一句 `SetJoinLabel` 的删改 |
| 97 | AllianceView.OnEditCancel | 九个节点的显隐，节点全已建 |
| 100 | AllianceView.OnToggleEditClicked | 同上（`GroupPrivacy` 本地枚举；只有 language 那一格缺源） |
| 117 | AlliancesController.InvalidateLocalGroup | 纯内存三字段清零 |
| 123 | AlliancesController.SetCurrentGroupCached | 纯内存写 + 去重 |
| 128 | AlliancesEventScorePanel.Initialize | `leaderBoardID` 是本地字符串分支 |
| 283 | ChatManager.GetChatSoundAsset | 197 条目 + `CardChatIntro` 全在本地 |
| 291 | ChatMessageData.GetMessage | 5 模板 + DeckShare，6 条词条本地官方表全有 |
| 296 | ChatMessageUI.Awake | 两颗钮的挂法 |
| 298 | ChatMessageUI.OnEnable | 重画即重算（等价口） |
| 299 | ChatMessageUI.OnMessageClicked | `DeckInfoPopup` + 卡组串解析都在本地 |
| 302 | ChatMessageUI.SetMessageText | 同上（三态材质缺源，但结构可落） |
| 305 | ChatMessageUI.UpdateTimer | 加 `TimestampMs` + 现读原版时间格式函数 |
| 328 | ChatPlayerOptionsPanel.SetPositionInScreen | 画布 px 平移 |
| 347 | ChatPreview.Show | 三条 tween 改帧插值 |
| 348 | ChatPreview.UpdatePreviews | `TakeLast(2)` |
| 352 | ChatPreviewMessage.Set | 一句富文本拼装（颜色已实读 #00FF20） |
| 369 | FriendUIElementWarpforge.Initialize | 四档常量在 prefab 序列化字段 |
| 372 | FriendUIElementWarpforge.UpdateStatusColor | 同上（共用一份 ApplyStatus） |
| 373 | FriendsData.IsFriend | `Friend.Id` 已有 |
| 388 | FriendsTab.ToFocus | 一句重画 |
| 429 | MessageDisplay.OnPointerClick | 三份 `TextLinkProcessing.c` 在本地 |
| 459 | BoosterPackBadge | 类型壳（无字段） |
| 460 | OfferBadge | 类型 + `storeId` |
| 461 | ChallengeMatch | 类型壳 + 基类 `Notification.cs` 可读 |

### 3·2 机制本地可做、**真实数据需远端**（18 条）—— 现在就能照原版落地，喂本地表即可验；数据到了自动生效

| 账行号 | 原版类.方法（简） | 本地能做的部分 | 卡在远端的部分 |
|---|---|---|---|
| 14 | AllianceInvitationEntry.Initialize | 行内容 / Join interactable / 重排 | `AlliancesConfig.maxMemberCount`（配置资产本地不存在）、徽标 id→图 |
| 21 | AllianceListEntry.Initialize | 同上 + 隐私文案 | 同上 |
| 26 | AllianceMemberEntry.Initialize | role 词条 / IsPlayer 换色（色已实读）/ 在线点 | 两处评级的 `group.currentSeason` / `TimedDraftEvent` |
| 30 | AllianceMemberList.HandleMemberClick | 摆位 / Role int / IsFriend | 成员真实角色 |
| 53 | AllianceMemberTab.Initialize | 页签记忆 + ToFocus 重画 | `GetCurrentGroup` 重拉 |
| 60 | AllianceRatingDisplay.Initialize | disableIfZero（已落）+ overrideIcon 口 | 段位换算（`LiveOpsManager.GetHandler<GameModes>`） |
| 73 | AllianceTrophiesView.Draw | Frame = TIER{n}；三条词条（已落） | 军团徽标 id→图；`FeatureConfig.IsArmyHidden` 配置 |
| 88 | AllianceTrophyEntry.Initialize | 进度/标题/计数（已落）+ Frame | 徽标 id→图 |
| 90 | AllianceUIRankingRow.Config | 三颗文本无条件建 | 联盟徽标 id→图 |
| 93 | AllianceView.Draw | 成员排序（已落）/ Edit 钮显隐 | 盟名外的全部组数据 |
| 95 | AllianceView.Initialize | 「先关再开」+ 本地组视图 | `GetGroupById` |
| 101 | AllianceView.Start | 隐私下拉 2 项 + 三颗钮监听 | `GameStaticData.GroupLanguages`（静态表） |
| 187 | BattleAlliancePanel.Open | 出场动画 / 头像 / 两支显隐 / 把分数接进评级格 | 徽标、评级图标（`LiveOpsAssetUtility`）、联盟名/分 |
| 357 | CreateAllianceMenu.Initialize | 隐私下拉 | `GroupLanguages`、`AlliancesConfig.createAlliancePrice` |
| 407 | JoinAllianceMenu.FillGroupInvitations | 空表关整颗 + 守卫 + `Members>0` | 邀请列表本体 |
| 408 | JoinAllianceMenu.FillOpenAlliances | 同上 + `Take(200)` + 钮 interactable | 公开联盟列表、搜索串（输入件未建） |
| 410 | JoinAllianceMenu.Initialize | 两表重建次序 + 『邀请只拉一次』 | 同上 |
| 438 | ProfileAllianceDisplay.Initialize | 有名/无名两支 | 联盟名与评级 |

### 3·3 只有远端入口（2 条，P3）

| 账行号 | 原版类.方法 | 为什么是远端 |
|---|---|---|
| 395 | Inbox.RefreshHandler(LiveopsHandlerData) | 唯一语义 = `await handler.RefreshHandler(payload)` + `Refresh<Badge>()`；本地**没有任何 LiveOps 载荷**（本地消息表是编译期常量） |
| 396 | Inbox.ValidateData(LiveopsHandlerData) | 同上；⚠️ **但版本闸那一段是纯本地函数** ⇒ 拆出来可做（施工单已写 `【可派·拆两半】`） |

---

## §4 P3 / 远端候选（**只给证据 + 时间限定，⛔ 不判 D**）

> 口径：`CLAUDE.md §四`——P3 候选**由调度台按 2026-10-26 授权自判**，但必须在账里写全证据 + 时间限定；⛔ 本代理不判 D。
> 另：本域**不属于**「不许判 D 的四类」里的任何一类（不是我们自己的 P2P、不是本地存档、不是引擎、这 2 条的 UI 部分另有对等物 `Shell/InboxWindow.cs`）。

| 账行号 | 断点证据（代码行） | 我们的替代物 / 覆盖说明 | 时间限定 |
|---|---|---|---|
| 395 | `Everguild.LiveOps.Inbox__RefreshHandler.c:29-43`（`AsyncTaskMethodBuilder.Start<Inbox_<RefreshHandler>d__9>`）→ `Inbox._RefreshHandler_d__9__MoveNext.c:35`（`await LiveOpsHandler<…>.RefreshHandler(data)`）、`:57`（`UiBadgeNotificationManager.Refresh<…>()`） | `Shell/InboxData.cs` 的**编译期消息表** + `Shell/InboxWindow.cs` 的正文/选中/领取；U 部分已照原版建 | 等远端 LiveOps 通道（与 `资料/完全复刻/判据/服务器与账户_不复刻清单_第2轮.md` 同批复核）。**到期再判 D** |
| 396 | `Everguild.LiveOps.Inbox__ValidateData.c:38-50` → `_ValidateData_d__7__MoveNext.c:77`（`await handler.ValidateData(data)`）+ `:203-205`（`new Version(Application.version)`）+ `:244-358`（min/max 版本闸，越界 `List.Remove`） | 同上；**版本闸可先做成本地纯函数** `InboxData.FilterByVersion(...)`（判据完整、不需载荷） | 同上；纯函数那一半**现在就做** |

---

## §5 共享瓶颈与「文件所有权」表（派活前必须照这张表排）

| 文件 | 归属包 | 谁能写 | 冲突点（为什么是瓶颈） |
|---|---|---|---|
| `Shell/SocialData.cs` | **J-01** | **只有 J-01** | 6 个包要加字段（ChatMessage×4 / Member×2 / Friend×1 / AllianceListing×1 / CachedAt）；**它是最强瓶颈**，必须第一个做完 |
| `Shell/AllianceMemberTab.cs`（1964 行） | **J-03** | 只有 J-03 | 10 条；`AllianceGeneralDetails.Build` 两棵树共用 |
| `Shell/AllianceSearchTab.cs`（766 行） | **J-02** | 只有 J-02 | 9 条同一状态机 |
| `Shell/ChatPanel.cs`（888 行，含 `ChatMessageRow`） | **J-09** | 只有 J-09 | 6 条同族（行模板 + 命中 + 时间 + 文本） |
| `Battle/BattleDriver.cs` | **J-07 + J-08（同一写手）** | 一个写手串行 | 战斗 P0 共用大件；J-07 改调用点 :5867、J-08 改 SpeakIntro |
| `Shell/BadgeNotifications.cs` | **J-14** | 只有 J-14 | 与商店线（`B9_菜单商店C_施工单.tsv:103-105`）**跨域重复 `BoosterPackBadge`** ⇒ 只能一个写手 |
| `Shell/AlliancesTab.cs` | **J-03** | 只有 J-03 | 53/68 都要 `ToFocus` 转调，归一个写手一次写完 |
| `Shell/DeckInfoPopup.cs` | 卡组线 | 卡组线写手 | 账 299 的 DeckShare 要在这里接 Import 态 |
| `Shell/InboxWindow.cs` | **J-12** | 只有 J-12 | 账 429 与收件箱线的建窗逻辑同文件 |
| `Shell/LeaderboardRow.cs` | **J-06** | 只有 J-06 | 玩家族与联盟族共用行模板 |
| `Shell/MainMenuRuntime.cs`（3623 行） | **J-10** | 只有 J-10 | 巨大文件，改动只许落在 `BuildChatPreview` / `SetChatPreviewVisible` 附近 |

---

## §6 并发波次建议（并发上限 10，别贴满）

| 波次 | 包 | 备注 |
|---|---|---|
| **第 0 波（1 路）** | **J-01** | 数据件先落地；这一波只 1 路（后 6 个包等它的字段） |
| **第 1 波（5 路）** | J-04 · J-05 · J-06（先读父类）· J-12 · J-14 | 全部与 J-01 无强依赖、文件互不相交；J-14 先与商店线确认写手 |
| **第 2 波（4 路）** | J-02 · J-03 · J-09 · J-11 | 四个大件，文件互不相交（`AlliancesTab.cs` 归 J-03）；J-13 可插在任意空闲路 |
| **第 3 波（2 路）** | J-07 + J-08（同一写手串行）· J-10 | J-10 依赖 J-09 的文本口径 |
| 不建议并行 | J-07/J-08 与任何动 `BattleDriver.cs` 的战斗线任务 | 同一文件只能一个写手 |

---

## §7 本域「怎么验」的合法读数口（白名单）

1. `bash 工具/typecheck.sh` —— 改完任何 `.cs` 立刻跑（几秒级，唯一挡「编不过」的东西）。
2. `Unity -executeMethod MainMenuScene.Run`（社交窗 / 聊天窗 / 收件箱 / 好友 / 联盟 / 排行榜 / 头像页**都挂在主菜单宿主的树上**）—— **由主对话在同步点统一跑**，写手不跑。
3. `Unity -executeMethod ShellScene.Run`（外壳窗/页签派发链，账 53/68/388 的 ToFocus 链在这一档更贴近）。
4. 断言：`CardPresentation/Editor/MainMenuScene.cs`（本域既有断言最多的一份）；改观感/出厂态（如账 128 两钮、账 407 空态）时**同步改断言，不是改实现**。
5. 反编译/资源读数：`工具/read_literal.py`（色/时长常量）· `python 工具/menu_dump.py bundle_menus_assets_all "<prefab 名>" --depth N --md`（字号/对齐/文本）· `bundle_menus_assets_all/MonoBehaviour/*.json`（序列化字段直读）。
6. ⛔ **禁用**：截图并排比、像素结论、`EffectCompare` 式视觉对拍（用户口径：画面由用户自己看）。

---

## §8 没查清的部分（如实记，⛔ 别当成已对齐）

1. `AllianceMemberOptionsPopup__Open.c` / `__Awake.c` 的 **outrank 判据**（账 30 的 Promote/Demote/Kick 三颗显隐）—— 本代理只读到 `MemberView.IsFriend` 的存在，没读角色比较式。
2. `AllianceView__Start.c` 里那两个 delegate 的**目标方法体**（⑥ `GoogleTranslation.onTranslationReady` 那一跳、⑦ `descriptionText` 的 DropDelegate = 「换行规整」）—— 只看到委托构造，没读到方法名/体（账 101）。
3. `UIRankingRow__Config.c`（父类）**是否无条件写三颗文本**（账 90 ② 的前提）—— 本代理未读父类。
4. `SupportMethods__GetTimeStringWithSingleDigit.c`（账 305 的**时间格式常量与进位规则**）—— 未读；⛔ 自拟必错。
5. `TextLinkProcessing__TryProcessText/__ProcessText/__ProcessURL.c` 的实现细节（账 429）—— 只确认三份产物存在。
6. `JoinAllianceMenu` 的 **Fetch 回调体**（`__c__DisplayClass…_b__N` 系列）：账 407『空表关整颗 Invitations』、账 408『`Members>0` / `Take(200)` / `Name.Contains` 过滤』都写在回调里，本代理只读了外层方法。
7. `AlliancesController.InvalidateLocalGroup` 的**调用点**（谁在什么时机失效缓存）—— 未查（账 117）。
8. `AllianceMemberEntry` 两组**在线状态 Color 常量**（`_DAT_1834b3040` 组 / `_DAT_1834b2c00` 组）的具体 4 字节 float —— 未用 `read_literal` 现读（账 26 ④）。
9. `ProfileNameTitleSection.cs:127-146`（`ProfileAllianceDisplay.Initialize` 的**内联真跑版**）—— 未读全文（账 438）。
10. `AllianceView__Draw.c` 两处 `AllianceRatingDisplay.Initialize` 的 **useSealSystem / zeroBasedRank 实参组合**（账 93 ⑤）—— 只看到调用，未逐值核参数。

---

## §9 跨行共享结论：联盟徽标族（14 / 21 / 73 / 88 / 90 / 187 都撞它）

1. **图在本地**：`bundle_alliancesbadgesimages_assets_all/Texture2D/` 有 **22 张** —— 18 张军团奖杯图（`Alliance_Trophies_<阵营>`，含 `BlackLegion` / `Dark Angels` / `Ork` / `Tau` / `Ultramarines` …）+ `Alliance_Trophies_TIER1..4` + `Alliance_Trophies_Default_v2`。
   **其中 TIER1..4 已导入** `Resources/Art/ui_menu/`（`Shell/AllianceTrophyArt.cs` 已在用）；**18 张军团图未导入**。
2. **映射不在本地**：原版链路 `GroupBadge.featuredTrophy → AllianceTrophyData.reward(LoadableReference<AllianceBadge>) → AllianceBadge.badgeImage`（`AllianceBadgeDrawer__Draw.c:24-30`、`__SetTierFrame.c:47-57`）。`AllianceBadge` 的 SO **全库 0 个**（已逐 bundle 核过 `assets_full` 的 MonoBehaviour 名单）⇒ **无 id→图**。
3. **兜底档存在**：`Draw(null)` 会回落 `AlliancesTrophyController.DefaultTrophy` ⇒ `Alliance_Trophies_Default_v2`。
   ⇒ **务实口径**：`Frame`（TIER 档位框）**可画**；`Badge` 只有「导入 18 张军团图 + 自建 army→图 表」之后才能画（那一步属资源导入 + 我们自定的映射 ⇒ 按铁律 18 打标记）；今天一律**空 + 出声**，⛔ 别按阵营名/卡名猜图。

---

## §10 与本文件一起交付的东西

- `判据/B9_社交C_施工单.tsv` —— 12 列 × 48 行（表头与 `B9_剩余C_施工单.tsv` **逐字相同**，LF、无 BOM；账行号已逐行核过「行号 ↔ 原版类 ↔ 状态 C」）
- `判据/B9_社交C_派活包.md` —— 本文件
- `报告/B9_J9SC_社交C施工单.md` —— 结论 / 逐条证据 / 没查清 / 改动清单 / 🔴 顺手发现
