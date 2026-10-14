# `AJ` · `descZh` 候选 27 条（「多 ≥3 词」档）逐张裁定 —— 判据 = PnP 成品卡图

> 只读裁定，**未改任何文件**（本报告是唯一新建件）· ⛔ 未跑 Unity / 未动 git / 未碰正本 / 未跑生成器。
> 候选来源 = `资料/普查产出_第三十一会话/AD_descZh两代.md` §三·3 那张 27 条表。
> 数据源（**只读**）：`MyGame/Assets/RuleEngine/Resources/cards_engine.json`（池 1127 张）·
> `数据/游戏数据/官方本地化_en+zh-CN.json`（`terms` 9441 条）· `数据/游戏数据/cardface_fixes.json`（`descZh` 255 条）·
> `数据/游戏数据/card_stats.json`（旁证）。
> 读数时刻 ≈ 2026-10-15。

---

## 汇总

| 档 | 条数 |
|---|---|
| **真缺陷**（我们的 `descZh` 与 PnP 卡面不符） | **16**（其中 3 条 `TAU51`/`ASH48`/`SAU58` **AD 今天已修** ⇒ **仍开着 13 条**） |
| **假阳** | **11** |
| **没开图** | **0**（27/27 全部亲开） |
| 合计 | 27 |

🔴 **文件名与卡的对应已逐张核过**：27 张的**费用 + 近战/远程/生命/护甲**与卡池**逐一吻合、0 处不符**
（脚本比 `cost/attack/ranged/health/armour`；`SOR31→32`、`TAU51→63`、`ASH48→52` 这类「文件号 ≠ 卡 id」全按**卡名 + 数值**定位）。
⚠️ 其中 2 张卡图**没有编号前缀**（`Ultramarines/4计策/zeal.png` = `UM40`、`…/supremacy.png` = `UM62`）——按卡名找不到时**必须列目录**。

---

## 逐条表

