# T09 批 3 · `MulliganPanel` 迁 uGUI 子画布 —— 执行报告

> **写手**：批 3（`OverHand` 组）· **唯一改过的文件** = `Unity/MyGame/Assets/CardPresentation/Battle/MulliganPanel.cs`
> **日期**：2026-10-16 · **判据**：`T09` §五·补③/④/⑨/⑩ · `T09c` §一（`Mulligan` 那一行）·
> **范例** = 批 2 五扇（`SettingsPanel` / `BattleLogPanel` / `EndPanel`+`BattleDoors` / `ErrorMessageBanner`）
> ⛔ 本文件是**唯一**写出去的产出（代码之外）；没有跑任何 `*.Run`、没有起 player、没动 git。

---

## 1. 结论

**做完了**：换牌窗里**全部图与字**都建在自己的 uGUI 子画布上，`typecheck` 0 错。

- **子画布** = `Mulligan Canvas`（`OverHand` / order 0 / `overrideSorting`，`EnsureSubCanvas` 按名字幂等）。
  层/order **照 `T09c` §一 我那一行**（原版 `Mulligan` @ `…/FrontCanvas/Safe area FrontCanvas`）。
- **画布上那个整屏帧** = `Mulligan`（`HudCanvas.FillParent` = 原版 `anchor (0,0)-(1,1)` + `sd (0,0)`）——
  本面板**所有件**的父节点就是它。**它的中心 = 屏心、局部单位 = 设计 px** ⇒ 原有那批 `cx/cy` 常量**原样可搬**。
- 逐件：**压暗 · 底条 · 圆钮 · 眼睛 · 提示行 · 先后手行 · 完成钮的字 · 每张牌那颗「换」钮 + 它的字** —— 9 类全在画布上
  （`OnCanvas` 这个新口可以一次断完：图那 4 件 `HudPiece.IsUgui` + 字那 3 个 `Label.IsUgui`）。
- 🔴 **命中与读回同批换了帧**（这一步是**必须**的，见 §6·1）：`HitDone` 与 `Bar/Done/EyeWorldPos`
  从「世界空间那一帧」（`LayoutSpace.ToDesignPixel` / `At`）换成**画布帧**（`PxOf` / `WorldOf`，
  = `_frame.InverseTransformPoint/TransformPoint`）。**16:9 下两者逐位同值**（差 float 舍入）。
- 🔴 **有意留在世界空间的两样**（写进文件头，⛔ 不是漏迁）：
  ① 世界空间那层**壳**（`MulliganPanel` 自己那个 `RectTransform`）= 「换牌区那一行」的换算基准
     （`RowLocalAt`/`RowWorldPosAt`）—— 那几张**起手牌还在世界空间**（`T09` 第 5 步才走 `Physics2D`），
     而 `BattleDriver:13315` 正是拿这几个口给卡定位 ⇒ 保它 ⇒ **卡的世界位逐位不变**；
  ② 卡体本身（`CardView`）—— 同上，属于第 5 步。这一条与 `BattleLogPanel` 把根节点留世界**同一类处置**。
- ⚠️ **代价**：`Editor/BattleScene.cs` 里 **4 条断言会红**（都是**尺变了**、不是行为坏了），
  逐条替换代码在 §4。另有 **2 处要主对话裁**（子画布的兄弟序 / 深度）在 §6。

---

## 2. 改动清单（旧口 → 新口 + 行号，**现读**）

行号一律指 `Battle/MulliganPanel.cs`（本次改完的现读）。

