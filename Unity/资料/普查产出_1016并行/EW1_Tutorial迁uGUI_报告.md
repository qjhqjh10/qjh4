# EW1 · TutorialOverlay 迁 uGUI（`T09` 批 3 最后一格）

> 执行代理 `EW1`（2026-10-16）。白名单 = `Battle/TutorialOverlay.cs` + 本报告。
> ⛔ 没跑 Unity、没动 git、没碰白名单外的文件。类型检查 = `TMPDIR=/tmp/wf_ew1 bash 工具/typecheck.sh` ⇒ **运行时 0 · 编辑器 0**（跑了两遍：主改完一次、收尾小改后一次）。
> 行号口径：**本件（`TutorialOverlay.cs`）的行号 = 本报告写就那一刻**；引 `BattleDriver.cs` / `BattleScene.cs` 一律给**搜哪一串**（那些文件当天内就漂过）。

---

## ① 结论 —— 逐件「做了 / 没做 + 为什么」

**总口径**：`TutorialOverlay` 那一族（原版三件宿主 + 我们六层件）**全部做出了画布档**，走**双模式**：
`Create(parent, uguiHost)` 的 `uguiHost` **传 null（出厂、也是今天唯一的调用形态）⇒ 世界档，与改之前逐位相同**；
传一块 `RectTransform` ⇒ **画布档**。**上游那一行登记在 `BattleDriver`（白名单外）⇒ 见 §③**（没登记 = 全跑世界档，⛔ 不是半迁移态）。

| 原版那一件（`T09c` §一 ① ② ③） | 我们落成 | 结论 |
|---|---|---|
| **① `Tutorial`** @ `…/Safe area FrontCanvas`，rm2/ovr true/`HandMoving`/**1500**/兄弟序 11/14 | 子画布 **`Tutorial Canvas`**（同名幂等建/取；rect 补成原版那个 **中心锚 + 1920×1080**） | ✅ **做了**。它接住我们**四组**件：`TutorialTip`（底板 + 2 字）· `InitTutorialTip`（2 字）· `TutorialArrows/arrow1·arrow2` · `TutorialObjs`（4 字 + 4 箭头） |
| **② `Tutorial highlight`** @ **根 `Canvas` 直子**，`HandMoving`/**601**/兄弟序 5/8 | 子画布 **`Tutorial highlight Canvas`**（父 = 根 canvas；出厂 `stretch` 形状**就是**原版那个 `RT_3501` ⇒ 不动它） | ✅ **做了**。高亮那一层独占它；世界档那句 `SetRenderQueue(601)` 在画布档走 `SetQ`（空操作 —— 层级归 (层, order)） |
| **③ `TutorialPointerCombat`** @ **`…/Tutorial`**（子件），`OverHand`/**10**/兄弟序 3/4 | 子画布 **`TutorialPointerCombat Canvas`**，**嵌在 ① 下面**（原版 `RT_3572.m_Father = 3206` 现读），rect 同 ①（中心锚 + 1920×1080） | ✅ **做了**。指点光标那一件独占它 |

**逐件**（我们这一侧的六层，全部迁到画布档）：

1. **小提示 `TutorialTip`**（底板 + `TipText` + `ContinueText`）→ ①。底板 `HudPiece`、两颗字 `HudCanvas.MakeText`；节点 rect = **判据** `430×150` px。
2. **开场两条 `InitTutorialTip`**（`TipText` + `TipText (1)`）→ ①（**建出来但不主动亮** —— 原版谁启用它**查不到**，这条没动）。
3. **左右箭头 `arrow1`/`arrow2`** → ①（原版那两层 `m_Sprite` 是**真·空引用** ⇒ 图是我们挑的载体；数据里 `showLeftArrow/RightArrow` 6 关**全 false** ⇒ 一次都不会亮，照旧）。
4. **教学标注 `TutorialObjs`**（4 字 + 4 箭头）→ ①（⚠️ **父链与原版不同**，见 §⑤·1）。
5. **高亮 `Tutorial highlight`** → ②。
6. **指点光标 `Pointer`** → ③。

**没做的（都不是本件能做的）**：① 上游登记那一句（`BattleDriver`）② 那条按路径找 `ContinueText` 的断言（`BattleScene`）③ `HudCanvas` 嵌套子画布那个幂等缺口 —— 三件都在 §③。

