# EW10 · `WfSlider` 迁 uGUI（`T09` 块 6 · 第二件）

> 执行代理 `EW10`（2026-10-16）。**只读侦察** `S9` 的判据 → `资料/普查产出_1016并行/S6S9_卡体与残件_施工方案.md`
> §2·2 / §3·2；配方 → `资料/任务线/战斗线/任务/T09_整层换uGUI.md` §五·补⑩。
> ⛔ 本件没跑 Unity、没动 git、没改白名单外的任何文件（含两份正本与任何 `任务总表.md`）。

---

## ① 结论

| 口 | 做了 / 没做 | 说明 |
|---|---|---|
| **建件口**（`Create`） | ✅ **做了** | 加**第 12 个形参** `Transform uguiHost = null`（**尾部可选** ⇒ 既有 4 个调用点一字未改）。非空 ⇒ 三层都建成 uGUI 件；空 ⇒ 世界档。 |
| `Contains` | ✅ **做了** | 算式**一个字未改**；模式差异只在它调用的 `HitHandlePadded` 那个 `ppu`（`_ugui ? 1f : U`）。 |
| `SetFromPointer` | ✅ **做了** | 算式**一个字未改**，也**不需要改**（见 ④ 的新旧算式对照）。 |
| `Layout` | ✅ **做了** | 算式未改；只多一条**分派**（画布档的填条走新函数 `LayoutFillCanvas`），另一处 z 从字面量改成字段（世界档同值）。 |
| 双模式成不成 | ✅ **成** | 判据只此一处：`_ugui = uguiHost != null`；**世界档逐位不变**（逐条核对见 ④ 的「16:9 逐位自证」）。**本件被两个宿主共用**（战斗 3 处 / 壳侧 3 处）⇒ 走**模式形参**、⛔ 没有全局开关。 |
| 落点 | ✅ 按裁定 | **面板内部件**：挂 `SettingsPanel._panel`（面板帧，743.2×758.6 px、中心 = 设计中心）；**⛔ 不新增排序层、不进 `HudCanvas.SubCanvasRank`**。 |
| 生产侧接线 | ✅ 做了（`Battle/SettingsPanel.cs` 是我的白名单） | 三处 `Create` 传 `uguiHost: _panel`、`center` 的 x/y **改交 px**（去掉 `LayoutSpace.Px(...)`）。 |
| 自检读口 | ✅ 加了（**白名单外那两处编辑器由主对话改**，清单见 ③） | 原四个「世界单位」口径的读口**契约不变**（画布档内部 ÷108 交出去）⇒ 调用点那句 `× 108f` **两档同一个算式**；另加 7 个 px 口径的读口 + 3 个兄弟序档。 |
| `Shell/SettingsWindow.cs` | ⛔ **一个字没改**（白名单里有它，但**不需要改**） | 它那三根**不传** `uguiHost` ⇒ 世界档、逐位不变。⚠️ 那正是「⛔ 不许做成全局开关」的原因。 |
| typecheck | ✅ `TMPDIR=/tmp/wf_ew10 bash 工具/typecheck.sh` → **运行时 0 错 / 编辑器 0 错** | |
| 注册表 / `ReanchorHud` | ✅ **从来没挂过（现核）** | `grep -n "WfSlider\|SliderAt" Battle/BattleDriver.cs` = **零命中** ⇒ 配方第 6 步（「把迁走的件从 `ReanchorHud` 与 4 个注册表里摘掉」）**对本件是空操作**。⚠️ 那三根当年**跟 `HudRoot` 震屏**是**父链**带来的（`BattleDriver.cs:14583` `SettingsPanel.Create(root, …)`，`root` = HUD 根），不是注册表 ⇒ 迁到画布后**不再跟**（与其余已迁面板一致）。 |

**一句话**：三根滑块现在是**面板内的 uGUI 件**（两张 `Image (Sliced)` + 一张方 `Image`），
与面板其余件同挂 `PopUps`/0 那块子画布；半迁移态的两条代价（面板底盖住滑块 / 跟 `HudRoot` 震屏）**都消失**。

---

## ② 改动清单

### `Unity/MyGame/Assets/CardPresentation/Battle/WfSlider.cs`（+413 / −41）

