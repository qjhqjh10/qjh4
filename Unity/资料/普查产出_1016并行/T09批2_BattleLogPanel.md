# T09 批 2（`PopUps` 组）· `BattleLogPanel`（战斗战报窗）迁 uGUI —— 执行代理报告

> **执行者**：动手代理（全权限子代理）· **日期** 2026-10-16 · **白名单** = `Battle/BattleLogPanel.cs`（只改了这一个文件）。
> **口径来源**：`资料/任务线/战斗线/任务/T09_整层换uGUI.md` **§五·补⑨ / §五·补⑩** ·
> `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` **§一**（`CemeteryLogPanel` 那一行）·
> `T09b_uGUI拦路判据.md` **§一 A·1/A·2/A·3**。
> ⛔ 本报告只列事实与判据；`Editor/BattleScene.cs` 的断言改动**留给主对话**（红线）。

---

## 1. 结论

**做完了**：这扇面板里的**每一件图与字都建在它自己的子画布上**，⛔ **没有一件留在世界空间**。

- **子画布 = `CemeteryLogPanel Canvas`**（原版宿主名 + `" Canvas"`，同 `LoadingMenu Canvas` /
  `TooltipManager Canvas` / 同批 `BattleDoors Canvas` 的先例）—— **`PopUps` 层 / `sortingOrder` −1**、
  `overrideSorting = true`（`EnsureSubCanvas` 里设）、挂在**根 canvas** 下、**不加 `CanvasScaler`**。
  层/order 判据 = `T09c` §一 的 `CemeteryLogPanel` 那一行 + `T09b` §A·2 #7。
- 面板节点（`CemeteryLogPanel`）+ 压暗层（`BattleLogShade`）都是子画布的子件；底板 / 四条边框 /
  10 行底板 / 10 行文字全挂在面板节点下。
- `typecheck.sh`：**本文件 0 错**；编辑器程序集 **0 错**（⇒ `Editor/BattleScene.cs` **照样编得过**，
  要改的只是**期望值/口径**，见 §4）。运行时程序集当前有错，**全部在 `Battle/SettingsPanel.cs`**
  （别的写手正写到一半，逐次读数见 §5）。
- **16:9 下逐位不变**：面板里每一件的世界位置、尺寸、以及断言读到的那些数（454.77 / 331.71 / 452.99 /
  539.07 / 95.77 / 707.302 / 748 / −1200 / 87 …）**与迁之前完全相同** —— 迁移是「换渲染路径」，不是「改版面」。
- ⚠️ **一处与配方不同**（理由与代价见 §6·1，请主对话裁）：`字`的**建件口**照配方用 `HudCanvas.MakeText`，
  但**位置**我用了**归一化锚点**（与图同一套），⛔ **没有**用 `PosPx`。
- ⚠️ **两条现存断言会变红**（§4·A/§4·B），其中一条是「节点换了宿主」的必然结果，另一条是
  **画布单位↔世界单位**在批处理那档宽高比下的算术结果（**画面其实是对的**）。
- ⚠️ **一处行为差**（不是本次引入的「错」，是**批次中间态**）：悬停那张卡（世界空间 `CardView`）
  现在排在整块 `PopUps` 画布**之下** —— 见 §6·4。

---

## 2. 改动清单（逐处「旧口（旧行号）→ 新口（现行号）」+ 子画布参数）

### 2·0 建出来的子画布

| 项 | 值 | 出处 |
|---|---|---|
| 名字 | **`CemeteryLogPanel Canvas`**（`BattleLogPanel.CanvasName`，`:322`） | 原版宿主 = `CemeteryLogPanel`（它**自己就是**一块 `Canvas`：`CV_2772`） |
| 层 / order | **`HudCanvas.SortingLayer.PopUps`(1054366423) / `−1`**（`LogSortingOrder`，`:324`） | `T09c` §一 那一行：`PopUps` / **−1** / 兄弟序 3/6 + `T09b` §A·2 #7（`CV_2772`） |
| `overrideSorting` | **true**（`EnsureSubCanvas` 体里设） | `T09b` §A·2 #7 |
| 父链 | **`HudCanvas.RootRect`（根 canvas）** —— `EnsureSubCanvas(null, …)` | `T09` §五·补⑨ 一·2 |
| `CanvasScaler` | **一个都不加**（`EnsureSubCanvas` 就没加） | `T09b` §五·1（原版全场景只有一个、在根上） |
| `RectTransform` | `anchor (0,0)-(1,1)` · `pivot 0.5` · `sizeDelta (0,0)`（铺满根画布） | `EnsureSubCanvas` 体 |
| **`localPosition.z`** | **没设（= 0，与父画布共面）** | ⛔ `T09` §五·补⑨ 一·3 明写「原版对应值主对话还没读 ⇒ **别猜**」；与批 1（`LoadingMenu Canvas` / `TooltipManager Canvas`）及同批 `BattleDoors Canvas` **同一先例** |
| 幂等 | **`ReuseOrMakeCanvas()`（`:750`）**：先 `HudCanvas.SubCanvas(name)` 找、没有才建 | ⚠️ `BattleLogPanel.Create` 在一次 `BattleScene.Run` 里**会被调两次**（生产 `BuildHud` + §A218 探针）⇒ 不幂等就留两块同名画布。同批 `BattleDoors.PairCanvas()` 是同一个做法 |

