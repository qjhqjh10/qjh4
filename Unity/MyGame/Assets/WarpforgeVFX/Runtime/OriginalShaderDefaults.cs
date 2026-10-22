// OriginalShaderDefaults.cs — **由 `工具/gen_shader_defaults.py` 生成，别手改**
//
// 它是什么：**原版 shader 自己声明的属性默认值**（bundle 里 `m_ParsedForm.m_PropInfo.m_Props[].m_DefValue`）。
//   只收 `WarpforgeEffectBinder.ApplyDerivedParticleDefaults` 会碰的那一组属性名（生成器里的 `RUNTIME_PROPS`）；
//   全量表在 `数据/游戏数据/original_shader_props.tsv`。
//
// 🔴 **为什么要有它（W64 · 兜底审计 B10）**：那几个「粒子派生属性」原来的值是**我们挑的**
//   （注释写着「对齐 URP 的默认状态」）—— 原版真值就在 Shader 资产里，读得到就不该用替代值。
//   ⚠️ **查不到 ≠ 默认 0**：表里没有 `(shader, prop)` = **原版这个 shader 根本没声明这一格**。
//      调用方必须把这种情况和「声明了、默认是 0」分开处理（见 `ApplyDerivedParticleDefaults` 的注释）。
//
// 现读：44 条 `(shader, prop)` · 覆盖 8 个 shader。
using UnityEngine;

namespace WarpforgeVFX
{
    public static class OriginalShaderDefaults
    {
        struct Row
        {
            public string shader;
            public string prop;
            public float x, y, z, w;
        }

        static readonly Row[] Rows =
        {
            new Row { shader = "Everguild/FX/Extra Color", prop = "_SOFTPARTICLES", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Everguild/FX/Particle Distortion Affect Transparents", prop = "_SOFTPARTICLES", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Everguild/FX/Particle Premultiply", prop = "_SOFTPARTICLES", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Everguild/FX/Particle Premultiply Greyscale Coloring", prop = "_SOFTPARTICLES", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Particles/Standard Unlit", prop = "_CameraFadeParams", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Particles/Standard Unlit", prop = "_CameraFadingEnabled", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Particles/Standard Unlit", prop = "_CameraFarFadeDistance", x = 2, y = 0, z = 0, w = 0 },
            new Row { shader = "Particles/Standard Unlit", prop = "_CameraNearFadeDistance", x = 1, y = 0, z = 0, w = 0 },
            new Row { shader = "Particles/Standard Unlit", prop = "_ColorAddSubDiff", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Particles/Standard Unlit", prop = "_ColorMode", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Particles/Standard Unlit", prop = "_SoftParticleFadeParams", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Particles/Standard Unlit", prop = "_SoftParticlesEnabled", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Particles/Standard Unlit", prop = "_SoftParticlesFarFadeDistance", x = 1, y = 0, z = 0, w = 0 },
            new Row { shader = "Particles/Standard Unlit", prop = "_SoftParticlesNearFadeDistance", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Lit", prop = "_BaseColorAddSubDiff", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Lit", prop = "_CameraFadeParams", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Lit", prop = "_CameraFadingEnabled", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Lit", prop = "_CameraFarFadeDistance", x = 2, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Lit", prop = "_CameraNearFadeDistance", x = 1, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Lit", prop = "_ColorMode", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Lit", prop = "_SoftParticleFadeParams", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Lit", prop = "_SoftParticlesEnabled", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Lit", prop = "_SoftParticlesFarFadeDistance", x = 1, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Lit", prop = "_SoftParticlesNearFadeDistance", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Simple Lit", prop = "_BaseColorAddSubDiff", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Simple Lit", prop = "_CameraFadeParams", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Simple Lit", prop = "_CameraFadingEnabled", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Simple Lit", prop = "_CameraFarFadeDistance", x = 2, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Simple Lit", prop = "_CameraNearFadeDistance", x = 1, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Simple Lit", prop = "_ColorMode", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Simple Lit", prop = "_SoftParticleFadeParams", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Simple Lit", prop = "_SoftParticlesEnabled", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Simple Lit", prop = "_SoftParticlesFarFadeDistance", x = 1, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Simple Lit", prop = "_SoftParticlesNearFadeDistance", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Unlit", prop = "_BaseColorAddSubDiff", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Unlit", prop = "_CameraFadeParams", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Unlit", prop = "_CameraFadingEnabled", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Unlit", prop = "_CameraFarFadeDistance", x = 2, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Unlit", prop = "_CameraNearFadeDistance", x = 1, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Unlit", prop = "_ColorMode", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Unlit", prop = "_SoftParticleFadeParams", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Unlit", prop = "_SoftParticlesEnabled", x = 0, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Unlit", prop = "_SoftParticlesFarFadeDistance", x = 1, y = 0, z = 0, w = 0 },
            new Row { shader = "Universal Render Pipeline/Particles/Unlit", prop = "_SoftParticlesNearFadeDistance", x = 0, y = 0, z = 0, w = 0 },
        };

        static System.Collections.Generic.Dictionary<string, Row> _byKey;

        static System.Collections.Generic.Dictionary<string, Row> ByKey
        {
            get
            {
                if (_byKey == null)
                {
                    _byKey = new System.Collections.Generic.Dictionary<string, Row>(Rows.Length);
                    foreach (var r in Rows) _byKey[r.shader + "\n" + r.prop] = r;
                }
                return _byKey;
            }
        }

        /// <summary>原版这个 shader **自己声明过**这个属性吗？声明过就给出它的默认值。</summary>
        public static bool TryGet(string shaderName, string prop, out Vector4 value)
        {
            value = Vector4.zero;
            if (string.IsNullOrEmpty(shaderName) || string.IsNullOrEmpty(prop)) return false;
            Row r;
            if (!ByKey.TryGetValue(shaderName + "\n" + prop, out r)) return false;
            value = new Vector4(r.x, r.y, r.z, r.w);
            return true;
        }

        /// <summary>同上，取 `.x`（标量属性的默认值就在 x 里）。</summary>
        public static bool TryGetFloat(string shaderName, string prop, out float value)
        {
            Vector4 v;
            if (TryGet(shaderName, prop, out v)) { value = v.x; return true; }
            value = 0f;
            return false;
        }

        /// <summary>表里有多少条（自检用）。</summary>
        public static int Count { get { return Rows.Length; } }

        /// <summary>表里覆盖多少个 shader（自检用）。</summary>
        public static int ShaderCount
        {
            get
            {
                var s = new System.Collections.Generic.HashSet<string>();
                foreach (var r in Rows) s.Add(r.shader);
                return s.Count;
            }
        }
    }
}
