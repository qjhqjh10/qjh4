# B141 · W130 · ArtBaker 三处修复

> 执行写手 `B141-W1` · 会话 128 · 2026-11-04
> 白名单：**只改了** `MyGame/Assets/CardPresentation/Editor/ArtBaker.cs`（+ 本报告）。
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没碰 `d:/2` · ⛔ 没碰账 / 活状态表 / 所有权表 / `ArenaBuilder.cs` / 任何 `.meta`。

## 0 · 一句话结果

三处都改完：**vfx 真值表改按表头认列**（2,984 行全部改对、arena 表 0 行变化）· **下划线命名能对回原版名**（`Card_Booster_Pack_Normal` 的 `textureType` 不再被打回 0）· **15-10 压缩档真值表落进 `ApplyArenaImportSettings`/`ApplyImportSettings`**（8 个原版名 / 39 个文件）。`typecheck` = **运行时 0 / 编辑器 0**。

---

## 1 · 修复一：两张真值表列序不同，vfx 行被按 arena 的偏移读

**判据**（现读表头，`cat -A` 逐列确认）：

| 表 | 表头（`\t` 分隔） | `wrapU` 下标 |
|---|---|---|
| `数据/游戏数据/vfx_texture_mips.tsv` | `name,width,height,mipCount,bundle,wrapU,wrapV,filterMode,aniso,npotScale` | **5** |
| `数据/游戏数据/arena_texture_import.tsv` | `name,width,height,mipCount,colorSpace,bundle,wrapU,wrapV,filterMode,aniso` | **6** |

**改法**：`LoadArtTruth` 不再用固定偏移 `6/7/8/9`，改成**读第一行表头、按列名取下标**（`HeaderIndex`）；表头认不出列 ⇒ **整张表跳过并出声**（⛔ 不退回固定偏移去猜）。`mipCount` 也一样改成按表头取（两张表都在 3，但不再依赖这个巧合）。

**改前 / 改后（vfx 表真实行，`(wrapU, wrapV, filterMode, aniso)`）**：

| 原版名 | mipCount | 改前读成 | 改后读成 | 说明 |
|---|---|---|---|---|
| `Shine trail` | 1 | **(1, 1, 1, -1)** | **(0, 1, 1, 1)** | 原版是 U=Repeat/V=Clamp 的**混合档**，改前被读成 `(1,1)` = 全 Clamp —— 正是「布尔白名单表达不了 U≠V」那条注释想表达的档位，实际**从没读对过** |
| `Noise Combined` | 1 | (0, 1, 1, -1) | **(0, 0, 1, 1)** | 改前 `wrapV` 读到 `filterMode` 列（1）⇒ 被写成 Clamp；`aniso` 读到 `npotScale`（-1）⇒ `HasFilter` 判据缺一半 |
| `Card Booster Pack Normal` | 9 | (0, 1, 1, -1) | **(0, 0, 1, 1)** | 同上 |
| `Laser_Wave_2` / `LightningTrail` / `Card Damage` | 9/8/9 | (0, 1, 1, -1) | **(0, 0, 1, 1)** | 同上 |

**影响面（逐行现算）**：
- `vfx_texture_mips.tsv`：**2,984 行全部**解析结果变化（`mipCount` 不变，wrap/filter/aniso 三格变）。
- `arena_texture_import.tsv`：**0 行变化**（改前改后逐行相同）—— 证明修复精确作用在 vfx 那一侧，没有把 arena 表改坏。
- 受影响的实际贴图 = 「只在 vfx 表命中、arena 表没覆盖」的那批 `Resources/Art` 贴图（arena 表的同名行仍以 arena 为准，顺序未变）。

> ⚠️ 同一份错描述也写在代码注释里（原 `:737`「两张表列序在要用的五格上一致」），**已就地订正**（铁律 5）。

---

## 2 · 修复二：`Card_Booster_Pack_Normal` 下划线命名匹配不上原版名

**改法**：`OriginalNameCandidates` 在原有候选（原样 → 剥 `_sprite`/`_0`/`_1`）之后补一档 **「下划线 → 空格」** 形态（精确名仍排最前，不改变已有命中）。

