# EW7 · `WaitBanner`（原版 `WaitText`）迁 uGUI —— `T09` 块 6

> **执行者**：子代理 `EW7`（T09 完成波 · 块 6）· **日期** 2026-10-16 · **本波唯一有画面可验的一件**。
> **白名单**（只改了这三个）：`Battle/WaitBanner.cs` · `Battle/BattleDriver.cs` · 本报告。
> ✅ **`TMPDIR=/tmp/wf_ew7 bash d:/4/Unity/工具/typecheck.sh` ⇒ 两行都是 `0`**（末次跑在最后一笔编辑之后）。
> ⛔ 没跑 Unity、没动 git、没碰白名单外的文件、没改任何正本/`任务总表.md`。

---

## ① 结论

### 1·1 建件口逐条（四个口 + 位置）

| 建件口（改前 · 世界档） | 画布档落到哪 | 结果 |
|---|---|---|
| `ImageQuad.Create`（压暗层，`WaitBanner.cs` 旧 `:184`） | **`HudCanvas.MakeImagePiece(host, null, …)`** | ✅ **迁了**。`tex` 传 **`null`** —— 判据 = 原版那颗 `Dark Shade` 本身 `m_Sprite = null`（**无图、纯色**，色 α 0.6118）⇒ 画布档这一件**比世界档那个 `SolidTex` 虚底更贴原版** |
| `MenuDraw.Nine`（框，旧 `:214`） | **`HudCanvas.MakeSlicedPiece`** | ✅ **迁了**。尺寸 `1323×90`、`m_Border (169,160,169,160)` **逐值照搬**；uGUI 侧用的就是 Unity 自己的 `Image (Sliced)`（= 原版那一件本来就是这个）⇒ 角块退化行为与原版同源 |
| `MenuDraw.Tiled`（填充，旧 `:274`） | 🔴 **`HudCanvas` 里【没有】平铺口** | ⚠️ **迁了，但走的是临时权宜**：`MakeSlicedPiece(…, border: 0)` 建一件 → 取回 `Image` → 写 `type = Tiled` + `pixelsPerUnitMultiplier = 2`（原版 `Background fill` 的 `m_Type = 2` / `m_PixelsPerUnitMultiplier = 2.0`）⇒ **格宽 64 画布 px，与世界档那 42 块同解**。⛔ 没有自造「第二份平铺建件法」，只在本件调用点改了这颗 `Image` 的两个字段，**并在报告 ③ 挂账请主对话收编** |
| `Label.Create`（字，旧 `:280`） | **`HudCanvas.MakeText`** | ✅ **迁了**。框/字号收的是 **设计 px**（世界档那一支才是 `LayoutSpace.Px(...)`）—— 即 `SetAutoFitBox(1270, 70, 18, 55.3, 36)` |
| 位置（`cx/cy` + `localPosition`） | **`HudCanvas.SetAnchorPx(rt, 0.5, 1, 967, 175.1)`** | ✅ **迁了**，而且**落回原版那两个数的原文**：`AnchorApPx(967, 175.1, 0.5, 1) = (7, −175.1)` = 原版 `WaitText` 的 `m_AnchoredPosition`。⛔ 没走 `PosPx`（见 ④ 那条「两套锚点约定」） |

### 1·2 双模式：✅ 成

- `Create(parent)` / `CreateWithArt(parent, popupTex, fillTex)` 新增形参 `RectTransform uguiHost = null`；
  **传 `null`（出厂）⇒ 走 `BuildWorld`，与加本档之前逐位相同**（世界档那一支**逐字未改**，只把 `_shade`
  的赋值多套了一层 `HudPiece.Of(...)`）。
- 生产路已接线：`BattleDriver.BuildHud` 传 `HudCanvas.RootRect`（拿不到根画布 ⇒ 传 null ⇒ 世界档，**并出声**）。
- 先例对照：`ErrorMessageBanner.Create(…, uguiHost)` · `Tooltip.UseHost` · `TutorialOverlay.Create(parent, uguiHost)`。

### 1·3 落点：照 `S9` 那条裁定（⛔ **没建 `Default` 子画布**）

