# B124 断言债分诊 3（CollectionScene 17 红 / DeckScene 2 红）· 2026-11-03

> 只读诊断。日志 `_tmp_view/collection_b123.log`（898,374 行 · `[Collection] === 结束：3427/3444 通过，**17 条失败**` @:898243）
> 与 `_tmp_view/deck_b123b.log`（744,611 行 · `DK === 结束：2422/2424 通过，**2 条失败**` @:744553）。
> 失败行一律按**行首前缀 + `✗`** 抽（⛔ 没用 `grep -c ✗`；日志里另有 4 处 `✗` 出现在**文案**内，已排除）。

## 〇 结论摘要

| 宿主 | 失败 | α 断言本身错/过期 | β 真实现缺陷 | γ 本批回归 | δ 夹具前提不成立 |
|---|---|---|---|---|---|
| CollectionScene | **17** | **14** | **1** | 0 | **2** |
| DeckScene | **2** | **1** | 0 | 0 | **1** |
| 合计 | 19 | 15 | 1 | 0 | 3 |

- **唯一 β（真实现缺陷）= `CollectionData.ParseDeck("")` 的卡组名被 `PlayerDeck` ctor 补成「新卡组」**（`Shell/CollectionData.cs:643` ↔ `RuleEngine/Core/DeckRules.cs:768`）。一行可修。
- **`DeckScene` 的 2 红不是「chip 判据 + A1007 子 sink 泄漏」**：A1007 的探针 `✗`（:735208）**没有泄漏**（父断言 `DeckScene.cs:1206` 断「自己的 sink 上恰好 1 条 ✗」**通过**）。第 2 条红是 **`B121WC ✗ C1`**（:743530），见 §二·D。
- **静态事件未退订那条不走**：两份日志**都没有真的抛 `MissingReferenceException`**（collection 里那 1 次是 `Q2_Assert.cs:169` 的**断言文案**，而那条断言是 ✓）；`DeckRuntime` 的两处守卫（`DeckRuntime.cs:4748` / `:9138`）都在，`B103F1` 那组 ✓。
- 15 条 α 里 **13 条是「2026-11-03 的产品改动按原版改了，但旧断言钉着旧语义」** —— 这正对上交接里那句「这些红不是本批引入」的**反面**：它们**确实是本批（121/122 会话 · commit `995da8d5`）带出来的**，只是产品侧改对了、断言侧没跟。

## 一 分组总表

| # | 组（日志前缀 / 区块） | 条数 | 日志行 | 断言源码 | 四类 | 一句话 |
|---|---|---|---|---|---|---|
| A1 | `[Collection]` 交互：点卡组格 | 1 | 794214 | `Editor/CollectionScene.cs:2243` | **α** | 点格**不再改**「当前卡组」（原版只开信息窗），断言还钉着旧语义（源码自己的 doc 就写着这条要改） |
| A2 | `[Collection]` A1051 剪贴板 | 2 | 801026 / 801092 | `Editor/CollectionScene.cs:3708-3710` · `3727-3745` | **α** | 剪贴板内容已按原版改成**多行分享串**，断言还在比「裸 base64」；`(2)` 还拿整串去 `FromBase64String` |
| A3 | `[Collection]` A11 前置（残留弹窗） | 1 | 822608 | `Editor/CollectionScene.cs:7037`（泄漏源 `:5301`） | **δ** | 两站式导入新开的那扇 `DeckInfoPopup(State=Import)` 自检**没关** ⇒ A11 扫到 1 扇 |
| A4 | `[Collection]` 页头 `Clear filters` | 2 | 825396 / 825426 | `Editor/CollectionScene.cs:8118` · `8144` | **α** | 卡背页无 chip 时钮被挪到 `ClearFilterX(2)=1488.59`（**且此时钮是隐藏的**），断言钉死 737.17 |
| A5 | `[Collection]` 卡背抽屉行序 | 2 | 825647 / 825695 | `Editor/CollectionScene.cs:8188` · `8198` | **α** | 该页行序已改成 **Owned 在前**（原版），断言还是「Army 在前」 |
| A6 | `[P4]` B4d | 1 | 886008 | `Editor/B104_P4_Assert.cs:400-403` | **α** | `SelectDeck` 已**不写盘** ⇒「落盘失败 ⇒ 不开窗」这条前提没了 |
| A7 | `B121C1` COLL-06 | 2 | 893631 / 893662 | `Editor/B121_C1_Assert.cs:98` · `110` | **α** | 拿 `Cell.R.y1`（含 `ArmyContentTop=50`）去比**行顶**常量 |
| A8 | `B121C2` A5 / A9 / D1 | 3 | 893987 / 894062 / 895800 | `Editor/B121_C2_Assert.cs:125-129` · `159-160` · `283-285` | **β** / **α** / **δ** | A5 = 真缺陷（空串名被补「新卡组」）· A9 = `InfoOf(null).Name` 是 `null` 不是 `""` · D1 = 夹具卡组没有阵营 ⇒ 卡背图取不到 |
| A9 | `B121C3` Ban Icon | 3 | 896774 / 896806 / 896927 | `Editor/B121_C3_Assert.cs:169` · `171` · `194` | **α** | 前两条忘了 `m_PreserveAspect=1`（方形 sprite 实绘 270×270）· 第三条把 `m_fontSizeBase` 当成 `m_fontSize` |
| B1 | `DK` chip 行判据 | 1 | 684776 = 744554（同一条） | `Editor/DeckScene.cs:2076-2080` | **α** | 拿 `ComputeLayout(nFac)` 与 `ComputeLayout(nFac+4)` 比 —— 后者就是「多 4 个阵营格」，跨一行本来就 +100 |
| B2 | `B121WC` C1 | 1 | 743530 | `Editor/B121_WC_Assert.cs:130-135` | **δ** | 夹具只放 1 张卡 ⇒ `Validate` 先在 ④ 张数档回 `TooFewCards`，走不到 ⑥b |

