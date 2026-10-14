# AN · 真值表（`card_ids.json`）与 `guid_map.tsv` 的 `exact` 列

> 第三十一会话 · 数据侧两笔。⛔ 没跑 Unity 批处理 / 没动 git / 没改正本；行号一律现读。
> 判据源 = 真值快照 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` ·
> 官方本地化 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` · PnP 成品卡图（**开图亲读**）· `工具/_guid_resolve_probe.py`。

---

## ① `card_ids.json` 那两处 —— 结论：**【不改】**（现核推翻了简报的前提）

### 1.1 三方读数（本笔逐条现读）

| 来源（现读） | `GSC64` | `GSC49` | GUID 行 `1a647ea39bbb0154b9673c5194e51a46` |
|---|---|---|---|
| `card_ids.json` 的 `mapping` | `Rusted Vents` | `Telephatic Domination` | `Telephatic Domination` |
| **真值快照**的 `cardName`（该 3 行的 `uniqueId`→`cardName`/`m_Name` 现读） | `Rusted Vents` · `Z-GenestealerCults-Rusted Vents` | `Telephatic Domination` · `T19 GenestealerCults-Telephatic Domination` | `Telephatic Domination` |
| 官方本地化 `Card_Name/GSC64` / `Card_Name/GSC49` | `Rusted Vent`（生锈通风口） | `Telepathic Domination`（心灵支配） | — |
| PnP 成品卡图（**本笔开图亲读** `Warpforge_68_Rusted-Vent.png` / `Warpforge_64_Telepathic-Domination.png`） | 卡面印 **`Rusted Vent`** | 卡面印 **`Telepathic Domination`** | — |

⇒ 🔴 **`card_ids.json` 与真值快照逐字一致**；**错字在真值快照那一侧**（`Rusted Vents` 多一个 s、`Telephatic Domination` 缺一个 h）。
简报里那句「真值快照与 PnP 卡面都是 `Rusted Vent` / `Telepathic Domination`」**不成立** —— 那是**官方/PnP 那一栏**的读数；
`AL_卡名与断行.md:42-43` 自己也标着「🔴 **真值站我们**」「两边都漏 `h`」。

### 1.2 取哪一边：**取真值快照（= 一个字都不改）**，理由三条

1. 本表 `note` 第一段自述语义 =「判据 = 原版卡 SO 的 `uniqueId` → `cardName`」⇒ 它是**照真值的镜像**；
   按 PnP/官方改它，等于让它**不再忠实**（而它的忠实度正是它唯一的价值）。
2. 简报自己给的口径：「**若真值快照本身就是错字，则以真值快照为准并出声**」—— 本条**正是**这种情况。
3. 全表复核（1.3）显示它**没有第三处可改** ⇒ 为这两条破例，只会让表变得**半真值半官方**。

### 1.3 全表复核（本笔新做，不是抽样）

键在快照里的 **1,194 行**中，**值 ≠ 快照 `cardName` 的只有 2 行**：
`#HB_BL34` / `#HB_DA1`（本表 `''`、快照 `null`，**同义**）⇒ **本表对真值快照 1,194/1,194 忠实**。
（表里另 116 行键不在快照中 = 快照之后的新卡/旧残留，`note` 已记。）

### 1.4 简报里另外两条前提，现核**不成立**

- 🔴 **`card_ids.json` 不是 `card_id_truth_index.json` 的输入**。生成器 `工具/gen_card_id_truth_index.py`
  **只读两个源**：`:41 TRUTH`（真值快照）+ `:42 OFFICIAL`（官方本地化），`:59-60` 现读；
  **全文件无 `card_ids` 字样**。⇒ 简报那条「表错 ⇒ 真值索引跟着错 ⇒ 下游 id 判定跟着错」的链路**不存在**。
  （真值索引与 `card_ids.json` 是 `gen_cards_engine.py:1572-1581` 那条级联里的**两条平行路**：①索引 → ②本表 → ③自造。）
