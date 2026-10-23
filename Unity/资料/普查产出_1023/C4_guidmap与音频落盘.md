# C4 · `guid_map` 补集落盘 + 缺件音频导入（执行代理 C）

> 2026-10-23。**未跑 Unity、未动 git、未改正本**。
> 本批两件都是主对话已批准的**落盘**动作；本报告记录落盘后的核对结果 + 一条**必须停手报告**的数据缺陷。

---

## ① 结论

### 1·1 `guid_map` 补集落盘 —— ✅ 完成，与干跑件逐字节相同

| 项 | 落盘后 |
|---|---|
| 文件 | `Unity/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` |
| **md5** | **`af6a1ea7b2faa5c20e272d715c44ba91`**（= 干跑件 `guid_map_bundles.cardsonly.dryrun.tsv` 的 md5） |
| **字节 / 行数** | **1,303,126 B / 6,265 行**（6,173 数据行 + 91 新增 + 1 表头） |
| 与干跑件 | **逐字节相同** |
| 现有 6,173 行原文 | **逐字节前缀不变**（旧文件 1,287,180 B 的全部内容 = 新文件前 6,174 行） |
| `git diff --numstat` | **`91 0`**（纯追加，0 删除/0 改行） |
| 未收「补类型」 | ✅ 按拍板只做「没有的行才加」；91 行都是我核过的**新 GUID**（Sprite↔Texture2D 同 GUID 的补类型**未**追加） |
| `source` 新取值 | **`catalog-internalid`**（91 行全部是这个值；下游按 `source` 白名单过滤的需同步） |
| 内容复查 | 新增 74 行 EC `cardassets` + 17 行 EC `cardanims`；类型 57 AudioClip / 17 MonoBehaviour / 9 Sprite / 8 Texture2D（与干跑一致） |

### 1·2 缺件音频导入 —— ✅ 文件已补；⚠️ **`voice_lines.json` 出现 4 条错配，按约定停手报告**

**`--check` 输出（真跑前）**：
```
音频源：1857 个文件（1857 个不同文件名）
卡 SO（归属① · 权威）：引用 698 条 / 唯一文件名 697 个 / 卡 641 张
  并进我们卡表 696 条 · **两张卡共用、不认** 1 条 · **卡不在我们卡表** 0 条 · **同 stem 两个扩展名** 0 条 · **音频源里没有** 0 条
…（15 个字段逐字段：summonAudio 引用 537/已解 536 · deathAudio 61/59 · attackAudio 59/59 · intro 59/58 · mirror 59/58 · threat 59/58 · wp 59/58 · gen2 59/58 · gen3 59/58 · cantdo 58/57 …）
（--check：没有写盘）
```
**真跑**：`已拷 1 条 · 已存在跳过 1826 条`。

- ① **新文件**：`Resources/Art/audio/vo/VO_UM_Firestrike Servoturret - Battlefields are filled with the remains of the obstinate.wav`
  —— **247,468 B**，**md5 `a6992e9149a591d1ec355929bb3050b6` = 源文件 md5**（逐字节拷贝）；同目录 `.wav.meta` 也已在（485 B，07:27 生成 —— **不是我写的**，见 ⑤·4）。
- ② **`voice_lines.json`**：写入成功；**行尾 LF 未翻**（crlf 0）。核对明细见 1·3。
  ⚠️ **落盘后同一分钟（07:27）文件被外部进程重排为 `indent=2`**（不是我写的；内容逐条比对不变）——
  现读 **413,937 B / 16,395 行 / md5 `7c05a220ecfda7be4b8d398940e4f791`**，`git diff --numstat` 因此是 **`845 / 744`**（内容级），不是整表缩进 diff。
- ③ **⚠️ 停手原因**：`voice_lines.json` 里 **EC56（Lord Kaphrael）新增的 5 条 ChatSound 中有 4 条指向了别的角色的音频** —— 根因是 `anim_address_map.json` 对**短键资产**的 GUID→名字有 **12 条真错**（见 1·4）。这是**上游数据缺陷**，不是本次脚本逻辑问题；按「变化超出预期就停手报告」的约定，我**没有继续修**，也没有再改任何文件。

