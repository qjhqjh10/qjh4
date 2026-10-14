# 旧卡 id 字面量残留 · 普查（2026-10-14）

> 扫描脚本：`Unity/工具/_scan_stale_cardids.py`（可复算，见 §五）· 全量命中 TSV：`_tmp_view/stale_ids.tsv`
> 姊妹件：`工具/_scan_stale_cardids.py --filecheck`（集合级文件体检）· `--dump x.json`（机器可读全量）

## 〇、方法

| 项 | 内容 |
|---|---|
| **旧池** | `_tmp_view/cardid_bak_1013/cards_engine.json`（1126 张，换 id 前）· **旧表** 同目录 `card_ids.json`（996 条） |
| **新池** | `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（1126 张）· **新表** `Unity/数据/游戏数据/card_ids.json`（1058 条） |
| **id 关系** | 两池都 1126 张：**193 个旧 id 消失 / 193 个新 id 出现**（同名搬迁）· **762 个 id 同号换卡** · **171 个没动** |
| **匹配** | 正则取 token（`SOR72` / `UM_Angels_of_Death` / `#HB_BL34`），**整 token ∈ 候选 id 全集（1329）** 才算命中 ⇒ `SOR1` 不会命中 `SOR12`；全库零命中 = 报「没查到」 |
| **判「本意是哪张卡」** | 取命中行 ±200 字符，归一化（去非字母数字→小写）后找**旧卡名 / 新卡名**（整名 → 名前 2 词 → 词长 ≥7 的单字）。旧名命中且新名不命中 ⇒ `STALE`；反之 `OK`；都不中 ⇒ `?` |
| **代码 / 注释判别** | 逐字符状态机（C# 的 `//` `/* */` `"…"` `@"…"`；Python 的 `#` / 三引号）⇒ `str`（活字符串）/ `bare`（裸标识符）/ `comment` |
| **「换 id 那趟动过没有」** | `git diff -U0 601005b^ -- <file>`（第二十六会话收口提交 = 换 id 那趟）⇒ `touched` / `kept`。⚠️ 只作旁证，**不进判级** |
| **已知误报源** | ① **同性名撞车**：新池有 187 个自造 id，`Terminator Champion` 同时挂在 `BL29`(真值) 与 `EC_Terminator_Champion`(自造) ⇒ 按卡名反查会认错；② id 是**置换**，逐行文本比对会把「换过 id 的行」和「没换过的行」配到一起（`--filecheck` 就是为此写的） |

## 一、总览

| 范围 | 扫了 N 个文件 | 命中(非「同卡」) | 真残留·功能性(§二A) | 真残留·文本级(§二B) | 判不了(§三) | 已换过(正常) | 新 id(新表独有) |
|---|---|---|---|---|---|---|---|
| `Assets/**/*.cs` | **389** | 976 | 0 | 416 | 445 | 17 | 98 |
| `工具/**/*.cs` | **10** | 98 | 0 | 30 | 56 | 6 | 6 |
| `工具/**/*.py` | **216** | 1062 | **34** | 164 | 532 | 192 | 140 |
| `资料/**/*.md` | **1588** | 11991 | 43 | 3401 | 7672 | 371 | 489 |
| `项目任务.md`+`CLAUDE.md` | **2** | 13 | 0 | 3 | 6 | 3 | 1 |
| 合计 | 2205 | 14140 | **77** | 4014 | 8711 | 589 | 734 |

> 「真残留」只收 `STALE`（同一处**并排写着旧卡名**）⇒ **本意必是那张旧卡**。`?`（旁边没有任何卡名）**一律不猜**，单列 §三。
> 文档那 4 列**不是判决**（md 全是散文，`str/comment` 不分），只是给下一会话一个量级。

## 二、🔴 真残留（本意是那张卡、现在指错了）

### 二·A 功能性（**裸 id 键**，会真被查到）—— **代码里 34 条**（全在 `工具/`）· 文档里另 43 条见 §四

