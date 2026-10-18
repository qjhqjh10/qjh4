# R · 残留 PnP / 自造卡牌 ID 普查（只读）

> 日期：2026-10-18 · 触发：用户「之前我们卡牌那里使用了大量 pnp 和自己设计的卡牌 ID，现在更正了，检查还有什么地方残留」
> 口径：**只普査、不修代码**；只改了本报告 §四 列出的 7 份 `资料/*.md` 笔误（不含 `资料/任务线/**`、`资料/历史/**`、`资料/普查产出_*/**`）。
> 判据：`数据/游戏数据/card_ids.json` 的 `mapping`（1,351 条 `uniqueId→卡名`）· `cards_engine.json`（1,127 张）· K06 备份 `d:/4/_tmp_view/backup_1018/k06/cards_engine.json.BEFORE`（旧池 1,127 张，114 张 id 变过）。

---

## 一、结论（一句话 + 数字）

**运行时/代码侧已经干净：live `.cs`、运行时 JSON、场景/prefab 里，旧池 id 与 `前缀_卡名` 自造 id 都是 0 处。残留集中在「文档」和「立绘孤儿文件」两类。**

| 类别 | 读数 | 说明 |
|---|---|---|
| live `.cs`（`MyGame/Assets/**`） | **0** 处旧/自造 id | 另有 54 处「旧 id 恰好是新真 id、且指向同一张卡」的良性引用（§六·C1） |
| 运行时 JSON（`Resources/**` + `数据/**`） | **0** 处坏 id | 生成的 10 份产物逐 id 校验全过；`card_ids.json` 的 `poolTrueIds` 是**故意保留的历史表** |
| 场景 / prefab / asset / anim / controller | **0** 处 | 1,246 个文件 |
| 活工具脚本 `.py`（非 `_` 开头） | **0** 处代码级坏 id | 命中全在注释或 K06 迁移脚本自身；`gen_icon_plan.py` 的 300+ 张逐卡表已随 K06 换过 |
| 文档（`资料/**`，不含历史/普查产出/任务线） | **约 125 行**需要处理 | 13 行旧池 id + 93 行 PnP 编号（§五 B2/B3 逐条表）；其中 19 行已改（§四） |
| 生成物 `资料/卡面图标_对照与缺口.md` | **617 行 / 169 个 id 不在现池** | 纯生成物，重跑 `工具/gen_icon_doc.py` 即同步（§五 B1） |
| 立绘孤儿 `Art/cards/art_*.png` | **197 个不在现池** | 其中 **164 个是旧 id 的陈旧副本**、**26 个是自设计 StarterCards 的（故意）**、**7 个是已删 id**（§五 B4） |
| 任务线 `资料/任务线/**` | **7 行** | 明文禁止改，只登记（§五 B6） |
| `工具/ruleprobe/scan.txt` | **38 行旧 id** | 未跟踪的旧 dump，K06 已记「不被任何脚本消费」 |

> 另：`项目任务.md`、`CLAUDE.md` 里对 114 个旧 id + 23 个自造 id 的扫描 = **0 命中**。

---

## 二、搜索面（先打出搜过哪里，再说「没有」）

### 2·1 目录 / 扩展名 / 文件数

| 组 | 根 | 扩展名 | 文件数 |
|---|---|---|---|
| live 代码 | `MyGame/Assets` | `.cs` | 435 |
| live 脚本 | `工具`（跳过 `_` 开头的历史一次性脚本） | `.py` | 266 |
| live 运行时数据 | `MyGame/Assets/CardPresentation/Resources` · `MyGame/Assets/RuleEngine/Resources` · `数据` | `.json` | 1,278 |
| 场景/资产 | `MyGame/Assets` | `.unity` `.asset` `.prefab` `.anim` `.controller` | 1,246 |
| live 文档 | `资料`（排除 `历史/` `普查产出_*/` `任务线/`） | `.md` `.tsv` `.txt` | 484 |
| 历史/普查产出 | `资料/历史` + `资料/普查产出_*` | `.md` `.tsv` `.txt` `.json` | 1,846 |
| 立绘 | `MyGame/Assets/CardPresentation/Resources/Art/cards` | `art_*.png` | 1,478 个 png（1,324 个带 id） |

### 2·2 关键词 / 正则

