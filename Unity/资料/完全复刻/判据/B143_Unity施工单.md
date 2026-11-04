# B143 · 只开一次 Unity 窗口的 9 项施工单

> 只读查判据代理 `B143-R1` · 2026-11-05 · 第一百四十三会话
> 消费者 = 调度台。本文件**只写判据与做法**，状态仍以 `活状态表.md` 为准。
> ⛔ 本轮未跑 Unity、未改任何源码/资产/场景/账。

## 总览（先看这张）

| # | ID | 备齐？ | 一句话 |
|---|---|---|---|
| 1 | `B142-15-10-BAKE` | ✅ 可照做 | 真值表**已并入**（`CompTruths` 144 条）。烘焙入口**不是** `BakeFromCLI`（它会重渲 `arena1_bg.png`），要用 `ApplyImportSettings` **＋** `ApplyArenaImportSettings` 两条。 |
| 2 | `B141-DXT5` | ✅ **代码那一半已落地**（`B143-W7` · 2026-11-05）· 烘焙待 W132 | 83 个原版名 / 123 个文件实例；`LightningTrail` 已在表内 ⇒ **只需新增 82 条**（60 DXT5 + 22 DXT5C）。**已加**：`ArtBaker.CompTruths` **144 → 226 条**（82/82 名在盘上命中 122 实例 + `LightningTrail` 1 = **123**；精确键 / `CanonName` 两档**零重复**）。**必须在烘焙前改完**，一次烘焙全落地。 |
| 3 | `B141-ARENAPS` | ✅ 可照做（附「别误判」三条） | 受影响的是 **283 份** `.meta`（不是 226：226 只是 `Textures/` 那部分）。全部会得到 `Standalone.overridden = 1`。 |
| 4 | `B141-BOOSTER47` | ✅ 可照做（逐份预期值见正文） | 12 份里 **11 份会变**；`Matcap_Booster_Pack` 盘上**已经是目标值**（报告那句过期）。mip 只变 6 张爆炸图。 |
| 5 | `B142-AUDIOMIXER` | ✅ 可照做 | 原版 `Main Mixer` = 5 组 / 10 个效果实例（5 Attenuation + 2 Duck Volume + 3 Send）。参数值逐个给全。 |
| 6 | `B142-UACD-RENDERER` | ✅ 已裁定（方案 A 分两步） | 原版 `UI Camera` = `rendererIndex 1`。本窗口做结构：建 UI renderer + `rendererIndex 1`，**mask 先填 `4294966015`**（阶段性偏离，最终要 `32`）；迁层另立 `B143-UI-LAYER5`。 |
| 7 | `B142-E27-UI-RENDERER` | ✅ 可照做（含两个方案） | 「哪台相机用它」= **`UI Camera`（Overlay，`m_CameraType: 1`）**，原版证据已定。资产怎么建、mask 填什么，见正文。 |
| 8 | `B141-CARDAA-UNITY` | ✅ 可照做 | shader 编译验法、五档预期、旧场景要不要重生成都有答案；启动接线**已存在**（`[RuntimeInitializeOnLoadMethod]`，见 §8），只剩「嵌套类是否被扫到」待一次启动读数。 |
| 9 | `B141-W24-NONSERIAL` | ✅ 可照做（两条路，推荐 B） | 五处 `frontZ: 0.5` 的归属逐个查清；重存路线与备份纪律写全。 |

**建议执行顺序**：§2 改代码 → typecheck → §1/§3/§4 同一次烘焙验收（三者是同一趟）→ §5 混音器 → §9 重存场景 → §8 Play 验 shader/五档 → §6/§7（需先拍板 mask 方案）。

---

## B142-15-10-BAKE

- **判据结论**：
  - 15-10 的压缩档真值表**已经并进** `ArtBaker.CompTruths`（现读 **144 条** = 战场 **8** + UI **136**；`Unity/MyGame/Assets/CardPresentation/Editor/ArtBaker.cs:970-1135`，`struct ArtCompressionTruth` `:962-968`）。
  - 15-10 那 8 个名字（`ArtBaker.cs:973-982`）：`Noise Combined` / `NoiseNRM` / `Default-Particle` / `MuzzleFlash_Side` / `Matcap Gold Emperos Children` = **Uncompressed**；`CandleFlame` / `Flame01` / `MatCap Polish` = **Compressed + 显式 DXT1**。
  - `Noise Combined` 的原版判据 = `m_TextureFormat 4`(RGBA32) · 128²；PNG alpha 实测 **0..196** ⇒ 必须 Uncompressed（DXT 会吃掉渐变 alpha）。
  - `Shine trail` 的原版判据 = `fmt 12`(DXT5) · wrap **(0,1)**（`数据/游戏数据/vfx_texture_mips.tsv` 行 `Shine trail`）。
- **我们现在**（逐字节现读）：
  - `Unity/MyGame/Assets/WarpforgeArena1/arenas/battlearena1/Textures/Noise Combined.png.meta`：Default 块 `textureFormat: -1 / textureCompression: 1 / crunchedCompression: 0 / overridden: 0`（`:74-79`）；Standalone 块同值（`:87-92`）；`maxTextureSize` Default **512** / Standalone **2048**；`enableMipMap: 0`（`:9`）· `wrapU/V: 0/0`（`:40-41`）—— mip/wrap 已经是对的原版值，**本轮只会动压缩档与 Standalone 的 `overridden`/尺寸**。
  - `Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu/Shine_trail.png.meta`：压缩档已是 DXT5（Default `:74` = `12/1`，Standalone `:87` = `12/1`、`:92 overridden: 1`）；**wrap 仍是 (1,1)**（`textureSettings.wrapU/V` `:40-41`）；`enableMipMap: 0`（`:9`）。
  - `Resources/Art/ui_menu/Noise_Combined.png.meta`：Default `textureCompression: 2`(BC7) `/ Standalone 1`（不一致），等烘焙。
- **原版**：见上（`报告/B141_W130_ArtBakerTruth.md` §3·3 逐张清单；原始实读缓存 `_tmp_view/b139_i/orig_textures.json`）。
- **开窗口时怎么做**（⛔ 串行，同一工程只能一个 Unity 实例）：
  1. **先确认真值表在**：`grep -c '^\s*{ "' ArtBaker.cs` 的字典块现读 **144** 条（判据见上）。**不需要再动它**。
  2. **若同时做 `B141-DXT5`**：先把 §2 那 82 条加进 `ArtBaker.cs` 并跑 `bash 工具/typecheck.sh`（0/0）——这样一次烘焙把两件事一起落盘（否则要烘两遍，每遍都要重导 283 张 + 上千张 `Resources/Art`）。
  3. **不要用 `ArtBaker.BakeFromCLI`**：它 = `BakeBackdrop()`（打开 `battlearena1.unity`、`Simulate` 粒子、渲一张 1920×1080 写 `Resources/Art/arena1_bg.png`）＋ `ApplyImportSettings()`（`ArtBaker.cs:79-87`）。这 9 项没有一项需要重渲背景。
  4. 执行（两条都**必须**跑；第一条管 `Resources/Art`，第二条管 `arenas`）：
     ```bash
     unset ELECTRON_RUN_AS_NODE
     "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
       -projectPath "D:\4\Unity\MyGame" -executeMethod ArtBaker.ApplyImportSettings -logFile -
     "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
       -projectPath "D:\4\Unity\MyGame" -executeMethod ArtBaker.ApplyArenaImportSettings -logFile -
     ```
     （两个方法都是 `public static void`、无参：`ArtBaker.cs:433` / `:595`。第二条末尾会打印「实读校验：没有一张被缩」——那句是真判据，别看「已重设 N 张」。）
