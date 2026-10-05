# H47 · `BattleScene.Run` 六条红 —— 修复（写手代理，2026-10-12）

> 输入 = `资料/普查产出_1012/D9_Battle六红诊断.md`（根因 + 最小改法，**照它落地，未重新诊断**）。
> 本件**没跑 Unity**（类型检查跑了，`0/0`）· 没动 git · 没改正本。
> ⚠️ **行号以本次现读为准**（三个文件今天被 B1/B2/H1/H8/H18/H20/H28 等改过，D9 给的行号已漂）。

---

## 一、结论

**六条红全部按 D9 的最小改法落地，三处改动都只动「我们自己的字面量 / 我们自己的自检口径」，
⛔ 没有放宽、没有删掉任何一条断言，⛔ 没有动任何原版判据与生产路径。**

| # | 红 | 改法 | 状态 |
|---|---|---|---|
| 1 | A423 那颗钮的位置 | `BattleDriver` 那四个 px 换成原版未取整值（+ 文案订正） | ✅ 期望值现在是精确的（算术见 §三·1） |
| 2 | A388 真卸载后钩子清零 | 按 `Application.isPlaying` 分档：Play 断 0；编辑模式**改断环境事实 + 出声** | ✅ 不再红，且**没有**变成恒真 |
| 3–6 | A431 四条 | `MakeAnimFx` 加「编辑模式是否模拟自毁」形参，**只有** `BuildSceneAnimFx` 传 `false` | ✅ 四条同一根因，一处修（算术见 §三·3） |

**⛔ 本件没有跑自检**（本轮规矩：攒到同步点跑）。**`BattleScene.Run` 是验收点**。

---

## 二、改动清单（`文件:行号`，每处一句为什么）

| # | 位置 | 改了什么 / 为什么 |
|---|---|---|
| 1 | `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs:7982`（+ `:7972-7981` 注释） | `HudAbs(..., 17.9f, 568.2f, 64.44f, 61.85f, ...)` ⇒ **`17.9293f, 568.174f, 64.443f, 61.846f`**。那四个是**原版未取整矩形**（同源 = 上面 `CameraResetRectPxW/H` 两个常量、`:2402` 注释、`m_RaycastPadding` 反推出的 48.443×45.846）—— 取整值让中心偏 0.0308/0.028 px > A423 自己的 1e-4 世界单位阈值 |
| 2 | `Editor/BattleScene.cs:10220-10224`（注释）+ `:10236-10241`（断言文案） | 把「画布 (50.1508, **480.903**)」订正为真值 **(50.1508, 599.097)**（y 从上数），并写清 `480.903 = 1080 − 599.097` 是**翻转后**的数、`ToWorld` 的 `y01` 收的正是它 ⇒ **代码里那句 `480.903f / 1080f` 一个字都不能改**（照标签去「修」代码会把方向改反） |
| 3 | `Editor/BattleScene.cs:10343-10354`（段落头注释） | 订正 W4 留下的错前提：「`OpenScene` ⇒ `OnDestroy` 跑过 ⇒ 计数早是 0 ⇒ 放它之后是恒真」——**错**：编辑模式不派 `OnDestroy`，放之后**会红**而不是恒真。**结论（放 `CheckSavedScene` 之前）不变**，理由换成「那之后 `driver` 已经没了、`driver.Begin` 做不了」 |
| 4 | `Editor/BattleScene.cs:10366-10388`（A388 (b) 段） | `Check(StaticHookCount == 0)` ⇒ **按 `Application.isPlaying` 分档**：Play 那一档照旧断 0；**编辑模式这一档改断「组件 `== null` ∧ 钩子原封不动」+ 一条 `Debug.LogWarning` 出声**（写明「验不了真卸载 ⇒ 归 `真Play待验清单` D39」）。两档各配一条 改坏法 |
| 5 | `Battle/ScenarioBlendables.cs:1661-1662`（签名）+ `:1649-1660`（`<param>` 新条目） | `MakeAnimFx` 加第 4 个形参 **`bool simulateSelfDestroyInEditor = true`**（默认值 = 老行为，`Create`/A341 一个字节不变） |
| 6 | `Battle/ScenarioBlendables.cs:1729-1742`（方法体那一跳） | `if (isPlaying) Destroy(go, t); else DestroyImmediate(go);` ⇒ `else if (simulateSelfDestroyInEditor) …; else { 出声 }`。**运行时那一档一个字没动**；编辑模式「只排不定」时打一条 `Debug.Log`（⛔ 不静默） |
| 7 | `Battle/ScenarioBlendables.cs:2083-2090`（调用点） | `MakeAnimFx(it, res, en != 0f && goActive, **false**)` —— 只有 `BuildSceneAnimFx` 传 `false`（它要的是「组件建出来 + 5 个子件改挂」，原版那一刻组件确实还在） |
| 8 | `Battle/ScenarioBlendables.cs:2001-2004`（`BuildSceneAnimFx` 头注）+ `:1976-1979`（`SceneAnimFxReparented` 文档） | 把「为什么传 `false`」「两档这个数都是 5」写进判据处，免得下次被改回去 |
| 9 | `Editor/BattleScene.cs:10410-10413`（A431 改坏法表） | 补第 ⑤ 条 改坏法 = **把那个 `false` 删掉/改回 `true` ⇒ 本段四条一起红** |
| 10 | `Editor/BattleScene.cs:10427-10432`（`wantReparen` 上方） | 照 D9 §三 记一条**提醒**：日后真跑 A418 那三步 ⇒ `battlearena2` 的 `reparent[]` **5 → 6**（多 `Embers`）⇒ `wantReparen[0]` / 断言文案 / 第 4 条点名的 5 个粒子名要一起改，否则 A431 会**因为另一个原因**再红 |

