# W · 小账两笔 —— `A1082`（`EndPanelProbe` 的静默 `return`）+ `A1083`（提示行预算）（动手写手交件 · 第六会话 · 2026-10-19）

> 范围 = **一笔两件**：`A1082`（全仓**最后一处**独立的同形静默 `return`，`Editor/EndPanelProbe.cs`）
> · `A1083`（`Shell/SearchingOpponentWindow.cs` 的「≤40 字」预算**重算**）。
> **只改了白名单里的 3 个文件**：`Editor/EndPanelProbe.cs` · `Shell/SearchingMatchPopup.cs` · `Shell/SearchingOpponentWindow.cs`。
> ⛔ **本轮一条 Unity 自检都没跑**（用户口径：待办没做完之前不跑）；✅ 秒级类型检查跑了 **4 趟**（见 §④，其中**第 3 趟被别人正在写的 `Core/Tooltip.cs` 污染**，如实记着）。
> ⚠️ 简报里的行号**已漂**（上一批写手在后）⇒ 本件一律**按现读定位 + 按符号名索引**。

---

## ① 结论（两笔各一句）

1. **`A1082`**：✅ **改了** —— `Editor/EndPanelProbe.cs` 的 `Shot()` 里 `Camera.main == null` 那一支**不再静默**：
   现在**出声**（`Debug.LogError(P + "  ✗ 截图 <file>：**没拍成** …")`）**并返回 `false`**，两个调用点各把它计进
   本探针**自己的失败账**（`shotFail`，进「探针跑完」那行合计 + 进 `EditorApplication.Exit` 的退出码）；
   **行为那半句一个字不变**（照旧**不写图、不渲**）。
   ⚠️ **没有用 `CheckSink`** —— 本文件**全文没有 `CheckSink` / `s.Fail`**（它是**独立探针**：自己建场景、自己开相机、
   自己数 `bad`）⇒ 照它**自己已有的**输出方式（`Debug.LogError(P + "  ✗ …")`，与本文件上面三处数量/显隐断言逐字同形）**+ 它自己的失败账**。
   ⛔ **没为这一处去引 `Editor/MenuCheck.cs`**（那会把一个**正被别的写手改**的文件拖进本笔）。
2. **`A1083`**：✅ **改了，选的是 ①「改预算」**（⛔ 不是「改截断」）—— 预算从「**字符数 ≤ 40**」改成
   「**字形宽度 ≤ 80 个半宽字位**」（中文 1 字 = 2 位 ⇒ **中文仍是 40 字**；英文 ≈ **80 字**）。
   **英文档下最长那条实测 = `140` 字**（`_started` 时对面离开：`Lobby/DeferToBattle{PeerLeftFrag + body}`）
   ⇒ 🔴 **改完仍然超**（140 > 80，英文档那几条 91~140 **一条都放不进**）—— **这是如实结论、不是没改完**
   （阈值算准之后这条告警的含义才明确）。**超限照旧出声**（两扇窗的 `ShowHint` 都 `LogWarning`）、**照旧照画**。

---

## ② 改动清单（`文件:行号 | 改前 | 改后`）

> 行号 = **改后现读**。三份文件**开工时在工作区里都是干净的**（`git diff --numstat` 空）。

### `A1082` · `Editor/EndPanelProbe.cs`

| # | 文件:行号 | 改前 | 改后 |
|---|---|---|---|
| 1 | `Editor/EndPanelProbe.cs:50-51` | `int bad = 0;` | 同一行补注释 + **新增** `int shotFail = 0;`（截图没拍成的张数） |
| 2 | `Editor/EndPanelProbe.cs:74` | `Shot(c.Name);` | `if (!Shot(c.Name)) shotFail++;` |
| 3 | `Editor/EndPanelProbe.cs:84` | `Shot("04_投降.png");` | `if (!Shot("04_投降.png")) shotFail++;` |
| 4 | `Editor/EndPanelProbe.cs:86-89` | `Debug.Log($"{P} ===== 探针跑完（数量对不上的：{bad}）=====");` / `… Exit(bad == 0 ? 0 : 1);` | 合计行改成「（数量/显隐对不上的：{bad} · **截图没拍成的：{shotFail}**）」 / `… Exit(bad == 0 **&& shotFail == 0** ? 0 : 1);`（+ 3 行注释写为什么） |
| 5 | `Editor/EndPanelProbe.cs:92-105` | （无 doc） | 新增 `Shot` 的 doc：**改前为什么不合格 / 行为哪半句不变 / 为什么用 `shotFail` 而不是 `CheckSink`** |
| 6 | `Editor/EndPanelProbe.cs:106` | `static void Shot(string file)` | **`static bool Shot(string file)`** |
| 7 | `Editor/EndPanelProbe.cs:109-114` | `if (cam == null) return;`（**静默**） | `if (cam == null) { Debug.LogError(P + "  ✗ 截图 " + file + "：**没拍成** —— `Camera.main == null`（改前这一支是**静默 return**…）"); return false; }` |
| 8 | `Editor/EndPanelProbe.cs:130` | （无） | `return true;`（拍了的那一路） |

