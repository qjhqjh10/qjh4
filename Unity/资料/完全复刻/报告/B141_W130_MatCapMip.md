# B141_W130 · 3 张 MatCap 开 mip

写手 `B141-W63` · 第一百二十八会话 · 2026-11-04

## 一、目标

W61 报告（`报告/B141_W130_MipsCanon.md` §顺手发现，:133-136）现读：
`Resources/Art/card3d/MatCap_{Bronze,Gold,Silver}.png.meta` 的**原版 `mipCount = 8`**，
而我们 `enableMipMap: 0` ⇒ 按铁律 11（照原版）改成 **1**。

## 二、判据

- `数据/游戏数据/vfx_texture_mips.tsv`（表头：`name width height mipCount bundle wrapU wrapV filterMode aniso npotScale`）：

| 行号 | name | width×height | mipCount | bundle |
|---|---|---|---|---|
| 1909 | `Matcap Bronze` | 128×128 | **8** | `battlesharedresources_assets_all.bundle` |
| 1910 | `Matcap Gold` | 128×128 | **8** | `battlesharedresources_assets_all.bundle` |
| 1913 | `Matcap Silver` | 128×128 | **8** | `battlesharedresources_assets_all.bundle` |

⇒ `mipCount = 8` 与 128² 自洽（128/64/32/16/8/4/2/1 = 8 级）⇒ **原版开 mip**。

- 落到我们文件的对应关系：`ArtBaker.TryFindTruth` 第三档 `CanonName`（去 `_` 与空格、忽略大小写）
  ⇒ `MatCap_Bronze` → `MatCapBronze` = `Matcap Bronze` → `MatcapBronze` ✅
  （`Editor/ArtBaker.cs:867-882`、`:1156`）。所以 `ArtBaker.OriginalMipEnabled`（`:911-917`）
  对这三张本来就会写 `true` —— **本次改的是 `.meta` 现值，与烘焙口径一致，不会两头打架**。

- 上下文（同一张表，说明「不是全池一刀切」）：
  `MatCap Card Level 1`（:1905）`mipCount = 1` ⇒ **本来就该关**，我们现值 `enableMipMap: 0` **正确，不动**；
  `MatCap Polish`（:1907）`mipCount = 1` ⇒ 同样不动。

## 三、逐张：原版真值 vs 改前 vs 改后

| 贴图（`Resources/Art/card3d/`） | 原版名（tsv 行） | 原版 mipCount | 改前 `enableMipMap` | 改后 | 改前 md5 | 改后 md5 |
|---|---|---|---|---|---|---|
| `MatCap_Bronze.png.meta` | `Matcap Bronze`（:1909） | 8 | **0** | **1** | `f287164d4782742fb66211b78866b20b` | `24d1d07760c9ca301e32261bba3e430e` |
| `MatCap_Gold.png.meta` | `Matcap Gold`（:1910） | 8 | **0** | **1** | `a0552c8fbffd03503f92c0da9e33284c` | `52af8c78bf74dde46c7e970c458b9782` |
| `MatCap_Silver.png.meta` | `Matcap Silver`（:1913） | 8 | **0** | **1** | `6b01815cb75cabafe3a071235ba8e7c4` | `b4612403450ac3e8323bef959fae29e6` |

改动行 = **每个文件第 9 行**：`    enableMipMap: 0` → `    enableMipMap: 1`（缩进 4 空格原样）。

## 四、验证（二进制读回）

三份文件**逐个**做「改前副本 vs 改后」逐行 + 逐字节比对：

| 检查项 | Bronze | Gold | Silver |
|---|---|---|---|
| 文件字节数（改前 → 改后） | 2652 → **2652** | 2652 → **2652** | 2652 → **2652** |
| 行数（`count('\n')`） | 117 → **117** | 117 → **117** | 117 → **117** |
| 行尾（CRLF / 裸 CR） | 0 / **0** | 0 / **0** | 0 / **0** |
| 逐行差异 | **仅第 9 行** | **仅第 9 行** | **仅第 9 行** |
| `enableMipMap: 1` 出现次数 | 1 | 1 | 1 |
| `enableMipMap: 0` 残留 | **0** | **0** | **0** |

⇒ **零其它键变化、零行数变化、零行尾漂移**（`Edit` 工具原样写入，未用 `sed -i` / python 文本写）。

改后 mip 相关键现值（三份完全一致，均与改前一致，只有 `enableMipMap` 变）：
`mipMapMode: 0` · `enableMipMap: 1` · `borderMipMap: 0` · `mipMapsPreserveCoverage: 0` ·
`mipMapFadeDistanceStart: 1` · `mipMapFadeDistanceEnd: 3` · `streamingMipmaps: 0` ·
`streamingMipmapsPriority: 0` · `ignoreMipmapLimit: 0` · `mipmapLimitGroupName:` （空）

**为什么 `mipMapMode` 保持 0**：真值表**只有 `mipCount` 一列**，没有 `mipMapMode` 的判据；
且项目里已按原版开 mip 的贴图就是这个组合（例 `CardPresentation/Art/原版/0_mainmenu/40k_bt_deck.png.meta`：
`mipMapMode: 0` + `enableMipMap: 1`）⇒ 跟随既有口径，⛔ 不自造。

