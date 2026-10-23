# R9 · 屏震 `rawSignal` 的采样算式（**反汇编坐实**，2026-10-23）

> 回答 `资料/普查产出_1023b/特效线_执行报告.md` §2·4 那条「采样算式本身查不到」。
> **已解出**：算式来自 `Cinemachine.NoiseSettings`，方法体在 **`GameAssembly.dll`** 里
> （`Cinemachine.dll` 是包 DLL，方法体不进 `decomp_full`；但 IL2CPP 把它的 C# 编进了 `GameAssembly.dll`，
> `d:/2/tools/il2cpp_out/dump.cs` 里有 RVA，`d:/4/_tmp_view/cpp2il_run/out_isil/IsilDump/Cinemachine/` 里有 ISIL）。
> 本件**只读**（未改任何工程文件、未跑 Unity、未动 git）。

---

## 〇、结论（一句话 + 算式）

**每一轴、每一条带（octave）各算一个标量，三条带【相加】（不是相乘、不是取最大）：**

```text
u   = Frequency × T                              // T = 信号时间（秒）
val = Constant == 1 ? Amplitude × 0.5 × sin(2π·u)
                    : Amplitude × (Mathf.PerlinNoise(u, 0) − 0.5)
```

- **`Constant = 1` 走正弦、`Constant = 0` 走 Perlin** —— 这条是最反直觉的一条（tooltip 只写「不随机化」）。
- Perlin 的第二参数是 **0**，不是 0.5；**变化的坐标在 x 轴**（我们现在用的是 y 轴，见 §五）。
- 三条带的幅度各是 **±0.5·A**（Perlin 那一支减 0.5，Constant 那一支乘 0.5）—— 合峰 = `0.5·ΣA`。
- 信号时间轴：`T = t × m_FrequencyGain + m_StartTimeOffset`，`t` = 冲量开始后的秒数
  （**缩放时间 `Time.time`**，`IgnoreTimeScale` 时才用 `realtimeSinceStartup`）；
  `m_StartTimeOffset = Random.Range(−1000, +1000)`（仅当 `m_Randomize=1` 且 `SignalDuration ≤ 0`）。
- 输出：`pos = Σ PositionNoise`（3 octave 逐轴相加）；`rot = Quaternion.Euler(Σ OrientationNoise × π/180)`。
- 之后还要过 `LegacySignalSource`：`pos ×= |velocity| × m_AmplitudeGain`，
  `pos = Quaternion.FromToRotation(Vector3.down, velocity) × pos`，`rot = SlerpUnclamped(identity, rot, gain)`。

**落地一句话**：把 `CardFeel.ShakeNoise01` 的「三条带全 Perlin、`Perlin*2−1`、y 轴采样、相位不分带」
换成上面的逐带 `Constant?sin:Perlin` + `−0.5` + **x 轴采样** + 共享 `T`；**`ShakeBandPeak = 0.149` 不要改**
（见 §五·5：这一改幅度会**同时自动落在正确的原版值上**）。

---

## 一、DLL 路径与版本

| 用途 | 路径 | 说明 |
|---|---|---|
| **真值本体**（唯一权威） | `d:/2/unity_run_ref/GameAssembly.dll` | 76,765,184 B · 2025-08-25 · `ImageBase = 0x180000000` · 7 个节（`.text` 0x1000 / `il2cpp` 0x4b3000 / `.rdata` 0x34b2000 …） |
| **RVA ↔ 方法名索引** | `d:/2/tools/il2cpp_out/dump.cs`（Il2CppDumper 产物） | 294,942 个方法；本件所有 `RVA: 0x…` 都出自这里 |
| **方法体反汇编（ISIL）** | `d:/4/_tmp_view/cpp2il_run/out_isil/IsilDump/Cinemachine/Cinemachine/*.txt` | Cpp2IL 的 ISIL 转储，带绝对地址 |
| 原版托管 DLL（签名桩） | `d:/2/Warpforge_code/Assemblies/Cinemachine.dll`（163,328 B） | 只有签名；`d:/2/Warpforge_code/Scripts/Cinemachine/` 是它的反编译桩 |
| IL2CPP 生成桩 | `d:/2/tools/il2cpp_out/DummyDll/Cinemachine.dll`（345,088 B） | 字段/属性/特性齐全，方法体空 |

