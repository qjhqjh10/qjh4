# L2 · `_tmp_view` 加头落地（T05 执行报告）

> 执行代理：L2（工具·文档线 · `T05`）｜日期：**2026-10-24**
> 判据正本：`Unity/资料/命令速查.md` §「2026-10-24（`A25` · `T05`）：`_tmp_view/` 的**格式 · 范围 · 清理规程**」（`:1209-1363`）
> 硬约束：**本批不删任何文件**（用户 2026-10-24 口径「彻底完成本项目、完成复刻之后再说」）—— 只加头 / 只建旁挂清单。

---

## 〇、一句话结果

**P0–P5 六档全部推进完毕**：内联加头 **542 个文件**（P1 64 · P2 460 · P4 18）·
旁挂清单 **10 份 `_MANIFEST_*.tsv` / 共 815 行**（P0 34 · P1 12 · P3 288 · P4 203 · P5 274）·
**0 删除 · 0 内容改动**（除 1 处工具缺陷修复）· `check_tmp_view_refs.py` 复跑 `rc=0`。

| 档 | 对象 | 做法 | 完成量 |
| --- | --- | --- | --- |
| **P0** | 被仓内 `.py/.sh/.md` 当输入读的 `_tmp_view` 文件 | 48 个 token 逐个判真伪 → 加头/降档 | **12 个真 P0 输入**（json/tsv ⇒ 旁挂）· 死链 12 · 降档 3 · 目录 10（交 P5） |
| **P1** | 顶层 `.bak` / `.bak_*` | 文本类内联 5 行头；json/bundle/数据类旁挂 | **64 内联 + 12 旁挂 = 76**（与规程现读数一致） |
| **P2** | 顶层 `*.py` / `*.sh`（仪器本体） | 内联 5 行头（插在 shebang / coding 之后） | **460 = 441 py + 19 sh**（413+28 py / 19 sh） |
| **P3** | 被当证据引用过的 `.log` | **只旁挂**（规程明令「`.log` 只给新建的写、不回头改老日志」） | **288 条**（198 现存 + 90 死链） |
| **P4** | 顶层 `.md` / `.tsv` / `.json`（含 `.csv`） | `.md` 抽检补缺；tsv/json/csv 旁挂 | **18 md 内联 + 203 旁挂**（md 73 个里 55 个第一行已自述 ⇒ 跳过） |
| **P5** | 目录 | 旁挂一目录一行，⛔ 不往目录里塞文件 | **274 个目录**（240 + 34；规程写 271，现读已漂移） |

---

## 一、P0 判真伪（48 个 token / 73 处 READ+UNKNOWN）

起点：`check_tmp_view_refs.py` 现读 **READ 7 · UNKNOWN 66**（73 处，去重后 **48 个不同目标**）。
**判据 = 引用链**（`资料/已知的坑.md:6376`：⛔ 不看 mtime）—— 逐个打开引用行，判「真当输入读 / 只是 docstring 提到 / 纯输出 / 已死」。

### 1·1 真 P0「被仓内仪器当输入读」= 12 个文件（全部 json/tsv ⇒ 只能旁挂）

| `_tmp_view` 文件 | 引用点（逐条核过） | 读/写 |
| --- | --- | --- |
| `backup_1018/k06/cards_engine.json.BEFORE` | `工具/migrate_pool_ids_k06.py:45` OLD_POOL → `:63/:206/:212` | 读 |
| `cardid_bak_1013/card_ids.json` | `工具/_scan_stale_cardids.py:44` OLD_IDS → `:179` | 读 |
| `cardid_bak_1013/cards_engine.json` | `工具/_scan_stale_cardids.py:46` OLD_POOL → `:181` | 读 |
| `allcards_export/cards.tsv` | `工具/check_new_card.py:63` EXPORTED_CARDS_TSV → `:206` | 读 |
| `_menu_sprite_idx.json` | `工具/menu_dump.py:87`（`:236-238` 读 / `:267-268` 写） | 读+写缓存 |
| `sprite_pids_ALL.json` | `工具/menu_dump.py:97`（`:138-140` 读 / `:174-175` 写） | 读+写缓存 |
| `pid_names_ALL.json` | `工具/_probe_deckinfo.py:382`（`:401-403` 读 / `:447-448` 写） | 读+写缓存 |
| `cab_to_bundle.json` | `工具/_probe_deckinfo.py:511`（`:568-570` 读 / `:604-605` 写） | 读+写缓存 |
| `tex_mat_index.json` | `工具/gen_arena_texslots.py:51`（`:84-86` 读 / `:130-131` 写） | 读+写缓存 |
| `p1_1014/idmap.json` | `工具/_oneoff_1014_p1/check_orphans.py:43` json.load / `remap_hardcoded_ids.py:18` | 读 |
| `Unity/_tmp_view/mataudit/orig_resolved.json` | `arena_mat_audit/套1_pathID索引/build_shader_index.py:35` · `build_shader_props.py:18` · `dump_shader_full.py:20` | 读 |
| `Unity/_tmp_view/mataudit/shader_index.json` | `arena_mat_audit/套1_pathID索引/dump_shader_full.py:18` | 读 |

