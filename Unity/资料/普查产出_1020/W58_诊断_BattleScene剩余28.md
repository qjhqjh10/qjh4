# W58-DIAG-BT2 · `BattleScene.Run` 第四轮剩余 28 条失败 · 逐条根因

> 代理 `W58-DIAG-BT2`（只读）。输入 = `_tmp_view/battle4.log`（2026-10-20 22:35）的 **28 条 `BT    ✗`**。
> ⛔ 未跑 Unity / 未跑 `*.Run` / 未改任何源码；本文所有结论 = 日志 + 源码 + 本地 uGUI 包源 交叉核对。
> 分类口径：**α** 断言过期（期望值写的不是现在的判据）· **β** 本批缺陷 · **γ** 既有回归（HEAD 前后代码/数据已如此）·
> **δ** 夹具前提不成立（生产本身没坏，是测试摆的局面/环境不对）。

---

## 〇、总表（28/28 全部覆盖）

| # | 日志行 | 断言 | 分类 | 一句话根因 | 优先级 |
|---|---|---|---|---|---|
| 1 | 96657 | T09-A1③ | δ（测试环境） | 出厂关着的钮在测试里才点亮；批处理下「激活」不给 `Graphic.depth`，`GraphicRaycaster` 跳过它 | **P0（一条修法清 5 条）** |
| 2 | 96820 | T09-A1⑤ | δ（同上） | 同上（`Contains=True / Raycast=False` 是同一件事的两面） | P0 |
| 3 | 97560 | T09-A2[cameraReset]⑤ | δ（同上） | cameraReset 也是「测试里才点亮」的两颗之一 | P0 |
| 4 | 97001 | T09-A2[settings]⑤ | α | 「上边内 1 px」那个点被 pad 外扩推到 **y=486.93 > Screen 480** ⇒ 被 `GraphicRaycaster` 的视口闸整条丢弃 | P1 |
| 5 | 97937 | T09-A2[endTurn]⑤ | α | 「右边内 1 px」那个点 x=646.26 > Screen 640 ⇒ 同上 | P1 |
| 6 | 92524 | A985③ 前提（空白点） | δ（同 #1 类） | Alliance 面板是子画布；它打开后从未被真正渲染过 ⇒ 面板里的 `Graphic` 全 `depth==-1` ⇒ 射线一条都不返回 | P0（与 #1 同修） |
| 7 | 92550 | A985③ 前提（挡射线的字） | δ（同上） | 同上（扫「字」也一条都不中） | P0 |
| 8 | 63627 | T16 挪出命中区 | δ | 取边界点**在 `SimulateHover` 之前**；hover 把卡放大 **1.3×** 并抬高 30 px ⇒ `outB` 仍在框内 | P1 |
| 9 | 63640 | T16 卡外松手 | δ | 同上（锁存没清 ⇒ 照样转发点击） | P1 |
| 10 | 91431 | T03 手牌 y 边界 | α | 二分助手 `edge03` **只回 x**（`ix=ox=0` ⇒ 恒 0）；上次只修了换牌那一份 `edgeM03` | P1（改一行） |
| 11 | 91595 | T03 场上 y 边界 | α | 同上，`edgeU03` 也是只回 x | P1（改一行） |
| 12 | 98059 | T09-D① | α | `d1Hits[0]` 是 `"ErrorMessageBanner.cs:973"`，拿它跟 `"ErrorMessageBanner.cs"` 比 ⇒ 恒不相等 | P1（改一行） |
| 13 | 95010 | B2·偏差8 夹具 | δ | `driver.Begin()` 后**玩家先手**，敌方根本没轮到 ⇒ 敌方棋盘**必然为空** | P1 |
| 14 | 95178 | B3·偏差9 夹具 | δ | 同上（B3 复用同一局） | P1 |
| 15 | 99587 | E（前提）Ork Nob 在 2 号格 | δ | 连续无洞模型：空盘请求 2 号格会被**夹到贴督军那格 = 3 号**；夹具还在读 `Board[2]` | P1 |
| 16 | 99639 | F（前提）Gun Drone 在 2 号格 | δ | 同上 | P1 |
| 17 | 99950 | W58④ 找得到 driver | α | 整段插在 `DestroyImmediate(driver)`（`BattleScene.cs:30101`）**之后** ⇒ `driver` 恒 Unity-null | P1 |
| 18 | 48021 | 精确语音 ≠ 兜底（灭自证） | γ | 精确表与兜底表已被数据重生成对齐 ⇒ 见证者归零（HEAD 已如此） | P2 |
| 19 | 51758 | 飘字在挨打那张卡上 | α | B4 给**敌方**飘字加了 `EnemyCardLocalToOurs/CardLocalToOurs = 0.9071` 的偏移缩放，断言没跟上 ⇒ 差值 **0.0532 恰好 = 0.0929 × |PopOffset|** | P1 |
| 20 | 51771 | 飘字不在 2D 行线上 | α | 同 #19 的连带（两个靶都少乘同一个因子） | P1 |
| 21 | 81807 | 手牌数渲染高 ≤64 | α/γ | 框 64 px 比我们字体的**最小行盒**还矮（min 50 px × 行盒 1.437 ≈ 72 px）⇒ 结构上装不下 | P2 |
| 22 | 82366 | 提示正文 = SO 的 `arg` | α | 判据写于「本地没有 I2 表」之前；官方本地化 2026-10-18 已并入 ⇒ 正文现在走 `Loc.T`（中文） | P1 |
| 23 | 87835 | 手牌层没被冻 | δ | `FrozenNow = _frozenForTest` **或** `Frozen()`，而 `Frozen = PlayerInputFrozen` **或** `WaitingForBlockingAction`；`Begin()` 后闸可能还没排空 | P1 |
| 24 | 87887 | 解开 ⇒ 两处同时恢复 | δ | 同上（`PlayerInputFrozen=false` 但 `FrozenNow` 仍真） | P1 |
| 25 | 88789 | G-2 点单位上不算跳过 | δ | 跳过被判成「收下」⇒ 说明 `TutorialClickHitsSomething(敌方督军位置)` 返回 false（最可能：那颗视图此刻不在激活层级，2D collider 随对象停用） | P1（先加读数） |
| 26 | 88802 | G-2 点空处置 skip | δ（连带） | 上一拍已经把窗口收掉 ⇒ `!_tutWaitArmed` ⇒ 这里必然 false | P1 |
| 27 | 71037 | 主机视角两边视图都在 | γ | 生产侧 `SetMySeat` 只写 `_me`；**翻回去**时另一侧的 `DeployPending` 账没清、`MyUnits[1]` 没重建（本轮诊断 §B 已记的同一族） | P2（生产） |
| 28 | 71320 | 主机视角槽 1 是我的兵 | γ | 同上（`MyUnits[1]==null` 的直接后果） | P2 |

