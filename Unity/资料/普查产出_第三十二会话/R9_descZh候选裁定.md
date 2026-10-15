# `R9` · `descZh`「换代后中文比英文旧一代」那批候选 —— 52 条逐张开 PnP 卡图裁定

> 写手：只读裁定代理（`R9`）。**唯一新建件 = 本报告**。⛔ **未改任何文件**（含两张正本 / 任何 `.cs` `.py` `.json`）· ⛔ 未动 git · ⛔ **未跑 Unity**（本机同工程只能一个实例）。
> 判据快照：卡池 `MyGame/Assets/RuleEngine/Resources/cards_engine.json` **1127 张 · v9** · `cardface_fixes.json` 的 `descZh` 人工层 **274 条** · 官方表 `数据/游戏数据/官方本地化_en+zh-CN.json`（`terms` 9441）。
> 权威尺子 = **PnP 成品卡图**（铁律 7）：`d:/2/Warpforge部队卡片/<阵营>/<1督军·2天赋·3部队·4计策·5防御卡>/*.png`。**52/52 张全部亲开**（下表路径均省略公共前缀 `d:/2/Warpforge部队卡片/`）。
> 读数时刻 ≈ 2026-10-15（第三十二会话）。

---

## 一、候选总数与来源

**候选总数 = `52`**（现读复现，⛔ 不是抄的）：
用 `AD` 留下的判据②脚本（`d:/4/_tmp_view/ad_gen_cmp6.py`，即「离线展开官方记号 → 剥标签 → 去纯数字 → **字面多重集差**」）在**现盘卡池**上重跑 ⇒ 打印
`可比 855 · 我们多 52 · 官方多 253` —— 与 `AD` §3·2 记的**逐字相同**（855/52/253 三个数全对）。
分档也复现：**多出 ≥3 词 × 27** + **1–2 词 × 25** = **52**。

**上一会话已消化的 19 条**（`AM_descZh收口.md` §一，逐条 19/19）**已剔出**；此外 `AD` 自己那 4 条（`SOR31`/`ASH48`/`SAU58`/`TAU51`）与 `SOR24`（写手未署名，见 `AM` §1·3）也已改到位。
⇒ 逐件归账后：**真正「从未被任何一笔裁过」的只有 `1` 条 —— `UM44 Armoured Support`**（它就是 `AD` §3·3 那个「清单 24 条 vs 分档表 25 条」**少列的那一条**，见 §四·0）。

**本件做的事**：把 52 条**全部**重新过一遍表，**52/52 张亲开 PnP 卡图**重读（不为省事引用别人的读数）；其中
**新裁出缺陷 4 条**（`UM44` · `af98d423` · `TL69` · `UM73`）—— 这 4 张**都不在** `cardface_fixes.json` 的 `descZh` 人工层（现读 274 条里没有它们）⇒ 落点与 `AD`/`AM` 同一通道。
⚠️ 另：`AJ`（27/27 开图）与 `AK`（24 条里 23 开图）是**上一会话已经做过的两轮同主题开图裁定**（与 `AM` 同一批），本件与它们的结论**逐条比对过**，分歧只有 2 处（`af98d423`/`TL69` 它们记了事实但判成「假阳」，本件判成**真缺陷**，理由见 §三）。

---

## 二、逐条裁定

> 裁定只有三种取值：`我们错 → 应改成 X` / `我们本来就对` / `判不了`。
> 「已修」= 2026-10-15 当天已由 `AD` 或 `AM`（`SOR24` 未署名）按卡面改到位，**本件复读卡面确认改对了**。
> 「我们 vs 官方哪一代」列：`我们 desc = PnP 那代` / `官方 base = 旧代` 的意思；🔴 新代 = **官方 `<键>_new`**（本会话已核：`_new` 那代才与 PnP 同代）。

