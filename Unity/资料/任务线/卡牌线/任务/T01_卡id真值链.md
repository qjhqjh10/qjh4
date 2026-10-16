# T01 · 卡 id 真值链 —— 每张卡的 id / 立绘 / 预组引用都走**原版真值**，不再「按卡名猜」

> **状态**：`✗ 未做 11` · `◐ 部分做 1`（**主体已做**：`A1758` 2026-10-13 已把全池 id 换成真值 ⇒ 本件是**它剩下的尾巴**）
> 🔴 **2026-10-16 现核（执行代理 `A5`）—— 见下面「## 二·〇」那一节**：`A2068` **已做**（立绘改走真值链）；
> `A1779` / `A2074` / `A1848` / `A1925` / `A2069` / `A1880` **已闭**（判据现核）；
> `A1869` 缺口 **171 → 118**（剩下那 118 = 快照之后新增、⛔ 等远端）；`A2053` **结案被新证据动摇**（**未动**，需拍板）。
> 报告 → `资料/普查产出_1016并行/卡牌线T01_报告.md`
> **关联 A 编号**：`A2068` `A2074` `A2075` `A2072` `A2069` `A1880` `A1869` `A1848` `A1925` `A2053` `A1779`（✗）· `A2052`（◐）
> **判据全集** → `资料/普查产出_第三十三会话/R9_卡牌id真值挖掘.md`（A2074/A2075/A2072）· `R1_远端包数据使用现状.md` §4（A2068）·
> `资料/普查产出_第三十会话/乙档六笔_判据查证.md` §A1779 · `资料/普查产出_第二十八会话/` 的 `W3`（A1848）/`W9`（A1869）/`A1759_逐条对账.md`（A1880）·
> `资料/普查产出_第三十二会话/D2_18条红诊断.md` 顺手发现 #1/#3（A2052/A2053）· `DIAG_battle六条红.md`（A1925）

---

## 一、要做什么

**卡 id 是这一整条线的钥匙**：它错了，立绘、语音、图标、推进器、预组牌、对账**全部跟着错**（而且是静默的）。
做完之后：**每一张卡的 id / 立绘 / 预组引用都能追到原版的一个具体对象**，「按卡名猜」这条兜底路**用不上了**。

⚠️ **注意它与 `A1758` 的关系**：`A1758` 已落地（真值 935 条换进 `card_ids.json`），**本件是它没覆盖到的三块**：
① **快照年代之后**那批卡（118 张 SW/新 UM/新 DA）· ② **立绘那一跳**（仍按卡名找）· ③ **卡名 ↔ id 的系统性错配**。

---

## 二、判据（逐条带出处）

