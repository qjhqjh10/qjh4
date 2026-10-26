# B8_D · `CardDef` / `UnitState` / `Aura` / `Enchantment` 四族 `B → A` ＋ 两条裁定

> 执行代理 **B8_D**（第八十二会话 · 批次 8）· 2026-10-26
> 白名单：`RuleEngine/Core/{CardDef,UnitState,Aura,Enchantment}.cs` ＋ 账补丁 `账/_parts/B8_D.tsv` ＋ 本报告。
> 账补丁 = **14 行**（6 条 A ＋ 2 条 C 进展 ＋ 6 条 B 新证据）。

---

## ① 结论

| 项 | 结果 |
|---|---|
| **真升 A** | **6 条**：`CardScript.GetOwnHero` · `CardScript.HasWhileInPlayAbility` · `CardScript.RemovePackBuffs` · `RawCardScript.IsEnviromentaleffect` · `RawCardScript.IsHeroEquivalent` · `RawCardScript.IsMinionOrAsset` |
| **就地更新（不升档）** | **8 条**：2 条 C（`HasWhileInPlayAdjacentEffect` / `MinionManager.GetNextSlotCheckingAdjacents`，谓词已落地、缺调用点）＋ 6 条 B（4 条 trait 列表查询族 ＋ `IsKeyword` ＋ `GetCurrentTraitValueWithModifiers`，都记了新查实的差异） |
| **任务 2（关键词全集对账）** | 原版 `DefinedTrait` **133** 个逐词对账完成（表在 §2.2）。**识别层已 100% 覆盖「原版数据实际用到的 51 个名字（50 个真 trait ＋ 哨兵 `none`）」**；池内 `keywords` / `cardTraits` 两列**静默丢弃 = 0**。⇒ 账里「`Requiem` 没实现」一句的准确含义是**行为层没有机制**，不是**识别层缺词**。补了 **7 个「卡面印得出但认不出」的词** ＋ **2 个「机制早就在读、只是没登记」的词**。 |
| **任务 3（相邻效果谓词）** | `CardScript.HasWhileInPlayAdjacentEffect` **按原版逐字实现**（替代口径 `AuraSpec.Adjacent` 在**谓词层已撤**），池内**恒 false**。调用点 `Data/SimpleAI.cs` 不在本批白名单 ⇒ 账仍 **C**，AI 布场影响面见 §⑥。 |
| **验证** | `typecheck` **0 / 0** · `ruleprobe check` **解析差异 0 行 · 哨兵合计 0 条 · rc=0** · `ruleprobe layers` 前后逐项同值（`cards=1128 auras=41 willrunOps=1337 rawKeywords=1148`）· `check_patch_conflicts` **无跨补丁重复键** |

**为什么只到 6 条 A（不是 10）** —— 逐条卡点都写在 §④，一句话概括：
池子里绝大多数 B 行的原版方法是**休眠的**（`survivor` 0 张 · `markerlight` 0 张 · `bastion` 0 张 · `jam` 0 张 · `requiem` 0 张 ·
quest 附魔 0 张 · trigger 200 0 条 · `mutationType` 全 0），**拿不到「玩家能看见」的离线读数**（G3 卡住）；
另有一族（trait 列表查询）本轮**查实了字段型差异**（见 §⑤），只能保持 B。⛔ 没有为了凑数把 G3 写成「断言绿了」。

---

## ② 逐条证据

### 2.1 六条 `B → A`

