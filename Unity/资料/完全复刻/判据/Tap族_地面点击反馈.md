# Tap 族 · 点战场地面的反馈 —— 完整判据（N12 第 1 轮，2026-10-25）

> **一句话**：原版点地面 = 一个**独立组件每帧自己读鼠标按下沿** → `Camera.main.ScreenPointToRay` →
> `Physics.RaycastNonAlloc(…, 3, LayerMask 0x40000001)` → 取**最近的一条** → 那条碰撞体身上的
> `TapHitMaterialDefinition` 决定**播哪个粒子 prefab + 哪个音效 cue + 冷却多久**。
> 我们这边：**战场里一个 Collider 都没有、代码里一处 `Physics.Raycast` 都没有** ⇒ 点击走到
> `BattleDriver.DrivePlayerTurn` 的空分支就没下文了 —— 这就是「点了没反应」的全部原因。
>
> **数据不是问题**：19 个粒子 prefab **已经全在我们的工程里**（`WarpforgeVFX/Prefabs/`），
> 11 个音效 cue 里 **1 个已在**（`Buff Mana 1`），剩下 10 个 cue / 21 条 clip **在 `assets_full` 里已导出、只差导入**。
> 213 个挂点、25 种（粒子+音效）组合、13 个场的分布**本文件全部列清**。
>
> **反编译不是问题**：`TapParticleController.Update` 在 ilspy 版里确实有 **87 条 `//IL_` 注记**，
> 但 Ghidra 的 `D:/2/tools/decomp_full/TapParticleController__Update.c` **0 条注记、控制流完整** ——
> 「被打烂、只能猜」这个前提**不成立**。本文件里每一个分支都是从 `.c` 的指令流读出来的，不是猜的。

> 🔴 **2026-10-25 · `N13` 复核轮（原写手 `N12` 已收工，本文件归 `N13` 维护；铁律 5）**
> 本轮改了 **4 处**，全部留了更正痕迹：
> 1. 🔴 **补上真正的方法体**：`TapHitMaterialDefinition.ParticleCollisionDefinition.PlayParticle`（**149 行**）
>    —— `TapParticleController.Update` 在 `.c:356` 直接调它。`TryPlayParticles`（42 行）**只是包装层、零调用**
>    （见 §2.3 的更正块 + §3.3 开头）。**这条最要紧**：照包装层写出来的东西"能跑、但不含粒子缓存/锚点/音效"。
> 2. **改归属**：`CheckCollision` 是 **`TapParticleController.CheckCollision`**（`:475`），不是 `TapHitMaterialDefinition.CheckCollision`。
>    同族的第三处：§2.1 原来引用的 `DEC/TapHitMaterialDefinition__CheckCollision.c` **这个文件不存在**。
> 3. **新增「实读 / 外推」一列**（§0 图例 + §0·b 汇总表 + §1.1/§1.1参数/§2.1/§2.3 各表）：凡不是真算出来的（层级/字段序/常量），
>    明确标出来 —— 起因是姊妹判据 `TraitEffectController_钩子层.md` 栽过一次「外推当结论」（`Grid floor` 层级）。
> 4. **修两处表格**：§2.5 的 `19 个粒子 prefab` 行（`ls *.prefab | grep …`）与 `clip wav` 行（`ls | grep -iE "…(A|B|C)"`）
>    里的竖线没转义，会把那两行**拆错列**（`\|` 已补）。
> 抽核结果（通过）：`hits=3` · `collisionMask=1073744897` · `debugCollisions=0` · `priority/status=2/0` ·
> 序列化字段名 4 个逐字对 · `battlesharedresources=45` · `arena1=20` · arena1/arena2 各 1 个控制器。
> 未抽核的条目全部列在 **§0·b**。

> 🔴🔴 **2026-10-25 · 第 2 轮订正（实现方 `N16` 按判据写代码时推翻 2 条；`N13` 复核确认；铁律 5）**
>
> | # | 上一版写的（❌） | 复核后（✅） |
> |---|---|---|
> | **1**（最要紧） | 「**玩家『点棋盘空地』最常撞到的就是它**（`CardLowBoardLimit`）」；T1 验收 = 「点棋盘空地有粒子」 | **不成立**：`CardLowBoardLimit` 在 **layer 11（`BoardCardLimit`）**，射线掩码 `0x40000001` = **layer 0 + 30** ⇒ **打不到**。arena1 的 20 条里 **19 条在 layer 0**（`Cube`/`Collision`/`Barrel*`/`Generator 1·2`/`Fences top`），第 20 条才是棋盘那颗；`emperorschildren`/`spacewolves` 各 1 条、**都是棋盘** ⇒ **这两场整场无反馈**。详见 §2.2 订正块 + §5.3 的 T1 |
> | **2** | 「B-3：`Bounds.get_center/extents` 被当成 `RaycastHit` 的字段」「`hit+0x20`」 | **理由错、结论不变**：`+0x20` 是**托管数组数据起点**、`0x2c` 是元素步长（44 B），不是「`hit+0x20` 字段」。`取最小 distance` 仍成立（同一元素指针喂 `get_collider`、日志 `Distance {d}`、`Bounds` 读 `0..11/12..23` = point/normal）。详见 §3.4 B-3 |
> | **3** | §0·b 里 6 项标「未抽核」 | **`N16` 全核了，全部成立**：213 逐场分布 · 18 个锚点（13 逐场 + 5 shared）· `delay` 六档 191/12/4/3/2/1 · **25 种组合连次数逐行相等** · **30 个 GUID 全解析** · **10 条 Tap cue 的 `clips/pitch/volume/timeToPlayAgain` 现读 10/10 全等** ⇒ 已改标「已抽核」 |
>
> 🔑 **这一轮给出的通用规律（已写进 `判据/_体检_引用与措辞.md`）**：
> **「实现方按判据写代码时，会把判据的错暴露出来」** ⇒ **「实现完回头核判据」这一步不能省**。
> 今天已是**第 7、8 例**（前六例：`setCost` 早退 · `Grid floor` 非直接子件 · `MarkerLightUsed` 走死代码 · `PlayParticle` 从未出现 · `CheckCollision` 归属 · 两个 `.c` 文件名与内容不符）。
> ⚠️ 这两条错**都在「实读」栏里**（我当时以为它们是从资产直读的），说明**「实读」也会错在「读的是哪份资产/哪个偏移」这一层**。

---

## §0 判据链与置信度分级

| 档 | 含义 | 本文件里占比 |
|---|---|---|
| **A** | 从 `.c` 指令流 / 资产序列化字段**直接读出**，可逐条复核 | 绝大多数 |
| **B** | 名字被反编译器解析错，但**按结构/偏移/签名判定**（本文件给出判定过程） | Q3 的 3 处 |
| **C** | 查不到 | 见 §6 |

🆕 **2026-10-25（`N13` 复核，铁律 5）：本文件的关键表新增「实读 / 外推」一列。** 三档口径：

| 标注 | 含义 | 动手前要不要复核 |
|---|---|---|
| **实读** | 真的从 `.c` 指令流 / 资产字段 / 全量 grep 读出来的（§7 有可复现命令） | 不用；直接照抄 |
| **实读 + 外推** | 控制流是实读，但其中**某个名字/类型**是靠偏移或调用形状映回去的 | **复核那一个点**（本文已给怎么核） |
| **外推** | 没真算，按「看着像」顺手推的（层级 / 字段序 / 常量取值） | 🔴 **必须先核，⛔ 不许照抄** |

> ⚠️ 为什么单独加这一列：同一份判据里，**「实读」和「外推」混排、长得一模一样** ——
> 上一版 `TraitEffectController_钩子层.md` 就栽在这里（`Grid floor` 的层级是**外推**的，
> 照抄写 `transform.Find("Grid floor")` ⇒ **恒 null 且静默**，见那份文件的订正表 F-A）。
> ⇒ 凡是标了「外推 / 实读 + 外推」的行，实现代理**先复核再写**，别把外推当结论。

### §0·b 其余「外推 / 未抽核」点一览（`N13` 2026-10-25）

> 这张表只收**不是纯实读**的条目；没列进来的 = `N12` 从 `.c`/资产直读且 `N13` 没发现疑点。
> 「N13 抽核」= 复核代理这轮**真去看过**的；「未抽核」= **判据本身可能没问题，但这轮没人再验一遍**。

