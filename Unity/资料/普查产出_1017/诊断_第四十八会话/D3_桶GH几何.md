# D3 · 桶 G（MainMenu 模式卡几何）+ 桶 H（Rewards 两条）· 只读诊断

> 诊断代理 D3（只读）。⛔ 未改任何代码/场景/资产，未跑 Unity，未动 git。
> 判据输入 = `资料/普查产出_1017/自检_第四十七会话_结果与分诊.md` 桶 G / 桶 H
> ＋ 本轮原始日志 `d:/4/_tmp_view/menu.log` · `d:/4/_tmp_view/rewards.log`（第四十七会话 19:14–19:52 那一趟）。
> 结论按四分类：**α = 断言错 · β = 实现缺陷 · γ = 本批回归 · δ = 夹具前提不成立**。

---

## 〇、一页总表

| # | 断言原文（日志） | 分类 | 一句话根因 | 改哪 |
|---|---|---|---|---|
| G1/G2 | `Tutorial`/`Practice` 卡宽 `0.463 ≈ 4.954`；列位 `8.657 ≈ 10.903` | **α**（由本批 `T12` 触发，γ 触发源） | `T12` 视口裁切把卡图 quad **物理缩成可见的那 50px**；`CheckCard` 还在量 quad 的「整卡」宽与中心 | `Editor/MainMenuScene.cs` 的 `CheckCard`（量法） |
| G3 | `（前提）命中区 ToggleHit 没建出来` | **β** | 命中区建在 `…/Toggle/ToggleHit`（45 会话把父节点改成 `tog`），而读口 `TogHit` 仍用**只查直接子件**的 `transform.Find` | `Shell/PracticeModePopup.cs:784` |
| G4 | `Glow = 523.794²（-1.000）` · `glowColor` · `alpha 0/1` 四条 | **β**（本批新窗） | 图名写成 `"Glow UI W40K"`（空格），`Resources` 里是 `Glow_UI_W40K.png`（下划线）⇒ `MenuUi` 取不到 ⇒ `MenuDraw.Rect` 静默返回 null | `Shell/FTUESelectDeckContainer.cs:116` |
| G5 | `队伍名字号 85.286 ≈ 82.100` | **α** | 原版这颗 TMP **开着自适应**（`m_enableAutoSizing=1`），82.1 只是序列化 nominal；断言拿**收敛值**比 nominal | `Editor/MainMenuScene.cs:13195` |
| G6 | `行底 3302 < 信使名/时间 3000/3000` 与 `正文 3000 > 头像 3305` | **β**（本批由 `A1024③` 触发 = γ 触发源） | `MenuDraw.TextCore` **先 `SetRenderQueue(q)`、后 `SetFace(face)`**；换 font asset 会换掉 `m_sharedMaterial` ⇒ 队列被冲回 3000 | `Shell/MenuDraw.cs:1890/1895`（挪一行） |
| H1 | `那一格的 Reward 图标量得到（有图）` | **γ**（45 会话 `T21` 引入；α 侧也有一半） | `BuildRewardIcon` 改走 `ItemDrawer` 后抽屉根节点叫 `WildcardDrawer`（不再叫 `Reward`）；且图标几何从「高 150 等比」变成「282.77 方形」 | `Editor/RewardsScene.cs:4425` + `Shell/ForgeTab.cs:924` |
| H2 | `进度条宽 110`（实得 `10.00`） | **α** | `RectOf` 取子树里**第一颗** `ImageQuad`；滑条是三块九宫格（左右端块 = `borderOut 20/2 = 10` 宽）⇒ 量到的是左端块 | `Editor/RewardsScene.cs:5263`（改 `RectOfUnion`） |

> 🔴 与分诊单一致：**这七条都不属于桶 A（字号 ×0.912）**，是一条独立根因各管各的。
> ⚠️ G5 的数值方向也印证这一点：它不是 ×0.912 变小，而是 ×1.0388 变大。

---

## G1 / G2 · 模式卡卡宽与列位（`Practice` / `Tutorial` 两张 1×1）

### 1. 结论
**α（断言口径没跟上 `T12` 的裁切语义）**；`T12`（本会话新增的横向滚动 + 视口裁切）是**触发源**，
但**裁切本身不是缺陷** —— 它等价于原版 `GameModes` 视口上的 `RectMask2D`（第四列本来就该被右缘切掉）。

### 2. 证据（链路逐环）

