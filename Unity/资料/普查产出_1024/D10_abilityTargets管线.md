# D10 · `abilityTargets` 管线审计（C4 顺手发现 4 的查死与裁定）

> 审计代理 `D10` · 2026-10-24 · 白名单内**零改动**（判定 = 不该收进这一列，判据见 §三）
> 判据三源：① 真值快照 `资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张 SO）·
> ② 全量反编译 `d:/2/tools/decomp_full/` · ③ 现读 `工具/gen_cards_engine.py` + `cards_engine.json`

---

## 〇 · 一句话结论

`abilityTargets` **只收 `cardAbilities[].abilityLogic[]`**，这是**该列的契约**（消费端按
`cardAbilities` 下标对齐卡面能力块）。`activeAbility.activeAbilityLogic[]` / `alternativeAbility.activeAbilityLogic[]`
的 `targetsAffected` **确实没被收，但也不该收进这一列** —— 补进去会**打破消费端的两道闸门**、
且**今天没有任何运行期消费者**（唯一消费者只认 `120`，而单位卡这条链恒带施放者 ⇒ 根本走不到）。
⚠️ 真正的复刻缺口在**别处**：`activeAbilityLogic` 的**能力内容**（`chosenAbility` / `damageCriteria` / `ta` / `furtherMultiplier`）
我们**整条没有按数据建模**（进表的只有三样：`hasActiveAbility` 那组闸、`animTimes` 的动画行、`unitConditions` 的 `ab=-1` 行），
而它的正文走的是文本解析层 —— 那里有一条**硬编码**（裸 `Deal N damage` → 自动选生命最低）与真值 `ta=30` 冲突。
**这条改的是 `EffectText.cs`，在本代理黑名单内 ⇒ 只能记账，不能改。**

---

## 一 · 现口径（`abilityTargets` 从哪些字段收 ta）

### 1·1 唯一收口点

`工具/gen_cards_engine.py` 全文只有**一处**读 `targetsAffected`：

| 位置 | 代码 | 读的字段 |
|---|---|---|
| `:1535-1562` `ability_targets_of()` | `:1552` `for j, lg in enumerate(ab.get("abilityLogic") or [])` · `:1553` `v = (lg.get("targetCriteria") or {}).get("targetsAffected")` | **只有** `trow["cardAbilities"][i]["abilityLogic"][j]["targetCriteria"]["targetsAffected"]` |
| `:3641-3645` 调用点 | `entry["abilityTargets"] = ability_targets_of(cid, name, trow, ABILITY_TARGETS_REPORT)` | 每张卡都落（没 ability 落 `[]`） |

`trow` = `full_raw`（`:1131` 展平后的那一层），所以上表就是 SO 原始字段路径。

### 1·2 逐字段清点（任务 1 的完整答案）

| 候选字段（SO 里实际存在） | 收不收 | 判据 |
|---|---|---|
| `cardAbilities[].abilityLogic[].targetCriteria.targetsAffected` | ✅ **只收这个** | `:1552-1557` |
| `activeAbility.activeAbilityLogic[].targetCriteria.targetsAffected` | ❌ 不收 | 全文无引用（`grep activeAbility` 只有 §1·4 那几列） |
| `alternativeAbility.activeAbilityLogic[].targetCriteria.targetsAffected` | ❌ 不收 | 同上 |
| `normalAbility` | — **本 build 里没有这个字段** | 1,351 张 SO 的 `full_raw` 键并集里**不存在**；`grep -o '"normalAbility"' = 0`。只有 `normalAbilityBoardCriteria`（criteria，不是 logic 行，已由 `ability_gate_of` 收进 `normalAbilityBoardCriteria` 列） |
| `otherAbilityAnims` | — **本 build 里没有这个字段** | `grep -o '"otherAbilityAnims"' = 0` |
| `activeAbility.loadingAbilityLogic` / `alternativeAbility.loadingAbilityLogic` | ❌（**空引用**） | 1,351 张里 `m_PathID != 0` 的 **0 张** ⇒ 没有「装填能力」这份数据 |

⚠️ **踩坑记录（给下一个写脚本的人）**：`alternativeAbility` 里的列表**不叫** `alternativeAbilityLogic`，
它和 `activeAbility` 一样叫 **`activeAbilityLogic`**（`grep -o '"alternativeAbilityLogic"' = 0`）。
我第一版扫描就是因为这个名字静默得到「alternative 行 0 张」，改用正确键后仍是 `SW21` 一张 —— 但**方法错一次就是无结论**。

### 1·3 同族列的收法对照（为什么 `abilityTargets` 这样收是对的）

| 列 | 收 `cardAbilities` | 收 `activeAbility` | 收 `alternativeAbility` | 依据 |
|---|---|---|---|---|
| `abilityTargets` | ✅ | ❌ | ❌ | **按 `cardAbilities` 下标对齐**（消费端两道闸门，见 §3·1） |
| `abilityMultipliers` | ✅ | ❌ | ❌ | 同上（同一套下标语义，`:1581-1586`） |
| `abilityTriggers` | ✅ | —（active 无 trigger） | — | `:1480-1508`，原版 active 走**虚拟 trigger 400**（见 §2·3） |
| `triggerPriority` / `abilityTriggerPriority` / `abilityContext` | ✅ | ❌ | ❌ | 都是 **`CardAbility`（= `cardAbilities` 元素）自带**的字段，activeAbility 对象上没有 |
| `animTimes` | ✅ | ✅（`tg=-1`、`o="-.j.k"`） | ❌ | `:2225-2230`。它是**按 `a`/`n`/`o` 做 join**，不靠下标对齐 ⇒ 可以跨两处 |
| `unitConditions` | ✅ | ✅（`ab=-1`） | ✅（`ab=-2`） | `:2027-2030` 显式 `scan_ability(-1/-2, …)`；现池实际只有 **1** 行 `ab=-1`、**0** 行 `ab=-2` |
| 能力闸（`hasActiveAbility`/`activeNeedsLoading`…） | — | ✅（对象级字段） | ✅ | `:1715-1760` |

### 1·4 生成器里所有碰 `activeAbility` 的地方（现读，供核对）

`:1732-1733`（`activeNeedsLoading` / `alternativeNeedsLoading`）· `:1783`+`:2029-2030`（`unitConditions` 的 `ab=-1/-2`）·
`:2185`/`:2190`+`:2225-2230`（`animTimes` 的 `-.j.k`）。**没有第四处**。

---

## 二 · 全池缺项对账

### 2·1 汇总（1,351 张 SO · 其中池内 1,127 张）

| 口径 | 全 1,351 张 SO | 池内 1,127 张 |
|---|---|---|
| 有 `activeAbility.activeAbilityLogic` 行 | **143 张 / 204 行** | **132 张 / 192 行** |
| 有 `alternativeAbility.activeAbilityLogic` 行 | **1 张 / 1 行**（`SW21 Wulfen Pack Leader`，`ta=10`） | 1 张 / 1 行 |
| 其中 `hasActiveAbility=1` | 88 张 | **87 张**（= 池内 ha=1 的全部） |
| 其中 `hasActiveAbility=0`（**本 build 的引擎路径不读这份数据**，见 §2·4） | 55 张 | 45 张 / 52 行 |
| `activeAbilityLogic` 行上 `furtherMultiplier != 0` | — | **72 张 / 109 行**（`abilityMultipliers` 同一盲区） |
| `activeAbilityLogic` 行 `ta == 120` | — | **1 行**（`SOR13` 第 2 行） |
| 池内「active 行非空 ∧ `cardAbilities` 为空」⇒ `abilityTargets == []` | — | **34 张** |

池内 `activeAbilityLogic` 的 `ta` 分布（192 行）：
`30:69 · 20:51 · 10:29 · 0:17 · 90:7 · 65:6 · 110:3 · 150:3 · 50:2 · 60:2 · 51:1 · 120:1 · 180:1`
（全 1,351 张 SO 是 204 行：`30:77 · 20:51 · 10:32 · 0:17 · 90:8 · …`，差的那 12 行全在池外 SO）
（对照 `cardAbilities` 那 1,616 行的分布：`10:399 · 30:322 · 0:286 · 20:236 · 120:67 · 150:54 …`
—— 两批的**形状完全不同**，说明它们确实是原版的两条独立能力链。）

### 2·2 逐张表（143 张，含池外 11 张）

`activeAbilityLogic ta` 按行序；`现 abilityTargets` 取 `cards_engine.json` 现读。

| # | SO id | 卡名 | cardType | hasActive | 在池 | activeAbilityLogic ta（逐行） | alternativeAbilityLogic ta | 现 abilityTargets |
|---|---|---|---|---|---|---|---|---|
| 1 | SW4 | Blood Claw | 0 | 1 | ✅ | 30 | — | [] |
| 2 | SAU37 | Orikan the Diviner | 10 | 0 | ✅ | 10 | — | [46:0] [570:20] |
| 3 | SOR3 | Morvenn Vahl | 10 | 1 | ✅ | 30, 20 | — | [46:0] [655:20] |
| 4 | AM36 | Maelon Dhrost | 0 | 1 | ✅ | 50 | — | [301:65] |
| 5 | DA23 | Bladeguard Veteran | 0 | 1 | ✅ | 30 | — | [] |
| 6 | GOF27 | Boss Zagstruk | 10 | 0 | ✅ | 10 | — | [46:0] |
| 7 | SW9 | Fenrisian Wolf | 0 | 1 | ✅ | 20 | — | [410:20,30] [410:30] |
| 8 | UM105 | Dreadnought | 0 | 1 | ✅ | 30, 110 | — | [] |
| 9 | SOR34 | Celestian Superior | 0 | 1 | ✅ | 0, 20 | — | [655:20] [605:0] |
| 10 | SOR5 | Battle Sister | 0 | 1 | ✅ | 20, 20, 20 | — | [655:20] |
| 11 | SW23 | Fenrisian Drop Pod | 0 | 1 | ✅ | 0, 0 | — | [] |
| 12 | SOR37 | Triumph of Saint Katherine | 0 | 1 | ✅ | 30, 150, 20 | — | [655:20] [660:80] |
| 13 | AM73 | Taurox Prime | 0 | 1 | ✅ | 0 | — | [] |
| 14 | AM34 | Valkyrie | 0 | 1 | ✅ | 10 | — | [180:0] |
| 15 | ASH74 | Jain Zar | 10 | 0 | ✅ | 30 | — | [46:0] [291:30] |
| 16 | UM76 | Lieutenant Titus | 10 | 0 | ✅ | 30 | — | [46:0] |
| 17 | AM21 | Tech-Priest Enginseer | 0 | 1 | ✅ | 51 | — | [301:20] [410:10] |
| 18 | UM98 | Devastator Marine | 0 | 1 | ✅ | 30 | — | [] |
| 19 | SOR24 | Simulacrum Imperialis | 0 | 1 | ✅ | 10, 20 | — | [655:20] |
| 20 | DA3 | Asmodai, Master Interrogator | 10 | 1 | ✅ | 0 | — | [490:0] [46:0] |
| 21 | UM88 | Valius Paxor | 10 | 0 | ✅ | 30 | — | [46:0] [31:20] |
| 22 | SAU36 | Nemesor Zahndrekh | 10 | 0 | ✅ | 10 | — | [46:0] [240:100] |
| 23 | UM102 | Terminator Captain | 0 | 1 | ✅ | 20 | — | [] |
| 24 | DA18 | Inner Circle Companion | 0 | 1 | ✅ | 20, 20, 20 | — | [] |
| 25 | AM12 | Flamer Trooper | 0 | 1 | ✅ | 10 | — | [] |
| 26 | SW1 | Logan Grimnar | 10 | 0 | ✅ | 65 | — | [46:0] |
| 27 | SW11 | Grey Hunter Pack Leader | 0 | 1 | ✅ | 30 | — | [] |
| 28 | SW21 | Wulfen Pack Leader | 0 | 1 | ✅ | 10 | 10 | [] |
| 29 | SAU66 | Ramatekh the Cruel | 10 | 0 | ✅ | 10 | — | [46:0] [20:10] |
| 30 | SOR30 | Celestian Sacresant | 0 | 1 | ✅ | 30, 20 | — | [655:20] |
| 31 | BL35 | Haarken Worldclaimer | 10 | 0 | ✅ | 30 | — | [46:0] [31:20] |
| 32 | AM35 | Leman Russ Vanquisher | 0 | 1 | ✅ | 90, 90 | — | [] |
| 33 | SW8 | Blackmane Reiver | 0 | 1 | ✅ | 65, 150 | — | [] |
| 34 | SOR26 | Aestred-Thurga | 0 | 1 | ✅ | 10, 20 | — | [655:20] [660:80] |
| 35 | TAU2 | Aun'Va | 10 | 0 | ✅ | 10 | — | [46:0] |
| 36 | 66486b3f…（SO 无 uniqueId，按 m_Name 同卡） | Tempestus Scion | 0 | 1 | 否 | 30, 90 | — | —（池外） |
| 37 | edb0fd16… | Medreyal Ghaelyn | 10 | 0 | 否 | 30 | — | —（池外） |
| 38 | UM87 | Roboute Guilliman | 10 | 0 | ✅ | 30 | — | [46:0] [145:20] |
| 39 | AM30 | Manticore | 0 | 1 | ✅ | 10 | — | [] |
| 40 | TL35 | Swarmlord | 10 | 0 | ✅ | 30 | — | [46:0] |
| 41 | SOR70 | Sororitas Rhino | 0 | 0 | ✅ | 30, 20 | — | [40:0] |
| 42 | BL34 | Abaddon the Despoiler | 10 | 0 | ✅ | 30 | — | [46:0] |
| 43 | SOR66 | Armorium Cherub | 0 | 1 | ✅ | 30, 20 | — | [0:30] [30:20,20] |
| 44 | b3a0be3e… | Haarken Worldclaimer | 10 | 0 | 否 | 30 | — | —（池外） |
| 45 | BL65 | Ghallaron the Pious | 10 | 0 | ✅ | 30 | — | [46:0] [90:20] |
| 46 | SAU35 | Imotekh the Stormlord | 10 | 0 | ✅ | 10 | — | [46:0] |
| 47 | AM4 | Faeburn Vanquisher | 0 | 0 | ✅ | 0 | — | [0:50] |
| 48 | UM89 | Incursor | 0 | 1 | ✅ | 30 | — | [0:30] |
| 49 | DA13 | Librarian | 0 | 1 | ✅ | 65, 20 | — | [] |
| 50 | DA1 | Azrael, Grand Master | 10 | 1 | ✅ | 65 | — | [490:0] [46:0] |
| 51 | ef547f82… | Nemesor Zahndrekh | 10 | 0 | 否 | 10 | — | —（池外） |
| 52 | DA27 | Ravenwing Talonmaster | 0 | 1 | ✅ | 10, 10 | — | [240:10] |
| 53 | DA75 | Master Zacharial | 10 | 1 | ✅ | 50, 0, 0 | — | [490:0] [46:0] |
| 54 | BL36 | Sylar Hexscorn | 10 | 0 | ✅ | 30 | — | [46:0] |
| 55 | UM106 | Ferren Areios | 0 | 1 | ✅ | 30 | — | [240:10] |
| 56 | TAU65 | War Shaper | 10 | 0 | ✅ | 10 | — | [46:0] [580:20] |
| 57 | UM91 | Tactical Marine | 0 | 1 | ✅ | 20 | — | [] |
| 58 | DA20 | Master Lazarus | 0 | 1 | ✅ | 30, 30, 20 | — | [] |
| 59 | SOR35 | Immolator | 0 | 1 | ✅ | 30, 110, 20 | — | [655:20] |
| 60 | UM48 | Varro Tigurius | 10 | 0 | ✅ | 30 | — | [46:0] |
| 61 | SW36 | Bjorn the Fell-Handed | 0 | 1 | ✅ | 10 | — | [20:10] |
| 62 | GOF75 | Warboss Gordrang | 10 | 0 | ✅ | 10 | — | [46:0] |
| 63 | AM22 | Primaris Psyker | 0 | 1 | ✅ | 90, 90 | — | [] |
| 64 | GOF77 | Beastboss Morgrim | 10 | 0 | ✅ | 10 | — | [46:0] [190:20] |
| 65 | SOR29 | Ephrael Stern | 0 | 1 | ✅ | 30, 20 | — | [655:20] [180:20] |
| 66 | bd6baa4e… | Tervigon（变体 SO） | 10 | 0 | 否 | 30 | — | —（池外） |
| 67 | AM69 | Tempestor Sergeant | 0 | 1 | ✅ | 10 | — | [301:10] |
| 68 | UM65 | Chaplain Letharius | 10 | 0 | ✅ | 30 | — | [46:0] [580:100] |
| 69 | SOR4 | Sister Novitiate | 0 | 1 | ✅ | 30, 20 | — | [655:20] [605:20] |
| 70 | TAU3 | Commander O'Maisos | 10 | 0 | ✅ | 10 | — | [46:0] [31:20] |
| 71 | GOF29 | Grukk Face-Rippa | 10 | 0 | ✅ | 10 | — | [46:0] [20:10] |
| 72 | UM94 | Scout Sniper | 0 | 1 | ✅ | 30 | — | [] |
| 73 | GOF28 | GhazghkullThraka | 10 | 0 | ✅ | 10 | — | [46:0] |
| 74 | SW5 | Hunting Wolf | 0 | 1 | ✅ | 10 | — | [] |
| 75 | SOR23 | Canoness | 0 | 0 | ✅ | 30, 20 | — | [605:20] [605:20] |
| 76 | DA71 | Watcher in the Dark | 0 | 1 | ✅ | 180, 0 | — | [] |
| 77 | ASH1 | Anvirr Keltoc | 10 | 0 | ✅ | 30 | — | [46:0] |
| 78 | AM59 | Astropath | 0 | 1 | ✅ | 65, 150 | — | [] |
| 79 | ASH76 | Lhykhis | 10 | 0 | ✅ | 30 | — | [46:0] |
| 80 | SOR69 | Zephyrim Superior | 0 | 1 | ✅ | 65, 20 | — | [655:20] |
| 81 | AM3 | Commissar Denkler | 10 | 1 | ✅ | 30 | — | [46:0] |
| 82 | TL34 | Tervigon | 10 | 0 | ✅ | 30 | — | [46:0] |
| 83 | AM66 | Tempestor Prime | 10 | 1 | ✅ | 30, 30 | — | [46:0] |
| 84 | **AM38** | **Baneblade Tank** | 0 | 1 | ✅ | **30** | — | **[240:100]** |
| 85 | DA7 | Ravenwing Bikes | 0 | 1 | ✅ | 0 | — | [] |
| 86 | DA29 | Darkshroud | 0 | 1 | ✅ | 30, 90 | — | [410:20] |
| 87 | UM90 | Phobos Lieutenant | 0 | 1 | ✅ | 20 | — | [120:10] |
| 88 | d1a037ff… | Marneus Calgar (tutorial) | 10 | 0 | 否 | 30 | — | —（池外） |
| 89 | SOR18 | Dialogus | 0 | 1 | ✅ | 30, 20 | — | [655:20] |
| 90 | AM70 | Medic Scion | 0 | 1 | ✅ | 60 | — | [0:30] |
| 91 | UM47 | Uriel Ventris | 10 | 0 | ✅ | 30 | — | [46:0] |
| 92 | SOR1 | Erika Luminas | 10 | 1 | ✅ | 30, 20 | — | [46:0] [655:20] |
| 93 | SOR2 | Junith Eruita | 10 | 1 | ✅ | 30, 20 | — | [46:0] [655:20] |
| 94 | UM101 | Terminator Sergeant | 0 | 1 | ✅ | 30 | — | [] |
| 95 | 52520927… | Grukk Face-Rippa（变体 SO） | 10 | 0 | 否 | 10 | — | —（池外） |
| 96 | ASH2 | Eliac Zephyrblade | 10 | 0 | ✅ | 30 | — | [46:0] |
| 97 | SOR13 | Simulacrum Bearer | 0 | 1 | ✅ | 10, 120, 20 | — | [655:20] |
| 98 | UM97 | Suppressor | 0 | 1 | ✅ | 60 | — | [] |
| 99 | GSC66 | Shanus Daskovian | 10 | 0 | ✅ | 30, 20 | — | [46:0] |
| 100 | GSC3 | Iconward Malak Vorenth | 10 | 0 | ✅ | 30, 20 | — | [46:0] |
| 101 | AM10 | Cadian Standard Bearer | 0 | 1 | ✅ | 10 | — | [0:0] |
| 102 | **AM5** | **Shock Trooper** | 0 | 1 | ✅ | **30** | — | **[]** |
| 103 | AM1 | Lord Solar Leontus | 10 | 1 | ✅ | 30 | — | [46:0] |
| 104 | SOR15 | Celestian | 0 | 1 | ✅ | 20, 20 | — | [655:20] |
| 105 | GSC1 | Primus Saffa Rhiannor | 10 | 0 | ✅ | 30, 20 | — | [46:0] |
| 106 | TL74 | Terror of Vardenghast | 10 | 0 | ✅ | 30 | — | [46:0] [120:20] |
| 107 | UM95 | Attack Bike | 0 | 1 | ✅ | 20, 20 | — | [] |
| 108 | UM92 | Firestrike Servo-Turret | 0 | 1 | ✅ | 20 | — | [] |
| 109 | SOR65 | Celestian Sacresant Aveline | 10 | 1 | ✅ | 30, 20 | — | [46:0] [655:20] |
| 110 | SOR68 | Ministorum Priest | 0 | 1 | ✅ | 30, 20 | — | [655:20] |
| 111 | UM93 | Vico Therbeus | 0 | 1 | ✅ | 20 | — | [20:10] |
| 112 | AM16 | Scout Sentinel | 0 | 1 | ✅ | 0 | — | [] |
| 113 | AM2 | Ursula Creed | 10 | 1 | ✅ | 0 | — | [46:0] |
| 114 | SOR17 | Sister Superior | 0 | 1 | ✅ | 30, 20 | — | [655:20] |
| 115 | SW3 | Ragnar Blackmane | 10 | 0 | ✅ | 0 | — | [46:0] |
| 116 | AM68 | Tempestus Scion | 0 | 1 | ✅ | 30, 90 | — | [] |
| 117 | DA65 | Chaplain Gabutheron | 0 | 1 | ✅ | 30, 0 | — | [690:10,10] |
| 118 | SOR71 | Adelaide the Serene | 0 | 0 | ✅ | 30, 20 | — | [0:0] [605:20,20] |
| 119 | SOR20 | Hospitaller | 0 | 1 | ✅ | 30, 20 | — | [655:20] |
| 120 | ec0d25fa… | Uriel Ventris (tutorial) | 10 | 0 | 否 | 30 | — | —（池外） |
| 121 | SOR6 | Battle Sister Gunner | 0 | 1 | ✅ | 30, 20 | — | [655:20] |
| 122 | b5ee585c… | Sylar Hexscorn (tutorial) | 10 | 0 | 否 | 30 | — | —（池外） |
| 123 | GSC2 | Magus Uthrel Naas | 10 | 0 | ✅ | 30 | — | [46:0] |
| 124 | SW29 | Morkai Eliminator | 0 | 1 | ✅ | 10 | — | [0:10] |
| 125 | SOR72 | Castigator | 0 | 0 | ✅ | 30, 20 | — | [660:10] |
| 126 | ASH3 | Medreyal Ghaelyn | 10 | 0 | ✅ | 30 | — | [46:0] |
| 127 | SOR25 | Imagifier | 0 | 1 | ✅ | 30, 30, 20 | — | [655:20] |
| 128 | UM46 | Marneus Calgar | 10 | 0 | ✅ | 30 | — | [46:0] |
| 129 | TL36 | Neurothrope | 10 | 0 | ✅ | 30 | — | [46:0] |
| 130 | AM11 | Shock Trooper Sgt. | 0 | 1 | ✅ | 0 | — | [] |
| 131 | DA14 | Ravenwing Speeder | 0 | 1 | ✅ | 30, 90 | — | [410:0] |
| 132 | SOR67 | Sacred Rose Sister | 0 | 1 | ✅ | 20, 20 | — | [655:20] [605:20] [605:20] |
| 133 | TAU1 | Shadowsun | 10 | 0 | ✅ | 10 | — | [46:0] |
| 134 | UM108 | Brutalis Dreadnought | 0 | 1 | ✅ | 10 | — | [] |
| 135 | UM99 | Phobos Librarian | 0 | 1 | ✅ | 30 | — | [180:20] |
| 136 | b5eedae3… | Uriel Ventris（变体 SO） | 10 | 0 | 否 | 30 | — | —（池外） |
| 137 | UM96 | Chapter Champion | 0 | 1 | ✅ | 20, 20 | — | [145:20] |
| 138 | AM28 | Chimera | 0 | 1 | ✅ | 0, 0 | — | [240:10] |
| 139 | GOF76 | Mozrog Skragbad | 10 | 0 | ✅ | 10 | — | [46:0] |
| 140 | UM103 | Terminator | 0 | 1 | ✅ | 20 | — | [] |
| 141 | AM23 | Bombast Field Gun | 0 | 1 | ✅ | 30, 110 | — | [] |
| 142 | UM104 | Bladeguard Ancient | 0 | 1 | ✅ | 30 | — | [240:10] |
| 143 | cb29b9bc… | Nemesor Zahndrekh（变体 SO） | 10 | 0 | 否 | 10 | — | —（池外） |

> 「缺的 ta」= 上表 `activeAbilityLogic ta` 那一列**全部**（它们一条都没进 `abilityTargets`）；
> `alternativeAbilityLogic ta` 也只 `SW21` 一张有（`10`），同样没进。

### 2·3 原版怎么用这些行（**它们不是死数据**）

`d:/2/tools/decomp_full/RawCardScript__ResolveActiveAbility.c`（全文 71 行，本会话逐行读过）：

```
:18   if (*(char *)(param_1 + 0x2aa) == '\0')            // hasAlternativeAbility
:21       lVar6 = *(longlong *)(param_1 + 0x260);        // → activeAbility
:31       lVar6 = *(longlong *)(param_1 + 0x2b8);        // → alternativeAbility（MeetsCriteria 为假时）
:50   lVar6 = *(longlong *)(lVar6 + 0x48);               // → 那个 Ability 对象的 activeAbilityLogic（List）
:58-67    while (iVar5 < *(int *)(lVar6 + 0x18))          // 逐行
:65           AbilityLogic__PlayAbility(lVar4, param_2, uVar1, 400, 0);   // 400 = ThisUnitAbilityUsed
```

而 `AbilityLogic__PlayAbility.c:262` 就是 `uVar14 = GetTargets(param_2, *(param_1 + 0x28) /* targetCriteria */, …)`，
`AbilityLogic__GetTargets.c:192-207`（`case 0x1e = target(30)`）把 **玩家选中的那个目标**原样返回、不做筛选。
⇒ **原版确实逐行消费 `activeAbilityLogic[].targetCriteria.targetsAffected`**（`ta=30` = 玩家点）。

### 2·4 哪些是「原版也不看」的（`hasActiveAbility=0` 那 55 张 / 池内 45 张）

`CardScript__DoesAbilityRequireLoading.c:13` 以 `hasActiveAbility(+0x220)` 为闸（我们 `ConditionEval` 的 280 已按它实现）；
`EntityScript__GetActiveAbilityDamage.c` 只在 `hasActiveAbility` 为真时改扫 `activeAbilityLogic`。
⇒ 这 45 张的 `activeAbilityLogic` 在本 build 是**没有被消费的残留数据**（`SW21` 的 alternative 支同理要看 `hasAlternativeAbility=1`，它恰好是 1）。

---

## 三 · 影响判定

### 3·1 `abilityTargets` 在本仓的**唯一**消费者链（逐环带行号）

```
cards_engine.json: card.abilityTargets
  → CardDef.cs:797/941-943/2162-2172           AbilityTargetSpec.Trigger / TargetsAffected（+ AbilityTargetsVersioned）
  → EffectText.cs:1557 → AssignAbilityTargets(:1913-1960)
        G1 = 整卡能力块数 == abilityTargets.Count        (:1925)
        G2 = 块内 op 数  == 该 ability 的 ta 长度         (:1933)
     通过 ⇒ op.SubjectTargetsAffected = ta[j]            (:1935 / :1956)
  → EffectResolver.cs:1402 `if (spec.Subjectless)` → :1404 `if (source != null)` … :1429 else
       ⇒ 只有「**无主语 op** ∧ **无施放者** ∧ **玩家没点目标**」三样同时成立，才读 :1442 `op.SubjectTargetsAffected`
       ⇒ 而且 :1443 **只认 `120 friendlyHero`**；其余值一律落到 :1456-1460 的「己方全体」近似并出声
