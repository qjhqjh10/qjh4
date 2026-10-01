// OriginalShaderBlendTable.cs — **由 `工具/gen_shader_blend.py` 生成，别手改**
//
// 它是什么：**原版每个 shader 的 Pass0 混合状态**（从 bundle 的序列化数据里读出来的真值）。
// 为什么要它：`ArenaOriginalMaterial.InferBlendFromShaderName` 原来**按 shader 名字猜**混合模式，
//   名字里没有 additive/premultiply/multiply/alpha blended/transparent 就一律当**不透明**。
//   实测踩到的：`Cruelty Particle Custom` 的源 shader = `Everguild/FX/Particle Shine Custom Vertex Streams`
//   （pass 里写死 `SrcAlpha→OneMinusSrcAlpha`，而名字里一个关键词都没有）⇒ 被导成不透明。
//
// ⚠️ **材质驱动的那些不收**（`Blend [_SrcBlend][_DstBlend]` 这种）—— 那种 pass 里读到的 `0/0` 是占位，
//   真值在材质上（`Everguild/FX/Extra Color` 就是这一族）⇒ 交给调用方按材质值处理。
namespace WarpforgeVFX
{
    public static class OriginalShaderBlendTable
    {
        // shader 名 → { srcBlend, dstBlend, zWrite }（Unity BlendMode 的数字值）
        static readonly System.Collections.Generic.Dictionary<string, int[]> Map =
            new System.Collections.Generic.Dictionary<string, int[]>
        {
            { "Custom/EditorIcon", new[] { 5, 0, 1 } },
            { "Custom/SuperSampled Texture", new[] { 5, 10, 0 } },
            { "Everguild/Card ImageUI", new[] { 5, 10, 0 } },
            { "Everguild/Cards/3D Card", new[] { 1, 0, 1 } },
            { "Everguild/Cards/3D Card Blend Image", new[] { 1, 0, 1 } },
            { "Everguild/Cards/3D Card Dissolve", new[] { 1, 0, 1 } },
            { "Everguild/Cards/3D Card Explosion", new[] { 1, 0, 1 } },
            { "Everguild/Cards/3D Card Stealth", new[] { 5, 10, 0 } },
            { "Everguild/Cards/BlobShadow", new[] { 5, 10, 0 } },
            { "Everguild/Cards/Card Swarm Effect", new[] { 1, 0, 1 } },
            { "Everguild/Cards/Gem Crystal Glitter", new[] { 1, 0, 1 } },
            { "Everguild/Cards/Gem Crystal Glitter Explosion", new[] { 5, 10, 0 } },
            { "Everguild/Cards/Necrons Base Death", new[] { 1, 0, 1 } },
            { "Everguild/Cards/Shatter Inner Pieces", new[] { 1, 0, 1 } },
            { "Everguild/FX/Burning", new[] { 1, 0, 1 } },
            { "Everguild/FX/Burning Dissolve", new[] { 1, 0, 1 } },
            { "Everguild/FX/Card Highlight And Shadow", new[] { 5, 10, 0 } },
            { "Everguild/FX/FX Shine For Animation", new[] { 5, 10, 0 } },
            { "Everguild/FX/Floor Planar Reflections Grainny", new[] { 1, 0, 1 } },
            { "Everguild/FX/Glow Shader", new[] { 5, 10, 0 } },
            { "Everguild/FX/Particle Distortion Affect Transparents", new[] { 5, 10, 0 } },
            { "Everguild/FX/Particle Premultiply", new[] { 5, 10, 0 } },
            { "Everguild/FX/Particle Shine Custom Vertex Streams", new[] { 5, 10, 0 } },
            { "Everguild/FX/ShieldVfx", new[] { 5, 10, 0 } },
            { "Everguild/FX/Specific/Necrons Rays", new[] { 5, 10, 0 } },
            { "Everguild/FX/Specific/Pray Glow", new[] { 5, 10, 0 } },
            { "Everguild/FX/Spiral Trail FX", new[] { 5, 10, 0 } },
            { "Everguild/FX/Sprite Dissolve Mask", new[] { 5, 10, 0 } },
            { "Everguild/FX/Sprite Greyscale", new[] { 5, 10, 0 } },
            { "Everguild/FX/Sprite Scan Lines", new[] { 5, 10, 0 } },
            { "Everguild/FX/TrailShader_1", new[] { 5, 10, 0 } },
            { "Everguild/FX/TrailShader_Fading", new[] { 5, 10, 0 } },
            { "Everguild/FX/Tutorial Highlight", new[] { 5, 10, 0 } },
            { "Everguild/FX/Vortex", new[] { 5, 10, 0 } },
            { "Everguild/Misc/Unlit shadows receiver", new[] { 1, 0, 1 } },
            { "Everguild/Sprites/Sprite Additive", new[] { 1, 1, 0 } },
            { "Everguild/UI/Card ImageUI Simple", new[] { 1, 10, 0 } },
            { "Everguild/UI/Card ImageUI Simple GreyScale", new[] { 1, 10, 0 } },
            { "Everguild/UI/Card Ready for level up", new[] { 1, 1, 0 } },
            { "Everguild/UI/Color Change", new[] { 1, 10, 0 } },
            { "Everguild/UI/Greyscale", new[] { 1, 10, 0 } },
            { "Everguild/UI/UI Border Highlight Appear", new[] { 5, 10, 0 } },
            { "Everguild/UI/UI Border Highlight Appear Pulsating Scale", new[] { 5, 10, 0 } },
            { "Everguild/UI/UI Border Mask", new[] { 1, 10, 0 } },
            { "Everguild/UI/UI Distort Color Change", new[] { 1, 10, 0 } },
            { "GlassRefraction", new[] { 5, 10, 1 } },
            { "Hidden/Core/FallbackError", new[] { 1, 0, 1 } },
            { "Hidden/Shader Graph/FallbackError", new[] { 1, 0, 1 } },
            { "Hidden/Universal Render Pipeline/FallbackError", new[] { 1, 0, 1 } },
            { "Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad", new[] { 5, 10, 0 } },
            { "Shader Graphs/Eclipse Tau", new[] { 5, 10, 0 } },
            { "Shader Graphs/Equalizer", new[] { 5, 10, 0 } },
            { "Shader Graphs/Fx_ParticleDissolve_apb", new[] { 5, 10, 0 } },
            { "Shader Graphs/Fx_RockDissolve", new[] { 1, 0, 1 } },
            { "Shader Graphs/Sprite HUE Color change", new[] { 5, 10, 0 } },
            { "Shader Graphs/UI Vignete Shader", new[] { 5, 10, 0 } },
            { "Spine/Special/HiddenPass", new[] { 1, 0, 0 } },
            { "UI/Additive", new[] { 1, 1, 0 } },
        };

        /// <summary>查得到就返回 true —— 调用方**优先用它**，查不到再退回「按名字猜」。</summary>
        public static bool TryGet(string shaderName, out int srcBlend, out int dstBlend, out float zwrite)
        {
            srcBlend = 0; dstBlend = 0; zwrite = 0f;
            if (string.IsNullOrEmpty(shaderName)) return false;
            int[] v;
            if (!Map.TryGetValue(shaderName, out v) || v == null || v.Length < 3) return false;
            srcBlend = v[0]; dstBlend = v[1]; zwrite = v[2];
            return true;
        }

        /// <summary>表里有多少条（自检用）。</summary>
        public static int Count { get { return Map.Count; } }
    }
}
