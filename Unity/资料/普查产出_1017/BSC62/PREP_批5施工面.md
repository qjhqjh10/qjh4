# PREP · T09 批 5 施工面（第四十三会话）

> 只读提取。一手 = `S6S9_卡体与残件_施工方案.md` **§1·3·补**（= `S10` 2026-10-17 现核订正，**批 5 最权威**）。
> 二手指针 = `任务/T09_整层换uGUI.md` §五/§六/§七/§八（**批 5 的账不在 T09 里**）· `交接_第四十二会话.md` §三·1/§四。

---

## 一、批 5 是什么（a / b / c 逐条）

一句话：**收 `ReanchorHud` 那一轮里「已经翻成 uGUI 的件」的退出账**（`S10` ④）。三块**互相独立**，但 a/b/c **全落在同一个文件 `BattleDriver.cs`** ⇒ 只能串行。

| 块 | 一句话 | 改哪个函数 / 哪一段 | 改前 → 改后 |
|---|---|---|---|
| **a** | 删一个**死口**（自检读口） | 删 `BattleDriver.cs` 的 `HudLabelScaleForTest(int)`（**现读 `:16219-16223`**）＋ 顺带订正两处注释（`BattleDriver.cs:16179` · `Editor/BattleScene.cs:~1951`） | 迁移后这族字**不再乘任何系数** ⇒ 该口**恒返回 1**、**已无活调用者**（全仓命中只剩 3 处，全是定义/注释，见 §四）。删掉它 + 把「留着只为编得过」的注释改掉 |
| **b** | `PlaceDeckLights` 把**相对累加**改成**绝对赋值** | `BattleDriver.cs` 的 `PlaceDeckLights()`（**现读 `:15892-15901`**），照 `PlaceHandPlate()`（**现读 `:15907-15926`**，尤其 `:15924-15925`）的写法 | 现：`_myDeckLight.NudgeWorld(Px(83.46), Px(-20.2))` —— `HudPiece.NudgeWorld` 的 uGUI 支是 **`_rt.anchoredPosition += …`（增量）**（`HudPiece.cs:246`）⇒ 依赖「调用前有人先把它拉回锚点」。改后：**直接写 `Rect.anchoredPosition =`（绝对）**，像 `PlaceHandPlate` 那样自足 |
| **c** | 从 `ReanchorHud` 删掉**三条已迁件的重贴** | `BattleDriver.cs` 的 `ReanchorHud()`（**现读 `:16562-16610`**，删 `:16582-16584` 那轮循环 · `:16590-16591` 两句 `FitDeckPile` · `:16596` 一句 `PlaceHandPlate`） | 删：① `for _hudImages… SetAnchorPosition` 那轮（**= 复位循环**）② `FitDeckPile(_myPile,…)` + `FitDeckPile(_foePile,…)` 两**调用** ③ `PlaceHandPlate()` 调用。**登记点可留**（`_hudImages`/`_hudImageSpots` 是「画布件锚点册」，语义仍成立） |

- 🔴 **`S10` 原表把 **b 行** 标成 `:15892-15901`、**c 行** 标成 `:16582-16596`**（**那是 2026-10-17 时点**；`BattleDriver.cs` 是纯 CRLF、行号漂得快，⚠️ **引它一律「搜哪一串」**：`void PlaceDeckLights()` · `void PlaceHandPlate()` · `void ReanchorHud()` · `for (int i = 0; i < _hudImages.Count; i++)`）。
- 三块**都不新建文件、不改签名、不加断言**（`S10` 未给新断言；`c` 删的调用对应的行为由既有断言覆盖）。

---

## 二、b 为什么必须先落 · c 为什么硬依赖 b

**机制（不是「因为 S10 这么说」）：**

