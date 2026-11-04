# B139 · 包 H —— 战场场景生成器（`ArenaBuilder`）与纹理烘焙器（`ArtBaker`）

> 执行代理 `B139-H` · 2026-11-04 · 任务 = `H-1`（生成器写出原版 ambient / lightmap / bake 参数）· `H-2`（`ArtBaker` 不再无条件覆盖已修好的 `.meta`）
> ＋ 调度台 2026-11-04 补发：`B139-I3`（三档 wrap 白名单 + 7 张 wrap / 3 张 aniso 按原版）
> ＋ 审计条目 `8-01…8-06` · `15-05` · `16-04/05/06`（`报告/B138_审计汇总_问题清单.md` §8/§15/§16）
> 判据全部**回源现读**：`报告/B139_Y_YAML与ProjectSettings.md` · `报告/B136_审计_纹理与图集.md` · `报告/B136_审计_网格与预制体结构.md` · `报告/B133_审计_战场场景与环境.md` · 原版 bundle `assets_full/**` · 原版场景 YAML。
> ⛔ 未跑 Unity（红线）· ⛔ 未动 git · ⛔ 未改活状态表 / 正本 · ⛔ 未碰 `CardPresentation/Editor/BattleScene.cs` · `WarpforgeVFX/Runtime/**`。

---

## 0 · 一句话结论

两件活的**根因都在「生成器不写 / 烘焙器无条件写」**，所以只改盘上的 YAML/`.meta` 一定会被下一次重生成打回。

- **H-1**：`ArenaBuilder` 现在建完场景会写 5 类原版参数（equator / ground / subtractiveShadow / 烘焙四格 / 每个 `MeshRenderer` 的 `m_StitchLightmapSeams=0`），并**新建了一个产物级自检** `B139_H_Assert`（挂进 `BuildAll`）。
- **H-2**：`ArtBaker` 的两个导入设置方法改成**按原版真值表逐张写**（`vfx_texture_mips.tsv` + `arena_texture_import.tsv`），表外**保留现值**；wrap 从布尔白名单升级成**三档**（`0,0` / `1,1` / `0,1` 混合）。
- 顺带把 `8-01/8-03/8-04/8-05/8-06/15-05/16-04/16-05/16-06` 一起做完（一条数据链 + 一条构建链，缺一边就是半成品）。

---

## 一 · H-1：`ArenaBuilder` 现在写哪些原版场景参数

> 判据 = `B139_Y` §一（13 场 `!u!104` / `!u!157` 逐场实读，本批**回读原版场景 YAML 复核过**，不是抄报告数字）。
> 落点 = `WarpforgeArena1/Editor/ArenaBuilder.cs`（现读行号见账补丁 `账/_parts/B139_H.tsv` 的「我们的落点」列；本文件被账按行号引用，故所有新成员追加在**类尾**、调用点一律**同行追加**，行数零变化）。

| 字段 | 原版值（13/13） | 改前（Unity 默认） | 现在谁写 | 数据来源 |
|---|---|---|---|---|
| `RenderSettings.m_AmbientEquatorColor` | `(0.3254717, 0.5862051, 1, 1)` | 同（B139-Y 已改盘上） | `ApplySceneRenderAndBakeSettings` | 清单 `ambient.equator`（原版 `m_AmbientEquatorColor` 实读） |
| `RenderSettings.m_AmbientGroundColor` | `(0.5896226, 0.5896226, 1, 1)` | 同上 | 同上 | 清单 `ambient.ground` |
| `RenderSettings.m_SubtractiveShadowColor` | `(0.6367924, 0.7275943, 1, 1)` | `(0.42, 0.478, 0.627, 1)` | 同上 | 🆕 清单 `ambient.subtractiveShadow`（生成本轮新增，抽自原版 `RenderSettings`） |
| `LightmapSettings.m_LightmapEditorSettings.m_BakeBackend` | `0` | `2` | `PatchLightmapEditorSettings`（`SaveScene` 之后按 YAML 落） | 原版场景 YAML（**四格不在 bundle 里**，见下） |
| … `m_PVRSampleCount` | `500` | `512` | 同上 | 同上 |
| … `m_PVRFilteringGaussRadiusIndirect` | `5` | `1` | 同上 | 同上 |
| … `m_PVRFilteringGaussRadiusAO` | `2` | `1` | 同上 | 同上 |
| 每个 `MeshRenderer.m_StitchLightmapSeams` | `0`（1206 个全 0） | `1`（Unity 新建默认） | `ApplyRendererParams` | 原版场景 YAML 逐渲染器实读 |

