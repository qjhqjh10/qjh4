# B141 · W129 · 4-01 字体面：「站点 → 面」表 + 卡组/收藏两窗接线（执行写手 `B141-W53`）

> 2026-11-04 · 批次 `B141` · 会话 第一百二十八 · 波次 `W129` · 判据包 **`W23`**（`判据/B141_W129_对账_UI动画.md`）+ `报告/B138_审计汇总_问题清单.md` §4 的 **4-01**。
> 范围 = **`Core/TmpFont.cs` 定表** ＋ **`Deck/DeckRuntime.cs` / `Shell/CollectionWindow.cs` 逐站点接线** ＋ 新建断言。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改 `d:/2` · ⛔ 未改账主表 / 活状态表 / 所有权表 / `CLAUDE.md`。

---

## 一 · 结论（一句话）

**「站点 → 面」表已建（8 行，每行带原版资产名 + 判据计数）；卡组编辑窗 27 个文本站点、收藏窗 19 个文本站点全部接到
`Face.LatinCardBody`（= 原版 `Pragati-Regular SDF`）**；**缺省面一个字没改**（`MenuDraw.Text` / `Label` / `TmpFont.NewText`
仍是 `Face.Cjk`），汉字照旧由 `Label.ActualFace` 兜底回 CJK。`工具/typecheck.sh` **0 / 0**（运行时 0 · 编辑器 0）。

| 件 | 改前 | 改后 | 证据 |
|---|---|---|---|
| `Core/TmpFont.cs` | 636 行 · 无站点表 | **744 行（+108）** · `SiteFaces` 8 行 + `FaceForSite` / `TryFaceForSite` | 表在 `:284`，查口在 `:305`/`:315` |
| `Deck/DeckRuntime.cs` | `Txt`/`TxtPx` 无 `face` 形参 · **0 个站点传面** | `Txt`/`TxtPx` 加 `face = Cjk` 缺省参 · **27 个站点**传 `LatinCardBody` | **行数零变化**（10,750 → 10,750） |
| `Shell/CollectionWindow.cs` | 19 个 `Text(` 站点**全走缺省 Cjk**（另 2 个本来就传了拉丁面） | **19 个站点**各加 `face: TmpFont.Face.LatinCardBody` | **行数零变化**（7,251 → 7,251） |
| `Editor/B141_W53_Assert.cs` | —— | 新建 **287 行 · 47 条断言**（表段 25 · 卡组窗 16 · 收藏窗 6） | 见 §五 |

---

## 二 · 「站点 → 面」表（`Core/TmpFont.cs` 的 `SiteFaces`）

判据一律是**原版 prefab 里那颗 TMP 的 `m_fontAsset` 实读**（不是按名字猜）。guid → 资产名 = `ExportedProject/Assets/*-Regular SDF.asset.meta`：

| guid | 资产 | 原版用在哪 |
|---|---|---|
| `2548b731a71f84745a1b13c211c923ca` | `Pragati-Regular SDF` | **菜单/弹窗正文与数值主字体** |
| `b80ad157b2e63114e94d0e228946d481` | `Asar-Regular SDF` | **标题族 + 卡名** |
| `4996a91cc356d51408135f9bb89218fa` | `RobotoCondensed-Regular SDF` | 1314 份 prefab 里 **0 颗** |
| `33ea99a992ff8a84b81de55ac13ed02c` | `NotoSerifCJK-Regular SDF` | prefab 里 **0 颗**，只作运行期 fallback |

