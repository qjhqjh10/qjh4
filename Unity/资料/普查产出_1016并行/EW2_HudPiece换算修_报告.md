# EW2 · HudPiece 两处写死 108 的换算（T09 家族既有缺陷）

> 触发账 = `普查产出_1016并行/R1_面板迁移审查_报告.md` §④ 顺手发现 **第 3 条**（`HudPiece.cs:158/177` 的 `* 108f`）。
> 白名单 = `Battle/HudPiece.cs` + 本报告。⛔ 未跑 Unity、未动 git、未改白名单外任何文件。
> 全部行号**现核**（2026-10-16 · 本报告写作时；⚠️ 并行代理正在改**别的**文件 ⇒ 行号可能已漂，
> 定位请以**标识符**为准 —— 本报告每条都带了名字）。

## ① 结论

**判定【不该改算术】—— `× 108` 两轴都保留；只改了【注释】+ 把那个字面量名成「设计尺」常量（数值逐位不变）。**

一句话理由：**这两个口的输入不是「世界单位」，是「设计 px ÷ 108」** —— 三个现存调用点
（`BattleDriver.cs:15850/15851/16304`）**全都先套了 `Px()`**（`BattleDriver.cs:1377` `static float Px(float px) { return px / 108f; }`），
所以 uGUI 支那个 `× 108` 是 `Px()` 的**逆**，交出来的正是**原版那个设计 px**（= 原版 `m_SizeDelta` / `anchoredPosition` 的数）。
换成 R1 说的「按轴实测系数」（批处理 kx 108 / ky 144）会把每一处偏移/尺寸乘成 **1.333 倍**：
回合灯从**83.46 px 飘到 111.28 px**（离开牌堆底板 33%）、卡背高变成原版 `m_SizeDelta` 的 **1.333 倍**
⇒ **复刻变差**，而且 R1 自己在同一条里警告的「只改半边比不换更坏」恰好会成立
（同一族的 `HudImageTex` 仍按 108 建件，`BattleDriver.cs:16351`）。

R1 那条的**观察**是对的（批处理下两轴系数不同：x 108 / y 144，`T09` §七·8），
**处方不成立** —— 偏 0.75 的不是这两个口的**写入**，是「**拿量出来的世界尺寸去折设计 px**」那一类**读回**（`q.WorldH × 108f`），
以及**以「量的世界尺寸」为输入**的写入（`TutorialOverlay.SetHighlight` 画布支，见 ③·5）。
同一批代理在 `T09` §七·8 亲手实测的结论就是「**画面其实是对的**」（`FrameWorldH × 108` 读出 47.88 而真值 63.84）。

## ② 改动清单

文件：`d:/4/Unity/MyGame/Assets/CardPresentation/Battle/HudPiece.cs`（**只此一个**）

| 处 | 行（改后现核） | 改前 → 改后 |
|---|---|---|
| 文件头「判据」块 | `:26-33` | **新增 8 行**：写回口那两个 `108` 的解释 + ⛔ 别换成实测系数 + 指到下面两处 doc |
| `NudgeWorld` doc | `:155-184` | 4 段扩写：单位契约（`Px()` 的逆）· ⛔ 为什么不能换（含 83.46→111.28 实测例）· **成对动清单**（`HudImageTex` + 三个调用点）· ✅ 16:9 逐位不变 |
| `NudgeWorld` 体 | `:186-204` | `_rt.anchoredPosition += new Vector2(dxWorld * 108f, dyWorld * 108f);` → `float k = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight; _rt.anchoredPosition += new Vector2(dxWorld * k, dyWorld * k);` |
| `SetWorldHeight` doc | `:212-227` | 3 段扩写（同上，含「换实测系数 ⇒ 卡背高 1.333 倍」与「读回要读 `sizeDelta`」） |
| `SetWorldHeight` 体 | `:228-234` | `float h = worldH * 108f;` → `float h = worldH * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight);`（`:232`） |

