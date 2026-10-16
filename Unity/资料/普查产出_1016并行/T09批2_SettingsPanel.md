# T09 批 2 · `SettingsPanel` 迁 uGUI 子画布（施工报告）

> **写手**：`T09` 批 2 的并行子代理 · **日期** 2026-10-16 · **白名单** = `Battle/SettingsPanel.cs`（唯一改过的文件）
> **口径**：现行「先复刻后检查」⇒ **只跑秒级 typecheck**（⛔ 不跑 `*.Run`、⛔ 不起 player）。
> **判据**：`任务/T09_整层换uGUI.md` §五·补⑨/⑩ · `普查产出_1016战斗判据/T09c_…md` §一（`SettingsPanel` 那一行）·
> `T09b_…md` §一。

---

## 一、结论

**面板自己的图 / 字：全部迁完** —— 建件口 **33 处**（`HudCanvas.MakeText` **13** · `Img` **16** ·
`HudCanvas.MakeSlicedPiece` **3**（投降 / 跳过 / 列表底）· `HudCanvas.MakeImagePiece` **1**（整屏压暗）；
⚠️ `Img` 体内还转发 1 次 `MakeSlicedPiece` —— 已单独算过，别重复数），
全部建在它自己的 **`PopUps`/order 0 子画布**上。
**grep 现核**：文件里 **`Label.Create` / `MenuDraw.Nine` / `TintAll` 生产调用 = 0**、
`ImageQuad.Create` **只剩 1 处、且在 doc 注释里**（`:609`）⇒ 面板自己**一个世界空间件都不剩**。
`typecheck` **6 次全 0 错**（运行时 + 编辑器两个程序集）。

**还差 3 根 `WfSlider`（音量滑块）—— 它们【没迁】，原因不是漏了，是白名单**：

- `Battle/WfSlider.cs` **不在本件白名单**（简报原文：「只读、不改；它是否有必须改的口，写进报告让主对话裁」）。
- 而 `WfSlider` 那一整族**内部全是世界单位**：`Create` 里 `_w = trackW/108f`、`_h`、`_handleH`、
  `_handleOffsetU`、`_slideInsetU` 五处 ÷108；`Contains` / `SetFromPointer` / `HitHandlePadded` 拿
  `_root.InverseTransformPoint` 出来的**世界单位局部坐标**去比这五个值；三件底图走的是
  `MenuDraw.Nine`（世界空间九宫格）+ `ImageQuad.Create`（世界空间 quad）。
  ⇒ 它**不能**在不改 `WfSlider.cs` 的前提下变成 uGUI 件；而**把它整棵子树挂到画布下**又是本仓
  明令禁止的**半迁移态**（世界空间 quad 挂在画布下不归画布管 ⇒ 位置对不上 + 层级不受控，
  `Label` 文件头与 `HudPiece` 文件头都写着这条铁律）⇒ 本轮**没有**那么做。
- ⇒ 它们**仍挂在世界空间那层壳**（本组件自己的宿主 `transform`）上、**一个个字都没动**
  （位置照旧走 `LayoutSpace.Px(px)` = `px÷108`）。**两条已知代价**（都随 `WfSlider` 迁完自动消失）：
  ① 子画布在 `PopUps` 层 ⇒ **面板底会盖住那 3 根滑块**（它们还画在 `Default` 层）；
  ② 它们仍跟着 `HudRoot` 的震屏走，而本面板其余件（已在画布上）不再跟。
  `Build` 里那段注释把这两条写在现场，⛔ 不是静默失败。
- **必须给 `WfSlider` 加的口** → 见 §六·1（**逐条列了，可直接照做**）。

**顺带一处「必须同时改」的结构**（不是选择）：面板根节点**没能整块进画布** ——
它现在还装着那 3 根滑块（世界空间），所以拆分成了
`世界壳(743.2×758.6，px/108)`（装滑块）+ `画布下的面板帧(743.2×758.6 px)`（装其余全部件）。
等 `WfSlider` 迁完，把世界壳删掉、`Create(parent,…)` 那个 `parent` 参数与 `MenuDraw.SetPxSize`
一起退场即可（那时 `transform` 就可以换成面板帧）。

---

## 二、改动清单（逐处「旧口 → 新口 + 行号」；行号 = **改后**文件）

### 2.0 子画布参数（配方第 1 步）

