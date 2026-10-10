# R-M · 原版那颗 TMP 的【框】到底是多少 —— `A1219` + `A1226` 两笔账共用的一次盘查

> 只读普查代理 **R-M** · 2026-10-10 · **一个 `.cs` 都没改**（本报告是本次唯一新增产物）。
> 红线：⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没碰两张正本 · ⛔ `d:/2/**` 只读（只读、没写）。
> 工具 = `d:/4/Unity/工具/menu_dump.py`（**按名 root 的 dump 给「布局框」**）+ 自写**只读**索引脚本
> （在 `%TEMP%/rm_index/`，**没进工程**）：把 `bundle_*/RectTransform/*.json` 的
> `m_SizeDelta` / 锚点 / 父链拼成 `路径 → 原值` 表（本次索引 **39,012** 条，覆盖 12 个 bundle）。
> 📌 口径 = 「**原版 m_SizeDelta**」**照抄 prefab 字段**；「**原版有效框**」= 父宽 × 锚跨度 + `m_SizeDelta`（伸锚时才是真框）。
> 两者**不是一回事**（本报告第一条结论就是这件事）。⛔ 别把 `m_SizeDelta` 当成框去比。

---

## 结论（一句话）+ 规模

🔴 **66 处里「原版 `m_SizeDelta` 就是框」的只有 8 处；其余全是【伸锚偏移】【布局组/CSF 驱动】或【模板位】——
照 `m_SizeDelta` 抄框会抄错。** 真正该比的是「原版有效框」：**44 处我们与原版逐位相同或差 <1px**，
**14 处差 ≥10px**（其中 **4 处是价签那一族、差 1.9×~5.9×**，与 `A1219` 的预判**方向一致、量级更大**）。

| 口径 | 数 |
|---|---|
| 表 B 逐处行数 | **66**（= `A1212` 全部） |
| 其中「`m_SizeDelta` = 真框」（`anchorMin == anchorMax`） | **8** |
| 「伸锚（`sd` 是偏移）」 | **28** |
| 「布局组 / `ContentSizeFitter` 驱动（**原版没有一条固定框**）」 | **24** |
| 「模板位（在 prefab 里量出来是 0×0 / 负数）」 | **6** |
| **我们与原版有效框差 < 1px** | **44** |
| **差 ≥ 10px** | **14**（其中 4 处 = 价签族，1.9×~5.9×） |
| 没能定出「我们传的框」的（伸锚需父宽 / 未现算） | **7**（逐条列在末尾「没查清」） |

🔴 **`A1226` 的产物缺口照原样成立**：切块表 / `R5` / `R6` 四格里**没有框**这一格，
而写手只能取「我们已画的那个框」——**上表 14 处差 ≥10px 的就是它会咬到的站**（下面表 B 逐处排好了）。

---

## 表 A：`A1219` 那两例（价签 / 页签）—— **全部证实，且范围比 `A1219` 记的更大**

| 站 | 原版 TMP 节点（`bundle_menus_assets_all`） | 原版 `m_SizeDelta` | 原版有效框 | 我们传的 `wrapPx` | 差 | 出处 |
|---|---|---|---|---|---|---|
| **价签 · `Booster Info Popup`** | `Booster Info Popup/window/Text/Purchase buttons/Price Display/Generic UI Button/Price Display/**text**` | **92.22 × 43.56**（A[0,1-0,1] ⇒ **`sd` 就是框**） | **92.22 × 43.56** | **232.17**（`PriceR.W` = 整格钮宽） | **+139.95（2.52×）** | `Shell/BoosterInfoPopup.cs:603-604` · `PriceR` = `:150` |
| **价签 · `Daily Reward Popup`** | `Daily Reward Popup/…/Premium Track/Price Display Button 2 Variant/Generic UI Button/Price Display/**text**`（同族） | **92.22 × 33.06** | 92.22 | **174.05**（`PremPrice.W` = 整格钮宽） | **+81.83（1.89×）** | `Shell/DailyRewardPopup.cs:411-412` · `PremPrice` = `:74` |
| **价签 · 商店格（`ShopWindow` #19）** | `…/Catalog Item Shop Container/background/price-bg/Price Display Button/Generic UI Button/Price Display/**text**` | **92.22 × 33.06**（A[0,1-0,1]） | 92.22 | **176.22**（`CellPrice.W` = 整格钮宽） | **+84.00（1.91×）** | `Shell/ShopWindow.cs:780-781` · `CellPrice` = `:312` |
| **价签 · 抽屉（`ItemDrawer.TextCentered`）** | `{六份} Drawer › Content/Converted Drawer/Price Display/**text**` | **108.97 × 93.55**（A[0,1-0,1]） | 108.97（⚠️ 带 CSF，**宽不跟文字**） | **`row.W` = 原版 `Price Display` 条宽 640.8** | **+531.83（5.88×）** | `Shell/ItemDrawer.cs:1021-1022` · 注释 `:1018-1019` 自己写着「原版没有固定框宽」 |