1. 原版：`GameModes` = `ScrollRect(horizontal=1)`，视口矩形 **x[167.04, 1920]**（`MainMenuRuntime.ModesW/H` 反推；
   `资料/主菜单_原版规格.md:173`）。卡 `itemSize = 535 × 414.4`、`spacing (20,20)`、`pad L38`
   ⇒ 列 c 左 = `205 + 555c`。第 4 列 = `1870 .. 2405` ⇒ **有 485px 在视口外**（这是原版就有的）。
2. 我们的实现（`Shell/MainMenuRuntime.cs`，`BuildModeCard`）：
   - `:2321-2326` `cx1 = max(x, clip.x1)` · `cx2 = min(x+w, clip.x2)`；`cx2-cx1 <= 0.01` ⇒ **连节点都不建**；
   - `:2349-2354` 卡图 `ImageQuad.Create(card, tex, Center(cx1, cx2, y, y+h), h/108 …)` 然后
     `SetAspect((cx2-cx1)/h)` ⇒ **quad 的物理宽就是可见宽**（不是整卡 535 再被 mask）；
   - 同一对 `cx1/cx2` 也用在暗带 `:2363`、`Nine(..., clip)` 边框、标题 `ClipText` 上。
   - `Tutorial`（col3 / row1）与 `Practice`（col3 / row0）：`x = 205+3×555 = 1870`，`w = 535`
     ⇒ `cx1 = 1870`、`cx2 = min(2405, 1920) = 1920` ⇒ **宽 50px、中心 1895**。
3. 运行时实测（`menu.log:12502-12514` 的探针，`CheckCard` 自带那段）：
   ```
   【探针】Base Game Mode Container 1x1 - Tutorial 子树：
         Background Image    50.0×414.4   q=2905 图=Container_Image_Tutorial
         TextDarkening       48.0×105.9
         Border_00            2.6×2.6 / Border_01 2.6×409.2 ……（只剩右缘那一竖条）
         Hit                 50.0×414.4   q=2931
   ```
   ⇒ **50.0 / 108 = 0.463**，`(1870+1920)/2 = 1895 → (1895-960)/108 = 8.657`。**逐位对上两条红的实得值**。
4. 断言侧（`Editor/MainMenuScene.cs`）：
   - `:14459-14475` `CheckCard`：`q.WorldW` 比 `w/108`、`q.transform.localPosition.x` 比 `Center(colLeft,colLeft+w,0,0).x`；
   - 调用点 `:1746`（Tutorial）与 `:1897`（Practice）；两条红分别落在 `:14470`（宽）与 `:14473`（列位）。
5. 为什么只有这两张红：三张 1×2 的右缘 740 / 1295 / 1850 都 ≤ 1920，`cx2 = x+w` 不被裁 ⇒ 量出来仍是 535 / 原中心。

### 3. 修法（推荐改断言，⛔ 不要拿掉裁切）
`CheckCard` 里两处：
- **宽**：改量**卡容器自己的 `RectTransform`**（`New` 只写 size、不写位置，`MainMenuRuntime.cs:212-218`
  `MenuDraw.SetPxSize`），它仍是完整设计宽 535：
  ```csharp
  var rt = card.GetComponent<RectTransform>();
  CheckNear(rt.rect.width * 108f, w, 0.5f, $"{what} 设计宽 = {w}px（视口只影响可见片，不影响设计矩形）");
  CheckNear(rt.rect.height * 108f, h, 0.5f, …);   // 高不受横向裁切，可与原 q.WorldH 二选一
  ```
- **列位**：改断**渲染出来的左边缘**（`quad 中心 − quad 半宽`），它在裁切下仍恒等于列左：
  ```csharp
  float leftPx = (q.transform.localPosition.x - q.WorldW * 0.5f) * 108f + 960f;
  CheckNear(leftPx, colLeft, 0.5f, $"{what} 列左 = {colLeft}px（= 205+555c；按渲染矩形左沿量，裁切不改变它）");
  ```
  （`localPosition.x` 与 `WorldW` 都在世界单位、父链 `lossyScale = 1`（§A332 那条前提已逐件断过）⇒ ×108 即画布 px。）

> 备选（不推荐）：断言「可见宽 = `min(colLeft+w, 1920) − max(colLeft, 167.04)`」。它把 `1920/167.04`
> 这两个原版视口值写进 `CheckCard`（可接受），但**和我们的「物理缩 quad」实现绑得太紧**，
> 将来若改成真正的 `RectMask2D`（quad 保持整卡、由遮罩削）这条会**假红**。上一种量法两代实现都成立。

