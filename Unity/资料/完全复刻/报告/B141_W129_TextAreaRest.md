# B141_W68 · `/Text Area` 族**剩余站点**收口（条目 `4-23` 第二包）

> 执行写手 `B141-W68` · 第一百二十八会话 · 波次 `W129` · 2026-11-04
> 上包 = `报告/B141_W129_TextInputPad.md`（`B141-W25`，只覆盖 `TextInputField.Create` 的 4/4 实例）
> 判据 = `资料/完全复刻/判据/B141_W129_对账_UI动画.md` **W11 包** · 审计 = `报告/B138_审计汇总_问题清单.md` §4 `4-23`
> 白名单外一个字节没动（⛔ `TextInputField.cs` 机制、⛔ 账 / 活状态表 / `CLAUDE.md` / 所有权表）

---

## §1 原版判据（一处参数，38 处同一值）

| 项 | 值 | 出处 |
|---|---|---|
| `RectMask2D.m_Padding` | `(−8, −5, −8, −5)`（x=Left · y=Bottom · z=Right · w=Top，**负 = 往外扩**） | `GenericPromptWindow.prefab:1337` |
| `RectMask2D.m_Softness` | `(0, 0)`（硬边） | 同文件 `:1338` |
| 分布 | **19 个 prefab / 38 处**，**38/38 都挂在名为 `Text Area` 的节点上**、参数逐值同一 | `报告/B141_W129_TextInputPad.md` §1（扫描口径同那份） |

**站点矩形**（`python -I 工具/menu_dump.py … --md` 现读，⛔ 不是从我们的常量反算）：

| 站点 | 原版节点 | 原版矩形 |
|---|---|---|
| 联盟搜索框 | `Social Submenu Variant ▸ …/Search Field/Text Area` | `1406.57,172.90 → 1758.57,229.86`（352.00×56.96） |
| 好友搜索框 | `Friends Menu Demo ▸ …/Search Field/Text Area` | `403.34,173.36 → 885.42,230.31`（482.08×56.95） |
| 聊天输入行 | `ChatPanel ▸ …/InputField (TMP)/Text Area` | 宽 **1120.00** · 高 **26.53**（序列化高 = 0，运行期由 CSF/VLG 定成 `Placeholder` 那一档） |
| 设置窗账号页/登录窗 | `Main Menu Settings Window ▸ …/InputEmail` 与 `…/InputPassword` 的 `Text Area` | 左/右内缩 20 设计 px、高 31（本仓 `AcAreaInset` / `AcAreaH` 已是那组值） |
| 改名窗 | `ChooseNameWindow ▸ Choose Name Input Field/Text Area` | 本仓 `TaL/TaT/TaR/TaB`（早前已逐值核过，本轮未动） |
| 反馈窗（自由反馈） | `Give Feedback Popup menu ▸ Free feedback/Input field/Text Area` | `410.48,647.57 → 1509.52,887.32`（本仓 `InputTextR` = 410.50/647.60/1509.50/887.30） |
| 反馈窗（开放题 ×3） | `… ▸ {S2Q4,S4Q2_Extra,S4Q3_Extra} ▸ Text Area ▸ **Text Area**`（**内层**那颗挂掩码） | 外 `121.38,2237.63→1796.38,2457.63` / 内 `131.38,2244.63→1786.38,2451.63` |

---

## §2 站点逐条（9 个 `Text Area` 节点 + 1 颗漏裁的字）

