# DIAG · `BattleScene.Run` 一红（`…而且没有弹任何卡`，`BattleScene.cs:15378`）

**结论一句话**：不是产品缺陷 —— 是**夹具前提塌了**。本会话给 `RuleCore.Draw` 加的 `Emit(EvtKind.Draw)`
（`A985⑥`）让**同一个 `driver` 实例**在前一个子用例里**真的弹了一张卡**，而那张卡**从没被收掉**
（`HideLogCard` 只在产品那条「点压暗层关面板」的路上被调），于是这一条读到的 `LogHoverCardKey`
是**上一个子用例的残留**。

分类：**(δ) 夹具前提不成立** 为主 · 附带 **(α) 断言测的是全局残留状态、没设基线**；**不是 (β)**。

---

## 1 红的那条在断什么（写点 / 条件 / 全部写点）

**读的口子** —— `BattleDriver.LogHoverCardKey`（`Battle/BattleDriver.cs:5725`）：
```csharp
get { return (_logCard != null && _logCard.gameObject.activeSelf) ? _logCardKey : null; }
```
⚠️ 读的是 **`activeSelf`（本地位）不是 `activeInHierarchy`** —— 面板根关掉时它照样报「弹着」（见 §6·顺手发现①）。

**写 `_logCard` / `_logCardKey` 的全部写点**（全仓 grep，只有这 2 个方法）：

| 写点 | 条件 |
|---|---|
| `ShowLogCard(key, row=-1)`（`BattleDriver.cs:5754`）：`null ⇒ CardView.Create(_logPanel.transform, …, "LogHoverCard")` · `SetActive(true)` · `_logCardKey = key`（`:5787`） | **`FindCardByName(key) != null`**；取不到 ⇒ `LogWarning` + `HideLogCard()` + `return false`（`:5757-5762`）。**调用者只有两个**：`TickLogCard`（悬停，`：5739` —— 面板可见 + `LinkKeyAt(wp)` 非空）与 `TryClickLogRow`（点行，`：5886` —— 面板可见 + 点在行矩形内 + **闸门开着** + 那一行**有卡**） |
| `HideLogCard()`（`:5791`）：`SetActive(false)` + `_logCardKey = null` | 四个调用点：`TickLogCard` 面板不可见 / 指针不在链接上（`:5741`/`:5743`）· `ShowLogCard` 查不到卡（`:5760`）· `TryClickLogRow` 那一行**没提卡**（`:5881`）· **`HandleBattleLog` 点压暗层关面板（`:3782`，`_logPanel.Hide(); HideLogCard();`）** |

**卡的归属**：`_logCard` 是 **`_logPanel.transform` 的子节点**（`:5765`），而 `_logPanel`（`_root = panel.transform`，
`BattleLogPanel.cs:536-545`）**只在 `BuildHud()` 里建一次**（`BattleDriver.cs:11592` + `_hudBuilt` 闩 `:11199`，
`Begin()` 不重建）⇒ **卡与面板跨局、跨子用例同生共死**。

---

## 2 闸门（`hideCemetery`）关着时【应该】发生什么

**判据（原版死类，但字段语义链的最上游）**：`d:/2/tools/decomp_full/CemeteryLogManager__ClickCemeterySlider.c`
```
:75  cVar3 = BattleManager__CanShowCemetery(lVar6, 0);
:76  if (cVar3 == '\0') {
:77    return;                                  ← 整段的最前面
:93  *(uint *)(param_1 + 0x70) = local_res10[0];   ← 「记下选中行」= SelectedRow
:139 CemeteryLogGroup__DisplayAction(lVar6, &local_98, 0);   ← 行/卡的展示
```
⇒ **「不记 `SelectedRow`」与「不弹卡」是同一件事，不是两件**：两者**都在那一条 `return` 之后**，
不存在「弹卡发生在闸门之前」。闸门那一支是**纯 no-op** —— 它**不会**去收一张**早就弹着**的卡
（原版也遇不到这种态：闸门只读 `stage+0x29`，一个关卡内恒定）。我们的 `TryClickLogRow` 照此写：
`if (!CemeteryRowClickAllowed) { Debug.Log(...); return true; }`（`BattleDriver.cs:5868-5875`），
**`return` 之前不碰 `_logCard`** —— **这一处是照原版的，不是缺陷**。