| 行（现核） | 一句话 |
|---|---|
| `:94-133` | 文件头加一节：**双模式**（为什么 / 落点 / 单位那条 `u` / 三层先后 / 读口契约 / 那处权宜）。 |
| `:94` 附近 `using UnityEngine.UI;` | 🆕 画布档要 `Image`（`ppuMul` / `sprite`）。世界档一个字都不用它。 |
| `:268` | `ImageQuad _handle` → **`HudPiece _handle`**（适配层，两档共用；用法 `.transform`/`.Texture` 一字未改）。 |
| `:269-275` | 🆕 `bool _ugui` · `Image _bgImg, _fillImg` · `float _fillZ, _handleZ`（带 doc）。 |
| `:343-356` | 🆕 `<param name="uguiHost">` + **签名加第 12 个形参**（尾部可选）。 |
| `:375-394` | 🆕 那一个 **`u`**（世界档 108 / 画布档 1）⇒ 四个字段各除一次；画布档 `capScale != 1` 出声；`_fillZ/_handleZ`。 |
| `:396-421` | 根节点**分派**：画布档 = `SetParent(uguiHost)` + **`HudCanvas.SetPxSize`**（真 px）+ `localPosition=(x,y,0)` + `HudCanvas.Add(…, center.z)`；世界档那三行**原样**。 |
| `:433-494` | 🆕 **画布档三层**（`else if (s._ugui)`）：两张 `Image (Sliced)`（`MakeSlicedPiece` + `ApplyPpuMul`）+ 一张方 `Image`（`MakeImageInParent`）；`HudCanvas.Add(rt, −queue / −queue−1 / −queue−2)`；两个建不出来各自出声。 |
| `:515-516` · `:534` · `:557-565` | 世界档那一支：`-0.001f`→`s._fillZ`、`-0.002f`→`s._handleZ`（**同值**）；手柄多套一层 `HudPiece.Of(...)`；`SetRenderQueue` 加 `.Quad != null` 守卫（世界档行为不变）。 |
| `:571-609` | `Layout()`：加 `if (_ugui) LayoutFillCanvas(t);` 分派；手柄那一行的 z 改字段。**算式本体未改**。 |
| `:612-628` | 🆕 `LayoutFillCanvas`（一张 `Image (Sliced)` 改矩形宽 = 原版 `anchorMax.x = value`）。 |
| `:653` · `:686-695` | `LayoutFill` 的退化那两行收口进共用件 **`CapOf`**（`Image.GetAdjustedBorders` 的唯一一份实现；**逐值等价**：同一条件 `cap*2 > total`、同一值 `total*0.5`）。 |
| `:704-718` | 🆕 `ApplyPpuMul`（给 `MakeSlicedPiece` 建出来的 `Image` 补 `pixelsPerUnitMultiplier`）+ 取不到 `Image` 时出声。 |
| `:788-796` | `Contains` 加一段「为什么它不分档」；`HitHandlePadded` 的 **`ppu` 按模式换**。 |
| `:910` 附近 | `SetFromPointer` 加一段「算式没改、换算在哪」。 |
| `:928-941` | `HasArt` 加画布档分支（等价判据：那张 `Image` 有 `sprite`）。 |
| `:950-975` | `TrackWorldH` 加画布档分支（读 `sizeDelta.y` ÷ `U`）。 |
| `:979-984` | `FillSpanW` 加画布档分支（`sizeDelta` + `localPosition`）。 |
| `:988-1010` | `FillCapWorldW` 加画布档分支（用 `CapOf` 再算一遍同一条规则）。 |
| `:1012-1086` | 🆕 **画布档自检读口**：`OnCanvas` · `Node` / `FillNode` / `HandleNode` · `TrackSizePx` / `FillSizePx` / `HandleSizePx` · `BgOrderZ` / `FillOrderZ` / `HandleOrderZ`。 |

### `Unity/MyGame/Assets/CardPresentation/Battle/SettingsPanel.cs`（+71 / −30，**含我进场前就在工作区里的 T09 批 2 改动**）

