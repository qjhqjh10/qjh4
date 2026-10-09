# 施工单 · 双语 ③「逐处换 `T(key)`」（只读盘点 · 2026-10-18）

> 判据来源：`项目任务.md` §三 第 23 条 · `资料/历史/A表已收口_1017_第九轮.md` §五 · **现读**（本单所有数字都是现读，不是照抄文档）。
> 两份逐条清单（写手直接照着做）：`施工单_双语③逐处换key_附_Shell.md`（33 条）· `施工单_双语③逐处换key_附_BattleDeck.md`（50 条）。
> 🔴 **本单的中心结论**：**`SetText` 已经不是这条线的尺子**（文档里那个「107 处 / 42 文件」已过期）——
> 真正剩下的是 **(a) 整句/整块的中文提示语**（不在 `SetText` 上）与 **(b) 11 条「键已写好、只差收进表」**。
> 另：**`Core/` 那一片两份附件都没扫**（见 ⑨·F1，是本单最大的盲区）。
## ① `Core/Loc.cs` 现状（1116 行）—— 文档说「21 条」，**实测 168 条**

| 项 | 实读 |
|---|---|
| 类 | `CardPresentation.Loc`（`public static`，`Core/Loc.cs:76`） |
| 词条数 | **`EntryCount` = 168**（`Table` = `Dictionary<string, Entry>`，`Entry` = `(Zh, En)` 两列） |
| 结构 | `Entry{ string Zh, En; }`，私有 `struct` ⇒ 外部**只能**通过下面这些口读 |

**取值口（写手只用这几个）**
- `string T(string key)` —— 按**当前语言**取。🔴 **缺键 ⇒ 返回【键名本身】 + `LogWarning`**（`LogWarning` **按键去重**；`MissingCount`/`LastMissingKey` 照旧每次 +1）。`T(null)`/`T("")` ⇒ `""`（不出声）。
- `bool HasEntry(string key)` —— **取词条前必须先判它**。🔴 `HasEntry(null)` ⇒ `false` + **只出声一次**（⛔ 不抛异常 —— 抛了会把整条自检崩掉）。
- `string EnOf(string key)` —— **不看当前语言**取英文列；没有 ⇒ `null`。⚠️ 只给「字体闸」用（`CardText.Term`），⛔ 别当第二条取值路。
- `string LanguageName(AvailableLanguages)` / `KeyOf(...)` —— 下拉 12 项的名字。
- `bool HasCjk(string)` —— **汉字墨高 vs 拉丁大写高**那条判据的**唯一一份**（接 `Label.SetCapHeight/SetGlyphHeight`）。
- `IEnumerable<string> AllChinese()` —— 建中文字体资产时要烘哪些字（**永远给中文，别加语言闸**）。

**状态 / 落盘**
- `Current` · `Effective`（没文案的语言折成 `Fallback`）· `Default = Chinese` · `Fallback = English` · `HasOwnText(l)`（只有 `Chinese`/`English` 为真）。
- `public const string PrefKey = "Language"` · `PersistOverride`（自检用：只改内存不写盘）· `SetLanguage(l) → bool`（变了没有）。
  🔴 **本类不发事件** —— 调用方自己重设已画出来的字。
- `MissingCount` / `LastMissingKey` / `FallbackCount` / `LastFallbackKey` / `MissingWarnedCount` / `HasWarnedMissing(k)`。
- 自检口：`RestoreForTest(lang)`（按给定值放回内存态，**不动 `PlayerPrefs`**）· `ResetForTest()` · `ReloadForTest()` · `ResetMissingWarnedForTest()`。
- `AvailableLanguages` 枚举 = 原版逐条照抄：`English=0 … Chinese=110`（**步进 10，⛔ 别改成 0..11**）；`Languages[]` = 12 项声明序。

**表按块分布（168 = 下面之和）**
`语言表头 20`（General 页 7 + `Settings/Online/Title` 1 + 12 个语言名）· 卡组编辑窗 9 · A891 续 `MenuDeck/*` 7 · 第三轮整改 9 · 商店 1 · 第六轮社交 6 · 第七轮社交 6 · 第八轮联盟 4 · 第十轮 3 · 第十一轮 6 · **卡面兵种行 `Card_Race/*` 43** · **`Battle/*` 第十二轮 28** · `Battle/HUD` 5 · 第十五轮 `G5` 6 · `G8` 2 · `G9` 13。
> ⚠️ 文档里那个「**21 条**」：现读**对不上** —— 最初那一批 = **20 条**（General 7 + Online 1 + 语言名 12），第 21 条起已经是卡组编辑那一块。**别照抄 21**，一律写 `Loc.EntryCount`。
## ② `SetText` 现读全表（**不是 107/42**）

