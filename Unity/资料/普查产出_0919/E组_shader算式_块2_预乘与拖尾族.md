# E 组 · 原版 shader **像素着色器算式** · 块 2「预乘与拖尾族」（7 个）

> 2026-09-19 · 只读子代理产出。数据源：原版 bundle 直读（`D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/*.bundle`，UnityPy py312，**未启动 Unity**）。
> 工具：`工具/disasm_dxbc.py`（ctypes 调 Windows 自带 `d3dcompiler_47.dll` 的 `D3DDisassemble`）。
> 复跑：`PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/disasm_dxbc.py "<名字片段>" --stage ps|vs`
> 属性/混合状态一栏引自 `资料/普查产出_0917/shader属性表_块2.md`、`_块4.md`（本文件里标 `块2.md:行号`，可复查）；
> 材质实值由 UnityPy 直读 `Material.m_SavedProperties`（同一次运行里读的，非二手）。
>
> **这份文档只回答一个问题**：原版这 7 个 shader 的 **frag 到底乘不乘颜色常量**。
> 起因：2026-09-19 我们从自建 shader `WarpforgeVFX/Particles/Extra Color*` 里删掉了那份 `_Color` 乘法
> （原版 `Everguild/FX/Extra Color` 的 `_Color` 只当 LOD bias），结果 **7 条效果从偏亮翻成偏暗**
> ⇒ 说明有一批原版 shader **本来就该乘颜色常量**。本文件把「哪几个、乘几份、乘在哪」定下来。

## 〇 · 记号与判据（先读这节）

**记号**

| 记号 | 含义 |
|---|---|
| `tex` / `t0` `t1` `t2` | ps 里的贴图采样。**哪个名字对哪个寄存器见各节**（本节末有口径） |
| `vColor` | 顶点色插值寄存器（`INTERP ... xyzw` 那个）。**7 个 shader 里编号不同**（v3/v4/v2/v1），见各节 |
| `cb0[i]` | **Unity 引擎全局**（`$Globals`）。本族点到的：`cb0[3].xy`=`_ScreenParams`、`cb0[4].x`=采样 LOD bias（**不是颜色**）、`cb0[19].x`=随时间单调增的引擎时间、`cb0[22].x`=`_ProjectionParams.x`、`cb0[24].z/.w`=`_ZBufferParams`、`cb0[25].w`+`cb0[4].z`=两个全局开关、`cb0[78..81]`=`unity_MatrixVP`。⚠️ **MotionVectors / DepthNormals 那几个 pass 里 `cb0` 换成了 `UnityPerDraw`**（`cb0[42].y` 就是那里的 motion-vector 开关） |
| `cb1[i]` | **材质常量缓冲**（UnityPerMaterial）。⚠️ 同一份缓冲**在不同阶段编号不同**：`Unlit UV scroll` 在 VS 里是 `cb2`、在 PS 里是 `cb1`；`TrailShader_1` 的 VS 里 `cb1` 是 `UnityPerDraw`（不是材质）。**比编号要在同一阶段内比** |
| `LinearToSRGB(c)` | 本族反复出现的一段算式：`log(\|c\|)*0.416667 → exp → *1.055-0.055`，低端（`c < 0.003131`）走 `c*12.9232`。**= UnityCG 的 `LinearToSRGB`**，下文写成一个函数 |
| `smoothstep01(u)` | `u*u*(3-2*u)`（本族里到处是手写的，不是 intrinsic） |

**判定（三选一，按任务书口径）**

- **A** = 不含颜色常量（`tex × vColor` 类）⇒ 我们现在的自建 shader（不乘 `_Color`）就对
- **B** = 含**一份**颜色常量乘子，且颜色这一层没有别的花样
- **C** = 更复杂（双色 / 溶解 / 遮罩 / 预乘 / UV 滚动 / sRGB 转换）

**🔴 一条必须先立的口径：本文件所有 `cb1[i]` 只写编号，不写属性名**

- 原版 DXBC **没有 RDEF**（`shader属性表_汇总.md:58`）。
- Unity 自己那段常量缓冲头**只有缓冲名与大小、没有成员表** —— 本轮实测：`TrailShader_1` 的 blob 头 **418 字节**，
  能读出的字符串**只有** `$Globals` / `UnityPerDraw` / `unity_MatrixVP`。⇒ **没有 name↔slot 映射**。
- 所以各节给的是 **槽位 + 用途 + 「属性表里有哪些候选属性名」**；只有在**数值解码**成立的地方才写「= 某属性」，
  并标 **（读到的）**。其余一律写「**槽位未证明**」。
- **反例提醒**：属性表里的**声明顺序 ≠ cbuffer 槽位顺序**。实测 `Particle Premultiply Greyscale Coloring`
  的两份颜色属性（`_Color`、`_Color2`）落在 `cb1[1]` 与 `cb1[3]`（**不连续**），说明 ShaderGraph 的 cbuffer 排布
  不按 Properties 块顺序 —— 按顺序推槽位会错。

**贴图寄存器与属性名的对应（口径）**：`属性表` 的「采样纹理」栏是按**寄存器顺序**列的名字 ——
本轮有两处可交叉核实（`Particle Premultiply`：`_MainTex`+`_CameraDepthTexture`，而 ps 里 t1 只在软粒子分支被读、t0 是主贴图 ✓；
`TrailShader_1`：`_MainTexture`+`_Noise_Combined`，而 t0 的 rgb 进颜色、t1 只进遮罩 ✓）。
⇒ 下文按此写 `t0=_XXX`，属「推的」，但两处同类都对上了。

---

## 一 · 总表

| shader | 判定 | 算式一行 | 节名 |
|---|---|---|---|
| `Everguild/FX/Particle Premultiply` | **B**（1 色） | `rgb = tex.rgb × LinearToSRGB(cb1[1].rgb) × vColor.rgb`；`a = tex.a² × cb1[1].a × vColor.a` | §二·1 |
| `Everguild/FX/Particle Premultiply Greyscale Coloring` | **C**（2 色·灰度着色·真预乘） | `rgb = (tex.a·vColor.a) × [tex.r × lerp(A,B,tex.r)] × vColor.rgb`，`A/B = LinearToSRGB(cb1[1]/cb1[3])` | §二·2 |
| `Everguild/FX/Particle Dissolve Premultiply` | **C**（1 色 + 溶解遮罩） | `rgb = s × LinearToSRGB(cb1[1].rgb) × tex.b × tex.r × vColor.rgb`，`s=smoothstep(sat((tex.g−v1.z)/(1−v1.z)))` | §二·3 |
| `Everguild/FX/Unlit UV scroll` | **C**（1 色 + 双贴图 hardlight + 顶点里滚 UV） | `rgb = lerp(L1, hardlight(base=L1, blend=L0), cb1[5].x) × LinearToSRGB(cb1[0].rgb) × vColor.rgb`（L0/L1 = 两张贴图的采样，见 §二·4） | §二·4 |
| `Everguild/FX/TrailShader_1` | **C**（2 色·沿拖尾插值 + 噪声滚动 + cutoff + fwidth AA） | `rgb = tex0.rgb × vColor.rgb × lerp(LinearToSRGB(cb1[0]),LinearToSRGB(cb1[1]), v1.x)` | §二·5 |
| `Everguild/FX/TrailShader_Fading` | **C**（2 色同上，**无顶点色**） | `rgb = k × tex0.rgb × lerp(LinearToSRGB(cb1[0]),LinearToSRGB(cb1[1]), v1.x)`，`k=1−2·v1.x·cb1[6].x+noise.b` | §二·6 |
| `Shader Graphs/Doomweaver effect` | **C**（1 色 + 双带遮罩 + 两层相加） | `rgb = band×LinearToSRGB(cb1[2].rgb)×cb1[3].z + cb1[1].z×(tex×vColor)` | §二·7 |

