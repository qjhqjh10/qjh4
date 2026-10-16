# T09 批 2（`PopUps` 组）· `EndPanel` + `BattleDoors` 迁 uGUI —— 执行代理报告

> **执行者**：动手代理（全权限子代理）· **日期** 2026-10-16 · **白名单** = `Battle/EndPanel.cs` + `Battle/BattleDoors.cs`（这两扇是一个单位）。
> **口径来源**：`资料/任务线/战斗线/任务/T09_整层换uGUI.md` **§五·补⑨ / §五·补⑩** ·
> `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` **§一** · `T09b_uGUI拦路判据.md` **§一**。
> ⛔ 本报告**只列事实与判据**；`Editor/BattleScene.cs` 的断言改动**留给主对话**（红线）。

---

## 1. 结论

**做完了**：这一对（结算面板 + 开门）里的**每一件图与字都建在子画布上**，⛔ **一件都没留在世界空间**。
- 子画布 = **`BattleDoors Canvas`**（原版宿主 `BattleDoors/Canvas`：**`PopUps` 层 / order 0 / `overrideSorting=true`**），
  挂在**根 canvas**（`BattleHud Canvas`）下 —— 由 `BattleDoors.PairCanvas()` 建/取（**幂等**）。
- `typecheck.sh`：**运行时 0 错 · 编辑器 0 错**（命令与两次读数见 §5）。
- ⚠️ **两处如实标的差异/缺口**（都不是「不做」，都记在 §7）：**父子链与原版相反**（照旧、本次不翻）·
  **子画布在父画布平面内的 z 没设**（原版值主对话没读到，⛔ 不猜 ⇒ 留 0，与批 1 先例一致）。
- 🔴 **有两条现存断言会变红，而且红得有道理**（见 §4·B）—— 它们拿**世界 z** 当「谁压谁」的判据，
  而这一对已经不在世界 z 这条排序轴上了。

---

## 2. 改动清单（逐处「旧口（旧行号）→ 新口」+ 子画布参数）

### 2·0 建出来的子画布（判据 → `T09c` §一 那两行）

| 项 | 值 | 出处 |
|---|---|---|
| 名字 | **`BattleDoors Canvas`** | 原版宿主 = `BattleDoors/Canvas`（GO `Canvas_1091`）。⚠️ 不叫裸 `Canvas`（那是原版 GO 自己的名字） |
| 层 / order | **`HudCanvas.SortingLayer.PopUps`(1054366423) / 0** | `T09c` §一 的 `BattleDoors` 那一行 + `T09b` §A·2 #4 |
| `overrideSorting` | **true**（`EnsureSubCanvas` 里设） | `T09b` §A·2 #4 |
| 父链 | **`HudCanvas.RootRect`（根 canvas）** —— `EnsureSubCanvas(null, …)` | `T09` §五·补⑨ 一·2（子画布要**继承根 canvas 的 `CanvasScaler`**） |
| `CanvasScaler` | **一个都不加**（`EnsureSubCanvas` 就没加） | `T09b` §五·1（原版全场景只有一个、在根上） |
| 兄弟序 | 它在 `RootRect` 下建得**很早**（`BuildHud` 里 `_endPanel = EndPanel.Create(root)`，早于 `BuildHudExtras`）<br>⚠️ **具体位序现在不准说死** —— `PopUps` 组另外几扇（战报/错误条）**同期也在迁**，谁先建谁在前 | ⚠️ 与别的 `PopUps/0` 画布的先后见 §6·7 |
| `RectTransform` | `anchor (0,0)-(1,1)` · `pivot 0.5` · `sizeDelta (0,0)`（铺满根画布） | `EnsureSubCanvas` 体 |
| **`localPosition.z`** | **没设（= 0，与父画布共面）** | ⛔ `T09` §五·补⑨ 一·3 明写「原版对应值主对话还没读 ⇒ **别猜**」；与**批 1** 的 `LoadingMenu Canvas` / `TooltipManager Canvas` 同先例 |

### 2·1 `Battle/BattleDoors.cs`

