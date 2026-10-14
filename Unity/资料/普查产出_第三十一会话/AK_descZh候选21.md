# `AK` · 逐张裁定 `descZh` 1–2 词档候选（**只读、只交报告**）

> 写手：只读裁定代理。⛔ 未改任何文件（唯一新建 = 本报告）· ⛔ 未跑 Unity / 生成器 / `ruleprobe` · ⛔ 未动 git。
> 判据快照：`MyGame/Assets/RuleEngine/Resources/cards_engine.json` **md5 `83a37522c2c4839e52c4d9fde80a463b` · 1127 张**
> （mtime `2026-10-15 01:47:13`）。⚠️ `AD` 记的 `64c21c90…` 已不是现盘值；**我读到过 `SOR24` 的两个不同值**（见末节）。
> **权威判据 = PnP 成品卡图**（铁律 7）：`d:/2/Warpforge部队卡片/**`，**逐张开图亲读 23/24**。

---

## 汇总（真缺陷 **8** / 假阳 **16**（术语 **1** · 图标名 **0** · 其它 **15**）/ 没开图 **1**）

- **真缺陷 8** = 名单里 2 条**已被别的写手补上**（`SOR31` 由 `AD` 那笔 · **`SOR24` 现盘已含「所有」**）+ **新发现 6 条**：`SOR4` · `TAU65` · `GOF61` · `GOF84` · `AM72` · `AM76`。
- **假阳 15 条的细分**（都不是「中文缺词」）：
  · **纯虚词/词形噪声 6**（`a`/`an`/`and`/`×2` · `Gains`↔`Gain`）：`SOR25` `SAU43` `SOR71` `af98d42` `TL69` `ASH35`
  · **`descZh` 早就含那个信息**、是官方 EN 那一代少（7）：`SOR60` `UM57` `ASH34` `TL75` `UM22` `851f73da` `64a657f`
  · **判据是空的**（官方表无 `UM_Terminator` 词条）（1）：`UM_Terminator`
  · **代差改写、语义等同**（`is Praying` ↔ `has Pray`）（1）：`SOR39`
- **术语差 1**：`UM73`（`troop` 我们写`troop`/官方写 `unit`，中文「单位/部队」）。
- ⚠️ **名单条数与文里写的对不上**：§三·3 正文写「另 **21** 条」，**实测列了 24 条**、而同节分档表写「1 词 ×13 · 2 词 ×12 = **25**」⇒ **正文那个「21」与它自己的分档表矛盾**，且 24 ≠ 25（**可能漏 1 条**）。本笔裁的是**名单上那 24 条**。

## 逐条表