**结论一句话：7/7 都含颜色常量 ⇒ 这 7 个原版 shader 都不能用「不乘 `_Color`」的自建 shader。**
份数：**1 份** 4 个（Premultiply · Dissolve Premultiply · Unlit UV scroll · Doomweaver）；**2 份** 3 个（Greyscale Coloring · TrailShader_1 · TrailShader_Fading）。

---

## 二 · 逐个 shader

### 1. `Everguild/FX/Particle Premultiply`

- **出处**：`battleprefabs_vfxandmisc_assets_all.bundle` + `wf_shaders_extra.bundle`（两处内容一致）
- **DXBC 段 = 16**，其中 **8 段 ps**、5 种算式：

| 段 | 字节 | 是什么 | 贴图 | 缓冲 |
|---|---|---|---|---|
| [2] | 864 | **主 ps**（无软粒子） | t0=`_MainTex` | CB0[5] CB1[2]，temps 3 |
| [3] | 2340 | **主 ps + 软粒子** | t0=`_CameraDepthTexture`、t1=`_MainTex` | CB0[131] CB1[3]，temps 6 |
| [6]/[7] | 692 | MotionVectors（读 `cb0[42].y`） | — | CB0[43]，temps 1 |
| [10]/[11] | 400 | DepthNormalsOnly：`o.xyz=normalize(v4.xyz)`, `o.w=0` | — | 无 cb |
| [14]/[15] | 316 | 空 pass（ShadowCaster）：`o=(0,0,0,0)` | — | 无 cb |

- **主 ps [2] 算式**（逐行可复查）：

```
c     = LinearToSRGB(cb1[1].rgb)              ; 颜色常量（HDR）
                                              ; 本 shader 属性表里**唯一的 Color** = `_Color`（由「过 sRGB ⇒ 是颜色」+ 唯一性排除，**推**）
t     = sample_b(t0, v1.xy)
o.rgb = t.rgb * c * vColor.rgb                ; ← **颜色常量乘在 rgb 上**
o.w   = t.a * cb1[1].a * vColor.a * t.a       ; ← 注意 t.a 被乘了两次（= t.a²）
```

- **DXBC 原文关键行**（段 [2]，`Particle Premultiply`）：

```
log r0.xyz, |cb1[1].xyzx| ; mul r0.xyz, r0.xyzx, l(0.416667,...) ; exp r0.xyz, r0.xyzx
mad r0.xyz, r0.xyzx, l(1.055000,...), l(-0.055000,...)        ; ← LinearToSRGB(cb1[1].rgb)
ge r1.xyz, l(0.003131,...), cb1[1].xyzx
mul r2.xyz, cb1[1].xyzx, l(12.923210,...)
movc r0.xyz, r1.xyzx, r2.xyzx, r0.xyzx
sample_b r1.xyzw, v1.xyxx, t0.xyzw, s0, cb0[4].x              ; cb0[4].x = LOD bias，不是颜色
mul r0.xyz, r0.xyzx, r1.xyzx
mul o0.xyz, r0.xyzx, v3.xyzx                                  ; × 顶点色
mul r0.x, r1.w, cb1[1].w ; mul r0.x, r0.x, v3.w ; mul o0.w, r0.x, r1.w
```

- **判定：B** —— **一份**颜色常量，颜色层没有别的花样。但要记两条形状：
  ① 常量**先过 `LinearToSRGB`**；② alpha 是 `t.a² × c.a × vColor.a`。
- **顶点色**：参与（`v3.xyzw`；`.rgb` 乘 rgb、`.w` 乘 alpha）。
- **alpha**：`t.a² × c.a × vColor.a`。**rgb 不乘 alpha** —— 名字叫 `Premultiply`，但 ps 里**没有** `rgb *= a`。
- **混合**（`块2.md:112`，**固定值、不是材质驱动**）：`SrcAlpha → OneMinusSrcAlpha ｜ α:One→OneMinusSrcAlpha`
  ⇒ **标准 alpha 混合，不是预乘混合** ⇒ 「Premultiply」这个名字与算式/混合状态**都对不上**（据实记录，不解释作者意图）。
- **软粒子变体 [3] 追加**：`f = pow(saturate((场景线性深度 − clipW)/cb1[2].x), cb1[2].y)`，然后 `o.rgb = f × rgb`、`o.w = f × (上面那份 alpha)`；
  其中 `clipW` 是把 `v4.xyz`（顶点带的世界坐标）过 `unity_MatrixVP` 取的 `.w`；分支/不分支由全局 `cb0[25].w == 1` 选（两条路都是取场景深度）。
  `cb1[2].x/.y` 的候选属性是 `_Depth_And_Offset`（默认 `(0.3,1,0,0)`，与 0.3 逐一吻合）—— **槽位=推的**。
- **材质实值（读到的）**：唯一一份材质 **`Explosion_big_ground`**：
  `_Color=(0.7490,0.7490,0.7490,1)`（≠ 属性默认 (1,1,1,0) ⇒ 是作者设的）· `_SOFTPARTICLES=1` · `_Depth_And_Offset=(0.3,1,0,0)`。
  ⇒ 这份材质上，原版实际乘的是 `LinearToSRGB(0.7490) = **0.8803**`（不是 0.749）。
  🔴 **不乘 `_Color` 的话，这份材质要亮 `1/0.8803 ≈ 1.136×`**（alpha 侧不变，因 `_Color.a = 1`）。

---

### 2. `Everguild/FX/Particle Premultiply Greyscale Coloring`

- **出处**：`wf_shaders.bundle` + `shaders_assets_all.bundle`（同名 shader 在各包里各一份，共 4 处命中、内容一致）
- **DXBC 段 = 12**，**7 段 ps**、3 种算式（VS 5 段）：

