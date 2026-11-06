# R4 · VFX Graph(3) / ComputeShader(24) / 剥源码 Shader(234) 逐件定性

> 会话 B105_R15（只读侦查）。判据全部来自 `D:/2/新解包资源/assets_full/`、原版 bundle 实读、
> 以及**我们工程自己的** `StreamingAssets/WarpforgeVFX/*.bundle`（运行时解析口）。
> ⛔ 没改任何既有文件；本件与 `资源/R4_定性.tsv`（261 行逐件表）是本轮唯一新增。
>
> **先看两句结论**：
> ① **三个族里真正「在用且还缺东西」的只有 Gargoyle Swarm 这一组**（1 VFX 资产 + 3 内核 + 1 材质 + 2 Texture3D）；
>    **Aeldari Portal / Dust 两套原版 VFX 资产、13 个内核、5 个输出材质早就在工程里**，卡在**没有 `com.unity.visualeffectgraph` 包**
>    ＋**5 个输出材质的 shader 悬空没有材质级重绑**（后者是本轮新查出来的）。
>
> ⚠️ **2026-11-05 更正（铁律 5，两句都已过期）**：
> · 原来写「**缺 3 个 Gargoyle Swarm 内核**」—— **实际是 16 份 ComputeShader 全在工程里**，其中正是 `_Gargoyle Swarm_ _System_ {Initialize Particle, Update Particle, OutputUpdate}` 三份（现读：`MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared/*.asset`，`find Assets -name '*.asset'` 后按首对象 `--- !u!72` 筛出 16 份）。错因 = 本表成文时还没拷进来，之后没人回来改这一行。
> · 原来写「**没有 `com.unity.visualeffectgraph` 包**（manifest / packages-lock / PackageCache 三处 0 命中）」—— **实际三处都有**：`MyGame/Packages/manifest.json:12` = `"com.unity.visualeffectgraph": "17.3.0"` · `packages-lock.json:178` 有条目 · `Library/PackageCache/com.unity.visualeffectgraph@b7de1dc238bb/` 在。错因 = 同上（本表写完之后才装的包）。
> · 另：`Gargoyle Swarm` 这份 VisualEffectAsset **工程内已有**（`MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared/Gargoyle Swarm.asset`，19,474 B，首对象 `--- !u!2058629509`）—— 本表 §二「在用·缺（工程内没有）」那一格也已不成立。
> ② **Shader 这一族 0 缺口**：**有具体消费者（效果 prefab / 战场材质表 / 代码按名取件）的 107 件 / 90 个名字，100% 都能从随包 bundle 取到原件**；
>    剩下的是「随包备用」「引擎自带」「第三方只登记在 GraphicsSettings」「原版菜单材质我们没消费」四类，
>    **没有一件是「要在用但没得用」**。

---

## 一、口径与「163」这个数

- `资源/R4_Shader_VFX_清单.tsv` 现读 **262 行 = 261 数据行**：`Shader` **234** · `ComputeShader` **24** · `VisualEffectAsset` **3**。
- **234 个 Shader 对象全部 `m_Script = null`**（`有HLSL源` 列 234/234 = 0），**全部有 `compressedBlob`**（`有DXBC` 234/234 = 1）。
  唯一名 **197**；其中 **174 个名字有 Ruri 反编译 `.shader` 源**（对象级 211/234）。
- 任务简报里的「Shader 163」与现表对不上 —— **最近似的是「在用件数」**（本件定性 `在用…` 合计 **161 件**：
  Shader 142 + ComputeShader 16 + VisualEffectAsset 3）。**口径以现表为准，⛔ 不要拿 163 当分母。**

## 二、分档统计（对象数 / 名字数）

### Shader（234 对象 / 197 名字）