---

## ② 改动清单（`文件:行号` + 一句话）

只有一个文件：`Unity/MyGame/Assets/CardPresentation/Battle/TutorialOverlay.cs`（**+469 / −53**）。

| 行（本报告写就时） | 一句话 |
|---|---|
| `:71-101` | 文件头新增 **`T09` 批 3 判据段**：三件宿主 / 三块 rect / 双模式 / 「取负」为何**不适用** / 两条如实记的差异 |
| `:190-206` | 新增 **z 档常量**（`ZTip 0.05` / `ZTipBg 0.07` / `ZFlat 0` / `ZHighlight 0.04` / `ZPointer 0.06`）—— **值逐位照抄改前那几个字面量**；只有 `ZTipBg` 是新的（画布档定死底板在字后） |
| `:208-215` | 新增 `CapPxPerScale = 7f/100f*Px = 7.56`（`scale` 档 → 画布档 px，见 §④·3） |
| `:243-258` | 字段类型 `ImageQuad` → **`HudPiece`**（`_tipBg` / `_arrowL` / `_arrowR` / `_highlight` / `_pointer` / `_annoArrows[4]`），逐个补了 doc |
| `:267-301` | 新增画布那一侧：`_cvTip` / `_cvHl` / `_cvPtr` · 三个画布名常量 · 三个 order 常量 · `UguiMode` 与三个画布 `RectTransform` 读口 |
| `:329-378` | 新增**自检口 `OnCanvas`**（灭自证）+ `ImgOnCanvas`/`TxtOnCanvas`/`NodeOnCanvas`/`UnderCanvas` —— `UnderCanvas` **沿父链手走、不过滤激活态**（⛔ 不写无参 `GetComponentInParent<Canvas>()`，那个对未激活件答「没有」） |
| `:392-410` | `Create(Transform)` → **`Create(Transform, RectTransform uguiHost = null)`**（可选形参 ⇒ **既有调用点 `Create(hudRoot)` 一字不动、行为不变**＝世界档） |
| `:412-443` | 新增 **`UseHost(host)`**（先例 `SkillPanel`/`Tooltip`）：传 null ⇒ 出声 + 留世界档；排在 `Build()` 之后 ⇒ **拆旧建新** |
| `:445-467` | 新增 `Teardown()` / `KillGo()`（**只拆件、不拆那三块子画布** —— 画布由 `EnsureSubCanvas` 按名字管） |
| `:469-514` | 新增 `EnsureCanvases()`（建齐三块 + 把 ①② 的 rect 补成 `SetPxSize(1920,1080)`）+ `ChildCanvas()`（嵌套那一块的「先按名字找」） |
| `:516-593` | 新增四个建件/摆位助手：`NewNode` · `PlainNode` · **`MakeImg`** · **`MakeLbl`** · **`CvPlace`** · `SetQ` |
| `:595-737` | `Build()` 重写：先备画布（失败 ⇒ 出声 + 落回世界档），三个 `parent` 按档取，六组件按档建；**世界档那一支逐字沿用原语句**（含 `InitTutorialTip` 那句 `localPosition = LayoutSpace.ToWorld(...)`） |
| `:787-823` | `SetTip`：+ 画布档摆位（`CvPlace`）· + 两颗字**判空**（画布档 `MakeText` 可能返 null） |
| `:834-844` | `SetInitTip`：+ 两颗字判空 |
| `:846-905` | `SetHighlight`：+ 画布档摆位；尺寸画布档走 **`CvSizePx`（各轴取逆）** —— 见 §②·补 与 §⑥·6 那条就地订正 |
| `:582-608` | 新增 **`CvSizePx`**（「量出来的世界尺寸」→ 画布设计 px，**按各轴实测系数的逆**，⛔ 不是一刀切 ×108） |
| `:882-895` | `SetPointer`：+ 画布档摆位 |
| `:897-930` | `SetAnnotations`：+ 四颗字判空 · + 箭头的画布档摆位 |
| `:334` | 新增只读口 **`TipContinueGo`**（给 `BattleScene` 那条断言用 —— 它原来是按**路径**找那颗 GO，画布档下那条路径不存在 ⇒ 见 §③·2） |
| `:71-101`·`:243-378` | （同上面几行，这里是新加的 doc 段与读口，一起列在此处只为对行号） |

