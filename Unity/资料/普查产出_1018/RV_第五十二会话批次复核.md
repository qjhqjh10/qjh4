# RV · 第五十二会话批次 · 独立复核报告

> 角色：**独立复核代理（不是作者）** · 2026-10-18
> 对象 = `cd d:/4 && git diff`（未提交工作树，20 文件 / +512 −90）。
> 红线遵守：**只读**（只写本文件）；⛔ 未动 git、⛔ 未跑 Unity/player/`*.Run`、⛔ 未改正本/总表。
> 唯一的执行动作 = `TMPDIR=/tmp/wf_review bash d:/4/Unity/工具/typecheck.sh`（结果见 §五）。
> 证据里的行号 = **改后工作树**的行号；判据一律指「原版字段 / 反编译 / 项目红线」，不拿我们自己的常量证明自己。

---

## 〇、结论汇总（按严重度）

| # | 结论 | 一句话 | 证据 |
|---|---|---|---|
| **F1** | **确认缺陷（边界态 · 中）** | `CardText.TermOr` 新加的官方兜底**绕过了「字体闸」**：`Loc.Current==中文 && 中文字体资产缺失` 时会回**中文**，而 `Term()` 那条路回英文 ⇒ 中英混排 + 方块（本工程静默失败红线） | `CardText.cs:597-598` · `CardText.cs:38-43` · `CardText.cs:553-561` · `Loc.cs:2954-2970` |
| **F2** | **确认缺陷（低）** | `_mechPool` 会把「资源缺失时返回的空表」**永久缓存** —— `Load()` 明确设计成「读不到不缓存、下次还能读到」，但 `_mechPool` 把它锁死了，之后所有卡的机制判定都拿空 `createPool`、且**不出声** | `BattleDriver.cs:14352` · `CardDatabase.cs:204-216` |
| **F3** | **疑似（低 · 性能）** | 中/英档第一次 `TermOr` 缺键就会载入 **9.2 MB** `official_loc_13lang.json`（此前中/英档**从不**载它）—— 新增一次性 JSON 解析，量级未测 | `CardText.cs:597` → `Loc.cs:2954/2894/2850`；文件 9,237,426 B |
| **F4** | **确认缺陷（文档 · 低）** | `任务总表` N84 写「`Editor/RewardsScene.cs` **9 条断言**同步」，实际改了 **11 条**（6 × `CheckFontWindow` + 5 × `CheckA1212`），W2_A1232 §7·3 自己也是 11 行 | `任务总表.md:251` · `git diff -U0 -- …/RewardsScene.cs` |
| **F5** | **确认（文档/白名单 · 低）** | `Shell/MenuScroll.cs` 被改了（+6/−1，纯 doc），但 N66/N67 报告的白名单**不含它**、且报告第 33-34 行与 §⑤·2 明说「没去改它的 doc / 建议另补」—— 两份口径打架 | `git diff --numstat` · `W2_N66_N67_滚动与ESC.md:3,33-34,129-131` · `MenuScroll.cs:99-108` |
| **F6** | **不是缺陷（但提醒）** | `official_names.json` **不是格式变化**：2,758 → 3,156（**+398**，删 0），另有 **1 条值变更** `Cardbacks/Cardback_DA_Warlord_AzraelAA`（`Azrael/阿兹瑞尔` → `Supreme Grand Master/至高大导师`）。与生成器/官方表口径一致 | 逐键比 `git show HEAD:` 与工作树；`W2_N71` §② |
| **F7** | 不是缺陷（记账） | `TabGradT/TabGradC` 与 `ParentEventWindow` 那份**逐位相同的两份常量**（白名单原因）。违反「同一条规则只写一处」，建议下轮收口 | `MainMenuSubmenuWindow.cs:55-63` vs `ParentEventWindow.cs:179-186` |
| **F8** | 不是缺陷（记账） | `T09Probe.cs` 文件头写「测完即删（不是常驻工具）」，仍在树里且本批又改了 +58/−5（HEAD 已有，不是本批新增） | `Editor/T09Probe.cs:1-12` |

---

## 一、逐条详述（F1–F8）

### F1 确认缺陷（边界态）：`TermOr` 官方兜底绕过字体闸

- **改法**（`CardText.cs:591-598`）：`Lang13Current` 之后加
  `string vo = Loc.OfficialCurrent(term); if (!string.IsNullOrEmpty(vo)) return vo;`
