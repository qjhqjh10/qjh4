# 逐卡 VFX 表 —— 链怎么接、断在哪（2026-09-29）

> 产物：
> · `d:/4/Unity/数据/游戏数据/card_vfx_by_card.json`（243 KB）
> · `d:/4/Unity/工具/gen_card_vfx_by_card.py`（可重跑、幂等、只读 `d:/2/`）
> · 本文件
>
> 跑法：`PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_card_vfx_by_card.py`
> （`--quiet` 只打统计；连跑两次除 `generatedAt` 外逐字节相同 —— 已实测）
>
> 正本待办 = `资料/待办判据_战场与战斗视图.md` 第 7 条（「逐卡 VFX 那三张表一条都没接」）。

---

## 〇、一句话结论

🔴 **原版那条链的第 2 跳（「原版卡 → 它自己的 animInfo 列表」）在本地是断的**：
逐卡绑定写在**卡预制体**（`RawCardScript` / `AbilityLogic` / `CardTrait`）上，而那些预制体只在
**远端 CCD 的 `allcards_assets_all.bundle`** 里 —— 本地 84 个包里没有、Addressables 缓存里也没有。

⇒ **本表 `byCard` 里「判据齐的逐卡条目 = 0」**，每一条都是带 `conf` 的**推断**；
判据齐的那一块是 `generic`（共享 `CardPrefab` 上的**通用**挂钩），但它**不是逐卡**。
`VfxMap.ByCard` 要真填满，得先拿到 `allcards_assets_all.bundle`（见 §二）。

---

## 一、链的每一跳（一行一跳）

| # | 从 | 到 | 怎么接的 | 判据 / 出处 | 结论 |
|---|---|---|---|---|---|
| 1 | 我们的卡 id | 原版那张卡 | **同一套 id**（我们的 id 就是原版 id，不是映射出来的） | `assets_full/bundle_prebuiltdecks_assets_all/MonoBehaviour/ASH_SK_2.json`、`bundle_draftpacks_assets_all/MonoBehaviour/Shoulders of Giants.json` 里的 `cardIds` 就是 `ASH53/ASH32/ASH35` 这种串；`数据/游戏数据/card_ids.json` 的 note 也写了同一口径 | ✅ **齐** |
| 2 | 原版卡 | 它自己的 animInfo 列表（每事件一条） | **接不上** —— 原版把它放在卡预制体上：`RawCardScript.{summonAnimDelayed, targetedSpellAnim, nonTargetedSpellAnim, customRangedAttackParticles}` · `alternateArts[].attackAnim` · `cardAbilities[].cardAnimations`(`AbilityLogic`) · `CardTrait.TraitEffects[].effectAnim` | 见 §二（五条独立证据） | 🔴 **断** |
| 3 | animInfo | `animAdressable` 的 GUID | 三张老表逐条交叉校验 | `animinfo_0824.json`(591) · `card_anim_map.json`(506) · `animinfo_lookup.json`(418)：`gen_anim_address_map.py` 每次重跑复核，**guid 0 条不符** | ✅ **齐** |
| 4 | GUID | 那张 CardAnim / VFX prefab | 各包 `AssetBundle/m_Container`（`guid → pathId`）+ UnityPy 重读原 bundle 反解 `(类型, 名字)` | `数据/索引/anim_address_map.json`：**1099 个 CardAnim 解出 1067**，解出的**全是 GameObject、全在 `bundle_battleprefabs_vfxandmisc_assets_all`** | ✅ **齐** |
| 5 | CardAnim 名 | 本地 prefab 名 | 同一张表的 `cardanim_to_asset[*].targetName` | 同上 | ✅ **齐** |
| P1 | 我们的卡**名** | 同名 CardAnim | 归一化后前缀匹配（`Acid Spray` → `AcidSpraySweepAttack_troop`）；事件靠词根/后缀判 | **原版不是这么找的** ⇒ 只是名字恰好对得上 | ⚠️ **推断** |
| P2 | 卡的**关键词** | `<关键词>TraitStart/TraitPlay/TraitTrigger` | 原版天赋动画的**构词法**（`ArmourTraitStart` · `SilenceTraitTrigger` · `VanguardTraitPlay`…） | 「哪个关键词配哪个 anim」真正判据在 `CardTrait` SO，**本地 0 命中** | ⚠️ **推断** |
| P3 | 共享卡预制体 `CardPrefab` | 每事件的**通用**挂钩 | 读 `MonoBehaviour_1744609728290659264.json` 的字段 + `RangedAttackParticlesByArmy` + `BattleAnims` | **判据齐**，但**不是逐卡** | ✅ **齐（非逐卡）** |