## 二 逐组详述

### A1 · 点卡组格不再改「当前卡组」（α · 1 红）

- 日志 `:794214` `✗ 点第 1 格 ⇒ 当前卡组换成它 —— 期望 [0]，实得 [13]`（`pick=0`、`before=13` ⇒ `CurrentIndex()` **没变**）；帧链 `CollectionScene.cs:2243` → `CollectionWindow.SelectDeck:5359` → `OpenDeckInfo:5454`。
- 产品侧 `Shell/CollectionWindow.cs:5330-5352` 的 `<summary>` **逐字写着这条要改**：
  「2026-11-03（B121-COLL-02）… 这一支**不再改「当前卡组」**… ⚠️ A600 那条断言若还钉着「点格后 `CurrentIndex` 变了」，它钉的是**旧语义**，要按本条改。」
  判据 = 原版 `CollectionDisplay<T>.Select` 只写 `SelectedItem` + `OnItemSelected`（`DeckInfoControls__OnSelect.c`）。
- **改断言**：`CollectionScene.cs:2243` → `Check(CollectionData.CurrentIndex(), before, "点格 ⇒ **不改**当前卡组（原版只开信息窗）")`。
  ⚠️ 同段 `:2247-2258`（开窗 / `Edit Deck` 交接下标）全部照旧 ✓，别一起动。

### A2 · A1051 剪贴板（α · 2 红）

- `:801026` `(1)` 实测串以 `* 测试卡组 1` 开头（多行），期望 = `DeckLibrary.ExportString(rawShare)`（**裸 base64**）。
- `:801092` `(2)` 文案自带 `（实得：**不是合法 Base64**）` ⇒ `Convert.FromBase64String(ClipGet())` 拿**整串多行**去解。
- 产品侧**是对的**：`Deck/DeckRuntime.cs:6046-6049` = `ExportString` → `DeckShareString(deck,s)` → 写剪贴板；原版 `DeckInfoPopup__ShareDeck.c:16` 写的正是 `MakeDeckString`（多行，`DeckInfoPopup__MakeDeckString.c`），末行才是载荷（导入端 `Split('\n').Last()`）。
- **改断言**：
  - `:3660` `wantShare` → `DeckRuntime.DeckShareString(rawShare, DeckLibrary.ExportString(rawShare))`；
  - `:3727-3728` 解析时先取**最后一行**：`ClipGet().Split('\n').Last()`，再 `FromBase64String`。
  - （`(2)` 的「期望值现场重算」结构不变 —— 只是把「被 Base64 的那一段」限定到末行。）

### A3 · A11 扫到 1 扇残留 `Deck info Popup`（δ · 1 红）

