# E 组 · shader 算式 · 块3：Unity 内置粒子 / UI shader 族（11 个）

> 🔴 **2026-09-19 晚 · 前提已被推翻（结论仍有效、只是不再需要照它改代码）**：
> 这份文档的前提是「**这 11 个内置 shader 只能继续用自建近似**」，于是给出「A 档不动 / B 档 2 个加颜色乘子 /
> C 档 4 个整条算式另写」的方案。**实测发现：原件一直在原版包里、而且在 URP 里照常渲染** ——
> 其中 **8 个名已改走原件**（`WarpforgeShaderMap.UseOriginal` 21 → 29），自建近似在这 8 个名上**下岗**
> ⇒ **下面那套「给自建加乘子」的做法不用做了**。
> **正本 = `资料/普查产出_0919/内置shader原件_加载崩溃_实测.md`**（含 7 个变体实测 + 复现命令）。
> ⚠️ **文档里那张算式表本身没错**（DXBC 反汇编是实读的，逐 shader 的差异仍然是真的）—— 它现在的用途是
> **复核原件行为**、以及**留给那两个暂缓的大头**（`Extra Color` 741 · `Particle Distortion` 233，仍走自建）。
> ⚠️ `Particles/Additive` 仍然是死条目（原版包里根本没有）。

> 2026-09-19 晚。**目的**：`EffectExporter.cs:52` 的 `ShaderMap` 把 22 个原版 shader 全指到同一个
> `WarpforgeVFX/Particles/Extra Color*`，而 2026-09-19 我们从自建 shader 里**删掉了一次 `_Color` 乘法**
> （判据：原版 `Everguild/FX/Extra Color` 的 DXBC 里 `_Color` 只当 LOD bias）⇒ **7 条效果从偏亮翻成偏暗**。
> 说明「**这个原版 shader 本来就该乘颜色常量**」这件事**逐 shader 不一样**。
> 这份文档把那 11 个内置 shader 的 **ps 算式**逐个反汇编出来，给出 **A / B / C 三档判定**。

**判据出处（全部可重跑）**

```bash
# 工具（2026-09-19 修好：之前声明了 bundle 列表却没用，内置 shader 一律误报「命中 0」）
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
  d:/4/Unity/工具/disasm_dxbc.py "<shader 名片段>" --stage ps
```

- 字节码所在包（实测，**不是猜的**）：`Warpforge_unitybuiltinassets.bundle`
  （`D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`，106 KB）
  —— 里面**正好 15 个** shader，= 原版「Always Included」那一批。
  `UI/Additive` 例外，在 **`shaders_assets_all.bundle`**（游戏自己的 shader 包）。
- 该包 **84 个 bundle 全扫过一遍**（按 Shader 对象名），确认下面 §二·4 的「不存在」是真不存在。
- 常量缓冲的**变量名**（`cb0[N]` 是谁）：来自 `SerializedPass.progFragment.m_CommonParameters`
  —— `m_ConstantBufferBindings[].m_Index` = DXBC 里的 `cbN`，
  `VectorParams[].m_Index` = **字节偏移**（/16 = 寄存器号），名字走 `m_NameIndex` → `Pass.m_NameIndices`。
  ⚠️ **只有一部分 shader 保住了这张表**；`Particles/Standard Unlit` 的 `m_ConstantBuffers` 是**空的**
  （`m_ShaderIsBaked=True`）⇒ 它的 `cb0[5]` **名字定不出来**（见 §四）。
- RDEF chunk **全部被剥掉**（`.c` 里的变量名读不到）——查过：这些段的 chunk 组合只有
  `ISGN / OSGN / SHDR`（或 `SHEX`），没有 `RDEF`。
- 混合状态来自 `SerializedShaderState.rtBlend0`（`SerializedShaderState` 上是 `rtBlend0..rtBlend7`，
  **不是** `rtBlend[0]`）；数值按 `UnityEngine.Rendering.BlendMode`：`0 Zero · 1 One · 2 DstColor · 3 SrcColor ·
  5 SrcAlpha · 10 OneMinusSrcAlpha`（后两条口径也由 `WFParticlesExtraColor.shader:45-46` 的注释反证）。

**记号**

| 记号 | 意思 |
|---|---|
| `tex` | `_MainTex` 采样结果（4 分量） |
| `vColor` | 顶点色（DXBC 里是 `v0` / `v1` / `v2`，随 shader 不同；每节写明） |
| `cbN[M]` | 常量缓冲 N 的第 M 个寄存器（**颜色常量**） |
| `lerp(a,b,t)` | `a + (b-a)·t` |

**判定分档**（照派活时的定义）

