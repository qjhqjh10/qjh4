# W · 设置窗双语线（四笔：`A1055` / `A1059` / `A1048` / `A1049`）

> 白名单（本件**实际只写了 1 个文件**）：`Editor/SettingsScene.cs`（**纯新增断言**，+80/−0）。
> ⛔ 没跑 Unity（按用户本轮口径：待办没做完之前不跑自检）· ✅ 秒级类型检查跑了（两次，0/0）· ⛔ 没碰 git 的写操作（只 `git diff --numstat` 只读）。
> 🔴 **简报的两条前提被现读推翻**（见 ⑥·1、⑥·2），下面每一条都按现读写。

---

## ① 结论（四笔各一句）

| 笔 | 简报说什么 | 现读是什么 | 我做了什么 |
|---|---|---|---|
| `A1055` 设置窗几页没接语言表 | 「**要做**」：页标题 / 音频页三滑块行标签 / `Auto zoom` / `BuildCheckRow` 一族 | 🔴 **生产代码已全部接完** —— 一部分在 `HEAD` 就有（音频三根 + `Auto zoom`），一部分在本工作区**未提交**（页标题 / 四个页签 / 图像页 6 处 `BuildCheckRow` 一族） | **零代码改动**（只逐处现核 + 补断言，见 ②·B） |
| `A1059` 四处「键已在表没接」 | 「要接线」 | 🔴 **四处全在 `HEAD` 里就接好了**（`git show HEAD:…SettingsWindow.cs` 逐处对过） | **零代码改动**；本件给它补了**缺的那条断言**（三根音量键，`HEAD` 与本工作区**都没有**） |
| `A1048` 换语言不刷新（真缺陷） | 「要照 `_genLabels` 补 `_onLabels` 链」 | ✅ **链已建**（`Shell/SettingsWindow.cs:715` 字段 · `:1351-1364` 扫它 · `:1391-1401` 两个登记口）且**已覆盖 8+ 件** | 生产代码零改动；**补了这条链上原来照不到的两件断言**（联机页说明行 = 两条键拼 · 音频页说明行） |
| `A1049` `**` 星号会不会印出来 | 「查一次 `ShowPopUp`」 | 🔴 **会印**（真缺陷口径）—— 判据见 ④·4；⛔ 按既有裁定**一个值都没动** | 只回答「印不印」，**零改动** |

**一句话**：这四笔的**生产代码在这一轮之前就已经落地**（简要是照更早的快照写的）；真正的缺口是**断言**，本件把 `A1059`（三根音量键）+ `A1048`（`_onLabels` 链上那两件拼串/单人件）补上了，并**回答了 `A1049`（会印）**。

---

## ② 改动清单

### ②·A 生产代码 —— **本件零改动**（下面全是「改前 → 改后」的**现核存档**，不是本件做的）

基准 = `git show HEAD:Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs`（3,366 行）vs 现读（3,520 行）。

