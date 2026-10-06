# E 组 shader 算式 · 块1「遮罩族」（5 个原版 shader 的**像素着色器**反汇编）

> 2026-09-19 · 只读子代理产出。数据源：原版 bundle 里的 **DXBC 字节码**（`UnityPy` + Windows 自带
> `d3dcompiler_47.dll` 的 `D3DDisassemble`，**未启动 Unity**）+ 原版材质 `m_SavedProperties` + shader 属性表。
> 复跑命令（**必须带 `PYTHONIOENCODING`**）：
> ```
> PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
>   d:/4/Unity/工具/disasm_dxbc.py "<shader 名片段>" --stage ps
> ```
> 每段 ps 的**指令流逐行**都在本文件里能对上；引用的行都是从反汇编文本原样抄的，没有改写。

**这 5 个 shader 是谁**：`EffectExporter.cs:52` 的 `ShaderMap` 里它们**全部**映射到自建
`WarpforgeVFX/Particles/Extra Color*`（标了 `*` = 近似替代）。2026-09-19 我们从这个自建 shader 里
**删掉了一次 `_Color` 乘法**，结果 7 条效果**从偏亮翻成偏暗** ⇒ 说明有别的原版 shader **本来就该用颜色常量**。
本文件就是来把「到底哪几个该用、以什么形状用」用字节码钉死的。

**受影响效果数**（出处 `资料/普查产出_0913/效果_shader_对账.md:135-147`，与 `普查产出_0917/效果_shader对账_小结.md:33-36` 一致）：

| shader | 用它做材质的效果数 |
|---|---:|
| `Everguild/FX/Multi Ray` | 33 |
| `Everguild/FX/Alpha Mask One Layer` | 29 |
| `Everguild/FX/Particle Dissolve Mask` | 29 |
| `Everguild/FX/Particle Shine Custom Vertex Streams` | 26 |
| `Everguild/FX/Alpha Masks Two Layer` | 9 |

---

## 结论总表

| # | 原版 shader | 判定 | 颜色算式（一行） | 顶点色 | 颜色常量几份 |
|---|---|---|---|---|---|
| 1 | `Particle Dissolve Mask` | **C** | `base = tex.a·(S(_Color)+tex.rgb)·vcol`；`o0.rgb = lerp(base, base·(v1.z,v1.w,v2.x), d)`；溶解带另加 `d·S(_Add_Color)` | ✅ v3 | **2**（`_Color` / `_Add_Color`） |
| 2 | `Alpha Mask One Layer` | **B** | `o0.rgb = t0·t1·_Color_Mutliplier·vcol` | ✅ v1 | **1**（**标量**灰度，不是 `_Color`） |
| 3 | `Alpha Masks Two Layer` | **C** | `o0.rgb = lerp(S(_Blend_Color), Overlay(S(_Blend_Color), tex·k·vcol), _Color_Multiplier)` | ✅ v2 | **1**（`_Blend_Color`）+ 两个噪声图 |
| 4 | `Multi Ray` | **C** | `o0.rgb = remap( Σ_N _ColorN·tex(uvN)[ch_N]·vcol )` | ✅ v2 | **4**（`_Color1..4`） |
| 5 | `Particle Shine Custom Vertex Streams` | **C** | `o0 = (tex + Shine_Color·smoothstep(shineTex)·k)·vcol` | ✅ v2 | **1**（走**加法**，不是整体乘子） |

**判定口径**（照派活时的定义）：**A** = 不含颜色常量 ⇒ 我们删掉 `_Color` 的自建 shader 是对的 ·
**B** = 含**一份**颜色常量乘子 ⇒ 该给它一个会乘 `_Color` 的自建 shader · **C** = 更复杂（双色/溶解/遮罩/
sRGB 转换/多贴图混合）⇒ 记形状，**可能得各自建**。

🔴 **最关键的一条（直接影响「7 条效果翻暗」那个案子）**：**这 5 个里唯一有真 `_Color` 属性、且材质上真的
带非白值的是 `Particle Dissolve Mask`**（18 个材质里 **16 个** `_Color` 非白、且是 HDR/高饱和值，见 §1·四）。
但它**不是乘子** —— 原版的 `_Color` 是**加**进贴图色的（`S(_Color) + tex.rgb`）**再**被 `tex.a·vcol` 缩放，
而且要**先过一条 sRGB 编码曲线**。⇒ 「删掉乘法 ⇒ 变暗」这个方向是对的（去掉了颜色常量就等于当它 = 白），
**但把 `_Color` 当乘子加回去也不等于原版**。另外 4 个 shader 的属性表里**根本没有 `_Color`**
（`Multi Ray` 是 `_Color1..4`、`Particle Shine` 是 `Color_a142…`、两个 Alpha Mask 是 `_Color_Multiplier/_Color_Mutliplier`），
材质上的 `_Color` 是 **URP Lit 残留的死值**（`Particle Shine` 那 11 个材质逐个核过，见 §5·四）。

---

## §0 · 记号与三条先钉死的前提

**记号**

| 记号 | 含义 |
|---|---|
| `tex` / `t0,t1,t2` | 贴图采样结果。**贴图顺序 = shader 属性表里 Texture2D 的声明顺序**（`_MainTex` → t0 …） |
| `v1,v2,v3,v4` | 顶点着色器传下来的插值寄存器（`INTERP`）。`v0` 是 `SV_POSITION`，不参与颜色 |
| `vcol` | 顶点色（**哪个 v 是顶点色逐 shader 写清**，不能默认 v1） |
| `cbN[M]` | 常量缓冲 `N` 的第 `M` 个 float4。`cb0` 是 Unity 全局、`cb1` 是每材质 |
| `S(c)` | **sRGB 编码曲线**（linear→sRGB 方向），见下 |
| `Overlay(a,b)` | Photoshop `叠加`：`b≤0.5 ? 2ab : 1−2(1−a)(1−b)` |
| `d` | 溶解带标志（0 或 1） |

**① `cb0[4].x` 是 LOD bias，不是颜色。** 每个 `sample_b` 的最后一位都是 `cb0[4].x`（5 个 shader 无一例外）。
派活时就提醒过这条，本文件所有算式里**没有把它当常量用过**。

