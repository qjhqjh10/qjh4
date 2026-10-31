# B107_AN1_U5 — `notPlayed` 22 条逐条复核（今天还成立吗）

> 只读产出，**未改 `card_vfx_by_card.json`**。
> 复核对象：`Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json` 的 `notPlayed[]`
> （现读 `notPlayedCount = 22`，`outcome` = **16 `duplicate` + 6 `unbacked`**，与 `_note` 的两种口径一致）。
> 回源：① 全量卡 SO `资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张 / 67 MB / 3,806 个不同 GUID）
> ② 全量 VFX 表 `数据/游戏数据/card_vfx_by_card.json`（`byCard` 1,129 张 / 4,585 条 `authoritative`，generatedAt **2026-10-31 11:46**）
> ③ 名字↔GUID `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` ④ 方法体 `d:/2/tools/decomp_full/`

## 结论

- **22/22 的「核心结论」今天仍然成立**：16 条 `duplicate` 的同一条 CardAnim 确实已在该卡的 `authoritative` 桶里（**连 `src` 字段路径都逐条对上**）；
  6 条 `unbacked` 的 CardAnim 确实**没有任何卡 SO 引用**，且「视觉没丢、只是不是这张卡的件」的旁证也都成立。
- **1 条的支持性细节过期**：`AM30`（第 1 条）写着「AM18（**ability**）」，现读 AM18 的那一条是 **`trigger`（TurnEnd 40）**，不是 `ability`。
  **核心结论不变**（AM30 的 `play` 确实没有逐卡件），但建议把括号里那个桶名改掉。
- **待核 0**。剩余不可复核项 2 个（都不是结论本身）：①「按名字猜出来的第二次记账」这条**产线来源**只能在生成器里核，数据侧只能核到「同 guid 已权威」；
  ② `why` 里「全 24.7 万件解包资产双扫」这一句本轮**没有重扫全库**（只扫了卡 SO / `CardTrait` / `card_anim_map` 三个源）。
- **不需要把任何一条从 `notPlayed` 移回播放桶**（没有一条发现新的权威依据）。

## 复核表 A · `duplicate`（16 条，全部成立）

| # | id | 卡名 | 事件 | why（摘要） | 今天还成立吗 | 证据（同卡 `authoritative` 里的那条） |
|---:|---|---|---|---|---|---|
| 3 | BL55 | Black Crusade | play | 同一 CardAnim 已在 play 桶 | ✅ 成立 | `byCard.BL55.play` = `abc7fb22…` @ `cardAbilities[0].abilityLogic[0].cardAnimations[0]`（另有 [1]/[2]/[3] 同 guid） |
| 4 | BL42 | Traitors' Hate | play | 同上 | ✅ 成立 | `byCard.BL42.play` = `db20b8de…` @ `[0].abilityLogic[0].cardAnimations[0]` |
| 7 | 68909cf0… | Jammed Communications | hit | 已在 trigger 桶 | ✅ 成立 | `byCard[68909cf0…].trigger` = `463fe631…` @ `[0].abilityLogic[0].cardAnimations[1]` |
| 9 | SAU26 | Plasmancer | attack_ranged | 已在 attack_ranged 桶 | ✅ 成立 | `byCard.SAU26.attack_ranged` = `4d97059b…` @ `customRangedAttackParticles`（另有 trigger 同 guid） |
| 10 | SAU50 | Dimensional Breach | play | 已在 play 桶 | ✅ 成立 | `byCard.SAU50.play` = `9e2678f1…` @ `[0].abilityLogic[0].cardAnimations[0]` |
| 11 | SAU50 | Dimensional Breach | deploy | 已在 play 桶 | ✅ 成立 | `byCard.SAU50.play` = `ddc4d9a2…` @ `[0].abilityLogic[0].cardAnimations[1]` |
| 12 | 30007162… | Lord of the Storm | hit | 已在 play 桶 | ✅ 成立 | `byCard[30007162…].play` = `26b0fe81…` @ `[0].abilityLogic[0].cardAnimations[0]` |
| 13 | d8a14171… | Relentless March | deploy | 已在 play 桶 | ✅ 成立 | `byCard[d8a14171…].play` = `ba1b8036…` @ `[0].abilityLogic[0].cardAnimations[0]`（该 guid 另被 SAU16/46/57/71/76 引用） |
| 14 | SOR8 | Arco-flagellant | attack_melee | 已在 trigger 桶 | ✅ 成立 | `byCard.SOR8.trigger` = `273bb79f…` @ `[0].abilityLogic[0].cardAnimations[1]` |
| 15 | SOR8 | Arco-flagellant | trigger | 已在 trigger 桶 | ✅ 成立 | `byCard.SOR8.trigger` = `230c929a…` @ `[0].abilityLogic[0].cardAnimations[0]` |
| 16 | SOR51 | Divine Intervention | play | 已在 trigger 桶 | ✅ 成立 | `byCard.SOR51.trigger` = `951b8365…` @ `[1].abilityLogic[0].cardAnimations[1]` |
| 17 | TL72 | Tyranid Invasion | deploy | 已在 play 桶 | ✅ 成立 | `byCard.TL72.play` = `69e644d8…` @ `[0].abilityLogic[0].cardAnimations[1]` |
| 18 | SOR21 | Death Cult Assassin | trigger | 已在 trigger 桶 | ✅ 成立 | `byCard.SOR21.trigger` = `230c929a…` @ `[0].abilityLogic[0].cardAnimations[0]` |
| 19 | SOR10 | Sister Repentia | trigger | 同上 | ✅ 成立 | `byCard.SOR10.trigger` = `230c929a…` @ 同上 |
| 20 | SOR32 | Mortifier | trigger | 同上 | ✅ 成立 | `byCard.SOR32.trigger` = `230c929a…` @ 同上 |
| 21 | SOR27 | Penitent Engine | trigger | 同上 | ✅ 成立 | `byCard.SOR27.trigger` = `230c929a…` @ 同上 |

> 判法：`why` 里给的 `cardAbilities[...]…cardAnimations[...]` 路径，逐条在**同一张卡**的 `authoritative` 条目 `src` 里出现 ⇒ 「同一件已在播、删除只是去重」成立。

## 复核表 B · `unbacked`（6 条，全部成立；1 条细节过期）

| # | id | 卡名 | 事件 | why（摘要） | 今天还成立吗 | 证据 | 若已过期应改为什么 |
|---:|---|---|---|---|---|---|---|
| 0 | ASH86 | Forewarned | play | `Forewarned Board` 零卡引用；视觉由 ASH48 的 `CosmicSerpent` 播；ASH86 的 play 是 `Buff_Khaine` | ✅ 成立 | 卡 SO 全表扫 `a0a1091e…` → **0 张卡**；`byCard.ASH86.play` = `Buff_Khaine`（`ed801be9…`）；`byCard.ASH48.play` = `CosmicSerpent`（`b277fe66…`）prefab `Cosmic Serpent` | — |
| 1 | AM30 | Manticore | play | `Manticore Board Down` 零卡引用；prefab 另由 `Artillery Ground Big` 引用，已在 AM53(play) / **AM18(ability)** | ✅ 核心成立 ⚠️ 细节过期 | `2bb742a0…` → **0 张卡**；prefab `Artillery Manticore down 1x` 命中 `AM53.play`（`39e1aa4a…`）与 `AM18.**trigger**`（`TurnEnd`, trigger 40）；AM30 `abilityCount=0`、无 `ThisCardPlayed(0)` | 把「AM18（ability）」改成「AM18（trigger / TurnEnd 40）」 |
| 2 | AM34 | Valkyrie | attack_ranged | `Valkyrie_MultiLaser_Hover_Target` 只被 AM75 引用；AM34 远程件是 `Atk_BulletImpact_Valkyrie_Missiles` | ✅ 成立 | `7f071e26…` 引用集 = **{AM75}**，且正是 `AM75.play` @ `[0].abilityLogic[0].cardAnimations[1]`；`byCard.AM34.attack_ranged` = `Atk_BulletImpact_Valkyrie_Missiles`（`e4501e8d…`） | — |
| 5 | a84a5b49… | Master Interromancer | hit | `Master Interromancer Hit` 零卡引用；其 prefab 与 play 权威件同一件；卡没有 `UnitDamaged(60)` | ✅ 成立 | `7a4f21e3…` → **0 张卡**；`byCard[a84a5b49…].play` = `Atk_SmiteLightning_white_Down`（`39d3e084…`）prefab **同名** `Psychic_Lightning_down_Intense_white`；卡 SO triggers = **0 / 90**（无 60） | — |
| 6 | EC29 | Heldrake | attack_melee | `HeldrakeStrike` 只被 BL53 引用；近战协程 0 处 CardAnim | ✅ 成立 | `39dca211…` 引用集 = **{BL53}** @ `BL53.play [0].abilityLogic[0].cardAnimations[0]`；`d:/2/tools/decomp_full/CardScript._AttackMeleeAnim_d__357__MoveNext.c` = **313 行 / `CardAnim|AddCardAnim` 命中 0** | — |
| 8 | SAU3 | Flayed One | deploy | `Flayed One Summon` 只被 SAU69 的 Artifice(560) 引用；SAU3 `summonAnimDelayed` 空 GUID | ✅ 成立 | `53f3e4ca…` 引用集 = **{SAU69}** @ `SAU69.trigger`，trigger = **560**；`cards_raw` SAU3 `summonAnimDelayed.m_AssetGUID = ""` | — |

## 本轮留下的两个不可复核点（都不影响上面的结论）

1. **「本条是按名字猜出来的第二次记账」** —— 这是**生成器侧**的来源描述；本轮只核到「同 guid 已权威」，
   没有重跑 `strip_unbacked_inference` / 名字猜测那一跳（复跑会重写 JSON，本轮明令只读）。
2. **「全 24.7 万件解包资产按 GUID+名字双扫」** —— 本轮扫的是 `cards_raw.json`（1,351 SO）+ `trait_collection.json`
   + `card_anim_map.json`；**没有重扫 `assets_full` 全库**。若要把「零引用」升级成全库结论，需另跑一次全库 GUID 扫描。