- 裸 `grep 'SetText('` = **183 行 / 48 文件**；**去掉文档注释与声明，真调用点 = 165 处 / 44 文件**（其中 4 个是 `Editor/` 自检宿主）⇒ **非 Editor 真调用点 40 文件**。
- 按**实参形状**分（这是判「还要不要动」的直接判据）：

| 实参形状 | 处数 | 含义 |
|---|---|---|
| `EXPR`（拼串/三元/方法调用） | 72 | 要看里面那个字面量 —— **这才是活的** |
| `VAR`（纯变量） | 63 | 多半已由上游算好（`CountText(Term,n)` / `CardText.Name(...)`） |
| `LOC`（实参里就含 `Loc.`/`CardText.`/`...Term`） | 14 | ✅ **已接好，别动** |
| `LIT`（直接传字面量） | 16 | 其中 **13 处是 `""`**；2 处纯数据（`"127.0.0.1"`/`"0"`/`"99"`）；**真①只有 1 处** = `Shell/CampaignTab.cs:742 "Points: "` |

⇒ **字面量直传的空壳已经挖光了**；剩下的是 `EXPR`/`VAR` 里那些**整句中文**。**结论：`SetText` 计数不再能当进度尺子。**

**各文件 ①（玩家可见、还没走 `Loc`）—— 数字来自两份附件，合计 110 处**

| 文件 | ① 处数 | 文件 | ① 处数 |
|---|---|---|---|
| `Battle/BattleDriver.cs` | **27** | `Shell/DeckSelectionPopup.cs` | 4 |
| `Shell/SettingsWindow.cs` | **12** | `Shell/MainMenuRuntime.cs` | 3 |
| `Shell/LiveOpsEventWindow.cs` | **11** | `Battle/SettingsPanel.cs` | 3 |
| `Deck/DeckRuntime.cs` | **10** | `Shell/ShopWindow.cs` · `PlayerProfileWindow.cs` · `LeaderboardWindow.cs` · `RankedEventWindow.cs` | 各 2 |
| `Shell/PracticeModePopup.cs` · `DeckInfoPopup.cs` | 各 7 | `Shell/ShopData.cs` · `CollectionData.cs` · `ProfileData.cs` | 各 1 |
| `Battle/ChatPopupPanel.cs` | 6 | `Battle/CardDisplayWindow.cs` · `MultiCardDisplay.cs` · `TutorialOverlay.cs` · `Deck/DeckEditorState.cs` | 各 1 |
| `Battle/EndPanel.cs` | 5 | 其余 **139 个 `.cs`** | **0** |

三类口径：**① = 110**（精确逐条）· **② = 开发日志/调试串**（Shell ≈940 语句、Battle/Deck 284 语句，**不进表**）· **③ = 原版 prefab 英文常量 / 资源名 / 玩家名 / 纯数据**（**不进表**）。
⚠️ ②/③ 的分界是**行级近似**（多行 `Debug.*` 的续行落进 ③）⇒ 引用时用 ①。
## ③ ⛔ **不该进语言表**的字串（最容易踩的一节）

