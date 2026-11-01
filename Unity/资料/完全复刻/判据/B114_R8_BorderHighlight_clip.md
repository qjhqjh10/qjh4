# `B114_W33` · 我们这颗 Border Highlight 自己的 `UNITY_UI_CLIP_RECT` 变体：软边还是硬边

> 只读查证代理 `R-8` · 2026-11-01。**没跑 Unity**、**没动 git**、**没碰任何源码/资产**。
> 判据只有两类：① 原版 shader 自己的 **DXBC 反汇编**（本轮**独立**从 JSON 重解了一遍，没依赖别人的中间文件）；
> ② Unity **本地安装**里那份 ShaderGraph UI 目标源码 `CanvasPass.hlsl`（逐指令对得上，作为算式「叫什么」的旁证）。
> ⛔ 没有把同族 `UI Border Highlight Pulse Alpha` / `UI/Additive` 当判据（§六 只作旁证引用一次）。
> ⛔ 没有把我们自己的 `WFUIBorderHighlight.shader` 当判据（只在 §四 做「差在哪」的对照）。

---

## 〇 一句话结论

**软边。** 它**不是** `step`，**也不是** `smoothstep` —— 是一条 **`saturate`（`clamp(x,0,1)`）的线性斜坡**：

```
color.a *= saturate( (_ClipRect.zw - _ClipRect.xy - abs(texCoord1.xy)) * texCoord1.zw ).x
         * saturate( … 同一式子… ).y
```

斜坡宽度写在**顶点段**（`texCoord1.zw`），等于 `4 × ( 0.25·_UIMaskSoftness + |pixelSize| )` 个对象空间单位。
我们现在的 `UnityGet2DClipping`（`step`×2 再相乘）是**硬边**（0/1 突变）—— 差的是**边界那一条过渡带的有无**，
不是「值差一点」。**`B114_W33` 的判断在【本 shader】上成立**（不是靠同族旁证推的，见 §二、§三）。

---

## 一 哪一段 DXBC 是 clip 变体（怎么定位的）

### 1·1 资产与结构

| 项 | 值 | 出处 |
|---|---|---|
| shader 名 | **`Everguild/UI/UI Border Highlight Appear Pulsating Scale`** | `Shader.m_ParsedForm.m_Name` |
| 资产 | `d:/2/新解包资源/assets_full/bundle_shaders_assets_all/Shader/Shader_-4463273156108355430.json` | —— |
| `compressedBlob` | **753,156 B**；`compressedLengths`/`decompressedLengths` 各 **2 个平台**：`[12524]→331,228` · `[35023]→421,928` | 本轮现读 |
| DXBC 段 | **128 段**（我按 `DXBC` 魔数 + 段内 `int32@+24` 长度自己切的，与 `工具/disasm_dxbc.py:split_dxbc` 同法） | 本轮现读 |
| 分段布局 | 每个 pass = **16 VS + 16 PS**；pass0 = `VS idx 0..15` / `PS idx 16..31`，pass1 = 32..63，pass2 = 64..95，pass3 = 96..127 | 段序实测（见下 1·3） |

### 1·2 关键字表（把 idx 翻成关键字组合）

`m_SubShaders[0].m_Passes[0].progFragment.m_PlayerSubPrograms[3]` 有 **16 条**，`m_KeywordIndices` 直接给出组合
（`m_KeywordNames[15]=_DEBUG` · `[16]=_AUTOROTATION` · `[17]=_FLIPDIRECTION` · **`[18]=UNITY_UI_CLIP_RECT`** · `[19]=UNITY_UI_ALPHACLIP`）。
**这 16 条的顺序 = 该 pass 的 16 段 PS 的顺序**（这条假设在 1·3 被内容自证），于是：