- **A** = ps **不含颜色常量**，就是 `tex × vColor` 一类 ⇒ 现在的自建 shader 口径对
- **B** = ps **含颜色常量乘子** ⇒ 这个原版 shader 需要一份「会乘颜色常量」的自建 shader
- **C** = 更复杂（预乘 / 双采样插值 / 加常量 / 取整 / 顶点色 ×2 …）⇒ 光 `tex × vColor` 顶不上

---

## 一 · 汇总表

| # | shader | DXBC 里的 ps 算式 | 判定 | ps 段数 | 混合 |
|---|---|---|---|---|---|
| 1 | `Mobile/Particles/Alpha Blended` | `o = tex × vColor` | **A** | 1 | `SrcAlpha OneMinusSrcAlpha` |
| 2 | `Mobile/Particles/Multiply` | `o = lerp(1, tex×vColor, tex.a×vColor.a)` | **C** | 1 | `Zero SrcColor` |
| 3 | `Particles/Standard Unlit` | `o.rgb = tex.rgb × cb0[5].rgb × vColor.rgb`（SM5 集是 `cb0[7]`） | **B** | 16（8 影子 + 8 颜色） | 材质 `_SrcBlend/_DstBlend` |
| 4 | `Particles/Additive` | —— **原版包里没有这个 shader** | **✗ 不存在** | 0 | —— |
| 5 | `UI/Additive` | `o.rgb = (tex + cb0[4]).rgb × cb0[2](=_Color) × a`；预乘 | **C** | 4 | `One One` |
| 6 | `UI/Default` | `o.rgb = (tex + cb0[3]).rgb × cb0[2](=_Color) × a`；预乘 + alpha 取整 | **C** | 4 | `One OneMinusSrcAlpha` |
| 7 | `Legacy Shaders/Particles/Additive` | `o = 2 × vColor × cb0[2](=_TintColor) × tex` | **B** | 2（硬/软） | `SrcAlpha One` |
| 8 | `Legacy Shaders/Particles/Alpha Blended` | `o = 2 × vColor × tex`，`o.w=sat(…)` | **A** | 2（硬/软） | `SrcAlpha OneMinusSrcAlpha` |
| 9 | `Legacy Shaders/Particles/Alpha Blended Premultiply` | `o = tex × vColor × vColor.a` | **A** | 2（硬/软） | `One OneMinusSrcAlpha` |
| 10 | `Legacy Shaders/Particles/Anim Alpha Blended` | `o = lerp(帧A, 帧B, t) × 2 × vColor`（双采样翻页） | **C** | 2（硬/软） | `SrcAlpha OneMinusSrcAlpha` |
| 11 | `Mobile/Particles/Additive`（对照） | `o = tex × vColor` | **A** | 1 | `SrcAlpha One` |

⇒ **需要「会乘颜色常量」的自建 shader 的，只有 2 个：`Legacy Shaders/Particles/Additive` 与
`Particles/Standard Unlit`。** 11 个目标里 1 个不存在、剩下 10 个 = **B 档 2 个 · A 档 4 个**（乘了反而错）·
**C 档 4 个**（另外一整套算式，光加个乘子救不回来）。

---

## 二 · 逐 shader

### 1. `Mobile/Particles/Alpha Blended` —— 判定 **A**

- 出处：`Warpforge_unitybuiltinassets.bundle`，`clens=[[854],[1158]]`，段 `#0 vs 784B` / `#1 ps 368B`
  （**只有 1 个 ps**，没有 SOFTPARTICLES / flipbook 分支）。
- 属性表（assets 自己写的）：`props = ['_MainTex']` —— **没有 `_Color`、没有 `_TintColor`**。
- 混合：`Blend SrcAlpha OneMinusSrcAlpha`，`colMask=15`，`QUEUE=Transparent`。

```
ps_4_0
dcl_constantbuffer —— 一条都没有（这个 ps 不读任何常量缓冲）
sample r0.xyzw, v1.xyxx, t0.xyzw, s0      // v1 = uv
mul    o0.xyzw, r0.xyzw, v0.xyzw          // v0 = 顶点色
```

- **顶点色参与** ✔（`v0` 整 4 分量）。**alpha** = `tex.a × vColor.a`，不做预乘、不取整。
- **ps 里没有任何 `cbN`** ⇒ 判 **A**：自建 shader 的 `tex × IN.color` 口径正确。

### 2. `Mobile/Particles/Multiply` —— 判定 **C**

- 出处：同 §1 包。段 `#0 vs 820B` / `#1 ps 488B`。属性表只有 `['_MainTex']`。
- 混合：**`Blend Zero SrcColor`**（= `dst × src`），`colMask=15`。