| 类 | 判据 / 出处 | 为什么 |
|---|---|---|
| **卡名 / 卡面效果文字 / 关键词 / 阵营名** | 走 `Core/CardText.cs`：`Name()` · `EffectSentence()` · `Keyword()` · `Faction()`。中文源头 = `cards_engine.json` 的 `NameZh`/`DescZh`（← `数据/卡牌翻译/zh_cards.json`） | **已经有自己那条线**（`CardText.Zh` 已含语档闸，`CardText.cs:36`）。再进 `Loc` = **两个权威**（工程红线）。🔴 任务里说的「卡面/卡名也跟着切」**已经做完了**（2026-10-18 第十三轮），**不是靠 `Loc` 做的**。 |
| **兵种行 `Card_Race/*`（43 条）** | **唯一的例外**：它**已经在 `Loc` 里** | 判据 = **原版自己就走 I2**（`GameStaticData.MinionRaceToString` 拼 `"Card_Race/"+race`）。⛔ 别再在 `CardText` 里另造一份。 |
| **战斗日志的开发者中文** `RuleCodes.Describe(code)` | `RuleEngine/Core/RuleCodes.cs:100` | 它是**开发者整句**、同时被 AI/网络/教程/自检（20+ 处）当人话用。✅ 该进表的是 `RuleCodes.TermKey(code)` 出的**词条键**（4 条，见 ⑥），**不是** `Describe` 的句子。 |
| **远端 I2 词条键**（`Battle/Tips/*` 24 条等） | `Loc.cs:615-616` | **键名进表可以**（值自拟/取 prefab 原文）；⛔ 但**不许把键名当文案画上屏** —— 今天 `Missions/Completed` 与 `MenuDeck/HUD/DiscardChanges` 正是这个毛病（见 ⑥）。 |
| **原版 prefab 的样例值 / 预置英文常量** | 例：`PurchasePremiumWindow.TxtPrice = "300,00"` · `TxtArmyName = "Ultramarines"` · `CampaignTab.cs:989 "Siguiente: 5d 20h 15m"`（西语占位）· `EnergySinglePlayerOnlyEventWindow.cs:456/480 "1256"/"751"` · `BoosterPackOpenWindow.cs:174 "Новинка!"`（俄语） | 那是**印刷样例**、不是文案。✅ 正确做法 = 换成**真值**或**词条**（它们正是 ⑥ 的候选），⛔ 不是把样例串搬进表。 |
| **玩家数据**（玩家名 / 卡组名 / 卡名 / 联盟名） | `ProfileData.PlayerName` · `State.Deck.Name` · `DeckSelectionPopup` 的 `raw.Name` · `MainMenuRuntime.CurrencyNames()` | 运行时值，不翻。 |
| **纯格式串**（`"{0}/{1}"` · `"/ "` · `"99"` · `"1-"`/`"8+"`） | —— | **分两种**：原版它是 `mTerm`（如 `Battle/Effect/*` 的 `"{0} 近战"`）⇒ **该进**；否则不进。 |
| **资源名 / 节点名 / `PlayerPrefs` 键 / `Debug.Log*` / 断言文案 / 自检 getter（`"<无>"`）** | Shell ≈940 · Battle/Deck 284 语句 | 不进。⚠️ 但**改那批中文会让按中文子串断言的旧自检变红** —— 见 ⑧·E1。 |
| ⚠️ **任务简报举的 `Resigned`** | `grep -rn 'Resigned'` 全仓 **0 命中** | **没查到**这个字面量（对不上简报的举例）⇒ 如实记，别照抄进单子。 |
## ④ 键名规范（照 `Loc.cs` 现表归纳）

1. **有原版键就必须用原版键**：键名 = 原版那颗 `I2.Loc.Localize` 的 **`mTerm` 原文**，且**注释里写清出处**（pid / prefab 节点路径 / `.c` 方法体 / `stringliteral.json` 地址）。
   ⛔ **不许自拟** —— 自拟了 = 查不到 = 界面上印键名。表里每一条的注释都在演示这件事。
2. **原版真没有 ⇒ 照同族形状自拟**，并在注释里**明写「自拟」** + 为什么。已有先例：`Settings/Online/Title` · `MenuDeck/Button/Random` · `Battle/Tips/GoFirst` · `MenuDeck/Error/SaveFailed` · 13 条 `MenuDeck/Error/*`。
3. 🔴 **别为了「看着整齐」统一前缀**：原版 `张牌`/`美容品` 用的是 `MenuShop/ShopItemType/*`（商城那族），`卡组信息`/`完成` 才在 `MenuDeck/*` 下（`Loc.cs:216-219` 明写）。
4. 🔴 **同一个中文词可能是两条不同的键**：`Select Language`（设置窗）vs `Select language`（建盟页）；`Create Deck`（大 D）vs `Create deck`（小 d）。⛔ 别合并。
5. 🔴 **英文列一律逐字符照抄原版 TMP 的 `m_text`**（含原版自己的小写/语病 —— `Touch input` 的小 i、`There are no **deck** …` 的单数）。取不到时**如实标「EN 自拟」**，⛔ 别把西语/葡语占位串当英文抄。
6. 中文列的三个来源，**注释里必须写清是哪一个**：① 用户实拍原版中文客户端（`资料/原版参照图/用户实拍_1017/*.png`）② 我们自己的译表 `数据/本地化/i18n/zh_CN.csv:<行>`（⚠️ **按英文源串索引，不是按 mTerm**）③ **自拟**。
7. 键名里的**下划线是原版的**（`MainMenu/Settings/ButtonLabel/Exit_Game`）—— ⛔ 别改成 `ExitGame`。
## ⑤ 「已有 vs 缺」——代码里引用、表里没有的键（现读全仓扫描）

**扫描口径**：`Assets/`（跳 `Plugins`/`TextMesh Pro`/`Settings`/`Scenes`/`Resources`/`Warcraft*` 等第三方与美术目录）全部 `.cs`，
收 `Loc.T(`/`Loc.HasEntry(`/`Loc.EnOf(`/`CardText.Term(`/`TermKey(`/`const string *Term*/*Key* = "…"`/字典字面量里的 term 形状串，再与 `Loc.Table` 求差。

