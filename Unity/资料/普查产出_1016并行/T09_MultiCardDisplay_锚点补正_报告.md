# T09 · MultiCardDisplay 锚点核与补正（执行代理 A11）

> 本件结论一句话：**面板那一级（画布档的 `Generic Multi Card Display Combat`）【已经是 x 轴拉伸 + y 轴点锚】，
> 与原版逐值一致 ⇒ 【无需改】，本件【一行代码都没改】**（`git diff HEAD -- MultiCardDisplay.cs` 空）。
> 但**同一扇的下一级**（`BattleContinueButton` 那一组）确实还是「按中心固定 px」⇒ 21:9 下偏 320 px，见 §⑥。

---

## ① 结论 —— 现状是哪种写法

**画布档下面板那一级 = 【按锚点】（x 轴 stretch），逐值 = 原版。**

`MultiCardDisplay.Create()` 里那个「画布本体」节点（`MultiCardDisplay.cs:223-231`）：

| 项 | 原版 `Generic Multi Card Display Combat`（RT_3370, arena2） | 我们 `_panel` | 行号 |
|---|---|---|---|
| `m_AnchorMin` | `(0, 0.5)` | `prt.anchorMin = new Vector2(0f, 0.5f)` | `MultiCardDisplay.cs:226` |
| `m_AnchorMax` | `(1, 0.5)` | `prt.anchorMax = new Vector2(1f, 0.5f)` | `MultiCardDisplay.cs:227` |
| `m_Pivot` | `(0.5, 0.5)` | `prt.pivot = new Vector2(0.5f, 0.5f)` | `MultiCardDisplay.cs:228` |
| `m_AnchoredPosition` | `(0, 0)` | `prt.anchoredPosition = Vector2.zero` | `MultiCardDisplay.cs:229` |
| `m_SizeDelta` | `(0, 818.0399780273438)` | `prt.sizeDelta = new Vector2(0f, BandBottomPx − BandTopPx)` = `(0, 818)` | `MultiCardDisplay.cs:230` |

⇒ 两轴性质 = **x 轴 stretch（吃父宽）· y 轴点锚（固定 818）**，与原版**同一种写法**
（818 vs 818.04 的取整是本文件一直在用的那一档，`MultiCardDisplay.cs:190-191` 已如实标着）。

**父链已核**：`_panel` 的父 = `HudCanvas.EnsureSubCanvas(...)` 建的子画布 RT（`MultiCardDisplay.cs:204-207`），
而它自己是**满屏 stretch**（`HudCanvas.cs:442-446`：`anchorMin 0 / anchorMax 1 / sizeDelta 0`）
⇒ 父宽 = 根 canvas 宽 ⇒ 本节点的 stretch 真的会跟着画布宽走（不是「stretch 了一个固定宽的父」）。

**🔴 唯一那个「两轴固定 px」的节点是世界壳，它不是原版这一级的对位件**（⛔ 别误判成缺陷）：

- `MultiCardDisplay.cs:196-198` 那个 `MultiCardDisplay` 节点走 `MenuDraw.SetPxSize(...)`，
  而 `MenuDraw.SetPxSize` 的体内是 `anchorMin = anchorMax = pivot = (0.5,0.5)` + `sizeDelta = Px(w), Px(h)`
  （`Shell/MenuDraw.cs:155-164`）⇒ **重合锚 + 固定尺寸（两轴都固定）**，无拉伸。
- 但它**不在画布上**（它是「世界壳」：只装**世界物件**卡片与整屏压暗，见 `MultiCardDisplay.cs:27-47` 与 `:192-195`）——
  世界空间里「stretch 锚点」**没有语义**（没有父矩形可锚），它的 `sizeDelta` 既不参与渲染、也不参与裁剪
  （本扇没有 `RectMask2D`/`ViewportClip`；`Editor/BattleScene.cs:16572-16574` 是**唯一**读它矩形的地方，
  且那一条的文案就写着「该节点已降级成世界壳」）。