### 1·3 `voice_lines.json` 的逐条变化（全量）

**体量**：基文件 **411,111 B / 16,295 行**（indent=2）→ 导入器写出 **346,030 B / 16,395 行**（indent=1）
→ **同一分钟被外部进程重排回 indent=2 = 413,937 B / 16,395 行 / LF**（内容逐条比对不变）。
⚠️ 导入器那次「字节下降 65,081 B」**不是内容丢了**：`json.dumps(base, ensure_ascii=False, indent=1)` = **343,694 B**，
与导入器输出 **346,030 B** 比 ⇒ **内容实际 +2,336 B**；现文件（indent=2）再序列化即 413,937 B。
⇒ **`git diff --numstat` 现为 `845 / 744`（内容级）**，不再有整表缩进 diff。

**内容变化清单（全量、逐条）**

1. **卡片 +3 / −1**（净 +2，640 → 642）：
   - **+`UM91` Tactical Marine**：`deploy` = `VO_UM_Tactical Marine - We are all sons of Lord Guilliman…wav`（`src=cardSO`）—— 原来记在 32-hex 卡 `dcbd2852…`（"Master Tactician"）名下，**按 SO 归位**；
   - **+`UM92` Firestrike Servo-Turret**：`deploy` = 本次补拷的 Firestrike `.wav`（`src=cardSO`）—— **本次导入的目标**；
   - **+`UM98` Devastator Marine**：`deploy` = `VO_UM_Devastator Marine - Keep firing!…wav`（`src=cardSO`）—— 原来记在 `UM25`（Devastator Centurion）名下，**按 SO 归位**；
   - **−`dcbd28525888ce54aaa93a47ba4de1e6`**（"Master Tactician"，32-hex 卡）：唯一那条 deploy 归 `UM91` 后**空卡移除**。
2. **41 张卡的 `name` 刷新**（全部是 `cards_engine.json` 的**现行卡名**，无 `faction` 变化；例 `AM11` `Shock Trooper Sgt`→`Shock Trooper Sgt.` · `AM32` `Leman Russ`→`Leman Russ Tank` · `EC57` `Xarahan Daemon Prince`→`Xarahan, Daemon Prince` · `GOF20` `Veteran Flyboy`→`Veteran Stormboy` · `SW38` `Land Rider`→`Land Raider` · `UM101` `2nd Company Terminator`→`1st Company Terminator` …）。
3. **台词行 +5**（全部 `EC56`，`src=cardSO`）：
   ```
   concede  VO_CSM_EC_Daemonette (Emily).ogg                    ❌ 应为 VO_CSM_EC_Lord Kaphrael_concede
   default  VO_CSM_EC_Lord Kaphrael_attack.ogg                  ✅（该卡 defaultChatSound → attack，名字正确）
   gen1     VO_CSM_EC_Blastmaster Noise Marine (Kevin).ogg       ❌ 应为 …Kaphrael_gen1
   greet    VO_CSM_EC_Daemon Prince Xarahan_gen5.ogg             ❌ 应为 …Kaphrael_greet
   threat   VO_CSM_EC_Icon of Excess Infractor (Andrew Dennis)   ❌ 应为 …Kaphrael_threat
   ```
   ⚠️ **运行时影响**：EC56 的 `concede` / `gen1` / `greet` / `threat` 在 `voice_lines.json` 里**同时有**「错误的 cardSO 行」和「正确的 orphan 行」，而 `VoiceLines.TryPick` 在 `rng=null` 时**取第一条** ⇒ **会取到错误的 4 条**（`ForChatButton = greet/threat/wp/gen1/gen2/gen3`、`ForConcede = concede/default`）。
