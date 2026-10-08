# 交件 · 断言波 —— `Editor/SettingsScene.cs`（上游：波 1b 设置窗接线）

> **写手**：动手写手（有 Edit/Write/Bash）· **2026-10-19**（本轮）
> **任务**：把本宿主的断言改成**跟着语言走**，并**补上波 1b 接线该有的断言**。
> **白名单**：✅ 只改了 `Unity/MyGame/Assets/CardPresentation/Editor/SettingsScene.cs`（**唯一**一个文件）。
> ⛔ 没跑 Unity · ⛔ 没动 git（只读跑 `git diff --numstat`）· ⛔ 没碰 `Shell/SettingsWindow.cs` / `Core/Loc.cs` /
> `Editor/BattleScene.cs` / `Editor/MenuCheck.cs` / `RuleEngine/**` / `项目任务.md` / `CLAUDE.md` / 其它 `资料/*.md`。

---

## ① 逐条：改了哪 10 处既有断言（行号 = **改完之后**）

| 文件:行 | 改前断言 | 改后断言 | 为什么 | 它现在分得出哪两种状态 |
|---|---|---|---|---|
| `SettingsScene.cs:565-576`<br>（原 `:543-546`） | `string wantTab = names[i]=="General" ? Loc.T("Settings/General/Title") : names[i];` | 期望值一律 `Loc.T(tabTitleKeys[i])`；新增 `tabTitleKeys` 表（`:533-538`） | 波 1b 把 **四个页签全接上词条**（`Graphics/Audio/Online` 原来 `Key=(string)null`、照 `Label` 原样画）⇒ 拿**节点名**当期望值**两档各红几条**：中文档 `图像/媒体/联机` ≠ `Graphics/Audio/Online`（红 3 条）；英文档 `Audio` 那格印的是 `Media`（红 1 条） | **接没接词条** + **当前是哪一档**（中文档 3 条、英文档 1 条，与旧写法的红/绿矩阵不同） |
| `SettingsScene.cs:1004-1013`<br>（原 `:975`） | `win.Flash.Contains("画质")` | `win.Flash.StartsWith(TermHead("Settings/Graphics/Flash/Quality"))` | 那句 `_flash` 改走词条（`SettingsWindow.cs:2070`）⇒ 英文档是 `Quality → …`，**写死中文的需子串只在中文档成立** | **`_flash` 按当前语档拼 vs 写死中文字面量**（后者在英文档红） |
| `SettingsScene.cs:1301-1307`<br>（原 `:1263`） | `…Contains("帧率上限")` | `…StartsWith(TermHead("Settings/Graphics/Flash/Fps"))` | 同上（`SettingsWindow.cs:2396`）；英文档 `FPS limit → …` | 同上 |
| `SettingsScene.cs:1309-1314`<br>（原 `:1265`） | `string[] tickTxt = { "30", "60", "Unlimited" };` | `{ "30", "60", Loc.T("Settings/Graphics/UnlimitedFPS") }` | 第 3 格那行**字**改走词条（`SettingsWindow.cs:2341`）⇒ 中文档是 `不限帧`。`[0]/[1]` 是纯数字，**不建键**、不换 | **第 3 格接了没有** + **当前语档**（中文档旧写法红） |
| `SettingsScene.cs:1321`<br>（原 `:1272`） | 同一循环里那句 `CheckTrue(TextOf(tk) == tickTxt[i], …)` | 同式子（数组改了值）+ 文案分 i==2 / 其它两种 | 同上（期望值随数组一起跟语言走） | 同上 |
| `SettingsScene.cs:1332-1338`<br>（原 `:1279-1280`） | `TextOf(ttl) == "FPS limit"` | `== Loc.T("Settings/Graphics/FrameLimit")` | 行标题接词条（`SettingsWindow.cs:2306`）⇒ 中文档 `帧率上限`。⚠️ 旧写法**英文档恰好绿**（EN 列逐字就是 `FPS limit`）—— 只断一种情况看不出来 | **接没接词条 + 当前语档**；旧写法在英文档**假绿** |
| `SettingsScene.cs:1494-1497`<br>（原 `:1436`） | `win.Flash.Contains("Small Screen UI")` | `…StartsWith(TermHead("Settings/Graphics/Flash/SmallScreenUI"))` | 需子串改走词条前缀（⛔ 不再抄「本来就长得像英文」的那个中文列字面量） | **键值真读过 vs 只是碰巧含那串**（键的中文列一改，旧写法假红） |
| `SettingsScene.cs:1574-1576`<br>（原 `:1514`） | `…Contains("Auto Zoom")` | `…StartsWith(TermHead("Settings/Graphics/Flash/AutoZoom"))` | 同上 | 同上 |
| `SettingsScene.cs:1664-1670`<br>（原 `:1603-1605`） | `TextOf(ssOn) == "Use super sampling"` | `== Loc.T("Settings/Graphics/EnableSuperSampling")` | 那一行的字接词条（`SettingsWindow.cs:1966`）⇒ 中文档 `超采样`。⚠️ 旧写法**英文档恰好绿** | **接没接词条 + 当前语档** |
| `SettingsScene.cs:1681-1683`<br>（原 `:1616`） | `…Contains("super sampling")` | `…StartsWith(TermHead("Settings/Graphics/Flash/SuperSampling"))` | 同上 | 同上 |