---

## 二、断在哪、差什么（证据 5 条，别重复查）

**缺的就是 `allcards_assets_all.bundle`** —— 里面是 `RawCardScript` 资产（远端 catalog 里明写着）。

1. **两份游戏安装的 bundle 文件集完全相同、都只有 84 个**，`ls | grep allcards` = 空
   （`d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64` 与
    `d:/2/unity_run_ref/…/StandaloneWindows64`，`diff` 两边的文件列表 **IDENTICAL**）。
2. **`catalog_main.json` 把 `allcards_assets_all.bundle` 列在「走 CCD 的 4 个包」里**
   （另 3 个：`alternateartstyles` / `localization` / `remotemisccontentdata`），
   远端路径 = `https://d03469d2-…client-api.unity3dusercontent.com/…/entry_by_path/content/?path=/allcards_assets_all.bundle`。
   catalog 里那一条的资产名就是 **`RawCardScript`**。
3. **Addressables 下载缓存里只有 catalog、没有包**：
   `<LocalLow>/Everguild/Warpforge/com.unity.addressables/` 只有 `catalog_main.{bin,hash,json}`（2.9 MB）。
   （`catalog.bin` 里搜 `allcards` 命中 3 处 ⇒ 这份 catalog 是**远端 catalog**。）
4. **本地 24.7 万份 dump 里，`RawCardScript` 的字段名 0 命中**：
   `summonAnimDelayed` · `nonTargetedSpellAnim` · `targetedSpellAnim` · `customRangedAttackParticles` ·
   `remnantBodyPrefab` · `relatedCard1` · `cardShortId` —— 逐个搜，**全 0**；
   只有**一份**共享卡预制体带 `cardImage`/`normalSummon`/`uniqueInGameID`（= `CardPrefab`，见 §三）。
5. **反向搜 GUID**：把 `bundle_*cardanims*` 里 **1100 个 CardAnim 资产的 guid** 当模式全库搜，
   命中的**只有各包的 `AssetBundle/m_Container`**（= 没有任何内容资产引用任何 CardAnim）
   ⇒ 卡预制体确实不在本地。
   ⚠️ 顺带一条**假阳性要排除**：`bundle_chaosspacemarinesemperorschildrencardanims` 与
   `…cardassets` 两个包的容器键**不是 GUID**（是 `'0'`/`'0c'`/`'1b'` 这种 1~3 字符短 hex，52 条）
   —— 拿它们当搜索模式会匹配到一大片文件，**必须按 `len(guid)==32` 过滤**。

**怎么补**：拿到 `allcards_assets_all.bundle` 后，读每张卡的
`RawCardScript`（`cardShortId` 对 id）+ 它引用的 `CardAnim`，把 `CardAnim → prefab` 走
`anim_address_map.json` 或直接读 `animInfo.animAdressable`，本脚本 §三·generic 那套解析链可原样复用。
⚠️ **只有这一条路**：本地既没有卡 prefab，也没有 `CardTrait` SO（`customObject`/`_traitEffects`/
`effectIcon`/`effectAnim`/`TriggerType` 逐个搜过，0 命中）。

---

## 三、表里到底有什么（三层，按 `conf` 过滤）

