# AUD05 · 状态 × 参数矩阵 ——【社交窗族】聊天 / 好友 / 联盟 / 社交子菜单

> 代理：`B103_A05-W6`（只读查判据） · 日期：2026-10-30 · 只读 `d:/2` 与 `Unity/MyGame/Assets`，未跑 Unity、未动 git、未动账与正本。
> 触发口径：铁律 5·c「一个值 ≠ 全部情况」。
> **本文件只覆盖社交窗族四组**（聊天 `ChatPanel`／好友页／联盟三支／`Social Submenu Variant` 外壳）。
> 通用结论（`smallScreenUI`、CanvasScaler 1920×1080 Expand、`GameWindow.extraScaleSmallScreen` 语义）**直接引用**
> `AUD05_state_matrix_卡组编辑.md` §2 / §5·5 / 附录A，⛔ 本文件不抄第二份。

---

## §0 结论摘要（先看这 7 行）

| # | 组 | 查出几种状态 | 我们缺哪几档（详表见 §4） | 强度 |
|---|---|---|---|---|
| 1 | **聊天**（`ChatPanel` + `Chat` + `ChatMessageRow` + `ChatPreview`） | **7 类**：频道数(1/2) · 禁言(是/否) · 发信侧(自己/别人) · 消息类型(6 模板 + 8 空串) · 翻译三态 · 玩家选项面板(5 钮 + 是否已好友) · 预览条(未收到/收到/点开) | ①行高固定 60（原版按消息长度长高）②新消息滑入 ③15s 计时刷新 ④翻译三态视觉 ⑤输入去富文本 ⑥按频道开窗 | ✅ 状态已查清；**5 条实现缺口** |
| 2 | **好友**（`Social Submenu Variant > Friends Tab`） | **5 档**：在线 Connected / 离开 Away / 离线 Disconnected / Default / 空表；排序两趟；双列网格 | 几乎全实现（`ApplyStatus` 四档齐）；缺「页签红点 provider」 | ✅ 已核实 |
| 3 | **联盟**（`AlliancesTab` / `AllianceSearchTab` / `AllianceMemberTab` / `AllianceView` / `TrophiesWindow` / 两个 popup） | **9 类**：已入盟/未入盟 · Join/Create 页 · 搜索有/无文本 · 隐私 Open/Private/满员 · 成员角色 4 档（8 钮显隐）· 在线三档 · 赛季切换(评分 0)· 奖杯 hideIfNotAchieved / 精选 · 活动面板 in/not-in alliance | ①成员行在线只做两档（缺 Away 黄、缺「离线透明」）②缺赛季规则 ③缺页签红点 provider | ✅ 已核实；**3 条实现缺口** |
| 4 | **社交子菜单**（`Social Submenu Variant`） | **4 档**：Alliances 页签（FeatureChecker 开/关）· NotMember/Member 变体 · 页签红点 · 小屏（1） | 全实现 | ✅ 已核实 |
| 5 | **两扇带 1.35 的窗** | `Member Options Panel` / `Alliance Trophy Info Popup`：小屏开 ⇒ **窗根 ×1.35**；小屏关 ⇒ ×1 | 已实现（常量 + 缩放器） | ✅ 已核实 |
| 6 | **一个值 ≠ 全部情况的关键点** | 好友页的 `GridLayoutGroup` 是 **Flexible（按宽现算）**、不是 `FixedColumnCount`；聊天行高**不是常量**（CSF PreferredSize 撑） | —— | ✅ 已核实 |
| 7 | **`Friends Menu Demo` 不是线上窗** | 主菜单场景对它 **0 引用**；真正的好友页在 `Social Submenu Variant > Friends Tab`（见附录A①） | —— | ✅ 已核实 |

---

## §1 权威公式 / 布局来源

### 1·1 共用（引用，不复制）