**共 10 条既有断言**（表里第 4/5 行是同一处的两句话，算一处）。

### 新增的辅助口（1 个）

| 行 | 是什么 | 为什么 |
|---|---|---|
| `SettingsScene.cs:231-246` | `static string TermHead(string key)` = 把一条**带 `{0}` 的词条**的**固定前缀**取出来（`{0}` 之前那一截） | `_flash` 那一族完整那句里夹着**运行期值**（画质档名 / `FpsText()`）⇒ 拿不到整句；而**前缀那一截才是词条的、跟着语档走**。⛔ 不写死中/英（那只是把红从这一档挪到那一档） |

---

## ② 新增的断言（`SettingsScene.cs:2783-3090`，一整节）

**位置**：接在 `:2776`（`win.TryOpen(null)`，A94 收尾重开窗）之后、A171 字号节之前 —— 那里整棵树是**刚重建过的新树**，且后面几条（A171 只量字号）不受语言影响。

**节标题**：`波 1b：接进语言表的那批字（四页签 / 四个页标题 / 图像页 6 处 / 联机页 8 处）—— 两语档各断一次`

> ⚠️ **「联机页 8 处」 vs 上游写的「9 条」**：上游 ③ 那张刷新链表写「联机页 9 条」，而它自己的 ①-E 逐行表
> 只有 **8 行**（RoleHost · RoleClient · TestPublicIp · IpLabel · PasswordLabel · Refresh · Save · CheckConnection）。
> 差额 = **`IP Label` / `Password Label` 各被建两次**（`BuildRoleBlock` 主机块 + 客机块各一次 ⇒ 登记点 4 处、
> **节点 8 → 登记 10**）。本节 22 条探针**按【节点】**点：主机块那两颗标签各一条，客机块走的是**同一个
> `BuildRoleBlock`**（同一条代码路径）⇒ 不重复点第二块。

### ②·1  22 处「节点上的字 == `Loc.T(键)`」——**中/英各断一次**

用**匿名对象数组** `probes`（`What` / `Key` / `Differ` / `Find`，四个字段绑在一起 —— ⛔ 不用平行数组，
那样改一行会把「名字/键/取法」错位到隔壁、**静默验错对象**）。取节点一律**闭包现取**（`Build()` 会整棵重建）。

| 组 | 处数 | 节点（全是**原版/既有节点名，一个字都没动**） | 键 |
|---|---|---|---|
| 四页签 | 4 | `Bar(root)` → `General` / `Graphics` / `Audio` / `Online` | `Settings/{General,Graphics,Media,Online}/Title` |
| 四个页标题 | 4 | `<页> Tab` → `Tab Title` | 同上（**与页签共用同一条键**，设计如此） |
| 图像页 | 6 | `Quality Selector/Quality selector text` · `Small Screen UI` · `Use super sampling` · `VSync` · `FPS Limit/Title` · `FPS Limit/FPS Slider` 第 3 格 | `…/SelectQuality` · `…/IncreaseUISize` · `…/EnableSuperSampling` · `…/Vsync` · `…/FrameLimit` · `…/UnlimitedFPS` |
| 联机页 | 8 | `Role Host/Text` · `Role Client/Text` · `Echo Button/Text` · `Host Block/IP Label` · `Host Block/Password Label` · `Host Block/Refresh/Text` · `Host Block/Save Button/Text` · `Client Block/Check Button/Text` | `Settings/Online/{RoleHost,RoleClient,TestPublicIp,IpLabel,PasswordLabel,Refresh,Save,CheckConnection}` |