| 文件:行号 | 字面量 | 旧表卡名 | 新表卡名 | 上下文一句话 |
|---|---|---|---|---|
| `工具/gen_trait_dict.py:48` | `AM32` | Chimera | Chimera | `('AM32','Chimera','duty')` —— ★ 见下方「六·1」 |
| `工具/gen_trait_dict.py:49` | `AM39` | Valkyrie | Born Soldier | `('AM39','Valkyrie','duty')` |
| `工具/gen_trait_dict.py:50` | `DA18` | Ravenwing Speeder | Inner Circle Companion | trait 表 |
| `工具/gen_trait_dict.py:51` | `DA30` | Ravenwing Talonmaster | Deathwing Strikemaster | trait 表 |
| `工具/gen_trait_dict.py:53` | `SOR5` | Morvenn Vahl | Battle Sister | trait 表 |
| `工具/gen_trait_dict.py:54` | `SOR9` | Battle Sister | Preacher | trait 表 |
| `工具/gen_trait_dict.py:57` | `SOR21` | Hospitaller | Death Cult Assassin | trait 表 |
| `工具/gen_trait_dict.py:59` | `SOR29` | Imagifier | Ephrael Stern | trait 表 |
| `工具/gen_trait_dict.py:61` | `SOR33` | Ephrael Stern | Saint Celestine | trait 表 |
| `工具/gen_trait_dict.py:62` | `SOR36` | Celestian Superior | Exorcist | trait 表 |
| `工具/gen_trait_dict.py:63` | `SOR39` | Immolator | Prayer | trait 表 |
| `工具/gen_trait_dict.py:64` | `SOR71` | Zephyrim Superior | Adelaide the Serene | trait 表 |
| `工具/gen_trait_dict.py:46` | `AM32` | Chimera | Chimera | 用法注释里的 `['AM32','AM39',...]` 举例 |
| `工具/import_original_art.py:841` | `AM5` | Ursula Creed | Shock Trooper | `ART_ID_ALIAS`（`'AA_HB'`+图名里写着 Ursula Creed） |
| `工具/import_original_art.py:842` | `BL1` | Abaddon the Despoiler | Chaos Spawn | 同上（图名 Abaddon the Despoiler） |
| `工具/import_original_art.py:844` | `SAU1` | Imotekh the Stormlord | Canoptek Scarab | 同上（图名 Imotekh the Stormlord） |
| `工具/import_original_art.py:845` | `GOF3` | Ghazghkull Thraka | Bomb Squig | 同上（图名 Ghazghkull Thraka） |
| `工具/import_original_art.py:843` | `DA3` | Azrael | Azrael | 同上（图名 …Azrael_v2）—— id 已搬走（新表无 `DA3`） |
| `工具/import_original_art.py:847` | `UM_Lieutenant_Titus` | Lieutenant Titus | Lieutenant Titus | 同上（图名 Lieutenant Titus） |
| `工具/ruleprobe/FaithProbe.cs:248` | `SOR27` | Canoness | Penitent Engine | `Scenario(pool,"SOR27",0)` 实战夹具 |
| `工具/ruleprobe/FaithProbe.cs:249` | `SOR27` | Canoness | Penitent Engine | 阈值 2 |
| `工具/ruleprobe/FaithProbe.cs:250` | `SOR27` | Canoness | Penitent Engine | 阈值 6 |
| `工具/ruleprobe/FaithProbe.cs:251` | `SOR11` | Crusader | Dominion | 阈值 2 |
| `工具/ruleprobe/FaithProbe.cs:252` | `SOR11` | Crusader | Dominion | 阈值 3 |
| `工具/ruleprobe/FaithProbe.cs:253` | `SOR68` | Sacred Rose Sister | Ministorum Priest | 阈值 4 |
| `工具/ruleprobe/FaithProbe.cs:254` | `SOR12` | Preacher | Retributor | 阈值 4 |
| `工具/ruleprobe/FaithProbe.cs:255` | `SOR12` | Preacher | Retributor | 阈值 5 |
| `工具/ruleprobe/FaithProbe.cs:256` | `SOR72` | **Adelaide the Serene** | Castigator | 阈值 6（简报点名的两条之一） |
| `工具/ruleprobe/FaithProbe.cs:257` | `SOR40` | **Paragon Warsuit** | Inspiring Oratory | 阈值 6（简报点名的两条之一） |
| `工具/ruleprobe/WillRun.cs:537` | `SOR28` | Dominion Superior | Zephyrim | `new[]{ "SOR28","SOR36" }` 两张一起扫 |
| `工具/ruleprobe/WillRun.cs:537` | `SOR36` | Celestian Superior | Exorcist | 同上 |
| `工具/ruleprobe/NonKwProbe.cs:403` | `GOF85` | Unleashed TramplaSquig | Snakebite Nob | `c.Id == "GOF85"` 现存唯一一处 |
| `工具/_fix_a6_descZh.py:26,47,53,55,56,67,73` | `EC30` `UM_Tactical_Doctrine` `TL80` `UM_Heavy_Intercessor` `UM_Outrider` `TL79` `ASH_Bright_Lance_Vyper` | 同左列（7 张自造 id 卡） | 同左（**id 已搬走**） | 一次性脚本，**再跑会把描述写错卡** |
| `工具/_fix_a7_talent.py:33,38` | `TL80` `DA3` | Psychophage / Azrael | 同左（id 已搬走） | 同上 |
| `工具/_verify_fixes.py:32,36` | `ASH_Autarch` `UM_Primaris_Chaplain` | Autarch / Primaris Chaplain | 同左（id 已搬走） | 复验拼版脚本，会渲错卡 |
| `工具/gen_cards_engine.py:542` | `AM3` | Lord Solar Leontus | Lord Solar Leontus | 仅注释里的 `heroId:"AM3"` 举例 |