| # | 旧口（旧行号） | 新口（现行号） | 备注 |
|---|---|---|---|
| 1 | —（新增） | **`CanvasName = "BattleDoors Canvas"`（:186）** + **`PairCanvas()`（:199-211）** | 幂等：`HudCanvas.SubCanvas(name)` 先找，没有才 `EnsureSubCanvas` |
| 2 | `MenuDraw.SetPxSize(go.transform, DoorsBoxW, DoorsBoxH)`（旧 :176） | **`HudCanvas.SetPxSize((RectTransform)go.transform, DoorsBoxW, DoorsBoxH)`（:229）** | 值**逐字没动**（1920.1199×1118.981），换的是**单位**（真 px，⛔ 不是 ÷108） |
| 3 | `go.transform.SetParent(parent, false)`（旧 :175） | **`SetParent(host, false)`（:224-228）** | `host` = 父件已在画布上就用父件（生产路 = `EndPanel` 根）、否则用 `PairCanvas()`（独立建/探针） |
| 4 | `go.transform.localScale = one * DoorsScale`（旧 :185） | **不变（:238）** | `A2105②` 那一颗，**照旧** |
| 5 | `MenuDraw.SetPxSize(go.transform, 1920, 1080)`（`video_player`，旧 :202） | **`HudCanvas.SetPxSize(…, ScreenWidthPx, ScreenHeightPx)`（:255）** | 同上，换成真 px |
| 6 | `_screen = ImageQuad.Create(transform, _rt, EndPanel.Pos(954,476,ZVideo), 1080/108, …, "video_screen")`（旧 :249-252） | **`RawImage` 节点（:311-323）**：中心锚 + `anchoredPosition (−6,+64)` + `sizeDelta (1920,1080)` + `texture = _rt` + `Add(…, ZVideo)` | 🔴 **不能用 `Image`**：`Image` 要 `Sprite`，而 `Sprite.Create` 只吃 **`Texture2D`**，我们的源是 **`RenderTexture`** ⇒ **API 上不成立**（⛔ 不是「没试」）。✅ 原版那一件**本来就是 `RawImage`**（本文件头那段） |
| 7 | `_screen.SetAspect(1920/1080)`（旧 :255） | **删（:308-310 注释说明）** | 那一句是补偿「旧 quad 按 **RT** 的 3840/1080 定宽」；uGUI 的 `sizeDelta` 是**显式写死**的 ⇒ 不需要 |
| 8 | `_screen.SetMaterial(_mat)`（旧 :266） | **`_screen.material = _mat`（:334）** | 自建 shader `CardPresentation/Video Split Alpha` **一个字没动**（它有 `[PerRendererData] _MainTex` + `_Color` × `COLOR` ⇒ 正好是 uGUI 那套约定） |
| 9 | `_screen.To…` 读回口（`ScreenVisible` / `Stop` / `Play` / `DropFrame`） | **同名前缀的 `RawImage` 口（:353 / :400 / …）** | `gameObject.SetActive` / `DestroyImmediate` 语义不变 |
| 10 | `ScreenWorldSize => _screen.WorldW/WorldH`（旧 :394） | **`RectTransform.GetWorldCorners()`（:465-471）** | 判据同 `HudPiece.WorldW`：⛔ **不是** `sizeDelta × 常数`（那个换算随分辨率变） |
| 11 | `_screen` 字段类型 `ImageQuad`（旧 :100） | **`RawImage _screen` + `RectTransform _screenRt`（:113-114）** | |
| 12 | 文件头「分层」那段写「压暗层 **z = 0**（……别去动它）」（旧 :87） | **就地订正（:86-99）** | 那句**早已过期**（`EndPanel` 的 dim 早已是 `ZDim = −0.50`）；同时补上「uGUI 档 z = 兄弟序键」的语义 |

### 2·2 `Battle/EndPanel.cs`