**② `S(c)` 的定义（原样抄自 DXBC，三条指令）**

```
   log  r, |c|                                    ; 取 log
   mul  r, r, 0.416667                            ; × 1/2.4
   exp  r, r                                      ; ⇒ c^(1/2.4)
   mad  r, r, 1.055, -0.055                       ; 1.055·c^(1/2.4) − 0.055
   ge   m, 0.003131, c ; mul r2, c, 12.923210 ; movc r, m, r2, r
                                                  ; c ≤ 0.003131 时改走 c·12.92321
```
这是**标准的 sRGB 传输曲线**（低段 `×12.92`、高段 `1.055·c^(1/2.4)−0.055`，阈值 0.003131）。
我按**曲线本身**叫它「sRGB 编码」；**它是不是 Unity 的 `LinearToSRGB` 从字节码里证不出来**（不写死）。
🔴 **它对算式的影响**：带它的 shader 里，颜色常量**不是直接乘进去的**，而是先被这条非线性曲线重映射
（例：0.5 → 0.735，10.68 → 2.78）⇒ **用 `_Color` 做一个线性乘子永远不等于原版**。

**③ RDEF 缺失 ⇒ 拿不到「属性名 ↔ cb 偏移」的官方映射。**
实测每个 DXBC 段的 chunk 只有 `ISGN,OSGN,SHDR` 三块（`工具/disasm_dxbc.py` 头注也写了这点），
**没有 `RDEF`** = 常量缓冲里的变量名没编进去。所以下面每节里的「`cb1[4].y` 就是 `_Color_Mutliplier`」
**都是推断**，我给的是**证据链**（属性类型/默认值 + 材质上的实际值 + 用法语义），不是字节码直读。
每条推断都标了「（推）」。

---

## §1 · `Everguild/FX/Particle Dissolve Mask`

**判定 = C**（双颜色常量 + 溶解遮罩 + sRGB 转换 + 双贴图）。

### 一 · 资源表（主变体）

| 项 | 值 |
|---|---|
| DXBC 总段数 | **67**（判活时按整 shader；同一个 blob 在 3 个包里**字节完全相同**，sha1 `dff5d8d08d`） |
| ps 段 | **37**（含多 pass × 关键字组合）· 不同的**指令流** 16 种 |
| pass 数 | 5 |
| 常量缓冲 | `CB0[5]`（Unity 全局）+ `CB1[6]`（每材质） |
| 贴图 / 采样器 | `t0,t1` / `s0,s1` —— t0 = `_MainTex`、**t1 = `_Disolve`**（溶解遮罩图，材质 `m_TexEnvs` 里两个都在） |
| temps | 3~4 |
| 关键字 | `_DISSOLVE_CHANNEL_R`（换遮罩通道 → 换掉 ps 变体）、`BOOLEAN_…` 两个、`_SURFACE_TYPE_TRANSPARENT` |

16 种指令流里**只有 8 种真的输出颜色**（idx 6–13），其余是深度/法线/空 pass。
8 种颜色变体只差三件事：**① 遮罩取哪个通道**（`.x`/`.y`/`.z`/`.w`，关键字定）**② 有没有 alpha 输出**
**③ 有没有 dissolve 的 lerp**。核心算式三种形状见下。

### 二 · 颜色算式（idx=8 那一族 = 最完整的一种）

```
C     = S(cb1[5].xyz)                      // 颜色常量② —— 就是 _Color（推，见「四」）
A     = S(cb1[4].xyz)                      // 颜色常量① —— _Add_Color（推）
uv    = v1.xy * cb1[2].xy + cb1[2].zw      // = _Disolve_ST
mask  = t1.Sample(uv)[ch]                  // ch 由 _DISSOLVE_CHANNEL / _DISSOLVE_CHANNEL_R 关键字定
d     = (mask 落在 [cb1[3].y, cb1[3].x] 带内) ? 1 : 0     // 二值，不是渐变
tex   = t0.Sample(v1.xy)                   // _MainTex_ST 在 VS 里已套过（ps 里直接吃 v1.xy）
base  = tex.a * (C + tex.rgb) * vcol       // vcol = v3.xyzw
o0.rgb = lerp( base , base * (v1.z, v1.w, v2.x) , d )      // 溶解带里换成另一路顶点流颜色
o0.a   = (tex.a + C.a) * tex.a * vcol.a * (v2.z ≥ mask ? 1 : 0)
```

**另一支形状（idx=6，没有 lerp 也没有 alpha）**：
```
o0.rgb = tex.a * (C + tex.rgb) * vcol + d * A      // 溶解带 = 加法，不是 lerp
o0.w   = 1
```
**第三支（idx=7）**：`o0.rgb = lerp(base, base·(v1.z,v1.w,v2.x), d)`、`o0.w = 1`（有 lerp 无 alpha）。
idx=13 是**相机淡出** pass（多出 `deriv_rtx/deriv_rty` 做 alpha 抖动 + `discard_nz`），颜色算式与 idx=8 同。

### 三 · DXBC 原文关键行（idx=8，逐字抄）

