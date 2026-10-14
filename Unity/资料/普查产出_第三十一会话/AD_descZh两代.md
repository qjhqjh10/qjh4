# `AD` · 卡牌线收口一批：`descZh` 两代源文本冲突（4 条一族）· 生成器侧三条过期/缺陷

> 写手：卡牌线收口批**唯一写手**（白名单：`工具/gen_cards_engine.py` · `数据/游戏数据/cardface_fixes.json` · 生成物）。
> ⛔ 本笔**未动 git** · **未动正本**（四份 + `资料/普查产出_第三十会话/**`）· **未跑任何 Unity 批处理**（用户口径：自检先等等）。
> ✅ 跑过：**秒级类型检查**（运行时 0 错 / 编辑器 0 错）· `python -I -X utf8 工具/gen_cards_engine.py`（**rc=0**，两次）·
> `bash 工具/ruleprobe.sh check`（读数与 `AB` 那笔**逐字相同**）· `bash 工具/ruleprobe.sh layers`（离线，**不占 Unity 实例**）。
> 读数时刻 ≈ **2026-10-15 01:20–02:20**。

---

## 〇、一句话结论（两主题各一行）

| # | 件 | 状态 | 一句话 |
|---|---|---|---|
| **一** | `descZh` 两代源文本冲突（`SOR31`/`ASH48`/`SAU58`/`TAU51`） | ✅ **做完** | **另 3 张全部亲开 PnP 成品卡图核实**（3/3 与我们的 `desc` 逐字一致、官方 EN 少一句）⇒ 四张一起补 `descZh`（**保留官方那句、把缺的半句补进去**，`desc` 一字未动）· 重跑进池 · 层序断言 **255/255 仍绿** |
| **二** | 生成器侧三条 | ✅ **做完**（一条带代价，见 §二·3） | ① 排除表清空（引擎已认那三词）② `DA83` 的 `armour` **0 → 1**（`armour` 列 >0 **80 → 81**）③ `TRAIT_DISPLAY` 三条改成**卡面字**（`frenzied`→`Destroyer` · `rage`→`Penitence`） |
| **三** | 立的那笔账 | ✅ **立了，且手段扩成** | 「中文比英文旧一代」的**比较手段从 102 张扩到 855 张**（关键 = 官方记号可**离线展开**）⇒ 候选 **52** 条（已知 4 条 **4/4 命中**、在已知那档 102 张上 **0 假阳**）；**48 条新候选待逐张开图裁定** |

**卡池**：`1127` 张 · `v9` · md5 改前 `8aa9b9bf5adfa0a7e249c14e8ef56cce` → 改后 **`64c21c901ade424b34ad0d0baf0298af`**
（改前那个 md5 与 `AB_接线批.md` 记的**逐字相同** ⇒ 起点核对了）。
🔬 **两主题的改动面是【各自隔离】量出来的**（都靠离线重建件，⛔ 不是推的；重建脚本在 `d:/4/_tmp_view/oldgen/`，该目录 `.gitignore` 掉了）：

| 比较 | 读数 |
|---|---|
| 改前池（`8aa9b9bf`，**重建时 `cardface_fixes.json` 还是改前那份**）vs 只跑完主题二的池 | **16 张**（= 主题二那 16 张，见 §2·4） |
| 只带主题一（生成器代码撤回主题二那 5 处）的池 `3a3a9958…` vs 最终池 | **还是那 16 张、一张不多** ⇒ **主题一的效果 = 只有那 4 张的 `descZh`**，别的字段一个字没动 |
| 改前池 vs 最终池 | **20 张**（16 + 4，两张表合起来） |

⚠️ **重跑那个重建脚本要注意**：它读的是**盘上当前的** `cardface_fixes.json`
⇒ **今天再跑一次，得到的是 `3a3a9958…`（含主题一）而不是 `8aa9b9bf…`**。`8aa9b9bf` 只有「主题一那次改动之前」才复现。

---

## 一、主题一：`descZh` 的「两代源文本冲突」（4 条一族）

### 1·1 结论

四张卡的**中文比英文少一句/半句** —— 成因**不是**「我们多写了一句」，而是
**`desc` 与 `descZh` 不是同一代源文本**：

| 源 | 年代 | 谁在用 |
|---|---|---|
| **PnP 成品卡图**（v1.5-3） | **2026-08-15**（与要复刻的客户端同期，已裁⑥「PnP 优先」） | 我们的 **`desc`** |
| **官方本地化 `Card_Description/*`** | **2025-07-30 远端包快照**（**比 PnP 旧**） | **P3 刚换过去的 `descZh`** |

⇒ 官方 EN 少一句，P3 把 `descZh` 换成官方那代之后，**中文就跟着少了**。
**主对话口径**：`desc` 是对的、⛔ 不改；**补 `descZh`**。

### 1·2 亲开图核实（逐张，铁律 7）

