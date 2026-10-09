# Shell/ 未走 Loc 的玩家可见文案（只读盘点）

> ⚠️ **落盘说明**：本文件由**主对话代为落盘** —— 做这份普查的只读代理（`Explore`）工具集里没有 Write/Edit，
> 它把全文交回了主对话。内容**逐字照它的交件**，只有这一行说明是后加的。
> 判据：`CardPresentation/Shell/*.cs` 全 **81** 个文件。方法 = 剥注释 → 剥 `Debug.Log*` / `Say` / `Note` /
> `Persist` / `chk` / `near` / `Check*` / `BriefState` 等开发汇（括号配对，含跨行块）→ 余下的中文/英文字面量逐条判。
> `Loc.T` 调用数一并核过（`CollectionWindow` 15 · `AlliancesTab` 15 · `SettingsWindow` 10 …）。

## 一、逐文件表

| 文件 | ①处数 | ②处数 | ③处数 | 主要落点（`行号: 字面量` 最多 3 条） |
|---|---|---|---|---|
| SettingsWindow.cs | 12 | 141 | 0 | `1410: 确定要退出游戏吗？` · `1410: 退出` · `2533: 三条路，从最省事开始：` |
| LiveOpsEventWindow.cs | 11 | 62 | 0 | `495: 这套卡组还没有战将 —— 去卡组编辑里选一个再来。` · `922: 这套卡组还没有选战将，开不了局。` · `243: 遭遇战（Skirmish · 12 张）` |
| PracticeModePopup.cs | 7 | 63 | 0 | `561/1459/1482: 未选战将` · `1611: 这套卡组还没有选战将，开不了局。` · `1671: 已经取消这一局的联机匹配 …` |
| DeckInfoPopup.cs | 7 | 75 | 0 | `836: 未选战将` · `1265: 这套卡组里有隐藏卡，开不了练习赛。` · `1369: 聊天窗**发不出消息**…` |
| DeckSelectionPopup.cs | 4 | 18 | 0 | `692: 预组卡组的数据读不到（Resources/prebuilt_decks.json）⇒ 先如实留空；` · `695: 这一页一副可用的都没有…` · `697: 没有可选的卡组` |
| MainMenuRuntime.cs | 3 | 27 | 0 | `716: 确定要退出游戏吗？` · `718: 退出游戏` · `720: 取消` |
| ShopWindow.cs | 2 | 42 | 0 | `791: 确定` / `791: 取消`（`ShowPopUp(LegendaryWarnText, …)` 的两个钮文案） |
| PlayerProfileWindow.cs | 2 | 8 | 0 | `741: 「…」的档案` · `745: 服务器数据 —— 本地版只有你自己那一份…` |
| LeaderboardWindow.cs | 2 | 16 | 0 | `684: 上一赛季的榜单在服务器上。…` · `685: 知道了` |
| RankedEventWindow.cs | 2 | 13 | 0 | `183: 排位赛需要服务器连接。…` · `184: 知道了` |
| ShopData.cs | 1 | 4 | 0 | `297: 你已经有 1 张传奇品质的这一件了。\n确定还要再买一张吗？` |
| CollectionData.cs | 1 | 28 | 0 | `新卡组`（默认卡组名）—— 🔴 **2026-10-09 现核订正**：原来写 `Lib.UniqueName("新卡组")`（写死字面量），**现在已走词条**：`Lib.UniqueName(Loc.T("MenuDeck/NewDeckName"))`（⛔ 按符号找，别按行号） |
| ProfileData.cs | 1 | 3 | 0 | `142: 玩家123`（`const DefaultPlayerName`） |
| 其余 63 个文件 | 0 | 见「四」 | — | 无（余下中文全在 `Debug.Log*` / `Say` / `BriefState` / 自检断言 / `_flash` 里） |

**① 合计 = 55 处**（13 个文件）；**② 约 940+ 处**（81 个 `.cs` 里 76 个含中文，绝大多数是 `Debug.Log`/`Say`/`BriefState`）；**③ 在 Shell 内几乎为零**。

## 二、① 类逐条清单（这是写手的活）

