# W12 特效：`WFUnlitAmbient` 三条（`Emissive Flickker` 整支 · 第二张图 · 手写 derivative 的 alpha-to-coverage）

> 执行代理 2026-10-12。改动文件 = `MyGame/Assets/WarpforgeVFX/Shaders/WFUnlitAmbient.shader`（LF）、
> `MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs`（CRLF）、`资料/特效还原_进度与交接.md`（CRLF，只改「还开着什么」那一条）。
> ⛔ 本轮**没跑 Unity**（铁律 13·3：子代理一律不许 `-executeMethod`）⇒ §三 的「改后读数」是**结构证明 + 待主对话跑**，不是测出来的。

## 一、三条各自的判据（原版真值 + 出处）

**先看「谁该有、谁不该有」**（`dump_shader_blob.blob_of()` 把**两份 shader 的 blob 分开**数 —— 按名字子串 `disasm_dxbc.py` 会把两份一起命中，分不开）：
`Everguild/UnlitAmbient Emissive Flickker` **32 段 · 4 段带第二张图 · 3 段有闪烁块 · 0 段有 derivative**；
`Everguild/UnlitAmbient` **336 段 · 0 段有闪烁块 · 12 段有 derivative** ⇒ **闪烁只在 Flickker 那份、A2C 只在基础那份。**
**① `Emissive Flickker`**（判据 `_tmp_view/dxbc/Everguild_UnlitAmbient_ps.txt` **段 0/1**，逐字）：
`LinearToSrgb(cb1[6]) × v2.xyz` → `div cb0[19].x / cb1[7].x` → 采 t0 取 `.x` → `k = n*(cb1[7].z-cb1[7].y)+cb1[7].y`
→ `o0.xyz = col + k·em`（**加法、最后一条**；在 `×_Color` 与阴影**之后**，**不吃雾**）。
**槽位铁证**（`progFragment.m_CommonParameters.m_ConstantBuffers`，本轮 UnityPy 实读；`slot = 字节偏移/16`）：
`_Color` off=80→cb1[5] · `_EmissiveColor` off=96→cb1[6] · `_FlickerSpeed` off=112(dim1)→**cb1[7].x** · `_FlickerMinMaxRange` off=116(dim2)→**cb1[7].yz**
⇒ 与指令里的槽号**逐一吻合**。⚠️ `_FlickerMinMaxRange` 是只用两分量的 Vector ⇒ **它的 `.x`=min、`.y`=max**。
`cb0[19].x` = `_TimeParameters.x`（`$Globals` 槽位表 → 正本 §十）。材质实值 `(0,1,0,0)` ⇒ k∈[0,1]。

**② 第二张图** = 与 ① **同一支**：`_SampleTexture2D_56151bd863ba4ebfae5e17e92f13cf49_Texture_1_Texture2D` 就是闪烁噪声图。
原版材质**确实赋了值**（`d:/2/解包整理/07_场景/battlearenasororitas/Material/Material_4.json` 的 `m_TexEnvs`；
导出侧 prefab 的 `texNames` 也有它）。⚠️ Flickker 里寄存器顺序与基础那份**相反**（**t0=噪声 · t1=`_BaseMap`**）——我们按**名字**取，不受影响。

**③ 手写 derivative 的 alpha-to-coverage** —— **不是原版作者手写的，是 Unity ShaderGraph 生成的 URP 函数**。
判据 = URP 自己的 `ShaderVariablesFunctions.hlsl:181-216`（`IsAlphaToMaskAvailable` / `SharpenAlphaStrict` / `AlphaClip`），
与原版段 23/24 **每条指令逐一吻合**：`saturate(((α-cut-0.5·fwidth(α))/max(fwidth,1e-4))+1)` ·
`clipVal=(a2c&&cut>0)?(a2cα-1e-4):(α-cut)` · `outAlpha=a2c?a2cα:α`（段 24 是这一版；段 23 的 else 支被折成 `l(1.0)`，另一支变体）。

