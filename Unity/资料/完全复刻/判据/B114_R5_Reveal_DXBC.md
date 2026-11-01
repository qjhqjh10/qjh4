# `B-45` · `_Reveal` 那段原版 shader 到底怎么算（DXBC 逐指令还原）

> 只读查证代理 `R-5` · 2026-11-01。**没跑 Unity**、没动 git、**没碰我们的 shader**。
> 所有结论的判据只有两类：① `d:/2/新解包资源/assets_full/.../Shader/Shader_-4463273156108355430.json`
> （Unity **自己的** `m_CommonParameters` 参数块，给出 `属性名 → 字节偏移`）；② 同一条 shader 的 **DXBC 反汇编**
> （`工具/disasm_dxbc.py`，走 `d3dcompiler_47.dll` 的 `D3DDisassemble`）。
> ⛔ 没有拿 ILSpy 注释、Ruri 反编译的 HLSL、我们自己的 `WFUIBorderHighlight.shader` 当判据
> （只在 §五 的交叉印证里引用一次 Ruri 的**同族兄弟** shader，且明确标了它只是旁证）。

---

## 一、原版 shader 身份

| 项 | 值 | 怎么定位的 |
|---|---|---|
| shader 名 | **`Everguild/UI/UI Border Highlight Appear Pulsating Scale`** | `Shader.m_ParsedForm.m_Name` |
| 资产 | `d:/2/新解包资源/assets_full/bundle_shaders_assets_all/Shader/Shader_-4463273156108355430.json`（`compressedBlob` 753,156 B / **128 个 DXBC 段**） | 材质 `Border Highlight Appear Yellow` 的 `m_Shader.m_PathID` 两跳 |
| 也在 | `wf_shaders.bundle`（工程里随包那一份，字节完全相同） | `工具/dump_shader_blob.py` 同时命中 |
| SubShader | 1 个 · **4 个 Pass** | pass0 = 主渲染（colMask 15、`Blend 5:SrcAlpha 10:OneMinusSrcAlpha, 1:One 10:OneMinusSrcAlpha`）；pass1 = 运动矢量（ShaderRequirements 4075）；pass2/pass3 = colMask 15 / colMask **0** |
| `_Reveal` 真正参与的程序 | **pass0 的 VS（生成 `v5.w`）+ pass0 的 PS（阈值判定）** | 见下 |

**关键前提（也是本轮能读通的钥匙）**：这颗 shader 的 DXBC **RDEF 段被剥**（128 段全是 `SHDR`，`RDEF 0`），
所以按资源名反查 `cbN[reg]` 一律 0 命中。**但 Unity 自己的 serde 里有 `m_CommonParameters` 参数块**，
里面每个被按名绑定的常量都带 `m_Index`＝**字节偏移**，`m_NameIndex`＝属性名。用它就能把 `cb1[5].x` 这种数字偏移**逐格翻成属性名**。

### 1·1 pass0 的常量缓冲布局（**这是全文的坐标表**）

`m_CommonParameters.m_ConstantBufferBindings` 明写 `$Globals → b0`、`UnityPerMaterial → b1` ⇒ **CB0 = `$Globals`，CB1 = `UnityPerMaterial`**。

CB0 = `$Globals`（`m_Size` **2128** = c0..c132）：

| 属性 | 字节偏移 | 寄存器 | 出处 |
|---|---|---|---|
| `_ScaledScreenParams` | 48 | `cb0[3]` | `progFragment` |
| `_GlobalMipBias` | 64 | `cb0[4].x` | `progFragment` |
| `_TimeParameters` | 304 | `cb0[19]` | `progFragment` |
| `_ProjectionParams` | 352 | `cb0[22]` | `progFragment` |
| （`_ClipRect` 一族，未按名绑定） | 2096 | `cb0[131]` | CLIP_RECT 变体里读 |

CB1 = `UnityPerMaterial`（`m_Size` **176** = 11 个 float4）：

| 属性 | 字节偏移 | 落在 | 出处 |
|---|---|---|---|
| **`_ColorSecondary`** | 0 | `cb1[0]` | `progFragment` |
| **`_ColorMain`** | 16 | `cb1[1]` | `progFragment` |
| **`_StartOffset`** | **32** | **`cb1[2].x`** | **`progVertex`**（只有 VS 绑它） |
| **`_Color_offsets`**（Dim 2） | **36** | **`cb1[2].y` / `cb1[2].z`** | `progFragment` |
| （未绑定，2 个 float + 1 个 float4） | 44 / 48 | `cb1[2].w` / `cb1[3]` | —— |
| **`_Noise_Strength_X_Speed_Y_Scale_Z`** | 64 | **`cb1[4].xyz`** | `progFragment` |
| **`_Reveal`** | **80** | **`cb1[5].x`** | `progFragment` |
| **`_Smoothness`** | **84** | **`cb1[5].y`** | `progFragment` |
| （未绑定） | **88 / 92** | **`cb1[5].z` / `cb1[5].w`** | ⚠️ 见 §三「不能确定」 |
| **`_Scale_X_Speed_Y`** | 96 | **`cb1[6].xy`** | `progFragment` |