1. **自造 id 形态**：`\b[A-Z]{2,4}_[A-Za-z0-9_]{2,60}\b`，再用「去掉前缀后的 slug 是否命中真卡名」过滤掉 `UM_CardDraw` 这类**原版特效/贴图资源名**。
2. **旧池 id（114 条）**：`poolTrueIds` 里 `key != value` 的 114 条；其中 **23 条是自造形态**（`UM_Phobos_Lieutenant` 等），**91 条是旧数字 id**（`DA83`、`SW13` 等）。注意 **76 条旧数字 id 同时又是另一张卡的真 id**（如旧 `SW4`=High King of Fenris，现 `SW4`=Blood Claw）——这批不能只按 token 判，必须**连同同行卡名**一起核。
3. **PnP 名**：从 `d:/2/Warpforge部队卡片/**/Warpforge_<N>_<名>.png` 解析出 1,060 个名字；去掉「仅标点/撇号差异」后，**30 个与真卡名实质不同**（`Land Raider` vs `Land Rider`、`Sons of Morkai Eliminator`、`Gauss Warrior` …），逐个在活文件里搜。
4. **名称(id) 配对**：`Name(ID)` · `Name`(ID) · `ID Name` 三种写法，逐对校验 `mapping[id] == Name`。
5. **本地化键**：`Card_Name/<id>` / `Card_Description/<id>` 的 id 是否在 `mapping`。

---

## 三、判据口径

| 集合 | 条数 | 来源 |
|---|---|---|
| 真 id（`mapping` 键） | **1,351** | `数据/游戏数据/card_ids.json`（当前 build 的 `allcards`，2026-10-18） |
| 当前卡池 id | **1,127** | `MyGame/Assets/RuleEngine/Resources/cards_engine.json` |
| 旧池 id（`poolTrueIds` 里 key≠value） | **114** | 同上；`poolIdsAreTrue: true` |
| 其中自造形态 `前缀_卡名` | **23** | 见下 |
| PnP 名 vs 真名实质不同 | **30** | PnP 文件名 vs `mapping` 值（归一化标点后） |
| 自设计 StarterCards（测试夹具） | **26** | `MyGame/Assets/RuleEngine/Data/StarterCards.cs` |

23 个自造 id：

```text
UM_2nd_Company_Terminator UM_Chaplain_Cassius UM_Chapter_Champion UM_Devastator_Marine
UM_Incursor UM_Phobos_Librarian UM_Phobos_Lieutenant UM_Scout_Sniper UM_Suppressor
UM_Tactical_Marine UM_Terminator UM_Vico_Therbeus UM_Angel_s_Wrath UM_Author_of_the_Codex
UM_Catechism_of_Death UM_Master_of_Battle UM_Primarch_of_the_XIII UM_Supreme_Strategist
UM_Attack_Bike UM_Dreadnought UM_Firestrike_Servo_Turret UM_Roboute_Guilliman UM_Valius_Paxor
```

> 口径差异：`K06_池id迁移_报告.md` 写「自造 id 14 张」，而 **K06 备份实测是 23 条自造形态**。差 9 条的原因没查清（可能是上一轮统计口径不同）；不影响结论（现池 0 张）。

---

## 四、A 档：已就地改的文档笔误（7 份 / 19 行）

| 文件:行 | 原文 | 改为 | 判据 |
|---|---|---|---|
| `资料/原版预组牌_核对.md:199` | `UM8 = Phobos Lieutenant（池里现在是自造 id UM_Phobos_Lieutenant）` | `UM90 = Phobos Lieutenant`（带 2026-10-18 订正注） | `mapping`：UM90=Phobos Lieutenant，UM8=Eliminator |
| `资料/预组卡组_原版规格.md:609` | 同上 | 同上 | 同上 |
| `资料/预组卡组_原版规格.md:210` | `card_ids.json（996 条、30 KB）` | `1,351 条 uniqueId→英文名、76 KB` | 现读文件 78,301 B / `counts.mapping=1351` |
| `资料/预组卡组_原版规格.md:212` | `1126 张里 973 张原版 id、153 张自造 id` | `1,127/1,127 全是真 id、自造 0` | 现池逐张核 |
| `资料/事件层_数据与设计.md:234` | `SW17 Grey Hunter` | `SW10 Grey Hunter` | `mapping`：SW10=Grey Hunter，SW17=Wulfen |
| `资料/真Play待验清单.md:151` | `Company Master（DA31）` | `Company Master（DA28）` | `mapping`：DA28=Company Master，DA31=Ezekiel |
| `资料/规则引擎_进度与交接.md:226` | 「自建 id→卡名表、996 条」 | 「真值表、1,351 条、`poolIdsAreTrue: true`」 | 现读 |
| `资料/阶段二_卡片详情窗_原版规格.md:245-246` | 「8 张天赋卡是自造 id」 | 「7 张、池内都是真 id；`cards.json` 的 id/cost/desc 全 null 是 PnP 源属性，不是自造证据」 | 现池 7 张 `(X's Talent)`；`cards.json` 全部卡片 id 均为 null |
| `资料/战术卡剩余7条_语义查证.md:36` | `ASH52 Cosmic Serpent` | `ASH48 Cosmic Serpent` | 现池 |
| `:37` | `BL77 Spreading Corruption` | `BL74 Spreading Corruption` | 现池 |
| `:38` | `GSC_Telephatic_Domination` | `GSC49 Telepathic Domination` | 现池 |
| `:39` | `SAU61 Undying Legions` | `SAU53 Undying Legions` | 现池 |
| `:40` | `TL53 Infinite Biomorphologies` | `TL50 Infinite Biomorphologies` | 现池 |
| `:41` | `GOF_Mekaniak` | `9dcbe3d0c04b02f4fbb19974ae0e37b2 Mekaniak` | 现池 |
| `:42` | `UM84` / `UM89` / `UM_Vico_Therbeus` | `UM100` / `UM106` / `UM93` | 现池：Chaplain Cassius / Ferren Areios / Vico Therbeus |
| `:43` | `GOF_Big_Choppa_Nob` / `TL30 Broodlord` | `GOF68 Big Choppa Nob` / `TL19 Broodlord` | 现池 |
| `:44` | `SW42 Bjorn the Fell-Handed` | `SW36 Bjorn the Fell-Handed` | 现池 |
| `:45` | `TL83 Norn Emissary` | `TL70 Norn Emissary` | 现池 |

