# WE5 · Label 第三个静默口（A491）

> 独占文件：`Battle/Label.cs`（共用件，动前已 `grep` 全部使用方）· 断言宿主 `Editor/SettingsScene.cs`（现核：本批**无人认领**，见 §六·1）
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用只读的 `git status` / `git diff --numstat`）· ⛔ 没改两张正本
> ⛔ 没越白名单（`Battle/{BattleDriver,ScenarioBlendables}.cs` · `Shell/*`（11 个写了名字的）· `Deck/DeckRuntime.cs` · `工具/*` · `Editor/ChatBoxProbe.cs` **全部只读**）
> ✅ 类型检查**改完立刻**跑过：`TMPDIR=/tmp/wf_we5 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**

---

## 一、结论

**A491 做完**：`Label.SetAlignLeft()` 在 `_tmp == null`（点阵后端）时**出声了**，且新口的 key 与 A476 **不撞**（两口的 key 见 §三）。改动极小、**有字体资产时行为逐位不变**（只有 `_tmp == null` 那一路多打一行日志），另在 `Editor/SettingsScene.cs` 落了 **6 条断言**（含 1 条直接咬「key 不撞」的判据 + 1 条反面对照）。

| 项 | 状态 | 一句话 |
|---|---|---|
| **A491 主体** | ✅ **做了** | `Battle/Label.cs:316-324` 的早退分支由 `return;` 改成 `NoteDotAlign("逐行左对齐", …); return;` |
| **出声助手** | ✅ **收成唯一一口** | `NoteDotAlign` 由「一个 2 参函数」重构成「1 个 3 参核心 + 1 个 2 参转发」⇒ **key 的拼法只有一份**（`which + "|" + 节点全路径`），A476 那条消息**逐字节不变**（已用字符串拼接复算验证，见 §二） |
| **key 不撞** | ✅ | A476 = `左对齐` / `右对齐`；**本件 = `逐行左对齐`**（`HashSet` 是全串相等 ⇒ 不相撞；且**前缀也不含**关系） |
| **断言** | ✅ **6 条**（§五） | ①点阵出声（A476 回归）· ②同节点上 `SetAlignLeft` **也**出声（= **key 不撞**的判据）· ③有 TMP 时**一行日志都不许出**（反面对照）+ 3 条前提 |
| **改坏法** | ✅ 逐条写了 | 删出声 ⇒ ②红；把 `which` 抄成 `左对齐` ⇒ ②红（静默复发）；把 `Debug.Log` 搬出 `if` ⇒ ③红 |
| **类型检查** | ✅ **0 / 0** | 见 §八（**没有一条错落在别人的文件上**） |
| **行尾** | ✅ **没翻** | 二进制读：`Battle/Label.cs` = **CRLF 979 · LF 979**（**纯 CRLF**，改前 952）· `Editor/SettingsScene.cs` = **CRLF 0 · LF 2524**（**纯 LF**，改前 2429） |

---

## 二、改动清单（行号 = 本件收工时**现读**；⚠️ 行号会漂，一律以你现读为准）

| # | 文件:行 | 改前 | 改后 | 照的是 A476 哪一处 |
|---|---|---|---|---|
| 1 | `Battle/Label.cs:316-324`（`SetAlignLeft`） | `if (_tmp == null) return;`（**一个字都不留**） | `if (_tmp == null) { NoteDotAlign("逐行左对齐", "逐行左对齐（每行在块内贴左）", "没生效、每一行仍在块内居中"); return; }` | 照 `:802` / `:812`（`AlignLeftOn` / `AlignRightOn` 的 `if (_tmp == null) { NoteDotAlign(…); return; }`）——**逐字同形** |
| 2 | `Battle/Label.cs:841-844`（旧 `NoteDotAlign(string, float)` 的函数体） | 函数体里自己拼 path / 自己 `Add` / 自己 `Debug.Log` | 收成**一行转发**：`NoteDotAlign(which, which + "（x=" + worldX + "）", "没生效、整块停在框心");` | A476 原实现（`:828-835`）—— 只把「消息装配」挪进核心，**key 规则一处没动** |
| 3 | `Battle/Label.cs:846-862`（**新增**核心 `NoteDotAlign(string which, string wanted, string symptom)`） | —— | path 拼法 / `_dotAlignNoted.Add` / `Debug.Log` **只此一份**；`which` 既是口名又是 key 头段 | A476 的 `_dotAlignNoted`（`key = 方法 + 节点全路径`，**每处一次**）那套原封不动搬进来 |
| 4 | `Battle/Label.cs:306-314`（`SetAlignLeft` 的 doc） | 无 | 补一段「**A491：点阵后端必须出声**」+ 「⛔ 别把 `which` 改成 `左对齐`/`右对齐`（撞 key ⇒ 静默复发）」+ 断言的落点 | 照 `:820-838`（A476 那一整套「为什么出声 / 为什么不逐次刷屏」doc 的口径） |
| 5 | `Editor/SettingsScene.cs:1533-1589`（**新增** `Section("A491：…")` 一整块） | —— | 6 条断言，见 §五 | 断言写法照 `资料/普查产出_1012/H37_字距顺序与静默口.md` §五·A476 给的形状（`AddComponent<Label>()` 造点阵后端 + 捕 `Application.logMessageReceived`） |
| 6 | `Editor/SettingsScene.cs:243-258`（**新增** `CaptureLogs(System.Action)`） | —— | 抓**全部**日志（含 `Log`）的网 | 同文件现成的 `CaptureErrors`（`:232`）**只收 Error/Exception** ⇒ 出声那类 `Debug.Log` 用它**恒为空**（会假红），所以另开一只 |

🔴 **A476 的消息逐字节没变**（本件复算过）：

```
改前： "[Label] ⚠️ 这一处要" + which + "（x=" + worldX + "），但**点阵后端没有对齐这回事**" + " ⇒ 没生效、整块停在框心（…）"
改后： "[Label] ⚠️ 这一处要" + wanted + "，但**点阵后端没有对齐这回事** ⇒ " + symptom + "（…）"
       其中 wanted = which + "（x=" + worldX + "）" · symptom = "没生效、整块停在框心"
