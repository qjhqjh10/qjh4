# W · 中英双语基础设施 + 两个设置窗的语言下拉（2026-10-17）

> 判据文件 = `资料/待办判据_卡面卡池与双语.md` **§23**（用户 2026-09-28 立项）；本件做的是它「⏭ 做法」的 **第 1、2 步**。
> 判据权威顺序照 CLAUDE.md：① 全量反编译 `d:/2/tools/decomp_full/` ② 解包资源 `assets_full/` ③ 成品卡图（本件用不到 ③）。
> 🔴 本件**没跑 Unity**（按简报）；断言写好**由主对话在收口时统一跑**。类型检查跑过（见 §⑤）。

## ① 结论

1. **`Core/Loc.cs` 已建**：一份语言表（**21 条词条**）+ `T(key)` + 当前语言（`PlayerPrefs` 键 **`"Language"`**、**全工程唯一一份**、读/写都 `PlayerPrefs.Save()`，照 `WarpforgeAudio.MusicPrefKey` 的写法）。
   默认 = **中文**；**12 项**下拉（枚举**照抄原版** `AvailableLanguages`，取值 0/10/…/110）；选到没有文案的语言（西/法/德/葡/俄/意/韩/日/捷/波）⇒ **回退英文 + `Debug.Log` 出声**（UI 上**不加**原版没有的提示行）。
   表里没有的键 ⇒ 返回**键名本身** + 出声（键名会画在界面上 = 看得见，⛔ 不是空串）。
2. **主菜单设置窗补了 General 页**（= 原版 `General Tab`，页签**第 1 个**，原版页签序也是它第一）：语言下拉 · 三颗开关 · 版本号 · 两颗钮 —— **逐项照原版**，几何/染色/字号每条都带反算算式（`Shell/SettingsWindow.cs` 那组 `Gen*` 常量）。
3. **对战设置窗补了 `Language Selector` 那一行**（原版 `BattleSettingsPanel/Language Selector`）：下拉框 + 框里当前语言名 + 箭头 + `Select Language` 标签；**点得动**（抬起那一帧触发，见 §④）。
4. **断言 ≈50 条**写进 `Editor/SettingsScene.cs`，其中把简报要求的那四条都落成**有鉴别力**的形式（见 §③）。
5. ⚠️ **三条已知偏离**（都记在代码注释里，按铁律 11「先记录、之后再完全复刻」）：
   ① **下拉的 12 行列表没建**（原版 `LanguagesDropdown > Template` 是 Unity 内置 `DropdownList` + 滚动视图）⇒ 改成「**点一下换下一个**」——与本窗图像页那颗**画质下拉同一种交互**；
   ② 原版那三颗开关的**消费者在服务器/匹配那侧** ⇒ 我们**只存值 + 点了如实出声**（用户 2026-09-28 口径允许）；
   ③ 页签那一列的**行高/间距仍是旧的**（我们 157.68×4 · 原版是 141.92 + spacing 8.92 · 5 个键）—— **本件没动版面**（那三页已收口过）。

## ② 语言表键清单（`Core/Loc.cs` 的 `Table`）

| 键名（= 原版 `Localize.mTerm`，逐条实读） | 中文（**我们译的**） | 英文（**原版 TMP `m_text` 原文**） | 出处 |
| --- | --- | --- | --- |
| `Settings/General/Title` | 通用 | `General` | `General Tab > Tab Title`（fs55） |
| `Settings/General/DisableBots` | 禁用机器人 | `Disable Bots` | `Checkboxes > Disable Bots`（fs42） |
| `Settings/General/DisableNotifications` | 禁用通知 | `Disable Notifications` | 同上 |
| `Settings/General/TouchInput` | 触摸输入 | `Touch input` | 同上（⚠️ 原版就是**小写 i**） |
| `Settings/General/RedeemCode` | 兑换码 | `Redeem Code` | `Bottom Buttons > Redeem Code`（fs40） |
| `MainMenu/Settings/ButtonLabel/Exit_Game` | 退出游戏 | `Exit Game` | `Bottom Buttons > Close Game Button`（fs38） |
| `MainMenu/Settings/ButtonLabel/SelectLanguage` | 选择语言 | `Select Language` | **两个窗共用**（bundle 里 43 处；对战那扇 `SelectLanguageText` 也是它） |
| `Settings/Online/Title` | 联机 | `Online` | 🔴 **自拟键**（原版没有「联机」页 —— 见 `SettingsWindow.cs` 文件头 ①） |
| `MainMenu/Settings/LanguageName/{English…Chinese}` ×12 | 英语/西班牙语/法语/德语/葡萄牙语/俄语/意大利语/韩语/日语/捷克语/波兰语/中文 | `English`…`Chinese`（= 枚举名） | 键的拼法 = 原版 `LanguageSelector.ResetLanguagesDropdown`（`String.Concat(前缀, 名字)` + `GetTermTranslation`）；**前缀字面量 `MainMenu/Settings/LanguageName/` 已在 `global-metadata.dat` 核到**（偏移 561102，紧邻 `…ButtonLabel/Exit_Game`）。选项名本体在**远端 I2 表** ⇒ 中/英两列是我们按“当前界面语言显示语言名”的语义填的 |

