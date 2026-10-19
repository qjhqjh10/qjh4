# W · 工具线「生成器 dry 闸」报告（2026-10-19）

> 执行者：写手代理（`general-purpose`）。⛔ 未跑 Unity / 未跑 `*.Run` / 未起 player / 未动 git / 未改任何正本 /
> 未重生成任何数据产物（全部用 `--dry` 自检）。
> 判据入口：`资料/普查产出_1019/W_工具线_T03T04_1019b.md` §四·2（22 个「裸跑就写盘」的清单）·
> `资料/任务线/工具文档线/任务总表.md` T03/T04 行 · `准则与做法.md`。
> 文件所有权：只改 `Unity/工具/gen_*.py` · `Unity/工具/_loc_official/{w4_verify,gen_official_names}.py` ·
> `Unity/资料/资源使用手册.md`（仅 A2063 一处）。⛔ 未碰 `Assets/**` 数据 · `Scenes/*.unity` · `任务线/**` · 正本 · `d:/2` · git。

---

## 一、结论

### 1·1 22 个脚本：**全部加上了 `--dry`**，默认行为**一个都没改**

口径：`DRY = "--dry" in sys.argv[1:]`（`gen_fallback_audit` 例外：它吃 4 个位置参数，先 `[a for a in argv if a != "--dry"]` 再取）。
**不带 `--dry` 时所有代码路径与以前相同**（新分支只有 `if DRY:` / `if not DRY:`，默认打印文案逐字保留）。

| # | 脚本 | 产物 | `--dry` 做什么 |
|---|---|---|---|
| 1 | `gen_animfx_modules.py` | `animfx_modules.json` + `module_materials.tsv` | 算完拍平、打印统计；两份都不写 |
| 2 | `gen_cardbacks.py` | `Resources/Cardbacks.json` | 算完对账（233 项 + 13 默认）、不写 |
| 3 | `gen_csc_rsp.py` | `$TMPDIR/wf_csc*.rsp`（**纯临时文件**） | 扫完 346+9 源 / 336 refs，写口换成空接收器（`_DrySink`） |
| 4 | `gen_environment_conditions.py` | `environment_conditions.json` + `EnvironmentConditions.json` | 两份都只算不写（含 `makedirs`） |
| 5 | `gen_fallback_audit.py` | 第 4 个位置参数那个 tsv | 算完 691 条、打印中位偏差、不写 |
| 6 | `gen_flicker_cs.py` | `FlickerData.gen.cs` | 算完 38 条、不写 |
| 7 | `gen_ghidra_lists.py` | 3 份 `D:/2/tools/*.txt`（**纯清单**） | 算完 294,942 methods / 26,507 strings，写口换成空接收器 |
| 8 | `gen_icon_doc.py` | `资料/卡面图标_对照与缺口.md` | 算完 2456 行、不写 |
| 9 | `gen_mat_renderqueue.py` | `mat_renderqueue.json` | 算完 1092 个材质、不写 |
| 10 | `gen_module_material_sources.py` | `module_material_sources.tsv` | 扫完 94 个包、定位 3 张材质、不写 |
| 11 | `gen_offensive_cards.py` | `offensive_cards.json` | 算完 13 阵营、不写 |
| 12 | `gen_offensive_cards_flat.py` | `Resources/OffensiveCards.json` | 算完 13 阵营 / 39 槽、不写 |
| 13 | `gen_pragati_digits.py` | PNG + JSON + TXT + `PragatiDigits.Data.cs` | 仍**解码 + 组图 + 算 base64**，4 份都不写 |
| 14 | `gen_prebuilt_decks.py` | `prebuilt_decks.json` + `tutorial_decks.json` | 两份都算、不写 |
| 15 | `gen_renderqueue_truth.py` | JSON + 2 份 TSV | 三份都不写 |
| 16 | `gen_shader_blend.py` | `original_shader_blend.tsv` + `OriginalShaderBlendTable.cs` | 两份都不写；**自检失败路径也不删盘上已有的表** |
| 17 | `gen_shake_presets.py` | `shake_presets.json` | 算完 17 个 preset、不写 |
| 18 | `gen_skybox_cubemap.py` | 6 张 PNG + `material_params.json` | 仍**逐面解码 BC6H（验长度）**，7 份都不写 |
| 19 | `gen_sunflare_cs.py` | 12 张贴图拷贝 + `SunFlareData.gen.cs` | 只验源贴图在不在、算 C# 表、不拷贝也不写 |
| 20 | `gen_talent_cards.py` | `talent_cards.json` + `Resources/talent_cards.json` | 走同一个 `write()` 助手（DRY 时只打印字节数） |
| 21 | `gen_trait_dict.py` | TSV + MD | 两份都算、不写（含 `makedirs`） |
| 22 | `gen_tutorial_stages.py` | `Resources/tutorial_stages.json` | ⚠️ **现读跑不到写入点**（见 §四·2），但闸已加 |