### 2·1 逐处换算（旧口 → 新口）

| # | 旧口（旧行号，`git show HEAD:` 那一版） | 新口（现行号） | 备注 |
|---|---|---|---|
| 1 | `ImageQuad.Create(root, CardArt.Solid(), (0,0,ZShade), 40f, center, "BattleLogShade")`（旧 `:533`） | **`HudCanvas.MakeImagePiece(_canvas, Solid(), "BattleLogShade", 0,0,1920,1080, ShadeColor)`（`:648`）** ＋ **`FillParent(_shade.Rect)`（`:652`）** | 压暗层是**整屏覆盖层** ⇒ 用 `FillParent`（判据 = `T09` §五·补⑩ 一 那张表那一行：⛔ 别写死 1920×1080） |
| 2 | `_shade.SetTint(ShadeColor)`（旧 `:537`） | **删** —— 颜色进建件口第 8 个形参（`:648`） | 同上表那条「`SetTint` → `Image.color`」 |
| 3 | `_shade.SetRenderQueue(OverlayQ)`（旧 `:538`） | **删** | 画布上的件**没有独立队列**（`T09` §五·补⑩ 一 那张表最后一行）；`OverlayQ` 常量**保留公开**（见 §2·2） |
| 4 | `panel.transform.SetParent(root, false)`（旧 `:543`，`root` = `HudRoot`） | **`SetParent(_canvas, false)`（`:661`）** | ⛔ 不挂 `hudRoot`（那是**反向补震**用的） |
| 5 | `MenuDraw.SetPxSize(panel.transform, PanelW, (PanelTopY01−PanelBotY01)×1080)`（旧 `:550`） | **删 → `HudCanvas.FillParent((RectTransform)panel.transform)`（`:673`）** | 🔴 **关卡 1**：`MenuDraw.SetPxSize` 内部是 `÷108`（世界单位）；uGUI 档要**真 px**。⛔ 面板节点必须是**铺满**的：子件的锚点是归一化的，只有父 rect = 1920×1080 那一块时「anchor = 设计 px ÷ 1920」才等于本文件一直用的绝对设计 px |
| 6 | —（新增） | **`HudCanvas.Add((RectTransform)panel.transform, ZPanel)`（`:675`）**，`ZPanel = −4.01f`（`:250`） | 面板节点是**压暗层的兄弟**，靠兄弟序排在它**之后**（画在上层）；⛔ 兄弟序 = 按 z 降序（`HudCanvas.Add`），所以 `ZPanel < ZShade` = 在后面 ✓ |
| 7 | `bgAt = LayoutSpace.ToWorld((PanelOpenX+BgCx)/1920, 1−BgCyTopPx/1080)`（旧 `:557`）＋ `ImageQuad.Create(_root, Solid(), (bgAt.x,bgAt.y,ZBg), Px(BgH), center, "LogBG")`（旧 `:558`） | **`HudCanvas.QuadFromTopPx(_root, Solid(), "LogBG", PanelOpenX+BgCx, BgCyTopPx, BgH, ZBg, BgColor)`（`:682`）** | 「左上原点 y 向下」px → canvas 局部；`hPx = BgH`（**不再 `Px()`**） |
| 8 | `bg.SetTint(BgColor)`（旧 `:564`）＋ `bg.SetRenderQueue(OverlayQ)`（旧 `:565`） | **删**（颜色进建件口；队列无意义） | —— |
| 9 | `at = ToWorld((PanelOpenX+RowsCx)/1920, cy01)`（旧 `:582`）＋ `ImageQuad.Create(_root, Ui(…neutral), (at.x,at.y,ZRowBg), Px(RowH), center, "LogRow"+i)`（旧 `:584`） | **`cyTop = (1−RowCenterY01(i)) × 1080`（`:707`）＋ `HudCanvas.QuadFromTopPx(_root, Ui(…neutral), "LogRow"+i, PanelOpenX+RowsCx, cyTop, RowH, ZRowBg, Color.white)`（`:710`）** | `cy01`（自底归一化 y）→ `cyTop`（自顶 px）：**同一条式子**（`ToWorld` 收的就是那个 y01） |
| 10 | `_rowBgs[i].SetRenderQueue(OverlayQ)`（旧 `:593`）＋ `_rowBgs = new ImageQuad[…]`（旧 `:577`） | **删 / `new HudPiece[…]`（`:701`）** | 见 §2·3 |
| 11 | `textAt = ToWorld(…, cy01)`（旧 `:600`）＋ `Label.Create(_root, "", (textAt.x,textAt.y,ZText), 3, color, (0,0.5), "LogRowText"+i)`（旧 `:602`）＋ `SetRenderQueue`（旧 `:605`） | **`HudCanvas.MakeText(_root, "", 0,0, 3, color, (0,0.5), ZText, "LogRowText"+i)`（`:726`）＋ `PlaceTextByTopPx(l, PanelOpenX+RowsCx−RowW/2+TextInsetPx, cyTop)`（`:728`）** | 🔴 **位置那两个数照配方是"设计 px"**（⛔ 不是世界单位）；锚点那一手见 §6·1 |
| 12 | `at = ToWorld(cxPx/1920, 1−cyPx/1080)`（旧 `:633`）＋ `ImageQuad.Create(_root, tex, (at.x,at.y,ZFrame), Px(hPx), center, …)`（旧 `:636`）＋ `SetRenderQueue`（旧 `:638`） | **`HudCanvas.QuadFromTopPx(_root, tex, "LogFrame_"+art, cxPx, cyPx, hPx, ZFrame, Color.white)`（`:804`）** | 🔴 那句 `ImageQuad.FitHeight(boxW, boxH, 贴图比例)`（内接，**四条的实绘尺寸**就靠它）**一个字没动**（`:803`）——`QuadFromTopPx` 与 `ImageQuad.Create` 都是「**按高定、宽由贴图比例来**」，逐字同义 |
| 13 | `MenuDraw.SetPxSize(go.transform, 1920, 1080)`（旧 `:523`，**外壳节点**） | **不动（`:609`）** | 🔴 **故意**：见 §3·B 与 §2·4 |
| 14 | `static float Px(float px) => px/108f`（旧 `:218`） | **删**（本文件不再有任何世界单位换算） | 关卡 1 的直接结果 |
| 15 | `BgColor = new Color(0,0.08f,0.01f,1f).linear`（旧 `:203`） | **`BgColor = new Color(0,0.08f,0.01f,1f)`（`:237`）** | 🔴 **判断、不是实读**：uGUI 的 `Image.color` 是**顶点色**（线性工程里按 sRGB 解释）⇒ 要喂**原版序列化的原值**；旧那条 `.linear` 是**材质**路要的。判据链与「没在真机比过色」都写在 `:226-236` |
| 16 | `PanelCenterTopPx` 属性（旧 `:108` 一带）＋ `PanelW` 由它给面板节点定高 | **`PanelCenterTopPx` 删**；`PanelW` → **`public`（`:62`）** | 面板节点不再有那条 rect（理由见 `:664-673`）；两个原版值一个没丢（653.7998 / 550.7653 都在注释与 `CardCenterPx` 里） |

