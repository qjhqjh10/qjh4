# D1013 · 诊断块 1（`BattleScene` 9 条 + `DeckScene` 2 条）

> 诊断时刻：2026-10-13 · **只读诊断代理**（未跑 Unity · 未改任何工程文件 · 只写本报告）
> 输入：`d:/4/_tmp_view/battle.log`（`1551 通过 / 9 失败`，结束行 `:40365`）· `d:/4/_tmp_view/deck.log`（`909/911`，结束行 `:16096`）
> 判据来源：日志原文 + 现读自检宿主源码 + 旁挂/预制体实测 + `d:/2/tools/decomp_full/`（本轮 11 条**都没用到反编译**，因为 11 条全落在「夹具/断言」那一侧）

## 一、总结论

| 档 | 条数 | 哪几条 |
|---|---|---|
| **(α) 断言错** | **4** | `A191(darkangels)` · `A514②` · `A547#1` · `A547#2` |
| **(β) 实现缺陷** | **0** | —— |
| **(γ) 本批回归**（行为被改坏） | **0** | —— |
| **(δ) 夹具前提不成立** | **7** | `A431`×4（arena2 / arena3 / tau / 合计）· `A514①` · `A463` · `A462` |

**11 条里 0 条是实现缺陷**，全部是**期望值 / 计数口径 / 段落位置与本轮新事实不同步**。两条主根因：

- 🔴 **5 条同一个根因（A431×4 + A514①）**：本轮 `ArenaBuilder.BuildArenaPrefabs` 重建了 13 场 prefab
  ⇒ 旁挂 `nodes[]` 要建的那几个分组节点**prefab 里已经有了** ⇒ `SceneAnimFxNodesCreated` 3/4/1/8 → **0**
  （对应地 `SceneAnimFxNodesReused` → 3/4/1/8）。**这是本仓 A514 本批故意要的行为**（`Battle/ScenarioBlendables.cs:2327-2352`
  「有同名子件就复用，不建两份——两份同名之后 `SceneResolver` 命中哪一份不确定」）⇒ **不是缺陷，是期望值过时**。
  出处在源码注释里**只预告了一半**（`Editor/BattleScene.cs:10480-10488` 预告 `reparent` 会变，没预告 `nodes` 会归零）。
- 🔴 **4 条是断言自己写坏**（与实现无关）：`A191` 的 `Animation` 计数少统计一张表 ·
  `A514②` 的检测串与文案自撞 · `A547×2` 取错了日志回调的参数。

---

## 二、逐条

### 1. `A191`：darkangels 的旁挂结构逐条对上（`battle.log:3631`）

- **判定：(α) 断言错**
- **证据**
  - 日志实得：「节点 **17/17** · 带 `Animation` 的 **1** 个 · 改挂 83 条 · **1 条树里没有**：目标 `BoardCamera`」
  - 条件在 `Editor/BattleScene.cs:1610`：
    `Check(dkSc != null && dkBad == 0 && dkNode == dkSc.nodes.Length && dkAnim >= 2, …)`。
    文案里没出现「**N 条摆错了**」⇒ `dkBad == 0`；「17/17」⇒ `dkNode == nodes.Length`。
    ⇒ **唯一不成立的是 `dkAnim >= 2`，实得 1**。
  - `dkAnim` 只在 **nodes 循环**（`:1540` `if (n.animation != 0 && hasAnim) dkAnim++;`）与
    **adds 循环**（`:1571-1574`）里自增，**targets 循环（`:1544-1561`）里没有**。
  - 数据侧（本轮重算前后对比，`git show HEAD:` vs 工作区）：
    `battlearenadarkangels_groups.json` 里 **`Directional Light` 从 `adds[]` 挪进了 `targets[]`**
    （HEAD：`adds=[{name:"Directional Light", animation:1}]` → 现在：`adds=[]`，
    `targets` 多一条 `{name:"Directional Light", parent:"Scenario", animation:1}`）
    ⇒ `dkAnim` 从 2 掉到 1。
  - **行为其实一点没坏**：生产侧 `ArenaBuilder.ApplyGroupNodes` 的 **targets 循环照样补组件**
    （`WarpforgeArena1/Editor/ArenaBuilder.cs:2606`：`if (t.animation != 0 && tr.GetComponent<Animation>() == null) → tr.gameObject.AddComponent<Animation>()`）。
    实读重建后的 `Unity/MyGame/Assets/Resources/ArenaPrefabs/battlearenadarkangels.prefab`：
    classID `111`（`Animation`）恰好 **2** 个，分别在 `Battle Arena Dark Angels baked` 与 `Directional Light` 上
    —— 与旁挂逐条对上。
  - 旁证：`_stats.animation = 2`（生成器 `Unity/工具/gen_arena_groups.py:651-653` 的算法 = **nodes + targets + adds 三张表之和**）
    ⇒ 「2」这个数字本来就是**跨三张表**的口径，而断言只数两张表。
  - ⚠️ 顺带澄清：那「**1 条树里没有：目标 `BoardCamera`**」**不是**本红的原因 ——
    tauviorla 那一条（`battle.log:3655`）带着同一句却**通过**，而且 `dkNotBuilt` 根本不在 `:1610` 的条件里（只在文案里）。