**⚠️ 为什么烘焙四格要走「写盘后改 YAML」而不是编辑器 API**（实查，不是偷懒）：
Unity 6 里 `UnityEditor.LightmapEditorSettings.*` 已全部标 `Obsolete`（XML 文档原话 *"This property is now obsolete. Use LightingSettings.indirectSampleCount."*），而 `UnityEngine.LightmapSettings` **只暴露 `lightmaps` / `lightProbes` / `lightmapsMode`**（`UnityEngine.CoreModule.xml` 现查）⇒ **没有对象访问口**，`new SerializedObject(...)` 拿不到那个 `!u!157` 对象。
`m_LightingSettings` 13 场又都是 `{fileID: 0}`（`B139-Y6` 已判：新建资产 = 孤儿假复刻）⇒ 只剩「对生成出来的场景 YAML 做定点改写」这一条路。场景本身就是本生成器的产物，改它 = 把值写进生成链。

**实现要点**：只扫 `--- !u!157` 块内的 `  m_LightmapEditorSettings:` 子块、只改这四行、行尾按原文件（Unity 写 LF）；**写完当场回读**，4 格里一个都没命中就红（⛔ 不静默）。用 python 在 `battlearena1.unity` 上逐行模拟过：命中 4 行、行号 71/74/88/89，与原值一致。

---

## 二 · H-2：`ArtBaker` 不再无条件覆盖

### 2·1 改前 / 改后

| 位置（现读行号） | 改前 | 改后 |
|---|---|---|
| `ApplyImportSettings` 的 `textureType` | 无条件 `Default` | `OriginalTextureType(path, 现值)` —— **原版法线图保留现值**、其余仍 `Default` |
| `ApplyImportSettings` 的 `mipmapEnabled` | 无条件 `false` | `OriginalMipEnabled(path, 现值)` —— 表里按原版 `m_MipCount>1`；**表外保留现值** |
| `ApplyImportSettings` 的 wrap | 布尔白名单（`RepeatTextures`）⇒ 其余一律 Clamp | `OriginalWrap(path, out u, out v)` —— **三档**：`0,0` Repeat / `1,1` Clamp / **`0,1` 混合**；表外退回老口径 Clamp |
| `ApplyArenaImportSettings` 的 `textureType` / `mipmapEnabled` | 无条件 `Default` / `false` | 同上两个 helper（这一条才是 `15-12` 的雷） |
| `ApplyArenaImportSettings` 的 `filterMode` / `wrapMode` / aniso | 固定 `Bilinear` / `Repeat` / 不动 | `OriginalWrap` + `OriginalFilterAniso`，表外**保持旧口径** |

### 2·2 判据从哪来（⛔ 不写死在代码里）

- `数据/游戏数据/vfx_texture_mips.tsv`（**2,984** 个原版 `Texture2D` 名；`工具/gen_vfx_texture_mips.py` 生成）
- `数据/游戏数据/arena_texture_import.tsv`（**127** 个战场名；`工具/gen_arena_texture_import.py` 生成）
- 两张表的「本方法要用的五格」列序**一致**：`mipCount=3 · wrapU=6 · wrapV=7 · filterMode=8 · aniso=9` ⇒ 一个解析器吃两张。
- 文件名 → 原版名：去 `_sprite` / `_0` / `_1` 后缀（与包 I 的 `_tmp_view/b139_i/analyze2.py` **同一套**判据，它靠这套把 3,009 个原版对象对回 1,724 张）。