- **`OfficialCurrent` 只看语档**（`Loc.cs:2954-2970`）：`Current == Chinese` ⇒ 取 `zh` 列（空则回 `en`），
  **完全不看 `TmpFont.Available`**。
- **红线**：`CardText` 文件头 + `Term()` 的 summary（`CardText.cs:553-561`）白纸黑字写着
  「**拿不到中文字体资产 / 语档不是中文时一律回英文列**（直接 `Loc.T` 会绕过这道闸，中文就画成空格/方块）」；
  `Zh`（`:38-43`）= `TmpFont.Available && Current == Chinese`，正是那道闸。
- **触发**：`Loc.Current == Chinese` 且 `TmpFont.Available == false`（字体资产缺失 / 被裁 / 回归）。
  - 有 Table 键 ⇒ `Term()` ⇒ `Zh ? T : EnOf` = **英文** ✅
  - 无 Table 键 ⇒ 新兜底 ⇒ **中文** ❌ ⇒ 同一张卡面**中英混排** + 中文渲成方块/空白。
- **建议改法**：过闸 —— 例如给 `OfficialCurrent` 加一个「中文列要求 `TmpFont.Available`」的判据，
  或在 `TermOr` 里对 `Current==Chinese && !Zh` 这一档改取**官方英文列**（新增 `Loc.OfficialEnglish(key)`，
  别把 `Lang13Text` 的私有口径复制一遍）；最省事 = 该态下**跳过官方兜底**、回 `fallback`（= 改前行为）。

### F2 确认缺陷（低）：`_mechPool` 锁死空表

- `CardDatabase.Load()`（`CardDatabase.cs:204-216`）：读不到 `Resources/cards_engine` 时**不写 `_cache`**，
  注释写明「这样补上资源后下一次调用还能读到」。
- `BattleDriver.cs:14352`：`var pool = _mechPool ?? (_mechPool = CardDatabase.Load());`
  ⇒ 第一次调用正好资源缺失 ⇒ `_mechPool` = **空 List**，后续永远不再 `Load()`。
- **后果**：`EffectText.OpHasMechanism(op, faction, <空池>, …)` 对「造牌 / 依赖卡池」那一类 op
  会判成「没机制」⇒ 卡面多标 `*`；**这条路径无日志**（静默）。改前每次 `Load()` 会自动恢复。
- **建议**：`var p = CardDatabase.Load(); if (p != null && p.Count > 0) _mechPool = p;`（空表不锁死）。

### F3 疑似（低 · 性能）：中/英档新增 9.2 MB 一次性载入

- 链路：`TermOr` → `OfficialCurrent` → `Lang13Text` → `LoadLang13`（`Loc.cs:2850-2870`）→
  `Resources.Load<TextAsset>("official_loc_13lang")` = **9,237,426 B / 10,862 键 × 13 列**。
- 改前中/英档**从不**载这个文件（文件头原话：「只在当前语言不是中文/英文时才载入」）。
- 现在第一次 `TermOr` **缺键**（卡名 `Card_Name/<英文名>`、阵营、关键词、工具短语那几族里 Table 没有的键）
  就会触发一次性解析。**不是缺陷**，但它是**新增的首帧成本**；T09 刚把「重复解析」清掉，别在这里又塞一个没量过的。
- **建议**：至少记一条「实测首次耗时」；若 >100 ms 再考虑换成小表（W2_N71 §① 已明确拒绝「再存一份 en+zh 全表」，
  那是铁律 6 的权衡 —— 若要动，需主对话裁）。

### F4 确认缺陷（文档）：N84 的断言条数 9 ≠ 实际 11

- 实际改动（`git diff -U0 -- …/RewardsScene.cs`）：
  `CheckFontWindow` 6 条（`:1310 / :1637 / :1733 / :1762 / :1783 / :1797`）
  ＋ `CheckA1212` 5 条（`:1856-1857 / :1859-1860 / :1866-1867 / :1871-1872 / :1891-1892`）= **11**。
- `W2_A1232` §7·3 那张表也是 **11 行** ⇒ 只有 `任务总表.md:251` 写成 9。
- **判据**：铁律 6（数字只留一处 / 两份迟早不一致）。**建议**：改 11，或直接写「见 W2_A1232 §7·3」。

### F5 确认（文档/白名单）：`MenuScroll.cs` 被改，而报告说没改