> **一句话总览**：真正「本批（工作区）引入」的只有 **#19/#20**（B4 的敌人飘字缩放没同步到断言）；
> 其余是 **测试环境/夹具**（δ，13 条）· **断言过期**（α，9 条）· **既有**（γ，5 条）。
> **没有任何一条指向本批新加的生产代码在真机 Play 下会坏**（#27 那条生产限制是既有的、且 `_me` 只在开局设置一次）。

---

## 一、T09 射线簇（7 条）——**先给结论：不是「屏点少乘了缩放」**

### 1·0 先把批处理的几何钉死（下面 7 条共用）

- `Screen = 640×480`（日志 `99121`：`width=640 / height=480`）·HUD 相机 `aspect` **钉死 16:9**（`BattleScene.cs:32613`；
  日志 `47205` `[投影诊断] 相机 aspect=1.7778 pixel=640x480`）。
- 根画布 `RootRect.rect = 1920×1080`（日志 `96657` 的「画布 1920.0×1080.0」），但它被**非等比**铺满 640×480：
  - **x：1 设计 px → 1/3 屏 px**
    （settings 命中框宽 113.904 px ⇒ 屏幕左右各 19.32 px = 56.952×0.3334）；
  - **y：1 设计 px → 4/9 屏 px**
    （settings 上内 1 px 在中心上方 63.142 设计 px ⇒ 屏幕差 28.06 px = 63.142×0.4444）。
- ⇒ **测试用的 `RectTransformUtility.WorldToScreenPoint` 本身没错**（`BattleScene.cs:29657/29809`），
  它跟画布的实际映射逐位一致。**「少乘缩放」的假设不成立**，不要按那个方向改。
- 真正的两个闸，一个在 uGUI 里、一个在批处理生命周期里：

```csharp
// Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/GraphicRaycaster.cs
:175  // If it's outside the camera's viewport, do nothing
:176  if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f) return;   // ← 屏外点整条丢弃

:324  if (!graphic.raycastTarget || graphic.canvasRenderer.cull || graphic.depth == -1) continue;
```

### 1·1 **五条（#1 #2 #3 #6 #7）：测试里才「点亮」的 uGUI 件在批处理下不可射线**

