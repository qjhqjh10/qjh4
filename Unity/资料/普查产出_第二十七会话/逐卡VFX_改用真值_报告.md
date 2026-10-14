# 逐卡 VFX 表 · 改用原版真值 — 报告（第二十七会话 · 2026-10-14）

> 交付物：`工具/gen_card_vfx_by_card.py`（改）· `数据/游戏数据/card_vfx_by_card.json`（重出）
> 判据：`导出/cards_1195_2022时代.json`（原版卡 SO 全字段）+ `资料/普查产出_第二十七会话/guid_map.tsv`
> + `数据/索引/anim_address_map.json`（`cardanim_to_asset`）

---

## 一、结论

1. **新档落地**：生成器新增 `conf = authoritative`（**原版真值**，不是推断），优先级最高；
   旧三档 `inferred-name` / `inferred-keyword` / `inferred-envcard` **一条都没删**，降级为兜底。
2. **数字（重出前 → 重出后）**：`covered` **465 → 1017** / 1126 ·
   `authoritativePerCardEntries` **0 → 3326**（其中 VFX **1773** 条 · 非 VFX 的 `_assetRefs` 1553 条）·
   `authoritativeCards` **0 → 956**（只靠 VFX 条目就够的 **895** 张）。
3. **旧档逐条未变**（对旧产物逐条比）：`inferred-name` 59 条 / 42 张 · `inferred-keyword` 555 条 / 429 张 —— **完全相同**。
4. 🔴 **验收标准 (b) 的「≥ 1000 张」【达不到】，而且不是实现问题**：
   我们卡池 1126 张里 **170 张在真值快照里根本不存在**（快照是 2022 时代、少后来加的
   SpaceWolves / Ultramarines / DarkAngels 等）⇒ **任何匹配方式的上限 = 1126 − 170 = 956 张**。
   「只算 VFX 条目」的上限更低（895）。本档已取到上限。**如实报数，没有拿别的来源垫**。
5. **不许静默失败**已落地：解不出的 **24 条**逐条留在 entry 的 `unresolved` 上（见 §三末）；
   另有 **2 条** `_assetRefs`（就是账上那 2 个已知坏 GUID）同样标出。

---

## 二、新增那一档怎么定的

**链路（判据齐、可逐字复核，**不是**按卡名/关键词推）**：

```
原版卡 SO 字段（`AssetReference`，存 GUID）
  → `guid_map.tsv`：GUID → 束名 / 容器键 / 资产原名 + `m_Script` 类名
  → 类名 == `CardAnim` → `anim_address_map.json.cardanim_to_asset`（`animInfo.animAdressable`）→ VFX prefab
  → 类名 == `GameObject` → 它本身就是 prefab
```

样本 `EC29 Heldrake`：`customRangedAttackParticles = 564af14f…` → `EC Heldrake Attack`（`CardAnim`）
→ prefab **`EC Heldrake Attack`**（`inLibrary: true`）。出处：`甲2_GUID到资源_调查.md §2·4` 同一条链。

**事件归口**（`soField` → 事件）：
`customRangedAttackParticles`→`attack_ranged` · `customDeathParticles`/`remnantBodyPrefab`→`death` ·
`summonAnimDelayed`→`deploy` · `cardAbilities[].abilityLogic[].cardAnimations[]` 与
`activeAbility.activeAbilityLogic[].cardAnimations[]`→**`ability`**。

🔴 **最后那一条要看清**：**归到 `ability` 是【我们拍的】**（`VfxMap.Ability`＝部队卡在场上发动技能）。
原版真正给的是 `CardAbility.trigger`（`AbilityTrigger` 枚举）与 `AbilityLogic.chosenAbility`
（`AbilityEffect` 枚举），**两个原样带在每条条目上**
（`abilityTrigger`/`abilityTriggerName`/`chosenAbility`/`chosenAbilityName`）⇒ 要精确到事件下游自己判。

**卡与真值的匹配（两段，各记 `joinKey`）**：① 卡 id 完全相同（**837** 张）；② 否则
**卡名归一化相同 + 阵营相同**（**119** 张）。⚠️ **阵营闸必须要有**：实测
`EC_Terminator_Champion` / `EC_Maulerfiend`（帝皇之子）在真值里**只有** `BL29`/`BL31`（黑色军团）同名卡，
不加闸就会**把别家的动画安到自家卡上**（那 2 张已如实记进 `nameJoinDroppedByFaction`，落旧档）。
同名多卡时优先取「`uniqueId` 是正常卡 id」的那一份；仍有歧义 **8 张**全取并打 `nameJoinAmbiguous`。

---

## 三、逐字段覆盖率（只统计「我们卡池 ∩ 真值」的 956 张卡）