| # | 原版方法 | 判据（🥇 `decomp_full`） | 我们的落点（行号） | 观测量 ＋ 两侧数值 | 机制差异（G2） |
|---|---|---|---|---|---|
| 1 | `CardScript.GetOwnHero()` | `CardScript__GetOwnHero.c:1-13`（＋🥈 cpp2il `CardScript.cs:20900-20907`） | `PlayerState.cs:72` · `UnitState.cs:51` | **本方督军是哪张卡**：原版 `GetHero(isPlayer)`；`play UM100` ⇒ 督军 = 夹具 `WARLORD 2/0/0/28`（≠刚出的卡）；`play ASH1` ⇒ 督军 = `WARLORD 2/2/0/23 {talent1}` = 自己 | 原版是 `BattleManager` 两侧的两个字段（按 `isPlayer` 位取）；我们是棋盘 `WarlordSlot` 上一个 `UnitState`（按座位号取） |
| 2 | `CardScript.HasWhileInPlayAbility()` | `CardScript__HasWhileInPlayAbility.c:1-61`（＋🥈 `CardScript.cs:6800-6863`，**第 ⑥ 支的 lambda 体在那里读得出**） | `UnitState.cs:2374` · `CardDef.cs:2462` | **该卡在场上是否施加在场所效果**：`play GOF91`（`abilityTriggers=[20]`）打之前 `FIX_C 1/0/0/2` / 打之后 `FIX_C 1/0/0/2 {slay1}` | 五支逐支照搬；第 ⑥ 支（`activeEffects` 里 `trait==pack && buffType==1`）我们判不了（`StatMod` 只带 `Attr/Value/Extrinsic/IsSet/Source`）⇒ 按 false ＋ `why` 出声，🔶 自己设计 |
| 3 | `CardScript.RemovePackBuffs()` | `CardScript__RemovePackBuffs.c:1-56` ＋ 第二调用点 `CardScript__CheckAndApplyCurrentPack.c:1-56`（**两条都读完，旧「未核第二调用点」已闭**） | `Aura.cs:641` · `UnitState.cs:2292` · `UnitState.cs:1520` | **pack 光环落在谁身上**：`play SW15` 印的 `{pack1}` ⇒ 落场 `SW15 3/2/0/5 {invulnerable1, pack1}` | 作用域更宽（原版只摘「自己给的 pack addStats」，我们摘全部光环授予再重加）；触发更宽（原版只比己方单位数，同数换位不重算 = 可证的实现偏差）；原版 `+0x228==1` 那一支我们模型里这一类位暂未建模 |
| 4 | `RawCardScript.IsEnviromentaleffect()` | `RawCardScript__IsEnviromentaleffect.c:5-13` ＋ **`SpellType` 枚举现读：200 = OffensiveCard · 210 = DefensiveCard**（`dump.cs:45900-45945`） | `CardDef.cs:136` | **卡的类型标签**：`play ASH55` ⇒ `[defence]`（`spellType=210`）True / `play UM100` ⇒ `[unit]`（`spellType=0`）False；池内 `spellType==210` 39 张 ⟺ `type='defence'` 39 张 | 逐字一致；唯一差别是未知档：我们默认 `-1`（列没落时），不进 {200,210} ⇒ 未知不会被误判成环境卡 |
| 5 | `RawCardScript.IsHeroEquivalent()` | `RawCardScript__IsHeroEquivalent.c:5-7` ＋ `CardTypeOptions`（Minion 0 / Hero 10 / Tactic 20 / Whispers 40） | `CardDef.cs:84` | `play ASH1` ⇒ `[hero]` True / `play UM100` ⇒ `[unit]` False；映射现读 **56/56 张 1:1** | 枚举数值 vs 字符串字面量（`Type` 是生成器常量，拼错 ⇒ 静默 false） |
| 6 | `RawCardScript.IsMinionOrAsset()` | `RawCardScript__IsMinionOrAsset.c:5-7` ＋ 同上枚举（Minion = 0） | `CardDef.cs:84` | `play UM100` ⇒ `[unit]` True / `play ASH1` ⇒ `[hero]` False；映射现读 **586/586 张 1:1** | 同 #5；另注 `Type=="unit"` 与「是不是部队」不是同一件事 |

> G4（两读数）全部满足：上表每一行的两个 `play` 输出是**同一条探针命令的两次不同卡读数**（或同卡「打之前 / 打之后」两快照）。
> G5（边界 ≥2）逐条写在账补丁的 `备注` 列里（每条都给了两类反例的具体张数）。

### 2.2 任务 2 —— 原版关键词全集 × 我们（逐词表）

# 原版 `DefinedTrait` 全集 × 我们两侧（2026-10-26 现读）

列义：**原版数据用** = `cards_raw.json` 1,351 张 SO 的 `CardTraits`（= 原版 `defaultTraits +0x230`）里出现过几处；
**卡面印名** = 官方本地化 `Card_Trait/*`（83 条）里有没有这个名字；**我们认得出** = `KeywordTable.Prefixes` 收不收；**我们有机制** = 在 `Implemented` 里。

