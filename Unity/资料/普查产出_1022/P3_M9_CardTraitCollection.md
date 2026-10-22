# P3 / M9 —— `CardTraitCollection` 那批原版 SO 到底在不在本机

> 调查代理 P3（只读）。日期 2026-10-22。
> 对应缺口条目：`资料/普查产出_1022/W64_素材导出缺口.md` 的 **M9** 与 **§四 ②**。

---

## 〇、结论（一句话）

**在。而且一个字节都不缺。**

| 东西 | 位置 | 坐标 |
|---|---|---|
| `CardTraitCollection`（**1 个** SO，`m_Name = TraitsCollection`） | **`d:/2/Warhammer 40k Warpforge/Warpforge_Data/sharedassets0.assets`** | **pathID = 531**，`byte_start = 8385824`，`byte_size = 2416` |
| `CardTrait` 派生 SO（**73 个**） | 同上 | **pathID = 458 … 530**（连续 73 个） |
| 其中 `CardTrait` / `CardTraitDuty` / `CardTraitFerocity` / `CardTraitOath` | 同上 | 69 / 1 / 1 / 2 个（script pathID 1795 / 1586 / 3039 / 231） |
| 那 8 个 HUD 图标 Sprite | 同上（Sprite 对象，同一文件内） | pathID 87 / 277 / 269 / 73 / 274 / 104 / 302 / 93 |
| 77 个 trait 的图标 Sprite | 同上（fileID=0 自引用） | pathID 68–307 区间 |
| 67 个 `effectAnim` 目标（`CardAnim`） | 15 个 `*cardanims*` Addressables 包里 | GUID → 见 §四 |

🔴 **上一轮「`_traitEffects` / `effectAnim` / `overrideByArmies` / `playIfWarlord` / `_stackable` / `_aIScore` / `_traitIcon` 各 0 命中」是【假阴性】。**
错因：这批 SO **不在任何 Addressables bundle 里**，而在**裸 player 文件 `sharedassets0.assets`**；
而 `assets_full` 的导出器对 `sharedassets0.assets` 的 **188 个 MonoBehaviour 失败了 182 个**
（`_stats.json`：`"sharedassets0.assets": {"objects":591, "json":347, "fail":182}`；
`assets_full/sharedassets0/MonoBehaviour/` 里只剩 6 个 JSON，且 6 个全是 script pathID=499，与 trait 无关）
⇒ **那批对象根本没有被导出成 JSON，所以按字段名 grep 当然 0 命中。**

---

## 一、搜过哪里 / 怎么搜的 / 命中什么

| # | 搜过哪里 | 怎么搜 | 命中 |
|---|---|---|---|
| 1 | `d:/2/新解包资源/assets_full/`（84 bundle ＋ `globalgamemanagers` / `level0` / `level1` / `resources` / `sharedassets0` / `sharedassets1`，共 90 个目录） | `grep -rl '"m_ClassName": "(CardTrait\|CardTraitCollection\|TraitEffect\|CardTraitData\|TraitPairs\|DefinedTrait)"'` | **4 个 MonoScript 桩**，全在 `globalgamemanagers/MonoScript/`：`MonoScript_1795.json`(CardTrait) · `MonoScript_3970.json`(CardTraitCollection) · `MonoScript_4115.json`(TraitEffect) · `MonoScript_4371.json`(CardTraitData) |
| 1b | 同上，`sharedassets0/MonoBehaviour/` | `ls` | **只有 6 个 JSON**（188 个 MonoBehaviour 里 182 个导出失败）⇒ 这里是假阴性源头 |
| 2 | `d:/2/Warhammer 40k Warpforge/Warpforge_Data/globalgamemanagers.assets` | UnityPy 1.24.2 枚举全部 `MonoScript`，按 `m_ClassName` 过滤 | **4453 个 MonoScript**；trait 系 8 个：`CardTrait`=**1795** · `CardTraitCollection`=**3970** · `TraitEffect`=**4115** · `CardTraitData`=**4371** · `CardTraitOath`=**231** · `CardTraitDuty`=**1586** · `CardTraitFerocity`=**3039** · `BoardTraitIcon`=**3671** |
| 3 | `d:/2/Warhammer 40k Warpforge/Warpforge_Data/` 的全部裸文件：`resources.assets`(616 MB) · `sharedassets0.assets`(188 MB) · `sharedassets1.assets`(0 MB) · `level0`(147 MB) · `level1`(2 MB) · `globalgamemanagers.assets`(29 MB) | ⚠️ 这批 MonoBehaviour **没有 type tree，`obj.read()` 必失败**（`ValueError: Expected to read 2416 bytes, but only read 48 bytes`）⇒ 改为**直接读原始字节**解析 `m_Script` PPtr（布局见 §三） | ✅ **`sharedassets0.assets` 命中 75 个**：CardTraitCollection ×1（531）、CardTrait ×69、CardTraitDuty ×1、CardTraitFerocity ×1、CardTraitOath ×2。其余 5 个文件 **0 命中** |
| 4 | 原版安装目录 84 个 `*.bundle`（`StreamingAssets/aa/StandaloneWindows64/`） | UnityPy 逐个包遍历 **66,459 个 MonoBehaviour**，读 `m_Script` PPtr，把 pathID 反查 `Waprforge_monoscripts.bundle`（CAB-17b1b5f865c595045b5fe42b7f8c8967）里 **722 个 MonoScript** 的 `m_ClassName` | **66,459 个里 0 个 trait SO**。718 个不同脚本类，`Trait` 系只有 `BoardTraitIcon`(7 次) 与 `EverguildTraitTooltipItem`(1 次) ⇒ **SO 确实不在任何 bundle 里** |
| 5 | Addressables 目录 `StreamingAssets/aa/catalog.bin`（1,305,148 B） | 正则抽 ASCII 串（12,363 条），按关键字过滤 | `CardTrait` **0** · `TraitCollection` **0** · `Unique` **0**；`trait` 只有 3 条，全是 `atlasindividual_assets_40ktraiticonatlas.bundle`。✅ 对照组：`CardFramesSO` / `EnviromentalEffectCardsSO` / `PrebuiltPack` **各 1 命中** ⇒ **catalog 确实会收录资产名，0 命中是有意义的** |
| 6 | `d:/2/解包整理/`（旧解包） | `find -iname "*trait*"` | 867 个文件，**全是 `CardAnim` / 音频 cue / Sprite**（`ArmourTraitStart` / `ArtificeTraitTrigger` / `BlindTraitPlay` …），**没有 `CardTrait*` SO**，也没有 `sharedassets0` 的游戏数据 dump |
| 7 | `D:/2/关服前存档/新建文件夹/Everguild_Warpforge/`（关服前 Unity 缓存，7 个文件 = 3×`__info` + 3×`__data`） | 文件名 `find -iname "*trait*"` = 0；3 个 `__data` 全是 `UnityFS` 头（531,962 / 69,582 / 10,764,942 B），用 UnityPy 打开枚举 `m_Name` | 531,962 B 那包 = **1,359 个 MonoBehaviour（= `allcards`，即 2026-10-18 找到的那份）**，trait 名只有 `T6 BlackLegion-Traitors Hate`（卡名）；另两包（2 Sprite + 2 Texture2D / 2 MonoBehaviour）无 trait ⇒ **不含 trait SO** |
| 8 | `d:/2/unity_run_ref/Warpforge_Data/` 与 `d:/2/10.13游戏/Warpforge_Data/`（同机另两份原版） | `md5sum sharedassets0.assets` | **三份完全同一文件**：`3b87529c8a3bda945a00b8b2ddf82202`（8,407,132 B）⇒ 结论对三份都成立 |
| 9 | 反编译 `d:/2/tools/decomp_full/` | `grep CardTraitCollection` / 读 `AssetLocator__GetUniqueAsset_object_.c` · `PlayerDataManager__Awake.c` | 加载链确认：`PlayerDataManager.Awake` → `AssetLocator.GetUniqueAsset<CardTraitCollection>()` → `CardTraitCollection.Init(obj)`（`PlayerDataManager__Awake.c:78-80`）。**它是"唯一资产"型 SO，不是 `Resources.Load`** |