### 2·2 保留不动的口（逐条说明为什么）

- **`OverlayQ = 4000`（`:267`，`public const`）** —— ⛔ **没删、没改值、可见性没动**。
  面板自己的件不再用它（见 §2·1 #3/#8/#10）；**唯一消费点仍是 `BattleDriver.ShowLogCard` 的
  `CardFan.SetCardQueue(_logCard, BattleLogPanel.OverlayQ + 1)`**（那张卡是**世界空间**的 `CardView`）。
  ⚠️ `Core/Tooltip.cs:112` 的注释引过这个数（只是说明两个场景不共存）—— **那边不用改**。
- **`ZHoverCard` / `CardBodyW/H` / `CardCenterPx` / `HoverCardLocalPos` / `RowCardLocalPos`** —— 全不动，
  **逐位同一个数**（理由写在 `HoverCardLocalPos` 的 doc `:86-96`）。
  ⚠️ 它们读的 `_root.position` 是**世界点**：旧值 = 「HudRoot 的世界位置 + 滑动位移」，
  新值 = 「画布中心的世界位置（= 0）+ 同一段滑动位移」⇒ **逐位相同**。
- **`RowAt` / `RowCenterWorld` / `LinkKeyAt` / `FindLinkProbe` / `RowAnchor`** —— 走路的世界坐标，
  适配层改造**逐字没动**（`HudPiece.WorldW/H` 走 `GetWorldCorners`，两种模式同一个判据）。

### 2·3 字段与读回口（类型改了、**签名全没变** ⇒ `Editor/BattleScene.cs` 照样编得过）

| 项 | 旧 | 新（行号） |
|---|---|---|
| `_bg` / `_rowBgs` / `_shade` | `ImageQuad` | **`HudPiece`**（`:329` / `:330` / `:332`） |
| `_frames`（四条边框） | `Dictionary<string, ImageQuad>` | **`Dictionary<string, HudPiece>`**（`:812`） |
| `RowBg(int)` | 返回 `ImageQuad` | **返回 `HudPiece`（`:825`）** —— 消费点 `log.RowBg(0).WorldW` / `.transform.position` 两样都有 ✓ |
| `RowsInFrontOfBg` | 比 `transform.localPosition.z` | **比 `HudPiece.OrderZ`（`:591`）** 🔴 上画布之后每个件的 `localPosition.z` **恒 0** ⇒ 老口会**恒判「行在底板后面」= 红**（画面其实是对的）；`OrderZ` 世界档逐位等于老那个 z、uGUI 档 = 它在画布里的排位档 |
| `FrameWorldW/H` | `q.WorldW/H` | **不变（`:831` / `:835`）** —— 见 §4·B 的注意事项 |
| `FrameCenterPx` / `BgCenterPx` | `LayoutSpace.ToPixel(...)` | **`LayoutSpace.ToDesignPixel(...)`（`:864` / `:876`）** —— `A990②`：这个数要去比**字面设计 px**，真逆是 `ToDesignPixel`。16:9 下两者只差 float 舍入（≤2.5e-4 px，容差 1.0）⇒ 断言照旧 |
| `RowCardLocalPos` | `LayoutSpace.ToPixel(pos).y` | **`LayoutSpace.PxY(pos.y)`（`:539`）** —— 只取 y（两个式子同值），顺手甩掉那条「x 斜率写死 108」的陷阱 |
| `FindLinkProbe` | `ToPixel` → `FromPixel` 往返 | **`ToDesignPixel` → `FromPixel`（`:558`）** —— 这一对才互逆 |
| 滑动 | `_root.localPosition.x`（世界单位） | **`_root.anchoredPosition.x`（设计 px）（`:976-983`）**，中间量 `_slideOffsetX` → **`_slidePx`**（`:905`） |
| `PanelLeftX` | 世界坐标反推 | **`PanelOpenX + _slidePx`（`:915`）** —— **同一个数**（拉开 87 / 收起 −1200，逐位） |
| 新增自检口 | — | **`SubCanvas`（`:345`）** · **`PanelRect`（`:349`）** · **`FrameSizePx`（`:846`）** |