| # | DefinedTrait | 值 | 原版数据用 | 卡面印名 | 我们认得出 | 我们有机制 |
|---|---|---|---|---|---|---|
| 1 | `ephemeral` | 5 | 125 | ✓ | ✓ | ✓ |
| 2 | `rally` | 10 | 108 | ✓ | ✓ | ✓ |
| 3 | `sneakattack` | 20 |  |  | ✓ | ✓ |
| 4 | `fast` | 40 |  | ✓ | ✓ | ✓ |
| 5 | `stealth` | 70 | 31 | ✓ | ✓ | ✓ |
| 6 | `shield` | 80 | 13 | ✓ | ✓ | ✓ |
| 7 | `stun` | 100 |  | ✓ | ✓ | ✓ |
| 8 | `syphon` | 110 |  |  |  |  |
| 9 | `backlash` | 120 | 8 | ✓ | ✓ | ✓ |
| 10 | `jam` | 130 |  | ✓ | ✓ | ✓ |
| 11 | `ward` | 140 |  |  | ✓ |  |
| 12 | `wardwb` | 145 |  |  | ✓ |  |
| 13 | `cantattack` | 150 | 1 | ✓ | ✓ | ✓ |
| 14 | `tainted` | 160 |  |  | ✓ |  |
| 15 | `unstoppable` | 180 | 1 |  | ✓ | ✓ |
| 16 | `coward` | 190 |  |  |  |  |
| 17 | `poisoned` | 200 |  |  | ✓ | ✓ |
| 18 | `bloodthirst` | 220 | 3 | ✓ | ✓ | ✓ |
| 19 | `droppod` | 230 |  | ✓ | ✓ | ✓ |
| 20 | `dodge` | 240 |  |  | ✓ | ✓ |
| 21 | `unstunnable` | 250 |  |  | ✓ | ✓ |
| 22 | `slay` | 260 | 16 | ✓ | ✓ | ✓ |
| 23 | `rage` | 280 | 5 | ✓ | ✓（→`penitence`） |  |
| 24 | `doublerage` | 300 |  |  | ✓ |  |
| 25 | `invulnerable` | 310 |  | ✓ | ✓ | ✓ |
| 26 | `corrupt` | 320 |  |  |  |  |
| 27 | `markofchaos` | 330 |  | ✓ |  |  |
| 28 | `giantkiller` | 340 |  |  | ✓ | ✓ |
| 29 | `blast` | 350 | 42 | ✓ | ✓ | ✓ |
| 30 | `berzerk` | 360 |  | ✓ | ✓ | ✓ |
| 31 | `maintenance` | 370 |  |  | ✓ | ✓ |
| 32 | `poisonous` | 380 |  |  |  |  |
| 33 | `flying` | 390 | 80 | ✓ | ✓ | ✓ |
| 34 | `activeability` | 400 |  |  |  |  |
| 35 | `psychicability` | 405 |  |  |  |  |
| 36 | `plasmaability` | 407 |  |  |  |  |
| 37 | `passiveability` | 410 |  |  |  |  |
| 38 | `whileinplayability` | 420 |  |  |  |  |
| 39 | `survivor` | 430 |  |  | ✓ | ✓ |
| 40 | `survivorspent` | 440 |  |  |  |  |
| 41 | `relentless` | 450 |  | ✓ | ✓ |  |
| 42 | `transient` | 455 |  |  |  |  |
| 43 | `flank` | 460 | 42 | ✓ | ✓ | ✓ |
| 44 | `sacrifice` | 470 |  |  | ✓ | ✓ |
| 45 | `vulnerable` | 480 |  | ✓ | ✓ | ✓ |
| 46 | `reload` | 490 |  |  |  |  |
| 47 | `notcollectable` | 500 |  |  |  |  |
| 48 | `secret` | 510 |  |  |  |  |
| 49 | `terror` | 520 |  |  | ✓ |  |
| 50 | `trap` | 530 |  |  |  |  |
| 51 | `secretorder` | 540 |  |  |  |  |
| 52 | `secretidentity` | 550 |  |  |  |  |
| 53 | `fear` | 570 |  |  |  |  |
| 54 | `meltdown` | 580 |  |  |  |  |
| 55 | `massive` | 590 |  |  |  |  |
| 56 | `resistant` | 600 |  |  | ✓ | ✓ |
| 57 | `pack` | 610 | 10 | ✓ | ✓ | ✓ |
| 58 | `psyker` | 620 |  |  |  |  |
| 59 | `reactor` | 625 |  |  |  |  |
| 60 | `firststrike` | 630 |  | ✓ | ✓ |  |
| 61 | `precognition` | 640 |  |  |  |  |
| 62 | `swarm` | 650 | 10 | ✓ | ✓ | ✓ |
| 63 | `psychicpower` | 660 |  |  |  |  |
| 64 | `plasmapower` | 665 |  |  |  |  |
| 65 | `sentinel` | 670 |  |  | ✓ | ✓ |
| 66 | `frenzied` | 680 | 10 | ✓ | ✓（→`destroyer`） |  |
| 67 | `spikes` | 700 |  |  |  |  |
| 68 | `horns` | 710 |  |  |  |  |
| 69 | `daemonhost` | 720 |  |  |  |  |
| 70 | `summonrift` | 730 |  |  | ✓ |  |
| 71 | `quest` | 750 |  |  | ✓ |  |
| 72 | `questcompleted` | 755 |  |  |  |  |
| 73 | `requiem` | 760 |  |  | ✓ |  |
| 74 | `landing` | 770 |  |  | ✓ | ✓ |
| 75 | `mutation` | 780 |  |  |  |  |
| 76 | `chaosonly` | 790 |  |  |  |  |
| 77 | `chooseone` | 820 |  | ✓ | ✓ |  |
| 78 | `resolution` | 840 |  | ✓ | ✓ |  |
| 79 | `talent` | 845 | 105 | ✓ | ✓ | ✓ |
| 80 | `bastion` | 850 |  |  | ✓ | ✓ |
| 81 | `mission` | 860 |  |  |  |  |
| 82 | `noncombatant` | 880 |  |  | ✓ | ✓ |
| 83 | `piercing` | 900 |  |  |  |  |
| 84 | `tide` | 910 | 24 | ✓ | ✓ | ✓ |
| 85 | `mob` | 920 | 16 | ✓ | ✓ | ✓ |
| 86 | `concussive` | 930 | 6 | ✓ | ✓（→`concussion`） |  |
| 87 | `large` | 940 |  | ✓ | ✓ |  |
| 88 | `mekaniak` | 950 |  |  |  |  |
| 89 | `support` | 960 |  | ✓ | ✓ |  |
| 90 | `pindown` | 970 |  | ✓ | ✓ | ✓ |
| 91 | `blind` | 975 |  | ✓ | ✓ | ✓ |
| 92 | `vanguard` | 980 | 43 | ✓ | ✓ | ✓ |
| 93 | `camouflage` | 985 | 23 | ✓ | ✓ | ✓ |
| 94 | `sniper` | 990 | 11 | ✓ | ✓ | ✓ |
| 95 | `codex` | 1000 | 31 | ✓ | ✓ | ✓ |
| 96 | `unstable` | 1010 | 19 | ✓ | ✓ | ✓ |
| 97 | `armour` | 1020 | 5 | ✓ | ✓ | ✓ |
| 98 | `regeneration` | 1030 | 9 | ✓ | ✓ | ✓ |
| 99 | `artifice` | 1040 | 19 | ✓ | ✓ | ✓ |
| 100 | `remnant` | 1050 | 28 | ✓ | ✓ | ✓ |
| 101 | `markofkhorne` | 1060 |  | ✓ |  |  |
| 102 | `markofnurgle` | 1070 |  | ✓ |  |  |
| 103 | `markofslaanesh` | 1080 |  | ✓ |  |  |
| 104 | `markoftzeentch` | 1090 |  | ✓ |  |  |
| 105 | `daemonicallegiance` | 1100 |  |  |  |  |
| 106 | `strike` | 1110 | 32 | ✓ | ✓ | ✓ |
| 107 | `combo` | 1130 |  | ✓ | ✓ |  |
| 108 | `shuriken` | 1120 | 11 | ✓ | ✓ | ✓ |
| 109 | `waystone` | 1140 | 27 | ✓ | ✓ | ✓ |
| 110 | `synapse` | 1150 | 13 | ✓ | ✓ | ✓ |
| 111 | `longrange` | 1160 | 13 | ✓ | ✓ | ✓ |
| 112 | `markerlight` | 1170 |  | ✓ | ✓ | ✓ |
| 113 | `companion` | 1180 | 8 | ✓ | ✓ | ✓ |
| 114 | `pray` | 1185 | 23 | ✓ | ✓ | ✓ |
| 115 | `notdestroyremnant` | 1190 |  |  |  |  |
| 116 | `ambush` | 1200 | 12 | ✓ | ✓ | ✓ |
| 117 | `uprising` | 1210 | 9 | ✓ | ✓ | ✓ |
| 118 | `duty` | 1220 | 24 | ✓ | ✓ | ✓ |
| 119 | `regiment` | 1225 | 13 | ✓ | ✓ | ✓ |
| 120 | `teleport` | 1230 | 7 | ✓ | ✓ | ✓ |
| 121 | `agenda` | 1235 | 13 | ✓ | ✓ | ✓ |
| 122 | `stomp` | 1240 | 8 | ✓ | ✓ | ✓ |
| 123 | `stimulation` | 1245 | 10 | ✓ | ✓ | ✓ |
| 124 | `ecstasy` | 1250 | 10 | ✓ | ✓ | ✓ |
| 125 | `cruelty` | 1255 | 9 | ✓ | ✓ | ✓ |
| 126 | `huntmark` | 1260 |  | ✓ | ✓ | ✓ |
| 127 | `ferocity` | 1265 | 9 | ✓ | ✓ | ✓ |
| 128 | `dontreturnferocity` | 1270 |  |  |  |  |
| 129 | `oath` | 1275 | 38 | ✓ | ✓ | ✓ |
| 130 | `oathinallturns` | 1276 |  |  |  |  |
| 131 | `oathdouble` | 1277 | 1 |  |  |  |
| 132 | `oathtripleactivation` | 1278 |  |  |  |  |
| 133 | `sentry` | 1280 | 3 | ✓ | ✓ | ✓ |

