# C3 · `guid_map` 短键缺口（干跑）+ 缺件音频记账

> 2026-10-23。执行代理 C。**未跑 Unity、未动 git、未改正本、未覆盖任何正式产物**。
> 判据源：`工具/gen_guid_map_from_bundles.py` · `工具/_guid_resolve_probe.py` · `工具/parse_catalog.py` ·
> `d:/2/Warhammer 40k Warpforge/…/aa/catalog.bin`（PC，`buildResultHash ae516dc07ca5e8f47265a8f5783d7137`，14,307 locations / 11,829 keys）·
> 安卓 1.35.0 `UnityDataAssetPack.apk` 内 `assets/aa/catalog.bin`（`buildResultHash 0dd7cbfa22666c0f520377fbebf8f416`，14,301 locations / 11,829 keys）·
> `d:/2/新解包资源/assets_full/**` · `d:/2/关服前存档`（只读）。

---

## ① 结论

### 1·1 短键缺口：**只增不冲突**（91 行新解出，0 冲突）

| 项 | 数 |
|---|---|
| catalog 里 `internalId` 不是 32-hex 的 **GUID key** | **256** = **241 个资产 GUID** + **15 个场景 GUID**（`SceneInstance`，默认跳过：卡 SO 不引用场景） |
| 241 个资产 GUID 中：**现有 tsv 完全没有的** | **91 个** ⇒ **新增 91 行** |
| 241 中：**现有 tsv 已有同名的** | **150 个 GUID / 239 行**（同一 GUID 有 Sprite+Texture2D 两行）⇒ 记「重复」，**不追加、不改行** |
| **冲突（同 GUID 解到不同名字）** | **0 条** |
| 新增按束 | `chaosspacemarinesemperorschildrencardassets_assets_all.bundle` **+74** · `…cardanims_assets_all.bundle` **+17** |
| 新增按类型 | `AudioClip` **57** · `MonoBehaviour`（CardAnim / ScenarioEnvironmentConditionSO）**17** · `Sprite` **9** · `Texture2D` **8** |
| 产物增量 | **+91 行 / +15,946 字节（+1.24%）**（1,287,180 → 1,303,126 B） |
| **只增不改的证明** | `--cards-only --no-catalog` 干跑产物与现盘上 `guid_map_bundles.tsv` **逐字节完全相同**（md5/长度见 ②）；带补集的干跑件 = **现表原文前缀（6,173 行逐字节相同）+ 末尾追加 91 行** |

**重复那 239 行的性质（逐条核过）**：判「重复」的口径 = **名字 + 束 + 容器键三者全对上**（类型可以不同），
实测 **同一 bundle 239/239 · 同一容器键 239/239 · 同一名字 239/239**；
其中 **148 行连类型都相同**，另 **91 行只差 `Sprite` ↔ `Texture2D`**（同一 GUID 的两份类型，名字一样）——
**不是冲突**，只是现有表只收了其中一份类型。要不要把这 91 个「补类型行」也收进来 = **另一条口径**（本批没做，见 ③·2）。
（口径已收紧：只对名字、束或键有一个对不上就报 **conflict** 并**拒绝写盘** —— 本次仍为 **0**。）

**新增里对卡池有实际价值的部分**：91 个新 GUID 中 **35 个被卡 SO 引用**（全是 `AudioClip`）；
其中 **9 个**在 `anim_address_map.json` 的 `guid_to_asset` **和** `card_vfx_by_card.json` 的 `_assetRefs`
**两处都没有** ⇒ 这是**真正的新覆盖**（EC 的聊天语音：`Lord Kaphrael` 的 gen3/intro/wp/cantDo ·
`Lucius` 的 mirror/threat/gen2 · `Xarahan` 的 wp/hurry）——直接喂 `import_original_audio.py` 的「卡 SO 12 个 ChatSound」那一级。