`WaitBanner` 的件**直接挂 `HudCanvas.RootRect`**、用 `HudCanvas.Add(rt, z)` 定兄弟序；本组件自己的节点
退成**不画东西的容器**（原版 `WaitText` 也只是个容器）。**不进 `SubCanvasRank`**（那张表只排子画布彼此）。

### 1·4 注册表 / `ReanchorHud`：**没有要摘的东西**（逐个现核）

`WaitBanner` **从来不在**那四个册子里（`_hudImages` / `_hudImageSpots` / `_hudLabels` / `_hudSpots`），
`ReanchorHud` 也不碰它（它那一轮只处理那两份册子 + `FitDeckPile` / `PlaceDeckLights` / `PlaceHandPlate` /
棋盘 / `selector` / `skillPanel` / `_replayBar` / `_unitChat`）⇒ **无摘除动作**。
画布档下它**也不需要重贴**：三层的锚点自己跟分辨率（这正是本波要的那件事）。

---

## ② 改动清单（`file:行号` · 一句话）

### `Unity/MyGame/Assets/CardPresentation/Battle/WaitBanner.cs`（**327 → 682 行**；下面的行号 = **最终态**现核）

| 行（改后） | 一句话 |
|---|---|
| `:35` / `:37-73` | 文件头补一句口径订正（那两行 `localPosition` 现在在 `BuildWorld()` 里）+ 新增一整段：**两层模型**（`BackCanvas` 的 `overrideSorting = false` ⇒ 并进根画布那一组 ⇒ ⛔ 不建子画布）+ **双模式** + **画布档的形状（那棵树）** + 两套锚点约定 |
| `:72` | 新增 `using UnityEngine.UI;`（画布档要读 `Image.type` / `pixelsPerUnitMultiplier`） |
| `:79-91` | `Z` 的 doc 扩写：画布档下它是**兄弟序档**；**为什么是这一档**（原版子序 6/7 ⇒ 盖住名牌一族、盖不住 `OverHand`/`PopUps`） |
| `:94-98` / `:101-105` | `ImageQuad _shade` → **`HudPiece _shade`**（两档共用）；新增 `bool _ugui` |
| `:113-117` / `:119-122` | 新增常量 `ShadeW/ShadeH = 3963.5/3366`（原版 `Dark Shade` 的 `m_SizeDelta`）· `FillPpuMul = 2f`（原版 `m_PixelsPerUnitMultiplier`） |
| `:157-164` / `:195-202` | 给 `BarFramePieces` / `BarFillPieces` 各补一句：**这两个口只对世界档有判别力**（画布档的框/填充各是 1 张 `Image`） |
| `:221-229` / `:231-242` | `Create` / `CreateWithArt` 加 `RectTransform uguiHost = null`（+ 双模式的 doc） |
| `:245-283` | `New(...)` 按档分流：画布档走 `SetParent(uguiHost)` + `HudCanvas.SetPxSize` + `PlaceAtBarCenter` + `HudCanvas.Add(rt, Z)`；世界档逐字照旧 |
| `:286-294` | `Build(...)` 拆成**分派 + `BuildWorld`**（世界档那一支逐字未改；`:297` 只多套一层 `HudPiece.Of`） |
| `:417-434` | 新增 `BarCenterXPx/YPx` 与 `PlaceAtBarCenter(rt)`（**三层共用同一条五元组**，一处写） |
| `:437-542` | 新增 **`BuildCanvas`**（压暗 / 框 / 填充 / 字四段，逐段带判据与「改坏法」） |
| `:566-665` | 新增画布档读口：`OnCanvas` · `BarCenterPx` · `RootSizePx` · `FrameSizePx` · `FrameIsSliced` · `FillSizePx` · `FillIsTiled` · `FillPpuMulValue` · `ShadeSizePx` · `ShadeHasSprite` · `RootOrderZ` · `ShadeOrderZ` / `PopupOrderZ` / `TextOrderZ` · `ShadeVisible` / `TextVisible` |
| `:667-682` | 🔴 **`SetVisible` 就地修**（顺手发现的真缺陷，见 ⑥·1） |