| 卡 id | 卡名 | 差的词 | PnP 卡面（抄录 · 亲读） | 开了图? | 裁定 |
|---|---|---|---|---|---|
| `SOR31` | Sister Dogmata | `+dies or` | ★未开（`AD` 已开·已修） | ✗ | **真缺陷·已修** |
| `SOR4` | Sister Novitiate | `+gain health` | `3☀: Gain +1 Health. Pray: Deploy a Sister Novitiate`（`Sorotitas/3部队/Warpforge_07_Sister-Novitiate.png`）＝我们 `desc` 逐字 | ✓ | 🔴**真缺陷**：`descZh` 只有`[[pray]]：((Deploy_x,$$SOR4$$))`，**整句「3☀：获得+1生命值」没了** |
| `SOR60` | Holy Fire | `+gain` | `…this turn. Gain 1☀`（`Sorotitas/5防御卡/Warpforge_64_Holy-Fire.png`）＝`desc` | ✓ | 假阳·其它（`descZh` 已有「获得 1 ☀」） |
| `TAU65` | War Shaper | `+strike heals` | `Strike: Heals 1. Talent: Grisly Feast`（`Tau/1督军/Warpforge_01_War-Shaper.png`）＝`desc` | ✓ | 🔴**真缺陷**：`descZh` 只有`[[talent]]：$$…$$`，**「攻击：治疗1」没了** |
| `TL69` | Neurotyrant | `+an whenever` | `…Whenever you play a non-Ephemeral Stratagem, create an Ephemeral copy of it in your hand`＝`desc` | ✓ | 假阳·其它（冠词 `an`；`Whenever`↔`When` 中文同作「当」）。⚠️`descZh` 反而多「1费的」卡面没有 |
| `UM57` | Primaris Invader | `+this turn` | `Codex: Give +1 Ranged Attack to all your Vehicles this turn`（`Ultramarines/3部队/invader.png`）＝`desc` | ✓ | 假阳·其它（`descZh` 已有「本回合」） |
| `UM73` | Oath of Moment | `+troop×2` | `Give +2 Health and "Strike: Trigger this **troop's** Codex ability" to a friendly **troop**`（`Ultramarines/4计策/oath of moment.png`）＝`desc` | ✓ | 假阳·**术语差**（中文写「单位」；卡面是 troop） |
| `SOR24` | Simulacrum Imperialis | `+all units` | `Pray: Give +1〔粉拳〕and +1〔紫枪〕**to all friendly units**`（`Sorotitas/3部队/Warpforge_30_Simulacrum-Celestian.png`）＝`desc` | ✓ | **真缺陷·现盘已修**（`descZh` 现为「给予**所有**友方单位…」；我早前读到旧值＝官方「一支友方单位」） |
| `SOR25` | Imagifier | `+a and` | `Pray: Give +1〔粉拳〕, +1〔紫枪〕and〔侧翼〕Flank **to a** friendly troop`（`Sorotitas/3部队/Warpforge_29_Imagifier.png`）＝`desc` | ✓ | 假阳·其它（虚词；官方那句是 `another` 才出的 delta） |
| `SOR39` | Prayer | `+has pray` | `…If the unit **has**〔祈祷〕Pray gain 1☀`（`Sorotitas/4计策/Warpforge_48_Prayer.png`）＝`desc` | ✓ | 假阳·其它（`descZh` 写「正在祈祷」＝官方旧代 `is Praying`，**语义等同、无缺词**） |
| `ASH34` | Avatar of Khaine | `+and camouflage` | `Armour 2. ② Gain〔爆裂〕Blast 6 **and**〔伪装〕Camouflage. Talent: …`（`Aeldari/3部队/Warpforge_42_Avatar-of-Khaine.png`）＝`desc` | ✓ | 假阳·其它（`descZh` 已有「获得爆裂 6 和伪装」） |
| `64a657f…` | Da Bigger Dey Iz... | `+and stomp` | `…give +1〔粉拳〕**and**〔践踏〕Stomp to your Warlord this turn`（`Orks/2天赋/Warpforge_02B_Da-Bigger-Dey-Iz.png`）＝`desc` | ✓ | 假阳·其它（`descZh` 已有「+1[攻击]和践踏」） |
| `AM72` | Vitus Gryf | `+strike` | `…**Strike**: Deploy a Tempestus Aquilon`（`Astra Militarum/3部队/Warpforge_08_Vitus-Gryf.png`）＝`desc` | ✓ | 🔴**真缺陷**：`descZh` 印 `[[slay]]`＝**「消灭」**，卡面是 Strike／攻击 |
| `AM76` | Armoured Lightning | `+armour` | `…give them〔盾〕**Armour 1**`（`Astra Militarum/4计策/Warpforge_12_Armoured-Lightning.png`）＝`desc` | ✓ | 🔴**真缺陷**：`descZh` 印「给他们**+1生命值**」 |
| `ASH35` | Wraithknight | `+gains` | `③ **Gains**〔无懈可击〕Invulnerable until your next turn`（`Aeldari/3部队/Warpforge_44_Wraithknight.png`）＝`desc` | ✓ | 假阳·其它（`Gains`↔官方 token 展开的 `Gain`，**词形差**） |
| `GOF61` | Rukkatrukk Squigbuggy | `+armour` | `**Armour 1**. Unstable. Strike: Deploy a Bomb Squig`（`Orks/3部队/Warpforge_29_Rukkatrukk-Squigbuggy.png`）＝`desc` | ✓ | 🔴**真缺陷**：`descZh` 印 `[[flank]]`＝**「侧翼」**（官方 base 那代），卡面是 Armour 1 |
| `GOF84` | Runtherd | `+ork` | `Mob: Create a random **Ork** Beast in your hand that costs 5 or less`（`Orks/3部队/Warpforge_09_Runtherd.png`）＝`desc` | ✓ | 🔴**真缺陷**：`descZh` 只有「随机的**野兽**」，**「兽人」没了** |
| `SAU43` | Solar Pulse | `+and` | `Blind all enemies, **and** they lose〔隐蔽〕Stealth and〔伪装〕Camouflage`（`Necron/4计策/Warpforge_55_Solar-Pulse.png`）＝`desc` | ✓ | 假阳·其它（虚词 and；`descZh` 全句都在） |
| `SOR71` | Adelaide the Serene | `+and` | `…Gain〔侧翼〕Flank **and**〔盾〕Shield. Rally: …`（`Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png`）＝`desc` | ✓ | 假阳·其它（虚词 and／官方把 shield 另起一句） |
| `TL75` | Termagant Brood | `+gain` | `…When a friendly unit triggers〔虫群〕Swarm, **gain +1〔紫枪〕**`（`Tyranid/3部队/Warpforge_6_Termagant-Brood.png`）＝`desc` | ✓ | 假阳·其它（`descZh` 已有「获得 +1 [远程]」；官方那代写的是 `deal 1 damage`） |
| `UM22` | Inceptor Sergeant | `+other` | `Codex: Give +1〔紫枪〕to your **other** troops`（`Ultramarines/3部队/inceptor srg.png`）＝`desc` | ✓ | 假阳·其它（`descZh` 已有「其他」） |
| `UM_Terminator` | Terminator | `+oath` | `**Oath 1**: Gain Armour 1`（`Ultramarines/3部队/Warpforge_18_Terminator.png`）＝`desc` | ✓ | 假阳·其它（**官方表里这张 0 词条** ⇒ 判据空；`descZh`「誓言 1：获得护甲 1」是完整的） |
| `af98d42…` | Legendary Commandant | `+an` | `Ephemeral. Choose an Astra Militarum Stratagem and add it to your hand.`（`Astra Militarum/2天赋/Warpforge_06_Legendary-Commandant.png`）＝`desc` | ✓ | 假阳·其它（冠词 `an`）。⚠️`descZh` 反而**多**「非传奇」卡面没有（官方 base 那句有） |
| `851f73da…` | Storm of Silence | `+troop` | `…①〔魂石〕Stun a random enemy **troop**`（`Aeldari/2天赋/Warpforge_02_Storm-of-Silence.png`）＝`desc` | ✓ | 假阳·其它（`descZh` 已有「部队」；官方那代是 `or stun a random enemy`） |