| DXBC idx | 关键字 | 段内有没有 `discard` | 段内有没有读 `cb0[131]` | 字节数 |
|---|---|---|---|---|
| 16,17,18 | `[]` / `_DEBUG` / `_DEBUG+_AUTOROTATION` | 否 | 否 | 2648 |
| 19 | `_AUTOROTATION,_FLIPDIRECTION` | 否 | 否 | 2656 |
| 20,21,22 | `UNITY_UI_ALPHACLIP`(+前两组) | **是** | 否 | 2772 |
| 23 | `_AUTOROTATION,_FLIPDIRECTION,UNITY_UI_ALPHACLIP` | **是** | 否 | 2780 |
| **24,25,26** | **`UNITY_UI_CLIP_RECT`**(+前两组) | 否 | **是** | **2844** |
| **27** | **`_AUTOROTATION,_FLIPDIRECTION,UNITY_UI_CLIP_RECT`** | 否 | **是** | 2852 |
| 28,29,30 | `UNITY_UI_CLIP_RECT,UNITY_UI_ALPHACLIP`(+前两组) | **是** | **是** | 2940 |
| 31 | `_AUTOROTATION,_FLIPDIRECTION,UNITY_UI_CLIP_RECT,UNITY_UI_ALPHACLIP` | **是** | **是** | 2948 |

**自证（不靠假设）**：`discard_nz` 只出现在 idx 20-23 与 28-31（正是带 `UNITY_UI_ALPHACLIP` 的那 8 条）；
`cb0[131]` 只出现在 idx 24-31（正是带 `UNITY_UI_CLIP_RECT` 的那 8 条）。两条独立特征与关键字表**逐条吻合**
⇒ 顺序假设成立，**纯 clip 变体 = idx 24/25/26**（`_FLIPDIRECTION` 版 = idx 27，只是 `v5.w` 取负，裁切段一模一样）。

> 另：**16 段 VS（idx 0-15）只有 3 个不同体**（`_DEBUG`/`_AUTOROTATION` 只改角度，**`UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP` 完全不影响 VS**）。
> 这很重要：说明 **`o3`（裁切插值器）是【无条件】算的**，无条件版也能读到它的公式。

### 1·3 独立复核（本轮自己跑的，不是拿别人的中间文件）

直接读 JSON → `lz4.block` 逐平台解压拼接 → 切 128 段 → `disasm_dxbc._d3d_disassemble()`（`d3dcompiler_47.dll`）
→ 段 16/24/27/28 段头分别是 `ps`，段 24 字节数 **2844**，与上表一致。

---

## 二 裁切算式的逐指令还原

### 2·1 片元段（PS idx 24，纯 `UNITY_UI_CLIP_RECT`，2844 B）—— 结尾 5 条就是全部裁切指令

```
add     r0.xy, -cb0[131].xyxx, cb0[131].zwzz   // r0.xy = _ClipRect.zw - _ClipRect.xy        = (宽, 高)
add     r0.xy, r0.xyxx, -|v3.xyxx|             // r0.xy = (宽, 高) - |texCoord1.xy|
mul_sat r0.xy, r0.xyxx, v3.zwzz                // r0.xy = saturate( r0.xy * texCoord1.zw )
mul     r0.x,  r0.y, r0.x                      // r0.x  = m.x * m.y
mul     o0.w,  r0.x, r0.w                      // color.a *= m.x * m.y
ret
```

- `v3` = PS 输入寄存器 3 = VS 的 `o3`（**INTERP2 / 第二个插值器**）。
  **输入签名的 Used 掩码是 `xyzw`**（不是只有 `xy`）—— 说明 `.zw` 真被读（见 2·2）。
- `mul_sat` = `saturate(a*b)` = `clamp(a*b, 0, 1)`（SM4 指令语义）。**没有** `step` / `ge` / `lt` / `smoothstep`。
- 这是四则里最后一段：它在 `o0.w` 上**再乘一次**，之前 `o0.w` 已经是 `alpha × reveal × 顶点 alpha`（B114_R5 §2·2 ⑧）。

**带 alphaclip 的那一档（idx 28/29/30）裁切段位置不同、式子完全相同**：

```
add     r1.xy, -cb0[131].xyxx, cb0[131].zwzz
add     r1.xy, r1.xyxx, -|v3.xyxx|
mul_sat r1.xy, r1.xyxx, v3.zwzz
mul     r0.w, r1.y, r1.x
mad     r1.x, r0.z, r0.w, l(-0.001)             // alphaAfterMask - 0.001
mul     r0.z, r0.w, r0.z
mov     o0.w, r0.z
lt      r0.z, r1.x, l(0.000000)
discard_nz r0.z
```