**行尾核对**（铁律：先二进制读、改完对一眼）：

```
python 二进制读：改前 CRLF 0 / LF 573  →  改后 CRLF 0 / LF 1024（纯 LF，没翻）
git diff --numstat -- …/TutorialOverlay.cs  ⇒  504  53  （**不是整篇重写**那个量级）
```

### ②·补 🔴 **收尾时按 `EW2` 现核就地订正了一处**（铁律 5 · 留更正痕）

**那一支原来的注释是错的、而且照它写也是错的**（两处都在 `SetHighlight` 的画布分支）：
它写着「⛔ 别在画布档调 `SetWorldHeight`（内部写死 ×108、y 偏 0.75）」—— 而**同一段代码自己写的 `w * Px` 就是同一个 ×108** ⇒ 自相矛盾。
🔑 **真判据 = 看那个量的【来路】**（`EW2` 现核，判据链完整）：

| 来路 | 该怎么换 | 本件哪几处 |
| --- | --- | --- |
| **输入是【设计 px】**（值本身就是设计值，或 = 设计 px ÷ 108） | **×108**（那是**同一个定义**的两个单位，⛔ 不是「量出来的」） | `MakeImg`（输入 = 我们挑的**世界单位**高 / 设计 px ÷108）· `MakeLbl`（`CapPxPerScale = 7.56`）· `NewNode`（直接吃设计 px，**不换算**） |
| **输入是【量出来的世界尺寸】**（`WorldW/H` 一族） | **本画布【各轴实测系数】的逆**（16:9 下才恰好 = ×108；批处理 x 1/108 · y 1/144 ⇒ y 要 ×144） | **只有 `SetHighlight` 那一处**（`size` 来自 `BattleDriver.TutAnchor` 取已迁件的 `WorldW/H`）；已改走新口 **`CvSizePx`**（`InverseTransformVector`） |

⚠️ **实测反例（`EW2`）**：原版 `130.7 × 80.4` px 的 `_endTurnBg` 量得 `(1.2102, 0.5583)` ⇒ 一刀切 ×108 交回 `(130.7, **60.3**)`
⇒ **高亮框比它框的那颗钮矮 25%**（**16:9 下恰好一致、看不出来**）。
✅ **本件全量复核过「还有没有别处吃量出来的世界尺寸」** ⇒ **只有这一处**（`_highlight.WorldW` 那句**只在世界档那一支**，
那里的 `WorldW` 就是世界 quad 自己的世界尺寸、口径自洽，**一个字没动**）。
✅ **`16:9 自证`（§④·3）不受影响**：16:9 下两轴系数都 = 1/108 ⇒ `CvSizePx` 与 ×108 **同值**，那张表的差列仍然全是 0。
⛔ **没碰 `HudPiece.cs`**（共用件；`EW2` 已判定**它的算术是对的**、只改了注释）。

---

## ③ 接线清单（白名单外谁该改哪一行）

> 🔴 **本条全部是「接线」，不是「缺陷」**：不接 ⇒ 今天照样跑世界档（与改前逐位相同），只是**没吃到画布档**。

1. 🔴 **`Battle/BattleDriver.cs` · `EnsureTutorialOverlay()` —— 唯一的登记点**（搜 `TutorialOverlay.Create(hudRoot)`）。
   改成（**顺带把 `HudCanvas.Ensure(LayoutSpace.Cam)` 带上** —— 它就是 `EnsureSubCanvas` 的前置，同族 4 处都这么写）：

   ```csharp
   HudCanvas.Ensure(LayoutSpace.Cam);                       // 幂等（同 AboveShader / SkillPanel 那几处）
   var tutCv = HudCanvas.EnsureSubCanvas(null, TutorialOverlay.SubCanvasName,
                                         HudCanvas.SortingLayer.HandMoving, TutorialOverlay.TipSortingOrder);
   if (tutCv == null)
       Debug.LogError("[Battle] `Tutorial` 的 `HandMoving` 子画布没建起来 ⇒ 教程表现层**留在世界空间档**"
                    + "（非 16:9 下会与已迁到画布的同族面板分家；`Create(null)` 自己也出声）");
   _tutOverlay = TutorialOverlay.Create(hudRoot, tutCv);     // ⛔ 别在这里另写画布名/层/order 字面量
   ```

   ⚠️ **另外两块画布不用这里建** —— 由 `TutorialOverlay` 自己建齐（第二块在根 canvas 下，第三块**嵌在第一块下面**；
   分三次传反而更容易把父链写错）。⚠️ **`EnsureRankedSubCanvas` 别用在三块中的任何一块上**：那张表只解决「同一 (层,order) 之内的并列」，而这三块的 (层,order) **互不相同** ⇒ 不在表里**是正常的**，用了它反而会喊一条**假** warning（见 §④·2）。
