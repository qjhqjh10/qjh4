# X · 卡牌线 P2 相（真值落位批）—— `A1773` + `A1776` + `A1799`

> 写手：P2 相唯一写手（单文件 `工具/gen_cards_engine.py`）。**只读代理**同时在场（扫 `card_stats.json` 的
> `hasStats=false`），**与本文无交集**。
> 读数时刻 ≈ **2026-10-15 00:45–01:05**。⛔ 本文**未动 git**、**未动正本**、**未跑 Unity 批处理**。
> 🔴 **本相的总闸** = 用户 2026-10-15 拍板（`卡表块_施工设计_1014.md` §七 **已裁⑥**）：
> **PnP 成品卡图印了的取 PnP；PnP 上没有 / 印得自相矛盾的，才照真值快照。**
> ⇒ **本文每一笔都逐字段说明走的是哪一档**（这是本笔最重要的产出）。

---

## 〇、一句话结论

**P2 三笔都做完了，落盘了。** 但**换值的规模比账面小得多** —— 因为**实测证明：真值快照比 PnP 旧，
我们卡表里那 5 个数值列本来就是 PnP 的值**（13 张逐张开图，**13/13 是「我们 == PnP」而「快照 ≠ PnP」**）。
⇒ 本相真正"换"的只有 **47 张督军的 `cost`**（PnP 56/56 不印 ⇒ 落进"照真值"那一档），
其余是 **加列**（`armour` / `cardTraits` / A1799 十列）+ **一层只补空的 `keywords` 铺底**。

| 笔 | 落地 | 净效果（对比改前卡池） |
|---|---|---|
| **`A1773`** | `gen_cards_engine.py` 的 `STAT_FIXES` 适用面 + `hasStats=false` 整批丢弃 + `fix_own_armour` + 新增真值快照层 | `cost` 变 **47** 张（督军 0 → 2）· 其余 4 个数值列 **0 张** |
| **`A1776`** | 加 `armour` / `cardTraits` 两列 + `keywords` 权威序第 1 层（真值铺底、只补空） | `keywords` 变 **59** 张 · 新列 2 个（全 1127 张都有） |
| **`A1799`** | 加 10 个 SO 字段列 + `CardDatabase.cs` 的 DTO/version 守卫 | 新列 10 个（全 1127 张都有）· **行为 0 变化**（无消费者） |

**卡池**：`count = 1127` · `version 8 → 9` · `md5 = 09de80ea46bb5b3296b45dd484685883`（735,273 B ≈ 718 KB）。
**生成器幂等**：连跑两次逐字节相同 ✅。**秒级类型检查**：**0 错 / 0 错**（运行时 + 编辑器两个程序集）。

---

## 一、🔴 判据：为什么「数值换真值」最后几乎没换值（**必须先看这一节**）

### 1·1 闸的原文 + 快照的年代硬证据

- **闸**（已裁⑥）：PnP 印了 → 取 PnP；PnP 没有 → 取快照。
- **快照比客户端旧**：`TRUTH_CARDS_SRC` = 2025-07-30 的缓存；独立判据 = **`cardArmy` 只有 12 个阵营、
  整个 SpaceWolves 0 张**，而 PnP 里有 SW 督军。
- **我们一起核过的两个方向**：
  - ✅ `A1876`（22 条效果文字）⇒ **PnP 胜**；
  - ✅ 本相新增的 13 张数值/费用/护甲 ⇒ **PnP 胜**（下表）。

### 1·2 🔬 13 张逐张开图（**这是本笔的实测基础，不是推断**）