### `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`

| 行（改后） | 一句话 |
|---|---|
| `:13963-13978` | `BuildHud` 里 `_waitBanner = WaitBanner.Create(root);` → **登记宿主**：`var waitHost = HudCanvas.Ensure(cam);`（幂等）+ `WaitBanner.Create(root, waitHost)` + `waitHost == null` 时 `Debug.LogError` 出声（⛔ 不静默半迁移）。9 行注释写清「⛔ 不建 `Default` 子画布 / ⛔ 不用 `EnsureRankedSubCanvas` / 这件不进 `SubCanvasRank`」 |

### ⚠️ 逐文件 `git diff --numstat` + **行尾核对**（二进制数出来的）

| 文件 | numstat | 行尾（改前 → 改后） | 结论 |
|---|---|---|---|
| `Battle/WaitBanner.cs` | **379 / 24** | **纯 LF**（改前 `CRLF 0 / LF 327` → 改后 `CRLF 0 / bareLF 682`，683 行） | ✅ **行尾没翻**，数字 ≈ 本件的真实增删 |
| `Battle/BattleDriver.cs` | **424 / 54** | **纯 CRLF**（改前 `17626/17626` → 改后 `CRLF 17639 / bareLF 0`，17640 行） | ✅ **行尾没翻**；⚠️ 那个 424/54 **含本波之前就在盘上的别人的改动**（本会话开始时的 `git status` 里这个文件已是 ` M`）—— **本件那一段只有 `@@ -13917 +13964,14 @@` ⇒ +14 / −1** |

---

## ③ 接线清单（白名单外 —— 主对话/别的写手按这个改）

### 3·1 🔴 `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` —— **三条会红**（同一节 §4.5）

> 现场：`var wait = driver.Wait;`（`BattleScene.cs:4397`）拿到的是**生产实例**，而它现在 100% 走画布档
> ⇒ 下面三条读的是**画布 px**，与旧的「世界单位期望」不再同量纲。**改法都是「按 `wait.OnCanvas` 分档」**，
> ⛔ 别把期望值写成 `Px(...)`（那是世界档那把尺）。
> ⚠️ **行号是 2026-10-16 现读、同一天里已经漂过 200+ 行** ⇒ 落地时**按内容搜**：
> `driver.Wait` · `TextWorldW` · `BarFramePieces` · `BarFillPieces` · `ShadeTint`（那一节是 `§4.5`）。

1. **`:4419-4425`（`TextWorldW/H ≤ 框`）**：画布档下 `Label.WorldW/H` 读的是 `TMP.textBounds`，
   **单位就是画布 px**（口径 → `T09` §七·8 末段）⇒ 期望该是 **`WaitBanner.WaitTextBoxW/H`（1270 / 70）**，
   而不是 `LayoutSpace.Px(...)`（那会得到 11.76 / 0.648 ⇒ **必红**）。
   建议：`float bw = wait.OnCanvas ? WaitBanner.WaitTextBoxW : LayoutSpace.Px(WaitBanner.WaitTextBoxW);`（bh 同理）。
2. **`:4486`（`wait.BarFramePieces >= 6`）**：画布档的框是**1 张 `Image (Sliced)`**（`BarFramePieces` 恒 0）⇒ 必红。
   建议改成分档：
   `wait.OnCanvas ? (wait.FrameIsSliced && Mathf.Abs(wait.FrameSizePx.x - 1323f) < 0.01f && Mathf.Abs(wait.FrameSizePx.y - 90f) < 0.01f) : wait.BarFramePieces >= 6`
   （`1323/90` 写**字面量**，⛔ 别引 `WaitBanner.PopupW` —— 那是被测实参）。
3. **`:4501`（`wait.BarFillPieces == 42`）**：画布档的填充是**1 张 `Image (Tiled)`** ⇒ 必红。建议分档：
   `wait.OnCanvas ? (wait.FillIsTiled && Mathf.Abs(wait.FillSizePx.x - 1323f) < 0.01f && Mathf.Abs(wait.FillSizePx.y - 90f) < 0.01f && Mathf.Abs(wait.FillPpuMulValue - 2f) < 1e-4f) : wait.BarFillPieces == 42`。

