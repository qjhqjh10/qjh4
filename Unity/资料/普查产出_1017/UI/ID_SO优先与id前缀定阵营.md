# ID · 为什么用 PnP 不用 SO，能不能全切；以及 id 前缀能不能定阵营

> 只读判据代理 · 2026-10-17 · 只读现核（未跑 Unity / 未动 git / 未改任何产品或正本）。
> 现读口径：卡池 `cards_engine.json` = **1127 张**；SO 快照 = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（1195 元素 / 1194 条带 `uniqueId`）。

## 一、结论摘要

**Q1（为什么不使用原版 SO / 能不能一切改用 SO）**：不是「不用 SO」——现有卡表 **17 列已经在用 SO**（`id/rarity/cardTraits/inventoryOptions/showInCollectionOptions/initiative/goesSecond` + `A1799` 那 10 列）。三围/费用/效果文字/兵种/卡名不用 SO，有两条硬原因：① **建表时（2026-08-16）SO 包本地根本没有**，唯一来源就是 PnP 卡图 OCR；② 2026-10-13 找回的 SO 是**旧快照**（Unity 2022.3.38f1 时代 · 整个 SpaceWolves 0 张 · 118 张现池卡不在里面），不是我们要复刻的最终数字版。**技术上能切（只改一个生成器的 4 行取值），但不能「一切使用原版 SO」**：118 张卡 SO 里没有；82 张冲突里 **68 张还没逐张开图核过**，直接切 = 静默把旧值盖到新值上。

**Q2（id 前缀 + SO 能否区分阵营与卡）**：**能定阵营**——13 个前缀与 13 个阵营 **1:1、0 例外**（`GOF`=Goff/Orks，`ASH`=Saim-Hann/Aeldari；其余见 §五表）。**能定卡**——SO 的 `uniqueId` **1194/1194 唯一**，按 id 查即可。**但三个前提**：① 32 位 GUID id **没有前缀**；② **SpaceWolves 在 SO 里 0 行**（前缀能定阵营，SO 定不了卡）；③ SO 阵营内 `cardName` 有 **84 组重名/188 行**（全是附魔/先锋/教程变体），只筛短 id 时 **0 重名**。

## 二、Q1-A 当初为什么选 PnP（证据；查不到的直说）

1. **时间线决定，不是「判过 PnP > SO」**：卡表源 `card_stats.json` 的合并报告写着「生成时间 2026-08-16（`merge_card_stats.py`）」——`Unity/数据/游戏数据/card_stats_report.md:1-14`；来源自述「PnP 卡面图（视觉模型 OCR，2026-08-16）」——`Unity/资料/卡牌数据表/卡牌数据总表.md:5`；抄录规格「唯一权威来源是卡图本身」——`Unity/资料/卡表核对_卡图提取/_提取规格.md:4`。
2. **SO 包 2026-10-13 才到手**：原版 `allcards` 包是从 `C:\Windows.old` 的 Unity 缓存里抢救出来的（`d:/2/远端缓存_旧profile_1013/README.md` §一；`Unity/资料/卡牌id_换真值_落地记录.md:20-22`）。在此之前本仓记录一直是「卡数据在远端 CCD / `inj FAIL: empty Data`」⇒ 当时**没有 SO 可读**。
3. **代码层面确认**：`gen_cards_engine.py:30` 的唯一源是 `card_stats.json`；三围/费用取值就是 `card_stats` 的 `cost/attack/health/ranged_attack`（`:1750-1753`），只有**督军 `cost`** 后来改成取 SO（`:1838-1848`，理由 = PnP 不印费用格）。
4. **没查到**一条 2026-08 的「PnP 比 SO 准」判据记录。已查：`gen_cards_engine.py` 注释、`git log`（`card_stats.json` 最早一版在 2026-09-11 初始提交里）、`资料/卡牌数据源对账_0912.md`、`资料/卡牌基座_进度与交接.md`、`任务线/卡牌线/任务/T02_卡表数据订正.md`。⇒ **当时的理由是可得性，不是质量比较。**

## 三、Q1-B 判据上该用哪个：谁更新 + 铁律 10 #3 怎么读

**SO 快照的年代/覆盖（现读）**：Unity `2022.3.38f1` 时代、缓存时间 2025-07-30（README §四）；12 个 `cardArmy`（10–120），**没有 SpaceWolves（130）**；`gen_cards_engine.py:112` 也写着「SW 68 / 新 UM 38 / 新 DA 12 根本不在里面」。

