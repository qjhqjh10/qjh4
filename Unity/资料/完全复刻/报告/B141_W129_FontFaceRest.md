# B141-W64 · W129 4-01 两处残留字体面（`TabButtonLabel` · `Deck Name`）

> 执行写手 `B141-W64` · 第一百二十八会话 · 2026-11-04
> 白名单内实际改动：`Shell/MainMenuSubmenuWindow.cs`（1 行）· `Shell/MenuDraw.cs`（1 行）· `Editor/B141_W53_Assert.cs`（+12 条断言）。
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没动账主表 · ⛔ 没动 `d:/2`（只读）。

---

## 一 · 一句话结论

两处**都已按原版填上 `face:`**，但 **第二处的原版面不是 Pragati，是 Asar**（施工单与 `报告/B141_W129_FontFace.md` §六·2 写错了）——
我按**现读判据**填了 `Face.LatinUi`（= `Asar-Regular SDF`），证据见 §三。`typecheck` = **0 错 / 0 错**。

| # | 站点 | 我们改前 | 改后 | 原版实读 | 落点 |
|---|---|---|---|---|---|
| 1 | 左栏页签 `Tab Buttons/*/Label/TabButtonLabel`（**一处修 4 窗**） | `Cjk` | **`Face.LatinCardBody`** | `Pragati-Regular SDF` | `Shell/MainMenuSubmenuWindow.cs:780` |
| 2 | 卡组格 `Deck Name`（`MenuDraw.DeckCell`） | `Cjk` | **`Face.LatinUi`** | **`Asar-Regular SDF`** ⚠️ 不是 Pragati | `Shell/MenuDraw.cs:3464` |

---

## 二 · 逐处：原版面 → 改后

### 1 · 左栏页签（`MainMenuSubmenuWindow.BuildTabButton`）

- **改前**：`Text(b, tabLabel.ToUpperInvariant(), …, autoBasePx: spec.AutoBase);` —— **没传 `face:`** ⇒ 走漏斗缺省 `Face.Cjk`（Noto）。
  这四颗由**同一个函数**建，四扇窗（奖励 / 商店 / 社交 / 收藏）**共用** ⇒ **一处修四窗**。
- **改后**：`… autoBasePx: spec.AutoBase, face: TmpFont.Face.LatinCardBody);`
- **行数变化**：代码行 **零变化**（在既有实参列尾追加）；另在紧邻的注释块补了 **1 行** `// ✅ 已复刻（…）` 标记 ⇒ 本文件 **+1 行**（`923 → 924`，`git show HEAD` 对过：改前 LF、改后仍 LF，无行尾翻转）。
  ⚠️ 该文件在 `资料/完全复刻/账/*.tsv` 里 **0 处**按 `文件:行号` 引用（已 `grep` 确认 `MainMenuSubmenuWindow.cs:` = 0 命中）⇒ 这 +1 行**不会**造成账漂移。

### 2 · 卡组格 `Deck Name`（`MenuDraw.DeckCell`）

- **改前**：`var nl = Text(cell, nameR, info.Name, …, autoBasePx: 30f * sy);   // B103_F6：…`
- **改后**：`… autoBasePx: 30f * sy, face: TmpFont.Face.LatinUi);   // B103_F6：…`
- 🔴 **追加位置特意落在该行第一个 `//` 之前**（CLAUDE.md 2026-10-30 那条「同一物理行追加会落进行末注释、`typecheck` 与账闸都看不出来」的坑）——已逐字回读确认 `face: …);` 在 `//` 左边。
- **行数变化**：**0**（整行文本替换，无增删）。
  ⚠️ 该文件下游有账落点（`卡组编辑.tsv:40` → `MenuDraw.cs:3513`；`收藏.tsv:203/204/205/221/312` → `:3614-3635/3684`）⇒ **行数零变化**是硬要求，已满足。

---

## 三 · 🔴 判据订正：`Deck Name` 的原版面 = `Asar-Regular SDF`（不是 Pragati）

施工单与 `报告/B141_W129_FontFace.md` §六·2 都写「原版那族（`Collection Deck` / `Deck Drawer` 等 6 份 prefab）**也是 Pragati**」。
**2026-11-04 现读：不成立。** 两个**互相独立**的来源给出同一结论：

**① 原始包（第一优先）—— `d:/2/新解包资源/assets_full/bundle_menus_assets_all`**