```
   log r0.xyz, |cb1[5].xyzx|
   mul r0.xyz, r0.xyzx, l(0.416667, 0.416667, 0.416667, 0.000000)
   exp r0.xyz, r0.xyzx
   mad r0.xyz, r0.xyzx, l(1.055000, 1.055000, 1.055000, 0.000000), l(-0.055000, -0.055000, -0.055000, 0.000000)
   ge r1.xyz, l(0.003131, 0.003131, 0.003131, 0.000000), cb1[5].xyzx
   mul r2.xyz, cb1[5].xyzx, l(12.923210, 12.923210, 12.923210, 0.000000)
   movc r0.xyz, r1.xyzx, r2.xyzx, r0.xyzx          ; ← 到这里 r0 = S(cb1[5].xyz) = S(_Color)
   sample_b r1.xyzw, v1.xyxx, t0.xyzw, s0, cb0[4].x
   add r0.xyz, r0.xyzx, r1.xyzx                    ; S(_Color) + tex.rgb   ← **加**，不是乘
   add r0.w, r1.w, cb1[5].w                        ; tex.a + _Color.a
   mul r0.xyzw, r1.wwww, r0.xyzw                   ; × tex.a
   mul r0.xyzw, r0.xyzw, v3.xyzw                   ; × 顶点色
   mov r1.xy, v1.zwzz
   mov r1.z, v2.x                                  ; (v1.z, v1.w, v2.x) = 溶解带的目标色（顶点流）
   mad o0.xyz, r1.xyzx, r0.wwww, r0.xyzx           ; ← lerp(base, base·顶点流色, d)
   mul o0.w, r0.w, r1.y                            ; r1.y = (v2.z ≥ mask 通道)
```
溶解带标志 `d` 与颜色常量①（idx=6 的原文）：
```
   add r0.x, -cb1[3].y, cb1[3].x                   ; 上界 − 下界
   ge r0.y, cb1[3].x, r1.x
   and r0.y, r0.y, l(0x3f800000)
   movc r0.x, r0.x, l(-1.000000), l(-0.000000)
   add r0.x, r0.x, r0.y
   max r0.x, r0.x, l(0.000000)                     ; ← d ∈ {0,1}，二值带
   log r0.yzw, |cb1[4].xxyz|  …  movc r0.yzw, r1.xxyz, r2.xxyz, r0.yyzw   ; S(cb1[4].xyz)
   mul r0.xyz, r0.xxxx, r0.yzwy                    ; d × S(_Add_Color)
   mad o0.xyz, r1.xyzx, v3.xyzx, r0.xyzx           ; base×vcol + d×S(_Add_Color)
```

### 四 · 寄存器 ↔ 属性（**推断**，但证据两条）

| 寄存器 | 属性 | 证据 |
|---|---|---|
| `cb1[2].xyzw` | `Vector4_d8c31f3b…` = **`_Disolve_ST`** | 它是**唯一**被当 uv 的 `mad v1.xy*cb1[2].xy+cb1[2].zw` 用的寄存器，而材质 `m_TexEnvs` 里的贴图只有 `_MainTex` / `_Disolve`；`_MainTex` 已被 t0 用掉 |
| `cb1[3].x` | `Vector1_4e2dbe…`（Range，def **0.43**） | 阈值上界（`ge cb1[3].x, mask`） |
| `cb1[3].y` | `Vector1_2780b56…`（Float，def **0.04**） | 阈值下界；`cb1[3].x − cb1[3].y` 就是带宽 |
| `cb1[4].xyz` | **`_Add_Color`**（Color，def **(0,0,0,0)**） | 只出现在「溶解带加法项」里；**18 个材质里 16 个 `_Add_Color` 就是 (0,0,0,0)**（另 2 个是低值灰 `(0.711,0.484,0.226,0.357)` / `(0.560,0.511,0.407,0.553)`）⇒ 这一项多数材质恒为 0，语义上就该是那个「附加色」 |
| `cb1[5].xyz` | **`_Color`**（Color，def **(3.223,0,0,0)**） | 与 vcol 相乘的**主色**位；18 个材质里 **16 个 `_Color` 非白**（`(2.000,0.151,0,1)` · `(0.932,5.340,2.686,1)` · `(10.681,1.901,9.331,1)` …，另 2 个是白）—— 只有放在主色位，粒子才可能是那个颜色 |
| `cb1[5].w` | `_Color.a` | 直接加在 `tex.a` 上 |

🔴 **`_Color` 在这 18 个材质里 16 个非白**（另 2 个是白） ⇒ **这就是「删掉 `_Color` 会变暗」的天然候选**：
原版 `o0 = tex.a·(S(_Color)+tex)·vcol`，`S(_Color)` 通常**比 `tex` 本身还大**（`S((10.68,1.90,9.33))≈(2.78,1.31,2.63)`）
⇒ 去掉颜色常量后从「S(_Color)+tex」掉到「tex」，**必然显著变暗**，且颜色从彩色变白。
**但补回来的做法不能是 `×_Color`**（原版是「加 S(_Color) 再乘 tex.a·vcol」）。

### 五 · 对我们的行动项

- 现在这个 shader 落在 `WarpforgeVFX/Particles/Extra Color*`（近似）。**要还原就得单建一个**：
  需要 `_Color`、`_Add_Color`、`_Disolve`+`_Disolve_ST`、两个溶解阈值（0.43/0.04）、`_DISSOLVE_CHANNEL`、
  以及 `S()` 那条曲线。**成本：中**（公式直白，但参数有 5 个）。
- 最低成本的**单变量 A/B**：只在自建 shader 里把 `_Color` 从「乘」改成「`S(_Color) + tex.rgb`」，
  拿 `Exile Glaive Dissolve` / `Generic Particle Dissolve For Sprites EC Pink` 这两个材质比一遍。
- 用它的 29 条效果里，`Atk_ExileGlaiveThrow`、`BacklashEffect`、`Buff_SW_Rune_*` 都在名单上
  （出处 `普查产出_0913/效果_shader_对账.md:241,261,277,399+`）。

---

## §2 · `Everguild/FX/Alpha Mask One Layer`

**判定 = B**（**一份**颜色常量乘子 —— 但它是**标量灰度**、名字是 `_Color_Mutliplier`）。

### 一 · 资源表

| 项 | 值 |
|---|---|
| DXBC 总段数 | **12**；ps **7** 段 / **6** 种指令流（同样 3 个包副本字节相同，sha1 `96b4fa76f2`） |
| pass 数 | 5 |
| 常量缓冲 | `CB0[5]` + `CB1[5]` |
| 贴图 / 采样器 | `t0,t1` / `s0,s1`（`_MainTex` = t0 · `_SecondaryTex` = t1） |
| temps | 2 |
| **只有 2 段 ps 真的输出颜色**（idx=1、2）；另外 5 段是 shadow caster / depth / normal / 空 | |

### 二 · 颜色算式（两种颜色变体，RGB 路径**完全相同**）