| 分档 | 对象 | 名字 | 含义 |
|---|---:|---:|---|
| **在用·原件随包** | 107 | 90 | 工程里有具体消费者，且该名字在 `StreamingAssets/WarpforgeVFX/*.bundle` 里能按名取到原件 |
| **在用·代码常量/表引用** | 35 | 28 | 名字出现在 `.cs` 的字面量里（`WindowButton.GrayShaderName` 这类常量、`OriginalShaderBlendTable`、探针），无材质级消费者；同样**全部随包** |
| 备用·随包无消费者 | 19 | 6 | 随包带着，但工程里没查到任何消费者 |
| 不用·引擎/URP 自带 | 40 | 40 | `Hidden/Universal*` / `Hidden/Core*` / `Hidden/TerrainEngine*` / `Skybox/*` / `Sprites/*` / `UI/Default` / `Legacy Shaders/VertexLit` 等 |
| 不用·第三方包仅登记 GraphicsSettings | 20 | 20 | `UI Extensions/*` ×19 + `MeshBaker/*` ×3 等，**引用者只有 `globalgamemanagers#5/#13`（Always Included Shaders），0 内容消费者** |
| 判不了·原版菜单材质未消费 | 13 | 13 | 原版菜单材质在用，但那些材质我们没导；Ruri 有源码，将来菜单线若要原版材质可照抄 |
| **在用·缺** | **0** | **0** | —— |

### ComputeShader（24）

| 分档 | 数量 | 明细 |
|---|---:|---|
| **在用·VFX Graph 内核（工程内）** | **13** | Aeldari Portal 10 + Aeldari Dust 3，`!u!72 ComputeShader` 资产都在 `Assets/WarpforgeVFX/ArenaOriginalPrefabs/` |
| **在用·缺（VFX Graph 内核未导入）** | **0** | ⚠️ **2026-11-05 更正：原写 3，实际 0** —— 16 份 ComputeShader 全在工程里，这 3 份 `[Gargoyle Swarm] {OutputUpdate, Initialize Particle, Update Particle}` 在其中（`WarpforgeVFX/ArenaOriginalPrefabs/_shared/*.asset`，首对象 `--- !u!72`）。错因 = 成文时未拷入、之后未回改 |
| 不用·引擎/包自带 | 8 | VFX 包内部件 5（`VFXCopyBuffer` · `VFXPrefixSum` · `VFXFillIndirectArgs` · `UpdateStrips` · `Sort`）＋ URP/Core 2（`VrsTexture` · `ProbeVolumeSamplingDebugPositionNormal`，**PackageCache 实证在 URP/Core 包里**）＋ `GenSdfRayMap`（SDF 生成，包内件，本机两包都没搜到 → 随 VFX/URP 版本来） |
| 合计 | 24 | **「16」= 13 在工程内 + 3 缺**（= 三套 VFX 的全部内核） |

### VisualEffectAsset（3）

| 分档 | 数量 | 明细 |
|---|---:|---|
| **在用·原版资产已导入并接线** | 2 | `Aeldari Portal`（9 system）· `Aeldari Dust`（2 system）—— `.asset` + 内核 + 输出材质齐，且**已接进** `battlearenaaeldari__Scenario.prefab` / `battlearenaleviathan__Scenario.prefab` |
| **在用·缺（工程内没有）** | **0** | ⚠️ **2026-11-05 更正：原写 1（`Gargoyle Swarm`，2 system），实际工程内已有** —— `WarpforgeVFX/ArenaOriginalPrefabs/_shared/Gargoyle Swarm.asset`（19,474 B，首对象 `--- !u!2058629509`）。剩余的是**接线**（4 处 `m_Asset` 空 guid + 2 个 `Texture3D` + 输出 shader），不是资产本身 |

---

## 三、VFX Graph 逐件表