| 项 | 值 | 判据 |
|---|---|---|
| 名字 | **`BattleSettingsPanel Canvas`** | 配方第 1 步（原版宿主名 + ` Canvas`） |
| 层 | `HudCanvas.SortingLayer.PopUps`（`1054366423`） | `T09c` §一 那一行 |
| order | **0** | 同上（`Canvas_2763.json`） |
| 建法 | `HudCanvas.SubCanvas(名字)` 先查、没有才 `EnsureSubCanvas(null, 名字, PopUps, 0)` | `EnsureSubCanvas` **不去重**（自检/探针会把面板建好几遍） |
| `overrideSorting` / `GraphicRaycaster` / ⛔ 无 `CanvasScaler` | 由 `EnsureSubCanvas` 内置 | `T09` §五·补⑨ 一·2 + `T09b` §A·1 |
| 面板帧 | 画布下新节点 `BattleSettingsPanel`（`HudCanvas.SetPxSize(743.2, 758.6)` px，落在**设计中心 (960,540)**，兄弟序 z = `PanelZ`） | 原版那颗 RT = anchor (0.5,0.5) 重合 + `sd 743.202×758.6345` |
| 整屏压暗 | 也是画布的直接子件（`FillParent`），z = `PanelZ + 0.02`（排在面板帧之前 = 压在底下） | `T09` §五·补⑩ 的「整屏覆盖层用 `FillParent`」 |

### 2.1 通用口（`SettingsPanel.cs`）

| 旧口（改前行号） | 新口（改后行号） |
|---|---|
| `static float U(float px) { return px / 108f; }`（`:503`） | **`→ return px;`**（`:536`）—— uGUI 档 1 单位 = 1 设计 px。⛔ 写回 ÷108 会让整扇面板缩成 1/108 且**不报错**（`§五·补⑩` 关卡一）。⚠️ 三个**世界空间**调用点（3 根滑块）改走 `LayoutSpace.Px(px)`（`:957/:962/:967`） |
| `Node()` 建**普通 `Transform`** + `localPosition=(U(cx),U(cy),z)`（`:506`） | `Node()` 建 **`RectTransform`**（中心锚 + 居中 pivot + `sizeDelta 0`）+ `localPosition=(cx,cy,0)` + **`HudCanvas.Add(rt,z)`**（`:546`） |
| `Img()`：`ImageQuad.Create(p,tex,pos,U(h),center,name)` + `SetAspect` + `SetRenderQueue(q)`（`:516`） | `Img()`：`HudCanvas.MakeSlicedPiece(p,tex,name,w,h,Vector4.zero,tint)` + `localPosition=(cx,cy,0)` + `HudCanvas.Add(rt,z)`（`:571`）。⚠️ `q` 形参**保留但已是空操作**（同 `Label.SetRenderQueue` 的先例）；`border=0` 的 `Image (Sliced)` 与 `Simple` 拉伸**像素等价** |
| （无） | 🆕 重载 `Img(p,name,tex,cx,cy,h,z)` = **只给高**、宽 = `h × 贴图比例`（`:602`）= `ImageQuad.Create` 的原语义（`T09` §五·补④ 第一行） |
| `TintAll(GameObject,Color)`（`:531`） | **删**（九宫格从「根 + N 块」变成**一张** `Image (Sliced)` ⇒ 染色直接给建件口） |
| `RectContains(root,world,w,h)`（`:539`） | **本体一字未改**（`:617`），只把 doc 说清「`w/h` 与局部坐标要同一把尺」；本面板里那把尺 = **px** |

### 2.2 `Create()`（`:656`）—— 建层

| 旧口 | 新口 |
|---|---|
| `new GameObject("SettingsPanel", RectTransform)` + `MenuDraw.SetPxSize(go,PW,PH)`（`:570`） | **原样保留**（它是**世界壳**、`A218` 那条读的就是它 + 装 3 根滑块） |
| （无） | 🆕 `HudCanvas.Ensure(LayoutSpace.Cam)` → `SubCanvas`/`EnsureSubCanvas` → 建**面板帧** `BattleSettingsPanel`（`HudCanvas.SetPxSize` + `PosPx(960,540)` + `Add(PanelZ)`）（`:679`–`:722`）。建不起来 ⇒ **出声 + 本面板不建**（⛔ 不退回世界 quad） |

### 2.3 `Build()` 逐件（旧 `Label.Create` → `HudCanvas.MakeText`；旧 `ImageQuad.Create` → `Img` / `MakeSlicedPiece`）

