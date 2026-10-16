# T09 批 3 · `CardDisplayWindow`（`OverHand` 组）迁移报告

> **写手**：主对话派出的**动手代理**（白名单只有 `Battle/CardDisplayWindow.cs` —— 本次改动**只碰了这一个文件**）。
> **判据**：`资料/任务线/战斗线/任务/T09_整层换uGUI.md` §五·补⑨/⑩ · `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一
> （`CardDisplayWindow` 那一行）· 同批既成写法 `Battle/MultiCardDisplay.cs` / `Battle/BattleLogPanel.cs` / `Battle/SettingsPanel.cs`。
> **行号一律「现读」**（本报告写完后本文件不再改动；引用旧行号时标 `旧:`）。

---

## 1 · 结论

1. ✅ **本窗已迁到它自己的子画布** `Card Display Window Canvas`（`OverHand` 层 / `sortingOrder 0`，
   **不带 `CanvasScaler`**）；窗本体 = 子画布下那个**铺满**的 `Card Display Window`（`HudCanvas.FillParent`）。
2. ✅ **7 件图 + 13 颗字 + 4 个分组节点**全部建在这块子画布上（逐处见 §2）。逐句对应：
   · 图 7 = 语音钮 1 + 风味底图 1 + 效果清单底板 5；
   · 字 13 = 提示行 1 + `AffectedBy` 1 + `EnchanterText` 5 + `EffectText` 5 + `LoreText` 1；
   · 分组 4 = 画布上 `Card Display` / `LowerSection` / `FlavourTextBG` / `EffectList`。
3. 🔴 **没迁 2 件（都是「有意」，不是漏迁）** —— **要调度台裁的那一条**：
   - **① 卡体那一族**（5 个位姿槽 + 相关卡，`CardView`）：`T09` §五·补⑧ 明写它走**第 5 步**
     （`Physics2D` 链路），`CardView` 也不在本件白名单 ⇒ 留在**世界壳**下（原点/矩形/缩放与迁之前**逐位相同**）。
   - **② 整屏压暗 `Menu Dark Background`**：🔴 **它与简报那句「所有图与字都在子画布上、⛔ 没有一个还留在世界空间」
     差的就是这一件**。理由、代价与「翻回去只要 3 行」→ **§7 · B1**。**我没自己拍板硬做**，也**没有**去发明
     「给卡体补一条排序层」那种机制（那是另一笔账，`T09` §七·8 / 批 2 报告 §6·3 都标着「没实机验过」）。
4. ✅ **卡体的世界坐标零风险**：`CardFan` 位姿换算、`MenuDraw.Local`、`SlotLocal` / `OpenStartLocal` /
   `PlayOpenAnimation` / `SwapToFront`、以及自检里 `LayoutSpace.ToPixel(卡的世界点)` 那一族 **一个字没改**
   （世界壳与迁之前那个根**逐位同构**：整屏 `RectTransform` + `localPosition = 0`）。
5. ✅ `typecheck`（秒级）：跑了 **6 次**，最终 **运行时 0 错 / 编辑器 0 错**（§5）。
6. ✅ **「画布在卡体之上」这条差异在本窗【一个像素都看不见】**（逐值算过，比 §7·B1 那条还让人放心）：
   卡体那一叠的**外接盒**（含转角，逐槽算过、极值都在槽 0 上）= x ∈ [698.4, 1221.6] · y ∈ [63.6, 896.4]
   （`CardFan`：槽 0 中心 (960,480) + `523.175×832.825`；其余 8 槽的 x 偏移**全为负**、尺寸 `_scl` 只减不增、
   转角最大 32.4° ⇒ 外接盒都落在槽 0 里面）；
   画布上的件：`EffectList` x ≥ **1358.1**（离卡体右缘 **136.5 px**）· 风味条 y ≥ **900.84**（离卡体下缘 **4.4 px**）·
   提示行 y = 1064 · 语音钮 x ∈ [1650.0, 1738.7] ⇒ **零重叠** ⇒ **没有一处需要「谁压谁」的裁决**。
   （补充判据：`EffectList` 在原版兄弟序里本来就排在 `Cards` **之后** —— `2D层_battlearena1全树.md:590` 那一段缩进 =
   `Card Display` 的孩子依次 `LowerSection`(1) / `Cards`(2) / `TutorialObjs`(3) / `EffectList`(4) ⇒ 即便将来卡体变大压过来，
   「画布在上」也是**对的方向**。）

---

## 2 · 改动清单（逐处「旧口（行号）→ 新口」）

### 2·0 子画布与两个节点的建立（`Create`，现读 `:286-329`）

| 旧 | 新 | 现读行号 |
|---|---|---|
| `go` 直接挂 `parent`、`MenuDraw.SetPxSize(go, 1920, 1080)`（**世界单位** = ÷108） | **新节点** `Card Display Window (world)`（世界壳）挂 `parent`，`MenuDraw.SetPxSize` **原样不动** | `:292-294` |
| —（原来没有画布） | `HudCanvas.Ensure(LayoutSpace.Cam)`（幂等兜底）+ **`HudCanvas.EnsureSubCanvas(null, "Card Display Window Canvas", HudCanvas.SortingLayer.OverHand, 0)`** | `:299-300` |
| 同上 | 建不出来 ⇒ **`DestroyImmediate(shell)` + `LogError` + `return null`**（⛔ 不退回去建世界空间 quad） | `:301-308` |
| 根节点 `go` = 组件自己那个 GO，挂 `parent` | 同一个 GO（名字 `Card Display Window` **不变**）改挂**子画布**，`HudCanvas.FillParent((RectTransform)go)` | `:319-323` |
| — | `w._cardsShell = shell.transform` · `w._subCanvas = cvRt.GetComponent<Canvas>()`（都在 `Build()` **之前**赋值） | `:325-326` |

**子画布参数**（判据 = `T09c` §一 那一行：`Card Display Window` @ `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas`）：
`name = "Card Display Window Canvas"` · `sortingLayerID = HudCanvas.SortingLayer.OverHand`（`2007638115`）·
`sortingOrder = 0` · `overrideSorting = true`（`EnsureSubCanvas` 内部设）· **无 `CanvasScaler`** · 自带 `GraphicRaycaster`（`EnsureSubCanvas` 内部设）。

### 2·1 逐处换算表（本文件内）

| # | 件 | 旧口（旧行号） | 新口（现读行号） | 备注 |
|---|---|---|---|---|
| 1 | 整屏压暗 `Menu Dark Background` | `MenuDraw.Rect(_root, …)`（旧 `:195-198`） | **⛔ 没迁**：`MenuDraw.Rect(_cardsShell, …)`（`:437-441`）+ `SetZ(_mask, ZMask)` —— 只换了父件（世界壳） | 见 §7·B1 |
| 2 | 提示行 `cdw_hint` | `Label.Create(_root, …, EndPanel.Pos(960,1064,ZContent), 2, …)` + `SetRenderQueue`（旧 `:206-208`） | `Text(_root, "cdw_hint", …, HudCanvas.PosPx(960,1064), 2, fontPx:0 …)`（`:457-459`） | `scale: 2` **照旧**；z 取 `ZContent − 0.02`（多给 0.02 的理由在代码注释里） |
| 3 | `Card Display` 容器 | `MenuDraw.Node(_root, …)`（旧 `:211-212`） | `Node(_root, "Card Display", CardL…CardB, ScreenC, ZGroup)`（`:466-467`） | 矩形**逐值不动** |
| 4 | `Cards` 容器（装卡体） | `MenuDraw.Node(cardDisplay, "Cards", …)`（旧 `:213-215`） | **⛔ 没迁**：改挂**世界壳**（`:477-483`），`MenuDraw.Node` + 同一 `PxRect` | 世界坐标逐位不变 |
| 5 | `LowerSection` 容器 | `MenuDraw.Node(cardDisplay, …)`（旧 `:216-217`） | `Node(cardDisplay, "LowerSection", …, CardDisplayC, ZGroup)`（`:468-469`） | 同上 |
| 6 | `FlavourTextBG` 容器 | `MenuDraw.Node(_lower, …)`（旧 `:218-219`） | `Node(_lower, "FlavourTextBG", …, LowerC, ZGroup)`（`:470-471`） | 同上 |
| 7 | 语音钮 `Voice Over Button` | `MenuDraw.Rect(_lower, tex, 88.655² 框, …, keepAspect: true)`（旧 `:225-227`） | `Img(_lower, tex, …, Rel(VoiceCx,BtnCy,LowerC), wPx, hPx, ZContent, null)`（`:495-502`），宽高仍走 `ImageQuad.FitHeight(BtnS, BtnS, 贴图比例)`（`:497-498`） | 贴图 128×128 ⇒ 内接后就是那个 88.655² 框；落位 = 相对 `LowerSection` 中心 (734.33, 0)（**原版就是这两个数**） |
| 8 | `EffectList` 容器 | `MenuDraw.Node(_root, …)`（旧 `:250-253`） | `Node(_root, "EffectList", …, ScreenC, ZGroup)`（`:525-528`） | |
| 9 | `AffectedBy` 标题 | `MenuDraw.Text(_fxRoot, tr, TitleText, …, EffTitlePx, QEffect, tr.W, 19.3, FxTitleAutoMax)` + `SetZ`（旧 `:259-267`） | `Text(_fxRoot, "AffectedBy", …, Rel(EffTitleCx, EffTitleCy, FxC), 5, EffTitlePx, EffTitleW, EffTitleH, 19.3, FxTitleAutoMax, ZContent−0.02, …)`（`:533-535`） | 四格（标称/下限/上限/框）**逐值不动**，只把单位从「世界单位」换成 px |
| 10 | 效果底板 `EffectBg` ×5 | `MenuDraw.Rect(_fxRoot, …)` + `SetZ`（旧 `:284-288`） | `Img(_fxRoot, bgTex, "EffectBg", Rel(EffCx, cy, FxC), EffBgW, EffBgH, ZContent, null)`（`:558-561`） | |
| 11 | `EnchanterText` ×5 | `MenuDraw.Text(…)` + `SetVAlign(Bottom, LayoutSpace.Px(EffWhoH))` + `SetZ`（旧 `:294-307`） | `Text(_fxRoot, "EnchanterText", …, EffWhoW, EffWhoH, 10, FxTextAutoMax, ZContent−0.02, …)` + `SetVAlign(Label.VAlign.Bottom, CardWinBox.EffWhoH)`（`:567-578`） | 🔴 框高那一格**去掉 `LayoutSpace.Px`**（uGUI 档直接给 px） |
| 12 | `EffectText` ×5 | `MenuDraw.Text(…)` + `SetZ`（旧 `:310-317`） | `Text(_fxRoot, "EffectText", …, EffWhatW, EffWhatH, 10, FxTextAutoMax, ZContent−0.02, …)`（`:585-589`） | |
| 13 | 风味底图 `FlavourTextBG/Image` | `MenuDraw.Rect(_bg, tex, 1320×178, "Image", QChrome, null, false)` + `SetZ`（旧 `:880-882`） | `Img(_bg, tex, "Image", Vector3.zero, 1320, 178, ZContent, null)`（`:1168-1172`） | `_bg` 自己那一块 = 那张图 ⇒ 相对父中心恒 (0,0) |
| 14 | `LoreText` | `MenuDraw.Text(_bg, r, body, …, LorePxBattle, QChrome, r.W, 18, LoreAutoMax)` + `SetZ`（旧 `:905-907`） | `Text(_bg, "LoreText", body, Rel(…LoreL/R/LoreT/B…, LoreBgC), 5, LorePxBattle, 1250, 93.47, 18, LoreAutoMax, ZContent, …)`（`:1196-1203`） | 四格逐值不动（**居中**，不是右对齐） |
| 15 | `Hide()` / `Show()` / `Hide` 的清理 | `MenuDraw.ClearChildren(_cards)` / `(_bg)` | **一个字没改**（`_cards` 在世界壳下、`_bg` 在画布上，两处都成立） | `:935-936`（`Hide`）· `:1156`（`RefreshLore` 重建那块之前） |

### 2·2 本文件新加的建件口（**uGUI 档的口**，⛔ 别在别处再写一份）

| 口 | 现读行号 | 语义 |
|---|---|---|
| `Rel(xPx, yPxFromTop, cx0, cy0)` | `:355` | 「整屏绝对 px（左上原点 y 向下）→ 父件局部（中心原点 y 向上）」**本文件唯一那一份换算**；与 `HudCanvas.PosPx` 同式（那个是「父件 = 整屏」的特例） |
| `Img(parent, tex, name, pos, wPx, hPx, z, tint)` | `:363` | 走 `HudCanvas.MakeImageInParent`（中心锚、`sizeDelta` 直接吃 px、`PA=0` 拉满 = 原版 `m_PreserveAspect = 0`）；贴图取不到 ⇒ 出声 + `null` |
| `Text(parent, name, text, pos, scale, fontPx, boxW, boxH, autoMin, autoMax, z, color, anchor)` | `:383` | 走 `HudCanvas.MakeText` → `Label.CreateUgui`；`SetGlyphHeight(px)` + `SetWrapWidth(px)` + `SetAutoFitBox(px…)` —— 与 `MenuDraw.TextCore` 的**唯一差别就是单位**（不 ÷108）+ 不设渲染队列 |
| `Node(parent, name, x1,y1,x2,y2, parentCenter, z)` | `:409` | 纯分组节点：`HudCanvas.SetPxSize(sizeDelta = px)` + `Rel` 落位 + `HudCanvas.Add(z)` |
| `SetZ(HudPiece, z)` | `:786` | 画布件的**兄弟序**档 —— **必须走 `HudCanvas.Reorder`**（`Add` 不去重，会留两条账，`T09` §七·11） |
| `SetZ(ImageQuad, z)`（**保留**） | `:776` | 世界空间 quad 的 z —— 本窗只剩**遮罩**一件走它 |

### 2·3 读回口 / 命中口

| 口 | 旧（旧行号） | 新（现读行号） | 说明 |
|---|---|---|---|
| `EffectTextInFrontOfBg(i)` | 比 `transform.position.z`（旧 `:124-130`） | 比 **`HudCanvas.OrderZOf`**（`:234-241`） | 上画布后 `localPosition.z` 恒 0 ⇒ 老口径**恒真、无信息量**；`Add` 按 z 降序 ⇒ **值小的仍在前**，语义同向 |
| `EffectWhoLeftPx/WhatLeftPx` → `LeftPx` | `LayoutSpace.PxX(世界)`（旧 `:138-144`） | `lb.WorldW`（**uGUI 档就是 px**）+ `localPosition.x` + `FxC.x`（`:252-257`） | `T09` §七·8：`Label.WorldW` 读 `textBounds`，uGUI 档单位 = 画布 px ✓ |
| `PlaceLeft(lb, basePos, boxW)` | 「世界长 → px」再「px → 世界」两跳（旧 `:481-487`） | **直接用 px**（`:761-769`） | ⛔ 留一个 `LayoutSpace.Px` 就把整行字推出框外（差 108 倍、不报错） |
| `HitSlot` / `HitVoice` / `ContainsPointer` / `HitBtn` | `LayoutSpace.ToPixel(世界点)` 比设计 px 常量 | **一个字没改**（`:806` / `:839` 现读） | 与「那件建在哪一层」无关（⚠️ 非 16:9 下画布会缩、世界那份不缩 ⇒ 两者分家，那是第 4/5 步的账） |
| 新增自检口 | — | `SubCanvas`（`:335`）· `CardsShell`（`:339`）· `WindowRect`（`:343`） | 形状照 `BattleLogPanel.PanelRect` |

### 2·4 简报 §⑤「同批必须做的四件」

| # | 要求 | 本件实况（带出处） |
|---|---|---|
| ① | 把迁走的件从 `ReanchorHud` / `_hudImages` / `_hudImageSpots` 那一族摘掉 | ✅ **无此账**：那两个注册表只由 `HudAbsTex/HudImageTex` 喂（`BattleDriver.cs:16120-16121`），`ReanchorHud`（`:16178-16224`）只拉 `_hudLabels` / `_hudImages` / 牌堆 / 灯 / 四个 `RefreshLayout`；`grep -n "_hudImages\|_hudImageSpots\|CardDisplay" BattleDriver.cs` ⇒ **本窗一件都不在里面**（掩罩/语音钮一直都是 `MenuDraw.Rect` 建的，从不登记） |
| ② | 读回口改成读 `RectTransform` / `HudPiece` / `sizeDelta` | ✅ 三处，见 §2·3（`EffectTextInFrontOfBg` / `LeftPx` / `PlaceLeft`）；`_fxBg` 类型 `ImageQuad[]` → `HudPiece[]`（`:197`），`_voiceBtn` `ImageQuad` → `HudPiece`（`:189`） |
| ③ | `RefreshLayout` 里对这批的操作 | ✅ **本件没有 `RefreshLayout`**（`grep RefreshLayout` 本文件零命中；`ReanchorHud` 也不调本件）—— uGUI 档的锚点由 `RectTransform` + 子画布自己管，**不需要**换分辨率重锚 |
| ④ | 报告里列出「哪几条断言要改成什么」 | ✅ 见 §4 |

---

## 3 · 父链对照（我们 vs 原版）

**原版**（`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:336-341,590`；⚠️ 那份是**截断版**，缺口在 HUD，
但本窗这一支**完整**，我逐行读过；坐标列格式 = `[x1−960, y1+540, w×h]`，与 `CardWinBox` 的常量**逐值吻合**）：

```
BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas
└── Card Display Window                 [0,1080 0x0]   （stretch = 整屏）
    ├── Menu Dark Background            [-2287,-206 4575x2572]   ← x1 = −1327.3 ✓ MaskL · y1 = −746.18 ✓ MaskT
    └── Card Display                    [-376,646 752x868]        ← x1 = 584 ✓ · y1 = 106 ✓
        ├── LowerSection                [-660,1461 1320x137]      ← x1 = 300 ✓ · y1 = 921.34 ✓
        │   ├── FlavourTextBG           [-660,1441 1320x178]      ← y1 = 900.84 ✓
        │   │   └── LoreText            [-625,1483 1250x93]       ← x1 = 335 ✓ · y1 = 943.11 ✓
        │   └── Voice Over Button       [690,1486 89x89]          ← 中心 (1694.3, 990.3) ✓ VoiceCx/BtnCy
        ├── Cards                       [-50,1030 100x100]        ← 中心 (960,540) ✓
        ├── TutorialObjs                [-2173,-504 4253x3159]    ← 我们**不建在这一支**（`TutorialOverlay` 自己建，**迁移前就如此**）
        └── EffectList (inactive)       → AffectedBy + Elements → 5 × EffectElement
            （`EffectList` 的 `localScale = 108.79` ⇒ 子树 1 单位 = 108.79 px，见 `CardWinBox` 那段出处）