| 资产 | 类型 | 原版引用链（实数） | 我们工程里 | 定性 | 还缺什么 |
|---|---|---|---|---|---|
| **Aeldari Portal** | VisualEffectAsset | 原版 `GameObject/Aeldari Portal VFX`（battleprefabs 包）＋ `battlearenaaeldari` 场景；R4 记 6 个引用者 | `battlearenaaeldari/Aeldari Portal.asset`（48,587 B）+ **10 个内核 `.asset`**（33–134 KB）+ **4 个输出材质 `.mat`**；`VisualEffect` 已接在 Scenario prefab（`:58401/:58512`） | **在用·已导入** | ① `com.unity.visualeffectgraph` **没装**（manifest / packages-lock / PackageCache 三处 0 命中）⇒ ⚠️ **2026-11-05 更正：这一句不成立 —— 实际三处都有**（`MyGame/Packages/manifest.json:12` = `17.3.0` · `packages-lock.json:178` 有条目 · `Library/PackageCache/com.unity.visualeffectgraph@b7de1dc238bb/` 在）；错因 = 成文后才装的包、之后未回改本条。原意仍成立的部分 = `VisualEffect` 组件是否真的恢复渲染**尚未验过**（要起 Unity，属 **A0 判定**）；② **5 个输出材质的 `m_Shader` 悬空**（guid 指向原版 shader，工程里没有这个 guid）——现有重绑器只走 **`GetComponentsInChildren<Renderer>`**（`ArenaOriginalShaderRebind.cs:135/179`），**碰不到 VFX 资产引用的材质** ⇒ 就算装了包也渲不出正确外观（见 §七·1） |
| **Aeldari Dust** | VisualEffectAsset | `battlearenaaeldari` 场景（`VisualEffect_3557`）；R4 记 6 个引用者 | `_shared/Aeldari Dust.asset`（20,386 B）+ **3 个内核 `.asset`** + **1 个输出材质**；接在 Aeldari + Leviathan 两个 Scenario prefab | **在用·已导入** | 同上两条（包 + 材质级 shader 重绑） |
| **Gargoyle Swarm** | VisualEffectAsset | 原版 3 个 `VisualEffect` 组件：`GameObject/{Gargoyle Swarm Near, Mid, Far}`（battleprefabs 包）→ 落进 `EnvironmentalCondition Leviathan Sweeping Infestation.prefab` 三处；另 `Saim Hann Infinity Circuit Overload.prefab` 一处指向的是 **Aeldari Portal**，同样空 guid | ❌ 工程内 0 命中 | **在用·缺** | **9 件本地全部可取**：VFX 资产（19,474 B）· 3 内核（71,892 B 等）· 输出材质（803 B）· 2 个 `Texture3D`（SDF 31×31×64 · vectorfield 64³）· `40k_battle_fx_gargoyle.png` · 输出 shader（EP 那份 772 B 是 **Dummy**，⛔ 别直拷）。施工单 → `报告/B101_R3_资源接线施工单.md` §B；4 处 `m_Asset` 空的修法照同报告 B-4，但**它给的三个行号已漂**（`44011/80002/80129`）—— 我现读 = Leviathan `:34305 / :44178 / :83372`（3 处 guid 全 0）、Saim Hann `:168211`（1 处，指向 **Aeldari Portal** 的 pid） |

> **「我们工程里有没有等价实现」**：**没有**，也不需要 —— 三套都是拿原版资产直接跑；
> 若最终判定不装 VFX 包，`s 报告/B100_D3.md` C 组给的兜底是「照原版重建 ParticleSystem」，那是另一条路（需另开任务）。

## 四、ComputeShader 逐件表（24）

### 4·1 16 个 VFX Graph 内核

| 内核（原版名） | 套 | 工程内？ | 内核名 / 线程组 | 参数表（CB）可读？ |
|---|---|:--:|---|---|
| `[Aeldari Portal] [System] Initialize Particle` | Portal | ✅ `.asset` | CSMain / 64·1·1 | ✅ |
| `[Aeldari Portal] [System] Update Particle` | Portal | ✅ | CSMain / 64·1·1 | ✅ |
| `[Aeldari Portal] [System (1)] Initialize Particle` | Portal | ✅ | CSMain | ✅ |
| `[Aeldari Portal] [System (1)] Update Particle` | Portal | ✅ | CSMain | ✅ |
| `[Aeldari Portal] [System (3)] Initialize Particle Strip` | Portal | ✅ | CSMain | ✅ |
| `[Aeldari Portal] [System (3)] Update Particle Strip` | Portal | ✅ | CSMain | ✅ |
| `[Aeldari Portal] [System (4)] Initialize Particle` | Portal | ✅ | CSMain | ✅ |
| `[Aeldari Portal] [System (4)] Update Particle` | Portal | ✅ | CSMain | ✅ |
| `[Aeldari Portal] [System (5)] Initialize Particle` | Portal | ✅ | CSMain | ✅ |
| `[Aeldari Portal] [System (5)] Update Particle` | Portal | ✅ | CSMain | ✅ |
| `[Aeldari Dust] [System] Initialize Particle` | Dust | ✅ | CSMain | ✅ |
| `[Aeldari Dust] [System] Update Particle` | Dust | ✅ | CSMain | ✅ |
| `[Aeldari Dust] [System] OutputUpdate` | Dust | ✅ | CSMain | ✅ |
| `[Gargoyle Swarm] [System] Initialize Particle` | Gargoyle | ❌ | CSMain / 64·1·1 | ✅（读 `assets_full`：`initParamsConst` 48 B/3 参数） |
| `[Gargoyle Swarm] [System] Update Particle` | Gargoyle | ❌ | CSMain / 64·1·1 | ✅（`updateParamsConst` 64 B/3 参数；1 texture / 2 in / 2 out） |
| `[Gargoyle Swarm] [System] OutputUpdate` | Gargoyle | ❌ | CSMain / 64·1·1 | ✅（`$Globals` 2112 B + `updateParamsConst` + `UnityStereoViewBuffer`；3 in / 1 out） |