4. **台词行 −16**：
   - `EC57` ×13（cantdo/concede/gen1/gen2/gen3/gen4/gen5/greet/hurry/intro/mirror/threat/wp）—— 2022 SO 的 EC57 **只有 `summonAudio`、没有任何 ChatSound** ⇒ 旧的 13 条是**按名猜的 orphan**，按 SO 权威**删除（正确）**；
   - `EC56` ×1（`default` 原指向 `…_gen7.ogg`）—— SO 的 default 是 `5a6da315…`（=Kaphrael_attack）⇒ gen7 落回 orphan（正确）；
   - `BL34` ×1（`VO_BlackLegion_Hexscorn_vs_Abaddon.ogg`，orphan 猜测）—— SO 不再支持，删除；
   - `UM25` ×1（`VO_UM_Devastator Marine - Keep firing!…`）—— 归位到 `UM98`（见上）。
5. **122 行的元数据变化（内容不变）**：`src` 119 条（`None→cardSO` 82 · `None→orphan` 32 · `orphan→cardSO` 4 · `cardSO→orphan` 1）+ `so` 14 条（13 条新增字段名 + 3 条 `summonAudio→None`）。
   **`ev` / `file` / `text` 三个字段在共同行上 0 变化**（程序化比对，不是肉眼）。

### 1·4 停手报告：`anim_address_map.json` 的 **12 条真错**（其中 6 条音频）

用更新后的 catalog/guid_map 复核 6,000 个共同 GUID：**5,946 同名 · 54 名字不同**；
54 条里 **42 条是假阳性**（同一 GUID 的 Texture2D / Sprite 两种类型，名字本来就不同，catalog 确认两边都对）；
**12 条是真错**（catalog + 本地束对象实读，`anim_address_map` 指向了**另一个对象**）：

| # | GUID | `anim_address_map` 写的 | **真值**（catalog 容器键 → 束内对象） | 领域 |
|---|---|---|---|---|
| 1 | `8c3458c0…` | `VO_CSM_EC_Daemonette (Emily)` | 键 `8` → **`VO_CSM_EC_Lord Kaphrael_concede`** | 音频 |
| 2 | `ad93b47f…` | `VO_CSM_EC_Blastmaster Noise Marine (Kevin)` | 键 `a` → **`…Kaphrael_gen1`** | 音频 |
| 3 | `92ba9ee3…` | `VO_CSM_EC_Daemon Prince Xarahan_gen5` | 键 `9` → **`…Kaphrael_greet`** | 音频 |
| 4 | `c3244a98…` | `VO_CSM_EC_Icon of Excess Infractor` | 键 `c` → **`…Kaphrael_threat`** | 音频 |
| 5 | `399710fd…` | `VO_CSM_EC_Daemon Prince Xarahan_wp` | 键 `3` → **`…Kaphrael_gen4`** | 音频 |
| 6 | `5a6da315…` | `VO_CSM_EC_Lord Kaphrael_gen7` | 键 `5` → **`…Kaphrael_attack`** | 音频 |
| 7 | `c6bb19dd…` | `CSM_EmperorsChildren_inf_Icon of Excess Infractor` | 键 `c6b` → `…_warlord_Lord Exultant` | 立绘 |
| 8 | `f0a86ec5…` | `CSM_EmperorsChildren_dae_Blissbringer` | 键 `f0a` → `…_strat_Tools of Torture` | 立绘 |
| 9 | `b0f1e4fd…` | `40k_Cardframes_stratagem_Emperor Children_tier2` | 键 `b0f` → `…_offensive_Aural Hijack` | 卡框 |
| 10 | `a062d6b0…` | `40k_Cardframes_stratagem_Emperor Children_tier4` | 键 `a06` → `…_strat_Elixir_Skorflense` | 卡框 |
| 11 | `c58ac7f6…` | `FX` | catalog **无此 key**（旧 build / 非 Addressable） | 其它 |
| 12 | `1cfd83ef…` | `BattleGroupHigh` | 同上 | 其它 |

**根因**：这 10 条错误**全部落在短键束**（EC cardassets 键 `8/a/9/c/3/5/c6b/f0a/b0f/a06`）——
`anim_address_map` 是旧链路（目录退键）建的，和本次 `guid_map` 补集修的是**同一族缺口**。
第 11/12 条 catalog 无 key，**判不了**（不算错、也不算对）。