### 2·3 `textureType` 为什么不照原版直接写 `NormalMap`（`B139-I1` 第 ① 步）

我们的 21 张法线 PNG 现在是**原版的 GPU 存储形态**（DXT5nm：X 在 A、Y 在 G，R 恒 255）。设 `textureType: 1` 会让 Unity 按 R,G 重打包（x ≡ +1）⇒ **法线全坏**。
所以本批只做 `B139-I1` 三步里的**第 ① 步**：`OriginalTextureType` 对 13 个原版法线名**一律保留现值**（既不打回 `Default`、也不擅自设 1）。
第 ② 步（按 `判据/B110_R5_资源侧放回11条.md:174-196` 把 21 张 PNG 重建成 RGB 形态）与第 ③ 步（设 `textureType: 1`）属**资源线**，不在本包。

---

## 三 · 审计条目

| 条目 | 做了什么 | 判据 / 落点 |
|---|---|---|
| **8-01** 30 个加性粒子被建成 Alpha 混合 | ① 6 份材质（30 颗粒子共用）改成 `_SrcBlend 5 / _DstBlend 1`、alpha 侧 `5/1`、`_Blend 2`；② **根因**修掉：推断出来的混合原来只写在 `SaveOrReuse` **之后**，实证没落盘（见下） | `ArenaBuilder.ApplyInferredParticleBlendPreSave` · 6 份 `.mat` |
| **8-02** 本体网格 shader 全换 `URP/Unlit` | **只补 `🔶 自己设计` 标记**（按 `B138` 的原话）；逐份重写原版 shader 属资源线 | `ArenaBuilder.cs` 的 `GetOrCreateMaterial` 头注释 |
| **8-03** `cullingMode` 没抽也没接 | 清单新增 `cullingMode`（原版 191 对象 ≠ 0），构建侧 `main.cullingMode = …` | 生成器 + `BuildContent` |
| **8-04** `ShapeModule.length` 没抽也没接 | 清单新增 `shapeLength`（原版 56 对象 ≠ 默认 5.0），构建侧 `sh.length = …` | 同上 |
| **8-05** `emitterVelocityMode` | 清单新增 + `main.emitterVelocityMode = …`（原版 805 对象 ≠ 0） | 同上 |
| **8-06** UV 模块 7 字段 | 清单新增 `uvFlipU/uvFlipV/uvMode/uvRowMode/uvChannelMask/uvStartFrame/uvSpeedRange`；构建侧接 6 个（`uvMode` 在 Unity 6 **没有 API**，见下） | 同上 |
| **15-05** Tiled 精灵丢 border | 旁挂 `_sprites.json` 新增 `border`（原版 `Sprite.m_Border`）→ `SpriteEntry.border` → `LoadSpriteAsset(..., border)` 写 `ti.spriteBorder`，并进 `changed` 判据 | `gen_arena_sprites.py` + `ArenaBuilder` |
| **16-04** `m_RenderingLayerMask` | 清单新增（原版 26/31 = `All(4294967295)`），`ApplyRendererParams` 写 | 生成器 + `ArenaBuilder` |
| **16-05** `m_LightProbeUsage` | 同上（原版 13 个 = `BlendedProbes`） | 同上 |
| **16-06** `m_SortingOrder` | 同上（原版 1 个 = 1：`Particles/Battle/TinyExplosion_Far/Shockwave`） | 同上 |

### 8-01 的根因（这次是**实证**，不是推测）

- 建场日志逐条打印「按 shader 名推断成 `Blend 5/1`」（`_tmp_view/arenabuild.log` 等 8 条日志里都有），
- 而盘上 `battlearena3/Materials/PS_citclr_light.mat` 一直是 `_SrcBlend: 5 / _DstBlend: 10`（Alpha）。
- 断点：那段 `ShouldInferParticleBlend → InferBlendFromShaderName → SetBlend` 写在 `SaveOrReuse` **之后**，而 `SaveOrReuse` 走 `EditorUtility.CopySerialized(fresh, existing)`。
- 修法：**在 `SaveOrReuse` 之前先对新建的那份 `Material` 写一遍**（`ApplyInferredParticleBlendPreSave`），`CopySerialized` 会把它带过去；后面那段保留（幂等 + 回读校验）。
- ⚠️ 另一条同源观察：161 份粒子材质里 **160 份 `_SrcBlendAlpha = 1`**，没有一份是 `SetBlend` 写的 `= srcBlend` ⇒ 佐证「`SetBlend` 的写在 SaveOrReuse 之后没落盘」。

