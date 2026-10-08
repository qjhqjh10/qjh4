# W · A964 战斗侧命中区探针（`E4` / `E2` / 模型③）—— 交件

> 宿主 = `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（**只改了这一个文件**）。
> 本件**没跑 Unity**（红线）、**没动 git**、**没改正本**。所有「会不会红」都是**静态代码判定**。

---

### 一、探针落点（哪一段 · 多少行）· 扫了什么 · 输出什么

🔴 **落点与简报不符 —— 已就地改位（这是本件第一件要说的事）**：

| | 简报 / 侦察 §4·0 指定 | 实际 |
|---|---|---|
| 位置 | A463 段收口 **之后**、`// 收尾：把玩家的 Auto Zoom…` **之前** | **`DestroyImmediate(driver)` 那一句之前** |
| 为什么 | —— | 🔴 **那里 `driver` 已经被销毁了**。`BattleScene.cs` 里 `UnityEngine.Object.DestroyImmediate(driver);` 在 A463 段**之前**约 1200 行，紧跟其后还有一条 `Check(driver == null && …)` 作证（`Application.isPlaying == false` 这一档它就是 null）。 |
| 判据 | —— | 用括号深度脚本现算：A463 段收口处 `driver` 早已不在树里；**整轮里 `driver` 活着的最后一处**就是 `DestroyImmediate` 上一行。 |

⇒ 探针插在 `DestroyImmediate(driver)` **之前**，**净 +422 行 / −0**（`git diff --numstat` = `627/17`，**17 条删除是本件之前就有的**）。
⚠️ 这条一并解释「为什么侦察会写错」：侦察自己 §6·8 写着「全程没跑 Unity」，且它读的是文件头 `Run` 的开头，
**中段那次销毁没被读到** —— 属本工程既有模式「简报前提被现读推翻」。

**扫什么**：`E4` 5 档 × 两态 · `E2` 四条可量行 · 模型③ 攻击三选一。
**输出**：`d:/4/_tmp_view/hitprobe/battle_hits.tsv`（每行一条候选 + 四边证据 + 结论）+ 段尾一行计数（打「E4 白名单 = …」）。
⛔ 不写 `资料/`；⚠️ **这类缺陷截图截不出来**（「点不到」与「点得到」在静态图上一样）。

---

### 二、阳性样本（`E12`）的判别式：怎么造的 · 期望报什么

- **本段不新造夹具**：`E12` 的病就是「`ImageQuad.Contains` **不看 `activeSelf`**」，而
  `BattleDriver` 的聊天钮 / 墓园钮**已经带守卫**（`HandleChatPopup` 的 `!_chatBtn.gameObject.activeSelf`、
  `HandleBattleLog` 的 `!_cemeteryBtn.gameObject.activeSelf`）⇒ **今天是阴性**。
- **判别式 = 把守卫删掉，探针必须红**：`E4` 的「藏起来不许响应」那两条断言就挂在它上面
  （断言文案里写死了「🔴 这就是本探针的已知阳性判别式 = `E12`」）。
- 🔴 **记账（这是 `E12` 修复的网）**：`E12` 那笔修复**一条断言都没有** —— `HudButtonActiveForTest`
  在这之前**全仓 0 个调用点**；本段是它**第一个调用方**。
- 📌 `A8` 那族（卡组格裸节点）在**外壳侧** ⇒ 战斗侧**不重复造**（侦察 §4·3 同结论）；
  战斗侧没有「拿不到 quad」这一档（`Contains` 不看 `activeSelf`），`E1` 在战斗侧**不存在**，换成 `E2`。

---

### 三、`E4` / `E2` / 模型③：判据 · 两态 · 🧨 改坏法 · 灭自证 · 三条前提怎么保证的

**`E4`（5 档 × 两态）** —— 判据「藏起来的钮不许响应」（原版 uGUI 钮 `SetActive(false)` 后收不到点击）
- 摆状态一律走 `SetHudButtonActiveForTest`（**只 `SetActive`、不起 tween** ⇒ 不会污染 `CameraResetPunchCount`）。
- 喂点 = `HudButtonWorldPosForTest(which)`；每格喂**一整个按下→松手**（这五颗原版都是 `onClick` = **抬起那一帧**做事），
  取**松手那一帧** `Tick*ForTest` 的接管结果 + 一颗**看得见的副作用**。