| # | 旧口（旧行号） | 新口（现行号） | 备注 |
|---|---|---|---|
| 1 | `go.transform.SetParent(parent, false)` + `MenuDraw.SetPxSize(go.transform, 1920, 1080)`（旧 :176-177） | **挂 `BattleDoors.PairCanvas()` + `HudCanvas.FillParent(rt)`（:182-186）** | **更贴原版判据**：原版 `EndBattlePanel` 实读就是 `anchor (0,0)-(1,1)` + `sizeDelta (0,0)`（= stretch，**不是**定死 1920×1080）；`Expand` 档下 21:9 也盖得满。⚠️ **`parent` 参数本件不再用**（先例 = `BuildLoadingCover(root)`） |
| 2 | `ImageQuad.Create(_root, SolidTex(…), (0,0,ZDim), 10.5, …, "dim")` + `SetTint`（旧 :199-202） | **`HudCanvas.MakeImagePiece(_root, CardArt.Solid(), "dim", …)`（:213-217）+ `FillParent` + `Reorder(…, ZDim)`（:218-219）** | 颜色仍是 `(0,0,0,0.97)`（**逐字没动**）。🔴 那张**按可见区宽高比现造**的临时贴图 `SolidTex`（旧 :543-557）**整段退场** —— 它是为绕过「`ImageQuad` 的宽只能由贴图比例推」才有的 |
| 3 | `MenuDraw.SetPxSize(contentGo.transform, 1920, 1080)`（旧 :214） | **`HudCanvas.FillParent`（:233）+ `Add(…, ZContent)`（:235）** | `content` 是我们自己的中间层（原版没有），判据照旧 = 屏矩形 |
| 4 | `Label.Create(_content, "", Content(960,260), 6, …, "end_title")`（旧 :216） | **`HudCanvas.MakeText(_content, "", 0f, 280f, 6, …, ZContent, "end_title")`（:270）** | 逐值换算：`Content(960,260)` = 世界 (0,+2.5926) = **屏心上方 280 px** |
| 5 | `Label.Create(…, Content(960,730), 2, …)`（旧 :217-218） | **`MakeText(_content, "", 0f, −190f, 2, …, ZContent, "end_sub")`（:272）** | `Content(960,730)` = **屏心下方 190 px** |
| 6 | `MenuDraw.SetPxSize(_skullRow, 648.1, 52.4)`（旧 :234） | **`HudCanvas.SetPxSize(rowRt, 648.1, 52.4)` + `rowRt.anchoredPosition = PosPx(960,790)`（:293-294）+ `Add(…, ZContent)`（:295）** | `PosPx(960,790)` = **(0, −250)**；`SetPxSize` 只管锚点/枢轴/尺寸，**不管位置**（故须显式定位置） |
| 7 | `ImageQuad.Create(_skullRow, tex, Content(960,790), U(52.4f), …, "skull_plate")` + `if (…!=null) SetAspect(648.1/52.4)`（旧 :235-243） | **`ImageInParent(_skullRow, tex, "skull_plate", 0, 0, 648.1f, 52.4f, ZContent)`（:306-307）** | 「先按贴图比例建、再 `SetAspect` 盖掉」**折成一个显式 `sizeDelta`** ⇒ 最终矩形**逐位不变**（648.1×52.4） |
| 8 | `ImageQuad.Create(_skullRow, tex, Content(x,790), U(71.2f), …)`（旧 :248-249，`x = 960+(i−1)·step`） | **`ImageInParent(_skullRow, tex, "skull_"+i, dx, 0, skullW, 71.2f, ZContent)`（:319-320）** | `dx = (i−1)·step`（相对**骷髅行中心**——那一行自己就在 960 上，故与旧的屏幕绝对偏移同值）；`skullW = 71.2 × 贴图宽高比`（旧 `ImageQuad` 是「按高定、宽由比例来」，uGUI 的 `PA=0` 会**拉满** ⇒ 宽必须自己算，否则画出来是压扁的方块） |
| 9 | `_doors = BattleDoors.Create(_root);`（旧 :270） | **同（:348）+ `HudCanvas.Add((RectTransform)_doors.transform, ZVideo)`（:349）** | **z 记账**：`_root` 的三个子件按 z 降序 = 兄弟序 ⇒ `[dim(−0.50), doors(−0.55), content(−0.60)]` ⇒ 画出来**压暗在底 / 视频居中 / 内容在最上** |
| 10 | `ImageQuad[] SkullQuads` / `ImageQuad _dim,_skullPlate` / `ImageQuad[] _skulls`（旧 :66,71,136,138,139） | **`HudPiece` 系（:84, 160-163）** | `HudPiece` 两种模式的 `WorldW/WorldH` **都返回世界单位** ⇒ 自检那两条读回**一个字不用改** |
| 11 | 私有 `Content(px,py)` / `U(px)`（旧 :50-55） | **删（:60-71 的注释说明去向）** | uGUI 档直接给 px |
| 12 | `_title.SetText(...)` / `_sub.SetText(...)`（旧 :319, 342） | **加了空判（:399, 428）** | `HudCanvas.MakeText`（= `Label.CreateUgui`）拿不到 TMP 字体时**返回 `null`**（出声、⛔ 不退化成点阵）；原来 `Label.Create` 恒非 null |
| 13 | —（新增） | **`ImageInParent(...)`（:551-563）** | 「**相对父中心**的设计 px」建图那一支，见 §6·1 |
| 14 | 文件头（旧 :1-31） | **补了一段 `T09` 批 2 的迁移说明（:32-52）** | 含单位/z 语义/退场清单/保留清单 |

