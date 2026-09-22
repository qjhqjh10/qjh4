# 战场 7 族原版 shader —— 逐族算式（**这条线做 shader 那一族的判据**）

> 2026-09-21 建。**为什么单独一份**：`资料/战场13场_逐场对账_0920.md` §二之二 只有「哪一族丢了多少个材质」，
> 而**怎么复刻**要的是「原版那段算式到底算什么」。这份就是它。反过来，正本那边不抄第二份。
>
> **怎么复现**（都是纯 Python，不碰 Unity）：
> ```bash
> # 属性表 + pass 状态 + 采样纹理 + 「槽位 → 名字」表（nameIndices）
> PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/dump_shader.py "<名字子串>"
> # 反汇编编译字节码（HLSL 源码被剥了，指令流在）
> PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/disasm_dxbc.py "<名字子串>" [--stage vs|ps] [--limit N]
> ```
> 🔴 **`disasm_dxbc.py` 的 `BUNDLES` 2026-09-21 补了 `battlesharedresources_assets_all.bundle`** ——
> 补之前查 `Pulsating Mesh` / `Tyranid Tentacle` 会得到**「命中 0 个」的假结论**（它们只在那一个包里）。
> **报「没有」之前先把「搜过哪几个包」打出来。**

## 〇、先说三条前提（解读全靠它们）

1. **原版 DXBC 被剥了 RDEF** ⇒ 指令里**没有 `cbuffer{...}` 块、没有纹理名**。
   「`cbN[i]` ↔ 属性名」**要靠 `dump_shader.py` 印的 `nameIndices`（槽位→名字）来对**
   —— 我拿 `Floor Planar Reflections` 验过一次：它把 `_BaseMap_ST`→reg3 · `_ReflectionStrength`→reg6 ·
   `_ReflectionColorMultiplier`→reg7 全列了出来，**别只靠取值猜**。
2. **段 ↔ 关键字的对应在字节码里读不出来**（关键字表不在 Shader 对象里）⇒ 只能说「这几段之间哪些指令不同」。
3. **时间项统一是 `cb0[19].x`（= `_TimeParameters` 的 t）**，**不是 `_SinTime`**（判据：`Vortex` 的 `m_NameIndices` 里有 `_TimeParameters`）。
   🔴 另：**`wf_shaders.bundle` 与 `shaders_assets_all.bundle` 是同一份内容**（pathID / blob 长度 / 段数全同）—— 别当两个包数。

## 一、总表

| 族 | 材质数 | 在哪个包 | 丢了什么（现状） | keyword |
|---|---|---|---|---|
| `Everguild/Unlit Wind` | 56 | `shaders_assets_all` | 草/帘幕/挂布**完全静止** | `_APPLYAMBIENTCOLOR` |
| `Everguild/FX/Tyranids/Pulsating Mesh` | 23 | 🔴 `battlesharedresources_assets_all` | 利维坦的肉**完全静止** | `_APPLYAMBIENTCOLOR` |
| `Everguild/FX/Unlit UV scroll` | 13 | `shaders_assets_all` | 地面光带贴图**完全静止**（用户圈的「平绿板」`Toxic Pool Glow`） | `_SOFT` · `_PREMULTIPLY` |
| `Everguild/Misc/Unlit shadows receiver` | 12 | `shaders_assets_all` | 道具/建筑**永远亮着**，与光照阴影脱节 | `_MAIN_LIGHT_SHADOWS*` · `_SHADOWS_SOFT` |
| `Everguild/FX/Tyranids/Tyranid Tentacle` | 9 | 🔴 `battlesharedresources_assets_all` | 触手是**冻住的棍子** | `_MOVEMENTXAXIS`（实测材质全开）· `_APPLYAMBIENTCOLOR` |
| `Everguild/FX/Floor Planar Reflections Grainny` | 3 | `shaders_assets_all` | 地板**不反射**（`_ReflectionStrength` 0.27~0.39 的镜面感全丢） | `_MAIN_LIGHT_SHADOWS*` · `_SHADOWS_SOFT` |
| `Everguild/FX/Vortex` | 2 | `shaders_assets_all` | 漩涡是**静态贴图** | 无 |