### 3·2 🆕 该**加**的断言（画布档真正的判别式；判据都在 ④）

在同一节（`wait != null` 那一支里）加一组，全部用**字面量**期望：

- `wait.OnCanvas` —— 生产路**真的登记了宿主**（断「接线没被漏掉」。🧨 把 `BuildHud` 里那个实参删掉 ⇒ 红）。
- `wait.BarCenterPx ≈ (967, 175.1)`（容差 0.01 px）—— 原版 `anchor (0.5,1)` + `ap (7.0,−175.1)`。
  🧨 改坏法：把 `PlaceAtBarCenter` 换成 `localPosition` ⇒ 偏 (父件枢轴) ⇒ 红。
- `wait.RootSizePx == (1344, 79.44)`（原版 `WaitText` 的 `m_SizeDelta`）——
  🧨 删掉 `HudCanvas.SetPxSize` ⇒ 红；⚠️ 别照世界档写 `Px(1344)`。
- `wait.ShadeSizePx == (3963.5, 3366)` **且 `!wait.ShadeHasSprite`**（原版 `Dark Shade` **无图、纯色**）——
  后半条是**灭自证**那一类：只断尺寸的话，给它塞一张图也照样绿。
- **层级两条**（这是本件最要紧的一格）：
  · `wait.RootOrderZ < HudCanvas.DefaultZ`（= 0.3）**且 `wait.RootOrderZ < 0f`** ——
    后者才是「盖得住**名牌一族**」的判据（字的档是 `HudLabelZ = 0`，字在图上）。
  · `wait.ShadeOrderZ > wait.PopupOrderZ && wait.PopupOrderZ > wait.TextOrderZ` ——
    **一条不变量的三段**（`Add` 的 z 大 ⇒ 画在底下 ⇒ 压暗在最下、字在最上）。
    🧨 灭自证：只断「某层 = 某个数」抓不住「两层写反」。
- （可选，量纲安全）`:4479/:4481` 的 `BarWorldW/H` 读的是**本件自己那两个常量**（`U(PopupW)`），
  **不是真读回**；画布档建议顺手换成 `wait.FrameSizePx`。

### 3·3 `§4.5b`（美术取不到那一档，`:4549-4600`）

这一节用的是 `WaitBanner.CreateWithArt(noArtRoot.transform, …)`（**世界档**）⇒ **三条都照旧绿**。
🆕 建议**加一节**：`WaitBanner.CreateWithArt(hostProbe, null, null, HudCanvas.RootRect)`，断
`!noArt.Visible && !noArt.ShadeVisible && !noArt.TextVisible`（后两个口是本件新加的，见 ⑥·1）
+ `warns == 1`（同一个 `[WaitBanner]` 文案，两档共用）。

### 3·4 `§A218`（`:16937-16942`）

那一节也走 `CreateWithArt`（世界档）⇒ **照旧绿**（`Find("wait_popup/wait_fillRoot")` 的树没动）。
🆕 建议照它**并排加一份画布档探针**，把 `RootSizePx` / `FrameSizePx` / `FillSizePx` / `BarCenterPx` 四组
**真 px** 断一遍（`HudCanvas.SizePx` 那条口径：⛔ 别拿世界尺寸 ×108 折回来）。

### 3·5 🔴 `Unity/MyGame/Assets/CardPresentation/Battle/HudCanvas.cs`（**不在本件白名单**）

**缺一个平铺口** —— 请收编成本件 ①·1 那个权宜的正式口，签名建议：
`MakeTiledPiece(Transform parent, Texture2D tex, string name, float wPx, float hPx, float ppuMul, float z, Color color)`。
这与 `T09` §七·3（「`HudCanvas` 缺相对父中心的建图口」）· §七·10（「缺普通图片 + 中心锚的口」）
是**同一族**的账（那两个写手各自造了一个权宜、主对话已说要在同步点收编）⇒ 本件是第三处，⛔ 别让它长出第二份。

