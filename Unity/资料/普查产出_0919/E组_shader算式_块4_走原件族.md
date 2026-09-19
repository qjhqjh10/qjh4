# E 组 · 原版 shader **像素着色器算式** · 块 4「走原件族」（5 个）

> 2026-09-19。数据源：**原版 bundle 直读**（`D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/*.bundle`，UnityPy py312，**未启动 Unity**，未改 `Unity/MyGame/` 下任何文件）。
> 工具：`工具/disasm_dxbc.py`（ctypes 调 Windows 自带 `d3dcompiler_47.dll` 的 `D3DDisassemble`）。
> 复跑：
> ```bash
> PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
>   d:/4/Unity/工具/disasm_dxbc.py "<shader 名片段>" --stage ps        # 像素阶段
> PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
>   d:/4/Unity/工具/disasm_dxbc.py "<shader 名片段>" --stage vs        # 顶点阶段
> ```
>
> **这一份只回答一件事**：这 5 个「走原件」（不被 `WarpforgeShaderMap.UseOriginal` 白名单覆盖、
> 也不（全部）被 `Replacements` 映射）的原版 shader，**frag 到底怎么算**——
> 颜色算式、用了哪几张贴图、`_Color`/颜色常量参不参与（几份）、alpha 怎么算、顶点色参不参与、有几段 ps。

---

## 〇 · 记号、判据、以及四条与既有记录冲突的事（先看这节）

### 〇·1 记号

| 记号 | 含义 |
|---|---|
| `tex` / `t0` `t1` `t2` `t3` | ps 里的贴图采样。**哪个名字对哪个寄存器本轮全部查实**（见 §〇·2 判据 ②） |
| `vColor` | 顶点色（ISGN 里 `COLOR` 语义插值进来的那个寄存器）。**5 支恰好都编号成 `v2`**，但**只有 3 支真的是顶点色** —— `Burning` / `Burning Dissolve` 的 `v2` 是 `_FireNoise` 的两组 UV |
| `cb0[i]` | **引擎全局缓冲 `$Globals`**（下面 §〇·4 有一节专讲它在本族的用途） |
| `cb1[i]` | **材质常量缓冲 `UnityPerMaterial`** ⚠️ **只在 PS 阶段是 cb1**（VS 阶段是 cb2，见 §五·4；MotionVectors pass 甚至会变成 cb2） |
| `LinearToSRGB(c)` | 本族**每一支都出现**的一段算式：`log(\|c\|)*0.416667 → exp → *1.055-0.055`，低端（`c ≤ 0.003131`）走 `c*12.9232`。即 sRGB 编码曲线。下文写成一个函数 |
| `smoothstep01(u)` | `u*u*(3-2*u)`（原版是手写的三条指令，不是 intrinsic） |
| `pown` | `log`→`*k`→`exp` 三连，即 `pow` |

### 〇·2 三级判据（这一份的结论都建在这三层上，每层都可复跑）

**① 算式 = DXBC 指令流**。`disasm_dxbc.py` 拿 DXBC 里的 `SHDR` 段喂 `D3DDisassemble`。
本轮实测这 5 支的 DXBC 段**只有 `ISGN / OSGN / SHDR`**，**没有 `RDEF`**（Unity 把常量名表剥掉了）——
所以**光看反汇编，`cb1[3]` 是谁是读不出来的**，必须靠 ② ③。

**② 贴图名 ← 两条独立证据，结论一致**

- `(a)` blob 里的**「绑定资源表」**：每条记录 = `{u32 名长, 名字, u32 0, u32 绑定下标, u32 同上, u32 类型(4=texture)}`。
  实测（PDAT，blob 偏移 13564 起）：
  ```
  @13564  03 00 00 00                        ← 3 条
  @13568  14 00 00 00 "_GrabPassTransparent" … 00 00 00 00  04 00 00 00
  @13608  0b 00 00 00 "_DistortTex"          … 01 00 00 00  04 00 00 00
  @13640  05 00 00 00 "_Mask"                … 02 00 00 00  04 00 00 00
  ```
  ⇒ `_GrabPassTransparent`=t0 · `_DistortTex`=t1 · `_Mask`=t2，**与该 pass 的 `dcl_resource` 条数逐个吻合**（下面 §五 逐条对过）。
- `(b)` **Unity 自己的 name↔slot 表**：`SerializedPass.m_NameIndices` + `progFragment.m_CommonParameters`
  （口径同 `普查产出_0919/E组_shader算式_块3_内置粒子族.md` 头部）。
  实测 `Spiral Trail FX` Pass[0] fragment：`cb0 → $Globals`、`cb1 → UnityPerMaterial`，
  `m_TextureParams[0] → _MainTex`、`[1] → _Noise` ✅ 与 (a) 同结论。

**③ `cb1[i]` 是谁 ← blob 里的「cbuffer 反射块」**
同一 blob 里（**非 DXBC 的那一半**，由 `VGlobals*/PGlobals*` 打头）有 cbuffer 成员表：
`{u32 名长, 名字, u32 缓冲字节数, u32 成员数, 成员数 × {u32 名长, 名字, u32 0, u32 1, u32 分量数, u32 0, u32 0, u32 字节偏移}}`。
⚠️ **这份偏移是 std140（Vulkan 那半的）**，`float2` 会被 8 字节对齐推开一格；
**DXBC 那半是紧凑打包**，`float` 后面直接接 `float2`。两次实测：
`Spiral Trail FX` 的 `_MainTextScale` 在反射表里是 `+56`（cb1[3].z），**而 DXBC 指令里读的是 `cb1[3].y/.z` ⇒ 实际在 +52**；
`Rays For Trail` 的 `_MainTextScale` 反射表就是 `+48`（cb1[3].x），DXBC 读 `cb1[3].xy` ✅ 两者一致。
⇒ **口诀：反射表给「名字 + 分量数」，槽位要用反汇编的用法反证一遍。**

### 〇·3 🔴 四条与既有记录**冲突 / 需要更正**的事（先看这个）

| # | 既有说法 | 本轮实测 | 证据 |
|---|---|---|---|
| **1** | `Spiral Trail FX` 在 `battleprefabs...bundle` 里默认队列 **2000**、在 `wf_shaders*.bundle` 里 **3000**，「两份来自不同 bundle、默认队列不同」 | ❌ **不成立**。两份**字节完全相同**，队列标签也相同 | 两份 blob 各 20560 字节、**sha1 都是 `70a317d52521`**；子着色器标签两份都是 `QUEUE='Transparent'`（=3000）、`RenderType='Transparent'`；pass 数都是 3、标签逐条相同。**5 支里唯一队列标签是 `Geometry`（=2000）的是 `Everguild/FX/Burning`** —— 怀疑当初是把 Burning 记成了 Spiral Trail FX。材质侧也没有 2000：`Spiral Trail FX Smoke` 材质 `CustomRenderQueue = -1`、`_QueueControl = 0`、`_QueueOffset = 0` |
| **2** | 这 5 个「**没被映射也没进白名单**」 | ❌ 对 **PDAT** 不成立 | `Unity/MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs:78`：`{ "Everguild/FX/Particle Distortion Affect Transparents", "WarpforgeVFX/FX/Distortion" }`。另 4 支（Spiral Trail FX · Rays For Trail · Burning · Burning Dissolve）**在 `Replacements` 里没有**，属实 |
| **3** | `_GrabPassTransparent` 是「**字节码里扫到的名字**」（`普查产出_0917/shader属性表_块2.md:107` 记成「声明属性之外额外扫到」） | ✅ **它真的被采样**，不是空声明 | 绑定表里它在 **t0**（无深度变体）与 **t1**（带深度变体）两个位置，且与该 pass 的 `dcl_resource` 条数吻合；反汇编里确实有一条 `sample_b … t…`，UV = `翻转Y的屏幕UV + 扭曲偏移`。⇒ `GrabPassTransparentFeature.cs` 的方向是对的 |
| **4** | 「`_Color` 不参与」（`Extra Color` 的结论）**能不能推广到这几支？** | ❌ **不能**。**两支拖尾族根本没有 `_Color`，但有两份颜色常量**；**两支 Burning 有 `_Color` 且真的乘进去** | `Spiral Trail FX`/`Rays For Trail` 的属性表里没有 `_Color`，只有 `_Color1`/`_Color2`，且都进了 `UnityPerMaterial`；`Burning`/`Burning Dissolve` 的 `_Color` 真的当乘子（§三·1、§四·1） |