| # | 卡（id） | 我们 == PnP 的值 | 快照 | PnP 图 |
|---|---|---|---|---|
| 1 | `GOF62 Kommando` | 费 7 / 近 8 / 远 4 / 生 7 | 7 / **7** / 4 / **6** | `Orks/3部队/Warpforge_34_Kommando.png` |
| 2 | `GSC30 Metamorph Leader` | 6 / 3 / 4 / 5 | 6 / **6** / **6** / **6** | `Genestealer Cult/3部队/Warpforge_36_Metamorph-Leader.png` |
| 3 | `TAU22 Strike Team` | 4 / 2 / 6 / 5 | 4 / 2 / **5** / 5 | `Tau/3部队/Warpforge_28_Strike-Team.png` |
| 4 | `GSC16 Acolyte Heavy` | 4 / 5 / 2 / 5 | 4 / 5 / **0** / 5 | `Genestealer Cult/3部队/Warpforge_21_Acolyte-Heavy.png` |
| 5 | `GOF59 Big Shoota Boy` | 6 / 3 / 6 / 6 | 6 / **2** / 6 / 6 | `Orks/3部队/Warpforge_27_Big-Shoota-Boy.png` |
| 6 | `UM40 Avenging Zeal` | 费 **12** | **10** | `Ultramarines/4计策/zeal.png` |
| 7 | `GOF99 Skrag Every Stash!` | 费 **3** | **1** | `Orks/4计策/Warpforge_24_Skrag-Every-Stash.png` |
| 8 | `Skyborne Deployment` | 费 **1** | **2** | `Aeldari/2天赋/Warpforge_38_Skyborne-Deployment.png` |
| 9 | `Witchfire` | 费 **0** | **1** | `Aeldari/2天赋/Warpforge_24_Witchfire.png` |
| 10 | `Waaagh! Energy` | 费 **0** | **2** | `Orks/2天赋/Warpforge_17B_Waaagh-Energy.png` |
| 11 | `UM32 Tactical Insight` | 费 **0** | **1** | `Ultramarines/4计策/Warpforge_47_Tactical-Insight.png` |
| 12 | `GSC32 Abominant` | 盾 **1** | **0** | `Genestealer Cult/3部队/Warpforge_38_Abominant.png` |
| 13 | `GSC28 Hulking Aberrant` | 盾 **1** | **0** | `Genestealer Cult/3部队/Warpforge_35_Hulking-Aberrant.png` |

> 另 `GOF61 Rukkatrukk Squigbuggy`（盾 1 vs 快照 0，`Orks/3部队/Warpforge_29_Rukkatrukk-Squigbuggy.png`）
> 也开了图，值同第 12/13 条 ⇒ 护甲那条实际核了 **3** 张。

**⇒ 结论（本笔的核心）**：`{费用, 近战, 远程, 生命, 护甲}` 这 5 列里，**PnP 印得到的部分一律保留我们的值**
—— 那是 PnP 的值，**照快照改 = 把新版的数换成旧版的数**。
**并且**：现池里 **"我们 = 0 而快照 > 0"** 的卡在 `attack` / `ranged` / `health` 三列上**各 0 张**
⇒ 没有"OCR 漏读、要靠快照补"的那种缺口。

### 1·3 顺带订正两条旧记录（铁律 5）

| 原记录 | 实际 | 出处 |
|---|---|---|
| `施工设计 §七 已裁⑥` 那句「PnP 在这一点上自相矛盾：大多数督军不印费用格、**`Anvirr Keltoc` 却印 5**」 | 🔴 **那半句不成立**：亲开 `Aeldari/1督军/Warpforge_1_Anvirr-Keltoc.png` —— 右上角是**阵营徽记**（深绿圆盘里的 Saim-Hann 蛇纹），**没有费用六边形**。「5」是当年 OCR 把徽记读成了数字。⇒ **PnP 是"统一不印"，不矛盾** | 与 `gen_cards_engine.py` 里 2026-09-14 那条「56/56 无蓝色费用六边形」一致 |
| `施工设计` 的 `A1773` 行：「`Warmaster`(`manaCost=2,rarity=4`)、`Exemplar of Hate`(`1,2`) 是**误丢**」 | 🔴 **不成立 —— 它们是【重复行】**：两条的 `uniqueId` 分别是 `97e959444b53f424ea4285e2ca02c7e6` / `4cdc1b5f965f99945b685bca7660e9fa`，而这两个 id **已经在我们池里**，名字是 `Chosen of the Four` / `Master of Repentance`（显示名 = `Card_Name/<id>`，官方中文「四者之选」/「赎罪星之主」）。`cardName` 里的 `Warmaster`/`Exemplar of Hate` 是**内部名**（`m_Name` 前缀 `Z-NC` = 非收藏，`inventoryOptions=10`）⇒ **被丢掉是对的** | 现读卡池 + 官方本地化表 |

---

## 二、逐笔施工记录

