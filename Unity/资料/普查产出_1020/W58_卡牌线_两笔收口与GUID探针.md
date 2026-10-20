# W58 · 卡牌线两笔收口与 GUID 探针（`W58-CARDFIX2`）

> 执行代理：`W58-CARDFIX2` · 2026-10-20。
> 来源：`资料/普查产出_1020/W58_卡牌线_剩余收口.md` §四·2（两处 `desc` 少一句）与 §三（`A2037②`）。
> 口径：**只做不验** —— ⛔ 未跑 Unity / `*.Run` / player / 截图；✅ 只跑纯 python 生成器、`ruleprobe`（纯 dotnet）、`gen_cardface_fixes.py --check`、`gen_cards_engine.py --check`。
> 未碰任何 `.cs`、正本、`CLAUDE.md`、`项目任务.md`、任务线总表；未重钉任何 `Pinned*Md5`。

---

## 〇、结论

1. **A 两卡 `desc` 已补**（`UM80` 缺 `Vanguard.` · `SAU28` 缺 `Flying.`），走 `cardface_fixes.json` → `gen_cards_engine.py --write` 正规通道。卡池**恰好 2 张卡、每张只动 `desc`**。
2. **B `A2037②` 已修**：`_guid_resolve_probe.py` 改为**二进制 catalog 优先**（安装版 `aa/catalog.bin` `ae516dc0…` → LocalLow `catalog_main.bin` `54383599…` → JSON 仅在两者都不在时回退并出声）。重生成后 **56 条退键 56/56 一致、`exact=False` 归零**；**5 条真错全部改正**。
3. **下游重生成**：`guid_map.tsv`、`guid_map_bundles.tsv`、`card_vfx_by_card.json`（数据份 + 运行时份）。4 条在池引用改正；EC62 不在池内 `byCard`，但其 map 行已改正。
4. **两处与任务书假设不同的现核（如实记）**：① 这两张卡的引擎 `keywords` 列**本来就含** `Vanguard` / `Flying` ⇒ `Has()` 改前改后都为真（`desc` 是 `EffectText` 的输入，静态词对结算不承重；补 `desc` 修的是卡面文字/次序与解析输入）；② `A2037②` 的实际消费链是 **`guid_map_bundles.tsv`**（不是旧 `guid_map.tsv`），所以两份都重生成，否则 `card_vfx` 不会变。

---

## 一、A 两卡证据与改法

### 1·1 三票同值

| 卡 | PnP 成品卡图亲读 | 官方本地化（`官方本地化_en+zh-CN.json`） | 开图抄录 `_合并总表.md` |
|---|---|---|---|
| `UM80 Bladeguard Sergeant` | `Ultramarines/3部队/bladeguard seargant.png`：`〔盾〕Armour 1. 〔骷髅〕Vanguard. 〔闪电〕Rally: …` | `Card_Description/UM80` = `[[armour]] 1. [[vanguard]]. [[rally]]: …` | `:1073` 同值 |
| `SAU28 Ghost Ark` | `Necron/3部队/Warpforge_36_Ghost-Ark.png`：`〔翼〕Flying. 〔书〕Talent: Reanimate. 〔齿轮〕Artifice: Deploy a Necron Warrior` | `Card_Description/SAU28` = `[[flying]]. [[talent]]: …` | `:534` 同值 |

两张 `descZh` **本来就对**（`[[vanguard]]。` / `[[flying]]。`），**未动**。

### 1·2 改法与写入位置

落点：`数据/游戏数据/cardface_fixes.json` 的 `desc` 列（键 = **源表名**，两卡均唯一；`Bladeguard Sergeant` / `Ghost Ark`），并在 `_manual_desc_note` 加留痕键 `_2026-10-20_W58_UM80_SAU28_desc补词`。

| 卡 | 旧 `desc` | 新 `desc` |
|---|---|---|
| `UM80` | `Armour 1. Rally: If you control no other troops, gain Armour 1.` | `Armour 1. Vanguard. Rally: If you control no other troops, gain Armour 1.` |
| `SAU28` | `Talent: Reanimate. Artifice: Deploy a Necron Warrior.` | `Flying. Talent: Reanimate. Artifice: Deploy a Necron Warrior.` |