### 2·4 ⑥① / ⑥③ 的两条（按任务单要求的「同批四件」）

- **① 摘注册表 / `ReanchorHud`：⛔ 无此账。** 现读（`grep -rn "ReanchorHud\|_hudImages\|_hudImageSpots\|_hudLabels" Battle/BattleDriver.cs`）：
  `ReanchorHud`（`:16137`）只走 `_hudLabels` + `_hudImages` + 几个**具名**件
  （`FitDeckPile` / `PlaceDeckLights` / `PlaceHandPlate` / board markers / backdrop / selector /
  `skillPanel` / `_replayBar` / `_unitChat`）—— **本面板一件都不在里面**（它自己在 `Build` 里建件、自己收着）。
  ⇒ **一个注册项都不用摘**（⑥① 那条对**这一扇**是空的）。
- **③ `RefreshLayout`：⛔ 本扇没有这个方法**（`grep -n "RefreshLayout" Battle/BattleLogPanel.cs` = 0 命中）
  ⇒ 无「重算世界位置」可退。
- **④ 要改的断言** → §4；**②读回口** → §2·3。

---

## 3. 父链对照（我们 vs 原版）—— ⛔ 本次**没有**把嵌套倒过来，只如实记差异

### A. 原版（判据 → `T09c` §一 + `T09b` §A·3）

```
BattleHud                       （纯 Transform）
└ Canvas  (根, CanvasScaler 挂这儿)
  └ BackCanvas                        （rm2 / overrideSorting false / Default / 0）
    └ Safe area BackCanvas
      └ LeftArea                      （RT_3042，sizeDelta (0,0) ⇒ 铺满）
        └ CemeteryLogPanel            ★【本身就是一块 Canvas】CV_2772: rm2 / overrideSorting TRUE
          │                           排序层 PopUps / order −1 / 兄弟序 3/6（父 LeftArea）
          ├ shade                     （7020×4544，黑 55%；父 = 面板自己）
          ├ BG   · Frame(Top/Bottom/Left/Right) · CemeteryActions(10×CemeterySliderUI→ActionText)
          └ CemeteryGroup/CardUI (1)  （悬停那张卡）
```
面板自己的 rect = `m_SizeDelta (794.06897, 0)` + **竖直 stretch 锚 y 0.18734702–0.79271716**
⇒ 高 653.7998；**左缘锚在父的左边界**（`anchor x = 0` + `anchoredPosition.x` 87 / −1200）。

### B. 我们（改完）

```
HudRoot                                   （纯 Transform，= 原版 BattleHud 那一级）
├ BattleLogPanel                          ★【我们的外壳节点，**故意留在世界空间**】
├ BattleHud Canvas                        （根 canvas，批 1 建的）
│ └ CemeteryLogPanel Canvas               （★本扇子画布：PopUps / −1 / overrideSorting true）
│   ├ BattleLogShade                      （全屏 Image；FillParent；**不跟着面板滑**）
│   └ CemeteryLogPanel                    （铺满子画布；**滑动的是它**）
│     ├ LogBG · LogFrame_{TOP,Bottom,Left,Right} · LogRow0..9 · LogRowText0..9
```

### C. 四处**如实标**的差异（⛔ 都不是「顺手倒过来」的活，主对话要裁就看这一节）

1. **我们的画布 = 子画布，原版的画布 = 面板节点本身**（`CV_2772` 挂在 `CemeteryLogPanel` 上）。
   我们做不到「一个节点既是 Canvas 又装内容」——`Canvas` 会接管自己的 `RectTransform`，
   而我们的面板节点必须是**铺满**的（`FillParent`，理由 §2·1 #5）。⇒ 拆成「子画布（层/order）+ 全屏面板节点」。
