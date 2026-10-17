# ID · 我们的卡数据链到底接没接原版 SO，以及能不能反查

## 一、结论摘要

- **A（用没用）**：**用了，但只用了 SO 的「身份/分类/枚举」那半**。`cards_engine.json`（现读 1127 张）由 `工具/gen_cards_engine.py` 生成；29–30 列里有 **17 列是原版 SO 直取**：`id / rarity / cardTraits / inventoryOptions / showInCollectionOptions / initiative / goesSecond` + A1799 那 10 列。**三围（攻/远/血）、费用、效果文字、兵种、卡名都不是 SO** —— 来自 PnP 成品卡图的 OCR（有意：PnP 优先于老快照）。
- **B（能不能读出数值）**：**能**。SO 的四项数值是扁平字段（`manaCost / meleeAttack / rangeAttack / maxHealth / armour`），改类型也不删。抽 UM 部队卡 `UM83 Valtus` 逐项比，**9/9 相等**。全池 1009 张能按 id join 的卡里，**927 张四项全等，82 张不等**（是 PnP 与 SO 两个来源冲突，不是读不出）。
- **C（能不能逆推是哪张卡）**：**不能只靠数值唯一确定**。1192 张按「`cardType`+`spellType`+四项+`armour`」只有 349 组，**134 组是多卡共享（977 张）**，最大一组 **139 张**；最坏如 36 张督军全是 2/2/2/35。必须再加 **`uniqueId`（1192/1192 唯一）**或 **`m_Name`（1192/1192 唯一）**。

## 二、A · 数据链逐列溯源

生成链：`card_stats.json`（1213 条 OCR 源表）→ `gen_cards_engine.py` → `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（1127）。
旁路真值：`cards_1195_2022时代.json`（1192 张真卡 + 2 张异画 `#HB_` + 1 条 `AddressablesBuildNumber`）→ 按 `uniqueId` join 进生成器。

| 列 | 源头 | 抽取方式 | 原版 SO？ |
|---|---|---|---|
| `name` | `card_stats.json` `name` | PnP 卡图 OCR（= 卡图资产名） | ✗ |
| `nameZh` | `官方本地化_en+zh-CN.json` 的 `Card_Name/*`；兜底 `zh_cards.json` 自译 | 官方表直取 | ✗ |
| `desc` | `card_stats.json` `desc`（+ `cardface_fixes.json` 人工） | PnP OCR / 逐张人工 | ✗ |
| `descZh` | 官方表 `Card_Description/*` → 自译 → 人工（三层，后者赢） | 本地化表 | ✗ |
| `cost` | `card_stats.json` `cost` + `STAT_FIXES`；**督军另取 SO `manaCost`** | OCR；督军 SO | 部分（督军） |
| `attack` | `card_stats.json` `attack` + `STAT_FIXES` | PnP OCR | ✗ |
| `ranged` | `card_stats.json` `ranged_attack` + `STAT_FIXES` | PnP OCR | ✗ |
| `health` | `card_stats.json` `health` + `STAT_FIXES` | PnP OCR | ✗ |
| `armour` | PnP `keywords` 的 `Armour N` 优先；SO `armour` 兜底 | 正则 + SO 兜底 | 兜底（现读 0 次触发） |
| `subtype` | `card_stats.json` `subtype` + `cardface_fixes` | PnP OCR / 人工 | ✗（SO `keyword` 枚举可等价，未采用） |
| `keywords` | OCR → SO `CardTraits` **只补空** → 修正表 → 正则 → 人工 | 多层 | 部分 |
| `faction` | `card_stats.json` `faction`（PnP 目录/索引映射） | 映射 | ✗ |
| `type` | `card_stats.json` `type`（OCR） | OCR | ✗（SO `cardType`+`spellType` 可完美复现，见 §五） |
| `rarity` | SO `cardRarity` 第 1 层 → 宝石表 → 特判 → OCR | SO | ✓ 1005/1005 一致 |
| `id` | SO `uniqueId`（经 `card_id_truth_index.json` / `card_ids.json`） | SO | ✓ 1009/1127 |
| `cardTraits` | SO `CardTraits` | SO | ✓ |
| `inventoryOptions` | SO 同名字段 | SO | ✓ |
| `showInCollectionOptions` | SO 同名字段 | SO | ✓ |
| `initiative` | SO 同名字段 | SO | ✓ |
| `goesSecond` | SO `goSecondCardInHand`（PPtr 解 id） | SO | ✓（50/56 督军） |
| A1799 十列 `attackDamageType / cardShortId / useOverDraw / cantBeRandomlyCreated / expansionPack / refNameId / itemDescriptionKey / fullHeroSpriteReference / eventSpriteName / targeted` | SO 同名 | SO | ✓ 10 列 |

