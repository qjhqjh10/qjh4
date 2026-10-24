# A10 · 余下 2 个 `unbacked` 桶的溯源（`ASH86` / `AM34`）

> 执行代理 **A10**（特效数据）。日期 **2026-10-24**。仓库根 `d:/4`。
> 承接 **`A9_四个unbacked桶.md`** 的 §七·1 顺手发现（"台账漏了 2 条"）。方法、五层查法、`UNBACKED_EVIDENCE` 写法照 A9 原样。
> ⛔ 未跑 Unity / player / `-executeMethod`；⛔ 未动 git；⛔ 未改正本（`CLAUDE.md` / `项目任务.md`）；⛔ 未碰任何 `.cs`。
> 判据优先级：`d:/2/tools/decomp_full/`（第一）→ 卡 SO 原始类型树（UnityPy `read_typetree`）→ `d:/2/新解包资源/assets_full/` → 导出表。

---

## 〇、一句话结论

**两条一条都没补回，维持静音 + 出声**；两条**都是「链到别的卡」**（不是零引用、不是同 prefab 重复）：

| 桶 | 权威链（本轮新增） | 结论 |
|---|---|---|
| `ASH86 Forewarned / play`（`Forewarned Board` → prefab `Cosmic Serpent`） | 该 CardAnim **全库零卡引用**；它落的 prefab `Cosmic Serpent` 另由 CardAnim `CosmicSerpent` 引用，而后者是 **`ASH48 Cosmic Serpent` 的 `play` 权威件** | **不播正确**（ASH86 自己的 `play` 权威件是 `Buff_Khaine`；补回 = 同一拍播两件） |
| `AM34 Valkyrie / attack_ranged`（`Valkyrie_MultiLaser_Hover_Target` → prefab `Valkyrie_MultiLaser_Hover`） | **全库唯一**的卡 SO 引用 = **`AM75 Valkyrie Drop`** 的 `cardAbilities[0].abilityLogic[0].cardAnimations[1]`（`ThisCardPlayed(0)` → `play`） | **不播正确**（AM34 自己的远程权威件是 `Atk_BulletImpact_Valkyrie_Missiles`，**不是同一件 prefab**；补回 = 把 AM75 的件错播一遍） |

改动：**只把这两条的 `notPlayed[].why` 写具体**（生成器 `UNBACKED_EVIDENCE` 加 2 条），
**事件桶、条目数、`notPlayed` 条数全部不变**（`entryCount` 2672 · `notPlayedCount` 22 · `problems` 0 · `leftInferred` 0）。
⇒ 至此 **6 个 `unbacked` 全部带逐条证据**（A9 的 4 条 + 本轮的 2 条）。

---

## 一、五层查法（逐层可核）

