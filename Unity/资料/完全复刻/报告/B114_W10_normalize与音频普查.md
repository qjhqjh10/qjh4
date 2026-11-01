# B114_W10 —— `_sfx_meta` 兜底 `normalize`（B-238）+ 全量 Ogg 页结构普查（B-241）

> 写手：`W-10` · 日期：**2026-11-01** · 只读普查 + 一处工具修补
> 碰过的文件：`Unity/工具/_sfx_meta.py`（改）· 新建 `Unity/工具/audio_ogg_page_census.py` · 本报告
> 没碰：任何 `.ogg` 本体 · `Assets/**` · 两份正本 · `账/*.tsv` · `Scenes/*.unity` · `.gitignore` · `ProjectSettings/*`
> 没跑 Unity、没动 git。
> 产物：`d:/4/_tmp_view/w10/census_full.json`（普查原始读数）· `d:/4/_tmp_view/w10/smoke*.json`（冒烟）

---

## §一 `B-238`：`pick_template` 兜底把 `normalize` 从 0 漂到 1 —— **已修**

### 1·1 缺陷复述（现读，不是转抄）

| 项 | 现读值 | 判据 |
|---|---|---|
| `FALLBACK_TEMPLATES` 两套模板的 `normalize` | 原来是 **`1`**（`.wav` 第 120 行 · `.ogg` 第 145 行） | `工具/_sfx_meta.py:120` / `:145` |
| 全树现读 `normalize` 分布 | **`0` × 2,494** · **`1` × 55** | 现扫 `Resources/Art/audio/**/*.meta`（2,549 份 `AudioImporter` meta） |
| 兜底什么时候生效 | `pick_template()` **只在目标目录一条同扩展名兄弟 `.meta` 都没有**时退回兜底 | `工具/_sfx_meta.py:393` |
| 漂移机理 | 冷重建第一份没有兄弟 ⇒ 拿兜底（`1`）；**之后所有兄弟的「众数」就等于第一份写出来的东西** ⇒ 整棵树跟着变 `1` | 同左 |

⇒ **把 `audio/` 删空再冷重建，2,494 份会从 `0` 漂到 `1`。** `normalize` 属 `UNPROVABLE`（原版 `AudioClip` typetree 里没有这个字段，见 `:287-291`），**本地没有任何数据源能给出逐条真值**，所以它只能「跟现状一致」，不能靠原版定。

**为什么它一直没被 `--verify` 抓到（本轮查实的第二个洞）**：`verify()` 的 ④ 段**算过**「同目录同扩展名众数」，但对**有原版真值**的文件，只要那 4 项对得上就整条 `continue`（`:607-614` 那句「差异是**故意的**，不是漂移」）⇒ 55 份 `normalize: 1` 属于「4 项对得上」那一档，**连黄都不报**。这就是「兜底写错了一年也没人发现」的原因。

### 1·2 修法（两处，都在**同物理行**上改，行号一个没动）

| # | 改了什么 | 位置 | 行号变化 |
|---|---|---|---|
| ① | `.wav` 兜底 `b"  normalize: 1\n"` → `b"  normalize: 0\n"` | `:120` | **0** |
| ② | `.ogg` 兜底 `b"  normalize: 1\n"` → `b"  normalize: 0\n"` | `:145` | **0** |
| ③ | 兜底模板上方 6 行注释就地改写（写明「`normalize` 必须 = 现读众数 0」+ 指向 `--check-fallback`） | `:96-101` | **0**（仍是 6 行） |
| ④ | 新增 `check_fallback()`（放在 `probe()` 之后、`def main()` **之前**）+ 三个 CLI 开关（`--check-fallback` / `--deep` / 改 `--self-test` 的帮助串）+ `--self-test` 末尾接一句 | 文件尾部 | **只动 `main` 及其后** |

