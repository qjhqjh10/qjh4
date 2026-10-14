# `AM` · `cardface_fixes.json` 侧收口批（四件）

> 写手：`AM` 笔（主对话按铁律 14 派下的四件，**单文件 + 它的输入，本笔是唯一写手**）。
> ⛔ 未动 git · ⛔ 未改正本（四份 + `资料/普查产出_第三十会话/**`）· ⛔ **未跑任何 Unity 批处理**（用户口径：自检先等等）。
> ✅ 跑过：**秒级类型检查**（运行时 0 错 / 编辑器 0 错）· `gen_cards_engine.py`（**rc=0**）· `--check`（**一致 ✅**）· `ruleprobe.sh check`（读数见 §五）。
> 读数时刻 ≈ **2026-10-15 02:10–02:25**。起点核对：改前卡池 md5 = `392d66475d3bb852885ca2fa02280c73`（= `AL` 记的**改后**值，逐字相同 ⇒ 起点接得上）。

---

## 〇、一句话结论（四件各一行）

| # | 件 | 状态 | 一句话 |
|---|---|---|---|
| **①** | 19 条 `descZh` 真缺陷 | ✅ **19/19 全改** | 🔴 **根因统一**：`descZh` 那一层是**官方旧一代**模板（`Card_Description/*` = 2025-07-30 远端快照），而 `desc` 是 PnP 那一代（`AD_descZh两代` 已记）。**能抄官方的一律抄**：2 张抄 `_new` 的中文列；改的那半句**逐句在官方表里找到同一句的中文**；3 张「印错关键词」只动那一个 token |
| **②** | `descZh` 缺 `<br>` | ✅ **7 处改完 · 16 处如实判「无接缝可插」** | 官方 zh 有 `<br>` 而我们没有的**共 23 张**；只有 **7 张**的官方那一刀落在**我们也有的句界**上 ⇒ 只插这 7 处；**其余 16 张一张没动**（15 张那一刀落在我们这一代根本没有的关键词段之后 · 1 张已经是真换行） |
| **③** | `keywords` 重复键 | ✅ **合并成一条** | `Fenrisian Drop Pod` 在同一对象里出现 2 次，**两条值逐字节相同**（`["Flying","Armour 1","Ferocity"]`，与池里现读一致）⇒ 删**靠前**那条（原本生效的就是靠后那条）⇒ **行为零变化** |
| **④** | `SOR24` 卡名 | ✅ **改完 + 同步 6 处键** | `Simulacrum Imperialis` → **`Simulacrum Celestian`**（官方 `Card_Name/SOR24` + PnP 卡面两票；`card_stats` 的 `ocrName` **本来就是** `Simulacrum Celestian`）· **`id` 一处未变（仍是 `SOR24`）** |

**卡池**：`1127` 张 · `v9` · md5 `392d66475d3bb852885ca2fa02280c73` → **`9d5f4affcd5916ab5343f8b01c24bed3`**

🔬 **改动面是逐卡比出来的**（⛔ 不是推的）—— 改前/改后池逐卡 dict 比对：

```text
cards: 1127 -> 1127        id sets identical: True
changed cards: 27          field counts: {'descZh': 26, 'name': 1}
  26 descZh = ①的 19 张 + ②的 7 张（两批不重叠）
  1  name   = SOR24（只有 name 一个字段变；ranged 已按 §三·⑤ 保回 4）
```

---
## 一、① 19 条 `descZh`（逐条 `OLD → NEW`）

> 判据出处 = `资料/普查产出_第三十一会话/AJ_descZh候选27.md`（13 条 · **27/27 全开图**）+ `AK_descZh候选21.md`（6 条 · **24 条里 23 张开图**）。
> 落点 = `数据/游戏数据/cardface_fixes.json` 的 `descZh` 列（**人工、最高序**）。**只改 `descZh`**，`desc` 一字未动（口径同 `AD`）。