**PnP 更晚的三条证据**（都不是「PnP 自己证明自己」）：
1. 🔴 **游戏内截图**（不是 PnP 打印卡）：`card_stats.json` 有 12 张卡的 `ocrSrc` 是 `Zrzut ekranu 2026-04-16 …`（游戏截图）。与 SO 冲突的正好 2 张，且 **2/2 都是「截图 == 我们/PnP、SO ≠」**：
   - `Lhykhis`：截图 `health = 25`（`card_stats.json:2911` 条目、`ocrSrc` 在 `:2958`），SO `maxHealth = 35`；
   - `Orian Laratharjos`：截图 `ranged_attack = 4`（`:289` 条目、`ocrSrc` `:315`），SO `rangeAttack = 5`。
2. 🔴 **游戏自己的官方 I2 表里有 16 对 `Card_Description/<id>_new`**（现读 16 条），`_new` 与 PnP 卡面一致而旧代不一致：`TL66` 卡面 `Tide 2` / 旧代 `Tide 4`；`TL44` 卡面 `+1 Health` / 旧代 `+2`（判据与开图记录 → `gen_cards_engine.py:1508-1524`）⇒ **数字版后来修订过卡面文本**，快照指向的是旧代。
3. **逐张开图实测**：2026-10-15 已对 14 张「我们 vs SO」有出入的卡逐张打开 PnP 成品图核过，**14/14 = 我们 == PnP、SO ≠ PnP**（名单与出处 → `gen_cards_engine.py:798-803`；其中 `Abominant`/`Hulking Aberrant`/`Rukkatrukk Squigbuggy` 三张的银盾 PnP 都印 1、SO `armour = 0`，见 `:1893-1900`）。
4. **已裁的口径**：2026-10-15 用户拍板（`施工设计 §七 已裁⑥`，转写在 `gen_cards_engine.py:788-806`）：**PnP 也印了的内容 → PnP 优先；PnP 上没有 / 印得自相矛盾的 → 照 SO 快照**（督军 `cost`、`CardTraits`、A1799 那 10 列都走这条）。

**铁律 10 #3 怎么读**：那条说「PnP 是印刷品，能当**内容**尺子，不能当**版式/显隐规则**尺子」——它是**允许**，不是「必须」。判「内容该用谁」仍要看**谁更贴近目标客户端**；上面的截图 + `_new` 证据说明 SO 快照更旧 ⇒ 内容层用 PnP 是成立的。**但 ⛔ 不能把「原版 SO」四个字直接当成「最终真值」——它只是 2022 时代的一份快照。**

## 四、Q1-C 能不能全切

**覆盖面对比（2026-10-17 现读）**

| 项 | 数 | 说明 |
|---|---|---|
| 卡池 | 1127 | `cards_engine.json` |
| SO 能按 `uniqueId` join 上 | **1009 / 1127（89.5%）** | 896 短 id + 113 GUID |
| 池内**不在 SO** | **118** | SpaceWolves 68 · Ultramarines 38 · DarkAngels 12（快照后新增） |
| 上述 118 张里**有 PnP 图** | **117 / 118** | `card_stats.ocrSrc` 非空 |
| SO 有、池没有 | **185** | 37 张环境/进攻卡（`cardType=20`；PnP 的 13 个 `进攻卡` 目录**全空、0 文件**）＋ 146 个 GUID 变体 SO（附魔/先锋/教程副本）＋ 2 个 `#HB_DA1`/`#HB_BL34`（画风记录，无 `cardName`） |
| 池内**没有 PnP 图** | 13 | 2 张 `card_stats` 无行（SAU35/SAU37）＋ 11 张 `ocrSrc` 为空；其中 **12 张在 SO 里** |
| 冲突（三围/费用/护甲） | **82 张** | 已逐张开图核 **14 张**，**余 68 张未核** |