只插入缺的那个词；**其余字符（含句末句点）逐字节照抄旧值**。依据 = `_2026-10-11_A1475_desc口径` ③ 第一档（静态关键词缺在 `desc` 里 ⇒ 补 `desc`）。

### 1·3 解析验证（纯 dotnet `ruleprobe play`，改前/改后逐行比）

| 卡 | 关键词集合（改前 = 改后） | 差异 |
|---|---|---|
| `UM80` | `armour1, vanguard1, rally1` | **仅**「卡面（英文）」一行由旧值变新值；事件电池/落场/伤害等其余输出**逐行相同** |
| `SAU28` | `flying1, talent1, artifice1` | 同上，**仅**「卡面（英文）」一行变 |

⇒ **两张卡本来就获得 `Vanguard` / `Flying`**（来源是卡池 `keywords` 列，不是 `desc`）；本次补 `desc` 未改变任何结算读数。

另用 `ruleprobe card` 对**旧池（`WFPROBE_POOL=<改前快照>`）与新池**逐行比解析结果：

- `UM80`：`kw=[armour,vanguard,rally]` 不变；`parse` 只多了 `Vanguard.` 这一段，`op`（`gain armour 1` + 条件 `ownnoothertroops`）**逐字段相同**，`unparsed=` / `partial=` 均为空。
- `SAU28`：`kw=[flying,talent,artifice]` 不变；`op` 仍是 `create Reanimate` + `deploy Necron Warrior` **两条、逐字段相同**，`unparsed=` / `partial=` 均为空。

### 1·4 卡池 diff（`cards_engine.json`）

| 项 | 值 |
|---|---|
| 生成器 | `python -I -X utf8 工具/gen_cards_engine.py --write`（连跑 2 次） |
| `count` | **1127 / 1127**（不变） |
| id 集合 | 新增 0 · 消失 0 |
| 逐卡 diff | **恰好 2 张**：`UM80`（`desc`）· `SAU28`（`desc`） |
| md5（改前） | `0441f4ec2e1832b4633c2c7393f580fd`（1,102,408 B） |
| md5（改后，两次 write 逐字节一致） | **`a56aa0bac2bcac00049eadd3ff2790dd`**（1,102,426 B） |
| `--check` | 一致 ✅ · rc=0 |
| `gen_cardface_fixes.py --check` | `subtype`/`keywords` 两列差 0 处 · 裸卡名键唯一性 ✅ · rc=0 |
| 行尾 | `cardface_fixes.json` **CRLF 2212 = LF 2212**（未翻）· `cards_engine.json` 单行无换行 |

> ⚠️ `git diff --numstat` 里 `cardface_fixes.json` 的 **128/23** 含**上一代理 `W58-CARDS` 那 17 张关键词重排**（同一文件、未提交）；本笔自身只加 **2 条 `desc` + 1 条留痕**（3 行）。`cards_engine.json` 为单行 JSON，**1/1** 是单行形态，改动量按上面的逐卡 diff 判。

---

## 二、B 探针修复与重生成数字

### 2·1 探针改法（`工具/_guid_resolve_probe.py`）

1. 删除 `CAT = …catalog_main.json`，改为候选序列：
   - ① `d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/catalog.bin`（`ae516dc0…`，14,307 locations，**与本地束同 build**）
   - ② `C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/com.unity.addressables/catalog_main.bin`（`54383599…`，14,301 locations）
   - ③ **仅当 ①② 都不存在**才回退 JSON（`a011a011…`），并在 stderr 出声「与本地束不同 build，结果不可信」。
2. 二进制解析复用同目录 `工具/parse_catalog.py`（`importlib` 按文件加载 —— `python -I` 不把脚本目录放进 `sys.path`）。
3. 二进制 → 与 JSON 同形的 `keys / buckets / ids / ents / rtypes`；`resolve_entry` 增加 `kind='bin'` 分支（`dependencySet` 直接展开束 location id）。
4. `main()` 增加打印 `catalog=` / 来源 / `buildResultHash` / `kind` / `locations`。

**回退验证**：显式用 LocalLow `catalog_main.bin`（`54383599…`）复跑同一 5 个 GUID，结果与安装版**逐条相同**（含 `exact=True` 与资产名）。

### 2·2 `guid_map.tsv`（`资料/普查产出_第二十七会话/`）