2. 🔴 **`Editor/BattleScene.cs` · 那条读 `ContinueText` 开关态的断言**（搜 `transform.Find("TutorialTip/ContinueText")`）——
   **接线之后必红 / 更糟的是静默失效**：画布档下 `TutorialTip` 挂在 `Tutorial Canvas` 下，不再是 `TutorialOverlay.transform` 的直接子件
   ⇒ 那句 `Find` 回 `null`，而断言写的是 `ovCont != null && ovCont.gameObject.activeSelf`（**null ⇒ 恒假、不报错**）。
   ✅ **本件已经把那个只读口补好了**（`TutorialOverlay.TipContinueGo`，`:334`；两档都对：世界档它就是
   `transform/TutorialTip/ContinueText` 那个 GO）⇒ 调用点那一行改成：

   ```csharp
   var ovCont = ov.TipContinueGo;      // ⛔ 别再用路径字符串（画布档下那条路径不存在）
   ```

   ⚠️ 同批值得顺带核一遍的：`ov.TipWorldPos`（那条「`Above` + `dx = 0` ⇒ 位置不动」的还原）在画布档下是
   `InverseTransformPoint` → `anchoredPosition` → `transform.position` 的**往返**，批处理下画布没排版时它不一定逐位复原（见 §⑤·4）。
3. 🔴 **`HudCanvas.cs`：`EnsureSubCanvas` 的「按名字幂等」只覆盖【根 canvas 的直接子件】**（`SubCanvas(name)` 只扫 `RootRect` 的 children）——
   传了 `parent`（嵌套）时它**按名字找不到已有的那一块、每调一次就再建一块**（静默）。
   ⇒ 本件用 `ChildCanvas()` 先 `parent.Find(name)` 挡了一层（那一块是**唯一**嵌套的）。**建议 `HudCanvas` 自己收口**（在 `EnsureSubCanvas` 里改成
   `parent != null ? parent.Find(name) : SubCanvas(name)`），否则下一个做嵌套画布的人会踩同一脚。⛔ 本件不动它（白名单外）。
4. ✅ **`ProjectSettings/TagManager.asset`：不用动**（主对话 2026-10-16 已裁定）—— `HandMoving`(887547619) 与 `OverHand`(2007638115)
   **都在**（`TagManager.asset:56-61` 现读；`HudCanvas.SortingLayer.HandMoving = 887547619` 与它逐位一致）。⇒ **缺层那一格划掉**。
5. ✅ **`HudCanvas.SubCanvasRank`：不用加条目**（主对话裁定）。理由（**照抄那一条**）：那张表只解决「同一 (层, order) 之内的并列」，
   而这三块的 **(层, order) 互不相同**（`HandMoving`/1500 · `HandMoving`/601 · `OverHand`/10）—— 别的 `OverHand` 画布全是 **order 0**
   ⇒ **没有并列 ⇒ 不需要排**。⚠️ 顺带避开一个陷阱：表项与 `SubCanvasName` 字面量对不上时 `RootRect.Find` 永远 null、**静默不排**（`HudCanvas.cs:578-580` 自陈）。
6. ✅ **`ReanchorHud` 与那四个注册表：本件【不在里面】** ⇒ **没有要摘的东西**（现核：`ReanchorHud()` 的函数体只碰
   `_hudLabels` / `_hudImages` / 牌堆 / `playerBoard` / `selector` / `skillPanel` / `_replayBar` / `_unitChat`，**全程没有 `_tutOverlay`**；
   `_hudExtras.Add` / `_hudLabels.Add` / `_hudImages.Add` 三个登记口都在 `BattleDriver` 自己那几个建件助手里，本件的件**一次都没走它们**）。
   ⇒ 第 6 步对本件**自动满足**（画布档的件不跟 `HudRoot` 走是**画布档的既定代价**，见 §⑤·3）。