**版本**：包名 `com.unity.cinemachine@915237edaa40`（DLL 内嵌的源文件路径
`Library/PackageCache/com.unity.cinemachine@915237edaa40/Runtime/Core/NoiseSettings.cs`），
所有 `HelpURL` 都写 `com.unity.cinemachine@2.9` ⇒ **Cinemachine 2.9.x**。
⚠️ **精确 patch 号没读到**（AssemblyVersion 是二进制字段，`strings` 里没有；不影响，算式以 IL 为准）。

**工具**：本机已有 `python 3` + `capstone 5.0.7`（未新装任何东西）；反汇编/常量读取都用它直读 `GameAssembly.dll`。

---

## 二、判据（逐方法，VA = 0x180000000 + RVA）

### 2·1 `Cinemachine.NoiseSettings+NoiseParams::GetValueAt(float time, float timeOffset)` — **RVA 0xA10610**

结构体字段（`dump.cs` · `TypeDefIndex: 16949`）：`Frequency` 0x0 · `Amplitude` 0x4 · `Constant` 0x8。

实测字节（`GameAssembly.dll` 文件偏移 0xA0F010）：

```text
cmp  byte ptr [rcx+8], 0          ; Constant
movaps xmm0, xmm1                 ; time
mulss xmm0, dword ptr [rcx]       ; × Frequency
addss xmm0, xmm2                  ; + timeOffset
jne  L_constant                   ; Constant != 0 ⇒ 正弦支路

; ---- Constant == 0：Perlin ----
xor  r8d, r8d                     ; (int)0
xorps xmm1, xmm1                  ; y = 0.0f  ← 第二参数是 0
call Mathf.PerlinNoise            ; VA 0x182FD71B0（Cpp2IL 已解析出名字）
subss xmm0, [0x1834B2BB4]         ; − 0.5
mulss xmm0, [rbx+4]               ; × Amplitude
ret

; ---- Constant != 0：正弦 ----
addss xmm0, xmm0                  ; 2t
mulss xmm0, [0x1834B2E90]         ; × π
call 0x180488130                  ; ← sin(float)
mulss xmm0, [rbx+4]               ; × Amplitude
mulss xmm0, [0x1834B2BB4]         ; × 0.5
ret
```

**三个常量是从 `GameAssembly.dll` 按 PE 节表读出来的**（不是猜）：

| 地址 | 文件偏移 | 4 字节 | 值 |
|---|---|---|---|
| `0x1834B2BB4` | 0x34B07B4 | `0000003f` | **0.5** |
| `0x1834B2E90` | 0x34B0A90 | `db0f4940` | **π = 3.14159274f** |
| `0x1834B2DC0` | 0x34B09C0 | `35fa8e3c` | **π/180 = 0.017453292f**（§2·3 用） |
| `0x1834B3470` / `0x1834B3124` | — | `00007ac4` / `00007a44` | **−1000f / +1000f**（§2·4 用） |

**`0x180488130` 是 `sin` 不是 `cos`** —— 两条独立判据：

1. **代码结构**：`|x| ≤ π/4` 的快路径（`0x180488187` → `0x180488320`）算的是 **S 多项式**
   `x + x·z·(S1 + z·(S2 + z·(S3 + z·S4)))`（`z = x²`），这是 fdlibm `sin` 的奇次核；
   C 多项式（`1 − 0.5z + z⁴·(…)`，`0x180488380`）只在象限归约后走 —— 正是 `sin` 的
   `switch(n & 3){0:Sin,1:Cos,2:−Sin,3:−Cos}`。
2. **数值**：把读到的系数代进快路径——
   `S1=−0.16666666666666666 · S2=0.008333333333333333 · S3=−0.0001984126984126984 · S4=2.7557319e−6`：
   `x=0.3 → 0.295520206661384` vs `math.sin(0.3)=0.295520206661340`（差 **4.4e−14**）；
   `x=0.7` 差 4.9e−10；`x=−0.5` 差 1.2e−11。