| # | 站点键 | 原版资产 | 我们的面 | 判据（计数） |
|---|---|---|---|---|
| 1 | `menu.body` | **Pragati-Regular SDF** | `LatinCardBody` | `Deck Editing Menu` **64/64** · `Collection Menu Variant` **57/57** · `Main Menu Settings Window` 102/102 · `BasicToolTip` 1/1 · `Toast` 1/1（2026-11-04 现读）；`B131_A6` 合计 4,431 颗 |
| 2 | `window.title` | **Asar-Regular SDF** | `LatinUi` | `Daily Streak Popup` · `EnergySinglePlayerOnlyEventWindow` 的 `Header With Back Button/Header Background/Window Title`（现读） |
| 3 | `section.title` | **Asar-Regular SDF** | `LatinUi` | `Factions Title` · `Current Streak(+Value)` · `Alliance Name`（**3 例现读，未穷举**） |
| 4 | `card.name` | **Asar-Regular SDF** | `LatinUi` | `2DCard/…/Name and description/*/NameText*`（`Catalog Item Shop Container` · `Draft Mode…` 现读） |
| 5 | `card.body` | **Pragati-Regular SDF** | `LatinCardBody` | 同两 prefab 的 `DescText*` / `DescTactic*` / `UnitType` / `RaceText` / `CostText`（现读） |
| 6 | `battle.hud.number` | **Pragati-Regular SDF** | `LatinCardBody` | `B131_A6` #32（`ManaText` fs40 Pragati） |
| 7 | `battle.hud.title` | **Asar-Regular SDF** | `LatinUi` | `B131_A6` #29/#30/#31（`OVERTIME!` / `END TURN` / `Title Text`） |
| 8 | `narrow` | **RobotoCondensed-Regular SDF** | `LatinNarrow` | 1314 份 prefab **0 颗** ⇒ 今天没有站点用它（键保留 = 「查得到、别猜」） |

> 🔴 **口径三条**（写进 `Core/TmpFont.cs` 的注释，⛔ 别推翻）：
> ① **缺省面仍是 `Face.Cjk`** —— 本表不改任何缺省值，只给**显式接线**的站点提供唯一出处；
> ② **中文仍由 `Label.ActualFace` 按文本兜底回 CJK**（三份拉丁资产没有汉字字形）；
> ③ 表里没有的站点 = **查不到**，`FaceForSite` 回 `Cjk`（⛔ 不猜一个面给它）。

**本波只用到第 1 行**（两窗原版整窗都是 Pragati）—— 表中其余行给后续各窗（W1/W6 等）按包 `W23` 直接取用。

---

## 三 · 逐窗接线

### 3·1 `Deck/DeckRuntime.cs`（原版 `Deck Editing Menu.prefab` **64/64 颗 TMP = Pragati**）

**helper 改动（零行数变化）**：

```csharp
Label Txt(…, Transform parent = null, bool relativeToParent = false, TmpFont.Face face = TmpFont.Face.Cjk)
    if (l != null) { l.SetRenderQueue(queue); if (face != TmpFont.Face.Cjk) l.SetFace(face); }
Label TxtPx(…, int queue, TmpFont.Face face = TmpFont.Face.Cjk)
    var l = Txt(key, s, x, y, w, h, 1, c, queue, face: face);
```

> ⚠️ **次序**：`SetFace` 在 `Txt` 里、`SetGlyphHeight` 在 `TxtPx` 里 ⇒ 换面**先于**按新面折算字号（反了会按旧面的 em 折）。

**27 个站点（全部 `menu.body` → `LatinCardBody`）**，行号 = **改后仍在原行**（零行数变化）：

| 站点（`Txt`/`TxtPx` 的 key） | 行 | 原版节点 |
|---|---|---|
| `hdr_back_t` | 1569 | `Header/Close/Button Text` |
| `hdr_fltlbl` / `hdr_clear_t` | 1629 / 1655 | `Header/Filters/Label` · `…/Button Text` |
| `hdr_wct0..3` | 1710 | `Header/WIldcard Counter/Counters/*/Counter` |
| `tab_tx0..2` | 1879 | `Sidebar/Window Options/Buttons/*/Label/Text` |
| `name_t` / `name_h` | 1946 / 1953 | `Sidebar/Window Options/Deck Name/Text Area/{Text,Placeholder}` |
| `row_n*` / `row_cnt*` / `row_x*` | 2078 / 2104 / 2170 | `Deck List drawer/…/Card Name` · `Count` |
| `empty_warn_l` | 2195 | `Deck List drawer/Empty Warning` |
| `curve_l*` / `curve_n*` | 2395 / 2404 | `Deck Information cost drawer/…/{Card Cost,Cards in deck}` |
| `typecnt_strat` / `typecnt_troops` | 2425 / 2426 | `Deck Counter Drawer/Text (TMP)` |
| `foot_done_t` / `foot_cnt` | 2487 / 2505 | `Footer/Done/Button Text` · `Sidebar/Footer/Counter` |
| `cosmoflt_title` / `flt_title_*` | 2568 / 2908 | `Cosmetic FIlter/Filters/Army Filter/Title` · `Card Filters/…/Title` |
| `flt_input_t` | 2883 | `Card Filters/…/Name FIlter/Input Field/Text Area/Text` |
| `poolcnt_*` | 3310 | 卡池计数（`Collection Card/Content/Counter`） |
| `cosm_empty_t` | 3462 | `Cosmetic Display/Scroll View/Empty Collection Warning` |
| `card_ghost_n` / `card_ghost_c` | 3939 / 3950 | 拖拽残影的卡名 / 费用 |
| `poolbantxt_*` | 9550 | `MenuDeck/Banned` 徽标 |
| `row_sft*` | 9886 | `Slot Free Text` |
| `hdr_chip_lab_*` | 10333 | 页头筛选 chip 文案 |

