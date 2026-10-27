# B9_W35CHECK —— 生成器/导入器 `--check` 的「产物存在性」体检与补缺

> 执行代理：B9_W35CHECK（2026-10-27）· 清单：`判据/生成器check覆盖表.tsv`
> 起因：`import_original_art.py --only-altart --check` 曾只查「源在不在」、不查「产物在不在」
> ⇒ 卡 id 改名后 13 天缺图无人知（W30 已就地补，`:1714-1721`/`:1739`）。本波把**这一族**普查一遍，
> 按影响面排序，先把「产物会被运行期静默消费」的那几族补齐。

## 一、结论

1. **`工具/` 下 106 个 `gen_*/import_*/bake_*.py`**（现读；无 `bake_`）。其中 **64 个有 `--check`**，
   另有 **29 个只有 `--dry` / `--verify`（干跑，不是校验）**。
2. **绝大多数「卡表 / 动画表 / 本地化」的 `--check` 已经是 A 型**（重算内容 vs 盘上逐字节比，
   缺失/过期 → ❌ + 非零退出）：`gen_cards_engine`/`gen_cards_truth`/`gen_cardanim_clips`/`gen_anim_clips`/
   `gen_card_anim_order`/`gen_ability_anim_durations`/`gen_battlehud_clips`/`gen_misc_clips`/`gen_division_clips`/
   `gen_camera_intro_clips`/`gen_official_loc`/`gen_official_loc_13lang` 等（逐条见 TSV「查产物=✅」）。
   **这批不用改**。
3. **真 B 型（`--check` 压根不碰产物）= 本波重点，共 4 类**：
   - **音效/语音 cue 族**（运行期直接按文件名加载）——`import_original_sfx` / `import_original_audio` /
     `import_battle_sfx` / `import_battle_bgm` / `import_remnant_sfx`：**拷贝循环整段在 `if not args.check` 里**，
     `--check` 只报「源能不能解析」，**产物（音频 + 表）缺失/过期一律静默绿**。→ **本波已 5 个全部补齐**。
   - **美术族**——`import_original_3dcard`（只查源）、`import_original_card_sdf`（只查源 + 已存在 PNG 的 meta）
     的产物 PNG 存在性查不到。→ **本波已补齐**。
   - **运行时直读的两张 JSON**——`gen_frame_uv`（`Core/CardView.FrameUv` 直读 `frame_uv.json`）、
     `gen_simple_animation_clips`（`WarpforgeAnimatorBridge` 直读）原来只「打一行存在性、恒 rc0」。→ **本波已补齐**。
   - **战场/预制件清单族（`gen_arena_*`，10 个）**——`--check` = 「只扫不写」（`if check: continue`），
     **从不比对盘上产物**。→ **本波只登记、未改**（见 §三）。
4. **仍未查产物但影响偏小、本波未改的 B 型**：`gen_effect_index` · `gen_env_blendables` ·
   `gen_card_inventory_options` · `gen_animator_controllers` · `gen_profile_cosmetics` ·
   `gen_tutorial_stages`（产物 json）· `gen_vfx_texture_mips`（只比行数、恒 rc0）· `gen_module_events`（只比 C# 表、不比 json）。
   逐条列在 TSV，附「B 型」备注。**下一波建议按此表补**。
5. **本波改了 9 个脚本**（7 个音频/美术 + 2 个运行时 JSON），逐个都改的是**只读的 `--check` 分支**，
   **写盘路径一个字节没动** ⇒ 生成幂等不变（`import_battle_bgm` 写两遍 `sha256` 相同，实测）。

## 二、证据（文件:行号）

- 普查口径：`ls 工具/gen_*.py import_*.py bake_*.py`（106 个，无 bake_）· `grep -l -- "--check"`（64 个）。
- 参考范式（W30）：`工具/import_original_art.py:1714-1721`（源在 → 再查 `os.path.exists(dst)`）、`:1739`（缺产物单列）。
- **B 型铁证**（拷循环被 check 跳过）：
  - `import_original_audio.py:1116` `if not args.check:`（拷贝循环；`--check` 时整段不跑）
  - `import_original_sfx.py:647` 同上；`import_battle_sfx.py:317`；`import_battle_bgm.py:201`
  - `import_remnant_sfx.py:169`（`--check` 在拷贝循环**之前**就 return）
  - `gen_frame_uv.py`（原）check 处「只打不写盘 return 0」——现有 `:187-193` 已改为内容比对
  - `gen_simple_animation_clips.py`（原）check 处「只打存在/不存在 rc0」——现有 `:113-121` 已改为内容比对
  - `gen_arena_mirror.py:107` `if check: continue` · `gen_arena_meshkeywords.py:162` · `gen_arena_quality_toggle.py:54`
    · `gen_arena_vertexcolors.py:703` · `gen_arena_negscale.py:332` · `gen_arena_psmesh.py:60`
    · `gen_arena_shadowflags.py:130` · `gen_arena_sprites.py:141` · `gen_arena_texslots.py:189` · `gen_arena_groups.py:704`