> 为什么 `Cinemachine` 的 C# 会调到这个「无名的 float sin」：IL2CPP 把 `Mathf.Sin`（RVA 0x2FD7450，
> `Math.Sin` = 0x18048C900）在调用点做了 float 特化/内联，`Cpp2IL` 在元数据里找不到名字就只打地址。
> 数学上就是 `sinf`。

### 2·2 `NoiseSettings+TransformNoiseParams::GetValueAt(float time, Vector3 timeOffsets)` — **RVA 0xA132E0**

对 X/Y/Z **各调一次** 2·1（偏移分别取 `timeOffsets.x / .y / .z`），装回 `Vector3`。
结构体布局：`X` 0x0 · `Y` 0xC · `Z` 0x18（每轴 12 字节）。

### 2·3 `NoiseSettings::GetCombinedFilterResults(TransformNoiseParams[] , float time, Vector3 timeOffsets)` — **RVA 0xA10670**

```csharp
result = Vector3.zero;
for (i = 0; i < noiseParams.Length; i++)
    result += noiseParams[i].GetValueAt(time, timeOffsets);   // ← 相加
return result;
```

### 2·4 `NoiseSettings::GetSignal(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot)` — **RVA 0xA107E0**

```csharp
pos = GetCombinedFilterResults(PositionNoise,    t, Vector3.zero);   // 字段 0x18
rot = Quaternion.Euler(GetCombinedFilterResults(OrientationNoise, t, Vector3.zero) × π/180);  // 字段 0x20
```

- 两次调用传的 `timeOffsets` 都是 **`Vector3.zero`**（`typeof(Vector3)` 静态字段 +0x0）⇒
  **`NoiseSettings` 当信号源用时没有任何内部随机相位**；
  带 `m_NoiseOffsets` 的随机相位是 `CinemachineBasicMultiChannelPerlin` 那条路，**不是这条**。
- 旋转：先 `×0.017453292`（度→弧度）再 `Quaternion.Internal_FromEulerRad`（`0x182FDAA50`）≡ `Quaternion.Euler(向量)`。
- `get_SignalDuration()` 恒返回 **0**（`xorps xmm0,xmm0; ret`）⇒ 见 2·5 的两个后果。

### 2·5 `CinemachineImpulseDefinition+LegacySignalSource` — **ctor 0xA2B5E0 · GetSignal 0xA2B350**

**ctor（随机起播时刻就在这里）**

```csharp
if (m_Def.m_Randomize) {                      // 定义 0x44
    if (m_Def.m_RawSignal.SignalDuration <= 0) // NoiseSettings ⇒ 0，恒真
        m_StartTimeOffset = Random.Range(−1000f, +1000f);   // 存在 +0x24
}
```

**GetSignal**

```csharp
T = timeSinceSignalStart * m_Def.m_FrequencyGain + m_StartTimeOffset;   // 0x3C、+0x24
if (m_RawSignal.SignalDuration > 0) {          // NoiseSettings = 0 ⇒ 整段跳过
    if (m_RepeatMode == Loop) T %= duration;
    else if (timeEnvelope.Duration > 1e-4) T *= timeEnvelope.Duration / duration;
}
m_Def.m_RawSignal.GetSignal(T, out pos, out rot);

float gain = m_Velocity.magnitude * m_Def.m_AmplitudeGain;   // 0x38
pos *= gain;
pos  = Quaternion.FromToRotation(Vector3.down, m_Velocity) * pos;   // 静态字段 0x24 = downVector
rot  = Quaternion.SlerpUnclamped(Quaternion.identity, rot, gain);   // 0x182FDB340
```

- `m_Velocity` 是 `GenerateImpulseWithVelocity` 传进来的**归一化方向**（见 2·6），所以
  `|velocity| = 1` ⇒ **幅度增益 = `m_AmplitudeGain` = `|preset.direction| × preset.amplitude`**。
