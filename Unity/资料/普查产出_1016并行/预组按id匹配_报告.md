# 预组「按 id 直查」修缺卡（执行代理 A13）

> 件：给预组的匹配机制补 **t0 = 按原版卡号直查**（同文件 `hero_match()` 早有同族先例）。
> ⛔ **卡名一个字没动**（用户 2026-10-16 拍板；判据 → `资料/已知的坑.md:4965,4977`）。
> 产物只能由脚本重跑生成 —— 本件重跑了 `gen_prebuilt_decks.py`（两份产物）与 `check_prebuilt_decks.py`。
> 数据快照（本报告所有数字都钉在这四个文件上）：`cards_engine.json` sha1 `869935d8…`（count **1127**）·
> `card_ids.json` sha1 `e88429a7…`（mapping **1310**）· `decklists.json` sha1 `c0ed8ab6…` ·
> `warlord_ids.json` sha1 `34c56320…`。两次连跑产物**逐字节相同**（幂等，可复现）。

---

## ① 结论

**给 `match()` 补了「先按 id 直查（t0）、查不到才回落名字」，两边（生产脚本 + 核对脚本）同批改完** ——
预组池 103 副里原本缺卡的 **10 副全部补齐**，`complete=false` **12 → 2**（剩下的 2 副 = C 类真缺卡）。

🔴 **但交接简报里的基线数（16 副）与现读不符 —— 那是【盘上那份过期产物】的数，不是现读**：

| 口径 | complete | complete=false | 说明 |
|---|---|---|---|
| 盘上那份（交接时看到的 `prebuilt_decks.json`，第三十一会话 `ee784b5` 生成） | 87 | **16** | A 类 4 + B 类 10 + C 类 2 ⇒ **简报的 16 是这一格** |
| **现读基线**（**未改任何代码**、用现池现代码跑一遍，2026-10-16 21:44） | 91 | **12** | B 类 10 + C 类 2 —— **A 类那 4 副已经好了**（原因见 ⑤，**不是本件改的**） |
| **改后**（本件，21:5x） | **101** | **2** | 只剩 C 类：`DA_SK_4`(4/12) · `DarkAngelsDeck7`(14/30) |

- **B 类 10 副逐副（`size` 全补满、`complete` 全 true）**：
  `OrksDeck2` 28/30→**30/30** · `OrksDeck5` 29/30→**30/30** · `ORK_SK_2` 11/12→**12/12** ·
  `EmperorsChildrenDeck5` 28/30→**30/30** · `EC_SK_3` 11/12→**12/12** · `SautekhDeck1` 28/30→**30/30** ·
  `SAU_SK_1` 11/12→**12/12** · `SautekhDeck2` 27/30→**30/30** · `SautekhDeck8` 28/30→**30/30** · `SAU_SK_2` 11/12→**12/12**。
- **核对器（236 副口径，`check_prebuilt_decks.py`）**：`full` **199 → 225**；`miss-known` 6 → **1**；
  `missing_unknown` **9（不变）**；suspect 53 → 32。翻转 26 副**全是 缺→可拼**，**0 副 可拼→缺**。
- **逐处清点**（236 副 × **5157** 处卡引用，改动前后各跑一遍 matcher 对比 —— 这是「有没有回归」的硬判据）：

  | 类别 | 处数 | 说明 |
  |---|---|---|
  | 同一张卡、档位升到 t0 | 5024 | 卡不变（原来靠 t1~t5 名字接上的那些） |
  | 完全没变 | 72 | 全是 `miss-unknown`（`DA67–DA74` + 2 个 GUID） |
  | 原来 `miss-known` → 现在 t0 命中 | **56**（36 组「副+卡号」） | 这就是要修的那一类 |
  | 🔴 原来**错配到另一张卡** → 现在接回正确那张 | **5**（4 组） | `EC26` 被 t5 接成 `EC25`（`EC_SK_1` ×1 · `EC_SK_3_2` ×1 · `EmperorsChildrenDeck1` ×2 · `EmperorsChildrenDeck4` ×1，字段是真名对真名：`Terminator Champion` vs `Terminator`）—— **同一个 bug 的静默面** |
  | 命中 → 缺（回归） | **0** | 无 |

