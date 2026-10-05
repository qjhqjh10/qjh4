# F8 · RewardsScene 最后两条红（`Collect` 前提）修复（**写手** · 2026-10-11 · 批次1）

白名单内只改了**两个 `.cs`**：`MyGame/Assets/CardPresentation/Editor/RewardsScene.cs` · `MyGame/Assets/CardPresentation/Shell/RewardWindow.cs`。
⛔ 没跑 Unity 自检（按简报由主对话跑 `RewardsScene.Run`，~11 秒）；✅ 跑过秒级类型检查（见 §六）。
`Shell/PointerLayer.cs` 与 `Shell/WindowsManager.cs` **一个字没动**（它们的口径经查**是对的**，见 §②·3）。

---

## ① 结论（一句话 + 三句）

**一句话**：`Shell/RewardWindow.cs` 在 `MenuDraw.Hit` **已经挂好那颗 `WindowButton`** 的命中节点上
**又挂了一颗** ⇒ 同一节点两颗同型按钮共用**同一颗**命中 quad ⇒ **队列与 z 逐位相同** ⇒
`PointerLayer` 那条「队列大的先、同队列 z 小的先」**分不出它们**，赢家退化成 `FindObjectsByType` 的**枚举顺序**；
而自检取件走的是 `GetComponentInChildren`（组件表**第一颗**）—— **两把尺子各挑一颗** ⇒
「引用相等（真鼠标点得到）」那条前提间歇性红。

1. **不是「点不到」**（所以**不能**当成真功能缺陷去改实现语义）：那两条红的同时，
   同一段的 `★ 点 Collect ⇒ OnCollect **发了一次**` 是**绿**的（`rewards.log:14492`）——
   两颗的 `onClick` **都**绑着 `OnCollectClicked` ⇒ 真点那一下**照样发**。
   **判别力**：`ButtonAt(中心) != wb` 而 `ClickAt(中心)` 又确实派发了 ⇒ 「赢家在这一颗的节点上、但不是这一颗」。
2. **根因是「一节点两颗」这件事本身**（本仓早就写成规矩的那族地雷，见 `Shell/MenuDraw.cs:1495-1499`
   「`ImageQuad` 世界 z 恒 0 ⇒ **同档时谁吃到命中退化成枚举顺序**，症状是**点不动的钮看着像正常工作**」）。
   全工程 `grep` 核过：**只有这一处**把 `AddComponent<WindowButton>()` 挂在 `MenuDraw.Hit`/`AddHit` 的返回值上。
3. **修法 = 删掉那颗冗余的**（`MenuDraw.Hit` 收的实参与那两句**逐字同源**：`onClick = OnCollectClicked`、
   `Bind(btnQ, ArtCollectBtn)`）⇒ 赢家唯一化，`ButtonAt == wb` **确定性成立**。
   ⛔ **没有**放宽/删掉那两条前提断言（它们是「点不到 ≠ 功能没做」的分辨器），也**没动**被测实现的行为。

---

## ② 证据

> ⚠️ **行号口径**：本节里凡引自**日志 / D3 报告 / 调用栈**的 `RewardsScene.cs` 行号都是**改动前**的
> （那些文件是当时的快照，⛔ 不许改）；**§③ 里的是改动后**的。改动后 `RewardsScene.cs` = **5705 行**（改前 5658）。
> 两套的换算：`ClickButtonByQuad` 内部 `+4`（`479 → 483`，那两条红的 `CheckTrue` 那一行）、§⑦ 起 `+32`（`4041 → 4073`）。

### 1. 失败现场（`d:/4/_tmp_view/rewards.log`，一次运行、共 `1178 通过 / 2 失败`）

| 行 | 内容 |
|---|---|
| `:14465` | `✓ （前提）`Collect Button` 上有 `WindowButton`` |
| `:14478` | `✗ 原版奖励窗的 `Collect`：（前提）…（引用相等 ⇒ 真鼠标点得到） —— 期望 [True]，实得 [False]`（栈：`RewardsScene.cs:479` ← `Run():4047`，**改动前**的行号） |
| `:14492` | **`✓ ★ 点 `Collect` ⇒ `OnCollect` **发了一次**`** ← **关键** |
| `:14504` | `✗ 原版奖励窗的 `Collect`（第 2 下）：同上`（`Run():4049`） |
| `:14518` | `✓ ★ …第 2 下**不再发**` |
| `:20679` | `=== 合计：1178 通过 / 2 失败 ===`（`:20690` / `:20701` 是 Unity 退出时把两条 error **重打**了一遍，不是多出来两条红） |