> ① `_Color_offsets` 只有 **Dim 2**（`m_Dim: 2`）⇒ 只占 `.y/.z` 两个 float，`.x` 不属于它。
> ② 三个文字纹理：`_MainTex → t0/s0`、`_Noise_R → t1/s1`（`progFragment.m_TextureParams`，按名绑定）。

---

## 二、算式还原

### 2·1 顶点着色器（pass0 VS）—— `v5.w`（「角度坐标」）就是在这里生成的

输入：`v0`=POSITION · `v1`=NORMAL · `v3`=TEXCOORD0(uv0) · `v4`=COLOR。
输出：`o0`=clip pos · `o1`=uv0 · `o2`=color · `o3`=裁剪矩形 · `o4`（**无人消费**）· `o5`（`o5.w` = 角度）。

原版按关键字编译出 **3 种 VS**（`m_PlayerSubPrograms` 的 `m_BlobIndex` / `m_KeywordIndices` 逐条对过；
同样三个关键字状态下 clip/alphaclip 组合的 VS **完全相同**，这也反证了分段顺序 = 关键字顺序）：

| 关键字 | 变体 | `角度 = ?`（度） | 极坐标原点 |
|---|---|---|---|
| `[]`（无） | VS-A | **`_StartOffset + cb1[5].z`** | **屏幕空间**：`(screenUV − uv0.zw)` |
| `_DEBUG`（+可选 `_AUTOROTATION`） | VS-B | **`_StartOffset`**（**没有** `_Rotation`、**没有**自转项） | **网格 uv**：`(uv0.xy − 0.5)` |
| `_AUTOROTATION + _FLIPDIRECTION` | VS-C | **`_StartOffset + 360·frac(t · cb1[5].w)`** | **屏幕空间**（同 VS-A） |

公共部分（三种都一样）：

```
// ── 世界/裁剪（row-vector 约定，矩阵按 cb1[0..3]=unity_ObjectToWorld、cb0[78..81]=unity_MatrixVP 展开）
world  = v0.x*M0 + v0.y*M1 + v0.z*M2 + M3
clip   = world.x*VP0 + world.y*VP1 + world.z*VP2 + VP3      // o0 = clip
o1 = v3 ; o2 = v4 ; o4.xyz = world

// ── 屏幕 UV（= Unity ComputeNonStereoScreenPos 的展开：o.xy = 0.5·clip.xy(projX 折进去) + 0.5·clip.w, 再 /clip.w）
sp.x = 0.5 + 0.5·clip.x / clip.w
sp.y = 0.5 + 0.5·(clip.y · _ProjectionParams.x) / clip.w     // cb0[22].x = ±1

// ── VS-A / VS-C：极坐标原点来自 uv0.zw（逐实例数据）
p = sp − uv0.zw              //  VS-B：p = uv0.xy − 0.5
//   实际指令：add r0.xy, r0.xyxx, -v3.zwzz   ← VS-A
//            add r1.xy, r1.xyxx, -|...|（略）; VS-B: add r0.yz, v3.xxyx, l(0,-0.5,-0.5,0)
// 注：VS-A 还做了一次 (+v3.zw) 再 (+0.5−v3.zw) 的抵消，纯属图节点留痕；净效果就是 p 绕 0 取极坐标。

// ── 按上表算角度（度）→ 弧度
ang_rad = radians(<见上表>)          // mul r0.z, l(0.017453)  ← 明确是「度 → 弧度」

// ── 旋转 p（顺时针；矩阵 [[cos, sin], [−sin, cos]]）
s = sin(ang_rad) ; c = cos(ang_rad)
q = float2(p.x*c + p.y*s, -p.x*s + p.y*c)

o4.w = 2 * length(q)                 // ★ 只算不用：PS 不声明 INTERP3 ⇒ 死的
o5.w = atan2(q.y, q.x) * 0.159236    // ← 唯一的「角度坐标」输出，PS 里就是 v5.w
```

指令出处（VS-A 本体，段 idx0）：
`mul r1.z, cb0[23].x, cb0[62].x` … `add r0.z, cb2[2].x, cb2[5].z`（`_StartOffset + 未命名`）
`mul r0.z, r0.z, l(0.017453)` · `sincos r1.x, r2.x, r0.z` · `dp2 r1.x, r0.xyxx, r3.yzyy`
`dp2 r1.y, r0.xyxx, r3.xyxx` · `dp2 r0.z, r0.xyxx, r0.xyxx` / `sqrt` / `add o4.w, r0.z, r0.z`
`mul o5.w, r0.x, l(0.159236)`