### 3·6 ⛔ 什么都不用做的地方（现核）

`BattleDriver.ReanchorHud` / 那四个注册表 / `SubCanvasRank` / `Battle` 其余文件（全仓 `grep WaitBanner`
只剩注释与常量引用，唯一生产调用点就是 `BuildHud` 那一处）。

---

## ④ 判据与出处 · 「选哪一档 z、为什么」 · 16:9 逐位自证

### 4·1 z 档：**取世界档那个 `Z = −0.45`（压暗 `Z+0.02`、字 `Z−0.01`）**，方向同向 ⇒ 值照搬

**为什么是这一档**（判据链，⛔ 不是成本）：

1. **原版事实**：`WaitText` 在 `BackCanvas` 的子序是 **最后一格（6/7）**、`BackCanvas` 的
   `overrideSorting = false` ⇒ 它的压暗**盖住 `LeftArea`/`RightArea` 名牌一族**、**盖不住**
   `FrontCanvas`（`OverHand`）与 `PopUps`（出处 → `S6S9_卡体与残件_施工方案.md` §2·1/§2·2/§3·2）。
2. **落到我们这边**：「名牌一族」= HUD 的**图**那一族（`HudImageZ` 0.3 及其 `−0.01/−0.02/−0.05/−0.055/−0.065`
   ⇒ **0.235~0.6**，另有装饰层 `HudDecorZ = 0.6`）**和字**那一族（`HudLabelZ = 0`，`BattleDriver.cs:16365`）。
   🔴 **字在图上**（`HudLabelZ` 的 doc 明写「字压在 HUD 图之上」）⇒ 只压过图不够，**必须 `z < HudLabelZ(0)`**。
3. `−0.45`（以及 `−0.43`/`−0.46`）**三条都 `< 0`** ⇒ 盖住整整一族 ✓；同时 `< HudCanvas.DefaultZ(0.3)` ✓
   ⇒ **满足主对话那条「z 小于 HUD 图族那一档」**，而且**与世界档的相对次序一字未改**（值照搬）。
4. **`OverHand`/`PopUps` 那些扇窗不受影响**：它们各在自己的子画布上，**层先于 order 比** ⇒ 本来就在本件之上。

⚠️ **如实标**：这三层的 z 值本身**是我们挑的**（原版那三层的 z 关系没有判据，见 `V4b_三件口径.md` §Q2）
—— 本件只保证「**它相对 HUD 两族的先后与原版一致**」+「**它与世界档逐件对得上**」，⛔ 不声称「原版就是这个数」。

### 4·2 16:9 逐位自证（按新落点算 vs 改前逐值）

| 项 | 世界档（改前） | 画布档（改后） | 16:9 |
|---|---|---|---|
| 条心 | 根在 `HudRoot` 原点（屏心）+ `(7, −175.1)px` ⇒ **(967, 175.1↓)** | `SetAnchorPx(0.5,1,967,175.1)` ⇒ `ap = (967−960, 0−175.1) = (7, −175.1)` ⇒ `CenterPx` 读回 **(967, 175.1)** | ✅ **逐位**（而且 `ap` 就是原版那两个数的原文） |
| 根 `WaitText` | `sd = Px(1344) × Px(79.44)` = 12.4444 × 0.73556 世界单位（@108 = **1344 × 79.44 px**） | `sd = 1344 × 79.44 px` | ✅ **逐位**（同一对 px） |
| 压暗层 | 中心 = 屏心 (960, 540)、1134 × 2016 世界单位（= 122472 × 217728 px ？→ 见下注） | 中心 = 条心 (967, 175.1)、**3963.5 × 3366 px** | ✅ **每个像素相同**（见下条「为什么尺寸不同也逐位」） |
| 压暗 α / 色 | `SolidTex` 白底 × `(0,0,0,0.6118)` | `Image` 无 sprite + `color (0,0,0,0.6118)` | ✅ 逐位（白 × 该色 = 该色） |
| 框 | `MenuDraw.Nine` 9 块 quad、1323 × 90 px、border (169,160,169,160) | `Image (Sliced)` 1323 × 90 px、同一 border、`ppuMul = 1` | ✅ 逐位（世界档 `borderOutPx` 缺省 = `border` ⇒ 角块 px 相同） |
| 填充 | 42 块 quad、格距 **64 px**、相位钉矩形左下角 | 1 张 `Image (Tiled)`、`ppuMul 2` ⇒ 格宽 **64 px**、相位同样自左下角 | ✅ 等价（1323÷64 → 21 列 × 90÷64 → 2 行）；⚠️ **层级不同**（见 ⑤·1） |
| 字 | 中心 (967, 175.1)、框 `Px(1270)×Px(70)` = 11.7593 × 0.64815 世界单位、自适应 [18, 55.3] base 36 | 中心同一点、框 **1270 × 70 px**、同一组字号 | ✅ 逐位 |
| 三层先后 | `−0.43`（压暗）> `−0.45`（框）> `−0.46`（字）—— z 大 = 在下 | `Add` 同值同向（z 大 ⇒ 兄弟序小 ⇒ 画在下） | ✅ 逐件对得上 |