---

## 二、为什么 `sharedassets0.assets` 才是源（结构性解释）

- 它是 **`level0`（启动场景）的 shared assets**，与 `globalgamemanagers.assets` 一起属于 **player 自带的那一层**，不经过 Addressables。
- 所以 **`catalog.bin` 里查不到它**（§一 #5 的 0 命中是正常的、也是自洽的）。
- `sharedassets0.assets` 里 `PreloadData`(pathID=1) 有 291 个预载资产 —— **不含 531**（实测 `m_PathID==531` 无命中），
  说明 531 是由别的路径持引用（脚本里那句 `AssetLocator.GetUniqueAsset` 找的是"唯一资产"，
  其实现是 `System.Linq.Enumerable.FirstOrDefault(locations, x => x.GetType() == typeof(T))`，
  见 `AssetLocator__GetUniqueAsset_object_.c:69` 与 `AssetLocator.__c__3_object____GetUniqueAsset_b__3_0.c:8-14`）。
  **这条加载链的最后一跳本报告没查到**（不在本次任务范围内），但不影响"数据在本地"这个结论。

---

## 三、二进制布局（**手写解析器已逐对象校验：73/73 个 `bytesRead == byte_size`**）

Unity 6（`6000.2f2`）的 MonoBehaviour 头 28 字节，**PPtr 是 `int32 fileID` 紧跟 `int64 pathID`（中间无填充）**：

```
+0    PPtr  m_GameObject        (int32 fileID + int64 pathID)  = 12 B
+12   int32 m_Enabled
+16   PPtr  m_Script            (int32 + int64)                = 12 B   ← 与 globalgamemanagers.assets 的 MonoScript pathID 对表
+28   string m_Name             (int32 len + bytes + 4 字节对齐)
```

> 校验法：用 `cosmeticsso_assets_all.bundle` 里能正常 `read()` 的对象反推 ——
> 实测 `m_Script.m_FileID` 在 +16、`m_Script.m_PathID` 在 +20，与上面的布局一致。

`CardTraitCollection`（对象 531）在 `m_Name` 之后：

```
List<TraitPairs> TraitCollection     (int32 count + count × TraitPairs)
TraitPairs = string name; int32 traitType(DefinedTrait); PPtr traitConfig(→CardTrait, fileID=0 同文件)
Sprite sabotageSprite / faithSprite / ferocitySprite / oathSprite /
       questPointsSprite / spiritStoneSprite / meleeAttackSprite / rangeAttackSprite   (8 × PPtr)
```

`CardTrait`（及 3 个子类，字段布局**完全相同** —— 子类只有重写的 `IsActive`，无新字段）：

```
Sprite _traitIcon                  (PPtr)
bool   _hasInPlayAnim   / _hasIcon / _isInPlayIcon   (每个 1 B + 3 B 填充)
int32  _stackable        (TraitStackType: replace=0 / add=10 / count=20)
float  _aIScore
AssetReferenceTyped<Object> customObject   (string guid + string subObjectName + string subObjectType)
List<TraitEffect> _traitEffects
   TraitEffect = string name;
                 AssetReferenceTyped<CardAnim> effectAnim  (同 3 string)
                 TraitEffectOverrideByArmy[] overrideByArmies   (int32 army + 同 3 string)×N
                 int32 TriggerType (EffectTriggerType)
                 bool  playIfWarlord (1 B + 3 B 填充)
```