| # | 落点（现读） | 改前（`HEAD`） | 改后（现读） | 键名来源 |
|---|---|---|---|---|
| 1 | `:1790` | `PageTitle(page, "Graphics");`（HEAD `:1732`） | `OnLangText(PageTitle(page, Loc.T(lkGfxTitle)), () => Loc.T(lkGfxTitle));` | `Settings/Graphics/Title` = **原版 `mTerm`**（`Loc.cs:203`，波 0b3 查实：`MonoBehaviour_…8895938149081907110.json` `"mTerm"` 同键两处，GO 名 `Graphics Tab`） |
| 2 | `:2593` | `PageTitle(page, "Audio");`（HEAD `:2474`） | `OnLangText(PageTitle(page, Loc.T(lkMediaTitle)), () => Loc.T(lkMediaTitle));` | `Settings/Media/Title` = **原版 `mTerm`**（`Loc.cs:204`，GO 名 `Media Tab`） |
| 3 | `:969-972` | 三颗页签 `Key = (string)null`（HEAD `:959-962`） | `Key = lkGfxTitle` / `lkMediaTitle` / `lkOnTitle` | 同上两条 + `Settings/Online/Title`（**自拟**，联机页原版没有） |
| 4 | `:1942-1943` / `:1953-1954` / `:1967-1969` / `:1975-1977` | `BuildCheckRow(… )` 不传 `labelText`（HEAD `:1894-1898`）⇒ **画的就是节点名** | 四行各传 `ssuText` / `azText` / `ssAaText` / `vsText`（`Loc.T` 工厂）+ 登记 `_gfxRowLabels` | `Settings/Graphics/{IncreaseUISize,AutoZoom,EnableSuperSampling,Vsync}`（`Loc.cs:1695/1041/1696/1697`） |
| 5 | `:2341` / `:2344` | `Text(…, FpsTickText[i], …)`（HEAD `:2243`） | `i == 2 ? Loc.T(lkGfxUnlimited) : FpsTickText[i]` + 第 3 格登记短链 | `Settings/Graphics/UnlimitedFPS`（`Loc.cs:1699`） |
| 6 | `:1805` / `:1808` | `Text(row, "Quality selector text", "Quality", …)`（HEAD `:1743`） | `Loc.T(lkGfxQuality)` + `OnLangText` | `Settings/Graphics/SelectQuality`（`Loc.cs:1694`） |
| 7 | `:2306-2310` | `Text(_fpsRow, "Title", "FPS limit", …)`（HEAD `:2215`） | `Loc.T(lkGfxFrameLimit)` + `OnGfxRowText` | `Settings/Graphics/FrameLimit`（`Loc.cs:1698`） |
| 8 | `:2601` / `:2625` | ✅ **HEAD 就已接**：`Text(rowN, "Label", Loc.T(keys[i]), …)`（HEAD `:2498`） | 同一句 + `OnLangText(lb, () => Loc.T(keys[k]))` | `MainMenu/Settings/SettingLabel/{Music,SoundFx}` · `Settings/Media/VoiceOvers`（`Loc.cs:690/691/692`，**原版 TMP `m_text` 逐字**） |
| 9 | `:1952-1954` | ✅ **HEAD 就已接**：`azText = () => Loc.T(lkGfxAutoZoom)`（HEAD `:1883`） | 同上（本件只核） | `Settings/Graphics/AutoZoom`（`Loc.cs:1041`） |
| 10 | `:2669-2721` | 联机页页标题 / `RoleButton` / `IP`·`Password` 标签 / 两个占位 / 说明行 / 测外网 / 刷新 / 保存 / 检查连接 | 全部 `Loc.T(键)` + `OnLangText` / `OnLangPlaceholder` | `Settings/Online/*`（表里全在） |

> 🔴 **`names`（`"Music"` / `"Sound Effects"` / `"Voice-overs"`）一个字没动** —— 它现在只喂**节点名**（`name + " Container"`），文字另走 `keys`（`Loc.cs:669` 那条注释里的口径：节点名不进本地化）。
> 🔴 **节点名一律没动**：`Graphics` / `Audio` / `Online` / `Small Screen UI` / `Auto Zoom` / `Use super sampling` / `VSync` / `30 FPS` / `Note` …（自检 `FindChild` / `Click` 全靠它们）。

### ②·B **本件唯一写的文件** —— `Editor/SettingsScene.cs`（新增断言，+80/−0）

| 落点（改后） | 改前 | 改后 | 判据/键 |
|---|---|---|---|
| `:2854-2887`（probe 表内新增 4 格） | 波 1b 那张 probe 表**只有图像页 + 联机页**，**音频页 0 格** | 新增 4 条：`Media Tab > Audio Settings > {Music,Sound Effects,Voice-overs} Container > Label` 各 1 + `Media Tab > Note` 1 | `MainMenu/Settings/SettingLabel/Music` · `…/SoundFx` · `Settings/Media/VoiceOvers` · `Settings/Media/AudioMixerNote` |
| `:2951-2988`（新增 ④′） | 无（**拼两条键那种形状照不到**） | 联机页说明行**整句**两档各断一次 + 2 条灭自证 + 1 条旁证 | `Settings/Online/TitleNote` + `"\n"` + `…/TitleNoteBody`；旁证 = `win.OnLabelCount >= 15` |
| `:2990-3010`（新增 ④″） | 无 | **反向断**：`FPS Slider` 前两格刻度两档下都必须是**纯数字** `30` / `60` | 表 A 明写这两格「不建键」（原版那两颗 TMP 就是 `30` / `60`） |