**SO 直取列的现核实证**：1009 张按 id join 的卡，`inventoryOptions / showInCollectionOptions / initiative / A1799 六个整数列` **逐一 == SO，0 处差异**；`rarity` 1005 张有 `cardRarity`，**0 处不一致**。

**已知记录核实**（三条都仍成立）：
- `card_stats.json` 的 `desc` **不是 SO** —— 是 PnP 卡图 OCR。证据：文件里带 `ocrSrc`（指向 `d:/2/Warpforge部队卡片/...png`）、`ocrName`；生成它的上游是 `d:/warpforge/tools/ocr_cards.py`（视觉模型按卡面提取）→ `merge_ocr.py` → `merge_card_stats.py`。⚠️ 现读 → `d:/4` 副本 **1213 条** vs Godot 旧副本 **1212 条** ⇒ 之后在 `d:/4` 侧有过人工改动，但抽取方式没变。⚠️ `ranged` 的源列 `ranged_attack` 也是 OCR：raw 里那列叫 `armor`，而 OCR prompt 把**紫圈**叫成了 armor（`ocr_cards.py` 的 `PROMPT`）⇒ `merge_card_stats.py:302,344` 把它改名成 `ranged_attack`（紫圈 = 远程，正是铁律 7 记的那个坑）。
- `card_ids.json` 的 `mapping`（1310 行）**真值部分就是 SO 抄的**：现核 **1194 个快照 `uniqueId` 全在表里，值 1194/1194 逐字 == SO `cardName`**。构成 = 933 真值短 id + 259 真值 GUID + 116 旧表残留 + 2 个 `#HB_` 空值。
- ⚠️ **就地订正简报里一句**（铁律 5）：中文**已经换进官方表了**，不是「还没换」。`nameZh` 自 2026-10-14、`descZh` 自 2026-10-15 起以 `官方本地化_en+zh-CN.json` 为首选；自译 `zh_cards.json` 只在官方表查不到时兜底（`gen_cards_engine.py:30-45,1382-1500`）。现核 `UM83` 的 `nameZh=瓦尔图斯`、`descZh=[[armour]] 1. 当友方单位攻击时…` 与官方表逐字相同。

## 三、`cards_1195_2022时代.json` 谁在读

**结论：不是「只是判据来源」—— 它已经进了正式数据链，9 个生成器直接读它。**

直接读（`json.load` 那一份原件）：
1. `gen_cards_engine.py`（`TRUTH_CARDS_SRC:807`：稀有度/督军 cost/CardTraits/armour 兜底/A1799 十列）
2. `gen_card_id_truth_index.py`（`:41`：id join 索引，喂给 `pick_truth_id`）
3. `gen_card_inventory_options.py`（`:55`）
4. `gen_card_initiative.py`（`:48`）
5. `gen_card_vfx_by_card.py`（`:123`，`--truth`）
6. `gen_talent_cards.py`（`:40`，`cardAbilities[trigger==46]` 的 `unitsToSummon`）
7. `gen_trait_dict.py`（`:26`，`CardTraits`）
8. `gen_ability_enum_dict.py`（`:29`，`activeAbility` 枚举）
9. `import_original_audio.py`（`:117`，12 个 `*ChatSound`）

间接读（读由它派生的产物）：`gen_card_anim_order.py`（`card_anim_order.json`；文件头记录 897/897 对 SO 逐 GUID 核过）· `gen_ability_anim_durations.py`（`card_vfx_by_card.json` 的 `ability[]`）· `import_original_art.py`（`card_vfx_by_card.json` 的 `_assetRefs[assetKind=art]`）。

一次性/诊断脚本：`_a1759_reconcile.py` · `_a1759_report.py` · `_guid_resolve_probe.py` · `_mine_allcards_fields.py` · `_oneoff_1014_p1/apply_card_ids_guids.py` · `_oneoff_1014_p1/build_picks.py`。（均只读 `d:/2`，不写。）

## 四、B · SO → 数值：UM 一张卡逐字段对比

