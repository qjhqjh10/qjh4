# W15 · AspectFiller 迁 / ReplayBar 迁 / in-hand 档断言

> 2026-10-17 · 执行写手 W15 · 三个文件互不相交：
> `Battle/BattleDriver.cs`（只动 `AspectFiller` 那一族）· `Battle/ReplayBar.cs` · `Editor/BattleScene.cs`（只加 in-hand 断言）。
> ⛔ 未跑 Unity 批处理 / 未起 player / 未动 git / 未改正本。

## 一、① AspectFiller：原版 layer/order 怎么读到的（或「读不到 + 出声」）· 改前 → 改后

**原版 layer / order 已读到（R5 留的那条「没查清」本次现核，判据 = `battlearena2` 现读）**：

| 项 | 实读值 | 出处 |
|---|---|---|
| 组件表 | 只有 `RectTransform + CanvasRenderer + Image` ⇒ **没有 `Canvas`、没有 `CanvasGroup`** | `bundle_scenes_scenes_battlearena2/GameObject/Aspect Ratio Filler {Top,Bottom}.json` |
| 祖先画布 | `BattleHud/Canvas`（RT 2952 / GO 1006）的 `Canvas_2765`：`m_RenderMode 1` · **`m_SortingLayerID = 0`(Default)** · **`m_SortingOrder = 0`** · `m_OverrideSorting = false` | `Canvas/Canvas_2765.json` |
| 组内序 | 它俩正是该 canvas 的 **`m_Children[0] / [1]`**（`RT_2952.m_Children = [3214, 3109, 2877, …]`）⇒ **画在所有 UI 之下** | `RectTransform/RectTransform_2952.json` |
| RT 值 | `anchor = pivot = (0.5,0.5)` · `sd = 3252.123046875 × 1842.02001953125` · Top `ap (0.0006713899783790112, 1641.0999755859375)` · Bottom `ap (−3.051800013054162e-05, −1641.0999755859375)` | `RectTransform/RectTransform_{3214,3109}.json` |
| Image 值 | `m_Sprite = 0` · `m_Color = (0,0,0,1)` · `m_RaycastTarget = 0` · `m_Type = 0`(Simple) · `m_PreserveAspect = 0` | `MonoBehaviour/MonoBehaviour_{5252,5423}.json` |

⇒ 它不另立排序组，属于根画布那一组；**我们的等价物 = 根画布 + 最大的 `z`**（`HudCanvas.Add` 按 z 降序插兄弟序 ⇒ z 大 = 最底下）。

**改前 → 改后（`Battle/BattleDriver.cs`）**：
- 字段 `ImageQuad _aspectFillerTop/_aspectFillerBottom` → **`HudPiece`**（走适配层）。
- `BuildOneAspectFiller`：`ImageQuad.Create(root, whiteTexture, FromPixel(cx,cy), Px(H), …)` +
  `SetAspect(W/H)` + `SetTint(黑)` + `SetRenderQueue(2999)` + `world z += 5`
  → **`HudCanvas.Ensure(cam)` + `HudCanvas.MakeAnchored(canvas, whiteTexture, name, 0.5, 0.5, pivot(0.5,0.5), H, W, z=5, 黑)`**
  + **`Rect.anchoredPosition = (AspectFillerTopX/Y | BottomX/Y)`**（原版 `m_AnchoredPosition` **逐值照抄**）。
- `BuildAspectRatioFillers()` **不再吃 `root`**（uGUI 件一律挂 `HudCanvas.RootRect`，⛔ 不挂 `hudRoot`=反向补震用的）；
  调用点同步改成无参。
- 根画布建不起来 ⇒ `Debug.LogError` + 返回 null（⛔ 不静默退回世界空间 quad = 半迁移）。
- 文案订正：旧注释引用的是 `battlearena1` 的 RT/MB 号（3021/2916/5038/5206），本次全部换成 `battlearena2`
  的实读号（3214/3109/5252/5423），并写明「16:9 恒在屏外」现在也与原版同口径（旧世界坐标下**任何比例**都露不出来）。