### 4. 没查清
- 没做「原版实拍 vs 我们 preview」的第四列并排比（按铁律 12 现行口径，本轮不做验收动作）。
- 如果主对话倾向「实现侧也要改」（例如卡容器 `rect` 仍想表达可见片），那要先定口径：**设计矩形 vs 可见矩形**；
  现在容器 = 设计矩形、quad = 可见矩形，两套口径并存但**没有任何断言把它们绑在一起**。

### 5. 顺手发现
🔴 **`T12` 这条新功能一条断言都没有**：`MainMenuRuntime.ModeScroll`/`RebuildModeCards`（`:2004/:2162-2165/:2170-2178`）
全仓只有定义、无自检引用（`grep ModeScroll` 只命中实现文件）。它的**唯一可观测后果**就是本轮这两条红。
建议补：滚到 `MaxOffset` ⇒ col3 两张卡 `cx2-cx1 = 535`（整卡进视口）、col0 被左缘裁（可见宽 = `900−…`）、
以及 `Panel`/`Hit` 跟着裁（`Hit` 现在确实只剩 50px 宽，与 `RectMask2D` 的射线语义一致）。

---

## G3 · `ToggleHit` 命中区「没建出来」

### 1. 结论
**β（实现缺陷：读口路径错）**。节点**建了**，只是 `TogHit` 这个属性找错层。

### 2. 证据
- `Shell/PracticeModePopup.cs:955` `var tog = New(root, "Toggle", new PxRect(TogL, TogT, TogR, TogB));`
- `:990` `HitOn(tog, tog, "ToggleHit", new PxRect(TogL, TogT, TogR, TogB), ToggleGameMode);`
  —— `HitOn` 第一形参就是**父节点**（`:2368` `HitOn(Transform parent, Transform basis, …)`），传的是 `tog`
  ⇒ 节点真实路径 = **`<窗根>/Toggle/ToggleHit`**。
- `:784` `public Transform TogHit { get { return transform.Find("ToggleHit"); } }`
  —— **`Transform.Find` 只查直接子件、且不支持路径里的多级**（这里路径就是单级 `"ToggleHit"`）⇒ 恒 `null`。
- 断言：`Editor/MainMenuScene.cs:3138` `box(pw.TogHit, "命中区 `ToggleHit`", 293.52f, 57.84f, …)`
  → 日志 `menu.log:18899` `✗ （前提）命中区 ToggleHit 没建出来`。
- 历史（证明是回归而非从未生效）：
  `git show 09ecc15^:…/PracticeModePopup.cs` 里是 `HitOn(root, root, "ToggleHit", …)`（挂在窗根，属性成立）；
  **`09ecc15`（第四十五会话）** 把四处 `*Hit` 一并挪到 `tog` 下，属性没跟着改。
- 旁证：`Editor/CollectionScene.cs:4233` 用 `FindChild(pwB.transform, "ToggleHit")`（**递归**，`MenuCheck.FindChild`
  = `GetComponentsInChildren<Transform>(true)`）⇒ 那边一直能取到。

### 3. 修法
`Shell/PracticeModePopup.cs:784` 改成走真实路径（或递归查找）：
```csharp
public Transform TogHit { get { return transform.Find("Toggle").Find("ToggleHit"); } }
// 或：return MenuCheck 以外的递归写法（生产代码里不要引 Editor 的那个）：
// transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "ToggleHit")
```
⛔ 别改 `HitOn(tog, tog, …)` 的父节点 —— 那会让 `ToggleSkirmishHit` / `ToggleClassicHit` / `ToggleLabelHit`
三块也跟着搬出去（它们是按 `tog` 的坐标系落的位）。

### 4. 没查清
- 另外三颗 `Toggle*Hit` 目前**没有任何断言**（只有主命中区被 §A218 那条量），是否也要各补一条由主对话定。

### 5. 顺手发现
`HitOn` 的 `basis == parent` 守卫（`:2380-2383`）只出声、不抛；这条红**正是因为父节点换了而读口没换** ——
同族的「改父链要同步读口」在本文件至少还有一处要核（`TogHit` 是唯一命中）。

---