> 行尾：7 份文件里 6 份是纯 LF（改后仍纯 LF），`资料/规则引擎_进度与交接.md` 是纯 CRLF（改后 CRLF=LF=905，未翻行尾）。

---

## 五、B 档：要人判 / 不能手改

### B1 · `资料/卡面图标_对照与缺口.md` —— 纯生成物，已严重过期

- 头部自己写着「由 `工具/gen_icon_doc.py` 从 `数据/游戏数据/card_icon_plan.json` 生成，别手改」。
- 实测：文档里 **880 个 distinct id、617 行**的 id 不在现池（169 个 distinct）；而 `card_icon_plan.json` 现读 **874 张、id 全在现池**。
- 典型：文档仍用 `DA76` / `SW65` / `UM_Suppressor` / `UM90`(Whirlwind) 等旧 id；现表已换成 `3887…` / `SW60` / `UM97` / `UM107`。
- **处置**：重跑 `python 工具/gen_icon_doc.py`（读现计划表）。本轮按「不跑生成器」未执行；⛔ 不要手改。

### B2 · 旧池 id / 自造 id 仍写在文档里（旧→新，逐条）

> 这批是 **K06 改过 id 的卡**；文档里的 token 现在是另一张卡（76 条会撞真 id）。已改的 2 行不在此表。

| 文件:行 | 旧→新 | 卡 |
|---|---|---|
| `资料/PnP卡图_逐张对账_0915.md:272` | `SW45 -> SW38` | Land Rider |
| `资料/PnP卡图_逐张对账_0915.md:303` | `SW35 -> SW28` | Blackmane Aggressor |
| `资料/PnP卡图_逐张对账_0915.md:369` | `UM87 -> UM104` | Bladeguard Ancient |
| `资料/PnP卡图_逐张对账_0915.md:659` | `SW35 -> SW28` | Blackmane Aggressor |
| `资料/卡实例身份_爆炸半径.md:161` | `SW16 -> SW9` | Fenrisian Wolf |
| `资料/卡实例身份_爆炸半径.md:170` | `SW5 -> a51adc8f5c311584385c3117cfd919e9` | High Rune Priest |
| `资料/原版预组牌_核对.md:109` | `SW46 -> SW40` | Bjorn's Shrine |
| `资料/待办判据_1018.md:100` | `DA83 -> DA66` | Ravenwing Ballistus Dreadnought |
| `资料/待办判据_1018.md:149` | `SW45 -> SW38` | Land Rider |
| `资料/待办判据_第三十三会话.md:1634` | `UM95 -> UM113` | Duty's End |
| `资料/待办判据_第三十三会话.md:1764` | `SW43 -> SW35` | Venerable Dreadnought |
| `资料/待办判据_第三十三会话.md:1799` | `SW13 -> SW8` | Blackmane Reiver |
| `资料/待办判据_第三十三会话.md:2576` | `DA76 -> 38871853fabb5244d8148766829230cd` | Refuse to Yield |

> `资料/待办判据_第三十三会话.md` 那几条是**在讲「当年 id 是怎么排的」**，属历史论证；改 token 会让论证失真 ⇒ 建议保留原样并加一句「K06 后这些旧号已作废」。
> `资料/卡面图标_对照与缺口.md` 的 19 行同类项见 B1（不要手改）。

### B3 · 文档里的 PnP 编号（`ID + 卡名` 对不上现真值）—— 93 条

> 这批多为 **PnP 编号**（当年按卡图文件号排的），现在同一个 token 在真值里是另一张卡。格式：`文件:行 | 现 token → 正确 id | 卡名`。