> 34 条**全部在表里**（同族的短行合并成了一行：`_fix_a6_descZh.py` 7 条 · `_fix_a7_talent.py` 2 条 · `_verify_fixes.py` 2 条）。
> 逐条（含行号）→ TSV（筛 `barelit=1, verdict=STALE`）。
> ⚠️ `gen_icon_plan.py:394/439/453`（`EC24`/`SOR44`/`TAU74`）**经集合级复核为「正常」**，不是残留 —— 见 §六·2。

### 二·B 文本级（id 与旧卡名**并排写着**，但落在字符串 / 注释里）—— 610 条 · 52 个文件

| 文件 | 条数 |
|---|---|
| `Assets/RuleEngine/Editor/RuleEngineTest.cs` | 120 |
| `工具/gen_icon_plan.py` | 82 |
| `Assets/RuleEngine/Core/EffectResolver.cs` | 47 |
| `Assets/RuleEngine/Core/CardDef.cs` | 39 |
| `Assets/RuleEngine/Core/EffectText.cs` | 39 |
| `Assets/RuleEngine/Core/RuleCore.cs` | 36 |
| `工具/_apply_kw_misextract.py` | 25 |
| `Assets/CardPresentation/Editor/CardFaceProbe.cs` | 23 |
| `Assets/CardPresentation/Core/CardText.cs` | 15 |
| `Assets/RuleEngine/Editor/RuleEngineTest_S23_Parse.cs` | 15 |
| `工具/ruleprobe/WillRun.cs` | 13 |
| `工具/ruleprobe/PlayProbe.cs` | 11 |
| `工具/gen_cards_engine.py` | 11 |
| `Assets/RuleEngine/Core/WhenEvent.cs` | 10 |
| `Assets/RuleEngine/Core/Aura.cs` | 9 |
| `Assets/CardPresentation/Editor/IconSetup.cs` | 8 |
| `工具/check_pnp_cards.py` | 8 |
| `Assets/RuleEngine/Core/CreatePool.cs` | 7 |
| 其余 34 个文件 | ≤6 每条 |

> 典型形状（会真骗到人）：`RuleEngineTest.cs:4404` `CheckTrue(tac != null, "夹具：卡池里有 Dynamic Offensive（TAU54）")`
> —— 断言文案点名 `TAU54`，而 `TAU54` 现在是 `Razorshark Strike`。**断言照样绿**（它按卡名找），但读的人会认错卡。
> 全量逐条 → `_tmp_view/stale_ids.tsv`（筛 `verdict=STALE, barelit=0`）。