⇒ **两档的裁切算式逐字相同**；差别只是 alphaclip 档在拿到 `alphaAfterMask` 之后先判一次 `discard`（阈值 0.001），
而两色渐变（`_Color_offsets`）在它**之后**才算。

### 2·2 顶点段（VS idx 0，无条件输出 `o3`）—— 斜坡宽度就是在这里算出来的

```
// ── o3.zw：斜坡标度（= 1 / 边界宽度）
mul   r1.z, cb0[23].x, cb0[62].x                 // = _ScreenParams.x * P00
mul   r1.w, cb0[23].y, cb0[63].y                 // = _ScreenParams.y * P11
div   r1.xy, l(1.0,1.0), |r1.zwzz|               // = pixelSize = 1 / |P·_ScreenParams|
mad   r1.xy, cb0[132].xyxx, l(0.25), r1.xyxx     // = 0.25*_UIMaskSoftness + |pixelSize|
div   o3.zw, l(0.0,0.0,0.25,0.25), r1.xxxy      // o3.zw = 0.25 / ( 0.25*softness + |pixelSize| )

// ── o3.xy：以裁切矩形【中心】为原点的二倍距离
max   r1.xyzw, cb0[131].xyzw, l(-2e10,…)         // clamp(_ClipRect, -2e10, 2e10)
min   r1.xyzw, r1.xyzw,          l( 2e10,…)
mad   r1.xy, v0.xyxx, l(2.0,2.0), -r1.xyxx       // 2*positionOS.xy - clampedRect.xy
add   o3.xy, -r1.zwzz, r1.xyxx                   // … - clampedRect.zw
```

⇒ 合起来 `o3.xy = 2·positionOS − clampedRect.xy − clampedRect.zw`，即**「相对裁切矩形中心的 2 倍距离」**。
（`B114_R5` §65 把它记成「裁剪矩形插值器」—— **方向对**，更准确的说法是「中心相对二倍距离 + 斜坡标度」，两半都在 `o3` 里。）

### 2·3 算式「叫什么」（旁证：本地 Unity 自带的 ShaderGraph UI 目标源码，逐指令吻合）

`d:/4/Unity/MyGame/Library/PackageCache/com.unity.render-pipelines.universal@7865b6b91f8a/Editor/ShaderGraph/Includes/CanvasPass.hlsl`

- 顶点（`:84-88`）：
  ```hlsl
  float2 pixelSize = output.positionCS.w;
  pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
  float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
  output.texCoord1 = float4(input.positionOS.xy * 2 - clampedRect.xy - clampedRect.zw,
                            0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));
  ```
- 片元（`:189-193`）：
  ```hlsl
  #ifdef UNITY_UI_CLIP_RECT
      half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(unpacked.texCoord1.xy)) * unpacked.texCoord1.zw);
      color.a *= m.x * m.y;
  #endif
  ```

**逐条对得上**：`-2e10/+2e10` 那个字面量（`l(-20000000000.0)`）在 VS 里出现在**恰好是 `clamp(_ClipRect,…)` 的位置** ——
这是 `cb0[131] = _ClipRect` 的**决定性证据**（不是按名字猜的；RDEF 被剥，`_ClipRect` 本来就没名字）。
`cb0[132].xy` 出现在 `0.25/(0.25·x + |pixelSize|)` 的**唯一**位置 ⇒ `= _UIMaskSoftnessX/Y`（**推断**，见 §五·2）。

> ⚠️ 一处小出入：源码写 `pixelSize = output.positionCS.w`，DXBC 里是**字面量 `1.0`**（`div r1.xy, l(1.0,1.0), |…|`）。
> 正交 UI 投影下 `positionCS.w ≡ 1`，数值上等价；但说明当年编译的那份 `CanvasPass.hlsl` 与本地这份**不是同一版**。见 §五·1。

---

## 三 软边还是硬边（结论 + 证据）

### ✅ 结论：**软边（线性 `saturate` 斜坡），不是硬边**

**证据（三条，全部来自本 shader 自己的字节码）**