**分工**（每条防的是什么失效模式）：

| 断言 | 防的失效模式 |
|---|---|
| `Check(got, Loc.T(Key))`（每档各一次） | 「调用点**没接词条**（还是写死字面量）」—— 写死英文的在**中文档**红、写死中文的在**英文档**红 |
| `CheckTrue(Loc.HasEntry(Key))` | 「**键名写错**」—— `Loc.T` 缺键会**把键名原样返回**，那样「拿键名当期望值」可能**看着对** |
| `CheckTrue(zh != en)`（21 处） | 🔴 **灭自证**：实现改回写死 + 期望值也一起改回写死 ⇒ **两处一起变绿**（只断单档时那种改法全绿） |
| `Loc.HasCjk(zh) && !Loc.HasCjk(en)`（21 处） | 「两档只是随手拼了两串」与「**真的读了两列**」分开；⚠️ `HasCjk` 区间含 `U+3000–U+303F` / `U+FF00–FFEF` ⇒ 英文列不许放全角 |
| `zh == en`（**只 `Settings/Graphics/Vsync` 1 处**，`Differ=false`） | 那条键**中英逐字都是 `VSync`**（原版那颗 TMP 本来就是英文）⇒ 它接词条的意义是「换语言时刷新」、**不是换字**；这一条把「我以为它该变」和「它确实不变」分开 |
| `CheckTrue(t != null)` | **前提**：节点取不到时，上面那条 `Check` 等于没验（假绿） |

### ②·2  两条「点一下才拼」的 `_flash`：**整句**两语档各断一次

| 断言 | 防的失效模式 |
|---|---|
| VSync 行点一下 ⇒ `win.Flash == string.Format(Loc.T("…/Flash/Vsync"), Loc.T("MainMenu/General/On"/"Off"))`（中/英各一次）+ 两句**不同** | `MainMenu/General/{On,Off}` 两条键**唯一的消费点**；防「接了键但拼串写错/两档同字」 |
| 点 VSync 时用 `VSyncSetterOverride` 挡住 + `CheckNear(QualitySettings.vSyncCount, vNow)` | 自检**不许改工程设置**（同 `:1083` 那条） |
| `win.SetFpsIndex(2, true)` ⇒ `Flash == string.Format(Loc.T("…/Flash/Fps"), Loc.T("…/FpsText/Unlimited"))`（中/英各一次）+ 两句**不同** | 同上；顺带钉住 `FpsText/Unlimited` 那条键真的被消费到 |
| 收尾 `Application.targetFrameRate` 逐值放回 | 自检**不许把进程帧率留在别的值上**（同 `:1260-1262`） |

### ②·3  `GfxRowLabelCount`（唯一测得出「短链泄漏」的只读口）

| 断言 | 防的失效模式 |
|---|---|
| 连调两次 `RebuildGfxRows()` ⇒ 这个数**一次都不许涨** | `RebuildGfxRows()` 挂在 `_gfxScroll.OnChanged`（**每滚一格都跑**）却在重建前**不清链** ⇒ 每次多几条指向刚被销毁的 `Label` 的闭包（**不报错、无界增长** = 静默） |
| `GfxRowLabelCount == 6` | 波 1b 起 = 四行标签各 1 + `FPS limit` 那行 2（行标题 + 第 3 格刻度）；**登记掉了 ⇒ 换语言那一格不刷 = 静默停在旧语言** |

### ②·4  语言那两句 `_flash`（**上游改了但一条断言都没有**）

`Settings/General/Flash/Language` + `Settings/General/LangHasNoTable`（`SettingsWindow.cs:1643-1644`）。