- **分类**：**δ（测试环境/夹具时序）**，不是生产缺陷。
- **根因链**：
  1. 两颗钮**出厂就是关着的**：`cameraReset`（`BattleDriver.cs:20149`）与 `offensive`（`BattleDriver.cs:3363` / `6131`）。
  2. 测试在打射线**前一刻**才 `SetHudButtonActiveForTest(..., true)`（`BattleScene.cs:29638` / `29791`），
     然后只补了 `SyncRaycasters()` + `Canvas.ForceUpdateCanvases()`（`:29640/29793`）。
  3. **批处理（非 Play）下 `SetActive(true)` 不派 `OnEnable`** —— 这不是猜测：同一轮日志 **`battle4.log:929`**
     直接打出了 `[BattleEventSystem] 编辑模式：激活那一刻没把本实例登记成 EventSystem.current ⇒ 已手工补登记`，
     那正是 `BattleEventSystem` 为这条环境事实写的兜底（`BattleEventSystem.cs:341-354`）。
  4. ⇒ 这两颗 `Image` 在 `GraphicRaycaster` 眼里要么不在射线表里、要么 `depth` 还是 **−1**（`Graphic.cs:393`
     `depth => canvasRenderer.absoluteDepth`，它由**真实渲染**的批处理赋值；`Canvas.ForceUpdateCanvases()`
     只跑 `CanvasUpdateRegistry` 的几何重建，**不给绝对深度**）。
  5. **同族反证**：同一次 A2 的另外四颗（settings/chat/cemetery/alliance）**全部绿** —— 差别只有一个：
     它们出厂就是亮的，**被 `Shot(cam, "01_开局")`（`BattleScene.cs:745`）真正渲染过**；`Shot` 里 `board3D.Render()`
     会把 HUD 相机（Overlay）一起渲进 RT（`BattleScene.cs:33099-33110`）。两颗测试里才亮的件**从没进过任何一次渲染**。
  6. A1⑤（#2）`Contains=True / Raycast=False` 是同一件事的另一面（`Contains` 读 `GetWorldCorners`，与射线表无关）。
- **A985③（#6 #7）** 是**同一类**：`Alliance Panel Canvas` 是**子画布**（日志 `92277`），面板在测试里才 `Open()`，
  之后**没有任何一次 `Shot`**；扫描 36 个点**一条命中都没有**（不是「被别的件盖住」——那会 `Count>0` 且首中不是压暗层）。
  射线器本身已由 `SyncRaycasters()` 补登记（`BattleScene.cs:27889`），所以最大嫌疑仍是 **子画布里所有 `Graphic` 的 `depth == -1`**。
- **修法（测试侧）**：
  1. **点亮之后先真渲一次**，再打射线。最省事：在这两段开头复用现成的 `Shot(cam, "…")`（它已处理 RT/两相机），
     或写一个 1×1 `RenderTexture` + `boardCam.Render()` 的小助手。
  2. **把环境事实钉成前提**，别让它再以「0 条命中」的形式出现：
     ```csharp
     var img = piece.gameObject.GetComponent<Image>();
     Check(img != null && img.depth != -1 && !img.canvasRenderer.cull,
           $"（前提）该件已被画布处理过：depth={img?.depth} cull={img?.canvasRenderer.cull}");
     ```
  3. A985③ 同样在 `panel985.Open()` 之后、扫描之前渲一次；并把「面板子画布里的 `Graphic` 有没有 `depth`」打进失败文案。
- **⚠️ 若加完渲染仍不中**，那就换成另一条假设（**子画布的 `GraphicRaycaster.eventCamera` 解析**）：
  `EnsureSubCanvas`（`HudCanvas.cs:529-543`）没有设置 `renderMode/worldCamera`；若本机 Unity 对**嵌套 Canvas**
  的 `canvas.renderMode` 返回 `Overlay`（而不是根画布的 `ScreenSpaceCamera`），`GraphicRaycaster.eventCamera`
  会退成 **null**（`GraphicRaycaster.cs:299-309`），于是把**屏幕**坐标当**世界**坐标比矩形 ⇒ 子画布上一件都打不中。
  这一条**必须先探一次**（`canvas.rootCanvas.renderMode / rootCanvas.worldCamera / renderMode` 打出来）再决定，
  因为若成立它是**生产缺陷**（所有面板在真机上点不动），不是夹具问题。
- **证据**：日志 `96657/96820/97560/92524/92550/929`；`BattleScene.cs:29638/29641/29791/29793/745/33099`；
  `BattleDriver.cs:20149/3363`；`GraphicRaycaster.cs:175/324`；`Graphic.cs:393`；`HudCanvas.cs:529`。

### 1·2 **两条（#4 #5）：pad 外扩把「内 1 px」点推出了屏幕**

- **分类**：**α（断言/取点方式过期）**。
- **根因**：`T09-A2[which]⑤` 的五个点里有两个是「命中框边 + 向内 1 设计 px」（`BattleScene.cs:29877-29881`）。
  而命中框 = 节点 rect **按负 padding 外扩**（原版值就是这个），已被推到屏幕边缘之外：
  - settings 上内 1：`screen=(613.95, **486.93**)`，而 **Screen.height = 480** ⇒ `ScreenToViewportPoint().y > 1`
    ⇒ `GraphicRaycaster.cs:176` **整条丢弃**（这就是为什么首中是 `<无>`、命中 0 条）；
  - endTurn 右内 1：`screen=(**646.26**,274.89)`，而 **Screen.width = 640** ⇒ `x > 1`，同上。
- **为什么只有一条红**：`inA2` 是 `&&` 短路的（`BattleScene.cs:29877-29881`）⇒ 第一个失败点之后不再打点，
  所以每颗钮只报一条。这也解释了 settings 报「上内」、endTurn 报「右内」、cameraReset 报「中心」。