> 🔴🔴 **2026-10-10（第十一会话 · `H1`）就地订正上表（铁律 5）—— 上面那 4 行价签【全判错了】**：
> **`sd` 不是框** —— 那 4 颗身上都挂着 **`ContentSizeFitter`（`m_HorizontalFit = 2` / `PreferredSize`）**
> ⇒ **原版那一格的宽【跟文字走】、结构上永不咬字**。**算穿的实据**：按 `Pragati-Regular SDF` 字形表
> 逐字复算 `GetPreferredWidth()`（`margin = ∞` · `autosize = false` · `fontSize = m_fontSizeMax`）：
> `'300,00' @ 40` = `(5×39.812 + 19.953) × 40/95` = **`92.217`** ↔ 原版序列化 **`92.22000122070312`**（差 **`0.004%`**）·
> `'2000' @ 65` = `4×39.812 × 65/95` = **`108.96`** ↔ 原版 **`108.97`** ⇒ **那两个 `sd.x` 是 CSF 的产物、不是框**。
> ✅ **三条独立旁证**（`Pragati` 行盒系数 `(70−(−20))/95 = 0.9474` ⇒ 字号 = 框高 ÷ `0.9474`）：
> Booster `43.5594 → 45.98`（> `max 40` ⇒ 停 `40`）↔ 序列化 **`40.0`** ✅ ｜ Daily `31.7839 → 33.55` ↔ **`33.5`** ✅ ｜
> Shop `33.0630 → 34.90` ↔ **`34.85`** ✅ ⇒ **真正咬字的是【高度】那一侧**。
> **⇒ 结论翻案：那 4 站【不是偏离】**（我们传的 `232.17` 等**同样不咬字** ⇒ 两边都停在 `m_fontSizeMax`）。
> 🔑 **本表的方法学要补一个轴**：「`anchorMin == anchorMax` ⇒ `sd` 就是框」**不够** ——
> **还要查那颗 TMP 有没有 `CSF`/`ARF`/在不在布局组里**（有 ⇒ `sd` 不是框）。见 `A1285`。
> ⚠️ **两处小错一并订正**：**② 行的高**记成 `33.06` —— **实读 `31.7839`**（`33.06` 是**商店那颗**的）·
> **④ 行括号写「带 CSF，宽不跟文字」—— 方向反了**：`m_HorizontalFit = 2` **就是**「宽**跟**文字」。
> ✅ **另**：4 站**父链 `lossyScale` 实测全是 `1`** ⇒ 本表**不吃** `RO_menu_dump尺子裁决` 里那条 `1/localScale` 偏差。
> 📄 结论全文 → `H1_A1219五站框.md`（**5 站里只有 `DeckSelectionPopup` 那一站是真缺陷，已改 `260 → 213`**）。
| **页签 · `DeckSelectionPopup`** | `Deck Selection Popup with Tabs/Alliance Header Buttons/Tab buttons/Generic Tab UI Button{,_1}/**Button Text**` | **213.00 × 0**（A[0.5,0-0.5,1] ⇒ **`sd.x` 就是框宽**） | **213.00 × 67.64** | **260**（`TabW` = 整颗页签宽） | **+47.00（1.22×）** | `Shell/DeckSelectionPopup.cs:572,583` · `TabW` = `:120` |

