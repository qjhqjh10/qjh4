# D1 · `SettingsScene.Run` 9 条红【只读诊断】

> 只读代理 D1 · 2026-10-19。**一个字代码都没改**（`git status` 只多本文件）。
> 判据来源：`Editor/SettingsScene.cs`（断言本体）· `Shell/SettingsWindow.cs` / `Shell/MenuDraw.cs` / `Battle/Label.cs`（实现）·
> `d:/4/_tmp_view/settings.log`（运行读数）· `普查产出_第十会话/_tmp_acct_full.txt`（原版 prefab 探针 dump）。
> ⛔ **没跑 Unity**（红线）⇒ 所有「修完会不会绿」都是**静态推理**，逐条标了置信度。
>
> 🔴 **2026-10-09 就地订正（第十会话 · 执行代理 `P0` 现核 · 铁律 5）—— 本报告 `#6` 那条修法里【括注的第二个口】不成立**：
> 原文写「（或 `PointerLayer.HitQuadForTest(hit.GetComponent<WindowButton>())`）」—— **实测那个口会把 `#8` 留红**：
> `HitQuad(b)` 的**头一句**是 `!b.isActiveAndEnabled` ⇒ 返回 null，而量「`Register Button`」那一刻它**正关着**
> （③ 登录档的 `Refresh` 把它 `SetActive(0)`；`settings.log:3931` 那条「登录档：`Register Button` 关」**是绿的** = 实据）
> ⇒ **那个口会造出一条新的假红**。
> ✅ **正解 = 本报告【主推的第一个】**（`GetComponentInChildren<ImageQuad>(true)`），**且 `true` 不许去掉**（`P0` 已在代码注释里写死这一条）。
> ⚠️ 连带订正：`#8` 表里那句「同 `#6`」**只对【取错了口】这件事成立**，**不对**「换哪个口都行」。

---

## 1. 九条 ×〔档 / 根因 / 原版值 vs 我们值 / 建议修法 / 证据〕

