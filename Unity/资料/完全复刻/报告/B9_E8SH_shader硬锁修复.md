# B9 · E8SH · shader 硬锁修复（P0：`WFMatDef.shader` 被写成替身名的 **417 个块**）

> 执行代理 **E8SH** · 2026-10-26（第八十四会话）· 输入 = `J3SH` 的判据（本代理**没有**重新普查，只复核它给的硬证）。
> **走了「路 A」**（`WarpforgeShaderMap` 加「替身名 → 原名」反查），**没有动任何 prefab / 材质数据、没有跑全量重生成**。
> ⛔ 未跑 Unity（任何 `-executeMethod` / `*.Run` / `_run_8_checks.sh`）· ⛔ 未做截图或像素比较 · ⛔ 未动 git / 未改两张正本 / 未改主账表。
> 唯一另动的两个文件是 J3 自己的判据与报告（**铁律 5 就地更正**，逐条见 §四·3）。

---

## 一、结论（先看这段）

### 1.1 修了什么

| 层 | 改动 | 位置 |
|---|---|---|
| 反查表 | 新增 `SurrogateToOriginal`（**8 条**：替身名 → 原名）+ `SurrogateMaterialOverrides`（**2 条**：一个替身顶多族时按**材质基名**分流） | `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` **类尾** |
| 反查函数 | 新增 `OriginalNameFor(shaderName, materialName)` · `MaterialBaseName` · `IsSurrogateName` | 同上（类尾） |
| 消费者 | `WarpforgeEffectBinder.Build` 里 **4 处按原版名查表**改走反查：孤儿值闸 `OriginalProperties` · `InferBlend` · `InferCull` · `InferZTest`（外加 `ApplyDerivedParticleDefaults` 的原版默认值查询） | `WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs`（**类尾**新增 `OriginalShaderNameOf`，5 处调用**行数不变**） |
| 出声 | 是替身名而反查表里没有 ⇒ **报一次警告**（按 `shader\|材质基名` 去重）；正常换名 ⇒ 一条 `Log` | 同上 |
| 顺带修的真缺陷 | `InferCull` 的「按名字兜底」把 `Particles/Standard Unlit` 强判成 `0`(Off)，而该族 `_Cull` 是**活值** ⇒ 删掉那一条（判据 §二·4） | `WarpforgeShaderMap.InferCull` |
| 探针 | `BlendProbe` 先用反查再比（否则这 43 个基材质被**静默跳过**不判） | `WarpforgeArena1/Editor/BlendProbe.cs` |

### 1.2 「前后读数」（三格 + 孤儿值闸）—— 四格合计 **0 → 80**（共 172 格）

| 原版名（改后喂进函数的） | 块 | Blend | Cull | ZTest | `OriginalProperties` |
|---|---:|---|---|---|---|
| `Everguild/FX/Extra Color` | 304 | 材质驱动（不覆盖） | 材质驱动 | 材质驱动 | **21 条**（改前 null） |
| `Mobile/Particles/Additive` | 50 | **5/1/0** | **0** | **4** | **1 条** |
| `Mobile/Particles/Alpha Blended` | 3 | **5/10/0** | **0** | **4** | **1 条** |
| `Particles/Standard Unlit` | 1 | 材质驱动 | 材质驱动（**本轮改**，见 §二·4） | **4** | **30 条** |
| `Everguild/FX/Multi Ray` | 37 | 材质驱动 | 材质驱动 | 材质驱动 | **32 条** |
| `Everguild/FX/TrailShader_1` | 6 | **5/10/0** | **0** | **4** | **14 条** |
| `Everguild/FX/Particle Distortion Affect Transparents` | 6 | **5/10/0** | **2** | **4** | **14 条** |
| `Everguild/FX/Unlit UV scroll` | 5 | 材质驱动 | 材质驱动 | 材质驱动 | **28 条** |
| `Everguild/FX/TrailShader_Fading` | 3 | **5/10/0** | **0** | **4** | **13 条** |
| `Everguild/Matcap/Matcap Full Options` | 2 | 材质驱动 | 材质驱动 | 材质驱动 | **35 条** |

