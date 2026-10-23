# D · 战斗层级审查（`W63·层级审查`）

> 只读审查（2026-10-23）。**生产文件一个字节没改**，Unity / git 都没碰。
> 判据优先级：① 原版反编译方法体 `d:/2/tools/decomp_full/` → ② 解包资源序列化字段（`d:/2/新解包资源/assets_full/` 与原始 bundle）→ ③ 我们自己的代码。
> 所有「原版值」都是**实读**，读法写在出处列；查不到的写「没查清」，⛔ 没有一条是「大概是」。
> ⚠️ **我们这一侧的行号是 2026-10-23 现读** —— `BattleDriver.cs` / `BattleScene.cs` 正被并发编辑，行号会漂；按**符号名**（方法名 / 字符串字面量）复核更稳。

---

## ① 结论

**大骨架是对的，卡牌这一侧不是同一个模型。**

对的那一半（别动）：
- **两台相机**的结构照原版做了：透视 `BoardCamera`（depth −1、Base）＋ 正交 HUD 相机（URP **Overlay**，挂在 board 的 cameraStack 上、cullingMask 摘掉 `ArenaLayer`）。原版就是 `BoardCamera`(depth −1, SolidColor) + `UI Camera`(depth 0, **Don't Clear**, mask = UI 层)。
- **HUD 的 uGUI 层**逐条对上：9 个排序层的名字/ID/顺序（`Default < Background < Elements < OverElements < Hand < HandMoving < OverHand < PopUps < Blocking`）照 `TagManager` 建了；根 canvas 的 `ScreenSpaceCamera + 一个 CanvasScaler(1920×1080, Expand)` 对上；各面板的「子画布 + `overrideSorting` + (层, order) + 兄弟序」也对上（含 `Tutorial` 1500 / highlight 601 / pointer 10 这种怪数值）。

不对的一半（**结构级差异**，详见 §②·0）：
- **原版卡＝不透明 3D 网格 + `SortingGroup` + `Canvas.sortingOrder`**；**我们卡＝透明 quad + 每张卡一段渲染队列 + 层内 `sortingOrder`**。
  最重要的后果：**渲染队列是全局的**（且 `renderQueue` 的优先级高于 sortingLayer/sortingOrder），而原版的「每张卡一个 order」只活在**排序层之内**。我们没被盖章的透明件（**所有特效、场地粒子**）都停在 3000 ⇒ **永远画在所有场上卡（2962–2979）之上**；手牌最上面那张恰好也是 3000 ⇒ 与特效同队同 order，只能靠距离。

**最严重的三条**：
1. **卡体渲染态**：原版 `Everguild/Cards/3D Card` = `RenderType Opaque`、无 `Queue` 标签（⇒ **Geometry 2000**）、`Blend One Zero`、`ZWrite 1`、`Cull Back`；我们是 `Queue Transparent(3000)` + `Blend SrcAlpha OneMinusSrcAlpha`（`Assets/CardPresentation/Shaders/Card3D.shader:41-45`）。
2. 🔴 **场上档特效锚点选错了节点**：`CardScript.GetEffectAnchor()` 的场上支 = `BattleCardUI.effectsAnchor`（`+0x1A0`），它的**序列化 PPtr 指向 `CardPrefab/Board Elements/EffectAnchor`**、`localPosition = (0, **1.34**, **−0.128**)`；
   我们的 `EffectAnchorBoard` 却建在 `FaceRoot`（≈`2DCard`）下、`(0, **+0.08**, 0)` —— 那个 `(0,0.08)` 取自**另一颗同名节点** `2DCard/Front/EffectAnchor`（`localPosition (0,0,0)`、带 `SortingGroup order=1`，**不被 `effectsAnchor` 引用**）。
   ⇒ 卡上特效位置 **Y 差 ≈0.406 卡单位、Z 差 0.128**（1080p 下约 27 px）；并且父级差一级（原版是 `2DCard` 的**兄弟**）。
   （附带订正：原版这颗锚点**没有** `SortingGroup` ⇒ 我们「没有 SortingGroup」这一点其实是对的，此前按「只有带 SortingGroup 的那颗才是它」的推断是**误判**。）
3. **卡体阴影开关反了**：原版 `Card 3D` 的 `MeshRenderer` 是 `m_CastShadows=1 / m_ReceiveShadows=1`，我们建成 `Off / false`（`Core/CardView.cs:5894-5895`）。

---

## ② 差异表

> 列：**层级 | 场景 | 原版期望值 | 我们的值 | 差异 | 出处**
> 「原版值」读法：场景/预制体走 UnityPy 读原始 bundle（`d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/*.bundle`，与 `assets_full` 同一构建），方法体走 `decomp_full`。

### ②·0 先说清「不是同一个模型」—— 卡牌这一层（单独一节）

| | 原版 | 我们 |
|---|---|---|
| 卡体几何 | **3D 网格**（`Card 3D`，881 顶点），不透明材质 | 同样有 3D 网格（`BuildBody3D`），但材质**透明队列** |
| 卡在哪个 pass | **不透明 pass（队列 2000）**，写深度、`Blend One Zero` | **透明 pass（3000）**，写深度、alpha 混合 |
| 整卡排序单元 | **`SortingGroup`**（`Board Elements/3DBody`，layer Default / order 0）—— 整卡作为一个排序单位 | **无 SortingGroup**；卡内靠 `Renderer.sortingOrder`（1..60） |
| 每张卡之间的先后 | `BattleCardUI.SetSortingOrder(n)` = `Canvas.sortingOrder = n` **且** `transform.SetSiblingIndex(n)`；场上 n = **玩家 10 / 敌人 0**（落在**2D 卡的 Canvas** 上，场上画面靠的是距离） | 每格一段**渲染队列**（2979…2962），手牌 3000…2980 |
| 卡上特效 | 场上锚点 = `Board Elements/EffectAnchor`（local `(0,1.34,−0.128)`、**无排序组件**）；手牌锚点 = `2DCard` 本身。另有一颗 `2DCard/Front/EffectAnchor`（`SortingGroup order=1`）**不被 `effectsAnchor` 引用**（用途未查清） | 场上锚点建在 `FaceRoot` 下 `(0,+0.08,0)`（**节点与位置都不对**，见 §②·B） |
| 卡与其它透明件 | 卡的 order 在**排序层**里比较 ⇒ 与别的透明件的关系是「层 + order」，可控 | 卡的队列号是**全局**的 ⇒ 与别的透明件的关系是「谁没被盖章谁就 3000」 |

**为什么这条比任何单个队列号都重要**：只要一件透明物（特效、粒子、未来新加的表现件）没被显式盖章，它就在 3000，
于是**恒画在所有场上卡之上**（场上卡 ≤ 2979），而**手牌最上面那张恰好也是 3000** ⇒ 与它同队、同 order 0，
谁压谁由「到相机的距离」决定（不可控）。原版把「卡与卡」「卡与 HUD」交给**排序层**（`Hand` 恒高于 `Default`），
只有「卡与自己锚点上的特效」才是同 key 靠距离 —— 范围比我们小得多。

### ②·A 相机 / HUD vs 世界

| 层级 | 场景 | 原版期望值 | 我们的值 | 差异 | 出处 |
|---|---|---|---|---|---|
| 3D 世界相机 | 战场 | `BoardCamera`：透视、`m_Depth=−1`、`m_ClearFlags=2`(SolidColor)、`m_CullingMask=1137`（bit0 Default·bit4 Water·bit5 UI·bit6 AfterTransparents·bit10 BoardElements）、物理相机 focal 28 / sensor 41.5×24 / gateFit 2 / lensShift.y −0.205 | `BoardCamera 透视（原版值）`：透视、depth −1、SolidColor、`cullingMask = 1<<8` | **掩码不同**（我们自定 `ArenaLayer=8`，整棵 arena prefab 挂 8）；光学参数逐值同 | `Camera/Camera_1461.json`（battlearena1）· `Editor/BattleScene.cs:36357`（`BuildBoardCamera`，行号 2026-10-23 现读，会被并发编辑改动） |
| UI 相机 | 战场 | `UI Camera`：**透视**、`m_Depth=0`、`m_ClearFlags=4`(**Don't Clear**)、`m_CullingMask=32`（仅 UI 层） | 正交 HUD 相机（`LayoutSpace.Apply`，z=−20），设成 URP **Overlay** 并压进 board 的 stack | **等价**（base→overlay 顺序 = 原版 depth −1→0）；差别只在正交/透视（原版画布共面 ⇒ 表现 1:1） | `Camera/Camera_1462.json` · `Core/LayoutSpace.cs:77-80` · `Editor/BattleScene.cs:36459-36464`（Base/Overlay stack） |
| HUD 根画布 | 战场 | 根 `Canvas`：`ScreenSpaceCamera`、`worldCamera=UI Camera`、`planeDistance=100`、`sortingOrder=0`、layer Default、**全场景唯一一个 `CanvasScaler`** | `BattleHud Canvas`：`ScreenSpaceCamera`、worldCamera=HUD 相机、`planeDistance=20.3`、`sortingOrder=0`、唯一 Scaler(1920×1080, Expand) | `planeDistance` 数值不同（我们把它绑到 HUD 的 z=0.3）；其余同 | `Canvas/Canvas_2572.json` · `Battle/HudCanvas.cs:54`、`:98-118` |
| 排序层名册 | 全场景 | 9 层，ID：Default 0 · Background 1954195551 · Elements 229362899 · OverElements 952166135 · Hand 2099470215 · HandMoving 887547619 · OverHand 2007638115 · PopUps 1054366423 · Blocking 2286135553（int 里是 −2008831743） | 同名同 ID 全部建出，顺序一致 | **无** | `globalgamemanagers/TagManager/TagManager_3.json` · `Battle/HudCanvas.cs:481-495` |
| 面板子画布（层/order） | 战场 HUD | 19 个 Canvas，逐条：`BackCanvas` WorldSpace/Default/0/不 override · `FrontCanvas` ScreenSpaceCamera/**OverHand/0**/override · `CemeteryLogPanel` PopUps/**−1** · `BattleSettingsPanel` PopUps/0 · `BattleDoors`(内层 Canvas) PopUps/0 · `UI Error Message Controller` PopUps/**1** · `Tutorial` HandMoving/**1500** · `Tutorial highlight` HandMoving/**601** · `TutorialPointerCombat` OverHand/**10** · `LoadingMenu` **Blocking/1000** · `TooltipManager` **Blocking/0** · 卡组容器 `Background/0、1` | 同名同层同 order（`BattleLogPanel.LogSortingOrder=−1`、`SettingsPanel` PopUps/0、`BattleDoors` PopUps/0、`ErrorMessageBanner` PopUps/1、`TutorialOverlay` 1500/601/10、`LoadingMenu` Blocking/1000、`TooltipManager` Blocking/0、OverHand 各面板 0） | **无**（这张表已经对上了，别再动） | 上列 19 个 `Canvas/*.json` + `MonoBehaviour` 名字 · `Battle/HudCanvas.cs:573`（`SubCanvasRank`）· `Battle/BattleLogPanel.cs:325,955` · `Battle/BattleDriver.cs:22368,23115,23435,21932,23167` · `Battle/TutorialOverlay.cs:310,521-524` |
| 同层同 order 的兄弟序 | 战场 HUD | 由原版 `m_Children` 序定（如 `Safe area FrontCanvas` 的 14 个子件；`FrontCanvas`(3) 在 `BattleDoors`(4) 之前） | `SubCanvasRank` 表按同一序排（PopUps 4 块 + OverHand 10 块） | **无** | `HudCanvas.cs:573-...`（表内注释逐行标了原版下标） |
| 全屏覆盖件 | 战场 | `Aspect Ratio Filler Top/Bottom`（3252×1842，给 `ScreenAspectRatioController` 用）· `Tutorial highlight` = 独立 Canvas(HandMoving/601) · `AboveShader` = **纯 `RectTransform` 容器**（`Safe area FrontCanvas` 第 8 个子件，**没有自己的 Canvas**，排序由父 `FrontCanvas` 的 OverHand/0 决定） | 前两者同名同层同 order；`AboveShader` 我们**单开了一块 OverHand/0 子画布** | `AboveShader` 结构不同但**排序等价**（同层同 order） | 场景 Canvas 表 · `GameObject 682`（`AboveShader` 组件只有 `RectTransform`）· `BattleDriver.cs:21932,23167` |

### ②·B 卡牌 vs 特效

| 层级 | 场景 | 原版期望值 | 我们的值 | 差异 | 出处 |
|---|---|---|---|---|---|
| 卡体着色/队列 | 场上 | `Card 3d Lvl1`：shader `Everguild/Cards/3D Card`，`RenderType=Opaque`、**无 Queue 标签 ⇒ Geometry 2000**、`m_CustomRenderQueue=−1`、`Blend(1,0)`=One/Zero、`ZWrite 1`、`ZTest 4`(LEqual)、`Cull 2`(Back) | `CardPresentation/Card3D`：`Queue=Transparent(3000)`、`RenderType=Transparent`、`Blend SrcAlpha OneMinusSrcAlpha`、`ZWrite On`、`Cull Back` | 🔴 **队列 + 混合不同** | prefab `Material`/`Shader` 实读（`battleprefabs_vfxandmisc`）· `Assets/CardPresentation/Shaders/Card3D.shader:41-45` |
| 卡体阴影 | 场上 | `Card 3D` MeshRenderer：`m_CastShadows=1`、`m_ReceiveShadows=1`、`m_DynamicOccludee=1`、lightProbe/reflectionProbe usage = 1 | `shadowCastingMode=Off`、`receiveShadows=false` | 🔴 **反了** | prefab `MeshRenderer` 实读 · `Core/CardView.cs:5894-5895`（`BuildBody3D`） |
| 整卡排序单元 | 场上 | `Board Elements/3DBody` = **SortingGroup**（layer Default / order 0）；卡内 Sprite/MeshRenderer 各带 order：`MinionLight −1`、`Card 3D 0`、`HealthText 0`、`Armour Background −1`、`Armour Text 1`、`近战/远程攻击字 1`、`SwarmIcon 1`、`DamageIcon 1 / DamageText 2`、`Minion Death Icon 2`、徽标族 0 | 无 SortingGroup；卡内 `sortingOrder`：shadow 1 · art 10 · glow 15 · frame 20 · artFront 25 · textBg 30 · costBg 31 · info 32 · gem 33 · ephemeral 34 · badgePlate 40 · badgeIcon 41 · badgeCounter 42 · armourIcon 50 · stat 60（**只对 `CardFace.Board`**） | **机制不同**：原版「组 + 小序号」，我们「队列段 + 大序号」。⚠️ 我们的数字是**我们自己排的梯子**（语义同向），**不是原版数值** | prefab 实读 · `Core/CardView.cs:1194-1234`（`ApplyBoardSortingOrders`/`SortingOrderFor`） |
| 场卡之间的先后 | 场上 | `CardScript.SetUnitInPlayLayer`：`BattleCardUI.SetSortingOrder(isPlayer ? 10 : 0)`，layer = `GetLayerId("Default")`；`SetSortingOrder` 内部**还** `transform.SetSiblingIndex(order)`。⚠️ 这个 order 落在 `BattleCardUI.canvas`（= `2DCard` 的 Canvas）上，而场上那张 2D 卡是**关掉**的（`ChangeCardToMinion` 只 `ToggleBody3D(true)`；`ShowBoardObjects(true)` 会 `Card2DController.Toggle(!on)`）⇒ **场上画面实际由距离排**：`3DBody` 的 SortingGroup 两边都是 Default/0 | 每格一段队列：`QBoardTop 2979`（我方中格）→ `QBoardFloor 2962`（敌方外侧）；行内「离督军越近越高」，玩家行整行高于敌方行 | 机制不同（**距离 vs 显式号**）；**方向一致**（玩家行 z=−6.655 更近 ⇒ 原版也压敌方；中格离相机最近 ⇒ 原版也最前）。⚠️ 见 §⑤·2：我们注释里「原版所有场卡同一个 order 10」是**错的** | `decomp_full/CardScript__SetUnitInPlayLayer.c` · `BattleCardUI__SetSortingOrder.c` · `BattleCardUI__ChangeCardToMinion.c` · `BattleCardUI__ShowBoardObjects.c` · `BoardLayout.cs:147-178` |
| 卡上特效锚点 | 场上 + 手牌 | `CardScript.GetEffectAnchor()`：状态 {0,1,4,8,9,10,12,16} → `card2DController.transform`（= `2DCard`）；**其余（场上）** → `BattleCardUI.effectsAnchor`（`+0x1A0`）。该字段的**序列化 PPtr 指向 `CardPrefab/Board Elements/EffectAnchor`** ⇒ `localPosition = (0, **1.34**, **−0.128**)`；该节点挂的是一个 follow/align 约束组件，**没有 SortingGroup**。另一颗同名 `2DCard/Front/EffectAnchor`（`localPosition (0,0,0)`，**SortingGroup order 1**）**不被 `effectsAnchor` 引用** | 手牌档 = `FaceRoot`（≈`2DCard`）✔；场上档 = 在 `FaceRoot` 下 `new GameObject("EffectAnchor")`、`localPosition = (0, **+0.08**, 0)` | 🔴 **场上档选错了节点+位置**：按我们的卡根约定（原点在卡心）应 = `From3DBody(0, 1.34, −0.128)` = **(0, −0.3257, −0.128)**；现值 `(0, +0.08, 0)` ⇒ **Y 高 0.406 卡单位、Z 多 0.128**（1080p 约 27 px）。父级也差一级（原版是 `2DCard` 的**兄弟**）。⚠️ 排序方面原版这颗**没有** SortingGroup ⇒ 我们也没有，**这一点一致** | prefab `MonoBehaviour_122102074273340352`（`BattleCardUI.effectsAnchor` 的 PPtr，实测解析）· `decomp_full/CardScript__GetEffectAnchor.c` · `Core/CardView.cs:1389-1413`、`:5602`（`From3DBody`） |
| 卡的 layer（Unity 层） | 场上 | 卡 prefab 的子节点 layer：`3DBody`/`Card 3D`/计数器 = **10（BoardElements）**；相机 mask 含 bit10 | 场卡 `SetLayer(ArenaLayer)` = **8（CustomPostEffectIgnore）** | 层号不同（我们自定 8，靠「只有 board 相机画 8」达到同样隔离） | prefab `GameObject.m_Layer` 实读 · `Board/ArenaSlots.cs:47`（`ArenaLayer = 8`）· `BattleDriver.cs:10326` |
| 特效的默认排序 | 全场 | 特效 prefab 自带 `m_SortingLayer/m_SortingOrder`，并可被 `DepthSorting`（字段 `sortOrder` + sprites/particles/meshes 三张表，`SetSortingOrder` 把每颗设成 `sortOrder + n`）批量改号；**`DepthSorting` 在托管代码里 0 个调用点** ⇒ 只可能由 UnityEvent/动画驱动 | 全包 **13,926** 个 `ParticleSystemRenderer` 统计：`sortingOrder=0` 占 **12,418**（89%）· `1` 805 · `−1` 437 · `2` 136；`sortingLayer=Default` 12,507；`sortingFudge=0` 13,295。`WarpforgeEffectPlayer` **不写** layer/queue/sortingOrder | 我们的特效**不随卡重排**：默认 order 0，队列 3000 | 全包统计（UnityPy）· `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:129-220` |
| 谁压谁（场上卡 vs 自己的特效） | 场上 | 场上可视体 = `3DBody`（SortingGroup Default/**0**）；卡上特效挂在 `Board Elements/EffectAnchor`（**无排序组件**）⇒ 用 prefab 默认（≈Default/**0**）⇒ **两者同 key、由距离排**；卡体不透明 ⇒ 「在卡前」靠深度测试画出来、「在卡后」被剔除。（`SetUnitInPlayLayer` 的 10/0 落在被关掉的 2D 卡 Canvas 上，对场上画面**不生效**） | 卡队列 **≤2979** < 特效 **3000** ⇒ **特效恒后画**（队列优先于 order）；卡体 `ZWrite On` 能挡住「在卡后」的 | **机制不同**（原版距离 / 我们队列）。对**粒子**结果基本一致（都有深度兜底）；差别在「不写深度的卡面覆盖层」与「同深度附近」这两处 —— 这条**没有实拍、判不了严重程度**，见 §④ | 原版：prefab `3DBody` SortingGroup + `effectsAnchor` PPtr + 特效统计；我们：`BoardLayout.cs:147` + `CardView.SetQueue` |
| 谁压谁（手牌 vs 自己的特效） | 手牌 | 手牌锚点 = `2DCard` 本身（`card2DController.transform`）；卡是 canvas，`sortingOrder` 常态 **0**、抬起 **21**；特效 prefab 默认 Default/**0** ⇒ **常态同 order，由距离决定**；**抬起时卡升到 21 ⇒ 确定地压住特效** | 手牌队列 3000…2980；最上面那张 = **3000**，与特效**同队列、同 order 0** ⇒ 由距离决定；其余卡（<3000）特效后画、恒在上 | 常态「不确定」两边**一样**；差别 = **原版抬起档 +21 的确定序我们没有**（见 §③ F6） | `PlayerHand__SetupCardInHand.c`（Hand/0）· `CardScript__ShowCardInHandLayer.c`（Hand/**0x15=21**）· `GetEffectAnchor.c`（手牌支 = `2DCard`）· `HandLayout.cs:315,323` · `CardFan.cs:190` |

### ②·C 粒子 vs 卡体

| 层级 | 场景 | 原版期望值 | 我们的值 | 差异 | 出处 |
|---|---|---|---|---|---|
| 未行动绿光（`CanActParticles` / `RotatingRing`） | 场上 | `m_SortingOrder=0`、`m_SortingLayerID=0`、`m_SortingFudge=−3 / −2`、材质队列 3000；**父节点 `3DBody` 是 SortingGroup** | 参数逐值照抄（fudge −3/−2 保留），并被 `SetQueue` 盖成**该卡的队列段** | 参数同；**差「不在 SortingGroup 内」** | prefab `ParticleSystemRenderer_605582064029899712.json` · `CardView.cs:1153-1181`（`SetQueue` 的粒子那一支）、`:5784`/`:5832`（两颗绿光建档当场盖章） |
| 深度遮挡 | 场上 | 卡体不透明、`ZWrite 1` ⇒ 卡后面的粒子被深度剔除；卡前面的粒子照常画 | 卡体 `ZWrite On`（但**在透明队列里**）⇒ 只要卡体**先画**就能同样剔除；我们卡 ≤2979 < 粒子 3000 ⇒ 成立 | 结果同，但**依赖队列序**（一旦谁把卡队列抬过 3000 或给粒子盖章，就会翻） | `Card3D.shader:43` · `BoardLayout.cs:147` |
| 血痕（`DamageIcon`/`DamageText`） | 场上 | SpriteRenderer order **1** / MeshRenderer order **2**（`3DBody` 组内，见 `已知的坑` 第 44 条） | 未单列（`SortingOrderFor` 里无 `damage` 名；走卡队列段 + 默认 0） | 「血痕被谁压」未逐条对；见 §④ | `已知的坑.md:6906`（第五十七会话 A1674 条） · `CardView.cs:1216-1234` |

### ②·D 场景装饰 vs 单位

| 层级 | 场景 | 原版期望值 | 我们的值 | 差异 | 出处 |
|---|---|---|---|---|---|
| 场地网格 | 战场 | 场景静态网格（`Scenario/Scenario Battle Arena 1 Baked/...`）在 **layer Default(0)**；相机 mask 含 0 | arena prefab 整棵 → **ArenaLayer(8)**；Volume 子件回 Default | 层号自定，隔离方式不同，**结果等价** | 场景 `GameObject.m_Layer` · `Editor/BattleScene.cs`（`ArenaRuntimeLoader` 段） |
| 单位 | 战场 | 卡在 Unity layer **10（BoardElements）**；`3DBody` 的 SortingGroup = sorting layer `Default` / order 0（两边都是）；`SetUnitInPlayLayer` 的 order 10/0 写在 2D 卡 Canvas 上（场上不显示） | 卡在 Unity layer **8**；无 SortingGroup | 隔离方式不同、**结果等价**（都只有一台透视相机画得到） | prefab 实读 · `ArenaSlots.cs:47`（`ArenaLayer`）· `CardScript__SetUnitInPlayLayer.c` |
| 遮挡关系 | 战场 | 场地不透明 → 先画、写深度；卡体不透明 → 同为不透明档，按距离排序 + 深度测试 | 场地不透明；卡体透明但 `ZWrite On` + `ZTest LEqual` ⇒ 被场地正确遮挡 | **无差异**（深度写/测通过） | `Card3D.shader` · 原版场景 |
| 场地粒子 vs 卡 | 战场 | 场景 60 个 ParticleSystemRenderer，只有 2 个非 0（`Battle/TinyExplosion_Close|Far/Shockwave` order 1），其余 0 / Default | 场地粒子在 3000 ⇒ **场上卡（≤2979）之后画**；卡体写深度 ⇒ 被卡挡住的仍剔除 | 等价性靠深度，不靠 order；**卡的覆盖层会被场地粒子盖住**（同 ②·B 的「特效压覆盖层」） | 场景 PSR 实读 · 全包统计 |

### ②·E 卡牌 vs 卡牌（手牌 / 换牌 / 墓地）

| 层级 | 场景 | 原版期望值 | 我们的值 | 差异 | 出处 |
|---|---|---|---|---|---|
| 手牌常态 | 手牌 | `PlayerHand.SetupCardInHand`：`SetSortingLayer("Hand")` + `SetSortingOrder(0)`（同时 `SetSiblingIndex(0)`） | 每张卡一段队列 `queueTop − top`（3000…2980），左 → 右 = 高 → 低（左压右） | 机制不同；**左右方向**未逐字坐实（见 §④） | `PlayerHand__SetupCardInHand.c` · `HandLayout.cs:315,323` · `CardFan.cs:190` |
| 手牌抬起/展示 | 手牌 | `ShowCardInHandLayer`：layer `Hand` + order **21**；`ShowCardBeingDrawn` / `ShowCardInCenter` → layer **`HandMoving`** | 「手牌档」= `CardFace.Hand`，队列仍由 `HandLayout` 给（无单独 +21 档） | **没接**「抬起 / 抽牌中 / 居中展示」的层与 order | `CardScript__ShowCardInHandLayer.c` · `ShowCardBeingDrawn.c` · `ShowCardInCenter.c` |
| 换牌 | 换牌 | `SetMulliganSortingLayer` → `ToggleOverrideSorting(false)`（**不 override**，继承父 canvas）；逐张 order = `MaxCardsInHand − n + 1` / `count − index − 1` | `MulliganPanel` 子画布 OverHand/0；卡沿用队列段 | 未逐条核（换牌区的父链与 override 语义） | `CardScript__SetMulliganSortingLayer.c` · `PlayerHand__SetupCardInMulligan.c` · `MoveCardToMulliganPos.c` |
| 墓地 / 攻击后 | 墓地 | `SetAsCemetery` → layer **`Elements`** + order 参数；`FinishAfterAttack` → layer `Elements` | 未找到对应实现（`CardFeel` 阵亡演出走别处） | **待核**（本条只查到原版判据） | `CardScript__SetAsCemetery.c` · `CardScript__FinishAfterAttack.c` |
| 卡背/牌库 | 战场 HUD | 牌库容器是**独立子画布**：`Player Deck Size Container` = Background/**1**、`Cardback Container` = Background/**0**（override=true）；`CardBackShadow` SpriteRenderer order **701** | 牌库数/卡背在 HUD canvas 族里（未逐条核层号） | **没查清** | `Canvas/Canvas_2578,2567,2584.json` · SpriteRenderer 表 |

---

## ③ 修复清单

> 风险分档：**高** = 会改画面全局关系或影响别的线；**中** = 局部可见变化、需要并排验收；**低** = 纯补值/纯文档。
> 每条都写「判据是哪一条」，⛔ 没有一条是「看着像」。

### F1 · 卡体渲染态（队列 / 混合）—— 风险 **高**
- **文件**：`Unity/MyGame/Assets/CardPresentation/Shaders/Card3D.shader:41-45`
- **怎么改**：`Tags { "Queue"="Geometry" "RenderType"="Opaque" }`、`Blend One Zero`（把 alpha 混合去掉）、`ZWrite On`、`Cull Back`；卡面圆角/描边若靠贴图 alpha，用 `clip(a - cutoff)`（AlphaTest 档 2450）而不是混合。
- **判据**：原版材质 `Card 3d Lvl1`（`m_CustomRenderQueue = −1`）+ shader `Everguild/Cards/3D Card` 的 pass 状态（`RenderType Opaque`、无 Queue 标签、`blend (1,0)`、`zWrite 1`、`zTest 4`、`cull 2`）。
- **⛔ 必须先解掉的耦合**：我们 `CardView.SetAlpha/_Color.a` 靠 alpha 混合做整卡淡出/溶解；改不透明后**淡出会失效**。
  原版的淡出不是靠 alpha —— 它换材质（`BattleCardUI.defaultMaterial / dissolveMaterial / dissolveMaterialLegendary`）。
  ⇒ 本项**要和「溶解材质」那一族一起做**，不能单独改 shader。
- **验收**：卡体渲图并排（`CardFaceProbe` 的场卡档）+ 真机看两张相邻卡的圆角边缘有没有出现硬边。

### F2 · 场上档特效锚点：改用正确的节点与位置 —— 风险 **低**（但影响面广：所有卡上特效的落点）
- **文件**：`Unity/MyGame/Assets/CardPresentation/Core/CardView.cs:1393-1407`（`EffectAnchorBoard` 的懒建块）
- **怎么改**：位置改成 `From3DBody(0f, 1.34f, −0.128f)`（= `(0, 1.34 − Height/2, −0.128)` ≈ `(0, −0.3257, −0.128)`）；
  父级按原版应是 `2DCard` 的**兄弟**（`Board Elements` 与 `2DCard` 同级）⇒ 最稳的做法是挂在**卡根**（`transform`）而不是 `FaceRoot`；若坚持挂 `FaceRoot`，至少要把 `Front` 的偏移算进去并说明（场上倾摆已关，差一个父级只影响倾摆语义）。
- **判据**：`BattleCardUI.effectsAnchor` 的序列化 PPtr → `CardPrefab/Board Elements/EffectAnchor`，`localPosition (0, 1.34, −0.128)`；`CardScript.GetEffectAnchor` 的场上支（状态 ∉ {0,1,4,8,9,10,12,16}）。
- **⛔ 别做**：给这颗锚点加 `SortingGroup` —— 原版这颗**没有**（带 SortingGroup 的是**另一颗**同名节点 `2DCard/Front/EffectAnchor`，不被 `effectsAnchor` 引用）。
- **验收**：抽一张会播「自身锚点」特效的卡（`AnimAnchors` 的 `0 self`）并排比原版参照图；或断言 `EffectAnchorBoard.localPosition == From3DBody(0,1.34,−0.128)`。

### F3 · 卡体阴影开关 —— 风险 **低**
- **文件**：`Unity/MyGame/Assets/CardPresentation/Core/CardView.cs:5894-5895`
- **怎么改**：`mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;`
- **判据**：prefab `Card 3D` MeshRenderer：`m_CastShadows=1`、`m_ReceiveShadows=1`、`m_DynamicOccludee=1`。
- **风险点**：原版场景有 `Directional Light`（`Scenario/Directional Light`）；我们 arena prefab 是否有等效光需一并确认，否则开了也不出影（只是白开）。

### F4 · 特效渲染层随卡走（治本）—— 风险 **高 · 建议先裁口径再动**
- **文件**：`Unity/MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:129-220`（`Play` 装配完 renderer 之后）
- **怎么改（两个候选，只能选一个）**：
  - **A（贴原版）**：给 `Play` 增加「父节点链上若有 `CardView` ⇒ 取该卡的排序号，把 renderer 的 `sortingLayer/order` 设成 `卡 order + 1`」；等价于原版 `SortingGroupAutoOffset`。
  - **B（贴我们现状）**：把特效 `renderer.sortingOrder` 设成 `卡队列段的参考 order`，并把 `renderQueue` 设成卡队列号 +1（保持在卡之上、但仍是该卡自己的段）。
- **判据**：原版 `DepthSorting`（`SetSortingOrder` 把 `sortOrder + n` 落到 sprites/particles/meshes 三张表）；另：`2DCard/Front/EffectAnchor` 那颗 `SortingGroup(order 1)` 说明原版确有「把某组 renderer 抬到卡 canvas 之上」的口子（用途未查清，见 §④·3b）。
- **为什么建议先裁**：A 只在卡是 canvas/sorting 模型时才是「逐位同原版」；我们是队列模型 ⇒ A 会把「卡的 order」和「别的透明件」重新绑在一起。**这是结构选择，不是 bug 修复**。
- **最小止血（可先做）**：至少把「卡上特效」的 `renderQueue` 盖成**该卡队列号 + 1**，让「特效恒在该卡之上、且不越过邻居卡」——这在我们模型里是可判定的。

### F5 · 文档订正（铁律 5）—— 风险 **低**
- **文件**：`Unity/MyGame/Assets/CardPresentation/Board/BoardLayout.cs:135-137`
- **怎么改**：把「`CardScript.SetUnitInPlayLayer` 给场上所有卡**同一个** order 10」改成「**玩家 10 / 敌人 0**（`-(isPlayer != 0) & 10`），并把 `SetSiblingIndex(order)` 一并写上」。
- **判据**：`d:/2/tools/decomp_full/CardScript__SetUnitInPlayLayer.c`。

### F6（待裁）· 手牌抬起 / 抽牌中 / 居中展示的层与 order —— 风险 **中**
- **文件**：`Core/CardFan.cs` / `Hand/HandLayout.cs`（若要做）
- **怎么改**：抬起档给 `Hand` 层语义上的「+21」；抽牌中/居中给 `HandMoving` 语义。我们是队列模型 ⇒ 等价做法 = 抬起时把该卡队列抬到 `queueTop + k`（并保证不超过 `CardDisplayWindow.QChrome` 那一档，避免压住大图窗）。
- **判据**：`ShowCardInHandLayer`（Hand/0x15）· `ShowCardBeingDrawn`/`ShowCardInCenter`（HandMoving）。
- **⚠️ 先确认原版触发时机**（hover? 拖拽? 展示?）—— 本轮没查触发点，见 §④。

---

## ④ 没查清的部分（缺哪条判据都写在后面）

1. **`SortingGroupAutoOffset` 到底挂没挂** —— 类在（`dump.cs:111266`），`Update` 方法体在，但 `assets_full` 全库 grep `"owner"` **0 命中** ⇒ **没有任何序列化实例**。
   缺：运行期实拍/探针（原版已关服，拉不起来）。
2. **原版 `2DCard` canvas 在 inPlay 时是否真的关掉** —— 旁证有了两条（`ChangeCardToMinion` 只调 `ToggleBody3D(true)`；`ShowBoardObjects(true)` 会 `Card2DController.Toggle(!on)`），但 `SetObjectVisibility(state)` 那张状态表是**分支森林**，我没能逐态解完（`0x2002c >> state` 的位掩码只解出 {2,3,5,17} 这一支）。
   缺：`CardStateOptions` 枚举值表 + `Card2DController.Toggle/SetCardVisibility` 的确切语义。
3. **原版「特效 vs 卡面」的最终胜负** —— `3DBody` 是 SortingGroup(Default/0)，特效 renderer 多数 Default/0，**两者同 key**；SortingGroup 与 Canvas 混排的运行时语义我没能静态坐实。
   缺：实拍。
3b. **`2DCard/Front/EffectAnchor`（`SortingGroup order=1`）到底由谁用** —— 它不是 `effectsAnchor`，也不在 `GetEffectAnchor` 的两支里；全库 grep 只在 prefab 里见到这一颗。
   缺：谁实例化特效会 `SetParent` 到它（`Card2DController` / `TraitFrames` / `DisplayTriggerAnim` 之类），或它只是给「2D 卡上的展示动画」预留的锚点。
4. **手牌常态左右谁压谁** —— `SetSortingOrder(0)` 会把 `SetSiblingIndex(0)` 也设一遍（`BattleCardUI__SetSortingOrder.c`），但布局（`CardsHorizontalLayout`）之后还改不改兄弟序没查。换牌那条 `count − index − 1` 明确是「左压右」。
   缺：`PlayerHand` 的手牌重排路径（`MoveCardsInHandToPosition`）是否重设 sibling。
5. **我们 Overlay 相机的 `clearFlags`** —— `Editor/BattleScene.cs:36418` 建相机时设了 `SolidColor`，之后才设成 URP Overlay。URP 下 overlay 应当忽略清屏标志，但**没跑起来验**（本轮不跑 Unity）。
6. **原版 `BoardCamera` 的 `cullingMask` 含 UI(5)，与 `UI Camera`(mask 32) 重叠** —— 若 UI 是 `ScreenSpaceCamera` 画布（只由 worldCamera 画），不冲突；但场景里**还有一批 WorldSpace 子画布（BackCanvas 等）**，理论上会被两台相机各画一次。没实拍，判不了。
7. **原版 VFX prefab 的 `m_SortingLayer` 小整数（1/2/3/6）是什么** —— 1,066 个是 6、167 个是 2、153 个是 1、33 个是 3；这些**不是**本工程 9 个层的 ID（除 0）。可能是旧工程层 ID 残留 / 也可能是 Unity 内置层索引。
   缺：这些 prefab 在原版里实际落在哪层（实拍或 `SortingLayer.NameToID` 逐值对）。
8. **墓地段（`SetAsCemetery` → layer `Elements`）在我们这边对应的实现** —— 没找到，`CardFeel` 的阵亡演出走的是另一条链。
9. **场地装饰件自身的 sortingOrder** —— 场景里 60 个 PSR 只有 2 个非 0；其余 SpriteRenderer 13 个里只有 2 个 `CardBackShadow`(order 701) 与教程箭头（layer 6, order 4/5）。够用但没逐件核「装饰件 vs 卡」的每一对。

---

## ⑤ 顺手发现（只报不改）

1. **任务行里的行号过期**：`W63·层级审查` 记的是 `CardView.cs:5059-5060`（阴影开关），**现读是 `:5894-5895`**（`BuildBody3D` 里）。同一行的 `Shaders/Card3D.shader:41-44` 仍然准（41–45 行）。
2. **`BoardLayout.cs:135-137` 的注释与原版反编译相反** —— 原版是 **玩家 10 / 敌人 0**（`-(*(char*)(p+0x40) != 0) & 10`），不是「所有场卡同一个 10」。这条会误导后续「按距离排」的推论（我们的 9 格 rank 恰好也是这个方向，所以画面没错，**错的是依据**）。
3. **原版 `BattleCardUI.SetSortingOrder` 是「双写」**：`Canvas.sortingOrder = n` **且** `transform.SetSiblingIndex(n)`。我们任何「补 order」的改动都要意识到这一点 —— 原版的序号同时是**兄弟序**。
4. **原版卡 prefab 里有两颗同名 `EffectAnchor`**（`Board Elements/EffectAnchor` 与 `2DCard/Front/EffectAnchor`），只有后者带 SortingGroup；`CardView.cs:1378-1381` 的注释据此判「场上档 = 带 SortingGroup 的那颗」，**但序列化字段 `BattleCardUI.effectsAnchor` 指向的是 `Board Elements/EffectAnchor`** ⇒ 那条推断是**反的**（详见 §②·B 的锚点行）。这也说明：**「同名节点里挑特征最像的那个」这种判法不成立，PPtr 字段才是判据**。
4b. **`CardView.From3DBody()` 已经存在**（`:5602`，`y −= Height/2`），F2 的换算直接复用它即可，⛔ 别再手写一份 `1.34 − 3.3313/2`。
5. **我们的 `ApplyBoardSortingOrders` 只对 `CardFace.Board` 生效**，手牌/收藏/卡池的层仍是 `sortingOrder 0` ⇒ 一旦手牌与大图窗/特效同队列，次序只能靠队列段和距离。这是有意为之（注释写了），但和原版「手牌也有一整套 Canvas order」不同。
6. **`HudCanvas` 已经建了完整的 9 层排序层，且 `Blocking = −2008831743` 的负数写法是对的**（`2286135553 − 2^32`）—— 这条别再当坑重踩。

---

## 附：本轮实读取证的清单（供复核）

| 取什么 | 怎么取 |
|---|---|
| 相机/Canvas/SortingGroup/SpriteRenderer/PSR 的序列化值 | `UnityPy.load(d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/scenes_scenes_battlearena1.bundle)` 后逐对象 `read()` |
| 卡 prefab（`CardPrefab`）整棵树 + 各 renderer 排序 | 同上，换成 `battleprefabs_vfxandmisc_assets_all.bundle`，按 GameObject 名定位 `CardPrefab` |
| 材质队列 / shader pass 状态 | `Material.m_CustomRenderQueue` + `Shader.m_ParsedForm.m_SubShaders[i].m_Tags / m_Passes[0].m_State` |
| 排序层名册 | `assets_full/globalgamemanagers/TagManager/TagManager_3.json` |
| 层名字符串 ↔ 反编译里的 `DAT_` | `GameStaticData.GetLayerId` 的 switch 常量 = **FNV-1a(32)** 哈希：`Default 0x19dc307e · Elements 0x236f8ec · Hand 0x44e67b40 · HandMoving 0xa28797e6 · OverHand 0xa9083d86 · Background 0xbafc0abd · Blocking 0xed81439e · OverElements 0x8dd2eac6`（逐个用 FNV-1a 复算命中） |
| 卡的层/order 状态表 | `decomp_full/CardScript__SetUnitInPlayLayer.c` · `ShowCardInHandLayer.c` · `ShowCardBeingDrawn.c` · `ShowCardInCenter.c` · `MoveBackToHand.c` · `StopShowingCardInHand.c` · `FinishAfterAttack.c` · `SetAsCemetery.c` · `SetMulliganSortingLayer.c` · `PlayerHand__SetupCardInHand.c` · `SetupCardInMulligan.c` · `MoveCardToMulliganPos.c` · `BattleCardUI__SetSortingOrder.c` / `SetSortingLayer.c` |
| 特效 prefab 排序统计 | 遍历 `battleprefabs_vfxandmisc_assets_all.bundle` 的 13,926 个 `ParticleSystemRenderer`（`m_SortingOrder` / `m_SortingLayer` / `m_SortingFudge`） |