> 校验证据：`Agenda`(458) 解析出 `28+12+12+12+4+4+4+12+4+20+36+4+4+4+4+4 = 164` = 对象 `byte_size` 164 ✅；
> `Invulnerable`(484) 带 6 条 `overrideByArmies`，解析出 448 = `byte_size` 448 ✅；
> `Shield`(511) 带 4 条，348 = 348 ✅。

---

## 四、解析结果（真值表）

### 4.1 `TraitsCollection` 的 77 条 `TraitPairs`

- 其中 **2 条的 `traitConfig` 是 null**：`berzerk`(360) / `dodge`(240) —— 原版**就是没配**，不是我们漏读。
- 其余 **75 条指向 73 个不同对象**（`oathDouble` / `oathInAllTurns` / `oathTripleActivation` 三条共用 `OathAdditionalEffect`=498）。
- `_aIScore` **73 个全是 0.0**；`_stackable` 只有 `MarkOfChaos` = **20**(count)，其余 72 个 = **10**(add)。
- `TriggerType` 数字 → 枚举（`CardTrait.EffectTriggerType`）：0=Start · 1=End · 2=OnPlay · 3=Trigger · 4=OnHand · 5=FromCode · 6=TriggerOnPlay。

| # | TraitPairs.name | DefinedTrait | CardTrait obj | CardTrait.m_Name | 图标 Sprite | stackable | traitEffects（name / TriggerType / playIfWarlord） |
|---|---|---|---|---|---|---|---|
| 1 | `agenda` | 1235 | 458 | Agenda | Atlas_trait_icon_agenda | 10 | TriggerOnPlay/6/T |
| 2 | `ambush` | 1200 | 459 | Ambush | Atlas_trait_icon_ambush | 10 | End/1/T |
| 3 | `armour` | 1020 | 460 | Armour | Atlas_trait_icon_armour | 10 | Start/0/F |
| 4 | `artifice` | 1040 | 461 | Artifice | Atlas_trait_icon_artifice | 10 | Trigger/3/T |
| 5 | `backlash` | 120 | 462 | Backlash | Atlas_trait_icon_backlash | 10 | Trigger/3/T |
| 6 | `berzerk` | 360 | **null** | — | — | — | — |
| 7 | `blast` | 350 | 463 | Blast | Atlas_trait_icon_blast | 10 | （空） |
| 8 | `blind` | 975 | 464 | Blind | Atlas_trait_icon_blind | 10 | OnPlay/2/T |
| 9 | `bloodThirst` | 220 | 465 | BloodThirst | Atlas_trait_icon_bloodThirst | 10 | Trigger/3/T ; TriggerOnPlay/6/T ; End/1/T |
| 10 | `camouflage` | 985 | 466 | Camouflage | Atlas_trait_icon_camouflage | 10 | OnPlay/2/T |
| 11 | `cantAttack` | 150 | 467 | CantAttack | Atlas_trait_icon_cantAttack | 10 | Start/0/T |
| 12 | `codex` | 1000 | 468 | Codex | Atlas_trait_icon_codex | 10 | Trigger/3/T |
| 13 | `combo` | 1130 | 469 | Combo | （null） | 10 | OnPlay/2/F |
| 14 | `companion` | 1180 | 470 | Companion | Atlas_trait_icon_companion | 10 | Start/0/F |
| 15 | `concussive` | 930 | 471 | Concussive | Atlas_trait_icon_concussive | 10 | Trigger/3/T |
| 16 | `cruelty` | 1255 | 472 | Cruelty | Atlas_trait_icon_Cruelty | 10 | Trigger/3/T |
| 17 | `daemonicAllegiance` | 1100 | 473 | DaemonicAllegiance | （null） | 10 | OnPlay/2/T |
| 18 | `dodge` | 240 | **null** | — | — | — | — |
| 19 | `dontReturnFerocity` | 1270 | 496 | NotFerocity | Atlas_trait_icon_ferocity | 10 | （空） |
| 20 | `duty` | 1220 | 474 | Duty | Atlas_trait_icon_duty | 10 | TriggerOnPlay/6/T |
| 21 | `ecstasy` | 1250 | 475 | Ecstasy | Atlas_trait_icon_Ecstasy | 10 | TriggerOnPlay/6/F |
| 22 | `ephemeral` | 5 | 476 | Ephemeral | Atlas_trait_icon_ephemeral | 10 | （空） |
| 23 | `fast` | 40 | 477 | Fast | Atlas_trait_icon_fast | 10 | Start/0/T |
| 24 | `ferocity` | 1265 | 478 | Ferocity | Atlas_trait_icon_ferocity | 10 | Start/0/F ; FromCode/5/F |
| 25 | `firstStrike` | 630 | 479 | FirstStrike | （null） | 10 | Start/0/T |
| 26 | `flank` | 460 | 480 | Flank | Atlas_trait_icon_flank | 10 | Start/0/T |
| 27 | `flying` | 390 | 481 | Flying | Atlas_trait_icon_flying | 10 | OnPlay/2/T |
| 28 | `frenzied` | 680 | 482 | Frenzied | Atlas_trait_icon_frenzied | 10 | Trigger/3/T |
| 29 | `huntMark` | 1260 | 483 | HuntMark | Atlas_trait_icon_huntMark | 10 | TriggerOnPlay/6/T ; Start/0/T ; OnPlay/2/T ; FromCode/5/T ; End/1/T |
| 30 | `invulnerable` | 310 | 484 | Invulnerable | Atlas_trait_icon_invulnerable | 10 | OnPlay/2/T |
| 31 | `large` | 940 | 485 | Large | （null） | 10 | Trigger/3/T |
| 32 | `longrange` | 1160 | 486 | LongRange | Atlas_trait_icon_longrange | 10 | FromCode/5/T ; End/1/T |
| 33 | `markOfChaos` | 330 | 487 | MarkOfChaos | Atlas_trait_icon_markOfChaos | **20** | （空） |
| 34 | `markOfKhorne` | 1060 | 488 | MarkOfKhorne | Atlas_trait_icon_markOfKhorne | 10 | OnPlay/2/T |
| 35 | `markOfNurgle` | 1070 | 489 | MarkOfNurgle | Atlas_trait_icon_markOfNurgle | 10 | OnPlay/2/T |
| 36 | `markOfSlaanesh` | 1080 | 490 | MarkOfSlaanesh | Atlas_trait_icon_markOfSlaanesh | 10 | OnPlay/2/T |
| 37 | `markOfTzeentch` | 1090 | 491 | MarkOfTzeentch | Atlas_trait_icon_markOfTzeentch | 10 | OnPlay/2/T |
| 38 | `markerlight` | 1170 | 492 | Markerlight | Atlas_trait_icon_markerlight | 10 | Start/0/T ; OnPlay/2/T ; FromCode/5/T ; End/1/T |
| 39 | `mekaniak` | 950 | 493 | Mekaniak | （null） | 10 | Trigger/3/T |
| 40 | `mob` | 920 | 494 | Mob | Atlas_trait_icon_mob | 10 | Trigger/3/T |
| 41 | `notDestroyRemnant` | 1190 | 495 | NotDestroyRemnant | Atlas_trait_icon_flying | 10 | （空） |
| 42 | `oath` | 1275 | 497 | Oath | Atlas_trait_icon_oath | 10 | Trigger/3/F |
| 43 | `oathDouble` | 1277 | 498 | OathAdditionalEffect | Atlas_trait_icon_oath | 10 | （空） |
| 44 | `oathInAllTurns` | 1276 | 498 | OathAdditionalEffect | Atlas_trait_icon_oath | 10 | （空） |
| 45 | `oathTripleActivation` | 1278 | 498 | OathAdditionalEffect | Atlas_trait_icon_oath | 10 | （空） |
| 46 | `pack` | 610 | 499 | Pack | Atlas_trait_icon_pack | 10 | Start/0/T |
| 47 | `piercing` | 900 | 500 | Piercing | （null） | 10 | Start/0/T |
| 48 | `pindown` | 970 | 501 | PinDown | Atlas_trait_icon_pindown | 10 | OnPlay/2/T |
| 49 | `pray` | 1185 | 502 | Pray | Atlas_trait_icon_pray | 10 | Trigger/3/T ; TriggerOnPlay/6/T |
| 50 | `rage` | 280 | 503 | Rage | Atlas_trait_icon_rage | 10 | （空） |
| 51 | `rally` | 10 | 504 | Rally | Atlas_trait_icon_rally | 10 | Start/0/T |
| 52 | `regeneration` | 1030 | 505 | Regeneration | Atlas_trait_icon_regeneration | 10 | Trigger/3/T |
| 53 | `regiment` | 1225 | 506 | Regiment | Atlas_trait_icon_regiment | 10 | Trigger/3/T |
| 54 | `relentless` | 450 | 507 | Relentless | Atlas_trait_icon_relentless | 10 | Trigger/3/T |
| 55 | `remnant` | 1050 | 508 | Remnant | Atlas_trait_icon_remnant | 10 | （空） |
| 56 | `resolution` | 840 | 509 | Resolution | Atlas_trait_icon_resolution | 10 | Trigger/3/T |
| 57 | `sentry` | 1280 | 510 | Sentry | Atlas_trait_icon_sentry | 10 | （空） |
| 58 | `shield` | 80 | 511 | Shield | Atlas_trait_icon_shield | 10 | OnPlay/2/T |
| 59 | `shuriken` | 1120 | 512 | Shuriken | Atlas_trait_icon_shuriken | 10 | FromCode/5/F |
| 60 | `slay` | 260 | 513 | Slay | Atlas_trait_icon_slay | 10 | Trigger/3/T |
| 61 | `sniper` | 990 | 514 | Sniper | Atlas_trait_icon_sniper | 10 | FromCode/5/T |
| 62 | `stealth` | 70 | 515 | Stealth | Atlas_trait_icon_stealth | 10 | OnPlay/2/T |
| 63 | `stimulation` | 1245 | 516 | Stimulation | Atlas_trait_icon_stimulation | 10 | Trigger/3/T |
| 64 | `stomp` | 1240 | 517 | Stomp | Atlas_trait_icon_stomp | 10 | FromCode/5/T ; Trigger/3/T |
| 65 | `strike` | 1110 | 518 | Strike | Atlas_trait_icon_strike | 10 | Trigger/3/T |
| 66 | `stun` | 100 | 519 | Stun | Atlas_trait_icon_stun | 10 | OnPlay/2/T |
| 67 | `swarm` | 650 | 520 | Swarm | Atlas_trait_icon_swarm | 10 | FromCode/5/T |
| 68 | `synapse` | 1150 | 521 | Synapse | Atlas_trait_icon_synapse | 10 | FromCode/5/F |
| 69 | `talent` | 845 | 522 | Talent | Atlas_trait_icon_talent | 10 | Trigger/3/F |
| 70 | `teleport` | 1230 | 523 | Teleport | Atlas_trait_icon_teleport | 10 | FromCode/5/F |
| 71 | `tide` | 910 | 524 | Tide | Atlas_trait_icon_tide | 10 | Start/0/T |
| 72 | `unstable` | 1010 | 525 | Unstable | Atlas_trait_icon_unstable | 10 | End/1/F |
| 73 | `unstoppable` | 180 | 526 | Unstoppable | （null） | 10 | （空） |
| 74 | `uprising` | 1210 | 527 | Uprising | Atlas_trait_icon_uprising | 10 | TriggerOnPlay/6/T |
| 75 | `vanguard` | 980 | 528 | Vanguard | Atlas_trait_icon_vanguard | 10 | OnPlay/2/T |
| 76 | `vulnerable` | 480 | 529 | Vulnerable | Atlas_trait_icon_vulnerable | 10 | OnPlay/2/T |
| 77 | `waystone` | 1140 | 530 | Waystone | Atlas_trait_icon_waystone | 10 | （空） |