**未动**（W61 已核、简报明令）：`textureCompression`（Default=2 / Standalone=1）· `wrapU/V`（0/0）·
`filterMode`（1）· `aniso`（1）· `textureFormat`（-1）· `maxTextureSize`（2048 / Default 档 1024；128² 不受限，8 级 mip 全可生成）。
✅ 与 `vfx_texture_mips.tsv` 一致：这三行 `wrapU=0 wrapV=0 filterMode=1 aniso=1`。

## 五、生效路径

纯资产改动、**零条 C# 自检**（本包未改任何 `.cs`，无需 `typecheck`）。

- `.meta` 已落盘；Unity 下次 **reimport 这三张 PNG** 时才真正生成 mip 链。
- 同一口径也由 **`ArtBaker.BakeFromCLI`** 强制执行（`ArtBaker.cs:624` / `:470` → `OriginalMipEnabled`），
  所以下次烘焙会**保持** `true`（不会被静默打回 —— 那正是 `B139_H` 修掉的旧 bug）。
- 画面影响：MatCap 在缩小时按 mip 链采样 ⇒ 更平滑（原版行为）。验收需起 player 或烘焙后看卡体高光。
- ⚠️ **`Resources/Art/` 整个目录在 `.gitignore:124` 里（未纳入版本库）** ⇒ 本次改动只落在**磁盘**上，
  `git status` 看不到（这不是漏改，是既有约定）。真正的长期判据是
  `vfx_texture_mips.tsv` + `ArtBaker.OriginalMipEnabled`（这两样已对，重新烘焙会重建同样的 `.meta`）。

## 六、顺手发现（不在本包白名单，只记账不动作）

1. **`Resources/Art/boosterpack/Matcap_Booster_Pack.png.meta` 现读 `enableMipMap: 0`，而原版
   `Matcap Booster Pack`（`vfx_texture_mips.tsv:1908`）= **256×256 · mipCount 9** ⇒ 同样该开。**
   它同属 `Resources/Art/**`，`ArtBaker` 烘焙时会按真值表自动改成 `true`；只是 `.meta` 现值还没改。
   ⇒ 建议调度台派一条同形小活（或等烘焙顺带覆盖）。
2. `MatCap_Card_Level1.png.meta` 现读 `0` —— **与真值一致（`:1905` mipCount 1），无需改**（写在这里防止后人重查）。
3. `MatCap Polish.png`（13 个战场各一份）现读 `0` —— 真值 `mipCount 1`（`:1907`），同样**无需改**。
4. `Matcap Gold Emperos Children`（真值 `:1911` 1024² mipCount 11）在两处工程副本里**都已是 `enableMipMap: 1`** ✅（arena + `WarpforgeVFX/ArenaOriginalPrefabs`）。

## 七、❓未查清

- 原版 `Texture2D` 的 **`streamingMipmaps` / `mipMapBias` / `filterMode` 之外的 mip 细项**：`vfx_texture_mips.tsv`
  没有这些列，`assets_full` 的 `Texture2D/*.json` 我们这次没读 ⇒ **这三张的 `streamingMipmaps` 是否原版为 1 未查**（现值 0）。
  按简报「⛔ 不动 mip 相关以外的键」本次未动；若日后要「完全复刻」，需要读原版 `Texture2D` 的
  `m_StreamData` / `m_MipMap` 标志单独核一轮。

// ✅ 已复刻（2026-11-04 · B141 · W63 · 判据 = `vfx_texture_mips.tsv:1909/1910/1913` mipCount=8）

---

## 八、续节 `B141-W63b` · `Matcap_Booster_Pack` 开 mip

写手 `B141-W63b` · 第一百二十八会话 · 2026-11-04（承接 §六 顺手发现第 1 条）

### 判据

`数据/游戏数据/vfx_texture_mips.tsv:1908`：

| 行号 | name | width×height | mipCount | wrapU/V | filterMode | aniso | bundle |
|---|---|---|---|---|---|---|---|
| 1908 | `Matcap Booster Pack` | 256×256 | **9** | 0 / 0 | 1 | 1 | `boosterpacks_assets_all.bundle` |

256² 对应 256/128/64/32/16/8/4/2/1 = **9 级** ⇒ 与 `mipCount = 9` 自洽 ⇒ 原版开 mip。

### 逐张：原版真值 vs 改前 vs 改后

| 贴图（`Resources/Art/boosterpack/`） | 原版名（tsv 行） | 原版 mipCount | 改前 `enableMipMap` | 改后 | 改前 md5 | 改后 md5 |
|---|---|---|---|---|---|---|
| `Matcap_Booster_Pack.png.meta` | `Matcap Booster Pack`（`:1908`） | 9 | **0** | **1** | `72cc92977f1f2f69938a6b093790f594` | `7f7ce5568ba9bebb0c9bb2016f8e175a` |

改动行 = **第 9 行**：`    enableMipMap: 0` → `    enableMipMap: 1`。

