# P-HIT · 「命中区覆盖」探针 · 只读侦察

> 立项 = `资料/待办判据_1018.md` **§A964**（用户 2026-10-18 点选）。本文件 = **第一步·只读侦察**的产出：
> 把「怎么写这个探针」查成一份**别人能照着写的规格**。⛔ 本文件**不含实现**、**没跑 Unity**、**没改任何别的文件**。
> 判据（已定、不改）：**凡参与点击命中的节点，它的【命中区矩形】必须覆盖它【画出来的矩形】**；
> ⚠️ 量 quad 不量节点 · ⚠️ 要命中就必须带 `ImageQuad` · ⚠️ 分层用 `RenderQueue` 不靠 z。

---

## 1. 一句话结论

**能批处理化，但要先改三处口径，否则跑出来是「一片假红 + 一堆假绿」。**

1. 🔴 **判据里的「画出来的矩形」在场景树里【没有唯一答案】** —— 同一个命中矩形上常叠着**装饰件**
   （实测例：`Shell/CampaignTab.cs:478-459` 的 `Premium Mark` 用 `localScale = (2,2,1)` 画在**同一个 `r`** 上，
   实显 200² 而命中区是 100²）⇒ 纯几何「谁盖住谁」扫描会把它报成缺陷。
   ⇒ **要么**给探针一份「可见件 → 归属哪颗按钮」的登记（要动 `MenuDraw.Hit` 加一个可选形参），
   **要么**只扫「不需要归属」的那几类（见 §5 的 E1/E3/E4）。
2. ✅ **有一类判据**今天就能机械判、零假红：**「不可命中」= `WindowButton` 拿不到活的 `ImageQuad`**（A8 那一族）。
   判据全在 `Shell/PointerLayer.cs:897 HitQuad` / `:944 Navigable` 一处。
3. 🔴 **两个已知阳性（`A8` / `E12`）今天【都已修掉】，不能当活体阳性** ⇒ 探针必须**造夹具**（§4）。
   ✅ 但我在战斗侧找到**一条今天还活着**的同类阳性：`CameraResetButtonHit`
   （命中 `48.443×45.846` **小于**实绘 `61.846²`，`Battle/BattleDriver.cs:3117-3122` vs `:11349`）
   —— 它是**原版 `m_RaycastPadding = (−8,−8,−8,−8)` 的正当结果** ⇒ 正好当「探针必须报得出来 + 白名单机制可用」的**校准样本**。
4. 📌 **切块：战斗侧 / 外壳侧两块互不依赖，可以并行两个写手**（不同文件，见 §5·文件所有权）。
   但**两块各有一个前置**：外壳侧要 `ShellScene.Build` 的可见性（今天是 `private`），战斗侧要一张**手写的命中入口表**（战场**没有命中表**，§2·b）。

---

## 2. 命中判定路径（外壳侧 / 战斗侧 **不是同一套**）

### 2·a 外壳侧 —— **有一张全局表，但是「现扫」不是「登记」**

| 问题 | 答案 | 出处 |
|---|---|---|
| 表在哪 | **没有登记表**。每次事件现扫全场：`Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None)` | `Shell/PointerLayer.cs:908 AllButtons` |
| 谁进表 | 场景里**所有** `WindowButton`（含关着的窗里的？—— 见下） | `:1103 CollectHits` |
| 键是什么 | **没有键**：命中靠逐颗比矩形，不是查字典 | `:1103-1131` |
| 矩形从哪来 | 那颗按钮**子树里第一个 `ImageQuad`**：`b.GetComponentInChildren<ImageQuad>()` | `:897 HitQuad` |
| 中心/半宽 | 中心 = `q.transform.position` → `LayoutSpace.PxX/PxY`；半宽 = `q.WorldW/H × lossyScale × 108 / 2` | `:870-886 HitBoxPx` |
| 谁不能命中 | ① `b.isActiveAndEnabled` 为假 ② `q == null` ③ `q.gameObject.activeInHierarchy` 为假 ④ 所属窗 `CurrentState == Background` | `:897-903` · `:1093 PointerReachable` |
| 赢家怎么定 | **渲染队列大的先**，同队列比 `z` 小的先（`q.transform.position.z`） | `:1059-1071 HitButton` |
| 注册/注销口 | ⚠️ **命中表没有注册口**（现扫）；**但有生命周期的是另一张表** = **滚动区表** `_scrolls`：`RegisterScroll` / `UnregisterOwnedBy` / `ClearScrolls` / `PruneScrolls` | `:188` `:201` `:212` `:223` |
| 可被自检直调的入口 | `ClickAt(px,py)` · `ButtonAt(px,py)` · `ScrollUnder(px,py)` · `HoverAt` · `PressAt/MoveTo/ReleaseAt` · `TickAt` | `:916-1054` |
| 计数口 | `ButtonCountForTest`（全场景）· `ButtonCountUnder(root)`（子树） | `:953` `:966` |

🔴 **`A867` 那次泄漏的是【滚动区表】不是命中表**（`_scrolls` 只增不减 / 死条目还能被滚轮命中）——
命中表是现扫的，所以它**没有生命周期问题、但也没有「谁登记过」这个信息**。
⇒ **探针不能问「表里有什么」，只能问「谁能被点中」**（`ButtonAt`）或「谁的 `ImageQuad` 拿不到」（`HitQuad`）。

🔴 **外壳侧「命中矩形」与「命中 quad」是【恒等】的**：`MenuDraw.Hit` 建命中区时
`SetPxSize(hit, hr.W, hr.H)` + `MakeHitQuad(hit, hr, q, …)`（quad 摆在该矩形中心、`anchor=(0.5,0.5)`、
`WorldH = hr.H/108`、`SetAspect(hr.W/hr.H)`）⇒ 由 `HitBoxPx` 反算出来的矩形 = `hr`（M==1 时逐位相等）。
**出处**：`Shell/MenuDraw.cs:1790 Hit` · `:1848-1850`（`SetPxSize` + `MakeHitQuad`）· `:1871 MakeHitQuad`。
⚠️ 而 `MenuDraw.Hit` 是**全壳唯一一处**建命中区（`WindowsManager.AddHit:321` 转调它、
`MenuWindowBase` 的 `AddHit` 已上移到 `GameWindow`、`MenuDraw.DeckCell` 也走 `MakeHitQuad:2813-2819`）
⇒ **「命中区 vs 命中 quad」这一对在外壳侧结构上不可能违判据**，
**唯一会违判据的是「命中 quad vs 它旁边那件【可见】的图形」**。

