// VoiceWaveDriver.cs —— 原版 `AudioWaveEqualizer` 的**逐句复刻**（2026-10-23）
//
// 为什么单独一件：聊天框那条「音波」的**动态**在原版不是 shader 自己算的，是这个 MonoBehaviour
// 每帧从 `AudioSource` 取频谱、算一个 0..1 的标量，`Material.SetFloat` 灌进
// `_MovementRange`（名字 = `DAT_184235590` 字符串，见 `资料/普查产出_1017/.../va_map.txt:303`），
// 再由 `CardPresentation/VoiceWave`（原版 `Shader Graphs/Equalizer` 的复刻）把它变成波形。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
// 判据：`d:/2/tools/decomp_full/AudioWaveEqualizer__{ctor,Initialize,Toggle,Update}.c`（9 月 18 日全量反编译）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   · `.ctor`：`+0x28 = 0x41200000`（**spectrumModifier = 10**）· `+0x2c = 0x41200000`（**speedChange = 10**）
//              · `+0x30 = new float[64]`（`FUN_1803f37c0(type, 64)`：第二个实参是**元素个数**，
//                 同一函数在 `AbilityLogic__GetTargets.c:282` 用 6 建长度 6 的数组可佐证）
//   · `Initialize(AudioSource)`：`audioSource = 参数` → **先 `enabled = false`** →
//              `instanceMaterial = new Material(image.material)` → `image.material = instanceMaterial`
//   · `Toggle(bool)`：只做 `Behaviour.enabled = 参数`
//   · `Update()`：
//       ① `if (audioSource == null) return;`（Unity `Object.op_Equality` ⇒ 被销毁也算 null）
//       ② `audioSource.GetSpectrumData(spectrum, 0, /*FFTWindow*/2)`   —— **2 = Hamming**
//       ③ **前 16 个 bin 求和**（`do { } while ((int)i < 0x10)`；数组是 64 长，后 48 个**不看**）
//       ④ `sum *= spectrumModifier` → `clamp(sum, 0, 1)`
//       ⑤ `k = clamp(Time.deltaTime * speedChange, 0, 1)`；`targetMovement = (sum - prev) * k + prev`
//       ⑥ `image.material.SetFloat("_MovementRange", targetMovement)`
//     ⚠️ 原版**不判 `isPlaying`** —— 没在播时 `GetSpectrumData` 天然给 0，靠 ⑤ 的平滑自然落回 0。
//
// 场景实例（`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4115.json`）：
//   `spectrumModifier = 15.0` · `speedChange = 10.0` —— 用的人按场景值改 Inspector，
//   代码默认值保持**构造函数**的 10 / 10（那是原版字段初值，别用场景值覆盖默认）。
//
// 用法（骨架；`UnitChatPanel` 的接线由主对话改，本件不动那个文件）：
//     var drv = waveGo.AddComponent<VoiceWaveDriver>();      // 或挂在气泡自己的件上
//     drv.spectrumModifier = 15f; drv.speedChange = 10f;     // 原版场景实例的值
//     drv.TargetRenderer = waveQuadRenderer;                 // 或 drv.TargetGraphic = image;
//     drv.Initialize(audioSource);                           // 建实例材质（原版那句 new Material）
//     drv.Toggle(true);                                      // 气泡出现时开
//
// ⚠️ 本组件**不创建材质**、也不改其它属性 —— 材质请用 `Core/VoiceWaveMaterial.cs` 建
//    （它把原版 `Equalizer_0` 的 6 个属性值 + 线性噪声贴图一次配齐）。只拿 `new Material(shader)`
//    的话 `_Noise` 是白的、`_XOffsets`/`_WaveScaleRemap` 是默认值 ⇒ 波形**不是原版的形状**。
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{

/// <summary>
/// 原版 <c>AudioWaveEqualizer</c> 的逐句复刻：每帧取 16 个频段 → ×幅度 → clamp01 → 按 dt×速度平滑
/// → 写 <c>_MovementRange</c>。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Warpforge/Voice Wave Driver")]
public class VoiceWaveDriver : MonoBehaviour
{
    // ── 原版字段（注释里的 `+0xN` = 反编译里的对象偏移，便于对回方法体）────────────────────
    [SerializeField] AudioSource audioSource;             // +0x40
    [SerializeField] Renderer targetRenderer;             // +0x20（原版是 uGUI `Image image`）
    [SerializeField] Graphic targetGraphic;               // 同一格的 uGUI 形态（二选一）
    [SerializeField] float spectrumModifier = 10f;        // +0x28（ctor 0x41200000）
    [SerializeField] float speedChange = 10f;             // +0x2c（ctor 0x41200000）

    float[] spectrum;                                     // +0x30
    float targetMovement;                                 // +0x38
    Material instanceMaterial;                            // +0x48
    bool warnedNoTarget;

    /// <summary>频谱数组长度（原版 `.ctor` 是 `new float[64]`）。</summary>
    public const int SpectrumLength = 64;
    /// <summary>参与求和的频段数（`Update.c` 的循环上界 `0x10`）。</summary>
    public const int Bins = 16;
    /// <summary>`GetSpectrumData` 的窗口（`Update.c` 第 3 个实参 = **2 = Hamming**）。</summary>
    public const FFTWindow Window = FFTWindow.Hamming;
    /// <summary>`Material.SetFloat` 的属性名（`DAT_184235590`）。⛔ 别改 —— shader 里就这个名字。</summary>
    public const string MaterialProperty = "_MovementRange";

    // ── 对外只读状态（自检/探针用，⚠️ 不改行为）──────────────────────────────────────────
    public float Movement { get { return targetMovement; } }
    public Material InstanceMaterial { get { return instanceMaterial; } }
    public AudioSource Source { get { return audioSource; } }
    public Renderer TargetRenderer { get { return targetRenderer; } set { targetRenderer = value; } }
    public Graphic TargetGraphic { get { return targetGraphic; } set { targetGraphic = value; } }

    /// <summary>`+0x28`（默认 10 = 原版 `.ctor` 的 0x41200000）。场景实例是 15。</summary>
    public float SpectrumModifier
    {
        get { return spectrumModifier; }
        set { spectrumModifier = value; }
    }

    /// <summary>`+0x2c`（默认 10 = 原版 `.ctor` 的 0x41200000）。场景实例是 10。</summary>
    public float SpeedChange
    {
        get { return speedChange; }
        set { speedChange = value; }
    }

    /// <summary>探针/自检专用：喂一份**假频谱**代替 `GetSpectrumData`（null = 回到真音频）。
    /// ⚠️ 设了它以后即使 `audioSource == null` 也照常算 —— 原版那条 null 早退会被绕过，**只给测试用**。</summary>
    public float[] SpectrumOverride { get; set; }

    /// <summary>原版 `Initialize(AudioSource)`：记下来源 → **关掉自己** → 把目标材质换成一份实例。</summary>
    public void Initialize(AudioSource src)
    {
        audioSource = src;                       // *(this+0x40) = 参数
        enabled = false;                         // Behaviour.set_enabled(this, 0)  —— 原版**先关**

        var src2 = GetTargetMaterial();
        if (src2 != null)
        {
            instanceMaterial = new Material(src2);   // new Material(image.material)
            instanceMaterial.name = src2.name + " (VoiceWave Instance)";
            SetTargetMaterial(instanceMaterial);
        }
        else
        {
            // 原版这里会直接 NRE（`image` 为空 ⇒ `FUN_1803f47a0` 抛）。我们出声但不炸：
            Debug.LogWarning("[VoiceWaveDriver] Initialize 时目标材质为 null（targetRenderer/targetGraphic 没接？）"
                             + " ⇒ 这一格不会有波形（⛔ 不静默）。", this);
        }
    }

    /// <summary>原版 `Toggle(bool)`：只开关组件自己。</summary>
    public void Toggle(bool option) { enabled = option; }

    void Update() { Tick(Time.deltaTime); }

    /// <summary>
    /// 原版 `Update()` 的**方法体**（`Time.deltaTime` 由参数给，便于自检固定步长）。
    /// 逐句顺序与反编译一致，⛔ 别「顺手优化」（比如改成判 `isPlaying`、把 16 改成求和全部、先把 dt 乘进别处）。
    /// </summary>
    public void Tick(float deltaTime)
    {
        // ① `UnityEngine.Object.op_Equality(audioSource, null)` ⇒ 被销毁也早退
        var fake = SpectrumOverride;
        if (fake == null && audioSource == null) return;

        // ② 取频谱（channel 0；Hamming）
        float[] spec;
        if (fake != null)
        {
            spec = fake;
        }
        else
        {
            if (spectrum == null) spectrum = new float[SpectrumLength];
            audioSource.GetSpectrumData(spectrum, 0, Window);
            spec = spectrum;
        }

        // ③ 前 16 个 bin 求和（原版数组 64 长，只读前 16）
        float sum = 0f;
        int n = spec.Length < Bins ? spec.Length : Bins;
        for (int i = 0; i < n; i++) sum += spec[i];

        // ④ ×幅度 → clamp01
        sum *= spectrumModifier;
        if (sum < 0f) sum = 0f; else if (sum > 1f) sum = 1f;

        // ⑤ 按 `deltaTime * speedChange`（同样 clamp01）做指数趋近
        float prev = targetMovement;
        float k = deltaTime * speedChange;
        if (k < 0f) k = 0f; else if (k > 1f) k = 1f;
        targetMovement = (sum - prev) * k + prev;

        // ⑥ 写进材质（原版每帧现取 `image.material`）
        var mat = GetTargetMaterial();
        if (mat != null) mat.SetFloat(MaterialProperty, targetMovement);
        else if (!warnedNoTarget)
        {
            warnedNoTarget = true;
            Debug.LogWarning("[VoiceWaveDriver] 目标材质为 null ⇒ 频谱算出来了但没地方写（⛔ 不静默）。", this);
        }
    }

    // ── 目标材质的读写：uGUI `Graphic` 与 `Renderer` 两种形态 ────────────────────────────
    Material GetTargetMaterial()
    {
        if (targetGraphic != null) return targetGraphic.material;   // 原版那一形态（Image）
        if (targetRenderer != null) return targetRenderer.sharedMaterial;
        return null;
    }

    void SetTargetMaterial(Material m)
    {
        if (targetGraphic != null) { targetGraphic.material = m; return; }
        if (targetRenderer != null) { targetRenderer.sharedMaterial = m; return; }
    }

#if UNITY_EDITOR
    void Reset()
    {
        // 挂在带 Renderer 的件上时自动接自己（只是便利，不影响原版语义）
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }
#endif
}

}   // namespace CardPresentation