> 🔴 **`0.159236`** 是十进制字面量，等于 **1/6.28**（1/6.28 = 0.1592356688 → 六位打印 0.159236）。
> 不是 `1/(2π)`（那会是 `0.159155`）。差 0.05%，视觉上无差别，但它说明图上写的是 **除以 6.28**。
> ⇒ `v5.w ∈ (−0.5, +0.5]`（atan2 的 (−π, π] 除以 6.28）。**没有 `+0.5` 归一到 0..1。**
> 这一点和 `_Reveal` 的 Range **`[-0.5, 1]`** 严丝合缝（见 §2·3）。

VS-C 的自转项本体（段 idx3）：
`mul r0.z, cb0[19].x, cb2[5].w` → `frc r0.z, r0.z` → `mad r0.z, r0.z, l(360.000000), cb2[2].x` → `mul r0.z, r0.z, l(0.017453)`

### 2·2 片元着色器（pass0 PS）—— 全部 66 条指令

`_Reveal` 那条链**只占最后 10 条**，但它前面的每一格都进了同一个输出，所以一并还原：

```
// ── 入参
suv   = v0.xy                     // SV_POSITION（像素）
uv    = v1.xy                     // o1 = uv0
col_v = v2.xyzw                   // o2 = 顶点色（uGUI 就是 Image.color）
ang   = v5.w                      // ← §2·1 的 o5.w

// ── ① 屏幕 UV（和 VS 同一套写法）
sp.x = suv.x / _ScaledScreenParams.x                                   // div r0.xy, v0, cb0[3].xy
sp.y = (_ProjectionParams.x < 0 ? (_ScaledScreenParams.y - suv.y) : suv.y) / _ScaledScreenParams.y

// ── ② 噪声 UV：绕 (−0.5) 取极坐标式旋转
p = float2(sp.x, 1 - sp.y) * _Noise_Strength.z - 0.5                   // mad r0.xy, r0.xzxx, cb1[4].zzzz, l(-0.5,-0.5)
a = frac(_TimeParameters.x * _Noise_Strength.y) * 6.283185             // mul/frc/mul  (cb0[19].x · cb1[4].y)
nuv = float2(p.x*cos(a) + p.y*sin(a), -p.x*sin(a) + p.y*cos(a)) + 0.5  // sincos + 两个 dp2 + add 0.5

// ── ③ 噪声采样（t1 = _Noise_R，只取 .r）
nz = tex2D_bias(_Noise_R, nuv, _GlobalMipBias.x).r * 2 - 1             // sample_b + mad 2, -1

// ── ④ 「Pulsating Scale」：按中心缩放 MainTex 的 uv
s  = 0.5*(1 - _Scale_X_Speed_Y.x) * (sin(_TimeParameters.x * _Scale_X_Speed_Y.y) + 1) + _Scale_X_Speed_Y.x
uv2 = s * (uv - 0.5) + 0.5                                             // mad r0.yz, r0.yyyy, r0.zzwz, 0.5
uv2 = uv2 + nz * _Noise_Strength.x                                     // mad r0.xy, r0.xxxx, cb1[4].xxxx, r0.yzyy
                                                                       //   ★ 同一个标量加到 x 和 y 两个分量上

// ── ⑤ 主纹理（t0 = _MainTex，UI 自己那张 sprite）
c = tex2D_bias(_MainTex, uv2, _GlobalMipBias.x)                        // sample_b t0

// ── ⑥ 双色渐变：由 sprite 的【红通道】按 _Color_offsets 两档阀值切换
k = smoothstep01( saturate( (c.r - _Color_offsets.x) / (_Color_offsets.y - _Color_offsets.x) ) )
        // add/mad(r0.x, -2, 3)/mul/mul ⇒ u²(3−2u)
col = lerp( LinearToSRGB(_ColorSecondary.rgb), LinearToSRGB(_ColorMain.rgb), k )
        // log2*0.4166667 → exp2 → *1.055 − 0.055；≤0.0031308 时改走 *12.92321（逐分量 movc）

// ── ⑦ ★★ `_Reveal` 的那条链 ★★
S   = _Smoothness                                        // cb1[5].y
hi  = ang + S                                            // add r1.x, v5.w,  cb1[5].y
lo  = ang - S                                            // add r1.y, v5.w, -cb1[5].y
w   = hi - lo = 2S                                       // add r1.x, -r1.y, r1.x
d   = _Reveal - lo = _Reveal - ang + S                   // add r1.y, -r1.y, cb1[5].x
u   = saturate( d / w )                                  // div r1.x, 1.0, r1.x ; mul_sat r1.x, r1.x, r1.y
m   = u*u*(3 - 2*u)                                      // mad r1.y, r1.x, -2, 3 ; mul r1.x,u,u ; mul r1.x, r1.x, r1.y
        ⇒  等价写法：  m = 1 − smoothstep( _Reveal − S , _Reveal + S , ang )
        ⇒  `_FLIPDIRECTION` 变体只是把上面每个 `v5.w` 换成 **`−v5.w`**（add r1.x, -v5.w, cb1[5].y 等 4 条）

// ── ⑧ 输出（★ 预乘：alpha 同时乘进 rgb 和 a）
o.rgb = col * c.a * m * col_v.rgb
o.a   =       c.a * m * col_v.a
        // mul r0.w, r0.w, r1.x ; mul r0.xyz, r0.wwww, r0.xyzx ; mul o0.xyzw, r0.xyzw, v2.xyzw
```