- **教程那 12 副**（顺带一起好，同一处代码）：`complete` 8 → **9**；`stage3 ai` 24→**28** 张 ·
  `stage5 ai` 27→**30** 张（并变 complete）。教程牌**顺序未动**（只插进原来缺的位，`difflib` 逐段核过只有 `insert`）。
- 判定逻辑本身：**两个 miss 标记的原意没变**（`miss-unknown` = 卡号在 `card_ids.json` 里查不到；
  `miss-known` = 查得到名字、但名字与各弱档都对不上池里的卡）—— t0 只是**插在名字阶梯之前**。
  实证（真数据）：`DA67` → `miss-unknown`；`847acecf…`（表名 `Gauss Warrior (tutorial)`）→ **`miss-known`**（改后仍如实报出）；
  合成用例（表里塞一个池里没有的假号）→ `miss-known`。**没有把错藏起来**。

---

## ② 改动清单

| 文件:行号 | 一句话 |
|---|---|
| `Unity/工具/gen_prebuilt_decks.py:211-231` | 新增 `id_direct()`：**`match()` 与 `hero_match()` 共用这一处** id 直查（⛔ 不抄两份）；`want_type` 可选（督军传 `"hero"`） |
| `…gen_prebuilt_decks.py:193-202` | 新增 `missing_parts()`：产物 `missing` 是**格式化字符串**，取张数/档位一律走它（带 assert，格式变了就炸） |
| `…gen_prebuilt_decks.py:204-207` | 新增 `warlord_name_of_table()`：剥 `阵营前缀_Warlord_` / `Presale_`（原来这段在两处手写） |
| `…gen_prebuilt_decks.py:255-274` | **`match()` 补 t0**：name 阶梯之前先 `id_direct`，命中即返回 `("t0", 我们的卡 id)`；miss 判定顺序**一字未动** |
| `…gen_prebuilt_decks.py:316-345` | `hero_match()` 改吃 `id_direct()`；🔴 id 命中时**仍把「名字表与池里对不上」写进 note**（`id命中(名字表作 SW_Fenrir)`），⛔ 不让「凑上了」把缺陷盖掉 |
| `…gen_prebuilt_decks.py:418-430` | `resolve_cards()`：**跨阵营的 t0 命中 = 卡表缺陷 ⇒ 出声**（进 `ctx["warn"]`，`main()` 打印计数） |
| `…gen_prebuilt_decks.py:587-599` | 教程坏副的报数从「缺几段」改成「缺张数（按档位）」 |
| `…gen_prebuilt_decks.py:615-627` | 产物 note `incomplete` **就地更正**（原文举的 `UM6/UM8/…` 现读**池里全有**，那句不成立 —— 见 ⑥·2） |
| `…gen_prebuilt_decks.py:23-25 / 233-236` | 抬头与 `build_matcher` 注明「两边同批补 t0」「卡名没动」 |
| `…gen_prebuilt_decks.py:709-712 / 726-740` | 控制台：教程缺卡按张数、`cross-faction id hits` 计数、`missing cards by tier` |
| `Unity/工具/check_prebuilt_decks.py:33-42` | 抬头口径加 t0（与生产脚本同一条规则） |
| `…check_prebuilt_decks.py:89-93` | 同名 `warlord_name_of_table()`（与生产脚本逐字一致） |
| `…check_prebuilt_decks.py:123` | 加 `by_id` 索引（t0 用） |
| `…check_prebuilt_decks.py:126-140` | **`match()` 补 t0**（返回值形状不变：`(tier, 池里的键, 表名)`） |
| `…check_prebuilt_decks.py:182-200` | `hero_check()` **对齐成 id 优先**（与 `hero_match()` 同口径），并保留「名字表作 X」的说明 |
| `…check_prebuilt_decks.py:240-244` | 「两种写法」表把 t0 里**需要人看一眼**的收进来（卡名不同 / 那张卡不在本副阵营） |
| `…check_prebuilt_decks.py:259 / 282` | TSV 新增 `t0` 列 |
| `…check_prebuilt_decks.py:315-322 / 341-342` | MD 口径加 t0 说明；总览新增「用到 t0」行，「只用 t1」那条改成**排除 t0** |
| `…check_prebuilt_decks.py:439 / 460` | 表里空名字的兜底文案；控制台加 `t0_occurrences` |
| 产物 `RuleEngine/Resources/prebuilt_decks.json` · `tutorial_decks.json` | **只由脚本重跑生成**（未手改一个字节） |