- 🧨 **改坏法**：删掉对应 handler 里那一句 `!…gameObject.activeSelf` ⇒ 该档红。
- 🔴 **灭自证**：每一档配一条「**亮着 ⇒ 必须接管（+ 有副作用）**」——「恒返回 false」的病实现会让「藏起来不响应」全绿、这一条红。
- **三条前提怎么保证的**（简报逐条点名）：
  1. `_settingsPanel.Visible` / `_chatPopup.Visible` / `_logPanel.Visible` **每一态开始时先 `CloseAll964()`** 压成假
     —— 否则那三处 handler 的**第一句**「面板开着 ⇒ 无条件接管并 return true」会短路掉，把「被短路」看成「命中」。
  2. **夹具先换一局非教程局**：进本段时驱动上是一局**教程局**（上一句 `driver.BeginTutorial(0)`），
     而教程 `ApplyTutorialHudVisibility` **无条件藏墓园钮**、按 `hideChat` 藏聊天钮
     （判据 = 原版 `BattleManager__TutorialSetup.c:52-60`）⇒ 不换局「亮着 ⇒ 必须响应」那半会**全假红**。种子用 A462 段同一个 `20260915`。
  3. `offensive` 那一格先 `RuleCore.ChooseOffensiveCard(ctx, 0, c.idx, c.envSO)` 选一张真卡
     （否则 `OpenOffensiveCardWindow()` 因查不到卡也返回 false）。
- ⚠️ **如实降级一处**：`chat` 的「亮着」那一半**只看接管、不看副作用** —— `HandleChatPopup` 打开气泡前有一道
  **4 秒冷却**（`_chatCooldown`，原版 `CHAT_INTERACTABLE_COOLDOWN`），冷却**是生产状态**（A462 段说过一句台词就置上去了）
  ⇒ 拿「气泡有没有开」当「有没有响应」会**假红**。冷却那一档照样 `return true`，接管才是判据。TSV 里记了当时的冷却读数。

**`E2`（覆盖）** —— 按 §A964 续 ② 的裁定走**报告式**：只断「生产函数自己就必须成立」的两条
- **(a) 视觉中心 / 实绘内侧那一点必中**；(b) **明显在实绘之外那一点必不中**（配对 = 灭自证：恒 `true` 会让 (a) 绿、(b) 红）。
- 命中一律**调生产函数**（⛔ 探针不重算一份算式 = 断「自证」）。四条可量行：
  1. **重置镜头钮** —— `CameraResetButtonHit` vs `CameraResetButtonDrawnPx`；实绘 0.45 半宽必中、3 倍半宽必不中。
     ⛔ 判决**不是**「违」：`CameraResetHitPxW/H` = `rect(64.443×61.846) + pad8×2` = **80.443×77.846** ⊇ 实绘 61.846²。
  2. **设置面板 4 颗 + 三根音量滑块** —— `HitResign`（**硬写 px 300×90**）· `HitDifficulty` · `HitAutoZoom`（两块原版矩形）；
     滑块断「轨道中心 / 手柄中心 / 左右两沿**都算命中**」（判据只此一份 = `WfSlider.HitBand`）。
  3. **战斗日志每一行** —— `RowAt` vs `RowBg(i)` 那颗 quad 的真实矩形（中心 + 横向 0.45 半宽都判回该行）。
  4. **`ChatPopup` 6 颗** —— `ButtonAt` vs `ButtonRect(i)`（中心用 1920×1080 绝对矩形换算，与 A462 段点台词同一条）。
- **模型③**（无矩形那一族，裁定「只断视觉中心必中」）：**棋盘格位那一条已经在跑**（`TryResolveSlot ∘ DropTargetWorld`
  每格往返一致，`BattleScene.cs` 落点那一段）⇒ **不重复造**；本段补的是**攻击三选一**：
  `UpdatePointer(ButtonWorld(k)) == k` **+ 远端 +10 必不中**（灭自证）。
  🔴 顺手钉了**前提**「选择器根在世界原点」（侦察 §6·10）：`UpdatePointer` 拿 `localPosition` 与喂进来的点相减，
  而真鼠标那一路喂的是**世界**坐标 ⇒ 根一偏就静默错（`ButtonWorld` 回的是 `localPosition`，往返**看不出来**）。