> 另 3 处原就有的 `SetFace(LatinCardBody)`（`:2124` 卡组行计数、`:7629`/`:7880` 两个筛选格）**没动**，与本次同面。

### 3·2 `Shell/CollectionWindow.cs`（原版 `Collection Menu Variant.prefab` **57/57 颗 TMP = Pragati**）

**19 个站点各加 `face: TmpFont.Face.LatinCardBody`**（走基类 `MainMenuSubmenuWindow.Text` 的既有 `face` 形参，**没新造 API**）：

| 行 | 站点 |
|---|---|
| 378 | 卡格计数 `Text (TMP)` |
| 1071 | Cards 页万能卡计数 |
| 1195 / 1880 / 2385 / 4237 | 四类 `Empty … Warning` |
| 1836 | Cosmetics 页页头 `Header Label` |
| 2537 | Styles 页风格名 |
| 3152 | 通用标签漏斗（`BuildLabel`） |
| 3333 | 筛选标题（`Title <x>`） |
| 3542 / 3609 | `Filters` · `Clear filters` |
| 3931 | `Back` 按钮字 |
| 4097 / 4112 | `Create` / `Import` 按钮字 |
| 4751 | Deck 页搜索框 `Input Text`（4-07 的 fs35 同一行） |
| 4788 | Deck 筛选 `Army` 标题 |
| 6654 | `Banned Text` |
| 7071 | `Title Army` |

> 另 2 处（`:4381` `Title Header` · `:4487` `Event Title`）**本来就传了** `LatinCardBody` ⇒ 本窗 21/21 个站点同面。

---

## 四 · 行数 / 行尾 / 账

| 文件 | HEAD 行数 | 现读 | 变化 | 行尾 |
|---|---|---|---|---|
| `Core/TmpFont.cs` | 636 | **744** | **+108**（纯新增块） | LF（改前也是 LF） |
| `Deck/DeckRuntime.cs` | 10,750 | **10,750** | **0** | **CRLF 10,750/10,750 · 裸 CR 0** |
| `Shell/CollectionWindow.cs` | 7,251 | **7,251** | **0** | **LF 7,251/7,251 · 裸 CR 0** |
| `Editor/B141_W53_Assert.cs` | —— | **287** | 新建 | LF |

- 🔴 **两窗为什么必须零行数**：`账/*.tsv` 按 `文件:行号` 引用 —— `DeckRuntime.cs` **160 处**（其中 **35 处 ≥ 8580**，
  正是我改 helper 的位置）、`CollectionWindow.cs` **1,414 处** ⇒ 插一行就是一片漂移。
  实测：三处 helper 改动全部**就地改写原行**（`Txt` 签名 + `if (l != null) …` 那一句、`TxtPx` 签名与其转发行），
  27 / 19 个站点全部把 `face:` 追加在**关闭括号之前**（不是追加在行尾）⇒ 既零行数、也**不会落进行尾 `//` 注释**。