**⚠️ 行尾核对（`git diff --numstat` + 二进制数 `\r\n`）**：
`gen_prebuilt_decks.py` **116/16**（文件 745 行）· `check_prebuilt_decks.py` **59/12**（468 行）·
`prebuilt_decks.json` **81/104**（4501 行）· `tutorial_decks.json` **11/11**（684 行）——
**四个文件改前改后 `CRLF=0`（纯 LF）**，diff 行数远小于文件行数 ⇒ **没有翻行尾**（HEAD 版也是 LF，
`git show HEAD:<路径> | file -b -` 无 CRLF 标记、二进制数 `CRLF=0` 一致）。

---

## ③ 接线清单（白名单外该改的，⛔ 本件没动）

| 谁 | 该改什么 | 为什么 |
|---|---|---|
| 主对话（正本） | 待办里那条「预组缺卡 16 副」的**基线数要换成「盘上那份 16 / 现读 12 / 改后 2」**；并记「A 类已经被池的 id 真值换入解决」 | 简报的 16 来自**过期产物**，照着它排活会重复算一遍 |
| 卡牌线 T01/台账（`_收口台账_第四十二会话.md:268,369-371`、`卡牌线T01_报告.md:51,115,141`） | 「这 5 个卡号压着 10 副预组」这条**代价已经没了** —— `GOF20` / `EC30` / `SAU4` / `SAU6` / `SAU8` 现在**不阻塞任何一副**（机制侧解决）；`GOF20` 卡名那笔（`Veteran Stormboy` vs `Veteran Flyboy`）**仍是待拍板，但不再是「压着牌」的理由** | 卡名口径与匹配机制是两件事，别混成一条 |
| `check_prebuilt_decks.py:22-26`（抬头那两行现读数） | 写的是 `card_ids.json` **996** 条、卡池 **1126** 张；**现读 = 1310 / 1127** | 属「文档数」不属「核对口径」⇒ 简报限定「只在核对口径确实要改时才动」，**我没动**。要改请主对话拍（1 行） |
| 另一笔账（不在本件） | `DA67–DA74` 8 个卡号（C 类）· 6 个 `*_BOT_FTUE_EASY` 的 GUID 督军 · 3 副教程的 GUID 卡 | 判据在池/表之外，见 ⑤ |

---

## ④ 判据与出处

| 结论 | 出处 |
|---|---|
| 卡名**一个字不动**（本件只修机制） | 用户 2026-10-16 拍板 → `资料/普查产出_1016并行/_收口台账_第四十二会话.md:268,369-371`；判卡名的既定口径 → `资料/已知的坑.md:4965,4977` |
| 「先按 id 直查、查不到才回落名字」在**同文件里已有先例** | `gen_prebuilt_decks.py:316-345`（`hero_match()`，2026-09-25 立的；本件把它抽成共用 `id_direct()`） |
| **两边必须同批改**（生产脚本 ↔ 核对脚本是同一份规则的两份拷贝） | `gen_prebuilt_decks.py:21-25`（抬头原文）+ `build_matcher` 抬头 |
| id 直查覆盖面 / 安全性实测 | 本件现测（`cards_engine.json` sha1 `869935d8…`）：236 副 **5157** 处卡引用里 **id 命中 5085 处（98.6%）**；命中处**阵营不符 0 处**、**指向督军 0 处**；池里 id **1127/1127 互不相同** ⇒ t0 **不按阵营过滤**（判据写在 `id_direct` 的 docstring） |
| 5 个卡号「池里有同 id 的卡、只是卡名写法不同」 | `card_ids.json` 现读：`GOF20`→`Veteran Stormboy`（池 `Veteran Flyboy`）· `EC30`→`Emperor's Children Maulerfiend`（池 `Maulerfiend`）· `SAU4`→`Gauss Warrior`（池 `Gauss Reaper Warrior`）· `SAU6`→`Tesla Immortal`（池 `Tesla Carbine Immortal`）· `SAU8`→`Gauss Immortal`（池 `Gauss Blaster Immortal`）；三来源对账见 `_收口台账_第四十二会话.md:247-251` |
| t0 的档位语义（为什么新开一档而不是并入 t1） | 产物/TSV 的 `t1` 那条写着「严格同名同阵营」⇒ 拿 id 命中冒充 t1 就是**把两种证据混一档**；t0 单列后「两种写法」表只留需要人看的 |
| 产物/TSV 的形状没变（C# 读得动） | `CardPresentation/Shell/PrebuiltDecks.cs:41-71`（`JsonUtility` 按名匹配；本件**没加/没删字段**）；教程那份的 note 字段全仓 C# **0 引用**（`grep -rn "incomplete" Assets --include=*.cs` 无命中） |
| 幂等 / 可复现 | 连跑两次 `gen_prebuilt_decks.py` → 两份产物 sha256 不变（`7f0cee65…` / `caaab751…`）；不引入随机、不依赖字典遍历顺序（t0 是单次 `dict.get`；`warn` 按卡组/卡序 append） |
| `heroNote` 是真·诊断字段（改它不影响运行时） | `PrebuiltDecks.cs:51` 注释「诊断用」；全仓除声明外无引用 |