> **注（压暗层的尺寸/中心都不同，画面却逐位）**：世界档那个 quad 高 = `DesignHeight × 1.05` = **11.34** 世界单位、
> 宽 = 11.34 × (`VisibleWidth / DesignHeight` = 1.77778) = **20.16** 世界单位 ⇒ 折 px（×108）
> = **2177.28 × 1224.72 px**，中心在**屏心** (960, 540) ⇒ 铺到 x ∈ [−128.64, 2048.64] · y ∈ [−72.36, 1152.36]；
> 画布档是 **3963.5 × 3366 px**、中心在**条心** (967, 175.1) ⇒ 铺到 x ∈ [−1014.75, 2948.75] · y ∈ [−1507.9, 1858.1]。
> 两者**都盖满整屏**（0..1920 × 0..1080）⇒ **每一个屏幕像素都落在同一个 `(0,0,0,0.6118)` 里** ⇒ 画面**逐位相同**。
> 中心不同（屏心 vs 条心）只在「盖不满屏」时才有意义 —— 而「以条心为中心」是**原版那一件的形状**
> （`Dark Shade` 是 `WaitText` 的子件）⇒ 画布档这条更贴原版。

### 4·3 判据出处一览（都去原件取过，⛔ 没抄第二份）

- 五元组 `anchor (0.5,1)` · `ap (7.0, −175.1)` · `sd (1344.0, 79.44)` ——
  运行时 dump `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv:350-355`
  ＋ `bundle_scenes_scenes_battlearena1` 的同名件实读（本文件原有注释里已逐值记着）。
- 三层结构 `Dark Shade`（`sd 3963.5×3366` · α 0.6118 · **无图纯色**）/ `Generic Popup Background`（1323×90 ·
  `m_Type 1` Sliced · border (169,160,169,160) · `m_Rect 359×336`）/ `Mask` / `Background fill`
  （`m_Type 2` Tiled · `m_Rect 128×128` · `m_PixelsPerUnitMultiplier 2.0`）/ `Text`（1269.5×70）—— 同上。
- 那颗 TMP 的框与字号（1270×70 · 18 / 55.3 / **36**）—— `bundle_scenes_scenes_battlearena2/MonoBehaviour/MonoBehaviour_4090.json`（`A2102⑤` / R6 那一轮现读）。
- `HudLabelZ = 0` / `HudImageZ = 0.3` / `HudDecorZ = 0.6` —— `BattleDriver.cs:16355/16365/16371`（现读）。
- `HudCanvas.DefaultZ = 0.3` —— `Battle/HudCanvas.cs:659`。
- `Add` 的 z **大 ⇒ 画在底下** —— `HudCanvas.Group.Add`（按 z 降序插）+ 同向的先例
  （`T09` §五·补⑩ · `UnitChatPanel.ZPortrait` / `BattleLogPanel.ZShade`）。
