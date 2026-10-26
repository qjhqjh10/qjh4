# B9 / J7MV —— 「有登记、无实物」全量扫（`m_AssetGUID` / AssetReference）

- 会话：第八十四会话 · 2026-10-26 · 只读取证代理 **J7MV**
- 作业口径：《完全审查与完全复刻_方案》§2.4 **第五类未覆盖「有登记、无实物」**
- 扫描对象：`d:/2/新解包资源/assets_full/`（91 个顶层条目 = **84 个 bundle 目录 + 6 个基础文件目录 + `_stats.json`**；共 **240,174 个 JSON**）
- 产物：`判据/B9_有登记无实物_全量.tsv`（3,974 行引用逐条）+ 本报告

---

## ① 结论

### 1.1 扫了多少

| 项 | 数 | 怎么得到 |
|---|---|---|
| `assets_full` JSON 总数 | 240,174 | `find -name '*.json'` 实测；`_stats.json` 的 `json` 列求和 = 240,173（差 1） |
| 我实际遍历 | **240,090** | 排除 84 个 `AssetBundle/*.json`（那是容器表本身，不是引用方） |
| 含 `m_AssetGUID` 字样的文件 | 3,180 | 其中 **234 个是 Shader 假阳性**（见 §③-1） |
| **真引用条数** | **3,974** | 与 `grep -c '"m_AssetGUID": "[0-9a-f]*"'` = 3,974 **完全一致**（独立校验） |
| 引用方文件数 | 2,946 | |
| 被引用的不同 guid | 2,073 | 2,039(A) + 20(A′) + 14(B) |
| 引用字段种类 | 40+ 种 | 主体：`animInfo.animAdressable` 1,067 · `imageReference` 718 · `cardBackSDF` 243 · `particleDefinition.{particles,audio}` 各 213 |

**两跳是怎么搭起来的（第二跳是本报告第一次实现，任何文档都没写过）：**

```
hop1  guid → bundle        : 数据/索引/guid_map.tsv（10,325 行）
hop2  bundle → asset       : <assets_full>/bundle_<名>/AssetBundle/AssetBundle_1.json
                             的 m_Container[guid] = { preloadIndex, preloadSize,
                                                     asset:{m_FileID, m_PathID} }
hop3  pathID → 名字/类型/存在: 用 UnityPy 打开 <安装目录>/aa/StandaloneWindows64/<名>.bundle
                             （84 个 .bundle，1.6 GB，8.5 秒全开）⇒ pathID → (m_Name, 类型)，
                             并给出「这个 pathID 是不是本 bundle 的容器目标」
```

⚠️ **第一跳与第二跳其实是同一份数据的两个视图** —— 实测 `guid_map.tsv` 的行集合与 84 个 bundle 的
`m_Container` 键集合**完全相等**（各自 10,325 个，差集两侧都是 0）。`tmp_build_guid_map.py` 就是拿
UnityPy 读 `sf.container` 生成的。⇒ **只有把 hop2 读到 `m_PathID`、hop3 解出对象名/存在性，第二跳才有信息量**；
单看「guid_map 有没有这个 guid」= 单看「容器里有没有这条」，**不构成独立证据**。

### 1.2 分类结果

| 状态 | 含义 | 条数 |
|---|---|---|
| **A** | guid 在某个 bundle 的容器里，且 **PPtr 指向的对象在那个 bundle 里真实存在**（逐条 UnityPy 核过） | **3,235** |
| **A′** | guid 不在容器里，但**容器键被截断**，能唯一复原到物理存在的对象（见 1.3） | **20** |
| **B** | **真·悬空引用**：guid 不在任何容器的任何键里，catalog 里也没有 | **14** |
| **C** | 解不出（格式不认识的 guid） | **0** |
| EMPTY | `m_AssetGUID` 为空字符串 = 原版就没指派（正常，不是缺陷） | 705 |

