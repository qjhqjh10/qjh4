# W · 文档注释线 —— 四笔（`A1019` / `A1021` / `A1024` / `A1037`）

> 线名 = **文档注释线**（⛔ 不改任何行为）。日期口径按工程记录 = **2026-10-18（第六会话）**。
> ⚠️ 本报告是**唯一产出**；`CLAUDE.md` / `项目任务.md`（两张正本）一行没动。

---

## ① 结论（四笔各一句）

| 笔 | 账 | 结论 |
|---|---|---|
| 1 | `A1019` | ✅ **已做** —— 在 `Core/Loc.cs` 那条 `MainMenu/General/OK` 的**注释里加了留痕块**（原来写 X / 实际是 Y / 错因 Z 全给了）；`Loc.cs:1086` 那条 entry 的**两个值一字未动**。 |
| 2 | `A1021` | 🔴 **不需要做 —— 两份订正横幅【早已在 `HEAD` 里】**（`1667ff6`，第四会话落地），两段的 X/Y/Z 与「17 条」「5 条」逐数吻合。**本笔零改动**（详细证据见 §③·2）。 |
| 3 | `A1024` | 🟡 **只落了「记录/注释」那一半** —— 判据的三步里 ① 早已记进判据文件，**②③ 是「重建原版 font asset + 接 fallback」、必须跑 Unity 且会牵动全仓版面** ⇒ 按「本轮⛔不跑 Unity」**没有动行为**，改为把**结论 + 未做状态**如实写进 `Core/TmpFont.cs` 的**文件头注释**（该文件正是判据点名的落点）。**②③ 请调度台另派**（见 §⑤）。 |
| 4 | `A1037` | ✅ **已做** —— `Shell/CollectionData.cs` 那句注释里补上**裁定口径**（「创建那一刻物化」不是「显示时翻」· 已有卡组不因切语言改名 · 同类还管 `A1047` 的 `DefaultPlayerName`），并**就地订正一处指错的指针**（原来指 `Loc.cs:1050-1065`，那现读是 `MainMenu/General/OK` 那一段）。`MenuDeck/NewDeckName` **三路都搜过 ⇒ 不是原版键**。 |

**四笔合计：键值 0 处改动 · 行为 0 处改动**（三份 `.cs` 的 diff 逐行核过：**只有注释行**，见 §④）。

---

## ② 改动清单

### 笔 1 · `Core/Loc.cs`（`A1019`）

| 文件:行号 | 改前 | 改后 |
|---|---|---|
| `Core/Loc.cs:1065-1085`（新增 21 行） | *（无）* | 新增注释块「🔴 **2026-10-18 留痕（`A1019`）…**」：原来写 X（13 处写死「知道了」）→ 实际是 Y（13 处改走本条，中文档钮字「知道了」→「确定」）→ 错因 Z（那 13 个「知道了」是我们自写的死串，原版键是 `MainMenu/General/OK`，没按原版键抄 ⇒ 按原版键收口后措辞必然跟着走）；并落两条现核（消费者 14 处 = 12 + 2；全仓写死 `"知道了"` 只剩 `Editor/ShellScene.cs:1132` 那处**自检样例正文**）。 |
| `Core/Loc.cs:1086` | `{ "MainMenu/General/OK", new Entry("确定", "OK") }, // …ZH **自拟**` | 同一条 entry，**值 `"确定"` / `"OK"` 逐字不变**，只在行尾注释后加「（⚠️ 有可见副作用，见上「留痕」块）」。 |