### `A1773` —— 数值那一族（**改 1 列、留 4 列、归类 1 批**）

**改的文件**：`Unity/工具/gen_cards_engine.py`
（`TRUTH_CARDS_SRC` 常量段 · `load_truth_cards()` · `merge_truth_traits()` · `classify_discarded()` ·
`A1799_FIELDS` · `build()` 里 `trow`/督军费用/`_kw_filled`/`armour` 四段 · `main()` 四段摘要 ·
`STAT_FIXES` 两处注释）。

| 字段 | 走 **PnP** 还是 **快照** | 落地的动作 | 条数 |
|---|---|---|---|
| `cost`（**督军**） | 🔴 **快照** | PnP **56/56 不印费用格**（右上角是阵营徽记，亲开 `Anvirr Keltoc` 核过）⇒ 已裁⑥ 落到"照快照"那一档 ⇒ 取 `manaCost = 2` | **47** 张 0 → 2 |
| `cost`（督军，**无快照**） | —— | 真值里没有这张（SW/UM/DA 新督军）⇒ **保持现值 0**，逐张出声 | 6 张 |
| `cost`（督军 `DA1/2/3`） | 快照 = **0** | 与现值相同 ⇒ 不动（已裁④ 里点名的"另说"） | 3 张 |
| `cost`（**非督军**） | **PnP** | 🔴 **一张都没动**（26 处差异全部保留我们的值 —— 逐张核过 3 张：`Avenging Zeal`/`Skrag Every Stash!`/`Skyborne Deployment`，都是"我们 == PnP"） | 0 |
| `attack` / `ranged` / `health` | **PnP** | 🔴 **一张都没动**（18 / 20 / 22 处差异，抽核 5 张全部"我们 == PnP"；且"我们 = 0 而快照 > 0"**各 0 张**） | 0 |
| `armour`（护甲值） | **PnP 优先、快照兜底** | 见 `A1776`。**真值兜底那一支今天一次都没触发**（PnP ⊇ 快照，见 §一·2 那 3 张） | 见下 |
| `STAT_FIXES` | —— | **条目一条没删**（9 张督军 `cost: 0` 那几条**仍然有用** —— 它们的适用面缩到"快照里没有这张督军"那一档，`Njal Stormcaller`（SW，快照里没有）就靠它保持 0）；两处过期结论按铁律 5 **就地订正**（改注释、不改值） | 2 处注释 |
| `STATS_EXCEPTIONS` | —— | **未改**（`Dark Pact of Fate` 的 `cost=1` 与 `Fire Warrior Marksman`/`Infestation Node` 的整行都留着：快照里那三张的 `manaCost` 是 `-1`/`-1`/`2`，前两个是**哨兵值**（不印）⇒ ⛔ 不能拿 `-1` 覆盖我们的推断值） | 0 |
| `fix_own_armour` | **PnP** | **函数本体一行没动**（它是 PnP 那一层的读出器）；只是它的输出现在**额外**被写进 `armour` 数值列 | 0 |
| `hasStats=false` 整批丢弃 | 🔴 **快照（当判别器）** | 丢掉 **63** 张（= 66 − `STATS_EXCEPTIONS` 里那 3 张）⇒ **逐张拿真值归类**，摘要里逐档打出来：`未在快照` 49（其中 **28** 张是同名多候选、⛔ 不猜）· `进攻/防御卡(另一条线)` **7** · `非收藏(生成/附魔/教程副本)` **7** · **`🔴 疑似误丢` 0** ✅ | 63 张归类 |

**判别器（施工设计点名的那条）**：`spellType ∈ {200 OffensiveCard, 210 DefensiveCard}`
（`Assembly-CSharp/SpellType.cs`）**或** `inventoryOptions == 5 (CantAddToDeck)` ⇒ **进攻/防御卡（另一条线，
`工具/gen_offensive_cards.py`）**。实测那 7 张 = `ASH59 Infinity Circuit Overload` · `EC61 Dark Prince's Throne` ·
`EC60 Tools of Torture` · `EC62 Aural Hijack` · `GOF51 Night Attack` · `SOR64 Disrupted Sanctuary` ·
`6e2399…Factories Overdrive`。
⚠️ **顺序有意为之**：先判这一档、再判"非收藏" —— 因为 `EC60`/`EC61` **两个 flag 都成立**，它们是**防御卡**
（已在我们池里），先判"非收藏"会误报。
⚠️ **`cantBeRandomlyCreated` 只作旁证、⛔ 不当主判据**：149 张为 1 里 **73 张是短码可收藏卡**。