```
o0.rgb = t0.Sample(v2.xy).rgb  ·  t1.Sample(v2.zw).rgb  ·  cb1[4].y  ·  v1.rgb
                                                            ↑            ↑
                                               _Color_Mutliplier（推）  顶点色 v1
```
**alpha 分两支**：
```
变体 A（idx=1）：o0.w = 1
变体 B（idx=2）：o0.w = saturate( pow( t0.a·t1.a·cb1[4].z , cb1[4].x ) · v1.w )
                                  ↑ _Alpha_Modifier_Multiply   ↑ _Alpha_Modifier（指数）
```

### 三 · DXBC 原文关键行（两段都抄全，因为很短）

```
----- [1] ps  608 字节 -----            ----- [2] ps  688 字节 -----
   dcl_resource_texture2d … t0,t1         dcl_resource_texture2d … t0,t1
   dcl_input_ps linear v1.xyz             dcl_input_ps linear v1.xyzw
   dcl_input_ps linear v2.xyzw            dcl_input_ps linear v2.xyzw
   sample_b r0.xyzw, v2.xyxx, t0…s0,cb0[4].x   sample_b r0.xyzw, v2.xyxx, t0…s0,cb0[4].x
   sample_b r1.xyzw, v2.zwzz, t1…s1,cb0[4].x   sample_b r1.xyzw, v2.zwzz, t1…s1,cb0[4].x
   mul r0.xyz, r0.xyzx, r1.xyzx                mul r0.xyzw, r0.xyzw, r1.xyzw
   mul r0.xyz, r0.xyzx, cb1[4].yyyy            mul r0.xyzw, r0.xyzw, cb1[4].yyyz   ; (y,y,y,z)
   mul o0.xyz, r0.xyzx, v1.xyzx                log r0.w, r0.w
   mov o0.w, l(1.000000)                       mul o0.xyz, r0.xyzx, v1.xyzx
                                               mul r0.x, r0.w, cb1[4].x
                                               exp r0.x, r0.x
                                               mul_sat o0.w, r0.x, v1.w
```
🔴 **注意 `cb1[4]` 被当 4 个独立标量用**（`.x` 指数、`.y` 颜色乘子、`.z` alpha 乘子）——
**不是**一个 float4 颜色。这是「Unity 把多个 Float 属性塞进同一个寄存器的空位」的典型样子。

### 四 · 寄存器 ↔ 属性（**推断**）

属性表原文（`m_ParsedForm.m_PropInfo`，含类型与默认值）：
`_MainTex`(TexEnv) · `_PrimaryTex_Scale_XY_Speed_ZW`(Vector) · `_SecondaryTex`(TexEnv) ·
`_SecondaryTex_Scale_XY_Speed_ZW`(Vector) · **`_Color_Mutliplier`(Float, def 1)** ·
**`_Alpha_Modifier`(Range 0.01–2, def 1)** · **`_Alpha_Modifier_Multiply`(Float, def 2)** · 后接 URP 那一串 `_Blend/_SrcBlend/…`

| 寄存器 | 属性（推） | 证据 |
|---|---|---|
| `cb1[4].x` | `_Alpha_Modifier` | 被当**指数**（`log → ×x → exp`），且它是唯一的 **Range[0.01,2]**、def 1（⇒ 默认恒等） |
| `cb1[4].y` | **`_Color_Mutliplier`** | 被当**颜色标量乘子**（`.yyyy` 广播），语义与名字逐字对得上 |
| `cb1[4].z` | `_Alpha_Modifier_Multiply` | 被当 **alpha 乘子**，def 2（⇒ 默认把 alpha 放大一倍） |

⚠️ 这个顺序**不是**属性表的声明顺序（声明是 `_Color_Mutliplier` 在前）—— 说明标量是被编译器**按用途打包**的，
所以**只能按用法定名**。这三条是「用法 + 类型 + 默认值」三重吻合，可信度高，但**字节码证不出名字**。

### 五 · 对我们的行动项

- 它是**唯一一个「形状就是 `tex × tex × 常量 × vcol`」的 shader** —— 也就是最接近 B 的那个。
  但**常量是标量灰度**（不是 RGB），所以「给它一个乘 `_Color` 的替代」只在**把 `_Color` 当灰度用**时才等价。
- 🔴 **先找材质再动手**：它在原版里**有 13 个材质**（出处 `资料/普查产出_0917/shader属性表_块1.md:202`：
  `Hexagon_cylinder` / `Hexagon_companion` / `Glow Layered Soft` … 那份表只列了前 3 个，写着「材质（13）」），
  **但这 13 个都不在 `battleprefabs_vfxandmisc_assets_all.bundle` 里** —— 我逐材质扫过那个包，用它的是 **0** 个
  ⇒ **要拿 `_Color_Mutliplier` 的实测值得去别的包找**（本文件没去找，别把「0」读成「没人用」）。
- 它的 5 个 ps 里只有 2 个出颜色，**且 RGB 路径两支完全一样** ⇒ 建 shader 时**不用管 pass 差异**。
- ⚠️ **同族还有一个没接的**：`Everguild/FX/Alpha Mask One Layer  Color Ramp`
  —— 🔴 **名字里是「两个空格」**（已用正则从属性表原文取名字核过：不重名的那个长 **33**、带 `Color Ramp` 的长 **45**，
  `33 + 2 + 10 = 45`，所以是**两个**空格，**别手打成空格后接不上**）。属性表多两个 Color（`_Main_Color` def (0,0,0,0)、
  `_Secondary_Color` def (1,1,1,0)），属性 27 条、**2 个材质**（出处同上一行 `:203-204`）。它**既不在**
  `WarpforgeShaderMap.Replacements` **也不在** `EffectExporter.ShaderMap`（我 grep 过两个文件）⇒ 现在会掉到占位
  `URP/Particles/Unlit`。**不在本次这 5 个的范围内**，但既然同族、只差两个颜色，值得并到同一批处理。

---

## §3 · `Everguild/FX/Alpha Masks Two Layer`

**判定 = C**（`_Blend_Color` 过 sRGB 后做 **Overlay 混合** + 两张噪声图 + alpha 幂曲线）。

### 一 · 资源表