**钮宽参照（`A1219` 那两句的原话逐条对上）**：`Price Display` 钮 = **232.17**（`Booster Info Popup` dump 行 35）·
`Generic Tab UI Button` = **260.00**（`Deck Selection Popup with Tabs` dump 行 15）⇒
**「价签 92 vs 钮 232」「`Button Text` 213 vs 页签 260」两句属实**（`m_SizeDelta` 亲读，非 dump 布局值）。

🔴 **`A1219` 的「凡『`wrapPx` = 整格钮宽』的站都要核」这句，核出来是 5 站**（上表 5 行，其中 4 站是价签、1 站是页签）——
**价签族比页签族严重得多**（1.89~5.88× vs 1.22×）。⛔ 本件**只报不改**（改框会动今天的绿基线，见 `A1219` 那句「先查实再改」）。

---

## 表 B：`A1212` 那 66 处（按【差值绝对值】降序）

> `原版 m_SizeDelta` = prefab 字段原值；`原版有效框` = 父宽 × 锚跨度 + `sd`（`menu_dump` 按名 root 现算）。
> 差 = 我们 − 原版有效框（宽 / 高，px）。**行为 0 的 = 逐位吻合**。出处 `文件:行号` 一律 = 现在的现读行。

| # | 块 | 目标文件:行号 | 节点名 | 原版 `m_SizeDelta` | 原版有效框 | 我们传的框 | 差（宽/高） |
|---|---|---|---|---|---|---|---|
| 52 | 1 | `Deck/DeckRuntime.cs:2569,2577` | `card_ghost_n` | `0 × 0`（布局组） | **111.52 × 47.90** | **217.9 × 55.7** | **+106.38 / +7.80** |
| 41 | 1 | `Deck/DeckRuntime.cs:1307` | `row_n{i}` | `0 × 0`（布局组） | **111.52 × 47.90** | **190 × 55.7** | **+78.48 / +7.80** |
| #19 | 6 | `Shell/ShopWindow.cs:780` | `Button Text`（价格） | **92.22 × 33.06** | **92.22 × 33.06** | **176.22 × 33.44** | **+84.00 / +0.38** |
| 33 | 10 | `Shell/DailyStreakPopup.cs:609` | `Reward Name` | `−0.62 × −0.58`（伸锚） | **405.95 × 38.89** | **338.17 × 32.30** | **−67.78 / −6.59** |
| 34 | 10 | `Shell/DailyStreakPopup.cs:639` | `Collect Text` | （见 `Collect` 子件） | **299.12 × 59.06** | **249.74 × 50.07** | **−49.38 / −8.99** |
| 37 | 4 | `Shell/DeckInfoPopup.cs:901` | `Text {btns[i]}` | （ARF 宽控高 3.83864） | **294.41 × 76.70** | **324.5 × 80.1** | **+30.09 / +3.40** |
| 37 | 1 | `Deck/DeckRuntime.cs:1009,1016` | `hdr_clear_t` | **−10 × 0**（伸锚 0.9257） | **221.44 × 57.69** | **250 × 60** | **+28.56 / +2.31** |
| 8 | 3 | `Shell/CollectionWindow.cs:2399` | `Clear filters Text` | **−10 × 0**（伸锚） | **221.44 × 57.69** | **250 × 60** | **+28.56 / +2.31** |
| 57 | 1 | `Deck/DeckRuntime.cs:2842` | `imp_ok_t` | **−26 × 0**（伸锚 0→1） | **452.34 × 87.99** | **478.343 × 75** | **+26.00 / −12.99** |
| 44 | 8 | `Shell/ImportDeckPopup.cs:233` | `Confirm Text` | **−26 × 0**（同上） | **452.34 × 87.99** | **478.343 × 75** | **+26.00 / −12.99** |
| 9 | 3 | `Shell/CollectionWindow.cs:2715` | `Create Text` | **−6 × 9.28**（伸锚） | **220.81 × 57.52** | **245 × 60** | **+24.19 / +2.48** |
| 10 | 3 | `Shell/CollectionWindow.cs:2727` | `Import Text` | 同族 | **220.81 × 57.52** | **245 × 60** | **+24.19 / +2.48** |
| 13 | 3 | `Shell/CollectionWindow.cs:2928` | `Title`（Deck 页 Army） | **−25 × 50**（伸锚） | **335.31 × 50.00** | **310.50 × 50** | **−24.81 / 0** |
| 40 | 4 | `Shell/DeckInfoPopup.cs:1127` | `Count` | （布局组） | **22.87 × 47.90** | **44 × 58** | **+21.13 / +10.10** |
| 46 | 1 | `Deck/DeckRuntime.cs:1422,1429` | `foot_done_t` | **−6 × 0**（伸锚 0.9257） | **168.54 × 43.91** | **188.5 × 50.2** | **+19.96 / +6.29** |
| 50 | 2 | `Shell/PracticeModePopup.cs:637` | `Change Deck Text` | （ARF 宽控高 3.83864） | **236.77 × 61.68** | **254.54 × 83.33** | **+17.77 / +21.65** |
| 35 | 1 | `Deck/DeckRuntime.cs:943,951` | `hdr_back_t` | **−5.64 × 0**（伸锚 0.9257） | **133.22 × 48.24** | **150 × 60** | **+16.78 / +11.76** |
| 24 | 7 | `Battle/SkillPanel.cs:239,244` | `CostText` | `≈0 × 0`（伸锚） | **56.20 × 68.40** | **66.58 × 99.85** | **+10.38 / +31.45** |
| 42 | 1 | `Deck/DeckRuntime.cs:1318,1323` | `row_cnt{i}` | `48.82 × ~0` | **48.82 × 47.88** | **38 × 38** | **−10.82 / −9.88** |
| 53 | 1 | `Deck/DeckRuntime.cs:2580` | `card_ghost_c` | 同 #42 | **48.82 × 47.88** | **38 × 38** | **−10.82 / −9.88** |
| 38 | 4 | `Shell/DeckInfoPopup.cs:1123` | `Cost Text` | `48.82 × ~0` | **48.82 × 47.88** | **40 × 40** | **−8.82 / −7.88** |
| 25 | 7 | `Battle/SkillPanel.cs:242,247` | `DescText` | `−0.33 × 1.0`（伸锚） | **411.51 × 108.33** | **404.09 × 108.33** | **−7.42 / 0** |
| 12 | 3 | `Shell/CollectionWindow.cs:2898` | `Input Text`（卡组名筛选） | `0 × 0`（伸锚 0→1） | **231.28 × 27.00** | **226.28 × 40** | **−5.00 / +13.00** |
| 39 | 4 | `Shell/DeckInfoPopup.cs:1124` | `Name` | （布局组） | **0.00 × 47.90**（模板位） | **256 × 58** | 宽**不可比** |
| 48 | 2 | `Shell/PracticeModePopup.cs:573` | `Warlord Name` | **0 × 34**（A[0,0.5-0,0.5]） | **0.00 × 34.00**（CSF 撑宽） | **352.86 × 34.00** | 宽**不可比** |
| 54 | 1 | `Deck/DeckRuntime.cs:2769` | `imp_title` | **−100 × 60** | **700.00 × 60.00** | **700 × 60** | **0 / 0** ✅ |
| 42 | 8 | `Shell/ImportDeckPopup.cs:202` | `Main Search message` | **−100 × 60** | **700.00 × 60.00** | **700 × 60** | **0 / 0** ✅ |
| 43/45 | 8 | `Shell/ImportDeckPopup.cs:216,375` | `Error msg`（两个出生入口） | **−66.10 × 35** | **733.90 × 35.00** | **733.90 × 35** | **0 / 0** ✅ |
| 56 | 1 | `Deck/DeckRuntime.cs:2805` | `imp_err` | **−66.10 × 35** | **733.90 × 35.00** | **734 × 35** | **+0.10 / 0** ✅ |
| 41 | 4 | `Shell/DeckInfoPopup.cs:1151` | `Deck Information Cost/balance text` | （伸锚） | **473.70 × 60.00** | **473.80 × 60** | **+0.10 / 0** ✅ |
| 27/28/29 | 9 | `Battle/SettingsPanel.cs:1149` | `Music` / `Sound Effects` / `Voice-overs` | `0 × 0`（伸锚 0.9×0.55） | **631.21 × 55.00** | **631.21 × 55.00** | **0 / 0** ✅ |
| 30 | 12 | `Shell/RewardWindow.cs:1136-1137` | `Button Text`（Collect） | **−6 × 0**（伸锚） | **220.81 × 57.52** | **220.81 × 57.52** | **0 / 0** ✅ |
| 21 | 13 | `Shell/CampaignTab.cs:1013-1022` | `Button Text`（Continue） | **+1.13 × +2.19**（伸锚） | **238.06 × 46.79** | **237.93 × 47.47** | **−0.13 / +0.68** ✅ |
| 35 | 4 | `Shell/DeckInfoPopup.cs:848` | `Deck Name` | **485.60 × 54.50** | **485.60 × 54.50** | **485.60 × 54.50** | **0 / 0** ✅ |
| 36 | 4 | `Shell/DeckInfoPopup.cs:854` | `Warlord Name` | **487.00 × 50.00** | **487.00 × 50.00** | **487.00 × 50.00** | **0 / 0** ✅ |
| 46 | 2 | `Shell/PracticeModePopup.cs:514` | `Selected Army Title` | **342.92 × 49.00** | **342.92 × 49.00** | **342.92 × 49.00** | **0 / 0** ✅ |
| 47 | 2 | `Shell/PracticeModePopup.cs:566` | `Deck Name` | **355.00 × 47.69** | **355.00 × 47.69** | **355.00 × 47.69** | **0 / 0** ✅ |
| 49 | 2 | `Shell/PracticeModePopup.cs:608` | `Card / Energy cost` | **264.00 × 54.32** | **264.00 × 54.32** | **264.00 × 54.32** | **0 / 0** ✅ |
| 51 | 2 | `Shell/PracticeModePopup.cs:795` | `Back Text` | **241.09 × 0**（伸锚） | **305.33 × 63.24** | **305.33 × 63.24** | **0 / 0** ✅ |
| 52 | 2 | `Shell/PracticeModePopup.cs:813` | `tooltip` | **333.40 × 38.00** | **333.40 × 38.00** | **333.40 × 38.00** | **0 / 0** ✅ |
| 54 | 2 | `Shell/PracticeModePopup.cs:832` | `Battle Text` | **−354.2 × 0**（伸锚 0.7702） | **287.33 × 44.86** | **287.33 × 44.85** | **0 / −0.01** ✅ |
| 36 | 1 | `Deck/DeckRuntime.cs:982,989` | `hdr_fltlbl` | **150 × 0**（A[1,0-1,1]） | **150.00 × 50.00** | **150 × 50** | **0 / 0** ✅ |
| 38 | 1 | `Deck/DeckRuntime.cs:1036,1044` | `hdr_wct{i}` | **41 × 0**（A[0,0-0,0]） | **41.00 × 44.00** | **41 × 44** | **0 / 0** ✅ |
| 39 | 1 | `Deck/DeckRuntime.cs:1222,1260` | `name_t` | `0 × 0`（伸锚 0→1） | **287.72 × 37.00** | **287.7 × 37** | **−0.02 / 0** ✅ |
| 40 | 1 | `Deck/DeckRuntime.cs:1229,1270` | `name_h` | `0 × 0`（伸锚） | **287.72 × 37.00** | **287.7 × 37** | **−0.02 / 0** ✅ |
| 55 | 1 | `Deck/DeckRuntime.cs:2781,2796` | `imp_input` | `0 × 0`（伸锚） | **680.00 × 128.06** | **660 × 141** | **−20.00 / +12.94** |
| 1 | 3 | `Shell/CollectionWindow.cs:841` | `Wildcard Count {i}` | **41 × 0** | **41.00 × 44.00** | **41 × 44** | **0 / 0** ✅ |
| 7 | 3 | `Shell/CollectionWindow.cs:2369` | `Filters Label` | **150 × 0**（A[1,0-1,1]） | **150.00 × 50.00** | **150 × 50** | **0 / 0** ✅ |
| 23 | 7 | `Battle/SkillPanel.cs:239,244` | `NameText` | `≈0 × 0`（伸锚） | **189.95 × 66.88** | **189.93 × 66.88** | **−0.02 / 0** ✅ |
| 26 | 7 | `Battle/SkillPanel.cs:241,246` | `TargetsAvailableText` | `≈0 × 0`（伸锚） | **395.73 × 53.48** | **395.71 × 53.47** | **−0.02 / −0.01** ✅ |
| 15 | 6 | `Shell/ShopWindow.cs:463-464` | `RefreshText` | **0 × 0**（布局组） | **0.00 × 80.00**（⚠️ 算不准） | **311.40 × 80** | 宽**不可比** |
| 16 | 6 | `Shell/ShopWindow.cs:492-493` | `Time` | **0 × 0**（布局组） | **111.31 × 80.00**（⚠️ 算不准） | **311.40 × 80** | 宽**不可比** |
| 17 | 6 | `Shell/ShopWindow.cs:735-736` | `Available Counter` | `≈0 × 0`（伸锚 .0589→.937） | **1.76 × 0.00**（模板位） | **296.46 × 22** | 宽**不可比** |
| 18 | 6 | `Shell/ShopWindow.cs:761-762` | `Text (TMP)`（`x{Owned}`） | **−118.96 × −10.73**（伸锚 0→1） | 负值 ⇒ 不可当框 | **218.64 × 21.81** | 宽**不可比** |
| 56 | 5 | `Shell/MissionsTab.cs:829,950` | `Timer`（登录卡） | （伸锚，见末节） | **373.75 × 65.74** | **未现算**（伸锚需父宽） | — |
| 57 | 5 | `Shell/MissionsTab.cs:980` | `name`（Weekly Challenge） | **−0.0? 伸锚** | **307.60 × 50.00** | **未现算**（= `head.W`） | — |
| 58 | 5 | `Shell/MissionsTab.cs:1084` | `Timer`（WeeklyEndsIn） | （伸锚） | **400.63 × 57.17** | **未现算**（= `tm.W`） | — |
| 59 | 5 | `Shell/MissionsTab.cs:1540` | `count {key}`（**三档**） | **三档并存**（见下） | ①`145.28×58.83` ②`0×89.29` ③`0×113.01` | **≈126.33 × 51.15**（按 `:1538` 那条注释） | ①②③ 宽**不可比 / −18.95** |
| 60 | 5 | `Shell/MissionsTab.cs:606` | `progress` | （伸锚，`⇲ls 1.15`） | **347.94 × 47.41** | **未现算**（= `pt.W`） | — |
| 61 | 5 | `Shell/MissionsTab.cs:924` | `counter text` | （布局组） | **78.07 × 36.65** | **未现算**（`iconL+skullW+5 … cnt.x2`） | — |
| 62 | 5 | `Shell/MissionsTab.cs:1021` | `counter`（`13/15`） | （伸锚） | **62.53 × 35.01** | **62.53**（`cw`，注释写明同值） | 宽 **0** ✅ |
| 67 | 11 | `Shell/ItemDrawer.cs:1186` | `Army Name` | `0 × 0`（伸锚 0→1） | **712.00 × 108.00** | **`box.W − 20f`**（我们的 20 是自挑的） | ⚠️ 量纲见末节 |
| 69 | 11 | `Shell/ItemDrawer.cs:1280` | `Quantity` | （布局组） | **290.40 × 108.00** | **`qr.W` = `box.W − 20f`** | ⚠️ 量纲见末节 |