## G4 · FTUE 阵营卡的 `Glow`（`GlowQuad == null` → alpha 读到 −1）

### 1. 结论
**β（实现缺陷：图名空格/下划线）**，本会话新建的 `FTUESelectDeckContainer` 引入。
（不是「未初始化 / 取错通道」—— quad 根本没建出来，所以断言读到的 `-1` 是它自己的哨兵值。）

### 2. 证据
- `Shell/FTUESelectDeckContainer.cs:116` `public const string GlowArtName = "Glow UI W40K";` —— **空格**。
- `:176` `var glowTex = Tex(GlowArtName, …);` → `:300-312` `Tex()` 走 `CardArt.MenuUi(name)`。
- `Core/CardArt.cs:581-592` `MenuUi` **原样拼路径**（`Root + "ui_menu/" + name` → `ui_deck/` → `ui/`），
  **不做空格→下划线归一**。落盘约定是「原版名、空格换下划线」——
  `Unity/工具/import_original_art.py:1619` `name.replace(' ', '_')`，`import_altstyle_logos.py:27` 同口径。
- 盘上真名：`Assets/CardPresentation/Resources/Art/ui_menu/Glow_UI_W40K.png`（存在，**下划线**）。
- 运行时：`menu.log:65196` `[FTUE] 图取不到：\`Glow UI W40K\`（\`Glow\` 的阵营光晕）⇒ 这一件没画`。
- 断链：`MenuDraw.Rect` `:1531` `if (tex == null) return null;` ⇒ `_glowQ = null`
  ⇒ `MainMenuScene.cs:12798` `GlowQuad != null ? CW(GlowNode) : -1f` 得 `-1`；
  `:12800-12805`（glowColor / alpha 0）、`:12835`（选中后 alpha 1）全挂。
  `GlowNode` 自己建了（`:12837` 的 `localScale = 1.1` 那条**通过**）。

### 3. 修法
`Shell/FTUESelectDeckContainer.cs:116`：`"Glow UI W40K"` → **`"Glow_UI_W40K"`**。
（与 `Core/CardView.cs:1238` 的 `CardArt.MenuUi("Glow_UI_W40K")` 同一条写法，那边一直是对的。）

### 4. 没查清
- 无。修完四条应全绿（`View.Glow` 的 rgb 数据本身由 prefab `starterDecks[].glowColor` 灌，已有断言在盯）。

### 5. 顺手发现（同一个根因，同一文件）
`:106` `public const string ShadowArtName = "40k_Smooth shadow background";` —— **也是空格**。
盘上真名 = `Resources/Art/ui_deck/40k_Smooth_shadow_background.png`（下划线；`MenuUi` 的 `ui_deck` 兜底能命中）。
日志同一段 `menu.log` 里还有一条 `[FTUE] 图取不到：\`40k_Smooth shadow background\`（\`Army Icon Shadow\` 的底）`。
⇒ **`Army Icon Shadow` 这一层现在没画**（目前没有断言盯着它，所以是**静默**的视觉缺口）。
建议一并改成 `"40k_Smooth_shadow_background"`。

---

## G5 · 队伍名字号 `85.286 ≈ 82.100`

### 1. 结论
**α（断言错）**：原版这颗 TMP **开着自适应**，`82.1` 是序列化 nominal；断言却拿 TMP 的**收敛值**（`FontPxNow`）比 nominal。