🔴 **行号纪律核对（外部文档引用了这个文件的行号，所以必须守恒）**：
`apply_original_prov` 仍 **:312** · `pick_template` 仍 **:393** · `ensure_meta` 仍 **:431** · `verify` 仍 **:485** · `self_test` 仍 **:622** —— **全部与改前一致**（`判据/B114_R4_B93重建性.md:25,31` 与 `活状态表.md:338` 引的就是这几个行号）。

### 1·3 新增自检 `--check-fallback [--deep]`（跑完能自证「兜底档现在是 0」）

四步，全部只读（唯一写盘 = `tempfile.mkdtemp()` 的探针目录，跑完 `shutil.rmtree`）：

1. **兜底模板的非真值行 ↔ 现读众数**（按「相对目录 + 扩展名」分组，逐行比，**不看那 4 项**——它们本来就该被逐条改写）；
2. **`normalize` 现读值** + 兜底模板的 `normalize`，非 `0` 直接记红；
3. **「冷重建会写成什么」逐份推算 vs 现状**（纯内存，O(份数)）：冷重建第一份必走兜底、之后众数 = 第一份的产物，而 `apply_original_prov` **只改那 4 行** ⇒ 「兜底模板 + 真值改写」的产物**就是冷重建的稳态值**（精确，不是抽样近似）；
4. `--deep`：临时空目录里**真跑一次 `ensure_meta`**（每个扩展名连放 3 份：第 1 份走兜底、第 2/3 份走众数），端到端证明代码路径真走到了兜底。

**现读结果（全绿）**：

```
【① 兜底模板 非真值行 ↔ 现读众数】
  bgm / .ogg     25 份 · 众数 25   ✅ 非真值行一致
  music / .ogg    2 份 · 众数 2    ✅ 非真值行一致
  sfx / .ogg     10 份 · 众数 8    ✅ 非真值行一致
  sfx / .wav    612 份 · 众数 566  ✅ 非真值行一致
  vo / .ogg   1,785 份 · 众数 1,778 ✅ 非真值行一致
  vo / .wav     115 份 · 众数 115  ✅ 非真值行一致
【② normalize 现读值】0 × 2494 · 1 × 55 ；兜底模板：.ogg 0 · .wav 0
【③ 冷重建（纯内存推算）vs 现状：2549 份】
  冷重建后 normalize 分布：0 × 2549
  与现状逐字节一致 2494 份 · 不一致 55 份（差在 normalize 的：55 份）
【④ 端到端（--deep）】.ogg 第 1/2/3 份 ✅ 0 · .wav 第 1/2/3 份 ✅ 0
===== 兜底档 = normalize: 0，冷重建不漂 =====   rc = 0
```

⇒ **① 冷重建结果与现状一致（默认 0）**：满足 —— 2,494 / 2,549 份逐字节相同；剩下 55 份是**历史遗留的 `1`**，冷重建会把它们写成 `0`（方向是 `1 → 0`，即回到默认档），**不再是 `0 → 1` 的整树漂移**。

### 1·4 灭自证（证明这条自检不是恒绿）

把 `FALLBACK_TEMPLATES` 在内存里改回 `normalize: 1` 再跑同一个函数：

```
【① 】6 个分组全部 🔴 非真值行不一致（兜底 vs 众数 → normalize: 1 → 0）
【② 】兜底模板：.ogg 1 · .wav 1
【③ 】冷重建后 normalize 分布：1 × 2549 · 与现状一致 55 份 · 不一致 2,494 份
===== 8 条不过 =====   rc = 1
```

⇒ 这条自检**能抓到 `0 ↔ 1` 任何一种方向的漂移**，不是「只要文件在就绿」。

### 1·5 回归（改完复跑）