| 项 | 值 |
|---|---|
| DXBC 总段数 | **92**（三个包副本字节相同，sha1 `cccc299c54`） |
| ps 段 | **47** / **15** 种指令流；其中 **10 组出颜色**（idx 9–18），另 5 组是 depth/normal/空 |
| pass 数 | 5 |
| 常量缓冲 | `CB0[5]` + **`CB1[8]`**（本块里 cb1 最大的一个） |
| 贴图 | 组1（idx=9）只 1 张（`t0`）；**组2–10 有 3 张**（`t0` = `_MainTex`，`t1`/`t2` = `_NoiseTex1`/`_NoiseTex2`）· 采样器 s0/s1/s2 |
| temps | 5 |

**变体差异（已逐行 diff 验证）**：idx=10/11/12/13 的**完整指令流只差一个字**——
`mul r0.x, r0.x, r1.x` → `r0.y` → `r0.z` → `r0.w`，也就是**取噪声图的哪个通道**
（`_NOISE1CHANNEL` / `_NOISE2CHANNEL` 那两个关键字）。**颜色算式逐行相同**。

### 二 · 颜色算式（idx=10 为「完整版」，逐行推出来的）

```
C    = S(cb1[6].xyz)                    // cb1[6] = _Blend_Color（Color，def (0,0,0,0)）
k    = cb1[5].z                         // _Blend_Color_Opacity（Range 0–1，def 1）
tex  = t0.Sample(v1.xy)                 // _MainTex，UV 由 VS 给
q    = tex.rgb * vcol.rgb               // vcol = v2
q   *= k
o0.rgb = lerp( C , Overlay(C, q) , cb1[7].x )        // cb1[7].x = _Color_Multiplier（Float def 1）
        // Overlay 展开：q ≤ 0.5 ? 2·C·q : 1 − 2(1−C)(1−q)   ← 逐通道比较，不是整体
o0.a = min( pow( Noise1[ch1] · Noise2[ch2] · (tex.a·vcol.a) · A2 , A1 ) , 1 ) · (tex.a·vcol.a)
        // A1 = cb1[5].x = _AlphaMultiplier（Range 0.01–2，**def 0.5**）
        // A2 = cb1[5].y = _Alpha_Multiplier_2（Float，def 1）
        // Noise1/2 的 UV = v3.zw / v3.xy，(tex.a·vcol.a) **出现两次**
```
**组1（idx=9，单贴图那支）** 没有噪声也没有 alpha：`o0.w = 1`，RGB 算式与上面同（只是 `q` 的 alpha 分量不参与）。

### 三 · DXBC 原文关键行（idx=10）

```
   log r0.xyz, |cb1[6].xyzx|
   mul r0.xyz, r0.xyzx, l(0.416667, 0.416667, 0.416667, 0.000000)
   exp r0.xyz, r0.xyzx
   mad r0.xyz, r0.xyzx, l(1.055000, 1.055000, 1.055000, 0.000000), l(-0.055000, -0.055000, -0.055000, 0.000000)
   movc r0.xyz, r1.xyzx, r2.xyzx, r0.xyzx          ; ← r0 = S(cb1[6].xyz) = S(_Blend_Color)
   add r1.xyz, -r0.xyzx, l(1.000000, 1.000000, 1.000000, 0.000000)
   add r1.xyz, r1.xyzx, r1.xyzx                    ; r1 = 2(1−C)
   sample_b r2.xyzw, v1.xyxx, t0.xyzw, s0, cb0[4].x
   mul r2.xyzw, r2.xyzw, v2.xyzw                   ; × 顶点色
   mad r3.xyz, -cb1[5].zzzz, r2.xyzx, l(1,1,1,0)
   mul r2.xyz, r2.xyzx, cb1[5].zzzz                ; q = tex·vcol·_Blend_Color_Opacity
   ge  r3.xyz, l(0.500000, 0.500000, 0.500000, 0), r2.xyzx    ; ← Overlay 的 0.5 分支
   mul r2.xyz, r0.xyzx, r2.xyzx
   add r2.xyz, r2.xyzx, r2.xyzx                    ; 2·C·q
   mad r1.xyz, r2.xyzx, r3.xyzx, r1.xyzx           ; 另一半 1−2(1−C)(1−q)
   add r1.xyz, -r0.xyzx, r1.xyzx
   mad o0.xyz, cb1[7].xxxx, r1.xyzx, r0.xyzx       ; ← lerp(C, Overlay, cb1[7].x)
   sample_b r0.xyzw, v3.xyxx, t2.xyzw, s2, cb0[4].x
   sample_b r1.xyzw, v3.zwzz, t1.xyzw, s1, cb0[4].x
   mul r0.x, r0.x, r1.x                            ; 噪声1[ch] × 噪声2[ch]
   mul r0.x, r0.x, r2.w                            ; × (tex.a·vcol.a)
   mul r0.x, r0.x, cb1[5].y                        ; × _Alpha_Multiplier_2
   log r0.x, r0.x
   mul r0.x, r0.x, cb1[5].x
   exp r0.x, r0.x                                  ; ^ _AlphaMultiplier
   min r0.x, r0.x, l(1.000000)
   mul o0.w, r0.x, r2.w                            ; × (tex.a·vcol.a) 再来一次
```

### 四 · 寄存器 ↔ 属性（**推断**，这一条证据最硬）

`cb1[5]` 的三个分量用法 `.x=指数(Range)` / `.y=乘子(Float)` / `.z=混合不透明度(0–1 Range)`
与属性表声明顺序 **`_AlphaMultiplier`(Range 0.01–2, def 0.5) · `_Alpha_Multiplier_2`(Float, def 1) ·
`_Blend_Color_Opacity`(Range 0–1, def 1)** **逐项吻合** ⇒ 这三个是**声明顺序打包**，可信。
`cb1[6]` = `_Blend_Color`（Color，def (0,0,0,0)）——它是 `CB1[8]` 里唯一的 Color 且被 `S()` 处理。
`cb1[7].x` = `_Color_Multiplier`（Float，def 1）—— 剩下唯一没着落的标量，且语义（整体缩放）与
`lerp` 的权重位吻合。