**「13 处」是怎么核出来的（现读）**：`MainMenu/General/OK` 的消费者 = **14 处** ——
`Loc.T("MainMenu/General/OK")` **12 处**（`Net/NetRuntime.cs:112,141` · `Shell/DeckInfoPopup.cs:1191,1284,1402,1429` · `Shell/LeaderboardWindow.cs:749` · `Shell/LiveOpsEventWindow.cs:933,944` · `Shell/PracticeModePopup.cs:1622` · `Shell/RankedEventWindow.cs:190` · `Shell/ShopWindow.cs:795`）
＋ `Loc.T(lkOk)` **2 处**（`Shell/SettingsWindow.cs:1671,3008`）。
第 14 处（`ShopWindow.cs:795`，传奇重复购买确认框，本来就带 `MainMenu/General/Cancel`）**原写不是「知道了」**
（`git log -S'"知道了"' -- Shell/ShopWindow.cs` **零命中**）⇒ **14 − 1 = 13**，与 `§A1019` 的「13 处」**逐数吻合**。
📌 `Editor/ShellScene.cs:1132` 那处 `"知道了"` 是 `PromptPopup` 的**自检样例正文**（注释自陈「本窗只负责排版，正文由调用方给」），**不是消费者、不上屏** ⇒ 别当成「还有一处没接」。

### 笔 2 · 两份输入表（`A1021`）

**零改动** —— 两份文件**在 `HEAD` 里就已经有订正横幅**：

| 文件 | 横幅位置 | 内容要点 |
|---|---|---|
| `Unity/资料/普查产出_1017/现核_按文件施工表.md` | `:3-14` | 「🔴 **2026-10-18 更正（铁律 5）：本表「现状 = 未做」那一列整体过期**」+ 逐条列出 **17 个**编号（`D35 D7 D9 D10 D44 D13 D14 D24 D15 D23 D33 D34 D43 D45 D46 D47 D48`）· 现读全部已做 · 错因 = 写于 `2cfb357` 落地**之前**的静态快照 |
| `Unity/资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md` | `:3-13` | 「48 条待办这个图景是错的 —— 真值 = 44 条已做、只剩 5 条（`D22` `D26` `D28` `D29①` + `A855`）」 |

判据 = `git show HEAD:<路径> \| head -16` 与 `head -8` 都能读到这两段；`git status --porcelain -- Unity/资料/普查产出_1017/` **空输出**（= 干净、已提交）。
落地 commit = **`1667ff6`**（`git log -S'本表「现状 = 未做」那一列整体过期'` / `-S'48 条待办'` 均回它）。

### 笔 3 · `Core/TmpFont.cs`（`A1024`）

| 文件:行号 | 改前 | 改后 |
|---|---|---|
| `Core/TmpFont.cs:8-42`（新增 35 行，**全注释**） | 文件头只有「原来走自写 5×7 点阵（只支持 ASCII）→ 2026-09-12 通了 TMP」那一段 | 其后追加一块「🔴 **2026-10-18（`A1024`）补记 —— 这不是「我们换了字体」，是「全仓只有一个字体」**」：**错的旧说法**（「根因 = 我们换了字体」）· **真正的根因**（现读只有 4 处赋字体、全是 `NotoSerifCJK` ⇒ 拉丁也用汉字字体渲）· **原版拉丁三种字体 + 判据路径**（`assets_full/bundle_fonts_assets_all/` 三份 Static JSON + 三张真图集，`m_AtlasPopulationMode=0` / `m_SourceFontFile=null` ⇒ 不必找 TTF）· **三步现状**（① 已记；**②③ 没做**，逐条写清差什么）· **换的时候要重量**（`WorldGlyphPerFontSize` / `WorldCapPerFontSize` 会牵动全仓版面；中文侧判据本就空、只有拉丁可逐字对）。 |

⚠️ **`TmpFont.cs` 的 diff = 37/0，逐行核过 `grep '^+' | grep -v '^+\s*//'` 为空 ⇒ 一个代码字符都没改**。
⛔ **判据 §A1024 的第 ②③ 步我没有做**（理由见 §⑤），**不是漏做**。

### 笔 4 · `Shell/CollectionData.cs`（`A1037`）