| # | 文件:行号（现读） | 改前 | 改后 | 覆盖实例 |
|---|---|---|---|---|
| 1 | `Shell/SettingsWindow.cs:6814` | `MenuDraw.Node(f.Root, "Text Area", area)` 裸节点；两颗字各传 `clip: area`（**没按 pad 外扩**，比原版内缩 8/5 px） | `ViewportClip.Hang(f.Root, "Text Area", area, TextInputField.AreaPad, AreaSoftness)`；两颗字 `clip` 省略（父链解析） | **4**（账号页 `InputEmail`/`InputPassword` + 登录窗 2 颗，同一个 `AcInput.Create`） |
| 2 | `Shell/ProfileTab.cs:1336` | `Node(fld, "Text Area", …)` 裸节点；`:1329` 注释写「**我们没有掩码体系**」（**已过期**，本轮按铁律 5 就地订正） | `ViewportClip.Hang(fld, "Text Area", …)` | 1 棵（改名窗） |
| 3 | `Shell/GiveFeedbackPopup.cs:529` | `MenuDraw.Node(inputNode, "Text Area", InputTextR)` 裸节点 | `ViewportClip.Hang(inputNode, "Text Area", InputTextR, …)` | 1（自由反馈） |
| 4 | `Shell/GiveFeedbackPopup.cs:745` | `MenuDraw.Node(box, "Text Area", inner)` 裸节点（**内层**那颗） | `ViewportClip.Hang(box, "Text Area", inner, …)` | 3（`S2Q4` / `S4Q2_Extra` / `S4Q3_Extra`，同一个 `BuildOpen`） |
| 5 | `Shell/ReferralPopupWindow.cs:509` | `Placeholder` **没裁**（只有 `_inputText` 那颗裁了） | 补 `MenuDraw.ClipText(_placeholder, TextClipRect(), Vector2.zero)`（同一份 `TextAreaR + (−8,−5,−8,−5)` 框） | 1 |
| 6 | `Shell/ChatPanel.cs:241` | 两颗字直接挂 `InputField (TMP)`，只被外层 `Chat Tab/Viewport` 裁 | 新增 `Text Area`（`ViewportClip.Hang(input, "Text Area", InputR, …)`），两颗字（`:272` / `:284`）改挂它 | 1 |
| 7 | `Shell/AllianceSearchTab.cs:425` | 两颗字直接挂 `Search Field`，只被 `Open Alliances/Viewport` 裁 | 新增 `Text Area`（矩形 = 原版 `SearchPhR`），两颗字（`:441` / `:454`）改挂它 | 1 |
| 8 | `Shell/FriendsTab.cs:172` | 两颗字直接挂 `Search Field`，只被外层 `Viewport` 裁 | 新增 `Text Area`（矩形 = 原版 `PlaceholderR`），两颗字（`:174` / `:595`）改挂它；`WireSearchField` 多收一个 `sfArea` 形参 | 1 |

**几何零变化**：所有 `Hang` 用的矩形 = 该站点**原来那个节点/字**的矩形（`MenuDraw.Local` 会抵消父节点位移）⇒ 只改**裁切边界**（从「不裁 / 按裸矩形裁」变成「按原版 pad 外扩后裁」），不改任何件的落点。

**⛔ 有意**不给**外层**那颗带 `Image` 的 `Text Area` 挂掩码（`GiveFeedbackPopup.cs` 的 `MenuDraw.Node(node, "Text Area", areaR)` 保留裸节点）—— 原版那颗**没有** `RectMask2D`，掩码在内层那颗。断言 `B9` 钉着。

**顺带订正的三处过期注释**（铁律 5）：
- `Shell/ProfileTab.cs:57-58`（文件头「`Mask` / `RectMask2D` 不做真裁切 —— 我们没有掩码体系」）
- `Shell/ProfileTab.cs:1329-1332`（站点上同一句）
- `Shell/FriendsTab.cs:71-74`（`Placeholder` doc「我们没有掩码体系，只画占位、不裁」）
- `Shell/AllianceSearchTab.cs:414-416`（「`Search Field/Placeholder` 仍然是**直接子件**」—— 本批它是孙节点了；`MenuCheck.FindChild` 是**递归**的 ⇒ 三条断言照样绿，已核 `Editor/MenuCheck.cs:141-147`）

---

## §3 行数变化（全部纯 LF · 无裸 CR）

