# `G1` · `SOR24` 撤改名 + `A2020` 的 `desc` 注明

> 写手：`G1` 笔（第三十二会话）。**只碰了** `数据/游戏数据/card_stats.json` · `数据/卡牌翻译/zh_cards.json` ·
> `数据/游戏数据/cardface_fixes.json` + 重跑生成器（`cards_engine.json` 是产物）。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改两张正本 · ⛔ 未碰 `工具/**` 的源码。
> 判据来源 = `资料/普查产出_第三十二会话/R3_卡牌线施工单.md` §4 + 顺手发现 #1 + §2（用户 2026-10-15 拍板）。

---

## 一、改了哪几处

| # | 文件 | 现读行号 | 旧逐字 | 新逐字 | 为什么这一处要改 |
|---|---|---|---|---|---|
| 1 | `数据/游戏数据/card_stats.json` | **22371** | `"name": "Simulacrum Celestian",` | `"name": "Simulacrum Imperialis",` | ✅ **这是卡池 `name` 的来源**（`gen_cards_engine.py:34 SRC` → `:1563 name = norm_str(c.get("name"))`，现核）。不改则池名不变 |
| 2 | 同上 | **22388** | `"rarity": null,` | `"rarity": "epic",` | 🔴 **连带**：卡名回到 `Simulacrum Imperialis` 之后**宝石表查不到这张卡** ⇒ 不改则池里 `rarity` 变**空串**（详见 §二「为什么多改了一格」） |
| 3 | 同上 | 22396 | `"ocrName": "Simulacrum Celestian"` | **不改** | 那是 **PnP 卡面印的字**的 OCR 记录（`ocrSrc`/`face` 都指着 `Warpforge_30_Simulacrum-Celestian.png`），**不是我们的卡名**；`工具/check_pnp_cards.py:69` 的 `PNP_NAME_ALIAS`（引擎名 → PnP 印名）用的就是这条对，改它会把这张卡配错 |
| 4 | `数据/卡牌翻译/zh_cards.json` | **3851** | `"Simulacrum Celestian": {` | `"Simulacrum Imperialis": {` | 它是「英文名 → 中英」的字典，**键是卡名**；不改 ⇒ 孤立键（官方 `Card_Name` 兜不住的卡会印英文名） |
| 5 | `数据/游戏数据/cardface_fixes.json` | **15** | `"Simulacrum Celestian": "2026-09-15 手工加…"` | `"Simulacrum Imperialis": …` | `_manual_subtype_note` 是**说明键**，也按卡名做键 ⇒ 一起改（说明**正文一个字没动**） |
| 6 | 同上 | **200** | `"Simulacrum Celestian": "Pray: Give +1 [Attack] and +1 [Ranged] to all friendly units",` | 键改、**值逐字不动** | `desc` 列按卡名做键 |
| 7 | 同上 | **342** | `"Simulacrum Celestian": "Infantry"` | 键改、值不动 | `_manual_subtype` 列（手工覆盖列） |
| 8 | 同上 | **956** | `"Simulacrum Celestian": "祈祷：给予所有友方单位 +1 [攻击]和 +1 [远程]。"` | 键改、值不动 | `descZh` 列 |
| 9 | 同上 | **1216** | `"Simulacrum Celestian": "Infantry"` | 键改、值不动 | `subtype` 列 |
| 10 | 同上 | **22**（新增行） | —— | 新键 `_2026-10-15_SOR24撤改名与desc注明` | ⭐ **本任务要的 `desc` 注明**（SOR24 那一格） |
| 11 | 同上 | **23**（新增行） | —— | 新键 `_2026-10-15_A2020_EphraelStern非卡面原文` | ⭐ **`A2020` 的落法**（见 §一·b） |
| 12 | 同上 | **2**（就地订正） | `…那会让同一条规则两处并存）。",` | 同一句后追加 `🔴 2026-10-15 撤回（G1）：本条 ④ 那次改名当天即被推翻…` | 铁律 5：`AM` 那条历史记录里写着「④ 卡名 → `Simulacrum Celestian`」，不改会在文件顶部留一句**读起来像现状**的错话 |
| 13 | `数据/游戏数据/card_ids.json` | 664 | `"SOR24": "Simulacrum Imperialis",` | **不改** | 现读**本来就是** `Simulacrum Imperialis`（真值表照抄快照）⇒ 改名后它反而**对上**了 |
| 14 | `MyGame/Assets/RuleEngine/Resources/cards_engine.json` | —— | —— | —— | 生成物，**只通过重跑生成器改**（§二） |