7. ⚠️ **`Editor/BattleScene.cs` 建议补的断言**（本批通例，接线那一轮一起加）：

   ```csharp
   Check(ov.CanvasRect != null && ov.HighlightCanvasRect != null && ov.PointerCanvasRect != null,
         "★ 三块教程子画布都建出来了（`Tutorial` / `Tutorial highlight` / `TutorialPointerCombat`）");
   Check(ov.CanvasRect.sortingLayerID == HudCanvas.SortingLayer.HandMoving && ov.CanvasRect.sortingOrder == 1500
      && ov.HighlightCanvasRect.sortingLayerID == HudCanvas.SortingLayer.HandMoving && ov.HighlightCanvasRect.sortingOrder == 601
      && ov.PointerCanvasRect.sortingLayerID == HudCanvas.SortingLayer.OverHand && ov.PointerCanvasRect.sortingOrder == 10,
         "★ 三块的 (层, order) = 原版那三行（判据 → `T09c` §一）");
   Check(ov.PointerCanvasRect.parent == ov.CanvasRect,
         "★ 光标那块**嵌在** `Tutorial` 那块下面（原版 `RT_3572.m_Father = 3206`）");
   Check(ov.OnCanvas, "★ **灭自证**：件都在 uGUI 子画布上（⛔ 不是「一半画布一半世界」）"
                    + "｜🧨 把某一件改回 `ImageQuad.Create(_root, …)` ⇒ 本条红（而 16:9 的截图看不出来）");
   ```

---

## ④ 判据与出处 + 16:9 自证

### 1. 判据（⛔ 一条都不是我编的）
| 事实 | 出处 |
|---|---|
| 三件的 **(层, order, 兄弟序)** | `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` **§一 `TutorialOverlay` 那一行**（引 `CV_2773`·`RT_3206` / `CV_2774`·`RT_3501` / `CV_2766`·`RT_3572`） |
| 三块的 **rect 五元组**（本件唯一新挖的一格） | **现读** `bundle_scenes_scenes_battlearena2/RectTransform/`：<br>· `RT_3206`（`Tutorial`）：`aMin=aMax=(0.5,0.5)` · `pivot (0.5,0.5)` · `ap (0,0)` · **`sd 1920×1080`** · `father 3055`(= `Safe area FrontCanvas`) · 4 子件<br>· `RT_3501`（`Tutorial highlight`）：**`aMin (0,0)` / `aMax (1,1)` / `sd (0,0)`** · `father 2952`(= 根 `Canvas`) · 0 子件<br>· `RT_3572`（`TutorialPointerCombat`）：中心锚 + **`sd 1920×1080`** · **`father 3206`**（⇒ 它在 `Tutorial` 下 ✓）· 1 子件 |
| 层名/层 id | `HudCanvas.SortingLayer`（那个类的注释自陈判据 = 原版 `TagManager_3.json`）＋ 我们的 `TagManager.asset:56-61`（`HandMoving` / `OverHand` 逐值对上） |
| 迁移配方（8 步） | `资料/任务线/战斗线/任务/T09_整层换uGUI.md` **§五·补⑩**（含「队列 → 兄弟序取负」那条**只对同一容器内的 `renderQueue` 阶梯**成立） |
| 建件口 / 单位口径 | `Battle/HudCanvas.cs` 文件头（1 canvas 单位 = 1 设计 px · `(层,order)` 定先后 · 子画布不加 `CanvasScaler`）＋ §五·补⑨ 四（喂错量纲**不报错**） |
| 原有那些「不是判据」的常量/边界 | 本件文件头（`Gap` / `HighlightPad` / `InitTipPxW` / 箭头图载体 / 标注文案与位置 = **我们挑的**）—— 本轮**一条都没改口径** |

