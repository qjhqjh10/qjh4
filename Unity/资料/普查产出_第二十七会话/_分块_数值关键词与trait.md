# 分块：数值 · 关键词 · trait（兵种/特性）· 卡 id 方案 —— 「本地拿不到原版数据 ⇒ 兜底/自设计/近似/推断/凭成品卡图反推」清单

> 只读普查（第二十七会话）。**只覆盖三个域**：① 数值（费用/近战/远程/生命/护甲/先攻）② 关键词 ③ trait（兵种/特性）+ 卡 id 方案。
> 判据来源（唯一能给出「原版真值」的那一份）：`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
> —— 原版 `RawCardScript` **1,195 张卡 SO 全字段**（Unity 2022.3.38f1 / 缓存 2025-07-30；⚠️ 比我们卡池老，个别卡的值未必等于最终版）。
> 我们那一侧：`Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（**1,126 张**，由 `工具/gen_cards_engine.py` 生成）。
> ⚠️ 本文是**普查**，不是裁定；「初判」只决定先后，**不决定做不做**（铁律 11）。

---

## 一、清单（每条一行）

| # | 域 | 位置(文件:行号) | 现状措辞原文(≤40字) | 我们兜底成了什么 | 缺的原版数据是什么 | 远端包能给吗 | 初判 |
|---|---|---|---|---|---|---|---|
| 1 | 数值 | `工具/gen_cards_engine.py:192` | 「数值修正 —— **OCR 读错/漏读**的卡。每条都对着卡面核过」 | `STAT_FIXES` 手写 25+ 张的费用/近战/远程（逐张开 PnP 成品卡图读出来） | `manaCost` / `meleeAttack` / `rangeAttack` / `maxHealth` | ✅ 能给（4 个字段，1,195 张全有） | 值得换 —— 可整池机器对账，替掉逐张读图 |
| 2 | 数值 | `工具/gen_cards_engine.py:996`（`fix_own_armour`） | 「补上「这张卡**自己**的护甲」—— 源数据漏了将近一半」 | 用 `desc` 正则 `^\s*Armour (\d+)` 反推 `Armour N` 关键词（98 张里只有 41 张进了 keywords） | `armour`（整数） | ✅ 能给（`armour`） | 值得换 —— 现做法只认「段首」，#3 那种效果给的会漏 |
| 3 | 数值 | `工具/gen_cards_engine.py:334` | 「9 张督军的「费用」：卡面**费用槽是空的** ⇒ 引擎里都该是 0」 | 56 张督军 `cost` 一律 **0**（判据 = PnP 卡图右上无蓝六边形，56/56 核过） | `manaCost`（督军真值 = **2**；例外 `DA1`/`DA2`/`DA3` = 0） | ✅ 能给（`manaCost`） | 存疑 —— 真值与我们的 0 **不同**；但督军不从手牌打出，要先判这个 2 是「真值」还是哨兵 |
| 4 | 数值 | `工具/gen_cards_engine.py:391`（`STATS_EXCEPTIONS`） | 「费用取同族值 1（③ 是唯一来源，**不是猜**）」 | `Dark Pact of Fate` 的 `cost=1` 按同 subtype 另三张推 | `manaCost` | ✅ 能给 | 值得换 |
| 5 | 数值 | `工具/gen_cards_engine.py:405`（`STATS_EXCEPTIONS`） | 「源表里这条 `hasStats=False` ⇒ 被整个跳过，卡池里**根本没有这张**」 | 一整行数值/兵种/关键词/稀有度 **手写**进白名单 | 整行（`manaCost`/`meleeAttack`/`rangeAttack`/`maxHealth`/`armour`/`cardRarity`） | ✅ 能给 | 值得换 |
| 6 | 数值 | `工具/gen_cards_engine.py:378` | 「`hasStats=false` 的 **81 张绝大多数是真噪音**，整批丢掉是对的」 | 81 张整批丢弃，白名单救回少数真卡 | 全池数值 | ✅ 能给 | 值得换 |
| 7 | 数值(先攻) | `MyGame/Assets/CardPresentation/Battle/BattleDriver.cs:2985-2993` | 「**所有督军的 `initiative` 相同** ⇒ 那条比先攻值的分支永远平局」 | 数据里**根本没有** `initiative` 字段；先后手靠硬币 | `initiative` | ✅ 能给（`initiative`） | 存疑 —— 真值 58/60 督军 = 20、2 张教程督军 = 30 ⇒ 注释基本成立；补字段后可对账 |
| 8 | 关键词 | `工具/gen_cards_engine.py:415` | 「**关键词数值被吃掉**：卡面写 `Armour 1. Blast 2`，我们的 `keywords` 是 `['Armour','Blast']`」 | 靠 1,118 张 PnP 卡图逐张核出来的 `cardface_fixes.json` 回填 | `CardTraits[]._traitValue` | ✅ 能给（`_traitValue`，650 张非空） | 值得换 —— 本次现算 680 处可比数值里**只差 1 处**（`TAU9 Pathfinder` companion 1 vs 真值 2）⇒ 换真值可把这 1 处收掉并自证 |
| 9 | 关键词 | `工具/gen_cards_engine.py:417` | 「**关键词整个缺失**：`Tide N`（兽人 4 张）/ `Regeneration N` / `Blast 3` 等」 | 靠 PnP 转录表 + `_manual_keywords` 补 | `CardTraits[].name` | ✅ 能给（`CardTraits`） | 值得换 |
| 10 | 关键词 | `数据/游戏数据/cardface_fixes.json:335`（`_manual_keywords`） | 「手工维护的……脚本会原样保留」 | **187 条**手工关键词覆盖（逐张读 PnP 成品卡图抄的） | `CardTraits` | ✅ 能给 | 值得换 |
| 11 | 关键词 | `工具/gen_cardface_fixes.py:106`（`read_new`） | 「1,118 张卡逐张看卡图**独立抄的**」（`_合并总表.md`） | `keywords`/`subtype` 两列**整列**从 PnP 转录表重算 | `CardTraits`（trait）· 兵种 | ⚠️ 部分（trait ✅ 有 `CardTraits`；**兵种此包没有**，见 #19） | 值得换（trait 那半） |
| 12 | 关键词 | `MyGame/Assets/RuleEngine/Core/CardDef.cs:4242` | 「`FirstNumber` 对「没有数字」的关键词**兜底返回 1**」 | 没印数字的关键词一律记 1 ⇒ 「印了 1」与「什么都没写」在解析结果里分不开（另配 `HasNumber` 打补丁） | `CardTrait.GetNewTrait(..., defaultValue = 0)` / `_traitValue` | ✅ 能给（`_traitValue`；650 张里绝大多数 = 0） | 值得换 —— 真值里 0 就是「没写」，可直接把兜底改回真值 |
| 13 | 关键词 | `资料/关键词三列对账.md:186` | 「现在只剩 2 个：`destroyer`（判据没查到，**宁可认不出也不猜**）· `ecstasy`（**阈值 X 拿不到**）」 | `destroyer` 只认得出不结算；`ecstasy` 阈值靠卡表关键词值 | `destroyer` 的原版机制判据 · `ecstasy` 的阈值 X | ⚠️ 部分（**`ecstasy` 阈值 = `CardTraits._traitValue`** ✅，例 `EC31`=5 / `EC57`=15 · **`destroyer` 在 `DefinedTrait` 与 `CardTraits` 里都没有**） | 值得换（ecstasy 那半，当场可做） |
| 14 | 关键词 | `工具/_apply_kw_misextract.py:42` | 「那一批清洗有 **3 个词【一个字都没生效】**，根因是键的优先级」 | 六路子代理逐张读 PnP 抄的「误抽清洗」表 | `CardTraits` | ✅ 能给 | 值得换 |
| 15 | 关键词 | `MyGame/Assets/RuleEngine/Core/CardDef.cs:3428` | 「`Duty:` 在卡池里 **0 张**（方括号是我们数据层的**占位**）」 | `duty` 只在 `Prefixes` 里登记，卡池 0 命中 | 🔴 **2026-10-14 更正（铁律 5 · 账 `A1898`）**：本条**已过期** —— 现读三口径 = `keywords` 带 `duty` **23 张** · `desc` 印 `Duty:` **18 张** · 原版真值 **24 张**；唯一还成立的半句是「**方括号 `[Duty]` 0 张**」（那是我们数据层的占位写法）。 `CardTraits`（`duty` **24 张**） | ✅ 能给 | 值得换 —— 真值直接证伪「卡池 0 张」 |
| 16 | 关键词 | `MyGame/Assets/RuleEngine/Core/CardDef.cs:1085` | 「查不到同名卡的 8 个……这 **7 个是真名字**，池里没有同名卡」 | 天赋 `Talent: X` 靠「去卡池找同名战术卡」；7 个真名查不到 ⇒ **天赋是哑的** | 卡 → 天赋卡那张战术卡的映射真值 | ⚠️ 部分（`CardTraits.talent` ✅ 有；「名字 → 哪张卡」要 `relatedCard1..4` / `cardAbilities` 才定，**还没查**） | 值得换（先查清映射那一跳） |
| 17 | trait | `资料/关键词三列对账.md:63` | 「原版+规则书都有、**我们没做**（26 个）★ 最该看的」 | 26 个原版 trait 未实现（另有 79 个原版有、规则书没收的） | `DefinedTrait`（本地已有）+ **哪些卡真的带它** = `CardTraits` | ✅ 能给（`CardTraits` 650 张） | 值得换 —— 真值一次给出「哪张卡挂哪个 trait」，不用再靠卡面推 |
| 18 | trait | `工具/gen_cards_engine.py:805`（`keywords`/`subtype` 源列）+ 本次现算 | 「上面那两列……都是**从 OCR 那份源表来的**，实测会串列、会掉数值」 | 全池 `keywords` 的「有没有这个词」靠 OCR 表 + PnP 转录表 | `CardTraits` | ✅ 能给 | 值得换 —— 🔴 **本次现算：837 张可比卡里 100 张缺原版 trait**（talent 32 · rally 16 · pray 14 · frenzied 10 · strike 7 · slay 4 · stimulation 4 · artifice 4 · rage 4 · ecstasy 3 · backlash 3 · duty 2 · agenda 2 · flank 2 · companion 2 · waystone/vanguard/regiment/ambush/unstoppable 各 1）；反向「我们有原版没有」26 张（`destroyer` 10 · `armour` 3 · `rally` 3 · `codex` 3 …） |
| 19 | trait(兵种) | `工具/gen_cardface_fixes.py:62`（`SUBTYPE_CANON`） | 「卡面那行**橙字** → 我们卡表里 `subtype` 的规范写法」 | `subtype`（`Infantry`/`Vehicle`/`Beast`/`Daemon`/`Drone`/`Battle Ability`…）**全靠读 PnP 卡面橙字** | 兵种真值 | ❌ 给不了 / **不是数据问题**（`RawCardScript` 全字段里**没有兵种字段**；`cardRaceTextReferenceId` **1,192 张全空**） | 存疑 —— 要另找兵种资产；⛔ 别把 `subtype` 当已证实 |
| 20 | trait(兵种) | `数据/游戏数据/cardface_fixes.json:323`（`_manual_subtype`） | 「**这一类卡从没被逐张核对过**」（`_合并总表.md` 没有 `6破坏卡`） | 4 张破坏卡 + 1 张中尉的 `subtype` **手工写死** | 兵种真值 | ❌ 给不了（同 #19） | 存疑 |
| 21 | 卡 id | `工具/gen_cards_engine.py:578`（`make_id`） | 「自造 id 的形态：`<前缀>_<卡名 slug>`。**故意长得和原版 id 不一样**」 | **187 张**卡拿不到原版 id ⇒ 自造（`UM`36 · `BL`17 · `GOF`17 · `TL`17 · `DA`16 · `TAU`16 · `GSC`15 · `SAU`13 · `EC`12 · `AM`11 · `ASH`9 · `SOR`8） | `uniqueId` | ⚠️ 部分（快照里没有这 187 张 ⇒ 要**更晚的** `allcards`） | 存疑 —— 判据在远端/更晚快照 |
| 22 | 卡 id | `数据/游戏数据/card_ids.json` 的 `note`（本次现算用 `cards_engine.json`） | note 自陈：「2026-08-17 **自建**：**PnP 卡表编号 = 游戏卡 ID 偏移 0**」 | 🔴 **102 张**卡用的是「旧表保留、**未经真值核对**」的数字 id（`SW`68 · `UM`15 · `DA`12 · `EC`6 · `TL`1）—— **长得像原版 id、没有下划线，所以看不出来**；而同一张旧表在与真值重叠的 873 条里**错了 851 条** | `uniqueId` | ⚠️ 部分（同 #21） | 值得换 —— **同源旧表 97% 是错的**，这 102 张应先按「可疑」逐张标出，别当已证实 |
| 23 | 卡 id | `工具/gen_tutorial_stages.py:255`（`OCR_NAME_FIX`） | 「⛔ 不是原版明确指认（原版卡 SO 包本地没有 ⇒ **钉不死**）」 | 教程里 2 张卡（`Gauss Warrior (tutorial)` / `Tesla Immortal`）按 PnP `ocrName` **裁定**映射到我们的卡 | 卡 `uniqueId` + 教程 SO 的引用 | ✅ 能给（`uniqueId` 1,194 个 + 教程 SO 都在手的可能性高） | 值得换 —— 真值到手指向 `SAU10` 一类可复核 |
| 24 | 卡 id | `工具/gen_prebuilt_decks.py:281` | 「疑似(近似)」/「查不到」（`SequenceMatcher ≥ 0.8` 才认） | 预组牌「卡名 → id」走**子串/近似**兜底，还留一档「疑似」 | `uniqueId`（原版预组资产里逐字写着） | ✅ 能给 | 值得换 —— 真值可在近似的同一批上直接判对错 |
| 25 | 卡 id | `工具/gen_offensive_cards.py:46` | 「候选名改从「`enviromentalEffectVFX` → SO 名 → 贴图名」**反推**，标 `cardName_candidate`」 | 进攻卡的卡名**一律 null**，只给反推候选（`cardName_candidate_src`） | 卡 `uniqueId` / `cardName`（原版 PPtr `m_FileID = 4` 指外部文件，解包 JSON 没有 externals 表） | ⚠️ 部分（若卡本体在 `allcards` 里、且 PPtr 的 PathID 能对上，则可解；**未验**） | 值得换 —— 真值里有 `uniqueId`↔`cardName` 全表，可拿来比对 |

