# 甲-2 · `AssetGUID → 本地资源` 调查（第二十七会话）

> 只读调查。脚本：`d:/4/Unity/工具/_guid_resolve_probe.py`（本会话新建）。
> 数据：`d:/4/Unity/资料/普查产出_第二十七会话/guid_map.tsv`（4985 行）。

## 一、结论

1. **路 A 打通了，而且是完全离线、可复跑的一条链**：
   卡 SO 字段的 AssetReference GUID → Addressables 目录三段（key / entry / bucket）→ **束名 + 束内容器键** → 用原 `.bundle`（UnityPy）反解出**资产原名**（含 `m_Script` 类名）。
2. 产出一张 `guid_map.tsv`：**4985 行 / 2372 个不同 GUID / 26 个束**，覆盖卡 SO 里**全部 9 类资产引用**（列 `uniqueId·cardName·field·assetGuid·bundle·containerKey·type·className·name`）。
3. **端到端接通到 VFX prefab**（复用已有的 `anim_address_map.json` 的 `cardanim_to_asset`）：卡 → `CardAnim` 名 → VFX `GameObject`，**1843/1843 行 100% 有落点**。
4. 覆盖率（卡 SO 非空 → 已解析）：`cardImageReference` **1171/1171**、`customRangedAttackParticles` **484/484**、`customDeathParticles` **72/72**、`remnantBodyPrefab` **73/73**、`attackAudio` **59/59**、`summonAudio` **536/537**、`deathAudio` **59/61**、`summonAnimDelayed` **1/1**。
   额外白捡：`cardAbilities[].abilityLogic[].cardAnimations[]` **1359 条 / 796 张卡**（+ `activeAbility` 100 条）。
5. **独立核验零不一致**：与另一条完全不同的路径（`工具/gen_anim_address_map.py`，2026-10-03 从 AssetBundle JSON dump 建的 `anim_address_map.json`）逐条比，**4704 行名字全一致**（1095 条的差异只是 Sprite/Texture2D 同一个 GUID 的另一半）。
6. **对账我们的逐卡 VFX 表**：`inferred-name` 真卡条目 **24/39 命中（61.5%）**；`inferred-keyword` **1/474（0.2%）**；我们表的 `ability` 事件 **0/119**。
   ⇒ 那张表的逐卡层基本可以**用真值替换**（它当时自标「hop 2 在本地断掉」——**其实没断**，见第五节）。

## 二、方法 + 证据

### 2·1 目录格式（实测解出来的，三段全用上了）

`C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/com.unity.addressables/catalog_main.json`

- `m_KeyDataString`（base64, 393924 B）= `[int32 count=10594]` + 10594 × `[byte type=0][int32 len][utf8]`。
  键 = **资产 GUID（32 hex）** 或**束名（带哈希，如 `allcards_assets_all_56c290c0….bundle`）**；尾部 48 个键是数字串（依赖集，见下）。
- `m_EntryDataString`（357060 B）= `[int32 count=12752]` + 12752 × **7×int32** =
  `(internalId, providerIndex, dependencies, depHash, ?, primaryKey, resourceType)`。
  `providerIndex` 0=`AssetBundleProvider`（这 85 条是束本身）/ 1=`BundledAssetProvider`（束内资产）；
  `internalId` 是 `m_InternalIds` 下标；`primaryKey` 是 `m_KeyDataString` 下标；`resourceType` 是 `m_resourceTypes` 下标。
- `m_BucketDataString`（185580 B）= `[int32 bucketCount=10594]` + 10594 条
  `[int32 dataOffset][int32 n][n×int32 entryIndex]` —— **bucket i 对应 keys[i]**（dataOffset 是 key 在 KeyData 里的记录起点，实测 i=0 → 4、i=1 → 84 … 单调递增，可自证）。
- 🔑 **依赖集**：资产 entry 的 `dependencies` 指到一个**数字串键**（如 `-1387717494`），**它不是束名**；用 bucket 展开它 → 得到若干 **束 entry**，那些 entry 的 `internalId` 才是束路径。这是「卡 → 束」唯一的一跳，**不展开 bucket 就永远只有 GUID**。
- `internalId` 大多是主键 GUID 本身（**12319/12653 完全相等**），334 条是 GUID 的**前缀**（例：`564af14f…` 的 internalId = `'56'`）；前缀那条就是束里 `m_Container` 的键。

### 2·2 束侧