2. **面板节点不再携带原版那条 rect（794.06897 × 653.7998）** —— 我们这边那条 rect **本来就没人用**
   （底板/边框/行全是**绝对屏幕 px**，`PanelOpenX` 早烘进常量里了），面板节点只承担「整块滑动」。
   ⇒ 现在**本文件里没有任何一个节点**是那个矩形；`PanelW` 已转 `public`（`:62`）**只当判据值**。
   ⚠️ 若主对话要它作为「可裁剪的面板框」存在，那是另加一个矩形容器节点的活（5 行），本笔没做。
3. **压暗层的位置**：原版 `shade` 是**面板的子件**（会跟着滑）；我们的是**面板节点的兄弟**（**不滑**）
   —— 这是**迁移前就有的**行为（旧代码把它建在 `_root` 之外），本次**照旧**、没改。
4. **坐标口径**：原版面板的内容是「**面板局部 px** + 面板左缘锚在屏幕左边（`LeftArea` 左边界）」；
   我们是「**绝对屏幕设计 px**（`PanelOpenX` 已烘进每个常量）+ 画布 1920 参考宽」。
   ⇒ 屏幕越宽（21:9 等），**原版的面板会一直贴着左屏边**，我们的是「1920 参考宽里靠左 87 px」
   （21:9 下 ≈ 屏左 407 px）。⚠️ 这是**批次级的口径**（不是本扇特有的），本笔**没动**、⛔ 也没法在本文件里单方面改。

---

## 4. 要改的断言（逐条；`Editor/BattleScene.cs` 由主对话改）

> 行号按**写作时的现读**（该文件正被主对话改，行号会漂；以**引文**为准）。

### A. 🔴 必红（节点换了宿主）

| 位置（引文） | 现在 | 该改成 |
|---|---|---|
| `:16475` `box(a218root.transform.Find("CemeteryLogPanel"), "日志面板 `CemeteryLogPanel`", 794.1f, 653.7998f, …)` | **永远 null**（面板节点已挂到子画布下、不在 `a218root` 里）⇒ `（前提）…没建出来` 红 | 用新口取件：**`log.PanelRect`（新，`:349`）** 或 `log.SubCanvas.transform.Find("CemeteryLogPanel")`；**期望值 = 屏矩形 1920×1080（px）**（面板节点现在是 `FillParent`）。⚠️ 那个 `box(...)` 助手（`:16423-16435`）的三条前提**对本节点不成立**：① 它比的是 `LayoutSpace.Px(wPx)`（世界单位）② 它断 `anchorMin == anchorMax && pivot == (0.5,0.5)` —— 面板节点是 **stretch 锚**（`FillParent`）。⇒ 需要一条 **px 版**的助手（比 `rt.rect.width/height` 与字面 px，如 `:16432` 那样但去掉 `LayoutSpace.Px`） |

### B. 🔴 必红（画布单位 ↔ 世界单位在**批处理那档宽高比**下的算术）

| 位置 | 现在读到 | 为什么 |
|---|---|---|
| `:3895` `float fTh = log.FrameWorldH("…frame_TOP") * 108f;` → 期望 **64.995** | **48.75**（= 64.995 × 0.75） | 见下面的推导 |
| `:3897` 同上 Bottom → 期望 **46.795** | **35.10** | 同上 |
| `:3899` 同上 Left → 期望 **571.236** | **428.43** | 同上 |
| `:3901` 同上 Right → 期望 **578.654** | **434.0** | 同上 |

**推导（为什么「画面是对的、数却是 0.75 倍」）**：批处理里 **`Screen = 640×480`（4:3）**
（`LayoutSpace.IsInsideScreen` 的 doc 记着实测），而 `BattleScene` 把 **`cam.aspect` 钉死在 `DesignAspect`(16:9)**
⇒ `CanvasScaler(1920×1080, Expand)` 的 `scaleFactor = min(640/1920, 480/1080) = 0.3333`
⇒ 画布 rect = **1920 × 1440 画布单位**，而它要铺在相机的 `17.7778 × 10` **世界单位**上
⇒ **两个轴的「世界单位/画布单位」不一样**：x = 1/108（= 1920 单位 ↔ 17.7778 世界单位，**与设计 px 恰好同值**）、
y = **1/144**（= 1440 单位 ↔ 10 世界单位）
⇒ 一条 `sizeDelta.y = h` 的边框，`WorldH × 108` = `h × 108/144` = **0.75 h**。
⚠️ **x 那一半在批处理里碰巧还是 108** ⇒ 所以**宽度那 4 条现在反而是绿的**（`:3894/:3896/:3898/:3900`），
**只有高度那 4 条红** —— 这是「批处理把相机 aspect 钉成 16:9 而视口是 4:3」造成的**各向异性**，
在**真窗口**里没这回事（那里两个轴一起变 0.75）。
✅ **推荐的改法（一劳永逸、且与环境无关）**：这 4 条**不读世界尺寸**，改读 **`log.FrameSizePx(art)`**
（**本笔新加的口，`:846`**，= `HudCanvas.SizePx` ⟵ `RectTransform.sizeDelta` ⇒ 逐位等于我们写进去的
原版实绘值）—— 于是 8 个数一次全换成精确值的写法：
`FrameSizePx("…TOP") = (707.302, 64.995)` · `Bottom = (706.700, 46.795)` ·
`Left = (93.147, 571.236)` · `Right = (62.814, 578.654)`（**期望字面量一个都不用改**）。
⚠️ 宽度那 4 条现在绿是**偶然**（见上），**建议一并换成 `FrameSizePx`**（同一条判据、同一个口径）。