```

结论三条：

1. **只有 `120` 有行为**。整列 1,616 个 logic 里 `120` 只有 67 行；B-17 报告（`资料/普查产出_1023b/引擎线_尾巴_执行报告.md` §B-17）
   实测**现池真正被修到的只有 1 张**（`GOF42 Da Irongob` 的 `heals 5` 落己方督军）。
2. **单位卡这条路永远走不到那一格**：单位触发正文 / 替代行动（`Duty`/`Pray`/`Ferocity`/`Agenda`）走
   `RuleCore.cs:11064 ResolveOps(ctx, owner, u, ops, …)`，**`source = u` 非空** ⇒ `:1404` 支取「施放者自己」，
   `:1442` 的 ta 读取被短路。`source == null` 的调用点只有：战术卡（`:2236`/`:2356`）、常驻效果（`:9032`）、
   手牌陷阱/监听（`:9346`/`:9361`）、死亡结转 `DeathWatch`（`:9979`，载体是**死者**的触发正文）。
   ⚠️ 这四处**全部来自 `cardAbilities` 一族的触发/卡级效果**，没有一处会吃 `activeAbilityLogic` 的正文
   ⇒ 结论不受影响。
3. ⇒ **132 张卡的 `activeAbilityLogic` ta：今天 0 个运行期消费者。**
   （唯一 `ta=120` 的那一行 `SOR13` 也是单位卡 ⇒ 同样走不到。）

### 3·2 C4 那条疑点逐项裁定（`AM38 Baneblade Tank`）

| C4 说 | 裁定 |
|---|---|
| `cardAbilities[0]` 只有 ta=100（相邻 +1 护甲） | ✅ 属实：`trigger=240(WhileInPlayVariable)`, `ta=[100](adjacentToSelfIfMeetsCriteria)` |
| `Duty: Deal 8 damage` 在 `activeAbility.activeAbilityLogic[0]`，ta=30 | ✅ 属实（`chosenAbility=10 doDamage`，`targeted=1`） |
| 「只要按 `cardAbilities` 生成 ⇒ 这条 Duty 伤害会丢」 | ✅ **数据上确实没进**；❌ **但这不是缺陷**：`abilityTargets` 的契约就是「按 `cardAbilities` 下标对齐」 |
| 「未核是否当前漏项」 | **当前不是漏项，而且补了也没用**：<br>① 补进同一数组 ⇒ `abil.Count` 变大 ⇒ **G1 反而失败**（`AM38` 的 desc 有 **2 个能力块**：`Armour 2. Adjacent units have Armour 1.` / `Duty: Deal 8 damage`，而 `cardAbilities` 只有 1 条 ⇒ **G1 现在就已经失败**，连已收的 `ta=100` 都落不到 op 上）；<br>② 即便对齐成功，那条 deal op **不是 `Subjectless`**（`Deal 8 damage` 走 `EffectText.cs:6340-6362` 的裸伤害支，目标 = `Auto` 敌方最低血），ta 也读不到 |

**`AM38` 的 desc 块数为什么是 2（不跑 Unity 的推法）**：`EffectText.Split` 按 `.`/换行切（`EffectText.cs:1198-1214`）⇒ 3 段；
`SegmentStartsAnotherAbility`（`:2020-2035`）对第 3 段 `Duty: Deal 8 damage` 命中
`^([a-z]+)\s*:\s*(.+)$`，`duty` ∈ `CardDef.RoutableTriggers`（`CardDef.cs:1223`）⇒ 块数 = 2 ≠ `abilityTargets.Count`(1) ⇒ **G1 失败**。

### 3·3 真正会「行为与数据不符」的那条（**不在本列上**）

原版 `ta=30` = **玩家点目标**（`AbilityLogic__GetTargets.c:192-207`）；
我们处理主动能力正文走的是**文本解析层**，那里有一条硬编码：

```
EffectText.cs:6340-6362   if (tok.Length == 0)   // 裸 `Deal N damage`
                              → Auto 目标 = 敌方 PickMost("-health")（自动选生命最低）