```
sample r0.xyzw, v1.xyxx, t0.xyzw, s0                   // tex
mul    r1.x,    r0.w, v0.w                             // t = tex.a × vColor.a
mad    r0.xyzw, r0.xyzw, v0.xyzw, l(-1,-1,-1,-1)       // tex×vColor − 1
mad    o0.xyzw, r1.xxxx, r0.xyzw, l(1,1,1,1)           // o = 1 + t·(tex×vColor − 1)
```

即 **`o = lerp(1, tex×vColor, tex.a×vColor.a)`**（把乘积往白色插值，配合 `Zero SrcColor` 做「带 alpha 的乘算」）。

- **不含颜色常量**（ps 无 `cbuffer`），但**算式不是 `tex × vColor`** ⇒ 判 **C**。
  ⚠️ 拿现在的自建 shader（`tex × vColor` + `SrcAlpha/OneMinusSrcAlpha` 之类）去顶它，
  **不是「差一个乘子」，是整条算式和混合模式都不一样**。
  （另注：`EffectExporter.InferFromShader` 对名字含 `multiply` 的返回 `(DstColor, Zero)`，
  与原版 `Zero SrcColor` **数学等价**：`src·dst = dst·src`。）

### 3. `Particles/Standard Unlit` —— 判定 **B**（本块最大牌的一个）

- 出处：同包。`clens=[[5659],[12972]]` → **`p0` 的 36896 字节里装着全部 `DXBC` 段，共 28 段**；
  `p1`（52036 字节）里 **`DXBC` 数为 0**（另一种容器，本块没解）。
- 28 段 = **12 vs + 16 ps**；按 chunk 名分 **`SHDR`（SM4，`*_4_0` 14 段）** 与 **`SHEX`（SM5，`*_5_0` 14 段）**。
  ps 的 16 段 = **8 个"影子类"（只写深度，`mov o0.xyzw, l(0,0,0,0)`）** + **8 个"颜色类"**。
  每个编译集（SM4 / SM5）= 6 vs + 8 ps。
- 属性表 30 条，4 分量颜色有：`_Color(1,1,1,1)` · `_EmissionColor(0,0,0,1)` · `_ColorAddSubDiff(0,0,0,0)` ·
  `_SoftParticleFadeParams` · `_CameraFadeParams`。
- 关键字表（`m_KeywordNames`）里确实有 `_COLOROVERLAY_ON` / `_COLORCOLOR_ON` / `_COLORADDSUBDIFF_ON`。
- 混合**不写死**：主 pass（Pass[2]）= `Blend [_SrcBlend][_DstBlend]` / `ZWrite [_ZWrite]` / `Cull [_Cull]`
  → **由材质驱动**（`ShaderMap` 把它指到自建 shader 是合适的，自建 shader 也走 `[_SrcBlend][_DstBlend]`）。
  另两个 pass：Pass[0] 带 `LIGHTMODE=GRABPASS`（`src=One dst=Zero`, `colMask=15`）、Pass[1] 影子
  （`src=One dst=Zero`, `colMask=14`）。

**颜色类 ps（SM4 那一套 4 个变体，`#20/#21/#24/#25`）**

```
sample      r0.xyzw, v2.xyxx, t0.xyzw, s0      // v2 = uv
mul         r0.xyz,  r0.xyzx, cb0[5].xyzx      // ← 颜色常量（4 分量，这里只用 rgb）
mul         o0.xyz,  r0.xyzx, v1.xyzx          // v1 = 顶点色（COLOR, 掩码 xyz）
mov         o0.w,    l(1.000000)               // 变体之一：alpha 直接写 1
```
另一种变体（`#21`）输入 `COLOR` 掩码是 `xyzw`，且不写死 alpha：
```
sample r0.xyzw, v2.xyxx, t0.xyzw, s0
mul    r0.xyzw, r0.xyzw, cb0[5].xyzw           // ← 同一颜色常量，alpha 也参与
mul    o0.xyzw, r0.xyzw, v1.xyzw               // o.a = tex.a × cb.a × vColor.a
```
**SM5 那一套（`#22/#23/#26/#27`）逐条相同，只是寄存器号换到 `cb0[7]`**（`CB0[8]` 窗口）：
```
sample_indexable(texture2d)(float,float,float,float) r0.xyz, v2.xyxx, t0.xyzw, s0
mul r0.xyz, r0.xyzx, cb0[7].xyzx
mul o0.xyz, r0.xyzx, v1.xyzx
mov o0.w,   l(1.000000)
```

**变体差异（4 个颜色变体 = 2×2）**

| 维度 | 取值 | 观测量 |
|---|---|---|
| 顶点色的 alpha 用不用 | 不用（`COLOR` 掩码 `xyz`，`o0.w=1`）／ 用（掩码 `xyzw`，`o0.w = tex.a·cb.a·vColor.a`） | Input signature 的 `Mask` 列 + `mov o0.w, l(1.0)` 有没有 |
| 有没有 `TEXCOORD3` 输入 | 有／无（**声明了但 `Used` 列是空的，ps 不读**） | Input signature |