* **改前一律是「—/—/—/null」**（替身名在三张表里都查不到；`OriginalProperties` 返回 null ⇒ 孤儿值闸**不过滤**）。
* **改后 80 格有取数，剩下 92 格是「原版本来就是材质驱动 / 属性驱动 ⇒ 正确结果就是不覆盖」**，不是缺口。
* ⚠️ 这张表是**按 `.cs` 里的四张表 + 分支逐行复刻成 python 算出来的推演**（脚本 `d:/4/_tmp_view/e8sh/readout.py`，会把源码里的表重新解析一遍，**不依赖我记忆**），
  **不是实跑** —— 实跑口在 §三·3。逐条明细（43 个基材质逐个）在同名脚本的输出里，报告只放汇总。

### 1.3 孤儿值闸「从不过滤 → 过滤」到底会丢什么：**数值影响 = 0**

把 417 块记录到的名字 / 替身 `Properties` / 原版 `DeclaredProps` 三方一比（脚本 `analyze.py`）：

| 族 | 会被新闸丢掉 **且替身声明了** | 顶掉的记录值 vs 替身默认值 |
|---|---|---|
| `… Extra Color EC`（304 块） | `_Cutoff` `_SoftParticlesFadeDistance` | `_Cutoff` **304/304 块都是 0.5** = 替身默认 0.5；`_SoftParticlesFadeDistance` 记录值 **恒 1** = 默认 1 |
| `… Matcap/Matcap`（2 块） | `_APPLYAMBIENTCOLOR` `_Cutoff` `_ExtraAmbientColor` `_FogContribution` | 记录值 `0` / `0.5` / `(1,1,1,0)` / `1`，**逐格等于替身默认值** |
| `… Particles/Extra Color`（54 块） | 并集 19–20 条（`_SrcBlend/_DstBlend/_ZWrite/_Surface/_Cull/_ZTest/_Color/_EmissionColor/_SOFTPARTICLES/…`） | 见下 |
| 其余 5 族（MultiRay / UnlitUVScroll / TrailShader1 / TrailShaderFading / Distortion） | **0 条** | —— |

`… Extra Color` 那 54 块的 19–20 条是**真·死值**（原版 `Mobile/Particles/*` 只声明 `_MainTex`；`_Glossiness/_Metallic/_Mode/_Parallax/_UVSec/_Cutoff/_BumpScale` 是**内置 Standard 的残留**），
丢掉的 `_Color` 记录值 = **`(1,1,1,1)`** = 替身默认；`_EmissionColor` 记录 `(0,0,0,1)`，但替身那一行加法 **2026-09-18 已删除**（`WFParticlesExtraColor.shader:249` 留作对照）⇒ 不参与着色。
⇒ **净效果：状态那三格按原版真值写入（§1.2），孤儿值闸只是把死值挡住，画面数值不改。** 这条让本批的视觉风险集中在「三格」一格里，而不是三个来源同时变。

### 1.4 规模就地更正（J3 的 35 / 76 两个数都偏低）

**43 个基材质 · 76 个不同材质名（其中 34 个带 `__<hash>`）· 417 个 `WFMatDef` 块 · 343 个 prefab。**
反查覆盖 **417/417 = 0 未覆盖**（脚本逐块跑过 `OriginalNameFor` 后断言「结果不再是 `WarpforgeVFX/…`」）。

### 1.5 验收状态（本代理能给的 / 不能给的）