| 段 | 字节 | 是什么 | 贴图 | 缓冲 |
|---|---|---|---|---|
| [1] | 1224 | **主 ps · 变体 a**（`o.w = 1`） | t0=`_MainTex` | CB0[5] CB1[4]，temps 4 |
| [2] | 1224 | **主 ps · 变体 b**（`o.w = a`）—— 与 [1] 只差最后一行 | 同 | 同 |
| [4] | 296 | MotionVectors | — | 无 cb |
| [6] | 692 | DepthNormals（读 `cb0[42].y`） | — | CB0[43] |
| [8]/[9] | 400 | DepthNormalsOnly | — | 无 cb |
| [11] | 316 | 空 pass | — | 无 cb |

- **主 ps 算式**：

```
A   = LinearToSRGB(cb1[1].rgb)                 ; 颜色常量①
B   = LinearToSRGB(cb1[3].rgb)                 ; 颜色常量②
m   = sample_b(t0, v1.xy).r                    ; ← 贴图 **R 通道**当灰度遮罩
rgb = m * lerp(A, B, m)                        ; = m*(A + m*(B-A))，m=0⇒0，m=1⇒B
o.rgb = (t.a * vColor.a) * rgb * vColor.rgb    ; ← **rgb 先乘 alpha（真预乘）**
o.w   = 1.0            [段1]  /  t.a*vColor.a   [段2]
```

- **DXBC 原文关键行**（段 [1]）：

```
log r0.xyz, |cb1[3].xyzx| ... movc r0.xyz, r1.xyzx, r2.xyzx, r0.xyzx     ; A/B 两份 LinearToSRGB
log r1.xyz, |cb1[1].xyzx| ... movc r1.xyz, r2.xyzx, r3.xyzx, r1.xyzx
sample_b r2.xyzw, v1.xyxx, t0.xyzw, s0, cb0[4].x
mul r1.xyz, r1.xyzx, r2.xxxx            ; r1 = A * m
mad r0.xyz, r0.xyzx, r2.xxxx, -r1.xyzx  ; r0 = B*m - A*m
mad r0.xyz, r2.xxxx, r0.xyzx, r1.xyzx   ; r0 = m*(B*m - A*m) + A*m = m*lerp(A,B,m)
mul r0.w, r2.w, v4.w                    ; a = tex.a * vColor.a
mul r0.xyz, r0.xyzx, v4.xyzx            ; × vColor.rgb
mul o0.xyz, r0.wwww, r0.xyzx            ; ← **预乘：rgb *= a**
mov o0.w, l(1.000000)                   ; 段[2] 这里是 mov o0.w, r0.w
```

- **判定：C**（**双色**常量 + 按贴图 R 通道的**灰度着色** + **rgb 真预乘** + alpha 双变体）。
- **预乘乘在哪**：**乘在 rgb 上**（`o.rgb = (tex.a·vColor.a)·rgb`）—— 本族里**唯一**真预乘的一个。
- **顶点色**：参与（`v4.xyzw`；rgb 与 alpha 都乘）。
- **alpha**：`tex.a × vColor.a`；段 [1] 把它换成 `1.0`（`_ALPHAPREMULTIPLY_ON` / `_ALPHAMODULATE_ON` 类关键字变体之一，**没证**是哪个）。
- **混合**（`块2.md:122`，材质驱动 `[_SrcBlend]→[_DstBlend]`）：
  材质实值 `Explosion_Color` / `FireRed` / `FireBlack` 全是 `_SrcBlend=1(One)`、`_DstBlend=10(OneMinusSrcAlpha)`
  ⇒ **预乘混合**，与「rgb 已预乘、o.w 输出真 alpha（段 [2]）」自洽 ✓。
- **材质实值（读到的）**：`_Color` = HDR 大值 ——`FireRed=(5.870,0.276,0,1)`、`Explosion_Color=(31.349,10.081,5.532,1)`、`FireBlack=(0,0,0,1)`；
  `_Color2` = `(2.271,1.700,0.404,1)`（FireBlack 是 `(0.219,3.565,0.219,1)`）；
  `_Depth_And_Offset=(0.3,1,0,0)`（本 shader 的 ps **没用到**，因为主 ps 只有 1 张贴图、无软粒子分支）。
  过 `LinearToSRGB` 后：`_Color` 31.35 → **4.378**、5.870 → **2.151**、`_Color2` 2.271 → **1.430**。
  ⇒ 这份材质的颜色乘子是 **1.4~4.4 倍**，**不是 1**。

---

### 3. `Everguild/FX/Particle Dissolve Premultiply`

- **出处**：`battleprefabs_vfxandmisc_assets_all.bundle` + `wf_shaders_extra.bundle`
- **DXBC 段 = 24**，**14 段 ps**、5 种算式（VS 10 段）：

| 段 | 字节 | 是什么 | 说明 |
|---|---|---|---|
| [2] / [4] | 1072 / 1116 | **主 ps · `o.w=1`**（两份签名变体，**指令流逐行相同**，差 44 字节在头部） | t0=`_BaseMap`，CB0[5] CB1[2]，temps 3 |
| [3] / [5] | 1080 / 1124 | **主 ps · `o.w = s·tex.a·vColor.w`** | 同上 |
| [8] / [9] | 272 / 316 | MotionVectors | CB0 为 UnityPerDraw |
| [12] / [13] | 668 / 712 | DepthNormals | |
| [16..19] | 376/376/420/420 | DepthNormalsOnly | `o.xyz=normalize(v3.xyz)`, `o.w=0` |
| [22] / [23] | 292 / 336 | 空 pass | |

- **主 ps 算式**（`_BaseMap` 的通道用途由属性显示名给出：**R:BaseMap, G:Dissolve, B:emissive mask, A:alpha**）：

```
e = LinearToSRGB(cb1[1].rgb)                 ; 颜色常量（本 shader 唯一的 Color = _EmissiveColor，材质实值见下）
d = saturate(v1.z)                           ; 逐顶点溶解驱动量（v1.xy 同时是 UV）
u = saturate((tex.g - d) / (1 - d))          ; tex.g = Dissolve 通道
s = u*u*(3 - 2*u)                            ; 手写 smoothstep（软化边）
o.rgb = s * (e * tex.b * tex.r * vColor.rgb) ; tex.b = emissive mask
o.w   = 1.0                     [段 2/4]
      = s * tex.a * vColor.w    [段 3/5]
```

- **DXBC 原文关键行**（段 [2]）：