> 「图标 Sprite」= `CardTrait._traitIcon` 指向同文件里的 Sprite 对象；
> **7 个是 null**：`combo` / `daemonicAllegiance` / `firstStrike` / `large` / `mekaniak` / `piercing` / `unstoppable`
> —— 原版就是空的（`(0,0)`），不是读错。

### 4.2 `CardTraitCollection` 自带的 8 个 Sprite（不是 trait 图标，是资源/HUD 图标）

| 字段 | Sprite 对象 | 名字 | `textureRect` |
|---|---|---|---|
| `sabotageSprite` | 87 | `Atlas_trait_icon_sabotage` | 80×80 @ (168,504) |
| `faithSprite` | 277 | `Atlas_trait_icon_faith` | 80×80 @ (84,168) |
| `ferocitySprite` | 269 | `Atlas_trait_icon_ferocity` | 80×80 @ (756,336) |
| `oathSprite` | 73 | `Atlas_trait_icon_oath` | 80×80 @ (672,84) |
| `questPointsSprite` | 274 | `Atlas_trait_icon_questPoints` | 80×80 @ (252,168) |
| `spiritStoneSprite` | 104 | `SpiritStone_empty` | 80×80 @ (0,0) |
| `meleeAttackSprite` | 302 | `Atlas_trait_icon_Melee` | 80×80 @ (84,420) |
| `rangeAttackSprite` | 93 | `Atlas_trait_icon_Ranged` | 80×80 @ (84,0) |