- **A 类的 0 缺陷是核过的**：3,235 条 A 引用所命中的容器条目，**没有一条**指向「本 bundle 里不存在的 pathID」。
- 84 个 bundle 的容器条目共 **13,448** 条、不同键 **10,325** 个。其中 2,490 个 guid 对应 **>1 个**容器目标
  （同一个 bundle 里 Texture2D + Sprite 这类主/子资产共用一条 address），33 个 guid 同时登记在 >1 个 bundle 里。
  ⇒ **「一个 guid = 一个资产」不成立**，取名字时要按类型筛。

### 1.3 两个 bundle 的容器键被截断（这一类最容易误判成「无实物」）

| bundle | 容器条目 | 键的形态 | 后果 |
|---|---|---|---|
| `chaosspacemarinesemperorschildrencardassets_assets_all` | 278 | **全部是 1~3 字符**（`'0' '00' '01' '980' 'b3' …`） | 按 guid 查容器查不到，但资产**全在** |
| `chaosspacemarinesemperorschildrencardanims_assets_all` | 52 | 同上 | 同上 |

- **UnityPy 与 JSON 导出给出的键一致**（都是截断的）⇒ 这是**原版数据本身就坏**，不是我们的解包 bug。
- 这些短键**都是真 guid 的前缀**：`'00'`→`00355ddf…`、`'716'`→`7163291b…`、`'980'`→`9800e16a…`、`'b3'`→`b3462cd7…` …
- **A′ 的 20 条就是这么定出来的**：对每条 B 候选，「按该字段 A 项的目标命名族 + 期望类型」筛，再用「短键是 guid 前缀」唯一命中 —— **20/20 唯一**（脚本逐条验，见 §②-3）。
- ⚠️ 运行时到底受不受影响**没证**（§③-2）。所以 A′ **不算「无实物」**，算「登记键损坏」。
- ⚠️ 这个损坏**污染了 `guid_map.tsv`**：其中 187 + 19 = **206 行**的第 1 列是 `'0' '0c' '5c' '6b'` 这种
  1~3 字符的「guid」（另有 15 条是 scene 的资产路径，见 §②-5-1）。

### 1.4 最该先修的 5 条（按 P0 主场域 × 影响面）

| # | 条目 | 条数 | 为什么排前面 |
|---|---|---|---|
| 1 | `SoundAssetCollection` 6 个字段（`abilityBoxClick` / `activateUnstunnable` / `activateSurvivor` / `activateTerror` / `activateSentinel` / `activateAutoAttack`） | 6 | **不是 EC 专属**：`abilityBoxClick` 的调用点是 `ActiveSkillDisplay.ClickDown` / `OnTouchDown`（每次点技能框），`activateUnstunnable` 的调用点是 `CardScript.Stun` ⇒ **全池战斗都受影响**；本机 `soundcollection` 里**零同名 SO** |
| 2 | `CardFramesByArmy.clanFrames[11]`（**army 120 = Emperor's Children**）4 档 × 4 字段 | 16 | P0 卡牌域；**16/16 物理存在**（A′），按名字取即可，是「最容易闭合」的一条 |
| 3 | `EnviromentalEffectCardsSO.enviromentalEffects[11]`（army 120）默认 + 3 张进攻卡 VFX | 4 | P0 战斗域；4/4 物理存在（A′），同上 |
| 4 | `CardAnim.animInfo.animAdressable` × 5：Vanguard 3 条 / `Rally_MasterOfExecutions` / `Self-Destruction_Effect` | 5 | 特效不播（静默）；其中 `MasterOfExecutions` prefab **在本机存在且可寻址，只是 guid 过期** ⇒ 可直接修 |
| 5 | `OpenWindowButton.windowToOpenPrefab`（挂在 `Main Menu Navigation Button`）/ `ParentEventData.EventAssets[key=8]` / `ScenarioEnvironmentConditionSO.scenarioObjects`（Fleet Support） | 3 | EC 活动入口打不开 / 活动第 4 张配图缺 / 战场环境物件为空 |