**真缺口 = 16 条**（另有 3 条是自检故意造的假键 `No/Such/Key/*`、2 条是前缀 `Card_Race/`·`MenuDeck/Error/`，**不算缺**）：
> 🔴 **2026-10-09 现核订正（铁律 5）**：原来写 **11 条** ⇒ **16 条**（`A1017` ③）。
> **错因** = 正文这个数没跟着**下面那张表**走：表里**逐键数**就是 16 ——
> `Battle/Tips/*` **4** · `Settings/Graphics/AutoZoom` **1** · 丢改动那扇 **3**（`MenuDeck/HUD/DiscardChanges` + `MainMenu/General/{Cancel,Discard}`）·
> `Missions/Completed` **1** · `MainMenu/PurchasePremium/Description` **1** · `SocialMenu/Alliances/TransferLeadership` **1** ·
> `Battle/AlliancePanel/*` **4** · `MenuDeck/Error/InvalidDeck` **1**。⇒ 波 0 按**表**补齐，⛔ 别按正文那个数。
> ⚠️ 同族两处「11」也一并改掉（本文件 ⑥ 与 波 0 那两行）—— 两份数字打架比没有更糟（铁律 6）。

| 键 | 引用点 | **今天走什么兜底** |
|---|---|---|
| `Battle/Tips/NotEnoughMana` · `NotYourTurn` · `NoTargetAvailable` · `NotEnoughRoom` | `RuleCodes.TermKey`（`RuleCodes.cs:191-194`）；消费点 `BattleDriver.cs:7771` | `Loc.HasEntry(键) ? Loc.T : RuleCodes.Describe(rc)` ⇒ **回我们的中文开发者句**。⚠️ `RuleCodes.cs:124/:153` 的示例写的是 `Loc.T(key) ?? Describe(rc)` —— 🔴 **那是错的**（`Loc.T` 缺键返回**键名**、**从不返回 null**）⇒ 写手照 :7771 那份，别照注释那份 |
| `Settings/Graphics/AutoZoom` | `Battle/SettingsPanel.cs:254`（`AutoZoomTermKey`）+ `:612` 显示点用 `AutoZoomLabelEn` | **显示的是我们写死的英文 `"Auto zoom"`** ⇒ 英文档正确、**中文档也印英文** |
| `MenuDeck/HUD/DiscardChanges` + `MainMenu/General/{Cancel,Discard}` | `Deck/DeckRuntime.cs:3606` · `Shell/PopUpGameWindow.cs:180-181` | 是**同一扇弹窗的同一笔账**：丢改动那扇。**画面上直接印键名**（注释自己写着「今天画面上显示的就是这个键」） |
| `Missions/Completed` | `Shell/DailyData.cs:181`（`CompletedMessage`），消费点 `:175` → `MissionsTab.cs:546` 那一行 | **印键名**（`SayCompletedKeyOnce()` 的注释自己承认） |
| `MainMenu/PurchasePremium/Description` | `Shell/PurchasePremiumWindow.cs:209`（`TermInfoBody`） | `string.Format(TermInfoBody, points)`，**印键名 + `{0}`** |
| `SocialMenu/Alliances/TransferLeadership` | `Shell/AllianceMemberOptionsPopup.cs:158` | 注释自己写「**只用作出声与断言，不换字**」⇒ 今天不显示 |
| `Battle/AlliancePanel/{PlayerLabel,TitleLabel,AllianceLabel,NotInAnAlliance}` | `Shell/AlliancePanelWindow.cs:152-155`，`LocOr()` = `Loc.HasEntry(term) ? Loc.T(term) : prefabEn` | ✅ **有兜底**：回 prefab 静态英文（`"Name:"` / `"Title:"` / `"Alliance:"` / `"This player is is still not part of an Alliance"`）⇒ **英文档对、中文档印英文** |
| `MenuDeck/Error/InvalidDeck` | `Deck/DeckRuntime.cs:3797`（`MenuDeckErrorKeyFallback`） | 数字族 `MenuDeck/Error/{0}` 的兜底 ⇒ 落到**未在表里**的键 |

> ✅ **反过来也查了（现读）**：168 条里 **`"键名"` 字面量在非 `Loc.cs` 的 `.cs` 里出现 104 条**；
> 其余 **64 条是「前缀 + 动态值」拼出来的**，**没有一条是「谁都不引用」的空键**：`Card_Race/*` **40 条**（`CardView.RaceTermPrefix` + `subtype`）· `MainMenu/Settings/LanguageName/*` **12 条**（`Loc.KeyOf(lang)`）· `MenuDeck/Error/*` **12 条**（`DeckRules.Describe` 拼枚举名）。
## ⑥ 复用清单（**键已经想好、只差收进表 / 只差换调用点**）