| A | 要做的事 | 判据（出处） | 做得到吗 |
|---|---|---|---|
| `A2068` | **「卡 → 立绘」改走真值链** —— `工具/import_original_art.py` 现在**按卡名在 bundle 里找图**，而 `cardImageReference` 的 **1,171 条 AssetGUID 已解成 assetName**、就在 `数据/游戏数据/card_vfx_by_card.json` 的 `_assetRefs` 里 | `普查产出_第三十三会话/R1_远端包数据使用现状.md` §4 | ✅ **判据齐、可立刻做** —— 特效与语音**都已改走真值链**，只有立绘没改 |
| `A1779` | **11 条督军 join 错配**：`AM3`/`AM66` **一直在我们池里**（`AM_Commissar_Elan` / `AM_Hektor_Thenmann`），只是卡名与真值 `cardName` 不一致 ⇒ `pick_id`（`工具/gen_cards_engine.py:1020`）**按名换 id 落空** ⇒ 走自造 id ⇒ **两头都查不到**。**真缺的只有 3 条** = `Marneus Calgar`/`Uriel Ventris`/`Sylar Hexscorn` 的 **`(tutorial)` 专用副本 SO**（我们一律只收正式那张） | `普查产出_第三十会话/乙档六笔_判据查证.md` §A1779 | ✅ **判据齐、⛔ 未动手** —— 最小方向 = 把**已有的**官方两级表 `load_official_cardname().by_en` 接进 `:1020` 作第二路 ⇒ **零数据改动救 5 条**（`DA1/2/3` · `GSC66` · `AM66`）+ 3 条显式别名（`AM3` / `BL36` 拼错 / `GSC3`）。⚠️ **`AM3` 数值冲突**（真值 `35/0` vs **PnP 与 `card_stats.json` 都是 `30/护甲 2`**）⇒ 按**铁律 7 以 PnP 成品卡图为尺**；⚠️ `AM3` 卡名要不要换成官方 `Commissar Denkler` 是**口径决定** |
| `A2074` | **督军 id 真值**（本机可解）：钥匙 = **卡组 SO 的 `deckAvatar` PPtr** → `cosmeticsso` 的 **`Avatar_<阵营>_<督军名>`**。**硬结论**：`SW1`=Logan Grimnar · `SW2`=Njal Stormcaller · `SW3`=Ragnar Blackmane（**我们错放成 SW1/SW4/SW9 = 照 PnP 文件号排的**）· `DA75`=Master Zacharial（确证）· `cardArmy 130 = SpaceWolves` | `R9_卡牌id真值挖掘.md` §二 · 真值表 = `普查产出_第三十三会话/_heroId_truth.json` | ◐ **只有一部分能做**：🔴 `A2078` 已订正 —— 那「9 条不一致」里**只有 3 条是真错**；`GOF75`/`UM65`/`ASH76` **不是卡名错**，是**原版 presale/异画【变体号】占了那些数字** ⇒ **改法需要原版完整编号表**（**没拿到**）。⚠️ 另 2 条不算错（`SAU36` 原版与 PnP 拼写不一致 · `SOR65` 立绘用职业名）、1 条待复核（`TL74`） |
| `A2053` | `GOF20` **名字对不上**：`card_ids.json` 写 `Veteran Stormboy`、池里是 **`Veteran Flyboy`**；PnP 只有 `Orks/3部队/Warpforge_31_Veteran-Stormboy.png`、**全池没有 `Veteran Stormboy`** | `普查产出_第三十二会话/D2_18条红诊断.md` 顺手发现 #3 | ✅ 同上（与 `A1779` 同源）—— 但它**同时是预组 `OrksDeck2` `size:28/30`（`complete=false`）的直接原因** ⇒ 与 `T06` 有交集 |
| `A1848` | 🔴 **池子 id ↔ `数据/游戏数据/card_ids.json` 不一致**：`card_ids.json` 写 `DA1 = Azrael, Grand Master`，而**池里是 `DA_Azrael`**（同族 `DA2`/`DA3`/`TAU32`/`TAU38`/`BL36`…） | `普查产出_第二十八会话/W3_相关卡接真值.md` | ⚠️ **根因没查清**（`gen_cards_engine.py` 的 id 取值段**没人读全**），**影响面可能远超本条** ⇒ **先查再动** |
| `A1880` | **真值里 256 张卡的 `uniqueId` 是 GUID**（不是 `XXXnn`）⇒ **纯按 id 对账会整片落空**；本件靠「卡名唯一」补配了 108 张 ⇒ **建议落成通用取卡规则** | `普查产出_第二十八会话/A1759_逐条对账.md` 顺手发现 3 | ✅ 规则已现成（`card_id_truth_index.json` 的 `by_official_en`/`by_tail`/`by_name` 三路） |
| `A1869` | **171 张接不上真值**（真值快照 2025-07-30 **比现池旧**：整个 SpaceWolves 62 张 + `DA75..DA86` 等）⇒ 现在记 `-1`、**不拦**、运行时只出一行警告 | `普查产出_第二十八会话/W9_卡组分流按原版.md` | ⚠️ 「要堵得**先做 `A1800`**」—— **`A1800` 已落地**（2026-10-15）⇒ **前置已解**、可以动 |
| `A2069` | **`A2052` 的前提是错的** —— 那 113 个 GUID **就是原版的 `uniqueId`**（快照里逐个按同 `uniqueId` 找得到、`m_Name` = **`Z-NC <阵营>-<名字>`**、**`cardShortId = 0`**）⇒ **原版对这批卡本来就只有 GUID 一个 id** | 本轮现算（`cards_engine.json` × `cards_1195_2022时代.json` 按 `uniqueId` join） | ✅ 要做的是**订正 `A2052`** + 看是否牵动 `A970` |
| `A2052` | **池里 113 张卡的 id 仍是 32 位 GUID**（`Skyborne Deployment` = `e59ac9e4…`，光 SaimHann 就 8 张） | `D2_18条红诊断.md` 顺手发现 #1 | ◐ **前提已被 `A2069` 推翻**（不是「真值没覆盖」）—— **仍成立的那半** = 它们是 **NC（不可构筑）卡** ⇒ `DeckRules.CannotAddToDeck` 会摘掉引用它们的夹具（**症状是真的**） |
| `A1925` | 官方 `Card_Name/*` **按 id 建键**（1065 条），而 `UM_Master_of_the_Fleet` 这个 id **与名字两边都对不上官方键** | `普查产出_第二十八会话/DIAG_battle六条红.md` | 与 `A970` 同源 |
| `A2075` | **普通卡的 id：本机【拿不到】，边界已逐条排除（8 条证据）**：① 84 个客户端 bundle 全扫、带 `uniqueId`+`cardName` 的 MB **0 命中** ② `globalgamemanagers`/`level0`/`resources.assets`/`sharedassets*` 同样 **0** ③ 掉落表与卡组 `deckHero` 的 PPtr 落不到本地 ④ `Avatar_*` SO 只带自己的 GUID ⑤ `catalog.bin` 键全 GUID ⑥ CCD 主机活着但 URL 要三个 UUID、**代码无硬编码** ⑦ 旧 profile 日志无 CCD 参数 ⑧ PnP 卡表**无 id 列** | `R9_卡牌id真值挖掘.md` §三/§四/§五 | ⛔ **等远端**（与 `A897`/`A970` 同源）—— **唯一判据 = 一份更新的 `allcards`**。➕ 已对出的**块结构**：SW1-3 督军 / SW4-39 部队 / SW40-65 计策 / SW66-68 防御（**块内顺序仍未知**）；**原版真的用了而我们没有的 id 只有 `DA65`–`DA74` 这 10 个**（PnP 里没有 ⇒ 数字版专有） |
| `A2072` | **那 118 张（SW 68 · UM 38 · DA 12）是【2025-07-30 之后】才进游戏的，`id` 是【我们自己排的】**（按 PnP 顺序：`DA75↔01_Master-Zacharial`、`DA76↔02`… 严丝合缝） | `待办判据_第三十三会话.md` 末节「SW / 新 UM / 新 DA 那 118 张」 | ⛔ **等远端**（同 `A897`/`A970`）。➕ **另发现缺口**：DA 编号 **61 → 75** 跳过 **65–74（10 个号）**，**两边都没有** |