```

⇒ 对「正文只有 `Deal N damage`、而真值 ta=30」的卡，**原版问玩家、我们自动挑**。
现池「active 行非空 ∧ `cardAbilities` 为空」的 34 张里，**4 张**就是这种裸伤害：
`AM5 Shock Trooper`（30）· `AM35 Leman Russ Vanquisher`（90，`targetIfMeetsCriteria` 同样用玩家选中的那个）·
`UM101 Terminator Sergeant`（30）· `UM94 Scout Sniper`（30）。
C4 顺手发现 2 已经对全池做过统计（33 张裸 `Deal <n> damage`，**33/33 都是 `ta=30`**，例外 `AM35 = 90`），本会话复核了其形状，**未发现反例**。
⚠️ **修点在 `RuleEngine/Core/EffectText.cs` = 本代理黑名单** ⇒ 只能记账（建议另立引擎线账，不要塞进 `abilityTargets` 这条线）。

### 3·4 同族的第二个盲区（`abilityMultipliers`）

`activeAbilityLogic[].furtherMultiplier` 同样没被收（池内 **72 张 / 109 行**非 0，`SAU37`/`SOR3`/`SOR5`… 多为 `1`，`AM73`/`AM16` 为 `2`）。
消费端 `EffectText.cs:1939` 的 G2 与 ta **同一套闸门** ⇒ 现状同样是「读不到」。这条一起记在 §六·2。

---

## 四 · 改动与 diff 汇总

**改动：无。** `工具/gen_cards_engine.py` 与 `MyGame/Assets/RuleEngine/Resources/cards_engine.json` **一个字节都没动**。

判据（本会话实跑）：

| 检查 | 结果 |
|---|---|
| `python -I -X utf8 工具/gen_cards_engine.py --check` 第 1 次 | `rc=0` · 末行「现有 … cards_engine.json 一致 ✅」 |
| 同上第 2 次 | `rc=0` · 末行相同 · `cmp` 两次完整输出 ⇒ **逐字节相同** |
| `sha256sum cards_engine.json`（两次 --check 前后） | 恒为 `1d1a5d6a2d0c658098d010abb8dfd908ccd52db17981b24a0c3094597ce7abd7` |
| `git status --porcelain 工具/gen_cards_engine.py MyGame/Assets/RuleEngine/Resources/cards_engine.json` | **空**（两个文件均无改动） |
| `cards_engine.json` 的 diff | **0 行 / 0 张卡 / 0 列** |

---

## 五 · 是否需重钉池指纹

**不需要** —— 没改卡表，`PinnedPoolMd5`（现读 `fafc0f562db54f139b29891b119b8c7e`，`工具/ruleprobe/Program.cs:216`）保持有效。

⚠️ **将来若采纳 §六·1 的方案**（真的落一列 active 的 ta）：`cards_engine.json` 必变 ⇒
需主对话按 `Program.cs:216` 那套流程**重钉 `PinnedPoolMd5`**（⛔ 本代理不改常量）。

---

## 六 · 没查清 / 没做 / 建议

### 6·1 建议（**留给调度台定口径**，本代理不擅自实施）

若要把 active 链的 ta 也落成数据，**唯一不破坏现有契约的形状**是**另开一列**（⛔ 不要并进 `abilityTargets`）：

- 现成的命名/下标先例有两条：`unitConditions` 用 `ab=-1`(active) / `ab=-2`(alternative)（`gen_cards_engine.py:1783`）；
  `animTimes` 用 `o="-.j.k"` + `tg=-1`（`:2185`/`:2190`）。
- 反编译还给了第三个线索：原版给这些行传的 trigger 就是 **400（`ThisUnitAbilityUsed`）**（`RawCardScript__ResolveActiveAbility.c:65`）
  ⇒ 形状可以是 `[{"trigger":400,"src":"active","ta":[…]}]`。
- 🔴 **但消费端（`CardDef`/`EffectText`/`EffectResolver`）必须同时改**才能有行为 —— 那是 `.cs`，本代理黑名单。
  **只落数据不改消费端 = 零行为变化**（本仓允许「先落数据」，但要在报告里写明，⛔ 不能报成「已修」）。
- 真要修 §3·3 那条**真缺陷**（裸 `Deal N damage` 该问玩家），至少要三件一起：
  ① active 行的 ta 落数据；② 卡面能力块 ↔ active 行的归位（`Duty:`/`Oath:`/`Ferocity:` 块要能指到 active 行，而不是靠 G1 计数）；
  ③ `EffectText.cs:6340` 那支在「这条 op 的 ta=30」时改成问玩家。**缺一件就是半成品**（会走成「问了但不生效」或「自动挑但仍报数据对」）。

### 6·2 没查清 / 没做

1. **没有逐张算池内「desc 能力块数」** —— 那要跑 C# 的 `CountAbilityBlocks`（`EffectText.cs:1994-2008`），本代理不跑 Unity。
   只对 `AM38` 按 `Split` + `SegmentStartsAnotherAbility` 的规则**逐条推**出「2 块 vs 1 ability」。
   ⇒ 「有多少张卡因为 G1 失配而让 `abilityTargets` 完全无效」**没有总量**，只知道 `abilityTargets == []` 的 34 张必然无效。
2. **`abilityMultipliers` 的同一盲区只统计未判消费**：72 张 / 109 行 active 行 `fm != 0`，消费端闸门与 ta 同一套（`EffectText.cs:1939`），
   推断同样读不到；**没有**逐张验证 op 数是否也能对上。
3. **没进原版实机**：原版已关服、进不了对局，「ta=30 一定弹选目标」的判据是**反编译**（`GetTargets.c:192-207`），
   不是运行期观测。若将来能跑，值得对 `AM5` 的 Duty 点一次确认。
4. **`activeAbility` 对象级 `targetCriteria`（不是 per-row）没查**：例如 `AM38.activeAbility.targetCriteria`
   = `ta=10(allFiltered)` + `friendlyUnits=1/enemyUnits=1/includeHero=1/includeMinions=1/includeRemnants=1`
   —— 这是「这条主动技能能选哪些格子」的过滤器，我们**任何一列都没收**；它影响的是 UI 高亮/合法性，不是效果目标。**登记为顺手发现，未深查**。
5. **`hasAlternativeAbility=1` 的分支闸已实现但 active/alt 两份 logic 都没进数据**：`SW21` 的两行都是 `ta=10`，
   所以今天接不接都一样；一旦有卡的 active 与 alternative 的 ta 不同，现状就**分不出该用哪一份**。

### 6·3 顺手发现（只报不改）

1. 🟡 **`AM38` 的 `abilityTargets` 目前整条是死数据**（G1 失败：2 块 vs 1 ability）——
   这比「漏了 Duty 那条」更值得记：**卡面正文带 `Duty:`/`Pray:`/`Oath:`/`Ferocity:` 这类触发器头的单位卡，
   `cardAbilities` 的条数与 desc 的能力块数天然是两个口径**（active 能力块没有对应的 `cardAbilities` 项）⇒ G1 **容易**失配（⛔ 未量化，见 6·2·1）。
   另一形状是 **G2 对不齐**：`SOR5 Battle Sister` 的 `cardAbilities` 只有 **1** 条（`ta` 长度 1），
   而正文 `Pray: Gain +1 Attack and +1 Ranged and heal 1` 至少给出 **2～3** 个效果（⚠️ 照正文推的，**没跑解析层核 op 数**）。
2. 🟡 **`alternativeAbility` 的列表键名是 `activeAbilityLogic`，不是 `alternativeAbilityLogic`** ——
   脚本按后者取会静默得 0 行（本代理第一版就踩了）。D10 之后写这类脚本的人注意。
3. 🟡 **`activeAbilityLogic` 行上的 `cardAnimations` 已经进了 `animTimes`（`o="-.j.k"`）** ——
   说明「active 链缺数据」不是设计决定，而是**逐列推进时漏了这一列**（`abilityTargets`/`abilityMultipliers` 都没跟上）。
4. 🟡 **`gen_cards_engine.py` 的 `unitConditions` 已经扫 active/alternative（`ab=-1/-2`）**，
   但现池只有 1 行 `ab=-1`、0 行 `ab=-2` —— 说明「原版的 active 行几乎不带 `conditionsFilter`」，
   这和 ta 那批（192 行）形成对比：**ta 有数据、conditions 没有**，两条别互相推广。
5. 🟢 **`normalAbility` / `otherAbilityAnims` 在本 build 的 SO 里根本不存在**（`grep -o` 各 0 次）——
   任务书里点名的这两个字段**不是漏收，是没有**。若别的文档/脚本引用过它们，那是个错名（建议就地核一下是谁写的）。