## 二、② ReplayBar：同上

**原版 layer / order 已读到（判据 = `battlearena2` 现读）**：
- 父链：`Replay (RT 3484) <- Holder (RT 3133) <- ReplayButtons (RT 3550) <- Safe area (RT 3693) <- BackCanvas (RT 2877) <- Canvas (RT 2952)`。
- `BackCanvas` 的 `Canvas_2768`：`m_RenderMode 2` · **`m_SortingLayerID = 0`(Default)** · **`m_SortingOrder = 0`** · `m_OverrideSorting = false`；
  根 `Canvas_2765` 同为 `Default / 0 / false` ⇒ **不另立排序组**，画在根画布这一组里，层内先后 = 兄弟序。
- `ReplayButtons`：`anchor (0.5,1)` · `pivot (0.5,0.5)` · `ap (−403, −66)` · `sd (293.6000061035156, 57.40599822998047)`。
- `Holder`（`RT_3133`）：`anchor/pivot (0.5,0.5)` · `ap (−11.47700023651123, 0)` · `sd (258.5379943847656, 59.428001403808594)`。
- 四枚钮（`RT_{3484,2953,3347,3002}`）：`sd (79.8, 48.57)` · `ap x = −86 / −6.9487762451171875 / −6.9487762451171875 / +72.251220703125` · `y = 0`。
  （Play/Pause 同座标互斥；`PA = 0` ⇒ 拉满矩形，不按贴图 95×59 比例。）

**改前 → 改后（`Battle/ReplayBar.cs`）**：
- 字段 4 × `ImageQuad` → **`HudPiece`**；`Make` 改走 `HudCanvas.MakeImageInParent(_holder, tex, name, apX, 0, 79.8, 48.57, z, white)`。
- 根 `Create`：`MenuDraw.SetPxSize`（世界单位 ÷108）→ **挂 `HudCanvas.RootRect` + 原版锚点/偏移/`sizeDelta`（设计 px）**；
  `parent` 参数保留但对 uGUI 档不再使用（⛔ 不挂 `hudRoot`）。
- `Holder`：`localPosition=Spot(...)` → **中心锚 + `ap (−11.477, 0)` + `sd (258.538, 59.428)`**。
- 删掉 `Spot` / `Place`；`RefreshLayout()` 变成**空操作**（uGUI 锚点自己跟画布缩放）。
  ⚠️ 仍保留该口 = `BattleDriver.ReanchorHud` 还在调它；文件白名单只允许改 `AspectFiller` 那一族，
  故**没有动 `BattleDriver.cs:17011` 的调用点**（空操作、无副作用）。
- 根画布建不起来 ⇒ `LogError`（不静默、不退回世界 quad）。

## 三、③ in-hand 断言：补了哪几条 · 每条断什么 · 灭自证是什么

位置：`Editor/BattleScene.cs`（`A156` 那组常态档三条之后，+~65 行）。

**A. 编译期数锚（in-hand 档第一次有独立断言）**
1. `HitHalfHInHand == 1.5251`（原版 `3.02 × 1.01 ÷ 2`；🧨 改成 ctor 默认 `1.2` ⇒ 红）。
2. `HitCenterDyInHand == −0.0151`（原版 `−(3.0502−3.02)/2`）。
3. `HitCenterDyInHand + HitHalfHInHand == 1.51` —— **上沿不动**（原版「只往下拉长」；改成一格居中 ⇒ 上沿 1.5251 ⇒ 红）。

**B. 运行时断言（读回实际输出，补 W14 指出的弱锚）**
- 真建一张 `CardView` 探针（`CardFace.Full`），用 `Contains` 沿**局部 y 轴二分 40 次**求出「还命中」的上下沿
  （x 恒取 0 ⇒ 与半宽无关；`FaceLayerRoot` = `Contains` 用的内层 `2DCard`）。