## 二、逐族算式

### 1 · `Unlit Wind`（56）—— 🔴 **不是 sin 摆动，是内联的程序化梯度噪声**
- **VS**：全 VS **0 个 `sincos`、0 次贴图采样**；常量 `289 / (1/289) / 34 / (1/41)` = Gustavson 那套 `mod289`+`permute` 的 Perlin。
  `uv = ((O2W·v).xz + t * P.z) * P.w` → `d = Perlin2D(uv)` → `disp = v.color.r * d * P.y` → `pos.xyz += (disp, disp, disp)`
- 🔴 **位移是同一个标量加到 x/y/z 三个分量**（`mad r0.xyz, v4.xxxx, r0.xxxx, v0.xyzx`）——
  **不是沿法线、也不是只动 y**。照「只动 y」做出来会是另一种飘法。
- **PS**：`col.rgb = LinearToSRGB(_Color.rgb) * tex2D(_MainTex, uv).rgb * v5`，基础变体 `v5 = (1,1,1)`。
- **属性**：`_Color` · `_ExtraAmbientColor` · `_MainTex` · **三个 `Vector1_<guid>`**（`nameIndices` 给的是 reg4/5/6）
  = 位移强度 / 时间滚动速度 / 空间频率。材质实值：`d24785e7…`=0.002/0.1/0.001 · `e2a1250f…`=0.5/10/2 · `fbd31611…`=0.5/0.001/0.1。
- **keyword**：`_APPLYAMBIENTCOLOR`（VS 两种体逐条比过：加了 `o5 = 1 + cb0[130].x*(LinearToSRGB(_ExtraAmbientColor) * cb0[56].rgb - 1)`，
  ⚠️ **它读的是全局 `cb0`、不是 `UnityPerMaterial`** —— 和正本 §五 那条「`_AmbientColorBlend` 是全局量、13 场全是 0」对得上）。
- **最小复刻**：世界 XZ 上的程序化噪声 + `t*速度` 滚动 + ×空间频率 + **顶点色 R 当遮罩**，位移加到 xyz 三分量；**不需要贴图**。

### 2 · `Pulsating Mesh`（23）
- **VS**：沿**法线**形变 + 心跳：
  `n = tex2Dlod(_NoiseTex, (O2W·v).xz * 0.1 * _NoiseSize + frac(t * _NoiseSpeed)).r` ← **世界 XZ，不是网格 UV**
  `hb = remap(sin(t*_HeartBeatSpeed + (O2W·v).x + (O2W·v).z) * 0.5 + 0.5, _HeartBeatMinMaxRemap)`
  `pos.xyz += v.normal * _DeformStrength * n * hb * v.color.r`
- **PS**：`o0.rgb = tex2D(_MainTex, uv).rgb * v5`，`o0.a = 1`。
- **属性**：`_MainTex` · `_NoiseTex`（VS 里 `sample_l` LOD0）· `_DeformStrength`(0.025~0.05) ·
  `_NoiseSpeed`/`_NoiseSize`(0.03~0.075 / 2.0) · `_HeartBeatSpeed`/`_HeartBeatMinMaxRemap`(1~2 / (0,1)·(0,2)) · `_ExtraAmbientColor`。
- 🔴 **关键两条**：噪声 UV 用**世界 XZ**；相位里**要加 `worldPos.x + worldPos.z`**（同一模型的不同实例错相，这是原版观感的一半）。

### 3 · `Unlit UV scroll`（13）—— 用户圈的那块「平绿板」
- **VS**：顶点**一动不动**，只写两组 UV：`o2.xy = v.uv * A.xy + frac(t * A.zw)`（`_MainTex`）·
  `o2.zw = v.uv * B.xy + frac(t * B.zw)`（`_SecondaryTex`）。`A`/`B` 都是 Vector4。
  ⇒ **「平」不是 bug，原版它本来就是平的一块，只是 UV 在滚。**
- **PS**：两张图按 **Overlay/HardLight** 混合再按不透明度插值：`m = b<=0.5 ? 2ab : 1-2(1-a)(1-b)`；
  `rgb = lerp(b, m, _Layers_Blend_Opacity) * LinearToSRGB(_Color.rgb) * v.color`；`o0.a = 1`（基础体）。