## 二、改动清单（文件:行号 / 改前 → 改后）

| 文件:行 | 改前 | 改后 |
| --- | --- | --- |
| `Shaders/WFUnlitAmbient.shader:347-357` | 无（`_EmissiveColor/_FlickerSpeed/_FlickerMinMaxRange` 声明了但 HLSL 里**一个都没用**） | `if (_EmissiveFlickerOn>0.5){ t=_TimeParameters.x; nuv=float2(t,t)/max(_FlickerSpeed,1e-4); n=拉普图.x; k=lerp(min,max,n); col += k*LinearToSrgb(_EmissiveColor.rgb)*IN.color.rgb; }` |
| `…:238-239` | 第二张图只声明、**从没采样** | `TEXTURE2D`+`SAMPLER(sampler_SampleTexture2D_…)`（采样器名两种拼读法同串） |
| `…:365` | `#ifdef _ALPHATEST_ON clip(tex.a*_Color.a - _ClipThreshold); #endif` + `return half4(col, tex.a*_Color.a)` | `return half4(col, AlphaDiscard(tex.a*_Color.a, _ClipThreshold));`（`_ALPHATEST_ON` 由 `AlphaDiscard` **内部判**，不用再包 `#ifdef`） |
| `…:114 / :116` | `_EmissiveColor=(0,0,0,1)` · `_FlickerSpeed=1` | **原版声明值** `(2.113019,2.297397,0,0)` · `5`（`dump_shader.py` 实读） |
| `…:117-123 / :205 / :388` | 无 | 新闸 `_EmissiveFlickerOn`（Float，默认 0，`[HideInInspector]`；**两侧 `CBUFFER` 都加了**，字段同序以保 SRP Batcher 兼容） |
| `…:53-100 / :49-50 / :304-315` | 「还没做的」那一段 · 「我们这 12 个材质不是那一支」 | 换成 2026-10-12 的判据段；**就地订正**那句（烛光网格**有** ch3，见 §五①） |
| `Runtime/WarpforgeEffectBinder.cs:279-293` | 无 | `if (m.HasProperty("_EmissiveFlickerOn")) m.SetFloat(..., d.shader=="Everguild/UnlitAmbient Emissive Flickker" ? 1f : 0f);` |

🔑 **为什么用浮点闸、不用 keyword**（正本原话是「要隔离得自己加 keyword」）：原版这一支**没有对应关键字**
⇒ **没有任何材质数据能替 keyword 保活**，player 构建里可能被剥掉变体；浮点闸同样能单变量 A/B，且不增变体数。
`_FlickerMinMaxRange` 的默认值 `(0,1,0,0)` **原本就是对的**（2026-09-30 已修），本轮没动。

## 三、读数对照（每条：改前偏差 → 改后偏差；跑的是哪个工具、命令是什么）

**工具与命令（我没有跑，原样给主对话）**：
```bash
WFBIND_FORCE_BUILTIN=1 WFSWEEP_SIDE=exp Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run -logFile d:/4/_tmp_view/sweep_flicker.log
python -I -X utf8 d:/4/Unity/工具/gen_fallback_audit.py <白名单.tsv> <改前自建.tsv> <改后自建.tsv> <out.tsv>   # 单效果对拍：换成 EffectCompare + WFBIND_FORCE_BUILTIN="UnlitAmbient Emissive Flickker"
```
**改前**（出处 = `资料/比对基线/兜底路审计_1002j.tsv` 头注释 + 逐行；开工前已留快照 `资料/普查产出_第二十二会话/_W12_snapshot_基线20261012/`，`兜底路审计_1002j.tsv` md5 = `32a82fa4ce310dd1eba669eb13cd3ec0`）：
693 个可比效果 · **中位 0.0000 · 均值 0.0568 · >0.10 = 12 条 · >0.30 = 0 · >1.0 = 0**。
与本条相关的 7 行（都含基础 `UnlitAmbient`）：`Dark Angels Void Combat` 0.226 · `Ultramarines Aerial Clash` 0.091 ·
`Leviathan Blazing Biomatter` 0.030 · `Leviathan Sweeping Infestation` 0.023 · `Space Wolves Full Moon` 0.003 ·
`Space Wolves First Light` 0.003 · `Astra Militarum Dawn Attack` 0.001。
**改后 = 未测，但可证明逐位不变**（证明见 §四）⇒ **预期：上表每一行、以及全表三个阈值计数全部不动。**
🔴 **这不是「自检绿」，是「测不出来」** —— 唯一能走到闪烁那一支的材质 `_FlickerMinMaxRange=(0,0,0,0)` ⇒ `k≡0`。
⇒ **`EffectCompare` / 兜底路 A/B 对这三条【不是有效验证】**（改了等于没改，两边当然一样）。
要真验必须做**正控**：把那份材质的 `_FlickerMinMaxRange` 临时改成 `(0,1,0,0)`（只改导出侧数据、不动原版），
再 `WFBIND_FORCE_BUILTIN="UnlitAmbient Emissive Flickker"` 对拍 —— 那时**形状差 = 配对错、亮度差 = 算式错、品红 = 编译错**才适用。