### 改动的 9 个文件（都是「只读 check 分支」新增，写盘路径未动）
1. `工具/import_original_sfx.py` — `--check` 增：JSON 比对 + `need_files` 逐个 `os.path.exists`/尺寸；缺→🔴rc1。
2. `工具/import_original_audio.py` — 同形（`used_files` + `voice_lines.json`）。
3. `工具/import_battle_sfx.py` — 把「要拷 N 个」升级为**点名缺哪些** + JSON 比对；缺→🔴rc1。
4. `工具/import_battle_bgm.py` — 同形（`uniq` + `battle_bgm.json`）。
5. `工具/import_remnant_sfx.py` — `--check` 早退前补 5 个音频产物存在性/尺寸（JSON 未比对，见 §三）。
6. `工具/import_original_3dcard.py` — `--check` 补 `DEST/dst` 存在性（10 个）。
7. `工具/import_original_card_sdf.py` — `--check` 补 104 张产物 PNG（+generic/cardback_noise）存在性。
8. `工具/gen_frame_uv.py` — `--check` 由「只打不写盘」改为比 `frame_uv.json` 内容（缺失/过期→❌rc1）。
9. `工具/gen_simple_animation_clips.py` — `--check` 由「只打存在性 rc0」改为比内容。

## 三、没查清的部分 / 未改（诚实登记，⛔ 不猜）

1. **`gen_arena_*` 十个 → 本波未改**：它们的 `--check` 是「只扫不写」，需要按每场的产物路径
   （`{ARENAS_ROOT}/<场>/<场>_XXX.json`）逐一补存在性比对。产物由**编辑器 `ArenaBuilder` 消费**（非运行期直读），
   影响面比音频/卡表低一档，且 10 个脚本 dst 命名各异、批量改风险高 ⇒ **按「别一把梭、按影响面」留到下一波**。
   TSV 里已逐条标「查产物 ❌ / B 型」。
2. **`gen_module_events --check`**：只比对 `WFModuleEvent.cs` 的生成区（运行期真正消费的是这份 C# 表），
   **`OUT` 那份 json 未比对**。C# 表已覆盖运行期口径 ⇒ 影响小；json 侧未查，留档。
3. **`gen_vfx_texture_mips --check`**：只比**行数**、且恒 `rc0` ⇒ 内容变了但行数不变时报不出来。未改。
4. **`import_remnant_sfx`**：`remnant_cue_props.json` 本 check **未比对内容**（脚本在写表之前就 `return`，
   要比对需把 `cues` 构造挪到 check 之前，属结构性改动）⇒ 只补了音频产物，JSON 侧留档。
5. **`--dry` 干跑族（29 个）**：`--dry` 语义 = 「算完不写盘」，**不是校验** —— 这些脚本若产物被删/过期，
   `--dry` 同样不会报（W30 同型）。本波**未逐条深读**其产物是否运行期消费，仅登记在 TSV 附录。
6. **`import_original_luts` / `import_deck_warlord_art`**：`--check` 会逐张 `os.path.isfile(dst)` 并打
   `已存在`/`--check`/`WOULD`（缺失**可见**），但**不报红、不查过期**。属「弱覆盖」，本波未强化。
7. **速度备注**：`gen_frame_uv --check` 现读会重算 150 张框图的徽记（`_dist_cheb` 每图最多 300 次全图 MinFilter），
   **单跑就是分钟级**（改前也是，与我无关）；`gen_simple_animation_clips --check` 要 `UnityPy.load` 全部 bundle，同样慢。

## 四、改动清单

| # | 文件 | 改的是什么 | 行尾 |
|---|---|---|---|
| 1 | `工具/import_original_sfx.py` | check 分支补 JSON+音频产物比对 | LF 保持 |
| 2 | `工具/import_original_audio.py` | check 分支补 JSON+vo 产物比对 | LF 保持 |
| 3 | `工具/import_battle_sfx.py` | check 分支补点名缺失+JSON 比对 | LF 保持 |
| 4 | `工具/import_battle_bgm.py` | check 分支补点名缺失+JSON 比对 | LF 保持 |
| 5 | `工具/import_remnant_sfx.py` | check 早退前补音频产物存在性 | LF 保持 |
| 6 | `工具/import_original_3dcard.py` | check 补产物 PNG 存在性 | LF 保持 |
| 7 | `工具/import_original_card_sdf.py` | check 补产物 PNG 存在性 | LF 保持 |
| 8 | `工具/gen_frame_uv.py` | check 由「只打」改为内容比对 | LF 保持 |
| 9 | `工具/gen_simple_animation_clips.py` | check 由「只打存在性」改为内容比对 | LF 保持 |
| — | `资料/完全复刻/判据/生成器check覆盖表.tsv` | 新建覆盖表（64 --check + 29 dry 附录） | 新文件 |

