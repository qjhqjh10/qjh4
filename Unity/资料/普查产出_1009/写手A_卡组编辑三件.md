# 写手A —— 卡组编辑三件（A247 · A190 · ArmyRowH · 件4）

## 〇、一句话（三件各自：改了 / 已做完无需改 / 没做成）

| 件 | 账号 | 状态 | 一句话 |
|---|---|---|---|
| 1 | **A247** | ✅ **改了** | `BuildCosmetics` 加可选形参（缺省 = 共用常量 18）· **卡组编辑那个调用点传 26** · 共用常量一个字没动；配 **2 条断言**（其中 1 条是「缺省仍是 18」的对照） |
| 2 | **A190** | ✅ **改了** | `UiLabelText` 补 `FindDeep` 一步（形状照抄同族四条）；配 **3 条断言**（结构前提 + 2 条字串 + 1 条负例，共 4 条 `Check`） |
| 3 | ArmyRowH | ✅ **实现已做完（2026-09-28 就对了）+ 断言本次补上** | `ArmyRowH(13) = 50 + 5×100 = **550**`（现读确认）；**实现零改动**，只补 **2 条**几何断言（原来一条都没有 ⇒ 改回去没人会红） |
| 4 | **A224①** | ✅ **已做完，无需改动** | 两条「真读数」断言 **A92 那轮已经落了**（`Editor/DeckScene.cs:2300-2308`），现读 TMP 的真字段 `Label.WrappingMode`，期望 0 / 1 |

改动文件（**只有这三个**，行尾都没被翻）：
`Core/FilterPanelModel.cs`（+16/−2 · LF）· `Deck/DeckRuntime.cs`（+23/−3 · CRLF）· `Editor/DeckScene.cs`（+108/−0 · CRLF）。

---

## 一、逐件

### 件1 · A247 —— 卡组编辑窗卡背抽屉 `'Owned only'` 的字号下限

- **判据**（出处 = `资料/普查产出_1008/波C2_A181_A212收藏窗_A214一.md` §七①，那条命令可复跑）：
  `Deck Editing Menu > … > Cosmetic Display > Cosmetic FIlter > Filters > Owned Toggle > Label`
  = **字号 32 · `auto[26~32]` · 折行 0**（现读 `python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 18 --md`）。
  **收藏窗**卡背页那颗是 `auto[18~32]`（= 我们本来就对）⇒ 两扇窗不同，按 **A77⑩「按窗分参数」**。
  账号原文 → `项目任务.md:461`（A247）。

- **改了什么**：
  1. `Core/FilterPanelModel.cs:505-510` —— 新增常量
     `public const float CosmoOwnedFontAutoMinDeckEdit = 26f;`（判据、命令、两窗差异全写在它的 `<summary>` 里）。
  2. `Core/FilterPanelModel.cs:518-519` —— `BuildCosmetics` **加可选形参**
     `float labelAutoMin = ToggleFontAutoMin`（+ `<param>` 说明缺省 = 收藏窗那份 18）。
  3. `Core/FilterPanelModel.cs:540` —— 那颗 `'Owned only'` 的 `LabelAutoMin` 从
     `ToggleFontAutoMin` 改成 **`labelAutoMin`**。
  4. `Deck/DeckRuntime.cs:3086-3093` —— **卡组编辑窗那唯一的调用点**显式传
     `FilterPanelModel.CosmoOwnedFontAutoMinDeckEdit`（注释写明「只在本调用点传、⛔ 别动共用常量」）。
  - ⛔ **`ToggleFontAutoMin = 18f` 一个字没动**（`FilterPanelModel.cs:144`，`git diff` 里它没出现）；
    `Shell/CollectionWindow.cs:1723` 那个调用点没传新形参 ⇒ **收藏窗行为逐位不变**。

- **新增断言在哪**：`Editor/DeckScene.cs:2355` 与 `:2358`（在既有 A62 #6 那条 `折行=0` 断言的**下面**、
  同一个 `if (cosmoLb != null && cosmoLb.CanRenderChinese)` 里；`cosmoLb = _rt.UiCosmoFilterCellLabel("$owned")`）。
  它们断言的是**原版的哪个值**：**TMP 自己的 `fontSizeMin`/`fontSizeMax`**，
  折回画布 px（`Label.FontSizeToPx`，与 `Editor/MainMenuScene.cs:6409` / `Editor/RewardsScene.cs:4721`
  那两处同族助手**同一条口径**）⇒ 期望 **下界 26 / 上界 32**。
  ⚠️ **不是**读我们传进去的常量（那是自证）。换算关系可闭式验证：`nomPx = FontSizeToPx(cur)`、
  `_tmp.fontSizeMin = cur·(26/nomPx)` ⇒ `FontSizeToPx(fontSizeMin) ≡ 26`（精确，容差 0.6 绰绰有余）。