**「有没有可读源码 / 参数表」的准确回答**（对 24 个 ComputeShader 都成立）：

- **参数表：有**。typetree 的 `variants[].constantBuffers[]` 有 **名字 / byteSize / 参数条数**；
  `variants[].kernels[].uniqueVariants[]` 还有 **`threadGroupSize` · `textures` · `inBuffers` · `outBuffers` · `builtinSamplers` · `requirements`**。
- **编译产物：有**。同一个 `uniqueVariants[].code` 是**编译后的字节码**（Gargoyle 三件实读 1,760 / 2,476 / 5,388 B；DXBC 魔数 `44 58 42 43` = `DXBC`，另有 GLSL 变体）。
- **HLSL 源码：没有**。Unity 不在 bundle 里存 CS 源码；且 **Ruri 只反编译 `Shader`，对 `ComputeShader` 无效** ⇒ 要重建内核逻辑只能靠 DXBC/DXIL 反汇编，**本机目前没有 CS 反汇编入口**（`工具/disasm_dxbc.py` 只针对 shader）。这是 ComputeShader 与 Shader 的关键差别。

### 4·2 8 个引擎/URP 包件

| 名字 | 是什么 | 判据 |
|---|---|---|
| `VFXCopyBuffer` | VFX Graph 内部（9 kernel：CopyStructBuffer / InitDeadList / BatchCopy…） | 引用者 = `globalgamemanagers#20`（Always Included）；随 VFX Graph 包 |
| `VFXPrefixSum` | VFX Graph（5 kernel） | 同上 |
| `VFXFillIndirectArgs` | VFX Graph（1 kernel） | 同上 |
| `UpdateStrips` | VFX Graph（1 kernel `UpdateParticleStrip`） | 同上 |
| `Sort` | VFX Graph（12 kernel `BitonicSort*`） | 同上 |
| `VrsTexture` | URP/Core 的 VRS 资源 | **`Library/PackageCache/com.unity.render-pipelines.core@*/Runtime/Vrs/VrsRenderPipelineRuntimeResources.cs` 实命中** |
| `ProbeVolumeSamplingDebugPositionNormal` | URP 探针体积 debug | **`com.unity.render-pipelines.universal@*/Runtime/RenderPipelineResources/UniversalRenderPipelineDebugShaders.cs` 实命中** |
| `GenSdfRayMap` | SDF 生成（21 kernel：JFA / DistanceTransform…） | 引用者 0；本机 URP/Core + VFX 包内都没搜到 ⇒ 随包版本提供，**不属游戏内容** |

> **结论**：这 8 个**不需要我们搬运/复刻** —— 它们是引擎或包资源；我们缺 VFX 包时其中 5 个随包补齐。

---

## 五、Shader 逐档名单（234 对象 / 197 名字）

> 逐件（对象级、含 bundle / PathID / 消费者 / 随包 / UseOriginal / Replacements）→ **`资源/R4_定性.tsv`**（261 行）。
> 下面只列「需要人看的」档。

### 5·1 在用·原件随包（107 对象 / 90 名字）

覆盖来源三条（都可复算）：
1. **效果 prefab 的 `WarpforgeEffectBinder.wfMaterialDefs[].shader`** —— 975 个带 `shader:` 字段的 prefab 里 **69 个不同原版名**；
2. **战场材质表** `ArenaOriginalPrefabs/_shared/arena_shader_guid_map.tsv` —— **17 个名字**；
3. **代码按名取件**：`TryResolve("…")` 15 个名字 · `TryGetShader("…")` 4 个 · `Shader.Find("…")` 24 个（含 Unity 内建）。