- **怎么验收**（读盘，⛔ 别用 `git status`：`.gitignore:80-84` 把 `arenas/*/Textures/` 和 `*.png(.meta)` 排除了、`:124` 排除整个 `Resources/Art/`）：
  | 文件 | 期望（烘焙后） |
  |---|---|
  | `arenas/battlearena1/Textures/Noise Combined.png.meta` | Default `textureFormat -1 / textureCompression 0`；Standalone `textureFormat -1 / textureCompression 0 / crunchedCompression 0 / overridden 1`；两处 `maxTextureSize: 512` |
  | `Resources/Art/ui_menu/Noise_Combined.png.meta` | Default + Standalone 都是 `textureFormat -1 / textureCompression 0`；Standalone `overridden` 保持烘焙前的值（`ApplyCompressionTruth` 只在显式格式时把 `overridden` 置 1，**不会**清 0） |
  | `Resources/Art/ui_menu/Shine_trail.png.meta` | `textureSettings.wrapU: 0` / `wrapV: 1`（`wrapW` 不动）；压缩档仍 `12/1` |
  | 战场其它 7 个名字的 39 个实例 | 与 `报告/B141_W130_ArtBaker.md` §3 那张表逐行一致（uncomp 5 名 / DXT1 3 名） |
- **风险与前提**：
  - `ApplyArenaImportSettings` 会 `SaveAndReimport()` **283 张** arena 贴图（其中 8192² 一张、4096² 二十余张）⇒ 这一条**很慢**，别和别的 Unity 任务并行；跑之前确认没有别的写手在改 `.cs`（`SaveAndReimport` 会触发脚本重编）。
  - 它会**顺带**改 `maxTextureSize` / `npotScale` / `alphaIsTransparency` / `mipmaps` / `wrap` / `filter` / `textureType` / `overridden` —— 见 §3，验收别把这些当回归。
  - `报告/B141_W130_ArtBakerTruth.md` §6 那三条「只有 Unity 能定」仍未定：① `Get/SetDefaultPlatformTextureSettings` 是否真把 format 写进 Default 块（回读 `ui_menu/Background.png.meta` 应仍是 `-1/0`，`ui_menu/UI_Deck_Warlord_Titus.png.meta` 应仍是 `12/1/1`）；② `overridden=0` 时 Unity 会不会忽略写入；③ Unity 接不接受 `format 12 + crunched 1`。**这三条烘焙后必须逐条回读**。
- **还差什么**：无（除上面三条只能 Unity 回读的）。

---

## B141-DXT5

- **判据结论**：
  - 原版战场贴图（`WarpforgeArena1/arenas/**/Textures/*.png`，**226 文件实例 / 116 唯一名**，按 `_tmp_view/b139_i/orig_textures.json` 100% 对回、0 未命中）格式分布 = **RGBA32 22 · DXT1 16 · BC7 65 · DXT5 96 · DXT5Crunched 27**。
  - 需要逐字照原版的那批 = **DXT5 61 个名字 / 96 实例** + **DXT5Crunched 22 个名字 / 27 实例** = **83 个名字 / 123 实例**。
  - 其中 `LightningTrail`（fmt 12）**已在 `CompTruths` 里**（`ArtBaker.cs:1061`）⇒ **实际只需新增 82 条**（60 条 DXT5 + 22 条 DXT5C）。
  - 目标档位：`UiDxt5(false)` = `Compressed` + 显式 `DXT5`（`ArtBaker.cs:1140`）；`UiDxt5(true)` 再加 `crunchedCompression: 1`。
- **我们现在**（逐文件现读 Default 块 `textureCompression`）：
  - DXT5 的 96 个实例里 **9 个已经是 `1`**、**87 个仍是 `2`(BC7)**。
  - DXT5Crunched 的 27 个实例 **27 个全是 `2`**。
  - 表外回退 = `CompressedHQ`（`OriginalCompression` `ArtBaker.cs:1160-1170` 的末行）⇒ 不新增条目，烘焙后它们**仍是 BC7**。