| 位置 | 条目 | 档 | 抽核结果 |
|---|---|---|---|
| §1.1 第 8 步 | `FUN_1805e38d0` ⇒ `Camera.main` | **实读 + 外推（函数名）** | 已标；用的地方是「有个相机」这件事，风险低 |
| §1.1 第 12 步 | 「挑 `hit.distance` 最小」 | **实读 + 外推（B-3）** | ✅ **已订正理由**（`N16` 推翻、`N13` 复核）：`+0x20` 是**数组数据起点**、`0x2c` 是元素步长，不是 `hit+0x20` 字段；**结论不变**（见 §3.4 B-3） |
| §2.1 字段表 | 偏移 → 字段名（8 个槽） | **实读（偏移）+ 外推（名字）** | 已标（同节更正块） |
| §2.2 | 总数 **213** / 每场分布（20·27·14·13·27·15·6·1·9·11·11·1·11） | 实读（N12 全量） | ✅ **已抽核（`N16`）**：213 逐场吻合（`N13` 另抽 `battlesharedresources`=45 / `arena1`=20 也一致） |
| §2.2 | 「25 种（粒子,音效）组合」及那张 25 行统计表 | 实读（N12 全量） | ✅ **已抽核（`N16`）**：**连次数逐行相等** |
| §2.2 | 「13 场全部有且**仅有 1 条**棋盘地面 `CardLowBoardLimit`」 | 实读（N12 全量） | ✅ 数量已抽核（每场 1 条）；🔴 **但「它是玩家最常撞到的」这个结论被推翻** ⇒ 见 §2.2 订正块（layer 11 不在掩码内） |
| §2.2 | `delayTimeToPlayAgain` 六档计数（191/12/4/3/2/1）· `targetAnchor` 非空 **18** 条 | 实读（N12 全量） | ✅ **已抽核（`N16`）**：六档吻合；18 个锚点也吻合（13 逐场 + 5 shared） |
| §2.4 | cue 参数表（pitch / volume / cooldown / clipList 共 11 行） | 实读（N12 读 `AudioCue` 资产） | ✅ **已抽核（`N16`）**：**10 条 Tap cue 的 `clips/pitch/volume/timeToPlayAgain` 从真 bundle 现读，10/10 全等**；30 个 GUID 全解析 |
| §2.5 | 「22 件 Tap prefab（需要 19 + 多 3）」·「10 条 cue 缺」·「21 条 wav 缺 20」 | 实读（`ls` / `grep`） | **未抽核**（本轮没人再数一遍） |
| §3.3 | 「`cacheParticles` 是 **static** Dictionary」 | **实读（结构）/ 建议复核** | 未核。判据：`.c` 里若真是 `static`，它应以 `DAT_xxx` 静态槽出现，而不是 `this+偏移` |
| §3.3 | 常量 `0.1f`（法线预偏移）· `float.MaxValue`（射线 maxDistance） | 实读（`read_literal.py` 直读 `0x1834b2dc4` / `0x1834b2d94`） | 已标；`0.1` 与 §7-④ 可复现 |
| §3.4 B-1/B-2/B-3 | 移动端两支的 Touch 属性 · `Bounds`↔`RaycastHit` 前 24 字节同构 · distance getter | 已标 B 档 | **B-3 理由已订正**；B-1（桌面不走）仍开着 |
| 🔴 §2.2 / §5.3 | 「点棋盘空地有反馈」 | 曾被当成**实读结论**（没标档） | 🔴 **已推翻并订正**：`CardLowBoardLimit` 在 **layer 11**，不在掩码 `0x40000001` 内 ⇒ **点棋盘空地原版没有反馈**；能打中的是 layer 0 的道具（木桶/栅栏/发电机/隐形 `Collision`/`Cube`）；`emperorschildren` / `spacewolves` 两场**整场无地面反馈** |

出处缩写：
- `ILSPY` = `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`（≈97.7% 真方法体，**带 `//IL_` 注记**）
- `DEC` = `D:/2/tools/decomp_full/`（Ghidra `.c`，**本族 0 条 `IL_` 注记**）
- `ASSETS` = `D:/2/新解包资源/assets_full/`（84 bundle，**当前 build**）
- `BUNDLE` = `D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`（真束，读 PathID↔名字只能靠它）
- `OUR` = `D:/4/Unity/MyGame/Assets/CardPresentation/`

⛔ **本文件没有一处拿「我们自己的常量」当判据**；「我们现状」那几栏一律指到 `文件:行` 或资产路径。

---

## §1 Q1 —— 谁接收点击：原版的路由，与我们断在哪

### §1.1 原版：**不是 uGUI、不是 `OnMouseDown`、不是 EventTrigger** —— 是一个组件自己在 `Update` 里射线

**入口**：`TapParticleController.Update`（`DEC/TapParticleController__Update.c` 全文 385 行，0 条注记）。
它自己每帧走一遍下面这套（**顺序即代码顺序**，全部是 `DEC` 的行号）：

| # | 判据 | 出处 | 实读 / 外推 |
|---|---|---|---|
| 1 | `battleManager == null` ⇒ 什么都不做 | `.c:74` | 实读 |
| 2 | `battleManager.battleStage(+0x294) != 50` ⇒ return（`50 = BattleStage.MatchProper`） | `.c:75`；`ILSPY/BattleStage.cs` | 实读 |
| 3 | `WindowsManager.Instance == null` ⇒ 异常；**`instance.currentWindow != null` ⇒ return（任何窗开着就不收）** | `.c:76-83` | 实读（`currentWindow` 覆盖哪些窗 = 查不到，见 §6-2） |
| 4 | `EventSystem.current == null` ⇒ 不做事 | `.c:87-92` | 实读 |
| 5 | **移动端分支**：`Application.isMobilePlatform && Input.touchCount > 0` 时取 `GetTouch(0)` 决定 `pointerId`（桌面恒 `-1`） | `.c:97-123`（细节见 §1.3 的 B 档） | 条件实读；**两支具体取 Touch 哪个属性 = 外推（B-1）**，桌面不走 |
| 6 | `EventSystem.current.IsPointerOverGameObject(pointerId)` ⇒ return（**指针压在 UI 上就不算点地面**） | `.c:129-136` | 实读 |
| 7 | **`!Input.GetMouseButtonDown(0)` ⇒ return（按下沿，不是松手）** | `.c:130-133` | 实读 |
| 8 | `Camera.main`（`FUN_1805e38d0`）—— 取不到就整段不做事 | `.c:140-144` | 实读（`FUN_1805e38d0` → `Camera.main` 是**按调用形状认定**的，与 §3.4 同族；旁证：本文件 §1.3-2 说我们工程里两处都在用 `Camera.main`） |
| 9 | `ray = cam.ScreenPointToRay(Input.mousePosition)` | `.c:143-148` | 实读 |
| 10 | **`Physics.RaycastNonAlloc(ray, hits, float.MaxValue, (int)collisionMask)`** | `.c:162-163` | 实读 |
| 11 | 0 条命中 ⇒ return | `.c:172-174` | 实读 |
| 12 | 遍历命中，**取 `hit.distance` 最小的一条**（`RaycastNonAlloc` **不排序**，所以必须自己挑） | `.c:175-278`（元素寻址见 `:182` `i*0x2c` / `:267-274`） | **实读 + 外推（B-3，2026-10-25 已订正理由）**：「挑最小」这个**控制流**是实读；比较用的那个值是把**数组元素首地址**喂给一个被误名的 getter（`MinMaxCurve.get_constantMax`）—— 🔴 **`+0x20` 是托管数组的数据起点、`0x2c` 是元素步长（44 字节），⛔ 不是「`hit+0x20` 这个字段」**。「它 = `RaycastHit.distance`」仍成立（同一指针喂 `get_collider`；日志格式串是 `Distance {d}`） |
| 13 | 那一条的 `collider.GetComponent<TapHitMaterialDefinition>()`，**拿不到 ⇒ return（不会顺延到下一条）** | `.c:312-321` | 实读 |
| 14 | 取 `hit.point` / `hit.normal`，走冷却闸 → `PlayParticle(point, normal, def.targetAnchor)` | `.c:322-361` | 实读（偏移 `def+0x30` = `targetAnchor`）；**行为体见 §3.3（`ParticleCollisionDefinition.PlayParticle`）** |

**关键参数（全是序列化真值，不是我们挑的）**：

