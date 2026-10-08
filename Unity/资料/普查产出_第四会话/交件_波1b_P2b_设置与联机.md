# 交件 · 双语③ 波 1b · P2b（设置 + 联机）—— 「波 0b 补完键」后的机械接线

> 白名单（只碰这 6 个）：`Shell/{SettingsWindow,MainMenuRuntime,ProfileData,LeaderboardWindow,RankedEventWindow,PlayerProfileWindow}.cs`
> 判据 = `交件_波1_P2_设置与联机.md` §④ + `交件_波0b_补并集键.md` §②/§③ + `施工单_双语③逐处换key.md` §③/§④ + **现读**。
> ⛔ 没跑 Unity、没碰 git、没改 `Editor/*`、**没动 `Core/Loc.cs`**、没改任何 `资料/*.md`。
> 🔴 **一律裸 `Loc.T(k)`**，⛔ 一个 `HasEntry ? … : 原串` 兜底都没留（键已全在表，兜底 = 静默失败）。

## ① 结论

- ✅ **P2 §④ 那张表 17 行，落地 16 行**（只差 `ProfileData.DefaultPlayerName` 一行 —— **等裁决**，见 ⑥·1）。
  合计换掉 **`SettingsWindow.cs` 47 处 / 42 条键**（含 4 处 `string.Format` 占位）、
  `LeaderboardWindow` 1 · `RankedEventWindow` 1 · `PlayerProfileWindow` 2 · `MainMenuRuntime` 3 条键名改指。