| # | 搜法 | 范围（本轮实跑） | 命中 |
|---|---|---|---|
| ① | **UnityPy 重读类型树**，递归收全部 `m_AssetGUID` + 逐字段列空/非空 | 当前 build 的 allcards `__data`（`D:/2/关服前存档/新建文件夹/Everguild_Warpforge/54bdb7b1…/ea8bca4f…/__data`）；扫到 **1,359 个 MonoBehaviour**，按 `m_Script.m_PathID == 8864902669260471755` 取到 ASH86 / AM34 | 两张卡各 **116 个顶层字段**（ASH86 1,663 个叶子 / AM34 1,513 个叶子）；**非空 GUID 集与 `cards_raw.json` 的 `full_raw` 逐条相同** ⇒ `full_raw` 没漏字段 |
| ② | **GUID 32 位 + 名字**双扫全部解码资产 | `d:/2/新解包资源/assets_full/**/*.json`（91 个 bundle 目录 / 24.7 万件）；GUID = `a0a1091e…`(Forewarned Board) `7f071e26…`(MultiLaser Target) `ccef7d44…`(Cosmic Serpent prefab) `3df06e24…`(MultiLaser prefab) `796d76e0…`(Forewarned_Board prefab) `736978d5…`(Valkyrie 导弹 prefab)；名字 = 文件名 glob + `"m_Name": "<名字>"` 精确串（`Forewarned Board` / `Valkyrie_MultiLaser_Hover_Target` / `Cosmic Serpent` / `Valkyrie_MultiLaser_Hover` / `Forewarned_Board` / `MultiLaser` / `CosmicSerpent`） | 每个 GUID 只命中**它自己的**记录 + **AssetBundle 容器条目**；名字侧只有一份同名资产（**无同名 GameObject/CardAnim 混装**，`className` 过滤后仍是 1 条） |
| ③ | 同上 | `assets_full/sharedassets0` · `sharedassets1` | **0**（GUID 与名字都 0） |
| ④ | 字段级 | `数据/游戏数据/trait_collection.json`（73 `CardTrait` / 75 `TraitEffect`） | **0**（词表扫过 `Forewarned` / `Forewarn` / `CosmicSerpent` / `Cosmic Serpent` / `Valkyrie` / `MultiLaser`） |
| ⑤ | 另一代真值交叉 | 2022 时代 **1,195 张**卡 SO 快照（`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`）+ 派生的 `数据/游戏数据/card_anim_order.json`（897 张有引用） | 同一结论：`Valkyrie_MultiLaser_Hover_Target` **只** AM75；`Forewarned Board` 与两个 prefab **0** |

> ① 的做法（可复跑）：`UnityPy.load(__data)` → 逐 `MonoBehaviour` `read_typetree()` → 按 `m_Script.m_PathID` 认卡 SO →
> 递归收 `m_AssetGUID`（**空串也收**，用来区分「字段不存在」和「字段为空」）。

---

## 二、逐条

### 1) `ASH86` `Forewarned` / `play` —— `Forewarned Board` → prefab `Cosmic Serpent`

**卡 SO 的引用字段（UnityPy 现读，116 字段里只有 5 条非空 GUID）**

| 字段 | 值 | 我们的事件桶 |
|---|---|---|
| `cardImageReference` | `b6162dcd…`（`Craftworld_SaimHann_strat_Forewarned`） | `_assetRefs` |
| `cardAbilities[0].abilityLogic[0].cardAnimations[0]`（trigger `ThisCardPlayed(0)` / `buffOthers(30)`） | `ed801be9…` = `Buff_Khaine`（prefab `KhaineBuff`） | **`play`（权威件）** |
| `cardAbilities[1].abilityLogic[0].cardAnimations[0..2]`（trigger `UseSpiritStone(600)`） | `aa976da2…`(`Eldar_TriggerSpiritstone_Warlord`) / `bbd02bca…`(`Eldar_TriggerSpiritstone_Mana`) / `f17dd2a8…`(`Buff_Eldar_Warp Spider_Target`) | `ability` |
| `targetedSpellAnim` / `nonTargetedSpellAnim` | **`m_FileID=0 / m_PathID=0`（空 PPtr）** | —— |
| `summonAudio` / `attackAudio` / `deathAudio` / `customRangedAttackParticles` / `customDeathParticles` / `remnantBodyPrefab` / `summonAnimDelayed` / `fullHeroSpriteReference` | **全部空 GUID** | —— |

**链到谁**

- `Forewarned Board`（CardAnim `a0a1091e1e8a44c46a876c4467f2f37d`，落在 **Saim-Hann 的** `aeldarisaimhanncardanims` 包）
  → prefab `ccef7d44…` = **`Cosmic Serpent`**。