### 8-06 里 `uvMode` 为什么只抽不接

原版 `UVModule.mode` 在 **Unity 6 没有对应 API**（`TextureSheetAnimationModule` 的成员是 `animation` / `rowMode` / `flipU/V` / `uvChannelMask` / `startFrame` / `speedRange`，没有 `mode`）——它是 Unity 5.5 之前的 legacy 字段（后被 `animation` 取代）。清单里**照抽原版真值**（供审计），构建侧**不接**并在代码里注明。

---

## 四 · `B139-I3`（调度台 2026-11-04 补发）

### 4·1 `RepeatTextures` 布尔白名单 → 三档

`ArtBaker.OriginalWrap(path, out u, out v)` 现在按**原版 `m_WrapU/V`** 逐张给：
`0,0`=Repeat · `1,1`=Clamp · **`0,1`=混合**（U Repeat / V Clamp）。
判据顺序：真值表 → `RepeatTextures` 老白名单（那 12 张原版 (0,0)）→ 表外按旧口径 Clamp。
`RepeatTextures` 数组**保留**（它只表达 `(0,0)` 那一档），doc 里已就地写明三档在哪。

### 4·2 7 张 wrap + 3 张 aniso（已改盘上，⛔ 只改该字段）

| 文件 | 字段 | 改前 | 改后（原版） |
|---|---|---|---|
| `WarpforgeVFX/Textures/Card Damage_sprite.png.meta` | wrapU/V | 1 / 1 | **0 / 0** |
| `WarpforgeVFX/Textures/Card Heal_sprite.png.meta` | wrapU/V | 1 / 1 | **0 / 0** |
| `WarpforgeVFX/Textures/Card Ready For Level Up_sprite.png.meta` | wrapU/V | 1 / 1 | **0 / 0** |
| `WarpforgeVFX/Textures/Up Rays_sprite.png.meta` | wrapU/V | 1 / 1 | **0 / 0** |
| `WarpforgeVFX/Textures/ring2.png.meta` | wrapU/V | 0 / 0 | **1 / 1** |
| `CardPresentation/Resources/Art/vfx/Shine trail.png.meta` | wrapU/V | 1 / 1 | **0 / 1**（混合档） |
| `CardPresentation/Art/原版/去重资源/2_40k_cardframe_rarity_rare.png.meta` | wrapU/V | 0 / 0 | **1 / 1** |
| `CardPresentation/Resources/Art/ui_menu/Background.png.meta` | aniso | 1 | **16** |
| `CardPresentation/Resources/Art/ui_menu/InputFieldBackground.png.meta` | aniso | 1 | **16** |
| `WarpforgeVFX/Textures/Debris_sprite.png.meta` | aniso | 1 | **16** |

⚠️ 补充：`WarpforgeVFX/Textures/Shine trail.png.meta` 复核时发现**本来就是 `(0,1)`**（`B139-I` 表里那条「我们 1,1」指的是 `Resources/Art/vfx/` 那一份）⇒ 只改了 `Resources/Art/vfx/` 那份，没动 VFX 那份。
⚠️ 10 个文件**行数零变化、行尾零变化**（LF、117/142 行不变），只改目标字段。

### 4·3 4 张 LUT 的 sRGB（**复核后保留**，反证如下）