⇒ **`:14492` 绿 = 那一下真的派发进了 `OnCollectClicked`**。而全文件**只有一处**绑 `OnCollectClicked`
（`Shell/RewardWindow.cs` 的 `Build()` 里那颗 `Hit` 的 `WindowButton`）⇒ 赢家**就在 `Collect Button/Hit` 这个节点上**。

### 2. 「那一刻 `ButtonAt(中心)` 到底返回什么」—— 逐条排除三个候选方向（都用上面的实据，不是猜）

| 候选 | 判 | 依据 |
|---|---|---|
| ① `Collect` 按钮那一刻**不激活** | ❌ 排除 | `:14465` 那条前提绿（节点在）；F7 改的 `ConfigureIsPreviewState` 只管 `_bgGet/_bgPrev/_glowGet/_glowPrev` **四件**（`Shell/RewardWindow.cs:498-499`），**不碰** `_collect`（`:414` 单独一句 `_collect.SetActive(ctx.OnCollect != null)`） |
| ② 被**更高队列**的东西盖住（压暗层 / 吸收层 / `Menu Vignette` / 壳窗） | ❌ 排除 | 那一刻 (960, 940) 上的候选只有三颗：`BackgroundHit` **3130**、`AbsorbHit` **3137**（`MenuDraw.Absorb` 恒用 `qContentMin−1`，`MenuDraw.cs:1708`）、`Collect` 的 `Hit` **3138**（`Shell/RewardWindow.cs:372` 传 `QCollectBg`）⇒ 3138 最大。**而且**：若赢家是前两颗，`ClickAt` 要么什么都不做（吸收层 `absorbOnly`）、要么**关窗**，**两种都不会**让 `collectCalls` 0→1 |
| ③ 中心落在**渲染矩形之外**（`QuadRectOf` 与 `HitBoxPx` 口径不一致） | ❌ 排除 | `ClickAt` 用的是**同一个** `cx/cy`、走**同一个** `HitButton` ⇒ 它命中了 ⇒ 那个点就在命中 quad 里。「矩形口径不一致」会**同时**让 `ClickAt` 落空（`:14492` 就红了） |

⇒ 只剩一种：**赢家在这一颗的节点上，但不是 `rwWb` 指的那一颗组件**。

### 3. 根因（代码级，逐句核过）

- `Shell/MenuDraw.cs:1410` `Hit(...)` 的**尾段**（`:1452-1458`）：建 `Hit` 节点 → `MakeHitQuad` →
  **`var wb = hit.gameObject.AddComponent<WindowButton>();`（`:1455`，无条件）** → `wb.onClick = onClick` →
  `if (target != null) wb.Bind(target, art, hoverArt, pressedArt)`。
- `Shell/RewardWindow.cs`（改前 `:353-359`）：`var hit = MenuDraw.Hit(btn, "Hit", CollectBtn, QCollectBg, OnCollectClicked, btnQ, ArtCollectBtn);`
  **后面紧跟** `hit.gameObject.AddComponent<WindowButton>()` + `wb.onClick = OnCollectClicked` + `wb.Bind(btnQ, ArtCollectBtn)`
  ⇒ **同一个 `GameObject` 上两颗 `WindowButton`**，实参逐字同源。
- 两颗的命中框来自**同一颗** quad（`PointerLayer.cs:813-819` `HitQuad` = `GetComponentInChildren<ImageQuad>()`）
  ⇒ `q.RenderQueue == 3138`、`q.transform.position.z` **同值** ⇒ `HitButton`（`PointerLayer.cs:975-987`）
  的挑法（`q > bestQ || (q == bestQ && z < bestZ)`，`<` 是**严格**）**分不出它们**，
  赢家 = `AllButtons()`（`PointerLayer.cs:824-825` = `Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None)`）**先枚举到哪颗**。
- 自检那一侧：`rwWb = rwBtn.GetComponentInChildren<WindowButton>(true)`（改后 `RewardsScene.cs:4075`）= 组件表**第一颗**。
  ⇒ 两把尺子**可以各挑一颗**（本轮实测就是这样）。