**合计**：原版 `DefinedTrait`（除 `none`）= **133** · 原版数据真正用到 = **51** 个名字（**含哨兵 `none` 1 处 ⇒ 真 trait = 50 个**）·
`Card_Trait/*` 卡面印名 = **83** · 我们 `Prefixes` 认得出 **95** 个规范键 · `Implemented` **77** 个规范键 ·
按名字对齐到原版枚举：**我们有机制 70 / 133**（另有 `rage`/`frenzied`/`concussive` 三个同义拼法指向已实现的键）。

**这张表怎么读 / 三个结论：**

1. **识别层已经够用**。原版数据真正用到的 **51** 个名字（`CardTraits` 计数；去掉哨兵 `none` 是 **50** 个真 trait）里，我们 `Prefixes` 认出 **49**，
   剩 `oathDouble` 由**卡面正文匹配**接住（`CardDef.AddStaticBattleRule` 认 `friendly oath abilities apply an additional time`），
   `none` 是哨兵。**池内 `keywords` / `cardTraits` 两列的静默丢弃现读 = 0**（1128 张全扫）
   ⇒ 账里「`Requiem` 没实现」的正确读法是**行为层缺机制**（`Implemented` 里没有），**不是**识别层缺词
   （`Prefixes` 2026-10-26 之前就已经收了 `requiem`）。