| 件（对象名 / 原版名） | 旧口（行号） | 新口（行号） | 备注 |
|---|---|---|---|
| 整屏压暗 `settings_shade`（`Dark Shade`） | `ImageQuad.Create(p,SolidTex(可见宽比),pos(0,0,Z+0.02),DesignHeight*1.05,center)`（`:587`） | `HudCanvas.MakeImagePiece(子画布,…,0,0,1,1,黑 .62)` + **`FillParent`** + `Reorder(Z+0.02)`（`:743`） | ⛔ 不写死 1920×1080（21:9 盖不满）；父 = 子画布（面板帧只有 743×758）；`Reorder`（⛔ 不是 `Add` —— `MakeImagePiece` 自己已登记过一次） |
| 面板底 `settings_bg` | `ImageQuad.Create(p,SolidTex(PanelW/PanelH),pos(0,0,Z),U(PanelH),center)`（`:596`） | `Img(面板帧,"settings_bg",tex,0,0,PanelW,PanelH,Z,0,色)`（`:761`） | **就地改一处（铁律 5）**：旧那套按 8×8 纯白贴图比例铺 ⇒ 实画 **758.6×758.6**（比面板宽 15.4 px）；现在精确 = **743.2×758.6** |
| 标题 `settings_title`（自拟） | `Label.Create(p,…,pos(0,U(PanelH/2)−U(70),Z−0.01),4,center)`（`:608`） | `MakeText(面板帧,…,0f,U(PanelH/2)−U(70),4,center,Z−0.01,name)`（`:772`） | 坐标/字号逐值不动 |
| 关闭钮圆底 `settings_close` | `ImageQuad.Create(p,UI_Button_Round_background,pos(cx,cy,Z−0.01),U(ClosePx),center)`（`:614`） | `Img(面板帧,…,cx,cy,U(ClosePx),Z−0.01)`（`:779`） | 宽仍按贴图比例（原语义） |
| 关闭钮叉 `settings_close_icon` | `ImageQuad.Create(p,40k_bt_close,pos(cx,cy,Z−0.02),U(ClosePx*0.42),center)`（`:616`） | `Img(面板帧,…,cx,cy,U(ClosePx*0.42),Z−0.02)`（`:781`） | 同上 |
| 投降钮 `settings_resign`（`40K_button` 九宫格） | `MenuDraw.Nine(p,tex,PxRect(画布 px 矩形),border(234/46),texW,texH,QPanel,"settings_resign")` + `transform.localPosition`（`:643`） | **`HudCanvas.MakeSlicedPiece(面板帧,tex,"settings_resign",300,90,border,ResignTint)`** + `Rect.localPosition=(−171.7,−310.5,0)` + `Add(Z−0.01)`（`:799`） | 原版那颗就是 `Image (Sliced)`；`ppuMul=1` ⇒ 画出来的端帽 = 原版那对 border ✓（旧那套不传 `borderOutPx`，逐值相同）；`QPanel` 与 `TintAll` 退场 |
| 投降字 `settings_resign_text` | `Label.Create(p,Loc.T(ResignTerm),pos(−171.7,−310.5,Z−0.02),4,白,center)`（`:657`） | `MakeText(面板帧,…,U(ResignCxPx),U(ResignCyPx),4,白,center,Z−0.02,name)`（`:813`） | —— |
| 跳过教程钮 `settings_skip_tutorial` | `MenuDraw.Nine(…)`（`:669`） | `MakeSlicedPiece(面板帧,…,"settings_skip_tutorial",300,90,border,ResignTint)` + `Add(Z−0.01)`（`:826`） | 与投降钮逐字同处置 |
| 跳过教程字 | `Label.Create(…)`（`:682`） | `MakeText(…)`（`:839`） | —— |
| 难度标签 / 按钮 / 值（**我们自拟的一行**） | `Label.Create` ×2 + `ImageQuad.Create`（`:705/:707/:709`） | `MakeText` ×2 + `Img(面板帧,…,"settings_diff_btn",U(70f),diffY,U(diffH),Z−0.01)`（`:862/:865/:868`） | —— |
| Auto Zoom 底图 / 勾（`40k_dropdown_bg` / `…checkmark`） | `ImageQuad.Create` + `FitHeight` ×2（`:731/:742`） | `Img(面板帧,…)` ×2（`:892/:901`） | `FitHeight` 内接算式**一字未改**（给的就是 px 高） |
| Auto Zoom 文字 | `Label.Create`（`:762`） | `MakeText`（`:919`） | —— |
| 静音行：两块底图 / 两颗勾 / 两颗 ON-OFF 字 / 说明 | `ImageQuad.Create` ×4 + `Label.Create` ×3（`:1289`–`:1329`） | `Img(面板帧,…)` ×4 + `MakeText(…)` ×3（`:1477`–`:1520`） | `FitHeight` 内接、fs、`SetAutoFitBox(px)` 全不动 |
| 语言行：框 / 框内语言名 / 箭头 / 左标签 | `ImageQuad.Create` + `Label.Create` ×3（`:842`–`:870`） | `Img(面板帧,…,"settings_lang_field",cx,cy,w,h,LangFieldZ,QPanel,LangFieldTint)` + `MakeText` ×3（`:1006`–`:1033`） | 那颗框的 z 收成常量 `LangFieldZ`（见 §2.5） |
| 下拉列表底板 `list_bg` | `MenuDraw.Nine(list,tex,PxRect,border(23,20),…,QLangList,tint,…,borderOut)`（`:1017`） | `MakeSlicedPiece(list,tex,"list_bg",w,573.96,**LstPanelBorderOut**,LstPanelTint)` + `Add(z)`（`:1202`） | ⚠️ 原版 `m_PixelsPerUnitMultiplier = 1.09`、`MakeSlicedPiece` 恒 1 ⇒ 只能把 1.09 先折进 border（端帽同值 21.1/18.35；切 uv 那三刀落在 21.1 贴图 px、原版 23 ⇒ 差 ≤2 贴图 px，如实记） |
| 列表 Viewport / Content / Scrollbar / item 各行 | `Node` + `Img` + `Label.Create`（`:1026`–`:1068`） | `Node`（RectTransform）+ `Img` + `MakeText`（`:1214`–`:1268`） | 行高 40.8707 / 勾 20 @ 行左 10 / 字内边距 20/10/2/1 逐值不动；三个队列常量（3141/3142/3143）**在本件已成死值**（doc 已订正） |
| 滑块标签 ×3（`settings_slider_label_i`） | `Label.Create`（`:1423`） | `MakeText(面板帧,…)`（`:1617`） | ⚠️ **标签在画布上、滑块不在**（两族）—— 见 §一「例外」 |
| 3 根滑块 `slider_music/fx/voice` | `WfSlider.Create(transform,name,Vector3(U(cx),U(cy),z),…)`（`:795`） | **`WfSlider.Create(transform,name,Vector3(LayoutSpace.Px(cx),LayoutSpace.Px(cy),z),…)`**（`:957/:962/:967`） | 🔴 **只改了「位置那个换算」**（`U` 変成 px 之后必须显式走 `LayoutSpace.Px`，否则偏 108 倍）；**其余一律原样**、仍挂在世界壳 `transform` 上 |