| # | id | 卡 | 改前 → 改后 | 改的那半句「照谁写」 |
|---|---|---|---|---|
| 1 | `UM20` | Sergeant Telion | `[[camouflage]]。[[codex]]：((Give_trait_x_to_your_units_this_turn,[[sniper]]))` → `[[camouflage]]。[[codex]]：降低你手中随机一张步兵的费用 1 点` | 官方 `_new`? 无。**抄同句**：`Card_Description/GOF24` = `…[[mob]]：降低你手中随机一张步兵的费用 3 点`（同句、N 由 3→1） |
| 2 | `SAU15` | Deathmark | `[[remnant]]。[[sniper]]` → `[[remnant]]。[[sniper]]。[[rally]]：如果你没有控制其他军队，获得[[stealth]]` | 🔵 **整条抄官方 `Card_Description/SAU15_new` 的中文列**（与卡面同代） |
| 3 | `GOF13` | Deffgun Loota | `当另一支军队死亡时，获得 ((Ranged_attack_icon_x,+1))` → `当另一支军队死亡时，对一个随机敌人造成 1-2 点伤害` | **抄同句**：`Card_Description/BL28` 的中文含 `对一个随机敌人造成 1-2 点伤害` |
| 4 | `GOF99` | Skrag Every Stash! | `本回合给一个敌方单位[[vanguard]]，并让一个随机敌人失去[[stealth]]` → `对所有敌人造成 1 点伤害，并使他们失去[[stealth]]` | **抄同句尾**：`Card_Description/DA27` / `Card_Abilities/DA27` 的中文 `…并使他们失去[[stealth]]` |
| 5 | `SAU70` | Damaged Hexmark | `[[remnant]]。[[frenzied]]。[[artifice]]：自行攻击` → `[[remnant]]。[[frenzied]]。[[artifice]]：攻击生命值最低的随机敌人` | **抄同词**：`Card_Description/SAU73` 的中文 `[[artifice]]：对生命值最低的敌人造成 3 点伤害` |
| 6 | `TAU3` | Commander O'Maisos | `[[talent]]：$$…$$` → `[[shield]]。在你的回合内拥有[[flying]]。[[talent]]：$$…$$` | 官方 `_new`? 无。**补的是官方 base 也没有的那半句**（`Shield. Has Flying during your turn.`）；用池内既有中文词 |
| 7 | `6ed51d59…` | Duelist's Hubris | `[[ephemeral]]。向一支敌方军队施加 [[vulnerable]] 1 或者治疗己方单位 1 点生命值` → `[[ephemeral]]。选择一项：向一支随机敌方军队施加 [[vulnerable]] 1，或治疗你的战将 1 点生命值` | **抄同句**：`选择一项：` = 官方 `Card_Description/ASH71` / `d01a5ba5…`；`治疗你的战将 1 点生命值` = `Modular_Description/Heal_1_to_your_warlord`（`恢复己方战将的 1 点生命值`）同义 |
| 8 | `ASH70` | Swooping Hawk Exarch | `[[waystone]]. [[flying]] [[rally]]: 本回合给敌人 [[vanguard]]` → `[[waystone]]. [[flying]] [[rally]]: 给予一个单位 [[vanguard]]，直到你的下个回合` | 目标/时限照卡面；「直到你的下个回合」用池内主力写法（`直到你的下个回合`），官方另有一档 `在下一个己方回合之前` |
| 9 | `SOR30` | Celestian Sacresant | `[[vanguard]]。[[armour]] 1。[[pray]]：((Gain_x,[[armour]] 1))` → `[[vanguard]]。[[armour]] 1。[[pray]]：((Heal_x_to_a_friendly_unit,3))` | 🔵 **逐字节同官方 `Card_Description/SOR20`**（也 = `SOR30_new` 的英文列那半句） |
| 10 | `GSC29` | Metamorph Iconbearer | `…并在你的手牌中创建2个复制品` → `…并在你的手牌中创建2个复制品，并将其费用降低 1` | **抄同句**：`Card_Description/GSC75` 中文 `并将它们的费用降低 1` |
| 11 | `ASH52` | Hosts of the Dead | `消耗你所有的灵魂石。每消耗一颗，部署一个 $$ASH20$$` → `部署一个 $$ASH20$$。消耗你所有的灵魂石。每消耗一颗，部署一个 $$ASH20$$` | 补的是**首句**「部署一个幽卫」；`部署一个 $$ASH20$$` 就是官方 zh 自己那半句的写法 |
| 12 | `TAU40` | Technological Supremacy | `((Give_x_health_to_a_friendly_unit,+3))。如果它是载具或战斗服，治疗3点` → `…，给予其 [[longrange]]` | `[[longrange]]` = `Card_Trait/longrange`（现池 **7 处**在用）；官方 zh 那半句是**错的**（`治疗3点`） |
| 13 | `TL24` | Harpy | `[[flying]]. 아군 $$TL37$$이 죽을 때, 또 다른 하나를 배치합니다` → `[[flying]]。当一个敌方单位攻击时，部署一个$$TL37$$` | 🔵 **整条抄官方 `Card_Description/TL24_new` 的中文列**（base 那一列是**韩文**且效果不同 —— 简报点名的「特别当心」那一条） |
| 14 | `SOR4` | Sister Novitiate | `[[pray]]：((Deploy_x,$$SOR4$$))` → `((Faith_cost_x,3)) 获得 +1 生命值。[[pray]]：((Deploy_x,$$SOR4$$))` | 🔵 **逐字节同官方 `Card_Description/SOR67`** 的第一句（`((Faith_cost_x,3)) 获得 +1 生命值。`） |
| 15 | `TAU65` | War Shaper | `[[talent]]：$$…$$` → `[[strike]]：((Heal_1))。[[talent]]：$$…$$` | `((Heal_1))` = `TAU65_new` 英文列那一句；`Modular_Description/Heal_1` 的中文 = `恢复 1 点生命值`（宏现池 **0 处**在用，但键在） |
| 16 | `GOF61` | Rukkatrukk Squigbuggy | `[[flank]]。…` → `[[armour]] 1。[[unstable]]。[[strike]]：((Deploy_x,$$GOF3$$))` | 🔴 **印错关键词**：`[[flank]]`（侧翼）→ `[[armour]] 1`（卡面是 `Armour 1`）。**只动那一个 token**，其余照旧 |
| 17 | `GOF84` | Runtherd | `…创造一个随机的野兽，其费用为5或更少` → `…创造一个随机的兽人野兽，其费用为5或更少` | 补的是**「兽人」**（卡面 `random **Ork** Beast`）。官方 zh 那一句没有这个词 ⇒ 只补词、不改句式 |
| 18 | `AM72` | Vitus Gryf | `…。[[slay]]：部署一个$$AM71$$` → `…。[[strike]]：部署一个$$AM71$$` | 🔴 **印错关键词**：`[[slay]]`（消灭）→ `[[strike]]`（攻击）。**只动那一个 token** |
| 19 | `AM76` | Armoured Lightning | `…如果你控制一个载具，给他们+1生命值` → `…给他们 [[armour]] 1` | 🔴 **印错关键词**：`+1生命值` → `[[armour]] 1`（卡面 `give them **Armour 1**`）。**只动那一处** |