| 项 | 值 | 出处 | 实读 / 外推（`N13` 2026-10-25 抽核） |
|---|---|---|---|
| `hits` 数组长度 | **3**（`new RaycastHit[3]`） | `ILSPY/TapParticleController.cs:512`（构造函数） | 实读 —— `N13` 复核同一行 ✅ |
| `collisionMask.m_Bits` | **`1073744897` = `0x40000001`**（layer 0 = Default **+** layer 30） | 13 个场景各一份，全同：`ASSETS/bundle_scenes_scenes_battlearena*/MonoBehaviour/MonoBehaviour_*.json` 的 `collisionMask` | 实读 —— `N13` 抽 `arena1/MonoBehaviour_4049.json` 现读 = `1073744897` ✅ |
| `debugCollisions` | **0**（全 13 场） | 同上 | 实读 —— 同上现读 = `0` ✅ |
| `priority` / `status` | **2**（`InitializationPriority.UI`）/ `0` | 同上；`ILSPY/InitializationPriority.cs` | 实读 —— 同上现读 = `2` / `0` ✅ |
| 控制器挂在哪 | `BattlePrefab/BattleBoardElements/TapParticleController`（**空物体、只挂脚本、localPos `(-100,0,0)`**） | UnityPy 读 `BUNDLE/scenes_scenes_battlearena1.bundle` GO pathID 102 + Transform 链；`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:840` 同位置印证 | 实读（两处独立印证）；⚠️ `localPos (-100,0,0)` 那三个数 `N13` **没抽核** ⇒ 用前自己看一眼 |
| 每场几件 | **13 场各 1 件**（共 13） | `ASSETS` 全库搜 MonoScript PathID `6117390686044107195` | 实读 —— `N13` 抽 `arena1` / `arena2` 各 **1** ✅（其余 11 场未抽） |

⚠️ **`Initialize()` 由 `InitializableBehaviour.Start()` → `SceneInitializer.QueueForInitialization(this)` 驱动**
（`ILSPY/InitializableBehaviour.cs`），`Awake()` 先把 `enabled=false` 关掉，`Initialize()` 再用
`enabled = (matchType != 100)` 打开 —— 所以**桌面正常对局它是开的**。

### §1.2 我们现状：点击走到哪一步断了

| 环节 | 原版 | 我们 | 出处 |
|---|---|---|---|
| 有没有 Collider | 213 个 `TapHitMaterialDefinition` 各挂在一颗有碰撞体的物件上 | **战场里 0 个 Collider** | `OUR` 全仓 `grep -n "Collider"` 运行时代码只剩注释；`Editor/` 里也没有 `AddComponent<…Collider>` |
| 有没有射线 | `Physics.RaycastNonAlloc` | **0 处**（`grep "Physics.Raycast"` 的 4 条命中全是注释） | `Battle/BattleDriver.cs:16023` · `:17515` · `:17552` · `Editor/BattleScene.cs:36099` |
| 指针→世界 | `Camera.ScreenPointToRay` + 真碰撞 | `LayoutSpace.ScreenToWorld` 投到 **z=0 平面**（`Core/LayoutSpace.cs:147-159`），只当命中表的输入 | `Battle/BattleDriver.cs:31349-31390`（`WorldPointer`） |
| 点击边沿 | `Input.GetMouseButtonDown(0)` | `_downEdge`（`PollInputEdges` 打的上升沿，语义相同） | `Battle/BattleDriver.cs:31410-31417` |
| 点空处发生什么 | 播反馈 | **什么也不发生**：`Update` 里所有模态/钮分支都 `return` 掉之后，`DrivePlayerTurn()` 只处理选择器 / 单位格 / 手牌拖拽 | `Battle/BattleDriver.cs:13607`（Update）→ `:14194`（`DrivePlayerTurn`） |
| 「什么东西能被点」的清单（我们自己的） | —— | `TutorialClickHitsSomething` = 3 颗 HUD 钮 + 结束回合 + 牌堆 + 两侧单位格 + 手牌 | `Battle/BattleDriver.cs:~17560`（注释 §17515 明写「原版 = `!IsPointerOverGameObject` + `Physics.RaycastNonAlloc == 0`；**我们 = 这张命中表**」） |
| UI 闸 | `EventSystem.IsPointerOverGameObject` | **等价物已经有了**：`BattleEventSystem.Instance` + `PointerOverUi()` | `Battle/BattleEventSystem.cs:121`（`Ensure`）· `Battle/BattleDriver.cs:8081-8086` |

⇒ **断点只有一处**：**没有碰撞体 + 没有射线**。点击本身（边沿、屏幕坐标、UI 闸、世界换算）我们全有。

### §1.3 差分要点（下一批别踩）

1. **不要复用 `BattleDriver._downEdge` 去驱动 Tap**。它是「谁先取谁得」的一次性边沿
   （`Battle/BattleDriver.cs:13845-13856` 的注释：`ClickedThisFrame()` 用掉就没了，`RequestTutorialSkip` 就是
   为了不吃掉它才直接读 `_downEdge`）。原版的 Tap 是**另一个组件自己读 `Input`**，与任何人都不共享状态。
   ⇒ 新组件自己读 `Mouse.current.leftButton.wasPressedThisFrame`（本工程只有新输入系统，
   `ProjectSettings.asset:932 activeInputHandler: 1`，见 `Battle/TouchInputManager.cs` 文件头 A 段）。
2. **`Camera.main` vs 我们的 `BattleDriver.cam`**：原版是 `Camera.main`。本工程战场相机存在且是**透视**
   （`Editor/BattleScene.cs:44992` 断言 `!bcam.orthographic`），`Camera.main` 也可用
   （`Battle/BattleDriver.cs:4810`、`Battle/BattlePostFx.cs:253` 都在用）。**照原版取 `Camera.main`**，
   取不到再退 `BattleDriver.cam` 并出声（⛔ 不要静默）。
3. **`collisionMask = 0x40000001` 必须原样用**（layer 0 + layer 30）。我们建的碰撞体要落在这两层之一。
4. **最近一条优先、且不写回退**：第 13 步拿不到组件就 `return` —— 不顺延到第二近的那条。这是原版的真实行为，
   ⛔ 别「顺手修好」。

---

## §2 Q2 —— `TapHitMaterialDefinition` 是什么

### §2.1 类结构（`ILSPY/TapHitMaterialDefinition.cs` + `DEC/TapHitMaterialDefinition.*.c`）

```
TapHitMaterialDefinition : MonoBehaviour
├─ particleDefinition : ParticleCollisionDefinition   (+0x20，嵌套类)
│   ├─ particles        : AssetReferenceTyped<GameObject>   (+0x10)   ← 粒子 prefab
│   ├─ audio            : AssetReferenceTyped<AudioCue>     (+0x18)   ← 音效 cue
│   ├─ instancedParticle: ParticleSystem                    (+0x20)   🔴 本 build **从不读写**（死字段）
│   └─ instanceAudioCue : AudioCue                          (+0x28)   ← 运行时缓存
├─ delayTimeToPlayAgain : float (+0x28)   ← 两次播放的最小间隔（秒），**0 = 不限制**
├─ useTargetAnchor      : bool  (+0x2C)   🔴 本 build **从不读**（全反编译只有声明处一处，见 §2.3）
├─ targetAnchor         : Transform (+0x30) ← 非空 ⇒ 粒子挂到这个锚点上（本地零位），空 ⇒ 世界点 + 法线
└─ remainingNotPlayTime : float (+0x38)   ← 冷却计时（`Update` 每帧减 dt，≤0 就 `enabled=false`）
```

字段偏移**实读**自 `DEC/TapHitMaterialDefinition__TryPlayParticles.c`
（`+0x28` = `delayTimeToPlayAgain`、`+0x38` = `remainingNotPlayTime`、`+0x20` = `particleDefinition`、`+0x30` = `targetAnchor`）
与 `DEC/TapHitMaterialDefinition.ParticleCollisionDefinition__PlayParticle.c`（粒子/音效那几个槽）。