2. **行为层 = 70 / 133**（另有 `rage` → `Penitence`、`frenzied` → `Destroyer`、`concussive` → `concussion` 三个**同义拼法**
   指向已实现的键）。剩下 63 个里，**45 个连名字都认不出**，但它们**全是隐藏 trait / 内部标记**
   （`activeAbility` / `passiveAbility` / `whileInPlayAbility` / `psyker` / `reactor` / `mutation` / `markOfX` …）——
   `Card_Trait/*`（83 条卡面印名）里**没有它们的名字**，登记进 `Prefixes` 只会给「未实现关键词」单与卡面徽章加噪声。
3. **真补的两笔**（本批写进代码）：
   · **7 个「卡面印得出、却认不出」的词** → `Prefixes`：`first strike`/`firststrike`(630) · `large`(940) ·
     `relentless`(450) · `resolution`(840) · `combo`(1130) · `support`(960) · `choose one`/`chooseone`(820)。
     ⚠️ 这 7 个**都没有机制**（不进 `Implemented`）⇒ 真出现时如实报「未实现关键词」＋ 卡面打 `*`，这才是对的。
     影响面实测：全池 `keywords`/`cardTraits` 两列静默丢弃 **改前改后都是 0**；`desc` 以这七个词开头的分句 **0 条**
     （`IsKeywordOnly` 要求**吃满整句**，故不会被误判成纯关键词声明）。
   · **2 个「机制早就在读、只是没登记 `Implemented`」的词**：`jam`（`ConditionEval.cs:501` · `EffectResolver.cs:11327` ·
     `RuleCore.cs:6022` · `RuleCore.cs:10458` 四处真读）· `sneakattack`（`CounterAttack.cs:49` 免反击）。
     ⚠️ **`terror` / `markofchaos` 故意不进**：`Implemented` 还被 `BattleDriver` 拿去过滤关键词外观
     （`BattleDriver.cs:27102/27111`），隐藏 trait 放进来会渲染**假徽章** —— 而 `Card_Trait/jam`、`Card_Trait/sneak`
     **都有官方本地化条目**（卡面印得出），`terror` / `markOfChaos` 那两个名字不是这两张脸。

