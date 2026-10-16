# EW4 · HUD 字族迁 uGUI（T09 块 3）

> 白名单 = `Battle/BattleDriver.cs` + `Battle/Label.cs` + 本报告。⛔ 未跑 Unity、未动 git、未改白名单外任何文件。
> 全部行号**现核**（2026-10-16 · 本报告写作时；⚠️ 写完后又做了一次 doc 结构整理 ⇒ **表里少数行号 +1~+3**，
> 另：并行代理正在改别的文件 ⇒ 定位请一律**以标识符为准** —— 本报告每处都带了名字）。
> 最后一次 `TMPDIR=/tmp/wf_ew4 bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**（在**最后一次改动之后**跑的）。

---

## ① 结论

### 1·a 🔴 **没有「半迁移态」—— 21 个建字点是【一个口】全迁的**（调度台最关心这一条）

**关键事实**：`BattleDriver.cs` 里 HUD 的字**不是 21 处各建各的**，而是**全部走同一个私有助手 `Hud(...)`**
（`:16297` 那一族，唯一口；`_hudLabels` 只在它里面 `Add`）。EW4 改的就是**那一个口** + 4 处外围（见 ②），
⇒ **21 处调用点【一行都没改】，它们同时从「世界空间 `Label.Create`」变成「画布档 `Label.CreateUgui` + 归一化锚点」**。
⇒ **不存在「一部分在画布、一部分在世界」**的状态（那种状态比两边都不迁更坏，也是本次中途被叫停时的担心点）。

**21 个建字点逐个（现核 `grep -n "= Hud("`；按文件顺序）** —— 全部**已迁**：

| # | 建的是什么字 | 标识符 | 行 | 锚点来源 |
|---|---|---|---|---|
| 1 | 回合行（「第 N 回合 YOUR TURN」；**我们自加**） | `_turnLabel` | `:13959` | `0.5f, 0.810f` |
| 2 | 敌方名牌上的名字 | `_enemyText` | `:14029` | `228.17147f/1920f, 1f−66.88264f/1080f` |
| 3 | 我方名牌上的名字 | `_myText` | `:14042` | `225.74343f/1920f, 1f−1002.47582f/1080f` |
| 4 | 里程碑 `x N` | `_skullScore` | `:14067` | `SkullScoreX01/Y01` |
| 5 | 我方信仰数 | `_myFaithText` | `:14172` | `MyFaithX01/Y01` |
| 6 | 敌方信仰数 | `_foeFaithText` | `:14175` | `FoeFaithX01/Y01` |
| 7 | 我方灵魂石数 | `_myStoneText` | `:14194` | `MyStoneX01/Y01` |
| 8 | 敌方灵魂石数 | `_foeStoneText` | `:14197` | `FoeStoneX01/Y01` |
| 9 | 我方能量 `N/M` | `_energyLabel` | `:14261` | `MyEnergyLabelX01/Y01` |
| 10 | 敌方能量 `N/M` | `_foeEnergyLabel` | `:14268` | `FoeEnergyLabelX01/Y01` |
| 11 | 手牌数 | `_handLabel` | `:14288` | `0.075f, 0.158f` |
| 12 | `END TURN`（**还兼命中区**） | `_endTurnLabel` | `:14326` | `EndTurnX01/Y01` |
| 13 | 回合时钟 | `_clockLabel` | `:14331` | `EndTurnX01, EndTurnY01+0.021f` |
| 14 | 我方牌库张数 | `_pileLabel` | `:14444` | `mySizeX/Y`（`MyDeckX01+DeckSizeDxMine/1920f` …） |
| 15 | 敌方牌库张数 | `_foePileLabel` | `:14447` | `foeSizeX/Y` |
| 16 | 提示条那颗字（原版 `AboveShader/TipText`） | `_tipText` | `:14516` | `tipX01/Y01`＝`(960+1.06)/1920, 1−(540−91.78)/1080`，**父件 = `AboveShader` 子画布** |
| 17 | 结算/结束那行大字 | `_resultLabel` | `:14542` | `0.5f, 0.5f` |
| 18 | 我方称号 | `_titleMe` | `:15035` | `217.62721f/1920f, 1f−1038.15523f/1080f` |
| 19 | 敌方称号 | `_titleFoe` | `:15038` | `217.92701f/1920f, 1f−102.402645f/1080f` |
| 20 | 我方任务点 `N/3` | `_qpTextMe` | `:15364` | `1865.8381719470285f/1920f, …` |
| 21 | 敌方任务点 `N/3` | `_qpTextFoe` | `:15367` | `1864.9734582742763f/1920f, …` |

**另有 1 处建字（不计入 21）= `_overtimeSplashText`（`:15532`）** —— 它在 `T09` 批 3 就已经是
`HudCanvas.MakeText`（加时 splash `OVERTIME!`，父件 = `AboveShader` 子画布下的 `OvertimeSplashText` 整屏节点），
**本件一个字没动**。⇒ `BattleDriver.cs` 里如今**再无** `Label.Create` 的调用点（只剩注释里提到它，见 `:1048/:1079/:14482`）。

### 1·b 「有意不缩 / 不迁的那三处」逐条裁定（`T09` §五·补⑦）

| §五·补⑦ 列的三处 | 本件裁定 | 判据 |
|---|---|---|
| ① **加时 splash 那一组** | ⏸ **不迁**（本件没碰它）—— ⚠️ **但它那条理由已经过期**：splash **整组早在批 3 就上了 `AboveShader` 子画布**，那颗字是 `HudCanvas.MakeText`（`:15532`），不是「还没迁」。它今天和它的底板**同容器同尺** ✓ ⇒ 结论不变、理由要换 | `BuildOvertimeSplash`（`:15502` 起）逐句现读 |
| ② **卡面文字** | ⏸ **不迁** ✓ —— 卡面 TMP 由 `CardView`/`CardFace` **直接建**（不经 `Hud()`，也不是 `Label` 那个口），卡是**世界物件** | `Hud()` 的调用点表里没有它 |
| ③ **还没迁的面板内部的字** | ⏸ **不迁** ✓ —— 面板的字一律**不经 `Hud()`**（各面板自己的 `Create`）。本件只动了 `Hud()` 这一个口 ⇒ 面板一个字没碰 | `grep "HudCanvas.MakeText\|Label.Create" Battle/*.cs`：`WaitBanner.cs:280` **仍是 `Label.Create`**（`WaitText` 那条属于**面板族**、还没轮到的批，见 ⑤） |

⇒ **没有一处「按 §五·补⑦ 有意不迁」的调用点落在 `Hud()` 的 21 处里** —— 21 处**全部是 HUD 上、非卡非面板的件**，
正是 §五·补⑧ 那条判据（「它是不是画在 HUD 正交平面上的、非卡非面板的静态件」）要的下一批。

### 1·c `HudTextScale` 退没退：**消费者【只剩自检断言】⇒ 代码侧已退场，本体待删（不在白名单）**

`grep -rn "HudTextScale" --include=*.cs`（全仓，现核）只剩三档：

| 谁 | 处 | 处置 |
|---|---|---|
| **`LayoutSpace.HudTextScale` 本体** | `Core/LayoutSpace.cs:67-75` | ⛔ **不在 EW4 白名单** ⇒ **未删**。**退场条件** = 下面那两条断言改掉之后，**连同它的 doc 一起删**（`T09` §五 第 5 步原文：「同批退掉 `HudTextScale`」） |
| **两条断言** | `Editor/BattleScene.cs:1905`（断 `HudTextScale == 1`）· `:1909`（断「HUD 标签真的乘上了」） | ⛔ 不在白名单 ⇒ 写进 ③ 接线清单。🔴 **第一条今天就是红的**：它写着「本批断言跑在 16:9」，而公式读的是 `Screen`（批处理 **640×480 = 4:3**）⇒ 实得 **0.75**（不是 1）。第二条在本件之后**结构上不可能成立**（字不再乘任何系数） |
| **说明性引用（不是消费者）** | `Battle/HudPiece.cs:182-184` · `BattleDriver.cs` 里 5 处「留痕」注释 · `T09` 文档 | 保留（那是判据/留痕，删了下一个会话会重新踩） |

**本件在 `BattleDriver.cs` 里已把它的两个消费点一起摘掉**：
① `Hud()` 里那句 `l.transform.localScale = Vector3.one * LayoutSpace.HudTextScale;`（**删**）
② `ReanchorHud` 里「HUD 标签那一整轮重贴」（`localPosition = ToWorld(spot)` + 上面那个系数，**整轮删**，`:16539` 留痕）
⇒ **`BattleDriver.cs` 现在一个消费者都没有**（`grep HudTextScale Battle/BattleDriver.cs` 命中的全是注释）。

---

## ② 改动清单（逐处 `文件:行号` + `git diff --numstat` + 行尾核对）

### 2·a `Battle/BattleDriver.cs`（我的改动，全部现核）

| 处 | 行 | 改前 → 改后（一句话） |
|---|---|---|
| **`Hud()` 体 = 那一族的唯一建字口** | `:16314-16347` | `Label.Create(root, text, LayoutSpace.ToWorld(x01,y01), …)`（**世界档**）→ **`Label.CreateUgui(cv, text, Vector3.zero, …)` + 显式 `anchorMin = anchorMax = (x01,y01)` + `anchoredPosition = 0`**；父件 = `root as RectTransform`（= 传进来的子画布）否则 `HudCanvas.Ensure(cam)`；`HudCanvas.Add(rt, HudLabelZ)` 定兄弟序；`SetAutoFitBox` **去掉 `Px(...)`（原样 px）** |
| **`Hud()` 的乘法系数** | 同上 | 删 `l.transform.localScale = Vector3.one * LayoutSpace.HudTextScale;`（画布统一缩，不再手乘） |
| **`Hud()` doc 末段（新增）** | `:16300-16307` | 新增 5 行：父件 = 画布 / 位置 = 归一化锚点 / `boxWpx` 等一律**设计 px** / 过渡系数在文件内再无消费者 |
| **新增常量 `HudLabelZ = 0f`** | `:16354-16364` | 字在容器里的兄弟序档（旧那套的世界 z 0 ⇒ 压在 `HudImageZ`(0.3) 的图之上）；doc 里写明「同容器里 splash 根也是 0 ⇒ 那两者先后由创建顺序定（跟今天一样）」 |
| **`ReanchorHud`：删掉标签那一轮** | `:16539-16547` | 原来 8 行（`localPosition = ToWorld(spot)` + `localScale = HudTextScale`）**整轮删**，留 9 行留痕（为什么删 + ⛔ 别加回来 + `HudTextScale` 待删） |
| **`_hudSpots` 删 + 注册表 doc 重写** | `:16141-16146` | 那份「重贴用坐标备份」随上面那一轮一起删（它是**为世界档重贴**存在的）；`_hudLabels` **留着**（自检读口用） |
| **`PlaceHandPlate` 重写** | `:15873-15894` | 旧：读 `transform.localPosition`（世界）+ `WorldW×k`（k = `localScale.x`，给过渡系数的补偿）→ `ToNormalized` → `SetAnchorPosition`。新：**板与字同父同锚 ⇒ `板.anchorMin/Max = 字.anchorMin` + `板.anchoredPosition = (字.WorldW/2, 字.WorldH/2)`**（px、y 向上）。⇒ **`k` 那一项（错位 25% 的补偿）同时退场** |
| **`_handLabel.SetAutoFitBox` 单位** | `:14295-14299` | `LayoutSpace.Px(HandLabelBoxW/H)`（÷108，**世界单位**）→ **`HandLabelBoxW/H` 原样**（234/64 **本来就是 px**，见 `:1462` 那条 ×100 桥） |
| **称号两颗 `SetGlyphHeight`** | `:15041-15045` | `SetGlyphHeight(TitleFontPx / 108f)` → **`SetGlyphHeight(TitleFontPx)`**（30.55 **本身就是原版画布 px**；那个口收「本标签所在空间的单位」） |
| **`TitleFontPx` 的 doc** | `:16377-16380` | 原文写「走 `SetGlyphHeight(px/108)`」→ 补 3 行说明两档各传什么 |
| **`_tipW` 字段 doc** | `:1025-1027` | 「（世界单位）」→「（**设计 px**）」+ 迁 uGUI 留痕 |
| **`HintWidth` doc** | `:17439-17444` | 单位从「世界单位 / 可见区宽 = `VisibleWidth`」→「设计 px / 画布宽 = 1920」，并点名用它那条断言要换尺（③） |
| **`HandLabelWorldW/H` doc** | `:10686-10692` | 单位 = 设计 px（框 234/64 本来就是 px）；⚠️ 别拿它比 `HudPiece.WorldH`（那个**仍是世界单位**） |
| **`PileLabelWorldW` doc** | `:15949-15952` | 同上 + 点名它那条断言**会混两把尺**（③） |
| **`PlateTextPosPx` / `MyPlateTextCenterY01`** | `:16029-16046` | 读回口：`LayoutSpace.ToNormalized(localPosition)`（**uGUI 档量纲就错了**）→ **读 `rt.anchorMin`**（锚点就是它在屏幕上的位置；16:9 下与旧写法**逐位相同**） |
| **`HudLabelScaleForTest` doc** | `:16179-16184` | 注明**迁后恒 1**、原意没了、只为编过（`BattleScene.cs:1908` 还在调它）+ 该换成哪两个新口 |
| **新增自检口 ×3** | `:16192-16219` | `HudLabelCount`（颗数，现读 21）· `HudLabelAnchorForTest(i)`（读回归一化锚点；越界 ⇒ `(-1,-1)`）· `HudLabelOnCanvasForTest(i)`（**断父链**：父 = 根画布或根画布下的子画布 —— 灭自证用，因为 `hudRoot` 是裸 `Transform`、挂它下面的字**也有** `anchorMin`） |
| **`HudImageTex` 认「画布 root」** | `:16446-16454` | `var parent = HudCanvas.Ensure(cam);` → **`var parent = root as RectTransform; if (parent == null) parent = HudCanvas.Ensure(cam);`**（详见 ⑥·2：提示条底板原来建在**根画布**、没进 `AboveShader` 子画布） |
| **提示条那一段的过期注释订正** | `:14480-14488` | 那句「下面 `HudImage`/`Hud` 的 parent 从 `root` 改成这块画布 —— 它们内部走 `HudCanvas` ✓」**当时不成立**（见 ⑥·2），已按事实改写 |

### 2·b `Battle/Label.cs`（共用件；本件里改的都是**画布档专属**的分支，世界档逐位不变）

| 处 | 行 | 改前 → 改后 |
|---|---|---|
| 🔴🔴 **新增 `UguiFontScale = 10f`** + 两处用它 | `:1141-1159`（常量 doc）· `TmpFontSize()` `:1161-1174` · `FontSizeToPxNow` `:785-794` | **画布档的字号换算原来是错的（差 10 倍）** —— 判据（TMP 源码）：`TMP_Text.cs:7183` `fontScale = … × (m_isOrthographic ? 1 : 0.1f)`，`TextMeshProUGUI.cs:802` 把 `m_isOrthographic = true`；而 `TmpFont.WorldGlyphPerFontSize`/`WorldCapPerFontSize` 是**拿 3D `TextMeshPro` 量的**（`TmpFont.MeasureGlyph` 里 `AddComponent<TextMeshPro>()`）⇒ 对画布档**小 10 倍**。修法 = `TmpFontSize()` **÷10**、`FontSizeToPxNow` **×10**（同一常量、一处定义）。详见 ⑥·1 |
| **新增 `MeasureBoundLimit`（唯一一份量纲上限）** | `:1262-1267` | `UsableBounds` 原来写 `lim = 100f * PxPerUnit` —— 而 `PxPerUnit` = 世界 108 / uGUI **1** ⇒ 实际给的是「世界档 10800（松 108 倍）· uGUI 档 **100**」，**与它自己的注释正好相反** ⇒ uGUI 档**任何宽过 100 px 的标签都判成「没量到」**（`A1751` 那条哨兵守卫把它吃掉、不出声）。现在两档都走 `MeasureBoundLimit`（世界 100 · uGUI 10800） |
| **`HasMeasuredWidth` 同一处** | `:1541-1556` | 写死 `w <= 100f` → `w <= MeasureBoundLimit`（同一个变量的第二个消费者，**同一份判据**）。不修的话：uGUI 档**宽过 100 px 的字静默不对齐**（`AlignLeftOn`/`AlignRightOn`） |
| **`VOffsetBlockWorld` / `OurInkCenterWorld` / `OneLineBoxWorld` 的三条守卫** | `:1669,1673`（doc）· `:1694` · `:1766` · `:1820` | 同族的另外 3 处 `100f` → `MeasureBoundLimit`（A712 垂直档那条路；不修的话 uGUI 档「块高 > 100 px」的件会被当成哨兵） |

### 2·c 行尾核对（**二进制读**，`io.open(p,'rb')`）

```
MyGame/Assets/CardPresentation/Battle/BattleDriver.cs   CRLF=17625  LF=17625   ⇒ 纯 CRLF，未翻 ✓
MyGame/Assets/CardPresentation/Battle/Label.cs          CRLF=1961   LF=1961    ⇒ 纯 CRLF，未翻 ✓
```

### 2·d `git diff --numstat`（逐文件）

```
409  53  Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs
 84  17  Unity/MyGame/Assets/CardPresentation/Battle/Label.cs
```

🔴 **这两个数不是本件的一半** —— 两个文件在本件开工**之前**就已经是 `M`（任务开头的 `git status` 里就挂着），工作区里还压着**别的写手的未提交改动**：
· **`BattleDriver.cs`**（逐 hunk 现核，起点行号）：`4189`/`4229`（`W36` · `A1768` 语音线）· `12333`（`W26` · `A1731` 特效线）· `14520`/`14570`/`15280`/`15310`/`15436`/`15487`（`T09` **批 3** 与**收口波 `G1`**：`W7`/`W55`/`W42`/`W43`/`W50`）；
· **`Label.cs`**：`:156` / `:162` 两处（**`CreateCore` 里「`localPosition` vs 父件枢轴」那段注释订正**，别的写手，与本件无关）。
**我的改动 = ② 那两张表里的那些行**（每条都带标识符，可 `grep` 定位）。

---

## ③ 接线清单（**白名单外**谁该改哪一行）

### 3·a `Editor/BattleScene.cs`（照本件的新口径改断言）

| 处 | 今天是什么 | 该改成什么（判据） |
|---|---|---|
| `:1903-1910`（**两条**） | ① `Check(\|HudTextScale − 1\| < 1e-4)` ② `float hl = drv.HudLabelScaleForTest(0); Check(hl ≈ HudTextScale)` | 🔴 **两条一起删**：① 在批处理（640×480）下**本来就是红的**（`HudTextScale` 实得 **0.75**，那条注释写的「本批跑在 16:9」是把 `cam.aspect` 当成了 `Screen`）；② 本件之后 `hl` **恒 1**（字不再乘任何系数）⇒ **结构上不可能成立**。**换成**下面 3·a-2 那组 |
| 🆕 该**加**（这一条才是本片真正的灭自证） | 无 | `Check(drv.HudLabelCount == 21, …)` 然后**逐颗**：`drv.HudLabelOnCanvasForTest(i)`（父链在画布上 —— 断「没留在 `hudRoot` 下」）＋ `drv.HudLabelAnchorForTest(i)` 与一个**自带的期望值表**逐位比（**⛔ 别从实现里读期望值** = 自证）。⚠️ 期望值取 ① 那张表的「锚点来源」列（21 条，其中 5 条是具名常量：`SkullScoreX01` · `My/FoeFaithX01` · `My/FoeStoneX01` · `My/FoeEnergyLabelX01` · `EndTurnX01` · `mySizeX/foeSizeX` · `tipX01`） |
| `:9350`（提示条宽度） | `Check(driver.HintWidth > 0 && driver.HintWidth < LayoutSpace.VisibleWidth * 0.9f)` | 右边换 **px 尺**：`< 1920f * 0.9f`（`VisibleWidth` 是世界单位 = 17.78）—— 不改的话左边现在是**几百 px**、右边只有 **16.0** ⇒ **这条当场红**（⛔ 别以为它「更松所以还是绿的」） |
| `:2028-2029`（牌库张数） | `drv.PileLabelWorldW < drv.MyDeckSizePlateWorldH * (442f/112f) − 0.05f` | 🔴 **混两把尺**：`PileLabelWorldW` 现在是 **px**（几十~几百），`MyDeckSizePlateWorldH` 走 `HudPiece.WorldH`（`GetWorldCorners`）**仍是世界单位**（≈0.547）⇒ 右边再乘 3.946 也只有 **2.16** ⇒ **恒红**。两个改法选一：① 左边改用 px 的底板宽（`HudCanvas.SizePx(_myDeckSizePlate.Rect).x`，本件没暴露读口 ⇒ 要先加口）② 右边换成 px 常量（原版那块板 **238.5 px** 宽，见 `DeckSizeDx*`/`MyDeckSizePx` 那组） |
| `:20173-20176`（手牌数框） | `HandLabelWorldW ≤ LayoutSpace.Px(234f) + 1e-4f`（`Px(234)` = 2.1667 世界） | 左边现在是 px ⇒ 右边换 **`234f` / `64f`** 原样（`HandLabelBoxW/H` 本来就是 px） |
| 任何用 `Label.FontPxNow` / `FontSizeToPx` 量 **uGUI** 标签的地方 | 静态 `FontSizeToPx` = **世界档口径** | 改走**实例口 `FontSizeToPxNow(fontSize, face)`**（`Label.cs:785`，按所在空间；本件已把它在画布档补齐 ×10）—— 用静态那条会「拿世界尺量 px 值」**恒差 108 倍** |

### 3·b `Core/LayoutSpace.cs`

| 处 | 处置 |
|---|---|
| `:48-75` `public static float HudTextScale`（+ 那一大段 doc） | **删**（`T09` §五 第 5 步明的「同批退掉」）。**退场条件已满足**：本件把 `BattleDriver.cs` 的最后两个消费点删掉之后，全仓只剩 `BattleScene.cs:1905/1909` 两条断言引用它 ⇒ **那两条改完（3·a-1）即可删**。🔴 **不许静默留着** —— 留着的后果是下一个会话看见它、以为还有世界档的 HUD 字需要它 |

### 3·c 别的文件（本件只报不改）

| 处 | 该改什么 | 判据 |
|---|---|---|
| `Battle/WaitBanner.cs:280` | 仍 `Label.Create`（**世界档**）：`WaitText` 那条提示条的字 | 它属**面板族**（`T09c` 映射表：`WaitBanner → WaitText` @ `BackCanvas`/`Default`/0）⇒ 按 §五·补⑨ 三「面板不能先于它内部的字单独迁」**整扇**留给后面的批。⛔ 本件不动它，但要如实记：**HUD 上仍有 1 处世界档的字** |
| `Battle/TutorialOverlay.cs:208-215` | 已按 `CapPxPerScale = 7.56` 办了「两档渲出来一样」——**本件的 ×10 修（②·b 第一条）恰好是它成立的前提**（见 ⑥·1 末段）。⚠️ 它自己**不用改**，但**要复跑验收** | `EW1_Tutorial迁uGUI_报告.md:222-223` |
| `Battle/HudPiece.cs:155-234` | 另一写手（`EW2`）刚改过（`NudgeWorld`/`SetWorldHeight` 的 `×108` → `LayoutSpace.DesignPxH/DesignHeight`，等值）—— 本件**没碰**它，但 `PlaceHandPlate` 现在走 `HudPiece.Rect` + 锚点，与它的口径不冲突 | `EW2_HudPiece换算修_报告.md` ① |

---

## ④ 判据与出处 + 16:9 逐位自证

### 4·a 关键判据（都是**读源码/读文件**，没有一处是猜）

| 结论 | 判据（可复查） |
|---|---|
| 「HUD 的字要与图**同一把尺**」 | `T09` §五·补⑦ 开头（`R = scaleFactor × 1080 / 屏高`；16:9 ⇒ 1）+ §五·补⑧ 那张表右列「HUD 的字（靠 `HudTextScale` 才跟上缩放）」 |
| **字的父件 = 画布**（⛔ 不是 `hudRoot`） | `HudImageTex` 里的现成先例（`:16446` 那段注释：⛔ 不能挂 `hudRoot` —— 那是反向补震用的）＋ `HudCanvas` 文件头第 2 条 |
| **位置 = 归一化锚点** | 图族就是这么建的：`HudImageTex` → `HudCanvas.MakeAnchored(...)` → `rt.anchorMin = rt.anchorMax = new Vector2(x01, y01)`（`HudCanvas.cs:641`）；重贴口也是 `HudPiece.SetAnchorPosition` = `anchorMin = anchorMax = (x01,y01); anchoredPosition = 0`（`HudPiece.cs:260-266`） |
| **尺寸一律设计 px** | `T09` §五·补⑨ 四（三处带单位的常数）+ §五·补⑩ 一（`MenuDraw.SetPxSize` 体内是 ÷108 ⇒ uGUI 档要真 px） |
| `Label` 的 uGUI 后端就绪 | `Label.cs:138-142` `CreateUgui` · `:1178-1186` `BuildTmp` 双后端 · `:313` `CapUnitOfScale` · `:1553` `PxPerUnit` |
| **画布档字号差 10 倍** | `…/BuiltInPackages/com.unity.ugui/Runtime/TMP/TMP_Text.cs:7183`（首选值那条同源 `:3920-3921`）· `TextMeshProUGUI.cs:802` · `TmpFont.MeasureGlyph` 用 `AddComponent<TextMeshPro>()`（3D） |
| `7 档` vs `7.56 档`（见 ⑤·1） | `Label.cs:302` `CapHeightWorldOfScale = 7f/100f` 世界单位/档 × 108 px/世界单位 = **7.56 屏 px**；uGUI 档 `CapPxOfScale = 7f`。**同一发现 → `EW1` 报告 `:222-223`** |
| 提示条底板该在 `AboveShader` 子画布 | 调用点自己的注释（`:14478` 原文）+ `T09c` §一（原版宿主 = `AboveShader`） |

### 4·b 16:9 逐位自证（位置）

**两条路径的代数恒等式**（`x01/y01` = 归一化锚点，x 从左、**y 从下**）：

- **改前（世界档）**：`localPosition = LayoutSpace.ToWorld(x01,y01) = ((x01−0.5)·VisibleWidth, (y01−0.5)·10)`；
  16:9 下 `VisibleWidth = 17.7778`、`1 世界单位 = 108 px` ⇒ 屏幕 px（左上原点、y 向下）
  = **(960 + (x01−0.5)·1920, 540 − (y01−0.5)·1080)** = **(x01·1920, (1−y01)·1080)**。
- **改后（画布档）**：`anchorMin = anchorMax = (x01,y01)` 且画布 rect 在 16:9 = **1920×1080** ⇒ 枢轴点落在
  **(x01·1920, (1−y01)·1080)** 处。
- ⇒ **两式逐字相同 ⇒ 差 = 0**（对**任意** `x01/y01` 成立，⛔ 不是「大致一样」）。

**逐颗现算**（左列 = 按改前的世界档公式算；右列 = 按 `x01·1920 / (1−y01)·1080` 算）：

| 标签 | 行 | x01 / y01（原文） | 世界档 → px | 画布锚点 → px | 差 |
|---|---|---|---|---|---|
| `_turnLabel` | `:13959` | `0.5f` / `0.810f` | (960.0000, 205.2000) | (960.0000, 205.2000) | 0 |
| `_enemyText` | `:14029` | `228.17147f/1920f` / `1f−66.88264f/1080f` | (228.1715, 66.8826) | 同 | 0 |
| `_myText` | `:14042` | `225.74343f/1920f` / `1f−1002.47582f/1080f` | (225.7434, 1002.4758) | 同 | 0 |
| `_skullScore` | `:14067` | `SkullScoreX01=0.11740` / `0.11134` | (225.4080, 959.7528) | 同 | 0 |
| `_myFaithText` | `:14172` | `MyFaithX01` / `MyFaithY01` | (1865.7984, 651.1968) | 同 | 0 |
| `_foeFaithText` | `:14175` | `FoeFaithX01/Y01` | (1865.7984, 192.5964) | 同 | 0 |
| `_myStoneText` | `:14194` | `MyStoneX01/Y01` | (1870.0992, 644.2956) | 同 | 0 |
| `_foeStoneText` | `:14197` | `FoeStoneX01/Y01` | (1868.6016, 199.8000) | 同 | 0 |
| `_energyLabel` | `:14261` | `MyEnergyLabelX01 = MyEnergyOrbX01 + (−1.5)/1920` | 见 `:1493-1506` 那条链 | 同 | 0 |
| `_foeEnergyLabel` | `:14268` | `FoeEnergyLabelX01`（同上，敌方链） | 见 `:1496-1508` | 同 | 0 |
| `_handLabel` | `:14288` | `0.075f` / `0.158f` | (144.0000, 909.3600) | 同 | 0 |
| `_endTurnLabel` | `:14326` | `EndTurnX01=0.96263` / `EndTurnY01=0.57819` | (1848.2496, 455.5548) | 同 | 0 |
| `_clockLabel` | `:14331` | `EndTurnX01` / `EndTurnY01+0.021f` | (1848.2496, 432.8748) | 同 | 0 |
| `_pileLabel` | `:14444` | `mySizeX = 0.90104+(−20.75)/1920` / `0.10648+165/1080` | (1709.2468, 800.0000) | 同 | 0 |
| `_foePileLabel` | `:14447` | `foeSizeX = 0.90104+(−5)/1920` / `0.90741−160/1080` | (1724.9960, 260.0000) | 同 | 0 |
| `_tipText` | `:14516` | `tipX01 = 961.0600586/1920` / `1−448.22/1080` | (961.0601, 448.2200) | 同 | 0 |
| `_resultLabel` | `:14542` | `0.5f` / `0.5f` | (960.0000, 540.0000) | 同 | 0 |
| `_titleMe` | `:15035` | `217.62721f/1920f` / `1f−1038.15523f/1080f` | (217.6272, 1038.1552) | 同 | 0 |
| `_titleFoe` | `:15038` | `217.92701f/1920f` / `1f−102.402645f/1080f` | (217.9270, 102.4026) | 同 | 0 |
| `_qpTextMe` | `:15364` | `1865.8381719470285f/1920f` / `…` | (1865.8382, 643.9899) | 同 | 0 |
| `_qpTextFoe` | `:15367` | `1864.9734582742763f/1920f` / `…` | (1864.9735, 198.7185) | 同 | 0 |

（表由脚本从源码里**现读** `x01/y01` 表达式 + 常量定义算出，不是手抄；`_skullScore` 等 5 行的常量值 = `:1432-1510` 那几条 `const/static readonly`。）

**非 16:9 下两者不再相同，而且【新的才是对的】**：锚点 = 相对**画布 rect**的归一化位置，
而画布 rect 随 `CanvasScaler(Expand)` 与屏一致 ⇒ 「字的相对位置」与**已经迁过去的图族同锚同式**
（图族走 `anchorMin=anchorMax=(x01,y01)`）⇒ 这就是 §五·补⑦ 要解决的那件事（「只换半边比不换更坏」）。
⚠️ **别拿「中心 + 固定 px」那一套（`HudCanvas.PosPx`/`MakeText` 的 px 入参）来读本件** —— 那是**面板内部**的口，
在 4:3/21:9 下会与图族分家（差异量级：4:3 下最多 ≈11% 屏高），**这正是本件不用 `MakeText` 的原因**（见 ⑤·2）。

### 4·c 16:9 逐位自证（尺寸）—— **两句话，一句是好消息、一句是偏差**

- ✅ **16 颗带框的**（`SetAutoFitBox`）：自适应的上下限是**按比例**折的（`fontSizeMax = cur × maxPx/nomPx`，`Label.cs:1073-1074`），
  且收敛目标是「装得下的最大号 ⇒ 由**框**（px）和字体度量定」⇒ **与档位/初始字号无关** ⇒ 迁前迁后**同一处**。
  ⚠️ **前提 = 本件顺手修的那个 ×10 缺陷**（不修的话 `cur` 与 `nomPx` 一起错 10 倍、**`min` 会真的卡住** ⇒ 小框的字撑破框）。
- ⚠️ **3 颗没框的**（`_turnLabel` `:13959` · `_clockLabel` `:14331` · `_resultLabel` `:14542` —— **都是我们自加的**）：
  字号只由 `scale` 档定 ⇒ 世界档 `0.07×scale` 世界单位 = **7.56×scale 屏 px**，画布档 `CapPxOfScale = 7` ⇒ **7.00×scale 屏 px**
  ⇒ **小 7.4%**（例：`_turnLabel` 的 scale 4 ⇒ 30.24 → 28.00 px 大写高）。**这一格没改，理由见 ⑤·1**。

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. 🔴 **「画布档一个 `scale` 档 = 7 px 还是 7.56 px」= 口径冲突，我没裁，报上来**（这是本件唯一一处**已知偏差**）。
   · 事实：世界档一档 = `7/100` 世界单位 × 108 px/单位 = **7.56 屏 px**；`Label.CapPxOfScale` = **7**（它的 doc 与 `T09` §五·补⑨ 四 都写「uGUI 档就是 7」——那句把「7 px @100 px/单位」当成了「7 px @108」）。
   · **同一发现、同一批**：`EW1` 给 `TutorialOverlay` 定了 `CapPxPerScale = 7.56`（「两档渲出来一样」），并在 `EW1` 报告 `:222-223` 里写「⚠️ 本批别的面板要是按『直接传整数 scale』写的，会有同一笔 7.4% 的差」。
   · **我为什么没统一改**：`CapPxOfScale` 是**共用件**的一个档位常量 ⇒ 一改就动**批 2/批 3 全部面板**里那些「靠裸 `scale` 定字号」的件（**几十处**，16:9 下 +8%），而那些件**本件既没碰、也跑不了验收**。
     ⇒ 两个选项请调度台裁：**(A)** 接受 7.4%（现状：本件只影响 3 颗我们自加的标签）；**(B)** 把 `CapPxOfScale` 改成 `7.56f`（= `EW1` 那条口径，全仓画布档 +8%、与「16:9 逐位不变」严格对齐）。
   · ⚠️ 无论选哪个，**`Label.SetSizes` 里 `scale = RoundToInt(approx / CapUnitOfScale)`**（`:1123`）会跟着变 —— 极少数件可能落到相邻档。
2. ⚠️ **本件没走 `HudCanvas.MakeText`（配方 §五·补⑩ 六 第 4 步的字面要求），走的是它下面的 `Label.CreateUgui` + 显式锚点**。
   · **不是省事**：`MakeText` 的位置入参是「**相对父件中心的设计 px**」（`Label.CreateUgui` 把锚定死在 `(0.5,0.5)` + 写 `localPosition`）
   —— 而 HUD 这一族的位置语义是「**归一化锚点**」（图族的就是这个）。用 `MakeText` 要么在 4:3/21:9 下与图族**分家**，要么**每次换分辨率都得重贴**（`MakeText` 不登记锚点）。
   · 单位那一半**完全照办**：喂进去的全是**设计 px**（`SetAutoFitBox` 去掉了 `Px()`）⇒ 配方警告的那个「世界单位 → 小 108 倍、不报错」的坑没有踩。
   · ⛔ 我**没有**在 `HudCanvas` 里加新口（那是白名单外）—— 锚点那两行是 `HudCanvas.MakeAnchored` 的**同式**，且 HUD 这一族**只此一处**（`Hud()`）。
     🔴 **如果调度台要求「必须走 `HudCanvas` 口」**，那就得给 `HudCanvas` 补一个「归一化锚点版建字口」（`MakeTextAnchored`），并把 `Hud()` 改过去 —— **那要另一笔**（`HudCanvas.cs` 不在本件白名单）。
3. ⚠️ **`HudImageTex` 那条改动（认「画布 root」）改变了图族的建件父件**（只对传 `RectTransform` 的那一处生效 = 提示条底板）。
   我判它是**修缺陷**（调用点自己的注释就写着该挂那块画布、`T09c` 的原版宿主也是 `AboveShader`），但它**属图族的批 3 成果** ⇒ 请复查一眼（见 ⑥·2）。
4. ⚠️ **没有真机/截图**（现行口径：不跑 Unity、不起 player）⇒ 「字画出来多大、位置对不对、提示条三件的叠法」**都只有静态推导**。
   **必须进真机的三档**：16:9（应与改前逐位同）· 4:3（图/字同尺、字不再偏大）· 21:9（锚点方向对）。
   ★ **本件最该先看的一处** = `_tipText`：它**改之前是坏的**（见 ⑥·2），改之后才第一次「该多大就多大」。
5. **没查清**：`Label.CreateUgui` 建的节点在**画布还没走过一帧布局**时读 `rt.rect`/`lossyScale` 会是什么（本件没依赖这两个量
   —— 锚点与 px 都不需要画布 rect **已解算**，这正是选锚点的附带好处；`HudCanvas.CenterPx` 那种「锚点 ×1920」的读回口才有这个前提，本件没用）。
6. **没做**（都不是本件范围，如实列出）：
   · `WaitBanner`（原版 `WaitText`）那颗字仍在**世界档**（面板族，见 ③·c）；
   · `ReanchorHud` 与 4 个注册表的**整体退场**（`T09` 第 6 步：本件只摘掉了标签那一轮，图族那一轮还在，且图族那轮 `SetAnchorPosition` 仍然必要 —— 它顺带把 `FitDeckPile`/`PlaceDeckLights`/`PlaceHandPlate` 的补偿重置回基准）；
   · `LayoutSpace` 那族读回口**成对归零**（`T09` 第 6 步）。

---

## ⑥ 顺手发现（只报不改，除注明的两处）

1. 🔴🔴 **【已修，但这是本件最大的发现】画布档的字号换算原来差 10 倍**（`Label` 的 uGUI 后端，批 1 留下的）。
   · **判据**（TMP 源码，本机自带包）：`TMP_Text.cs:7183` `fontScale = m_currentFontSize / pointSize × scale × (m_isOrthographic ? 1 : 0.1f)`；
     `TextMeshProUGUI.cs:802` `m_isOrthographic = true`（3D `TextMeshPro` 保持出厂 `false`）
     ⇒ **同一个 `fontSize`，画布档渲出来是 3D 档的 10 倍**。
   · 而字号↔尺寸的两个比值（`WorldGlyphPerFontSize` 0.0948 / `WorldCapPerFontSize` 0.0683）是 `TmpFont.MeasureGlyph` **拿 3D `TextMeshPro` 量的**（`AddComponent<TextMeshPro>()`），**没有 TMP_UGUI 那一份** ⇒ 对画布档小 10 倍。
   · **后果 ①**：`TmpFontSize()` 给的字号大 10 倍 ⇒ **没开自适应的画布标签**（本件之前：批 2/批 3 里那些只有 `scale` 的标题/按钮文案）**字大 10 倍**；
     **后果 ②**：`FontSizeToPxNow`（自检读回）给 1/10 的值。
   · **为什么没被早发现**：`SetAutoFitBox` 的上下限是**按比例**折的（`cur × maxPx/nomPx`）⇒ 有框的件里 `cur`/`nomPx` 一起错 10 倍、**比值不变**，收敛到「装得下的最大号」的结论**看不出来**；只有 **`min` 卡住**或**没框**时才现形（小框那档 = 字号地板大 10 倍 ⇒ 字撑破框）。
   · **本件已修**（`Label.cs`：`TmpFontSize()` ÷10 · `FontSizeToPxNow` ×10 · 一个常量 `UguiFontScale` 管两处）。**世界档零改动**（`_ugui` 为假时两处都乘/除 1）。
   · 🔑 **旁证**：`EW1` 给 `TutorialOverlay` 定的「画布档一个档 = 7.56 **px**」（`CapPxPerScale`）**只有在 ×10 修好之后才成立** —— 它走 `SetCapHeight(scale×7.56)` → `TmpFontSize()`。不修的话那颗字是 **10 倍大**。⇒ 这条修完，**批 2/批 3 迁移过的面板（含 Tutorial）要复跑一次真机验收**（我改的是共用件，影响面**超出本件**，所以单列）。
2. 🔴 **提示条那一组原来是「半迁移态」**（已按 ②·a 修）：
   · 调用点 `:14478` 的注释写着「`HudImage`/`Hud` 的 `parent` 从 `root` 改成**这块画布**」，
     但 `HudImageTex` **从来只 `Ensure(cam)`**（= 建**根画布**）、`Hud()` 当时走的是**世界空间** `Label.Create`
     ⇒ 实测结果：**底板在根画布（`Default` 层）· 字在世界空间（还挂在子画布下、被画布缩放乘了一次 ⇒ 小 108 倍、几乎看不见）**。
   · 本件两处都改了（`HudImageTex` 认画布 root；`Hud()` 认画布 root）⇒ 现在**三件同容器**：底板 `HudImageZ`(0.3) → 字 `HudLabelZ`(0) → 加时 splash 根(0)
     ⇒ 兄弟序与**原版**「`TipText` 在前、`OvertimeSplashText` 在后」一致 ✓。
3. ⚠️ **`BattleScene.cs:1905` 那条断言今天就是红的**（不是本件改红的）：「本批断言跑在 16:9」那句把 `cam.aspect` 当成了 `Screen`
   —— `HudTextScale` 读的是 `Screen`（批处理 640×480）⇒ 实得 **0.75**、期望 1。见 ③·a-1。
4. ⚠️ **同族还有几处「写死 100」的量纲守卫**（本件顺手统一了，见 ②·b）—— 全仓可能还有别的件用「100」当哨兵阈值
   （本件只扫了 `Battle/Label.cs`，⛔ 没扫别的文件）。
5. ⚠️ **过期注释两处**（不影响行为，未改，因不在本件范围/会与别的写手撞）：
   · `BattleDriver.cs:15549`「位置靠 `LayoutHudLabels` 之后按文字实际宽度重算」—— 全仓**没有** `LayoutHudLabels` 这个方法（应为 `PlaceOvertimeIcon`）；
   · `BattleDriver.cs:6652` 注释里说 `HudAbs → HudImageTex → ImageQuad.Create` —— 图族**早已不是** `ImageQuad`（现在是 `HudCanvas.MakeAnchored`）。
6. ⚠️ **`⑦ 未做` 的 `_tipText` 命中/显示链**：`SetTipVisible` 同时开关底板与字（`:9414-9415`）✓ 本件没动；但**底板的父件**变了（根画布 → `AboveShader` 子画布）⇒ 两者**始终同容器**，显隐不会分家 ✓。
7. ✅ **`_handLabel` 的框**（`A1239` 那笔）在本件里是**成对**动的：`SetAutoFitBox` 的入参去掉了 `Px()` ⇒ 框 234×64 px 不变、字号随之正确；
   而 `PlaceHandPlate` 现在按**字块 px 尺寸**摆板 ⇒ 「板与字错位 25%」那个补偿**没有留下的必要**（判：**删对了**，见 ②·a）。

---

## 可直接粘进总表的一行

| **`EW4`** | **HUD 字族整族迁 uGUI**（`T09` §五 第 5 步的另一半） | ✅ **21 个建字点全迁**（都走同一个 `Hud()` 口 ⇒ 无半迁移态）· `HudTextScale` 在 `BattleDriver.cs` 的**两个消费点已删**（本体待删，见 ③·b）· `PlaceHandPlate` 改为「同父同锚 + px 偏移」（25% 补偿项退场）· 🔴 **顺手修了两个共用件缺陷**：**画布档字号换算差 10 倍**（`Label.cs` · 判据 = TMP 源码 `m_isOrthographic`，**已修**）与 **`UsableBounds`/`HasMeasuredWidth` 的量纲上限取反**（**已修**，另有 3 处同族守卫统一）；**16:9 位置逐位自证 = 0**（21 行表）· **偏差 1 处**：3 颗无框标签的字号 7.00 vs 7.56 px/档（**7.4%**，口径待裁 → ⑤·1） | 报告 `资料/普查产出_1016并行/EW4_HUD字族迁uGUI_报告.md` · `typecheck` **0/0** · ⛔ 未跑 Unity/未真机 |