> ⚠️ **2026-10-25 就地更正（`N13` 复核；铁律 5）**：上一版这里写的是
> 「`DEC/TapHitMaterialDefinition__TryPlayParticles.c` / **`CheckCollision.c`**」——
> **`TapHitMaterialDefinition__CheckCollision.c` 这个文件根本不存在**
> （`ls D:/2/tools/decomp_full/ | grep TapHitMaterialDefinition` 只有 `__.cctor/__.ctor/__Awake/__OnDestroy/__TryPlayParticles/__Update` + 嵌套类三个），
> `CheckCollision` 是 **`TapParticleController.CheckCollision`**（同 §2.3 ① 那处错的同一个根因）。
>
> 🔴 **本节的「实读 / 外推」要分开看**：
> · **偏移本身 = 实读**（`.c` 里 `*(float *)(x + 0x28)` 这类是直接读出来的）；
> · **「这个偏移 = 哪个字段名」= 外推**（靠 IL2CPP 的字段序与 ILSpy 的声明序**逐格吻合**反推）。
> ⇒ 动手前若某个偏移要落成代码，**先在同一方法里看它被怎么用**（读？写？和谁比较？），再决定名字；对不上就停手报出来。

**序列化字段名（写进 manifest / prefab 就用这套名字，已在资产里逐字确认）**：

```json
"particleDefinition": { "particles": {"m_AssetGUID":"…"}, "audio": {"m_AssetGUID":"…"} },
"delayTimeToPlayAgain": 0.2,
"useTargetAnchor": 0,
"targetAnchor": { "m_FileID": 0, "m_PathID": 0 }
```
（样例：`ASSETS/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4014.json`）

### §2.2 213 个实例：分布 + 组合（**这就是全部数据**）

**总数 = 213**（`ASSETS` 全库按 `m_Script.m_PathID = 5926791653216923474` 逐文件核）。

| bundle | 数量 | 说明 |
|---|---|---|
| `battlesharedresources` | **45** | 共享 prefab 里的（`BattlePrefab/…` 那一棵树，含每场都有的 `CardLowBoardLimit`） |
| `scenes_scenes_battlearena2` | 27 | |
| `scenes_scenes_battlearenaastramilitarum` | 27 | |
| `scenes_scenes_battlearena1` | 20 | |
| `scenes_scenes_battlearenablacklegion` | 15 | |
| `scenes_scenes_battlearena3` | 14 | |
| `scenes_scenes_battlearenaaeldari` | 13 | |
| `scenes_scenes_battlearenaleviathan` | 11 | |
| `scenes_scenes_battlearenasororitas` | 11 | |
| `scenes_scenes_battlearenatauviorla` | 11 | |
| `scenes_scenes_battlearenagenestealers` | 9 | |
| `scenes_scenes_battlearenadarkangels` | 6 | |
| `battleprefabs_vfxandmisc` | 2 | 两个 prefab 资产本身 |
| `scenes_scenes_battlearenaemperorschildren` | 1 | 只有棋盘那一颗 —— 🔴 **且它在 layer 11（不在掩码）⇒ 这场实际【无】地面反馈** |
| `scenes_scenes_battlearenaspacewolves` | 1 | 只有棋盘那一颗 —— 🔴 **同上，这场实际【无】地面反馈** |
| **合计** | **213** | |

**去重后只有 25 种（粒子, 音效）组合**，且 **19 个粒子 prefab / 11 个音效 cue 全部可解析成真名**：

| 次数 | 粒子 prefab | 音效 cue |
|---|---|---|
| 56 | Tap Rock | Tap Hit Rock |
| 34 | Tap Hit Metal | Tap Hit Metal Heavy |
| 23 | Tap Hit Metal Small | Tap Hit Metal Light |
| 20 | Tap Hit Metal Red | Tap Hit Metal Heavy |
| 14 | Default Tap Floor | Tap Hit General |
| 10 | Tap squishy | Tap Hit Squishy |
| 7 | Tap Rock | Tap Hit General |
| 7 | Tap Hit Metal Small | Tap Hit Metal Heavy |
| 5 | Tap Hit Metal Small | Tap Hit General |
| 5 | Tap Plasma generator | Tap Hit Spark |
| 4 | Tap Firepit 2 | Tap Hit Fire |
| 4 | Tap Floor Grille | Tap Hit Fire |
| 3 | Tap Webway Portal | Tap Hit Spark |
| 3 | Tap Hit Metal | Tap Hit Metal Light |
| 2 | Tap Toxic Sludge | Tap Hit Water |
| 2 | Tap Monolith Right Glow | Tap Monolith |
| 2 | Tap Toxic Sludge green | Tap Hit Water |
| 2 | Tap Monolith Left Glow | Tap Monolith |
| 2 | Tap Rock | **Buff Mana 1**（⚠️ 不是 Tap 族 cue，见下） |
| 2 | Tap Fire Eldar | Tap Hit Fire |
| 2 | Tap Firepit | Tap Hit Fire |
| 1 | Generic Tap Material | Tap Hit General |
| 1 | Tap Squig | Tap Squig |
| 1 | Tap Red Glow Eye | Tap Monolith |
| 1 | Tap Rock | Tap Hit Metal Light |

**字段取值分布（213 条全量）**：

- `delayTimeToPlayAgain`：`0.0`×**191** · `0.2`×**12** · `0.5`×**4** · `1.0`×**3** · `0.1`×**2** · **`60.0`×1**（arena2 的 `Tap collisions/Cube` → `Tap Squig`，一分钟只能响一次）。
- `targetAnchor`：**非空 18 条**（`useTargetAnchor=1` 恰是同样那 16+2 条，两字段一一对应）。
  ⚠️ `useTargetAnchor` 在**运行时无读取点**（§2.3），真正生效的是 **`targetAnchor` 是否为 null**。
- `targetAnchor` 非空的那些：黑军团 7 条（Firepit / Floor Grille / Red Glow Eye）、arena2 的 Squig 1 条、arena3 的 Monolith 2 条、arena1/2/3 共享的 Firepit 2、Monolith 左右。

**每场一粒「棋盘地面」**：**13 个场全部有且仅有 1 条**
`Default Tap Floor + Tap Hit General`，挂在
`BattlePrefab/BattleBoardElements/Colliders/CardBoardLimits/CardLowBoardLimit`
（`delayTimeToPlayAgain = 0`，无锚点）。

> 🔴🔴 **2026-10-25 就地订正（`N16` 实现时推翻判据，`N13` 复核确认；铁律 5）—— 上一版这一段的结论是错的：**
>
> **上一版写**：「⇒ **玩家「点棋盘空地」最常撞到的就是它**；这也是 §7 建议的第一件可验收产物」。**不成立。**
>
> **硬证据（三项互相印证）**：
> 1. `CardLowBoardLimit` 的 `m_Layer = **11**`（`ASSETS/bundle_scenes_scenes_battlearena1/GameObject/CardLowBoardLimit.json` 现读）；
>    `globalgamemanagers/TagManager/TagManager_3.json` 的 `layers[11] = 'BoardCardLimit'`、`layers[30] = 'GroundCollider'`、`layers[5]='UI'`。
> 2. 射线掩码 `0x40000001` = **bit0（`Default`）+ bit30（`GroundCollider`）** ⇒ **layer 11 不在掩码里**。
> 3. 我按「bundle 内 GO → Transform → m_GameObject」把 **arena1 的 20 条逐条解析**了一遍：
>    **19 条在 layer 0**（名字是 `Cube` / `Collision` / `Generator 1·2` / `Barrels Right` / `Barrel Left` / `Barrel Right 2` / `Barrels Left2` / `Fences top`），
>    **第 20 条就是 `CardLowBoardLimit`（layer 11）**。
>    另两场：**`battlearenaemperorschildren` / `battlearenaspacewolves` 各只有 1 条，且都是 `CardLowBoardLimit`** ⇒ **这两场 100% 打不中**。
>
> ⇒ **正确结论**：
> · **点棋盘正中央/空地，原版【没有】任何反馈**（那一条永远射线不到）；
> · 能触发的是**场景道具**：木桶 / 栅栏 / 发电机 / 隐形 `Collision`/`Cube` 盒；
> · **`battlearenaemperorschildren` 与 `battlearenaspacewolves` 这两场整张图都没有任何地面反馈**（不是我们漏了，是原版就这样）。
>
> ⚠️ **这条错的杀伤力**：若照上一版做 T1，验收会变成「点棋盘空地看着没反应 ⇒ 以为没做对」，然后去**给 board limit 补 layer / 把掩码改大** —— 那是**改坏原版行为**。

### §2.3 两个死字段 + 三个「零调用」方法（⚠️ 别照抄成实现）