| 文件 | 前 | 后 | Δ |
|---|---:|---:|---:|
| `Shell/SettingsWindow.cs` | 7,438 | 7,442 | **+4** |
| `Shell/ProfileTab.cs` | 2,017 | 2,024 | **+7** |
| `Shell/GiveFeedbackPopup.cs` | 1,249 | 1,256 | **+7** |
| `Shell/ReferralPopupWindow.cs` | 891 | 895 | **+4** |
| `Shell/ChatPanel.cs` | 1,352 | 1,358 | **+6** |
| `Shell/AllianceSearchTab.cs` | 1,141 | 1,155 | **+14**（其中 **+5 不是本包**：`B141-W70` 同时在改这份文件，见 §7-1） |
| `Shell/FriendsTab.cs` | 696 | 703 | **+7** |
| `Editor/B141_W68_Assert.cs`（新建） | — | 355 | +355 |

---

## §4 断言：新建 `CardPresentation/Editor/B141_W68_Assert.cs`（`class B141W68_Assert`，全局命名空间）

**绿跑共 55 条（A 36 · B 19）**；另有 7 条「源码读得到」的前提（`B0a`–`B0g`）**只在失败时报红**。

| 段 | 条 | 内容 |
|---|---:|---|
| A（运行时夹具，用完即销毁） | **36** | 三颗**原版矩形**的 `Text Area` 节点（联盟 1406.57→1758.57 / 好友 403.34→885.42 / 聊天 1120×26.53）：pad 四分量**逐字面量** `(−8,−5,−8,−5)` · softness `(0,0)` · `BaseRect` 逐值 · `RenderClip` = 再外扩 8/5 后的四个数 · 结构性判别式 `ΔW=16 / ΔH=10` · 节点下的标签拿到 `ClippedTextGuard` |
| B（源码结构 · **非注释行**计数） | **19** | 8 个站点逐条：`ViewportClip.Hang(...)` 恰 1 · 旧 `MenuDraw.Node/Node(...)` 恰 0 · 两颗字挂新节点恰 2 · `clip: area` 恰 0 · `ReferralPopupWindow` 的 `Placeholder` 裁切恰 1 |

**灭自证（🧨 每条怎么才能红）**：

- **任意一处**退回 `MenuDraw.Node(...)` / `Node(...)` ⇒ 对应站的「Hang 恰 1」+「旧写法 0」**两条同时红**；
- **任意一处**只把一颗字的父件改回去 ⇒ 对应站的「挂 `xxx` 恰 2」+「旧写法 0」红；
- `SettingsWindow` 把 `clip: area` 加回来 ⇒ **B3 红**（且 `A15` 报「形参盖节点」那档会 +1 —— 那就是「节点白挂」的静默形态）；
- pad 改 0 / 正值 ⇒ **A2–A5 + A7/A8/A9/A11/A12** 一片红；
- **两条判据不共因**：「pad 值对不对」（A2–A5）与「框有没有真的外扩」（A9 / A12）分开断；「写法换没换」（B 组）与「值对不对」（A 组）也分开。
- **A14 是 A13 的反向控制**：同一颗字、父链上**没有**节点 ⇒ 不该有守卫。若 A13 在任何情况下都绿（例如守卫恒挂），A14 就会红。

**期望值来源**：全部是 **prefab / dump 字面量**（`−8/−5`、`1406.57`、`1120.00×26.53` …），⛔ 不从被测常量反算（否则「常量与断言一起改」就能蒙过）。

**接线（本包不动宿主）**：宿主合计行之前、**同一物理行、该行第一个 `//` 之前**追加
`B141W68_Assert.Run(); _sink.Pass += B141W68_Assert.Pass; _sink.Fail += B141W68_Assert.Fail;`
（⚠️ 计数器变量名以宿主当时那一对为准 —— 本仓已 6 次踩「代码落进行尾注释 ⇒ 编译过、断言一条没跑」。）

---

## §5 验证

