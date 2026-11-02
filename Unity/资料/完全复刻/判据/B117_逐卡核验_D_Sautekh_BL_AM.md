# B117 逐卡核验 D（Sautekh · BlackLegion · AstraMilitarum）

范围：`全卡核验_判定.tsv` 里本组「引擎判定 = 未验到」的 **91 张**（AM 27 · BL 31 · SAU 33）。
判法：对每张卡的每个效果节点（trigger / chosenAbility / buffType）回源找**引擎实现分支**，给 `文件:行号`。
⚠️ 前提（本报告的核心发现）：**我们的引擎是「卡面英文文本驱动」**（`EffectText.Parse` → `EffectOp` → `EffectDispatch`），
数据面的 `abilityTriggers` / `chosenAbility` 只在**正文解析为空时**当兜底（`ER:21381 FireTriggerRowsByChosenAbility`，且只认 `320`）。
⇒ S3 表里那些「未验到」绝大多数是**夹具测不到**（电池不触发该事件 / 夹具没铺坟场、没弹窗 / 牌库只读 id），**不是引擎没分支**。

## 一、结论速览

本组 91 张：**✅能执行 90 · 🟠真缺口 1 · ❓查不到 0**

按缺口类型聚类（每类一行）：
- 🟠 **trait 没进 `keywords` 列** → `SAU7 Canoptek Wraith` 的 `Unstoppable` 只在 `cardTraits`，而 `Has()` 只读 `keywords`（`CD:839` 构造参数 / `CardDatabase.cs:1549` 传 `c.keywords`）。
- ✅ **纯关键词卡 21 张**（无 ability 行）：vanguard / armour / blast / flank / flying / stealth / remnant / regeneration / destroyer / longrange 全有行为分支（`RC:5711` 飞行、`RC:7224` blast、`RC:2649` regeneration、`RC:3567` flank、`RC:5679` destroyer、`CounterAttack.cs:60` longrange、`RC:10020` remnant）。
- ✅ **trigger 510 一族 6 张**：`BroadcastWhen(GetsDarkPact)` `ER:12674` → `FireBoardWhenListener` → `FireTriggerByNumber(510)`；名字映射 `ER:7646/7819/7913`。
- ✅ **trigger 570 一族 6 张**：`BroadcastWhen(Reanimated)` `ER:15848`；映射 `ER:7645/7820`。
- ✅ **其余 trigger**：20 `ER:8311` · 41 `ER:8796` · 90/80 `ER:8179` · 120 `RC:6396` · 180 `ER:8174` · 190 `ER:7640` · 240 `ER:19739` · 280 `RC:8224/8249` · 301 `ER:10219` · 410 `RC:13036` · 500 `Enchantment.cs:1324` · 560 `ER:2855` · 580 `RC:7058` · 661 `RC:12976`。
- ✅ **buffType 各族**：10 deal `ER:1064` · 20 heal `ER:1073` · 30/31/40/50 give `ER:1122` · 0 draw `ER:1074` · 70 deploy `ER:1141` · 100 create `ER:1140` · 110 destroy `ER:1087` · 130 stun `ER:1088` · 140 return `ER:1086` · 430 reanimate `ER:1144` · 475 reloadduty `ER:1121` · 250/320 走正文 / `RC:17474` 那道闸。

> 文件别名：`ER`=RuleEngine/Core/EffectResolver.cs · `RC`=RuleEngine/Core/RuleCore.cs · `ET`=RuleEngine/Core/EffectText.cs ·
> `CD`=RuleEngine/Core/CardDef.cs · `AU`=RuleEngine/Core/Aura.cs · `GP`=RuleEngine/Core/GivePayload.cs · `EN`=RuleEngine/Core/Enchantment.cs

## 二、逐卡