| 文件:行号 | 改前 | 改后 |
|---|---|---|
| `Shell/CollectionData.cs:302-306` | `// 默认卡组名走词条（键 **`MenuDeck/NewDeckName`** … 见 `Core/Loc.cs:1050-1065`；…`<br>`// ⚠️ **如实记**：这个名字**会写进存档** ⇒ 英文档下新建的卡组字面就叫 `New deck`（数据被翻了）。`<br>`//    施工单 §⑦ P1 那一行把同一件事标成「需裁决」，但调度台在 `Loc.cs:1057-1062` 已裁决「三处一律用本键」。` | ① **指针就地订正**（铁律 5）：那两处**现读落在 ③「通用弹窗的三颗钮」**（`MainMenu/General/OK`）那一段、**与默认卡组名无关** ⇒ 改成「按 **⑪「默认 / 演示卡组名」那一节** 认」，并写明⛔别按行号找；② 追加「🔴 **2026-10-18（`A1037`）裁定：保留现状，⛔ 别再翻案**」—— **它【不是】「显示时才翻」，是「创建那一刻按当前语档生成名字、当场物化进存档」**：英文档新建盘上就是 `New deck`、中文档新建就是 `新卡组`（**预期行为、不是缺陷**）；**已有卡组不会因为玩家改语言而改名**（那串字已经是**玩家数据**）——这才是与「显示时翻」的分水岭；反面（把**玩家自己命名**的卡组名塞进 `Loc`）才是错的；同一口径还管 `Shell/ProfileData.cs` 的 `DefaultPlayerName`（`A1047`，同类裁定「翻」）⇒ 铁律 6：同一条规则只留一份；③ 把 **`NewDeckName` 三路搜索的结论**逐路写进注释（见 §③·4）。 |
| `Shell/CollectionData.cs:330` | `string name = Lib.UniqueName(Loc.T("MenuDeck/NewDeckName"));` | **逐字不变**（只是它上方的注释变长了）。 |

---

## ③ 证据

### 3·1 `A1019` 的旧文案与消费者（`D:/4/Unity/MyGame/Assets/CardPresentation/` 下）

- 现读**已无**任何消费者写死 `"知道了"`；全仓仅剩 `Editor/ShellScene.cs:1132`（自检样例正文）。
- `HEAD` 里 `Net/NetRuntime.cs` 还有 **2** 处（本会话 `WNet` 改成 `Loc.T("MainMenu/General/OK")`，工作树剩 1 处 = 那条注释）；
  `Shell/{DeckInfoPopup,LeaderboardWindow,LiveOpsEventWindow,PracticeModePopup,RankedEventWindow,SettingsWindow}.cs` 在 `HEAD` **已经是 0**（`1667ff6` 换掉）。
- 原版键判据（已由前几波写进 `Loc.cs` 的同一段注释，本次未改）：`stringliteral.json` 只 `MainMenu/General/OK`（`0x42BE718`）·
  prefab `Localize.mTerm`（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_2304394373469816941.json`）· 同 GO TMP `m_text = OK`；
  `decomp_full/CatalogItemContainer__TryPurchase.c:37,41,43` 用它 —— 三条证据与 `CatalogItemContainer` 那一处**都对得上**。

### 3·2 `A1021` 的两段横幅**已在 `HEAD`**（本笔零改动的证据）

```
$ git status --porcelain -- Unity/资料/普查产出_1017/     → （空）
$ git show HEAD:Unity/资料/普查产出_1017/现核_按文件施工表.md | head -16
  → :3  > 🔴 **2026-10-18 更正（铁律 5）：本表「现状 = 未做」那一列整体过期。**
    :5  > **实际是 Y**：这 **17 条现读【全部已做】**（`D1–D48` 整体现读 = **44 条已做**，只剩 `D22`/`D26`/`D28`/`D29①` + `A855`）
$ git show HEAD:Unity/资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md | head -8
  → :3  > 🔴 **2026-10-18 更正（铁律 5）：「48 条待办」这个图景是错的 —— 真值 = 44 条已做、只剩 5 条。**
