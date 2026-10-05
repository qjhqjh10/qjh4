# V4b · 三件口径（只读查证 · 2026-10-05）

> 只读：未改任何 `.cs` / 正本 / 判据文件。⚠️ `Shell/MainMenuRuntime.cs` **正被别的写手改** ⇒ 我引它的两处（`:139` 常量 · `:151-168` 层序决定）是**当时那一刻**的文本。

## Q1 · A266 —— `TmpFont.SetWrapWidth` 在未激活对象上整趟生成字形

**现状**
- 体 `Core/TmpFont.cs:208-214`：`textWrappingMode=Normal` · `sizeDelta=(w,0)` · **`t.GetTextInfo(t.text)` 无条件**。
- `GetTextInfo` = `SetText`+`SetArraySizes`+`ComputeMarginSize`+**`GenerateTextMesh()`**（`MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TextMeshPro.cs:360-374`），**没有 `m_isAwake` 闸**（对照 `OnPreRenderObject` `:2119` 有闸）⇒ 未激活时字形照生成、Phase III 被 `m_renderMode=DontRender` 跳过 ⇒ **「有字模、没渲染网格」**（A260 根因全文 → `资料/普查产出_1008/X_RewardsScene崩溃修.md` §1·3/§1·4）。
- 非调不可的理由：折行宽出自 `ComputeMarginSize()`（`TextMeshPro.cs:2015-2032`），它**只在 `OnEnable`/`GetTextInfo`/`OnValidate` 跑**。
- **`Battle/Label.cs` 与 `Core/TmpFont.cs` 都没有 `OnEnable`、没有 `isActiveAndEnabled`**（现读全文件）⇒ 今天没有任何「激活时补做」的口子。
- 调用面：`SetWrapWidth` **15 真调用**（`Core/CardView.cs:1751/1794` · `Shell/MenuDraw.cs:1278/1293` · `MainMenuRuntime.cs:929/933` · `MatchLogRow.cs:143` · `PlayerProfileWindow.cs:585` · `PromptPopup.cs:117/240` · `SettingsWindow.cs:1387` · `Deck/DeckRuntime.cs:2973` · `Editor/{ChatBoxProbe.cs:64,DeckScene.cs:2360}`）＋ **`SetAutoFitBox` 47 处**（其第一句就是它，`Battle/Label.cs:426`）⇒ 两条折行入口共用这一处。
- A260 现场链：`ForgeTab.BuildCell`（**未激活**的锻造页）→ `Shell/MenuWindowBase.cs:281` → `MenuDraw.TextBox:1293` → 这里。

**判据**
- TMP 自己的 `OnEnable`（`TextMeshPro.cs:630-666`）= `ComputeMarginSize()` + `SetAllDirty()`；**`SetAllDirty` 只登记重排**，真生成走 `TMP_UpdateManager` 的更新/渲染回调 ⇒ **批处理无帧循环 ⇒ 不跑**（`CLAUDE.md` §三）。⇒ 「只加闸、等 TMP 自己补」**在自检里恒不成立**。
- `ForceMeshUpdate()` 未激活时**早退**（`TextMeshPro.cs:2119`，除非 `ignoreActiveState:true`）。
- ⇒ 延后那一刀**必须显式生成**（`GetTextInfo` 或 `ForceMeshUpdate(true)`），**并补 `Label.RefreshBounds()`**（`Battle/Label.cs:567`，否则 `WorldW`/摆位还停在旧版面）。

**可选做法 + 代价**