| 命令 | 结果 |
|---|---|
| `--verify`（`--dir` 指 `audio/` 根） | **0 红 / 0 黄**（与改前一致：音频 2,549 条 · guid 2,549 个 · 4 项不一致 0 · 无真值 0） |
| `--self-test` | **全过**（真值表 2,549 名字 · 管线 2,549/2,549 ×2 模板 · 覆盖率 2,549/2,549 有真值） |
| `--backfill --dry-run` | 音频 2,549 条 · **补 meta 0 条（已存在 2,549）** ⇒ 幂等路径未受影响 |
| `--probe "Hit Fire 1"` | 输出里 `normalize: 0`（原来 `1`） |
| `python -m py_compile` | OK |

`git diff --numstat`：**`232 / 10`**（新增块，不是整篇翻行尾）· 行尾 **纯 LF**（`CRLF 0 / LF 1,123 / 裸 CR 0`），与改前一致。

### 1·6 🟠 还开着的一条（**不在我的白名单里，交调度台**）

现状那 **55 份 `normalize: 1`**（48 in `sfx/` + 7 in `vo/`，清单见 §三·A）与其余 2,494 份**不一致**。`normalize: 1` 会让 Unity **在导入时做响度归一化**，`0` 不做 —— 这不是纯元数据差别，**那 55 条音在游戏里的音量与其它条不同**。原版走 FMOD，**没有这层归一化** ⇒ 更可能 `0` 才是对的口径（但 `normalize` 属 `UNPROVABLE`，⛔ 不能说「原版就是 0」）。

- **要不要改**：属于 `.meta` 编辑，**不在 W-10 的 ✅ 白名单**（白名单只给了 `_sfx_meta.py` / 新建脚本 / 报告），所以**本轮没动**。
- **怎么改（若批）**：`ensure_meta()` 是幂等的、**不会覆盖已存在的 meta**，所以不能用 `--backfill`；要另开一次性脚本把这 55 份的 `normalize:` 行就地改 `0`（**同物理行改**，行数不变）。

---

## §二 `B-241`：全量 Ogg 页结构普查（1,822 份，只读）

### 2·1 判据与量法（**先说一个指引偏差**）

简报说「与 `判据/B112W_B197_granule假说.md` **§5·2** 同一套判据」。⚠️ 现读那份文件：**§5·2 是「体积与字节差」**，**真正的量法在 §2·2「三个量怎么算」**（尤其 §2·2 第 2 条「页边界实解累计」）。我按 **§2·2** 做，并把 §5·2 的体积口径用作本轮的费用估算。**这是指引与判据不一致，不是我改了判据** —— 记在这里供调度台订正。

**两条等价量法，本轮都用了**：

| 量法 | 做法 | 代价 |
|---|---|---|
| **A · 真解码**（B197 §2·2 原法） | 截断到该页末尾 → granule 设 `1e7` → 打 EOS → 重算 CRC → **ffmpeg native Vorbis** 解码 → 输出样点数 | 每页一次解码，O(页数) 次进程；**只用于抽样交叉验证** |
| **B · 块长推算**（B197 §2·2·2 自己写的「与解码器无关」那个量） | 逐包读 `blockflag` → 页累计 `= Σ_{i≥1}(block[i-1]/4 + block[i]/4)` | 一次线性扫描，**全量 1,822 份只要 ~5 秒** |

**量法 B 要读 setup 头里的模式表**，本轮踩了两个坑（都已就地写进脚本注释，别重踩）：

1. 🔴 **Vorbis 的位打包是 LSB-first**（`oggpack_read` 从每字节 bit0 往上读）。写成 MSB-first 的症状是 **codebook sync 读出 `0x424356`（应 `0x564342`）** —— 同一个值按字节反过来，因为 `0x564342` 的 24 位按 LSB-first 打出来**正好是 `42 43 56`**。**判定法**：拿一份 ffmpeg+libvorbis 现编的参考文件一比就知道**不是文件坏了**。
2. 🔴 **音频包的第一个 bit 是「包类型」标志（必须 0），模式号在它后面**（`vorbis_synthesis`：`if(oggpack_read(opb,1)!=0) return OV_ENOTAUDIO;`）。从 bit0 直接读模式 ⇒ **每个包都读出「模式 0」**，整棵树都算成同一种块长。**判定法**：实测 3 个文件的全部音频包 **bit0 恒为 0**（`45/45`、`39/39`、`11,183/11,183`），而 bit1..bit7 都有分布 —— 恒定的那一位不可能是模式号。