- `:822608` 测得 1 扇；`:822602` 是 A11 自己那次清扫的 `DeckInfoPopup:Close()` 帧。
- 打开点：`:814167` / `:814808` `[ImportDeck] 解析成功 ⇒ 开 Deck info Popup（State=Import）并关自己`；此后到 A11 之前**没有任何 `DeckInfoPopup:Close`**（`:814979` / `:815744` 是 `TryOpen 命中 Background 支`，只提前台）。
- 泄漏源 = `Editor/CollectionScene.cs:5294-5302`：③「真串」那一段开了 `DeckInfoPopup(State=Import)`（新行为，commit `995da8d5` 同日引入），**只断言它开出来了，没关**。
- 产品行为**符合原版**（两站式：解析 → 开信息窗 → 玩家点 `Import Deck` 才入库）⇒ **夹具侧补收窗**。
  建议在 `:5302` 之后加：
  `foreach (var p in Object.FindObjectsByType<DeckInfoPopup>(FindObjectsSortMode.None)) if (p != null) p.Close();`
  （A11 `:7035-7040` 那条**保留**：它是「又漏关一扇」的哨兵，别为了变绿去放宽它。）

### A4 · 页头 `Clear filters`（α · 2 红）

- `:825396` 断言中心 = **737.17**（= 左缘 612.17 + 250/2）；`:825426` 断言「标题左缘 1681 > `Clear filters` 文字右缘 1717」。
- 实测两者都指向 **左缘 1488.59 ⇒ 中心 1613.59、文字右缘 1717**（`1717 = 1613.59 + 103.5`，与实测字宽自洽）。
- 现行为：`Shell/CollectionWindow.cs:7112` `cx = n>0 ? HdrClearX(n) : ClearFilterX(page)`；`:7024` `ClearFilterX(2)=1488.59`（2026-11-03 `B121-COLL-08`），其余三页仍 612.2。
- ⚠️ **那一刻钮是隐藏的**：`CollectionScene.cs:8128-8138` 自己写着「没有筛选时 `Clear filters` 那一颗本身也是关着的 ⇒ 两颗 TMP 都量不到」，量之前临时点亮。
  原版判据 `clearFiltersButton.SetActive(0 < toggleOptions.Count)`（`…NotifyFilterChange`）⇒ 无 chip 时**看不见**，所以 1488.59 与标题「重叠」**当前不可见**。
- **改断言（推荐）**：
  - `:8118` → 期望值改成 `FilterPanelModel.HdrClearX(n) + 125f`（n = 该页 chip 数；n==0 时用 `CollectionWindow.ClearFilterX(page) + 125f`），并在文案里写明「这是**隐藏态**的位置，只钉『没被别的页的口径污染』」；
  - `:8144` → **加前提**：只有 `n > 0`（钮真可见）时才断「不叠」，且用 `HdrClearX(n)`；`n == 0` 时登记为「未验（钮隐藏）」⛔ 不要断。
- ❓ **悬而未决（不要顺手改）**：`CollectionWindow.cs:3479-3485`（2026-10-12）写着「四页都是 612.17…Cardback 那 876.42 是模板值…照 1488.59 摆会与标题压在一起」，与 `:7024` + `FilterPanelModel.cs:1208-1216`（卡背页保留 1488.59）**正面冲突**。本轮 `menu_dump.py "Cardback Tab" --depth 4` 现读：`Header ▸ Filters` = **592.2→1468.6（宽 876.42）**、`Clear Filter Button` = **1488.6→1738.6**（工具对该 CSF `h:MinSize？` 标了不确定）。⇒ **两份自家说法打架**，按铁律 5 应就地订正一处；在裁定前**别改实现**（改了会让 `B121_C1_Assert.cs:73-79` 五条红）。建议派一条只读代理按 prefab 序列化 + 三页对照定案。

### A5 · 卡背抽屉行序（α · 2 红）

- `:825647` `Army 第 1 格中心 y` 实测 **320.90**，期望 270.90；`:825695` `Owned 行中心 y` 实测 **195.90**，期望 758.71。
- 差值自洽：`320.90 = 155.9 + 65 + 50 + 50`（armyTop 65 = `CosmoSpacing1 15 + ToggleRowH 50`）· `195.90 = 155.9 + 15 + 25`（Owned 落在 `CosmoSpacing1`）。
  即：该页已改成 **Owned 在前 → Army**（`Core/FilterPanelModel.cs:1131` `ownedFirst`，2026-11-03 `B121-COLL-06`；判据 `menu_dump "Cardback Tab"`：`Spacing 15 → Owned Toggle 50 → Army Filter`，见本节 dump 现读）。