- 顺序也要说清：**索引先命中**，本表只在索引查不到时兜底。

### 1.5 改了哪些文件哪些行

| 文件 | 改了什么 |
|---|---|
| `Unity/数据/游戏数据/card_ids.json` | **`mapping` 一行都没动**（1310 行不变；`GSC64`/`GSC49`/GUID 三行的值原样）。只在 **`note`（第 1314 行）末尾追加一段带日期的留痕**（照该文件既有惯例）：记「这两行与该 GUID 的 `cardName` 逐字一致 · 错字在快照侧 · ⛔ 别按 PnP/官方改本表 · 全表复核 1,194/1,194 · 后果：名字→id 退路落空但 id 由索引给出、不变」。字节 `50,625 → 51,948`（+1323）· **CRLF 1315 / LF 1315（纯 CRLF）与改前一致** · JSON 可解析 · 只有 1 处 `bytes.replace`、命中计数 1→1 |
| `Unity/数据/游戏数据/card_id_truth_index.json` | **重跑生成器后逐字节不变**（md5 前后同为 `23d88e7e420f06c6089c30e9a5bb48cb`，180,235 B）⇒ 卡池 id 集合没被这两条牵动 |
| `Unity/工具/gen_card_id_truth_index.py` | **没改**（它根本不读本表，无需改） |
| 卡池 | 那两张的 id 现读仍是 **`GSC64` / `GSC49`**（`MyGame/Assets/RuleEngine/Resources/cards_engine.json`，`name` 已是官方拼写 `Rusted Vent` / `Telepathic Domination`） |

### 1.6 ⚠️ 顺手发现（**只报，⛔ 一条都没改**）

- **(a)** 卡池那两张已改向官方拼写 ⇒ 本表这条「**名字 → id 的退路**」（`gen_cards_engine.load_ids()` `:704-716`
  按 `_norm_name(值)` 建表、`:1572-1581` 回查）**对这两张会落空**。**今天 id 仍稳**：先是真值索引命中
  （`card_id_truth_index.json` 的 `by_official_en['rustedvent']=['GSC64']` · `['telepathicdomination']=['GSC49']`）⇒ id 不变。
  **风险**：索引一旦失效（如官方本地化重生成），这两张会掉到「③自造 id」。
  **要不要把本表这两行同步成我方卡名**，是**口径问题**（本表语义「照真值」 vs `gen_cards_engine.py:264`/`:314`
  那条「改名后已把本表值同步改好，否则 id 掉成自造」的先例）—— **我没动，请主对话裁**。
- **(b)** `card_stats.json` 的 `noise_reason` 里仍写 `Rusted Vents(GSC68)`（`AL` 已记、本笔没动，理由同 `AL` §五·6）。

---

## ② `guid_map.tsv` 加 `internalId` / `exact` 两列

### 2.1 结论

TSV 现为 **11 列**（9 + `internalId` + `exact`）；**前 9 列与改前逐行完全相同**（4,985 行 **0 处**不同）；
**退键 56 行**（= `exact=False`），与 `AI_guid退键.md` §1.2 的读数**逐条吻合**。

### 2.2 判据（已写进脚本注释）

**退键 ⟺ `containerKey` ≠ 该 GUID 在 catalog 里的 `internalId`**（⛔ 不是「键短」）。
`_guid_resolve_probe.py` 现读落点：

| 行 | 内容 |
|---|---|
| `:27-38` | 文件头「🔴 退键的判据」整段（判据 + 反例数字 + 「⛔ 别再退回 9 列」） |
| `:198` | 候选键排序 `key=lambda k: (k != r['iid'], -len(k))` |
| `:210` / `:218` | 两条失败出口的 hit 也带上 `iid=r['iid']` |
| `:238-239` | `got.append(dict(..., iid=r['iid'], ..., exact=(k == r['iid'])))` |
| `:348` | `rows.append((..., h.get('iid'), h.get('exact')))` |
| `:351-356` | 表头 `…\tname\tinternalId\texact`（含「⛔ 别删这两列」的注释） |
| `:362-363` | 跑完打印「退键(exact=False) N 行」（自带现核） |