| 卡id | 卡名 | 判定 | trigger | ta | buffType | 证据（文件:行号） | 备注 |
|---|---|---|---|---|---|---|---|
| AM15 | Minka Lesk | ✅能执行 | 46/240 | 100/30 | create/2 | ER:1140 · AU:273 | 光环「Friendly Infantry costs 1 less」走 AuraSpec；Talent 走 create |
| AM62 | Munitorum Supplies | ✅能执行 | 0 | 100/30 | create/buff | ER:1133 · ER:1142 | choosecard + 尾句 lowercost |
| AM24 | Bullgryn Bone'ead | ✅能执行 | 无 | 无 | 无 | CD:8330 · RC:7224 | 纯关键词（Vanguard/Armour 1/Blast 2） |
| AM31 | Cadian Castellan | ✅能执行 | 301 | 30 | 2 | ER:10219 · ER:1142 | Regiment 广播 + lowercost |
| AM8 | Cadian Medic | ✅能执行 | 301/410 | 320/20 | heal | ER:1073 · RC:13036 | 410 正文跳已接 |
| AM17 | Commissar | ✅能执行 | 661 | 30 | 2 | RC:12976 · WE:1379 | 「triggers Duty」事件短语 ET/WhenEvent 已收，卡名被点名 |
| AM4 | Faeburn Vanquisher | ✅能执行 | 0 | 30/0 | buff | ER:1133 · ER:1142 | 手牌选牌降落费 |
| AM9 | Kasrkin | ✅能执行 | 301/180 | 10/0 | damage/draw | ER:1064 · ER:10219 · ER:1074 | 180 只发信号，正文 `targetdies` 承担效果（ER:8174） |
| AM13 | Ogryn | ✅能执行 | 无 | 无 | 无 | CD:8330 | 纯 Vanguard |
| AM72 | Vitus Gryf | ✅能执行 | 20/120 | 31/70 | buff/deploy | AU:273 · RC:7058 · ER:1141 | 光环给 Flank；Strike 部署 |
| AM52 | Armoured Squadron | ✅能执行 | 0 | 100/30 | create/buff | ER:1133 · ER:1140 | 载具选牌进手 + 降 2 费 |
| AM54 | At All Costs | ✅能执行 | 0/410 | 0/380/320 | draw/buff | ET:11011 · ET:8193 · RC:17474 | `warlordlowhp` 条件有专门判据 |
| AM39 | Born Soldier | ✅能执行 | 0/90 | 10/0 | damage/draw | ER:1064 · ER:1074 | 后置条件 targetdies |
| AM51 | Cadia Stands | ✅能执行 | 0 | 30 | buff | ET:5471 · ER:8796 | TryPersistent + TurnStart 常驻 |
| 1ff4815a… | Efficiency and Excellence | ✅能执行 | 0 | 10/0 | damage/draw | ER:1064 · ER:1074 | Ephemeral 不影响结算 |
| AM40 | Full Throttle | ✅能执行 | 0 | 50 | buff | ET:447 · ET:8290 | 「next Vehicle you play」有专条判据 |
| AM55 | Mechanised Infantry | ✅能执行 | 0/190 | 40/70 | buff/deploy | ER:1141 · ET:5471 | 部署 ×3 + 常驻降费 |
| AM47 | Press the Attack | ✅能执行 | 0 | 0/475 | draw/… | ER:1121 · ER:1074 | reloadduty 动词 + DoReloadDuty ER:15492 |
| AM43 | Recon Operation | ✅能执行 | 0 | 30/0 | buff/draw | ET:4536 · ET:11046 | lose Stealth/Camouflage + `no enemy` 条件 |
| AM58 | Siege Warfare | ✅能执行 | 0/410 | 0/250/320 | draw/auto | ET:8990 · 对拍 board_add | S3 自身已验 AM58 的 250 行「对」 |
| AM75 | Valkyrie Drop | ✅能执行 | 0 | 10/70 | damage/summon | ER:1064 · ER:1141 | 后置条件部署 |
| AM56 | Vengeance for Cadia | ✅能执行 | 0/90 | 30/40 | buff | ER:1122 · ET:5471 | 单位加属性 + 常驻降费 |
| AM33 | Basilisk | ✅能执行 | 无 | 无 | 无 | CD:7308 · RC:7224 | 纯关键词（Long Range/Blast 3） |
| AM29 | Hydra | ✅能执行 | 301 | 10 | damage | ER:10219 · ER:1064 | 随机飞行目标 |
| AM26 | Taurox | ✅能执行 | 301 | 100 | create | ER:1140 · ER:10219 | 造牌进手 |
| AM34 | Valkyrie | ✅能执行 | 180 | 10/70 | damage/summon | ER:8174 · ER:1141 | 180 只发信号；「每死一个部署」由正文后置条件承担 |
| AM66 | Hektor Thenmann | ✅能执行 | 46 | 10/130 | damage/stun | ER:1064 · ER:1088 | 伤害 + 晕 |
| BL12 | Aspiring Champion | ✅能执行 | 510 | 40 | 1 | ER:12674 · ER:1122 | 510 广播在 ApplyMarkOfChaos 尾巴 |
| BL2 | Black Legionary | ✅能执行 | 无 | 无 | 无 | — | 卡面 desc 为空、无效果节点（无可执行项） |
| BL3 | Chaos Legionary | ✅能执行 | 510 | 10 | damage | ER:12674 · ER:1064 | |
| BL1 | Chaos Spawn | ✅能执行 | 无 | 无 | 无 | RC:3567 | 纯 Flank |
| BL19 | Chaos Terminator | ✅能执行 | 无 | 无 | 无 | CD:8330 | 纯 Vanguard |
| BL63 | Havoc | ✅能执行 | 510 | 10 | damage | ER:12674 · ER:1064 | |
| BL5 | Hound of Abaddon | ✅能执行 | 无 | 无 | 无 | CD:8330 | 纯 Vanguard |
| BL32 | Iskandar Khayon | ✅能执行 | 560 | 100/30 | create/buff | ER:2855 · ER:1140 | Artifice 侧触发 FireTriggerOnSide |
| BL7 | Khorne Berzerker | ✅能执行 | 90 | 40 | darkpact | GP:740 · ER:8179 | `gain a Dark Pact of X` 有 payload 解析 |
| BL69 | Meltagun Legionary | ✅能执行 | 510 | 10 | damage | ER:12674 · ER:1064 | |
| BL26 | Obliterator | ✅能执行 | 无 | 无 | 无 | — | 卡面 desc 为空、无效果节点 |
| BL9 | Possessed Marine | ✅能执行 | 无 | 无 | 无 | RC:3567 | 纯 Flank |
| BL29 | Terminator Champion | ✅能执行 | 无 | 无 | 无 | CD:1356 | 纯 Armour 1 |
| BL11 | Veteran Legionary | ✅能执行 | 无 | 无 | 无 | CD:8330 | 纯 Vanguard |
| BL17 | Warp Talon | ✅能执行 | 无 | 无 | 无 | RC:5711 · ConditionEval:340 | Flying / Stealth |
| 36892cd9… | Bitter Blows | ✅能执行 | 0 | 110/30 | destroy/buff | ER:1087 | 「lowest Health」目标筛选走 TargetCriteriaEval |
| BL55 | Black Crusade | ✅能执行 | 0 | 0/250 | draw/auto | ET:8990 | `Deploy 4 random troops from your deck` 有专条 |
| BL50 | Execution | ✅能执行 | 0 | 110 | destroy | ER:1087 | S3 里 playRc=18 ErrNoTargetAvailable = 夹具无「受伤的敌方部队」，不是缺口 |
| BL75 | Idolatrous Despoilers | ✅能执行 | 0 | 30 | buff | ET:1040 · ET:5471 | 常驻 + `Deployed` 只对刚部署那个生效 |
| 3f38d2e2… | Rites of Possession | ✅能执行 | 0/90 | 10/70 | damage/summon | ER:1064 · ER:1141 | 2-4 区间伤害 + 后置部署 |
| BL48 | Skeins of Fate | ✅能执行 | 0 | 30/0 | buff/draw | ET:5245 · ER:1074 | `for each Dark Pact` 的 CountScope 在 ET:5243-5245 |
| BL56 | Throne of the Heretic | ✅能执行 | 0 | 30/100 | buff/create | ER:1133 · ER:1142 | |
| BL52 | Unholy Smite | ✅能执行 | 0/240 | 10/30 | damage/buff | ER:1064 · ER:19739 | 240 快照刷新通道 |
| BL72 | Accursed Helbrute | ✅能执行 | 510 | 40 | 8 | GP:533 · ER:12674 | 「gain it as well」= `CopyEventPact` 专条 |
| BL28 | Helbrute | ✅能执行 | 510 | 10 | damage | ER:12674 · ER:1064 | |
| BL31 | Maulerfiend | ✅能执行 | 580 | 10 | damage | RC:7058 · ET:5245 | Strike 触发 + Dark Pact 计数 |
| BL27 | Venomcrawler | ✅能执行 | 20 | 50 | buff | AU:172 · ER:8311 | Aura.cs 注释直接点名 BL27 |
| 4a4857… | Dark Pact of Fate | ✅能执行 | 500 | 40 | 1/3 | EN:1324 · ER:1122 | 500 附魔触发跳 |
| 9a2cad… | Dark Pact of Blood | ✅能执行 | 500 | 40 | 1/3 | EN:1324 · ER:1122 | |
| b8e81b… | Dark Pact of Excess | ✅能执行 | 500 | 40 | 1 | EN:1324 · ER:1122 | |
| 1dfc9b… | Dark Pact of Resilience | ✅能执行 | 500 | 40 | 1/3 | EN:1324 · ER:1122 | |
| SAU2 | Canoptek Plasmacyte | ✅能执行 | 190 | 30 | 3 | ER:7640 · ER:1122 | 部署事件 → 给 Regeneration |
| SAU7 | Canoptek Wraith | 🟠真缺口 | 无 | 无 | 无 | CD:839+CardDatabase.cs:1549 | 见 §三 |
| SAU20 | Chronomancer | ✅能执行 | 41/46 | 430/100 | reanimate/create | ER:8796 · ER:1144 | TurnBeforeEnd 常驻通道 |
| SAU14 | Cryptothrall | ✅能执行 | 无 | 无 | 无 | RC:10020 · CD:8330 | Remnant + Vanguard |
| SAU3 | Flayed One | ✅能执行 | 570 | 40 | fast | ER:15848 · ER:1122 | |
| SAU8 | Gauss Immortal | ✅能执行 | 无 | 无 | 无 | RC:10020 · RC:2649 | Remnant + Regeneration 2 |
| SAU4 | Gauss Warrior | ✅能执行 | 570 | 40 | 1 | ER:15848 · ER:1122 | |
| SAU71 | Immortals Phalanx | ✅能执行 | 570 | 70 | deploy | ER:3962 · ER:15848 | 「deploy a copy of this troop」有专条（注释里误写 SAU72） |
| SAU22 | Lokhust Heavy Destroyer | ✅能执行 | 570 | 10 | damage | ER:15848 · ER:1064 | |
| SAU72 | Lokhust Lord | ✅能执行 | 80 | 100 | create | ER:8179 · ER:1140 | Backlash 关键词 + 造牌 |
| SAU23 | Lychguard | ✅能执行 | 无 | 无 | 无 | CD:1356 · RC:10020 | Remnant/Armour 2/Vanguard |
| SAU5 | Necron Warrior | ✅能执行 | 无 | 无 | 无 | RC:10020 · CD:8330 | |
| SAU27 | Scarab Swarm | ✅能执行 | 240 | 40 | 70 | AU:656 · ER:19739 | MeleeEqualsHealth 唯一生产者就是它 |
| SAU18 | Skorpekh Destroyer | ✅能执行 | 无 | 无 | 无 | RC:5679 · RC:3567 | |
| SAU11 | Technomancer | ✅能执行 | 560/410 | 320/430 | reanimate | ER:2855 · ER:1144 · RC:13036 | |
| SAU10 | Triarch Praetorian | ✅能执行 | 无 | 无 | 无 | RC:10020 · CD:8330 | |
| SAU33 | Vargard Obyron | ✅能执行 | 280 | 236 | — | RC:6242 · RC:17584 | `Bodyguard` 文本标记 + TryRedirectAttackToBodyguard |
| SAU47 | Curse of the Phaeron | ✅能执行 | 0 | 3/30 | buff | ER:1122 | ca=3 是 vfx 行，真效果在 30 那一行 |
| SAU55 | Immortal Pride | ✅能执行 | 0 | 3/30/50 | buff | ER:1122 · ET:5471 | 全体加甲 + 常驻 Talent 降费 |
| SAU58 | Implacable Conqueror | ✅能执行 | 0 | 0/30 | draw/buff | ER:1074 · ER:1142 | |
| 3000716… | Lord of the Storm | ✅能执行 | 0/90 | 10/20/100 | damage/heal/create | ER:1064·1073·1140 | |
| cbabeb23… | Master Chronomancer | ✅能执行 | 0 | 3/420 | vfx/decktop | ET:6640 · ER:5107 | choosecard 的 `decktop` 动作 |
| SAU40 | Protection Protocol | ✅能执行 | 0 | 30/430 | buff/reanimate | ER:1122 · ER:1144 | 「or」二选一走 AltHolds |
| SAU39 | Reconstitution Protocol | ✅能执行 | 0 | 140 | return | ET:6579 · ER:1133 | 选牌来源 = `died this game` ⇒ srcKind=dead（ET:6579） |
| SAU61 | Resurrection Vault | ✅能执行 | 0/410 | 320/430/30 | reanimate/buff | ER:1144 · RC:13036 | |
| SAU52 | Self-Destruction | ✅能执行 | 0 | 260 | destroy+deal | ER:1087 · ET:5292 | 「for each one destroyed」有专条 |
| SAU46 | Spyder Nest | ✅能执行 | 0 | 3/70 | vfx/deploy | ER:1141 | |
| SAU57 | Tomb World | ✅能执行 | 0 | 70 | summon | ET:9072 · ER:1141 | `that died this game` 部署源 |
| SAU21 | Doom Scythe | ✅能执行 | 无 | 无 | 无 | RC:5711 · RC:3567 | |
| SAU12 | Tomb Blade | ✅能执行 | 无 | 无 | 无 | RC:10020·5711·3567 | |
| SAU37 | Orikan the Diviner | ✅能执行 | 46/570 | 10/20/100 | damage/heal/create | ER:15848 · ER:1073 | |
| SAU66 | Ramatekh the Cruel | ✅能执行 | 20/46 | 30/10/100 | buff/damage | AU:273 · ER:8311 | 「Your other Destroyers have +1」= 光环 |
| SAU59 | Awakened Obelisk | ✅能执行 | 0 | 10/100 | damage/create | ER:1064 · ER:1140 | 后置 `targetdies` 造牌 |