- **改断言**：`:8188-8190` 期望 y → **320.90**，文案改成「这一页 **Owned 在前**，Army 从 65 起」；`:8198-8199` 期望 y → **195.90**，文案改成「Owned 是第一行」。`x` 那两条（`:8187`）不变 ✓。

### A6 · P4 `B4d`（α · 1 红）

- `:886008` `B4d 落盘失败 ⇒ 不开 Deck info Popup` 实测 False（**开了**）。
- 前提已消失：`CollectionWindow.SelectDeck`（`:5352`）现在**只** `OpenDeckInfo(i)`，不写盘 ⇒ 既不可能「落盘失败」，也不会「因失败而不开窗」。
- **改断言**：删 `:400-403` 两条（`B4c` 现在是**空转发**：`CurrentIndex` 不变只因为谁都没动它 ⇒ 判零信息），改断**新语义**：
  「`cw.SelectDeck(tgt)` ⇒ 盘上文件**逐字节不变** + `CollectionWindow.LastOpened != null`」。
  ⚠️ 改完顺手核 `:408` 的 `wm.HidePopUp(true)` 能不能收掉 `SelectDeck` 现在**必然开**的那扇 `DeckInfoPopup`（收不掉 ⇒ 会更靠后的宿主排查漏窗）。

### A7 · B121C1 COLL-06（α · 2 红）

- `:893631` 收藏窗 `Army 行顶` 实测 **115**（期望 65）；`:893662` 卡组编辑窗 实测 **65**（期望 15）。
- `B121_C1_Assert.cs:98` / `:110` 用的是 `cells[i].R.y1`，而 `FilterPanelModel.BuildCosmetics:1136` 写的是
  `y = armyTop + ArmyContentTop + …`（`ArmyContentTop = 50f`，`:224`）⇒ **实测值恒 = 行顶 + 50**。两条**都**差 50 正是这个。
- **改断言**：`:98` 期望 → `65f + FilterPanelModel.ArmyContentTop`（= 115）；`:110` → `15f + FilterPanelModel.ArmyContentTop`（= 65）。
  或改成 `CheckNear(firstFacY - FilterPanelModel.ArmyContentTop, 65f/15f, …)` —— 后者更能挡「连 Content 偏移一起抄错」。
  ⚠️ `:97`（Owned 行顶 15）与 `:99`/`:111`（灭自证）**都通过**，别动。

### A8 · B121C2 三条

1. **A5（β · 真缺陷）** `:893987` 实测 名「新卡组」/ 卡 0 / 督军「」。
   `Shell/CollectionData.cs:643` `new PlayerDeck("", null, null, new List<string>(), 0)` → `RuleEngine/Core/DeckRules.cs:768`
   `Name = string.IsNullOrEmpty(name) ? "新卡组" : name;` ⇒ 空名被补。
   同一处坑 123 会话在**导入路**已按原版修过（`RuleEngine/Data/DeckLibrary.cs:494` `noLookup.Name = deckName ?? "";`），**空串那一路漏了**。
   **修实现**（1 行）：`:643` 后加 `broken.Name = "";`（或给 ctor 加一条「空名原样保留」的重载）。判据 = `CardDeck__DeckBasicSetup.c:21-24`（`+0x10` 名字原样写、无空名守卫）。
   ⚠️ 顺带：`DeckRules.cs:768` 那个**写死中文**的默认名绕过了 `Loc`（`MenuDeck/NewDeckName` 在 `Loc.cs:1966`）⇒ 英文档会印中文，属另一笔（见 §九 ③）。
2. **A9（α）** `:894062` `InfoOf(null).Name` 是 `null`；`CollectionData.cs:660-663` 的 `new DeckInfo()` 不给 `Name` 赋值。
   断言文案自己写的是「**字段全默认**的 `DeckInfo`（不抛）」—— 字段默认就是 `null`，与 `Name == ""` 自相矛盾。
   **两种改法择一**：① 断 `string.IsNullOrEmpty(CollectionData.InfoOf(null).Name)`（改断言，最小）；② 改实现 `new DeckInfo { Name = "" }`（与另一支 `info.Name = d.Name ?? ""` 的归一化一致）。**建议 ②**（调用方少一处判 null），改完这条断言原样留着当哨兵。