### 五 · 🔴 一个反直觉的实测：**这个 shader 的材质里 `_Blend_Color` 全是黑的**

出处 `资料/普查产出_0917/替代shader丢失的源属性_0917.md:145-150`：
```
| WarpforgeVFX/Particles/Extra Color | Alpha Masks Two Layer Variant Blood Twirl | 3 |
     _Blend_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _NoiseTex1=Noise Combined / _NoiseTex2=Noise Combined |
```
`C = S(0) = 0` 代进去，上面那一长串**塌成**：
```
o0.rgb = K · ( q > 0.5 ? 2·k·q − 1 : 0 )     // K = _Color_Multiplier
```
⇒ 实战里它是一个**高通/阈值发光**效果（**只留亮过一半的像素**），颜色**全部来自贴图 × 顶点色**。
**推论（推，但可直接验）**：拿我们的 `Extra Color`（`tex×vcol×_Color`）替它会**多出一大片不该有的
中间调**（症状 = 偏亮/发糊，不是偏暗）⇒ 它不太可能是「7 条翻暗」的元凶，但**是「不像」的大户**。
（口径：逐材质明细里列了 **6 个**，`_Blend_Color` **全是 (0,0,0,0)**；属性频次表记 `_Blend_Color×9`，
说明该 shader 一共 9 个材质，**另 3 个这次没有逐条核对**。）

### 六 · 对我们的行动项

- 单建成本**中偏高**（Overlay + 两张噪声 + alpha 幂曲线 + 三个标量），但**只有 9 条效果**用它 ⇒ 优先级低。
- 若只想先对齐「亮暗」：把自建 shader 改成 `max(0, 2·k·tex·vcol − 1)` 就能拿到一大半形状。

---

## §4 · `Everguild/FX/Multi Ray`

**判定 = C**（**四份**颜色常量 + 单贴图四通道当遮罩 + 值域重映射）。

### 一 · 资源表

| 项 | 值 |
|---|---|
| DXBC 总段数 | **22**（三副本同字节，sha1 `9866fe261f`）；ps **12** / **8** 种指令流；真的出颜色的只有 **3 段**（idx=2,3,4） |
| pass 数 | 5 |
| 常量缓冲 | `CB0[5]` + `CB1[10]`（**带 `_USEBORDERFADE` 的变体是 `CB1[13]`**） |
| 贴图 | **只有 1 张 `t0` = `_MainTex`**，但**采 4 次**（4 组不同 UV）· 采样器 s0 |
| temps | 5 |

### 二 · 颜色算式（idx=3 = 最完整）

```
C1 = S(cb1[8]) ; C2 = S(cb1[5]) ; C3 = S(cb1[7]) ; C4 = S(cb1[6])      // 4 份颜色常量！
s  = C1.rgb · t0.Sample(v3.xy)[.x]        // ← 只取 **R** 通道
   + C2.rgb · t0.Sample(v3.zw)[.y]        // ← 只取 **G** 通道
   + C3.rgb · t0.Sample(v4.xy)[.z]        // ← 只取 **B** 通道
   + C4.rgb · t0.Sample(v4.zw)[.w]        // ← 只取 **A** 通道
a  = saturate( Σ_N C_N.a · 对应通道 ) · vcol.a          // 颜色的 **alpha 分量 = 每条射线的权重**
s *= vcol.rgb
R  = cb1[9]                                // = _Remap_Color_Min_XY_Max_ZW
o0.rgb = (s − R.x) · (R.w − R.z) / (R.y − R.x) + R.z    // 标准值域重映射
```
**`R`（= `cb1[9]`）的语义**：代码算的是 `(s − R.x)·(R.w − R.z)/(R.y − R.x) + R.z` —— 这就是
**教科书的值域重映射**，且它把 **`(R.x, R.y)` 当一对（源区间的 min/max）**、**`(R.z, R.w)` 当另一对（目标区间）**。
14 个用它的材质实测取值分布（本次直读 `battleprefabs_vfxandmisc_assets_all.bundle`）：
`(0,10,0,10)`×9 · `(0,10,0,5)`×3 · `(0,5,0,10)`×1 · `(0,1,0,1)`×1。
⚠️ **这 4 组里 `.x` 和 `.z` 恒为 0** ⇒ 加减项都消失，公式塌成 **`s · R.w / R.y`**，也就是一个**恒定增益**：
恒等（10/10，9 个）· **减半**（5/10，3 个）· **×2**（10/5，1 个）· 恒等（1/1，1 个）。
⇒ 实战里它**不是**一条曲线，只是「亮几倍」；`DCannon_Trail_MultiRay` 那 3 个是**真的暗一半**的材质。
（属性名 `_Remap_Color_Min_XY_Max_ZW` 里 `.xy`/`.zw` 哪一对叫 Min 哪一对叫 Max 从字节码分不出，
但因为 `.x=.z=0`，**两种命名读法在现有数据上给出同一个结果**，不影响实现。）

**变体差异（3 种）**：
| idx | o0.w | 差别 |
|---|---|---|
| 2 | `1` | 不透明，无边框淡出 |
| 3 | `saturate(Σ C_N.a·ch)·vcol.a` | 有 alpha |
| 4 | 上面的 `·borderFade(v1.xy)·cb1[12].x` | 多 `_USEBORDERFADE`（`cb1[11]` = `_Border_Fade_XminXmax_YminYmax`(def 0,1,0.45,1.1)、`cb1[12].x` = `_BorderFadeMultiplier`(def 1)） |

### 三 · DXBC 原文关键行（idx=3）