### 2·b 战斗侧 —— **没有命中表，逐 handler 各判各的**

`BattleDriver.cs:12584-12598 LogClickBeat` 的注释白纸黑字写着：
> 「**战场里没有单一命中表**（卡 / 手牌 / HUD / 面板各判各的）」

实测**三种命中模型**并存：

| 模型 | 形状 | 实例（文件:行号） |
|---|---|---|
| **① 局部矩形**（逐字精确） | `transform.InverseTransformPoint(world)` 后比 `± W/2, ± H/2`（**含 `anchor` 偏移**） | `Battle/ImageQuad.cs:528 Contains` · `Battle/Label.cs:185 Contains` · `Core/CardView.cs:3396 Contains` · `Battle/SkillPanel.cs:277` · `Battle/WfSlider.cs:398`（含手柄探出轨道那一块） |
| **② 硬写 px 矩形** | 拿一串常量算 px 再比 —— **与实绘矩形无关** | `BattleDriver.cs:3117 CameraResetButtonHit`（`CameraResetHitPxW/H`）· `Battle/SettingsPanel.cs:376 RectContains`（`ResignWPx/HPx`）· `Battle/ChatPopupPanel.cs:357 Hit`（`r.width × VisibleHeight/RefH`）· `Battle/MultiCardDisplay.cs:236 HitContinue`（`BarCx/BarW/BarCy/BarH`）· `BattleDriver.cs:6285 HitMyDeckPile`（`DeckPlatePx`） |
| **③ 最近格 / 圆**（**没有矩形**） | 找最近的槽位或圆心距 | `Board/BoardLayout.cs:376 TryResolveSlot`（x 最近 + `snapTolerance`）· `Board/ArenaSlots.cs:147 TryResolveSlot`（屏幕 x 最近 + `pitch×0.55`）· `Battle/AttackSelector.cs:616-638 UpdatePointer`（**圆**：`d.x²+d.y² ≤ half²`） |

**点击路由（`PollInputEdges` 一族）**：`BattleDriver.cs:12551 PollInputEdges` 只算**两条沿**
（`_downEdge` / `_upEdge`，`Mouse.current` 轮询，`:12606 PointerHeld` → `:12609 PointerHeldRaw`），
**不做命中**。命中在**每个 handler 自己那一句**里：
`HandleSettings:2892` · `HandleOffensiveButton:2947` · `HandleCameraResetButton:3127` · `HandleChatPopup:3202` ·
`HandleBattleLog:3556` · `HandleMulligan:3701` · `HandleChoose:4574` · `HandleReplayBar:3474` ·
`TickMultiCards` / `TickCardDisplayClick:6258` · `DrivePlayerTurn`（棋盘/手牌那一支）。
⇒ **加一处命中 = 加一处 `Contains`**，**没有任何一处能自动保证「命中区 ≥ 实绘」**。

**手牌拖拽另算一条路**：`Hand/CardInteraction.cs` 是**独立 `MonoBehaviour`**（自己 `Update`），
命中 = `:583 HitTest` → 逐张 `CardView.Contains`（`:590`），赢家按 `z` 最小（`:592`）。
它**不走 `BattleDriver`**，也**不走 `PointerLayer`**；冻结闸门也是另一份（`:180-190` `Frozen`）。
⚠️ 它还有一条 `PointerWorldSafe:641` 的**野值防护**（世界坐标超可见区 ±1.5 倍就丢帧）—— 探针喂点时要绕开它。

---

## 3. 「渲染矩形」怎么量（逐类绘制件 —— **先读现成的，别另造**）

> 全仓**只有两个**绘制组件：`CardPresentation/Battle/ImageQuad.cs:13` 与 `CardPresentation/Battle/Label.cs:29`
> （`grep -rn "class ImageQuad\|class Label" --include=*.cs d:/4/Unity/MyGame/Assets` ⇒ **各只一处**）。
> ⚠️ 但**量法**在编辑器里有**四份**（`ShellScene` / `MainMenuScene` / `CollectionScene` / `RewardsScene`），
> 探针**必须挑一份并写明用的是哪一份**。

### 3·1 `ImageQuad` 的四边形

**现成读口（两份，语义有别，⛔ 别再造第三份）：**

| 读口 | 算式 | 出处 |
|---|---|---|
| `ShellScene.QuadPxRect(q, out x1,y1,x2,y2)` | `LayoutSpace.ToPixel(q.transform.position)` ± `q.WorldW/H × 108 / 2` | `Editor/ShellScene.cs:141-151` |
| `MainMenuScene.QuadPxRect`（**同形**，写的是 `PxX/PxY` 直式） | `q.WorldW/H × 108f` | `Editor/MainMenuScene.cs:10581-10591` |

- `WorldH` = `_worldH`（建它时传进去的高），`WorldW` = `_worldH × _aspect`：`Battle/ImageQuad.cs:107-108`。
- 网格本体（= **真正画出来的四边形**）：`RebuildMesh()`，顶点 = `[-anchor.x*W, (1-anchor.x)*W] × [-anchor.y*H, (1-anchor.y)*H]`：`:343-365`。
- 🔴 **陷阱 1（量纲）**：这两份 `QuadPxRect` **都不乘 `lossyScale`**，而
  `PointerLayer.HitBoxPx:883-884` **乘**。`TransformScalerBySmallScreenUI` 把窗根乘 M 之后两者分家
  （1.2 倍窗 ⇒ 实绘是 1.2 倍、`QuadPxRect` 少报 17%）。自检跑在**出厂态（M==1）**所以一直没露
  （A167 那段的原话：`PointerLayer.cs:862-865`）。⇒ **探针要两态都跑，量之前自己乘/除 M。**
- 🔴 **陷阱 2（anchor）**：`PointerLayer.HitBoxPx` 拿 `q.transform.position` 当**矩形中心**，
  这**只对 `anchor == (0.5,0.5)`** 成立（`RebuildMesh` 会按 anchor 平移网格）。
  今天外壳所有命中 quad 都是 `(0.5,0.5)`（`MakeHitQuad:1877` · `MenuDraw.Rect:1374` · `MenuDraw.Node`），
  但**没有任何断言钉住**。⇒ 探针加一条结构断言：**「命中 quad 的 anchor 必须是 (0.5,0.5)」**。
  （战斗侧不受影响：`Contains` 用的是 `InverseTransformPoint` + `anchor`，天然正确。）
