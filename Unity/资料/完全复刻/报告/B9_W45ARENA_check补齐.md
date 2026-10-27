# B9_W45ARENA —— 生成器 `--check` 的 B 型补齐（arena 十件 + mips + module_events）

> 执行代理：B9_W45ARENA（2026-10-27）· 承接 `B9_W35CHECK`（`报告/B9_W35CHECK_生成器check体检.md` §一·4 / §三）
> 判据表：`判据/生成器check覆盖表.tsv`（本波已就地更新 12 行）
> 口径（继承上一路）：**只改只读的 `--check` 分支，写盘路径一个字节没动**；⛔ 没跑 Unity、没动 git、没碰 `Assets/**`。

## 结论

1. **B 型（「看着有 check、其实啥都没查」）里最大的一族 —— `gen_arena_*` 十个 —— 全部补齐**。
   它们的 `--check` 原来是「只扫不写」（`if check: continue` / `if not check: 才写`），**盘上产物缺失也恒 rc0**。
   现在：**缺产物 → 🔴 逐个点名 + rc=1**；**内容与重算结果不同 → 🔴 标记过期 + rc=1**；
   **原版这一场本来没有某类对象、盘上却留着旧旁挂 → 🔴 标「产物多余」**（`ArenaBuilder` 会照旧表多建/错建）。
2. **`gen_vfx_texture_mips`**：原来**只比行数、恒 rc0** ⇒ 改成**逐字节比内容**，
   并实测了**最关键的那一格**：行数相同、内容改一个字符 ⇒ 🔴 rc=1 且点名「第一处不同在第 N 行」。
3. **`gen_module_events`**：原来 `check()` **只比对 `WFModuleEvent.cs` 生成区**，`数据/游戏数据/module_events.json`
   缺失/过期**一个字节都不报** ⇒ 现在 json 侧也逐字节比（缺失/过期 → 🔴 rc1）。
4. 🔴 **新 check 一上线就抓到两处真缺陷（不是误报，本波不许写 `Assets/**` ⇒ 只登记，没修）**：
   - **`gen_arena_sprites`：13 场里 11 场根本没有 `<场>_sprites.json`**，
     而 `ArenaBuilder.BuildSprites` 是 `if (!File.Exists(path)) return 0;` —— **静默不建**。
     受影响对象逐场现读：`battlearena1/2/3/aeldari/blacklegion/darkangels/emperorschildren/genestealers/leviathan/spacewolves` 各 1 个
     `Crosshair`（`Atack_Icon BW`），`battlearenasororitas` 4 个 `Bottom Filler *`（`Card Text smooth background`）。
   - **`battlearenaastramilitarum_sprites.json` 是旧格式（缺 `active` 键）** ⇒ `JsonUtility` 反序列化成 `false`
     ⇒ `BuildSprites` 里 `if (!e.active) { nInactive++; continue; }` **把这 10 个 sprite 全跳过**
     （该场的 10 个正是 `Fake Light Glow*` 假光斑）。**产物在、但等于没有**。
   - **`battlearenaastramilitarum_meshkeywords.json` 过期**：产物是 **2026-10-09** 的，而清单已 **2026-10-26** 重生成
     ⇒ 少 2 条网格（`Bunker Gun ` / `Generator `，**名字带尾随空格**）。
5. 10 个 arena 脚本里 **8 个 rc0**（产物份数按各自「期望有产物」的闸算：
   `meshkeywords`/`shadowflags`/`negscale`/`vertexcolors`/`groups` **13/13** · `texslots` 13/13（槽集合） ·
   `mirror` 8/8 · `psmesh` 4/4 · `quality_toggle` 1/1 · `sprites` **1/13**），
   2 个红（`meshkeywords` 的 astra 过期 + `sprites` 的 11 缺 1 过期，**都是真账**）。
   `gen_vfx_texture_mips` / `gen_module_events` 现读**绿**。

## 证据（文件:行号）

### 改前的 B 型铁证（承 `B9_W35CHECK` 报告 §二）
- `工具/gen_arena_mirror.py:107`（改前）`if check: continue` · `gen_arena_meshkeywords.py:162` ·
  `gen_arena_quality_toggle.py:54` · `gen_arena_vertexcolors.py:703` · `gen_arena_negscale.py:332` ·
  `gen_arena_psmesh.py:60` · `gen_arena_shadowflags.py:130` · `gen_arena_sprites.py:141` ·
  `gen_arena_texslots.py:189` · `gen_arena_groups.py:704` —— **一律只扫不写、从不比对盘上产物**。