| 项 | 值 |
|---|---|
| 我们的 `.meta` | `sRGBTexture: 0`（4 张：`Resources/WarpforgeVFX/LUT/{LUT Blind, LUT Blizzard, LUT Overexpose High, LUT_Dark}`） |
| 原版 | `m_ColorSpace = 1` |
| **反证 1** | 本工程 **Gamma 色彩空间**（`ProjectSettings/ProjectSettings.asset:50` `m_ActiveColorSpace: 0`）。Gamma 空间下 sRGB 旗标**不参与采样转换**（Unity 只在 Linear 空间做 sRGB→linear）⇒ 采样结果逐字节相同。 |
| **反证 2** | LUT 是**数据不是颜色**（`ArtBaker` / `ArenaBuilder.EnsureLutImportSettings` 两边都按这个口径写），标成 sRGB 只会在切到 Linear 时**破坏**查表。 |
| **旁证** | `报告/B136_审计_纹理与图集.md` #14 已判「原版 4 张标 cs=1 的在 Gamma 下旗标不生效，我们 0 = 原始字节，等效」。 |
| 结论 | **保留 `sRGB: 0`**，不按字面复刻。⚠️ 若将来工程切 Linear，这 4 张要一起改（本行就是那条判据）。 |

---

## 五 · 「重跑生成器 / 烘焙之后，哪些字段会恢复成原版值」（要求的那张表）

> 这张表就是验收口径：**跑一次下面两条命令，盘上应当逐项变成「原版值」这一列**。

| # | 触发动作 | 文件 | 字段 | 原版值 |
|---|---|---|---|---|
| 1 | `ArenaBuilder.BuildFromCLI`（每场）/ `BuildAll`（13 场） | `WarpforgeArena1/Scenes/battlearena*.unity` | `m_AmbientEquatorColor` | `(0.3254717, 0.5862051, 1, 1)` |
| 2 | 同上 | 同上 | `m_AmbientGroundColor` | `(0.5896226, 0.5896226, 1, 1)` |
| 3 | 同上 | 同上 | `m_SubtractiveShadowColor` | `(0.6367924, 0.7275943, 1, 1)` |
| 4 | 同上 | 同上 | `m_BakeBackend` / `m_PVRSampleCount` / `m_PVRFilteringGaussRadiusIndirect` / `m_PVRFilteringGaussRadiusAO` | `0 / 500 / 5 / 2` |
| 5 | 同上 | 同上 | 每个 `MeshRenderer.m_StitchLightmapSeams` | `0`（1200+ 个全 0） |
| 6 | 同上（建 prefab / 建场都走 `BuildContent`） | `Resources/ArenaPrefabs/*.prefab` · `Scenes/*.unity` | `m_RenderingLayerMask` | 逐对象，原版 26/31 = `4294967295` |
| 7 | 同上 | 同上 | `m_LightProbeUsage` | 逐对象，原版 13 个 = `1` |
| 8 | 同上 | 同上 | `m_SortingOrder` | 逐对象，原版 1 个 = `1` |
| 9 | 同上 | `WarpforgeArena1/arenas/*/Sprites/*.png.meta`（经 `LoadSpriteAsset`） | `spriteBorder` | 原版 `m_Border`（sororitas 的 3 个 `Bottom Filler *` = `(7,0,7,9)`） |
| 10 | 同上 | 粒子 | `ParticleSystem.MainModule.cullingMode` / `emitterVelocityMode` · `ShapeModule.length` · UV 6 格 | 逐对象（原版 ≠ 默认的见 §三） |
| 11 | 同上 | `arenas/*/Materials/PS_*.mat` | `_SrcBlend / _DstBlend / _SrcBlendAlpha / _DstBlendAlpha / _Blend` | 加性 = `5 / 1 / 5 / 1 / 2` |
| 12 | `ArtBaker.BakeAll` / `BakeFromCLI`（**只跑 `ApplyImportSettings`**） | `Resources/Art/**/*.png.meta` | `enableMipMap` · `wrapU/wrapV` · `filterMode` · `aniso` | 逐张 = 原版真值表；表外**保持现值** |
| 13 | `ArtBaker.ApplyArenaImportSettings`（菜单） | `WarpforgeArena1/arenas/**/*.png.meta` | 同上 + `textureType` | 同上（法线图保留现值，见 2·3） |