### 1·1 三条「印错关键词」为什么优先（简报点名）

`GOF61` / `AM72` / `AM76` —— 卡面印的是 **`Armour 1` / `Strike` / `Armour 1`**，而我们的 `descZh` 印的是 **`[[flank]]`（侧翼）/ `[[slay]]`（消灭）/ `+1生命值`**。
⇒ 卡面上会**多印一个侧翼、少印一个护甲**（`GOF61`）、**把「攻击」印成「消灭」**（`AM72`）、**把「护甲 1」印成「+1 生命值」**（`AM76`）—— 都是**玩家按字面就会理解错**的那一类。三张的 `keywords` 列**本来就是对的**（`['Armour 1','Unstable','Strike']` / `['Flying','Strike']` / `[]`），错的只有 `descZh` 正文里那一个 token。

### 1·2 🔴 一条「差点误判成第 4 个印错关键词」的（**本笔现核纠回来**）

`SAU70` 的 `descZh` 里有 `[[frenzied]]`，而它的 `keywords` 列是 `Destroyer` —— 看着像 `AH` §4·3 那一族「token 名 ≠ 卡面」。
**现读官方表**：`Card_Trait/frenzied` = **`['Destroyer', '毁灭者']`** ⇒ **`frenzied` 就是 `Destroyer` 的内部枚举名**，卡面渲染出来是「毁灭者」，**没有错**。
⇒ 本笔**没动它**（只改 Artifice 那半句）。这条写进报告是因为「token 名 ≠ 显示名」这一点很容易让下一轮把它当缺陷改掉。

### 1·3 ⛔ 没碰的（简报点名「别重复改」的 5 条 + 两批的假阳）

- **已被别的写手修过、本笔先现读现值确认「已修」⇒ 一条没重复改**：`TAU51`（AD 笔）· `ASH48`（AD 笔）· `SAU58`（AD 笔）· `SOR31`（AD 笔）· `SOR24` 的 `descZh`（AK 报「现盘已含『所有』」，本笔现读 = `祈祷：给予所有友方单位 +1 [攻击]和 +1 [远程]。` ⇒ 已修，未动）。
- **AJ 的 11 条假阳**（`GOF18` / `SOR13` / `f104d4c2` / `GOF29` / `GSC28` / `UM40` / `ASH12` / `GOF106` / `UM62` / `TL47` / `SOR29`）：**一条没动**（判据见 `AJ` §假阳分类：人工值已与卡面逐字对应 / 官方只是 `((…))` 记号未展开 / delta 来自我们 `desc` 自己）。
- **AK 的 16 条假阳 + 1 条术语差**（`SOR60`/`UM57`/`TL69`/`UM73`/`SOR25`/`SOR39`/`ASH34`/`64a657f…`/`ASH35`/`SAU43`/`SOR71`/`TL75`/`UM22`/`UM_Terminator`/`af98d42…`/`851f73da…`）：**一条没动**。
- ⚠️ **AK 报的「名单条数与正文写的对不上」**（正文「另 21 条」vs 实测 24 条 vs 分档表 25）：**本笔只做两批明确点名的 19 条**，那个数差**原样留给主对话**（⛔ 本笔没替它补第 25 条）。

---

## 二、② `descZh` 补 `<br>`（**7 处改完 · 16 处如实不插**）

### 2·1 🔴 先说清：`<br>` 对 `descZh` 的**任何消费者**有没有影响

**`descZh` 一共被 5 个地方读**（现扫 `MyGame/Assets/**` 的 `.cs`，⛔ 不是推的）：

| # | 读它的地方 | 会不会被 `<br>` 影响 |
|---|---|---|
| 1 | `BattleDriver.FaceTextFull`（`BattleDriver.cs:12728-12729`）→ `CardIcons.Rewrite` → **TMP** | ✅ **这是唯一「该被影响」的一处**：TMP 自己的解析器把 `<br>` 当 `LF`（`TMP_RichTextTagsCommon.cs:133` `BR = 2256`；判据 → `AH` §1·1）⇒ 卡面**该换行**。我们的代码**一处都不碰**它 |
| 2 | `CreatePool.MentionedCards`（中文那一趟） | ✅ **实测零影响**，见 §2·2 |
| 3 | `DeckEditorState.cs:457` 的**搜索命中**（`Hit(c.DescZh, needle)`） | ⚠️ 只可能影响「搜索词**正好跨过**那个插入点」的检索（例：搜 `护盾。集结`）；`<br>` 插在 `。` 之后，**不新增/不删除任何汉字**，其余检索一字不变 |
| 4 | `RelatedCards.cs:110`（详情窗「相关卡」） | = 走 2 那一口，同 §2·2 |
| 5 | 自检/编辑器探针（`BattleScene.cs:12838` 数 `<br>` · `IconSetup.cs:454/1159` · `RuleEngineTest` 那几处 `Contains`） | ✅ 逐条核过：§2·3 |

