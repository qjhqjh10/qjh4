# T09 批 3（`OverHand` 组）· `MultiCardDisplay` + `CardChoicePanel`/`ChoosePanel` 迁 uGUI

> 写手：并行子代理（**只有这三个文件的写权**）。日期 2026-10-16。
> 判据 = `资料/任务线/战斗线/任务/T09_整层换uGUI.md` §五·补⑨/⑩ · `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一
> 范例 = 批 2 的 `Battle/SettingsPanel.cs` · `Battle/BattleLogPanel.cs` · `Battle/ErrorMessageBanner.cs`。

---

## 1. 结论

- **三件都迁完了**：所有 `ImageQuad` / `Label` 建件口改成 uGUI 子画布上的 `HudPiece` / `Label`（`MakeText`），
  位置/尺寸一律**设计 px**（`HudCanvas.PosPx` / `MakeImageInParent`），`z` 语义照搬（值一个没改，改由 `HudCanvas.Add` 换算成兄弟序）。
  `typecheck` **0 错**（运行时 0 / 编辑器 0）。
- 🔴 **一处【有意】没迁、且必须如实上报**：两扇面板那层**整屏压暗**（`MultiCardDisplay.mcd_mask` / `CardChoicePanel._shade`）
  **留在世界空间**。**理由是硬的**（不是省事）：它必须画在**候选卡【后面】**，而候选卡是 `CardView`
  （`MeshRenderer`，`Default` 排序层、`renderQueue 3000`），**不归画布管**；我们的子画布在 **`OverHand`**（更高）
  ⇒ **画布上的任何件都画在卡片【上面】**（判据 = `T09` §五·补⑤ ⑤「给了 `sortingOrder` ⇒ 那一扇**盖住所有还没迁的件**」）。
  迁上去的后果是**具体可算的**：`mcd_mask` α0.7725 ⇒ 卡被压到 **23% 亮度**；`ChoiceShade` α0.55 ⇒ 卡压到 **45%亮度**
  —— 而这两个窗的用途**就是**看/挑那些卡。⇒ **在卡片自己迁上画布之前，这一件不能迁**（同批的 `CardDisplayWindow`
  —— 别人的文件 —— 是**同一处**结构，建议同步核一眼）。
- 因此本批的落地形态 = **「世界壳 + 画布本体」两个节点**（先例 = 批 2 的 `SettingsPanel`）：
  世界壳装**卡片 + 那层压暗**（两样都必须世界空间），画布本体装**图与字**。
- 附带的两条代价（**已如实记着**，都随「第 6 步：手牌/场上卡」一起消失）：
  ① **卡片不跟 `CanvasScaler` 缩放** ⇒ 非 16:9 下卡与画布那一组（卡下那颗钮、标题、底条）会**分家**（16:9 逐位重合）；
  ② 同一条导致**画布件与卡片的世界坐标差 0.75 倍**（只在非 16:9）—— 自检里**不受影响**（见 §4）。
- **没有任何一条现存断言因为它变红**（逐条核过，见 §4）；只有**建议新增**的两条 `box(...)`。

## 2. 改动清单（逐件分开写）

> ⚠️ **下表的行号 = 本报告落盘那一刻的现读值**（三个文件都是改完之后的行号）。文件再被改一句就会漂 ±1~2
> ⇒ **核对时请按符号名 grep**（`PanelRect` / `EnsureSubCanvas` / `MakeImageInParent` / `HitContinue` …），⛔ 别只按行号找。

### 2·A `Battle/MultiCardDisplay.cs`（**556 行** · 现读行号）

| 处 | 旧口 | 新口（行号现读） |
|---|---|---|
| 子画布 | 无 | `HudCanvas.EnsureSubCanvas(null, "Generic Multi Card Display Combat Canvas", SortingLayer.OverHand, 0)` —— `:205-215`（含 `SubCanvas` 幂等取用；建不起来则**出声 + 本窗不建**，⛔ 不退回世界空间） |
| 画布本体 | 无 | 新节点 `Generic Multi Card Display Combat`（= 原版 GO 名）· **锚点逐字照原版**：`anchorMin (0,0.5)` / `anchorMax (1,0.5)` / `pivot (0.5,0.5)` / `anchoredPosition 0` / `sizeDelta (0, 818)` + `Add(ZBody)` —— `:216-232`（⛔ 别退回「重合锚 + 写死 1920×818」—— 那在 21:9 下会窄 640 px；也别用 `FillParent` —— 高会变成画布高） |
| 世界壳（**保留**） | `MenuDraw.SetPxSize(go, 1920, 818)` | **一字未改** —— `:198`（它现在只装卡片与压暗） |
| 整屏压暗 | `ImageQuad.Create(_root, …)` | **保留**（**有意**，见 §1）—— `:249-257` |
| 标题 | `Label.Create(_root, "", Pos(960, 162.6, ZContent), …)` | `HudCanvas.MakeText(_panel, "", tp.x, tp.y, …)` —— `:272-275`（`tp = PosPx(960, BandTopPx+HeaderH/2)`）；`SetScriptHeight(…, 38f, **1f**)`（旧 = `EndPanel.PxPerUnit`=108） |
| Continue 条 | `ImageQuad.Create(_root, tex, Pos(BarCx,BarCy,ZContent), U(BarH), …)` + `SetAspect(577.5/63.84)` | `HudCanvas.MakeImageInParent(_panel, tex, "mcd_bar", BarCx−960, BarCy−540, **577.5, 63.84**, ZContent)` —— `:303-308`（**宽高各给一个数** ⇒ `SetAspect` 那一步不再需要，落进 `sizeDelta` 的正是原版 `m_SizeDelta`） |
| 圆钮 | `…U(CircleH)…` | `MakeImageInParent(…, **80.47, 79.64**, …)` —— `:310-315`（旧那套靠 `SetAspect(80.47/79.64)` 得到同一对数；**顺带**：本件现在终于**逐位等于**原版 rect） |
| 「继续」字 | `Label.Create(_root, continueText, Pos(BarCx+BarW/2−60, BarCy, ZContent), …)` | `MakeText(_panel, …)` —— `:322-326`；`SetScriptHeight(…, 38f, 1f)` |
| 底部提示 | `Label.Create(_root, "点「继续」…", Pos(960, 983, ZContent), 2, …)` | `MakeText(_panel, …)` —— `:333-336` |
| 卡片 | `CardView.Create(_root, …)` + `SetPose(Pos(...))` | ⛔ **一字未改**（卡是**世界物件**，父 = 世界壳）—— `Show()` `:399-410` · `Pos()` `:540` |
| **删掉**的私有助手 | `static float U(px) = px/108` | **已删**（本件只剩 `_mask` 一件在世界空间、而它不开 px）—— 原位留了一段「为什么删 + 兄弟件的 `U` 别跟着删」的注释 `:542-548` |
| 读回口 | `ContinueBarDrawnPx = (WorldW, WorldH) × 108` | `HudCanvas.SizePx(_bar.Rect)`（**= `sizeDelta`**，设计 px）—— `:513`；圆钮同 —— `:525`（口径 = `T09` §七·8「读回一律读 `sizeDelta`」） |
| 命中 | `px = world.x×108+960` / `py = 540−world.y×108` | **改成用条自己的局部系反算**：`_bar.Rect.InverseTransformPoint(world)` → `px = BarCx+l.x`、`py = BarCy−l.y` —— `HitContinue` `:466-487`；取不到条时**退回老式子**（保持旧行为） |
| 自检口 | 无 | 新增 `public RectTransform PanelRect`（画布本体）—— `:178` |

### 2·B `Battle/CardChoicePanel.cs`（**621 行** · 现读行号）—— 基类，`ChoosePanel` 继承它

| 处 | 旧口 | 新口（行号现读） |
|---|---|---|
| 子画布 | 无 | `EnsureSubCanvas(null, "ChooseCardMenu Canvas", OverHand, 0)` —— `:219-228`（建不起来 ⇒ 出声 + **图与字这一批不建**；卡片照旧、`HandleClick` 仍能选） |
| 画布本体 | 无 | 新节点 `ChooseCardMenu`（= 原版 GO 名）· **锚点逐字照原版**：`anchor (0,0)-(1,1)` + `sizeDelta (0,0)` = `HudCanvas.FillParent`（整屏/铺满画布）+ `Add(ZBody)` —— `:229-239` |
| 整屏压暗 | `ImageQuad.Create(root, Solid, At(960,540), U(1200), …)` | **保留**（**有意**，同 §2·A）—— `:245-255` |
| 标题 | `Label.Create(root, …, At(TitleCx,TitleCy), 8, …)` + `SetGlyphHeight(U(TitlePx))` | `MakeText(_panel, …, tp.x, tp.y, 8, …)` —— `:268-273` + `SetGlyphHeight(**lo.TitlePx**)`（不再 `U(...)`） |
| 底条 | `ImageQuad.Create(…, U(BarH))` + `SetAspect(577.5/63.84)` | `MakeImg(_panel, …)` = `MakeImageInParent(…, **577.5, 63.84**, Z)` —— `:278-279`（助手 `:304-310`） |
| 圆钮 | `ImageQuad.Create(…, U(PlayH))`（宽 = 高×贴图比例） | `ImgByHeight(_panel, 40k_UI_bt_play, …, **79.64**, Z)` —— `:281-282`（助手 `:315-322`；**口径照旧** = 按高定宽） |
| 确认字 | `Label.Create(…, At(ConfirmCx,ConfirmCy), 6, …)` + `SetGlyphHeight(U(33.7))` | `MakeText(_panel, …)` —— `:284-288` + `SetGlyphHeight(**33.7f**)` |
| 眼睛 | `ImageQuad.Create(root, 40k_UI_bt_eye, At(EyeCx,EyeCy), U(EyePx), …)` | `ImgByHeight(_panel, …)` —— `:290-291` |
| 每张卡的钮 + 它的字 | `ImageQuad.Create(transform, UI_Button_Mulligan, pos, U(77), …)` / `Label.Create(transform, …, pos, 5, …)` + `SetScriptHeight(text, 28, **108**)` | `ImgByHeight(_panel, …, **77**, Z−0.02)` / `MakeText(_panel, …, cxPx−960, btnCyPx−540, 5, …)` + `SetScriptHeight(text, 28, **1**)` —— `LayOutCards` `:375-428` |
| 卡行布局 | 一套世界单位（`U()` + `At()`） | **两套并存**：卡片那一支**一字未改**（世界单位）· 装饰那一支**直接算 px**（`SpacingPx(n)` 与 `Lo.CardRowCy`）⇒ 16:9 逐位重合 —— `:384-392` |
| **删掉**的一段 | 建完 `foreach (…) localPosition.z += Z` 逐件拨 z | 那个 z 现在**直接当建件口的 `z` 形参**传给 `HudCanvas.Add`（兄弟序、同向）⇒ 相对先后逐件不变 —— 原位留了注释 `:261-264` |
| `DoneWorldPos` / `EyeWorldPos` | `At(ConfirmCx,ConfirmCy) + (0,0,Z)`（按常量算的世界点） | **改读实件**（`_confirm.transform.position` → `_bar` → 老常量式 逐级兜底）—— `:516-535`（理由：画布两轴系数可不同 ⇒ 常量算出来的点**不再落在画出来的矩形里**） |
| 自检口 | 无 | 新增 `public RectTransform PanelRect` —— `:150` |

### 2·C `Battle/ChoosePanel.cs`（**178 行**）

- `Create` 里那个节点**保留世界空间写法**（`MenuDraw.SetPxSize(go, 1920, 1080)`）—— 它现在是**世界壳**（装候选卡 + 压暗层），
  `:100-121` 逐条写明「⛔ 别换成 `HudCanvas.SetPxSize`」（那会把 1920 当设计 px = 世界单位的 1/108，而卡那一支按世界单位算）。
- 文件头补了 4 行指路（原版宿主 / 世界壳 vs 画布本体 / 判据在哪）。**几何代码零改动**（本文件本来就没有建件口）。

### 2·D 同批四件

| # | 要求 | 结果 |
|---|---|---|
| ① | 摘注册表 | **无此账** —— `BattleDriver.ReanchorHud()`（`:16178-16224` 现读）只碰 `_hudLabels/_hudImages` 两族 + `FitDeckPile`/`PlaceDeckLights`/`PlaceHandPlate`/棋盘 marker/`backdrop`/`selector`/`skillPanel`/`_replayBar`/`_unitChat` —— **`_multiCards` 与 `_choosePanel` 一个都不在里面**；`_hudImages`/`_hudImageSpots`/`_hudLabels`/`_hudSpots` 四个注册表也从未收过本批的件（它们只收 `HudAbs/HudAbsTex/HudImage/HudImageTex` 那一族） |
| ② | 读回口改读 `RectTransform`/`HudPiece`/`sizeDelta` | ✅ 四处（见 §2·A 表末两行 + `DoneWorldPos`/`EyeWorldPos`） |
| ③ | `RefreshLayout` 里对本批的操作 | **无此账** —— 同 ①（`ReanchorHud` 里那 4 个 `RefreshLayout` 调用是 `selector`/`skillPanel`/`_replayBar`/`_unitChat`） |
| ④ | 报告里列「哪几条断言要改成什么」 | ✅ §4 |

## 3. 父链对照（我们 vs 原版）

| | 原版 | 我们（本批之后） |
|---|---|---|
| `MultiCardDisplay` | `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas/Generic Multi Card Display Combat`（自身**无 `Canvas` 组件**，随 `FrontCanvas` = `OverHand`/0；兄弟序 **4/14**；`anchor (0,0.5)-(1,0.5)` + `sizeDelta (0, 818.04)`）<br>子树：`Header Text` · `Viewport/Content/CardUI Reference` · `BattleContinueButton` ·（`Menu Dark Background`）—— **卡片是自己子孙**、全在 uGUI 里 | `HudRoot/MultiCardDisplay`（**世界壳**，1920×818 世界单位）→ `mcd_mask`(世界 quad) + `mcd_card0..N`(**`CardView` 世界物件**)<br>＋ `BattleHud Canvas/<Generic Multi Card Display Combat Canvas>`(`OverHand`/0, `overrideSorting`) → `Generic Multi Card Display Combat`（**锚点逐字复刻原版**：横向 stretch × 818 设计 px）→ `mcd_header`/`mcd_bar`/`mcd_circle`/`mcd_continue`/`mcd_hint` |
| `ChoosePanel` | `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas/ChooseCardMenu`（同上；兄弟序 **5/14**；`anchor (0,0)-(1,1)` + `sizeDelta (0,0)`）<br>子树：`ChooseCardMenuAnchor`（**只有 `Transform`**）· `ButtonsGroup` · `ChooseText` | `HudRoot/ChoosePanel`（**世界壳**，1920×1080 世界单位）→ `ChoiceShade`(世界 quad) + 候选卡（**`BattleDriver` 建在它下面**）<br>＋ `BattleHud Canvas/<ChooseCardMenu Canvas>`(`OverHand`/0) → `ChooseCardMenu`（`FillParent` = 原版那对锚点/`sizeDelta`）→ `ChoiceTitle`/`ChoiceBar`/`ChoiceCircle`/`ChoiceConfirmText`/`ChoiceEye`/`ChoiceCardBtn_i`/`ChoiceCardBtnText_i` |
| `CardChoicePanel` | **原版无此对象**（我们合成的共用壳）—— 它服务的原版对象 = `ChooseCardMenu`（`T09c` §一那一行） ⇒ 子画布的层/order 照 `ChooseCardMenu` 取，**不是**另立一格 | 同 `ChoosePanel`（基类里建，见 §2·B） |

**三处如实记的差异**（⛔ 都不在本批白名单内、没顺手改）：
1. **「世界壳 + 画布本体」这一层是【我们自己的】**（原版这两个面板各自就是**一整棵 uGUI 子树**）—— 起因 = 卡片是世界物件。
2. **卡片不进画布**（原版 `CardUI` 在 `Viewport/Content` 下）—— `T09` §五·补⑧ 那条边界（第 6 步）。
3. **原版随父 `FrontCanvas` 渲染**，我们建的是**同层同序 + `overrideSorting`** 的子画布（口径 = `T09` §五·补⑨ 一·2）。

## 4. 要改的断言（逐条 · `Editor/BattleScene.cs`，**本批没动那个文件**）

**结论：现存断言【一条都不用改】才是对的** —— 逐条核过下面四处，给的是**建议新增/改注释**。

| 位置（现读） | 断言 | 现在会怎样 | 建议 |
|---|---|---|---|
| `:16532` | `box(ChoosePanel.Create(...).transform, "选牌面板根", 1920f, 1080f, …)` | **绿**（那是**世界壳**：`box` 的非画布那一支，`lossyScale = 1`、`rect = LayoutSpace.Px(1920/1080)` ✓） | ① 标签改成「**世界壳**（装候选卡 + 压暗）」② **新增一条**：`box(p.PanelRect, "选牌面板画布本体 `ChooseCardMenu`", a218CvW, a218CvH, "原版 `ChooseCardMenu`（`anchor(0,0)-(1,1)` + `sizeDelta (0,0)` ⇒ 整屏）；我们**逐字**用 `FillParent` 表达 ⇒ 量的是**画布 rect**")` —— **期望值要用 `a218CvW/a218CvH`**（同 `EndPanel`/`BattleLogPanel` 那两条；⛔ 别写 1920×1080） |
| `:16546` | `box(MultiCardDisplay.Create(...).transform, "多卡展示窗根", 1920f, 818f, …)` | **绿**（同上，世界壳，`MenuDraw.SetPxSize` 一字未改） | 同上：**新增** `box(mcd.PanelRect, "多卡展示窗画布本体 `Generic Multi Card Display Combat`", a218CvW, 818f, "原版那一级 = `anchor (0,0.5)-(1,0.5)` + `sizeDelta (0, 818.04)` ⇒ **横向铺满画布** × 818.04 高")` —— 宽用 `a218CvW`（我们**逐字复刻了横向 stretch**；16:9 下它就等于 1920） |
| `:21114-21190`（`A964/E2④`） | `ContinueBarWorldPos` / `ContinueBarDrawnPx` / `ContinueCircleWorldPos` / `ContinueCircleDrawnPx` + `HitContinue` 的 4 条（内侧中 / 外侧不中 / `offL` / `offT`） | **绿**（**逐值验算过**，见下） | **不用改**。⚠️ 但 `db964.y` 的**打印值**在其它宽高比下会变（旧 = `WorldH×108`，新 = `sizeDelta.y`）；16:9 与编辑器档下**两者都是 63.84**，所以连打印值都不变 |
| `:8130-8160` · `:15801-16135` | 多卡窗 / 选牌面板那一整族（`Visible`/`HeaderShown`/`CardCount`/`ContentWidthPx`/`CardCypx`/标题与按钮文案/`SimulateChoosePick·Done`） | **绿** | 不用改（全是行为/文案断言；`SimulateChoose*` 走的是**卡自己的 `Contains`**（世界）、不受画布影响） |

**`E2④` 那 4 条为什么仍是绿的**（逐值推的，三种档全过）：
`HitContinue` 现在用**条自己的局部系**把世界点折回 px（`RectTransform.InverseTransformPoint`）⇒ 与「画出来的那个矩形」**永远同一个坐标**。
断言把 px 偏移折成世界偏移用的是常数 108，所以进到 `HitContinue` 里时**两个轴各按所在档的系数还原**
（16:9 = 108/108 · 真 4:3 = 144/144 · **编辑器批处理 = x108 / y144**）：
`offT`（上沿外 20 px）⇒ 落到留白带内（最紧的一档是编辑器那档：`py = 907.9` vs 下沿 `905.2`，**余量 2.7 px**）✓；
`offL`（左沿外 20 px，横向**无**外扩）⇒ 始终在外 ✓；内侧两点与「圆钮中心」⇒ 始终在内 ✓；
`outM` 的纵向探针 = 1.5 倍实绘高 ⇒ 三档都在命中区外（944.9 / 881.3 / 849.4 vs 上界 905.2）✓。

⚠️ **一处口径要请主对话留意**（不是断言改不改的问题）：`ContinueBarDrawnPx` 现在是**设计 px**（= 原版 `m_SizeDelta`），
**不再是「屏幕上量到的 px」** —— 与批 2 的 `log.FrameSizePx` 同一口径（`T09` §七·8）。若哪天要断「非 16:9 下实绘多少屏 px」，得另开一个口。

## 5. 类型检查

```
TMPDIR=/tmp/wf_mcd bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 --- 运行时错误数: 0
--- 编辑器程序集 --- 编辑器错误数: 0
```
（中途一次跑出来的 9 条错全在 `CardDisplayWindow.cs` / `MulliganPanel.cs` —— **别人的文件、当时正在写**；重跑即归零，⛔ 没去动它们。）
`git diff --numstat`：`MultiCardDisplay.cs 199/50` · `CardChoicePanel.cs 235/42` · `ChoosePanel.cs 24/2`
（行数 556 / 621 / 178；⛔ 没有一个文件被整篇重写）；
三个文件改完仍是**纯 LF**（改前 = 0 CRLF/纯 LF，现读同）✓ 不是整篇重写。

## 6. 顺手发现的（**只报，没改** —— 红线：不许顺手改）

1. 🔴 **`MultiCardDisplay.Show` 里 `availPx` 把【世界单位】当 px 用** —— `MultiCardDisplay.cs:378`：
   `float availPx = LayoutSpace.VisibleWidth - SideMarginPx * 2f;`
   `LayoutSpace.VisibleWidth` 是**世界单位**（16:9 = 17.78，见 `Core/LayoutSpace.cs:111-119`），而 `SideMarginPx` = 60 **px**
   ⇒ 16:9 下 `availPx = 17.78 − 120 = **−102.2**`（负数！）⇒ `totalPx > availPx` **恒真**、`k = Mathf.Max(0.45f, 负数/totalPx)`
   **恒等于 0.45** ⇒ **多卡窗里每张卡永远被缩到基准的 45%**（卡高 520.3 → **234.1 px**），**与张数无关**。
   正解应是 `LayoutSpace.DesignPxW - SideMarginPx*2f`（= 1800）。为什么一直没人发现：`:8151` 那条断言写的是
   `… || mcd.UsedScale > 0f`（**第三支恒真**）⇒ 它盖住了这个洞（本文件 2026-10-15 刚给 `CardHeightPx` 订正过一次同类量纲错，
   这一处是**另一个**）。**未改**（改它会动实绘尺寸、要有配套断言）。
2. ⚠️ **同一个原版素材，两个面板画出来不一样宽**：`40k_UI_bt_play` 原版 rect = **80.47 × 79.64**；
   `MultiCardDisplay` 现在**逐位给了 80.47**（本批改的），而 `CardChoicePanel` 仍走「按高定宽 + 贴图比例」
   ⇒ 实绘 **79.64 正方**、**窄 0.83 px（1%）**（既有偏离，不是本批引入的；贴图本身 128×128）。
   要收口就把 `ImgByHeight` 那一处换成显式 `(80.47, 79.64)`（同批 `MulliganPanel` 也有一份同款）。
3. ⚠️ **`CardChoicePanel` 的 `_cardBtns` 与 `_cards` 会静默错位**（潜在）：`LayOutCards` 里是
   `if (q != null) _cardBtns.Add(q);`（`:410`）而 `_cardTexts` **无条件 Add** ⇒ 只要某一张的贴图取不到，
   两个列表的下标就与 `_cards` 错位（`ApplyPicks`/`ButtonIndexAt`/`SetVisible` 全按同一下标索引）。
   今天四个贴图都在（`CardPresentation/Art/原版/去重资源/` 现读）⇒ 不触发；**已记着，未改**。
4. ⚠️ **两扇面板的「继续条 / 底条 / 卡上那颗钮」都建成了 `raycastTarget = false`**（`MakeImageInParent` 默认装饰件），
   而原版那几颗是 `m_RaycastTarget = 1`（判据 → `MultiCardDisplay.HitContinue` 的注释）⇒ **第 4 步（迁按钮族 + 退两沿）**
   时这几处要一起翻成 `true`，否则 uGUI 的点击永远收不到。今天没有 `EventSystem`（有意不建），所以现在无所谓。
5. ⚠️ **A218 探针的泄漏多两处**（`T09` §七·12 那条已记的老账）：本批的两扇各自多建
   **1 块子画布 + 1 个画布本体节点**，而画布上的节点**不随 `a218root` 销毁** ⇒ 探针每跑一次多留 4 个节点。**未改**。

## 7. 没查清 / 没做的

1. 🔴 **那两层整屏压暗仍留世界空间**（§1）—— 这是本批**唯一**没满足「所有图与字都上画布」的地方。
   判据与后果都算清了，但**要不要为它单独破例**请主对话定（备选方案：给压暗单独建一块**排序层 = `Default` +
   负 `sortingOrder`** 的子画布 —— 那能同时押在卡片**后面**、又在 `OverHand` 画布**前面**；
   **我没敢试**：`T09` §五·补⑨ 一·3 明写「子 canvas 靠在父平面哪个 z 由它自己的 `localPosition.z` 决定 ——
   这一条主对话还没读原版的对应值，⛔ 别猜」）。
2. **卡片没迁**（不在本批白名单/口径内）：卡仍走世界单位、`CardView` 仍 `MeshRenderer`
   ⇒ 非 16:9 下卡与画布那一组会分家（§1 代价①）。**这一条决定了 §7·1 什么时候能收口。**
3. **`UsedScale`/横向滚动**：原版 `Generic Multi Card Display Combat` 根上带 `ScrollRect`（横向滚动）+
   `RectMask2D`，我们仍是「装不下就等比缩」。本批**没做**（要动，前提是卡片先上画布）。
4. ⚠️ **`CardDisplayWindow`（同批、别人的文件）有【同一处】结构**（整屏 `Menu Dark Window` 遮罩 + `CardView`），
   请主对话核一眼它有没有把遮罩迁上画布 —— 若迁了，那个窗的卡会被压暗到 22.75%。
5. **没跑自检、没起 player**（现行口径）；`E2④` 那 4 条是**手推验算**的绿，不是跑出来的绿。