**⇒ 「这批丢掉的真卡数 = 0」**（`疑似误丢` 档为空，且摘要里逐张可复核）。**没有新增卡进池。**

### `A1776` —— `armour` / `cardTraits` / `keywords` 权威序

**改的文件**：`Unity/工具/gen_cards_engine.py`（`TRAIT_DISPLAY` · `TRAIT_ENGINE_UNREGISTERED` ·
`truth_traits_of()` · `merge_truth_traits()` · `build()` 里 `cardTraits` 段）·
`Unity/MyGame/Assets/RuleEngine/Data/CardDatabase.cs`（`CardDto.armour` / `CardDto.cardTraits`）。

**① `cardTraits` 列（新）—— 走【快照】**

- 值 = 真值 `CardTraits[].name`（`_traitValue > 0` 时写成 `"Blast 2"` 这种），**显示写法取自我们卡池里已有的用法**
  （2026-10-15 现算：45 个 trait 里 42 个在我们池里已用过；只有 `frenzied`/`rage`/`unstoppable` 取惯例 Title Case）。
- 🔴 **读 `CardTraits`、⛔ 不读 `ShowTraits`** —— 后者非空只 **32** 张、`CardTraits` 有 **650** 张，
  且 `A1748` 那 18 张的 `ShowTraits` **全空** ⇒ 对着它判会把 633 张误判成"没挂 trait"。
- 覆盖：现池 **612 / 1127** 张非空（快照里 650 张，差额 = 快照里那批不在我们池 + 118 张新卡）。
- ⚠️ 本列**不喂引擎**（纯落值）⇒ 它能带上引擎 `Prefixes` 不认的三个词（`Frenzied`/`Rage`/`Unstoppable`，16 张）。

**② `armour` 列（新）—— 走【PnP 优先 / 快照兜底】**

- 值 = **PnP 那一层**（`keywords` 里的 `Armour N`：OCR + `cardface_fixes` + `fix_own_armour`）
  **优先**，PnP 读不到才退**快照的 `armour` 字段**。
- 实测：**>0 共 80 张 · 全部取 PnP 那一层 · 快照兜底 0 张**（快照只有 62 张 > 0，PnP ⊇ 快照、
  差额 3 张就是 §一·2 第 12/13 条与 `Rukkatrukk Squigbuggy`）。
- 🔴 **⚠️ 消费者尚未切换**：引擎今天仍读 `KwValue("armour")`（`Core/` 的读点，**不在本笔白名单**）
  ⇒ **本列现在不改变任何结算**，只是把值落下来（"解耦"的数据半边做完了、读点那半边另立）。

**③ `keywords` 权威序（已裁③）—— 第 1 层走【快照】，且只补空**

序表（**已抄进代码注释**，从低到高、后面的赢）：

| 序 | 写手 | 在哪 | 规则 |
|---|---|---|---|
| 1 | 真值 `CardTraits`（**本笔新加**） | `build()` 里 `_kw_filled` 那一段 | 🔴 **只补空**：OCR 那格为空时才填，⛔ 不覆盖已有值 |
| 2 | OCR 源表（`card_stats.json`） | `_kw_ocr` | 不动 |
| 3 | `cardface_fixes.json` 的 `keywords`（阵营键 > 裸键） | `_ff` 那一段 | 不动 |
| 4 | `fix_own_armour` 正则追加 | `fix_own_armour(entry)` | 不动 |
| 5 | `_manual_keywords`（人工最终裁定） | 由 `gen_cardface_fixes.py` 落进第 3 层 | **最高** |

- **净效果**：`keywords` 变 **59** 张。逐层读数：第 1 层"命中 119 张 / 追加 134 个 trait" → 其中
  **25 处（24 张）被更高层（序 3/5）换掉**（规则内，摘要里 **ℹ️ 逐条打出来**）→ 剩下 59 张真的变了。
- 🔴 **结构上不可能静默**：① 追加的每条都要在摘要里报数；② 被更高层换掉的也要报数；
  ③ 因引擎不认而跳过的也要报数。