**改前 / 改后（`OriginalTextureType` 返回值）**：
- 文件：`Assets/CardPresentation/Resources/Art/boosterpack/Card_Booster_Pack_Normal.png`（`.meta:57` 现读 `textureType: 1`）
- 改前：候选 = `{Card_Booster_Pack_Normal}` ⇒ 13 个原版名全是**带空格**的 ⇒ 不命中 ⇒ `IsOriginalNormalMap=false` ⇒ **返回 `Default`(0)**（下次 `BakeAll` 会把 `1` 静默打回 `0`）
- 改后：候选 = `{Card_Booster_Pack_Normal, Card Booster Pack Normal}` ⇒ 命中 `NormalMapNames[0]` ⇒ **返回 `current` = 1**（保留现值）✅

**副作用（已逐张现算，不是估的）**：`Resources/Art` 下另有 **47 个**文件因此**首次**对回原版真值（下划线 ↔ 空格，逐个同名同物，无一对像误配）：
- **35 个**：原版 mip/wrap/filter/aniso 与盘上现值**完全一致** ⇒ 下次烘焙**无变化**（这批反过来证明配对是对的）。
- **12 个**会真的变：

| 文件 | 原版名 | 下次烘焙的变化 |
|---|---|---|
| `boosterpack/Booster_Pack_Explosion.png` 及 `_Necron` / `_SW_1` / `_Tau` / `_Tyranid` / `Explosions_Sororitas`（6 张） | `Booster Pack Explosion…` | mip **0→1** · wrap **1,1→0,0** |
| `boosterpack/Matcap_Booster_Pack.png` | `Matcap Booster Pack` | ~~mip **0→1** · wrap **1,1→0,0**~~ ⚠️ **2026-11-05 `B143-R1` 现读：盘上已是目标值**（`enableMipMap: 1` / `wrapU: 0` / `wrapV: 0`）⇒ 本条「下次烘焙会变」**已过期**，实际无变化 |
| `boosterpack/Card_Booster_Pack_Normal.png` | `Card Booster Pack Normal` | wrap **1,1→0,0**（mip 已是 1） |
| `ui_menu/Card_Ready_For_Level_Up.png` | `Card Ready For Level Up` | wrap **1,1→0,0**（⚠️ 2026-11-05 `B143-R1` 补：**mip 目标是 0 不是 1**，盘上现值 `enableMipMap: 0` 已对，别再按「mip 0→1」改它） |
| `ui_menu/UI_Dirt_And_Noise_skratches.png` | `UI Dirt And Noise skratches` | wrap **1,1→0,0** |
| `ui_menu/Up_Rays.png` | `Up Rays` | wrap **1,1→0,0** |
| `ui_menu/Shine_trail.png` | `Shine trail` | wrap **1,1→0,1** |

> `Shine_trail.png` 这条是**交叉验证**：同物异名的 `vfx/Shine trail.png.meta` 早就是 `(0,1)`（原版真值），下划线那一份却停在 `(1,1)` —— 正是这处命名匹配的缺口。

---

## 3 · 修复三：15-10 战场贴图压缩档（原版比我们「低档」的那批）

**原状**：`ApplyArenaImportSettings` **无条件**写 `CompressedHQ`（BC7）⇒ 原版是 **RGBA32** 或 **DXT1** 的贴图被压成 BC7，比原版低档。

**判据**：原版 `Texture2D.m_TextureFormat`（UnityPy 直读 84 个 `StandaloneWindows64/*.bundle`；本轮用 `_tmp_view/b139_i/orig_textures.json` 缓存件，3,009 对象 / 2,984 唯一名，与 `报告/B136_审计_纹理与图集.md` #11 同口径）+ `判据/B141_W130_对账_资源.md` §一 15-10。

**精确名单是现算的、不是猜的**：把 `WarpforgeArena1/arenas/**/Textures/*.png` 的 **116 个唯一名 100% 对回** `orig_textures.json`（零未命中），原版格式分布 = BC7 25 · DXT5 61 · DXT5Crunched 22 · **DXT1 3 · RGBA32 5**。⇒ 需要修的恰好这 **8 个名字**：

| 原版名 | 原版 fmt | 原版尺寸/mip | 导入档（新） | 文件实例 |
|---|---|---|---|---|
| `Noise Combined` | 4 = RGBA32 | 128²·1 | `Uncompressed` | **14**（13 战场 + `Resources/Art/ui_menu/Noise_Combined.png`） |
| `NoiseNRM` | 4 = RGBA32 | 128²·8 | `Uncompressed` | 3 |
| `Default-Particle` | 4 = RGBA32 | 64²·7 | `Uncompressed` | 4 |
| `MuzzleFlash_Side` | 4 = RGBA32 | 64²·7 | `Uncompressed` | 1 |
| `Matcap Gold Emperos Children` | 4 = RGBA32 | 1024²·11 | `Uncompressed` | 1 |
| `CandleFlame` | 10 = DXT1 | 512²·10 | `Compressed` + 显式平台 `DXT1` | 1 |
| `Flame01` | 10 = DXT1 | 2048²·12 | `Compressed` + 显式平台 `DXT1` | 2 |
| `MatCap Polish` | 10 = DXT1 | 128²·1 | `Compressed` + 显式平台 `DXT1` | 13 |