类型检查（`TMPDIR=/tmp/wf_h47 bash d:/4/Unity/工具/typecheck.sh`）：**运行时 0 · 编辑器 0**（跑了 2 次，含末次）。
⚠️ 期间**没有**出现「错在别人文件」那一档 ⇒ 无需归因。
行尾：三份 `.cs` 改完复核过（`BattleDriver.cs` / `BattleScene.cs` **纯 CRLF**、`ScenarioBlendables.cs` **纯 LF**，`bareLF = 0`）⇒ 没翻。

---

## 三、逐条：修的是什么 · 改坏法

### 1 · A423 那颗钮的位置（`:38780`）

**修的是**：实现里那四个 px 的**精度**（不是断言、不是阈值）。改后算术**逐位对上**判据：

| 量 | 期望（断言，原版直读） | 改后实现（`HudAbs`） |
|---|---|---|
| 中心 x（px） | 50.1508 | 17.9293 + 64.443/2 = **50.1508** |
| 中心 y（px，从上） | 599.097 | 568.174 + 61.846/2 = **599.097** |
| 画出来的 | 61.846×61.846（PA=1 内接） | h=61.846 ⇒ **61.846**（改前 61.85，也只差 0.004 px） |

**🧨 改坏法**（已写进断言文案）：把 `BuildHudExtras` 那四个 px 写成取整值（`17.9 / 568.2 / 64.44 / 61.85`）⇒ 中心偏 0.0308/0.028 px = 2.9e-4/2.6e-4 世界单位 ⇒ 本条红（= 2026-10-12 之前的状态）。
**⛔ 没做**：没放宽 `1e-4` 阈值（D9 §五·D 那条提醒照旧成立）；`48.443×45.846` 那两个命中区常量来自 `CameraResetRectPxW/H`，**不受这四个字面量影响**。

### 2 · A388 的 (b) 半（`:39055`）—— 断言前提错

**修的是**：**断言的**前提（不是实现）。落地后两档：

* `Application.isPlaying`（真 Play）⇒ `Check(现存 == 0)`，判据 = `OnDestroy` 第一句就是 `DetachStaticHooks()`；
* 编辑模式（批处理，今天的实况）⇒ `Check(driver == null && 现存 == hookedBeforeB)` + `LogWarning`。

**为什么编辑模式这半分**不是**恒真**：它断了**两件实读的事**（组件真没了、钩子原封不动）—— 「弱断言分不出两种状态 = 没断」这条规矩下它是合格的：把 `[ExecuteAlways]` 加到 `BattleDriver` 上它立刻就红。
**🧨 改坏法**：① 给 `BattleDriver` 加 `[ExecuteAlways]`（或让这一路真派 `OnDestroy`）⇒ 现存变 0 ⇒ 编辑模式那半红 —— 那是「环境变了、好消息」，把 Play 那一档提成无条件即可；② Play 档：`OnDestroy` 不摘/摘漏一条 ⇒ 现存 ≥ 1 ⇒ 红。
**⚠️ 仍未验的真路径**：离开战场场景 → 再进一次（走真卸载，不是 `DestroyImmediate`）—— 批处理够不着 ⇒ **`资料/真Play待验清单.md` D39**（已有那一行，本件**没有**改它：不在白名单）。

### 3–6 · A431 四条（`:39115` / `:39127` / `:39139` / `:39297`）—— 一条根因

**修的是**：`MakeAnimFx` 在**编辑模式**把刚建好的宿主当场 `DestroyImmediate` 这个**动作的可选性**。
改后 `battlearena2` 的链：`nodes[]` 3 个照建 → `MakeAnimFx(..., false)` **不删宿主** ⇒ `c != null` ⇒ 组件 1 个 → ③ 改挂 5 个 → `missed` 0。
四个数逐一落到期望：**组件 1 / 节点 3 / 改挂 5 / 没对上 0**（另两场 2/4/0/0 与 2/1/0/0 本来就没踩中 ⇒ 不受影响）。
**⚠️ 本件**没跑自检** ⇒ 「四条确实转绿」是**按 D9 的链条推的**（`c == null` 那一跳是 `continue` 的**唯一**入口），要由同步点上那次 `BattleScene.Run` 盖章。