$ git log --oneline -S'48 条待办' -- <对账文件>            → 1667ff6
$ git log --oneline -S'本表「现状 = 未做」那一列整体过期' -- <现核文件> → 1667ff6
```

⇒ **`§A1021` 这一条账是「记录过期」**（那份判据写在这些横幅落地**之前**）。⚠️ **我没有再加第二段横幅**（铁律 6：同一件事只留一处）。

### 3·3 `A1024` 的判据在哪、结论是什么

- `grep '^## §A1024' d:/4/Unity/资料/待办判据_第四会话.md` → **`:276` 命中**（判据指针**没指空**；文末 §附 另有 `:334` 的「整行搬出 · 原文」）。
- 判据正文（`:276-287`）+ 🆕 查证回执（`:334+`）+ 只读报告 `资料/普查产出_第四会话/查证_探针覆盖率与字体口径.md` §3 三处**互相一致**：
  **我们没换字体** · **根因 = 全仓只有一个字体** · **原版三份 font asset 与图集在本地** · **fallback 链 TMP 支持、但我们现在两条都是空的**。
- 🔴 **现读复核（我自己重跑的，不是照抄）**：
  `grep -rn 't.font = Font'` ⇒ 只有 `Core/TmpFont.cs` 的 `NewText` 与 `MeasureGlyph` 两处；三个探针在 `Editor/`。
  `grep -rn '"m_FallbackFontAssetTable"\|m_FallbackFontAssetTable' Resources/Fonts/…asset` ⇒ 空表（与判据一致）。

### 3·4 🔴 `MenuDeck/NewDeckName` —— **三路都搜了**（结论：**不是原版键**）

| 路 | 怎么搜 | 结果 |
|---|---|---|
| ① `assets_full` 的 `mTerm` | `grep -roh '"mTerm": *"MenuDeck[^"]*"' d:/2/新解包资源/assets_full/ \| sort -u` | **42 条 `MenuDeck*`，无 `NewDeckName`**。同族近邻 = `MenuDeck/HUD/New` · **`MenuDeck/HUD/EditDeckName`** · `MenuDeck/MenuButtons/CreateDeck` |
| ② prefab 组件字段的 `text` | `grep -rl '"New deck"\|"My deck"' d:/2/新解包资源/assets_full/` | **0 个文件**（`New deck` 0 · `My deck` 0）。**正对照**：已知原版串 `"Auto zoom"` = **14 个文件**；`bundle_menus_assets_all/` 里带 `"m_text"` 的文件 = **4894** ⇒ **扫描本身有效**，这个 0 不是「没搜到」 |
| ③ `d:/2/tools/il2cpp_out/stringliteral.json` | `grep -c 'NewDeckName'` | **0 命中**。该文件里 `MenuDeck*` 只有 **33 条**，`DeckName` 一族**只有一条前缀串** `MenuDeck/DeckName/`（不带后缀，说明是运行时拼的） |

⇒ **`MenuDeck/NewDeckName` = 我们的自拟键**（与 `Core/Loc.cs` ⑪ 那一节的记录**一致**）；该节同时记着「原版**没有**这三个名（`MenuDeck/DefaultDeckName` / `NewDeckName` / `DemoDeckName`）· 唯一同族真键是 `MenuDeck/HUD/EditDeckName`」。

---

## ④ 验证

**秒级类型检查**（改了 `.cs` ⇒ 必跑；`TMPDIR=/tmp/wf_doc` 独立，避开别的写手）：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

**行尾**（二进制读，改前 → 改后 —— 全是**纯 LF**、没有被翻）：

| 文件 | 改前 CRLF / LF | 改后 CRLF / LF |
|---|---|---|
| `Core/Loc.cs` | 0 / 1975 | **0 / 1997** |
| `Shell/CollectionData.cs` | 0 / 394 | **0 / 417** |
| `Core/TmpFont.cs` | 0 / 310 | **0 / 347** |

**`git diff --numstat`（含上一会话未提交的改动 —— 那不是本批写手的）**：

```
255  8  Unity/MyGame/Assets/CardPresentation/Core/Loc.cs
 37  0  Unity/MyGame/Assets/CardPresentation/Core/TmpFont.cs
 65  3  Unity/MyGame/Assets/CardPresentation/Shell/CollectionData.cs