- 原版这一级（`Generic Multi Card Display Combat`）在我们的画布档里由**两个节点**分担
  （文件头 `:27-36` 那张「世界壳 + 画布本体」表）⇒ **真正对位的是 `_panel`**，而它已经是 stretch。

**子件对位（逐处 `文件:行号`）**

| 原版子件（arena2 pid / arena1 pid） | 原版锚点（实读） | 我们的对位件 | 我们的写法 | 判定 |
|---|---|---|---|---|
| `Viewport`（RT_3371 / a1 RT_3176） | `aMin(0,0) aMax(1,1) sd(0,0)` · `localScale 0.7` | **没有对位节点** —— 卡片直接挂世界壳（`MultiCardDisplay.cs:411` `CardView.Create(_root, …)`）；那 0.7 已折进 `CardHeightPx = 520.3`（`:126-139`，账 `A2105`） | —— | 见 §⑤（卡片未迁，属第 6 步） |
| ↳ `Content`（RT_3369 / a1 RT_3174） | `aMin(0,0) aMax(1,1) apos(−0.00085,0) sd(−1917.688,0)`（= `ContentSizeFitter` 跑之前的塌缩值） | 无（同上） | —— | 同上 |
| ↳ `CardUI Reference`（RT_3616） | `aMin=aMax=(0,1) apos(1.272,−409) sd(2.5437,3.3686) localScale **223.14**` | 无（卡片不走节点树） | —— | 同上 |
| `Menu Dark Background`（RT_3176 / a1 RT_2983） | `aMin=aMax=(0.5,0.5) piv(0.5,0.5) apos(0,0) sd(4574.6, 2572.36)` · **点锚（固定尺寸）** | `mcd_mask`（`MultiCardDisplay.cs:249-256`） | **世界空间** `ImageQuad`，高 = `LayoutSpace.DesignHeight × 1.2`、宽 = 高 × 实比(`VisibleWidth/DesignHeight`) ⇒ 宽 = **1.2 × 实拍可见宽** | **不是画布件**（有意，见 `:37-47` 三条理由）；**尺寸上「任意宽高比都盖得满」**（21:9：1.2×23.33 = 28.0 世界单位 ≥ 23.33 ✓），性质上也 = 点锚固定尺寸 ⇒ 与原版同档 |
| `Header Text`（RT_3586 / a1 RT_3391） | `aMin=aMax=(0.5,1) piv(0.5,1) apos(0,0) sd(1192.37, 63.204)` | `mcd_header`（`MultiCardDisplay.cs:272-275`） | `HudCanvas.PosPx(960, BandTopPx + HeaderH*0.5)` ⇒ 面板**中心 x** + 相对顶边 `HeaderH/2`，中心锚 | ✅ 与原版同语义（`anchor.x = 0.5` ⇒ 画布变宽时**不许**跟着移） |
| `BattleContinueButton`（RT_3251 / a1 RT_3058） | **`aMin=aMax=(1,0) piv(1,0) apos(−24.226,−53) sd(832.89, 49.84)`** ⇒ **右-下锚**；子树 `Button`(577.5×63.84, apos 132.11) · `Text`(见 §⑥-b) · `CircleButton`(80.469×79.636, apos 284.13) | `mcd_bar`(`:303-305`) · `mcd_circle`(`:309-313`) · `mcd_continue`(`:322-324`) | **三件直接挂面板、按【相对面板中心】的固定 px 偏移**（`BarCx − 960`、`CircleCx − 960` …） | ❌ **写法不同**（原版锚右边、我们锚中心）⇒ 21:9 下偏 **320 px**，见 §④/§⑥-a |

## ② 改动清单

**无。** `MultiCardDisplay.cs` 一个字没动（本件判为「已经是 x 拉伸」），
`git diff --numstat HEAD -- Unity/MyGame/Assets/CardPresentation/Battle/MultiCardDisplay.cs` = **空**。