**A. 表里有键、显示点却没走它（零成本，直接换）**
- `Battle/TutorialOverlay.cs:417` `"Continue"` → **`Battle/Tips/Continue`**（表里有）。
- `Battle/SettingsPanel.cs:612` `AutoZoomLabelEn("Auto zoom")` → **`Settings/Graphics/AutoZoom`**（键在本文件 `:254` 记着，**只差把它加进表**）。
- ✅ **已接线（`A1029` ② · 2026-10-09 现核）**：`Core/CardView.cs` 的 `CreatedByLine` 已走词条 —— 现在是
  **`Loc.HasEntry(CreatedByTerm) ? Loc.T(CreatedByTerm).Replace("{0}", 创建者) : 旧的硬拼 + 出声`**
  （键 `Battle/HUD/CreatedBy` 的**现行**条目在 `Core/Loc.cs` 的 `Battle/HUD/CreatedBy` 那一行，⛔ 别按行号找）。
  - ⚠️ **原记录两处要订正**：① 路径原来写 `Battle/CardView.cs`，**实际在 `Core/CardView.cs`**；
    ② 原写「**那一行还是惰性的**」**已过期** —— 惰性那半是**旧的**，今天已兑现（`A985④`）。
  - ⚠️ **接线的形状不是裸 `Loc.T`**：它**先判 `Loc.HasEntry`**（`A985④` 点名要的那一跳）——
    少了它，键一旦被删/改名，`Loc.T` 会把**键名本身**印到卡面上（`Loc.T` 缺键返回键名、从不返回 null）。
- `Battle/ChatPopupPanel.cs:76` 那 6 颗钮（`Greet`/`Threat`/`Well Played`/`Taunt`/`Sorry`/`Oops`）→ 原版标签来自远端 I18N，**本地没有** ⇒ 建 6 条自拟键。
- `Shell/` 里 `未选战将` 4 处 → 直接复用**已在表**的 `MenuDeck/Error/NoWarlord`；`没有可选的卡组` → 复用 `MenuCollection/NoDecksFound`。
- `Shell/ShopData.cs:297` 的键名 **有出处**（`ShopWindow.cs:785` 的注释自己写着 `MenuShop/ExtraLegendaryWarning`）⇒ 键名可照抄、只差收进表 + 值。

**B. 「底座键」建议先补（跨窗复用，补完才能批量替换）**
`MainMenu/General/OK`（**原版只有大写 `OK`**；中文列 = `确定`）· `MainMenu/General/Cancel`（`取消`）· `Demo/MainMenu/{ExitGame,ExitButton,CancelButton}`（退出确认那三句）。
> 🔴 **2026-10-09 现核订正（铁律 5 · `A1017` ①）**：这一行原来写的两个键名**都已不成立** ——
> ① 原来写 `MainMenu/General/Ok`（**小写 k**）⇒ **原版不存在这个拼法**，只有大写 **`MainMenu/General/OK`**（三条独立判据，见 `A1017` ①）；
> ② 原来写 `MainMenu/Settings/ExitGame/{Confirm,Ok}` ⇒ 那是**波 1 当时的自拟键**，已被 `A1026` 裁定换成**原版真键**
> `Demo/MainMenu/{ExitGame,ExitButton,CancelButton}`（`stringliteral.json` 各 1 条）。**两张表都搜过才叫查过**
> （只搜 `assets_full` 会得出「原版没有」的假结论 —— 波 0 当年就是这么漏的）。
> 出处 = `Core/Loc.cs` 里 `MainMenu/General/OK` 与 `Demo/MainMenu/*` 那两组条目（⛔ **按条目名找，别按行号**）。
⚠️ `确定` 要落 `MainMenu/General/Confirm`（表里中文列是「**确认**」）还是另立 —— **需裁决**。
⚠️ 顺带记一条**已裁定的可见副作用**：`MainMenu/General/OK` 的 ZH 列取「确定」，而**改前那批站点写的是 `知道了`**
⇒ 中文档会由「知道了」变成「确定」。**判据照原版键**（铁律 11），但**必须留痕**，别让它看起来像我们随手改的（详见 `A1019`）。
## ⑦ 批次切块（**文件所有权零交集** · 顺序照 §23：卡组编辑 → 主菜单/外壳 → 战斗 HUD → 各窗口）