3. **D1（δ · 夹具）** `:895800` `CardbackFrontForTest == null`。
   日志 `:895608` 有 `[DeckInfo] 卡背取不到（`` / 阵营 ）⇒ Cardback 那一层不画，出声` —— 夹具 `B121_C2_Assert.cs:276` 的卡组 `WarlordId/DefensiveId` 全 `null`、`Faction = ""` ⇒ `CardArt.DeckCardback(null,"")` 为 null ⇒ `DeckInfoPopup.cs:2135` 整块跳过（`BuildCardbackFront` 连调都没调）。
   **修夹具**：给它一个**真督军**（`CollectionData.Raw(0).WarlordId`）或显式 `CardbackId`，让 `tex != null`。
   ⚠️ 同组 `:286` `if (front != null)`、`:301` `if (cb != null)` 是「前提不成立就**静默跳过**」的写法 ⇒ D1 红了、D2/D3 压根没跑（日志里确实没有 D2/D3 行）。按纪律应改成显式前提断言。

### A9 · B121C3 Ban Icon（α · 3 红）

产品实现 `Shell/MenuDraw.cs:3846-3860` **是对的**（`keepAspect: true` = 原版 `m_PreserveAspect = 1`，注释 `:3759` 自己写着），断言三处量错：

1. `:896774` 高 实测 **270.000** 期望 339.849：sprite `40k_Cross_icon_cross_big_Banned_card` 是 **512×512**，作者框 300×377.61 保比后**实绘 300×300**（宽那条 `:168` 期望 270 恰好 ✓，是可反证的自洽点）。
   ⇒ 期望改成 `OrigIconW * SX`（= 270）。
2. `:896806` 上沿 实测 **12.326** 期望 −22.599：保比后纵向居中，`−22.599 + (339.849−270)/2 = 12.3245` ✓（与实测逐位吻合）。
   ⇒ 期望改成 `CellRect.y1 + (OrigIconY + (OrigIconH - OrigIconW) * 0.5f) * SY`。
3. `:896927` 字号 实测 **32.400** 期望 34.425：读的是 `tlb.FontSizeBase`（= TMP `m_fontSizeBase`），实现传的是 `autoBasePx: TxtBasePx*sy = 36×0.9 = 32.4` ✓；38.25 是 **`m_fontSize`**（`MenuDraw.Text` 的 `fontPx` 形参，走 `SetGlyphHeight`）。
   ⇒ 期望改成 `TxtBasePx * SY`（32.4），文案改成「`m_fontSizeBase = 36`（38.25 是 `m_fontSize`，不是 base）」；要断 38.25 得先给 `Label` 加一个**标称字号**读口（现在没有，`Label.FontSize` 是 autosize 收敛后的值）。

### B1 · DeckScene chip 多选（α · 1 红 · 全批唯一一条「永远不可能绿」）

- `:684776`（重列 `:744554`）`（判据）多选 4 个 ⇒ Rarity 行顶必须一动不动（实测 100.0000）`，`DeckScene.cs:2078`。
- 代码 `:2076-2077`：`LNo = ComputeLayout(nFac)` · `LSel = ComputeLayout(nFac + 4)`；而 `FilterPanelModel.cs:82` `RarityTop = RowArmy + ArmyRowH(cells)`、`:65` `rows = ceil(n/3)`。
  **`nFac+4` 就是「多 4 个阵营格」**：13→17 跨一行（`ArmyCell = 100`）⇒ 差值恒 **100**。这条**与 chip 毫无关系**，写成什么样都必红。
- `git blame` 证据：`995da8d5 (2026-11-03)` 这一行是新写的（同一块 `:2061-2088` 全是 121/122 会话）。
- **改断言**：删 `:2077-2080` 四条（`LSel` 那半是旧语义「chip 算进格数」的残影）。真正的判别式在 `:2081-2087`（拿**真实 `Build` 输出**的 `$rar:common` y 比 `LNo.RarityTop + RarityContentTop`）—— 它 **✓**，够了。
  ⛔ 别改成 `ComputeLayout(s)`（那和 `LNo` 同一个函数 ⇒ 同义反复）。

### B2 · B121WC C1（δ · 夹具 · 1 红）