- 树里确实有 `Unity/MyGame/Assets/CardPresentation/Shell/MenuScroll.cs` 的 +6/−1（`NotchPx` 的 doc，
  `MenuScroll.cs:99-108` 新增「唯一一处不是原版值的传参 = 主菜单模式卡区 120」）。
- 但 `W2_N66_N67_滚动与ESC.md:3` 白名单 = `MainMenuRuntime / GameWindow / PointerLayer / LiveOpsEventWindow / MenuWindowBase`；
  `:33-34` 写「`MenuScroll.cs` 不在白名单，没去改它的 doc」；`:129-131` 又把它列成「建议」。
- **判断**：改动**内容正确、零行为变化**（纯注释），所以**不是实现缺陷**；问题是**口径不一致**
  （要么代理越界、要么主对话后补）。**建议**：主对话确认归属，把 N66 报告那两句改成「已补」或回退该 doc。

### F6 不是缺陷（但「1/1」这个描述会误导）

- 语义差集（用 Python 逐键比 HEAD 与工作树）：`count` 2,758 → 3,156；新增 **398** 键（Achievements / Avatars /
  Titles / Card_Name / Event_Description / Cardbacks / PrebuiltDecks …）；删除 **0**；共有键里值变化 **1** 条。
- 值变化那条：`Cardbacks/Cardback_DA_Warlord_AzraelAA` `Azrael/阿兹瑞尔` → `Supreme Grand Master/至高大导师`
  —— 与 `W2_N71` §② 记的一致（来源官方表当前值）。
- 已核：`Editor/` 里没有断言钉这条卡背名（`grep Azrael` 的命中全在引擎卡名/台词/异画图，不在卡背词条）。
- **结论**：不是缺陷；但「单行 JSON，1/1」看得出**行数**看不出**内容**，复核时不要只信 numstat。

### F7 不是缺陷（记账）：两份逐位相同的 Tab 渐变常量

- `MainMenuSubmenuWindow.cs:55-63` 与 `ParentEventWindow.cs:179-186` 的 `TabGradT/TabGradC` 逐位相同；
  代码注释已说明「`MenuDraw.cs` 不在本批白名单 ⇒ 两处各留一份」。
- 与铁律「同一条规则两处写 = 迟早不一致」抵触 ⇒ **建议**下轮收口到 `MenuDraw`（或一个公共常量）。

### F8 不是缺陷（记账）：T09Probe 仍在树里

- 文件头 `Editor/T09Probe.cs:12` 写「⚠️ 测完即删（不是常驻工具）」。本批又改了它。HEAD 已有，不是本批引入；
  收口时记得删（或明确留成常驻诊断工具并改掉这句）。

---

## 二、判据/数值核对 —— **已逐项验过、确认正确**

> 下面是重点找的第 1 条（数值/判据对不对）的逐项结果。**全部亲读原版 prefab / 反编译**，不是转述作者报告。

### V1 · D1/D4 的 188 换算 —— ✅ 正确

- 原版 `UIGradient` 实测（`bundle_menus_assets_all` 里 **8 份逐值相同**）：
  `m_color1 = (0.22352943, 0.01176471, 0.01960784)` · `m_color2 = (0.04705883, 0, 0.01568628)` ·
  `m_angle = 82` · `m_ignoreRatio = 1`
  （`MonoBehaviour_-3308698737451929820.json` 等；`Tracks/bg` 那颗 = `MonoBehaviour_8521710320608567247.json`，
  其宿主 GameObject = `bg_5183763081869680591.json` —— 报告里把**宿主 GO 的 id** 写成了「MB 的 id」，是**标签不准**、值本身对）。
- 反编译 `UIGradient__ModifyMesh.c:134-157`：投影按 **`dir` 交叉配对**（`x` 用 `dir.y`、`y` 用 `dir.x`），
  颜色 = `Lerp(c2, c1, t)`（**t=0 → c2**）。⇒ 82° 时有效轴 ≈ `(0.990, 0.139)`（左→右变亮、**t=0 在左 = c2 暗**），
  与 `R_T29` §0·2 的实拍（左 `#0C0004` 暗 → 右 `#390305` 亮）一致。
- 本仓 `CardArt.Gradient`（`CardArt.cs:478-501`）= `(cosφ, sinφ)`、**t=0 → c1** ⇒ 要还原必须
  `φ = 180 + atan2(cos82, sin82) = 188`。**188 正确**（与 `DeckRuntime:1396-1399` / `ParentEventWindow:150` 同一条换算）。