**自洽验证（本笔现算）**：`(containerKey != internalId) ⟺ (exact == 'False')` —— **不符行 = 0**。
**新的判据读数（用新列现算）**：键长 ≤2 的 219 行 ⇒ **真阳 30 / 假阳 189 / 假阴 26**（逐长度：1 字符 1/17、2 字符 29/172、3 字符 26/36、32 字符 0/4704）。

### 2.3 加列前后

| | 改前 | 改后 |
|---|---|---|
| 数据行 / 总行 | 4,985 / 4,986 | 4,985 / 4,986 |
| **列** | 9 | **11** |
| 字节 | 1,045,461 | 1,226,528 |
| md5 | `09716b6b0e39fd35e38cc7fb67bd663a` | `e4f4fc73c21c5f636f0e9d0353bda4f2` |
| 行尾 | LF（`CRLF=0` / `LF=4,986`） | LF（同） |
| distinct GUID | 2,372 | 2,372（不变） |
| `exact` 取值 | — | `True` 4,929 · **`False` 56** |

⚠️ **路径现核**：简报写的 `Unity/数据/游戏数据/guid_map.tsv` **不存在**；探针 `OUTDIR` = `d:/4/Unity/资料/普查产出_第二十七会话`
（`_guid_resolve_probe.py:32`），**真正在用的那份**就是 `资料/普查产出_第二十七会话/guid_map.tsv`（所有消费者都指它）⇒ 我改的是这一份。

### 2.4 重建已跑过、`exact` 会跟着走

改完脚本后**整表重跑**（改前先留副本）：`python -I -X utf8 工具/_guid_resolve_probe.py map`
⇒ 打印 `wrote d:/4/Unity/资料/普查产出_第二十七会话\guid_map.tsv 4985 rows; 2376 distinct guids; 退键(exact=False) 56 行`。
（`2376` = 查过的 GUID 数（含解不出行的），TSV 里 distinct = 2,372。）
前置条件都在：catalog 存在 · `d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64` 84 个束 · UnityPy 1.25.3。

### 2.5 `guid_map.tsv` 的全部消费者（现扫 `d:/4`）

**真读它的（3 处，全是 `csv.DictReader` ⇒ 表尾加列不影响；无一处按下标取值）**

1. `Unity/工具/gen_card_vfx_by_card.py` —— `:124 (GUID_MAP_TSV)` → `:742 load_guid_map()`（`csv.DictReader`，只按列名取）
   → 用在 `:861` / `:928` → `resolve_so_ref()` `:771-773`（取 `className` / `type` / `name` / `bundle` / `containerKey`）。
   **改后实测（本笔跑了函数本体）**：`load_guid_map()` 返回 **2,372 个 GUID、`err=None`**，每条记录多出 `internalId` / `exact` 两键。
   ⚠️ 该文件 `:129` 的 `CONF_PRIORITY` 里那个 `"exact"` 是**另一个东西**（VFX 命中档位），与这一列**无关**。
2. `Unity/工具/_oneoff_1014_p1/build_picks.py` —— `:22 (GUIDMAP)` + `:79` `csv.DictReader`，只用 `field` / `uniqueId` / `name`。
3. `d:/4/_tmp_view/a1770/` 的一次性 scratch 探针 —— `probe.py:14` · `probe2.py:10` · `probe3.py:6` · `probe5.py:10`，全 `DictReader`（`probe3.py:21` 还打印 `fieldnames`）。

**只在注释/文档里提到它、并不读它（现核）**