### `A1083` · `Shell/SearchingMatchPopup.cs`（预算的**定义处**）

| # | 文件:行号 | 改前 | 改后 |
|---|---|---|---|
| 1 | `Shell/SearchingMatchPopup.cs:405-452` | （无） | **新增 48 行的账**：那 40 是怎么算的 / **为什么今天必须重算**（`P6d` 之后这一行放的是**我们自己的文案**）/ 重算 = **同一套算法换字宽** / **为什么不按语档分支** / **为什么选「改预算」不选「截断」** / **英文档逐路径长度表（最长 140）** / **改完仍超** / **没查清的一条** |
| 2 | `Shell/SearchingMatchPopup.cs:450-453` | （无） | 新增 `public const int HintLineMaxWidth = HintLineMaxChars * 2;`（**80 个半宽字位**） |
| 3 | `Shell/SearchingMatchPopup.cs:455-459` | `public const int HintLineMaxChars = 40;`（doc = 原那条粗算） | **名字与取值一字不改**（`Editor/NetSelfTest.cs` 拿它当中文档上限读）⇒ **只换 doc**：说明它现在是「= `HintLineMaxWidth ÷ 2`」、判「会不会被压小」要用 `HintLineWidth` |
| 4 | `Shell/SearchingMatchPopup.cs:461-481` | （无） | 新增 `public static int HintLineWidth(string text)`（中文/日文/全角 = 2 位、其余 = 1 位；**代理对算一个全宽字**） |
| 5 | `Shell/SearchingMatchPopup.cs:483-498` | （无） | 新增 `static bool WideGlyph(char c)`（东亚宽/全角区间，照 `Unicode EAW = W/F`） |
| 6 | `Shell/SearchingMatchPopup.cs:502-521` | `if (text.Length > HintLineMaxChars)` + 旧文案 | `int width = HintLineWidth(text);` / `if (width > HintLineMaxWidth)` + 新文案（**同时报「N 个半宽字位」与「N 个字符」**） |

### `A1083` · `Shell/SearchingOpponentWindow.cs`（**消费侧** + 订正那句错账）

| # | 文件:行号 | 改前 | 改后 |
|---|---|---|---|
| 1 | `Shell/SearchingOpponentWindow.cs:75-89` | 「…三个收包入口都在 `NetSession`…⇒ 走到本窗的字符串**已经 ≤ `NetProtocol.MaxPeerTextChars`**…同 `NetMatchmaking.cs:450-452` 的口径…本行按框只放得下约 40 字」 | **就地订正**（铁律 5，留订正痕）：`P6d` 之后是「先钳 → 再取词」⇒ **到这一行的是我们自己的文案、可以远超 40**；≤40 管的是**对端原串**、不是取词后的整句；入口改列**四个**（按符号）；`NetMatchmaking.cs:450-452` 那条**行号引用删掉**（它早就漂到一个别的 case 上）⇒ 改成按 `case` 名引；预算指到 `HintLineMaxWidth` |
| 2 | `Shell/SearchingOpponentWindow.cs:105-115` | `if (text.Length > SearchingMatchPopup.HintLineMaxChars)` + 旧文案 | `int width = SearchingMatchPopup.HintLineWidth(text);` / `if (width > SearchingMatchPopup.HintLineMaxWidth)` + 新文案（**与弹窗那一份同口、同一把尺子**） |

---

## ③ 证据

### 3·1 `A1082` 的判据与「为什么这么选」