- 🔴 **陷阱 3（九宫格/平铺）**：`CreateNineSlice:422` / `CreateTiled:499` 返回的是**根节点**，
  **根上没有 `ImageQuad`**（quad 在 8~9 个/若干个子块上）⇒ 若哪天有 `WindowButton` 挂在那种根上，
  `HitQuad` 会取到**第一块角块**，命中区缩成一块角。今天 `grep` 没发现这种用法
  （108 处 `.Nine(` 调用点里没有一处紧跟着 `AddComponent<WindowButton>()`）—— **但探针要显式断言这一条**。
- 🔴 **陷阱 4（软边切块）**：走了 `MenuDraw.ApplySoftEdges` 的宿主会**被切成多块独立 quad**
  （`ImageQuad.cs:40-96` 的 `_softKids`）⇒ **只量宿主那一块会漏**，要量「所有块的并集」（`PiecesOutsideClip:121` 那种扫法）。

### 3·2 TMP 文字的渲染矩形

**现成读口 = `ShellScene.TmpSpanPx`（全仓唯一一份实现，2026-10-16 已收口），两个重载：**

| 重载 | 用途 | 出处 |
|---|---|---|
| `TmpSpanPx(Label lb, out minX,out minY,out maxX,out maxY, bool includeInactive=false)` | 单条文字 | `Editor/ShellScene.cs:192-200` |
| `TmpSpanPx(Transform root, …, out int verts)` | **扫子树所有 TMP** | `:217-225` |
| 内层唯一实现 `SpanOfTmp` | `isVisible` 过滤 + **按 `materialReferenceIndex` 取槽** + `LayoutSpace.ToPixel(tmp.transform.TransformPoint(v))` | `:237-264` |

- 另一族（**有意不收**，各自原因写在注释里）：`Editor/RewardsScene.TmpVertPx / TmpVertsAndAlpha / TmpGlyphUvW`（要逐点序列）· `Editor/MainMenuScene.CountSoftFadedTextVerts`（按 y 带数 alpha）· `Editor/IconSizeProbe.InkHeight`（**离树合成**的 TMP，单位是局部单位不是画布 px）。
- 战斗侧**已有的另一套**：`Editor/BattleScene.cs:249 TmpInkCenterPx(Label, int line=-1)`（量**字墨中心**，
  相对本节点、向上为正）——它是**同一个数据源**（`characterInfo[i].topLeft/bottomLeft`），但只给中心不给四边。
  ⇒ 探针要**四边**就用 `TmpSpanPx`；要**中心对齐**再考虑它。
- ⚠️ `Label.WorldW/WorldH`（`Battle/Label.cs:91-93`）**不是**渲染矩形 —— 那是它自己的框，
  「字号对而溢出」时照样看着对（`AutoFitBox` 那条教训，`ShellScene.cs:156-157` 写得最清楚）。

### 3·3 「副本」清点（题面问的「别名/副本」）

- **绘制组件没有副本**：`ImageQuad` / `Label` 各一个类，都在 `CardPresentation/Battle/` 下（命名空间 `CardPresentation`）。
  外壳**没有**第二份 `ImageQuad`。
- **量法有副本**（§3·1/§3·2）——这是探针真正要小心的地方。
- **命中路径有 3 条独立实现**：① `Shell/PointerLayer.cs`（外壳）② `BattleDriver` 的逐 handler（战斗）
  ③ `Deck/DeckRuntime.cs:732 HandlePointer` / `:1379 Hit`（**卡组编辑**，它自己一条路，**不在 A964 的范围里**，
  但它是最早有真鼠标交互的一条 ⇒ 将来要扫它得另开一块）。

---

## 4. 两个已知阳性样本的现状（`A8` / `E12`）

### 4·1 `A8` —— 卡组格命中区是裸节点

| 项 | 事实 | 出处 |
|---|---|---|
| 当年长什么样 | `MenuDraw.DeckCell` 的命中区用 `Node()` 建**裸节点**（不带 `ImageQuad`）⇒ `PointerLayer.HitQuad` 拿不到 ⇒ **真鼠标点不动**，而 `wb.Click()` 直调照样过 ⇒ 自检全绿 | `Shell/MenuDraw.cs:2795-2806` 的注释（原文保留在里面） |
| 怎么修的 | 加 `MakeHitQuad`（**全工程只此一份**，2026-10-04 A26 收口）；`DeckCell` 现在 `Node(cell,"Hit",hr)` + `MakeHitQuad(hit, hr, qHit, Vector3.zero)` | `MenuDraw.cs:2813 MakeHitQuad` · `:2818-2820` |
| 今天还能不能当阳性 | ❌ **不能**。今天那条路已经带回 quad。 | —— |
| 现有的回归锁（照着写就行） | `Editor/CollectionScene.cs:1334-1412` —— **走真命中路**：`pl.ButtonAt(PxOf(c0.position.x), PxYOf(c0.position.y))` 断言拿到的是**这一格**的 `Hit` 节点；再加「压在视口边那一格只认露出来那块的中心」。注释自己写着「**这一段是会红的**：把 `DeckCell` 改回裸节点它立刻红」 | `Editor/CollectionScene.cs:1341-1412` |
| 怎么**造**一个阳性 | ① **静态**：直接对每颗 `WindowButton` 问「子树里第一个 `ImageQuad`（且 `activeInHierarchy`）拿不拿得到」；② **构造夹具**：造一颗**只有 `WindowButton`、没有 quad** 的节点（`MenuDraw.Node(...)` + `AddComponent<WindowButton>()`），探针必须报出来。判别式 = 给它补上 `MakeHitQuad` ⇒ 必须**不**报。 | —— |

### 4·2 `E12` —— 聊天钮 / 墓园钮「藏起来了还点得到」