### 验证（二进制读回）

| 检查项 | 结果 |
|---|---|
| 文件字节数（改前 → 改后） | 2652 → **2652**（delta 0） |
| 行数（`count('\n')`） | 117 → **117** |
| 行尾（CRLF / 裸 CR） | 0 / **0**（原就是纯 LF） |
| 逐行差异 | **仅第 9 行** |
| `enableMipMap: 1` / `: 0` 出现次数 | 1 / **0** |
| 其余键 | **未动**：`mipMapMode: 0` · `borderMipMap: 0` · `mipMapsPreserveCoverage: 0` · `mipMapFadeDistance(Start/End): 1/3` · `streamingMipmaps: 0` · `streamingMipmapsPriority: 0` · `ignoreMipmapLimit: 0` · `textureCompression`（Default 2 / Standalone 1）· `textureFormat: -1` · `maxTextureSize: 2048` · `filterMode: 1` · `aniso: 1` |

### 生效路径

纯资产、零条 C# 自检。`.meta` 已落盘，Unity 下次 reimport 该 PNG 时生成 9 级 mip 链；
`ArtBaker.BakeFromCLI` 经真值表（`OriginalMipEnabled`，`ArtBaker.cs:911-917`）同样会写 `true`，不会打回。
⚠️ 同 §五：`Resources/Art/` 在 `.gitignore:124`，改动只在磁盘。

### 顺手发现（白名单外，只记不动）

- **本文件 `wrapU: 1` / `wrapV: 1`，而原版真值是 `0` / `0`（`vfx_texture_mips.tsv:1908`）** ⇒ 属 wrap，不属本包白名单（简报：只改 mip 相关键）。
  `ArtBaker.OriginalWrap`（`ArtBaker.cs:922-929`）会按真值表写 `(0,0)`，下次烘焙自动对回。
  ✅ **已由续节 `B141-W63c` 做完**（本文件现读已是 `0 / 0`，见 §九）。
- `filterMode: 1` / `aniso: 1` 与原版一致 ✅。

✅ 已复刻（2026-11-04 · B141 · W63b · 判据 = `vfx_texture_mips.tsv:1908` `Matcap Booster Pack` mipCount=9）

---

## 九、续节 `B141-W63c` · `Matcap_Booster_Pack` wrap 对回 `(0,0)`

写手 `B141-W63c` · 第一百二十八会话 · 2026-11-04（承接 §八 顺手发现）

### 判据

`数据/游戏数据/vfx_texture_mips.tsv:1908`（`Matcap Booster Pack`，256×256，`boosterpacks_assets_all.bundle`）：

| 列 | wrapU | wrapV | filterMode | aniso | npotScale |
|---|---|---|---|---|---|
| 原版真值 | **0** | **0** | 1 | 1 | -1 |

（`wrapU/V = 0` = **Repeat**；与表里另外三张 `Matcap Bronze/Gold/Silver` 同档。）

### 原版真值 vs 改前 vs 改后

| 键 | 原版真值 | 改前 | 改后 |
|---|---|---|---|
| `wrapU` | 0 | **1** | **0** |
| `wrapV` | 0 | **1** | **0** |
| `wrapW` | 无判据（真值表没有此列） | 1 | **1（未动）** |
| `enableMipMap` | 1（§八 已改） | 1 | **1（未动）** |
| `filterMode` / `aniso` | 1 / 1 | 1 / 1 | **1 / 1（未动）** |

文件：`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/boosterpack/Matcap_Booster_Pack.png.meta`

### 验证（二进制读回）

| 检查项 | 结果 |
|---|---|
| 文件字节数 | 2652 → **2652**（delta 0） |
| 行数（`count('\n')`） | 117 → **117** |
| 行尾（CRLF / 裸 CR） | 0 / **0**（纯 LF 未变） |
| 逐行差异 | **仅第 40 行 `wrapU: 1 → 0` · 第 41 行 `wrapV: 1 → 0`** |
| md5 | `7f7ce5568ba9bebb0c9bb2016f8e175a` → `b388593fed2c66b6f974c180053c6d48` |
| `enableMipMap` / `filterMode` / `aniso` / `wrapW` | **全部未动**（逐行比对方差集里不出现） |

### 生效路径

纯资产、零条 C# 自检。`.meta` 已落盘，Unity 下次 reimport 该 PNG 时按 Repeat 采样；
`ArtBaker.OriginalWrap`（`ArtBaker.cs:922-929`）从真值表读 `(0,0)` ⇒ 烘焙也不会打回。
⚠️ 同 §五：`Resources/Art/` 在 `.gitignore:124`，改动只在磁盘。

### 后续（无）

本文件现在 4 个受核键（`enableMipMap` / `wrapU` / `wrapV` / `filterMode`+`aniso`）**全部与原版一致**；
只剩字段结构里的 `wrapW = 1` 无判据（真值表不给这一列，按简报不动）。

✅ 已复刻（2026-11-04 · B141 · W63c · 判据 = `vfx_texture_mips.tsv:1908` `Matcap Booster Pack` wrapU/V = 0/0）