- **原版**：`fmt 12` = DXT5 · `fmt 29` = DXT5Crunched（枚举出处 `判据/B108_AUD09_纹理格式.md` §1·3）。
- **开窗口时怎么做**：
  1. 在 `ArtBaker.cs` 的 `CompTruths` **末尾**（现读最后一条是 `:1134 { "UI_Deck_Warlord_Winged Tyranid Prime", UiDxt5(true) },`，字典在 `:1135 };` 关闭）追加下面两组 **82 条**（每组前加一行分组注释；⛔ 别插进既有成员中间，改完跑 `bash 工具/typecheck.sh`）：
     ```csharp
     // ── 原版 `fmt 12`(DXT5) ⇒ `Compressed` + 显式 `DXT5`（B141 · B143：战场 60 名 / 95 实例；`LightningTrail` 已在上面 UI 组）──
     { "Asteroid_sheet_2", UiDxt5(false) },
     { "Battle Arena Aeldari Ground Mask", UiDxt5(false) },
     { "Battle arena 1 Clouds", UiDxt5(false) },
     { "Circle Halo", UiDxt5(false) },
     { "Circle Particle", UiDxt5(false) },
     { "Circle_Hoop", UiDxt5(false) },
     { "Cloud_puff", UiDxt5(false) },
     { "Dark Angels Ground Atlas", UiDxt5(false) },
     { "Debris", UiDxt5(false) },
     { "Emperors Children Ground Atlas", UiDxt5(false) },
     { "Emperos Children Fake Distort 1", UiDxt5(false) },
     { "Emperos Children Fake Distort 2", UiDxt5(false) },
     { "Explosion", UiDxt5(false) },
     { "Explosion Distort", UiDxt5(false) },
     { "FireBallEmission", UiDxt5(false) },
     { "Fireball", UiDxt5(false) },
     { "Flame03", UiDxt5(false) },
     { "Flare 6", UiDxt5(false) },
     { "Floor fire embers", UiDxt5(false) },
     { "Genestaler Fan", UiDxt5(false) },
     { "Glow Rays", UiDxt5(false) },
     { "LargeFlame01", UiDxt5(false) },
     { "Lens_Flare_1", UiDxt5(false) },
     { "Lightning_Burst_Random", UiDxt5(false) },
     { "Lightning_Random", UiDxt5(false) },
     { "NoiseContrast", UiDxt5(false) },
     { "Ork eye", UiDxt5(false) },
     { "Scarab_Necron", UiDxt5(false) },
     { "Shaft_SoftEdges", UiDxt5(false) },
     { "Shimmers01", UiDxt5(false) },
     { "SmallDots", UiDxt5(false) },
     { "Smoke", UiDxt5(false) },
     { "Smoke5", UiDxt5(false) },
     { "SmokePuff", UiDxt5(false) },
     { "SmokePuff01", UiDxt5(false) },
     { "Sororitas Atlas 3-_BaseMap-atlas-0", UiDxt5(false) },
     { "Sororitas Ground Atlas", UiDxt5(false) },
     { "Spark", UiDxt5(false) },
     { "Spark UI", UiDxt5(false) },
     { "Sparkle Large", UiDxt5(false) },
     { "SphereAlbedo", UiDxt5(false) },
     { "SplatNormal", UiDxt5(false) },
     { "Square Smooth", UiDxt5(false) },
     { "Square_Glow_Icon", UiDxt5(false) },
     { "TinyFlame", UiDxt5(false) },
     { "TinyStones", UiDxt5(false) },
     { "Trail_fading", UiDxt5(false) },
     { "Twinkle", UiDxt5(false) },
     { "WaterRipplesParticle", UiDxt5(false) },
     { "bubble", UiDxt5(false) },
     { "burst_puff", UiDxt5(false) },
     { "chestrays_mat", UiDxt5(false) },
     { "drops_cloud", UiDxt5(false) },
     { "glowsphere01", UiDxt5(false) },
     { "line", UiDxt5(false) },
     { "ring4", UiDxt5(false) },
     { "shockwave", UiDxt5(false) },
     { "smokeysteam", UiDxt5(false) },
     { "spacemarine droppod", UiDxt5(false) },
     { "wispySmoke", UiDxt5(false) },
     // ── 原版 `fmt 29`(DXT5Crunched) ⇒ 同上 + `crunchedCompression: 1`（22 名 / 27 实例）──
     { "Battle Arena 1 Back  Background", UiDxt5(true) },
     { "Battle Arena 1 Background", UiDxt5(true) },
     { "Battle Arena 1 Floor", UiDxt5(true) },
     { "Battle Arena 2 Ground", UiDxt5(true) },
     { "Battle Arena Aeldari Atlas 2", UiDxt5(true) },
     { "Battle Arena Aeldari Atlas-mat-_BaseMap-atlas-0", UiDxt5(true) },
     { "Battle Arena Necrons Ground With Reflection Mask", UiDxt5(true) },
     { "Battle Arena Space Wolves Floor", UiDxt5(true) },
     { "Battle Arena Tau Viorla Combined Material-mat-_BaseMap-atlas-0", UiDxt5(true) },
     { "Battle Arena Tau Viorla Ground Atlas", UiDxt5(true) },
     { "Battle Arena Tyranids Ground", UiDxt5(true) },
     { "BattleArena1 Texture Baked", UiDxt5(true) },
     { "BattleArena2Atlas Baked", UiDxt5(true) },
     { "BattleArena2Atlas Baked 2", UiDxt5(true) },
     { "BattleArena3Atlas-mat-_BaseMap-atlas-0", UiDxt5(true) },
     { "Black Legion Atlas 2-mat-_MainTex-atlas-0", UiDxt5(true) },
     { "Black Legion Ground Atlas", UiDxt5(true) },
     { "Data-mat-_MainTex-atlas-0", UiDxt5(true) },
     { "FireExplosion", UiDxt5(true) },
     { "LargeFlame02", UiDxt5(true) },
     { "SmokeLoop", UiDxt5(true) },
     { "Space Wolves Battle Arena Baked Atlas-_BaseMap-atlas-0", UiDxt5(true) },
     ```
     ⚠️ 有一个名字含**两个连续空格**（`Battle Arena 1 Back  Background`）—— 盘上文件名也是两个空格（`cat -A` 现读 `Battle Arena 1 Back  Background.png$`），照抄别合并。
     ⚠️ 表项按**原版名**键、查表带 `CanonName`（去 `_`/空格、忽略大小写，`ArtBaker.cs:1156`）⇒ 混合分隔符能命中。
  2. 改完 `bash 工具/typecheck.sh` ⇒ 期望 `运行时错误数: 0 · 编辑器错误数: 0`。
     - ✅ **已执行（2026-11-05 · `B143-W7`）**：82 条已按上面原样并入 `ArtBaker.cs` 的 `CompTruths` 末尾（分组注释 `:1136-1141` · 条目 `:1142-1224` · 字典 `:1225` 关闭）；
       现读 **226 条**（原 144 + 82）。逐条核对：82 名全部在盘上命中（**122 实例** + `LightningTrail` 1 = **123**）；
       **精确键 / `CanonName` 两档与现有 144 条均零重复**；内部零重复。`typecheck` 运行时 0 / 编辑器 0。**未烘焙**。
  3. 然后按 §1 的两条烘焙命令执行（**一次**）。
- **怎么验收**（`ApplyArenaImportSettings` 走的是**另一条写入路径**，与 `Resources/Art` 不同，⛔ 别按 UI 那套判）：
  | 项 | 期望 |
  |---|---|
  | 每个 DXT5 文件的 Default 块 | `textureFormat: -1`（战场路径不写 Default 的 format）· `textureCompression: 1` · `crunchedCompression: 0` |
  | 每个 DXT5 文件的 Standalone 块 | `textureFormat: 12` · `textureCompression: 1` · `crunchedCompression: 0` · `overridden: 1` |
  | 每个 DXT5C 文件 | 同上，但两块的 `crunchedCompression: 1` |
  | 计数 | Standalone `textureFormat: 12` 的文件 = **123**（96 + 27）；其中 `crunchedCompression: 1` = **27** |
  | 反例 | 若某个文件 Standalone 仍是 `textureFormat: -1` ⇒ Unity 没采纳显式格式（退路见 §1「风险」那条 ③，需再查） |
- **风险与前提**：
  - **顺序硬约束**：先改 `CompTruths`、后烘焙。反了就要再烘一遍（283 张 + 上千张重导）。
  - 表按原版名匹配、**不按路径** ⇒ 同名图在别的被扫目录里会一起命中。本轮实测：`Resources/Art` 下只有 `ui_menu/LightningTrail.png` 命中（它本来就已经是 DXT5 ⇒ 零变化）；`WarpforgeVFX/ArenaOriginalPrefabs/**` 有 143 处同名副本，但**那两个方法都不扫那个目录**（扫描根只有 `ArtDir` 与 `Assets/WarpforgeArena1/arenas`）⇒ 不受影响。
  - 新加的短名（`line` / `Smoke` / `Spark` / `Debris` / `Explosion` / `Fireball` / `bubble`…）将来若有人在 `Resources/Art` 放同名图，会被一起改成 DXT5 —— 这是「按原版名」的既有设计，记一笔即可。
- **还差什么**：无。

---

## B141-ARENAPS

- **判据结论**：`ApplyArenaImportSettings` 的扫描根 = `AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/WarpforgeArena1/arenas" })`（`ArtBaker.cs:597`）⇒ 它覆盖的是 **283 份** `.png.meta`，不是 226：
  - `Textures/` 下 **226** 份（= 报告口径）；
  - 其余 **57** 份 = 各场的 `preview_*` / `hide_*` / `holes_*` / `only_*` 掩码图 + `Sprites/Atack_Icon BW.png` 等。
  - 对每份都会写 `ps.overridden = true`（`ArtBaker.cs:650`，**无条件**，既有行为）。
- **我们现在**（283 份逐份现读）：
  - `overridden`（Default, Standalone）= **`(0,0)` × 85** + **`(0,1)` × 198**。
  - Default 块 `textureCompression` = **`1` × 85** + **`2` × 198**（没有任何一份是 `0`）。
  - Default 块 `textureFormat` **283/283 全是 `-1`**。
  - `maxTextureSize`（Default, Standalone）分布：`(2048,2048)` 93 · `(4096,4096)` 26 · `(512,2048)` 44 · `(1024,2048)` 4 · `(512,512)` 108 · `(1024,1024)` 7 · `(8192,8192)` 1。