（除 `SpiritStone_empty` 用 texPPtr (0,5) 外，其余 7 个都指向**同一张图集纹理** texPPtr (0,37)。
图集本体在 `atlasindividual_assets_40ktraiticonatlas.bundle`，该包共 **78 个 `Atlas_trait_icon_*` Sprite** —— 已在 `assets_full` 里正常导出。）

### 4.3 `effectAnim` 的 67 个 GUID → `CardAnim` 名（**全部 67/67 解析成功**）

> 解析法 = 打开 15 个 `*cardanims*.bundle`，读每个 CAB 的 `container` 键（= Addressables 的 GUID 键）→ pathID → 对象 `m_Name`。

| GUID | CardAnim 名 | 所在 bundle |
|---|---|---|
| `4ae02eb9e870e964db52a85ed290696a` | Ambush Effect | genestalercultscardanims |
| `a4088dc3e336f124e9f555510b71d9c6` | ArmourTraitStart | cardanimsgeneral |
| `13a7b7da1fdb9404fbe5ccbc57514c6e` | ArtificeTraitTrigger | cardanimsgeneral |
| `42d0bc7d68e0230499c668e9aab43e59` | BacklashTraitTrigger | cardanimsgeneral |
| `ddaaf29c84bb8f842b8422885348e0fd` | BlindTraitPlay | cardanimsgeneral |
| `874dd68077c21cc4c86c5362b436d792` | BloodThirstGainAnim | cardanimsgeneral |
| `5a60dbe6e84c80742bf4f9c74b0502ed` | BloodThirstIdleAnim | cardanimsgeneral |
| `2b63fd560f1162e468349038c37ad642` | BloodThirstLoseAnim | cardanimsgeneral |
| `04b0c6e3f3eeb4b409368ee67804bcb5` | BluntImpact_Stomp_Down | cardanimsgeneral |
| `f91c06ce089539140a767ae1c30f08fb` | BluntImpact_Stomp_Forward | cardanimsgeneral |
| `cd3972f02c5f6c945b70ac4e34e5c1e7` | CamouflageTraitPlay | cardanimsgeneral |
| `31da34149fe75bb44a5acb577b0b0187` | CantAttackAnim | cardanimsgeneral |
| `9869f9d036ce2854ab95004fec1a7598` | CodexTraitTrigger | cardanimsgeneral |
| `3fce8ddee18e6944d9fdeef1b2199bbd` | CompanionTraitStart | cardanimsgeneral |
| `20e1db36c752eb5409e67119f4433f1a` | ConcussiveTraitTrigger | cardanimsgeneral |
| `9c0734f33ace0db44acabaf75e11f588` | CrueltyTraitTrigger | cardanimsgeneral |
| `a8c3f891601e9ca4c9e3b7c8cd0f37a3` | DutyAnim | cardanimsgeneral |
| `42abf1e9d0e87d64b8e8e91ba150ad9b` | EcstasyTraitTrigger | cardanimsgeneral |
| `cc61d807ec8234b3082ec764f07b4808` | Eldar_Shuriken_Short_Trait | aeldarisaimhanncardanims |
| `6144c1d442cbb964a85e6501399f07f2` | FastTraitStart | cardanimsgeneral |
| `fa13471fa8de4d74e83822e2bd49f794` | Ferocity Recall to deck | spacemarinesspacewolvescardanims |
| `0e5d56145ea269844837cb0bedeb24eb` | FerocityAnim | cardanimsgeneral |
| `8c379c5db12284b4b8d7353fa7f00006` | FlankTraitStart | cardanimsgeneral |
| `a81f347069299044481251a67e683037` | FlyingTraitPlay | cardanimsgeneral |
| `6a3ebaf28b9d7c74d9377de9ff5588fd` | FrenziedTraitTrigger | cardanimsgeneral |
| `e8dd2159956272b488aefe1901a216d6` | HuntMark Damage Anim | cardanimsgeneral |
| `8569ddf9cb908cd43a21b7d6d7f082c2` | HuntMark End Anim | cardanimsgeneral |
| `b664ea5d6484f83438a29fce3afdb989` | HuntMark Idle Anim | cardanimsgeneral |
| `95778df2378ded94da0aafc793216540` | HuntMark Stack Anim | cardanimsgeneral |
| `0a6f7af3797f19f459318feeabaf7a79` | HuntMark Start Anim | cardanimsgeneral |
| `ea9551018eef070429f258bf5da49f19` | InvulnerableBLTraitPlay | cardanimsgeneral |
| `841e49ce1bb1ab6409d1b0ca2b10308c` | InvulnerableGSCTraitPlay | cardanimsgeneral |
| `08b76498400033843b155abb260f5427` | InvulnerableGuardTraitPlay | cardanimsgeneral |
| `51fabe2a07ec80a4f9e60fd88f549f0e` | InvulnerableLeviathanTraitPlay | cardanimsgeneral |
| `e8e0e89495f211d4aae68ed1498feecb` | InvulnerableTAUTraitPlay | cardanimsgeneral |
| `5d246ebfe1414934cbc3df1e1c940b44` | InvulnerableTraitPlay | cardanimsgeneral |
| `8249c79fec4d44c4ca41755045dc863d` | Invulnerable_SW_TraitPlay | cardanimsgeneral |
| `64178449148019f43b9980567056dcab` | LongRangeAnim | cardanimsgeneral |
| `7e8ca71a32aa8e043af3748e0869c5ea` | LongRangeTraitEnd | cardanimsgeneral |
| `abe2e28b387aa9d418346dcae6475ce0` | MarkOfKhorneTraitPlay | cardanimsgeneral |
| `e3bd6529302157840aaa1f130fe125c3` | MarkOfNurgleTraitPlay | cardanimsgeneral |
| `1918dba5fd658384a91c9ed1610e9f8c` | MarkOfSlaaneshTraitPlay | cardanimsgeneral |
| `0e6be8dea74038b4dbbe54115b291aff` | MarkOfTzeenchTraitPlay | cardanimsgeneral |
| `e22d23a8ffdb4804eaab9370247c3fea` | MarkerlightTraitEnd | cardanimsgeneral |
| `047f05d1fc1185148b0efc43f02514d3` | MarkerlightTraitFromCode | cardanimsgeneral |
| `7ed23fc3db385bf4ba9621335bb0f266` | MarkerlightTraitPlay | cardanimsgeneral |
| `3bdfa049840f62644b563575bd42623b` | MarkerlightTraitStart | cardanimsgeneral |
| `2a21bd1096e20a74ab579989ab5fafff` | MobTraitTrigger | cardanimsgeneral |
| `e88772cb08fb73243970183f0f1dff7a` | Oath_Effect | cardanimsgeneral |
| `098d35eb98259de479dae6ccace2b777` | Pack OnPlay Anim | cardanimsgeneral |
| `58c04a0188df8f7408469d58740ffb27` | PinDownTraitPlay | cardanimsgeneral |
| `3a47c27d3826b534a9014c70433e7b5e` | Pray_Idle | cardanimsgeneral |
| `96ce731bac5181e4fa9516b352fb493f` | Pray_Start | cardanimsgeneral |
| `f21e512ab071d36479a4cbfda6edc6a9` | RallyTraitStart | cardanimsgeneral |
| `3f3a5ba316ce0ab46baf23c3a1ea9049` | RegenerationTraitTrigger | cardanimsgeneral |
| `d65e4c8cadde4b646a2815741bfdcf71` | RegimentAnim | cardanimsgeneral |
| `e162a1563a8acb94793b8a61be2a2bb6` | RelentlessTraitTrigger | cardanimsgeneral |
| `50b0ea0f4948557479de1e3db6c3d27e` | Sentry_Effect | cardanimsgeneral |
| `bba49b40661864f4c9a214ff1d282abe` | ShieldTraitECPlay | cardanimsgeneral |
| `3fde447d811e17743b8adea2ee0fd763` | ShieldTraitNecronPlay | cardanimsgeneral |
| `66b7b2b312de4914f8f788ed19f62f3f` | ShieldTraitPlay | cardanimsgeneral |
| `29f81690e299042488ae8032665f6f98` | ShieldTraitSororitasPlay | cardanimsgeneral |
| `85b4a93db566e5b459c337cd5e04232a` | ShieldTraitTAUPlay | cardanimsgeneral |
| `7d2ab14978e66a242bfd8ed3d85b3f1c` | ShurikenTraitFromCode | cardanimsgeneral |
| `7dc58e5b008d8804f8fe911400c9bd53` | Slay_Effect_target | cardanimsgeneral |
| `254794f776f20c7438ffee13c81bc945` | SniperTraitFromCode | cardanimsgeneral |
| `beab83fd5c9157642993c8febd915121` | StealthTraitPlay | cardanimsgeneral |
| `06c5534784474f440bfee6dcbf08360c` | StimulateTraitTrigger | cardanimsgeneral |
| `3d21ccdce390e1946a75a59167e6ba67` | StrikeTraitTrigger | cardanimsgeneral |
| `be723eb2aec4c5241a42fd4ae51a895a` | StunTraitPlay | cardanimsgeneral |
| `f387e5894b071f14bb79a0a59c5f87d9` | SwarmTraitFromCode | cardanimsgeneral |
| `f587b0eb3d6b66144b0363cd1a1d7e41` | SynapseTraitFromCode | cardanimsgeneral |
| `a36c3ce6d808dd84992bcf005a2fff22` | TalentTraitTrigger | cardanimsgeneral |
| `50728df9730796d48aedea73047a2f1d` | TeleportCardAnim | spacemarinesdarkangelscardanims |
| `5af24b17bf73eff498d5d3807dd2994c` | TideTraitStart | cardanimsgeneral |
| `5d7fa4738d00d154795eccdd93365c44` | UnstableTraitEnd | cardanimsgeneral |
| `0c1819e921598dd4d80b9fb8c79310a7` | Uprising Effect | genestalercultscardanims |
| `45746aadb340e664b87453c64c0518c8` | VanguardIdleAnim | cardanimsgeneral |
| `9bb41fe7da6e6614dbdd1c5bd2e31e31` | VulnerableTraitPlay | cardanimsgeneral |