### 〇·4 一个**五支通用**的发现：颜色常量**一律先过一次 `LinearToSRGB` 编码**

5 支的 frag **每一支**、**每一份颜色常量**，在参与运算前都走了同一段指令：

```
   log  r1.xyz, |cb1[N].xyzx|          ; 取绝对值再 log（负/>1 的 HDR 值也能过）
   mul  r1.xyz, r1.xyzx, l(0.416667)   ; 1/2.4
   exp  r1.xyz, r1.xyzx                ; ← 到这里是 pow(c, 1/2.4)
   mad  r1.xyz, r1.xyzx, l(1.055), l(-0.055)
   ge   r3.xyz, l(0.003131), cb1[N].xyzx
   mul  r4.xyz, cb1[N].xyzx, l(12.923210)
   movc r1.xyz, r3.xyzx, r4.xyzx, r1.xyzx
```

⇒ `LinearToSRGB(c) = c ≤ 0.003131 ? c*12.9232 : 1.055*c^(1/2.4) - 0.055`。

**这条对亮度的影响很大，而且是非单调的**（它是 γ 压缩）：

| 材质实值（`Material.m_SavedProperties`，UnityPy 直读） | 原版实际用到的值 |
|---|---|
| `_Color1 = 0.6981`（`Spiral Trail FX Smoke`） | `0.8533`（**变亮** 1.22×） |
| `_Color2 = 4.7594`（`Wispy_Trail_EC Bullet`） | `1.9660`（**变暗** 0.41×） |
| `_Color1 = 16.0598`（`Aeldari death trail`） | `3.2996`（**变暗** 0.21×） |
| `_Color02 = 501.9608`（**5 支一个都不读**，只作量级示例） | `14.0227`（**变暗** 0.028×） |

⇒ **如果我们的自建 shader 直接拿线性色值去乘/去插值，HDR 那一侧会亮出一个量级**。
这一条与「`Buff_DA_Forest_Self` 亮度比 2.20 偏亮」的量级对得上，**建议作为第一优先验证项**（见 §一·5）。

⚠️ 另外：这些材质上挂着**一堆 shader 根本不读的属性** —— `_Color01` `_Color02`(值 501.96!) `_Color3` `_Color4`
`_Final_Color_Multiply` `_SmoothStep_IN` `_SmoothStep_OUT` `_MainTextureSpeed` `_DissolveSpeed`
`_Noise_Speed_Scale` `_Noise_Speed_Scale_2` `_Vector2` `_Vector4` … 都不在上面任何一支的 `UnityPerMaterial` 里。
**按材质属性表实现 = 一定会实现一堆原版没有的东西。**

---

## 一 · `Everguild/FX/Spiral Trail FX` ⭐（最要紧的一条）

**一句话算式**：

```
rgb   = lerp( LinearToSRGB(_Color1), LinearToSRGB(_Color2), smoothstep01(clamp01((_MainTex(u').r - S0) / (S1 - S0))) ) * vColor.rgb
alpha =                       ↑同一个 t                                                                              * vColor.a
u'    = ((v1.xy + _NoiseStrenght * (_Noise(u_n).r - 0.5)) - 0.5) * _MainTextScale + 0.5
u_n   = frac( _TimeParameters.x * _Scale_XY_Speed_ZW.zw + v1.xy * _Scale_XY_Speed_ZW.xy )
```

**🔴 端点方向（易接反，逐指令核过）**：`t = 0 → _Color1`，`t = 1 → _Color2`。
（`add r0.yzw, r0.yyzw, -r1.xxyz` 里 `r0.yzw`=`SRGB(cb1[4])`=`SRGB(_Color2)`、`r1.xyz`=`SRGB(_Color1)`，
所以 `mad r0.yzw, t, r0.yzw, r1.xyz` = `t*(C2−C1)+C1`。）

**🔴 关键：`_MainTex` 只用到 `.r`，它的 rgb 被彻底丢掉；`_Noise` 只用来扰动 UV，不参与颜色。**
所以这一支**根本不是** `tex × vColor`（也不是 `tex × _Color × vColor`）。

### 一·1 DXBC 原文关键行（ps 段 #1，1632 字节，= Pass[0] 主 pass）

```
   mul r0.xy, cb0[19].xxxx, cb1[8].zwzz                            ; 时间 × _Scale_XY_Speed_ZW.zw
   frc r0.xy, r0.xyxx
   mad r0.xy, v1.xyxx, cb1[8].xyxx, r0.xyxx                        ; 噪声 UV
   sample_b r0.xyzw, r0.xyxx, t1.xyzw, s1, cb0[4].x                ; t1 = _Noise
   add r0.x, r0.x, l(-0.500000)
   mad r0.xy, cb1[3].xxxx, r0.xxxx, v1.xyxx                        ; _NoiseStrenght
   add r0.xy, r0.xyxx, l(-0.500000, -0.500000, 0.000000, 0.000000)
   mad r0.xy, cb1[3].yzyy, r0.xyxx, l(0.500000, 0.500000, …)       ; _MainTextScale
   sample_b r0.xyzw, r0.xyxx, t0.xyzw, s0, cb0[4].x                ; t0 = _MainTex
   add r0.x, r0.x, -cb1[6].x
   add r0.y, -cb1[6].x, cb1[6].y
   div r0.y, l(1.000000…), r0.y
   mul_sat r0.x, r0.y, r0.x                                        ; t = clamp01((m.r - _SmoothStep.x)/(_SmoothStep.y-_SmoothStep.x))
   mad r0.y, r0.x, l(-2.000000), l(3.000000)
   mul r0.x, r0.x, r0.x
   mul r0.x, r0.x, r0.y                                            ; t = t*t*(3-2t)
   log r0.yzw, |cb1[4].xxyz|  …（LinearToSRGB(_Color2)，见 §〇·4）…
   log r1.xyz, |cb1[5].xyzx|  …（LinearToSRGB(_Color1)）…
   add r0.yzw, r0.yyzw, -r1.xxyz
   mad r0.yzw, r0.xxxx, r0.yyzw, r1.xxyz                           ; = t*(SRGB(_Color2)-SRGB(_Color1)) + SRGB(_Color1) ⇒ t=0 出 _Color1、t=1 出 _Color2
   mul o0.xyzw, r0.yzwx, v2.xyzw                                   ; × 顶点色（rgb 与 a 各乘各的）
```