- `Collection Deck/content/Deck/Deck Name`（`m_text = 'Deck name'`）的 `m_fontAsset = {m_FileID: 5, m_PathID: **-8244042478085975641**}`
- `Deck Drawer/Content/Collection Deck/content/Deck/Deck Name` · `Ranked Deck Selection/Deck Name` · `Deck Details/Deck Name` —— **同值 -8244042478085975641**
- pathID → 名字：`Unity/资料/完全复刻/资源/R0_资源覆盖账.tsv:83490`
  `bundle_fonts_assets_all.bundle  -8244042478085975641  MonoBehaviour  **Asar-Regular SDF**`

**② 导出工程（旁证）—— `d:/2/tools/assetripper_out/ExportedProject/Assets/Collection Deck.prefab`**

- `Collection Deck/content/Deck/Deck Name` 的 `m_fontAsset` guid = `b80ad157b2e63114e94d0e228946d481`
- 该 guid = `Asar-Regular SDF.asset.meta:2`（同目录 `Pragati-Regular SDF.asset.meta:2` = `2548b731a71f84745a1b13c211c923ca`）

**对照：页签那一处的确是 Pragati（施工单第 1 条是对的）**

- 四窗 `Content Area/Tab Buttons/*/Label/TabButtonLabel` 逐颗 `m_fontAsset` pathID = **`3485036404935369831`**
  （`R0_资源覆盖账.tsv:83554` → **`Pragati-Regular SDF`**）；AssetRipper 那份 guid 全是 `2548b731…` = Pragati。
- 覆盖面：`Rewards Base Submenu Variant` 4 颗 · `Collection Menu Variant` 4 颗 · `Social Submenu Variant` 2 颗 ·
  `Shop Menu Variant` 1 颗（母版 `Pacotes`，其余运行期克隆）—— **11 颗逐颗同值**。

**错因（我的判断，供调度台参考）**：原版里有**两处同名节点**，面不同 ——

| 节点 | 原版面 | 判据 |
|---|---|---|
| `Collection Deck/content/Deck/Deck Name`（**卡组格**，本件） | **Asar-Regular SDF** | pathID `-8244042478085975641` |
| `Deck Editing Menu/…/Sidebar/Window Options/Deck Name/Text Area/Text`（**卡组编辑侧栏输入框**） | **Pragati-Regular SDF** | pathID `3485036404935369831`；同一 prefab 里 `Placeholder` 也是 Pragati |

⇒ 「Deck Name = Pragati」这个说法**对侧栏输入框成立、对卡组格不成立**，多半是把两处并成一处了。
（顺带：本件 `E4` 断的 `name_t` 正是**侧栏那一个**、面 = `LatinCardBody` —— 与上表第 2 行自洽。）

**⛔ 我没改 `报告/B141_W129_FontFace.md`**（白名单外）。请调度台在该文件 §六·2 就地订正（按铁律 5 留更正痕迹），
并同步 `B141_W129_FontFace.md` §六·1 的「该文件归 `B141-W5`」/ §六·2 的「该文件归 `B141-W14`」那两句 ——
两处**本件已做完**（`B141-W64` 持有这两个文件的写权，见 `文件所有权_当前.md` 里 W5 / W14 两行仍是旧的「🔄 在飞」）。

---

## 四 · 断言（+12 条，`Editor/B141_W53_Assert.cs`）

接线方式不变（宿主在白名单外，本批**没接**）：`B141_W53_Assert.Run()` 现在会**多跑一段 G**；
`B141_W53_Assert.Run(win)` 会**多跑 F3..F5**。