- **`Forewarned Board` 本身：两代卡 SO 快照 + 全量资产扫描 = 零卡引用**（1,351 现行 / 1,195 的 2022 快照都核过），`CardTrait` 0。
- 但 **`Cosmic Serpent` 这个 prefab 不是孤件**：同一 prefab 还挂在 CardAnim **`CosmicSerpent`**（`b277fe66…`）上，
  而 `CosmicSerpent` **正是 `ASH48 Cosmic Serpent` 的 `play` 权威件**（`cardAbilities[0]` / `triggerSpiritStone(440)`；运行期表 `ASH48.play[0]` = authoritative）。
  另有 `CosmicSerpent_Secondary`（`dd35fee4…`）落 **`Cosmic Serpent_Secondary`**（另一件 prefab），是 ASH48 的 `ability`。
- 🔴 **同名陷阱**：包里另有一个 **只差下划线**的 prefab **`Forewarned_Board`**（`796d76e0…`，`battleprefabs_vfxandmisc`），
  它**全库零引用**、也**没有任何 CardAnim 指向它**；而 `Forewarned Board` 这条 **CardAnim 指向的是 `Cosmic Serpent`**。
  ⇒ ⛔ 别拿那个同名 prefab 反推「这条 CardAnim 应该指向它」。

**结论**：**不播正确**，⛔ 别恢复。ASH86 是法术（`cardType 20`），`play` 那一拍的原版权威件是 `Buff_Khaine`；
两个 spell-anim 指针本来就是空的（走共享 spell anim）。补回 `Forewarned Board` = 同一拍播两件，
且那件美术**本来就在 1,195/1,351 两代数据里都没有主儿**——它更像美术留的半成品变体（prefab 与 ASH48 共用）。

---

### 2) `AM34` `Valkyrie` / `attack_ranged` —— `Valkyrie_MultiLaser_Hover_Target` → prefab `Valkyrie_MultiLaser_Hover`

**卡 SO 的引用字段（现读 116 字段里 6 条非空 GUID）**

| 字段 | 值 | 桶 |
|---|---|---|
| `cardImageReference` | `8a7eb5e6…`（`AstraMilitarum_veh_Valkyrie`） | `_assetRefs` |
| `summonAudio` | `1792f5f1…`（`VO_AM_Valkyrie - Prepare for rapid insertion!`） | `audio.deploy` |
| **`customRangedAttackParticles`** | **`e4501e8d…` = `Atk_BulletImpact_Valkyrie_Missiles`（prefab `BulletImpact_ValkyrieMissile`）** | **`attack_ranged`（权威件）** |
| `activeAbility.activeAbilityLogic[0].cardAnimations[0..1]`（`doDamage(10)`） | `a8c3f891…`(`DutyAnim`) / `34fac67e…`(`Atk_GatlingSweep`) | `ability` |
| `cardAbilities[0].abilityLogic[0].cardAnimations[0]`（trigger `UnitDeathByAbility(180)` / `summonUnit(70)`） | `1e56d919…` = `Guard Summon` | `death`/召唤 |
| `targetedSpellAnim` / `nonTargetedSpellAnim` / `summonAnimDelayed` / `attackAudio` / `deathAudio` / `customDeathParticles` / `remnantBodyPrefab` / `fullHeroSpriteReference` | **全空** | —— |

**链到谁**

- `Valkyrie_MultiLaser_Hover_Target`（CardAnim `7f071e26b37bdf642aad6b6472dc5e5e`，`astramilitarumcardanims` 包）
  → prefab `3df06e24…` = **`Valkyrie_MultiLaser_Hover`**。
- **全库唯一**的卡 SO 引用 = **`AM75 Valkyrie Drop`**：`cardAbilities[0].abilityLogic[0].cardAnimations[1]`
  （同一条 logic 的第 0 步是 `Valkyrie_Hover_Dust`，第 1 步才是它；`ThisCardPlayed(0)` → **`play` 桶**，运行期表 `AM75.play[1]` = authoritative）。
  **两代快照（1,351 现行 / 1,195 的 2022）都只有这一处。**
