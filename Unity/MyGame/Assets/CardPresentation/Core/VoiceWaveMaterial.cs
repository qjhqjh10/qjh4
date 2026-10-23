// VoiceWaveMaterial.cs —— 按**原版 `Equalizer` 材质的实际数值**建一份音波材质（2026-10-23）
//
// 为什么需要它：原版那条音波的形状**完全**由材质的 6 个属性 + 两张贴图决定，其中
// `_XOffsets` / `_WaveScaleRemap` / `_Noise{1,2}ScaleAndSpeed` 都不是 shader 默认值。
// 只写 `new Material(Shader.Find("CardPresentation/VoiceWave"))` ⇒ 拿到的是 shader 里的**默认值**
// （`_XOffsets=(0,0.33)`、`_WaveScaleRemap=(0,10)`、噪声速度 0.3/0.2）⇒ 形状与原版**不同**。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
// 判据（两个原版 `.mat`，都在 `d:/2/tools/assetripper_out/ExportedProject/Assets/`）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   · `Equalizer_0.mat`  ← **战斗场景实际用的那份**（bundle pathID `-3262226040299197450`，
//      由 `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4509.json` 的 `m_Material`
//      指过来）。`_MainTex = null`（贴图由 uGUI `Image` 的 sprite 供）。
//        `_MovementRange = 0.005911797`
//        `_XOffsets = (0, -1.2, 0, 0)`
//        `_WaveScaleRemap = (0, 13.27, 0, 0)`
//        `_Noise1ScaleAndSpeed = (0.1, 0.1, 0, 0.5)`
//        `_Noise2ScaleAndSpeed = (0.05, 0.05, 0, 0.54)`
//   · `Equalizer.mat`（工厂原件，没被场景改过）作对照：
//        `_MovementRange = 1` · `_XOffsets = (0, 0.33)` · `_WaveScaleRemap = (0, 10)`
//        `_Noise1 = (0.05,0.05,0,0.3)` · `_Noise2 = (0.05,0.05,0,0.2)` · `_MainTex = 那张线图`
//     ⇒ **本文件默认用 `Equalizer_0`（场景那份）**；`CreateFactoryPreset()` 给工厂原件那一套。
//   · 两张贴图：
//        `_MainTex` = `40k_voicelines_radio_wave equalizer`（708×96，我们工程 `Art/ui/` 里那份
//                     与 bundle 里逐字节相同）。⚠️ 原版贴图导入 = **wrapU Repeat / wrapV Clamp**，
//                     我们那份 meta 是 Clamp/Clamp ⇒ shader 里用 `frac/saturate` 显式补偿（见 shader §四·2）。
//        `_Noise`   = `Noise Combined`（128×128，原版 **linear** 导入）。
//                     ✅ 工程里 `Art/card_sdf/cardback_noise.png` 与它**逐像素相同**（0 差异）
//                     且导入就是 `sRGBTexture: 0` / 无 mip / bilinear ⇒ 直接挂这张。
//                     ⛔ 别挂 `Art/ui_menu/Noise_Combined.png`：那张是 **sRGB** 导入，噪声值会被 gamma 解码。
using UnityEngine;

namespace CardPresentation
{

/// <summary>原版 <c>Equalizer</c> 材质数值的音波材质工厂（属性名/数值均逐项来自原版 `.mat`）。</summary>
public static class VoiceWaveMaterial
{
    public const string ShaderName = "CardPresentation/VoiceWave";

    // ── `Equalizer_0.mat`（**战斗场景实际用的那份**）──
    public const float SceneRestMovement = 0.005911797f;                 // `_MovementRange`
    public static readonly Vector4 SceneXOffsets = new Vector4(0f, -1.2f, 0f, 0f);
    public static readonly Vector4 SceneWaveScaleRemap = new Vector4(0f, 13.27f, 0f, 0f);
    public static readonly Vector4 SceneNoise1 = new Vector4(0.1f, 0.1f, 0f, 0.5f);
    public static readonly Vector4 SceneNoise2 = new Vector4(0.05f, 0.05f, 0f, 0.54f);

    // ── `Equalizer.mat`（工厂原件，场景没改过的那份）──
    public const float FactoryRestMovement = 1f;
    public static readonly Vector4 FactoryXOffsets = new Vector4(0f, 0.33f, 0f, 0f);
    public static readonly Vector4 FactoryWaveScaleRemap = new Vector4(0f, 10f, 0f, 0f);
    public static readonly Vector4 FactoryNoise1 = new Vector4(0.05f, 0.05f, 0f, 0.3f);
    public static readonly Vector4 FactoryNoise2 = new Vector4(0.05f, 0.05f, 0f, 0.2f);