| 项 | 事实 | 出处 |
|---|---|---|
| 当年长什么样 | 教程局里那两个钮被 `SetActive(false)`（原版 `TutorialSetup` 就是 `SetActive`），但**我们这两颗是 `ImageQuad`** —— `Contains()` **不看 `activeSelf`** ⇒ **看不见却点得开** | `Battle/TutorialOverlay.cs` 那条对话 + `真Play待验清单.md` E12 行 |
| 怎么修的 | 在**命中那一句之前**补 `activeSelf` 判据：聊天钮 `if (_chatBtn == null \|\| !_chatBtn.gameObject.activeSelf \|\| !_chatBtn.Contains(WorldPointer())) return false;`；墓园钮 `if (!_cemeteryBtn.gameObject.activeSelf) return false;` | `Battle/BattleDriver.cs:3220-3224` · `:3581-3585` |
| 同族已经有的 | `_offensiveBtn`（`:2949`）· `_cameraResetBtn`（`:3118`）本来就判了 `activeSelf` | —— |
| 同族**漏了**的（**今天不可达，是隐患不是活缺陷**） | `_settingsBtn`（`:2908-2909`）**没有** `activeSelf` 判据 —— 但全仓**从来没关过它**（`SetVis`/`SetActive` 都不作用在它身上）⇒ 现在点不出问题 | `BattleDriver.cs:2908-2909` |
| 🔴 **今天有没有断言钉住它** | ❌ **没有**。`ChatButtonVisibleForTest` / `CemeteryButtonVisibleForTest` 只断**可见**（`Editor/BattleScene.cs:14653/14656/14673/14683`），`HudButtonActiveForTest`（`BattleDriver.cs:12700`）**全仓 0 个调用点**。那些 `TickChatInputForTest`（`BattleScene.cs:13290-13320`）断的是**按下 vs 松手**那条沿，**不是**「藏起来还响应」 | `grep -rn "HudButtonActiveForTest\|ChatButtonVisibleForTest" Editor/` |
| 今天还能不能当阳性 | ❌ 不能当**活体**阳性（代码已带守卫）；✅ **但可以当【判别式】阳性**：探针写「`SetActive(false)` 后喂钮心给路由 ⇒ 必须 `false`」—— **把 `activeSelf` 那一句删掉它立刻红**。这就是 A964 要的那种「改哪两处会一起变绿」的反面。 | —— |

### 4·3 🔴 一条【今天活着】的阳性（我找到的，建议当探针的校准样本）

**`CameraResetButtonHit`（战斗侧「重置自动镜头」钮）**

| 项 | 值 | 出处 |
|---|---|---|
| **命中矩形** | `CameraResetHitPxW × CameraResetHitPxH` = `64.443−16 = **48.443**` × `61.846−16 = **45.846**` | `BattleDriver.cs:3035-3036` · `:3117-3122` |
| **实绘矩形** | `HudAbs(root, "40k_UI_bt_center_camera", 17.9293, 568.174, 64.443, 61.846, …)` —— 而 `HudAbs` 里 **`w` 只参与算中心，实绘宽由贴图比例定**（`BattleDriver.cs:11299` 那句注释）；贴图 `40k_UI_bt_center_camera.png` 是 **237×237（1:1）** ⇒ **实绘 = 61.846 × 61.846** | `:11349` · `:11187-11195` · PNG 头实读 |
| 差 | 命中比实绘**每边少约 6.7~8 px** ⇒ **命中区不覆盖实绘** ⇒ **按 A964 的字面判据，这就是一条违规** | —— |
| 但它是**正当的** | 原版 `Image.m_RaycastPadding = (−8,−8,−8,−8)`（负 = 往里缩）⇒ 原版**本来就**「画得比点得到的大」 | `:3028-3032` 的判据段 |
| 为什么正好当校准样本 | ① 跑一次一定报得出来（不会「扫完 0 条」）；② 报出来之后按原版判据进**白名单** ⇒ 顺手验证「白名单 + 每条写判据」这套机制可用；③ 它是**判据反例**，逼着探针把判据说清楚：**「命中 ≥ 实绘」只在原版 `m_RaycastPadding ≥ 0` 时成立** | —— |

⚠️ **同族的另外两条**（也要一起进白名单或一起判）：`Shell/MenuDraw.PaddedHitRect` 那族
（`GameWindow.Clip`/`ClipPad` 把命中区**缩到视口内** ⇒ 命中 < 实绘是**正当**的，`ShellScene.cs:3020-3100` 已有一套断言）·
`TutorialOverlay` 的跳过钮（命中 `300×90` > 实绘 `300×65.64`，**反方向**，是 `FitHeight` 内接的正常结果，
`Battle/TutorialOverlay.cs:318-322` 有注释）。

---

## 5. 探针规格（入口 · 宿主 · 断言 · 产物 · 文件所有权 · 切块）

### 5·0 四条**可机械判定**的检查（建议按这个编号写）

| 编号 | 检查 | 要不要「可见件归属」 | 假红风险 |
|---|---|---|---|
| **E1** | **不可命中**：每颗 `WindowButton` 的 `HitQuad` 必须非 null | ❌ 不要 | **零** |
| **E2** | **覆盖**：命中矩形 ⊇ 实绘矩形 | ✅ **要** | **高**（装饰件 / 原版负 padding） |
| **E3** | **平手**：同一渲染队列的两块命中区取交非空 ⇒ 报（A9 那一族） | ❌ 不要 | 低（要白名单：**故意的**同档，如 `Absorb` 与文字档同号，`MenuDraw.cs:2103-2106` 明说无害） |
| **E4** | **藏起来还响应**：钮 `SetActive(false)` 后，喂它的中心给对应路由 ⇒ 必须 `false`；`SetActive(true)` ⇒ 必须 `true`（**两态**） | ❌ 不要 | 低 |

> 🔴 **E1 + E4 就能覆盖 `A8` / `E12` 两条立项理由**，而且**零假红**。
> **E2 是最贵也最容易假红的一条** —— 建议**分两级上**（见 5·3·B）。

### 5·1 宿主与入口约定

**现成的 `Run` 约定**（照抄 `Editor/IconSizeProbe.cs`，全文 195 行，是最好的模板）：

```csharp
public static class XxxProbe {
    const string P = "XXX ";
    public static void Run() {
        Directory.CreateDirectory(OutDir);          // OutDir = @"d:/4/_tmp_view/<名字>"
        … 断言 …
        Finish();
    }
    static void Finish() { if (Application.isBatchMode) EditorApplication.Exit(0); }
}
```
- `IconSizeProbe.cs:17-19` 的用法注释给出了命令行形状：`-executeMethod IconSizeProbe.Run`。
- `Editor/Round1015Probe.cs:89/196/209` 是**一个文件两个入口 + 一个 `All()` 串跑**的形状（参考它怎么组织）。
- `Editor/EndPanelProbe.cs:85` 是**带退出码**的形状：`EditorApplication.Exit(bad == 0 ? 0 : 1)`。
- `Editor/ChatBoxProbe.cs:16` 是最小的一个（最短的模板）。