1. 裁切乘数是 `saturate((边距) × 标度)` —— **`mul_sat`** 一条指令，`mul_sat dst, a, b` 语义就是 `clamp(a*b, 0, 1)`。
   全段**没有**任何 `step` / `ge` / `lt` / `movc` 参与裁切（`lt` 只在 alphaclip 档用于 `discard`）。
2. 边界是**有限宽**的：`(宽 − 2|x−cx|)` 从 `>0` 走到 `<0` 的区间宽度 = `1/texCoord1.z ≠ 0`；
   这个宽度**由顶点段算出来并插值下来**（`o3.zw`），不是常数 0。
3. 对照：**真正的硬边**长什么样 —— 本机 `UnityGet2DClipping`
   （`D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/CGIncludes/UnityUI.cginc:4-8`）：
   ```hlsl
   inline float UnityGet2DClipping (in float2 position, in float4 clipRect)
   {
       float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
       return inside.x * inside.y;
   }
   ```
   **两条 `step`** —— 0/1 突变、无过渡带。两者在字节码层面是**不同形状**，不是参数差异。

**「软」到什么程度**（不是 `smoothstep` 那种 S 曲线，是**直线斜坡**）：

```
mask(x) = clamp( (W_rect − 2·|x − c|) · k , 0, 1 ),    k = texCoord1.z = 0.25 / ( 0.25·S + |p| )
⇒ 过渡带总宽（对象空间单位） = 1/k = 4·( 0.25·S + |p| ) = S + 4|p|
```

`S = _UIMaskSoftnessX/Y`（ShaderGraph 默认 **1.0**，见 §五·2）；`|p|` = 「半个屏幕像素折成对象单位」
（Overlay + canvas scale 1 时 `|p| = 0.5`）。⇒ **默认配置下过渡带 ≈ 3 个对象单位 ≈ 3 px**，随画布缩放线性变化。

> ⚠️ 数值 3 px 是「套源码 + 默认属性值」**推**出来的，不是从原版实拍量的 —— 标为**推断**，本任务只要求定性。

---

## 四 与我们现在 `UnityGet2DClipping` 的差

我们现在（`d:/4/Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader:258-259`）：

```hlsl
#ifdef UNITY_UI_CLIP_RECT
    o.a *= UnityGet2DClipping(i.worldXY, _ClipRect);
#endif
```

| 项 | 原版（本 shader 的 clip 变体） | 我们 | 差 |
|---|---|---|---|
| 判定形状 | `saturate((边距)·k)` ×2 相乘 —— **线性斜坡** | `step` ×2 相乘 —— **0/1 突变** | 🔴 **边界少了一条 3 px 的过渡带**。凡是「斜着画/缩放后的」图元，原版边缘有抗锯齿、我们的边缘是阶梯（且随画布缩放更明显） |
| 参照位置 | **`positionOS.xy`**（网格/对象空间，`v0.xy`） | **`mul(unity_ObjectToWorld, v.vertex).xy`**（**世界**空间） | 🔴 **空间不一样**。只要画布 `object→world` 不是恒等（Overlay 下 `CanvasScaler` 一定有缩放/平移），两者**根本不等价**。见 §五·4 |
| 边界宽度从哪来 | 顶点段算 `o3.zw = 0.25/(0.25·_UIMaskSoftness + |pixelSize|)`，**插值下来** | **没有这个量** | 🔴 我们缺 `_UIMaskSoftnessX/Y` 两个属性与整条 `pixelSize` 链 |
| 裁切矩形读取 | `_ClipRect`（`$Globals` cb0[131]）；VS 里先 `clamp(±2e10)` 再送插值；PS 里**不再 clamp** | `_ClipRect`（自己的材质属性）；每像素直接读 | 等价（插值 vs 直读在几何上同结果，原版那样做是为了省 uniform 带宽） |
| alphaclip | `discard` 判的是 **`alpha×reveal×顶点α×mask`**，阈值 `0.001` | `clip(o.a - 0.001)`，`o.a` 同样是 `alpha×reveal×顶点α×mask` | ✅ 等价 |
| 生效条件 | 只有图元挂在 `RectMask2D`/`Mask` 下时 Unity 才开这个关键字并喂 `_ClipRect` | 本工程**没人**设 `_ClipRect` / 没人开关键字 ⇒ **惰性**（脚本注释已如实记） | 两边今天都不跑，但**复刻要照形状补对** |