### C. 🟡 现在绿、但口径该换（`A990②`）

| 位置 | 现状 | 建议 |
|---|---|---|
| `:3994` `var row0Px = LayoutSpace.ToPixel(log.RowBg(0).transform.position);`（期望 454.77 / 331.71） | 16:9 绿（y 逐位同；x 在 16:9 只差 float 舍入） | 换成 **`LayoutSpace.ToDesignPixel`** —— 它比的是**字面设计 px** ⇒ 非 16:9 下才成立（同一族：`:4002` `ToPixel(log.RowAnchor(0))` 期望 95.77） |
| `:3953-3957` `rb0.WorldW * EndPanel.PxPerUnit ≈ 748` | 批处理里绿（x 那一轴 = 108）· **真 4:3 窗口里会是 561** | 换成 **`HudCanvas.SizePx(log.RowBg(0).Rect).x == 748`**（`HudPiece.Rect` 是公开口） |
| `:3969-3970` 那条断言的**文案**写「`{…} 世界单位 > 0.01`」 | 值仍成立（阈值极松，px 更大 ⇒ 更绿） | 文案改「本档单位（uGUI 档 = 设计 px）」—— `RowTextWidth` 的 doc 我已同步（`:590` 一带） |

### D. ✅ 逐条核过「**不用改**」的（列全，免得主对话重复核）

`:3881`（`RowsInFrontOfBg`，已换口，仍绿）· `:3894/:3896/:3898/:3900`（四条的**宽**，见 §4·B 的注意）·
`:3912`（`FilledRows`）· `:3914-3918`（`RowHeightPx` / `RowTotal`）· `:3926-3952`（`RowBgName`/`RowText`/`RowArt`，
走 `HudPiece.Texture` ✓）· `:3958-3967`（`RowText(0)`）· `:3975-3984`（`PanelLeftX`：−1200 / 87，**逐位**）·
`:3998-4001`（`BgCenterPx`，已换 `ToDesignPixel`）· `:4015-4035`（`FrameCenterPx` 四条 + 「比面板中心偏左 16.6」）·
`:4040-4057`（链接 / 悬停弹卡：`Label.LinkAt` 走 TMP 自己的 `FindIntersectingLink`，画布下的 `TMP_UGUI` 同一个口）·
`:4078-4117`（`RowAt` / `RowCenterWorld` / `RowCardLocalPos` / `SelectRow`）·
**`:16470-16473`（A218 box ①「日志面板根 = 1920×1080」）** ← 这条**保持绿**，因为**外壳节点故意没迁**（见 §2·1 #13）。

### E. 建议**新增**的断言（`T09` §五·补⑥ 那条「层本身对不对才是自证」）

本笔新加了三个口（`SubCanvas` `:345` / `PanelRect` `:349` / `FrameSizePx` `:846`），可直接断：
1. `log.SubCanvas != null` 且 `overrideSorting == true`、`sortingLayerID == HudCanvas.SortingLayer.PopUps`、
   `sortingOrder == LogSortingOrder(−1)`；
2. **子画布上不许有 `CanvasScaler`**（`log.SubCanvas.GetComponent<CanvasScaler>() == null`）；
3. **面板里的件确实在画布上**：`log.RowBg(0).IsUgui == true`（`HudPiece.IsUgui` 是公开口）＋
   每个件都在那棵画布子树里（`log.PanelRect.IsChildOf(log.SubCanvas.transform)`）；
4. **兄弟序**：压暗层是子画布的第 0 个子件、面板节点在它之后（＝面板内容画在压暗层**之上**）。
   ⚠️ 这一条是**本扇唯一**能抓「压暗层盖住内容 / 内容被压暗层吃掉」的自证。

---

## 5. 类型检查

命令（按任务单，带独立 `TMPDIR`）：`TMPDIR=/tmp/wf_log bash d:/4/Unity/工具/typecheck.sh`

| 次 | 运行时 | 编辑器 | 备注 |
|---|---|---|---|
| 1 | **0** | **0** | 改完 `Build` 后 |
| 2 | 26 | 0 | 报错**全在 `Battle/SettingsPanel.cs`**（别的写手正写到一半：`CS0029 ImageQuad→HudPiece` 一族）⇒ **不是我的活**，按任务单**没去动别人的文件** |
| 3（隔 45 s 重跑） | 25 | 0 | 同上；`grep -c "BattleLogPanel" <输出>` = **0** |
| 4（加完 `FrameSizePx`） | 19 | 0 | 同上 |
| 5 | 13 | 0 | 同上 |
| 6（收尾前最后一跑） | **1** | **0** | 唯一那条仍在 `Battle/SettingsPanel.cs`（`CS0104 Object` 不明确）⇒ 那位写手正在收尾；**本文件始终 0 错** |