**两个宿主的「怎么建场景」**：

| 侧 | 建法 | 可见性 | 备注 |
|---|---|---|---|
| **外壳** | `ShellScene.Build(out Transform root)` —— 里面已含：新建空场景 + `Main Camera`（`aspect` 必须在建任何东西**之前**定死）+ `AudioListener` + `ShellRuntime.Build()` | 🔴 **`private static`**（`Editor/ShellScene.cs:556`）⇒ **别的文件调不到** | 要么把它改成 `internal static`（**1 行**），要么把探针**写进 `ShellScene.cs` 里当新段** |
| **战斗** | `BattleScene.BuildScene(out BattleDriver driver, out BoardLayout playerBoard, …)` | ✅ **`public static`**（`Editor/BattleScene.cs:17122`） | ⚠️ 但 `BattleScene.Run` 在 `BuildScene` **之前**还压了一堆全局态（`CardTween.Mode = Manual` · `ReplayStore.OverrideDir` · `AutoZoom/SmallScreenUI` 压成确定档 —— `BattleScene.cs:281-330`）⇒ 单独建场景时要**手抄**这一堆 ⇒ **夹具漂移风险**（本工程有先例：「夹具手抄 `WindowsManager`」带出 8 条级联红） |

⇒ 🔴 **我的建议：两个探针都写成【宿主文件里的新段】**（`ShellScene.cs` 里一段 / `BattleScene.cs` 里一段），
**理由**：① 直接复用宿主已经建好的现场与那堆全局态压平；② 文件私有助手现成（`QuadPxRect:141` · `TmpSpanPx:192` ·
`Check/CheckTrue/CheckNear:27/39/67` · `MakeNavButton:531` · `BattleScene.TmpInkCenterPx:249` · `BuildScene` 的 `out` 参数）；
③ 宿主文件**互不重叠** ⇒ **并行仍然成立**。
⚠️ **代价**：这**推翻**了 `资料/可并行任务清单.md:570` 那一行「各自**新建**一个 `Editor/*.cs` 探针文件」的措辞
⇒ **要么按本条改那一行，要么接受下面「新文件」那一版的额外接线**（§5·4 列了两条路的清单）。

### 5·2 断言用什么

**宿主里现成的 `Check` 家族**（别另造一套）：

| 助手 | 出处 |
|---|---|
| `Check<T>(got, want, msg)` | `Editor/ShellScene.cs:27-37`（`BattleScene` 里的同族在它自己的头部） |
| `CheckTrue(bool, msg)` | `ShellScene.cs:39` |
| `CheckNear(float got, float want, float tol, string msg)` | `ShellScene.cs:67` |
| `Section(string)` | `ShellScene.cs:25` |

🔴 **写断言的三条纪律**（本工程反复踩，`可并行任务清单.md:592` 把它列成「派活必写」）：
① **不许自证/同义反复** —— 期望值要来自**原版字面量或独立那一侧**，⛔ 不读我们自己的常量；
② **弱断言分不出两种状态** —— 每条要配**两态**（改坏法必须红）；
③ **灭自证** —— 补一条**结构上不可能同时满足**的断言（例：`SetActive(false)` ⇒ 路由必须 `false`；
`SetActive(true)` ⇒ 必须 `true`；把实现的守卫删掉就会**恰好有一个红**）。

### 5·3 逐侧规格

#### 5·3·A 外壳侧（宿主 `Shell.unity`）

**扫描顺序（必须逐窗，否则假红）**：
```
shell.Windows.CloseAllWindows();
foreach 每扇窗 W:            // 从 ShellScene.Run 已有的开窗清单里取（或按 GameWindow 子类枚举）
    W.Open()                 // ⚠️ 窗没开 ⇒ 它的钮 activeInHierarchy 假 ⇒ HitQuad 拿不到 ⇒ E1 全假红
    扫一遍（E1 / E3）
    关掉
```
- ⚠️ `WindowsManager.CloseAllWindows()` 是现成的（`ShellScene.cs:1257` 在用），
  `OpenWindow(win, …)` 也是（`:984`）。
- ⚠️ **被压到 `Background` 的窗不参与指针命中**（`PointerLayer.cs:1093 PointerReachable`）⇒
  扫描时**一次只开一扇**（或把底下那扇的状态算进去），否则 E3/E2 会读到「谁都没赢」。

**E1（不可命中）的判据只此一份** —— `HitQuad`（`PointerLayer.cs:897-903`）。
🔴 **探针不许重抄这段**（抄了就是自证）。两条出口（挑一条，或两条都发）：
- `PointerLayer` 加一个**只读**口，例如 `public static bool HitQuadForTest(WindowButton b)` /
  `HitRectPx(WindowButton b, out float x1,out float y1,out float x2,out float y2)`（把 `:870 HitBoxPx` 的算式交出来）；
- 或**反着问**：拿那颗钮的**世界中心**去 `PointerLayer.Instance.ButtonAt(px,py)`，看赢家是不是它
  （`CollectionScene.cs:1362` 就是这么干的）——**这条路更「真」**（走的是生产的赢家算法）。
  ⚠️ 反着问的假红来源：**被别的件盖住**（那是 E3 的题目，不是 E1 的）⇒ 报的时候要把「赢家是谁」一起打出来。

**E3（平手）**：对每队**同队列**命中区取交 ⇒ 非空逐条报。
白名单：`Absorb` 算出来的那档**故意**可能撞文字档（`MenuDraw.cs:2103-2106` 明说「别去改它」）——
但文字层**不带命中区**（同处注释），所以只比**命中区之间**就不会踩到这一条。

**E4（藏起来还响应）**：外壳侧今天没有这种缺陷（`HitQuad` 已经要求 `activeInHierarchy`）
⇒ 这条**只做战斗侧**（见下）。⚠️ 但**别把它误加到外壳侧**：外壳侧「藏起来」= 不可命中是**正确**行为，
与战斗侧的「`ImageQuad` 不看 `activeSelf`」**正好相反**（见 §6·9）。

