# WCoreDeck —— `TermOr` 闸门收口 + `Missions/Completed` 接线

- 日期：2026-10-18（第六会话）
- 写手：`WCoreDeck`（动手写手，有 Edit/Write/Bash）
- 白名单（6 个）：`Core/FilterPanelModel.cs` · `Deck/DeckRuntime.cs` · `Shell/DailyData.cs` ·
  `Shell/MissionsTab.cs` · `Editor/DeckScene.cs` · `Editor/CollectionScene.cs`
- **实际改了 5 个；`Editor/DeckScene.cs` 一个字没动**（它的 4165 那条 `A1031③` 不在本批）

---

## ① 结论（逐条一句话）

1. **`TermOr` 闸门两处本体（`FilterPanelModel.cs` / `DeckRuntime.cs`）都已删除**，7 个调用点全部收成**裸 `Loc.T(键)`** —— 全仓 `grep TermOr` **代码引用 = 0**（只剩 10 行**注释里的更正痕迹**，逐条标注「已删除 / 已收口」）。
2. **收口对界面是零变化**：那 14 条键**在 HEAD 就已经全在表里**（`git show HEAD:…/Loc.cs` 逐条核过）⇒ 闸门当年返回的就是 `Loc.T`，与裸调用**同值**。唯一会变的是第 3 件。
3. **`Missions/Completed` 已接表**：`DailyData.DailyCounterText` 从 `return CompletedMessage;`（把**键名**当文案印）改成 `return Loc.T(CompletedMessage);` —— **只此一处实现**（选的是 `DailyData.cs:175` 那一支，`MissionsTab.cs:553` 那行**没改**、只负责画）。
4. **显示链复核（与简报一致）**：`MissionsTab.Txt1`（`Shell/MissionsTab.cs:392`）→ `_win.Text` = `MenuWindowBase.Text`（`Shell/MenuWindowBase.cs:284`）**原样吃字符串、从不查表**（`MenuWindowBase.cs` / `RewardsWindow.cs` 的 `grep 'Loc\.'` 各 **0** 命中）⇒ 这就是「键在表里、屏上却印键名」的成因。
5. **三处指定的过期注释全部就地订正**（保留了「⚠️ 日期 更正：原来写 X，实际是 Y」痕迹），另**顺手订正了同一族 5 处**（都在白名单内，见 ② 的 D/E/F/G/H/I）。
6. 🔴 **一条真红预警（不在我白名单，我一个字没改）**：`Editor/RewardsScene.cs:1647` 断言 `TextOf(progFull63) == "Missions/Completed"`（**按字面量断键名**）⇒ 本批做完它**必红**（现在会印 `已完成` / `Completed`）。详见 ⑤-1。

---

## ② 改动清单（`文件:行号（改后）` | 改前 | 改后）

> 行号 = **改后**文件里的行号（行号会漂，符号名才是锚）。

### A. `Core/FilterPanelModel.cs`

