# H17 · A416（`ShowPopUp` 收编到 `PopUpGameWindow`）+ 3 宿主一次性协调落地（写手 · 2026-10-12）

> 白名单内改动：`Shell/WindowsManager.cs`（本体 + 注释）· `Editor/ShellScene.cs`（改夹具 + 新断言 + 1 个助手）
> · `Editor/MainMenuScene.cs`（3 处）· `Editor/CollectionScene.cs`（2 处）。
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改正本 · ⛔ 没越白名单 · ⛔ 没碰 `Editor/BattleScene.cs` / 别的写手的落点。
> 逐字改法出处 = `资料/普查产出_1012/H5_DeckScene红与ShowPopUp收编.md` §③（**按它落的，行号按实读订正**）。

---

## ① 结论

✅ **一次落完**（(a) 本体 + (b)(c)(d) 3 宿主 6 处 + (e) 注释侧 + 断言）—— H5 §3·2 说的「只落 (a) ⇒ 3 宿主必红」
那件事**没有发生**，因为 6 处替换是同一次改完的。类型检查 **0/0**（运行时 + 编辑器两个程序集）。

| 件 | 状态 |
|---|---|
| (a) `WindowsManager.ShowPopUp` 转调 `ShowMessagePopUp`（含「先 `Close()` 再回调」那层包装 + 第二颗钮必挂回调） | ✅ `Shell/WindowsManager.cs:1130-1139` |
| (b) `ShellScene` 3 处（点钮改成**跳过吸收层**·⑤b 改成**直建 `PromptPopup`**） | ✅ `Editor/ShellScene.cs:832-836` / `:905-911` |
| (c) `MainMenuScene` 3 处（夹具不认类型 · 两处清场改**类型无关**） | ✅ `:3633-3637` / `:1749-1756` / `:3862-3869` |
| (d) `CollectionScene` 2 处（清场 + 读「隐藏卡」提示窗） | ✅ `:2482-2488` / `:2508-2516` |
| (e) 注释侧（`WindowsManager` 那两段「为什么还没收编」按 H5 §⑧ 就地删掉；`PromptPopup` 那两处**不在白名单**⇒ 只报） | ✅ 部分（见 §⑤·2） |
| 断言 | ✅ **9 条**（H5 §3·3(f) 的 7 条 + H5 §⑦·5 与 §③(a) 各自点名要求的 2 条） |
| `Shell/PopUpGameWindow.cs` | **一字未改** —— 断言要的观察口（`MessageShown` / `TwoButtons` / `PrefabName` / `PrimaryShown` / `SecondaryShown`）**本来就有** |

---

## ② 改动清单（文件:行号 · 每处一句为什么）

### `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs`（**只有 `ShowPopUp` 那一处是行为**）

| 行 | 改动 | 为什么 |
|---|---|---|
| `:28-32` | 文件头 ① 的「二次更正」补上收编结果 | 原来那句「拿 `PromptPopup` 当 `ShowPopUp` 的宿主是**一处【已知偏离】**」**今天不成立了** —— 留着就是错的（铁律 5） |
| `:604-607` | `closeOnEsc == false` 那张窗清单**补上 `PopUpGameWindow`(0)** | 收编之后 `ShowPopUp` 开的就是它；漏了会让下一轮有人按「清单里没有它」反推「它 ESC 关得掉」（H5 §⑦·3 点名要核的那一眼） |
| `:1100-1128` | `ShowPopUp` 文档段重写：① 宿主 = `PopUpGameWindow` ② **保留**「原来那条前提是错的」的更正痕迹与**错因**（按猜的名字搜资源）③ 新增「为什么还要包一层 `Close()`」④ 新增「第二颗钮只要有 `cancelText` 就必须挂回调」 | **删掉**的是「为什么今天【还没有】收编」那一整段（H5 §⑧：那是**临时说明、不是知识**）；**留下**的是判据与教训 |
| `:1130-1139` | **本体**：`PromptPopup.Create + OpenWindow` → **`ShowMessagePopUp(...)` 一层包装** | 逐字照 H5 §3·3(a) —— 两个 lambda 都是「**先 `Close()` 再回调**」（旧宿主 `PromptPopup.Choose` 的顺序）；右钮的回调**按 `cancelText != null` 挂**（⛔ 不是按 `onCancel != null`） |
| `:1141-1157` | `ShowMessagePopUp` 文档段：删「为什么并存两条」、改成「**A416 起全壳只有这一条**」+ 记「11 处里只有 `ShopWindow:730` 是 2 按钮档」 | 那段「并存两条、新代码走这一条」**今天过期**；顺带把「两版 prefab 都有生产消费者」这条事实写实 |