⚠️ 但**发行版的活判据里根本没有这道闸**（`CemeteryManager__ClickCardLink.c` 没有 `CanShowCemetery`
调用；本会话 `G9` 已把 `CemeteryLogManager` 一族判为无实例的死类 ⇒ 见 `BattleLogPanel.cs:343-398`）——
这道闸是我们「按那个字段语义做的、没有原版活消费点」。**它今天不该被改动**（不是本条红的成因）。

---

## 3 本会话哪一处改动让它红（逐条验证）

**是 `A985⑥` 的 `EvtKind.Draw`，不是 `A991`。** 链子逐环可查（全部静态可复现，无需跑）：

1. **夹具是教程局**：`allowStage`（`stage=93, playerStarts=true, hideCemetery=false`，`BattleScene.cs:15337-15340`）
   ⇒ `tutorial != null`；`_vars` 走 `TutorialScenario` ⇒ **`showMulligan = false`**（`GameplayVariables.cs:337`），
   而批处理默认 **`mulliganEnabled = false`**（`BattleDriver.cs:3840`）⇒ `ctx.MulliganOpen = false`
   （`RuleCore.cs:275`：`openMulligan && ctx.Vars.showMulligan`）⇒ **`Begin()` 不早退**，走到
   `RuleCore.BeginTurn(Ctx)`（`BattleDriver.cs:2886`，「先手第 1 回合：能量 2、**抽 1**」）。
   ★ 反证：日志同上那段确实打了 `[Battle] 教程局：先手 = 玩家`，且**没有** `OpenMulligan` 那一支的痕迹。
2. `BeginTurn` 的抽牌 = `for (int i = 0; i < nDraw; i++) Draw(ctx, ctx.Active);`（`RuleCore.cs:837`，
   `nDraw = Vars.drawCardsPerTurn = 1`）⇒ 进 `Draw`。
3. `Draw` 尾部**本会话新增**：`if (!ctx.MulliganOpen) ctx.Emit(EvtKind.Draw, p, -1, card.Name);`
   （`RuleCore.cs:1211`，`git diff` 里 `+` 行）⇒ `ActionLog` 收 1 条（`BattleContext.cs:1344-1361`，
   `Emit` 无条件追加）。
4. `RefreshBattleLog`（`BattleDriver.cs:5671`）**没有 `case EvtKind.Draw`** ⇒ 落 `default:` ⇒
   `line = $"{who}「{card}」"`，而 `Entry.CardId = e.CardId`（非空）⇒ **`RowCardKey(0)` 非空**。
5. `hideCemetery=0` 那一格：`ClickLogRowForTest(0)`（`:15346`）⇒ 闸门放行 ⇒ `ShowLogCard(key, 0)`
   ⇒ **`_logCard.SetActive(true)` + `_logCardKey = key`**。
   ★ **日志实据**：`battle.log:56614` = `[Battle] 点日志第 1 行 ⇒ 弹这张卡…` 且**没有**
   `日志里那张卡「…」…找不到` 的告警 ⇒ `FindCardByName` 成功 ⇒ 卡真弹起来了。
6. `driver.BattleLog.Hide()`（`:15350`）是**面板自己的 `Hide()`**（`BattleLogPanel.cs:723`）——
   **它不收卡**；产品那条路是在 `HandleBattleLog` 里**成对**调的（`BattleDriver.cs:3782`），夹具抄了近路。
   之后的 `Begin()`/`ShowBattleLog()`（`:15357`/`:15359`）**也都不碰** `_logCard`（全仓 `_logPanel` 引用点已逐个核过）。
7. `hideCemetery=1` 那一格：闸门那一支**纯 no-op**（§2）⇒ 卡**仍旧弹着** ⇒
   `Check(driver.LogHoverCardKey == null, …)`（`:15378`）**读到上一个子用例的残留** ⇒ 红。

**改动前为什么绿**：`Draw` 不发事件 ⇒ `ActionLog` 空 ⇒ `RefreshBattleLog` 的 `_logEntries` 空 ⇒
`RowCardKey(0) == null`（`i >= _last.Count`）⇒ `TryClickLogRow` 走「这一行没提卡」那一支
⇒ **调了 `HideLogCard()`** ⇒ `LogHoverCardKey == null`。这正是那条断言的文案自己写的
「**这一局的日志是空的 ⇒ 不弹卡**」（`:15349`）—— **那个括号就是本条的隐藏前提**。