- ⚠️ 一个**既有**近似（不是本批引入）：`CardArt.Gradient` 的分母是 `|dx|+|dy|`，原版是 `fVar18` 那套；
  188（近水平）下左右中点差 ≈6% 色标（原版左中点 t≈−0.06 夹到 0，本仓 0.062）。不影响方向，记账即可。

### V2 · `GradC1` 的 G/B 订正 —— ✅ 正确

- 旧 `(0x39,0x05,0x03)` = (57,5,3) → 新 `(0.2235294, 0.0117647, 0.0196078)` = **(57,3,5)**，
  与 prefab `m_color1 = (0.22352943, 0.01176471, 0.01960784)` 逐位一致；`GradC2` = (12,0,4) 本来就对。

### V3 · D2 的 4 色标 `Gradient2` —— ✅ 正确

- prefab（6 份 type=1/blend=0/mod=1，逐位相同）：`ctimes = (0, 21974, 47995, 65535)` ·
  `key0` 黑(a=1) · `key1`/`key2` 白 · `key3 = (0.2830189,0.2830189,0.2830189)` · `m_NumAlphaKeys = 2` ·
  `atime 0 / 65535` ⇒ **alpha 取 `key0.a/key1.a = 1/1`**（色标自己的 `.a` 不生效）—— 与 `TabGradT/TabGradC` 逐位一致。
- `MenuDraw.GradientV`（`MenuDraw.cs:1609-1626`）：`t=0` 在**下**（`yTop = y2 − H·t1`）＋
  `SetCornerColors(cs[i], cs[i], cs[i+1], cs[i+1])`（BL/BR = 下档、TR/TL = 上档）—— 与 `Gradient2` **type=1 用 y 轴**一致。
- **关键**：反编译 `Gradient2__ModifyMesh.c:248-254` 在 `_modifyVertices` 时调 `SplitTrianglesAtGradientStops`
  ⇒ 原版**真的按色标把三角形切开**（不是 4 顶点线性）⇒ `GradientV` 的多带分解**是逐像素等价的**，不是过度拟合。

### V4 · D3 四角颜色顺序 —— ✅ 正确

- prefab `MonoBehaviour_8800391613077463723.json`：`m_topLeftColor = m_topRightColor = (0.22352941, 0.01176471, 0.01568628)` ·
  `m_bottomRightColor = (0.11320752, 0.00160199, 0.00366875)` · `m_bottomLeftColor = (0.15094340, 0.04912781, 0.05101327)`
  —— 与 `DailyStreakPopup.cs:377-381` 的四个实参**逐个对上**。
- `ImageQuad.SetCornerColors(bl, br, tr, tl)`（`ImageQuad.cs:425-429`）写进 `m.colors` 的序 = 顶点序
  0/1/2/3 = 左下/右下/右上/左上（`ImageQuad.cs:386-390`：`y0 = −anchor.y·h` = 本地底 = 屏幕底）
  ⇒ `bl` 确实是**屏幕左下**；原版 `Bilerp` 的 y 也是本地 +y 向上 ⇒ 语义一致。
- 原版 `bg` 的 `Image`：`m_Sprite = 0`（null）· `m_Color` 白 · `m_Type = 0(Simple)` ⇒ 用 `CardArt.Solid()` + 顶点色正确。
- 原版 rect `sizeDelta (−0.00012, 812.1)`、`a=(0,0.5)-(1,0.5)`、`pos y=−18.888` ⇒ 0,152.85→1920,964.95，
  与代码 `Bg` 常量 0,152.84→1920,964.94 吻合（四舍五入）。

### V5 · D5 的 Multiply 顶点色算法 —— ✅ 正确

- prefab `MonoBehaviour_8417643871566819108.json`：`_gradientType = 0`（横）· `_blendMode = 2`（Multiply）·
  `_modifyVertices = 1` · `key0 = (0.10377359, 0.00440548, 0.00440548)` @ `ctime0 = 4241/65535 = 0.06471` ·
  `key1` 白 @ 65535 · `atime 0/65535`、`key0.a/key1.a = 1/1` —— 与代码注释逐字一致。