**上述名字 100% 在随包 bundle 里**（`wf_shaders` 45 + `wf_shaders_extra` 42 + `wf_builtin` 15 + `wf_tmp_shaders` 4 + `wf_booster_shaders` 1 + `wf_booster_fxshaders` 6 + `wf_arena_shaders` 7 + `wf_arena_2` 4 + `wf_arena_tauviorla` 5 + `wf_arena_mirror` 3）。

> 另一条独立核验：**`WarpforgeShaderMap` 两张表的每一个键都随包**
> —— `UseOriginal` 65/65 在随包集合里，`Replacements` 32/32 也在。
> ⇒ **「自建替代」那 25 个 `.shader` 现在只是【bundle 不在场时的兜底】**，
> 正常态（`StreamingAssets/WarpforgeVFX/` 在）**一个都不会被用到**。这与两张表自己的注释一致，本轮首次用数据坐实。

### 5·2 备用·随包无消费者（19 对象 / 6 名字）

`Everguild/FX/Color Gradient` · `Everguild/Misc/URP Transparent Shadow Receiver` ·
`Hidden/UI/Default (UIEffect)` · `Shader Graphs/Nebula` · `TextMeshPro/Mobile/Distance Field` · `Unlit/ScrollingTextures`

> 不是缺陷：随包是按原版 shader 包**整包拷贝**来的。⚠️ 其中 `Hidden/UI/Default (UIEffect)` 只在 `wf_arena_mirror.bundle` 里，
> 是镜像链的附带件；`Shader Graphs/Nebula` 在 `wf_shaders.bundle` 里但工程里 0 引用。

### 5·3 不用·第三方包仅登记 GraphicsSettings（20 名字）

`UI Extensions/*` 19 个 + `MeshBaker/{AlbedoShader, NormalMapShader, Unlit/UnlitWithAlpha}` + `UI/Particles/Hidden`
—— **引用者全只有 `globalgamemanagers#5` 与 `globalgamemanagers#13`**（= Always Included Shaders 列表），
**没有任何 Material / prefab / 场景引用**。⇒ **可以明确判「内容没用」**（原版把它们塞进了 always-included，但没有内容用它）。

### 5·4 判不了·原版菜单材质未消费（13 名字）

`Everguild/UI/{Division Change, Greyscale Add, Pulsating, UI Glow SDF Noise, UI Distort, Additive, UI Clock, UI Border Highlight Pulse Alpha, Scan Lines Alpha Mask}` ·
`Shader Graphs/UI Container Main Menu` · `Everguild/FX/{FX Shine For Animation Normal, Genestealers Flood, Two passes blur}`

- **原版侧**：都有真实消费者（`bundle_menus_assets_all` 的材质；`Genestealers Flood` 在 Genestealers 战场场景）；
  **R4 的「我们有没有用」对它们写的是 `已用（数据表）（经引用者）` —— 那是【继承来的】，不能当「我们在用」的证据**（见 §七·3）。
- **我们侧**：工程 `.mat` / `.prefab` / 代码**都没有真正的消费者**（只有 3 处注释引用了名字：`CardView.cs:8298`、`MainMenuRuntime.cs:2773`、`RankedDivisionChangeWindow.cs:1336`，且都已给出**自建等价物**的落地方式）。
- **为什么判「判不了」而不是「不用」**：菜单线现在是**代码自建的壳**（`MenuDraw`/`ImageQuad` + 我们自己的材质），原版菜单材质根本没导进来 ⇒「原版要用、我们现在不用」。
  将来若菜单线要复刻原版材质，**这 13 个都有 Ruri 反编译源码**，可照抄重建（比 shader 反汇编省事）。

### 5·5 不用·引擎/URP 自带（40 名字）