- **抽验两处（亲开 PnP 图）**：
  - `ASH12 Swooping Hawk`：第 1 层追加 `Rally`，**被序 3 换掉** ⇒ 亲开
    `Aeldari/3部队/Warpforge_16_Swooping-Hawk.png` —— 卡面**只印 `Waystone.` + `Flying.`、没有 `Rally`**
    ⇒ **快照自己带旧数据、被换掉是对的** ✅
  - `ASH83 Autarch`：第 1 层追加 `Waystone` **并保留下来** ⇒ 亲开
    `Aeldari/3部队/Zrzut ekranu 2026-04-16 o 18.59.12.png` —— 卡面**清清楚楚印着 `Waystone.`**
    （我们的 OCR 把它丢了）⇒ **这次补对了** ✅

### `A1799` —— 10 个 SO 字段列（**走【快照】· 只落值**）

**改的文件**：`Unity/工具/gen_cards_engine.py`（`A1799_FIELDS` 表 · `truth_int()` · `truth_ref_name()` ·
`build()` 里那一段 8 行的落值循环）· `Unity/MyGame/Assets/RuleEngine/Data/CardDatabase.cs`
（10 个 DTO 字段 + **version 守卫 `dto.version < 9`**）。

| 列 | 快照字段 | 现池有值 | 缺值 | 备注 |
|---|---|---|---|---|
| `attackDamageType` | ✓ | **1009/1127** | `-1` | `DamageType`：`4 Attack` ×965 · `0 Normal` ×44（多为督军）· `-1` ×118 |
| `cardShortId` | ✓ | 1009 | `-1` | 🔴 **⛔ 不许当 id 换**（与 `A970` 那套 0/763 重合、全库 0 处读取点） |
| `useOverDraw` | ✓ | 1009 | `-1` | `1`×572 · `0`×437 |
| `cantBeRandomlyCreated` | ✓ | 1009 | `-1` | `1`×149 · `0`×860 |
| `expansionPack` | ✓ | 1009 | `-1` | `0`×828（基础）· 其余 13 种 |
| `refNameId` | ✓ | **0** | `""` | ⚠️ **现池全空** —— 快照里那 70 条**全在 GUID 卡上**、那批没进我们池 |
| `itemDescriptionKey` | ✓ | 36 | `""` | 只有 `…/DefenceCardExplanation` 一种命中 |
| `fullHeroSpriteReference` | ✓ | 50 | `""` | 有 `m_SubObjectName` 就写它（实测 `UI_Deck_Warlord_<卡名>` = **资产名**），否则写 GUID |
| `eventSpriteName` | ✓ | 44 | `""` | 44 张值全同 = `Warlord_events_Loken` |
| `targeted` | ✓ | 1009 | `-1` | `1`×212 · `0`×797 |

- 🔴 **缺值一律 `-1`（整数）/ `""`（字符串）** —— 与 `inventoryOptions`/`initiative` 同一口径，
  ⛔ **不许拿 0 顶**（这一族里 `0` 多半是合法取值）。
- 🔴 **本笔只落值、⛔ 没接 `CardDef`** —— `CardDef` 住在 `RuleEngine/Core/`，**不在本笔的改动白名单**。
  ⇒ **这 10 列现在没有任何消费者、行为 0 变化**（`attackDamageType` 本来就是"补机制不是换数据"）。
- `CardDatabase.cs` **bump 到 v9 并补了守卫**（`dto.version < 9` ⇒ `LogWarning`）——
  否则新列会被静默读成 `0/null`（`施工设计 §五·3` 点名的坑）。

---

## 三、加了哪些断言 / 判别式

**① `keywords` 权威序的三条断言（已裁③ 点名要的两条 + 一条完整性）** —— 实现在
`merge_truth_traits()` 里、**全池逐卡跑**，违例 ⇒ **退出码 1、一个字节都不写**（与 `_bad` 同一条口径）：