| 项 | 值 | 出处 |
|---|---|---|
| CanvasScaler | `ScaleWithScreenSize` · ref **1920×1080** · `ScreenMatchMode=Expand` · `MatchWidthOrHeight=0` | `AUD05_state_matrix_卡组编辑.md` §2 末行（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2577.json`） |
| `GameWindow.extraScaleSmallScreen` | 恒 `1f`、全库 0 覆写 ⇒ 「不覆盖」语义 | 同上 §5·5 附录A② |
| `TransformScalerBySmallScreenUI` 生效条件 | `menuScale != 1 **且** GameStaticData.smallScreenUI` ⇒ `enabled=true`；否则 `false` | `out_cpp2il_dev_ilspy/TransformScalerBySmallScreenUI.cs:69-80`（`Initialize` 逐句） |
| `smallScreenUI` 出厂值 | `false` | `GameStaticData` cctor（同卡组编辑 §3·1 S0） |

### 1·2 本族专属的「会变的」算式

| 件 | 算式 / 规则 | 出处 |
|---|---|---|
| **好友列表列数** | **UGUI `GridLayoutGroup`，`m_Constraint = 0 (Flexible)`** ⇒ `cols = floor((Content.w − padL − padR + spacingX) / (cellW + spacingX))`；**`constraintCount = 2` 是死值**（Flexible 下不采）。Content.w = **1543.65** · cell 721.3 · spacing 11.2 · pad(L16,R0) ⇒ `floor(1538.85 / 732.5)` = **2 列** | `Social Submenu Variant > Friends Tab > Friends List > Friends Container > Viewport > Content`（`menu_dump.py … "Social Submenu Variant" --depth 7`，`【GridLayoutGroup】 cellSize=721.3×84.82 spacing={11.2,12.7} pad=16,0,20,0 constraint=0(2)`） |
| **好友列表内容高** | `ContentSizeFitter m_VerticalFit = 1 (MinSize)` ⇒ `padT+padB + rows×cellH + (rows−1)×spacingY`（空表 = `20+0−12.7 = 7.3`，与 prefab 出厂 `sz=(0,7.3)` 逐值相等） | 同上（CSF 刻在 Content 上）；我方算式 `FriendsTab.cs:330` |
| **聊天每行高度** | **不是常量**：根 `ContentSizeFitter v:MinSize`、子件 `VerticalLayoutGroup spacing=12 pad=(L25,R60,T10,B10)`，正文 `Message` 自己挂 `ContentSizeFitter v:PreferredSize`（22px · 折行=1）⇒ **消息越长行越高** | `menu_dump.py … bundle_mainmenualwaysloaded_assets_all "ChatMessageRow" --depth 3`（`ChatMessageRow CSF v:MinSize=57 … Message CSF v:PreferredSize`） |
| **聊天行左右内边距** | 按发信侧**对调**：自己发 `L=timePadding(25)/R=namePadding(60)`；别人发 `L=60/R=25` | `out_cpp2il_dev_ilspy/ChatMessageUI.cs:179-193`（`SetLayout`）；prefab 基准 `monoBehaviour_-7156338170297815041` 的 `namePadding=60 · timePadding=25` |
| **聊天滚动** | **不是 `ScrollRect`**，是 TheFallenGames **OSA** 虚拟列表：`_ContentPadding=(25,25,5,5)` · `_ContentSpacing=10` · `_DefaultItemSize=60` · `_ScrollSensivity=20` | `bundle_mainmenualwaysloaded_assets_all/MonoBehaviour/MonoBehaviour_-8786325734254409834.json`（我方 `ChatTab.cs:92-117` 已逐字段抄录） |
| **聊天消息缓存上限** | 每频道 **100 条**，超了 `RemoveAt(0)` | `ChatGlobalManager.cs:2848-2851`（`TryCacheMessage`） |
| **聊天新消息动画** | 最后一条 `DOMoveY(自身 y, 0.3s).From(y−10)` | `ChatContentView.cs:90-107`（`Update`） |
| **聊天计时刷新** | `updateDelay = 15`（秒）⇒ 每 15 秒 `UpdateTimers()` 刷一遍相对时间 | `ChatContentView.cs:518`（ctor）+ `:71-89`（`Update`） |
| **玩家选项面板定位** | 面板 **x / z 保持不动**，只把 **y 对齐被点那一行**（`ChatMessageRow` 的 pivot = (0.5,0.5) ⇒ 即**行中心**） | `decomp_full/ChatPlayerOptionsPanel__ShowOptionsFor.c:66-78`；行 pivot = `bundle_mainmenualwaysloaded_assets_all/RectTransform_7760131448890879999.json` |
| **联盟成员排序** | `OrderByDescending(m => m.Rating)`，行号 = 排序后序号（1 起） | `AllianceMemberList.cs:193-227` |
| **联盟成员在线色** | `Away(2)` ⇒ **(1, 0.9216, 0.0157, 1)** 黄 · `Connected(3)` ⇒ **(0, 1, 0, 1)** 绿 · 其它 ⇒ **(0,0,0,0) 透明**，**但自己那一行恒绿** | `decomp_full/AllianceMemberEntry__Initialize.c:166-192`（常量 = 0x1834b3040 / 0x1834b2c00，二进制实读） |
| **联盟搜索过滤** | ①服务器按串查 ②`Where(MemberCount > 0)` ③**仅当串非空**再 `Where(name.Contains(串, OrdinalIgnoreCase))` ④`Take(200)` | `JoinAllianceMenu.cs:1182-1272`（`FillOpenAlliances`） |
| **联盟成员数上限** | `AlliancesConfig.maxMemberCount` = **20**（代码默认值；**远端 config 资产本地 0 命中** ⇒ 现行只可说「默认 20」） | `AlliancesConfig.cs:5`（`private int maxMemberCount = 20;`） |

---

## §2 prefab 序列化值（可引用 AUD02 的地方已标行）

> 🔴 AUD02 已给的 **不重查**：`ChatPanel` = `AUD02_serialized_params.tsv:676-768` · `Chat` = `:584-675` · `AllianceRankingRow Variant` = `:555-583` · `ChatPreview` = `:769-792`。
> 本节只补 AUD02 **没有**的窗（好友 / 联盟四件 / 社交子菜单）与新读到的关键数。

### 2·1 窗根与小屏

| 窗 / 件 | 关键序列化值 | 出处 |
|---|---|---|
| `ChatPanel` 根 | RT anchors `(1,0)-(1,1)` · pivot `(0.21,0.5)` · `sizeDelta=(1972,0)` · `anchoredPos=(-1584,0)` ⇒ 绝对矩形 **-78.12 … 1893.88 × 0 … 1080** · `menuScale` 缺省(1) · `extraScaleSmallScreen=1.0` | `GameObject/ChatPanel.json` → `RectTransform_1905389679266213426.json` · `MonoBehaviour_1773913423464410674.json` |
| `Social Submenu Variant` 根 | 1920×1080 · Content Area `167.17,70.94→1920,1080` · `extraScaleSmallScreen=1.0` · 无 `menuScale` | `GameObject/Social Submenu Variant.json`：`MonoBehaviour_4523486740489232547.json` |
| `Member Options Panel` 根 | **`menuScale = 1.35`** · `extraScaleSmallScreen=1.0`（= 「不覆盖」）· 面板 `387.30 × 390` | `MonoBehaviour_-1026376857256685800.json` |
| `Alliance Trophy Info Popup` 根 | **`menuScale = 1.35`** · `extraScaleSmallScreen=1.0` · 窗 `1128.55 × 663.30` | `MonoBehaviour_-3185861090812863363.json` |

### 2·2 社交子菜单（`Social Submenu Variant`）

| 节点 | 值 | 出处 |
|---|---|---|
| `Tab Buttons`（左栏） | `VerticalLayoutGroup`：`spacing=0` · `align=1` · `pad=0,0,120,0` · `ctrlW=1 / ctrlH=0` · `expandW=1 / expandH=0` · 宽 **165** | `menu_dump.py … "Social Submenu Variant" --depth 5`（`Tab Buttons … spacing=0.0 align=1 pad=0,0,120,0 ctrlW=1 ctrlH=0 expandW=1 expandH=0`） |
| `Alliances Tab Button` / `Friends Tab Button` | 各 **165 × 180**；`EverguildToggle isOn=0 onColor=(1,1,1,1) offColor=(0.75,0.75,0.75,1)`；`Alliances Tab Button` 另挂 `FeatureChecker featureKey=Alliances` | 同上 |
| `Badge Highlight`（两颗） | **35 × 35** · `Image` + **`UiBadgeNotification`**（`showCounter=1`）· 位置 = 键右上（首颗 `283.9,215.5→318.9,250.5`，键 `190.9→370.9`） | 同上；定义 = `MonoBehaviour_7673959444828905635.json` |
| `Badge` 的通知类型 | `notificationType = Everguild.UI.Notifications.AlliancesNotificationBadge` · `useFilter = 0` | `MonoBehaviour_7673959444828905635.json` |
| `Alliances Tab` 两支 | `AllianceNotMemberVariant` act **T** · `AllianceMemberVariant` act **F** | `menu_rect.py … "Social Submenu Variant" --depth 4` |
| `Shadow` / `Shadow (1)` | `Shadow` act T（47.64 宽，`(0,0,0,0.475)`）· `Shadow (1)` act **F**（49.43 宽） | 同上 + `menu_dump` |

### 2·3 好友页（`Social Submenu Variant > Friends Tab`）

| 节点 | 值 | 出处 |
|---|---|---|
| `Friends Tab` 根 | act **F**（默认在 Alliances）· `FriendsTab` 字段 `addButton / friendElementPrefab / friendsContainer / inputFieldUserName / instantDuelButton / usernameText` | `menu_dump … --depth 6` |
| `Friends Container` | `ScrollRect h=0 v=1 mode=1 inertia=1 elasticity=0.1 decel=0.135` · 1543.65 × 765.26 @ `332.2,314.8` | 同上 |
| `Content` | `ContentSizeFitter(MinSize)` + **`GridLayoutGroup` cellSize 721.3×84.82 · spacing (11.2,12.7) · pad(16,0,20,0) · constraint=0(Flexible)** | 同上 `--depth 7` |
| `Friend Info Item`（行） | 670.16 × 58.46 · `FriendUIElementWarpforge` · `connectedColor = (0.3094, 0.6415, 0.2572, 1)` · `awayColor = (0.9961, 0.5961, 0.1176, 1)` | `menu_dump … bundle_menus_assets_all "Friend Info Item"`；`MonoBehaviour_-842203043721886928.json` |
| 行内状态点 | `Connected Image` act **F**（出厂关）· `Disconnected` act T · `Disconnected` 色 **(0.84, 0.494, 0.44, 1)** | 同上 |

### 2·4 联盟四件

| 节点 | 值 | 出处 |
|---|---|---|
| `Alliance List Entry`（公开列表行） | 750-ish 宽 × **110 高** · `AllianceListEntry` | `menu_rect`（行高见我方 `AllianceSearchTab.cs:57` 记的 110） |
| `Alliance Member Entry`（成员行） | **750 × 100** · `AllianceMemberEntry` · `isPlayerEntryColor = (0.15259, 0.48235, 0, 1)` · `background` 染色 `(1,0.36,0,1)` / 内 `Image` `(0.481,0.182,0,1)` | `menu_dump … "Alliance Member Entry" --depth 3`；`MonoBehaviour_-2068273713529560925.json` |
| 成员行 `connection status` | **单颗 Image** 22.56×29.02，出厂色 `(0,1,0.0736,1)`（运行期被 §1·2 三档覆盖） | 同上 |
| `Member Options Panel` | `bg` 387.30 宽 · `Buttons` `VerticalLayoutGroup` · 8 颗（`Challenge / Add as a friend / Profile / Promote / Demote / Kick / Quit / Debug Add Skulls`），宽 357.30、**步进 65** | `menu_rect … "Member Options Panel"`（该树 `sizeDelta` 出厂为 0 ⇒ 用 VLG 现算） |
| `Alliance Trophy Info Popup` | `Category` act **F** · `Controls` 下 `Progress` / `selectButton` · `ProgressBar`/`Next Tier`/`Checkbox` 更深 | `menu_rect … "Alliance Trophy Info Popup" --depth 4` |
| `Invitation List Entry` | 高 **110** · `AllianceInvitationEntry` · 两键 `Join` / `Reject`（各 200×57）· `Member Count` 样例 `17/20` | `menu_dump … "Invitation List Entry" --depth 3` |
| `AllianceRankingRow Variant` | 见 AUD02 `:555-583`（`Ranking` 50.3 / `Name` 40 / `Guild Name` 30 / `Points` 43.2 / `m_characterSpacing=1.2`） | AUD02 |

---

## §3 状态 × 参数矩阵（四组分表）

> 每格带出处；**查不到写 `?`**，⛔ 不拿序列化默认值顶替。

### 3·1 聊天（`ChatPanel` / `ChatTab` / `ChatMessageRow` / `ChatPlayerOptionsPanel` / `ChatPreview`）

| # | 状态 | 判定条件 | 参数 / 后果 | 出处 |
|---|---|---|---|---|
| C1 | **频道 · Global only** | `ChatGlobalManager.subscribedChannels` 只含 `Global`（`IsInGroup == false`） | **1 个页签**（只 Global） | `ChatGlobalManager.cs:1044-1059`（`Initialize`：无条件订阅 Global；`IsInGroup` 才追加 Alliance，id = `CurrentGroupId`） |
| C2 | **频道 · 已入盟** | `IsInGroup == true` 且 `AlliancesManager.IsAlliancesEnabled` | **2 个页签**（Global + Alliance）；页签图标 `ChannelType==0 ? globalIcon : allianceIcon` | `ChatPanel.cs:566-649`（`SetupTabs`） |
| C3 | **页签视觉序** | 始终 | `VerticalLayoutGroup reverse=1 + UpperRight` ⇒ 视觉序 = **创建序倒过来** | AUD02 `:584` 三行（`Chat` / `Tab Buttons`）· 我方 `ChatPanel.cs:22 · :82` |
| C4 | **禁言 · 否** | `!PlayerDataManager.IsBannedFromChat(channelType)` | `sendMessageButton.interactable = true` · `inputField.interactable = true` · 输入框**清空** | `ChatPanel.cs:472-497`（`CheckChatBan`） |
| C5 | **禁言 · 是** | `IsBannedFromChat(channelType)` | 发送键 `interactable=false` · 输入框 `interactable=false` · 输入框文字 = I2 词条（本地无 value） | 同上 |
| C6 | **发信侧 · 别人** | `message.playerId != 我的 id` | 底色 = **friendBackgroundColor (0.1059, 0.0039, 0.0039, 0.7059)** · `pad L60 / R25` · 走 `Friend Header`（头像在左） · **整行可点**（`messageButton`）· 头像可点（`portraitButton`） | `ChatMessageUI.cs:116-177`（`Set`）+ `:179-193`（`SetLayout`）；我方 `ChatPanel.cs:806/814/889-892` |
| C7 | **发信侧 · 自己** | 同上相反 | 底色 = **playerBackgroundColor (0.0431, 0.0941, 0.1333, 0.8078)** · `pad L25 / R60` · `Player Header`（头像在右） · **头像不可点** | 同上 |
| C8 | **消息类型 · Message(50) / Announcement(100)** | `type ∈ {50,100}` | 正文 = `translatedMessage ?? msg`；**点击 = `TranslateMessage()`** | `ChatMessageData.cs:138-269`；`ChatMessageUI.cs:280-312` |
| C9 | **消息类型 · Join/Leave/Kick** | 20 / 30 / 60 | 键 `SocialMenu/Alliances/Messages/{类型名}`，替换 `{playerName}` `{targetPlayer}` `{locData}`（`locData` 再走一次 GetTranslation） | 同上 |
| C10 | **消息类型 · Promoted/Demoted** | 80 / 90 | 同上，但 `{locData}` = **角色词条**（`data` 先 TryParse int，否则按 `/` 切末段 Enum.TryParse；失败 ⇒ 空串） | 同上 |
| C11 | **消息类型 · DeckShare(150)** | 150 | 键 `…/Messages/DeckShare` + `string.Format(键, playerName, deck.deckName)`；卡组串 = `data.Split('\n')` **末行**；点击 ⇒ 开 `DeckInfoPopup`（**Import** 态） | 同上；`ChatMessageUI.cs:280-312` |
| C12 | **消息类型 · 其余 8 种** | 0/10/40/70/110/120/130/140/160 | **一律空串**（原版就是 `return ""`）· 点击**无分支** | `ChatMessageData.cs:261-262` |
| C13 | **翻译三态** | `translationStatus` = Normal / Translating / Translated | Normal ⇒ `normalFontMaterial`；Translating ⇒ `translatingFontMaterial` + 正文尾加 ` (翻译中)` 词条；Translated ⇒ `translatedFontMaterial` | `ChatMessageUI.cs:195-228`（`SetMessageText`）· `ChatMessageData.cs:11-16`（枚举） |
| C14 | **空消息表** | 0 条 | 列表 0 行、内容高 = `padTop + padBottom`（OSA 空表） | `ChatContentView` 无特判（`OnInitialized` 只回放缓存） |
| C15 | **消息过长** | 正文折行超一行 | **行高跟着长**（`Message` CSF PreferredSize） | §1·2 行高式 |
| C16 | **新消息进列表** | 非缓存消息且列表已初始化 | 最后一行 `DOMoveY(0.3s)` 从 **y−10** 滑上来；若上一段动画还在 ⇒ 先 `Kill(complete:true)` | `ChatContentView.cs:409-454`（`AddMessage`）+ `:71-107`（`Update`） |
| C17 | **相对时间文本** | `timestamp` → `UtcNow − 时间` | `SupportMethods.GetTimeStringWithSingleDigit(ts, eventTimer:true)`（如 `1d 6h`）；**每 15s 刷一次** | `ChatMessageUI.cs:257-268` · `ChatContentView.cs:518` |
| C18 | **玩家选项面板 · 未开** | 出厂 | `Player Options Panel` act **F** | `menu_rect … "Chat"`（`Player Options Panel … INACT`） |
| C19 | **玩家选项面板 · 开** | 点头像（`ShowOptions`） | 面板 `SetActive(true)`；**x/z 不动、y 对齐被点行中心**；标题 = `message.playerName` | `ChatPlayerOptionsPanel.cs:74-112` · `decomp_full/…ShowOptionsFor.c` |
| C20 | **已加好友** | `FriendsData.IsFriend(playerId)` | `AddButton.interactable = false`（**不隐藏**，只是不可点） | `ChatPlayerOptionsPanel.cs:98-100` |
| C21 | **切页签** | `OnTabChange` | `optionsPanel.SetActive(false)` + `CheckChatBan()` | `ChatPanel.cs:464-470` |
| C22 | **预览条 · 未收到消息** | `receivedMessages == false`（或订阅后仍无 Global 消息） | 关着；`icon.localScale = 0` · `messageContainer.fillAmount = 0` · 两条 `ChatPreviewMessage` `enabled=false` | `ChatPreview.cs:46-67`（`OnBeforeInitialization`）· `:283-303`（`WaitAndAnimate`） |
| C23 | **预览条 · 收到 Global 消息** | `channel == "Global" && messages.Count > 0` | `SetActive(true)`；icon `DOScale 1 (0.6s OutBack, From 0)`；容器 `DOFillAmount 0→1 (0.4s, delay 0.2)` + `DOFade 0→1 (0.2s, delay 0.2)`；两行填 `TakeLast(消息, 2)` | `ChatPreview.cs:180-195`（`Show`）· `:203-278`（`UpdatePreviews`） |
| C24 | **预览条 · 点开** | 点按钮 | icon `DOScale 0 (0.3s InBack)`；容器 `fill 1→0 (0.2s, delay 0.1)` + `fade 1→0 (0.15s, delay 0.1)`；**OnComplete 才** `SetActive(false)` + 开 `ChatPanel`（带 `ChatPanelContext(ChatRoom.Global)`） | `ChatPreview.cs:107-179`（`OpenChat`） |
| C25 | **预览条两行文本** | 始终 | 18px？→ 实际 `Message Preview{, (1)}/text` 两档字号（AUD02 记 18 与 23 两组，**以 AUD02 `:769-792` 的逐行值为准**）· `overflowMode = Ellipsis(1)` · `wrap=0` · `RaycastTarget=0` | AUD02 `:769-792` |
| C26 | **聊天窗 Context 起始频道** | `ChatPanelContext.startingChannel` | 打开时 `Enumerable.FirstOrDefault(tabs, t => t.channel.ChannelType == startingChannel)` ⇒ `ChangeTab`；取不到则落第一个页签 | `ChatPanel.cs:224-462`（`Open`）· 调用点 `ChatPreview.cs:123/330`（Global）、`DeckInfoPopup.cs:1176`（`channelType`，可为 Alliance） |

### 3·2 好友（`Social Submenu Variant > Friends Tab`）

| # | 状态 | 判定条件 | 参数 / 后果 | 出处 |
|---|---|---|---|---|
| F1 | **在线 Connected(3)** | `ChatGlobalManager.GetPlayerStatus(id) == Connected` | 显示 `Connected Image`（绿点）+ `color = connectedColor (0.3094,0.6415,0.2572,1)`；`Disconnected` 关 | `FriendUIElementWarpforge.cs:54-96`（`Initialize`）· `:118-154`（`UpdateStatusColor`） |
| F2 | **离开 Away(2)** | `== Away` | `Connected Image`（**同一个 sprite**）+ `color = awayColor (0.9961,0.5961,0.1176,1)` | 同上 |
| F3 | **离线 Disconnected(1) / Default(0) / 其它** | 其它 | `connectedImage.SetActive(false)` + `notConnected.SetActive(true)`（**不改色**） | 同上 |
| F4 | **空表** | `friendList.Count == 0` | 容器 0 行；内容高 = **7.3**（CSF MinSize 空表式） | §1·2 · `FriendsTab.cs:263-341`（`FillInFriendsContainer`） |
| F5 | **排序** | 始终 | 两趟 `List.Sort`：先 `SortByName` = `string.Compare(b.name, a.name, OrdinalIgnoreCase)`（**Z→A**），再 `SortByConnected`（Connected 优先，其余 0）。**`List.Sort` 不稳定** ⇒ 组内次序无承诺 | `FriendsTab.cs:223-261`（两个比较器）· `:320-330`（调用点） |
| F6 | **列数** | 始终 | `GridLayoutGroup Flexible` ⇒ **按宽现算**（现值 2 列）—— ⛔ 不是 `constraintCount=2` | §1·2 |
| F7 | **列表满 → 滚动** | 行数 × 84.82 > 765.26 | `ScrollRect` 竖滚（`v=1`，`elasticity 0.1`，`decel 0.135`） | §2·3 |
| F8 | **搜索框** | 始终可输入 | **只用于「加好友 / 即时对战」两个消费方**；**好友列表不做过滤**（`FillInFriendsContainer` 全文不读输入框） | `FriendsTab.cs:87-221`（`ClickAddFriend` / `ClickDuelPractice`）—— 判据 = 方法体全文 |
| F9 | **`Instant duel` 键** | prefab 有该字段 | `OnOpen` 里 `if ((bool)instantDuelButton)` 才挂监听（**键在就挂**） | `FriendsTab.cs:87-100` |
| F10 | **加好友失败** | 名字非法 / 查不到 | `ShowPopUp("Demo/FriendsMenu/InvalidName")` / `("Demo/FriendsMenu/CantFind")` | `FriendsTab.cs:161-210` |
| F11 | **删好友** | 点删除 | 确认弹窗（`Demo/FriendsMenu/DeleteFriendConfirmation`，词条带 `{name}`）⇒ 确认后删 + 整表重填 | `FriendsTab.cs:446-497` |
| F12 | **页签红点** | `FriendsBadge` 计数 > 0 | `Badge Highlight` alpha 渐显（`UiBadgeNotification`，3.0/s）+ 计数文本 | `UiBadgeNotification.cs:161-193 / 296-411`；**计数来源 = `ChatGlobalManager.CheckNotification()` = `PendingPracticeMatches.Count`**（`ChatGlobalManager.cs:3101-3110`，⚠️ **不是好友申请**） |
| F13 | **小屏** | `smallScreenUI == true` | 本窗无 `menuScale` ⇒ **不缩放**；仅 TMP 侧（`AUD05_卡组编辑` §2 那档） | §2·1 |

### 3·3 联盟

#### 3·3·a 外壳与两支

| # | 状态 | 判定条件 | 参数 / 后果 | 出处 |
|---|---|---|---|---|
| A1 | **未入盟** | `AlliancesController.CurrentPlayerCache == null`（反编译里写成 `<= 0`，是 `==null` 的伪码） | `AllianceNotMemberVariant.SetActive(true)` + `Initialize()`；`AllianceMemberVariant.SetActive(false)` | `AlliancesTab.cs:11-27`（`ToFocus`）· `:81-97`（`Refresh`） |
| A2 | **已入盟** | 相反 | `AllianceMemberVariant.Initialize()`；`AllianceNotMemberVariant.SetActive(false)` | 同上 |
| A3 | **Alliances 功能开关** | `FeatureConfig.CheckFeature("Alliances")` | `Alliances Tab Button` / 左竖导航第 5 键 `SetActive`；关掉时社交窗应落 `Friends` 页 | `FeatureChecker.cs:11-19`（`OnEnable` 派发 `OnFeatureChecked`）· 挂点表见我方 `Shell/FeatureChecker.cs:13-24` |

#### 3·3·b 未入盟支（`AllianceSearchTab`：Join / Create / 详情）

| # | 状态 | 判定条件 | 参数 / 后果 | 出处 |
|---|---|---|---|---|
| A4 | **Join 页** | `onCreateTab == false` | `joinButton.isOn = 1`；`joinAllianceMenu` 开、`createAllianceMenu` / `allianceView` 关；`Join` 键文案 = `SocialMenu/Alliances/Join` | `AllianceSearchTab.cs:150-171`（`Initialize`） |
| A5 | **Create 页** | `onCreateTab == true` | `createButton.isOn = 1`；`createAllianceMenu` 开 | `AllianceSearchTab.cs:172-183` |
| A6 | **联盟详情态** | `HandleDisplayAlliance(group)` | 两键都 `isOn = false`；`allianceView` 开、两个 menu 关；**`Join` 键文案被换成 `MainMenu/MainButtons/ButtonLabel/Back`**；`onCreateTab = false` | `AllianceSearchTab.cs:224-255` |
| A7 | **搜索 · 无文本** | `searchInput.text.Trim().Length == 0` | 只过滤 `MemberCount > 0`；`Take(200)` | §1·2 · `JoinAllianceMenu.cs:1182-1272` |
| A8 | **搜索 · 有文本** | 同上 > 0 | 追加 `name.Contains(串, OrdinalIgnoreCase)`；服务器侧也按同串查（本地无服务器 ⇒ 只剩本地这一道） | 同上 |
| A9 | **搜索 · 取数中** | `FillOpenAlliances` 进行中 | `searchButton.interactable = false` + `MenuLoadingController.ShowLoading()`；回调第一句 `HideLoading()` + 恢复可点 | `JoinAllianceMenu.cs:1189-1203` |
| A10 | **搜索 · 空结果** | 过滤后 0 条 | 列表 0 行（**原版没有「无结果」文案**） | 方法体全文 0 处 `empty/noResult` |
| A11 | **邀请列 · 空** | `invitations.Count == 0` 或过滤后空 | `invitationList.SetActive(count != 0)` —— **整颗（标题 + 行容器）关掉** | `JoinAllianceMenu.cs:480-538` · `:584-700` |
| A12 | **邀请列 · 有** | > 0 | 展开；`loading` 重入守卫（`if (loading) return;`） | 同上 |
| A13 | **行 · 公开可加入** | `privacy == Open(0)` ∧ `MemberCount < maxMemberCount` | `joinButton.interactable = true`，文案 `Join` | `AllianceListEntry.cs:225-290` |
| A14 | **行 · 满员** | `MemberCount >= max`（且 Open） | `joinButton.interactable = false`（**文案仍 Join**） | 同上 |
| A15 | **行 · 私密** | `privacy == Private(1)` | `interactable = false` + **文案换成 `SocialMenu/Alliances/Privacy/Private`** | 同上 |
| A16 | **行 · alliance 为 null** | 数据缺失 | `Debug.LogError("Alliance for {name} is null")` + `Destroy(gameObject)` | `AllianceListEntry.cs:282-289` |
| A17 | **建盟表单** | Create 页 | `Select Language` / `Select Privacy` 两个下拉（`GameStaticData.GroupLanguages` 索引定位） | `AllianceView.cs:406-414`（同一族）· 我方 `AllianceSearchTab.cs:963-980` |

#### 3·3·c 已入盟支（`AllianceMemberTab` / `AllianceView` / `TrophiesWindow`）

| # | 状态 | 判定条件 | 参数 / 后果 | 出处 |
|---|---|---|---|---|
| A18 | **General 页** | `onTrophyTab == false` | `generalButton.isOn = 1`；`trophiesView` 关、`generalView` 开；`GetCurrentGroup(generalView.Draw)` | `AllianceMemberTab.cs:28-75`（`Initialize`）· `:77-97`（`ShowGeneralView`） |
| A19 | **Trophies 页** | `onTrophyTab == true` | 反过来的三句 `SetActive` | `AllianceMemberTab.cs:99-113` |
| A20 | **成员列表排序 / 行号** | 始终 | `OrderByDescending(Rating)`；行号 = 排序后序号（1 起，`indexText`） | `AllianceMemberList.cs:193-227` |
| A21 | **成员数标题** | 始终 | `"{Members 词条} {count}/{max}"`（`SocialMenu/Alliances/Members` + `AlliancesConfig.maxMemberCount`） | `AllianceMemberList.cs:158-191` |
| A22 | **成员行 · 角色** | `GroupRole` 4 档 | `playerRole.text = GetTranslation("SocialMenu/Alliances/Role/" + ((Enum)role).ToString())` | `AllianceMemberEntry.cs:186-195` |
| A23 | **成员行 · 赛季评级** | `member.season == group.currentSeason` | 评价值 = `member.Rating`；**不等 ⇒ 0** | `decomp_full/AllianceMemberEntry__Initialize.c:118-125` |
| A24 | **成员行 · 活动(Draft)评级** | `LiveOpsManager.GetHandler<GameModes>().GetActiveEvent<TimedDraftEvent>()` | 有活动 ⇒ `member.events[eventId]`；**无活动 ⇒ 0** | 同上 `:126-150` |
| A25 | **成员行 · 评分为 0** | `disableIfZero = true` | `AllianceRatingDisplay` **整块 `SetActive(false)`**（图标 + 数字一起藏） | `AllianceRatingDisplay.cs:61-70`（方法头两句） |
| A26 | **成员行 · 在线三档** | 见 §1·2 | Away 黄 / Connected 绿 / 离线 **透明**（自己恒绿） | §1·2 |
| A27 | **成员行 · 自己那一行** | `member.IsPlayer` | `graphicsToChangeColorIfIsPlayer` 两颗 `Graphic.color = isPlayerEntryColor (0.15259,0.48235,0,1)` | `decomp_full/AllianceMemberEntry__Initialize.c:213-224`；prefab `MonoBehaviour_-2068273713529560925.json` |
| A28 | **成员选项面板 · 5→8 钮显隐** | 见右列 | `Challenge = !isSelf` · `Add as a friend = !isSelf && !isFriend` · `Profile = !isSelf` · `Promote = outrank && role < Leader(3)` · `Demote = outrank && role > Member(0)` · `Kick = outrank` · `Quit = isSelf` · **`Debug Add Skulls` 恒关**（`Awake()` 第一句） | `AllianceMemberOptionsPopup.cs:308-449`（`Open`）· `:139-143`（`Awake`） |
| A29 | **面板 · Promote 换字** | `member.role == Admin(2)` | 文案 = `SocialMenu/Alliances/TransferLeadership`；否则 `SocialMenu/Alliances/Promote` | `AllianceMemberOptionsPopup.cs:360-367` |
| A30 | **面板 · 1.35** | `smallScreenUI == true` | 窗根 ×**1.35**（`menuScale`）；关 ⇒ ×1 | §2·1 + §1·1 |
| A31 | **联盟详情 · 查看/编辑两态** | `toggleEditButton` | 查看态：`save/cancel/privacyDropdown/languageDropdown` 关、`regionText` 开、`toggleEdit` 开；编辑态：反过来 + `privacyDropdown.value = group.privacy`、`languageDropdown.value = IndexOf(GroupLanguages, group.region)` | `AllianceView.cs:427-538`（`Draw` 尾段 / `OnToggleEditClicked` / `OnEditCancel`） |
| A32 | **编辑入口可见性** | `isCurrentGroup && myRole >= Admin(2)` | `toggleEditButton.SetActive(true)`；否则关 | `AllianceView.cs:461-480` |
| A33 | **描述可编辑性** | `group.blockDescriptionChange \|\| myMember.blockDescriptionChange` | `descriptionText.interactable = !(… )` | `AllianceView.cs:487-501` |
| A34 | **联盟详情 · 语言/隐私合并串** | 始终 | `regionText.text = GetTermTranslation("SocialMenu/Alliances/Language/"+region) + "/" + GetTranslation("SocialMenu/Alliances/Privacy/"+privacy)` | `AllianceView.cs:371-382` |
| A35 | **奖杯 · 未达成隐藏** | `AllianceTrophyData.hideIfNotAchieved` | 该奖杯格**不建/不显示** | `AllianceTrophiesView.cs:150-198` |
| A36 | **奖杯 · 达成数文案** | `achieved == 1` | 单数键 `SocialMenu/Alliances/Trophies/Count/Singular`；否则复数 `…/Trophies/Count`（`string.Format` 带计数） | `AllianceTrophiesView.cs:262-281` |
| A37 | **奖杯 · 选中态** | `ToggleSelected(entry)` | 上一个 `highlight.SetActive(false)`、新的 `SetActive(true)`（**单选高亮**） | `AllianceTrophiesView.cs:288-300` |
| A38 | **奖杯弹窗 · 进度条** | `!progressBar.IsFilled && !data.dontShowProgress` | `progressHolder.SetActive(true)`，`SetProgress(groupScore, targetMilestone, "{0}/{1}")`；否则整块关 | `TrophyInfoPopup.cs:88-107` |
| A39 | **奖杯弹窗 · 精选勾选框** | `CurrentTier > 0 && myRole >= Admin(2)` | `toggle.gameObject.SetActive(true)`；`toggle.isOn = IsFeatured`；**若已精选且就是默认精选** ⇒ `interactable = (trophy.id != DefaultTrophy.featuredTrophy.id)` | `TrophyInfoPopup.cs:108-141` |
| A40 | **活动积分面板 · 已入盟** | `AlliancesManager.IsInGroup` | `inAllianceGO` 开 / `notInAllianceGO` 关；名字 + 进度条（`normalizedValue = InterpolateMilestones(score, [0, …thresholds])`）+ 里程碑刻度 | `AlliancesEventScorePanel.cs:110-161` |
| A41 | **活动积分面板 · 未入盟** | 相反 | `notInAllianceGO` 开；`JoinAlliance` 键 = 关全部窗 + 打开 Social 页 | `AlliancesEventScorePanel.cs:162-189` |
| A42 | **排行榜行 · 自己那一行** | `data.OwnPlayer` | `bgHighlight.enabled = true` / `bgNormal.enabled = false`；名字取**本机玩家名**（不是 `data.Name`） | `UIRankingRow.cs:22-50` |
| A43 | **排行榜行 · 回收/隐藏** | `Hide()` | `ToggleComponents(false)`（名次/分数/名字/奖杯图全关）+ `bgNormal.enabled = true` | `UIRankingRow.cs:52-81` |

### 3·4 社交子菜单（`Social Submenu Variant`）

| # | 状态 | 判定条件 | 参数 / 后果 | 出处 |
|---|---|---|---|---|
| S1 | **默认页** | 出厂 | `Alliances Tab` act T · `Friends Tab` act **F** ⇒ 默认 Alliances | §2·2 |
| S2 | **Alliances 被 FeatureChecker 关掉** | `CheckFeature("Alliances") == false` | `Alliances Tab Button` `SetActive(false)` ⇒ 页签消失，须落 Friends | `FeatureChecker.cs:11-19`；我方 `SocialWindow.cs:163` |
| S3 | **页签红点** | 见 F12 / 联盟 | 两颗各 35×35、`UiBadgeNotification`（alpha 补间 **3.0/s**，计数文本 `showCounter=1`） | `UiBadgeNotification.cs:161-193` |
| S4 | **联盟红点计数** | `AlliancesManager.CheckNotification()` | `IsAlliancesUnlocked && CurrentPlayerCache == null` ⇒ **`groupInvitationsCache.Count` 条**；否则 **0 条** | `AlliancesManager.cs:1755-1787` |
| S5 | **好友红点计数** | `ChatGlobalManager.CheckNotification()` | **`PendingPracticeMatches.Count`** 条（⚠️ 与「好友申请」无关） | `ChatGlobalManager.cs:3101-3110` |
| S6 | **小屏** | `smallScreenUI == true` | 本窗无 `menuScale` ⇒ **不缩放** | §2·1 |

---

## §4 我方落点与档位（现在哪一档 · 缺哪几档）

### 4·1 已对上（逐条）

| 状态 | 我方落点 | 判 |
|---|---|---|
| C6/C7 发信侧（底色 + 左右 padding 对调） | `ChatPanel.cs:806`（`RowBgColor`）· `:889-892`（`RowPadLeft/Right`，`PadSwap=35`） | ✅ 对 |
| C13 翻译三态的**字段** | `SocialData.cs:166-168`（`TranslationStatus` 0/1/2） | ⚠️ 只有字段，无视觉（见 4·2） |
| C8–C12 消息类型模板 | `SocialData.cs:273-330`（`Text`，6 模板 + 其余空串，含 Promoted/Demoted 角色词条回退） | ✅ 对（两处差异已如实记） |
| C19 面板 y 对齐 + x 不动 | `ChatPanel.cs:649`（`rowCenterY` = 行中心；行 pivot 0.5 ⇒ 等价） | ✅ 对 |
| C20 已好友 ⇒ 不可点（不隐藏） | `ChatPanel.cs:646-648` | ✅ 对 |
| C21 切页收起面板 | `ChatPanel.cs:669-679` | ✅ 对 |
| C4/C5 禁言 | `ChatPanel.cs:692-697` + `ChatPanel.cs:1033-1058`（`IsBanned` 走可注入 provider，默认 null） | ✅ 结构对（数据源本地无） |
| F1–F3 好友四档在线色 | `FriendsTab.cs:496-533`（`StatusOf` / `ApplyStatus`，含 `ConnectedColor` / `AwayColor`） | ✅ 对 |
| F5 两趟排序 | `FriendsTab.cs:301-310` + `:475-494` | ✅ 对 |
| F6 双列现算 | `FriendsTab.cs:286-289`（`ColumnsFor`） | ✅ 对（现值 2） |
| A1/A2 未入盟/已入盟两支 | `AlliancesTab.cs:126-150` | ✅ 对 |
| A4–A6 Join/Create/详情三态 + Join 键换字 | `AllianceSearchTab.cs:143-235` | ✅ 对 |
| A7–A10 搜索过滤 + Take(200) + 取数中置灰 | `AllianceSearchTab.cs:592-630` · `:460-475` | ✅ 对（2026-10-30 `B102_ALLY` 刚落） |
| A11 邀请列空 ⇒ 整颗关 | `AllianceSearchTab.cs:119-121` · `:498-505` | ✅ 对 |
| A13–A15 行的隐私/满员/换字 | `AllianceSearchTab.cs:698-757` | ✅ 对 |
| A18/A19 General ↔ Trophies | `AllianceMemberTab.cs:284-320` | ✅ 对 |
| A20 排序 + 行号 | `AllianceMemberTab.cs:2072-2095` | ✅ 对 |
| A28/A29 成员选项 8 钮显隐 + Promote 换字 | `AllianceMemberOptionsPopup.cs:455-490` | ✅ 对（`Outrank = SameGroup && Role < MyRole` 在 `:216`） |
| A30 1.35 小屏 | `AllianceMemberOptionsPopup.cs:126`（`MenuScale = 1.35f`）· `TrophyInfoPopup.cs:129/253-256` | ✅ 对 |
| A31–A33 联盟详情查看/编辑 | `AllianceMemberTab.cs:1497-1510` 一带 | ✅ 对 |
| S2 Alliances 页签开关 + 默认页改落 Friends | `SocialWindow.cs:163` | ✅ 对 |
| S3 角标补间 | `SocialWindow.cs:248-262`（只 `Recount`，⛔ 不写颜色） | ✅ 结构对 |

### 4·2 **缺口清单（一句一档，带落点）**

1. 🔴 **聊天行高固定 60** —— `ChatTab.cs:34` `DefaultRowH = 60f`，`Rebuild` 里 `all[i].Height > 0 ? Height : 60`（`:157/:177`），而 `SocialData.ChatMessage.Height` **全仓无写入方** ⇒ 长消息行高不增长（原版由 `Message` 的 CSF PreferredSize 撑高，见 §1·2）。**缺「消息过长」这一档**。
2. 🔴 **新消息滑入动画缺** —— `ChatTab.cs:137-193` 只重建行，无 `DOMoveY(…, 0.3s).From(y−10)`。
3. 🟡 **15 秒计时刷新缺** —— `ChatTab`/`ChatPanel` **无 `Update`**；`SocialData.TimeText` 只在建行时算一次。
4. 🟡 **翻译三态视觉缺** —— `ChatPanel.cs:905-916` 点消息只 `Debug.LogWarning`（走远端 `TranslationService`）；字段已留（`SocialData.cs:168`），但**没有 Translating/Translated 的字体材质与 `(翻译中)` 后缀**。
5. 🟡 **输入框不去富文本标签** —— 原版 `inputField.m_OnValueChanged → TextMessageValidationTools.RemoveRichTextTags`（`ChatPanel.cs:154-161`）；我方 `TextInputField.Bind`（`ChatPanel.cs:280`）无这一步。
6. 🟡 **聊天窗按频道打开缺** —— `SocialWindow.cs:273-278` 恒 `Global`；原版走 `ChatPanelContext.startingChannel`（C26）。同理 `DeckInfoPopup.cs` 的 `ShareDeckToChat` 分享后**不开聊天窗**、也不落 Alliance 频道（原版 `DeckInfoPopup.cs:1166-1180` 会 `OpenWindow<ChatPanel>(context)`）。
7. 🔴 **成员行在线只做两档** —— `AllianceMemberTab.cs:1914-1919`：`m.Online` 二选一建 `40K_icon_status_online`（绿）/ `40K_icon_status_offline`（红）。原版是**单颗 Image、三档色**（Away 黄 `(1,0.9216,0.0157,1)` / Connected 绿 `(0,1,0,1)` / 离线**透明**，自己恒绿），**且没有 offline 精灵**。⇒ 缺 Away 档、缺「离线透明」、多了一张原版没有的红点图。数据侧 `SocialData.Member.Online` 是 `bool`（`SocialData.cs:103`），也需补 `Status`。
8. 🔴 **成员行赛季规则缺** —— `SocialData.Member` 无 `season`/`currentSeason` 字段（`SocialData.cs:98-124`）⇒ 原版「`member.season != group.currentSeason` ⇒ 评级显示 0」（A23）无法表达。
9. 🟡 **两个社交页签红点无 provider** —— 全仓无 `AlliancesNotificationProvider` / `FriendsNotificationProvider`（`grep -rn "AlliancesNotificationProvider\|FriendsNotificationProvider"` = 0 命中）⇒ `UiBadgeNotification.Recount()` 恒 0。今天与原版一致（关服后邀请/练习赛请求都取不到），但**结构上永远点不亮**。
10. ⚪ **`SocialData.cs:106-107` 注释过期** —— 写着「颜色值本地读不到 ⇒ 不换色」，但 `AllianceMemberTab.cs:1780` 已用 `IsPlayerEntryColor` 实现（铁律 5：就地改掉）。**不算缺口、只算文档债。**

### 4·3 缺口之外的两条「口径提醒」

- **好友页红点语义**要写进代码注释：`FriendsBadge` = **待处理练习赛邀请**，不是好友申请（S5）。
- **聊天 2 个页签**是我们按 `🔶 自己设计` 落地（`ChatPanel.cs:114-115`）；原版是 `subscribedChannels` 驱动（C1/C2）。本地 `ChatGlobalManager` 是否会在关服下完成 `Initialize`（从而订阅到 Global）**未实测** ⇒ 见 §6·1。

---

## §5 按文件分组的施工建议（调度台按文件切）

| # | 文件 | 要做什么 | 判据（本文件哪一节） | 风险 |
|---|---|---|---|---|
| **A** | `Shell/ChatTab.cs` | ①行高：接 `SocialData.ChatMessage.Height`（没有就用 TMP 实测高 / `CVText` 的换行数算），把"固定 60"改成"至少 60"；②新消息滑入动画 | §1·2 · C15/C16 · §4·2 ①② | 中：`Rebuild` 被断言按行号引用（`BuiltRows`/`ContentBottom`），**只允许类尾追加** |
| **B** | `Shell/ChatPanel.cs`（只改注释 + 类尾） | `TranslationStatus` 三态的视觉（材质 + `(翻译中)`）**暂不做**，但把 `SocialData.cs:166-168` 的字段职责写清、别让后人以为已实现 | C13 | 低 |
| **C** | `Shell/ChatPanel.cs`（类尾追加） | 输入框 `TextInputField` 加 `RemoveRichTextTags` 等价校验（`ChatPanel.cs:280` 的那个回调里串一层） | C·4·2⑤ | 低 |
| **D** | `Shell/AllianceMemberTab.cs` | 成员行状态改成**单颗 Image 三档色**（Away/Connected/离线透明/自己恒绿），并补 `SocialData.Member.Status` | §1·2 · A26 · §4·2⑦ | 中：行 builder 被 5+ 条账按行号引用 |
| **E** | `Shell/SocialData.cs` | `Member` 补 `Season` / `CurrentSeason`（或直接存「该显示的评分数」），把 A23 写进 `RankedRating` 的来源注释 | A23 · §4·2⑧ | 低 |
| **F** | `Shell/SocialData.cs`（注释） | 改掉 `:106-107` 那条过期注释 | §4·2⑩ | 无 |
| **G** | **新文件**（`Shell/SocialBadgeProviders.cs`）+ `Shell/SocialWindow.cs` 一行 | 两个 provider：`AlliancesNotificationProvider`（`groupInvitationsCache.Count`，`IsAlliancesUnlocked && 未入盟` 才出）/ `FriendsNotificationProvider`（`PendingPracticeMatches.Count`）；本地数据源为 0 时**出声说明** | S4/S5 · §4·2⑨ | 低（只接红线同一模式） |
| **H** | `Shell/SocialWindow.cs` / `Shell/ChatPanel.cs` | `OpenChat(ChatRoom)` 支持 `startingChannel`；`DeckInfoPopup.ShareDeckToChat` 落对应频道 | C26 · §4·2⑥ | 中：`ChatPanel` 被账按行号引用 40+ 处 |
| **I** | 断言（`Editor/ShellScene.cs` / `MainMenuScene.cs`：调度台独占） | 补：C6/C7 的 `RowPadLeft/Right` 两值 · F1–F3 三色 · A26 三色 · A28 8 钮显隐矩阵 · A30 的 1.35 | §3 各表 | 低（⚠️ 期望值不许从被测常量读回，自证） |

> ⛔ **本波不要**把 `ChatPanel.Channels`（2 个）改成 1 个：C1 依赖 `ChatGlobalManager` 在关服下是否 `Initialize` 成功，**未实测**（§6·1）⇒ 先查清再动。

---

## §5·5 **不做 / 为什么**（裁定前的默认立场）

> 按 CLAUDE.md §18·0：**只有「服务器相关」才允许自己设计**；其余一律复刻。本节**没有已裁定不做**的项，
> 只列「需要调度台裁定」的三项（都不是我能拍的）。

| # | 项 | 为什么需要裁定 | 若不裁定时的默认做法 |
|---|---|---|---|
| N-1 | **聊天翻译三态视觉** | 判据 `TranslationService` 在远端（关服）⇒ 这是**服务器相关**。可以「本地模拟 → 假装翻译」也可以「保持原文 + 出声」 | 现状（保持原文 + 出声）**符合 §18·0 的「服务器相关才可自设计」**，默认保持；但**三态视觉本身是客户端行为**（材质/后缀），照原版补上并不依赖服务器 ⇒ 建议**补视觉、不做翻译** |
| N-2 | **社交两页签红点 provider** | 数据源（邀请 / 练习赛请求）在服务器；但**计数公式在客户端**（S4/S5） | 建 provider 骨架 + 本地 0 计数出声（不编造计数） |
| N-3 | **`AlliancesConfig.maxMemberCount`** | 远端 config 本地 0 命中，代码默认 **20** | 照代码默认 20 落地，并在账里写「默认值，非远端真值」 |

---

## §6 **未查清**（本文件全部 `?` 与不确定项）

### 6·1 `ChatGlobalManager` 在关服下会不会完成 `Initialize`（决定「1 个页签还是 2 个」）

- 事实：`Initialize()` 无条件 `SubscribeToChannel("Global")`（`ChatGlobalManager.cs:1044-1048`），随后 `ConnectToChatServer()`（`:1060`）；`ConnectToChatServer` 在**组件禁用 / GO 不激活**时会 `LogError` 并走 `IL_00ba` 继续连（`:1063-1108`）。
- `SingletonInitializableBehaviour` 的初始化时机**未读**（是不是需要服务器握手成功）。
- ⇒ **`subscribedChannels` 在关服下可能是 1 条（Global）也可能 0 条**，两者都会让我们「固定 2 个页签」偏离。
- **还差什么**：读 `SingletonInitializableBehaviour` 的 `Initialize` 派发链，或在 player 上 dump `ChatPanel` 的页签数。

### 6·2 `AlliancesConfig` 的远端真值

本地只有 `MonoScript`（`globalgamemanagers/MonoScript/MonoScript_3630.json`），**没有配置资产** ⇒ `maxMemberCount` 只能写「代码默认 20」。**还差什么**：远端 CCD / `d:/2/关服前存档/…/UnityDataAssetPack.apk` 里的 config 包。

### 6·3 `FeatureConfig` 的 `Alliances` 真值

`CheckFeature(key)` 读 `featureStatus` 字典（远端）。某 key **未登记**时的返回支**未逐句读**（我方 `Shell/FeatureChecker.cs:26-33` 记的是「表里没有 ⇒ true」，来源是调度台 2026-10-29 裁定）。**还差什么**：读 `FeatureConfig.CheckFeature` 的**尾段**（`FeatureConfig.cs:329-420` 之后）。

### 6·4 `Friends Container > Viewport` 的 `RectMask2D`

- 原版 `Friends Container > Viewport` 挂 `RectMask2D`（`menu_dump` 只印出字段名 `m_Padding,m_Softness`，**没印值**）。
- 该 bundle 里 `RectMask2D` 有 **150 个实例**，名字大量重名（`Viewport` × 60+）⇒ **无法按名定位**到这一个。
- 我方现按**硬边**建（`FriendsTab.cs` 的 `Hang(container, "Viewport", ContainerR, Vector4.zero, zero)`）。
- **还差什么**：按「GO 链 = Social Submenu Variant › … › Friends Container › Viewport」逐个 pid 反查（`menu_rect --rt` 拿到 pid 后读 `MonoBehaviour_<pid>`）。
- ⚠️ 对照：聊天那一页的软边 **(0,22)** 已经是**逐字实读**的（`ChatTab.cs:36-54`），不要再拿它推广到好友页。

### 6·5 `AllianceRankingRow Variant` 的「名次图标」

`RankingIcon` 在 1/2/3 名时是否换图、`Background` / `BackgroundHighlight` 是否分档 —— AUD02 `:555-583` 只覆盖 `m_Type` / 字号 / `m_characterSpacing`，**没有名次档**。本文件判 `?`。
**还差什么**：读 `AllianceUIRankingRow` 的基类 `UIRankingRow`（已读，**它只做 `bgNormal/bgHighlight` 两态 + `OwnPlayer`**）⇒ 名次图标很可能是**美术统一 + 文本名次**，无分档；需要拿原版实拍确认。

---

## 附录 A · 顺手发现（不属本窗族主结论，但值得记账）

1. 🔴 **`Friends Menu Demo` 不是线上窗** —— `bundle_scenes_scenes_mainmenuwarpforge` 里对它 **0 命中**（`grep -rl "Friends Menu Demo"` = 空）；真正的好友页 = `Social Submenu Variant > Friends Tab`。任务简报里把它列为「好友窗」时要注意：**判据要取 `Social Submenu Variant` 下那棵**，`Friends Menu Demo` 那棵树是 demo（行 58.46 高、单列 VLG、带 `IsPlayerFirsttoggle` 调试开关）。
2. 🔴 **`FriendsBadge` 计数 = 待处理练习赛请求**（`ChatGlobalManager.cs:3101-3110` 的 `PendingPracticeMatches.Count`），**不是好友申请**。写 provider 时别按「申请数」算。
3. 🔴 **成员列表在线色是「单颗 Image + 三档颜色」，没有 offline 精灵**（§1·2）。`Friend Info Item` 才有两张图（`Connected Image` + `Disconnected`）——**两族做法不同**，⛔ 别互相照抄。我方成员行正是照好友行抄的（§4·2⑦）。
4. 🟡 **`ChatMessageRow` 根 pivot = (0.5,0.5)**（`RectTransform_7760131448890879999.json`）⇒ 原版「面板 y 对齐 `message.transform.position.y`」**等于对齐行中心**，我方 `(r.y1+r.y2)*0.5f` 是等价写法，**不是偏离**（这点值得写进注释，否则后人会来"修"）。
5. 🟡 **`AllianceView` 的 `regionText` 与 `AllianceListEntry` 的 `regionText` 不是同一个串**：详情页是 `语言/隐私`（`AllianceView.cs:371-382`），列表行只有语言（`AllianceListEntry.cs:236-244`）。
6. ⚪ **`AllianceMemberEntry` 的 `playerRating` 字段 = 「赛季/排位」那格，`draftRating` = 活动积分**（字段偏移 `+0x40/+0x48`）。我方按名字对到 `Ranked Rating` / `Draft Rating` 节点，语义一致；但**排序用的是 `Rating`（int）**，不是这两个显示串（`SocialData.cs:109-113` 已记）。
7. ⚪ **`UiBadgeNotification` 的补间是 `Time.deltaTime × 3f` 线性趋近 + `|Δ|<0.01` 收尾**（`UiBadgeNotification.cs:161-193`），不是 DOTween 曲线 —— 断言不能按 DOTween 的曲线写。

---

## 附录 B · 覆盖边界（本文件到底查了多少）

| 维度 | 覆盖 |
|---|---|
| **反编译源** | ① `cpp2il/out_cpp2il_dev_ilspy/*.cs`（**主力**）：`ChatPanel` / `ChatTab` / `ChatContentView` / `ChatEntry(View)` / `ChatMessageUI` / `ChatMessageData` / `ChatChannel` / `ChatPlayerOptionsPanel` / `ChatPreview` / `ChatGlobalManager`(节选) / `FriendsTab` / `FriendUIElementWarpforge` / `FriendsData` / `AlliancesTab` / `AllianceSearchTab` / `AllianceMemberTab` / `AllianceMemberList` / `AllianceMemberEntry` / `AllianceListEntry` / `AllianceView` / `AllianceMemberOptionsPopup` / `AlliancesEventScorePanel` / `AllianceTrophiesView` / `AllianceTrophyEntry` / `TrophyInfoPopup` / `AllianceRatingDisplay` / `UIRankingRow` / `AllianceUIRankingRow` / `UiBadgeNotification` / `TransformScalerBySmallScreenUI` / `FeatureChecker` / `AlliancesConfig` / `GroupRole` / `ChatMessageType` / `ChatPlayerStatus` · ② `decomp_full/*.c`（**只在 ilspy 读不出时用**）：`AllianceMemberEntry__Initialize.c` · `ChatPlayerOptionsPanel__ShowOptionsFor.c` · ③ 二进制直读常量（`GameAssembly.dll` 的两条颜色） |
| **预制体** | `ChatPanel` / `Chat` / `ChatMessageRow` / `ChatPreview`（读 AUD02）· `Social Submenu Variant`(深度 5–7) · `Friend Info Item` · `Alliance Member Entry` · `Alliance List Entry` · `Member Options Panel` · `Alliance Trophy Info Popup` · `Invitation List Entry` · `AllianceRankingRow Variant`（读 AUD02） |
| **未读** | `ChatGlobalManager` 其余 ~3000 行（只读了订阅/缓存/通知/离群点）· `AlliancesController`(2768 行，只读了名字与 `CheckFeature` 相关) · `JoinAllianceMenu`(1859 行，只读了 `Initialize`/`FillOpenAlliances`/`Awake`) · `AllianceTrophy`(541 行，只读了 `CurrentTier`/`MaximumTier` 附近) · `CreateAllianceMenu`(256) 全文未读 · `AllianceTrophyEntry` 只读了 `highlight` 相关 |
| **`?` 的数量** | **5 格**：§6·1（关服下页签数）· §6·2（`maxMemberCount` 远端值）· §6·3（`FeatureConfig` 未登记 key 的返回支）· §6·4（好友页 Viewport 软边）· §6·5（名次图标是否分档） |
| **没跑的东西** | Unity 批处理 / 真机 / `typecheck`（只读代理红线，一律没跑） |
| ⚠️ 提醒 | **「本文件把状态查清了」≠「原版这一族全复刻了」** —— 本文件只回答「哪个状态对应哪套参数/显隐」，不回答「我们画得对不对」（那要并排实拍，属调度台验收）。§4·2 的 10 条才是可施工的缺口清单。 |