```
log r0.xyz, |cb1[1].xyzx| ... movc r0.xyz, r1.xyzx, r2.xyzx, r0.xyzx   ; LinearToSRGB(cb1[1])
sample_b r1.xyzw, v1.xyxx, t0.xyzw, s0, cb0[4].x
mul r0.xyz, r0.xyzx, r1.zzzz          ; × tex.b（emissive mask）
mul r1.xzw, r1.xxxx, v2.xxyz          ; tex.r × 顶点色
mul r0.xyz, r0.xyzx, r1.xzwx
mov_sat r0.w, v1.z                    ; d
add r1.x, -r0.w, r1.y                 ; tex.g - d
add r0.w, -r0.w, l(1.000000)          ; 1 - d
div r0.w, l(1.000000,...), r0.w
mul_sat r0.w, r0.w, r1.x             ; u = saturate((tex.g-d)/(1-d))
mad r1.x, r0.w, l(-2.000000), l(3.000000) ; mul r0.w, r0.w, r0.w ; mul r0.w, r0.w, r1.x   ; s
mul o0.xyz, r0.wwww, r0.xyzx          ; rgb *= s
mov o0.w, l(1.000000)
```

- **判定：C**（**一份**颜色常量 + **溶解遮罩** + rgb 被预先压暗）。
- **预乘乘在哪**：rgb 乘的是**溶解 smoothstep `s`**，**不是** alpha；`o.w = 1` 那个变体才是「预先乘暗 + alpha 交给混合方程」的写法。
- **顶点色**：参与（`v2.xyz` 或 `v2.xyzw`；3 分量变体只乘 rgb，4 分量变体另乘 `.w` 进 alpha）。
- **混合**（`块2.md:92`，材质驱动）：材质实值 `Muzzle Flash`：`_SrcBlend=1(One)`、`_DstBlend=10(OneMinusSrcAlpha)`
  ⇒ 预乘混合，与 `o.w=1` 变体配套 ✓。
- **材质实值（读到的）**：`Muzzle Flash._EmissiveColor=(11.182,9.425,5.445,0)`
  ⇒ 过 `LinearToSRGB` 后 **11.18 → 2.830**（`.a=0` 没被读）。
  ⚠️ 注意这个材质上还有一堆**孤儿属性**（`_Color`/`_BaseColor`/`_Metallic`… 属性表里本 shader 没有它们）⇒ 别读，是死值。

---

### 4. `Everguild/FX/Unlit UV scroll`

- **出处**：`wf_shaders.bundle`（**DXBC 段 = 42**，本族里最多；其中 **24 段 ps**，主 ps 只有 6 段、其余是 MV/DN/空 pass）
- **主 ps 只有 3 种算式，每种两份签名变体**（3 对之间的指令流**逐行 diff 为 0**，差的 44 字节在头部）：

| 段 | 字节 | 是什么 | 贴图 | 缓冲 |
|---|---|---|---|---|
| [4] / [7] | 1348 / 1392 | 两层合成，**`o.w = 1`**；只吃 `v1.xyz`（3 分量顶点色） | t0,t1 | CB0[5] CB1[6]，temps 5 |
| [5] / [8] | 1412 / 1456 | 同 + 4 分量顶点色，**`o.w = saturate(a·cb1[5].y)`** | t0,t1 | CB0[5] CB1[6]，temps 6 |
| [6] / [9] | 2888 / 2932 | **软粒子**（多一张深度图 + 全屏 UV 分支） | t0=`_CameraDepthTexture`、t1,t2 | CB0[131] CB1[6]，temps 7 |
| 其余 | 296..736 | MotionVectors / DepthNormals / DepthNormalsOnly / 空 pass | | |

- **主 ps 算式**（`t0=_MainTex`、`t1=_SecondaryTex`，名字对应=推的；UV 见下）：

```
L0 = sample_b(t0, v2.zw)        ; 层 0 = _MainTex   ← 它的 uv 来自「UV Scale(XY) Speed(ZW)」
L1 = sample_b(t1, v2.xy)        ; 层 1 = _SecondaryTex
H  = hardlight(base = L1, blend = L0)
     ; 逐通道： L1<=0.5 ? 2*L1*L0 : 1-2*(1-L1)*(1-L0)
c  = lerp(L1, H, cb1[5].x)      ; cb1[5].x = 图层混合强度
o.rgb = c * LinearToSRGB(cb1[0].rgb) * vColor.rgb
o.w   = 1.0                     [段 4/7]
      = saturate(L1.a*cb1[0].a*vColor.a*cb1[5].y)   [段 5/8]
      ; 软粒子变体再乘 f = pow(saturate((场景深度 − clipW)/cb1[5].z), cb1[5].w)
```

- **DXBC 原文关键行**（段 [4]）：

```
sample_b r0.xyzw, v2.zwzz, t0.xyzw, s0, cb0[4].x      ; 层0
add r1.xyz, -r0.xyzx, l(1.000000,...)
sample_b r2.xyzw, v2.xyxx, t1.xyzw, s1, cb0[4].x      ; 层1
add r3.xyz, -r2.xyzx, l(1.000000,...) ; add r3.xyz, r3.xyzx, r3.xyzx
mad r1.xyz, -r3.xyzx, r1.xyzx, l(1.000000,...)        ; 1-2*(1-L1)*(1-L0)
ge r3.xyz, l(0.500000,...), r2.xyzx                   ; L1<=0.5 ?
movc r4.xyz, r3.xyzx, l(0,0,0,0), l(1,1,1,0)
and r3.xyz, r3.xyzx, l(0x3f800000,...)
mul r1.xyz, r1.xyzx, r4.xyzx
mul r0.xyz, r0.xyzx, r2.xyzx ; add r0.xyz, r0.xyzx, r0.xyzx ; mad r0.xyz, r0.xyzx, r3.xyzx, r1.xyzx
add r0.xyz, -r2.xyzx, r0.xyzx
mad r0.xyz, cb1[5].xxxx, r0.xyzx, r2.xyzx             ; lerp(L1, H, cb1[5].x)
log r1.xyz, |cb1[0].xyzx| ... movc r1.xyz, r2.xyzx, r3.xyzx, r1.xyzx   ; LinearToSRGB(cb1[0])
mul r0.xyz, r0.xyzx, r1.xyzx ; mul o0.xyz, r0.xyzx, v1.xyzx ; mov o0.w, l(1.000000)
```

- **判定：C**（**一份**颜色常量 + **双贴图 hard-light 合成** + 图层不透明度 + 软粒子 + 顶点里滚 UV）。
- **🔴 名字骗人：「UV scroll」不在 ps 里**。ps 里**没有任何时间参数**；两层是 hardlight 混合。
  滚动在**顶点着色器**里，且写得清清楚楚（`Unlit UV scroll` 的 VS，段 [0]）：