**排除 `A991`**：它的 diff 全在 `TutorialOverlay.cs`（删 `SkipPanel` 等四个转发口）与
`SettingsPanel.cs`（钮搬过去）—— 本段夹具**不建 overlay、不点跳过**，且 `A991` **不写 `ActionLog`、
不碰 `_logCard`、不碰 `CemeteryRowClickAllowed`**（`git diff` 逐文件看过，`TutorialScript.cs` 那 2 行
只是文档里的 `&lt;` 转义）。**与这条红无关**。

**同类残留本会话之前就踩过一次**：`SelectedRow` 侧同一形状的残留上一轮已用
`driver.BattleLog.SelectRow(-1)`（`:15368`）+ 一条前提断言（`:15369`）摆平
（见 `资料/普查产出_1018/WB_教程21红修.md` §1·5 R2）；**当时卡那一侧靠的正是「日志是空的」这层护盾**
⇒ 护盾今天被 `Draw` 事件拆掉，同一个坑在「卡」这一侧现形。

---

## 4 分类（α/β/γ/δ）+ 判据

**主：(δ) 夹具前提不成立。**
判据：断言自己的文案写着前提（「这一局的日志是空的」`BattleScene.cs:15349`），而这个前提**今天不成立**
（§3 第 3~5 环）；前提来自**别人文件里一次有意的引擎改动**，且那次改动**在文档里写明是故意发的**
（`BattleEvent.cs:130-135`「教程局的起手…照发」；`RuleEngineTest.cs:17888-17893` 也已为它改过口径）
⇒ 是**夹具的假设过期**，不是「引擎做错了」。

**次：(α) 断言测的是全局残留状态、没设基线。**
判据：`LogHoverCardKey` 是**跨子用例存活**的视图态（§1 的归属那一段），而这**一整块**（`:15335-15380`）
用的**是同一个 `driver` 实例、同一个面板**（`_hudBuilt` 闩）；先例是同一块的 `SelectRow` 侧**已经补过基线**
⇒ 本条缺的正是同一条基线。任何「让日志不再空」的合法改动都会再红一次 —— 这就是 (α) 的味道。

**(γ) 本批回归**：**只有「红是本批引进的」这一层成立**，**不足以定性** —— 它**不是**行为回归
（原版闸门那一支本来就不收卡；产品关面板那条路是配对的）。

**(β) 实现缺陷：不成立。** 三条判据：① 产品唯一关面板的入口 `BattleDriver.cs:3782` 是 `Hide()`+`HideLogCard()` 成对；
② 闸门那一支「不碰卡」**是照原版的**（§2，`ClickCemeterySlider.c:76-77` 的 `return` 是纯 no-op）；
③ 让日志变非空的那次改动**本身有文档、有自检口径**，不是误伤。

---

## 5 最小改法（2~3 个 + 理由 + 风险）

### 候选 1（推荐）· 在**上一个子用例的尾巴**把卡基线摆平，保留 `== null`
位置：`BattleScene.cs:15350` 之后（`driver.BattleLog.Hide();` 的下一行）加一句：
```csharp
driver.BattleLog.Hide();
driver.SimulateLogHover(Vector3.zero);   // 面板已关 ⇒ TickLogCard 第一句就 HideLogCard（BattleDriver.cs:5741）
```
再在 `:15368-15369` 那组前提旁边补一条对称的前提：
```csharp
Check(driver.LogHoverCardKey == null, "（前提）闸门这一档开始前「一张悬停卡都没弹」");
```
- **理由**：① 与同一块里 `SelectRow(-1)` 那条基线**同形同源**（上一轮 R2 的做法，用户/工程已接受这个先例）；
  ② 用的是**产品同一条路**（`TickLogCard` 在面板不可见时收卡 —— 正是产品语义：面板关了卡就不该在），
  **不加测试专用 API、不改产品行为**；③ 断言本身仍断 **`== null`（最强形式）**，不回退成「差量比较」。
- **风险**：极低，但**必须诚实标注**：这条断言**只覆盖「闸门关着 + 之前没弹过卡」这一种情况**
  （铁律 5·c）—— 「之前弹着卡再点」这一态原版分辨不出来（闸门一个关卡内恒定）。
  ⚠️ `SimulateLogHover(Vector3.zero)` 走的是 `!_logPanel.Visible` 那一支（**不靠 TMP 命中测试**）
  ⇒ 确定性好；**必须放在 `Hide()` 之后**（放到 `ShowBattleLog()` 之后就会依赖 TMP 几何，不确定）。