1. **复位循环 + 增量平移是一对**：`ReanchorHud` 里那轮 `for _hudImages… SetAnchorPosition(x01,y01)` 的作用 = **把每个画布件拉回它的归一化锚点**（`HudPiece.SetAnchorPosition` 体内 = `anchorMin=anchorMax=(x01,y01)` **且 `anchoredPosition = Vector2.zero`**，`HudPiece.cs:307-313`）—— 它是**绝对复位**。
2. **`PlaceDeckLights` 是相对累加**：它靠 `NudgeWorld`，而 `NudgeWorld` 的 uGUI 支 = `_rt.anchoredPosition += …`（`HudPiece.cs:243-248`）。**它自己不回基准** ⇒ **必须**由上面那轮循环先复位，它再加那 `(83.46, −20.2)` 设计 px 的偏移。代码里那两处注释也白纸黑字写着这层依赖（`BattleDriver.cs:15889-15890`「换分辨率时会把灯上的偏移抹掉，所以那边也要再调一次」· `:16593`「上面那一轮会把它拉回中心」）。
3. **⇒ c 删掉那轮复位循环**：`PlaceDeckLights` 就**失去了「回基准」这一步**。此后 **`ReanchorHud` 每跑一次（每次换分辨率）= 在上一帧的位置上再 `+=` 一次** ⇒ 回合灯**一次偏一点、静默累加**（**16:9 看不出来**，只有换分辨率才现形）。
4. **⇒ b 先落**：把 `PlaceDeckLights` 改成**绝对赋值**后，它**不依赖任何复位**（调用几次都一样，幂等）；**这时** c 删那轮循环才是安全的。
5. ✅ **这不是纸上推演 —— 已有实测**：`交接_第四十二会话.md:154/170` 记着一条红：**牌堆回合灯 `x01 0.901 / y01 0.106`，而断言期望（原版）`0.94451 / 0.08778`**（`Editor/BattleScene.cs:3589`），该行初判就直接点名「**正是 `S10` 警告过的 `PlaceDeckLights` 的 `+=` 累加**」。

---

## 三、文件所有权表

| 文件 | 本批改什么 | 改动量（估） | ⛔ 与别的活的交叠 |
|---|---|---|---|
| **`Battle/BattleDriver.cs`** | a（删 1 方法 + 1 注释）· b（1 方法体，2 行）· c（删 1 循环 + 3 句调用） | **小**（≤ 20 行净） | 🔴 **三个瓶颈都指向它**：**批 4（d/e）也改它** ⇒ **同一时刻只能一个写手**；⛔ **批 5 与批 4 必须串行**（`S10` 明写 d/e「**再一轮**（与 c 撞 `BattleDriver.cs`）」） |
| **`Editor/BattleScene.cs`** | a（**一句注释**，`~:1951` 那块） | 极小 | 🔴 **第二瓶颈**（全批断言侧 + 块 3/4 都要它）；批 4 的 `Editor/` 断言侧也在它里 |
| `Battle/HudPiece.cs` | **不动** | 0 | ⚠️ 但 `NudgeWorld` 的 `+=` **就是 b 的病根**（`HudPiece.cs:246`）—— ⛔ **本批只改调用点，⛔ 别去改 `HudPiece`**（`W76`「写死 108」是**另一笔独立的账**，且是**跨件共用件**，动它会波及非战斗侧） |
| `Battle/HudCanvas.cs` | **不动** | 0 | 主对话扣着 |
| 文档/账（f：记 `W76` 与 `_hudSpots` 已删） | 纯文档 | — | ✅ **本批唯一可全程并行的**（`S10` ④ 表） |

**⇒ 结论**：a/b/c **是一件活用三个提交点，不是三件能并行的活** —— 派一个写手、按 a→b→c 顺序做完，**别开三个代理**（会撞同一个文件、后写的静默盖前面的）。

---

## 四、逐条判据（带出处；没查到的写「没查到 + 搜过哪些词」）

**a · 死口判据**
- 该口现读 `BattleDriver.cs:16219-16223`，它自己的 doc（`:16212-16218`）已写着「§五 第 5 步后就**恒为 1** · **这个口的原意没了** · **留着只为编得过**」。
- **现核「有无活调用者」**（`grep -rn "HudLabelScaleForTest" MyGame/Assets`）⇒ **全仓 3 处命中，零活调用**：`BattleDriver.cs:16179`（注释）· `BattleDriver.cs:16219`（**定义**）· `Editor/BattleScene.cs:1951`（**注释**，且该注释本身说旧断言「已删」）。⇒ **静默可删**。
- ⚠️ **一处需顺带订正**：`BattleDriver.cs:16215-16216` 的注释写「`Editor/BattleScene.cs:1908` 还在调它」—— **该行号与那句都过期**（现核无调用）⇒ 删方法时**把这句一起改**（铁律 5）。