| # | 档 | 一句话根因 | 原版值 vs 我们值 | 建议修法（⛔ 不是我做） | 证据 |
|---|---|---|---|---|---|
| **1** | **β** 实现缺陷 | `BuildTabs` 画页签字时走的是**裸 `Loc.T(键)`**，而 `Settings/Account/Title` **不在 `Loc` 表里** ⇒ `Loc.T` 按契约**返回键名本身** | 原版 `Account Tab` 页签字 = 词条 `Settings/Account/Title` 的英文 `Account`；我们屏上是 `Settings/Account/Title` | `SettingsWindow.cs:1333` 那一行不要写 `Loc.T(specs[i].Key)`，改走**本页已有的两步漏斗**（`Loc.HasEntry(Key) ? Loc.T(Key) : 英文原文`；`AcTerm` 就是这个形状）——同页 `:1761` 的页标题**走对了**（实得 `Account`）。🔴 **同一条也要改 `:2233`**（`RefreshTexts` 里 `Lb.SetText(Loc.T(Key))` ⇒ 一换语言页签又变回键名） | `SettingsWindow.cs:1306`（`Key = lkAcTitle`）· `:1333`（裸 `Loc.T`）· `:1667-1678`（`AcTerm` 两步路）· `:1761`（走对了）· `:2232-2233`（刷新链同病）· 日志 `settings.log:429`（`Loc` 自己报「没有这个词条 ⇒ 印的是键名本身」）、`:1773` |
| **2** | **β** 实现缺陷 | `External Link Icon` **节点压根没建**：`Rect(...)` 的贴图 `Copy@3x` 本地没有 ⇒ `MenuDraw.Rect` **第一句 `if (tex == null) return null;`** 直接返回、**连节点都不建**，而这一行**没有兜底** | 原版 `Account Tab > Player Id > External Link Icon` 存在（图 `Copy@3x`）；我们一棵子树里没有这颗节点 | 照本窗**同族的既有形状**补兜底：`:2110-2111`、`:2136` 两处就是 `var q = Rect(...); var node = q != null ? q.transform : Node(...);` ⇒ `:1774` 照抄（图导进来后**自动亮**，与「图缺了要出声」不冲突——`Tex` 已经出声） | `SettingsWindow.cs:1774`（无兜底的 `Rect`）· `:2110-2111` / `:2136`（同窗既有兜底写法）· `MenuDraw.cs:1494`（`if (tex == null) return null;`）· 日志 `settings.log:506`「图缺了：`Copy@3x`」（出声在、节点不在） |
| **3** | **α** 断言自己错 | 判别式第二半 `FindChild(acPage,"Login Button") == null` —— **`acPage` 整棵子树里含弹窗**，而弹窗里那颗**就叫 `Login Button`（无空格）** ⇒ 恒不为 null | 原版确有**两颗**：页里 `Login Button `（尾空格）+ 弹窗里 `Login Button`（无空格）—— 实现**两颗都建对了**（带空格那颗的矩形断言 `settings.log:3652` 是绿的） | 把第二半**收窄到页内那一格**：`FindChild(FindChild(acPage,"Unregistered Buttons"), "Login Button") == null`（⛔ 别 trim、别删判别式） | `SettingsScene.cs:810-813`（`FindChild` 的 `want`）· `:947-950`（弹窗那颗无空格）· `SettingsWindow.cs:2122`（弹窗名）· `:1884`（页里 `AcLoginBtnName`）· `MenuCheck.cs:141-147`（`FindChild` = 递归 `GetComponentsInChildren`，**先父后子**） |
| **4** | **α** 断言自己错 | `CheckAtS` 比的是**节点中心**，而 `EmailText` 这颗 Label **被 `AlignLeft` 挪过**（`Label.AlignLeftOn` 把节点中心移到「左沿 + 宽/2」） ⇒ 中心天然不等于原版矩形中心 | 原版 TMP `EmailText` 矩形 `[596.52,261.43]–[1056.52,321.43]`（460×60，`Left/Capline`）；我们**渲染出来的字**在同一位置，**节点**被缩到字宽 ⇒ 中心偏 151.31px | 换 **本窗已有的那个助手**：`CheckLeftS(FindChild(acPage,"EmailText"), 596.52f, 1056.52f, 261.43f, 321.43f, "`EmailText`")`（它比**左边缘**，`SettingsScene.cs:238` 的 doc 就写着「`AlignLeft` 会把 Label 的节点挪走 ⇒ 不能拿它的位置去比矩形中心」） | `SettingsScene.cs:825`（断的什么）· `:238-247`（`CheckLeftS` + 那句 doc）· `:766`（页标题就是用它断的）· `SettingsWindow.cs:1784`（`AlignLeft`）· `Battle/Label.cs:AlignLeftOn`（`localPosition.x = worldLeftX + WorldW*0.5` ⇒ 左沿对、中心偏）· 日志 `:3442` |
| **5** | **α** 断言自己错 | 同 #4（`PasswordText`） | 同 #4（偏 126.46px，比 `E-mail` 少偏 = 字更长 ⇒ 缩得更少，**与 #4 同源且可互证**） | 同 #4：`CheckLeftS(..., 596.52f, 1056.52f, 390.71f, 450.71f, ...)` | `SettingsScene.cs:827` · `SettingsWindow.cs:1791` · 日志 `:3472` |
| **6** | **α** 断言自己错 | 取节点的口错了：`FindChild(cn,"Hit")` 取到的是**外层 `Hit` 节点**（`MenuDraw.Hit` 建的裸 `RectTransform`+`WindowButton`），而 quad 是它的**子件、名字也叫 `Hit`** ⇒ `GetComponent<ImageQuad>()` 恒 null ⇒ 打印的是**归零值** `0.00×0.00` | 原版关窗钮射线区 = 子件 `Icon` 外扩 20 ⇒ 设计 96.37×94.50；**我们的命中区真建了**（证据：`:910` 那条「弹窗开着时 `Login Button` 的命中区生效」**是绿的**，`settings.log:4039`） | 取 quad 而不是节点：`var hq = hit.GetComponentInChildren<ImageQuad>(true); CheckQuadRectS(hq != null ? hq.transform : null, null, …)`（或 `PointerLayer.HitQuadForTest(hit.GetComponent<WindowButton>())`）。⚠️ `CheckQuadRectPx` 的 doc 已经写明「取的是**节点自己**那颗 quad」 | `SettingsScene.cs:944-946`（调用）· `:210-229`（`CheckQuadRectPx` = `GetComponent`）· `MenuDraw.cs:2005`（外层 = `new GameObject(name,…)` + `WindowButton`）· `:2036-2039`（quad = `ImageQuad.Create(node,…,"Hit")` = **子件**）· `:1492-1494`（`Rect` 首句） |
| **7** | **α** 断言自己错 | **同 #6 —— 同一个根因**（弹窗 `Login Button > Hit`） | 应落在 278.3×54.0（`Hit(n,"Hit", LwBtnL, LwBtnT, LwBtnR, LwBtnB)`，设计 309.17×60 × 0.9） | 同 #6 | `SettingsScene.cs:947-950` · `SettingsWindow.cs:2127` |
| **8** | **α** 断言自己错 | **同 #6 —— 同一个根因**（`Register Button > Hit`，走 `AccountBigButton` 的 `Hit`） | 应落在 270.0×81.0（设计 300×90 × 0.9） | 同 #6 | `SettingsScene.cs:951-953` · `SettingsWindow.cs:1932` |
| **9** | **α** 断言自己错 | 白名单**漏了一个已写进注释的值**：`:3771` 那条注释白纸黑字写着「43 → **38.7**」，但 `wantPx` 数组里**没有 38.7f** | 原版那三颗社交钮的 `Button Text` **`fontSize = 43.0`**（探针 dump 亲读）⇒ × 0.9 = **38.70** —— 我们实现**是对的**，是白名单漏了 | `:3774` 的 `wantPx` 里补 `38.7f`；**并把 `:3814` 那句硬编码的「允许的 12 个」清单也补上 38.7**（`wantPx.Length` 会自动变 13，「12 个」那两个字要跟着改） | `SettingsScene.cs:3767-3774`（注释说了 43→38.7，数组没放）· `:3812-3819`（扫描 + 硬编码清单）· `SettingsWindow.cs:726`（`AcSocTxtFont = {43, 31.049999, 43, 31.049999, 43}` ⇒ **正好 3 个 43**）· `_tmp_acct_full.txt:212-215`（`Button Text` / `fontSize = 43.0`）· 日志 `:21135` |

