# C7 · `anim_address_map.json` 消费方审计（只读）

> 2026-10-23。执行代理 C。**只读**：未改任何代码/数据、未动 git、未跑 Unity、未改正本、`d:/2` 只读。
> 对象 = `C6/T14` 那次更新（**10 条改值 + 25 条新增**，`guid_to_asset` 10,539→10,564；
> `cardanim_to_asset` / `cardanim_guid_to_name` **0 变化**）会不会改变各消费方的行为。

---

## ① 结论

### 1·1 真正**解析**该文件的读点（全仓 7 处 + 2 处间接）

| # | 读点 | 读哪一节 | 用途 | 本次更新的影响 |
|---|---|---|---|---|
| 1 | `工具/gen_card_vfx_by_card.py:118` → `:322-346`（`AddrIndex`） | `cardanim_to_asset`（`:326`）· `guid_to_asset`（`:327`，建 `by_pathid`） | `:427` CardPrefab 的 PPtr 按 pathId 反查；`:535` `BattleAnims.heal` 按 GUID 反查；`:564` `RangedAttackParticlesByArmy` 按 pathId 反查 | **不变**：① `cardanim_to_asset` 逐字节相同；② **`pathId` 索引完全一致**（更新前/后都 **13,432 个 pid**、**新增 0 / 删除 0 / first-wins 名字变化 0**）；③ `heal` 的 GUID `4ac621b3…` **不在** 10 改 / 25 增里 |
| 2 | `工具/gen_animfx_modules.py:53` → `:128-129` | **只** `cardanim_guid_to_name` + `cardanim_to_asset` | 生成 `animfx_modules.json`（模块数据） | **不变**（两节 0 变化） |
| 3 | `工具/gen_adjacent_vfx_animinfo.py:62` → `:153-154` | **只** `cardanim_guid_to_name` + `cardanim_to_asset` | 生成 `adjacent_vfx_animinfo.json` | **不变**（同上） |
| 4 | `工具/gen_cardanim_clips.py:58` → `:65` | **只** `cardanim_to_asset` | 生成 `cardanim_clips.json` | **不变** |
| 5 | `工具/import_original_audio.py:120` → `:331,368` | `guid_to_asset`（**第三来源/兜底**，排在 `_assetRefs` 与 `guid_map` 之后） | 卡 SO 的 12 个 ChatSound → 音频名 | **不变**（实测重跑导入器：`voice_lines.json` **md5 完全不变** `d5140017…`） |
| 6 | `工具/verify_t15_anim_chains.py:51` → `:107-109,173-190` | `guid_to_asset` + `cardanim_guid_to_name` + `cardanim_to_asset` | **验证工具**（27 个 EC CardAnim 链 + 旧表对账） | ⚠️ **1 条断言变红**（**不是产品缺陷**，是断言过期）：`:189` 要求 `stats.containerEntriesGuidUpgradeSkipped["旧表 exact=False…"] > 0`；二进制 catalog 下该计数为 **0**（不再需要「整族跳过」）⇒ 报 FAIL。其余 **24 条全 OK**（含「exact=False 的行没有被对到别的对象」`wrong=0` —— 真正关心的那条**通过**） |
| 7 | `工具/gen_anim_address_map.py:114,1264` | 自己产出 / 内部对账 | 生成器 | 本批已重跑，无异常 |
| 间接 A | `工具/gen_card_anim_order.py:77` → `:175` | 读 `数据/游戏数据/card_anim_order.json`（**静态产物**） | 生成运行期瘦身表 | **不变**：该产物里**没有任何** 10 改/25 增的 GUID（逐条 grep = 0） |
| 间接 B | `工具/check_new_card.py:60,252` | 读 `card_anim_order.json` | 新卡检查 | **不变**（同上） |

### 1·2 三个特别核的问题