⇒ 两串在 Python 里拼出来 `old == new` = **True**（同一个表达式的 float 转字符串位置也没变，culture 效应一致）
```

**没改**的（逐条核过）：

- 两个 `Align*On` 的**正常路径**（`RefreshBounds` / `HasMeasuredWidth` / `ParentXInDesignSpace`）**一字未动**；
- `HasMeasuredWidth()` 那条早退**照旧不出声**（A476 的有意守卫，理由见那一段 doc）；
- `SetAlignLeft` 的正常路径（`_tmp.alignment = TextAlignmentOptions.Left;`）**一字未动**；
- 同族**其它** `_tmp == null` 早退（`SetWrapWidth` / `SetWrappingMode` / `ForceRelayout` / `SetFontSize` / `SetAutoFitBox`）**一处没碰** ⇒ 顺手发现 §七·1。

---

## 三、key 不撞车核对

| 口 | 方法 | `which`（= key 的头一段） | 现读 |
|---|---|---|---|
| A476 | `AlignLeftOn` | **`左对齐`** | `Battle/Label.cs:802` |
| A476 | `AlignRightOn` | **`右对齐`** | `Battle/Label.cs:812` |
| **本件（A491）** | `SetAlignLeft` | **`逐行左对齐`** | `Battle/Label.cs:320` |

- key 的**完整形状** = `which + "|" + 节点全路径`（`Battle/Label.cs:860`，**只此一份**）。
- `_dotAlignNoted` 是 `System.Collections.Generic.HashSet<string>` ⇒ 判等是**全串相等** ⇒ `左对齐|P` 与 `逐行左对齐|P` 是**两个不同的 key**（既不是同串，也不构成前缀误判 —— `HashSet` 不做前缀匹配）。
- 🔴 **为什么这也需要断言咬住**：`_dotAlignNoted` 是**进程内静态**、同一 key **只出声一次** ⇒ 一旦有人把新的 `which` 写成 `左对齐`，症状是「**同一节点上 A476 先响过一次之后，`SetAlignLeft` 再也不出声**」（静默复发，**只在同一个进程里现形**）—— 所以 §五 的 ★② 特意**在同一个探针节点上先调 `AlignLeftOn` 再调 `SetAlignLeft`**。

---

## 四、`Label` 使用方 grep 清单（哪些文件在用 · 本改动对它们的影响）

**搜法**（可复现）：`grep -rln "\bLabel\b" --include=*.cs MyGame/Assets/CardPresentation/`

| 口径 | 命中 |
|---|---|
| 含注释里提到 `Label` 的 | **88 个 `.cs`**（Battle 18 · Core 8 · Deck 2 · Shell 49 · Editor 10 · Net 1） |
| **去掉整行注释**、真用到类型的 | **71 个 `.cs` / 577 处**（最多的：`Editor/MainMenuScene.cs` 65 · `Editor/RewardsScene.cs` 52 · `Editor/CollectionScene.cs` 50 · `Deck/DeckRuntime.cs` 42 · `Editor/SettingsScene.cs` 32 · `Editor/ShopScene.cs` 29 · `Editor/BattleScene.cs` 20 · `Editor/DeckScene.cs` 19 · `Battle/BattleDriver.cs` 18 · `Shell/SettingsWindow.cs` 17） |

**`SetAlignLeft` 的全部调用点（现读，共 7 处）**：

| 调用点 | 性质 | 这一处走哪条路 |
|---|---|---|
| `Battle/CardDisplayWindow.cs:228` | 生产（`_fxTitle`） | 该 Label 由 `MenuDraw.Text(...)` 建 ⇒ 字体资产在时是 **TMP 后端** ⇒ **一个字都不出**（行为零变化） |
| `Battle/CardDisplayWindow.cs:263` | 生产（`_fxWho[i]`） | 同上 |
| `Battle/CardDisplayWindow.cs:272` | 生产（`_fxWhat[i]`） | 同上 |
| `Battle/UnitChatPanel.cs:309` | 生产（台词框） | 同上（`b.text` 由 `Label.Create` 建） |
| `Editor/ChatBoxProbe.cs:38 / :54 / :63` | 探针（`ChatBoxProbe.Run`，**不在 11 条自检里**） | `Label.Create` 建 ⇒ 同上；**本件没碰这个文件** |

**本改动对使用方的影响**：

- **有字体资产（= 现在所有自检、所有生产路径）** ⇒ 走的还是那句 `_tmp.alignment = …`，**一行日志都不多** ⇒ 对全部 71 个使用方、全部自检**逐位零影响**。
- **没有字体资产（`TmpFont.Available == false`）** ⇒ 那一批 `SetAlignLeft()` 会各打**一行** `Debug.Log`（**每个「节点全路径」一次**，不是每次调用一行）。给「谁在吞日志」的宿主提个醒：
  - `Editor/{BattleScene,CollectionScene,RewardsScene,SettingsScene,ShellScene,ShopScene}.cs` + `Core/ClickLog.cs` 共 **7 处**挂 `Application.logMessageReceived`（现读全仓）。
  - 逐个核过：**都是窄作用域 + 按类型/子串过滤**（例 `Editor/ShopScene.cs:1616-1624` 只数 `LogType.Warning` 且 `Contains("没有奖励表")`；`Core/ClickLog.cs:89-94` 只在 `_capturing` 为真时收、且只留 24 条）⇒ **点阵后端那一行 `Log` 不会被任何一条既有断言数进去**。⚠️ 前提是**字体资产在**（不在时那几行断言里最该红的是 `Editor/SettingsScene.cs` 的 A228 前提 `CanRenderChinese`）。
- 唯一的**新增依赖**：`SetAlignLeft` 现在**依赖 `NoteDotAlign` 存在**（同文件私有函数 + 静态 `HashSet`）—— 已过类型检查。

---

## 五、断言清单（断什么 · 怎么分辨两种状态 · 改坏法 · 用的 id 是不是本次运行没出现过的 · 落点）

**落点**：`Editor/SettingsScene.cs:1533-1589`（`Section("A491：…")`），位置 = **A165 那一块（`:1312-1515`）之后、A167 那一节之前** —— 该处**不在任何 `if` 守卫里**（`Run()` 体里的自由语句，同 A167），且**不依赖任何窗口对象**。
**宿主自检**：`SettingsScene.Run`（11 条里的第 10 条，实测 ~10s）。
**为什么选它**：① 同族断言（A228 的 `Label.Align*On` 两态）**本来就在这个文件**；② 本批**无人认领** `Editor/SettingsScene.cs`（判据见 §六·1）；③ 我的断言**不需要窗口**，但 `BattleScene.Run`（另一处现成的 Label 断言宿主）**有在飞写手**（见 §六·1）。

| # | 行 | 断什么 | **怎么分辨两种状态** | **改坏法（怎么改就红）** | 用的 id / 名字 |
|---|---|---|---|---|---|
| 前提① | `:1540` | 探针落在**点阵后端**（`CanRenderChinese == false`） | —— | 夹具坏了（`AddComponent` 那条路万一建出 TMP）⇒ 红 | 节点名 **`A491 dot-align probe`** |
| **★①** | `:1545-1550` | **回归 A476**：`AlignLeftOn(1f)` 在点阵后端下**出声**（消息含 `点阵后端没有对齐这回事`） | 与「不出声」两态：出声那一条靠**同一只网**在**同一次调用**里抓到的行数（打印出来了） | 删掉 `AlignLeftOn` 首句那句 `NoteDotAlign(...)` ⇒ 红 | 同上（**第一次**用它） |
| **★★②** | `:1555-1562` | **本账**：`SetAlignLeft()` 在点阵后端下**出声了**（`a491L2.Count > 0`）。**它同时是「key 不撞 A476」的判据** —— ① 刚在**同一节点**上消费过 key，② 还能响 ⇒ 两口 key 不同 | 出声 / 不出声两态（**故意不断文案**：文案是定位锚、不是判据） | ① 删掉 `SetAlignLeft` 里那句 `NoteDotAlign(...)`（退回静默 = 修前状态）⇒ 红；② 把它那一支的 `which` 也写成 `"左对齐"` ⇒ ② 被 ① 吃掉 ⇒ 红（**静默复发**） | 同上（**第二次**用，正是「撞车」的那一格） |
| **★③** | `:1580-1587` | **反面对照**：**有 TMP** 时 `SetAlignLeft()` **一行日志都不出**（`Count == 0`） | 与②合起来才分得出**三态**：②只断「出声」⇒ **无条件出声照样绿**；③只断「不出声」⇒ **把出声整句删掉照样绿** | 把 `NoteDotAlign` 那一句搬到 `if (_tmp == null)` **外面** ⇒ 红 | 节点名 **`A491 tmp label`**（另起一棵 `A491 tmp probe root`） |
| 前提② | `:1571` | `TmpFont.Available`（③ 要的就是「有 TMP」那一档） | —— | 没字体资产 ⇒ 红（**同族前提**：A228 那一节也会红，所以本条的加入不改变「谁先红」） | —— |
| 前提③ | `:1576` | ③ 的探针确实是 TMP 后端（`CanRenderChinese == true`） | —— | 夹具坏了 ⇒ 红 | `A491 tmp label` |

🔴 **「本次运行没出现过的 id」那条坑（`资料/已知的坑.md` §「按 key 做进程内去重」）已经照做**：

- `_dotAlignNoted` 是**进程内静态** HashSet、key 含**节点全路径** ⇒ **探针节点名必须是本次运行没出现过的**：本案用 **`A491 dot-align probe`**（全仓唯一，本趟第一次出现）+ **`A491 tmp label`** ⇒ 不会命中跑过的 key（**假红**）。
- 本条**只跑一次**（两个口在同一个节点上各调一次）⇒ 不触碰 H37 §五·A476 标注的那条「同一进程里只能跑一次、否则假红」。
- 前提③ 那一条**另起一棵树**（`A491 tmp probe root`）—— 因为**同名节点**会让两个探针的 key 相同（`HashSet` 按全路径判等，两个根同名 = 两条 key 撞在一起）。

**灭自证（防「两边一起改回去」）**：★② 与 ★③ **结构上不可能同时被「单向改坏」满足** ——

- 静默（删出声）⇒ ②红；无条件出声 ⇒ ③红；**key 撞车** ⇒ ②红。三种改法各撞一条，且 ②③ 分别用「有/无 TMP」两档夹具，**不是同一条判据的两种写法**。

---

## 六、没查清 / 没做的

1. 🔴 **断言宿主的「所有权」我是按 1013 批次计划判的，不是当场问的**（简报允许「拿不准就只报不改」，我的判断依据逐条列出，请调度台复核）：
   - `资料/普查产出_1013/批次计划_1013.md:83-91`（在飞表）与 `:93-150`（波 3–8）**都没把 `Editor/SettingsScene.cs` 列进任何写手的「独占文件」**；
   - `git status --porcelain`：**它没有未提交改动**（同为 Label 断言宿主的 `Editor/BattleScene.cs` 有 **83/2** 的未提交改动 = W-B1 的 A513/A410/A515，**所以我没碰它**）；
   - `A435_迁移表.md:251-263` 的「丙」把 `SettingsScene.Run` 当**跑的对象**，但它 owns 的是 `Shell/*.cs` + `Editor/ShellScene.cs`（**不含 `Editor/SettingsScene.cs`**）。
   - ⚠️ **风险如实说**：`A338`（全仓「指向我们自己 `.cs` + 行号」411 处）与将来的 A435 丙**都会碰自检宿主**（A338 明说要动**全部六份宿主**）⇒ 本件在 `Editor/SettingsScene.cs` 里**加了 95 行**，**行号会漂**。我加的都是**新块**（`:243-258` 与 `:1512-1589`），删掉成对即可回退。
2. ⚠️ **断言没跑**（用户口径：A 表清完再跑；⛔ 本件不许跑 Unity）⇒ 本件是**静态核对 + 类型检查 0/0**，**不是「跑绿了」**。要跑的就是 **`SettingsScene.Run`**。**这一条要如实转述**。
3. ⚠️ **通过数会变**：本件给 `SettingsScene.Run` **加了 6 条 `CheckTrue`**（§五表）⇒ 收口跑那一趟的「断言合计」会比上一轮 **+6**（若都过）。**别把 +6 当成异常**。
4. ⚠️ **命中区/其它自检的覆盖面**：`Label` 是共用件（71 个文件真在用），但**本件的行为改动在「有字体资产」时为零**（只有 `_tmp == null` 那一路多一行日志）⇒ 从覆盖面看 **`SettingsScene.Run` 就够**；是否连跑别的宿主 = **调度台的活**（铁律 12 的「动共用件先 grep 谁在用」我已经做了，清单在 §四）。
5. **没做的**：`Editor/ChatBoxProbe.cs`（3 处 `SetAlignLeft`，探针，且在简报的「不许碰」语境里）**一个字没动**；`Battle/CardDisplayWindow.cs:379` 那条注释（「⚠️ 不能只调 `Label.SetAlignLeft()`」）**照旧成立**、**没改** —— 它讲的是**另一件事**（`SetAlignLeft` 只管 TMP 的 `alignment`、不管**块位置**；出声之后这条仍然对）。

---

## 七、顺手发现（⛔ 本件一个都没改）

1. 🔴 **同族的 `_tmp == null` 静默早退，`Label.cs` 里还有 5 处**（H37 §七·1 那个「**第三个**」只数了**对齐**那一族；A 表里**没有**下面这些的账）—— 逐条现读：

   | # | 现读 | 方法 | 现读行为 | 它自己的 doc 怎么说 |
   |---|---|---|---|---|
   | 1 | `Battle/Label.cs:243` | `SetWrapWidth(float)` | `if (_tmp == null) return;`（**不出声**） | 只说「点阵后端…」（`:267`：「拿不到字体资产时 `_tmp == null` ⇒ 待办恒空」），**没说出不出声** |
   | 2 | `:386` | `SetWrappingMode(int)` | `if (_tmp == null) return;`（**不出声**） | `:382` 写「点阵后端**直接返回**」（只描述行为） |
   | 3 | `:426` | `ForceRelayout()` | `if (_tmp == null) return;`（**不出声**） | `:421` 写「点阵后端…**什么都不做（如实，不假装）**」= **自陈静默** |
   | 4 | `:463` | `SetFontSize(float)` | `if (_tmp == null \|\| worldSize <= 0f) return;`（**不出声**） | `:459` 写「没字体资产时会**静默无效**」= **自陈静默** |
   | 5 | `:586` | `SetAutoFitBox(…)` | `if (_tmp == null) return;`（**不出声**） | 同一族的「折行 + 自适应」总口（它第一句就转调 `SetWrapWidth`） |

   **对照（已经出声的）**：`SetCharSpacing:478`（`Debug.Log`）· `AlignLeftOn:802` / `AlignRightOn:812`（`NoteDotAlign`）· `SetAlignLeft:320`（**本件**）。
   ⇒ 按 `CLAUDE.md` §三「**不许静默失败** —— 没实现的东西要**说出来**」，这 5 处**形状与 A476/A491 完全同族**（`_tmp == null` = 「这一档功能不存在」而调用方以为生效了）。**要不要出声、出声到什么粒度（每处一次 / 每次一行）是口径问题** —— 本件**不裁**（简报：不许自己发明口径），**请调度台另立账**（按铁律 11：**要做**，只有先后；⛔ 别写成「影响小、暂缓」）。其中 #3/#4 的 doc **自己就写着静默**，所以那两条还多一条「doc 与红线打架」，一并留给出账的人。
   ⚠️ **本件没改它们**（越界）：`SetAlignLeft` 之外的改动都会影响别的账（`SetAutoFitBox` = A492 正在动的那一口）。
2. 🟡 **`Editor/SettingsScene.cs` 的 A228 前提注释措辞略旧**（`:1523-1526` 那一带，本件**没改**）：
   现读写「（前提）TMP 后端在 —— 走点阵兜底时 `Align*On` **首句就 return**（空操作），下面四条无从谈起」。
   A476（2026-10-12）之后那两个方法在点阵后端**不再「空操作」**（会出声后 return）⇒ **不是错、但是旧措辞**（它想说的是「下面四条量不出东西」）。本件**没顺手改**（不是我的账，也不在这条简报范围内）。
3. 🟡 **`SetAlignLeft` 的 doc 里「⚠️ 要在**量尺寸之前**调」这条纪律，代码里没有守卫**（本件**没改**）：`SetAlignLeft` 只写 `_tmp.alignment`，不校验调用顺序；真违反了会不会有害**本件没查**（`UnitChatPanel` / `CardDisplayWindow` 的调用次序在它们的注释里另有说明）。
4. 🟢 **本件摸到的一条可复用经验**（不是账，是工具）：`NoteDotAlign` 这类「进程内静态 HashSet 去重」的口，**断言必须让两个不同的口在【同一个节点】上各响一次** —— 那是「key 不撞」**唯一**能被咬住的形状（换个节点就测不出撞车）。本件把它写进了 `Editor/SettingsScene.cs:1551-1554` 的注释里。

---

## 八、类型检查结果

```
TMPDIR=/tmp/wf_we5 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- **两次跑都是 0 / 0**（改完助手那次 + 改完断言那次）；⚠️ 按简报口径注明：**当时树里有别人的未提交改动**（`Battle/BattleDriver.cs` 110/23 · `Battle/ScenarioBlendables.cs` 150/6 · 各 `Shell/*` · `Editor/{BattleScene,DeckScene,MainMenuScene}.cs` 等）—— **没有一条错落在别人的文件上**，所以没有触发简报里那条「重跑 + 如实记」。
- 本件自己的 `git diff --numstat`（**只读命令**）：`Battle/Label.cs` **30 / 3** · `Editor/SettingsScene.cs` **95 / 0**。
- 行尾自检（二进制读，判据 = `b.count(b'\r\n')` 对 `b.count(b'\n')`）：`Battle/Label.cs` **979 / 979**（纯 CRLF）· `Editor/SettingsScene.cs` **0 / 2524**（纯 LF）⇒ **两个文件各自保持原样、一行都没翻**。