### 2.4 单位 / 尺寸类调用点（漏一处就是静默错）

| 旧口 | 新口 | 判据 |
|---|---|---|
| `ApplyLangFont`: `l.SetScriptHeight(text,px,108f)`（`:893`） | **`pxPerUnit = 1f`**（`:1074`） | uGUI 档 1 单位 = 1 px；传 108 ⇒ 字号小 108 倍、不报错 |
| `SetAutoFitBox(U(w),U(h),min,max,base)` / `SetVAlign(Bottom,U(LabelBoxH))` | **调用点文本未改**（`U` 变恒等 ⇒ 值自动成 px） | `Label.SetAutoFitBox` 的框宽/框高走**本档单位** |
| `CloseIconHit` 的 `float ppu = 1f / U(1f);`（= 108） | **同一行，现在自己得 1**（`:1951`，doc 已订正） | 它本来就是「本件唯一那个换算的倒数」 |
| `LangRowRectPx` / `MuteChatRectPx` 的 `local.x * 108f` + `WorldW * 108f` | **`local.x`**（局部已是 px）+ **`HudCanvas.SizePx`**（成对的那个逆）（`:1447` / `:1591`） | **返回值仍是 px** ⇒ 自检期望值不动 |
| `CloseDrawnSize` / `AutoZoomBoxDrawnSize` / `AutoZoomCheckDrawnSize` / `LanguageFieldDrawnSize` / `LanguageArrowDrawnSize` | `WorldW/H`（世界单位）→ **`HudCanvas.SizePx`（设计 px）**（`:2005/:2051/:2059/:2075/:2083`） | **返回值变 px** ⇒ 自检要跟着改（见 §四） |

### 2.5 命中判定（真实输入与自检走同一条）

| 口 | 旧 | 新 |
|---|---|---|
| `HitAutoZoom`（`:1593`）/ `HitMuteChat`（`:1355`） | `transform.InverseTransformPoint`（**世界壳**、世界单位）比 `U(Az/MuteHitPx)` | **`_panel.InverseTransformPoint`**（面板帧、px）比同一组常量（`:1788` / `:1543`） |
| `Contains` / `HitLanguage` / `HitDifficulty` / `HitClose` / `HitLangRow` | `piece.Contains(world)`（`HudPiece` 走 `GetWorldCorners`） | **`RectContains(piece.transform, world, SizePx)`**（本件自己的局部系 + 尺寸；`:1913/:1814/:1981/:1944/:1345`）—— 与 `HitResign` 同一套写法，**也不再依赖「画布有没有被引擎摆过」** |
| `HitResign` / `HitSkipTutorial` | `RectContains(_resignBtn.transform,…,U(w),U(h))` | **一字未改**（btn 现在是一张 `Image`，中心锚；`U` 成 px ⇒ 值 = px） |
| 语言列表「压在面板其余件之上」 | 旧那套靠队列 3141/3142/3143（见 `QLangList` 的 doc） | **`ShowLangList` 把语言框 `Reorder(LangListFrontZ=−0.60)`、`HideLangList` 挪回 `LangFieldZ`**（`:1285` / `:1316`）。🔴 **必须有它**：列表是挂在**那颗框下面**的（`Show()` 的父链 + 自检 `LangListNode.parent == LangFieldNode`），迁到画布后先后**只由兄弟序**定 ⇒ 不给这一句，列表会被**排在框后面的件**（如静音行 `z−0.05`）盖住 —— 实测几何：列表底边伸到面板 y = **−249.3**，静音行占 y ∈ **[−281.5,−226.6]** ⇒ **重叠 22.7 px** |