另：**`overrideByArmies` 只出现在 2 个 trait 上**（其余 71 个数组为空）——
`Invulnerable`(484) 6 条 / `Shield`(511) 4 条，`CardArmy` 值见 `CardArmy.cs:3-16`：

| trait | army | CardArmy 名 | guid → CardAnim |
|---|---|---|---|
| Invulnerable | 50 | BlackLegion | `ea9551…` → InvulnerableBLTraitPlay |
| Invulnerable | 70 | TauEmpire | `e8e0e8…` → InvulnerableTAUTraitPlay |
| Invulnerable | 60 | Leviathan | `51fabe…` → InvulnerableLeviathanTraitPlay |
| Invulnerable | 90 | Genestealers | `841e49…` → InvulnerableGSCTraitPlay |
| Invulnerable | 100 | AstraMilitarum | `08b764…` → InvulnerableGuardTraitPlay |
| Invulnerable | 130 | SpaceWolves | `8249c7…` → Invulnerable_SW_TraitPlay |
| Shield | 70 | TauEmpire | `85b4a9…` → ShieldTraitTAUPlay |
| Shield | 40 | Sautekh | `3fde44…` → ShieldTraitNecronPlay |
| Shield | 80 | Sororitas | `29f816…` → ShieldTraitSororitasPlay |
| Shield | 120 | EmperorsChildren | `bba49b…` → ShieldTraitECPlay |