类型检查（改前 = 改后同一份源码）：
`TMPDIR=/tmp/wf_a11 bash d:/4/Unity/工具/typecheck.sh` ⇒ `运行时错误数: 0` · `编辑器错误数: 0`。

## ③ 接线清单（白名单外的件、及哪些调用点将来要跟着动）

**本件不需要任何接线改动**（没改代码）。以下是「若将来要动这一扇」必须一起看的表：

| 文件:行号 | 现在读的是什么 | 结论 |
|---|---|---|
| `Editor/BattleScene.cs:16578` | `box(a218Mcd.PanelRect, "多卡窗**本体**（画布上）", a218CvW, 818f, …)`，其中 `a218CvW = HudCanvas.RootRect.rect.width`（`:16539-16541`） | ✅ **已经是「画布宽」口径**、与 x-stretch 自洽 ⇒ **不用改**。⚠️ 但**它挡不住回退**：批处理屏 640×480 ⇒ `scaleFactor 0.3333` ⇒ 画布宽恰好 = **1920**（`:16536-16538` 的注释）⇒ 「改回固定 px」在这条断言下**照样绿**（见 §④ 表：16:9/4:3 两套写法同值）。若主对话要挡，建议另加一条**结构式**断言（不依赖画布宽）：`anchorMin == (0,0.5) && anchorMax == (1,0.5) && sizeDelta.x == 0` —— **不在我的白名单内，只提不改** |
| `Editor/BattleScene.cs:16572-16574` | `box(a218Mcd.transform, "多卡展示窗**世界壳**", 1920f, 818f, …)` | ⛔ **别跟着改**：世界壳有意是「重合锚 + 固定 1920×818」（世界空间没有 stretch），改它会连带毁掉卡片那一支的 `Pos()` 量纲（`MultiCardDisplay.cs:192-195`） |
| `Editor/BattleScene.cs:8149-8160` | `availPx = 1920f − 120f` 与 `mcd.ContentWidthPx ≤ availPx + 1f` | 与 `MultiCardDisplay.cs:385` 的 `LayoutSpace.DesignPxW − SideMarginPx*2` **成对**。若将来把 `availPx` 改成跟画布宽走（见 §⑥-c），**这一条要一起改**（否则非「画布宽 == 1920」档下会红） |
| `Editor/BattleScene.cs:21157-21168`（`A964/E2④`） | 读 `ContinueBarWorldPos / ContinueBarDrawnPx / ContinueCircleWorldPos / ContinueCircleDrawnPx`，并用 `PxPerUnit964 = 108f`（`:20765`）做 px↔世界换算 | 本件不影响（没动）。⚠️ **将来修 §⑥-a 时必动**：那 108 是「x 斜率」的 16:9 值（`LayoutSpace.cs:228-234` 已就地订正过这条口径） |
| `Battle/BattleDriver.cs:14505` | 生产路径唯一建造点 `_multiCards = MultiCardDisplay.Create(root)` | 无影响 |
| `Battle/HitContinue` 的读者 | 生产路径**不调**（`BattleDriver.TickMultiCards` 走「松手即关」），今天只有 `A964/E2④` 在用（`MultiCardDisplay.cs:469-472` 自己写着） | 无影响 |

## ④ 判据与出处 + 16:9 / 4:3 / 21:9 三档算式对照

**判据（全为解包实读，本件现核，不是转抄）**