| 字段 / 方法 | 结论 | 怎么证的 | 实读 / 外推 |
|---|---|---|---|
| `TapHitMaterialDefinition.useTargetAnchor` | **本 build 从不读** | 全量 ilspy `.cs` 里 `useTargetAnchor` 只有**声明**一处（`grep -rn useTargetAnchor`）；`.c` 里 `PlayParticle` 只看 `param_4(is null)`，即实参 `targetAnchor`；16 个 `use=1` 的实例 `targetAnchor` **全部非空** ⇒ 两字段在数据上等价 | 实读（口径限定：ILSpy 的**读点**保名字 ⇒ 「不读」可信；CLAUDE.md 那条「写点会丢名字」不影响本条） |
| `ParticleCollisionDefinition.instancedParticle` | **本 build 从不读写** | `DEC/TapHitMaterialDefinition.ParticleCollisionDefinition__PlayParticle.c` 里 `param_1+0x20` **零出现**（用的是局部 `local_res8` 存缓存实例） | 实读（偏移） |
| `ParticleCollisionDefinition.IsValid()` | **零调用点** | `grep -rn IsValid` 全量 ilspy：只在定义处；两个调用方走的是**内联的** `particles.RuntimeKeyIsValid() && audio.RuntimeKeyIsValid()` | 实读（全量 grep） |
| `TapHitMaterialDefinition.TryPlayParticles()` | **零外部调用** —— 它只是 **42 行的包装层**（冷却门 + 两个 `RuntimeKeyIsValid` + 转调 `PlayParticle` + `enabled=true` + 重置冷却） | `grep -rln TryPlayParticles` 全量 `.c` / `.cs` 只有定义处；`DEC/TapHitMaterialDefinition__TryPlayParticles.c` 全文 42 行 | 实读 |
| `TapParticleController.CheckCollision()` | **零外部调用**（私有 + 死方法，逻辑已内联进 `Update`） | `TapParticleController.cs:475` 定义；`grep -rn TapParticleController__CheckCollision` 全量 `.c` **0 个调用点** | 实读 |
| 🔴 **`TapHitMaterialDefinition.ParticleCollisionDefinition.PlayParticle()`** | **这才是真正被执行的行为体（149 行）**：缓存粒子实例 / 锚点 vs 世界点 / 音效 —— 全在里面 | `DEC/TapParticleController__Update.c:356` 直接调它；`DEC/TapHitMaterialDefinition__TryPlayParticles.c:32` 也调它；方法体 `DEC/TapHitMaterialDefinition.ParticleCollisionDefinition__PlayParticle.c`（149 行） | 实读（两处调用点 + 方法体） |

> 🔴🔴 **2026-10-25 就地更正（`N13` 复核、原写手 `N12` 已收工；铁律 5）—— 上一版这里有两处会让人抄错：**
>
> **① 归属写错**：上一版把 `CheckCollision()` 写成 `TapHitMaterialDefinition.CheckCollision()`。
> **实际它是 `TapParticleController.CheckCollision`**（`ILSPY/TapParticleController.cs:475`，私有）。
> ⚠️ 为什么容易搞混：`TapHitMaterialDefinition` 与 `TapParticleController` 在同一个 `Update` 流程里前后脚出现
> （一个被射线找到、一个负责射线），文件又都在 `Tap*` 前缀下 ⇒ 按名字配对就会配错。
> ⇒ 本文件里凡是 `CheckCollision`，**一律指 `TapParticleController` 那个**。
>
> **② 漏掉了真正的方法——这条最要紧**：上一版只写了「`TryPlayParticles` / `CheckCollision` 零调用 ⇒
> 真正驱动反馈的只有 `TapParticleController.Update`」。**结论对，但少了中间那一跳**：
> `Update` 不是「自己把粒子生出来」，它在 `:356` 调 **`TapHitMaterialDefinition.ParticleCollisionDefinition.PlayParticle(...)`**
> —— 那个 **149 行**的方法才是行为体。
> 🔴 **为什么这条一定会让人抄错**：`TryPlayParticles`（**42 行**）和 `Update` 里那段是**同一套逻辑的两份实现**
> （冷却门 → `ReadOnly` 校验 → `PlayParticle` → `enabled=true` → 重置 `remainingNotPlayTime`）。
> 「零调用」那行会引导实现者去读 `TryPlayParticles`，它**读起来完全自洽、也能跑**，
> 但它是**包装层** —— 抄它 = 抄了一份「调用别人的门」，
> 而**粒子实例怎么缓存、锚点怎么用、音效怎么播**（= 玩家真正看到/听到的东西）**一行都没抄到**。
> ⇒ 实现顺序必须是：**先读 `PlayParticle`（149 行）→ 再读 `Update` 那段（:322-361）→ `TryPlayParticles` 只用来交叉验证「门的顺序」。**
> ⛔ 别再照「零调用」那一行去 `TryPlayParticles` 里找实现。

### §2.4 数据可解性：**全解得出，没有卡点**

| 跳 | 怎么解 | 结果 |
|---|---|---|
| `particles.m_AssetGUID` → 资产 | `python -I -X utf8 d:/4/Unity/工具/_guid_resolve_probe.py g <guid>`（二进制 `catalog.bin`，与本地束同 build） | **19/19 命中**，全部在 `battleprefabs_vfxandmisc_assets_all.bundle`，名字全部 `Tap *` |
| `audio.m_AssetGUID` → cue | 同上 | **11/11 命中**，全部在 `soundcollection_assets_all.bundle`，10 条 `Tap *` + `Buff Mana 1` |
| cue → `clipList`（PathID） → clip 名 | **必须读真束**（解包目录里 `AudioClip/` 只按名字存、没有 PathID↔名映射）；用 UnityPy 读 `BUNDLE/soundcollection_assets_all.bundle` | **21/21 命中**：`Hit Rock 1-3` · `Hit Fire 1-3` · `Hit Floor Generic 1-2` · `Hit Metal Light 1-2` · `Hit Heavy Metal 1-2` · `Hit Spark 1-3` · `Hit Squishy 1-2` · `Hit Water 1-2` · `Hit Monolith` · `Squig Growl 1` |
| 这些 wav 在不在本地 | `ASSETS/bundle_soundcollection_assets_all/AudioClip/` | **20/20 已在**（`Shuriken Catapult.wav` 那条是噪音，不算） |

**cue 的参数（照抄进表就是原版值）**：

| cue | pitch | volume | cooldown | clips |
|---|---|---|---|---|
| Tap Hit Rock | 0.9–1.1 | 0.2 | 0.1 | Hit Rock 1/2/3 |
| Tap Hit Metal Heavy | 0.95–1.05 | 0.2–0.3 | 0.1 | Hit Heavy Metal 1/2 |
| Tap Hit Metal Light | 0.95–1.05 | 0.5–0.7 | 0.1 | Hit Metal Light 1/2 |
| Tap Hit General | 0.9–1.1 | 0.2 | 0.1 | Hit Floor Generic 1/2 |
| Tap Hit Fire | 0.9–1.1 | 0.2 | 0.1 | Hit Fire 1/2/3 |
| Tap Hit Spark | 0.9–1.1 | 0.2 | 0.1 | Hit Spark 1/2/3 |
| Tap Hit Squishy | 0.9–1.1 | 0.2 | 0.1 | Hit Squishy 1/2 |
| Tap Hit Water | 0.9–1.1 | 0.2 | 0.1 | Hit Water 1/2 |
| Tap Monolith | 0.95–1.05 | 0.1 | 0.1 | Hit Monolith |
| Tap Squig | 0.9–1.1 | 0.3 | 0.1 | Squig Growl 1 |
| **Buff Mana 1** | 1.0 | 1.0 | 0.5 | Buff_Mana |

⚠️ **cue 名 ≠ clip 名**（`Tap Monolith` → `Hit Monolith`；`Tap Squig` → `Squig Growl 1`）⇒ ⛔ 别拿 cue 名去 `Resources/` 找文件。

### §2.5 我们的现状：**粒子全在，音效差 10 条 cue**