- ⛔ **一处既有断言都没动**（改动是纯追加：`git diff --numstat` 从 `391/16` 变成 `471/16` ⇒ **+80 / −0**）。
- ⚠️ **越了一点白名单字面**：派单写「`Editor/SettingsScene.cs`（**仅当你的改动让它的断言变红时**）」，而本件是**新增**断言 —— 依据是**验收标准 3 / 4**（`A1048` 那条链**必须有断言**、至少一条**灭自证**），而 `A1059` 的判据原文里「断言宿主 = `Editor/SettingsScene.cs`」也是写死的。**如调度台不认，删掉这三段即可，生产代码不受影响**（它们是纯追加）。

---

## ③ 新键清单

**本件新建 0 条键**（`Core/Loc.cs` 一个字没动）。涉及的四笔用到的键**全部现读在表**：

- 页标题 / 页签：`Settings/{General,Graphics,Media,Online}/Title`（`Loc.cs:203/204/210` + General 那条）
- 音频三根：`MainMenu/Settings/SettingLabel/{Music,SoundFx}` · `Settings/Media/VoiceOvers`（`Loc.cs:690/691/692`）
- 音频说明行：`Settings/Media/AudioMixerNote`（`Loc.cs:1232`）
- 图像页：`Settings/Graphics/{AutoZoom,SelectQuality,IncreaseUISize,EnableSuperSampling,Vsync,FrameLimit,UnlimitedFPS}`（`Loc.cs:1041/1694/1695/1696/1697/1698/1699`）
- 联机页：`Settings/Online/*`（`Title`/`TitleNote`/`TitleNoteBody`/`Role{Host,Client}`/`IpLabel`/`PasswordLabel`/`IpPlaceholder`/`PasswordPlaceholder`/`TestPublicIp`/`Refresh`/`Save`/`CheckConnection`/`Ok` …）
- 表规模：`new Entry(` = **425**（开工时现数，与简报「刚有人加过 12 条 ⇒ 别按旧数推算」一致）。

---

## ④ 证据

1. **`A1055` 已做完的判据**（两条独立路）：
   - `git show HEAD:…/SettingsWindow.cs`（3,366 行）里还是 `PageTitle(page, "Graphics")`（`:1732`）/ `PageTitle(page, "Audio")`（`:2474`）/ 三颗页签 `Key = (string)null`（`:959-962`）/ `BuildCheckRow(… )` 不传 `labelText`（`:1897/1900`）；
   - 现读（3,520 行）逐处已是 `Loc.T(键)` + `OnLangText` / `OnGfxRowText`（行号见 ②·A）。
   > ⇒ **不是「有人做了一半」，是「做完了、A 表没跟上」**（本轮第 3 个例子；同族见 `调度台_日志.md` §一）。