合计 **39 个文件**（战场 38 + `Resources/Art` 1）。**α 通道已逐张核**（PIL 读 PNG）：
`Noise Combined` alpha **0..196**（必须 Uncompressed，DXT 会吃掉渐变 alpha）· `Default-Particle` 0..255 · `MuzzleFlash_Side` 0..255 · `NoiseNRM` / `Matcap Gold Emperos Children` / `CandleFlame` / `Flame01` / `MatCap Polish` 的 alpha **恒 255**（DXT1 安全）。
**不改的**：原版 BC7（25 名 / 65 实例）—— 现状 `CompressedHQ` 已同档；原版 DXT5 / DXT5Crunched（83 名 / 123 实例）—— 我们给的是 BC7，**比原版高**（`B136` 判为「更高档、不是缺陷」）⇒ 见 §6 待裁。

**落点**：`ArtBaker.cs` 新增 `CompTruths`（8 条）+ `OriginalCompression()` + `ApplyCompressionTruth()`；`ApplyArenaImportSettings` 的顶层与 **Standalone 平台块**都改成 `ctruth.Compression`（平台块原来写死 `CompressedHQ`，会盖掉顶层），DXT1 三条额外写 `ps.format = DXT1` + `ps.overridden = true`；`ApplyImportSettings` 也接同一张表（只影响 `ui_menu/Noise_Combined.png`）。

> **关于「❓精确名单」**：`B141` R9 把 15-10 的精确名单记为 ❓（「本轮未重跑逐张映射」）。**本轮把它查出来了**——
> 不需要重跑 UnityPy：`B139_I` 于 2026-11-04 已经做过同口径扫描并留下缓存件 `_tmp_view/b139_i/orig_textures.json`
> （报告 `报告/B139_I_纹理导入.md` 首段自陈「本批现读，不是抄 B136」，3,009 对象 / 2,984 唯一名）。
> 用它把战场那 116 个唯一名**100% 对回**（零未命中）⇒ **15-10 不再有 ❓ 名字**。
> ⚠️ 但**张数**仍有矛盾（审计写 32、现算 38 个实例）⇒ 见 §6·3，那条仍是 ❓。


---

## 4 · 改动清单（供账闸 / 复核）

文件：`Unity/MyGame/Assets/CardPresentation/Editor/ArtBaker.cs`
**行数：876 → 991（+115；`git diff --numstat` = +123/−8）**，行尾仍是 **CRLF 991 / 裸 CR 0**。

| 位置（改后行号） | 改了什么 |
|---|---|
| `:530-533` | `ApplyImportSettings` 末尾接 `ApplyCompressionTruth`（表外不动） |
| `:633-636` | `ApplyArenaImportSettings` 顶层压缩档改 `OriginalCompression` |
| `:643-644`（platform 块） | `ps.textureCompression = ctruth.Compression` + 可选 `ps.format`（DXT1） |
| `:786-800` | `OriginalNameCandidates` 补「下划线→空格」候选 |
| `:820-844` | `LoadArtTruth` 按表头认列（`HeaderIndex`），表头认不出 ⇒ 整表跳过并出声 |
| `:739-744`（注释） | 就地订正「两张表列序一致」那条**错描述**（铁律 5） |
| `:920-990`（新增，类尾） | `ArtCompressionTruth` / `CompTruths` / `OriginalCompression` / `ApplyCompressionTruth` / `HeaderIndex` |

> ✅ 账不受行号漂移影响：`grep "ArtBaker.cs:" 账/*.tsv` = **0 条**（本文件没有被账按行号引用）。
> 新成员全部追加在**类尾**（`:920` 之后），没有插进既有成员之间。

标记：三处改动都打了 `// ✅ 已复刻（2026-11-04 · B141 · <判据出处>）`（`:530` `:633` `:786` `:820`）。

---

## 5 · 验证