### 2.6 同批四件（配方第 6/7 步）

① 注册表摘除：**本面板的件从来不在** `ReanchorHud` / `_hudImages` / `_hudImageSpots` 里
（那 4 个注册表只收 `HudAbs*`/`HudImage*` 那一族，本件自建自管）⇒ **无需摘除**（已核：
`grep -n "settingsPanel" BattleDriver.cs` 只有建面板、开窗、点击分发三类调用，没有注册表项）。
② 读回口：改成读 `RectTransform` / `HudPiece`（见 §2.4）。
③ `RefreshLayout`：**本件没有 `RefreshLayout`** —— 面板的位置/尺寸全是常量，
`ReanchorHud` 也不碰它 ⇒ **无操作需要撤**（换分辨率时它整块跟画布缩放，这正是本批的目的）。
④ 断言改动清单 → §四。

---

## 三、父链对照（我们 vs 原版；⚠️ 有差异，**没顺手倒过来**）

| 层 | 原版（`T09c` §一 + `T09b` §A·3） | 我们（改后） | 差异 |
|---|---|---|---|
| 面板本体 | `BattleSettingsPanel` @ `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas`（**`PopUps`/0**）+ 自身一颗 `Canvas` | 子画布 `BattleSettingsPanel Canvas`（`PopUps`/0，挂在**根画布**下）+ `BattleSettingsPanel` 帧节点 | 🔴 **`Safe area FrontCanvas` 那一级我们没有**（我们根本没有 `FrontCanvas` 这棵树）—— 与 `HUD`/`Tooltip`/`LoadingMenu` 三批**同一个已记差异**（`§五·补⑨` 一） |
| 面板内的「世界壳」 | ——（原版没有这一层） | `SettingsPanel`（世界空间，743.2×758.6 px÷108，只装 3 根 `WfSlider`） | 🔴 **本批临时物**（因 `WfSlider` 未迁；见 §一） |
| 面板在屏幕上的位置 | anchor (0.5,0.5) 重合 + `ap (0,−29.317)` + `sd 743.202×758.6345` | 面板帧：中心锚 + `743.2×758.6` px，落在设计中心 **(960,540)** | ⚠️ 原版那个 `ap (0, −29.317)` **我们没做**（且**不是本批引入的**：改前也是「面板中心 = 屏幕中心、`MenuDraw.SetPxSize` 只写 `sizeDelta`」）⇒ **记着，别当已复刻** |
| 整屏压暗 | `BattleSettingsPanel` 里的 `Dark Shade`（3963.5×2693）**同一棵子树** | 画布的直接子件（= 面板帧的**兄弟**） | ⚠️ 层级上差一级（原版在面板内）；**几何/次序一致**（铺满 + 在面板底之后画） |
| 语言下拉列表 | `Template`/`Dropdown List` **自带 Canvas**（`overrideSorting` + `sortingLayerName = "PopUps"`）挂在 `LanguagesDropdown` 下 | 普通 uGUI 子树（靠**兄弟序**顶上，见 §2.5） | ⚠️ 等价物、不是逐位（两种忠实修法见 §五·3） |

---

## 四、要改的断言（逐条：哪一条 · 现在期望什么 · 应该改成什么）

> `Editor/BattleScene.cs` **不在本件白名单** ⇒ 一条都没动，全部列在这里。
> 行号 = 现读（该文件正被别的写手改，引用前请按内容核一次）。

1. **`A424③`（`:10405`–`:10406`）两张图内接尺寸** —— 现在 `sp.AutoZoomBoxDrawnSize * 108f`
   与 `sp.AutoZoomCheckDrawnSize * 108f`（`* 108f` 是「世界单位 → px」那一步）。
   **该改成**：**去掉 `* 108f`**（口现在直接给设计 px）。⚠️ 不改 = `Inscribe(bd)` 判假（~8000 px）⇒ 红。