- **修法（测试侧）**：取点后先过 `LayoutSpace.IsInsideScreen`（`LayoutSpace.cs` 已有这个口），
  **屏外的边直接跳过并记读数**；或把「内 1 px」改成 `min(1 px, 到屏幕边缘的安全距离)`。
  ⛔ 不要改 `raycastPadding`（那是原版判据）。
- **证据**：日志 `97001/97937` 的 screen 坐标；`BattleScene.cs:29803-29809/29877-29881`；`GraphicRaycaster.cs:175-177`。

> **结论（对应任务单的优先判据）**：T09 这 7 条是 **两个独立原因**：5 条 = 「测试里才激活 / 打开 ⇒ 没被渲染过 ⇒ 不可射线」，
> 2 条 = 「pad 外扩后的边界点落在 640×480 屏幕之外」。**没有一条是屏点缩放算错**。

---

## 二、T16 输入 2 条（#8 #9）

- **分类**：**δ（夹具几何与 hover 动画的先后）**。
- **根因**：测试在 `SimulateHover` **之前**用未悬停时的局部系取边界点：
  `inA/inB = halfW×0.86/0.90`、`outB = halfW×1.10`（`BattleScene.cs:19903-19905`），**然后**才
  `SimulateHover(inB)`（`:19917`）→ `UpdateHover` → `Relayout`（`CardInteraction.cs:355-361/861-865`）→
  `HandLayout.Refresh` 给被悬停那张 **`scale *= hoverScale(1.3)` + 抬高 30 px**（`HandLayout.cs:232/759-760`）。
  ⇒ `outB` 相对新框只有 `1.10 / 1.3 = 0.846` 个半宽，**仍在卡内** ⇒ `TickPressCandidate` 里那句
  `if (_pressInside && !card.HitByCollider(world)) _pressInside = false;`（`CardInteraction.cs:405`）**不触发**；
  `EndPressCandidate` 的 `_pressInside && _travel < TapThreshold`（`:436`）照旧成立 ⇒ 转发点击。
  两条是**同一个根因的两个观测面**（锁存没清 / 卡外松手仍算点击）。
- **为什么「灭自证」那条还是绿的**：「同样位移、没出卡 ⇒ 锁存仍为 1」本来就该绿。
- **修法（测试侧）**：把边界点的计算**移到 `SimulateHover` 之后**（`HitLocalToWorldForTest` 用的是当前 transform，
  会自然带上 1.3 倍缩放）；或者保留取点顺序但把外侧采样改成 `halfW × 1.45`（> 1.3，跨过悬停放大）。
  ⛔ 别改 `TickPressCandidate` 的判据口（W57 B2a 有意换成真 collider）。
- **证据**：日志 `63627/63640`（前提「inB 在内 / outB 在外」**绿**，但那是悬停前取的）；
  `BattleScene.cs:19903-19937`；`CardInteraction.cs:355/385/405/436`；`HandLayout.cs:232/759`。

---

## 三、T03 边界 2 条（#10 #11）

- **分类**：**α（测试助手 bug；上一轮只修了三份里的**一份**）**。
- **根因**：`edge03`（手牌，`BattleScene.cs:27043-27055`）与 `edgeU03`（场上，`:27152-27164`）的返回式是
  `return ix + (ox - ix) * lo;` —— **只回 x**。纵向边界传的是 `ix = ox = 0` ⇒ 无论二分收敛到哪，**恒返回 0.0000**。
  这正是上一轮诊断 §M 抓到的问题；修复只落在了换牌那一份 `edgeM03`（`:20357-20370`，带 `vertical` 形参、
  返回 `vertical ? ly : lx`）。
- **为什么横向四条一直绿**：横向 `oy-iy=0`，返回式本来就对。
- **修法**：把 `edgeM03` 的写法原样复制到 `edge03` / `edgeU03`（加 `bool vertical`，返回插值后的那一轴），
  调用点改成 `edge03(true, 0f, …, 0f, …)`。三份从此同形，⛔ 别再各写一份。
- **证据**：日志 `91431/91595`「实得 0.0000 / 0.0000」；`BattleScene.cs:27043-27059/27152-27168` 对照 `:20357-20374`。

---

## 四、T09-D①（#12）

- **分类**：**α（断言里的文件名比较写错）**。
- **根因**：扫描结果存的是 `"ErrorMessageBanner.cs:973"`（`BattleScene.cs:30067` 的
  `Path.GetFileName(f1) + ":" + (li1+1)`），而判据写：
  ```csharp
  System.IO.Path.GetFileName(d1Hits[0]) == "ErrorMessageBanner.cs"   // :30071
  ```
  `Path.GetFileName("ErrorMessageBanner.cs:973")` **就是它自己**（没有路径分隔符可切）⇒ 恒 `!= "ErrorMessageBanner.cs"`。
  日志已经把「实得 1 处：ErrorMessageBanner.cs:973；目录齐全 = True」打全了 —— 三个条件里前两个都成立，只差这一比。
- **修法**：`d1Hits[0].Split(':')[0] == "ErrorMessageBanner.cs"`（或 `StartsWith("ErrorMessageBanner.cs:", …)`）。
  顺手把 `d1Hits` 的元素改成 `(file, line)` 二元组，避免以后再有同族拼串比较。