**技术改动点（只一个生成器）**：`gen_cards_engine.py:1750-1753` 的三围/费用取值（`c.get("cost"/"attack"/"health"/"ranged_attack")`）改成「SO 优先 / PnP 兜底」。督军 `cost` 已经是 SO（`:1838-1848`）。兵种可从 SO `keyword` 枚举映射（5=Infantry · 10=Vehicle · 15=Monster · 25=Battlesuit · 30=Drone · 35=Beast · 40=Daemon；526 张有值、仅 4 处例外：`BL1 Chaos Spawn` 与 3 张 EC 督军）。**效果文字不能直接取自 SO**——SO 只有 I2 键（`itemDescriptionKey` / `cardDescriptionTextReferenceId`），要另接官方本地化 `Card_Description/*`（1098 键；官方中英表 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`）。卡名同理：SO 的 `cardName` 是显示名，46 张与我们的资产名不同。

**切了会变的列（现读差异）**：`cost` 26 张 · `health` 22 · `ranged` 20 · `attack` 18 · `armour` 3（字段有重叠，合计 82 张）；`subtype` 4 张（若走 keyword）；`name` 46 张（归一化比对；原文逐字不同 72 张）。

**风险**：
- 68 张未核冲突**直接进游戏**（含督军血量 25↔35 这种大数）；
- 118 张卡 **SO 无值**，没有兜底就会变 0/空；
- SO 有**整列哨兵**：`initiative` 1190/1192 = 20（疑似未填列）、`cardRarity = 0` 是「无值」哨兵、`cardShortId = 0` 65 张；⛔ 不能原样照抄；
- SO 只有 12 阵营，**SpaceWolves 全缺**。

**切换前必须先做**：① 把 68 张逐张开 PnP/游戏截图核（按现有开图流程）；② 定死兜底链（SO 无值 → 保留 PnP/OCR，⛔ 不清空）；③ 不消费哨兵列；④ 只改生成器取值层，**不改 `d:/2` 快照本体**；⑤ 重跑生成器并对「82 张差异表」逐张复核。

**结论：能切（技术上小改），但不能「一切使用原版 SO」**——SO 覆盖不到 118 张最新卡，68 张冲突未核前切 = 静默换值。是否切、先核哪一批，**留给主对话/用户裁**（本报告只给判据与风险）。

## 五、Q2 id 前缀 → 阵营 映射表

判据：`gen_cards_engine.py:811-825` 的 `ID_PREFIX`（注释说来源 = `decklists.json` 实测统计）＋ **本轮现算**：SO `uniqueId` 前缀 × `cardArmy` 交叉表 **0 例外**；`cardArmy` → 阵营名用 `card_stats.json` 的 `factionId/faction`（13/13 一一对应）；PnP 目录名旁证。

| 前缀 | 我们卡表阵营 | SO `cardArmy` | PnP 目录（旁证） | 备注 |
|---|---|---|---|---|
| `UM` | Ultramarines | 10 | `Ultramarines` | |
| `GOF` | Goff | 20 | `Orks` | **GOF = Goffs**（Orks 氏族/子阵营；游戏里的 Orks 阵营就叫 Goff） |
| `ASH` | SaimHann | 30 | `Aeldari` | **ASH = Saim-Hann**（灵族方舟世界；游戏里的 Aeldari 阵营就是 Saim-Hann） |
| `SAU` | Sautekh | 40 | `Necron` | SAU = Sautekh 王朝 |
| `BL` | BlackLegion | 50 | `Chaos` | |
| `TL` | Leviathan | 60 | `Tyranid` | TL = Leviathan 虫巢舰队 |
| `TAU` | TauEmpire | 70 | `Tau` | |
| `SOR` | Sororitas | 80 | `Sorotitas`（目录拼写如此） | |
| `GSC` | Genestealers | 90 | `Genestealer Cult` | |
| `AM` | AstraMilitarum | 100 | `Astra Militarum` | |
| `DA` | DarkAngels | 110 | `Dark Angels` | |
| `EC` | EmperorsChildren | 120 | `Emperor_s Children` | |
| `SW` | SpaceWolves | **130（SO 里 0 行）** | `Space Wolves` | 前缀在卡池里 68 张、全部短 id |

**例外/边界**：
- **同一前缀跨阵营 = 0 例**；**一个阵营两个前缀 = 0 例**（短 id 范围内，池与 SO 都是）。
- **32 位 GUID id**：SO 259 条、池 113 张——**没有前缀**，只能按 `uniqueId` 查（例外：`#HB_DA1` / `#HB_BL34` 两条画风记录，有 `cardArmy` 但无 `cardName`）。
- **自造 id**（形如 `UM_Terminator`）：前缀在 `_` 前，池内 23 张，全部 Ultramarines（`gen_cards_engine.py:828-832`）。