- **全工程核过**（`grep -rn "AddComponent<WindowButton>()"` + 每处回看 30 行）：
  **只有 `Shell/RewardWindow.cs:356`（改动前那 7 行里的那一句）**的宿主是 `MenuDraw.Hit`/`AddHit` 的返回值；
  其余每一处（`CampaignRewardWindow.cs:579` · `CardDetailPopup.cs:791` · `PromptPopup.cs:224` ·
  `MenuDraw.cs:2079` · `MainMenuRuntime.cs:378/784/804/873/1162` …）建的都是**自己的节点** ⇒ 单颗。
  ⇒ **6 处 `ClickButtonByQuad` 全绿、只有这一处红**，与这个「独一份」完全吻合。

### 4. 「是不是新暴露 / 早退吞掉的？」—— **都不是**（含对比结论）

1. **不是「F7 放出来的早退」**：这两条**从来没被吞过**。同一段的 `OnCollect` 计数那条（`:4048`）在两次运行里都**跑到了**，
   两条前提就在它**前面一行**（`:4047` vs `:4049`），同一个 `if (rwWb != null)` 块里 —— 没有早退的结构。
2. **不是 F7 引入的回归**：F7 对**本夹具**是**行为等价**的 —— 本档 `IsPremiumLocked = false / IsPreview = false`
   （`RewardsScene.cs:3910`）下，那四处 flag 改动**前后同解**（`_premium`: `false&&…` = `false&&…`；
   `_tap`: `!false` = `!false`；`_reveal`: `0` = `0`）；A 组那句 `CloseAllWindows()` 从旧 `:4170` 挪到 `:4432`
   —— **两处都在 `:4047` 之后**。
3. **它本来就是【枚举顺序】决定的 ⇒ 时绿时红**（这是**本轮**才第一次落到红的那一边）：
   **反证 = D3 自己的输入**（`资料/普查产出_1010/D3_Rewards八条红诊断.md:3` 写明输入是
   `d:/4/_tmp_view/rewards.log` 那一次 **1163 通过 / 8 失败**），而 D3 §一/§二 列的 8 条 = A 组 4 张截图
   + B1（`RectMask2D`）+ B2 + B3 + B4 —— **`Collect` 那两条前提不在其中**。而 B1 在 `:3916`、
   §⑦ 在 `:4041-4052`，**同一个新增块里**（D3 §二 B1 原话：「戊2 新段（新增块 3871..4172 内）」）
   ⇒ **那一次它们跑到了、而且是绿的**。
4. **⚠️ 直接逐行对比做不到（如实记）**：那次 1163/8 的日志路径**就是同一个** `d:/4/_tmp_view/rewards.log`，
   已被本轮 10:06 那次覆盖；`grep -rl "原版奖励窗的" d:/4/_tmp_view/` 全目录**只命中当前这一份**。
   ⇒ 「两次结果不同」这条是**由 D3 的失败清单反推**出来的，不是两份日志并排比出来的。
   **本轮的诊断加强（§③·B）就是为了下次不用再反推**。
5. **它与 `HEAD` 的关系**：`git show HEAD:…/RewardsScene.cs | grep -c "原版奖励窗的"` = **0**
   ⇒ 这两条调用点是**本工作区未提交的新活**（`RewardWindow` 自检段整个是新增块），
   所以「历史日志里没有它」不影响上面的结论。

---

## ③ 改动清单

### A. `MyGame/Assets/CardPresentation/Shell/RewardWindow.cs`（**删掉冗余的第二颗**）

| 行（改后） | 改动 |
|---|---|
| `:353-372` | 删掉 `if (hit != null) { var wb = hit.gameObject.AddComponent<WindowButton>(); … }` 那 6 行，**逐条写明为什么它冗余、代价是什么、改坏法是什么**（铁律 5）；`:372` 只剩 `MenuDraw.Hit(btn, "Hit", CollectBtn, QCollectBg, OnCollectClicked, btnQ, ArtCollectBtn);` |

**等价性（逐条核过）**：`MenuDraw.Hit` 内部 `wb.onClick = onClick`（= `OnCollectClicked`）、
`wb.Bind(btnQ, ArtCollectBtn, null, null)`（`hoverArt`/`pressedArt` 默认 null = 原来那句两参调用的默认值）
⇒ 删掉之后**行为一字不变**，只是不再有第二颗。

