# W18 · `FltCell` 彻底去重 + A830 换口 + A835 文档漂移 —— 写手报告（2026-10-16）

> 白名单内落地：**`Shell/CollectionWindow.cs`**（主）· **`Editor/CollectionScene.cs`**（两笔各一处）。
> ⛔ 没跑 Unity（铁律 12）· ⛔ 没动 git / 两张正本 · ⛔ **`Core/FilterPanelModel.cs` 一个字没改**（不需要，见 §①·1）。
> 类型检查：`TMPDIR=/tmp/wf_w18 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**（跑了 2 次，两次都 0/0）。
> 行尾：两文件改前改后**都是纯 LF**（现数 CRLF 0 / LF 2888 · 6494）。`git diff --numstat`：CollectionWindow **109/123** · CollectionScene **165/53**。

## ① 结论

**1）`FltCell` 去重 —— 做了「彻底」那一档：整个镜像结构删掉了。**
派单写的是「把画图那段也搬进模型」，我**没有**把画图代码搬进 `Core/FilterPanelModel.cs`，两条理由：
- **搬不动**：那段画图依赖 `Node` / `Rect` / `TextAligned` / `AddHit`（含 `Scope(owner, …)` 闭包）/ `_fltScroll.Shift`
  —— 全是本窗私有，搬过去等于把窗口撕成两半。
- **不必搬**：真正的重复**不是**画图，是「**17 字段镜像 + 两处逐字段对拷**」。现在 `FilterPanel.Cells` **直接装
  `FilterPanelModel.Cell`**（与 `Deck/DeckRuntime.cs` 同一条路）⇒ **对拷点 = 0**，模型加一格字段这边**一个字都不用改**。
  `FilterPanelModel.cs` 因此没改 —— 它**已经是**唯一来源（字段名两边**逐字相同**，所以读点一个都没动，见 §③·A）。
- 唯一挪位置的是「加面板原点」那一跳：**建模型时**（`R = Abs(c.R)`）→ **画的时候**（`… Abs(c.R) …`）。
  同一个 `Abs`、同一入参、同一顺序 ⇒ **数值逐位相同**。
- ⚠️ 为什么不把原点做成模型的形参（那也能算「搬进模型」）：模型自己的 doc 写着「坐标一律是**面板内**……
  出口处再加面板原点」；让 `Cell.R` 按调用方产出**两种含义**是静默坐标 bug 的温床，且 `DeckRuntime.cs` 在白名单外。

**2）A830 —— 尺子从实现里搬出来了（比「只换条口」更强）。** `StyleLogoWidthPx`（读 `Label.WorldW` = 字段缓存
`_tmpW/_tmpH`）**整条删掉**：只改它的口 = 尺子仍长在**被测实现**身上（`CollectionWindow` 正是 `CollectionScene.Run`
的被测对象）。现在**自检自己量** TMP 的 `textBounds` —— 即 W6 刚建好的 `LabelRenderedPx`，与 `RectOf` / `TitleLeftPx` /
`ShopScene` / `RewardsScene` **同一条口**（先例口径 → `资料/普查产出_1014/RO_缓存口径与输入三件.md` §一·3）。

**3）A835 —— 派单列的 4 处全改**；另有 **2 处同源漂移**一并改了（§② 第 15、19 条；理由与「要不要改回来」见 §④·1）。

## ② 逐处改动（行号 = 现读）

| # | 位置 | 改前 | 改后 |
|---|---|---|---|
| 1 | `Shell/CollectionWindow.cs:214-221` | `struct FltCell {` … 17 字段 … `}`（原 214-259 整段） | 整段删，代以 8 行说明（模型那一份是唯一来源 · 画的时候过 `Abs` · ⛔ 别再建镜像） |
| 2 | 同 `:241` | `readonly List<FltCell> Cells = new List<FltCell>();` | `readonly List<FilterPanelModel.Cell> Cells = …`（+ 3 行 doc：类型 = 模型那一份、坐标 = 面板内） |
| 3 | 同 `:313` | `List<FltCell> _fltCells` | `List<FilterPanelModel.Cell> _fltCells` |
| 4 | 同 `:1676/1679/1689` | `cosmo ? c.R : _fltScroll.Shift(c.R)`（建模型时已 `Abs` 过） | `Abs(c.R)` / `Abs(c.Bg)` / `Abs(c.Lab)`（画的时候过）+ 6 行注释记「⛔ 一个都别漏」 |
| 5 | 同 `:1729` | `static Color CellTint(FltCell c)` | `static Color CellTint(FilterPanelModel.Cell c)`（与 `DeckRuntime.CellTint` **逐字同签名**；函数体一字未动） |
| 6 | 同 `:1883`（`BuildFilterRowModel`） | `var src = …; Build(…, src …); foreach (var c in src) _fltCells.Add(new FltCell {…16 行…});` | `FilterPanelModel.Build(FltState, FltW, _fltCells, …);` ← **对拷点 1/2 收掉** |
| 7 | 同 `:1900`（`BuildCosmoRowModel`） | 同上（`BuildCosmetics` 那一份） | `FilterPanelModel.BuildCosmetics(…, FltW, _fltCells);` ← **对拷点 2/2 收掉** |
| 8 | 同 `:1905`（`Abs` 的 doc） | 「面板内 → 页面绝对（**只此一处**）」 | 「换算**函数**只此一处；调用点 = 格子那三处 + `BuildNameRow` 那三处」 |
| 9 | 同 `:1238` · `:1296-1301` | `_styleLogo` 的 doc「自检用它量渲染宽度」· `public float StyleLogoWidthPx { get { return _styleLogo.WorldW*108f; } }` | doc 改成「自检**自己**量 TMP `textBounds`」；**属性整条删掉**（`_styleLogo` 字段保留 —— 建的那一处仍要判空） |
| 10 | `Editor/CollectionScene.cs:4890` · `:4911-4914` | `CheckTrue(win.StyleLogoWidthPx <= 512f+1f, …)`（`_styleLogo == null` ⇒ 读 `0f` ⇒ **恒真**） | `var sLogo=FindChild(spage,"Art Style Logo"); var sLogoLb=sLogo?…GetComponentInChildren<Label>(); float sLogoW=LabelRenderedPx(sLogoLb).x; CheckTrue(sLogoLb!=null && sLogoW>5f && sLogoW<=513f, …)` |
| 11 | 同 `:5065-5069` | 「收起时那三件 `Label` **压根没建出来**」 | + 订正（铁律 5）：**只对根治之前成立**；根治后照样建出来；①② 夹具照留，但「在不在」不再是验收口 |
| 12 | 同 `:5079-5083` | 「把 ④ 删掉 ⇒ 三条当场全红（…这三件**真的不在**）」 | + 订正：**已不成立** ⇒ ④ 今天没有断言盯着（要主对话裁） |
| 13 | 同 `:5097-5121` | 「②仍然开着的是「重建时机」那一半……**读的都是节点当下位置** ⇒ 恒不相交 ⇒ 全判框外」 | 整段改成**引文（留痕）** + ✅ 现状（框取 `ViewportClip.BaseRect`）+ 🔴 今天真欠的是**落点**（+385px）；「括注不成立」那条现读结论保留 |
| 14 | 同 `:5141-5146` | 「**根治 —— ⏳ 仍开着**……跨 `Shell/MenuDraw.cs` / `MenuWindowBase.cs`，改面大」 | 「**根治 —— ✅ 2026-10-16 已落**」+ 逐处出处 + ⏭ 同族仍开着的是 **A834** |
| 15 | 同 `:5147-5153` | 「验收断言 …… **删掉 ④ ⇒ 三条红**（这条断言的电就在这儿）」 | 订正痕 + 「④ 今天没有断言盯着 / 要主对话裁」（**这处超出派单列的 4 处**，理由见 §④·1） |
| 16 | 同 `:5174-5180` | 「这一步就是 A811 修法的**验收点**」+「**改坏法**：删掉 ④ ⇒ 三条当场全红」 | 同上（同源第二处，与第 12 条同一句话） |
| 17 | `Shell/CollectionWindow.cs:281-294`（`RowsBuiltOffBase` 头注） | 「**最近那一版是「按错框」建的**……**带闸的件全被判成「框外」**」+ 病根段（原文） | 原文留痕 + ✅ 现状 + 🔴 今天这个标志防的是**可见错位**（+385px），不再是「整列空白」 |
| 18 | 同 `:454-470`（④ 的理由段） | 「**为什么会欠那一版**……恒不相交……**整列是空的**」+「根治那条……**本轮不做**」 | 原文留痕 + ✅ 根治已落（`ViewportClip.BaseRect`）+ 🔴 今天欠的是**落点**；「闸本身」那条裁定原样保留 |
| 19 | 同 `:493-499` | 「配套的验收断言……**应当在**（改前会红）」 | + 订正：那个「改前」= **根治之前**；今天删 ④ 也不会红 ⇒ ④ 没断言盯着（**也超出派单的 4 处**） |

## ③ 受影响的断言（逐条）

**A. `FltCell` 去重（第 1 笔）—— 期望值一个都没动，静态判「逐位同值」：**
- 所有筛选格/标题/搜索框的几何断言（`Cell_$rar_*` / `Title *` / `Name Filter` / `Input Text` 一族）：
  入参变成 `Abs(c.R)` / `Abs(c.Bg)` / `Abs(c.Lab)`，**函数体、常量、调用顺序全未变**，只是从「建模型时算一次」
  变成「画的时候算一次」（`Abs` 是纯函数，`FltL`/`FltT` 是 `const`）⇒ 数值**逐位相同**。
- `win.FilterCellCount`（`CollectionScene.cs` 的 **31** / **14** 两条）与 `CosmoFilterCellCount`：只读
  `FilterPanel.Cells.Count` —— 元素**类型**变了、`Count` 语义没变 ⇒ **不受影响**。
- 「同一层不许压住 / 渲染队列」那族：`Node` / `Rect` / `AddHit` 的入参未动。
- ⚠️ 为什么这版风险最低：`FltCell` 的 17 个字段名与模型**逐字相同** ⇒ `c.Icon` / `c.IconOff` / `c.Key` / `c.On` /
  `c.Label` / `c.LabelPx` / `c.LabelAuto*` / `c.LabelBase` / `c.LabelRight` / `c.LabelCenter` / `c.LabelWrap` /
  `c.OffTint` 这些**读点一个字都没改**。只有 3 个 rect + 1 个签名动过。
- ⚠️ `FilterPanelModel.Build` / `BuildCosmetics` **不自己 `Clear`**（现读确认）⇒ 直接写进 `_fltCells` 依赖
  调用方先清（`RebuildFilterRowsNow:1613` 那一句，未动）。与 `DeckRuntime` 那条路一致。

**B. A830（第 2 笔）—— 受影响的就是那一条，且语义**更强**：**
- 旧 `win.StyleLogoWidthPx <= 512+1` → 新 `sLogoLb != null && sLogoW > 5f && sLogoW <= 512+1`。
  ⚠️ **不只是换口**：旧写法在 `_styleLogo == null` 时返回 `0f`，`0 ≤ 513` **恒真** —— 一处**假绿**
  （`AutoFitBox` 教训的同族：字没了照样绿）。新写法把那一档补成**红**。
- **值不变**：`RefreshBounds` 写 `_tmpW = |textBounds.size.x|`（`Battle/Label.cs` 的 `RefreshBounds`），
  而 `LabelRenderedPx` 量的是**同一个 `textBounds`** 过 `localToWorldMatrix` 的宽（TMP 子件缩放 = 1）
  ⇒ 两者**逐位同值**（与 W4/W5/W6 三个宿主同一条口径）⇒ 期望值 **512** 与容差 **1px 都不用动**。
- `StyleLogoWidthPx` 没有第二处读者（全仓 grep 只剩 doc 里的名字）。

**C. A835（第 3 笔）—— 断言一条都没改**：第 11–19 条**全是** `//` 或 `///` 注释、文案。