| # | 旧口（世界空间 quad / 世界单位） | 新口（uGUI 子画布 / 设计 px） | 行号 |
|---|---|---|---|
| 1 | `MenuDraw.SetPxSize(root, 1920, 1080)`（**面板帧**那一份） | **`HudCanvas.FillParent(_frame)`** | `:620` |
| — | ⛔ 壳那一份 `MenuDraw.SetPxSize(go.transform, …)` | **保留不动**（卡那一行的基准；`A218` 期望值不用改） | `:592` |
| 2 | `ImageQuad.Create(go.t, CardArt.Solid(), At(960,540), U(1200), …, "MulliganShade")` + `SetAspect` + `SetTint` 三连 | `HudCanvas.MakeImagePiece(frame, …, "MulliganShade"…)` → `FillParent` → **`Reorder(Z)`** | `:629-636` |
| 3 | `Label.Create(go.t, promptText, At(967,106.5), 8, …)` | `HudCanvas.MakeText(frame, …, Px(967,106.5).x/.y, 8, …, z = Z−0.05)` | `:672` |
| 4 | `SetAutoFitBox(U(1307.064), U(69.44), …)` | `SetAutoFitBox(1307.064, 69.44, …)`（**去掉 `U()`**） | `:684` |
| 5 | `Label.Create(go.t, "", At(966.03,156.5), 7, …)` | `HudCanvas.MakeText(frame, …, …)` | `:696` |
| 6 | `SetAutoFitBox(U(TurnW), U(TurnH), 18, 55, TurnPx)` | `SetAutoFitBox(TurnW, TurnH, 18, 55, TurnPx)` | `:714` |
| 7 | `ImageQuad.Create(… "40k_bt_underbutton", At(BarCx,BarCy), U(BarH), …)` + `SetAspect(BarW/BarH)` | `Img(frame, …, BarCx, BarCy, BarH, Z)` + `SetAspect`（逐字同义） | `:719` / `:727` |
| 8 | `ImageQuad.Create(… "40k_UI_bt_play", At(PlayCx,PlayCy), U(PlayH), …)` | `Img(frame, …, PlayCx, PlayCy, PlayH, Z)` | `:728` |
| 9 | `Label.Create(go.t, DoneLabel, At(DoneCx,DoneCy), 6, …)` | `HudCanvas.MakeText(frame, …, z = Z−0.05)` | `:731` |
| 10 | `SetAutoFitBox(U(DoneBoxW), U(DoneBoxH), …)` | 去 `U()` | `:740` |
| 11 | `ImageQuad.Create(… "40k_ui_bt_eye"…)` | `Img(frame, …, EyeCx, EyeCy, EyeH, Z)` | `:743` |
| 12 | 五句 `localPosition += (0,0,Z / Z−0.05)` → 逐块 | **删** —— 层深度改由建件口的 `z` 形参承担（画布上 z 只用来排兄弟序，写进 `localPosition` 会把件推离平面） | `:745` 附近（删处） |
| 13 | 每张牌的钮：`ImageQuad.Create(transform, "UI_Button_Mulligan", RowLocalAt+…U(btnH))` | `Img(_frame, …, RowCx+RowDX(i,n), RowCy+halfCardPx+BtnBelowCardPx, btnH, Z)` | `:856-859` |
| 14 | 每张牌的字：`Label.Create(transform, …)` + `lP.z += Z−0.05` | `HudCanvas.MakeText(_frame, …, z = Z−0.05)` | `:886` |
| 15 | `Label.SetScriptHeight(text, px, **108f**)` | `…,**1f**`（uGUI 档「1 局部单位 = 1 px」；⛔ 传 108 ⇒ 字号小 108 倍、**不报错**） | `:212` |
| 16 | 命中：`LayoutSpace.ToDesignPixel(world)` | **`PxOf(world)`**（= `_frame.InverseTransformPoint` + 回左上原点；`_frame == null` ⇒ 不认） | `:967-982` |
| 17 | `BarWorldPos/DoneWorldPos/EyeWorldPos = At(…)+(0,0,Z)` | **`WorldOf(cx,cy)`**（= `_frame.TransformPoint(Px(cx,cy))`） | `:985` / `:1004` / `:1007` |
| 18 | `CardButtonLocalZ` = `transform.localPosition.z` | **`HudPiece.OrderZ`**（兄弟序账上的 z 档；画布上 `localPosition.z` 恒 0 ⇒ 旧读法**恒绿、无判别力**） | `:1120` |
| 19 | 字段类型 `ImageQuad` → **`HudPiece`** | `_shade` / `_bar` / `_play` / `_eye` / `List<HudPiece> _cardBtns` | `:246-247` / `:254` |
| 20 | （新）换牌区行偏移 | **`RowDX(i,n)`**（设计 px，**只此一份** —— 卡那条过一遍 `U()`、钮那条直接用） | `:390` / `:526` |
| 21 | （新）画布帧的两个节点 | `_frame`（`Mulligan`）/ `_cv`，自检口 `FrameRect` / `CanvasRect` / `PxOf` / `BarSizePx` / `OnCanvas` | `:276-280` / `:385` / `:396-443` / `:1147` / `:1157` |