## 四、有没有把别的效果改坏（怎么证的）

**证明（结构性的，不靠跑）**——改动只落在两条路径上：
1. **闪烁那一段只在 `_EmissiveFlickerOn > 0.5` 时执行**，而这个值由 `WarpforgeEffectBinder` **只**在
   `d.shader == "Everguild/UnlitAmbient Emissive Flickker"` 时置 1。导出的 958 个效果里**恰好 1 个** prefab 有这种材质
   （`grep -rl "UnlitAmbient Emissive Flickker" MyGame/Assets/WarpforgeVFX/Prefabs/` ⇒ `EnvironmentalCondition Sororitas Disrupted Ceremony`）
   ⇒ **另外 957 个效果一个字都进不到这一段**。那 1 个的材质实读 `_FlickerMinMaxRange={r:0,g:0,b:0,a:0}` ⇒ **k 恒为 0**
   （`lerp(0,0,n)`）⇒ 加进去的是**精确的 0**。
2. **`AlphaDiscard` 与原来那句 `clip` 恒等**：`_AlphaToMaskAvailable == 0`（未开 MSAA/alpha-to-mask）时
   `clipVal = α - cut`、`outAlpha = α`，与改前**逐位相同**；`_ALPHATEST_ON` 关着时它是**恒等**（返回入参）。
   9 个 `UnlitAmbient*` prefab 里 `grep -c "_ALPHATEST_ON"` **全为 0**（实读）⇒ 这 9 个材质走的都是恒等支。
3. **两个默认值改动可证无影响**：那份 Flickker 材质的 `floatNames`/`colorNames` 里**有** `_FlickerSpeed`、`_EmissiveColor` ⇒ 默认值不会被用到；其余材质根本进不了那一段。

⇒ 结论：**本轮读数应当【一条都不变】**。⚠️ 这是「改动不生效」而非「改动正确」的证明 —— 正确性靠 §一 的逐指令/逐槽位对齐。

## 五、顺手发现（只报不改）

1. 🔴 **我们的 OBJ 导出链把【顶点色】通道丢了，而原版网格有。** 实读 bundle：`Candles 213`
   `m_VertexData.m_Channels` = ch0(3)/ch1 法线(3)/**ch3 dim=4 颜色**/ch4 UV；而 `…/battlearenasororitas/Models/Candles 213.obj`
   **每个 `v` 只有 3 个 float** ⇒ 我们这份 shader 拿到的 `IN.color` 恒为 `(1,1,1,1)` ⇒ ① 里 `× IN.color.rgb` 乘的是白色。
   ⛔ 修它在 `工具/gen_arena_*.py` / 导出链（**另一条线**）。**同时订正**了 `WFUnlitAmbient.shader` 里
   「我们这 12 个材质不是带 `COLOR` 的那支 VS 变体」这句 —— **只对天空那一族成立**。