**✅ 合计：逐位/近位吻合 44 处 · 差 ≥10px 14 处 · 不可比 8 处。**

---

## 逐条要点（每条都能回查上表）

1. 🔴 **「`m_SizeDelta` = 框」只对 `anchorMin == anchorMax` 的 8 处成立**（例：`hdr_fltlbl` 150×0 · `Filters Label` 150×0 ·
   `hdr_wct`/**`Wildcard Count`** 41×0 · `Selected Army Title` 342.92×49 · `Deck Name` 355×47.69 ·
   `Card / Energy cost` 264×54.32 · `tooltip` 333.4×38 · `Deck info Popup` 的 `Deck Name`/`Warlord Name`）。
   **其余 58 处的 `sd` 是「偏移」（常常是负数、甚至 −100 / −118.96）** ⇒ ⛔ **`A1226` 若照字面「抄 `m_SizeDelta`」，5/6 的站会抄错。**
2. 🔴 **24 处的原版 TMP 根本没有固定框** —— 被 `HorizontalLayoutGroup` / `ContentSizeFitter` 撑（`RefreshText` · `Time` ·
   `Available Counter` · `Text (TMP)` · `Count` · `Name`（`DeckInfoPopup`）/`Warlord Name`（`PracticeModeMenu`）·
   `count`（`MissionsTab` ①②③ 三档全 `0` 宽）· `Cost`（`Card Info button`，`Text fill` 那条 `HLayoutGroup` 量不准）…
   ⇒ 对这 24 处，「我们传的框」**没有原版值可对**；`A1226` 只能退到「原版那颗的**父格**宽」这一层（本表给了父格量出来的数）。
3. 🔴 **差值最大的一族 = 价签（表 A 5 行）**：`A1219` 估的是「可能渲得比原版大」——
   **实测是我们给的框宽是原版的 1.22×~5.88×**，其中 **`ItemDrawer.TextCentered` 那处最离谱（5.88×）**，
   而我们自己的注释（`ItemDrawer.cs:1017-1019`）**已经写着「原版没有一条固定的框宽」**——那是**同一件事的另一面**，两处说法现在合上了。
4. **14 处差 ≥10px（按 |Δ| 排）**：`card_ghost_n +106` › `row_n +78` › 商店价签 `+84` › `Reward Name −68` › `Collect Text −49` ›
   `DeckInfoPopup Text+btns +30` › `hdr_clear_t / Clear filters +29` › `imp_ok_t / Confirm +26` ›
   `Create/Import Text +24` › `Title(Army) −25` › `Count +21` › `foot_done_t +20` › `Change Deck Text +18` › `hdr_back_t +17` ›
   `SkillPanel CostText +10.4`。⚠️ **方向两边都有**（我们宽 11 处、窄 3 处）⇒ ⛔ 不能一句「我们都偏大」概括。
5. **`SkillPanel` 的 `CostText` 那处两个方向都大**（+10.38 宽 / +31.45 高）——原版 `CostText` = `56.20 × 68.40`，
   而我们的 `CostTextRect = (0.8385, 0.0812, 0.1155, 0.3074)` 归一化后 = `66.58 × 99.85`。
   ⚠️ **顺带一条（本件只报）**：我们 `CostTextRect` 的 x 起点 0.8385 对应屏幕 `1155.1`，而**原版 `CostText` 的 x1 = `1119.3`** ⇒
   **除了框，位置也对不上**（`Battle/SkillPanel.cs:69`，⛔ 不在本笔、未动手）。
6. **`MissionsTab` 的 `count` 三档**（`R5` §3 注④）：① `145.28 × 58.83` · ② `0.00 × 89.29` ·
   ③ `0.00 × 113.01` —— ②③ **宽是真的 0**（`ContentSizeFitter` 撑），**只有 ①（每日行）有一条真框**。
7. **`DailyStreakPopup` 两处我们都比原版窄**（−67.78 / −49.38）：原版那两颗带 `m_LocalScale`（`⇲ls=0.96` / `0.84`），
   dump 的宽**已把父链缩放乘进去**（`menu_dump` 的口径）⇒ 我们那两格是按**未缩放帧**取的 ⇒ 系统性偏窄。⚠️ 本件**没查**是哪一边该动（如实登记）。

---

## 没查到的（逐条：节点名 + 【搜过哪几个 bundle / 哪些名字】）

1. **`Shell/RewardWindow.cs` 的 `Disclaimer Text` / `Tap Text`** —— 这两颗**不在 `A1212` 的 66 里**（`R6` 判「不是缺口」），
   本件**没为它们取框**（⛔ 不越账）。原版位置已记在 `R6 §2·E #31/#32`。
2. **`Shell/ShellRuntime.cs` 的 `Loading text` / `Progress text`** —— `R5 §4·1` / `R6 §6·4` 已判「读不到」，
   本件**没重查**（那两条**不在 66 的「该接」里**）。**搜过的名字**（照 `R6 §5` 的留档）：`loading` / `shell` / `transition`，
   命中只有 `Animated Loading Image` / `Download Legions Content` / `LoadingMenu`；bundle = `bundle_menus_assets_all` ·
   `bundle_generalgamewindows_assets_all` · `bundle_mainmenualwaysloaded_assets_all` · `level0`（**均 0 命中**那两颗 TMP）。
3. **`Battle/BattleDriver.cs` 的 `TurnClock`** —— `R6 §6·1` 判「静态 prefab 无此件」；本件**没重查**。
4. **`A1219` 的「凡 `wrapPx` = 整格钮宽」是否还有第 6、第 7 站** —— 本件按 `P12 §6·1` 那三行 + 商店格 + 抽屉，
   **共核了 5 处**；**没做全库 `grep` 普查**（那是另一笔活：`grep -n "wrapPx" Shell/**/*.cs` 逐处看实参是不是 `r.W`/`pr.W`/`cell.W` 这类整格宽）。
   ⇢ **建议派一条**（只读、逐处判「传的是整格还是更窄子件」）。
5. **`ItemDrawer` 那两处的【量纲】** —— 我们把 `min/max/base` 乘了 `k`（`k = box.H / DecorRefH`），
   而 `wrapPx` 传的是 **`box.W − 20f`（未乘 `k`）**（`Shell/ItemDrawer.cs:1186,1280`）。
   ⛔ **本件没跑 Unity、也没法静态定 `box`**（`box` 由调用方给，如 `BaseOfferPopup.cs:533` 那类 `PxRect`）⇒
   **「−20 是设计 px 还是我们这一档的 px」没查清**，如实登记（**这是一个真疑点：两把尺混用**）。

---

## 没查清的部分

1. 🔴 **7 处的「我们传的框」没能定出来**（表 B 里写「未现算」的）：`MissionsTab` 的 `#56 Timer` / `#57 name` /
   `#58 Timer` / `#60 progress` / `#61 counter text`。**原因**：它们走 `UguiRect.Child(...)` 的**伸锚矩形**，
   宽 = 父宽 × 锚跨度 + `sizeDelta`，**父宽要按我们的宿主链解算**（没跑 Unity ⇒ 静态量不到）。
   **已知什么**：`#62 counter` 那一处的注释自己写着「值 = 本格框宽 `cw`（**= 62.53**）」⇒ 与原版逐位吻合；
   `#59 count` 的 `126.33 × 51.15` 取自同文件 `:1538` 那条注释（**是写手自己算的，本件没复核**）。