### `Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs`

| 行 | 改动 | 为什么 |
|---|---|---|
| `:41-52` | **新增** `ClickPopUpButton(GameWindow popup, string btnName)`（按名字取钮 + `ClickForTest`，取不到打红） | `GetComponentInChildren<WindowButton>()` 在 `PopUpGameWindow` 上会**先撞上压暗层那颗吸收层**（`absorbOnly` 早退）⇒ 点了等于没点。本仓同类助手 = `DeckScene.ClickPopupButton`（那份在白名单外，不借） |
| `:832-836` | ⑤ 段点钮：`GetComponentInChildren<WindowButton>() + ClickForTest` → `ClickPopUpButton(popup, "Generic UI Button")` | H5 §3·3(b) 第 1 处。1 按钮版的钮名照 prefab = `Generic UI Button` |
| `:840-896` | **新增 ⑤·a A416 段**（9 条断言，见 §③） | H5 §3·3(f) 的 7 条 + §⑦·5（1 按钮档）+ §③(a)（没给 `onCancel` 也要关得掉） |
| `:905-911` | ⑤b 段：`shell.Windows.ShowPopUp(...)` + `popUpWindow as PromptPopup` → **`PromptPopup.Create(...)` + `OpenWindow(pp)`**（断言文案改成「它**不再是** `ShowPopUp` 的宿主」） | H5 §3·3(b) 第 2 处：本节验的是 **`PromptPopup` 自己的版面**（15 条），不直建的话收编后 `pp` 恒 null ⇒ **整段静默跳过** |
| `:2489-2491` | 「退出游戏」那扇的断言**文案**补一句：它是 `MainMenuRuntime` **自己 `PromptPopup.Create`** 的，⛔ 不是 `ShowPopUp` 的宿主了 | 文案原来写「原版 `SettingsMenu.ExitGamePopup` → `WindowsManager.ShowPopUp`」⇒ 收编后会被读成「这扇也该换成 `PopUpGameWindow`」（**其实不该**，它不是那条链开的）；**断言本身一字未动** |

### `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`

| 行 | 改动 | 为什么 |
|---|---|---|
| `:1749-1756` | 练习窗那处清场：`FindObjectsByType<PromptPopup>` → `pw.Manager.popUpWindow?.Close()`（**类型无关**）+ 清不掉时**出声** | H5 §3·3(c)：按类型清场收编后**静默变成空做** ⇒ 模态窗盖截图。先例 = `Editor/ShopScene.cs:1512-1513` |
| `:3633-3637` | 遭遇窗夹具：`sk.Manager.TopWindow as PromptPopup` → **`sk.Manager.TopWindow`**（不 `as`）+ 断言文案去掉 `PromptPopup` | H5 §3·3(c)：`as PromptPopup` 会**恒 null** ⇒ `Close()` 空做 ⇒ `sk` 停在 `Background` ⇒ 下面两组 `CheckAbsorbRule` 的 `found` 假（那一段注释自己记着这处回归） |
| `:3862-3869` | 排位窗那处清场：同上改法（用 `rk.Manager`） | 同 `:1749` |

### `Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`

| 行 | 改动 | 为什么 |
|---|---|---|
| `:2482-2488` | 「隐藏卡」那节起手清场：按类型 → **类型无关**（`win.Manager.popUpWindow?.Close()`） | H5 §3·3(d) |
| `:2508-2516` | 读那扇提示窗：`PromptPopup hp` + `FindObjectsByType` → **`GameWindow hp = wm.popUpWindow`**；正文读法**一字未改**（两边节点同名 `MessageText`） | H5 §3·3(d)：按类型找 ⇒ `hp` 恒 null ⇒ 「弹出提示说清原因」那条断言**退化成空断**（还挂着「不许静默」的名义） |

