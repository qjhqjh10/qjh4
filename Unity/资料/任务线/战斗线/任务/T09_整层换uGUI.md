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


世界空间那套 `Label` 的尺寸**是按【整数档】量化的**（`Battle/Label.cs:998`
`scale = Mathf.Max(1, Mathf.RoundToInt(approx / CapHeightWorldOfScale))`）⇒
**它表示不出 0.75 这种分数缩放**（乘上去会被 `RoundToInt` 抹掉，只剩 ±33% 的档）。
⇒ **非 16:9 只能靠【把标签族也迁到 uGUI】才能修**（uGUI TMP 的 `fontSize` 是浮点）。

**下一批（标签族）要动的量**：`Hud()` 是**唯一**的 HUD 建字口（`Label.Create` 在 `BattleDriver` 里只有两处，
另一处是加时横幅那件）· 但 HUD 标签的**尺寸口有 4 处**：
`Hud()` 里的 `SetAutoFitBox(Px(boxWpx), …)` · `_handLabel.SetAutoFitBox` · `_titleMe/_titleFoe.SetGlyphHeight` ·
`_overtimeSplashText.SetScriptHeight(…, 108f)`。
🔑 **`SetScriptHeight(text, px, pxPerUnit)` 那个 `pxPerUnit` 是个现成的缝**（「1 世界单位 = 多少 px」）。
⚠️ **别用 `LayoutSpace` 的全局映射去修**（那是 2026-10-15 试过并**当天回滚**的路，
且现在图已不在那条映射上 —— 改了只会更乱）。

---

## 六、还差什么 / 判不准的（⛔ 不猜；**完整 7 条 → 普查 §五**）

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