**模式表不从 setup 头开头解**：它前面塞着 codebook / floor / residue / mapping 几大段，任何一处读歪都会静默漂（第一版照 spec 逐段解，**在 residue 段读歪**，读出 `residue_type = 7452`）。改成**从包尾反推**：`mode_count(6)+1` → `nmode × [blockflag(1)+window(16)+transform(16)+mapping(8)]` → `framing(1)`，用「整表读完正好落回包尾 + `windowtype/transformtype` 恒 0 + framing == 1」三条硬约束。反推常同时得到 `nmode=1` 与 `nmode=2` 两个解 ⇒ **判据 = 哪种读法让更多页的 granule 对得上**（本普查问的就是这个）。

**全树现读的结构（顺带核实的口径）**：1,822 / 1,822 份都是 **`blocksize_0 = 256` · `blocksize_1 = 2048` · `mode_count = 2` · `blockflags = (0, 1)`**，0 例外。

**末页为什么单独判**：EOS 会把最后一个长块的尾巴裁掉，**末页 granule 天然小于累计**（实测 `eos_trim` 范围 **0 – 1,023**，全 < `blocksize_1 = 2,048`）⇒ 末页判据 = **`granule == 原版真值（len × freq）`**，没有真值那一档才退到「`0 ≤ 累计 − granule < 2048`」。

### 2·2 交叉验证（量法 B vs 量法 A，抽样）

```
【ffmpeg 交叉验证】抽样 40 份 · 逐页核 86 处 · 一致 86 · 不一致 0
```

再加两条**外部**对账（都不是我自己算的）：

- 本轮扫出的 43 份坏文件与 `_tmp_view/b112w_b197/final_scan.json` 里 B197 记的 **C43 名单逐条相同**（交集 43 / 差集两侧都是空）；
- 抽一份与 B197 §三 的逐页表逐格比：`VO_BL_Ghallaron_gen2` B197 记 **page2 gran 31872 / 实解 173,888 / 总 177,881**，本轮量法 B 独立算出 **173,888** —— 一格不差。

### 2·3 读数（全量，不是抽样外推）

| 量 | 读数 |
|---|---|
| 扫的 `.ogg` | **1,822 份**（`vo` 1,785 · `bgm` 25 · `sfx` 10 · `music` 2），**解析失败 0** |
| 音频页总数 | **7,000**（页 0 = 标识头、页 1 = 注释+setup，**页 2.. 是音频页**） |
| **中间音频页**（= 唯一可能出缺陷的那类） | **5,178** |
| 只有 1 个音频页（结构上**不可能**有中间页缺陷） | **1,286 份** |
| 有中间页（**风险面**） | **536 份**（`vo` 500 · `bgm` 25 · `sfx` 9 · `music` 2） |
| **中间页 granule 不对的文件** | **43 份**（**全在 `vo/`**） |
| **末页 granule 不对的文件** | **0 份**（1,820 份对得上原版真值；另 2 份 `music/main_theme.ogg` · `music/menu_idle_theme.ogg` 在 `clip_index.json` 里没有真值，按 EOS 裁剪带判 **合规**） |

**页数分布**（全树）：3 页 **1,286** · 4 页 **47** · 5 页 **34** · 6 页 **124** · 7 页 **191** · 8 页 **70** · 9 页 **34** · 10 页 5 · 11 页 3 · 17 页 1 · **≥100 页 27**（`bgm` 25 + `music` 2，即全部 BGM/主题曲；最长 141 页）。

**那 43 份的共同形状**（43/43 一致，0 例外）：