### 2. 证据
- 原版 prefab 现读（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1382404294116747836.json`，
  另两份 `-2222072736603984378` / `-5514717903917896186` 逐值相同）：
  ```
  m_text = Red Team
  m_fontSize = 82.0999984741211   m_fontSizeBase = 36.0
  m_fontSizeMin = 18.0            m_fontSizeMax = 86.4000015258789
  m_enableAutoSizing = 1
  ```
- 我们侧的常量与调用：`Shell/ParentEventWindow.cs:244`
  `P1TeamFont = 82.1f, P1TeamBase = 36f, P1TeamMin = 18f, P1TeamMax = 86.4f`；
  `:676-677` `MenuDraw.TextBox(…, P1TeamFont, P1TeamMin, QPageText, P1TeamMax, P1TeamBase, face: LatinCardBody)`
  ⇒ `MenuDraw.TextBox:1940-1943` 因 `fontPx(82.1) > autoMinPx(18)` **真的开了 `SetAutoFitBox(w,h,18,86.4,36)`**。
- `Battle/Label.cs:861` `FontPxNow => FontSizeToPxNow(_tmp.fontSize, ActualFace)` —— 读的是
  **TMP 二分之后的 `fontSize`**（`SetAutoFitBox` 里 `enableAutoSizing = true` + `ForceMeshUpdate()`，`:1126-1134`）。
- 实测 85.286 落在窗口 `[18, 86.4]` 内 ⇒ 这不是「越界」，是**正常的收敛**（框宽够、字短 ⇒ 往上长）。
- 断言：`Editor/MainMenuScene.cs:13195` `CheckNear(nmA.FontPxNow, 82.1f, 0.6f, "队伍名字号 = **82.1**（prefab 实读）")`
  → `menu.log:68008` 实得 85.286。
- 同族既有正确写法（本文件就有）：`CheckFontWindow`（`:13465`）= 断 `fontSizeMin/Max`（窗口本身）；
  `CheckFontBase`（`:13484`）= 反射断 `m_fontSizeBase`；`CheckFontInRange`（`:13451`）= 断收敛值**落在窗口内**。

### 3. 修法
`Editor/MainMenuScene.cs:13195` 换成「窗口 + 起点 + 落在窗口内」三件（与 ⑧ 节 `CheckFontInRange` 同一形状）：
```csharp
var nmNode = tA.Find("Bottom Text/Team Name");
CheckFontWindow(nmNode, 18f, 86.4f, "队伍名自适应窗口 = **原版 18 / 86.4**");
CheckFontBase  (nmNode, 36f,        "队伍名 `m_fontSizeBase` = **36**（prefab 实读）");
CheckFontInRange(nmNode, 18f, 86.4f, "队伍名收敛值落在窗口内");
```
（若还想钉 nominal 82.1，用 `Label.TmpFontSize` 那一档读口，⛔ 不要用 `FontPxNow`。）

### 4. 没查清
- **原版屏幕上真实的收敛值**：要跑原版 TMP 二分才拿得到（本体已关服、且这颗的 `m_fontAsset` 是 Pragati 系资产）。
  ⇒ 只能断定「82.1 不是合法的收敛值期望」，**不能**反过来说 85.286 与原版逐像素一致。

### 5. 顺手发现
- 同一条错误写法在别处还会再犯：`FontPxNow` 适合**固定字号**的件；凡是 `autoMin/Max` 真开了的，
  一律要走窗口 / base / `InRange`。`P1TeamMax` / `P2TeamMax` / `P1Tap*` 这几颗都有自适应。

---

## G6 · 聊天消息行的渲染队列（`3302 < 3000` 与 `3000 > 3305`）

### 1. 结论
**β（实现缺陷：`MenuDraw.TextCore` 里 `SetRenderQueue` 排在 `SetFace` 之前）**；
本会话（`A1024③` 换面）给这三个标签加了 `face: LatinCardBody` ⇒ **γ 触发**。
后果不只是断言：**三个文字层现在都在 3000，比 `RowBackground`（3302）低 ⇒ 字被行底盖住**（真·画面缺陷）。

### 2. 证据（机制链）
- `Shell/MenuDraw.cs:1883-1905`（`TextCore`，`Text`/`TextBox` 共用的唯一漏斗）：
  ```
  :1887  var lb = Label.Create(...);
  :1890  lb.SetRenderQueue(q);              // ← 先设队列
  :1895  if (face != TmpFont.Face.Cjk) lb.SetFace(face);   // ← 后换字体面
  ```
- `Battle/Label.cs:883-890` `SetFace → SyncFace`；`:892-899` `SyncFace` 里 **`_tmp.font = want`**。
- TMP 源码（本机 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/`）：
  - `TMP_Text.cs:173-177` `font` setter → `m_fontAsset = value; LoadFontAsset(); …`
  - `TextMeshProUGUI.cs:1243-1263` `LoadFontAsset()`：新 font asset 的 atlas 与旧 `m_sharedMaterial` 不符
    ⇒ **`m_sharedMaterial = m_fontAsset.material;`**。
  - `Label.SetRenderQueue`（`Label.cs:229-243`）改的是 **`_tmp.fontMaterial`（实例）**；
    `Label.RenderQueue`（`:211-220`）读的也是 `fontMaterial`。`m_sharedMaterial` 一换，实例材质被丢弃
    ⇒ 队列回到字体资产共享材质的默认值 **3000**。