| # | 文件:行号 | 当前字面量（原文照抄） | 建议的词条键名 | 为什么建议这个键名 | 该键在 `Loc.cs` 里**在不在** |
|---|---|---|---|---|---|
| 1 | DeckInfoPopup.cs:836 · PracticeModePopup.cs:561,1459,1482 | `未选战将` | `MenuDeck/Error/NoWarlord` | `Loc.cs:904` 已有（`还没有选战将` / `No warlord selected yet`），语义一致 ⇒ **复用，别新造** | **在** |
| 2 | DeckInfoPopup.cs:1175,1265,1372 · LeaderboardWindow.cs:685 · LiveOpsEventWindow.cs:915,922 · PracticeModePopup.cs:1611 · RankedEventWindow.cs:184 · SettingsWindow.cs:1399,2558 | `知道了` | 🔴 **`MainMenu/General/OK`**（**大写 `OK`** —— ⛔ 不是 `Ok`） | 🔴 **2026-10-09 现核订正（`A1017` ①）**：原写 `MainMenu/General/Ok` ⇒ **原版只有大写拼法**（三条独立判据，见 `A1017` ①）；且**原文不是「自拟」** —— 原版**真有**这颗 `Localize.mTerm = "MainMenu/General/OK"`，EN 列 = 逐字符 `OK`，ZH 列 `确定`（**自拟**） | 🔴 **在**（原写「不在」已过期） |
| 3 | SettingsWindow.cs:1410 · MainMenuRuntime.cs:716 | `确定要退出游戏吗？` | `MainMenu/Settings/ExitGame/Confirm` | 同族 `MainMenu/Settings/ButtonLabel/Exit_Game` 已在表 ⇒ 沿该族形状；原版键未查 ⇒ **自拟** | 不在 |
| 4 | MainMenuRuntime.cs:718（`退出游戏`）· SettingsWindow.cs:1410（`退出`） | `退出游戏` / `退出` | `MainMenu/Settings/ExitGame/Ok` | 两条同义（一处是简称）⇒ 建议**合并成一条键**；**自拟** | 不在 |
| 5 | ShopWindow.cs:791 | `确定` | `MainMenu/General/Confirm` | 表里已有（`确认`/`Confirm`）⇒ **复用**（⚠️ 中文列是「确认」不是「确定」，要按同一串还是另立，**需裁决**） | **在** |
| 6 | MainMenuRuntime.cs:720 · ShopWindow.cs:791 | `取消` | `MainMenu/General/Cancel` | 对照**已在表**的 `MainMenu/General/Confirm` 造同族；**自拟** | 不在 |
| 7 | ShopData.cs:297 | `你已经有 1 张传奇品质的这一件了。\n确定还要再买一张吗？` | `MenuShop/ExtraLegendaryWarning` | **键名有出处**：`ShopWindow.cs:785` 注释自己写着 `MenuShop/ExtraLegendaryWarning`（英文原文在远端 I2，本地没有）⇒ 键名可照抄 | **不在**（`Loc.cs` 尚未收录） |
| 8 | LiveOpsEventWindow.cs:922 · PracticeModePopup.cs:1611 | `这套卡组还没有选战将，开不了局。` | `MenuDeck/Error/NoWarlord` | 同 #1，**复用** | **在** |
| 9 | LiveOpsEventWindow.cs:495 | `这套卡组还没有战将 —— 去卡组编辑里选一个再来。` | `MenuDeck/Error/NoWarlord` 或自拟 `MenuDeck/HUD/NoWarlordText` | 语义同 #1，但这条是**整句**（原版 `No Deck Text`）⇒ 保整句就自拟 | **在**（键在，口径不同，**需裁决**） |
| 10 | LiveOpsEventWindow.cs:992-993 · PracticeModePopup.cs:1671 | `已经取消这一局的联机匹配 —— 对面会收到通知，**双方都没有开局**。\n想再打一次：两边各自重新点一次 \`Battle!\`。` | `Settings/Online/MatchCancelled` | 这一族（联机页）**原版没有**，`Loc.cs` 里 `Settings/Online/Title` 就是自拟先例 ⇒ **自拟** | 不在 |
| 11 | LiveOpsEventWindow.cs:998 · PracticeModePopup.cs:1676 | `取消不了这一局：` | `Settings/Online/MatchCancelFailed` | 同上，**自拟** | 不在 |
| 12 | LiveOpsEventWindow.cs:243 | `遭遇战（Skirmish · 12 张）` / `经典（Classic · 30 张）` | `MenuDeck/GameMode/Skirmish` · `MenuDeck/GameMode/Classic` | 同族已有 `MenuDeck/*`；原版键未查 ⇒ **自拟** | 不在 |
| 13 | LiveOpsEventWindow.cs:280-281,291-294 | `这副预组是「…」的，不能用在…里 —— 换一副。` · `「…」是「…」的卡组，不能用在…里 —— 换一副，或点 \`Create deck\` 建一副新的` | `MenuDeck/Error/WrongGameMode`（+ #12 两条模式键做占位符） | 带占位符的整句 ⇒ 换 key 时要一起改成 `string.Format`；**自拟** | 不在 |
| 14 | LiveOpsEventWindow.cs:287 | `还没有可用的卡组 —— 先点 \`Create deck\` 建一副` + `的。` | `MenuDeck/Error/NoDeckForMode` | 同族已有 `MenuDeck/Error/TooFewCards` 等 ⇒ 沿形状；**自拟** | 不在 |
| 15 | DeckInfoPopup.cs:1265 | `这套卡组里有隐藏卡，开不了练习赛。` | `MenuDeck/Error/HiddenCards` | 沿 `MenuDeck/Error/*` 族；**自拟** | 不在 |
| 16 | DeckInfoPopup.cs:1369 / 1371 | `聊天窗**发不出消息**（原版走服务端，我们这条线没有网络）。…` · `原版是**平台分享**。…这是这一副的卡组串，可以自己复制：\n` | `MenuDeck/Share/ChatUnavailable` · `MenuDeck/Share/PlatformShare` | 同族已有 `MenuDeck/Share/PasteDeck` ⇒ 沿形状；**自拟** | 不在 |
| 17 | SettingsWindow.cs:1395-1396 | `兑换码要走原版的服务器（\`GeneralTab.RedeemCode\` → \`TryRedeemCode\` → 远端校验），这个项目没有那台服务器 ⇒ **这里兑不了**。` | `Settings/General/RedeemCodeUnavailable` | `Settings/General/RedeemCode` 已在表 ⇒ 这是它的说明句；**自拟** | 不在 |
| 18 | SettingsWindow.cs:2293 | `音量走 AudioMixer（与对局内设置面板同一套）` | `Settings/Media/AudioMixerNote` | `Settings/Media/VoiceOvers` 已在表 ⇒ 沿族；**自拟** | 不在 |
| 19 | SettingsWindow.cs:2327-2328 | `这一页不是原版（原版是联网游戏，没有「当主机」这回事）。\nIP 直连 —— 公网怎么走 / 路由器要不要放开端口：点这一行看` | `Settings/Online/TitleNote` | 沿自拟族 `Settings/Online/*` | 不在 |
| 20 | SettingsWindow.cs:2369-2377 | `外网看到的地址（刚探的）：…` 整块（8 条串，含 `（没探到）`/`没有`/两个 `t +=` 追加） | `Settings/Online/PublicAddress/*`（建议拆 6~8 条） | 自拟；`ShowPopUp` 用 | 不在 |
| 21 | SettingsWindow.cs:2533-2555 | `三条路，从最省事开始：…` 整块（**23 条串**，最长的一块） | `Settings/Online/HowToConnect/*`（建议拆条） | 自拟；`ShowPopUp` 用 | 不在 |
| 22 | SettingsWindow.cs:2572 | `（会话还没建 —— 点一下 Host 的保存，或 Client 的检查连接）` | `Settings/Online/StatusNoSession` | `_statusLabel.SetText(...)` ⇒ **真上屏**；自拟 | 不在 |
| 23 | SettingsWindow.cs:2415 | `例如 192.168.1.10` | `Settings/Online/IpPlaceholder` | 输入框 placeholder，玩家可见；自拟 | 不在 |
| 24 | SettingsWindow.cs:2418 | `留空 = 不校验` | `Settings/Online/PasswordPlaceholder` | 同上；自拟 | 不在 |
| 25 | LeaderboardWindow.cs:684 | `上一赛季的榜单在服务器上。\n本地版没有赛季数据，所以这里只能看看界面。` | `MainMenu/RankedWindow/LeaderboardOfflineNote` | 沿已在表的 `MainMenu/RankedWindow/Leaderboard`；**自拟** | 不在 |
| 26 | RankedEventWindow.cs:183 | `排位赛需要服务器连接。\n本地版没有联机（将来做 P2P），所以这里只能看看界面。` | `MainMenu/Ranked/OfflineNote` | 沿已在表的 `MainMenu/Ranked/GoToCreateDeck`；**自拟** | 不在 |
| 27 | PlayerProfileWindow.cs:741 | `「` + who + `」的档案` | `MainMenu/Social/ProfileTitle` | 自拟 | 不在 |
| 28 | PlayerProfileWindow.cs:745 | `服务器数据 —— 本地版只有你自己那一份（原版这一页由服务器填）` | `MainMenu/Social/ProfileOfflineNote` | 自拟 | 不在 |
| 29 | DeckSelectionPopup.cs:692-693 | `预组卡组的数据读不到（Resources/prebuilt_decks.json）⇒ 先如实留空；跑 \`python 工具/gen_prebuilt_decks.py\` 重新生成` | `MenuDeck/Error/PrebuiltMissing` | ⚠️ 这条**写着脚本路径**、是开发者向的 ⇒ 也可判 ②；若判 ① 则自拟 | 不在 |
| 30 | DeckSelectionPopup.cs:695 | `这一页一副可用的都没有（**拼不齐的按原版口径整副不显示**）` | `MenuDeck/Error/NoUsablePrebuilt` | 自拟 | 不在 |
| 31 | DeckSelectionPopup.cs:697 | `没有可选的卡组` | `MenuCollection/NoDecksFound` | 表里最近邻（`没有符合当前筛选的卡组` / `There are no deck …`）⇒ 可**复用** | **在** |
| 32 | CollectionData.cs:260 | `新卡组` | 🔴 **`MenuDeck/NewDeckName`** | 这是**新建卡组的默认名**（会显示、玩家可改）⇒ 不宜复用 `MenuDeck/MenuButtons/CreateDeck`（那是钮文案）。🔴 **2026-10-09 现核订正（`A1017` ②）**：原来写 `MenuDeck/HUD/NewDeckName` —— **本工程已统一成 `MenuDeck/NewDeckName`**（三处消费点同一条键：`Deck/DeckEditorState.cs` 的 `NewDeck` · `Deck/DeckRuntime.cs` 的 `SetDeckName` 那一处 · `Shell/CollectionData.cs` 的 `CreateDeck`，⛔ 按符号找，别按行号） | **在**（原写「不在」已过期） |
| 33 | ProfileData.cs:142 | `玩家123` | `MainMenu/Profile/DefaultPlayerName` | 默认显示名（`DefaultPlayerName`），上屏 ⇒ ①；若按「玩家名」判也可归 ③，**需裁决** | 不在 |