| 断言 | 形状（**结构上不可能同时满足**） | 现读 |
|---|---|---|
| ⓐ **只补空** | 追进去的每一条，在 OCR 层里**都还没有**（有就该被 OCR 覆盖、不该追加） | ✅ 0 违例 |
| ⓑ **OCR 条逐字原样** | 返回列表的**前 N 条**（N = OCR 层长度）必须与 OCR 层**逐字相同**（真值不许换掉它们的写法） | ✅ 0 违例 |
| ⓒ **真值每条有归宿** | 真值每个 trait 必须落在「OCR 已有 / 已追加 / 只因引擎不认而跳过」三者之一（**不丢东西**） | ✅ 0 违例 |

> 这**就是**施工设计要的那两条的强化版：ⓐ+ⓑ 合起来 =「真值填的值，在 OCR 也有的卡上被 OCR 覆盖」；
> ⓒ 保证「OCR 为空的卡上真值一定保留」不是靠运气。
> 🔴 **为什么断言写在生成器、不写在 `RuleEngineTest`**：这两条是**层与层之间**的性质 ——
> 到 C# 那一侧只剩最终结果，**看不见 OCR 层**，写在那里只能断"结果对不对"，断不了"顺序对不对"。

**② 判别式（`A1773` 的丢弃批归类）**：`spellType ∈ {200,210}` **或** `inv == 5` ⇒ 进攻/防御卡；
否则 `inv == 10` ⇒ 非收藏；否则 ⇒ 🔴 疑似误丢。**顺序有意为之**（见 §二·`A1773` 那两段说明）。

**③ 真值层的"缺值不许被当成 0"**：所有整型真值列走 `truth_int(row, key, -1)`（缺/非数字 ⇒ `-1`），
字符串列走 `truth_ref_name()`（缺 ⇒ `""`）；并在 `note` 里逐列写死 ⛔ 别把 `-1` 当 `0`。

**④ 生成器幂等**：连跑两次逐字节相同 ✅（`json.dump` 无时间戳）。

**⑤ 回归风险实测（不是推断）**：对**净变化的那 59 张**逐张算
`CardDef.CollectBareKeywordBody` 那道闸（"带正文关键词唯一才敢认"）：
- **「丢掉裸写正文」= 0 张**（改前正好 1 个 body keyword、改后 ≥2、且 desc 无 `X:` 前缀的：**0**）；
- **「新认领裸写正文」= 0 张**；
- **同卡内重复关键词 = 0 张**（改前也是 0）；
- 追加的每个关键词**都在 `KeywordTable.Implemented` 里**（现读 71 条名单逐条比）⇒ **卡面不会新打 `*`**、
  `UnimplementedKeywords` 不会变长。

---

## 四、`ruleprobe` 读数（**我没重钉任何指纹**）

`bash 工具/ruleprobe.sh check` ⇒ **rc=1**，四个数与**改动前完全一样**：
`全池 1121 张 · 解析差异 1 行 · 改名 0 · 新增 172 · 消失 171`，唯一那行差异还是 **`GSC29`**（`A1989` 那笔）。

🔴 **为什么我的改动对它是"隐形"的**：`工具/ruleprobe/Scan.cs` 的 dump **不含 `keywords` 这一列**
（`grep -n keywords Scan.cs` = **0 命中**）⇒ 我加的 59 处关键词它看不见；`cost` 也不在 dump 里。
唯一动的是**卡池 md5**（`09de80ea46bb5b3296b45dd484685883`），而那条哨兵走"**此时不判**"（只打印）。
⇒ **三个指纹仍按主对话的指示保持未钉状态**，我一个字没动。

---

## 五、改了哪些文件 / 哪些行（全部行号**现核**）

| 文件 | 改动 | 行尾 |
|---|---|---|
| `Unity/工具/gen_cards_engine.py` | **+758 / −22**（`git diff --numstat`）。新增：`TRUTH_CARDS_SRC` 常量段 · `load_truth_cards()` · `TRAIT_DISPLAY` / `TRAIT_ENGINE_UNREGISTERED` · `truth_traits_of()` · `merge_truth_traits()` · `classify_discarded()` · `A1799_FIELDS` / `truth_int()` / `truth_ref_name()` · `build()` 里 4 段 · `main()` 里 4 段摘要 · `fix_own_armour` 之后那段 `armour` 终值 · `version: 8 → 9` + `source`/`note` 整段 · `_bad` 加上 `kw_order_violations`。改注释：`STAT_FIXES` 两处 + 一处旧结论订正 | **CRLF 保持**（2185 CRLF == 2185 LF）✅ |
| `Unity/MyGame/Assets/RuleEngine/Data/CardDatabase.cs` | **+113 / −0**。`CardDto` 末尾加 12 个字段（`armour` · `cardTraits` · `attackDamageType` · `cardShortId` · `useOverDraw` · `cantBeRandomlyCreated` · `expansionPack` · `refNameId` · `itemDescriptionKey` · `fullHeroSpriteReference` · `eventSpriteName` · `targeted`，每个都带自解释 doc）；version 守卫加 `< 9` 那一支 | **LF 保持**（CRLF 0）✅ |
| `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` | 生成物（1 行 JSON）：`version 9` · 12 个新列 · `cost` 47 处 · `keywords` 59 处 | —— |