---

## 二·〇 🔴 2026-10-16 逐条现核（执行代理 `A5`）—— 哪些已闭 · 哪些**订正**

> ⚠️ **本节是现场核出来的、按铁律 5 保留痕迹**；上面那张表的状态格是**当天之前**的读法，以本节为准。
> 全文与命令 → `资料/普查产出_1016并行/卡牌线T01_报告.md`。

| A | 2026-10-16 现核结论 | 判据（现读） |
|---|---|---|
| **`A2068`** | ✅ **已做** —— `import_original_art.py` 加了 ⓪·5「真值链」那一层（读 `card_vfx_by_card.json` 的 `byCard[id]._assetRefs`，`assetKind=="art"`），**卡名那一支降为兜底** | 现读 **1,009 张可用 / 0 张盘上缺图**；与卡名一路逐张比 **1,006 张相同**，余 3 张全是真值侧脏值；产物 `jobs=1127 / 配不上 0 / 缺 rect 0`（**改前改后逐张相同**） |
| **`A1779`** | ✅ **已闭**（**不是「未动手」** —— 真值索引那一层（`pick_truth_id`）已经把 8 条全接上了）。🔴 **另订正它那句数值冲突**：原文写「PnP 卡图与 `card_stats.json` 都是 `30 / 护甲 2`」—— **PnP 那半不成立**：亲开 `Astra Militarum/1督军/Warpforge_01_Commissar-Denkler.png` 读到的是一排两个圆（**左红剑 2 · 右紫枪 2**）+ 最右绿框 **30**，**全图没有银盾**（= 护甲 0）；`card_stats.json` 那一格的 `armor: 2` 是**紫圈（远程）值被 OCR 读进了 `armor`**（`A2043` 那族）⇒ 现池 `attack2/ranged2/health30/armour0` **与 PnP 逐格吻合**，**一个字都不用改** | 池内 `AM3`=Commissar Denkler（真值 `uniqueId`）· 真值 `maxHealth 35`（2022 快照，**比 PnP 旧** ⇒ 铁律 7 以 PnP 为尺）· PnP 卡图亲读 |
| **`A2074`** | ✅ **已闭（0 条真错）** —— `R9 §2·1` 那张「9 条不一致」表**整体被推翻**：逐条拿**原版侧两个独立来源**核（卡 SO 的 `cardName`/`m_Name` + 官方 `Card_Name/<id>`）—— `GOF75`=`Warboss Gordrang` · `UM65`=`Chaplain Letharius` · `ASH76`=`Lhykhis` · `SAU36`=`Nemesor Zahndrekh` · `SOR65`=`Celestian Sacresant Aveline`（**5/5 与现池逐字相同**）；`SW2`/`SW3`/`AM3` 三条**已改好**（现池 = Njal Stormcaller / Ragnar Blackmane / Commissar Denkler）⇒ **一条都不剩**。🔴 `_heroId_truth.json` 那张 `deckAvatar` 表**是可疑的那一侧**（TL74 是 63 条里**唯一**指向非 `Warlord_*` 的名字） | `cards_engine.json` 现读 · `官方本地化_en+zh-CN.json` 的 `Card_Name/*` · `cards_1195_2022时代.json` 的 `cardName`/`m_Name`/`cardType` |
| **`A2074` 的 `TL74`（原「待复核」）** | ✅ **已解** —— 原版自己的 `Card_Name/TL74` = **`Terror of Vardenghast`** / 范德加斯特的恐怖；真值卡 SO 同：`m_Name = Leviathan_war_Terror of Vardenghast`、`cardType = 10`（督军）、`cardShortId = 368` ⇒ **现池 TL74 = `Terror of Vardenghast` 是对的**；`Neurogaunt` 其实是 **`TL66`**（`Card_Name/TL66`） | 同上一行 |
| **`A2053`** | 🔴 **结案被新证据动摇 ⇒ 本批【没动】（需拍板）** —— 2026-09-16 那条「**别再翻案**」（`gen_cards_engine.py` 的 `STAT_FIXES` 段）站的是「`Veteran Flyboy` 有四条**独立于 PnP** 的游戏侧证据」。但那四条里 **`zh_CN.csv` 那条当天就被证伪**（它由 `sync_from_d2.py` 从**我们自己的 Godot 复刻** `d:/warpforge/data/i18n` 同步而来，`gen_cards_engine.py` 的 2026-10-18 更正已写死），而**后到的两条原版侧硬证据都站 `Veteran Stormboy`**：① 真值卡 SO `cardName`/`m_Name`（`20 Goff-Veteran Stormboy`）② 官方 `Card_Name/GOF20` ③ 语音 `VO_Goff_Veteran Stormboy - …ogg`。⚠️ 第 4 条「抽卡包 `Flyboyz`」现读 **`cardIds=['GOF6','GOF56','GOF46']`、不含 `GOF20`** ⇒ 与这张卡**无关**。**具体代价**：`card_ids.json` 的 `GOF20 → Veteran Stormboy` 与现池名对不上 ⇒ `gen_prebuilt_decks.py` 的 `match()` 判 **`miss-known`** ⇒ **`OrksDeck2` 掉 2 张（`size:28/30`）** | `d:/2/新解包资源/assets_full/bundle_orksgoffcardassets_assets_all/{Sprite,AudioClip}/` · 官方 `Card_Name/GOF20` · `bundle_draftpacks_assets_all/MonoBehaviour/Flyboyz.json` · `工具/gen_prebuilt_decks.py:218` |
| **`A1848`** | ✅ **已闭** —— 它记的「`card_ids.json` 写 `DA1 = Azrael, Grand Master`、池里是 `DA_Azrael`」是 **`A1758` 之前的读数**；现读池内 **`DA1` = `Azrael`**、`DA2`/`DA3`/`TAU32`/`TAU38`/`BL36` 同形（**全是真值 id**）。根因就是「旧 id 是自建的」，已由 `A1758`（2026-10-13）+ P1 相消掉 | `cards_engine.json` 现读 |
| **`A1925`** | ✅ **已闭** —— 官方表里**键就是那个 GUID**：`Card_Name/9238c27730a0c424288d46ed0f4a742c` = `Master of the Fleet` / 舰队之主 ⇒ 现池那张（id = 同 GUID）**两边都对得上**。⚠️ 「官方 `Card_Name/*` 按 id 建键」这条本身成立（现读 **1,065 键**，其中 **124 键是 32 位 GUID**） | `官方本地化_en+zh-CN.json` |
| **`A1869`** | ◐ **缺口收窄 171 → 118** —— 现读「池内 id 不在真值快照」的正好 **118 张**（SpaceWolves 68 / Ultramarines 38 / DarkAngels 12）。⚠️ **剩下这 118 条不是「没做」**，是快照 2025-07-30 之后新增的 ⇒ 与 `A2075`/`A2072` 同一条 ⛔ **等远端** | `cards_engine.json` × `cards_1195_2022时代.json` 现读 |
| **`A2069` / `A2052`** | ✅ **订正已成立**（上面 `A2069` 行说的方向对）：池内 **113 张 GUID 形 id** 就是**原版自己的 `uniqueId`**（`cardShortId = 0` 的 NC 卡、官方 `Card_Name` 也建在该 GUID 键上）⇒ **不是「真值没覆盖」**。仍成立的那半：它们是 NC 卡，`DeckRules.CannotAddToDeck` 会摘掉引用它们的夹具 | 现读 113 张 · `官方本地化_en+zh-CN.json` |
| **`A1880`** | ✅ **规则在**（`card_id_truth_index.json` 的 `by_official_en`/`by_tail`/`by_name` 三路，`pick_truth_id` 就在用）；⚠️ **仍留一笔**：`by_name` 继承真值 `cardName` 的**陈旧值**（`card_truth_index` 是生成物、生成器 `工具/gen_card_id_truth_index.py` **不在本批白名单内**）⇒ 接线清单里记了 | `资料/普查产出_1016战斗判据/卡id统一化_报告.md` §六·1 |