**等值证明（不是"大致一样"）**：`LayoutSpace.DesignPxH = 1080f`（`Core/LayoutSpace.cs:211`）· `DesignHeight = 10f`（`:36`）；
`1080f / 10f` 的两个操作数与商 108 都**精确可表** ⇒ IEEE-754 除法返回**恰好 108f**（python float32 现算 `1080/10 == 108` → **True**）
⇒ 运行期 **bit 不变**，且与 `LayoutSpace.Px(px) = px / (DesignPxH/DesignHeight)`（`:214`）**严格互逆**（同一个常量表达式）。

- `git diff --numstat`：`59 4 Unity/MyGame/Assets/CardPresentation/Battle/HudPiece.cs`（**不是整篇重写** ✓）
- **行尾核对**：改前 **LF（CRLF 0 / LF 244）** → 改后 **LF（CRLF 0 / LF 299）**（二进制读计数）⇒ **没翻行尾** ✓
- **typecheck**：`TMPDIR=/tmp/wf_ew2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 · 编辑器错误数 0** ✓
  （第一次跑出 8 条，**全在 `Battle/TutorialOverlay.cs`** = 另一代理当时正在写的文件；隔 45 秒重跑归零 ⇒ 与本件无关，未去动别人的文件）
- **行为影响面**：**0 处行为变化**。4 个现存写入调用点的落点逐位不变（见④表）。

## ③ 接线清单（白名单外 —— 谁该改哪一行）

1. **`BattleDriver.cs:1377` `Px()`** —— 本口那把尺的另一半。今天的口径 = 两边都 108，**必须成对**（改一边 = 两把尺）。
2. **`BattleDriver.cs:16351` `HudImageTex` 的 `hPx = worldHeight * 108f`** —— **同一个 `Px()` 契约**，
   `_myDeckPlate / _foeDeckPlate / _myPile / _foePile / _myDeckLight / _myDeckSizePlate / _handPlate / _myPlate / …`
   整族都走它。🔴 **若将来要动本口的语义，这一行必须同批动**（半迁移 ⇒ 灯的偏移与底板尺寸两把尺，静默错位）。
3. **三个调用点 `:15850` `:15851` `:16301` `:16304`** —— 若采纳 2 里那条「把口改成显式 px」的路子
   （`NudgePx(dxPx, dyPx)` / `SetHeightPx(hPx)`：quad 支自己 `Px()`、uGUI 支原样吃 px），
   这四处**去掉 `Px()` 包装**，**行为逐位不变**，而 `108` 只剩 `LayoutSpace` 一处。⛔ 本件**没动**（白名单外 + 迁移脚手架别长新口）。
4. **`Editor/BattleScene.cs` 的读回**（拿**世界尺寸**折设计 px 去比字面 px ⇒ 批处理下 0.75 倍，**都是已迁到 uGUI 的件**）：
   `:2021-2025`（`MyDeckSizePlateWorldH` 比 `59.11/108`）· `:2028-2030` · `:2037-2043`（`DeckPlateWorldH*108f` 比 218.63）·
   `:2048-2053`（`HandPlateWorldH` 比 `65.7/108`）· `:3432-3433`（`MyPlateWorldH*108f` 比 101.04）·
   `:7426-7428`（`SkullPlateWorldH*108f` 比 52.4）。
   **改法**：读 **`HudCanvas.SizePx(q.Rect)`**（= `sizeDelta`，逐位等于原版 `m_SizeDelta`，`Battle/HudCanvas.cs:366`）。
   **先例已在**：`BattleLogPanel.FrameSizePx`（`:852`）· `MultiCardDisplay.ContinueBarDrawnPx`（`:665`）—— `FB` 那批已铺。
   ✅ **反向核对（这几处 ⛔ 不要动）**：`:14619-14624`（`Aspect Ratio Filler` 两枚）· `:15034/15038` · `:11350`（`WfSlider` 手柄）·
   `:14854`（`Label`）· `BattleLogPanel.RowAt:483`（世界点比世界尺寸）—— 这些件**还没迁**（世界档），
   `WorldW/H × 108 = 设计 px` 在这里**是对的**。
5. 🔴 **`Battle/TutorialOverlay.cs:876`（正在被别的代理迁）—— 这一支的输入是「量的世界尺寸」，不是设计 px**：
   ```csharp
   if (rt != null) rt.sizeDelta = new Vector2(w * Px, h * Px);   // :876  ·  const float Px = 108f;  ← :180
   ```
   而 `w/h` 来自 `size` = **量的世界尺寸**（`BattleDriver.cs:9843-9859` 的 TutAnchor 取**已迁件**的 `WorldW/WorldH`）。
   **实测数**：`_endTurnBg`（原版 rect 130.7×80.4 px）在批处理下量出 `(1.2102, 0.5583)` 世界单位
   ⇒ `×108` 交回 **`(130.7, 60.3)`** —— **高亮框比它要框住的那颗钮矮 25%**（16:9 下才刚好）。
   ⇒ **这一支要的是「按轴实测的逆」**（批处理 `kx=108 / ky=144`，`T09` §七·8），或更干净的做法：
   **在源头改读 `sizeDelta`**（`BattleDriver.cs:9843-9859` 那几个 TutAnchor 直接给 px）。
   ⚠️ **这不是「HudPiece 写错了」**：同一个数在 px 域是对的 ⇒ **同一个常量在两种来路下只能对一种**。
   判据是「**输入是设计 px，还是量出来的世界尺寸**」——两处各按自己的来路算，⛔ 别把 HudPiece 改成去凑这一处。
   ⚠️ 它 `:871-872` 的注释「⛔ 别在画布档调 `SetWorldHeight`（它内部写死 ×108、批处理下 y 轴会偏 0.75）」
   —— **那半句不成立**：它自己写的 `w * Px`（`Px = 108f`）与 `SetWorldHeight` 的 `worldH * 108f` **是同一个算式**
   （差别只在 `SetWorldHeight` 保持当前宽高比、它显式给 `w×h`）。

## ④ 判据与出处 + 16:9 逐位不变的复算 + 调用点逐个的影响面

### 判据（全部现读）

| # | 事实 | 出处 |
|---|---|---|
| 1 | **已迁族的尺寸/位置 = 设计 px**（「位置 = 锚点（归一化）· 尺寸 = **设计 px**」） | `资料/任务线/战斗线/任务/T09_整层换uGUI.md` §五·补⑧ 那张表 · §五·补⑤「高按世界单位 ×108 取设计 px」 |
| 2 | 那把尺的定义：`Px(px) = px / (DesignPxH / DesignHeight)`；`DesignPxH = 1080f` · `DesignHeight = 10f` | `Core/LayoutSpace.cs:214` · `:211` · `:36` |
| 3 | 调用点的来路：`Px(px) = px/108f`；回合灯偏移 `(83.46, −20.2)` 是**从原版 rect 推出来的设计 px**；卡背 `Px(h)` 的 `h` = 原版节点高 | `Battle/BattleDriver.cs:1377` · `:1363-1376` · `:16301` · `:16304` |
| 4 | 两轴系数怎么来的：批处理 `Screen=640×480` + `cam.aspect` 钉 16:9 ⇒ `CanvasScaler(Expand)` `scaleFactor = min(640/1920, 480/1080) = 0.33333` ⇒ 画布 rect = **1920×1440** 画布单位，铺在 **17.7778×10** 世界上 ⇒ **kx = 1920/17.7778 = 108 · ky = 1440/10 = 144** | `T09` §七·8（同批实测：`FrameWorldH × 108` 读出 47.88 / 真值 63.84）· `Battle/HudCanvas.cs:104-113`（ScreenSpaceCamera + Expand） |
| 5 | 谁「对」：原版 HUD 是画布件 ⇒ 它的 `anchoredPosition`/`m_SizeDelta` 单位是**画布单位**（与 `CanvasScaler` 无关）；窄屏上原版那件**本来就按 `scaleFactor` 缩**（世界支 = 画布支 × `1/R`，`R = scaleFactor × 1080/屏高`） | `Core/LayoutSpace.cs:67-100`（`HudTextScale` 的推导） |
| 6 | 同族写入点用的是同一把尺（⇒ 改半边 = 两把尺） | `Battle/BattleDriver.cs:16351`（`hPx = worldHeight * 108f`） |
| 7 | `WorldW/H` 走 `GetWorldCorners()`，**返回世界单位**（两种模式同义）；`Label.WorldW/H` 在 uGUI 档**就是 px** | `Battle/HudPiece.cs:19-25` · `BattleDriver.cs:15549` · `CardDisplayWindow.cs:261` |

### 16:9 逐位不变的复算

- **保留版（本件）**：`1080f / 10f == 108f` **逐位相等**（现算 True）⇒ 与改前**同一串 bit**；quad 支一字未动。
- **对照（假想的"实测系数"版）**：16:9 ⇒ `scaleFactor = 1` ⇒ 画布 1920×1080、世界 17.7778×10 ⇒ **kx = ky = 108**
  ⇒ **在 16:9 下两种改法也逐位相同** ⇒ 🔑 **「16:9 逐位不变」这条分辨不了两种改法**（正是本仓那条「只改半边在 16:9 下逐位等价、全绿」的老陷阱）。
  分辨器只能是**批处理 640×480**（或真机窄屏）：那边 kx 108 / ky 144，差 **0.75 = 108/144**。
- **调用点的往返精度**（既有行为，本次未动）：`Px()` → `× k` 在 float32 下差 **≤ 10 ulp**
  （实测 `83.46 → 83.45999908447266`、`217.39 → 217.38999938964844`、`23.4 → 23.399999618530273`，**绝对差 < 1e-3 px**）——
  不影响任何断言容差（那批容差是 0.02~1.5 px）。

### 调用点逐个的影响面

**（a）走 `HudPiece` uGUI 支的（= 会被本件影响的，共 4 处）**

| 调用点 | 档 | 输入来路 | 本件（保留 ×108）落地值 | 若换成实测系数会怎样 |
|---|---|---|---|---|
| `BattleDriver.cs:15850` `_myDeckLight.NudgeWorld(Px(83.46), Px(-20.2))` | uGUI | 设计 px | `anchoredPosition += (83.46, −20.2)` px ⇒ **与原版 rect 逐位**、与牌堆底板同尺 | `(111.28, −26.9)` px ⇒ **灯飘出底板 33%** |
| `:15851` `_foeDeckLight`（`73.6 / −9`） | uGUI | 设计 px | 同上 | `(98.13, −12)` |
| `:16301` `FitDeckPile` 的 `SetWorldHeight(Px(h))` | uGUI | 原版卡背节点高 px | `sizeDelta.y = h`（= 原版 `m_SizeDelta`） | `sizeDelta.y = 1.333 h`（**比原版卡背高 33%**，且与牌堆底板/卡背 SDF 错开） |
| `:16304` `FitDeckPile` 的 `NudgeWorld(Px(dx), Px(dyUp))` | uGUI | `CardbackFace.Fit` 的 padding px | `(dx, dyUp)` px | `(1.333dx, 1.333dyUp)` ⇒ 画心偏出原版位置 |

**（b）也调同名口、但走【世界支】或根本不是 `HudPiece` 的（本件不影响，列出以免误判）**

| 调用点 | 接收者 | 为什么不受影响 |
|---|---|---|
| `BattleDriver.cs:16301/16304` 的 **quad 支**（`_quad != null`） | `ImageQuad` | 本件只改了 `_rt != null` 那一支 |
| `Battle/ChatPopupPanel.cs:514` `q.SetWorldHeight(h)` | `HudPiece` | 在 `Put` 的**世界分支**里（`:511` 的 `Put` 里 `:514` `if (_ugui) { … return; }` 先返回）⇒ 永远走 `_quad` 支；输入 `h = r.height × VisibleHeight/RefH` 是真世界单位 ✓ 世界档自洽 |
| `Battle/SkillPanel.cs:729 / :748` | `HudPiece` | 都在 `UguiMode ? … : …` 的**世界分支**（`:686-694` 的注释就是为这件事写的） |
| `Battle/TutorialOverlay.cs:881` `_highlight.SetWorldHeight(h)` | `HudPiece` | 同上：`if (UguiMode) … else _highlight.SetWorldHeight(h);`（画布支走 `sizeDelta`，见 ③·5） |
| `Core/CosmeticPreview.cs:148` | `ImageQuad` | 根本不是 `HudPiece` |
| `Shell/MenuDraw.cs:1138/1686/1747` · `Shell/SettingsWindow.cs:4009/4378` · `Deck/DeckRuntime.cs:2077/2108/3313/3396/7212` · `Editor/ShellScene.cs:1563/1636` | `ImageQuad` | 同上（逐个现核过接收者类型） |

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. **没跑 Unity**（本批口径）⇒ 「④里那批读回断言在批处理下到底红不红」是**按判据推的**（机制 + `T09` §七·8 同批同类实测），
   **不是实测**。我给的落点清单（`:2021-2043` 等）请以「照判据该改」为准，红不红由跑 `BattleScene.Run` 的人定音。
2. **原版在 4:3 窗口里 HUD 画布件的实况/实拍：本地没有**（原版已关服）。「`anchoredPosition` 是画布单位、不随 `CanvasScaler` 变」
   是 **UGUI 语义层面的判据**（本仓惯例允许），**不是我们量出来的**；也没跑到 `unity_run_ref` 的实况。
3. `TutorialOverlay` 那一支最终走哪条（把源头改读 `sizeDelta` / 画布支按轴实测 / 保持 108）——**不在我白名单**，我只把两条路的数摆出来（③·5），**没动它**。
4. 调用点普查方式 = `grep -rn "\.NudgeWorld(\|\.SetWorldHeight(" MyGame/Assets --include=*.cs`（全仓、逐条现读接收者类型）。
   **间接调用（反射 / 转发口）本普查抓不到** —— 未发现先例，但这是**覆盖面的边界**，如实记。
5. 没查 `HudCanvas.MakeSlicedPiece` 那一族（面板边框）有没有同类「读回用错尺」的地方 —— **不是本件**，属面板族账（`T09` §七·9~16 那一片）。

## ⑥ 顺手发现（只报不改）

1. 🔴 **`Battle/TutorialOverlay.cs:866-876` 的自相矛盾**（它的注释判 `SetWorldHeight` 的 ×108 错，自己写 `w * 108f`）—— 全文见 ③·5；
   真正需要按轴实测系数的是**那一支**，不是 `HudPiece`。
2. ⚠️ **`BattleDriver.cs:3818 / 4091 / 8120 / 8137`** 四个**自检用** `*DrawnPx` 访问器把**已迁件的 `WorldW/H`** 乘 `108f`
   （或 `EndPanel.PxPerUnit`）⇒ 批处理下 **0.75 倍**。**只喂自检/探针、不进产品几何** ✓，但探针读数会误导
   （建议同 ③·4 改读 `HudCanvas.SizePx`）。
3. ⚠️ **`HudPiece.WorldW/H`（uGUI 档 = 世界单位）与 `Label.WorldW/H`（uGUI 档 = px）同名不同量纲** ——
   本会话「0.75」类困惑的总源头。`BattleLogPanel.RowAt:483` 是**用得对**的那一类（世界点比世界尺寸）；
   只要有人把这个数折成 px，就会踩。⇒ 建议口径：**读回一律走 `HudCanvas.SizePx`**（先例 `FrameSizePx` / `ContinueBarDrawnPx`）。
4. ✅ 顺手复核到一条**已闭环**：`BattleLogPanel` 的行命中（`:475-488`)**跨模式同义**、无尺混，
   与它 `:470-473` 自己写的注释一致 ✓（不是我先前怀疑的那类「局部 px 比世界尺寸」）。

---

### 可直接粘进总表的一行

- **`HudPiece` 两处 `× 108f`（`NudgeWorld`/`SetWorldHeight`）—— 判定【不改算术】**：输入是「设计 px ÷ 108」（三个调用点全先套 `Px()`，`BattleDriver.cs:1377/15850/15851/16301/16304`），`×108` 是它的**逆**、交出原版 `m_SizeDelta`；换实测系数（批处理 ky 144）会把灯飘出底板 33%、卡背高 1.333 倍 ⇒ **复刻更差**。已做：注释写死契约 + 字面量名成 `DesignPxH/DesignHeight`（bit 不变，typecheck 0/0）。
- 真账在**另外两处**（白名单外，已写接线清单）：① `Editor/BattleScene.cs` 那批「世界尺寸折 px」的**读回**（`:2021-2043/3432/7426`，改读 `HudCanvas.SizePx`）② `Battle/TutorialOverlay.cs:876`（输入是**量的世界尺寸**，需按轴实测的逆）。