**合计：α 7 条（#3/#4/#5/#6/#7/#8/#9）· β 2 条（#1/#2）· γ 0 · δ 0。**

---

## 2. 共同根因（同源的要一起改）

* **A 组 = #6 + #7 + #8（一个根因，⛔ 别三条各修一遍）**：`MenuDraw.Hit` 建的是**一颗裸节点 + 一颗同名子 quad**，
  而这三条断言都用 `GetComponent<ImageQuad>()` 去取**外层的节点**。
  ⇒ 三条**全部恒红、与实现对错无关**（= 这三条**零验证力**；而 `:910` 那条走 `PointerLayer.HitQuadForTest` 的**是绿的**，正好互证）。
* **B 组 = #4 + #5（一个根因）**：对**被 `AlignLeft`/`AlignRight` 挪过的 Label** 用「比中心」的量法 ⇒ 必红。
  本仓**已经**有对的助手（`CheckLeftS`，`:238`），是**这两条没照它写**。
* **C 组 = #3（独立）**：`FindChild` 的**作用域**忘了弹窗是页的子件 ⇒ 判别式被弹窗那颗同名件顶掉。
* **#1 / #2 / #9 各自独立**。

---

## 3. 同族「没被这 9 条覆盖」的（第 4 问）

| 线索 | 还开着的那一半 |
|---|---|
| #1 `BuildTabs` 裸 `Loc.T` | **`RefreshTexts` `:2233` 同病** —— 切一次语言（或 `OnLangText` 刷新）页签就变回键名。⛔ **只改 `:1333` 不算改完**。另：`Editor/SettingsScene.cs` 的「波 1b」两语档那一节（日志 `17108–17189`）**只点了 4 个页签**，`Account` 那一格**不在里面** ⇒ 键进表后没有第二条守卫 |
| #2 `Rect` 无兜底 | 本窗**只有这一处** `Rect` 的返回值被丢弃又没兜底（其余 4 处都写了 `q != null ? q.transform : Node(...)`）。但**顺带查出更大的一个**：见 §5 第 1 条 |
| #6/7/8 命中区量法 | 新一节**只断了 3 颗** Hit（绿关窗钮 / 弹窗 Login / Register）。**本窗还有约 20 颗 `Hit` 一条都没量**（5 个页签键、页里 `Login Button `、`Switch Account`、`Logout`、`Twitch`、`Delete`、`Reset`/`Forgot Password`、`Subscribe Newsletter`、5 个社交钮、弹窗 `Forgot Password`、两颗输入框、语言 Blocker…）⇒ 谁将来再照 `FindChild(x,"Hit")+CheckQuadRectS` 写，**还是恒红**（形状坑，不是数据坑） |
| #9 字号白名单 | 扫描是**全树 + 含 inactive**（`GetComponentsInChildren<Label>(true)`，`:3796`）⇒ **没有 Label 逃得掉**（3 颗恒关的社交 `Button Text` 就是被它抓出来的）。⚠️ 但它只认 `Label` —— 文字若从**别的入口**进（点阵后端 / 非 `Label` 的 TMP）就扫不到；本窗目前只有 `MenuInputField` 那一条旁路，`:3794` 已点名且它自己过了 0.9 |
| #3 的判别式 | 页里那颗 `Login Button ` 与弹窗那颗 `Login Button` **名字只差一个空格**，**全工程只在本窗出现** ⇒ 下一个窗若也要用这个名字，先核一遍（W4 报告 §7.1 已记） |