## 六、Q2 前缀 + SO 能否唯一定位（含阵营内 cardName 唯一性实测）

1. **前缀 → 阵营：能，0 例外**（上表；SO 前缀×army 交叉表、池内 id×faction 交叉表都现算过）。
2. **`uniqueId` → 卡：能**——SO 1194 条 `uniqueId` **1194/1194 唯一**。
3. **阵营内 `cardName`：不唯一**——SO 里 **84 组重名 / 188 行**（例：`(110,'The Rock')×4`、`(60,'Infinite Biomorphologies')×7`、`(60,'Hyper-adaptation')×7`），全部是**变体 SO 与基准卡同名**（`m_Name` 带 `_enchantment` / `_vanguard` / `T*` 后缀）。**只取短 id（基准卡）时：0 组重名**（933 条短 id）。
4. **`m_Name` 唯一但格式不统一**：1194/1194 唯一；但只有 **780/1194** 的尾巴等于 `cardName`，现有至少 5 种形态：`33 EmperorsChildren-Heldrake` · `T3 BlackLegion-…_enchantment` · `Z-NC Sautekh-…` · `T18 The Chapter's Due`（无阵营段）· `CSM_EmperorsChildren_war_…` ⇒ ⛔ 不能当稳定解析键。
5. **我们的卡名 vs SO `cardName`**：按 id join 的 1009 张里，逐字不同 **72** 张、归一化（去大小写/标点/空格）后仍不同 **46** 张（例：`Azrael` vs `Azrael, Grand Master`、`Leman Russ` vs `Leman Russ Tank`、`Fire prism` vs `Fire Prism`）⇒ **按名 join 会落空 46 处**，按 id join 不会。
6. `cardShortId` **不是**干净的「基准卡」标记：65 条短 id 是 0、33 条 GUID 非 0。

**小结**：`前缀 + uniqueId` = **总能唯一定位**（SO 有的卡）；`前缀 + cardName` = **只对短 id 基准卡成立**；`GUID id` = 前缀这条路不存在，只能查 `uniqueId`。

## 七、可操作查法（给我一个 id，怎么三步查到阵营 + 卡）

1. **判 id 形态**：`^[A-Za-z]+\d+$` → 短 id，前缀 = 字母段；`^[0-9a-fA-F]{32}$` → GUID，无前缀；形如 `<前缀>_<slug>` → 前缀在 `_` 前。
2. **前缀查 §五表** → 阵营（`GUID` 跳过这步，改看第 3 步的 `cardArmy`）。
3. **按 id 查卡**：先查我们卡池 `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` 的 `cards[].id`（有 `name/faction/subtype/…`）；查不到再查 SO 快照 `cards_1195_2022时代.json` 的 `uniqueId`（有 `cardName` / `m_Name` / `cardArmy`）：
   - `cardArmy` 用 §五表反查阵营（10=UM … 130=SW）；
   - 仍查不到 ⇒ 可能是环境/进攻卡（37 张，`cardType=20`）或变体 SO；**出声说「不在池/不在 SO」，别猜**。
   - 辅助索引：`数据/游戏数据/card_id_truth_index.json`（`by_official_en`/`by_tail`/`by_name`/`army`）与 `card_ids.json` 的 `mapping`（1310 条）。

## 八、还没查清的（⛔ 不猜）

1. **82 张冲突里 68 张还没逐张开 PnP 图核过**（已核 14 张）。核完之前不能断言「PnP 全对」。
2. **最终数字版（Unity 6 客户端）的卡 SO 本地没有**——现存 SO 只是 2022 时代快照；无法证明它与最终版逐字段一致（只能靠截图 + `_new` 文本 + 14 张开图收窄）。
3. **PnP 印卡套的版本号/发布日期**只知 rulebook `1.5-3`（fan revival project，**非官方**）；没有官方发布日期。能说的只有：它含 SW 与快照后新增的 UM/DA，故**晚于快照**。
4. **37 张环境/进攻卡的数值**：PnP 的 13 个 `进攻卡` 目录**全空**，本地只有 SO 一条判据（`offensive_cards.json` 另成体系，不在主池）。
5. **`m_Name` 的编号/前缀规则**没有统一文档（`T*` / `Z-NC` / `CSM_*` / 纯数字 / `#HB`），本轮只做了形态归纳，未找到生成规则。