| 断言 | 防的失效模式 |
|---|---|
| ① 先摆 `English` → 点 `Chinese` 那一行 ⇒ `Flash == string.Format(Loc.T("…/Flash/Language"), Loc.LanguageName(Loc.Current), Loc.Current)`；② 反向再切一次，两句**不同** | 那条键**两档不同**（中 `语言 → 中文（Chinese）` / 英 `Language → English (English)`）；防「写死单语」 |
| 中/英两档 ⇒ 那句 flash **不该**带后半句 | `LangHasNoTable` 只在「本地没文案」那一档拼 ⇒ 防「无条件拼上后缀」 |
| 切到第一款**本地没文案**的语档 ⇒ `Flash == 前半句 + Loc.T("Settings/General/LangHasNoTable")`，**且比前半句长** | 「本地没文案 ⇒ 多出后半句」这条语义；判别式挡住「只印前半句也能过」 |
| `CheckTrue(!Loc.HasOwnText(Loc.Current))` | 前提（真的落在「没文案」那一档，否则上面那条验的是别的东西） |

⚠️ **为什么必须「从另一种语言切过去」**：`ChooseLanguage` 在「点的就是当前那一档」时**提前 return、连 `_flash` 都不碰**
⇒ 原地断会拿上一轮留下的旧 flash 去比（假绿/假红）。三步都是**真的换档**，与玩家当前设置无关。

---

## ③ 我**没改**的既有断言 + 逐条理由

| 处 | 现在断什么 | 为什么**不该改** |
|---|---|---|
| `:592`（页签循环里）`CheckTrue(FindChild(Bar(root),"Account") == null && …"Support" == null)` | 原版那两个键**不建** | 断的是**节点**，与语言无关（波 1b 一个节点名都没动） |
| `:601-604` 那两条 `FindChild`/`Click(Bar(root), names[i])` | 切页点**节点名** | 同上（上游明写「节点名一个都不许动」） |
| `:651` `CheckTrue(title != null && !string.IsNullOrEmpty(TextOf(title)))` | 页标题**非空** | 不是「等于某串」⇒ 与语档无关；改成断具体键反而把「页标题在不在」这件事**盖掉** |
| `:611` `Check(TextOf(n), Loc.T(tabTitleKeys[i]))` 之外那条 `CheckAtS` / `CheckAtS(n, 341.52f, …)` | 几何 | 与语言无关 |
| `:745` `Check(TextOf(verNode), "v"+Application.version)` | 版本号 | 版本号**不进语言表**（原版 `String.Concat("v", 版本)` 就是拼的） |
| `:765` `Check(TextOf(capNode), Loc.LanguageName(Loc.Current))` | 框里那行字 = 当前语言名 | **本来就跟着语言走**（走 `Loc.LanguageName`），波 1b 没动它 |
| `:770` / `:783` / `:818` / `:819` 那几条 `Loc.T(…)` | 已走词条 | **本来就是对的形状**，波 1b 没改这三处 |
| `:1265-1270` `FindChild(slNode, SettingsWindow.FpsTickName[i])` + `CheckTrue(tk != null)` | **节点名** | 上游明写「断 `FpsTickName[i]` 节点名的那条**不受影响**」——节点名照旧；⛔ 别把它也改成断文本（那会**丢掉**「节点名没动」这条保护） |
| `:1494` 那条（`小屏缩放器` 四条探针）、`:1778` 起 `A228`、`:1901` 起 `A491/A546/A545` 那几节 | Label 的**日志文案**（`a491L1.Exists(m => m.Contains("点阵后端没有对齐这回事"))` 等） | 那些是**我们自己的诊断日志**文案、**不在词条表里**、也不是玩家看到的界面字 ⇒ 与语档无关 |
| `:2278` 起音频页全部（含 `auNames = { "Music", ... }`） | 几何 + 节点名 | 音频页三行**这波没接词条**（`Music/Sound Effects/Voice-overs` 仍在表 B1 之外）⇒ 改了反而是**越界断言** |
| `:2385` 起语言下拉 12 行那节（`A862`） | 结构 / `LangRowCount` / 行字号 | 与语档无关；本节已经点过两次行、跑通 |
| `:2745` `CheckTrue(win.Flash != null && win.Flash.Length > 0)` | 点【检查连接】**有反馈** | 只断「有没有」⇒ 与语档无关（联机页那一整片的文案**不在本波范围**，属 P6） |
| `:2957` 起压暗层 / 关闭钮 / 字号那一族 | 几何 / 状态机 / 字号 | 与语言无关 |

