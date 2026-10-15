# `G3` · 稀有度这一层（`SOR24` 空串回归）+ 孤立键守卫

> 写手：`G3` 笔（第三十二会话）。**只碰了三处**：`工具/gen_cards_engine.py` ·
> `数据/游戏数据/card_stats.json:22388` 一格 · 产物 `cards_engine.json`（**只经重跑生成器**）。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改两张正本 · ⛔ 未碰 `cardface_fixes.json` / `zh_cards.json` / `gen_icon_plan.py`。
> 开工时**现读**卡池 md5 = `e7d2236238c14d99461fbd52951cec01`（1127 张）。
> 全部产出/日志：`d:/4/_tmp_view/g3_run{1,2,3}.txt` · `g3_demo_D{1,2,3}.txt` · 补丁脚本 `g3_patch.py` · 演示 `g3_demo.py` ·
> 改前副本 `d:/4/_tmp_view/g3_bak/`。

---

## 一、根因链（现核）

**宝石表** `资料/卡牌数据表/卡牌宝石稀有度_0824.md`（= `gen_cards_engine.py:33 RARITY_SRC`，**生成器唯一的稀有度输入**）：

- 表头自述「2026-08-24 **vision 全量宝石扫描** 1118/1118」，列 = `批次 | 文件名(PnP) | 卡名 | 宝石色 | 稀有度`。
- `load_rarity():635-643` 收的是 **`cells[2]`（卡名）→ `cells[4]`（稀有度）**；现读 **1118 数据行 → 1113 个键**
  （差的 5 个是**卡名重复**，后写的赢 —— 与 `:95` 那句「5 个卡名重复、其中 3 个稀有度不同」吻合，不是新问题）。
- 🔴 **建键用的是【PnP 卡面写法】**（文件名那一列就是 `Warpforge_NN_<卡名>.png`）⇒ 我们池里那些
  「用了**美术资产名**」的卡，**按现名查必然落空**。SOR24 就是这一格：
  - 宝石表 `:698` = `S | Simulacrum-Celestian.png | Simulacrum Celestian | 紫 | epic`
  - 我们池（`card_stats.json:22371` 的 `name`）= **`Simulacrum Imperialis`**（今天刚撤改名回来的），
    而同一行的 `:22396 ocrName = Simulacrum Celestian`。
- 查表链（`list(1596-1700 主循环)`）：`rarity_lookup(name)`（原名 → 归一化 → `difflib` 0.88 兜底）
  → 落空再看 `RARITY_BY_FACTION[(阵营, name)]` → 再无就看 `RARITY_UNLISTED[name]`
  → 都落空 **且源值非法** ⇒ `rarity = ""` + 进 `still_missing`。
  ⇒ 撤改名后**三级全落空**、源表那一格是 `null` ⇒ 空串。**全程只有一行不带卡名的 ⚠️**（`still_missing`），
  而 G1 笔当天把源表那一格临时改成 `"epic"` 兜住了，所以**这次没出响声**（静默失败）。

**四个东西各自扮演什么（现核，逐个给判据）：**

| 名字 | 角色 | 谁在读 | 现读事实 |
|---|---|---|---|
| `卡牌宝石稀有度_0824.md` | **唯一的输入表**（目视宝石扫描） | `gen_cards_engine.py:33/635` | 1118 行 → 1113 键，**按 PnP 卡面写法建键** |
| `RARITY_BY_FACTION`（`:105-176`） | **强制覆盖** `(阵营, 改名后的 name)`；在 `RARITY_UNLISTED` **之前**、源值**之前**生效 | `:1654-1656` | **23 条** = 重名 3 + 2026-09-15 PnP 对账 2 + B20 撤改名 4 + 宝石表读错 13 + 本笔 1 |
| `RARITY_UNLISTED`（`:181-237`） | 卡名 → 稀有度，**只在宝石表查不到时**才用（语义 = 「表里**没有**这张卡」） | `:1657-1659` | 21 条（9 张没进 OCR 流水线 + 12 张卡面取色） |
| `数据/游戏数据/gems_rarity.json` | 🔴 **死文件（在 `d:/4` 里）** | **无人读** | 见下 |

**`gems_rarity.json` 是不是死文件 —— 现核：是（在 `d:/4` 里），但它不是从来就没人读。**