- **根因**：本轮重算把 `Directional Light` 从 `adds[]` 挪到 `targets[]`，而 `dkAnim` 的计数口径没跟着扩到 `targets[]`。
- **最小改法**：`Editor/BattleScene.cs:1561`（`dkMove++;` 那一行**之前**）在 targets 循环里补一段与 adds 循环同形的计数：
  `if (t.animation != 0) { if (tr.GetComponent<Animation>() != null) dkAnim++; else { dkBad++; dkBadWhat += $"`{t.name}` 没补上 `Animation` "; } }`
  ⛔ **别改成 `dkAnim >= 1`**（那是弱化，放掉了「`Directional Light` 有没有 `Animation`」）。
  ⚠️ tauviorla 那一半（`tvAnim >= 2`，`:1681`）**也补**的话会变成 3 —— **仍 ≥ 2，不会红**（tau 的 `targets` 里也有 1 条 `Railgun BIG (1)` 带 `animation`）。
- **置信度：高**

### 2. `A431`：`battlearena2` 的 `RocketTrail` 那一族真建出来了（`battle.log:39421`）

- **判定：(δ) 夹具前提不成立**
- **证据**
  - 实得「组件 1 · **新建节点 0** · 改挂 5 · 没对上 0」（文案自带实得值）；期望在 `Editor/BattleScene.cs:10538`，值来自 `:10506-10509` 的 `wantBuilt={1,2,2}` / `wantNodes={3,4,1}` / `wantReparen={5,0,0}`。
    ⇒ 只有 **`SceneAnimFxNodesCreated == 3`** 这一项不成立（其余 1/5/0 三项都对上）。
  - 旁挂侧实读 `Unity/MyGame/Assets/Resources/EnvBlendables.json` → `sceneStandaloneBuild[0]`：
    `nodes = 3`（`Scenario` / `Scenario/Battle Arena 2 Particles` / `…/RocketTrail`）· `reparent = 5` ✓
  - 重建后的 prefab 里这三个节点**都在**：`Resources/ArenaPrefabs/battlearena2.prefab` 有
    `m_Name: Scenario`(:214731) · `Battle Arena 2 Particles`(:113742) · `RocketTrail`(:170786)（prefab mtime 2026-10-06 12:42 = 本轮重建）
  - 代码路径：`Battle/ScenarioBlendables.cs:2327-2340` —— `var exist = FindChildByName(parentTr, nodeName); if (exist != null) { SceneAnimFxNodesReused++; … continue; }`
    ⇒ 节点已在 ⇒ **一个都不新建**。
  - 同段其余两条自证都**通过**（`battle.log:39433` 「RocketTrail 底下挂着 ≥5 颗粒子，实得 10」· `:39445` 开关那条 ✓）。
- **根因**：期望值描述的是「prefab 里还没有这些分组节点」的**旧状态**（那时靠运行时新建）；本轮 prefab 重建后改走「复用」那一支。
- **最小改法**：见第 4 条（三场共用一套改法）。
- **置信度：高**

### 3. `A431`：`battlearena3` 那一族（`battle.log:39553`）

- **判定：(δ) 夹具前提不成立**
- **证据**：实得「组件 2 · **新建节点 0** · 改挂 0 · 没对上 0」，期望 `2 / 4 / 0 / 0`（`Editor/BattleScene.cs:10587`）。
  旁挂 `sceneStandaloneBuild` 里 `battlearena3.nodes = 4`（`Scenario` / `Scenario/Particle Effects` / `…/Lightning_Green` / `…/Lightning_Green (1)`）；
  重建后的 `battlearena3.prefab` 里有 `m_Name: Scenario`(:53537) 与 `Lightning_Green`(:108941) / `Lightning_Green (1)`(:175825)。
  同段两条开关断言（`battle.log:39565` 那条 `Lightning_Green`）**通过**。
