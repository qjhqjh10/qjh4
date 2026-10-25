# `CardTrait._stackable` 真值表（第 2 轮 · 定案）

> **一句话结论**：**`_stackable` 不是占位 `10`，它是真值** ——
> **75 个有 SO 的词条里 74 个 = `add(10)`、1 个 = `count(20)`（`MarkOfChaos`）**；
> `berzerk` / `dodge` 两个 pair **根本没有 SO**（原版如此）。
> ⇒ **K2/K3 `addTrait` 的前置已经满足，可以开工**；`判据/战斗逻辑驱动_AddEffect分派器.md:171` 里
> 「那个 10 是『未解』的占位编码」**是一条错记录，本文就地推翻（铁律 5）**。
>
> 口径：**统计一律用 `MyGame/Assets/RuleEngine/Resources/cards_truth.json`**；⛔ 不用 `cards_raw.json`（每卡两份同构树 ⇒ 翻倍）。

---

## §0 判据链（三级独立，逐级可复算）

| 级 | 判据 | 实读了什么 | 结果 |
|---|---|---|---|
| 🥇 **枚举值** | `D:/2/tools/il2cpp_out/dump.cs:21302-21308`（**带显式值的签名桩**） | `enum CardTrait.TraitStackType` | `replace = 0` · `add = 10` · `count = 20`（三个常量逐个读，⛔ 不是数成员个数） |
| 🥈 **字段位置** | `dump.cs:21327-21346` | `CardTrait` 的 8 个 `[SerializeField]` **声明顺序 + 托管偏移** | `_traitIcon(0x18) · _hasInPlayAnim(0x20) · _hasIcon(0x21) · _isInPlayIcon(0x22) · **_stackable(0x24)** · _aIScore(0x28) · customObject(0x30) · _traitEffects(0x38)` —— **声明序 = 序列化序**，`_stackable` 在第 5 位 |
| 🥉 **原版资产逐对象** | `d:/4/Unity/工具/export_trait_collection.py`（**`--check` 只读模式**，现跑） | `d:/2/Warhammer 40k Warpforge/Warpforge_Data/sharedassets0.assets` 的 73 个 `CardTrait` MonoBehaviour 原始字节，按上表布局逐字段走 | **`bytesRead == byte_size` 73/73 全通过**；`[check] ✓ 与盘上逐字节相同 (73087 B, md5 cbb9809d7fb9796527c49118b33eba27)` |
| 旁证（行为） | `CardScript.cs:18060` / `.c:494` | `count` 分支把 `traitValue` 当**次数**累加（`+1`） | `MarkOfChaos` = `count` 与「黑暗契约记份数」自洽（K7 上一轮已实测「契约累加」） |

> **为什么 `bytesRead == byte_size` 能当硬证据**：`_stackable` 之后还跟着 `_aIScore`(float) 和**变长**的
> `customObject`(3 个 string) + `_traitEffects`(变长数组，元素里还有 string)。**只要字段宽度错 4 字节，
> 后面的变长结构就必然读飞、字节数不可能对上**。73/73 全对上 ⇒ 布局与偏移都正确 ⇒ `_stackable` 是**实读**，
> **不是**外推、也不是默认值填充。
>
> ⚠️ **实读免责**：`export_trait_collection.py` 用的是**固定偏移解析**（不是 `ttread.py` 的生成类型树）——
> `判据/CardTrait词条权威表.md:3` 那句「用 IL2CPP 生成树解出（`工具/ttread.py`）」**归因不准**（`ttread.py` 全仓 0 个调用者）。
> 但**结论不受影响**：上表第 🥇🥈 级是独立来源，第 🥉 级有 73/73 字节数校验。

---

## §1 真值表（77 条 TraitPair 全列）

`pathID` = 该词条 `CardTrait` SO 在 `sharedassets0.assets` 里的对象 id；`—` = 该 pair 的 `traitConfig` 是 null（原版没给 SO）。