- **方向映射**：`FromToRotation(Vector3.down, velocity)` ⇒ 原始信号的 **−Y 指向 velocity**，
  +Y 指向 velocity 的反向。⚠️ 这条决定「屏幕上是往上还是往下」，与我们现在的 `verticalSign` 独立（§五·6）。
- `m_RepeatMode` / 拉伸那段对 `NoiseSettings` **不生效**（SignalDuration=0）。

### 2·6 游戏侧怎么把 preset 喂进去

| 位置 | 事实 |
|---|---|
| `d:/2/tools/decomp_full/CameraShakePreset__Play.c` | `DoShake(this, preset.sustainTime(0x14), attackTime(0x18), decayTime(0x1c), amplitude(0x20), frequency(0x24), direction(0x28), delay(0x10), rawSignal(0x38))` |
| `d:/2/tools/decomp_full/CameraShakerManager._DoShake_d__33__MoveNext.c` | 写进池里 `CinemachineImpulseSource.m_ImpulseDefinition`：`+0x30 = rawSignal ?? manager.defaultRawSignal` · `+0x38 = |direction| × amplitude` · `+0x3C = frequency` · `+0x58/0x5C/0x60 = attack/sustain/decay`；`velocity = normalize(direction)` |
| `d:/2/tools/decomp_full/CameraShakerManager__Awake.c:69` | `defaultRawSignal = sourceReference.m_ImpulseDefinition.m_RawSignal`（**场景里那个**） |
| `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4914.json` | 场景 `CinemachineImpulseSource`：**`m_ImpulseType = 3`（Legacy ✓）** · `m_RawSignal` = pathID **1276427422462347950** · `m_Randomize = 1` · `m_RepeatMode = 0` · `m_ImpactRadius = 10000` · `m_PropagationSpeed = 343` · 默认包络 attack 0 / sustain **0.5** / decay **0.3**（运行时被 preset 覆盖） |
| `…/battlearena1/MonoBehaviour/MonoBehaviour_4546.json` | `CinemachineImpulseListener`：`m_Gain = 1.0` · `m_UseCameraSpace = 1` · `m_ChannelMask = 1` |
| **pathID 1276427422462347950 = `Warpforge 6D Shake`** | 判据：`d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/Warpforge 6D Shake_1276427422462347950.json`（旧解包把 pathID 写进文件名）；`CardPresentation/Core/CardFeel.cs:291` 也记着这条映射 |

### 2·7 `CinemachineImpulseManager+ImpulseEvent::GetDecayedSignal` — **RVA 0xA2A060**

```text
dt   = CurrentTime − m_StartTime − distance / max(m_PropagationSpeed, 1.0f)
gain = m_Envelope.GetValueAt(dt) × DistanceDecay(distance)
m_SignalSource.GetSignal(dt, out pos, out rot)
pos *= gain
rot  = Quaternion.SlerpUnclamped(identity, rot, gain)     // 旋转被缩放第二次（第一次在 LegacySignalSource）
```

- `CurrentTime`（`CinemachineImpulseManager::get_CurrentTime` RVA 0xA17A90）：
  `IgnoreTimeScale ? Time.realtimeSinceStartup : CinemachineCore.CurrentTime`；
  后者（RVA 0xA05DA0）在内部计时 < 0 时回落到 **`Time.time`（缩放时间，RVA 0x301B720）**。
- 场景里 `m_ImpactRadius = 10000`、相机就是 listener ⇒ `DistanceDecay = 1`、`distance/speed ≈ 0`
  ⇒ **屏震实际只剩「包络 × 信号」**。
- 包络曲线来自 `m_TimeEnvelope`；`m_DecayShape` 是两点曲线 `(0,1) → (1,0.01)`，
  切线只在 2 点以上生效 ⇒ **线性降到 0.01**（与 `CardFeel.ShakeDecayTo = 0.01` 一致）。