- **根因**：同第 2 条。
- **最小改法**：见第 4 条。
- **置信度：高**

### 4. `A431`：`battlearenatauviorla` 那一族（`battle.log:39639`）

- **判定：(δ) 夹具前提不成立**
- **证据**：实得「组件 2 · **新建节点 0** · 改挂 0 · 没对上 0」，期望 `2 / 1 / 0 / 0`（同一个 `Check`，`Editor/BattleScene.cs:10587`）。
  旁挂 `battlearenatauviorla.nodes = 1`（`Scenario/Battle Arena Tau Viorla Baked/Railgun BIG (1)/Big Gun Effect`）；
  `battlearenatauviorla.prefab` 有 `m_Name: Scenario`(:439101)。
- **根因**：同第 2 条。
- **最小改法（第 2~4 条共用）**：`Editor/BattleScene.cs`
  - `:10509` 之后加 `int[] wantReused = { 3, 4, 1 };`
  - `:10538`（arena2 那条单独写的 `Check`）：把 `… == wantNodes[0] …` 换成 `… SceneAnimFxNodesCreated == 0 && … SceneAnimFxNodesReused == wantReused[0] …`，并改文案（「新建节点 {x}（期望 3）」→「新建 0 · 复用 {wantReused[0]}」）
  - `:10587`（循环里那条）：同样改（循环体里多读一个 `reused` 局部量）
  - `:10610`（合计）：`totalNodes == 8` → `totalNodes == 0 && totalReused == 8`
  - **更耐改的写法**（推荐一并做）：断 `nodes + reused == wantNodes[i] && missed == 0`（「旁挂每一条都得在树里落地」），
    再单配一条 `nodes == 0` 钉住「prefab 已把分组节点烘进去了」这一档 —— 将来谁把节点从 prefab 里拿掉，红的会是那条**点名**的断言。
- **置信度：高**

### 5. `A431`：三场合计（`battle.log:39663`）

- **判定：(δ) 夹具前提不成立**
- **证据**：实得「建出 5 个组件 ✓ · **新建节点 0**（8）· 改挂 5（5）· 没对上 0（0）」，条件在 `Editor/BattleScene.cs:10610`；
  五项里只有 `totalNodes == 8` 不成立。合计 8 = 3+4+1（第 2~4 条）。
- **根因**：同第 2 条（合计只是各场之和）。
- **最小改法**：同第 4 条最后一条（`totalNodes == 0 && totalReused == 8`）。
- **置信度：高**

### 6. `A514①`：节点已存在就复用（`battle.log:39748`）

- **判定：(δ) 夹具前提不成立**
- **证据**：实得「复用 **3** · 新建 **0** · 场根底下叫 `Scenario` 的子件 **2** · 组件 1 · 改挂 5 · 没对上 0」，
  期望 `1 / 2 / 1 / 1 / 5 / 0`（`Editor/BattleScene.cs:10656`）。
  - 「复用 3 / 新建 0」= 第 2 条那个根因（prefab 已含全部 3 个节点）。
  - 「`Scenario` 子件 2 个」是**夹具自己**造成的：`:10644-10646` 先手工摆了一颗 `Scenario`，
    而 prefab 自带的那颗**也已经叫 `Scenario`** ⇒ 两份同名。**生产路径不会这样**（传进来的 root 就是 prefab 那一份）。
  - 该段另外两条**通过**（`battle.log:39760` 「复用不改写」· `:39772` 「复用那一档出声」）。
- **根因**：夹具（手工摆节点）与「prefab 已经自带这些节点」这个新事实叠加 ⇒ 期望值与**同名重复**这个坏状态一起过时。
- **最小改法**（推荐）：删掉 `:10644-10646` 那三行「先摆一颗 `Scenario`」，把 `:10656` 的期望改成
  `复用 3 · 新建 0 · 场根底下叫 `Scenario` 的子件 1 · 组件 1 · 改挂 5 · 没对上 0`；
  再把 `:10667` 那条「复用**不改写**」的判据改成「调用**前后** prefab 那颗 `Scenario` 的 `localPosition` 逐位不变」
  （语义不变，仍然是「只出声、不动我们的树」）。
  ⛔ **省事版**（保留摆节点、期望改 `3 / 0 / 2 / 1 / 5 / 0`）**不推荐** —— 那等于把「同一个父底下两份同名」断成合格。