- `工具/gen_vfx_texture_mips.py:150-156`（改前）：`if check: ... len(old)==len(out)` ⇒ **只比行数、`return 0`**。
- `工具/gen_module_events.py:393-420`（改前）：`check(records)` 只解析 `CS`（`WFModuleEvent.cs`）的生成区，
  **`OUT`（`module_events.json`）根本没被读**。

### 判据来源（照抄的范式）
- `工具/import_original_art.py:1714-1721`（源在 → 再查 `os.path.exists(dst)`）+ `:1739`（缺产物单列）
  —— W30 已经补过的正例，本波 10 个 arena 脚本全部照这一条形状写。

### 改后（每个文件都新增同一形制的一对 helper，并在 `main()` 末尾返回 rc）
| 文件 | `wf_check_artifact` | 调用点 | rc 出处 |
|---|---|---|---|
| `工具/gen_arena_quality_toggle.py` | `:56` | `:160` | `:164` |
| `工具/gen_arena_mirror.py` | `:54` | `:264` | `:269` |
| `工具/gen_arena_meshkeywords.py` | `:81` | `:225` | `:230` |
| `工具/gen_arena_psmesh.py` | `:62` | `:191` | `:205` |
| `工具/gen_arena_shadowflags.py` | `:54` | `:189` | `:194` |
| `工具/gen_arena_negscale.py` | `:62` | `:393` | `:398` |
| `工具/gen_arena_sprites.py` | `:53` | `:374` | `:434` |
| `工具/gen_arena_texslots.py`（`wf_check_texslots`） | `:67` | `:418` | `:456` |
| `工具/gen_arena_vertexcolors.py` | `:79` | `:910` | `:914` |
| `工具/gen_arena_groups.py` | `:109` | `:814` | `:842` |
| `工具/gen_vfx_texture_mips.py`（内联） | `:150-173` | — | `:163/:170` 的 `return 1` |
| `工具/gen_module_events.py`（`check(records, stats)`） | `:423-444` | — | `:429/:442` |

### 内容比对的**两档**（为什么不是纯逐字节）
- 先比原始字节（`old == text`）⇒ 完全一致就过；
- 不一致时再比**语义**（`json.loads(old) == json.loads(text)`）⇒ **只差缩进/键序不算过期**
  （消费侧是 Unity `JsonUtility`，键序无关；`gen_arena_sprites` 实测过「语义相同、字节不同」的真实样本）。
- **`gen_arena_texslots` 是唯一的例外**：它写盘那份 `keep_list` 只收**导出成功**的槽
  （`export_tex` 返回 False 时该槽被丢掉），而 `--check` 不许导出 ⇒ 静态重算必然多出那些槽 ⇒ 逐字节比会**假红**。
  改成两头兜：① json 里逐条点名的 `texFile` 必须在 `Textures/` 下；② 槽集合只在**「盘上多出来」**的方向报红
  （重算多出的只出声、不判红）。理由已写进 `工具/gen_arena_texslots.py:60-64` 的注释。

### 产物路径（逐个脚本读它自己的拼法，⛔ 没照抄别的脚本）
| 脚本 | 产物 | 写盘侧「期望有产物」的闸 |
|---|---|---|
| quality_toggle | `arenas/<场>/<场>_qualitytoggle.json` | 有 `ObjectTogglerByQuality` 才写 |
| mirror | `<场>_mirror.json` | 有 `qualityPresets` 的 MB 才写 |
| meshkeywords | `<场>_meshkeywords.json` | 恒写（可为空表） |
| psmesh | `<场>_psmesh.json` | 有 `renderMode=4` 的粒子才写 |
| shadowflags | `<场>_shadowflags.json` | 恒写 |
| negscale | `<场>_negscale.json` | 恒写 |
| sprites | `<场>_sprites.json` + `Sprites/<tex>.png` | `out` 非空才写 |
| texslots | `<场>_texslots.json` + `Textures/<tex>.png` | `out`（网格支）非空才写 |
| vertexcolors | `<场>_vcol.json` | 恒写 |
| groups | `<场>_groups.json` | 恒写 |
| vfx_texture_mips | `数据/游戏数据/vfx_texture_mips.tsv` | 恒写 |
| module_events | `数据/游戏数据/module_events.json` + `WFModuleEvent.cs` 生成区 | 恒写 |

## 没查清的部分