**结论**：**可以覆盖**（只增、0 冲突、原表逐字节可复现）；**等拍板**，本批**没有覆盖**。
建议命令（拍板后）：
```
D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/gen_guid_map_from_bundles.py --cards-only
```
（⚠️ **正式表的实际路径** = `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` —— 本仓**没有** `数据/游戏数据/guid_map*.tsv`，
这只需知悉覆盖目标；本批干跑件在 `_tmp_view/wfC3/`。）

### 1·2 两条「无束命中」的短键（不计入 91）

| GUID | internalId | 目录束 | 状态 |
|---|---|---|---|
| `0195addac64424715b03a2310dfbbfe9` | `0` | `alternateartstyles_assets_all.bundle` | **本地安装目录没有这只束**（只读主 `aa/StandaloneWindows64` 与 `unity_run_ref` 两处都没有）⇒ `lookup` 无束可查 |
| `cd516d1bf3fd249f3ad4776db89e70f7` | `c` | 同上 | 同上 |

🔎 **但关服前存档里有这只束**：`d:/2/关服前存档/新建文件夹/Everguild_Warpforge/7416b641…/8657f84dd6401b7f761195881a7bb0cd/__data`（69,582 B），
容器键 `0` / `c` 各带 `Texture2D + Sprite` 两份，名字分别是 **`WarpForgeLogo Style`** / **`Cell Shaded Alternative Art Style`**
（`float` 逐条实读，见 ②）。⚠️ 它们是**菜单风格资产、不是卡资产**（没有任何卡 SO 引用）⇒ 本批**不补**；
要补就得给生成器加「缓存束回退」路径 = 另立账。

### 1·3 PC catalog ↔ 安卓 1.35.0 catalog 互证：**完全一致**

| 项 | PC | 安卓 1.35.0 | 一致？ |
|---|---|---|---|
| 资产 GUID 集合 | 11,703 | 11,703 | **交集 11,703 / 两侧独有 0 / 0** |
| `GUID → internalId` | — | — | **不同 0 条** |
| `GUID → 资源类型` | — | — | **不同 2 条**（都是长键 GUID：`1e59d172…` / `a291e5dd…`，PC `ComputeShader` vs 安卓 `VisualEffectAsset` —— 平台差异，不属短键） |
| 短键 GUID 集合 | 241 | 241 | **完全相同** |
| **短键名字**（91 条补集逐条） | — | — | **91/91 相同 · 0 不同 · 0 缺** |

⇒ 短键补集的名字有**两条互相独立的来源**背书（PC 安装束 + 安卓 asset pack），不是单点判据。

### 1·4 缺件音频：**找到了，是导入缺口、不是源缺口**

- **源文件**：`d:/2/新解包资源/assets_full/bundle_spacemarinesultramarinescardassets_assets_all/AudioClip/VO_UM_Firestrike Servoturret - Battlefields are filled with the remains of the obstinate.wav`
  —— **247,468 B · RIFF/WAVE PCM 16-bit mono 30,614 Hz**（`file` 实读）。
- **判据链**：卡 **`UM92 Firestrike Servo-Turret`（Ultramarines）** 的 `summonAudio` = GUID **`3192663f15f6ea846bf518eab1f0b1e2`**
  → catalog → `spacemarinesultramarinescardassets_assets_all.bundle` 容器键同 GUID → 束内对象
  `AudioClip` **`VO_UM_Firestrike Servoturret - …obstinate`**（`m_Frequency 30614` / `m_Channels 1`，与 WAV 头逐值吻合）。
- **为什么工程里没有**：`Resources/Art/audio/vo/` 里**恰好缺这 1 条**（对照 SO 的 697 个唯一音频名，缺 1）；
  `voice_lines.json`（2026-10-18 生成）里**连 UM92 整张卡都没有**（0 条 `Firestrike`）⇒ 是**导入产物过期**，不是源缺。
- **建议（⛔ 未执行）**：
```
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/import_original_audio.py
```
  `--check` 现读：「卡 SO 引用 698 条 / 唯一文件名 697 / **音频源里没有 0 条**」⇒ 真跑会补拷 **1 个文件** 到
  `Resources/Art/audio/vo/` 并**重写** `voice_lines.json`（`Resources/Art/` 被 .gitignore 排除，仓库里只会多出 `voice_lines.json` 的 diff）。
  ⚠️ 它会顺手把 `voice_lines.json` 从 10-18 版刷新到现在（UM92 等新条目会一起进来）—— **拍板后再跑**。