- `cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w68 bash 工具/typecheck.sh`
- **最终：`运行时错误数: 0` · `编辑器错误数: 0`**（跑了 3 次：改完站点后 1 次、建完断言后 1 次、修完文档后 1 次，三次都是 0/0）。
- 文件行尾：8 份全部 **纯 LF · 无裸 CR**（逐份 `b.count(b'\r\n') == 0` 且 `lone_cr == 0`）。
- 源码闸自校验：断言 B 段那 **19 个 needle** 在本包 7 份源码里现数 = `1/0/0 · 1/0 · 1/1/0/1 · 1 · 1/2/0 · 1/2/0 · 1/2/0` —— 与断言期望**逐条一致（mismatch = 0）**。
- **⚠️ 没有跑 Unity**（红线）：断言**未实际执行**，只过了类型检查；接线由调度台完成。

---

## §6 🔴 账漂移提醒（**调度台必须串行处理**）

这 7 份文件**都被账按 `文件:行号` 引用**（`资料/完全复刻/账/*.tsv`，粗数：`社交_聊天` 115 · `菜单_外壳` 45 · `个人资料_成就_排行` 41 · `战斗界面_HUD` 20 …），
本包**共 +49 行**（不含新建的断言文件）⇒ `check_ledger_points` **会报一批漂移**。

⚠️ 按 §16·8 / 铁律 13·3：`--fix` **必须由调度台在「没有 `.cs` 写手 + 补丁已合并完」时串行跑一次**
（`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix`），本包**没有动账**。

---

## §7 ❓ 未查清 / 需要调度台知道

1. 🔴 **本包与 `B141-W70` 撞了同一份文件**：`Shell/AllianceSearchTab.cs:36-40` 在我改的过程中多出一段
   `✅ 已复刻（2026-11-04 · B141-W70 · W129 5-07 …）` 的注释（+5 行，**注释-only、未碰代码**）。
   ⇒ 两份改动**互不冲突**（typecheck 0/0 已证），但 §2/§3 的行号与行数**含那 5 行**，读之前请**现读**。
   按铁律 13·3 这属于**文件所有权**问题，本代理不擅自回退对方的改动。
2. `GiveFeedbackPopup` 开放题那颗**内层** `Text Area` 的内缩：我们写的是 `inner = areaR 四边内缩 10 / 7`
   （`Shell/GiveFeedbackPopup.cs:744`），原版 dump 是 **左 10 / 右 10 / 上 7 / 下 6**（`m_SizeDelta = (−20,−13)`，
   横向 10/10、纵向按锚点分则应是 6.5/6.5，dump 出来是 7 / 6）。
   ⇒ 我们比原版**高 1 px**（206 vs 207）。**本轮没改**：① 它是**几何**、不在本包「补 pad 掩码」的范围；
   ② 7/6.5 这一档**判据本身有歧义**（dump 的 7/6 是 float 往返的产物），说不上「我们错」。
   记在这里，等有人专门收「开放题输入框几何」时一起核。
3. 本包**没有跑 Unity**（红线）⇒ 断言只做了类型检查，**没有实际执行**；`ClipText` 在批处理里可能落进
   `TextClipUploadSkipped`（「这一段此刻没在渲染」）那条诊断 —— 那是既有行为，不是本包引入的。
4. `Shell/FriendsTab.cs` 的原版 `Text Area/Text` 那颗 TMP **本轮仍没读到字号档**（静态就是 U+200B 零宽字符、
   字号栏为空）⇒ 仍按占位那颗同档取（这是 `B102_FRIEND` 的既有如实记录，本轮未动）。
5. 📌 **`报告/B141_W129_TextInputPad.md` §4 的「❓ 剩余站点」表本轮已全部落地** —— 那份文件不在本包白名单、
   没有回改；建议调度台在它 §4 顶部补一句「→ 已由 `报告/B141_W129_TextAreaRest.md` 收口（2026-11-04 · `B141_W68`）」。