**行尾**：改前逐文件量（**二进制读**）：`card_stats.json` CRLF 36003/36003 · `zh_cards.json` CRLF 4937/4937 ·
`cardface_fixes.json` CRLF 1932/1932 —— 全是 **CRLF**。改后复核 **36003/36003 · 4937/4937 · 1934/1934**（只多出新增的两行）⇒ **没翻行尾**。
`git diff --numstat` 逐文件：`card_stats.json` **2/2** · `zh_cards.json` **1/1** · `cardface_fixes.json` **13/6**（= 5 处键 5/5 + `AM` 那行 1/1 + 2 条新注释）· `cards_engine.json` **1/1**（单行 JSON）。

**写法**：三个文件一律**字节级单行替换**（`bytes.replace`，前后都打计数并 assert：
`card_stats` 名 1 处 / `zh_cards` 键 1 处 / `cardface_fixes` 键 **5 处**）—— ⛔ **没有** `json.dumps` 整篇重写
（`card_stats.json` 有手工缩进变体、`cardface_fixes.json` 有重复键）。每次写完立刻 `json.load` 验一遍。
改前副本：`d:/4/_tmp_view/g1_bak_1015/{card_stats,zh_cards,cardface_fixes}.json`。

### 一·b `A2020` 那条注明（`SOR29 Ephrael Stern`）写了什么

落在 `_manual_desc_note`（本文件承载 `desc` 层说明的**既有**注释字典，写法照抄它现有的 `_<日期>_<账>_<主题>` 键），
内容 = 用户指定的那句 + 三处判据（PnP 卡面亲读 / 官方 `Card_Description/SOR29` / OCR 源表都没有 `to an enemy troop`）+
**它是承重的**（`ruleprobe` 基线 `:686` 的那条 `deal` op 目标规格就是从这句解出来的）⇒ **⛔ 不许只删文本不管 op** +
**还没查清**的那一格（真值快照里这条 ability SO 的 target 字段）。判据全文指向 `R3_卡牌线施工单.md` §2。
⚠️ **本笔一个字都没改 `Ephrael Stern` 的 `desc` 值**（按 R3 的路 B）。

---

## 二、生成器重跑

```
python -I -X utf8 d:/4/Unity/工具/gen_cards_engine.py     →  rc=0   （跑了两次，输出同一份，幂等）
```
日志：`d:/4/_tmp_view/g1_gen_run.txt`（第一次）· `d:/4/_tmp_view/g1_gen_run2.txt`（改完注释后的复跑）

**卡池 diff（对【紧前一次生成】= 盘上那份 `md5 ca726e0854eb932b218b6edaf633c99b`）**：变了 **1 张**。

| id | 字段 | 旧 → 新 |
|---|---|---|
| `SOR24` | `rarity` | `""` → `"epic"` |

**md5**：`ca726e0854eb932b218b6edaf633c99b` → **`e7d2236238c14d99461fbd52951cec01`**
（张数 **1127** 不变 · id 集合逐张相同）

⚠️ **另注（口径要说清）**：简报给的起始 md5 是 `6e8c5786125d8dacf3d9575943f25500`，而我开工时盘上已是
`ca726e08…` —— **本会话期间另一个写手也在重跑生成器**（卡池 mtime `10:37:26 → 10:42:18`，md5 两次都变）。
好在：**那一次重跑已经吃到了我改的三份数据**（所以「紧前一次」那份里 `name` 已经是 `Simulacrum Imperialis`），
⇒ 上面这张表只剩 `rarity` 一格。**对 `HEAD`（`md5 9d5f4aff…`）**比：`SOR24` 是
`name Celestian→Imperialis`（本次）+ `rarity epic→epic`（不变）；同批另有 **9 张卡**被**别的线**改过
（`SOR39`/`TL44`/`TL66`/`TL69`/`UM44`/`UM68`/`UM72`/`UM73`/`af98d423…`）—— 逐张核过：**那 9 张在我改之前就已经变了**（`HEAD → 10:42 那份` 同样是这 10 张），⛔ 不是我改的。