- `:743530` `Validate` 实读 **`TooFewCards`**，期望 `InvalidCard`。
- 夹具 `B121_WC_Assert.cs:127-133`：`Fresh()` 造的是**空库**，只 `CardIds.Add(UnitA)` **一张** ⇒ `DeckRules.Validate` 在
  **④ 张数**（`DeckRules.cs:486-488`，`want = CardCount(...)`）就返回 `TooFewCards`，**走不到 ⑥b**（`:502` 的 `IsNotDeckable`）。
- **修夹具**：把 `CardIds` 填满到 `want` 张（30 张 `UnitA`；⑥b 在 ⑦ 同名上限**之前**返回，所以 29 张重复不会先撞 `CopyLimitExceeded`），再断 `InvalidCard`。或者直接断
  「把张数那档喂饱之前的返回是 `TooFewCards`」—— **不推荐**，那等于放弃这条判据。
- **顺手发现（真偏离，属另一笔）**：**原版的次序是「先逐卡判、后数张数」**——
  `DeckUtility__ValidateDeck.c:44-56` 先调 `ValidateDeckCards`，通过后才比 `+0x58` 的张数（`*param_2 = 5`）；
  我们 `DeckRules.Validate` 是 ④ 张数 → ⑥b 逐卡。只影响**错误码优先级**（哪个错误先被报），不影响合法/非法判定。建议立账，⛔ 别在这轮顺手改（会动一大批错误码期望）。

## 三 疑似已修、待复跑确认

**无**。本次两份日志都是 123 会话修完之后跑的（collection 07:53 / deck_b123b 08:12），19 条红**全部**在现代码上复现。
（123 会话那三件修复 —— `AbilityHealRowDto/AbilityManaRowDto` 的 `[Serializable]`、`DeckLibrary` 空名、`B118W14_Assert` 零宿主 —— 与这 19 条无交集。）

## 九 顺手发现

1. **`PlayerDeck` ctor 的默认名是写死中文**：`DeckRules.cs:768` `? "新卡组"` 绕过 `Loc.T("MenuDeck/NewDeckName")`（键在 `Loc.cs:1966`）⇒ 英/其它语档会印中文。`DeckEditorState.cs:264` 那一路已走词条，ctor 这一路没有（且**没有 `🔶 自己设计` 标记**）。→ 建议补标记 + 改走 `Loc.T`。
2. **`B121_C2_Assert.cs:286` / `:301` 是「`if (前提) Check(结论)`」** —— D1 一红就静默带过 D2/D3（日志里确实没有 D2/D3）。按纪律改成显式前提断言。
3. **`B104_P4_Assert.cs:408` 的 `wm.HidePopUp(true)` 收不掉 `DeckInfoPopup`** —— 而 `SelectDeck` 现在**必然**开一扇。P4 这一段在 A11 之后跑，漏的窗会落到更后面的宿主排查里，建议改 A6 时一并核。
4. **A1051 的 `(1)` 与 `(2)` 现在共用同一条「过期口径」**：`wantShare` 走 `DeckLibrary.ExportString`、产品走 `CopyDeckToClipboard`（含 `DeckShareString` 包装）—— 两个函数名相近、语义已不同。建议把「分享串 = `DeckShareString(deck, ExportString(deck))`」这条收进**一个**测试口，别两处各拼一次。
5. **`CollectionWindow.cs:3479-3485`（2026-10-12 判据块）与 `:7024` / `FilterPanelModel.cs:1208-1216`（2026-11-03）互相矛盾**，且两份都自称有 prefab 依据（见 A4 的 ❓）。按铁律 5「两份说法打架比没有更糟」，**必须有一处被就地订正**并保留更正痕迹。
6. **`DeckRules.Validate` 的判据次序与原版不同**（见 B2 末段）—— 建议立账（账里归 `DeckRules` 类尾，别改主表）。

## 附 · 复跑口径

- collection：`[Collection] === 结束：3427/3444`，改完后应 = `3444/3444`（A3 的收窗不影响计数）。
- deck：`DK === 结束：2422/2424`，改完后应 = `2424/2424`。
- ⛔ 判绿红**只认这两行**；`>>> A1007 探针 ✗` 是**设计内**的子 sink 输出（父断言 `DeckScene.cs:1206` 靠它计数），⛔ 别当红。