- listener 端（`CinemachineImpulseListener::PostPipelineStageCallback` RVA 0xA16E80）：
  `GetImpulseAt(state 位置, 2D?, channelMask, out pos, out rot)` → `pos,rot ×= m_Gain(=1)`
  → `m_UseCameraSpace(=1)` 则先乘相机旋转 → 加进 `CameraState` 的 Position/OrientationCorrection。

---

## 三、可直接照写的伪代码（C#，落到我们的 2D 版）

```csharp
// ===== 原版 Cinemachine 那一层的等价值（逐行对应 §2·1 / 2·2 / 2·3）=====
static float AxisValue(float freq, float amp, bool constant, float T)
{
    float u = freq * T;                                   // 注意：T 已经是「信号秒」
    return constant
        ? amp * 0.5f * Mathf.Sin(u * 2f * Mathf.PI)       // Constant=1
        : amp * (Mathf.PerlinNoise(u, 0f) - 0.5f);        // Constant=0（y=0，变化在 x）
}

// 三个 octave 相加（PositionNoise），rot 用 OrientationNoise（我们是 2D，丢 rot）
static Vector3 Signal(float T, TransformNoiseParams[] ch)
{
    Vector3 v = Vector3.zero;
    for (int i = 0; i < ch.Length; i++)
        v += new Vector3(AxisValue(ch[i].X.Frequency, ch[i].X.Amplitude, ch[i].X.Constant, T),
                         AxisValue(ch[i].Y.Frequency, ch[i].Y.Amplitude, ch[i].Y.Constant, T),
                         AxisValue(ch[i].Z.Frequency, ch[i].Z.Amplitude, ch[i].Z.Constant, T));
    return v;
}

// ===== 我们 CardFeel 里应该长的样子（t = 冲量开始后的秒数，phase = m_StartTimeOffset）=====
public static float ShakeNoise01(float t, float phase, float frequencyGain, bool[] constant /*按带*/)
{
    float T = t * frequencyGain + phase;          // ⚠️ 是 T 共享，不是每带各加一个 phase
    float s = 0f;
    for (int i = 0; i < ShakeBandHz.Length; i++)
        s += AxisValue(ShakeBandHz[i], ShakeBandAmp[i], constant[i], T);
    return s / ShakeBandPeak;                     // 0.149 —— 不要改，见 §五·5
}
```

`phase`（= 原版 `m_StartTimeOffset`）：`Random.Range(−1000f, +1000f)`（我们用
`seed → System.Random` 派生，保住「对局可复现」红线），**不是**现在那个 `[0, 100)`。

---

## 四、默认信号是谁 + 逐 preset

**默认信号 = `Warpforge 6D Shake`**（场景 `m_RawSignal`，见 §2·6）。它的 `PositionNoise`
（= `…No rotation` 逐位相同；`…One Axis` 只有 Y 非零）：

| 带 | X | Y | Z |
|---|---|---|---|
| [0] | f 3.200 a 0.011 **c 1** | f **1.900** a **0.059** **c 1** | f 3.330 a 0.021 **c 1** |
| [1] | f 7.700 a 0.009 **c 1** | f **9.100** a **0.040** **c 0** | f 9.220 a 0.009 **c 1** |
| [2] | f 51.510 a 0.002 **c 1** | f **55.540** a **0.050** **c 1** | f 58.550 a 0.017 **c 1** |

⇒ 屏幕竖直（Y）那一列的真波形：

```text
Y(T) = 0.5·0.059·sin(2π·1.9·T)  +  0.040·(Perlin(9.1·T, 0) − 0.5)  +  0.5·0.050·sin(2π·55.54·T)
```

**`Constant` 的分布就是「低/高频两条正弦 + 中频一条 Perlin」** —— 我们现在三条全 Perlin，这是最显眼的偏离。

preset → 信号（`特效线_执行报告.md` §2·1，本件复核过）：

| preset | rawSignal | Constant（Position Y 三条带） |
|---|---|---|
| `Shake Forward Pull` / `Shake Land on ground` / `Shake Upwards Hand` | `…One Axis` | 1 / 0 / 1 |
| `Shake UI Earthquake` / `Shake UI Hit` | `…No rotation` | 1 / 0 / 1 |
| 其余 12 个（**含 `Shake Hit Small`**） | **空** ⇒ 落到默认 | `Warpforge 6D Shake`（Y 同样是 1/0/1） |