### 一·2 cb* 槽位清单（任务点名要的那张）—— **PS 阶段**

`cb0 = $Globals`（§〇·2 判据 ②(b) 直证），`cb1 = UnityPerMaterial`（同上）。

| 槽 | 名字 | 类型 | 在这一支里干什么 | 影响亮度？ |
|---|---|---|---|---|
| `cb0[4].x|y` | `_GlobalMipBias` | float2 | **两条 sample_b 的 LOD bias**（`cb0[4].z` 在 Burning Dissolve 里是 `_AlphaToMaskAvailable`，见 §四·2） | 间接（mip 层级＝糊不糊） |
| `cb0[19].x` | `_TimeParameters.x` | float4 的 .x | 噪声 UV 的自走速度（`frac(t * speed + uv*scale)`） | ❌ 不影响亮度（只挪 UV） |
| `cb1[3].x` | `_NoiseStrenght` | float | 噪声对主贴图 UV 的扰动幅度 | ❌ 不影响亮度 |
| `cb1[3].y|z` | `_MainTextScale` | float2 | 主贴图 UV 缩放（DXBC 紧凑打包在 `.y/.z`；反射表 std140 记的是 `+56`＝`.z/.w`） | ❌ |
| **`cb1[4].xyz`** | **`_Color2`** | Color4 | **渐变终点（t=1 时的颜色）** | ✅ **是颜色本尊** |
| **`cb1[5].xyz`** | **`_Color1`** | Color4 | **渐变起点（t=0 时的颜色）** | ✅ **是颜色本尊** |
| `cb1[6].x|y` | `_SmoothStep` | float2 | 主贴图 **`.r` 的窗口上下限**（决定 t，从而决定两个颜色怎么混） | ✅ **强烈影响**（整段亮度由它决定） |
| `cb1[8].xy|zw` | `_Scale_XY_Speed_ZW` | float4 | 噪声 UV 的缩放/自走速度 | ❌ |
| `cb1[0..2]` / `cb1[7]` | —— | —— | **反汇编里一次都没引用**（`_Radius`/`_FXForce`/`_Loops`/`_Displacement`/`_NOISECHANNEL` 都不在 ps 的 `UnityPerMaterial` 里） | ❌ |

**另外两处影响「看起来多亮」的东西，不在 cb 里**：

- **`vColor`（`v2.xyzw`）**：最终 `o0 = lerp(...) * vColor`，**rgb 与 a 都乘** ⇒ 粒子系统的着色/淡出直接改亮度。
- **`_MainTex` 的 `.r`**：它决定 t；`_SmoothStep` 窗口相对纹理亮度的位置一变，出的是 `_Color1` 还是 `_Color2` 就反过来。
  `Spiral Trail FX Smoke` 材质 `_SmoothStep = (0, 1)` ⇒ t = 纹理红通道本身。

### 一·3 贴图清单

| 寄存器 | 名字 | 用到的通道 | 作用 |
|---|---|---|---|
| `t0` / `s0` | `_MainTex` | **只有 `.r`** | 产 t（渐变权重）。**rgb 丢弃** |
| `t1` / `s1` | `_Noise` | **只有 `.r`** | UV 扰动的偏移量（`(r-0.5) * _NoiseStrenght`） |

**没有抓屏贴图。**

### 一·4 ps 段数 / 差异 / alpha / 顶点色

- **ps 段 3 个**，正好对应 3 个 pass（`m_SubShaders[0]` 3 个 pass，标签依次 `(主)` / `LIGHTMODE=MOTIONVECTORS` / `LIGHTMODE=DepthNormalsOnly`）：
  - `[1]` 1632 B = 主 pass（上面那段）
  - `[3]` 668 B = MotionVectors（`CLIP_POSITION_NO_JITTER`，`ne r0.z, cb0[42].y, l(0)`；**这一 pass 的 `cb0` 其实是 `UnityPerDraw`**，`m_NameIndices` 直证，见 §〇·1 注）
  - `[5]` 376 B = DepthNormalsOnly（`o0.xyz = normalize(v3.xyz); o0.w = 0`）
  - **主 pass 只有 1 个变体**（虽然属性里有 `_NOISECHANNEL_R/_G/_C/_A` 四个关键字，blob 里只存了读 **`.r`** 的那一份）
- **alpha = `t * vColor.a`**（`mul o0.xyzw, r0.yzwx, v2.xyzw`，`.w` 一路没被别的东西碰过）
- **顶点色参与**：`v2.xyzw`，rgb 与 a 都乘
- 着色器自带的标签：子着色器 `QUEUE='Transparent'`（=3000）、`RenderType='Transparent'`；
  该 shader 的材质 `Spiral Trail FX Smoke` 只设了 `_NOISECHANNEL/_NoiseStrenght/_Radius/_FXForce/_Loops/_Displacement/_QueueControl(0)/_QueueOffset(0)`，
  **没有覆盖混合/深入/队列** ⇒ 走 shader 侧默认值（本轮**没读到**那些默认值，不写）

### 一·5 对「`Buff_DA_Forest_Self` 亮度比 2.20」的含义

1. **最可能的一环是 §〇·4 的 `LinearToSRGB`**：这支的两个颜色是 HDR 实值（`Spiral Trail FX Smoke` 是 0.698/0.5，
   但同族别的材质有 4.76、16.06、501.96），不编码直接乘就会亮 2~5 倍。**先量这一项。**
2. **t 的来源被我们做错也会偏亮**：原版 t 只来自 `_MainTex.r` 经 `_SmoothStep` 窗口；
   若我们按 `tex.rgb` 乘亮度，纹理非纯红的地方就会多乘一份。
3. **`vColor` 一定不能漏**（粒子着色/淡出都在这）。
4. **`_Noise` 不该乘进颜色**（它只挪 UV）。
5. ⚠️ 材质上那些 `_Color01/_Color02/_Final_Color_Multiply/_SmoothStep_IN/_OUT` 是**别的 shader 的遗留**，
   这一支**一个都不读**。

---

## 二 · `Everguild/FX/Rays For Trail`

**一句话算式**：

```
rgb   = lerp( LinearToSRGB(_Color1), LinearToSRGB(_Color2), smoothstep01(clamp01((_MainTex(u').r - S0)/(S1 - S0))) ) * vColor.rgb
alpha = 1（不透明变体） / vColor.a（透明变体）
u'    = (v1.xy + _NoiseStrenght * (_Noise(v3.xy)[通道] - 0.5) - 0.5) * _MainTextScale + 0.5
```

**端点方向与 §一 相同：`t = 0 → _Color1`，`t = 1 → _Color2`。**

**与 Spiral Trail FX 是同一族**（属性表一样、颜色算式一字不差），差别只有两点：

1. **噪声 UV 不在 ps 里算** —— `v3.xy` 是**顶点着色器**插值进来的。VS 里那段是：
   ```
   mul o3.xy, v3.xyxx, cb2[2].xxxx, r0.xyxx     ; r0.xy = frac(_TimeParameters.x * cb2[2].yzyy)
   ```
   即 `uv = 顶点UV * _Scale_X_And_Speed_YZ.x + frac(time * _Scale_X_And_Speed_YZ.yz)`
   —— **属性名的字面意思就是它的用法**（x = Scale，YZ = Speed）。VS 里 `cb2 = UnityPerMaterial`（见 §〇·1 注）。