### 2. 为什么**不往 `SubCanvasRank` 加条目**（复核主对话那条裁定，本件实测支持它）
`ApplySubCanvasRank` 只按表**重排已建出来的那几块**，而它服务的矛盾是「**同层同 order ⇒ 谁在前**」。
本件三块的 (层, order) = `(HandMoving,1500)` · `(HandMoving,601)` · `(OverHand,10)` —— **两两不同**，
`(层,order)` 自己就定死了先后（`HandMoving` 层在 `OverHand` **之下**，见 `HudCanvas.SortingLayer` 那段层序注释）。
⇒ 加进表里**只会多一个「名字对不上就静默不排」的风险点**，收益 = 0。

### 3. **16:9（1920×1080）自证 —— 两档逐值相同**（逐件算）
换算依据：16:9 下 `CanvasScaler(Expand)` 的 `scaleFactor = 1` ⇒ **1 canvas 单位 = 1 屏 px**；而 HUD 侧 **1 世界单位 = 108 屏 px**（`LayoutSpace.DesignPxH / DesignHeight`）。
`CvPlace` 走 `InverseTransformPoint` ⇒ 局部位 = 世界偏移 × (1 canvas 单位/世界单位) = ×108（两个轴都 108，16:9）。

| 件 | 世界档（改前 = 改后） | 画布档（本次） | 差 |
|---|---|---|---|
| `TutorialTipBg` | 高 = `TipPxH/Px` = 150/108 = **1.38889** 世界单位；宽 = 高 × 图比例 | `hPx = 1.38889×108 = **150.00**` px；宽 = `hPx × a` | **0** |
| `TipText` | `scale 3` ⇒ 大写高 3×0.07 = **0.21** 世界单位 | `SetCapHeight(3 × 7.56) = **22.68**` px | **0**（0.21×108 = 22.68） |
| `ContinueText` | `scale 2` ⇒ **0.14** 世界单位 | **15.12** px | **0** |
| `arrow1`/`arrow2` | 高 **0.45** 世界单位 | **48.60** px | **0** |
| 高亮的**尺寸** | 最终矩形 = `w × h`（`w = max(size.x,0.3)+0.12`，h 同式）世界单位 | `sizeDelta = CvSizePx(_cvHl, (w,h))`；**16:9 下 = `(w×108, h×108)`** px | **0**（世界档那三行的终态也是 `w×h`） |
| `Pointer` | 高 **100/108 = 0.92593** 世界单位、宽按图比例 | 高 **100.00** px、宽 `100×a` | **0** |
| `annoArrow[i]` | 高 **0.3** 世界单位 | **32.40** px | **0** |
| `annoLabel[i]` | `scale 2` ⇒ **0.14** 世界单位 | **15.12** px | **0** |
| **四个动态锚点**（提示 / 高亮 / 光标 / 标注箭头） | 世界点 `P` 直接写 `transform.position` | `anchoredPosition = InverseTransformPoint(P)` = `(P.x×108, P.y×108)` | **0**（同一个屏点） |
| `InitTutorialTip` | `localPosition = ToWorld(0.5,0.72)` = `(0, 2.2)` 相对 `hudRoot` | 同一个世界点 `_root.TransformPoint(ToWorld(…))`（`_root` 恒在 `hudRoot` 原点）⇒ `(0, **237.60**)` px | **0**（2.2×108 = 237.6） |
| 容器的 rect | 世界档那几个是**裸 `GameObject`**（没有 rect，尺寸由图/字自己决定） | `TutorialTip` = **430×150**（判据）· `InitTutorialTip` = 700×100（我们挑的）· `TutorialObjs` = 1920×1080 | 只影响「子件按非中心锚排放」这一档 —— **本件所有子件都是中心锚 + `anchoredPosition`** ⇒ 画出来**逐位相同** |

⚠️ **只有一处**两档**不**同值（有意，且**只在画布档**）：小提示**底板与它那两颗字的兄弟序**。
世界档它们是**共面**（都在 `z = 0.05`，谁压谁不定义）；画布档必须定死 ⇒ 底板 `z = 0.07`（排前 = 画在**底下**）、字 `z = 0.05`。
意图 = 底板在字后面（世界档那两件的**建序**就是这个意图）。

---

## ⑤ 没查清 / 没做的（⛔ 不许猜）