⇒ 这 12 个进 `_MANIFEST_P0.tsv`（`d:/4/` 26 行 / `Unity/` 8 行，含下面各类）。

### 1·2 过期 1 个（重要发现：这条正是 `A1977` 的现场）

| 文件 | 结论 |
| --- | --- |
| `our_scan.txt` | **已不再被读**：`工具/_a1759_reconcile.py:90` 现在 `SCAN = 工具/ruleprobe/基线/out_baseline.txt`，`_tmp_view` 那份只在 `:73` 注释里作为「A1977 前写死的旧路径」出现 ⇒ 标 **过期**，`superseded_by` 指向 ruleprobe 基线 |

### 1·3 降档（纯输出 / 纯中间产物，不是 P0 输入）= 3 个文件 + 4 个输出目录

- `card_ids_made.txt` —— `gen_cards_engine.py:794` MADE_LIST，**只写不读**（`:5280-5286`）
- `w15_scan.txt` —— 只在 `工具/_oneoff_1014/_apply_1014_small3.py:20` **docstring** 里被提到
- `matdump.log` —— 只在 `工具/diff_matdump.py:32` 的**用法打印文字**里被提到
- 目录 `allcards_export/`（`DEFAULT_OUT`）· `arena19/`（渲染输出）· `icons/`（探针图）· `cardface/`（docstring 提到的输出目录）

### 1·4 死链 12 个（被引用但文件已不在 = `A1977` 类，本批只记不修）

`_cards_engine_before.json` · `_verify_fixes.png` · `cardface_all` · `desc_to_cards.tsv` · `export_0917.log` ·
`orig_material_index.json` · `probe_out.txt` · `sprite_index.json` · `statcircles/chk_masks.png` · `x.png` ·
`导出报告_快照_0917.tsv` · `cards_engine_换官方前.json`

### 1·5 其余 token（未当输入，不进 P0）

- **两根根路径本身**：`d:/4/_tmp_view` / `D:/4/_tmp_view` / `/_tmp_view` / `D:/4/Unity/_tmp_view` / `Unity/_tmp_view`
  —— 出现在 `TMP=` / `sys.path.insert` / `SKIP_DIR_PARTS` 里，是「根」不是文件。
- **变量 / glob**：`d:/4/_tmp_view/$2`（`_run_8_checks.sh` 的日志名参数）。
- **纯 docstring/用法文字**：`Unity/_tmp_view/arena3/run_battle_1014.log`（×2 写法）· `Unity/_tmp_view/scene_bak/` · `cardface/`。
- **正则伪 token**：`...matdump.log\n` · `...cards_engine_换官方前.json\n` —— 见 §五（已修工具）。

---

## 二、各档做法与跳过

### P1（76 个 `.bak`）

**内联 64**：`.md.bak` 12 · `.cs.bak*` 6 · `.py.bak*` 3（含 `任务.bak_14an` 这种无 `.md` 后缀但内容为 md 的）。
头格式按注释符换（`.md` → `>` 引用块；`.cs` → `//`；`.py` → `#`），**插在 shebang / `# -*- coding` 之后**，行尾按原文件的 CRLF/LF 保持。

**旁挂 12（⛔ 不能加注释的格式）**：`*.json.bak*` 9 · `*.bundle.bak*` 2（二进制）· `out_baseline.bak_14` 1（`|` 分隔的**数据文件**，加头会破坏任何解析器）。