---

## 六 · 需要 Unity 窗口复核（本包**未跑 Unity**）

| # | 跑什么 | 看什么 | 期望 |
|---|---|---|---|
| 1 | `WF_ARENA=battlearena1 -executeMethod ArenaBuilder.BuildFromCLI` | 日志里 `[Arena] 烘焙参数已落 4/4 格` | 4/4；随后 `grep -c "m_BakeBackend: 0" Scenes/battlearena1.unity` = 1 |
| 2 | 同上 | `[Arena] 烘焙参数回读…` 若打红 | 说明 `!u!157` 块写法变了（Unity 版本），要人读 YAML |
| 3 | 同上 + 打开场景 | Lighting 面板「Lightmapper」 | 原版是 `Enlighten`（`m_BakeBackend 0`）；**Unity 6 已移除 Enlighten 后端**，可能在打开时被归一回 `2`（`B139-Y` §二·5 已预警）——若被归一，本条记「引擎不支持、无法保持」 |
| 4 | `-executeMethod ArenaBuilder.BuildAll` | 结尾 `[B139_H] 合计：✓ N · ✗ 0` | 全绿（断言已挂在 `BuildAll` 末尾） |
| 5 | 同上 | `[B139_H] ✗ …` 若出现 | 按那一条的文案回查（每条都写了字段名与期望值） |
| 6 | `ArtBaker.BakeFromCLI` 之后 | `git status` / 逐张 `.meta` | **不应出现**「包 I 的 49 张 mip / 17 张 wrap 被改回去」 |
| 7 | 同上 | 日志里 `贴图真值表已载入：N 条` | N > 3000（两张表合计）；若打 Warning「读不到真值表」⇒ 表路径错了 |
| 8 | `-executeMethod B139_H_Assert.RunFromCLI` | `[B139_H]` 全部 ✓ | 同 4 |

⚠️ **为什么这些必须留着让 Unity 验**：`typecheck` / 账闸 / 退出码都**不能证明这段代码会执行**（本工程踩过 6 次「代码落进 `//` 注释」）。产物自检 `B139_H_Assert` 是唯一能证明「跑过了、字段真的写进去了」的口径。

---

## 七 · 校验命令与结果

```bash
TMPDIR=/tmp/wf_h bash d:/4/Unity/工具/typecheck.sh
  → 运行时错误数: 0 · 编辑器错误数: 0

python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A
  → 合计：A 5932 条 · 通过 5932（其中告警 894 条）· **漂移 0 · 失败 0**

python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry --only B139_H
  → 更新 1 · 新增 9 · 无变化 0（dry-run，未写盘）
     · 「更新 1」= `卡牌面_渲染.tsv` 里 `B139_I` 那行 `ApplyArenaImportSettings()`（原记「未改，归包 H」，本批已改，落点从 `:625` 订正到方法声明行 `:591`）
     · 域归一：`其他/基础设施` → `卡牌面/渲染`（跟随主表，merge 工具自己做的）
```

数据侧（本批跑过的）：

| 命令 | 结果 |
|---|---|
| `"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/scripts快照/gen_unity_arena_manifest.py --all --no-copy` | **13/13 场**清单重生成（rc=0）。**逐场核过「除新键外与 HEAD 逐字节相等」**：13/13 全 `rest-identical = True`（`meshes`/`particles` 条数不变：30/34 · 56/46 · 48/46 · 42/23 · 83/20 · 51/39 · 59/34 · 82/13 · 48/35 · 45/45 · 83/49 · 56/31 · 51/163） |
| `"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_arena_sprites.py` | **13/13 场** `_sprites.json` 重生成（rc=0）；**只有 sororitas 的 3 个 `Bottom Filler *` 有非零 border = (7,0,9)** ← 见下注，其余全 0；diff 只有新增的 `border` 键（6 行/个） |
| `python -I -X utf8 d:/4/_tmp_view/b139_h/fix_mats_and_metas.py` | 6 份 `.mat` + 10 份 `.meta`，**逐字段回读**（`bad = 0`）；10 份 `.meta` 行数与行尾零变化 |
| `python -I -X utf8 d:/4/_tmp_view/b139_h/mirror_assert.py` | **离线镜像** `B139_H_Assert` 的五组检查（读盘、不跑 Unity）：**OK=138 · BAD=0** |