| 行（现核） | 一句话 |
|---|---|
| `:44-62` | 文件头那条「例外：三根 `WfSlider` 仍在世界空间」**改写成过去时 + 留痕**（逐条记「现在怎么变了」；并记「世界壳必须留着，A218 读它」）。 |
| `:260-266` | `QPanel` 的 doc：画布档下它**不再是渲染队列**，是 `WfSlider` 三层兄弟序的基准（值不动）。 |
| `:633-638` | `RectContains` 的 doc：本面板**已无世界空间那档**。 |
| `:683-684` · `:693-696` | `_panel` / `PanelRect` 的 doc：世界壳现在**一个件都不装**；跨档找件走 `PanelRect`。 |
| `:700-709` | `Create` 里那段：**`MenuDraw.SetPxSize` 那一行原样留着** + 说明为什么⛔ 别删（A218 读它）。 |
| `:1002-1033` | **三处 `WfSlider.Create` 传 `uguiHost: _panel`**、`center` 的 x/y 改交 **px**（`SliderCx / SliderCy` 原样、去掉 `LayoutSpace.Px(...)`）；上面那整段注释改写成「已迁 + 为什么这样对得上」。 |
| `:1703-1706` | `SliderLabel` 的 doc：标签与滑块**同挂 `_panel`、同一把尺**（原来那半句「两族」已过期）。 |

### 行尾核对（逐文件，二进制读）

```
git diff --numstat
71      30      Unity/MyGame/Assets/CardPresentation/Battle/SettingsPanel.cs
413     41      Unity/MyGame/Assets/CardPresentation/Battle/WfSlider.cs
（Shell/SettingsWindow.cs 无输出 = 没改）

python -I -X utf8 -c "io.open(p,'rb')" →
WfSlider.cs       bytes  97454  CRLF 0   LF 1116
SettingsPanel.cs  bytes 186778  CRLF 0   LF 2208
SettingsWindow.cs bytes 484966  CRLF 0   LF 5552
```
⇒ **三个文件都是纯 LF，没有翻行尾**（改前逐个二进制数是 `0 / 744` · `0 / 2171` · `0 / 5552`）。
⚠️ 两个文件的 diff **远小于行数**（413/41 · 71/30）⇒ 不是「整篇重写」。

---

## ③ 接线清单（白名单外谁该改哪一行）

> ⚠️ `Editor/BattleScene.cs` **现在另有代理在改**（本件读它的时候行号已经漂过一次）⇒ 下面一律**按内容写**，⛔ 不写行号。
> 判据：本件把三根滑块从「世界壳 `sp.transform` 下」搬到了「**面板帧 `sp.PanelRect` 下**」，
> 且它们的手柄/填条**不再是 `ImageQuad`**（是 `Image`）。

### A. `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` —— **必改（否则红）**

| # | 现在（搜这一串） | 改成 | 为什么 |
|---|---|---|---|
| A1 | `sp.transform.Find("slider_" + sq.SliderName)`（在 `slider_fill` 那段里） | `sq.Node`（本件新口；或 `sp.PanelRect.Find("slider_" + sq.SliderName)`） | 画布档下滑块**不在** `sp.transform` 下 ⇒ 原来那条 `Check(false, "Fill 那一层在…")` 会红。 |
| A2 | `sp.transform.Find("slider_" + s0.SliderName + "/slider_handle")` | `s0.HandleNode` | 同上。 |
| A3 | `sp.transform.Find("slider_" + sq.SliderName + "/slider_handle")`（`A197` 那个循环） | `sq.HandleNode` | 同上。 |
| A4 | `hNode.GetComponent<ImageQuad>()`（`A1301①` 与 `A197` 两处）拿 `hq.WorldW/WorldH` | 换成 **`sq.HandleSizePx`**（本件新口，两档都交 px） | 画布档的手柄是 `Image`、没有 `ImageQuad` ⇒ `hq == null` ⇒ 那两条会打「前提不满足」红。 |
| A5 | `A1301①` (c) 那条判别式里的 `hQuad.WorldW * 0.5f` | `s0.HandleSizePx.x * perPx.magnitude * 0.5f` | 同一个物理量（**画出来的**手柄半宽），换成 px 口径那支。 |
| A6 | `A130` 那段「量 `slider_fill` 子树里 `ImageQuad` 的并集高」 | `sq.FillSizePx.y`（本件新口）+ 留着「两层同高」那条配对 | 画布档的填条是**一张 `Image (Sliced)`**、没有 quad 子件 ⇒ 并集恒 0 ⇒ 那条红。 |
| A7 | `a218Set` 那条文案「对局内设置面板**世界壳**（只装 3 根滑块）」 | 文案改成「世界壳（**已空**，只留 `sizeDelta`）」 | 数字（743.2×758.6）**不用改**、断言也不会红 —— 只是文案过期了。 |