🔴 **另外两件必须写清楚的**：
- **`descZh` 从来不进解析器** —— `EffectText.cs:7445-7447` 写死这条（「`DescZh` 全仓**没有任何地方**喂进 `EffectText.Parse`」）⇒ **`<br>` 对 op 串、对 `ruleprobe` 的解析差异，结构上不可能有影响**。
- **全仓没有任何一处把 `<br>` 转成 `\n`**（现扫 `RuleEngine/**` + `CardPresentation/**` 的 `.cs`，`"<br>"` 只在两处出现：`BattleScene.cs` 那条自检的计数、`RuleEngineTest.cs:13329` 拼 markdown 表格）⇒ 「有消费者先归一化 `<br>`→换行、再分句」这条**路径不存在**。

### 2·2 🔬 `MentionedCards` 的命中数：**实测 0 变化**（这是简报点名要答的那一格）

**做法**：把 `CreatePool.MentionedCards` **逐字移植成一份离线 Python 演算台**（`d:/4/_tmp_am/mention.py`，仓外、非交付物；含 `Norm` / `NormCjk` / `HasCjk` / `Singular` / `StripTalentSegments` / `TalentCutAt` / `PickNamed` / 两趟扫描 + `AddMention` 的位置合并）。

**先验演算台可信**（拿现池跑，与 `RuleEngineTest.cs:19317-19318` 钉住的值对）：

```text
演算台（改前池 392d6647…）：UNION on descZh = 60 / 32   ← 与钉住的 60 / 32 逐字相同 ✅
                            英文那一趟跑在 descZh 上 = 0   ← 与代码注释里那句「命中 0 张」逐字相同 ✅
```

**再比改前/改后**：

```text
mentionsZh   before 60   after 60
distinctZh   before 32   after 32
per-card 逐卡比：differing = 0（1127 张，一片零差异）
英文那一趟跑在 descZh 上：after = 0
```

⇒ 🔴 **`RuleEngineTest` 那两条钉住的期望值（`60` / `32`）不用重钉，一条都不动。**
（原因也讲得通：`<br>` 只往中文文本里插 4 个 ASCII 字符，`NormCjk` 只收 `a-z0-9` + 汉字 ⇒ 它给归一化串加的只有 `br` 两个字母，**插在句界上 ⇒ 既不新增也不切断任何汉字串**；7 张的每一处又都**逐条 assert 过「去掉 `<br>` 逐字节还原」**。）

### 2·3 自检/编辑器那几条，逐条核过

| 断言 | 现状 | 受不受影响 |
|---|---|---|
| `BattleScene.cs:12838` 「`<br>` 必须一处不少地穿过 `CardIcons.Rewrite`」（`brAfter == brBefore`） | 现读带 `<br>` 的 `descZh` **60 张**（改前 53 张） | ✅ `<br>` **进得去也出得来**：那 7 张的 `descZh` 都**不含官方记号** ⇒ 走 `card_icon_plan` 那一支，而 `card_icon_plan.json` 里这 7 个 id **一条都没有**（现读 `plan.get(id) == None`）⇒ 原样返回。另 `RewriteMarks` 的四支正则（`[[…]]` / `((…))` / `$$…$$` / `(N)`）都匹配不到 `<br>` |
| 同一条的前提 `brCards > 20` | 53 → 60 | ✅ 更宽裕 |
| `IconSetup.cs:1159` `NormKey(c.DescZh)`（`⑤c` 的旁证串） | 加了 `br` 两个字母 | ✅ `⑤c` 是 `Log`/`Warning` **不是 `Check`**，且它拿 `NormKey(图名)` 去 `IndexOf` ⇒ 一个叫 `br` 的图名不存在 |

### 2·4 逐条（7 处，`OLD → NEW`；**只在已有的 `。` 句界上插**）

| id | 卡 | 官方 zh 那一刀落在哪 | 我们 | 改后 |
|---|---|---|---|---|
| `ASH34` | Avatar of Khaine | `[[armour]] 2。<br>…。`<br>`…。<br>[[talent]]：…` | `护甲 2。②：获得爆裂 6 和伪装。天赋：凯恩之怒。` | `护甲 2。<br>②：获得爆裂 6 和伪装。<br>天赋：凯恩之怒。` |
| `ASH64` | Devoted of Khaine | `…+1))。<br>((Spirit_x,1)) …` | `给予你的部队 +1 [攻击]。①重复此效果` | `…+1 [攻击]。<br>①重复此效果` |
| `33eabec1…` | Wrath of Khaine | `…+2,+2))。<br>((Spirit_x,2)) …` | `给予你的单位 +2 [攻击]和 +2 [远程]。②重复此效果` | `…+2 [远程]。<br>②重复此效果` |
| `SOR71` | Adelaide the Serene | `…[[flank]]。<br>[[rally]]：…` | `6 ☀：获得侧翼和护盾。集结：…` | `6 ☀：获得侧翼和护盾。<br>集结：…` |
| `SOR47` | Miraculous Feat | `…[[invulnerable]]<br>((Faith_cost_x,5))：…` | `本回合给予 1 个友方单位 [无敌] 无敌。6 ☀：…` | `…无敌。<br>6 ☀：…` |
| `UM23` | Bladeguard Lieutenant | `[[armour]] 1。[[vanguard]]<br>[[codex]]：…` | `护甲 1。先锋。典籍：治疗 3。` | `护甲 1。先锋。<br>典籍：治疗 3。` |
| `SOR44` | Moment of Grace | `…+2))<br>((Faith_cost_x,5))：…` | `本回合给予 1 个友方单位 +2 [攻击]和 +2 [远程]。5 ☀：抽 1 张牌` | `…+2 [远程]。<br>5 ☀：抽 1 张牌` |