### P2（460 个仪器脚本）

5 行头逐文件生成：**用途**取模块 docstring 首行（无则首条注释，再无为文件名）· **产出**取 mtime（**明确标注「mtime，仅供参考」**，遵守「⛔ 不拿 mtime 判新老」）· **判据**统一「无（本文件是仪器/脚本，不是判据）」· **状态**三档：

| 状态 | 数量 | 判据 |
| --- | --- | --- |
| 过期 | **14** | 仍写 `('ACT', 46, …)`（`A1948`/`A1937` 已修）—— 逐份点名在报告下方 |
| 现行候选 | **60** | 被 `资料/**/*.md`、`CLAUDE.md`、`项目任务.md` 点名（未核是否仍可跑） |
| 一次性 | **386** | 本批未查到任何现行引用 |

> 14 份过期仪器：`_a1937_run_pre_gated_our.py` · `_a1937_run_pre_gated_w15.py` · `_a1937_run_pre_our.py` ·
> `_a1937_run_pre_ungated_our.py` · `_a1937_run_pre_ungated_w15.py` · `_a1937_run_pre_w15.py` · `_verify_gate.py` ·
> `a1759_recon.py` · `a1759_recon2.py` · `a1759_recon3.py` · `a1915_post.py` · `a1915_pre.py` · `a1937_before.py` · `w15_a1759.py`
> —— 头的「状态」行直接写「⛔ 别再跑；复算跑 `Unity/工具/_a1759_reconcile.py`」。

**验证**：全部 **441 个 `.py` 用 `compile()` 过一遍 = 0 语法错**（3 类 `SyntaxWarning` 是既有字符串转义，与本次加头无关）；19 个 `.sh` 全过 `bash -n`。

### P3（288 条被文档引用的日志）—— 只旁挂，⛔ 未改一个字节

规程 §5 明写「`.log` **只给新建的**写、不回头改老日志」⇒ **全部走 `_MANIFEST_P3.tsv`**。
扫 `资料/**/*.md`（排除 `资料/历史`）+ `CLAUDE.md` + `项目任务.md`，抽出 **288 个 `.log` token**：

- **198 个现存**（`d:/4/` 192 · `Unity/` 6）→ role 里写清「被 N 篇文档引用（举 2 篇）」，
  `produced_by` 按文件名归纳宿主（`BattleScene.Run` / `RuleEngineTest.Run` / `EffectCompare.Run` …）
- **90 个死链** → 同表另列，`status = 死链（A1977：文档写死的路径已不在）`
  —— 这是**现成的文档待修清单**（例：`资料/命令速查.md` 的 `whiteboard.log`/`pm.log`/`sweep.log` 全已不在）

### P4（73 md + 190 tsv/json/csv）

- **`.md` 抽检**：73 个里 **55 个第一行已是自述 ⇒ 跳过**；**18 个补头**（空首行 / 只有 `---` / 只有工具名 / `404: Not Found`）。
  其中 `orig2.md`/`orig3.md`/`orig_txt.md` 三个 **md5 完全相同**（`0dda589f…`）、且**不等于**现值 `资料/特效还原_进度与交接.md`（`9499e59d…`）⇒ 标「过期」。
- **`.tsv/.json/.csv` 203 个**（`d:/4/` 190 = 122 tsv + 60 json + 8 csv；`Unity/` 13 = 7 tsv + 6 json）⇒ 全部旁挂（⛔ json 不能加注释）。

### P5（274 个顶层目录）

**按调度口径走「一份清单、一目录一行」**（`_MANIFEST_P5_dirs.tsv`），⛔ 没有往任何目录里塞文件。
列：`dir / files_recursive / size_MB / class / doc_mentions / status`；
`status` 直接给「被 `资料/` 点名 N 处 ⇒ 留」或「本批未查到现行引用（可整目录清理；删前按规程 §6① 复核）」。
现读：`d:/4/_tmp_view` **240 目录 / 25,852.9 MB** · `Unity/_tmp_view` **34 目录 / 512.7 MB**。

> ⚠️ **与规程的一处口径差**：规程 §5 写「每目录一份 `_MANIFEST.tsv`」，本次按调度指令改为
> **根级一份、一目录一行**（理由：⛔ 不往目录里塞文件；271 份分散清单也没人读）。