🔴 **三条硬约束**（否则撞车/白跑）：① **`Core/Loc.cs` 一个时刻只能一个写手** ② **`Editor/*` 自检宿主一个时刻只能一个写手**（`ShellScene`/`MainMenuScene`/`ShopScene`/`RewardsScene`/`BattleScene`/`CollectionScene`/`DeckScene` 被多批共用）③ **秒级类型检查**要么主对话统一跑，要么每代理一个独立 `TMPDIR`（`TMPDIR=/tmp/wf_a1 bash d:/4/Unity/工具/typecheck.sh`）—— ⚠️ 独立 `TMPDIR` **只解决输出覆盖，解决不了「看见别人写到一半的 `.cs`」**，报错全在**不是你负责的文件**上就无视、隔一会儿重跑。

**波 0（串行，必须先做完）· 补 16 条缺键 —— 白名单 = `Core/Loc.cs`（1 个写手）**
产出：⑤ 那张表 **16** 条（⚠️ **2026-10-09 现核：原写「11」是错的**，见 ⑤ 顶部那条订正）+ ⑥·B 的 3 条底座键 + `MenuShop/ExtraLegendaryWarning`。**每条都要带出处注释**（原版键 or 明写自拟）。
断言宿主：**本波不改**（改表不算改宿主），由波 2 统一加。
> ⚠️ **本波做完才能开波 1** —— 波 1–4 全都要引用这些键。

**波 1（4 条并行）· 生产代码 —— 只碰各批自己的消费文件，⛔ 一个 `Editor/` 都不碰**

| 批 | 白名单文件 | 预计 ① 处数 | 主要落点 |
|---|---|---|---|
| **P1 卡组编辑** | `Deck/DeckRuntime.cs` · `Deck/DeckEditorState.cs` · `Core/FilterPanelModel.cs` | 10 + **F1 未扫** | `"返回"` `"把卡拖到这里"` `"默认卡背"` 导入错误那 3 条 · 默认卡组名 4 处（⚠️ 是**写进存档的数据**：口径存疑 —— 「会显示 ⇒ ①」还是「数据不翻、显示时再翻」**需裁决**，见附件 B §五） |
| **P2 主菜单/外壳 A（设置 + 联机）** | `Shell/SettingsWindow.cs` · `Shell/MainMenuRuntime.cs` · `Shell/ProfileData.cs` · `Shell/LeaderboardWindow.cs` · `Shell/RankedEventWindow.cs` · `Shell/PlayerProfileWindow.cs` | **22** | **最重的一块**：`SettingsWindow.cs:2369-2377`（8 条）与 `:2533-2555`（**23 条**）整段弹窗文案 |
| **P3 主菜单/外壳 B（社交 + 弹窗 + 战术页）** | `Shell/LiveOpsEventWindow.cs` · `PracticeModePopup.cs` · `DeckInfoPopup.cs` · `DeckSelectionPopup.cs` · `ShopWindow.cs` · `ShopData.cs` · `CollectionData.cs` | **28** | 联机匹配取消那一族（4 条）· 模式名 2 条（`遭遇战/经典`）· 卡组串校验整句 |
| **P4 战斗 HUD / 各窗口** | `Battle/BattleDriver.cs` · `Battle/EndPanel.cs` · `Battle/ChatPopupPanel.cs` · `Battle/SettingsPanel.cs` · `Battle/CardDisplayWindow.cs` · `Battle/MultiCardDisplay.cs` · `Battle/TutorialOverlay.cs` · `Shell/CampaignTab.cs` · `Shell/ReferralPopupWindow.cs` · `Shell/RankedRewardEventWindow.cs` | **45** | **战斗日志 11 句整簇**（`BattleDriver.cs:5646-5678`）· 结算副标题 5 条 · 6 颗聊天钮 · `TutorialOverlay:417`（零成本） |

> 🔴 **2026-10-09 现核订正（铁律 5 · `A1029` ①）**：**P4 那一行原来写「预计 ① 处数 = 38」⇒ 真值 45**
> （按该批白名单**逐站点现读**数的结果）。⚠️ **错因没查清** —— 只记两个读数，⛔ 别拿一个猜测的成因填空。
> ⚠️ **同表两处已过期，写手别再照抄**：
> ① **P1 那一行**的「默认卡组名 4 处…… **需裁决**」⇒ **已裁**（`A1037`／`A1047`，口径 = **翻**：
> 「**创建那一刻按当前语档生成、当场物化进存档**」，⛔ 不是「显示时才翻」）；
> ② 表里 `SettingsWindow.cs:2369-2377` / `:2533-2555` 一类**行号是 2026-10-18 的读数**，而 `P2b` 已落地
> ⇒ **一律按符号名 / 条目名现读**（⛔ 别把漂掉的行号再抄一遍、也别制造新的死行号）。