2. **`14b4①`（`:10560`）下拉框实画尺寸** —— `sp.LanguageFieldDrawnSize * 108f` ⇒ **去掉 `* 108f`**（期望 250×59.4 px 不变）。
3. **`14b4①`（`:10565`）箭头内接** —— `sp.LanguageArrowDrawnSize * 108f` ⇒ **去掉 `* 108f`**（期望 ≤20 不变）。
4. **`A964/E2③`（`:20977`）关闭钮实绘尺寸** —— `sp964e.CloseDrawnSize * PxPerUnit964`（`PxPerUnit964 = 108f`）
   ⇒ **改成 `= sp964e.CloseDrawnSize;`**（口现在给 px）。⚠️ 该块后面那几行（`sc964 + (0.45*sd.x*0.5f/PxPerUnit964, …)`）
   **一个字不用改**（`sd` 是 px、`/108` = px→世界 ✓ 自洽）。
   ↳ 已核：那一节里**没有**别的 `*DrawnSize` 用法（`AutoZoomBoxDrawnSize`/`AutoZoomCheckDrawnSize` 在全文件
   只出现在 §四·1 那两条）。
5. **`§A218`（`:16480`）「对局内设置面板根」** —— `box(SettingsPanel.Create(a218root.transform,null,null).transform,
   "对局内设置面板根", 743.2f, 758.6f, …)`。**现在照样绿**（世界壳仍写着 `px/108` + `lossyScale 1`），
   但它量的是**世界壳**（只装 3 根滑块）、**不是面板帧**了。
   **该改成**：另加一条量**面板帧**的（新增只读口 `sp.PanelRect`）——
   `Check(Mathf.Abs(sp.PanelRect.rect.width − 743.202f) < 0.01f && Mathf.Abs(sp.PanelRect.rect.height − 758.6345f) < 0.01f, "对局内设置面板**帧**（画布内）= 743.2×758.6 **px**")`。
   ⚠️ **`box()` 那个助手直接用不了**：它的两条前提是「`lossyScale == 1`」（画布链是 1/108 ✗）与
   「`rect == LayoutSpace.Px(px)`」（画布节点写的是**真 px** ✗）⇒ 要么给 `box` 加一个「画布档」变体，
  要么照上面那行**手写**（推荐后者，判据更直白）。
6. **`A276`（`:10336`–`:10355`）投降钮那一层的 z 口径** —— 现象：uGUI 档**没有局部 z**，
   `ResignLocalZ` 已改报**登记的兄弟序 z**（`HudCanvas.OrderZOf`）⇒ 现在 `rA == rB == PanelZ−0.01`
   ⇒ 那条 `|rA−rB| < 1e-5` **照样绿，但已不含原来的信息**（原来它咬的是「不许把父件的世界 z 混进来」）。
   **该改成**（保住可分辨性）：`Check(Mathf.Abs(rA − rB) < 1e-5f && rA < HudCanvas.OrderZOf(sp.PanelRect), "…投降钮登记在面板帧**之前**（z 更小 = 更靠前）")`
   —— 或直接**退役**这一条并把 `ResignLocalZ` 标成「uGUI 档无意义」（由主对话裁）。
   ➕ **同批**：那个探针建的两颗面板**现在挂到画布上了**（不再挂在 `zr2`/`zr3` 下）⇒
   `Object.DestroyImmediate(zr2/zr3)` 之后**面板节点留着**（`Hide()` 过、看不见，但是泄漏）
   ⇒ 建议补 `if (pA != null) Object.DestroyImmediate(pA.gameObject);`（`pB` 同）。
7. **不需要改的（已核，⚠️ 别误改）**：
   · `A424` 那条几何（`:10395`–`:10402`）与静音行那条（`:10488`–`:10493`）—— 都是
     `sp.transform.position + (px/108)`，只比 **x/y** ⇒ 16:9 下逐位成立 ✓（画布在那个比例下的
     px→世界因子正好 = 1/108）；
   · `LangRowRectPx` / `MuteChatRectPx` 那几条（`:10486` / `:10768` / `:10773`）—— 返回语义**仍是 px** ✓；
   · `14b5` 那些 `qBg0.WorldW` / `(…)*108f`（`:10778`–`:10796`）—— `HudPiece.WorldW` 两种模式都返回**世界单位** ✓；
   · 14b5 的 `sp.LangRowBg/LangRowCheck`（`:10777/:10820`）—— 返回类型 `ImageQuad` → `HudPiece`，
     但自检用的是 **`var`** ⇒ **零改动**（类型检查已过 ✓）；
   · 三根滑块那一整族（`14c`，`:10872`–`:11050`）—— 位置/尺寸/命中在**世界空间那一帧**里逐字未动
     （`sp.transform.Find("slider_…")` 仍成立 ✓）。

---

## 五、顺手发现的（⛔ 本件只报不改）