> 🔴 **2026-10-09 现核订正（铁律 5 · `A1017` 之后的读数）** —— **本表下面这几行也已过期，写手别再照抄**：
>
> - **#3 / #4**：`MainMenu/Settings/ExitGame/{Confirm,Ok}` ⇒ 那是**波 1 当时的自拟键**，已被 `A1026` 裁定换成
>   **原版真键** `Demo/MainMenu/{ExitGame,ExitButton}`（`stringliteral.json` 各 1 条）⇒ 这两行的「建议键名」作废。
> - **#6**：`MainMenu/General/Cancel` 现在**在表**（原写「不在」）。
> - **#9**（`LiveOpsEventWindow` 那条整句）：`A1034` **已裁 = 保整句**，整句键 **`MenuDeck/HUD/NoWarlordText` 已建**；
>   短键 `MenuDeck/Error/NoWarlord` **仍留给那 4 处「未选战将」**。⇒ 本行那个「**需裁决**」**已不成立**。
> - **#33**：`MainMenu/Profile/DefaultPlayerName` 的「**需裁决**」**已裁**（`A1047`：**翻**，与 `A1037` 同一口径）。
> ⚠️ 本表所有**行号都是 2026-10-18 的读数**，而 `P1`–`P4` 已落地一部分 ⇒ **一律按符号名现读**
> （⛔ 别把漂掉的行号再抄一遍、也别制造新的死行号）。