⚠️ **两套编译集（SM4/SM5）里，这 4 个颜色变体的算式完全一样，都乘同一个颜色常量** ——
**没有找到「不乘颜色常量」的颜色变体**。

⚠️ **`cb0[5]`（SM5 是 `cb0[7]`）是谁：没定名。** 判据链：
`$Globals` 绑在 **cb0**（`BufferBinding(m_Index=0, m_NameIndex=1)` + `m_NameIndices=[('$Globals',1),('_MainTex',0)]`），
但 **`m_ConstantBuffers` 是空的**（`m_ShaderIsBaked=True`）⇒ **寄存器→变量名这张表被剥了**。
可说的只有：它是**唯一的 4 分量颜色乘子**，属性表里 4 分量颜色只有那 5 个，
且 560 个用它材质的 `_Color` 里 **369 个是白、191 个不是**（`2.0 / 2.828 / 4.0 / 31.99 / 766.99 …`）
⇒ **如果它是 `_Color`，删掉这个乘法对 191 个材质是实打实的变暗**。**但不许把它写成已知。**

**顶点着色器（顺带查到，值得记一笔）**

- SM5 那套里带 `SV_InstanceID` 的 vs（`#17/#19`，"PROCEDURAL_INSTANCING_ON"）多了一段：
```
add r2.xyzw, v2.xyzw, l(-1,-1,-1,-1)          // v2 = 顶点色
mad r2.xyzw, cb0[5].xxxx, r2.xyzw, l(1,1,1,1) // 1 + (vColor−1)·cb0[5].x
mul o1.xyzw, r0.xyzw, r2.xyzw                 // r0 = 从 structured buffer(t0,56B) 解码的粒子色
```
  即 **`o.color = 粒子色 × lerp(1, 顶点色, cb0[5].x)`**；`cb0[5].x` 是个标量插值因子，
  **名字同样没定**（同一条 `m_ConstantBuffers` 被剥）。**这条只在实例化变体里出现**，
  非实例化的 vs 是裸的 `mov o1.xyzw, v2.xyzw`。
  ⚠️ **观测到的分布**（`SHDR`/`SHEX` chunk 名 + Input signature 的 `SV_InstanceID`，逐段核过）：
  带 `SV_InstanceID` 的 vs **全落在 SM5（`SHEX`）那一套**（影子类 `#4-#7`、主类 `#17/#19`），
  SM4（`SHDR`）那一套（`#0-#3` / `#16` / `#18`）**一个都没有**。
  **这个不对称我没能解释**（`m_SubPrograms` 被剥，关键字↔变体的表不在资产里）。
  ⇒ 我**没能**把 `_COLOROVERLAY_ON / _COLORCOLOR_ON / _COLORADDSUBDIFF_ON` 对上任何一段代码：
  **查不到**（见 §四）。

### 4. `Particles/Additive` —— **原版包里没有这个 shader**（判定：✗ 不存在）

- `ShaderMap` 里有这条（`EffectExporter.cs`）：`{ "Particles/Additive", "WarpforgeVFX/Particles/Extra Color*" }`。
- **84 个 bundle 全扫**（按 Shader 对象名汇总），含 `particle` 的只有：
  `Mobile/Particles/{Additive, Alpha Blended, Multiply}` ·
  `Legacy Shaders/Particles/{Additive, Alpha Blended, Alpha Blended Premultiply, Anim Alpha Blended}` ·
  `Particles/Standard Unlit` · URP 的 `Universal Render Pipeline/Particles/{Unlit, Simple Lit, Lit}`。
  **没有 `Particles/Additive`**。
- `Warpforge_unitybuiltinassets.bundle`（= 原版「Always Included」那批）里正好 15 个 shader，
  `Particles/Additive` 不在其中；其它包也没有。
- ⇒ **这条 `ShaderMap` 是死条目**（大概是照 Unity 老文档/旧版名字抄的）。
  `Particles/Additive` 是 Unity 4 时代的旧名字，5.x 之后叫 `Legacy Shaders/Particles/Additive`。
  ⚠️ **不改 ShaderMap，只是记下来**（本轮只写这一份文档）。

### 5. `UI/Additive` —— 判定 **C**（`_Color` 在**顶点着色器**里乘）

- 出处：**`shaders_assets_all.bundle`**（游戏自己的包，**不是** builtin 那个）。
  它**不是 Unity 内置 shader** —— 属性表是 UI 那套（`_MainTex / _Color / _Stencil* / _ColorMask / _UseUIAlphaClip`），
  但混合是加算。**共 4 份实例**（`shaders_assets_all` ×2 + 我们工程 `wf_shaders.bundle` ×2），
  4 份的 ps 大小**逐段相同**（488/564/688/764）⇒ 同一份东西。