- **第 3 条断言（对照）**：`Editor/DeckScene.cs:2362-2377` —— 同一份模型、**不传新形参**跑一遍探针，
  断言 `Cell.LabelAutoMin` **仍是 18**（= 收藏窗那颗的原版值）。这条钉的是「⛔ 不许改共用常量」。
  ⚠️ 这条**不走 UI**（纯模型），所以谁把 `ToggleFontAutoMin` 改成 26、或把新形参默认值改掉，它必红。

- 🔴 **改坏法（能区分 26 与 18）**：
  · 把 `Deck/DeckRuntime.cs:3092-3093` 那个 `CosmoOwnedFontAutoMinDeckEdit` 实参**删掉**
    ⇒ 退回缺省 18 ⇒ `FontSizeToPx(cosmoLb.FontSizeMin)` 读成 **18**，与期望 26 差 8（容差 0.6）
    ⇒ **`:2355` 那条红**（文案：「卡背页 `'Owned only'` 自适应下界 = 原版 26」）。
  · 把共用常量 `ToggleFontAutoMin` 改成 26 ⇒ **`:2374` 那条对照红**（且收藏窗一起被改歪）。
  · 把 `ToggleFontPx` 改掉 ⇒ **`:2358` 那条红**。

### 件2 · A190 —— `UiLabelText` 只有 `Root.Find` 一步

- **先现读确认（简报要求）**：**当时确实只有一步**，未被别人改过 ——
  `Deck/DeckRuntime.cs`（改前）`public string UiLabelText(string key)` 的方法体就是
  `var t = Root.Find(key); var lb = t != null ? t.GetComponent<Label>() : null; return lb != null ? lb.Text : null;`
  （`git show HEAD:…DeckRuntime.cs` 同上，连 `Lookup` 都没有）⇒ **不是「已做完」，真改**。
- **判据**：同族另外四条（`UiHasQuad` / `UiTextureName` / `UiQuadCount` / `UiQueueOf`）**在 2026-10-05/10-07
  那轮已统一到 `Lookup` → `Root.Find` → **`FindDeep`** 的找法**（见它们各自的注释里那两段）；账号原文 →
  `项目任务.md:409` · 全文 → `资料/待办判据_1007.md` §A190。
- **改了什么**：`Deck/DeckRuntime.cs:2362-2373` —— `Transform t = Root.Find(key);` 之后**加一步**
  `if (t == null) { var deep = FindDeep(Root, key); if (deep != null) t = deep.transform; }`。
  **形状逐字照抄** `UiQuadActive` / `UiNodeRect`（直接子件优先、深查找兜底），⛔ 没有自己发明找法。
  ⚠️ 优先序没动 ⇒ 既有 key（`poolcnt_0` 那种直接子件）读到的是**同一个节点**、既有断言**一个数都不变**。
- **新增断言在哪**：`Editor/DeckScene.cs:1179-1209`（紧跟在既有 A77 ⑭ 那一段的**后面**，抽屉**开着**、
  且上一行刚验过 `UiFilterInputText == "Search"`）。共 4 条 `Check`：
  · `:1195` **结构前提**：`flt_title_Army` 在 `Root` 那棵树里、**但不是直接子件** ——
    由**本文件自己的** `FindDeep`（`GetComponentsInChildren<Transform>`，**另一份实现**）+ `_root.Find` 作证；
  · `:1200` `UiLabelText("flt_title_Army")` == **`Army`**（原版行小标题的文案）；
  · `:1203` `UiLabelText("flt_input_t")` == **`Search`**（原版 `Placeholder` 原文；与上一行 `UiFilterInputText`
    量的是**同一颗**节点，这条只是改走**名字**）；
  · `:1207` **负例**：`UiLabelText("flt_title_zzz")` == `null`（钉住这一步不是恒真）。
- 🔴 **改坏法**：把 `DeckRuntime.UiLabelText` 里 `FindDeep` 那一步**删掉**（退回只走 `Root.Find`）
  ⇒ `flt_title_Army` / `flt_input_t` 两条都读成 **`null`** ⇒ **`:1200` 与 `:1203` 两条红**
  （结构前提 `:1195` 仍绿 —— 它量的是树，不是这条读数）。

### 件3 · ArmyRowH = 550（**实现本来就对**）