- 全 `d:/4` 逐扩展名 grep `gems_rarity`：**没有任何 `.py` / `.cs` 把它 `open`/`load` 过**
  （`工具/check_prebuilt_decks.py:307,345` 只是**报告文案里提到它的名字**；`工具/_同步日志_data.json:20`
  是 `sync_from_d2.py` 的**拷贝台账**，不是消费者）。
- 它**在旧 Godot 仓里确实有消费者**：`d:/warpforge/autoload/game_data.gd:210`
  `_read("res://data/gems_rarity.json").get("cards", {})` ⇒ 是 **Godot 期遗留产物**，
  搬进 Unity 后**没有任何一层接上**（`资料/索引与盘点/解包资源使用地图.md:1023` 记的是那条 Godot 管线）。
- 内容与那份 `.md` **同源**（同一轮 1118/1118 扫描）：`Simulacrum Celestian → {gem: 紫, rarity: epic}`。
  ⇒ **它不是一个独立的第二判据**（同一次人眼读数），只能当旁证。
- ⚠️ ⛔ **本笔没删它**（不在白名单；且删它要先确认没人按路径读它 —— 已确认，但那是另一笔账）。

---

## 二、`epic` 对不对

**判据 = 两条互相独立的路，都指向 `epic`。**

1. **宝石表**：`卡牌宝石稀有度_0824.md:698` 那一行 `Simulacrum Celestian | 紫 | epic`
   —— 按「文件名」列找到的（`Simulacrum-Celestian.png`）。
   ⚠️ **按我们现名 `Simulacrum Imperialis` 找是找不到的**（那位写手实测 0.88 阈值不命中，我复核：**确实不命中**，
   `difflib` 归一化名 `simulacrumimperialis` vs `simulacrumcelestian` 相似度低于 0.88）。