- **判据** = `资料/普查产出_第六会话/W_断言尾巴_A1031与A1007.md` §一（`A1007①`）· §二 #1 · §六·1：
  `A1007` 把 `Editor/MenuCheck.cs` 那处改成 `s.Fail++` + `Failures.Add` + `Debug.LogError("   ✗ 截图 …：没拍成")`，
  并**点名** `Editor/EndPanelProbe.cs:91` 是**同形的第 8 份独立副本**（**在白名单外**，那一轮没动）。
- **现读落点**：`git show HEAD:…/EndPanelProbe.cs` 与工作区**一致**（未提交改动 = 0）⇒ 简报说的 `:88-91` 现读就是
  `static void Shot(string file) { var cam = Camera.main; if (cam == null) return; … }`（`return` 在 `:93`，差 2 行 = 漂）。
- **为什么这么选（简报点名要写清）**：`EndPanelProbe.cs` **不接 `CheckSink` 那一套** ——
  全文搜不到 `CheckSink` / `s.Fail`（它是**独立探针**：`Run()` 自己 `NewScene` + 自己造带 `MainCamera` 标签的相机 + 自己数 `bad`）。
  ⇒ 按简报那句「用它**自己已有的**输出方式出声」：
  ① **出声**照本文件**已有的方言** `Debug.LogError(P + "  ✗ …")`（`:63` / `:68` / `:72` 三处逐字同形）；
  ② **进失败账**照本文件**已有的账**（`bad` 那条线 + `EditorApplication.Exit`）—— 只是**另开一格** `shotFail`，
  因为合计行原来写的是「**数量**对不上的」（把截图失败塞进 `bad` 会让那行**说假话**）。
  ⛔ 没引 `MenuCheck`（白名单只允许「`A1082` 确实要用它那一支时」改它 —— 本件**确实没用**，所以**一行没碰**）。

### 3·2 `A1083` 的判据与重算

- **判据 ①**（简报指定）= `Net/NetMatchmaking.cs` 的 `DeferToBattleLayer` 那段账 + `HandleLobbyPeerClosed`
  那两段（**现读在 `:226-248` 一带；简报说的 `:226-233` 差几行、按符号认**）：
  它逐字写着「`what` + 字面量 ≤ 40」（中文档），并**自己就标了**「⚠️ **英文档本来就超**（`DeferToBattle` 的 EN 列
  光模板就 86 字）—— 那是**接线前就有的**……`HintLineMaxChars` **只对中文列算过**」。
- **判据 ②**（现读该窗口自己那条算式）= `Shell/SearchingMatchPopup.cs` 的 `HintLineMaxChars`，原 doc 写着：
  「框 700×148 · 自适应 4~50 ⇒ **50px 时一行约 14 字 × 约 3 行 ≈ 41 字**，留余量取 40（**中文按等宽算**；拉丁字更窄 ⇒ 实际更多）」。
  ⇒ 两条判据**指向同一件事**：那个 40 的**算法里写死了「中文的字宽」**。
- **重算**（与那 40 **同一套算法**、只换字宽）：
  中文字宽 = 1 em ⇒ 700 ÷ 50 = 14 字/行 × 3 行 = 42 → 取 40（≈5% 余量）；
  拉丁字宽 ≈ 0.5 em ⇒ 700 ÷ 25 = 28 字/行 × 3 行 = 84 → 同比例留余量 = **80**。
  ⇒ 统一成「**半宽字位**」，上限 **80 位**（`HintLineMaxWidth`），中文 1 字 = 2 位。
- **两道旁证让 80 不像硬凑的**：① 自检清单里那两条**自己写下来的中文模板**（`PeerLostHint` 29 字 / `PeerLeftHint` 15 字）
  换算后是 58 / 30 位，**都在 80 以内**；② `PeerLostHint` 的 **EN 模板 = 78 字符**（未替换 `{0}`）—— **78 < 80**，
  即「英文整句模板级」本来就刚好卡在这条线内，**是替换 `{0}` 那一段把它顶出去的**。