| 段 | 条 | 断什么 | 灭自证（改坏哪一处会红） |
|---|---|---|---|
| **F3..F5** 左栏页签 | 3 | F3 前提：扫到 **≥4** 颗页签文案（原版 4 颗 `TabButtonLabel`）· F4 ★ 四颗**指派面全是 `LatinCardBody`** · F5 ★ **实际生效面按语种走**（有汉字 ⇒ `Cjk`、纯拉丁 ⇒ 指派面） | 删掉 `MainMenuSubmenuWindow.cs:780` 的 `face:` ⇒ F4 红；把兜底拆掉 / 写死成一面 ⇒ F5 红 |
| **G0..G8** 卡组格 `Deck Name` | 9 | G0 前提：两种面是不同枚举值 · G1/G2 前提：`DeckCell` 与它里面的 `Deck Name` 建得出来 · **G3 ★ 指派面 = `LatinUi`（Asar）** · **G4 ★ 纯拉丁名 ⇒ `ActualFace` = `LatinUi`** · G5 换中文后指派面不变 · **G6 ★ 中文名 ⇒ `ActualFace` 回 `Cjk`** · G7 前提：对照标签建出来 · **G8 ★ 对照标签（没传 `face:`）仍是出厂 `Cjk`** | 改回 `Cjk` **或照施工单误填 `LatinCardBody`** ⇒ G3 红；把 `MenuDraw.Text` 的**缺省面**改成拉丁面 ⇒ G8 红；兜底被拆 ⇒ G6 红；「`ActualFace` 恒 Cjk」那种假绿 ⇒ G4 红 |

**为什么 G3 单独钉「`LatinUi` 而不是随便一个拉丁面」**：`LatinCardBody` 与 `LatinUi` 都能让「已不是 Cjk」这句话成立，
只断「不是 Cjk」会被**填错面**蒙混过去 —— 这正是本件抓到的那类错。F4 + G3 两条合起来构成
「**页签 Pragati / 卡组格 Asar**」这对**结构上不可能同时满足**的约束（G0 再钉一次两者是不同枚举值）。

**前提写成硬断言**（CLAUDE.md「`if (x != null) Check(...)` = 静默跳过」那条）：G1/G2/G7/F3 全是 `T(...)`，不是 `if`。

---

## 五 · 验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_b141_w64 bash 工具/typecheck.sh` | **运行时 0 · 编辑器 0** |
| 行尾 | 两个 `.cs` 改前改后都是 **LF**（`b.count(b'\r\n')` = 0 · 裸 CR = 0） |
| 行数变化 | `MenuDraw.cs` **0** · `MainMenuSubmenuWindow.cs` **+1**（那 1 行是注释标记，且该文件 0 处账落点） |
| 断言执行 | ⛔ **没跑**（红线：不跑 Unity）⇒ 12 条**一条都还没执行过** |
| `git diff` | 只碰白名单三个文件（`B141_W53_Assert.cs` 仍是未跟踪的新文件，属 W53 建、本件续写） |

---

## 六 · ❓ 仍开着 / 交调度台

1. **`Deck Name` 的判据订正还没写进 `B141_W129_FontFace.md`**（白名单外）—— 见 §三 末段，请调度台就地改掉 §六·2，否则下一个人会照旧说法把 `LatinUi` 改回 `LatinCardBody`。
2. **`文件所有权_当前.md` 里 `B141-W5`（`MainMenuSubmenuWindow.cs`）与 `B141-W14`（`MenuDraw.cs`）仍是「🔄 在飞」** —— 实读 mtime 停在 `18:16` / `18:26`（本件开工前 1.5 小时以上无写入），两份报告 18:38 / 18:52 都已落盘 ⇒ **那两行已过期**，请改成「✅ 交回」并把本件（`B141-W64`）登记为这两个文件的下一个写手。
3. **12 条断言未执行** —— 接宿主的仍是白名单外（`Editor/DeckScene.cs` / `Editor/CollectionScene.cs` / `Editor/ShellScene.cs`），
   且 `Run(CollectionWindow)` 那一段要 `CollectionWindow` 建完之后调；**真机/宿主跑过之前，「屏上是不是 Pragati / Asar」不算验过**。
4. **`SweepOwnLabels` 的阈值没动**（仍是「≥N 且占多数」的计数式）—— 本件只把两处共享件从「排除项」升级成**逐颗钉死**，
   ⛔ 没改成「其它一律为 0」：窗里还有别的共享壳件（`MenuDraw` 建的非本窗件），严格式会把这笔算到 W53 头上。
5. **页签文案在中文档下是汉字** ⇒ 走 `Label.ActualFace` 的 CJK 兜底（**不是** Pragati）——这是**原版渲染模型**（拉丁面 + CJK fallback 两说，
   见 `B109_E21` 的 `CjkFallbackFace`）；本件**没碰**这条开关，`CjkFallbackFace` 仍是出厂 `false` ⇒ 中文页签实际用 Noto，英文档用 Pragati。
   若日后要「中文也用 Pragati + fallback」（原版 `fallbackFontAssetTable` 那条路），那是另一件活，别混进本件。