| 卡 SO 字段 | 真值非空条目 | 解出条目 | 真值非空张数 | 出的事 |
|---|---|---|---|---|
| `cardImageReference` | 965 | **965** | 956 | — （进 `_assetRefs`，art） |
| `customRangedAttackParticles` | 439 | **439** | 439 | — |
| `customDeathParticles` | 61 | **61** | 61 | — |
| `remnantBodyPrefab` | 68 | **68** | 68 | — |
| `summonAnimDelayed` | 1 | **1** | 1 | — |
| `cardAbilities[].abilityLogic[].cardAnimations[]` + `activeAbility…` | 1204 | **1182** | 726 | 见下 |
| `summonAudio` / `attackAudio` / `deathAudio` | 505 / 41 / 42 | **505 / 41 / 42** | 同 | （进 `_assetRefs`，audio） |

**解不出来的 24 条（全部留在 entry 上，`prefab: null` + `unresolved`）**：

| 条数 | 谁 | 原因 |
|---|---|---|
| 11 | `AM61` `DA60` `UM52`（GUID `5251c0f9…`）· `SOR43` `SOR48` `SOR52` `SOR55`（GUID `febb0660…`） | **GUID 在目录里有、本地束里没有那个容器键**（两个 build 的差）—— 🔴 **这是账上没记过的【新】**，见 §七 |
| 13 | `SAU65/47/55` `SAU_Master_Chronomancer` `SAU46/31` `GOF3/15/2/30/47` `UM39/28` | CardAnim 名是 `Wait_1s` / `Wait_1,5s` / `Wait_300ms` / `Wait_2s` —— **原版是纯计时、没有 `animInfo.animAdressable`**（不是解错，是本来没有） |
| 2（`_assetRefs`） | `BL47` 的 `summonAudio` · `GOF2` 的 `deathAudio` | 就是账上那 2 个已知坏 GUID（`fda6e5bc…` / `11641bc6…`） |

另：权威档共 **557** 个不同 prefab 名，其中**只有 1 个不在我们的效果库**（`Teleport Trigger Board Effect Only`）。

---

## 四、旧档降级说明

- **一条都没删**：对旧产物逐条比，`inferred-name` 59 条 / `inferred-keyword` 555 条**逐条仍在**，`src`/`why` 一字未动。
- **升档的卡**：**552 张**卡原来**一个条目都没有**、现在有了 `authoritative`（另有 404 张原来有旧档、现在两者都有）；
  合并后**只有旧档、没有任何权威条目**的卡 = **61 张**（= 1017 covered − 956 authoritative）。
- **只有旧档的卡为什么是它们**：① 170 张快照里没有（真值年代）② 61 张快照里有、但**它的卡 SO 里那 6 个
  VFX 字段全空**（例：`BL1 Chaos Spawn` / `BL5 Hound of Abaddon` 有 `cardAbilities: 0`）。
- **优先级**（写进 `_conventions.confPriority`，消费者取第一个命中的）：
  `authoritative > exact > exact-not-per-card > inferred-envcard > inferred-name > inferred-keyword`。
- 同一事件下，权威条目**排在推断条目前面**（`merge_authoritative` 前插），旧档**没有**被覆盖删除。

---

## 五、改了哪几行注释（铁律 5：就地改正 + 保留痕迹）

| 文件:行（改后） | 改前一句 | 改后一句 |
|---|---|---|
| `工具/gen_card_vfx_by_card.py:48-66` | 「**第 2 跳（卡 → 它自己的 animInfo 列表）在本地断掉**…⇒ **逐卡覆盖拿不到**」 | 「⚠️ **2026-10-14 更正**：原来写 X，**实际是 Y（通）**，错因 Z（把「载体的文件不在本地」当成「那一跳断掉」）」 |
| `:70-74` | 「所以这份表是**三层拼起来的**」 | 「**四层**拼起来的」+ 新增 `authoritative` 那一条的说明 |
| `:420-422` | 「卡自己的 `summonAnimDelayed`（逐卡延迟特效）**在远端包里，本地没有**」 | 「是另一条，见 `byCard[*].deploy` 的 `conf = authoritative`」+ 更正句 |
| `:509-511` | 「其余阵营走 `customRangedAttackParticles`（**逐卡，远端包**）」 | 「逐卡；⚠️ 更正：原来写「远端包、本地没有」——**错**」 |
| `:514-518` | 「`AlternateArtCard.attackAnim`（**逐卡，远端包**）⇒ 本地判不了」 | 「前半句错；后半句仍成立但理由换：真值里 `attackAnim` 只有 2 张且**都是空引用** ⇒ **判据为空**（铁律 11 例外①）」 |
| `:1131-1133`（`chain` hop 2） | `conf: "broken"`，「**断** —— 判据在 `allcards_assets_all.bundle`」 | `conf: "authoritative"`，「✅ **通**」 |
| `:1100-1128`（原 `broken_hop2` 整块） | 键名 `broken_hop2`，5 条「断链证据」 | 改名 **`hop2_corrected`**，含 `was` / `now` / `wrongBecause` / `evidence` / `limits` |
| `:1176-1185`（`_headline`） | 「逐卡那一跳…**在本地断掉** … 每一条都是推断」 | 「🔴 **2026-10-14 更正：原来写 X，实际是【通的】**…」 |
| `:675-680`（原来是第 675 行的 print） | 「↳ **判据齐的逐卡条数 = 0**（原版那跳在远端包里）」 | 改成打 `conf` 优先级 + `[3/4]` 那一行报权威档条数/张数 |
| `stats.authoritativePerCardEntries` | **硬编码 `0`** | 真数（并新增 `authoritativeVfxEntries` / `authoritativeCards` / `authoritativeCardsVfxOnly` / `authoritative`） |