2. 🔴 **游戏侧真值（更硬，且与本笔新加的守卫同源）**：原版卡 SO 快照
   `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
   → `{uniqueId: "SOR24", cardName: "Simulacrum Imperialis", cardRarity: 3}`。
   枚举映射是**现算的、不是猜的**：拿这份快照与卡池按 `uniqueId` join（1009 张）做混淆矩阵，
   **每个稀有度只对应一个取值、0 例外** ⇒ `1=common · 2=rare · 3=epic · 4=legendary · 5=special`
   ⇒ **`cardRarity=3` = `epic`**。
   ⚠️ 顺带**独立佐证了撤改名**：快照里这张卡的名字**本来就是 `Simulacrum Imperialis`**（不是 `Celestian`）。

**结论 = `epic` 正确。** 依据链：游戏自己的枚举（`cardRarity=3`）＋ 卡面宝石读数（`:698` 紫=epic）；
`gems_rarity.json` 同值但**同源、不算独立**。⚠️ 本笔**没有**再开一次卡图取色（没有新判据，只是重复第 2 条）。

**顺带核到的更强一条**：这份快照的 `cardRarity` 与**整个卡池**按 id join **1005/1005 全一致（0 例外）**
（另 4 张快照里是 `0`，见 §四②）⇒ 它不只是「SOR24 的判据」，而是**全池一个未启用的权威稀有度源**（见「顺手发现」#2）。

---

## 三、改了哪几处

| # | 文件 | 现读行号 | 旧 | 新 | 为什么**这一层**是对的 |
|---|---|---|---|---|---|
| 1 | `数据/游戏数据/card_stats.json` | **22388** | `"rarity": "epic",` | `"rarity": null,` | 本仓口径 = **`card_stats.json` 是 OCR 原始源表，修正一律落在生成器一侧**（`工具/gen_cardface_fixes.py:35` 的措辞；`STAT_FIXES` 其余 29 条键**全都保持源表原值不动**）。那一格是 G1 笔**当天临时**兜的底（它自己报出来请裁）⇒ 回到原值，修在生成器 |
| 2 | `工具/gen_cards_engine.py` | **175**（注释 163-174） | ——（无此条） | `("Sororitas", "Simulacrum Imperialis"): "epic",`（加进 `RARITY_BY_FACTION`） | 这张卡**宝石表里有**（`:698`），只是键是 PnP 写法 ⇒ 按 `RARITY_BY_FACTION` 的既有用法「**(阵营, 现名) → 强制值**」补一条，**与 B20 那四条（`Land Rider` / `Morkai Eliminator` / `2nd Company Terminator` / `Bladeguard Lieutenant`）完全同形** |
| 3 | 同上 | **229**（`RARITY_UNLISTED` 注释内） | 原注写「源表里这张卡现在叫 `Simulacrum Celestian`」（**已不成立**） | 补一段 2026-10-15 补记：名字当天撤回、删键仍然对、判据落点在 `RARITY_BY_FACTION`、源表那一格回 `null` | 铁律 5：那句话现在**读起来像现状**，不改会误导下个会话 |
| 4 | `工具/gen_cards_engine.py` | **2087-2144**（`build()` 末尾） | —— | 稀有度守卫（两条谓词，见 §四） | —— |
| 5 | 同上 | `1617-1620`（记账口）· `1693`（登记点）· `2224`（返回值）· `2280/2290`（解包）· `2398-2416`（打印）· `2757`（`_bad`）· `2780-2781`（`_why`） | —— | 同上 | —— |
| 6 | `MyGame/Assets/RuleEngine/Resources/cards_engine.json` | —— | —— | —— | 生成物，**只经重跑生成器改**（本笔重跑 5 次） |

**为什么不是「恢复 `RARITY_UNLISTED` 那条」**（简报点名的三选一，我选了 `RARITY_BY_FACTION`）：
① 那张表的语义是「宝石表里**没有**这张卡」，而这张宝石表里有 —— R6 笔删它的**理由②**至今成立；
② 同形的三条先例（`2nd Company Terminator` / `Bladeguard Lieutenant` / `Land Rider`）都在
`RARITY_BY_FACTION`（B20 当时就是从 `RARITY_UNLISTED` **移**过来的）；
③ `RARITY_BY_FACTION` 是**强制覆盖**（先于源值），`RARITY_UNLISTED` 只在查不到时兜底 —— 语义更对。

**没有改成「按 id 查」**（简报明令 ⛔）：那是另一笔大工程。

---

## 四、守卫

**加在哪**：`build()` 末尾（`armor_family_bad` 之后、`return` 之前），打印在 `main()` 的守卫段里，
🔴 **`_bad` 里多一个 `or bool(rarity_guard_bad)`** ⇒ 非 0 = rc 1 = **一个字节都不写**（同 `A1626` 口径）。

**为什么是两条谓词**（一条盖不住两个方向；判据都写进代码注释了）：

**① 症状层 —— 「空串、但别名还能在宝石表查到」**（谓词：卡片最终 `rarity == ""` **且** 宝石表按它的
`ocrName` 能查到 ⇒ 报）

- **抓什么**：改名导致的**静默丢失**（本次事故的**原样复现**）。
- 🔴 **为什么用 `ocrName` 而不是 `name`**：`name` 正是主循环**刚查过**的那一条 —— 拿它再查一次
  **同义反复、恒为假**（查得到就根本不会是空串）⇒ 那就成了自证。`ocrName`（源表里「PnP 卡面印的字的
  OCR 记录」）是**独立**的另一个键，改名的场景下它**往往还指着旧写法**（SOR24 的 `:22396` 至今是
  `Simulacrum Celestian`）。
- **漏什么**：连 `ocrName` 也对不上的改名 —— 那多半是「宝石表里本来就没这张卡」，**不该报**。
  本谓词**只报「查得到却没查到」**，不报「查不到」。

**② 权威层 —— 与游戏侧真值逐张比**（谓词：`cards[i].rarity` ≠ `M[truth[id].cardRarity]` ⇒ 报；
`M = {1:common,2:rare,3:epic,4:legendary,5:special}`）

- **抓什么**：改名后**不是空串、而是拿了一个错值**（掉回源表某个旧 OCR 值）—— ① 看不见的那一半。
- **权威性**：`cardRarity` 是**游戏自己的枚举**，比宝石表（人眼看印刷品记的色名）高一级（铁律 10 第 3 条）。
- **标定**：映射现算（1005 张、混淆矩阵 0 例外）；本守卫落地时**全池 0 处不一致** ⇒ 它**不是补漏、是防回归**。
- ⚠️ `cardRarity` 是 `0` / 缺字段 ⇒ **视为「无值」跳过**（快照里那 4 张 GSC「破坏卡」就是 `0`：`Improvised Barricade` /
  `Poisoned Supplies` / `Jammed Communications` / `Cult Propaganda`，它们的稀有度**只有卡面一条判据**），
  ⛔ **不拿 0 顶**（同 `truth_int` 的 `-1` 口径）；跳过条数**打印出来**（现读 4）。
- **漏什么**：快照里没有的卡（自造 id 那 23 张；不是 118 —— 那 118 是「非真值索引」的老账）。

⛔ **没做「宝石表孤立键守卫」**（简报标题提的那种形状），理由 = **它今天必红**：宝石表 1118 行里
**68 行的卡名在我们池里根本没有对应卡**（`Dogmata` / `Land Raider` / `Spanner` / `Veteran Stormboy` …
—— 正是「PnP 写法 vs 我们的名」那一批），加它就得配一张 68 行的白名单，白名单会烂。
**等价的目的由 ① 达成**（只报「查得到却没查到」），且不需要白名单。

### 有效性演示（三次**真实违反**，各跑一次生成器；注入只动 `gen_cards_engine.py` 一个字串，跑完立刻撤回）

| # | 注入 | rc | 它出声了吗（照抄日志） | 卡池落盘了吗 |
|---|---|---|---|---|
| `D1` | **删掉本笔那条 `("Sororitas","Simulacrum Imperialis")`**（= 撤改名的真实场景） | **1** | `🔴 稀有度这一层有 2 处**对不上**：`<br>`· Sororitas/Simulacrum Imperialis —— `rarity` 落成了**空串**，但宝石表**按这张卡的 `ocrName`（`Simulacrum Celestian`）能查到 `epic`［原名］**`<br>`· SOR24/Simulacrum Imperialis —— 与**游戏侧真值**不一致：卡池是 ``、原版卡 SO 快照是 `epic`（`cardRarity=3`）` | ❌ **md5 不变** |
| `D2` | 删掉 `RARITY_UNLISTED` 的 `Acolyte Iconward`（**另一张卡**，证明不是给 SOR24 定制的） | **1** | `· Genestealers/Acolyte Iconward —— … 按这张卡的 `ocrName`（`Iconward Malak Vorenth`）能查到 `rare`［原名］`<br>`· GSC3/Acolyte Iconward —— … 卡池是 ``、快照是 `rare`（`cardRarity=2`）` | ❌ **md5 不变** |
| `D3` | 把 `("DarkAngels","Bladeguard Veteran")` 的值 `rare` → `common`（**不产生空串、只错值**） | **1** | `🔴 稀有度这一层有 1 处**对不上**：`<br>`· DA23/Bladeguard Veteran —— 与**游戏侧真值**不一致：卡池是 `common`、原版卡 SO 快照是 `rare`（`cardRarity=2`）` | ❌ **md5 不变** |

- 三次都印出 `🔴 退出码 1、**一个字节都不写**（「稀有度这一层 N 处对不上…」没解决之前不落盘）`。
- **D1/D2 各 2 条** = 同一张卡被两条谓词各抓一次（① 空串 + ② 与真值不一致）；**D3 只有 1 条** ⇒ 这正是 ② 存在的理由。
- ✅ **撤回后**：生成器 md5 回到 `299f467b1a856e14fed16e96cb5f8514`（与未注入时**逐字节相同**）；
  卡池 md5 全程 = `e7d2236238c14d99461fbd52951cec01`（**与注入前逐字节相同**）。
- ⛔ **临时改动一个都没留**（三次都是 `cp` 整份快照还原 + `assert md5 == base`）。

---

## 五、机器复核

| 项 | 结果 |
|---|---|
| 生成器 rc | **0**（连跑 3 次：`g3_run1/2/3.txt` 全 rc=0） |
| 幂等 | ✅ 三次输出**逐字节相同** ⇒ 卡池 md5 `e7d2236238c14d99461fbd52951cec01`（**= 本笔开工时现读的值**） |
| 卡池 md5 旧 → 新 | `e7d2236238c14d99461fbd52951cec01` → `e7d2236238c14d99461fbd52951cec01`（**未变**）⚠️ 原因见下 |
| **`SOR24` 的 `rarity` 现值** | **`epic`**（`cards_engine.json`，来源 = `RARITY_BY_FACTION`）· `SOR13 Simulacrum Bearer` = `common` |
| 全池空稀有度 | **0 张**（`by_rarity`：common=330 · rare=320 · epic=230 · legendary=197 · special=50） |
| 守卫现读 | `稀有度这一层：①别名漏 **0** 张 ✅ · ②与游戏侧真值一致 ✅（比过 1005 张；另有 4 张快照 `cardRarity` 是 `0`/缺字段，按「无值」跳过）` |
| `git diff --numstat` | `gen_cards_engine.py` **373/41** · `card_stats.json` **1/1** · `cards_engine.json` **1/1**（⚠️ 这 1/1 是**G1 笔留下的**，非本笔 —— 本笔跑完 md5 与开工时相同） |
| 行尾 | `gen_cards_engine.py` **CRLF 2795/2795**（纯）· `card_stats.json` **CRLF 36003/36003**（纯）—— 改完量的，**没翻**（二进制读数） |
| 语法 | `ast.parse` 通过（每次落盘后都跑） |

**⚠️ 卡池 md5 为什么没变（要说清，免得被读成「没生效」）**：G1 笔当天把源表那一格改成 `"epic"`，
所以**改前改后稀有度的值本来就是同一个** —— 本笔做的是**把它从错的那一层挪到对的那一层**
（源表 `null` + 生成器特判）。⇒ **md5 相同恰恰是验收标准②的意思**：`card_stats.json` 回 `null`、
产物一个字都不许变。若本笔把它挪错了层，md5 必然变。

**本笔改动的字节数**：生成器 `206,821 → 216,479 B`（+9,658 B，全是注释+守卫），md5 `1d8ad55b…` → `299f467b…`。

---

## 六、没查清 / 没做的

1. ⚠️ **没有重开卡图取色核 `epic`** —— 判据已有两条（§二），再取一次是同义反复。若调度台要「印刷品亲读」那一档，另派。
2. ⚠️ **`RARITY_UNLISTED` 没有孤立键守卫**（R6 笔建议过）：本笔改走「卡片侧 + 真值侧」两条（理由见 §四末）。
   要不要再补一条「键 ↔ 池内卡名」的孤立键守卫（照 `orphan_stat_fix` 形状），**留给调度台裁** ——
   它今天必绿（实读 `RARITY_UNLISTED` 21 键 / `RARITY_BY_FACTION` 22 键**全部命中池内卡**），
   与 ② 有一定重叠，但能抓「键变成了死键**且**那张卡本来有别的判据兜住」这一种（本次 SOR24 就是这种）。
3. ℹ️ **`RARITY_UNLISTED` 顶部那句「三份 0824 表没收录的 9 张」**（`:178-180`）现在**读起来偏窄**（表里已 21 条，
   后面各段自带了「🆕 …批」的说明）—— **没改**（不在本笔的两件事里，且怕与别的写手撞行）。**已记在此，供调度台裁**。
4. ⛔ **没跑 Unity、没跑 `ruleprobe`、没动 git**（红线）。
5. ℹ️ 快照里 `cardRarity=0` 的**第 5 个元素**是 `#HB_DA1`（无 `uniqueId`、不在池里），故「跳过 4 张」的 4 张全是 GSC 破坏卡。