2. **噪声读哪个通道由关键字 `_NOISECHANNEL_*` 决定**，blob 里存了 4 个主 pass 变体：

| ps 段 | 字节 | 读的通道 | 输出 alpha |
|---|---|---|---|
| `[1]` | 1592 | `_Noise.r` | **`1.0`**（`mov o0.w, l(1.0)`，`v2` 只声明 `xyz`） |
| `[2]` | 1572 | `_Noise.r` | `vColor.a` |
| `[3]` | 1572 | `_Noise.b` | `vColor.a` |
| `[4]` | 1572 | `_Noise.a` | `vColor.a` |

材质实值可交叉：`_NOISECHANNEL` = `0` 的材质（Nurgle/Chaos Bullet/Wispy_Cut/Orbital）↔ 读 `.r`；
`= 2` 的（Pyrovore / EC Bullet）↔ 读 `.b`；`= 3` 的（Aeldari / Necron death trail）↔ 读 `.a`。
（⚠️ **是相关不是证明**：三份材质的 `m_ShaderKeywords` 在 bundle 里读出来是 `None`，关键字本身没读到；`0→.r / 2→.b / 3→.a` 这 3 个数据点与「4 个变体」自洽。）

### 二·1 DXBC 原文关键行（ps 段 #2，1572 字节，读 `.r` 的透明变体）

```
   log r0.xyz, |cb1[4].xyzx|  …（LinearToSRGB(_Color2)）…
   log r1.xyz, |cb1[5].xyzx|  …（LinearToSRGB(_Color1)）…
   add r0.xyz, r0.xyzx, -r1.xyzx
   sample_b r2.xyzw, v3.xyxx, t1.xyzw, s1, cb0[4].x          ; t1 = _Noise，UV 是顶点插值来的
   add r0.w, r2.x, l(-0.500000)                              ; ← 变体 [3]/[4] 这里是 r2.z / r2.w
   mad r2.xy, cb1[2].wwww, r0.wwww, v1.xyxx                  ; _NoiseStrenght
   add r2.xy, r2.xyxx, l(-0.500000, -0.500000, …)
   mad r2.xy, cb1[3].xyxx, r2.xyxx, l(0.500000, 0.500000, …) ; _MainTextScale
   sample_b r2.xyzw, r2.xyxx, t0.xyzw, s0, cb0[4].x          ; t0 = _MainTex
   add r0.w, r2.x, -cb1[6].x
   add r1.w, -cb1[6].x, cb1[6].y
   mul_sat r0.w, r0.w, r1.w
   mad r1.w, r0.w, l(-2.000000), l(3.000000)
   mul r0.w, r0.w, r0.w
   mul r0.w, r0.w, r1.w                                      ; smoothstep01
   mad r0.xyz, r0.wwww, r0.xyzx, r1.xyzx                     ; lerp(_Color1 → _Color2, t)：t=0 出 _Color1、t=1 出 _Color2
   mul o0.xyzw, r0.xyzw, v2.xyzw                             ; × 顶点色
```

### 二·2 cb 槽位表（PS 阶段）

| 槽 | 名字 | 作用 |
|---|---|---|
| `cb0[4].xy` | `_GlobalMipBias` | 采样 LOD bias |
| `cb1[2].w` | `_NoiseStrenght` | 噪声扰动幅度（✅ 反射表与指令都指向 `+44`，**两支拖尾这里编号不同**：Spiral 在 `cb1[3].x`） |
| `cb1[3].xy` | `_MainTextScale` | 主贴图 UV 缩放（✅ 反射表 `+48` 与指令 `cb1[3].xy` 一致） |
| **`cb1[4].xyz`** | **`_Color2`** | 渐变终点（t=1） |
| **`cb1[5].xyz`** | **`_Color1`** | 渐变起点（t=0） |
| `cb1[6].x|y` | `_SmoothStep` | 主贴图 `.r` 的窗口 |
| （VS 侧 `cb2[2]`） | `_Scale_X_And_Speed_YZ` | x = 噪声 UV 缩放，yz = 自走速度 —— **只在顶点阶段** |

### 二·3 贴图 / alpha / 顶点色 / 段数

| 寄存器 | 名字 | 通道 | 作用 |
|---|---|---|---|
| `t0`/`s0` | `_MainTex` | **`.r`** | 产 t |
| `t1`/`s1` | `_Noise` | `.r` / `.b` / `.a`（按关键字） | UV 扰动 |

- **ps 段 17 个 / pass 5 个**（`(主)` · `DepthOnly` · `MOTIONVECTORS` · `DepthNormalsOnly` · `SHADOWCASTER`）。
  主 pass **4 变体**（上表）；其余：`o0=v0.z`（3 个，296 B）· 运动矢量（3 个，692 B）· `o0.xyz=normalize(v4)`（4 个，400 B）· `o0=0`（3 个，316 B）。
  ⚠️ **哪一段属哪个 pass 是按出现顺序推的**（DXBC 里没有 pass 名），但顺序与 `LIGHTMODE` 标签序列**完全对上**。
- **alpha**：不透明变体恒 `1.0`；透明变体 `= vColor.a`
- **顶点色参与**：`v2`（透明变体是 `xyzw`，不透明变体只声明 `xyz`）
- **没有抓屏贴图**

### 二·4 对「`EC Sword Cut Board DMC Style` 亮度比 0.59 偏暗」的含义

同 §一·5 的 2/3/4 条，外加一条**本支独有的**：
**t 的两个端点方向**——`t=0` 出 `_Color1`、`t=1` 出 `_Color2`（`cb1[5]` 是起点、`cb1[4]` 是终点，**编号是反的**）。若把两个颜色接反，或在 t 上少乘一步
`smoothstep01`（原版是 `t²(3-2t)`，不是线性），某一段就会明显偏暗。
另外这一支的材质 `_SmoothStep` 常被设成 `(0, 1)` 或 `(0.72, 1)`，**窗口很窄时像素绝大部分落在 `_Color2` 附近** ——
`Wispy_Trail_EC Bullet` 的 `_Color1=(1.0,0.39,0.92,a=0.37)`、`_Color2=(4.76,0.97,3.51,a=0.16)`，
`_SmoothStep=(0.72,1.0)`：**t 只有在主贴图红通道 > 0.72 时才明显起来**。

---

## 三 · `Everguild/FX/Burning`

**一句话算式**：

```
rgb   = lerp( LinearToSRGB(_Warm), LinearToSRGB(_Hot), clamp01(f * bmDistorted.a) ) * bmDistorted.a
      + _Color.rgb * bmDistorted.rgb
alpha = 1.0
f     = smoothstep01( clamp01( (_FireNoise(v2.xy).r * _FireNoise(v2.zw).r * NM.x - NM.y) / (NM.z - NM.y) ) )
uv'   = (v1.xy + smoothstep01(clamp01((bm.a - DM.x)/(DM.y - DM.x))) * f * DM.z) * _BaseMap_ST.xy + _BaseMap_ST.zw
```