### 1·5 主对话已拍板的（本条只记，⛔ 不做）

- **106 条 `dead` 推断条目：保留，不删**（`merge_authoritative` 既定「旧三档一条都不删」；运行时按 `confPriority` 永远取不到）。
- **34 条 `conflict_event` / 11 条 `prefab_only`：保留**（判据在缺失的 `CardTrait` SO）。

---

## ② 证据（`文件:行号` / 路径）

**代码（本批唯一改动）**
- `工具/gen_guid_map_from_bundles.py`
  · 文件头新增「短键束的 GUID 补集」章节（判据 / 取舍 / 用法）
  · `CAT_BIN_CANDIDATES` / `HEX32` / `_probe_module()` / `pick_catalog()` / `catalog_short_rows()`（新增）
  · `main()` 新增 `--catalog` / `--no-catalog` / `--catalog-include-scenes` / `--allow-conflicts`，并在写盘前挂补集；
    有冲突默认 `sys.exit(2)` **不写盘**
  · 复用（未改）：`工具/_guid_resolve_probe.py` 的 `load_catalog` / `resolve_guid` / `lookup`
- 干跑产物（`_tmp_view/`，**未覆盖正式表**）：
  · `d:/4/_tmp_view/wfC3/guid_map_bundles.cardsonly.dryrun.tsv`（= 现表 + 91 行，1,303,126 B）
  · `d:/4/_tmp_view/wfC3/guid_map_bundles.base.tsv`（`--no-catalog`，与现表**逐字节相同**，1,287,180 B）
  · `d:/4/_tmp_view/wfC3/dryrun_cardsonly2.log`（统计 + 2 条无束命中）
  · `d:/4/_tmp_view/wfC3/dup_check.py` / `xbuild_compare.py` / `android_names.py`（只读分析脚本）
  · `d:/4/_tmp_view/wfC2/cat_archive/`（PC catalog 解析）· `d:/4/_tmp_view/wfC2/cat_android/`（安卓 catalog 解析）

**基线复现（只增不改的硬证据）**
```
committed  guid_map_bundles.tsv          = 1,287,180 B / 6,173 数据行
--cards-only --no-catalog 干跑           = 与上面 **逐字节相同**
--cards-only（带补集）干跑               = 1,303,126 B / 6,264 数据行
前 6,173 数据行 vs 现表                  = **逐字节相同**；末尾多 91 行（source=catalog-internalid）
```

**两条无束命中的缓存束实读**
```
d:/2/关服前存档/新建文件夹/…/7416b641…/8657f84d…/__data （69,582 B）
  key '0' → Texture2D 'WarpForgeLogo Style' + Sprite 'WarpForgeLogo Style'
  key 'c' → Texture2D 'Cell Shaded Alternative Art Style' + Sprite 'Cell Shaded Alternative Art Style'
```
（`0195adda…` 的 internalId = `0`；`cd516d1b…` 的 = `c`，见 `cat_archive/catalog_locations.tsv`。）

**缺件音频的证据**
```
d:/4/Unity/数据/游戏数据/card_vfx_by_card.json → byCard.UM92._assetRefs:
  {soField: summonAudio, guid: 3192663f15f6ea846bf518eab1f0b1e2,
   assetName: VO_UM_Firestrike Servoturret - Battlefields are filled with the remains of the obstinate,
   assetBundle: spacemarinesultramarinescardassets_assets_all.bundle}
源：assets_full/bundle_spacemarinesultramarinescardassets_assets_all/AudioClip/<同名>.wav（247,468 B，PCM 16-bit mono 30614 Hz）
工程：Resources/Art/audio/vo/ 该名字 **0 个文件**（其余 696 个唯一名都在）
voice_lines.json：'Firestrike' / 'Servoturret' / 'obstinate' / 'Battlefields' 全 **0 命中**（UM92 整张卡缺失）
import_original_audio.py --check：<卡 SO 引用 698 / 唯一文件名 697 / 音频源里没有 0 条>
```