- **原版 / 目标**：不是「原版值」，而是**这套导入函数的既定输出形状**。烘焙后（= §1 + §2 之后的预期，全部 283 份）：
  | 项 | 期望值 | 数量 |
  |---|---|---|
  | Standalone `overridden` | **1** | 283 / 283 |
  | Default `overridden` | **0**（本函数不写这一格） | 283 / 283 |
  | Default `textureCompression` | `0`(未压缩) | **22** |
  | | `1`(Compressed) | **139** = DXT1 16 + DXT5 96 + DXT5C 27 |
  | | `2`(CompressedHQ/BC7) | **122** = 原版 BC7 65 + 非 `Textures/` 那 57 |
  | Standalone `textureFormat` | `10`(DXT1) | **16** |
  | | `12`(DXT5/DXT5C) | **123** |
  | | `-1`(Automatic) | **144** = 未压缩 22 + BC7 122 |
  | Default `textureFormat` | **`-1` × 283**（⛔ 不是 `10/12`） | 283 |
- **开窗口时怎么做**：与 §1 的 `ArtBaker.ApplyArenaImportSettings` **同一条命令**，不需要额外动作；**先在 §2 把 82 条加进真值表**，否则 BC7 会是 245 而不是 122。
- **怎么验收**（三条「别误判」）：
  1. **`Default.textureFormat` 保持 `-1` 是正常的** —— 这条路径只写 `ti.textureCompression` 与 Standalone 块，**不**调用 `ApplyCompressionTruth`（那个才写 Default 的 format）。实际生效格式由 `Standalone.overridden = 1` 那一块决定。
  2. **85 份 `Standalone.overridden` 从 0 变 1 是本任务的预期结果**，不是「谁把资产改了」。
  3. **`maxTextureSize` 会普遍变化**（cap = 源边向上取 2 的幂，封顶 8192；`ArtBaker.cs:619-620`）—— 例如 `Noise Combined` 会从 2048 收到 512。
  - 判据脚本口径：逐份读 `platformSettings` 里 `buildTarget: DefaultTexturePlatform` / `Standalone` 两段，取 `textureFormat/textureCompression/crunchedCompression/overridden` 四键；**⛔ 不要按行号数**（`grep -c textureFormat` 会把顶层 `textureFormat: 1`（`:33`）和 `textureFormatSet` 一起数进来）。
- **风险与前提**：
  - 这 283 份里 **`Textures/` 与 `*.png.meta` 都被 `.gitignore` 排除**（`.gitignore:80-84`）⇒ `git status`/`git diff --numstat` **看不到**这批变更，验收只能读文件。
  - 烘焙会重导 8192²/4096² 大图，耗时以十分钟计；跑之前不要同时开别的 Unity。
  - 若某张 arena 图源文件缺失/损坏，`GetSourceTextureWidthAndHeight` 会回退 `LoadAssetAtPath` 再回退 2048（`:611-618`）—— 日志不会有告警，只能靠末尾「没有一张被缩」那句判。
- **还差什么**：无。

---

## B141-BOOSTER47

- **判据结论**：`OriginalNameCandidates` 补了「下划线 → 空格」一档（`ArtBaker.cs:780-800`）后，`Resources/Art` 有 47 个文件首次对回 `vfx_texture_mips.tsv` 真值。逐份现读后，**12 个里只有 11 个会变**：
  | # | 文件（`Resources/Art/…`） | 原版名 | 原版真值（mip / wrapU,V / filter / aniso） | 现读（mip / wrapU,V） | 烘焙后 | 变？ |
  |---|---|---|---|---|---|---|
  | 1 | `boosterpack/Booster_Pack_Explosion.png` | `Booster Pack Explosion` | 1 / 0,0 / 1 / 1 | 0 / 1,1 | **1 / 0,0** | ✅ |
  | 2 | `boosterpack/Booster_Pack_Explosion_Necron.png` | `Booster Pack Explosion Necron` | 1 / 0,0 / 1 / 1 | 0 / 1,1 | **1 / 0,0** | ✅ |
  | 3 | `boosterpack/Booster_Pack_Explosion_SW_1.png` | `Booster Pack Explosion SW 1` | 1 / 0,0 / 1 / 1 | 0 / 1,1 | **1 / 0,0** | ✅ |
  | 4 | `boosterpack/Booster_Pack_Explosion_Tau.png` | `Booster Pack Explosion Tau` | 1 / 0,0 / 1 / 1 | 0 / 1,1 | **1 / 0,0** | ✅ |
  | 5 | `boosterpack/Booster_Pack_Explosion_Tyranid.png` | `Booster Pack Explosion Tyranid` | 1 / 0,0 / 1 / 1 | 0 / 1,1 | **1 / 0,0** | ✅ |
  | 6 | `boosterpack/Booster_Pack_Explosions_Sororitas.png` | `Booster Pack Explosions Sororitas` | 1 / 0,0 / 1 / 1 | 0 / 1,1 | **1 / 0,0** | ✅ |
  | 7 | `boosterpack/Matcap_Booster_Pack.png` | `Matcap Booster Pack` | 1 / 0,0 / 1 / 1 | **1 / 0,0** | 1 / 0,0 | ❌ 已是目标值 |
  | 8 | `boosterpack/Card_Booster_Pack_Normal.png` | `Card Booster Pack Normal` | 1 / 0,0 / 1 / 1 | 1 / 1,1 | **1 / 0,0** | ✅ |
  | 9 | `ui_menu/Card_Ready_For_Level_Up.png` | `Card Ready For Level Up` | **0**(mipCount 1) / 0,0 / 1 / 1 | 0 / 1,1 | **0 / 0,0** | ✅（只 wrap） |
  | 10 | `ui_menu/UI_Dirt_And_Noise_skratches.png` | `UI Dirt And Noise skratches` | 0 / 0,0 / 1 / 1 | 0 / 1,1 | **0 / 0,0** | ✅（只 wrap） |
  | 11 | `ui_menu/Up_Rays.png` | `Up Rays` | 0 / 0,0 / 1 / 1 | 0 / 1,1 | **0 / 0,0** | ✅（只 wrap） |
  | 12 | `ui_menu/Shine_trail.png` | `Shine trail` | 0 / **0,1** / 1 / 1 | 0 / 1,1 | **0 / 0,1** | ✅（只 wrapV） |
  - **两处报告订正（铁律 5，请调度台回改 `报告/B141_W130_ArtBaker.md` §2 那张表）**：
    ① `Matcap_Booster_Pack` 盘上**已经是** `enableMipMap: 1 / wrapU: 0 / wrapV: 0` ⇒ 那句「mip 0→1 · wrap 1,1→0,0」已过期；
    ② `Card_Ready_For_Level_Up` 括号里的「mip 已是 1」不成立 —— 现读 `enableMipMap: 0`，而原版 `mipCount = 1` ⇒ **目标就是 0，不变**。
- **我们现在**：见上表「现读」列（全部逐份读 `textureSettings` / `mipmaps`）。
- **原版**：`数据/游戏数据/vfx_texture_mips.tsv` 同名行（列序 `name,width,height,mipCount,bundle,wrapU,wrapV,filterMode,aniso,npotScale`）。
- **开窗口时怎么做**：与 §1 的 `ArtBaker.ApplyImportSettings` 同一条命令，无需额外动作（`OriginalMipEnabled` `:912` / `OriginalWrap` `:922` / `OriginalFilterAniso` `:932` 都从同一张 TSV 查）。
- **怎么验收**：逐份读 `.meta` 的 `mipmaps.enableMipMap` 与 `textureSettings.wrapU/wrapV`，与上表「烘焙后」列逐格比。**不要**期待 `wrapW` 变（`OriginalWrap` 只写 U/V，`wrapW` 保持现值 1）。
- **风险与前提**：
  - `ApplyImportSettings` 会把 `Resources/Art` 下**全部**贴图重设一遍（`alphaIsTransparency=false`、`npotScale=None`、`maxTextureSize` 等）—— 这 12 张只是其中会变的 12 张，不代表其它没动。
  - `Resources/Art/` 整个目录被 `.gitignore:124` 排除 ⇒ 验收只能读文件。