`UM83 Valtus`（Ultramarines 部队卡，SO `m_Name = 7 Ultramarines-Valtus`）：

| 字段 | SO 值 | 我们 `cards_engine` | 一致 |
|---|---|---|---|
| `cost` ← `manaCost` | 9 | 9 | ✓ |
| `attack` ← `meleeAttack` | 9 | 9 | ✓ |
| `ranged` ← `rangeAttack` | 9 | 9 | ✓ |
| `health` ← `maxHealth` | 9 | 9 | ✓ |
| `armour` | 1 | 1 | ✓ |
| `rarity` ← `cardRarity=3` | epic | epic | ✓ |
| `id` ← `uniqueId` | UM83 | UM83 | ✓ |
| `inventoryOptions / showInCollectionOptions` | 0 / 5 | 0 / 5 | ✓ |
| `initiative` | 20 | 20 | ✓ |
| `targeted / cardShortId / attackDamageType / expansionPack / useOverDraw / cantBeRandomlyCreated` | 0 / 473 / 4 / 55 / 1 / 0 | 同左 | ✓ |
| `subtype` | SO 无字符串字段，`keyword=10`（=Vehicle） | Vehicle（OCR 卡面） | ✓（不同来源） |

**全池对账**：1009 张 join 上，**927 张四项全等**；**82 张不等**，逐张如下（`费/攻/血/远/甲` 左=我们、右=SO；全部是「PnP 值 vs 老快照」冲突，生成器有意让 PnP 赢）：

```
`ASH15` Howling Banshee 攻3≠4 · `ASH79` Orian Laratharjos 远4≠5 · `ASH20` Wraithguard 攻3≠2
`e59ac9e46f4ea804784e954c3b820e77` Skyborne Deployment 费1≠2 · `d3e99cb18b8cbba4081486f57f510f75` Witchfire 费0≠1 · `ASH29` Wave Serpent 血8≠7
`ASH76` Lhykhis 血25≠35 · `AM3` Commissar Denkler 血30≠35 · `BL71` Havoc Champion 血7≠8
`453b0e8f88e094a17866a6979f389ef5` Infernal Gaze 费2≠1 · `BL74` Spreading Corruption 费4≠3 · `DA19` Interrogator Chaplain 远3≠2
`DA51` Covert Operation 费3≠4 · `a84a5b49f0ca9144585058db6987c2a0` Master Interromancer 费1≠3 · `DA55` Wages of Retribution 费3≠4
`6ed51d59b9f8ea24ab0dc7ceaef7956c` Duelist's Hubris 费1≠2 · `EC55` Lucius The Eternal 血25≠35 · `EC57` Xarahan Daemon Prince 血30≠40
`GSC20` Aberrant 血6≠5 · `GSC32` Abominant 甲1≠0 · `GSC16` Acolyte Heavy 远2≠0
`GSC15` Acolyte Specialist 攻2≠1 · `GSC28` Hulking Aberrant 甲1≠0 · `GSC23` Hybrid Metamorph 攻4≠3
`GSC30` Metamorph Leader 攻3≠6,血5≠6,远4≠6 · `GSC11` Neophyte Heavy 血4≠3 · `GSC44` Subterranean Ambush 费4≠5
`SAU70` Damaged Hexmark 血4≠5 · `SAU55` Immortal Pride 费7≠6 · `GOF83` Grot on Bombsquig 攻1≠2
`GOF88` Nob on Smasha Squig 攻6≠5 · `GOF99` Skrag Every Stash! 费3≠1 · `GOF96` Snakebite Battlewagon 血11≠10
`GOF76` Mozrog Skragbad 血40≠35 · `GOF82` Beast Snagga Nob 远2≠1 · `GOF85` Snakebite Nob 血5≠4
`e3d67d86f73f7564eb0bf886ec848af6` Waaagh! Energy 费0≠2 · `GOF68` Big Choppa Nob 远5≠4 · `GOF59` Big Shoota Boy 攻3≠2
`GOF62` Kommando 攻8≠7,血7≠6 · `GOF18` PainBoy Sniklaw 攻6≠5,远3≠2 · `GOF66` Rokkit Boy 攻3≠2,远5≠4
`GOF60` Tankbusta 攻5≠4,远5≠4 · `GOF20` Veteran Flyboy 攻7≠6 · `GOF37` Da Green Horde 费3≠4
`GOF25` Gorkanaut 血10≠9 · `GOF63` Gunwagon 远8≠7 · `GOF70` Krumpaklaw 远6≠4
`GOF61` Rukkatrukk Squigbuggy 甲1≠0 · `SOR6` Battle Sister Gunner 远3≠2 · `SOR15` Celestian 攻2≠1
`SOR22` Dominion Superior 远6≠5 · `SOR11` Dominion 远4≠3 · `SOR20` Hospitaller 远3≠2
`SOR16` Missionary 血5≠4 · `SOR17` Sister Superior 血5≠4 · `SOR57` Hallowed Martyrs 费5≠4
`SOR59` Sacred Altar 费1≠0 · `TAU27` Broadside Battlesuit 攻4≠3 · `TAU34` Coldstar Battlesuit 攻6≠5
`TAU21` Stealth Battlesuit 血5≠4 · `TAU6` Gun Drone 远2≠1 · `TAU7` Marker Drone 远2≠1
`TAU10` Stealth Drone 血1≠2 · `TAU12` Darkstrider 攻3≠2 · `TAU18` Fire Warrior Marksman 血5≠4
`TAU25` Firesight Marksman 远5≠4 · `TAU22` Strike Team 远6≠5 · `TAU42` Experimental Drones 费4≠5
`TAU63` Tidewall Gunrig 费0≠1 · `TAU30` Sky Ray Gunship 攻4≠3,远9≠8 · `TL66` Neurogaunt 费2≠1
`a58e05916eb5e994daa0349cbe237001` Synaptic Imperative 费2≠1 · `TL35` SwarmLord 血40≠35 · `UM40` Avenging Zeal 费12≠10
`9238c27730a0c424288d46ed0f4a742c` Master of the Fleet 费2≠1 · `UM32` Tactical Insight 费0≠1 · `4a48457c24877f345be2e96f867c1e94` Dark Pact of Fate 费1≠-1
`9a2cadc64c80a6b42912441105bb1cee` Dark Pact of Blood 费1≠-1 · `b8e81b2fe85cda747a68c4d6326b669c` Dark Pact of Excess 费1≠-1 · `1dfc9bc738bfc4f4d93d48f165aee510` Dark Pact of Resilience 费1≠-1
`EC56` Lord Kaphrael 血25≠35
```