#### 5·3·B 战斗侧（宿主 `Battle.unity`）

**① E4（藏起来还响应）—— 最便宜、马上能写、今天就有判别力**：
```
foreach which in {"chat","cemetery","offensive","settings","cameraReset"}:
    var w = driver.HudButtonWorldPosForTest(which);      // 已有口，BattleDriver.cs:12686；"cameraReset" 要补一个 case
    driver.SetHudButtonActiveForTest(which, false);      // ⚠️ 要补：今天没有这个口（HudButtonActiveForTest:12700 只能读不能写）
    BattleDriver.PointerWorldForTest = w;                // :12622
    … 走对应 Tick*ForTest（:12644-12666）…
    断言「没反应」
    SetHudButtonActiveForTest(which, true); 断言「有反应」
```
- 现成的口：`HudButtonWorldPosForTest(which)`（`:12686`，4 档：settings/cemetery/chat/offensive）·
  `TickChatInputForTest` / `TickLogInputForTest` / `TickHudButtonsForTest` / `TickSettingsInputForTest`（`:12648-12666`）。
- ⚠️ **要补两个口**（都是**只读/只写测试态**，不碰生产路径）：`SetHudButtonActiveForTest(which, on)` ·
  `HudButtonWorldPosForTest` 的 `"cameraReset"` 档（今天只有 4 档）。
- **判别式**：把 `BattleDriver.cs:3223` 那句 `!_chatBtn.gameObject.activeSelf` 删掉 ⇒ 这条必须红。

**② E1（不可命中）**：战斗侧没有 `PointerLayer`，「命中表」是逐 handler 的 `Contains`
⇒ E1 在这一侧**语义不同**：`ImageQuad.Contains` 本身**不看 `activeSelf`**，
`Label.Contains` / `CardView.Contains` 同样不看 ⇒ 战斗侧**没有 E1 这个缺陷面**（但是有 E4）。
⇒ 战斗侧的 E1 换成：**「HUD 各颗钮 / 面板各颗钮，拿一个【在实绘矩形内、但在命中矩形外】的点去问」** ——
就是 E2。

**③ E2（覆盖）—— 战斗侧唯一可能真违判据的是「硬写 px 矩形」那一族（§2·b 表 · 模型②）**。
建议写一张**手写的命中入口表**（战场没有表 ⇒ 探针必须自带）：

| 名字 | 命中判定（**调生产函数**，别重写） | 实绘矩形来源 | 今天预期 |
|---|---|---|---|
| 设置钮 / 墓园钮 / 聊天钮 / 进攻卡钮 | `q.Contains` | 同一个 quad | **恒等**（结构上不违） |
| 重置镜头钮 | `driver.CameraResetButtonHit(w)`（`public`，`:3117`） | `_cameraResetBtn.WorldW/H` | 🔴 **违**（48.443×45.846 vs 61.846²），**原版正当** |
| 结束回合 | `_endTurnBg.Contains` / `_endTurnLabel.Contains`（`BattleDriver.cs:6795-6796`） | 那两颗自己 | **恒等** |
| 日志面板行 | `logPanel.RowAt(w)`（`BattleLogPanel.cs:403`，**public**） | `_rowBgs[i]` 那颗 quad | **恒等**（但它**比原版宽**：原版只认行内链接，我们认整行底板 —— 已记账，`BattleDriver.cs:5600-5602`） |
| 手牌各张 | `CardInteraction.HitTest`（**private** ⇒ 用 `SimulateHover(world)` 间接问，`CardInteraction.cs:665`） | `CardView.Contains` 用的 `Width/Height`（`Core/CardView.cs:188-189`）+ `transform.localScale` | **恒等**（`InverseTransformPoint` 自带缩放） |
| 场上单位 | `BattleDriver.HitSlot`（`private`，`:12714`）· 或 `SimulateTapUnit(side, slot)`（`:12738`） | `CardView` 同上 | **恒等** |
| 设置面板 4 颗（Resign/Difficulty/AutoZoom/Close） | `SettingsPanel.HitResign/HitDifficulty/HitAutoZoom/HitClose`（都 `public`，`:1296/1303/1312/1180`） | Resign 走 `RectContains(_resignBtn.transform, w, ResignWPx, ResignHPx)` ⇒ **要另外拿到实绘件** | ⚠️ **要量**（Resign 用的是硬写 px；其余三颗走 `Contains` ⇒ 恒等） |
| `ChatPopup` 6 颗台词钮 | `ChatPopupPanel.ButtonAt`（`public`，`:349`） | `_btns[i]` 的 `rect`（同一份） | **恒等**（`Hit` 用的就是 `rect`） |
| 多卡窗「继续」 | `MultiCardDisplay.HitContinue`（`public`，`:236`） | 横条 + 圆钮那几颗 quad | ⚠️ **要量**（硬写 `BarCx/BarW/CircleD`） |
| 我方牌堆 | `BattleDriver.HitMyDeckPile`（`public static`，`:6285`） | 牌堆底板 quad | ⚠️ **要量**（硬写 `DeckPlatePx`） |
| 技能面板 | `SkillPanel.Contains`（`public`，`:277`） | 九宫格那几块（`PanelW/H`） | ⚠️ **要量** |
| 滑块（三根音量 + FPS） | `WfSlider.Contains`（`public`，`:398`） | 轨道 + 手柄 | ⚠️ **要量**（且它**故意**把探出轨道的手柄也算进来 —— `:391-397` 写了判据，别报成缺陷） |

🔴 **表里每一行的「命中判定」必须调【生产函数本身】**（它们多数已经是 `public`）。
**不许在探针里重写一遍算式** —— 那就是「两边一起改回去还全绿」的自证。

**④ 三选一面板 / 棋盘格位：判据不足**（模型③ 没有矩形）—— 见 §7。

### 5·4 文件所有权（两条路，挑一条）

**路 A（推荐）· 宿主内新段**