**b · 改法与判据**
- **照抄对象** = `PlaceHandPlate`（`BattleDriver.cs:15907-15926`）：它同批已改成**绝对**写 `_handPlate.Rect.anchoredPosition = new Vector2(_handLabel.WorldW*0.5f, _handLabel.WorldH*0.5f)`（`:15924-15925`），**不依赖复位**。
- **待改的现状** = `:15899-15900` 的 `_myDeckLight.NudgeWorld(Px(MyLightDxPx), Px(MyLightDyPx))`，常量 `MyLightDxPx=83.46 / MyLightDyPx=-20.2`（`:1376-1377`）。
- **病根出处** = `HudPiece.NudgeWorld` uGUI 支 `_rt.anchoredPosition += …`（`HudPiece.cs:243-248`）。
- **原版值（判据）** = 回合灯 `x01 0.94451 / y01 0.08778`（= 锚点矩形 + `anchoredPosition`），在 `Editor/BattleScene.cs:3586-3590`（含 `lp.x`/`lp.y` 读 `drv.MyDeckLightPos01`）。**我们现值 = 0.901 / 0.106**（`交接_第四十二会话.md:170`）。
- ⚠️ **`MyDeckLightPos01` 读回口在哪 / 它走哪一档**：本报告**没现核**（超出批 5 施工面，属 d/e 的读回口那笔）—— ⛔ **不猜**。

**c · 删除安全性判据**
- `FitDeckPile` **自身幂等**（`BattleDriver.cs:16429-16430` 的 doc：「每次先 `SetAnchorPosition(x01,y01)`（= 回到基准，⛔ 不要在现位置上累加）… `ReanchorHud` 每切一次分辨率就会调一次」）⇒ 删 `ReanchorHud` 里那两句**调用**只是「少调一次幂等函数」，**不改变几何**。
- `PlaceHandPlate` 同 b（已是绝对）⇒ 删调用不改几何。
- 那轮**复位循环**是 **b 的承重前提**（见 §二）。
- 出处 = `S10` §1·3·补 ③④（`S6S9_…md:56-58 / 60-66`）。

**查不到的（⛔ 不猜）**：`S10` 自己列的两条 —— ① `S6` 那组换算口计数**复现不出**（`ToWorld` 报 72，现核总命中行才 61）②「12 个 `RefreshLayout`」**查不到**（全仓 `RefreshLayout()` **定义只有 5 个**）。⇒ **一律以现核为准**（`S6S9_…md:71-72`）。

---

## 五、批 5 之后 T09 还剩什么

（权威 = `交接_第四十二会话.md:51` 那张「下一步」表）

| 剩什么 | 状态 / 阻塞点 | 出处 |
|---|---|---|
| **批 4（= d/e）** | 各面板**读回口** + `Editor/`**断言侧**：世界→px 换原生读。**阻塞 = 与 c 撞 `BattleDriver.cs`** ⇒ **必须排在批 5 之后**（同文件、串行） | `S6S9_…md:66` · 交接 `:51` |
| **卡体②**（第二条物理面 `Collider2D`+`Physics2D`） | ✅ **已裁定「先记账」**（不急着建）。判据：原版确有此面，但 `GetTargetInPos` 已证**不是点击链**（是瞄准/悬停射线） | `S6S9_…md:16` · 交接 `:51` |
| **卡体③**（卡面是否改 uGUI WorldSpace Canvas） | **重写级大账** —— 我们卡面现为**自建 `Mesh`+`MeshRenderer`+TMP 子件**（`CardView.cs:210/2768`） | `S6S9_…md:24` · T09 头 `:29` |
| **卡体①**（命中面对齐） | 与②/③同族（`Core/CardView.cs`，块 1/2 内部串行）；**卡体整批 = 第 5/6 步** | `S6S9_…md:79-80/82` |
| **`ReanchorHud` 剩下的** | **5 件世界档必须留**（`EnsureMarkers`×2 · `backdrop` · `_replayBar` · 3D 棋盘那半）＋ **2 件画布档按既有裁定留**（`selector`/`skillPanel`/`_unitChat`） ⇒ **`ReanchorHud` 本批后仍不能整拆** | `S6S9_…md:56-58` |
| **步 8 统一验收** | **三档宽高比截图并排 + `wf_click.py` 真点**（⛔ 不能只跑断言收口）。⚠️ **前提未解**：`Editor/BattleScene.cs` 把 `cam.aspect` 钉死在 `DesignAspect`（现读行号**漂过、别照抄**，搜 `cam.aspect`）⇒「16:9 是断言唯一覆盖的档」 | T09 §五 `:98-106` / `:236-249` |
| 另（非本批，已在别处收口） | `OverHand` 5 扇 + `AlliancePanel`/`SkillPanel`/`UnitChat`/`AttackSelector` 的裁定与落地（A1/A2/A3/A9 + G 波）**都不算批 5 的事** | T09 §八 · 任务总表 `:20` |

---

## 六、⛔ 别顺手改的（邻接但属别的批）