> 说明：`U(px) = px/108f` **保留**（只剩 `RowLocalAt` 一处用，喂**卡**那种世界位）；
> 画布那一侧**一律直接用 px**（`T09` §五·补⑩ 那张配方表）。

---

## 3. 父链对照（我们 vs 原版）

| | 我们（迁完） | 原版（`T09c` §一 我那一行 / `bundle_scenes_scenes_battlearena2`） | 差异 |
|---|---|---|---|
| 排序组的宿主 | `BattleHud Canvas`(根) → **`Mulligan Canvas`**（子画布 · `overrideSorting` · `OverHand`/0） | `BattleHud/Canvas` → **`FrontCanvas`**（Canvas · `overrideSorting` · `OverHand`/0）→ `Safe area FrontCanvas`（**纯版式壳**） | 🔴 我们**少一级** `Safe area FrontCanvas` · 🔴 命名按「原版**节点名** + ` Canvas`」惯例（先例 = 主对话自己在 `BattleDriver:14429/15239` 写的 `AboveShader Canvas`）—— 见 §6·2 |
| 面板帧 | `Mulligan Canvas` → **`Mulligan`**（`FillParent`） | `…/Safe area FrontCanvas` → **`Mulligan`**（`anchor (0,0)-(1,1)` + `sizeDelta (0,0)`） | ✅ **逐字段一致**（`FillParent` 就是那一对值；批 2 同口径） |
| 面板帧的子件 | 平铺 **8 类**：`MulliganShade`(我们自加) · `MulliganPrompt` · `MulliganTurnText` · `MulliganContinueBar` · `MulliganContinueCircle` · `MulliganDoneText` · `MulliganHideButton` · `MulliganBtn_*`（每张牌一颗）+ `MulliganBtnText_*` | `Mulligan` → `MulliganAnchor`(**只有 `Transform`**) · `ButtonsGroup`(→ `MulliganContinueButton`[底/字/圆钮] · `HideMulliganButton`) · `MulliganText`(→ `Text` · `TurnText`) | 🔴 **三处结构差异（都不是本件引入的，如实记、本次不翻）**：① 我们没有 `MulliganAnchor` / `ButtonsGroup` / `MulliganText` 这三层**容器**（相机是平铺的，几何常量全是**绝对屏 px**）；② 原版「完成钮」的底/圆钮/字是 **`MulliganContinueButton` 的三个子件**，我们是三颗平铺件（命中走 `MulliganPanel.HitDone` 的**框**，不靠父子）；③ 那颗「换」钮原版是**每张卡运行时挂**的 `CardMulliganButtonFrame`（挂在**卡**下），我们挂在**面板帧**下 |
| 壳（我们自加） | `HudRoot`（= 原版 `BattleHud` 那一级） → `MulliganPanel`（世界空间 · **零渲染件**） | **原版没有** | 有意保留：换牌区那一行的世界位换算基准（§1） |
| 深度 | 子画布 `localPosition.z = 0`（= 根 canvas 平面 = `planeDistance 20.3` ⇒ 世界 z 0.3） | 原版那一级 **`T09b` 没读到** | ⏸ **没猜**（§6·3，与批 2 五扇同档） |

---

## 4. 要改的断言（逐条；全在 `Editor/BattleScene.cs`，⛔ 我一个字没动）