| 块 | 独占文件 | 自检落点 | 撞车核算 |
|---|---|---|---|
| **H-外壳** | `Editor/ShellScene.cs`（新增 §A964 那一段 + 复用 `Build/Check/QuadPxRect/TmpSpanPx/MakeNavButton`） | `ShellScene.Run` | ⚠️ 与 `可并行任务清单.md` 里 **A833 的「`Editor/MainMenuScene.cs`（或 `ShellScene.cs`）」那个「或」** 有交集 ⇒ **必须先钉死 A833 落到 `MainMenuScene.cs`** |
| **H-战斗** | `Editor/BattleScene.cs`（新增 §A964 那一段） | `BattleScene.Run` | 本批**没有别人**碰它 ✅ |
| **共用件（两个写手都要，⚠️ 必须调度台自己先做或排第一个）** | `Shell/PointerLayer.cs`（加 `internal` 只读口）· `Battle/BattleDriver.cs`（加 `SetHudButtonActiveForTest` + `"cameraReset"` 档） | —— | `BattleDriver.cs` 也**没有别人**在本批碰 |

**路 B（照 `可并行任务清单.md:570` 原文）· 各自新建 `Editor/*.cs`**

| 块 | 独占文件 | 额外要动的 | 代价 |
|---|---|---|---|
| **H-外壳** | 🆕 `Editor/HitProbeShell.cs` | `Editor/ShellScene.cs` 的 `Build` 改 `internal static`（1 行）；`ShellScene.QuadPxRect`（`private`）也要 `internal` 或另写一份（⚠️ **另写一份就是量法第四份**） | 多一次改宿主、多一份量法风险 |
| **H-战斗** | 🆕 `Editor/HitProbeBattle.cs` | 要用 `BattleScene.BuildScene`（`public` ✅）+ **手抄** `BattleScene.Run` 开头那一堆全局态压平（`:281-330`） | **夹具漂移**风险（本工程有先例） |

📌 **不论走哪条路**：`Shell/PointerLayer.cs` 那个只读口**都建议加** —— 否则探针只能抄 `HitBoxPx` 的算式，
而「两处写同一条规则 = 迟早不一致」是本工程 §三 的明文。

### 5·5 产物写哪