**已做的（供对照，⛔ 别重做）**：✅ `A1758`（2026-10-13 第二十六会话）全池 id 换真值 —— 详见「二、已完成」那一行 ·
✅ `A1800`（187 个名样式 id 的 join 规则）· ✅ `A1782`（`ocrName` 换回真值）· ✅ `A1778`（10 个 id 缺口）· ✅ `A2078`/`A2079`/`A2076`（三条**订正知识**，见 `任务总表.md` 二）。

---

## 三、我们现状

| 层 | 现读 | 出处 |
|---|---|---|
| **真值快照** | `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（原版 `allcards` 包 · **1,195 个 `RawCardScript`** · Unity **2022.3.38f1** / 缓存 **2025-07-30**） | `card_id_truth_index.json` 头部 |
| **id 映射表** | `数据/游戏数据/card_ids.json` —— **本会话现读**：`mapping` **1,310** 条、其中 **GUID 形键 259** 条（= 原版那 259 张 GUID 卡**现在已经进表了**）。⚠️ 判据件 `第三十三会话 §A1800` 里写的「`mapping` 1058 键 · **GUID 形 0**」是 **`A1758` 整表重建【之前】的旧读数**，⛔ 别再引用 | 本会话现读 ＋ `普查产出_第三十会话/乙档六笔_判据查证.md` §A1800 |
| **join 索引** | `数据/游戏数据/card_id_truth_index.json`（`by_official_en` / `by_tail` / `by_name` 三路）· 生成器 = `工具/gen_card_id_truth_index.py` · 消费者 = `工具/gen_cards_engine.py` | 同左 |
| **卡池（生成物）** | `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` —— **张数一律 `count` 现读**（2026-10-15 现读 = **1127**） | 本会话现读 |
| **立绘那一跳** | `工具/import_original_art.py` **按卡名找图**（这就是 `A2068`） | `R1_远端包数据使用现状.md` §4 |
| **督军那一层** | `_heroId_truth.json` 已有真值；**但 SW 的编号体系与原版冲突**（我们照 PnP 文件号排） | `R9_卡牌id真值挖掘.md` §二 |

---

## 四、怎么改（**按可服务性排序**；一条一条来，⛔ 别一次改整列）

1. ✅ **`A2068` 立绘改走真值链 —— 2026-10-16 已做**（`import_original_art.py`）。落地形状与实测读数见 §二·〇；
   一句话：新增 **⓪·5 真值链**那一层（读 `card_vfx_by_card.json` 的 `byCard[id]._assetRefs`，`assetKind=="art"`），
   **卡名那一支降为兜底**；优先序 = **⓪ `ART_ID_ALIAS` → ⓪·5 真值链 → ① 卡名子串 → ② 相似度**
   （⓪ 必须在 ⓪·5 之前：`EC56`/`EC60` 两条是**开 PnP 亲核过、故意与真值快照相反**的，`A1770`）。
   ⚠️ 已重跑核对：产物 **`jobs=1127 / 配不上 0 / 缺 rect 0`**，且与改前**逐张相同**（0 张立绘变化）。
2. ✅ **`A1779` —— 2026-10-16 现核：已闭，不必再动。** 这一条当时列的「最小方向」是**另一条路**；实际落地的是**更好的那条** ——
   `build()` 里 `pick_truth_id()`（真值索引 `card_id_truth_index.json`，三条路 `by_official_en`/`by_tail`/`by_name`）
   **排在老的 `pick_id()` 之前**（`工具/gen_cards_engine.py:1780` 那一带）⇒ 那 8 条**全接上了**，
   现池 `AM3`/`AM66`/`BL36`/`DA1`/`DA2`/`DA3`/`GSC3`/`GSC66` **8/8 都是真值 id**。**零数据改动**。
   ⚠️ **`AM3` 的数值也不用动**（现池与 PnP 逐格吻合，见 §六·6 —— 那条「冲突」是**记录写错了**）。
3. ✅ **`A2053` + `A1848` + `A1925` 三件 —— 2026-10-16 现核**：`A1848`/`A1925` **已闭**（见 §二·〇）；
   🔴 **`A2053` 是唯一还开着的，而且需要拍板**（2026-09-16 那条「别再翻案」的**判据基础已被后到的证据动摇**，
   但要改就得动 `cardface_fixes.json` + `zh_cards.json`，**都在本线白名单外**）⇒ 详见 §二·〇 与
   `资料/普查产出_1016并行/卡牌线T01_报告.md` 的「接线清单」。
4. **`A1880` + `A1869` 的「通用取卡规则」**：GUID 形 `uniqueId` 走 `by_official_en`；前置 `A1800` **已落地** ⇒ 可以动。
5. ✅ **`A2069` → 订正 `A2052`** —— 订正已成立（见 §二·〇）。⚠️ `A970` 那一条**没被本批牵动**（它管的是另一族）。
6. ✅ **`A2074` 督军那 3 条真错 —— 2026-10-16 现核：已全改好，且另 6 条也已逐条证伪**（见 §二·〇）⇒ 本条**无待办**。
   ⚠️ 原版完整编号表**仍然没拿到** —— 它现在是 `A2072`/`A2075`（SW 全族块内顺序）**唯一还缺**的东西，⛔ 别在解析层猜。
7. **`A2075` / `A2072` 两条 ⛔ 等远端** —— **不要**在解析层猜 id（本线红线）。

---

## 五、做完了怎么自检

**现在（只做不验口径）**：
- 改 `.cs` / 生成器后**只跑秒级类型检查**：`bash d:/4/Unity/工具/typecheck.sh`；
- **生成器自带的口径检查**可以跑（`python -I -X utf8 工具/gen_cards_engine.py --check` —— 它是**复刻的输入**、不是验收）：
  判据 = ① **`count` 前后差值 = 真新增数**（`A1783` 定的验收判据）② **0 重 id** ③ 插图 **N/N 配得上 · 缺 rect 0**。
- ⛔ 不跑 `RuleEngineTest.Run` / `BattleScene.Run`（用户 2026-10-15 口径）。

**本件 2026-10-16 实跑的读数**（`A2068` 那一笔）：

- 全池 **1,127 张** ⇒ `import_original_art.py --check`：**配上 1127 / 配不上 0 / 缺 sprite rect 0**（真值链 984 + 卡名一路 143）；
  `Art/cards` 现读 **缺图 0**；
- **改前改后逐张比对 = 0 张立绘变化**（这是本笔最该记的读数：真值链不是「换了画」，是「把 1,009 张的画**追到了原版对象**」）；
- 回归口：`工具/collect_unmatched_art.py` 读的那行正则 `插图：配上 (\d+) 张，配不上 (\d+) 张，缺 sprite rect (\d+) 张`
  **仍然命中**（那句后面只多了一段括注）。

**将来统一验收**：进游戏看**立绘有没有配错**（尤其同名跨阵营那 3 组：`Terminator Champion`/`Maulerfiend`/`Terminator`）、
看**预组页每一副都能开局**（`A2053` 那一族的结果）。

---

## 六、还差什么 / 判不准的（⛔ 不许猜）

| # | 缺什么 | 现状 |
|---|---|---|
| 1 | **原版完整编号表**（presale / 异画**变体号**占了哪些数字） | **没拿到** —— 🔴 **2026-10-16 订正：这一条原来写「⇒ `A2074` 那 3 条真错改不了」，前半句仍成立、后半句不成立了** —— 那「9 条」**已逐条核完、0 条真错**（`GOF75`/`UM65`/`ASH76` 的现池名与**原版卡 SO + 官方 `Card_Name` 逐字相同**，`A2078` 的「变体号」说法是对**另一件事**（`_heroId_truth.json` 那张表）的解释，不是卡名错）⇒ 这张编号表现在**只卡 `A2072`/`A2075`**（SW 全族块内顺序） |
| 2 | **更新的 `allcards`** | 判据在远端；快照是 2022 时代 / 2025-07-30 ⇒ `A2075`/`A2072` 卡在这 |
| 3 | **`gen_cards_engine.py` 的 id 取值段**（`pick_id` 一族） | **没人读全** ⇒ `A1848` 的根因未定；本件**动之前必须先读全** |
| 4 | `DA65`–`DA74` 那 10 个号是什么 | **两边都没有**（PnP 里没有、我们池里没有）—— 原版数字版专有；**搜过** = PnP 五类目录 + 我们池 + 快照 |
| 5 | 🔴 **2026-10-16 订正：这条不再是「待复核」—— 已解**（见 §二·〇）：原版 `Card_Name/TL74` + 真值卡 SO 两边都说 `Terror of Vardenghast` ⇒ **现池是对的**；`Neurogaunt` 其实是 `TL66` | ✅ 判据齐 |
| 6 | 🔴 **2026-10-16 订正：这条原来写「PnP = `30/护甲 2`」—— PnP 那半是错的**。亲开 `Astra Militarum/1督军/Warpforge_01_Commissar-Denkler.png`：左红剑 **2** · 右紫枪 **2** · 绿框血 **30**，**没有银盾**（护甲 0）；`card_stats.json` 的 `armor: 2` 是**紫圈（远程）值**（`A2043` 那族）。⇒ 现池 `2/2/30/0` **与 PnP 逐格吻合**；真值 `maxHealth 35` 是 2022 快照值、**比 PnP 旧** ⇒ 铁律 7 不采它 | ✅ **已解，无待办** |
