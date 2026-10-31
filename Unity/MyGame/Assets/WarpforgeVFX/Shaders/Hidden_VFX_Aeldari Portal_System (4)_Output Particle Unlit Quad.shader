// Hidden_VFX_Aeldari Portal_System (4)_Output Particle Unlit Quad.shader
// 名称 = `Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad`（原版 VFX Graph 输出 shader 的复刻件 · Aeldari Portal · System (4)）
//
// 唯一来源（第一权威）= Ruri 真反编译体（SPIR-V → HLSL 交叉编译产物）：
//   `D:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/1e59d1729d67847d186ed2dfccb91988_0.shader`
//   原生 shader 本体另在随包 bundle 里（`StreamingAssets/WarpforgeVFX/wf_arena_shaders.bundle` /
//   `wf_shaders_extra.bundle`，按名可取）；但 Unity 拒绝把 bundle 里的 Shader 序列化成工程资产
//   （项目已知坑），而 `.mat` 的 `m_Shader` 只认工程资产 ⇒ 按 R4-04 的「手写 6 个 shader」路线，
//   把原版反编译体**原样搬进工程**。
//
// 只做了**可移植性归一**（逐条都可逆、无行为差异）：
//   ① 去掉 `#pragma use_dxc` —— 不是 Unity 认的 pragma（Unity 行的是 `never_use_dxc`），让 Unity 自选编译器；
//   ② `register(tN, space0)` → `register(tN)` —— register space 是 DXC/SPIR-V 写法，FXC 不认（槽位不变）；
//   ③ `precise float` → `float` —— 指令级精度提示，不改变数值结果；
//   ④ `float(uint64_t(x))` → `float(x)` —— 同一个 uint→float 转换，去掉 64 位依赖；
//   ⑤ `SPIRV_Cross_BaseVertex/BaseInstance` → 0 —— VFX 走 DrawProcedural，基础顶点/实例恒为 0。
//   ⑥ `ByteAddressBuffer.Load<uint>(x)` → `.Load(x)` —— 同一语义（非模板重载返回 uint），FXC 不认模板形式；
//   ⑦ 片元块里的全局 `static float4 SV_Target;` → `spv_Color` —— HLSLSupport.cginc 在部分编译档把 `SV_Target`
//      宏映射成 `COLOR`，会与本块已有的 `static float4 COLOR;` 重定义；只改**变量名**，输出语义（SV_Target0）不变。
//   **未改**：Shader 名 · SubShader Tags · Pass 名/Tags · Blend/ZWrite/Cull/ZClip · 顶点/片元算式。
//
// 🔑 **`.meta` 的 guid 刻意沿用【原版 shader 的 guid】**：`9c6a6f6750d7ff94e8669f2fdea8a8f0`
//   —— 6 个 `.mat` 的 `m_Shader` 里本来就写着这个 guid（原版遗留、一直悬空），
//   把工程 shader 的 guid 设成同一个值 ⇒ **`.mat` 一个字节都不用改**，引用自动接上
//   （该 guid 在工程 `Assets/**` + `Packages/**` 的 `.meta` 里现读 0 命中，无冲突）。
//
// 🔴 本文件**不是**「自己设计」：语义完全来自原版反编译体。
// ⚠️ 本批不跑 Unity ⇒ **未经 Unity 编译验证**；真机/批处理验收见 `资料/完全复刻/报告/B107_R4.md`。
Shader "Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad" {
    SubShader {
        Tags {
            "IGNOREPROJECTOR"="true"
            "QUEUE"="Transparent+0"
            "RenderType"="Transparent"
        }
        Pass {
            Name "UniversalForwardOnly"
            Blend SrcAlpha One, SrcAlpha One
            ZClip On
            ZWrite Off
            Cull Off
            Tags {
                "LIGHTMODE"="UniversalForwardOnly"
            }
            HLSLPROGRAM
            #pragma target 5.0
            #include "HLSLSupport.cginc"
            #pragma vertex main
            #pragma fragment main

            // Stage: Vertex
            #ifdef SHADER_STAGE_VERTEX
            // Stage: Vertex, Blob: 1, ParamBlob: 0, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            ByteAddressBuffer graphValuesBuffer : register(t0);
            ByteAddressBuffer attributeBuffer : register(t1);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[21u] : packoffset(c0);
                float3 _WorldSpaceCameraPos : packoffset(c21);
                float _Globals_Unmapped_at_348 : packoffset(c21.w);
                float4 _Globals_Unmapped_at_352[56u] : packoffset(c22);
                column_major float4x4 unity_MatrixVP : packoffset(c78);
            };

            cbuffer type_UnityPerDraw : register(b1)
            {
                column_major float4x4 unity_ObjectToWorld : packoffset(c0);
                column_major float4x4 unity_WorldToObject : packoffset(c4);
            };

            cbuffer type_PerInstance : register(b2)
            {
                float _InstanceIndex : packoffset(c0);
                float _InstanceActiveIndex : packoffset(c0.y);
            };

            Texture2D<float4> bakedTexture : register(t2);
            SamplerState samplerbakedTexture : register(s0);

            static float4 gl_Position;
            static int gl_VertexIndex;
            static int gl_InstanceIndex;
            static int gl_BaseVertexARB;
            static int gl_BaseInstanceARB;
                        static float2 TEXCOORD;
            static float4 COLOR;
            static float3 TEXCOORD_6;
            static uint2 INDEX;

            struct SPIRV_Cross_Input
            {
                uint gl_VertexIndex : SV_VertexID;
                uint gl_InstanceIndex : SV_InstanceID;
            };

            struct SPIRV_Cross_Output
            {
                float2 TEXCOORD : TEXCOORD4;
                float4 COLOR : TEXCOORD8;
                float3 TEXCOORD_6 : TEXCOORD12;
                uint2 INDEX : TEXCOORD16;
                float4 gl_Position : SV_Position;
            };

            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }

            float _60(float3 _58, float3 _59)
            {
                float _64 = _58.x * _59.x;
                return mad(_58.z, _59.z, mad(_58.y, _59.y, _64));
            }

            float _74(uint _73)
            {
                return float(_73);
            }

            void vert_main()
            {
                uint _82 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _91 = (_82 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _97 = asuint(_InstanceActiveIndex);
                uint _98 = _97 * 112u;
                bool _116 = _91 >= graphValuesBuffer.Load(ByteAddressMask(_98 >> 2u, 4u) * 4 + 0);
                if (_116)
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _141 = asuint(_InstanceIndex) * 455104u;
                uint _145 = (_141 + (_91 << 2u)) << 2u;
                bool _153 = attributeBuffer.Load(ByteAddressMask((_145 + 700172u) >> 2u, 4u) * 4 + 0) == 0u;
                if (_153)
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                if (_153)
                {
                    gl_Position.x = _116 ? asfloat(0xffffffffu /* nan */) : 0.0f;
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _188 = ByteAddressMask((_98 + 80u) >> 2u, 4u);
                uint2 _194 = uint2(graphValuesBuffer.Load(_188 * 4 + 0), graphValuesBuffer.Load((_188 + 1u) * 4 + 0));
                uint _199 = (_91 + _141) << 2u;
                uint _209 = ByteAddressMask((_199 + 1260288u) >> 2u, 4u);
                uint _218 = ByteAddressMask((_145 + 700160u) >> 2u, 4u);
                uint3 _228 = uint3(attributeBuffer.Load(_218 * 4 + 0), attributeBuffer.Load((_218 + 1u) * 4 + 0), attributeBuffer.Load((_218 + 2u) * 4 + 0));
                uint _240 = ByteAddressMask((((_141 + (_91 * 3u)) << 2u) + 1400320u) >> 2u, 4u);
                uint3 _249 = uint3(attributeBuffer.Load(_240 * 4 + 0), attributeBuffer.Load((_240 + 1u) * 4 + 0), attributeBuffer.Load((_240 + 2u) * 4 + 0));
                COLOR.x = asfloat(_249.x);
                COLOR.y = asfloat(_249.y);
                COLOR.z = asfloat(_249.z);
                uint _262 = ByteAddressMask((_145 + 140032u) >> 2u, 4u);
                uint4 _275 = uint4(attributeBuffer.Load(_262 * 4 + 0), attributeBuffer.Load((_262 + 1u) * 4 + 0), attributeBuffer.Load((_262 + 2u) * 4 + 0), attributeBuffer.Load((_262 + 3u) * 4 + 0));
                float _286 = mad(clamp(asfloat(_275.w) / asfloat(attributeBuffer.Load(ByteAddressMask(_199 >> 2u, 4u) * 4 + 0)), 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                float _313 = asfloat(_228.x);
                float _314 = asfloat(_228.y);
                float _315 = asfloat(_228.z);
                float3 _316 = float3(_313, _314, _315);
                float _318 = rsqrt(_60(_316, _316));
                float _319 = _318 * _313;
                float _320 = _318 * _314;
                float _321 = _318 * _315;
                float _326 = asfloat(asuint(_WorldSpaceCameraPos.y));
                float _346 = asfloat(asuint(_WorldSpaceCameraPos.x));
                float _360 = asfloat(asuint(_WorldSpaceCameraPos.z));
                float _373 = asfloat(_275.x);
                float _374 = asfloat(_275.y);
                float _375 = asfloat(_275.z);
                float _376 = _374 - (mad(unity_WorldToObject[2u].y, _360, mad(unity_WorldToObject[0u].y, _346, _326 * unity_WorldToObject[1u].y)) + unity_WorldToObject[3u].y);
                float _377 = _375 - (mad(unity_WorldToObject[2u].z, _360, mad(unity_WorldToObject[0u].z, _346, _326 * unity_WorldToObject[1u].z)) + unity_WorldToObject[3u].z);
                float _378 = _373 - (mad(unity_WorldToObject[2u].x, _360, mad(unity_WorldToObject[0u].x, _346, _326 * unity_WorldToObject[1u].x)) + unity_WorldToObject[3u].x);
                float _385 = mad(_320, _377, -(_321 * _376));
                float _386 = mad(_321, _378, -(_319 * _377));
                float _387 = mad(_319, _376, -(_320 * _378));
                float3 _388 = float3(_385, _386, _387);
                float _389 = _60(_388, _388);
                bool _390 = _389 < 9.9999997473787516355514526367188e-06f;
                float _392 = rsqrt(_389);
                float _394 = _392 * _386;
                float _395 = _392 * _387;
                float _396 = _390 ? 1.0f : (_392 * _385);
                float _402 = -(_390 ? 0.0f : (_320 * _395));
                float _403 = -(_321 * _396);
                float _412 = _74(_82 & 1u);
                float _413 = _74(_82 & 2u);
                float _418 = asfloat(attributeBuffer.Load(_209 * 4 + 0));
                float _419 = _418 * 0.1500000059604644775390625f;
                float3 _430 = float3(_412 - 0.5f, mad(_413, 0.5f, -0.5f), 1.0f);
                float _431 = _60(float3(_419 * _396, _418 * _319, _373), _430);
                float _433 = _60(float3(_390 ? 0.0f : (_419 * _394), _418 * _320, _374), _430);
                float _435 = _60(float3(_390 ? 0.0f : (_419 * _395), _418 * _321, _375), _430);
                float _465 = mad(unity_ObjectToWorld[2u].x, _435, mad(unity_ObjectToWorld[0u].x, _431, _433 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _466 = mad(unity_ObjectToWorld[2u].y, _435, mad(unity_ObjectToWorld[0u].y, _431, _433 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _467 = mad(unity_ObjectToWorld[2u].z, _435, mad(unity_ObjectToWorld[0u].z, _431, _433 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _467, mad((unity_MatrixVP[0u].x), _465, _466 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _467, mad((unity_MatrixVP[0u].y), _465, _466 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _467, mad((unity_MatrixVP[0u].z), _465, _466 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _467, mad((unity_MatrixVP[0u].w), _465, _466 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _535 = asfloat(attributeBuffer.Load(_209 * 4 + 0));
                float _536 = 1.0f / _535;
                float3 _543 = float3(-(_536 * (_390 ? _402 : mad(_321, _394, _402))), -(_536 * (_390 ? _403 : mad(_319, _395, _403))), -(_536 * mad(_396, _320, -(_390 ? 0.0f : (_319 * _394)))));
                float _550 = _60(_543, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _557 = _60(_543, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _564 = _60(_543, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _565 = float3(_550, _557, _564);
                float _567 = rsqrt(_60(_565, _565));
                float _574 = ((_535 * (_535 * _419)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_567 * _550) * _574;
                TEXCOORD_6.y = (_567 * _557) * _574;
                TEXCOORD_6.z = (_567 * _564) * _574;
                COLOR.w = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_194.y) > 0.5f) ? (mad(-frac(_286 * 128.0f), 0.0078125f, _286) + 0.00390625f) : _286, asfloat(_194.x)), 0.0f).w;
                TEXCOORD.x = _412;
                TEXCOORD.y = _413 * 0.5f;
                INDEX.x = 0u;
                INDEX.y = _97;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                gl_VertexIndex = int(stage_input.gl_VertexIndex);
                gl_InstanceIndex = int(stage_input.gl_InstanceIndex);
                gl_BaseVertexARB = 0;
                gl_BaseInstanceARB = 0;
                vert_main();
                SPIRV_Cross_Output stage_output;
                stage_output.gl_Position = gl_Position;
                stage_output.TEXCOORD = TEXCOORD;
                stage_output.COLOR = COLOR;
                stage_output.TEXCOORD_6 = TEXCOORD_6;
                stage_output.INDEX = INDEX;
                return stage_output;
            }

            #endif

            // Stage: Fragment
            #ifdef SHADER_STAGE_FRAGMENT
            // Stage: Fragment, Blob: 3, ParamBlob: 2, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[4u] : packoffset(c0);
                float2 _GlobalMipBias : packoffset(c4);
            };

            Texture2D<float4> mainTexture : register(t0);
            SamplerState samplermainTexture : register(s0);

            static float2 TEXCOORD;
            static float4 COLOR;
            static float4 spv_Color;

            struct SPIRV_Cross_Input
            {
                float2 TEXCOORD : TEXCOORD4;
                nointerpolation float4 COLOR : TEXCOORD8;
            };

            struct SPIRV_Cross_Output
            {
                float4 SV_Target : SV_Target0;
            };

            void frag_main()
            {
                float4 _44 = mainTexture.SampleBias(samplermainTexture, float2(TEXCOORD.x, TEXCOORD.y), asfloat(asuint(_GlobalMipBias.x)));
                spv_Color.w = clamp(_44.w * COLOR.w, 0.0f, 1.0f);
                spv_Color.x = _44.x * COLOR.x;
                spv_Color.y = _44.y * COLOR.y;
                spv_Color.z = _44.z * COLOR.z;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                TEXCOORD = stage_input.TEXCOORD;
                COLOR = stage_input.COLOR;
                frag_main();
                SPIRV_Cross_Output stage_output;
                stage_output.SV_Target = spv_Color;
                return stage_output;
            }

            #endif

            ENDHLSL
        }
        Pass {
            Blend SrcAlpha One, SrcAlpha One
            ZClip On
            ZWrite Off
            Cull Off
            Tags {
                "LIGHTMODE"="Universal2D"
            }
            HLSLPROGRAM
            #pragma target 5.0
            #include "HLSLSupport.cginc"
            #pragma vertex main
            #pragma fragment main

            // Stage: Vertex
            #ifdef SHADER_STAGE_VERTEX
            // Stage: Vertex, Blob: 1, ParamBlob: 0, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            ByteAddressBuffer graphValuesBuffer : register(t0);
            ByteAddressBuffer attributeBuffer : register(t1);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[21u] : packoffset(c0);
                float3 _WorldSpaceCameraPos : packoffset(c21);
                float _Globals_Unmapped_at_348 : packoffset(c21.w);
                float4 _Globals_Unmapped_at_352[56u] : packoffset(c22);
                column_major float4x4 unity_MatrixVP : packoffset(c78);
            };

            cbuffer type_UnityPerDraw : register(b1)
            {
                column_major float4x4 unity_ObjectToWorld : packoffset(c0);
                column_major float4x4 unity_WorldToObject : packoffset(c4);
            };

            cbuffer type_PerInstance : register(b2)
            {
                float _InstanceIndex : packoffset(c0);
                float _InstanceActiveIndex : packoffset(c0.y);
            };

            Texture2D<float4> bakedTexture : register(t2);
            SamplerState samplerbakedTexture : register(s0);

            static float4 gl_Position;
            static int gl_VertexIndex;
            static int gl_InstanceIndex;
            static int gl_BaseVertexARB;
            static int gl_BaseInstanceARB;
                        static float2 TEXCOORD;
            static float4 COLOR;
            static float3 TEXCOORD_6;
            static uint2 INDEX;

            struct SPIRV_Cross_Input
            {
                uint gl_VertexIndex : SV_VertexID;
                uint gl_InstanceIndex : SV_InstanceID;
            };

            struct SPIRV_Cross_Output
            {
                float2 TEXCOORD : TEXCOORD4;
                float4 COLOR : TEXCOORD8;
                float3 TEXCOORD_6 : TEXCOORD12;
                uint2 INDEX : TEXCOORD16;
                float4 gl_Position : SV_Position;
            };

            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }

            float _60(float3 _58, float3 _59)
            {
                float _64 = _58.x * _59.x;
                return mad(_58.z, _59.z, mad(_58.y, _59.y, _64));
            }

            float _74(uint _73)
            {
                return float(_73);
            }

            void vert_main()
            {
                uint _82 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _91 = (_82 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _97 = asuint(_InstanceActiveIndex);
                uint _98 = _97 * 112u;
                bool _116 = _91 >= graphValuesBuffer.Load(ByteAddressMask(_98 >> 2u, 4u) * 4 + 0);
                if (_116)
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _141 = asuint(_InstanceIndex) * 455104u;
                uint _145 = (_141 + (_91 << 2u)) << 2u;
                bool _153 = attributeBuffer.Load(ByteAddressMask((_145 + 700172u) >> 2u, 4u) * 4 + 0) == 0u;
                if (_153)
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                if (_153)
                {
                    gl_Position.x = _116 ? asfloat(0xffffffffu /* nan */) : 0.0f;
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _188 = ByteAddressMask((_98 + 80u) >> 2u, 4u);
                uint2 _194 = uint2(graphValuesBuffer.Load(_188 * 4 + 0), graphValuesBuffer.Load((_188 + 1u) * 4 + 0));
                uint _199 = (_91 + _141) << 2u;
                uint _209 = ByteAddressMask((_199 + 1260288u) >> 2u, 4u);
                uint _218 = ByteAddressMask((_145 + 700160u) >> 2u, 4u);
                uint3 _228 = uint3(attributeBuffer.Load(_218 * 4 + 0), attributeBuffer.Load((_218 + 1u) * 4 + 0), attributeBuffer.Load((_218 + 2u) * 4 + 0));
                uint _240 = ByteAddressMask((((_141 + (_91 * 3u)) << 2u) + 1400320u) >> 2u, 4u);
                uint3 _249 = uint3(attributeBuffer.Load(_240 * 4 + 0), attributeBuffer.Load((_240 + 1u) * 4 + 0), attributeBuffer.Load((_240 + 2u) * 4 + 0));
                COLOR.x = asfloat(_249.x);
                COLOR.y = asfloat(_249.y);
                COLOR.z = asfloat(_249.z);
                uint _262 = ByteAddressMask((_145 + 140032u) >> 2u, 4u);
                uint4 _275 = uint4(attributeBuffer.Load(_262 * 4 + 0), attributeBuffer.Load((_262 + 1u) * 4 + 0), attributeBuffer.Load((_262 + 2u) * 4 + 0), attributeBuffer.Load((_262 + 3u) * 4 + 0));
                float _286 = mad(clamp(asfloat(_275.w) / asfloat(attributeBuffer.Load(ByteAddressMask(_199 >> 2u, 4u) * 4 + 0)), 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                float _313 = asfloat(_228.x);
                float _314 = asfloat(_228.y);
                float _315 = asfloat(_228.z);
                float3 _316 = float3(_313, _314, _315);
                float _318 = rsqrt(_60(_316, _316));
                float _319 = _318 * _313;
                float _320 = _318 * _314;
                float _321 = _318 * _315;
                float _326 = asfloat(asuint(_WorldSpaceCameraPos.y));
                float _346 = asfloat(asuint(_WorldSpaceCameraPos.x));
                float _360 = asfloat(asuint(_WorldSpaceCameraPos.z));
                float _373 = asfloat(_275.x);
                float _374 = asfloat(_275.y);
                float _375 = asfloat(_275.z);
                float _376 = _374 - (mad(unity_WorldToObject[2u].y, _360, mad(unity_WorldToObject[0u].y, _346, _326 * unity_WorldToObject[1u].y)) + unity_WorldToObject[3u].y);
                float _377 = _375 - (mad(unity_WorldToObject[2u].z, _360, mad(unity_WorldToObject[0u].z, _346, _326 * unity_WorldToObject[1u].z)) + unity_WorldToObject[3u].z);
                float _378 = _373 - (mad(unity_WorldToObject[2u].x, _360, mad(unity_WorldToObject[0u].x, _346, _326 * unity_WorldToObject[1u].x)) + unity_WorldToObject[3u].x);
                float _385 = mad(_320, _377, -(_321 * _376));
                float _386 = mad(_321, _378, -(_319 * _377));
                float _387 = mad(_319, _376, -(_320 * _378));
                float3 _388 = float3(_385, _386, _387);
                float _389 = _60(_388, _388);
                bool _390 = _389 < 9.9999997473787516355514526367188e-06f;
                float _392 = rsqrt(_389);
                float _394 = _392 * _386;
                float _395 = _392 * _387;
                float _396 = _390 ? 1.0f : (_392 * _385);
                float _402 = -(_390 ? 0.0f : (_320 * _395));
                float _403 = -(_321 * _396);
                float _412 = _74(_82 & 1u);
                float _413 = _74(_82 & 2u);
                float _418 = asfloat(attributeBuffer.Load(_209 * 4 + 0));
                float _419 = _418 * 0.1500000059604644775390625f;
                float3 _430 = float3(_412 - 0.5f, mad(_413, 0.5f, -0.5f), 1.0f);
                float _431 = _60(float3(_419 * _396, _418 * _319, _373), _430);
                float _433 = _60(float3(_390 ? 0.0f : (_419 * _394), _418 * _320, _374), _430);
                float _435 = _60(float3(_390 ? 0.0f : (_419 * _395), _418 * _321, _375), _430);
                float _465 = mad(unity_ObjectToWorld[2u].x, _435, mad(unity_ObjectToWorld[0u].x, _431, _433 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _466 = mad(unity_ObjectToWorld[2u].y, _435, mad(unity_ObjectToWorld[0u].y, _431, _433 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _467 = mad(unity_ObjectToWorld[2u].z, _435, mad(unity_ObjectToWorld[0u].z, _431, _433 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _467, mad((unity_MatrixVP[0u].x), _465, _466 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _467, mad((unity_MatrixVP[0u].y), _465, _466 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _467, mad((unity_MatrixVP[0u].z), _465, _466 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _467, mad((unity_MatrixVP[0u].w), _465, _466 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _535 = asfloat(attributeBuffer.Load(_209 * 4 + 0));
                float _536 = 1.0f / _535;
                float3 _543 = float3(-(_536 * (_390 ? _402 : mad(_321, _394, _402))), -(_536 * (_390 ? _403 : mad(_319, _395, _403))), -(_536 * mad(_396, _320, -(_390 ? 0.0f : (_319 * _394)))));
                float _550 = _60(_543, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _557 = _60(_543, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _564 = _60(_543, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _565 = float3(_550, _557, _564);
                float _567 = rsqrt(_60(_565, _565));
                float _574 = ((_535 * (_535 * _419)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_567 * _550) * _574;
                TEXCOORD_6.y = (_567 * _557) * _574;
                TEXCOORD_6.z = (_567 * _564) * _574;
                COLOR.w = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_194.y) > 0.5f) ? (mad(-frac(_286 * 128.0f), 0.0078125f, _286) + 0.00390625f) : _286, asfloat(_194.x)), 0.0f).w;
                TEXCOORD.x = _412;
                TEXCOORD.y = _413 * 0.5f;
                INDEX.x = 0u;
                INDEX.y = _97;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                gl_VertexIndex = int(stage_input.gl_VertexIndex);
                gl_InstanceIndex = int(stage_input.gl_InstanceIndex);
                gl_BaseVertexARB = 0;
                gl_BaseInstanceARB = 0;
                vert_main();
                SPIRV_Cross_Output stage_output;
                stage_output.gl_Position = gl_Position;
                stage_output.TEXCOORD = TEXCOORD;
                stage_output.COLOR = COLOR;
                stage_output.TEXCOORD_6 = TEXCOORD_6;
                stage_output.INDEX = INDEX;
                return stage_output;
            }

            #endif

            // Stage: Fragment
            #ifdef SHADER_STAGE_FRAGMENT
            // Stage: Fragment, Blob: 3, ParamBlob: 2, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[4u] : packoffset(c0);
                float2 _GlobalMipBias : packoffset(c4);
            };

            Texture2D<float4> mainTexture : register(t0);
            SamplerState samplermainTexture : register(s0);

            static float2 TEXCOORD;
            static float4 COLOR;
            static float4 spv_Color;

            struct SPIRV_Cross_Input
            {
                float2 TEXCOORD : TEXCOORD4;
                nointerpolation float4 COLOR : TEXCOORD8;
            };

            struct SPIRV_Cross_Output
            {
                float4 SV_Target : SV_Target0;
            };

            void frag_main()
            {
                float4 _44 = mainTexture.SampleBias(samplermainTexture, float2(TEXCOORD.x, TEXCOORD.y), asfloat(asuint(_GlobalMipBias.x)));
                spv_Color.w = clamp(_44.w * COLOR.w, 0.0f, 1.0f);
                spv_Color.x = _44.x * COLOR.x;
                spv_Color.y = _44.y * COLOR.y;
                spv_Color.z = _44.z * COLOR.z;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                TEXCOORD = stage_input.TEXCOORD;
                COLOR = stage_input.COLOR;
                frag_main();
                SPIRV_Cross_Output stage_output;
                stage_output.SV_Target = spv_Color;
                return stage_output;
            }

            #endif

            ENDHLSL
        }
    }
}