**⚠️ 术语澄清**：`gen_csc_rsp` / `gen_ghidra_lists` 是「产物只是文件」型的脚本，但它们**有可计算的内容**
（源清单 / 地址表），`--dry` 仍有意义 ⇒ 没有哪个脚本需要写「本脚本只有写入语义」。
**不适合 `--dry` 的脚本：0 个。**

### 1·2 `gen_cardface_fixes.py --check` 那个红：**盘上那个键是合法的真实修正，不该删**

- **盘上多出来的键**：`cardface_fixes.json` 的 `subtype` 列里 `"Sister Repentia": "Infantry"`。
- **它是真实修正的判据（三处互证）**：
  1. **卡面亲读**：`d:/2/Warpforge部队卡片/Sorotitas/3部队/Warpforge_19_Sister-Repentia.png` —— 卡名 `Sister Repentia`，橙字 = **`Infantry`**（本代理本次开图亲读）。
  2. **逐张提取源表**：`Unity/资料/卡表核对_卡图提取/_合并总表.md:723` = `| Warpforge_19_Sister-Repentia.png | Sister Repentia | 3 | 3 | 0 | — | 4 | Infantry | 部队 | common | …`。
  3. **源表是错的、修正已生效**：`数据/游戏数据/card_stats.json` 里这张卡 `subtype = "Unit"`（**类型词**，正是这个生成器要修的那个 bug 形状）；`Resources/cards_engine.json` 的 `SOR10 / Sister Repentia` 现读 `subtype = "Infantry"`。
- **根因（不在数据、在脚本里）**：`工具/gen_cardface_fixes.py:81-85` 的 `NAME_ALIAS` 还有一条
  `"Sisters Repentia": "Sister Repentia"` —— 这是「我们旧名是复数、卡面是单数」时代的桥。
  `:173-174` 把它反向成 `ALIAS_REV["Sister Repentia"] = "Sisters Repentia"`；`:190` 拿卡面行名
  `Sister Repentia` 去反查成**旧名**；`:191-193` 在 `card_stats.json` 里查不到旧名（源表**已经改名成单数**）⇒ `continue` 跳过整张卡
  ⇒ 算出来的表里没有这一条 ⇒ `--check` 报「盘上多 1 条」。
- **一行修法（已验证，但本批【未改】，只报告）**：删掉 `NAME_ALIAS` 里 `"Sisters Repentia"` 那一条。
  模拟验证：把这条别名去掉后 `--check` = **差 0 处（一致 ✅）**、`字段修正 41 张`、`rc=0`。
  另两条别名**仍然需要**（`card_stats.json` 里 `Lord Kakophonist` / `Threnodic Choir Noise Marine` 仍是旧名）。
- 🔴 **风险（不修会咬人）**：下一次有人照流程跑 `gen_cardface_fixes.py`（不带 `--check`）会把这条**合法修正静默删掉**
  ⇒ `SOR10` 的 `subtype` 退回 `Unit`（类型词），并撞上「不许有单位卡的 subtype 是类型词」那条判据。
  **删数据是主对话的活**，本代理未动盘上产物、也未改脚本的这条别名。