- **先 grep 现读**：`grep -rn "ArmyRowH" Assets/` ⇒ 实现只有一处 =
  `Core/FilterPanelModel.cs:57-61`
  `rows = ceil(n/3); return ArmyContentTop(50) + rows × ArmyCell(100);`
  ⇒ `ArmyRowH(13) = 50 + 5×100 = **550**`。**这是 2026-09-28 就修好的**（本轮**实现零改动**）。
  判据 = `资料/卡组编辑界面_查证_0920.md` §四（原文：「Army 行高改成『内容高』（`50 + 行数×100`）」，
  同一节记着 150 的老行为 = 溢出 350px 压在 Rarity/Cost 上、而且「点 Rarity 实际改的是阵营」）。
- ⚠️ **但断言一条都没有**：`grep -n "ArmyRowH" Editor/DeckScene.cs` 命中的**全是注释**（`:2247` 那条等）
  ⇒ 把 `ArmyRowH` 改回写死 150，**DeckScene 594 条全绿** = 这条账「实现对了、没人守」。
- **新增断言在哪**：`Editor/DeckScene.cs:1127-1168`（插在既有 `$owned` / `$upgradable`
  两条 rect 断言**之后**、搜索框那三件之前；此时抽屉开着且**滚动位 = 0**）。3 条 `Check`：
  · `:1145` 前提 `state.Factions().Count == 13`（13 格 ÷ 3 = 5 行才谈得上 550）；
  · `:1153` 最后一格 Army 的渲染矩形（面板内 x = 14+列×107、y = 179.02+50+行×100、格 100×100）；
  · `:1160` **`$rar:common`（Rarity 首格）的渲染矩形 = 中心 y `156 + 179.02 + 550 + 65 + 50 = 1000.02`**
    —— **这一条就是「Army 行 = 550（不是 150）」的判别点**；
  · `:1165` **几何判别**（不靠 550 这个数本身）：最后一格 Army 的**底**到 Rarity 格的**顶**，
    期望间隙 = Rarity 行 `Content` 顶内缩 **65**；写死 150 时是 **−335**（压在一起）。
  ⚠️ 量的是**建出来那两行格子的渲染矩形**（`UiFilterCell`），⛔ **不是** `ArmyRowH(13)` 的返回值
  —— 读被测函数自己 = 自证。
- 🔴 **改坏法**：把 `FilterPanelModel.ArmyRowH` 改成写死 `150f` ⇒ `$rar:common` 中心读成 **600.02**
  （差 400px，容差 0.6）⇒ `:1160` 红；同时间隙读成 **−335** ⇒ `:1165` 也红。**两条一起红**。

### 件4 · A224①（次要）—— Cost / Type 两族的 `WrappingMode` 真读数

- **结论：已做完，无需改动。** A92 那轮（2026-10-07，提交 `39b82f0`）已经照裁定落了「滚到底读、读完滚回 0」
  的自检口 + 两条真读数断言：
  · 自检口 = `Deck/DeckRuntime.cs:2237-2243` `UiScrollFilters(float dy)`（**自检口，不是生产路径**）；
  · 断言 = `Editor/DeckScene.cs:2298-2311`：
    `Check(typeLb.WrappingMode, 0, "★ 类型族 = 原版 `折行=0`")`（`:2303`）与
    `Check(costLb.WrappingMode, 1, "★ 费用桶 = 原版 `折行=1`")`（`:2307`），
    标签取自 `_rt.UiFilterCellLabel("$type:hero" / "$cost:1")`。
  · `Label.WrappingMode` = `(int)_tmp.textWrappingMode`（`Battle/Label.cs:257-260`）⇒ 读的是 **TMP 的真字段**；
    期望值 0 / 1 来自原版 dump，**不是**我们自己的常量。
- ⚠️ **本会话没有读数**（我没跑 Unity，签发的简报也不许跑）⇒ 「实设成几档」的**数**要等同步点那次
  `DeckScene.Run` 才有；但按上一批的 **594/594 全绿**（简报给的），这两条断言是**执行过且过的**、
  并且两条「（前提）…标签在且是真 TMP」也过了 ⇒ 读数 = **0 / 1**，与期望相符。
- 🔴 若同步点复跑发现这两条红：按简报要求**报回来，⛔ 不改实现去迁就断言**。

---

## 二、没做成的 / 没查清的（写清差什么，⛔ 别猜）

1. **本会话没有 Unity 读数** —— 三件新断言**一条都没执行过**（简报不许跑 Unity，同一工程串行）。
   落地的是「判据 + 代码 + 断言 + 改坏法」，**绿不绿要看同步点那次 `DeckScene.Run`**。
   我对自己那几条的把握点都写成了可闭式验证的形式（见件1 的 `FontSizeToPx(fontSizeMin) ≡ 26` 那段），
   但**没有实测**。