- 段：`#0..#3 vs 852B×4` / `#4..#7 ps 488/564/688/764`。
- 混合：**`Blend One One`**（真加算），`colMask=0`（**不写颜色掩码**），`ZTest unity_GUIZTestMode`。

**PS（4 个变体，`#4`）**

```
ps_4_0
dcl_constantbuffer CB0[5], immediateIndexed
sample r0.xyzw, v2.xyxx, t0.xyzw, s0            // v2 = uv
add    r0.xyzw, r0.xyzw, cb0[4].xyzw            // ← cb0[4] = _TextureSampleAdd（下面有硬证据）
mul    r0.xyzw, r0.xyzw, v1.xyzw                // v1 = 顶点色（**已经含 _Color**，见 VS）
mul    o0.xyz,  r0.wwww, r0.xyzx                // 预乘：rgb × a
mov    o0.w,    r0.w                            // 输出 alpha
```

- 变体：`#5` = 多一条 `_UseUIAlphaClip` 的 `discard`；`#6/#7` = 多 `UNITY_UI_CLIP_RECT`
  （`ge r0.xy, v3.xyxx, cb0[5].xyxx` / `ge r0.zw, cb0[5].zzzw, v3.xxxy` —— 拿 `cb0[5]` 当裁剪矩形）。

**`cb0[4]` = `_TextureSampleAdd` 是硬证据**（资产自己的 CB 参数表）：
`Pass.m_NameIndices=[('$Globals',1),('_MainTex',0),('_TextureSampleAdd',2)]`，
`PS cb0 = $Globals size=96B(6 regs) partial=True` → `vec _TextureSampleAdd reg 4 (byte 64) dim=4`
—— 与指令里的 `cb0[4].xyzw` **逐一吻合**。

**VS（`#0`）—— `_Color` 在这里**

```
vs_4_0
dcl_constantbuffer CB0[4] / CB1[4] / CB2[21]
...
mul    o1.xyzw, v1.xyzw, cb0[2].xyzw     // o1 = 顶点色 × cb0[2]   ← 这个常量是 _Color
mad    o2.xy,   v2.xyxx, cb0[3].xyxx, cb0[3].zwzz   // cb0[3] = _MainTex_ST（.xy/.zw 用法）
```
- `cb0[2]` 判为 **`_Color`** 的依据：① 它在 ps/VS 里都是「**4 分量整读**」，且乘的是**顶点色**，
  UI shader 里这只能是 tint；② 同一 CB 里 `_TextureSampleAdd` 已被资产定在 **reg 4**、
  `_MainTex_ST` 在 **reg 3**（VS 里 `.xy/.zw` 用法的只有它）⇒ 剩下 reg 2 只能是 `_Color`
  （该 shader 9 个属性里 `_Stencil*`/`_ColorMask`/`_UseUIAlphaClip` 都不进 HLSL 常量缓冲）。
- ⇒ **`_Color` 的乘法在 VS，ps 里没有 `_Color`**。

**判定 C**：算式 = `(tex + _TextureSampleAdd) × vColor × _Color`，再做**预乘**、输出 alpha；
**拿 `tex × vColor` 顶它 = 丢 `_TextureSampleAdd`、丢预乘、并且丢 `_Color`**。

**⚠️ 附带查到的一处不一致（与算式无关，但会直接改亮度）**：
原版混合是 **`One One`**，而 `EffectExporter.InferFromShader("UI/Additive")` 走的是
`n.Contains("additive") → (SrcAlpha, One)`（`EffectExporter.cs:823-824`）。
ps 输出**已经是预乘的**（`o.rgb = rgb × a`），再让 SrcFactor 乘一次 alpha ⇒ **按 a² 变暗**。
（`ApplyRenderState` 只在材质有 `_SrcBlend/_DstBlend` 时才照搬，UI 那套属性里没有 ⇒ 一定走推断分支。）

### 6. `UI/Default` —— 判定 **C**（同上，多两条细节）

- 出处：`Warpforge_unitybuiltinassets.bundle`。段 `#0..#3 vs 1300B×4` / `#4..#7 ps 608/684/776/852`。
- 混合：`Blend One OneMinusSrcAlpha`，`colMask=0`，`ZTest unity_GUIZTestMode`。
- 属性表：`_MainTex / _Color(Tint) / _Stencil* / _ColorMask / _UseUIAlphaClip`（9 条）。

**PS（`#4`）**