---

## 五、取法（完整可跑脚本，已在本机跑通）

> ⚠️ 这批对象**没有 type tree**，UnityPy 的 `obj.read()` 会在 `read_typetree` 里
> 抛 `ValueError: Expected to read 2416 bytes, but only read 48 bytes`。
> **必须走原始字节手工解析**（布局见 §三）。下面这份脚本 **73/73 个对象都通过了 `bytesRead == byte_size` 断言**。

```python
# 运行： "D:/2/Warpforge_tools/py312/python.exe" -X utf8 -I <本文件>
import UnityPy, struct, json, os

P  = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/sharedassets0.assets"
AA = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"

env = UnityPy.load(P)
raws, types = {}, {}
for obj in env.objects:
    types[obj.path_id] = obj.type.name
    if obj.type.name != "MonoBehaviour":
        continue
    sf = obj.assets_file; r = sf.reader; pos = r.Position
    try:
        r.Position = obj.byte_start
        raws[obj.path_id] = r.read(obj.byte_size)
    finally:
        r.Position = pos

class R:
    def __init__(s, b): s.b = b; s.p = 0
    def i32(s):
        s.p += (-s.p) % 4; v = struct.unpack_from('<i', s.b, s.p)[0]; s.p += 4; return v
    def i64(s):
        v = struct.unpack_from('<q', s.b, s.p)[0]; s.p += 8; return v
    def f32(s):
        s.p += (-s.p) % 4; v = struct.unpack_from('<f', s.b, s.p)[0]; s.p += 4; return v
    def bo(s):
        s.p += (-s.p) % 4; v = s.b[s.p]; s.p += 4; return bool(v)
    def st(s):
        n = s.i32()
        if n <= 0: return ""
        v = s.b[s.p:s.p+n].decode('utf-8', 'replace'); s.p += n; s.p += (-s.p) % 4; return v
    def pp(s): return (s.i32(), s.i64())

def hdr(b):                 # MonoBehaviour 头 28B
    r = R(b); r.pp(); r.i32(); sc = r.pp(); nm = r.st(); return sc, nm

def parse_collection(pid):
    b = raws[pid]; r = R(b); r.p = 28
    nm = r.st(); n = r.i32()
    pairs = [{"name": r.st(), "traitType": r.i32(), "traitConfig": r.pp()} for _ in range(n)]
    sprites = {k: r.pp() for k in ("sabotageSprite","faithSprite","ferocitySprite","oathSprite",
                                   "questPointsSprite","spiritStoneSprite","meleeAttackSprite","rangeAttackSprite")}
    assert r.p == len(b), (r.p, len(b))
    return nm, pairs, sprites

def parse_cardtrait(pid):
    b = raws[pid]; r = R(b); r.p = 28
    out = {"pathID": pid, "m_Name": r.st(), "traitIcon": r.pp(),
           "hasInPlayAnim": r.bo(), "hasIcon": r.bo(), "isInPlayIcon": r.bo(),
           "stackable": r.i32(), "aIScore": r.f32(),
           "customObject": (r.st(), r.st(), r.st()), "traitEffects": []}
    for _ in range(r.i32()):
        e = {"name": r.st(), "guid": r.st(), "subObjectName": r.st(), "subObjectType": r.st(),
             "overrides": [], "TriggerType": None, "playIfWarlord": None}
        for _ in range(r.i32()):
            e["overrides"].append({"army": r.i32(), "guid": r.st(), "sub": r.st(), "type": r.st()})
        e["TriggerType"] = r.i32(); e["playIfWarlord"] = r.bo()
        out["traitEffects"].append(e)
    assert r.p == len(b), (pid, r.p, len(b))          # ← 关键自检
    return out

collection_pid, traits = None, {}
for pid, b in raws.items():
    sc, nm = hdr(b)
    if   sc[1] == 3970: collection_pid = pid
    elif sc[1] in (1795, 231, 1586, 3039): traits[pid] = parse_cardtrait(pid)

nm, pairs, sprites = parse_collection(collection_pid)   # nm == "TraitsCollection", 77 条

# GUID -> CardAnim 名（读 15 个 *cardanims* 包的 container 键）
guidmap = {}
for bn in sorted(os.listdir(AA)):
    if "cardanim" not in bn: continue
    e = UnityPy.load(os.path.join(AA, bn))
    for _k, v in e.files.items():
        for _kk, sf in v.files.items():
            cont = getattr(sf, 'container', {}) or {}
            if not cont: continue
            objs = sf.objects.values() if isinstance(sf.objects, dict) else sf.objects
            names = {}
            for o in objs:
                if o.type.name == "MonoBehaviour":
                    try: names[o.path_id] = o.read().m_Name
                    except Exception: pass
            for ck, pv in cont.items():
                guidmap[ck] = (bn, names.get(getattr(pv, 'path_id', None)))

json.dump({"name": nm, "pairs": pairs, "sprites": sprites, "traits": traits},
          open("TraitsCollection.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("OK:", len(pairs), "pairs /", len(traits), "CardTrait objects")
```