**没动的**（有意）：
- `EndPanel.Pos(px,py,z)` 与 `PxPerUnit` —— `CardDisplayWindow` / `MultiCardDisplay` / `Editor/BattleScene.cs` /
  `Editor/EndPanelProbe.cs` **还在用**（那些件**没迁**），⛔ 不是本批的活。
- `BattleDoors` 的**时序**（`SetupDoor` / `Play` / `Advance` / `DropFrame` / `Finished` / 音效 cue）——
  **一个字节没改**；`SetupDoor` 里 `_screen` 的销毁/重建语义也不变。
- 自建 shader · `DoorsScale` · `DoorsBoxW/H` · 三个 z 常量（**值全没动**）。

---

## 3. 父链对照（我们 vs 原版 —— **必须写的那一段**）

| | **原版**（判据 → `T09c` §一 / `T09b` §A·2#4 / §A·3） | **我们（本次迁完之后）** |
|---|---|---|
| 链 | `BattleHud/Canvas`（根）→ `BattleDoors` → **`Canvas`**（`PopUps`/0/ovr=true）→ `EndBattlePanel` | `BattleHud Canvas`（根）→ **`BattleDoors Canvas`**（子画布）→ `EndPanel` → `BattleDoors` |
| 门的画布挂在哪 | 根 canvas 的**直接子件 `BattleDoors`** **下面**（兄弟序 4/8） | 根 canvas 的**直接子件**（子画布**没有**中间那颗 `BattleDoors` 节点） |
| `EndBattlePanel` 挂在哪 | `BattleDoors/Canvas` **下**（兄弟序 0/1） | `BattleDoors Canvas` **下** ✓（这一跳对上了） |
| `BattleDoors` 节点挂在哪 | 在 `EndBattlePanel` **之上**（它是 `Canvas` 的父） | 在 `EndPanel` **之下**（`:348 BattleDoors.Create(_root)`） |

🔴 **如实记的差异（⛔ 本次故意不翻 —— 那是另一笔账）**：
**`BattleDoors` 与 `EndBattlePanel` 的父子关系是【反的】。**
- 原版：`… → BattleDoors → Canvas → EndBattlePanel → {Background, Video Image, AllRewardsHolder}`
- 我们：`… → BattleDoors Canvas → EndPanel → {…, BattleDoors → video_player}`
- **取法**：按 `EndPanel` 文件头 + `T09` §五·补⑩ 二 定的口径 —— **本次只按「谁的画布装谁」迁**（这一对的画布 = 原版 `BattleDoors/Canvas`，所以两者都挂在它下面），
  **保留我们既有的嵌套**（`BattleDoors.Create(_root)` 一个字没改）。
- ⚠️ **副作用（如实记）**：原版 `EndBattlePanel` 的 `lossyScale = 1.06647`（`A2105②` 记的 `C2` 实测），
  是因为它在带缩放的 `BattleDoors` **下面**；我们这边它在缩放**上面** ⇒ 我们的 `EndBattlePanel` 一级 `lossyScale` 是 1/108×1（**没有** 1.0665），
  而 `BattleDoors` 的**子树**（`video_player` / `video_screen`）照旧带 1.06647。
  ⇒ 「整块结算界面比原版小 6.65%」这件事**只被修了一半**（视频那一支修了、骷髅/标题那一支没有）——
  **这正是「父子颠倒」这笔账的下游**，本次不动它，**记给主对话**。

**其余父链/宿主全对上**：门的画布 = `PopUps`/order 0/`overrideSorting=true` ✓ ·
`EndPanel` 根 = 整屏 stretch（原版 `EndBattlePanel` 的 `anchor(0,0)-(1,1)`+`sizeDelta(0,0)`）✓ ·
`SkullsHolder` = 648.1×52.4 中心锚（原版那一格）✓ · `video_player` = 1920×1080（原版 `Video Image`）✓ ·
`BattleDoors` 根矩形 = 1920.1199×1118.9810 ✓。

---

## 4. 要改的断言（逐条 · 交给主对话）

> ⚠️ **`Editor/BattleScene.cs` 正被主对话改**（我读的两次之间行号就漂了 +29）⇒ **下面行号是 2026-10-16 20:1x 的现读**，改的时候按**内容**找。
> ⚠️ **两条与 `Battle/EndPanel.cs`／`Battle/BattleDoors.cs` 直接相关、必须改的断言在下面 A/B 两组**；
> `Editor/EndPanelProbe.cs` / `Editor/VideoProbe.cs` 只用 `.transform` / `.gameObject` / 行为口 ⇒ **一个字都不用改**（已核）。