**为什么是这 7 张、而不是全部 23** —— 口径照 `AL` §2·2/§2·3（`SOR71` 那条先例）：**官方的 `<br>` 必须落在「我们也有的那个句界」上才插**；
落在**官方比我们多的那一整段之后**的（`[[waystone]]。` / `[[ephemeral]]。` / `[[vanguard]]。` / `[[flying]]。` / `[[blast]] 4。` 这些**关键词段我们这一代 `descZh` 根本不写**）⇒ **没有接缝、⛔ 不硬插**。

### 2·5 ⛔ 16 处「无接缝可插」（**如实列出，不是漏做**）

| 类 | 张 | 卡 | 判读 |
|---|---|---|---|
| **甲 · 官方那一刀落在「我们这一代没有的关键词段」之后** | **15** | `ASH17` · `ASH8` · `ASH14` · `ASH20` · `ASH35` · `20d0f445…`(Da Red Waaagh) · `6ba4b132…`(Follower of Gork) · `3ebb1590…`(Greatest Warboss) · `9f4e1b7e…`(More Dakka) · `SOR19` · `4d348cff…`(Hymn of Battle) · `f104d4c2…`(Righteous Repugnance) · `SOR36` · `UM22` · `UM9` | 例：官方 = `[[waystone]]。<br>((Spirit_x,2)) …`，我们 = `②：部署 1 个幽卫`（**整条就一句**）⇒ `[[waystone]]。` 那一段我们没有，**没有东西可以在它前面断** |
| **乙 · 已经是真换行了** | **1** | `SOR56` Beacon of Faith | 我们的 `descZh` = `给予你的单位 +2 生命。\n4 ☀：…。\n7 ☀：…。`（**3 段真 `\n`**）⇒ 中文档下**本来就渲染成 3 行**，⛔ 不需要再插 `<br>` |

（甲类里 `f104d4c2…` 另有一层原因：官方那一代**整条少了 `4 能量：…` 那句** ⇒ 只有 1 处 `<br>`，落在 `[[ephemeral]]。` 之后。）

### 2·6 逐条 **assert**（简报点名要的那一条）

对 7 张逐条跑（脚本 `d:/4/_tmp_am/`）：

```text
① y.replace('<br>', '') == x        ← 7/7 「是纯插入」：只多 4 个 ASCII 字符，一个汉字都没动
② 分句结果：EffectText.Split 的口径 = new[]{'.','\n','\r'} + 每段 Trim()
   ⇒ 7/7 段数 1 → 1（这 7 处的接缝是【中文句号 `。`】、不是 `.`，所以它本来就不是分句符）
   ⇒ 「插前 == 插后」在这里的表现形式就是①「去掉 `<br>` 逐字节还原」；
     若接缝是 ASCII `.` 则分段逐字相同（`AL` 的 38 张就是那种形态）
③ ⛔ 另核了一层：**没有任何代码把 `<br>` 归一化成 `\n` 再分句**（见 §2·1 末两条）⇒ 不存在「换了分句结果」的第二条路径
```

---

## 三、③ `keywords` 重复键 `Fenrisian Drop Pod`

### 3·1 查清了什么（两条值分别是什么 / 哪一条该留）

现读 `cardface_fixes.json` 的 `keywords` 对象（` "keywords": {` … ` }`，共 **252 个键、其中 1 个重复**）：

```text
第 1 处（旧 1196-1200 行）   第 2 处（旧 1427-1431 行）
  "Fenrisian Drop Pod": [      "Fenrisian Drop Pod": [
   "Flying",                    "Flying",
   "Armour 1",                  "Armour 1",
   "Ferocity"                   "Ferocity"
  ],                           ],
```

🔴 **两条的值逐字节相同**（本笔逐字节比过，不是眼看）⇒ **合并/删哪一条，行为都零变化**。
另一处正在用的（`"_manual_keywords"` 里第 348 行）也是**同一份值**。

**判据（三条都指向同一条）**：
1. **JSON 语义**：同一个对象里同名键 = 后者覆盖前者 ⇒ **原本生效的是靠后那条**；
2. **池里现读**：`SW31 Fenrisian Drop Pod` 的 `keywords` = `["Flying","Armour 1","Ferocity"]`，与两条都一致；
3. **`_manual_keywords`**（脚本最后应用、手工赢）：同一份值 ⇒ 无论删哪条都是它。

⇒ **做法：删【靠前】那条**（保留原本生效的靠后那条）。改前改后池里 `keywords` 一个字节都没变。

### 3·2 顺手发现（⛔ 只报，没改）

- 🔴 **这个重复键是【手工写的】，不是脚本产生的** —— `工具/gen_cardface_fixes.py:352` 的合并是 `out[target][name] = val`（**按名赋值**）⇒ 脚本跑多少次都只会覆盖同名键、**不会新增重复键**。
  ⇒ 本笔删掉之后**不会被脚本加回来**（Python dict 按名赋值也不改已有键的位置 ⇒ 顺序稳定）。
- ⚠️ **`card_stats.json` 有一处缩进是 4 空格**（`AL` §五·2 报的 `:253621` 附近）—— 本笔现核仍在 ⇒ **再次印证该文件不能用结构重写**。

