# C6 · `T14`：修 `anim_address_map.guid_to_asset` 的短键真错（改生成器 + 落盘）

> 2026-10-23。**未跑 Unity、未动 git、未改正本、未动 `d:/2` 写、未动 `guid_map_bundles.tsv` / `.cs`**。
> 本批按主对话拍板：**修生成器，不手改产物**；做法 = catalog `internalId` 优先（二进制、与本地束同 build）。

---

## ① 结论

### 1·1 生成器改了什么（一句话）

`工具/gen_anim_address_map.py` 的 catalog 来源从**旧 JSON 快照**（2025-07-30，与本地束不同 build）改为
**优先二进制 catalog**（安装版 `aa/catalog.bin`，`buildResultHash ae516dc07ca5e8f47265a8f5783d7137`；
LocalLow `catalog_main.bin` 次之；旧 JSON **只在两份二进制都不在时兜底**），并新增统一的
`iter_catalog_locations()`（`.bin` 复用 `工具/_guid_resolve_probe.py` 的 `_load_catalog_bin`，`.json` 用原有解码）
⇒ 短键束按 **`(bundle, internalId)`** 解到**真实对象**。旧 JSON 的 `internalId` 是**截断键**
（例 `8c3458c0…` 它给 `8c`，本 build 真键是 `8`）—— 这就是 10 条真错的根因。

### 1·2 干跑结果（先写 `_tmp_view/wfC6/anim_address_map.dryrun.json`，未覆盖正式产物）

| 项 | 结果 |
|---|---|
| **12 条真错里「可判的 10 条」** | ✅ **10/10 全部纠正**（逐条见 1·3） |
| **另 2 条判不了的**（`c58ac7f6…` / `1cfd83ef…`） | ✅ **原样保留**（它们的类型是 `AudioMixerGroupController`，catalog 里**没有这两个 key**；不是短键问题） |
| **42 条「同 GUID 的 Texture2D/Sprite 两名」假阳性** | ✅ **一条没动**（逐条比：这 42 个 GUID 不在 changed/added 集合里） |
| `guid_to_asset` 体量 | **10,539 → 10,564**（**+25 新增** / **0 删除** / **10 条改值**） |
| `cardanim_to_asset` | **1,099 → 1,099（0 变化）** |
| `cardanim_guid_to_name` | **1,099 → 1,099（0 变化）** |
| `unresolved` | **0 变化** |
| 产物增量 | **+12,623 字节**（5,646,420 → 5,659,043 B） |
| 幂等 | 连跑两次（`--stamp` 固定），md5 **`f2e9cbd6344112929672e82e686b2ccf`** 两遍相同 |

**+25 新增是什么**：它们全是**短键束里之前根本没有 GUID 身份**的资产（旧 JSON 的 iid 截断/缺失），
二进制 catalog 给出了真键后第一次解出 —— **8 条 EC 卡框 Sprite/Texture2D + 17 条 EC 语音**
（`Kaphrael` 的 gen3/gen5/gen7/intro/wp/cantDo · `Lucius` 的 mirror/threat/gen2/gen5/vsEC/vsWE/vsFulgrim/vsDA/vsAstraMilitarum ·
`Xarahan` 的 attack/hurry/wp/gen5 等）。**不是**改已有条目，是补缺。

### 1·3 10 条「可判真错」逐条（现盘 → 干跑/落盘后）

| # | GUID | 旧名字（错） | **新名字（catalog 真值）** |
|---|---|---|---|
| 1 | `8c3458c061660f344a2608c23cd6ef89` | `VO_CSM_EC_Daemonette (Emily)` | **`VO_CSM_EC_Lord Kaphrael_concede`** |
| 2 | `ad93b47f3d8fe62438f11b7b12b14619` | `VO_CSM_EC_Blastmaster Noise Marine (Kevin)` | **`VO_CSM_EC_Lord Kaphrael_gen1`** |
| 3 | `92ba9ee361ae8a24ba31d08714b47671` | `VO_CSM_EC_Daemon Prince Xarahan_gen5` | **`VO_CSM_EC_Lord Kaphrael_greet`** |
| 4 | `c3244a98b92ed7b41a9e6f0dd8687e7b` | `VO_CSM_EC_Icon of Excess Infractor (Andrew Dennis)` | **`VO_CSM_EC_Lord Kaphrael_threat`** |
| 5 | `399710fd0d7e1b54f90166dabef5f130` | `VO_CSM_EC_Daemon Prince Xarahan_wp` | **`VO_CSM_EC_Lord Kaphrael_gen4`** |
| 6 | `5a6da3153cbdeeb4aaffda01d5bb1f8e` | `VO_CSM_EC_Lord Kaphrael_gen7` | **`VO_CSM_EC_Lord Kaphrael_attack`** |
| 7 | `c6bb19dd848ad4c8fb2de98c9e5c96df` | `CSM_EmperorsChildren_inf_Icon of Excess Infractor` | **`CSM_EmperorsChildren_warlord_Lord Exultant`** |
| 8 | `f0a86ec5e49d1464eaae7c24036e5c56` | `CSM_EmperorsChildren_dae_Blissbringer` | **`CSM_EmperorsChildren_strat_Tools of Torture`** |
| 9 | `b0f1e4fdde1b58f4595c067f48a6b891` | `40k_Cardframes_stratagem_Emperor Children_tier2` | **`CSM_EmperorsChildren_offensive_Aural Hijack`** |
| 10 | `a062d6b02506b2342b1c9bcb2b0a0201` | `40k_Cardframes_stratagem_Emperor Children_tier4` | **`CSM_EmperorsChildren_strat_Elixir_Skorflense`** |