| 资产 | 我们有什么 | 出处 |
|---|---|---|
| 19 个粒子 prefab | **全部已在**（`Prefabs/` 下共 **22 件** Tap 相关 prefab = 需要的 19 + 多出的 `Tap Blue Glow` / `Tap Blue Glow Blink` / `Tap Green Glow`） | `Assets/WarpforgeVFX/Prefabs/`（`ls *.prefab \| grep -i tap` = 22） |
| 粒子 prefab 结构 | 根 GameObject 名 = 原版名，**自带 ParticleSystem**；`playOnAwake: 0` · `looping: 0`（三颗都核过） | `grep -n "playOnAwake\|looping" "Assets/WarpforgeVFX/Prefabs/Tap Rock.prefab"` = `:51,53` `:4960,4962` `:10233,10235` |
| 音效 cue 表 | **10 条缺**；`Buff Mana 1` **已有**（cue + `Buff_Mana.wav` 都在） | `Resources/animfx_sounds.json` 里 `"name":"Buff Mana 1"`；cue 表缺的 10 条 = §2.4 那张表除最后一行 |
| clip wav | **21 条中 20 条缺**（`Buff_Mana.wav` 已有） | `Resources/Art/audio/sfx/` 里 `ls \| grep -iE "^Hit (Rock\|Fire\|Floor\|Metal\|Heavy\|Spark\|Squishy\|Water\|Monolith)"` = **0 条** |
| 导入工具 | `工具/import_original_sfx.py`（**有「人手声明」口**，正是给「三条自动路径都推不出来的 cue」用的）· `工具/_sfx_meta.py`（补 `.meta`） | 两个脚本的文件头 |
| 播放口 | `WarpforgeVFX.WFSoundPlayer.Play(WFSoundCue cue, Vector3 worldPos, bool is2d)` → 随机 clip + 区间 pitch/volume → `PlayOneShot` | `WarpforgeVFX/Runtime/WFSoundPlayer.cs:97-118` |

⚠️ **`WFSoundPlayer.Play` 没有 cue 自己的冷却**（原版 `AudioCue.ConfigAudioSource` 有 `lastTimePlayed`/`timeToPlayAgain`）。
⇒ Tap 那一层要么按「每个定义各存一份 cue 实例」补（照原版），要么显式声明接受偏差。**首选照原版**。

---

## §3 Q3 —— `TapParticleController` 的真实行为（能确定 / 不确定，分开写）

### §3.1 反编译状态（这是上一批放弃的直接原因，**结论要更正**）

| 文件 | `//IL_` 注记 | `NoteDecompilerIssue` | 后果 |
|---|---|---|---|
| `ILSPY/TapParticleController.cs` 整个文件 | **97** | — | 看着很吓人 |
| ├ `Initialize` | 1 | 0 | 完全可读 |
| ├ `CheckCollision` | 7 | 1 | 完全可读（且是死方法） |
| └ **`Update`** | **87** | **37** | ilspy 版**不可直接照抄** |
| `ILSPY/TapHitMaterialDefinition.cs` 整个文件 | **33** | — | |
| ├ 嵌套 `PlayParticle` | 26 | 12 | |
| └ `TryPlayParticles` / `Update` | 4 / 1 | 1 / 0 | 完全可读 |
| **`DEC/TapParticleController__Update.c`** | **0** | — | ✅ **控制流完整可读** |
| `DEC/…__Initialize.c` / `…__CheckCollision.c` / `TapHitMaterialDefinition.*.c` / `…PlayParticle.c` | **0 / 0 / 0 / 0** | — | ✅ 同上 |

⇒ 🔴 **「反编译打烂了」只对 ilspy 版成立；Ghidra 版（`DEC`）这一族一个注记都没有。**
本文件的行为描述**全部以 `DEC` 为准**，ilspy 只用来拿字段名/签名。

**仍然存在的两处（B 档，不影响桌面）**：见 §3.4。

### §3.2 `Update` 的完整行为（可以直接照这份写）

```
Update():
  if (battleManager == null) return
  if (battleManager.battleStage != MatchProper(50)) return
  wm = WindowsManager.Instance; if (wm == null) return
  if (wm.currentWindow != null) return                       // 任何窗开着
  es = EventSystem.current;   if (es == null) return
  pointerId = -1
  if (Application.isMobilePlatform && Input.touchCount > 0):
        …（§3.4 B-1，桌面不走）
  if (es.IsPointerOverGameObject(pointerId)) return          // 压在 UI 上
  if (!Input.GetMouseButtonDown(0)) return                   // 按下沿
  cam = Camera.main;          if (cam == null) return
  ray = cam.ScreenPointToRay(Input.mousePosition)
  n = Physics.RaycastNonAlloc(ray, hits /*len 3*/, float.MaxValue, (int)collisionMask)
  if (debugCollisions) Debug.Log($"[{Time.frameCount}] Detected {n} collisions")
  if (n < 1) return
  best = index of MIN hit.distance over [0,n)                // 无组件过滤（B-3：读的是**数组元素**里的 distance；`+0x20` = 数组数据起点、`0x2c` = 步长）
  if (debugCollisions) { 逐条 Debug.Log("…Checking collision against {name}. Distance {d}. Has Material definition {b}") }
  Debug.Log($"[{Time.frameCount}] Selected {hits[best].collider.name}")
  def = hits[best].collider.GetComponent<TapHitMaterialDefinition>()
  if (def == null) return                                    // ⚠️ 不顺延
  if (def.delayTimeToPlayAgain != 0 && def.remainingNotPlayTime > 0) return
  pd = def.particleDefinition
  if (!pd.particles.RuntimeKeyIsValid()) return
  if (!pd.audio.RuntimeKeyIsValid()) return
  pd.PlayParticle(hits[best].point, hits[best].normal, def.targetAnchor)
  def.enabled = true
  def.remainingNotPlayTime = def.delayTimeToPlayAgain
```

### §3.3 `PlayParticle` 的完整行为

> 🔴 **先看这句（2026-10-25 补，`N13`）**：这一节讲的 **`ParticleCollisionDefinition.PlayParticle`（149 行）
> 才是「真正被执行的行为体」**。
> 上面 §3.2 那段 `Update` 伪代码的**最后两步**（`pd.PlayParticle(...)` + `enabled=true` + 重置冷却）
> 与 **`TapHitMaterialDefinition.TryPlayParticles`（42 行）是同一套逻辑的两份实现** ——
> `TryPlayParticles` 零外部调用、只是包装层（详见 §2.3 的更正块）。
> ⇒ **实现顺序：本节（149 行）→ §3.2 的 `:322-361` → 最后才用 `TryPlayParticles` 交叉验证门的顺序。**
> ⛔ 别只读 `TryPlayParticles` 就动手：它读起来自洽、也能跑，但**粒子缓存 / 锚点 / 音效一个都不在里面**。

```
PlayParticle(position, normal, parent):
  if (!particles.valid || !audio.valid) return
  if (!cacheParticles.TryGetValue(particles, out ps)) {
      go = particles.Load()                     // GameObject
      src = go.GetComponent<ParticleSystem>()
      ps = Instantiate(src)                     // 克隆整棵子树
      cacheParticles.Add(particles, ps)
  }
  if (instanceAudioCue == null) instanceAudioCue = audio.Load()
  if (parent == null) {
      ps.transform.position = position + normal * 0.1f      // 常量 = 0.1（已从 .rdata 直读，见下）
      ps.transform.rotation = Quaternion.LookRotation(normal)
  } else {
      ps.transform.parent        = parent
      ps.transform.localPosition = Vector3.zero
      ps.transform.localRotation = Quaternion.identity
  }
  ps.Play()
  instanceAudioCue.Play3D(position, MixerType.FX)           // 第 4 实参恒 0 = FX
```

**常量复核**（`工具/read_literal.py` 直读 `GameAssembly.dll`）：

| 地址 | 4 字节 | 结论 |
|---|---|---|
| `0x1834b2dc4` | `0.1` ✅ | `normal × 0.1f` 那个偏移 |
| `0x1834b2d94` | `3.40282e+38` | `float.MaxValue`（射线 `maxDistance`） |

🔴 **三条容易被漏掉的行为（全是从 `.c` 读出来的）**：

1. **粒子实例是全局共享的、按「粒子 prefab」缓存**（`static Dictionary<AssetReference,…>`），
   不是每次点击新建。⇒ 25 种组合里 `Tap Rock` 出现 56 次，运行时**只有 1 个 `Tap Rock` 实例**被反复挪位置。
   `OnDestroy()` 只有一句 `cacheParticles.Clear()`（任意一个定义被销毁就整表清空）。
2. **锚点语义是「本地零位」**：`targetAnchor` 非空时粒子被挂到锚点下、本地位置/旋转归零
   ⇒ 特效出现在**锚点所在处**，跟你点在哪儿无关（黑军团的壁炉/格栅就是靠这个）。
3. **冷却双保险**：`delayTimeToPlayAgain`（定义自带的秒数）+ `AudioCue.timeToPlayAgain`（cue 自带的 0.1s，按 cue 实例算）。
   前者靠 `TapHitMaterialDefinition.enabled=false/true` 让 `Update` 只在冷却期跑（省一次每帧遍历）。