### 2.3 任务 3 —— `HasWhileInPlayAdjacentEffect` 照原版逐字判

**原版判据**（`CardScript__HasWhileInPlayAdjacentEffect.c:1-44` ＋ 🥈 `cpp2il/CardScript.cs:6865-6919`，两份一致）：

1. `HasCurrentTrait(0x2f8 = 760 requiem)` ⇒ **true**；
2. 否则 `HasWhileInPlayAbility()` 为假 ⇒ **false**；
3. 否则逐条 `rawCardScript.cardAbilities(+0x290)`：`ability.targetCriteria(+0x30)` 非空 ∧ `targetsAffected(+0x10) == 100`
   ⇒ **true**；扫完没有 ⇒ **false**。
   🔴 读的是 **ability 级 `+0x30`**，⛔ 不是 logic 级 `AbilityLogic.targetCriteria(+0x28)`。

**新落点**：`UnitState.cs:2429`（`HasWhileInPlayAdjacentEffect(out why)`）＋ `UnitState.cs:2374`（`HasWhileInPlayAbility(out why)`，五支）
＋ `CardDef.cs:6297`（`AnyAbilityLevelCriteriaTargetsAffected`，逐行扫 ability 级 criteria、带 `why` 三态）。

**替代口径的撤除**：`AuraSpecs[i].Adjacent` **不再出现在这个谓词里**（它留在 `Aura.ApplyAura` 那一层，那是它的本职）。

**两读数（差分实验）**：

| 口径 | `HasWhileInPlayAdjacentEffect` 为真的卡 | 怎么读的 |
|---|---|---|
| **替代口径（改前）** | **41 张** | `ruleprobe layers` 的 `auras[].Adjacent == 1`：ASH31 · AM15 · AM72 · AM38 · AM28 · BL18 · BL27 · BL35 · DA27 · EC33 · EC15 · EC27 · GSC4 · GSC30 · GSC71 · SAU67 · SAU30 · SAU36 · SAU66 · GOF91 · GOF88 · GOF11 · GOF2 · GOF29 · SW19 · SW15 · SW18 · SW25 · SW38 · TAU28 · TAU32 · TAU29 · TAU19 · TAU24 · TAU20 · TAU3 · UM104 · UM18 · UM14 · UM107 · UM88 |
| **原版逐字（改后）** | **0 张（恒 false）** | 逐支现扫：`requiem` 卡 **0 张** · `jam` 卡 **0 张** · ability 级 `ta == 100` 的行 **0**（86 行分布 `{10:62, 30:8, 20:8, 90:7, 55:1}`）· 对照：**logic 级** `ta == 100` 也 **0** 行 ⇒ 即便改成读 logic 级，今天结果相同 |

⚠️ 这支谓词的**唯一消费者是 AI 布场**（`AI__ExecuteAction.c:60-63`），离线探针读不到「AI 落哪一格」⇒ 账仍 **C**（G3 ✗）。

---

## ③ 改了哪些文件、哪些行（新增行数）

| 文件 | 改动 | 行数 |
|---|---|---|
| `RuleEngine/Core/CardDef.cs` | ① `Prefixes` 里补 7 个「印得出却认不出」的词（＋11 行：9 条表项 ＋ 说明块）；② `Implemented` 里补 `Jam, SneakAttack`（＋8 行：2 个 token ＋ 说明块）；③ 类尾新增 `AnyAbilityLevelCriteriaTargetsAffected(int, out string)`（＋56 行，含判据注释块） | **+112 / −0** |
| `RuleEngine/Core/UnitState.cs` | 类尾新增 `HasWhileInPlayAbility(out string)` · `HasWhileInPlayAdjacentEffect(out string)` · `HasWhileInPlayAdjacentEffect()`（＋判据注释块） | **+123 / −0** |
| `RuleEngine/Core/Aura.cs` | 1 行**就地改写**：给「整份重算 vs 原版只比单位数」那条补 `🔶 自己设计（非原版）` 标记（**行数不变**，铁律 18·b） | **+1 / −1** |
| `RuleEngine/Core/Enchantment.cs` | **未改动** | 0 |
| `账/_parts/B8_D.tsv` | 表头 ＋ 14 行（6 A · 2 C · 6 B），LF、每行 11 字段 | 新建 |