### A) §A218 探针那一段（5 条必改 + 1 条助手 + 1 条注释）

**病根**：`box()` 这具尺子断的是 `rt.rect.width/height == LayoutSpace.Px(wPx)`，并**先断前置 `lossyScale == 1`**
（`BattleScene.cs:16437-16452`）。**挂在画布下的件两条前提全变**：
- `lossyScale` = 画布那一级（16:9 下 **1/108**；`BattleDoors` 子树再 ×1.06647）；
- `rt.rect` / `rt.sizeDelta` 的单位 = **canvas 单位 = 设计 px** ⇒ **`rect.width` 本身就是期望值那对数**，⛔ 不能再过 `LayoutSpace.Px`。

| 行（现读） | 现在 | **该改成** |
|---|---|---|
| **`box()` 助手 :16437-16452** | 前置 `lossyScale == 1`；比 `rt.rect.* == LayoutSpace.Px(期望)` | ② 给这个助手加一条**按「在不在画布下」分支**的口：Canvas 下的件断 `lossyScale == (1/108)`（`BattleDoors` 子树 ×1.06647）**且** `rt.rect.* == 期望 px`（**不过 `LayoutSpace.Px`**）；非画布件保持原样。<br>🔴 **说明**：`BattleScene.cs:16433` 那句「验收口径 = `rect.width/height` == 原版那对 px（经**唯一那一份**换算 `LayoutSpace.Px`）」+ `:16418-16420`「战斗侧这些件挂在 `HudRoot` 下、链上无缩放」**对本对已经不成立**（就地订正）。 |
| **:16456** | `var a218End = EndPanel.Create(a218root.transform);` | ⚠️ **`parent` 参数现在被忽略** ⇒ 建出来的面板挂在 **`BattleDoors Canvas`** 下、**不是** `a218root` 下。若还想让它挂探针根下：**做不到**（面板必须在画布上）。⇒ 保留调用（返回值仍有效），**把注释改掉**；下游 `a218End.transform.*` 照旧可用。 |
| **:16457** | `box(a218End.transform, "结算面板根 EndPanel", 1920, 1080, "原版 EndBattlePanel（stretch）")` | 本件现在是 **stretch**：`anchorMin==(0,0) && anchorMax==(1,1) && sizeDelta==(0,0)`，**解析后的 rect = 1920×1080**（16:9）。⇒ 断「锚点 0..1 + `sizeDelta 0` + `rect == 1920×1080`」 |
| **:16458** | `box(a218End.transform.Find("content"), "结算内容层 content", 1920, 1080, …)` | 同上（`content` 也是 `FillParent`）。`Find("content")` 仍然找得到 ✓ |
| **:16460** | `box(a218End.transform.Find("content/SkullsHolder"), "骷髅行 SkullsHolder", 648.1, 52.4, …)` | 结构没变（中心锚 + 648.1×52.4），**只是期望值不能再过 `LayoutSpace.Px`** |
| **:16489** | `var a218Doors = BattleDoors.Create(a218root.transform);` | ⚠️ `a218root` 不在任何画布下 ⇒ 本件**挂到这一对的子画布下**去了（`BattleDoors.Create` 的分支） |
| **:16490** | `box(a218Doors.transform, "开门视频根 BattleDoors", 1920.1199, 1118.981, "…它自己 localScale 1.0665 ⇒ 视觉 2048×1193，**缩放不复刻**")` | 期望值口径同 A；⚠️ **`src` 文案里「缩放**不复刻**」是【过期】的** —— `A2105②` 早已把 `localScale = 1.06647` 加上去了（本文件 `BattleDoors.cs:238`）。⇒ 就地订正为「缩放**已复刻** ⇒ `lossyScale = 1.06647 × 画布那一级`」 |
| **:16492** | `box(a218Doors.transform.Find("video_player"), "视频组件宿主 video_player", 1920, 1080, …)` | 期望值口径同 A（`Find` 照旧有效） |

### B) 🔴🔴 **两条现存断言【会变红】，而且红得有道理**（`BattleScene.cs:10196-10206`）