### §3.4 不确定的部分（B 档）+ 补法

| # | 不确定的是什么 | 为什么 | 怎么补 | 影响 |
|---|---|---|---|---|
| **B-1** | 移动端 `pointerId` 那两行：`.c:109` 的 `WebSocketSharp_Net_HttpStreamAsyncResult__get_Count` 与 `.c:121` 的 `UIElements_BackgroundSize__get_sizeType` 各是 **Touch 的哪个属性** | Ghidra 把两个 int/enum 返回值的 getter 名字解析到了**别的类**（`Touch` 的 int getter 有 `fingerId`/`tapCount`，enum 有 `phase`/`type`） | 需要时按 RVA 反汇编（`ILSPY` 里对应地址是 `@1820CFE00` / `@1812BB0D0`） | **桌面恒不走这一支**（`Application.isMobilePlatform == false`），不影响 PC 验收 |
| **B-2** | `Bounds.get_center` / `Bounds.get_extents` 被当成 `RaycastHit` 的字段读取 | `RaycastHit` 与 `Bounds` 前 24 字节布局相同（12+12） | 已按偏移判定：offset 0 = `hit.point`，offset 12 = `hit.normal`；与 `ILSPY/CheckCollision` 里 `raycastHit.point` / `.normal` **互相印证** | 无（已定） |
| **B-3** | 选最近那条用的 getter 名字是 `ParticleSystem.MinMaxCurve.get_constantMax` | 同样是名字误解析 | 🔴 **2026-10-25 订正理由（结论不变）**：那个 getter 收的是**托管数组第 i 个元素的首地址**（`.c:182` `lVar18 = i * 0x2c`；`:267-274` 传 `lVar10 + 0x20 + i*0x2c`）—— **`+0x20` 是数组数据起点、`0x2c` 是 `RaycastHit` 的元素步长（44 字节）**，⛔ **不是「`hit+0x20` 这个字段」**（上一版就是这么误读的）。「它 = `RaycastHit.distance`」由三点支撑：① 同一元素指针喂 `get_collider`；② 日志格式串是 `Distance {d}`；③ `Bounds.get_center/extents` 读元素 `0..11 / 12..23` = `point/normal`（与 `CheckCollision` 的 `.point/.normal` 互证） | 无（已定；**理由已订正**） |

🔴 **「丢失的方法体」在本族基本不存在** —— 唯一真正缺失的是 **`WindowsManager.currentWindow` 具体指哪些窗**
（那是另一个类型，本文件不展开；对我们而言 = 「有模态窗开着」）。

---

## §4 Q4 —— 什么条件下不播

| 闸 | 原版判断 | 值/出处 | 哪些模式会被关掉 |
|---|---|---|---|
| **① 对局模式** | `Initialize()`：`enabled = (battleManager.matchType != 100)` | `DEC/TapParticleController__Initialize.c`（`iVar1 != 100`）；`ILSPY/MatchType.cs`：**`Tutorial = 100`** | **只有「教程局(`Tutorial`=100)」整件关掉**。其余 17 档（Ranked 10 / Practice 90 / Campaign 130 / Replay 160 / FastMode 200 …）**全开** |
| ② 战斗阶段 | `battleManager.battleStage != 50` ⇒ return | `DEC/…__Update.c:75`；`BattleStage.MatchProper = 50` | **换牌期(40) / 战后(60) 不播**；只在对局正戏里播 |
| ③ 有没有窗开着 | `WindowsManager.Instance.currentWindow != null` ⇒ return | `.c:76-83` | 任何窗（设置 / 聊天 / 多卡 / 卡详情 / 选择框…）开着时**整帧不收** |
| ④ 指针在不在 UI 上 | `EventSystem.IsPointerOverGameObject(pointerId)` | `.c:129-136` | HUD 钮、手牌区等 uGUI 射线靶上**不算点地面** |
| ⑤ 不是按下 | `!Input.GetMouseButtonDown(0)` | `.c:130` | 只有**按下沿**，松手/按住都不算 |
| ⑥ 冷却 | `delay != 0 && remainingNotPlayTime > 0` | `.c:331-334` | 按定义各自的 `delayTimeToPlayAgain`（场上 0/0.1/0.2/0.5/1.0/60 六档） |
| ⑦ 资源无效 | `particles` / `audio` 任一 `RuntimeKeyIsValid() == false` ⇒ 不播 | `.c:335-350` | 数据侧：213 条里**没有一条是空引用**（§2.2 全表 25 组都能解） |
| ⑧ 最近那条没组件 | 上面第 13 步 | `.c:312-321` | 结构上的：碰撞体上没挂定义 ⇒ 这一下**白点** |

⚠️ **`debugCollisions` 全 13 场都是 0** ⇒ 原版**不会**打那两行 `[frame] Detected/Checking collision/Selected` 日志。

---

## §5 Q5 —— 我们的落点（施工表）

### §5.1 文件级落点

| 原版成员 | 建在哪（新文件） | 需要哪些数据 | 依赖谁 |
|---|---|---|---|
| `TapParticleController`（`InitializableBehaviour`） | **`OUR/Battle/TapParticleController.cs`**（独立 `MonoBehaviour`，**类名 = 文件名**，CLAUDE.md 那条 `MonoBehaviour` 存根铁律） | `collisionMask`（写死 `0x40000001`，与序列化值一致 + 注释出处）、`hits[3]` | `Camera.main`；`BattleEventSystem.Instance`；`BattleDriver` 的两条只读判据（下） |
| `TapHitMaterialDefinition`（+ 嵌套 `ParticleCollisionDefinition`） | **`OUR/Battle/TapHitMaterialDefinition.cs`** | 4 个序列化字段（§2.1 名字逐字照抄） | `WFSoundPlayer` / `WFSoundBank`；粒子 prefab 直连（本工程不需要 `AssetReference`） |
| 建碰撞体 + 挂定义 | **`Assets/WarpforgeArena1/Editor/ArenaBuilder.cs`**（`BuildContent` / `BuildArenaPrefabs` 那一段） | manifest 新段 `tapHits[]`（下） | `WarpforgeArena1/arenas/<场>/<场>_manifest.json` |
| 数据生成 | **`工具/scripts快照/gen_unity_arena_manifest.py`**（⚠️ **真身在这里**，不是 `d:/2` 那份） | 源 = `07_场景/<场>/`（有 `BoxCollider/` `MeshCollider/` 与那 20/27/… 条 `TapHitMaterialDefinition`；⚠️ **该 dump 有重复文件**，同一对象会存成 `X.json` 与 `X_X.json` 两份，按 `m_PathID` 去重） | —— |
| 音频 cue + clip | **`工具/import_original_sfx.py`**（手列表多 10 条 cue）· clip 拷进 `Resources/Art/audio/sfx/` | §2.4 的 cue/clip 名 | `WFSoundBank`（同一个字典，不用新开表） |
| HUD/UI 闸 | **`OUR/Battle/BattleDriver.cs` 暴露一处只读判据**（见 §5.2 ④） | —— | `BattleEventSystem` / 各模态旗标 |

**`manifest` 的 `tapHits[]` 建议形状**（每项一条 = 原版一个实例）：

```json
{ "path": "Scenario/.../Baked/Fences top",     // 完整层级（原版那棵树的路径，§2.2 抓的就是它）
  "go":   "Fences top",                        // 叶子名（与 meshes[] 的 go 对得上）
  "collider": { "type": "Mesh" },              // 或 {"type":"Box","center":[..],"size":[..]}
  "particles": "Tap Hit Metal",                // 用**名字**，不用 AssetReference（我们的 prefab 已在工程里）
  "audio": "Tap Hit Metal Heavy",
  "delay": 0.2,
  "anchor": null }                             // 非空时写锚点的 path
```

### §5.2 关键设计裁断（照原版，别自己发明）

1. **必须是独立组件 + 自己的 `Update`**，不要塞进 `BattleDriver.Update` 的分支里。
   原版就是独立组件（`BattlePrefab/BattleBoardElements/TapParticleController`），
   塞进 `BattleDriver` 会重新引入「谁先取边沿」的耦合。
2. **不要复用 `BattleDriver._downEdge`**（一次性边沿，见 §1.3-1）。组件自己读
   `Mouse.current.leftButton.wasPressedThisFrame`。