> 我们的注释 `WFUIBorderHighlight.shader:139` 写「`UnityGet2DClipping`：本机 Unity 6000.3.23f1 实测是**硬边 step**（无 softness 项）」——
> **这句话本身是对的**（`UnityUI.cginc:4-8` 确实是 `step`），但它**不能**推出「原版也是硬边」。
> 本轮判据显示：**原版用的不是 `UnityGet2DClipping`，是 ShaderGraph UI 目标那条 `saturate` 软边**。

---

## 五 没查清的部分（逐条点名，⛔ 不填猜测）

1. **`pixelSize` 的 `w` 被折成了字面量 `1.0`。**
   本地 `CanvasPass.hlsl:84` 是 `pixelSize = output.positionCS.w`，DXBC 是 `div r1.xy, l(1.0,1.0), |…|`。
   正交 UI 下 `w ≡ 1` ⇒ **数值上无差**，但说明当年编译用的 `CanvasPass.hlsl` 版本与本地这份不同。
   我**没有**去把 6000.2.6f2 那一版的 URP 包找出来比对（本地只有 6000.3.23f1 的包）。**不当作已查清**。
2. **`cb0[23]` = `_ScreenParams`、`cb0[62]/[63]` = 投影 2×2 的对角线 —— 推断，不是按名绑定。**
   依据：`cb0[23].x·cb0[62].x` / `cb0[23].y·cb0[63].y` 的形状正好对应
   `mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy)`（列主序 ⇒ `m[0].x=P00`、`m[1].y=P11`）。
   RDEF 被剥 + serde 里这几个槽位没有名字（`progVertex.m_CommonParameters` 只按名绑定了
   `unity_ObjectToWorld` / `unity_WorldToObject` / `_StartOffset`）⇒ **本机拿不到名字作证**。
3. **`cb0[132].xy` = `_UIMaskSoftnessX/Y` —— 同样是推断**（依据是它在 `0.25/(0.25·x+|p|)` 的唯一位置，与源码逐字同形）。
   **它的运行时值**：ShaderGraph Canvas 目标里两条属性的默认值 `value = 1.0f`
   （`…/com.unity.shadergraph@b4b75cc00891/Editor/Generation/Targets/Canvas/CanvasProperties.cs:82,93`），
   而 `d:/2/新解包资源/assets_full` 全库 `grep UIMaskSoftness` = **0 命中**（5 份材质都没覆写）⇒ 运行期应取默认 1.0。
   ❓ 但**谁在运行时给这两个槽位赋值**没查到：本地 `com.unity.ugui` 的 C# 里 `grep _UIMaskSoftness` = **0 命中**
   （只有 ShaderGraph 那侧引用它）。也就是说它到底是「材质常量」还是「Unity native 逐帧喂的」，**本轮没定案**。
4. **`_ClipRect` 到底是【对象空间】还是【画布空间】，本地定不了。**
   §四 我按「原版用 `positionOS`」下的结论（这是字节码**确定**的）；
   但 `RectMask2D` 把 `Clipping.FindCullAndClipWorldRect()` 的结果交给 `CanvasRenderer.EnableRectClipping()` 时用的是**画布空间**
   （`RectMask2D.cs:226,281` 的变量名 `m_LastClipRectCanvasSpace`、`Clipping.cs:25` 取 `canvasRect`），
   而 shader 侧比的是**网格对象空间** ⇒ 中间必然有一步 native 换算，**那一步不在本地源码里**，我读不到。
   ⇒ 结论仍成立（「原版的参照量是 `positionOS`」），但**「`_ClipRect` 是什么空间」这一半没查清**。
5. **`_FLIPDIRECTION` 版（idx 27）我只逐段确认了「裁切段与 idx 24 逐字相同」**，没再展开它前面那段（那与裁切无关，B114_R5 已覆盖）。

---

## 六 顺手发现（⛔ 一条都没动手改）