> ⚠️ 也**没有**把 `:1004/:1301/:1494/:1574/:1681` 那五条从 `StartsWith/Contains` 升级成**整句精确比**：
> 整句里夹着**运行期值**（画质档名 / `FpsText()` / `On/Off`），而 `{0}` 那一半的判据已经在
> ②·2 那两条**精确比**里覆盖到了 ⇒ 这里保持「前缀跟语档」这一档即可（改精确反而会把
> 「档名从哪来」变成第二份推导）。

---

## ④ 证据

### `git diff --numstat`（只这一份文件）

```
391     16      Unity/MyGame/Assets/CardPresentation/Editor/SettingsScene.cs
```

**行尾**：改完仍是**纯 LF**（二进制读：`CRLF=0` / `LF=3433`；文件 3433 行）。
`numstat` 的 391/16 与改动量相符（新增一整节 300+ 行 + 10 处就地改），**不是行尾被翻**。
（⛔ 全程用 Edit 工具，**没用 `sed -i`**。）

### 秒级类型检查

命令：`TMPDIR=/tmp/wf_w8 bash d:/4/Unity/工具/typecheck.sh`（**独立 `TMPDIR`** —— 并行期别的写手在飞）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（跑过 **4** 次：第 1 次报 **2 条自己的编译错** —— 见 ⑤·1；改掉后三次都是 0/0。）

### ⛔ 没跑 Unity

本波红线不跑 Unity ⇒ **`SettingsScene.Run` 一次都没跑**，本节所有断言**都没经过实跑**。
下一次同步点复跑时的口径：宿主 = 本文件那一条 ⇒ **只跑 `SettingsScene.Run` 一条**。

---

## ⑤ 顺手发现的（⛔ 一条都没自己改，交调度台分流）

### 5·1 我自己踩的编译坑（已修，记下来给下一个写手）

**非逐字字符串里的 `{On,Off}` 会被当成插值洞**：我原来写
`$"…这是 `MainMenu/General/{On,Off}` 两条键唯一的消费点"` ⇒
`error CS0103: 当前上下文中不存在名称"On"/"Off"`。要写成 `{{On,Off}}`。
（类型检查**当场就报出来了** —— 这条正好印证「改完 `.cs` 立刻跑」值这个价。）

### 5·2 🔴 `ActionButtonAt` 的调试日志用的是**建钮那一刻**的语言（上游 5·2 已记，本轮**复核成立**）

`Shell/SettingsWindow.cs:2913` 的 `Debug.Log($"[Settings] 点了 `{label}`")` 捕的是形参 `label`
（= 建时的 `Loc.T` 结果）⇒ 换语言后点这颗钮，**日志里印的还是建窗那一刻的语言**。
**不是界面问题**（界面走 `OnLangText`，实测有断言盯着）⇒ 本轮**没动**。

### 5·3 ⚠️ 上游 5·1 列的「预计必红 4 条」我**逐条核过**，另外**多查出 2 条**

| 上游列的 | 判定 |
|---|---|
| `:543-546` 页签期望值是节点名 | ✅ **必红**（中文档 3 条 / 英文档 1 条 —— 比上游写的还多一种红法） |
| `:1279` `== "FPS limit"` | ✅ 中文档红 |
| `:1603` `== "Use super sampling"` | ✅ 中文档红 |
| `:1272` 第 3 刻度 | ✅ 中文档红 |
| **（我补）`Shell/SettingsWindow.cs:2070` 那条 `_flash`** ⇒ 宿主 `:975` `Contains("画质")` | 🔴 **英文档红**（英文档那句是 `Quality → …`）—— 上游没列 |
| **（我补）同上 `…/Flash/Fps`** ⇒ 宿主 `:1263` `Contains("帧率上限")` | 🔴 **英文档红** |
| **（我补）** `Flash/{SmallScreenUI,AutoZoom,SuperSampling}` 那三条 `Contains("…")` | ✅ **不红**（那三条键的**中文列刻意照抄了调用点的英文字串** —— `Loc.cs:1615-1617`）⇒ 保持 `Contains` 也绿；本轮仍**升级成 `TermHead(键)`**（键值一改就假红，见 ①） |

⇒ **结论：实际必红 = 上游那 4 条 + 我补的 2 条 = 6 条**（其中 `:975/:1263` 只在英文档红）。

### 5·4 上游 5·3：`FpsTickText[2]` 现在是**死值** —— 本轮**没删、也没碰**（仍是另一笔）