| # | 卡 id | 卡名 | 差在哪（我们 vs 官方/我们的 `descZh`） | PnP 卡面（抄录关键句） | 开图 | 裁定 |
|---|---|---|---|---|---|---|
| 1 | `UM20` | Sergeant Telion | 官方/descZh 的 Codex 句是「给你的单位加 [[sniper]] 特性」——**和卡面不是同一句效果** | `Camouflage. Sniper. Codex: Lower the cost of a random Infantry in your hand by 1` | ✅ | **真**（不是「缺半句」，是**整句效果不同**） |
| 2 | `SAU15` | Deathmark | 官方/descZh 缺**整句** `Rally: …gain Stealth` | `Remnant. Sniper. Rally: If you control no other troops, gain Stealth` | ✅ | **真**（官方有 `_new` 键 = 卡面这代，可直接抄） |
| 3 | `TAU51` | Saviour Protocols | 官方缺第二句 `Choose a Drone and add it to your hand` | （同 AD §一·2） | ✅ | **真 · AD 已修** |
| 4 | `GOF13` | Deffgun Loota | 官方后半句 = `gain +1 Ranged`；卡面 = `deal 1-2 damage to a random enemy` | `When another troop dies, deal 1-2 damage to a random enemy` | ✅ | **真**（后半句效果完全不同） |
| 5 | `GOF18` | PainBoy Sniklaw | 官方只有 `Talent:` 一句；但 `descZh` 是**人工值**、与卡面逐字对应 | `Rally: Heal 3 to a friendly unit. Talent: Sawbonez` | ✅ | **假阳**（中文已满） |
| 6 | `GOF99` | Skrag Every Stash! | 官方整句不同（`Give vanguard to an enemy unit this turn…`）；卡面 = `Deal 1 damage…` | `Deal 1 damage to all enemies, and they lose Stealth` | ✅ | **真** |
| 7 | `SAU70` | Damaged Hexmark | 官方 Artifice 句 = `Attacks by itself`；卡面 = `Attacks a random enemy with lowest Health` | `Remnant. Destroyer. Artifice: Attacks a random enemy with lowest Health` | ✅ | **真** |
| 8 | `SOR13` | Simulacrum Bearer | 官方缺 `and to your Warlord this turn`；`descZh` 人工、与卡面一致 | `Pray: Give +1 <拳> to your troops, and to your Warlord this turn` | ✅ | **假阳** |
| 9 | `TAU3` | Commander O'Maisos | 官方/descZh 只有 `天赋：$$…$$`，**缺 `Shield. Has Flying during your turn.`** | `Shield. Has Flying during your turn. Talent: Drone Companion` | ✅ | **真**（真缺的那半句 =「你的回合内具有飞行」；`Shield` 在 `keywords` 列里） |
| 10 | `f104d4c2…` | Righteous Repugnance | 官方 base 缺 `4 Energy: …` 整句；官方 `_new` 仍缺 `and Heal them 1`；`descZh` 人工、与卡面一致 | `Ephemeral. Give +1 <拳> and +1 <枪> to your units this turn. 4 <能量>: Give +2 <拳> and +2 <枪> instead and Heal them 1` | ✅ | **假阳** |
| 11 | `6ed51d59…` | Duelist's Hubris (Lucius' Talent) | 官方/descZh = 「治疗**己方单位** 1 点」且**无** `Choose one`；卡面 = `Heal 1 to your **Warlord**` + `Choose one` | `Ephemeral. Choose one: Give <Vulnerable> Vulnerable 1 to a random enemy troop or Heal 1 to your Warlord` | ✅ | **真**（目标错 + 丢 `Choose one`） |
| 12 | `ASH70` | Swooping Hawk Exarch | 官方/descZh = 「本回合给**敌人** vanguard」；卡面 = 给**一个单位**、**直到你下个回合** | `Waystone. Flying Rally: Give Vanguard to a unit until your next turn` | ✅ | **真**（目标 + 时限双错） |
| 13 | `GOF29` | Grukk Face-Rippa | 官方只有 `Talent:` 一句；`descZh` 人工、与卡面一致 | `Your troops with Tide have +1 <拳>. Talent: Da Red Waaagh` | ✅ | **假阳** |
| 14 | `GSC28` | Hulking Aberrant | 官方是 `+3 melee this turn`（另一代）；`descZh` 人工、与卡面一致 | `Armour 1 Concussive. Ambush: Gain +2 <拳> and Stun a random enemy` | ✅ | **假阳** |
| 15 | `SOR30` | Celestian Sacresant | 官方/descZh 的 Pray = 「获得护甲 1」；卡面 = `Pray: Heal 3 to a friendly unit` | `Vanguard. Armour 1 Pray: Heal 3 to a friendly unit` | ✅ | **真**（Pray 效果完全不同） |
| 16 | `UM40` | Avenging Zeal | 官方末句 = `when you trigger codex`；`descZh` 人工、与卡面一致 | `Give +2 <拳> and +2 <枪> to all your units in play and in hand. Lower cost by 1 when a friendly Troop or Stratagem trigger <Codex> Codex` | ✅ | **假阳** |
| 17 | `GSC29` | Metamorph Iconbearer | 官方/descZh **缺末半句** `Lower their cost by 1` | `Rally: Choose a 2-cost Genestealer Cults troop and create 2 copies in your hand Lower their cost by 1` | ✅ | **真** |
| 18 | `SOR29` | Ephrael Stern | `descZh` = 官方值、**与卡面一致**；但**我们的 `desc` 多写了 `to an enemy troop`**（卡面没有） | `Camouflage. Pray: Deal 2-3 damage. If target dies, gain 1 <faith>` | ✅ | **假阳**（另立一笔 → §四） |
| 19 | `UM62` | Battlefield Supremacy | `descZh` = 官方值，**两句都在**（第二句是 `((Draw_a_Vehicle))` 记号） | `Lower the cost of all Vehicles in your hand and deck by 1. Draw a Vehicle from your deck` | ✅ | **假阳**（记号未展开 = 渲染层欠账，不是缺句） |
| 20 | `ASH12` | Swooping Hawk | 官方**一条能力句都没有**；`descZh` 人工、与卡面一致 | `Waystone. Flying. <1 灵魂石> Gain +1 <枪> and Flank` | ✅ | **假阳** |
| 21 | `ASH48` | Cosmic Serpent | 官方缺第二句 | （同 AD §一·2） | ✅ | **真 · AD 已修** |
| 22 | `ASH52` | Hosts of the Dead | 官方/descZh **缺首句** `Deploy a Wraithguard.` | `Deploy a Wraithguard. Spend all your Spirit Stones. For each one, deploy a Wraithguard` | ✅ | **真** |
| 23 | `GOF106` | Power of the Waaagh! | `descZh` 人工、与卡面一致 | `Your Warlord gains "Mob: Gain +1 <拳> until your next turn"` | ✅ | **假阳** |
| 24 | `SAU58` | Implacable Conqueror | 官方缺第二句 | （同 AD §一·2） | ✅ | **真 · AD 已修** |
| 25 | `TAU40` | Technological Supremacy | 官方/descZh 末句 = 「治疗 3 点」；卡面 = `give it Long Range` | `Give +3 Health to a friendly unit. If it's a Vehicle or Battlesuit, give it <Long Range> Long Range` | ✅ | **真** |
| 26 | `TL24` | Harpy | 🔴 官方 base 是**韩文**且效果不同（`아군 $$TL37$$이 죽을 때…`）；官方 `_new` 才是卡面那代 | `Flying. When an enemy attacks, deploy a Spore Mine` | ✅ | **真**（本批**最重**的一条） |
| 27 | `TL47` | Hive Fleet Leviathan | `descZh` = 官方值，**两句都在**（第二句是 `((Draw_a_troop))` 记号） | `Your troops cost 1 less for the rest of this battle. Draw a troop from your deck` | ✅ | **假阳**（同 19） |

---

## 假阳分类（11 条，判据逐类给）