- **英文档逐路径长度（2026-10-19 逐条算出来，脚本见 §④ 末）**：

  | 路径（`NetMatchmaking`，`SetHint` 是私有 ⇒ **全仓唯一写口**） | EN 字符 | 中文档字符 |
  |---|---|---|
  | `_started` 时掉线 `DeferToBattle{PeerLostFrag}` | 106 | 29 |
  | `_started` 时离开 `DeferToBattle{PeerLeftFrag + body}` | **140 ← 最长** | 42 ~ 70 |
  | 未开局掉线 `PeerLostHint{tail}` | 110 ~ 124 | 36 ~ **40** |
  | 未开局离开 `PeerLeftHint{body, tail}` | 105 | 35 ~ 63 |
  | `Reset` 后对面回来 `LobbyRestored` | 91 | 39 |

  （`body` = 对端转述那一段：收侧**先 `NetSession.ClampPeerText` 钳到 40** 再 `Unpack` 取词 ⇒ **上界 40**；
  `tail` = `Lobby/MatchRevoked`(EN 35 / ZH 10) 或 `Lobby/NotMatchingThisGame`(EN 49 / ZH 14)。
  另：`_started` 时 `what` 里若 `body` 命中 `Wire/*`，EN 最长那条是 `Wire/BadResume` = 40 —— **也还是 40**。）