| 位置 | 现核结论 |
|---|---|
| `工具/gen_cards_engine.py:1056` | 字符串内的注释；它的 id 源是 `card_ids.json`（`:625 IDS_SRC`） |
| `工具/import_original_art.py:226` · `:236` | **注释**；`AI` 说它不读 TSV，**现核成立**（全文件无该路径常量、无对该文件的 `open`） |
| `工具/import_original_audio.py:235` | 注释；它读的是**产物** `数据/游戏数据/card_vfx_by_card.json` |
| `MyGame/Assets/RuleEngine/Data/CardDatabase.cs:177` | 注释 |
| `工具/sync_from_d2.py:107` | 同步的是**同名另一支**文件（见下） |

**同名但完全不同的两份（⛔ 别混）**：`Unity/数据/索引/guid_map.tsv` 与 `Unity/资料/原版参照图/Unity参照管线_0825/data/guid_map.tsv`
—— 4 列 `GUID / None / ? / bundle`、10,325 行、**纯 CRLF**、两份 md5 相同（`3bd47d3dbcf7478321b5ceb2055f4210`），
由 `sync_from_d2.py:107` 从 `d:/2` 同步。**本笔一个字节没碰。**

### 2.6 顺手发现（只报）

- **(a)** `AI_guid退键.md` §1.2 结尾那句「按『≤2 字符』判 = 多报 **171** 行、漏报 26 行（真阳 30 / 假阳 **171** / 假阴 26）」
  与**它自己那张逐长度表**（1 字符 17 非退键 + 2 字符 172 非退键 = **189**）**不自洽**；
  同一句已被抄进 `AA_A1770.md:177`（**两处都带着 171**）。用新列现算 = **真阳 30 / 假阳 189 / 假阴 26**。
  ⚠️ 这两处**不在本笔白名单** ⇒ 只报不改。
- **(b)** `_guid_resolve_probe.py` 的 docstring 原来写「`map` 产出 guid_map.tsv **+ card_field_map.tsv**」——
  `card_field_map.tsv` **早就不产了**（现读 `:329` 只写 guid_map.tsv）；本笔改 docstring 时**顺手删掉那半句**。

---

## 没查清 / 边界

- **没查**「目录 `internalId` 与束容器键为什么会不一致」的**根因**（`AI` 也没查）—— 本笔只把「判据 = 两者不等」落成列。
- **没跑** `gen_cards_engine.py`（简报明写另一写手在跑）⇒ 1.6(a) 那句「退路会落空」是**读代码推的**
  （`load_ids()` `:704-716` 用 `_norm_name(值)` 建表、`:1572-1581` 回查），**没做端到端**。
- **没碰**：`cardface_fixes.json` / `card_stats.json` / `zh_cards.json` / `import_original_art.py` / `gen_cards_engine.py`
  / `RuleEngine/**` / `CardPresentation/**`；**四份正本 + `资料/普查产出_第三十会话/**` 一个字节没动**；**没动 git**；**没跑任何 Unity 批处理**。

## 验收

- ✅ `TMPDIR=/tmp/wf_an bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 · 编辑器错误数 0**。
- 🔵 `bash d:/4/Unity/工具/ruleprobe.sh check` ⇒ **rc=1**（与简报一致「来源已知」）：唯一失败的是**解析基线指纹**那条哨兵，
  它报的解析差异 = `GSC29|Metamorph Iconbearer`（**别的写手改的卡池/解析器**，与本笔无关 —— 本笔动的是 note 字符串 + 一个 `.py` 探针 + 一份 TSV）。**⛔ 没有重钉指纹。**
- 🔵 `python -I -X utf8 工具/gen_card_id_truth_index.py` ⇒ 重跑成功、产物**逐字节不变**。
- 改动清点：`数据/游戏数据/card_ids.json`（`note` +1 段，第 1314 行） · `工具/_guid_resolve_probe.py`（`37+/8−`） ·
  `资料/普查产出_第二十七会话/guid_map.tsv`（9 → 11 列） · 新增本报告。`card_id_truth_index.json` 与改前逐字节相同（它本身仍是未跟踪文件）。