- ✅ **`A1026` 照办**：退出窗那三条**全部改指原版键** `Demo/MainMenu/*`（见 ③）。
- ✅ **秒级类型检查**：`TMPDIR=/tmp/wf_p2b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**。
- ✅ **行尾**：`git diff --numstat` = `SettingsWindow 121/57 · MainMenuRuntime 21/4 · LeaderboardWindow 6/2 ·
  RankedEventWindow 8/2 · PlayerProfileWindow 7/2`（**不是整篇重写**）；`SettingsWindow.cs` 实测 **CRLF 0 / LF 3115**（纯 LF，没翻）。
- ⚠️ **中文档零变化**：所有键值都是**逐字**照调用点原文建的（波 0b 干的）⇒ 本波只换「从哪取字」。
  **唯一会变的是英文档**（原来印中文，现在印英文）。
- 🔴 **联机页那 8 颗写死英文钮：表里【一条键都没有】⇒ 本波没改**（不许自造键）。逐颗见 ④。

## ② 逐处改动表（文件:行号 = **改后**行号；原行号见 `交件_波1_P2…md` §④）

| # | 文件:行号 | 原字串（首句） | 改成 key | 依据 |
|---|---|---|---|---|
| 1 | `SettingsWindow.cs:1461` | 兑换码要走原版的服务器（…） | `Settings/General/RedeemCodeUnavailable` | 波 0b §② 第 25 行（自拟，两张表 0 命中） |
| 2 | `SettingsWindow.cs:2359` | 音量走 AudioMixer（…） | `Settings/Media/AudioMixerNote` | 波 0b §② 第 26 行 |
| 3 | `SettingsWindow.cs:2375` | 页标题 `"Online"` | `Settings/Online/Title` | **表里早就有**（`Loc.cs:187`，ZH 联机 / EN Online） |
| 4 | `SettingsWindow.cs:2395` | 这一页不是原版（…）+ IP 直连 —— … | `Settings/Online/{TitleNote,TitleNoteBody}` | 波 0b §② 第 27–28 行 |
| 5 | `SettingsWindow.cs:2415` | 正在探测「外网看到的地址」… | `Settings/Online/ProbingPublicAddress` | 波 0b 第 29 行 |
| 6 | `SettingsWindow.cs:2439-2442` | 外网看到的地址（刚探的）：/ · IPv4：/ · IPv6：/ **（没探到）** / 本机网卡上的公网 IPv6：/ **没有** | `…/PublicAddress/{Title,V4,V6,NotFound,LocalV6,None}` | 波 0b 第 30–35 行（`NotFound` 是波 0b 从 P2 的 `RouterNat*` 里**拆出来**的） |
| 7 | `SettingsWindow.cs:2444/2446` | ⚠️ 两个不一样 → / ✅ 两边都有公网 IPv6 → | `…/PublicAddress/{Mismatch,BothOk}` | 波 0b 第 36–37 行（**两处合并后的名字**，以波 0b 为准） |
| 8 | `SettingsWindow.cs:2484` | 例如 192.168.1.10 | `Settings/Online/IpPlaceholder` | 波 0b 第 38 行 |
| 9 | `SettingsWindow.cs:2487` | 留空 = 不校验 | `Settings/Online/PasswordPlaceholder` | 波 0b 第 39 行 |
| 10 | `SettingsWindow.cs:2509-2512` | ✅ 主机已就绪…/把这行给朋友 → /（没设密码）/ 主机没起来：/ NetRuntime 不在 | `…/{HostReady,HostReadyToFriend,HostReadyNoPassword,HostFailed,NetRuntimeMissing}` | 波 0b 第 40–44 行 |
| 11 | `SettingsWindow.cs:2524` | NetRuntime 不在（自检里要自己建） | `Settings/Online/NetRuntimeMissing` | 同上（**与 #10 末条同键** —— 两处本就是同一句，原文一长一短，现**统一取长的那版**） |
| 12 | `SettingsWindow.cs:2569` | ⚠️ 一块可用网卡都没找到 —— … | `Settings/Online/NoNicFound` | 波 0b 第 45 行 |
| 13 | `SettingsWindow.cs:2578` | `本机地址 {n}/{m}：{label}` | `Settings/Online/LocalAddr`（`string.Format`，3 参） | 波 0b 第 46 行 |
| 14 | `SettingsWindow.cs:2579-2581` | （IPv6） / 　—— 再点一下换下一个 / 虚拟网卡那两句 | `Settings/Online/{IsV6,ClickAgain,VirtualNic}` | 波 0b 第 47–49 行（`VirtualNic` 是 P2 的 `VirtualNic`+`VirtualNicBody` **合并后**的名字） |
| 15 | `SettingsWindow.cs:2606-2619` | `ShowHowToConnect()` 整块（原 23 字面量） | `Settings/Online/HowToConnect/{Intro,Lan,VirtualLan,PublicDirect,DontUseTestSite,NoHolePunching,LocalCheckTitle,PublicV6Yes,PublicV6No,VirtualNicYes,VirtualNicNo,UpnpNote}`（12 条） | 波 0b 第 51–62 行（**按句拆成 12 条**，值是逐字拼接的产物） |
| 16 | `SettingsWindow.cs:2637` | （会话还没建 —— …） | `Settings/Online/StatusNoSession` | 波 0b 第 50 行 |
| 17 | `LeaderboardWindow.cs:688` | 上一赛季的榜单在服务器上。… | `MainMenu/RankedWindow/LeaderboardOfflineNote` | 波 0b 第 63 行 |
| 18 | `RankedEventWindow.cs:189` | 排位赛需要服务器连接。… | `MainMenu/Ranked/OfflineNote` | 波 0b 第 64 行（⚠️ 值**仍带那句已过期的「（将来做 P2P）」**，见 ⑥·2） |
| 19 | `PlayerProfileWindow.cs:746` | `「{who}」的档案` | `MainMenu/Social/ProfileTitle`（`string.Format`，1 参） | 波 0b 第 65 行（**带占位符** ⇒ 走 `Format`，⛔ 不用 `Replace`） |
| 20 | `PlayerProfileWindow.cs:751` | 服务器数据 —— 本地版只有你自己那一份… | `MainMenu/Social/ProfileOfflineNote` | 波 0b 第 66 行 |
| 21 | `SettingsWindow.cs:1014-1058` | ——（新增 42 条 `const lk*` + 出处注释） | —— | 键名不许在代码里写第二遍 |

**两处默认值上的口径差（如实记，均按波 0b 的表取值）**：① `HostFailed` 那条，原来的兜底是短版
`"NetRuntime 不在"`，现统一成长版 `"NetRuntime 不在（自检里要自己建）"`（**中文档这一处会多出括号那半句**）；
② 其余全部逐字相同。

## ③ `A1026` 那三条改指了什么

| 位置 | 波 1 用的自拟键 | **改成（原版键）** | 值 |
|---|---|---|---|
| `SettingsWindow.cs:1008` / `:1475` | `MainMenu/Settings/ExitGame/Confirm` | **`Demo/MainMenu/ExitGame`** | 确定要退出游戏吗？/ Are you sure… |
| `SettingsWindow.cs:1009` / `:1475` | `MainMenu/Settings/ExitGame/Ok` | **`Demo/MainMenu/ExitButton`** | 退出游戏 / Exit game |
| `MainMenuRuntime.cs:720` / `:737` | `MainMenu/General/Cancel` | **`Demo/MainMenu/CancelButton`** | 取消 / Cancel |

- 判据 = `d:/2/tools/il2cpp_out/stringliteral.json` 里 `Demo/MainMenu/{ExitGame,ExitButton,CancelButton}` 各 1 条，
  且与 `MainMenuRuntime.cs` 那段老注释逐字吻合；**两张表都搜过才叫查过**（只搜 `assets_full` 会得出假「原版没有」）。
- ✅ **`MainMenuRuntime` 那三条仍是 `static` 属性**（不是 `const`/`static readonly`）—— 属性每次现读 ⇒ 换语言后新弹的窗当场就是新语言。
- ⚠️ 自拟键 `MainMenu/Settings/ExitGame/{Confirm,Ok}` **仍留在 `Loc.cs` 表里**（本波无权删）⇒ 现在是**两条同值空键**，建议波 2 或后续清掉（**只报告**）。

## ④ 联机页写死英文钮 —— 逐颗查表结果（**8 颗，表里 0 条**）

| 颗 | 文件:行号 | 原字串 | 在表？ |
|---|---|---|---|
| 角色钮 ×2 | `SettingsWindow.cs:2378/2379` | `"Host"` / `"Client"` | ❌ 不在 |
| 测外网钮 | `:2413` | `"Test Public IP"` | ❌ 不在 |
| 标签 ×2 | `:2474/2476` | `"IP address"` / `"Password"` | ❌ 不在 |
| 刷新钮 | `:2497` | `"Refresh"` | ❌ 不在 |
| 保存钮 | `:2501` | `"Save"` | ❌ 不在 |
| 检查连接钮 | `:2519` | `"Check Connection"` | ❌ 不在 |

> 查法：`Core/Loc.cs` 全表逐键搜这 8 个串（含 `EnOf` 英文列）⇒ **0 命中**。⚠️ 唯一沾边的是
> `Settings/Online/Title`（EN `Online`）—— 那是**页标题**，已用在 #3；⛔ **不是**这几颗钮的键，没拿它顶替。
> ⇒ 联机页**仍半中半英**；要收口得先**建 8 条键**（下一位写手 + `Loc.cs` 动作）。

## ⑤ 仍缺的键（**只报告，没自造**）

1. `Settings/Online/*` 那 8 颗钮（见 ④）—— 波 0b 的 P2 那 46 条里确实**不含**。
2. **`Net/NetRuntime.cs:109 / :138`** 仍直传 `"知道了"`（P2 交件 §⑦·2 已上报过一次；**不在本波白名单**，也没在施工单四批名单里）。

## ⑥ 没做到 / 拿不准

1. 🔴 **`ProfileData.cs:142` `DefaultPlayerName = "玩家123"` 没改 —— 等裁决**。
   波 0b 已建键 `MainMenu/Profile/DefaultPlayerName`（`Loc.cs:1205`），但**两条判据打架**：
   ① 它是**玩家可见的占位名**（和 `FriendsTab` 的 `Demo/FriendsMenu/EnterPlayerName` 同类，那条已进表）；
   ② 施工单 §③ 明写 **「玩家名 … 运行时值，不翻」**，且它**流进联机握手**（`NetSession.PlayerName()` →
   发给对面）+ `NetMatchmaking._myName` 缓存一次 ⇒ 中途换语言会两边不一致。
   ⛔ **两条判据对撞 = 停手，不自造口径**。要做只需三行（`const` → `static` 属性；全仓消费点只有 `:133` 一处）。
2. ⚠️ **`MainMenu/Ranked/OfflineNote` 的中/英两列里那句「（将来做 P2P）」已过期**（P2P 2026-09-26 就完了）。
   接线照做了（#18），但**值在 `Core/Loc.cs:1197`、不在本波白名单** ⇒ **文案没改**，⚠️ **等 `Loc.cs` 的写手订正**
   （两列都要改：ZH「（将来做 P2P）」→ 应说「本地没有联机」；EN 同）。
3. ⚠️ **`Settings/Online/*` 那批原文带 `**加粗**` 星号**（波 0b 逐字照抄）。**`ShowPopUp` 会不会把星号真印出来
   仍然没核**（P2 §⑤ 与波 0b §⑦ 都提过、都没查）。本波**照键取值**，没在文案里删星号。
   要查的真判据 = `WindowsManager.ShowPopUp` 走的那个文本组件是不是 TMP rich text（`<b>` 才加粗，`**` 不会）。
4. ⚠️ **联机页 / 音频页那几件是 `Build()` 里建一次的**（页标题、说明行、输入框占位、`_flash`）
   ⇒ **开着窗换语言不会当场刷新**（`RefreshTexts()` 只覆盖 `_genLabels` / `_tabLabels` 两条表，
   见 `SettingsWindow.cs:1228`）。原版这批本来就是我们新加的，**不算偏离原版**，但属「可见的粗糙」——
   **需要一条刷新链**（照 `_genLabels` 的形状加 `_onLabels`），**本波没做**（超出「换 key」范围）。
   注：`ShowHowToConnect()` / `EchoText()` / `RefreshLocalIp()` / `RedeemCode()` 都是**点/算的那一刻**取值 ⇒ 这三处没这问题。
5. ⛔ **本轮没写断言**（按 brief ⑥）。**下一波需要**：宿主 **`Editor/SettingsScene.cs`**（联机页 + 兑换码 + 退出确认）
   与 **`Editor/MainMenuScene.cs`**（退出窗三句）。
   要加：① 逐条 `Loc.HasEntry(键)`（**42 条键全断**，坑表 #18）② **两语档各断一次**（`Loc.RestoreForTest` 换档 ⇒
   `ShowHowToConnect` / `EchoText` 的产物必须跟着变）③ **灭自证 C1**：`zh != en && !Loc.HasCjk(en)`；
   ④ `MainMenuRuntime.ExitGameText` 是**属性** ⇒ 断言里**每次现读**（⛔ 别开头抓一次存起来）；⑤ `Loc.EntryCount >= 302`。

## ⑦ 顺手发现的（⛔ 一处都没顺手改）

1. ⚠️ **左栏页签 `Online` 那颗仍是英文**：`SettingsWindow.cs:902` 的 `Key = (string)null` ⇒ 照 `Label` 原样画。
   而 `Settings/Online/Title`（ZH 联机）**现在已被页标题消费**（#3）⇒ 语义上**同一条键可同时喂页签**（先例 =
   `Settings/General/Title` 既当页签又当页标题）。⛔ **本波没动它**（不在 P2 §④ 表里、页签属整窗左栏）—— **只报告**，改是一行。
2. ⚠️ **`_flash` 那批开发者串确认「不上屏」**（复现了 P2 的结论）：`SettingsWindow.cs:1436`（`"语言 → "`）·
   `:1815/1823/1834/1857/1879/2224`（画质档 / VSync / Small Screen UI / Auto Zoom / super sampling / 帧率上限）
   —— 消费者只有 `Editor/SettingsScene.cs` 的断言与 `Debug.Log` ⇒ 照 P2 ③ **没当文案翻**。**联机页的 `_flash` 是例外、确实上屏**（`:2638 RefreshOnline → _statusLabel.SetText`），那批已全换。
3. ℹ️ 本波做「残留中文扫描」（6 个文件、逐行取字符串字面量、剔 `Debug.*` 与注释行）⇒ **玩家可见的中文只剩
   `ProfileData.cs:142` 一条**（= ⑥·1 那条待裁）。其余命中全是 `Debug.Log` 的续行 / ② 类 `_flash` / `BriefState`。
4. ℹ️ `SettingsWindow.cs:993` 的 `lkExitGame = "MainMenu/Settings/ButtonLabel/Exit_Game"`（**通用页那颗「退出游戏」钮**）
   与 `Demo/MainMenu/ExitButton`（**退出确认窗的确定钮**）是**两颗不同的东西**，键名不同是**对的**，⛔ 别合并。