- 运行时：`menu.log:57701` 行底 **3302** / 信使名·时间 **3000**；`menu.log:57729` 正文 **3000** / 头像 3305。
  （`ChatPanel` 的阶梯：`QContent = 3302`（`RowBackground`，`Shell/ChatPanel.cs:56` + 建点 `:588`）、
  `QHead = 3303`（`Sender`/`Time`，`:70`）、`QAvatar = 3305`、`QText = 3306`（`Message`，`:70`）——
  同一行常量表 `:56-71`。）
- 「本批触发」证据：`git log -S"LatinCardBody" -- Shell/ChatPanel.cs` 只命中 **`3990ca6`**（第四十七会话）；
  同一提交里 `TextCore` 的 `face` 形参也是新加的（`git log -S"if (face != TmpFont.Face.Cjk) lb.SetFace(face)"` 同）。

### 3. 修法
`Shell/MenuDraw.cs` 的 `TextCore`：**把 `lb.SetRenderQueue(q)` 挪到 `SetFace(...)` / `SetGlyphHeight(...)` 之后**
（字号那一步不换材质，但一起挪最稳）：
```csharp
var lb = Label.Create(...);
if (face != TmpFont.Face.Cjk) lb.SetFace(face);
if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
lb.SetRenderQueue(q);                 // ← 移到面/字号之后（换面会换 m_sharedMaterial、冲掉实例材质）
```
（或保留原序、但在 `SetFace` 之后再补一次 `lb.SetRenderQueue(q)`；前者少一次实例化。）
⚠️ 这条**不是只修聊天窗**：全仓 `face: TmpFont.Face.*` 调用点 **424 处**（`grep -rn "face: TmpFont.Face"`），
凡是指望 `q` 形参定层的，现在**全部落在 3000**。已经手工补过 `SetRenderQueue` 的站（如
`CampaignTab.cs:243` 的 `_title.SetRenderQueue(QHeaderTitle)`、`ForgeTab.cs:840`）不受影响 ——
它们恰好是「后补一次」的写法。

### 4. 没查清
- 除聊天窗外，**哪些站真的因为 3000 被盖住**没逐站看画面（本轮按铁律 12 不做验收动作）。
  断言侧只有这两条在盯队列，其余站点的视觉影响要另派一轮（或修完后进游戏抽查）。

### 5. 顺手发现
- `Label.SetFace` 的 doc 说「当场换掉已有的 TMP 并重排」，**没提「会冲掉实例材质/渲染队列」** ——
  建议在 `SetFace`/`TextCore` 的注释里补一句（否则下一个人还会把 `SetRenderQueue` 写在前面）。
- 同类风险还有 `SetFace` 之后 `SetText` 的那一跳（`Label.cs:888`，文本没变会早退，今天无害）。

---

## H1 · 「那一格的 `Reward` 图标量得到（有图）」

### 1. 结论
**γ（45 会话 `T21` 引入的回归）**，断言侧同时有一半是 α（几何期望也过期了）。
`FindChild(cellEdge, "Reward")` 取不到 —— 因为抽屉根节点现在**按抽屉类命名**，不叫 `Reward`。

### 2. 证据
- `Shell/ForgeTab.cs:912-931`（`BuildRewardIcon`，注释写明 **2026-10-17 T21** 改走 `ItemDrawer`）：
  ```
  :920  var spec = ItemDrawer.Spec("Wildcard" + Selected + rw.WildcardRarity, rw.Art, rw.Label);
  :924  st.NodeName = ItemDrawer.PickDrawer(spec, DrawerOverride.Default) ?? "Reward";
  :925  var res = ItemDrawer.Draw(holder, holderR, spec, rw.Qty, DrawerOverride.Default, st);
  ```
- `Shell/ItemDrawer.cs:849-853` `Draw()` 用 `style.NodeName` 建根节点；
  `:260` `DrawerWildcard = "WildcardDrawer"`、`:262` `WildcardIconDrawer`、`:264` `IconDrawer`
  ⇒ 节点名 **一定不是 `Reward`**（`Reward` 只是 `??` 的兜底，而 `Wildcard` 一定答得出）。
- 断言：`Editor/RewardsScene.cs:4425-4427`
  `var rwEdge = FindChild(cellEdge, "Reward"); … CheckTrue(rwQ != null && rwQ.Texture != null, …)`
  → `rewards.log` 该条红；紧邻的两条（`< 整块宽` / `uv 跟着截`）与下半段「对照」都被 `if` 静默跳过。