1. **`import_remnant_sfx.py` 的 `remnant_cue_props.json` 内容比对 —— 本波没做**。
   原因：**它在我的文件白名单之外**（我只独占 `工具/gen_arena_*.py` · `gen_vfx_texture_mips.py` · `gen_module_events.py`），
   而任务红线条 5「⛔ 不越白名单」的代价是整批作废。它需要把 `cues` 的构造挪到写表之前（结构性改动，
   波及写盘路径），**建议派一个拥有该文件的代理单独做**，并先确认「写盘路径不变」。
2. **`gen_arena_sprites` 那 11 份缺失产物 + astra 那份过期，本波只登记、没生成** ——
   生成要把文件写进 `Assets/WarpforgeArena1/arenas/**`，而本件红线是「⛔ 不动 `Assets/**`」。
   ⇒ **需要一次「允许写 Assets」的重跑 + 一次 `ArenaBuilder` 建场**（且 `Sprites/*.png` 的 `.meta` 由 Unity 刷新生成）。
   ⚠️ 我**没有**验证「补上这 11 份之后画面会变成什么样」——那要跑 Unity，本波禁止。
3. **`battlearenaastramilitarum_meshkeywords.json` 少的那 2 条**：已确认是「产物比清单旧」
   （`_meshkeywords.json` mtime **2026-10-09 07:43** vs `_manifest.json` **2026-10-26 02:13**），
   但**这 2 个网格（`Bunker Gun ` / `Generator `）本来带什么关键字、我们建场时缺了会差多少**，
   本波没逐条核（要重跑生成器拿到新产物才能对）。**名字带尾随空格**是既有的 `norm()` 归一化覆盖范围，未另议。
4. **`gen_arena_texslots` 的 json 内容没有逐字节比**（见上「两档」那节）—— 这是**有意留的**，
   代价是「json 里某条 `tex` 名字被黑改成另一张已存在的图」这种极小概率漂移查不出来，但 PNG 存在性 + 槽集合两个方向能兜住常规过期。
5. **`gen_arena_camera.py` / `gen_arena_particle_{colorspace,streams,pivot}.py` 本波没动** ——
   它们**连 `--check` 都没有**（只有 `--dry` 干跑），产物是 `<场>_camera.json` 等、由 `ArenaBuilder` 编辑器期消费。
   本波范围是「补齐已有的 `--check`」⇒ 给它们**新增** `--check` 属另一件事，未做（见「顺手发现」③）。
6. **`gen_module_events --check` 的 C# 侧语义没变**：仍然要求 `WFModuleEvent.cs` 生成区与
   `records` 逐字段一致（`ROW_RE` 解析）。json 侧现在与 C# 侧**并列**判，**任一侧红就 rc=1**。

## 改动清单

| # | 文件 | 改的是什么 | 行尾 |
|---|---|---|---|
| 1 | `工具/gen_arena_quality_toggle.py` | `--check` 补：产物存在性 + 内容比对（含「多余产物」） | LF 保持 |
| 2 | `工具/gen_arena_mirror.py` | 同上 | LF 保持 |
| 3 | `工具/gen_arena_meshkeywords.py` | 同上 | LF 保持 |
| 4 | `工具/gen_arena_psmesh.py` | 同上（+ `main()` 改为返回 rc、`sys.exit(main())`） | LF 保持 |
| 5 | `工具/gen_arena_shadowflags.py` | 同上（+ `sys.exit(main())`） | LF 保持 |
| 6 | `工具/gen_arena_negscale.py` | 同上（+ `sys.exit(main())`） | LF 保持 |
| 7 | `工具/gen_arena_sprites.py` | json 内容比对 + **每个 `Sprites/<tex>.png` 存在性** | LF 保持 |
| 8 | `工具/gen_arena_texslots.py` | json 存在性 + **每个 `texFile` 的 PNG 存在性** + 槽集合「盘上多出」方向 | LF 保持 |
| 9 | `工具/gen_arena_vertexcolors.py` | 产物存在性 + 内容比对 | LF 保持 |
| 10 | `工具/gen_arena_groups.py` | 产物存在性 + 内容比对 | LF 保持 |
| 11 | `工具/gen_vfx_texture_mips.py` | `--check` 由「只比行数、恒 rc0」→ **逐字节比内容**（缺失/过期 → rc1 + 点名第一处不同行） | LF 保持 |
| 12 | `工具/gen_module_events.py` | `check(records)` → `check(records, stats)`：**补 `module_events.json` 侧比对** | LF 保持 |
| — | `资料/完全复刻/判据/生成器check覆盖表.tsv` | 更新 12 行的「查产物 / 查过期 / 本波改了没 / 验证结果 / 备注」 | LF 保持（96 行、每行 8 列，已复验） |