| 特征 | 取值 |
|---|---|
| 目录 | 全部 `vo/` |
| 页数 | **一律 4 页**（= 2 个音频页） |
| 坏页位置 | **一律页 2**（每份**只有 1 个**坏页） |
| 坏页段数 | **一律 250 段** |
| 坏页 granule | **一律 `31,872`**（= `(250−1) × 128`，正是 B197 §2·3 那条 AssetRipper 公式） |
| 该页实解累计 | min **97,280** · 中位 **168,064** · max **215,104**（与 B197 §三 C43 那一行**逐项相同**） |
| 实解 < 31,872 的 | **0 / 43**（所以每一份都真的写小了一整段） |
| 末页 granule | 43/43 **都 == 原版真值** ⇒ 缺陷只在中间页 |

⚠️ **反向提醒**：`granule == 31,872` **本身不是缺陷标记** —— 全树有 **44** 个中间页写 `31,872`，其中 **1 个是对的**（`vo/VO_Goff_Deff Dread - Iz stuck inside dis ting an dat makes me MAD.ogg` 页 2，段数 155，实解**正好** 31,872）。判据必须是「**granule == 实解累计**」，⛔ 别按「值等于 31,872」去筛。

### 2·4 结论：**要修多少份**（把这句话写死成数字）

| 若 `B-204` 的结论是… | 要修 |
|---|---|
| **FMOD 看「中间页」granule**（像 vgmstream / libsndfile） | **43 份 / 43 个页**（全在 `vo/`）—— **不是 450 份、更不是 1,292 份**；修法 = 只改 granule + 重算 CRC，**文件大小不变**，实测**每份只动 6–7 个字节、合计 295 字节**（分布：6 字节 × 6 份 · 7 字节 × 37 份） |
| **FMOD 只看「末页」granule**（像 ffmpeg native） | **0 份**（1,822/1,822 的末页 granule 都对） |

📌 **对 `B-241` 那句「约 1,292 份从没扫过」的直接回答**：已全部扫完。**除已知的 C43 之外，本轮没有扫出任何新的坏文件**（43 − 43 = 0）。⇒ B-241 那条「若 B-204 落到结论②，修的范围必须按全量重扫」**已经做完了**，答案就是上面的 43。

📌 **对 `B-197` / `B-204` 的旁证**：现盘上那 450 份 **q8/q10 重编码版**（第四波产物）的中间页 granule **全对**（它们不在 43 里）⇒ 这 450 份就算 FMOD 看 granule 也**不需要再修**；它们的问题只剩「**+24.73 MB 是不是白花**」（B-197 §五 已算过：只改 granule 那版在三路解码器上都是 `corr = 1.000000`，而 q8 版是 0.9990–0.9999）。

### 2·5 逐份清单（43 份 · `gran` = 页头写的 · `cum` = 实算累计 · 差 = `cum − gran`）