```
mul r0.xy, cb0[19].xxxx, cb2[3].zwzz      ; 时间 × 速度
frc r0.xy, r0.xyxx                        ; frac() 回绕
mad o2.zw, v3.xxxy, cb2[3].xxxy, r0.xxxy  ; uv = 顶点uv × Scale + frac(时间 × Speed)
mul r0.xy, cb0[19].xxxx, cb2[4].zwzz
frc r0.xy, r0.xyxx
mad o2.xy, v3.xyxx, cb2[4].xyxx, r0.xyxx
```
  ⇒ **用到 `cb*` 里的时间与速度参数**：时间 = `cb0[19].x`（引擎全局，**随时间单调增**），速度/缩放 = 材质侧两个 4 分量
  —— 属性表里这两个属性的**显示名逐字写着** `UV Scale (XY) Speed (ZW)` 与 `UV Scale (XY) Speed (ZW) 2`
  （默认 `(1,1,0.2,0)` / `(1,1,0.4,0)`），与算式 `uv = texcoord*scale + frac(time*speed)` **完全吻合** ⇒ **这一条是读到的**（名字解码）。
  ⚠️ 阶段编号：VS 里材质缓冲叫 **`cb2`**、PS 里叫 **`cb1`**，槽位同源。
- **顶点色**：参与（`v1.xyzw`；段 5/8 里 rgb 与 a 都乘，段 4/7 只乘 rgb）。
- **混合**（`块2.md:294`，材质驱动）：材质实值 `Slash_Roll`：`_SrcBlend=5(SrcAlpha)`、`_DstBlend=10(OneMinusSrcAlpha)` ⇒ 标准 alpha 混合。
- **材质实值（读到的）· `cb1[5]` 这一槽能对上**：`Slash_Roll._Layers_Blend_Opacity=0.5`、`_FinalAlphaMultiplier=1`、
  `_Depth_X_Falloff_Y=(0.5,0.5,0,0)` ⇒ `cb1[5] = (0.5, 1, 0.5, 0.5)`，与算式里三处（lerp 权重 / alpha 乘子 / 软粒子距离与指数）
  **名字+数值+语义三重吻合** ⇒ 这一槽的对应**是读到的**。
  `Slash_Roll` 上另外挂的 `_DissolveSpeed`/`_MainTextureSpeed`/`_TRAILALPHACUTOFF`/`_TailLength` **不在本 shader 的属性表里**
  ⇒ 同 `Muzzle Flash` 那种**换过 shader 的死值**，别读。

---

### 5. `Everguild/FX/TrailShader_1`

- **出处**：`battleprefabs_vfxandmisc_assets_all.bundle` + `wf_shaders_extra.bundle`
- **DXBC 段 = 8**，**4 段 ps**：

| 段 | 字节 | 是什么 | 贴图 | 缓冲 |
|---|---|---|---|---|
| [1] | 1848 | **主 ps** | t0=`_MainTexture`、t1=`_Noise_Combined` | CB0[20] CB1[7]，temps 5 |
| [3] | 1196 | MotionVectors | 两张 | CB0[20] CB1[43] CB2[7] |
| [5] | 904 | DepthNormals | 两张 | 同上 |
| [7] | 828 | 空 pass | 两张 | 同上 |

- **主 ps 算式**（`v1.xy` = 拖尾自己的坐标：`.x` = **沿拖尾的年龄**；`v2.xyzw` = 顶点色）：

```
k  = 1 - v1.x + sample_b(t1, uvN).b - cb1[6].x        ; 溶解/淡出遮罩（cb1[6].x = 属性表里的 _TRAILALPHACUTOFF）
uvM = v1.xy + cb0[19].x * cb1[3].xy                   ; 主贴图 uv —— **时间在 ps 里直接用**
uvN = v1.xy * cb1[3].z + cb0[19].x * cb1[4].xy        ; 噪声 uv
a  = k * sample_b(t0,uvM).r * vColor.w
o.rgb = sample_b(t0,uvM).rgb * vColor.rgb * lerp(LinearToSRGB(cb1[0].rgb), LinearToSRGB(cb1[1].rgb), v1.x)
o.w  = cb0[4].z != 0 ?  (用屏幕导数做的解析 AA 版，见下)  :  a
       AA = saturate( a / max(|ddx a|+|ddy a|, 1e-4) + 0.5 )       ; = smoothstep(0, fwidth(a), a)
       discard 当 (选中的那个值 < 0)
```

- **DXBC 原文关键行**（段 [1]，`TrailShader_1`）：

```
mul r0.xy, cb0[19].xxxx, cb1[4].xyxx
mad r0.xy, v1.xyxx, cb1[3].zzzz, r0.xyxx
sample_b r0.xyzw, r0.xyxx, t1.xyzw, s1, cb0[4].x        ; 噪声
add r0.x, -v1.x, l(1.000000) ; add r0.x, r0.x, r0.z ; add r0.x, r0.x, -cb1[6].x
mad r0.yz, cb0[19].xxxx, cb1[3].xxyx, v1.xxyx
sample_b r1.xyzw, r0.yzyy, t0.xyzw, s0, cb0[4].x        ; 主贴图
mul r0.x, r0.x, r1.x ; mul r0.y, r0.x, v2.w ; mad r0.x, v2.w, r0.x, l(-0.500000)
deriv_rtx r0.z, r0.y ; deriv_rty r0.w, r0.y ; add r0.z, |r0.w|, |r0.z|
mad r0.w, -r0.z, l(0.500000), r0.x ; max r0.z, r0.z, l(0.000100) ; div r0.z, r0.w, r0.z
add_sat r0.z, r0.z, l(1.000000)
ne r1.w, cb0[4].z, l(0.000000) ; movc o0.w, r1.w, r0.z, r0.y ; lt r0.x, r0.x, l(0.000000) ; discard_nz r0.x
log r0.xyz, |cb1[1].xyzx| ... movc r0.xyz, r2.xyzx, r3.xyzx, r0.xyzx   ; 颜色②
log r2.xyz, |cb1[0].xyzx| ... movc r2.xyz, r3.xyzx, r4.xyzx, r2.xyzx   ; 颜色①
add r0.xyz, r0.xyzx, -r2.xyzx ; mad r0.xyz, v1.xxxx, r0.xyzx, r2.xyzx ; mul r0.xyz, r1.xyzx, r0.xyzx
mul o0.xyz, r0.xyzx, v2.xyzx
```