**🧨 改坏法**（已写进 A431 段的 ⑤）：把这个 `false` 删掉/改成 `true` ⇒ 宿主当场被删 ⇒ 组件被误记成「建不出来」、③ 改挂被 `continue` 跳过 ⇒ **本段四条一起红**（= 2026-10-12 之前的状态）。
**⛔ 没做**：没动 `SelfDestroyScheduled++` 的位置（A341 的观测点语义不变：它数的是「**排定**过几次」，编辑模式「排而不销」也照 +1）；没动 `A341` 的合成探针（它走 `Create`、用默认 `true`）⇒ **A341 那两条断言一个字节没碰**。

---

## 四、真 Play 建议

| 建议 | 怎么验 | 归属 |
|---|---|---|
| **真卸载路径**（A388 的真那半） | 进战场 → 离开战场场景 → 再进一次（走真卸载，不是编辑器 `DestroyImmediate`）⇒ 看静态钩子有没有残留（`StaticHookCount`）、`CombatCameraZoom` 缩放状态残留 | **`资料/真Play待验清单.md` D39**（已有，本件没改） |
| **`RocketTrail` 6 秒后自毁的观感** | 真 Play 进 `battlearena2` ⇒ 6 秒后看那颗 `RocketTrail` 连同它底下 5 颗粒子一起没（**编辑模式这一档现在不模拟**，只有真 Play 走 `Destroy(go, 6s)`） | 可并入 `真Play待验清单` 的「表现手感」一类；本件只在代码注释里点明 |
| **那颗钮的位置/手感** | 滚轮动一下镜头 ⇒ 钮出现；点它 ⇒ 镜头回默认 | `真Play待验清单` 既有口径（`BattleDriver.cs:2436` 已指过去） |

---

## 五、没查清（⛔ 不猜）

1. **`OnDestroy` 在编辑模式是否「一律」不派** —— 本件**只读**：`D9` 的硬证据覆盖 `Awake/OnEnable/Update`，`OnDestroy` 属同一族但**没有单独实证**。**本次实测到的那一档**是「`DestroyImmediate(BattleDriver 组件)` 之后 `StaticHookCount` 仍是 17」（`battle.log:39055`）⇒ 我改的断言**只押这一档**（组件没了 ∧ 钩子没动），没有把「一切生命周期消息都不派」写成事实。D9 §二·2 那 5 行探针**没做**（本件不许跑 Unity）。
2. **A431 那四条转绿** = 按 D9 链条推的，**未跑**（本件不跑自检）。
3. **`ChatButton` 那四个字面量**（`50.9 / 880.2 / 64.44 / 61.85`）D9 列了「没查」—— 我也**没查**（属顺手发现，见下）。

---

## 六、顺手发现（⛔ 只报不改）

| # | 发现 | 出处 |
|---|---|---|
| 1 | 🟡 **`ChatButton` 也是同一批取整字面量**：`HudAbs(..., 50.9f, 880.2f, 64.44f, 61.85f, "ChatButton")` —— 中心 x = 83.12，而同族粗口径 `HudExtraPosPx` 那条写的是 83.15（差 0.03 px）。**今天不红**（那条容差 1.5 px），但和 A423 是**同一族隐患**：只要哪天有人给它加一条细口径断言就会红。要修得先读那件原版的 `sizeDelta / anchoredPosition`（`RectTransform` 那一份我**没查**）⇒ 留 A 表 | `Battle/BattleDriver.cs:7967` · `D9_Battle六红诊断.md` §四·3 |
| 2 | 🟡 **`BattleScene.cs:10443` 附近那句「原版那颗 `destroyTime = 6` 一销毁要连带这 5 个子件粒子」现在语义变了半格**（编辑模式不再当场销毁）—— 文案本身没错（说的是**原版/真 Play** 的行为），但读者可能误以为编辑器里它也会没。**已在我写的调用点注释里点明**，未改原句（它会牵动断言文案，留给下一次动 A431 的人一并收） | `Editor/BattleScene.cs` A431 段 |
| 3 | 🔴 **`BuildSceneAnimFx` ① 建节点是**无条件** `new GameObject`**（不查「是不是已经有了」）—— D9 §四·4 已记，本件**未动**（不在本件范围；清单重跑后若 prefab 里真出现这条链会**建重且静默**） | `Battle/ScenarioBlendables.cs` `BuildSceneAnimFx` ① |
| 4 | 🔴 **`MakeAnimFx` 那 3 处静默 `return null`**（`targets/res` 空 · 循环里没有 `kind == "animfx"` · `AddComponent` 返回 null）—— D9 §五·B 已记，本件**未动**；本次这条根因正是「被记成『建不出来（宿主/字段缺）』」那句话把 A418 那条错路指了出去 | `Battle/ScenarioBlendables.cs` `MakeAnimFx` |
| 5 | 🔴 **`driver.Begin()` 第二次只接回 17/19 条钩子**（差额 = `UnitTweenRuntime.HeroBySeat` / `SeatOf`，只在 `BuildHud` 里赋值、而 `BuildHud` 被 `_hudBuilt` 闩住）—— D9 §五·F 已记。本件**没动**（A388 (b) 现在就建立在这个 17 上；要修得另开一件、归调度台） | `D9_Battle六红诊断.md` §五·F |