- **置信度：高**

### 7. `A514②`：旁挂没读到那一档每次都出声、且不谎报（`battle.log:39808`）

- **判定：(α) 断言错**
- **证据**：实得「两次各回 **0 / 0** ✓；「一条都没建」出现 **2** 次（期望 2）✓；**日志里有「已经建过」= True（期望 False）✗**」。
  - `battle.log:39784` 与 `:39796` 就是那两条命中——它们**正是**同一条「**一条都没建**」警告（⇒ `missB == 2` 对）。
  - 那条警告的**正文自己**含「已经建过」：`…⚠️ 这一趟**不记** `_sceneAnimFxRoot`：记了会让同一个实例再进来时**谎报「已经建过」**`。
    检测串在 `Editor/BattleScene.cs:10710`：`if (m.Contains("已经建过")) claimB = true;`
  - 真正的「谎报」那句在 `battle.log:39457`（`**这个战场实例已经建过** ⇒ 不重复建`）——它属于 A431 段，**不在** `gotB` 的采集窗口里。
  - 反证「采集参数没错」：同一个 sink 抓到了 `missB == 2`（子串 `**一条都没建**` 只可能出现在**消息正文**里，不可能是堆栈）
    ⇒ `gotB.Add(m)` 收的确实是**第 1 个参数 = 消息**。
- **根因**：**检测串与文案自撞** —— 命中的是那条消息里的**说明文字**，不是「谎报」那条消息本身。
- **最小改法**：`Editor/BattleScene.cs:10710` → `if (m.Contains("这个战场实例已经建过")) claimB = true;`
  （真谎报那句带「这个战场实例」，新警告那句只带「已经建过」⇒ 两者可分）。
- **置信度：高**

### 8. `A463`：收工「一个订阅者都不剩」（实得 2）（`battle.log:40319`）

- **判定：(δ) 夹具前提不成立**
- **证据**
  - 断言在 `Editor/BattleScene.cs:11241`；本段**自己那一台**摘得很干净（`finally` 里 `rig463Ss.UnregisterResolutionSignal()`，`:11235`）。
  - 全工程注册口只有两处（`grep RegisterResolutionSignal()`）：`Battle/CombatCameraZoom.cs:360`（`EnsureScreenSizeComponent()` 里，
    由 `CombatCameraZoom.Initialize` 走到）与 `Battle/BattleCameraSreenSize.cs:167`（`Start()`，批处理不跑）。
  - 本轮会建出实例的地方共 **3 处**：主战场那台（`Battle/BattleDriver.cs:6125` `_autoZoom = gameObject.AddComponent<CombatAutoZoom>()` → `InitializeAutoZoom()` → `CombatAutoZoom.Initialize()` → `CameraZoom.Initialize(true)`）·
    A422 rig（`BattleScene.cs:10037 rigAz.Initialize()`）· A463 rig（`:11062`）。
  - **主战场那台还活着**：`:10439` 的 `DestroyImmediate(driver)` 销毁的是 `BattleDriver` **组件**，
    而 `CombatAutoZoom` / `BattleCameraSreenSize` 由 `gameObject.AddComponent` 加在 **`sceneRoot`** 上 ⇒ 不受影响
    （`battle.log:39312` 那条只确认「组件真没了」）。⇒ 这是**1 个合法订阅者**。
  - **A422 rig 那台泄漏**：它的 finally（`:10239-10244`）只 `DetachMinionEvent()` + `DestroyImmediate(rigGo)`，
    **没有** `UnregisterResolutionSignal()` ⇒ 静态事件上留了一个指向**已销毁**组件的委托。
  - 🔴 **直接证据**：`battle.log:40233` = `[BattleCameraSreenSize] \`Initialize\` 的 combatCameraZoom 与 cameraVerticalFramer 两个都 没接上 …`，
    位置正好夹在 A463 ㈢（`:11134`）与 ㈣（`:11150` 的 `NotifyScreenResolutionChanged()`）之间 ——
    **两个引用都是空**只可能是**已销毁**的实例 ⇒ 抬那一次信号**真的打到了泄漏的订阅者**。
  - 该消息还有 `_refsMissingNoted` 闩（`Battle/BattleCameraSreenSize.cs:213`）⇒ 只报一次，所以看不到第二条。