- **还差什么**：无。

---

## B142-AUDIOMIXER

- **判据结论**（原版 `D:/2/tools/assetripper_out/ExportedProject/Assets/AudioMixerController/Main Mixer.mixer`，逐块现读）：
  - **5 个组**：`Voices`(`:45`) · `Master`(`:108`) · `FX`(`:145`) · `Music`(`:207`) · `Jingles`(`:269`)。
  - **10 个效果实例** = 5×`Attenuation` + **2×`Duck Volume`** + **3×`Send`**（「9 条效果链」这个说法与实读差 1；按组列全如下，以这份为准）：
    | 组 | `m_Effects` 顺序（= 链顺序） | 参数 |
    |---|---|---|
    | `Voices` | `Attenuation` → `Send` → `Send` | 两条 Send 的 `m_SendTarget` = `FX` 的 Duck Volume（`fileID 24400016`）/ `Music` 的 Duck Volume（`24400022`）；`m_EnableWetMix = 1`；电平 `0 dB`（snapshot `:345/:346`） |
    | `Master` | `Attenuation` | — |
    | `FX` | `Attenuation` → **`Duck Volume`** | 7 个参数 = **−25 / 2 / 0.2 / 0.3 / 0 / 10 / 1**（snapshot `:331-337`） |
    | `Music` | `Attenuation` → **`Duck Volume`** | 7 个参数 = **−35 / 2 / 0.2 / 0.3 / 0 / 10 / 1**（snapshot `:338-344`） |
    | `Jingles` | `Attenuation` → `Send` | Send 的 `m_SendTarget` = `Music` 的 Duck Volume；`m_EnableWetMix = 1`；电平 `0 dB`（snapshot `:347`） |
  - 组起始 dB（原版 snapshot）：Master **−0.16685188**，其余四组 **0**（`:321-329`）。
  - `FX.mixer` 里那条 `Receive`（原版 `AudioMixerController/FX.mixer:70`）**原版也是孤儿**（Main Mixer 没有 Send 指向它）⇒ 我们那条不是缺陷，⛔ 别「修」。
- **我们现在**：
  - 资产 = `Unity/MyGame/Assets/CardPresentation/Resources/Audio/Main Mixer.mixer`（另两份 `Music.mixer` / `FX.mixer` 在 `报告/B131_审计_音频参数.md` A8/A10 已判一致）。
  - 5 个组都在：`FX:9` · `Master:120` · `Voices:178` · `Music:197` · `Jingles:216`。
  - **每个组的 `m_Effects` 只有 1 条 `Attenuation`**；全文件 `m_EffectName` 现读只有 `Attenuation`（**0 条 Duck Volume / 0 条 Send**）。
  - 组音量已对：Master `-0.16685188`（`:161`），FX/Music/Voices/Jingles 全 `0`（`:163-169`）⇒ **这一格不用动**。
- **原版**：见上。
- **开窗口时怎么做**（`活状态表` 口径：**必须进 Unity Audio Mixer 窗口，⛔ 不许文本改 YAML**）：
  1. 菜单 `Window > Audio > Audio Mixer`，选中 `Main Mixer`（`Resources/Audio/Main Mixer.mixer`）。
  2. **FX 组**：选中 FX → 下方 `Add Effect` / 效果下拉 → **Duck Volume**。把 7 个数值字段（Inspector 里按显示顺序 = 序列化的 `Param_0…Param_6`）设为 **−25 / 2 / 0.2 / 0.3 / 0 / 10 / 1**。
  3. **Music 组**：同样加 **Duck Volume**，设为 **−35 / 2 / 0.2 / 0.3 / 0 / 10 / 1**。
  4. **Voices 组**：加 **Send**，把 `Target` 选到 **FX 组的那条 Duck Volume**；`Level` = **0 dB**（保持 `m_EnableWetMix = 1`）。再加**第二条 Send**，`Target` = **Music 组的那条 Duck Volume**，`Level` = **0 dB**。
  5. **Jingles 组**：加 **Send**，`Target` = **Music 组的那条 Duck Volume**，`Level` = **0 dB**。
  6. `Ctrl+S` 存工程（mixer 改动随资产保存）。
  - ⚠️ `Send` 的 Target 下拉在 Unity 里是「组/组内效果」的层级列表；若只列组，就先选 `FX`，然后在 Inspector/序列化里核对 `m_SendTarget` 指向的是 **Duck Volume 的 effect**（不是组）——判据以 YAML 为准（见验收）。
- **怎么验收**（读完盘 YAML，不跑游戏也能验 90%）：
  | 项 | 期望 |
  |---|---|
  | `m_EffectName: Duck Volume` 出现次数 | **2** |
  | `m_EffectName: Send` 出现次数 | **3** |
  | `m_EffectName: Attenuation` 出现次数 | 5（不变） |
  | FX / Music / Voices / Jingles 的 `m_Effects` 条数 | 2 / 2 / 3 / 2 |
  | 3 条 Send 的 `m_EnableWetMix` | 全 `1` |
  | 3 条 Send 的 `m_MixLevel` 对应 snapshot 值 | 全 `0`（dB） |
  | 两条 Duck 的 7 个参数值 | FX `-25/2/0.2/0.3/0/10/1`；Music `-35/...` |
  | 组音量 snapshot | Master `-0.16685188`，其余 0（**不该变**） |
  | 运行时听感 | 语音播放时 Music 组被压（`B131` A12 描述的「说话时音乐自动压低」）；可用 `WarpforgeAudio` 播一条 Voices cue 同时播 BGM，听/录一段 |
- **风险与前提**：
  - 加效果**不会**改组的 `m_GroupID` / 音量 GUID ⇒ 代码里 `VolumeFX/VolumeMusic/…` 那 5 个 exposed 参数不受影响。
  - Unity 保存 mixer 时可能重排 GUID/块序 ⇒ **只按「名字 + 值」验收**，⛔ 别按 fileID。
  - 7 个参数在 Inspector 里的**英文标签名本机查不到**（Unity 内置效果定义在编辑器二进制里，不在工程内）—— 已确认的是**序列化顺序与值**；照「第 1..7 个数值框」填即可。
- **还差什么**：Inspector 标签名查不到（不影响执行，按顺序填值 + 回读 YAML 值即闭环）。

---

## B142-UACD-RENDERER

- **判据结论**：原版战斗场景里 **Overlay 相机**的 `UniversalAdditionalCameraData.m_RendererIndex = 1`，Base 相机是 `-1`：
  - 原版 `D:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity`：
    - `:237359 m_CameraType: 0`（Base）→ `:237362 m_RendererIndex: -1`；
    - `:239531 m_CameraType: 1`（Overlay）→ `:239533 m_RendererIndex: 1`，该 UACD 属于 GameObject `&350` = **`UI Camera`**（`m_TagString: UICamera`，`:6490-6505`）；
    - 同一台相机的 `Camera.m_CullingMask.m_Bits = 32`（`:26581-26583`，= 只有层 5 `UI`）。
  - 原版 13 个战场场景同形：Base `-1` / UI Overlay `1`（`grep -n "m_RendererIndex" .../Battle Scenarios/*.unity` 逐场可见）。