1. 🔴 **`WFUIBorderHighlight.shader` 的 clip 两路与原版不等价**（不只是软/硬）：
   - 软边 vs 硬边（§三）；
   - **`i.worldXY`（世界）vs 原版 `positionOS`（对象）** —— 这条更隐蔽：即使把软边公式抄对，空间不对照样错。
   落点：`Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader:154,181,259`。
2. 🟠 **我们缺 `_UIMaskSoftnessX` / `_UIMaskSoftnessY` 两个属性**（原版 shader 的属性表里有，见 §五·3）；
   要照抄软边，这两个得一起补，否则斜坡宽度会退化成「4|pixelSize|」（≈2 px，且不能由材质调）。
3. 🟠 **`B114_R5` §1·1 的 CB0 表可以补两格**：`cb0[131] = _ClipRect`（本轮用 `±2e10` 字面量坐实）、
   `cb0[132].xy = _UIMaskSoftnessX/Y`（推断）。R5 §三·❓6 说「本轮没深挖它叫什么」—— **本文件就是那一挖**。
4. 🟠 **`o3` 的准确语义**：`o3.xy = 2·positionOS − clampedRect.xy − clampedRect.zw`（中心相对二倍距离）、
   `o3.zw = 0.25/(0.25·softness + |pixelSize|)`（斜坡标度）。R5 §65 记的「裁剪矩形插值器」方向对但不够精确，
   将来若有人按「它就是矩形」去用，会算错（比如误以为是 `min/max` 四分量）。
5. ⚪ **同族 `UI Border Highlight Pulse Alpha` 的 Ruri HLSL 是同一式子**（**旁证，不作判据**）：
   `d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_UI Border Highlight Pulse Alpha/Sub0_Pass0_Fragment_b9.hlsl:118-124`
   ```hlsl
   _289 = (_ClipRect.z) - (_ClipRect.x);
   _290 = (_ClipRect.w) - (_ClipRect.y);
   _297 = _289 - abs(INTERP_2.x);
   _298 = _290 - abs(INTERP_2.y);
   _303 = _297 * INTERP_2.z;
   _304 = _298 * INTERP_2.w;
   _307 = clamp(_304,0,1) * clamp(_303,0,1);
   SV_Target.w = _307 * _262;
   ```
   —— 与我们的字节码 `mul_sat` ×2 + 相乘**同一个式子**（顺序不同、数学等价）。**这只能说明「同族一致」，本轮结论不依赖它。**
6. ⚪ **判「哪一段是哪个变体」有一个便宜的自证法**（本轮用的）：`discard_nz` 有/无 ↔ alphaclip 关键字、
   `cb0[131]` 读/不读 ↔ clip 关键字。两组特征与 `m_KeywordIndices` 交叉验证，**不用逐段读全**。

---

## 七 出处（可复查坐标）

| 件 | 路径 |
|---|---|
| 原版 shader（JSON） | `d:/2/新解包资源/assets_full/bundle_shaders_assets_all/Shader/Shader_-4463273156108355430.json` |
| 关键字表 / 子程序表 | 同上 · `m_ParsedForm.m_KeywordNames` / `m_SubShaders[0].m_Passes[0].progFragment.m_PlayerSubPrograms[3]` |
| clip 变体字节码 | 同 blob 的 **DXBC 段 idx 24**（2844 B）；`_FLIPDIRECTION` 版 idx 27（2852 B）；`+ALPHACLIP` 版 idx 28（2940 B） |
| 算式源码旁证 | `d:/4/Unity/MyGame/Library/PackageCache/com.unity.render-pipelines.universal@7865b6b91f8a/Editor/ShaderGraph/Includes/CanvasPass.hlsl:84-88, 189-193` |
| 软边属性默认值 | `…/com.unity.shadergraph@b4b75cc00891/Editor/Generation/Targets/Canvas/CanvasProperties.cs:76-96` |
| 硬边对照 | `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/CGIncludes/UnityUI.cginc:4-8` |
| 我们这一侧 | `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader:139,154,181,258-259` |
| 反汇编工具 | `d:/4/Unity/工具/disasm_dxbc.py`（`_d3d_disassemble` / `split_dxbc`）· `d:/4/Unity/工具/dump_shader_blob.py`（`blob_of`） |