2. 🔴 **基础 `Everguild/UnlitAmbient` 的那句 `lerp(tex, v2, v2.w)` 在【带顶点色的网格】上不是空操作**
   （`v2` = COLOR 那支 VS 的 `mov o2.xyzw, v4.xyzw`），而我们写的是 `col = tex*_Color` ⇒ **对那种网格是取近似**。
   还没查清：那 8 个基础材质落在哪些网格上、那些网格颜色通道是什么。**判据半已齐**，但**不是「只差照做」** ⇒ 记成新账。
3. ⚠️ **战场清单只记 `_BaseMap`** ⇒ 战场那条路绑不上噪声图（`n`=1 ⇒ `k` 恒为 max = 「只亮不闪」）。正本已记，归 `gen_arena_*.py`。
4. ⚠️ `cb0[61]` **推出来是 `unity_FogColor`**（按 `$Globals` 槽位表逐条累加：`_RTHandleScale`=28 ✓ · `unity_AmbientSky`=56 ✓ ·
   `unity_MatrixV`=66-69 ✓ · `unity_MatrixVP`=78-81 ✓ · `unity_MatrixInvVP`=82-85 ✓ 三个矩阵锚点全中 ⇒ 61 落在 `unity_FogColor`）。
   **没坐实**，且只出现在 VS（`o2/o3.xyz`，配 `w=0`）——**对 PS 无影响**，本轮没用到它。
5. 🔴 **本条改动在当前 958 个效果上【观测不到】**（见 §三）——这既是④的证明，也是**本轮最大的操作结论**：
   **别拿「A/B 没动」当成「已验过」**。
6. ℹ️ `Runtime/ArenaParticleKeywords.cs` 在 `git status` 里是脏的（主对话 2026-10-12 加的 `WF_NOSOFT` 诊断开关）——**我没碰**（简报说它冻结）。
7. 🔴 **简报里的账号对不上**：简报写「账 `A1589`（`任务_卡组卡牌战斗.md` `## 四`）」，但那一行的 `A1589` 是
   **`battlearena1` 的红点/火区地面光池（要消融才能定）**，与 WFUnlitAmbient 无关。这三条真正的位置 =
   **`项目任务.md` §三 第 4 条 ②（`:237`）** + `资料/特效还原_进度与交接.md:1796`（该处已按本次结果改成 ✅）· **它没有 A 号**。
8. ℹ️ 那一支「死蜡烛」材质名叫 **`Sororitas Atlas 2 Candles dead`**（`_FlickerMinMaxRange=(0,0,0,0)`）——「活蜡烛」（战场 `Candles 9/21/23`）才是 `(0,1,0,0)`/`(0,0.8,0,0)`。

## 六、没做到 / 没查清

- **没跑 Unity ⇒ 没有「改后读数」。** 铁律 13·3 明令子代理不许 `-executeMethod`；命令已在 §三 给全。
  现场**真正的验收**（含正控）+ 「这几条读数该跑哪几条」按铁律 13·6 属**主对话在同步点**做。
- 已做但**只能**做静态验证：HLSL 两段新代码用本机 `d3dcompiler_47`（`D3DCompile`，ps_4_0 **与** ps_5_0、
  float 与 half 两版桩）**编过 0 错**（`/tmp/wf_w12/frag_check.hlsl` · `frag_half.hlsl`）；
  `TMPDIR=/tmp/wf_w12 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
  ⚠️ 这不是 Unity 的 shader 编译（URP 宏/变体没走）⇒ **URP 下能不能编过仍待一次 Unity 运行确认**。
- **没查清**：② 里那条「基础 shader + 带顶点色网格」的 lerp（§五②）· `cb0[61]` 的名字 ·
  战场那条路该不该也置 `_EmissiveFlickerOn`（该线已叫停，`ArenaOriginalMaterial` **我没碰**）。
- **没跑真 Play**（正本 §十四 末第 7 条本来就把 `_SOFT` 支/顶点色那支归真 Play）。