### A. `:15548-15551` 提示行「渲染尺寸 ≤ 框」
- 现读：`mp.PromptLabel.WorldW <= LayoutSpace.Px(1307.064f) + 1e-4f && mp.PromptLabel.WorldH <= LayoutSpace.Px(69.44f) + 1e-4f`
- **病**：`Label.WorldW/H` 在 uGUI 档**就是设计 px**（TMP `textBounds`，`T09` §七·8 末那条自己写的），
  而右边 `LayoutSpace.Px(…,)=12.10 / 0.64` ⇒ **恒假**（前提：`_prompt` 非 null）。
- **改成**：`mp.PromptLabel.WorldW <= 1307.064f + 1e-4f && mp.PromptLabel.WorldH <= 69.44f + 1e-4f`
  （打印串里那两个 `LayoutSpace.Px(...)` 一起改）。

### B. `:15553-15556` 「继续」那颗的字「渲染尺寸 ≤ 框」
- 同上：`LayoutSpace.Px(368.929f) → 368.929f` · `LayoutSpace.Px(62.242f) → 62.242f`。

### C. `:15597-15604` 换牌那行的**位置**
- 现读：`var tw = mp.TurnLabel.transform.position; float tcx = LayoutSpace.PxX(tw.x), tcy = LayoutSpace.PxY(tw.y);` 比 `(966.03, 156.5)`
- **病**：`PxX/PxY` 是**世界帧**的逆（y 恒 108 px/世界单位），而画布帧的 y 系数**随屏变**
  （批处理下 **1/144**）⇒ 量出来是 `(966.4, **252.4**)` ⇒ **红**。
- **改成**：`var t = mp.PxOf(mp.TurnLabel.transform.position);` 再比 `(966.03, 156.5)`（容差 1.5 不用动；
  新读法在 16:9 下就是精确值）。`PxOf` = 本次新加的口。

### D. `:15610-15613` 底条渲染尺寸
- 现读：`Mathf.Abs(mp.BarWorldW * 108f - 577.5f) < 1.5f && Mathf.Abs(mp.BarWorldH * 108f - 63.84f) < 1.5f`
- **病**：`HudPiece.WorldW/H` 走 `GetWorldCorners()` ⇒ **世界单位**，而两轴系数还能不同
  （`T09` §七·8 实测）⇒ 批处理下 `BarWorldH*108` 读出 **47.9**（真值 63.84）⇒ **红**（x 那一半仍绿）。
- **改成**：`var bs = mp.BarSizePx;` → `Mathf.Abs(bs.x - 577.5f) < 1.5f && Mathf.Abs(bs.y - 63.84f) < 1.5f`
  （`BarSizePx` = 新口，读 `sizeDelta`，**与建件同一把尺**）。打印串里的 `*108f` 一起去掉。

### E. `:15621-15674` §A1301③「完成换牌」命中框那一整块（4 条 `Check`）
- 现读：`perPxX/perPxY = LayoutSpace.FromPixel(…) − LayoutSpace.FromPixel(…)`（**世界帧**的 1 设计 px 向量），
  再拿它乘 `71.92` / `288.75` / `86.4` / `119.75` / `278.75` / `61.92` 去探 `mp.HitDone`。
- **病**：`HitDone` 与 `mp.BarWorldPos` 现在**都在画布帧**，而 perPx 还是世界帧的
  ⇒ 批处理/4:3 下 `1 世界帧 px = 1.333 画布 px` ⇒ ① `inM`（内 1 px 必中）**红**；
  ② `corner` 那条判别式的 y = 61.92×1.333 = **82.6 > 71.92** ⇒ **红**。
  （`outM` 与 `oldCircle` 两条仍绿。）
- **改成**（只换那两行，其余**一个字不用改**）：
  ```csharp
  Vector3 perPxX = mp.FrameRect.TransformVector(1f, 0f, 0f);      // 1 设计 px 的世界向量（画布帧）
  Vector3 perPxY = mp.FrameRect.TransformVector(0f, -1f, 0f);     // 屏幕向下 = −y（画布帧 y 向上）
  ```
  我按**画布帧**重算过四条：`inM` ✓ · `outM` ✓ · `oldCircle` 距框心 (−86.4, −119.75) px（|y|>71.92）**必不中** ✓ ·
  `corner` (278.75, 61.92) px 落在 288.75 × 71.92 内 **必中** ✓ —— **判别力与灭自证一个字不减**。