| 文件:行 | 现 token → 正确 id | 卡名 |
|---|---|---|
| `资料/事件层_数据与设计.md:127` | TL30 → TL19 | Broodlord |
| `资料/事件层_数据与设计.md:183` | AM60 → AM55 | Mechanised Infantry |
| `资料/事件层_数据与设计.md:183` | AM61 → AM56 | Vengeance for Cadia |
| `资料/事件层_数据与设计.md:184` | TAU66 → TAU57 | Burgeoning Empire |
| `资料/事件层_数据与设计.md:209` | BL38 → BL20 | Child of Torment |
| `资料/事件层_数据与设计.md:210` | BL19 → BL8 | Noise Marine |
| `资料/事件层_数据与设计.md:239` | SAU72 → SAU71 | Immortals Phalanx |
| `资料/事件层_数据与设计.md:242` | DA47 → DA42 | Reconnaissance Mission |
| `资料/关键词图标/_规则书关键词表.md:194` | AM13 → AM12 | Flamer Trooper |
| `资料/关键词图标/_规则书关键词表.md:194` | AM34 → AM30 | Manticore |
| `资料/关键词图标_现状与总表.md:79` | ASH75 → 851f73daf747eb642934a16429b391db | Storm of Silence |
| `资料/关键词图标_现状与总表.md:236` | GSC70 → GSC69 | Jackal Outrider |
| `资料/关键词图标_现状与总表.md:247` | SOR72 → SOR71 | Adelaide the Serene |
| `资料/关键词图标_现状与总表.md:249` | TAU36 → TAU28 | Enforcer Battlesuit |
| `资料/关键词图标_现状与总表.md:250` | SAU70 → SAU68 | Lokhust Destroyer |
| `资料/关键词图标_现状与总表.md:251` | GSC15 → a7bb23355a30d344e9d2decf976d91e1 | Poisoned Supplies |
| `资料/单位卡desc与光环_批次划分.md:442` | EC7 → EC32 | Daemonette |
| `资料/卡实例身份_爆炸半径.md:170` | ASH66 → ASH57 | Webway Entrance |
| `资料/卡实例身份_爆炸半径.md:170` | GSC4 → fd67b47572987b84cb2990ea1ecfe982 | Brood Prophet |
| `资料/卡实例身份_爆炸半径.md:170` | SAU48 → SAU45 | Hyperlogical Strategist |
| `资料/卡实例身份_爆炸半径.md:170` | SAU38 → 65b2be828179515458a282cf1b027182 | Indomitable Will |
| `资料/卡实例身份_爆炸半径.md:170` | AM6 → af98d423c7279a14c84c0d0382c53541 | Legendary Commandant |
| `资料/卡实例身份_爆炸半径.md:170` | SW5 → a51adc8f5c311584385c3117cfd919e9 | High Rune Priest |
| `资料/卡实例身份_爆炸半径.md:170` | TAU3 → f2b80d575cbf8f64597b991022beaf35 | Serene Unifier |
| `资料/卡实例身份_爆炸半径.md:170` | GSC16 → 4959cd9e99597f74ca9af0bd65ae6307 | Experimental Bio-Horrors |
| `资料/卡实例身份_爆炸半径.md:170` | EC21 → EC10 | Infractor Obsessionist |
| `资料/卡实例身份_爆炸半径.md:170` | EC64 → EC59 | Vial Tanks |
| `资料/卡实例身份_爆炸半径.md:171` | SW4 → SW2 | Njal Stormcaller |
| `资料/卡实例身份_爆炸半径.md:172` | TL53 → TL50 | Infinite Biomorphologies |
| `资料/卡面图标_现状与缺口.md:53` | DA16 → DA13 | Librarian |
| `资料/卡面图标_现状与缺口.md:53` | DA43 → DA38 | Repulsor |
| `资料/卡面图标_现状与缺口.md:56` | DA38 → DA34 | Unforgiven Redemptor |
| `资料/卡面图标_现状与缺口.md:72` | DA16 → DA13 | Librarian |
| `资料/卡面图标_现状与缺口.md:284` | ASH75 → 851f73daf747eb642934a16429b391db | Storm of Silence |
| `资料/卡面图标_现状与缺口.md:295` | DA44 → DA39 | Ancient Reliquary |
| `资料/卡面图标_现状与缺口.md:295` | EC8 → EC33 | Alluress |
| `资料/卡面图标_现状与缺口.md:296` | EC17 → EC1 | Sonic Blaster Noise Marine |
| `资料/原版预组牌_核对.md:15` | BL2 → 97e959444b53f424ea4285e2ca02c7e6 | Chosen of the Four |
| `资料/原版预组牌_核对.md:15` | BL26 → BL13 | Chosen |
| `资料/原版预组牌_核对.md:104` | TAU19 → TAU5 | Fire Warrior |
| `资料/原版预组牌_核对.md:104` | TAU39 → TAU33 | Razorshark |
| `资料/原版预组牌_核对.md:105` | UM2 → dcbd28525888ce54aaa93a47ba4de1e6 | Master Tactician |
| `资料/原版预组牌_核对.md:105` | UM45 → 0544047df88d2ba4cad9c17ff080c0eb | Greatest Deeds |
| `资料/原版预组牌_核对.md:105` | UM72 → UM71 | Storm Speeder Hailstrike |
| `资料/原版预组牌_核对.md:105` | UM75 → UM74 | Rapid Deployment |
| `资料/原版预组牌_核对.md:105` | UM79 → UM81 | Eliminator Sergeant |
| `资料/原版预组牌_核对.md:106` | UM1 → UM46 | Marneus Calgar |
| `资料/原版预组牌_核对.md:106` | UM9 → cf0b346f70e748c4bbfc4dc03ab39ead | Smite |
| `资料/原版预组牌_核对.md:110` | SOR19 → SOR10 | Sister Repentia |
| `资料/原版预组牌_核对.md:113` | DA11 → DA6 | Sergeant Taaman |
| `资料/原版预组牌_核对.md:210` | DA11 → DA6 | Sergeant Taaman |
| `资料/原版预组牌_核对.md:243` | SAU30 → SAU23 | Lychguard |
| `资料/原版预组牌_核对.md:243` | TAU58 → TAU52 | Storm of Fire |
| `资料/原版预组牌_核对.md:243` | UM15 → UM5 | Primaris Reiver |
| `资料/原版预组牌_核对.md:243` | UM6 → f923701f207d23a4f81d6ff5891b4a0c | Master of Arcana |
| `资料/已知的坑.md:509` | DA43 → DA38 | Repulsor |
| `资料/已知的坑.md:5752` | ASH12 → ASH61 | Weapons Platform |
| `资料/已知的坑.md:5752` | BL11 → BL2 | Black Legionary |
| `资料/已知的坑.md:5753` | BL43 → BL26 | Obliterator |
| `资料/已知的坑.md:5753` | GOF12 → GOF8 | Hornhelmz Boy |
| `资料/已知的坑.md:5753` | TL12 → TL61 | Stranded Termagant |
| `资料/已知的坑.md:5753` | UM14 → UM6 | Primaris Intercessor |
| `资料/待办判据_1018.md:100` | SOR32 → SOR31 | Sister Dogmata |
| `资料/待办判据_1018.md:100` | SW36 → SW29 | Morkai Eliminator |
| `资料/待办判据_1018.md:100` | SW45 → SW38 | Land Rider |
| `资料/待办判据_1018.md:100` | UM83 → UM101 | 2nd Company Terminator |
| `资料/待办判据_1018.md:121` | GSC1 → GSC3 | Acolyte Iconward |
| `资料/待办判据_1018.md:205` | AM73 → AM72 | Vitus Gryf |
| `资料/待办判据_1018.md:205` | DA30 → DA27 | Ravenwing Talonmaster |
| `资料/待办判据_1018.md:205` | EC8 → EC33 | Alluress |
| `资料/待办判据_1018.md:205` | GSC64 → 1a647ea39bbb0154b9673c5194e51a46 | Telephatic Domination |
| `资料/待办判据_1018.md:205` | TAU31 → TAU24 | Devilfish |
| `资料/待办判据_1018.md:212` | TAU31 → TAU24 | Devilfish |
| `资料/待办判据_1018.md:219` | TAU31 → TAU24 | Devilfish |
| `资料/待办判据_1018.md:348` | DA31 → DA28 | Company Master |
| `资料/待办判据_战场与战斗视图.md:900` | BL2 → 97e959444b53f424ea4285e2ca02c7e6 | Chosen of the Four |
| `资料/待办判据_第三十三会话.md:1464` | ASH44 → ASH35 | Wraithknight |
| `资料/待办判据_第三十三会话.md:1554` | ASH75 → 851f73daf747eb642934a16429b391db | Storm of Silence |
| `资料/待办判据_第三十三会话.md:1734` | SOR74 → SOR73 | Sacred Rose |
| `资料/待办判据_第三十三会话.md:1764` | SW25 → SW16 | Long Fang |
| `资料/待办判据_第三十三会话.md:1799` | SW31 → SW23 | Fenrisian Drop Pod |
| `资料/待办判据_第三十三会话.md:1859` | BL68 → BL55 | Black Crusade |
| `资料/待办判据_第三十三会话.md:2463` | TAU35 → TAU29 | Cadre Fireblade |
| `资料/待办判据_第二十五会话.md:539` | GOF90 → GOF91 | Beastboss on Squigosaur |
| `资料/战斗UI_原版对账表.md:298` | DA12 → DA9 | Aggressor |
| `资料/阶段二_卡片详情窗_原版规格.md:361` | BL36 → BL23 | Sorcerer |
| `资料/预组卡组_原版规格.md:620` | DA11 → DA6 | Sergeant Taaman |
| `资料/预组卡组_原版规格.md:653` | SAU30 → SAU23 | Lychguard |
| `资料/预组卡组_原版规格.md:653` | TAU58 → TAU52 | Storm of Fire |
| `资料/预组卡组_原版规格.md:653` | UM15 → UM5 | Primaris Reiver |
| `资料/预组卡组_原版规格.md:653` | UM6 → f923701f207d23a4f81d6ff5891b4a0c | Master of Arcana |
| `资料/待办判据_第四会话.md:138` | TAU38 → TAU34 | Coldstar Battlesuit |
| `资料/待办判据_第四会话.md:138` | TAU13 → TAU9 | Pathfinder |