✅ **不用改的**（现核过）：
- `sq.TrackWorldH * 108f ≈ 12`（`A96`）—— 本件给画布档补了同一契约的分支；
- `WfSlider.HandleHitPx(12f + 22.406f)`（静态纯算术口，与模式无关）；
- `sp.SliderAt(i).HasArt` / `.Value` / `.LeftWorld` / `.RightWorld` / `.HandleWorldPos` / `SetValue` /
  `sp.PointerFrame(...)` 那一整族（`Contains`/`SetFromPointer` 的算式没改，且 `TransformPoint` /
  `InverseTransformPoint` 两档都自洽）；
- **`A218` 那颗探针** `WfSlider.Create(a218root.transform, "A218Probe", …)` + `box(a218root.transform.Find("slider_A218Probe"), …)`
  —— 它**不传 `uguiHost`** ⇒ 世界档，**逐位不变**（它的 `FillWorldW/FillCapWorldW` 那组也照旧）。

### B. `Editor/BattleScene.cs` —— **建议新增（本件的自证）**

| # | 断言 | 为什么（判别力） |
|---|---|---|
| B1 | `sp.SliderAt(i).OnCanvas` == **true** | 「一半在画布、一半在世界」正是本件要修的那件事；⛔ 别用「`SliderAt(i) != null`」替（两档都真 = 同义反复）。 |
| B2 | `sp.SliderAt(i).Node.parent == sp.PanelRect` | 落点的**结构判别式**：挂错了（还挂世界壳 / 挂到别的容器）当场红。 |
| B3 | `sp.transform.childCount == 0` | 世界壳**已经空了**（本件搬完三根）⇒ 这是「一件都没留下」的正面证据（🧨 反面：谁把某一根留在壳里 ⇒ 红）。 |
| B4 | 三层兄弟序**不变量**：`BgOrderZ > FillOrderZ > HandleOrderZ`（三条都非 `NaN`） | `queue` 那套换成了兄弟序 ⇒ 这是「轨道在下、手柄在上」的**唯一**判别式。🧨 灭自证：三处都写同一个 z（最容易的写坏法）⇒ 红；「只断其中一层等于某个数」抓不住。 |
| B5 | `slider_bg` / `slider_fill` 的 `Image.type == Sliced` **且** `pixelsPerUnitMultiplier == 2` | 端帽 `92 / 15` 画布 px 的两个前提（少任一个 ⇒ 端帽宽一倍、中段短一半）。读口：`Node.Find("slider_bg").GetComponent<Image>()`。 |
| B6 | `sq.TrackSizePx == (561.08, 12)` · `sq.FillSizePx.y == 12`（与轨道同高）· `sq.HandleSizePx == (34.406, 34.406)` | 期望值一律写字面量（原版值），⛔ 不引用 `WfSlider.*` 常量。 |
| B7 | 面板底不再盖住滑块：滑块根那一档 `z` **小于** `_bg` 那一档 | 本件修的就是「面板底盖住那三根」。⚠️ 现成口只有 `OrderZOf`（`_bg` 没有公开读口）⇒ 要么加一个读口、要么用 B3+B2 结构式代替。**如实记：这条现在没有现成口。** |

### C. `Unity/MyGame/Assets/CardPresentation/Editor/SettingsScene.cs` —— **建议新增（反面那一半）**

| # | 断言 | 为什么 |
|---|---|---|
| C1 | 壳侧三根 `win.AudioSliders[i].OnCanvas` == **false** | 与 B1 **配对**才挡得住「把模式形参写成全局开关」—— 那一改会让壳侧三根一起翻、而 16:9 下**看不出来**（`T09` §五·补⑦）。 |

### D. `Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs` —— **不改**

它那三根 `WfSlider.Create(rowN, "vol" + i, …)` **不传** `uguiHost` ⇒ 世界档。
⛔ 别为了「统一」给它补上 —— 壳侧窗口系统还没 uGUI 化（`T09` §八·2 那条路 A 是另一笔大账），
强行挂画布会是**半迁移态**。**本件一个字没改它**（`git diff` 无输出）。

---

## ④ 判据与出处