---

## 4. 没查清的部分（如实）

1. **没跑 Unity**（红线）⇒「照上面改完会不会绿」**是静态推理**：
   * #6/7/8：#6 的期望值我逐项核过（`ClosePad = (-20)⁴` @ `SettingsWindow.cs:864`、`LwCloseIcon*` @ `:796` ⇒ 设计 96.37×94.50 / 屏幕 86.73×85.05 = 日志里那句「应落在…」）⇒ **应当绿**，但**未实测**。
   * #1/#2/#9：改完应当绿。
   * #4/#5：`CheckLeftS` 的容差 2px、量的是 `position.x*108 - WorldW*108/2`；`AlignLeftOn` 的算式与它同源 ⇒ **应当绿**，未实测。
2. **`Loc.HasEntry(null)` 的具体行为没读** —— 但日志已给出等价的硬证据：`:1773` 期望值取到的是 `Account`（= `HasEntry(tabTitleKeys[2])` 判 false 那一支）。顺带指出：**那句断言文案里的 `Loc.HasEntry("")` 是假象** —— `:655` 的 `tabTitleKeys` 第 3 格就是字面 `null`，`$"{null}"` 印成空串；**生产用的键是 `Settings/Account/Title`（非 null）**。文案建议顺手写死真键（否则下一个人会照这句假象去查）。
3. **原版 `m_fontSize = 43` 只核到 Discord 那颗**（`_tmp_acct_full.txt:215`）；另两颗（Facebook / Youtube）是**同一个常量数组**给的 43（`:726`），没有再逐颗回 dump 核 —— 与「3 个 38.70」的读数自洽。

---

## 5. 顺手发现（⛔ 只报不改）

1. 🔴 **五个社交 logo【一次都没画】—— `AcSocArt` 是死代码。** `SettingsWindow.cs:700` 定义了
   `AcSocArt`（`Discord-Logo-Color` / `Instagram_icon` / `fb-icon` / `Twitter_Social_Icon_…` / `YouTube_…`），
   但 `BuildAccountPage` 的 ⑤ 那一段（`:1840-1873`）**只有 `Node(...)` + `Text(...)` + `Hit(...)`，一次 `Rect` 都没有**
   ⇒ 那五颗是**空节点**、连「图缺了」都不会出声（日志里只有 `Copy@3x`，**没有任何一条社交图名**）。
   ⚠️ **这与 W4 报告 §1.3 的说法相反**（它写「那六颗…（`Tex` 已经出声）」、§7.6 写「图导进来**本行一个字都不用改**」）
   —— 社交那五颗**没有那一行**：把 PNG 导进 `Resources/Art/ui_menu/` 之后**它们仍然是空的**，还得补 5 行 `Rect`。
2. `Player Id` 那一格整棵是 `SetActive(false)`（原版恒关）⇒ 里面 `External Link Icon` 就算按 #2 修好，
   在本窗的可见验收里也**永远看不到**（只有树断言看得见）。
3. `AcSocKey`/`AcSocEn`/`AcSocUrl` 三把数组与 `AcSocNode` 同长（5）；⑤ 段里的 `int si = i` / `int k = i`
   拷贝是**该保留**的（`:1859-1870` 的注释记着崩溃成因）—— 别趁重构删掉。