- 行尾：四个 `.cs` 全部 **LF、裸 CR = 0**（`CardDef.cs` 8,404 行 · `UnitState.cs` 2,444 行 · `Aura.cs` 1,019 行 · `Enchantment.cs` 701 行）。
- `A1990` 守卫：本批**没有**新增 `new UnitState(` 入口 ⇒ 不需要白名单。
- 新增成员全部**追加到类尾**（`UnitState` 类尾 · `CardDef` 类尾）⇒ 下游 `.cs` 行号只在**尾后**变化。

---

## ④ 没查清的部分（逐条写清「差什么」）

1. **`StatMod` 缺 `trait` / `buffType` 两列** ⇒ `HasWhileInPlayAbility` 第 ⑥ 支（`activeEffects` 里 `trait==pack && buffType==1`）
   逐字判不了，本批按 false ＋ `why` 出声。**差的是**：把这两个字段加进 `StatMod`（`UnitState.cs` 白名单内，但要先决定
   由谁在 `AddStatMod` 的**所有**调用点填值 —— 现读 `_statMods` 的写入点不止一处）＋ `Auras.GrantTag` 要不要带来源卡。
2. **trait 列表查询族的「armour 归属」**（见 §⑤·1）：要判 `HasCurrentTrait(armour)` 在原版读哪张表，
   需要 `EntityScript.GetCurrentTraitValue` / `HasCurrentTrait` 的**完整方法体**（`EntityScript.cs:3116-3219` 那一族在
   🥇 `decomp_full` 里**没有独立 `.c`**，只有 🥈 cpp2il 版；`724 条无方法体清单` 的嫌疑未排除）。
3. **`IsKeyword` 的 4 张分歧**（BL1 · EC55/56/57）是**数据**问题还是**映射**问题，没定 —— 要 `gen_cards_engine.py` 那一侧
   说清 `subtype` 的来源（它在白名单外）。见 §⑤·2。
4. **AI 部署格**仍然没有离线读数口（`ruleprobe` 没有「跑一局 AI 并 dump 落格」的模式）⇒ 任务 3 的最终验收
   只能等调用点接线后由真机/一局 dump 收口（同 `账:1264` 那条的历史结论）。
5. **`+0x228` / `+0x249` / `+0x24b` / `+0x260` / `+0x270`**（`CardScript__Awake.c` 里清的那几个位）语义**未解**，
   所以 `Awake`（账行「CardScript.Awake()」）没升 A：它的可观测量（落场三围/关键词）实际验证的是 `CardSetup`，不是 `Awake`。

---

## ⑤ 顺手发现（报上来，**没改**）

1. 🔴 **`armour` 不在原版 `defaultTraits` 里** —— 这是本轮最硬的一条新账。
   `cards_raw.json`（1,351 张 SO）的 `CardTraits`（= 原版 `RawCardScript.defaultTraits +0x230`）**不含 armour**：
   实测 `ASH34 Avatar of Khaine` 的 `CardTraits = [talent]`，护甲在 SO 的**独立字段** `armour = 2`；
   而我们的 `keywords` 列把 `Armour N` 并进来了。⇒ **带 `Armour N` 的 79 张卡**（`Armour 1` 62 · `Armour 2` 17）
   在 `CountTraits` / `CheckIfTraitIsInList` / `FindTraitInList` / `GetDefaultTraitValue` 这一族上**两侧答案相反**
   （例 `ASH34`：原版 1 条 / 我们 2 条）。⇒ 那 4 条账均**保持 B**，差异已写进补丁。
   **要收口需先裁定**：「armour 算不算一条 trait」（我们的 `KwValue(armour)` 实际对应的是原版那个 **`armour` 字段**，
   不是 `defaultTraits` 里的一条）。
2. 🔴 **`IsKeyword` 有 4 张卡对不上**：`BL1`（SO `keyword=40 Daemon` vs 我们 `subtype='Infantry'`）·
   `EC55/EC56/EC57`（SO `keyword=5 Infantry` vs 我们 `subtype='Warlord'`）。其余 **585/589** 张 1:1。
   另有 SO `keyword==0 (none)` 的 **539 张**：原版 `IsKeyword(none)` 真 / 我们假（该实参实际无人使用）。