```

**我们（迁移后）**：

```
HudRoot（世界空间）
└── Card Display Window (world)         ★ 世界壳（我们加的，⛔ 非原版件）· 整屏 + 原点 + 无缩放
    ├── Menu Dark Background            ← **有意没迁**（世界空间 quad，队列 3000、z −2.0）
    └── Card Display                    ← 原版那一级的矩形逐值照旧
        └── Cards → CardUI (0..N)       ← **卡体**（`CardView`，世界空间；第 5 步那批）

BattleHud Canvas（根 canvas，HudCanvas 建）           ← 与上面那棵**不是**父子关系
└── Card Display Window Canvas         ★ 子画布（OverHand / order 0）
    └── Card Display Window            ← 本组件自己那个节点，`FillParent` = 原版那个 stretch 整屏
        ├── cdw_hint                   ★ 我们自加的（原版没有这一行，见代码里 W12/W6 那条判据）
        ├── Card Display   (752×868)   ← 只装 `LowerSection`
        │   └── LowerSection (1320×137)
        │       ├── FlavourTextBG (1320×178) → Image + LoreText
        │       └── Voice Over Button (88.655²)
        └── EffectList  → AffectedBy + 5 × (EffectBg + EnchanterText + EffectText)
```

**四处差异（逐条如实记）**：
1. 🔴 **`Cards` 不在 `Card Display` 下了** —— 它装的是卡体（世界空间），只能挂到**世界壳**下
   ⇒ **两个节点同名**（世界壳下一个 `Card Display`，画布上一个）。这是「卡体那一族没迁」的必然结果。
2. 🔴 **原版那一整棵在同一个父件下**（`Card Display Window`），我们分成**两棵树**（世界壳 + 子画布）——
   同上，随第 5/6 步收口。
3. ⚠️ **`EffectList` 我们挂在窗根、原版挂在 `Card Display` 下**（原版 `2D层_battlearena1全树.md:590` 的缩进是
   `Card Display` 的子件）—— **这不是本次引入的**（迁移前就挂根，本次没动这一层，只换了单位）。
   ⇒ 新账（不在本件白名单的范围内也能改，但改层级会影响 `_fxRoot` 的坐标系 ⇒ 建议留给 OverHand 组收口时一起做）。
4. ⚠️ **锚点体系**：我们一律**中心锚**（`Img`/`Text`/`Node` 都是），原版各件锚点不同
   （例：`EffectList` 那一块 `EffL = 1358.1` 距右缘只 **21.4 px** ⇒ 原版那一级看起来是**贴右缘**的）。
   ⇒ 非 16:9 下我们会**恒中心偏移**、原版会**跟边**。这是 `T09` 全批的既有口径（§五·补⑩ 只规定「根用 `FillParent`」），
   ⛔ 本件没自己发明逐件锚点 —— 如实记，交调度台。

---

## 4 · 要改的断言（逐条；⛔ 本代理**没改** `Editor/BattleScene.cs`）

### 4·1 🔴 会**变红**的一条（必须改）

| 位置 | 现状 | 为什么红 | 改成什么 |
|---|---|---|---|
| `Editor/BattleScene.cs:16528` | `box(CardDisplayWindow.Create(a218root.transform).transform, "卡牌展示窗根", 1920f, 1080f, "原版 `Card Display Window`（stretch ⇒ 整屏）");` | `box()` 对**画布下的件**走的是新那一支尺子（`:16490-16499`：比 `rt.rect` 与字面 px）——本窗根现在挂在子画布下、`FillParent` ⇒ 它的 `rect` = **子画布的 rect**。批处理下 `Screen = 640×480` 且 `cam.aspect` 钉死 16:9 ⇒ `scaleFactor = 0.3333` ⇒ **画布 rect = 1920×1440**（判据 = 同批 `T09批2_*` 报告 + `T09` §七·8）⇒ 期望值 1080 那条**必红** | `box(..., "卡牌展示窗根（画布上）", a218CvW, a218CvH, "原版 `Card Display Window`（stretch ⇒ **铺满画布**；`Expand` 档下画布比 1080 高）");`（`a218CvW/H` 就在上面 `:16517-16518` 现成） |

⚠️ **本报告里 `BattleScene.cs` 的行号是「写报告那一刻的现读」** —— 主对话**正在改那个文件**
（我今天先读到的 `:16507`，半小时后同一句已经挪到 `:16528`）⇒ 请按**内容**（`box(CardDisplayWindow.Create(…)`）
去定位，别按行号。

**建议顺手补两条**（同一段里，成本极低、而且**这才是本片真正的自证**）：
```csharp
// ① 世界壳：仍是「世界空间整屏」那一档（老尺子那一支）
box(CardDisplayWindow.Create(a218root.transform).CardsShell, "展示窗**世界壳**（压暗 + 卡体那一族的父件）",
    1920f, 1080f, "⚠️ 这一级是**我们加的**（原版没有）：只为把世界空间那两样挂住 —— 见报告 §1/§7·B1");
// ② 画布上那个分组帧：矩形逐值 = 原版 `Card Display`（752×868）
```
（② 需要把 `CardDisplayWindow` 的 `Card Display` 帧暴露出来；本件**没加**那个口 —— 加它属于「为断言改生产代码」，
按简报「断言由主对话统一改」的口径，我不越界。若需要我可以补一个只读口，报同一句即可。）

### 4·2 ✅ **不需要改**的（逐条核过，说明为什么）

| 位置 | 现在读什么 | 为什么仍然成立 |
|---|---|---|
| `BattleScene.cs:5476-5486` | 在 `driver.CardDisplay.transform.GetComponentsInChildren<Transform>(true)` 里数 `EffectBg` / `EnchanterText` / `EffectText` 各 5 个 | `EffectList` 那一支**整支在画布上、父链仍在 `cdw.transform` 下** ⇒ 计数照旧 5/5/5 ✓ |
| `BattleScene.cs:8077-8104` | `cdw.transform.GetComponentsInChildren<Transform>(true)` 收 `winNodes`，断「没有 `Show Card Text`」「有 `Voice Over Button`」「没有 `CardDisplayOptions` 那一族」 | 三样都在画布上、都在 `cdw.transform` 子树里 ✓（卡体搬走了，但那几条**不数卡体**） |
| `BattleScene.cs:8106-8122` | `cdw.transform.Find("Card Display") → Find("LowerSection") → Find("FlavourTextBG")`，断「没字 ⇒ 整块不亮」 | 画布上**仍有** `Card Display/Card/Section/FlavourTextBG` 这条链 ✓（`FlavourTextBG` 的 `SetActive` 逻辑一个字没改） |
| `BattleScene.cs:7741-7832`（整叠卡：位姿 / 缩放 / 转角 / 队列 / 开窗动画起点·半程·终点） | `LayoutSpace.ToPixel(sv.transform.position)`、`transform.localScale`、`CardQueue(...)` | **卡体的世界位与队列一个字没改** ✓（世界壳逐位同构） |
| `BattleScene.cs:7848-7886`（点击区四条边） | `cdw.HitSlot(EndPanel.Pos(...))` | 命中口一个字没改 ✓ |
| `BattleScene.cs:5420-5429`（左缘 / 文字在底板前面） | `EffectWhatLeftPx(i)` / `EffectTextInFrontOfBg(i)` | 两个口都**同批换了实现**（§2·3）⇒ 断的语义不变；`EffectWhoLeftPx` 的期望值 `EffCx − EffWhoW/2 = 1394.5` 与我们新算式**逐值相同**（算式：`local.x + FxC.x − w/2`，其中 `local.x = (EffCx − FxC.x) − dx` ⇒ 得 `EffCx − dx − w/2 = EffCx − EffWhoW/2` ✓） |
| `BattleScene.cs:11305-11308` | 只引 `CardDisplayWindow.QChrome 3000` 这个**名字**（在文案里） | `QChrome` **值不变、也没删**（`CardDisplayWindow.cs:138`，只是本件不再调 `SetRenderQueue`）✓ |

### 4·3 🆕 建议新增的断言（照批 2 的形状；**要主对话落**）

1. **子画布本身**（判据 = `T09c` §一 那一行）：
   `var cv = cdw.SubCanvas;` ⇒ `cv != null` && `cv.overrideSorting == true` &&
   `cv.sortingLayerID == HudCanvas.SortingLayer.OverHand` && `cv.sortingOrder == 0`
   && **`cv.GetComponent<CanvasScaler>() == null`**（原版全场景只有一个、在根上）。
2. **「没有一件还留在世界空间」的机械证明**（本片最硬的一条，也是本次验收标准的落地）：
   `cdw.transform.GetComponentsInChildren<ImageQuad>(true).Length == 0`
   （画布上的件是 `Image`/`TMP_UGUI`，任何 `ImageQuad` 都是**没迁干净**的漏网）。
3. **卡体确实在世界空间**（灭自证：防「顺手把卡也挪上画布」而不出声）：
   `cdw.CardsShell != null` && `cdw.SlotView(0).transform.IsChildOf(cdw.CardsShell)` && 世界壳 `lossyScale == 1`。
4. **单位对**（防「把世界单位喂进 uGUI 档 ⇒ 小 108 倍」那种静默错）：读 `sizeDelta` 比px 字面量 ——
   遮罩 4574.6×2572.36（⚠️ 它是**世界空间**那一件，走不到 `sizeDelta`，改读 `WorldW/H × 108`）·
   风味底图 **1320×178** · 效果底板 **534.7×129.4** · 语音钮 88.655²。
   ⚠️ 画布件的 `sizeDelta` 就是 px，**别**再 `× 108`（`T09` §七·8）。
5. **`_hint` 那一档**（它是本件唯一有意偏离 z 文字的地方）：`HudCanvas.OrderZOf(...) < ZGroup` 之类的次序断言 —— 可选。

---

## 5 · 类型检查

| 次 | 命令 | 结果 |
|---|---|---|
| 1 | `TMPDIR=/tmp/wf_cdw bash d:/4/Unity/工具/typecheck.sh` | 运行时 **2 错 —— 全在 `Battle/MulliganPanel.cs(852,879)`（`error CS0103: 当前上下文中不存在名称"frame"`）**＝**别的写手正在写的文件**；编辑器程序集因上游失败报 `CS0006`（WFCheck.dll 没产出）⇒ **本文件没有一条错**（若有会被一并列出） |
| 2 | 同上（隔 45 s 重跑） | **运行时 0 错 / 编辑器 0 错** |
| 3 | 同上（改完「遮罩留在世界空间」那一段之后） | **运行时 0 错 / 编辑器 0 错** |
| 4 | 同上（最后一条注释改完之后） | **运行时 0 错 / 编辑器 0 错** |
| 5 | 同上（**收尾那一次**，工作区里别人还有半成品在飞） | **运行时 0 错 / 编辑器 0 错** |
| 6 | 同上（**最后一条注释改完之后**） | **运行时 0 错 / 编辑器 0 错**（终态；`git diff --numstat` = **394 / 91** ⇒ 行尾没被翻、不是整篇重写） |

⚠️ 第 1 次那两条**不是本件的问题**（简报 §⑧ 那条：错误全集中在别人负责的文件上 ⇒ 隔会儿重跑并写清）✓ 重跑即消。

---

## 6 · 顺手发现的（⛔ 我没改别人的文件）

1. 🔴 **`Battle/MultiCardDisplay.cs` 与本件是同一道选择题**（批 3 的另一位写手，同一批）：
   它把 **`mcd_mask` 留在世界空间**、并在文件头写了三条理由（含「在卡片迁上画布之前，『必须画在卡片后面』的
   整屏件一个都不能上画布」）——**与本件 §7·B1 同一条**。⇒ **OverHand 组收口时，这一条按「一条口径」统一过一遍**
   （现在批里有两种做法：`MultiCardDisplay` 留世界 / `MulliganPanel` 把 shade 迁上画布 —— 后者若 shade 要盖住卡，
   它的卡也会被压暗，**建议主对话回看那一处**）。
2. ⚠️ **`HudCanvas.Group` 的账不回收已销毁的子件**（`T09` §七·4 已记）——**本窗是这一族里最重的使用者**：
   `RefreshLore()` 每次开窗（`Show`）都 `MenuDraw.ClearChildren(_bg)` + 重建 2 件 ⇒ 那一组的 `_zs/_kids` **每次 +2 条
   僵尸记录**（只增不减，一直涨）。今天**无害**（`Group.Add` 最后是 `SetSiblingIndex(i)`，索引超界被 Unity 夹到末尾
   ⇒ 同 z 的先后仍然等于创建顺序，我逐值推过），但多局/反复开关会积累 ⇒ 仍缺一个 `PruneDestroyed`。
3. ✅ **`2D层_battlearena1全树.md` 的坐标列格式**（顺手核出来的，可复用）：`[x1 − 960, y1 + 540, w×h]`
   （我拿 `CardWinBox` 的 8 个常量逐值验过：`MaskL −1327.3` / `CardL 584` / `LowerL 300` / `LoreBgT 900.84` /
   `LoreL 335` / `CardsCx 960` / `VoiceCx 1694.33` / `EffTitleCy 161.2` **全部吻合**）——
   ⚠️ 但那份是**截断版**（`README` 与文件头都写着「缺口正好在 HUD」）⇒ 查 HUD 侧的件仍要用 arena2 或别的 10 份。
4. 📌 **`EffectList` 在我们这棵树里挂根、原版挂 `Card Display` 下**（§3·3）—— 迁移前就如此，本次**没动这一层**
   （只换了单位）；`_fxRoot` 的坐标系跟着父件走 ⇒ 改层级要连带重算 `FxC`（一处常量）⇒ 建议收口时一起做。
5. ⚠️ **`BattleScene.cs:11305-11308` 那句文案**现在**半过期**：`CardDisplayWindow.QChrome 3000` 这个数在本件
   已经**不再是任何件的实际队列**（只剩遮罩那一件的**来历**注释 + 那一处 `MenuDraw.Rect` 的队列形参）——
   卡体那一族「**卡 ≤ 3000**」那条约束**仍然成立**（`CardView.cs:713-720`：`QCard = 3000` 是**上限**，
   全仓几处按它写死），但**本件不再靠它分层**了。
   ⛔ 我没改那句话（`BattleScene.cs` 不在白名单）。

---

## 7 · 没查清 / 没做的（含**要调度台裁的一条**）

### A · 没做的（都不在本件白名单 / 都是别的批次）

| # | 事项 | 为什么不在这里做 |
|---|---|---|
| A1 | **`Editor/BattleScene.cs` 的断言**（§4·1 那一条必改 + §4·3 五条建议） | 简报 §⑥ 红线：所有面板的断言都由**主对话统一改** |
| A2 | **`HudCanvas.Group` 补 `PruneDestroyed`**（§6·2） | `Battle/HudCanvas.cs` 是**主对话正在改**的文件 |
| A3 | **`EffectList` 挂回 `Card Display` 下**（§3·3） | 涉及 `_fxRoot` 坐标系重算 ⇒ 属 OverHand 组收口那一步（`T09` §五 第 5 条） |
| A4 | **逐件锚点复刻**（§3·4：原版 `EffectList` 看起来贴右缘） | `T09` 全批口径是「根 `FillParent` + 件用中心锚」；逐件锚点是**另一笔账**，⛔ 不自己发明 |
| A5 | 非 16:9 下「画布上的钮」与「世界空间的卡」分家（命中那一支） | 第 4/5 步的账（同 `MulliganPanel` 文件头那条「只换半边」的已知代价） |

### B · 🔴 **要调度台裁的一条：整屏遮罩留在世界空间**（＝与简报验收标准的差）

**B1 · 事实与理由**
- **它是什么**：`Menu Dark Background`（原版 4574.6×2572.36 · `color = (0,0,0,0.772549)`），原版是
  `Card Display Window` 的**第 1 个子件**（`2D层_battlearena1全树.md:337`）⇒ **它在卡体【下面】**。
- **为什么不能上画布**：卡体还在 `Default` 排序层（`CardView` 全族 `renderQueue 3009+`），而我们的子画布是
  **`OverHand`（更高）**；uGUI **先比排序层**（`T09b` §A·0·3：层有全局先后 → 层内比 order → 再比兄弟序）
  ⇒ **画布上的任何件都画在卡体之上**（`T09` §五·补⑤ ⑤：「给了 `sortingOrder` ⇒ 那一扇盖住所有还没迁的件」）。
  ⇒ 遮罩挪上去 = **遮罩盖住卡体 = 本窗打开时只剩一块压暗**（窗就废了）。
- **同批先例**：`Battle/MultiCardDisplay.cs` 的 `mcd_mask`（**同一个原版组件、同一个 alpha**）就是**有意不迁**的，
  它文件头写着「**在卡片迁上画布之前，「必须画在卡片后面」的整屏件一个都不能上画布**」。
- **✅ 两个机制都指向「卡在遮罩之上」**：即便「排序层 vs `renderQueue` 谁先比」这一条**没实机验过**
  （`T09` §五·补⑨ 一·3 自己写着没验），本件两种假设下**结果相同** —— 同层（`Default`/0）时比 queue（遮罩 3000 < 卡 3009+），
  比层时两者同层 ✓ ⇒ **与今天逐位相同**。（而「挪上画布」在「层优先」那条假设下**当场出错**。）
- **代价**：本窗留在世界空间的图/字 = **1 件**（遮罩）。其余 7 图 + 13 字 + 4 分组节点全在画布上。

**B2 · 如果调度台要「按简照字面全迁」**：改 **3 行**（`:437-441` 换成
`Img(_root, CardArt.Solid(), "Menu Dark Background", Vector3.zero, 4574.6, 2572.36, ZMask, CardWinBox.ShadeColor)`；
`_mask` 类型 `ImageQuad → HudPiece`（`:187`）；`SetZ(ImageQuad, …)` 那半支随之成死代码）。
⚠️ 但那样**必须同批**给卡体补一条排序层规则（`sortingLayerID = OverHand` + 每槽 `sortingOrder`，
并在 `SwapToFront` 里跟着重排）—— 那是**本件没有实机验证手段**的那条路（批 2 报告 §6·3 的「选项 ② / ③」），
⛔ 我**没有**自己做（红线：不自己发明口径）。

### C · 没查清的（逐条写「查了什么、结果是什么」）

1. **`EventSystem` / 画布上的命中**：本件**没接** uGUI 射线（原版 `voiceOverButton` 是 `Button`）——
   命中仍走 `HitVoice(世界点)`（`BattleDriver.HandleDisplayWindowClick`）。**没查**：改走射线要 `EventSystem`，
   而它按 `T09` §五·补③ 三·3「本轮不建」⇒ 本件跟随该口径，⛔ 没自己建。
2. **原版各件的锚点**（`m_AnchorMin/Max`）：本件**只读了矩形**（全树 dump 只有坐标/尺寸，没有锚点字段）
   ⇒ §3·4 那条「贴右缘」是**从矩形位置推的**（距右缘 21.4 px），**不是**实读 `m_AnchorMin`。
   要逐位对齐得去读 `RectTransform_*.json` —— ⛔ 本件没做（不在本批的收口范围内）。
3. **`40k_UI_bt_voicelines` 的框**：贴图 128×128（比例 1）⇒ `keepAspect` 与「拉满」**同值**，
   所以本件「内接」那条算式**在本工程这张图上没有鉴别力**（照原版写法保留，便于以后换图）。
4. **`m_Sliced` / `m_PixelsPerUnitMultiplier`**：本窗**没有九宫格件**（唯一那颗 `40K_display` 是拉伸的）
   ⇒ 批 2 report §七·14 那条（ppuMul 恒 1）**本件不涉及**。
