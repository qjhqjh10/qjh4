// OriginalShaderBlendTable.cs — **由 `工具/gen_shader_blend.py` 生成，别手改**
//
// 它是什么：**原版每个 shader 的【第一个有程序的渲染 pass】状态**（从 bundle 的序列化数据里读出来的真值）。
//   ⚠️ 不是字面 pass0 —— `Particles/Standard Unlit` 的 pass0 是 `gpuProgramID = -1` 的空 pass，
//      照它读会把一堆 0 当真值（2026-10-12 修）；现在取第一个有程序、且不是 ShadowCaster/DepthOnly/MotionVectors 的 pass。
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
//   ⇒ **本表的消费者：`WarpforgeShaderMap.InferCull`（cull 那一格）＋ `InferZTest`（2026-10-19 加）。**
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

        /// <summary>🆕 2026-10-19（`T05`）：原版 shader 名 → **烘死在 pass 里的 `zTest`**
        /// （`0=Disabled · 2=Less · 4=LessEqual · 8=Always`，Unity `CompareFunction` 的数值）。
        /// 🔴 判据与 cull 同一条：**只收 `name` 不以 `_` 开头的那一格**（= 值烘死）；
        ///   属性驱动（`_ZTest`）的**故意不在表里** ⇒ `TryGetZTest` 返回 false、调用方不许覆盖。
        /// ⚠️ 这格原来「表里读到了、代码里没人查」；消费者 = `WarpforgeEffectBinder.Build`</summary>
        static readonly System.Collections.Generic.Dictionary<string, int> ZTestMap =
            new System.Collections.Generic.Dictionary<string, int>
        {
            { "Custom/EditorIcon", 4 },
            { "Custom/SuperSampled Texture", 4 },
            { "Everguild/Card ImageUI", 0 },
            { "Everguild/Cards/3D Card", 4 },
            { "Everguild/Cards/3D Card Blend Image", 4 },
            { "Everguild/Cards/3D Card Dissolve", 4 },
            { "Everguild/Cards/3D Card Explosion", 4 },
            { "Everguild/Cards/3D Card Stealth", 4 },
            { "Everguild/Cards/BlobShadow", 4 },
            { "Everguild/Cards/Card Swarm Effect", 4 },
            { "Everguild/Cards/Gem Crystal Glitter", 4 },
            { "Everguild/Cards/Gem Crystal Glitter Explosion", 4 },
            { "Everguild/Cards/Necrons Base Death", 4 },
            { "Everguild/Cards/Shatter Inner Pieces", 4 },
            { "Everguild/FX/Burning", 4 },
            { "Everguild/FX/Burning Dissolve", 4 },
            { "Everguild/FX/Card Highlight And Shadow", 4 },
            { "Everguild/FX/FX Shine For Animation", 4 },
            { "Everguild/FX/Floor Planar Reflections Grainny", 4 },
            { "Everguild/FX/Glow Shader", 4 },
            { "Everguild/FX/Particle Distortion Affect Transparents", 4 },
            { "Everguild/FX/Particle Premultiply", 4 },
            { "Everguild/FX/Particle Shine Custom Vertex Streams", 4 },
            { "Everguild/FX/ShieldVfx", 4 },
            { "Everguild/FX/Specific/Necrons Rays", 4 },
            { "Everguild/FX/Specific/Pray Glow", 4 },
            { "Everguild/FX/Spiral Trail FX", 4 },
            { "Everguild/FX/Sprite Dissolve Mask", 4 },
            { "Everguild/FX/Sprite Greyscale", 4 },
            { "Everguild/FX/Sprite Scan Lines", 4 },
            { "Everguild/FX/TrailShader_1", 4 },
            { "Everguild/FX/TrailShader_Fading", 4 },
            { "Everguild/FX/Tutorial Highlight", 4 },
            { "Everguild/FX/Tyranids/Tyranid Tentacle", 4 },
            { "Everguild/FX/Vortex", 4 },
            { "Everguild/Misc/Unlit shadows receiver", 4 },
            { "Everguild/Sprites/Sprite Additive", 4 },
            { "Everguild/UI/Card ImageUI Simple", 0 },
            { "Everguild/UI/Card ImageUI Simple GreyScale", 0 },
            { "Everguild/UI/Card Ready for level up", 0 },
            { "Everguild/UI/Color Change", 0 },
            { "Everguild/UI/Greyscale", 0 },
            { "Everguild/UI/UI Border Highlight Appear", 4 },
            { "Everguild/UI/UI Border Highlight Appear Pulsating Scale", 4 },
            { "Everguild/UI/UI Border Mask", 0 },
            { "Everguild/UI/UI Distort Color Change", 0 },
            { "GlassRefraction", 4 },
            { "Hidden/Core/FallbackError", 4 },
            { "Hidden/Shader Graph/FallbackError", 4 },
            { "Hidden/Universal Render Pipeline/FallbackError", 4 },
            { "Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad", 4 },
            { "Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad", 4 },
            { "Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad", 4 },
            { "Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad", 4 },
            { "Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad", 4 },
            { "Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad", 4 },
            { "Hidden/VideoComposite", 4 },
            { "Hidden/VideoDecode", 8 },
            { "Legacy Shaders/Particles/Additive", 4 },
            { "Legacy Shaders/Particles/Alpha Blended", 4 },
            { "Legacy Shaders/Particles/Alpha Blended Premultiply", 4 },
            { "Legacy Shaders/Particles/Anim Alpha Blended", 4 },
            { "Legacy Shaders/VertexLit", 4 },
            { "Mobile/Particles/Additive", 4 },
            { "Mobile/Particles/Alpha Blended", 4 },
            { "Mobile/Particles/Multiply", 4 },
            { "Particles/Standard Unlit", 4 },
            { "Shader Graphs/Eclipse Tau", 4 },
            { "Shader Graphs/Equalizer", 4 },
            { "Shader Graphs/Fx_ParticleDissolve_apb", 4 },
            { "Shader Graphs/Fx_RockDissolve", 4 },
            { "Shader Graphs/Sprite HUE Color change", 4 },
            { "Shader Graphs/UI Vignete Shader", 4 },
            { "Skybox/Cubemap", 4 },
            { "Spine/Special/HiddenPass", 4 },
            { "Sprites/Default", 4 },
            { "Sprites/Mask", 4 },
            { "UI/Additive", 0 },
            { "UI/Default", 0 },
            { "Universal Render Pipeline/Lit", 4 },
            { "Universal Render Pipeline/Particles/Lit", 4 },
            { "Universal Render Pipeline/Particles/Simple Lit", 4 },
            { "Universal Render Pipeline/Particles/Unlit", 4 },
            { "Universal Render Pipeline/Unlit", 4 },
        };

        /// <summary>查得到 = 这个 shader 的 zTest 是**烘死的**、可以覆盖；返回 false = 属性驱动，别动。</summary>
        public static bool TryGetZTest(string shaderName, out int zTest)
        {
            zTest = -1;
            if (string.IsNullOrEmpty(shaderName)) return false;
            return ZTestMap.TryGetValue(shaderName, out zTest);
        }

        /// <summary>`ZTestMap` 里有多少条（自检用）。</summary>
        public static int ZTestCount { get { return ZTestMap.Count; } }

        /// <summary>🆕 2026-10-19（`T05`）：原版 shader → **它自己声明的属性名**（空格分隔）。
        /// 用途 = 丢弃材质上的**孤儿值**（内置 Standard 残留、原 shader 属性表里根本没有的名字）。
        /// 🔴 空表 = 这个 shader 的属性表读不到 ⇒ 调用方**必须当成「不知道」**、别过滤（不许静默清空）。</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> DeclaredProps =
            new System.Collections.Generic.Dictionary<string, string>
        {
            { "Custom/EditorIcon", "_Color _MainTex" },
            { "Custom/SuperSampled Texture", "_AAScale _MainTex" },
            { "Everguild/Card ImageUI", "_Clamp_Range _Color _ColorMask _MainTex _Stencil _StencilComp _StencilOp _StencilReadMask _StencilWriteMask _UseUIAlphaClip" },
            { "Everguild/Cards/3D Card", "_BaseMap _CardImage _CountersIntensity _MatCap _MatCapPower _MatCap_Intensity _QueueControl _QueueOffset _USE_BLEND _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_TargetCard_2954523001_Texture2D unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Cards/3D Card Blend Image", "_BaseMap _Blend _BorderWidth _CardImage _ColorBorderWidth _CountersIntensity _DissolveAmount _DissolveColor _DissolveColorSecondary _DistortAmount _DistortCenter _MatCap _MatCapPower _MatCap_Intensity _Noise _QueueControl _QueueOffset _TargetImage _USE_BLEND unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Cards/3D Card Dissolve", "_AlphaClipThreshold _BaseMap _BorderColor _BorderColor2 _BorderWidth _CardImage _ClampRange _CountersIntensity _DISSOLVE_CHANNEL _DissolveAmount _DissolveTex _DissolveUVScale_XY_Offset_ZW _EnvironmentColor _MatCap _MatCapPower _MatCap_Intensity _QueueControl _QueueOffset _SCREENSPACEUV _USE_BLEND _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_TargetCard_2954523001_Texture2D unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Cards/3D Card Explosion", "_AlphaClipThreshold _BaseMap _BorderColor _BorderColor2 _BorderWidth _CardImage _ClampRange _CountersIntensity _DISSOLVE_CHANNEL _DissolveAmount _DissolveTex _DissolveUVScale_XY_Offset_ZW _Distance _MatCap _MatCapPower _MatCap_Intensity _NoiseContribution _NormalContribution _QueueControl _QueueOffset _RandomOffsetForVariation _SCREENSPACEUV _TextureOffsetModifier _USE_BLEND _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_TargetCard_2954523001_Texture2D unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Cards/3D Card Stealth", "_BaseMap _CardImage _Color _CountersIntensity _HeightFade _InteriorAlphaIntensity _InteriorAlphaMaskPosition _InteriorAlphaSpread _MatCap _MatCapPower _MatCap_Intensity _Noise1_Scale_XY_Speed_ZW _Noise2_Scale_XY_Speed_ZW _NoiseDistortion _NoiseSmoothStep _NoiseTex _QueueControl _QueueOffset _USE_BLEND _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_TargetCard_2954523001_Texture2D unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Cards/BlobShadow", "_MainTex" },
            { "Everguild/Cards/Card Swarm Effect", "_BaseMap _CardImage _ClampRange _ColorDistanceRemap _CountersIntensity _DisplacementController _DisplacementNoise _LateralMaskSharpness _MaskDisplacement _MatCap _MatCapPower _MatCap_Intensity _MaxOffset _NoiseMaskIntensity _QueueControl _QueueOffset _USE_BLEND _WarpColor _WorldUvAnimationMultiplier _WorldUvsTiling _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_TargetCard_2954523001_Texture2D unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Cards/Gem Crystal Glitter", "_FresnelColor _FresnelPower _InnerColor _MatCap _NoiseTex _Noise_1_Add_x_Pow_y _Noise_2_Add_x_Pow_y _Noise_Scale _QueueControl _QueueOffset _ScrollSpeed unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Cards/Gem Crystal Glitter Explosion", "_FresnelColor _FresnelPower _InnerColor _MatCap _NoiseTex _Noise_1_Add_x_Pow_y _Noise_2_Add_x_Pow_y _Noise_Scale _QueueControl _QueueOffset _RemapDistance _ScrollSpeed _VertexOffset unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Cards/Necrons Base Death", "_BaseMap _CardImage _ClampRange _CountersIntensity _EnvironmentColor _MatCap _MatCapPower _MatCap_Intensity _QueueControl _QueueOffset _Shatter_Color _Shatter_Smooth_Step _Shatter_Texture _USE_BLEND _VerticalFade _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_Noise_768840626_Texture2D _var3DCardColor_5016ed2ee9af478d982cd1ba9da52701_TargetCard_2954523001_Texture2D unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Cards/Shatter Inner Pieces", "_BaseMap _Color _EnvironmentColor _MatCap _MatCapPower _MatCap_Intensity _QueueControl _QueueOffset _SmoothStep _USEVAT unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Alpha Mask One Layer", "_AlphaClip _AlphaToMask _Alpha_Modifier _Alpha_Modifier_Multiply _Blend _CastShadows _Color_Mutliplier _Cull _DstBlend _DstBlendAlpha _MainTex _PrimaryTex_Scale_XY_Speed_ZW _QueueControl _QueueOffset _SecondaryTex _SecondaryTex_Scale_XY_Speed_ZW _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Alpha Masks Two Layer", "_AlphaClip _AlphaMultiplier _AlphaToMask _Alpha_Multiplier_2 _Blend _Blend_Color _Blend_Color_Opacity _CHANNELSELECTOR _CastShadows _Color_Multiplier _Cull _DstBlend _DstBlendAlpha _MainTex _NOISE1CHANNEL _NOISE2CHANNEL _Noise1ScaleAndOffset _Noise2ScaleAndOffset _NoiseTex1 _NoiseTex2 _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Burning", "_BaseMap _Color _DistortMask_Smooth_step_XY_Intensity_Z _FireNoise _Hot _Noise_Multiplier_X_smooth_step_YZ _Noise_Speed_Scale _Noise_Speed_Scale_2 _QueueControl _QueueOffset _Warm unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Burning Dissolve", "_BaseMap _BorderWidth _Color _DissolveAmmount _Dissolve_Border_Color _Dissolve_Inner_Color _DistortMask_Smooth_step_XY_Intensity_Z _FireNoise _Hot _Noise_Multiplier_X_smooth_step_YZ _Noise_Speed_Scale _Noise_Speed_Scale_2 _QueueControl _QueueOffset _Warm unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Card Highlight And Shadow", "_ALPHACHANNEL _Inner_Edge _Inner_Fallof _MainTex _NOISE_CHANNEL _Noise _NoiseIntensity _NoiseMaskScale _Noise_Speed_Scale_StepLow_StepHigh _Offset_Outline _Offset_Shadow _Outer_Edge _Outer_Fallof _Outline _Scale _ShadowColor _SpriteAlphaEdge_Inner _SpriteAlphaEdge_Outer unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Card Remnant Death Icon", "_AlphaClip _AlphaToMask _Blend _CastShadows _Color _Color_2 _Cull _DistortStrenght _DstBlend _DstBlendAlpha _Floating_Displacement _Floatting_Speed _MainTex _Noise _QueueControl _QueueOffset _Scale _Speed _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Color Gradient", "_AlphaClip _AlphaToMask _Base_Gradient _Blend _Brightness _CastShadows _Contrast _Cull _DstBlend _DstBlendAlpha _Mask _Mask_Sharpness _QueueControl _QueueOffset _Range_1 _Range_2 _Replace_Black _Replace_White _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Extra Color", "_AlphaClip _AlphaToMask _Blend _CastShadows _Color _Cull _DstBlend _DstBlendAlpha _MainTex _QueueControl _QueueOffset _SOFTPARTICLES _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/FX Shine For Animation", "Color_a142b0353b4945a49e1990789c04ea35 Texture2D_0bfdc50de139497fa85c0cd08848879a Vector1_c76b369469204ddeb1ee5d707098307c _AutoSpeed _MainTex _Mask_Size _Shine_Movement _Shine_Scale _USESCREENSPACE _USETILEMASK unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Floor Planar Reflections Grainny", "_BaseMap _BlurRadius _ColorCompensation _ExtraAmbientColor _Iterations _ReflectionColorMultiplier _ReflectionStrength _ShadowColor" },
            { "Everguild/FX/Glow Shader", "_Alpha_Strength _Alpha_Texture _Base_color _EmissionStrength _Emission_Color _QueueControl _QueueOffset _Time_multiplier unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Halo UV scroll", "Vector3_65160a41016e4bb0ad3f5043d258b214 Vector4_62056e41ff4042358d02be808b0352f9 _AlphaClip _AlphaToMask _Blend _CastShadows _Color _Cull _DstBlend _DstBlendAlpha _MainTex _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/MarkerLight", "_AXIS _AlphaClip _AlphaToMask _Alpha_Modifier _Alpha_Modifier_Multiply _Blend _CastShadows _Color _Color_Mutliplier _Cull _DistortMap _Distort_Speed _Distort_Strenght _DstBlend _DstBlendAlpha _MainTex _PrimaryTex_Scale_XY_Speed_ZW _QueueControl _QueueOffset _Ring_Color _RippleScale _RippleSmoothstep _RippleStrenght _RipplesCount _SecondaryTex _SecondaryTex_Scale_XY_Speed_ZW _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Multi Ray", "_AlphaClip _AlphaToMask _Blend _BorderFadeMultiplier _Border_Fade_XminXmax_YminYmax _CastShadows _Color1 _Color2 _Color3 _Color4 _Cull _DstBlend _DstBlendAlpha _General_Speed_Modifier _MainTex _QueueControl _QueueOffset _Remap_Color_Min_XY_Max_ZW _Scale_1_XY_Speed_ZW _Scale_2_XY_Speed_ZW _Scale_3_XY_Speed_ZW _Scale_4_XY_Speed_ZW _SrcBlend _SrcBlendAlpha _Surface _USEBORDERFADE _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Particle Dissolve Mask", "BOOLEAN_D80F51B627624648A0B3B2D563AA7541 BOOLEAN_F96BE1E681E64E0E98322A14BDBD35A3 Vector1_2780b5644c1e463480f9eca2dba65f9a Vector1_4e2dbe7bff184ada93bb896e70f4c06d Vector4_d8c31f3b5dd04c76a4cc43d5d7873e6d _Add_Color _AlphaClip _AlphaToMask _Blend _CastShadows _Color _Cull _DISSOLVE_CHANNEL _Disolve _DstBlend _DstBlendAlpha _MainTex _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Particle Dissolve Premultiply", "_AlphaClip _AlphaToMask _BaseMap _Blend _CastShadows _Cull _DstBlend _DstBlendAlpha _EmissiveColor _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Particle Distortion Affect Transparents", "_ANIMUVS _Depth_And_Fallof _DistortTex _DistortionStrength _Mask _QueueControl _QueueOffset _SOFTPARTICLES _USEMASK _UVScale _UVSpeed unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Particle Premultiply", "_Color _Depth_And_Offset _ENABLEVERTEXSTREAMS _MainTex _QueueControl _QueueOffset _SOFTPARTICLES unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Particle Premultiply Greyscale Coloring", "_AlphaClip _AlphaToMask _Blend _CastShadows _Color _Color2 _Cull _Depth_And_Offset _DstBlend _DstBlendAlpha _ENABLEVERTEXSTREAMS _MainTex _QueueControl _QueueOffset _SOFTPARTICLES _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Particle Shine Custom Vertex Streams", "BOOLEAN_7733627BA3A1447E9D9F28A82CD0E4D4 Color_a142b0353b4945a49e1990789c04ea35 Shine_Width Texture2D_0bfdc50de139497fa85c0cd08848879a Vector1_90576362d340403fa7a5cc36416fb824 Vector1_a48438ecc6b346ab85bcbad930c537e2 Vector1_c76b369469204ddeb1ee5d707098307c _MainTex _ShineScale unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Rays For Trail", "_AlphaClip _AlphaToMask _Blend _CastShadows _Color1 _Color2 _Cull _DstBlend _DstBlendAlpha _MainTex _MainTextScale _NOISECHANNEL _Noise _NoiseStrenght _QueueControl _QueueOffset _Scale_X_And_Speed_YZ _SmoothStep _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/ShieldVfx", "_Disolve _DisolveBorderColor _DissolveEdgeThickness _Dissolve_Noise_Mutliplier _ExtraSine _ExtraSineIntensity _GreenChanelVertexColorMultiplier _InteriorSpreadPosition _Interior_Spread _Interior_SpreadColor _NOISECHANNEL _Noise _Noise1_Scale_XY_Speed_ZW _Noise2_Scale_XY_Speed_ZW _Noise_Color1 _Noise_Color2 _NormalDistance _QueueControl _QueueOffset _RimColor _RimPower _ShinePulsatingSpeed _Shine_Scale_XY_Speed_ZW _Twirl_Center_XY_Offset_ZW _Twirld_Strenght _VertexNoise_Scale_XY_Speed_ZW unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Specific/Necrons Rays", "_Bottom_Color _DistortAmountBottom _DistortAmountTop _ExtraNoiseMultiplierBottom _ExtraNoiseMultiplierTop _MinTex _Noise_Bottom_Scale_And_Speed _Noise_Top_Scale_And_Speed _QueueControl _QueueOffset _Ray_Botom_Width _Ray_Top_Width _RaysHorizontalOffset _RaysVerticalOffset _SweepRaySpeed _TopColor _USEVERTEXSTREAMS unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Specific/Pray Glow", "_Bottom_Color _DistortAmountBottom _DistortAmountTop _ExtraNoiseMultiplierBottom _ExtraNoiseMultiplierTop _MinTex _Noise_Bottom_Scale_And_Speed _Noise_Top_Scale_And_Speed _QueueControl _QueueOffset _Ray_Botom_Width _Ray_Top_Width _TopColor _USEVERTEXSTREAMS _VerticalFade _VerticalFadeOffset unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Spiral Trail FX", "_Color1 _Color2 _Displacement _FXForce _Loops _MainTex _MainTextScale _NOISECHANNEL _Noise _NoiseStrenght _QueueControl _QueueOffset _Radius _Scale_XY_Speed_ZW _SmoothStep unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Sprite Dissolve Mask", "BOOLEAN_D80F51B627624648A0B3B2D563AA7541 Vector1_2780b5644c1e463480f9eca2dba65f9a Vector4_d8c31f3b5dd04c76a4cc43d5d7873e6d _Color _DISSOLVE_CHANNEL _Disolve _DissolveAmount _MainTex unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Sprite Greyscale", "_GreyScale _MainTex _QueueControl _QueueOffset unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Sprite Scan Lines", "_AntialiasScale _GlitchPercentaje _MainColorIntensityFix _MainTex _Noise _NoiseColor _NoiseIntensity _NoiseScaleAndSpeed _ScanLinesIntensity unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/TrailShader_1", "_Color01 _Color02 _DissolveScale _DissolveSpeed _MainTexture _MainTextureSpeed _Noise_Combined _QueueControl _QueueOffset _TRAILALPHACUTOFF _TrailLenght unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/TrailShader_Fading", "_Color01 _Color02 _DissolveScale _DissolveSpeed _MainTexture _MainTextureSpeed _Noise_Combined _QueueControl _QueueOffset _TailLength unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Tutorial Highlight", "_UVScale _ViewportPoint unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Tyranids/Pulsating Mesh", "_APPLYAMBIENTCOLOR _AlphaClip _AlphaToMask _Blend _CastShadows _Cull _DeformStrength _DstBlend _DstBlendAlpha _ExtraAmbientColor _FogContribution _HeartBeatMinMaxRemap _HeartBeatSpeed _MainTex _NoiseSize _NoiseSpeed _NoiseTex _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Tyranids/Tyranid Tentacle", "_APPLYAMBIENTCOLOR _Color _Displacement _ExtraAmbientColor _FogContribution _ForceMask _MOVEMENTXAXIS _MainTex _QueueControl _QueueOffset _Riple_Size _Speed _YMaskMinMax unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Unlit UV scroll", "Vector4_1 Vector4_62056e41ff4042358d02be808b0352f9 _AlphaClip _AlphaToMask _Blend _CastShadows _Color _Cull _Depth_X_Falloff_Y _DstBlend _DstBlendAlpha _FinalAlphaMultiplier _Layers_Blend_Opacity _MainTex _PREMULTIPLY _QueueControl _QueueOffset _SOFT _SecondaryTex _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/FX/Vortex", "_ClampRange _MainTex _Main_Color _NoiseScale _Offset _QueueControl _QueueOffset _Reflected_Image _Reflection_Color _RotationSpeed _Secondary_Color _TwirReflectionStrength _Twirl_Strength _Zoom unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Matcap/Matcap Full Options", "_AlphaClip _AlphaClipThreshold _AlphaToMask _Blend _BorderColor1 _BorderColor2 _BorderWidth _CastShadows _Color _Cull _DissolveAmount _DstBlend _DstBlendAlpha _EmissionColor _EmissionTex _Intensity _MainTex _MatCap _NOISECHANNEL _Noise _Normal _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _USEEMISSION _USENOISE _USENORMAL _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Matcap/Matcap Full Options VAT", "_AlphaClip _AlphaClipThreshold _AlphaToMask _Blend _BorderColor1 _BorderColor2 _BorderWidth _CastShadows _Color _Cull _DissolveAmount _DstBlend _DstBlendAlpha _EmissionColor _EmissionTex _Intensity _MainTex _MatCap _NOISECHANNEL _Noise _Normal _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _USEEMISSION _USENOISE _USENORMAL _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Matcap/Matcap With Texture", "_APPLYAMBIENTCOLOR _AlphaClip _AlphaToMask _Blend _CastShadows _Color _Cull _DstBlend _DstBlendAlpha _ExtraAmbientColor _FogContribution _Intensity _MainTex _MatCap _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Misc/Unlit shadows receiver", "_BaseMap _ExtraAmbientColor _ShadowColor" },
            { "Everguild/Sprites/Sprite Additive", "PixelSnap _AlphaTex _Color _EnableExternalAlpha _Flip _MainTex _RendererColor" },
            { "Everguild/UI/Campaign Points", "_AlphaClip _AlphaToMask _Blend _CastShadows _Center_rotation _Color1 _Color2 _Cull _DstBlend _DstBlendAlpha _MainTex _NoiseTex _Noise_Scale_XY_Speed_ZW _QueueControl _QueueOffset _Rotation_Speed _Rotation_Speed_2 _Rotation_Speed_Noise _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/UI/Card ImageUI Simple", "_Color _ColorMask _MainTex _Stencil _StencilComp _StencilOp _StencilReadMask _StencilWriteMask _UseUIAlphaClip" },
            { "Everguild/UI/Card ImageUI Simple GreyScale", "_Color _ColorMask _GreyScale _MainTex _Stencil _StencilComp _StencilOp _StencilReadMask _StencilWriteMask _UseUIAlphaClip" },
            { "Everguild/UI/Card Ready for level up", "_BottomFade _Color _ColorA _ColorB _ColorG _ColorMask _ColorR _MainTex _SpeedScaleLayerA _SpeedScaleLayerB _SpeedScaleLayerG _SpeedScaleLayerR _Stencil _StencilComp _StencilOp _StencilReadMask _StencilWriteMask _UseUIAlphaClip _VerticalFadeBlueAlpha _VerticalFadeRedGreen" },
            { "Everguild/UI/Color Change", "_Brightness _Color _ColorMask _Contrast _HUE _MainTex _Saturation _Stencil _StencilComp _StencilOp _StencilReadMask _StencilWriteMask _UseUIAlphaClip" },
            { "Everguild/UI/Greyscale", "_Color _ColorMask _GreyScale _MainTex _Stencil _StencilComp _StencilOp _StencilReadMask _StencilWriteMask _UseUIAlphaClip" },
            { "Everguild/UI/UI Border Highlight Appear", "_AUTOROTATION _AutoRotationSpeed _ColorMain _ColorSecondary _Color_offsets _DEBUG _FLIPDIRECTION _Noise_R _Noise_Strength_X_Speed_Y_Scale_Z _QueueControl _QueueOffset _Reveal _Rotation _SMOOTHGRADIENT _ScaleFromCenter _Smoothness _StartOffset unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/UI/UI Border Highlight Appear Pulsating Scale", "_AUTOROTATION _AutoRotationSpeed _ColorMain _ColorSecondary _Color_offsets _DEBUG _FLIPDIRECTION _Noise_R _Noise_Strength_X_Speed_Y_Scale_Z _QueueControl _QueueOffset _Reveal _Rotation _Scale_X_Speed_Y _Smoothness _StartOffset unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/UI/UI Border Mask", "_Color _ColorMask _MainTex _Stencil _StencilComp _StencilOp _StencilReadMask _StencilWriteMask _UseUIAlphaClip _maskData _maskMultiplier" },
            { "Everguild/UI/UI Distort Color Change", "_Brightness _Color _ColorMask _Contrast _DistortionAmount _DistortionSpeedAndScale _HUE _MainTex _NoiseTex _Saturation _Stencil _StencilComp _StencilOp _StencilReadMask _StencilWriteMask _UseUIAlphaClip" },
            { "Everguild/Unlit Wind", "Vector1_d24785e7c76f4f12925b2f74dd719668 Vector1_e2a1250f285e4e4c8b276ed6b053e097 Vector1_fbd31611b0de4f50ba79632772d9ba9f _APPLYAMBIENTCOLOR _AlphaClip _AlphaToMask _Blend _CastShadows _ClipThreshold _Color _Cull _DstBlend _DstBlendAlpha _ExtraAmbientColor _FogContribution _FogContribution_2 _MainTex _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/UnlitAmbient", "_APPLYAMBIENTCOLOR _AlphaClip _AlphaToMask _BaseMap _Blend _CastShadows _ClipThreshold _Color _Cull _DistortSpeed_XY_Scale_ZW _DistortStrength _DistortTex _DistortionStrength _DistortionStrength_1 _DstBlend _DstBlendAlpha _ENABLEMATCAP _ExtraAmbientColor _FogContribution _Matacap _MatcapIntensity _QueueControl _QueueOffset _ROTATEUVS _RotationSpeed _SrcBlend _SrcBlendAlpha _Surface _USEDISTORT _UVSpeed _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/UnlitAmbient Emissive Flickker", "_APPLYAMBIENTCOLOR _AlphaClip _AlphaToMask _BaseMap _Blend _CastShadows _ClipThreshold _Color _Cull _DstBlend _DstBlendAlpha _EmissiveColor _ExtraAmbientColor _FlickerMinMaxRange _FlickerSpeed _FogContribution _QueueControl _QueueOffset _RECEIVESHADOWS _SampleTexture2D_56151bd863ba4ebfae5e17e92f13cf49_Texture_1_Texture2D _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Everguild/Wind Matcap", "Vector1_d24785e7c76f4f12925b2f74dd719668 Vector1_e2a1250f285e4e4c8b276ed6b053e097 Vector1_fbd31611b0de4f50ba79632772d9ba9f _AlphaClip _AlphaToMask _Blend _CastShadows _ClipThreshold _Color _Cull _DstBlend _DstBlendAlpha _MainTex _Matcap _MatcapIntensity _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "GlassRefraction", "_AlphaCutoff _Color0 _EmissionColor _Normals _Scale _texcoord" },
            { "Hidden/Core/FallbackError", "" },
            { "Hidden/Shader Graph/FallbackError", "" },
            { "Hidden/Universal Render Pipeline/FallbackError", "" },
            { "Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad", "" },
            { "Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad", "" },
            { "Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad", "" },
            { "Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad", "" },
            { "Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad", "" },
            { "Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad", "" },
            { "Hidden/VideoComposite", "_MainTex" },
            { "Hidden/VideoDecode", "_MainTex _SecondTex _ThirdTex" },
            { "Legacy Shaders/Particles/Additive", "_InvFade _MainTex _TintColor" },
            { "Legacy Shaders/Particles/Alpha Blended", "_InvFade _MainTex _TintColor" },
            { "Legacy Shaders/Particles/Alpha Blended Premultiply", "_InvFade _MainTex" },
            { "Legacy Shaders/Particles/Anim Alpha Blended", "_InvFade _MainTex _TintColor" },
            { "Legacy Shaders/VertexLit", "_Color _Emission _MainTex _Shininess _SpecColor" },
            { "Mobile/Particles/Additive", "_MainTex" },
            { "Mobile/Particles/Alpha Blended", "_MainTex" },
            { "Mobile/Particles/Multiply", "_MainTex" },
            { "Particles/Standard Unlit", "_BlendOp _BumpMap _BumpScale _CameraFadeParams _CameraFadingEnabled _CameraFarFadeDistance _CameraNearFadeDistance _Color _ColorAddSubDiff _ColorMode _Cull _Cutoff _DistortionBlend _DistortionEnabled _DistortionStrength _DistortionStrengthScaled _DstBlend _EmissionColor _EmissionEnabled _EmissionMap _FlipbookMode _LightingEnabled _MainTex _Mode _SoftParticleFadeParams _SoftParticlesEnabled _SoftParticlesFarFadeDistance _SoftParticlesNearFadeDistance _SrcBlend _ZWrite" },
            { "Shader Graphs/Doomweaver effect", "_AlphaClip _AlphaToMask _Blend _BorderSharpness _CastShadows _Cull _DstBlend _DstBlendAlpha _ExtraBorderColorMultiplier _ExtraColorMultiplier _FadeInBorderColor _FadeInBorderWidth _Float _MainTex _QueueControl _QueueOffset _SrcBlend _SrcBlendAlpha _Surface _ZTest _ZWrite _ZWriteControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Shader Graphs/Eclipse Tau", "_APPLYAMBIENTCOLOR _EclipseColor _EclipseColorBorder _EclipseRange _ExtraAmbientColor _FogContribution _MainColor _MainTex _Scale unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Shader Graphs/Equalizer", "_MainTex _MovementRange _Noise _Noise1ScaleAndSpeed _Noise2ScaleAndSpeed _WaveScaleRemap _XOffsets unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Shader Graphs/Fx_ParticleDissolve_apb", "Boolean_52F3CBA5 Texture2D_F593E37E _QueueControl _QueueOffset unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Shader Graphs/Fx_RockDissolve", "Texture2D_EDA87E5 _QueueControl _QueueOffset unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Shader Graphs/Sprite HUE Color change", "_Brightness _Contrast _Hue_Offset _MainTex _Saturation unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Shader Graphs/UI Vignete Shader", "_AFFECTSALPHA _AffectColor _FinalAlpha _Hardness _INVERTALPHA _Noise _NoiseSize _Offset _QueueControl _QueueOffset _Radius _SmoothStep _USEMAINTEX _UVScale unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Skybox/Cubemap", "_Exposure _Rotation _Tex _Tint" },
            { "Spine/Special/HiddenPass", "" },
            { "Sprites/Default", "PixelSnap _AlphaTex _Color _EnableExternalAlpha _Flip _MainTex _RendererColor" },
            { "Sprites/Mask", "PixelSnap _AlphaTex _Color _Cutoff _EnableExternalAlpha _MainTex" },
            { "UI/Additive", "_Color _ColorMask _MainTex _Stencil _StencilComp _StencilOp _StencilReadMask _StencilWriteMask _UseUIAlphaClip" },
            { "UI/Default", "_Color _ColorMask _MainTex _Stencil _StencilComp _StencilOp _StencilReadMask _StencilWriteMask _UseUIAlphaClip" },
            { "Universal Render Pipeline/Lit", "_AddPrecomputedVelocity _AlphaClip _AlphaToMask _BaseColor _BaseMap _Blend _BlendModePreserveSpecular _BumpMap _BumpScale _ClearCoatMask _ClearCoatSmoothness _Color _Cull _Cutoff _DetailAlbedoMap _DetailAlbedoMapScale _DetailMask _DetailNormalMap _DetailNormalMapScale _DstBlend _DstBlendAlpha _EmissionColor _EmissionMap _EnvironmentReflections _GlossMapScale _Glossiness _GlossyReflections _MainTex _Metallic _MetallicGlossMap _OcclusionMap _OcclusionStrength _Parallax _ParallaxMap _QueueOffset _ReceiveShadows _Smoothness _SmoothnessTextureChannel _SpecColor _SpecGlossMap _SpecularHighlights _SrcBlend _SrcBlendAlpha _Surface _WorkflowMode _XRMotionVectorsPass _ZWrite unity_Lightmaps unity_LightmapsInd unity_ShadowMasks" },
            { "Universal Render Pipeline/Particles/Lit", "_AlphaClip _AlphaToMask _BaseColor _BaseColorAddSubDiff _BaseMap _Blend _BlendModePreserveSpecular _BlendOp _BumpMap _BumpScale _CameraFadeParams _CameraFadingEnabled _CameraFarFadeDistance _CameraNearFadeDistance _Color _ColorMode _Cull _Cutoff _DistortionBlend _DistortionEnabled _DistortionStrength _DistortionStrengthScaled _DstBlend _DstBlendAlpha _EmissionColor _EmissionMap _FlipbookBlending _FlipbookMode _Glossiness _Metallic _MetallicGlossMap _Mode _QueueOffset _ReceiveShadows _Smoothness _SoftParticleFadeParams _SoftParticlesEnabled _SoftParticlesFarFadeDistance _SoftParticlesNearFadeDistance _SrcBlend _SrcBlendAlpha _Surface _ZWrite" },
            { "Universal Render Pipeline/Particles/Simple Lit", "_AlphaClip _AlphaToMask _BaseColor _BaseColorAddSubDiff _BaseMap _Blend _BlendOp _BumpMap _BumpScale _CameraFadeParams _CameraFadingEnabled _CameraFarFadeDistance _CameraNearFadeDistance _Color _ColorMode _Cull _Cutoff _DistortionBlend _DistortionEnabled _DistortionStrength _DistortionStrengthScaled _DstBlend _DstBlendAlpha _EmissionColor _EmissionMap _FlipbookBlending _FlipbookMode _Glossiness _Mode _QueueOffset _ReceiveShadows _Smoothness _SmoothnessSource _SoftParticleFadeParams _SoftParticlesEnabled _SoftParticlesFarFadeDistance _SoftParticlesNearFadeDistance _SpecColor _SpecGlossMap _SpecularHighlights _SrcBlend _SrcBlendAlpha _Surface _ZWrite" },
            { "Universal Render Pipeline/Particles/Unlit", "_AlphaClip _AlphaToMask _BaseColor _BaseColorAddSubDiff _BaseMap _Blend _BlendOp _BumpMap _CameraFadeParams _CameraFadingEnabled _CameraFarFadeDistance _CameraNearFadeDistance _Color _ColorMode _Cull _Cutoff _DistortionBlend _DistortionEnabled _DistortionStrength _DistortionStrengthScaled _DstBlend _DstBlendAlpha _EmissionColor _EmissionMap _FlipbookBlending _FlipbookMode _Mode _QueueOffset _SoftParticleFadeParams _SoftParticlesEnabled _SoftParticlesFarFadeDistance _SoftParticlesNearFadeDistance _SrcBlend _SrcBlendAlpha _Surface _ZWrite" },
            { "Universal Render Pipeline/Unlit", "_AddPrecomputedVelocity _AlphaClip _AlphaToMask _BaseColor _BaseMap _Blend _BlendOp _Color _Cull _Cutoff _DstBlend _DstBlendAlpha _MainTex _QueueOffset _SampleGI _SrcBlend _SrcBlendAlpha _Surface _XRMotionVectorsPass _ZWrite" },
            { "Unlit/ScrollingTextures", "_Color _CullMode _DstBlend _Gradient _MainTex _MainTex2 _Mask _NoiseMask _Outline _OutlineColor _Speed _Speed2 _SrcBlend _UpColor _Width _ZTest _ZWrite" },
        };

        static System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>> _propCache;

        /// <summary>这个原版 shader **自己声明过**的属性名集合；返回 null = 属性表读不到 / 这个 shader 不在表里。
        /// ⚠️ `X_ST`（纹理的缩放/偏移，Unity 隐式生成）与 `X_TexelSize` 按**基名** `X` 判——
        ///   它们本来就不会写进 Shader 的 `m_PropInfo`，照字面判会把活的 `_MainTex_ST` 一起丢掉。</summary>
        public static System.Collections.Generic.HashSet<string> OriginalProperties(string shaderName)
        {
            if (string.IsNullOrEmpty(shaderName)) return null;
            if (_propCache == null) _propCache =
                new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>>();
            System.Collections.Generic.HashSet<string> s;
            if (_propCache.TryGetValue(shaderName, out s)) return s;
            string raw;
            if (!DeclaredProps.TryGetValue(shaderName, out raw) || string.IsNullOrEmpty(raw))
            {
                _propCache[shaderName] = null;   // 读不到 ⇒ 「不知道」⇒ 不过滤
                return null;
            }
            s = new System.Collections.Generic.HashSet<string>(raw.Split(' '));
            _propCache[shaderName] = s;
            return s;
        }
    }
}