- **我们现在**：
  - `Unity/MyGame/Assets/CardPresentation/Scenes/Battle.unity`：
    - `:940 m_Name: UI Camera` → UACD `:961 m_CameraType: 1` · **`:963 m_RendererIndex: -1`**（要改成 `1`）· `:962 m_Cameras: []`；
    - `:1485 m_CameraType: 0`（BoardCamera/Base）→ `:1488 m_RendererIndex: -1`（**正确，别动**）· `:1486-1487 m_Cameras: [{fileID: 688387911}]`（= UI Camera，说明 overlay 真的挂在 base 上）。
  - 我们 UI Camera 的 `m_CullingMask.m_Bits = 4294966015`（= 除层 8/10 全渲），与「渲染器 mask」是两回事。
  - **场景里 31 个 GameObject 全是 `m_Layer: 0`**（`grep -o "m_Layer: [0-9]*" | sort | uniq -c` 现读：只有 `0` × 31）；运行时建的 UI 也默认层 0。
- **原版**：见上（`rendererIndex = 1`，且**依赖**第 1 项 renderer = UI renderer）。
- **开窗口时怎么做**（**有前置**）：
  1. **先做 §7**：必须先在 `PC_RPAsset` 的 `m_RendererDataList` 里出现第 2 项（index 1）。
  2. 打开 `Battle.unity` → 选中 `UI Camera` → Inspector `Camera` 组件 → `Rendering > Renderer` 下拉选 `UI_Renderer`（等价于把 `:963` 写成 `1`）。
  3. `Ctrl+S` 存场景。
- **怎么验收**：
  - `grep -n "m_RendererIndex" Battle.unity` ⇒ `:963` 应为 `1`、`:1488` 仍 `-1`。
  - `PC_RPAsset.asset` 的 `m_RendererDataList` 有 **2** 项、`m_DefaultRendererIndex: 0`。
  - Play：UI 仍正常显示（见下「风险」）。
- **风险与前提（🔴 已裁定：方案 A 分两步，本窗口只做结构那一半）**：
  - 调度台 2026-11-05 裁定：**目标仍是照原版（UI renderer mask 32 + UI 迁层 5）**，但拆成两步：
    - **本次窗口（结构那一半）**：按原版建 UI renderer 资产、`UI Camera.m_RendererIndex = 1`、Base 保持 `-1`；**新 renderer 的 3 个 mask 先填 `4294966015`**（= 当前 UI Camera 的 cullingMask，覆盖我们 UI 实际所在的层），**保证画面不消失**。
    - **另一条账 `B143-UI-LAYER5`（调度台记）**：整族 UI 迁层 5 + mask 改 32 + 视觉验收，等主域完成后再做。
  - 🔴 **报告/账里必须写清这一句**：**当前 mask = `4294966015` 是阶段性偏离，最终目标是 `32`**（铁律 11：只有先后，不是不做）。
  - 原因：原版 UI 全在层 5，我们 UI 全在层 0（场景 31 个 GO 全 `m_Layer: 0`，运行时 UI 也默认 0）；直接 mask 32 会让整屏 UI 消失，而本窗口无法逐屏验收。
- **还差什么**：需要调度台/用户拍板 A 还是 B（B 可当场做，A 要单开迁层任务）。

---

## B142-E27-UI-RENDERER

- **判据结论**（原版 `D:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/UniversalRenderPipelineAsset_Renderer UI.asset`）：
  - `:13 m_Name: UniversalRenderPipelineAsset_Renderer UI`；
  - `:26 m_RendererFeatures: []`（**无任何 feature**，⛔ 不要带 `GrabPassTransparent`）；
  - `m_RendererFeatureMap:` 空；`postProcessData: {fileID: 0}`；`xrSystemData: {fileID: 0}`；`m_UseNativeRenderPass: 0`；`m_AssetVersion: 3`；
  - 三个 mask（`:34/:37/:40`）= **32**（只有层 5 = `UI`）；
  - `m_DefaultStencilState` 与主 renderer 相同（`compare 8 / pass 2 / fail 0 / zFail 0`）；`m_RenderingMode: 0`；`m_DepthPrimingMode: 0`。
  - **顺序判据**：原版 `UniversalRenderPipelineAsset High/Ultra.asset:19-22` = `[主 renderer, UI renderer]`、`m_DefaultRendererIndex: 0`；`Low/Medium.asset:19-22` = `[LOW renderer, UI renderer]`、同一份 UI renderer。
  - **「哪台相机用它」= `UI Camera`（Overlay，`m_CameraType: 1`）** —— 这是原版场景里唯一 `m_RendererIndex: 1` 的相机（见 §6 出处）。
- **我们现在**：
  - `Assets/Settings/` 只有 **2** 份 renderer：`PC_Renderer.asset`（guid `f288ae1f4751b564a96ac7587541f7a2`）· `Mobile_Renderer.asset`（guid `65bc7dbf4170f435aa868c779acfb082`）——**没有 UI renderer**。
  - `Assets/Settings/PC_RPAsset.asset:19-21`：`m_RendererDataList` **1 项**（PC_Renderer）、`m_DefaultRendererIndex: 0`。
  - `Assets/Settings/Mobile_RPAsset.asset:19-21`：**1 项**（Mobile_Renderer）、默认 0。
  - 当前生效档 = PC（`ProjectSettings/QualitySettings.asset:7 m_CurrentQuality: 1`、`:64 name: PC`，见 `报告/B142_E27.md` §6.3）。
- **原版**：见上。
- **开窗口时怎么做**（在 Unity 编辑器里做，⛔ 别手写资产 YAML——`m_Script` 的 guid 是**本工程 URP 版本**的，抄原版的 `c88ab7…` 会挂不上）：
  1. Project 窗口选中 `Assets/Settings/PC_Renderer.asset` → `Ctrl+D` 复制 → 重命名 **`UI_Renderer`**（路径 `Assets/Settings/UI_Renderer.asset`）。
  2. 打开它：删掉 `m_RendererFeatures` 里的**两条**（`GrabPassTransparent` + 另一条，见 `PC_Renderer.asset:45-47`），让列表变空、`m_RendererFeatureMap` 清空（`:48`）；`m_UseNativeRenderPass` 关（`:49`，原本就是 0）。
  3. 三个 Layer Mask：**本次填 `4294966015`**（= 当前 UI Camera 的 cullingMask，画面零变化）—— **这是阶段性偏离，最终目标是 `32`**（调度台 2026-11-05 裁定；迁层账 = `B143-UI-LAYER5`，见 §6）。
  4. 其它字段保持与 PC_Renderer 一致（`postProcessData` 指向同一个 `41439944…`；`m_RenderingMode 0`、`m_DepthPrimingMode 0`、stencil 状态不动）。原版 `postProcessData` 是 0，但若填 0，UI 相机就丢了后处理数据（我们的 UI 相机 `m_RenderPostProcessing: 0`，实际不用）——**保守做法：保持复制来的值不动**，并记账。
  5. `PC_RPAsset.asset` 的 Inspector（`Rendering > Renderer List`）→ 添加第二项 = `UI_Renderer`；`Default Renderer` 保持 `PC_Renderer`（index 0）。`Mobile_RPAsset` 同样加第二项（原版 Low/Medium 也是两级都带 UI renderer）。
  6. `Ctrl+S` 存工程。