**① 25 个新增 GUID 里，有没有卡池内的卡会因此换语音 / 换特效？—— 0 张。**
- 25 个新增 = **19 AudioClip + 5 Sprite + 1 Texture2D**；**没有任何 CardAnim / GameObject prefab** ⇒ **VFX 路径 0 影响**。
- 其中 **10 个 AudioClip 被卡 SO 引用**（`EC55 Lucius` 的 mirror/threat/gen2 · `EC56 Kaphrael` 的 gen3/intro/wp/cantDo · `EC57 Xarahan` 的 attack/wp/hurry），
  但**唯一读 `guid_to_asset` 的语音消费方 = 导入器**（第三来源），而它在 C5 起就已由更高优先级的 `guid_map` 解出同一条 → 本次重跑 **0 变化**。
- 另外 9 个 AudioClip（`Lucius` 的 gen5/vsEC/vsWE/vsFulgrim/vsDA/vsAstraMilitarum 等）+ 6 个卡框图 **不被任何卡 SO 引用** ⇒ 纯补缺、无消费。

**② 10 条改值里的 4 条美术/卡框 GUID，有没有被 UI 用到？—— 今天没有。**
- 4 条都是 `cardImageReference`（`EC56` / `EC60` / `EC62` / 一个 32-hex 卡），但**没有任何 UI/管线经 `anim_address_map` 解 `cardImageReference`**：
  `import_original_art.py` 用自己的 GUID 表；`gen_card_vfx_by_card.py` 的立绘走 `guid_map.tsv`（**不是** `AddrIndex`）；运行时卡面走 `Resources`（`CardArt`）。
- 交叉验证：`card_vfx_by_card.json`（用 guid_map 建的）对这 4 条的 `assetName` **已经**是新名字 ⇒ **新值才是和既有产品一致的那一份**，旧值是孤立的错值。
- 另 6 条改值是 EC 语音（上面已判 0 变化）；`399710fd…`（Xarahan_wp）当前卡 SO 不引用。

**③ `catalogKind` / `catalogJson` 新字段会不会让按旧 schema 严格解析的人炸？—— 不会（0 个读者）。**
- 全仓 grep `catalogKind` / `catalogJson`：**只有生成器自己**（`:1332-1336`）；**没有任何**下游读这两个键。
- 所有读点都是 `doc.get("guid_to_asset")` 这一级（Python 字典），**没有 JSON Schema 校验 / 没有「顶层键必须全等」的断言**；`catalogJson` 只是值从 `.json` 变成 `.bin`（键名没改）。
- ⚠️ 唯一因 stats 变红的是 `verify_t15`（见 1·1 #6），那是**语义断言过期**，不是 schema 解析失败。

### 1·3 一句话总结

**这次更新对全部消费方：6 个「不变」· 1 个验证工具「断言过期变红（产品无错）」· 0 个「取到新名字导致行为改变」。**
`guid_to_asset` 的 10 改 / 25 增目前**没有会改变运行时/工具产物的读点**（`pathId` 索引逐条相同是硬证据）；
它的价值是**把索引本身修正到与 `guid_map`/`card_vfx_by_card` 一致**，供未来读点使用。

---

## ② 证据

### 2·1 搜索口径（搜过的词 / 范围）
- **词**：`anim_address_map`（字面量）· `guid_to_asset` · `cardanim_to_asset` · `cardanim_guid_to_name` · `catalogKind` · `catalogJson`；另交叉搜 `cardImageReference`。
- **范围**：`d:/4/Unity` 全树（排除 `MyGame/Library` 包缓存与 `MyGame/Temp` 临时副本，二者命中见下）。
- **命中文件类型**：`md 56 · py 9 · json 9 · cs 9 · log 2`（`.log` / `_tmp_view` 备份均为历史日志，不作读点）。
- **`MyGame/Temp/ScriptUpdater/1655989618/214480239_BattleScene.cs`** 是编辑器临时目录里的副本（**未逐字节比对**，见 ③·4），按「临时副本、不参与构建」处置，**不是消费者**。