1. 🔴 **`HudCanvas` 那四个「设计 px」建件口里，图那一侧是【归一化锚】、字那一侧是【中心相对 px】——
   两套模型在非 16:9 下不一致**（`MakeImage`/`MakeImagePiece`/`QuadFromTopPx`/`MakeAnchored` 用
   `x/1920`、`1−y/1080` 归一化；`MakeText`/`PosPx`/`MakeSlicedPiece`/`FillParent` 是中心相对）。
   面板这族**只能用后者**：原版 `BattleSettingsPanel` 自己的 RT 就是 **anchor (0.5,0.5) 重合 + `ap` + `sd`**
   （中心锚），而用归一化锚在 4:3 下会把一颗 743×758 的面板画到**屏幕上方 1/3 处**（我不给推导了，
   反正是「1.333 倍偏外」那一族）。⇒ **本件全部用中心相对**；相应的缺口 =
   **`HudCanvas` 没有「普通图片 + 中心锚」的口**（只有 `MakeSlicedPiece` 是中心锚）⇒ 我用
   **`MakeSlicedPiece + border = 0`**（与 `Simple` 拉伸像素等价）顶。**建议**：给 `HudCanvas` 补一个
   中心锚的普通像口（或在 `QuadFromTopPx` 的 doc 里写明「只对 16:9 精确」）。
2. 🔴 **`HudCanvas.Group.Add` 不去重**：同一个件被 `Add` 两次会在那一组的账里留两条记录
   （排序索引此后算偏）。`MakeImage`/`MakeImagePiece`/`MakeAnchored`/`MakeText` **自己就登记一次** ⇒
   调用点想挪档必须走 **`Reorder`**（本件 `_shade` 就是这么写的，注释在现场）。
3. ⚠️ **`MakeSlicedPiece` 的 `pixelsPerUnitMultiplier` 恒 1**：要复刻原版那颗 1.09（语言列表底板）
   只能把 border 预先 ÷1.09 ⇒ **画出来的端帽对、切 uv 那三刀差 ≤2 贴图 px**。要逐位对齐得让
   `MakeSlicedPiece` 收一个 `ppuMul` 形参（`HudCanvas` 的事）。
4. ⚠️ **语言下拉列表在画布里的层级**（§2.5）：原版是**自己的 canvas**（`overrideSorting` + `PopUps`）
   ⇒ 逐位做法 = 给列表建一层子画布（`EnsureSubCanvas(父件=语言框, "…", PopUps, 0)` 支持传父件）；
   本件**先用兄弟序顶**（`Reorder`），如实记。
5. ⚠️ **画布上的节点不再随 `parent` 一起销毁**（与 `BattleHud Canvas` 同一条既有口径）⇒
   §四·6 那两条探针泄漏；生产侧：`Hide()` 之后那些件都是 inactive ✓，但节点会留在子画布下。
6. ℹ️ `HudPiece.SetTexture` 只有 1 个形参 ⇒ 旧那套 `SetTexture(tex, keepAspect: true)` 的第二个实参退场；
   **不丢语义**（uGUI 换 `Sprite` 不动 `sizeDelta` ⇒ 比例天然由行框定）。
7. ℹ️ `_langField` 的 `40K_dropdown_field_closed` / `_langArrow` 那颗箭头的**图名仍是「我们的选择」**
   （原版那颗 PathID 本地没解出名字，`Shell/SettingsWindow` 同一条）—— 与本次迁移无关，照旧记着。

---

## 六、没查清 / 没做的（写清搜过什么、结果是什么；⛔ 不猜）