> **写盘路径零改动**的证据：12 个脚本的 `dst`/`OUT` 拼法、`io.open(..., 'w', ...)` 与 `makedirs` 一字未改
> （`git diff` 里只有 `if check:` 分支与 helper）；且 **质量开关/mirror/psmesh/shadowflags/negscale/vertexcolors/groups
> 7 个 + `gen_vfx_texture_mips` + `gen_module_events` 的 `--check` 现读与盘上产物逐字节一致**，
> `gen_arena_meshkeywords` 12/13 场一致（唯一例外是真过期，见结论 4），
> `gen_arena_texslots` 是**槽集合 + PNG 存在性**一致（不是逐字节，见上「两档」那节）
> ⇒ 「重算 → 写盘」那条路今天产出的字节 = 盘上那份（texslots 除外，那条按上面两条判）。

### 逐个脚本：改前行为 / 改后行为 / 红绿双验结果

| 脚本 | 改前 `--check` | 改后 `--check` | 齐绿 | 红（移走产物） | 其它红 |
|---|---|---|---|---|---|
| `gen_arena_quality_toggle` | 只扫不写，恒 rc0 | 缺→🔴rc1；内容≠→🔴；多余→🔴 | ✅ rc0（2 个对象） | ✅ 藏 `battlearenaaeldari_qualitytoggle.json` → 🔴rc1 点名 | — |
| `gen_arena_mirror` | 同上 | 同上 | ✅ rc0（8 场） | ✅ 藏 `battlearena3_mirror.json` → 🔴rc1 | — |
| `gen_arena_meshkeywords` | 同上 | 同上 | 12/13 ✅ | ✅ 藏 `battlearena1_meshkeywords.json` → 🔴rc1 点名 | 🔴 **astra 真过期**（见结论 4） |
| `gen_arena_psmesh` | 同上（`main()` 无 rc） | 同上 + 「多余」 | ✅ rc0（19 颗/4 场） | ✅ 藏 `battlearenablacklegion_psmesh.json` → 🔴rc1 | — |
| `gen_arena_shadowflags` | 同上（`main()` 无 rc） | 同上 | ✅ rc0（734 条/13 场） | ✅ 藏 `battlearena1_shadowflags.json` → 🔴rc1 | — |
| `gen_arena_negscale` | 同上（`main()` 无 rc） | 同上 | ✅ rc0（27 条） | ✅ 藏 `battlearena3_negscale.json` → 🔴rc1 | — |
| `gen_arena_sprites` | 同上（`if check or not out: continue`） | 同上 + PNG 存在性 | **仅 1/13**（tauviorla） | ✅ 藏 `battlearenatauviorla_sprites.json` → 🔴rc1 点名 | 🔴 **11 场缺 json、astra 过期**（结论 4） |
| `gen_arena_texslots` | 同上 | json+PNG 存在性、槽集合 | ✅ rc0（13/13 · 90 个网格有非主贴图槽 · 逐条点名 **193** 条 `texFile`，全部在盘） | ✅ 藏 `battlearena1_texslots.json` → 🔴rc1；**藏 `Textures/TinyFlame.png` → 🔴rc1 点名** | — |
| `gen_arena_vertexcolors` | 同上 | 同上 | ✅ rc0（13/13 逐份一致） | ✅ 藏 `battlearena2_vcol.json` → 🔴rc1 | — |
| `gen_arena_groups` | 同上 | 同上 | ✅ rc0（13/13 逐份一致） | ✅ 藏 `battlearena1_groups.json` → 🔴rc1 | — |
| `gen_vfx_texture_mips` | 只比行数 → 恒 rc0 | 逐字节比内容 | ✅ rc0（2984 行一致） | ✅ 移走 TSV → 🔴rc1 | ✅ **行数不变、改 1 个字符 → 🔴rc1 + 点名第 2 行**（原实现这一格查不出来） |
| `gen_module_events` | 只比 C# 生成区 | C# 生成区 **+** json 侧 | ✅ rc0（2 条） | ✅ 移走 `module_events.json` → 🔴rc1（C# 侧仍打印一致） | — |

> 每一条红验做完都**当场把产物还原**，并 `git status` 复核：
> `Unity/MyGame/Assets/WarpforgeArena1/arenas` 与 `Unity/数据/游戏数据` **零残留改动**
> （`git status` 里那两个路径下只有**别的代理**改的 `cardanim_clips.json`）。