- `AssetBundle.m_Container` = `[键 → {preloadIndex, preloadSize, asset:{m_FileID, m_PathID}}]`，**键就是目录里的 internalId**（同一套「短 GUID」命名；实测 `0d7` / `56` / `eac` 在两处逐字相同）。
- 一部分束的键是**全长 GUID**（cardanims / cardanimsgeneral / battleprefabs），一部分是**短前缀**（Emperor's Children 的 cardassets，键只有 1–3 字符，例 `d`/`db`/`db9` 同时存在）⇒ 取「是 GUID 前缀的键里**最长**的那个」，并**用资源类型复核**（目录给 `CardAnim`/`UnityEngine.Sprite`…，束侧给 UnityPy 类型 / `m_ClassName`）。
- `MonoBehaviour` 的类名走 `Waprforge_monoscripts.bundle`：`MonoScript.PathID → m_ClassName`（实测 `8650191106899952245 → 'CardAnim'`）。
- 本地束：`d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/*.bundle`（84 个）。

### 2·3 命令

```bash
python -I -X utf8 d:/4/Unity/工具/_guid_resolve_probe.py survey      # 字段覆盖率
python -I -X utf8 d:/4/Unity/工具/_guid_resolve_probe.py card EC29   # 一张卡全部引用
python -I -X utf8 d:/4/Unity/工具/_guid_resolve_probe.py g <guid>
python -I -X utf8 d:/4/Unity/工具/_guid_resolve_probe.py map         # 重建 guid_map.tsv（8 秒）
```

### 2·4 样本链（可逐字复核）

`EC29 Heldrake`：
`customRangedAttackParticles = 564af14f7004445469e0424bcce41d1d`
→ keys[3607] → entry 4379 = `(3589, 1, 10546, 2082598726, -1, 3607, 1)`，`internalId='56'`，rtype `CardAnim`
→ bucket[10546] = `[20, 76]` → 束 `chaosspacemarinesemperorschildrencardanims_assets_all` + `tweenandshakes_assets_all`
→ 束内容器 `'56'` → PathID `8267393584198781881` → `MonoBehaviour` **`EC Heldrake Attack`**（`m_Script` → `CardAnim`）
→ `anim_address_map.json.cardanim_to_asset` → VFX `GameObject` **`EC Heldrake Attack`**。
同一张卡的 `cardImageReference = 0d720e0b…` → keys[638] → entry 747/748（`internalId='0d7'`，Texture2D + Sprite）→ 束 `…emperorschildrencardassets` → **`CSM_EmperorsChildren_veh_Heldrake`**。

## 三、对账结果

### 3·1 与我们已有的 `anim_address_map.json`（独立路径）

| 字段 | 行数 | 该 GUID 在它表里 | 名字一致 |
| --- | --- | --- | --- |
| cardImageReference | 2342 | 2190 | **2190** |
| cardAbilities…cardAnimations | 1359 | 1282 | **1282** |
| summonAudio | 536 | 501 | **501** |
| customRangedAttackParticles | 484 | 471 | **471** |
| deathAudio / attackAudio | 118 | 114 | **114** |
| customDeathParticles / remnantBodyPrefab / summonAnimDelayed | 146 | 146 | **146** |

**合计 4704 行，名字 0 条不一致。**（另 1095 条 type 差异 = 同一 GUID 的 Sprite/Texture2D 另一半。）

### 3·2 与我们的逐卡表 `数据/游戏数据/card_vfx_by_card.json`（对账对象）

| 我们的事件（`src`） | 真卡条目 | 命中真值 | 结论 |
| --- | --- | --- | --- |
| `trigger`（`keyword-trait`，`<关键词>Trait*`） | 318 | **1** | 真值不在卡 SO 里（见 4·6），但公式推断**没有一条**被支持 |
| `ability`（`keyword-trait`） | 119 | **0** | 真值是 `cardAnimations[]` 的实名（如 `CosmicSerpent_Secondary`），与公式不符 |
| `_unclassified`（`name-match`） | 27 | **17**（63.0%） | 这条推断法**可用**，但只覆盖 59 条 |
| `play` / `attack_melee` / `attack_ranged`（`name-match`） | 10 | 6 | 同上 |
| `deploy`（`name-match`） | 2 | 0 | 逐卡 `summonAnimDelayed` 全卡池只有 1 张有值 |

- `name-match` 合并计：**24/39 = 61.5%**；未命中的 15 条多是 `_self` / `_warlord` / `_target` **变体名**（那类动画不被该卡引用，是 warlord/别的入口在用）。
- `keyword-trait` 合并计：**1/474 = 0.2%** ⇒ **555 条 `inferred-keyword` 建议整层作废**，改用 `guid_map.tsv` 的真值。
- 反向覆盖率：真值共 **3682 条 anim 引用 / 1190 张卡**，我们的表只提到 **25 条（0.7%）**、1168 张卡**一条都没提**。
- 「同一 GUID 必共用同一特效」这条等价约束：真值里 `customRangedAttackParticles` 只有 **206 个不同 GUID / 484 张卡**（101 个 GUID 被多卡共用，最多 25 张卡）⇒ **共用是原版常态**；我们那张逐卡表按卡名各推一个名字，**结构上表达不出共用**（例：`Atk_BulletImpact_2shot` 一个效果被 13 张卡用）。

## 四、还没查清的 / 还差什么