**波 2（2–3 条并行）· 断言宿主（一个宿主一个写手）**
`Editor/{DeckScene, CollectionScene}`（P1）· `Editor/{SettingsScene, MainMenuScene}`（P2）· `Editor/{ShellScene, RewardsScene, ShopScene}`（P3）· `Editor/BattleScene`（P4）。
> 📌 上一轮实测的**瓶颈就是这一波**（8 次「代码做完、宿主不在白名单里」）⇒ **先派波 1、再派波 2**，别反过来。

> 🔴 **本单没有覆盖的**：`Core/CardText.cs`（迁移已迁的那几条）+ `Core/FilterPanelModel.cs` 的**选项表**（`RarityNames` / `TypeLabels` / `CostBuckets` / `"Owned only"` / `"Upgradable only"`）—— 见 ⑨·F1，**建议单开一批 P5**。
## ⑧ 断言与灭自证（**改完怎么证**）

**A. 现成模板**（照 `Editor/DeckScene.cs:5015-5075` 的 G8 那一段抄，它是这条线上写得最全的一条）：
```
Loc.RestoreForTest(AvailableLanguages.Chinese);  …走生产链…  string zh = <屏幕上那句>;
Loc.RestoreForTest(AvailableLanguages.English);   …同一条链…  string en = <屏幕上那句>;
CheckTrue(Loc.HasEntry(key),  $"词条键在表里（{key}）");                       // 坑表 #18
CheckTrue(zh != en && zh.Length > 0 && en.Length > 0, "两档逐字不同且都非空");   // ★ 灭自证主判据
CheckTrue(zh.Contains(Loc.T(key)), "中文档 = Loc.T(键)");
CheckTrue(en.Contains(Loc.T(key)), "★ 英文档：同一处跟着换成英文"+
                                   "｜🧨 改回写死中文 ⇒ 只有这条红（中文档那条照样绿）");
```
⚠️ **一律先 `Loc.RestoreForTest`，⛔ 别用 `Loc.SetLanguage`**（会写 `PlayerPrefs`）—— 或先把 `Loc.PersistOverride = true`。跑完**必须放回**玩家原值。

**B. 每批都要加的三条**
1. **「本批的键都在表里」** —— 逐键 `Loc.HasEntry`（**坑表 #18：原版有词条 ≠ 我们表里有键**）。`Editor/BattleScene.cs:8866` 的 §W6 就是这形状。
2. **「本批的字面量不再出现在本批文件里」** —— 把该批每个 ① 的原文列成数组，对**源码文件**做包含检查 ⇒ 谁写回来就红。
   ⚠️ **别按 `grep -c ✗` 数文案**（工程踩过）；也**别拿「被测代码自己会打印的东西」当过滤词**（自证坑）。
3. **两语档各断一次**（A 那两条）—— 只断中文档 = 半边绿。

**C. 灭自证（挡住「把实现和检测器一起改回去」）—— 三档任选，越多越好**
- **C1 结构上不可能同时满足**：断 `zh != en` **且** `!Loc.HasCjk(en)`（英文里不许有汉字）⇒ 「两边一起改回写死中文」当场红。
- **C2 反档**：断 `Loc.HasEntry(key)` **且** `Loc.T(key) != key` ⇒ 光把键塞进表但值是键名，也会红。
- **C3 「状态不随语言走」**（照 G8 的 ★②-d）：诊断串/错误码两档**逐字相同且非空** ⇒ 只有**显示**跟着语言。
  🔴 少了「非空」那半，`null == null` 会**假绿**。
- **D. 表本身**：断 `Loc.EntryCount >=` 开工前基线（写死一个数）⇒ 挡住「把键删掉、断言也一起删」。

**E. 会误伤的坑（先读再改）**
1. **`Editor/SettingsScene.cs` 有 5+ 条按中文子串断**（`:997 / :1285 / :1458 / :1505 / :1607 / :2678` 读 `win.Flash.Contains("画质")` 一类）⇒ P2 改 `SettingsWindow._flash` 那批中文**会带红** ⇒ **改断言的人和改代码的人排在同一批**。
2. **`Loc.T` 缺键返回键名** ⇒ 任何 `?? 兜底` 都是死代码，一律 `Loc.HasEntry(k) ? Loc.T(k) : 兜底`。⚠️ `RuleCodes.cs:124/:153` 的示例**是错的**，顺手订正（铁律 5）。
3. **自检每一格先摆回基线** · **跑自检必须串行**（同工程只能一个 Unity 实例）· **别改一条跑一遍**（铁律 12）。
## ⑨ 没查清的部分 + 顺手发现

