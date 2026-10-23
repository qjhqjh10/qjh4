# 特效线 · UI 音波线（`AudioWaveEqualizer`）—— 执行报告（2026-10-23）

> 任务：把聊天框那条音波线从「自写近似 shader」换成**原版材质/shader 的逐项复刻**。
> 对应台账：`资料/任务线/特效线/任务总表.md` §一 `UI·音波线（AudioWaveEqualizer）`。
> 本件只动白名单内文件；`UnitChatPanel.cs` / `Battle/**` / `RuleEngine/**` / `Shell/**` 一个字没动。

---

## 一、结论

### 1·1 原版素材：**全部找到了，而且 shader 的数学拿到了**

| 件 | 出处 | 状态 |
|---|---|---|
| 材质（场景实际用的那份） | `bundle_battlesharedresources_assets_all/Material/Material_-3262226040299197450.json`（AssetRipper 里叫 `Equalizer_0.mat`） | ✅ 属性值逐项读出 |
| 材质（工厂原件，对照） | `d:/2/tools/assetripper_out/ExportedProject/Assets/Equalizer.mat` | ✅ |
| shader | `Shader Graphs/Equalizer`（`bundle_shaders_assets_all/Shader/Shader_2656665607827278157.json`） | ✅ |
| **shader 字节码** | 同上的 `Shader.compressedBlob` → 3 个 DXBC 容器（vs,vs,**ps**） | ✅ **没被剥**；`D3DDisassemble` 解出 **ps_4_0 37 条指令** |
| **cbuffer 布局** | 同一 shader 另一平台的 blob = Unity **GLSL 反射表**（明文参数名 + 字节偏移） | ✅ 见 §1·3 |
| 贴图 1（线图） | `40k_voicelines_radio_wave equalizer`（708×96），工程 `Resources/Art/ui/` 那份与 bundle 逐字节相同 | ✅ |
| 贴图 2（噪声） | `Noise Combined`（128×128，原版**线性**导入）；工程 `Resources/Art/card_sdf/cardback_noise.png` 与它**逐像素相同**且 `sRGBTexture: 0` | ✅ 直接用 |
| 场景实例参数 | `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4115.json` → `spectrumModifier=15` · `speedChange=10` | ✅ |
| 那格 Image | 同 bundle `MonoBehaviour_4509.json` → `m_Material`=上面那份材质 · `m_Sprite`=线图 · `m_Color`=**(0.6988, 0.9811, 0.9265, 1)** | ✅ |

**之前那条「原版 shader 的位移数学拿不到（无名字/无可读字节码）」是错的**：
没有的是 HLSL 源码，**编译字节码（DXBC）在 `compressedBlob` 里**（与 `资料/…/dump_shader_blob.py` 头部的记录一致）。
解出来的办法（可复跑）：`compressedLengths/decompressedLengths` 按平台切块 → LZ4 解压 → 拼出 DXBC 容器 →
`d3dcompiler_47.dll` 的 `D3DDisassemble` 反汇编（探针脚本留在 `d:/4/_tmp_view/wave_scratch/`，非仓库文件）。

### 1·2 原版 ps_4_0 的 37 条指令（逐条搬进 shader，指令号可对回反汇编）

```
 0: base = tex2D(_MainTex, uv)                                     ; sample_b（bias = _GlobalMipBias）
 1-4: n2 = tex2D(_Noise, uv*_Noise2ScaleAndSpeed.xy + frac(t*_Noise2ScaleAndSpeed.zw))   ; 只用 .r
 5: d  = _WaveScaleRemap.y - _WaveScaleRemap.x
 6-9: s1 = _MovementRange*(0.5*d*n2.r + _WaveScaleRemap.x - 20) + 20
10-14: tap1 = tex2D(_MainTex, (uv.x + _XOffsets.x, s1*(uv.y-0.5)+0.5))
15-18: n1 = tex2D(_Noise, uv*_Noise1ScaleAndSpeed.xy + frac(t*_Noise1ScaleAndSpeed.zw))   ; 只用 **.b**
19-22: s2 = _MovementRange*(0.5*d*n1.b + _WaveScaleRemap.x - 20) + 20
23-25: tap2 = tex2D(_MainTex, (uv.x + _XOffsets.y, s2*(uv.y-0.5)+0.5))
26-30: v = saturate(_MovementRange^4 * ((tap1.r + tap2.r) - base.b) + base.b)     ; mad_sat
31   : c = v * color
32-34: saturate(c.a) == 0 ⇒ discard
35   : o = c * color                     ← **顶点色乘两次**
```

### 1·3 🔴 关键坑：`_MovementRange` 到底落在哪个寄存器（这一条差点判错）