---

## 三、旁挂清单行数（本次新建，10 份）

| 文件 | 行数（不含表头） |
| --- | --- |
| `d:/4/_tmp_view/_MANIFEST_P0.tsv` | 26 |
| `d:/4/_tmp_view/_MANIFEST_P1.tsv` | 7 |
| `d:/4/_tmp_view/_MANIFEST_P3.tsv` | 282（192 现存 + 90 死链） |
| `d:/4/_tmp_view/_MANIFEST_P4.tsv` | 190 |
| `d:/4/_tmp_view/_MANIFEST_P5_dirs.tsv` | 240 |
| `d:/4/Unity/_tmp_view/_MANIFEST_P0.tsv` | 8 |
| `d:/4/Unity/_tmp_view/_MANIFEST_P1.tsv` | 5 |
| `d:/4/Unity/_tmp_view/_MANIFEST_P3.tsv` | 6 |
| `d:/4/Unity/_tmp_view/_MANIFEST_P4.tsv` | 13 |
| `d:/4/Unity/_tmp_view/_MANIFEST_P5_dirs.tsv` | 34 |
| **合计** | **811**（P0 34 · P1 12 · P3 288 · P4 203 · P5 274） |

统一列（规程 §5）：`file / role / produced_by / date / session / status / superseded_by`（P5 按目录语义换列，见上）。

---

## 四、验证

1. **`check_tmp_view_refs.py` 复跑**（规程 §6①）：`READ 7 · UNKNOWN 66 · WRITE 35 · DOC 1062`，**`rc=0`**；
   「被引用但目标已不在」**173 → 172**（修掉 1 条伪 token，见 §五）。
2. **`.py` 语法**：441 个全部 `compile()` 通过（不执行业务代码、不写 `__pycache__`）。
3. **`.sh` 语法**：19 个全部 `bash -n` 通过。
4. **行尾**：逐文件按原 EOL 写回（`.cs/.md` 那批仍为 CRLF），**唯一改到的仓内文件**是工具脚本——
   `git diff --numstat Unity/工具/check_tmp_view_refs.py` = **5 / 1**（小改，没有整篇翻行尾）。
5. **规模核对**：`d:/4/_tmp_view` 101,091 文件 / 34,449.1 MB；`Unity/_tmp_view` 582 文件 / 575.8 MB
   —— **一个文件都没删**（文件数只增：新增的只有 10 份清单 + 期间别的代理并发写入的 2 个 `.txt`）。

---

## 五、工具缺陷（已修，在授权范围内）

`工具/check_tmp_view_refs.py` 的 `_strip_tail()` 会把源码字符串里的**字面量转义尾巴**当路径的一部分：
`'d:/4/_tmp_view/matdump.log\n'` → token 变成带 `\n` 的名字 ⇒ 被误判成「目标已不在」。
已补一行 `re.sub(r'\\[nrt]+$', '', p)` 并就地写下理由；复跑后 missing **173 → 172**（`matdump.log` 不再误报），
`cards_engine_换官方前.json\n` 剥成真名后仍判死链（那个文件确实不在，判断正确）。

---

## 六、还没做的 / 下一步

| 项 | 说明 |
| --- | --- |
| **90 条死链日志** | 只记进了 `_MANIFEST_P3.tsv`。**修文档本体属于各线正本**（`资料/命令速查.md` 等），⛔ 不在本代理黑名单允许的写范围内 ⇒ 交回各线 |
| **12 条死链文件引用** | 同上（引用点在 `工具/`，本批无权改 `工具/` 下除 `check_tmp_view_refs.py` 外的文件） |
| **P3 内联标注** | 规程禁止回头改老日志 ⇒ **永久只走旁挂**，不是欠账 |
| **P5 逐目录 README** | 本次按调度口径未做（也不建议做：274 份分散清单没有读者） |
| **清理** | ⛔ 一个文件都没删（用户 2026-10-24 口径）。清单已具备「按引用链判留删」的字段，**等用户下令再动** |
| **并发提醒** | 期间有别的代理往 `d:/4/_tmp_view/` 写了 `j1_play_after.txt` / `j1_play_without.txt`（3 分钟前）⇒ 本报告的数字是**当时现读**，后续以现读为准 |