> ⇒ **所有屏震的竖直波形都是同一组「sin + Perlin + sin」**；差别只在 `frequency`（时间缩放）、
> `amplitude×|direction|`（幅度）和方向。R9 的工作量确实是 1 份算式，不是 5 份。

---

## 五、与现有实现的差异（`CardPresentation/Core/CardFeel.cs:1238-1248` `ShakeNoise01`）

| # | 现在 | 原版（判据） | 影响 |
|---|---|---|---|
| **1** | `Mathf.PerlinNoise(0f, f·t + phase)` —— 变化在 **y** 轴 | `Mathf.PerlinNoise(u, 0f)` —— 变化在 **x** 轴 | Unity 的 Perlin 是 2D 梯度噪声，`noise(0,y)` ≠ `noise(y,0)` ⇒ **整条波形不同** |
| **2** | `Perlin × 2 − 1`（±1） | `Perlin − 0.5`（±0.5） | 每带幅度 2×（但与 5 一起算净效果，见下） |
| **3** | 三条带**全 Perlin** | `Constant=1` 的带走 `0.5·sin(2πu)`；本件用的是 **1/0/1** | 低频 1.9 Hz 与高频 55.54 Hz 两条**根本不是噪声**，是纯正弦；观感的「抖动质感」主要来自这两条 |
| **4** | 一个 `phase` 直接加到参数上（对所有带同一个偏移） | `T = t·gain + offset` 后**再乘各带的 f** ⇒ 每条带的相位 = `2π·f·offset`，本质上是**每条带各自的随机相位** | 现在三条带被强制同相起播 —— 一旦按 #3 换成 1/0/1（两条正弦），同相就**肉眼可见**（三条带一起从 0 起振），必须一起改 |
| **5** | 归一化 `s / ShakeBandPeak(0.149)`，`x_i ∈ [±1]` | 每带 `±0.5A`，合峰 `0.5ΣA = 0.0745` | **净值：改完 1~4 之后，如果 `ShakeBandPeak` 保持 0.149，峰值位移会自动变成原版的精确值**——因为现在的「峰值 = ΣA」假设本身就多算了 2×。⇒ ⛔ **别顺手把 0.149 改成 0.0745**，那会让屏震只有原版的一半 |
| **6** | 只取 Y、只做「竖直位移」；`verticalSign` 定上下 | 完整 3D：`pos` 三轴 + `rot`（OrientationNoise，本件 default 信号有 9 条），再过 `FromToRotation(Vector3.down, velocity)` | 我们 2D 战场丢 X/Z 与旋转是**可接受的简化**，但**符号**要按 `FromToRotation(down, v)` 重算：raw **−Y** 指向 velocity 方向（现在 `verticalSign=+1` 的来源是「preset.direction 的 y 符号」，与原版这条链不等价） |
| **7** | `ShakePhaseRange = 100f`，`phase ∈ [0,100)` | `Random.Range(−1000, +1000)`（**含负、±1000**） | 相位分布不同（分布形状一样是均匀，只是区间）；属「代理值」，可一并订正 |
| **8** | `ShakeFrequencyGain = 0.05f` 写死（取自 `Shake Hit Small`） | **每个 preset 自己的 `frequency`**（UI Hit = 0.1、UI Earthquake 另值…） | `WFModuleScreenShake.Request.frequency` 已经有值，但 `BattleDriver.HookAnimFxShake`（`BattleDriver.cs:2533-2541`）**只用了 amplitude×|direction|**，frequency/sustain/attack/decay **全丢** ⇒ 别的 preset 走 AnimFX 时用的是 Hit Small 的时间轴 |

> 差异 8 顺带解释了为什么现在「频率带对、波形不对」这句话只对了一半：**基频对，但每条带的函数形式、采样轴、幅度系数都不对。**

---