**⛔ 没受影响的（逐条实读核过，别误伤）**：`Editor/ShopScene.cs:1512-1513`（本来就类型无关 ✅）·
`Editor/RewardsScene.cs:4275`（同 ✅）· `Shell/MainMenuRuntime.cs:621`（「退出游戏」是它**自己 `PromptPopup.Create`**）·
`Editor/ShellScene.cs` 的 `PromptPopup.Create` 两处探针（`:2221` / `:2849`）· `Editor/RewardsScene.cs:5124`
（只读 `PromptPopup.QText` 静态常量）。⚠️ H5 §3·2 表里写的 `ShopScene.cs:1454-1455` **行号已漂**
（实读 = `:1512-1513`，别按旧号去找）。

---

## ③ 断言（9 条 · 每条带改坏法）

落点 = `Editor/ShellScene.cs:840-896`（`Section("A416：…")`），全部走**生产那条路**（`ShowPopUp` + `WindowButton.ClickForTest`
= `PointerLayer` 派发时调的那一个）。

| # | 断言 | 怎么改会红 |
|---|---|---|
| 1 | `popup is PopUpGameWindow`（宿主 = 原版那两扇） | `ShowPopUp` 改回 `PromptPopup.Create` ⇒ 红 |
| 2 | `hp1.MessageShown == "自检弹窗"`（正文 = 调用方**明文**） | 把明文当术语键去过一层表（或调用点改成传键）⇒ 红 |
| 3 | `!hp1.TwoButtons` + `PrefabName == "MessagePopupWindow"`（**只给一颗 ⇒ 1 按钮版**） | `PopUpGameWindow.Create` 里 `secondaryKey` 那个判据改成恒 `true` ⇒ 红（H5 §⑦·5 点名要补的那一档） |
| 4 | `hp1.PrimaryShown == "确定"` | 钮标不贴 / 贴错键 ⇒ 红 |
| 5 | `hp2.TwoButtons` + `PrefabName == "MessagePopupWindow2Buttons"`（**给两颗 ⇒ 2 按钮版**） | 那个判据改成恒 `false` ⇒ 红 |
| 6 | `hp2.PrimaryShown == "确定"` **且** `hp2.SecondaryShown == "取消"` | `Configure` 里两个键**传反** ⇒ 两条一起红 |
| 7 | 点**右钮** ⇒ `canceled2 == true`（`onCancel` 真的执行了）；点**左钮** ⇒ `fired2 == true` | 两颗钮的**回调**接反 / 某颗钮 `onClick` 没挂 ⇒ 对应那条红 |
| 8 | 点完钮 ⇒ `!window.gameObject.activeSelf`（**窗自己关掉了**） | 收编时**漏掉那层 `Close()` 包装** ⇒ 红（旧宿主 `PromptPopup.Choose` 是「先关再回调」，不包就不是等价替换） |
| 9 | **没给 `onCancel` 也关得掉**（`ButtonRight` 的回调按 `cancelText != null` 挂；按「有 `onCancel` 才挂」写 ⇒ 那颗钮 `onClick` 是 null ⇒ 红） | 见左 |