- 落点（挂 `RootRect` + `Add(z)`、⛔ 不建 `Default` 子画布）—— `S6S9_卡体与残件_施工方案.md` §2·1 / §3·2。
- 那条整屏压暗裁定（「必须画在某个世界件**下面** ⇒ 留世界；必须画在它**上面** ⇒ 上画布」）—— `T09` §八·5。
  本件：它的压暗**必须盖住名牌一族**（那族今天**已经是画布件**了）⇒ **上画布** ✓；而**原版它盖不住**
  `FrontCanvas`/`PopUps` ⇒ 那两族各在自己的子画布上、层更高 ⇒ 本来就在上面 ✓（不需要 z 去表达）。

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. 🔴 **填充层与框的先后：画布档与世界档【不一样】，我知道、并且选了「照原版」那一侧。**
   - uGUI 里**子件的图形恒画在父件自己的图形之后** ⇒ 画布档把填充（框的子件，= 原版 `Generic Popup Background/Mask/Background fill` 的嵌套）画在**框之上**；
     而世界档靠 z 把它画在**框之后**（`WaitBanner.cs` 旧注释：「填充在框后面 —— 原版它是被 `Mask` 裁在框里的」）。
   - **为什么选照原版**：① 嵌套本来就是原版的（世界档那条 z 是**我们**为世界坐标模型绕的弯）；
     ② **uGUI 的次序规则在本仓有先例** —— `HudCanvas.SubCanvasRank` 里那句
     `"ChatPopup Canvas" // 4/7（在 Unit Chat 内部 ⇒ 按「子件画在父件之上」紧跟其后）`
     ⇒ 「子件画在父件之上」是**已落地的口径**，不是我的推断；
     ③ 实测那张图**极淡**：`40k_popup_texture` 的 α **最大值 = 90/255（0.353）· 均值 20.9/255（0.082）· 一个完全不透明的像素都没有**
     ⇒ 它在框上最多是把框按 35% 混一层暗绿，**不会把框盖掉**；
     ④ 原版那两级还有个 `Mask`（把填充裁在框的形状里），**我们两档都没有 Mask** ⇒ 差异的**大头**与本次迁移无关。
   - ⛔ **世界档一个字没改** ⇒ 世界档仍与改前逐位相同 ✓；**画布档**才会看到这一处小差别。
   - 🔧 若主对话认为「宁可先保世界档画法」：把 `wait_fillRoot` 从**框的子件**搬成**框的同级兄弟**、
     给它 `Add(rt, Z + 0.01f)`（框是 `Z`）⇒ 画在框之前 ✓（`BuildCanvas` 里只动两行 + 删一句 `SetParent`）。
2. ⚠️ **`MakeSlicedPiece` 的 `pixelsPerUnitMultiplier` 恒 1**（`HudCanvas.cs:186-187` 自陈：原版那颗
   `Image` 的该字段**没读到**）⇒ 框的角块大小：「原版 `m_Border ÷ ppuMul`」那一格**两档都没有判据**
   （世界档 `MenuDraw.Nine` 也是按裸 border 铺的）⇒ **不是本次引入的偏离**，如实记着。
   同一件事还有 `T09批2_SettingsPanel.md` 那条账（那边原版值读到了 1.09、也只能把 border 预除）。
3. ⚠️ **画布档的压暗层中心与世界档不同**（条心 vs 屏心）—— 见 4·2 那条注：今天两者都盖满整屏 ⇒
   画面逐位相同；**但若哪天把它的尺寸改小到盖不满屏，两者会分家**（判据 = 原版它是 `WaitText` 的子件 ⇒ 以条心为中心）。
4. ⚠️ **`BarHasArt`（`BarFramePieces == 9`）在画布档恒 `false`** —— 这个口全仓没人用（现核），
   本波**不动它**（属于「世界档读口」，与 `BarFramePieces` 同一族，见 ② 那两行 doc）。
5. ⚠️ **`wait_shade` 走的是 `MakeImage` 的归一化锚点**（与它同容器的 HUD 图族同一套），
   框/字走点锚 ⇒ **两套约定**。16:9 逐位相同；比 16:9 **高**的屏上两者差 135 屏 px（`T09` §八·补 那条通式），
   而它压在 **3366 px 高**的压暗层上**看不出来**（盖满整屏这件事不受影响）。**没合并这两套约定**（那是 `HudCanvas`/全线的账）。