### 2·3 `_Reveal` 的语义（人话版）

- **`v5.w` 是一个 −0.5…+0.5 的「角度坐标」**（`atan2/6.28`，**不归一、不加 0.5**）。
- **`m = 1 − smoothstep(_Reveal−_Smoothness, _Reveal+_Smoothness, v5.w)`**：
  `v5.w` **小于** `_Reveal−S` 的地方 **全亮**（m=1），大于 `_Reveal+S` 的地方 **全灭**（m=0），中间是一条宽 **2S** 的软边。
- `_Smoothness` 就是**软边半宽**，单位与角度坐标相同（1 个整圈 = 1.0）。
  若 `S = 0` ⇒ `1/0 = +inf`，退化成硬边界（正好落在边界上会得到 `inf*0 = NaN`）。
- ⇒ **`_Reveal` 从 −0.5 推到 +0.5（再加 S）就扫完一整圈**；`_Reveal ≥ 0.5+S` 之后全亮，`_Reveal ≤ −0.5−S` 全灭。
  这解释了材质里的三个值：`Border Effect For Level up` = **−0.5（全灭/出厂态）**、
  `Border Highlight Appear Yellow` = **0.723（常亮）**、`Border Highlight Rotating Campaign` = **1.0（常亮）**。
- 扫的方向：被揭开的集合是 `{ ang < 阈值 }`，阈值从 −0.5 涨到 +0.5 ⇒ 起点是 `atan2 = −π` 那条缝
  （正对着 **−X 方向**、也就是 `sp − uv0.zw` 指向左侧时），然后角度**连续增大**的方向推进。
  `_FLIPDIRECTION` 取 `−v5.w` ⇒ 方向镜像。
- `_StartOffset` / `cb1[5].z` / 自转**都不进 m 的算式**，它们只改变 `v5.w` 本身（§2·1 的旋转）。
  ⇒ 「缝」在哪、往哪转，全在顶点段决定。

---

## 三、能确定 vs 不能确定

### ✅ 能确定（判据 = 名字 + 字节偏移，或纯指令）

1. **`_Reveal` = `cb1[5].x`（byte 80）、`_Smoothness` = `cb1[5].y`（byte 84）** —— `progFragment.m_CommonParameters` 按名绑定。
2. **`_Reveal` 的算式**：`m = 1 − smoothstep(_Reveal−S, _Reveal+S, v5.w)`（10 条指令逐条列在 §2·2 ⑦）。
3. **`_Smoothness` 是软边半宽**（`w = 2S`），**不是** 0..1 的「柔化强度比值」。
4. **`v5.w` = `atan2(q.y, q.x) * 0.159236`**，值域 `(−0.5, +0.5]`；`0.159236` 是字面量 ≈ 1/6.28。
5. **`_Reveal` 的 Range 是 `[-0.5, 1]`、默认 1.0**（`m_PropInfo`：`def=[1.0, -0.5, 1.0, 0]`）。我们写的 `Range(-1,1)` 是错的。
6. **`_StartOffset` = `cb1[2].x`（byte 32），只在【顶点】着色器里被读**，单位**度**（Range 0..360），
   参与方式 = 与旋转角**相加**（`add r0.z, cb2[2].x, cb2[5].z`；自转变体是 `mad r0.z, frac(t·speed), 360, _StartOffset`）。
   ⇒ **我们 shader 第 106-109 行那句「`_StartOffset` 不进算式」是错的**（见 §五·2）。
7. **`_Noise_Strength_X_Speed_Y_Scale_Z` = `cb1[4].x/.y/.z` = 强度/转速/尺度**：
   `.z` 缩放屏幕 UV、`.y` 是**圈/秒**（`frac(t·.y)·2π`）、`.x` 是**同一个标量加到 uv 的两个分量**（不是各向异性）。
8. **`_Scale_X_Speed_Y` = `cb1[6].x/.y`**：`.x` = 缩放**下限**（不是幅度）、`.y` = sin 的**弧度/秒**。
   实际缩放 `s = 0.5(1−x)(sin(t·y)+1)+x`，取值 `[x, 1]`（`x=0.9,y=3` ⇒ 0.9…1.0、周期 2.09 s）。
   （这里的 `t` = `_TimeParameters.x`。**`_TimeParameters.x` 就是「秒」** —— 判据是 URP 自己的声明：
   `Library/PackageCache/com.unity.render-pipelines.universal@…/ShaderLibrary/UnityInput.hlsl:46`
   `float4 _TimeParameters; // t, sin(t), cos(t)`。这条顺带把「`_AutoRotationSpeed = −0.5` ⇒ 2 s 一圈」的量纲坐实。）