| 项 | 结果 |
|---|---|
| **秒级类型检查** `TMPDIR=/tmp/wf_E8SH bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（改完全部文件后又跑了一次，仍 0/0） |
| **`ruleprobe play check`** | 结算指纹 **`16db33fa…` ≠ 基线 `af8e8178…`**，但 **与本批无关**：`ruleprobe.csproj` 只编 `RuleEngine/**`（`EnableDefaultCompileItems=false` + 白名单），**根本不编 `WarpforgeVFX/**`** ⇒ 纯 shader 层改不动它。差异是 **2 张卡**（`BL18` / `UM18`）—— 正是另一路代理本轮在改的光环卡（`Auras.RefreshInPlayNumericAndKeywords` 那条）。② 哨兵（基线内容 vs 源码常量）**通过**。 |
| **画面** | **没量**（不许做像素比较）。孤儿值闸与三格都会改变 343 个 prefab 的材质状态 —— 需要调度台起 Unity 做一次 A/B，见 §三·2。 |

---

## 二、逐条证据

### 二·1 规模 / 归属：两路硬证（**不是按名字猜**）

**路①：`assets_full` 的 PPtr。** 先把「PathID → Shader 名」全局建表（202 个带 PathID 的 `Shader/*.json`），再用 J3 给的 8 组 PPtr 自校验 —— **8/8 全对**：

| PPtr（`m_FileID`, `m_PathID`） | 解出的原名 | J3 的原名 | 一致 |
|---|---|---|---|
| (0, −8640852129309073691) | `Everguild/FX/Multi Ray` | 同 | ✅ |
| (0, 8442397235057580651) | `Everguild/FX/TrailShader_1` | 同 | ✅ |
| (0, 684306895731615801) | `Everguild/FX/TrailShader_Fading` | 同 | ✅ |
| (0, 4341849964770849447) | `Everguild/Matcap/Matcap Full Options` | 同 | ✅ |
| (7, −418309281451410444) | `Everguild/FX/Extra Color` | 同 | ✅ |
| (7, 5091587426579848444) | `Everguild/FX/Unlit UV scroll` | 同 | ✅ |
| (7, −3785611105477734646) | `Everguild/FX/Particle Distortion Affect Transparents` | 同 | ✅ |
| (2, 2855819893914472268) | `Mobile/Particles/Additive` | 同 | ✅ |

**路②：prefab 内部的兄弟块**（比 PPtr 更直接 —— 同一次导出、同一个材质，一份写原名一份写替身名）：

| 材质基名 | 原名块数 : 替身名块数 | 判定 |
|---|---|---|
| `Laser_Stream_MultiRay 1` | 2 : 2 | 原名 = `Everguild/FX/Multi Ray` |
| `LightningTrail` | 138 : 137 | 原名 = `Everguild/FX/Extra Color` |
| `RippleSubtle Distort` | 205 : 4 | 原名 = `… Particle Distortion Affect Transparents` |
| `Flash1` | 14 : 2 | 原名 = `Mobile/Particles/Additive` |
| **`IceTrail`** | **1 : 1**（**同一个 prefab** `PathOfTheWarrior_Projectile.prefab` 里 `IceTrail` 与 `IceTrail__605375ed` 并排） | 原名 = **`Particles/Standard Unlit`** |
| **`smokesoft_dark`** | **55 : 3** | 原名 = **`Mobile/Particles/Alpha Blended`** |

**逐族现读数（本批的真实规模）**：

| 替身名 | 块 | prefab | 不同名 | 带 `__hash` | 基名 | 判定的原版名 |
|---|---:|---:|---:|---:|---:|---|
| `WarpforgeVFX/Particles/Extra Color EC` | 304 | 275 | 26 | 12 | **14** | `Everguild/FX/Extra Color` |
| `WarpforgeVFX/Particles/Extra Color` | 54 | 53 | 14 | 7 | **8** | 6×`Mobile/Particles/Additive` · 1×`Particles/Standard Unlit` · 1×`Mobile/Particles/Alpha Blended` |
| `WarpforgeVFX/FX/MultiRay` | 37 | 31 | 19 | 7 | 12 | `Everguild/FX/Multi Ray` |
| `WarpforgeVFX/FX/TrailShader1` | 6 | 4 | 4 | 2 | 2 | `Everguild/FX/TrailShader_1` |
| `WarpforgeVFX/FX/Distortion` | 6 | 6 | 4 | 2 | **2** | `… Particle Distortion Affect Transparents` |
| `WarpforgeVFX/FX/UnlitUVScroll` | 5 | 5 | 5 | 2 | 3 | `Everguild/FX/Unlit UV scroll` |
| `WarpforgeVFX/FX/TrailShaderFading` | 3 | 2 | 2 | 1 | 1 | `Everguild/FX/TrailShader_Fading` |
| `WarpforgeVFX/Matcap/Matcap` | 2 | 1 | 2 | 1 | 1 | `Everguild/Matcap/Matcap Full Options` |
| **合计** | **417** | **343（并集）** | **76** | **34** | **43** | —— |

> 🔴 **J3 的两处归属错，本代理已就地更正（铁律 5）**：
> ① `WarpforgeVFX/Particles/Extra Color` **不是**「6 个材质全归 `Mobile/Particles/Additive`」—— 它**一个替身顶 3 个原版族**（`IceTrail` = `Particles/Standard Unlit`、`smokesoft_dark` = `Mobile/Particles/Alpha Blended`）
> ⇒ **光按 shader 名反查会把这 2 个归错族**，所以本批的反查表多了「替身名 + 材质基名」那一层。
> ② 基材质数 35 → **43**（EC EC 9→14、Extra Color 6→8），块数 76 → **417**（76 是「不同材质名」那个数，J3 把两者记混了）。

### 二·2 反查表为什么这样定（`__hash` 变体的来源）

那 76 个不同名里 **34 个带 `__<hex>` 后缀**（`EffectExporter` 给「同名不同源材质」加）。**变体来源 = 与基名同源**，两条证据：
① 带后缀的名字，其基名在别的 prefab 里也以**原名**出现过（上表 路②）；
② 后 417 块按 `OriginalNameFor(shader, 去掉 hash 的基名)` 跑完，**417 块 100% 落到 10 个原版名之一**，没有一块落空。
⇒ 「只修 35（或 43）个基材质够不够」这个问题的答案是：**代码侧按名字/基名反查一次全中**；但**数据侧**（路 B）要按块改，就是 417 处。

### 二·3 三格与孤儿值闸的代码证据（改在哪一行）

| 消费点 | 改前 | 改后 |
|---|---|---|
| `WarpforgeEffectBinder.cs:410` | `OriginalProperties(d.shader)` | `OriginalProperties(OriginalShaderNameOf(d))` |
| `:442` | `InferBlend(d.shader)` | `InferBlend(OriginalShaderNameOf(d))` |
| `:461` | `InferCull(d.shader)` | `InferCull(OriginalShaderNameOf(d))` |
| `:473` | `InferZTest(d.shader)` | `InferZTest(OriginalShaderNameOf(d))` |
| `:554` | `ApplyDerivedParticleDefaults(m, d.shader, …)` | `…(m, OriginalShaderNameOf(d), …)` |
| `:380` | `TryResolve(d.shader, …)` | **一字未改**（解析路径不动，见 §三·1） |

`:380` 不动是**故意的**：改它 = 这 417 块**整体换渲染源**（替身 → 原件），那是 343 个 prefab 的画面变化，必须 A/B（`WFBIND_FORCE_BUILTIN` sweep）。类内注释里写死了「⛔ 别顺手改」。

### 二·4 `InferCull` 那一条：为什么必须删掉 `Particles/Standard Unlit`

`InferCull` 末尾有一段「按名字兜底」，其中一条是 `n == "particles/standard unlit"` → 返回 `0`（Off）。**这条是错的**，两条判据：

1. **该族的 `cull` 是属性驱动**（本函数自己的注释 `:499-501` 写着：`pass0` 是 `gpuProgramID=-1` 的空 pass，真正渲染 pass 是 `pass2`、`cull` 读的是 `_Cull`）—— 那 `CullMap` 里**故意没有它**是对的，但兜底又不该反过来猜。
2. **原版：6 个该族材质里 `_Cull`=0 的 5 个、`_Cull`=2（Back）的 1 个**，那 1 个就是 `IceTrail`：
   `bundle_battleprefabs_vfxandmisc_assets_all/Material_3773910547246591993.json` 的 `m_SavedProperties.m_Floats` 里 **`["_Cull", 2.0]`**（另有 `["_SrcBlend", 5.0]`）。

⇒ 删掉那一条后：5 个行为不变（材质本来就是 0），第 6 个恢复原版的 **Back**。**只影响「原版名 = Standard Unlit 且走我们替身」的材质 —— 本轮之前是 0 个**（28 块 Standard Unlit 都解析到原件，`HasProperty` 那一步虽会写，但写的是原件 shader 上的活值……⚠️ 见 §五·2 那条），**本轮起 `IceTrail` 那 1 块会走到**（= 硬锁修复带来的第一例）。

### 二·5 🔴 更正 J3 P1 第 4 条：`Legacy Shaders/Particles/Alpha Blended` **不缺 `_TintColor`**

用仓里的 `工具/disasm_dxbc.py "Legacy Shaders/Particles/Alpha Blended" --stage ps` 实读（`Warpforge_unitybuiltinassets.bundle`，**只有 1 个 PS 变体**），**全部 5 条指令**：

```
add    r0.xyzw, v1.xyzw, v1.xyzw      ; 2 × 顶点色
sample r1.xyzw, v2.xyxx, t0.xyzw, s0
mul    r0.xyzw, r0.xyzw, r1.xyzw      ; × 主贴图
mov_sat o0.w, r0.w
mov    o0.xyz, r0.xyzx
ret
```

**`dcl_constantbuffer` 一条都没有 ⇒ 这一族的编译体根本不读 `_TintColor`** ⇒ 我们替身不乘它是**照原版**（`WFLegacyParticlesAlphaBlended.shader` 文件头 2026-10-02 那条判据是对的，⛔ **别按 J3 那条去加**）。
`_TintColor` 只是**声明了但不用**（`m_PropInfo` 里有）—— 「声明过」≠「会采」，这正是铁律里「属性表里没有的硬编码」的镜像坑。

### 二·6 🔴 更正 J3 P1 第 3 条：`Anim Alpha Blended` 不是「滚 UV」，是**两帧插值**

同一支工具实读该族（2 个 PS 变体：无软粒子 / 有软粒子）：

```
; PS（无软粒子支）—— 双采样 lerp
sample r0.xyzw, v2.zwzz, t0.xyzw, s0     ; 下一帧 UV（TEXCOORD1.zw，粒子系统写）
sample r1.xyzw, v2.xyxx, t0.xyzw, s0     ; 当前帧 UV（TEXCOORD0.xy）
add    r0.xyzw, r0.xyzw, -r1.xyzw
mad    r0.xyzw, v3.xxxx, r0.xyzw, r1.xyzw ; lerp(cur, next, blendFactor)
add    r1.xyzw, v1.xyzw, v1.xyzw          ; 2 ×（顶点色 × _TintColor）
mul    r0.xyzw, r0.xyzw, r1.xyzw
mov_sat o0.w, r0.w ; mov o0.xyz, r0.xyzx

; VS：`_TintColor` 在这里乘进去、`_MainTex_ST` 也在这里做
mul o1.xyzw, v1.xyzw, cb0[2].xyzw                 ; o1 = 顶点色 × _TintColor
mad o2.xyzw, v2.xyzw, cb0[3].xyxy, cb0[3].zwzw    ; o2.xy = uv × ST.xy + ST.zw
```

⇒ 正确的描述是：**`2 × 顶点色 × _TintColor × lerp(tex(uv), tex(uv2), blend)`**，UV 由**粒子系统**（Texture Sheet Animation）按帧写进 TEXCOORD0/1，**shader 自己不滚**。
所以替身真正缺的是「**两帧插值 + `_TintColor`(VS) + ×2 + `_InvFade` + `ColorMask RGB`」，不是「UV 滚动」**。方案见 §四·2。

---

## 三、没查清的部分 / 必须由调度台起 Unity 才能收口

1. **后果①「永远走不到原件」没修**（本批只修 ②③）。两种收口路，都要人来定：
   * **数据侧（推荐）**：让 `WFMatDef.shader` 回到原名 —— 要重跑导出（⛔ 本批不许），改的是 **417 处**（按「替身名 + 材质基名」查表可批量做）；
   * **代码侧**：把 `TryResolve(d.shader)` 也换成 `OriginalShaderNameOf(d)` —— **1 行**，但等于把这 417 块整体切到原件渲染，**必须先 A/B**。
2. **画面没量**。预期变化面 = 343 个 prefab，但按 §1.3 的核算，**孤儿值闸这一路数值影响为 0**，真正会变的只有三格：
   `Extra Color` 族 54 块（混合 5/10→5/1 或保持、Cull→0、ZTest→4）+ 另外 4 族 23 块（Blend/Cull/ZTest 由「表值」写进 —— 目标 shader 没声明那些属性时 `SetFloat` 是 no-op）。
   A/B 建议：`WFBIND_FORCE_BUILTIN` 全量 sweep（`EffectCompare` / `EffectSweepBatch`）**在本批之前/之后各跑一次**，比对「预期变好的族」有没有反着变。
3. **实跑读数口**（都要起 Unity，本代理不许跑）：
   * `BlendProbe.Run` —— **本批已改**成先反查再判，跑它正好把这 43 个基材质的三格从「跳过不判」变成「真判」。报告落在 `d:/4/_tmp_view/blend_report.tsv`。
   * `ShaderResolveProbe.Run` —— 白名单 64 条走原件的现状不变（本批没动白名单）。
   * 自检宿主：动的是 `WarpforgeVFX/Runtime/*`（共用件）⇒ 按铁律 12 应跑**全部 12 条**（本代理不跑，留给同步点）。
4. **`WarpforgeVFX/UnlitAmbient` 没法填反查表**：它一个顶 **3** 个原版族（`UnlitAmbient` / `… Emissive Flickker` / `Unlit Wind`），而当前 **0 块硬锁** ⇒ 没有「兄弟块」证据可依。已做兜底：`OriginalShaderNameOf` 对**任何**「是替身名但表里没有」的情况**报一次警告**，所以将来它被硬锁时会叫出来、不会静默退化成「三格不覆盖」。
   ⚠️ 另有两处仍在比 `d.shader` 字面值：`IsBaseUnlitAmbient`（`:294`）与 `_EmissiveFlickerOn`（`:490`）—— 同理当前无影响，但**将来 UnlitAmbient 被硬锁时它们会静默失效**（§五·3）。
5. **`WarpforgeVFX/Matcap/Matcap` 同时顶 `Matcap With Texture`**（后者 4 材质 / 70 prefab，当前 **0 块硬锁**）⇒ 名字反查只对 `Full Options` 成立。将来若出现，会归错族（`OriginalNameFor` 会返回 `Full Options`，那两条「多出来的属性被闸挡掉」的判定就反了）。
6. **根因仍未查清**（J3 §三·3 的遗留）：谁把替身名写进 `d.shader`。本轮的新线索见 §五·1（关键字集合的形状）。

---

## 四、改动清单

### 四·1 `.cs`（3 个文件，行尾全部保持原样：两个是 CRLF、`BlendProbe` 是 LF）

| 文件 | 增/删 | 内容 |
|---|---|---|
| `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` | **+126 / −2**（616→740 行） | ① 类尾新增 `SurrogateToOriginal`(8) · `SurrogateMaterialOverrides`(2) · `MaterialBaseName` · `IsSurrogateName` · `OriginalNameFor`（含判据大段注释）② `InferCull` 删 `\|\| n == "particles/standard unlit"`（−1/+2 行注释）③ `InferBlend` 的 doc 行就地补一句「参数必须是原版名」（**零行数变化**）|
| `WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs` | **+48 / −5**（735→778 行） | 类尾新增 `OriginalShaderNameOf(WFMatDef)` + 去重表 `SurrogateNameReported`；`Build` 里 **5 处调用改为走它（这些行本身零增删）** |
| `WarpforgeArena1/Editor/BlendProbe.cs` | **+6 / −3**（152→155 行） | 先 `OriginalNameFor` 再 `InferBlend`；报告首列与失败文案改用原版名 |

* **新成员全部追加在类尾**（铁律 N9：插在中间会让下游行号整体 +N）；`WarpforgeEffectBinder.Build` 内部的 **5 处以「就地换实参」方式改，行数不变** ⇒ `:397` 之后的行号**一个都没动**。
* 已 `grep` 确认这些符号只落在这三个文件里，**都在别人正在改的文件清单之外**。

### 四·2 没有实现、但给了方案的（P1，**本批刻意不做实现**）

| 项 | 状态 | 为什么本批不做 | 方案要点 |
|---|---|---|---|
| `Particles/Standard Unlit` 单开替身（缺 22 条真实属性） | **方案**（J3 已给，本轮补强） | 新 shader 无法在本批编译验证（不许起 Unity）⇒ 渲品红的代价比现状差 | 按 `Material_3773910547246591993` 这类材质实读的 39 个属性补：软粒子 4 格 + flipbook（`_FlipbookMode`/`_Mode`）+ 颜色模式（`_ColorMode`/`_ColorAddSubDiff`）+ `_BlendOp` + 扰动 3 格 + `_EmissionEnabled/_EmissionMap`。⚠️ `[Toggle(KEY)]` 的 KEY **别**在 CBUFFER 里声明同名变量（否则开关键字就 `#define`、编不过、渲品红） |
| `Legacy Shaders/Particles/Anim Alpha Blended` 单开替身 | **方案（按 §二·6 更正后）** | 同上（新 shader） | `2 × 顶点色 × _TintColor × lerp(tex(uv0.xy), tex(uv1.zw), uv2.x)` + `_InvFade` 软粒子 + `ColorMask RGB`；混合 5/10、Cull Off、ZTest 4 全烘死。**⛔ 不是「UV 滚动」** |
| `Legacy Shaders/Particles/Alpha Blended` 补 `_TintColor` | **❌ 不做（判据被推翻）** | 编译体不读它（§二·5）⇒ 加了就是**凭空多一个乘项** | 无 |
| `UI/Additive` 单开 UI 替身（Stencil 组 + ZTest 0） | 未动 | 本包 0 材质，本批 P0 之外 | J3 §六 第 5 条 |
| 解析路径 / 数据侧修 417 块 | 未动 | 见 §三·1 | —— |

### 四·3 判据 / 报告文件（**铁律 5 就地更正**，都不是正本）

| 文件 | 改了什么 |
|---|---|
| `判据/B9_shader近似映射_对账.tsv` | ① 硬锁小节标题里的「35 个材质 / 54 张卡」→ 补现读数（43 基材质 / 76 名 / **417 块** / 343 prefab）② `Extra Color EC` 材质数 9→**14**、`Distortion` 1→**2** ③ `Mobile/Particles/Additive` 那行的「还差什么」补上「这一个替身还顶 2 个族，必须按材质基名分流」④ `Legacy … Alpha Blended` 那行的「缺 `_TintColor`」整句改写成反汇编结论 ⑤ `Legacy … Anim Alpha Blended` 那行的「UV 动画（序列帧）」改写成「两帧插值」。**改完校验：全文件每行仍是 11 列** |
| `报告/B9_J3SH_shader近似对账.md` | §1.2 补「规模现读」更正块；§五·6 补「`__hash` 变体来源已查清 + 417/417 覆盖」；§六 序 1 标注「代码侧已做、数据侧待同步点」 |

### 四·4 账（`账/_parts/`）：**本批没有补丁，理由写在下面**

本批改的是**我们自己的映射/装配代码**与**资产事实**，账表的 `原版类 / 方法(完整签名)` 两列**要求落到一个原版方法**上 —— 而这批的对造物是**资产级**的（`Material.m_Shader` PPtr · `Shader.m_ParsedForm.m_Name` · prefab 里的 `WFMatDef` 块），账的格式不建模资产行。
强行写一条会把「我们的 `InferBlend`」塞进「原版方法」列 = 伪账 ⇒ **不写**。
这些证据已经落在 `判据/B9_shader近似映射_对账.tsv`（硬锁小节，本轮已就地更正）与本报告里；若调度台要立「资产/映射层」的新账区，本报告 §二 可直接当逐条判据用。

---

## 五、🔴 顺手发现（只报不改，除 §二·4 那条已修）

1. **417 个硬锁块记录的关键字集合是「URP 的形状」**：`_SURFACE_TYPE_TRANSPARENT` 417/417、`_SOFTPARTICLES` 4、`_ANIMUVS`/`_USEMASK` 各 1；而**同族的原名块**带的是 `SoftParticles_ON` / `BlendAlpha` / `Clip_ON` / `FrameBlend_ON` / `VertLight_OFF` 一整套。
   ⇒ 这 417 块里**至少有一部分是从 URP 材质转过来的**（或材质换 shader 后 `m_ValidKeywords` 留着旧关键字）—— 这是 J3 §三·3「根因未查清」的**新线索**，可用来定位「谁写进去的」。
2. **`InferCull` 的兜底在 28 块 Standard Unlit 上可能一直在改活值**：那 28 块原名块里，`InferCull("Particles/Standard Unlit")` 会返回 0，而 `m.HasProperty("_Cull")` 在**原件**上是真的 ⇒ 解析到原件时也会把材质的 `_Cull` 改写成 Off。本轮只删了兜底分支（= 这条路径不再发生），**但没有量过它过去有没有把画面改坏**（6 个原版材质里 5 个 `_Cull` 本来就是 0，大概率无害）。⚠️ 与 2026-10-02 D 那条修（`Mobile/Particles/*` 的 cull 烘死 Off）**不冲突**，那条在 `mobile/particles` / `legacy shaders/particles` 分支里，没动。
3. **`WarpforgeEffectBinder` 两处仍在比 `d.shader` 字面值**：`IsBaseUnlitAmbient`（`:294`，比 `Everguild/UnlitAmbient`）、`_EmissiveFlickerOn`（`:490`，比 `… Emissive Flickker`）。反查表里没有 `WarpforgeVFX/UnlitAmbient`（见 §三·4），所以**当前无影响**；将来那族被硬锁，这两支会静默失效。要不要现在就改成反查，取决于要不要为它先查清「一个替身顶 3 族」的分流规则。
4. **`Legacy Shaders/Particles/Additive` 是否真乘 `_TintColor` 没复核**：`WFLegacyParticlesAdditive.shader` 文件头说「Additive 那支还多乘 `_TintColor`」。同族、同法一次反汇编就能定（本批只验了 `Alpha Blended` 与 `Anim Alpha Blended` 两支）—— 建议下一轮顺手做掉。
5. **`WarpforgeShaderMap.cs` 的 `CullMap` 注释写「现读 79 条」，实读 78 条**（J3 §五·1 已报，本轮复核成立：`CullMap.Count` 逐条数 = 78）。未改（在生成器产物的注释里，且不影响行为）——建议改那句注释或让 `gen_shader_blend.py` 自己写数字。
6. **替身 `WFParticlesExtraColor._EmissionColor` 依然是「凭空加的属性」**（那个 shader 文件头 2026-09-18 的 ⏳ 还挂着）：本批只是让孤儿值闸开始挡它（记录值被丢、吃默认白），而**着色里那一行加法 2026-09-18 已删** ⇒ 当前无影响，但那个属性本身仍该删。