```
stats.cardsInPool            = 1126
stats.cardsCovered           = 466      ← ⚠️ 全是推断
stats.cardsUncovered         = 660
stats.authoritativePerCardEntries = 0   ← 判据齐的逐卡条目：一条都没有
stats.entriesByConf          = inferred-name 59 条 / 42 张 · inferred-keyword 555 条 / 430 张
stats.cardsCoveredByEvent    = trigger 357 · ability 137 · play 6 · deploy 4 · hit 3 · attack_melee 2 · attack_ranged 2 · _unclassified 31
```

### ① `generic` —— **判据齐**（原版每张卡共用的那条通用链，**非逐卡**）

| 事件 | prefab | 判据（字段级） | 效果库里有没有 |
|---|---|---|---|
| `deploy` | `Invoke Minion Card Fade Default` | `CardScript.normalSummon`（CardAnim）→ 它的 `animAddressable` 指同名 GameObject | ✅ |
| `deploy` | `Invoke Minion Card Fade Legendary` | `CardScript.legendarySummon` | ✅ |
| `deploy` | `Invoke Minion Hits Ground` (+`… Legendary`) | `CardScript.landOnGroundParticles`（旁边 `timeToLand=0.2` / `timeBeforeLand=0.05`） | ✅ |
| `hit` | `AttackHitSmall` | `CardScript.attackHitSmallParticles` —— 🔴 原版 `attackHitBigParticles` **指向同一个 pathId** | ✅ |
| `death` | `Card 3D Death Explosion` | `CardScript.cardDestroyFX`（`UnitDeath` 里 `Instantiate(cardDestroyFX, 卡位置, 3D体旋转)`） | ❌ **库里没有**（唯一一件缺口，见 §六） |
| `heal` | `Healing_Circles` | 全局槽 `BattleAnims.heal`（guid `4ac621b3…`）；同表**其余 12 个槽全是空引用** | ✅ |
| `attack_ranged` | 按阵营：10→`BulletImpact_2shot_trail` · 20→`BulletImpact_2shot_trail_ork_NEW` · 40→`NecronGauss` · 50→`BulletImpact_2shot_trail_chaos` | `RangedAttackParticlesByArmy.rangedParticlesByClan`（**原版只有这 4 个阵营**） | ✅ 4/4 |
| `deploy`（阵营召唤圈） | `deploySummonCandidates`：`AeldariSummon→BlueSummonCircle` · `RelentlessMarchSummon→GreenSummonCircle` · `Tau_Summon`/`Tau_Kroot_Summon→Tau_SummonCircle` | 这几个 CardAnim 的 `animInfo.animAdressable`。⚠️「哪个 CardAnim 属哪个阵营」**没有本地字段**（名字前缀而已）⇒ 只列清单、不硬绑 | ✅ 3/3 |

> `inLibrary` 是脚本逐条现查的（`Assets/WarpforgeVFX/Prefabs/<名>.prefab` 在不在），不是抄的清单。
> 全表缺口只有一条，汇总在 `generic.notInVfxLibrary`。
>
> ⇒ **这 8 个槽建议直接拿去替换 `VfxMap.ByEvent` 里现在的替代品**（那些是当初「没得选才挑的」）；
> 但**近战那一支仍然没有**：原版近战特效挂在卡的 3D Animator（`card3DAnimationController`）
> + `AlternateArtCard.attackAnim`（逐卡，远端包）。

### ② `byCard` —— **全是推断**，逐条带 `conf` / `src` / `why`

- `conf: "inferred-name"`（59 条 / 42 张）：卡名归一化后是某个 CardAnim 名的前缀。
  例：`EC38 Heldrake → HeldrakeStrike`（判成 `attack_melee`）、
  `SAU9 Flayed One → Flayed One Summon`（判成 `deploy`）、
  `ASH_Forewarned Forewarned → Forewarned Board`（判成 `play`，其 prefab 是 `Cosmic Serpent`）。
  事件按词根判（`summon/spawn`→deploy、`strike/slash/claw`→attack_melee、
  `bullet/las/gauss`→attack_ranged、`damage/hit`→hit、`board/battleground`→play），
  **判不出的进 `byCard[id]._unclassified`**（31 条，例：`BL62 Execution → Execution_BlackLegion`），不硬塞。