- AM34 自己的远程件是**另一件 prefab**：`Atk_BulletImpact_Valkyrie_Missiles` → `BulletImpact_ValkyrieMissile`（`736978d5…`），
  与 `Valkyrie_MultiLaser_Hover` **不是同一件**（⇒ 不适用 A9 §二·4 那种"已在别拍播、视觉没丢"的判定）。

**结论**：**不播正确**。把 AM75 的"Valkyrie 空投扫射"演出安到 AM34 的远程攻击上 = 事件归属错，
且 AM75 打出时同一件会**播两遍**。原版 AM34 的远程就只有 `customRangedAttackParticles` 这一个逐卡字段
（116 字段里没有第二个"远程"槽）。

---

## 三、改动清单（全部在白名单内）

| 文件 | 改了什么 | `git diff --numstat` |
|---|---|---|
| `D:/4/Unity/工具/gen_card_vfx_by_card.py` | `UNBACKED_EVIDENCE` **加 2 条**（`:1604` `("ASH86","Forewarned Board")`、`:1615` `("AM34","Valkyrie_MultiLaser_Hover_Target")`）＋ 表头注释更新为"现覆盖全部 6 个 `unbacked`"。**只加文字，不改任何判定分支** | `76/0`（含 A9 上一轮未提交的 `+54`） |
| `D:/4/Unity/数据/游戏数据/card_vfx_by_card.json` | 同一次运行产出：`stats.inferenceGone.unbacked[]` 2 条 + `stats.notPlayed[]` 2 条 `why` + `generatedAt` | `13/13` = `generatedAt` 1 + 6 条证据 × 两处 = 13（与 A9 的 4 条同构） |
| `D:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json` | 同一次运行产出：`notPlayed[].why` 2 条 + `generatedAt`（单行文件） | `1/1` |
| `D:/4/Unity/资料/普查产出_1024/A10_余下两个unbacked.md` | 本报告 | 新文件 |

**没有改**：任何 `.cs`（`VfxMap` / `TraitFrames` / `AnimFXCheck` / `BattleScene` 均未碰）· `RuleEngine/**` · 两张正本 · git。
**没有补回任何条目**（没有「按名字猜」的补回）。

---

## 四、验收（实跑结果）

```bash
cd d:/4/Unity
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" 工具/gen_card_vfx_by_card.py --quiet \
  --generated-at "2026-10-24 10:20:00"          # ① 第一次
cp 数据/游戏数据/card_vfx_by_card.json /tmp/full_run1.json
cp MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json /tmp/rt_run1.json
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" 工具/gen_card_vfx_by_card.py --quiet \
  --generated-at "2026-10-24 10:20:00"          # ② 第二次
cmp /tmp/full_run1.json 数据/游戏数据/card_vfx_by_card.json && echo FULL_IDENTICAL
cmp /tmp/rt_run1.json   MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json && echo RT_IDENTICAL
```

**本轮实际结果**：

| 项 | 结果 |
|---|---|
| 确定性（两次跑逐字节） | `FULL_IDENTICAL` / `RT_IDENTICAL` |
| 计数闸 | `22 2672 0 0`（`notPlayedCount` / `entryCount` / `problems` / `leftInferred`） |
| 全量件与运行时件一致 | `stats.notPlayed == rt.notPlayed` → **True** |
| `notPlayed` 构成 | `16 duplicate + 6 unbacked`（未变） |
| 行尾 | 全量件 `CRLF=0 / LF=86104`；运行时件 `CRLF=0 / LF=0`（单行无换行）；生成器 `CRLF=0 / LF=2307` |
| `git diff --numstat` | 生成器 `76/0` · 全量件 `13/13` · 运行时件 `1/1`（**数字对得上，没翻行尾**） |

> ⚠️ `gen_card_vfx_by_card.py` **没有 `--check` 开关**（A9 已核，本轮复核仍无）⇒ 验收用「固定 `--generated-at` 连跑两次 + `cmp`」。

---