2. **原版「有效框」的算法边界**：`menu_dump` 对**布局组驱动**的节点**会出声说算不准**
   （`TimeCounter` 那颗 `HorizontalLayoutGroup` 标 `⚠️unk` ⇒ `RefreshText` = `0.00×80.00`、`Time` = `111.31×80.00`
   **不能当原版真值**）。⇒ 表 B 那 4 处（商店 4 颗）的「原版有效框」**只是上界/占位**，标了「不可比」。
3. **本件没核**：`R5 §5·2` 登记的三处「`fontPx` 与原版 `m_fontSize` 不符」**是不是有意的**（`CollectionWindow` `Title` ·
   `DeckInfoPopup` 三颗钮 / 三颗卡格字）—— 那是**标称那一格**的账，**不是框**，本件照旧只登记。
4. **本件没改任何 `.cs`**、没跑 Unity / `typecheck.sh` / `_run_8_checks.sh`、没动 git、没碰两张正本、
   没往 `d:/2/**` 写一个字节；索引脚本与该缓存都在 `%TEMP%/rm_index/`（**没进工程**）。

---

## 方法学（可复用，写给下一个会话）

- **判「原版那颗 TMP 的框」，`m_SizeDelta` 只在 `anchorMin == anchorMax` 时才是答案** ——
  否则要 `父宽 × 锚跨度 + sd`；被布局组/CSF 管的**根本没有固定框**。
  ⛔ 拿 `m_SizeDelta` 裸值去比（本次实测：28 处是伸锚偏移、6 处是模板位）会**静默抄错**。
- **一条命令拿到全树**：`D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/menu_dump.py <bundle> "<根名>" --depth N --no-sprite`
  —— 它**按名找根**时会把父链算进去（`--rt` 那一支在**跨文件**的祖先上会退回整屏假设，本次实测过一次、数不对，别用）。
- **要 prefab 字段原值**（不是布局后）：本件用的只读索引（`RectTransform/*.json` + `m_Father` 爬链）最省事，
  一次建索引 39,012 条、之后逐条查。