**影响面**：`import_original_audio.py` 的 ①·B（12 个 ChatSound）GUID→名字用的是
`card_vfx_by_card._assetRefs` ∪ `anim_address_map.guid_to_asset`，**不吃 `guid_map`** ⇒ 本次 4 条错配就是从这里来的
（第 6 条被 `_assetRefs` 的正确名字遮住，所以对）。

**建议（未做，等拍板）**：
1. 用更新后的 `guid_map_bundles.tsv`（或 catalog）**重建 `anim_address_map.json` 的 `guid_to_asset`**（至少这 12 条）；
2. 或在 `import_original_audio.py` 的 `load_guid_name_index` 里把 `guid_map_bundles.tsv` 加为**更高优先级的第三来源**；
3. 修完后**重跑** `import_original_audio.py` ⇒ 4 条错配会自动变成正确的 `…Kaphrael_concede / _gen1 / _greet / _threat`。
⛔ 在修好之前，`voice_lines.json` 这 4 条**是错的**（其余变化均为正确刷新）。

---

## ② 证据

**`guid_map` 落盘**
- 命令：`D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/gen_guid_map_from_bundles.py --cards-only`
- 日志：`d:/4/_tmp_view/wfC3/official_run.log`
- md5/字节/行数、与干跑件逐字节相同、旧 6,173 行前缀不变 —— 见 `d:/4/_tmp_view/wfC3/guid_map_bundles.base.tsv` 与 `…dryrun.tsv` 的对比（本报告 1·1 的数）
- `git diff --numstat`：`91 0`

**音频导入**
- `--check` 日志：`d:/4/_tmp_view/wfC4/audio_check.log`
- 真跑日志：`d:/4/_tmp_view/wfC4/audio_run.log`（末行 `已拷 1 条 · 已存在跳过 1826 条`）
- 新文件：`Resources/Art/audio/vo/VO_UM_Firestrike Servoturret - …obstinate.wav`（247,468 B · md5 `a6992e91…` = 源）
- 源：`d:/2/新解包资源/assets_full/bundle_spacemarinesultramarinescardassets_assets_all/AudioClip/<同名>.wav`
- voice_lines：基文件快照 `d:/4/_tmp_view/wfC4/voice_lines.base.json`；逐条 diff 日志 `d:/4/_tmp_view/wfC4/voice_diff.log`（脚本 `diff_voice.py`）
- `anim_address_map` 复核：`d:/4/_tmp_view/wfC4/anim_map_verify.py`（catalog `lookup` 逐 GUID 实读；脚本 `anim_map_vs_guidmap.py` 出 54 条候选）
- GUID `8c3458c0…` 的实读链：catalog key **`8`** → `chaosspacemarinesemperorschildrencardassets_assets_all.bundle` → PathID `4754310492230562471` = **`VO_CSM_EC_Lord Kaphrael_concede`**（而 `anim_address_map` 指的 PathID `4057260588233156656` = `VO_CSM_EC_Daemonette (Emily)`）

**`voice_lines.json` 体量**
- 落盘前：411,111 B / 16,295 行 / md5 `2649cba693843a6c00f183f1ed24041a`（indent=2）
- 导入器输出：346,030 B / 16,395 行 / LF（`json.dumps(..., ensure_ascii=False, indent=1)` **逐字节相同**）
- **现盘（外部重排回 indent=2）**：**413,937 B / 16,395 行 / md5 `7c05a220ecfda7be4b8d398940e4f791`**
- 内容级：base 按 indent=1 重序列化 = 343,694 B ⇒ **净内容 +2,336 B**（现文件内容与导入器输出**逐条相同**，只差缩进）

---

## ③ 没查清 / 待裁决