**自拟的键**（简报要求列出来）：`Settings/Online/Title` 一条；外加三个 `PlayerPrefs` 键 `DisableBots` / `DisableNotifications` / `TouchInput`（原版**不落盘**，它存玩家存档；三个 `GameStaticData` 字段的**正式字段名本地查不到** ⇒ 按开关名起）。落盘语言键用**用户口径**的 `"Language"`。

## ③ 改动清单

1. **`Core/Loc.cs`（新建，317 行）+ `Core/Loc.cs.meta`**：`AvailableLanguages`（12 值照抄原版）· `Languages[]` · `Current`/`Effective` · `T(key)` · `LanguageName` · `HasCjk`（**cap 0.72 em / 汉字 1 em 那条判据的唯一一份**）· `SetLanguage`（落盘）· `ReloadForTest`/`ResetForTest`/`RestoreForTest`/`PersistOverride` · 记账口 `MissingCount`/`FallbackCount`/`LastMissingKey`。
2. **`Shell/SettingsWindow.cs`**：`SettingsTab` 加 `General = 0`（其余 +1）· `BuildTabs` 加第 4 个键（**节点名恒为 `General`**，画上去的字走词条 —— 自检按节点名找）· 新增 `BuildGeneralPage`（页标题 / `VersionText` / `Language Selector`（`LanguagesDropdown` > `Label`+`Arrow`）/ 三颗 `GenToggleRow` / 两颗 `GenButton`）· `Gen*` 常量组（每条带反算算式）· `RefreshTexts`/`RefreshGenChecks`/`CycleLanguage` · `AlignRight` · `GeneralFlags`（三颗开关的值，`PlayerPrefs` + `PersistOverride`）· `RedeemCode`/`ExitGame`/`QuitNow`（点了**如实出声**）· 文件头 ③ 就地更正（3 页 → 4 页）。
3. **`Battle/SettingsPanel.cs`**：新增 `Lang*` 常量组（面板内 px，每条带反算算式）· `BuildLanguageRow(Z)`（框 + 当前语言名 + 箭头 + 标签，**字号按语种选**：`ApplyLangFont`）· `HitLanguage` + `LangPointerFrame`（**按下→抬起**一对边沿，抬起那一帧触发）· `RefreshTexts` · 一行 `BuildLanguageRow(Z)` 调用 + `SetActive` 里补四件。
4. **`Editor/SettingsScene.cs`**：① 页签那段 3 → 4（`names`/`pages` 两个数组 + 两个循环 + 「`General` 不建」那条改成「`Account`/`Support` 不建」）；② A171 字号扫描的允许表加三档 **34.2 / 25.2 / 16.2**（原版 38 / 28 / 18 × 0.9，每档写清出处，见 `Editor/SettingsScene.cs:2544`）；③ **两节新断言**（≈50 条，`Editor/SettingsScene.cs:584` 与 `:664`）。

**关键落点（行号只作定位、别当判据）**：`Shell/SettingsWindow.cs` 的 `SettingsTab`(`:73`) · `Gen*` 常量(`:309`) · `BuildGeneralPage`(`:768`) · `CycleLanguage`(`:926`) · `RefreshTexts`(`:942`) · `GeneralFlags`(`:1036`)；`Battle/SettingsPanel.cs` 的 `Lang*` 常量(`:60`) · `BuildLanguageRow`(`:479`) · `RefreshTexts`(`:533`) · `HitLanguage`(`:735`) · `LangPointerFrame`(`:754`)。