9. **`_Color_offsets` 是双色渐变的两个红通道阈值**（`.x` 低档 → `_ColorSecondary`，`.y` 高档 → `_ColorMain`），
   **不是** 位置偏移；且两个颜色都先做 **Linear→sRGB** 再插值。
10. **`_Range` / `_RangeStartOffset` / `_Add` / `_ColorMask` / `_NoiseStrength` 根本不是这颗 shader 的属性**
    （`m_PropInfo` 只有 19 条，逐条列过）。它们只存在于材质 JSON 里 ⇒ **死值**。
11. **`_Rotation` / `_AUTOROTATION` / `_FLIPDIRECTION` / `_DEBUG` / `_AutoRotationSpeed` 是 `shader_feature` 关键字**
    （`m_KeywordNames` 里 15=`_DEBUG`、16=`_AUTOROTATION`、17=`_FLIPDIRECTION`），
    同时也有同名 float 属性；但这几个 float **只用于选关键字**，编译产物里除了下面两条外没有其它读取口。
12. **`_DEBUG` 打开时：`_Rotation` 项和自转项都被编译掉了**（VS-B 只有 `_StartOffset`）。
    而且 VS-B 的极坐标原点从「屏幕空间 − `uv0.zw`」换成了「网格 `uv0.xy − 0.5`」。
13. **混合状态**：`Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`（**不是加色**）。
    输出是**预乘**的（`rgb *= c.a*m`），alpha 侧用 `One OneMinusSrcAlpha` 正好自洽。
14. **四个 pass / `ZWrite 0` / `ZTest 4(LEqual)` / `Cull 2(Back)` / `colMask 15`**（pass0）。
15. **实际在用这颗 shader 的材质只有 5 个**（全 `assets_full` 扫 `m_Shader.m_PathID` 命中）：

| 材质 | `m_ValidKeywords` | 走哪个 VS | `_Reveal` | `_Smoothness` | `_StartOffset` | `_Rotation` | `_AutoRotationSpeed` |
|---|---|---|---|---|---|---|---|
| `Border Highlight Appear Yellow`（menusharedresources） | `[]` | **VS-A** | 0.723 | 0.032 | 0 | 0 | −180（**死**） |
| `Border Effect For Level up`（scenes_mainmenuwarpforge） | `[]` | **VS-A** | **−0.5** | 0.032 | 0 | 0 | −180（**死**） |
| `Border Highlight Rotating Campaign`（menus） | `_AUTOROTATION,_FLIPDIRECTION` | **VS-C** | 1.0 | 0.706 | 0 | 0 | **−0.5（唯一真读到的）** |
| `Border Gacha Button Glow`（menus） | `_AUTOROTATION,_DEBUG` | **VS-B** | 1.0 | 0.169 | 0 | 7.42（**死**） | −186.64（**死**） |
| `UI Border Highlight Appear Pulsating Scale`（模板材质） | `_DEBUG` | **VS-B** | 1.0 | 0.05 | 0 | 0 | 1（**死**） |

> ⇒ **带 `_DEBUG` 的两个材质，`_Rotation 7.42` 和 `_AutoRotationSpeed −186.64` 都是死值**；
> 真正会自转的只有 `Border Highlight Rotating Campaign`（`−0.5` ⇒ **0.5 圈/秒 = 2 s 一圈**）。
> 这条也顺带把「`_AutoRotationSpeed = −180` 是不是疯转」的疑问消掉了 —— 那些材质根本不走那条分支。

### ❓ 不能确定（逐条点名，⛔ 不填猜测）

1. **`cb1[5].z`（byte 88）到底是哪个属性 —— 只能按语义推，没有名字作证。**
   - 事实：VS-A 里它是 `_StartOffset + cb1[5].z`，再 `×0.017453`（度→弧度）。
   - 事实：`UnityPerMaterial` 还空着 7 个 float 槽（44/48/52/56/60/88/92），
     候选属性正好 7 个：`_DEBUG` `_FLIPDIRECTION` `_Rotation` `_AUTOROTATION` `_AutoRotationSpeed` `_QueueOffset` `_QueueControl`。
   - 事实：`progVertex.m_CommonParameters` **只按名绑定到 `_StartOffset`**，剩下的槽位在 serde 里是「unmapped」。
     `RDEF` 又被剥 ⇒ **从本地数据拿不到「byte 88 = 谁」的证明**。
   - 推断（**不是判据**）：唯一取值像「角度」的候选是 **`_Rotation`**（材质里是 7.42 / −2.06 / 0）。
     想要证实只能做 §四 的第 3 条差分实验。**在此之前，请一律按「`_Rotation`（待证）」写，别写成已证。**
2. **`cb1[5].w`（byte 92）= 自转**转速**（圈/`_TimeParameters.x`）—— 同样是推断。**
   依据更强一些：它是 VS-C 里 `frac(t · X) · 360` 的 `X`，而候选里只有 `_AutoRotationSpeed` 是「速度」语义；
   且唯一走 VS-C 的材质值 **−0.5**，按「圈/秒」解释正好是 2 s 一圈（唯一讲得通的量纲）。
   **仍然是推断，不是按名绑定。**
