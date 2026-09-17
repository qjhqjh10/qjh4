# 替代 shader 丢掉的源属性（2026-09-17 全量重导实测）

> 数据来源：`EffectExporter.Run` 全量重导的日志 —— **`_tmp_view/` 是 gitignore 的，会被清空**，
> 所以结论落在这里。生成本文件的工具：`Unity/工具/_extract_dropped_props.py`（可复跑）。
> 产出它的代码：`EffectExporter.ImportMaterial` 里那段「源上有真值、目标 shader 却不认」的警告
> —— **2026-09-17 新加的**；在此之前这条路是**静默**的（裸 `continue`），所以这份账以前根本不存在。

## 一句话

这份清单是「**我们自建的替代 shader 比原版少了哪些属性**」的第一份有据可查的账。
**不是新 bug** —— 是一直流失、只是以前一声不吭的**还原度缺口**。修不修、怎么修要单独拍板：
每个属性都要先回原版 shader 的属性表（`资料/普查产出_0917/shader属性表_汇总.md`）确认语义，
**别按属性名猜**（本项目在「token 名 ≠ 语义」上踩过）。

## 汇总（按目标 shader）

| 目标 shader | 受影响材质 | 丢掉的属性条数 | 丢得最多的属性 |
|---|---:|---:|---|
| `WarpforgeVFX/Particles/Extra Color` | 100 | 202 | _SecondaryTex×33, _Color2×19, _Add_Color×18, _Disolve×18, _Color1×14, _Color3×14 |
| `Universal Render Pipeline/Particles/Unlit` | 32 | 120 | _Noise×18, _Color1×10, _Color2×10, _RimColor×6, _Noise_Color1×6, _Noise_Color2×6 |
| `WarpforgeVFX/Matcap/Matcap` | 25 | 87 | _BorderColor1×25, _BorderColor2×25, _EmissionColor×24, _Noise×9, _EmissionTex×4 |
| `Universal Render Pipeline/Unlit` | 15 | 51 | _MatCap×9, _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D×4, _BorderColor2×3, _EmissionColor×2, _Noise×2, _BorderColor1×2 |

## 属性频次（全局，前 40）

| 属性名 | 次数 | 像什么 |
|---|---:|---|
| `_SecondaryTex` | 35 | 第二张图（叠加层） |
| `_Noise` | 29 | 噪声图 |
| `_Color2` | 29 | 渐变色 2 |
| `_BorderColor2` | 28 | 边框色 2 |
| `_BorderColor1` | 27 | 边框色 1 |
| `_EmissionColor` | 26 | 发光颜色（**WFMatcap 没有这个属性 ⇒ 24 个材质的发光被丢**） |
| `_Color1` | 24 | 渐变色 1 |
| `_Add_Color` | 18 | 附加色 |
| `_Disolve` | 18 | 溶解遮罩图 |
| `_Color3` | 14 | 渐变色 3 |
| `_Color4` | 14 |  |
| `Texture2D_0bfdc50de139497fa85c0cd08848879a` | 13 |  |
| `Color_a142b0353b4945a49e1990789c04ea35` | 13 |  |
| `_MatCap` | 9 | MatCap 贴图 |
| `_Blend_Color` | 9 |  |
| `_NoiseTex1` | 9 | 噪声图 |
| `_NoiseTex2` | 9 | 噪声图 |
| `_RimColor` | 6 | 边缘光颜色 |
| `_Noise_Color1` | 6 |  |
| `_Noise_Color2` | 6 |  |
| `_Interior_SpreadColor` | 6 |  |
| `_ExtraSine` | 6 | 附加正弦纹理 |
| `_DisolveBorderColor` | 6 |  |
| `_EmissionTex` | 5 | 发光图 |
| `_Color01` | 5 |  |
| `_Color02` | 5 |  |
| `_MainTexture` | 5 |  |
| `_var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D` | 4 |  |
| `_TintColor` | 4 |  |
| `_FireNoise` | 4 | 火焰噪声图 |
| `_Warm` | 4 |  |
| `_Hot` | 4 |  |
| `_Noise_Combined` | 3 |  |
| `_Dissolve_Border_Color` | 3 |  |
| `_Dissolve_Inner_Color` | 3 |  |
| `_InnerColor` | 2 |  |
| `_FresnelColor` | 2 |  |
| `_NoiseTex` | 2 |  |
| `_EnvironmentColor` | 2 |  |
| `_MinTex` | 2 | 遮罩图 |

## 逐材质明细（172 条）