- ✅ **验证「没落进注释」**：逐条比对改前/改后，**46 个站点里 `face:` 在 `//` 之后的 = 0**。
- ✅ **改后行号 = 改前行号**（零行数）⇒ 账不需要 `--fix`。

---

## 五 · 断言（`Editor/B141_W53_Assert.cs`，47 条）

**接线**（宿主在白名单外，本批没接；各加一行即可）：

```csharp
// Editor/DeckScene.cs
B141_W53_Assert.Run(_rt); _sink.Pass += B141_W53_Assert.Pass; _sink.Fail += B141_W53_Assert.Fail;
// Editor/CollectionScene.cs
B141_W53_Assert.Run(win); _sink.Pass += B141_W53_Assert.Pass; _sink.Fail += B141_W53_Assert.Fail;
// 只要表那一段（不建窗）：B141_W53_Assert.Run();
```

| 段 | 条 | 断什么 | 灭自证 |
|---|---|---|---|
| **A 表形** | 6 | 表长 8 · 站点键唯一 · 每行都有原版资产名与判据出处 · `Face` 落在枚举里 | 删行 / 改键 ⇒ 红 |
| **B 绑定** | 4 | **每行的面解析出来的资产名 = 它记的那份原版资产**（Pragati / Asar / RobotoCondensed 三份都要取得到） | 换枚举序 / 换 `FaceResource` 绑定 / 抄错行 ⇒ 红 |
| **C 缺省与兜底** | 8 | 未知站点 → `Cjk` 且 `TryFaceForSite` 回 false · `Label` 出厂面仍是 `Cjk` · 指派拉丁面后**含汉字文本 `ActualFace` 回 `Cjk`** · **纯拉丁文本仍是拉丁面**（C7 证明 C6 不是「恒 CJK」） | 把缺省改拉丁 ⇒ C4 红；去掉兜底 ⇒ C6 红；把 `ActualFace` 写死 CJK ⇒ C7 红 |
| **D 灭自证** | 7 | `menu.body=Pragati` · `window.title=Asar` · **`card.name ≠ card.body`**（同一颗卡上两张不同面）· `menu.body ≠ window.title` · HUD 标题 = Asar · `narrow` = Roboto · **表里两种面各 ≥3 行** | 「整表刷成同一个面」⇒ D3/D4/D7 同时红 |
| **E 卡组窗** | 16 | `hdr_back_t` / `hdr_wct0` / `tab_tx0` / `name_t` / `foot_done_t` / `foot_cnt` 六颗**按名钉**（每颗：前提存在 + 面 = `LatinCardBody`）＋ 整窗扫（排除 `CardView` 子树与共享件 `Deck Name`）**≥12 颗拉丁面且占多数** | 去掉任一站点 `face:` ⇒ 该名那颗红；整片回 Cjk ⇒ 整窗那三条红 |
| **F 收藏窗** | 6 | `Title Header` 按名钉 + 整窗扫 **≥6 颗拉丁面且占多数** | 同上 |

> ⚠️ **E7/F2 为什么是「计数式」不是「其它一律为 0」**：窗里还有**别的写手**建的件（见 §六 的 1、2），
> 严格式会把别人的缺口算到本件头上。逐名钉的六颗 + `Title Header` 已经把本件每个站点锁死。

---

## 六 · ❓ 查不到 / 仍开着（**白名单外**，如实登记，⛔ 未动）

1. **左栏页签 `Tab Buttons/*/TabButtonLabel` 仍是 Cjk（4 个窗共用一条）** —— 建它的是
   `Shell/MainMenuSubmenuWindow.cs:777` 的 `Text(...)`（**没传 `face:`**）。原版 `Collection Menu Variant` 的
   `Deck/Cards/Cosmetics/Styles` 四颗**都是 Pragati**（本报告 §二 的 57/57 里就含它们）。⇒ 建议给那一行补
   `face: TmpFont.Face.LatinCardBody`（奖励窗/商店/社交窗同源，一处修四窗）。**该文件归 `B141-W5`。**