1. 🔴 **`WfSlider` 要加的口（逐条；这是本批唯一没做完的件）**
   判据 = 现读 `Battle/WfSlider.cs`（744 行，本件只读未改）：
   | 位置 | 现在写的 | uGUI 档需要 |
   |---|---|---|
   | `Create` `:310`–`:318` | `_w=trackW/108` · `_h=trackH/108` · `_handleH=handlePx/108` · `_handleOffsetU=handleOffset/108` · `_slideInsetU=SlideRightInsetPx*capScale/108` | 全部**直接吃 px**（÷108 退场）；`Create` 需要一个**模式形参**（⛔ 不能全局开关：外壳 `Shell/SettingsWindow.cs` 三根 + FPS 那根**共用**本件、那边还留在世界档） |
   | `Contains` `:550` · `HitHandlePadded` `:569` | `float ppu = U;  // = 108` 且局部坐标是世界单位 | `ppu = 1`（画布局部 = px）；`HandleFrameW`/`_handlePx` 那两块矩形直接按 px 比 |
   | `SetFromPointer` `:609` | `(_root.InverseTransformPoint(world).x + _w*0.5f)/(_w - _slideInsetU)` | 同一算式，但 `_w/_slideInsetU` 已是 px（上式不用改字） |
   | `Create` `:360`/`:379` 两层底图 | `MenuDraw.Nine(...)`（世界空间九宫格，`borderOutPx` = 92/15） | `HudCanvas.MakeSlicedPiece(父,tex,name,trackW,trackH, **borderOutPx**, tint)`（原版 `m_PixelsPerUnitMultiplier = 2.0` ⇒ 端帽 = border÷2 ⇒ 与 §五·3 同一个技巧）· 中心锚 + `localPosition` 显式给 |
   | `Create` `:406` 手柄 | `ImageQuad.Create(_root,handleTex,pos(0,0,−0.002),_handleH,…)` | uGUI `Image`（中心锚、`sizeDelta = handlePx²`），`handleOffsetU` 那一跳不变 |
   | `Layout` `:421`–`:441` · `LayoutFill` `:463` · `FillPlace` `:484` · `FillSpanW` `:677` · `FindFillQuad` `:495` | 三块子 **`ImageQuad`** 的 `localScale.x`/`localPosition` | 等价物 = **改 `Image.anchorMax.x`**（原版 `Slider.UpdateVisuals` 就是这么做的）或三块子 `Image`；读口要改走 `RectTransform`（`FillSpanW` 现在 `GetComponentsInChildren<ImageQuad>` ✗ 在画布上一个都取不到 ⇒ **返回 0**） |
   | `TrackWorldH` `:647` · `FillWorldW` `:705` · `FillWorldLeftX` `:715` · `FillCapWorldW` `:728` | 读 `ImageQuad.WorldW/WorldH` | 同上（自检 `TrackWorldH*108 == 12` 那条要么改读 px、要么改走 `GetWorldCorners`） |
   | `HasArt` `:635` · `SetVisible` `:631` · `WorldPos/HandleWorldPos/LeftWorld/RightWorld` `:738`–`:742` | —— | `SetVisible`/`WorldPos` 一类**不受影响**；`HasArt` 要看 `_fillRoot.transform.childCount`（uGUI 档得换成判 `Image.sprite`） |

   ⚠️ **`WfSlider` 迁完之前，那 3 根滑块会被面板底盖住**（§一·①）；迁完之后本件那三行调用点（`:957/:962/:967`）
   的**位置换算**也要跟着改（从 `LayoutSpace.Px` 回 `U`/px）。
2. **两条「没做」的**（本轮**故意不动**、都留给主对话裁）：
   ① 原版那颗面板的 `ap (0, −29.317)`（面板中心比屏幕中心**低 29.3 px**）我们改前改后都**没做** —— 与本批无关的旧账；
   ② 语言列表的「自成一层 canvas」（§五·4）。
3. **验收一条都没跑**（现行口径：「先复刻后检查」：⛔ 不跑 `*.Run`、⛔ 不起 player）。
   本件**必须有**的验收：三档宽高比截图并排（16:9 / 4:3 / 21:9）+ `wf_click.py` 真点
   （设置按钮 → 面板 → 投降 / 跳过 / 难度 / Auto Zoom / 静音 / 语言下拉 12 行 / 三根滑块）
   —— ⚠️ 那三根滑块**这一轮一定看不到**（§一·①），**别把它当新缺陷**。
4. **搜过什么**：`grep -rn "SettingsPanel" Assets --include=*.cs`（除本件与 `Editor/BattleScene.cs` 外
   只有注释引用 ✓ 本件**不是**跨场景共用件 ⇒ 单档迁移，不需要 `Tooltip`/`ErrorMessageBanner` 那种双模式）；
   `grep -n "HudCanvas\.\|EnsureSubCanvas\|…" Battle/BattleDriver.cs Core/Tooltip.cs`（照抄已落地两批的口径）；
   `grep -rn "WfSlider" 资料/ --include=*.md`（**没有任何文档说滑块要迁 uGUI** ⇒ 上面第 1 条是现读
   `Battle/WfSlider.cs` 自己推的，⛔ 不是抄来的）；
   `grep -n "HudCanvas.Reset\|RootRect = null\|Destroy.*HudCanvas" Assets --include=*.cs`（**没有**拆除口 ⇒
   子画布与 `BattleHud Canvas` 一样是「根对象、不随父销毁」，§五·5 那条泄漏就是这么来的）。

---

## 七、类型检查（逐次结果）

| 次 | 结果 |
|---|---|
| 1 | **1 错**：`SettingsPanel.cs(688): CS0104` `Object` 在 `UnityEngine.Object` 与 `object` 之间歧义（本文件有 `using System`）⇒ 改 `UnityEngine.Object.DestroyImmediate` |
| 2 | **0 错**（运行时 0 / 编辑器 0） |
| 3 | 0 错 |
| 4 | 0 错 |
| 5 | 0 错 |
| 6 | **0 错**（**交付前最后一次**，命令行：`TMPDIR=/tmp/wf_set bash d:/4/Unity/工具/typecheck.sh`） |

⚠️ 全程只跑这一个（秒级、带独立 `TMPDIR`），**一次 `*.Run` 都没跑、player 一次都没起**（现行口径）。