3. **`_FLIPDIRECTION` 在【顶点】段有没有副作用，测不出来。**
   本地只编译了 `_AUTOROTATION + _FLIPDIRECTION` 这一种组合（没有任何材质是 `_FLIPDIRECTION` 单独开），
   所以 VS-C 相对 VS-A 的差异里，「自转」和「翻转」**分不开**。PS 侧的 `−v5.w` 是确定的。
4. **`_QueueOffset` / `_QueueControl` 在不在 CBUFFER 里** —— 它们 flags=1（HideInInspector）、任何程序都没按名绑定过，
   理论上仍占槽位（`m_Size 176` 有 4 个 float4 说明有用不到的预留），但**无法证明它们具体在哪一格**。
5. **`o4.w`（`2·length(q)`，径向距离）算出来没人用** —— PS 的输入签名里 INTERP3 是未使用寄存器。
   我没有去查「是不是被别的 pass / 别的变体读了」以外的可能；至少 pass0 的 8 种 PS 都不读它。
6. **`cb0[131]` 的裁剪矩形**（`UNITY_UI_CLIP_RECT` 变体）在 `$Globals` 里而不是 `UnityPerMaterial`，
   这一点和 UGUI 常见做法不同；本轮没深挖它叫什么（不是本任务范围）。

---

## 四、下一步差分实验该量哪几个点

> 目的只有两个：① 把 §三 的**推断**（byte 88/92）坐实；② 把「我们重写的算式对不对」变成可量化的。
> 建议一律固定 `uv0.zw`（= 极坐标原点）与 `_MainTex`，只动一个标量。

**① `_Reveal` 扫描（验证 §2·3 的 `m = 1 − smoothstep(R−S, R+S, ang)`）**

| `_Reveal` | `_Smoothness` | 期望 | 量什么 |
|---|---|---|---|
| −0.5 | 0.032 | 只剩 `ang ≈ −0.5` 那一个点有 0.5，其余 0 | 整圈峰值位置 |
| −0.5 | 0 | **全灭**（硬边，边界落在 −0.5） | 有无 NaN 亮点 |
| 0.0 | 0.032 | 揭开半圈（`ang < −0.032` 全亮，`ang ∈ (−0.032, 0.032)` 过渡） | 边界中心角 = 0.0 |
| 0.25 | 0.032 | 边界中心角 = **0.25** | 边界角是否 = `_Reveal` |
| 0.5 | 0.032 | 基本全亮（只剩 `ang ∈ (0.468, 0.5]` 灭） | 残留角的宽度 = 2S |
| 0.723 | 0.032 | 全亮 | —— |
| 0.0 | 0.169 | 过渡带宽 **0.338**（整圈的 33.8%） | 量两档 S 的带宽比 = 0.169/0.032 = 5.28× |
| 0.0 | 0.706 | 过渡带宽 **1.412 > 1 整圈** ⇒ 全圈都是渐变、没有「全亮」段 | 边界对角色的存在性 |

- **关键判据是「边界中心角是否正好等于 `_Reveal`」** —— 我们的实现如果写成 `ang < _Reveal` 就对了；
  如果写成 `reveal = (_Reveal+1)/2` 再比 `ang∈[0,1]`（我们现在就是这么写的），**边界会整体偏移，且软边宽度错 2 倍**。
- 另外量 **`_Smoothness = 0.706` 时是否出现「整圈都是灰的」**（没有纯亮段）—— 这条只有「S 是绝对半宽」的写法才会出现。

**② 角度坐标值域（验证 `atan2/6.28` 且**不加 0.5**）**
把 `_Reveal` 设成 **−0.5**、`_Smoothness` 设成 **0**，然后绕一圈取 8 个方位读 alpha 为 1 的那一个点：
它必须落在 **`atan2 = −π`（−X 方向）** 上。若落在 +X 或 +Y，说明 `atan2` 的自变量顺序/符号抄反了。

**③ ⚠️ 定 `byte 88` 是不是 `_Rotation`（§三·❓1 唯一能坐实的方法）**
在 **无关键字** 材质（`Border Highlight Appear Yellow` 或 `Border Effect For Level up`）上：
- `_StartOffset = 0`、`_Reveal = −0.5`、`_Smoothness = 0`；
- 把 `_Rotation` 从 **0 → 90**；
- **看那个单点亮斑是否绕中心整体转了 90°（顺时针）**。
  - 转了 ⇒ `byte 88 = _Rotation`（顺手也验了旋转方向）；
  - 没转 ⇒ `byte 88` 是别的属性，`_Rotation` 另有出处（那就把 7 个候选逐个开关一次，各拍一张）。
- 同样地，把 `_StartOffset` 从 0 → 168 再拍一张，确认它与 `_Rotation` 是**相加**（`168+90 = 258`）而不是相乘。