---

## 四、④ `SOR24` 卡名同步（**改了 6 处键 + 1 处值**）

### 4·1 为什么改（三票）

| 源 | 现读 |
|---|---|
| **官方 `Card_Name/SOR24`**（2026-10-14 解开的远端本地化包） | **`Simulacrum Celestian`** |
| **PnP 成品卡图**（`Sorotitas/3部队/Warpforge_30_Simulacrum-Celestian.png`，`AK` 亲开） | **`Simulacrum Celestian`** |
| **`card_stats.json` 的 `ocrName`** | **`Simulacrum Celestian`**（本来就是它！错的只有 `name`） |
| `card_ids.json`（真值表） | `SOR24 → Simulacrum Imperialis` 🔴 **站旧名**（同 `GSC64`/`GSC49`：真值表自己带错字） |

`nameZh` = **`天界模拟体`**（官方 zh 一致）**一处未动**。

### 4·2 同步了的 6 处键（简报说 5 个列，实测**多一处**）

| 文件 | 列 | 处数 | 备注 |
|---|---|---|---|
| `数据/游戏数据/card_stats.json` | **`name`**（源表） | **1** | 单行替换，锚点全行、改前 `count==1` |
| `数据/卡牌翻译/zh_cards.json` | `cards` 的**键** | **1** | 无同名键冲突（本来**没有** `Simulacrum Celestian` 那个键）⇒ **不是合并**，只需改键 |
| `数据/游戏数据/cardface_fixes.json` | **`desc`** | 1 | 值一字未动 |
| 同上 | **`descZh`** | 1 | 值一字未动 |
| 同上 | **`subtype`** | 1 | `Infantry` |
| 同上 | **`_manual_subtype`** | 1 | `Infantry` |
| 同上 | **`_manual_subtype_note`** 的说明键（🆕 **简报没点到的第 6 处**） | 1 | 那一格是**按卡名做键的散文说明**（`_manual_subtype_note`），键不跟就成了一格对不上任何卡的历史注释。**值（散文）一字未动** |

⚠️ **`_manual_keywords` 里没有这张卡**（现扫）⇒ 简报说的那一列本笔**没东西可改**。
⚠️ **`keywords` 列同样没有它**（现读 `cardface_fixes.json` 的 `keywords` 里 0 处命中 `Simulacrum`）。
⛔ **`card_ids.json` 那条旧名本笔没碰**（不在白名单 —— 有另一个写手在改它）。

### 4·3 验收

- ✅ **`id` 一处未变**：池里仍是 **`SOR24`**（判据 = 改前/改后池按 id 求差集，`id sets identical: True`）。
  **为什么没变**：`pick_truth_id`（`gen_cards_engine.py:739-776`）的候选表 ① 就是 **`by_official_en`**（官方英文名）⇒ 新名 `Simulacrum Celestian` **在官方表里直接命中 `SOR24`**。
- ✅ **`卡面修正表：所有键都匹配上了 ✅`**（没有孤立键 = 三张按卡名查的表都同步到位）
- ✅ **`同卡两键一致性 ✅`** · **`裸卡名键唯一性 ✅`**（`Simulacrum Celestian` 在全池只有 1 张）
- ✅ `--check` **一致 ✅**（幂等 = 不动点）

### 4·4 🔴 改名的**连带**（本笔现核抓到的真回归，已修）

**症状**：改完名字重跑卡池 ⇒ `SOR24` 的 **`ranged` 由 4 掉成 0**（其余字段一个没动）。
**根因**：`STAT_FIXES`（`工具/gen_cards_engine.py:399`）那条是**按【改名前】的卡名**做键的（`Simulacrum Imperialis → {"ranged": 4}`），而查表用的是 `name_before_rename`（`:1506` / `:1561`）⇒ 改名之后**查不到**、**静默**落回 `card_stats.json` 的 `ranged_attack`（那里是 `null`）。
**判读**：卡面是 **红 5 / 紫 4 / 绿框 6**（工具自己那条注释写着，PnP 也核过）—— `ranged=0` 是**错的**，而且是一张紫枪 4 的部队变成打不到人。
**本笔的做法**：按白名单 **⛔ 没碰 `工具/**`**，改在**根因那一格** —— `card_stats.json` 的 `"ranged_attack"` 由 **`null` → `4`**（值 = PnP 紫 4 = 改名前池里的值，**逐字相同**）。
**现在**：`SOR24` 的 `ranged` = **4** ✅（改后池逐卡比对里 `SOR24` 只剩 `name` 一个字段变）。
**还欠一步**：`STAT_FIXES` 里那条现在是**死键**（永远查不到），建议由 `工具/**` 的主人**删掉**（⛔ **别只把键改成新名** —— 那会让同一条规则两处并存）。本笔把这一段原话写进了 `cardface_fixes.json` 的 `_2026-10-15_AM_descZh收口` 说明里。

---

## 五、改了哪些文件 / 验收读数（照抄，可复核）

### 5·1 文件与行