## 三、⚠️ 判不了（需人工看）—— 1033 条 · 58 个文件

| 文件 | 条数 |
|---|---|
| `工具/gen_icon_plan.py` | 367 |
| `Assets/RuleEngine/Editor/RuleEngineTest.cs` | 80 |
| `工具/_apply_kw_misextract.py` | 74 |
| `Assets/CardPresentation/Editor/IconSetup.cs` | 57 |
| `Assets/CardPresentation/Shell/CampaignData.cs` | 50 |
| `Assets/RuleEngine/Editor/RuleEngineTest_S14.cs` | 29 |
| `Assets/RuleEngine/Core/EffectText.cs` | 28 |
| `Assets/RuleEngine/Core/EffectResolver.cs` | 27 |
| `工具/_fix_a6_descZh.py` | 23 |
| `Assets/RuleEngine/Core/CardDef.cs` | 20 |
| `工具/ruleprobe/FaithProbe.cs` | 19 |
| `Assets/CardPresentation/Core/CardText.cs` | 16 |
| `工具/ruleprobe/WillRun.cs` | 16 |
| `工具/_verify_fixes.py` | 16 |
| 其余 44 个文件 | ≤15 每条 |

- **为什么判不了**：id 是**置换**。同一个字面量 `UM78`，换 id 前是 `Chapter Champion`、现在是 `Heavy Intercessor`；
  行里既没写旧名也没写新名 ⇒ **看不出写的人当时想的是哪张**。⛔ 这里**一条都没有猜**。
- **该谁看**：`gen_icon_plan.py` 那 367 条几乎全在一个**按卡 id 键的图标计划表**里 —— 它整表用
  `--filecheck` 复过（§六·2），**整表正确**，可以整批跳过；真正要看的是「行里只有 id、没有别的证据」的那些，
  优先 `RuleEngineTest*.cs`（夹具/哨兵）与 `CampaignData.cs`（战役节点图）。
- 全量 → `_tmp_view/stale_ids.tsv`（筛 `verdict=?`）。

## 四、文档里的（只列命中位置，**不判**）—— 11991 处 / 311 个文件

| 文件 | 命中 |
|---|---|
| `资料/卡面图标_对照与缺口.md` | 2941 |
| `资料/说明书/03_卡牌/卡牌定义与卡组数据.md` | 2281 |
| `资料/历史/A表已收口_第十四会话.md` | 239 |
| `资料/PnP卡图_逐张对账_0915.md` | 203 |
| `资料/普查产出_1018/V-PACK_抽卡包采集.md` | 192 |
| `资料/阶段二_锻造厂与战役页_原版规格.md` | 171 |
| `资料/任务_卡组卡牌战斗.md` | 156 |
| `资料/历史/A表已收口_第二十会话.md` | 153 |
| `资料/原版预组牌_核对.md` | 144 |
| `资料/普查产出_第二十会话/C1_卡表数据与文本订正.md` | 143 |
| `资料/普查产出_第二十会话/W7_E5截断订正.md` | 132 |
| `资料/普查产出_1017/W_B26_卡id回填.md` | 127 |
| `资料/普查产出_第十四会话/W_中文徽记缺口批.md` | 111 |
| `资料/普查产出_1018/V-ID_卡id命名空间复核.md` | 106 |
| `资料/普查产出_0918/11⑤_ResolveTargets退化支_普查.md` | 99 |
| 其余 296 个文件 | ≤98 每个 |

**已确认待改的 4 处**（`项目任务.md` 里，按同一处又写了旧卡名 ⇒ 是 `STALE`，不是 `?`）：

| 文件:行号 | 字面量 | 旧表卡名 | 新表卡名 | 说明 |
|---|---|---|---|---|
| `项目任务.md:530` | `BL57` | Hosts of Chaos | Hosts of Chaos | id 已搬走 |
| `项目任务.md:530` | `DA59` | Unwavering Zeal | Unwavering Zeal | id 已搬走 |
| `项目任务.md:530` | `GOF61` | Gretchin Tower | Rukkatrukk Squigbuggy | 同号换卡（**最危险的一种**） |
| `项目任务.md:437` | `GOF90` | Beastboss on Squigosaur | Wurrboy | 判不了（行里没有卡名） |