3. **碰撞体要真建**，别用「按世界坐标算区域」的分析式替身 —— 原版的语义是
   「最近的那条碰撞体说什么就是什么」（§1.1 第 12/13 步），区域近似会给出**不一样的答案**。
4. **模态窗闸只留一处判据**：由 `BattleDriver` 暴露（例如 `public bool TapBlocked`），
   覆盖它 `Update` 里那串 `return` 的模态分支（设置 / 聊天 / 多卡 / 卡详情 / 选择框 / 换牌 / 终局 / 回放条）。
   ⛔ 不要在 Tap 组件里再列一份窗表（两份迟早不一致）。
5. **音效走 `WFSoundPlayer.Play(cue, worldPos, is2d:false)`**，但 **cue 冷却要自己补**（§2.5 末的 ⚠️）。
6. **粒子实例复用是原版行为**：按 prefab 缓存一个实例、反复挪位置 —— ⛔ 别「每次 Instantiate 再销毁」（那是偏差）。

### §5.3 分件建议（给下一批，按这个顺序）

| 件 | 做什么 | 文件 | 验收 |
|---|---|---|---|
| **T1**（最小可验收） | 🔴 **2026-10-25 改对象（`N16` 推翻判据后）**：~~只做棋盘那一颗~~ ⇒ **做一件【看得见的场景道具】**，例 **arena1 的 `Barrels Right`**（layer 0 ✅ 在掩码内）或 `Generator 1`。建 1 个 BoxCollider + `TapHitMaterialDefinition` + `TapParticleController`（桌面路径）。🔴 **行为体照 `ParticleCollisionDefinition.PlayParticle`（§3.3，149 行）写，⛔ 不要照 `TryPlayParticles`（42 行包装层，§2.3）**。⚠️ **棋盘那一颗 `CardLowBoardLimit` 照原版留在 layer 11、⛔ 别让它进掩码**（见 §2.2 的订正块） | `Battle/TapParticleController.cs` · `Battle/TapHitMaterialDefinition.cs` · `Editor/ArenaBuilder.cs` | 真 Play：**点木桶/发电机那一带有粒子 + 有音**（需先做 T2 的音频）；**点棋盘正中央应当【没有任何反馈】** —— 这条和上一条**同样重要**，它挡「为了让棋盘也有反应去改掩码」 |
| **T2** | 音频：10 条 cue + 20 条 clip 导入（`import_original_sfx.py` 手列表） | `工具/import_original_sfx.py` · `Resources/Art/audio/sfx/` · `Resources/animfx_sounds.json` | `WFSoundBank.CueCount` 增 10；`MissingClips` 不增 |
| **T3** | 13 场全量：manifest 加 `tapHits[]` + ArenaBuilder 建碰撞体/挂定义 | `gen_unity_arena_manifest.py` · `ArenaBuilder.cs` · 13 份 manifest · 13 件 prefab | 逐场核实例数（20/27/14/13/27/15/6/1/9/11/11/1/11 + 每场 1 颗棋盘） |
| **T4** | 闸门与冷却：`battleStage`/模态窗/UI/教程局 四条 | `BattleDriver.cs`（只读判据）+ T1 组件 | 教程局不响、换牌期不响、窗开着不响 |
| **T5** | 锚点 18 条（黑军团 7 / arena2 1 / arena3 2 / 共享 8） | manifest `anchor` 字段 + ArenaBuilder | 点壁炉/格栅时特效出现在锚点而不是点击处 |

---

## §6 查不到 / 还差什么

| # | 差什么 | 搜过什么 | 怎么补 |
|---|---|---|---|
| 1 | 移动端 `pointerId` 那两行的确切属性（B-1） | `ILSPY` 的两个 `Method not found @` 地址；`DEC` 的误名 getter | 按 RVA 反汇编 `0x1820CFE00` / `0x1812BB0D0`。**不影响 PC** |
| 2 | `WindowsManager.currentWindow` 到底覆盖哪些窗（对我们 = 「模态窗」的完整清单） | 本族反编译 | 从 `WindowsManager` 类单独取；或先用我们 `BattleDriver.Update` 的模态分支当判据（§5.2-4） |
| 3 | `TapHitMaterialDefinition` 的 `m_Enabled` 序列化值（13 场全为 1）与原版 `Awake` 里 `enabled=false` 的相互作用 | 资产 `m_Enabled` | **不需要解**：Unity 的 `Awake` 与 `enabled` 无关、必跑；`enabled=false` 之后 `Initialize` 也不碰它 —— 只有首次播放才置 true。照抄即可 |
| 4 | arena3 有 **1 条定义挂在没有碰撞体的宿主上**（`Scenario/Battle Arena 3 Baked` 根，`Tap Rock + Buff Mana 1`） | UnityPy 逐条核宿主组件 | 原版这条**永远不会触发**（`GetComponent` 只在碰撞体的 GameObject 上找）。⛔ 不要为它补碰撞体 |
| 5 | `07_场景` dump 的重复文件（`X.json` + `X_X.json`）是否会带出**旧 build** 的值 | arena1 逐条比对：40 个文件 = 20 条 × 2，`go` / `delay` 完全一致 | 用 `assets_full`（当前 build，213 条精确）当**唯一数据源**；`07_场景` 只当坐标/层级来源 |

---

## §7 附录：可复现的证据命令

```bash
# ① 213 个实例 + 25 种组合 + 每场分布（读 assets_full，不写任何文件）
python -I -X utf8 - <<'PY'
import json,glob,collections,os
SG='5926791653216923474'
files=[f for f in glob.glob(r'D:/2/新解包资源/assets_full/*/MonoBehaviour/*.json')
       if SG in open(f,encoding='utf-8',errors='ignore').read()]
print(len(files), collections.Counter(f.split(os.sep)[-3] for f in files).most_common())
PY

# ② GUID → 资产名（二进制 catalog，与本地束同 build）
python -I -X utf8 d:/4/Unity/工具/_guid_resolve_probe.py g 06946a9ab456c474f9b4f86e00fecf9c

# ③ cue → clip 名（**必须读真束**）
python -I -X utf8 - <<'PY'
import UnityPy
env=UnityPy.load(r'D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/soundcollection_assets_all.bundle')
for o in env.objects:
    if o.path_id == 6857198296051779852:          # Buff Mana 1
        print(o.read().m_Name)
PY

# ④ 常量直读
python -I -X utf8 d:/4/Unity/工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" 0x1834b2dc4

# ⑤ 层级/碰撞体（UnityPy 读场景束；assets_full 的 GameObject 文件名是「名字_pathid.json」）
```

---

## §8 一页速查（给执行代理贴在显示器上）

- 触发 = **按下沿**（不是松手） · 目标 = **最近的那条碰撞体**（不是最深/最先）
- 掩码 = **0x40000001** · hits 上限 = **3** · 预偏移 = **法线 × 0.1**
- 冷却 = 定义自带 `delayTimeToPlayAgain`（0/0.1/0.2/0.5/1/60）+ cue 自带 `0.1s`
- 特效实例 = **按 prefab 缓存一件、反复挪** · 锚点非空 ⇒ **本地零位**（跟你点哪儿无关）
- 不播：教程局(100) / 换牌期 / 战后 / 有窗开着 / 指针在 UI 上 / 没按下 / 冷却中 / 最近那条没挂定义
- 🔴 **点棋盘正中央【不该】有反馈** —— 棋盘那颗 `CardLowBoardLimit` 在 **layer 11**，不在掩码 `0x40000001`（= layer 0 + 30）内。
  **能打中的只有 layer 0 的道具**（木桶/栅栏/发电机/隐形 `Collision`·`Cube` 盒）。⛔ **别为了「让棋盘也有反应」去改掩码或给 board limit 换层**（那是改坏原版行为）
- 🔴 **`battlearenaemperorschildren` / `battlearenaspacewolves` 两场整场无地面反馈**（各自唯一那条 tapHit 就是棋盘那颗，打不到）
- 🔴 **要抄的方法体 = `ParticleCollisionDefinition.PlayParticle`（§3.3，149 行）**；
  `TapHitMaterialDefinition.TryPlayParticles`（42 行）只是包装层、零调用，⛔ 别拿它当实现（§2.3）
- **粒子和音效的名字、次数、每场分布**：§2.2；**哪些已在工程里**：§2.5
- **哪些结论是「外推」**：§0·b（标了「实读 / 外推」的表，照抄前先看那一列）