| 类 | 条数 | 卡 | 判据 |
|---|---|---|---|
| **甲 · `descZh` 已被人工值覆盖、且与卡面逐字对应** | **8** | `GOF18`·`SOR13`·`f104d4c2`(Righteous Repugnance)·`GOF29`·`GSC28`·`UM40`·`ASH12`·`GOF106` | 这 8 张在 `cardface_fixes.json` 的 `descZh` 里**都有键**（现读），池里的 `descZh` **≠ 官方 zh**；逐张开图比 ⇒ 中文把卡面两句都译全了。delta 全部来自「官方条目是**另一代**效果」。 |
| **乙 · `descZh` = 官方值，官方两句都在，只是第二句是 `((…))` 记号** | **2** | `UM62`·`TL47` | 官方 `((Draw_a_Vehicle))` / `((Draw_a_troop))` 是**可展开的记号**（`AD` §3·1 已证 340 条 `((Key,a,b))` 全在表里）；展开后两句齐全 ⇒ **不是「少一句」**，是**渲染层没展开**（`AD` §五·3 那笔 / `A1982`）。 |
| **丙 · `descZh` = 官方值、与卡面一致，delta 全来自【我们 `desc` 自己多写】** | **1** | `SOR29` | 卡面没有 `to an enemy troop`（亲读），官方也没有，**只有我们的 `desc` 有** ⇒ 该修的是 `desc` 不是 `descZh`。 |

---

## 顺手发现（只报，一条都没改）

1. 🔴 **`SOR29 Ephrael Stern` 的 `desc` 比 PnP 卡面多一句 `to an enemy troop`**（卡面亲读 = `Pray: Deal 2-3 damage. If target dies, gain 1 <faith>`）。属**另一笔**（改 `desc` 前要守 `warpforge-card-data-vs-pnp` 那条：先拿 `zh_CN.csv` 精确查）。`card_stats.json` 同一条也写成 `Deal 2-3 damage.`（**没有** `to an enemy troop`）⇒ 两处我们自己不一致。
2. 🔴 **`TAU3` 的 `desc` 把关键词也抄进去了**（`desc` = `Shield. Has Flying during your turn. Talent: Drone Companion`，而 `keywords` 列 = `['Shield','Talent: Drone Companion']`）⇒ 卡面会把 `Shield` / `Talent: …` **印两遍**。同批 27 张里**只有它**是这个形态（其余 26 张的 `desc` 都不含关键词句）。
3. ⚠️ **`TAU51` 的卡名**（`AD` §五·2 报过）**本笔复核成立**：卡面印 `Saviour Protocols`，池里叫 `Saviour Protocolst`（多一个 `t`）。
4. ℹ️ **官方 `_new` 键只有 6 张**落在本批：`SAU15`·`SOR30`（值是坏值 `"["`）·`ASH48`·`SAU58`·`TL24`·`f104d4c2`。其中 **`SAU15`/`ASH48`/`SAU58`/`TL24` 的 `_new` 与卡面同代** ⇒ 这 4 张有现成判据可抄；另 12 张真缺陷**没有** `_new`，只能人工补译。

---

## 没查清 / 搜过什么

- **27/27 全部亲开卡图**（唯一权威判据，铁律 7）—— **没有一条是「没开图」**。
- 走法：`find "d:/2/Warpforge部队卡片" -iname "*<关键词>*" -name "*.png"`，24 组关键词；两组落空（`Avenging` / `Battlefield` / `Zeal`）后**列了 `Ultramarines/` 全目录**才找到无编号的 `4计策/zeal.png`、`4计策/supremacy.png`。
- 数据侧只读了 4 个文件（见文首），**没跑生成器 / 没跑 `ruleprobe`**（简报禁止）。
- **没查清**：除 `UM62`/`TL47` 外，其余 25 张 `descZh` 里的 `((…))` / `[[…]]` / `<nobr><link>` 记号在我们卡面上**实际渲染成什么**（那是渲染层，`A1982`）—— 本笔只判「文本层是否缺句」。
- **没查清**：`SOR30` 的官方 `_new` 值 = `"["`（坏值）⇒ 那一档判据为空。
- **没查清**：官方 9441 条里**还有没有别的键**（非 `Card_Description/<id>`）可以抄同句 —— 本笔只按 `Card_Description/<id>` 与 `<id>_new` 两条查。

---

## 倾向（裁定与修法由主对话下）

「我们比官方多出字面」这条判据在 **≥3 词这一档的精度 ≈ 59%**（16 真 / 27），**假阳 11 条**集中在「官方是旧一代」与「记号未展开」两件事上。
⇒ 倾向：**逐条按 PnP 卡面写 `descZh`、落 `cardface_fixes.json` 的 `descZh` 列**（⛔ 不改 `desc`，与 `AD` 口径一致）；
4 张（`SAU15`/`ASH48`/`SAU58`/`TL24`）优先抄官方 `_new`。另 `SOR29` 的 `desc` 多字属**单独一笔**。