**结论**：**本文件 0 错**；**编辑器程序集 0 错** ⇒ `Editor/BattleScene.cs` 现在**编得过**
（§4 那些是**值/口径**的改动，不是编译错误，主对话改起来不会连带别的宿主）。

---

## 6. 顺手发现的（⛔ 都没自己改）

### 6·1 🔴 `HudCanvas.PosPx` 与 `QuadFromTopPx` **不是同一个锚定口径**（本笔按「统一锚定」办，请裁）

- **事实**：`PosPx(xPx,yPxFromTop)` = `(xPx − 960, 540 − yPxFromTop)`，而 `MakeText`/`Label.CreateUgui`
  把它写进**中心锚**的 `localPosition` ⇒ **相对父中心**；`QuadFromTopPx`/`MakeImage`/`MakeAnchored` 用的是
  **归一化锚点**（`anchor = 设计 px ÷ 1920 / 1080`）⇒ **按父 rect 摊开**。
- **两者只在父 rect = 1920×1080 时逐位相同**。而 `CanvasScaler(Expand)` 下画布 rect 只在 16:9 是那个尺寸：
  4:3 ⇒ 1920×**1440** · 16:10 ⇒ 1920×**1200**（**批处理现在就是 1920×1440**，见 §4·B）。
- **代价（逐值算过）**：同一行里「图用锚点、字用中心锚」⇒ 字会**离开它那一行的底板**：
  **16:10 最多错开 29 px**（行高才 43.134）· **4:3 最多 135 px** · 批处理里 row0 差 **69 画布单位
  ≈ 23 屏 px**。⇒ 这正是 `T09` §五·补⑦ 那条「**只换半边比不换更坏**」在同一扇面板内部的翻版。
- **本笔怎么办的**：**口子照配方（`MakeText`）**，只是**建完把锚点摆成和图同一套**
  （`PlaceTextByTopPx`，`:771`；3 行、带完整推导的注释）⇒ 16:9 逐位不变、非 16:9 下「字还在自己那一行里」。
  ⚠️ **这是本笔唯一偏离配方的一处**（配方那句是「位置 → `HudCanvas.PosPx`」）。**请主对话裁**：
  要统一成哪一种口径（改 `PosPx` 让它是锚定式、或反过来把图也改成中心锚）—— 两种都能让我这一处**更简单**。
  ⚠️ 同批 `EndPanel`/`BattleDoors` 那一位写手**大概率也会碰到同一条**（他们报告里的「位置」那几行值得对一眼）。

### 6·2 🔴 批处理是 **4:3 视口 + 16:9 相机** ⇒ 「世界尺寸」读数在两个轴上不同系数

见 §4·B 的推导。**影响面不只本扇**：凡是「拿 `HudPiece.World* / *WorldH` 去比字面 px」的断言
（批 1 的 HUD 图族、本扇的 4 条边框、`DrawHud` 那一族）**在批处理里都可能是 0.75 倍**。
🔑 **判据**：**比设计 px 就用 `HudCanvas.SizePx(RectTransform)` / `HudPiece.Rect`，别用世界尺寸**。
（本笔已给本扇加了 `FrameSizePx` 直接支持这条。）

### 6·3 🔴 悬停那张卡（世界空间）现在排在**整块 `PopUps` 画布之下**

- **事实**：`CardView` 是**世界空间**的 mesh、`CardFan.SetCardQueue` 给它 **4001**，而它的 `Renderer` 的
  **排序层仍是 `Default`**。画布走**排序层**优先 ⇒ `PopUps`(1054366423) > `Default`(0)
  ⇒ **压暗层与面板现在都画在那张卡之上**。
- **可见后果（两个，都算得出来）**：① 那张卡被 **55% 黑**压暗（它整块都在压暗范围内）；
  ② 卡体 **226×359.8**、中心 `(33.96, 530.28)`（自顶）⇒ 跨 `x ∈ [−79.0, 147.0]`，而面板拉开后左缘在 **87**
  ⇒ **右边约 60 px 宽、360 px 高的一条**被面板盖住。
- **迁移前是什么样**：卡 4001 > 面板/压暗 4000 ⇒ 卡**压在两者之上**（这是原版的样子：原版那张
  `CemeteryGroup/CardUI (1)` **就在面板那块 Canvas 里**）。
- **候选修法（都在别的文件里，⛔ 我没动）**：
  ① 把那张卡也搬进画布 —— **本笔验过这条路会坏**：`CardView.SetPose` 是
     `transform.localPosition = pos`，而 `HoverCardLocalPos` 交的是**世界单位** ⇒ 挂进画布会差 **108 倍**
     （**这正是我把外壳节点留在世界空间的原因**，见 §3·B）。
  ② 给那张卡的 `Renderer` 设 **`sortingLayerID = PopUps` + `sortingOrder ≥ 0`**（同层内就压在子画布的 −1 之上）。
     ⚠️ **风险如实标**：`T09` §五·补⑨ 一·3 自己写着「排序层 vs `renderQueue` **谁先比**，我们**没实机验过**」
     —— 而那张卡同时挂着 queue 4001 ⇒ **要真机验一次再定**。
  ③ 或者等**卡体那一批**（`T09` 第 5/6 步，`Physics2D` 那条）一起收口。