2. **卡组格的名字 `Deck Name` 仍是 Cjk** —— 建它的是 `Shell/MenuDraw.cs` 的 `DeckCell`（`Text(cell, nameR, info.Name, …, "Deck Name", …)`）。
   原版那族（`Collection Deck` / `Deck Drawer` 等 6 份 prefab）**是 `Asar-Regular SDF`**（⚠️ 2026-11-04 `B141-W64` 现读订正：原写 Pragati 错；同名节点「卡组编辑侧栏输入框」才是 Pragati）—— 证据：`Collection Deck.prefab` `Deck Name` 的 `m_fontAsset` pathID `-8244042478085975641` = `R0_资源覆盖账.tsv:83490` `Asar-Regular SDF`，guid `b80ad157…` 同。**该文件归 `B141-W14`（已交回）。**
3. **卡片本身**（`Core/CardView.cs` 的卡面文字）走的是另一条链（`TmpFont.NewText(face)`），本批**没碰**；
   4-01 在卡面上的那一半按 `card.name` / `card.body` 两行判据另派。
4. **本批未跑 Unity** ⇒ 47 条断言**一条都还没执行过**；「两窗真实渲出来是不是 Pragati」只有真机/宿主跑过才算数。
   `DeckRuntime` / `CollectionWindow` 的构建期文本（`Txt`/`Text`）在**未激活**对象上照样能读到 `Label.Face` ⇒
   宿主一接即可验；**画面像不像**仍要进游戏看。
5. **`section.title` 那行只核了 3 例**（`Factions Title` / `Current Streak` / `Alliance Name`），
   **没有穷举** —— 它不在本波两个窗里，写进表是为了让后面的人「有处可查」；真要用时按现读再核一遍（铁律 19·4）。

---

## 七 · 过程说明（所有权表已过期，请调度台更新）

- 开工时 `资料/完全复刻/文件所有权_当前.md`（19:15 那一版）写着
  `B141-W29 → Deck/DeckRuntime.cs` · `B141-W32 → Shell/CollectionWindow.cs`，**状态仍是「🔄 在飞」**。
- 我在动手前**核过这两路已经收工**：① 两份文件 mtime 停在 19:28 / 19:34，此后 20 分钟无写入；
  ② 它们的报告 `报告/B141_W129_DeckDrag.md`（19:40）与 `B141_W129_Collection.md`（19:33）都已落盘且含验收结论；
  ③ **它们的断言已接进宿主**（`Editor/DeckScene.cs:5557` 的 `B141_W29_Assert.Run(_rt)`；`Editor/CollectionScene.cs` 的 `B141W32_Assert.Run()`）
  ⇒ 按本仓「一文件一写手」的判法，这两份文件**当时已无人写**，我才按简报授权写入。
- **建议调度台把这两行的状态从「在飞」改成「✅ 交回」并把本件（`B141-W53`）登记为这两份文件的下一个写手**。

---

## 八 · 判据来源（全部可复查）

- `资料/完全复刻/判据/B141_W129_对账_UI动画.md` —— `W23` 包那一行 + §4 的 `4-01` 行（含「原版 Pragati 4,431 / Asar 548 / Noto 0」）。
- `资料/完全复刻/报告/B138_审计汇总_问题清单.md` §4 `4-01`。
- `报告/B131_审计_UI版面与字体.md` + `报告/_parts/B131_A6_字体与排版.md`（#34/#35 与「字体资产用途分配」表）。
- **原版 prefab 实读**（2026-11-04）：`d:/2/tools/assetripper_out/ExportedProject/Assets/` 下
  `Deck Editing Menu.prefab`（64 颗 TMP）· `Collection Menu Variant.prefab`（57 颗）· `Main Menu Settings Window.prefab`（102 颗）·
  `BasicToolTip.prefab` · `GameObject/Toast.prefab` · `Catalog Item Shop Container.prefab`（32 Pragati + 7 Asar）·
  `Daily Streak Popup.prefab`（7 + 3）· `Draft Mode Timed Mode Window.prefab` · `EnergySinglePlayerOnlyEventWindow.prefab`；
  guid → 资产名 = 同目录 `*-Regular SDF.asset.meta`。
- ⚠️ 解析脚本（只读、临时）在 `d:/4/_tmp_view/b141_w53/font_sites.py`，逐 prefab 输出「guid | 节点全路径」。