## 顺手发现

1. 🔴 **`gen_arena_sprites` 的产物缺失是「静默级」的**：`ArenaBuilder.BuildSprites`
   （`WarpforgeArena1/Editor/ArenaBuilder.cs:5308-5313`）是 `if (!File.Exists(path)) return 0;` ——
   连一行警告都不打。11 场各缺 `Crosshair`（sororitas 另缺 4 个 `Bottom Filler`）。
   **建议**：① 重跑生成器补齐 11 份 + 重生成 astra 那份；② 顺手把 `BuildSprites` 的
   「文件不存在」改成 `Debug.LogWarning`（与本项目「不许静默失败」一致）——
   ② 属 `Assets/**` 改动，本波不碰。
2. **旧格式的旁挂能被 `JsonUtility` 吃下去而不报错**（astra 那份缺 `active` 键 ⇒ `bool` 默认 `false`）。
   ⇒ **旁挂加字段时，必须同时重生成全部 13 场**，否则老文件会静默退化成「全部关闭」。
   这条与「`JsonUtility` 把 null 物化成整棵子树」是同一族的坑，建议记进 `资料/已知的坑.md`（本波未写，避免与写正本的代理撞车）。
3. **`gen_arena_camera.py` 只有 `--dry`、没有 `--check`**（产物 `<场>_camera.json` 13/13 都在），
   `gen_arena_particle_{colorspace,streams,pivot}.py` 同形。它们由 `ArenaBuilder` 编辑器期消费，
   **不在「运行期直读」那一档** ⇒ 按任务口径只判不补。若以后要补，照本波的 `wf_check_artifact` 抄即可。
4. **29 个 `--dry/--verify` 脚本的「产物是否运行期直读」判了一眼**（方法：正则取脚本里的产物常量 → 在
   `MyGame/Assets/**/*.cs` 里按文件名搜引用方，并区分 `Editor/` 与非 `Editor/`）：

   | 脚本 | 产物 | 运行期(`非Editor`) 引用 | 判定 |
   |---|---|---|---|
   | `gen_cardbacks.py` | `CardPresentation/Resources/Cardbacks.json` | **11** | 🔴 **运行期直读 ⇒ 该有 `--check`**（白名单外，本波未动） |
   | `gen_offensive_cards_flat.py` | `CardPresentation/Resources/OffensiveCards.json` | **6** | 🔴 同上 |
   | `gen_prebuilt_decks.py` | `RuleEngine/Resources/prebuilt_decks.json` / `tutorial_decks.json` | **8 / 4** | 🔴 同上 |
   | `gen_flicker_cs.py` | `WarpforgeArena1/Editor/FlickerData.gen.cs` | 0（生成 `.cs`，缺了**编不过**） | 🟢 不是静默型 |
   | `gen_arena_camera.py` 等 4 个 arena 干跑族 | `arenas/<场>/…` | 0（`ArenaBuilder` 编辑器期） | 🟡 编辑器消费，非运行期 |
   | 其余 21 个 | 正则**没抓到产物常量** | — | ⏭ **本波没判出来**（不是「没有产物」，是「我的正则没命中」；⛔ 别当成已核查） |

   ⚠️ 上表第 1–3 行是**本波唯一能确定「运行期直读但只有干跑」**的三个；
   它们和 `gen_tutorial_stages` / `gen_card_inventory_options` / `gen_env_blendables` / `gen_effect_index` /
   `gen_animator_controllers` / `gen_profile_cosmetics` 一样，**都在我的文件白名单之外**，
   `B9_W35CHECK` 报告 §三 已经登记过，**留给下一波（要按文件所有权派活）**。
5. **`gen_vfx_texture_mips --check` 的输出里有一行 `🔴 **1 张同名贴图的 wrap/filter/aniso 在不同包里不一致**：ring2`
   （`battleprefabs_vfxandmisc` wrap(1,1) vs `duplicateassetisolation` wrap(0,0)）—— 这是**脚本原有的扫描诊断**，
   与产物校验无关、**不影响 rc**。别把这一行的 `🔴` 当成 check 失败。
6. `gen_arena_sprites --check` 现读会顺带把**缓存索引**写进 `d:/4/_tmp_view/sprite_index.json`
   （脚本自己的缓存，第一次跑会建）—— 属既有行为，`--check` 并没有「完全不写盘」，
   但它写的**不是工程产物**，不影响红线判断。⚠️ 这条不要被误读成「`--check` 会改产物」。