## 五、本次扫了什么、没扫什么

- **扫了**：`Unity/MyGame/Assets/**/*.cs`（389）· `Unity/工具/**/*.cs`（10）· `Unity/工具/**/*.py`（216）·
  `Unity/资料/**/*.md`（1588）· `项目任务.md` · `CLAUDE.md`。
- **跳过**：`bin/` `obj/` `__pycache__/` `.git/` `_tmp_*`，以及**全部 `.json` 数据文件**（`cards_engine.json` 等，它们本来就是判据本身）。
- **关键词 / 判据**：候选 id 全集 = 旧池 ∪ 新池 ∪ 旧表 ∪ 新表 = **1329** 个；
  token 正则 `(?<![A-Za-z0-9_#])(#?[A-Za-z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)*)`。
- **复算方式**：
  ```bash
  python -I -X utf8 d:/4/Unity/工具/_scan_stale_cardids.py --tsv out.tsv --dump out.json
  python -I -X utf8 d:/4/Unity/工具/_scan_stale_cardids.py --filecheck      # 集合级文件体检
  ```
- **没扫的**：`.json` / `.tsv` / `.asset` / `.unity` / 场景 / 预制体里的 id 字面量（不在本次范围）。

## 六、顺手发现的东西（**未动手改**，交调度台分流）

1. 🔴 **`工具/gen_trait_dict.py` 整张表是「旧 id + 新卡名」混搭**（该文件是 **git 未跟踪的新文件**，第一行注释就写着「真值表只采到 4.3%」）。
   形状 `('卡id', '卡名', 'trait')` —— **卡名是新的、id 是旧的**，34 条里 12 条属此类。用它生成的 trait 词典会**挂错卡**。
   同族：`import_original_art.py` 的 `ART_ID_ALIAS`（6 条，§二A）—— 而 `资料/卡牌id_换真值_落地记录.md` §三·4 说「22 个键已换算」，
   **实际这个表还有 6 条没换**（文件被后续轮次改过？）⇒ 与落地记录**打架**，请裁定。
2. ✅ **判据升级（建议写进交接）**：id 是**置换**，**逐行文本比对会骗人** —— 实测 `gen_icon_plan.py:394` 换 id 前后
   行文本都是 `("EC24","[Attack]")…`（撞车），行级判据说「没动过」。**正确判法是集合级**：
   把 pre 版该文件的「id 多重集」整体过一遍「旧 id→同卡新 id」映射，再和现版比 ——
   `gen_icon_plan.py` 的 `[Attack]` 表 119 条**只有 1 条「差」**，且那 1 条是**卡名撞车**（`Terminator Champion` 同时挂在 `BL29` 真值 与 `EC_Terminator_Champion` 自造）⇒ **整表其实是对的**。已实现为 `--filecheck`。
3. ⚠️ **自造 id 与真值 id 会撞名**：新池 1126 张里 **187 张是自造 id**（`UM_Angels_of_Death` 那类），
   与真值 id **同池共存**、且有**同名**情形（上条）⇒ 任何「按卡名反查 id」的脚本都会**静默取错**（`setdefault` 取先到者）。
   建议：按卡名反查一律**显式排除自造 id**，或改用「id → 名」单向。
4. ⚠️ **`.cs` 里 id 只在注释里、代码按卡名走**是主流形状（`RuleEngineTest.cs` 120 条 STALE 里绝大多数如此）⇒
   这批**不影响运行**，但**断言文案会点名错的卡**（§二B 的 `TAU54` 例），人读时会被带偏。
5. 📌 `工具/_scan_stale_cardids.py` 是**本次新增**的文件；`_tmp_view/stale_ids.tsv` / `.json` 是它的**临时产物**，可随时 `--tsv/--dump` 重出。