**怎么判这 82 张**：不是「读不出 SO」的缺陷，而是**两个来源冲突，且生成器已裁「PnP 赢」**（`gen_cards_engine.py` 的 `TRUTH_CARDS_SRC` 段）。其中 **14 张**（Kommando / Metamorph Leader / Strike Team / Acolyte Heavy / Big Shoota Boy / Avenging Zeal / Skrag Every Stash! / Skyborne Deployment / Witchfire / Waaagh! Energy / Tactical Insight / Abominant / Hulking Aberrant / Rukkatrukk Squigbuggy）**已逐张开 PnP 图核过，结论全是「我们==PnP、SO≠PnP」**；**余 68 张没有逐张核过 PnP**（详见 §七）。另：`BL1 Chaos Spawn` 我当场开了 PnP 图，卡面橙字确为 `Infantry`，而 SO `keyword=40`（Daemon）⇒ 这条也是 PnP 对、SO 旧。

## 五、C · 逆推：1192 张按「数值 + 类型」分组实测

分组键 = `(cardType, spellType, manaCost, meleeAttack, rangeAttack, maxHealth, armour)`：

| 口径 | 行数 | 组数 | 唯一组 | 多卡共享组 | 共享组内行数 | 最大组 |
|---|---:|---:|---:|---:|---:|---:|
| 全部（类型+spellType+四项+甲） | 1192 | 349 | 215 | **134** | **977** | **139** |
| 只要部队（四项+甲） | 540 | 307 | 204 | 103 | 336 | 11 |
| 部队 + 阵营 | 540 | 500 | 466 | 34 | 74 | 4 |
| 督军 | 60 | 7 | 1 | 6 | 59 | 36 |