- 反编译 `Gradient2__ModifyMesh.c:303-314`：`==1` 加法、**`==2` 乘法（求值色 × 原顶点色）**、其余**替换**；
  同一 `if` 块里 alpha 也一起乘/替。原版宿主 `Image.m_Color` = **白 (1,1,1,1)** ⇒ 乘法与替换同结果 ⇒
  把顶点色设成求值色、贴图照乘 = 逐像素等价（`ImageQuad` 的网格顶点色 → shader `tex × 顶点色`）。
- **两带分解正确**：`_modifyVertices` 时 `SplitTrianglesAtGradientStops` 会按色标切三角形 ⇒
  「0..6.47% 恒定 key0 + 6.47%..1 线性到白」是**原版真实渲染**（不是把 4 顶点线性当成了原版）。
- ⚠️ 原版侧栏 `Background` 的 sprite 是**外链**（`m_FileID=2 / pid -2833714758900102314`），
  本 bundle 的 `Sprite/` 里**没有**对应文件（`glob` 0 命中）⇒「我们那张 `40k_main_tab_background` 就是这张 sprite」
  本轮**没独立证实**（沿用既有口径）。见 §四。

### V6 · D6 压暗 alpha 0.773 —— ✅ 与同族一致（`SettingsWindow.ShadeColor`）

### V7 · A1232 四族数值 —— ✅ 真的来自「有 GUID 的独立 prefab」

11 颗 TMP 逐字段实读（`m_fontSize / Min / Max / Base / enableAutoSizing`），与 W2_A1232 §2 表**逐位一致**：

| MB | 节点 | 实读 字号 / auto / base | 代码落点 |
|---|---|---|---|
| `-4404432906334234416` | Daily Mission `progress` | 35 / **[25~35]** / 36 | `MissionsTab.cs:747` |
| `2940275149309614288` | Daily Mission `Button Text` | 35 / **[5~35]** / 12 | `:759` |
| `765343277870163540` | Daily Login `count` | 40 / **[20~30]** / 36 | `:940` |
| `9034657916644959828` | Daily Login `Button Text` | 35 / **[12~35]** / 12 | `:953` |
| `8273104765812614740` | Daily Login `Timer` | 28 / **[15~28]** / 36 | `:1002` |
| `311881750146743500` | Skulls `header/name` | **30** / [20~30] / 46 | `:1073` |
| `-8063506949765759796` | Skulls `count` | **30** / **[20~30]** / 36 | `:1193`（`fontPx: 30f`） |
| `8596436757323545804` | Skulls `Button Text` | **35** / **[12~35]** / 12 | `:1211` |
| `-997214307310601012` | Skulls `Timer` | **28** / **[15~28]** / 36 | `:1228` |
| `1212692818519020298` | Weekly `header/name` | 36 / **[20~36]** / 46 | `:1278`（`20f`） |
| `6907930910134838868` | Daily Login `header/name` | 30 / [20~30] / 46 | `BuildCardHeader`（未变） |

- **父链**：`-8063506949765759796` 的根 = `Daily Skulls Mission Container`（`m_Father = 0`）；
  `-4404432906334234416` 的根 GO = `Daily Mission Container`（pid `1682130975087859920`）。
- **Addressables 容器**（`AssetBundle/AssetBundle_1.json`，297 条 `[guid, {asset:{m_FileID,m_PathID}}]`）：
  `b07892dd…` → `Daily Mission Container` · `c4be7a6f…` → `Daily Skulls Mission Container` ✅
  ⇒「页内副本无 GUID、运行时只能选独立 prefab」这条结构判据成立（服务端到底选哪一档仍是**假设**，文档已如实标注）。

### V8 · 断言没有被削弱 —— ✅

- `CheckFontWindow`（`RewardsScene.cs:11510-11545`）/ `CheckA1212`（`:11553-11582`）读**实际 `Label` 的 TMP 字段**
  （`FontSizeMin/Max/Base` → `FontSizeToPx`）与**字面量**比，期望值来自 prefab，不是从实现常量读回来的。
- `CardBaseDemo`：旧期望 `"TAU46"` → `"Sense of Stone"`，并补 `enName != "TAU46"` 判别式（防「两边都回 id」也绿）✅。
  官方表 `Card_Name/TAU46` 实读 = `("Sense of Stone", "石之感")` ✅。
- A1232 的 11 条新期望值都 = prefab 值 × `localScale 1.15`（例如 25/35 → 28.75/40.25、20/30 → 23/34.5、12/35 → 13.8/40.25、
  15/28 → 17.25/32.2），**每条都对得上 §V7 的实读**。