---

### 四、白名单（含 `_settingsBtn` 那条隐患）

| 档 | 藏起来会不会接管 | 守卫在哪 |
|---|---|---|
| `chat` / `cemetery` / `offensive` / `cameraReset` | **不会**（断 `false`） | 四颗 handler 各有一句 `!…gameObject.activeSelf` |
| 🔴 **`settings`** | **会**（`HandleSettings` 第三句**没有** `activeSelf` 守卫，而且面板会当场开出来） | ⛔ **无** |

- `settings` 那一条写成**记账断言**（不是验收）：断言文案点明「生产不可达（出厂就亮着、全仓没有关它的路径）⇒
  是隐患、不是活缺陷；⛔ 别顺手补守卫」——与 `BattleDriver` 那段注释同一口径。
- 🔴 **另加一条「白名单台账」**：断「藏起来还会响应的钮**恰好只有 `settings` 一颗**」——
  多一颗 = 新漏网（真缺陷）；**少一颗 = 有人补了守卫**（好消息，白名单该更新）。这一条是防白名单漂移的牙。

---

### 五、覆盖率 · 类型检查 · 行尾 · 没查清 / 停手

**`E4` 覆盖率 = 5/5 档**（`settings` / `chat` / `cemetery` / `offensive` / `cameraReset`），两态全覆盖。
**`E2` 覆盖率（如实）** —— 侦察 §5·3·B 那张 12 行表里，本件**实建 5 行**，**未覆盖 7 行**：

| 未覆盖 | 为什么（⛔ 不是「不做」） |
|---|---|
| 手牌各张 · 场上单位 | `CardInteraction` 那一侧**没有公开的实绘几何读口**（`HitTest` private；`SimulateHover` 只回结果不回矩形） |
| 设置面板「关闭」那颗 | `SettingsPanel` **没有公开的中心读口**（`Resign/Difficulty/AutoZoom` 都有，就这颗没有） |
| 多卡窗「继续」 · 我方牌堆 · 技能面板 | 命中都是**硬写 px**，但**实绘几何没有公开读口**（`MultiCardDisplay.HitContinue` / `HitMyDeckPile` / `SkillPanel.Contains`）⇒ 无从比 |
| 结束回合 | `_endTurnBg/_endTurnLabel` 都是 **private 字段**，没有 `Hit` 口 |
| 日志面板「行内链接」 | 侦察自己标「恒等」；行那一档已覆盖 |

⇒ 上表**每一条都要补一个只读口**才能量（形状照 `CameraResetButtonDrawnPx`）——**建议另开一件**，
⛔ 本件不越白名单去改 `Battle/`。

**类型检查**：`TMPDIR=/tmp/wf_hp2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
**行尾**：`git diff --numstat` = `627/17`（本件 **+422/−0**，17 条删除是进本件之前就有的）；
二进制计数 `CRLF 18503 == LF 18503` ⇒ **纯 CRLF，没被翻**。⛔ 全程用 Edit、没用 `sed -i`。
**⚠️ 没跑 Unity**：本件所有「会不会绿」都是静态判定。**第一次真跑之前，「0 条违」不可信** ——
所以段里先排 `E4`（两条已知阳性判别式），再排 `E2`；`E4` 那两条一红就先别信 `E2`。

**顺便（只报告，⛔ 没改）**：侦察 §6 点名的那条「**六处 `ImageQuad.Contains` 的命中区该不该也过原版
`m_RaycastPadding`**」= **判不准、建议另立一件** —— 我如实复述：`Contains` 量的是**画出来多大**，
与「点哪儿算中」本是两回事；原版负 padding 是**外扩**（`RECON_A964前置.md` §0），套到 `Contains` 上会**把命中区放大到画框之外**，
而据什么放大、六处是否同一档，**判据一条都没有** ⇒ ⛔ 不动 `BattleDriver`，留给调度台另立。

**没查清 / 停手**：① 未覆盖那 7 行（见上表）；② `E2` 只跑**一态**（出厂态：`SmallScreenUI` 关 ⇒ `lossyScale == 1`），
**小屏缩放档没跑**（陷阱 7/1：`M ≠ 1` 时量法会分家）——如实记；③ `chat` 的副作用那一格降级（见三）。