- 几何也过期：旧实现是「高 150 等比」（`:936-940` 那段兜底分支），新抽屉是
  `ItemDrawer.Square(box, st.IconFill)`（`ItemDrawer.cs:1278-1282`）⇒ **282.77 见方的方形**
  （`RewardsWindow.Node` 的 `RewardTransform` = 297.655×384.66，`ForgeTab.cs:826-829`；`st.IconFill = 0.95`，`:922`）。
  所以 `fullW = 150 × texW/texH`（`RewardsScene.cs:4431`）**这一条即使把节点找对也会红**。

### 3. 修法
两处一起改：
1. **取件**（`Editor/RewardsScene.cs:4425`）：`FindChild` 是**递归**的（`MenuCheck.FindChild`）
   ⇒ 直接取抽屉里那颗图标节点 `ItemDrawer.NodeIcon`（`ItemDrawer.cs:274` = `"Icon"`）：
   ```csharp
   var rwQ = FindChild(cellEdge, "Icon")?.GetComponentInChildren<ImageQuad>();   // 抽屉里的 Icon（Wildcard/Icon 两档同名）
   ```
   （或把 `ForgeTab.cs:924` 的 `st.NodeName` 固定成 `"Reward"` —— 但那样与原版「抽屉 prefab 实例化后保留自己名字」不符，
   且 `CampaignTab.cs:553` 用的是 `"Item Drawer"`，两处口径又不一致。**建议改断言**。）
2. **整块宽**（`:4431` 与对照那半段 `:4447`）：改成新几何
   `fullW = Mathf.Min(297.655f, 384.66f) * 0.95f = 282.7723f`（方形）——
   ⚠️ `0.95` 是**我们挑的**（`ForgeTab.cs:922` 注释已如实标注），不是原版值。

### 4. 没查清
- `ForgeData.RewardAt(lvV)` 落在 `WildcardDrawer` 还是别的真表抽屉，没在运行时打印过（代码路径上一定非 `"Reward"`，
  但 `DrawerOverride.Default` 与 `ItemTypeOf` 的真表那一跳是否命中、命中哪个，要看 `[ItemDrawer]` 那条日志）。
  建议修的时候顺手在自检里把 `res.Drawer` 打出来。
- 「压在视口边上那格」的 `Icon` 在裁切后是否 `UvRect.width < 1` 没实测（代码路径看起来会，但没跑到）。

### 5. 顺手发现
- `RewardsScene.cs:4445-4454` 的下半段「对照：完全落在视口里的那一格」**没有前置断言** ——
  `rwIn` 取不到时整段**静默跳过**（今天就是这种情况）。同族纪律（本文件到处写「（前提）…建了」）
  要求先断 `rwInQ != null`，否则「对照」永远不跑也看不出来。

---

## H2 · 「进度条宽 110」（实得 10.00）

### 1. 结论
**α（断言量错对象）**：滑条是**九宫格三块**，`RectOf` 取子树里**第一颗** `ImageQuad` = **左端块**，
其宽正好 = `borderOut.x = 20/2 = 10`。

### 2. 证据
- `Shell/CampaignTab.cs:101-102`：
  ```
  static readonly Vector4 ArmyBarBorder    = new Vector4(20f, 0f, 20f, 0f);
  static readonly Vector4 ArmyBarBorderOut = new Vector4(20f / 2f, 0f, 20f / 2f, 0f);   // = 10px 端块
  ```
- `:764-767` `BuildArmySlider`：`band = PxRect(r.x1 … r.x2)`（r = 条目宽 120 − 两侧各 5 ⇒ **宽 110**），
  然后 `_win.Nine(item, "40k_campaign_bar_bg", band, ArmyBarBorder, …, "Slider/Background", ArmyBarBorderOut)`。
- `MenuDraw.Nine`（`MenuDraw.cs:1612-1649`）→ `ImageQuad.CreateNineSlice(...)`：横向 border `(20,0,20,0)`、
  `borderOut = (10,0,10,0)`、`fillCenter = true` ⇒ **3 块**：左端 10 / 中段 / 右端 10。
  （同一机制在模式卡那条探针里看得到：`Border_00 2.6×2.6 / Border_01 2.6×409.2 …`，即「九宫格会拆成多颗 quad」。）