### V9 · 缓存修复的副作用 —— ✅ 安全（附一条边界提醒 = F2）

- `CardDef` 全字段是 **只读取值属性**；唯一的可写成员 = `public CardDef GoesSecondCard { get; set; }`（`CardDef.cs:136`），
  而它只在 `CreatePool.ResolveGoesSecond`（`CreatePool.cs:668-687`）里写，那个方法**只被 `CardDatabase.Parse` 调**
  （`CardDatabase.cs:327`，全仓 grep 无第二个生产调用点）⇒ **浅拷贝不会泄漏写**。
  `StartWithInHand` 的 `Add` 只在 `CardDef` 构造期（`CardDef.cs:1671`）。
- `_mechPool` 只读：`EffectText.OpHasMechanism(…, IReadOnlyList<CardDef> createPool, …)`（`EffectText.cs:1781`）✅。
- 列表本身：`Load()` 每次 `new List<CardDef>(_cache)` ⇒ 调用方 `Sort/Add/Remove` 污染不到缓存 ✅（注释的动机成立）。
- 边界：**F2**（空表被 `_mechPool` 锁死）。另：`InvalidateCache()`（`CardDatabase.cs:199`）目前**零调用点** —— 不是缺陷，
  但意味着「同一 Unity 会话里改完卡表想重读」这条路**还没人用**，将来要用得自己调。

### V10 · `Loc.T` 是否改变已有键 —— ✅ 不变

- 新分支写在 `if (!inTable)` 内、且排在 bulk 的 `return` 之后（`Loc.cs:3284-3336`）；`HasEntry` 未动（`Loc.cs:3420`）⇒
  Table 命中 / bulk 命中的键**返回值逐字节不变**。
- 实际扫了可能翻的**字面量**断言：`No/Such/Key/___`（`SettingsScene.cs:1318`）· `No/Such/Key/__W1_S2__`（`DeckScene.cs:8085`）
  —— 都不在官方表 / bulk ⇒ 仍走缺键档 ✅；`Card_Name/Ironclad`、`Card_Trait/armour`、`Card_Name/Ember Warlord`、`Armies/Goff`
  —— 都在手写 Table ⇒ 不变 ✅。
- `CardText.Term`（`CardText.cs:562-572`）**没有被 diff 改**，但它内部那句 `Loc.T(term)` 的返回值会因 F1/T 的变化而变
  （缺 Table 键时从「键名」变成官方值）—— 这是 N71 的**意图**，不是缺陷；见 §四「没查清」。

### V11 · 行尾 / 越界 —— ✅ 行尾正常；白名单见 F5

- 逐文件 `rb` 计数：`BattleDriver / CardText / DeckRuntime / CardBaseDemo` = **CRLF 且 crlf==lf**（未混、未翻）；
  `Loc / RewardsScene / T09Probe / daily* / GameWindow / MainMenuRuntime / MainMenuSubmenuWindow / MenuScroll / MissionsTab /
  PointerLayer / CardDatabase / official_names.json / 三份 资料` = **纯 LF**。
- `git diff --numstat` 全部是**小数字**（最大 `MissionsTab 105/29`、`RewardsScene 23/23`），没有「整篇重写」那种数字 ✅。
- 白名单：`Shell/MenuScroll.cs` 超出 N66/N67 自述白名单（= **F5**）；`Editor/T09Probe.cs` 与三份 `资料/*.md`
  不在派单的「重点文件」里，但它们属于本会话别的子批（T09 探针 / 主对话正本），**不算越界**。

---

## 三、`CardBaseDemo` 那条「唯一翻掉的断言」的**静态**复核

- 扫过的工作：`Editor/` 里 `CardText.Name("…") / Keyword("…") / Faction("…") / Phrase("…") / Term("…")` 的字面量调用点
  全部列出并逐个查了「在手写 Table 吗 / 在官方表吗」⇒ **只有 `CardBaseDemo.cs:1476` 那条会翻**（就是本批改掉的那条）。
- ⚠️ **但这是静态结论**：动态键（`Name(c.Name)` / `Faction(c.Faction)` / `Keyword(canonical)` / `TermOr(动态键)`）
  无法穷举；而 `CardText.Keyword` 中文档那条路（`CardText.cs:732`）对「官方有、Table 没有」的 **31 条 `Card_Trait/*`**
  现在会回中文（例如 `frenzied` → 毁灭者）。这是 N71 的意图，但**没有现成断言钉它**（作者报告也这么说），
  并且**没有跑自检**（红线）⇒ **不能保证 0 条翻红**。见 §四。