## 三、本目录里「与语言无关但会显示」的坑

- 已有英文占位常量**直接上屏**、换语言不会变：`SearchingOpponentWindow.cs:57 PlaceholderName="Player name"` · `ProfileTab.cs:339 PlaceholderName="Player Name"/PlaceholderTitle="Player Title"` · `AvatarTab.cs:83 BtnLabel="Select"` · `TitleTab.cs:66 BtnLabel="Select"` · `AllianceEventScorePanel.cs:48 PlaceholderName="Alliance Name"`。
- prefab 出厂**俄语**占位串会显示：`BoosterPackOpenWindow.cs:174 NewBadgeFactoryText="Новинка!"` / `:178 BannedFactoryText="Запрещено"`（照口径属 ③）。
- `SettingsWindow._flash`（`:703`）**不画到屏上** —— 只被 `Editor/SettingsScene.cs` 的 `CheckTrue(win.Flash.Contains("画质"))` 一类自检读；但**改这批中文会让 5+ 条自检变红**（`Editor/SettingsScene.cs:997 / 1285 / 1458 / 1505 / 1607 / 2678` 按中文子串断言）。同类：`DailyData.LastResetMessage` / `CollectionData.LastSelectError` / `CampaignData`+`ForgeData` 的 `why`。
- 按 `Loc.Current` 拼串：`SettingsWindow.cs:1372-1373`（`"语言 → " + Loc.LanguageName(...) + (Loc.HasOwnText(...) ? "" : "　⚠️ 本地没有这一套文案 ⇒ 界面文字**回退英文**")`）—— 后半句若上屏会是语言相关的 ①；实测只在 `_flash` 里 ⇒ 目前 ②。
- 显示的是玩家名 / 卡名 / 数据读进来的名字：`DeckSelectionPopup` 的 `raw.Name`、`PlayerProfileWindow` 的 `who`、`LiveOpsEventWindow` 的 `wl.Name`、`MainMenuRuntime.CurrencyNames(...)` ⇒ ③，**别进表**。