### 2·2 读点的逐处出处
- `gen_card_vfx_by_card.py:118`（`ADDR_MAP = …`）· `:322-346`（`AddrIndex.__init__` / `prefab_of` / `asset_of_guid` / `asset_of_pathid`）· 调用 `:427`（`resolve_pptrs(CARD_SCRIPT_MONO…)`）· `:535`（`BattleAnims.heal`）· `:564`（`RangedAttackParticlesByArmy`）。
- `gen_animfx_modules.py:53`（`ADDR_MAP`）· `:124-129`（`by_guid = …cardanim_guid_to_name`；`by_name = …cardanim_to_asset`）。
- `gen_adjacent_vfx_animinfo.py:62`（`ADDR_MAP`）· `:153-154`（同两节）。
- `gen_cardanim_clips.py:58`（`ADDR_MAP`）· `:65`（`c2a = am["cardanim_to_asset"]`）。
- `import_original_audio.py:120`（`ANIM_ADDRESS_MAP`）· `:331,368`（第三来源；`idx.setdefault` ⇒ 低于 `_assetRefs`/`guid_map`）。
- `verify_t15_anim_chains.py:51`（`AMAP`）· `:107-109`（三节）· `:173-190`（旧表对账；**`:189-190` 是过期断言**）。
- 间接：`gen_card_anim_order.py:77`（读 `card_anim_order.json`）· `check_new_card.py:60,252`（同）。

### 2·3 关键核对（可复跑）
- **`pathId` 索引不变**（`d:/4/_tmp_view/wfC7/pid_check.py`）：
  ```
  pids before 13432 after 13432 · pids added 0 removed 0 · first-wins name changed 0
  ```
  （这正是 `gen_card_vfx_by_card.AddrIndex.by_pathid` 的语义：`setdefault(pid, rec)`，按 JSON 顺序 first-wins。）
- **heal GUID 不在变更集**：`4ac621b34e639034f9fe743adbd9b882` 在 map 里（`:11416`），未被改/未被新增。
- **语音回归**：`import_original_audio.py` 重跑后 `voice_lines.json` md5 = `d51400179329c2cd74ddae776ea493a9`（与重跑前相同）。
- **`verify_t15` 全量**：`d:/4/_tmp_view/wfC7/verify_t15.log` —— **24 OK / 1 FAIL**（FAIL = `stats.containerEntriesGuidUpgradeSkipped` 过期，见 1·1 #6）。
- **交叉验证（4 条美术）**：`card_vfx_by_card.json` 的 `_assetRefs` 里这 4 个 GUID 的 `assetName` = `CSM_EmperorsChildren_warlord_Lord Exultant` / `…strat_Tools of Torture` / `…strat_Elixir_Skorflense` / `…offensive_Aural Hijack` —— **与新 anim map 逐字相同**（旧 anim map 是唯一不一致处）。
- **25 新增的卡 SO 引用**：10 个 AudioClip 被引用（EC55/56/57 的 chat/attack 字段），其余 15 个无引用；**无 CardAnim/GameObject**（见 1·2①）。
- **`card_anim_order.json`（全量 + 运行期）**：10 改/25 增的 GUID **出现 0 次**（逐条 grep）。
- **`catalogKind`/`catalogJson`**：仅 `gen_anim_address_map.py:1332-1336` 写；全仓无其它读者。