3. **`GetCurrentTraitValueWithModifiers` 的 `≥0` 夹紧**：原版方法体收尾有 `num5 < 0 ⇒ 0`，我们的 `KwValue` **不夹**
   ⇒ 「关键词值永不为负」这条全局不变量我们**只在个别读点**自己 `Math.Max(0,…)`。池内现读 **0 张**可达卡。
4. **`SpellType` 枚举名字本轮解出来了**（旧账写「210 附近缺失」）：`OffensiveCard = 200` · `DefensiveCard = 210`
   （`dump.cs:45900-45945`）⇒ 已在 A 行的 `判据出处` 里就地订正。
5. **`AddKeyword` 的旧落点行号过期**：旧账写 `UnitState.cs:954`，现读 954 是注释行、真声明在 **1,090**（已在新补丁里改正）。
6. **`game` 侧一处口径**：生成器把 SO 的 `cardType = 20 (Tactic)` **细分**成 `tactic`(406) / `defence`(39) / `enchantment`(1)
   —— 这 40 张的 `cardType` 仍是 20 ⇒ `IsHeroEquivalent` / `IsMinionOrAsset` 两侧都给 false，**不构成分歧**，
   但将来若有人写 `IsTactic()` 一类的谓词，必须以 `cardType == 20` 为判据、⛔ 别用 `type`。

---

## ⑥ 给调度台的请求

1. **派活给 `Data/SimpleAI.cs` 的写手（任务 3 的收尾，一行改动）**：把
   `SimpleAI.HasAdjacentEffect(CardDef c)`（`SimpleAI.cs:180-186`）改成调
   `u.HasWhileInPlayAdjacentEffect()`（`RuleEngine/Core/UnitState.cs:2429`），
   并把 `NextDeploySlot` 的扫描改调 `BoardSlots.NextSlotCheckingAdjacents`。
   ⛔ **不要**再把 `AuraSpecs[i].Adjacent` 当这个谓词的判据源。
2. **AI 布场影响面（接线前后的预测值）**：
   · 改前：**41 张**卡只要在场上，`NextDeploySlot` 的第 1/2 支就可能命中 ⇒ AI 会「贴着那张卡部署」；
   · 改后：**0 张** ⇒ 第 1/2 支永不命中，**落点永远走第 3 支（平衡支）**：
     两侧人数相等时**先放右**（原版 `leftCount < rightCount ? 左 : 右+1`），真实序列 = 右近 → 左近 → 右次 → 左次 …
   · ⇒ 行为变化是「AI 不再贴光环卡布场」，属**预期内**（原版就是如此，我们此前是替代口径）。
   · **建议**：接线后跑一次真机对局看 AI 布场，并考虑给 `ruleprobe` 加一个「AI 落格 dump」模式（这是 `账:1264` 的 G3 卡点）。
3. **账漂移预期**：`CardDef.cs` / `UnitState.cs` 的插入点分别在**类尾**与 `Prefixes`/`Implemented` 表内
   ⇒ 只有「落点在插入点之后」的账行可能漂。**本批实测**：`check_ledger_points` 对 `战斗逻辑驱动.tsv` 报
   **漂移 0 · 失败 2（两条都是既有的 `CardFeel.cs` 注释行 / 语句行落点，与本批无关）**；
   `Aura.cs` 是**就地改写一行**（行数不变）⇒ 零漂移。
   ⚠️ 合并本补丁后请再跑一次 `check_ledger_points`（新 A 行的落点已全部对齐到**声明行**）。
4. **`ruleprobe check` 基线不用重钉**：本批 `解析差异 0 行 · 哨兵 0`，卡池 md5 与基线不一致那条是**既有的**（改前就报）。
5. **新账（按铁律 5·b 提请立账，本批未改代码）**：
   · `armour` 是否算 trait（影响 79 张卡 × 4 条账，见 §⑤·1）；
   · `IsKeyword` 的 4 张数据分歧（BL1 / EC55 / EC56 / EC57，见 §⑤·2）；
   · `KwValue` 的 `≥0` 夹紧（见 §⑤·3）；
   · `StatMod` 补 `trait`/`buffType` 两列（`HasWhileInPlayAbility` 第 ⑥ 支的收口前提，见 §④·1）。