| 判据 | 出处 |
|---|---|
| 原版对应物 = `Music` / `FX` / `Voiceover Slider`（标准 uGUI `Slider`）@ `BattleSettingsPanel`；继承它 = **(rm 2, ovr true, `PopUps`, 0)**；兄弟序 0/2 · 5/9 | `S6S9_卡体与残件_施工方案.md` §2·2（本件白名单外，只读） |
| 落点裁定 = **面板内部件 ⇒ 挂面板的画布容器；不新增层、不进 `SubCanvasRank`** | 同上 §3·2 |
| 配方 8 步（子画布 / `FillParent` / `MakeSlicedPiece` / `MakeText` / 单位 px / 摘注册表 / 改断言 / typecheck） | `T09` §五·补⑩ |
| 「`Add` 的 z 大 ⇒ 画在底下」⇒ 三层要**取负** | `T09` §五·补⑩ 那段就地订正（原句「语义同向、值照搬」对 `renderQueue` 是错的） |
| 画布件「1 单位 = 1 设计 px」；`CanvasScaler(1920×1080, Expand)` | `Battle/HudCanvas.cs` 文件头 + `T09` §五·补③ 一 |
| 原版 `Background` / `Fill` 两颗 `m_RaycastTarget = 1`、`m_RaycastPadding = 0`；手柄 `−25` | `WfSlider.cs` 文件头 `A1301①` 那两段（本件沿用，未改） |
| 端帽 = `m_Border ÷ multipliedPixelsPerUnit` | **本机 uGUI 源码实读**：`…/com.unity.ugui/Runtime/UGUI/UI/Core/Image.cs` 的 `Image.multipliedPixelsPerUnit`（`= pixelsPerUnit × m_PixelsPerUnitMultiplier`，`pixelsPerUnit = sprite.pixelsPerUnit ÷ canvas.referencePixelsPerUnit`）+ `GenerateSlicedSprite:1157` `GetAdjustedBorders(border / multipliedPixelsPerUnit, rect)` |
| 退化（总宽 < 两端端帽） | 同一份源码 `GetAdjustedBorders:1479-1511`：`adjustedRect.size < combinedBorders` ⇒ 两端**各乘** `size/combined`（对称边 ⇒ 各 = `total/2`，与 `CapOf` 同值） |
| 🔴 **`ppuMul` 那条路的 uv 是对的、预除 border 那条是错的** | 同一份源码 `GenerateSlicedSprite:1140-1158` 用的是 **`GetOuterUV` / `GetInnerUV`**（= 用 **sprite 自己的 `border` ÷ 贴图尺寸** 算出来的 uv）**与** `adjustedBorders`（画出来的 px）**分开**取 ⇒ 传 `border = 184` + `ppuMul = 2` ⇒ 端帽画 92 px、uv 段仍是贴图那 184 列 = **原版**；把 border 预除成 92 ⇒ 画出来同宽、**uv 段变成 92 列**（花纹不对）。 |
| 原版战斗那三根的矩形 **561.08 × 12.00**、手柄实画 **34.406**、端帽 **92 / 15**、滑区右让 **10** | `WfSlider.cs` 文件头（既有，未改） |

### 16:9 逐位自证（`scaleFactor = 1`）

前提（`HudCanvas` 文件头那条推导）：16:9 下 canvas 的 rect = **1920×1080 画布单位**铺在相机的
**17.78×10 世界单位**上 ⇒ **1 画布单位 = 1/108 世界单位**，正是**世界档**那个 `U = 108f`。
两档共用同一个面板几何（`_panel` 中心 = 设计中心 = 屏幕中心；世界壳中心 = 面板中心）⇒ 逐项对表：