```
ps_4_0
dcl_constantbuffer CB0[4], immediateIndexed
mul    r0.x,   v1.w, l(255.000000)          // v1 = 顶点色（已含 _Color）
round_ne r0.x, r0.x
mul    r0.w,   r0.x, l(0.003922)            // a = roundtrip(alpha) —— 8bit 量化，不是原值
sample r1.xyzw, v2.xyxx, t0.xyzw, s0        // v2 = uv
add    r1.xyzw, r1.xyzw, cb0[3].xyzw        // ← cb0[3] = _TextureSampleAdd（资产表：reg 3 / byte 48）
mul    r0.xyzw, r0.xyzw, r1.xyzw            // × 顶点色
mul    o0.xyz,  r0.wwww, r0.xyzx            // 预乘
mov    o0.w,    r0.w
```
- 变体：`#5` = 加 `_UseUIAlphaClip` 的 `discard_nz`（判据 `a·tex.a − 0.001 < 0`）；
  `#6/#7` = 加 `UNITY_UI_CLIP_RECT`（`add r0.xy, -cb0[4].xyxx, cb0[4].zwzz` → `cb0[4]` 是裁剪矩形，
  `CB0[5]` 窗口）。
- `cb0[3] = _TextureSampleAdd`：硬证据同上（`nameIndices` 第 3 项 + `vec _TextureSampleAdd reg 3 (byte 48)`）。

**VS（`#0`）**：与 UI/Additive 同构，`mul o1.xyzw, v1.xyzw, cb0[2].xyzw` ⇒ **`_Color` 同样在 VS**
（这里 `_MainTex_ST` 在 `cb0[5]`、`cb0[4]` 是 `±2e10` 夹过的裁剪矩形、`cb0[6]` 是屏幕尺寸那一路）。
⇒ **PS 里没有 `_Color`。**

**判定 C**：`o = (tex + _TextureSampleAdd) × vColor(with _Color) × roundedAlpha`，预乘。
比 UI/Additive 多的两条：**alpha 过了一遍 `×255 → round → /255`**；混合是 `One OneMinusSrcAlpha`。
**⚠️ 同一处不一致**：`InferFromShader("UI/Default")` 三个关键字都不命中 ⇒ 落到兜底
`(One, Zero, zwrite=1, transparent=false)`（`EffectExporter.cs:831`）⇒ **会把 UI 图当不透明画**。
（我只读了这张表，**没查 UI 材质走不走这条导入链** —— 见 §四。）

### 7. `Legacy Shaders/Particles/Additive` —— 判定 **B**

- 出处：builtin 包。段 `#0 vs 784B` / `#1 vs 1180B` / `#2 ps 484B` / `#3 ps 824B`
  ⇒ **1 个 shader、2 个 ps**：`#2` = 硬，`#3` = `SOFTPARTICLES_ON`。
- 属性表：`_TintColor=(0.5,0.5,0.5,0.5)` · `_MainTex` · `_InvFade=(1.0,0.01,3.0,0.0)`。
- 混合：**`Blend SrcAlpha One`**，`colMask=14`（**不写 alpha**），`QUEUE=Transparent`。

**PS `#2`（硬）**

```
ps_4_0
dcl_constantbuffer CB0[3], immediateIndexed
add    r0.xyzw, v1.xyzw, v1.xyzw           // 2 × vColor   （v1 = 顶点色；PS 里 COLOR 是第 2 个输入，故是 v1）
mul    r0.xyzw, r0.xyzw, cb0[2].xyzw       // × 颜色常量  ← 就是它
sample r1.xyzw, v2.xyxx, t0.xyzw, s0
mul    r0.xyzw, r0.xyzw, r1.xyzw
mov_sat o0.w,  r0.w
mov     o0.xyz, r0.xyzx
```

**`cb0[2] = _TintColor` —— 这条是硬证据**（资产自己的 CB 参数表，这个 shader 没被剥）：
```
Pass0 nameIndices = [('$Globals', 0), ('_TintColor', 1)]
PS cb0 = $Globals  size=80B(5 regs) partial=True
      vec _TintColor   reg 2 (byte 32) dim=4
```
⇒ `cb0[2]` 在 DXBC 里逐字对上 `_TintColor`。**这是本块唯一一个「常量名 + 寄存器号都坐实」的 B 档**。

**PS `#3`（软）**：算式同上，alpha 多一段深度淡出 —— 读深度图 `t0`（`s1` 采样器），
`cb1[7].z / cb1[7].w` 是投影参数，`cb0[4].x` 是淡出距离（按 `m_PropInfo` 的 `_InvFade=(1.0,0.01,3.0,0)`
判为 **`_InvFade`**；参数表 `partial=True` 没列它）：
```
div    r0.xy, v3.xyxx, v3.wwww
sample r0.xyzw, r0.xyxx, t0.xyzw, s1
mad    r0.x, cb1[7].z, r0.x, cb1[7].w
div    r0.x, l(1.0), r0.x
add    r0.x, r0.x, -v3.z
mul_sat r0.x, r0.x, cb0[4].x      // ← _InvFade
mul    r0.w, r0.x, v1.w
```