- **日志**：`-logFile d:/4/_tmp_view/<名字>.log`（照 `工具/_run_8_checks.sh` 的惯例；`/` 分隔，别用 `\`）。
- **中途 JSON/TSV 明细**（每条违规一行：`节点路径 · 命中矩形 · 实绘矩形 · 四边差值 px · 队列表`）：
  `d:/4/_tmp_view/hitprobe/`（`IconSizeProbe.cs:29` 的 `OutDir = @"d:/4/_tmp_view/iconsize"` 就是这个惯例）。
  🔴 **要不要进 `资料/普查产出_<日期>/` 由调度台定** —— 探针本身**不许写 `资料/`**（那是正本区）。
- **截图**：⚠️ **这类缺陷截不出来**（「点不到」在静态图上和「点得到」长得一样）⇒
  **不靠截图**，靠「差值 px」+ 节点路径。这一点要写进探针头部，免得下一个人拿截图当验收。

---

## 6. 风险与陷阱（会让探针**假绿 / 假红**的东西）

| # | 陷阱 | 后果 | 处置 |
|---|---|---|---|
| 1 | `QuadPxRect`（`ShellScene.cs:141` / `MainMenuScene.cs:10581`）**不乘 `lossyScale`**，而 `PointerLayer.HitBoxPx:883` **乘** | 小屏缩放开关一开（M≠1）两侧分家 ⇒ **假红/假绿** | 量渲染矩形时**自己乘/除 M**；**两态都跑**（出厂 M==1 会把这条完全掩盖） |
| 2 | `PointerLayer.HitBoxPx` 拿 `transform.position` 当**矩形中心** ⇒ 只对 `anchor=(0.5,0.5)` 成立 | 哪天有非中心 anchor 的命中 quad ⇒ 命中区整体偏移半格，**静默** | 加一条**结构断言**：命中 quad 的 anchor 必须是 `(0.5,0.5)` |
| 3 | 九宫格/平铺的**根节点没有 `ImageQuad`**（`CreateNineSlice:422` / `CreateTiled:499`） | 若有 `WindowButton` 挂在这种根上，`HitQuad` 会取到**第一块角块** ⇒ 命中区缩成一块角 | 断言「命中 quad 不是九宫格/平铺的子块」；今天 108 处 `.Nine(` 没这种用法 |
| 4 | 窗**没开**/被压到 `Background` ⇒ `HitQuad` 恒 null（`PointerLayer.cs:899/1093`） | 直接扫全场景 ⇒ **把没开的窗全报成「点不到」**（一片假红） | **逐窗开一次再扫**，一次只开一扇 |
| 5 | 装饰件叠在同一矩形上（`CampaignTab.cs:459-461` 的 `Premium Mark`，`localScale=(2,2,1)`） | 纯几何「谁盖住谁」⇒ **假红** | E2 要么带**归属登记**，要么只扫「中心重合那一族」并**逐条人工裁**（把每条的 diff 打出来） |
| 6 | **原版本来就**「命中比实绘小」：`m_RaycastPadding` 为负（`CameraResetButtonHit`）· `RectMask2D.m_Padding`（`MenuDraw.PaddedClip`）· `FitHeight` 内接（`TutorialOverlay` 跳过钮） | 一开跑**一片红**，而且每条都是「照原版做的」 | **白名单 + 每条写判据**；判据正文要写清「**「命中 ≥ 实绘」这条只在原版 padding ≥ 0 时成立**」 |
| 7 | **参数是运行时按状态改的**（铁律 5·c）：`TransformScalerBySmallScreenUI`(M) · `Clip`/`ClipPad`（视口）· `CardHighlight` 悬浮缩放 · `AttackSelector` 选中 1.3 倍 · `SetHighlightScale` | 同一颗钮在**不同状态**下矩形不同 ⇒ 报告写「0 条」可能只是**这一态**没露 | 报告里**写清跑的是哪一态**；只覆盖一态时**明说** |
| 8 | 批处理里 `Mouse.current == null`、`WorldPointer()` 是**死点**（`BattleDriver.cs:12615` 明说）、`Screen` = 640×480 | 任何「真点一下」的路径都验不了 | 用 `PointerWorldForTest:12622` / `PointerHeldForTest:12619` / `PointerLayer.ButtonAt:935` 这些钉子 |
| 9 | 🔴 **两侧对「关掉」的语义【正好相反】**：外壳侧 `HitQuad` **要求 `activeInHierarchy`** ⇒ 关掉 = 不可命中（对）；战斗侧 `ImageQuad.Contains` **不看 `activeSelf`** ⇒ 关掉 = **照样命中**（E12 那个病） | 把外壳侧的规则套到战斗侧（或反过来）⇒ **假绿** | E1 只在外壳侧做、E4 只在战斗侧做，**别合并** |
| 10 | `AttackSelector.UpdatePointer`（`Battle/AttackSelector.cs:633`）把 `_icons[i].transform.localPosition` 与**世界坐标** `world` 相减 | 只因为 `sceneRoot` 在世界原点才成立（`Editor/BattleScene.cs:17125/17258`）；一旦根有偏移 ⇒ 命中**静默**错 | 探针量它之前**先断前提**「`selector.transform.position == 0`」 |
| 11 | **自证**：探针若抄一份命中算式 / 只读我们自己的常量当期望值 | 实现与检测器**一起改回去还全绿** | 命中一律调**生产函数**（§5·3·B 那张表）；每条配**两态 + 灭自证**断言 |
| 12 | 量「画出来的」时漏掉：`SetTint(0,0,0,0)` 的**透明件**（那是命中区自己，要排除）· `localScale`（`SkillPanel.cs:248`、`Premium Mark`）· **软边切块**（`ImageQuad._softKids`） | 漏量 ⇒ 假绿；把透明命中 quad 当「可见件」⇒ 恒等假绿 | 排除规则写死：`Tint.a < 1e-3` 且 `name == "Hit"` 的**不**算实绘件；软边要量**并集** |
| 13 | 命中表**每次事件现扫**（`AllButtons()` = `FindObjectsByType`）⇒ 多开一扇窗、多一颗探针夹具都会改变结果 | 探针自己建的夹具**会污染**后续断言 | 夹具建完**必须 `DestroyImmediate`**（批处理没有帧循环，`Destroy` 不生效 —— §三 明文） |
| 14 | `WindowButton.Click()` 在 `onClick == null` / `interactable == false` 时**静默不派发**（`PromptPopup.cs:1095-1108`） | 「点中了」≠「有效果」⇒ 别把「命中」当成「能用」 | 探针只判**命中**；「有没有绑动作」是 `PointerLayer.LogHit:379` 那一族的事，别混进这条普查 |

---

## 7. 没查清的部分（⛔ 不拿猜测填空）

1. 🔴 **战斗侧「没有矩形」的那一族，判据怎么写 —— 判据不足。**
   `BoardLayout.TryResolveSlot`（`:376`）· `ArenaSlots.TryResolveSlot`（`:147`）· `AttackSelector.UpdatePointer`（`:616`）
   用的是「最近格/圆」而不是矩形。A964 的判据（「命中矩形 ≥ 实绘矩形」）**对这种模型不成立**。
   **缺的是调度台的一条裁定**：是「外接矩形 ≥ 实绘矩形」还是「圆心距 ≥ 图标内接圆半径」——
   两种写法结论会不同。⛔ 我不替它裁。
2. **`AttackSelector` 的根节点是不是恒在世界原点** —— 今天 `Editor/BattleScene.cs:17258-17260` 建在
   `sceneRoot`（`new GameObject("Battle")`，`：17125`）下 ⇒ 是；但**没有断言**，
   而且**我不知道有没有别的宿主**（只 grep 了 `Editor/BattleScene.cs` 与 `Battle/*.cs` 里带 `position` 的行）。
3. **外壳侧 46 处 `MenuDraw.Hit` 调用点里有多少处 `vis ≠ hr`** —— **一处都没逐处核**。
   （只做了两个抽样：`CampaignTab.cs:481` 的 `r` 与同处 `_win.Rect(node, …, r, …)` 是**同一个 `r`**；
   `PromptPopup.MakeButton:238` 的 `WindowButton` 直接挂在**可见 quad** 上 ⇒ 恒等。）
   ⇒ **这正是探针要产出的东西**，不是这次侦察能回答的。
4. **全仓有没有 `WindowButton` 挂在九宫格/平铺根上** —— 只做了**侧面判断**
   （`grep -B2 "AddComponent<WindowButton>()"` 里没有 `.Nine(`；108 处 `.Nine(` 调用点没穷举每处的宿主）。
   ⇒ **结论是「没找到」，不是「没有」。**
5. **`ShellScene.cs:531 MakeNavButton` 造的夹具会不会污染同一次 `Run` 里后面的断言** ——
   我只读到它建节点、没读它**有没有被清理**。
6. **没有跑 Unity**（红线）⇒ 本文件里所有「实绘 = 61.846²」这类读数都是**静态代码判定**
   （`HudAbs` 的 `w` 只参与算中心 + PNG 头实读 237×237），**没有在 Unity 里量过一次**。
   `QuadPxRect` / `TmpSpanPx` 在批处理下对**这些具体件**的可用性也**没实测**。
7. **`_settingsBtn` 的 `activeSelf` 守卫缺失**（`BattleDriver.cs:2908-2909`）—— 我 grep 了
   `_settingsBtn` 的全部 8 处出现，**没看到任何 SetActive/SetVis 作用在它身上**，
   但**没有穷举**「别的文件会不会通过 `transform`/`gameObject` 关它」。⇒ 记的是「今天不可达」，不是「永远不可达」。
8. **`grep` 覆盖范围**（照红线，把搜过的东西列出来）：
   - 搜过的目录：`d:/4/Unity/MyGame/Assets/CardPresentation/**`（`Battle/` `Shell/` `Hand/` `Board/` `Core/` `Deck/` `Editor/`）·`d:/4/Unity/资料/*.md`。
   - 搜过的关键词：`AddComponent<WindowButton>` · `absorbOnly` · `Absorb` · `class ImageQuad` · `class Label` ·
     `QuadPxRect` · `TmpSpanPx` · `TmpInkCenterPx` · `materialReferenceIndex` · `Contains(world)` ·
     `public bool Hit` · `HitTest` · `HitQuad` · `CollectHits` · `PointerReachable` · `.Nine(` ·
     `HudButtonActiveForTest` · `ChatButtonVisibleForTest` · `A964` · `A940` · `A8` · `E12`。
   - **没搜过**：`d:/2/**`（原版资源）—— 本次只判「我们的实现长什么样」，
     **原版那一侧的判据（`m_RaycastPadding` / `m_Padding` 逐窗实读值）这次一条都没核**，
     探针要用的白名单必须有原版判据，**那是下一步的活**。