```
10196   var dimT = ep2.transform.Find("dim");
10197   float dimZ = dimT != null ? dimT.position.z : 0f;
...
10199   foreach (var cv in Object.FindObjectsOfType<CardView>()) if (cv.name.StartsWith("Hand")) handMaxZ = max(…)
10202   Check(dimT != null && dimZ < handMaxZ, $"压暗层在最靠前（dim z={dimZ:F2} < 手牌最远 z={handMaxZ:F2}）");
10204   var subT = ep2.transform.Find("content/end_sub");
10205   Check(subT != null && subT.position.z < handMaxZ, $"结算副标题盖在手牌上面（…）");
```
- **现读会发生什么**：`dim` 与 `content/end_sub` 现在挂在子画布上 ⇒ 它们的 `position.z` = **画布平面那一级**
  （根画布 `planeDistance 20.3`，相机在 z=−20 ⇒ **z = +0.3**）；而**手牌是还没迁的世界空间 quad**（z ≈ 0~0.24）
  ⇒ `0.3 < 0.24` **为假** ⇒ **两条都红**。
- **这两条抓的是真事**（「压暗/内容要盖住手牌」），但**判据该换了**：uGUI 档「谁压谁」= **(排序层, order, 兄弟序)**，
  不再走世界 z（口径 → `T09` §五·补⑨ 一）。
- **建议改法（二选一，都能灭自证）**：
  1. **断层**：`HudCanvas.SubCanvas("BattleDoors Canvas")` 非空、其 `sortingLayerID == HudCanvas.SortingLayer.PopUps`、
     `overrideSorting == true`、`sortingOrder == 0`；**并且**手牌那些卡**不在任何画布上**（`GetComponentInParent<Canvas>() == null`
     ⇒ 它们落在隐式的 `Default` 层）⇒ 「PopUps > Default」这条**结构性**判据成立。
  2. **断画出来的次序（灭自证更强）**：比 `dim` 与手牌**在同一台上画出来的先后** —— 例如取两者材质的 `renderQueue` +
     所在排序层 ID，按 `(层ID序, order, 兄弟序)` 排序后断言 dim 在后。⚠️ 层 ID 是**乱序哈希**（`Default=0` 最小是巧合），
     ⚠️ 别拿 ID **数值大小**当先后（要用 `T09b` §A·1 那张**层表顺序**）。
  - ⚠️ 断言里那两句文案（「z=… < 手牌最远 z=…」）也要一起改成新判据的措辞。

### C) 一条**过期注释**（不是断言，但同一段里）

| 行（现读） | 现在 | **该改成** |
|---|---|---|
| **`BattleScene.cs:17276`** | 「⚠️ **「面板为 `null`」那一半自检里造不出来**（`EndPanel.Create` 恒建、**不返回 `null`**，见 `S2` §二.6）」 | 🔴 **本批起【不再恒真】**：`EndPanel.Create` 现在**会返回 `null`** —— 拿不到根画布（`LayoutSpace.Cam` 为空）时，`BattleDoors.PairCanvas()` 出声并给 `null`（口径：⛔ 绝不退回世界空间 quad）。⇒ 那半边的措辞要改（`EndPanelProbe` 那种「先 `LayoutSpace.Apply(cam)` 再建」的路仍然拿得到面板）。<br>⚠️ `BattleDriver` 那侧**每一处都已有 `_endPanel != null` 判**（`:3104/:7620/:11764/:16361/:16454` 等）⇒ **行为安全**。 |

### D) 明确**不用改**的（已逐条核过）

- `BattleScene.cs:7213-7320`（结算面板那一节）：`Outcome` / `ResultText` / `ShownSkulls` / `SkullQuads[i].transform.position.x`
  （**世界 x 的大小序不变** ⇒ 从左到右那两条照旧成立） / `activeSelf` 计数 / `SkullPlateWorldW*108 ≈ 648.1`
  （`HudPiece.WorldW` 在 uGUI 档量的是 `GetWorldCorners`、**返回世界单位** ⇒ `×108` 那两步**仍然成立**）
  / `RewardRowPieces` —— **一条都不用动**。
- `BattleScene.cs:7363-7500`（开门视频那一节）：全是行为口（`HasVideo/Playing/ScreenVisible/Elapsed/ExitReady/FrameDropped`）。
- `BattleScene.cs:20097-20160`（`skipSequence` 那一节）：`ep.Doors.*` 行为口。
- `Editor/EndPanelProbe.cs:58-72`：`panel.SkullQuads[i].transform.position.x * 108f + 960f` —— 画布 16:9 下 1 单位 = 108 px、
  画布中心在世界原点 ⇒ **这串数算出来仍是设计 px**（逐位同上）；`panel.SkullRow.gameObject.activeSelf` ✓。