| 项 | 读数 / 证据 |
|---|---|
| `cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w1 bash 工具/typecheck.sh` | **运行时错误数: 0 · 编辑器错误数: 0**（独立 TMPDIR，无旧 DLL 干扰） |
| #1 改前/改后 | §1 那张表（用真实 TSV 行跑同一套解析逻辑逐行比；vfx 2,984 行全变 · arena 0 行变） |
| #2 改前/改后 | §2（`IsOriginalNormalMap` false→true；`OriginalTextureType` `Default(0)` → `current(1)`） |
| #3 名称匹配 | §3（8 个名字全部命中 39 个文件；按 `OriginalNameCandidates` 同一套逻辑现算） |
| 行尾 | CRLF 991 / `\n` 991 / 裸 CR 0 |

---

## 6 · 还差什么 / ❓

1. **必须一次 Unity 才能验收（⛔ 本轮没跑）**：
   - #3 的 `Uncompressed` / `DXT1` 只在下次 `BakeFromCLI`（或单独跑 `ApplyArenaImportSettings`）后落到 `.meta`；`DXT1` 那条是**显式平台格式**，`TextureImporterFormat.DXT1` 会不会被 Unity 采纳需要复跑后读 `.meta` 的 `platformSettings.textureFormat`（期望 10）确认 —— 若 Unity 忽略，退路是 `textureCompression: 1` + `allowsAlphaSplitting`（但**还没试过，⛔ 不当结论**）。
   - #2 的副作用（§2 那 12 张）同样只在下次烘焙时生效。
2. **断言没加**：白名单只有 `ArtBaker.cs` + 本报告 ⇒ 没新建 `B141_W1_Assert`。建议调度台派一个「断言写手」补三条：
   ① `LoadArtTruth` 对 `Shine trail` 读出 `(0,1)`、对 `Noise Combined` 读出 `(0,0)`（**结构上不可能同时满足**的判别式，防「按表改回去」）；
   ② `OriginalTextureType("Card_Booster_Pack_Normal", 1) == 1`；
   ③ `OriginalCompression` 对 8 个名字返回期望档、对 `ExplosionEmission` 返回 `CompressedHQ`。
   （现有 `B111_R2W_Assert` 只反射 `RepeatTextures` 名单，**没有覆盖这三条**；`RepeatTextures` 本轮未改。）
3. **15-10 的计数与审计报告对不上，需要复核（不是本轮能定）**：
   `B136` #11 / `B141` R9 写「32 张低档（uncomp 18 / BC7 11 / DXT1 13）」，但按 `orig_textures.json` 现算（226 个文件实例、116/116 唯一名全命中）是 **uncomp 22 + DXT1 16 = 38 个实例**、BC7 是 **65** 不是 11。
   ⇒ 怀疑 `B136` 那一格是按「231/283 张按名对回」的子集统计的（漏配的多），**不是我这边的数错**；建议复核后回填 `B136`/`B138` 的数字（本轮白名单不含那两份报告）。
4. **原版 DXT5（96 实例）/ DXT5Crunched（27 实例）我们仍给 BC7**：`B136` 判「更高档、不是缺陷」，本轮**没改**。若用户口径是「连更高档也要逐字照原版」，那需要再开一条（改 83 个名字的显式 `DXT5` / `DXT5Crunched`）—— ⛔ 本轮不自己拍这个口径。

---

## 7 · 顺手发现（⛔ 未改，白名单之外）

1. **`vfx/Shine trail.png` 与 `ui_menu/Shine_trail.png` 是同一原版贴图的两种命名**，前者 `.meta` 已是原版 `(0,1)`、后者 `(1,1)`（改后下次烘培会对齐）。`Assets/WarpforgeVFX/ArenaOriginalPrefabs/.../Shine trail.png`（第三份）不在 `ArtBaker` 扫描范围，**一直是 `(0,1)`** —— 三份现在口径一致。
2. **`OriginalCompression` 用的是「原版名」匹配，不是路径** —— 将来若把 `Flame01` / `MatCap Polish` 之类再拷到别的目录（如 `Resources/Art/vfx/`），会**自动**拿到同样的压缩档，不需要再加规则。
3. **`ApplyArenaImportSettings` 对每张 arena 贴图都写 `ps.overridden = true`**（既有行为，本轮未动）：现读 226 份 `.meta` 的平台块 `overridden: 0` ⇒ 下次跑这个方法会把它们全改成 `1`，是一次**大范围 `.meta` 变更**（不是本轮引入的，但会与 #3 同批出现，验收时别误判）。