1. **`TutorialObjs` 的父链与原版不同 —— 本件【没翻】**。原版它在 `Card Display/Card Display Window`（HUD 那一支，全树 `:578`），
   我们建在 `Tutorial Canvas` 里。依据 = `T09` §五·补⑩ 二（「本次只按『谁的 canvas 装谁』迁，把差异如实记着，⛔ 别顺手把嵌套倒过来」）⇒ **记着**，属另一笔账。
2. **画布上的件【不跟 `HudRoot` 的震屏】**：`CardFeel.ShakeCamera(cam, hudRoot, …)` 动的是 HUD 根，而画布是**根对象**。
   世界档的那几件跟着 `hudRoot` ⇒ 震屏那一瞬间两档会差一个震幅。与 `SkillPanel` 那条同源（那扇也是「世界壳跟震、画布件不跟」）。
   **本轮没做**（要做得先把「画布也要震」这件事定口径，不在本件范围）。
3. **批处理下画布没排过版时 `InverseTransformPoint` 的可靠性**：本件所有动态摆位都走它（口径同 `SkillPanel.Contains` / `MulliganPanel.PxOf` /
   `AttackSelector.AnchorLocal`）。**没跑到实况**（红线：不跑 Unity）⇒ **不能证明**「批处理下那三块子画布的 `rect`/世界矩阵已经排好」。
   若自检里发现位置读数不对，先怀疑这一点，**别怀疑换算式子**（式子 = 现成四处的同一份）。
4. **`BattleScene` 那条「`Above` + `dx = 0` ⇒ 位置不动」的还原**在画布档下是 `InverseTransformPoint → anchoredPosition → position` 的往返。
   16:9 且画布已排版 ⇒ 逐位复原；**没排版 ⇒ 不保证**（同 3）。⇒ 接线那一轮若那条红，先判「是不是 3 那件事」。
5. **`InitTipPxW = 700` 是我们挑的**（原版那个 rect 判据是 `100×100`）—— 那一层**不亮**、也不参与布局 ⇒ 尺寸只影响这个空容器。
   本轮**没改**（改它会动世界档，且没有任何判据支持 700 之外的数）。⚠️ 记着：它**不是**原版值。
6. **指点光标的真载体仍是「我们挑的 `40k_general_bt_arrow`」** —— 原版按**阵营**取光标图（`ArmyUtilities.GetCursorForArmy` →
   `CursorByArmySO`），**那份 SO 本地没解出来** ⇒ **阵营→光标没有判据**，⛔ 不猜（同文件头 ②，本轮没动）。
7. **没做的三件接线**（§③ 1/2/3）—— 全在白名单外。
8. ⛔ **没跑 Unity**（红线）⇒ 「画布档真机长什么样」**本轮一次都没验**（本批通例：攒到复刻完成再统一验收）。

---

## ⑥ 顺手发现（只报不改）

1. 🔴 **教学标注那四颗字从来没被摆过位**（现核本件 `SetAnnotations`）：循环里只 `SetText` + 开显隐，**只有箭头**被摆到
   `anchorWorlds[i]`；四颗字全留在 `TutorialObjs` 的原点 ⇒ **四段文字叠在屏幕正中同一处**，而箭头各在各自锚点上。
   **世界档与画布档都一样**（不是本轮引入的）。⇒ 这是**真缺陷**（按铁律 11 要做），但修它**会动世界档的渲染**，
   而本轮的硬要求是「世界档逐位相同」⇒ **没动，报上来由主对话裁**（修法很小：把 `_annoLabels[i]` 摆到 `anchorWorlds[i]` + 一个偏移，
   两档共用 `CvPlace` / `position` 那一支）。
2. 🔴 **`HudCanvas.EnsureSubCanvas` 的幂等只覆盖根 canvas 的直接子件**（`SubCanvas(name)` 只扫 `RootRect` 的 children）——
   嵌套子画布**每调一次建一块**（静默）。本件用 `ChildCanvas()`（先 `parent.Find`）挡住 ⇒ 见 §③·3。**建议 `HudCanvas` 收口**。