2. **`A1059` 已在 `HEAD`**：`git show HEAD:…/SettingsWindow.cs` 的 `:1883` `azText = () => Loc.T(lkGfxAutoZoom)` · `:2498` `Text(rowN, "Label", Loc.T(keys[i]), …)` —— 与现读**逐字相同**。
3. **`A1048` 链的形状**（现读）：字段 `:715 readonly List<Action> _onLabels`；`RefreshTexts()` `:1355-1364`（`_genLabels` → `_onLabels` → `_gfxRowLabels` 三条 for）；登记口 `:1391-1401`（`OnLangText` / `OnLangPlaceholder`）；`Build()` `:881` 与 `_genLabels` 一起 `Clear()`。**登记点现读 19 处**（图像页 2 + 音频页 5 + 联机页 12）。
4. **`A1049` 判据（会印）**——三跳逐跳现读，**三跳都不转换、不剥**：
   - ① `WindowsManager.ShowPopUp`（`Shell/WindowsManager.cs:1492`）→ `ShowMessagePopUp` → `PopUpGameWindow`（`Shell/PopUpGameWindow.cs:371` `MenuDraw.Text(winNode, msgR, Term(_msgKey), …)`；`:170` `Term()` 在 `Terms` 空表时**原样返回**）；
   - ② `MenuDraw.Text`（`Shell/MenuDraw.cs:1753-1776`）**不做任何字符串处理** → `Label.SetText`（`Battle/Label.cs:156`）→ `SetTextTmp`（`:1011-1017`）= **`_tmp.text = text`（原文赋值）**；
   - ③ TMP 的富文本**只认 `<`** —— 包源码实证：`Library/PackageCache/com.unity.ugui@27635d171a/Runtime/TMP/TMP_Text.cs:956` `protected bool m_isRichText = true;` · `:2278 if (c == '<' && m_isRichText)` · `:4018 if (m_isRichText && charCode == 60)  // '<'` ⇒ **`*`（0x2A）从不进富文本分支**，`**` 逐字画出来（富文本开着也一样）。
     ⚠️ 点阵兜底那条路也只剥 `<tag>`：`Battle/Label.cs:166` → `CardIcons.StripTags`（`Core/CardIcons.cs:136-140`，正则 `</?[a-zA-Z][^>]*>`）⇒ 星号照样画。
   - **普查（双语两张表都算）**：`Core/Loc.cs` 里**值里含 `**` 的条目 = 26 条**（不是记录里的 14），且**中英两列都有 `**`**（脚本逐条解 `new Entry("ZH","EN")` 两个字面量后判定）：
     `MenuDeck/Error/{EffectOnlyCard,ImportNotPersisted,WrongGameModeDeck,NoUsablePrebuilt}` · `Settings/General/{RedeemCodeUnavailable,LangHasNoTable}` · `Settings/Online/PublicAddress/{Mismatch,BothOk}` · `Settings/Online/HowToConnect/{PublicDirect,DontUseTestSite,PublicV6No,UpnpNote}` · `Settings/Online/MatchCancelled` · `Settings/Online/Lobby/{PlayedVsBot,StartAfterCancel,MissedCancel,PeerCancelled,ModeMismatch}` · `Settings/Online/Cancel/WhyStarted` · `Settings/Online/Echo/NoEcho` · `Settings/Online/Upnp/{NoResponse,NoService,PortTaken,Rejected,Cgnat,Ok}`。
     其中 **`HowToConnect/*` 那 4 条**正是玩家点联机页那行说明弹出来的「怎么联机」窗（`Shell/SettingsWindow.cs:2975-3008` 把 12 条键拼成一句 → `wm.ShowPopUp`）⇒ **那个窗的中文/英文正文里会各印一批 `**`**。
   - ⛔ **按既有裁定一个值都没改**（`**` 是调用点原文自带的字符 ⇒ 去掉 = 悄悄改文案）。
5. **键存在性交叉核**（脚本，`Shell/SettingsWindow.cs` × `Core/Loc.cs`）：`lk*` 常量 **81 条全部**在 `Loc.cs` 的 425 条键里；文件里 **`Loc.T("字面")` = 0 处**；**零调用点常量 = 0** ⇒ **不存在「裸 `Loc.T` 打空」**（验收标准 2）。
6. **中文档 / 英文档可见变化**（验收标准 5，逐条标）：
   - **中文档会变字**（英文 → 中文）：`Graphics`→`图像` · `Audio`→**`媒体`** · `Quality`→`画质` · `FPS limit`→`帧率上限` · `Unlimited`→`不限帧` · `Small Screen UI`→`小屏 UI` · `Use super sampling`→`超采样` · `Auto zoom`→`自动缩放` · `Music`→`音乐` · `Sound Effects`→`音效` · `Voice-overs`→`语音`。**原因 = 中文档原来印的就是英文**（键的 ZH 列是中文）⇒ 这是**修对了**，不是「改文案」（第四会话已裁、见 `交件_设置窗未接的标签.md` §①）。
   - 🔴 **英文档唯一一处变字 = 音频页页签与页标题 `Audio` → `Media`**（键 `Settings/Media/Title`，波 0b3 三重判据：键名 + GO 名 `Media Tab` + 同 GO TMP 西语译文 `Multimedia`）——**只此一处，且是有意为之**（已进 `A1063` 记录）。其余各条 EN 列与改前**逐字相同**。
   - 中文两列里 `音效` / `语音` 是 `Loc.cs` 自己标「**自拟**」的（真值在远端 I2 表）⇒ **不是原版文案**（`Loc.cs:691/692` 自己写着）。

---

## ⑤ 验证