---

## ② 逐条证据

### ②-1 复核 `K1P0` 的首例 —— **结论要推翻**

| 项 | K1P0 的说法 | 实测 |
|---|---|---|
| 引用点 | `ItemDrawerConfig.asset:201-214` → `drawerReference.m_AssetGUID = 7ea5632eb059b5b4493c5047a53e1706` | ✅ 属实（`d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/ItemDrawerConfig.asset:207`） |
| 登记 | —— | ✅ guid 在 `guid_map.tsv:6598` → `menus_assets_all.bundle`；且是 `menus` bundle `m_Container` 的键（`preloadIndex 111768`，`asset = {m_FileID:0, m_PathID:-3438027711880944002}`） |
| 「导出树里没有这个 guid」 | 判为无实物 | ⚠️ **这句话字面成立、结论不成立**：那个 pathID 在 `menus_assets_all.bundle` 里**真实存在**，UnityPy 读出它是 **GameObject，`m_Name = "Deck Drawer"`** |
| 为什么导出树搜不到 | —— | 🔴 **AssetRipper 给 bundle 资产【重新生成】guid**：导出工程 19,727 个 `.meta` 的 guid 与 catalog 的 10,325 个 guid **交集 = 0**（0.0%）。所以「按 guid 在导出树里搜」对**任何** bundle 资产都必然落空，不能当判据 |

⇒ **`7ea5632e…` 属于 A 类（找得到）**。K1P0 那条「`DeckDrawer` 按配置装载会静默落兜底」的结论**没有证据支持**，
按铁律 5 应就地订正（本代理只读，未改任何文档）。

### ②-2 A 类（3,235 条）—— 逐条在 TSV 里，带 bundle/对象名/类型/pathID

抽样格式（TSV 前两行）：

```
bundle_aeldarisaimhanncardanims_assets_all/MonoBehaviour/AeldariRecall_2_ability.json:14
  animInfo.animAdressable  8f4146eb…  battleprefabs_vfxandmisc_assets_all/Recall_Eldar_2 [GameObject] @pathID=-3997810185810544445  A
bundle_aeldarisaimhanncardanims_assets_all/MonoBehaviour/AeldariRecall_2_ability_quick.json:14
  animInfo.animAdressable  566ed65d…  battleprefabs_vfxandmisc_assets_all/Recall_Eldar_2_quick [GameObject] @pathID=7383665915439035459  A
```

- 3,235 条 A 引用分布在 **42 个 bundle** 的目标上；命中最多的是 `battleprefabs_vfxandmisc`（特效 prefab）、`soundcollection`、各阵营 `cardassets`。
- **0 条** A 引用的目标对象缺失（实测：把每条 A 引用的全部容器目标逐个查 `bundle_objs.json`）。

### ②-3 A′ 类（20 条）—— 容器键被截断，资产物理存在

全部落在 **army 120（Emperor's Children）**，分属两个 SO：