- **根因**：`2 = 主战场（合法、活着） + A422 rig 泄漏（已销毁）`；本段的假设「只有我这一台注册过」不成立。
- **最小改法**（两件，建议都做）
  1. `Editor/BattleScene.cs:11241` 改成**增量**自证：在本段建 rig 之前（`:11042` 那句 `GameObject rig463 = …` 之上）记
     `int sigBefore463 = BattleCameraSreenSize.ResolutionSignalSubscriberCountForTest;`，
     收工断 `… == sigBefore463`（文案写清「场上还有主战场那台合法订阅者，所以基线不是 0」）。⛔ **别写成 `<= 2`** 那种死数字。
  2. 顺手堵住泄漏：`:10242` 的 `DetachMinionEvent()` 旁边补
     `var czRef = rigAzRef != null ? rigAzRef.CameraZoomForTest : null; if (czRef != null && czRef.battleCameraScreenSize != null) czRef.battleCameraScreenSize.UnregisterResolutionSignal();`
     （改完 ① 的基线从 3 回到 2，**仍 ≠ 0**，所以 ① 必须一起改）。
- **置信度**：根因 **高**（`battle.log:40233` 是直接证据）· 「2 的构成」**高**（三台实例逐一排除）

### 9. `A462`：（前提）场上有一台 `BattleDriver`（`battle.log:40342`）

- **判定：(δ) 夹具前提不成立**
- **证据**
  - 场上**唯一**那台 driver 由 `Editor/BattleScene.cs:98` `BuildScene(out BattleDriver driver, …)` 建出（`:12607 driver = sceneRoot.AddComponent<BattleDriver>();`），
    在 `:10439` 被 `UnityEngine.Object.DestroyImmediate(driver);` —— `battle.log:39312` 的 A388 自己确认「组件**真没了**（`== null`）」。
  - 本段（`:11247-11725`，**478 行**）要的是**活着、HUD 已建**的 driver：`wb4.Settings`（设置面板）·
    `wb4.TickSettingsInputForTest()` · `wb4.HudButtonWorldPosForTest(…)` · `wb4.BattleLog` ·
    `wb4.SimulateOpenCommand(probe5)`（要真 `ctx` 才能开选择器）—— 只有跑过 `Begin` 的那台才有。
  - 反证「不是查找方式的问题」：后面 `CheckSavedScene` 打开存档场景之后同一条查找**又有结果** ——
    `battle.log:41396 ✓ 存档里有 BattleDriver`。⇒ 缺失**只发生在这一段的位置上**。
- **根因**：**段落位置**（本轮新加，被追加到整轮末尾），不是代码缺陷 —— 它前面约 1000 行处 driver 已经卸掉了。
- **最小改法**：把 `:11247-11725` 整段**搬**到 `driver.Begin("Ultramarines","Goff",20260915);` + `driver.SimulateMulliganDone();`
  （`:10402-10403`）之后 —— 那里 driver 活着、HUD 已建、开局已走完。搬完在本批里跑 `BattleScene.Run` 收口，
  并**复核 A388(b) / A515 的 `hookedBeforeB` 读数**没被本段改动（本段自己 try/finally 收尾，理论上不碰静态钩子）。
  ⛔ **备选（不推荐）**：只把 `:10439` 的销毁与两段 `realUnload388` 分档往后挪到 A462 之后 ——
  那会把 A388「销毁后**立刻**读数」变成「中间隔了一整段」，读数不再可分。
- **置信度**：根因 **高** · 落点 **中**（搬完要复核 A388/A515 两条读数）

### 10. `A547#1`：写盘失败「出声」（`deck.log:15305`）