D3D 反汇编里只看到 `cb1[3].x/.y/.z`，光看指令会以为 `cb1[3]` **整个**是 `_WaveScaleRemap`、`_MovementRange` 没被用到。
**GLSL 反射表（明文）**给出了 `UnityPerMaterial`（10 vec4）里每个参数的**字节偏移**：

| 参数 | offset | 寄存器 | 分量 |
|---|---|---|---|
| `_XOffsets` | 0x10 | cb1[1] | 3 |
| **`_MovementRange`** | **0x30** | **cb1[3].x** | 1 |
| **`_WaveScaleRemap`** | **0x38** | **cb1[3].y / .z** | 2 |
| `_Noise2ScaleAndSpeed` | 0x40 | cb1[4] | 4 |
| `_Noise1ScaleAndSpeed` | 0x50 | cb1[5] | 4 |

⇒ `cb1[3].x` = **`_MovementRange`**（C# 每帧 SetFloat 的那个），`.y/.z` = `_WaveScaleRemap.x/.y`。
**两者共用一个 vec4** —— 只看 D3D 指令永远分不开，必须查这张偏移表。

### 1·4 贴图通道语义（这张线图，708×96）

| 通道 | 内容 | 用在哪 |
|---|---|---|
| **B** | 只有第 **45/46 两行** = 255，其余 0 | 指令 29/30 的 `base.b` ⇒ **静止态那条细线** |
| **R** | **等化器竖条**（高矮不一的竖条，基线 42–44 行） | 指令 26/29 的 `tap.r` ⇒ **有声时的波形** |
| A | 第 41–44 行 = 255 | ⛔ **原版一个 alpha 都没采**（旧近似拿它画线，是错的） |

**行为结论**：`_MovementRange = 0`（静音）⇒ `v = base.b` ⇒ 一条 2px 直线；
`→1` 时 `v = saturate(tap.r 的和)`，且 `m⁴` 让低音量几乎不可见 ⇒ 高音量才长出竖条。
（CPU 侧用真贴图跑了一遍同样的算式验证过：m=0/0.0059/0.3 都只有 1 行亮；m=0.6 略厚；m=0.9 出现明显竖条。）

### 1·5 驱动器（`AudioWaveEqualizer`）逐句对照

| 原版（`d:/2/tools/decomp_full/AudioWaveEqualizer__*.c`） | 我们的 `VoiceWaveDriver` |
|---|---|
| `.ctor`：`+0x28=10f` · `+0x2c=10f` · `+0x30 = new float[64]` | `spectrumModifier=10` · `speedChange=10` · `SpectrumLength=64` |
| `Initialize`：记来源 → **先 `enabled=false`** → `new Material(image.material)` → `image.material = 它` | `Initialize(AudioSource)` 同序（实例材质 + `enabled=false`） |
| `Toggle(bool)`：只 `enabled = 参数` | `Toggle(bool)` 同 |
| `Update`：`if (src == null) return;` → `GetSpectrumData(spectrum, 0, 2)` → **前 16 bin 求和** → `×spectrumModifier` → `clamp01` → `k=clamp01(dt*speedChange)` → `(target-prev)*k+prev` → `SetFloat("_MovementRange", …)` | `Tick(float)` **逐句同序**（`Window = FFTWindow.Hamming`，`Bins = 16`） |
| **不判 `isPlaying`** | 同样不判 |

---

## 二、改动清单

