using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>
    /// 原版 `MaterialFlickerEffect` 的复刻（13 个战场共 **38 个**对象挂着它：火把 / 地面 / 塔楼 / 木桶 / 发电机自发光）。
    ///
    /// **算式**（`d:/2/tools/decomp_full/MaterialFlickerEffect__Update.c` + `.rdata` 常量，逐条对齐过）：
    /// <code>
    /// t     = time + desync
    /// th    = t * frequency
    /// x     = th + 0.25 * sin(0.35 * th)
    /// noise = amplitude * ( 0.35*sin(1.67*x + 0.15) + 0.59*sin(x + 0.52)
    ///                     + 0.28*sin(3.24*x - 0.11) + 0.17*sin(7.02*x - 0.42) )
    /// alpha = (noise + 1.0) * 原alpha * (fade - 0.25) / 0.75     // playOnAwake=1 ⇒ fade 恒 1
    /// </code>
    /// **已用原版实况验过**：那两个「烘焙光斑」的 `material.color.a` 在 **0.20~2.03** 之间动、
    /// 22 次采样均值 **1.035 / 1.118**，与本算式（均值 1.000）吻合 ⇒ 常量与四个正弦权重都对。
    /// 判定正本 = `资料/战场13场_逐场对账_0920.md` §一 ①-a；数据表 = `Editor/FlickerData.gen.cs`。
    ///
    /// ⚠️ **两条口径**：
    /// 1. `desync` 原版是 `Random.Range(0,100)`（**不可复现**）—— 我们改成按 (场, 名字) 定死
    ///    （生成器算的），好让同一份构建每次渲出来一模一样。**这一处是「我们挑的」**。
    /// 2. 写颜色用 `GetVector/SetVector` 而不是 `material.color`：后者在 Linear 色彩空间里
    ///    **会对 RGB 再做一次 sRGB→线性转换**（本工程在 `ArenaOriginalMaterial` 上栽过）。
    ///    原版写的是同一个值，只是不经过那一层 —— 用 Vector 才和它逐位一致（alpha 本来就不做转换）。
    /// </summary>
    [DisallowMultipleComponent]
    public class WFMaterialFlicker : MonoBehaviour
    {
        public float amplitude   = 1f;
        public float frequency   = 1f;
        public float fadeInTime  = 1f;
        public float fadeOutTime = 1f;
        public bool  randomStart = false;
        public float desync      = 0f;
        public bool  playOnAwake = true;

        /// <summary>诊断/自检用：≥0 时 `Update` 用它当时间。**批处理下没有帧循环**，靠它把相位推出来。</summary>
        public static float TimeOverride = -1f;

        Renderer _r;
        Material _mat;
        Vector4  _base;          // 原颜色的 RGB(A) —— alpha 里那个「原α」的来源
        float    _origAlpha = 1f;
        float    _fade = 1f;
        bool     _playing;

        /// <summary>最近一次算出来的 alpha（自检读它）。</summary>
        public float CurrentAlpha { get; private set; }

        void Awake()
        {
            _r = GetComponent<Renderer>();
            if (_r == null) return;

            _mat = _r.material;                       // ⚠️ 实例（原版也是 `.material`，会实例化一份）
            if (_mat != null)
            {
                _base = _mat.HasProperty("_BaseColor") ? _mat.GetVector("_BaseColor")
                                                       : (Vector4)_mat.color;
                _origAlpha = _base.w;
            }
            _playing = playOnAwake;
            _fade = playOnAwake ? 1f : 0f;
            _r.enabled = playOnAwake;                 // 原版 `Awake` 里就是这一句
            CurrentAlpha = playOnAwake ? _origAlpha : 0f;
        }

        void Update() { Tick(TimeOverride >= 0f ? TimeOverride : Time.time); }

        /// <summary>按给定时间算一次并把 alpha 写进材质（原版 `Update` 的全部内容）。</summary>
        public void Tick(float time)
        {
            if (_mat == null) return;

            float dt = (_playing ? 1f / Mathf.Max(fadeInTime, 1e-4f) : -1f / Mathf.Max(fadeOutTime, 1e-4f)) * Time.deltaTime;
            // 批处理/自检下 `Time.deltaTime` 可能是 0 ⇒ fade 不动（playOnAwake=1 时 fade 本来就是 1，无影响）
            if (TimeOverride >= 0f) dt = 0f;
            _fade = Mathf.Clamp01(_fade + dt);

            float th = (time + desync) * frequency;
            float x  = th + 0.25f * Mathf.Sin(0.35f * th);
            float noise = amplitude * (0.35f * Mathf.Sin(1.67f * x + 0.15f)
                                     + 0.59f * Mathf.Sin(x + 0.52f)
                                     + 0.28f * Mathf.Sin(3.24f * x - 0.11f)
                                     + 0.17f * Mathf.Sin(7.02f * x - 0.42f));
            float a = (noise + 1f) * _origAlpha * ((_fade - 0.25f) / 0.75f);

            var v = _base; v.w = a;
            if (_mat.HasProperty("_BaseColor")) _mat.SetVector("_BaseColor", v);
            else _mat.SetVector("_Color", v);
            CurrentAlpha = a;

            // 原版：fade 归零就把渲染器关掉（playOnAwake=1 时永远走不到）
            if (_fade <= 0f && _r != null) _r.enabled = false;
        }

        /// <summary>自检用：算一遍**不改材质**，返回 alpha。判据与 `Tick` 共用同一段（避免两份算式）。</summary>
        public float SampleAlpha(float time)
        {
            float th = (time + desync) * frequency;
            float x  = th + 0.25f * Mathf.Sin(0.35f * th);
            float noise = amplitude * (0.35f * Mathf.Sin(1.67f * x + 0.15f)
                                     + 0.59f * Mathf.Sin(x + 0.52f)
                                     + 0.28f * Mathf.Sin(3.24f * x - 0.11f)
                                     + 0.17f * Mathf.Sin(7.02f * x - 0.42f));
            return (noise + 1f) * _origAlpha * ((_fade - 0.25f) / 0.75f);
        }
    }
}