- **属性**：`_Color` · `_MainTex(+_ST)` · `_SecondaryTex(+_ST)` · `Vector4_62056e41…` · `Vector4_1` ·
  `_Layers_Blend_Opacity` / `_FinalAlphaMultiplier` / `_Depth_X_Falloff_Y`（**三处算式互证**）。
  实测 `Vector4_62056e…`=(0.31,0.70,0.01,0) / (10,1,-0.63,0) / (0.15,-1,-0.1,0) ⇒ **`.xy`=平铺、`.zw`=滚动速度**，与算式吻合。
- **keyword**：`_SOFT`（**真改代码**：多采 `_CameraDepthTexture`，`alpha *= pow(saturate((场景Z-粒子Z)/_Depth_X_Falloff_Y.x), _Depth_X_Falloff_Y.y)`）· `_PREMULTIPLY`。

### 4 · `Unlit shadows receiver`（12）
- **PS**：🔴 **真的接实时阴影**：`sc = mul(4×4, worldPos)` → `s = sample_c_lz(阴影图, sc.xy/sc.w, sc.z)`
  （`dcl_sampler mode_comparison`）→ `s = (1-s)*<全局强度>`；越界归 0；`s *= _ShadowColor.a`；
  `col = lerp(BaseMap.rgb*tint, _ShadowColor, s)`。208 条那一支是 `_SHADOWS_SOFT`（31 次 `sample_c_lz` PCF）。
- **VS**：`uv = v.uv * _BaseMap_ST.xy + _BaseMap_ST.zw` —— **这一族是全族里唯一能一字不差坐实 `UnityPerMaterial reg0 = _BaseMap_ST` 的地方**。
- **属性**：`_BaseMap(+_ST)` · `_ShadowColor`（默认 (0.35,0.4,0.45,1)，材质实测 0.52~0.69 偏色）· `_ExtraAmbientColor` · 全局 `_AmbientColorBlend`。

### 5 · `Tyranid Tentacle`（9）
- **VS**：世界 Y 遮罩 × 正弦波 × **沿局部轴**的位移：
  `ym = smoothstep01(saturate((y - _YMaskMinMax.x) / (_YMaskMinMax.y - _YMaskMinMax.x)))`
  `phase = y*_Riple_Size + t*_Speed + ((O2W·v).x + (O2W·v).z)`
  `pos.xyz += (0,0,_Displacement) * (_ForceMask * sin(phase)) * ym` ← `_MOVEMENTXAXIS=1` 时换成 +X（变体确实改了指令）
- **属性**：🔴 **这一族名字能一一对上**（`nameIndices` 顺序 = cbuffer = blackboard，且与寄存器吻合）：
  `_YMaskMinMax` · `_Speed`(0.2~0.3) · `_Riple_Size`(0.03~0.21) · `_ForceMask`(1.0) · `_Displacement`(0.1~1.0)。
- 🔴 **`_YMaskMinMax` 必须读** —— 它决定「根不动、梢摆得大」。

### 6 · `Floor Planar Reflections Grainny`（3）
> ⚠️ **2026-09-22 更正**：标题原写「🔴 这条直接关系到「整体偏亮」」—— **「整体偏亮」那条已被证伪**
> （分箱统计的假象，正本坑 ⑨）。这一族的算式**仍然照做**（现在已接上原版 shader），但**别再拿它去追「全图提亮」**。
- 🔴 **名字不骗人：它真的采样一张屏幕空间图当反射**：`refl = sample(反射图, 屏幕UV).rgb * 反射色`，
  屏幕 UV 是 VS 算好传下来的（`0.5*clip.xy/clip.w + 0.5`，带 `_ProjectionParams.x` 翻转）——
  URP 里这就是 `_CameraOpaqueTexture` 的采样方式，**不是自带贴图**。
  `col = lerp(BaseMap.rgb*tint, saturate(base + refl*baseAlpha)/<除数>, <混合强度>)`