| # | 形状 | 改哪 | 批处理（无帧循环） | 代价 / 风险 |
|---|---|---|---|---|
| 1 | **`Label` 记待办 + 激活时补**：`SetWrapWidth` 只存待办（**宽度 + 模式两样**）；`_tmp!=null && _tmp.isActiveAndEnabled` 才立刻做；加 `OnEnable(){TryApply();}`，**并在 `SetText`/`RefreshBounds` 尾巴再试一次**（防「激活那刻 TMP 子件还没 `Awake`」） | `Battle/Label.cs` + `TmpFont` 出一个不加闸的 `SetWrapWidthNow` | ✅ `SetActive(true)` **同步**触发 `OnEnable`（不等帧）；补做那一下自理 | 🔴 **模式会被打乱**：`MainMenuRuntime.cs:929/933` 是 `SetWrapWidth` **紧跟** `SetWrapping(false)` ⇒ 只存宽度会把 `0` 静默变回 `1` |
| 2 | **旁挂待办表 + 显式 flush**：`TmpFont` 静态表 + `TmpFont.Flush(root)`，谁激活谁调 | `Core/TmpFont.cs` + 每个激活点（页签切换 / 窗 `Show`） | ✅ 完全确定、不看 Unity 回调序 | 要**穷举激活点**，漏一个 = 又静默；静态表跨自检要清 |
| 3 | **待办挂在 `_tmp` 那个 GO 上**（小 `MonoBehaviour`；同 GO 上 `Awake` 必先于 `OnEnable`）＝ 1 的加固版 | 新文件（如 `Core/WrapWidthApplier.cs`）+ `TmpFont.cs` | ✅ 顺序有保证 | 多一个组件/文件；只在真要延后时才挂 |
| — | ❌「只加 `isActiveAndEnabled` 闸」（已被否定那版）：`sizeDelta` 写对了，**网格永不重排** | | ❌ 自检里恒不生效 | 静默视觉回归 |

**怎么断言**（宿主 `Editor/RewardsScene.cs`，A260 现场）
1. `tabButtons.Click(2)`（现 `:2077`）**之前**断 `LineCount == 0`（`Battle/Label.cs:334`）—— 现在这条会**红**（红的就是缺陷本身）。
2. `Click(2)` **之后**断 `Wrapping == true` + `LineCount >= 2` + `WorldW ≈ 框宽`。
3. 工厂级：父链未激活 → `SetWrapWidth` → 断 `LineCount==0` → `SetActive(true)` → 断 `LineCount>=2`。
⚠️ 建议加**一个只读口**（先例 `Label.DumpSizes():534`）让断言看到「生成没生成」，别去摸 TMP 私有字段。

**⚖️ 判不了**
- 形状 1 里 `Label.OnEnable` 与 TMP 子件 `Awake` 的先后 —— Unity 未文档化；要么按形状 3 规避、要么实跑量一次（⛔ 别用「应该先父后子」顶替）。
- 批处理里 `SetActive(true)` 是否**同步**跑完 `OnEnable`：我按 Unity 语义判「同步」、**未实测**（本件不跑 Unity）⇒ 要一次实测坐实。

## Q2 · A276 —— 三处「世界 z 反推局部 z」

**现状（现读；父链一并给数）**
- `Battle/WaitBanner.cs:212`：`_popup.transform.localPosition = (cx, cy, Z − transform.position.z)`。`_popup` = `MenuDraw.Nine` 的根（队列 `BattleQChrome=3000`，`:191`），**父 = WaitBanner 自己**；兄弟 `_shade:161` = 裸局部 `Z+0.02`、`_text:239` = 裸局部 `Z−0.01` ⇒ **同一父节点下两套口径**。
- `Battle/SettingsPanel.cs:199`：`_resignBtn`（`MenuDraw.Nine`，队列 `QPanel`）同理，**父 = SettingsPanel**；兄弟 `_close/_closeIcon:167/169` = 裸局部 `Z−0.01/−0.02`、`_resignText:204` = `Z−0.02`。
- `Battle/MulliganPanel.cs:258`：`pos = c.transform.position + (0, −cardH·0.30, Z − c.transform.position.z)` ⇒ **`pos.z` 恒 = `Z`**（那一项自相消），随后被 `ImageQuad.Create` 写进 **localPosition**（`Battle/ImageQuad.cs:134`）⇒ **这一处是恒等式**，与兄弟（`:145/195-197` 裸局部 `Z`/`Z−0.05`）同口径。⚠️ 同一行的 **x/y 拿卡的世界坐标当局部坐标**（同族第二例，今天等价）。
- **父链数字**：`HudRoot`（`Battle/BattleDriver.cs:6879-6882`，`SetParent(transform,false)`）· 场景根 `Battle`（`Editor/BattleScene.cs:8840` `new GameObject` 无缩放）· 三件面板都 `SetParent(parent,false)`、全仓无人写它们的 `localScale` ⇒ **`transform.position.z ≡ 0`、`lossyScale.z ≡ 1`**。唯一动 `hudRoot.localPosition` 的是震镜头：方向 `dir = Vector3.up`（只有 x/y，`Core/CardFeel.cs:731`）、`OnKill` 复位（`:754`）。
- 这个值**只喂渲染排序**：三处四层**全在同一队列 3000**（`_popup`/`_fillRoot` 显式传；`_shade`/文字走材质默认，`Battle/ImageQuad.cs:126/167-178` 写清默认就是 `Sprites/Default` 的 3000）⇒ 同队列里**只剩 z（到相机距离）在排**。