> 判读建议：`事件层_数据与设计`、`关键词图标_现状与总表`、`卡面图标_现状与缺口`、`单位卡desc与光环`、`战斗UI_原版对账表`、`阶段二_卡片详情窗` 是**现役设计文档**（建议改）；`待办判据_第*会话`、`原版预组牌_核对`、`预组卡组_原版规格` 多为**当时推导记录**（建议只加「K06 后旧号作废」一句，不逐条改）。
> ⚠️ 有些名字有**跨阵营同名**（Aggressor / Terminator / Chosen 等），替换前必须**带阵营核**，别只按名字换。

### B4 · 立绘孤儿（`Art/cards/art_*.png`）

- 目录共 1,478 个 png，其中 1,324 个是 `art_<id>.png` 形式。
- **现池 1,127 张全部有图**（missing = 0）。
- **197 个 id 不在现池**，分三类：

| 类 | 个数 | 例 | 判定 |
|---|---|---|---|
| 自设计 StarterCards 26 张 | **26** | `abyss_titan` · `ember_archer` · `tide_minion` · `ballista` | **故意保留**（`StarterCards.cs` 的测试/演示卡在用） |
| 旧 id 的陈旧副本（同一张卡已有新 id 图） | **156** | `ash_empyric_ambush`（新图 `art_dc7e575c….png` 已在）· `da_asmodai`（新图 `art_da3.png` 已在）· `um_roboute_guilliman`（新图 `art_um87.png` 已在） | **死文件**（无任何代码按这个 id 取）；共约 **220 MB** |
| 未匹配 / 已删 id | **15** | `am_commissar_elan` · `bl_sylar_hexcorn` · `da_sergeant_naaman` · `sor_sisters_repentia` · `tau_saviour_protocolst`（拼写）· `ec72`–`ec77` · `tl81` | 前 8 个多半是 PnP 名/拼写差异的旧图；`ec72-77`/`tl81` 是 `_oneoff_1015_p1fix/clean_dead_cardids.py` 明列的**已删 id** ⇒ 旧图 |