| # | 引用方 | 字段 | guid | 复原到（截断键 → 目标） |
|---|---|---|---|---|
| 1 | `staticgeneralassets/MonoBehaviour/CardFramesByArmy.json:1086` | `clanFrames[].framesByTier[].stratagemHighlightFrame` | `9800e16a…` | `'980'` → `40k_Cardframes_stratagem_Emperor Children_SDF_tier1` |
| 2 | `…:1091` | `….stratagemFrame` | `b3462cd7…` | `'b3'` → `40k_Cardframes_stratagem_Emperor Children_tier1` |
| 3 | `…:1096` | `….troopHighlightFrame` | `00355ddf…` | `'00'` → `40k_Cardframes_Troop_Emperor Children_SDF_tier1` |
| 4 | `…:1101` | `….troopFrame` | `7163291b…` | `'716'` → `40k_Cardframes_Troop_Emperor Children_tier1` |
| 5 | `…:1109` | stratagemHighlight | `23c7b1a2…` | `'23c'` → `…_SDF_tier2` |
| 6 | `…:1114` | stratagem | `b06c0bc6…` | `'b0'` → `…_tier2` |
| 7 | `…:1119` | troopHighlight | `a17cb7ce…` | `'a1'` → `Troop_…_SDF_tier2` |
| 8 | `…:1124` | troop | `19e56d39…` | `'19'` → `Troop_…_tier2` |
| 9 | `…:1132` | stratagemHighlight | `17ef5d05…` | `'17'` → `…_SDF_tier3` |
| 10 | `…:1137` | stratagem | `4b37d31f…` | `'4b'` → `…_tier3` |
| 11 | `…:1142` | troopHighlight | `73249313…` | `'73'` → `Troop_…_SDF_tier3` |
| 12 | `…:1147` | troop | `7d41790c…` | `'7d'` → `Troop_…_tier3` |
| 13 | `…:1155` | stratagemHighlight | `dce0c121…` | `'dc'` → `…_SDF_tier4` |
| 14 | `…:1160` | stratagem | `a020e9cd…` | `'a0'` → `…_tier4` |
| 15 | `…:1165` | troopHighlight | `dbc383ba…` | `'db'` → `Troop_…_SDF_tier4` |
| 16 | `…:1170` | troop | `bb7e1025…` | `'bb7'` → `Troop_…_tier4` |
| 17 | `duplicateassetisolationso/MonoBehaviour/EnvironmentalEffectCardsSO.json:823` | `enviromentalEffects[].defaultEnviromentalEffectVFX` | `b0ea89cc…` | `'b0'` → `Emperor's Children Default Condition` (MonoBehaviour) |
| 18 | `…:835` | `….offensiveCards[].enviromentalEffectVFX` | `71265f99…` | `'71'` → `Environmental Condition Emperor's Children 1 Green` |
| 19 | `…:847` | 同上 | `eb40a866…` | `'eb4'` → `… 2 Fumes` |
| 20 | `…:859` | 同上 | `dba07ead…` | `'db'` → `… Empyric Rift` |

**唯一性校验**（脚本 `prefix+type+name` 三重筛）：20/20 都只剩唯一候选。
其余 12 个阵营（army 10…110、130）的 192 条同类引用全部是 **A**，可直接取到
（例：army 10 tier0 → `40k_Cardframe_stratagem_Ultramarines_SDF_tier1`，`spacemarinesultramarinescardassets`）。

### ②-4 B 类（14 条）—— 真·悬空