★ `SOR31` 的图**本笔没开**（`AD` §1·2 已亲读并已修）⇒ **没开图 1 张**，其余 23 张全开。
**每条的「卡面 ＝ 我们 `desc`」都成立**（除 `SOR31` 未复核）⇒ **本笔没有发现「`desc` 本身错」的卡**。

## 没查清 / 交回主对话

- **`SOR24` 的 `descZh` 我读到过两个版本**：早前一次读 = 官方记号串（`((Give_x_melee_and_y_ranged_to_a_friendly_unit,+1,+1))` ⇒ 展开成「**一支**友方单位」）；再读 = 「给予**所有**友方单位…」。两次之间 `cards_engine.json` 于 **01:47:13** 被重写（md5 `64c21c90…` → **`83a37522…`**）。⇒ **该条已被别的写手补上**；请主对话核一下是谁补的、有没有别条也一起被补（我**没有** git 权限去查改动面）。
- **`SOR24` 的卡名可疑**：我们 `name` = `Simulacrum Imperialis`，而**卡面与官方 `Card_Name/SOR24` 都是 `Simulacrum Celestian`**（`nameZh`「天界模拟体」与官方 zh 一致）。与 `TAU51 Saviour Protocolst`（`AH` 那笔）同族。
- **搜过哪些词 / 目录**：按卡**名**在 `d:/2/Warpforge部队卡片/**` 全树 glob（1375 张 PNG）找图；`Imperialis` / `Primaris` / `Invader` / `Oath` / `Inceptor` / `Novitiate` 逐个搜过 —— `Imperialis` **全库 0 命中**（故 `SOR24` 只能靠**官方名** `Simulacrum Celestian` 定位到 `Warpforge_30_…`，并用**数值 5/5/4/6 + Infantry** 复核对得上）。`卡牌信息权威表_0824.md` / `卡牌完整信息库_0824.md` 只用来定位，**判词一律以开图为准**。
- **未做**（按简报）：⛔ 没去推数字版（PnP 与数字版不是同一版数据）；⛔ 没开图以外任何资产（`card_stats.json` 等未读）。