**🔴 这里 `_Color` 真的参与（一份，乘 `_BaseMap.rgb`）；颜色常量一共三份**（`_Hot`、`_Warm`、`_Color`）。
`_Hot`/`_Warm` 那一对里面还有一次**乘 `bmDistorted.a`**（先 lerp 再乘 alpha），**这是原版的预乘，不要省**。

### 三·1 DXBC 原文关键行（ps 段 #1，1992 字节，主 pass）

```
   mad r0.xy, v1.xyxx, cb1[1].xyxx, cb1[1].zwzz              ; _BaseMap_ST
   sample_b r0.xyzw, r0.xyxx, t0.xyzw, s0, cb0[4].x          ; t0 = _BaseMap（第一次，未扭曲）
   add r0.x, r0.w, -cb1[9].x
   add r0.y, -cb1[9].x, cb1[9].y
   mul_sat r0.x, r0.y, r0.x
   mad r0.y, r0.x, l(-2.000000), l(3.000000)
   mul r0.x, r0.x, r0.x
   mul r0.x, r0.x, r0.y                                      ; = smoothstep01((bm.a - DM.x)/(DM.y-DM.x))
   sample_b r1.xyzw, v2.xyxx, t1.xyzw, s1, cb0[4].x          ; t1 = _FireNoise @ v2.xy
   sample_b r2.xyzw, v2.zwzz, t1.xyzw, s1, cb0[4].x          ; 同一张 @ v2.zw
   mul r0.y, r1.x, r2.x                                      ; 两次采样都只取 .r，相乘
   mad r0.y, r0.y, cb1[7].x, -cb1[7].y                       ; × NM.x - NM.y
   add r0.z, -cb1[7].y, cb1[7].z
   div r0.z, l(1.000000…), r0.z
   mul_sat r0.y, r0.z, r0.y                                  ; + smoothstep01 →
   mul r0.z, r0.y, cb1[9].z                                  ; × DM.z（扭曲强度）
   mad r0.xz, r0.xxxx, r0.zzzz, v1.xxyx                      ; uv' = v1.xy + smoothstep01(bm.a) * f * DM.z
   mad r0.xz, r0.xxzx, cb1[1].xxyx, cb1[1].zzwz              ; 再乘一次 _BaseMap_ST
   sample_b r1.xyzw, r0.xzxx, t0.xyzw, s0, cb0[4].x          ; t0 = _BaseMap（第二次，已扭曲）
   mul_sat r0.x, r0.y, r1.w                                  ; k = clamp01(f * 第二次采样的 .a)
   log r0.yzw, |cb1[3].xxyz|  …（LinearToSRGB(_Hot)）…
   log r2.xyz, |cb1[4].xyzx|  …（LinearToSRGB(_Warm)）…
   add r0.yzw, r0.yyzw, -r2.xxyz
   mad r0.xyz, r0.xxxx, r0.yzwy, r2.xyzx                     ; lerp(_Warm → _Hot, k)
   mul r0.xyz, r0.xyzx, r1.wwww                              ; ★ × 第二次采样的 .a（预乘）
   mad o0.xyz, cb1[8].xyzx, r1.xyzx, r0.xyzx                 ; ★ += _Color.rgb * _BaseMap.rgb
   mov o0.w, l(1.000000)                                     ; ★ alpha 恒 1
```

### 三·2 槽位 / 贴图 / 段数

| 槽 | 名字 | 说明 |
|---|---|---|
| `cb1[1]` | `_BaseMap_ST` | 经典 `uv*xy+zw`，**用了两次** |
| `cb1[3]` | `_Hot` | 颜色（渐变终点） |
| `cb1[4]` | `_Warm` | 颜色（渐变起点） |
| `cb1[7].x/y/z` | `_Noise_Multiplier_X_smooth_step_YZ` | 属性名＝用法，一字不差 |
| `cb1[8].xyz` | `_Color` | **乘 `_BaseMap.rgb`（唯一一份 `_Color`）** |
| `cb1[9].x/y/z` | `_DistortMask_Smooth_step_XY_Intensity_Z` | 属性名＝用法，一字不差 |
| `cb0[4].x` | `_GlobalMipBias` | 采样 LOD bias |

| 寄存器 | 名字 | 通道 | 作用 |
|---|---|---|---|
| `t0`/`s0` | `_BaseMap` | `.rgb`（乘 `_Color`）与 `.a`（两次采样都只用 `.a` 做门/权重） | |
| `t1`/`s1` | `_FireNoise` | **只有 `.r`**，且在 `v2.xy` 与 `v2.zw` 各采一次后**相乘** | |

- **ps 段 5 个 / pass 5 个**（`(主)` · `DepthOnly` · `MOTIONVECTORS` · `DepthNormalsOnly` · `SHADOWCASTER`），每个 pass 恰好 1 个变体
- **顶点色不参与**（`v2` 是 `_FireNoise` 的两组 UV，不是 COLOR）
- **alpha 恒 1.0**；子着色器标签 `QUEUE='Geometry'`（**=2000**，5 支里唯一不是 Transparent 的）、`RenderType='Opaque'`
  ⇒ 这一支是**不透明**的自发光火焰（材质 `Burning` 只把 `_QueueControl` 设成 −1、`_QueueOffset` 设成 0，
  **没有改混合**）。⚠️ **这正是「2000 vs 3000」那件事最可能的来源**（见 §〇·3 第 1 条）

---

## 四 · `Everguild/FX/Burning Dissolve`

**一句话算式**（比 Burning 多一条「溶解切割」支线，颜色常量共 **5 份**）：

```
cut    = (s0 >= _FireNoise(v2.xy).r) ? 1 : 0                    ; s0 = ldst*(1+2b) - b
内边框色 = lerp( LinearToSRGB(_Dissolve_Inner_Color), LinearToSRGB(_Dissolve_Border_Color), t1 )
内体色   = lerp( LinearToSRGB(_Warm),                 LinearToSRGB(_Hot),                 k )
rgb    = 内边框色 * cut  +  内体色 * bmB.a  +  _Color.rgb * bmB.rgb
alpha  = 屏幕空间导数抗锯齿算出来的「覆盖率」（并据此 discard）；无 `_AlphaToMaskAvailable` 时写 1.0
```

### 四·1 DXBC 原文关键行（ps 段 #1，3648 字节，主 pass）