- **判定：C**（**两份**颜色常量沿拖尾插值 + 噪声滚动 + cutoff 遮罩 + fwidth 抗锯齿 + discard）。
- **用到的 `cb*` 时间/速度参数**：时间 `cb0[19].x`（**在 ps 里直接乘**）；速度/平铺在 `cb1[3]`（主贴图 xy、噪声的平铺在 z）与 `cb1[4]`（噪声 xy）。
- **顶点色**：参与（`v2.xyzw`：`.rgb` 乘 rgb、`.w` 乘 alpha）。
- **alpha**：`k × t0.r × vColor.w`，可选替换成 fwidth 解析 AA（`cb0[4].z` 这个全局开关决定用哪个，**开关语义未证**）。
- **混合**（`块2.md:237`，**固定值**）：`SrcAlpha→OneMinusSrcAlpha ｜ α:One→OneMinusSrcAlpha`；`cull=Off`；`zW=Off`。
- **材质实值（读到的）**：属性表记本 shader 有 **4 份材质**（表里只印了 3 个名字：`Wispy_Trail_Deathspinner` / `Wispy_Trail_Psychomancer` / `Wispy_Trail_Plasmancer`，
  `块2.md:243`），本轮把这 3 份都读了：
  `_Color01` HDR 且 **`.a = 0`**（如 Psychomancer `(0.932, 8.476, 8.320, 0)`）、`_Color02`（如 `(0.949, 2.0, 1.467, 1)`）
  ⇒ **`.a` 没被用**（ps 只取 rgb）✓。
  `_MainTextureSpeed` = `(-2,0,0,0)`（Psychomancer/Plasmancer）/ `(0.5,0,0,0)`（Deathspinner）、
  `_DissolveSpeed` = `(1,1,0,0)` / `(-0.5,0,0,0)`、`_TRAILALPHACUTOFF=0`、`_DissolveScale` = 1 / 35 / 0.5。
  ⇒ `cb1[3]`/`cb1[4]` **就是这两个速度**（名字说的贴图与算式里乘的贴图一致、数值也一致）——**哪个是哪个未证明**；
  但 **`.z` 一定是 0**：本 shader 属性表里的 **float4 候选只有这两个**（`_DissolveScale`/`_TRAILALPHACUTOFF`/`_TrailLenght` 都是标量），
  而两者在全部可查材质里 `.z` 都是 0 ⇒ **噪声 uv 的 `v1.xy` 项实际被乘 0**，
  **噪声只随时间滚、不随拖尾滚**（这条是读到的：材质值 + 指令流两边都定死了）。

---

### 6. `Everguild/FX/TrailShader_Fading`

- **出处**：`battleprefabs_vfxandmisc_assets_all.bundle` + `wf_shaders_extra.bundle`
- **DXBC 段 = 8**，**4 段 ps**（[1] 主 1788 · [3] MV 1152 · [5] DN 860 · [7] 空 784）
- 与 `TrailShader_1` **同一套属性**（少 `_TRAILALPHACUTOFF`、多 `_TailLength`），结构也几乎一样，**三处实质差异**：

| | `TrailShader_1` | `TrailShader_Fading` |
|---|---|---|
| 遮罩 | `k = 1 - v1.x + noise.b - cutoff` | `k = 1 - 2*v1.x*cb1[6].x + noise.b`（cutoff 变成**衰减速率**） |
| rgb | `t0.rgb × vColor.rgb × lerp(c0,c1,v1.x)` | **`k × t0.rgb × lerp(c0,c1,v1.x)`**（多了一次 k） |
| 顶点色 | 有（`v2.xyzw`） | **没有**（主 ps 只声明 `dcl_input_ps linear v1.xy`） |
| alpha | `k × t0.r × vColor.w` | `saturate(k)`（可选换成 fwidth AA），**不乘 t0.r** |

- **主 ps 算式**：

```
k  = 1 - 2*v1.x*cb1[6].x + sample_b(t1, uvN).b
uvM = v1.xy + cb0[19].x * cb1[3].xy ; uvN = v1.xy * cb1[3].z + cb0[19].x * cb1[4].xy
o.rgb = k * sample_b(t0,uvM).rgb * lerp(LinearToSRGB(cb1[0].rgb), LinearToSRGB(cb1[1].rgb), v1.x)
o.w   = saturate(k)  或 fwidth AA 版；< 0 时 discard
```

- **DXBC 原文关键行**（段 [1]，`TrailShader_Fading`）：

```
mul r0.xy, cb0[19].xxxx, cb1[4].xyxx ; mad r0.xy, v1.xyxx, cb1[3].zzzz, r0.xyxx
sample_b r0.xyzw, r0.xyxx, t1.xyzw, s1, cb0[4].x
mad r0.x, -v1.x, cb1[6].x, l(1.000000) ; add r0.x, r0.x, r0.z ; mad r0.x, -v1.x, cb1[6].x, r0.x
mad r0.yz, cb0[19].xxxx, cb1[3].xxyx, v1.xxyx
sample_b r1.xyzw, r0.yzyy, t0.xyzw, s0, cb0[4].x
mul r0.xyz, r0.xxxx, r1.xyzx          ; ← rgb 乘 k（TrailShader_1 这里没有这一步）
mov_sat r0.w, r0.x                    ; deriv_rtx/deriv_rty 的 AA 同 TrailShader_1
...
add r1.xyz, r1.xyzx, -r2.xyzx ; mad r1.xyz, v1.xxxx, r1.xyzx, r2.xyzx ; mul o0.xyz, r0.xyzx, r1.xyzx
```

- **判定：C**（**两份**颜色常量 + 噪声滚动 + 衰减 + fwidth AA）。
- **预乘/alpha**：rgb 乘的是遮罩 `k`（不是 alpha）；`o.w = saturate(k)`。
- **顶点色**：**不参与**（7 个里唯一一个）。
- **材质实值（读到的）**：唯一一份 `Wispy_Trail_Dimensional_Breach`：
  `_Color01=(0.409,2.015,0.443,0)`、`_Color02=(0.066,0.066,0.066,0)`、`_DissolveSpeed=(1,0,0,0)`、
  `_MainTextureSpeed=(0.2,0,0,0)`、`_DissolveScale=0.5`、`_TailLength=0.5`
  ⇒ `cb1[6].x` = `_TailLength`(0.5)（本 shader 没有 `_TRAILALPHACUTOFF`，属性表里第 6 槽的候选只有 `_TailLength`，**槽位=推的**）。

---

### 7. `Shader Graphs/Doomweaver effect`

- **出处**：`battleprefabs_vfxandmisc_assets_all.bundle` + `wf_shaders_extra.bundle`
- **DXBC 段 = 12**，**7 段 ps**（VS 5 段）：

| 段 | 字节 | 是什么 | 贴图 | 缓冲 |
|---|---|---|---|---|
| [1] | 1404 | **主 ps · `o.w = 1`** | t0=`_MainTex` | CB0[5] CB1[4]，temps 3 |
| [2] | 1404 | **主 ps · `o.w = saturate(alpha)`**（与 [1] 只差最后一行） | 同 | 同 |
| [4] | 272 | MotionVectors | — | 无 cb |
| [6] | 668 | DepthNormals | — | CB0[43] |
| [8]/[9] | 376 | DepthNormalsOnly | — | 无 cb |
| [11] | 292 | 空 pass | — | 无 cb |

- **主 ps 算式**：

```
c    = LinearToSRGB(cb1[2].rgb)                        ; 颜色常量（= 材质 _FadeInBorderColor，见下，读到的）
t    = v1.y + 2*v1.z - 1                               ; 遮罩坐标（-1..1，顶点流给的）
w0   = cb1[1].y - cb1[1].x                             ; 带宽
s1   = smoothstep01( saturate((t - cb1[1].x)/w0) )
s2   = smoothstep01( saturate((t - cb1[3].x - cb1[1].x)/w0) )
band = s1 - s2                                         ; 一条带子（两个 smoothstep 相减）
o.rgb = band * c * cb1[3].z  +  cb1[1].z * (sample_b(t0,v1.xy).rgb * vColor.rgb)
o.w   = 1.0    [段1]  /  saturate(s1 * tex.a * vColor.a)   [段2]
```