```
   log r0.xyz, |cb1[8].xyzx| … movc r0.xyz, r1.xyzx, r2.xyzx, r0.xyzx    ; C1 = S(cb1[8])
   sample_b r1.xyzw, v3.xyxx, t0.xyzw, s0, cb0[4].x
   mul r0.xyz, r0.xyzx, r1.xxxx                     ; C1 · tex(v3.xy).x
   mul r0.w, r1.x, cb1[8].w                         ; + C1.a · tex.x  → alpha 累加
   …（C2 同形：sample v3.zw，取 .y，× cb1[5].w）…
   …（C3 同形：sample v4.xy，取 .z，× cb1[6].w）…
   …（C4 同形：sample v4.zw，取 .w，× cb1[7].w）…
   add r0.xyzw, r0.wxyz, r1.wxyz                    ; ← 4 路 rgb 与 4 路 alpha 分别求和
   mad r0.yzw, r0.yyzw, v2.xxyz, -cb1[9].xxxx       ; s·vcol − R.x
   mov_sat r0.x, r0.x
   mul o0.w, r0.x, v2.w                             ; alpha = saturate(ΣC.a·ch) · vcol.a
   add r1.xy, -cb1[9].zxzz, cb1[9].wyww             ; r1.x = R.w − R.z ; r1.y = R.y − R.x
   mul r0.xyz, r0.yzwy, r1.xxxx
   div r0.xyz, r0.xyzx, r1.yyyy
   add o0.xyz, r0.xyzx, cb1[9].zzzz                 ; ← (s−R.x)·(R.w−R.z)/(R.y−R.x) + R.z
```

### 四 · 寄存器 ↔ 属性（**推断**）

| 寄存器 | 属性 | 证据 |
|---|---|---|
| `cb1[5..8]` | `_Color1.._Color4` | 唯一 4 个 Color 属性；材质值全是 HDR（`Laser_Stream_MultiRay Mining`：`_Color1=(26.254,13.667,2.601,1.000)`、`_Color2=(123.564,62.275,5.246,0.808)` …），且 **`.a` 恰好被当权重用**（材质里 `_Color2.a=0.808`、`_Color3.a=0.992`）⇒ 与「alpha 当权重」的用法自洽 |
| `cb1[9]` | `_Remap_Color_Min_XY_Max_ZW` | 见上：14 个材质的实测值代进去就是 `s·R.w/R.y`（9 个恒等 · 3 个减半 · 1 个×2） |
| `cb1[11]` / `cb1[12].x` | `_Border_Fade_XminXmax_YminYmax` / `_BorderFadeMultiplier` | 只出现在 `_USEBORDERFADE` 那支；默认值 (0,1,0.45,1.1)/1 与材质逐项一致 |
| `cb1[0..3]` | `_Scale_1..4_XY_Speed_ZW` | ps 里一次都没出现（全在 VS 里做 UV 滚动）；材质里 4 个都在 |

### 五 · 对我们的行动项

- **33 条效果**用它（本块里最多）⇒ 若要做，**优先它**。但成本也最高：4 色 + 4 通道遮罩 + 重映射 + 边框淡出。
- 🔴 **注意一个「我们一定错」的地方**：原版把**一张贴图的 R/G/B/A 四个通道当四张遮罩**用，
  所以 `_MainTex` 本身多半是**灰度/通道分离图**。我们用 `Extra Color` 替它 ⇒ 等于把整张贴图当颜色乘上去，
  **形状完全不同**（会是一片色块，不是四条射线）。

---

## §5 · `Everguild/FX/Particle Shine Custom Vertex Streams`

**判定 = C**（一份颜色常量，但走**加法发光**，不是整体乘子；两贴图 + smoothstep + **顶点流分支**）。

### 一 · 资源表

| 项 | 值 |
|---|---|
| DXBC 总段数 | **6**（三副本同字节，sha1 `161f6e4d48`）；**ps 只有 2 段**，2 种指令流，这就是全部 |
| pass 数 | 2 |
| 常量缓冲 | `CB0[5]` + `CB1[6]` |
| 贴图 / 采样器 | `t0,t1` / `s0,s1`（`_MainTex` = t0；`t1` = `Texture2D_0bfdc50d…`，材质上叫 **`Shine trail`**） |
| temps | 5 |
| 关键字 | `BOOLEAN_7733627BA3A1447E9D9F28A82CD0E4D4`（def 1）= 「用顶点流」开关 |

### 二 · 颜色算式（两段的**唯一差别**在「强度/偏移取自常量还是顶点流」）

```
uv   = rot(v1.xy − 0.5, cb1[4].x) + 0.5          // 绕中心旋转 cb1[4].x（sincos 那段）
uv   = uv · cb1[3].xy + cb1[3].zw · k            // k = 变体A: cb1[4].y   /  变体B: v1.z（顶点流）
sh   = t1.Sample(uv)                             // Shine trail
t    = saturate( (sh.rgb − W.x) / (W.y − W.x) )  // W = cb1[5] = Shine_Width（def (0,1,0,0)）
t    = t·t·(3 − 2t)                              // smoothstep
glow = t · cb1[2].xyz                            // ← 颜色常量（唯一的 Color 属性）
o0.xyzw = ( t0.Sample(v1.xy)  +  glow · s ) · vcol     // s = 变体A: cb1[4].z / 变体B: v1.w；vcol = v2.xyzw
```
- **顶点色参与**：✅ 整条 `o0`（rgb **和** a）都乘 `v2.xyzw`。
- **alpha**：`o0.a = tex.a · vcol.a`（发光项不改 alpha —— `mad` 只动 `.xyz`）。
- **t0 的 alpha 是硬门槛**：`eq r1.x, r0.w, 0 ; discard_nz r1.x` ⇒ **alpha 恰好为 0 的像素直接丢弃**。
- **两段的差别（就该 shader 名字里的 "Custom Vertex Streams"）**：
  | | UV 偏移的缩放 | 发光强度 |
  |---|---|---|
  | idx=4 | `cb1[4].y`（常量） | `cb1[4].z`（常量） |
  | idx=5 | `v1.z`（顶点流 TEXCOORD0.z） | `v1.w`（顶点流 TEXCOORD0.w） |
  ⇒ idx=4 的 ps 只声明 `dcl_input_ps linear v1.xy`（没有 zw），idx=5 是 `v1.xyzw`。

### 三 · DXBC 原文关键行（idx=4，另一段只差标出的两行）

