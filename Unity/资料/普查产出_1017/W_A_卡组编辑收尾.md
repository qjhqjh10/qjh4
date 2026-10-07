# W_A · 卡组编辑收尾（D2 / D3 / D4 / D46 第二处）—— 写手代理报告（2026-10-17）

## ① 结论：四处都走 `Loc.T`，键名 = **原版 prefab 上那颗 `Localize` 的 `mTerm`**

| # | 处 | 原版节点（`Deck Editing Menu` 全树） | **mTerm（键）** | 原版英文（那颗 TMP 的 `m_text`） | 原版中文（**实拍**） |
|---|---|---|---|---|---|
| D2 | 顶栏过滤器钮 | `…/Content Area/Header/Filters/Label` | `MenuDeck/Filters/Filters` | `Filters`（fs40） | **过滤器** |
| D3·1 | 左栏页签 1 | `…/Sidebar/Window Options/Buttons/Cards/Label/Text` | `MenuShop/ShopItemType/Cards` | `Cards`（fs34） | **张牌** |
| D3·2 | 左栏页签 2 | 同上 `Info/Label/Text` | `MenuDeck/HUD/DeckDescription/DeckInfo` | `Deck info`（fs31.5） | **卡组信息** |
| D3·3 | 左栏页签 3 | 同上 `Cosmetics/Label/Text` | `MenuShop/ShopItemType/Cosmetics` | `Cosmetics`（fs28.15） | **美容品** |
| D4 | 左栏底部钮 | `…/Sidebar/Footer/Done/Button Text` | `MenuDeck/MenuButtons/Done` | `Done`（fs40） | **完成** |