1. **`anim_address_map.json` 怎么修**：重建 `guid_to_asset`（用新 guid_map）还是给导入器加第三来源 —— 两条路的取舍没定（不在本批白名单）。
2. **那 4 条错配的处置**：保留待修 vs 先把 `voice_lines.json` 回滚到基文件（UM92 那条会一起丢）—— 按「停手报告」未做。
3. **12 条真错里的第 11/12 条**（`c58ac7f6…` / `1cfd83ef…`）：catalog 无 key，**判不了**；它们对本题（卡语音）无影响。
4. **`voice_lines.json` 的缩进归一（2→1）**要不要接受：现行导入器规范就是 indent=1（下次重跑还会是 1）；若团队更看重最小 diff，需要**改导入器的序列化**（改一处，但会再翻一次整表）。

---

## ④ 改动清单

| 文件 | 改动 |
|---|---|
| `Unity/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` | **+91/−0**（6,173 → 6,264 数据行；md5 `af6a1ea7…`；= 干跑件逐字节） |
| `Unity/MyGame/Assets/CardPresentation/Resources/Art/audio/vo/VO_UM_Firestrike Servoturret - …obstinate.wav`（+ `.meta`） | 新增 1 个音频（247,468 B，与源 md5 相同）；`.meta` 是 Unity 自动生成（见 ⑤·4） |
| `Unity/MyGame/Assets/CardPresentation/Resources/voice_lines.json` | 内容：3 卡新增 / 1 卡移除 / 41 卡名刷新 / +5 / −16 台词行 / 122 行 `src`/`so` 元数据（净 +2,336 B）；**含 4 条错配待修**。格式化：导入器写 indent=1（346,030 B）→ **同一分钟被外部进程重排回 indent=2（413,937 B）**，内容不变；`git diff` = `845/744` |
| `d:/4/_tmp_view/wfC4/*`（未入仓） | 基文件快照 / 日志 / 只读复核脚本 |
| `Unity/资料/普查产出_1023/C4_guidmap与音频落盘.md` | 本报告 |

**验过**：`git status` 范围 = 上述 4 个文件（`Resources/Art/` 在 .gitignore 内）· `git diff --numstat` = **`91 0`**（tsv）+ **`845 744`**（voice_lines，内容级；缩进已被外部进程归一）· `voice_lines.json` LF 未翻 · 未动 `.cs` / 未跑 Unity / 未改正本。

---

## ⑤ 顺手发现

1. **`guid_map` 与 `anim_address_map` 是两套解**：本次补集修好的短键族，`anim_address_map` 里仍有 **10 条真错**（6 音频 + 4 美术/卡框）⇒ 只要下游还用它，就会继续错；建议把它的 `guid_to_asset` 从新 guid_map 重建（**建议，未做**）。
2. **导入器不读 `guid_map`**：`load_guid_name_index` 只并 `_assetRefs` ∪ `anim_address_map` ⇒ 本次 4 条错配正是这个断层造成的；修法二选一（见 ③·1）。
3. **`voice_lines.json` 的缩进：基文件 2、导入器写 1、落盘后又被外部进程改回 2** ——
   这三种状态在同一分钟内出现过；**内容始终相同**（只差缩进）。现在 `git diff` 是 `845/744`（内容级），
   但**下一次跑导入器还会把它写成 indent=1**（整表 diff 16K 行）⇒ 要根治得改导入器的序列化口径（**未做**）；
   在那之前，遇到「整表缩进 diff」可判定为**格式化、不是内容变化**（判据 = 解析后逐条比对，本报告 §1·3）。
4. **`.wav.meta` 是 07:27 自动出现的**（导入器源码不写 meta）⇒ 说明当时**有 Unity 在跑**（自动 import 了新文件）。本批红线是「不跑 Unity」，我没有起 Unity；但这条要注意：**在别人跑批处理时写 `Resources/` 会被它捡走**（本次结果无害：meta 正常、GUID 新分配）。
5. **`anim_address_map` 的 42 条假阳性**提醒：同一 GUID 常有 Texture2D+Sprite 两份、**名字可以不同**（Sprite 名可能是 `Orc Frame` 这类共享名）。以后对账必须按 `(GUID, type)` 而不是只按名字，否则会把 42 条正常项误报成错。