```
   add r0.x, cb1[11].y, l(1.000000)                       ; _BorderWidth
   add_sat r0.y, v1.z, cb1[11].x                          ; v1.z = 溶解进度(顶点流)，cb1[11].x = _DissolveAmmount
   mad r0.x, r0.y, r0.x, -cb1[11].y
   add r0.z, r0.x, -cb1[11].y
   sample_b r1.xyzw, v2.xyxx, t1.xyzw, s1, cb0[4].x       ; t1 = _FireNoise
   ge r0.z, r0.z, r1.x
   movc r0.zw, r0.zzzz, l(0,0,0,-0.500000), l(0,0,1.000000,0.500000)
   deriv_rtx r1.y, r0.z
   deriv_rty r0.z, r0.z
   add r0.z, |r0.z|, |r1.y|                               ; fwidth 式抗锯齿
   …
   ne r1.z, cb0[4].z, l(0.000000)                         ; cb0[4].z = _AlphaToMaskAvailable
   movc o0.w, r1.z, r0.z, l(1.000000)
   lt r0.z, r0.w, l(0.000000)
   discard_nz r0.z                                        ; ★ 硬边裁掉
   log r1.yzw, |cb1[12].xxyz|  …（LinearToSRGB(_Dissolve_Border_Color)）…
   log r2.xyz, |cb1[3].xyzx|   …（LinearToSRGB(_Dissolve_Inner_Color)）…
   add r1.yzw, r1.yyzw, -r2.xxyz
   mad r0.yzw, r0.yyyy, r1.yyzw, r2.xxyz                  ; lerp(Inner → Border, t1)
   sample_b r2.xyzw, v2.zwzz, t1.xyzw, s1, cb0[4].x       ; _FireNoise 第二次（@v2.zw）
   mul r1.x, r1.x, r2.x                                   ; 两次 .r 相乘
   mad r1.x, r1.x, cb1[8].x, -cb1[8].y                    ; _Noise_Multiplier_X_smooth_step_YZ
   …
   mad r1.zw, v1.xxxy, cb1[1].xxxy, cb1[1].zzzw           ; _BaseMap_ST
   sample_b r2.xyzw, r1.zwzz, t0.xyzw, s0, cb0[4].x       ; t0 = _BaseMap（未扭曲）
   …（用 .w 做 _DistortMask 的 smoothstep → 扭曲量）…
   sample_b r2.xyzw, r1.yzyy, t0.xyzw, s0, cb0[4].x       ; t0 = _BaseMap（已扭曲）
   mul_sat r1.x, r1.x, r2.w                               ; k = clamp01(f * bmB.a)
   log r1.yzw, |cb1[4].xxyz|  …（LinearToSRGB(_Hot)）…
   log r3.xyz, |cb1[5].xyzx|  …（LinearToSRGB(_Warm)）…
   mad r1.xyz, r1.xxxx, r1.yzwy, r3.xyzx                  ; lerp(_Warm → _Hot, k)
   mul r1.xyz, r1.xyzx, r2.wwww                           ; ★ 预乘 bmB.a
   mad r1.xyz, cb1[9].xyzx, r2.xyzx, r1.xyzx              ; ★ += _Color.rgb * bmB.rgb
   mad o0.xyz, r0.yzwy, r0.xxxx, r1.xyzx                  ; ★ += 内边框色 * cut
```

### 四·2 槽位 / 贴图 / 段数

| 槽 | 名字 | 作用 |
|---|---|---|
| `cb1[1]` | `_BaseMap_ST` | 两次采样都用它 |
| `cb1[3].xyz` | `_Dissolve_Inner_Color` | 溶解区「内侧」色 |
| `cb1[4].xyz` | `_Hot` | 火焰渐变终点 |
| `cb1[5].xyz` | `_Warm` | 火焰渐变起点 |
| `cb1[8].x/y/z` | `_Noise_Multiplier_X_smooth_step_YZ` | 属性名＝用法 |
| `cb1[9].xyz` | `_Color` | **乘 `_BaseMap.rgb`** |
| `cb1[10].x/y/z` | `_DistortMask_Smooth_step_XY_Intensity_Z` | 属性名＝用法 |
| `cb1[11].x` | `_DissolveAmmount` | 溶解进度偏移 |
| `cb1[11].y` | `_BorderWidth` | 边框宽度 |
| `cb1[12].xyz` | `_Dissolve_Border_Color` | 溶解区「外缘」色 |
| `cb0[4].x` / `cb0[4].z` | `_GlobalMipBias` / `_AlphaToMaskAvailable` | 后者由 Unity 名字表直证（Pass[0] 的 `m_NameIndices` 里 `$Globals` 后面紧跟 `_GlobalMipBias`、`_AlphaToMaskAvailable`） |

| 寄存器 | 名字 | 通道 | 作用 |
|---|---|---|---|
| `t0`/`s0` | `_BaseMap` | `.a` 做门 + 扭曲量，`.rgb` 乘 `_Color` | |
| `t1`/`s1` | `_FireNoise` | **只有 `.r`**，`v2.xy` / `v2.zw` 各采一次后相乘 | |

- **ps 段 5 个 / pass 5 个**（主 · DepthOnly · MOTIONVECTORS · DepthNormalsOnly · SHADOWCASTER）
  - `[1]` 3648 B 主 · `[3]` 592 B DepthOnly（只采 `_FireNoise` 做 discard）· `[5]` 980 B 运动矢量 + discard · `[7]` 688 B DepthNormals + discard · `[9]` 612 B ShadowCaster + discard
  - ⚠️ **DepthNormals/ShadowCaster pass 在 frag 里也 `discard`** —— 溶解期间影子/法线要跟着镂空，我们若省掉这一步会出现「影子还在、本体没了」
- **顶点色不参与**（`v2` 是 `_FireNoise` 的 UV）
- **alpha**：不是纹理 alpha，而是**硬边覆盖率**（`fwidth` 式抗锯齿）→ 写进 `o0.w` 供 alpha-to-mask，并据此 `discard`；关掉 alpha-to-mask 时写 1.0
- **`v1.z` 是溶解进度**（顶点流来的插值量），不是贴图通道
- 队列标签 `QUEUE='AlphaTest'`（=2450）、`RenderType='Opaque'` ⇒ **裁切型不透明**，不是半透明

---

## 五 · `Everguild/FX/Particle Distortion Affect Transparents`（抓屏扭曲）

**一句话算式（主 pass）**：

```
alphaDistort = pow( clamp01( (sceneDepth - fragClipW) / _Depth_And_Fallof.x ), _Depth_And_Fallof.y ) * vColor.a
offset       = (_DistortTex(v3.w, v4.w).r * _DistortTex(v3.w, v4.w).a * 2 - 1) * _DistortionStrength
rgb.a        = _GrabPassTransparent( float2(screenUV.x, 1 - screenUV.y) + alphaDistort * offset ) * vColor
alpha        = 上面那个 .a （带 _USEMASK 时再 × _Mask(v1.xy).a）
```

**🔴 结论先行：`_GrabPassTransparent` 真的被采样**（不是「声明了没读」）——
它读的是**屏幕空间的重采样**，这就是「抓屏扭曲」；UV 是 `翻转 Y 的屏幕 UV + 扭曲偏移`。

### 五·1 贴图绑定（三条独立证据都对上）

| 变体（按 DXBC `dcl_resource` 条数） | t0 | t1 | t2 | t3 | 对应的 ps 段 |
|---|---|---|---|---|---|
| 2 张（940 B） | **`_GrabPassTransparent`** | `_DistortTex` | — | — | `[9] [14] [19] [22]` … |
| 3 张（1076 B） | **`_GrabPassTransparent`** | `_DistortTex` | `_Mask` | — | `[10] [11] [15] [16] [20] [21] [23]` … |
| 3 张（2260 B，带软粒子） | `_CameraDepthTexture` | **`_GrabPassTransparent`** | `_DistortTex` | — | `[12] [17]` |
| 4 张（2396 B，软粒子+遮罩） | `_CameraDepthTexture` | **`_GrabPassTransparent`** | `_DistortTex` | `_Mask` | `[13] [18]` |