| # | 引用方 | 字段 | guid | catalog 里有没有 | 后果 / 本机有没有替代 |
|---|---|---|---|---|---|
| 1 | `cardanimsgeneral/MonoBehaviour/VanguardProcAnim.json:14` | `animInfo.animAdressable` | `bde8e58a…` | **没有** | Vanguard 结算特效不播。本机 `battleprefabs` 只有 `VanguardIdleEffect`/`Vanguard Onslaught Board`，**没有** Vanguard 特效 prefab |
| 2 | `…/VanguardTraitPlay.json:14` | 同上 | `58b108b4…` | 没有 | 同上 |
| 3 | `…/VanguardTraitStart.json:14` | 同上 | `4fe77abf…` | 没有 | 同上 |
| 4 | `chaosspacemarinesblacklegioncardanims/MonoBehaviour/Rally_MasterOfExecutions.json:14` | 同上 | `44ef424c…` | 没有 | 特效不播。**替代物在**：`battleprefabs/GameObject/MasterOfExecutions`（可寻址）⇒ 引用的是**过期 guid** |
| 5 | `orksgoffcardanims/MonoBehaviour/Self-Destruction_Effect.json:14` | 同上 | `b7c12d75…` | 没有 | 特效不播。本机无同名/同族 prefab（`*Self*Destr*` 全库只命中这张卡自己的 SO 与卡图） |
| 6 | `duplicateassetisolationso/MonoBehaviour/SoundAssetCollection.json:48` | `abilityBoxClick` | `2eee7367…` | 没有 | 技能框点击音不响。`soundcollection` 里 `ButtonClick*` 有 8 个，但**没有** abilityBox 专属 SO |
| 7 | `…:93` | `activateUnstunnable` | `4d4975d2…` | 没有 | 该 trait 激活音不响；全库 `*Unstunnable*` = 0 |
| 8 | `…:98` | `activateSurvivor` | `f5b94c43…` | 没有 | 同上；`*Survivor*` = 0 |
| 9 | `…:103` | `activateTerror` | `a35d6ec1…` | 没有 | 全库只有 `Psychic Terror` / `Cult Sentinel` 这类**卡名**，不是音效 SO |
| 10 | `…:108` | `activateSentinel` | `78cf42c0…` | 没有 | 同上 |
| 11 | `…:113` | `activateAutoAttack` | `b6b23aff…` | 没有 | `*AutoAttack*` = 0 |
| 12 | `menus/MonoBehaviour/MonoBehaviour_7644617844080014881.json:17` | `windowToOpenPrefab` | `e50cee8d…` | 没有 | 该组件挂在 GameObject **`Main Menu Navigation Button`** 上 ⇒ 主菜单那个导航按钮点了打不开窗 |
| 13 | `cosmeticsso/MonoBehaviour/Release Emperors Children Parent Event.json:70` | `drawData.EventAssets[].value`（`key=8`，声明类型 `UnityEngine.Sprite`） | `ba6b95f7…` | 没有 | EC 活动的第 4 张配图缺。同 SO 里 `key=7`=`WF_EC Keyart-GP-nologos`、`key=0`=`40k_campaign_Expansion Pass`、`key=2`=`UI_Deck_Warlord_Lord Exultant` 都是 A |
| 14 | `spacemarinesultramarinescardanims/MonoBehaviour/EnvironmentalCondition Ultramarines Fleet Support.json:28` | `scenarioObjects` | `e6745e95…` | 没有 | 「Fleet Support」的**场景物件列表为空**（该战场环境只剩光照/雾）。同 bundle 里 `Stormraven_Center` / `Stormraven_Shadow_Target` 都可寻址，可能是原本要指的东西 |

**14 条的共同点**：**没有一条**在 `catalog.bin` 里 ⇒ 它们不是「远端包没下载」，而是**从来没被登记过**
（引用写的是过期/作废的 guid）。对照：A′ 的 20 条**全部**在 catalog 里。

**独立性说明**：`catalog.bin`（1,305,148 B，Addressables 的 ContentCatalogData）是**第三份独立数据源**。
校验结果：A 类 3,235/3,235 都能在 catalog 里找到 ✅；A′ 20/20 能 ✅；B 14/14 不能 ✅ —— 三个桶互不越界。

### ②-5 三种「不是缺陷」的情形（避免下游误报）

1. **15 个 scene bundle 的容器键是资产路径、`asset = {m_FileID:0, m_PathID:0}`**（例
   `Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity`）——这是 Unity 对 scene bundle 的正常写法
   （scene 对象不给 pathID）。它们也是 `guid_map.tsv` 里 15 条「路径当 guid」的来源。
2. **705 条 `m_AssetGUID: ""`** —— 原版就没指派（`CosmeticItemTitle` 462 · `ManaTypeHolder` 130 ·
   `CardDisplayAttackTypeButton` 52 · `CardAnim` 27 · `ScenarioEnvironmentConditionSO` 13 · `PremiumItem` 13 …）。
3. **`duplicateassetisolationso_assets_all` 不是废弃副本**：`SoundAssetCollection` 与
   `EnvironmentalEffectCardsSO` **全库只有这一份**（Addressables 的 duplicate-asset-isolation 把被多处引用的
   SO 单独隔离成一个 bundle），所以上表第 6~11 条是**活引用**。

---

## ③ 没查清（写清还差什么）