- 判据：文件名 `art_<id>.png`（小写 id），消费口按 `cards_engine.id` 取；孤儿不会被取到。
- **处置建议**：只删「第二类 156 个」；删前再跑一次 `import_original_art.py` 确认不缺图（本轮不删）。
- 注：`Resources/Art/` 被 `.gitignore` 忽略，这些文件不进 git，换机器重跑生成器即可清掉。

### B5 · `工具/ruleprobe/scan.txt`

- 493,866 B，2026-10-14 的旧 dump；含 **38 行** 旧/自造 id（`DA83` / `SW68` / `UM_Roboute_Guilliman` 等）。
- K06 报告已记：「未跟踪的旧 dump，不被任何脚本消费」。
- `工具/ruleprobe/基线/out_baseline.txt` / `out_play_baseline.txt` 已重生成，**0 命中**。

### B6 · `资料/任务线/**`（明文禁改，只登记）

| 文件:行 | 现文 | 说明 |
|---|---|---|
| `资料/任务线/卡牌线/任务/T01_卡id真值链.md:35,39,40` | `DA75..DA86` / `SW66` / `DA76` | 描述「2025-07-30 之后新卡、id 自己排」的**历史结论**；K06 后旧号已作废，但这是任务记录 |
| `资料/任务线/卡牌线/任务/T02_卡表数据订正.md:37,85,110` | `DA83`（A2016 卡名谁对） | 该卡的现真 id = `DA66`；任务线本轮不改 |
| `资料/任务线/卡牌线/任务总表.md:105` | `SW20/DA78/UM87/GSC48/SOR76` | 同上 |

---

## 六、C 档：看着像、其实不是错

