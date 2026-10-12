// OriginalShaderBlendTable.cs — **由 `工具/gen_shader_blend.py` 生成，别手改**
//
// 它是什么：**原版每个 shader 的 Pass0 渲染状态**（从 bundle 的序列化数据里读出来的真值）。
// 为什么要它：`ArenaOriginalMaterial.InferBlendFromShaderName` 原来**按 shader 名字猜**混合模式，
//   名字里没有 additive/premultiply/multiply/alpha blended/transparent 就一律当**不透明**。
//   实测踩到的：`Cruelty Particle Custom` 的源 shader = `Everguild/FX/Particle Shine Custom Vertex Streams`
//   （pass 里写死 `SrcAlpha→OneMinusSrcAlpha`，而名字里一个关键词都没有）⇒ 被导成不透明。
//
// ⚠️ **材质驱动的那些不收**（`Blend [_SrcBlend][_DstBlend]` 这种）—— 那种 pass 里读到的 `0/0` 是占位，
//   真值在材质上（`Everguild/FX/Extra Color` 就是这一族）⇒ 交给调用方按材质值处理。
// ⚠️ **2026-10-01：本表那条「先查表再覆盖混合」的实验在【战场那条路】上被 sweep 判定为净变差并回退** ——
//   事实经过见 `WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs` 的 `InferBlendFromShaderName` 注释。
//   表本身没错（是实读），错在「目标 shader 已经是兜底的那一个、混合照抄原件不等于画面对」。
//   ⇒ **本表今天的消费者只有一个：`WarpforgeShaderMap.InferCull`**（cull 那一格，与混合无关）。
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
            { "Everguild/FX/Tyranids/Tyranid Tentacle", new[] { 5, 10, 1 } },
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
            { "Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad", new[] { 5, 10, 0 } },
            { "Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad", new[] { 5, 1, 0 } },
            { "Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad", new[] { 5, 1, 0 } },
            { "Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad", new[] { 5, 1, 0 } },
            { "Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad", new[] { 5, 1, 0 } },
            { "Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad", new[] { 5, 10, 0 } },
            { "Hidden/VideoComposite", new[] { 5, 10, 1 } },
            { "Hidden/VideoDecode", new[] { 1, 0, 0 } },
            { "Legacy Shaders/Particles/Additive", new[] { 5, 1, 0 } },
            { "Legacy Shaders/Particles/Alpha Blended", new[] { 5, 10, 0 } },
            { "Legacy Shaders/Particles/Alpha Blended Premultiply", new[] { 1, 10, 0 } },
            { "Legacy Shaders/Particles/Anim Alpha Blended", new[] { 5, 10, 0 } },
            { "Legacy Shaders/VertexLit", new[] { 1, 0, 1 } },
            { "Mobile/Particles/Additive", new[] { 5, 1, 0 } },
            { "Mobile/Particles/Alpha Blended", new[] { 5, 10, 0 } },
            { "Mobile/Particles/Multiply", new[] { 0, 3, 0 } },
            { "Shader Graphs/Eclipse Tau", new[] { 5, 10, 0 } },
            { "Shader Graphs/Equalizer", new[] { 5, 10, 0 } },
            { "Shader Graphs/Fx_ParticleDissolve_apb", new[] { 5, 10, 0 } },
            { "Shader Graphs/Fx_RockDissolve", new[] { 1, 0, 1 } },
            { "Shader Graphs/Sprite HUE Color change", new[] { 5, 10, 0 } },
            { "Shader Graphs/UI Vignete Shader", new[] { 5, 10, 0 } },
            { "Skybox/Cubemap", new[] { 1, 0, 0 } },
            { "Spine/Special/HiddenPass", new[] { 1, 0, 0 } },
            { "Sprites/Default", new[] { 1, 10, 0 } },
            { "Sprites/Mask", new[] { 1, 0, 0 } },
            { "UI/Additive", new[] { 1, 1, 0 } },
            { "UI/Default", new[] { 1, 10, 0 } },
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

        /// <summary>🆕 2026-10-12：原版 shader 名 → **烘死在 pass 里的 `cull` 值**（`0=Off · 1=Front · 2=Back`）。
        /// 🔴 **只收 `name` 不以 `_` 开头的那一格**（= 不是属性引用、值烘死）——
        ///   属性驱动的那种（`Everguild/*` 的 `_Cull`）**故意不在表里** ⇒ `TryGetCull` 返回 false、
        ///   `InferCull` 就该**不覆盖**。判据与实读表 → `WarpforgeShaderMap.InferCull` 的注释。</summary>
        static readonly System.Collections.Generic.Dictionary<string, int> CullMap =
            new System.Collections.Generic.Dictionary<string, int>
        {
            { "Custom/EditorIcon", 2 },
            { "Custom/SuperSampled Texture", 0 },
            { "Everguild/Card ImageUI", 0 },
            { "Everguild/Cards/3D Card", 2 },
            { "Everguild/Cards/3D Card Blend Image", 2 },
            { "Everguild/Cards/3D Card Dissolve", 2 },
            { "Everguild/Cards/3D Card Explosion", 2 },
            { "Everguild/Cards/3D Card Stealth", 2 },
            { "Everguild/Cards/BlobShadow", 2 },
            { "Everguild/Cards/Card Swarm Effect", 2 },
            { "Everguild/Cards/Gem Crystal Glitter", 2 },
            { "Everguild/Cards/Gem Crystal Glitter Explosion", 2 },
            { "Everguild/Cards/Necrons Base Death", 2 },
            { "Everguild/Cards/Shatter Inner Pieces", 2 },
            { "Everguild/FX/Burning", 2 },
            { "Everguild/FX/Burning Dissolve", 2 },
            { "Everguild/FX/Card Highlight And Shadow", 0 },
            { "Everguild/FX/FX Shine For Animation", 0 },
            { "Everguild/FX/Floor Planar Reflections Grainny", 2 },
            { "Everguild/FX/Glow Shader", 2 },
            { "Everguild/FX/Particle Distortion Affect Transparents", 2 },
            { "Everguild/FX/Particle Premultiply", 2 },
            { "Everguild/FX/Particle Shine Custom Vertex Streams", 0 },
            { "Everguild/FX/ShieldVfx", 2 },
            { "Everguild/FX/Specific/Necrons Rays", 0 },
            { "Everguild/FX/Specific/Pray Glow", 0 },
            { "Everguild/FX/Spiral Trail FX", 0 },
            { "Everguild/FX/Sprite Dissolve Mask", 0 },
            { "Everguild/FX/Sprite Greyscale", 2 },
            { "Everguild/FX/Sprite Scan Lines", 0 },
            { "Everguild/FX/TrailShader_1", 0 },
            { "Everguild/FX/TrailShader_Fading", 0 },
            { "Everguild/FX/Tutorial Highlight", 0 },
            { "Everguild/FX/Tyranids/Tyranid Tentacle", 2 },
            { "Everguild/FX/Vortex", 2 },
            { "Everguild/Misc/Unlit shadows receiver", 2 },
            { "Everguild/Sprites/Sprite Additive", 0 },
            { "Everguild/UI/Card ImageUI Simple", 0 },
            { "Everguild/UI/Card ImageUI Simple GreyScale", 0 },
            { "Everguild/UI/Card Ready for level up", 0 },
            { "Everguild/UI/Color Change", 0 },
            { "Everguild/UI/Greyscale", 0 },
            { "Everguild/UI/UI Border Highlight Appear", 2 },
            { "Everguild/UI/UI Border Highlight Appear Pulsating Scale", 2 },
            { "Everguild/UI/UI Border Mask", 0 },
            { "Everguild/UI/UI Distort Color Change", 0 },
            { "GlassRefraction", 0 },
            { "Hidden/Core/FallbackError", 2 },
            { "Hidden/Shader Graph/FallbackError", 2 },
            { "Hidden/Universal Render Pipeline/FallbackError", 2 },
            { "Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad", 0 },
            { "Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad", 0 },
            { "Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad", 0 },
            { "Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad", 0 },
            { "Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad", 0 },
            { "Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad", 0 },
            { "Hidden/VideoComposite", 0 },
            { "Hidden/VideoDecode", 0 },
            { "Legacy Shaders/Particles/Additive", 0 },
            { "Legacy Shaders/Particles/Alpha Blended", 0 },
            { "Legacy Shaders/Particles/Alpha Blended Premultiply", 0 },
            { "Legacy Shaders/Particles/Anim Alpha Blended", 0 },
            { "Legacy Shaders/VertexLit", 2 },
            { "Mobile/Particles/Additive", 0 },
            { "Mobile/Particles/Alpha Blended", 0 },
            { "Mobile/Particles/Multiply", 0 },
            { "Shader Graphs/Eclipse Tau", 0 },
            { "Shader Graphs/Equalizer", 0 },
            { "Shader Graphs/Fx_ParticleDissolve_apb", 0 },
            { "Shader Graphs/Fx_RockDissolve", 2 },
            { "Shader Graphs/Sprite HUE Color change", 0 },
            { "Shader Graphs/UI Vignete Shader", 2 },
            { "Skybox/Cubemap", 0 },
            { "Spine/Special/HiddenPass", 2 },
            { "Sprites/Default", 0 },
            { "Sprites/Mask", 0 },
            { "UI/Additive", 0 },
            { "UI/Default", 0 },
        };

        /// <summary>查得到 = 这个 shader 的 cull 是**烘死的**、可以覆盖；返回 false = 属性驱动，别动。</summary>
        public static bool TryGetCull(string shaderName, out int cull)
        {
            cull = -1;
            if (string.IsNullOrEmpty(shaderName)) return false;
            return CullMap.TryGetValue(shaderName, out cull);
        }

        /// <summary>`CullMap` 里有多少条（自检用）。</summary>
        public static int CullCount { get { return CullMap.Count; } }
    }
}
