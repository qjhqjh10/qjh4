# 卡牌数据库合并报告

生成时间: 2026-08-16 (merge_card_stats.py)

## 概要

- index 卡总数: 1193
- 有 OCR 数值 (hasStats=true): 1106
  规则分布: {'exact': 1055, 'share': 2, 'alias': 11, 'alias_share': 1, 'contain': 21, 'ratio': 16}
- 显示层清洗: 关键词剔除芯片词 31 张 / subtitle 清空 72 张 / subtype 回收 17 张
- 无数值 (index 有, OCR 无): 87
- 未匹配 OCR 条目 (社区/新类型, community_cards.json): 33
- OCR 总数: 1136 (跳过空名 0)

## 模糊/别名匹配明细 (需人工复核)

| index 卡名 | OCR 卡名 | 规则 | 说明 |
|---|---|---|---|
| Medic Scion (AstraMilitarum/unit) | Scion Medic (Infantry) | alias | 人工别名: Scion Medic |
| Leman Russ (AstraMilitarum/unit) | Leman Russ Tank (Vehicle) | contain | - |
| Helfire Pit (BlackLegion/tactic) | Hellfire Pit (Defence) | ratio | - |
| Helfire Torch (BlackLegion/tactic) | Hellfire Torch (Defence) | ratio | - |
| Sylar Hexcorn (BlackLegion/hero) | Sylar Hexscorn (Warlord) | ratio | - |
| Sergeant Naaman (DarkAngels/unit) | Sergeant Taaman (Infantry) | ratio | - |
| Ravenwing Ballistus Dreadnought (DarkAngels/unit) | Ballistus Dreadnought (Vehicle) | contain | - |
| Dual Screamer Kakophonist (EmperorsChildren/unit) | Screamer Kakophonist (Infantry) | contain | - |
| Icon of Excess Infractor (EmperorsChildren/unit) | Icon of Excess (Infantry) | contain | - |
| Lord Kakophonist (EmperorsChildren/unit) | Varius, Lord Kakophonist (Unit) | contain | - |
| Threnodic Choir Noise Marine (EmperorsChildren/unit) | Threnodic Noise Marine (Troop) | ratio | - |
| Duelist's Hubris (Lucius' Talent) (EmperorsChildren/tactic) | Duelist's Hubris (Tactic) | contain | - |
| Euphoric Strike (Lord Exultant's Talent) (EmperorsChildren/tactic) | Euphoric Strike (Event) | contain | - |
| Excessive Vigour (Daemon Prince's Talent) (EmperorsChildren/tactic) | Excessive Vigour (Spell) | contain | - |
| Rusted Vents (Genestealers/unit) | Rusted Vent (Defence) | ratio | 阵营/类型族不一致 |
| Telephatic Domination (Genestealers/tactic) | Telepathic Domination (Spell) | ratio | - |
| Deathmark (Sautekh/unit) | Deathmark Remnant (Infantry) | alias | 人工别名: Deathmark Remnant |
| Gauss Blaster Immortal (Sautekh/unit) | Gauss Immortal (Infantry) | alias | 人工别名: Gauss Immortal |
| Gauss Reaper Warrior (Sautekh/unit) | Gauss Warrior (Infantry) | alias | 人工别名: Gauss Warrior |
| Lokhust Destroyer (Sautekh/unit) | Lokhurst Destroyer (Infantry) | ratio | - |
| Lokhust Heavy Destroyer (Sautekh/unit) | Lokhurst Heavy Destroyer (Infantry) | ratio | - |
| Lokhust Lord (Sautekh/unit) | Lokhurst Lord (Infantry) | ratio | - |
| Tesla Carbine Immortal (Sautekh/unit) | Tesla Immortal (Infantry) | alias | 人工别名: Tesla Immortal |
| Awakening Obelisk (Sautekh/tactic) | Awakened Obelisk (Defence) | alias | 人工别名: Awakened Obelisk |
| Extermination Protocols (Sautekh/tactic) | Extermination Protocol (spell) | contain | - |
| Reconstitution Protocols (Sautekh/tactic) | Reconstitution Protocol (spell) | contain | - |
| HB (Sautekh/unit) | Imotekh the Stormlord (Warlord) | alias_share | 别名组共享: Imotekh the Stormlord |
| stormlord (Sautekh/unit) | Imotekh the Stormlord (Warlord) | alias | 人工别名: Imotekh the Stormlord |
| Diviner (Sautekh/unit) | Orikan the Diviner (Warlord) | alias | 人工别名: Orikan the Diviner |
| Dok’s Toolz (Painboss' talent) (Goff/tactic) | Dok's Toolz (Spell) | contain | - |
| Ferocious Rage (Beastboss' Talent) (Goff/tactic) | Ferocious Rage (Upgrade) | contain | - |
| Da Bigger Dey Iz... (Mozrog's Talent) (Goff/tactic) | Da Bigger Dey Iz... (Warlord) | alias | 人工别名: Da Bigger Dey Iz... |
| Special Dose (Zodgrod Wortsnagga Talent) (Goff/tactic) | Speshul Dose (Spell) | alias | 人工别名: Speshul Dose |
| Waaagh! Energy (Weirdboy Talent) (Goff/tactic) | Waaagh! Energy (Tactic) | contain | - |
| Ork Spanner (Goff/unit) | Spanner (Infantry) | contain | - |
| Armorium Cherub (Sororitas/unit) | Armoirium Cherub (Infantry) | ratio | - |
| Sister Dogmata (Sororitas/unit) | Dogmata (Infantry) | contain | - |
| Sisters Repentia (Sororitas/unit) | Sister Repentia (Unit) | ratio | - |
| Triumph of Saint Katherine (Sororitas/unit) | Triumph of St. Katherine (Infantry) | ratio | - |
| Relics of Saint Katherine (Sororitas/tactic) | Relics of St. Katherine (Relic) | ratio | - |
| Morkai Eliminator (SpaceWolves/unit) | Sons of Morkai Eliminator (Infantry) | contain | - |
| Land Rider (SpaceWolves/unit) | Land Raider (Vehicle) | ratio | - |
| Stormsurge Battlesuit (TauEmpire/unit) | Stormsurge (Vehicle) | contain | - |
| Aunshi Ethereal (TauEmpire/unit) | Aun'Shi (Infantry) | contain | - |
| Breacher Fire Warrior (TauEmpire/unit) | Fire Warrior Breacher (Infantry) | alias | 人工别名: Fire Warrior Breacher |
| Experimental Drone (TauEmpire/tactic) | Experimental Drones (Command) | contain | - |
| Saviour Protocolst (TauEmpire/tactic) | Saviour Protocols (Action) | contain | - |
| Razorshark (TauEmpire/unit) | Razorshark Fighter (Vehicle) | contain | - |
| Chairon (Ultramarines/unit) | Chaireon (Infantry) | ratio | - |

## 未匹配 index 卡 (无 OCR 数值)

- Blackout | SaimHann/unit
- Infinity Circuit Overload | SaimHann/unit
- Normal Conditions | SaimHann/unit  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Webway Rift | SaimHann/unit
- Dawn Attack | AstraMilitarum/unit
- Factories Overdrive | AstraMilitarum/unit
- Normal | AstraMilitarum/unit  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Planetary invasion | AstraMilitarum/unit
- Righteous Gaze | AstraMilitarum/tactic
- Commissar Elan | AstraMilitarum/hero
- Daemonic Feast | BlackLegion/unit
- Helfire Outburst | BlackLegion/unit
- Normal Conditions | BlackLegion/unit  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Warp Storm | BlackLegion/unit
- Khorne | BlackLegion/unit
- Nurgle | BlackLegion/unit
- Slaanesh | BlackLegion/unit
- Tzeench | BlackLegion/unit
- Method to the Madness | BlackLegion/tactic
- Warmaster | BlackLegion/tactic
- v2 | DarkAngels/unit
- Asteroid Zone | DarkAngels/tactic
- Exemplar of Hate | DarkAngels/tactic
- Normal Conditions | DarkAngels/tactic  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Star Orbiting | DarkAngels/tactic
- Void Combat | DarkAngels/tactic
- Threnodic Choir Flawless Blade | EmperorsChildren/unit
- Threnodic Choir Flawless | EmperorsChildren/unit
- Aural Hijack | EmperorsChildren/unit
- Empyric Rift | EmperorsChildren/unit
- Stimm-Vents Leak | EmperorsChildren/unit
- Dark Prince´s Throne | EmperorsChildren/tactic
- Normal | EmperorsChildren/tactic  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Tools of Torture | EmperorsChildren/tactic
- Mining Tremors | Genestealers/unit
- Normal Conditions | Genestealers/unit  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Sump Overspill | Genestealers/unit
- Toxic Fumes | Genestealers/unit
- Ambush | Genestealers/unit
- Hive Fleet Arrival | Genestealers/tactic
- Xeno-Mutations | Genestealers/tactic
- Acolyte Iconward | Genestealers/hero
- Earthquake | Sautekh/unit
- Immortal Beams | Sautekh/unit
- Normal Conditions | Sautekh/unit  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Solar Storm | Sautekh/unit
- Annihilation Command | Sautekh/tactic
- Disentegration Capacitors | Sautekh/tactic
- Veteran Flyboy | Goff/unit
- Dust Storm | Goff/unit
- Night Attack | Goff/unit
- Normal Conditions | Goff/unit  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Spore Cloud | Goff/unit
- Simulacrum Imperialis | Sororitas/unit
- Disrupted Sanctuary | Sororitas/tactic
- Normal | Sororitas/tactic  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Purgator Mirabilis | Sororitas/tactic
- Raging Storm | Sororitas/tactic
- Shrine Bombardement | Sororitas/tactic
- Slay the Heretic | Sororitas/tactic
- Default Conditions | SpaceWolves/unit
- Everstorm | SpaceWolves/unit
- First Light | SpaceWolves/unit
- Full moon | SpaceWolves/unit
- Grav Inhibitor Drone | TauEmpire/unit
- Fire Warrior Sniper | TauEmpire/unit
- Normal Conditions | TauEmpire/unit  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Electro-Static Interference | TauEmpire/unit
- Radiation Storm | TauEmpire/unit
- Solar Eclipse | TauEmpire/unit
- Crushing Claws | Leviathan/tactic
- Acid Rain | Leviathan/unit
- Blazing Biomass | Leviathan/unit
- Normal Conditions | Leviathan/unit  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Sweeping Infestation | Leviathan/unit
- Armoured Exoskeleton | Leviathan/tactic
- Devourer Cannon | Leviathan/tactic
- Infestation Node | Leviathan/tactic
- Sporecaster Biostructure | Leviathan/tactic
- 2nd Company Terminator | Ultramarines/unit
- Bladeguard Lieutenant | Ultramarines/unit
- Lieutenant with Combi-Weapon | Ultramarines/unit
- Fleet Support | Ultramarines/unit
- Normal Conditions | Ultramarines/unit  ← 疑似索引噪音(占位卡面名) / 立绘与其它卡重复
- Orbital Bombardment | Ultramarines/unit
- Thunderstorm | Ultramarines/unit
- Predator Destructor | Ultramarines/unit

### 接近匹配但未确认 (ratio 0.75~阈值)

- Commissar Elan ~> Commissar Denkler (ratio 0.76)
- Sporecaster Biostructure ~> Protective Bio-structure (ratio 0.80)
- 2nd Company Terminator ~> 1st Company Terminator (ratio 0.85)
- Bladeguard Lieutenant ~> Bladeguard Veteran (ratio 0.81)

## 未匹配 OCR 卡 (社区/新卡类型, 供 P3)

- Commissar Denkler | Warlord | 阵营文件夹 Astra Militarum | PnP 卡面
- Lord Commander | spell | 阵营文件夹 Astra Militarum | PnP 卡面
- Dark Pact of Fate | Dark Pact | 阵营文件夹 Chaos | PnP 卡面
- Dark Pact of Fate | Dark Pact | 阵营文件夹 Chaos | 社区截图
- Dark Pact of Blood | Dark Pact | 阵营文件夹 Chaos | PnP 卡面
- Dark Pact of Blood | Dark Pact | 阵营文件夹 Chaos | 社区截图
- Dark Pact of Excess | Dark Pact | 阵营文件夹 Chaos | PnP 卡面
- Dark Pact of Excess | Dark Pact | 阵营文件夹 Emperor_s Children | PnP 卡面
- Dark Pact of Excess | Spell | 阵营文件夹 Chaos | 社区截图
- Dark Pact of Resilience | Dark Pact | 阵营文件夹 Chaos | PnP 卡面
- Dark Pact of Resilience | Dark Pact | 阵营文件夹 Chaos | 社区截图
- Chosen of the Four | Pact | 阵营文件夹 Chaos | PnP 卡面
- Diabolic Strength | Psychic Power | 阵营文件夹 Chaos | 社区截图
- Infernal Gaze | Psychic Power | 阵营文件夹 Chaos | 社区截图
- Unhallowed Gifts | Psychic Power | 阵营文件夹 Chaos | 社区截图
- Master of Repentance | Tactic | 阵营文件夹 Dark Angels | PnP 卡面
- Bladeguard Veteran | Infantry | 阵营文件夹 Ultramarines | PnP 卡面
- Lord Kaphrael | Warlord | 阵营文件夹 Emperor_s Children | PnP 卡面
- Veldras the Sublime | Infantry | 阵营文件夹 Emperor_s Children | PnP 卡面
- Armoury of Excess | Defence | 阵营文件夹 Emperor_s Children | PnP 卡面
- Decadent Throne | Defence | 阵营文件夹 Emperor_s Children | PnP 卡面
- Iconward Malak Vorenth | Warlord | 阵营文件夹 Genestealer Cult | PnP 卡面
- Poisoned Supplies | Sabotage | 阵营文件夹 Genestealer Cult | 社区截图
- Undying Legions | Event | 阵营文件夹 Necron | PnP 卡面
- Veteran Stormboy | Infantry | 阵营文件夹 Orks | PnP 卡面
- Simulacrum Celestian | Troop | 阵营文件夹 Sorotitas | PnP 卡面
- Moment of Grace | Spell | 阵营文件夹 Sorotitas | PnP 卡面
- Fire Warrior Marksman | Infantry | 阵营文件夹 Tau | PnP 卡面
- Protective Bio-structure | Spell | 阵营文件夹 Tyranid | PnP 卡面
- Exemplary Warrior | Support | 阵营文件夹 Ultramarines | PnP 卡面
- Tactical Insight | Tactic | 阵营文件夹 Ultramarines | PnP 卡面
- Predator Annihilator | Vehicle | 阵营文件夹 Ultramarines | PnP 卡面
- 1st Company Terminator | Infantry | 阵营文件夹 Ultramarines | PnP 卡面

## 冗余 OCR 副本 (同一卡多份截图/重复文件)