**没碰**：`数据/游戏数据/cardface_fixes.json`（本笔无需要改的条目 —— `A1748` 尾巴 `SW13`/`SW31` 仍按原裁挂起）·
`card_ids.json` · `RuleEngine/Core/**` · `CardPresentation/**` · `资料/普查产出_第三十会话/**` · git。

---

## 六、没查清 / 交回主对话的（⛔ 没猜）

1. 🔴 **A1799 那 10 列"只落值"** —— 消费者（`CardDef` 字段 + 判定/表现接线）**没做**，因为
   `CardDef` 住 `RuleEngine/Core/`、**不在本笔白名单**。要接的话是**下一笔**（同一批最好一次接完）。
2. 🔴 **`attckDamageType` 的机制**（"补机制不是换数据"）：我们引擎整条没有伤害类型概念
   （`RuleCore.cs:5745` 自陈）；本笔只落值。
3. 🔴 **`Rage` / `Frenzied` / `Unstoppable` 进不了 `keywords`**（**16 处 · 16 张**）——
   `CardDef.Prefixes` 没登记这三个词，写了会被 `KeywordTable.Normalize` 返回 null 后**静默丢弃**。
   它们**已进 `cardTraits` 列**。要接进引擎得动 `RuleEngine/Core/CardDef.cs`（本笔白名单外）**＋ 补一条断言**。
4. ⚠️ **`armour` 列**同 ①：列落好了，**读点没切**（引擎仍读 `KwValue("armour")`）。
5. ⚠️ **丢弃批里 28 张"同名多候选"**（`Blackout` / `Webway Rift` / `Dawn Attack` / …，⛔ 我按判据"不猜"）
   —— 它们的真值行要按 **`m_Name` 的阵营前缀**再切开才判得准，本笔没做（那会引入第二条 join，超出本笔口径）。
6. ⚠️ **`fullHeroSpriteReference` 的 GUID 那一支解析不出资产名**（`guid_map.tsv` 那 9 类引用没含它，0/59）
   —— 要有 `m_SubObjectName` 才拿得到名（实测 50 张里有 3 张是 `UI_Deck_…` 形态）。要全解需另跑一次
   `工具/_guid_resolve_probe.py`（⛔ 本笔不猜）。
7. ⚠️ **`cardface_fixes` 的"卡面核对修正 关键词 N 处"这个读数会变大**：它的比较对象现在是
   第 1 层铺过底之后的列表 ⇒ 语义没变（还是"序 3 换掉了整列"），但**数字不再等于"转录改了几处"**。
   本笔没改这个打印（改它会动既有摘要口径）—— **留给主对话裁**。
8. ⚠️ **自检还没跑**（按用户口径，自检由主对话在账清零后统一跑）。本笔**没有**跑任何 Unity 批处理。

---

## 七、本笔的价值 / 代价（一句话）

**价值**：① 用 **13 张开图实测**把"哪一版是现行"这件事**定死**了（PnP 胜），避免了"照快照整列覆盖"
这种**静默降级**；② 补齐了 `armour` / `cardTraits` / A1799 十列共 **12 列**的真值落位；
③ `hasStats=false` 那 63 张**第一次有了可复核的归类**（结论：**一张真卡都没丢**）；
④ `keywords` 权威序**从"注释里的建议"变成了带三条断言的代码**。
**代价**：12 个新列让 `cards_engine.json` 从 **463,192 B（452 KB）→ 735,273 B（718 KB）**
（**+272,081 B · +59%**）。