- `Editor/VideoProbe.cs:218-223, 371-372`：`Doors.DecodedFrames/HasVideo/Clip/Length` + `ContentVisible` ✓。

---

## 5. 类型检查

**结论：本批这两个文件 0 错；两个程序集在「把别人正在写的半成品换成 HEAD 版」的口径下各 0 错。**

```
# ① 官方脚本（最后一次读数，20:2x）
$ TMPDIR=/tmp/wf_end bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
（20 条 error，**全部在** Assets\CardPresentation\Battle\SettingsPanel.cs）
运行时错误数: 26
--- 编辑器程序集 ---
编辑器错误数: 0
```
- ⚠️ **中途也撞过一次别人的半成品**：`BattleLogPanel.cs(592/597/617/619/636/644/652/695/710)` ——
  `ImageQuad`→`HudPiece` 的赋值 / `SetRenderQueue` / `Px` 找不到（9 条）。**两次都全在别的写手的文件里**
  （红线：`BattleLogPanel.cs` / `SettingsPanel.cs` **不许碰**）⇒ 按纪律**没去动**，隔一会儿重跑即换人。
  **本批自己的两个文件在每一次读数里都是 0 条。**

```
# ② 为拿到「整棵树（含编辑器程序集）」的可信读数：临时把两个【别人的】半成品换成 HEAD 版
#    —— 做法 = 把 HEAD 版写到 /tmp/wf_end/，只替换【临时 rsp 里那两行源清单】，⛔ 没碰工作区
$ git show HEAD:.../BattleLogPanel.cs  > /tmp/wf_end/BattleLogPanel_head.cs
$ git show HEAD:.../SettingsPanel.cs   > /tmp/wf_end/SettingsPanel_head.cs
$ dotnet csc.dll @wf_csc_head2.rsp    → 运行时 error CS 计数 = 0
$ dotnet csc.dll @wf_csc_editor.rsp   → 编辑器 error CS 计数 = 0
```
⇒ 这两条证明：
1. **`Battle/EndPanel.cs` + `Battle/BattleDoors.cs` 编得过**；
2. 🔴 **`Editor/BattleScene.cs` · `Editor/EndPanelProbe.cs` · `Editor/VideoProbe.cs` 与 `SkullQuads` 那个类型改动
   （`ImageQuad[]` → `HudPiece[]`）**一起编得过**（它们只用 `.transform` / `.gameObject` ⇒ 本来就不该受影响，
   现在**实测坐实**）。

- ⛔ **没跑** `*.Run` 自检 · **没起** player（现行口径「先复刻后检查」）。
- ⚠️ **`git diff --numstat` 复核过**：`EndPanel.cs` **158 / 70** · `BattleDoors.cs` **102 / 25**（不是整篇重写）；
  行尾**逐字节核过仍是纯 CRLF**（`CRLF=565 LF=565` / `482 / 482`）。

---

## 6. 顺手发现的（⛔ 我自己一条都没改）

1. **`HudCanvas` 缺一支口 = 「相对父中心」的建图口**（我这次在本文件里写了 `EndPanel.ImageInParent`）。
   `HudCanvas.MakeImage` / `QuadFromTopPx` 把 `x/y` 当**屏幕绝对 px**（锚点写 `x/1920`、`1−y/1080`）
   —— **只有当 `parent` 恰好是一整屏矩形时**才等于「相对父中心」。`SkullsHolder`（648.1×52.4）就不是 ⇒ 直接套会把件摆到别处（**静默**）。
   🔴 **这是本批**（以及后面所有「面板里有**子矩形**容器」的扇）**通用的缺口** ⇒ 建议主对话把它收进 `HudCanvas`（我这份是**私有的临时件**，别让它长出第二份 —— 铁律「两处写同一条规则 = 迟早不一致」）。
2. **`RawImage` 这一族 `HudPiece` 包不住**（它只持 `Image`）。任何 **RenderTexture 驱动**的件都只能走 `RawImage`
   （`Sprite.Create` 只吃 `Texture2D`）⇒ 后面若还有「视频/RT 贴图」的件，**别去试 `Image`**。
3. **`HudCanvas.Group` 不回收已销毁的子件**：`Stop()` 每次 `DestroyImmediate(_screen.gameObject)`，而那个容器的 `Group`
   里留着它的僵尸条目（`_zs/_kids`）⇒ 每开一局 `SetupDoor` 就多一条。今天**无害**（`SetSiblingIndex` 会被 Unity 夹到末位，
   而且那个容器里同时只有一个真件），但**换分辨率/多局跑久了会积累** ⇒ 记账待查（`Group` 缺一个 `PruneDestroyed`）。