1. 🔴 **`ReanchorHud` 的「5 件世界档」**（`EnsureMarkers`×2 · `backdrop` · `_replayBar` · 3D 棋盘那半）—— **本批一件都不许删**（`S6S9_…md:56-58`）。
2. 🔴 **`ReanchorHud` 的「2 件画布档但按裁定留」**（现读 = `selector.RefreshLayout()` `:16603` · `skillPanel.RefreshLayout()` `:16605` · `_unitChat.RefreshLayout()` `:16609`）—— **按既有裁定留**（`selector` 是画布档、帧位置从世界锚点算，换分辨率必须重排；`_unitChat` 幂等）。⛔ **别当成「已迁就跑不掉」删掉**。
3. 🔴 **`_hudLabels` 本体 + 3 个只读口**（`HudLabelCount==21` 的逐颗断言 + `HudLabelOnCanvasForTest` / `HudLabelAnchorForTest` 在用）—— **留**。**只**删 `HudLabelScaleForTest`（a）。出处 `BattleDriver.cs:16175-16179` · `S6S9_…md:46-47`。
4. 🔴 **`HudPiece.cs` 的 `W76`（`NudgeWorld`/`SetWorldHeight` 写死 `×108f`）** —— **跨件共用件**（`HudPiece.cs:158/177`），⛔ **本批别碰**；它是**独立的一笔账**（且本批 6 扇/3 调用点没踩）。
5. 🔴 **批 4（d/e）的面板读回口 + `Editor/` 断言侧** —— **与 c 撞 `BattleDriver.cs`** ⇒ **本批不许顺手动**（`S6S9_…md:66`）。
6. ⚠️ **`_hudImages` / `_hudImageSpots` 登记点** —— `S10` 明写 c 只删那一轮循环、**登记点可留**（语义已变「画布件锚点册」）⇒ ⛔ **别顺手把两个 List 也删了**（`S6S9_…md:35 · :65`）。
7. ⚠️ **批 3 的「队列 → 兄弟序取负」逐扇核**（`W53`，收口动作）· `PopUps` 组三块画布兄弟序 · `EndPanel/BattleDoors` 父子颠倒 —— **都是别的账**，⛔ 别混进批 5。

---

## 七、`S8`（`TransformScalerBySmallScreenUI`）在 T09 里的命中情况

- 🔴 **`T09` 里 0 命中** —— 搜过并确认无（`grep -n "TransformScaler|SmallScreen|小屏|1\.12|1\.30"` 于 `任务/T09_整层换uGUI.md` ⇒ **空**）。T09 全文**不提**它。
- **它在哪**（不是 T09 里，所以「0 命中」属实）：`资料/普查产出_1016并行/EW3_ChatPopup淡入修_报告.md` **§⑥·1**（`:164-173`）—— 原版 `ChatPopup` 根上挂着它（`menuScale = 1.2999999523162842`）；`battlearena2` **全场景 10 个节点**挂它，`menuScale = 1.12 · 1.20 · 1.25×5 · 1.30×3`。行为（`decomp_full/TransformScalerBySmallScreenUI__Initialize.c` / `__LateUpdate.c` 现读）：`Initialize` 只在「`MenuScale != 1f` **且某静态开关为真**」时 `enabled=true`，`LateUpdate` 把 `transform.localScale` **乘上 `menuScale`**（带防抖缓存）。
- **`S8` 本身**：⏳ **未回**（跑得最久、按「可能卡死」预留处置）· 台账 `_收口台账_第四十二会话.md:645`。
- **会不会影响批 5？** ⚠️ **不影响** —— a/b/c **全在 `BattleDriver.cs` 内的 `PlaceDeckLights` / `ReanchorHud` / 一个死口**，与「节点上的一个小屏缩放组件」**没有交叠**。批 5 **不需要**这条判据。
- **会不会影响步 8 的验收口径？** 🔴 **可能，但未查清 —— ⛔ 不猜**。`EW3` 自己写着「**我没查清**：那个静态开关到底是什么条件（小屏阈值？画质档？），以及**我们的非 16:9 路径是不是已经把它等效掉了**（`T09` 正在做的锚点/子画布迁移有可能覆盖了同一件事）」。`交接_第四十二会话.md:90`（§三·12）明写：**「这条可能影响 `T09` 的验收口径（若它就是原版处理非 16:9 的手段）⇒ 新会话要么重派一个更窄的简报，要么先按『未查清』记着」**。
- ⇒ **给主对话的建议（本报告只报不改）**：批 5 **照做**（不受它阻塞）；**步 8 统一验收前**，要么**重派一个窄简报**查清「那个静态开关的条件 + 它是不是原版非-16:9 的手段」，要么在步 8 里**如实标「未查清」**（铁律 2：没查到就说没查到）。

---

> 本报告由只读提取代理写；⛔ 未跑 Unity、未跑 typecheck、未动 git、未改任何产品代码/文档（除本文件）。