**下一步（建议给实现代理）**：
1. 把上面脚本落成 `工具/export_trait_collection.py`，输出 `Unity/数据/游戏数据/trait_collection.json`
   （结构：77 条 `{name, traitType, traitConfig}` ＋ 73 个 `CardTrait` 全字段 ＋ 8 个 Sprite）。
2. `effectAnim` 的 GUID 用 §4.3 那张表换成人能读的名字（`CardAnim` 在哪个包、叫什么）。
   ⚠️ **别只存 GUID**：`CardAnim` 的资源名是**语义名**（`ArmourTraitStart` / `ShieldTraitTAUPlay` …），
   直接决定我们能不能接演出。
3. `_traitIcon`（Sprite 名 `Atlas_trait_icon_*`）与图集 `atlasindividual_assets_40ktraiticonatlas.bundle` 的 78 个 Sprite 对表 —— 图标图片 `assets_full` **已经导出**，可直接用。
4. 与现有 `资料/普查产出_第二十七会话/A1748_原版trait真值.md`（那份是**卡上挂哪些 trait**，来源 `cards_1195_2022时代.json`）**不是同一份东西**：
   那份 = 卡 → trait 列表；本报告 = **trait → 触发时机 → 演出**。两者互补，别合并。

---

## 六、要改的旧结论（铁律 5）

| 文件 | 旧话 | 实际 | 错因 |
|---|---|---|---|
| `资料/普查产出_1022/W64_素材导出缺口.md` **M9** | 「`assets_full` 全库按 `_traitEffects` … 搜 = 各 0 命中 …… **导出（源不在已解出的包里）**」 | **源在本地**：`d:/2/Warhammer 40k Warpforge/Warpforge_Data/sharedassets0.assets`，object **531** + **458–530** | ① `assets_full` 对 `sharedassets0.assets` 的 188 个 MonoBehaviour **导出失败 182 个**（`_stats.json`），JSON 里根本没有这批对象；② 按字段名 grep 只能证明"导出的 JSON 里没有"，**不能证明"资源里没有"**；③ 这批 SO 不在任何 Addressables bundle 里（84 包 66,459 个 MonoBehaviour 逐一反查 = 0），catalog 里也没有 —— 它在**裸 player 文件**那一层 |
| 同上 **§四 ②** | 「**没查**：裸 `resources.assets` / `sharedassets*` / 尚未解的远端包」 | **已查**：`resources.assets`(616) / `sharedassets0.assets`(188) / `sharedassets1.assets`(0) / `level0`(147) / `level1`(2) / `globalgamemanagers.assets`(29) 全部逐个 MonoBehaviour 读了 `m_Script` PPtr ⇒ **只有 `sharedassets0.assets` 命中** | 本轮补查完成 |

---

## 七、还差什么（没查清的部分，如实列出）

1. **`CardTraitCollection.Instance` 的最后一跳没验** ——
   `PlayerDataManager.Awake` 走 `AssetLocator.GetUniqueAsset<CardTraitCollection>()`，
   而这个方法是在 `SingletonBehaviour<AddressablesManager>.Instance` 的某个位置列表上做 `FirstOrDefault`（按 `Type` 比较）。
   该列表**由谁、在什么时候**把 `sharedassets0` 里这个非 Addressables 的 SO 注册进去，**没查到**。
   影响：不影响"数据在本地"，但若要让引擎复刻 `GetUniqueAsset` 的行为，得再查一次。
2. **`combo` / `daemonicAllegiance` / `firstStrike` / `large` / `mekaniak` / `piercing` / `unstoppable` 的 `_traitIcon` 是 null** ——
   原版数据如此。它们是否在别处（如 `BoardTraitIcon` 的 `traitIcons[]` 预制体）另有图标，**没查**。
3. **`TraitEffect` 里 4 个 `customObject` GUID 未解析**（`2415b1…` / `564290…` / `6fdb2c…` / `fb07f5…`）——
   它们**不是** `effectAnim`（`effectAnim` 67 个全解析成功），是 `CardTrait.customObject` 那个"多用途对象"。
   `catalog.bin` 里**都有**这四个 GUID，但**不在 15 个 `*cardanims*` 包的 container 键里** ⇒ 得再扫其它包。**没查**。
4. **`Assets/` 侧的接入点** —— 我们工程里 trait 现在怎么表示的、要不要替换成这份真值，
   属于实现决策，本报告（只读调查）不碰。