| # | `definedTraitName` | SO `m_Name` | `definedTrait` | `_stackable` | 枚举名 | pathID | 判据 |
|---|---|---|---|---|---|---|---|
| 0 | agenda | Agenda | 1235 | 10 | **add** | 458 | 实读 |
| 1 | ambush | Ambush | 1200 | 10 | **add** | 459 | 实读 |
| 2 | armour | Armour | 1020 | 10 | **add** | 460 | 实读 |
| 3 | artifice | Artifice | 1040 | 10 | **add** | 461 | 实读 |
| 4 | backlash | Backlash | 120 | 10 | **add** | 462 | 实读 |
| 5 | **berzerk** | —（无 SO） | 360 | **null** | — | — | 实读（pair 存在、`traitConfig` 为 null） |
| 6 | blast | Blast | 350 | 10 | **add** | 463 | 实读 |
| 7 | blind | Blind | 975 | 10 | **add** | 464 | 实读 |
| 8 | bloodThirst | BloodThirst | 220 | 10 | **add** | 465 | 实读 |
| 9 | camouflage | Camouflage | 985 | 10 | **add** | 466 | 实读 |
| 10 | cantAttack | CantAttack | 150 | 10 | **add** | 467 | 实读 |
| 11 | codex | Codex | 1000 | 10 | **add** | 468 | 实读 |
| 12 | combo | Combo | 1130 | 10 | **add** | 469 | 实读 |
| 13 | companion | Companion | 1180 | 10 | **add** | 470 | 实读 |
| 14 | concussive | Concussive | 930 | 10 | **add** | 471 | 实读 |
| 15 | cruelty | Cruelty | 1255 | 10 | **add** | 472 | 实读 |
| 16 | daemonicAllegiance | DaemonicAllegiance | 1100 | 10 | **add** | 473 | 实读 |
| 17 | **dodge** | —（无 SO） | 240 | **null** | — | — | 实读（pair 存在、`traitConfig` 为 null） |
| 18 | dontReturnFerocity | **NotFerocity** | 1270 | 10 | **add** | 496 | 实读 |
| 19 | duty | Duty | 1220 | 10 | **add** | 474 | 实读 |
| 20 | ecstasy | Ecstasy | 1250 | 10 | **add** | 475 | 实读 |
| 21 | ephemeral | Ephemeral | 5 | 10 | **add** | 476 | 实读 |
| 22 | fast | Fast | 40 | 10 | **add** | 477 | 实读 |
| 23 | ferocity | Ferocity | 1265 | 10 | **add** | 478 | 实读 |
| 24 | firstStrike | FirstStrike | 630 | 10 | **add** | 479 | 实读 |
| 25 | flank | Flank | 460 | 10 | **add** | 480 | 实读 |
| 26 | flying | Flying | 390 | 10 | **add** | 481 | 实读 |
| 27 | frenzied | Frenzied | 680 | 10 | **add** | 482 | 实读 |
| 28 | huntMark | HuntMark | 1260 | 10 | **add** | 483 | 实读 |
| 29 | invulnerable | Invulnerable | 310 | 10 | **add** | 484 | 实读 |
| 30 | large | Large | 940 | 10 | **add** | 485 | 实读 |
| 31 | longrange | LongRange | 1160 | 10 | **add** | 486 | 实读 |
| 32 | **markOfChaos** | MarkOfChaos | 330 | **20** | **count** | 487 | 实读 |
| 33 | markOfKhorne | MarkOfKhorne | 1060 | 10 | **add** | 488 | 实读 |
| 34 | markOfNurgle | MarkOfNurgle | 1070 | 10 | **add** | 489 | 实读 |
| 35 | markOfSlaanesh | MarkOfSlaanesh | 1080 | 10 | **add** | 490 | 实读 |
| 36 | markOfTzeentch | MarkOfTzeentch | 1090 | 10 | **add** | 491 | 实读 |
| 37 | markerlight | Markerlight | 1170 | 10 | **add** | 492 | 实读 |
| 38 | mekaniak | Mekaniak | 950 | 10 | **add** | 493 | 实读 |
| 39 | mob | Mob | 920 | 10 | **add** | 494 | 实读 |
| 40 | notDestroyRemnant | NotDestroyRemnant | 1190 | 10 | **add** | 495 | 实读 |
| 41 | oath | Oath | 1275 | 10 | **add** | 497 | 实读 |
| 42 | oathDouble | OathAdditionalEffect | 1277 | 10 | **add** | 498 | 实读 |
| 43 | oathInAllTurns | OathAdditionalEffect | 1276 | 10 | **add** | **498**（与 42/44 同一个 SO） | 实读 |
| 44 | oathTripleActivation | OathAdditionalEffect | 1278 | 10 | **add** | **498**（同上） | 实读 |
| 45 | pack | Pack | 610 | 10 | **add** | 499 | 实读 |
| 46 | piercing | Piercing | 900 | 10 | **add** | 500 | 实读 |
| 47 | pindown | PinDown | 970 | 10 | **add** | 501 | 实读 |
| 48 | pray | Pray | 1185 | 10 | **add** | 502 | 实读 |
| 49 | rage | Rage | 280 | 10 | **add** | 503 | 实读 |
| 50 | rally | Rally | 10 | 10 | **add** | 504 | 实读 |
| 51 | regeneration | Regeneration | 1030 | 10 | **add** | 505 | 实读 |
| 52 | regiment | Regiment | 1225 | 10 | **add** | 506 | 实读 |
| 53 | relentless | Relentless | 450 | 10 | **add** | 507 | 实读 |
| 54 | remnant | Remnant | 1050 | 10 | **add** | 508 | 实读 |
| 55 | resolution | Resolution | 840 | 10 | **add** | 509 | 实读 |
| 56 | sentry | Sentry | 1280 | 10 | **add** | 510 | 实读 |
| 57 | shield | Shield | 80 | 10 | **add** | 511 | 实读 |
| 58 | shuriken | Shuriken | 1120 | 10 | **add** | 512 | 实读 |
| 59 | slay | Slay | 260 | 10 | **add** | 513 | 实读 |
| 60 | sniper | Sniper | 990 | 10 | **add** | 514 | 实读 |
| 61 | stealth | Stealth | 70 | 10 | **add** | 515 | 实读 |
| 62 | stimulation | Stimulation | 1245 | 10 | **add** | 516 | 实读 |
| 63 | stomp | Stomp | 1240 | 10 | **add** | 517 | 实读 |
| 64 | strike | Strike | 1110 | 10 | **add** | 518 | 实读 |
| 65 | stun | Stun | 100 | 10 | **add** | 519 | 实读 |
| 66 | swarm | Swarm | 650 | 10 | **add** | 520 | 实读 |
| 67 | synapse | Synapse | 1150 | 10 | **add** | 521 | 实读 |
| 68 | talent | Talent | 845 | 10 | **add** | 522 | 实读 |
| 69 | teleport | Teleport | 1230 | 10 | **add** | 523 | 实读 |
| 70 | tide | Tide | 910 | 10 | **add** | 524 | 实读 |
| 71 | unstable | Unstable | 1010 | 10 | **add** | 525 | 实读 |
| 72 | unstoppable | Unstoppable | 180 | 10 | **add** | 526 | 实读 |
| 73 | uprising | Uprising | 1210 | 10 | **add** | 527 | 实读 |
| 74 | vanguard | Vanguard | 980 | 10 | **add** | 528 | 实读 |
| 75 | vulnerable | Vulnerable | 480 | 10 | **add** | 529 | 实读 |
| 76 | waystone | Waystone | 1140 | 10 | **add** | 530 | 实读 |