---

## ③ 没查清 / 留待裁决

1. **要不要把 91 行「补类型」也收进来**（同一 GUID 的 Sprite↔Texture2D 另一份）：本批按「名字相同 = 重复」**没有**追加；
   收的话增量再 +91 行左右。口径取决于下游是否区分 Sprite/Texture2D（`guid_map` 的现有约定是允许同 GUID 多行）。
2. **`alternateartstyles` 那 2 个短键**：名字已从关服前存档解出（见 ②），但束不在安装目录；要不要给生成器加
   「缓存束回退」= 另立账（它们不是卡资产，不影响卡池）。
3. **15 条场景短键**：`SceneInstance`，本批跳过。卡 SO 不引用场景 ⇒ 低优先；要收需加载 14 只场景束（慢），
   `--catalog-include-scenes` 已留口。
4. **安卓 catalog 的 2 条类型差异**（`ComputeShader` vs `VisualEffectAsset`，均为长键）：属平台差异，未深究。

---

## ④ 改动清单

| 文件 | 改动 |
|---|---|
| `Unity/工具/gen_guid_map_from_bundles.py` | 新增 catalog 短键补集（`CAT_BIN_CANDIDATES` / `catalog_short_rows` / 4 个 CLI 开关）；冲突默认不写盘；文件头补判据与用法。**正式 tsv 未写**。 |
| `d:/4/_tmp_view/wfC3/*`（未入仓） | 干跑产物 + 只读分析脚本（见 ②）。 |
| `Unity/资料/普查产出_1023/C3_guidmap短键与缺件音频.md` | 本报告。 |

**验过**：`python -I -X utf8` 跑通；干跑件与现表的关系逐字节核过（前缀相同 + 91 行追加）；未触碰
`数据/游戏数据/guid_map*.tsv`、`card_vfx_by_card.json`、`CardPresentation/**`、`WarpforgeVFX/**`、正本；
`d:/2` 只读（安卓束解压到 `_tmp_view/wfC3/android/`，未改存档）。

---

## ⑤ 顺手发现

1. **短键缺口不是「EC 专属」而是「短键束专属」**：catalog 里 241 个资产短键 GUID 的分布 =
   **EC `cardassets` 187 个（278 个 location）/ EC `cardanims` 52 个 / `alternateartstyles` 2 个**。
   其余 12 个阵营的卡束容器键**都是 32-hex** ⇒ 旧枚举路本来就覆盖。**为什么 EC 是短键 = 没查**（可能是打包时该束走了另一套命名，判据不在本地）。
2. **`anim_address_map.json` 也不是全覆盖**：91 条新增里有 **10 个**卡 SO 引用的 GUID 不在它的 `guid_to_asset`（其中 9 个连 `_assetRefs` 也没有，全是 EC 聊天语音）⇒ 覆盖本次补集对语音线是**真增量**，不是「重复数据」。
3. **`voice_lines.json` 已落后两张表**（10-18 vs vfx 10-20 / 音频目录 10-21）：除了本条缺件，UM92 整张卡的语音都没进表
   ⇒ 建议把「`import_original_audio.py` 是否该在 vfx 表重出后自动重跑」写进流程（**建议，未做**）。
4. **`guid_map` 的 `source` 列新增了一个取值**：`catalog-internalid`（旧值 = `bundle-container` / `old-map`）。下游若按 `source` 白名单过滤，需要同步（本仓现读只有生成器自己读这列）。
5. **PC 与安卓 catalog 的 GUID / internalId 全同**这件事本身值得记一笔：两张 catalog 的 `buildResultHash` 不同（`ae516dc0…` vs `0dd7cbfa…`），但**资产键表逐条相同** ⇒ 以后查「某个 GUID 在不在这个 build」用哪一份都可以（类型那 2 条除外）。