| 项 | 改前 | 改后 |
|---|---|---|
| 行数 / GUID | 4,985 / 2,372 | **4,997 / 2,374** |
| `exact=False` | **56 行 / 31 GUID** | **0** |
| md5（连跑两次逐字节一致） | `e4f4fc73c21c5f636f0e9d0353bda4f2` | **`eae8b7f9badb21df725675ff5727cf3e`**（1,229,682 B，两次相同） |

**56 条退键 56/56 一致**：按 `(assetGuid, bundle, containerKey, type, name)` 逐行对，**56/56 全命中**（`containerKey` 与资产名一字不变，只有 `internalId` 由旧 JSON 的短前缀改成二进制真值、`exact` 由 `False` 变 `True`）。

**净变化**：5 条真错行（下节）+ 16 条**新解出的覆盖增量**（2 个 GUID：`febb0660…` → `FlameBurst_Sororitas`、`5251c0f9…` → `Artillery Ground`，均在 `cardanimsgeneral_assets_all.bundle`；旧 JSON 目录里根本没有这两条）。

### 2·3 5 条真错（两处 catalog 对比 + 束内打开验证）

| 卡 | 字段 | 旧（JSON 目录，错） | 新（二进制目录，对） |
|---|---|---|---|
| `EC60 Armoury of Excess`（源表名 `Tools of Torture`） | `cardImageReference` | `f0` → `CSM_EmperorsChildren_dae_Blissbringer` | **`f0a` → `CSM_EmperorsChildren_strat_Tools of Torture`** |
| `EC56 Lord Kaphrael` | `cardImageReference` | `c6` → `CSM_EmperorsChildren_inf_Icon of Excess Infractor` | **`c6b` → `CSM_EmperorsChildren_warlord_Lord Exultant`** |
| `EC56 Lord Kaphrael` | `attackAudio` | `5a` → `VO_CSM_EC_Lord Kaphrael_gen7` | **`5` → `VO_CSM_EC_Lord Kaphrael_attack`** |
| `4eec3b13… Skorflense` | `cardImageReference` | `a0` → `40k_Cardframes_stratagem_Emperor Children_tier4` | **`a06` → `CSM_EmperorsChildren_strat_Elixir_Skorflense`** |
| `EC62 Aural Hijack` | `cardImageReference` | `b0` → `40k_Cardframes_stratagem_Emperor Children_tier2` | **`b0f` → `CSM_EmperorsChildren_offensive_Aural Hijack`** |

### 2·4 `guid_map_bundles.tsv`（`资料/普查产出_1018/cardanim_guid_map/`）

`card_vfx_by_card.py` 的默认输入是这一份（⛔ 不是旧 `guid_map.tsv`）。按既有口径用 `--cards-only` 重建（默认全扫会多出 ~4,300 条非卡束行；`--cards-only` 是 `gen_cards_engine.py` 注释里的标准重建命令）。

| 项 | 改前 | 改后 |
|---|---|---|
| 行 / GUID | 6,173 / 6,140 | **6,173 / 6,140**（不变） |
| `exact=False` | 56 行（old-map 回填） | **0** |
| md5（连跑两次逐字节一致） | `d8777661d9f1ab34a7beb6f6b19ca5ed` | **`c0621b94c3d96df83ff2bb82ed817ecc`**（1,287,180 B，两次相同） |

**逐行 diff**：36 行变化 = ① **31 行**（56 条退键那 31 个 GUID，`internalId` 改正 + `exact False→True`）② **5 行**（上面 5 条真错，`containerKey`/`internalId`/`name` 一起改正）。`old-map` 回填 148 行**条数不变**。

### 2·5 `card_vfx_by_card.json`

| 文件 | 改前 md5 | 改后 md5（两次相同） | 字节 |
|---|---|---|---|
| `数据/游戏数据/card_vfx_by_card.json` | `e63f72dbd9668d9e5b53283a10332f29` | **`b1ab2e16a92ae47ffecccd223201e965`** | 2,874,074 |
| `MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json` | `f88c079514b0ec8a6448ca30e5548eca` | **`e8bb2a19266877e60d7ef7ed1df69f9d`** | 389,817 |

生成器改动：新增 `--generated-at`（默认仍取当前时间；仅用于确定性复跑，⛔ 不改默认行为）。两次复跑均带 `--generated-at "2026-10-20 19:30:00"`。

**`byCard` 逐卡 diff（按语义比较，忽略 `generatedAt`）**：13 张卡有变化，分成两类：