| 文件（`Resources/Art/audio/` 下） | 页数 | 坏页 | gran | cum | 差 |
|---|---|---|---|---|---|
| `vo/Sautekh_Orikan_vsAbaddon.ogg` | 4 | 2 | 31,872 | 194,496 | 162,624 |
| `vo/Sautekh_Orikan_wp.ogg` | 4 | 2 | 31,872 | 156,864 | 124,992 |
| `vo/Tutorial1_prebattle1_Ventris.ogg` | 4 | 2 | 31,872 | 138,048 | 106,176 |
| `vo/Tutorial1_turn201_Calgar.ogg` | 4 | 2 | 31,872 | 132,672 | 100,800 |
| `vo/Tutorial2_turn205_Ghazghkull.ogg` | 4 | 2 | 31,872 | 185,984 | 154,112 |
| `vo/VO_ADS_Battle Sister Superior - Bolters ready Sisters No mercy for the faithless.ogg` | 4 | 2 | 31,872 | 172,096 | 140,224 |
| `vo/VO_ADS_Celestian Sacresant Aveline_Backup4.ogg` | 4 | 2 | 31,872 | 177,472 | 145,600 |
| `vo/VO_ADS_Celestian Sacresant Aveline_gen2.ogg` | 4 | 2 | 31,872 | 215,104 | 183,232 |
| `vo/VO_ADS_Celestian Sacresant Aveline_vsMorvenn.ogg` | 4 | 2 | 31,872 | 164,480 | 132,608 |
| `vo/VO_ADS_Celestian Superior - I am the light to your darkness.ogg` | 4 | 2 | 31,872 | 174,784 | 142,912 |
| `vo/VO_ADS_Erika Luminas_concede.ogg` | 4 | 2 | 31,872 | 154,176 | 122,304 |
| `vo/VO_ADS_Junith Eruita_Backup2.ogg` | 4 | 2 | 31,872 | 185,088 | 153,216 |
| `vo/VO_ADS_Junith Eruita_concede.ogg` | 4 | 2 | 31,872 | 149,696 | 117,824 |
| `vo/VO_ADS_Junith Eruita_gen7.ogg` | 4 | 2 | 31,872 | 185,088 | 153,216 |
| `vo/VO_ADS_Morvenn Vahl_gen3.ogg` | 4 | 2 | 31,872 | 181,056 | 149,184 |
| `vo/VO_ADS_Morvenn Vahl_vsShadowsun.ogg` | 4 | 2 | 31,872 | 202,560 | 170,688 |
| `vo/VO_ADS_Saint Celestine - Believe in the Emperor of Mankind and you shall walk in His light.ogg` | 4 | 2 | 31,872 | 204,352 | 172,480 |
| `vo/VO_AM_Hektor Thenmann_Backup5.ogg` | 4 | 2 | 31,872 | 148,800 | 116,928 |
| `vo/VO_AM_Hektor Thenmann_gen4.ogg` | 4 | 2 | 31,872 | 148,800 | 116,928 |
| `vo/VO_AM_Hektor Thenmann_wp.ogg` | 4 | 2 | 31,872 | 134,016 | 102,144 |
| `vo/VO_AM_Medic Scion -On your feet! If you can breathe you can fight.ogg` | 4 | 2 | 31,872 | 128,192 | 96,320 |
| `vo/VO_BL_Ghallaron_gen2.ogg` | 4 | 2 | 31,872 | 173,888 | 142,016 |
| `vo/VO_BL_Ghallaron_greet.ogg` | 4 | 2 | 31,872 | 167,616 | 135,744 |
| `vo/VO_BL_Ghallaron_threat.ogg` | 4 | 2 | 31,872 | 201,664 | 169,792 |
| `vo/VO_BlackLegion_Abaddon_vsGrukk.ogg` | 4 | 2 | 31,872 | 195,392 | 163,520 |
| `vo/VO_CSM_EC_Daemon Prince Xarahan_concede.ogg` | 4 | 2 | 31,872 | 203,456 | 171,584 |
| `vo/VO_CSM_EC_Lucius_gen5.ogg` | 4 | 2 | 31,872 | 168,064 | 136,192 |
| `vo/VO_DA_Azrael_gen7.ogg` | 4 | 2 | 31,872 | 101,312 | 69,440 |
| `vo/VO_GSC_Saffa Rhiannor_greet.ogg` | 4 | 2 | 31,872 | 165,824 | 133,952 |
| `vo/VO_Goff_Deffgun Loota - Shootin an lootin in dat order.ogg` | 4 | 2 | 31,872 | 175,680 | 143,808 |
| `vo/VO_Goff_Ghazghkull_Concede.ogg` | 4 | 2 | 31,872 | 150,592 | 118,720 |
| `vo/VO_Goff_Zagstruk_gen6.ogg` | 4 | 2 | 31,872 | 185,536 | 153,664 |
| `vo/VO_Ork_Goff_Gordrang_vsOMaisos.ogg` | 4 | 2 | 31,872 | 179,264 | 147,392 |
| `vo/VO_Ork_Goff_Stikkboy - No time for finkin' only time for krumpin'.ogg` | 4 | 2 | 31,872 | 182,848 | 150,976 |
| `vo/VO_SaimHann_Zephyrblade_gen1.ogg` | 4 | 2 | 31,872 | 175,680 | 143,808 |
| `vo/VO_SaimHann_Zephyrblade_vsKeltoc.ogg` | 4 | 2 | 31,872 | 136,256 | 104,384 |
| `vo/VO_Sautekh_Canoptek Wraith - rustle.ogg` | 4 | 2 | 31,872 | 97,280 | 65,408 |
| `vo/VO_SpaceWolves_Iron Priest - Sharp senses, sharper steel.ogg` | 4 | 2 | 31,872 | 114,752 | 82,880 |
| `vo/VO_SpaceWolves_Njal Stormcaller_Backup2.ogg` | 4 | 2 | 31,872 | 134,912 | 103,040 |
| `vo/VO_SpaceWolves_Njal Stormcaller_concede.ogg` | 4 | 2 | 31,872 | 114,752 | 82,880 |
| `vo/VO_Tau_Empire_Aun'va_mirror.ogg` | 4 | 2 | 31,872 | 114,304 | 82,432 |
| `vo/VO_Tau_Empire_Shadowsun_vsCalgar.ogg` | 4 | 2 | 31,872 | 155,072 | 123,200 |
| `vo/VO_UM_Chaplain Letharius_wp.ogg` | 4 | 2 | 31,872 | 183,744 | 151,872 |