| 位置 | 改前 | 改后 |
|---|---|---|
| `:370-378`（注释） | 「⚠️ **表里的键还没补齐**（波 0 未含本族）⇒ 取词一律经 `TermOr` 闸门…；波 0b 补键后自动生效」 | 「🔴 **2026-10-18 更正（铁律 5）**：…**已过期**：本族那 **10** 条键早已全部进表…⇒ 闸门**已删除**…取词一律是**裸 `Loc.T(键)`**」（含订正痕迹） |
| `:375-381`（`RarityNames` doc） | 「它现在只是 `RarityTermKeys` 的**兜底**（键没进表时印它）—— 显示走词条」 | 「⚠️ 2026-10-18 更正（铁律 5）：…**两半都已过期**…本常量现在只剩一个用处：它是原版 prefab 的 `alternativeText` 原文这份事实的落点」 |
| `:416-425`（`TypeLabels` doc） | 「类型三格的**兜底字**（键没进表时印它）…只当闸门未开时的兜底」 | 「类型三格的**英文串**…显示不再吃它…它现在只剩一个用处：`Deck/DeckEditorState.cs` 按类型名搜卡时**额外**比一次这串原版英文」 |
| `:436-448`（闸门本体） | `public static string TermOr(string key, string fallback) { return Loc.HasEntry(key) ? Loc.T(key) : fallback; }` + 整段 doc | **整块删除**（doc 里的「⛔ 别把闸门去掉改成裸 `Loc.T`」也随之作废） |
| `:441-448`（`TypeLabelAt`） | `? TermOr(TypeTermKeys[i], TypeLabels[i]) : null` | `? Loc.T(TypeTermKeys[i]) : null`；doc 补「原来经 `TermOr` 闸门；闸门已删 ⇒ 现在是裸 `Loc.T(键)`」 |
| `:581-588`（卡牌页两开关） | `Label = TermOr(k == 0 ? OwnedOnlyTerm : UpgradableOnlyTerm, k == 0 ? "Owned only" : "Upgradable only"),` | `Label = Loc.T(k == 0 ? OwnedOnlyTerm : UpgradableOnlyTerm),` |
| `:634-637`（Rarity 五档） | `Label = TermOr(RarityTermKeys[i], RarityNames[i]),` | `Label = Loc.T(RarityTermKeys[i]),` |
| `:681-686`（Type 三档注释） | 「`TypeLabels[i]` 是兜底」 | 补一行「兜底已随闸门删掉；`TypeLabels` 现在只给搜卡那条路当原版英文串」 |
| `:925-928`（卡背抽屉 Owned） | `Label = TermOr(OwnedOnlyTerm, "Owned only"), …` | `Label = Loc.T(OwnedOnlyTerm), …` |

### B. `Deck/DeckRuntime.cs`

| 位置 | 改前 | 改后 |
|---|---|---|
| `:1271-1275`（空卡组提示） | 「⚠️ 该键**波 0 未进表** ⇒ 走 `TermOr` 闸门」+ `TermOr("MenuDeck/HUD/DragCardsTip", "把卡拖到这里")` | 「✅ 2026-10-18 收口（铁律 5）：…**已过期**…⇒ 现在是**裸 `Loc.T`**」+ `Loc.T("MenuDeck/HUD/DragCardsTip")` |
| `:2178-2184`（A1032 删件注释） | 拿 `TermOr("MenuDeck/HUD/DefaultCardback", "默认卡背（没选过）")` **当函数名引用**（那个调用点上一轮已删） | 改成描述**当年那句**的写法（「经一道 `Loc.HasEntry` 闸门…」），不再引用一个已不存在的符号 |
| `:3988-3995`（导入空/不合法） | 「⚠️ 键**波 0 未进表** ⇒ 走 `TermOr` 闸门」+ 两个 `TermOr(…)` | 「✅ 2026-10-18 收口」+ `Loc.T("MenuDeck/Error/ImportEmpty")` / `Loc.T("MenuDeck/Error/ImportBadString")` |
| `:4022-4030`（导入未落盘） | 「⚠️ 键**波 0 未进表** ⇒ 走闸门」+ `Loc.HasEntry(k) ? Loc.T(k).Replace(…) : "导入失败：…"` 三元 | 「✅ 2026-10-18 收口：**前半已过期**」+ `Loc.T("MenuDeck/Error/ImportNotPersisted").Replace("{0}", failReason)`（`{0}` 用 `Replace` 那一半注明**仍然成立**） |
| `:6152-6160`（闸门本体） | `static string TermOr(string key, string fallback) {…}` + 整段 doc | **整块删除**，换成一段说明（为什么删：键全在表 / 闸门是静默兜底、与「不许静默失败」冲突） |

### C. `Shell/DailyData.cs`（第 3 件 · `B5`/`A1012` 前半）