    /// <summary>
    /// 建一份**战斗场景那份**（`Equalizer_0`）的音波材质。
    /// </summary>
    /// <param name="waveTexture">
    /// `_MainTex`。传 null 也行（uGUI `Image` / `ImageQuad.SetTexture` 会在渲染时把 sprite 贴图
    /// 绑到 `_MainTex` 上）；`ImageQuad` 这条线建议直接传 `CardArt.Ui("40k_voicelines_radio_wave_equalizer")`。
    /// </param>
    /// <param name="tint">
    /// 我们加的 `_Color`（原版没有，见 shader 头 §四·1）。它**替的是原版顶点色**，
    /// 所以 shader 里和顶点色一起**乘两次**（原版 `o = v * color * color`）。
    /// 传原版那格的实际染色 **`MonoBehaviour_4509.json` 的 `m_Color` = (0.6988, 0.9811, 0.9265, 1)**
    /// 就得到与原版一致的绿色（默认白 = 白波）。
    /// </param>
    public static Material Create(Texture waveTexture = null, Color? tint = null)
    {
        return Build(SceneRestMovement, SceneXOffsets, SceneWaveScaleRemap,
                     SceneNoise1, SceneNoise2, waveTexture, tint);
    }

    /// <summary>工厂原件（`Equalizer.mat`）那一套数值 —— 只在对照/自检里用。</summary>
    public static Material CreateFactoryPreset(Texture waveTexture = null, Color? tint = null)
    {
        return Build(FactoryRestMovement, FactoryXOffsets, FactoryWaveScaleRemap,
                     FactoryNoise1, FactoryNoise2, waveTexture, tint);
    }

    static Material Build(float restMovement, Vector4 xOffsets, Vector4 waveScaleRemap,
                          Vector4 noise1, Vector4 noise2, Texture waveTexture, Color? tint)
    {
        var sh = Shader.Find(ShaderName);
        if (sh == null)
        {
            Debug.LogError("[VoiceWaveMaterial] 找不到 shader `" + ShaderName + "`"
                         + "（`Resources/VoiceWave.shader` 丢了吗？）⇒ 音波没有材质。");
            return null;
        }

        var m = new Material(sh);
        m.name = "Equalizer (VoiceWave replica)";
        if (waveTexture != null) m.mainTexture = waveTexture;

        var noise = NoiseTexture();
        if (noise != null) m.SetTexture("_Noise", noise);
        else Debug.LogWarning("[VoiceWaveMaterial] 取不到线性噪声图（`Art/card_sdf/cardback_noise`）"
                            + " ⇒ 噪声采样变成白图，波形的「每像素缩放」会退化成一个常数（⛔ 不静默）。");

        m.SetFloat("_MovementRange", restMovement);
        m.SetVector("_XOffsets", xOffsets);
        m.SetVector("_WaveScaleRemap", waveScaleRemap);
        m.SetVector("_Noise1ScaleAndSpeed", noise1);
        m.SetVector("_Noise2ScaleAndSpeed", noise2);
        m.SetColor("_Color", tint ?? Color.white);
        return m;
    }

    /// <summary>
    /// 原版 `_Noise` 那张图（`Noise Combined`，**线性采样**）。
    /// 工程里现成的等价件 = `Art/card_sdf/cardback_noise.png`（与原件逐像素相同 + `sRGBTexture: 0`）。
    /// 返回的是 Resources 里的**原资产**，只读，可共享。
    /// </summary>
    public static Texture2D NoiseTexture()
    {
        var t = CardArt.CardbackSdfNoise();
        if (t != null) return t;
        // 兜底：`cardback_noise` 万一没导进来，用同名的那张（⚠️ 那张是 sRGB，值会偏）
        var fallback = CardArt.MenuUi("Noise_Combined");
        if (fallback != null)
        {
            Debug.LogWarning("[VoiceWaveMaterial] `Art/card_sdf/cardback_noise` 缺失，退回 `ui_menu/Noise_Combined` ——"
                           + "那张是 **sRGB** 导入，噪声值与原版不同（缩放会偏），请补导线性那份。");
        }
        return fallback;
    }
}

}   // namespace CardPresentation