- 原版根：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3370.json`
  `m_AnchorMin (0,0.5)` · `m_AnchorMax (1,0.5)` · `m_Pivot (0.5,0.5)` · `m_AnchoredPosition (0,0)` · `m_SizeDelta (0, **818.0399780273438**)`；
  GO 名 `Generic Multi Card Display Combat`、父 = `Safe area FrontCanvas`（RT_3055）。
  🔴 **battlearena1 的同名副本逐值相同**（根 = `RT_3175`，同五条字段）⇒ 两场不是两种做法。
- 子件（arena2；arena1 括号内）：`Menu Dark Background` RT_3176(a1 RT_2983) · `Header Text` RT_3586(a1 RT_3391) ·
  `Viewport` RT_3371(a1 RT_3176, scale 0.7) → `Content` RT_3369(a1 RT_3174) → `CardUI Reference` RT_3616(scale 223.14) ·
  **`BattleContinueButton` RT_3251(a1 RT_3058) `aMin=aMax=(1,0) piv(1,0) apos(−24.2258,−53) sd(832.89,49.84)`**。
- 名字是用「GO 的 `m_Component` 里含该 RT pid」反查出来的（`bundle_scenes_scenes_battlearena2/GameObject/*.json`），
  不是按 pid 猜的（⛔ 本机踩过「同 pid 在两份 CAB 里是不同对象」：arena2 的 `RT_3058` **不是**那颗钮，arena1 的才是）。
- 画布口径：根 `CanvasScaler` = `ScaleWithScreenSize` / `ReferenceResolution (1920,1080)` / `ScreenMatchMode Expand`
  ⇒ `scaleFactor = min(w/1920, h/1080)`（简报逐值给的口径，本件沿用，未重推）。

**算式**（uGUI 通式：`rect.W = |aMax.x − aMin.x| × 父宽 + sd.x`；`rect.H = |aMax.y − aMin.y| × 父高 + sd.y`；
父 = 子画布，满屏 stretch ⇒ 父宽/父高 = 画布宽/高）

先算画布：

| 屏 | scaleFactor | 画布（单位） | 画布宽 |
|---|---|---|---|
| 16:9 1920×1080 | `min(1.0, 1.0)` = **1.0** | 1920 × 1080 | **1920** |
| 4:3 1440×1080 | `min(0.75, 1.0)` = **0.75** | 1920 × **1440** | **1920** |
| 21:9 2560×1080 | `min(1.3333, 1.0)` = **1.0** | **2560** × 1080 | **2560** |
| （批处理 640×480） | `min(0.3333, 0.4444)` = 0.3333 | 1920 × 1440 | 1920 |

面板本体 `rect`：

| 档 | **现在（= 原版，锚点式）** | 若真是「两轴固定 px」(重合锚 + `sd 1920×818`) | 差 |
|---|---|---|---|
| 16:9 | `1 × 1920 + 0` = **1920** × `0 × 1080 + 818` = **818** | 1920 × 818 | **0 px** ⇒ **这档分不出两套写法** |
| 4:3 | `1 × 1920 + 0` = **1920** × `0 × 1440 + 818` = **818** | 1920 × 818 | **0 px** ⇒ **4:3 也分不出**（`Expand` 下画布宽仍是 1920，只是变高） |
| 21:9 | `1 × 2560 + 0` = **2560** × **818** | 1920 × 818 | **640 px**（每边 320）⇒ **本件本来要修的就是这一格，而我们【已经是左边那列】** |

**自证（简报 §二·4 要的那两问）**

1. **1920 宽画布（16:9 与 4:3）**：按锚点算得 `1920 × 818` —— 与「我们原来那套值」（= 文件原本写死的那对、
   也是 `Editor/BattleScene.cs:16578` 的期望值 `a218CvW(=1920) × 818`）**相等**（本件没改代码，故「改前 = 改后」恒等）。
2. **2560×1080（21:9）**：锚点式 = `2560 × 818`；固定 px 式 = `1920 × 818`
   ⇒ **两者的差 = 640 px（每边 320）** —— 这就是本件要确认「有没有」的那个差，**我们这一级没有这个差**。

**同一扇【下一级】的差（顺手算的，见 §⑥-a，只报不改）**

`BattleContinueButton` 组，原版 `piv(1,0) apos(−24.2258,−53) sd(832.89,49.84)`
⇒ 组心从画布左起 = `W − 24.2258 − 832.89/2` ⇒ 条心 = 组心 `+ 132.11`、圆钮心 = 组心 `+ 284.13`：

| 档 | 原版条心 | 我们条心 | 差 | 原版圆钮心 | 我们圆钮心 | 差 |
|---|---|---|---|---|---|---|
| 16:9 | 1611.44 | 1611.45 | −0.01 | 1763.46 | 1763.45 | +0.01 |
| 4:3 | 1611.44 | 1611.45 | −0.01 | 1763.46 | 1763.45 | +0.01 |
| 21:9 | **2251.44** | **1931.45** | **+319.99** | **2403.46** | **2083.45** | **+320.01** |

闭式：差 = `(画布宽 − 1920) / 2`（我们锚中心、原版锚右沿 ⇒ 画布一宽就分家）。
（我们的数是 `BarCx − 960 = 651.45` / `CircleCx − 960 = 803.45` 加画布中心；原版的数是上面那条右锚链算出来的。）

## ⑤ 没查清 / 没做的

- ⛔ **没跑任何 Unity**（红线）：不上画布实拍、不跑 `*.Run`、不进游戏看 21:9 观感 ⇒
  「21:9 下这扇长什么样」是**算式结论**，不是实拍结论。
- **原版 `Content` 运行时的实际宽没查清**：静态表里 `sd.x = −1917.688` 是 `ContentSizeFitter` 跑之前的塌缩值
  （原版真正的排布是 `ScrollRect` 横向滚 + `RectMask2D` 裁）。它影响「原版一屏放几张」，**不影响本件结论**
  （我们自己的「装不下就等比缩」本来就是替代机制，见文件头 ③）。
- **卡片那一支仍未迁**（`Viewport/Content/CardUI Reference` 三级在对位表里是「无」）：
  卡片直接挂世界壳（`:411`），`SetPose` 收世界坐标 ⇒ **非 16:9 下卡片与画布那一组图/字会分家**
  （`:405-410` 已如实记，随第 6 步「手牌/场上卡迁 uGUI」一起消失）。**本件没碰它。**
- **`mcd_mask` 的 21:9 覆盖**只做了算式核对（宽 = 1.2 × `VisibleWidth` = 28.0 ≥ 23.33）⇒ 覆盖成立；
  **未实拍**。
- 没查：本扇在**比 16:9 更「窄」**（如 4:3 竖屏 / 16:10）下原版根与我们的 `rect` 是否仍逐位一致 ——
  按算式（父宽恒 1920/更窄时 `scaleFactor` 取 w/1920 ⇒ 画布宽恒 = 1920）**应一致**，但**没逐个档列**。

## ⑥ 顺手发现（不属于本件的，只报不改）

**a. 🔴 `BattleContinueButton` 那一组是「右-下锚」，我们写成了「按中心固定 px」⇒ 21:9 下偏 320 px**（详 §④）
- 判据：RT_3251 `aMin=aMax=(1,0) piv=(1,0) apos(−24.2258,−53) sd(832.89,49.84)`（arena2；arena1 的 RT_3058 逐值相同）。
  按右锚链反算出来的条心/圆钮心在 16:9 下 = **1611.44 / 1763.46**，与我们写死的 `BarCx=1611.45` / `CircleCx=1763.45` 逐位吻合
  ⇒ **数值当初取的是同一个 1920 基准，只是把锚点丢掉了**。
- ⛔ **本件没改，理由是「不是单文件活」**（铁律 14 的「迅速」三条里的 ② 过不了）：
  条/圆钮一挪，`HitContinue`（`MultiCardDisplay.cs:475-493` 的硬写 px 矩形 + 它的 `InverseTransformPoint` 口径，
  `A1061` 刚订正过）与 `Editor/BattleScene.cs:21157-21168`（`A964/E2④`，用 `PxPerUnit964=108` 折 px）**必须同步改**，
  否则会**新造**一个「画到哪 / 判到哪」不一致（比今天更糟）。⇒ 建议**立账、单独一件**。

**b. 🟡「继续」两个字的位置：我们比原版右了 ≈168 px，字压到了圆钮上**（与宽高比**无关**，16:9 下就如此）
- 原版 `Text`（RT_3020 / a1 RT_2827）：`aMin(0,0) aMax(0.770243,0.9) piv(0.5,0.5) apos(145.1,0) sd(−354.2,0)`
  ⇒ 框 = `287.10 × 44.856`、**从左起 x[1385.05, 1672.41]**；TMP（MB_4083）`m_text='Continue'` · `m_fontSize 38` ·
  **`m_HorizontalAlignment = 4 (Right)`** · `m_margin = 0` ⇒ **字形右沿 ≈ 1672.41**（= 圆钮左沿 1723.2 再往左 50 px）。
- 我们：`_barText` 字心 = `BarCx + BarW*0.5 − 60` = **1840.20**（`MultiCardDisplay.cs:322`）
  ⇒ 比原版字右沿还右 ~168 px；按 fs38 拉丁大写 ≈27.4 px 估字宽 ≈150 px（**未实测**）⇒
  字大致落在 `[1765, 1915]`，**压在圆钮 `x[1723.2,1803.7]` 上、并越过条的右沿 1900.2 约 15 px**。
- 文件头 ④（`:61`）已如实把它标成「**我们挑的**近似」⇒ 属铁律 11 那一类（要做）。
  ⛔ **本件没改**：`Label` 只有居中对齐，要正确复刻得先「量出字宽再左移字心」（TMP 激活顺序那条坑也在），
  够不上铁律 14 的「① 判据已查清」。⇒ 建议立账。

**c. 🟡 `Show()` 的可用宽是常量 1920 ⇒ 21:9 下会「多缩」**（只报不改，简报明令别碰这一行）
- `MultiCardDisplay.cs:385` `availPx = LayoutSpace.DesignPxW − SideMarginPx * 2f` = **1800**（**同量纲、不是回退**，
  上一轮那个量纲缺陷的修法就是它，本件**没动**）。
- 而画布宽在 21:9 是 **2560**、面板本体也是 2560 宽 ⇒ 用 `max(MinShrink, 1800/total)` 会比「按面板宽 2440 算」**多缩**。
- ⚠️ 要改必须与 `Editor/BattleScene.cs:8149-8160` 的 `availPx = 1920f − 120f` **同批改**（那条是它的期望值）。
- ⛔ 本件不改：① 简报明令「该行只许是 `LayoutSpace.DesignPxW`」；② 我们这整套「缩」本来就是替代原版
  `ScrollRect` 的**自选机制**（原版不缩、只滚）⇒ 「阈值该跟画布宽还是跟 1920」是**口径问题、要主对话裁**，不是能自己拍的。

---

### 可直接粘进总表的一行（≤3 行）

> **T09·MultiCardDisplay 锚点核（A11，2026-10-16）**：判为**无需改** —— 画布档面板本体
> `MultiCardDisplay.cs:223-231` 已是 `anchor(0,0.5)-(1,0.5) + sizeDelta(0,818)`，与原版 `Generic Multi Card Display Combat`
> （arena2 RT_3370 / arena1 RT_3175，两场逐值同）**一致**；世界壳那个「两轴固定」是**有意**的（世界空间无 stretch 语义，`sizeDelta` 惰性）。
> ✅ 新增待办（本件只报不改）：**① `BattleContinueButton` 组原版是 `anchor(1,0) piv(1,0)` 右-下锚（判据 RT_3251）
> ⇒ 我们写成「按中心固定 px」，21:9 下偏 320 px；修它必须同批改 `HitContinue`(`:475-493`) + `BattleScene.cs:21157-21168`** ·
> ② 「继续」字位比原版右 ≈168 px、压到圆钮（原版 `Text` 框 x[1385.05,1672.41] + `H=4(Right)`） ·
> ③ `Show()` 的 `availPx` 是常量 1920，21:9 下会多缩（口径待裁，⛔ 与 `BattleScene.cs:8149` 成对）。
