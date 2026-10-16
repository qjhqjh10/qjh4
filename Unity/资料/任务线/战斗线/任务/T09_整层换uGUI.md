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

**⛔ 不换的**（判据 → 普查 §三）：竞技场 3D 层 · 原版挡板（那是 `BoxCollider` 物理世界）· **手牌/场上卡/卡面**（原版走 `Physics2D`，不是 `GraphicRaycaster`）· 粒子/特效 · 准星（名字自带 `NoCanvas2D`）。
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

## 六、还差什么 / 判不准的（⛔ 不猜；**完整 7 条 → 普查 §五**）

1. 🔴 **「谁压谁」判不了**：原版 8 个 `GraphicRaycaster` 的 **`sortingOrder` 没读到**；我们这边也没定（`BattleDriver.cs:3942` 明写「保持现有算法」）⇒ 换 uGUI 时这一条**必须先补判据**，否则面板会被底板盖住（本工程 2026-09-20 踩过同型）。
2. **原版 `UIstate` 15 档 → 哪个 handler 的逐档映射没展开** ⇒ 我们那 15 处 `if (Handle…)` 换成什么顺序，**还没定**。
3. **`BattleScene.cs` 那批 `Check(` 里哪些会因为换层而红，没逐条判**。
4. **`AboveShader/TipText` 等未建件与本次迁移的先后** —— 没查。
5. `Board/` 那一侧（`BoardLayout`/`ArenaSlots`）的命中口 —— 读的时刻那个文件**正被别的写手改**，没下结论。

---

> **本文件由主对话维护**（铁律 6：本线待办只在 `任务总表.md` 与这里，⛔ 别在别处再开一份）。
> 开工前先读 [`战斗线/准则与做法.md`](../准则与做法.md)（六步流程 + 判据入口 + 已知的坑）。