> 注：sororitas 三个 `Bottom Filler *` 的 border 是 **`(7, 0, 7, 9)`**（`(left, bottom, right, top)`），上面那处 `(7,0,9)` 是笔误 —— 以 `_sprites.json` 现读为准。

**先行验证（跑生成器之前做的，不是事后找补）**：用**未修改**的生成器重跑 `battlearena1`，产物与盘上那份**逐字节相同**（838,481 B）⇒ 生成器确定性成立，这才敢 `--all`。

---

## 八 · 顺手发现 / 未做（交调度台分流）

1. 🔴 **`ArenaBuilder` 的 `LoadSpriteAsset` 里还有一处无条件 `ti.mipmapEnabled = false`**（精灵贴图）。它和 `ApplyTextureImportSettings` 的逐张真值（`arena_texture_import.tsv` 覆盖 `Sprites/` 18 份）**互相打架**，谁后跑谁赢。本批**没动**（不在审计条目里，且改了要先判精灵要不要 mip）。建议并入纹理线。
2. 🔴 **`_tmp_view/b139_i/backup/`**（包 I 的 55 个 `.meta` 原件）与本批的 `_tmp_view/b139_h/backup/`（10 个）都在，可按相对路径还原。
3. ⚠️ **13 场 `Scenes/battlearena*.unity` 与 `arenas/*/Materials/`、`Resources/ArenaPrefabs/` 全部被 `.gitignore` 排除**（`B125-ARENAGITIGNORE`）⇒ 本批对 `.mat` 的修复**进不了 git**，只能靠 `ArenaBuilder` 重生成（代码已改，重跑即得同样结果）。
4. ⚠️ **`Manifest` 里新增了 8 个键**（`subtractiveShadow` · 3 个渲染器格 · 3 个粒子格 · UV 7 格）。旧清单（本批之前生成的）**不会**有这些键 ⇒ `JsonUtility` 会给字段初始化值（`renderingLayerMask=1` / `lightProbeUsage=1` / `shapeLength=5` / `uvRowMode=1` / `uvChannelMask=-1`），**等于旧行为**，不会炸；但 `subtractiveShadow` 为 null 时会打 Warning 并保留 Unity 默认。
5. ⚠️ **`BuildContent` 里 `ApplyRendererParams` 每建一个 `MeshRenderer` 就 new 一次 `SerializedObject`**（13 场约 1200 个）。批处理下可接受；若将来嫌慢，可改成「先把所有 mr 收集起来、一把 SerializedObject」。
6. ⚠️ **真值表按**文件名**对回原版名（去 `_sprite`/`_0`/`_1` 后缀）**，同 `B139-I` 的口径。风险 = **同名不同资产**（原版本来就有同名多份，`vfx_texture_mips.tsv` 3,009→2,984 里那 25 组被折叠成一条）⇒ 少数 `Resources/Art` 图可能对到另一张同名原版而拿到它的 mip/wrap。后果只是「多/少一层 mip」这类小偏差；**要做的是**：跑完 `BakeFromCLI` 后 `git diff` 扫一遍 `.meta`，对「本来没 mip 却被打开」的那几张按包 I 的清单逐个核（`B139_I_纹理导入.md` §一 15-03 的 21/28 名单就是权威名单）。
7. ⚠️ **`ArtBaker.ApplyImportSettings` 现在会按真值表写 `anisoLevel`**（本轮补）。`WarpforgeVFX/Textures/*` **不在这两个方法管辖范围内**（`ApplyImportSettings` 只扫 `Resources/Art`）⇒ 那里的 `Debris_sprite` 是**手工改的 `.meta`**，将来谁也不会覆盖它（好）；反过来，若有人把 `ArtBaker` 的扫描根目录扩大，要记得它也在表里。