- **怎么验收**：
  - `grep -n -A4 "m_RendererDataList" Assets/Settings/PC_RPAsset.asset` ⇒ **2 个 `- {fileID: 11400000, guid: …}`**，且第 2 个 = `UI_Renderer.asset.meta` 的 guid。
  - `UI_Renderer.asset`：`m_RendererFeatures: []`、`m_RendererFeatureMap:` 空、三个 `m_Bits` 按所选方案（32 或 4294966015）。
  - `Mobile_RPAsset.asset` 同样 2 项。
  - Play：UI 可见；失真（层 6）**只叠一次**（这正是原版把 UI renderer 拆出来的目的：UI 相机不再跑 GrabPass）。
- **风险与前提**：
  - `m_RendererFeatureMap` / `m_RendererFeatures` 的 fileID 只在本资产内有效，复制后必须**清空再重建**，⛔ 别留 PC 的两条 feature——否则 UI 相机也会跑一遍抓屏（重复扭曲）。
  - 加第 2 项不会影响 index 0 的现有行为（`m_DefaultRendererIndex` 仍 0）。
  - mask 的取舍见 §6（A 严格 / B 保画面）。
- **还差什么**：无（mask 的阶段性偏离与 `B143-UI-LAYER5` 已在 §6 写清）。

---

## B141-CARDAA-UNITY

- **判据结论**：
  - shader = `Unity/MyGame/Assets/CardPresentation/Resources/Shaders/CardSprite.shader`（`Shader "CardPresentation/CardSprite"`，`:39`；`#pragma multi_compile __ SUPERSAMPLING_ON` `:68`；`_AAScale` 属性默认 **0** `:51`，默认 0 是我们自设计并已打 `🔶`）。
  - 原版：`Custom/SuperSampled Texture` 的 `SUPERSAMPLING_ON` 变体 = 4-tap RGSS；材质 `Card Frame and Cardback for combat` 的 `_AAScale = 1.25`；开关 = 全局关键字，按画质档 `enableCardsAntiAliasing`（quality **0/1/2 关、3/4 开**）。
  - 我们的落点：`CardView.ApplyCardAaScale`（`Core/CardView.cs:16352-16361`）只对卡框（`:4475`）与卡背（`:7607`）写 **1.25**；`CardAaScale` 常量 `:16346`；共享基材显式写 0（`:8009`）。
  - 关键字切换：`CardPresentation.SettingsWindow.SuperSampling.CardsAntiAliasingAllowed`（`Shell/SettingsWindow.cs:6642`）与 `ApplyCardAntiAliasingKeyword()`（`:6653-6657`），由 `SuperSampling.Apply()` 开头调用（`:6661`，关键字那句在 `:6667`）。
- **我们现在**：
  - `B141_W33_Assert.Run()` **已接线**（`Editor/BattleScene.cs:47035` 那条聚合行），B 组覆盖 shader 存在 / `_AAScale` 默认 0 / 卡框=1.25 / 基材=0 / 卡背=1.25 / 五档阈值。
  - **旧版「0 调用者」结论已作废（2026-11-05 就地订正，铁律 5）**：`ApplyAtStartup()` 上面**有** `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`（`SettingsWindow.cs:6713`）⇒ 启动时由 Unity 反射调用，**grep 符号搜不到它**。先前「全仓 0 调用者」是**按符号名 grep 的假结论**，⛔ 别再照它去「补接线」。
  - 残留风险（唯一）：该特性挂在**嵌套静态类** `SettingsWindow.SuperSampling` 上，而本工程 `Battle/BattleSfx.cs:464` 有一条自述「嵌套类里的 `RuntimeInitializeOnLoadMethod` **不保证被扫到**」（同文件因此把入口放在外层类）。**这是可验证的，不是可猜的**：Play/player 启动后读一次 `Shader.IsKeywordEnabled("SUPERSAMPLING_ON")`（默认档 Ultra(4) ⇒ 期望 **true**）。若为 false ⇒ 才需要把这一跳挂到外层类（另派写手/另记一条），⛔ 本轮不猜着改。
  - 旧场景材质：`Scenes/CardBase.unity` 里 **0 处 `_AAScale`**（`grep -c` 现读 0；`Battle.unity` 也没有序列化的 CardSprite 材质）。原因：那批 `Material` 是 `CardBaseDemo` 用**旧版 shader** 建的、存在场景里。**运行时无害** —— `CardView` 建卡时会 `SetFloat("_AAScale", 1.25)`，属性存在就能写进去；只有「直接用场景里那批旧材质、不经 CardView」才会是 0。
- **原版**：见上（判据沉淀在 `CardSprite.shader` 文件头 `:20-38` 与 `B141_W33_Assert.cs:1-30`）。
- **开窗口时怎么做**：
  1. **编译**：Project 里选中 `CardSprite.shader` → 右键 `Reimport`。Console 里**搜 `Shader error in 'CardPresentation/CardSprite'`**（有 = 编不过，零容忍）；另可在 Play 时读 `Shader.Find("CardPresentation/CardSprite").isSupported`（编不过时为 `false`）。
  2. **静态断言**：跑 `-executeMethod BattleScene.Run`，看 `B141W33` 那组逐条 `✓/✗`（B0/B1/B2/B3a/B3b/B3c/B4★/B7/B8/B9/B10/B★/B11/B12…）。**B0 只能证明 shader 资产找得到，不证明编译通过** —— 编译必须靠第 1 步。
  3. **五档切换**（逐档看同一处：卡框/卡背边缘的锯齿）：
     - 在设置窗把画质下拉依次选 5 档（VeryLow/Low/Medium/High/Ultra），每次关窗；
     - 每档读 `Shader.IsKeywordEnabled("SUPERSAMPLING_ON")`；期望 **false / false / false / true / true**；
     - 视觉：0/1/2 档卡框边缘锯齿明显；3/4 档边缘被 4-tap RGSS 抹平（`_AAScale = 1.25`）。
     - ⚠️ 第 0 档启动时关键字是 **false**（因为上面那条缺陷）——**别把它当成「0 档正确」的证据**；要先用 High/Ultra 关一次窗把关键字打开，再回头测低档。
  4. **旧场景要不要重生成**：**不需要**（运行时由 `CardView` 写 1.25）。若本轮 §9 顺带跑 `CardBaseDemo.Run` 重存了 `CardBase.unity`，那批材质会带 `_AAScale: 0` 落盘（基材显式 0），只是让盘上数据更自洽，不影响画面。
- **怎么验收**：
  - Console 无 shader 编译错误；
  - `B141_W33_Assert` 全绿（`BattleScene.Run` 的 `B141W33 合计` 行）；
  - 五档关键字读数 0/0/0/1/1；
  - 视觉 A/B：同一张卡在 quality 2 vs 3 下卡框边缘并排截图（`_tmp_view/shots/…`）。
- **风险与前提**：
  - 全局关键字：`SUPERSAMPLING_ON` 一旦打开，**所有**用这支 shader 且 `_AAScale > 0` 的材质都会走 RGSS。目前只有卡框/卡背被写成 1.25，其余显式 0 ⇒ 安全；若将来有人给别的层写 >0，会静默变超采样。
  - 启动接线**已存在**（`[RuntimeInitializeOnLoadMethod]`，见上）；唯一未定的是嵌套类扫描（用一次启动读数验证，⛔ 别先改代码）。