---

## 四、没查清 / 未验证（如实登记）

1. **没有跑任何 `*.Run` / player / 真机**（红线）⇒ 「断言全绿」「唯一翻一条」都只是**静态推演**，
   不是实测。**建议**主对话在收口时至少跑一次 `CardBaseDemo.Run` + `RewardsScene.Run`（这两条覆盖本批断言改动）。
2. **`Daily Login Bonus Container` / `Weekly Mission Container` 的「根 GameObject 名」没读到**：
   容器表指向的 pid（`-6352492451613759916` / `1384672845988647690`）在本 bundle 的扁平 dump 里
   **没有对应 GameObject 文件**；但它们的子 MB 父链**终止于该 pid（`m_Father = 0`）** ⇒ 归属成立、名字这一格未证。
3. **原版侧栏 sprite 名字没独立证实**：`m_Sprite` 外链 pid `-2833714758900102314` 在本 bundle `Sprite/` 里 0 命中
   （`m_FileID = 2` 指向别的文件），所以「`40k_main_tab_background` = 原版这张」沿用既有口径、未复核。
4. **N79 的性能数字**（`2,477 ms → 6.5 ms`、`226,114 ms → 81.4 ms`、`2,424 ms → 0.4 ms`）来自作者日志，**未复跑**。
5. **F1 的触发条件是边界态**（`TmpFont.Available == false`）：正常构建里大概率不触发，我没有验证「字体资产能不能真的缺」。
6. **`T09Probe` 的反射调用**（`UnimplementedNotes`）没验证签名是否仍匹配 —— 它找不到只 `LogWarning`，不影响编译。

---

## 五、编译（唯一跑过的执行动作）

```
TMPDIR=/tmp/wf_review bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```
（独立 `TMPDIR`，不与别人互相覆盖；⚠️ 若别的代理当时正在写 `.cs`，假错会集中在其文件上 —— 本次 0/0，无此问题。）

---

## 六、搜过哪些词 / 哪些包（便于下轮复现）

- **包**：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 的
  `MonoBehaviour/`（全量扫 `_gradientType` = 27 个、扫 `m_color1` = 8 个、扫 `m_topLeftColor`）、
  `GameObject/`、`RectTransform/`（16,510 个建索引走父链）、`AssetBundle/AssetBundle_1.json`（297 条容器）、`Sprite/`（按名/按 pid）。
- **反编译**：`UnityEngine.UI.Extensions.Gradient2__{ModifyMesh,SplitTrianglesAtGradientStops,FindStops,BlendColor,GetBounds}.c` ·
  `UIGradient__ModifyMesh.c` · `UICornersGradient__ModifyMesh.c` · `UIGradientUtils__{RotationDir,Bilerp,LocalPositionMatrix,CompensateAspectRatio}.c`。
- **代码 grep**：`CardDatabase.Load()`（217 行/216 调用）· `SetCornerColors` · `GradC1|GradC2` · `TabGradT` ·
  `NotchPx|NotchK|_modeScroll|ModeScroll` · `TermOr|CardText.Name|Keyword|Faction|Phrase|Term` ·
  `GoesSecondCard|StartWithInHand` · `MissingCount|FallbackCount` ·
  `UiHasQuad|UiQuadActive|UiQuadRect|UiQueueOf|UiTextureName|UiNodeRect` · `"Background"|"Tab Buttons"|"Content Area"|"side_bg"` ·
  `"STUN"|"FLYING"|"RALLY"|"VANGUARD"|"STEALTH"|"ARMOUR"|"SHIELD"|"DODGE"|"BLIND"|"SURVIVOR"|"BACKLASH"` · `Azrael`。
- **数据**：`Resources/official_loc_13lang.json`（10,862 键，查 `Card_Name/TAU46` / `Card_Trait/armour` / `Armies/Goff` 等）·
  `Resources/official_names.json`（HEAD vs 工作树逐键差集）· `Loc.cs` 的 `Table`（按 `grep -c "\"键\""` 判是否登记）。
- **文件系统**：用户桌面 `C:/Users/qjh36/Desktop/warpforge参考图/` 现数 = **15 张**（与交接文档本批订正一致 ✅）。