3. ⚠️ **`Label` 的 `scale` 档在两个空间里【渲出来不一样大】**：`Label.CapPxOfScale = 7`，世界档那个档 = `7 / Label.PixelsPerUnit(100) = 0.07`
   世界单位，而 HUD 的世界比例是 **108 px/单位** ⇒ 世界档真渲出来是 **7.56 屏 px**、画布档（直接传 `scale`）是 **7.00 px** ⇒ **差 7.4%**。
   本件按「两档渲出来一样」办（`CapPxPerScale = 7.56`）；⚠️ **本批别的面板**要是按「直接传整数 `scale`」写的，会有同一笔 7.4% 的差
   （收口时值得扫一眼 —— 只影响**用整数 scale 建字**的那些，用 `SetGlyphHeight(px)` 的不受影响）。
4. ⚠️ `HudPiece` **没有 `SetRenderQueue`** ⇒ 本件要调队列只能走 `p.Quad.SetRenderQueue(q)`（画布档 `Quad == null` ⇒ 空操作）。
   本件包了个 `SetQ` 并写了注释；**这一格值得收进 `HudPiece`**（现在全仓至少有 `Label` 有、`HudPiece` 没有 —— 不对称）。
   ⛔ 本件不动 `HudPiece.cs`（既不在白名单，同日另有代理在做它的 `NudgeWorld`/`SetWorldHeight` 那笔账）。
5. ⚠️ `_tipGo` 那两颗字与底板在世界档下**共面**（都在 `z = 0.05`，谁压谁靠建序不定义）——
   画布档必须定死先后（本轮定成「底板在后」）。**世界档那一格没动**（改它 = 改世界档渲染）。
6. 🔴🔴 **【跨全批的口径】「换 px」要看那个量的【来路】**（收尾时按 `EW2` 现核就地订正了本件一处，全文 → §②·补）：
   * **输入是设计 px**（或「设计 px ÷ 108」那种**由常量算出来的**世界单位）⇒ **×108**（同一定义的两个单位）；
   * **输入是量出来的世界尺寸**（`HudPiece.WorldW/H` / `Label.WorldW/H` 一族，`GetWorldCorners` 出来的）
     ⇒ **必须取【本画布各轴实测系数】的逆**（`CvPlace` 的 `InverseTransformPoint` / `CvSizePx` 的 `InverseTransformVector` 就是这一族的通用做法）。
   一刀切 ×108 的后果 = **16:9 全绿、别的宽高比下短 25%**（`EW2` 实测 `_endTurnBg`：真 `80.4` px、算出来 `60.3` px）。
   ⚠️ **同族另一件**：`HudPiece.WorldW/H`（uGUI 档 = **世界单位**）与 `Label.WorldW/H`（uGUI 档 = **px**）**同名不同量纲**
   —— 那是本会话「0.75 倍」那类困惑的总源头。📌 **口径：读回尺寸一律走 `HudCanvas.SizePx(q.Rect)`**
   （先例 `BattleLogPanel.FrameSizePx` / `MultiCardDisplay.ContinueBarDrawnPx`）。
   ✅ **本件自查**：本件的自证（§④·3）是**解析算出来的**、**没有**读 `WorldW/H` 回比；实现里读 `WorldW` 的**只有 `SetHighlight` 世界档那一支**
   （那里它就是世界 quad 自己的尺寸、口径自洽）⇒ 无其它同类隐患。

---

## 可直接粘进总表的一行

```
| T09 批 3 · TutorialOverlay | ✅ 三件宿主各起一块子画布（`Tutorial Canvas` HandMoving/1500 · `Tutorial highlight Canvas` HandMoving/601 · `TutorialPointerCombat Canvas` OverHand/10，**第三块嵌在第一块下**）+ 双模式（`Create(parent, uguiHost=null)`，出厂 = 世界档逐位不变）+ **高亮尺寸按各轴取逆**（`CvSizePx`，收尾按 `EW2` 现核订正）；typecheck 0/0 | 余账：① `BattleDriver.EnsureTutorialOverlay` 登记那一行 ② `BattleScene` 那条 `transform.Find("TutorialTip/ContinueText")` 要用新口 `ov.TipContinueGo` ③ `HudCanvas.EnsureSubCanvas` 嵌套幂等缺口 ④ **顺手发现**：教学标注四颗字从来没摆位（四段文字叠在一起，两档都有） ⑤ `TutorialObjs` 父链与原版不同（如实记） |
```