**为什么多改了一格（`rarity`）** —— 这是**改名逼出来的连带**，走的是「先查清再动手」那条：
1. 稀有度的**权威来源是宝石表** `资料/卡牌数据表/卡牌宝石稀有度_0824.md`（`gen_cards_engine.py:33 RARITY_SRC` → `:1598 rarity_lookup(name)`），
   而那一行（`:698`）是按 **PnP 文件名**建的键：`| S | Simulacrum-Celestian.png | Simulacrum Celestian | 紫 | epic |`。
2. 卡名回到 `Simulacrum Imperialis` 后，**现算实测**：原名 ✗ / 归一化 ✗ / `difflib` 兜底 ✗（阈值 `0.88`，两者相似度 **0.667**）。
3. 而 `RARITY_UNLISTED` 里那条 `"Simulacrum Imperialis": "epic"` **已被 `AM` 当天删掉**（`gen_cards_engine.py:204` 的注释），
   源表这行 `rarity` 又是 `null` ⇒ 落到 `:1611 elif rarity not in RARITIES:` ⇒ **`still_missing` + `rarity = ""`**。
4. 🔴 **已实测，不是推的**：改名前那份池里 `SOR24.rarity` **确实是 `""`**（池子被别的写手重跑之后我读到的那份）。
5. 影响面**现核**：卡面底部那颗**稀有度宝石**（`CardView.RarityGem`）· **稀有度筛选**（`FilterPanelModel` / `DeckEditorState:393`）·
   **卡组构筑的复制上限**（`DeckRules.CopyLimit(c.Rarity)`）+ 卡池行的 `RarityTint`。
   ⚠️ **卡框档位不在其中** —— `CardArt.TierOf` 自 2026-09-27 起**恒返回 `MaxTier`**（本笔初稿写错过一次，**已就地订正**）。
6. 等效的另外两条落点（`RARITY_BY_FACTION` 补键 / 恢复 `RARITY_UNLISTED`）**都在 `工具/gen_cards_engine.py` 里**
   —— 那是**另一位写手的文件**，本笔按简报 ⛔ 不许碰 ⇒ 只在自己白名单内、**在同一个文件的另一条列**上补了这一格。
   🔴 **口径待调度台核**：那条守卫的注释里记着「用户 2026-10-15 拍板：⛔ 不改源表」（原话范围是 **`armor` 一族**，
   改它会拆掉家族判据不变式）。本笔动的是 `rarity`、**不动 `armor`**、**不动成员**，三条守卫全绿（§二末）。
   若不接受这一格，替代方案见上（两条都在 `.py` 里）—— **请裁**。

**生成器报告的三条守卫（两次跑都全绿）**：
```
卡面修正表：所有键都匹配上了 ✅            ← 说明我再改的 5 处 cardface_fixes 键【没有孤立键】
数值修正表：所有 29 个键都匹配上了 ✅
`armor` 一族：源表里 `armor` 有值的 6 行与预期一致 ✅   ← 见 §四·顺手发现（另一条线已预置本次改名的别名）
```

---

## 三、确认「链上每一处都同步了」

`grep -rn "Simulacrum Celestian"` **全仓**（排除 `Library/` · `_tmp_view/` · `__pycache__/` · `资料/普查产出_*/` · `资料/历史/`）**现读**：