**判据 —— z 序有没有被 `RenderQueue` 取代？**
- **没有取代，只是「同队列才轮到 z」**：`资料/已知的坑.md:237/1410` 的口径是「该分层的要分队列」，而这三处**恰恰都在同一队列** ⇒ z 是**唯一**判据；相机正交 ⇒ 它是确定的（不是那条「靠几何偶然」的坑）。
- ⇒ 这条**不是「根本不该改」**：今天无观测影响（三项都退化成同一个数），但 z 在这里**仍承重**。

**三条候选**

| | 什么条件与今天不同 | 会不会真改排序 |
|---|---|---|
| **(a) 裸局部 z**（= 兄弟件口径；Mulligan 那处删掉自相消项） | **任何条件都逐位相同**（父世界 z 恰为 0） | 不会（三个数一个都没变） |
| **(b) `(Z − parent.position.z)/parent.lossyScale.z`**（=「落世界 z = Z」；`MenuDraw.Local` 就是这口径 `Shell/MenuDraw.cs:25-26`） | 父链**被挪 z** 或**带非 1 缩放**时才分家 —— 今天都不发生 | 今天不会；将来父链一动 (a)(b) 就分家 |
| **(c) 维持现状 + 如实标注** | —— | 不会；但两套口径并存 |

**⚖️ 我建议 (a)**，理由：① 同一父节点下的四层深度**只该有一套口径**（现在 `_popup` 用世界、兄弟用局部）；② 选 (b) 就引入「父链 `lossyScale`」这个会腐化的依赖（本仓已立「别指望 `localScale`」那条）；③ (a) 今天**逐位不变** ⇒ 零观测风险。
**我判不了**：这三件**原版**的 z 关系没有判据（`Z` 常量本身是我们挑的）⇒ (a) 是「我们内部自洽」，**不是「照原版」**；写进正本时应写成「**收口径**」而不是「对齐原版」。

## Q3 · A283 —— 弹窗打开时顶栏 3600–3604 谁吃点击

**现状**
- `Shell/MainMenuRuntime.cs:167-168`：`QBarPanel 3600 · QBarAvatarFrame 3601 · QBarContent 3602 · QBarText 3603 · QBarOverlay 3604`；`:151-166` 写着「整条顶栏抬到所有窗之上」＋**自记一条反证**（原版兄弟序里 `3 - PopUp Holder` 排在 `Upper bar` **之后**）。
- `Shell/PointerLayer.cs:1009-1013`：`PointerReachable` —— **不属于任何 `GameWindow` 的按钮恒放行** ⇒ 弹窗打开时顶栏照样进候选；挑赢家 = **`RenderQueue↓` 再比 z**（`:1040-1044`）。
- 断言（现读）：`Editor/MainMenuScene.cs:392`（顶栏档 > 最高窗档）· `:413`（带内 ≥12 张图）· **`:1456-1457`「顶栏压住选卡组弹窗」**。