- **证据**：日志 `98059`；`BattleScene.cs:30055-30074`。

---

## 五、部署 / 夹具 5 条（#13–#17）

> 任务单写「6 条（B2/B3/B5/E/F/W58④）」，但本轮 **28 条里没有 B5 的红**（日志 `BT    ✗` 全表已核）。
> 按实际红点给 5 条；B5 可能已被上一批夹具修复顺带清掉，建议复跑后现数（⛔ 别按条数写死）。

### 5·1 B2/B3（#13 #14）

- **分类**：**δ（夹具前提在 `Begin()` 后必然不成立）**。
- **根因**：两段都读**当前这局**的引擎棋盘（`b2ctx = driver.Ctx`，`BattleScene.cs:28576/28797`），
  而 B2 开头刚 `driver.Begin(...)`（`:28572`）—— 新局棋盘是空的；只有当 `b2ctx.Active != b2me` 时才推一次 AI
  （`:28578`）。**玩家先手时敌方一次都没动** ⇒ `Players[1-b2me].Board[*]` **必然全空** ⇒「敌方场上有一个单位」必红。
  B3 复用同一局（没有任何新的部署），同理。
- **修法（测试侧）**：别指望 AI 摆靶，**显式摆**一个敌方单位（照本文件既有写法：
  `ctx.Players[foe].Board[BoardSlots.SlotOf(BoardSlots.Right,0)] = new UnitState(...)`，注意连续无洞），
  或先把回合轮转到 AI 再 `StepThrough`。⛔ 不要为这条改生产。
- **证据**：日志 `95010/95178`；`BattleScene.cs:28572-28680/28795-28887`。

### 5·2 E / F（#15 #16）——**连续无洞模型把 2 号格夹到了 3 号格**

- **分类**：**δ（夹具过期；引擎行为是 2026-10-01 起有意的原版语义）**。
- **根因**：`RuleCore.PlayCard(cE, 0, iNob, 2)`（`BattleScene.cs:31442/31465`）之后直接读 `Board[2]`。
  但 `PlayCard` 的落点由 `BoardSlots.Insert` 算（`RuleCore.cs:3251`），
  `Resolve` 对「空盘 + 请求左侧第 2 格」的规则是：`index = |2-4|-1 = 1 > count(0)` ⇒ **夹到 0**
  ⇒ 真正落点 = `SlotOf(Left,0) = 3`（`BoardSlots.cs:136-140`）。
  ⇒ `Board[2]` 恒 null。**这不是回归，是连续无洞模型的既定语义**（`BoardSlots.cs:92-111` 的 doc）。
- **修法（测试侧）**：把判据改成「在棋盘上找得到这张卡」，用**实际落点**继续（`PlayCard` 会广播落点；
  或扫 `Board[0..8]` 找到 `Card == nob` 的格号），⛔ 别再写死 `2`。
- **证据**：日志 `99587/99639`；`BattleScene.cs:31439-31446/31463-31467`；`BoardSlots.cs:115-141`；`RuleCore.cs:3251`。

### 5·3 W58④（#17）

- **分类**：**α（整段测试插在了 `DestroyImmediate(driver)` 之后）**。
- **根因**：`RunCore` 在 `BattleScene.cs:30101` 把 `driver` 组件销毁了，文件里两处注释都写着
  「之后不要再依赖 driver」（`:30153`、`:30658`）。而 W58 治疗表现段（`:31480` 起）最末那节读 `driver`（`:31662`），
  此刻 `driver` 是指向**已销毁组件**的 C# 引用 ⇒ Unity 的 `== null` **为真** ⇒ 恒走 `Check(false, …)`。
- **修法**：把 W58④ 那一整块**搬到 `DestroyImmediate(driver)` 之前**（文件里已有两次同族搬家的先例，
  见 `:23961-23964` 的注释），或给这一段单独 `Begin()` 一局新 driver。
- **证据**：日志 `99950`；`BattleScene.cs:30101/30153/30658/31480/31662`。

---

## 六、文案 / 冻结 9 条（#18–#26）

### 6·1 精确语音「灭自证」（#18）

- **分类**：**γ（既有；HEAD 对照见下）**。
- **根因**：判据要求「至少一条精确绑定与后缀兜底不同」，实测 `0`（日志 `48034`：`696 条命中 · 0 条与兜底不同`）。
  精确表的来源是 `Resources/card_vfx_by_card.json` 的 `cards[].audio`（`BattleScene.cs:9037` 那条日志；
  断言在 `:14744-14774`），兜底表是 `Resources/voice_lines.json`。两份数据在此前某次重生成后**已经对齐**。