4. **同层同 order 的三块 `PopUps/0` 画布的先后由兄弟序定**（`T09b` §五·4）：我们这块是 `RootRect` 的**第 1 个子件**
   （`BuildHud` 里 `_endPanel = EndPanel.Create(root)` 早于 `BuildHudExtras`）⇒ 别处**晚建**的 `PopUps/0` 子画布
   （设置窗）会排在**我们之后 = 画在我们上面**。⚠️ 原版根 canvas 的子件序是 `[2]BackCanvas [3]FrontCanvas [4]BattleDoors`
   ⇒ 看那张表，设置窗（在 `FrontCanvas` 里）**排在门之前**。**两者相反** ⇒ `PopUps` 组收口时要**显式定兄弟序**（不是本批能定的）。
5. **`Battle/SettingsPanel.cs:547`** 的注释还写着「和 `EndPanel.SolidTex` 同一个做法」——`SolidTex` 本批**已删**（它在别的写手文件里，⛔ 我没碰）⇒ 那句注释该顺手改掉（铁律 5）。
6. **`EndPanel.Create` 的 `parent` 形参现在是死参**（同 `BuildLoadingCover(root)` 的先例）。两个探针（`BattleScene.cs:16456`、
   `Editor/EndPanelProbe.cs:34`）都还在传一个**不再生效**的根 —— 改注释即可，**别改签名**（签名一改要动 `BattleDriver.cs`，那是主对话正在改的文件）。
7. **本批的建件顺序 = 兄弟序**：`HudCanvas.Add` 是按 **z 降序**插的（`Group.Add`），所以「`dim` 先建」这件事**不影响**谁压谁 ——
   判据是三个 z 值（−0.50 / −0.55 / −0.60），已逐条核过（§2·2#9）。

---

## 7. 没查清 / 没做的（⛔ 不猜）

1. 🔴 **子画布在父画布平面内的 z（`localPosition.z`）· 没设、也没读原版值** —— `T09` §五·补⑨ 一·3 明写
   「主对话还没读原版的对应值，⛔ 别猜」⇒ 我**留 0**（与父画布共面），与**批 1** 的 `LoadingMenu Canvas` /
   `TooltipManager Canvas` 同一个先例。**谁压谁**目前只靠 `(排序层 PopUps, order 0, 兄弟序)` 三件套。
2. **父子颠倒那笔账的下游我没修**（见 §3）：`EndBattlePanel` 那一级**没有** `lossyScale 1.06647`
   ⇒ 骷髅底板/标题比原版小 6.65%（**视频那一支是对的**）。要修得**先把嵌套翻过来**（另一笔账）或**逐件补缩放**，
   两条都超出本件白名单 ⇒ **如实记，不做**。
3. **没跑真机/批处理渲染**（现行口径）⇒ 下面三件**只是「按代码推」，没验**：
   ① 画布在 `Camera.Render()` / 批处理截图下**渲不渲得出来**（⚠️ 但**批 1 那 52 件图 + 标签族已经在画布上了**，
      这条路已经铺过 ⇒ 风险低）；
   ② `RawImage` + 自建 shader（`[PerRendererData] _MainTex`）在 **CanvasRenderer** 下的实际表现
      （⚠️ 这条按 shader 属性表推是**成立**的：`CanvasRenderer.SetTexture` 绑的正是 `_MainTex`，`COLOR` 就是 uGUI 的顶点色）；
   ③ 「21:9 下 `FillParent` 的压暗/面板是不是真的盖满」（按 `Expand` 的 `scaleFactor = min(W/1920, H/1080)` 推是**盖满**的）。
4. **`skullW`（骷髅按贴图比例算出来的宽）没跟原版对过** —— 旧那套也是同一算式（`ImageQuad` 的「按高定」），
   所以**逐位不变**；但 `40k_battle_Win_Skull` 的原始 px 尺寸我**没去核**（自检也没断过骷髅的宽）⇒ 记着。
5. `T09` §五·补⑩ 说这一对里 **`ErrorMessageBanner`（`PopUps`/1）是安全顺序的第一件**、我们要按 **order 从高到低**迁；
   我这一件是 **order 0** —— **没去动别的扇**（红线），**组内次序**要等 5 件齐了在主对话那侧收口。