**判据出处（逐条现读，不是转抄施工单）**：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`
的 `GameObject/Deck Editing Menu.json`（根 RT `2633458108729585444`）→ 沿 `m_Children` 走 247 个节点，
对每颗挂 `Localize` 的 MB 读 `mTerm`、对每颗 TMP 读 `m_text` / `m_fontSize` / `m_HorizontalAlignment` /
`m_VerticalAlignment`（走 `工具/menu_rect.py` 的 `Bundle` 索引；临时脚本在 `/tmp`，没进仓）。
⚠️ **三条与施工单不同的细节（就地订正）**：
- 🔴 **键名分属两个前缀**：三颗页签里 `Cards`/`Cosmetics` 用的是**商城那一族** `MenuShop/ShopItemType/*`，
  只有 `Info` 在 `MenuDeck/HUD/…` —— **原版如此**（已写进 `Loc.cs` 注释，⛔ 别「看着不齐」去统一）。
- `Footer/Done/Button Text` 那颗上挂着**两颗** `Localize`（`MenuDeck/MenuButtons/Done` +
  登录页共用的 `MenuLogin/Login/DoneButton`）⇒ 取**第一颗**。
- 中文那五个词**逐字复核过实拍**（`资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`，1581×887，
  比例尺 0.8234/0.8213）：裁三块放大 3~4 倍亲读 —— 页头金框钮右侧 `过滤器` · 左栏名牌上
  `张牌/卡组信息/美容品` · 左下钮内 `完成`。⇒ 这是**原版中文**（不是我们译的），`Loc.cs` 里逐条标了「★实拍」。

**D46 第二处（导入窗对齐）**：原版 `Import Deck Popup/Window/Input Field/Text Area/{Placeholder,Text}`
= **HA `1`(Left) · VA `256`(Top)**（两颗都是；rect 620,377→1300,505.06）—— 与我们传的 `Center` **相反**。
✅ 已按 `Deck/DeckRuntime.cs` 那份**已经改对的同款**落地。

## ② 改动清单

| 文件 | 改了什么 |
|---|---|
| `Core/Loc.cs` | **只加词条**：新增「卡组编辑窗」一组 **5 条**（上表那五对），键名照原版 `mTerm`；注释写清节点出处 / 英文 = TMP 原文 / 中文 = 实拍（与表内其余「我们译的」区分） |
| `Deck/DeckRuntime.cs` | **只动文案**：`BuildHeader` 的 `hdr_fltlbl` · `BuildSidebar` 的 `tabTx[3]` · `BuildFooter` 的 `foot_done_t` 三处字面量 → `Loc.T("…")`。⛔ 版面常量 / 队列号 / 删件 / 顶栏（`Shell/TopBar.cs`）一个字没碰 |
| `Shell/ImportDeckPopup.cs` | `RefreshInputText()` 末尾补 `lb.SetAlignLeft(); MenuDraw.SetVAlign(lb, VAlign.Top, new PxRect(TxtL,TxtT,TxtR,TxtB));`（顺序死：先 HA 后 VA）。**订正两处错注释**：函数 doc 那句「原版 `Text`/`Placeholder` 都是 hAlign=Center」+ 文件头那三条对齐记录 |
| `Editor/DeckScene.cs` | **只加断言**：G1 节新增 ⑭（见下）。其余一字未动 |

**新增断言（G1 节 ⑭，16 条）**：
- **① 词条层**（不建窗 ⇒ 与运行语言无关）：中文档 5 条 = 原版实拍那五个词；英文档 5 条 = 那几颗 TMP 的原文。
- **② 实况层**：**先切中文、再临时建一扇探针窗**（`DeckRuntime.Build`，形状同 `Run()` 里那三处 `*Smoke`），
  量 `hdr_fltlbl` / `tab_tx0..2` / `foot_done_t` 五颗 `Label` 印的字 = 原版实拍那五个词，用完 `DestroyImmediate`。
  全程 `Loc.PersistOverride = true`（⛔ 不写 `PlayerPrefs["Language"]`），`finally` 里还原语言与注入点。
- 🧨 **判别式**：把 `DeckRuntime` 那三处 `Loc.T(...)` 换回写死英文 ⇒ ②那五条**在中文档下必红**；
  把词条中文列改回英文 ⇒ ①那五条红。⛔ 期望值全取**原版字面量**，一处也不从 `DeckRuntime`/`Loc` 读回来。

**顺手发现（同一次实读查出 · ⚠️ 本篇【没改】 · 请调度台分流开账）**：
- 🔴 `Import Deck Popup` 的 `Main Search message`（"Paste your deck"）与 `Error msg` 两颗 TMP 在 prefab 里
  都是 **HA `2`(Center)**（VA 512），而 `Shell/ImportDeckPopup.cs` 那两处 `Txt(…, Align.Right, …)` 传的是右对齐
  ⇒ **我们右对齐、原版居中**（`A1_外壳与弹窗.md:118` 那句 `hAlign=Right` 是没实读的转抄）。
  ⛔ 没顺手改的原因：**改它要另配断言**，而本窗的断言在 `Editor/ShellScene.cs`（**不在本批白名单**）。
  已在 `ImportDeckPopup.cs` 注释里就地记下（实读值 + 「待开账」）。判据校准：本包 TMP 的 `(HA,VA)` 分布
  `(2,512)` 1627 条最多、`(1,512)` 446 条（`Legendary Wildcard`/`Select your avatar` 这类确实该左对齐）
  ⇒ **1=Left · 2=Center · 4=Right**，与工程既有 `SetAlignLeft()` → HA=1 口径一致。
- ⚠️ 导入窗占位符的**词条键**是 `MenuDeck/HUD/EnterText`（prefab 实读），而**两处仍写死** `Enter text...`
  （`Shell/ImportDeckPopup.cs` 与 `Deck/DeckRuntime.cs` 的 `imp_input`）—— **不在本批四件里、没动**。

## ③ 没查清 / 复跑时需盯一眼的

1. 🔴 **D3 换成中文之后那三颗页签的字号会由 `m_enableAutoSizing` 重算**（原版中文客户端同理）。
   判据上应当仍成立（`卡组信息` 4 个汉字会缩到 ~24.7px 贴住 98.96 宽、`张牌` 停在 34 上限），
   但 `DeckScene` 里既有的 A51 F3 那两条（`:2292` 渲染 ≤ 框 · `:2314`「字号=34 **或被压到框边界**」）
   **我没跑过**（本次禁止跑 Unity）⇒ **请在那次复跑里盯这三条**。版面参数一个没动（框 98.96×40 · min/max 10/34）。
2. **换语言时窗口不自己刷新**（`Loc.SetLanguage` 明文「本类不发事件，调用方自己重设」）—— 卡组编辑窗今天
   **没有** `RefreshTexts()` ⇒ 开着窗换语言要重开才变。全工程统一口径（两个设置窗各只有自己那份），**没动**。
3. `MenuShop/ShopItemType/Cards` 是**原版商城与页签共用**的一条词条 ⇒ 将来商城用到它，中文一律「张牌」
   （原版同键同值，不是我们挑的）。

## ④ 类型检查（收尾那一次，原样贴）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（中途一次报 `RuleEngine/Core/EffectResolver.cs` 7 条 `CS0841` —— **那是别人当时正在写的文件**、
不在白名单，按简报「等几分钟重跑」处置；重跑即 0/0。）
行尾纪律：`DeckRuntime.cs`/`DeckScene.cs` **CRLF 未翻**（5272/5272 · 4640/4640）· `ImportDeckPopup.cs`/`Loc.cs`
保持 **LF**；`git diff --numstat` 逐次核过。