### 2·4 非读者（只有注释/文档引用，列出来免得下次重查）
- `.cs`（**全是注释**，无 `File.`/`Resources.Load`/`JsonUtility` 读）：`Battle/BattleDriver.cs:11018` · `Deck/DeckRuntime.cs:3712,3732` · `Core/DraggableController.cs:74` · `Core/TraitParticles.cs:28` · `Core/TraitFrames.cs:78,121` · `Core/VfxMap.cs:249,315,373` · `Editor/BattleScene.cs:7978,8549` · `WarpforgeVFX/Runtime/WFModuleInstanceParticleAdjacent.cs:74,77,121` · `WarpforgeArena1/Editor/EffectExporter.cs:892,900,934` · `Shell/LiveOpsLocalCatalog.cs:144`。
- `工具/extract_missing_shaders.py:197-198,297`（注释里的审计结论）· `工具/read_itemdrawerconfig.py:59,111`（注释）· `工具/gen_cardanim_clips.py:20-21`（文件头注释）· `资料/AnimFX_实现与接线.md:168`（文档）。
- `MyGame/Assets/CardPresentation/Resources/{cardanim_clips,voice_lines}.json` · `数据/游戏数据/{adjacent_vfx_animinfo,cardanim_clips,card_anim_order,card_vfx_by_card}.json` —— 里面只是**元数据字符串**提到该文件，不是读点。
- `_tmp_view/**` 的 `.bak` / `.log` —— 历史备份/日志，不是读点。

---

## ③ 没查清

1. **`verify_t15_anim_chains.py` 的过期断言要不要改** —— 本批只读，**未改**；它现在退出码 1。建议把 `:189-190` 改成「二进制 catalog 下 `skipped == 0` 也算通过，并另断 `wrong == 0`（已有）」，或直接断言 31 个 `exact=False` GUID 现在都解到正确名字（更贴 T14 的目的）。
2. **仓库外/历史脚本**：只盘了 `d:/4/Unity`；`d:/2` 的 `Warpforge_tools` 下若有脚本读这份索引，**没盘**（按红线 `d:/2` 只读，且它们属另一仓）。
3. **静态产物未重生成**：`card_anim_order.json`、`card_vfx_by_card.json`、`animfx_modules.json` 等**没有**因本次更新重跑 —— 分析结论是「不需要」（相关 GUID 不出现或名字本就一致），但**没有用重跑做端到端证明**（重跑会写产物，超出只读范围）。
4. **`MyGame/Temp/ScriptUpdater`** 只按「同内容副本」处置，未逐字节比对（它是编辑器临时目录，不参与构建）。

---

## ④ 改动清单

**无**（本批只读：未改代码/数据，未动 git，未跑 Unity，未改正本）。只新增本报告与 `_tmp_view/wfC7/` 下的只读分析脚本/日志。

---

## ⑤ 顺手发现

1. **`verify_t15_anim_chains.py` 现在会红**（1/25）：断言 `stats.containerEntriesGuidUpgradeSkipped["旧表 exact=False…"] > 0` 是**旧路径的产物**；二进制 catalog 下这个计数本来就是 0。**真正要守的那条（exact=False 的行没被对错对象）是绿的**（`wrong=0`）。建议主对话按 ③·1 更新它。
2. **旧数字散在注释/文档里**：`10,539` 至少还在 `工具/extract_missing_shaders.py:297` · `工具/gen_cardanim_clips.py:21` · `WarpforgeArena1/Editor/EffectExporter.cs:934` 三处注释里（现读 **10,564**）。**不影响行为**，但按铁律 5 迟早要订正（本批只读，未动）。
3. **`catalogKind` / `catalogJson` 目前是「写而不用」**：下游没人读。若将来要判「这份索引是不是同 build 的 catalog 产出」，读 `catalogKind == "bin"` 即可；但注意 `catalogJson` 这个**键名仍是历史的**（值现在是 `.bin`），第一次接的人容易被名字误导。
4. **`jump` 风险最高的一处其实是 `gen_card_vfx_by_card` 的 `by_pathid`**（全局 pid 索引、first-wins）—— 这次**恰好** pid 集合完全不变（13,432），所以安全；以后若新增条目带来**新 pid**，要重跑这条审计（脚本 `_tmp_view/wfC7/pid_check.py` 可直接复用）。
5. **`import_original_audio` 的三级来源现在是「`_assetRefs` → `guid_map` → `anim_address_map`」**：本次更新对它是 0 变化，但也意味着**它已经不会再吃到 anim map 的旧错值**；如果将来有人把 `guid_map` 从它的来源里拿掉，anim map 现在也已经是**对的**（本批修复的价值之一）。