## 五、没查清 / 没做

1. **没有原版实况**：原版已关服、后端不可达 ⇒ 本报告结论全部来自**卡 SO 原始类型树 + 全量资产扫描 + 两代快照**，不是实拍。
   若将来能进原版看，优先看 **ASH86 打出**（预期：只有 `Buff_Khaine` 那一件）与 **AM34 远程攻击**（预期：导弹命中那件，不是空投扫射）。
2. **没有给生成器新增 `--check`** —— 那会改脚本对外接口，属「发明口径」，留调度台裁。
3. **`AM34 / Valkyrie_Hover_Dust`（`_unclassified` 那条）没改** —— 不在本轮「两条」范围内（见 §六·1，证据已备好）。
4. **`ASH48` 的 `Cosmic Serpent` 在 2022 快照里没有 anim 引用** —— 那份快照比现行 build 旧（`card_anim_order.json` 的 `_coverage` 也显示只有 897/1,195 张卡有引用），
   不构成反证；本轮用的是「现行 SO + 运行期表」判 ASH48 的权威档。**没去追** ASH48 在 2022 版是不是换了另一件。

---

## 六、顺手发现（交给调度台分流）

1. 🟡 **`AM34 / Valkyrie_Hover_Dust`（`_unclassified`，全量件里有、运行期 `notPlayed` 里没有）也是「链到别的卡」。**
   它的 CardAnim `Valkyrie_Hover_Dust`（`c438c0c6a15006a489aa78cb202fbc17`，prefab `Valkyrie Flyover Hover Dust`）
   在**现行运行期表里是 `AM75 Valkyrie Drop` 的 `play[0]` 且为 authoritative**（`cardAbilities[0].abilityLogic[0].cardAnimations[0]`）。
   ⇒ 若调度台要给这条也补证据，措辞可直接用「全库唯一引用 = AM75 的 play 首步」。
   本轮**没改**它（不在「两条」范围内，避免越界）。
2. 🔴 **`guid_map_bundles.tsv` 的 `uniqueId` 回填不完整，不能当"无引用"的独立判据。**
   实测反例：`Valkyrie_Hover_Dust`（`c438c0c6…`）**明明被 AM75 的卡 SO 引用**（生成器从卡 SO 读出并落了 authoritative），
   但该 guid 在 `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` 里**只有 bundle-container 行、没有 uniqueId 行**。
   ⇒ 以后谁想"用 guid_map 有没有 uniqueId 行"来判零引用，会**得到假阴性**。**判零引用必须走卡 SO 类型树 / `cards_raw` 逐字段扫。**
   （本轮 ASH86 那条的措辞已按这条订正：**不引用 guid_map 的缺行作为证据**。）
3. 🟡 **包里有一个"孤儿 prefab"`Forewarned_Board`（`796d76e0…`，`battleprefabs_vfxandmisc`）**：
   全库零引用、没有任何 CardAnim 指向它；而**同名的 CardAnim `Forewarned Board` 指向的是 `Cosmic Serpent`**。
   ⇒ 典型的「名字近似但不同件」，已写进对应 `why`，防止下个会话拿它反推。
4. 🟢 **`notPlayed` 的 6 条 `unbacked` 现已全部带逐条证据**（A9 4 条 + 本轮 2 条）⇒ 台账里"6 条 unbacked 无证据"这个口子可以核销。
5. 🟢 **同名混装这次没有撞到**：`Forewarned Board` / `Valkyrie_MultiLaser_Hover_Target` 在全库都只有**一份 CardAnim**，
   而 `Cosmic Serpent` / `Valkyrie_MultiLaser_Hover` 只有 **GameObject（prefab）**、没有同名 CardAnim。
   （A9 提醒的 pitfall 仍然成立：手工建 name→GUID 表**必须带 `className` 过滤**；本轮走 `cardanim_guid_by_name()` 的过滤，结果只有 1 条。）