> 共 **25 条**（达到上限）。截断说明：本表按「✅ 能给真值」优先，`存疑` 的 5 条（#3 #7 #19 #20 #21）与 `不适用` 的 0 条都保留在表内；
> **没有更值得挤进来的长尾** —— 其余命中项（`check_pnp_cards.py:222` 的模糊兜底、`gen_card_vfx_by_card.py:44-47` 的 `inferred-*` 三层、`待办判据_战场与战斗视图.md:1230` 的 `huntMark` 留白）
> 分属「插图/特效/语音」域，**不在本次三个域内**，只记进 §四。

---

## 二、扫了什么 / 没扫什么

**扫过的路径**

- `d:/4/Unity/MyGame/Assets/**/*.cs`（重点：`RuleEngine/Core/{CardDef,CreatePool,RuleCore,EffectText,CardInstance}.cs`、`RuleEngine/Data/{StarterCards,DeckBuilder,DeckLibrary,TutorialDatabase}.cs`、`CardPresentation/Battle/BattleDriver.cs`）
- `d:/4/Unity/工具/*.py`（重点：`gen_cards_engine.py` · `gen_cardface_fixes.py` · `gen_trait_dict.py` · `gen_tutorial_stages.py` · `gen_prebuilt_decks.py` · `gen_offensive_cards.py` · `gen_card_vfx_by_card.py` · `_apply_kw_misextract.py` · `check_pnp_cards.py` · `keyword_frequency.py`）
- `d:/4/Unity/资料/*.md`（重点：`关键词三列对账.md` · `关键词频次表.md` · `卡牌id_换真值_落地记录.md` · `交接_第二十七会话.md` · `全量反编译复核_靠推断的清单.md` · `卡表逐张核对_与对账.md` · `卡牌数据源对账_0912.md` · `待办判据_战场与战斗视图.md` · `普查产出_第二十七会话/A1748_原版trait真值.md`）
- `d:/4/项目任务.md`（§三 各条 + §29·b/§29·d 那一族）
- `d:/4/Unity/数据/游戏数据/{cardface_fixes.json, card_ids.json}`（只读）
- 真值侧：`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（现算）· `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs`（字段表）

**用过的关键词**

兜底 · 自设计 · 自己挑的 · 我们挑的 · 近似 · 推断 · 反推 · 推算 · 猜 · 拿不到 · 本地没有 · 查不到 · 零命中 · 降级 · fallback · 占位 · OCR · 抄错 · 人工 · `_manual_` · 临时 · 非原版 · 偏离 · 硬编码 · 写死 ·
subtype · trait · Trait · 兵种 · keyword · Keyword · 关键词 · manaCost · cost · armour · health · melee · ranged · initiative · cardId · card_id · defence · 防御卡

**没扫的**

- 特效 / 语音 / 插图 / UI 版式（另一个域，虽然命中了同一批关键词）
- `卡片效果`（`cardAbilities` / `chosenAbility`）那一层 —— 已在 `交接_第二十七会话.md` §二·甲 5 立账（`A1759`），本文不重复
- 没有跑任何 Unity / 任何 `-executeMethod` / 没有动 git / 没有改任何既有文件

---

## 三、还没查清的

1. 🔴 **`esc`/`ecstasy` 之外的「只出现在 desc、不在 `keywords`」的词**：本次对账只比了 `keywords` 数组，而引擎另有 `CollectTriggerOps` 从 `desc` 收正文那一支（`CardDef.cs`）。⇒ **#18 的「100 张缺 trait」是上界还是净缺口，没查**（可能有一部分靠 `desc` 前缀已经接上了）。
   判据入口：`工具/ruleprobe.sh`（离线探针）+ `RuleCore.UnimplementedKeywords()`。
2. **先攻 `initiative` 到底是不是有效值**：真值里 1,190 张 = 20、2 张教程督军 = 30。**没查到**「原版在哪读它、20 是不是「无值」哨兵」—— 所以 #3（督军 `manaCost` = 2）也一样：**那个 2 是哨兵还是真值，没查**。判据入口：`d:/2/tools/decomp_full/SupportMethods__GetPlayerGoesFirstWithInitiative.c`（本次只读到我们自己的注释，没读原件）。
3. **兵种（`subtype`）在不在别的资产里**：`RawCardScript` 没有该字段、`cardRaceTextReferenceId` 全空 ⇒ **没查到**它藏在哪（可能是另一只 SO / I2 词条 / 卡预制体）。⇒ #19/#20 的「❌ 给不了」是**本包给不了**，不是「原版没有」。
4. **#21 那 187 张自造 id** 里，有多少是「快照之后新增」、有多少是「原版本来就没有 id（token / 环境卡）」—— **没分开数**。
5. **#22 那 102 张**的旧表 id **是否 100% 错** —— 只知同源旧表在重叠的 873 条里错了 851 条；**这 102 张一条都没逐张验过**。
6. **#16 天赋那 7 个查不到同名卡的**：真值里 `CardTraits.talent` 在，但**「天赋名 → 那张战术卡」那一跳**（`relatedCard*` / `cardAbilities` / `choicesDefinition`）**没查**。
7. **#25 进攻卡**：真值里 `uniqueId ↔ cardName` 全表在手，**但没验**它与 `gen_offensive_cards.py` 那个 `m_FileID=4` 的 PPtr 能不能对上（`PathID` 是否落在同一只包的 externals 命名空间里）。

---

## 四、顺手发现的东西（不在三个域内，只报不判）

1. 🔴🔴 **`cardRarity` 是 1..5 的整数，真值里有**（分布 2:315 / 1:314 / 3:226 / 4:202 / 5:131 / 0:6）—— 而我们的稀有度是**靠 PnP 卡图宝石取色**判的（`工具/gen_cards_engine.py:90` 的 `RARITY_BY_FACTION` + `:152` 的 `RARITY_UNLISTED`，那张表自己写着「这是**卡名当 id** 这个根病第三次咬人」）。⇒ **这是一条现成的真值，能把整族稀有度特判砍掉。**
2. **`keyword`（单数、`Keyword` 枚举、整数）字段全池都有**（0:644 / 5:372 / 10:123 / 15:21 / 35:10 / 25:8 / 30:7 / 40:7）—— **不知道它是什么语义**（`RawCardScript.cs:39` 只有 `public Keyword keyword;`）。**没查**。若它是「默认攻击方式（近战/远程）」之类，可能又是一条真值。
3. **`cardType` 只有 3 个值**（0:540 / 10:60 / 20:592）+ `spellType` 12 个值 —— 我们卡表 `type` 是 `unit/tactic/hero/defence` 四值（`BattleDriver` 那一侧）。**两者的映射关系没核**。
4. **`startingTroopsInPlay` / `startingCardInHand`（2）**：`startingTroopsInPlay` 是**数组**（不是单个 PPtr）—— 交接文档把它记成「2 / 见文件」；真值里它是列表，**没数**。
5. **`alternateArts` 只有 2 张卡非空**，而我们的异画记忆档写「本地有 7 张」—— **两处口径没对过**。
6. **`card_ids.json` 的 `note` 至今仍写着「自建：PnP 卡表编号 = 游戏卡 ID 偏移 0」**，但 `mapping` 已在 2026-10-13 重建（935 真值 + 123 旧值）—— **`note` 与 `mapping` 已经不一致**（#22 的成因就在这）。