---

## ⑤ 没查清 / 没做的

1. 🔴 **A 类那 4 副（`SW_SK_1` / `SW_SK_2` / `SpaceWolvesDeck1` / `SpaceWolvesDeck3`）—— 本件没改它，它已经好了**：
   现读 `heroId` 就是字面上的 **`SW3` / `SW2`**（`decklists.json`），池里 `SW3` = **Ragnar Blackmane**(hero)、
   `SW2` = **Njal Stormcaller**(hero) ⇒ 按 id 解得出、`complete=true`。
   ⚠️ **盘上那份（过期产物）当时是 `heroId=""` + note `id指向非督军(unit)/(tactic)`** —— 说明**变的是池**
   （第三十三会话的卡 id 真值换入把 `SW3/SW2` 从 unit/tactic 换成了督军），**不是本脚本**。
   ⛔ **没查清**：`warlord_ids.json` 说 `SW3 = SW_Fenrir`、`SW2 = SW_Long Fang`（与池里名字对不上）；
   「这 4 副**该**配 Fenrir 还是 Ragnar」**没查**（本脚本照 `decklists.json` 的 id 解，**不替它裁**）。
   本件只保证**这件事看得见**：note 现在写 `id命中(名字表作 SW_Fenrir)`。
2. **C 类 2 副没动**：`DA_SK_4`（4/12）· `DarkAngelsDeck7`（14/30）—— 缺 `DA67–DA74` 共 8 个卡号 / **24 张**，
   全 `miss-unknown`（`card_ids.json` 里没有、池里也没有；`资料/原版预组牌_核对.md` 已记「本地没有 id→名的表能补」）。
   生成器控制台现在会打 `missing cards by tier = miss-unknown:24`。
3. **核对器里仍「缺」的 11 副**（= 225/236 之外那 11）：`DA_SK_4` · `DarkAngelsDeck7` ·
   3 副教程（`AeldariDeckTutorial` 缺 `7a02359776…`；2 副 `NecronsTutorial` 缺 `847acecf…`）·
   6 副 `*_BOT_FTUE_EASY`（🔴 **它们的 30 张卡全解析得出**，只卡在**督军**：`heroId` 是 6 个 32 位 GUID，
   **池里没有、`warlord_ids.json` 里也没有**）。
   ⛔ **没查**：那 6 个 GUID 督军在本地还有没有别的表（它们只出现在机器人/FTUE 那族；我**没**去翻 `d:/2` 的别的 SO）。
4. **生成器里仍 incomplete 的教程 3 副**：`stage3 player` / `stage3 ai`（`847acecf…` ×2，`miss-known`）·
   `stage5 player`（`7a02359776…` ×2，`miss-unknown`）。见 ⑥·3 有一条可能的二阶修法（**没做**）。
5. **没做**：给 t0 加阵营闸。理由：0 反例 + 加了反而有可能**静默丢牌**；改成「跨阵营即判缺」是**换语义**，要另拍。
   现读的防线是「`resolve_cards` 出声 + 核对器表里标 `[阵营]`」（两者都在本件里接上了）。
6. **没做**：`prebuilt_decks.json` 那份产物的**卡序**没被任何断言复检到「与原版 SO 逐位相同」这一层 ——
   本件只核了「新插进去的卡都在原来缺的位置」（`difflib` 逐段：只有 `insert`、无 `replace`/`delete`）。
7. **没做**（本件范围外，见 ③）：`check_prebuilt_decks.py` 抬头那两处过期现读数（996 / 1126）。