- **DXBC 原文关键行**（段 [1]）：

```
log r0.xyz, |cb1[2].xyzx| ... movc r0.xyz, r1.xyzx, r2.xyzx, r0.xyzx   ; LinearToSRGB(cb1[2])
mad r0.w, v1.z, l(2.000000), v1.y ; add r0.w, r0.w, l(-1.000000)
add r1.x, r0.w, -cb1[1].x ; add r0.w, r0.w, -cb1[3].x ; add r0.w, r0.w, -cb1[1].x
add r1.y, -cb1[1].x, cb1[1].y ; div r1.y, l(1.0,...), r1.y
mul_sat r1.x, r1.y, r1.x ; mul_sat r0.w, r0.w, r1.y
mad r1.y, r1.x, l(-2.000000), l(3.000000) ; mul r1.x, r1.x, r1.x ; mul r1.x, r1.x, r1.y   ; s1
mad r1.y, r0.w, l(-2.000000), l(3.000000) ; mul r0.w, r0.w, r0.w ; mad r0.w, -r1.y, r0.w, r1.x ; band = s1-s2
mul r0.xyz, r0.wwww, r0.xyzx ; mul r0.xyz, r0.xyzx, cb1[3].zzzz
sample_b r2.xyzw, v1.xyxx, t0.xyzw, s0, cb0[4].x
mul r2.xyzw, r2.xyzw, v2.xyzw          ; × 顶点色
mul r0.w, r1.x, r2.w                   ; alpha = s1 * tex.a * vColor.a
mul r0.xyz, r0.xyzx, r0.wwww
mad o0.xyz, cb1[1].zzzz, r2.xyzx, r0.xyzx   ; ← 第二项：cb1[1].z × (tex×vColor)
mov o0.w, l(1.000000)
```

- **判定：C**（**一份**颜色常量 + 双 smoothstep **带子** + 两层相加）。
- **顶点色**：参与（`v2.xyzw`；`tex.rgb` 先乘 `vColor.rgb`、`.w` 进 alpha）。
- **预乘/alpha**：rgb 不被 alpha 乘；`o.w = 1` 或 `saturate(s1·tex.a·vColor.a)`。
- **材质实值（读到的，且能对出槽位）**：唯一一份 `Doomweaver effect`：
  `_BorderSharpness=(0.370, 0.480, 0, 0)` — 算式里带子的两端正是 `cb1[1].x=0.37`、`cb1[1].y=0.48`、带宽 = `.y-.x` ✓ **读到的**
  `_FadeInBorderColor=(18.090, 561.791, 766.996, 0)` — 过 `LinearToSRGB` 后 **766.996 → 16.743**（不是 767）✓ **读到的**
  `_ExtraBorderColorMultiplier=37.190` → `cb1[3].z`（过 sRGB 后 4.705；本 shader 里它**不过 sRGB**，是直接乘的标量）
  `_FadeInBorderWidth=0.02` → `cb1[3].x`（**槽位=推的**：只有 0.02 放进「带子偏移」讲得通，`_Float=0.847` 放进去会把带子压平）
  `cb1[1].z` = `_BorderSharpness.z` = **0** ⇒ **第二项（`cb1[1].z × tex×vColor`）在这份材质里恒为 0**
  （注意：`_ExtraColorMultiplier=1` 是另一个属性，**不是** `cb1[1].z` —— HLSL 打包规则下 `float` 上不了 `float4` 的 `.z`）。
  `_SrcBlend=5(SrcAlpha)`、`_DstBlend=10(OneMinusSrcAlpha)` ⇒ 标准 alpha 混合。

---

## 三 · 横向结论（对上游的直接含义）

1. **7/7 都乘颜色常量** ⇒ 这 7 个原版 shader 都**不能**用「不乘 `_Color`」的自建 shader。
   份数 1 份：`Particle Premultiply`（`cb1[1]`）· `Dissolve Premultiply`（`cb1[1]`）· `Unlit UV scroll`（`cb1[0]`）· `Doomweaver`（`cb1[2]`）；
   份数 2 份：`Greyscale Coloring`（`cb1[1]`+`cb1[3]`）· `TrailShader_1`/`_Fading`（`cb1[0]`+`cb1[1]`）。

2. **常量一律先 `LinearToSRGB`**（7/7，无一例外）。⚠️ **直接抄属性值当乘子是错的**：

   | 属性实值 | 过了 `LinearToSRGB` 之后 |
   |---|---|
   | `_Color=0.749`（Particle Premultiply） | **0.880** |
   | `_Color=31.349`（Greyscale / Explosion_Color） | **4.378** |
   | `_Color=5.870`（Greyscale / FireRed） | **2.151** |
   | `_Color2=2.271`（Greyscale） | **1.430** |
   | `_EmissiveColor=11.182`（Dissolve Premultiply） | **2.830** |
   | `_FadeInBorderColor=766.996`（Doomweaver） | **16.743** |

   ⇒ 这些 HDR 属性**被 sRGB 编码强烈压缩**（767 → 16.7，31 → 4.4）。**复刻「乘不乘」只是第一步，乘多少必须照这段算式走**。

3. **预乘/alpha 的实际形状**（「预乘」二字在这 7 个里各指不同东西）：

   | shader | rgb 是否乘 alpha | alpha 算式 | 混合（材质实值） |
   |---|---|---|---|
   | `Particle Premultiply` | ❌ | `t.a² × c.a × vColor.a` | SrcAlpha→OneMinusSrcAlpha（**固定**） |
   | `Greyscale Coloring` | ✅（×`t.a`×`vColor.a`） | `t.a × vColor.a` | One→OneMinusSrcAlpha（**真预乘**） |
   | `Dissolve Premultiply` | ❌（乘的是溶解 `s`） | `1`（或 `s·t.a·vColor.w`） | One→OneMinusSrcAlpha |
   | `Unlit UV scroll` | ❌ | `saturate(L1.a·c.a·vColor.a·cb1[5].y)`（或 `1`） | SrcAlpha→OneMinusSrcAlpha |
   | `TrailShader_1` | ❌ | `k·t0.r·vColor.w`（+fwidth AA） | SrcAlpha→OneMinusSrcAlpha（**固定**） |
   | `TrailShader_Fading` | ❌（乘的是 `k`） | `saturate(k)`（+AA） | SrcAlpha→OneMinusSrcAlpha（**固定**） |
   | `Doomweaver` | ❌ | `saturate(s1·t.a·vColor.a)`（或 `1`） | SrcAlpha→OneMinusSrcAlpha |

   ⇒ **只有 1 个真预乘**（`Greyscale Coloring`）；`Particle Premultiply` 名字叫预乘但**rgb 不乘 alpha、混合也是标准的**。