- 🔴 **但「Grainny」是骗人的**：`_Iterations` 与 `_BlurRadius` **在整族字节码里从未被读过**（4 个变体全查、整族 0 个 `loop`）
  ⇒ 模糊/颗粒**不在这个 shader 里**。
- **属性**（`nameIndices` 坐实的）：`_BaseMap_ST`(reg3) · `_ShadowColor`(reg1) · `_AmbientColorBlend`(reg4) ·
  `_ExtraAmbientColor`(reg5) · **`_ReflectionStrength`(reg6，材质实测 0.27~0.39)** · `_ReflectionColorMultiplier`(reg7) · `_ColorCompensation`(reg8)。
- ⚠️ 部分材质上还挂着 `_KernelSize/_ReflectionPower/_ReflectionPower2/_ReflectionTint/_DepthPower` ——
  **这些不在本 shader 的属性表里**，是别的反射 shader 的残留，**别照抄**。
- **最小复刻**：**抓一张不透明场景图 + 按屏幕 UV 采样 + 按 `_ReflectionStrength` 混**；不用做模糊。

### 7 · `FX/Vortex`（2）
- **PS**：旋转 UV + **两层循环 8×2 = 16 次 hash = Voronoi/Worley F1**（无贴图，`frac(sin(dot(p,(15.27,99.41))))` 那套）：
  `uv' = rot(v1-0.5, length(v1-0.5)*_Twirl_Strength) + t*_RotationSpeed; uv' *= _NoiseScale;`
  `col = lerp(色1, 色2, voronoi)`；再采 `_Reflected_Image`（uv = `voronoi*_Offset.z + (o2.w,o3.w)`）乘 `_Reflection_Color` 叠上去。
- **属性**：`_Zoom` · `_ClampRange`（Remap 的 in/out 四值）· `_Offset`（`.xy` 主 UV 偏移 / `.z` 反射图扭曲）；
  `_NoiseScale`(3.08) / `_RotationSpeed`(0.15) / `_Twirl_Strength`(13.39)。**无 keyword**。

## 三、给下一步的三条

> 🔴 **2026-09-21 晚更新：这三条里 ①②已经做掉了** —— 现在的做法与成绩见
> `资料/战场13场_逐场对账_0920.md` §二之二（那份是正本，这里不抄第二份）。一句话：
> **7 族已经真的用上原版 shader 了**（运行时重建，arena1 实测 28/28、亮度比 0.987→0.994、
> 「风真的在动」也验过）。**下面三条保留原样**，因为第 ③ 条（槽位 ↔ 名字）**仍然没做完**。

1. **要复刻优先做的三条**（都是一眼能看出来、且公式已经拿到）：
   ① `Unlit Wind` 的程序化风 ② `Unlit UV scroll` 的两组滚动 UV ③ `Unlit shadows receiver` 的主光阴影接收。
2. 🔴 **`Floor` 那一族要单独对待**：它**真的在采样屏幕空间不透明图** ⇒ ~~它是「整体偏亮」的头号嫌疑~~
   ⚠️ **2026-09-22 更正**：**后半句作废**（「整体偏亮」是假象；而且 `WF_HIDE=Floor` 的跳变是**露出天空盒**造成的，正本坑 ⑥）。
   **这一族仍然要照原版接**（已接），但**不是「全图提亮」的原因**。
   （正本 §一 第 1 条里 `WF_HIDE=Floor` 实测**影响 60% 像素**）。**查它的优先级应该提前**。
3. ⚠️ **没能查实、留给下一轮的**（都卡在「属性名 ↔ cbuffer 槽位」这一层，因为 RDEF 被剥）：
   ① `Unlit Wind` 三个 `Vector1_<guid>` 各自落在**哪个分量**；② `Vortex` 的 `[0]/[2]/[4]` 三个标量名；
   ③ `Floor Planar` 的 `[5]`（除数）与 `[8]`（`_ColorCompensation` 的用法）。
   **要坐实只有两条路**：跑原版实况 dump 材质的实际 cbuffer，或**先按 `dump_shader.py` 的 `nameIndices` 对槽位**（这条更省事，我验过一次它是对的）。