| 项 | 读数 |
|---|---|
| 秒级类型检查（改前基线） | `TMPDIR=/tmp/wf_set1 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数: 0 / 编辑器错误数: 0** |
| 秒级类型检查（改后） | 同上 ⇒ **0 / 0** |
| 行尾（`Editor/SettingsScene.cs`） | 改前 **CRLF 0 / LF 3433** · 改后 **CRLF 0 / LF 3513**（**纯 LF 未翻**，计数用 `io.open(p,'rb')`） |
| 行尾（其余三个白名单文件，本件**只读**） | `Shell/SettingsWindow.cs` CRLF 0 / LF 3520 · `Core/Loc.cs` 0 / 1975 · `Shell/SearchingMatchPopup.cs` 未动 |
| `git diff --numstat`（`Editor/SettingsScene.cs`） | 改前 **391/16** → 改后 **471/16**（**+80 / −0** = 纯追加，不是整篇重写） |
| 键存在性 | 见 ④·5（81/81 常量在表 · 0 处 `Loc.T("字面")`） |
| Unity 自检 | **按用户本轮口径一条都没跑**（`-executeMethod` / `_run_8_checks.sh` 均未调用）⇒ ⚠️ **「切档那一刻字真的跟着变」目前只有代码判据，要等收口那趟自检才算数** |

---

## ⑥ 没查清 / 停手的

1. 🔴 **简报「判据原文 → `资料/待办判据_第四会话.md` `## §A1055`（整节读）」不成立** —— 那份文件（344 行）里**只有 `§A1059`**（`:322-324`），**没有 `§A1055` / `§A1048` / `§A1049` 三节**。本件改从 `项目任务.md:648/649/655` 三行原文 + `资料/普查产出_第四会话/交件_{设置窗未接的标签,换语言刷新链}.md` 取判据。**建议调度台把这三节补进判据文件**（或把指针改成那两行）。
2. 🔴 **简报「`Shell/SettingsWindow.cs`（:1621 `"Graphics"` · :2344 `"Audio"`）写死英文」也是过期快照** —— 现读那两处已接键（②·A #1/#2），且简报给的行号在现读里**都不是**那两句。
3. ⚠️ **`Shell/SettingsWindow.cs` 是双写手共用的**（铁律 13·3 的隐患，**本件没踩到**）：`W_外壳量法线`（`A990②`）在**本会话 23:34** 改了它的 `SetFpsFromPointer`（现读 `:2465-2481`，`LayoutSpace.ToPixel` → `MenuDraw.PixelOfDesign`），而**派单把整份文件放在我的白名单里**。我据此**一个字节都没写这个文件**（也因为 ②·A 那 10 处本就做完了）。⇒ **建议：下批把 `SettingsWindow.cs` 按行段切开授权**（同族的 `Core/Loc.cs` 也在我的白名单里，而 `P6d` 23:28 刚写过它 —— 我同样没动）。
4. ⚠️ **`A1049` 只回答了「印不印」，没给修法**（按裁定：值不许动）。**「怎么修」仍是开着的账** —— 可选项有 ①把 26 条的两列 `**` 去掉 ②给显示口加一层 markdown→TMP 转换（`**x**`→`<b>x</b>`，并把 `richText` 显式置 1）③保持现状并在正本标注「玩家会看到星号」。**这一条要调度台裁**（我只做 ①「查」那一半）。
5. ⚠️ **`**` 那批键的**展示口**我只点了两处**（`ShowPopUp` 的「怎么联机」窗 + 联机页状态行 `_flash`）；`MenuDeck/Error/*` 那 4 条走的是**卡组编辑**的错误弹窗（`Deck/DeckRuntime.cs` 的 `ShowInvalidDeckPopUp`），**同一条 `ShowPopUp` 路**⇒ 结论一致，但**没逐处现核调用点**（不影响「印不印」这个结论）。
6. ⚠️ **`BuildCheckRow` 的 `labelText == null ⇒ 画 nodeName` 这条兜底现在 0 调用点**（4 行全传了）—— 它是**留着当兜底**还是该删，**没判**（删了会改行为，不删就是一条死分支）。如实报。

---

## ⑦ 顺手发现的（**一处都没改**）