| 文件 | 说明 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Resources/VoiceWave.shader` | **整份重写**：原版 `Shader Graphs/Equalizer` 的逐条复刻（属性名 / pass 状态 / 37 条算式 / 顶点色乘两次 / discard）。保留 shader 名 `CardPresentation/VoiceWave`（`UnitChatPanel` 的 `Shader.Find` 不用改）。 |
| `Unity/MyGame/Assets/CardPresentation/Core/VoiceWaveDriver.cs`（+meta） | 新件：`class VoiceWaveDriver`（`namespace CardPresentation`）—— 原版 `AudioWaveEqualizer` 逐句复刻；另给探针用的 `Tick(dt)` / `SpectrumOverride` / `SpectrumModifier` / `SpeedChange`。 |
| `Unity/MyGame/Assets/CardPresentation/Core/VoiceWaveMaterial.cs`（+meta） | 新件：`static class VoiceWaveMaterial` —— 按原版 `Equalizer_0` 的 6 个属性值 + 线性噪声图建材质；`Create()` / `CreateFactoryPreset()` / `NoiseTexture()`。 |
| `Unity/MyGame/Assets/CardPresentation/Editor/VoiceWaveProbe.cs`（+meta） | 新件：探针 `VoiceWaveProbe.Run`（A 段驱动器 8 条 + B 段 shader 6 条，含灭自证）。 |

**没动**：`UnitChatPanel.cs`（战斗代理在改）· `Battle/**` · `RuleEngine/**` · `Shell/**` · `Editor/{IconSetup,CardBaseDemo,BattleScene,ShellScene,MainMenuScene}.cs` · `ArenaOriginalPrefabs/**` · 任何 `任务总表.md` / 正本 / git。

---

## 三、探针 / 断言

### 3·1 跑法（主对话跑；本代理跑不了 Unity 批处理）

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod VoiceWaveProbe.Run -logFile -
# 退出码 0 = 全过；日志前缀 `WAVEPROBE `
```

### 3·2 A 段（驱动器，纯 CPU，不需要 GPU）

| # | 断言 | 期望 |
|---|---|---|
| A0 | `Initialize` 建了**实例**材质、且组件被关 | `inst != mat` · `enabled == false` |
| A1 | 三态·**直线**（全 0 频谱） | `Movement == 0`，材质 `_MovementRange == 0` |
| A2 | 三态·**有音频** | 饱和到 `1` |
| A3 | 三态·**静音**（从 1 落下，dt=0.05·speed=10） | 一步 `0.5`；继续掉到 0 |
| A4 | **灭自证①**：第 16–63 bin 灌 1000 | 读数 `0.24`（**不是** 1.0，也**不是** 0.015） |
| A5 | **灭自证②**：16 bin ×15 = 240 | 读数 `1`（没 clamp 会是 240） |
| A6 | **灭自证③**：一步 k=0.25 | 读数 `0.25`（不去平滑会是 1） |
| A7 | 构造默认值 | `10 / 10 / 64 / 16 / Hamming(2) / "_MovementRange"` |
| A8 | `audioSource == null` 且无假频谱 | 早退，不抛、读数不动 |

### 3·3 B 段（shader，渲到 RT 读**alpha**——原版 `o.a = v*color.a²`，color.a=1 时 alpha 就是 v）

| # | 断言 | 期望 |
|---|---|---|
| B1 | `m=0` ⇒ alpha = `base.b`；换噪声 / `_XOffsets` / `_WaveScaleRemap` 都不许变 | `0.25` |
| B2 | `m=0.5`、R=1、B=0 ⇒ alpha = m⁴×2 | `0.125` |
| B3 | **反证** `m=0.25` ⇒ alpha | `0.0078`（若写成 m² 会是 `0.0625`） |
| B4 | v 缩放：带在 v∈[0.82,0.84]、m=1 ⇒ 亮带位置 | `v ≈ 0.5497`（缩放 = `6.635`；缩放 20 会落在 0.517） |
| B5 | 顶点色乘**两次**（顶点色 0.5、v=1） | RGB = `0.25`（乘一次会是 0.5） |
| B6 | 噪声通道不对称（tap1 用 `_Noise2.r`、tap2 用 `_Noise1.b`） | u=0.10 → `0.668`；u=0.05 → `0.834`；两者必须不同 |

> B 段全程用**白噪声 / 速度 0**，所以与 `_Time` 无关、结果确定。像素容差 2/255（B3 放 3/255）。

---

## 四、接线（给 `UnitChatPanel` 的两行 —— 主对话照抄）

现在那两行是（`UnitChatPanel.cs:544-550` 附近）：

```csharp
var waveSh = Shader.Find("CardPresentation/VoiceWave");
b.waveMat = new Material(waveSh);
b.waveMat.mainTexture = waveTex;
b.waveMat.SetColor("_Color", Color.white);
b.wave.SetMaterial(b.waveMat);
```

**换成**（属性值一次配齐，别只 `new Material` —— 那会拿到 shader 默认值，波形不是原版形状）：

```csharp
b.waveMat = CardPresentation.VoiceWaveMaterial.Create(waveTex, WaveTint);   // ← 原版 Equalizer_0 的 6 个属性 + 线性噪声
b.wave.SetMaterial(b.waveMat);
```

**驱动器**（原版 `VoiceLineBox.SetUpAudio` 那两行；`UnitChatPanel` 里建气泡处或 `ShowChat` 时）：

```csharp
var drv = b.wave.gameObject.AddComponent<CardPresentation.VoiceWaveDriver>();
drv.TargetRenderer = b.wave.GetComponent<MeshRenderer>();   // 或 drv.TargetGraphic = <uGUI Image>
drv.SpectrumModifier = 15f; drv.SpeedChange = 10f;          // 原版场景实例值
drv.Initialize(audioSource);   drv.Toggle(true);
```

并把 `Advance` 里那段手写频谱（现在 `UnitChatPanel.cs:1047-1074`）删掉/改成 `drv.Tick(dt)`
（两处都跑会**双写** `_MovementRange`，看着也对但平滑会变成两重）。

> ⚠️ `WaveTint` 是 `UnitChatPanel` 里现成的 `(0.6988, 0.9811, 0.9265)` —— 原版这格的 `Image.m_Color`。
> 新 shader 里 `_Color` 是**替原版顶点色**的，和顶点色一样**乘两次**；传这个 tint 才得到原版的绿色。

---

## 五、没查清 / 判不准 / 顺手发现

### 5·1 判不准（已在实现里做了选择，都写在 shader 头 §四）

1. **`_MainTex` / `_Noise` 的 wrap**：原版导入 = `wrapU: 0(Repeat)` / `wrapV: 1(Clamp)`（线图）、`0/0`（噪声）；
   我们这两张 PNG 的 meta 都是 Clamp。**那两份 `.meta` 不在本件白名单** ⇒ shader 里显式 `frac`(U) / `saturate`(V) 补偿。
   差别只剩**接缝那 1 列**的双线性混色。根治：把
   `Resources/Art/ui/40k_voicelines_radio_wave_equalizer.png.meta` 的 `wrapU` 改 0（一行，**请主对话决定**）。
2. **`_GlobalMipBias`**：原版 `sample_b` 的 LOD bias 取自它（URP 里随画质档可能非 0）。我们内置管线没这个概念 ⇒ 按 0；
   原版贴图 `mipCount = 1`、我们的也是 `enableMipMap: 0` ⇒ 与 `tex2D` 等价。
3. **`ZTest`**：原版是 **LEqual**（旧近似写的是 `ZTest Always`），本次**照原版改成 LEqual**。
   若真机发现音波被别的东西挡住 —— 那是摆放/深度的锅，**不是 shader 的锅**（别改回 Always 掩盖）。
4. **`_Color`**：原版**没有**这个属性（染色走顶点色）。我们加它是为了 `ImageQuad.SetTint`（它写 `material.color`）。
   用法按「替补顶点色」处理（乘两次）；`_Color = 白` 时与原版逐位等价。

### 5·2 顺手发现（没改白名单外的文件，只报）

1. 🔴 **`资料/普查产出_0917/shader属性表_块4.md` §112 有一处与字节码不符**：它写 Equalizer 的
   「采样纹理（5）… ｜ 字节码里额外扫到（0）无」，但 ps_4_0 明明 `dcl_resource_texture2d` 了 **t0 和 t1**（两个 sampler）。
   八成是扫描脚本只认声明属性里的第一个纹理槽。**不影响本件结论**（我从反汇编直接读的），但那条记录是错的，建议订正。
2. 材质 `Equalizer_0` 里那一堆 `_WaveScale1/2/3` · `_Movement` · `_Resolution` · `_AlphaClip` · `_Parallax` · `_Color` …
   是**旧版 shader 的残留死值**（当前 shader 属性表里没有它们）—— 与 CLAUDE.md「原版材质带残留值」一致，⛔ 别拿它们当判据。
3. `AudioWaveEqualizer` 的频谱数组长度 = **64**（不是 16）：`FUN_1803f37c0(type, 0x40)` 的第二个实参是**元素个数**
   —— 同一个函数在 `AbilityLogic__GetTargets.c:282` 用 `6` 建长度 6 的数组（`+0x18` 即长度）可佐证。**只求和前 16 个**。
4. `AudioWaveEqualizer.Update` 用的是 `Time.deltaTime`（受 timeScale 影响），不是 unscaled；我们照做（`Tick(Time.deltaTime)`）。
5. 原版 `Initialize` 里那两句顺序是**先 `enabled=false` 再建材质**；材质是 `new Material(image.material)`（会在 Image 上换成实例）。

### 5·3 没跑的验证（主对话要跑）

* Unity 批处理（本代理禁跑）：`VoiceWaveProbe.Run`（上面 §3·1）。
* 真机：气泡播放语音时看**声音**（T06 已记：`Battle.unity` 原本 0 台 AudioListener，靠 `BattleDriver.EnsureAudioListener` 兜底）——
  声音没有 ⇒ 频谱恒 0 ⇒ 音波恒为直线（这次也不会例外，**先听声音再判音波**）。
* 受影响宿主：本件只改**新文件 + 一个 Resources shader**，没动任何 `Editor/{BattleScene,...}` 宿主；
  按铁律 12「纯新增件」口径，**建议跑 `BattleScene.Run` + `AnimFXCheck`**（shader 变体的编译在 player 构建时才会暴露，
  批处理里至少能确认 `Shader.Find` 拿得到、材质建得出）。