| 卡 | PnP 卡图路径（**亲读**） | 卡面效果文字（抄录） | 与我们 `desc` | 与官方 EN |
|---|---|---|---|---|
| **`SOR31 Sister Dogmata`** | `d:/2/Warpforge部队卡片/Sorotitas/3部队/Warpforge_32_Dogmata.png` | `[闪电] Rally: Deal 1 damage. If the target **dies or** is a friendly unit, gain 1 [太阳]` | **逐字一致** | 少 `dies or` |
| **`ASH48 Cosmic Serpent`** | `d:/2/Warpforge部队卡片/Aeldari/4计策/Warpforge_52_Cosmic-Serpent.png` | `Trigger the abilities requiring Spirit Stones of all your troops. **② Repeat this effect**`（费用 3） | **逐字一致** | 少第二句 |
| **`SAU58 Implacable Conqueror`** | `d:/2/Warpforge部队卡片/Necron/4计策/Warpforge_58_Implacable-Conqueror.png` | `Lower the cost of all troops in your deck by 2. **Draw a troop**`（费用 5） | **逐字一致** | 少第二句 |
| **`TAU51 Saviour Protocolst`** | `d:/2/Warpforge部队卡片/Tau/4计策/Warpforge_63_Saviour-Protocols.png` | `Your Drones cost 1 less for the rest of this battle. **Choose a Drone and add it to your hand**`（费用 4） | **逐字一致** | 少第二句 |

> `SOR31` 那张是 `AB` 亲读的（`AB_接线批.md` §四·1），**本笔复读了一次、逐字相同**；另 3 张本笔亲读。
> 四张的**费用/数值**与卡池逐一吻合 ⇒ 文件与卡**对得上**（`ASH48` 费 3 · `SAU58` 费 5 · `TAU51` 费 4）。
> ⚠️ 文件名编号 ≠ 卡 id 数字（例：`SOR31` → 文件号 `32`；`TAU51` → 文件号 `63`）—— 是**按分类目录重号**的，
> 只能按「同编号候选集 + 卡名 + 数值」比。本笔四张都按**卡名**定位。

### 1·3 改法（**保留官方那句、把缺的半句补进去**）

落点 = `数据/游戏数据/cardface_fixes.json` 的 **`descZh` 列**（⛔ 不是改 `desc`、⛔ 不是把官方那句删掉）。
补译的**两个来源都有判据**（⛔ 没有一处是自创）：

| 卡 | 补后 `descZh` | 补译的判据 |
|---|---|---|
| `SOR31` | `[[rally]]：((Deal_1_damage))。如果目标死亡或为友方单位，获得 ((Faith_cost_x,1))` | 官方句原样保留，`死亡或` 插在它中间；`如果目标死亡` 是官方表自己那 11 处的写法（例 `Card_Description/ASH73` = `如果目标死亡，对相邻单位造成2点伤害`）。**形态与改前一致**（`AB` 给的那个形状） |
| `ASH48` | `触发你所有军队需要灵魂石的能力。②重复此效果` | 官方句原样保留；补的那半句的**官方记号形态**在官方表里就有 —— `Card_Description/ASH48_new` = `…troops.<br>((Spirit_x,2)) ((Repeat_this_effect))`（zh 列未译）。`②` 是**我们能渲染的那一代**：`card_icon_plan` 给 `ASH48.descZh` 的 token 就是 `②`→`SpiritStone_2`；`重复此效果` = `Modular_Description/Repeat_this_effect` 的官方 zh |
| `SAU58` | `将你卡组中所有军队的费用降低2点。抽一张军队牌` | 官方句原样保留；`抽一张军队牌` 是官方表对**同一句 EN** 自己的译法（`Card_Description/b3364cdc…` = `补充你所有的能量。抽一张军队牌`，同句末位置）。官方 `_new` 键那条写的是 `((Draw_a_troop))`（`Modular_Description/Draw_a_troop` zh = `从己方卡组中抽一张军队卡牌`）—— ⚠️ 用它会**原样印出来**（见 §一·5） |
| `TAU51` | `在此次战斗剩余时间内，你的无人机的花费减少 1。选择一个无人机并加入你的手牌` | 官方句原样保留；`选择…并加入你的手牌` 是官方表对 `add it to your hand` 的译法（`Card_Description/fd67b475…` = `…选择一支非传奇的基因窃取者教团军队并加入你的手牌`）。⚠️ `Choose a Drone` 在官方 `Modular_Description` 里 **0 命中** ⇒ 只能写**字面中文**（照上面那句的口径） |

**验收（判据 4 条，都已跑）**：