1. **真错改正（4 条引用 / 3 张卡）**：
   - `EC56`：`cardImageReference` → `CSM_EmperorsChildren_warlord_Lord Exultant`（`c6b`）；`attackAudio` → `VO_CSM_EC_Lord Kaphrael_attack`（`5`）；`deathAudio` 不变。
   - `EC60`：`cardImageReference` → `CSM_EmperorsChildren_strat_Tools of Torture`（`f0a`）。
   - `4eec3b13… Skorflense`：`cardImageReference` → `CSM_EmperorsChildren_strat_Elixir_Skorflense`（`a06`）。
   - `EC62` **不在池内 `byCard`**（池外卡）⇒ 无 `card_vfx` 行；其 map 行已改正（见 §2·4）。
2. **关键词次序的连带（10 张卡，仅顺序）**：`EC28` `EC57` `GOF26` `GOF4` `GSC32` `SAU28` `SOR16` `TAU65` `TL68` `UM80` —— 上一代理 `W58-CARDS` 重排了 17 张卡的 `keywords` 次序，本次重生成把该次序带进 `inferred-keyword` 的 `trigger` 列表。逐张核对：**列表元素集合完全相同，只有顺序变化**（`sorted(json.dumps)` 后逐张相等），不是新增/丢失事件。

**运行时份**：`EC56` 的 `audio[0].n` 由 `VO_…_gen7` 改为 `VO_…_attack`（真实修正）；其余为上述 trigger 顺序 + `generatedAt`。

### 2·6 未触碰

- ⛔ 未重钉任何 `PinnedParseMd5` / `PinnedPlayMd5` / 基线指纹（按任务红线）。
- ⛔ 未跑 Unity、`*.Run`、player、截图。
- ⛔ 未改 `.cs`、正本、任务线总表。
- `gen_cards_engine.py --check` 在 map 重生成后**再跑一次**：`cards_engine.json 一致 ✅`（这 5 条改动都不是 `CardAnim`，不影响池产物）。

---

## 三、幂等与确定性

| 产物 | 连跑次数 | 结果 |
|---|---|---|
| `guid_map.tsv` | 2 | 逐字节一致 `eae8b7f9…` |
| `guid_map_bundles.tsv` | 2 | 逐字节一致 `c0621b94…` |
| `cards_engine.json` | 2（`--write`） | 逐字节一致 `a56aa0ba…`；`--check` 一致 ✅ |
| `card_vfx_by_card.json`（数据份） | 2（固定 `--generated-at`） | 逐字节一致 `b1ab2e16…` |
| `card_vfx_by_card.json`（运行时份） | 2（同上） | 逐字节一致 `e8bb2a19…` |

> 不带 `--generated-at` 时，`card_vfx` 两次只差 `generatedAt` 一行（生成器默认行为未改）。

---

## 四、没查清 / 需要主对话收口的

1. **`PinnedPlayMd5` 要重钉**：本笔改了 `UM80` / `SAU28` 的 `desc`（解析器输入之一）⇒ 引擎线收口时须重生成 play 基线并重钉；本批按红线未做。
2. **`gen_icon_plan.py` 未重跑**（白名单外）：`Vanguard.` / `Flying.` 两种 token 写法在别的卡上已有条目，理论上句首扫描器会命中；但「这两枚徽记由计划表画」需要重跑后才成立，未验证。
3. **`card_vfx` 的 9 张 trigger 顺序变化**是上一批 `keywords` 重排的下游结果（集合不变）。若主对话希望本次只留「5 条真错」的 diff，这一批顺序变化需要接受或另批处理（本笔按「重生成即反映当前卡池」的原则未回滚）。
4. **`guid_map.tsv` 多出的 16 行**（2 个 GUID）是二进制目录覆盖面比旧 JSON 大带来的**新增覆盖**，不是错误；未逐条在运行时验证（本批不跑 Unity）。
5. **EC62 只有 map 级修正**：它不在我们卡池，`card_vfx_byCard` 里没有它；如果将来入池，`_assetRefs` 会直接取到已改正的 `b0f` 行。
6. 生成器仍有一条**与本笔无关**的既有告警：`VO_UM_Firestrike Servoturret - Battlefields are filled with the remains of the obstinate` 不在 `Resources/Art/audio/vo/`（源包侧缺，非本笔引入）。