- **判定：(α) 断言错**
- **证据**
  - 实得 `[False]`；断言在 `Editor/DeckScene.cs:3846`。
  - **那句话真的打了**：`deck.log:15249` = `[Deck] 导入失败：卡组串读出来了，但**没写进存档**——存档写入失败：Could not find a part of the path "…\__wf_a547_no_such_dir__\x.json.tmp".（重启就没了）`；
    它的调用栈是 `DeckRuntime.Say`（`Deck/DeckRuntime.cs:1948` 的 `Debug.Log("[Deck] " + …)`）← `TryImport`(`:2520`) ← `UiTryImport`(`:2949`) ← **`DeckScene.cs:3836`**，
    ⇒ 完全在 sink 窗口（`:3832` 订阅 ~ `:3839` 退订）**之内**。
  - **但 sink 收错了参数**：`:3831` = `Application.LogCallback sink = (cond, msg, type) => logs.Add(msg);`。
    `Application.LogCallback` 的签名是 `(string condition, string stackTrace, LogType type)` ⇒ **第 2 个是堆栈**，消息在 `condition`。
  - 全仓反证（`grep Application.LogCallback`，共 20 处）：`BattleScene.cs:208/2849/9555/10648/10691/10737/10762` ·
    `CollectionScene.cs:2201/2206/2515/2520/5712/5791/5803/5991` · `RewardsScene.cs:4440/4478/7619`
    —— **全部用第 1 个参数**；只有 `DeckScene.cs:3831` 这一处用第 2 个。
  - 最硬的反证：`BattleScene.cs:10691` 那处用第 1 个参数，实测在 `battle.log:39808` 抓到「**一条都没建**」出现 **2** 次
    （`missB == 2`）—— 子串 `**一条都没建**` 只可能出现在**消息正文**里 ⇒ **第 1 个参数就是消息**。
  - 同一窗口第三条断言的实得也吻合：「`spoken` **不**含「已导入」」通过（堆栈里当然没有）。
- **根因**：取错日志回调参数（`logs` 里装的全是 stackTrace，三条 `Contains` 全在比堆栈）。
- **最小改法**：`Editor/DeckScene.cs:3831` → `Application.LogCallback sink = (cond, msg, type) => logs.Add(cond);`
  （或把参数名改成 `message` / `stack` 再 `logs.Add(message)`，免得下一个人再踩）。
- **置信度：高**

### 11. `A547#2`：报的是真原因（`deck.log:15333`）

- **判定：(α) 断言错**
- **证据**：实得 `[False]`；断言在 `Editor/DeckScene.cs:3850`（`spoken.Contains(lib.LastError)`）。
  **同一根因**（`spoken` 里是堆栈）。真原因确实在消息里：`deck.log:15249` 那句的
  `存档写入失败：Could not find a part of the path "…"` 就是 `DeckLibrary.LastError` 的原话
  （`TryImport` 的 `SaveFailReason()` 用的就是它，`Deck/DeckRuntime.cs:2261-2263`）。
- **根因**：同第 10 条。
- **最小改法**：与第 10 条**同一行改动**（`:3831`）—— 改完这一条会自动绿。
  ⛔ **不许**用「把期望改成堆栈里有的东西」来修：那会把「报的是**真原因**」降级成「报的是调用栈」。
- **置信度：高**

---

## 三、按档汇总

- **(α) 断言错 4 条** —— `A191(darkangels)`（计数少一张表）· `A514②`（检测串自撞）· `A547#1` / `A547#2`（取错回调参数，**一处改动同时修两条**）
- **(β) 实现缺陷 0 条** —— 11 条红里没有一条是产品代码做错了
- **(γ) 本批回归 0 条** —— 没有「以前对、这轮被改坏」的行为；`A191` / `A514②` / `A547×2` 是**断言自身**的问题，`A431×4`/`A514①`/`A463`/`A462` 是**夹具/期望**的问题
- **(δ) 夹具前提不成立 7 条** —— `A431×4`（prefab 重建 ⇒ `nodes` 不再新建）· `A514①`（同根因 + 夹具自摆节点）· `A463`（场上另有合法订阅者 + 一处夹具泄漏）· `A462`（段落位置在 driver 卸载之后）