**判据 —— 本地够判；四条独立信号全指「原版弹窗在顶栏之上」**
1. **兄弟序**（现读原始 JSON）：`RectTransform_1540`（`Safe area Only Horizontal`）的 `m_Children = [1103, 1169, 1095, **1092 Upper bar**, 1115, 1109, **1121 3 - PopUp Holder**, 1098, …]` ⇒ 弹窗在后 = 画在上面。
2. **排序层（最硬）**：`3 - PopUp Holder`（GO 45）上挂 `Canvas_1075` = `m_OverrideSorting: true` · `m_ReceivesEvents: true` · `m_SortingLayerID **1054366423**`（**我亲读**）；而 `globalgamemanagers/TagManager/TagManager_3.json` 的 `m_SortingLayers` 里 **1054366423 = `PopUps`（第 8 条）**，顶栏所在的根画布 `Canvas_1077` 是 **`Default`（第 1 条）** ⇒ Unity 语义下**排序层索引压过 order** ⇒ `PopUps` **永远**画在 `Default` 之上（与 order 无关的硬保证）。
3. `1`/`2` 两颗锚点只有 RT+WindowHolder，**只有 `3 - PopUp Holder` 多一颗专用 `CustomRaycaster`** ⇒ 原版对「弹窗在最上」是**额外硬保证**（我复核了那两颗 Canvas 字段；`CustomRaycaster` 一条转引 `资料/普查产出_1006/A154_A155_窗口档位与缩放.md:124-131`）。
4. 名字阶梯 10/5/15（below / above / popup）＋ `windowsPlacement = 15`：选卡组弹窗**两个变体都是 15**（`A154:326-327`；我们 `Shell/DeckSelectionPopup.cs:204` 同）⇒ 走的正是 `3 - PopUp Holder`。

⇒ **结论：原版弹窗打开时，顶栏那一带吃到的是弹窗那一层（压暗层）⇒ 点它 = 关窗。我们「顶栏恒可点」= 真偏离。**
⇒ 且原版**渲染序 = 射线序**（同一条 canvas 排序）⇒「谁画在上」与「谁吃点击」**是同一件事，拆不开**。

**⚠️ 与 2026-09-28 的记录正面冲突**：`_tmp_view/项目任务_瘦身前备份_1008.md:196`（现正本里那条指针已随 10-08 瘦身消失）写「我们按实拍」，实拍 = **用户给的截图**（`资料/真Play待验清单.md:116` D17：「原版核不了实况 ⇒ 只能对着用户那张实拍比」）。**上面 4 条本地判据全部与那张实拍相反。**

**「我们那 5 个档位是不是照原版写的」——不是照抄的数**：原版没有「逐件队列」这回事（uGUI = 兄弟序 + 一层 `PopUps`）；`3600~3604` 是**我们自己起的梯子**（`MainMenuRuntime.cs:151-157` 如实写着「从 3600 起、最高窗档 3500」），带内相对次序才取自原版兄弟序（Highlight→Border→Image，`:141-149`）。⇒ 它把原版「弹窗在最上」**反过来**了。

**可选做法 + 代价**

| # | 做法 | 代价 |
|---|---|---|
| A | 维持现状 + 如实标注（= 2026-09-28 用户拍板） | 与 4 条本地判据相反；这一格永远挂着 |
| B | 顶栏整条降回弹窗之下（`QBar*` < 各窗档） | **推翻用户拍板**；含**没有全屏压暗层**的窗也会盖住顶栏 |
| C | **只拆命中、不动渲染**：`PointerReachable` 加「有非 `Closed` 的 `GameWindow` 且其压暗层覆盖该点 ⇒ 顶栏不可达」 | 造出「顶栏看得见、点不到」——与原版「渲染序=射线序」的模型不符；动共用件 `Shell/PointerLayer.cs`（多条宿主受影响） |
| D | 先请用户把那张实拍原图（或那一帧）再给一次 | 零代码；唯一能解释「判据 vs 实拍」矛盾的路 |

**⚖️ 我判不了**：那张实拍**拍的是哪扇窗、哪一帧** —— 我只有二手转述（`MainMenuRuntime.cs:152-155`）、**没见到图**。若那图是练习窗/选卡组窗 ⇒ 与 4 条判据正面冲突；若图上是**没有压暗层的窗**或别的层（例如 `2 - Canvas Holder Above upper bar`）⇒ 两边可同时成立。
**「补一次原版实拍」这条路我劝退**（有实测判据）：`资料/命令速查.md` 记「**主菜单只拍得到顶栏**（97% 靠运行时实例化）」、卡组编辑界面「实例化即黑」⇒ SceneJumpShot 进主菜单**拿不到「弹窗打开」那一帧**。真要试，四行 = ① `scenes_scenes_mainmenuwarpforge.bundle` ② `8` ③ `d:/4/_tmp_view/orig_mainmenu_1010/`（别用 `_tmp_view/orig`，那儿会被当垃圾清） ④ 留空（只 dump 一次 + 截一张）；看 `runtime_ui_dump_*.tsv` 里 `3 - PopUp Holder` 有没有子件 —— **大概率没有**。