| 目标 shader | 材质 | 丢掉条数 | 具体属性 |
|---|---|---:|---|
| `Universal Render Pipeline/Particles/Unlit` | Aeldari death trail | 3 | _Noise=Noise Combined / _Color1=RGBA(16.060, 0.000, 0.151, 1.000) / _Color2=RGBA(0.000, 17.148, 6.878, 0.604) |
| `Universal Render Pipeline/Particles/Unlit` | Card board Frame SDF | 3 | _ShadowColor=RGBA(0.000, 0.000, 0.000, 0.000) / _Outline=RGBA(1.000, 1.000, 1.000, 0.447) / _Noise=Noise Combined |
| `Universal Render Pipeline/Particles/Unlit` | EC Shield VFX | 7 | _RimColor=RGBA(0.831, 0.675, 0.886, 1.000) / _Noise_Color1=RGBA(0.001, 0.000, 0.001, 0.102) / _Noise_Color2=RGBA(2.314, 0.742, 4.167, 1.000) / _Noise=Noise Combined / _Interior_SpreadColor=RGBA(2.082, 0.638, 1.899, 1.000) / _ExtraSine=HologramLines Generic Popup / _DisolveBorderColor=RGBA(17.721, 11.730, 19.580, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Edge Glow Shader Axe | 7 | _RimColor=RGBA(0.538, 1.469, 2.000, 1.000) / _Noise_Color1=RGBA(0.029, 0.090, 0.151, 0.000) / _Noise_Color2=RGBA(0.335, 0.776, 1.000, 1.000) / _Noise=Noise Combined / _Interior_SpreadColor=RGBA(1.000, 0.000, 0.000, 1.000) / _ExtraSine=Shine Noisy / _DisolveBorderColor=RGBA(0.090, 0.635, 1.000, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Edge Glow Shader Sword | 2 | _Base_color=RGBA(0.753, 0.898, 1.000, 0.000) / _Emission_Color=RGBA(1.000, 0.869, 0.588, 0.808) |
| `Universal Render Pipeline/Particles/Unlit` | FX Shine For Animation | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(1.000, 1.000, 1.000, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | FX Shine Noise | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine Noisy / Color_a142b0353b4945a49e1990789c04ea35=RGBA(1.000, 1.000, 1.000, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Glow Material | 2 | _Base_color=RGBA(1.000, 0.346, 0.316, 0.000) / _Emission_Color=RGBA(1.000, 0.334, 0.000, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | MarkerLight Blue | 3 | _SecondaryTex=Glow UI W40K / _DistortMap=Explosion Distort / _Ring_Color=RGBA(0.000, 1.832, 4.595, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | MarkerLight Orange | 3 | _SecondaryTex=Glow UI W40K / _DistortMap=Explosion Distort / _Ring_Color=RGBA(11.182, 1.401, 0.000, 1.000) |
| `Universal Render Pipeline/Particles/Unlit` | Muzzle Flash | 1 | _EmissiveColor=RGBA(11.182, 9.425, 5.445, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Necron Shield VFX | 7 | _RimColor=RGBA(0.675, 0.857, 0.886, 1.000) / _Noise_Color1=RGBA(0.280, 0.576, 0.271, 0.000) / _Noise_Color2=RGBA(0.742, 4.167, 1.470, 1.000) / _Noise=Tiled Hexagon Inverted / _Interior_SpreadColor=RGBA(0.029, 0.613, 0.020, 1.000) / _ExtraSine=Shine Noisy / _DisolveBorderColor=RGBA(0.703, 3.557, 7.464, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Necron Skull | 2 | _Color_2=RGBA(0.205, 1.304, 0.000, 0.765) / _Noise=Noise Combined |
| `Universal Render Pipeline/Particles/Unlit` | Necron death trail | 3 | _Noise=Noise Combined / _Color1=RGBA(2.255, 6.185, 12.260, 1.000) / _Color2=RGBA(6.321, 17.148, 0.000, 0.604) |
| `Universal Render Pipeline/Particles/Unlit` | Necrons Flying Monolyth Rays | 3 | _MinTex=Noise Combined / _Bottom_Color=RGBA(0.043, 0.529, 0.193, 1.000) / _TopColor=RGBA(4.595, 2.973, 0.000, 1.000) |
| `Universal Render Pipeline/Particles/Unlit` | Rock Spike | 3 | _FireNoise=Noise Combined / _Warm=RGBA(5.278, 0.100, 0.000, 1.000) / _Hot=RGBA(2.639, 2.111, 0.166, 1.000) |
| `Universal Render Pipeline/Particles/Unlit` | Rock Spike Dissolve | 5 | _FireNoise=Noise Combined / _Warm=RGBA(5.278, 0.100, 0.000, 1.000) / _Hot=RGBA(2.639, 2.114, 0.166, 1.000) / _Dissolve_Border_Color=RGBA(0.961, 0.019, 0.000, 0.000) / _Dissolve_Inner_Color=RGBA(2.462, 0.995, 0.000, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Shield VFX | 7 | _RimColor=RGBA(0.129, 0.352, 0.988, 1.000) / _Noise_Color1=RGBA(0.660, 0.000, 0.440, 0.000) / _Noise_Color2=RGBA(0.153, 9.734, 9.410, 1.000) / _Noise=Noise Combined / _Interior_SpreadColor=RGBA(0.306, 0.000, 1.000, 1.000) / _ExtraSine=Shine Noisy / _DisolveBorderColor=RGBA(0.090, 0.635, 1.000, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Sororitas Pray | 3 | _MinTex=Noise Combined / _Bottom_Color=RGBA(1.000, 0.668, 0.420, 1.000) / _TopColor=RGBA(2.271, 1.469, 0.000, 1.000) |
| `Universal Render Pipeline/Particles/Unlit` | Sororitas Shield VFX | 7 | _RimColor=RGBA(0.886, 0.810, 0.675, 1.000) / _Noise_Color1=RGBA(0.576, 0.523, 0.271, 0.102) / _Noise_Color2=RGBA(4.167, 2.605, 0.742, 1.000) / _Noise=Noise Combined / _Interior_SpreadColor=RGBA(1.560, 0.717, 0.397, 1.000) / _ExtraSine=Shine Noisy / _DisolveBorderColor=RGBA(7.464, 5.790, 0.703, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Spiral Trail FX Smoke | 3 | _Noise=Noise Combined / _Color2=RGBA(0.500, 0.500, 0.500, 1.000) / _Color1=RGBA(0.698, 0.698, 0.698, 1.000) |
| `Universal Render Pipeline/Particles/Unlit` | TAU Shield VFX | 7 | _RimColor=RGBA(0.675, 0.857, 0.886, 1.000) / _Noise_Color1=RGBA(0.271, 0.474, 0.576, 0.000) / _Noise_Color2=RGBA(0.727, 3.936, 4.167, 1.000) / _Noise=Tiled Hexagon Inverted / _Interior_SpreadColor=RGBA(0.227, 0.405, 0.698, 1.000) / _ExtraSine=Shine Noisy / _DisolveBorderColor=RGBA(0.703, 3.557, 7.464, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Tendril Dissolve | 5 | _FireNoise=Noise Combined / _Warm=RGBA(0.724, 0.457, 1.491, 1.000) / _Hot=RGBA(1.224, 0.632, 2.000, 1.000) / _Dissolve_Border_Color=RGBA(0.026, 0.000, 0.961, 0.000) / _Dissolve_Inner_Color=RGBA(6.575, 1.768, 6.544, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Tendril Thorns Dissolve | 5 | _FireNoise=Noise Combined / _Warm=RGBA(0.000, 0.000, 0.000, 1.000) / _Hot=RGBA(0.817, 0.000, 1.895, 1.000) / _Dissolve_Border_Color=RGBA(0.243, 0.000, 1.000, 0.000) / _Dissolve_Inner_Color=RGBA(6.575, 1.768, 6.544, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Vortex portal | 4 | _Main_Color=RGBA(0.316, 0.746, 0.943, 0.761) / _Secondary_Color=RGBA(0.000, 0.138, 0.839, 0.612) / _Reflected_Image=Aeldari Vortex Reflection / _Reflection_Color=RGBA(0.303, 0.334, 0.415, 0.000) |
| `Universal Render Pipeline/Particles/Unlit` | Wispy_Trail_Chaos Bullet | 3 | _Noise=Noise Combined / _Color1=RGBA(0.085, 0.000, 0.000, 1.000) / _Color2=RGBA(1.000, 0.029, 0.000, 0.388) |
| `Universal Render Pipeline/Particles/Unlit` | Wispy_Trail_Cut | 3 | _Noise=Noise Combined / _Color1=RGBA(3.531, 0.000, 0.491, 1.000) / _Color2=RGBA(2.000, 0.921, 1.870, 1.000) |
| `Universal Render Pipeline/Particles/Unlit` | Wispy_Trail_EC Bullet | 3 | _Noise=Noise Combined / _Color1=RGBA(1.000, 0.392, 0.924, 0.369) / _Color2=RGBA(4.759, 0.965, 3.510, 0.157) |
| `Universal Render Pipeline/Particles/Unlit` | Wispy_Trail_Nurgle Bullet | 3 | _Noise=Noise Combined / _Color1=RGBA(0.000, 0.000, 0.000, 1.000) / _Color2=RGBA(0.462, 0.632, 0.140, 1.000) |
| `Universal Render Pipeline/Particles/Unlit` | Wispy_Trail_Orbital | 3 | _Noise=Noise Combined / _Color1=RGBA(0.000, 0.137, 2.000, 1.000) / _Color2=RGBA(0.915, 1.648, 2.000, 1.000) |
| `Universal Render Pipeline/Particles/Unlit` | Wispy_Trail_Pyrovore | 3 | _Noise=Noise Combined / _Color1=RGBA(0.000, 0.000, 0.000, 1.000) / _Color2=RGBA(2.852, 1.682, 0.794, 1.000) |
| `Universal Render Pipeline/Particles/Unlit` | Wispy_Trail_Tyranid Bullet | 3 | _Noise=Noise Combined / _Color1=RGBA(0.000, 0.000, 0.000, 1.000) / _Color2=RGBA(1.255, 1.600, 0.110, 1.000) |
| `Universal Render Pipeline/Unlit` | Card 3d Lvl1 | 2 | _MatCap=MatCap Card Level 1 / _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D=Noise Combined |
| `Universal Render Pipeline/Unlit` | Card 3d Swarm Merge | 4 | _DisplacementNoise=Noise Blob / _WarpColor=RGBA(0.863, 0.062, 1.982, 0.000) / _MatCap=MatCap Polish / _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D=Noise Combined |
| `Universal Render Pipeline/Unlit` | Card 3d WH40K Explosion Green | 5 | _MatCap=MatCap Polish / _DissolveTex=Noise Combined / _BorderColor=RGBA(0.968, 3.031, 0.238, 0.000) / _BorderColor2=RGBA(0.000, 0.492, 0.576, 0.000) / _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D=Noise Combined |
| `Universal Render Pipeline/Unlit` | Card Shatter Inner Material | 2 | _EnvironmentColor=RGBA(1.000, 1.000, 1.000, 0.000) / _MatCap=MatCap Polish |
| `Universal Render Pipeline/Unlit` | Eclipse Tau | 4 | _MainColor=RGBA(1.000, 1.000, 1.000, 0.000) / _EclipseColor=RGBA(0.255, 0.138, 0.138, 0.000) / _EclipseColorBorder=RGBA(2.000, 0.737, 0.000, 0.000) / _ExtraAmbientColor=RGBA(1.000, 1.000, 1.000, 0.000) |
| `Universal Render Pipeline/Unlit` | GlassRefraction | 2 | _Color0=RGBA(1.000, 0.873, 0.873, 0.000) / _Normals=NoiseNRM |
| `Universal Render Pipeline/Unlit` | Mat_Fx_ParticleSet_apb | 1 | Texture2D_F593E37E=Tex_fx_particle_set |
| `Universal Render Pipeline/Unlit` | Mat_Fx_Rock | 1 | Texture2D_EDA87E5=Tex_fx_rock |
| `Universal Render Pipeline/Unlit` | Pragati-Regular White_thick Offset for 3D | 5 | _OutlineColor=RGBA(0.000, 0.000, 0.000, 1.000) / _ReflectFaceColor=RGBA(0.000, 0.000, 0.000, 1.000) / _ReflectOutlineColor=RGBA(0.000, 0.000, 0.000, 1.000) / _UnderlayColor=RGBA(0.000, 0.000, 0.000, 0.500) / _GlowColor=RGBA(1.000, 1.000, 1.000, 0.500) |
| `Universal Render Pipeline/Unlit` | Remnant Shatter | 5 | _EnvironmentColor=RGBA(1.000, 1.000, 1.000, 0.000) / _MatCap=Card base Matcap / _Shatter_Texture=Card Shatter / _Shatter_Color=RGBA(11.943, 22.364, 4.801, 1.000) / _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D=Noise Combined |
| `Universal Render Pipeline/Unlit` | Spirit Stone | 4 | _InnerColor=RGBA(0.015, 0.134, 0.092, 0.000) / _FresnelColor=RGBA(0.078, 0.580, 0.749, 0.000) / _NoiseTex=Noise Combined / _MatCap=Matcap Mod Spirit Stone |
| `Universal Render Pipeline/Unlit` | Spirit Stone Explosion | 4 | _InnerColor=RGBA(0.015, 0.134, 0.092, 0.000) / _FresnelColor=RGBA(0.078, 0.580, 0.749, 0.000) / _NoiseTex=Noise Combined / _MatCap=Matcap Mod Spirit Stone |
| `Universal Render Pipeline/Unlit` | SporeMine | 1 | _Matcap=Matcap Skin |
| `Universal Render Pipeline/Unlit` | Stikka_mat | 5 | _MatCap=MatCap Polish / _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `Universal Render Pipeline/Unlit` | Vanguard_Frame VAT | 6 | _MatCap=MatCap Polish / _EmissionTex=Vanguard_emission / _EmissionColor=RGBA(2.000, 1.028, 1.028, 1.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(4.595, 4.173, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | Border Glow Rectangle | 3 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | ChemVial_1 | 4 | _EmissionTex=ChemVial_Emission / _EmissionColor=RGBA(28.519, 0.135, 0.135, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | Choppa-mat | 2 | _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | Desolation_Missile_mat | 4 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | Desolation_Missile_mat_matcap | 3 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | DynamiteBundle | 3 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | GSC Dagger | 4 | _EmissionTex=GSC Dagger Emission / _EmissionColor=RGBA(0.614, 1.782, 0.638, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | Harpoon Mat | 4 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | IceSpike | 3 | _EmissionColor=RGBA(0.255, 0.629, 0.887, 1.000) / _BorderColor1=RGBA(0.000, 0.775, 1.000, 1.000) / _BorderColor2=RGBA(0.000, 0.302, 1.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | KrootJavelin_mat | 3 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | MatCap Metal Generic | 4 | _EmissionColor=RGBA(1.000, 1.000, 1.000, 0.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | Mutate_spikes-steel | 4 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | OrkSkin | 4 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 0.000) / _BorderColor2=RGBA(1.862, 4.887, 0.438, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | RockDebris | 3 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | RockDebris_asteroid | 3 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | RockDebris_blueish | 3 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | RockDebris_ground | 3 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | RockDebris_reddish | 3 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | RockSpike | 4 | _EmissionTex=lava_rock / _EmissionColor=RGBA(4.541, 1.284, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | RockSpike mild | 4 | _EmissionTex=lava_rock / _EmissionColor=RGBA(1.774, 0.995, 0.510, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | Squig1 | 4 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | Squig2 | 4 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | Squig_Hook_mat | 4 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | Tentacle matcap test | 4 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _Noise=Noise Combined / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Matcap/Matcap` | WaterRipples_Puddle | 3 | _EmissionColor=RGBA(0.000, 0.000, 0.000, 1.000) / _BorderColor1=RGBA(1.000, 0.000, 0.000, 1.000) / _BorderColor2=RGBA(1.000, 0.908, 0.000, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Alpha Masks Two Layer Variant Blood Twirl | 3 | _Blend_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _NoiseTex1=Noise Combined / _NoiseTex2=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Alpha Masks Two Layer Variant Lava Ground | 3 | _Blend_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _NoiseTex1=Glow Rays / _NoiseTex2=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Alpha Masks Two Layer Variant Rift Background | 3 | _Blend_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _NoiseTex1=Noise Combined / _NoiseTex2=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Alpha Masks Two Layer Variant Vortex Warhead | 3 | _Blend_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _NoiseTex1=Glow UI W40K / _NoiseTex2=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Alpha Masks Two Layer Variant Webway | 3 | _Blend_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _NoiseTex1=Nebula Content / _NoiseTex2=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Alpha Masks Two Layer Variant Webway 2 | 3 | _Blend_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _NoiseTex1=Noise Combined / _NoiseTex2=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Battle Arena 3 Texture Baked UV scroll Solar Storm | 1 | _SecondaryTex=Battle arena 1 Clouds |
| `WarpforgeVFX/Particles/Extra Color` | Camouflage_Icon | 1 | _SecondaryTex=Glow |
| `WarpforgeVFX/Particles/Extra Color` | Cruelty Particle Custom | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine Noisy / Color_a142b0353b4945a49e1990789c04ea35=RGBA(0.915, 0.401, 0.412, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | DCannon_Trail_MultiRay 1 | 4 | _Color1=RGBA(2.152, 2.853, 5.073, 0.502) / _Color2=RGBA(3.849, 7.341, 16.335, 1.000) / _Color3=RGBA(1.001, 1.774, 3.476, 1.000) / _Color4=RGBA(1.107, 0.455, 2.635, 0.455) |
| `WarpforgeVFX/Particles/Extra Color` | DigitalSymbols_3 | 1 | _SecondaryTex=Square_Glow_Icon |
| `WarpforgeVFX/Particles/Extra Color` | Doomweaver effect | 1 | _FadeInBorderColor=RGBA(18.090, 561.791, 766.996, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | EC_Whip_Lucius | 1 | _SecondaryTex=SmokeLoopAlpha |
| `WarpforgeVFX/Particles/Extra Color` | Ecstasy Particle Custom | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(0.940, 0.694, 1.000, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | Everguild_FX_Multi Ray | 4 | _Color1=RGBA(0.096, 0.915, 0.022, 1.000) / _Color2=RGBA(0.000, 1.000, 0.104, 0.314) / _Color3=RGBA(0.311, 0.753, 0.000, 0.992) / _Color4=RGBA(0.139, 0.660, 0.115, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Exile Glaive Dissolve | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Exile Glaive Glow | 1 | _SecondaryTex=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Explosion_Color | 1 | _Color2=RGBA(2.271, 1.700, 0.404, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Fast Particle Custom | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(0.585, 0.338, 0.268, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | Ferocity Particle Custom | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(1.000, 0.566, 0.533, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | FireBlack | 1 | _Color2=RGBA(0.219, 3.565, 0.219, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | FireRed | 1 | _Color2=RGBA(2.271, 1.700, 0.404, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Flame02 | 1 | _TintColor=RGBA(0.860, 0.860, 0.860, 0.500) |
| `WarpforgeVFX/Particles/Extra Color` | Flames Loop Green | 1 | _Color2=RGBA(0.587, 2.608, 0.478, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Flames Loop Red | 1 | _Color2=RGBA(2.271, 1.700, 0.404, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Flank Particle Custom | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(0.585, 0.338, 0.268, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | Gauss_Stream_MultiRay | 4 | _Color1=RGBA(33.009, 67.794, 29.815, 1.000) / _Color2=RGBA(10.792, 47.937, 14.808, 0.314) / _Color3=RGBA(0.090, 0.375, 0.122, 0.992) / _Color4=RGBA(0.552, 2.635, 0.455, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Gauss_Stream_Stylized | 4 | _Color1=RGBA(6.673, 36.274, 8.213, 1.000) / _Color2=RGBA(1.032, 2.119, 0.932, 1.000) / _Color3=RGBA(10.600, 39.278, 7.967, 1.000) / _Color4=RGBA(0.000, 0.000, 0.000, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | Gauss_Stream_Vespid | 4 | _Color1=RGBA(39.099, 67.794, 29.815, 1.000) / _Color2=RGBA(13.911, 47.937, 10.792, 0.314) / _Color3=RGBA(0.112, 0.376, 0.090, 0.992) / _Color4=RGBA(0.919, 2.635, 0.455, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites Ambush Variant | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites Create Sabotage | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites EC Elixir | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites EC Pink | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites GSC Card Variant | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites Long Range Variant | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites Multiply Edge | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites Orks Cardback | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites Penitence Variant | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites Pray Variant | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Generic Particle Dissolve For Sprites Tau Cardback | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=HexagonGradient |
| `WarpforgeVFX/Particles/Extra Color` | Generic Trait Particle Shine | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(1.000, 0.453, 0.000, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | Glow Layered Soft | 1 | _SecondaryTex=Glow UI W40K |
| `WarpforgeVFX/Particles/Extra Color` | Glow Space Dark Angels Asteroid Zone | 3 | _Blend_Color=RGBA(0.706, 0.184, 0.016, 1.000) / _NoiseTex1=Noise Combined / _NoiseTex2=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Glow Space Dark Angels Void Combat | 3 | _Blend_Color=RGBA(0.000, 1.055, 3.221, 0.000) / _NoiseTex1=Noise Combined / _NoiseTex2=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | GoopMist_blood | 1 | _TintColor=RGBA(0.345, 0.000, 0.000, 0.502) |
| `WarpforgeVFX/Particles/Extra Color` | Growth_1 | 1 | _SecondaryTex=Battle arena 1 Clouds |
| `WarpforgeVFX/Particles/Extra Color` | Growth_3 | 1 | _SecondaryTex=Growth_2_Icon |
| `WarpforgeVFX/Particles/Extra Color` | Helspear Dissolve | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Hexagon_companion | 1 | _SecondaryTex=Square_Glow_2_Icon |
| `WarpforgeVFX/Particles/Extra Color` | Hexagon_cylinder | 1 | _SecondaryTex=Border Thick Circle FX |
| `WarpforgeVFX/Particles/Extra Color` | Hexagon_tiled | 1 | _SecondaryTex=Glow Rays |
| `WarpforgeVFX/Particles/Extra Color` | Hexagon_tiled 2 | 1 | _SecondaryTex=Square_Glow_Icon |
| `WarpforgeVFX/Particles/Extra Color` | JainasMor | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Laser_Stream_MultiRay 1 | 4 | _Color1=RGBA(58.917, 15.622, 11.950, 1.000) / _Color2=RGBA(64.000, 0.000, 0.000, 0.314) / _Color3=RGBA(0.376, 0.090, 0.090, 0.992) / _Color4=RGBA(2.635, 0.455, 0.523, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Laser_Stream_MultiRay Mining | 4 | _Color1=RGBA(26.254, 13.667, 2.601, 1.000) / _Color2=RGBA(123.564, 62.275, 5.246, 0.808) / _Color3=RGBA(5.340, 2.752, 1.385, 0.992) / _Color4=RGBA(2.635, 1.481, 0.455, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Markerlight Square | 1 | _SecondaryTex=ScanLines |
| `WarpforgeVFX/Particles/Extra Color` | Markerlight Triangle | 1 | _SecondaryTex=ScanLines |
| `WarpforgeVFX/Particles/Extra Color` | Markerlight Triangle Sharp | 1 | _SecondaryTex=ScanLines |
| `WarpforgeVFX/Particles/Extra Color` | MegaBlasta_Multiray | 4 | _Color1=RGBA(90.510, 87.578, 5.550, 1.000) / _Color2=RGBA(54.340, 24.040, 14.610, 0.314) / _Color3=RGBA(0.755, 0.619, 0.146, 0.992) / _Color4=RGBA(3.518, 1.337, 0.149, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Mob Particle Custom | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(0.585, 0.338, 0.268, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | Necrons Remnant Up Glow | 1 | _SecondaryTex=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Oath Particle Custom | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(0.156, 0.573, 1.000, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | Particle_Whip_MultiRay | 4 | _Color1=RGBA(0.765, 7.310, 0.191, 1.000) / _Color2=RGBA(1.117, 5.038, 1.548, 0.314) / _Color3=RGBA(1.246, 3.012, 0.000, 0.992) / _Color4=RGBA(0.552, 2.635, 0.455, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | RiftCannon_Trail_MultiRay | 4 | _Color1=RGBA(3.736, 4.314, 6.092, 0.502) / _Color2=RGBA(27.033, 50.862, 52.739, 1.000) / _Color3=RGBA(5.464, 10.045, 11.067, 1.000) / _Color4=RGBA(4.708, 8.272, 6.507, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Rock Spike Dissolve Particle | 2 | _Add_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | SW_Runes_1_dissolve | 2 | _Add_Color=RGBA(0.560, 0.511, 0.407, 0.553) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | SW_Runes_1_dissolve_Large | 2 | _Add_Color=RGBA(0.711, 0.484, 0.226, 0.357) / _Disolve=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | SaimHann_Icon | 1 | _SecondaryTex=Glow |
| `WarpforgeVFX/Particles/Extra Color` | Sand Storm | 1 | _SecondaryTex=Sand Wind 2 |
| `WarpforgeVFX/Particles/Extra Color` | Sand Storm Intense | 1 | _SecondaryTex=Sand Wind 2 |
| `WarpforgeVFX/Particles/Extra Color` | Sentry Particle Custom | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(1.000, 0.792, 0.665, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | Shuriken Particle Custom | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(0.192, 0.616, 0.557, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Shuriken_Trail_MultiRay | 4 | _Color1=RGBA(2.012, 9.424, 15.796, 0.353) / _Color2=RGBA(4.699, 13.487, 39.849, 0.831) / _Color3=RGBA(0.703, 0.818, 1.000, 0.596) / _Color4=RGBA(0.911, 2.980, 5.271, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Slash_Roll | 1 | _SecondaryTex=Shine Noisy |
| `WarpforgeVFX/Particles/Extra Color` | Snow Storm | 1 | _SecondaryTex=Sand Wind 2 |
| `WarpforgeVFX/Particles/Extra Color` | Snow Storm Slow | 1 | _SecondaryTex=Sand Wind 2 |
| `WarpforgeVFX/Particles/Extra Color` | Sororitas_Flames_TwoLayer | 3 | _Blend_Color=RGBA(0.000, 0.000, 0.000, 0.000) / _NoiseTex1=Noise Combined / _NoiseTex2=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Stealth_Icon | 1 | _SecondaryTex=Glow |
| `WarpforgeVFX/Particles/Extra Color` | Stimulate Particle Custom | 2 | Texture2D_0bfdc50de139497fa85c0cd08848879a=Shine trail / Color_a142b0353b4945a49e1990789c04ea35=RGBA(0.695, 1.000, 0.901, 0.000) |
| `WarpforgeVFX/Particles/Extra Color` | Strike_Effect_Background | 1 | _SecondaryTex=Glow |
| `WarpforgeVFX/Particles/Extra Color` | Strike_Effect_Skull | 1 | _SecondaryTex=Glow |
| `WarpforgeVFX/Particles/Extra Color` | Strike_Effect_Sword | 1 | _SecondaryTex=Glow |
| `WarpforgeVFX/Particles/Extra Color` | Synapse_Icon | 1 | _SecondaryTex=Glow |
| `WarpforgeVFX/Particles/Extra Color` | Tau_Ion_Stream_MultiRay | 4 | _Color1=RGBA(29.815, 44.419, 67.794, 1.000) / _Color2=RGBA(10.792, 20.454, 47.937, 0.314) / _Color3=RGBA(0.090, 0.107, 0.376, 0.992) / _Color4=RGBA(0.150, 2.076, 2.888, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Teleport Trail | 4 | _Color1=RGBA(0.436, 0.902, 1.955, 1.000) / _Color2=RGBA(0.803, 1.439, 2.935, 1.000) / _Color3=RGBA(11.816, 50.215, 54.456, 1.000) / _Color4=RGBA(8.002, 0.000, 34.297, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Tesla_Stream_MultiRay | 4 | _Color1=RGBA(4.306, 29.446, 23.764, 1.000) / _Color2=RGBA(21.240, 54.251, 51.518, 0.463) / _Color3=RGBA(0.278, 0.546, 0.726, 0.992) / _Color4=RGBA(0.455, 2.635, 2.494, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | Toxic Pool Up light Burning | 1 | _SecondaryTex=Smoke Tile Texture |
| `WarpforgeVFX/Particles/Extra Color` | Toxic Pool Up light miasma | 1 | _SecondaryTex=Smoke Tile Texture |
| `WarpforgeVFX/Particles/Extra Color` | Toxic Pool Up light miasma 2 | 1 | _SecondaryTex=Smoke Tile Texture |
| `WarpforgeVFX/Particles/Extra Color` | Up Rays Additive | 1 | _SecondaryTex=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Up Rays Blend | 1 | _SecondaryTex=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Vulnerable_Icon | 1 | _SecondaryTex=Glow |
| `WarpforgeVFX/Particles/Extra Color` | Wispy_Trail_Deathspinner | 3 | _Color01=RGBA(0.466, 3.134, 4.238, 0.000) / _Color02=RGBA(1.266, 1.530, 4.649, 0.000) / _MainTexture=Trail_Custom_Continuous |
| `WarpforgeVFX/Particles/Extra Color` | Wispy_Trail_Dimensional_Breach | 4 | _Color01=RGBA(0.409, 2.015, 0.443, 0.000) / _Color02=RGBA(0.066, 0.066, 0.066, 0.000) / _MainTexture=Trail_Custom_Continuous / _Noise_Combined=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Wispy_Trail_Plasmancer | 4 | _Color01=RGBA(0.932, 8.476, 1.243, 0.000) / _Color02=RGBA(0.949, 2.000, 1.467, 1.000) / _MainTexture=Trail_Custom_Continuous / _Noise_Combined=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Wispy_Trail_Psychomancer | 4 | _Color01=RGBA(0.932, 8.476, 8.320, 0.000) / _Color02=RGBA(0.949, 2.000, 1.467, 1.000) / _MainTexture=Trail_Custom_Continuous / _Noise_Combined=Noise Combined |
| `WarpforgeVFX/Particles/Extra Color` | Wispy_Trail_Resurrection_Orb | 3 | _Color01=RGBA(0.932, 8.476, 1.243, 0.000) / _Color02=RGBA(1.412, 4.649, 1.266, 0.000) / _MainTexture=Trail_Custom_Continuous |
| `WarpforgeVFX/Particles/Extra Color` | dust_1_m | 1 | _TintColor=RGBA(0.701, 0.767, 0.830, 1.000) |
| `WarpforgeVFX/Particles/Extra Color` | fx_explosion | 1 | _TintColor=RGBA(0.500, 0.500, 0.500, 0.500) |