```
   sample_b r0.xyzw, v1.xyxx, t0.xyzw, s0, cb0[4].x
   eq r1.x, r0.w, l(0.000000)
   discard_nz r1.x                                  ; ← 主贴图 alpha=0 直接丢像素
   add r1.xy, v1.xyxx, l(-0.500000, -0.500000, …)
   sincos r2.x, r3.x, cb1[4].x                      ; ← UV 旋转角
   dp2 r3.y, r1.xyxx, r4.xyxx
   dp2 r3.x, r1.xyxx, r4.yzyy
   add r1.xy, r3.xyxx, l(0.500000, 0.500000, …)
   mul r1.zw, cb1[3].zzzw, cb1[4].yyyy              ; 变体B 这里是： mul r1.zw, v1.zzzz, cb1[3].zzzw
   mad r1.xy, r1.xyxx, cb1[3].xyxx, r1.zwzz
   sample_b r1.xyzw, r1.xyxx, t1.xyzw, s1, cb0[4].x
   add r1.w, -cb1[5].x, cb1[5].y
   add r1.xyz, r1.xyzx, -cb1[5].xxxx                ; (sh.rgb − Shine_Width.x)
   div r1.w, l(1.000000, …), r1.w                   ; 1 / (W.y − W.x)
   mul_sat r1.xyz, r1.wwww, r1.xyzx
   mad r2.xyz, r1.xyzx, l(-2.000000, …), l(3.000000, …)
   mul r1.xyz, r1.xyzx, r1.xyzx
   mul r1.xyz, r1.xyzx, r2.xyzx                     ; ← smoothstep
   mul r1.xyz, r1.xyzx, cb1[2].xyzx                 ; ← × 颜色常量
   mad r0.xyz, r1.xyzx, cb1[4].zzzz, r0.xyzx        ; 变体B 这里是： mad r0.xyz, r1.xyzx, v1.wwww, r0.xyzx
   mul o0.xyzw, r0.xyzw, v2.xyzw                    ; ← × 顶点色（rgb 与 a 都乘）
```

### 四 · 🔴 **材质上的 `_Color` 是死的**

这个 shader 的属性表里**没有 `_Color`**，只有 `Color_a142b0353b4945a49e1990789c04ea35`（Color，def (1,1,1,0)）
—— 材质里它是**每张卡不同**的真值（`Shuriken`=(0.192,0.616,0.557,1) · `Flank`=(0.585,0.338,0.268,0)
· `Sentry`=(1.000,0.792,0.665,0) …，**11 个材质逐个核过，11/11 都带这个真颜色属性**），
而**同一个材质里的 `_Color` 也逐个核过：11/11 全是 `(1,1,1,1)`**。
⇒ **`_Color` 是 URP Lit 的残留条目**（`m_SavedProperties` 会保留 shader 没有的历史属性）。
**凡是我们按 `_Color` 做乘法的自建 shader，落到这些材质上都是 ×1 = 无变化** ⇒ 这 11 个材质不是「7 条翻暗」的份。

### 五 · 对我们的行动项

- 成本**中**：两贴图 + 一次旋转 UV + smoothstep + 一个颜色 + 两种强度来源。
  单建它还能顺带把「`_MainTex` 丢掉」（见 `普查产出_0917/替代shader丢失的源属性_0917.md` 汇总里
  `Texture2D_0bfdc50d…×13`、`Color_a142b…×13`）一起修。
- **26 条效果**用它；`BloodThirstEffect_Gain` 这类在名单上（`效果_shader_对账.md:326`）。

---

## §6 · 横向对照（5 个一起看能看出的三件事）

1. **sRGB 编码 `S()` 出现在 3 个 shader 里**（`Particle Dissolve Mask`、`Alpha Masks Two Layer`、`Multi Ray`），
   **不出现**在另外 2 个（`Alpha Mask One Layer`、`Particle Shine`）里。
   ⇒ 凡是有 `S()` 的，**颜色常量都不是线性乘进去的** ⇒ **任何 `×_Color` 的近似都不成立**（这三个全是 C）。
2. **本块里唯一「形状就是 `tex × 常量 × vcol`」的是 `Alpha Mask One Layer`**，而它的常量是**标量灰度**。
   ⇒ 判 **B 的那一个恰好不是 `_Color` 语义** —— 也就是说，**在我们删掉 `_Color` 乘法这件事上，这 5 个 shader
   没有一个是「该乘 `_Color`」的**；「7 条翻暗」的元凶只可能是 `Particle Dissolve Mask`（加法式用 `_Color`）。
3. **`vcol` 编号每个 shader 都不同**，别照抄：`Alpha Mask One Layer`/`Particle Shine` = `v1`；
   `Alpha Masks Two Layer` = `v2`；`Particle Dissolve Mask`/`Multi Ray` = `v3`/`v2`。

## §7 · 这份文件**不能**当成结论用的地方（免得下个会话直接引用）

1. **属性名 ↔ cb 寄存器**全是推断（**RDEF 缺失**）。三条证据链：属性**类型/默认值**（`m_PropInfo`）·
   **材质上的实际值** · **用法语义**。`§3` 里 `cb1[5]` 那三条吻合度最高；`§2` 的 `.x/.y/.z` 是「按用途定名」。
2. **我只扫了 `battleprefabs_vfxandmisc_assets_all.bundle` 一个包里的材质**（那 5 个 shader 的**材质实际值**
   全部来自它）。所以：`Alpha Mask One Layer` / `Alpha Masks Two Layer` 在**那个包里**材质数 = 0，
   但它们在原版里分别有 **13** / **9** 个材质（出处 `普查产出_0917/shader属性表_块1.md:202` 等），
   **只是落在别的包里** —— 这份文件**没有**去核那两个 shader 的材质实测值。
3. **没跑过实况**：本文件全部来自**静态字节码 + 材质序列化**。原版效果跑起来的实际观感没有比对过。
4. **变体数**按**单份 blob** 统计；同一个 shader 在 `battleprefabs_vfxandmisc_assets_all.bundle` /
   `wf_shaders_extra.bundle` / `wf_shaders.bundle` / `shaders_assets_all.bundle` 里有多份**字节完全相同**的副本
   （sha1 已逐一核对），所以「ps 段数」不会因取哪一份而变。