## 六、没查清 / 还欠的

1. **`Cinemachine` 精确 patch 号**（只知道 2.9.x、package hash `915237edaa40`）。
   ⚠️ 不影响算式：全部结论来自 `GameAssembly.dll` 的机器码，不是从源码抄的。
2. **Unity `Mathf.PerlinNoise` 的归一化范围**：dump 里是 `[FreeFunction("PerlinNoise::NoiseNormalized")]`
   （原生实现，`RVA 0x2FD71B0`）⇒ 名义 [0,1]，但**允许轻微越界**。
   原版减 0.5 之后也可能略微超出 ±0.5 —— 这是原版行为，照抄即可，**不要 clamp**。
3. **`m_TimeEnvelope.m_AttackShape` / `m_DecayShape` 的曲线求值**（`AnimationCurve.Evaluate`）
   只在场景件里读到两点 `(0,1)→(1,0.01)`；运行时被 preset 覆盖的只有三个**时间**，曲线不变
   ⇒ 我们现有的线性包络（`ShakeEnvelope`）与之等价，**这条不用改**。
4. **`GetDecayedSignal` 里 `m_DirectionMode` 的分支**（Fixed / RotateTowardSource）没逐指令读完
   —— 场景是 `m_DirectionMode = 0 (Fixed)`，且 listener 就在冲量源附近，本件没继续往下挖。
5. **OrientationNoise 的最终观感**：legacy 路径**确实用了 rot**（`SlerpUnclamped` 两次），
   但我们 2D 版不抖角度 ⇒ 这部分**不复刻**（属 §五·6 的简化，需在账上写明是「用户可见的已知简化」，
   不是「原版没有」）。

---

## 七、落地要改的四处（+1 处附带）

| # | 文件 | 改什么 |
|---|---|---|
| 1 | `d:/4/Unity/工具/gen_shake_presets.py` | 现在只抽 6 标量 + direction + `hasRawSignal`（`:56-63`）。加：解析 `rawSignal.m_PathID` → 从 `bundle_tweenandshakes_assets_all/MonoBehaviour/` 的同名 SO 读 `PositionNoise[3]/{X,Y,Z}/{Frequency,Amplitude,Constant}`（`OrientationNoise` 可一起收，标明暂不用）；空 `rawSignal` 的 preset 标成 `default` |
| 2 | `d:/4/Unity/MyGame/Assets/Resources/WarpforgeVFX/shake_presets.json` | 每个 preset 加 `rawSignal` 段（带名 + 3×3×3 表 + `usesDefault`）。改 `note`（现在自承「没复刻」） |
| 3 | `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Runtime/WFModuleScreenShake.cs` | `PresetRec`（`:161-167`）加字段；`Request` 带上原始表；`Resolve`（`:199-244`）填；删掉/替换 `:219-225` 那条「不静默」警告（改成真正采到了） |
| 4 | `d:/4/Unity/MyGame/Assets/CardPresentation/Core/CardFeel.cs` | `ShakeNoise01`（`:1238-1248`）换成 §三 的写法（逐带 Constant + x 轴 Perlin + −0.5 + 共享 `T`）；`ShakePhaseRange`（`:1160`）改 ±1000；`ShakeBandHz/Amp` 的注释补 `Constant` 列；**`ShakeBandPeak` 保持 0.149 并写清为什么**（§五·5） |
| **+1** | `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs:2533-2541` | `HookAnimFxShake` 目前只传 `worldAmp/delay`，把 `req.frequency`（及 sustain/attack/decay）也传下去 —— 否则 4 处改完，AnimFX 那条支路仍然用 Hit Small 的时间轴（差异 8） |

⚠️ 落地时的自检口径：断言要盯**原版参数**（如「带 0 在 `T` 上必须是 `sin(2π·1.9·T)`，不是 Perlin」），
不要只断「`ShakeNoise01` 返回有限值」——那对两种实现都成立。
`ShakeNoise01` 是 `public`，可加一条「给定 `t/phase` 的数值与手算一致」的断言（纯函数、可复算）。