2. **件3 的断言依赖「本窗 `Factions()` 恒 13」** —— 这条由既有断言
   `Check(_rt.UiFilterCellCount, 31, …)` 钉住；我那条 `Check(facN, 13, …)` 是**前提**，
   它若红说明阵营表变了、后面两条失去判别力（文案里写明了）。
3. **`$fac:<最后一格>` 的格子存在性**没单独钉 —— 我用了 `if (…) else Check(true, false, …)`，
   量不到会**红**（不静默）。Army 行 5 行的格子都在可见带内（最下一行 Bg 顶 = 785.02 < 1080.1），
   按 `RefreshFilterCells` 的裁切判据应当建得出来。
4. **件4 的 `Check` 期望值身份**：`WrappingMode` 期望 0 / 1 来自原版 dump；但我**没有**回头复核那份
   dump 的原始行（A92 已复核过、判据一个字没改）—— 本轮**没重查**，如实记。

---

## 三、顺手发现的（⛔ 我一个都没改）

1. **`UiLabelText` 是全工程最后一个「找节点」的裸口子**：同族另外四条 + `UiQuadActive` / `UiNodeRect` /
   `UiQuadRect` 现在**全都有 `FindDeep` 了**（我核过这 7 个）。⇒ 这一类「容器下找不到」的静默**清零**。
2. **`ArmyRowH` 这条账「实现对了、没人守」** —— 值得顺手查一遍：还有多少 2026-09 那批修好的
   真缺陷是**只改了实现、没配断言**的（本件就是其中一条，`grep` 出来的 `DeckScene` 命中全是注释）。
   ⛔ 我没去普查，只报这一条。
3. **`Shell/CollectionWindow.cs:1723` 那个 `BuildCosmetics` 调用点**：本轮**故意没传**新形参
   （= 收藏窗的 18）。⚠️ 收藏窗那颗的下限**今天没有任何断言**（`grep -n "FontSizeMin" Editor/CollectionScene.cs`
   零命中）⇒ 谁把缺省形参改掉，收藏窗会**静默被改歪而没有一条红**。这是**另账**，我按简报没动那个文件。
4. `Editor/CollectionScene.cs:2971` 的注释把 `BuildCosmetics` 称作「与卡组编辑那扇窗**同一份模型**」
   —— 模型确实同一份，但**从本轮起那颗 `'Owned only'` 的两窗取值不同（26 vs 18）**；
   那条注释读起来像是「两窗逐字相同」，**将来容易误导**（我在 `FilterPanelModel` 的常量注释里写清了）。
   ⛔ 没改（那个文件不归我）。

---

## 四、跑过什么

- **秒级类型检查**：`TMPDIR=/tmp/wf_wA bash d:/4/Unity/工具/typecheck.sh`
  ⇒ **`运行时错误数: 0` · `编辑器错误数: 0`**（跑了 1 次，在**三处改动全部落盘之后**）。
  ⚠️ 注意：同一时刻 `git diff --numstat` 显示还有**别的写手**在动
  `Shell/{MenuDraw,MenuScroll,SettingsWindow,PracticeModePopup}.cs` 与
  `Editor/{CollectionScene,MainMenuScene,RewardsScene,SettingsScene,ShellScene,ShopScene}.cs`
  —— 我这一轮**没有**出现「假错」（0/0），所以无需区分归属。
- **`git diff --numstat`**（只列我碰的三个 + 行尾复核）：

  | 文件 | +/− | 行尾（`b.count(b'\r\n')` / `b.count(b'\n')`） |
  |---|---|---|
  | `Core/FilterPanelModel.cs` | **16 / 2** | CRLF **0** · LF **552** ⇒ **纯 LF**（与 HEAD 一致） |
  | `Deck/DeckRuntime.cs` | **23 / 3** | **3879 / 3879** ⇒ **纯 CRLF**（3859 + 20 新增，一致） |
  | `Editor/DeckScene.cs` | **108 / 0** | **2540 / 2540** ⇒ **纯 CRLF**（2432 + 108 新增，一致） |

  ⇒ **没有一行行尾被翻**（判据 = CRLF 计数 == LF 计数，且增量 == 新增行数）。
  全部用 Edit 工具改的，⛔ 没用 `sed -i` / python 文本写。
- **没跑**：Unity / `DeckScene.Run` / `_run_8_checks.sh`（简报不许；同一工程同时只能一个实例，调度台在同步点统一跑）。
- **没动**：`git`（无 add/commit/checkout/stash/reset）· `项目任务.md` · `CLAUDE.md` ·
  `资料/普查产出_1009/A表核对_块*.md`（另外三路只读代理的产出）· 白名单外的任何 `.cs`。