证据：① blob 绑定表（`@13564` 起三条：`_GrabPassTransparent`=0 / `_DistortTex`=1 / `_Mask`=2；`@13408` 起两条：`_GrabPassTransparent`=0 / `_DistortTex`=1；带深度那组是 `_CameraDepthTexture`=0 / `_GrabPassTransparent`=1 / `_DistortTex`=2 / `_Mask`=3）；
② 每张表的条数与同组 `dcl_resource` 条数逐个吻合；③ 用法自洽（t0 在 2260/2396 变体里被 `_CameraDepthTexture_TexelSize` 夹 + `_RTHandleScale` 缩、并拿 `.x` 去反投影算深度）。

### 五·2 DXBC 原文关键行

`[13]`（2396 B，主 pass 的完整变体）关键段：

```
   mul r0.x, v3.y, cb0[79].w
   mad r0.x, cb0[78].w, v3.x, r0.x
   mad r0.x, cb0[80].w, v3.z, r0.x
   add r0.x, r0.x, cb0[81].w                              ; cb0[78..81] = unity_MatrixVP → r0.x = 顶点 clipPos.w
   mov r1.x, v0.x
   div r1.xy, r1.xyxx, cb0[3].xyxx                        ; 屏幕 UV
   mov r2.x, v3.w
   mov r2.y, v4.w
   sample_b r2.xyzw, r2.xyxx, t2.xyzw, s2, cb0[4].x       ; t2 = _DistortTex（UV 来自 VS 的 UV 动画）
   mul r2.x, r2.x, r2.w                                   ; .r × .a
   mad r0.yz, r2.xxyx, l(0,2,2,0), l(0,-1,-1,0)
   mul r0.yz, r0.yyzy, cb1[0].xxxx                        ; × _DistortionStrength
   sample_b r2.xyzw, r2.xyxx, t0.xyzw, s0, cb0[4].x       ; t0 = _CameraDepthTexture
   …（用 .x 做 Linear01Eye/LinearEyeDepth，再 |z| 取深度）…
   add r0.x, -r0.x, r0.w                                  ; sceneDepth - fragDepth
   div_sat r0.x, r0.x, cb1[2].x                           ; ÷ _Depth_And_Fallof.x
   log r0.x, r0.x
   mul r0.x, r0.x, cb1[2].y
   exp r0.x, r0.x                                         ; pow(…, _Depth_And_Fallof.y)
   mul r0.x, r0.x, v2.w                                   ; × 顶点色 .a
   mad r1.xy, r1.xyxx, l(1.000000, -1.000000, 0, 0), l(0, 1.000000, 0, 0)   ; 翻转 Y
   mad r0.xy, r0.xxxx, r0.yzyy, r1.xyxx                   ; 屏幕UV + 衰减×扭曲
   sample_b r0.xyzw, r0.xyxx, t1.xyzw, s1, cb0[4].x       ; ★ t1 = _GrabPassTransparent
   mul r0.xyzw, r0.xyzw, v2.xyzw                          ; × 顶点色
   sample_b r1.xyzw, v1.xyxx, t3.xyzw, s3, cb0[4].x       ; t3 = _Mask
   mul o0.w, r0.w, r1.w                                   ; 只乘进 alpha
   mov o0.xyz, r0.xyzx
```

`[9]`（940 B，无深度的最简变体）关键段 —— **同一套写法，只是少了深度那一截**：

```
   mov r0.x, v3.w
   mov r0.y, v4.w
   sample_b r0.xyzw, r0.xyxx, t1.xyzw, s1, cb0[4].x       ; t1 = _DistortTex
   mul r0.x, r0.x, r0.w
   mad r0.xy, r0.xyxx, l(2,2,0,0), l(-1,-1,0,0)
   mul r0.xy, r0.xyxx, cb1[0].xxxx
   …（屏幕 UV + 翻转 Y）…
   mad r0.xy, v2.wwww, r0.xyxx, r1.xzxx                   ; ★ 这里乘法的顺序是「顶点色 .a × 扭曲」
   sample_b r0.xyzw, r0.xyxx, t0.xyzw, s0, cb0[4].x       ; ★ t0 = _GrabPassTransparent
   mul o0.xyzw, r0.xyzw, v2.xyzw
```

### 五·3 槽位 / alpha / 顶点色 / 段数

| 槽 | 名字 | 作用 |
|---|---|---|
| `cb0[3].xy` | 屏幕尺寸（URP `_ScaledScreenParams`） | 屏幕 UV；**这一支的 Pass[0] 名字表里确实有 `_ScaledScreenParams`**，但槽位是**按用法认的**（合并 `$Globals` 里 `_ScaledScreenParams`/`_GlobalMipBias`/`_ProjectionParams` 相邻，而 `cb0[4].x` 已由 mip bias 定死在 `_GlobalMipBias`） |
| `cb0[4].x` | `_GlobalMipBias` | 采样 LOD bias |
| `cb0[24].z/.w`、`cb0[25].w` | `_ZBufferParams`、`unity_OrthoParams` | 线性化深度 / 正交分支（**名字由 DXBC `$Globals` 成员表直证**） |
| `cb0[28].xy`、`cb0[130].xy` | `_RTHandleScale`、`_CameraDepthTexture_TexelSize` | 抓屏/深度的 UV 缩放与夹边（**直证**） |
| `cb0[66..69]`、`cb0[78..81]`、`cb0[82..85]` | `unity_MatrixV` / `unity_MatrixVP` / `unity_MatrixInvVP` | 反投影算世界位置（**直证**） |
| **`cb1[0].x`** | **`_DistortionStrength`** | 扭曲强度（本族 `cb1` 里被引用最多的材质槽：60 处） |
| **`cb1[2].x|y`** | **`_Depth_And_Fallof`** | 深度衰减的**距离**与**指数**（`pow(sat((sceneDepth-fragDepth)/x), y)`） |

**VS 侧**（⚠️ `cb2 = UnityPerMaterial`，见 §五·4）：

```
   mul r0.xy, cb0[19].xxxx, cb2[4].xyxx      ; _TimeParameters.x × _UVSpeed.xy
   frc r0.xy, r0.xyxx
   mad r0.xy, v3.xyxx, cb2[4].zwzz, r0.xyxx  ; × _UVScale.zw
   mov o3.w, r0.x
   mov o4.w, r0.y                            ; ← 这就是 ps 里的 v3.w / v4.w
```

- **贴图**：`_GrabPassTransparent`（抓屏）· `_CameraDepthTexture`（软粒子深度）· `_DistortTex`（扭曲图，只读 `.r×.a`）· `_Mask`（`_USEMASK` 时只取 `.a`）
- **顶点色参与**：`v2.xyzw`，**rgb 与 a 都乘**；`v2.w` 还额外乘在深度衰减上
- **alpha** = `_GrabPassTransparent.a * vColor.a`（`_USEMASK` 时再 × `_Mask.a`）
- **ps 段 45 个 / pass 3 个**（`(主)` · `MOTIONVECTORS` · `DepthNormalsOnly`）：
  主 pass 15 个变体（940 / 1076 / 2260 / 2396 四种体量，来自 `MATERIAL_QUALITY_* × _USEMASK × _SOFTPARTICLES` 的组合）、
  运动矢量 15 个（716 B）、DepthNormals 15 个（400 B）
- ⚠️ **`_UVSpeed` / `_UVScale` 只在 VS 里被读**（ps 里 `cb1` 只用到 `[0].x` 与 `[2].x/.y`，`cb0[19]` 在 ps 里一次都没出现）
  ⇒ **UV 动画全在顶点着色器**；只在 ps 里找它是找不到的。