1. **2 个 GUID（3 处字段引用）不在目录里**：`fda6e5bccaf40a14aa3b7f0249bd8dd8`（`BL47 Disgustingly Resilient` 的 `summonAudio`）、`11641bc635cf50942b52e516f66d2ba6`（`GOF2 Makari the Grot` 的 `deathAudio`）。**目录 JSON 与 `catalog.bin` 里都搜过，两个都没有** ⇒ 这两个引用本身就解不出（可能没进 Addressables 组 / 是别版本残留）。
2. **本机目录 JSON 有已知字符串损坏**：`m_InternalIds` 里有一条写成 `chaosspacemarinesemperirschildren…`（少个 `o`），而 `catalog.bin` 与本地束名都是 `…emperorschildren…`（实据：`.bin` 里 `emperorschildren` 命中 9 次、`emperirschildren` 0 次；JSON 里各 1 次/2 次）。→ 脚本因此加了 `difflib` 束名兜底（cutoff 0.85）。**没查清损坏是否只有这一条**。
3. **目录 JSON 与本地束不是同一个 build**：JSON 里束名带哈希（`…_3bc6cd16908a260…bundle`），本地文件不带（`…_assets_all.bundle`）；`catalog.bin` 是当前版（mtime 2026-02-27）。**本次只解了 JSON**（三段可读），**`.bin`（BinaryStorageBuffer 格式）没解** —— 它是权威版，值得后续补一个解析器。
4. **`whileInPlayAnim` 在卡 SO 里是 `int`（1192 张卡，值 0/1），不是 AssetReference**（另有 3 张干脆没有这个键）⇒ 04 简报把它列进「卡 → 动漫」那族是不成立的，**它不可用本链解析**。
5. **`targetedSpellAnim` / `nonTargetedSpellAnim` 在 1192 张卡里全是 `{m_FileID:0, m_PathID:0}`（空）** ⇒ 这一族本地判据为空。
6. **天赋/关键词触发动画（`trigger`）的真值不在卡 SO**：`CardTraits[]` 只有 `{name,_trait,_traitValue,_hide}`，没有动画引用 ⇒ 那一层要去 `CardTrait` 那张 SO（`TraitEffects[].effectAnim`）里找，**本次没做**。
7. **卡 SO 快照是 1195 张的 2022 时代 dump**（当前卡池 1126 张）⇒ 本表覆盖的是那份快照；当前卡池的差集没覆盖。
8. `alternateArts[]` 是 PPtr（不是 AssetReference），本次只采到 2 条；**异画那一路没打通**。

## 五、顺手发现

1. 🔴 **「hop 2 在本地断掉」这个结论是错的（铁律 5 口径要改）**：`gen_card_vfx_by_card.py` 头部与 `card_vfx_by_card.json` 的 `_headline` 都写着「卡 → 它自己的 animInfo 列表在本地断掉」，理由是「卡预制体只在远端 CCD 的 `allcards_assets_all.bundle` 里」。
   **实际**：卡 SO 的字段本身就是 `AssetReference`（存 GUID），而 GUID→束→资产名这一跳**一直可解**（2026-10-03 的 `gen_anim_address_map.py` 已经建了 `guid_to_asset` 10325 条 + `cardanim_to_asset` 1099 条，**只是没人把卡 SO 的字段引过来**）。本会话补的就是这一跳。
2. 束内容器键的**两套命名**（全长 GUID vs 短前缀）此前没人写过；`anim_address_map.json` 的 `containerEntriesNonGuidKey: 345` 就是那批短键，而它只吃全长 GUID 那一半 ⇒ 它漏掉的正是 Emperor's Children 这批新卡（本次已补齐，`cardImageReference` 24 张 EC 卡原来会漏）。
3. `customDeathParticles`：**72 张卡只有 1 个 GUID/1 个 prefab**；`remnantBodyPrefab`：**73 张卡 / 2 个 prefab** ⇒ 这两个字段几乎不是「逐卡」的。
4. **21 例跨阵营共用**（不是解错）：例 `AM32 Leman Russ Tank` → `Atk_TankShot_big` 落在 `orksgoffcardanims` 束；`SOR66` 与 `GSC21` **共用同一个 GUID** → 同一个 `Atk_BulletImpact_Vox-pulse`。Addressables 把共享资产收进了一个群 ⇒ 依赖集里只列一个 cardanims 束是正常的。
5. `cardImageReference` 解出来的名字就是**原版立绘贴图名**（如 `CSM_EmperorsChildren_veh_Heldrake`、`GenestealerCults_inf_Neophyte Initiate`），共 1058 个不同名 —— 比我们现有的立绘命名更有判据价值。
6. 目录 `m_InternalIds` 里 208 个「短字符串」不是损坏，是**资产名被缩成 GUID 的最短前缀**这条命名的另一面（16 个 1 字符 + 142 个 2 字符 + 50 个 3 字符）。