### 2·6 复跑方式

```bash
# 全量（~5 秒，纯只读；唯一写盘 = --out 的 JSON）
python -I -X utf8 工具/audio_ogg_page_census.py --out <json> 
# 带 ffmpeg 真解码交叉验证（抽样 N 份，会写 tempfile 临时目录、跑完删）
python -I -X utf8 工具/audio_ogg_page_census.py --out <json> --validate-ffmpeg 40
# 只扫子目录 / 冒烟
... --only vo/ --limit 200
```
⛔ **不带 `--out` 只打用法、什么都不做**（本项目有过 `--help` 误触真写入的先例）。
工具默认的 ffmpeg 路径 = `D:/4/_tmp_view/b112_r1w/tools/ffmpeg/ffmpeg.exe`（B197 留下的那份 7.1，找到才跑验证，找不到就跳过并出声）。

---

## 没查清的部分

1. 🔴 **`B-204` 那条读数（FMOD/Unity 看不看中间页 granule）本轮没测** —— 红线是不跑 Unity。§2·4 的两个数字（43 / 0）是**两个分支各自的答案**，不是结论。**⛔ 不许拿本报告当「43 份一定得修」的依据。**
2. **`clip_index.json` 里没有 `music/main_theme.ogg` / `music/menu_idle_theme.ogg` 的真值**（2,549 条里没有这两个名字 —— 它们是我们自己命名的 BGM）。它们的末页 granule 只能按「EOS 裁剪带」判（`0 ≤ 累计 − granule < 2048`），现读**合规**，但**没有原版真值背书**。
3. **`normalize` / `quality` / `sampleRateOverride` 依旧没有原版真值**（`B111_T2` §B-25 已证原版 typetree 无这三个字段）⇒ 本报告 §一 只是「让兜底与现状一致」，**不是「原版就是这样」**。
4. **`sfx/ogg` 与 `vo/ogg` 的非真值行完全一致**（本轮现扫：6 个「目录 × 扩展名」分组的非真值行**只有一种**），所以一个按扩展名的兜底模板够用。⚠️ 但这条**只在当前树成立** —— 若将来某个目录出现不同的非真值配置（例：有人手工关掉 `forceToMono`），兜底会退回陈旧值。`--check-fallback` ① 就是为这个准备的（它会按分组点名）。

---

## 顺手发现

**A. 那 55 份 `normalize: 1` 是「真」的不一致，不只是元数据洁癖** —— `normalize: 1` = Unity **导入时做响度归一化**，`0` = 不做。那 55 条音在游戏里的音量与其余 2,494 条**不同**。清单（现读）：