- **HEAD 对照（γ 的硬证据）**：
  - `git diff HEAD -- Core/VoiceLines.cs` = **空**（本轮没动判据代码）；
  - `cards[].audio` 逐卡比 HEAD vs 工作区：**696 条两边相同**，只有 `EC56` 的 `attack` 一条从
    `…_gen7` 换成 `…_attack`（与本条无关）；
  - `voice_lines.json` 最近一次整表重写是 **`3251df05`（第五十会话）**，在 HEAD 之前第 18 个提交。
  ⇒ 本条**不是本批引入**。
- **修法**：与同文件里已经处理过的 `exOnly > 0` 那条同法（`:14775-14784` 的注释）——把 `exDiff` 从「断言」
  降成**读数**，或者给精确档找一个新的**结构性见证者**（例如断「精确档按 `ev` 过滤后与兜底表顺序表不同源」）。
- **证据**：日志 `48021/48034`；`BattleScene.cs:14737-14784`；`git diff HEAD -- VoiceLines.cs` 空。

### 6·2 飘字位置 2 条（#19 #20）——**B4 的敌方偏移缩放没同步到断言**

- **分类**：**α（断言没跟上 2026-10-19 B4 的生产改动）**。
- **根因（数值逐位吻合）**：生产摆飘字时是：
  ```csharp
  // BattleDriver.cs:16717
  Vector3 popAt = at + PopOffset * (popFoe
      ? PopNumber.EnemyCardLocalToOurs / PopNumber.CardLocalToOurs : 1f);
  ```
  比值 = `0.5507 / 0.6071 = 0.9071`（`PopNumber.cs:139-158`）。而断言算的是**不乘因子**的
  `popBase + BattleDriver.PopOffset`（`BattleScene.cs:16280-16284`）。受击者是对面单位 ⇒ `popFoe = true`
  ⇒ 差值应为 `|1-0.9071| × |PopOffset| = 0.0929 × 0.573 ≈ **0.0532**` —— 与日志的 `0.0532` **逐位一致**。
  #20 是同一笔的连带：`dLinePop` 拿**已缩放**的实际位置去比**未缩放**的 2D 行线靶，只剩 `0.0310`（≤0.05）⇒ 红。
- **修法**：把「敌/我偏移缩放」收成一个**只读口**（铁律 6，判据只此一处），生产与断言都读它，例如
  `BattleDriver.PopOffsetWorldFor(seat)`（内部复用 `PopNumber.CardLocalToOursFor`，并暴露 `_me` 判断）。
  断言改成 `popAt = popBase + PopOffsetWorldFor(e.Player)`；
  #20 改成直接断**两个基准之间的距离**（`|DropTargetWorld - SlotPosition| > 阈值`），不要拿飘字位置去比第二个靶
  （两个靶现在只差 ~0.03–0.05，比不出「落在哪」）。
- **证据**：日志 `51758/51771`；`BattleDriver.cs:16714-16719`；`BattleScene.cs:16280-16296`；`PopNumber.cs:139-158`。

### 6·3 手牌数渲染高（#21）

- **分类**：**α/γ（断言与我们的字体度量不相容；HEAD 已如此的可能性最大）**。
- **根因**：`HandLabelWorldW/H` 读的是 uGUI 标签的实测设计 px（`BattleDriver.cs:12398-12399`），
  实测 `164.75 × 75.81`，而框是 `234 × 64`（`BattleDriver.cs:1572-1573`）。
  宽没问题、**高多 11.8 px**。autofit 的下限是 **50 px**（`HandLabelFsMin`，`:1573`），
  而我们字体的行盒 ≈ **1.437 em**（`CLAUDE.md`「汉字渲染」那条；原版 Pragati 是 0.947 em）
  ⇒ **最小字号那一档的行盒就已经 ≈ 72 px > 64 px 的框**。单行时 TMP 的行盒不会为了塞进框而再压
  （它压的是字号，而字号已经到 min）⇒ 结构上装不进。另注：文案走 `Loc.T("Battle/HUD/CardsInHand")`（官方表 EN = `Cards in hand`，
  2026-10-18 起中文档会给中文）—— **行盒差是字体度量问题，不是文案问题**。
- **HEAD 对照**：`BattleScene.cs` 这一段与 HEAD **逐字相同**（`git diff HEAD` 无该块，最近改动 `2ef39041`）；
  `Label.cs` / `TmpFont.cs` 本轮**无改动**。⇒ 不是本批引入（大概率自 uGUI 字族迁移 / 本地化合并后一直如此）。
- **修法（二选一，都是「照原版的效果」而不是「放宽断言」）**：
  1. 把这一颗的 `HandLabelFsMin` 从 50 降到能落进 64 px 框的值（≈ 64 / 行盒比 ≈ 40–44 px），
     并在注释里写明「原版 auto 下限 0.5 局域 = 50 px 是**原版字体**的度量；我们的拉丁面行盒 1.437 em ⇒ 需按目标像素折算」；
  2. 或改成断「**行盒高度 ≤ 框**」并把字体面换成行盒更接近原版的那一档（`TmpFont` 已有三份拉丁资产）。
  ⛔ 不要把断言改成「≤ 80」了事（那就是把「画得比原版高 18%」静默接受）。