| 量 | 世界档算出来（世界单位 / px） | 画布档算出来 | 逐位？ |
|---|---|---|---|
| 滑块根矩形 | `MenuDraw.SetPxSize(561.08, 12)` → `sizeDelta = (561.08/108, 12/108)` | `HudCanvas.SetPxSize(561.08, 12)` → `sizeDelta = (561.08, 12)` 单位 = `(561.08/108, 12/108)` 世界 | ✅ |
| 根中心 | `localPosition = (SliderCx/108, SliderCy/108)` | `localPosition = (SliderCx, SliderCy)` 单位 = 同一个世界偏移 | ✅ |
| 轨道端帽 | `borderOutPx = 92` px → 画 `92/108` 世界 | `border = 184` ÷ `(1×2)` = `92` 单位 → 同一个世界宽 | ✅ |
| 填条端帽 | `15` px | `border = 30` ÷ 2 = `15` 单位 | ✅ |
| 填条总宽 | `t × 561.08` px（`LayoutFill` 三块拼出来） | `sizeDelta.x = t × 561.08` 单位 | ✅ |
| 手柄边长 | `ImageQuad.Create(worldHeight = 34.406/108)` → 画 `34.406/108` 世界 | `sizeDelta = (34.406, 34.406)` 单位 | ✅ |
| 手柄中心 | `-w/2 + 12/108 + t×(w − 10/108)` | `-561.08/2 + 12 + t×(561.08 − 10)` 单位 = **同一个世界点** | ✅ |
| 命中 | `InverseTransformPoint` → 世界单位；`ppu = 108` | → 设计 px；`ppu = 1` | ✅ 同一块框 |
| 三层先后 | `renderQueue` 3000/3001/3002 | 兄弟序 z −3000/−3001/−3002（`Add` 的 z 大 = 画在底下）⇒ **轨道在下、手柄在上**，同序 | ✅ （机制换、**次序同**） |

⇒ **16:9 下两档画出来的屏幕矩形逐位相同**；差异只在 ④ 上面那条「三层先后靠什么表达」。

### 三条交互口的新旧算式对照

| 口 | 旧（世界档） | 新（画布档） | 差在哪 |
|---|---|---|---|
| `Contains(Vector3 world)` | `l = _root.InverseTransformPoint(world)`（局部**世界单位**）；① `Mathf.Abs(l.y) <= _h*0.5f && l.x >= -_w*0.5f && l.x <= _w*0.5f`；② `HitHandlePadded(l.x,l.y,handleCx)` | **同一个 `l`**（局部**设计 px**）；①②**同一个算式**（`_w/_h` 已是 px） | 🔴 **只差 `HitHandlePadded` 里的 `ppu`**：`U`(108) → **1**。不换 ⇒ 手柄命中框**大 108 倍**（`PxRect` 那两半是照 px 写死的），而画面完全正常。 |
| `SetFromPointer(Vector3 world)` | `t = Clamp01((l.x + _w*0.5) / (_w − _slideInsetU))` | **同一个算式** | **没有差** —— 分母 `_w − _slideInsetU` 两档各自是「px」，`l.x` 与它同域（世界档都是世界单位、画布档都是设计 px）。⇒ 「换算是靠字段那一次除」的样板。 |
| `Layout()` | `t = InverseLerp(…)`；填条 → `LayoutFill`（三块子 quad 改 `localPosition` + `localScale.x`）；手柄 → `localPosition = (−w/2 + offset + t(w − inset), 0, −0.002)` | 填条 → **`LayoutFillCanvas`**（`sizeDelta = (t×_w, _h)`、`localPosition = (−w/2 + t·_w/2, 0, 0)`）；手柄 → **同一个算式**，z = `_handleZ`(0) | **分派**多一支；填条那一支改的是**矩形宽**（= 原版 `UpdateVisuals` 的 `anchorMax.x = value`）而不是三块 quad 的位置/缩放 —— 两者**语义同一**（端帽恒宽、中段拉伸），只是画布档下由 uGUI 切片自己实现（退化也归它）。 |

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. **没跑 Unity、没起 player** ⇒ 「16:9 下逐位相同」是**逐值推的**（④ 那张表 + `HudCanvas` 的现成推导），
   **没有截图并排**。「4:3 / 16:10 也不坏」同理 —— 那是 `CanvasScaler` 的机制，本件只保证**几何落在画布单位里**。
2. **`HudCanvas.MakeSlicedPiece` 没有 `ppuMul` 形参** ⇒ 本件用**临时权宜**（建完 `GetComponent<Image>()` 补那个字段，
   取不到时出声）。⚠️ 这是**同族的第三处**权宜（先例 `WaitBanner` 的 Tiled），已记在 ⑥。
3. **端帽画出来的宽只能「再算一遍」**（uGUI 不给量切片结果的 API）⇒ `FillCapWorldW` 的画布档分支用的是
   `CapOf`（= `Image.GetAdjustedBorders` 的唯一一份实现）。**如实记：这是「同一条规则的第二处用户」而不是第二份实现**
   （`LayoutFill` 用的是同一个函数）；但**边不对称时它不成立**（本件两张图的 border 左右相等，所以成立）。