### B. `MyGame/Assets/CardPresentation/Editor/RewardsScene.cs`（**诊断** —— 简报第 1 条的硬要求）

| 行（改后） | 改动 |
|---|---|
| `:472-488` | `ClickButtonByQuad`：失败文案**不再只打 `[True]/[False]`** —— 现在带上**中心点 `(cx, cy)`** + **实得赢家** + **期望那一颗**的完整身份（`BtnId`） |
| `:495-514` | **新增** `static string BtnId(WindowButton b)`：**名字 + 短父链（4 层）** + **命中 quad 的队列 / z**（= `PointerLayer` 挑赢家那两个键；不满足 `HitQuad` 门槛的**明说「不进命中表」**，免得队列数把人带偏）+ **同一节点上有几颗 `WindowButton`** |

**判别力（一句话）**：下次再出这条红，日志里会直接写「实得赢家 `Hit`/…/Collect Button · 队列 3138 · z 0.000 ·
同节点 `WindowButton` **2** 颗（这一颗是第 2 颗）」—— 一眼看出是「一节点两颗」，不用再像本轮那样从
`OnCollect` 计数那条绿里去反推。

### C. `MyGame/Assets/CardPresentation/Editor/RewardsScene.cs` §⑦（**新增一条断言，旧的一条没动**）

| 行（改后） | 改动 |
|---|---|
| `:4077-4091` | **新增**：`Check(rwWbs.Length, 1, "（前提）`Collect Button` 上**只有一颗** `WindowButton`（实得 N 颗 —— 同节点多颗时队列/z 分不出它们，命中赢家退化成枚举顺序）")`，`rwWbs = rwBtn.GetComponentsInChildren<WindowButton>(true)` |

- **它在断的是本仓那条既有规矩**（`Shell/MenuDraw.cs:1495-1499`：同档命中区谁吃到 = 枚举顺序 ⇒ 是缺陷），
  ⛔ **不是**从被测实现里读期望值（数的是**场上节点上有几颗组件**，与 `RewardWindow` 的常量无关）。
- **不弱化任何东西**：`:4074`(取件) / `:4076`(有) / `:4094`+`:4096`(两条 `ClickButtonByQuad`) /
  `:4095`+`:4097`(两条 `OnCollect` 计数) **一条没删、一个字没改**。
- **断言条数净变化**：**+1**（§⑦）。⚠️ 复跑合计应从 `1180` 变 **`1181 通过 / 0 失败`**。

### ③·改坏法（每条都能真红）

| # | 改坏法 ⇒ 红 |
|---|---|
| C（新那条） | 把 `RewardWindow.Build()` 里 `hit.gameObject.AddComponent<WindowButton>()` **加回去** ⇒ **立刻红**（确定性，不必去撞枚举顺序） |
| A + 前提 | 只把 C 删掉、A 也还原 ⇒ 又退化成**间歇红**（本轮症状） |
| A | 把 A 改成「删 `MenuDraw.Hit` 那一侧、留自己那颗」⇒ 悬停换图那条链会断（`Bind` 不在同一颗上）＋ `CheckArt`/`AuditHoverSwap` 一族会红 |
| B | 把 `BtnId` 换回 `[False]` 两个字 ⇒ **不红**（诊断不是断言）—— 如实记，它只加**可读性**，判别力在 C |
| ⛔ 禁止的「修法」 | 把 `wb` 换成「这一点上枚举出来的第一颗」再比 ⇒ **自证**（拿被测枚举顺序当期望），且「点不到 ≠ 功能没做」这条分辨器**当场失效** |

---

## ④ 没查清的部分（如实记）

1. **没跑到实况**：⛔ 没跑 Unity（红线）⇒ 本轮**没有**打印出「那一刻赢家到底是第几颗组件」。
   上面的结论 = **代码逐句 + 日志三条（`:14465` / `:14478` / `:14492`）** 推出来的，
   ⛔ **不是**实测打印。**修完之后的复跑会有**（`BtnId` 已在文案里）。
2. **`FindObjectsByType(FindObjectsSortMode.None)` 的实际枚举顺序**：**查不到、也没法在批处理里量**（没跑 Unity）。
   它是 Unity 内部行为，本仓无判据。**修完之后这件事不再影响任何断言**（赢家唯一了），所以**不打算再查**。