- **证据**：日志 `81807`；`BattleScene.cs:25207-25212`；`BattleDriver.cs:1572-1573/12398-12399`；
  `Label.cs:144/1566/1195-1290`。

### 6·4 提示正文（#22）

- **分类**：**α（判据写于「本地没有 I2 表」之前）**。
- **根因**：断言要求 `ov.TipText == tipAct.arg`（`BattleScene.cs:25272-25275`），文案里写着
  「本地没有 I2 词条表 ⇒ 用 SO 里带的那份英文原文」。但**官方本地化已于 2026-10-18 并入**
  （`官方本地化_en+zh-CN.json`；`CLAUDE.md` 同条），提示正文现在走 `Loc.T(arg)` ⇒ 实测是中文
  「这是你的战将。<br>他的攻击值为2。<br>拖…」。
- **修法**：期望值改成 `Loc.T(tipAct.arg)`（若 `Loc` 的当前语档是中文；`Loc.T` 就是生产同一口），
  并把注释里的「本地没有 I2 表」改成「官方表已于 2026-10-18 并入，正文走 `Loc.T`」。⚠️ 不要改生产去直接印 `arg`。
- **证据**：日志 `82366`；`BattleScene.cs:25270-25275`；`CLAUDE.md` 铁律 2 的 2026-10-18 就地补注。

### 6·5 冻结 2 条（#23 #24）

- **分类**：**δ（夹具没有排空 `Begin()` 之后的阻塞窗口）**。
- **根因**：`FrozenNow = _frozenForTest || (Frozen != null && Frozen())`（`CardInteraction.cs:200`），而
  `Frozen = () => PlayerInputFrozen || WaitingForBlockingAction`（`BattleDriver.cs:3726`）。
  `driver.Begin(...)`（`BattleScene.cs:25967`）之后**只断了一句**就开始读（`:25972`），没有像本文件别的段落那样
  排空时间线/等闸（对照 B2 的 `StepThrough + while (WaitingForBlockingAction) Step(…)`，`:28574-28580`）。
  ⇒ 驱动自身 `PlayerInputFrozen=false`（第一条绿），但 `WaitingForBlockingAction` 仍真 ⇒ 第 2、4 条红。
  第 4 条把 `PlayerInputFrozen` 拨回 false 也解不开（OR 的另一半还在）。
- **修法（测试侧）**：`Begin` 之后先 `if (driver.InMulligan) driver.SimulateMulliganDone(); StepThrough(driver);`
  `while (driver.WaitingForBlockingAction) Step(1/30f);` 再断；或者把这条断言的靶改成
  `!driver.PlayerInputFrozenForTest`（它要验的就是「掉线冻结」那一档，不是演出闸），
  并另留一条「闸也会冻手牌」的独立断言。
- **证据**：日志 `87835/87887`；`BattleScene.cs:25965-25993`；`BattleDriver.cs:3726`；`CardInteraction.cs:192-200`。

### 6·6 G-2 教程跳过 2 条（#25 #26）

- **分类**：**δ（夹具选的点/单位在那一刻不可命中）** —— 但**需要先加一行读数再定稿**（下面写清）。
- **根因链（由「第三条绿」反推）**：第二条（空处置 skip）返回 false，而**紧接着**「跳过 ⇒ 窗口当场收工 + 气泡关掉」
  却 **绿**（日志）。这说明窗口**已经被关掉了** —— 只能是第一条（点单位）**被当成跳过收下了**：
  `RequestTutorialSkip(unit)` 返回 true → `_tutSkipNext=true` + `FinishTutorialActionWait()` 关气泡
  （`BattleDriver.cs:12728-12751`）⇒ 第二条落到 `if (!_tutWaitArmed) return false;`（`:12733`）。
  ⇒ 真正的问题只有一个：`TutorialClickHitsSomething(敌方督军位置)` 返回了 **false**（`BattleDriver.cs:12767-12776`）。
  最可能的两个成因（按概率）：
  1. **那颗敌方督军视图此刻不在激活层级**（教程开局把敌方督军藏着 —— 本文件 ⑤ 段已经验过
     `!fw.gameObject.activeSelf`，`BattleScene.cs:25248-25255`）。`HitSlot` 走 `Physics2D.OverlapPoint`
     （`CardView.cs:5967-5991`），对象停用 ⇒ collider 不参与查询 ⇒ 判「空处」。
  2. 那一点恰好落在 `TransformPoint` 的中心但 collider 有偏移/缩放（概率低）。
- **修法**：
  1. 先在 `TutorialClickHitsSomething` 的每个子判据上打一行读数（或加个 `TutorialClickHitsSomethingForTest`
     返回命中的是哪一项），把「是哪一项没判中」一次问清；
  2. 夹具侧：把「点单位」的靶换成**一个确定激活且已建视图的我方/敌方单位**（或先让它 `SetActive(true)`），
     并在前提里断 `g2Foe.gameObject.activeInHierarchy`（现在只断了 `!= null`，`:26091`）。
  ⛔ 不建议直接往命中表里加「不活跃视图也算」——原版 `Physics.RaycastNonAlloc` 同样打不到停用的 collider。