- 常态档：`[−1.51, +1.51]`（中心 0）。
- `SetFace(CardFace.Hand)` 后：**上沿仍 `+1.51`** · 下沿 `−1.5402` · 由实测反算 半高 `1.5251` / 中心 `−0.0151`（与字面量比，容差 1e-3）。
- **灭自证**：上面 A 组读的是**常量**，挡不住「`HitRect()` 里另加偏移」；B 组量的是 `HitRect()` + `Contains` 的**实际输出**
  ⇒ 在 `HitRect`/`Contains` 里加任何额外偏移都会让上下沿/中心偏掉而红。期望值一律写**字面量**，⛔ 不读 `CardView` 的常量回比。
- 探针 `DestroyImmediate` 收尾（不残留节点）。

## 四、常态档那条弱锚（运行时断言补没补上，为什么）

**补上了**。`HitCenterDy` 是 `const 0`，编译期那条 `Abs(HitCenterDy) < 1e-5` 只能证明常量没被改；
本批新增的运行时二分（`Contains` 实测上/下沿）覆盖常态档 `±1.51` 与 in-hand 档 `[−1.5402, +1.51]`
⇒ `HitRect()` 里对**任一档**另加偏移都会红。⚠️ 唯一残留盲区：若有人**同时**改常量与断言字面量（同义反复），
那属于「改判据」而不是「改实现」，不在本组防线内（与既有三条的防线同性质）。

## 五、类型检查结果

`TMPDIR=/tmp/wf_w15 bash d:/4/Unity/工具/typecheck.sh`（改完每段各跑一次，末次结果）：
- 运行时程序集：**0 错**
- 编辑器程序集：**0 错**
（中途一次唯一的错是本批自己写的 `CardData` struct 三元写 `null`，已改成 `default(CardData)`。）

## 六、没能判 / 未落的（⛔ 不猜）

1. **16:9 下三件的画面**：按现行口径未跑 Unity / 未截图 —— 视觉等价性**未验收**；判据侧只做到「参数/层序逐值一致」。
   非 16:9 下 `AspectFiller` 的露出时机依赖 `CanvasScaler(Expand)` 的运行时画布矩形，也**未实拍**。
2. **`AspectFiller` 的 Image 渲染材质**：原版 `m_Material = null`、`m_Sprite = null`，UIKit 默认材质对纯色空图的行为
   未在运行时读过（我们照 `MakeAnchored` 既有口径用白 texture + 纯色，与 HUD 图族同一套）。
3. **ReplayBar 的跨画布排序**：原版挂在 `BackCanvas`（非 override 的嵌套 canvas）里；我们没有那一级嵌套（直接挂根画布），
   `z=0.3` 与 HUD 图族同档 —— 与原版「同一排序组」等价，但**没有逐件兄弟序对照**（原版 BackCanvas 内部还有 7 个子件）。

## 七、顺手发现（⛔ 没动手）

1. `Battle/HudCanvas.cs` 文件头/注释里「还没迁的四格 = DebugButtons/PopupAnchor/Tutorial/LoadingMenu」**已过期**
   （R5 已指出；Tutorial/LoadingMenu 早已迁，另两个本就不建）—— 该文件是只读白名单，未改。
2. `ReplayBar.cs` 文件头上半段仍引用 `battlearena1` 的 GO/RT 号（143/3355/3498 等），与本次 `battlearena2` 实读号并存；
   两处指的是同一套层级，但**两套编号混写**容易误导（本批只在下半段补了 `battlearena2` 的现读值，未重写旧段）。
3. `Editor/BattleScene.cs` 的 `A218` 探针 `ReplayBar.Create(a218root.transform)` 现在**不再挂 `a218root`**（挂根画布）；
   它是幂等探针、不断言父链，故不受影响；但若将来给 `a218root` 做「子树清点」类断言，需要知道这一件已经不在它下面。