| 位置 | 改前 | 改后 |
|---|---|---|
| `:157-177`（`DailyCounterText` doc） | 「**本地没有语言表**…⇒ 照本仓先例**照抄 prefab 里那个串本身**」 | 「⚠️ **2026-10-18 更正（铁律 5）**：…**已过期，而且那不是「办不到」、是我们的缺陷**」+ 三条（原版英文列确实拿不到 → 仍成立 / 键早在我们自己表里 / **错因 = 显示点没接表**）+「那条先例本身没被推翻…先查 `Loc.HasEntry`，是键就接表」 |
| `:184-189`（函数体） | `return CompletedMessage;` | `return Loc.T(CompletedMessage);`（+3 行说明） |
| `:198-207`（`SayCompletedKeyOnce` doc + 日志文案） | 「**我们正在显示一个词条键**…这一处的文案是原版词条键、不是我们编的」· 日志里「**本地没有语言表** ⇒ 我们**照抄这个键本身**」 | 「…接上表之后屏上印的是**词条的文案**（中文档「已完成」/ 英文档 `Completed`），不再印键名」· 日志改成「走 `Loc.T` 接**自己那份表**（ZH「已完成」/ EN `Completed`；两列都是**我们自拟**，原版那两列在远端 CCD、本地拿不到）」 |
| `:194`（`CompletedMessage` 常量） | —— | **未动**（它是**键名**，不是文案；简报说的「可以留着」照办） |

### D. `Shell/MissionsTab.cs`（只改注释，**没有第二处实现**）

| 位置 | 改前 | 改后 |
|---|---|---|
| `:547-553`（`progress` 那一行的注释） | 「它同时管着「那个串是原版的 I2 词条【键】、**本地没有语言表** ⇒ 照抄键本身 + 出声」」 | 「…⇒ 经 `Loc.T` 接**我们那份表**（ZH「已完成」/ EN `Completed`）+ 第一次用到时出声」+「⚠️ **2026-10-18 更正（铁律 5）**：…**已过期**：键早在表里，是**我们没接**；`A1012` 前半已在 `DailyData.DailyCounterText` 收口」 |
| `:553`（调用那一行） | `AlignL(Txt1(parent, pt, DailyData.DailyCounterText(index), …), pt)` | **未动**（唯一实现放在 `DailyData`，避免「两处写同一条规则」） |

### E. `Editor/CollectionScene.cs`（**注释一行、断言零改动** —— 说明见 ⑤-3）

| 位置 | 改前 | 改后 |
|---|---|---|
| `:4643-4654`（`Owned` 标签那条断言的注释） | 引 `TermOr(OwnedOnlyTerm, "Owned only")`，并说「上面那条 `HasEntry` 不是装饰：`TermOr` 是…⇒ 键不在表里时…**红得看不出原因**」 | 改成不引那个已删符号（改引符号名 `BuildCosmetics`，不再引漂号 `:579`）；并把 `HasEntry` 的前提改写成**今天真正的作用**：键不在表 ⇒ **实现与期望都是键名 ⇒ 假绿**；另注明**断言本身一个字没动** |

---

## ③ 证据（现读行号 / 判据出处）

| 判据 | 出处（本次亲读） |
|---|---|
| **14 条键全部在表**（`HasEntry` = `Table.ContainsKey`，与语档无关） | `Core/Loc.cs`：`Missions/Completed:1035` · `MenuDeck/HUD/DragCardsTip:1127` · `MenuDeck/Error/ImportEmpty:1128` · `ImportBadString:1129` · `ImportNotPersisted:1133` · `Card_Rarity/{Common,Rare,Epic,Legendary,Special}:1135-1139` · `DeckDescription/{Minions:1141,Spells:1142}` · `Filters/{ShowOwnedOnly:1143,ShowUpgradableOnly:1144}` · `Card_Race/Warlord:568`；`HasEntry` 本体 = `Core/Loc.cs:1727`（`return Table.ContainsKey(key);`） |
| **「零界面变化」是怎么证出来的** | `git show HEAD:Unity/…/Core/Loc.cs` 里逐条 grep：**14 条键在 HEAD 就都在**（含 `DeckDescription/Minions`、`Spells` —— 用不带 `",` 的宽 pattern 复过一次）⇒ 闸门当年 `HasEntry=true` ⇒ 返回的就是 `Loc.T` ⇒ 与裸调用**同值** |
| **显示链（键名为什么会印上屏）** | `Shell/DailyData.cs:191`（`return Loc.T(CompletedMessage)` 那一行上游）· `Shell/MissionsTab.cs:553` → `Txt1`（`Shell/MissionsTab.cs:392`，`return _win.Text(parent, text, …)`）→ `MenuWindowBase.Text`（`Shell/MenuWindowBase.cs:284`，签名 `Text(Transform parent, string text, …)`，**原样吃字符串**）· `grep -c 'Loc\.' Shell/MenuWindowBase.cs = 0` · `Shell/RewardsWindow.cs = 0` |
| **键 `Missions/Completed` 的判据（原版怎么用它）** | `DailyData.cs` 头部 `MissionCounterDisplay__Setup.c:23-79` 两支；MB `3476392019656054992` `displayCompletedMessage=1` · `completedMessage="Missions/Completed"`；`Core/Loc.cs:1028-1035` ⑦ 那一节（原版 EN/ZH 两列都不存在 ⇒ **我们自拟**，不是「原版文案」） |
| **同族「闸门」的既有形状（本工程别处仍在用，本批没动）** | `Shell/AlliancePanelWindow.cs:355` `LocOr(term, prefabEn)`；`BattleDriver.cs:7771`（`BattleScene.cs:3004` 注释引）—— 那是**别的笔**的白名单，⛔ 本批没碰 |
| **`TypeLabels` 现在的唯一用处** | `Deck/DeckEditorState.cs:415-416`（`Hit(TypeLabelAt(i)) \|\| Hit(TypeLabels[i])`），上游注释 `:407-411` |

