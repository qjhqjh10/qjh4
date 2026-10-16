# T09 · 战斗 HUD 整层换 uGUI —— 让窗口宽高比怎么变都不坏 & 点得到

> **状态**：`✗` 未做（**跨多轮**）。**关联 A 编号**：`A2098`（主件）· `A2097` · `A2092`（残余）· 下游 `A2093`。
> **起于**：2026-10-15（用户报「原版不会被窗口宽高比影响」，正本 §三 症结）。
> **判据正本** = [`战斗复刻_正本.md`](../战斗复刻_正本.md) §2·1 / §2·2 / §2·3 / §2·4 / §2·6 / §3 全节。
> 🔴 **施工面普查（2026-10-16，**先读它**）** = [`资料/普查产出_1016战斗/T09_uGUI迁移面普查.md`](../../普查产出_1016战斗/T09_uGUI迁移面普查.md) —— **件数 / 分步 / 风险全在那份里，本文件只留「做什么 + 判据在哪 + 现状 + 先做哪步」**（⛔ 不抄第二份，铁律 6）。

---

## 一、要做什么（做完之后有什么不同）

把战斗 HUD 从**「世界空间 quad + 正交相机 + 手搓像素命中」**换成**原版那套 uGUI**：
`Canvas`(ScreenSpaceCamera → hudCamera) + `CanvasScaler`(1920×1080/**Expand**) + `GraphicRaycaster` + `EventSystem`，
**卡体走 `Physics2D`**（原版那条命中链），**竞技场 3D / 粒子 / 准星留在世界空间**。

做完之后玩家能感到的三件事：
1. **窗口宽高比怎么变都不坏** —— 现在 4:3 下**整个 HUD 偏大 1.333 倍**（正本 §3·1b）。
2. **画出来的和点得到的永远是同一个东西** —— 现在是**两套尺**（建件用实测宽、读回写死 108）⇒ 非 16:9 下命中框与画面对不上（正本 §3·1）。
3. **点一下只触发一次** —— 现在手搓两条沿 + 15 处模态 `return` 链，与 uGUI `onClick` 并存会**双触发**（静默）。

## 二、判据（逐条带出处；**数值一律去正本/普查取，别在本文件里抄**）

| 判据 | 在哪 |
|---|---|
| 原版那 6 行字段（`m_UiScaleMode=1` · `1920×1080` · **`m_ScreenMatchMode=1`=Expand** · `ReferencePixelsPerUnit=100`）与 `scaleFactor = min(W/1920, H/1080)` 的算法出处 | 正本 **§2·1**（含那次「三路代理说法不一、主对话拿判据裁断」的记录） |
| 宽高比两道保险（`ScreenAspectRatioController` 夹 **4:3–22:9** 收黑边 · `CameraVerticalFramer` 按 aspect 查曲线改 **`sensorSize`**，⛔ 不是 `fieldOfView`） | 正本 **§2·2** |
| 两台相机（战场透视 `fov 46.397` / HUD `fov 40`）、**运行期没人改 aspect** | 正本 **§2·3** |
| **零像素常量 + 物理命中**：`ScreenPointToRay`+`Physics.Raycast` 取板面 · **`Physics2D.GetRayIntersectionNonAlloc` 取卡** · 8 个 `GraphicRaycaster` 的宿主名 | 正本 **§2·4** |
| 原版每帧的「一条 `if…return` 链 vs 我们」对照物（**1 根 `UIstate` int 15 档**） | 正本 **§2·6** |
| **我们已经踩过两次的那条死路**（只改 `LayoutSpace` 的模型 ⇒ 当天回滚，根因是「一个全局映射不可能同时是两个轴的逆」） | 正本 **§3·1c**（+ `LayoutSpace.cs:60-79` 的代码注释） |
| 「只换半边比不换更坏」的逐句结论（小一圈 / 元素互压） | 正本 **§3·1d** |

## 三、我们现状（逐处；**件数一律去普查 §一 取**）

| 面 | 现状 | 出处 |
|---|---|---|
| 布局模型 | 世界空间 quad + **正交**相机；`VisibleHeight` **恒 10**、`Scale = min(1, …)` | `Core/LayoutSpace.cs:49-98` |
| 换算口 | 建件走 `FromPixel`（实测宽 ✓）· 读回走 `ToPixel`/`PxX`（**写死 108** ✗）· **唯一正确的逆** = `ToDesignPixel`/`PxPerWorldX`（`A990②` 建的，战场侧**一直没迁**） | `LayoutSpace.cs:185-273` |
| 命中 | 手搓世界点→局部比矩形：`ImageQuad.Contains` / `Label.Contains` / `HitPaddedRect` + 5 个 `*Hit` 钮口 + 面板族 `Contains` | `Battle/BattleDriver.cs` · `Core/ImageQuad.cs:528` · `Battle/Label.cs:185` |
| 输入派发 | `PollInputEdges` 两条沿 + `Update()` 里 **15 处 `if (HandleXxx()) return;`** | `BattleDriver.cs:16341 / :8174-8260` |
| 换分辨率重锚 | `ReanchorHud` 逐条把 **12 个件**拉回锚点 + 4 个注册表 | `BattleDriver.cs:15731-15769` |
| **缺的前置** | `com.unity.ugui` ✅ 有；🔴 **`com.unity.modules.physics2d` 没有**；**21 个 `.unity` 里 `Canvas`/`CanvasScaler`/`GraphicRaycaster`/`EventSystem` 一个都没有** | 普查 §一末 + 我 2026-10-16 现核（`Packages/manifest.json` 只有 `com.unity.ugui`；`grep -rl CanvasScaler --include=*.unity` = 空） |
| 已有可复用的桥 | `Core/UguiRect.cs`（锚点五元组 → 绝对 px）→ `LayoutSpace.RectCenter` | 普查 §一末 |

## 四、怎么改（**分 8 步；先做第 0 步**）

> 完整描述（每步「做完能验证什么」）→ **普查 §四**。这里只留顺序与第一步。

| 步 | 一句话 | ⚠️ 关键约束 |
|---|---|---|
| **0** | **补 `com.unity.modules.physics2d` 进 `MyGame/Packages/manifest.json`** | **纯前置、零行为改动**；不补它，原版那条命中链的物理前提就没有。⚠️ 这是**现在就能做、且不撞任何件**的一步 |
| 1 | 把「唯一一份逆」铺到战场侧：`HudAbsTex` 的 `/108f` ↔ `HitPaddedRect` 的 `ppu` **成对**改 | **必须成对**，只换半边更坏（正本 §3·1d） |
| 2 | 先**只建不迁**：`BattleHud/Canvas`(ScreenSpaceCamera) + `CanvasScaler(Expand)` + `GraphicRaycaster` + `EventSystem` | 建完对着正本 §2·1 那张 6 行表逐字段核 |
| 3 | 迁最纯的一族：`HudAbs`/`HudAbsTex` 后面的**静态件** | 4:3 与 16:9 两档并排看 |
| 4 | 迁按钮族 + **退掉两条手搓沿** | ⚠️ 一次全改完（`ClickedThisFrame` 只 4 处 / `ReleasedThisFrame` 约 30 处）—— 漏一条 = **记录与触发少一大半而画面正常** |
| 5 | 迁手牌/棋盘命中（`CardInteraction.HitTest`） | 顺手修**「落格比的是卡不是指针」**那条结构差异 |
| 6 | 拆 `ReanchorHud` + 4 注册表 + 12 个 `RefreshLayout` | 拆完换分辨率**不再需要**宽高闸 |
| 7 | 收尾：`ToPixel`/`PxX` 那族读回口逐个退场 | 判据 = `FromPixel` 与 `ToDesignPixel` **成对归零** |

✅🔴 **2026-10-16（第 2/3 步的【第一片】已落地 —— 代码已改，⛔ 未跑自检）**
- **新件 `Battle/HudCanvas.cs`**：`BattleHud Canvas`（**根对象**）＝ `Canvas`(ScreenSpaceCamera → HUD 相机,
  `planeDistance 20.299` ⇒ z=0.299, `sortingOrder 0`) + `CanvasScaler(1920×1080 / **Expand** / refPPU 100)`
  + `GraphicRaycaster`；`EventSystem` **按 §五·补③ 有意不建**（判据没收 + 现在没有可点件）。
  另给了一个 `MakeImage(...)`（**建件的唯一写法**：锚点 = `(x+w/2)/1920`、`sizeDelta = (w,h)`、`PA=0` 拉伸）
  与一对反读口 `CenterPx/SizePx`（自检用，**与 `MakeImage` 成对的逆**）。
- **第一族 = 能量座那 6 枚光点**（`BuildEnergyLights`/`AddEnergyLight` 已改成 uGUI，**旧 quad 路已删**，
  ⛔ 没有两边各留一份）。选它的理由：**只有 1 处断言碰它** · 纯装饰、**没有可点件**（不牵动第 4 步）·
  整族同 z、可整族同批走。
- **断言**（`Editor/BattleScene.cs`）：原来那 6 枚的逐枚矩形改读**设计 px**（容差 1.5/1.0 → **0.5**，
  因为 uGUI 下是精确的）；🔴 **另补了一组「层本身对不对」的断言** —— `renderMode / worldCamera /
  planeDistance / sortingOrder / CanvasScaler 三件 / GraphicRaycaster / 层`
  （**这一组才是本片真正的自证**：迁过去的件自己的断言只能证明「常量进得去」）。
- ⏭ **还没做**：`GraphicRaycaster` 与 `EventSystem` 都还没有可点件用它；**其余 `HudImageZ` 族（约 74 处）
  整族一起迁**才能去掉那个 0.001 的 z 错位；第 4 步（输入）与第 6 步（拆 `ReanchorHud`）未动。

：竞技场 3D 层 · 原版挡板（那是 `BoxCollider` 物理世界）· **手牌/场上卡/卡面**（原版走 `Physics2D`，不是 `GraphicRaycaster`）· 粒子/特效 · 准星（名字自带 `NoCanvas2D`）。
**边界一句话**：**换的是原版 `BattleHud/Canvas` 下那 19 个 Canvas 的内容（HUD/弹窗/卡面窗/门）；`Scenario/*` + `BattleBoardElements/*` + 卡体留在世界空间。**

## 五、做完了怎么自检（**按当时口径**）

- **现在**：只跑 `bash d:/4/Unity/工具/typecheck.sh`（秒级）。⛔ 不跑 `*.Run`、不起 player（现行「先复刻后检查」）。
- **将来统一验收时**（这一件**必须**这样验，别只跑断言）：
  ① **进游戏，把窗口拖成 4:3 / 16:10 / 21:9 三档各截一张**，与原版参照图并排；
  ② 用 `工具/wf_click.py` **真点**：每颗钮点一次 ⇒ 看 `_tmp_view/click_log.txt` **是不是只记了一条**（双触发是本件最典型的静默失败）。
- 🔴 **换层之前必须先解开两处钉死的环境**，否则「全绿 ≠ 对」：
  ① `Editor/BattleScene.cs:23119` 把 `cam.aspect` **钉在 `DesignAspect`**（正本 §3·2 引的 `:22701` 是旧行号）；
  ② 断言里按**旧尺**算期望值的那批（普查 §二 最后一行给了范围：**含 `Hit`/`Contains`/`FromPixel`/`ToPixel`/`ToDesignPixel` 的 458 行**要逐条现看）。

## 五·补 🔴 **2026-10-16：第 1 步的配方【没照 §3·1d 那张表执行】—— 那张表要先重推**

普查 §四 说第 1 步 = 「把唯一一份逆铺到战场侧：`HudAbsTex` 的 `/108f` ↔ `HitPaddedRect` 的 `ppu` **成对**改」，
并按正本 §3·1d 那张两行表：**建件（尺寸）一律再乘 `LayoutSpace.Scale`** · **读回（命中）x 走 `PxPerWorldX`、y 走 `PxY`(108)**。
**我没照它改**，因为按 4:3 逐值推一遍发现**那张表只说了一半**：

| 轴 | 建件（尺寸） | 位置 | 读回（命中） |
|---|---|---|---|
| **x** | 需 ×0.75（= `Scale`）✅ 表里有 | 走 `FromPixel` 归一化 ⇒ **已经**是 1440 px/单位 ✅ | 需 1440（= `PxPerWorldX`）✅ 表里有 |
| **y** | **也需要 ×0.75**（原版 4:3 参考画布是 **1920×1440**，不是 1080 ⇒ 竖直方向同样缩） | 🔴 **也偏 1.333 倍**（我们按 1080 归一化、原版按 1440）—— ⚠️ 正本 §3·1b 那句「位置两边都归一化 ⇒ 一致」**只对 x 成立** | y 那半表里写 108（「恒真」）—— ⚠️ 若建件侧缩了，**y 也要跟着换**才自洽 |

**推导（4:3 · 1080 高）**：原版 `CanvasScaler(Expand)` ⇒ `scaleFactor = min(1440/1920, 1080/1080) = 0.75`
⇒ 参考画布 **1920×1440 画布px** 铺在 1440×1080 屏px 上 ⇒ **1 画布px = 0.75 屏px（两个轴都是）**。
我们：可见高恒 10 世界单位、屏高 1080 ⇒ 1 世界单位 = 108 px，画布 = 1440×1080 px
⇒ 设计 px 直接当屏 px 用 ⇒ **离中心 1.333 倍远、尺寸也大 1.333 倍**（两个轴都是）。
⇒ **正解其实是 §3·1c 那条（把整层按 Expand 重做），而不是「只补一个 Scale 乘子」**；而 §3·1c 已经因为
「一个全局映射不可能同时是两个轴的逆」被回滚过 —— **两条路其实是同一个问题**。
🔴 **执行顺序要改**：**先把「坐标映射模型」这一条判据定死**（是走 Expand + 按画布尺寸归一化，还是别的），
**再**回来动 `Px` / `HitPaddedRect`。⛔ **别按现在那张表成对改** —— 只改一半比不改更坏（正本 §3·1d 自己说的）。
⚠️ 本条**没有改动任何代码**（只记推导）；判据来源 = 逐值手推 + `LayoutSpace.cs:80-98` / `178-219` 现读。

### 五·补② 🔴 **主对话裁断（2026-10-16 · 定口径）**

**第 1 步（补斜率）取消。** 理由两条：
① **它修不对** —— 上表逐格推下来，「位置」那一格 §3·1d 根本没管，而位置在 4:3 下同样偏 1.333 倍；
② **它会用掉唯一的安全边界** —— 现在「16:9 下逐位不变」是我们**仅有的**那条不变量（现存断言全钉在 16:9），
把斜率改成一个「只在非 16:9 生效、却又不是终态」的中间物，等于拿它换一个**错的中间态**。
⇒ **直接走原版那条路**：`m_AnchorMin/Max` 锚点体系（= §3·1c 早就写下的结论「正确的修法是 `A2098`」）。
⛔ **别再回来补斜率。**

**第 2 步（先只建 `Canvas`/`CanvasScaler`/`GraphicRaycaster`）也一并调整**：**不单独建**。
🔴 依据有两条硬约束：
- ⚠️ **不能把 `Canvas` 挂到现有 HUD 根上** —— `Canvas` 会**接管自己的 `RectTransform`**（`ScreenSpaceCamera` 档
  还会把它摆到 `planeDistance` 处）⇒ 我们那棵「世界单位、原点在屏幕中心」的 HUD **会被整体搬走**，画面当场全错。
- 所以要建只能是**另起一个空对象** —— 但**没有任何件挂在它下面时，它就是一个死件**
  （本仓既有口径：「⛔ 别建死件」，见 `任务/T04` §四 第 5 条）。
⇒ **改成：`Canvas` 与「第一个迁过去的件」同批建**（建完立刻有东西用它，并且当批就能对着
  正本 §2·1 那张 6 行表 + `!u!223` 逐条核）。原版字段已读好备用：
  `m_RenderMode = 1`（ScreenSpaceCamera）· `m_PlaneDistance = 100` · `m_Camera` = hud 相机 ·
  `m_SortingOrder = 0`（`bundle_scenes_scenes_battlearena2/Canvas/Canvas_2762.json` 实读）。

## 五·补③ 🔴 **主对话定的「坐标与层级模型」（2026-10-16 —— 这一步不动代码就推不下去，先定死）**

### 一、`Canvas` 一建出来，「我们那套 px 常量」就能 1:1 用（⇒ 4:3 的错自动被修掉）

逐值推一遍（正交的 `ScreenSpaceCamera`）：`Canvas` 的 `RectTransform` 由 Unity 撑成**相机的像素矩形**
⇒ **1 canvas 单位 = 1 屏 px**；而正交相机可见高恒 **10 世界单位** ⇒ 1 单位 = `10/屏高` 世界单位。
16:9（1920×1080）下 ⇒ 1 单位 = 10/1080 = **1/108 世界单位** —— **正是我们现在的 `HudImageZ` 那一套 108 px/单位**。
⇒ **迁移后 `w/h/cx/cy` 这些设计 px 常量原样搬进 `RectTransform`（锚点 + `sizeDelta`）就是对的那一档。**

再加 `CanvasScaler(1920×1080, **Expand**)`：`scaleFactor = min(W/1920, H/1080)`。
- **16:9 ⇒ `scaleFactor = 1` ⇒ 与今天【逐位相同】**（现存断言全在 16:9 ⇒ **迁移不会让它红**）。
- **4:3（1440×1080）⇒ `scaleFactor = 0.75`** ⇒ 「1920 宽的设计稿」正好铺满 1440 ⇒ **正是原版口径**
  （对照：今天在 4:3 下**整体偏大 1.333 倍** = 正本 §3·1b 那条）。

🔑 **这就是「唯一正确的逆」的落地方式**：**换层之后 `FromPixel`/`ToPixel` 那一对不再需要**
（px 常量活在 reference space 里，由 `CanvasScaler` 统一缩放）—— 即普查第 7 步「成对归零」的自然结果。
⛔ **别再去改 `LayoutSpace` 的斜率**（那是 §五·补② 已取消的第 1 步）。

### 二、`Canvas` 与现有 quad 怎么互相插队 —— **靠 `planeDistance`，不是 `sortingOrder`**

我们的 HUD 是**世界空间 quad**（`LayoutSpace.Apply` 把相机摆在 `z = −20`、看 +Z），分层**靠 z**（`HudImageZ = 0.3`；
`SetWorldHeight` 的 doc 明写「保留 z —— HUD 靠它分层」），面板才额外用 `SetRenderQueue(4000)` 逃出去。
而 `Canvas` 的 `planeDistance` **就是它离相机的距离** ⇒
🔑 **`planeDistance = 目标 z + 20`** ⇒ canvas 落在**同一套 z 排序轴**上，与 quad 正常插队。
- 例：HUD 静态图族在 `HudImageZ = 0.3` ⇒ `planeDistance = 20.3`；
  背景 `BattleBackdrop.z = 6` ⇒ 26；进战斗黑场在 `z = −4.5`（且队列 4100，本来就压一切）。
- ⛔ **不要去动 `sortingOrder` 来插队** —— 一旦用 `sortingOrder`，它会**先于** `renderQueue` 比较
  ⇒ 那块 canvas 会**整块**跳到所有 quad 的前面或后面（面板当场被盖住）。
- ⚠️ **推论（这条决定「能不能一族一族迁」）**：**同一个 z 上的族必须【整族同批迁】** ——
  只迁一半的话，canvas（z=0.3）与剩下的 quad（z=0.3）**同 z 同队列**、先后**未定义**。
  另：**按钮族不能先于输入迁移**（迁了就不再响应 —— 见普查第 4 步）。

### 三、三条「建 `Canvas` 时的硬约束」（判据 → `普查产出_1016战斗判据/T09b_uGUI拦路判据.md`）

1. 🔴 **`CanvasScaler` 只挂在【根】Canvas 上**，**子 Canvas 一个都不加**（原版逐场现读：全场景只有一个）。
2. ⛔ **不能把 `Canvas` 挂在 HUD 根下**（`Canvas` 会接管自己的 `RectTransform`，`ScreenSpaceCamera` 档还会把它
   摆到 `planeDistance` 处 ⇒ 整棵 HUD 被搬走）⇒ **建成【根对象】**（不挂父），
   同时也避开「`hudRoot` 是**反向补震**用的」那件事（挂在它下面会被补偿两次）。
3. ⏸ **`EventSystem` 这一轮【不建】** —— 两个理由：① **判据没收**（`T09b` 没查它的字段；
   按铁律 2 **不猜**）② 现在**没有可交互件**，建了就是**死件**（本仓既有口径）。
   ⇒ 等**第 4 步（迁按钮族）**时与第一个可点件**同批**建。
   ✅ `GraphicRaycaster` 随 `Canvas` 一起建（它是 `Canvas` 的组件、不占独立对象）。

### 四、第 2/3 步的执行形态（据此改动）

- **建层与「第一族」同批**：`BattleHud/Canvas`（根对象）+ `CanvasScaler(1920×1080/Expand/refPPU 100)`
  + `GraphicRaycaster`，`renderMode = ScreenSpaceCamera` · `worldCamera = LayoutSpace.Cam` ·
  `planeDistance = 20.3` · `sortingOrder = 0`；
  **同批迁**普查第 3 步那一族（名牌 / 能量 / 时钟 / 牌堆底板 / 六枚 Glow —— 它们在 `HudImageZ` 同一 z 上）。
- ⚠️ **同批要改的地方**（⛔ 不能漏，漏了就是「只改半边」）：
  ① `_hudImages` / `_hudImageSpots` 注册表与 `ReanchorHud` 里对应那几行（迁走的件**不再需要**重锚）；
  ② `BattleScene.cs` 里碰这批件的断言（`EnergyLights` / `HudImage*` **15 处**）—— 它们读的是 `ImageQuad` 的
  `WorldW/H`，改成读 `RectTransform` 的等价量（**顺带把期望值从「旧尺」换成 reference px**）。
  🔴 **这一步必须跑一次 `BattleScene.Run` 才知道红没红**（2620 条 `Check`）——
  ⇒ **与「先复刻后检查」的口径冲突，要先跟你确认**（见下面那条挂起项）。

### 五、换层期间断言怎么办（**照现行口径办，只是把代价记下来**）

- 🔴 **事实**：迁一层 ⇒ 那一层的断言（读 `ImageQuad` 的那批、以及普查 §二 点出的
  **458 行含 `Hit`/`Contains`/`FromPixel`/`ToPixel`/`ToDesignPixel`**）**必须同批改**。
- ✅ **现行口径 = 不跑**（铁律 12 第八次「先完全复刻，复刻好了再检查」；判别句：
  「跑 `BattleScene.Run` 看红没红」= **验证我刚改的对不对** ⇒ **停**）。
  ⇒ **本件照办**：只跑**秒级类型检查**，⛔ 不跑 `*.Run`、⛔ 不起 player。
- ⚠️ **如实记下这个代价**：普查「这一步最大的风险」自己写着「**只改半边在 16:9 下逐位等价、全绿，
  坏只在别的宽高比上现形**」—— 而 **16:9 恰好是断言唯一覆盖的那档**
  （`BattleScene.cs:23119` 把 `cam.aspect` 钉死在 `DesignAspect`）⇒ **换层期间基本没有自动反馈**。
  ⇒ **8 步走完之后那次统一验收，必须按 §五 那两条来**（三档宽高比截图并排 + `wf_click.py` 真点一遍），
  ⛔ 不能只跑断言就收口。
- 📌 唯一的例外**不是本件**：**改了自检宿主自己**（`Editor/BattleScene.cs`）时，如果哪一步需要确认
  「我改的断言还编得过」—— 那由**秒级类型检查**兜（它管编不编得过），够用。

## 五·补④ 🔴 **`ImageQuad` → uGUI 的逐项映射表**（2026-10-16 —— 迁那 ~74 处之前先照这张表，⛔ 别各写各的）

> 这张表是**给剩下那一族的**（`HudAbs`/`HudAbsTex`/`HudImage`/`HudImageTex` 的调用点）。
> 逐项都标了「uGUI 那一侧怎么写」与「⚠️ 容易错在哪」——**漏一条就是一处静默错**。

| `ImageQuad` 这一侧 | uGUI 那一侧 | ⚠️ 容易错在哪 |
|---|---|---|
| `Create(parent, tex, pos, **worldHeight**, anchor, name)` | `sizeDelta = (h × **aspect**, h)`，`aspect = tex.w/tex.h` | 🔴 **是「按【高】定、宽由贴图比例来」——⛔ 别当成正方、也别当成 `(w,h)` 照抄**。（`MakeImage` 现在收的是 `(w,h)` ⇒ **调用点自己按比例算出 w**，或另加一个「按高定」的重载） |
| `SetAspect(a)`（**盖掉**贴图比例） | `sizeDelta = (h × a, h)` | 只在「框比例 ≠ 贴图比例」时出现（原版 `PA=0` 拉满 / `PA=1` 内接）；⛔ 别一律套贴图比例 |
| `SetTint(c)` | `Image.color = c` | —— |
| `SetTexture(t)` | `Image.sprite = Sprite.Create(t, …)` | ⚠️ `Image` 要的是 **`Sprite`**、不是 `Texture2D` |
| `SetMaterial(m)`（牌堆 SDF 那一族） | `Image.material = m` | 原版 SDF shader 那条链不变 |
| 位置 `SetAnchorPosition(x01, y01)` | `anchorMin = anchorMax = (x01, y01)` + `anchoredPosition = 0` | ⚠️ **`y01` 从【下】算**（`LayoutSpace.ToWorld` 的约定）——uGUI 锚点也是从下算 ⇒ 直接对应；⛔ 别把「从上看」的 y 直接塞进去 |
| `SetRenderQueue(q)`（**面板那一档 4000**） | ⚠️ **uGUI 没有这个口** ⇒ 用**子 Canvas + `overrideSorting` + `sortingOrder`** | 🔴 **原版就是这么干的**（T09b：`CemeteryLogPanel` 靠 `overrideSorting` 脱离父层）⇒ 面板族要**各起一个子 Canvas**，⛔ 别想在根上用一个 `sortingOrder` 摆平 |
| `WorldW` / `WorldH`（自检读回，**世界单位**） | `sizeDelta × canvas.scaleFactor ÷ 108` | ⚠️ 自检要改读**设计 px**（`HudCanvas.SizePx`）⇒ 期望值也要跟着从「世界」换成「px」 |
| `Contains(worldPos)`（命中的 4 处） | `GraphicRaycaster`（第 4 步） | 🔴 **在那之前这些件【不能迁】** —— 迁了就点不到 |
| 进 `_hudImages` / `_hudImageSpots`、被 `ReanchorHud` 拉回 | **不进** —— 锚点由 `RectTransform` 自己管 | ⚠️ 迁一件就**同批**从注册表与 `ReanchorHud` 里摘掉它，否则「一半跟画布走、一半跟世界走」 |
| 挂在 `root`（HUD 根）下 | 挂在 **`HudCanvas` 的根 `RectTransform`** 下 | ⛔ 别挂在 `hudRoot` 下（那是反向补震用的） |

**两族的分界线**（⛔ 别混）：**纯视觉件**（无名牌/能量/时钟/牌堆底板/六枚 Glow 那一族）可以按本表迁；
**带 `Contains` 的**（按钮族 / 手牌 / 棋盘）**必须等第 4/5 步**。

### 五·补⑤ **第 3 步那一族的【执行底账】**（2026-10-16 盘出来的，⛔ 迁移前拿它当清单）

**① 量**：`HudAbs` **12** · `HudAbsTex` **4** · `HudImage` **11** · `HudImageTex` **25** = **52 个调用点**，
落在 **38 个字段**上（`_myPlate` / `_enemyPlate` / `_titleBgMe|Foe` / `_myPile` / `_foePile` /
`_myDeckPlate` / `_foeDeckPlate` / `_myDeckLight` / `_foeDeckLight` / `_myDeckSizePlate` / `_foeDeckSizePlate` /
`_handPlate` / `_myFaithIcon` / `_foeFaithIcon` / `_myStoneIcon` / `_foeStoneIcon` / `_myStoneGem` / `_foeStoneGem` /
`_myQuestIcon` / `_foeQuestIcon` / `_myQuestJoin` / `_foeQuestJoin` / `_myEnergyPlate` / `_foeEnergyPlate` /
`_energyAccumMe` / `_energyAccumFoe` / `_skullIcon` / `_overtime` / `_tipBg` / `_hudAvatarMe` / `_hudAvatarFoe` /
`_settingsBtn` / `_chatBtn` / `_cemeteryBtn` / `_cameraResetBtn` / `_allianceBtn` / `_offensiveBtn` / `_endTurnBg`）。

**② 🔴 分界线：带命中的【不能】先迁**（迁了就点不到 —— 见 §五·补④ 最后两行）。
已认出的**带命中那一批 = 6 个**：`_settingsBtn` · `_chatBtn` · `_cemeteryBtn` · `_cameraResetBtn` ·
`_allianceBtn` · `_offensiveBtn`（＋ `_endTurnBg`，它走 `HitEndTurn`）。
⇒ **其余 ~31 个是纯视觉**，可以按 §五·补④ 那张表**整批迁**。

> ⚠️ **这张分类【别信脚本】**：2026-10-16 用「同行的 `Contains` / `HitPaddedRect` / `<字段>Hit`」自动筛，
> **当场漏判 `_cameraResetBtn`**（它的命中在 `CameraResetButtonHit:3596` 那一支里、不在同一个表达式上）
> ⇒ **每一件都要自己回一眼「它有没有被某个 `*Hit` / `HandleXxx` 读」**。
> 🔑 现成的五个命中口（普查给过）：`CameraResetButtonHit:3596` · `SettingsBtnHit:3747` · `ChatBtnHit:3764` ·
> `CemeteryBtnHit:3780` · `AllianceButtonHit:3841`，再加 `HitEndTurn:7895`。

**③ 整批迁的时候【同批】要动的三处**（⛔ 漏一处就是「只改半边」）：
1. **建件那 4 个助手**（`HudAbs`/`HudAbsTex`/`HudImage`/`HudImageTex`）—— 迁走的那批改走 `HudCanvas.MakeImage`；
2. **`_hudImages` / `_hudImageSpots` 注册表 + `ReanchorHud`**（迁走的件**不再需要**重锚 ⇒ 同批摘掉）；
3. **`BattleScene.cs` 里碰这批的断言**（普查点出的 **458 行**含 `Hit`/`Contains`/`FromPixel`/`ToPixel`/`ToDesignPixel`）——
   期望值要从「世界坐标」换成「**设计 px**」（`HudCanvas.CenterPx/SizePx`）。

**④ 迁完那一批之后**，`HudCanvas.PlaneDistance` 那个 **20.299 可以改成 20.3**（那 0.001 的错位是为了
「已迁的排在没迁的前面」而临时留的）。

**⑤ 🔴🔴 2026-10-16 盘底账时查出的【计划变更】—— 第 3 步不能单独做。**
- **事实**：那一族的**纯视觉件**与**按钮件**原本**同一个 z（`HudImageZ = 0.3`）**，
  而 canvas 只能落在**一个** z 上（0.299 = **在全部 0.3 之前**）⇒ 迁走的件会**盖住**没迁的按钮件
  （按钮件里 `_settingsBtn`/`_offensiveBtn`/`_cemeteryBtn`/`_chatBtn` 与 `_foeFaithIcon`/`_foeStoneGem` 那些
  **同在屏幕右/左边缘、会重叠**）⇒ **按钮会「看得见的地方没了、命中还在」**（半死不活）。
- **反过来也不成**：把 canvas 放到 0.301（在 0.3 之后）⇒ 已迁的件会被**所有**没迁的件盖住。
- ⇒ **第 3 步（纯视觉族）与第 4 步（按钮族 + 退两沿）必须【合批】**，⛔ 不能先做 3 再做 4。
  这也与普查 §二 那条硬约束一致（「同一个 z 上的族必须整族同批迁」）—— 只是当时**没认出「按钮也算同族」**。
- ⚠️ **本来会踩的**：原计划写的是「第 3 步迁最纯的一族（4:3 与 16:9 两档截图并排看）」——
  **那一步按现在的分法做出来就是「按钮被盖住」**，而且因为断言钉在 16:9、**它不会红**。
- 🔴 **本件（`T09`）在那之前【不往下迁】**：理由 = 每一片都是**看不见的改动**，
  而逐片迁又**必然**经过上面那个半死不活的中间态 ⇒ 攒到最后一次做、一次验收。

## 五·补⑥ ✅ **2026-10-16：那一族已接上适配层 `HudPiece`（第 3+4 步的【第一步】，代码已改、编译 0 错）**

**做了什么**：新件 `Battle/HudPiece.cs` —— 一个「件」**要么**旧的 `ImageQuad`（世界空间 quad）、
**要么** uGUI 的 `RectTransform + Image`，**二选一**（⛔ 铁律：**绝不允许两边都建**，见那份文件头）。
把 `HudAbs` / `HudAbsTex` / `HudImage` / `HudImageTex` **四个建件助手**与**那 39 个字段**的类型
从 `ImageQuad` 换成 `HudPiece`；连带改了：`HitPaddedRect` · `ApplyTitle` · `FitDeckPile` · `MakeDeckSdf` ·
`SetDeckLight` · `HitTip` · `PosOf` · `SetVis`/`Vis`（新增 `HudPiece` 重载）· `_hudImages`/`_hudExtras`
（`List<HudPiece>`）· `_playedPips` · `MyPileQuad`/`MyDeckSdfQuad`/`FoeDeckSdfQuad`。

🔑 **为什么这么做（这是本步的全部价值）**：那 52 处建件在别处被用了 **~140 次**
（`.Texture` 32 · `.gameObject` 32 · `.transform` 25 · `.WorldW/H` 28 · `.SetAspect` 8 · `.SetTint` 7 ·
`.Contains` 3 · `.Tint` 2 · `.SetTexture` 2 · `.SetAnchorPosition` 1）。
直接换字段类型 ⇒ 那 140 处**每处都要改**、而且**每处都是看不见的改动**。
用适配层挡住 ⇒ **140 处一行没改**（只动了 6 个调用的类型），改动面收成 **1 个新文件 + 6 个签名**。

✅ **本步【零行为改动】**：`HudPiece` 现在**全部包的是旧 quad**（`HudPiece.Of(q)`）——
所以 16:9 / 非 16:9 都与改之前**逐位相同**。这是有意的：**先把缝切出来、再翻件**。
⛔ **别跳过这一步直接翻**（那就是把 140 处不可见的改动一次压进来）。

**✅ 2026-10-16 【翻件也做了】（同日第二步，编译 0 错）**：
`HudImageTex` / `HudAbsTex`（连带 `HudAbs` / `HudImage`）**已改成建 uGUI 件** —— 四个助手一改，**整族一起翻**。
- 建件口 = **`HudCanvas.MakeAnchored(parent, tex, name, x01, y01, pivot, hPx, wPx, z, color)`**
  （`ImageQuad.Create(..., pos, worldHeight, anchor, name)` 的对应物：**高按世界单位 ×108 取设计 px、宽按贴图比例**）。
- 🔑 **兄弟序按 z 降序排**（`HudCanvas.Add`）—— 旧那套靠 z 分层、uGUI 靠兄弟序，映射过来**逐件对得上**。
  ⛔ **别只用创建顺序**（那一族里有 `HudImageZ - 0.02` / `- 0.065` 这种彼此有先后的件）。
  `HudDecorZ`（0.6）那块装饰底板因此自动落到**最前 = 画在最底下** ✓。
- 🔴 **同一个 z 上的族**：现在**整族都在画布上**了 ⇒ 那个 0.001 的临时错位（`PlaneDistance 20.299`）
  **可以改成 20.3**（⚠️ 本会话**没改**，留到下一次一起动 —— 它与 `HudImageZ` 那个常量是同一处判据）。
- 连带改掉的口（翻完之后它们**全成了错的**，逐处改）：`PlaceDeckLights`（`localPosition` → **`HudPiece.NudgeWorld`**）·
  `FitDeckPile` 的偏移（同上）· `_myDeckPlate/_foeDeckPlate` 的 `+0.02` z 微调（→ **`HudPiece.NudgeZ`**）·
  几个读回（`ToNormalized(transform.localPosition)` → **`HudPiece.Anchor01()`**；`.localPosition.z` → **`HudPiece.OrderZ`**）。
  ⛔ **`BuildOneAspectFiller` 那一件【没迁】**（不在那一族里）⇒ 它仍走旧 `ImageQuad` 那套。
- 🔴 **`HudCanvas.Ensure` 现在对 `cam == null` 出声并返回 null** ⇒ 调用点**不建那一件**
  （⛔ 绝不退回去建世界空间 quad —— 半迁移态比不迁移更坏，而且**在 16:9 下看不出来**）。

🔴🔴 **翻件当场抓到的两个【同类单位坑】（已修，但这条要记住 —— 它是本件最容易静默出错的地方）**：
翻完之后，凡是拿「**世界单位**」去比「**件的局部坐标**」的地方**全成了错的** ——
**旧 quad 的局部单位 = 世界单位；uGUI 的局部单位 = 设计 px**（差 108 倍）。
1. **`HitPaddedRect`**：`ppu` 原来恒取 `1/Px(1f)` = 108 ⇒ 翻件后命中框会**大 108 倍**（画面完全正常）⇒
   改成 `q.IsUgui ? 1f : 1f / Px(1f)`。
2. **`CameraResetButtonHit`**：阈值 `Px(px)` 是世界单位、而 `l` 是局部坐标 ⇒ 同样差 108 ⇒ 加了按模式的换乘 `k`。
✅ **已经核过【不需要改】的**：`WorldW/WorldH * 108f`（`HudPiece.WorldW` 两种模式都返回**世界单位** ⇒ 对）、
`DOPunchScale`（无量纲）、`gameObject.SetActive`、`transform.position`（两种模式都是世界点）、
`q.Contains(...)`（走 `GetWorldCorners` ⇒ 两种模式都对）。
🔑 **规矩**：以后凡是对「件」做**局部坐标/单位**运算的地方，**先问它是哪一种模式**（`HudPiece.IsUgui`），
⛔ 别默认 108 —— 这一条**在 16:9 下不会红**，只有真机量才看得见。

## 五·补⑦ 🔴 **翻完图片族之后暴露的【下一批必须做的】= 标签族**（2026-10-16 查实）

**状态**：`PlaneDistance` 已经从临时的 **20.299 改回 20.3**（整族都在画布上 ⇒ 不再需要那个 0.001 错位），
断言同步改了。typecheck 0 错。

🔴 **问题**：现在 **图在画布上（跟 `CanvasScaler` 缩放）、字还是世界空间 quad（不跟）** ⇒
**非 16:9 下两者会不一致**（这正是普查警告的「只换半边比不换更坏」）。
量出来的系数：世界空间那套「1 世界单位 = 屏高/10 px」⇒ 一个 `f px` 的字画出来是 `f × 屏高/1080` **屏 px**；
画布那套是 `f × scaleFactor`。⇒ **两者之比 `R = scaleFactor × 1080 / 屏高`**：
- 16:9（1920×1080）⇒ `1 × 1 = 1` ⇒ **逐位不变**（所以本会话的改动在 16:9 下看不见）；
- 4:3（1440×1080）⇒ `0.75`；16:10（1920×1200）⇒ `0.9`。

🔴🔴 **【想用「一个过渡系数先把标签抹平」这条路 —— 一开始判「走不通」，2026-10-16 当天订正为「走得通，而且是最省的那条】：
- 原判：世界空间那套 `Label` 的尺寸**是按整数档量化的**（`Battle/Label.cs:998`
  `scale = Mathf.Max(1, Mathf.RoundToInt(approx / CapHeightWorldOfScale))`）⇒ **表示不出 0.75**。
- ✅ **订正**：那半句**只对「拿它去乘尺寸参数」成立** —— 正解是**乘 `localScale`**：
  它是**浮点**、不会被 `RoundToInt` 吃掉，而且**等价于画布本身在做的那件事**（缩一个已在的场景子树）。
  ⇒ **不必重写 `Label` 那 1700 行、也不必把 57 个建字点整批换 `TextMeshProUGUI`。**

✅ **2026-10-16 已实现**（代码已落地、typecheck 0 错）：
- 新量 **`LayoutSpace.HudTextScale`**（`R = scaleFactor × 1080 / 屏高`，推导写在那个属性的 doc 里）：
  **16:9 ⇒ 1**（逐位不变）· 4:3 ⇒ **0.75** · 16:10 ⇒ **0.9** · 21:9 ⇒ 1。
- **`Hud()` 建完就设**（那是 HUD 建字的**唯一**口）· **`ReanchorHud` 换分辨率时重算**（这个系数**跟分辨率走**，
  ⛔ 别只在建的时候设一次）· **`PlaceHandPlate`** 跟着用**缩放后的**宽高（否则那块板会与字错位 25%）。
- 🔴 **有意【不】缩的三处**（写进 `HudTextScale` 的 doc 了，⛔ 别顺手全加上）：
  ① **`_overtimeSplashText`** —— 它那一整组（含它自己的两块 quad）**都还没迁**，内部的字得跟**自己的底板**同尺才自洽；
  ② **卡面文字**（卡是**世界物件**，不跟画布缩放走）；③ **还没迁的面板内部**的字。
- ⚠️ **`Label.WorldW/WorldH` 仍是未缩的原始值**（它们量的是 TMP 的字形尺寸）⇒ 谁要用「画出来的尺寸」
  得自己乘 `transform.localScale.x`（`PlaceHandPlate` 就是这么做的）。**16:9 下两者相等**，
  所以断言在那个档上不受影响。

## 五·补⑧ 📌 **翻件之后「还没迁」的都是什么**（本轮收口时的边界）

| 已迁（在画布上，跟 `CanvasScaler` 缩） | 还没迁（世界空间 quad / 旧尺） |
|---|---|
| **HUD 的图那一族**（52 处建件 / 39 个字段：名牌底板 / 能量 / 时钟 / 牌堆 / 图标 / 头像 / 标题条 / 六枚 Glow…） | **HUD 的字**（靠 `HudTextScale` 才跟上缩放，见 §五·补⑦）· **面板族**（设置 / 日志 / 结算 / 换牌 / 选牌 / 放大窗…）· **手牌与场上卡** · **加时 splash 那一组** · **`AspectFiller`** · 粒子/特效 · 准星 |
| 位置 = 锚点（归一化）· 尺寸 = **设计 px** | 位置 = `LayoutSpace.ToWorld` · 尺寸 = `到 108 px/世界单位` |

🔑 **判据（下次继续时照这个切）**：**「它是不是画在 HUD 正交平面上的、非卡非面板的静态件」** ——
是 ⇒ 归这一族的下一批；不是 ⇒ 各自有各自的批次（面板族要连它内部的字一起、卡体走 `Physics2D` 那条）。


> 🔴 **2026-10-16 订正（铁律 5）**：本文件原来在 §五·补⑧ 之后还留着一段**没删干净的旧草稿**，
> 它写「非 16:9 **只能靠**把标签族也迁到 uGUI 才能修（uGUI TMP 的 `fontSize` 是浮点）」——
> **与它上面 §五·补⑦ 自己的订正矛盾**（那里已判「乘 `localScale` 这条路**走得通**」，并已落地
> `LayoutSpace.HudTextScale`）。**错因**：那半句只对「拿它去乘**尺寸参数**」成立，而正解是**乘 `localScale`**。
> 那段里**唯一有价值的两条**（HUD 标签的**尺寸口有 4 处**、`SetScriptHeight` 的 `pxPerUnit` 是个现成的缝）
> 已搬进 **§五·补⑨ · 四**。⇒ 原文已删，⛔ **别按它办事**。

---

## 五·补⑨ 🔴 **面板族的口径 + 「面板内部的字」怎么迁（2026-10-16 · 主对话定）**

> **判据来源**：`普查产出_1016战斗判据/T09b_uGUI拦路判据.md` **§一**（9 个 sorting layer 全表 ·
> 8 个带 `GraphicRaycaster` 的宿主逐条 · 各 `Safe area FrontCanvas`/`LeftArea` 的子件序 ·
> 另 11 个无 GR 的 Canvas）＋ 主对话现读 `MyGame/ProjectSettings/TagManager.asset` 与 `Battle/Label.cs`。
> ⛔ **本节的数值一律去 `T09b` §一 取**（铁律 6：别在这儿抄第二份）。

### 一、🔴 原版的层级模型与我们的落地模型（**这才是「面板族能不能一族一族迁」的判据**）

**原版的事实**（`T09b` §一）：全场景**只有 1 个 `CanvasScaler`**（在**根** `Canvas` 上）·
压制关系靠 **(排序层, `sortingOrder`, 兄弟序)** 三件套 · 9 层从下到上是
`Default < Background < Elements < OverElements < Hand < HandMoving < OverHand < PopUps < Blocking` ·
**面板全在 `PopUps`**（战报 −1 / 设置 0 / 门 0 / 弹窗锚点 1 / 错误条 1）· **tooltip 与 LoadingMenu 在 `Blocking`**。

🔴 **我们现在的现状（2026-10-16 现读）**：`ProjectSettings/TagManager.asset` 里**只有 `Default` 一个排序层**；
全工程**没有任何一个 `.unity` 带 `CanvasScaler`**。⇒ **要照原版分层，得先把那 9 层建出来。**

**定的模型（三条）：**

1. **建 9 个同名排序层进 `ProjectSettings/TagManager.asset`**（逐位对齐；纯新增、零行为改动）。
2. 🔴 **每个迁过去的族 = 【嵌在 HUD 根 canvas 下的一个子 `Canvas`】**（⛔ **不是各自一个根 Canvas**），
   按原版的 `(排序层, order)` 设 + **`overrideSorting = true`**。
   > 🔴 **2026-10-16 当天订正（这一格我先前写错过，留痕）**：原来写的是「建成根对象、`overrideSorting` 无意义」——
   > **两半都错**：① **根 Canvas 没有 `CanvasScaler` 就不跟宽高比缩放**（`scaleFactor` 恒 1）⇒
   > 那样迁过去**根本没解决本件要解决的问题**；而原版**全场景只有一个 `CanvasScaler`**（在根上）
   > ⇒ 我们**不许给每个面板各配一个**。② 正解 = **子 Canvas** —— 它**继承父 canvas 的缩放**、
   > 又靠 `overrideSorting` **脱离父层另立排序组**（`T09b` §A·2 记的原版 4 件就是这么干的）。
   > **错因**：只想着「别挂在 `hudRoot` 下」（那条是防**反向补震**，见 §五·补③ 三·2），
   > 于是把「不挂 hudRoot」推成了「建成根对象」，**漏了 CanvasScaler 与嵌套这两件事**。
   > ✅ **仍然成立的两条**：⛔ 别挂在 `hudRoot` 下（那是反向补震用的）·
   > 嵌套 canvas 的 `renderMode` **不参与**（`T09b` §A·4 末那条）⇒ 只设 `overrideSorting` + 层 + order。
   ⚠️ **子 Canvas 的 `RectTransform` 自己摆位**（嵌套 canvas 不被 Canvas 组件接管）——
   面板要**整块跟着父 canvas 的坐标系走** ⇒ 锚点/尺寸按设计 px 给，与图族同一套（见 §五·补④）。
3. 🔴 **层序与深度【同时】设、且让两者一致**（层越高 ⇒ `planeDistance` 越小 = 离相机越近）。
   **理由（这条是本节的承重结论）**：我们**还留着大量世界空间 quad**（非 canvas 渲染器，
   它们隐式落在 `Default` 层/0 序），而「排序层 vs `renderQueue` 谁先比」这条 **我们没有实机验过**
   （现行口径不许起 player）。**两个机制都指向同一个次序 ⇒ 引擎按哪条判都是同一个结果**，风险归零。
   ⛔ 所以**不许只设层、把 `planeDistance` 随便给**。
   ⚠️ 子 Canvas 靠在**父 canvas 平面的哪个 z** 由它自己的 `RectTransform.localPosition.z` 决定
   （子 canvas 没有 `planeDistance` 这个字段）—— 这一条**主对话还没读原版的对应值**，
   ⛔ **别猜**：动手前先去 `T09b` §A·3 的兄弟序 + 各宿主 RT 现读，把「面板在父 canvas 平面内的 z」定死。

### 二、面板 → 原版宿主 映射表

🔴 **完整 21 行**（我们的每个面板 → 原版 GO / 宿主 / `(rm, overrideSorting, 排序层, order, 兄弟序)` / 逐件出处）
→ **`资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一**。
⛔ **本文件不抄第二份**（铁律 6）—— 这里只留**主对话的三条裁定**（判据全文在那份 §三）：

| 裁定 | 结论 |
|---|---|
| **`WaitBanner` 的宿主** | → **`WaitText`**（`BackCanvas < Safe area BackCanvas`，`Default`/0，兄弟序 **6/7**）。🔴 **不是 `AboveShader`** —— 本文件早先那张表里写的 `WaitBanner → AboveShader` **是错的**（错因：照节点名「AboveShader」望文生义当成「提示条」，**没读那个节点的子件表**）。实读：`AboveShader` 的子件是 **`TipText` + `OvertimeSplashText`**，与 `WaitBanner.cs` 文件头的子件表对不上；而 `WaitText` 的**对得上**。 |
| **`AboveShader` 归谁** | 归我们的 **`TipText` + `OvertimeSplashText`** 那一族（`OverHand`/0，兄弟序 **8/14**）—— 与 `MulliganPanel` 等 `Safe area FrontCanvas` 直子**同一个排序组**，先后由兄弟序定。 |
| **`PopupAnchor`**（原版 `PopUps`/order 1 的**空 Canvas**，唯一组件 `WindowHolder`） | ⏸ **本轮不建**（我们战斗侧零引用；同名机制在 `Shell/WindowHolder.cs`+`WindowsManager.cs` 已有）—— 按本仓「⛔ 别建死件」口径。⚠️ **这不是「不做」**（铁律 11）：等**战斗侧的弹窗宿主**真需要独立锚点时再建。 |

⚠️ 另有一条**要就地订正的过期文件头**（铁律 5）：`Battle/CardChoicePanel.cs:1-16` 写着
「`ChoosePanel` **与** `MulliganPanel` 都从这个类派生」，而实读 `grep -rn ": CardChoicePanel"` 只命中
**`ChoosePanel.cs:48` 一处**（`MulliganPanel.cs:77` 是 `: MonoBehaviour`）⇒ 它现在**只是 `ChoosePanel` 的基类**。

### 三、⛔ **面板族【不能】先于「面板内部的字」单独迁**（本件的硬前提）

面板里**全是字**（`MulliganPanel` 5 个 `Label.Create` · `SettingsPanel` 13 个 · `EndPanel` 2 个 ·
`CardChoicePanel` 3 个 · `CardDisplayWindow` 1 个 · `BattleLogPanel` 1 个 ……）。
**图搬进 canvas、字留在世界空间 = 非 16:9 下两者分家**（=`§五·补⑦` 那条「只换半边比不换更坏」的同一个坑）。
⇒ 🔴 **先给 `Label` 加 uGUI 后端，再迁第一个面板。**

### 四、🔴 「面板内部的字」= **给 `Label` 加 uGUI 后端**（**不做适配层**，理由在下面）

**设计（主对话 2026-10-16 现读 `Battle/Label.cs` + `Core/TmpFont.cs` 之后定的）：**

- **为什么不做 `HudPiece` 那种适配层**：`Label.Create` 全工程 **89 个调用点**（生产侧 40+，
  面板占 30+），而 `Label` 的公开面比 `ImageQuad` 大得多（`SetAutoFitBox` / `SetGlyphHeight` /
  `SetScriptHeight` / `SetAlignLeft` / `SetVAlign` / `SetWrapWidth` / `WorldW/H` / `Contains` /
  `LinkAt` / `RenderQueue` …）⇒ 适配层要把这些**全部镜像一遍**，**两套实现迟早分叉**。
- 🔑 **而 `Label` 已经为「加后端」准备好了**：它本来就是**双后端**（`BuildTmp()` / `BuildDot()`，
  见 `Label.cs:1051` 与 `Label.cs:111` 的派发），`_tmp` 上**用到的 17 个成员全部是 `TMP_Text` 基类上的**
  （`fontSize` / `fontMaterial` / `textWrappingMode` / `textInfo` / `font` / `rectTransform` /
  `ForceMeshUpdate` / `enableAutoSizing` / `fontSizeMin/Max` / `fontSharedMaterial` /
  `characterSpacing` / `textBounds` / `text` / `color` / `alignment` / `isActiveAndEnabled`）
  ⇒ 把字段类型从 `TextMeshPro` 提到 **`TMP_Text`** 即可让 `TextMeshProUGUI` 复用**全部**逻辑。
- 🔑 **单位也是现成的**：`Label` 里**已经有一处** `PxPerWorld`（`Label.cs:1412` =
  `LayoutSpace.DesignPxH / DesignHeight` = **108**）⇒ 世界那一档 **1 单位 = 1 世界单位**；
  uGUI 那一档 **1 单位 = 1 设计 px**（canvas 单位就是设计 px，见 `§五·补③ 一`）⇒
  **`PxPerWorld` 取 1** 即可，**其余算式一个字不用改**。
- **要改的只有三处【带单位的常数】**（⛔ 漏一处就是静默错，且 16:9 下看不出来）：
  ① `PxPerWorld`（`Label.cs:1412`，static → 按模式取 108 / 1）
  ② `CapHeightWorldOfScale`（`Label.cs:207` = `7f/100f` 世界单位/档 → uGUI 档是 **7 px/档**）
  ③ `UsableBounds` 的**量纲上限**（`Label.cs:1089` 的 `w/h <= 100f` = 「100 世界单位」→
     uGUI 档是 **100 × 108 = 10800 px**）—— 🔴 **不换这个，uGUI 下任何正常宽度的标签都会被判成「没量到」
     而停在框心**（`A1751` 那条哨兵守卫会把它吃掉、**不出声**）。
- **调用点的数也要跟着换**：uGUI 档里**一律传设计 px**（去掉 `Px(...)`）。
  🔴 **这一步会静默错**：传了世界单位（= px ÷ 108）⇒ 字小到看不见、**而没有任何报错**。
  ⇒ **每个迁过去的调用点都要在报告/注释里写明「这些数是 px」**，并配一条断言。
- **`SetRenderQueue` 在 uGUI 档里不动**（canvas 由所在 Canvas 的排序/兄弟序决定）—— 照 `HudPiece` 的先例
  **写成有注释的空操作**，并靠 `Label.IsUgui` 让审计查得到（⛔ 不是「悄悄没做」）。
- **点阵兜底（`BuildDot`）在 uGUI 档里不成立**（它是 `MeshRenderer`，挂在 canvas 下不归 canvas 管）
  ⇒ **拿不到 TMP 字体时出声、不静默退化成点阵**（本工程红线：不许静默失败）。
- ⚠️ **`SetScriptHeight(text, px, pxPerUnit)` 那个 `pxPerUnit` 是个现成的缝**（「1 单位 = 多少 px」）——
  它是 HUD 侧 `_overtimeSplashText` 唯一在用的一处，**留着不动**（那件有意不迁，见 `§五·补⑦`）。
- ⚠️ **`HudTextScale`（`§五·补⑦` 那条过渡系数）是【临时物】**：HUD 标签族一旦迁到 uGUI 档，
  **它就该退场**（uGUI 的字由 canvas 统一缩放，不再需要手乘）。⛔ **别让它和 uGUI 档长期并存**。

### 四·补 🔴🔴 **2026-10-16：面板族【不能一扇一扇迁】—— 面板必须【按排序组整批迁】**（主对话现读推出来的）

**起因**：原计划第 3 步是「拿一个最小的面板（`UnitChatPanel`，1 图 2 字、且**根已经是
1920×1080 的设计 px `RectTransform`**）走通全链当样板」。**读完之后判【不能这么做】**，三条：

1. 🔴 **一块 canvas 是【一个整体渲染组】** ⇒ 它的件**不能**与外面的世界空间 quad **逐件**插队
   （`overrideSorting` 的子 canvas 更是一整个排序组）—— 整组在**一个时刻**画完。
   而面板现在**全靠 z + `renderQueue`（3000~4100）**与彼此、与 HUD、与卡**逐件**插队。
2. ⇒ **只迁一扇面板**的结果 = 那一扇要么**盖住所有还没迁的件**（给了 `sortingOrder`），
   要么**被所有 `renderQueue > 3000` 的件盖住**（不给 `sortingOrder`）—— **两种都不是原版的次序**。
   ⚠️ 这**不是**「16:9 下看不出来」那一类：它**任何宽高比下都错**，只是**很难一眼看出**。
3. 🔑 **正确切法 = 按【排序组】整批迁**（一个组迁完，组内先后由 `(层, order, 兄弟序)` 定）：
   - **`PopUps` 组**（设置 / 战报 / 门 / 结算 / 错误条）：它们**本来就该盖在所有 2D 之上**
     ⇒ **整批迁是安全的**（迁完不会盖错谁）。⚠️ 组内 order 有**三个 0 并列**，原版靠**兄弟序**分先后
     ⇒ 我们也要把这三块子 canvas 的**兄弟序**排对（`T09c` §一 逐行给了原版的兄弟序）。
   - 🔴 **2026-10-16 收窄（原先这条说得太绝对，铁律 5）**：**不是「必须一次整组迁完」** ——
     只要按 **【组内 order 从高到低】** 迁，**每一步都是安全的**：
     已迁的那几块在 `PopUps` 层 ⇒ **一定压在还没迁的 `Default` 层 quad 之上**，
     而它们本来就该在那些件之上（组内更高的 order）✓。
     ⇒ **安全顺序 = 错误条(1) → 设置/门/结算(0) → 战报(−1)**；⛔ **反着来就不安全**
     （先把 `−1` 的战报迁上去 ⇒ 它会盖住本该压在它上面的设置窗）。
   - **`OverHand` 组**（HUD 主层：`Mulligan` / `Card Display Window` / `Multi Card Display` /
     `Unit Chat` / `ChooseCardMenu` / `ActiveSkillDesc` / `Alliance Panel` / `AboveShader` /
     `AttackSelector`(这个在 `Default`)）—— 这一组**与 HUD 自己同层** ⇒ 要和
     **§五·补⑦⑧ 那两批（HUD 图 / HUD 字）一起收口**，⛔ 不能单独动。
   - **`Blocking`**（tooltip + 加载黑场）：同理，独立、且在最顶。
   ⇒ **第 3/4 步合并成「按组收口」**，⛔ **别再按「哪一扇最小」挑**。

⚠️ **本条的代价（如实记）**：原本想先用一扇小面板**试出坐标系换算的坑**，现在试不了了 ——
⇒ **那一族的第一批必须【先挑一个组里最简单的一整组】做**（`PopUps` 组里
`EndPanel`（3 图 3 字、无命中）或 `ErrorMessageBanner` 最像），**做完立刻在组内比次序**。

### 五、本批执行顺序（**一件件来，⛔ 不并行改同一族**）

1. ✅ **`ProjectSettings/TagManager.asset` 补 9 个排序层**（纯新增；2026-10-16 已落地，+24 行）
2. ✅ **`Label` 加 uGUI 后端**（**新能力、零行为改动**：现有 89 个调用点一个都不动，默认仍是世界档）
   ＋ `TmpFont.NewTextUgui` ＋ `HudCanvas` 的 `MakeText`/`MakeImagePiece`/`MakeSlicedPiece`/`EnsureSubCanvas`/
   `SubCanvas`/`SortingLayer`/多容器 z 账（2026-10-16 已落地，typecheck 0 错）
3. ✅ **`Blocking` 组**（2026-10-16 已落地，typecheck 0 错）—— **加载黑场**（`Blocking`/1000，从「世界空间
   全屏 quad + 队列 4100」翻成**子画布上一张全屏 `Image`**）· **`Tooltip`**（改**双模式**：宿主由**建 UI 的那一方**
   登记，`UseHost(host)`；战斗侧登记 `Blocking`/0 的子画布，**卡组编辑那边不登记 ⇒ 自动留在世界档**）。
   ⚠️ **两处顺带**：① `Tooltip.PanelCenter` 的语义**按档变**（世界档 = 世界单位 · uGUI 档 = 设计 px）
   ⇒ 新增 `PanelCenterWorld` 给跨档比较用，`BattleScene` 那条位置断言已改用它 ② 新增 4 条断言
   （两块子画布的 `overrideSorting`/层/order · 子画布**不带 `CanvasScaler`** · 排序层 ID 次序的**灭自证**）
   ⚠️ **一处如实标的差异**：uGUI 档的九宫格底图是**一张 `Image (Sliced)`**（原版就是这样），
   但**原版那颗 `Image` 的 `m_PixelsPerUnitMultiplier` 主对话没读到** ⇒ 暂用默认 1，记账待查。
4. ✅ **`PopUps` 组**（设置 / 战报 / 门 / 结算 / 错误条）—— **2026-10-16 五扇的代码都落地了**（`typecheck` 0 错）。
   🔴 **2026-10-16：`ErrorMessageBanner` 已翻完**
   （order **1**，组内最高 ⇒ **安全顺序的第一件**；它是**跨场景共用件** ⇒ 走**双模式**：`Create(..., uguiHost)`，
   战斗侧传 `PopUps`/1 子画布、**外壳侧（`Shell/MessageToast`）不传 ⇒ 自动留世界档**，与改之前逐位相同。
   九宫格底条在 uGUI 档 = **一张 `Image (Sliced)`**。配 3 条新断言：子画布的 `overrideSorting`/层/order ·
   不带 `CanvasScaler` · **`IsUgui` 灭自证**）
   ✅ **其余 4 扇同日也翻完**（三个写手代理按**文件所有权**切块并行，`EndPanel`+`BattleDoors` 因互相嵌套算**一个单位**）：
   `SettingsPanel`（`PopUps`/0）· `BattleLogPanel`（`PopUps`/**−1**）· `EndPanel`+`BattleDoors`（`PopUps`/0）。
   🔴 **主对话在同步点补的两格**：① 同层同 order 的三块子画布**显式定兄弟序**
   （`HudCanvas.SubCanvasRank` = 设置窗在**前**、门/结算在**后** ⇒ **门/结算压在设置窗之上**，判据 = 原版根 canvas
   子件序里 `FrontCanvas`(3) 在 `BattleDoors`(4) **之前**）② `BattleScene.cs` 那批断言按三份清单统一改完
   （`* 108f` 那一族去掉 · `box()` 助手加了**画布分支** · 结算层序判据从「世界 z」换成「排序组」·
   战报四条边框改读 `FrameSizePx` · A276 那条**如实标成恒真、记账**）。
   ⚠️ **仍未做（都记账，见 §七）**：`SettingsPanel` 那 **3 根 `WfSlider` 还在世界空间**（面板分成「世界壳 +
   画布本体」两层，本轮真机看那 3 根**一定不对**）· `EndPanel`/`BattleDoors` 父子颠倒的下游（小 6.65% 只修一半）·
   语言下拉列表逐位要**它自己的子画布**（本轮先用兄弟序顶）· 原版 `CemeteryLogPanel` 自己那级 794.07×653.80 **无节点承载**。
5. ⏭ **`OverHand` 组收口** —— 与 **HUD 图族 + HUD 字族**（`§五·补⑦⑧`）**一起做**
   （`OverHand` 就是原版的 HUD 主层；⛔ 单独动面板会与 HUD 抢次序）· 同批**退掉 `HudTextScale`**
6. ⏭ **之后**：手牌/场上卡（走 `Physics2D` 那条）· 拆 `ReanchorHud` + 4 注册表 ·
   `LayoutSpace` 读回口**成对归零**
7. 🔴 **8 步走完之后那次统一验收**（三档宽高比截图并排 + `wf_click.py` 真点）**照 `§五` 那两条来**，
   ⛔ **不能只跑断言就收口**。

---

## 五·补⑩ 🔴 **迁一扇面板的【配方】**（2026-10-16 主对话摸出来的 —— 批 2 照它做，⛔ 别各写各的）

> **为什么要有这一节**：读第一扇（`EndPanel`）时撞到**两处跨全批的硬关卡**，不先解决就会**每扇都踩一遍**。

### 一、🔴 关卡 1：`MenuDraw.SetPxSize` 写的是**世界单位**，uGUI 档不能用

`MenuDraw.SetPxSize(t, wPx, hPx)` 体内是 **`rt.sizeDelta = new Vector2(LayoutSpace.Px(wPx), LayoutSpace.Px(hPx))`**
—— 而 `LayoutSpace.Px` **就是 ÷108**（`Shell/MenuDraw.cs` 现读）。
⇒ 今天面板族那些 `SetPxSize(root, 1920, 1080)` 实际写进去的是 **20×10 世界单位**（只在 16:9 下「碰巧」等于整屏）。
🔴 **uGUI 档的 `sizeDelta` 要的是【真 px】**（canvas 单位就是设计 px）⇒ 沿用 `SetPxSize` 会把整扇面板缩成 **1/108**，
**而画面只是「小得看不见」、不报错**。
✅ **处置**：`HudCanvas` 已加 **4 个换算助手**（2026-10-16，typecheck 0 错），批 2 一律改走它们：

| 旧口（世界单位） | 新的 uGUI 口 | 备注 |
|---|---|---|
| `MenuDraw.SetPxSize(rt, w, h)` | **`HudCanvas.SetPxSize(rt, w, h)`** | 中心锚 + `sizeDelta` 直接吃 px |
| 整屏覆盖层写死 `1920×1080` | **`HudCanvas.FillParent(rt)`** | ⛔ 别写死 —— `Expand` 档下**屏比画布宽**时（21:9）会盖不满两边 |
| `Pos(px, py, z)` / `Content(px, py)` | **`HudCanvas.PosPx(px, py)`** | 「左上原点 y 向下」→ canvas 局部（中心原点、**y 向上**）；式子 = 旧那个 ×108 |
| `ImageQuad.Create(p, tex, pos, worldH, anchor, name)` | **`HudCanvas.QuadFromTopPx(p, tex, name, xPx, yPxTop, hPx, z, color)`** | **按【高】定、宽由贴图比例来**；要盖比例的自己调 `SetAspect` |

⚠️ **`U(px) = px/108f` 那种局部助手**：uGUI 档直接**用 px**（`hPx`），⛔ 别再除 108。
⚠️ **z 那个形参**：旧那套是世界 z（**大的在后**），`HudCanvas.Add` 的 z 是**兄弟序**（**大的也在后**）⇒ **语义同向，值照搬**。

### 二、🔴 关卡 2：我们这边的**嵌套结构与原版不同**

**实例（读 `EndPanel` 时发现）**：`EndPanel.Build()` 最后一句是 **`_doors = BattleDoors.Create(_root);`**
—— 也就是**我们这边 `BattleDoors` 挂在 `EndPanel` 里面**；而原版是 **`EndBattlePanel` 挂在 `BattleDoors/Canvas` 下**
（父链 `BattleHud/Canvas/BattleDoors/Canvas/EndBattlePanel`，判据 → `T09c` §一）。
⇒ **迁之前先对一次父链**：我们这边谁挂在谁下面，**未必等于原版**。
⛔ **别顺手把嵌套倒过来**（那是另一笔账）—— **本次只按「谁的 canvas 装谁」迁，并把差异如实记着**。

### 五、批 2（`PopUps` 组）的**逐件分类**（2026-10-16 主对话现核，⛔ 别按印象办事）

| 面板 | 原版宿主（层/order） | **跨场景吗** | 备注 |
|---|---|---|---|
| **`ErrorMessageBanner`** | `PopUps` / **1** | 🔴 **是** —— `Shell/MessageToast.cs` 握着它（`_banner.Preset == MenuPreset`）· `DeckInfoPopup` 也提它 | ⇒ **要双模式**（同 `Tooltip` 那套：**宿主由各场景自己给**，外壳侧不给 ⇒ 留世界档）。⚠️ 只有它是 `PopUps`/**1**（组内最高）⇒ **它是安全顺序的第一件** |
| `SettingsPanel` | `PopUps` / 0 | ❌ 否（`Shell/SettingsWindow.cs` 只是**注释**提到） | |
| `BattleDoors` | `PopUps` / 0 | ❌ 否（`Shell/ShellRuntime.cs` 只是注释） | ⚠️ 它在我们这边**挂在 `EndPanel` 里面**（见 §五·补⑩ 二） |
| `EndPanel` | `PopUps` / 0（原版挂在 `BattleDoors/Canvas` 下） | ❌ 否 | ⚠️ 同上 |
| `BattleLogPanel` | `PopUps` / **−1** | ❌ 否（`Core/Tooltip.cs` 只是注释里引它的 `OverlayQ` 常量） | ⇒ **安全顺序的最后一件** |

🔑 **判据来源**：`grep -rn "<类>.Create\|<类>.Show\|new <类>\|: <类>" --include=*.cs Shell/ Deck/ Core/`
—— **只有 `ErrorMessageBanner` 有真调用点**，其余四扇全是**注释/常量引用**。

### 六、配方（每扇面板照这 8 步）

1. **建/复用它的子画布**：`HudCanvas.EnsureSubCanvas(null, "<原版宿主名> Canvas", 层, order)`
   —— 层/order **去 `T09c` §一 那一行取**（⛔ 别自己编）。
2. **根节点**挂进子画布：`FillParent`（⛔ 不是 `MenuDraw.SetPxSize(root, 1920, 1080)`）。
3. **图**：`ImageQuad.Create(...)` → `HudCanvas.QuadFromTopPx(...)`（返回 `HudPiece`，字段类型跟着改）。
4. **字**：`Label.Create(...)` → **`HudCanvas.MakeText(...)`**（**设计 px**；⛔ 世界单位喂进去**小到看不见**）。
5. **位置**：`Pos/Content(x,y,z)` → **`HudCanvas.PosPx(x,y)`**（z 另传给建件口）。
6. **同批**：把迁走的件从 `ReanchorHud` 与那 4 个注册表里**摘掉**（⛔ 否则「一半跟画布走、一半跟世界走」）。
7. **同批改断言**：`Editor/BattleScene.cs` 里碰这扇面板的那几条 —— 读过 `sizeDelta`（世界单位）的期望值要换成 **px**。
8. **跑 `bash 工具/typecheck.sh`**（秒级）—— ⛔ 不跑 `*.Run`、不起 player（现行口径）。

### 四、⛔ 一批做完之前别插队

`PopUps` 这 5 件（设置 / 战报 / 门 / 结算 / 错误条）**必须同批过完**（见 §四·补 第 1 条）——
组内 order 有**三个 0 并列**，靠**兄弟序**分先后；只迁一半 ⇒ 剩下的世界空间 quad 与新画布**次序无定义**。

---

### 七、🆕 **本批挖出的新账**（2026-10-16 · 写手 C 做 `EndPanel`+`BattleDoors` 时查出）

1. 🔴 **`EndPanel`/`BattleDoors` 的父子【与原版是反的】，而且**它有一处下游未修** ——
   原版：`BattleHud/Canvas → BattleDoors → Canvas(PopUps/0) → EndBattlePanel`；
   我们（迁完后）：`BattleHud Canvas → BattleDoors Canvas → EndPanel → BattleDoors`（**后两跳反了**）。
   ⚠️ **下游（如实记）**：原版 `EndBattlePanel` 的 `lossyScale = 1.06647` 是**因为它在带缩放的 `BattleDoors` 下面**；
   我们在缩放**上面** ⇒ 「整块结算界面小 6.65%」**只修了一半**（视频那一支带 1.06647 ✓、骷髅/标题那一支没有）。
   ⇒ **要修得先翻嵌套、或逐件补缩放** —— **新账，本批白名单外**。
2. 🔴 **同层同 order 的 `PopUps/0` 子画布，先后由【兄弟序】定；我们这块很可能与原版相反** ——
   原版根 canvas 的子件序是 `[3] FrontCanvas` 在 `[4] BattleDoors` **之前**（= 设置窗在门之前）；
   而我们那块画布**建得很早** ⇒ 很可能排在前面 ⇒ **`PopUps` 组收口时必须显式定序**（⛔ 不是本批能定的）。
3. 🔴 **`HudCanvas` 缺「相对父中心」的建图口** —— `MakeImage`/`QuadFromTopPx` 把 `x/y` 当**屏幕绝对 px**
   （锚点 = `x/1920`、`1−y/1080`）；**只有 `parent` 本身就是整屏矩形时**它才等于「相对父中心」。
   面板里的**子矩形容器**（`SkullsHolder` 之类）**不是**整屏 ⇒ 直接套会把件摆到别处、**而且静默**。
   写手 C 在 `EndPanel` 里自造了一个 `ImageInParent`（**临时**）⇒ 🔴 **主对话要在同步点收编进 `HudCanvas`**，
   ⛔ **别让它长出第二份**（铁律：两处写同一条规则 = 迟早不一致）。
4. ⚠️ **`HudCanvas.Group` 不回收已销毁的子件** —— `BattleDoors.Stop()` 每局 `DestroyImmediate(_screen)`，
   那个容器的 Group 里会留**僵尸条目**（今天无害，多局/换分辨率会积累）⇒ 缺一个 `PruneDestroyed`。
5. ⚠️ **`HudPiece` 包不住 `RawImage`** —— 任何 **RenderTexture 驱动**的件（开门视频那层）只能走 `RawImage`
   （`Image` 要 `Sprite`，而 `Sprite.Create` 只吃 `Texture2D` ⇒ **API 上不成立**）。
   ✅ 顺带一条**更忠原版**的收获：原版那一件本来就是 **`RawImage`**。
6. ⚠️ `EndPanel.Create` 的 `parent` 形参**成了死参**（先例 = `BuildLoadingCover(root)`）⇒ **改注释即可，⛔ 别改签名**。
7. ⚠️ `Battle/SettingsPanel.cs:547` 的注释还引 `EndPanel.SolidTex`（本批已删）⇒ 顺手订正（**别人的文件**，等同步点）。
8. 🔴🔴 **【系统性口径】`Canvas` 的「单位 ↔ 世界单位」两轴系数【不同】，而且随屏变** —— 写手 B 在 `BattleLogScene`
   上实测出来的：**批处理下 `Screen = 640×480`（4:3）而 `cam.aspect` 被钉死 16:9**
   ⇒ `CanvasScaler(Expand)` 的 `scaleFactor = min(640/1920, 480/1080) = 0.3333`
   ⇒ **画布 rect = 1920×1440 画布单位**，铺在 17.78×10 世界上
   ⇒ **x 轴 1 单位 = 1/108 世界单位 · y 轴 1 单位 = 1/144** ⇒ **两轴不同**！
   ⇒ 🔴 **一切「拿画布件的世界尺寸 ÷ 1/108 折回 px」的读回都会偏 0.75 倍**（实测：`FrameWorldH*108`
   读出 48.75 / 35.10 / 428.43 / 434.0，而真值 64.995 / 46.80 / 571.24 / 578.67 — **画面其实是对的**）。
   ✅ **正解 = 读回一律读 `sizeDelta`**（`HudCanvas.SizePx` / 写手 B 新加的 `log.FrameSizePx(art)`），
   ⛔ **别走「世界尺寸 ×108」那条路**。
   ⚠️ **本条不止影响面板族** —— **批 1 的 HUD 图族、以及任何「拿 `HudPiece.WorldH` 去比字面 px」的断言同病**
   （`HudPiece.WorldW/H` 走 `GetWorldCorners` ⇒ **返回世界单位**，两轴系数还不一样）。
   ⚠️ 注意区分：**`Label.WorldW/H` 是另一回事** —— 它读 `TMP.textBounds`，在 uGUI 档里**就是画布单位（px）** ✓
   （所以 `ErrorMessageBanner` 里 `WorldW * 1f` 那条是**对的**）。
9. 🔴 **`SettingsPanel` 里 3 根 `WfSlider` 仍在世界空间**（写手 A 报，`WfSlider.cs` 不在它的白名单）——
   面板根因此**分成两层**：「世界壳（装滑块）+ 画布面板帧（装其余）」。**两条代价**（随 `WfSlider` 迁完消失）：
   ① 子画布在 `PopUps` ⇒ **面板底会盖住那 3 根滑块** ② 它们仍跟 `HudRoot` 震屏走、面板其余件不跟。
   ⇒ **新账**：`WfSlider` 要加一口「按模式取单位」（`Create` 的 5 处 ÷108 + `ppu` + 两层底图 `MenuDraw.Nine` +
   手柄 `ImageQuad.Create` + `LayoutFill/FillSpanW` 那族 + `TrackWorldH` 等读口）。
   ⚠️ **它还被 `Shell/SettingsWindow` 三根 + FPS 那根共用** ⇒ ⛔ **不能全局开关**，要**模式形参**。
   ⚠️ **本轮真机看那 3 根一定不对 —— 别当新缺陷**。
10. 🔴 **`HudCanvas` 缺「普通图片 + 中心锚」的口**（写手 A + C 各自撞了一次）——
   `MakeImage`/`QuadFromTopPx` 是**归一化锚**（只在 16:9 下等价于「相对父中心」），`MakeSlicedPiece` 才是中心锚。
   两人各用了一个权宜（A 用 `MakeSlicedPiece + border=0`、C 自造 `ImageInParent`）⇒
   **主对话要在同步点补这一个口并收回两处权宜**（⛔ 别长出第三份）。
11. ⚠️ **`HudCanvas.Group.Add` 不去重** ⇒ 同一个件 `Add` 两次会留两条记录、索引此后算偏。
   `MakeImage*`/`MakeAnchored`/`MakeText` **自己就登记一次** ⇒ 调用点**想挪档必须走 `Reorder`**（A 的 `_shade` 就是这么写的）。
12. ⚠️ **画布上的节点【不随 `parent` 销毁**（与 `BattleHud Canvas` 同一条既有口径）⇒
   A218/A276 两个探针**每跑一次泄漏 2 个隐藏面板节点**（生产侧也会留节点）⇒ 探针要补 `DestroyImmediate(p.gameObject)`。
13. ⚠️ **`PopUps`/order 0 的【三块画布】谁在先，只由兄弟序定**（设置 / 门 / 结算）——
   `EnsureSubCanvas` **不收兄弟序参数** ⇒ 这一格归主对话在同步点定（见 §七·2 那条，原版是设置窗在门**之前**）。
14. ⚠️ `MakeSlicedPiece` 的 `ppuMul` 恒 1 ⇒ 要复刻原版那个 1.09 只能把 border 预 ÷1.09（画出来对、切 uv 差 ≤2 贴图 px）；
   要逐位得让它收一个 `ppuMul`。
15. ⚠️ **原版 `BattleSettingsPanel` 的 `ap = (0, −29.317)`**（面板中心比屏幕中心低 29.3 px）——
   **改前改后都没做**（旧账）⇒ 记着，⛔ 别当已复刻。
16. ⚠️ **原版语言下拉列表是它自己的 canvas**（`overrideSorting` + `PopUps`）⇒ 逐位做法是给它**建一层子画布**
   （`EnsureSubCanvas` 支持传父件）；A 本件先用**兄弟序**顶（`Reorder`），已注释 + 已报告。

---

### 八、批 3（`OverHand` 组）的账（2026-10-16）

> **原版这一组的子件序（`Safe area FrontCanvas` 的 `m_Children`，`T09b` §A·3）**：
> `[0] Drag Attack Selector · [1] ActiveSkillDesc · [2] Unit Chat · [3] Mulligan · [4] Generic Multi Card Display Combat ·
> [5] ChooseCardMenu · [6] Card Display Window · [7] DebugButtons · [8] AboveShader · [9] PopupAnchor ·
> [10] BattleSettingsPanel · [11] Tutorial · [12] LoadingMenu · [13] Alliance Panel`
> 🔑 **同组同 order ⇒ 先后【只由兄弟序定】** ⇒ 迁移要按**从高到低**（13 → 1），⛔ 反着来时
> 先迁的会**压住本该在它上面的**。

1. ✅ **已落地**：`AboveShader`（**8**，提示条 + 加时 splash）· `Mulligan`（**3**）· 另有三扇在飞（6 / 5 / 4）。
2. 🔴 **`Alliance Panel`（13）【不是顺手一格】—— 它是壳侧的窗**：`Shell/AlliancePanelWindow.cs`
   整套走**壳的窗口系统**（`MenuDraw` 系列助手 + 显式渲染队列阶梯 `QShade…QText`），
   ⛔ **不能**把它挂到战斗子画布下（世界空间 quad 挂画布下 = 本仓明令禁止的半迁移态）。
   ⇒ **要么让壳侧窗口系统也支持 uGUI、要么给战斗侧单独重建一份** —— **属跨线（界面线）的大活**，
   **新账**。⚠️ **中间态影响很窄且已知**：它会短暂地被已经迁到 `OverHand` 画布的那几扇压住。
3. ⚠️ **`AttackSelector`（原版子件序 0、且在 `Default` 层、`overrideSorting = false`）** ——
   它不是「自己的画布」，而是**随父画布按兄弟序渲染**（`T09b` §A·2#5）⇒ 迁它时应当把它放进
   **该组的那块画布**里、并给它**最低的兄弟序**，⛔ 不是另起一块画布。
4. ⚠️ **`Drag Attack Selector` 有命中**（拖拽起手）⇒ 按 §五·补④，带命中的件要么走 `HudPiece.Contains`
   （`GetWorldCorners`，两档都对），要么等输入那一步。
5. 🔴🔴 **裁定（2026-10-16 主对话）：整屏压暗层【留世界】还是【上画布】—— 看它必须画在谁的哪一侧。**
   批 3 里同一个选择题**两个写手给出了相反的答案**，我核完判 **两边都对**，规则是：
   > **「必须画在某个【世界空间件】下面的整屏件 ⇒ 留世界；必须画在它【上面】的 ⇒ 上画布」**
   - `CardDisplayWindow.Menu Dark Background`：必须画在**放大的那张卡【下面】**，而那张卡**还是世界件**
     （`CardView` 属第 5 步、本批没迁）⇒ **留世界** ✓（上去反而会**盖住卡**，本窗打开即废）。
     ✅ 同批先例 = `MultiCardDisplay.mcd_mask`（同一原版组件、同一 alpha，文件头早写着这条）。
   - `MulliganPanel` 的压暗：必须**盖住手牌**（世界件、`Default` 层）⇒ **上画布** ✓
     （`OverHand` 画布压在 `Default` 之上）—— ⚠️ **这一条依赖「排序层优先于 depth」那条承重假设**（§五·补⑨ 一·3）。
   ⚠️ **两条都成立的前提是同一个**：卡体还在 `Default` 层。⇒ **卡体那一批（第 6 步）做完之后，这两件要回头复核**
   （那时「必须画在卡下面」的件可以整体上画布、用兄弟序表达）。
6. 🔴 **`HudCanvas.Group` 不回收僵尸条目的代价在 `CardDisplayWindow` 最重**（写手 A 逐值算过）：
   `RefreshLore` **每次开窗** `ClearChildren(_bg)` + 重建 2 件 ⇒ **每次 +2 条僵尸账**
   （今天无害：`SetSiblingIndex` 夹取后仍是创建顺序）。⇒ `PruneDestroyed` 这条（§七·4）**优先级上升**。
7. ✅ **好消息（写手 A 逐值算过）**：卡体那一叠的外接盒（含转角）x ∈ [698.4, 1221.6] · y ∈ [63.6, 896.4]，
   而画布上的件离它最近的也有 **4.4 px**（风味条）/ **136.5 px**（效果清单）⇒ **零重叠**
   ⇒ 「画布压在卡体之上」这条差异**在本窗一个像素都看不见**。
8. ⚠️ 另：`EffectList` 我们挂**窗根**、原版挂 `Card Display` 下（**迁移前就如此**，本批只换单位没动层级）
   ⇒ 收口时若改层级要连带重算 `FxC`。**新账**。
9. ✅🔴 **写手 C 的第二扇（`MultiCardDisplay`）与 A【独立得出同一条规则】**：它同样把那层整屏压暗
   （`mcd_mask` / `ChoiceShade`）**留在世界空间**，理由与 A 一字不差（卡体是 `Default` 层的 `MeshRenderer`、
   **不归画布管** ⇒ 上画布会盖住卡）。**两条独立结论互相印证了 §八·5 那条裁定** ✓。
10. 🔴🔴 **【已修】`MultiCardDisplay` 的 `availPx` 量纲错**（写手 C 顺手查出、主对话当场修 + 换断言）：
   原式 `LayoutSpace.VisibleWidth − SideMarginPx * 2f` = **世界单位 17.78 − px 120 = −102.2**
   ⇒ **恒判「装不下」、永远缩到 `MinShrink` 45%**（卡高 520.3 → 234 px），**与张数无关**。
   ✅ 改成 `LayoutSpace.DesignPxW − SideMarginPx * 2f`（1920 px，同量纲）；
   ✅ **同时把盖住它的那条断言换掉** —— `BattleScene.cs` 原来那条第三支 `mcd.UsedScale > 0f` **恒真**
   （scale 永远 > 0）⇒ **整条没有判别力**，正是它让这个缺陷活了很久。现在换成**真不变量**
   「一排宽 ≤ 可用宽」，并写明改坏法。
   ⚠️ **这条与迁 uGUI 无关**（老代码就有）—— 按**铁律 11**（与原版不符的全部要做）修掉。
11. ⚠️ **两扇那几颗钮现在是 `raycastTarget = false`，原版是 1** ⇒ **第 4 步（输入）要一起翻**。**新账**。
12. ⚠️ **`40k_UI_bt_play` 原版 rect 80.47×79.64，而 `CardChoicePanel` 仍画 79.64 正方**（窄 0.83 px，既有偏离；
    `MultiCardDisplay` 本批已逐位给 80.47）⇒ **要做**（铁律 11）。**新账**。
13. ⚠️ `CardChoicePanel` 的 `_cardBtns` 与 `_cards` 在某张贴图缺失时会**静默错位**（潜在，今天不触发）⇒ **新账**。
14. ⚠️ A218 探针每次多泄漏 **4 个**画布节点（§七·12 老账的延伸）。

### 六、还差什么 / 判不准的（⛔ 不猜；**完整 7 条 → 普查 §五**）

1. ~~🔴 **「谁压谁」判不了**：原版 8 个 `GraphicRaycaster` 的 `sortingOrder` 没读到~~
   ✅ **2026-10-16 已查全**（`T09b_…md` §一）：8 个组件的字段**完全一致**，谁压谁由所在 `Canvas` 定；
   **9 个 sorting layer 全表** + 8 个宿主的 `(renderMode, overrideSorting, 层, order, 兄弟序)` 都解出来了。
   ✅ **并且本文件 §五·补③ 已经据此定死了我们这边的模型**（⚠️ 我们**不用** `sortingOrder` 插队 ——
   用 `planeDistance`，见那一节）。
2. ~~**原版 `UIstate` 15 档 → 哪个 handler**~~ ✅ **2026-10-16 已查全**（`T09b_…md` §二）：
   枚举真名 **`UIstateOptions`**（`dump.cs:46314-46332`）· 15 档全名 + 逐档「谁处理 / 怎么迁移」。
   🔴 **带来的结论要记住**：我们那 15 处 `if…return` **混了两种东西**（面板优先级 / 战斗阶段）
   ⇒ **别 1:1 搬成 `switch`，要拆成两条通道**。
3. **`BattleScene.cs` 那批 `Check(` 里哪些会因为换层而红，没逐条判**（**2620 条** `Check`；
   普查点出其中 **458 行**含 `Hit`/`Contains`/`FromPixel`/`ToPixel`/`ToDesignPixel`）。
   🔴 **这条与 §五·补③「五、挂起项」是同一件事** —— 要不要在换层期间跑一次，等你拍板。
4. **`AboveShader/TipText` 等未建件与本次迁移的先后** —— 没查。
5. `Board/` 那一侧（`BoardLayout`/`ArenaSlots`）的命中口 —— 读的时刻那个文件**正被别的写手改**，没下结论。

---

> **本文件由主对话维护**（铁律 6：本线待办只在 `任务总表.md` 与这里，⛔ 别在别处再开一份）。
> 开工前先读 [`战斗线/准则与做法.md`](../准则与做法.md)（六步流程 + 判据入口 + 已知的坑）。