- 断言用的 `RectOf`（`Editor/RewardsScene.cs:623-637`）：
  `var q = t.GetComponentInChildren<ImageQuad>();`（**只取第一颗**）→ `w = q.WorldW * 108f`
  ⇒ 命中左端块：**宽 10.00、高 12**（高那条 `:5264` 因此**通过**，正好互相印证）。
- 断言：`:5249-5263`
  `var sl = FindChild(it0, "Slider/Background"); … CheckNear(x2-x1, 110f, 1f, "进度条宽 110")`
  → `rewards.log` 实得 `10.00`。
- 排除了「被视口裁」：同一格的 `HighlightBG`（`:5229-5230`）宽 120 **通过**、`Icon` 100×100 也通过
  ⇒ `it0`（= 选中格 `#0 Ultramarines`，`menu.log` 同段）完全在视口内。

### 3. 修法
`Editor/RewardsScene.cs:5263` 的取件换成**并集**（本文件现成有 `RectOfUnion`，`:664-674`，
注释就是「多块拼起来」那一档）：
```csharp
if (FindChild(it0, "Slider/Background") != null &&
    RectOfUnion(FindChild(it0, "Slider/Background"), out x1, out y1, out x2, out y2))
{
    CheckNear(x2 - x1, 110f, 1f, "进度条宽 **110**（原版 `sizeDelta.x = −10`）");
    CheckNear(y2 - y1,  12f, 1f, "进度条芯高 **12**…");
}
```
（`RectOfUnion` 在本窗用 `QuadGate.Self` —— 端块/中段都 `activeSelf == true`，今天零差异。）

### 4. 没查清
- 换成 `RectOfUnion` 后是否**恰好 110**：没跑过。理论上 = `band.W = (120−5) − 5 = 110`；
  但 `item0` 若被左缘裁到（`Campaign Army Selector/Viewport` 有 `soft=(0,0) pad=(0,0,0,0)`），
  并集会小于 110 —— 需要在复跑时看一眼实得值（若 < 110，说明 item0 贴边，得先 `FocusSelectedArmy()`
  或把取件换成一颗**完整落在视口里**的条目）。

### 5. 顺手发现
- 同族的 `Campaign` 进度条**填充/描边**三块都在，但只有 `Background` 被量；`Fill` 的宽度（按 `frac`）
  与 `Outline` 都没断言 —— 今天 `HasContent` 只有 UM 有内容，补断言时要挑对格子。
- `RectOf`「只取第一颗 quad」这个坑在本文件还有别的 Nine 站（凡是用 `RectOf` 量九宫格节点的都会偏小），
  建议在 `RectOf` 的 doc 上写死「**九宫格节点请改用 `RectOfUnion`**」（现在两段的 doc 都没互相指路）。

---

## 附：修法落点清单（给主对话排期用）

| 文件 | 位置 | 改什么 | 对应 |
|---|---|---|---|
| `Editor/MainMenuScene.cs` | `CheckCard`（`:14459-14475`） | 宽量容器 `RectTransform`、列位量渲染左沿 | G1/G2 |
| `Shell/PracticeModePopup.cs` | `:784` | `TogHit` 路径补 `/Toggle/` | G3 |
| `Shell/FTUESelectDeckContainer.cs` | `:116`（+ 顺手 `:106`） | `Glow_UI_W40K` / `40k_Smooth_shadow_background` | G4 |
| `Editor/MainMenuScene.cs` | `:13195` | 换 `CheckFontWindow` + `CheckFontBase` + `CheckFontInRange` | G5 |
| `Shell/MenuDraw.cs` | `TextCore` `:1890` | `SetRenderQueue` 挪到 `SetFace` 之后 | G6（影响 424 站） |
| `Editor/RewardsScene.cs` | `:4425` / `:4431` / `:4445-4454` | 取 `"Icon"`；整块宽改 282.7723；对照段补前置断言 | H1 |
| `Editor/RewardsScene.cs` | `:5263` | `RectOf` → `RectOfUnion` | H2 |

> ⚠️ 这四件改完按覆盖面复跑：`MainMenuScene.Run`（G1–G6）· `RewardsScene.Run`（H1/H2）。
> `MenuDraw.TextCore` 是共用件 ⇒ 按铁律 12 还要带 `ShellScene` / `ShopScene` / `CollectionScene`
> （它们也吃 `Text`/`TextBox` 的队列），至少 `ShellScene` + `ShopScene`。