`Settings/Graphics/FpsTickText` 的 `[2]` 不再参与画字（`SettingsWindow.cs:2341`），
`[0]/[1]` 仍在用。宿主的 `tickTxt[i]` **与它无关**（那是「这一刻屏幕上该是什么字」）⇒ 本轮不动。
**⚠️ 顺手提醒下一个写手**：⛔ 别把宿主的 `tickTxt` 与 `SettingsWindow.FpsTickText` 弄混 ——
**前者是这行字、后者是原版英文原文的出处**，两者现在**明显不是同一个值**（中文档）。

### 5·5 上游 5·4：`A1063` 那两条**值订正**现在才真该做（本轮仍**没做** —— 键在 `Core/Loc.cs`、不在白名单）

- `Settings/Online/StatusNoSession` ZH 里的 `Host`/`Client` → `主机`/`客机`
- `Settings/Online/HowToConnect/DontUseTestSite` ZH 里的 `【Test Public IP】` → `【测外网】`
**前置条件已满足**（联机页那两颗钮现在会印中文了，本轮还给它俩各加了一条断言）。

### 5·6 ⚠️ 联机页那一片**没有一条**文案断言（本波只接了 9 个显示字，其余 ≈60 处仍写死中文）

我逐条扫过 `:2621-2790` 整个联机页节：断言全是**节点存在性 / 几何 / 角色切换 / IP 框内容 / 会话状态**，
**没有一条**按中文串断按钮上的字 ⇒ 本波那 9 处接键**不会**让既有断言红（也不需要改）。
⚠️ 但**这也意味着**：那 9 处的「接上了没有」原先**完全没有断言** ⇒ 本轮 ②·1 那 8 条是**新加的保护**。

### 5·7 ℹ️ `GfxRowLabelCount` 的**唯一断言主机就是本文件**（上游 5·3 复核成立）

`grep -rn GfxRowLabelCount --include=*.cs` 只命中 `Shell/SettingsWindow.cs`（定义）与本文件（②·3 新加的两条）
⇒ 那条「重建不许涨」的口径**以前没有任何人跑过**。

---

## ⑥ 没查清 / 停手的部分（逐条）

1. 🔴 **本节所有断言都没经过实跑**（本波红线不跑 Unity）。**预计风险最高的是这三条**，复跑时若红请**先怀疑它们**：
   - `GfxRowLabelCount == 6` —— 若某条行的 `Label` 建失败（图缺 / `Text` 返 null），这个数会 < 6 ⇒ **红，而不是泄漏**。要判开：看 `OnLabelCount`/该行节点在不在。
   - ②·4 那三条语言 flash —— 依赖「语言是否**真的换了档**」（`Loc.Languages` 的行号 = 下拉行号）。
     若 `Loc.Languages` 声明序动过，`iZh/iEn/iNoOwn` 是**现算**的 ⇒ 仍然对；但若 `LangRowHit` 与
     `Loc.Languages` **下标语义不一致**，这三条会红。**判据**（`A862` 那节已经有先例）：`win.LangRowHit(3)` ⇒ `Loc.Current == Loc.Languages[3]`。
   - 22 处里**联机页那 8 处**读的是**未显示那一块**（`HostBlock`/`ClientBlock` 只有一块 active）——
     判据是 `MenuCheck.FindChild` 走 `GetComponentsInChildren<Transform>(true)`（**含 inactive**）⇒ 应取得到；
     `Label.Text` 也是普通字段（不是量测）⇒ 读得到。**若红，先看是不是这个前提不成立**。
2. **`_flash` 那几条的「两档不同」判别式**依赖中英两条键值真的不同 —— `Settings/Graphics/Flash/Vsync` 的
   **前缀**两档同字（只 `On/Off` 不同）⇒ 我断的是**整句**，那一对确实不同（`VSync → 关` vs `VSync → Off`）。
   **没实跑核过**。
3. **上游 5·2 那条「`ActionButtonAt` 日志用建钮时的语言」** —— 我**只读代码确认**，没跑起来看日志。
4. **`Battle/SettingsPanel.cs` 的语言行**仍是「点一下换下一个」（`SettingsWindow.cs:1347` 那条注明的**独立既有偏离**）——
   **不在本件白名单**，**没动**。