**④ 定 `byte 92` 是不是 `_AutoRotationSpeed`**
用 `Border Highlight Rotating Campaign` 那套关键字（`_AUTOROTATION + _FLIPDIRECTION`）：
- 固定 `_AutoRotationSpeed = −0.5`，录 4 秒（≈120 帧 @30fps），量亮斑转一圈用了几秒 ⇒ 必须 **2.0 s**；
- 再改成 `−0.25` ⇒ 必须 **4.0 s**（线性关系 ⇒ 量纲是「圈/秒」）；
- 同时用 `_FLIPDIRECTION = 0/1` 两张，确认方向相反。

**⑤ 噪声（验证 `_Noise_Strength` 三个分量的含义）**
材质 `Border Highlight Appear Yellow`：`(.005, .005, 15)`；对比 `Border Gacha Button Glow`：`(.02, .01, 8.17)`。
- `.z`：固定其余，`.z` 从 **8.17 → 15**，噪声颗粒应**变细约 1.84 倍**（屏幕 UV 缩放，所以与元素大小无关 —— 这条本身也是个好判据：
  **把 UI 元素放大一倍，噪声纹样不应跟着放大**）。
- `.y`：`.y = −0.1` ⇒ 噪声绕中心 **10 s 转一圈**；`.y = 0.005` ⇒ 200 s 一圈（基本静止）。
- `.x`：`.x = 0.02` 时 uv 的抖动幅度就是 ±0.02（占元素宽度 2%）；`.x = 0.005` ⇒ ±0.5%。
  ⚠️ **判据点：x/y 两个分量加的是同一个标量**（各向同性平移），不是各加各的。

**⑥ 双色渐变（验证 `_Color_offsets` 是红通道阈值）**
固定一张纯红渐变的 `_MainTex`（左 0 → 右 1），`_Color_offsets = (0.8, 1)`：
- `c.r ≤ 0.8` 区域必须是 **`_ColorSecondary`**、`c.r ≥ 1.0` 区域必须是 **`_ColorMain`**；
- 把 `_Color_offsets` 改成 `(0.0, 1.0)` ⇒ 整个渐变段铺满全宽（这一步能一秒区分
  「阈值」和「位移」两种解释）。
- 再验 **sRGB 转换**：把 `_ColorMain` 设成 `(0.5,0.5,0.5,1)`，屏幕上应显示 **≈0.735**（sRGB(0.5)=0.7354），
  不是 0.5。若显示 0.5 ⇒ 少了 Linear→sRGB 那一段。

**⑦ 输出预乘（验证 `rgb *= c.a*m`）**
固定 `m=1`、顶点色白，把 `_MainTex` 的 alpha 设成 **0.5**：
- 预乘写法 ⇒ 屏幕亮度 ≈ **半**（`rgb*0.5` 再乘 `SrcAlpha` 的 0.5 ⇒ 实际 0.25）；
- 非预乘写法 ⇒ 亮度只按 `SrcAlpha` 掉一次。
- ⚠️ 这条同时会被混合模式影响（`Blend SrcAlpha OneMinusSrcAlpha` 而不是 `One One`），
  所以**先把 `Blend` 抄对再看这一条**。

---

## 五、顺手发现（⛔ 一条都没动手改）

1. 🔴 **我们的 `WFUIBorderHighlight.shader:106-109` 有一段判据是错的**：
   > 「`_StartOffset` **不进算式** —— 它是声明属性，但不在这颗 shader 编译字节码的常量名里 ⇒ 原版像素代码不读它」
   **实际**：`_StartOffset` 在 **`progVertex` 的 `m_CommonParameters` 里按名绑定在 byte 32**，
   VS 里两处读它（`add r0.z, cb2[2].x, cb2[5].z` / `mad r0.z, r0.z, 360.0, cb2[2].x`）。
   **它不进的是「片元」那段，不是「整个算式」** —— 它决定缝在哪。同一条注释里
   「`_Range`/`_RangeStartOffset`/`_Add`/`_ColorMask` 是死值」这半句 **成立**。
   （那次大概只看了 `names_in()` 的 ASCII 扫描 —— 而 RDEF 被剥时它一个名字都扫不出来，
   所以「26 个常量名」这个前提本身也可疑，值得复核那条判据当时是怎么取到的。）
2. 🔴 **我们的属性表有 4 条原版根本不存在的属性**：`_Range` `_RangeStartOffset` `_Add` `_ColorMask`（`m_PropInfo` 19 条里没有）。
   留着不致命（`HasProperty` 会兜底），但会让后人以为它们生效 —— 和 `B112W` 报告 §七·7 记的 `MenuFxMaterials`
   写死属性名是同一个坑。
3. 🔴 **`_Reveal` 的 Range 抄错了**：原版 **`[-0.5, 1]`，默认 `1.0`**；我们写的是 `Range(-1,1)` 默认 0.723。
   `_Smoothness` 原版 Range `[0,1]` **默认 0.05**（我们默认 0.032，那是材质值不是 shader 默认值）。
   `_Noise_R` 原版默认 `"white"`（我们写 `"gray"`）。三个默认值都影响「新建材质时看到什么」。