- **证据**：日志 `88789/88802`（以及紧随其后的那条 ✓）；`BattleScene.cs:26076-26107`；`BattleDriver.cs:12728-12776`；
  `CardView.cs:5967-5991`；`BattleScene.cs:25248` 的「敌方督军先藏着」。

---

## 七、联机视角 2 条（#27 #28）

- **分类**：**γ（既有生产限制：`SetMySeat` 中途翻座位不重建/不清账；与上一轮诊断 §B 同一族）**。
- **根因（读数直接指向）**：
  - `_me=1` 那一档**通过**（`MyUnits[5]=Battlewagon` / `FoeUnits[1]=Scout Bike`），说明 `DrainQueue` 那一步
    （`BattleScene.cs:21843`）是有效的；
  - 再 `SetMySeat(0)` 之后：`MyUnits[1]=null`，且 `DeployPending seat0/1 = **True**`（日志 `71037`）——
    也就是**翻回来这一下**把座位 0 的视图丢了、而 pending 账还挂着。`SetMySeat` 只写 `_me`
    （`BattleDriver.cs:2811-2816`），`SyncBoard` 用 `IsPendingDeploy` 压住建造（文件注释 `BattleScene.cs:21838` 自己引了 `BattleDriver.cs:17482`）。
  - #28 是 #27 的直接后果（`MyUnits[1]==null` ⇒ 选择器弹不出）。
- **HEAD 对照**：`SetMySeat` 与 `SyncBoard` 都不在本轮工作区 diff 里（`git diff HEAD` 无这两个方法），
  且上一轮 `W58_诊断_BattleScene50条.md` §B 已记「`SetMySeat` 中途翻座位不重映射身份表 = 生产侧未支持场景」⇒ **既有**。
- **修法（生产，铁律 11：要完全复刻这个能力）**：`SetMySeat(seat)` 翻到**不同**座位时：
  ① 清空 `_myUnits/_foeUnits` + 两张身份表；② 清掉属于 0/1 两侧的 `_pendingDeploy`（或按实例重映射）；
  ③ `RefreshAll()` 全量重建。测试侧随后只需 `DrainQueue`。
  ⚠️ 真机联机只在开局设置一次 `_me`，所以这一条**今天不影响玩家**；但掉线重连若换座会踩到。
- **证据**：日志 `71037/71320`；`BattleScene.cs:21820-21876`；`BattleDriver.cs:2811-2816`；
  上一轮诊断 §B；`git diff HEAD` 无 `SetMySeat/SyncBoard`。

---

## 八、给主对话的处置建议（按「一次修多条」排序）

1. **P0（一条修法清 5 条）**：T09 段在「点亮按钮 / 打开 Alliance 面板」之后**真渲一次**再打射线，
   并加「`Image.depth != -1`」前提断言。涉及 `BattleScene.cs:29638-29641 / 29789-29794 / 27889-27918`。
   若加渲染仍不中，转查「子画布 `GraphicRaycaster.eventCamera` 是否为 null」（那会是生产缺陷）。
2. **P1（改取点/改用例，共 12 条）**：T09 settings/endTurn 屏外点（2）· T16 悬停后取点（2）·
   T03 两份 `edge*` 加 `vertical`（2）· B2/B3 显式摆敌方单位（2）· E/F 读真实落点（2）·
   W58④ 搬到 `DestroyImmediate(driver)` 之前（1）· 提示正文改 `Loc.T`（1）。
3. **P1（一行）**：T09-D① 的文件名比较（1）。
4. **P1（生产 + 断言同源）**：飘字敌方偏移缩放（2）—— 建议新增 `PopOffsetWorldFor(seat)` 只读口。
5. **P1（夹具）**：冻结段 `Begin` 后排空闸（2）· G-2 先加读数、再换一个激活的单位（2）。
6. **P2**：语音灭自证降级为读数（1）· 手牌数字框高与字体行盒（1）· `SetMySeat` 重建（2，生产）。

## 九、没查清 / 需要一次运行才能定稿的部分

- **T09 的「`depth == -1`」是推论**（代码 + 日志相关性很强，但没有一条直接读数）。加两行读数即可定案：
  `img.depth / img.canvasRenderer.cull / GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas).Contains(img)`。
- **子画布 `GraphicRaycaster.eventCamera`**：本机 Unity 对嵌套 `Canvas.renderMode/worldCamera` 的返回值没有现成记录；
  若 A985 加渲染后仍 0 命中，必须先打这三个字段再下结论（它决定「生产通不通」还是「只是自检环境」）。
- **G-2 的「单位视图是否 activeInHierarchy」**：日志没有打印，只能从「第三条绿」反推「第一条被收下了」。
- **手牌数标签的 `FontPxNow` / 行数 / 实际框高**：日志没打；`75.81` 的来源（1 行 72 px 还是 2 行 36 px）需要一次读数确认，
  再决定「降 min 字号」还是「换字面」。