4. **本件的 `raycastTarget` 没动**（画布档三层仍是 `false`，原版 `Background`/`Fill`/`Handle` 都是 `1`）——
   与 `T09` §八·11 那条同族（第 4 步「迁按钮族 + 退两条手搓沿」要一起翻）。⚠️ 今天**没有 `EventSystem`** ⇒ 设了也不生效。
5. **`_panel` 的 `ap = (0, −29.317)`**（原版 `BattleSettingsPanel` 的中心比屏幕中心低 29.3 px）**本件没做** ——
   既有旧账（`T09` §七·15），⛔ 别当已复刻。它会让三根滑块**整体偏高 29.3 px**，与「面板底盖住滑块」**无关**。
6. **`Editor/BattleScene.cs` / `Editor/SettingsScene.cs` 我没碰**（白名单外；`BattleScene.cs` 另有代理在改）
   ⇒ 所以 **`BattleScene.Run` 现在是【预期红】**（③-A 那 7 处）。**本件按现行口径没跑它**。
7. ③-B7（「面板底不再盖住滑块」那条断言）**现在没有现成读口**：`_bg` 的兄弟序档要从
   `HudCanvas.OrderZOf` 拿，而 `_bg` 是 `SettingsPanel` 的私有字段、没有公开读口。**需要主对话决定**：
   加一个 `SettingsPanel` 读口，还是用 B2/B3 那两条结构式代替。

---

## ⑥ 顺手发现（只报不改）

1. 🔴 **`HasArt` 的断言文案与它实际查的东西不符（两档都有，进场前就在）**：
   文案写「三张图都取到了（轨道 / 填充 / 手柄）」，而实现只查**填条**（世界档那句 `_fillRoot.transform.childCount > 0`）
   与**手柄**的贴图 —— **轨道那张（`_bgRoot`）没查**。⇒ 「轨道图取不到」这一档今天是**静默**的
   （滑块会画成「没有底色的一条」）。本件没改世界档那个判据（怕动行为），画布档那一支**按同一口径镜像**
   （也是只查填条 + 手柄），⛔ **别把这条当成「已经查了三张」**。
2. **`HudCanvas` 缺 `ppuMul` 形参** ⇒ 本件成了**第三处**同类权宜（另两处：`WaitBanner` 的 `Image (Tiled)`、
   `HudCanvas.MakeSlicedPiece` 自己的注释也自陈「要逐位得让它收一个 `ppuMul`」）⇒ 建议主对话收编
   `MakeSlicedPiece(parent, tex, name, w, h, border, ppuMul, color)`，然后本件那 2 行 + `WaitBanner` 那 3 行一起退场。
3. **`Battle/SettingsPanel.cs` 的「世界壳」现在是个空节点**（只留着 `sizeDelta` 给 `A218` 读）——
   它**原版没有中间这一级**（原版 `BattleSettingsPanel` 直接挂在 `Safe area FrontCanvas` 下）。
   属 `T09` §五·补⑩ 二 那条「嵌套结构未必等于原版」同族，**本笔不动**，记在账上。
4. **`A218` 探针的用法告诫仍然成立**（`value: 0.5f` ⇒ 填条真只画半宽）；本件新增的那几支读口
   （`FillSizePx` 等）在**世界档**下也能用 ⇒ 将来把 `A218` 那组「按层名取子树」的写法换成读口会更省。
5. `Battle/SettingsPanel.cs:650` 那句注释仍引 `EndPanel.SolidTex` —— `T09` §七·7 那条账（「谁碰它顺手订正」）
   **还在**，本件没碰那一行。

---

## 📌 可直接粘进总表的一行

`| 块 6 · `WfSlider` | ✅ **2026-10-16 `EW10`**：双模式 `Create(..., uguiHost)`，战斗三根已迁 `SettingsPanel._panel`（面板内部件 · 不新增层）· 三条交互口按模式换算（`Contains`/`SetFromPointer` 算式未改、只换 `ppu`）· 世界档逐位不变（壳侧未动）· typecheck 0 错 · ⏭ **白名单外**：`BattleScene.cs` 7 处必改 + 7 条建议新增（见 `普查产出_1016并行/EW10_WfSlider迁画布_报告.md` ③） |`