| 文件 | 改动 | 行尾 | 字节 |
|---|---|---|---|
| `Unity/数据/游戏数据/cardface_fixes.json` | **+85 / −43**（19 条 `descZh` · 7 处 `<br>` · 删重复键 5 行 · 5 处卡名键 · 1 条说明键） | **CRLF 1927 / LF 1927（纯 CRLF）** | `192500 → 197522` B · md5 `28de924e… → 81c9e75873e0781073621347f871c7b1` |
| `Unity/数据/游戏数据/card_stats.json` | **+29 / −29**（1 处 `name` · 1 处 `ranged_attack`） | **CRLF 36003 / LF 36003** | `1172163 → 1172159` B · md5 `a47c50be… → 2108282561b9084f4514411df9e3b770` |
| `Unity/数据/卡牌翻译/zh_cards.json` | **+9 / −13**（1 处键） | **CRLF 4937 / LF 4937** | `151368 → 151367` B · md5 `52f4741d… → d4af11a393f85ae8819d37fe35a97176` |
| 生成物 `MyGame/Assets/RuleEngine/Resources/cards_engine.json` | 重跑覆盖（单行 JSON） | 单行 | `728 KB` · md5 `392d6647… → 9d5f4affcd5916ab5343f8b01c24bed3` |

- 🔴 **全程 python **字节级 `wb`** 拼接**（⛔ 未用 `sed -i`、⛔ **未用 `json.dumps` 结构重写**）。
  每处替换**前后都打 `count` 并 `assert == 1`**（重复键那处用「先定位 ` "keywords": {` 这一锚点、再在其后取**第一处**」，因为两条值逐字节相同、锚点本身不唯一）。
- 三份 JSON 改后都 `json.loads` 回读通过，并**逐条回读比对**（19 条 `descZh` 值 / 7 条 `<br>` 值 / 重复键唯一 / 6 处卡名键）。
- ⛔ **没碰**：`工具/**` · `RuleEngine/**` · `CardPresentation/**` · `card_ids.json` · 官方本地化 json · 四份正本 · `资料/普查产出_第三十会话/**`。
- 🗂 **改前副本**在 `d:/4/_tmp_am/bak/`（三张源表 + 改前卡池），要回滚拿它比。演算台与脚本也在 `d:/4/_tmp_am/`（仓外、非交付物）。

### 5·2 读数

```text
$ TMPDIR=/tmp/wf_am bash 工具/typecheck.sh        ⇒ 运行时错误数: 0 · 编辑器错误数: 0
$ python -I -X utf8 工具/gen_cards_engine.py      ⇒ rc=0
  卡面修正表：所有键都匹配上了 ✅
  同卡两键一致性 ✅（裸卡名键与 阵营/卡名 键没有对同一列给出不同的值）
  裸卡名键唯一性 ✅（每个裸键的卡名在全池只出现一次）
  `descZh` 层序  断言：cardface_fixes.descZh（人工、最高序）最终生效 274/274 条 · 违例 0 处 ✅
  真值快照     卡池 id 能在快照里找到 1009/1127 张（与 AL/AD/AH 那几笔逐字相同）
  卡 id        用原版 1104 张（真值索引那一层 1009）· 自造 23 张（共 1127）
  写出 …/cards_engine.json (728 KB)
$ python -I -X utf8 工具/gen_cards_engine.py --check  ⇒ rc=0 · 「一致 ✅」（幂等 = 不动点）
$ bash 工具/ruleprobe.sh check                    ⇒ rc=1（**已知**：GSC29 那 1 行 + ⑦ 指纹哨兵未钉；与 AB/AD/AH/AL 那几笔同形）
  全池 1121 张 · 解析差异 1 行 · 改名 3 行 · 新增 172 · 消失 171
  ⇒ 「解析差异 1 行」= **与改前逐字相同**（GSC29|Metamorph Iconbearer）⇒ 19 条 descZh 改写对 op 串零影响 ✔
  ⇒ 「改名 3 行」= GSC64 + GSC49（AL 那 8 条里的 2 条）+ **SOR24（本笔）** ⇒ 由 2 → 3，**如实报**
$ 卡池 MyGame/Assets/RuleEngine/Resources/cards_engine.json
  1127 张 · v9 · md5 392d66475d3bb852885ca2fa02280c73 → 9d5f4affcd5916ab5343f8b01c24bed3
  descZh 含 <br>：53 ⇒ 60（+7）
  改前/改后逐卡 dict 比对：changed 27 · ids identical True · 字段集合 {descZh×26, name×1}
$ 离线演算台（MentionedCards 移植）
  mentionsZh 60 → 60 · distinctZh 32 → 32 · 逐卡差异 0 · 英文趟 on descZh 0 → 0
```

⚠️ **本笔【没跑】任何 Unity 批处理**（简报明写）⇒ **卡面实际渲染成几行、19 张的中文实际怎么显示，只有真机 / `RuleEngineTest` 能验**。
本笔能给的最强证据 = **TMP 侧判据（`AH` 已证 `<br>`→换行）+ `MentionedCards` 离线复算零差异 + `ruleprobe` 解析零差异**。

---

## 六、顺手发现（**只报，⛔ 一条都没改**）