- **还差什么**：shader 真编译只能 Unity 验（本文件已给验法）。

---

## B141-W24-NONSERIAL

- **判据结论**：
  - 原版 `CardScript__ShowCardInHand.c:104` 的 `local_40 = get_position().z − DAT_1834b2dbc`，该字面量 `.rdata 0x1834B2DBC` 4 字节实读 = **0.01f**（原版世界单位）；过桥 `14.835 / 108` ⇒ **0.0013736 我们的单位 = 0.148 px**。
  - 我们的字段已改成 `0.01f * OurUnitsPerWorldUnit` 并加 `[System.NonSerialized]`：
    - `Hand/HandLayout.cs:284`（`[NonSerialized]`）· `:285`（初始化器）· `:803`（唯一施加点：`float z = i * zOrderStep - (i == hoveredIndex ? frontZ : 0f);`）
    - `Hand/CardInteraction.cs:42`（`[NonSerialized]`）· `:43`（初始化器）—— ⚠️ 该字段**现读零读取方**（保留只为口径同步）。
  - 五处旧序列化值（全部 `frontZ: 0.5`，逐处查清归属）：
    | 场景 | 行 | 所属组件 | 身份 |
    |---|---|---|---|
    | `Scenes/Battle.unity` | **640** | `CardPresentation.CardInteraction` | 我方手牌交互（`hand: 895181130` / `cam: 688387911`） |
    | `Scenes/Battle.unity` | **1307** | `HandLayout` | 我方手牌（`cardScale: 0.73005104`、`invertRotation: 0`、`useExtraSpaceOnSelectedCard: 1`） |
    | `Scenes/Battle.unity` | **2544** | `HandLayout` | 敌方手牌（`cardScale: 0.54`、`invertRotation: 1`、`useExtraSpaceOnSelectedCard: 0`） |
    | `Scenes/CardBase.unity` | **8150** | `CardInteraction` | 卡基座演示 |
    | `Scenes/CardBase.unity` | **22445** | `HandLayout` | 卡基座演示手牌 |
- **我们现在**：五处 YAML 仍是 `frontZ: 0.5`；字段已 `[NonSerialized]` ⇒ **不生效**（Unity 读场景时忽略它），但 YAML 还在。
- **原版**：0.01 原版世界单位（换算值 0.0013736）。
- **开窗口时怎么做**（两条路，推荐 **B** 或视「场景是否本就该重建」选 A）：
  - **A · 生成器重存（工程既有路径，会整套重建）**：
    1. **备份**：`cp Unity/MyGame/Assets/CardPresentation/Scenes/{Battle,CardBase}.unity D:/4/_tmp_view/scene_bak/`（CLAUDE.md §二 的纪律）；记 `git show HEAD:Unity/MyGame/Assets/CardPresentation/Scenes/Battle.unity | wc -l`。
    2. 跑（串行）：
       ```bash
       unset ELECTRON_RUN_AS_NODE
       "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
         -executeMethod BattleScene.BuildAndSaveScene -logFile -
       "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
         -executeMethod CardBaseDemo.Run -logFile -
       ```
       （`BattleScene.BuildAndSaveScene` = `Editor/BattleScene.cs:241`；`CardBaseDemo.Run` = `Editor/CardBaseDemo.cs:119`，存盘在 `:284`。后者还会渲一批截图到它的 `OutDir`。）
    3. **代价**：`Battle.unity` 上一次提交停在 **2026-10-27**（`BattleScene.cs:47169` 的自述），之后有 `B120-W19` 等代码改动 ⇒ 重建是**预期的、可能很大的 diff**（`git diff --numstat` 可能上万行）。**先备份，再对比；不对就 `git checkout --` 还原**。
  - **B · 手删 5 行（最小改动，不重建）**：这 5 行是 `[NonSerialized]` 字段的残留，删掉在语义上是**空操作**（Unity 本来就不读）。用 Edit 工具逐行删 `frontZ: 0.5`（**保持文件原有行尾**，⛔ 别用 `sed -i`），然后让 Unity 打开一次场景确认无报错。
    - 优点：diff 只有 5 行、零重建风险；
    - 缺点：绕过了「生成器是唯一写手」的口径 ⇒ 必须记账「手工删了 5 行 YAML，判据 = 字段已 NonSerialized」。
    - ⚠️ 本工程历史上手改场景被证明过「只存在于盘上、重跑生成器就丢」；但这里是**删残留**，重跑生成器同样不会写回 ⇒ 不会被冲掉。
- **怎么验收**：
  - `grep -c "frontZ:" Battle.unity CardBase.unity` ⇒ 两边都 **0**；
  - `grep -c "^--- !u!115" <scene>` ⇒ **0**（`SceneStubAudit.Run` 也会自证）；
  - `B141_W24_Assert` 的 A5（`HandLayout.frontZ` 必须 `[NonSerialized]`）与 A1/A2/B1/B5 全绿（该断言已接在 `BattleScene.cs:47035`，跑 `BattleScene.Run` 即可）；
  - 若走 A：`git diff --numstat -- <scene>` 逐项过一遍，确认没有「不该变的地方变了」；再 `git status` 确认没有半成品。
- **风险与前提**：
  - 走 A 时：`BattleScene.BuildAndSaveScene` 内联跑 `B139_S_CameraSerialized_Assert.CheckSavedScene` / `B140_WF_Assert.CheckSceneRenderBake` / `ArenaBuilder.PatchLightmapEditorSettings` / `SceneStubAudit`（`BattleScene.cs:253-255`），**存盘已发生**；其中任何一条红都不回滚存盘 ⇒ 仍要按备份还原。
  - 走 A 时 `CardBaseDemo.Run` 会覆盖 `CardBase.unity` 并写截图；确认 `CardBase.unity` 里没有手工修过的东西（现读 `git status` 干净）。
  - 走 B 时：删行后 Unity 首次打开场景会重序列化一次（只是把缺失字段补成默认/忽略），不会把 0.5 写回。
- **还差什么**：无。

---

## 附：本波「顺手发现」（不属 9 条，供调度台立账）

1. ⚠️ **【2026-11-05 就地订正，原结论作废】** 先前写的「`SuperSampling.ApplyAtStartup()` 全仓 0 调用者」**不成立**：它上面有 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`（`Shell/SettingsWindow.cs:6713`），是按特性调用的，grep 符号名必然 0 命中。**不需要补接线**；唯一未定的是「嵌套类里的该特性是否被 Unity 扫到」（见 §8，可用一次启动读数验证）。
2. ⚠️ **`活状态表` / 报告里的「226 份 arena .meta」是 `Textures/` 的口径**，`ApplyArenaImportSettings` 实际覆盖 **283** 份（多出 57 份 `preview_*/hide_*/Sprites/`）。见 §3。
3. ⚠️ **`报告/B141_W130_ArtBaker.md` §2 有两处过期**（`Matcap_Booster_Pack` 已是目标值；`Card_Ready_For_Level_Up` 的 mip 目标就是 0）。见 §4 表下注。
4. ⚠️ **`B142` 混音器「9 条效果链」与实读差 1**：原版 `Main Mixer` 是 **10 个效果实例**（5 Attenuation + 2 Duck Volume + 3 Send）；若只数非 Master 的链 = 9。见 §5。