**汇总**：`add` **74** 条 · `count` **1** 条 · 无 SO **2** 条（`berzerk` / `dodge`）。
（`TraitsCollection` 是**集合 SO**、不是词条，没有 `_stackable` —— 77 个 pair 里不含它。）

### §1·1 三条容易踩的（都实读过，别按名字猜）

1. **`dontReturnFerocity` 的 SO 名是 `NotFerocity`**（pathID 496）—— 上一版 `CardTrait词条权威表.md` 把它列成「我们工程里连子串都没有 / 没有 SO」，**「没有 SO」是错的**（SO 在，只是 `m_Name` 与 `definedTraitName` 不同）。
2. **`oathDouble` / `oathInAllTurns` / `oathTripleActivation` 三个 pair 指向【同一个】 SO**（`OathAdditionalEffect`，pathID 498）——
   所以 `CardTraitCollection.definedTraits` 里会有**三个 key 指向同一个对象**。⇒ 查「某 trait 的 SO」按 **key**（`DefinedTrait`）取，⛔ 别按 `m_Name` 去重。
3. **`berzerk`(360) / `dodge`(240) 没有 SO** ⇒ `CardTraitCollection.GetTrait()` 返回 **null**（`CardTraitCollection.cs:334` 先 `ContainsKey` 后取值）。见 §3。