1. **234 条 Shader 假阳性**：`Shader/*.json` 的 `m_ParsedForm` 里有一个常量缓冲成员叫 `m_AssetGUID`
   （值是 `{"data[0]":…}` 这样的 16 字节哈希），不是引用。已在 TSV 里剔除；
   ⚠️ 下游若用 `grep m_AssetGUID` 扫 assets_full，**会把这些也算进去**，请改用「值是 32 位 hex 字符串」判据。
2. **容器键截断是否影响运行时 —— 未证**。`CardFramesSO.TierCardFrameAddressableReference.GetSprite`
   （`decomp_full/CardFramesSO.TierCardFrameAddressableReference__GetSprite.c:41,46`）走的是
   `Everguild.Addressables.AssetReferenceTyped<object>.Load(...)`，即按 guid 交给 Addressables；
   而 Addressables 的 **provider 侧代码在包 DLL 里**（`UnityEngine.AddressableAssets.dll`），
   `decomp_full` / cpp2il 产物里都没有 ⇒ **装的是 catalog 的 internalId 还是 bundle 容器键，本机判不了**。
   差什么：真机实跑（EC 阵营卡框是否正常显示），或拿到 Addressables provider 的方法体。
3. **B 类 14 条「本来想指谁」有 6 条没定死**（另 8 条含义清楚）：`ParentEventData key=8` 想指哪张图（候选
   `WF_Raid_EC bg tiled`=`96c24f52…` 已登记但无人引用）· `Main Menu Navigation Button` 想开哪扇窗
   （menus 里名字含 `Window` 的对象 139 个，其中**没有** `Expansion Pass Window` 命中）· Vanguard 3 条想指哪个 prefab ·
   `Self-Destruction_Effect` 想指哪个爆炸 prefab。差什么：远端/后续版本的 catalog，或卡面/活动截图旁证。
4. **远端包只试到 3 个**：catalog 里有、本地 `aa/StandaloneWindows64` 里没有的 bundle 共 3 个 ——
   `allcards_assets_all`、`alternateartstyles_assets_all`、`localization_assets_all`。
   三者的 `__data` 都在 `d:/2/关服前存档/安卓版本warpforge…/Everguild_Warpforge_Unity_Cache/` 里找到了
   （allcards 531,962 B / 1,359 个容器键；altart 69,582 B；localization 10,764,942 B = `I2Languages`+`LanguageRemoteAsset`），
   **逐一试过：B 类 14 个 guid 命中 0**。差什么：不能排除还有第 4 个远端包（Android 侧 catalog 未解）。
5. **基础文件的对象级导出失败**：`_stats.json` 显示 `objects 247,612 / json 240,173 / exported 2,555 / fail 905`
   （`resources.assets` 616 · `sharedassets0.assets` 182 · `level0` 73 · `globalgamemanagers.assets` 29）。
   这 905 个对象是**对象级「有登记无实物」**，与本次「guid→资产」是**两条不同的轴**，本报告未覆盖。
6. 33 个 guid 同时登记在 >1 个 bundle，而 `guid_map.tsv` 每 guid 只留 1 个 bundle（后写覆盖）⇒
   按 `guid_map` 找会**指错 bundle**。本报告的 hop2/hop3 走的是**容器全量**，不受影响；
   但下游若只用 `guid_map`，要记得这一点。

---

## ④ 改动清单（只动了这两份文件）

| 文件 | 说明 |
|---|---|
| `d:/4/Unity/资料/完全复刻/判据/B9_有登记无实物_全量.tsv` | 新建。3,975 行（表头 + 3,974 条引用）；列 = `引用方(文件:行) · 引用字段 · guid · 反查到的bundle/asset名 · 状态A/B/C · 后果 · 还差什么`；LF 行尾（与同目录既有 TSV 一致）；986 KB。⚠️ `EMPTY` 那 705 行的「文件:行」**没有行号**（行号是按 guid 文本定位的，空 guid 无文本可定位）—— 需要精确行号时按「文件 + 字段」现查。 |
| `d:/4/Unity/资料/完全复刻/报告/B9_J7MV_有登记无实物.md` | 本文件 |