- **判定 B**（含颜色常量乘子）。而且**数值上不能省**：2 × `_TintColor`，材质实测
  `_TintColor` = `1.0`(36) / `0.5`(23) / `(1,1,1,0.5)`(22) / `0.5147`(7) / `0.772`(8) …
  ⇒ **净乘子随材质在 0.5×~2× 之间**，忽略它一半的效果亮度直接差一倍。
- ⚠️ **对接线的坑**：原版这条颜色叫 **`_TintColor`**，我们的自建 shader 只有 **`_Color`**；
  `EffectExporter.MapProp` 只映射 `_MainTex→_BaseMap` / `_Color→_BaseColor`（`EffectExporter.cs:805-815`），
  `_TintColor` 会走 `default: return p` ⇒ 目标材质 `HasProperty("_TintColor")` 为假 ⇒
  **`_TintColor` 值静默丢掉**（复制循环里 `continue`）。

### 8. `Legacy Shaders/Particles/Alpha Blended` —— 判定 **A**

- 段 `#0 vs 796B` / `#1 vs 1192B` / `#2 ps 436B` / `#3 ps 792B`（硬/软）。
- 属性表**也写着 `_TintColor`**，但 **ps 一次都没读它**（编译产物里没有 cbuffer）——
  属性只是声明了没用，编译器把它删了。
- 混合 `SrcAlpha OneMinusSrcAlpha`，`colMask=14`。

```
ps_4_0
add    r0.xyzw, v1.xyzw, v1.xyzw      // 2 × vColor
sample r1.xyzw, v2.xyxx, t0.xyzw, s0
mul    r0.xyzw, r0.xyzw, r1.xyzw      // 没有 cbN
mov_sat o0.w,  r0.w
mov     o0.xyz, r0.xyzx
```
- **没有颜色常量** ⇒ **A**。但注意**顶点色被乘了 2**（Unity 老写法）——
  自建 shader 若只写 `tex × vColor`，这一族会**暗一半**（`_TintColor` 不参与，所以没有别的东西补回来）。
  软粒子变体 `#3` 用 `cb0[4].x` 做深度淡出（`_InvFade`），**不是颜色**。

### 9. `Legacy Shaders/Particles/Alpha Blended Premultiply` —— 判定 **A**

- 段 `#0 784` / `#1 1180` / `#2 ps 396` / `#3 ps 752`。属性表 `_MainTex` + `_InvFade`（**没有 `_TintColor`**）。
- 混合 **`Blend One OneMinusSrcAlpha`**（预乘），`colMask=14`。

```
ps_4_0
sample r0.xyzw, v2.xyxx, t0.xyzw, s0
mul    r0.xyzw, r0.xyzw, v1.xyzw      // × vColor
mul    o0.xyzw, r0.xyzw, v1.wwww      // × vColor.a ← 预乘
```
- **不含颜色常量** ⇒ **A**。**顶点色不乘 2**（与 §8 不同！）。软粒子变体只说 alpha。

### 10. `Legacy Shaders/Particles/Anim Alpha Blended` —— 判定 **C**

- 段 `#0 924` / `#1 1320` / `#2 ps 612` / `#3 ps 968`。属性表同 §8（含未使用的 `_TintColor`）。
- 混合 `SrcAlpha OneMinusSrcAlpha`，`colMask=14`。

```
ps_4_0
sample r0.xyzw, v2.zwzz, t0.xyzw, s0        // 第 2 帧（uv 在 TEXCOORD1.zw）
sample r1.xyzw, v2.xyxx, t0.xyzw, s0        // 第 1 帧（uv 在 TEXCOORD0.xy）
add    r0.xyzw, r0.xyzw, -r1.xyzw
mad    r0.xyzw, v3.xxxx, r0.xyzw, r1.xyzw   // lerp(帧1, 帧2, v3.x)
add    r1.xyzw, v1.xyzw, v1.xyzw            // 2 × vColor
mul    r0.xyzw, r0.xyzw, r1.xyzw
mov_sat o0.w,  r0.w
```
- **不含颜色常量**（无 cbuffer），但**双采样 + 插值**（`v3.x` = 翻页帧间系数）
  ⇒ 判 **C**：`tex × vColor` 顶不上（**丢一整帧**）。软粒子变体同样带 `_InvFade` 深度淡出。

### 11. `Mobile/Particles/Additive`（对照）—— 判定 **A**（复现基准）