---

## §2 本 build 里 `addTrait` 到底会授哪些 trait（池内实测）

口径：`cards_truth.json` 的 **1,351 张 SO**，扫**整卡**（`abilities` + `activeAbility` + `alternativeAbility` 全走），只取 `buffType == 3(addTrait)` / `90(addTraitRemnant)`。
**合计 370 条**（`addTrait` **368** + `addTraitRemnant` **2**）—— 与账上 `addTrait(368)` **逐数吻合**。

| trait | 次数 | 卡数 | 该效果的 `traitValue` 分布 | `_stackable` |
|---|---|---|---|---|
| flank | 62 | 52 | 全 0 | add |
| vanguard | 33 | 26 | 全 0 | add |
| fast | 32 | 26 | 全 0 | add |
| shield | 30 | 25 | 全 0 | add |
| blast | 24 | 21 | 1×6 · 2×12 · 3×4 · 5×1 · 6×1 | add |
| invulnerable | 21 | 18 | 全 0 | add |
| stealth | 21 | 19 | 全 0 | add |
| vulnerable | 19 | 17 | 1×6 · 2×8 · 4×3 · 5×1 · 0×1 | add |
| markerlight | 18 | 15 | 1×4 · 2×12 · 3×2 | add |
| huntMark | 14 | 12 | 恒 1 | add |
| blind | 12 | 8 | 全 0 | add |
| shuriken | 10 | 9 | 1×6 · 2×4 | add |
| bloodThirst | 10 | 8 | 全 0 | add |
| flying / camouflage / concussive | 各 8 | 7 / 8 / 7 | 全 0 | add |
| **markOfChaos** | **7** | **7** | 全 0 | **count** |
| sentry | 7 | 6 | 1×5 · 2×2 | add |
| regeneration | 5 | 4 | 1×4 · 2×1 | add |
| longrange | 4 | 3 | 全 0 | add |
| sniper / stun | 3 | 2 / 3 | 全 0 | add |
| stomp / pack | 2 | 2 | 全 0 | add |
| ferocity | 1 | 1 | 1 | add |
| **notDestroyRemnant**（`buffType 90`） | 1 | 1 | 0 | add |
| **shield**（`buffType 90`） | 1 | 1 | 0 | add |

> ⚠️ 这张表**只覆盖池内 1,128 张可玩卡之外的 SO 全集口径**：表里的次数已按「整卡扫」口径，⛔ 别再用只扫 `abilities` 的口径复算（那会少 21 条 `addTrait`）。
> ⚠️ `buffType == 8 markOfChaos`（**附魔**）与 `trait == 330 markOfChaos`（**词条**）是**两件东西**，⛔ 别混。

---

## §3 上游语义：`_stackable` 到底被谁读、读来干什么（定案）

**唯一读点** = `CardScript.AddEffect` 的 `addTrait` 分支里那段**挂载去重循环**（`CardScript.cs:17937-18092` / `CardScript__AddEffect.c:483-560`）。
它管的是「**同一个 trait 第二次加上来时，旧记录怎么变**」：

| `_stackable` | 命中同一条已存在记录时 | 没有命中时（新挂） |
|---|---|---|
| `replace(0)` | `旧.traitValue = Math.Max(旧, 新)` | 直接 `Add`，`traitValue` 用效果自己的 |
| `add(10)` | `旧.traitValue += 新.traitValue` | 直接 `Add`，`traitValue` 用效果自己的 |
| `count(20)` | `旧.traitValue += 1` | **先把 `newEffect.traitValue` 强制改成 1**，再 `Add` |

「命中」= **六条同时成立**：`buffType ∈ {3, 90}` ∧ `trait` 相同 ∧ `markOfChaosType` 相同 ∧ `daemonHostType` 相同 ∧ `SameTimingAsEffect()` ∧ **两侧 `extrinsic` 都为假**。
（`SameTimingAsEffect` = `CardEffect.cs:142`：`untilEndOfTurn / untilPlayerTurnStart / untilEnemyTurnStart / untilFollowingPlayerTurnStart / untilNextAttack` 逐格相等；带施放者归属的那几格还要求「施放侧相同」。）