---

## ④ 验证

**秒级类型检查**（自己的 TMPDIR，原样贴）：

```
$ cd d:/4/Unity && TMPDIR=/tmp/wf_coredeck bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（注释改动后又跑了一次，仍为 `0 / 0`。⚠️ 同期有别的写手在改 `BattleDriver.cs` / `Loc.cs` / `BattleScene.cs` / `SettingsScene.cs`，本次两次都没报它们的错。）

**验收 1 —— `TermOr` 归零（全仓）**：

```
$ grep -rn "TermOr" . | wc -l      → 10
$ grep -rn "TermOr" . | grep -v "//"  → （空）
```

⇒ **代码引用 0 处**；余下 10 行**全是注释**，逐行都带「已删除 / 已收口（铁律 5）」标记，分布：
`Core/FilterPanelModel.cs` 4 · `Deck/DeckRuntime.cs` 5 · `Editor/CollectionScene.cs` 1。
🔴 **口径要说清**：简报写「白名单里 `grep TermOr` = 0」，**严格按字面不是 0** —— 我把**函数本身**删干净了（0 引用、0 定义），
但那 10 行是**铁律 5 要求保留的更正痕迹**（「原来写 X」里 X 就是那个名字）。**若调度台要求字面 0**，
说一声即可把这些注释改成不引符号名（一行 `Edit` 的事）。

**验收 2 —— `Missions/Completed` 只一处实现**：

```
$ grep -rn "Loc.T(CompletedMessage)\|Loc.T(DailyData.CompletedMessage)" .
./Shell/DailyData.cs:191:            return Loc.T(CompletedMessage);
```

**行尾判定（改前 → 改后，二进制读 `b.count(b'\r\n')` / `b.count(b'\n')`）**：

| 文件 | 改前 | 改后 |
|---|---|---|
| `Core/FilterPanelModel.cs` | `0 / 930`（LF） | `0 / 943`（LF） |
| `Deck/DeckRuntime.cs` | `6445 / 6445`（**CRLF**） | `6455 / 6455`（**CRLF**） |
| `Shell/DailyData.cs` | `0 / 2008`（LF） | `0 / 2027`（LF） |
| `Shell/MissionsTab.cs` | `0 / 1531`（LF） | `0 / 1535`（LF） |
| `Editor/CollectionScene.cs` | `0 / 7499`（LF） | `0 / 7503`（LF） |
| `Editor/DeckScene.cs` | `5921 / 5921`（**CRLF**） | **未动**（`git diff --numstat` 里没有它） |

**`git diff --numstat`**（本次 5 个文件；数字都很小 ⇒ 没有「整篇被翻」）：

```
36  23  Unity/MyGame/Assets/CardPresentation/Core/FilterPanelModel.cs
32  24  Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs
31  12  Unity/MyGame/Assets/CardPresentation/Shell/DailyData.cs
5    1  Unity/MyGame/Assets/CardPresentation/Shell/MissionsTab.cs
8    4  Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs
```

（全用 `Edit` 工具逐段改，**没有** `sed -i`、**没有** python 文本模式写。）

---

## ⑤ 没查清 / 停手的部分

### ⑤-1 🔴 `Editor/RewardsScene.cs:1647` —— 本批做完**必红**，而它**不在我白名单**（我一个字没改）

- **现读**：`Editor/RewardsScene.cs:1582` 那一节（`Section("🆕 A63 progress 到顶换文案 + A64 …")`）里，
  `:1647` 是 `Check(TextOf(progFull63), "Missions/Completed", "到顶 ⇒ 显示 completedMessage —— 原版 prefab 里那个串本身…")`
  —— **按字面量断「键名」**。
- **本批之后**：那一格印的是 `Loc.T("Missions/Completed")` = **ZH「已完成」/ EN `Completed`** ⇒ **既不等于键名 ⇒ 必红**。
- **它该改成什么**（没做，因为不属我白名单）：与 `Editor/CollectionScene.cs:4650-4655` 现成的那套同形 ——
  先 `CheckTrue(Loc.HasEntry("Missions/Completed"), "（前提）…在表里")`，再 `Check(TextOf(progFull63), Loc.T("Missions/Completed"), …)`。
- ⚠️ 同处 `:1649` 的断言文案也是旧的（「它是原版的 **I2 词条【键】**、不是文案：本地没有语言表 ⇒ 照本仓先例照抄键本身」）⇒ 一并要改。
- **判据（为什么必须改而不是回退实现）**：`Core/Loc.cs:945` 自己就把 `Missions/Completed` 列在
  「**已经上屏的真缺陷**」那一族里；`A1012` 前半就是要修它。**回退实现 = 明知缺陷不修**。

### ⑤-2 `Shell/MainMenuSubmenuWindow.cs` —— 这个文件名**查不到**（简报里点了它）

- 简报写「`MenuWindowBase` / `MainMenuSubmenuWindow` / `RewardsWindow` 三处 `grep 'Loc\.'` 全 0 命中」。
- **现读**：`Shell/` 下**没有** `MainMenuSubmenuWindow.cs`（`ls Shell/ | grep -i 'submenu|mainmenu'` 只出 `MainMenuRuntime.cs` / `MainMenuRuntime.cs.meta`；
  `find . -iname "*submenu*"` 只命中 `Art/…/UI_Deck_Information_submenu_Back*.png` 两族图）。
- 另两个**属实**：`Shell/MenuWindowBase.cs` = 0 · `Shell/RewardsWindow.cs` = 0。
- **影响**：不影响本批结论（显示链已由 `Txt1 → MenuWindowBase.Text` 那一跳坐实）。**我要搜过的词**：`submenu`（文件名，大小写不敏感）、`mainmenu`（`Shell/` 目录名）。

### ⑤-3 `Editor/CollectionScene.cs` 那处注释改动的**性质声明**（白名单条件的边界）

- 白名单写「`Editor/CollectionScene.cs` —— **仅当你的改动会让它的现有断言变红时才改**」。
- **现读**：本批**不会让它变红**（键在表 ⇒ 闸门与裸调用同值；它断的是 `Loc.T(键)`，两版都成立）。
- 我仍改了它 —— 理由是**铁律 5**：那段注释**直接写着被删掉的那个函数名**，且把它当判据讲（「`TermOr` 是…⇒ 键不在表里时红得看不出原因」），
  **改完就成了假注释**（讨论一个不存在的符号）。改的是**纯注释**、**断言一字未动**。
  ⇒ **若调度台认为这越了白名单，请明确**：我可以立刻回滚这一处（`git checkout` 那一行由你做，或我按你的口径改回）。

### ⑤-4 「闸门还剩 10 处注释提及」的口径问题

见 ④ 验收 1 的红字。**没停手、也没自作主张删痕迹** —— 等调度台一句话。

### ⑤-5 没跑的

- ⛔ **没跑 Unity**（红线）；因此 `RewardsScene.Run` 那条红是**推出来的、不是跑出来的**（依据 = 断言按字面量比、实现已改；**未实跑验证**）。
- ⛔ 没动 `Editor/DeckScene.cs:4165`（`A1031③`「按字面量断『新卡组』」）—— 那是另一波，本批**确认它不受影响**（`DeckScene.cs` 里 `grep CheckText` = **0 命中**、`grep ImportError` 只断「非空」）。

---

## ⑥ 顺手发现的（⛔ 我一个字都没改 —— 全在白名单外）

1. 🔴 `Core/Loc.cs:1033-1034`（**已过期，需订正**）：写着
   「消费点 = `Shell/DailyData.DailyCounterText:175` → `Shell/MissionsTab.cs:549` 那一行（**今天那一行印的就是键名** —— 本行加进去就恢复正常）」。
   ⇒ 本批之后**前半的「今天」已不成立**（已是 `Loc.T`），且两个行号都漂了（现在 `DailyData.cs:191` / `MissionsTab.cs:553`）。
   建议改成「✅ 2026-10-18 `A1012` 前半已接上（`DailyData.DailyCounterText` 返回 `Loc.T(键)`）」。
2. 🟡 `Editor/RewardsScene.cs:1649` 的断言文案同上（见 ⑤-1）。
3. 🟡 **简报第 4 件（`Core/Loc.cs:1521` 注释仍写 `SettingsWindow.EchoText()`）—— 现读已被别人就地订正过**：
   旧名仍在 `Loc.cs:1524`（出处那一行），但**紧跟的 `:1525-1528` 已经有一段「⚠️ 2026-10-18 之后·第六会话就地订正（铁律 5，`A1064` 末条）」**，
   写明「`EchoText` 已改名 **`EchoFlash`**、返回值从 `string` 改成 `Func<string>`，落点 = `Shell/SettingsWindow.cs` 的
   `Func<string> EchoFlash(NetConfig.ExternalAddrs r)`」。⇒ **我核过同名改动确实在**（`SettingsWindow.cs:2749` 签名实读、
   `:3025` 调用点 `SetFlash(EchoFlash(_echoResult))`）；`git show HEAD` 里**没有**那段订正 ⇒ 那是**本会话另一个写手**加的。
   **不需要我再动**（`Core/Loc.cs` 也不在我白名单）。
4. 🟢 `Core/Deck/DeckEditorState.cs:407-411` 的注释说 `TypeLabels` 是「**闸门未开时两者相同**」—— 闸门已删，
   但这句的**事实描述仍成立**（`TypeLabels` 现在专供「英文档也能搜到」，与 `TypeLabelAt` 随语档取值并不相同）。
   属**别的笔的白名单**，未动、也不急。
5. 🟢 `FilterPanelModel.RarityNames`（`:376`）**改完只剩「事实落点」这一个用处**（不再参与显示）—— 已在它自己的 doc 里写明，未删（它记的是原版 `alternativeText` 原文，是判据）。

---

## 摘要（≤300 字）

`TermOr` 取词闸门（P1 留的静默兜底）已**整体删除**：`Core/FilterPanelModel.cs` 与 `Deck/DeckRuntime.cs` 两个本体删掉，7 个调用点全部收成**裸 `Loc.T(键)`**；全仓 `grep TermOr` **代码引用 0**（只剩 10 行注释里的「已删除」更正痕迹，铁律 5 要求保留）。因那 14 条键**在 HEAD 就已全在表里**（逐条核过 `git show HEAD:Loc.cs`），**界面零变化** —— 唯一变的是第 3 件：`Missions/Completed` 原先被**当文案直接印键名**（`DailyData.DailyCounterText` 里 `return CompletedMessage;`，而显示链 `MissionsTab→MenuWindowBase.Text` 从不查表），现改成 `return Loc.T(CompletedMessage)`，**只此一处实现**；中文档不变（「已完成」）、英文档从印键名变成 `Completed`。三处指定过期注释已就地订正，另顺手订正同族 5 处。类型检查 **0 错**，行尾全未翻（`DeckRuntime.cs` 仍 CRLF）。🔴 **一条真红预警**：`Editor/RewardsScene.cs:1647` 按字面量断 `"Missions/Completed"`，本批做完**必红**，需把期望值改成 `Loc.T(键)` —— 该文件不在我白名单，我没动。