### 1·3 `A2066` `w4_verify.py`：**仪器侧修完，34 过 / 2 红；2 红全是数据问题**

任务书写「32 过 / 4 红」，本代理上手现读是 **31 过 / 5 红**（`Core/Loc.cs` 本会话被别的代理改过，条目已长到 775）。
逐条判：

| 红 | 判 | 处置 |
|---|---|---|
| ① `new Entry(` = 777 ≠ 720 | **仪器假设过期**（720 是 2026-10-14 一轮的读数） | ✅ 改成只数**代码行**（排除 `//` 注释）；条目数改成下限 775。**现读 775（正则 774 + 拼接写法 1）** |
| ① `正则可见 774 = 表 777 − 1` | **仪器 bug**：文件头说明里自己写了两次 `new Entry(`（一句还是「复算：`grep -c 'new Entry(' …」）⇒ 775 条被数成 777 | ✅ 随上一条一起修；现在 774 = 775 − 1 通过 |
| ④ 官方 Battle 键缺 3 | **数据真缺**（详见 §3·2） | ⛔ 只报告 |
| ④ Battle EN 不同 2 | **数据不一致**（官方表换了词，详见 §3·2） | ⛔ 只报告 |
| ⑥ 命中数 459/234/102/102/95 ≠ 368/234/95/95/74 | **仪器假设过期**（数据只增不减，旧期望值会把「变好」报成红） | ✅ 改成**下限**（459/234/102/102/95），现读只打印 |

另修：④ 末尾那句 `官方 Battle 键 156 条：全部在表、EN 全部与官方一致` 原来是**无条件打印**——
上面两条已红它还喊全绿（假读数）。改成按现读算：`在表 153 条、EN 一致 151 条`。

### 1·4 `A1854` / `A2063`：都修了（判据够）

- **`A1854` `gen_official_names.py`**：`PrebuiltDecks/*` 是**按英文名的生成期 join**（我们 `deckId` 与官方键不同源）。
  ✅ 文件头写明「上游一变必须重跑」；✅ `resolve_prebuilt_decks()` 加了一条**只报不拦**的 mtime 检查：
  `prebuilt_decks.json` 比产物新 ⇒ 打红字「join 已过期，必须重跑」。顺带把 docstring 里过期的
  `74 副 / 10 例歧义` 改成 **现读 95 副 / 73 例**（官方 `PrebuiltDecks/*` 现读 215 条）。
- **`A2063` `资料/资源使用手册.md`**：那条判据现在在 **`:269`**（任务书写 `:249` 已漂走；判据 = grep `gems_rarity.json`）。
  ✅ 原来写「稀有度 → `数据/游戏数据/gems_rarity.json`」—— 实际 `工具/gen_cards_engine.py` 的 `RARITY_SRC` 读的是
  `资料/卡牌数据表/卡牌宝石稀有度_0824.md`。已就地订正并注明「`gems_rarity.json` 是 Godot 时代快照、**不是判据**」。
  ⚠️ `gems_rarity.json` **留还是删**任务书自陈待裁 ⇒ 未动文件本身。

### 1·5 验证（本批纯工具 ⇒ 零条 Unity 自检）

| 验证 | 结果 |
|---|---|
| `python -I -m py_compile` × **24**（22 个生成器 + `w4_verify` + `gen_official_names`） | ✅ 全过 |
| `--dry` 实跑 | ✅ **22/22 都跑过**：21 个 `exit 0`；`gen_tutorial_stages` `exit 1`（**先于本批的** assert，见 §四·2，写入点没到） |
| **盘上产物 mtime / size / sha256 前后不变** | ✅ 逐项比过（详见下表；含 20 个文件 + `gen_csc_rsp` 的 `$TMPDIR`：干跑后 **0 个文件**） |
| `gen_cardface_fixes.py --check` | ✅ 仍与 §1·2 相同（本代理**未改**该脚本/产物） |
| `w4_verify.py` | ✅ **34 过 / 2 红**（3 条仪器红修掉；剩 2 条数据红） |
| 行尾 | ✅ 未翻：`资源使用手册.md` **CRLF 582/582**；各 `.py` `git diff --numstat` 最大 **31/20**（无整篇重写） |

**mtime+sha 前后一致的样本（`前 == 后` 已逐项核对）**：
`mat_renderqueue.json`（19446 B / 5332a4de…）· `shake_presets.json`（5435 / c4339858…）· `OffensiveCards.json`（15589 / 19e01bbc…）·
`trait词典_原版真值.tsv` + `A1748_原版trait真值.md` · `pragati_digits.*`（三份原本就不存在，干跑后仍**不存在**）·
`PragatiDigits.Data.cs`（45934 / 2ed07062…）· `animfx_modules.json`（2637874 / bb67bd26…）· `module_materials.tsv` ·
`卡面图标_对照与缺口.md` · `environment_conditions.json` + `EnvironmentConditions.json` · `offensive_cards.json` ·
`renderqueue_truth.json` + 2 TSV · `Cardbacks.json` · `FlickerData.gen.cs` · `SunFlareData.gen.cs` ·
`original_shader_blend.tsv` + `OriginalShaderBlendTable.cs` · `skybox/{pZ,mX,material_params}` · `prebuilt_decks.json` +
`tutorial_decks.json` · `D:/2/tools/{all_methods,all_strings,wanted_methods}.txt` · `talent_cards.json` ×2 ·
`module_material_sources.tsv`。

---

## 二、改动清单（文件:行号）

### 2·1 22 个生成器：`DRY` 常量 + 用法说明 + 写口闸

| 文件 | 用法说明 | `DRY` | 写口闸（行） |
|---|---|---|---|
| `工具/gen_animfx_modules.py` | `:28` | `:38` | `:254`（JSON）/ `:274`（TSV）/ `:318`（产物大小打印） |
| `工具/gen_cardbacks.py` | `:32` | `:41` | `:194` |
| `工具/gen_csc_rsp.py` | `:1-2` | `:6` | `_DrySink` `:10-22`；`with _out(...)` `:41` / `:80`；干跑打印 `:111` |
| `工具/gen_environment_conditions.py` | `:34` | `:39` | `:211`（CANON）/ `:241`（FLAT，含 makedirs）/ 末两行 `→` 加「干跑：未写」 |
| `工具/gen_fallback_audit.py` | `:11` | `:22`（`_DRY`/`_ARGS`） | `:35`（argv 过滤）/ `:83` |
| `工具/gen_flicker_cs.py` | `:36-37` | `:42` | `:150` |
| `工具/gen_ghidra_lists.py` | `:2` | `:6` | `_DrySink` `:10-22`；3 个 `with _out(...)` `:30/:34/:51`；干跑打印 `:56` |
| `工具/gen_icon_doc.py` | `:8` | `:17` | `:286` |
| `工具/gen_mat_renderqueue.py` | `:16` | `:26` | `:63` / `:74` |
| `工具/gen_module_material_sources.py` | `:22` | `:33` | `:140`（提前 return） |
| `工具/gen_offensive_cards.py` | `:8` | `:62` | `:694` |
| `工具/gen_offensive_cards_flat.py` | `:12` | `:17` | `:47` |
| `工具/gen_pragati_digits.py` | `:24` | `:34` | `:186`（PNG/JSON/TXT）/ `:229`（C#）/ 尾部 3 行打印标注「干跑：未写」 |
| `工具/gen_prebuilt_decks.py` | `:6` | `:58` | `:714` / `:722` / `:729`（`wrote` 打印） |
| `工具/gen_renderqueue_truth.py` | `:22` | `:32` | `:109`（3 份一起）/ `:126`（`已落表` 打印） |
| `工具/gen_shader_blend.py` | `:12-14` | `:31` | `:210`（TSV）/ `:362`（C# 表）/ **`:387`/`:406` 自检失败不再删盘** |
| `工具/gen_shake_presets.py` | `:24` | `:34` | `:77` |
| `工具/gen_skybox_cubemap.py` | `:26` | `:34` | `:86`（makedirs）/ `:94`（逐面 save）/ `:102`（params） |
| `工具/gen_sunflare_cs.py` | `:33` | `:42` | `:216`（makedirs）/ `:223`（copyfile）/ `:330`（C#） |
| `工具/gen_talent_cards.py` | `:43` | `:48` | `write()` 助手 `:80`（DRY 时只打印）/ `:191`（makedirs） |
| `工具/gen_trait_dict.py` | `:16` | `:27` | `:157`（TSV，含 makedirs）/ `:347`（MD）/ 两行 `TSV ->`/`MD ->` 标「干跑：未写」 |
| `工具/gen_tutorial_stages.py` | `:6` | `:54` | `:755` |

### 2·2 其它改动

| 文件:行 | 原 → 新 | 理由 |
|---|---|---|
| `工具/_loc_official/w4_verify.py:30-43` | `n_entry = s.count("new Entry(")`、`期望 720` → **按代码行数**（排除 `//` 注释）+ **下限 `ENTRY_FLOOR = 775`** | `A2066`：注释里两次 `new Entry(` 把 775 数成 777；720 是过期读数 |
| `工具/_loc_official/w4_verify.py:60-76` | 标题「105 条」→「官方现读 156 条」；末句**无条件**「全部在表、EN 全部一致」→ 按现读算「在表 153、EN 一致 154」 | 同一处假读数；标题数字过期 |
| `工具/_loc_official/w4_verify.py:108-113` | 写死 `368/234/95/95/74` → **下限** `459/234/102/102/95`，现读照旧打印 | `A2066`：旧期望把「数据变好」报成红 |
| `工具/_loc_official/gen_official_names.py:30-33` | 用法段无 A1854 说明 → 加「`PrebuiltDecks` 是生成期 join，上游一变必须重跑」 | `A1854` |
| `工具/_loc_official/gen_official_names.py:142` | `现读 74 副 / 歧义 10 例 / 官方 97 条` → `现读 95 副 / 73 例 / 官方 215 条（2026-10-19）` | T04 过期读数（现读实跑） |
| `工具/_loc_official/gen_official_names.py:155-159` | （无）→ `prebuilt_decks.json` 比产物新 ⇒ 打印「join 已过期，必须重跑」 | `A1854` 的可执行守卫（只报不拦） |
| `资料/资源使用手册.md:269` | `稀有度 → 数据/游戏数据/gems_rarity.json（1118/1118 映射）` → `资料/卡牌数据表/卡牌宝石稀有度_0824.md` + 「`gems_rarity.json` 是 Godot 快照、不是判据」 | `A2063`（判据 = `gen_cards_engine.py` 的 `RARITY_SRC`） |

---

## 三、没查清 / 未做

### 3·1 `w4_verify` 剩下的 2 条红（**数据问题，只报告**）

| 条目 | 现读 | 说明 |
|---|---|---|
| `Battle/Tips/DamageFatigueEnemy` **缺** | 官方 EN `Enemy deck is empty. Your enemy takes {0} damage from fatigue.` / ZH `敌人的卡组已空。你的敌人因疲劳受到{0}点伤害。` | 🔴 **代码真的在用**：`Assets/CardPresentation/Battle/BattleDriver.cs:1145` `const string DamageFatigueEnemyTerm = "Battle/Tips/DamageFatigueEnemy";`（`:18741` 取文本）⇒ **真缺一条玩家可见文案** |
| `Battle/MatchType/FastMode` / `FastModeRanked` **缺** | 官方 `Unranked Skirmish` / `未排名的小冲突`；`Skirmish` / `小规模战斗` | 全仓 `.cs` **找不到这两个键的消费点**（`RuleEngine/Core/BattleContext.cs:1538/1559` 出现的是注释与枚举值 `FastModeRanked = 211`，**不是本地化键**）⇒ 是「官方有、我们没接」还是「我们不用这两个键」**未判死**，未改检查 |
| `Battle/MatchType/Ranked` EN 不一致 | 我们 `Ranked`；官方 `Classic`（ZH 我们「排名」/ 官方「经典」） | 官方表在 2026-10-18 并入的**当前 build**里换了词；这格要不要跟着换是**数据决策**，未改 |
| `Battle/MatchType/Unranked` EN 不一致 | 我们 `Unranked`；官方 `Unranked Classic` | 同上 |

### 3·2 其它没查清

1. **`gen_tutorial_stages.py` 现在根本跑不完**（见 §四·2）：根因是 `OCR_NAME_FIX` 那条与
   `card_stats.json` 哪边过期**没查**——只确认「assert 在写入之前」，所以产物没被动过。
2. `w4_verify` ⑥ 的覆盖率缺口未查：`Titles 459/462`（3 条没有官方名）· `Avatars 234/469`（235 条没有）·
   `预组 95/103`（8 副没有）—— 是「官方表本来就不覆盖」还是「我们漏了」**没查**。
   现仪器只把它们当**下限**，不会因为缺口报红。
3. `gems_rarity.json` 留/删：任务书自陈**待裁**，本批只改手册里的指针、没动文件。

---

## 四、顺手发现（只报，未越界改）

1. **`w4_verify` ④ 的末句原来是假读数**：`官方 Battle 键 156 条：全部在表、EN 全部与官方一致`
   **无条件打印**——上面两条 `chk` 已经红了它仍喊全绿。已改成按现读算（本批修）。
2. **🔴 `gen_tutorial_stages.py --dry` 现读 `exit 1`（先于本批就存在）**：
   ```
   AssertionError: OCR_NAME_FIX[Gauss Warrior (tutorial)]=SAU4（Gauss Warrior）
                   在 card_stats.json 里没有 `ocrName` ⇒ 判据不成立
   ```
   位置 = `工具/gen_tutorial_stages.py` 的 `verify_ocr_fix()`（assert 在写盘**之前**）。
   ⇒ **当前源表与脚本自带的 `OCR_NAME_FIX` 已对不上**，`Resources/tutorial_stages.json`（现读 652,073 B）
   是**旧产物**、现在无法重生成。哪一边过期没查（属数据线）。快照：干跑前后产物 `size/sha` 未变。
3. `gen_official_names.py` 的 docstring 原来写「74 副 / 10 例」是过期读数（现读 95 / 73）——已改（§2·2）。
4. `gen_trait_dict.py` 的两份产物仍写到 **`资料/普查产出_第二十七会话/`**（旧目录，不是本次产出目录）——
   这是它本来的行为，未改；调度台若要重跑它，注意会覆盖那份旧报告。
5. `gen_shader_blend.py` 的失败路径原来会 `os.remove(OUT/OUT_CS)`。**干跑时若不拦，一次失败的 dry-run
   会把盘上那两张好表删掉** —— 已把删除也放进 `not DRY` 分支（这是本批唯一一处「不加会咬人」的设计点）。
6. `gen_csc_rsp.py` 用 `TMPDIR`（`typecheck.sh` 的并发隔离就靠它）。实测 `TMPDIR=/tmp/wf_dry_a … --dry`
   跑完该目录 **0 个文件**，说明空接收器真的没建文件。
7. `gen_skybox_cubemap.py --dry` 仍逐面解码 BC6H（6 × 262,144 字节，长度校验通过）——**解码链本身**
   在干跑里也被验了一遍，这是有意的。
8. `gen_prebuilt_decks.py --dry` 现读 `practice=103 complete=103 · tutorial 12 副 complete=12`、
   `cross-faction id hits = 0` —— 顺手确认了当前预组数据是自洽的。