- `sfx/` 48 份：`Aeldari Reinforcement drop 2 jingle.ogg` · `Ork expansion pop up jingle.ogg`（这 2 份 `.ogg`）＋ 46 份 `.wav`：`Airstrike Gatling Short` `battle_turn_end_timer_3` `Blast` `Bolter_1shot_nurgle` `Bolter_4shot` `Camouflage` `Concussive` `CreateCard_4` `Dark Angels Void Combat` `Electric Cut 1` `Electric Cut 2` `Exorcist 1..4` `Exorcist start` `FlamerSweep` `Forge Collect` `Front Line` `Gauss_Attack` `Mulligan Replace` `Ork Shoota Long Low` `Orks Harpoon Shot Huh` `Plasma Explosion 2` `Pulse Explosion` `Rally` `Relentless` `Remove card from deck` `Resolution` `Shoota_5shot` `Stealth` `TacticalInsight` `Talent_Hexscorn` `Tide` `TideOfMuscle_2` `UI Click 2` `UI Click 3 Bip` `UI Click 4 Bip` `UI Click 5` `UI Click 5_no delay` `UI Click Cancel Bip` `UI Click Card 4 cancel` `UI Click Confirm 2` `UI Click Confirm 3` `UI Click Wrong` `UI Purchase 1`
- `vo/` 7 份：`VO_BL_Ghallaron_Backup1 (vsAeldari)` · `VO_CW_SaimHann_Extra10 - The wrath of Khaine grows within me` · `VO_Goff_TBC - Deff on wheelz` · `VO_Sautekh_TBC - I cannot be destroyed` · `VO_Sautekh_TBC - Retreat is not the Necron way` · `VO_Sautekh_TBC - The Sautekh will triumph` · `VO_UM_TBC1 - For the Emperor`

⇒ **建议**：批一次把它们改成 `0`（同物理行改，行数不变），口径与其余 2,494 份对齐。**本轮没动**（不在白名单）。

**B. `--verify` 的「例外放行」把一整类漂移吞掉了** —— `verify()` ④ 段对有真值的文件只看那 4 项，非真值字段（`normalize` / `quality` / `sampleRateOverride` / 结构行）**一律不比**。这正是 `B-238` 挂了一轮没人发现的原因，也是那 55 份「连黄都不报」的原因。✅ 已用 `--check-fallback` 补上这一档（只比非真值行）；**但 `verify()` 本身没改**（改它会动 `:485-616` 一大段，且 `判据/B114_R4` 引了这个行号区），留作后续。

**C. `.ogg` 兜底模板里 `loadType: 2` / `preloadAudioData: 1` 与现读众数（`1` / `0`）不一致** —— 这两项**是 `PROV_FIELDS`**，写盘前一定被原版真值改写（现读 2,549/2,549 都有真值 ⇒ 现在**永远轮不到兜底值**）。⚠️ 但若将来加一条**真值表里没有**的音，`.ogg` 会拿到 `loadType 2 / preload 1`（sfx 时代那套），而 `stderr` 会出声（`_prov_note`）。**没改**：改它等于改 4 项的行为，超出本活范围，且当前**零可观测差异**。

**D. 那 43 份的缺陷「只错中间页、末页全对」再次印证 B197 的根因** —— 43/43 的坏页段数恒为 250、granule 恒为 `(250−1)×128 = 31,872`、且是**非累计**写法（5 页文件第 3 页也写 31,872 那条在 B197 §2·3 已证）。本轮全量扫（5,178 个中间页）**没有再发现第二种形状**的缺陷。

**E. 复核工具口径**：`_sfx_meta.py` 里 `SFX_DIR` 默认只指 `sfx/`，而 `--verify` / `--self-test` / `--check-fallback` 都吃 `--dir`。跑全树音频请显式给
`--dir d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio`（本报告所有读数都是这么跑的）。