## 四、没查清的部分

- 原版 mTerm **一个都没查**（按口径不解析 prefab）⇒ 除第 7 条（代码注释自带）外，第二节所有「建议键」都是**自拟**。
- `_flash` 是否真**从不上屏**：`grep -rn "_flash"` + `grep -rn "\.Flash"` 全工程 → Shell 内只有 `Flash` 属性与 `Debug.Log` 两处消费者，唯一外部消费者在 `Editor/SettingsScene.cs`。若将来有人把 `Flash` 画上去，那 ~15 条立刻变 ①。
- 大票 `sb.Append("…")` 摘要串（`RewardWindow.cs:1506-1517` · `BoosterInfoPopup.cs:635-640` · `CampaignRewardWindow.cs:1023-1035` · `CampaignTab.cs:1095-1098` · `ForgeTab.cs:953-956` · `CollectionWindow.cs:3158-3161` · `OfferContainer.cs:1395-1396` · `DeckSelectionPopup.cs:728-731` · `ShellRuntime.cs:546-551` · `WindowsManager.cs:1604-1608` · `RewardWindow` / `RewardsWindow` / `ShopWindow:193` / `DailyRewardPopup:524` / `InboxWindow:590` / `ShellParts:64` / `ViewportClip:155`）：我判 ②（最终由 `Debug.Log(...)` 输出），但**没有逐条追到那句 `Debug.Log`**（靠 `BriefState`/`Last*`/`Summary` 命名 + 相邻日志判定）⇒ 其中若有被 `Text(...)` 消费的，**需复核**。
- `ShopData.cs:297` 的键名 `MenuShop/ExtraLegendaryWarning` 是从 `ShopWindow.cs:785` 的**注释**里读到的，**没在** `Core/Loc.cs` 或资源里核过。
- 没扫 `Shell/` 之外的目录（任务范围限 `Shell/`）；`TMP_Dropdown` 选项只扫到 `SettingsWindow` 语言下拉（走 `Loc.LanguageName`，已覆盖），别处没扫到 `AddOption` + 中文字面量。