1. **live `.cs` 里 54 处旧 id 现在指向同一张卡**：K06 已把代码里的旧 token 换成新真 id，但有些新 id 恰好等于另一张旧卡的号（`SW4`=Blood Claw、`SW36`=Bjorn、`UM90`=Phobos Lieutenant…）。逐行核过**卡名与 id 一致**，0 处不符。例：`CardText.cs:292` `SW4 Blood Claw`、`CardDef.cs:3294` `SW23 Fenrisian Drop Pod`。
2. **`数据/游戏数据/card_ids.json` 的 `poolTrueIds`**：38 行旧 id（DA76-86 / SW65-68 / 23 个自造形态）+ 76 条「旧号撞真号」= 114 条，是**历史迁移表**，`poolIdsAreTrue: true`，故意保留。
3. **官方本地化键**：`Card_Name/*` 里 27 个、`Card_Description/*` 里 40 个不在 `mapping` —— 全是**原版自己的键**：`BL_OldMarkOfKhorne/Nurgle/Slaanesh/Tzeentch` · `EC65`–`EC75`（当前 build 没有这些卡）· `ASH19_changed` / `UM57_new` / `SOR24_new` 这类**原版占位/改稿键** · `Berzerk` / `Fast` / `Flank` / `FirstStrike` 等**关键词描述键** · `NormalOffensiveConditions`。⛔ 不是我们的 PnP id。
4. **PnP 名只留在「源头/出处」里**：`数据/游戏数据/card_stats.json` 的 `ocrName` / `ocrSrc`、`数据/索引/*` 的原始资产名、`资料/卡牌数据表/**`、`资料/卡表核对_卡图提取/**`、ASR 转写 TSV —— 这些是 PnP/OCR **派生来源与留痕**，卡池 `name` 已 1,127/1,127 与 `mapping` 真名逐字一致。
5. **注释里讨论 PnP 名 vs 真名**（`CreatePool.cs:444-446` 的 `Gauss Warrior ↔ Gauss Reaper Warrior` 对照、`PragatiDigits.cs:85` 的 `EC41 Chaos Land Raider`）—— 是**故意的说明**，不是数据。
6. **`_oneoff_*` / `_` 开头的历史脚本**（`工具/_oneoff_1015_p1fix/remap_stale_cs.py`、`_oneoff_1015b/land2.py` 等）—— 一次性迁移记录，K06 明确「不动 `_` 开头脚本」。
7. **`RuleEngine/Data/StarterCards.cs` 的 26 张自设计卡**（Ember Legion / Tide Swarm）—— 文件头写明「自己设计的一套测试卡牌」，用于 `RuleEngineTest` / `BattleScene` 夹具；它们的 id = 卡名，**不进卡池、不参与抽卡**。loc 表里那 26 条 `Card_Name/*` 与立绘也是配套的。
8. **`资料/语音转写_ASR_0918/*.tsv|json`** —— 2026-09-18 的 ASR 原始产物，含旧 id；是资料留痕，不被运行时读（运行时读 `Resources/voice_lines.json`，已 K06 迁移、0 坏 id）。
9. **`资料/说明书/03_卡牌/卡牌定义与卡组数据.md`** —— 原版卡包/卡组 SO 的**原版 id**，与 `mapping` 一致（抽查 UM23=Bladeguard Lieutenant、UM47=Uriel Ventris、GOF1=Grot 全对），不是 PnP 编号。

---

## 七、没查清的部分

1. **「K06 自造 id 14 张」vs 备份实测 23 条**：差 9 条的原因没查清（可能上一轮统计只数了「没有数字后缀」的那一批）。结论不受影响。
2. **7 个已删 id 的立绘**（`ec72`–`ec77`、`tl81`）：确认是旧 id，但这批卡是「从当前 build 移除」还是「改名」，没查清；建议直接按死文件处理前先问一句。
3. **`am_commissar_elan` / `da_sergeant_naaman` / `tau_saviour_protocolst`** 等 8 个未匹配立绘：名字与现名有 PnP 拼写差（`Commissar Elan`、`Sergeant Naaman`、`Saviour Protocols`、`Sisters Repentia`），但**卡池里也没有完全同名的卡**（`Sergeant Naaman` 不在池、`Naaman` 也没有），所以「它们该对应哪张现卡」没查清。
4. **`资料/卡实例身份_爆炸半径.md:160` 的 `DA20/24/28/33/36/42/53/81/82`**：`DA81/82` 已是作废旧号；这 9 个号里哪些换成哪些真 id，需要作者按「Deathwing Terminator 一族」重新枚举（现池只有 `DA17 Deathwing Terminator` 这一张精确同名）。
5. **两台机器上的玩家存档**：K06 当天扫过本机 `AppData/LocalLow/**/WarpforgeDecks.json` 与 `daily.json` = 0 处旧 id；本轮**没有重扫**（只读普查范围外）。
6. **`资料/卡面图标_对照与缺口.md` 重跑后是否与旧内容等价**：没跑生成器，未验。

---

## 八、顺手发现（本轮不改）