**最坏样本**（光看数值完全不可区分）：
- **139 张计策** `cardType=20, cost=2, 其余 0`：`Stomp 'Em / The Rock(×4) / Perfect Hosts / Wrath of Khaine …`
- **79 张进攻卡** `spellType=200, cost=-1`：`Infinity Circuit Overload / Radiation Storm / Solar Eclipse …`
- **36 张督军** `2/2/2/35`：`Orikan / Boss Zagstruk / Jain Zar / Lieutenant Titus / Nemesor Zahndrekh …`
- **11 张部队** `2/2/2/3/甲0`：`Vox Operator / Tempestus Scion / Slugga Boy / Trukk Boy / Primaris Intercessor …`
- 部队 + 阵营仍不唯一（34 组）：如 AstraMilitarum 的 `2/2/2/3` 有 4 张（`Vox Operator / Tempestus Scion ×2 / Cadian Medic`）。

**结论**：数值 + 类型**不能唯一确定**；需要：
- **`uniqueId`** —— 1192 张里 **1192 个，全局唯一**（唯一可靠主键）；
- 或 **`m_Name`**（资产名，1192/1192 唯一，且自带阵营与 `_enchantmentN` 后缀）；
- ⚠️ `cardName` **不够**：1192 行只有 1085 个不同名字，87 个重名（194 行）；`cardName + cardArmy` 仍有 84 组重名（含 107 个 `_enchantment` 副本、76 个本体各带 1–6 份）；
- ⚠️ **外观 GUID 也不够**：`cardImageReference` 1060 个不同 GUID，75 组共享（最多 7 张共用一张图）。
- 补充：SO 的 `cardType + spellType` 是**完美的类型分类器**（1009/1009）：`0/0=unit`、`10/0=hero`、`20/0=tactic`、`20/210=defence`。

## 六、可操作的查法

```python
import json
snap = json.load(open(r"d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json", encoding="utf-8"))
byid = {r["uniqueId"]: r for r in snap if r.get("uniqueId")}          # 1194 键（含 2 个 #HB_）
row  = byid["UM83"]                                                    # id → 全字段
# 反查：名字 → 可能多张，必须再按 cardArmy / m_Name 收窄
byname = {}
for r in snap:
    if r.get("uniqueId") and r.get("cardName"):
        byname.setdefault(r["cardName"].lower(), []).append(r)
# 只有数值时：先按 (cardType,spellType,四项,armour) 取候选集，再用 uniqueId/卡图 GUID 判
```

- **有 id 缺名字/数值**：`byid[id]` 一次拿全（含 `cardName / m_Name / 四项 / cardAbilities / cardImageReference`）。
- **有名字缺 id**：`byname[名字]` → 再按 `cardArmy` 或 `m_Name` 后缀（`_enchantmentN`）收窄；⚠️ 我们卡池 **46/1009 张的 `name` 与 SO `cardName` 不同**（例 `AM32 Leman Russ` vs `Leman Russ Tank`、`GSC3 Acolyte Iconward` vs `Iconward Malak Vorenth`），名字直接 join 会落空 —— 用 `card_ids.json`（id→SO 名）或我们的 `id` 桥接。
- **有 id ↔ 卡面/特效**：id → `cardImageReference.m_AssetGUID` → `guid_map.tsv` / `card_vfx_by_card.json` → 资产名（判据链见 `R9_卡牌id真值挖掘.md` / `R10_…`）。
- **只有数值想认卡**：数值只给候选集（§五），**必须**再用名字/立绘/能力文本判；⛔ 不要按数值猜一张。

## 七、还没查清的（⛔ 不猜）

1. **82 张冲突里余下 68 张没有逐张开 PnP 图核过** —— 目前只有「生成器口径 = PnP 优先」＋ 14 张实证，**不能宣称 82 张全部「我们==PnP」**。
2. 生成器注释说「核过 **13 张**」，但随后枚举了 **14 张卡名**（`Kum…Rukkatrukk`），**计数与清单差一**，需校对（不影响结论）。
3. **`keyword` 枚举 → `subtype` 的完整映射**只实测出 `5/10/15/25/30/35/40` 七档（Infantry/Vehicle/Monster/Battlesuit/Drone/Beast/Daemon）；`keyword=0` 的 386 张（计策/督军/防御卡）不携带兵种。**我们没用这一列**，兵种仍走 OCR。
4. SO 的 **`cardAbilities` 效果语义**目前只用了三处（`activeAbility` 动画时长、天赋 `unitsToSummon`、2 张进攻卡）；**效果结算仍以卡面文字解析为准**，SO 能力树没有整棵接进引擎 —— 这条只回答到「用的是哪几处」，**没查「还剩多少可接」**。