4. 🔴 **我们的 `_Reveal` 语义和原版差得很远**（`shader:115-122`）：
   - 我们：`reveal=(_Reveal+1)*0.5`、`opened=saturate((reveal*(1+soft)-ang)/soft)` —— 这是「按比值展开 + **相对**柔化」；
   - 原版：`m = 1 − smoothstep(_Reveal−S, _Reveal+S, ang)` —— **`_Smoothness` 是绝对半宽（整圈的多少倍）**。
   - 后果：**同一条 `_Smoothness = 0.032` 在原版是「整圈的 6.4% 宽」，在我们这儿会随 `_Reveal` 变宽变窄**，
     而且 `_Reveal` 的 0 点也不在同一个角上（我们把 −1..1 映射到 0..1，原版 −0.5..1 直接当角度用）。
5. 🔴 **原版整条颜色链我们完全没复刻**（不是 `_Reveal` 的锅，但同一个 frag）：
   - 原版双色渐变由 **`_MainTex.r`** 经 `_Color_offsets` 两档阈值 `smoothstep` 决定；
     我们的实现拿**噪声值** `n` 直接 `lerp`，**根本没有采 `_MainTex`**（我们声明了 `_MainTex` 但 frag 里从没 `tex2D` 过它）。
   - 原版两个颜色都先 **Linear→sRGB**；我们没有。
   - ⇒ 「边框形状」在原版来自 **`_MainTex.a`**（乘进输出）；我们是自己拿 `length(c)` 画的径向衰减（`shader:121`）。
   - ⇒ **`_Reveal` 只是「让已经由 sprite 定形的高亮沿角度显形」**，不是「画一圈亮带」。
6. 🟠 **`_Noise_Strength` 的 `.x` 我们理解成了「振幅比」**，原版是**同一个标量加到 uv 的 x 和 y 上**（各向同性平移）；
   `.y` 原版是 **圈/秒**（`frac(t·y)·2π`，`y=−0.1` ⇒ 10 s 一圈），我们写的是 `_Time.y * y * 0.01`；
   `.z` 原版是**屏幕 UV 的缩放**（8.17 ⇒ 全屏约 8 个平铺），我们用的是 `length(c) * z`（径向）。
7. 🟠 **`_Scale_X_Speed_Y` 的 `.x` 是「缩放下限」不是「幅度」**：`s = 0.5(1−x)(sin(t·y)+1)+x ∈ [x,1]`。
   我们现在完全没用这个属性（`shader` 里 `_Scale_X_Speed_Y` 只出现在 `ang * max(.x,0.001)`，语义完全不同）。
8. 🟠 **`_Rotation` / `_AutoRotationSpeed` 在绝大多数材质里是死值** —— 因为它们都带 `_DEBUG` 关键字。
   将来如果有人「按材质值抄实现」（比如照着 `−186.64` 去做自转），会做出原版根本没有的疯转。
9. 🟠 **原版 4 个 pass，我们只有 1 个**：pass1 是运动矢量（`unity_MatrixPreviousM` + `unity_MotionVectorsParams`，
   ShaderRequirements 4075），pass2 是另一份 unlit，pass3 的 `colMask = 0`（不写颜色，多半是深度/预通道）。
   如果我们的 UI 不需要 motion vector，可以继续只做 pass0，但**别再写「原版只有 1 个 pass」**。
10. ⚪ **`0.159236` vs `1/(2π)`**：原版是除以 **6.28**。做量化对比时如果差 0.05% 的角度，别以为是 bug。
11. ⚪ **同族的 `UI Border Highlight Pulse Alpha` 有 Ruri 反编译的可用 HLSL**
    （`d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_UI Border Highlight Pulse Alpha/`），
    它的噪声旋转段（`Sub0_Pass0_Fragment_b7.hlsl:66-73`）与本 shader 的 `②③` **逐指令同构**，
    可以当**旁证**（我核过：`_112 = (sp.x·z−0.5, (1−sp.y)·z−0.5)`、`uv' = (dot(p,(cos,sin))+0.5, dot(p,(−sin,cos))+0.5)`
    —— 与我们 DXBC 里的 `dp2 r1.x/r1.y` 完全一致）。**但它不是本 shader 的判据**，
    本 shader 的 `_Reveal` 段那边**没有**对应源码，只能走 DXBC。
12. ⚪ 试验用的中间产物都在 **`d:/4/_tmp_view/b114_r5/`**（`ps_full.txt` 原始反汇编、`ps_clean.txt` 去 NUL 版、
    `ps_distinct.txt` 去重后的 23 个 PS 体、`split.py` / `cbmap.py` / `mats.py` 三个只读脚本）。
    ⚠️ 提醒后人：`disasm_dxbc.py` 落盘的 `--out` 文件末尾带 **NUL 字节**（`GetSize` 把结尾 0 也算进去了），
    `grep` 会把它当二进制。我这边是先把 `\x00` 剔掉再分析的。