🔴 **`_stackable` 管的不是「身上有几个 trait」，是「这条 `CardEffect` 记录的 `traitValue` 怎么合」**：
- 「身上有没有这个 trait」由 **`CurrentTraits`（`CardTraitData` 列表，按 trait 去重）** 表示 —— `AddTraitSilently`（`CardScript.cs:19301`）**先 `Any(同 _trait)` 判重，已有就什么都不做**，与 `_stackable` **无关**；
- 「这个 trait 现在数值是多少」= `CardTraitData._traitValue` **＋** 所有 `addTrait` 记录的 `traitValue` 之和（`GetCurrentTraitValueWithModifiers`，`EntityScript.cs:3406-3487`）。
- `HasCurrentTrait`（`EntityScript.cs:3116-3219`）对普通单位 = **`CurrentTraits` 命中 ∪ `activeEffects` 里有同 trait 的 `addTrait`**（所以即使 `CardTraitData` 被摘掉，只要效果记录还在，仍算「当前带着」）。残骸单位只看 `addTraitRemnant`。

**⇒ 对我们的引擎意味着（可直接照做）**：我们只有一份表示 —— `UnitState._keywords: Dictionary<string,int>`（关键词 → 值）。
- `add` ⇒ `_keywords[k] += v`（**现在 `AddKeyword` 就是这个**，`UnitState.cs:1093`）
- `count` ⇒ `_keywords[k] += 1`（**不看效果自己的 v**；`EffectResolver.cs:11933` 的 `AddKeyword(MarkOfChaos, 1)` 已经是这个语义）
- `replace` ⇒ `_keywords[k] = max(旧, v)` —— **本 build 没有一条 trait 是 `replace`** ⇒ 今天**零可观察量**；要不要写由实现方定（写了也无害，但**别为它改任何现有分支**）。

---

## §4 与既有文档的冲突（就地订正）

| 文件:行 | 原写法 | 实际 | 处置 |
|---|---|---|---|
| `判据/战斗逻辑驱动_AddEffect分派器.md:171` | 「该表把值都写成 `10`，⚠️ 那个 10 是『未解』的占位编码，不是枚举值 ⇒ 要用时回 `工具/ttread.py` 解 `_stackable`」 | **10 就是枚举值 `add`**；表里唯一非 10 的是 `MarkOfChaos = 20`；已用 73/73 字节数校验复核 | **本条正文已按本节订正**（见该文件这一行的新写法） |
| `判据/CardTrait词条权威表.md:3` | 「用 IL2CPP 生成树解出（`工具/ttread.py`）」 | `ttread.py` 全仓 0 调用者；实际产出 JSON 的是 `工具/export_trait_collection.py`（固定偏移 + `bytesRead==byte_size` 校验） | 归因不当，**结论不受影响**；本文 §0 已如实标出 |
| `判据/CardTrait词条权威表.md:51-53` | `NotFerocity` / `OathAdditionalEffect`「我们完全没有 / 没有 SO」 | `NotFerocity` **有 SO**（pathID 496，= `dontReturnFerocity`）；`OathAdditionalEffect` 是 `Oath` 的**三兄弟共用的一个 SO**（498，服务 1276/1277/1278） | 本文 §1·1 已订正 |

---

## §5 还差什么 / 没查清

1. **`replace` 分支的行为没有实测样本** —— 本 build 没有 `replace` 的 trait，所以那一支是**读代码得出的**（`CardScript.cs:18015-18040`），不是实测。写实现时可以照代码写，但**不要为它配「行为断言」**（无观测量）。
2. **`_stackable` 在「两份效果 timing 不同」时不参与合并** —— 此时 `activeEffects` 会有**两条**同 trait 记录，读取时**相加**（`GetCurrentTraitValueWithModifiers`），所以 `replace` 的「取大」在跨 timing 情形下**退化成相加**。这是原版行为（不是我们的 bug），但**本 build 没有 `replace` ⇒ 零可观察量**，只记不实现。
3. **`_aIScore`**：74/74 全是 `0.0`（同一个脚本现读）⇒ AI 评分**不走**这个字段，⛔ 别接。