## 三、真缺口清单（🟠）

1. **SAU7 Canoptek Wraith — `Unstoppable` 没进 `keywords` 列 ⇒ 那句「ignore Vanguard when attacking」不生效**
   - 卡面（`cards_engine.json`）：`This troop can ignore enemy units with Vanguard when attacking`
   - 数据：`keywords = ["Remnant"]`，`cardTraits = ["Unstoppable","Remnant"]`（**全池唯一**一张带 Unstoppable，现扫 1,129 张）
   - 引擎：`keywords` 是 `_keywords` 的唯一来源（`CD:839` 构造形参 `IEnumerable<string> keywords`；`CardDatabase.cs:1549` 传 `c.keywords`）。`cardTraits` 只喂 `CountTraits/HasDefaultTrait/…`（`CardDatabase.cs:1721-1731` 明写「本列不喂引擎」）。
   - 后果：`ConditionEval.cs:242` 的判据是 `u.Has(RuleCore.Unstoppable) || u.Has(KeywordTable.Flying)` ⇒ 恒 false ⇒ 该句无效。
   - 另一条路也走不通：`ET:8783` 明写这句**被 WhenSuffix 的第三道闸挡下**（尾巴 `attacking` 不是事件短语）。
   - 该改哪：`工具/gen_cards_engine.py` 的 `keywords` 生成口径（把 `cardTraits` 里的 Unstoppable 并进 keywords），**不是**引擎。