---

## 顺手发现（**都不是本笔的活，供调度台分流**）

1. 🔴 **`资料/资源使用手册.md:249` 写错了判据层**：`稀有度 → 数据/游戏数据/gems_rarity.json（1118/1118 映射）`
   —— 生成器实际读的是 `资料/卡牌数据表/卡牌宝石稀有度_0824.md`（`gen_cards_engine.py:33 RARITY_SRC`），
   `gems_rarity.json` **在 `d:/4` 里根本没有消费者**（§一）。⛔ 那文件不在我的白名单，**没改**（铁律 5 待办）。
2. 🔴 **原版快照的 `cardRarity` 是一个全池覆盖、却完全没被用的稀有度源**：
   1005/1005 与卡池一致、`0` 例外。**今天换成它当第 1 层，一个值都不会变**，但能**从根上消掉**
   「PnP 写法 vs 我们的名」这一整类 join（本笔修的、B20 修的四条、`RARITY_UNLISTED` 的 21 条**都属这一类**）。
   本笔只把它当**守卫**用（不敢越权改源）。⇒ 建议立账：**「稀有度第 1 层是否改用 `cardRarity`」**。
3. ℹ️ `RARITY_BY_FACTION` 里 13 处「宝石表读错」的条目（`:139-151`）与 `cardRarity` **也全一致**
   ⇒ 那批**不是**「读错、我们改对」，而是**宝石表那一轮视觉扫描的读数偏差**——同一条账（#2）能一并收。
4. ℹ️ `数据/游戏数据/gems_rarity.json` **在旧 Godot 仓有消费者**（`d:/warpforge/autoload/game_data.gd:210`）
   ⇒ 它是**搬迁遗留**，不是「从来没用过」。将来清理要连那个仓的用法一起看。