---

## ⑥ 顺手发现（不属本件，只报不改）

1. 🔴 **`warlord_ids.json` 与池里的督军名「大批对不上」** —— 预组池 103 副里 **22 副**的 `heroNote` 现在被标出来
   （12 个不同 heroId）：`SW3` 表写 `SW_Fenrir`（池 Ragnar Blackmane）· `SW2` 表写 `SW_Long Fang`（Njal Stormcaller）·
   `SOR1` 表写 `SOR_Arco Flagellant`（Erika Luminas）· `SOR2` 表写 `SOR_Dialogus`（Junith Eruita）·
   `SOR3` 表写 `SOR_Preacher`（Morvenn Vahl）· `SOR65` 表写 `SOR_Death Cult Assassin`（Celestian Sacresant Aveline）·
   `AM3` 表写 `AM_Warlord_Commissar Deckler`（Commissar **Denkler**，拼写）· `BL65` 表写 `BL_Presale_Abaddon`
   （Ghallaron the Pious）· `ASH76` 表写 `ASH_Jain Zar`（Lhykhis）· `SAU36` 表写 `SAU_Warlord_Nemesor Zhandrekh`
   （Nemesor **Zahndrekh**，拼写）· `TL74` 表写 `TL_Neurogaunt`（Terror of Vardenghast）· `UM65` 表写
   `UM_Presale_Marneus Calgar`（Chaplain Letharius）。⇒ 那张表**只能当第二判据**（与已记的 `A876` 同族）。
2. 🔴 **教程产物 note 里 `incomplete` 那段原文是错的** —— 它举的「`UM6/UM8/UM15/UM19/UM20/UM21/UM25/UM26/UM33/UM36/UM38`
   这类连续编号里的洞」，**现读这批 UM 号池里全都有**（`UM6` = Primaris Intercessor、`UM26` = Epistolary Librarian …），
   它们**从来不是**教程缺卡的原因（真原因全是 32 位 GUID 卡）。**已在生成器里就地更正**（保留更正痕）。
3. ⚠️ **一条可能的二阶修法（没做）**：`847acecf…` 在 `card_ids.json` 里叫 **`Gauss Warrior (tutorial)`**，
   而 `card_ids.json` 里 **`SAU4` 的值就是 `Gauss Warrior`** ⇒ 一条「**名字 → 表里的值 → 卡号 → 池**」的桥
   能再救教程 **2 副**（`stage3 player/ai`）。**没做**：它是**新机制**（且 `mapping.values()` 里有重名），要另裁。
4. ⚠️ 6 副 `*_BOT_FTUE_EASY` 是**低成本可赢**的：30 张卡全齐，**只差 1 个督军 GUID** ⇒ 一旦那 6 个 GUID 的
   督军找得到（别处 SO / 远端表），它们**立刻**从「缺」变「可拼」。
5. ⚠️ `check_prebuilt_decks.py` 抬头两处现读数过期：`card_ids.json` 现读 **1310** 条（写的 996）、
   池现读 **1127** 张（写的 1126）。本件按白名单约束**没动**（属文档数，不属核对口径）。
6. 📌 诊断字段的**边界**已确认：`heroNote` / `missing` / `problems` / 教程产物的 note **C# 侧都不读**
   （`PrebuiltDecks.cs:41-71` 只声明；`heroNote` 自带「诊断用」注释）⇒ 本件改这些字符串**不会动玩法**。
   反过来说：**改了这些字段也不会有任何界面反馈**，要让人看见得另接（不在本件）。

---

## 📋 可直接粘进总表的一行

**`A13` ✅ 预组缺卡按 id 直查已修**（`gen_prebuilt_decks.py` + `check_prebuilt_decks.py` **同批**补 t0，卡名未动）：
预组池 `complete` **91→101**、`complete=false` **12→2**（只剩 C 类 `DA_SK_4`/`DarkAngelsDeck7`），核对器 **199/236→225/236**、
零回归（5157 处逐处清点：0 处命中→缺；另修掉 5 处 `EC26→EC25` 静默错配）。
⚠️ **简报的「16 副」是盘上过期产物的数** —— 现读基线就是 12（A 类 4 副早已随池的 id 真值换入变 complete），
A 类**不是本件修的**；`warlord_ids.json` 与池的督军名对不上（12 个 id）已如实写进 `heroNote`。