（每条的 `pathId` / `dump` 同步改了；第 7–10 条的 `type` 在 Texture2D↔Sprite 之间换了主档 —— 两种类型都在 `also` 里，名字都是真值。）

### 1·4 落盘与核对

- 正式产物 **已覆盖**：`数据/索引/anim_address_map.json` = **5,659,043 B / md5 `f2e9cbd6…`**，与干跑件**逐字节相同**；行尾 **LF 未翻**（crlf 0）。
- `generatedAt` 保持原值 `2026-10-19 22:25:40`（用 `--stamp`，**不引入时间戳 diff**）。
- `git diff --numstat`：
  - 产物：**`761 / 503`**（⚠️ **行级**；**内容级只有 10 改 + 25 增 + 14 个 stats 字段** —— 见 1·5）
  - 生成器：**`136 / 44`**
- **回归**：重跑 `import_original_audio.py` 一次 ⇒ **`voice_lines.json` md5 完全不变**（`d51400179329c2cd74ddae776ea493a9`，0 变化）⇒ 导入器已优先读 `guid_map`，本修复对它无副作用。

### 1·5 为什么行级 diff 是 761/503（而内容级只有 35 条）

逐路径深度对比（`_tmp_view/wfC6/deepdiff.log`）：**全部 76 个差异路径 = `guid_to_asset` 62 条 + `stats` 14 条**；
`cardanim_to_asset` / `cardanim_guid_to_name` / `unresolved` / `sources` / `howToJoin` / `schema` / `generatedBy` **逐字段相同**。
行级 diff 被放大的两个原因（都是**格式/位置**，不是内容）：
1. JSON 是 `indent=1`，每个字段一行；**25 个新 GUID 键插在（近似有序的） 10k 行字典中间**，每插一处就把后面整块"推"一下；
2. 10 条改值 + 少数 GUID 因配对从短键 `8c` 换到 `8` 而**位置微移**（`difflib.SequenceMatcher` 相似度 **0.9953**）。
⚠️ **没有删除任何 GUID**（`removed = 0`）；**没有重排 `cardanim_*` 两张表**。

---

## ② 证据

**代码**
- `工具/gen_anim_address_map.py:92-112`：新增 `CATALOG_BIN_CANDIDATES`（安装版 → LocalLow）+ 为什么必须二进制（旧 JSON 截断键 ⇒ 10 条真错）。
- `:328-390`：新增 `_probe_module()` / `iter_catalog_locations(path)`（`.bin` 走 `_guid_resolve_probe._load_catalog_bin`；`.json` 走 `decode_catalog_json`，两个分支产出同一四元组）。
- `:392-…`（`load_catalog_internal_ids`）：catalog 选择改为 **bin → json**，主体改为消费 `iter_catalog_locations()`；返回值/打印带 `（二进制，与本地束同 build）`。
- `:780-790`：新增 `--out`（干跑落点，默认仍写正式产物）。
- `:1332` 附近：stats 增 `catalogKind`（`bin`/`json`）；`catalogJson` 现为 `.bin` 路径。
- 文件头 `:49-56` 补 2026-10-23 的判据与过期说明（旧「只读 catalog_main.json」=> 「二进制优先」）。

**干跑/落盘日志与对拍**
- 干跑：`d:/4/_tmp_view/wfC6/dryrun.log`（`catalog: …catalog.bin（二进制，与本地束同 build）；短键容器项校验后升级 330 条（跳过 0 条）`）·
  `dryrun2.log`（第二次，md5 相同）