```

⇒ **扣掉别人的份，本批的净增量**：`Loc.cs` **+21 行注释**（+ 那条 entry 的尾注改动，值不变）· `TmpFont.cs` **+35 行注释（全文件 diff 都是我的）** · `CollectionData.cs` **+28 行注释 + 2 行改写**。

**逐行核「只有注释变了」**（关键一条）：

```
$ git diff -U0 -- Core/TmpFont.cs | grep '^[-+]' | grep -v '^+++\|^---' | grep -v '^[-+]\s*//'
  → （空）
$ git diff -U0 -- Core/Loc.cs | grep '^+' | grep -v '^+++' | grep -v '^+\s*//'
  → （空 —— 我的新增行全是 // 注释；其余命中全是别的写手的键）
$ git diff -U0 -- Core/Loc.cs | grep -n 'MainMenu/General/OK'
  → 74:-  { "MainMenu/General/OK", new Entry("确定",     "OK") }, …ZH **自拟**
  → 97:+  { "MainMenu/General/OK", new Entry("确定",     "OK") }, …ZH **自拟**（⚠️ 有可见副作用，见上「留痕」块）
      ⇒ 两个值 `"确定"` / `"OK"` 逐字相同
```

---

## ⑤ 没查清 / 停手的

1. 🔴 **`A1024` 的第 ②③ 步我【停手】了 —— 不是没看见，是不能在本轮做**，理由三条，请调度台裁：
   - **必须先跑 Unity**：② 是「从原版 JSON + 图集 PNG 重建三份 Static `TMP_FontAsset`」，本质是编辑器脚本 + `-executeMethod`；本轮⛔不跑 Unity（用户口径：待办没做完之前不跑自检，而这一步本身就是 Unity 腿）。
   - **会改行为、且会牵动全仓版面**：③ 要改 `NewText` 里 `t.font = Font` 那一跳（判据点名的 `Core/TmpFont.cs:155` 就是它）—— 判据自己写着「`WorldGlyphPerFontSize`/`WorldCapPerFontSize` 要按新字体重测、**全仓版面会重排**」。本线的定义是「⛔ 不改任何行为」，硬做**会当场掀翻别的在飞写手**（卡面/菜单/战斗全部依赖这套换算）。
   - **判据自己还有一条没查清**（`§3·4` 原文）：`Asar Regular White` 等 **58 个 Material** 要不要一并复刻 · 重建后 `ApplyOutline` 那套 `_OutlineWidth/_Underlay*` 会不会被覆盖 ⇒ 即使跑 Unity，这条也得先有答案。
   ⇒ **建议**：单开一笔（占 `Core/TmpFont.cs` + `Editor/TmpSetup.cs` + `Resources/Fonts/`），**按「逐件换、逐件验收」**排，且必须排在**别的写手全停**之后。
2. ⚠️ **`§A1037` 自己的指针是旧的**：账上写「`Shell/CollectionData.cs:260` 用 `Lib.UniqueName(...)`」，**现读在 `:307`**（本批注释之后 `:330`）—— 文件被前几波的注释撑长了。⛔ 我没动判据文件（白名单：**仅当 `A1024` 判据要求动它**才可改），**报给调度台**。
3. ⚠️ `Core/Loc.cs` ⑪ 那一节里写着「ZH 三列 = **改之前**写死在 `Deck/DeckRuntime.cs:746/3430/6323` 与 `Shell/CollectionData.cs:260` 的那三个原话」——
   它**自己标了「改之前」**，属**历史坐标**、不是错，**我没动**；但按符号认仍然更稳（`DeckRuntime.SetDeckName` / `DeckEditorState.NewDeck` / `CollectionData.CreateDeck`）。
   📌 **另注**：`git status` 里 `Unity/资料/待办判据_第四会话.md` 显示 `M`（`15/5`）—— **那是别的写手的未提交改动，不是我**：
   我**一行都没动它**（本批只读它）。判据第 5 项白名单的条件（「仅当 `A1024` 判据要求动它」）**未触发**，因为 §A1024 自己写着
   「① 把上面的路径写进正本（**本节已写**）」⇒ **无需再改**。
4. ⚠️ **`A1021` 的「13 / 14」之外还有一条小口径差**：`§A1019` 说「13 处」，我核出 14 个**消费者**、13 个原本写「知道了」——
   两者**不矛盾**（第 14 个 = `ShopWindow` 那颗，本来就不写「知道了」）。已在 `Loc.cs` 的留痕块里写死这个算法，免得下个会话再数一遍。

---

## ⑥ 顺手发现的（只报，未改）

1. 🔴 **`A1021` 这一条账整体是「记录过期」** ⇒ 与第六会话开工时销掉的 6 条**同一类**（都是交接文档没跟上）。建议**销账**（横幅已在 `HEAD`，`1667ff6`）。
2. 🔴 **`Core/Loc.cs` 里同一处「OK 键」的判据注释与「留痕」是两段**（`:1043-1063` 查证 / `:1065-1085` 留痕）。
   下个会话若再动这一条键，**两段都要看**（查证管「键名对不对」、留痕管「改了会看到什么」）。
3. ⚠️ **`Editor/ShellScene.cs:1132` 那处 `"知道了"` 是个「像消费者但不是」的诱饵** —— `交件_波1_P3` 已点过一次
   （「按中文子串 grep 的人会误当消费者」），这次核 13 处时它又跳出来一次 ⇒ 值得在留痕里写死（我写了）。
4. 📌 **`stringliteral.json` 里 `MenuDeck/DeckName/` 是带尾斜杠的前缀串**（无后缀） ⇒ 原版有一处是「运行时拼键名」的
   （`…/DeckName/` + 变量）。**未追**（不在本批范围）—— 若将来要把「卡组名」做进原版键，这条路要先查。

---

## ⑦ 摘要（300 字以内）

四笔里 **两笔真做、一笔早就是好的、一笔只落了一半**。

`A1019`：`Core/Loc.cs` 那条 `MainMenu/General/OK` 的注释里加了一段**留痕**（原来写 X：13 处写死「知道了」；实际是 Y：全按原版键收口 ⇒ 中文档钮字变「确定」；错因 Z：那 13 个串是我们自写的、不是照原版键抄的），并核出现读消费者 **14 处、其中 13 处原写「知道了」**，与账上「13 处」吻合。**键值一字未动**。

`A1021`：**两份订正横幅【早已在 `HEAD` 里】**（`1667ff6`，第四会话落地；`git show HEAD:` 可读、工作树干净）⇒ **零改动**，这笔账是「记录过期」，建议销。

`A1037`：`Shell/CollectionData.cs` 注释补上**裁定口径**（创建那一刻物化、不是显示时翻；已有卡组不因切语言改名）＋**就地订正一处指错的指针**；`MenuDeck/NewDeckName` **三路都搜过 ⇒ 不是原版键**（mTerm 42 条无它 · prefab text 0 文件、正对照 14/4894 · stringliterals 0）。

`A1024`：判据三步里 ①已记、**②③要跑 Unity 且会牵动全仓版面 ⇒ 停手**，只把结论与「没做」状态如实写进 `Core/TmpFont.cs` 文件头（纯注释），**请另派**。

**三份 `.cs` 的 diff 逐行核过：只有注释；类型检查 0/0；行尾纯 LF 未翻。**