1. **句子构成一致**（简报点名）：四张的 `desc` 与 `descZh` 都是 **2 句对 2 句**（`SOR31` 的中文含「死亡或」那一档）✅
2. `descZh` **层序断言**（`gen_cards_engine.py:1820-1831` 那条结构断言）⇒ 跑完打的是
   **`cardface_fixes.descZh`（人工、最高序）最终生效 255/255 条 · 违例 0 处 ✅`**（改前 251/251）✅
3. **裸卡名键唯一性**（`gen_cards_engine.py:2071-2087` 的 `bare_ambiguous`）⇒ 跑完打 **`裸卡名键唯一性 ✅`**；
   四个键（`Sister Dogmata` / `Cosmic Serpent` / `Implacable Conqueror` / `Saviour Protocolst`）
   在全池**都只出现一次**（现扫）⇒ 档位没踩 `_bad`（`_bad` 一旦为真 ⇒ **rc=1、一个字节都不写**）✅
4. **解析差异不该变**（简报点名）⇒ `ruleprobe check` 读数与 `AB` 那笔**逐字相同**：
   `全池 1121 张 · 解析差异 1 行 · 改名 0 行 · 新增 172 · 消失 171`，唯一那行仍是 `GSC29`（`A1989` 那笔）✅
   ⇒ **改 `descZh` 对解析是隐形的**（解析读 `desc`），**符合预期**。

### 1·4 改了哪些文件哪些行

| 文件 | 位置 | 改动 |
|---|---|---|
| `Unity/数据/游戏数据/cardface_fixes.json` | `descZh` 对象**开头 4 行** | 新增 4 条（上表） |
| 同上 | 顶层第 1 个键 | 新增说明键 `_2026-10-15_AD_descZh两代`（判据链 / 官方 `_new` 键 / 两档补译来源） |
| `Unity/工具/gen_cards_engine.py` | **无改动**（本主题一个字没动） | —— |
| 生成物 `MyGame/Assets/RuleEngine/Resources/cards_engine.json` | 重跑覆盖 | 4 张的 `descZh` 变更 |

- 行尾：`cardface_fixes.json` 改前 **CRLF 1906 / LF 1906**（纯 CRLF）→ 改后 **CRLF 1911 / LF 1911**（**仍纯 CRLF**），
  **+5 行 / −0 行**（`187753 → 190648` 字节）· `git diff --numstat` = `27 1`（含别人未提交的存量，⚠️ 见 §五·4）
- JSON 用 `json.loads` 回读校验通过（`descZh` 251 → **255**）· 四条值**逐字比对一致**（脚本内 assert）
- 🔴 用 **python 字节级 `wb` 拼接**（⛔ 不是 `sed -i`）· 替换前后**都打了计数**（锚点 `"descZh": {` 与
  `{\r\n "_2026-10-15_TL40_…"` 各命中 **1** 处，带 assert）

---

## 二、主题二：生成器侧三条过期/缺陷

### 2·1 ① `TRAIT_ENGINE_UNREGISTERED` 清空（`工具/gen_cards_engine.py:849-872`）

```python
TRAIT_ENGINE_UNREGISTERED = set()      # 改前 = {"frenzied", "rage", "unstoppable"}
```

- **判据**：`AB` 已于同日把这三个词登记进引擎（`Core/CardDef.cs` 的 `Prefixes`：`rage`→`Penitence` ·
  `frenzied`→`Destroyer` · `unstoppable`→`RuleCore.Unstoppable`；见 `AB_接线批.md` §三）。
  ⇒ 生成器这侧**必须跟着改**，否则「引擎认得 / 生成器仍排除」**两处写法并存**。
- **集合留着不删**（注释写明）：它是**将来新 trait 的保险闸** —— 往 `TRAIT_DISPLAY` 加引擎还没登记的名字时
  先加进来，让摘要**逐条报数**（`merge_truth_traits` 的 `skipped`），而不是静默写进 `keywords` 再被引擎丢掉。
- **键的口径已写进注释**：这个集合按 **`TRAIT_DISPLAY` 的值**（归一化后）比，⛔ 不是真值枚举名
  （改 `TRAIT_DISPLAY` 的值时必须同步看它 —— 这是个容易踩的耦合）。
- 同批改的两处**过期文案**：`gen_cards_engine.py:1644-1655`（`cardTraits` 列那段「它可以带上引擎不认的那三个」）
  与 `:2339-2344`（摘要那句「没登记这三个词」）。两处都按铁律 5 **就地订正**。

### 2·2 ② `DA83` 的护甲列缺陷（`工具/gen_cards_engine.py:1974-2016` `fix_own_armour`）

**先查清了「为什么漏」**（简报点名要先查的）：

- `DA83` 的 `keywords` 是**裸 `Armour`（没数字）**、`desc` = `Armour 1. When you create or play a Secret, …`。
- 旧那道闸 = `any(re.search(r"armou?r", k) for k in keywords)` ⇒ **裸词也算「已经护甲」** ⇒ 直接 `return`。
  **而裸词恰恰是「OCR 没读到数字」的形态**，不是「已经有值」。
- ⇒ 列写 **0**；`desc` 段首那个 `Armour 1` 从没被捡起来。

**改法（两条）**：

- ⓐ 闸只看**带数字**的护甲条目（`re.search(r"armou?r\s*\d+", k)`）—— 有数字才是「已经有值」；
- ⓑ 裸词那一格**原地升级**成 `Armour N`（**保序、不追加第二条**）——
  追加会让卡面印两遍（`护甲。护甲 1。`，`CardText.KeywordSegment` 逐条比字面）。

**影响面（现算，全池只有这一张落到本分支）**：现扫「关键词里有裸 `Armour` 且 `desc` 以 `Armour N` 开头」= **1 张**。

| 读数 | 改前 | 改后 |
|---|---|---|
| `DA83.keywords` | `['Armour']` | **`['Armour 1']`** |
| `DA83.armour` 列 | `0` | **`1`** |
| 全池 `armour` 列 > 0 | `80` | **`81`**（摘要原话：`armour 列 >0 共 81 张 · 取 PnP 那一层 81 · 真值兜底 0`）✅ 与简报要求的数一致 |
| 旧口径 `KwValue(armour)` | `1`（`FirstNumber` 对裸词兜底）| `1`（**不变**）|

⇒ `AB` 在 `CardDef` 里加的「列优先、旧口径兜底」那一支**没删**（`RuleEngineTest.cs:2150-2152` 的 `DA83 有效护甲 = 1` 值锁照旧过），
而且**「只有旧口径有值」那一档现在变成空集** —— `AB` 那条白名单断言（`RuleEngineTest.cs:2111-2140`）**仍然绿**（白名单放行 0 张也是过）。

### 2·3 ③ `TRAIT_DISPLAY` 的显示名（`工具/gen_cards_engine.py:834-841`）

```python
"frenzied": "Destroyer",   # 改前 "Frenzied"
"rage":      "Penitence",  # 改前 "Rage"
"unstoppable": "Unstoppable"   # 不变 —— 官方表里【没有】这条显示名，见下
```

- **`cardTraits` 列是谁在读**（简报点名要查的）：全仓 `grep` ⇒ **只有 `Data/CardDatabase.cs`** 把它读进
  `CardDto.cardTraits`（`:119` doc · `:123` 字段 · `:223/:230` 版本注释），**没有任何消费点**
  （`CardDef` / `RuleCore` / `RuleEngineTest` / `CardPresentation` 里**零处读**）⇒ `AB` 说的「纯落值、不喂引擎」**核实成立**。
  ⚠️ 但**这个改动不只是显示**：`TRAIT_DISPLAY` 的值**同时**决定 `merge_truth_traits` 往 `keywords` 追加的那个串
  （两列同源）—— 所以它**会**改卡池。
- **为什么必须改成卡面字**：`frenzied` 若不改，10 张卡的 `keywords` 会**多出一条 `Frenzied`**、
  而它们**本来就有 `Destroyer`** ⇒ 卡面会印 `Destroyer. Frenzied.`（**重复且多出一个卡面没有的词**）。
  改成 `Destroyer` 之后**归一化同名 ⇒ 被 `have` 挡下 ⇒ 0 张真追加**（摘要实测：`Destroyer` 没出现在追加表里）。
- **判据**：官方本地化表 `Card_Trait/frenzied` = `Destroyer`/`毁灭者` · `Card_Trait/rage` = `Penitence`/`忏悔`
  （另有**化石键** `Card_Trait/frenzied_old` = `Frenzied` ⇒ 改名史的实据）· 卡面亲读（`SAU18 Skorpekh Destroyer`
  印 `Remnant. Destroyer. Flank`、全图**没有 `Frenzied`**；`SOR10 Sisters Repentia` 印 `Penitence: Gain +2 ✊`）。
- **`unstoppable` 为什么保留 Title Case**（与本条其余两个**不同性质**，已写进注释）：
  官方表 **9441 条里没有这条**（只有成就标题等命中），卡面 `SAU7 Canoptek Wraith` **也没印这个词**
  ⇒ **它没有官方显示名**；而这个字面量**必须**是 `unstoppable`，因为引擎 `Prefixes` 登记的就是它，
  写成别的拼法会被 `KeywordTable.Normalize` 丢掉。⇒ **这一格是「引擎的拼法」，不是「卡面字」**。

### 2·4 本主题的**池改动面**（逐字段对出来的，⛔ 不是推的）

把改前的池**离线重建**（复制一份生成器、把本笔 5 处改动逐条撤回、`DST` 指到临时路径）⇒ 重建件 md5
= **`8aa9b9bf5adfa0a7e249c14e8ef56cce`**，与 `AB` 记的**逐字相同** ⇒ 起点可信。两次产物逐卡比：

| 类别 | 张数 | 改了什么 |
|---|---|---|
| `cardTraits` `Frenzied` → `Destroyer` | **10**（`SAU3/9/18/22/25/32/66/68/70/72`） | **只动 `cardTraits`**，`keywords` **一张都没动** |
| `cardTraits` `Rage` → `Penitence` | **5**（`SOR8/10/21/27/32`） | 同上 |
| `keywords` 追加 `Penitence` | **4**（`SOR8`/`SOR10`/`SOR21`/`SOR27`；`SOR32` 本来就有） | `SOR10`/`SOR27` 从 `[]` → `['Penitence']` |
| `DA83` | **1** | `keywords ['Armour'] → ['Armour 1']` · `armour 0 → 1` |
| **合计变更卡数** | **16** | 全池 1127 张里**只有这 16 张**有字段变化（逐卡 dict 比，脚本已断言 id 集合相同） |
| `SAU7 Canoptek Wraith` | **0** | 真值那层给它追加了 `Unstoppable`，**被更高层（`cardface_fixes` 的 `keywords`）换掉** ⇒ **池一个字没变**、卡面也没有多出关键词（摘要里 `被更高层换掉 … Unstoppable=1` 就是它） |

**引擎行为面**：那 4 张 `Penitence` 卡的效果**本来就在跑** —— `CardDef.CollectTriggerOps` ① 是**从 `Desc` 收正文**
（`CardDef.cs:751-765`），⛔ **不靠 `Has()`**。用**离线真引擎**（`ruleprobe layers`）现读那 4 张：
`ch.trig = 1`（`SOR8`/`SOR10`/`SOR21`/`SOR27` 各 1 条触发 op，`SOR32` 同）⇒
`BodyResolves("penitence")` = `TriggerOps != null` **四张全为真**
⇒ **`RuleEngineTest` 的 `TestA1618TwoParsersAgree`（`Check(star, 0)`，`:23085`）不会因为这 4 张变红** ✅（这条是**跑真引擎量出来的**，不是推的）。

### 2·5 ⚠️ 代价：`AB` 那条断言会**变红一条**（⛔ 本笔修不了）

`RuleEngineTest.cs:2166-2171`（`AB` 本会话刚加的）：

```csharp
Check(pen, 1, "★ 全池 `Has(penitence)` = 1 张（登记 `Rage` 前后必须相同 —— 多了就是前缀撞车）");
Check(des, 10, "★ 全池 `Has(destroyer)` = 10 张（登记 `Frenzied` 前后必须相同）");
Check(uns, 0,  "★ 全池 `Has(unstoppable)` = 0 张（`SAU7` 走的是卡面句那条入口）");
```

改后现算：`Has(penitence)` **1 → 5**（那 4 张）· `Has(destroyer)` **10 → 10**（不变）· `Has(unstoppable)` **0 → 0**（不变）。
⇒ **只有第 1 条会红**，它写死了那个基线数。

- **这是「清空排除表」这条指令的**必然**结果**（简报要的就是让这三个词流进 `keywords`），
  但它在代码上落在**本笔白名单之外**（`RuleEngine/Core/**` 与测试都在 `AB` 那边）⇒ **本笔不改，如实报**。
- 三个数**为什么只有第一个变**：`frenzied` 那 10 张的 `keywords` **本来就有 `Destroyer`**（改成卡面字后归一化同名 ⇒ 被挡下）；
  `unstoppable` 那一张被更高层换掉了（§2·4）。⇒ **「前缀撞车」这个判别式里的「至少一个动」这次不是撞车，是指令本身要的效果**。
- 🔴 **建议由主对话裁**：要么把第 1 条重钉成 `5`（并把注释改写成「4 张 `Penitence` 是【生成器放行】带来的、已逐张核过不是撞车」），
  要么把这条改动整体回退（回退只有 5 处、见 §六）。**两条都不该由本笔擅自做。**

---

## 三、立的那笔账：「`descZh` 换代后，到底有多少张中文比英文旧一代」

> 简报：`AB` 的手段只覆盖 **102 张**（其余「官方无条目 / 官方值是坏值 / 官方值带富文本标记**无法逐字比**」）⇒ 要扩手段。

### 3·1 手段**扩成了**：102 → **855** 张（+753）

**关键发现：官方那些"无法逐字比"的记号，其实可以【离线展开】** —— 词条就在我们手上那份官方表里：

| 记号 | 展开成 | 官方表里的条目数 | 本池缺词条 |
|---|---|---|---|
| `((Key,a,b))` | `Modular_Description/Key`（`{0}`/`{1}` 代入） | **340** | **0** |
| `[[枚举名]]` | `Card_Trait/枚举名` | **77** | **0** |
| `$$卡id$$` | `Card_Name/卡id` | —— | **0** |

⇒ 展开 + 剥标签 + 去纯数字之后，**855 张**都成了「字面骨架」可比的（现读覆盖面：
可比 **855** · 官方无条目 **122** · 官方值坏（去空白 < 3 字符）**150** —— 1127 = 855+122+150）。
（`AB` 那笔记的 118/149/858/102 是另一套划分的口径，**别拿两套数互减**。）

### 3·2 两个判据 · 发现多少条

| 判据 | 口径 | 命中 | 备注 |
|---|---|---|---|
| **① 句数** | 官方 EN 的句子数 **<** 我们 `desc` 的句子数 | **13** 条 | 抓「**整句**缺失」；含已知的 `ASH48`/`SAU58`/`TAU51`（**不含** `SOR31` —— 它是**半句**，句数相同） |
| **② 字面多重集差** | 展开后**我们比官方多出**的词（去纯数字、剥我们的 `[...]` token、消掉已知术语差） | **52** 条 | 抓「**多字面**」；⚠️ **含词序不同那一类也抓得到** ⇒ 已知 4 条 **4/4 全中** |
| ② 的反向（官方比我们多） | —— | 253 条 | 🔴 **不能当缺陷判据**：绝大多数是**架构差** —— 官方把**关键词声明写在 `Card_Description` 里**，我们放在 `keywords` 那一列（例 `waystone`/`flying`/`ephemeral`/`duty`/`flank`） |

**判据② 的精度自检（拿 `AB` 那 102 张已知结论当尺子）**：在「官方值**无**记号」那 102 张上，
本判据标了 **3 张** = `ASH48` / `SAU58` / `TAU51`，**与逐字比的结果一致、0 假阳**
（`SOR31` 不在那 102 张里 —— 它的官方值带记号）。⇒ 判据在**有已知答案的那一档**上**准**。

**判据② 的 52 条按「多出几个词」分档**：1 词 × 13 · 2 词 × 12 · **≥3 词 × 27**。
（1 词那一档多是**术语/图标假阳**：例 `SAU43 +and` · `GOF84 +ork`；2 词那一档两种都有。）

**已知的两类假阳**（写在这里，免得下个会话把它们当缺陷）：

1. **术语差**：官方 `Card_Trait/questPoints` = `Quest Progress`，而我们 19 张卡的 `desc` 写 `Quest Points`/`Quest Point`
   ⇒ 判据把它当「我们多出 `point(s)`」。（本笔的脚本已把这一族**消噪**掉，清单里看不到它们。）
2. **图标名差**：官方 `((Ranged_attack_icon_x,+2))` 展开后**只剩数字**（数字被丢），而我们写 `+2 [Ranged]` ⇒
   我们这侧多出 `ranged`/`attack`；同族还有 `((Spirit_x,1))` vs `1 [Spirit Stone]`、`((Faith_cost_x,1))` vs `1 Energy`。
   （本笔的脚本把 `ranged/attack/spirit/stone/melee/energy/quest/point(s)` 这一族**消噪**掉了。）
   ⚠️ 这一族也说明 **`SOR31` 那条 delta 里的 `energy` 是假阳、真 delta 只有 `dies or`**。

### 3·3 那 48 条**新候选**（待逐张开 PnP 卡图裁定，⛔ 本笔不猜）

「多 ≥3 词」那 **27 条**（最像「整句/整从句多出」的一档）：

| 卡 | 多出 | 卡 | 多出 |
|---|---|---|---|
| `UM20 Sergeant Telion` | +10 | `GSC29 Metamorph Iconbearer` | +4 |
| `SAU15 Deathmark` | +9 | `SOR29 Ephrael Stern` | +4 |
| ★`TAU51 Saviour Protocolst` | +9 | `UM62 Battlefield Supremacy` | +4 |
| `GOF13 Deffgun Loota` | +6 | `ASH12 Swooping Hawk` | +3 |
| `GOF18 PainBoy Sniklaw` | +6 | ★`ASH48 Cosmic Serpent` | +3 |
| `GOF99 Skrag Every Stash!` | +6 | `ASH52 Hosts of the Dead` | +3 |
| `SAU70 Damaged Hexmark` | +6 | `GOF106 Power of the Waaagh!` | +3 |
| `SOR13 Simulacrum Bearer` | +6 | ★`SAU58 Implacable Conqueror` | +3 |
| `TAU3 Commander O'Maisos` | +6 | `TAU40 Technological Supremacy` | +3 |
| `f104d4c2… Righteous Repugnance` | +6 | `TL24 Harpy` | +3 |
| `6ed51d59… Duelist's Hubris` | +5 | `TL47 Hive Fleet Leviathan` | +3 |
| `ASH70 Swooping Hawk Exarch` | +5 | | |
| `GOF29 Grukk Face-Rippa` | +5 | | |
| `GSC28 Hulking Aberrant` | +5 | | |
| `SOR30 Celestian Sacresant` | +5 | | |
| `UM40 Avenging Zeal` | +5 | | |

另 **21 条**是 1–2 词档（`SOR31`【+2 `dies or`】· `SOR4 +gain health` · `SOR60 +gain` · `TAU65 +strike heals` ·
`TL69 +an whenever` · `UM57 +this turn` · `UM73 +troop×2` · `SOR24 +all units` · `SOR25 +a and` · `SOR39 +has pray` ·
`ASH34 +and camouflage` · `64a657f… +and stomp` · `AM72 +strike` · `AM76 +armour` · `ASH35 +gains` · `GOF61 +armour` ·
`GOF84 +ork` · `SAU43 +and` · `SOR71 +and` · `TL75 +gain` · `UM22 +other` · `UM_Terminator +oath` · `af98d42… +an` ·
`851f73da… +troop`）—— **里面两种都有**（`SOR31` 那条是真的、`+and`/`+ork` 那几条是假阳）。

🔴 **定案口径**：这 48 条**只是候选** —— 「我们的英文比官方 EN 多」有**多条**成因
（PnP 那代更新 / 我们的 `desc` 被人工修过 / 官方条目本身旧 / 官方把两句并成一句），
**真值只能逐张开 PnP 成品卡图**（铁律 7）。
⇒ **本笔只把点名的那 3 张开图核实并修了**（**另 1 张由 `AB` 亲开**）；
**其余 48 条立账**、⛔ 本笔不猜、⛔ 不改。

### 3·4 手段**还差什么**（如实写）

- 那 **122 张「官方无条目」** 与 **150 张「官方值是坏值」** —— **判据是空的**（官方快照比现池旧）
  ⇒ 这两档**扩不了**，除非拿到更新的官方表（`A970` 那条线：真值表只采到 4.3%）或**逐张开 PnP 卡图**。
- 判据① 抓不到「**半句/从句**」缺失（`SOR31` 就是），判据② 抓到了但会带术语/图标假阳
  ⇒ **没有一条判据能单独定案**；本笔给的是**候选集 + 已知答案上的精度自检**。
- ⚠️ 判据② 用的「**官方记号可展开**」这件事**只对比较有效** —— 游戏里那 220 张 `descZh` 的
  `((…))` **仍然原样印在卡面上**（`Modular_Description/*` 不在 `Resources/`，`A1982` 那笔的数据缺口，见 §五·1）。

---

## 四、有没有加断言（含判别式）

🔴 **本笔【没加】任何 C# 断言** —— `RuleEngine/Editor/RuleEngineTest.cs` **不在本笔白名单**（另一写手在动 `Core/**`）。
本笔靠的判据是**跑的时候就会炸/出声**的那几条（都在生成器里，跑一次就验）：

| 已有的判据（生成器内） | 位置 | 本笔跑出来的读数 | 判别式（**结构上不可能同时满足**） |
|---|---|---|---|
| `descZh` 层序结构断言（人工值必须最终生效） | `gen_cards_engine.py:1820-1831` | **255/255 · 违例 0** | 官方层插到人工层**之后** ⇒ 官方值盖掉人工值 ⇒ 逐条红；把人工那层删掉也红 |
| 裸卡名键唯一性（`bare_ambiguous` ⇒ `_bad` ⇒ rc=1 不落盘） | `:2071-2087` / `:2375` / `:2387-2398` | **`✅`** | 一个裸键服务多张卡 ⇒ 值只可能是其中一张的；加一条**同名**卡的裸键立刻红 |
| 关键词权威序三条（ⓐ 只补空 · ⓑ OCR 条逐字原样 · ⓒ 每条有归宿） | `:911-931` | **0 违例** | ⓒ 的「归宿计数」在**清空排除表**之后仍要闭合 —— 若有人把 `skipped` 那条路删掉而不真放行，ⓒ 立刻红 |

**本笔认为该补、但本笔不能补的断言**（⛔ 给主对话，别由代理自己写自证断言）：

1. **`DA83` 的值锁**：`keywords` 含 `Armour 1` 且 `armour == 1`（判据 = PnP 卡面银盾 `1`）。
   判别式：把 `fix_own_armour` 的闸写回**宽**那一版 ⇒ `armour` 列回落 0、`Armour 1` 条目消失 ⇒ 红。
   （`AB` 那条「`DA83` 有效护甲 = 1」**测的是最终有效值、不是列**，所以它**测不出**这次的回退 —— 这正是要补的理由。）
2. **全池 `armour` 列 > 0 = 81**（+ 下界 `≥ 80`），判别式：列读点被改窄 / `fix_own_armour` 被回退。
3. **`cardTraits` 里不许出现 `Frenzied` / `Rage`**（判据 = 官方 `Card_Trait/frenzied` = `Destroyer`、
   `Card_Trait/rage` = `Penitence`）。判别式：把 `TRAIT_DISPLAY` 改回 Title Case ⇒ 立刻红。
4. **那 4 张 `Penitence` 的 `BodyResolves` = 真**（本笔已用离线真引擎量过 = 4/4 真，见 §2·4）
   —— 建议把这条**并进 `A1618` 那条的读数**里，免得下个会话看到 `Has(penitence)` 变 5 就以为坏了。

---

## 五、顺手发现（**只报，一条都没改**）

1. 🔴 **官方表里有 18 个 `_new` 键 —— 那一代才是 PnP 的源，而生成器只读基础键**。
   现读 `Card_Description/ASH48_new` = `Trigger the abilities requiring Spirit Stones of all your troops.<br>((Spirit_x,2)) ((Repeat_this_effect))`、
   `Card_Description/SAU58_new` = `Lower the cost of all troops in your deck by 2. ((Draw_a_troop))`
   —— **与我们的 `desc` 逐字同代**（zh 列**未译**、直接是英文照抄）。
   ⇒ 「两代源文本」的**机制解释就在这张表里**：`_new` = 改版那一代（PnP 用了它），基础键 = 2025-07-30 那代。
   **本笔没动**（改 `load_official_desc` 让他优先读 `_new` 是**另一笔**：它会让 `descZh` 反过来变成**未译的英文**）。
2. ⚠️ **`TAU51` 的卡名可疑**：我们叫 **`Saviour Protocolst`**，官方 `Card_Name/TAU51` = `Saviour Protocols`、
   PnP 卡面（`Warpforge_63_Saviour-Protocols.png`）印 `Saviour Protocols` ⇒ 我们多了一个尾巴 `t`（像 OCR/抄录错）。
   与 `A970` 那条线同族（改卡名前要先拿 `zh_CN.csv` 精确查，见 `warpforge-card-data-vs-pnp`）—— **没查清谁对，只报**。 ✅ **2026-10-15 已查清并改正（`AH` 笔）**：三源一致 —— 官方 `Card_Name/TAU51` = `Saviour Protocols` · `数据/游戏数据/card_ids.json` 的真值映射 `TAU51 → Saviour Protocols` · PnP 卡面亲读 ⇒ **我们多一个尾巴 `t`**，已按已裁⑤ 改成 `Saviour Protocols`（落点 = `card_stats.json` 的 `name` 字段；卡池 md5 `64c21c90…` → `83a37522…`）。同族清单见 `资料/普查产出_第三十一会话/AH_br与卡名.md`。
3. ⚠️ **`descZh` 里的 `<br>` 会在卡面上【原样印出来】**：全仓 `CardPresentation/**` 里**没有任何一处**处理 `<br>`
   （`grep` 0 命中），而现池有 **52 张** `descZh` 含它（官方 zh 的换行写法）。
   ⇒ 本笔给 `ASH48` 那半句**故意没有用 `<br>`、改用 `。`**（官方 `ASH48_new` 用的是 `<br>`）—— 理由就是它今天渲染不出来。
   同族的 `((…))` 缺口见 `A1982`。**立账：`<br>` 需要一个渲染层**（要么转 `\n`、要么在 `CardIcons` 里吃掉）。
   🔴 **2026-10-15 更正（`AH` 笔 · 铁律 5）—— 上面这条【不成立】**：错因 = **只 grep 了我们自己的代码**、没查引擎。
   `<br>` **根本不经过我们的代码**，是 **TMP 自己的解析器**把它换成换行：
   `MyGame/Library/PackageCache/com.unity.ugui@…/Runtime/TMP/TMP_Text.cs:2277-2296`（`PopulateTextProcessingArray()`，
   `if (c == '<' && m_isRichText)` 之后 `case MarkupTag.BR:` ⇒ 塞一个 `unicode = 10` 的字符元素、`readIndex += 3`）；
   字面量表 `TMP_RichTextTagsCommon.cs:133` = `BR = 2256,  // <br> Line Feed (LF) \u000A`。
   原版侧同样**原样交给 TMP**（全量反编译 `d:/2/tools/decomp_full/` 里 `<br>` **0 命中**）。
   铁律 7 人眼判据：PnP 成品卡面印的就是两段（`Aeldari/3部队/Warpforge_7_Guardian-Defender.png` 的
   `Waystone.` 与 `Shuriken 2` 各占一行，而上一行右边还空着一大片 ⇒ 显式断行）。
   ⇒ **`ASH48` 那半句的 `。` 已于同日改回官方写法 `。<br>`**（落点 = `cardface_fixes.json` 的 `descZh` 列），
   卡池 md5 `64c21c90…` → `83a37522…`；断言落在 `Editor/BattleScene.cs` 的 ②·E。
   全过程 → `资料/普查产出_第三十一会话/AH_br与卡名.md`。
   ⚠️ 上面那句「**立账：`<br>` 需要一个渲染层**」**一并作废** —— 那一层**引擎里本来就有**（TMP），⛔ 不需要我们写。

4. ⚠️ `RuleEngineTest.cs:23022` 那句读数注释「（带 `rally` 86 / `strike` 23 / `slay` 9 / `backlash` 3 / `penitence` 1）」
   里的 `penitence 1` 现在变成 **5**（同样是 §2·5 那条，**另一处**要跟着改的文案）。⛔ 本笔不改（白名单外）。
5. ⚠️ **两处代码注释因为本笔改动作废、但本笔不能改**（`RuleEngine/Core/**` 不在白名单）：
   `Core/CardDef.cs:4252`「全池 10 张 `cardTraits=Frenzied` 的卡」—— 现在 `cardTraits` 里**没有 `Frenzied`** 了；
   `Core/RuleCore.cs:3601`「卡的 `cardTraits` 列里写的 `Frenzied` 指的就是它」—— 同上。**建议主对话顺手订正（铁律 5）。**
6. ℹ️ `Cosmic Serpent` 的 `desc` **本来就在** `cardface_fixes.json` 的 `desc` 列里（人工修过）——
   这解释了为什么它的 `desc` 有 PnP 那两句话、而官方 EN 只有一句。
7. ℹ️ `Saviour Protocols` 的 PnP 卡图文件名是 `Warpforge_63_…`（`TAU51`），与 `ASH48`→`52`、`SOR31`→`32`、`SAU58`→`58` 一样，
   **文件号与卡 id 不是一回事**（按分类目录重号）—— 谁下次按 id 数字找图会找错。

---

## 六、没查清 / 交回主对话

1. 🔴 **`RuleEngineTest.cs:2170` 的 `Has(penitence) = 1` 会红**（现算 = 5）—— 见 §2·5。**要么重钉基线、要么回退**，本笔停手。
2. 🔴 **§三·3 那 48 条候选**要逐张开 PnP 卡图（铁律 7）才能定案 —— 本笔只开了点名的那 3 张。**已立账，等派活。**
3. ⚠️ `_new` 键那一笔（§五·1）该不该让 `descZh` 取 `_new` 那代 —— **口径要主对话裁**：
   取 `_new` ⇒ 中文会**变成英文**（zh 列未译）⇒ 反过来更差；不取 ⇒ 那 2 张（`ASH48`/`SAU58`）**今天已经靠本笔的人工值补上了**。
4. ⚠️ 生成器这一侧的 `git diff` 里混着**别人未提交的存量**（`992/32` 那对数里本笔只占 5 处、约 60 行）——
   **提交时别把别人的活一起卷进去**（本笔未动 git）。
5. ⚠️ `cardTraits` 列的**语义边界**：它现在既是「真值 `CardTraits` 的镜像」又是「显示写法」，
   两者在 `frenzied`/`rage` 上**不可兼得**（改成卡面字就丢了枚举名）。本笔选了**卡面字**（判据 = 列的 doc 自称「显示写法」）。
   若将来有人要按它做 trait 统计，得先知道 `Destroyer` 这一格**可能来自 `frenzied`**。

---

## 七、验收读数（照抄，可复核）

```text
$ python -I -X utf8 工具/gen_cards_engine.py                ⇒ rc=0
  关键词铺底  真值 CardTraits 只补空：命中 124 张 · 追加 139 个 trait（改前 119 / 134）
  护甲值列    armour 列 >0 共 81 张 · 取 PnP 那一层 81 · 真值兜底 0（改前 80 / 80 / 0）
  descZh 层序 断言：cardface_fixes.descZh（人工、最高序）最终生效 255/255 条 · 违例 0 ✅（改前 251/251）
  裸卡名键唯一性 ✅
$ python -I -X utf8 工具/gen_cards_engine.py --check        ⇒ rc=0 · 「一致 ✅」（幂等）
$ bash 工具/ruleprobe.sh check                              ⇒ rc=1（已知：GSC29 那 1 行 + ⑦ 指纹哨兵未钉）
  全池 1121 张 · 解析差异 1 行 · 改名 0 行 · 新增 172 · 消失 171   ← 与 AB 那笔【逐字相同】
$ bash 工具/ruleprobe.sh layers <tmp>                       ⇒ rc=0 · cards=1127 auras=40 willrunOps=1334
  SOR8/10/21/27/32 的 ch.trig 全 = 1 ⇒ BodyResolves(penitence) = 真
$ TMPDIR=/tmp/wf_ad bash 工具/typecheck.sh                  ⇒ 运行时 0 错 / 编辑器 0 错
卡池 MyGame/Assets/RuleEngine/Resources/cards_engine.json
  1127 张 · v9 · md5 8aa9b9bf5adfa0a7e249c14e8ef56cce ⇒ 64c21c901ade424b34ad0d0baf0298af
```

**改了哪些文件**：`Unity/工具/gen_cards_engine.py`（5 处：`TRAIT_DISPLAY` 两个值 + 头注 · `TRAIT_ENGINE_UNREGISTERED` ·
`fix_own_armour` 闸 · `cardTraits` 列注释 · 摘要文案）·
`Unity/数据/游戏数据/cardface_fixes.json`（+5 行：4 条 `descZh` + 1 条说明键）·
生成物 `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`。
**行尾**：两个源文件改前改后**都是纯 CRLF**（`CRLF == LF`）✅ · ⛔ 未用 `sed -i` · ⛔ 未动 git · ⛔ 未跑 Unity 批处理。