### 验证结果（逐脚本）
| 脚本 | 齐全（绿） | 缺产物（红） | 幂等 |
|---|---|---|---|
| import_original_sfx | ✅ 543 音频 + 表一致 | ✅ 藏 JSON→🔴 rc1 | 写路径未动 |
| import_battle_sfx | ✅ 29 音频 + 表一致 | ✅ 藏 JSON→🔴 rc1 | 未动 |
| import_battle_bgm | ✅ 25 音频 + 表一致 | ✅ 藏 JSON→🔴 rc1 | ✅ 写两遍 sha256 相同 |
| import_remnant_sfx | ✅ 5/5 音频 | ✅（同型，红路径逻辑静态核） | 未动 |
| import_original_3dcard | ✅ 10/10 | ✅ 藏 1 张→🔴 rc1 | 未动 |
| import_original_card_sdf | ✅ 106 产物 | ✅ 藏 1 张→🔴 rc1 | 未动 |
| import_original_audio | 同 sfx 形（未单跑，源读 bundle 慢） | 同 sfx 形 | 未动 |
| gen_frame_uv | ✅ 合成夹具：write=0/green=0 | ✅ 合成夹具：藏产物→rc=1 | 未动 |
| gen_simple_animation_clips | ✅ 合成夹具：write=0/green=0 | ✅ 合成夹具：藏产物→rc=1 | 未动 |

> 说明：`gen_frame_uv` / `gen_simple_animation_clips` 的「比内容」改法与已双验通过的
> `gen_anim_clips:204-210`、`gen_misc_clips:487-492` **逐字同形**（`old = read if exists else None; same = old == text`），
> 且写盘路径未改动。这两条的**真实全量输入是分钟级**（`gen_frame_uv` 150 张框图 × `_dist_cheb` 每图最多 300 次全图 MinFilter；
> `gen_simple_animation_clips` 要 `UnityPy.load` 全部 bundle），故本波用**合成夹具**（临时小图 / 空 bundle 目录，
> `main()` 内 write→green→red 三态）实证了比对逻辑：`write=0/green=0/藏产物=1`，两条全过。

## 五、顺手发现

1. **`import_remnant_sfx.py` 的 `DEST` 是相对路径**（`"MyGame/..."`，`:49`），
   而其它音频导入器是绝对路径（`"d:/4/Unity/..."`）⇒ **必须在 `d:/4/Unity` 下运行**，换个 cwd 就会写到别处或崩。
   （不是本波引入；仅记录。）
2. **`gen_frame_uv` / `gen_simple_animation_clips` 的 `--check` 慢得离谱**（分钟级），
   根源是重算全量输入（`_dist_cheb` 每图最多 300 次全图 MinFilter；UnityPy 全 bundle 加载）。
   若将来要把它们塞进 CI / 同步点，建议给「只比产物、不重算」的快速档，或缓存输入哈希。
3. **`gen_frame_uv` 的 `if check:` 原来放在 `data` 构造之前**（早退），意味着 `--check` 连「重算」都没做、
   纯打一行字 —— 这是「看着有 check、其实什么都没查」的最隐蔽形态（比 arena 的 `continue` 更隐蔽）。
   建议后续普查时把「check 分支里有没有读取产物」作为一条机器可查的判据。
4. 本族里 **A 型（重算 vs 盘上逐字节比）是主流且最佳范式**，`gen_trait_effect_bindings:280-286` 与
   `gen_anim_clips:204-210` 是两条可以照抄的模板。

## 交接给下一个代理（轮换用）
- **已完成**：音频 cue 族 5 个 + 美术 2 个 + 运行时 JSON 2 个，共 9 个补齐；TSV 覆盖表已建。
- **下一波（按影响面排序，判据已在 TSV 里）**：
  1. `gen_arena_*` 十个（编辑器消费，`--check` 只扫不写）——需按场产物路径逐个补存在性/内容比对。
  2. `gen_vfx_texture_mips`（只比行数、恒 rc0）· `gen_module_events`（json 侧）· `gen_tutorial_stages`（产物 json）
     · `gen_card_inventory_options` · `gen_env_blendables` · `gen_effect_index` · `gen_animator_controllers` · `gen_profile_cosmetics`
     ——都是「`--check` 不碰产物」的 B 型，补法照本波（`old=read if exists else None; same=old==text; rc1`）。
  3. 29 个 `--dry` 干跑脚本：先判「产物是否运行期消费」，是则要么加 `--check` 内容比对、要么把 `--dry` 升级成会报红的校验。
- **别推翻**：A 型脚本（见 TSV「查产物=✅」）不要动；本波 9 个只改了 check 分支，写盘路径未动，别「顺手重构」写盘那段。