`Hidden/Universal Render Pipeline/*`（Bloom / UberPost / TemporalAA / CopyDepth / XR/* …）·
`Hidden/Universal/CoreBlit*` · `Hidden/Core*` · `Hidden/TerrainEngine/*` · `Hidden/VoxelizeShader` ·
`Hidden/VFX/Empty` · `Skybox/{Procedural, Cubemap}` · `Sprites/{Default, Mask}` · `UI/Default` · `Legacy Shaders/VertexLit`

> 引用者几乎全是 `globalgamemanagers#5`（Always Included）＋ `sharedassets0.assets#1`（原版 URP 资产）。
> **我们的 URP 17.3.0 自己带这些**，不需要搬原版那一份。

---

## 六、在用的下一步（按「还差什么」）

| 分组 | 件数 | 状态 | 下一步 |
|---|---:|---|---|
| Shader 在用（原件随包） | 142 对象 / 118 名字 | **可解析、无缺口** | 只需要**真机 A/B**（e.g. `EffectSweepBatch` / `EffectCompare`）确认解析到的是原件、不是占位；**没有要补的资产** |
| ComputeShader 13（Aeldari） | 13 | 工程内 `.asset` | 与 VFX 资产同一条阻塞（装包 + 材质级 shader 重绑）；A0 判定由调度台做 |
| ComputeShader 3（Gargoyle） | 3 | 缺 | 按 `报告/B101_R3` §B 拷贝（本地可取；内核 `.asset` 直接拷，`code` 字段自带编译产物） |
| VisualEffectAsset 2（Aeldari） | 2 | 已导入接线 | ① 装/不装包（都行得通的路：装包 or ParticleSystem 兜底）；② **补一条材质资产级 shader 重绑** |
| VisualEffectAsset 1（Gargoyle） | 1 | 缺 | 同上 §B：9 件拷贝 + 4 处 `m_Asset` 空 guid + 2 个 Texture3D 重建（要跑 Unity）+ 输出 shader |

**Shader 侧「能不能从 DXBC 反出等价参数与 pass 状态」——能，且本机已验证**：

- **属性表 / 渲染状态**：`m_ParsedForm.m_PropInfo` + `m_SubShaders[].m_Passes[].m_State`（blend/zwrite/cull）在 typetree 里，工具 `工具/dump_shader.py`。
- **DXBC**：`Shader.compressedBlob` 解出后可读 **subshader tags / pass 列表 / `m_GrabPass` / 资源名（RDEF 明文）**。
  本轮实跑 `dump_shader_blob.py "Multi Ray" --passes`：1 个 SubShader、5 个 pass、`m_GrabPass=None`、84,568 B 字节码、
  16 个常量名（`_Color1..4` · `_BorderFadeMultiplier` · `_Border_Fade_XminXmax_YminYmax` · `_Remap_Color_Min_XY_Max_ZW` …）。
- **完整源码**：174 个名字有 Ruri 反编译 `.shader`（更省事）；**没有 Ruri 的 23 个 = 7 个 10 B 的 URP/XR 内部件 + 16 个 Unity 内建件**，
  两类都**不需要源码**（内建件我们直接随包编译原件）。
- 先例：工程里 25 个自建 shader（`Assets/WarpforgeVFX/Shaders/WF*.shader`）就是「按 DXBC 逐条重写」出来的。

---

## 七、顺手发现（新查出来的，按铁律 5·b 落账）

1. 🔴 **5 个 VFX 输出材质的 `m_Shader` 悬空，且现有重绑器管不到它们**（本轮新查）。
   - 证据：`ArenaOriginalPrefabs/_shared/Hidden_VFX_Aeldari Dust_System_Output Particle Unlit Quad.mat`
     里 `m_Shader: {fileID: 4800000, guid: f2642f618e317484b9b0a2930a6690b6, type: 3}`，该 guid 在工程 `*.meta` 里 **0 命中**（Portal 四条同理）。
   - 现有重绑 `ArenaOriginalShaderRebind` 只遍历 **`root.GetComponentsInChildren<Renderer>(true)`**（`:135` / `:179`），
     而 `ArenaOriginalShaderIndex.cs:66-68` 自己写着「这 5 个由 VFX Graph 资产引用 ⇒ 13 颗 prefab 的树里一个都碰不到，只能按材质资产级验」——
     **验（探针 B 组）有，重绑没有**。
   - 影响：**就算装了 `com.unity.visualeffectgraph`，Aeldari Portal/Dust 也因输出材质 shader 悬空而渲不对**。
   - 两条可选修法（写手可做）：① 加一个「材质资产级」重绑（按 `ArenaOriginalShaderIndex` 的 5 个名字 + `TryResolve` 给 `.mat` 赋 shader，
     注意别把 `.mat` 改脏 —— 至少要在运行时对克隆材质做）；② 或按 `B101_R3` §B-3 手写 6 个 `Hidden/VFX/...` shader。

2. ⚠️ **`报告/B101_R3` §B-3「6 个输出 shader 全缺」需要就地补一句适用范围**（铁律 5）。
   在【工程 `.shader` 资产】口径下它成立（`Assets/` 里确实 0 命中）；
   但**运行时按名取原件这条链上 6/6 都已经随包**（5 个在 `wf_arena_shaders.bundle`、`Gargoyle` 那个在 `wf_shaders_extra.bundle`）。
   ⛔ 不要因为那句就去手写 6 个 shader 而不知道「其实原件已经在包里」——先决定走「材质级重绑」还是「手写」，两条都行，别两条都堆。

3. ⚠️ **`R4_Shader_VFX_清单.tsv` 的「我们有没有用」列不能当「我们在用」的证据**：
   它对 75 条写 `无独立名字（经引用者）`、对 72 条写 `已用（数据表）（经引用者）` —— 全是**沿引用者继承**来的
   （生成器 `_tmp_view/wfres/r4_shaders.py` 的 fallback 分支）。它证明的是「**原版那一侧的引用者**在我们侧有用/没用」，不是「这个 shader 我们用了」。

4. ⚠️ **ComputeShader 没有 Ruri 这条退路**：`Ruri` 产物只覆盖 `Shader`（`ruri_out_1020_*` 里全是 `.shader`）。
   16 个 VFX 内核的「逻辑」只能从 `uniqueVariants[].code`（DXBC/DXIL）反汇编 —— **本机没有 CS 反汇编入口**。
   好消息：**VFX 内核根本不用我们自己写**（原件就是编译产物，直接拷 `.asset` 即可）。

5. ⚠️ **`WarpforgeShaderMap.Replacements` 与 `UseOriginal` 的键 100% 都已随包**（32/32、65/65），
   ⇒ 自建 shader 只在「bundle 不在场」时生效。这条本轮首次用数据坐实；**⛔ 别把自建 shader 当成「在用件」去验收**。

6. ⚠️ **20 个第三方 shader（UI Extensions / MeshBaker）只在 Always Included 列表里**，原版也没有内容引用它们。
   ⇒ 可以判「内容没用」，但**不能据此说「原版没有这个包」** —— 包在，只是没人用。

---

## 八、覆盖边界

- 本件**只覆盖** `R4_Shader_VFX_清单.tsv` 的 261 个对象，以及与本族直接相关的「我们侧」证据
  （随包 bundle 名表 / `WarpforgeShaderMap` / `WarpforgeShaderLoader` / `ArenaOriginalShaderIndex` / 效果 prefab 的 `shader:` 字段 / `.cs` 字面量）。
- **不覆盖**：① Shader 的**视觉正确性**（画面像不像）—— 那要真机 A/B，本件不做；
  ② 未在清单里的 Shader/ComputeShader（若有遗漏，需重跑 `_tmp_view/wfres/r4_shaders.py`）；
  ③ VFX Graph 的**运行时行为**（ParticleSystem 兜底 vs 装包，属 A0 判定）；
  ④ `d:/2` 只读，没说「原版包里有/没有」以外的任何推断。
- **没验**：① 装 `com.unity.visualeffectgraph` 后 Aeldari 是否真的恢复渲染（要起 Unity / 判定 A0）；
  ② 5 个输出材质的 shader 若走「原件」路线，VFX Graph 是否接受运行时换过 shader 的材质（要实测）；
  ③ Gargoyle 的 `40k_battle_fx_gargoyle.png` 是不是 flipbook（影响手写 shader 的 UV 动画，判不出）。

## 九、可复跑命令

```bash
# 1) 我们随包了哪些 shader（名字表）
D:/2/Warpforge_tools/py312/python.exe -I -X utf8 -c "import UnityPy,glob,os; [print((o.read_typetree().get('m_ParsedForm') or {}).get('m_Name',''), os.path.basename(f)) for f in glob.glob(r'D:/4/Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/*.bundle') for o in UnityPy.load(f).objects if o.type.name=='Shader']"

# 2) 某个 shader 的属性表 / pass 状态
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/dump_shader.py "Multi Ray"

# 3) 某个 shader 的 DXBC 资源名 / GrabPass / subshader tags
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/dump_shader_blob.py "Multi Ray" --passes

# 4) 本件逐件表
D:/4/Unity/资料/完全复刻/资源/R4_定性.tsv
```