- 段 `#0 vs 784B` / `#1 ps 368B`，属性表 `['_MainTex']`，混合 `Blend SrcAlpha One`。
```
ps_4_0
sample r0.xyzw, v1.xyxx, t0.xyzw, s0
mul    o0.xyzw, r0.xyzw, v0.xyzw
```
- 与交接里那条基准**逐字一致**（368 字节、两行指令）⇒ 基准可靠，可继续引用。
- 注意它和 §1 的 `Alpha Blended` **ps 二进制完全相同**（都是 368B），**差别只在混合状态**。

---

## 三 · 对接线（`ShaderMap` / 自建 shader）的影响

1. **该乘颜色常量的只有 2 个**：`Legacy Shaders/Particles/Additive`（`_TintColor`，已坐实）与
   `Particles/Standard Unlit`（`cb0[5]`/`cb0[7]`，名字未定但**一定是颜色乘子**）。
   现在自建 shader 里**一次 `_Color` 都不乘** ⇒ 用这两个 shader 的效果**偏暗**，与「7 条翻暗」自洽。
2. **反过来，多乘也不行**：§1/§8/§9/§11 四个（Mobile 两个 + Legacy AB 两个）ps 里
   **一个 `cbN` 都没有**，给它们加 `_Color` 只会把亮的搞更亮。
3. **顶点色 ×2 是一条独立的坑**：`Legacy Shaders/Particles/{Additive, Alpha Blended, Anim Alpha Blended}`
   三条在 ps 里把顶点色**乘 2**；`Alpha Blended Premultiply` 与 Mobile 那两条**不乘**。
   自建 shader 目前是 `tex × IN.color`（无 ×2）⇒ 这三条**天然暗一半**，与 `_Color` 无关。
4. **`Mobile/Particles/Multiply` 与 `Legacy Shaders/Particles/Anim Alpha Blended`** 不是「差个乘子」，
   是**算式不同**（lerp/双采样）；`UI/Default`、`UI/Additive` 还多 `_TextureSampleAdd`、预乘、alpha 取整，
   并且 **`_Color` 挂在 VS 上**（ps 里查不到 `_Color`）。
5. **`Particles/Additive` 这条 `ShaderMap` 是死条目**（原版包 84 个全扫，没有这个名字）。
6. `Particles/Standard Unlit` 的混合/深度/剔除是**材质驱动**的
   （`Blend [_SrcBlend][_DstBlend]` / `ZWrite [_ZWrite]` / `Cull [_Cull]`），与自建 shader 的口径一致 ✔。

---

## 四 · 这次没查到的（**不许当已知用**）

| 没查到 | 卡在哪 | 下一步能怎么查 |
|---|---|---|
| `Particles/Standard Unlit` 的 `cb0[5]`/`cb0[7]` **变量名** | 该 shader 的 `progFragment.m_CommonParameters.m_ConstantBuffers` 是**空的**（`m_ShaderIsBaked=True`）；`RDEF` chunk 也**被剥**（chunk 只有 `ISGN/OSGN/SHDR\|SHEX`） | ① 跑原版游戏读该材质的实际常量（要加探针）；② 下 Unity **6000.2.6f2**（bundle 头里的版本，实测 `UnityFS 5.x.x6000.2.6f2`）的 `builtin_shaders` 源码对寄存器顺序 |
| 同上，VS 里 `lerp(1, vColor, cb0[5].x)` 的 `cb0[5].x` **是谁** | 同上 | 同上 |
| **关键字 → 变体** 的对应（`_COLOROVERLAY_ON` 等到底编没编） | `SerializedProgram.m_SubPrograms` 为空、`m_ParameterBlobIndices` 只剩索引 ⇒ **对照表不在资产里** | 只能按程序内容分类（本文就是这么做的）；要逐关键字点名得跑实况 |
| `UI/Additive` / `UI/Default` 的 `cb0[2]` 是不是 `_Color` | 是**推断**（4 分量整读 + 乘顶点色 + 同 CB 里 `_TextureSampleAdd`/`_MainTex_ST` 已被资产定在别的寄存器）—— 资产表里**没有** `_Color` 这一行 | 找一个没被剥的同类 UI shader（`Everguild/UI/Card ImageUI Simple` 等）看它的 `$Globals` 表 |
| UI 材质到底走不走 `EffectExporter.ApplyRenderState` | 只读了那张表，**没查 UI 材质的导入路径** | 查 `EffectExporter` 的遍历入口 / `Rendering/` 那边的 binder |
| `clens[1]` 那 52 KB（`Particles/Standard Unlit`）是什么 | 里面 `DXBC` 数为 0，本文没解 | 读它的头几个字节判容器（DXIL/其它 API），与本块无关 |

**顺带更正一条口径**：`disasm_dxbc.py` 里的 `BUNDLES` 表**含**这两个包，但那 11 个目标里
**9 个只在 `Warpforge_unitybuiltinassets.bundle`**，`UI/Additive` 只在 `shaders_assets_all.bundle`——
以后搜 shader 先扫这两个。