**Q9 · §三 第 23 条那两条旧账，现核**
- ✅ **来源图现在【在仓库里】**：`d:/4/Unity/资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`（2,230,898 B，与另 9 张同目录）⇒ §23 那句「**那张来源图不在仓库里** ⇒ 复核只能再问用户」**已过期**（用户 2026-10-17 补了实拍）—— **建议就地订正**（那是 `项目任务.md`，不在本单写权限内）。
- ✅ **「两处打架」已解决**：词条以 `Loc.cs` **现表**为准（168 条），判据文件那份 13 词只当历史。

**Q10 · 相关卡只按英文匹配 —— ✅ 仍然是**（没被修）：`Core/RelatedCards.cs:43` 把 **英文 `card.Desc`** 喂 `CreatePool.MentionedCards`，而 `CreatePool` 的名字索引建在 **`Norm(c.Name)`（英文）**上（`CreatePool.cs:548-551`）；唯一吃中文的是**黑暗契约**那一支（`:704-706` 同时查 `NameZh`/`DescZh`）—— 特例，不是通用索引。
- **索引该建在哪**：`CreatePool.cs:548-551` 建名字索引那一处**并排再建一张 `Norm(c.NameZh)` 索引**，并把 `RelatedCards.Find` 的入参改成**「按当前语档取 `Desc`/`DescZh`」**。🔴 否则中文档下相关卡**按英文文本匹配、屏上却印中文** ⇒ 玩家看到的「相关」是错的（**不是不翻，是翻错**）。

**F1 🔴 本单最大盲区：`Core/` 整片两份附件都没扫**
- `Core/FilterPanelModel.cs` 的 `RarityNames = {Common,Rare,Epic,Legendary,Special}`（`:348`）· `TypeLabels = {Warlord,Troops,Stratagem}`（`:369`）· `CostBuckets[].Label`（`:361-364`）· `"Owned only"`/`"Upgradable only"` **都被 `DeckRuntime.cs:5214/:5427` 直接画上屏、不过 `Loc`**。
- 🔴 **顺手发现（施工前必须先查）**：`TypeLabels` 的 `Warlord`/`Stratagem` **与已进表的 `Card_Race/Warlord`(=战将)/`Card_Race/Stratagem`(=策略) 同串** ⇒ **可能直接复用那两条键**。⚠️ 原版这几个筛选 option 挂**哪条 `mTerm`** —— **没查**（本单不解析 prefab）；按字面串猜键名正是 P1 那类错。
- 其余未扫：`Core/` 38 件的**显示**字面量（`Tooltip` / `CardView` / `CardWinBox` / `CardText` / `DraggableController` …）。

**F2 · 另外三条「没查清」**
- **战斗日志那 11 句**（`BattleDriver.cs:5646-5678`）**疑似**就是二进制里 `Battle/Cemetery/*` 那 19 条（`Loc.cs:615-616` 记着），**没逐条对过** ⇒ 键名先自拟、施工前核一次。
- **`Battle/Tips/*` 那 24 条**里有没有能直接复用给提示行的句子 —— **没核**。
- **`Shell/*` 的 `_flash`/`Last*Message` 是否真不上屏** —— 只追到 `Editor/SettingsScene.cs` 一个消费者（判 ②），**没逐条追**；若将来上屏，那 ~15 条立刻变 ①。

**F3 · 顺手发现（供调度台分流，本单不动）**
1. 🔴 `RuleCodes.cs:124`/`:153` 的示例 `Loc.T(key) ?? RuleCodes.Describe(rc)` **是错的**（`Loc.T` 从不返回 null），与 `BattleDriver.cs:7771` 的正确写法**并存** ⇒ 属「两处写同一条规则」。**建议就地订正**。
2. 🔴 **同一句整句中文被复制到 3–4 个文件**（`未选战将` / `这套卡组还没有选将，开不了局。`，见 `DeckInfoPopup.cs:836`/`LiveOpsEventWindow.cs:922`/`PracticeModePopup.cs:1611`）⇒ **建 1 条键、四处复用**，⛔ 别各起一名。
3. ⚠️ `Shell/` 里 **8 处英文占位常量直接上屏**（`PlaceholderName="Player name"` / `BtnLabel="Select"` …）—— 换语言不变；两份附件把它们归了 ③ ⇒ **别漏**。
4. ⚠️ **`SetText` 之外的显示路径最常漏** —— 各文件**自己的 `Text(...)` 包装签名不同**：`Shell/MenuWindowBase.cs:284` = `(parent, **text**, x1..)`；`MenuDraw.cs:1686` = `(parent, rect, **text**, …)`；`SettingsWindow.cs:2662` = `(p, **nodeName**, **text**, …)`。**改之前先读那一份局部 helper 的签名**。
