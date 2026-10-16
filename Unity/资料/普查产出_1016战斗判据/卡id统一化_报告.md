# 卡 id 统一化（2026-10-16）

> **一句话结论**：全池 1127 张卡的 `id` **已经就是原版真值**（1009/1009 可对上的都用了真值 `uniqueId`）；
> 「有 ID 的卡」这一档**没有需要统一的对象**。本次真正动的是 —— ① 报清「短 id vs 32 位 GUID」的裁定（**不是同一张卡，不合并**）；
> ② 把 9 个 `.cs` 里 **34 处陈旧 id 字面量**（注释与 1 条断言文案）改回当前卡池 id。
> 判据源 = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（1,195 个 `RawCardScript` 的 `uniqueId`）＋ 官方 `Card_Name/*`。

---

## 一、全仓普查表（每一处 id 使用：位置 / 用的是真值还是自造 / 要不要改）

现读口径：卡池 = `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（**1127 张**）；真值 = 快照 1,195 个 `uniqueId`。

| # | 位置 | 用的是哪种 id | 判定 | 改了吗 |
|---|---|---|---|---|
| 1 | **`cards_engine.json`（卡池本体）** | **1009 张 = 真值 `uniqueId`**（✅）；**118 张 = 自造**（SW/UM/DA） | ✅ **已统一**（见 §三·没改哪些） | 否 |
| 2 | `cards_engine.json` 的 `cardShortId` / `refNameId` 两列 | `cardShortId` 与真值**逐张相同（0 不符）**；`refNameId` 全池为空 | ✅ 一致 | 否 |
| 3 | **`数据/游戏数据/card_ids.json`**（`id→卡名` 桥表，1310 键） | 1009 键 = 池内真值；95 键 = 池内自造（118 的短/UM 形态）；185 键 = 真值但不在我们池（含 67 个变体 GUID）；**21 键 = 自造残留**（不在池、不在真值） | ⚠️ 桥表带**陈旧残留 + 同名歧义**（详见 §五） | 否（见 §五·1） |
| 4 | **`数据/游戏数据/card_id_truth_index.json`**（`名字→真值 id` 索引） | 键/值全是真值 id；但 `by_name` 继承了真值 `cardName` 的**陈旧值**（见 §六·1） | ✅ id 侧无误；⚠️ 名字侧有陈旧 | 否 |
| 5 | `数据/游戏数据/card_inventory_options.json` · `card_initiative.json` · `related_cards.json` · `talent_cards.json` · `warlord_goes_second.json` | 全部**按我们卡池 id**落表；现读**0 个池外 id** | ✅ 一致 | 否 |
| 6 | `数据/游戏数据/decklists.json`（原版 236 副预组） | `cardIds`/`heroId` 用**原版短 id**：5085 池内 · 41 真值但不在池 · 31 池外（`DA65`–`DA74` + 1 GUID） | ✅ 原版命名空间（桥=card_ids.json） | 否 |
| 7 | `MyGame/Assets/**/Resources/*.json`（`prebuilt_decks.json` / `tutorial_decks.json` / `tutorial_stages.json` / `card_icon_plan.json` / `card_vfx_by_card.json` / `voice_lines.json` / `related_cards.json` / `talent_cards.json` / `ability_anim_durations.json`） | 全部**用当前卡池 id**；现读各文件 **0 个池外 id** | ✅ 一致 | 否 |
| 8 | **`.cs` 里的 id 字面量**（`MyGame/Assets/**/*.cs`） | 活代码字符串：**0 处非池内 id**（改前 1 处见 §三）；**注释里的陈旧 id：34 处**（已改，见 §三） | ⚠️→✅（注释已改） | **是（34 处）** |
| 9 | `工具/gen_*.py`（`gen_cards_engine.py` / `gen_prebuilt_decks.py` / `gen_icon_plan.py` …） | 消费卡 id 的**活代码**：0 处非池内 id；**陈旧 id 只在注释**（如 `gen_icon_plan.py` 的 `BL60` / `TL79` / `ASH58` / `GSC60`） | ⚠️ 注释陈旧，**未改**（见 §五·2） | 否 |

**关键事实（复核命令都写在本节末）**：非 `.cs` 的**任何数据文件里，没有一个池外卡 id**（唯一例外是桥表 `card_ids.json` 的 21 个自造残留 + decklists 引用原版的 `DA65`–`DA74`）。

```bash
# ① 卡池 id 有没有非真值的（应只回 SW/UM/DA 那 118）
python -I -X utf8 -c "import json;P=json.load(open(r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json',encoding='utf-8'))['cards'];T={e['uniqueId'] for e in json.load(open(r'd:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json',encoding='utf-8')) if e.get('uniqueId')};import collections;print(collections.Counter(c['faction'] for c in P if c['id'] not in T))"
# ② .cs 里有没有「非注释、非池内」的卡 id 字面量（应回 0）
```

---

## 二、短 id vs 32 位 GUID 的裁定 —— **两张不同的 SO，⛔ 不合并**

### 结论
`AM51`（短）与 `44761bc257d1f1243ac5f6058459ede6`（32 位 hex）**不是同一张卡的两个写法，是两个不同的卡 SO**（**基准卡** vs 它的**附魔/先锋/教程变体**）。⇒ 按任务书 ② 的规则：**不合并**。

### 依据（逐条带出处）
| 证据 | 内容 |
|---|---|
| **快照里的 `m_Name` 后缀** | `AM51` = `T13 AstraMilitarum-Cadia Stands`；GUID = `T13 AstraMilitarum-Cadia Stands_enchantment`、`2 AstraMilitarum-Tempestus Scion_withVanguard`、`2 Goff-Makari the Grot_tutorial`、`SaimHann_war_Medreyal Ghaelyn_EASY` … —— **后缀就是「变体类型」** |
| **`cardShortId`** | 基准卡有编号（`AM51`=771）；**变体一律 `cardShortId=0`** |
| **`refNameId`** | 变体（`_withVanguard`）的 `refNameId` 指回**基准卡的 id**（`66486b3f…` 的 `refNameId=AM68`）⇒ 数据集自己承认「我是 AM68 的变体」 |
| **官方 `Card_Name/*`** | 变体**多数没有官方卡名条目**；而有官方条目的 GUID（`Skyborne Deployment` / `Dark Pact of Fate` / `Chosen of the Four` / `Master of Repentance`）**是真正的独立卡**（`cardShortId=0` 的 NC 卡），官方名与 `m_Name` 一致 |
| **现读的 105 个「多名 id」** | 其中 **74 个**是「短 id ＋ GUID」并存；这 **74 个 GUID 全是变体**（`_enchantment 66` · `_EASY 6` · `_withVanguard 1` · `_tutorial 1`）—— **无一例外** |

### 我们池里用哪一种
- 凡「短 id ＋ GUID 并存」的卡名（如 `Cadia Stands`），**我们池取的都是短 id（基准卡 `AM51`）**；变体 GUID **不在我们池**（现读 42 个这样的卡名，全部取短）。
- **113 张池内 GUID** 是**另一回事**：那些卡名的**唯一 id 就是 GUID**（基准卡本身就是 GUID 形态的 NC 卡，`m_Name` 形如 `Z-NC <阵营>-<名>`），官方 `Card_Name` 也建在**这个 GUID 键**上（例 `Card_Name/e59ac9e46f4ea804784e954c3b820e77` = `Skyborne Deployment`）⇒ **它们本来就是原版的 id，动不了也不该动**。
  🔴 这条与台账 `A2069`（`D2_18条红诊断` 的「113 张 GUID 是缺陷」其**前提被推翻**）一致 —— **A2052 的订正方向是对的**。

---

## 三、改了哪些（逐条：改前 → 改后 + 判据 + 复核方式）／ 没改哪些（为什么）

### ✅ 已改：9 个 `.cs`，34 处陈旧 id 字面量（**全是注释，外加 1 条断言文案**；零运行时影响）

判据 = **该注释点名的卡、在当前卡池里的 id**（现读 `cards_engine.json`）。旧写法要么是 A1758 换 id **之前**的旧池 id，要么把 **PnP 文件号**当成了 id。

| 文件 | 行 | 改前 → 改后 | 判据（注释点名的卡 → 池内 id） |
|---|---|---|---|
| `RuleEngine/Core/EffectText.cs` | 106/1282/2502/6195/6222 | `BL77` → `BL74` | `Spreading Corruption` = **BL74** |
| `RuleEngine/Core/EffectText.cs` | 7032 | `TL83` → `TL70` | `Norn Emissary` = **TL70** |
| `RuleEngine/Core/BattleContext.cs` | 464 | `BL77` → `BL74` | 同上 |
| `RuleEngine/Core/BattleContext.cs` | 862 | `TL82` → `TL69` | `Neurotyrant` = **TL69** |
| `RuleEngine/Core/CardDef.cs` | 2494 / 2507 | `TL83` → `TL70` | `Norn Emissary` |
| `RuleEngine/Core/CardDef.cs` | 2507 | `UM_Avenging_Zeal` → `UM40` | `Avenging Zeal` = **UM40** |
| `RuleEngine/Core/CreatePool.cs` | 62 | `TL82` → `TL69` | `Neurotyrant` |
| `RuleEngine/Core/EffectResolver.cs` | 2562 | `TL82` → `TL69` | `Neurotyrant` |
| `RuleEngine/Core/EffectResolver.cs` | 4327 | `AM63` → `AM58` | 写的是 `AM63 Siege Warfare`；`Siege Warfare` = **AM58**（`AM63` 在真值是 `Dawn Attack`） |
| `RuleEngine/Core/EffectResolver.cs` | 6808 | `BL77` → `BL74` | `Spreading Corruption` |
| `RuleEngine/Core/EffectResolver.cs` | 7475 | `AM77` → `AM76` | `Armoured Lightning` = **AM76** |
| `RuleEngine/Core/RuleCore.cs` | 6352 | `TL79` → `TL75` | `Termagant Brood` = **TL75** |
| `RuleEngine/Core/RuleCore.cs` | 6649 / 7298 | `BL77` → `BL74` | `Spreading Corruption` |
| `RuleEngine/Editor/RuleEngineTest.cs` | 9283 | `EC62` → `EC60` | `Armoury of Excess` = **EC60** |
| `RuleEngine/Editor/RuleEngineTest.cs` | 9304 / 9326 | `SOR62` → `SOR49` | `Shrineworld` = **SOR49**（`SOR62` 在真值是 `Raging Storm`；旧号来自 PnP 文件号 `Warpforge_62_Shrineworld`） |
| `RuleEngine/Editor/RuleEngineTest.cs` | 10093 / 10095 / 15266 / 15327 | `TL83` → `TL70` | `Norn Emissary` |
| `RuleEngine/Editor/RuleEngineTest.cs` | 11183 | `TL82` → `TL69` | `Neurotyrant` |
| `RuleEngine/Editor/RuleEngineTest.cs` | 15440 | `BL77` → `BL74` | `Spreading Corruption` |
| `RuleEngine/Editor/RuleEngineTest_S23_Parse.cs` | 327 / 338 / 347 | `TL83` → `TL70` | `Norn Emissary` |
| `RuleEngine/Editor/RuleEngineTest_S23_Parse.cs` | 327 | `UM_Avenging_Zeal` → `UM40` | `Avenging Zeal` |
| `CardPresentation/Editor/DeckScene.cs` | 4305 | `ASH_Autarch` → `ASH83`；`DA66` → `DA61` | `Autarch` = **ASH83**（白盒代码在 :4312 本来就写 `DA61`）；`Plasma Generator` = **DA61** |

**复核方式**：
```bash
git diff --numstat   # 9 个文件应全是「小改动」（1/1 · 2/2 … 9/9），不是整篇
bash d:/4/Unity/工具/typecheck.sh   # 已跑：运行时错误数 0 · 编辑器错误数 0
```
⚠️ 改这些文件时踩了一次 **CRLF 坑**：`DeckScene.cs` / `EffectResolver.cs` / `RuleEngineTest.cs` 是 **CRLF**，按 `\n` 切再按 `\r\n` 拼会产生 `\r\r\n`（整篇重写，`numstat` 8759/8759）。**已就地修复**（把 `\r\r\n` 还原成 `\r\n`），现 diff 干净。**下一个会话改这三份时注意。**

### ⛔ 没改哪些（为什么）
1. **全池 1009 张的 `id`** —— **已经是真值**，无改动。（这是本次最大的「没改」，也是最该说清的。）
2. **118 张 SW/UM/DA 的 `id`** —— 用户明确「暂时别管」（见 §四）。
3. **`card_ids.json` 的 21 个自造残留键**（`AM20`/`AM77`/`DA65`/`DA66`/`TL76`–`TL86` …） —— ③ 里 `DA65`/`DA66` 是 `decklists.json` 的**桥**（原版预组引用了它们，删了会断桥）；其余 19 个**未被任何数据/代码引用**，但**清不清要主对话裁**（见 §五·1）—— 本批**只报未改**（循 `M_卡牌P1_id置换.md:126`「旧表残留全部保留、要不要清请主对话裁」的先例）。
4. **`gen_*.py` 注释里的陈旧 id** —— 见 §五·2。
5. **`.cs` 注释里的旧「自造形」id**（`AM_Hektor_Thenmann` / `UM_Angels_of_Death` / `GOF_Big_Choppa_Nob` …，约 60 处） —— 见 §五·3（**其中一部分是「自造 id 形态」的教学示例，⛔ 不能无脑替换**）。

---

## 四、⛔ 没动的：118 张（SW/UM/DA）清单与计数

| 阵营 | 张数 | id 形态 |
|---|---|---|
| **SpaceWolves** | **68** | 短 id（`SW1`…`SW68`）—— 原版真正的 SW 编号，我们照 PnP 文件号排的（`A2072`） |
| **Ultramarines** | **38** | **两种**：短 id `UM87`–`UM101`（15 个）＋ 自造形 `UM_<名字>`（23 个，如 `UM_2nd_Company_Terminator`/`UM_Roboute_Guilliman`） |
| **DarkAngels** | **12** | 短 id（`DA76`–`DA87`） |
| **合计** | **118** | —— |

**为什么别动**：真值快照（Unity `2022.3.38f1` / 缓存 `2025-07-30`）**早于原版那次大版本重建**（原版现客户端 `6000.2.6f2`）⇒ 这 118 张是**重建之后才进的游戏**，本机**拿不到它们的真值 `uniqueId`**（判据边界 → `R9_卡牌id真值挖掘.md` §四/§五；`A2075`/`A2072` 均「⛔ 等远端」）。**唯二例外已另行裁定**：`SW2`/`SW3`（`card_ids.json` 的 note 记「已由 `deckAvatar` 换成 Njal/Ragnar」），**本批不碰**。

---

## 五、还差什么 / 判不准的（⛔ 不许猜）

1. **`card_ids.json` 的 21 个自造残留键要不要清 —— 请主对话裁**。
   - 它们**不在我们池、也不在真值**（现读 21 个：`AM20` `AM77` `BL76` `BL77` `DA65` `DA66` `GSC48` `GSC78` `SAU77` `SOR76` `TAU76` `TL76` `TL77` `TL78` `TL79` `TL80` `TL82` `TL83` `TL84` `TL85` `TL86`）。
   - **其中 19 个**（除 `DA65`/`DA66` 外的全部）**未被任何 `.json`/`.cs`/`gen_*.py` 引用**，且**与池内某张卡同名**（`AM20`↔`AM59 Astropath`、`TL82`↔`TL69 Neurotyrant` …）⇒ 是「同名两 id」歧义源（`A1800` 当年就是靠「候选只剩一个在真值内就取它」兜的底）。
   - **`DA65`/`DA66` 必须留**：`decklists.json`（原版 236 副）引用了 `DA65`–`DA74`，删了会断预组桥。
   - ⛔ 本批**没动**（保留先例 + 需主对话裁）。如要清，走一次性脚本 + `git diff --numstat` 看着行尾。
2. **`gen_*.py` 注释里的陈旧 id（未改，约 20 处）**：`gen_icon_plan.py`（`BL60` `ASH58` `GSC60` `TL79` 等）、`zh_crosscheck.py`（`BL77`）、`_a1759_reconcile.py`（`#HB_*`，是占位符不是卡 id，留）。⚠️ **它们是「逐卡表」的锚**（`gen_icon_plan.py` 里那些 id 是**被处理的卡**的键），**改之前必须先确认那个 id 在生成器里是「查卡」还是「当键」** —— 本批**没读全，故不动**（`A1848` 那句「`gen_cards_engine.py` 的 id 取值段没人读全」同族的谨慎）。
3. **`.cs` 注释里的旧「自造形」id（未改，约 60 处）**：`AM_Hektor_Thenmann`(`→AM66`) · `UM_Angels_of_Death`(`→UM64`) · `GOF_Big_Choppa_Nob`(`→GOF68`) · `GSC_Jammed_Communications` · `DA_Convoke_the_Circle` · `GOF_Worst_Temper` · `UM_Avenging_Zeal`(`→UM40`) · `ASH_Autarch`(`→ASH83`，**部分已随手改**) …
   🔴 **为什么不一刀切**：其中**有一批是「自造 id 形态」的教学示例，必须照旧**（改了反而错）——
   例 `CardDef.cs:56`（「形如 `ASH_Autarch` / `ASH32`」）· `CardDatabase.cs:32`（「`AM_Some_Card` 这样的自造 id」）· `RuleEngineTest.cs:8818`（「id 的两种形态：原版 `AM12` / 自造 `AM_Some_Card`」）。
   ⇒ 要修得**逐处判「引用」还是「示例」**，本批**只做了能自动判的那 34 处**（用「非池内真值/表 id ↔ 池 id」的差集扫出来的），**旧自造形的留给下一批**（建议单开一条账，别和本次混）。
4. **`GOF50` 的注释歧义（未改）**：`EffectResolver.cs:4326` 写 `GOF50 Tide of Muscle`、`BattleContext.cs:957` 等写 `GOF50 / …`。真值 `GOF50` = `Dust Storm`，池内 `Tide of Muscle` 在 **GOF39** ⇒ 那几处**指向的卡本身要判**（是「Dust Storm」还是「Tide of Muscle」），**不猜**。

---

## 六、顺手发现的新问题（→ 应由主对话分流立账）

1. 🔴 **真值 `cardName` 列本身有陈旧值（~67 处）** —— 这是**本次「同名两 id」的总根因**。
   实测样例：`e59ac9e46f4ea804784e954c3b820e77` 的 `cardName` 写成 `Path of the Warrior`，但它的 `m_Name`（`Z-NC SaimHann-Skyborne Deployment`）与官方 `Card_Name`（`Skyborne Deployment`）**都说是 Skyborne Deployment**。
   更大的样本：`Dark Pact of Fate/Blood/Excess/Resilience` 四条（真值 `cardName` = `Mark of Tzeentch/Khorne/Slaanesh/Nurgle (enchantment)`）、`Chosen of the Four`（真值 `cardName`=`Warmaster`）、`Master of Repentance`（真值 `cardName`=`Exemplar of Hate`）—— **官方 `Card_Name` 全部站我们池这一边**。
   ⇒ **凡「拿真值 `cardName` 当权威」的地方都要留这个心眼**。本批**没动**（真值快照是只读的 `d:/2/` 档案；改正则得改生成器口径）。与 `M_卡牌P1_id置换.md:131`（点 7）**同一处**，那里只记了 `Skyborne Deployment` 一例，这里补全到一族。
2. ⚠️ **卡池覆盖缺口（不是 id 问题，但同源）**：现读**116 张真值卡**「id 与卡名都落不进我们池」—— 多为**环境/进攻卡**（`Normal Conditions (各阵营)` · `Solar Storm` · `Radiation Storm` · `Thunderstorm` · `Dust Storm` · `Night Attack` …）、**教程变体**（`… (tutorial)`）、以及少数计策（`Dawn Attack`/`AM63` · `Daemonic Feast`/`BL60` · `Aural Hijack`/`EC62` · `Shrine Bombardment`/`SOR63` · `Aerial Clash`/`UM49`）。
   ⚠️ **其中一部分可能是有意不入池**（同族先例：`DA62`–`DA64` 进攻卡「有意不入主池」）⇒ **别当缺陷直接补**，先判「该不该在池」（建议并进 `T02`/`A1783` 那一族）。
3. ✅ 记账：`A2069` 的结论**复核成立**（113 张 GUID 就是原版 `uniqueId`）；`A2052` 的订正方向对。