- 产物：`d:/4/_tmp_view/wfC6/anim_address_map.dryrun.json`（5,659,043 B，md5 `f2e9cbd6…`）· 现盘同 md5
- 基线快照：`d:/4/_tmp_view/wfC6/anim_address_map.before.json`（5,646,420 B，md5 `f2999876…`）
- 对拍脚本：`d:/4/_tmp_view/wfC6/compare.py`（逐 GUID 增/删/改）· `deepdiff`（逐路径；非 `guid_to_asset` 的只有 stats）
- catalog 真值（逐条可复跑）：`工具/_guid_resolve_probe.py g <GUID>`；本报告 1·3 的每条都可用它复核（例
  `8c3458c0…` → key `8` → `VO_CSM_EC_Lord Kaphrael_concede`，PathID `4754310492230562471`）。
- 交叉验证：新产物里 **1,859 个 AudioClip GUID** 与 `guid_map_bundles.tsv` 的 AudioClip 行**逐条同名（0 mismatch）**。

**stats 变化（二进制 vs 旧 JSON）**
```
containerEntries                 13,747 → 13,778
containerEntriesGuidKey          13,402 → 13,433
containerEntriesGuidViaCatalog      245 → 330
containerEntriesGuidUpgradeSkipped  {exact=False:23, 类型不符:6} → {}（0）
containerEntriesGuidViaVerifiedRetreat 54 → 0（31 GUID；二进制已直接解，不再走退键）
uniqueGuids                      10,318 → 10,343 (+25)
guidsWithMultipleObjects          2,575 → 2,581
dumpPathComputed                 13,728 → 13,759
catalogJson                      …catalog_main.json → …aa/catalog.bin；catalogKind: null → "bin"
```

---

## ③ 没查清

1. **+25 条新增的消费面**：它们是真值（catalog 真键 + 束内对象实读），但**哪些下游会因此多查到东西**只核了语音线（`import_original_audio` 0 变化）；其它消费方未逐个盘（本批白名单外）。
2. **2 条判不了的**（`c58ac7f6…`→`FX` · `1cfd83ef…`→`BattleGroupHigh`）：类型 `AudioMixerGroupController`，catalog 无 key —— **保留原值**；它们是不是「旧 build 的名字」没查（与短键缺口无关）。
3. **行级 diff 的位置噪声**：要不要让生成器输出**稳定的键序**（例如全排序）是另一条口径 —— 本次没动（全排序会把现有"近似有序"的整表重排，diff 更大）。

---

## ④ 改动清单

| 文件 | 改动 |
|---|---|
| `Unity/工具/gen_anim_address_map.py` | **+136/−44**：catalog 二进制优先（`CATALOG_BIN_CANDIDATES`）+ `iter_catalog_locations()` 统一两路 + `load_catalog_internal_ids` 改用 + `--out` + `catalogKind` 统计 + 文件头/注释订正；**正式产物不在本文件里手改任何值** |
| `Unity/数据/索引/anim_address_map.json` | 重生成：**10 条改值 + 25 条新增 + 14 个 stats 字段**；0 删除；`cardanim_*`/`unresolved` 0 变化；5,646,420 → **5,659,043 B**；LF；`generatedAt` 不变 |
| `d:/4/_tmp_view/wfC6/*`（未入仓） | 干跑件 / 基线快照 / 日志 / 对拍脚本 |
| `Unity/资料/普查产出_1023/C6_T14_anim_address_map.md` | 本报告 |

**验过**：幂等（两跑同 md5）· 与干跑件逐字节相同 · 行尾 LF 未翻 · `guid_map_bundles.tsv` / `voice_lines.json` / `.cs` / `d:/2` **未动** · 未跑 Unity · 未改正本。

---

## ⑤ 顺手发现

1. **「退键族」这条路现在自然归零**：旧版用 `exact=False` 的 31 个 GUID 走「三元组唯一命中」升级（54 条容器项）；二进制 catalog 直接给出真键 ⇒ `containerEntriesGuidViaVerifiedRetreat` **54 → 0**。代码**保留**（只有 JSON 兜底时才用得上），不删。
2. **旧 JSON 与本地束不是同一 build 这件事，影响面比预想大**：除 10 条真错外，还有 **25 个短键 GUID 在旧 JSON 里根本没有**（不是配错，是没配）；二进制 catalog 一次补齐。⇒ 未来任何「靠 catalog_main.json 建索引」的脚本都该换成 `aa/catalog.bin`（**建议**，未动其它脚本）。
3. **`catalogKind` 新字段**：`bin` / `json`；下游若要判「这份产物用的是不是同 build 的 catalog」，读它即可（旧文件没有这个键 = json 时代）。
4. **`catalogJson` 字段名现在是历史包袱**（值可能是 `.bin`）—— 为兼容下游**没改名字**，只加了 `catalogKind`；要改名得同步所有读者（**未做**）。
5. **索引产物里的键序是「近似有序、但非全序」**（由包/容器遍历顺序决定）：这就是 `git diff` 行数会被放大的结构性原因；本轮**没动**，若要长期降低 diff 噪声，建议另立一条「索引产物统一排序」的账（**建议，未做**）。