**⛔ 没有自证 / 没有弱断言**：9 条里没有一条从**被测实现**里读期望值 —— 期望值全来自原版 prefab 的**节点名**
（`ButtonLeft` / `ButtonRight` / `Generic UI Button` / `MessageText`）与**建树规则**（「按钮数 > 1 ⇒ 2 按钮版 prefab」）。
第 3 条另配一条**反自证前提**：`!(FindChildIn(hp1.transform, "Generic UI Button")` 上挂的是 `MenuDraw.Absorb`
建的那颗吸收层（`MenuDraw.WasAbsorb`）`—— 万一「点钮」那条其实点到了吸收层，这条先红。

---

## ④ 老调用点仍能弹窗（`ShowPopUp` 全清单 —— 收编**一行都没改**）

判据：11 处生产调用点 + 自检站点。**全部传【明文】**（`PopUpGameWindow.Term()` 查不到就原样返回）⇒ 收编对调用点零影响。

| 站点 | 钮数 |
|---|---|
| `Shell/ShopWindow.cs:730`（传奇重复购买确认） | **2 钮**（`确定`/`取消` + `onOk`，`onCancel` 空）——**唯一**那一处 2 按钮档 |
| `Shell/SettingsWindow.cs:1629`（怎么联机） · `Shell/RankedEventWindow.cs:181`（排位要服务器） · `Shell/PracticeModePopup.cs:1476`（没督军） · `Shell/LiveOpsEventWindow.cs:828,835` · `Shell/LeaderboardWindow.cs:608`（上赛季榜单） · `Shell/DeckInfoPopup.cs:1166,1243,1326`（隐藏卡 / 分享卡组串） · `Net/NetRuntime.cs:105`（联机提示） | 各 **1 钮**（`okText` + `null`） |
| 自检：`Editor/ShellScene.cs:822`（`"自检弹窗"` + 回调） | 1 钮 |

⇒ **1 按钮版**（`MessagePopupWindow`）= 10 处生产 + 1 自检；**2 按钮版** = `ShopWindow` 那 1 处 + 自检新加的 3 处。
（H5 §⑦·5 的顾虑「1 按钮版收编后才第一次拿到生产消费者」**成立且已覆盖**：第 3 条断言钉的就是它。）

---

## ⑤ 没查清 / 待判据（⛔ 不猜）

1. 🔴 **本轮按用户口径没跑自检** ⇒ H5 §3·2 那张「会怎样」的表与**我的**改法都只有**代码路径推演**。
   与实跑不符时**以实跑为准**。📌 同步点必跑 **4 条**（**必须一起跑**）：`DeckScene.Run` · `ShellScene.Run` ·
   `MainMenuScene.Run` · `CollectionScene.Run`（判据 = 铁律 12 判据②：动了**共用件 `WindowsManager` 的行为** ⇒ 跑全部受影响的那几条）。
2. ⚠️ `Shell/PromptPopup.cs` 的**两处注释已过期**，但**该文件不在本件白名单** ⇒ 未改（只报，见 §⑥·2）。
   要改的两处：文件头 `:1` 的「（「暂无服务器」等的宿主）」、类注释 `:35` 的「也是边界③「点了如实提示」的**唯一宿主**」
   —— 收编后 `ShowPopUp` 不再用它。
3. ⚠️ **`hp1.MessageShown` 这类断言依赖 `Label.Text` 返回「源串」而不是「渲染后可见的那截」**
   （实读 `Battle/Label.cs:79` = `_text`，与 `PopUpGameWindow.MessageShown` 的定义一致）—— 这条**没在 Unity 里跑过**，
   若实跑发现 `Text` 会因自适应缩字号而改写 ⇒ 那两条要改成读节点存在性 + 长度，⛔ 不许放宽成「不空即可」。
4. ⚠️ **H5 报告里多处行号已漂**（`ShellScene:818/829/831` · `MainMenuScene:3626` · `CollectionScene:2472/2451` ·
   `ShopScene:1454-1455`）—— 本件全部**按名字重找**、以实读为准；上面 §② 给的是**改后**的实读行号。
5. ⚠️ **`ShowPopUp` 的 `closeOnEsc`**：原版签名里有、我们没有（H5 §⑥·4）。本件**没加**这个形参（唯一需要 `1` 的那处
   = `MainMenuRuntime` 自己直建 `PromptPopup`，与收编无关）——**如实留着**。

---

## ⑥ 顺手发现（⛔ 一个都没在本件里改，只报）

1. 🔴🔴 **真缺陷（现状已在飞）：复用一个「还开着」的消息弹窗 ⇒ 正文/钮标/回调【不刷新】。**
   - 代码：`ShowMessagePopUp` 的复用支只调 `win.Configure(...)`（`Shell/PopUpGameWindow.cs:242-251` = **只写字段**），
     而 `OpenWindow → TryOpen` 在 `CurrentState == Open` 那一支**什么都不做**（`Shell/WindowsManager.cs:419-426`
      「同窗再开不刷内容」）⇒ **建树那一步（`Build()`）根本不会再跑**。
   - 原版：`PopUpGameWindow__ConfigurePopUp.c` 是**直接写活组件**（`SetText(文案, …)` + 逐颗 `LiveButtons` 贴标签/挂回调）
     ⇒ 复用那一刻**内容是会变的**。
   - ⚠️ 同一份代码里 `Shell/WindowsManager.cs:1188-1190` 的注释原来写着「**上面那一支必须用 `Configure` 先把
     正文/两钮/回调改掉**」——**那句话与实现的语义对不上**（`Configure` 改的是字段、不是那棵树）。
     ✅ **本件已就地订正**那一句（`Shell/WindowsManager.cs:1188-1200`：写清「复用不刷新」+ 原版对照 + 「本处还没修」
     + 改坏法）；**行为一个字没动**，⛔ 修它本身要等调度台开账。
   - 触发面：`DeckRuntime.ShowInvalidDeckPopUp`（A364）本来就走复用；**A416 之后 11 处 `ShowPopUp` 也走它**
     （最像真会撞的是 `Net/NetRuntime.cs:105` —— 网络回调可能在另一扇弹窗开着时到）。
   - ⛔ **本件没改**：`Shell/PopUpGameWindow.cs` 本件只准「加只读观察口」，改 `Configure` 是**行为改动**（越权）。
     判据已备齐（原版那一支 + 我们的两支），**请调度台开账**。
2. ⚠️ `Editor/ShellScene.cs:944-945` 那段注释「`extraScaleSmallScreen` 在我们这套里**没有消费者**……小屏缩放器没实现」
   —— **A165 / 2026-10-06 就做完了**（`WindowsManager.TryOpen` 里那句 `ApplySmallScreenScale()`）。D2 报告记过、**至今没人改**。
   ⚠️ 本件**能**改（ShellScene 在白名单里）但**没改**：它属于「顺手发现」，按派工口径只报不改，免得与别的写手撞行。
3. ⚠️ `Shell/PromptPopup.cs` 文件头 `:1` / 类注释 `:35` 的「唯一宿主」两处过期（见 §⑤·2）—— 白名单外。
4. ⚠️ H5 报告 §⑦·1 说的「D2 报告里『8 个既有调用点』那处」—— **仍没人改**（H5 只改了 `WindowsManager` 那一处）。归调度台。
5. ⚠️ **`ShopScene` 的清场是本仓唯一「已经类型无关」的写法**（`:1512-1513`）—— 建议把它当成这一类改动的
   **参照实现**写进正本（它同时证明了「按字段判 `popUpWindow`」在收编前后都成立）。
6. ℹ️ `Shell/WindowsManager.cs:604-607` 那张 `closeOnEsc == false` 的窗清单我**补了 `PopUpGameWindow`(0)**
   （这是本件白名单内的注释订正，不是行为）。

---

## ⑦ 行尾与自检

- 行尾复核（`io.open(...,'rb')`，⛔ 不是文本模式的假读数）：
  `Shell/WindowsManager.cs` **CRLF 0 / LF 1223** · `Editor/ShellScene.cs` **CRLF 0 / LF 3001** ·
  `Editor/MainMenuScene.cs` **CRLF 0 / LF 8109** · `Editor/CollectionScene.cs` **CRLF 0 / LF 5065**
  ⇒ **四份都是纯 LF，一行没翻**（这四个文件改前也都是纯 LF）。
- 秒级类型检查：`TMPDIR=/tmp/wf_h17 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 · 编辑器 0**。
  ⚠️ 中途有一次两程序集也都是 0/0 —— 那说明**当时没有别的写手的半成品**在树上。
- **没跑**任何 Unity 批处理 / 自检（本件红线 + 用户口径）。