---

## ⑥ 顺手发现（只报不改 / 一条已顺手修）

1. 🔴 **【已顺手修 · 铁律 5·b + 14 三条判据全中】`SetVisible` 在「美术取不到」那一档会让压暗层与文字永久挂在屏上。**
   - **改前**：`SetVisible(on)` 第一句是 `if (_popup == null) return;`。而压暗层与文字在 `Build` 里
     **建出来就是 active 的**，`Visible` 的判据**只有 `_popup`** ⇒ 那一档下
     **`Visible` 报 `false`、屏幕上却挂着一整屏 `α0.6118` 的黑罩 + 那行字**，而且 `UpdateHud` 每帧调
     `SetVisible(false)` 会被这一句**早退**掉 ⇒ **盖到对局结束**（美术目录被删 / `40k_popup` 取不到 —— 本工程明确支持的退路）。
   - **改后**：三件一起开关；框在 ⇒ 照旧按它早退（重复调用不重复设置的语义不变）；框不在 ⇒ 另外两层照样关掉。
   - ⚠️ **`Editor/BattleScene.cs` §4.5b 那句括号（「`Visible` 假 —— 不是半截挂着一块压暗」）在改之前【是没有判据的】**
     ⇒ 新加了两个读口 `ShadeVisible` / `TextVisible`，并在 ③·3 里给了该补的断言。
   - ✅ **世界档正常那一支（框在）行为一字未变** ⇒ 既有断言不受影响（已核 §4.5b 三条与 §A218）。
2. ℹ️ **`BarWorldW` / `BarWorldH` 不是真读回**：它们是 `U(PopupW)` / `U(PopupH)` = **本件自己那两个常量的算术**，
   所以 `BattleScene.cs:4479/:4481` 那两条**恒真**（「尺子自己会坏」那一族）。本波**没动**（白名单外的断言），
   但建议顺手换成 `wait.FrameSizePx`（画布档）/ `HudPiece.WorldW`（世界档）——已写进 ③·2 末条。
3. ℹ️ **`MakeSlicedPiece` 内部 `Sprite.Create(..., border)` 用的是 `tex` 的实际尺寸** ⇒ 框那张 `40k_popup`
   在工程里是 359×336（= 原版 `m_Rect`）✓，`MenuDraw.Nine` 那边还得显式传 `359f/336f`（本件画布档**不需要**、
   也就**没有第二个地方写这个数**了）—— 一处小收获。
4. ℹ️ **本件是块 6 里唯一「不进 `SubCanvasRank`」的一件**（因为它不建子画布）⇒ 单排它的
   `HudCanvas.IsRanked(name)` 那条警告**不该**加在它身上（`EnsureRankedSubCanvas` 的语义是「表里的名字」）。

---

## 📌 可直接粘进总表的一行

- ✅ **`T09` 块 6 第一件（`WaitBanner` / 原版 `WaitText`）：已迁 uGUI**（挂 `HudCanvas.RootRect` + `Add(z)`，⛔ 不建 `Default` 子画布；双模式 `uguiHost`，`BattleDriver.BuildHud:13973` 已接线）· typecheck 0/0 · 顺手修掉「美术取不到时压暗层永久挂屏」的真缺陷。
- ⚠️ **欠**：`HudCanvas` 补一个平铺口 `MakeTiledPiece`（本件用临时权宜顶的，见 `EW7` 报告 ③·5）· `Editor/BattleScene.cs` **三条断言要按 `wait.OnCanvas` 分档**（`§4.5` 的 `TextWorldW/H` 与 `BarFramePieces>=6` / `BarFillPieces==42`）+ 补一组画布档断言（报告 ③·2）。
- ℹ️ **一处如实记的差**：画布档里填充（框的子件）画在**框之上**（uGUI 子件恒在父件图形之后）= 原版嵌套；世界档那条「填充在框后面」是旧的 z 绕法（实测那张图 α 最大仅 0.353 ⇒ 差异极小），见报告 ⑤·1。