| # | 卡 id | 卡名 | 池 `descZh`（截断 ≤120 字） | 我们 vs 官方哪一代 | **PnP 卡面我读到的** | 裁定 | PnP 文件路径 |
|---|---|---|---|---|---|---|---|
| 1 | `ASH70` | Swooping Hawk Exarch | `[[waystone]]. [[flying]] [[rally]]: 给予一个单位 [[vanguard]]，直到你的下个回合` | 我们 `desc`=PnP 那代；官方 base=旧代（`to an enemy this turn`） | `Waystone. Flying Rally: Give Vanguard to a unit until your next turn` | **已修**（`AM`） | `Aeldari/3部队/Warpforge_07_Swooping-Hawk-Exarch.png` |
| 2 | `ASH12` | Swooping Hawk | `①：获得 +1 [远程]和侧翼` | 官方 base **整条能力句都没有**（只有 `[[waystone]]. [[flying]]`）⇒ 假阳 | `Waystone. Flying. ① Gain +1 (紫枪) and (侧翼) Flank` | 我们本来就对 | `Aeldari/3部队/Warpforge_16_Swooping-Hawk.png` |
| 3 | `ASH34` | Avatar of Khaine | `护甲 2。<br>②：获得爆裂 6 和伪装。<br>天赋：凯恩之怒。` | 官方 base 是**另一代效果**（`Gain Blast 4`、**无 Camouflage**）⇒ 假阳 | `Armour 2. ② Gain Blast 6 and Camouflage. Talent: Wrath of Khaine` | 我们本来就对 | `Aeldari/3部队/Warpforge_42_Avatar-of-Khaine.png` |
| 4 | `ASH48` | Cosmic Serpent | `触发你所有军队需要灵魂石的能力。<br>②重复此效果` | 我们 `desc`=PnP 那代 = **官方 `_new` 那代**；base=旧代（少第二句） | `Trigger the abilities requiring Spirit Stones of all your troops. ② Repeat this effect` | **已修**（`AD`） | `Aeldari/4计策/Warpforge_52_Cosmic-Serpent.png` |
| 5 | `ASH52` | Hosts of the Dead | `部署一个 $$ASH20$$。消耗你所有的灵魂石。每消耗一颗，部署一个 $$ASH20$$` | 我们 `desc`=PnP 那代；base=旧代（**整条缺首句**） | `Deploy a Wraithguard. Spend all your Spirit Stones. For each one, deploy a Wraithguard` | **已修**（`AM`） | `Aeldari/4计策/Warpforge_62_Hosts-of-the-Dead.png` |
| 6 | `851f73da` | Storm of Silence | `你的战将本回合获得 +2 [攻击]。①：眩晕 1 个随机敌方部队` | 官方 base 是另一代（`… or stun a random enemy`，**无 troop**）⇒ 假阳 | `Ephemeral. Your Warlord gains +2 (粉拳) this turn. ① Stun a random enemy troop` | 我们本来就对 | `Aeldari/2天赋/Warpforge_02_Storm-of-Silence.png` |
| 7 | `ASH35` | Wraithknight | `③：获得 [无敌] 无敌直到你的下个回合` | 官方 base 少 `Armour 2` / `Shuriken 8` 的**卡面句**（我们放 `keywords` 列）⇒ 假阳 | `Armour 2. Shuriken 8. ③ Gains Invulnerable until your next turn` | 我们本来就对 | `Aeldari/3部队/Warpforge_44_Wraithknight.png` |
| 8 | `AM72` | Vitus Gryf | `[[flying]]。拥有[[flying]]的友方军队拥有[[flank]]。[[strike]]：部署一个$$AM71$$` | 我们 `desc`=PnP；base=旧代（**`[[slay]]`=消灭**） | `Flying. Friendly troops with Flying have Flank. Strike: Deploy a Tempestus Aquilon` | **已修**（`AM`） | `Astra Militarum/3部队/Warpforge_08_Vitus-Gryf.png` |
| 9 | `AM76` | Armoured Lightning | `部署三个 $$AM68$$ 并赋予他们 [[vanguard]]。如果你控制一个载具，给他们 [[armour]] 1` | 我们 `desc`=PnP；base=旧代（`+1 Health`） | `Deploy three Tempestus Scion and give them Vanguard. If you control a Vehicle, give them Armour 1` | **已修**（`AM`） | `Astra Militarum/4计策/Warpforge_12_Armoured-Lightning.png` |
| 10 | `af98d423` | Legendary Commandant | `[[ephemeral]]。选择一张**非传奇**星界军计谋并加入你的手牌` | 🔴 **反方向**：我们 `desc`=PnP 那代（**无** `non-Legendary`）；官方 base=旧代（**有** `non-Legendary`）⇒ `descZh` 抄了旧代 ⇒ 多一个限定词 | `Ephemeral. Choose an Astra Militarum Stratagem and add it to your hand.`（卡面**没有** `non-Legendary`；我们的 `desc` 逐字一致） | **我们错 → 应改成 `[[ephemeral]]。选择一张星界军计谋并加入你的手牌`**（判据：删掉卡面没有的限定词；官方表把 `non-Legendary` 译「非传说/非传奇」的制度见 §顺手发现3） | `Astra Militarum/2天赋/Warpforge_06_Legendary-Commandant.png` |
| 11 | `6ed51d59` | Duelist's Hubris (Lucius' Talent) | `[[ephemeral]]。选择一项：向一支随机敌方军队施加 [[vulnerable]] 1，或治疗你的战将 1 点生命值` | 我们 `desc`=PnP；base=旧代（目标错 + 无 `Choose one`） | `Ephemeral. Choose one: Give Vulnerable 1 to a random enemy troop or Heal 1 to your Warlord` | **已修**（`AM`） | `Emperor_s Children/2天赋/Warpforge_04_Duelists-Hubris.png` |
| 12 | `GSC28` | Hulking Aberrant | `震荡。伏击：获得 +2 [攻击]并眩晕一个随机敌人。` | 官方 base 是另一代（`+3 melee this turn`、无 Stun）⇒ 假阳 | `Armour 1 Concussive. Ambush: Gain +2 (粉拳) and Stun a random enemy` | 我们本来就对 | `Genestealer Cult/3部队/Warpforge_35_Hulking-Aberrant.png` |
| 13 | `GSC29` | Metamorph Iconbearer | `[[rally]]: 选择一支2费基因窃取者教团军队，并在你的手牌中创建2个复制品，并将其费用降低 1` | 我们 `desc`=PnP；base=旧代（缺末半句） | `Rally: Choose a 2-cost Genestealer Cults troop and create 2 copies in your hand Lower their cost by 1` | **已修**（`AM`） | `Genestealer Cult/3部队/Warpforge_31_Metamorph-Iconbearer.png` |
| 14 | `SAU70` | Damaged Hexmark | `[[remnant]]。[[frenzied]]。[[artifice]]：攻击生命值最低的随机敌人` | 我们 `desc`=PnP；base=旧代（`Attacks by itself`） | `Remnant. Destroyer. Artifice: Attacks a random enemy with lowest Health` | **已修**（`AM`） | `Necron/3部队/Warpforge_06_Damaged-Hexmark.png` |
| 15 | `SAU15` | Deathmark | `[[remnant]]。[[sniper]]。[[rally]]：如果你没有控制其他军队，获得[[stealth]]` | 我们 `desc`=PnP = **官方 `_new` 那代**；base=旧代（整条缺 Rally 句） | `Remnant. Sniper. Rally: If you control no other troops, gain Stealth` | **已修**（`AM`） | `Necron/3部队/Warpforge_21_Deathmark.png` |
| 16 | `SAU58` | Implacable Conqueror | `将你卡组中所有军队的费用降低2点。抽一张军队牌` | 我们 `desc`=PnP = **官方 `_new` 那代**；base=旧代（缺第二句） | `Lower the cost of all troops in your deck by 2. Draw a troop` | **已修**（`AD`） | `Necron/4计策/Warpforge_58_Implacable-Conqueror.png` |
| 17 | `SAU43` | Solar Pulse | `((Blind_all_enemies))。他们失去[[stealth]]和[[camouflage]]` | 两边同代（假阳：虚词 `and`；官方 `Blind all enemies.` 我们写成裸宏） | `Blind all enemies, and they lose Stealth and Camouflage` | 我们本来就对 | `Necron/4计策/Warpforge_55_Solar-Pulse.png` |
| 18 | `GOF84` | Runtherd | `[[mob]]：在你的手中创造一个随机的兽人野兽，其费用为5或更少` | 我们 `desc`=PnP；base=旧代（缺 `Ork`） | `Mob: Create a random Ork Beast in your hand that costs 5 or less` | **已修**（`AM`） | `Orks/3部队/Warpforge_09_Runtherd.png` |
| 19 | `GOF106` | Power of the Waaagh! | `你的战将获得“群体：直到你的下个回合获得 +1[攻击]”。` | 官方 base 是另一代（`this turn`）⇒ 假阳 | `Your Warlord gains "Mob: Gain +1 (粉拳) until your next turn"` | 我们本来就对 | `Orks/4计策/Warpforge_29_Power-of-the-Waaagh.png` |
| 20 | `GOF99` | Skrag Every Stash! | `对所有敌人造成 1 点伤害，并使他们失去[[stealth]]` | 我们 `desc`=PnP；base=旧代（整句效果完全不同） | `Deal 1 damage to all enemies, and they lose Stealth` | **已修**（`AM`） | `Orks/4计策/Warpforge_24_Skrag-Every-Stash.png` |
| 21 | `64a657fc` | Da Bigger Dey Iz... (Mozrog's Talent) | `临时。对一个敌人造成 1 点伤害。如果目标存活，本回合给予你的战将 +1 [攻击]和践踏。` | 官方 base 缺 `and Stomp` ⇒ 假阳 | `Ephemeral. Deal 1 damage to an enemy. If target survives, give +1 (粉拳) and Stomp to your Warlord this turn` | 我们本来就对 | `Orks/2天赋/Warpforge_02B_Da-Bigger-Dey-Iz.png` |
| 22 | `GOF13` | Deffgun Loota | `当另一支军队死亡时，对一个随机敌人造成 1-2 点伤害` | 我们 `desc`=PnP；base=旧代（`gain +1 Ranged`，效果完全不同） | `When another troop dies, deal 1-2 damage to a random enemy` | **已修**（`AM`） | `Orks/3部队/Warpforge_18_Deffgun-Loota.png` |
| 23 | `GOF18` | PainBoy Sniklaw | `集结：治疗一个友方单位 3 点生命值。天赋：锯骨大夫。` | 官方 base **只有 Talent 一句**⇒ 假阳 | `Rally: Heal 3 to a friendly unit. Talent: Sawbonez` | 我们本来就对 | `Orks/3部队/Warpforge_26_Painboy-Sniklaw.png` |
| 24 | `GOF61` | Rukkatrukk Squigbuggy | `[[armour]] 1。[[unstable]]。[[strike]]：((Deploy_x,$$GOF3$$))` | 我们 `desc`=PnP；base=旧代（`[[flank]]`=侧翼） | `Armour 1. Unstable. Strike: Deploy a Bomb Squig` | **已修**（`AM`） | `Orks/3部队/Warpforge_29_Rukkatrukk-Squigbuggy.png` |
| 25 | `GOF29` | Grukk Face-Rippa | `你的拥有潮涌的部队获得 +1 [攻击]。天赋：红色哇战吼。` | 官方 base **只有 Talent 一句**⇒ 假阳 | `Your troops with Tide have +1 (粉拳). Talent: Da Red Waaagh` | 我们本来就对 | `Orks/1督军/Warpforge_01_Grukk-Face-Rippa-2.png` |
| 26 | `SOR71` | Adelaide the Serene | `6 ☀：获得侧翼和护盾。<br>集结：若你的信仰少于 6 ☀，每有 1 个敌方单位便获得 1 ☀。` | 官方 base 多 `[[shield]]/[[flying]]` 的**声明句**（我们放 `keywords`）⇒ 假阳 | `Flying. 6☀ Gain Flank and Shield. Rally: If you have less than 6☀, gain 1☀ for each enemy unit` | 我们本来就对 | `Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png` |
| 27 | `SOR30` | Celestian Sacresant | `[[vanguard]]。[[armour]] 1。[[pray]]：((Heal_x_to_a_friendly_unit,3))` | 我们 `desc`=PnP = **官方 `_new` 那代**；base=旧代（`Pray: Gain Armour 1`） | `Vanguard. Armour 1 Pray: Heal 3 to a friendly unit` | **已修**（`AM`） | `Sorotitas/3部队/Warpforge_31_Celestian-Sacresant.png` |
| 28 | `SOR29` | Ephrael Stern | `[[camouflage]]。[[pray]]：((Deal_x_damage,2-3))。如果目标死亡，获得 1 ☀` | `descZh` = 官方 base **逐字**、与卡面一致 ⇒ 假阳（delta 来自**我们 `desc` 自己多写**） | `Camouflage. Pray: Deal 2-3 damage. If target dies, gain 1☀` | 我们本来就对（`descZh`）；🔴 我们的 `desc` 多 `to an enemy troop` ⇒ 另立一笔（`AJ` 已报，仍未收） | `Sorotitas/3部队/Warpforge_33_Ephrael-Stern.png` |
| 29 | `SOR25` | Imagifier | `祈祷：给予 1 个友方部队 +1 [攻击]、+1 [远程]和侧翼` | 官方 base 是旧代（`Give [[flank]] to another friendly troop`，**少两个属性**）；`_new` 才同代 ⇒ 假阳 | `Pray: Give +1 (粉拳), +1 (紫枪) and Flank to a friendly troop` | 我们本来就对 | `Sorotitas/3部队/Warpforge_29_Imagifier.png` |
| 30 | `SOR13` | Simulacrum Bearer | `给予你的部队 +1 [攻击]，本回合同样给予你的战将` | 官方 base 缺 `and to your Warlord this turn` ⇒ 假阳 | `Pray: Give +1 (粉拳) to your troops, and to your Warlord this turn` | 我们本来就对 | `Sorotitas/3部队/Warpforge_18_Simulacrum-Bearer.png` |
| 31 | `SOR24` | Simulacrum Celestian | `祈祷：给予所有友方单位 +1 [攻击]和 +1 [远程]。` | 我们 `desc`=PnP = **官方 `_new` 那代**（`to_all_friendly_units`）；base=旧代（`to_a_friendly_unit`） | `Pray: Give +1 (粉拳) and +1 (紫枪) to all friendly units` | **已修**（写手未署名，见 `AM` §1·3；本件复读卡面确认对） | `Sorotitas/3部队/Warpforge_30_Simulacrum-Celestian.png` |
| 32 | `SOR31` | Sister Dogmata | `[[rally]]：((Deal_1_damage))。如果目标死亡或为友方单位，获得 ((Faith_cost_x,1))` | 我们 `desc`=PnP；base=旧代（缺 `dies or` 半句） | `Rally: Deal 1 damage. If the target dies or is a friendly unit, gain 1☀` | **已修**（`AD`） | `Sorotitas/3部队/Warpforge_32_Dogmata.png` |
| 33 | `SOR4` | Sister Novitiate | `((Faith_cost_x,3)) 获得 +1 生命值。[[pray]]：((Deploy_x,$$SOR4$$))` | 我们 `desc`=PnP；base=旧代（**整条缺首句**） | `3☀: Gain +1 Health. Pray: Deploy a Sister Novitiate` | **已修**（`AM`） | `Sorotitas/3部队/Warpforge_07_Sister-Novitiate.png` |
| 34 | `SOR60` | Holy Fire | `本回合给予你的战将 +2 [攻击]，并给予相邻部队 +1 [攻击]。获得 1 ☀。` | 官方 base 少末句 `Gain 1 ☀` ⇒ 假阳 | `Give +2 (粉拳) to your Warlord and +1 (粉拳) to adjacent troops this turn. Gain 1☀` | 我们本来就对 | `Sorotitas/5防御卡/Warpforge_64_Holy-Fire.png` |
| 35 | `SOR39` | Prayer | `((Give_x_to_a_friendly_unit,[[shield]])). 如果该单位有[[pray]]，获得((Faith_cost_x,1))` | 我们 `desc` = **官方 `_new` 那代**（`has [[pray]]`）；base=旧代（`is Praying`）⇒ 代差但语义等同 | `Give Shield to a friendly unit. If the unit has Pray gain 1☀` | 我们本来就对 | `Sorotitas/4计策/Warpforge_48_Prayer.png` |
| 36 | `f104d4c2` | Righteous Repugnance | `本回合给予你的单位 +1 [攻击]和 +1 [远程]。4 能量：改为给予 +2 [攻击]和 +2 [远程]，并治疗 1 点生命` | 官方 base 缺 `4 Energy:` 整句、`_new` 仍缺 `and Heal them 1` ⇒ 假阳 | `Ephemeral. Give +1 (粉拳), +1 (紫枪) to your units this turn. 4☀: Give +2 and +2 instead and Heal them 1` | 我们本来就对 | `Sorotitas/2天赋/Warpforge_06_Righteous-Repugnance.png` |
| 37 | `TAU51` | Saviour Protocols | `在此次战斗剩余时间内，你的无人机的花费减少 1。选择一个无人机并加入你的手牌` | 我们 `desc`=PnP；base=旧代（缺第二句） | `Your Drones cost 1 less for the rest of this battle. Choose a Drone and add it to your hand` | **已修**（`AD`） | `Tau/4计策/Warpforge_63_Saviour-Protocols.png` |
| 38 | `TAU40` | Technological Supremacy | `((Give_x_health_to_a_friendly_unit,+3))。如果它是载具或战斗服，给予其 [[longrange]]` | 我们 `desc`=PnP；base=旧代（`heal it 3`） | `Give +3 Health to a friendly unit. If it's a Vehicle or Battlesuit, give it Long Range` | **已修**（`AM`） | `Tau/4计策/Warpforge_48_Technological-Supremacy.png` |
| 39 | `TAU3` | Commander O'Maisos | `[[shield]]。在你的回合内拥有[[flying]]。[[talent]]：$$c4f5c920…$$` | 我们 `desc`=PnP；base=旧代（只有 Talent 一句） | `Shield. Has Flying during your turn. Talent: Drone Companion` | **已修**（`AM`） | `Tau/1督军/Warpforge_4_Commander-OMaisos.png` |
| 40 | `TAU65` | War Shaper | `[[strike]]：((Heal_1))。[[talent]]：$$b3735ef3…$$` | 我们 `desc`=PnP = **官方 `_new` 那代**；base=旧代（只有 Talent 一句） | `Strike: Heals 1. Talent: Grisly Feast` | **已修**（`AM`） | `Tau/1督军/Warpforge_01_War-Shaper.png` |
| 41 | `TL75` | Termagant Brood | `潮涌 3。友方单位触发虫群时，获得 +1 [远程]` | 官方 base 是另一代（`deal 1 damage to a random enemy`）⇒ 假阳 | `Swarm. Tide 3. When a friendly unit triggers Swarm, gain +1 (紫枪)` | 我们本来就对 | `Tyranid/3部队/Warpforge_6_Termagant-Brood.png` |
| 42 | `TL24` | Harpy | `[[flying]]。当一个敌方单位攻击时，部署一个$$TL37$$` | 我们 `desc`=PnP = **官方 `_new` 那代**；base=旧代（**韩文**且效果不同） | `Flying. When an enemy attacks, deploy a Spore Mine` | **已修**（`AM`） | `Tyranid/3部队/Warpforge_36_Harpy.png` |
| 43 | `TL69` | Neurotyrant | `[[flying]]。[[synapse]]。当你使用一张非[[ephemeral]]计谋时，你的手中将生成一张**1费的**[[ephemeral]]复制品。` | 🔴 **反方向**：我们 `desc`=PnP 那代（**无 `1-Cost`**）；官方 base=旧代（**有 `a 1-Cost … copy`**）⇒ `descZh` 抄了旧代 | `Flying. Synapse. Whenever you play a non-Ephemeral Stratagem, create an Ephemeral copy of it in your hand`（卡面**没有** `1-Cost`；我们的 `desc` 逐字一致） | **我们错 → 应改成 `[[flying]]。[[synapse]]。当你使用一张非[[ephemeral]]计谋时，你的手中将生成一张[[ephemeral]]复制品。`**（删「1费的」） | `Tyranid/3部队/Warpforge_9_Neurotyrant.png` |
| 44 | `TL47` | Hive Fleet Leviathan | `本场战斗剩余时间内，你的军队的费用减少1。((Draw_a_troop))` | 官方 base 同代（第二句都是 `((Draw_a_troop))` 裸宏）⇒ 假阳（属渲染层欠账） | `Your troops cost 1 less for the rest of this battle. Draw a troop from your deck` | 我们本来就对 | `Tyranid/4计策/Warpforge_62_Hive-Fleet-Leviathan.png` |
| 45 | `UM22` | Inceptor Sergeant | `给予你的其他部队 +1 [远程]。` | 官方 base 缺 `other` ⇒ 假阳 | `Flying Flank. Codex: Give +1 (紫枪) to your other troops` | 我们本来就对 | `Ultramarines/3部队/inceptor srg.png` |
| 46 | `UM20` | Sergeant Telion | `[[camouflage]]。[[codex]]：降低你手中随机一张步兵的费用 1 点` | 我们 `desc`=PnP；base=旧代（**整句效果不同**：`Give [[sniper]]…`） | `Camouflage. Sniper. Codex: Lower the cost of a random Infantry in your hand by 1` | **已修**（`AM`） | `Ultramarines/3部队/telion.png` |
| 47 | `UM_Terminator` | Terminator | `誓言 1：获得护甲 1` | 🔴 **候选本身是键误配**：本卡**无官方条目**，脚本按英文名 `Terminator` 命中了**另一张卡 `EC25`** ⇒ delta `oath` 来自 `EC25`（判据空） | `Oath 1: Gain Armour 1` | 我们本来就对（delta 是**假阳**，见 §顺手发现1） | `Ultramarines/3部队/Warpforge_18_Terminator.png` |
| 48 | `UM44` | Armoured Support | `在这场**战斗**的剩余时间里，给你投入战场的载具 [[armour]] 1。` | 🔴 **反方向·词级**：我们 `desc`=PnP 那代（`the rest of the **match**`）；官方 base=旧代（`the rest of this **battle**`）⇒ `descZh` 抄了旧代那个词 | `For the rest of the match, give Armour 1 to Vehicles you put in play`（`desc` 逐字一致；**只有 `match` 这处**与我们的中文对不上） | **我们错 → 应改成 `在这场比赛的剩余时间里，给你投入战场的载具 [[armour]] 1。`**（判据：官方表对 `the match` 的既定译法 = 「比赛」——`Card_Description/ASH59`/`AM65` 两条现读都是「比赛开始…」；⛔ 不是自创词） | `Ultramarines/4计策/Warpforge_59_Armoured-Support.png` |
| 49 | `UM40` | Avenging Zeal | `给予你场上和手牌中的所有单位 +2 [攻击]和 +2 [远程]。当 1 个友方部队或战术触发[典籍]典籍时，此卡费用 -1。` | 官方 base 是另一代（`when **you** trigger [[codex]]`）⇒ 假阳 | `Give +2 (粉拳) and +2 (紫枪) to all your units in play and in hand. Lower cost by 1 when a friendly Troop or Stratagem trigger Codex` | 我们本来就对 | `Ultramarines/4计策/zeal.png` |
| 50 | `UM62` | Battlefield Supremacy | `将你手中和卡组中所有载具的费用降低 1 点。((Draw_a_Vehicle))` | 官方 base 同代（第二句都是 `((Draw_a_Vehicle))` 裸宏）⇒ 假阳 | `Lower the cost of all Vehicles in your hand and deck by 1. Draw a Vehicle from your deck` | 我们本来就对 | `Ultramarines/4计策/supremacy.png` |
| 51 | `UM73` | Oath of Moment | `给予友方**单位**+2生命值和''[[strike]]：触发此**单位**的[[codex]]能力''` | 🔴 **反方向·术语**：我们 `desc`=PnP 那代（`a friendly **troop**` / `this **troop's**`）；官方 base=旧代（`unit`）⇒ `descZh` 抄了旧代的 `unit`→「单位」 | `Give +2 Health and "Strike: Trigger this troop's Codex ability" to a friendly troop` | **我们错 → 应改成 `给予友方**部队**+2生命值和''[[strike]]：触发此**部队**的[[codex]]能力''`**（判据：卡面 `troop`；且**池内自己**的约定就是 `troop`→「部队」（同批 `SOR25`/`SOR13`/`UM22`/`GOF29` 都写「部队」）⇒ 中文写「单位」会让玩家以为**督军也算**） | `Ultramarines/4计策/oath of moment.png` |
| 52 | `UM57` | Primaris Invader | `典籍：本回合给予你所有载具 +1 [远程]。` | 我们 `desc`=PnP = **官方 `_new` 那代**（`this turn`）；base=旧代 ⇒ 假阳 | `Codex: Give +1 Ranged Attack to all your Vehicles this turn` | 我们本来就对 | `Ultramarines/3部队/invader.png` |

---

## 三、汇总

### 我们错：**4 条**（本次新裁，全部**尚未施工**）

| # | 卡 | 现在（`descZh` 现池值，= 官方 base 那代） | 建议改成 | 判据一句话 |
|---|---|---|---|---|
| 1 | `UM44` Armoured Support | `在这场**战斗**的剩余时间里，给你投入战场的载具 [[armour]] 1。` | `在这场比赛的剩余时间里，给你投入战场的载具 [[armour]] 1。` | 卡面 `the rest of the **match**`；官方表把 `the match` 译「比赛」（`ASH59`/`AM65`） |
| 2 | `af98d423` Legendary Commandant | `[[ephemeral]]。选择一张**非传奇**星界军计谋并加入你的手牌` | `[[ephemeral]]。选择一张星界军计谋并加入你的手牌` | 卡面 `Choose an Astra Militarum Stratagem…`，**无** `non-Legendary`；我们的 `desc` 与卡面逐字一致 |
| 3 | `TL69` Neurotyrant | `…你的手中将生成一张**1费的**[[ephemeral]]复制品。` | `…你的手中将生成一张[[ephemeral]]复制品。` | 卡面 `create an Ephemeral copy of it in your hand`，**无** `1-Cost`；我们的 `desc` 与卡面逐字一致 |
| 4 | `UM73` Oath of Moment | `给予友方**单位**+2生命值和''[[strike]]：触发此**单位**的[[codex]]能力''` | `给予友方**部队**+2生命值和''[[strike]]：触发此**部队**的[[codex]]能力''` | 卡面两处都是 `troop`；池内对 `troop` 的既有约定是「部队」 |

> 🔴 **这 4 张共同点**：都是「**`descZh` = 官方 base 的中文逐字**」而 `desc` = 卡面那代 ⇒ **中文留着旧代才有/旧代写法的词**。
> 落点 = `数据/游戏数据/cardface_fixes.json` 的 **`descZh` 列**（人工、最高序）；现读 **4 张都不在那个 274 条的人工层里** ⇒ 与 `AD`/`AM` 同一通道、**⛔ 不用动 `desc`**（`desc` 已与卡面逐字一致）。
> ⚠️ 4 张**都没有**官方 `<键>_new` 键（现读）⇒ **无现成中文可抄**，只能按卡面删词（`af98d423`/`TL69`/`UM73`）/ 换词（`UM44`）。

### `AJ`/`AK` 判「假阳」而本件判「真缺陷」的 2 条（分歧留痕）

- `af98d423`：`AK` 原文写「假阳·其它（冠词 `an`）。⚠️`descZh` 反而**多**「非传奇」卡面没有」—— **事实它记对了，但归类成了假阳**。本件按「`descZh` 必须与卡面同代」判成**真缺陷**。
- `TL69`：`AK` 原文写「假阳·其它（冠词 `an`；`Whenever`↔`When`）。⚠️`descZh` 反而多「1费的」卡面没有」—— 同上。
  ⇒ 两条**不是推翻它的读数**（它明确写了卡面没有），是**裁定尺度不同**：它把「delta 是虚词」当假阳，本件把「`descZh` 多出卡面没有的限定词」当缺陷。**请主对话裁尺度**。

### 我们本来就对：**24 条**

`ASH12` · `ASH34` · `851f73da`(Storm of Silence) · `ASH35` · `GSC28` · `SAU43` · `GOF106` · `64a657fc`(Da Bigger Dey Iz) · `GOF18` · `GOF29` · `SOR71` · `SOR29` · `SOR25` · `SOR13` · `SOR60` · `SOR39` · `f104d4c2`(Righteous Repugnance) · `TL75` · `TL47` · `UM22` · `UM_Terminator` · `UM40` · `UM62` · `UM57`
（24 张**全部亲开图**核过：`descZh` 的中文与卡面逐字/逐义对上；delta 全部来自「官方 base 是旧代」或「虚词/词形/裸宏未展开」。其中 `UM_Terminator` 的 delta 来自**键误配**。）

### 判不了：**0 条**

### 已修（本件复读卡面确认改对）：**24 条**

`ASH70` · `ASH48` · `ASH52` · `AM72` · `AM76` · `6ed51d59`(Duelist's Hubris) · `GSC29` · `SAU70` · `SAU15` · `SAU58` · `GOF84` · `GOF99` · `GOF13` · `GOF61` · `SOR30` · `SOR24` · `SOR31` · `SOR4` · `TAU51` · `TAU40` · `TAU3` · `TAU65` · `TL24` · `UM20`
（= `AD` 4 · `AM` 19 · `SOR24` 1，写手未署名；**24 + 24 + 4 = 52** ✅ 一条不漏）

---

## 四、没查清 / 判不了的

**0 · 🔴 先把「少列的那一条」交代清楚（本件最重要的发现之一）**
`AD` §3·3 自己记着「正文写『另 21 条』、**实测列了 24 条**、分档表写 **1 词×13 + 2 词×12 = 25**」⇒ **24 ≠ 25，缺 1 条**，当时没认出是哪条。
本件**重跑判据②脚本**拿到全量 52 条，与 `AD` 实列的 51 条求差 ⇒ **缺的那一条 = `UM44 Armoured Support`**（delta = `match the`，正是 2 词档、正好补满 12）。
⇒ **`UM44` 是这 52 条里唯一一条从未被任何一笔（`AD`/`AJ`/`AK`/`AM`）开图裁过的**。本件已开图并裁定（见 #48）。
（`SOR31` 在 `AK` 表里标的是「名单里第 1 条、★未开图、`AD` 已修」⇒ 它与 `UM44` 不是同一条。）

**1 · `UM44` 的「战斗 → 比赛」是词级差异，尺度请裁**
判据是硬的（卡面 = `the match`；官方表对 `the match` 的既定中文 = 「比赛」），但**池内只有这一张** `desc` 含 `match`（现扫 1127 张，**仅 `UM44`**）⇒ 没有池内先例。
本件按「官方表惯例」给「比赛」。若主对话认为中文侧应统一用「对局」之类，请改口径 —— **本件没自创词**。

**2 · `UM73` 的「单位 vs 部队」不是孤例，池内**21 张**同族**
现扫「`desc` 含 `troop` 而 `descZh` 含『单位』」= **21 张**（`UM73` 是其中之一；全列见 §顺手发现2）。
本件**只对 `UM73` 开图核过**（卡面两处都是 `troop`）⇒ 其余 20 张**我没有逐张开图**，**不能**说它们都一样错（有些可能卡面本来写 `unit`，是我们 `desc` 写错）。
⇒ 建议**单开一笔**：逐张开图 → 定一份「`troop`→部队 / `unit`→单位」的池内口径清单。⛔ 只改 `UM73` 一张会让池内更不一致。

**3 · 本批 4 条新缺陷的**渲染**没验**：⛔ 本件不许跑 Unity ⇒ 「改完之后卡面上那半句长什么样」只有真机 / `RuleEngineTest` 能验。
另：`SAU43`/`TL47`/`UM62`/`SOR39` 等**裸宏**（`((Blind_all_enemies))` / `((Draw_a_troop))` / `((Give_x_to_a_friendly_unit,[[shield]]))`）**会原样印在卡面上**（`A1982` 已立账的渲染层缺口）——**本件不重复立账**。

**4 · 官方 `_new` 键这条线**：本批 52 条里只有 `ASH48`/`SAU15`/`SAU58`/`SOR30`/`SOR39`/`SOR25`/`TL24`/`UM57`/`f104d4c2` 有 `_new`（其中 `SOR30`/`SOR24` 的 `_new` **zh 列是空的**）⇒ 「抄 `_new` 的中文列」这条捷径**本批对 4 条新缺陷一条都用不上**。

---

## 顺手发现（只报，⛔ 一条都没改；请主对话分流）

1. 🔴 **判据②有一个「键误配」假阳类：`UM_Terminator`** —— 它是**自造卡**（全池 23 张之一，**官方表里没有 `Card_Description/UM_Terminator`**），而脚本的 `off_name_en` 按**英文名**兜底，把 `Terminator` 命中了 **`Card_Description/EC25`**（另一个阵营的同名卡）⇒ 那行 delta `oath` **来自 `EC25`，与本卡无关**。
   **现扫**：52 条里 `key != id` 的**只有它一张** ⇒ 这一类假阳在本批**只有 1 条**，但**手段本身**会在「自造卡 / 同名卡」上继续误报 ⇒ 建议在判据②里加一句「`key != id` 且本卡无官方条目 ⇒ 标为判据空」。
2. ⚠️ **池内 `troop` → 「单位」共 21 张**（现扫；`desc` 含 `troop` 而 `descZh` 含「单位」）：
   `ASH85` · `ASH73` · `AM10` · `AM47` · `EC2` · `EC1` · `d01a5ba5…`(Inscrutable Cunning) · `SAU7` · `SAU56` · `GOF104` · `SW29` · `SW26` · `SW65` · `9f75a6fe…`(Hero of the Empire) · `TL11` · `UM15` · `UM87` · `UM77` · `UM40` · `UM95` · `UM73`
   其中 `UM73` 本件**已开图确认**卡面写 `troop`（⇒ 真缺陷）。其余 20 张**未开图**（见 §四·2）。
   ⚠️ 注意 `UM40` **也在名单里但本件判它「本来就对」**（它的「单位」指的是 `desc` 里 `all your units`——卡面确实写 `units`，那处**没错**；「部队」那处是 `a friendly Troop`，也对）⇒ **这条名单会误抓**，必须逐条看 `desc` 的**哪一处**是 troop。
3. ⚠️ **`non-Legendary` 的中文在池内有 3 种写法**：**非传说**（`DA77`/`UM_Suppressor`/`UM97`）· **非传奇**（`fd67b475…`/`af98d423`）⇒ 与「Warlord→战将」那次同类，**该统一**（现扫各 3 / 2 处）。
4. ⚠️ **`UM44` 是池内唯一 `desc` 含 `match` 的卡**（现扫 1127 张只有它 1 张）⇒ 中文侧该怎么写**没有池内先例**（见 §四·1）。
5. ℹ️ **官方 base 的 EN 里 `[[…]]` 声明句常被我们搬进 `keywords` 列** —— 本批 52 条里属这一类的（`ASH12`/`ASH34`/`ASH35`/`GSC28`/`SOR71`/`UM22`/`UM20`/`TL75`…）**都不是缺陷**（简报已把它判掉）；本件逐条核过 `keywords` 列，**全部能覆盖**（例：`ASH35` 的 `keywords=['Armour 2','Shuriken 8']`、`TL75` 的 `['Swarm','Tide 3']`）。
6. ℹ️ `AJ` 报的两条**仍开着**的账（本件**没碰**，因为都不属本主题）：
   ① **`SOR29` 的 `desc` 多 `to an enemy troop`**（卡面/官方都没有；`card_stats.json` 同条也没有 ⇒ 我们自己两处不一致）；
   ② **`TAU3` 的 `desc` 把关键词句抄进去了**（`Shield. Has Flying during your turn.`，而 `keywords` 列也有 `Shield`/`Talent: …`）⇒ **卡面会印两遍**。
7. ℹ️ **本件 52 张全部亲开图**，用具名路径列在 §二 ⇒ 下次要复核不用再按名搜（其中 5 张**无编号前缀**：`Ultramarines/3部队/{telion,inceptor srg,invader,Warpforge_18_Terminator}.png` · `Ultramarines/4计策/{zeal,supremacy,oath of moment}.png` —— 按卡名 glob **会漏**，必须列目录）。