- `conf: "inferred-keyword"`（555 条 / 430 张）：卡的关键词 → `<关键词>Trait*` 动画。
  例：`AM26 Armoured Sentinel`（kw `Armour 1`）→ `ArmourTraitStart` → prefab `ArmourEffect`。
  ⚠️ 每条都带 `traitTriggerType`（原始的是 `EffectTriggerType.Start0/OnPlay2/Trigger3`）——
  **归并成我们那两个事件名（`trigger`/`ability`）是降信息的**，别当成同一件事。
- `conf: "inferred-envcard"`：**0 条**。进攻卡（环境效果卡）那一族**不在我们卡池里**
  （`EnvironmentalEffectCardsSO` 的卡是 PPtr、`m_FileID=4` 且本地没有 externals 表 ⇒ 连卡名都解不出），
  39 个候选名一个都没在 1126 张里对上 ⇒ 全进 `envcardUnmatched`。那一族另有正本
  = `数据/游戏数据/offensive_cards.json`（§25 那条线）。

### ③ 660 张「一个事件都没有」的原因

| 原因 | 张数 | 例子 |
|---|---|---|
| 卡**一个关键词都没有**（纯战术/防御/无关键词部队） | **455** | `ASH_Autarch`（Autarch）· `ASH12`（Weapons Platform） |
| 有关键词，但关键词族**在原版没有天赋动画**（是资源/计数类词，不是 trait） | **185** | `ASH19`（`Waystone` · `Shuriken 2`）· `ASH_Farseer`（`Waystone`） |
| 天赋动画名**在、但那几条 CardAnim 的 guid 悬空**（资产没随包发） | **20** | 全是 `Vanguard` 一族：`ASH28`(Vengeful Wraithblade) · `ASH25`(Wraithguard) · `AM18`(Ogryn)… —— 对不上的是 `VanguardTraitPlay`/`VanguardTraitStart`/`VanguardProcAnim`（另两条悬空：`Rally_MasterOfExecutions` · `Self-Destruction_Effect`） |

> 另：`anim_address_map.json` 里 **27 条 CardAnim 的 `m_AssetGUID` 本来就是空的**（例 `SwarmAnim`）
> —— 这些就算名字对上也解不出 prefab。

---

## 四、坑（别推翻）

1. **`animinfo_lookup.json` 不是「卡表」** —— 它的键是 **VFX prefab 的根 GO 名**（418 个根 / 591 个 CardAnim，
   多个 CardAnim 共用一个 prefab 根就有多行）。`d:/warpforge/scripts/battle.gd:3489` 那句注释写得很清楚
   （「root GO 名 → {sp/ep/tm/move}」），**当初把它当逐卡表是误读**。
   `card_anim_map.json` 同理：它是「CardAnim 名 → 参数 + guid」按阵营包分了组，**没有卡**。
2. **`m_FileID != 0` 的 PPtr 也能解** —— 本项目没有 externals 表，但**实测 path_id 在全库唯一**：
   拿 `RangedAttackParticlesByArmy` 那 4 条（`m_FileID=15`）验过，各命中唯一一条
   （`Atk_BulletImpact_2shot` / `…_ork` / `Atk_Gauss_Basic` / `…_chaos`）。
   做法 = 拿 pathId 去 `anim_address_map.json` 的容器索引反查，漏的用 UnityPy 读原 bundle 补。
3. **`anim_address_map.json` 的容器索引只收 Addressables 容器项** ⇒ 非容器对象（如
   `Card 3D Death Explosion`、`normalSummon` 那两条）**不在里面**，必须回落到 UnityPy，不然会静默漏。
4. **短 hex 键**（帝皇之子那两个包的 52 条）当搜索模式会匹配到一大片 —— 过滤 `len==32`。
5. **`BattleAnims.heal` 是 `m_AssetGUID` 引用，不是 PPtr**（同一份 JSON 里两种混着）——
   只认 `m_PathID` 会漏掉**唯一一个有值**的槽。