1. 🔴 **`Loc.cs` 的 `**` 条目是 26 条、不是记录里的 14 条**（`资料/待办判据_*` 与本轮简报都写 14）——而且**中英两列都有**（不是「只有中文那列」）。⇒ 正本那条计数要订正（铁律 5）。
2. 🔴 **「怎么联机」窗（`ShowHowToConnect`）是 `**` 最集中的展示口**：它把 12 条键拼成一句（`Shell/SettingsWindow.cs:2975-3008`），其中 4 条带 `**` ⇒ **玩家一打开就看见 6 组星号**。同族：联机页状态行那 12 条 `Upnp/PublicAddress/Lobby/*`。
3. ⚠️ **`Loc.cs` 的一条注释已漂**：`Shell/SettingsWindow.cs` 音频页那三行的注释（`Loc.cs:686-689` 那一族）写「父链 `BattleSettingsPanel < Volume Sliders < …`」—— 那是**战斗侧**的父链；设置窗这一侧的父链是 `Media Tab > Audio Settings > {Music,Sound Effects,Voice-overs} Container > Label`（现读）。**两处同名键、两条父链**，注释只写了前者（不影响判据，影响「按父链反查」时的定位）。
4. ⚠️ **`SetFpsFromPointer` 的改法与它自己 doc 的「剩余偏差」**（`Shell/SettingsWindow.cs:2465-2481`）自陈：`UpdateFpsDrag` 里仍有两处 `LayoutSpace.PxX/PxY`（不在那件白名单里）⇒ 与 `A990②` 的「非 16:9 真点偏」是**同一条尾巴**，别当成已收口。
5. ℹ️ `Editor/SettingsScene.cs` 里 `Click(Bar(root), "Audio")`（音频页那一节）点的是**节点名** `Audio`，而页签上画出来的字现在中文档是「媒体」⇒ **「按显示字找节点」这条路已经不通**（所有自检都按节点名走 ⇒ 现在没事，但将来谁按字找会静默找不到）。
6. ⚠️ **断言夹具的一条口径差**（**本轮没改，如实报**）：`FindChild` 用的是 `GetComponentInChildren<Transform>(**true**)`（含 inactive），而 `TextOf` 用的是 **单参** `GetComponentInChildren<Label>()`。本仓另一处记录明确警告「**单参那版只找激活的对象**」（`Editor/MainMenuScene.cs:10966` 的同族教训）。而本节实测是**绿**的（`历史/A表已收口_第五会话.md:64`：`SettingsScene` **698/0**）⇒ **本件照同一形状写**（音频页 / 图像页那一列在那一刻都是**未显示**的页，读得到）。⛔ 但这条口径差仍是**将来会咬人的地雷**（谁把某个件挪到「出厂就 `SetActive(false)`」时，单参版会把它读成 null、断言**静默变红或静默跳过**）⇒ 值得单开一小账。

---

## 摘要（≤300 字）

四笔里**三笔的生产代码早在这一轮之前就做完了**，简要是照旧快照写的：`A1055`（页标题/页签/图像页 6 处 `BuildCheckRow` 一族）在本工作区**未提交**地做完、`A1059` 四处与 `A1048` 的 `_onLabels` 链在 **`HEAD` 里**就接好了；现读逐处对过 `git show HEAD:…SettingsWindow.cs`（3,366 行）vs 现读（3,520 行），并交叉核了 81 条键常量全部在表、`Loc.T("字面")` 0 处。真正的缺口是**断言**：音频页 4 件（`A1059` 三根音量键 + `A1048` 那条说明行）与联机页「两条键拼一行」那件**一条断言都没有** ⇒ 本件在 `Editor/SettingsScene.cs` **纯追加 +80 行**（4 格两语档 probe + 拼串行的整句断言 + 2 条灭自证 + 反向前两格刻度 + 旁证），类型检查 0/0、行尾纯 LF 未翻。`A1049` 查清并回答：**星号会印** —— `ShowPopUp`→`PopUpGameWindow`→`MenuDraw.Text`→`Label.SetTextTmp`（`_tmp.text = text` 原文赋值，三跳都不转换），TMP 富文本只认 `<`（`TMP_Text.cs:956/2278/4018`），点阵兜底也只剥 `<tag>`；带 `**` 的键 **26 条、中英两列都有**（不是 14 条），按裁定一个值都没动。未跑 Unity 自检（用户本轮口径）。另报：`SettingsWindow.cs` 被两个写手共用（`W_外壳量法线` 23:34 改过它），本件一个字节没碰。