1. 🔴 **`card_ids.json`（真值表）第三条错字：`SOR24 → Simulacrum Imperialis`**（官方 `Card_Name/SOR24` 与 PnP 都是 `Simulacrum Celestian`）⇒ 与 `AL` §五·1 报的 `GSC64`/`GSC49` **同族、同一张表**。本笔**没碰它**（不在白名单、且另有写手在改它）。⇒ **那张表列一个新写手去清**。
2. 🔴 **`STAT_FIXES` 按旧名做键 = 一个静默陷阱**（详见 §四·4）：**任何**把这批 `ranged_attack=null 且 armor=N` 的卡改名，都会**静默**把 `ranged` 打成 0。全池现读「`ranged_attack` 为 null 且 `armor` 非 null」的卡 **6 张**（工具注释说其中 4 张是单位/督军、另 2 张是改名前的重复行）—— 本笔只碰了 `SOR24` 那一张。
3. ⚠️ **`_manual_subtype_note` 那一格是「按卡名做键的散文说明」**（简报说的「5 个列」没提到它）⇒ 下次改名时要**多同步这一处**，否则会留一格对不上任何卡的说明。
4. ⚠️ **`AH`/`AL` 那句「`Card_Name/*` 在 `Resources/` 下也没有」现读仍成立**：`RewriteMarkCardRefs`（`CardIcons.cs:623-638`）对 `$$ID$$` **一律「原样留着 + 出声」**（官方 id 那 116 种 / 151 处）。⇒ 本笔新写的 `$$ASH20$$` / `$$TL37$$` / `$$SOR4$$` / `$$GOF3$$` / `$$AM71$$` / `$$AM68$$` / `$$c4f5c920…$$` / `$$b3735ef3…$$` **与池内既有写法同形**，**不改变这条已知缺口**（属 `A1982`）。
5. ℹ️ **官方 `Card_Description/<id>_new` 两种存储形态并存**：`SAU15_new` / `TL24_new` 是 `['en','zh-CN']` **两列**、`SOR30_new` / `TAU65_new` / `SOR24_new` 是**裸字符串（只有英文）**。⇒ 下次要抄 `_new` 得先判类型（本笔对前两者抄了中文列，后三者只能自己译）。
6. ⚠️ **`AH` 报的「`descZh` 含 `<br>` 的 53 张」现读对得上**（改后 60 = 53 + 7）⇒ 那两个数互相印证。
7. ℹ️ **`AL` 报的 `SOR10` 那条历史（19 条改名已回滚、禁「只凭 PnP / 第三方改」）与本笔的 ④ 不冲突**：本笔的票是**官方 `Card_Name`**（2026-10-14 才解开的远端本地化包）+ `card_stats.ocrName`，**都不是 PnP 派生**。
   ⚠️ 但**如实记**：`工具/gen_cards_engine.py:396-400` 那段 2026-09-16 的注释写的是「**卡名别跟着 PnP 改**」并**点了这一张卡**（`Warpforge_30_Simulacrum-Celestian.png`）—— 那一条的**理由是「`card_ids.json` 用的就是我们现在这个名字」**，而那条理由**在 2026-10-15 之后不再成立**（已裁⑤ = 以官方 `Card_Name` 为准；`GSC64`/`GSC49` 就是同一形态被改掉的）。**那段注释与那条 `STAT_FIXES` 键都该由 `工具/**` 的主人收口**，本笔 ⛔ 没碰。
8. ⚠️ **`descZh` 侧没有 `<br>` 的等价物在 `desc` 侧**：`desc` 用**真 `\n`**（`AL` 那 38 张）、`descZh` 用 **`<br>`**（官方模板那一代就是 `<br>`）。两条并列存在**不是不一致**（`AL` 只改 `desc`、本笔只改 `descZh`），但下一轮别把其中一边改成另一边。

---

## 七、没查清 / 交回主对话

1. 🔴 **`card_icon_plan.json` 该不该重出** —— 本笔的 19 条 `descZh` 都**含官方记号** ⇒ 走 `RewriteMarks` 那一支、**绕开**按卡的计划表；7 处 `<br>` 那 7 个 id 在计划表里**一条都没有**（现读）。⇒ **本笔判断「不需要重出」**，但那份表在 `CardPresentation/Resources/`（**不在本笔白名单**）⇒ **请主对话核一眼**（它今天在 git 里已经是 M 状态）。
2. ⚠️ **卡面实际渲染**没验（⛔ 不许跑 Unity 批处理）⇒ 建议下次收口时拿真机或 `RuleEngineTest` 渲 3~5 张：**`TAU65 War Shaper`**（`((Heal_1))` 这个宏**现池 0 处置信度**、最该看一眼）· **`SOR30`**（`((Heal_x_to_a_friendly_unit,3))`）· **`SOR4`**（`((Faith_cost_x,3))`）· **`ASH34 Avatar of Khaine`**（两处 `<br>`）· **`GOF61`**（`[[armour]] 1`）。
3. ⚠️ **`AK` 报的「名单条数对不上」（21 / 24 / 25）**：本笔只做两批点名的那 19 条 ⇒ **可能还有第 25 条没被裁**，请主对话核。
4. ⚠️ **`AJ` 的顺手发现 #1（`SOR29` 的 `desc` 多写 `to an enemy troop`）与 #2（`TAU3` 的 `desc` 把 `Shield.` 抄进去了 ⇒ 关键词可能印两遍）**本笔**都没碰**（`desc` 不在本笔改动范围）⇒ 仍是**开着的账**。
5. ⚠️ **`AL` §五·3 那 13 处「官方宏值 vs 我们 `desc` 数字」对不上**（`SOR23` 官方 3/6 vs 我们 2/4 …）本笔**没碰**（本笔 19 条里 `SOR4` 用的 `((Faith_cost_x,3))` 与官方 `SOR67` 同值、`GSC29` 的 `2费` 是卡面原文）⇒ 仍开着。