3. **`RewardWindow.cs` 是未入 git 的新文件**（`git status` = `??`）⇒ `git diff --numstat` 对它**无输出**，
   本报告里它的「改动行号」是**当前文件的行号**，不是 diff 行号。
4. **两条红的**「第 2 下」**那一条为什么也红**：它跟第一条走同一条路（同一个 `rwWb`、同一个中心）⇒ `:14492` 绿、
   `:14518` 绿，说明**第 1 下已经把 `_collectArmed` 关掉了**，第 2 下本来就不该发（那条是绿的）。
   ⚠️ 但**「第 2 下」的前提那条红与 `_collectArmed` 无关** —— 它是**纯引用比较**，两次都输在同一个根因上。
   ⇒ **一条根因，两条红**（不是两个独立缺陷）。

---

## ⑤ 顺手发现（**只报不改**）

1. 🔴 **`Shell/RewardWindow.cs:549-550` 的 `Dump()` 仍把两个 flag 混成一串**
   （`态 PremiumLocked/Ok · /Preview//Collect` —— `Ok` 与 `Preview` 是**两把尺子**）。纯诊断输出、不影响任何断言
   （F7 已记过一次，本次复核**仍然成立**）。**建议**（不在本轮白名单内）：拆成 `PremiumLocked=? · Preview=?`。
2. **同族「一节点两颗 `WindowButton`」这次查完 = 0 处**（全工程 30 处 `AddComponent<WindowButton>()` 逐处回看 30 行）——
   如实记一个**反向结论**，省得下一个人再查一遍。
3. **同族「自己建 `Hit` 节点 + 自己挂 `WindowButton`」还有 6 处**（`DeckSelectionPopup.cs:567` ·
   `ImportDeckPopup.cs:330` · `PracticeModePopup.cs:1707` · `ShopWindow.cs:680` · `MenuDraw.cs:2079` ·
   `MainMenuRuntime.cs:378`）—— 它们**各自复制了一遍 `MenuDraw.Hit` 的尾段**（建节点 → 建透明 quad →
   `AddComponent` → `onClick` → `Bind`）。**今天不是缺陷**（每处只有一颗），但属于 CLAUDE.md §三
   「两处写同一条规则 = 迟早不一致」那一族。**只记账**。
4. **`BtnId` 只有 `RewardsScene.cs` 有**：它是通用诊断（`ClickButtonByQuad` 的失败文案），
   别的宿主没有 `ClickButtonByQuad` 的同族副本（`grep` 核过）⇒ 没有第二份要一起改。

---

## ⑥ 跑过的检查

| 检查 | 读数 |
|---|---|
| **秒级类型检查** `TMPDIR=/tmp/wf_f8 bash d:/4/Unity/工具/typecheck.sh` | **运行时程序集 0 错 · 编辑器程序集 0 错**（改完两个 `.cs` 之后跑的**最后一次**） |
| **Unity 自检** | ⛔ **没跑**（按简报：由主对话跑 `RewardsScene.Run`，~11 秒）。⚠️ Unity 只能串行 |
| 行尾 | 两个文件**纯 LF**（`RewardsScene.cs` 0 CRLF / 5705 LF · `RewardWindow.cs` 0 CRLF / 561 LF）· ⛔ 没用 `sed -i`，一律 Edit 工具 |
| 越界 | 只动白名单里的两个 `.cs`；`PointerLayer.cs` / `WindowsManager.cs` **未动**（§②·3 已证它们的口径是对的）· ⛔ 没动 git · ⛔ 没改正本与 `资料/待办判据_*.md` / `资料/已知的坑.md` |

### 复跑时对一眼（**期望合计**）
本轮之前 = `1178 通过 / 2 失败`（总数 1180）⇒ 预期 **`1181 通过 / 0 失败`**：
两条红翻绿（**+2**）+ 新增那条计数断言（**+1**）。
⚠️ 若通过数**低于** 1181 或失败数**不为 0** ⇒ 按 `资料/已知的坑.md` 那条「通过数也是判据」的口径查（早退 / 异常吞断言）。
⚠️ 若「同节点 `WindowButton` N 颗」出现在**任何**失败文案里 ⇒ 说明**又有人往 `MenuDraw.Hit` 的返回值上挂了第二颗**。