| 现读位置 | 处置 | 为什么留着 |
|---|---|---|
| `数据/游戏数据/card_stats.json:22396` `"ocrName"` | 留 | PnP 卡面字的 OCR 记录（§一 #3） |
| `数据/游戏数据/card_stats_raw.json:8938` | 留 | 键是 **PnP 图路径**（`…/Warpforge_30_Simulacrum-Celestian.png`），值是那张图 OCR 出来的字 —— 是**记录**；全仓**无任何消费者**（grep 现核） |
| `数据/游戏数据/card_stats_report.md:193` | 留 | `merge_card_stats.py` 2026-08-16 生成的**旧报告** |
| `数据/游戏数据/community_cards.json:436,437` | 留 | 2026-08 的社区截图 OCR 输入件，**无消费者** |
| `数据/游戏数据/gems_rarity.json:2751` | 留 | ⚠️ **它不是生成器的输入**（`RARITY_SRC` 是那份 `.md`）；全仓**无消费者**（`check_prebuilt_decks.py` 只在注释里提过它）—— 属**遗留产物**，改它 = 改死文件 |
| `数据/游戏数据/官方本地化_en+zh-CN.json` | 留 | **原版官方表**（`Card_Name/SOR24` 官方就是这个词）—— 那是判据、不是我们的卡名 |
| `工具/check_pnp_cards.py:69` | 留 | `PNP_NAME_ALIAS`：键 = **引擎卡名**（现读已回到 `Simulacrum Imperialis`）、值 = **PnP 印名** ⇒ **改名后这条反而活了、且正确** |
| `工具/gen_cards_engine.py` 的 8 处（`:206,209,410,414,466,476,481,485`） | 留 | ⛔ 另一位写手的文件；其中 `:476`/`:485` 是**他们**为本次改名**预置的别名**（§四） |
| `工具/_gen_tsv2_28.py:42` · `工具/_oneoff_1015b/land2.py:212` · `land_1032.py:308` | 留 | 一次性脚本 / 落账脚本里的**引文**（含 `A2022` 那条待办原文） |
| `资料/**` 共 12 处（`卡牌数据源对账_0912.md` · `卡牌数据表/*0824.md` 三份 · `卡表核对_卡图提取/*` 四份 · `原版预组牌_核对.md` · `任务_卡组卡牌战斗.md`） | 留 | 都是**逐张开 PnP 卡图抄录的原始记录**（PnP 上印的就是这个词）/ 待办表里的引文 —— ⛔ 两张正本与历史记录本笔不许动 |

⇒ **链上（喂给生成器 / 运行时会读的）一处 `Simulacrum Celestian` 都不剩**；剩下的全是记录、官方表、别人的文件。

**同一轮复查「按卡名做键、运行时真会读的」其它文件（都**不用改**，且**改名后反而对上**）**：

| 文件 | 键是 | 现读 | 结论 |
|---|---|---|---|
| `数据/游戏数据/card_ids.json:664` | id → 卡名 | `SOR24: Simulacrum Imperialis` | ✅ 已对 |
| `MyGame/Assets/CardPresentation/Resources/ability_anim_durations.json:274` · `数据/游戏数据/同名文件:12581` | **按卡名查**（`AbilityAnimDurations.cs:119 _byName[it.name]`） | `Simulacrum Imperialis` | ✅ 不用改 —— 改名**把它从孤立里救回来** |
| `Resources/voice_lines.json:9223` · `数据/游戏数据/card_vfx_by_card.json` | 前者外层按 `id`；后者的 `assetName` = **原版资产名** | —— | ✅ 不用改 |
| `数据/本地化/i18n/zh_CN.csv:2251,3747,4941,6135` | 键 = 英文卡名（内部命名基准） | `Simulacrum Imperialis` | ✅ 已对 |
| `Resources/card_icon_plan.json` | 无 `SOR24` 卡名键（现核） | —— | ✅ 无孤立键；且它是生成物、⛔ 另一位写手 |
| `prebuilt_decks_full.json` / `decklists.json` | **按 id**（`SOR24`） | —— | ✅ 不用改 |
| `cards.json` · `decks.json` · `card_index.json` · `texture_index.json` · `anim_address_map.json` | 遗留产物 / **原版资产名索引** | `Simulacrum Imperialis` | ✅ 不用改（后两者是**证据**：原版资产名就是 Imperialis） |
| `.cs` | —— | 只有 `EffectText.cs` 两条 doc 注释提到 `Simulacrum Bearer`（**另一张卡**） | ✅ 一处都不用改（与简报一致） |

---

## 四、没查清 / 没做的

1. 🔴 **`rarity` 那一格的口径**：见 §二 第 6 点。**要调度台裁**（用户 2026-10-15 拍板「⛔ 不改源表」的范围
   是不是只限 `armor` 一族）。若不接受，本笔这一格可**一行回滚**（`card_stats.json:22388` 改回 `null`），
   替代落点 = `gen_cards_engine.py` 的 `RARITY_BY_FACTION` / `RARITY_UNLISTED`（那份文件的主人做）。