未动：任何 `.cs` / 资产 / 主账表 / 正本 / `d:/2` 档案库；未跑 Unity；未动 git。
（取证脚本与中间产物全在 `%LOCALAPPDATA%/Temp/j7mv/`：`scan.py` `passb.py` `ctx.py` `names.py` `makedtsv.py`
+ `rows.json` `index.json` `bundle_objs.json`。要复跑就照这个顺序。）

---

## ⑤ 🔴 顺手发现

1. **`guid_map.tsv` 的第 2/3 列（asset 名 / 类型）全是 `None` / `?`** —— 生成脚本
   `d:/2/Warpforge_tools/tmp_build_guid_map.py` 用 `byid.get(pid)` 反查对象名，而负 pathID 查不到，
   `name` 恒 `None`。⇒ **那张表只能当「guid→bundle」，不能当名字表**。本报告用 UnityPy 重新解出了名字，
   但**没有回写**（`d:/2` 与索引属被别人引用的正本，改动要主对话自己下）。
2. **`guid_map.tsv` 被截断键污染**：206 行第 1 列是 `'0' '0c' '5c'` 这类 1~3 字符「guid」+ 15 行是 scene 资产路径
   ⇒ 用它做 `32 位 hex` 的严格校验会**静默漏掉/混入**。建议：读表时先判 `len==32 and all hex`。
3. **AssetRipper 导出树不能当 guid 判据**：19,727 个 `.meta` guid ∩ catalog 10,325 个 guid = **0**。
   这条影响的不只是 K1P0 —— 任何「原版里有没有这个资产」的普查，**都不要用导出树的 guid 去对 catalog 的 guid**。
   要按 `catalog 的 guid → bundle → 容器 pathID → UnityPy 解名字` 这条链走。
4. **同一 guid 会对应多个对象**（2,490 个），且同一对象可能有多条容器键（EC 那 278 条里 Texture2D 与 Sprite
   各一条）。⇒ 「guid → 一个资产名」的说法在写文档时必须带「按类型筛」。
5. **`assets_full` 里 `Texture2D` 与 `Sprite` 同名成对出现**（例 `WF_EC Keyart-GP-nologos` 两个文件），
   这是正常的主/子资产，不是重复导出。
6. **卡框这一族我们只搬了 5 张**：原版按阵营的卡框 **120 个 Sprite**（13 个阵营：11 个各 8、
   Emperor Children 与 Space Wolves 各 16；另有 `CardFrame_<n>` 编号对象 136 个、5 张稀有度框），
   而 `Unity/MyGame/Assets` 里 `*Cardframe*` 只有 **24 个命中 = 5 张稀有度框（Art 副本 + Resources 副本 + meta）
   + `CardFrameGlitch.shader` + 1 张 VFX 图** ⇒ **per-army 卡框一个都没进工程**。
   若 P0 卡牌面要按原版按阵营出框，这是**一整族欠账**（含 EC 那 16 张）。
7. **`SoundAssetCollection` 28 个字段的真实分布**：16 A / 6 B / 6 空（`abilityBoxEnter` `abilityBoxExit`
   `syphon` `activateBlast` `activateSacrifice` `dissolveCardInHand`）⇒ 那 6 个是**原版就没配**，别当缺陷补。
8. **EC 阵营的内容是「后加的一整批」**：A′ 的 20 条**全部**是 army 120，B 里还有 2 条 EC 活动相关
   （`Release Emperors Children Parent Event`、主菜单导航按钮），且两个 EC bundle 的容器键全坏。
   建议把「Emperor's Children 的资产/登记」当**一个专题**去查（远端包？后期补丁？），不要一条一条查。
9. `TapHitMaterialDefinition` 的 426 条战场材质引用**全部 A** —— 战场线可以放心按 guid 取。