`grep` 了「第 2 跳 / 远端包 / allcards / hop 2」在本文件内的**全部**出现处，逐处改完（上面 10 处）。

---

## 六、还没查清 / 还差什么

1. **`CardTrait.TraitEffects[].effectAnim`（天赋触发那一支）没做** —— 那份 SO 不在卡 SO 里，
   本地也没找到（这一支的 `trigger` 事件仍只有 `inferred-keyword`）。**账上仍未清**。
2. **`alternateArts[]` 是 PPtr**（不是 AssetReference，本地无 externals 表），`attackAnim` 真值里全空 ⇒ 异画那一路未打通。
3. **近战那一支**（`card3DAnimationController`）在卡 SO 里根本没有字段 ⇒ 仍然是空的。
4. **170 张卡没有真值**（快照年代）；**61 张卡有真值但 SO 里 VFX 字段全空**。两者都只能落旧档。
5. **2 个 VFX GUID 解不出**（`5251c0f9…` / `febb0660…`，见 §三）—— 目录与本地束不是同一个 build，
   本轮**没查**「是不是本地还有别的束带这个容器键」，也没解 `catalog_main.bin`。
6. **缓存目录 `C:/…/Everguild/Warpforge/com.unity.addressables/catalog_main.json` 当前是否还在**
   （本机 alive 时才有）—— 生成器**不依赖它**（只用 `guid_map.tsv`），但重建 `guid_map.tsv` 要它。

---

## 七、顺手发现（请调度台分流）

1. 🔴 **两个 VFX GUID 解不出，账上没记过**：`5251c0f9139cdbc4a9d04097422966bb`（`AM61` / `DA60` / `UM52`）
   · `febb0660d6738624fb3cefa66cdde9bb`（`SOR43` / `SOR48` / `SOR52` / `SOR55`）。
   两者**在目录里是有的**（`_guid_resolve_probe.py g` 解到 `spacemarinesultramarinescardanims` /
   `sororitascardanims` + `tweenandshakes`），**但本地束里没有那个容器键** ⇒ 与 `甲2` 报告 §四·3
   「目录 JSON 与本地束不是同一个 build」同源。**建议**：这 7 张卡在接 `byCard` 时会静默少一条动画。
2. 🔴 **`tweenandshakes_assets_all.bundle` 本地有 74 件 tween 资产**（`Shake Hit Small` · `Impact Light Tween` ·
   `Recoil Normal Tween` …），**这次是第一次真的进去数过**。这正是 `兜底自设计_总清单.md` **#17/#18**
   要找的「逐卡 `UnitTweenSO` 绑定」那一族 —— **兜底清单那两行的「存疑」可以直接改成「能给」**（资产在本地）。
   我没有动它（越界），只报上来。
3. **我们卡池里 13 张卡的卡名和原版有拼写出入**（difflib ≥0.88 命中）：
   `BL_Sylar_Hexcorn`↔`Sylar Hexscorn` · `DA_Sergeant_Naaman`↔`Sergeant Taaman` ·
   `SOR_Sisters_Repentia`↔`Sister Repentia` · `TAU_Saviour_Protocolst`↔`Saviour Protocols` ·
   `BL_Hellfire_Torch/Pit`↔`Helfire Torch/Pit` …（多数是**我们这边**错）。**没改**（卡名是 `cards_engine.json` 的事）。
4. **`EC_Terminator_Champion` / `EC_Maulerfiend` 在快照里只有黑色军团同名卡** ⇒ 我们把帝皇之子那两张
   判成了「有真值」还是「没真值」，**本轮按「没真值」处理**（阵营闸挡掉）；若它们真是 EC 的卡，
   说明快照确实缺 EC 后期内容。
5. **`cardImageReference` 就是原版立绘贴图名**（如 `CSM_EmperorsChildren_veh_Heldrake`），
   956 张卡逐 id 全有 —— 比现在的「按名模糊配图」硬，`import_original_art.py` 那条链可以用它当判据。
6. **运行时没人读这张表**：全仓扫过，`byCard` 没有任何 `.cs`/`.py` 消费（只有注释引用 `generic.*`）⇒ 本次 shape 变化零影响。
   ⚠️ **但文档有引用**：`资料/普查产出_第五会话/查证_26逐卡AnimInfo可达性.md:10,98` 两次点名本表的 `broken_hop2`
   —— 该键已改名 **`hop2_corrected`**（在 `资料/` 白名单外，**我没改**，请调度台处置）。