### 五·4 对「抓屏族」的两条操作口径

1. **`_GrabPassTransparent` 必须绑**（`GrabPassTransparentFeature.cs` 的方向正确），而且它绑的是
   **透明物画完之后**的相机颜色 —— 名字里的 `Transparents` 就是这个意思。
2. ⚠️ **不要在 ps 里按 `cb1` 找 `_UVSpeed`/`_UVScale`**；照 §五·3 的 VS 段去接（VS 里是 `cb2[4]`）。

---

## 六 · 横向对照表

| shader | 颜色常量几份 | `_Color` 参与？ | 顶点色 | 贴图 | alpha 来源 | ps 段 / pass | 队列标签 |
|---|---|---|---|---|---|---|---|
| `Spiral Trail FX` | **2**（`_Color1`/`_Color2`，各过一次 LinearToSRGB 再 lerp） | **没有 `_Color` 这个属性** | ✅ `v2.rgb` 与 `.a` | `_MainTex`(只 `.r`) · `_Noise`(只 `.r`，挪 UV) | `t * vColor.a` | 3 / 3 | Transparent(3000) |
| `Rays For Trail` | **2**（同上） | 同上 | ✅ | `_MainTex`(只 `.r`) · `_Noise`(.r/.b/.a 按关键字) | 不透明变体＝1；透明变体＝`vColor.a` | 17 / 5 | Transparent(3000) |
| `Burning` | **3**（`_Hot`/`_Warm` 一对 + `_Color` 一份） | ✅ **乘 `_BaseMap.rgb`** | ❌ | `_BaseMap`(.rgb/.a) · `_FireNoise`(只 `.r`，采两次相乘) | 恒 1.0 | 5 / 5 | **Geometry(2000)** |
| `Burning Dissolve` | **5**（`_Dissolve_Inner_Color`/`_Dissolve_Border_Color` + `_Hot`/`_Warm` + `_Color`） | ✅ **乘 `_BaseMap.rgb`** | ❌ | `_BaseMap` · `_FireNoise`(只 `.r`) | 硬边覆盖率 + `discard` | 5 / 5 | AlphaTest(2450) |
| `Particle Distortion Affect Transparents` | **0 个颜色常量**（只有 `_DistortionStrength` 与 `_Depth_And_Fallof` 两个数值参数） | ❌ 没有 `_Color` | ✅ `v2.rgba` | **`_GrabPassTransparent`** · `_CameraDepthTexture` · `_DistortTex` · `_Mask` | `grab.a * vColor.a (× mask.a)` | 45 / 3 | Transparent(3000) |

**一句话分类**：

- **拖尾族（2 支）**：`tex.r` 当权重 → 两色渐变 × 顶点色。**不是** `tex × vColor`，**没有** `_Color`。
- **燃烧族（2 支）**：`_Hot`/`_Warm`（Burning）或再加溶解两色（Burning Dissolve）的渐变 × `_BaseMap.a` **预乘**，
  **外加一份 `_Color × _BaseMap.rgb`**；噪声只用来切/扭 UV。`Burning` 不透明、`Burning Dissolve` 裁切。
- **抓屏族（1 支）**：颜色**全部来自抓屏贴图**，材质只给「扭曲强度」和「深度衰减」。

---

## 七 · 出处与复跑清单

| 结论 | 出处 |
|---|---|
| 5 支的 blob 字节数 / ps 段数 | `disasm_dxbc.py "<名>" --stage ps`（本次落盘在 `d:/4/_tmp_view/shader4/*_ps.txt`，**临时目录，可删**） |
| Spiral Trail FX 两份 sha1 相同 | `hashlib.sha1(blob)`，两份都 `70a317d52521`；`d:/4/_tmp_view/shader4/cmp_bundles.py` |
| 队列标签 / pass 标签 | `SerializedShader.m_SubShaders[0].m_Passes[*].m_State.m_Tags`（`m_NameIndices` 同来源） |
| `cb0=$Globals` / `cb1=UnityPerMaterial` / 贴图寄存器 | `SerializedPass.m_NameIndices` + `progFragment.m_CommonParameters.m_ConstantBufferBindings` / `m_TextureParams` |
| `cb1[N]` 名字与字节偏移 | blob 里的 cbuffer 反射块（`PGlobals*`/`VGlobals*` 打头那半）—— 解析脚本见本节末 |
| `_GrabPassTransparent` = t0/t1 | blob 绑定资源表（`@13408` / `@13564` / `@14136` / `@14728`） |
| 材质实值 | `Material.m_SavedProperties`（`m_Floats` / `m_Colors` / `m_TexEnvs`）+ `m_CustomRenderQueue`，UnityPy 直读；15 个材质清单见 `d:/4/_tmp_view/shader4/mats_out.txt` |
| 混合/深入状态 | **本轮没从这两个包读到可靠的混合状态**（`SerializedShaderState` 的 blend 字段在 UnityPy 这一版读出来是 `None`），所以 §一~§五 只写**队列/RenderType 标签**与**算出来的 alpha**，不写混合方程。`资料/普查产出_0917/shader属性表_块2.md` 里那一栏**未复核**，仅作旁证 |
| `WarpforgeShaderMap` 里 PDAT 已映射 | `Unity/MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs:78` |

**本轮用的临时脚本**（都在 `d:/4/_tmp_view/shader4/`，**不在工程里**，可整目录删；要复跑随便挑）：
`refl.py`（扫 cbuffer 反射块 → 名字/分量数/偏移）· `restab2.py`（扫绑定资源表 → 贴图名↔寄存器）·
`globals_parse.py`（`$Globals` 成员名→cb0 槽）· `nameidx.py`（Unity 自己的 name↔slot 表）·
`cmp_bundles.py`（跨 bundle 比 blob/队列）· `mats.py`（材质实值）。

**没查到 / 没定死的**（不猜，留在这儿）：

1. **`_NOISECHANNEL` 的数值 ↔ 通道**：只有 3 个数据点（0/2/3 ↔ .r/.b/.a）与 4 个 ps 变体自洽，
   材质的 `m_ShaderKeywords` 在 bundle 里读出来是 `None` ⇒ **关键字本身没读到**，没法把「哪个关键字名对哪个变体」钉死。
2. **`Spiral Trail FX` 的 `LinearToSRGB` 是哪个美术节点引入的**（ShaderGraph 的 Colorspace 节点？还是作者手写）—— 读不到（源码被剥）。
3. **`Spiral Trail FX` 的 `cb1[0..2]` / `cb1[7]` 是谁**：反汇编一次都没引用，反射表也只列 6 个成员 ⇒ **未证明**（不影响算式）。
4. **`Buff_DA_Forest_Self` / `EC Sword Cut Board DMC Style` 这两个效果自己的材质**：在
   `battleprefabs_vfxandmisc_assets_all.bundle` 与 `shaders_assets_all.bundle` 里**没扫到同名材质**
   （两支各只有 15 个材质引用这 5 个 shader）⇒ 它们的材质在**别的 bundle 或预制体内**，本轮没取到实值。
5. **`Particle Distortion Affect Transparents` 的 `cb0[3]`**：按用法＝屏幕尺寸，**反射表里没有它的槽位**（只有名字）。