6. **原版 `attackHitSmallParticles` 与 `attackHitBigParticles` 指向同一个对象** ——
   不是我们抄错，`m_PathID` 都是 `4239831775333917772`。
7. **`BlueSummonCircle` 只有 prefab、没有 CardAnim**；引用它的是**按阵营的** CardAnim
   （`AeldariSummon` / `Tau_Summon` …）。`vfx_wiring_j*.tsv` 里把部署标成 `BlueSummonCircle`
   是 **Godot 侧的口径**，不是原版字段。

---

## 五、示例查询

把某张卡的所有条目打出来（含 `conf`/`why`）：

```bash
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" -c "import json,sys;d=json.load(open(r'd:/4/Unity/数据/游戏数据/card_vfx_by_card.json',encoding='utf-8'));print(json.dumps(d['byCard'].get(sys.argv[1],'(这张卡一个事件都没接上)'),ensure_ascii=False,indent=1))" AM20
```

只看**判据齐**的那一块（该拿去替 `VfxMap.ByEvent` 的）：

```bash
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" -c "import json;d=json.load(open(r'd:/4/Unity/数据/游戏数据/card_vfx_by_card.json',encoding='utf-8'));print(json.dumps(d['generic'],ensure_ascii=False,indent=1))"
```

按 conf 过滤（例如只要 name 那一层）：

```bash
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" -c "import json;d=json.load(open(r'd:/4/Unity/数据/游戏数据/card_vfx_by_card.json',encoding='utf-8'));print({k:{e:[i['prefab'] for i in v] for e,v in m.items()} for k,m in d['byCard'].items() if any(i.get('conf')=='inferred-name' for v in m.values() for i in v)})"
```

---

## 六、下一步（按铁律 11：都要做，只有先后）

1. 🔴 **`allcards_assets_all.bundle`** —— 这是**唯一**能把这跳补齐的东西（远端 CCD）。
   拿到之前，逐卡那一栏**只能**停在推断层；拿到之后重跑本脚本即可（脚本里 §generic 那套 PPtr 解析直接复用）。
   ⚠️ **不要**因为「拿不到」就把 `byCard` 写成看起来完整的表（红线）。
2. **`generic` 那 8 个槽接进 `VfxMap.ByEvent`** —— 判据齐、改动小，能把 `play/deploy/hit/death` 从
   「替代品」换成原版自己的 prefab。
   ⚠️ **效果库里已经有的 8 件**（脚本逐条现查的，逐条见 `generic.*.inLibrary`）：
   `Invoke Minion Card Fade Default/Legendary` · `Invoke Minion Hits Ground( Legendary)` ·
   `AttackHitSmall` · `Healing_Circles` · `BulletImpact_2shot_trail(_ork_NEW/_chaos)` · `NecronGauss` ·
   `BlueSummonCircle` · `GreenSummonCircle` · `Tau_SummonCircle`。
   🔴 **唯一缺口 = `Card 3D Death Explosion`**：`Assets/WarpforgeVFX/Prefabs/` 里没有，
   但**解包资源里有**（`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/Card 3D Death Explosion.json`），
   而且**粒子数据也已经转过了**（`数据/粒子/particles/` 与 `particles3d/` 各一份）。
   ⇒ 缺的不是数据，是**一次「按名字导入单个原版 prefab」** —— 与
   `资料/待办判据_战场与战斗视图.md` 里「12 个 trait 框」那条的**前置缺口是同一个**，一次做完两处都通。
   ⚠️ `VfxMap.cs:58` 那句「❌ 不在导出索引」**是对的、不要改**（`effect_index.json` 958 条里确实 0 命中）；
   它说的是**我们的导出索引**，不是解包资源。
3. **近战那一支**（`AlternateArtCard.attackAnim` + `card3DAnimationController`）本地判不了，
   与第 1 条同一个前置（远端包）。