**改动面（给调度台）**：
| 文件 | 处 | 类型 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` | `:1561` 附近（A191 补 targets 计数）· `:10506-10610`（A431 期望值）· `:10644-10667`（A514① 夹具）· `:10710`（A514② 检测串）· `:11042`+`:11241`（A463 delta）· `:10242`（A422 finally 补摘）· `:11247-11725` 搬家（A462） | 全部只动自检宿主 |
| `Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs` | `:3831` 一行 | 只动自检宿主 |

⚠️ 都只改 `Editor/` 宿主 ⇒ 按铁律 12，复跑**只跑 `BattleScene.Run` + `DeckScene.Run`** 两条即可（不必全套）。

---

## 四、判不了的（如实列，⛔ 没猜）

1. **`A431` 注释里那条预告没发生**：`Editor/BattleScene.cs:10480-10488` 写着「跑完 A418 那三步 ⇒ `battlearena2` 的
   `reparent[]` 会 **5 → 6**（多出 `Embers`）」。实测 `reparent` 仍是 **5**（`EnvBlendables.json` 的
   `sceneStandaloneBuild[battlearena2].reparent` = `BigExplosion`/`Smoke`/`Twinkle`/`Fire Small`/`Launch Smoke`）。
   我查到：`battlearena2_groups.json` 的 `targets[]` 里**有** `Embers`（parent = `…/RocketTrail/BigExplosion`），
   而 `Unity/工具/gen_env_blendables.py:961-971` 那一跳有一道 `is_built_at(cname, cwp)` 闸
   （「原版关着 / 被闸门挡掉 ⇒ 不在我们树里」）。
   **是「闸门仍然挡着 `Embers`」还是「那一步没重跑」我判不了** —— 要跑一次 `python 工具/gen_env_blendables.py`
   看它统计行里的 `scene_standalone_reparent` 才知道。**不影响本块 11 条的判定**（A431 的期望值本来就写 5、实测也是 5）。
2. **`A463` 那 2 个订阅者的逐实例身份**：我用「三处建实例 + 一条 `battle.log:40233` 的 LogError」把它判成
   「主战场 1（活着） + A422 rig 1（已销毁）」——**构成是排除法推的**。若要逐实例坐实，得加一句只读探针
   （打印 `ScreenResolutionChanged.GetInvocationList()` 里每个 `Target` 的名字）再跑一次 `BattleScene.Run`。
   **不影响结论**（无论那 1 个泄漏的是谁，`== 0` 这条断言的前提都不成立）。
3. **`BattleCameraSreenSize.Start()` 在批处理下跑不跑**：本块只依赖「它跑不跑都不改变计数」
   （`RegisterResolutionSignal()` 是 `-=` 再 `+=`，**每实例恒 1**，`Battle/BattleCameraSreenSize.cs:172-178`）⇒ 对本块结论无影响，
   但本仓对「批处理下生命周期跑不跑」**至今没有定论**（源文件自己写着「没有定论」），我**没有**去证。

---

## 五、顺手发现（⛔ 我没改任何东西）

1. 🔴 **A422 rig 真泄漏静态订阅者**（真实、小、可修）：`BattleScene.cs:10239-10244` 的 finally 不摘 `ScreenResolutionChanged`。
   后果不崩、**是静默污染**：抬一次信号就冒一条 `[BattleCameraSreenSize] Initialize 的 … 没接上`（`battle.log:40233` 已经冒了），
   而 `_refsMissingNoted` 闩住 ⇒ 之后再也看不见。建议与第 8 条的 delta 一起修。
2. **`Application.LogCallback` 参数顺序全仓只错这一处**：我 grep 了全部 20 处（清单见第 10 条证据），
   `DeckScene.cs:3831` 是**唯一**取第 2 个参数的。⇒ 只改一行，不需要全仓扫。
3. **`A431` 的注释预告已过期**（`BattleScene.cs:10480-10488`）：那段说「否则 A431 会因为**另一个原因**再红一次」——
   它预告的原因是 `reparent 5→6`，而**实际**红的原因是 `nodes 3/4/1→0`。这条注释现在会把人带偏，
   建议连同第 2~5 条的期望值一起订正（并在那儿补一句「prefab 重建后 `nodes[]` 会走复用支」）。
4. **`A191` 的 `Animation` 计数口径值得顺手统一**：`_stats.animation`（生成器 `工具/gen_arena_groups.py:651-653`）
   是 **nodes + targets + adds 三张表之和**；13 场里只有 `battlearenadarkangels`（2 vs 1）与
   `battlearenatauviorla`（3 vs 2）两场与「只数两张表」不等。补上 targets 计数之后两条都能与 `_stats` 对上，
   顺便可以把写死的 `>= 2` 改成「现读旁挂 / `_stats`」，别再留一个手抄的常数。
5. **建议（不是发现）**：整轮末尾这四段（段头分别在 `BattleScene.cs:10480`(A431) · `:10615`(A514) · `:10811`(A463) · `:11247`(A462)）都在
   `DestroyImmediate(driver)`（`:10439`）**之后**，前三条都自建 rig 所以没事，`A462` 就是被这个位置坑的。
   建议在该文件那段加一句注释纪律：「本轮收尾这几段不许依赖 `driver`（它已在 `:10439` 卸掉）」——
   否则下次再往末尾追加还会踩同一个坑。