- **改完还超不超**：**超**。英文档 5 条路径 91/105/106/110~124/**140** 位，**一条都不止 80** ⇒
  `ShowHint` 在英文档下**仍然每次都出声** —— **照旧（⛔ 没有改成静默）**。
  阈值算准之后这条告警的含义变清楚了：「**这一行在满字号 50px 下装不下它**」，而不是原来那句拿中文尺子量出来的假告警。
- **⛔ 为什么不选「改截断」**：这一行的整句就是「告诉玩家刚才发生了什么」 —— 截了 = **把该玩家看的话吃掉**；
  而「超限」只是个**诊断阈值**（超了照旧画）。两扇窗各有一份 `ShowHint`，截断还要**同步改两处**且会**动到**
  `SearchingOpponentWindow.HintText`（自检读它）。⇒ **把阈值算准 + 照旧出声**。
- **⛔ 为什么不按 `Loc.Current` 分支**（`Chinese ? 40 : 80`）：词条表**只有中、英两列**（其余 10 档一律**回退英文**，
  见 `Loc` 文件头）⇒ 语档分不出字形宽度；一句话里还能**中英混排**（`{0}` 里就是对端转述那段）⇒
  只有**看字本身**才算得对。顺带也就不碰「别按语档写死」那条纪律。

### 3·3 与既有的两条断言/见证**对得上**（改前逐条核过，⛔ 没有一条被本件推翻）

| 既有那条 | 本件之后 |
|---|---|
| `Editor/NetSelfTest.cs:205-209`：`Loc.T("Lobby/PeerLostHint").Length <= SearchingMatchPopup.HintLineMaxChars` | ✅ **一字不用改** —— `HintLineMaxChars` **名字与取值都是 40**（本件没动它） |
| `Editor/NetSelfTest.cs:1174-1186`（`A932⑤`）：推一句 **79 字符**的「对端文本」提示，断**原样照收**（不做第二道钳） | ✅ **仍然原样照收**（本件**不加任何截断**）；那句实测 **154 个半宽字位 > 80** ⇒ **照旧触发告警**（与那条注释写的「会顺带触发…那是预期的」一致） |
| 中文档那条账（本工程原口径） | ✅ **一条新告警都不会多**：`位 ≤ 2 × 字符数` ⇒「**新判据会喊**」必然推出「**旧判据也会喊**」；反向只在 41~42 字**擦边**处可能从「喊」变「不喊」（= 少喊一句，是**选定的方向**，已在注释里写明） |
| `Net/NetProtocol.cs` 的 `MaxPeerTextChars = 40`（**对端原串**那把钳） | ✅ **两件事、互不动** —— 那 40 管「对端可控的原串」，本件管「这一行放不放得下」 |

---

## ④ 验证

**① 秒级类型检查**（`TMPDIR=/tmp/wf_small bash d:/4/Unity/工具/typecheck.sh`，**共 4 趟**）：

| 趟 | 结果 |
|---|---|
| 1（改完 `A1082` 全部 + `A1083` 主体） | `运行时错误数: 0` / `编辑器错误数: 0` |
| 2（`A1083` 注释订正后） | `运行时错误数: 0` / `编辑器错误数: 0` |
| 🔴 3 | `运行时错误数: 1` —— **`Core/Tooltip.cs(513,94): error CS1002`**：**不是我的文件**，`ls -l` 读到它的 mtime = **我这次读表前 1 秒**（另一个写手**正在写**它）⇒ 按简报「报错全在你白名单之外就不是你的问题」，**⛔ 我没碰它** |
| 4（等 75 秒后复跑） | `运行时错误数: 0` / `编辑器错误数: 0` ✅（⇒ 第 3 趟的错确实是**别人的半成品**，已自愈） |

**② 行尾**（改前 → 改后，二进制读 `b.count(b'\r\n')` vs `b.count(b'\n')`）：

| 文件 | 改前 | 改后 |
|---|---|---|
| `Editor/EndPanelProbe.cs` | `CRLF=0 / LF=107`（纯 LF） | `CRLF=0 / LF=130`（**仍纯 LF**） |
| `Shell/SearchingOpponentWindow.cs` | `CRLF=0 / LF=350`（纯 LF） | `CRLF=0 / LF=364`（**仍纯 LF**） |
| `Shell/SearchingMatchPopup.cs` | `CRLF=0 / LF=442`（纯 LF） | `CRLF=0 / LF=539`（**仍纯 LF**） |

（三份**一个都没翻行尾**；全程用 **Edit 工具**，⛔ 没用 `sed -i`、⛔ 没用文本模式 `open(...,'w')`。
⚠️ 本次**踩到一次 Edit 的坑**：第一版写 `WideGlyph` 时把区间边界写成**字面字符**，想改成 `\uXXXX` 转义时
Edit **匹配不上**（报「也试过转义互换」）⇒ 改用 python **二进制读**把码点打出来核（`1100/115F · 2E80/303E ·
3041/33FF · 3400/4DBF · 4E00/9FFF · A000/A4CF · AC00/D7A3 · F900/FAFF · FE30/FE4F · FF00/FF60 · FFE0/FFE6`
—— **逐条正确**）⇒ 字面字符版**原样留着**，**没再动**。）

**③ `git diff --numstat`**（只列本批动的 3 个文件；开工时它们**两个数都是 0/0**）：

```
 30   7   Unity/MyGame/Assets/CardPresentation/Editor/EndPanelProbe.cs
105   8   Unity/MyGame/Assets/CardPresentation/Shell/SearchingMatchPopup.cs
 24  10   Unity/MyGame/Assets/CardPresentation/Shell/SearchingOpponentWindow.cs
```

（数字都远小于各文件行数 ⇒ **没有整篇重写的行尾事故**。⚠️ 工作区里**还有一大批别的改动**（`Net/` · `RuleEngine/` ·
`Editor/其它宿主` · `资料/` …）= **别的写手 / 上一会话**的，**不是本件**。）

**④ 长度账怎么算出来的**（可复算）：本件写了一个一次性 python 脚本把 `Core/Loc.cs` 的 `{ "键", new Entry(ZH, EN) }`
**按 C# 字符串字面量规则**解出来（含 `\n` / `\"` / `\\` 转义），再按 §③·2 那五条路径把 `{0}`/`{1}` 替换掉、
按 `WideGlyph` 那套区间数「半宽字位」。脚本落在 `%TEMP%/wf_small/`（**⛔ 不在工程里、不在白名单外乱写**）。

---

## ⑤ 没查清 / 停手的

1. 🔴 **140 个英文字符在 700×148 里究竟被压到多少 px、那个字号还看不看得清 —— 量不到。**
   **不许跑 Unity** ⇒ **没有渲染就没有读数**；我**没有**用「大概是 30px 还能看」这种推算去顶替（也**没有**据此把
   80 改大/改小）。⇒ 如实留在这儿：**「改完仍超」这条结论是确定的**（140 > 80 是算术），
   **「那条告警在英文档下算不算误报」是不确定的**。若调度台要收口它，判据只能来自**一次真渲染**（不在本笔白名单内）。
2. ⚠️ **`A1082` 改动了 stdout**（原「（数量对不上的：N）」→「（数量/显隐对不上的：N · 截图没拍成的：M）」）
   —— **只在「这一笔要出声」时才用得上那半句**，但**行本身永远变**。若某个基线/对账脚本在**逐字**比这行
   （`EndPanelProbe` **不在**那 12 条自检里，我没找到引用它的基线），那是一处要跟着冻的地方 —— **本件没跑，故没验**。
3. ⚠️ `EndPanelProbe` 的**退出码**现在会把「没拍成」也算作失败（`bad == 0 && shotFail == 0`）——
   这是我按 `A1007`「落进失败合计里，谁都不会错过」那条**故意**做的；**若调度台要的是「只出声、退出码不变」**，
   把 `&& shotFail == 0` 删掉即可（一处，1 行）。

---

## ⑥ 顺手发现的（⛔ 都在白名单外，**一个字没碰**）

1. 🔴 **`Editor/NetSelfTest.cs:1116-1118`（`A932⑤` 的 doc）里那句「超过 40 字」已经过期**：
   它说那条断言会「顺带触发 `ShowHint` 那条**超过 40 字**的告警」。行为**仍然对**（实测 **154 位 > 80** ⇒ 照旧触发），
   但**判据的名字变了** ⇒ 那句话该改成「占 80 个半宽字位」。**不在本笔白名单**。
2. 🔴 **`Net/NetProtocol.cs` 的 `MaxPeerTextChars` doc（`:282-285`）写死了行号**：
   「取 40 的判据 = ……`Shell/SearchingMatchPopup.cs:408` 的 `HintLineMaxChars = 40`」—— **`:408` 早就漂了**
   （现读那 40 在 `:459`；而且今天起它身边**多了一个 `HintLineMaxWidth = 80`**）。那段注释**自己**在
   `:279-280` 刚写完「⛔ 以后别在本文件写死行号」⇒ 反手又写了一个。**不在本笔白名单**。
3. ⚠️ **`Core/Loc.cs:1491-1492`** 里那句「`PeerLostHint`/`PeerLeftHint` 是提示行（`HintLineMaxChars = 40`…）」
   **仍然成立**（中文档），但它只提了中文那一把尺子；今天起**英文列走 80 位**。
   旁证一条：两条模板的 **EN 列 = 78 / 22 字符**，**都在 80 以内**（是替换 `{0}` 之后才顶出去）。
4. ⚠️ **`Shell/SearchingOpponentWindow.cs` 里那条已经漂掉的引用已被我顺手改掉**（`NetMatchmaking.cs:450-452`
   → 按 `case` 名引）：现读 `:450-452` 落在 `case NetKind.Deck` 的**函数体中间**，不是那条口径 ——
   与「英文文字档里写死行号」是同一族病（本笔在**自己白名单内**的那一份已经清掉）。

---

## 摘要（300 字以内）

两笔都做完了，只动白名单 3 个文件（`Editor/EndPanelProbe.cs` · `Shell/SearchingMatchPopup.cs` ·
`Shell/SearchingOpponentWindow.cs`），**未跑 Unity**，秒级类型检查 4 趟（**第 3 趟被别的写手正在写的
`Core/Tooltip.cs` 污染**，等 75 秒复跑已 0 错），行尾仍是纯 LF，`git diff --numstat` = 30/105/24。

`A1082`：`EndPanelProbe.Shot` 的 `Camera.main == null` 那支**不再静默** —— `Debug.LogError("   ✗ 截图 …：没拍成")`
+ 返回 `false`，两个调用点计进**本探针自己的失败账** `shotFail`（合计行 + `EditorApplication.Exit` 退出码）；
**行为那半句不变**（照旧不写图、不渲）。**没用 `CheckSink`**：该文件是独立探针、全文没有 `CheckSink`/`s.Fail`
⇒ 照它**自己已有的**方言（`Debug.LogError(P + "  ✗ …")`，同文件三处同形）与自己的账 ⇒ `MenuCheck.cs` **一行没碰**。

`A1083`：预算从「字符数 ≤ 40」改成「**半宽字位 ≤ 80**」（同一套算法换字宽：中文 1 字 = 2 位 ⇒ **中文仍 40 字**，
英文 ≈ 80），`HintLineMaxChars = 40` **原样保留**（自检读它）；选 **①改预算**（⛔ 不截断）。**英文档最长实测 140 字**
（`DeferToBattle{PeerLeftFrag+body}`）⇒ **改完仍超**（91~140 无一 ≤ 80）⇒ **照旧出声、照旧画**。
没查清一条：140 英文字符**实际被压到多少 px** —— 不许跑 Unity ⇒ 没有渲染就没有读数。