### 候选 2 · 加一个 `driver.HideLogCardForTest()`（产品类里加测试入口）
- **理由**：意图最直白，和既有 `ClickLogRowForTest` / `SimulateLogHover` / `CemeteryClickAllowedForTest`
  的写法一致；将来别的宿主碰到同一残留可以直接调。
- **风险**：往产品类加测试口（工程对「给产品类开后门」有明确保留，见 `BattleDriver.cs:5768-5773` 那条 A185 的裁定）；
  且**多一个可被误用的口**。**不如候选 1 干净**。

### 候选 3 · 改成「差量比较」（`LogHoverCardKey == 点之前那个值`）
- **理由**：一句话，不需要摆基线；语义上也说得通（「这一下没有改变任何东西」）。
- **风险**：**弱** —— 若闸门被改坏成「放行」，`ShowLogCard(0)` 会写**同一个 key** ⇒ 这一条仍绿；
  真正挡住它的是上一行 `SelectedRow == selBefore`（`:15372`）。
  也就是说它是**用断言换绿灯**、把鉴别力全押在邻居身上 ⇒ **只在候选 1 落不下去时用**。

### ⛔ 明确**不推荐**的两个
- **删掉这条断言**（`:15378`）：**不接受**。它断的是「闸门关着不弹卡」这半边语义，
  删了就只剩 `SelectedRow`（记账）而**没有**「不弹卡」；这正是「为了让断言变绿而改断言」。
- **让 `TryClickLogRow` 的闸门支去调 `HideLogCard()`**：**造相反语义** —— 原版那条 `return` 是纯 no-op
  （§2 判据），「拦下点击 = 顺手收掉一张早就弹着的卡」在原版不存在（那种态在原版不可达）。
  改了就变成**我们发明的行为**，而且会掩护真正的残留问题。

---

## 6 没查清的部分 / 顺手发现

1. **没跑 Unity**（红线）⇒ 「候选 1 落地后这条转不转绿、旁边那几条会不会受影响」**只有跑一次
   `BattleScene.Run` 才能定**。本报告只做了**静态时序**推演（每一环都给了 `文件:符号`）。
2. **顺手发现①（探针会撒谎，但**不是**本条红的成因）**：`LogHoverCardKey` 读 **`activeSelf`**（`BattleDriver.cs:5727`）
   —— `_logCard` 是面板根的子节点，**面板根关掉（`FinishSlideForTest` → `_root.SetActive(false)`，
   `BattleLogPanel.cs:742`）时 `activeSelf` 仍是 true** ⇒ 这个口会报「弹着」而玩家看不见。
   本条的现场里**面板是可见的**（`:15359` 刚 `ShowBattleLog()`）⇒ 与这条红无关；
   但它同族（「量一个看不见的东西」）。**是否改成 `activeInHierarchy` 请调度台裁**（另两条读它的断言
   `:2916`/`:2960` 都在面板可见时跑，改了对它们应无影响 —— 但我**没跑**，不下结论）。
3. **顺手发现②（本会话 `A985⑥` 的一个已生效的可见副作用，请调度台分流）**：
   `EvtKind.Draw` **不落空** —— `RefreshBattleLog` 的 `default:`（`BattleDriver.cs:5671-5672`）会把它印成
   「`回合 N　我方「卡名」`」（**没有动词**），而 `RefreshBattleLog` 头注释里那套「按原版 19 条词条重新分类」
   **还没做**（`BattleDriver.cs:5623-5637` 明写 `ActionYouDraw` 归在待办里）。
   ⇒ **今天每回合抽牌都会在战斗日志上多出一行读起来很怪的记录**，而原版那一行**只在抽到已布设的陷阱时才记**
   （`BattleEvent.cs:124-129`）。**这不是本条的成因**，是同一个改动的另一半后果，**该进待办**（判据已齐）。
4. 未查：`EvtKind.Draw` 在**教程起手**那一支（`TutorialScript.SetupInitialHand` → `RuleCore.Draw`，
   `TutorialScript.cs:1339`）**本夹具里到底发没发** —— `allowStage` 没设 `playerStartingTroopsInHand`
   ⇒ `SetupInitialHand` 首句 `list.Length == 0 ⇒ return 0`（`:1318`）⇒ **静态看是不发**，
   本条红的那条 `Draw` 来自 `BeginTurn` 的回合 1 抽牌。**没有实测**（不跑 Unity 就分不出这两支），
   但**两支都不影响结论**（都进 `ActionLog`、`RowCardKey(0)` 都非空）。