### F. `:15688-15707` §A276（那颗钮那一层的 z）—— **不用改，仍绿**
- `CardButtonLocalZ` 改成读 `OrderZ`（= 兄弟序档 −0.9）⇒ 两次重建都是 −0.9 ⇒ 绿。
- ⚠️ **语义换了**（旧读法是 `localPosition.z`，画布上恒 0 ⇒ 那条会**恒绿、无判别力**）——
  我在 doc 里写明；要**加强**它的话见下面 §G。

### G. （**建议**，非必须）补一批「层本身对不对」的断言（照批 2 的做法）
```csharp
var cv = mp.CanvasRect;                        // 子画布
Check(cv != null && cv.overrideSorting, "…");
Check(cv.sortingLayerID == HudCanvas.SortingLayer.OverHand && cv.sortingOrder == 0,
      "换牌窗的排序组 = 原版那一行（OverHand / 0）");
Check(cv.GetComponent<CanvasScaler>() == null, "子画布不带 CanvasScaler（原版全场景只有一个、在根上）");
Check(mp.OnCanvas, "★ 灭自证：换牌窗的图与字都在 uGUI 画布上（不是退回世界空间 quad）");
```
（`CanvasRect` / `OnCanvas` 都是本次新加的口。）

### H. `:16505` §A218「换牌面板根 = 整屏矩形」—— **不用改**
- 壳仍在世界空间、`MenuDraw.SetPxSize` 没动 ⇒ 走 `box()` 的老那一支（`lossyScale == 1` + `LayoutSpace.Px(1920)`）⇒ 绿。
- ⚠️ 若主对话想让它改量**画布帧**（`mp.FrameRect`，那才是原版 `Mulligan`），期望值要按批 1/2 的做法换成
  `a218CvW/a218CvH`（批处理 = 1920×1440）——`FrameRect` 已经够用，我没再加口。

---

## 5. 类型检查