2. ⚠️ **中文卡名没动，且本笔不判它**：池里 `SOR24.nameZh` 现读 = **`天界模拟体`**（来自官方 `Card_Name/SOR24`，
   按 **id** 取 ⇒ 与英文名那条链**不相干**）；`zh_cards.json` 那个键的值是**我们自译**的 `帝皇圣像`。
   ⇒ 中文档下卡面印 `天界模拟体`、英文档下印 `Simulacrum Imperialis`（`CardText.Name(Name, NameZh)`）。
   **要不要把中文也换回 `帝皇圣像`？—— 简报没交代，本笔 ⛔ 不自己发明口径，如实报上来。**
3. ⚠️ **`card_stats.json` 里 `SOR24` 的 `art` / `voice` / `decks` 三列仍带旧写法**（`…_Simulacrum Imperialis.png` /
   `SOR_SimulacrumImperialis`）—— 它们**本来就是** `Imperialis`（原版资产名），改名后从「不一致」变回「一致」，✅ 无需处理。
   `desc` 那一列仍是 OCR 原文 `Pray: Give +1 Armor and +1 Attack to all friendly units.`（被 `cardface_fixes.desc` 盖掉）—— 同 `AM`/`AH` 的先例，**没动**。
4. **`A2020` 的根问题仍开着**：原版那一击到底打谁 —— 真值快照里 ability SO 的 target 字段**本笔没查**
   （R3 §2「没查清的」那一格）。本笔只落了「保留文本 + 注明」这一半。
5. **本笔没跑任何 Unity 自检 / 没跑 `ruleprobe`**（按简报红线）。⇒ 上面「卡面画出来对不对」是**按源码推的**，不是渲出来的。

---

## 顺手发现

1. 🟢 **另一条线已经预置了本次改名的别名**（`工具/gen_cards_engine.py:476-486`，当天新立的守卫 `OCR_ARMOR_FAMILY`，账 `A2043`）：
   `OCR_ARMOR_FAMILY` 按 **(阵营, 卡名)** 比 6 行，且有一条 `OCR_ARMOR_FAMILY_ALIAS`
   （`("Sororitas","Simulacrum Imperialis") → ("Sororitas","Simulacrum Celestian")`），注释写着「源表里 `Sororitas` 那一行的卡名**可能正被另一个写手改**」。
   ⇒ 那写手与本次改名**是知情的、不冲突**；`armor` 一族守卫两次跑都绿即证。（⚠️ 但也说明：**同一个文件我确实只读没写**。）
2. 🔴 **本会话有并发重跑**：卡池在 `10:37:26` 与 `10:42:18` 各被重跑一次（md5 `6e8c5786… → ca726e08… → e7d22362…`）。
   本笔的两次跑与那两次**没有互相破坏**（最终幂等、rc=0），但如果下个会话要拿 md5 当基线，**要现读、别用简报里那个数**。
3. ⚠️ **`ABILITY_ANIM_DURATIONS` 的小缺口**：`数据/游戏数据/ability_anim_durations.json` 里有 `SOR24` 的条目、
   全表只 62 条（覆盖面本来就窄）—— 本笔只确认**键是卡名、现读匹配**，**没验**它在运行时真被查到（要真机/自检）。
4. 🟡 **`gems_rarity.json` 是死文件**：它看起来像「宝石表」，但生成器读的是 `资料/卡牌数据表/卡牌宝石稀有度_0824.md`
   （`RARITY_SRC`），全仓**没有任何代码读这个 json**。⇒ 它容易误导「改了它就等于改了稀有度」——
   `R3` §1 第 2 点当时也是引它作判据的（结论**恰好**不错，因为那份 `.md` 与它同值）。
   建议由调度台决定**是删是标注**（本笔 ⛔ 不擅自删文件）。
5. 🟡 **`cardface_fixes.json` 的「说明键」有两套形状并存**：`_manual_subtype_note` 是**「卡名 → 说明」**，
   `_manual_desc_note` 是**「`_<日期>_<账>_<主题>` → 说明」**。本笔按后者（它就是这个 `desc` 层的注释字典），
   但用户原话里「给 `Ephrael Stern` **那条**加一句注明」更像前者的形状 —— **若结算口径要「卡名键」，改一行即可**（如实报）。