## 四、查不到的（❓）

无。

## 五、顺手发现

1. **卡名两源不一致（数据层）**：`cards_truth.json` 与 `全卡核验_判定.tsv`/`cards_engine.json` 的卡名不同 ——
   `AM66` 真值是 `Tempestor Prime`（TSV 写 `Hektor Thenmann`）；`1dfc9bc7…` 真值是 `Mark of Nurgle (enchantment)`（TSV 写 `Dark Pact of Resilience`）；
   `4a48457c…` = `Mark of Tzeentch`（TSV `Dark Pact of Fate`）。⛔ 按卡名 join 两个数据源会错配。
2. **`EffectResolver.cs:3963` 的注释把卡号写错**：写的是 `SAU72 Immortals Phalanx`，实际 `SAU72` = Lokhust Lord、`Immortals Phalanx` = **SAU71**。
3. **`trigger 180` 是「只发信号、不结算」**（`ER:8173-8176` 自述）。所以数据面 `trigger 180 + chosenAbility 70/10` 的行
   **不会**被结构化派发执行 —— 有 4 张（`AM9`/`AM34`/`SOR29`/`UM99`）靠**正文后置条件 `targetdies`** 承担效果。
   ⇒ 若将来有卡「只有 180 数据行、卡面没写条件句」，就会静默不结算。今天池内 0 张。
4. **S3 的「未验到」不等于「引擎没实现」**：128 条本组未验到行里，闸是「电池不触发该事件」「夹具结构上不适用（ephemeral/defence）」
   「ta=65 牌库只读 id」「需要玩家在弹窗里选」这类**测量口径**问题的占绝大多数。⇒ 下次核验建议直接把这类闸计入「无法由此电池判定」，
   别记成「未验到」（否则会被后续会话当缺口重做 —— 与 CLAUDE.md「重复劳动」那条同源）。
5. **`SAU33` 的 236 分派是「信号 + 改目标」两半分开的**：`RC:17584` 只补发 `EvtKind.Trigger` + 日志，
   真正的改向在 `RC:6242 TryRedirectAttackToBodyguard`（读 `Bodyguard` 文本标记）。⇒ 别以为「236 没进 `RC:17474` 那道闸」就等于没做。