## ④ 没查清的 / 要主对话裁的

1. **🔴 派单列了 4 处漂移，我改了 6 处。** 多出来的是**同源**（A811 根治让它们一起失效）：
   「**删掉 ④ ⇒ 下面三条（前提）…在 当场全红**」（`CollectionScene.cs:5079-5083` / `:5147-5153` / `:5174-5180`）
   与 `CollectionWindow.cs:493` 那句「（改前会红）」。
   **为什么不只报不改**：它在**同一段注释里**和刚改好的订正**直接打架**（上面写「④ 补的是落点」、
   下面写「删 ④ ⇒ 三条红」）—— 正是铁律 5 说的「两份说法打架比没有更糟」。**若判越界，改回来只动这几行。**
2. **🔴 一个真缺口（不是文档问题）：`ApplyDrawerSlide` ④ 今天【没有断言盯着】。**
   根治之后「收起期间重建」也会把 `Input Text` / `Cell_owned/Label` / `Title Army` 建出来 ⇒ 那三条只查
   **在不在**的断言**删掉 ④ 也全绿**。要盯住 ④ 得改成量**落点**（展开到位后那三件应落在 `Abs(r)` 上、
   而不是偏 +385px）。⚠️ **本件没跑 Unity** ⇒ 这条是**静态推读**（判据 = `W11_A811根治.md` §③·C / §⑤·D，
   那份自己也是静态的）⇒ 建议**跑一次 `CollectionScene.Run`** 或**另开一笔**补落点断言。
3. **没验的**：本件**没跑 Unity**（铁律 12）⇒ §③ 的「逐位同值 / 不受影响」全是**静态推读**。
   按铁律 12 的判据本批属「**只动一个宿主**」= `Editor/CollectionScene.cs` ⇒ 要跑就**只跑 `CollectionScene.Run`**。
   ⚠️ 但 `Shell/CollectionWindow.cs` 是**两个宿主**的被测件（`CollectionScene` + `DeckScene`）—— `DeckScene`
   那一半本件没碰（改的只有筛选格那一段，卡组窗走 `DeckRuntime` 自己那份）⇒ 若主对话要保守，加跑 `DeckScene.Run`。
4. **顺手记（不改）**：`Deck/DeckRuntime.cs:3540` 一族用 `FltAbs(...)`，与 `CollectionWindow.Abs` 是同形的**第二份**
   （那文件在白名单外）。⚠️ 但**未必该合并**：模型产出的是「面板内」坐标，两扇窗各有一个出口函数是**同一条口径**的
   两个实例，合并反而要引入「面板原点」参数（§①·1 那条否决理由）。真要收，建议收成**模型侧一个静态助手**、两边转调。