4. **顶点色**：**6/7 参与**；唯一不参与的是 **`TrailShader_Fading`**（主 ps 里没有顶点色输入）。
   参与时，**rgb 与 alpha 都乘**（除 `Unlit UV scroll` 段 4/7 与 `Dissolve Premultiply` 的三分量变体只乘 rgb）。

5. **`cb*` 里的时间/速度参数**（只有 3 个 shader 用到）：

   | shader | 时间在哪 | 速度在哪 | 形状 |
   |---|---|---|---|
   | `Unlit UV scroll` | `cb0[19].x`，**用在 VS** | 材质侧两个 4 分量 `cb[3]`/`cb[4]` | `uv = 顶点uv×Scale + frac(时间×Speed)` |
   | `TrailShader_1` | `cb0[19].x`，**用在 PS** | `cb1[3]`（主贴图 xy / 噪声平铺 z）、`cb1[4]`（噪声 xy） | 主贴图 `v1.xy + t*speed`；噪声 `v1.xy*0 + t*speed` |
   | `TrailShader_Fading` | 同上 | 同上 | 同上 |
   | 其余 4 个 | **ps 里没有 `cb0[19]`**（不用时间） | — | — |

6. **`cb1` 槽位速查**（只到「用途」这一级，名字见各节）：

   | shader | 颜色 | 其他 |
   |---|---|---|
   | `Particle Premultiply` | `cb1[1]` | `cb1[2]` = 软粒子 `_Depth_And_Offset`（推） |
   | `Greyscale Coloring` | `cb1[1]`,`cb1[3]` | — |
   | `Dissolve Premultiply` | `cb1[1]` | — |
   | `Unlit UV scroll` | `cb1[0]` | `cb1[5]`=(图层强度, alpha 乘子, 软粒子距离, 指数)（读到的）；VS 的 `cb2[3]/[4]`=两组 Scale+Speed（读到的） |
   | `TrailShader_1` | `cb1[0]`,`cb1[1]` | `cb1[3]`/`cb1[4]`=两组速度，`cb1[6].x`=cutoff |
   | `TrailShader_Fading` | `cb1[0]`,`cb1[1]` | `cb1[3]`/`cb1[4]`=两组速度，`cb1[6].x`=`_TailLength` |
   | `Doomweaver` | `cb1[2]` | `cb1[1]`=`_BorderSharpness`（读到的）, `cb1[3]`=宽度/亮度标量 |

7. **可以直接抄的两条判据**：
   - 「**原版乘不乘颜色常量**」= 看 ps 里有没有 `log|cb1[k].xyz|*0.416667 → exp → *1.055-0.055` 这一段（本族 7/7 都有，且这就是**颜色常量存在的指纹**）；找 `mul o0.xyz, r?, r?` 里有没有它。
   - **颜色常量的槽位**里，`cb1[k].w` 只有在 `Particle Premultiply`（`_Color.a`）与 `Unlit UV scroll`（`cb1[0].a`）里进过 alpha；其余一律只取 `.rgb`。

---

## 四 · 没查到 / 未证明（不许静默）

1. **`cb1[i]` ↔ 属性名**：除 **`Doomweaver.cb1[1]`/`cb1[2]`/`cb1[3].z`** 与 **`Unlit UV scroll.cb1[5]`** 外，**全部未证明**。
   不可证的理由（本轮实测过）：
   - 原版 DXBC **只有 ISGN/OSGN/SHDR 三段，没有 RDEF**（`shader属性表_汇总.md:58`）；
   - Unity 自己那段常量缓冲头**不带成员表**：`TrailShader_1` 的 blob 头 418 字节，逐字节扫只得到
     `$Globals` / `UnityPerDraw` / `unity_MatrixVP` 三个串；
   - 属性表的**声明顺序 ≠ 槽位顺序**（`Greyscale Coloring` 的两份颜色落在不连续的 `cb1[1]`/`cb1[3]` ⇒ 反例）。
2. **`Particle Premultiply` 的另外 11 处引用材质没看**（`资料/普查产出_0917/补充shader引用清单.md:33` 记它被 12 个效果/材质引用）。
   本轮只读了 `Explosion_big_ground` 一份（`_Color=(0.749,…,1)`）⇒ §二·1 里那个 **1.136× 的量化只对这一份成立**，别外推。
3. **`cb0[19].x` 的名字没证**：能证的只有「随时间单调增、被当线性时间用（`frac(时间×速度)`）」。
   是 `_Time.x`（t/20）还是 `_Time.y`（t）**本机没法区分**（引擎全局的成员表在出包时被剥了）。
4. **两个全局开关的语义没证**：`cb0[25].w`（软粒子两条路的选路）与 `cb0[4].z`（拖尾用不用 fwidth AA）——
   只知道它俩怎么用，不知道叫什么。
5. **`Unlit UV scroll` 的 t0/t1 与 `_MainTex`/`_SecondaryTex` 的对应是推的** ⇒ 因此「谁是 hardlight 的 base、谁是 blend 层」
   也跟着不确定（算式本身「base = t1 那次采样」是读到的）。
6. **`Greyscale Coloring` 的两份常量哪个是 `_Color`、哪个是 `_Color2` 没证**（只证了就是这两份）。
7. **`Unlit UV scroll` 那 3 对主 ps 的差异没查到**：每对的**指令流逐行 diff 为 0**（差 44 字节全在签名/头部）
   ⇒ 说明那对的关键字差异**不改变 ps 的算式**，但**具体是哪两个关键字**没查。
8. **本轮没跑原版游戏对帧**（只做反汇编 + 读材质）。E 组那 7 条从偏亮翻偏暗的**是否止住**，
   要靠 `EffectCompare` 按 `WFCMP_ISO=all` 复扫验证。
9. **`Particle Premultiply` 的 `o.w = t.a²·c.a·vColor.a` 里那个多出来的 `t.a`**：
   指令流是这么写的（`mul r1.w → cb1[1].w → v3.w → × r1.w`），**原因不明**，本文件只报事实。

---

## 五 · 附：本文件里所有「读到的」判据怎么复现

```bash
# ① 反汇编（ps / vs）
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/disasm_dxbc.py "TrailShader_1" --stage ps
# ② 属性表 + 混合状态（二手，可复查）
#   资料/普查产出_0917/shader属性表_块2.md:234-253 · 块4.md:158-168
# ③ 材质实值（本轮新读的，UnityPy 直读，未落盘）
#   Material.m_SavedProperties.m_Colors / .m_Floats，按 m_Name 过滤
#   （新版本 UnityPy 里这两个是 list，不是 dict —— 用 [(k,v) for k,v in d] 取）
```