**四条验收对应**：① 12 项 = `Loc.Languages.Length == 12` + 首末两项的**枚举值字面量**（0 / 110）；② 切换后文案真的变 = **走真实点击链** `Click(row,"LanguageHit")`，且**两态自己定死**（先落 `Chinese` 再点 ⇒ `English`，隔着中/英边界 ⇒ 字必然不同；⛔ 不顺玩家当前的档点，那会从 `English` 换到 `Spanish` —— **两边都回退英文**、字一样 ⇒ 假红），然后断「框里那行字变了 + 那一行的标签变了（旧串 → 新串都打出来）+ 页签那行也变 + 底下钮也变」；③ 落盘/回读 = 写盘后读**字面量键名** `PlayerPrefs["Language"]`（0 与 110 两态）+ `Loc.ReloadForTest()` 模拟重开；④ 回退 = 选 `Japanese` ⇒ 取到的仍是英文那一列 **且** `FallbackCount` +1 **且**日志里有一条含「回退英文」（判别式：同一句英文在中文那列做过对照，两态真的分得开）。外加：缺键不静默（返回键名 + 计数 + 出声）、三颗开关**各翻各的**、版本号 = `"v"+Application.version`、两颗钮的几何/文案。

🔴 **还欠一条断言（不在本件白名单）**：对战那扇的 `Language Selector` 那一行**没有自检覆盖**（它的宿主是 `Editor/BattleScene.cs`，本件白名单里没有）。本件已经为它开好了只读口，接过去只要几条：`SettingsPanel.LanguageRowBuilt` · `LanguageCaptionText` · `LanguageLabelText` · `LanguageFieldWorldPos` · `LanguageFieldDrawnSize`（应 = 250×59.4 px ÷ 108）· `LanguageArrowWorldPos` · `LanguageArrowDrawnSize`（preserveAspect：46×19 内接进 20×20）· `HitLanguage(world)`，点击链喂 `PointerFrame(pos,true)` → `PointerFrame(pos,false)`（真实输入就是这两下）。
本件已经为它开好了只读口，接过去只要几条：`SettingsPanel.LanguageRowBuilt` · `LanguageCaptionText` · `LanguageLabelText` · `LanguageFieldWorldPos` · `LanguageFieldDrawnSize`（应 = 250×59.4 px ÷ 108）· `LanguageArrowWorldPos` · `LanguageArrowDrawnSize`（preserveAspect：46×19 内接进 20×20）· `HitLanguage(world)`，点击链喂 `PointerFrame(pos,true)` → `PointerFrame(pos,false)`（真实输入就是这两下）。

## ④ 没查清的部分（⛔ 都是「查不到」，不是「猜了没写」）

1. **原版下拉那 12 行列表的项高/框**：本地有（`Template` 框 `356.10×516.56`、项图 `40K_dropdown_item*` 717×92），但**本件没建**（见 §①-①）。
2. **General 页三行勾选框的**子件**尺寸**：`menu_dump` 自己标「⚠️ 主轴尺寸算不准 ⇒ 别照抄」（`HorizontalLayoutGroup ctrlW=1`，印出来是 0.00 宽）⇒ 用了**隔壁那颗同族开关**（`battlearena1` 的 `Auto Zoom Toggle/Toggle`，同组件族 + 同一对图 + 同行高）的 `74.0616×57.6656` 与「标签左沿 = 行左 + 79」；两条独立印证：同族开关 + **两处中间那条 4.94px 的缝逐值相同**。
3. **那三颗勾选框自己的 `m_Colors`**：`menu_dump` **不印**它，逐文件反查的脚本这次没打通 ⇒ 底色那个绿（`(0.2863,0.9647,0.6863)`）是**同族推断**（来源 = 那颗 `Auto Zoom Toggle` 的 MB 实读），不是本页实读。
4. **下拉箭头那颗 `Image.m_Sprite`**：PathID `-1891211968353393973` 本地**没解出名字** ⇒ 图名 `40K_dropdown_arrow_closed` 是**我们的选择**（工程里另外两处下拉就是这么接的）。
5. **原版中文文案**：远端 I2 表（84 个 bundle 无本地化包）⇒ 本件所有中文**都是我们译的**（表中已标）。
6. **`DisableBots` 等三格在 `GameStaticData` 里的正式字段名**：签名桩不带偏移注释 ⇒ 只按偏移（+0xeb / +0xe8 / +0x11d）与**开关名**对应。
7. ⚠️ **`Editor/MainMenuScene.cs:614` 的断言文案已经过期**（写着「三个页签：图像 / 音频 / 联机」，现在是 4 页）—— 那个文件**不在本件白名单**，请主对话派件改掉（断言本身只断 `Tab Buttons` 存在，不会红）。

## ⑤ 类型检查结果（原样贴，`TMPDIR=/tmp/wf_loc bash d:/4/Unity/工具/typecheck.sh`）

```text
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（中途出现过 `Deck/DeckRuntime.cs` 与 `Editor/MainMenuScene.cs` 的报错 —— **都不是本件的文件**，是别的代理当时正写到一半；等它们写完之后重跑即 0/0。）