**本件的文件：0 错**（最后一次把我的代码改完那一遍，整仓运行时 0 / 编辑器 0）：
```
$ TMPDIR=/tmp/wf_mul bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

- 一共跑了 **6 遍**（每改完一段一遍）。最后两遍的唯一报错在 **`Battle/CardDisplayWindow.cs`**
  （`CS1503`/`CS0029`，`ImageQuad ⇄ HudPiece`，报错行号两遍从 1156 → 427 → 434 一直在动）
  —— **那不是本件的文件**（`CardDisplayWindow` 是另一位写手正在迁的那一扇）⇒ 按简报口径：
  **我只如实报告、一个字没碰**，那也是最后两遍拿不到「整仓 0」的原因。
- ⛔ 按要求**没跑**任何 `*.Run`、**没起** player。

---

## 6. 顺手发现的（⛔ 我一个都没顺手改）

### 6·1 为什么「命中/读回必须同批换帧」（本件最要紧的一条）
件搬到画布上之后，**世界帧的逆**（`ToDesignPixel`/`PxX`/`PxY`）**不再**是「件真正画在哪」的逆：
画布的「设计 px → 世界单位」系数**随屏变、而且两轴还能不同**（`T09` §七·8 实测：批处理下 x 1/108、y **1/144**）。
⇒ 若只搬画面不搬命中，4:3 下「换牌那一行/那颗钮」会**看得见的地方点不到**（差 1.333 倍）。
本件把 `HitDone` + 三个 `*WorldPos` 一起换成 `_frame` 那一套变换，**任何宽高比下都咬合**
（驱动层 `SimulateMulliganDone/Eye` 与真人点击走的就是这两个口）。
🔑 **这也解释了批 2 那五扇为什么没踩到**：它们的命中判定本来就是 `_panel.InverseTransformPoint` 那一族
（`SettingsPanel.RectContains:628`），**已经是画布帧** —— 本件是**跟着它们改的**，不是新发明。

### 6·2 🔴 **`OverHand` 组的子画布【兄弟序】很可能与原版相反**（要主对话裁）
- 事实：`AboveShader Canvas`（主对话自己在 `BattleDriver:14429/15239` 建的）**先**建、我这块 `Mulligan Canvas` **后**建
  ⇒ 我是**更后的兄弟** ⇒ **画在它上面**；
- 原版是**反的**：同一个 `FrontCanvas` 组里 `Mulligan` 兄弟序 **3/14** < `AboveShader` **8/14** ⇒ **AboveShader 压 Mulligan**。
- 同一组里还有（`T09c` §一 那一列）`SkillPanel` 1 · `UnitChat` 2 · `MultiCardDisplay` 4 · `ChoosePanel` 5 ·
  `CardDisplayWindow` 6 · `AlliancePanel` 13 —— **都在同一块 canvas 里靠兄弟序分先后**。
- ⇒ 与 `PopUps` 那格（`T09` §七·2 / `HudCanvas.SubCanvasRank`）**完全同型**：
  **要一个 `OverHand` 的次序表**（我这一块的 rank = 3/14 ⇒ 排在 `AboveShader` **之前**）。
  ⛔ 我没动 `HudCanvas.SubCanvasRank`（那是白名单外的文件）。

### 6·3 ⚠️ **子画布的「深度」这一格仍然空着**（`T09` §五·补⑨ 一·3 要求「层与深度同时设」）
- 我随批 2 先例：**没设** `localPosition.z`（原版那个值主对话没收；`EnsureSubCanvas` 也不收这个参数）
  ⇒ 本面板落在**根 canvas 平面**（世界 z = 0.3）。
- ⚠️ **可观察的后果（如实记）**：**压暗层盖不住换牌区那几张手牌** —— 卡还在世界空间、z = −0.9
  （比 0.3 **离相机近**）⇒ 透明排序里卡画在压暗层之上。
  改之前两者**同在 z −0.9**（同 z 的先后本来就不确定），所以这是**行为有变**的一处；
  ⛔ 修法不该在本件里发明（要么给子画布一个深度判据、要么等第 5 步卡迁上画布）—— **请主对话裁**。

### 6·4 ⚠️ **非 16:9：画布上的钮 与 世界空间的卡 会错开**（本件的已知中间态，给数）
两个帧本来就差一个系数（`T09` 正文那条「4:3 下整体偏大 1.333 倍」）。逐值算（`_shade`/钮都由**设计 px** 出）：
| 宽高比 | `CanvasScaler` | 卡底沿（画布 px，距屏心） | 钮心（设计 px） | 相对错开 |
|---|---|---|---|---|
| 16:9 | sf 1 · 画布 1920×1080 | −203.5 | −242.4 ⇒ **卡下沿下方 38.9 px**（= `BtnBelowCardPx` ✓） | **0** |
| 16:10 | sf 1 · 画布 1920×1200 | −226.1 | −242.4 | 钮偏高 **22.6 px** |
| 4:3 | sf 0.75 · 画布 1920×1440 | −271.3 | −242.4 | 钮偏高 **67.8 px**（那 41 px 高的钮整颗压在卡的下沿上） |
| 21:9 | sf 1 · 画布 2560×1080 | −203.5 | −242.4 | **0**（y 系数仍是 1/108） |

- ⚠️ 同一档下**卡本身在画布上的宽度**也变了：16:9 = 292 px（= 设计值）· 4:3 = **389 px**（`2.704 世界单位 × 144`）
  ⇒ 那颗钮（162.6 px 宽 = 设计值的 42%）**相对卡显得更小**（同一个 1.333 系数，不是第二处错）。
- 🔀 **另一条路（我没走，供裁）**：钮的位置不从**设计 px** 出，而改写「**从卡的世界位反算**」
  （`_frame.InverseTransformPoint(RowWorldPosAt(i,n))` + 一个按画布系数换算的下偏）⇒ 任何宽高比下
  钮都**贴着卡**；代价是「钮」不再落在原版那套设计 px 上（等于把卡的偏差复制给钮）。
  ⛔ 两条路都说得通，**我没自己拍** —— 按 §4④ 配方的字面意思走了**设计 px** 那条。

### 6·5 ⚠️ `Create` 失败那一档会**软锁**（不是崩）
子画布建不起来（根 canvas 没立 = 没有 HUD 相机）时我按批 2 口径 `return null`（⛔ 不退回世界空间）。
⚠️ 而 `BattleDriver.OpenMulligan:4719` 是 `if (_mulligan == null) return;`、`TickMulligan:4637` 也判 `_mulligan == null`
⇒ 那一档**换牌阶段永远走不完**（软锁，不崩）。**与批 2 五扇同一档**（`SettingsPanel` 那边甚至是 NRE）——
如实记，⛔ 没在别人的文件里补空判。

### 6·6 `Battle/CardChoicePanel.cs:1-16` 的过期文件头
仍写着「`MulliganPanel` 也派生自它」—— 实读 `grep -rn ": CardChoicePanel"` 只命中 `ChoosePanel.cs:48` 一处
（`T09c` §二·3 / §三·4 已记，**仍未订正**，那不在我的白名单里）。

### 6·7 `HudCanvas.Group` 不回收僵尸条目（`T09` §七·4 已知）—— 本件会累积
本件每局 `Open`/`Close` 都会 `DestroyImmediate` 旧按钮，而 `Group._kids/_zs` 不清
（实测路径：`A276` 探针连调 3 次 `mp.Open(...)`）。今天无害（`IndexOf` 对已销毁件不误判），
但**多局/换分辨率会积累** —— 缺 `PruneDestroyed`。

---

## 7. 没查清 / 没做的

1. **子画布的深度（`localPosition.z`）没设** —— 判据（原版那一级的值）`T09b` 没读到、批 2 五扇也没设；
   后果与请裁点见 §6·3。⛔ 按铁律 2 **没猜**。
2. **`OverHand` 组的宿主：合一块还是各一块？** 原版这一组全在**同一块** `FrontCanvas` 下（兄弟序 1…13）。
   我按批 2 的命名惯例起了**自己**的 `Mulligan Canvas`（先例 = `AboveShader Canvas`）——
   如果主对话要**合成一块** `FrontCanvas Canvas`，那是 `EnsureSubCanvas` 名字与全部 `OverHand` 写手的**同批改动**，
   ⛔ 不在我这一件的白名单里。**次序问题见 §6·2。**
3. **画布上的钮 vs 世界空间的卡**（非 16:9 错开）—— 给了数、给了另一条路，**等裁**（§6·4）。
4. **没有**「摘注册表」这一步可做（`§五`① 那一条）：**无此账** —— `MulliganPanel` 从来不在
   `_hudImages` / `_hudImageSpots` / `_hudLabels` / `_hudExtras` 里（那四个注册表只有
   `HudAbs/HudAbsTex/HudImage/HudImageTex` 四个助手会写，本件用的是裸 `ImageQuad.Create`），
   现读 `BattleDriver.ReanchorHud:16178-16224` 全文 **零** `_mulligan` ⇒ 不需要摘。
5. **`RefreshLayout` 那一步**（`§五`③）：**无此账** —— 本件没有 `RefreshLayout`；
   驱动层 `ReanchorHud` 也不碰它。本件里唯一跟着分辨率走的是**换牌区那一行**（`ReapplyRowPoses`），
   那是**卡**那一侧、本来就按世界位走 ⇒ **一个字没动**。
6. **没做的**（都在别人名下）：卡体迁 uGUI（第 5 步）· 输入退两条手搓沿 / `GraphicRaycaster` 真接（第 4 步）·
   拆 `ReanchorHud`（第 6 步）· `LayoutSpace` 读回口成对归零（第 7 步）·
   `CardChoicePanel.cs` 的过期文件头（§6·6）。
7. **没有跑任何 `*.Run` 自检、没有起 player**（现行口径）⇒ 本件**没有**「真机看到的那一档」的证据；
   8 步走完那次统一验收（三档宽高比截图并排 + `wf_click.py` 真点）时，本扇要**至少点这三处**：
   「完成换牌」（含倒计时自动完成那一路）· 「眼睛」（开关 + 压暗一起收）· 每张牌那颗「换」钮。