1. **`数据/游戏数据/cards.json` / `card_stats.json` 的所有 `id` 字段都是 null**（1,193 / 1,213 条）——它们只是 PnP 源表，卡池 id 由 `gen_cards_engine.py` 另取。`阶段二_卡片详情窗` 曾把「id 为 null」当成「自造 id」的证据，已订正。
2. **`card_stats.json` 有 1,213 条，现池 1,127 张**：多出的 86 条含 `noise=true` 占位/重复（已知的 `Normal Conditions` / 同图重复 / 前导空格 `" Iron Priest"`），生成器已过滤。
3. **`工具/gen_icon_plan.py` 里逐卡表（`TOKEN_BY_CARD` / `BARE_TOKEN_BY_CARD` / `FACE_ICON_COUNT`）约 300+ 个 id**：抽查 20 条全部与现池一致（K06 已迁移）。但它是**手写表**，将来再改 id 时仍是高风险点 —— 建议加一条「表里每个 id 都必须在 `cards_engine.json` 里」的自检（现在没有）。
4. **`资料/卡面图标_对照与缺口.md` 的 distinct id 数（880）> 计划表卡数（874）**：多出来的正是旧 id 行，佐证它整体过期。
5. **`资料/原版预组牌_核对.md` 自 2026-10-18 起不再被 `check_prebuilt_decks.py` 重写**（脚本注释明写），所以本轮对它手改**不会被生成器覆盖**。
6. **`资料/待办判据_1018.md:121`** 自己已经写明根因：「`card_ids.json` 的值 = PnP OCR 名，卡池 `name` = 原版资产名，两者不等就静默掉成自造 id」——本轮结论与它一致，只是范围扩到了文档/立绘。
7. **`资料/预组卡组_原版规格.md:212` 写「153 张自造 id」**，而 K06 之前实测是 **114 张旧 id + 23 张自造形态**；这两个数字口径不同，已在 §四订正为「现 0 张」。

---

## 九、复现方法（只读）

```bash
cd d:/4/Unity

# 1) 旧 id 清单（114 条；key != value）
python -I -X utf8 -c "import json,io;d=json.load(io.open('数据/游戏数据/card_ids.json',encoding='utf-8'));print(sum(1 for k,v in d['poolTrueIds'].items() if k!=v))"

# 2) live .cs 里 114 条旧 id 的命中（应只剩「旧号=新真号」的良性注释）
grep -rnE "\b(SW4|SW10|UM90|UM93|UM100)\b" MyGame/Assets --include=*.cs | head

# 3) 运行时数据逐 id 校验（id 必须在 mapping∪pool）
#    见本轮脚本思路：voice_lines / card_vfx_by_card / card_icon_plan /
#    card_anim_order / ability_anim_durations / related_cards / talent_cards /
#    prebuilt_decks / tutorial_stages → 全 0 坏

# 4) 场景/prefab（应 0）
grep -rlE "\b(UM_Terminator|UM_Phobos_Lieutenant|DA83|SW68)\b" \
  --include=*.unity --include=*.asset --include=*.prefab MyGame/Assets

# 5) 立绘孤儿
python - <<'PY'
import os,re,json,io
d='MyGame/Assets/CardPresentation/Resources/Art/cards'
pool={c['id'].lower() for c in json.load(io.open('MyGame/Assets/RuleEngine/Resources/cards_engine.json',encoding='utf-8'))['cards']}
print([f for f in os.listdir(d) if f.endswith('.png') and re.match(r'art_(.+)\.png$',f) and re.match(r'art_(.+)\.png$',f).group(1).lower() not in pool][:20])
PY
```

---

## 十、一句话交底

**代码与运行时数据已经全部是真 id；要收口只剩三件：① 重跑 `gen_icon_doc.py` 同步生成文档；② 决策 156 个旧 id 立绘孤儿要不要删；③ 文档里的 PnP 编号（93 条）按「现役文档改 / 历史记录只加注」两类处理。**

---

## 处置记录（主对话 · 2026-10-18）

- ✅ **① 已做**：`gen_icon_doc.py` 重跑 → `资料/卡面图标_对照与缺口.md`（2,451 行 · 缺口 0）。
- ✅ **② 已做（用户拍板删）**：**156 个旧 id 立绘孤儿已删除**。
  判据 = 「去掉阵营前缀后的名字能在现池找到同名卡 + 该卡已有 `art_<真id>.png`」；
  ⚠️ 已排除 `StarterCards.cs` 的 26 张（含同名陷阱 `falcon` —— 池里也有 `ASH27 Falcon`，按 id 命名区分）。
  实删 **312 个文件**（156 png + 156 meta）= **236.6 MB**；清单 → `d:/4/_tmp_view/backup_1018/art_orphans_deleted_20261018.tsv`。
  删后复核：现池 **1,127/1,127 全部有图**；StarterCards **26/26 仍在**；磁盘上 art png 1,324 → **1,168**。
- ⏳ **③ 未处理**：93 条文档 PnP 编号 —— 现役设计文档改、历史推导记录只加「K06 后旧号作废」一句（⛔ 不逐条改历史）。
- ⏳ 未处理：15 个未匹配/已删 id 的图（保留，等作者判）。