### 6·4 🟡 本扇现在**压在**还没迁的 `PopUps/0` 三扇之上（批次中间态，**不是缺陷**）

`PopUps` 组里本扇是 **order −1（最低）**，按 `T09` §四·补 的顺序（1 → 0 → −1）本应**最后**迁。
本批五扇**并行**在做（设置 / 门 / 结算 / 错误条 / 本扇）⇒ **在其余四扇落地之前**，
本扇会盖在「还留在世界空间、队列 4000」的设置窗 / 开门 / 结算之上。
⇒ **请确认其余四扇同批落地**（这也是 `T09` §五·补⑩ 四 那条「一批做完之前别插队」）。

### 6·5 🟡 面板的**字**在 16:9 下就比迁之前小 **7.4%**

`Label` 的 uGUI 档把「一个 `scale` 档」定成 **7 px**（`Label.cs:294` 的 `CapPxOfScale`），
而世界档是 `7/100` 世界单位 = **7.56 px**。本扇行文字用 `scale = 3` ⇒ uGUI 档 **21 px** vs 旧 **22.68 px**。
*（= `Label` 那边上游的取整口径，不是本扇能修的；**如实标**，供主对话判断要不要改成 `7.56`。）*

### 6·6 🟡 `HudCanvas.MakeImage` 会把件**登记进 z 账**；同一个件 `Add` 两次会同件占两行账

本笔给压暗层改档用的是 **`HudCanvas.Reorder`**（`:655`，先删后插）而**不是** `Add` ——
`Group.Add` 不去重（`HudCanvas.cs:296-304`）。⚠️ 下一扇面板（或后面拆 `ReanchorHud` 那一步）改 z 时请照这个先例。

### 6·7 🟢 本扇**没有**可点件，所以与「输入」两条路不冲突（顺带核过）

面板里的图与字（**16 张图** = 压暗层 1 + 底板 1 + 四条边框 + 10 行底板；**10 块字**）的
`raycastTarget` 全是 **false**（`MakeAnchored` / `MakeImage` / `TMP_UGUI` 的出厂值），
且 `EventSystem` 本就**没建** ⇒ 子画布上**不会**有 uGUI 射线抢输入；「点面板外关面板 / 点行弹卡」
仍走手搓那条（`BattleDriver.HandleBattleLog` → `TryClickLogRow` → `RowAt`）。⇒ **不存在双触发**。

---

## 7. 没查清 / 没做的（逐条写「查了哪些文件 / 哪些词、结果是什么」）

1. **色**（`BgColor` 那条 `.linear` 去掉、`ShadeColor` 不变）**没在真机比过** —— 现行口径⛔ 不许起 player、
   ⛔ 不跑 `*.Run`。判据链写在 `:226-236`（原版序列化原值 + uGUI 顶点色的 sRGB 约定 + 旧材质路为什么要 `.linear`）。
   **要收口就得在统一验收时截图与原版并排比一次。**
2. **层 vs 队列谁先比**（决定 §6·3 那个候选修法 ② 成不成立）—— `T09b` §A·0·3 只给了机制描述
   （层 → order → 兄弟序），**没有实机证据**；本笔也没验（同一口径）。记在 §6·3。
3. **`Battle/SettingsPanel.cs` 的那些编译错** —— 逐次读数（26 → 25 → 19 → 13 → **1**）**全部**指向那一个文件
   （别的写手正在收尾），按任务单 **⛔ 没动**。**本文件的报错数始终是 0**（`grep -c "BattleLogPanel" <输出>` = 0）。
4. **面板那条原版 rect（794.06897 × 653.7998）现在没有任何节点承载** —— 是**有意**的（§3·C·2）。
   本笔没建「只装 rect 的空节点」（那是**死件**，且现状没人消费它）。若主对话要它（裁剪 / 命中框），说一声即可。
5. **`PopupAnchor` / 事件系统 / `GraphicRaycaster` 的实际收事件** —— 本扇**零可点件**，本笔**没验**
   （`T09` 第 4 步的活；`EventSystem` 按 §五·补③ 三·3 **本轮不建**）。
6. **`OvertimeSplashText` / `TipText` / 加时 splash 那一族**、**`AspectFiller`**、**手牌与场上卡** ——
   本笔**一件没碰**（各有各的批次；`T09` §五·补⑧ 的边界）。
7. **没查**：`BattleDriver` 里那三处 `Editor/BattleScene.cs:20203/20251` 的 `ClickLogRowForTest` 那条用例
   是否受画布影响 —— 它走的是 `RowAt(world)` + `SelectRow` + `ShowLogCard`（**都不是渲染路径**）⇒
   **读代码判「不受影响」**，⛔ **没跑自检**（现行口径）。
8. **白名单外一律没动**：`Editor/BattleScene.cs` · `Battle/HudCanvas.cs` · `Battle/HudPiece.cs` ·
   `Battle/BattleDriver.cs` · `Battle/Label.cs` · `Core/Tooltip.cs` · `Core/TmpFont.cs` · 别的面板 ·
   `CLAUDE.md` / `项目任务.md` / 任何 `任务总表.md` · git。
